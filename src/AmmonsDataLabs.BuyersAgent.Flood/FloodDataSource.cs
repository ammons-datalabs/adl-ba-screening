namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Indicates the source of flood data used for a lookup result.
/// The HybridFloodDataProvider uses a tiered lookup strategy, attempting
/// higher-accuracy sources first before falling back to less precise methods:
/// - Tier 1 (BccParcelMetrics): Precomputed metrics keyed by lotplan. Highest accuracy,
/// equivalent to BCC FloodWise reports. Uses parcel boundary intersection with flood
/// extents computed offline. Currently available for Brisbane City Council.
/// - Tier 2 (ParcelIntersectsExtents): Runtime intersection of parcel boundary polygon
/// with flood extents. Intended for councils outside BCC coverage (e.g., Ipswich, Logan)
/// where precomputed metrics are not available. NOT YET IMPLEMENTED.
/// - Tier 3 (PointBuffer): Point-based proximity search using geocoded centroid with
/// a buffer distance. Fallback when Tier 1/2 data is unavailable. Least accurate due
/// to geocoding imprecision and lack of parcel boundary awareness.
/// </summary>
public enum FloodDataSource
{
    /// <summary>
    /// Source could not be determined (geocoding failed, no data available, etc.).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Tier 1: Result derived from precomputed parcel metrics.
    /// Highest accuracy - equivalent to council flood reports (e.g., BCC FloodWise).
    /// Metrics are keyed by lotplan with plan-level fallback.
    /// Currently available for Brisbane City Council.
    /// </summary>
    BccParcelMetrics,

    /// <summary>
    /// Tier 2: Result derived from runtime parcel polygon intersection with flood extents.
    /// Intended for councils outside BCC coverage (e.g., Ipswich, Logan) where
    /// precomputed metrics are not available.
    /// NOT YET IMPLEMENTED - reserved for future use.
    /// </summary>
    ParcelIntersectsExtents,

    /// <summary>
    /// Tier 3: Result derived from point-buffer proximity to flood zones.
    /// Uses geocoded centroid with 30m buffer for spatial query.
    /// Fallback when Tier 1/2 data is unavailable.
    /// Least accurate due to geocoding imprecision and lack of parcel boundary awareness.
    /// </summary>
    PointBuffer
}