namespace AmmonsDataLabs.BuyersAgent.Flood;

public class FloodLookupResult
{
    public required string Address { get; init; }
    public FloodRisk Risk { get; init; }
    public string[] Reasons { get; init; } = [];
}