using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodZoneIndexTests
{
    private static FloodZone HighRiskSquare()
    {
        var poly = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        return new FloodZone
        {
            Id = "high-1",
            Risk = FloodRisk.High,
            Geometry = poly
        };
    }

    private static FloodZone LowRiskBiggerSquare()
    {
        var poly = GeoFactory.CreatePolygon(
            new GeoPoint(-27.50, 152.98),
            new GeoPoint(-27.50, 153.07),
            new GeoPoint(-27.43, 153.07),
            new GeoPoint(-27.43, 152.98));

        return new FloodZone
        {
            Id = "low-1",
            Risk = FloodRisk.Low,
            Geometry = poly
        };
    }

    [Fact]
    public void FindZoneForPoint_InsideSingleZone_ReturnsThatZone()
    {
        var zone = HighRiskSquare();
        var index = new InMemoryFloodZoneIndex([zone]);
        var point = new GeoPoint(-27.46, 153.02);

        var result = index.FindZoneForPoint(point);

        Assert.NotNull(result);
        Assert.Equal(zone.Id, result!.Id);
        Assert.Equal(FloodRisk.High, result.Risk);
    }

    [Fact]
    public void FindZoneForPoint_OutsideAllZones_ReturnsNull()
    {
        var zone = HighRiskSquare();
        var index = new InMemoryFloodZoneIndex([zone]);
        var point = new GeoPoint(-27.60, 153.20);

        var result = index.FindZoneForPoint(point);

        Assert.Null(result);
    }

    [Fact]
    public void FindZoneForPoint_InOverlappingZones_ReturnsHighestRisk()
    {
        var high = HighRiskSquare();
        var low = LowRiskBiggerSquare();
        var index = new InMemoryFloodZoneIndex([low, high]);
        var point = new GeoPoint(-27.46, 153.02);

        var result = index.FindZoneForPoint(point);

        Assert.NotNull(result);
        Assert.Equal(FloodRisk.High, result!.Risk);
        Assert.Equal("high-1", result.Id);
    }

    [Fact]
    public void FindNearestZone_ReturnsInside_WhenPointInsidePolygon()
    {
        var zone = HighRiskSquare();
        var index = new InMemoryFloodZoneIndex([zone]);
        var pointInside = new GeoPoint(-27.46, 153.02);

        var result = index.FindNearestZone(pointInside, 30);

        Assert.NotNull(result);
        Assert.Equal(FloodZoneProximity.Inside, result!.Proximity);
        Assert.Equal(0, result.DistanceMetres);
        Assert.Equal(zone.Id, result.Zone.Id);
    }

    [Fact]
    public void FindNearestZone_ReturnsNear_WhenOutsideButWithinBuffer()
    {
        var zone = HighRiskSquare();
        var index = new InMemoryFloodZoneIndex([zone]);
        var pointJustOutside = new GeoPoint(-27.481, 153.02);

        var result = index.FindNearestZone(pointJustOutside, 200);

        Assert.NotNull(result);
        Assert.Equal(FloodZoneProximity.Near, result!.Proximity);
        Assert.True(result.DistanceMetres > 0);
        Assert.True(result.DistanceMetres <= 200);
        Assert.Equal(zone.Id, result.Zone.Id);
    }

    [Fact]
    public void FindNearestZone_ReturnsNull_WhenNoZoneWithinBuffer()
    {
        var zone = HighRiskSquare();
        var index = new InMemoryFloodZoneIndex([zone]);
        var pointFarAway = new GeoPoint(-27.60, 153.20);

        var result = index.FindNearestZone(pointFarAway, 30);

        Assert.Null(result);
    }

    [Fact]
    public void FindNearestZone_ReturnsHighestRisk_WhenMultipleZonesWithinBuffer()
    {
        var high = HighRiskSquare();
        var low = LowRiskBiggerSquare();
        var index = new InMemoryFloodZoneIndex([low, high]);
        var pointInside = new GeoPoint(-27.46, 153.02);

        var result = index.FindNearestZone(pointInside, 30);

        Assert.NotNull(result);
        Assert.Equal(FloodRisk.High, result!.Zone.Risk);
        Assert.Equal(FloodZoneProximity.Inside, result.Proximity);
    }
}