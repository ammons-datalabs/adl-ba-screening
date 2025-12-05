namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class GeoPointTests
{
    [Fact]
    public void Constructor_ValidLatLon_CreatesValue()
    {
        const double lat = -27.4705;
        const double lon = 153.0260;

        var point = new GeoPoint(lat, lon);

        Assert.Equal(lat, point.Latitude);
        Assert.Equal(lon, point.Longitude);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Constructor_InvalidLatitude_Throws(double lat)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(lat, 153));
        Assert.Equal("Latitude", ex.ParamName);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Constructor_InvalidLongitude_Throws(double lon)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(-27, lon));
        Assert.Equal("Longitude", ex.ParamName);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var p1 = new GeoPoint(-27.4705, 153.0260);
        var p2 = new GeoPoint(-27.4705, 153.0260);
        var p3 = new GeoPoint(-27.5, 153.0);

        Assert.Equal(p1, p2);
        Assert.NotEqual(p1, p3);
    }
}