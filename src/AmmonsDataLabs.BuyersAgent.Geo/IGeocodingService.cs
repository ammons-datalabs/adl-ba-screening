namespace AmmonsDataLabs.BuyersAgent.Geo;

public interface IGeocodingService
{
    Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
