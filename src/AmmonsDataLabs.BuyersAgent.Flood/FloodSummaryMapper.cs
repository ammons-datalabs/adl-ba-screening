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

        var riskLabel = ComputeRiskLabel(result);

        return new FloodSummary
        {
            Address = result.Address,
            OverallRisk = result.Risk.ToString(),
            RiskLabel = riskLabel,
            HasFloodInfo = hasFloodInfo,
            Source = FormatSource(result.Source),
            Scope = FormatScope(result.Scope),
            Notes = result.Reasons.Length > 0 ? string.Join(" ", result.Reasons) : null
        };
    }

    private static string ComputeRiskLabel(FloodLookupResult result)
    {
        // Known risk from BCC parcel metrics
        if (result.Source == FloodDataSource.BccParcelMetrics && result.Risk != FloodRisk.Unknown)
        {
            // PlanFallback uses aggregate data - may overstate risk for individual lots
            return result.Scope == FloodDataScope.PlanFallback ? $"{result.Risk}*" : result.Risk.ToString();
        }

        // Inside a flood extent but no classified risk (e.g., 5 Bellambi Place)
        return result.HasAnyExtentIntersection ? "Check manually" :
            // No flood data available
            "No mapped flood risk";
    }

    private static string FormatSource(FloodDataSource source)
    {
        return source switch
        {
            FloodDataSource.BccParcelMetrics => "BCC Parcel Metrics",
            FloodDataSource.ParcelIntersectsExtents => "Parcel Intersection",
            FloodDataSource.PointBuffer => "Point Buffer (30m)",
            _ => "Unknown"
        };
    }

    private static string FormatScope(FloodDataScope scope)
    {
        return scope switch
        {
            FloodDataScope.Parcel => "Lot-specific",
            FloodDataScope.PlanFallback => "Plan-level (aggregated)",
            _ => "Unknown"
        };
    }
}
