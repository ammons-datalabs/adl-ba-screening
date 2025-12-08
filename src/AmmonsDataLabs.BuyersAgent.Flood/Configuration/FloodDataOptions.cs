namespace AmmonsDataLabs.BuyersAgent.Flood.Configuration;

public sealed class FloodDataOptions
{
    public const string SectionName = "FloodData";

    public string DataRoot { get; init; } = "/data/flood";
    public string ExtentsFile { get; init; } = "bcc/flood-extents.ndjson";
    public string OverallRiskFile { get; init; } = "bcc/flood-risk.ndjson";

    /// <summary>
    /// Path (relative to DataRoot) for BCC parcel-level metrics NDJSON file.
    /// </summary>
    public string BccParcelMetricsParcelFile { get; init; } = "bcc/parcel-metrics.ndjson";

    /// <summary>
    /// Path (relative to DataRoot) for BCC plan-level metrics NDJSON file.
    /// </summary>
    public string BccParcelMetricsPlanFile { get; init; } = "bcc/plan-metrics.ndjson";
}