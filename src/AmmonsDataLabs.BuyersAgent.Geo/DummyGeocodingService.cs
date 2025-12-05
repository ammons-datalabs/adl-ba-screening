namespace AmmonsDataLabs.BuyersAgent.Geo;

/// <summary>
/// A dummy geocoding service that returns a fixed location for any address.
/// Use only for development and testing until a real geocoding provider is integrated.
/// </summary>
public sealed class DummyGeocodingService : IGeocodingService
{
    // Default to Brisbane CBD coordinates
    private static readonly GeoPoint DefaultLocation = new(-27.4705, 153.0260);

    public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Task.FromResult(new GeocodingResult
            {
                Query = address ?? string.Empty,
                Status = GeocodingStatus.Error,
                Provider = nameof(DummyGeocodingService)
            });

        return Task.FromResult(new GeocodingResult
        {
            Query = address,
            NormalizedAddress = address.Trim(),
            Location = DefaultLocation,
            Status = GeocodingStatus.Success,
            Provider = nameof(DummyGeocodingService)
        });
    }
}