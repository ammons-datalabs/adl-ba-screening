namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class FloodLookupResponse
{
    public required List<FloodLookupResult> Results { get; init; }
}