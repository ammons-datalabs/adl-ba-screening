using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class FloodDataOptionsBindingTests
{
    [Fact]
    public void BindsFromConfigurationSection()
    {
        var initialData = new Dictionary<string, string?>
        {
            ["FloodData:DataRoot"] = "../local-flood-data",
            ["FloodData:ExtentsFile"] = "custom-extents.ndjson",
            ["FloodData:OverallRiskFile"] = "custom-overall.ndjson"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData!)
            .Build();

        var services = new ServiceCollection();
        services.Configure<FloodDataOptions>(config.GetSection(FloodDataOptions.SectionName));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<FloodDataOptions>>().Value;

        Assert.Equal("../local-flood-data", options.DataRoot);
        Assert.Equal("custom-extents.ndjson", options.ExtentsFile);
        Assert.Equal("custom-overall.ndjson", options.OverallRiskFile);
    }
}