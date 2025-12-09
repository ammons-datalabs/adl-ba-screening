using System.Net.Http.Json;
using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Geo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

/// <summary>
/// Integration tests for Tier 3 (point-buffer) flood lookup behavior.
/// Uses HybridFloodDataProvider with an empty metrics index to test point-buffer fallback.
/// </summary>
public class FloodLookupGisIntegrationTests
{
    [Fact]
    public async Task Lookup_ReturnsHighRiskFromPointBuffer()
    {
        await using var factory = new GisTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = "1 Flood St" } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("High", content);
        Assert.Contains("point buffer", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lookup_ReturnsNoneRiskWhenOutsideZone()
    {
        await using var factory = new GisTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(IGeocodingService) ||
                        d.ServiceType == typeof(IFloodZoneIndex) ||
                        d.ServiceType == typeof(IFloodDataProvider) ||
                        d.ServiceType == typeof(IBccParcelMetricsIndex))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove) services.Remove(descriptor);

                services.AddSingleton<IGeocodingService, OutsideZoneGeocodingService>();
                services.AddSingleton<IFloodZoneIndex, TestFloodZoneIndex>();
                services.AddSingleton<IBccParcelMetricsIndex>(new InMemoryBccParcelMetricsIndex([], []));
                services.AddScoped<IFloodDataProvider, HybridFloodDataProvider>();
            });
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/screening/flood/lookup", new
        {
            Properties = new[] { new { Address = "100 Safe Street" } }
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Outside zone should be None risk (successfully checked, no zone found)
        Assert.Contains("None", content);
        Assert.Contains("No flood zone found", content);
    }

    private sealed class GisTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
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
                        d.ServiceType == typeof(IBccParcelMetricsIndex))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove) services.Remove(descriptor);

                // Register test implementations (empty metrics to force Tier 3 fallback)
                services.AddSingleton<IGeocodingService, TestGeocodingService>();
                services.AddSingleton<IFloodZoneIndex, TestFloodZoneIndex>();
                services.AddSingleton<IBccParcelMetricsIndex>(new InMemoryBccParcelMetricsIndex([], []));
                services.AddScoped<IFloodDataProvider, HybridFloodDataProvider>();
            });
        }
    }

    private sealed class TestGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                NormalizedAddress = "1 Flood Street, Testville",
                Location = new GeoPoint(-27.46, 153.02), // Inside the test zone
                Status = GeocodingStatus.Success,
                Provider = nameof(TestGeocodingService)
            });
        }
    }

    private sealed class TestFloodZoneIndex : IFloodZoneIndex
    {
        private readonly FloodZone _zone;

        public TestFloodZoneIndex()
        {
            var poly = GeoFactory.CreatePolygon(
                new GeoPoint(-27.48, 153.00),
                new GeoPoint(-27.48, 153.05),
                new GeoPoint(-27.45, 153.05),
                new GeoPoint(-27.45, 153.00));

            _zone = new FloodZone
            {
                Id = "zone-1",
                Risk = FloodRisk.High,
                Geometry = poly
            };
        }

        public FloodZone? FindZoneForPoint(GeoPoint point)
        {
            var ntsPoint = GeoFactory.CreatePoint(point);
            return _zone.Geometry.Contains(ntsPoint) ? _zone : null;
        }

        public FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres)
        {
            var ntsPoint = GeoFactory.CreatePoint(point);
            if (_zone.Geometry.Contains(ntsPoint))
                return new FloodZoneHit
                {
                    Zone = _zone,
                    DistanceMetres = 0,
                    Proximity = FloodZoneProximity.Inside
                };
            return null;
        }

        public FloodRisk? FindRiskOverlayForPoint(GeoPoint point) => null;
    }

    private sealed class OutsideZoneGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                NormalizedAddress = "100 Safe Street, Safeville",
                Location = new GeoPoint(-27.50, 153.20), // Outside the test zone
                Status = GeocodingStatus.Success,
                Provider = nameof(OutsideZoneGeocodingService)
            });
        }
    }
}