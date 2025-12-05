namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Maps FloodLookupResult to FloodSummary for human-readable output.
/// </summary>
public static class FloodSummaryMapper
{
    public static FloodSummary FromResult(FloodLookupResult result)
    {
        return new FloodSummary
        {
            Address = result.Address,
            OverallRisk = result.Risk.ToString(),
            HasFloodInfo = result.Risk != FloodRisk.Unknown && result.Source != FloodDataSource.Unknown,
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
