using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodSummaryMapperTests
{
    [Fact]
    public void FromResult_WithHighRisk_ReturnsSummaryWithHighRisk()
    {
        var result = new FloodLookupResult
        {
            Address = "3/241 Horizon Drive, Westlake",
            Risk = FloodRisk.High,
            Source = FloodDataSource.BccParcelMetrics,
            Scope = FloodDataScope.PlanFallback,
            Reasons = ["Risk derived from BCC parcel metrics for plan GTP102995."]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("3/241 Horizon Drive, Westlake", summary.Address);
        Assert.Equal("High", summary.OverallRisk);
        Assert.Equal("BCC_PARCEL_METRICS", summary.Source);
        Assert.Equal("PlanFallback", summary.Scope);
        Assert.True(summary.HasFloodInfo);
        Assert.Contains("GTP102995", summary.Notes);
    }

    [Fact]
    public void FromResult_WithNoRisk_ReturnsSummaryWithNone()
    {
        var result = new FloodLookupResult
        {
            Address = "117 Fernberg Road, Paddington",
            Risk = FloodRisk.None,
            Source = FloodDataSource.BccParcelMetrics,
            Scope = FloodDataScope.Parcel,
            Reasons = ["BCC parcel metrics indicate no flood risk for 1RP84382."]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("None", summary.OverallRisk);
        Assert.Equal("Parcel", summary.Scope);
        Assert.True(summary.HasFloodInfo);
    }

    [Fact]
    public void FromResult_WithUnknownRisk_ReturnsSummaryWithNoFloodInfo()
    {
        var result = new FloodLookupResult
        {
            Address = "Invalid Address",
            Risk = FloodRisk.Unknown,
            Source = FloodDataSource.Unknown,
            Scope = FloodDataScope.Unknown,
            Reasons = ["Geocoding failed: NotFound"]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("Unknown", summary.OverallRisk);
        Assert.Equal("UNKNOWN", summary.Source);
        Assert.False(summary.HasFloodInfo);
    }

    [Fact]
    public void FromResult_WithPointBuffer_ReturnsPointBufferSource()
    {
        var result = new FloodLookupResult
        {
            Address = "100 Eagle Street, Brisbane",
            Risk = FloodRisk.Medium,
            Source = FloodDataSource.PointBuffer,
            Scope = FloodDataScope.Unknown,
            Reasons = ["Location falls inside Medium likelihood flood zone (GIS)."]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("POINT_BUFFER", summary.Source);
        Assert.True(summary.HasFloodInfo);
    }

    [Fact]
    public void FromResult_WithEmptyReasons_ReturnsNullNotes()
    {
        var result = new FloodLookupResult
        {
            Address = "Test Address",
            Risk = FloodRisk.None,
            Source = FloodDataSource.BccParcelMetrics,
            Scope = FloodDataScope.Parcel,
            Reasons = []
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Null(summary.Notes);
    }

    [Fact]
    public void FromResult_WithMultipleReasons_CombinesIntoNotes()
    {
        var result = new FloodLookupResult
        {
            Address = "Test Address",
            Risk = FloodRisk.Medium,
            Source = FloodDataSource.BccParcelMetrics,
            Scope = FloodDataScope.PlanFallback,
            Reasons = ["Risk derived from BCC parcel metrics.", "Source flags: FL_MED_RIVER, FLOOD_INFO"]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Contains("Risk derived from BCC parcel metrics.", summary.Notes);
        Assert.Contains("FL_MED_RIVER", summary.Notes);
    }
}
