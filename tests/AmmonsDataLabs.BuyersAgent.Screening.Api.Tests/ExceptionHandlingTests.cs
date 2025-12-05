using System.Net;
using System.Net.Http.Json;
using AmmonsDataLabs.BuyersAgent.Flood;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class ExceptionHandlingTests(ThrowingWebApplicationFactory factory)
    : IClassFixture<ThrowingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task WhenServiceThrows_ReturnsProblemDetails()
    {
        var payload = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = "Any Address" }]
        };

        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.NotNull(problem.Extensions["traceId"]);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class ThrowingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IFloodDataProvider));

            if (descriptor is not null) services.Remove(descriptor);

            services.AddSingleton<IFloodDataProvider, ThrowingFloodDataProvider>();
        });
    }
}

public class ThrowingFloodDataProvider : IFloodDataProvider
{
    public Task<FloodLookupResult> LookupAsync(string address, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Simulated failure for testing");
    }
}