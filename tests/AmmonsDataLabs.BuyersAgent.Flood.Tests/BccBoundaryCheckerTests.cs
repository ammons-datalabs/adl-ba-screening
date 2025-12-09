using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class BccBoundaryCheckerTests
{
    [Fact]
    public void IsInsideBccBounds_BrisbaneCbd_ReturnsTrue()
    {
        var point = new GeoPoint(-27.4705, 153.0260);
        Assert.True(BccBoundaryChecker.IsInsideBccBounds(point));
    }

    [Fact]
    public void IsInsideBccBounds_Sydney_ReturnsFalse()
    {
        var point = new GeoPoint(-33.8688, 151.2093);
        Assert.False(BccBoundaryChecker.IsInsideBccBounds(point));
    }

    [Fact]
    public void IsInsideBccBounds_GoldCoast_ReturnsFalse()
    {
        // Gold Coast is south of BCC bounds
        var point = new GeoPoint(-28.0167, 153.4000);
        Assert.False(BccBoundaryChecker.IsInsideBccBounds(point));
    }
}
