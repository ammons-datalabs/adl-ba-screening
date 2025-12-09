using AmmonsDataLabs.BuyersAgent.Flood.Configuration;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public interface IFloodZoneDataLoader
{
    IReadOnlyList<FloodZone> LoadZones(FloodDataOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the classified flood risk zones from the overall risk file.
    /// These are smaller polygons with specific risk classifications (Low/Medium/High).
    /// </summary>
    IReadOnlyList<FloodZone> LoadRiskZones(FloodDataOptions options, CancellationToken cancellationToken = default);
}