using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class BccParcelMetricsIndexTests
{
    [Fact]
    public void TryGet_ParcelHit_ReturnsParcelScope()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"3GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":["FL_HIGH_RIVER"]}"""
            ],
            []);

        Assert.True(index.TryGet("3GTP102995", out var metrics));
        Assert.Equal(MetricsScope.Parcel, metrics.Scope);
        Assert.Equal(FloodRisk.High, metrics.OverallRisk);
        Assert.Equal(FloodRisk.High, metrics.RiverRisk);
        Assert.True(metrics.HasFloodInfo);
        Assert.Contains("FL_HIGH_RIVER", metrics.EvidenceMetrics);
    }

    [Fact]
    public void TryGet_PlanFallback_WhenParcelMissing()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"1GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            [
                """{"lotplan":"PLAN:GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":["FL_HIGH_RIVER"]}"""
            ]);

        // Lot 3 doesn't exist in parcel data, should fallback to plan
        Assert.True(index.TryGet("3GTP102995", out var metrics));
        Assert.Equal(MetricsScope.PlanFallback, metrics.Scope);
        Assert.Equal(FloodRisk.High, metrics.OverallRisk);
        Assert.Equal("3GTP102995", metrics.LotPlanOrPlanKey); // Returns the queried lotplan
        Assert.Equal("GTP102995", metrics.Plan);
    }

    [Fact]
    public void TryGet_NoMetrics_ReturnsFalse()
    {
        var index = BccParcelMetricsIndexTestFactory.Create([], []);

        Assert.False(index.TryGet("20SP191298", out _));
    }

    [Fact]
    public void TryGet_ParcelExists_DoesNotUsePlanFallback()
    {
        // Even if plan has higher risk, parcel metrics should be used
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"1GTP102995","plan":"GTP102995","overall_risk":"Low","river_risk":"Low","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            [
                """{"lotplan":"PLAN:GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":["FL_HIGH_RIVER"]}"""
            ]);

        Assert.True(index.TryGet("1GTP102995", out var metrics));
        Assert.Equal(MetricsScope.Parcel, metrics.Scope);
        Assert.Equal(FloodRisk.Low, metrics.OverallRisk); // Uses parcel risk, not plan
    }

    [Fact]
    public void TryGet_CaseInsensitive()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"3GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            []);

        Assert.True(index.TryGet("3gtp102995", out var metrics));
        Assert.Equal(FloodRisk.High, metrics.OverallRisk);
    }

    [Fact]
    public void TryGet_PlanFallback_CaseInsensitive()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [],
            [
                """{"lotplan":"PLAN:GTP102995","plan":"GTP102995","overall_risk":"Medium","river_risk":"Medium","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ]);

        Assert.True(index.TryGet("5gtp102995", out var metrics));
        Assert.Equal(MetricsScope.PlanFallback, metrics.Scope);
        Assert.Equal(FloodRisk.Medium, metrics.OverallRisk);
    }

    [Fact]
    public void TryGet_InvalidLotPlanFormat_ReturnsFalse()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [],
            [
                """{"lotplan":"PLAN:GTP102995","plan":"GTP102995","overall_risk":"Medium","river_risk":"Medium","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ]);

        // Invalid lotplan format (no digits) should return false due to FormatException catch
        Assert.False(index.TryGet("INVALID", out _));
        Assert.False(index.TryGet("ABC", out _));
        Assert.False(index.TryGet("NO_PLAN_MATCH", out _));
    }

    [Fact]
    public void TryGet_SkipsEmptyAndNullLines()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                "",
                "   ",
                """{"lotplan":"1RP12345","plan":"RP12345","overall_risk":"Low","river_risk":"Low","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            []);

        // Should skip empty lines and still load valid data
        Assert.True(index.TryGet("1RP12345", out var metrics));
        Assert.Equal(FloodRisk.Low, metrics.OverallRisk);
    }

    [Fact]
    public void TryGet_ParsesAllRiskLevels()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"1RP11111","plan":"RP11111","overall_risk":"High","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}""",
                """{"lotplan":"2RP22222","plan":"RP22222","overall_risk":"Medium","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}""",
                """{"lotplan":"3RP33333","plan":"RP33333","overall_risk":"Low","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}""",
                """{"lotplan":"4RP44444","plan":"RP44444","overall_risk":"None","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":false,"has_overland_flow":false,"evidence_metrics":[]}""",
                """{"lotplan":"5RP55555","plan":"RP55555","overall_risk":"InvalidValue","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            []);

        Assert.True(index.TryGet("1RP11111", out var high));
        Assert.Equal(FloodRisk.High, high.OverallRisk);

        Assert.True(index.TryGet("2RP22222", out var medium));
        Assert.Equal(FloodRisk.Medium, medium.OverallRisk);

        Assert.True(index.TryGet("3RP33333", out var low));
        Assert.Equal(FloodRisk.Low, low.OverallRisk);

        Assert.True(index.TryGet("4RP44444", out var none));
        Assert.Equal(FloodRisk.None, none.OverallRisk);

        Assert.True(index.TryGet("5RP55555", out var invalid));
        Assert.Equal(FloodRisk.Unknown, invalid.OverallRisk); // Invalid values map to Unknown
    }

    [Fact]
    public void TryGet_HasOverlandFlow_SetsOverlandFlowRisk()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"1RP12345","plan":"RP12345","overall_risk":"Medium","river_risk":"Low","creek_risk":"Low","storm_tide_risk":"None","has_flood_info":true,"has_overland_flow":true,"evidence_metrics":["FL_OVERLAND"]}"""
            ],
            []);

        Assert.True(index.TryGet("1RP12345", out var metrics));
        Assert.Equal(FloodRisk.Unknown, metrics.OverlandFlowRisk); // has_overland_flow=true -> Unknown
        Assert.Contains("FL_OVERLAND", metrics.EvidenceMetrics);
    }

    [Fact]
    public void TryGet_NoOverlandFlow_SetsOverlandFlowRiskToNone()
    {
        var index = BccParcelMetricsIndexTestFactory.Create(
            [
                """{"lotplan":"1RP12345","plan":"RP12345","overall_risk":"Low","river_risk":"Low","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":[]}"""
            ],
            []);

        Assert.True(index.TryGet("1RP12345", out var metrics));
        Assert.Equal(FloodRisk.None, metrics.OverlandFlowRisk); // has_overland_flow=false -> None
    }
}

/// <summary>
/// Factory for creating in-memory BccParcelMetricsIndex instances for testing.
/// </summary>
public static class BccParcelMetricsIndexTestFactory
{
    public static IBccParcelMetricsIndex Create(
        string[] parcelJsonLines,
        string[] planJsonLines)
    {
        return new InMemoryBccParcelMetricsIndex(parcelJsonLines, planJsonLines);
    }
}

public class NdjsonBccParcelMetricsIndexTests : IDisposable
{
    private readonly string _tempDir;

    public NdjsonBccParcelMetricsIndexTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"metrics-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void TryGet_LoadsFromFiles_ParcelHit()
    {
        // Write parcel metrics file
        var parcelPath = Path.Combine(_tempDir, "parcel-metrics.ndjson");
        File.WriteAllText(parcelPath,
            """{"lotplan":"3GTP102995","plan":"GTP102995","overall_risk":"High","river_risk":"High","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":false,"evidence_metrics":["FL_HIGH_RIVER"]}""" +
            "\n");

        var options = Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            BccParcelMetricsParcelFile = "parcel-metrics.ndjson",
            BccParcelMetricsPlanFile = "plan-metrics.ndjson"
        });

        var index = new NdjsonBccParcelMetricsIndex(options);

        Assert.True(index.TryGet("3GTP102995", out var metrics));
        Assert.Equal(MetricsScope.Parcel, metrics.Scope);
        Assert.Equal(FloodRisk.High, metrics.OverallRisk);
    }

    [Fact]
    public void TryGet_LoadsFromFiles_PlanFallback()
    {
        // Write plan metrics file only (no parcel file)
        var planPath = Path.Combine(_tempDir, "plan-metrics.ndjson");
        File.WriteAllText(planPath,
            """{"lotplan":"PLAN:GTP102995","plan":"GTP102995","overall_risk":"Medium","river_risk":"Medium","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":true,"has_overland_flow":true,"evidence_metrics":["FL_MED_RIVER"]}""" +
            "\n");

        var options = Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            BccParcelMetricsParcelFile = "parcel-metrics.ndjson", // doesn't exist
            BccParcelMetricsPlanFile = "plan-metrics.ndjson"
        });

        var index = new NdjsonBccParcelMetricsIndex(options);

        // Lot 3 doesn't exist, should fallback to plan
        Assert.True(index.TryGet("3GTP102995", out var metrics));
        Assert.Equal(MetricsScope.PlanFallback, metrics.Scope);
        Assert.Equal(FloodRisk.Medium, metrics.OverallRisk);
    }

    [Fact]
    public void TryGet_MissingFiles_ReturnsFalse()
    {
        var options = Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            BccParcelMetricsParcelFile = "nonexistent-parcel.ndjson",
            BccParcelMetricsPlanFile = "nonexistent-plan.ndjson"
        });

        var index = new NdjsonBccParcelMetricsIndex(options);

        Assert.False(index.TryGet("3GTP102995", out _));
    }

    [Fact]
    public void TryGet_LazyLoads_OnlyOnce()
    {
        var parcelPath = Path.Combine(_tempDir, "parcel-metrics.ndjson");
        File.WriteAllText(parcelPath,
            """{"lotplan":"1RP84382","plan":"RP84382","overall_risk":"Unknown","river_risk":"Unknown","creek_risk":"Unknown","storm_tide_risk":"Unknown","has_flood_info":false,"has_overland_flow":false,"evidence_metrics":[]}""" +
            "\n");

        var options = Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            BccParcelMetricsParcelFile = "parcel-metrics.ndjson",
            BccParcelMetricsPlanFile = "plan-metrics.ndjson"
        });

        var index = new NdjsonBccParcelMetricsIndex(options);

        // First call loads the data
        Assert.True(index.TryGet("1RP84382", out var metrics1));
        Assert.False(metrics1.HasFloodInfo);

        // Second call should use cached data (we can't easily verify this without mocking,
        // but we can at least verify it returns the same result)
        Assert.True(index.TryGet("1RP84382", out var metrics2));
        Assert.False(metrics2.HasFloodInfo);
    }
}