using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class ParquetMetricsDataPrepTests : IDisposable
{
    private readonly string _tempDir;

    public ParquetMetricsDataPrepTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"parquet-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Run_BuildsParcelMetrics()
    {
        // Arrange
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        TestParquetBuilder.CreateTestMetricsFile(parquetPath,
            ("1GTP102995", "FL_HIGH_RIVER", "1"),
            ("1GTP102995", "FLOOD_INFO", "1"),
            ("0GTP102995", "FL_LOW_RIVER", "1"));

        // Act
        var result = ParquetMetricsDataPrep.Run(parquetPath);

        // Assert: parcel-level
        var lot1 = result.ParcelMetrics.Single(r => r.LotPlan == "1GTP102995");
        Assert.Equal(FloodRisk.High, lot1.RiverRisk);
        Assert.True(lot1.HasFloodInfo);

        var lot0 = result.ParcelMetrics.Single(r => r.LotPlan == "0GTP102995");
        Assert.Equal(FloodRisk.Low, lot0.RiverRisk);
    }

    [Fact]
    public void Run_BuildsPlanMetrics_MaxOfAllLots()
    {
        // Arrange
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        TestParquetBuilder.CreateTestMetricsFile(parquetPath,
            ("1GTP102995", "FL_HIGH_RIVER", "1"),
            ("0GTP102995", "FL_LOW_RIVER", "1"),
            ("10GTP102995", "FL_MED_CREEK", "1"));

        // Act
        var result = ParquetMetricsDataPrep.Run(parquetPath);

        // Assert: plan-level should aggregate max risk from all lots
        var plan = result.PlanMetrics.Single(r => r.Plan == "GTP102995");
        Assert.Equal(FloodRisk.High, plan.RiverRisk); // Max(High, Low) = High
        Assert.Equal(FloodRisk.Medium, plan.CreekRisk); // Only lot 10 had creek
        Assert.Equal(FloodRisk.High, plan.OverallRisk); // Max(High, Medium) = High
    }

    [Fact]
    public void Run_HandlesMultiplePlans()
    {
        // Arrange
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        TestParquetBuilder.CreateTestMetricsFile(parquetPath,
            ("1GTP102995", "FL_HIGH_RIVER", "1"),
            ("1RP84382", "FL_LOW_CREEK", "1"));

        // Act
        var result = ParquetMetricsDataPrep.Run(parquetPath);

        // Assert
        Assert.Equal(2, result.PlanMetrics.Count);
        Assert.Contains(result.PlanMetrics, p => p.Plan == "GTP102995");
        Assert.Contains(result.PlanMetrics, p => p.Plan == "RP84382");
    }

    [Fact]
    public void Run_SkipsEmptyLotplans()
    {
        // Arrange
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        TestParquetBuilder.CreateTestMetricsFile(parquetPath,
            ("1GTP102995", "FL_HIGH_RIVER", "1"),
            ("", "FL_LOW_RIVER", "1")); // Empty lotplan should be skipped

        // Act
        var result = ParquetMetricsDataPrep.Run(parquetPath);

        // Assert
        Assert.Single(result.ParcelMetrics);
        Assert.Single(result.PlanMetrics);
    }

    [Fact]
    public void Run_TracksAepLevels()
    {
        // Arrange
        var parquetPath = Path.Combine(_tempDir, "test.parquet");
        TestParquetBuilder.CreateTestMetricsFile(parquetPath,
            ("1GTP102995", "01AEP_RIVER", "13.6"),
            ("1GTP102995", "002AEP_RIVER", "17.5"),
            ("1GTP102995", "FL_DFL", "11.9"));

        // Act
        var result = ParquetMetricsDataPrep.Run(parquetPath);

        // Assert
        var lot1 = result.ParcelMetrics.Single(r => r.LotPlan == "1GTP102995");
        Assert.Equal(13.6m, lot1.OnePercentAepRiver);
        Assert.Equal(17.5m, lot1.PointTwoPercentAepRiver);
        Assert.Equal(11.9m, lot1.DefinedFloodLevel);
    }
}
