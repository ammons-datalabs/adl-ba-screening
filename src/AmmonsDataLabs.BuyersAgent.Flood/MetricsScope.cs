namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Indicates the scope of metrics data returned for a parcel lookup.
/// </summary>
public enum MetricsScope
{
    /// <summary>
    /// Scope could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Metrics are specific to the parcel (lotplan).
    /// </summary>
    Parcel,

    /// <summary>
    /// Metrics are aggregated at the plan level (fallback when parcel metrics unavailable).
    /// </summary>
    PlanFallback
}