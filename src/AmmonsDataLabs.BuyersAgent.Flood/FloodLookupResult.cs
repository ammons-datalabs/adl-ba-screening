namespace AmmonsDataLabs.BuyersAgent.Flood;

public class FloodLookupResult
{
    public required string Address { get; init; }
    public FloodRisk Risk { get; init; }
    public FloodZoneProximity Proximity { get; init; }
    public double? DistanceMetres { get; init; }
    public string[] Reasons { get; init; } = [];

    /// <summary>
    /// The source of the flood data used for this result.
    /// </summary>
    public FloodDataSource Source { get; init; } = FloodDataSource.Unknown;

    /// <summary>
    /// The scope of the flood data used for this result.
    /// </summary>
    public FloodDataScope Scope { get; init; } = FloodDataScope.Unknown;
}