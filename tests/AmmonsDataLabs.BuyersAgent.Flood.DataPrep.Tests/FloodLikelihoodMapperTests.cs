namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class FloodLikelihoodMapperTests
{
    [Theory]
    [InlineData("High", FloodRisk.High)]
    [InlineData("HIGH", FloodRisk.High)]
    [InlineData("Medium", FloodRisk.Medium)]
    [InlineData("Low", FloodRisk.Low)]
    [InlineData(null, FloodRisk.Unknown)]
    [InlineData("", FloodRisk.Unknown)]
    [InlineData("SomethingElse", FloodRisk.Unknown)]
    public void MapLikelihoodString_ReturnsExpectedEnum(string? input, FloodRisk expected)
    {
        var result = FloodLikelihoodMapper.Map(input);
        Assert.Equal(expected, result);
    }
}
