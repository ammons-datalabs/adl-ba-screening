namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// A simplified, human-readable flood risk summary for a property.
/// Designed for display in web UIs and CSV exports.
/// </summary>
public sealed class FloodSummary
{
    /// <summary>
    /// The queried or normalized address.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Overall flood risk: None, Low, Medium, High, or Unknown.
    /// Used for sorting and CSS styling.
    /// </summary>
    public required string OverallRisk { get; init; }

    /// <summary>
    /// Human-friendly display label for the risk.
    /// Provides actionable guidance for cases like unclassified flood extents.
    /// </summary>
    public required string RiskLabel { get; init; }

    /// <summary>
    /// True if we found flood data for this location.
    /// False if geocoding failed or no data was available.
    /// </summary>
    public bool HasFloodInfo { get; init; }

    /// <summary>
    /// Data source used: BCC_PARCEL_METRICS, PARCEL_INTERSECTS_EXTENTS, POINT_BUFFER, or UNKNOWN.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Data scope: Parcel, PlanFallback, or Unknown.
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>
    /// Human-readable notes explaining the result.
    /// Combines reasons from the lookup result.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// True when property intersects a flood extent but has no parcel/plan metrics
    /// and no classified FPA risk. Indicates a council data gap requiring manual review.
    /// </summary>
    public bool IsDataGap { get; init; }

    /// <summary>
    /// Distance in metres to nearest flood extent, when property is nearby but outside.
    /// Null when inside an extent or no extent found within search radius.
    /// </summary>
    public double? NearbyDistanceMetres { get; init; }

    /// <summary>
    /// True if the location is outside Brisbane City Council coverage area.
    /// We only have flood data for BCC properties.
    /// </summary>
    public bool IsOutsideCoverageArea { get; init; }
}
