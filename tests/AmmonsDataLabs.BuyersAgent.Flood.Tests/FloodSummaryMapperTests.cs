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

    [Fact]
    public void FromResult_WithUnknownRiskButExtentIntersection_ReturnsHasFloodInfoTrue()
    {
        // Simulates 5 Bellambi Place scenario: inside unclassified flood extent
        var result = new FloodLookupResult
        {
            Address = "5 Bellambi Place, Westlake",
            Risk = FloodRisk.Unknown,
            Source = FloodDataSource.PointBuffer,
            Scope = FloodDataScope.Unknown,
            HasAnyExtentIntersection = true,
            Reasons = ["Location falls inside Unknown likelihood flood zone (point buffer).",
                       "Property is inside an unclassified flood extent. Manual FloodWise check recommended."]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("Unknown", summary.OverallRisk);
        Assert.Equal("POINT_BUFFER", summary.Source);
        Assert.True(summary.HasFloodInfo); // Key assertion: HasFloodInfo is true despite Unknown risk
        Assert.Contains("unclassified flood extent", summary.Notes);
    }

    [Fact]
    public void FromResult_WithUnknownRiskAndNoExtentIntersection_ReturnsHasFloodInfoFalse()
    {
        // Simulates geocoding failure or no data scenario
        var result = new FloodLookupResult
        {
            Address = "Unknown Address",
            Risk = FloodRisk.Unknown,
            Source = FloodDataSource.Unknown,
            Scope = FloodDataScope.Unknown,
            HasAnyExtentIntersection = false,
            Reasons = ["Geocoding failed: NotFound"]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("Unknown", summary.OverallRisk);
        Assert.False(summary.HasFloodInfo); // No extent intersection = no flood info
    }

    [Fact]
    public void FromResult_WithKnownRiskAndExtentIntersection_ReturnsHasFloodInfoTrue()
    {
        // Normal case: known risk from parcel metrics
        var result = new FloodLookupResult
        {
            Address = "118 Fernberg Road, Paddington",
            Risk = FloodRisk.Low,
            Source = FloodDataSource.BccParcelMetrics,
            Scope = FloodDataScope.Parcel,
            HasAnyExtentIntersection = true,
            Reasons = ["Risk derived from BCC parcel metrics."]
        };

        var summary = FloodSummaryMapper.FromResult(result);

        Assert.Equal("Low", summary.OverallRisk);
        Assert.True(summary.HasFloodInfo);
    }
}
