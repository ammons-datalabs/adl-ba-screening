using Parquet;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Reads BCC flood-awareness-property-parcel-metrics Parquet file and aggregates metrics
/// at both parcel (lotplan) and plan level.
/// </summary>
public static class ParquetMetricsDataPrep
{
    /// <summary>
    /// Result containing both parcel-level and plan-level aggregated metrics.
    /// </summary>
    public sealed class Result
    {
        public required IReadOnlyList<BccParcelMetricsRecord> ParcelMetrics { get; init; }
        public required IReadOnlyList<BccParcelMetricsRecord> PlanMetrics { get; init; }
    }

    /// <summary>
    /// Processes a BCC parcel metrics parquet file and produces aggregated metrics.
    /// </summary>
    /// <param name="parquetPath">Path to the parquet file</param>
    /// <returns>Result containing parcel and plan-level metrics</returns>
    public static Result Run(string parquetPath)
    {
        var byLotPlan = new Dictionary<string, LotAccumulator>(StringComparer.OrdinalIgnoreCase);
        var byPlan = new Dictionary<string, LotAccumulator>(StringComparer.OrdinalIgnoreCase);

        using var fs = File.OpenRead(parquetPath);
        using var reader = ParquetReader.CreateAsync(fs).GetAwaiter().GetResult();

        // Find the column indices
        var schema = reader.Schema;
        var lotplanField = schema.DataFields.First(f => f.Name == "lotplan");
        var metricField = schema.DataFields.First(f => f.Name == "metric");
        var valueField = schema.DataFields.First(f => f.Name == "value");

        for (int rg = 0; rg < reader.RowGroupCount; rg++)
        {
            using var rgReader = reader.OpenRowGroupReader(rg);

            var lotplanColumn = rgReader.ReadColumnAsync(lotplanField).GetAwaiter().GetResult();
            var metricColumn = rgReader.ReadColumnAsync(metricField).GetAwaiter().GetResult();
            var valueColumn = rgReader.ReadColumnAsync(valueField).GetAwaiter().GetResult();

            var lotplans = (string?[])lotplanColumn.Data;
            var metrics = (string?[])metricColumn.Data;
            var values = (string?[])valueColumn.Data;

            for (int i = 0; i < lotplans.Length; i++)
            {
                var lotplan = lotplans[i];
                var metric = metrics[i];
                var value = values[i];

                if (string.IsNullOrWhiteSpace(lotplan) || string.IsNullOrWhiteSpace(metric))
                    continue;

                LotPlanParts parts;
                try
                {
                    parts = LotPlanParts.Parse(lotplan);
                }
                catch (FormatException)
                {
                    // Skip invalid lotplan formats
                    continue;
                }

                // Accumulate at parcel level
                if (!byLotPlan.TryGetValue(lotplan, out var accLot))
                    byLotPlan[lotplan] = accLot = new LotAccumulator();
                accLot.ApplyMetric(metric, value);

                // Accumulate at plan level
                if (!byPlan.TryGetValue(parts.Plan, out var accPlan))
                    byPlan[parts.Plan] = accPlan = new LotAccumulator();
                accPlan.ApplyMetric(metric, value);
            }
        }

        var parcelMetrics = byLotPlan
            .Select(kvp =>
            {
                var parts = LotPlanParts.Parse(kvp.Key);
                return kvp.Value.ToRecord(kvp.Key, parts.Plan);
            })
            .ToList();

        var planMetrics = byPlan
            .Select(kvp => kvp.Value.ToRecord(lotPlan: $"PLAN:{kvp.Key}", plan: kvp.Key))
            .ToList();

        return new Result
        {
            ParcelMetrics = parcelMetrics,
            PlanMetrics = planMetrics
        };
    }
}
