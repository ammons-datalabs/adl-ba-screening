using static System.Enum;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodDomainTests
{
    [Fact]
    public void FloodRisk_HasExpectedValues()
    {
        // Assert
        Assert.True(IsDefined(typeof(FloodRisk), FloodRisk.Unknown));
        Assert.True(IsDefined(typeof(FloodRisk), FloodRisk.None));
        Assert.True(IsDefined(typeof(FloodRisk), FloodRisk.Low));
        Assert.True(IsDefined(typeof(FloodRisk), FloodRisk.Medium));
        Assert.True(IsDefined(typeof(FloodRisk), FloodRisk.High));
    }
}