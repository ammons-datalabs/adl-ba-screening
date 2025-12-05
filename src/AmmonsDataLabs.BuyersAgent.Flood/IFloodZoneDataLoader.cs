using AmmonsDataLabs.BuyersAgent.Flood.Configuration;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public interface IFloodZoneDataLoader
{
    IReadOnlyList<FloodZone> LoadZones(FloodDataOptions options, CancellationToken cancellationToken = default);
}