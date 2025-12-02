namespace AmmonsDataLabs.BuyersAgent.Flood;

public class FloodLookupResult
{
    public required string Address { get; init; }
    public FloodRisk Risk { get; init; }
    public FloodZoneProximity Proximity { get; init; }
    public double? DistanceMetres { get; init; }
    public string[] Reasons { get; init; } = [];
}