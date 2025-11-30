using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class BccFloodZoneIndex(
    IFloodZoneDataLoader loader,
    IOptions<FloodDataOptions> options)
    : IFloodZoneIndex
{
    private readonly IFloodZoneDataLoader _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly FloodDataOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly object _sync = new();
    private InMemoryFloodZoneIndex? _inner;

    public FloodZone? FindZoneForPoint(GeoPoint point)
    {
        EnsureLoaded();
        return _inner!.FindZoneForPoint(point);
    }

    private void EnsureLoaded()
    {
        if (_inner is not null)
            return;

        lock (_sync)
        {
            if (_inner is not null)
                return;

            var zones = _loader.LoadZones(_options, CancellationToken.None);
            _inner = new InMemoryFloodZoneIndex(zones);
        }
    }
}
