namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Snapshot of BCC flood metrics for a parcel or plan.
/// </summary>
public sealed class BccMetricsSnapshot
{
    /// <summary>
    /// The lotplan or plan key this snapshot represents.
    /// For parcel scope, this is the lotplan (e.g., "3GTP102995").
    /// For plan fallback, this may be "PLAN:GTP102995" or the queried lotplan.
    /// </summary>
    public required string LotPlanOrPlanKey { get; init; }

    /// <summary>
    /// The plan portion of the lotplan (e.g., "GTP102995").
    /// </summary>
    public required string Plan { get; init; }

    /// <summary>
    /// Overall flood risk (max of river, creek, storm tide).
    /// </summary>
    public FloodRisk OverallRisk { get; init; }

    /// <summary>
    /// River flooding risk level.
    /// </summary>
    public FloodRisk RiverRisk { get; init; }

    /// <summary>
    /// Creek/waterway flooding risk level.
    /// </summary>
    public FloodRisk CreekRisk { get; init; }

    /// <summary>
    /// Storm tide/coastal flooding risk level.
    /// </summary>
    public FloodRisk StormTideRisk { get; init; }

    /// <summary>
    /// Overland flow risk level.
    /// </summary>
    public FloodRisk OverlandFlowRisk { get; init; }

    /// <summary>
    /// Whether this parcel/plan has any flood information in BCC data.
    /// </summary>
    public bool HasFloodInfo { get; init; }

    /// <summary>
    /// The scope of this metrics snapshot.
    /// </summary>
    public MetricsScope Scope { get; init; } = MetricsScope.Unknown;

    /// <summary>
    /// Raw evidence metric flags from the BCC data.
    /// </summary>
    public string[] EvidenceMetrics { get; init; } = [];
}
