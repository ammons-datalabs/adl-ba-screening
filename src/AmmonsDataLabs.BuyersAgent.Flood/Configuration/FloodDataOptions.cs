namespace AmmonsDataLabs.BuyersAgent.Flood.Configuration;

public sealed class FloodDataOptions
{
    public const string SectionName = "FloodData";

    public string DataRoot { get; init; } = "/data/flood";
    public string ExtentsFile { get; init; } = "bcc/flood-awareness-extents.ndjson";
    public string OverallRiskFile { get; init; } = "bcc/flood-awareness-overall.ndjson";

    /// <summary>
    /// Path (relative to DataRoot) for BCC parcel-level metrics NDJSON file.
    /// </summary>
    public string BccParcelMetricsParcelFile { get; init; } = "bcc/bcc-parcel-metrics-parcel.ndjson";

    /// <summary>
    /// Path (relative to DataRoot) for BCC plan-level metrics NDJSON file.
    /// </summary>
    public string BccParcelMetricsPlanFile { get; init; } = "bcc/bcc-parcel-metrics-plan.ndjson";
}