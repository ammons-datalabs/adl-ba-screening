using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Flood data provider backed by geocoding + GIS flood zones.
/// Lives in the Flood domain so it can be reused by HTTP APIs, bots, etc.
/// </summary>
public sealed class GisFloodDataProvider : IFloodDataProvider
{
    private readonly IGeocodingService _geocoding;
    private readonly IFloodZoneIndex _zoneIndex;

    public GisFloodDataProvider(
        IGeocodingService geocoding,
        IFloodZoneIndex zoneIndex)
    {
        _geocoding = geocoding ?? throw new ArgumentNullException(nameof(geocoding));
        _zoneIndex = zoneIndex ?? throw new ArgumentNullException(nameof(zoneIndex));
    }

    public async Task<FloodLookupResult> LookupAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.Unknown,
                Reasons = ["Address was empty or whitespace."]
            };
        }

        var geoResult = await _geocoding.GeocodeAsync(address, cancellationToken);

        if (geoResult.Status != GeocodingStatus.Success || geoResult.Location is null)
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.Unknown,
                Reasons = [$"Geocoding failed: {geoResult.Status}"]
            };
        }

        var zone = _zoneIndex.FindZoneForPoint(geoResult.Location.Value);

        if (zone is null)
        {
            return new FloodLookupResult
            {
                Address = geoResult.NormalizedAddress ?? address,
                Risk = FloodRisk.Unknown,
                Reasons = ["No flood zone found at this location (GIS)."]
            };
        }

        return new FloodLookupResult
        {
            Address = geoResult.NormalizedAddress ?? address,
            Risk = zone.Risk,
            Reasons = [$"Location falls inside {zone.Risk} likelihood flood zone (GIS)."]
        };
    }
}
