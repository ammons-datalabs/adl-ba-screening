using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Flood data provider backed by geocoding + GIS flood zones.
/// Uses buffered proximity search to reduce false negatives from geocoding imprecision.
/// </summary>
public sealed class GisFloodDataProvider(
    IGeocodingService geocoding,
    IFloodZoneIndex zoneIndex) : IFloodDataProvider
{
    private const double DefaultBufferMetres = 30.0;

    private readonly IGeocodingService _geocoding = geocoding ?? throw new ArgumentNullException(nameof(geocoding));
    private readonly IFloodZoneIndex _zoneIndex = zoneIndex ?? throw new ArgumentNullException(nameof(zoneIndex));

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
                Proximity = FloodZoneProximity.None,
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
                Proximity = FloodZoneProximity.None,
                Reasons = [$"Geocoding failed: {geoResult.Status}"]
            };
        }

        var hit = _zoneIndex.FindNearestZone(geoResult.Location.Value, DefaultBufferMetres);

        if (hit is null)
        {
            return new FloodLookupResult
            {
                Address = geoResult.NormalizedAddress ?? address,
                Risk = FloodRisk.None,
                Proximity = FloodZoneProximity.None,
                Reasons = ["No flood zone found at this location (GIS)."]
            };
        }

        var reason = hit.Proximity == FloodZoneProximity.Inside
            ? $"Location falls inside {hit.Zone.Risk} likelihood flood zone (GIS)."
            : $"Location is {hit.DistanceMetres:F1}m from {hit.Zone.Risk} likelihood flood zone (GIS).";

        return new FloodLookupResult
        {
            Address = geoResult.NormalizedAddress ?? address,
            Risk = hit.Zone.Risk,
            Proximity = hit.Proximity,
            DistanceMetres = hit.DistanceMetres > 0 ? hit.DistanceMetres : null,
            Reasons = [reason]
        };
    }
}
