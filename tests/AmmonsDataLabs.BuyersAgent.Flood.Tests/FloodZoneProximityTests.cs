namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodZoneProximityTests
{
    [Fact]
    public void FloodZoneProximity_HasExpectedValues()
    {
        Assert.Equal(0, (int)FloodZoneProximity.None);
        Assert.True(Enum.IsDefined(typeof(FloodZoneProximity), FloodZoneProximity.Inside));
        Assert.True(Enum.IsDefined(typeof(FloodZoneProximity), FloodZoneProximity.Near));
    }

    [Fact]
    public void FloodZoneHit_CanBeCreatedWithRequiredProperties()
    {
        var zone = new FloodZone
        {
            Id = "test-zone",
            Risk = FloodRisk.Medium,
            Geometry = null!
        };

        var hit = new FloodZoneHit
        {
            Zone = zone,
            DistanceMetres = 15.5,
            Proximity = FloodZoneProximity.Near
        };

        Assert.Equal(zone, hit.Zone);
        Assert.Equal(15.5, hit.DistanceMetres);
        Assert.Equal(FloodZoneProximity.Near, hit.Proximity);
    }

    [Fact]
    public void FloodZoneHit_InsideProximity_HasZeroDistance()
    {
        var zone = new FloodZone
        {
            Id = "inside-zone",
            Risk = FloodRisk.High,
            Geometry = null!
        };

        var hit = new FloodZoneHit
        {
            Zone = zone,
            DistanceMetres = 0,
            Proximity = FloodZoneProximity.Inside
        };

        Assert.Equal(FloodZoneProximity.Inside, hit.Proximity);
        Assert.Equal(0, hit.DistanceMetres);
    }
}