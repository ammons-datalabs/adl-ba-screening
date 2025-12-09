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

        // IsDataGap: property intersects flood extent but has no parcel/plan metrics
        // and no classified FPA risk (e.g., overland flow areas like 5 Bellambi Place)
        var isDataGap = result.HasAnyExtentIntersection &&
                        result.Source != FloodDataSource.BccParcelMetrics &&
                        result.Risk == FloodRisk.Unknown;

        // NearbyDistanceMetres: populated when property is near (but outside) a flood extent
        var nearbyDistance = result.Proximity == FloodZoneProximity.Near ? result.DistanceMetres : null;

        return new FloodSummary
        {
            Address = result.Address,
            OverallRisk = result.Risk.ToString(),
            RiskLabel = riskLabel,
            HasFloodInfo = hasFloodInfo,
            Source = FormatSource(result.Source),
            Scope = FormatScope(result.Scope),
            Notes = result.Reasons.Length > 0 ? string.Join(" ", result.Reasons) : null,
            IsDataGap = isDataGap,
            NearbyDistanceMetres = nearbyDistance,
            IsOutsideCoverageArea = result.IsOutsideCoverageArea
        };
    }

    private static string ComputeRiskLabel(FloodLookupResult result)
    {
        // Outside BCC coverage area
        if (result.IsOutsideCoverageArea)
            return "Outside BCC";

        // Known risk from BCC parcel metrics
        if (result.Source == FloodDataSource.BccParcelMetrics && result.Risk != FloodRisk.Unknown)
        {
            // PlanFallback uses aggregate data - may overstate risk for individual lots
            return result.Scope == FloodDataScope.PlanFallback ? $"{result.Risk}*" : result.Risk.ToString();
        }

        // Point Buffer with classified risk from overlay (e.g., 222 Margaret Street)
        if (result.Source == FloodDataSource.PointBuffer && result.Risk != FloodRisk.Unknown && result.Risk != FloodRisk.None)
        {
            // Mark with ^ to indicate it's from risk overlay (less reliable than parcel-level)
            return $"{result.Risk}^";
        }

        // Inside a flood extent but no classified risk (e.g., 5 Bellambi Place - overland flow)
        // This is a data gap: council has extent data but no FPA1-4 classification
        if (result.HasAnyExtentIntersection)
            return "Unclassified extent";

        // Near (but outside) a flood extent - show distance
        if (result.Proximity == FloodZoneProximity.Near && result.DistanceMetres.HasValue)
            return $"Near extent ({result.DistanceMetres.Value:F0}m)";

        // No flood extent intersection at all
        return "No mapped flood extent";
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
