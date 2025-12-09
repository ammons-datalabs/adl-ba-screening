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

    private static FloodZone MediumRiskZone()
    {
        var poly = GeoFactory.CreatePolygon(
            new GeoPoint(-27.50, 153.10),
            new GeoPoint(-27.50, 153.15),
            new GeoPoint(-27.47, 153.15),
            new GeoPoint(-27.47, 153.10));

        return new FloodZone
        {
            Id = "bcc-medium-1",
            Risk = FloodRisk.Medium,
            Geometry = poly
        };
    }

    [Fact]
    public void Constructor_ThrowsOnNullLoader()
    {
        var options = Options.Create(new FloodDataOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new BccFloodZoneIndex(null!, options));
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        var loader = new StubFloodZoneDataLoader([], []);

        Assert.Throws<ArgumentNullException>(() =>
            new BccFloodZoneIndex(loader, null!));
    }

    [Fact]
    public void FirstCall_LoadsZonesFromLoader()
    {
        var options = Options.Create(new FloodDataOptions { DataRoot = "/tmp/flood" });
        var loader = new StubFloodZoneDataLoader([HighRiskZone()], []);

        var index = new BccFloodZoneIndex(loader, options);

        var zone = index.FindZoneForPoint(new GeoPoint(-27.46, 153.02));

        Assert.NotNull(zone);
        Assert.Equal("bcc-high-1", zone!.Id);
        Assert.Equal(1, loader.LoadExtentsCalls);
    }

    [Fact]
    public void MultipleCalls_OnlyLoadOnce()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([HighRiskZone()], []);
        var index = new BccFloodZoneIndex(loader, options);

        var pt = new GeoPoint(-27.46, 153.02);

        _ = index.FindZoneForPoint(pt);
        _ = index.FindZoneForPoint(pt);
        _ = index.FindZoneForPoint(pt);

        Assert.Equal(1, loader.LoadExtentsCalls);
    }

    [Fact]
    public void NoZonesLoaded_ReturnsNull()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([], []);

        var index = new BccFloodZoneIndex(loader, options);

        var zone = index.FindZoneForPoint(new GeoPoint(-27.46, 153.02));

        Assert.Null(zone);
    }

    [Fact]
    public void FindNearestZone_DelegatesToExtentsIndex()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([HighRiskZone()], []);
        var index = new BccFloodZoneIndex(loader, options);

        // Point outside the zone but close enough to find
        var hit = index.FindNearestZone(new GeoPoint(-27.44, 153.02), maxDistanceMetres: 5000);

        Assert.NotNull(hit);
        Assert.Equal("bcc-high-1", hit!.Zone.Id);
        Assert.True(hit.DistanceMetres > 0);
    }

    [Fact]
    public void FindNearestZone_ReturnsNull_WhenNoZoneWithinDistance()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([HighRiskZone()], []);
        var index = new BccFloodZoneIndex(loader, options);

        // Point very far from zone
        var hit = index.FindNearestZone(new GeoPoint(-28.00, 154.00), maxDistanceMetres: 100);

        Assert.Null(hit);
    }

    [Fact]
    public void FindRiskOverlayForPoint_LoadsRiskZonesSeparately()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([HighRiskZone()], [MediumRiskZone()]);
        var index = new BccFloodZoneIndex(loader, options);

        // First call extents (loads extents)
        _ = index.FindZoneForPoint(new GeoPoint(-27.46, 153.02));
        Assert.Equal(1, loader.LoadExtentsCalls);
        Assert.Equal(0, loader.LoadRiskCalls);

        // Now call risk overlay (loads risk zones)
        var risk = index.FindRiskOverlayForPoint(new GeoPoint(-27.48, 153.12));

        Assert.Equal(1, loader.LoadExtentsCalls);
        Assert.Equal(1, loader.LoadRiskCalls);
        Assert.Equal(FloodRisk.Medium, risk);
    }

    [Fact]
    public void FindRiskOverlayForPoint_ReturnsNull_WhenPointOutsideRiskZones()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([], [MediumRiskZone()]);
        var index = new BccFloodZoneIndex(loader, options);

        var risk = index.FindRiskOverlayForPoint(new GeoPoint(-27.00, 153.00));

        Assert.Null(risk);
    }

    [Fact]
    public void FindRiskOverlayForPoint_OnlyLoadsRiskZonesOnce()
    {
        var options = Options.Create(new FloodDataOptions());
        var loader = new StubFloodZoneDataLoader([], [MediumRiskZone()]);
        var index = new BccFloodZoneIndex(loader, options);

        var pt = new GeoPoint(-27.48, 153.12);

        _ = index.FindRiskOverlayForPoint(pt);
        _ = index.FindRiskOverlayForPoint(pt);
        _ = index.FindRiskOverlayForPoint(pt);

        Assert.Equal(1, loader.LoadRiskCalls);
    }

    private sealed class StubFloodZoneDataLoader(
        IEnumerable<FloodZone> extentsZones,
        IEnumerable<FloodZone> riskZones) : IFloodZoneDataLoader
    {
        private readonly IReadOnlyList<FloodZone> _extentsZones = extentsZones.ToList();
        private readonly IReadOnlyList<FloodZone> _riskZones = riskZones.ToList();

        public int LoadExtentsCalls { get; private set; }
        public int LoadRiskCalls { get; private set; }

        public IReadOnlyList<FloodZone> LoadZones(FloodDataOptions options,
            CancellationToken cancellationToken = default)
        {
            LoadExtentsCalls++;
            return _extentsZones;
        }

        public IReadOnlyList<FloodZone> LoadRiskZones(FloodDataOptions options,
            CancellationToken cancellationToken = default)
        {
            LoadRiskCalls++;
            return _riskZones;
        }
    }
}