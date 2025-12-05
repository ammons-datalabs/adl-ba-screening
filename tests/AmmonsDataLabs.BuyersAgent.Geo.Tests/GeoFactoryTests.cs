using NetTopologySuite.Geometries;

namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class GeoFactoryTests
{
    [Fact]
    public void CreatePoint_UsesLonAsX_LatAsY()
    {
        var gp = new GeoPoint(-27.4705, 153.0260);

        var point = GeoFactory.CreatePoint(gp);

        Assert.Equal(153.0260, point.X, 4);
        Assert.Equal(-27.4705, point.Y, 4);
    }

    [Fact]
    public void CreatePolygon_ClosesRingAndIsValid()
    {
        var p1 = new GeoPoint(0, 0);
        var p2 = new GeoPoint(0, 1);
        var p3 = new GeoPoint(1, 1);
        var p4 = new GeoPoint(1, 0);

        var poly = GeoFactory.CreatePolygon(p1, p2, p3, p4);

        Assert.IsType<Polygon>(poly);
        Assert.True(poly.IsValid);
        Assert.True(poly.Shell.IsClosed);
        Assert.Equal(5, poly.NumPoints); // closed ring
    }

    [Fact]
    public void PolygonContainsPoint_InsideSquare_ReturnsTrue()
    {
        var p1 = new GeoPoint(0, 0);
        var p2 = new GeoPoint(0, 1);
        var p3 = new GeoPoint(1, 1);
        var p4 = new GeoPoint(1, 0);
        var poly = GeoFactory.CreatePolygon(p1, p2, p3, p4);

        var inside = GeoFactory.CreatePoint(new GeoPoint(0.5, 0.5));
        var outside = GeoFactory.CreatePoint(new GeoPoint(2, 2));

        Assert.True(poly.Contains(inside));
        Assert.False(poly.Contains(outside));
    }
}