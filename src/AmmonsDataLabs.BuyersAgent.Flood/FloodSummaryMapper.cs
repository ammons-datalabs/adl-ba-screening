namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Maps FloodLookupResult to FloodSummary for human-readable output.
/// </summary>
public static class FloodSummaryMapper
{
    public static FloodSummary FromResult(FloodLookupResult result)
    {
        // HasFloodInfo is true if:
        // - We have a known risk from a known source, OR
        // - The property intersects any flood extent (even if risk is Unknown)
        var hasFloodInfo =
            (result.Risk != FloodRisk.Unknown && result.Source != FloodDataSource.Unknown) ||
            result.HasAnyExtentIntersection;

        return new FloodSummary
        {
            Address = result.Address,
            OverallRisk = result.Risk.ToString(),
            HasFloodInfo = hasFloodInfo,
            Source = FormatSource(result.Source),
            Scope = result.Scope.ToString(),
            Notes = result.Reasons.Length > 0 ? string.Join(" ", result.Reasons) : null
        };
    }

    private static string FormatSource(FloodDataSource source)
    {
        return source switch
        {
            FloodDataSource.BccParcelMetrics => "BCC_PARCEL_METRICS",
            FloodDataSource.ParcelIntersectsExtents => "PARCEL_INTERSECTS_EXTENTS",
            FloodDataSource.PointBuffer => "POINT_BUFFER",
            _ => "UNKNOWN"
        };
    }
}
