namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Represents the flood metrics for a BCC parcel or plan, derived from the
/// flood-awareness-property-parcel-metrics dataset.
/// </summary>
public sealed class BccParcelMetricsRecord
{
    /// <summary>
    /// The full lotplan identifier (e.g., "3GTP102995") or "PLAN:{plan}" for plan-level aggregates.
    /// </summary>
    public required string LotPlan { get; init; }

    /// <summary>
    /// The plan portion of the lotplan (e.g., "GTP102995").
    /// </summary>
    public required string Plan { get; init; }

    /// <summary>
    /// The highest risk level across all flood sources.
    /// </summary>
    public FloodRisk OverallRisk { get; init; }

    /// <summary>
    /// Risk level from Brisbane River flooding.
    /// </summary>
    public FloodRisk RiverRisk { get; init; }

    /// <summary>
    /// Risk level from creek/waterway flooding.
    /// </summary>
    public FloodRisk CreekRisk { get; init; }

    /// <summary>
    /// Risk level from storm tide flooding.
    /// </summary>
    public FloodRisk StormTideRisk { get; init; }

    /// <summary>
    /// List of metric names that contributed to this record's risk assessment.
    /// </summary>
    public string[] EvidenceMetrics { get; init; } = [];

    /// <summary>
    /// True if FLOOD_INFO metric was set to 1, indicating parcel has flood data.
    /// </summary>
    public bool HasFloodInfo { get; init; }

    /// <summary>
    /// True if OLF_FLAG metric indicates overland flow risk.
    /// </summary>
    public bool HasOverlandFlow { get; init; }

    /// <summary>
    /// 1% AEP (Annual Exceedance Probability) flood level for river, in mAHD.
    /// </summary>
    public decimal? OnePercentAepRiver { get; init; }

    /// <summary>
    /// 0.2% AEP flood level for river, in mAHD.
    /// </summary>
    public decimal? PointTwoPercentAepRiver { get; init; }

    /// <summary>
    /// Defined Flood Level (DFL) in mAHD.
    /// </summary>
    public decimal? DefinedFloodLevel { get; init; }

    /// <summary>
    /// Historic flood level 1 (typically January 2011) in mAHD.
    /// </summary>
    public decimal? HistoricFloodLevel1 { get; init; }
}