using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class GisFloodDataProviderTests
{
    // Test-only stub nested classes
    private sealed class StubGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.Equals(address, "1 Flood St", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new GeocodingResult
                {
                    Query = address,
                    NormalizedAddress = "1 Flood Street, Testville",
                    Location = new GeoPoint(-27.46, 153.02),
                    Status = GeocodingStatus.Success,
                    Provider = nameof(StubGeocodingService)
                });
            }

            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.NotFound,
                Provider = nameof(StubGeocodingService)
            });
        }
    }

    private sealed class StubFloodZoneIndex : IFloodZoneIndex
    {
        private readonly FloodZone? _zone;

        public StubFloodZoneIndex(FloodZone? zone) => _zone = zone;

        public FloodZone? FindZoneForPoint(GeoPoint point) => _zone;
    }

    [Fact]
    public async Task LookupAsync_AddressInHighRiskZone_ReturnsHighRiskWithReason()
    {
        var geocoding = new StubGeocodingService();

        var polygon = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        var zone = new FloodZone
        {
            Id = "zone-1",
            Risk = FloodRisk.High,
            Geometry = polygon
        };

        var index = new StubFloodZoneIndex(zone);

        var sut = new GisFloodDataProvider(geocoding, index);

        var result = await sut.LookupAsync("1 Flood St", CancellationToken.None);

        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.Equal("1 Flood Street, Testville", result.Address);
        Assert.Contains("flood zone", string.Join(' ', result.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_GeocodingNotFound_ReturnsUnknownRisk()
    {
        var sut = new GisFloodDataProvider(new StubGeocodingService(), new StubFloodZoneIndex(null));

        var result = await sut.LookupAsync("999 Nowhere Rd", CancellationToken.None);

        Assert.Equal(FloodRisk.Unknown, result.Risk);
        Assert.Contains("Geocoding failed", string.Join(' ', result.Reasons));
    }

    [Fact]
    public async Task LookupAsync_NoZoneFound_TreatedAsUnknownWithReason()
    {
        var geocoding = new StubGeocodingService();
        var index = new StubFloodZoneIndex(null);
        var sut = new GisFloodDataProvider(geocoding, index);

        var result = await sut.LookupAsync("1 Flood St", CancellationToken.None);

        Assert.Equal(FloodRisk.Unknown, result.Risk);
        Assert.Contains("No flood zone found", string.Join(' ', result.Reasons));
    }

    [Fact]
    public async Task LookupAsync_BlankAddress_ReturnsUnknownWithReason()
    {
        var sut = new GisFloodDataProvider(new StubGeocodingService(), new StubFloodZoneIndex(null));

        var result = await sut.LookupAsync("   ", CancellationToken.None);

        Assert.Equal(FloodRisk.Unknown, result.Risk);
        Assert.Contains("Address was empty", string.Join(' ', result.Reasons));
    }
}
