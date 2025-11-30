namespace AmmonsDataLabs.BuyersAgent.Flood.Configuration;

public sealed class FloodDataOptions
{
    public const string SectionName = "FloodData";

    public string DataRoot { get; init; } = "/data/flood";
    public string ExtentsFile { get; init; } = "bcc/flood-awareness-extents.ndjson";
    public string OverallRiskFile { get; init; } = "bcc/flood-awareness-overall.ndjson";
}
