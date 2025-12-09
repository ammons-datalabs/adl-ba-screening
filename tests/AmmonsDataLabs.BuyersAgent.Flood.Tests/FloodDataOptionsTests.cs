using AmmonsDataLabs.BuyersAgent.Flood.Configuration;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodDataOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new FloodDataOptions();

        Assert.Equal("/data/flood", options.DataRoot);
        Assert.Equal("bcc/flood-extents.ndjson", options.ExtentsFile);
        Assert.Equal("bcc/flood-risk.ndjson", options.OverallRiskFile);
        Assert.Equal("bcc/parcel-metrics.ndjson", options.BccParcelMetricsParcelFile);
        Assert.Equal("bcc/plan-metrics.ndjson", options.BccParcelMetricsPlanFile);
        Assert.Equal("bcc/addresses.ndjson", options.AddressesFile);
    }
}