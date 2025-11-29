using AmmonsDataLabs.BuyersAgent.Flood;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any IFloodScreeningService registration
            var descriptor = services.SingleOrDefault(d => 
                d.ServiceType == typeof(IFloodScreeningService));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            
            // Add deterministic stub for testing
            services.AddSingleton<IFloodScreeningService, StubFloodScreeningService>();
        });
    }
}