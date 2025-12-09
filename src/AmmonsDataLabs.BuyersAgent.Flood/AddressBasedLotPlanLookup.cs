using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Lot plan lookup implementation that uses the addresses.ndjson file.
/// Finds the nearest address point to the given coordinates and returns its lot plan.
/// </summary>
public sealed class AddressBasedLotPlanLookup : ILotPlanLookup
{
    private const double EarthRadiusMetres = 6_371_000.0;

    private readonly AddressPoint[] _addresses;
    private readonly ILogger<AddressBasedLotPlanLookup> _logger;

    public AddressBasedLotPlanLookup(
        IOptions<FloodDataOptions> options,
        ILogger<AddressBasedLotPlanLookup> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var filePath = Path.Combine(opts.DataRoot, opts.AddressesFile);

        _addresses = LoadAddresses(filePath);
        _logger.LogInformation("Loaded {Count} address points for lot plan lookup", _addresses.Length);
    }

    public string? FindLotPlan(double latitude, double longitude, double maxDistanceMetres = 40.0)
    {
        string? bestLotPlan = null;
        var bestDistance = maxDistanceMetres;

        foreach (var addr in _addresses)
        {
            var distance = HaversineDistance(latitude, longitude, addr.Latitude, addr.Longitude);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLotPlan = addr.LotPlan;
            }
        }

        if (bestLotPlan is not null)
        {
            _logger.LogInformation(
                "Found lot plan {LotPlan} at {Distance:F1}m from ({Lat}, {Lon})",
                bestLotPlan, bestDistance, latitude, longitude);
        }
        else
        {
            _logger.LogWarning(
                "No lot plan found within {MaxDistance}m of ({Lat}, {Lon}). Searched {Count} addresses.",
                maxDistanceMetres, latitude, longitude, _addresses.Length);
        }

        return bestLotPlan;
    }

    private AddressPoint[] LoadAddresses(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Address file not found: {FilePath}", filePath);
            return [];
        }

        var addresses = new List<AddressPoint>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var record = JsonSerializer.Deserialize<AddressRecord>(line, options);
                if (record?.LotPlan is not null && record.Latitude.HasValue && record.Longitude.HasValue)
                {
                    addresses.Add(new AddressPoint(
                        record.LotPlan,
                        record.Latitude.Value,
                        record.Longitude.Value));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse address line");
            }
        }

        return addresses.ToArray();
    }

    /// <summary>
    /// Calculates the Haversine distance between two points in metres.
    /// </summary>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));

        return EarthRadiusMetres * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private readonly record struct AddressPoint(string LotPlan, double Latitude, double Longitude);

    private sealed class AddressRecord
    {
        [System.Text.Json.Serialization.JsonPropertyName("lot_plan")]
        public string? LotPlan { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("latitude")]
        public double? Latitude { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("longitude")]
        public double? Longitude { get; init; }
    }
}
