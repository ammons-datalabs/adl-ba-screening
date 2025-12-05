using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// File-based implementation of IBccParcelMetricsIndex.
/// Loads parcel and plan metrics from NDJSON files.
/// Uses lazy loading with double-check locking.
/// </summary>
public sealed class NdjsonBccParcelMetricsIndex(IOptions<FloodDataOptions> options) : IBccParcelMetricsIndex
{
    private readonly FloodDataOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly object _sync = new();
    private InMemoryBccParcelMetricsIndex? _inner;

    public bool TryGet(string lotPlan, out BccMetricsSnapshot metrics)
    {
        EnsureLoaded();
        return _inner!.TryGet(lotPlan, out metrics);
    }

    private void EnsureLoaded()
    {
        if (_inner is not null)
            return;

        lock (_sync)
        {
            if (_inner is not null)
                return;

            var parcelPath = Path.Combine(_options.DataRoot, _options.BccParcelMetricsParcelFile);
            var planPath = Path.Combine(_options.DataRoot, _options.BccParcelMetricsPlanFile);

            var parcelLines = File.Exists(parcelPath)
                ? File.ReadLines(parcelPath)
                : [];

            var planLines = File.Exists(planPath)
                ? File.ReadLines(planPath)
                : [];

            _inner = new InMemoryBccParcelMetricsIndex(parcelLines, planLines);
        }
    }
}