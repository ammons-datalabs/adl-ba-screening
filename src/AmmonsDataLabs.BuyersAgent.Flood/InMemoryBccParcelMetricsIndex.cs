using System.Text.Json;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// In-memory implementation of IBccParcelMetricsIndex for testing.
/// Loads metrics from NDJSON string arrays.
/// </summary>
public sealed class InMemoryBccParcelMetricsIndex(
    IEnumerable<string> parcelJsonLines,
    IEnumerable<string> planJsonLines)
    : IBccParcelMetricsIndex
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, BccMetricsSnapshot> _byLotPlan = LoadFromLines(parcelJsonLines, MetricsScope.Parcel);
    private readonly IReadOnlyDictionary<string, BccMetricsSnapshot> _byPlan = LoadFromLines(planJsonLines, MetricsScope.PlanFallback);

    public bool TryGet(string lotPlan, out BccMetricsSnapshot metrics)
    {
        if (_byLotPlan.TryGetValue(lotPlan, out metrics!))
            return true;

        LotPlanParts parts;
        try
        {
            parts = LotPlanParts.Parse(lotPlan);
        }
        catch (FormatException)
        {
            metrics = default!;
            return false;
        }

        if (_byPlan.TryGetValue(parts.Plan, out var planMetrics))
        {
            // Return a copy with the queried lotplan and PlanFallback scope
            metrics = new BccMetricsSnapshot
            {
                LotPlanOrPlanKey = lotPlan,
                Plan = planMetrics.Plan,
                OverallRisk = planMetrics.OverallRisk,
                RiverRisk = planMetrics.RiverRisk,
                CreekRisk = planMetrics.CreekRisk,
                StormTideRisk = planMetrics.StormTideRisk,
                OverlandFlowRisk = planMetrics.OverlandFlowRisk,
                HasFloodInfo = planMetrics.HasFloodInfo,
                EvidenceMetrics = planMetrics.EvidenceMetrics,
                Scope = MetricsScope.PlanFallback
            };
            return true;
        }

        metrics = default!;
        return false;
    }

    private static IReadOnlyDictionary<string, BccMetricsSnapshot> LoadFromLines(
        IEnumerable<string> jsonLines,
        MetricsScope scope)
    {
        var dict = new Dictionary<string, BccMetricsSnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in jsonLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var dto = JsonSerializer.Deserialize<MetricsDto>(line, JsonOptions);
            if (dto is null) continue;

            var snapshot = new BccMetricsSnapshot
            {
                LotPlanOrPlanKey = dto.Lotplan ?? "",
                Plan = dto.Plan ?? "",
                OverallRisk = ParseRisk(dto.Overall_Risk),
                RiverRisk = ParseRisk(dto.River_Risk),
                CreekRisk = ParseRisk(dto.Creek_Risk),
                StormTideRisk = ParseRisk(dto.Storm_Tide_Risk),
                OverlandFlowRisk = dto.Has_Overland_Flow ? FloodRisk.Unknown : FloodRisk.None,
                HasFloodInfo = dto.Has_Flood_Info,
                EvidenceMetrics = dto.Evidence_Metrics ?? [],
                Scope = scope
            };

            var key = scope == MetricsScope.Parcel ? dto.Lotplan : dto.Plan;
            if (!string.IsNullOrEmpty(key))
            {
                dict[key] = snapshot;
            }
        }

        return dict;
    }

    private static FloodRisk ParseRisk(string? risk)
    {
        if (string.IsNullOrEmpty(risk)) return FloodRisk.Unknown;

        return risk.ToLowerInvariant() switch
        {
            "high" => FloodRisk.High,
            "medium" => FloodRisk.Medium,
            "low" => FloodRisk.Low,
            "none" => FloodRisk.None,
            _ => FloodRisk.Unknown
        };
    }

    private sealed class MetricsDto
    {
        public string? Lotplan { get; set; }
        public string? Plan { get; set; }
        public string? Overall_Risk { get; set; }
        public string? River_Risk { get; set; }
        public string? Creek_Risk { get; set; }
        public string? Storm_Tide_Risk { get; set; }
        public bool Has_Flood_Info { get; set; }
        public bool Has_Overland_Flow { get; set; }
        public string[]? Evidence_Metrics { get; set; }
    }
}
