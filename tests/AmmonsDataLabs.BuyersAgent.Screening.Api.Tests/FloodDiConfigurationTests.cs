using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Geo;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class FloodDiConfigurationTests
{
    private static WebApplicationFactory<Program> CreateFactory(bool useGisProvider)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Flood:UseGisProvider", useGisProvider ? "true" : "false");
            });
    }

    [Fact]
    public void WhenUseGisProviderTrue_RegistersGisTypes()
    {
        using var factory = CreateFactory(useGisProvider: true);
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.IsType<GisFloodDataProvider>(sp.GetRequiredService<IFloodDataProvider>());
        Assert.NotNull(sp.GetService<IFloodZoneIndex>());
        Assert.NotNull(sp.GetService<IGeocodingService>());
    }

    [Fact]
    public void WhenUseGisProviderFalse_RegistersSimpleProvider()
    {
        using var factory = CreateFactory(useGisProvider: false);
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.IsType<SimpleFloodDataProvider>(sp.GetRequiredService<IFloodDataProvider>());
    }
}
