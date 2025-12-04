using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Writes BccParcelMetricsRecord objects as NDJSON (newline-delimited JSON).
/// </summary>
public static class BccParcelMetricsNdjsonWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes records as NDJSON to the provided TextWriter.
    /// </summary>
    public static void Write(IEnumerable<BccParcelMetricsRecord> records, TextWriter writer)
    {
        foreach (var record in records)
        {
            var dto = new RecordDto
            {
                Lotplan = record.LotPlan,
                Plan = record.Plan,
                OverallRisk = record.OverallRisk.ToString(),
                RiverRisk = record.RiverRisk.ToString(),
                CreekRisk = record.CreekRisk.ToString(),
                StormTideRisk = record.StormTideRisk.ToString(),
                HasFloodInfo = record.HasFloodInfo,
                HasOverlandFlow = record.HasOverlandFlow,
                OnePercentAepRiver = record.OnePercentAepRiver,
                PointTwoPercentAepRiver = record.PointTwoPercentAepRiver,
                DefinedFloodLevel = record.DefinedFloodLevel,
                HistoricFloodLevel1 = record.HistoricFloodLevel1,
                EvidenceMetrics = record.EvidenceMetrics
            };

            var json = JsonSerializer.Serialize(dto, JsonOptions);
            writer.WriteLine(json);
        }
    }

    /// <summary>
    /// DTO for JSON serialization with proper naming and null handling.
    /// </summary>
    private class RecordDto
    {
        public required string Lotplan { get; init; }
        public required string Plan { get; init; }
        public required string OverallRisk { get; init; }
        public required string RiverRisk { get; init; }
        public required string CreekRisk { get; init; }
        public required string StormTideRisk { get; init; }
        public required bool HasFloodInfo { get; init; }
        public required bool HasOverlandFlow { get; init; }
        public decimal? OnePercentAepRiver { get; init; }
        public decimal? PointTwoPercentAepRiver { get; init; }
        public decimal? DefinedFloodLevel { get; init; }
        [JsonPropertyName("historic_flood_level_1")]
        public decimal? HistoricFloodLevel1 { get; init; }
        public required string[] EvidenceMetrics { get; init; }
    }
}
