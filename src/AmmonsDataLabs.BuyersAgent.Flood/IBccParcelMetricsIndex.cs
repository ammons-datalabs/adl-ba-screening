namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Provides lookup of BCC parcel metrics by lotplan.
/// </summary>
public interface IBccParcelMetricsIndex
{
    /// <summary>
    /// Attempts to get metrics for the specified lotplan.
    /// If parcel-level metrics are not available, falls back to plan-level metrics.
    /// </summary>
    /// <param name="lotPlan">The lotplan to look up (e.g., "3GTP102995").</param>
    /// <param name="metrics">The metrics snapshot if found.</param>
    /// <returns>True if metrics were found (at parcel or plan level), false otherwise.</returns>
    bool TryGet(string lotPlan, out BccMetricsSnapshot metrics);
}
