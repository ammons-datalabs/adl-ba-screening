using System.Net.Http.Json;
using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

/// <summary>
/// End-to-end integration tests using golden addresses mapped to known flood zones.
/// These tests use the sample-flood-risk.ndjson data with centroids calculated from actual polygons.
/// </summary>
public class GoldenAddressIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _geocodingFilePath;

    // Golden addresses mapped to centroids from sample-flood-risk.ndjson
    private static readonly GoldenAddress[] GoldenAddresses =
    [
        // Low Risk zones
        new("1 Low Risk Lane, Brisbane QLD", -27.402689, 152.977800, "Low"),
        new("2 Low Risk Lane, Brisbane QLD", -27.392803, 152.997297, "Low"),

        // Medium Risk zones
        new("1 Medium Risk St, Brisbane QLD", -27.379577, 152.999988, "Medium"),
        new("2 Medium Risk St, Brisbane QLD", -27.393363, 152.992768, "Medium"),

        // High Risk zones
        new("1 High Risk Ave, Brisbane QLD", -27.362153, 152.984494, "High"),
        new("2 High Risk Ave, Brisbane QLD", -27.407568, 152.994589, "High"),
    ];

    private record GoldenAddress(string Address, double Lat, double Lon, string ExpectedRisk);

    public GoldenAddressIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"golden-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        // Create geocoding file with golden addresses in NDJSON format
        _geocodingFilePath = Path.Combine(_tempDir, "golden-geocoding.ndjson");
        using var writer = File.CreateText(_geocodingFilePath);
        foreach (var a in GoldenAddresses)
        {
            // Parse address to extract components for component-based lookup
            var parts = ParseGoldenAddress(a.Address);
            var json = JsonSerializer.Serialize(new
            {
                lot_plan = $"TEST{parts.house}",
                house_number = parts.house,
                corridor_name = parts.street.ToUpperInvariant(),
                corridor_suffix_code = parts.suffix,
                suburb = parts.suburb.ToUpperInvariant(),
                latitude = a.Lat,
                longitude = a.Lon,
                normalized_address = a.Address
            });
            writer.WriteLine(json);
        }
    }

    private static (string house, string street, string suffix, string suburb) ParseGoldenAddress(string address)
    {
        // Parse addresses like "1 Low Risk Lane, Brisbane QLD"
        var commaIdx = address.IndexOf(',');
        var streetPart = address[..commaIdx].Trim();
        var suburbPart = address[(commaIdx + 1)..].Trim();

        // Remove state code from suburb
        var spaceIdx = suburbPart.LastIndexOf(' ');
        var suburb = spaceIdx > 0 ? suburbPart[..spaceIdx] : suburbPart;

        // Parse street: "1 Low Risk Lane" -> house="1", street="Low Risk", suffix="LN"
        var words = streetPart.Split(' ');
        var house = words[0];
        var suffix = NormalizeSuffix(words[^1]);
        var street = string.Join(" ", words[1..^1]);

        return (house, street, suffix, suburb);
    }

    private static string NormalizeSuffix(string suffix)
    {
        return suffix.ToUpperInvariant() switch
        {
            "LANE" => "LN",
            "STREET" or "ST" => "ST",
            "AVENUE" or "AVE" => "AVE",
            _ => suffix.ToUpperInvariant()
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        // The sample NDJSON is located in the DataPrep.Tests/Resources folder
        var resourcesPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests", "Resources"));

        var geocodingPath = _geocodingFilePath;

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove existing registrations
                    var descriptorsToRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(IGeocodingService) ||
                            d.ServiceType == typeof(IFloodZoneIndex) ||
                            d.ServiceType == typeof(IFloodDataProvider) ||
                            d.ServiceType == typeof(IFloodZoneDataLoader) ||
                            d.ServiceType == typeof(IBccParcelMetricsIndex))
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Use FileGeocodingService with golden addresses
                    services.AddSingleton<IGeocodingService>(_ =>
                    {
                        var options = Microsoft.Extensions.Options.Options.Create(
                            new FileGeocodingOptions { FilePath = geocodingPath });
                        return new FileGeocodingService(options);
                    });

                    // Register loader and index
                    services.AddSingleton<IFloodZoneDataLoader, NdjsonFloodZoneDataLoader>();
                    services.AddSingleton<IFloodZoneIndex>(sp =>
                    {
                        var loader = sp.GetRequiredService<IFloodZoneDataLoader>();
                        var options = Microsoft.Extensions.Options.Options.Create(
                            new FloodDataOptions
                            {
                                DataRoot = resourcesPath,
                                ExtentsFile = "sample-flood-risk.ndjson"
                            });
                        return new BccFloodZoneIndex(loader, options);
                    });

                    // Empty metrics index to test Tier 3 point-buffer behavior
                    services.AddSingleton<IBccParcelMetricsIndex>(new InMemoryBccParcelMetricsIndex([], []));
                    services.AddScoped<IFloodDataProvider, HybridFloodDataProvider>();
                });
            });
    }

    [Theory]
    [InlineData("1 Low Risk Lane, Brisbane QLD", "Low")]
    [InlineData("2 Low Risk Lane, Brisbane QLD", "Low")]
    public async Task Lookup_LowRiskGoldenAddress_ReturnsLowRisk(string address, string expectedRisk)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = address } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedRisk, content);
    }

    [Theory]
    [InlineData("1 Medium Risk St, Brisbane QLD", "Medium")]
    [InlineData("2 Medium Risk St, Brisbane QLD", "Medium")]
    public async Task Lookup_MediumRiskGoldenAddress_ReturnsMediumRisk(string address, string expectedRisk)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = address } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedRisk, content);
    }

    [Theory]
    [InlineData("1 High Risk Ave, Brisbane QLD", "High")]
    [InlineData("2 High Risk Ave, Brisbane QLD", "High")]
    public async Task Lookup_HighRiskGoldenAddress_ReturnsHighRisk(string address, string expectedRisk)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = address } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedRisk, content);
    }

    [Fact]
    public async Task Lookup_UnknownAddress_ReturnsGeocodingFailure()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = "999 Unknown St, Nowhere QLD" } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        // Should indicate geocoding failed with NotFound status
        Assert.Contains("Geocoding failed: NotFound", content);
    }

    [Fact]
    public async Task Lookup_AllRiskLevels_ReturnsExpectedResults()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Test all three risk levels in a single batch request
        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[]
            {
                new { Address = "1 Low Risk Lane, Brisbane QLD" },
                new { Address = "1 Medium Risk St, Brisbane QLD" },
                new { Address = "1 High Risk Ave, Brisbane QLD" }
            }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // All three risk levels should appear in response
        Assert.Contains("Low", content);
        Assert.Contains("Medium", content);
        Assert.Contains("High", content);
    }
}
