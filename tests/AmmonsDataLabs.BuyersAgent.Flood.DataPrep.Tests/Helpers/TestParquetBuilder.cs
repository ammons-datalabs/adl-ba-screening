using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;

/// <summary>
/// Helper to create synthetic Parquet files for testing.
/// </summary>
internal static class TestParquetBuilder
{
    /// <summary>
    /// Creates a test parquet file with the BCC flood metrics schema.
    /// </summary>
    public static void CreateTestMetricsFile(string path, params (string lotplan, string metric, string value)[] rows)
    {
        var schema = new ParquetSchema(
            new DataField<string>("objectid"),
            new DataField<string>("lotplan"),
            new DataField<string>("metric"),
            new DataField<string>("value"));

        var objectids = rows.Select((_, i) => i.ToString()).ToArray();
        var lotplans = rows.Select(r => r.lotplan).ToArray();
        var metrics = rows.Select(r => r.metric).ToArray();
        var values = rows.Select(r => r.value).ToArray();

        using var fs = File.Create(path);
        using var writer = ParquetWriter.CreateAsync(schema, fs).GetAwaiter().GetResult();
        using var rgWriter = writer.CreateRowGroup();

        rgWriter.WriteColumnAsync(new DataColumn(schema.DataFields[0], objectids)).GetAwaiter().GetResult();
        rgWriter.WriteColumnAsync(new DataColumn(schema.DataFields[1], lotplans)).GetAwaiter().GetResult();
        rgWriter.WriteColumnAsync(new DataColumn(schema.DataFields[2], metrics)).GetAwaiter().GetResult();
        rgWriter.WriteColumnAsync(new DataColumn(schema.DataFields[3], values)).GetAwaiter().GetResult();
    }
}