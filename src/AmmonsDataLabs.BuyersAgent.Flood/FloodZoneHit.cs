namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class FloodZoneHit
{
    public required FloodZone Zone { get; init; }
    public double DistanceMetres { get; init; }
    public FloodZoneProximity Proximity { get; init; }
}