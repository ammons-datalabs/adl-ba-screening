using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class FloodDataOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new FloodDataOptions();

        Assert.Equal("/data/flood", options.DataRoot);
        Assert.Equal("bcc/flood-awareness-extents.ndjson", options.ExtentsFile);
        Assert.Equal("bcc/flood-awareness-overall.ndjson", options.OverallRiskFile);
    }
}
