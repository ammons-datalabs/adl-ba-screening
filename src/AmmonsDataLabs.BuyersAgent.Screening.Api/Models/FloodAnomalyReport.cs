namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Models;

public sealed record FloodAnomalyReport
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public required string Address { get; init; }
    public string OverallRisk { get; init; } = "Unknown";
    public string Source { get; init; } = "Unknown";
    public string Scope { get; init; } = "Unknown";

    // Reasons from checkboxes
    public bool MetricsMissing { get; init; }
    public bool DisagreesWithFloodWise { get; init; }
    public bool BuildingSafeButLandWet { get; init; }
    public bool GeocodeLooksWrong { get; init; }
    public bool OtherReason { get; init; }

    public string Notes { get; init; } = "";

    // Auto-filled on server
    public DateTimeOffset CreatedUtc { get; init; }
}
