using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class BccParcelMetricsNdjsonWriterTests
{
    [Fact]
    public void Write_ProducesOneJsonPerLine()
    {
        // Arrange
        var records = new List<BccParcelMetricsRecord>
        {
            new()
            {
                LotPlan = "1GTP102995",
                Plan = "GTP102995",
                OverallRisk = FloodRisk.High,
                RiverRisk = FloodRisk.High,
                CreekRisk = FloodRisk.Unknown,
                StormTideRisk = FloodRisk.Unknown,
                HasFloodInfo = true,
                EvidenceMetrics = ["FL_HIGH_RIVER", "FLOOD_INFO"]
            },
            new()
            {
                LotPlan = "1RP84382",
                Plan = "RP84382",
                OverallRisk = FloodRisk.Unknown,
                RiverRisk = FloodRisk.Unknown,
                CreekRisk = FloodRisk.Unknown,
                StormTideRisk = FloodRisk.Unknown,
                HasFloodInfo = false,
                EvidenceMetrics = []
            }
        };

        using var sw = new StringWriter();

        // Act
        BccParcelMetricsNdjsonWriter.Write(records, sw);

        // Assert
        var lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        // Each line should be valid JSON
        foreach (var line in lines)
        {
            var doc = JsonDocument.Parse(line);
            Assert.NotNull(doc);
        }
    }

    [Fact]
    public void Write_IncludesAllFields()
    {
        // Arrange
        var record = new BccParcelMetricsRecord
        {
            LotPlan = "1GTP102995",
            Plan = "GTP102995",
            OverallRisk = FloodRisk.High,
            RiverRisk = FloodRisk.High,
            CreekRisk = FloodRisk.Medium,
            StormTideRisk = FloodRisk.Low,
            HasFloodInfo = true,
            HasOverlandFlow = true,
            OnePercentAepRiver = 13.6m,
            PointTwoPercentAepRiver = 17.5m,
            DefinedFloodLevel = 11.9m,
            HistoricFloodLevel1 = 13.3m,
            EvidenceMetrics = ["FL_HIGH_RIVER"]
        };

        using var sw = new StringWriter();

        // Act
        BccParcelMetricsNdjsonWriter.Write([record], sw);

        // Assert
        var json = sw.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("1GTP102995", root.GetProperty("lotplan").GetString());
        Assert.Equal("GTP102995", root.GetProperty("plan").GetString());
        Assert.Equal("High", root.GetProperty("overall_risk").GetString());
        Assert.Equal("High", root.GetProperty("river_risk").GetString());
        Assert.Equal("Medium", root.GetProperty("creek_risk").GetString());
        Assert.Equal("Low", root.GetProperty("storm_tide_risk").GetString());
        Assert.True(root.GetProperty("has_flood_info").GetBoolean());
        Assert.True(root.GetProperty("has_overland_flow").GetBoolean());
        Assert.Equal(13.6m, root.GetProperty("one_percent_aep_river").GetDecimal());
        Assert.Equal(17.5m, root.GetProperty("point_two_percent_aep_river").GetDecimal());
        Assert.Equal(11.9m, root.GetProperty("defined_flood_level").GetDecimal());
        Assert.Equal(13.3m, root.GetProperty("historic_flood_level_1").GetDecimal());
    }

    [Fact]
    public void Write_OmitsNullNumericFields()
    {
        // Arrange
        var record = new BccParcelMetricsRecord
        {
            LotPlan = "1RP84382",
            Plan = "RP84382",
            OverallRisk = FloodRisk.Unknown,
            RiverRisk = FloodRisk.Unknown,
            CreekRisk = FloodRisk.Unknown,
            StormTideRisk = FloodRisk.Unknown,
            HasFloodInfo = false,
            EvidenceMetrics = []
        };

        using var sw = new StringWriter();

        // Act
        BccParcelMetricsNdjsonWriter.Write([record], sw);

        // Assert
        var json = sw.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("one_percent_aep_river", out _));
        Assert.False(root.TryGetProperty("point_two_percent_aep_river", out _));
        Assert.False(root.TryGetProperty("defined_flood_level", out _));
    }
}
