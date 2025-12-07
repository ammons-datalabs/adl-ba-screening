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
}
