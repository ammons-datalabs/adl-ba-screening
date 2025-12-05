using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class BccFloodZoneIndexTests
{
    private static FloodZone HighRiskZone()
    {
        var poly = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        return new FloodZone
        {
            Id = "bcc-high-1",
            Risk = FloodRisk.High,
            Geometry = poly
        };
    }

    [Fact]
    public void FirstCall_LoadsZonesFromLoader()
    {
        var options = Options.Create(new FloodDataOptions { DataRoot = "/tmp/flood" });
        var loader = new StubFloodZoneDataLoader([HighRiskZone()]);

        var index = new BccFloodZoneIndex(loader, options);

        var zone = index.FindZoneForPoint(new GeoPoint(-27.46, 153.02));

        Assert.NotNull(zone);
        Assert.Equal("bcc-high-1", zone!.Id);
        Assert.Equal(1, loader.LoadCalls);
    }

    [Fact]
    public void MultipleCalls_OnlyLoadOnce()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([HighRiskZone()]);
        var index = new BccFloodZoneIndex(loader, options);

        var pt = new GeoPoint(-27.46, 153.02);

        _ = index.FindZoneForPoint(pt);
        _ = index.FindZoneForPoint(pt);
        _ = index.FindZoneForPoint(pt);

        Assert.Equal(1, loader.LoadCalls);
    }

    [Fact]
    public void NoZonesLoaded_ReturnsNull()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([]);

        var index = new BccFloodZoneIndex(loader, options);

        var zone = index.FindZoneForPoint(new GeoPoint(-27.46, 153.02));

        Assert.Null(zone);
    }

    private sealed class StubFloodZoneDataLoader(IEnumerable<FloodZone> zones) : IFloodZoneDataLoader
    {
        private readonly IReadOnlyList<FloodZone> _zones = zones.ToList();
        public int LoadCalls { get; private set; }

        public IReadOnlyList<FloodZone> LoadZones(FloodDataOptions options,
            CancellationToken cancellationToken = default)
        {
            LoadCalls++;
            return _zones;
        }
    }
}