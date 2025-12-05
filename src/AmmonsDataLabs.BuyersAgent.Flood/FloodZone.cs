using NetTopologySuite.Geometries;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class FloodZone
{
    public required string Id { get; init; }
    public required Geometry Geometry { get; init; }
    public required FloodRisk Risk { get; init; }
}