namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Accumulates flood metrics for a single lot/parcel and produces a BccParcelMetricsRecord.
/// Handles the long-format metric rows from the BCC parquet file.
/// </summary>
internal sealed class LotAccumulator
{
    /// <summary>
    /// Maps BCC metric names to flood source and risk level.
    /// Based on BCC Flood Awareness Property Parcel Metrics reference.
    /// </summary>
    private static readonly Dictionary<string, (FloodSource Source, FloodRisk Risk)>
        RiskMetricMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // River risk indicators
            ["FL_VLOW_RIVER"] = (FloodSource.River, FloodRisk.Low), // Very Low -> Low
            ["FL_LOW_RIVER"] = (FloodSource.River, FloodRisk.Low),
            ["FL_MED_RIVER"] = (FloodSource.River, FloodRisk.Medium),
            ["FL_HIGH_RIVER"] = (FloodSource.River, FloodRisk.High),

            // Creek/waterway risk indicators
            ["FL_VLOW_CREEK"] = (FloodSource.Creek, FloodRisk.Low), // Very Low -> Low
            ["FL_LOW_CREEK"] = (FloodSource.Creek, FloodRisk.Low),
            ["FL_MED_CREEK"] = (FloodSource.Creek, FloodRisk.Medium),
            ["FL_HIGH_CREEK"] = (FloodSource.Creek, FloodRisk.High),

            // Storm tide risk indicators
            ["FL_VLOW_ST"] = (FloodSource.StormTide, FloodRisk.Low), // Very Low -> Low
            ["FL_LOW_ST"] = (FloodSource.StormTide, FloodRisk.Low),
            ["FL_MED_ST"] = (FloodSource.StormTide, FloodRisk.Medium),
            ["FL_HIGH_ST"] = (FloodSource.StormTide, FloodRisk.High)
        };

    private readonly HashSet<string> _evidence = new(StringComparer.OrdinalIgnoreCase);
    private FloodRisk _creek = FloodRisk.Unknown;
    private decimal? _definedFloodLevel;
    private bool _hasFloodInfo;
    private bool _hasOverlandFlow;
    private decimal? _historicFloodLevel1;

    private decimal? _onePercentAepRiver;
    private decimal? _pointTwoPercentAepRiver;
    private FloodRisk _river = FloodRisk.Unknown;
    private FloodRisk _stormTide = FloodRisk.Unknown;

    /// <summary>
    /// Applies a single metric row to this accumulator.
    /// </summary>
    /// <param name="metric">The metric name (e.g., "FL_HIGH_RIVER")</param>
    /// <param name="value">The metric value (typically "1" for flags, or a decimal string for levels)</param>
    public void ApplyMetric(string metric, string? value)
    {
        if (string.IsNullOrWhiteSpace(metric))
            return;

        // Handle FLOOD_INFO flag
        if (metric.Equals("FLOOD_INFO", StringComparison.OrdinalIgnoreCase))
        {
            if (value == "1")
            {
                _hasFloodInfo = true;
                _evidence.Add(metric);
            }

            return;
        }

        // Handle OLF_FLAG (Overland Flow)
        if (metric.Equals("OLF_FLAG", StringComparison.OrdinalIgnoreCase))
        {
            if (value == "1")
            {
                _hasOverlandFlow = true;
                _evidence.Add(metric);
            }

            return;
        }

        // Handle numeric level metrics
        if (TryParseDecimalMetric(metric, value, "01AEP_RIVER", ref _onePercentAepRiver))
            return;
        if (TryParseDecimalMetric(metric, value, "002AEP_RIVER", ref _pointTwoPercentAepRiver))
            return;
        if (TryParseDecimalMetric(metric, value, "FL_DFL", ref _definedFloodLevel))
            return;
        if (TryParseDecimalMetric(metric, value, "FL_HIS1_RIVER", ref _historicFloodLevel1))
            return;

        // Handle risk flag metrics
        if (!RiskMetricMap.TryGetValue(metric, out var info))
            return;

        // Only process non-empty, non-zero values
        if (string.IsNullOrWhiteSpace(value) || value == "0")
            return;

        _evidence.Add(metric);

        switch (info.Source)
        {
            case FloodSource.River:
                _river = Max(_river, info.Risk);
                break;
            case FloodSource.Creek:
                _creek = Max(_creek, info.Risk);
                break;
            case FloodSource.StormTide:
                _stormTide = Max(_stormTide, info.Risk);
                break;
        }
    }

    /// <summary>
    /// Produces the final record for this lot/parcel.
    /// </summary>
    public BccParcelMetricsRecord ToRecord(string lotPlan, string plan)
    {
        var overall = Max(Max(_river, _creek), _stormTide);

        return new BccParcelMetricsRecord
        {
            LotPlan = lotPlan,
            Plan = plan,
            RiverRisk = _river,
            CreekRisk = _creek,
            StormTideRisk = _stormTide,
            OverallRisk = overall,
            EvidenceMetrics = _evidence.ToArray(),
            HasFloodInfo = _hasFloodInfo,
            HasOverlandFlow = _hasOverlandFlow,
            OnePercentAepRiver = _onePercentAepRiver,
            PointTwoPercentAepRiver = _pointTwoPercentAepRiver,
            DefinedFloodLevel = _definedFloodLevel,
            HistoricFloodLevel1 = _historicFloodLevel1
        };
    }

    private bool TryParseDecimalMetric(string metric, string? value, string targetMetric, ref decimal? target)
    {
        if (!metric.Equals(targetMetric, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, out var parsed))
        {
            target = parsed;
            _evidence.Add(metric);
        }

        return true;
    }

    private static FloodRisk Max(FloodRisk a, FloodRisk b)
    {
        return (FloodRisk)Math.Max((int)a, (int)b);
    }
}

/// <summary>
/// Flood source categories used in BCC flood data.
/// </summary>
internal enum FloodSource
{
    Unknown = 0,
    River,
    Creek,
    StormTide
}