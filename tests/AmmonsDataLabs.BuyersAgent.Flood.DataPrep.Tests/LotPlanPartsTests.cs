using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class LotPlanPartsTests
{
    [Theory]
    [InlineData("3GTP102995", "3", "GTP102995")]
    [InlineData("0GTP102995", "0", "GTP102995")]
    [InlineData("00000BUP13745", "00000", "BUP13745")]
    [InlineData("11SP116632", "11", "SP116632")]
    [InlineData("1RP84382", "1", "RP84382")]
    [InlineData("20SP191298", "20", "SP191298")]
    [InlineData("90RP851746", "90", "RP851746")]
    public void Parse_SplitsCorrectly(string lotplan, string expectedLot, string expectedPlan)
    {
        var parts = LotPlanParts.Parse(lotplan);

        Assert.Equal(expectedLot, parts.Lot);
        Assert.Equal(expectedPlan, parts.Plan);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ThrowsOnNullOrEmpty(string? lotplan)
    {
        Assert.Throws<ArgumentException>(() => LotPlanParts.Parse(lotplan!));
    }

    [Theory]
    [InlineData("GTP102995")]   // No lot number prefix
    [InlineData("123")]         // No plan suffix
    public void Parse_ThrowsOnInvalidFormat(string lotplan)
    {
        Assert.Throws<FormatException>(() => LotPlanParts.Parse(lotplan));
    }

    [Fact]
    public void CommonLotPlan_ReturnsLot0OnSamePlan()
    {
        var parts = LotPlanParts.Parse("3GTP102995");

        Assert.Equal("0GTP102995", parts.CommonLotPlan);
    }

    [Fact]
    public void IsCommonLot_ReturnsTrueForLot0()
    {
        var parts = LotPlanParts.Parse("0GTP102995");

        Assert.True(parts.IsCommonLot);
    }

    [Fact]
    public void IsCommonLot_ReturnsFalseForNonZeroLot()
    {
        var parts = LotPlanParts.Parse("3GTP102995");

        Assert.False(parts.IsCommonLot);
    }
}
