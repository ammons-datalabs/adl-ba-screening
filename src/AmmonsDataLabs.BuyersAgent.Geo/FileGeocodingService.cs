using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class FileGeocodingService : IGeocodingService
{
    private readonly FileGeocodingOptions _options;
    private readonly Lazy<Dictionary<string, GeoPoint>> _addressLookup;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FileGeocodingService(IOptions<FileGeocodingOptions> options)
    {
        _options = options.Value;
        _addressLookup = new Lazy<Dictionary<string, GeoPoint>>(LoadAddresses);
    }

    public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address ?? string.Empty,
                Status = GeocodingStatus.Error,
                Provider = "FileGeocoding"
            });
        }

        Dictionary<string, GeoPoint> lookup;
        try
        {
            lookup = _addressLookup.Value;
        }
        catch (Exception)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.Error,
                Provider = "FileGeocoding"
            });
        }

        var normalizedQuery = address.Trim().ToUpperInvariant();

        if (lookup.TryGetValue(normalizedQuery, out var location))
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                NormalizedAddress = address.Trim(),
                Location = location,
                Status = GeocodingStatus.Success,
                Provider = "FileGeocoding"
            });
        }

        return Task.FromResult(new GeocodingResult
        {
            Query = address,
            Status = GeocodingStatus.NotFound,
            Provider = "FileGeocoding"
        });
    }

    private Dictionary<string, GeoPoint> LoadAddresses()
    {
        if (string.IsNullOrWhiteSpace(_options.FilePath) || !File.Exists(_options.FilePath))
        {
            throw new FileNotFoundException($"Geocoding file not found: {_options.FilePath}");
        }

        var json = File.ReadAllText(_options.FilePath);
        var entries = JsonSerializer.Deserialize<GeocodingEntry[]>(json, JsonOptions) ?? [];

        var lookup = new Dictionary<string, GeoPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Address)) continue;
            var key = entry.Address.Trim().ToUpperInvariant();
            lookup[key] = new GeoPoint(entry.Lat, entry.Lon);
        }

        return lookup;
    }

    [UsedImplicitly]
    private sealed record GeocodingEntry(string Address, double Lat, double Lon);
}
