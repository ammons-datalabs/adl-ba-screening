using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Models;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

public sealed class FileFloodAnomalyStore : IFloodAnomalyStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileFloodAnomalyStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "flood-anomalies.ndjson");
    }

    public async Task AddAsync(FloodAnomalyReport report, CancellationToken cancellationToken = default)
    {
        var enriched = report with { CreatedUtc = DateTimeOffset.UtcNow };

        var line = JsonSerializer.Serialize(enriched, _jsonOptions);

        await using var stream = new FileStream(
            _path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            4096,
            useAsync: true);

        await using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    public async Task<IReadOnlyList<FloodAnomalyReport>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return Array.Empty<FloodAnomalyReport>();

        var list = new List<FloodAnomalyReport>();
        var needsRewrite = false;

        // Read all lines first, then close the file
        string[] lines;
        await using (var stream = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            4096,
            useAsync: true))
        {
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken);
            lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if the JSON contains an id field
            using var doc = JsonDocument.Parse(line);
            var hasId = doc.RootElement.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrEmpty(idProp.GetString());

            var item = JsonSerializer.Deserialize<FloodAnomalyReport>(line, _jsonOptions);
            if (item is null) continue;

            if (!hasId)
            {
                // Assign a stable ID and mark for rewrite
                item = item with { Id = Guid.NewGuid().ToString("N") };
                needsRewrite = true;
            }

            list.Add(item);
        }

        // Rewrite file with IDs if needed
        if (needsRewrite)
        {
            await RewriteFileAsync(list, cancellationToken);
        }

        return list;
    }

    private async Task RewriteFileAsync(IReadOnlyList<FloodAnomalyReport> items, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            _path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true);

        await using var writer = new StreamWriter(stream);
        foreach (var item in items)
        {
            var line = JsonSerializer.Serialize(item, _jsonOptions);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return false;

        var all = await GetAllAsync(cancellationToken);
        var filtered = all.Where(a => a.Id != id).ToList();

        if (filtered.Count == all.Count) return false;

        await RewriteFileAsync(filtered, cancellationToken);
        return true;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_path))
            File.Delete(_path);

        return Task.CompletedTask;
    }
}
