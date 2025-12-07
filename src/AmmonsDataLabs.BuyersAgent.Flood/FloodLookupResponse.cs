namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class FloodLookupResponse
{
    public required List<FloodSummary> Results { get; init; }
}