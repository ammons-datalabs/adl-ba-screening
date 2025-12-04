using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class LotAccumulatorTests
{
    [Fact]
    public void ApplyMetric_TracksHighestRiskPerSource_River()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_LOW_RIVER", "1");
        acc.ApplyMetric("FL_HIGH_RIVER", "1");

        var record = acc.ToRecord("3GTP102995", "GTP102995");

        Assert.Equal(FloodRisk.High, record.RiverRisk);
        Assert.Equal(FloodRisk.High, record.OverallRisk);
        Assert.Contains("FL_LOW_RIVER", record.EvidenceMetrics);
        Assert.Contains("FL_HIGH_RIVER", record.EvidenceMetrics);
    }

    [Fact]
    public void ApplyMetric_TracksHighestRiskPerSource_Creek()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_MED_CREEK", "1");

        var record = acc.ToRecord("20SP191298", "SP191298");

        Assert.Equal(FloodRisk.Medium, record.CreekRisk);
        Assert.Equal(FloodRisk.Medium, record.OverallRisk);
    }

    [Fact]
    public void ToRecord_OverallRiskIsMaxOfSources()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_MED_RIVER", "1");
        acc.ApplyMetric("FL_HIGH_CREEK", "1");

        var record = acc.ToRecord("20SP191298", "SP191298");

        Assert.Equal(FloodRisk.Medium, record.RiverRisk);
        Assert.Equal(FloodRisk.High, record.CreekRisk);
        Assert.Equal(FloodRisk.High, record.OverallRisk);
    }

    [Fact]
    public void ApplyMetric_TracksFloodInfo()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FLOOD_INFO", "1");

        var record = acc.ToRecord("1GTP102995", "GTP102995");

        Assert.True(record.HasFloodInfo);
        Assert.Contains("FLOOD_INFO", record.EvidenceMetrics);
    }

    [Fact]
    public void ApplyMetric_TracksStormTide()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_LOW_ST", "1");

        var record = acc.ToRecord("1SP123456", "SP123456");

        Assert.Equal(FloodRisk.Low, record.StormTideRisk);
    }

    [Fact]
    public void ApplyMetric_TracksOverlandFlow()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("OLF_FLAG", "1");

        var record = acc.ToRecord("1SP123456", "SP123456");

        Assert.True(record.HasOverlandFlow);
    }

    [Fact]
    public void ApplyMetric_IgnoresZeroValues()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_HIGH_RIVER", "0");

        var record = acc.ToRecord("1SP123456", "SP123456");

        Assert.Equal(FloodRisk.Unknown, record.RiverRisk);
        Assert.DoesNotContain("FL_HIGH_RIVER", record.EvidenceMetrics);
    }

    [Fact]
    public void ApplyMetric_IgnoresNullValues()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_HIGH_RIVER", null);

        var record = acc.ToRecord("1SP123456", "SP123456");

        Assert.Equal(FloodRisk.Unknown, record.RiverRisk);
    }

    [Fact]
    public void ApplyMetric_IgnoresEmptyStringValues()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_HIGH_RIVER", "");

        var record = acc.ToRecord("1SP123456", "SP123456");

        Assert.Equal(FloodRisk.Unknown, record.RiverRisk);
    }

    [Fact]
    public void ApplyMetric_VeryLowMapsToLow()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_VLOW_RIVER", "1");

        var record = acc.ToRecord("1SP123456", "SP123456");

        // VLOW (Very Low) should map to Low risk
        Assert.Equal(FloodRisk.Low, record.RiverRisk);
    }

    [Fact]
    public void ApplyMetric_TracksAepLevels()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("01AEP_RIVER", "13.6");
        acc.ApplyMetric("002AEP_RIVER", "17.5");

        var record = acc.ToRecord("1GTP102995", "GTP102995");

        Assert.Equal(13.6m, record.OnePercentAepRiver);
        Assert.Equal(17.5m, record.PointTwoPercentAepRiver);
    }

    [Fact]
    public void ApplyMetric_TracksDefinedFloodLevel()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_DFL", "11.9");

        var record = acc.ToRecord("1GTP102995", "GTP102995");

        Assert.Equal(11.9m, record.DefinedFloodLevel);
    }

    [Fact]
    public void ApplyMetric_TracksHistoricFloodLevels()
    {
        var acc = new LotAccumulator();

        acc.ApplyMetric("FL_HIS1_RIVER", "13.3");

        var record = acc.ToRecord("1GTP102995", "GTP102995");

        Assert.Equal(13.3m, record.HistoricFloodLevel1);
    }

    [Fact]
    public void ToRecord_NoMetrics_ReturnsUnknownRisk()
    {
        var acc = new LotAccumulator();

        var record = acc.ToRecord("1RP84382", "RP84382");

        Assert.Equal(FloodRisk.Unknown, record.OverallRisk);
        Assert.Equal(FloodRisk.Unknown, record.RiverRisk);
        Assert.False(record.HasFloodInfo);
        Assert.Empty(record.EvidenceMetrics);
    }
}
