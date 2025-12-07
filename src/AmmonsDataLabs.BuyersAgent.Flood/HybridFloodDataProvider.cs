using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Hybrid flood data provider that uses a tiered lookup strategy:
/// - Tier 1: Precomputed parcel metrics (lotplan -> metrics, with plan fallback). Currently BCC only.
/// - Tier 2: Runtime parcel boundary intersection with flood extents. NOT YET IMPLEMENTED.
/// Intended for councils outside BCC (e.g., Ipswich, Logan) where precomputed metrics unavailable.
/// - Tier 3: Point-buffer proximity to flood zones. Fallback when Tier 1/2 unavailable.
/// See FloodDataSource enum for detailed tier documentation.
/// </summary>
public sealed class HybridFloodDataProvider(
    IGeocodingService geocoding,
    IBccParcelMetricsIndex metricsIndex,
    IFloodZoneIndex zoneIndex)
    : IFloodDataProvider
{
    private const double DefaultBufferMetres = 30.0;

    private readonly IGeocodingService _geocoding = geocoding ?? throw new ArgumentNullException(nameof(geocoding));

    private readonly IBccParcelMetricsIndex _metricsIndex =
        metricsIndex ?? throw new ArgumentNullException(nameof(metricsIndex));

    private readonly IFloodZoneIndex _zoneIndex = zoneIndex ?? throw new ArgumentNullException(nameof(zoneIndex));

    public async Task<FloodLookupResult> LookupAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new FloodLookupResult
            {
                Address = address ?? "",
                Risk = FloodRisk.Unknown,
                Source = FloodDataSource.Unknown,
                Scope = FloodDataScope.Unknown,
                Reasons = ["Address was empty or whitespace."]
            };

        var geo = await _geocoding.GeocodeAsync(address, cancellationToken);

        if (geo.Status != GeocodingStatus.Success)
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.Unknown,
                Source = FloodDataSource.Unknown,
                Scope = FloodDataScope.Unknown,
                Reasons = [$"Geocoding failed: {geo.Status}"]
            };

        // Tier 1: BCC parcel metrics (if lotplan available)
        if (!string.IsNullOrEmpty(geo.LotPlan))
        {
            var tier1Result = TryTier1Lookup(geo);
            if (tier1Result is not null)
                return tier1Result;
        }

        // Tier 2: Runtime parcel boundary intersection (for Ipswich, Logan, etc.)
        // NOT YET IMPLEMENTED - BCC uses Tier 1 precomputed metrics instead

        // Tier 3: Point-buffer proximity
        if (geo.Location is not null) return Tier3Lookup(geo);

        return new FloodLookupResult
        {
            Address = geo.NormalizedAddress ?? address,
            Risk = FloodRisk.Unknown,
            Source = FloodDataSource.Unknown,
            Scope = FloodDataScope.Unknown,
            Reasons = ["Could not determine flood risk: no lotplan or location available."]
        };
    }

    private FloodLookupResult? TryTier1Lookup(GeocodingResult geo)
    {
        if (!_metricsIndex.TryGet(geo.LotPlan!, out var metrics))
            return null;

        var scope = metrics.Scope == MetricsScope.Parcel
            ? FloodDataScope.Parcel
            : FloodDataScope.PlanFallback;

        var reasons = new List<string>();

        if (metrics.HasFloodInfo)
        {
            var scopeDescription = scope == FloodDataScope.PlanFallback
                ? $"(plan-level fallback for {metrics.Plan})"
                : $"(parcel: {geo.LotPlan})";

            reasons.Add($"Risk derived from BCC parcel metrics {scopeDescription}.");

            if (metrics.EvidenceMetrics.Length > 0)
                reasons.Add("Source flags: " + string.Join(", ", metrics.EvidenceMetrics));

            return new FloodLookupResult
            {
                Address = geo.NormalizedAddress ?? geo.Query,
                Risk = metrics.OverallRisk,
                Proximity = FloodZoneProximity.Inside,
                DistanceMetres = null,
                Reasons = reasons.ToArray(),
                Source = FloodDataSource.BccParcelMetrics,
                Scope = scope,
                HasAnyExtentIntersection = true
            };
        }

        // Property exists in BCC data but has no flood info = confirmed no flood
        reasons.Add($"BCC parcel metrics indicate no flood risk for {geo.LotPlan}.");

        return new FloodLookupResult
        {
            Address = geo.NormalizedAddress ?? geo.Query,
            Risk = FloodRisk.None,
            Proximity = FloodZoneProximity.None,
            DistanceMetres = null,
            Reasons = reasons.ToArray(),
            Source = FloodDataSource.BccParcelMetrics,
            Scope = scope
        };
    }

    private FloodLookupResult Tier3Lookup(GeocodingResult geo)
    {
        var hit = _zoneIndex.FindNearestZone(geo.Location!.Value, DefaultBufferMetres);

        if (hit is null)
            return new FloodLookupResult
            {
                Address = geo.NormalizedAddress ?? geo.Query,
                Risk = FloodRisk.None,
                Proximity = FloodZoneProximity.None,
                Reasons = ["No flood zone found within buffer distance (point buffer)."],
                Source = FloodDataSource.PointBuffer,
                Scope = FloodDataScope.Unknown
            };

        var isInside = hit.Proximity == FloodZoneProximity.Inside;

        string reason;
        switch (isInside)
        {
            case true when hit.Zone.Risk == FloodRisk.Unknown:
                // Consolidated message for unclassified flood extents
                reason = "Property is inside an unclassified flood extent (point buffer). Manual FloodWise check recommended.";
                break;
            case true:
                reason = $"Location falls inside {hit.Zone.Risk} likelihood flood zone (point buffer).";
                break;
            default:
                reason = $"Location is {hit.DistanceMetres:F1}m from {hit.Zone.Risk} likelihood flood zone (point buffer).";
                break;
        }

        var reasons = new List<string> { reason };

        return new FloodLookupResult
        {
            Address = geo.NormalizedAddress ?? geo.Query,
            Risk = hit.Zone.Risk,
            Proximity = hit.Proximity,
            DistanceMetres = hit.DistanceMetres > 0 ? hit.DistanceMetres : null,
            Reasons = reasons.ToArray(),
            Source = FloodDataSource.PointBuffer,
            Scope = FloodDataScope.Unknown,
            HasAnyExtentIntersection = isInside
        };
    }
}