using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Writes AddressLookupRecord objects as NDJSON (newline-delimited JSON).
/// </summary>
public static class AddressLookupNdjsonWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes records as NDJSON to the provided TextWriter.
    /// </summary>
    public static void Write(IEnumerable<AddressLookupRecord> records, TextWriter writer)
    {
        foreach (var record in records)
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            writer.WriteLine(json);
        }
    }
}
