using AmmonsDataLabs.BuyersAgent.Flood;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
[UsedImplicitly]
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any IFloodScreeningService registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IFloodDataProvider));

            if (descriptor is not null) services.Remove(descriptor);

            // Add deterministic stub for testing
            services.AddSingleton<IFloodDataProvider, StubFloodDataProvider>();
        });
    }
}