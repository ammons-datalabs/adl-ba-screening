namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Provides flood risk data for a given address.
/// Implementations may range form simple demo rules to
/// full GIS/council data lookups
/// </summary>
public interface IFloodDataProvider
{
    Task<FloodLookupResult> LookupAsync(string address, CancellationToken cancellationToken = default);
}