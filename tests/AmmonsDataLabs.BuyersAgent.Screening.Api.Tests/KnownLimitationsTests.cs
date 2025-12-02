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
/// Documents known limitations of the flood screening system.
/// These tests are marked as Skip to prevent CI failures while documenting expected future behavior.
///
/// Reference materials:
/// - Resources/FloodWiseReports/3-241 Horizon Drive FloodWise Report.pdf
/// </summary>
public class KnownLimitationsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _geocodingFilePath;

    public KnownLimitationsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"limitations-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _geocodingFilePath = Path.Combine(_tempDir, "test-geocoding.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// KNOWN LIMITATION: Point-based geocoding cannot accurately assess flood risk for multi-dwelling developments.
    ///
    /// Background:
    /// - Address: 3/241 Horizon Drive, Westlake QLD 4074 (Lot 3 on GTP102995)
    /// - BCC FloodWise Report shows: Medium risk (1% AEP flood extent affects 75-100% of property)
    /// - Our system returns: Low risk / Near proximity (~7.7m from flood zone)
    ///
    /// Root cause:
    /// - Geocoding returns the centroid of the multi-dwelling development (241 Horizon Drive)
    /// - The centroid is outside the flood extent, but the actual lot boundary intersects with it
    /// - BCC FloodWise uses lot boundary intersection analysis, we use point-in-polygon
    ///
    /// Resolution:
    /// - Would require cadastral (lot boundary) data to perform proper lot-to-flood-zone intersection
    /// - Queensland cadastral data available from QSpatial: https://qldspatial.information.qld.gov.au
    ///
    /// Reference: Resources/FloodWiseReports/3-241 Horizon Drive FloodWise Report.pdf
    /// </summary>
    [Fact(Skip = "Known limitation: Point-based geocoding cannot assess lot boundary flood intersection for multi-dwelling developments")]
    public async Task HorizonDrive_LotBoundaryIntersection_ShouldReturnMediumRisk()
    {
        // Arrange - Real coordinates from Azure Maps geocoding
        var geocodingData = new[]
        {
            new { address = "3/241 Horizon Drive, Westlake QLD 4074", lat = -27.5491928, lon = 152.9106459 }
        };
        await File.WriteAllTextAsync(_geocodingFilePath, JsonSerializer.Serialize(geocodingData));

        await using var factory = CreateFactoryWithRealFloodData();
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = "3/241 Horizon Drive, Westlake QLD 4074" } }
        });

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Assert - BCC FloodWise reports Medium risk for this lot
        // Currently fails: Our system returns Low/Near because the geocoded point is outside the flood extent
        Assert.Fail($"""
            KNOWN LIMITATION: Point-based geocoding cannot assess lot boundary flood intersection

            Address: 3/241 Horizon Drive, Westlake QLD 4074 (Lot 3 on GTP102995)

            Expected (BCC FloodWise):
              - Risk: Medium
              - 1% AEP flood extent affects 75-100% of property
              - Based on lot boundary intersection analysis

            Actual (Our System):
              {content}

            Root Cause:
              - Geocoding returns centroid of multi-dwelling development
              - Centroid coordinates: -27.5491928, 152.9106459
              - Point is ~7.7m from flood zone (outside), but lot boundary intersects it

            Resolution:
              - Requires cadastral (lot boundary) data for lot-to-flood-zone intersection
              - QSpatial: https://qldspatial.information.qld.gov.au

            Reference: Resources/FloodWiseReports/3-241 Horizon Drive FloodWise Report.pdf
            """);
    }

    /// <summary>
    /// Documents current behavior for comparison with the known limitation above.
    /// This test passes and shows what our system currently returns.
    /// </summary>
    [Fact]
    public async Task HorizonDrive_CurrentBehavior_ReturnsLowNearProximity()
    {
        // Arrange - Real coordinates from Azure Maps geocoding
        var geocodingData = new[]
        {
            new { address = "3/241 Horizon Drive, Westlake QLD 4074", lat = -27.5491928, lon = 152.9106459 }
        };
        await File.WriteAllTextAsync(_geocodingFilePath, JsonSerializer.Serialize(geocodingData));

        await using var factory = CreateFactoryWithRealFloodData();
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = "3/241 Horizon Drive, Westlake QLD 4074" } }
        });

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Current behavior: Returns Low risk with Near proximity
        // The geocoded point is ~7.7m from the flood zone (not inside it)
        Assert.Contains("\"risk\":\"Low\"", content);
        Assert.Contains("\"proximity\":\"Near\"", content);
    }

    private WebApplicationFactory<Program> CreateFactoryWithRealFloodData()
    {
        // Use real BCC flood data from processed directory
        var floodDataRoot = "/Users/jaybea/Projects/ADL/BA-Screening-Data/bcc-flood/processed";
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
                            d.ServiceType == typeof(IFloodZoneDataLoader))
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Use FileGeocodingService
                    services.AddSingleton<IGeocodingService>(_ =>
                    {
                        var options = Microsoft.Extensions.Options.Options.Create(
                            new FileGeocodingOptions { FilePath = geocodingPath });
                        return new FileGeocodingService(options);
                    });

                    // Use real flood data
                    services.AddSingleton<IFloodZoneDataLoader, NdjsonFloodZoneDataLoader>();
                    services.AddSingleton<IFloodZoneIndex>(sp =>
                    {
                        var loader = sp.GetRequiredService<IFloodZoneDataLoader>();
                        var options = Microsoft.Extensions.Options.Options.Create(
                            new FloodDataOptions
                            {
                                DataRoot = floodDataRoot,
                                ExtentsFile = "flood-risk.ndjson"
                            });
                        return new BccFloodZoneIndex(loader, options);
                    });

                    services.AddScoped<IFloodDataProvider, GisFloodDataProvider>();
                });
            });
    }
}
