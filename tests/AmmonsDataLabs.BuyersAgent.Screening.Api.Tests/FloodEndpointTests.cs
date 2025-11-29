using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmmonsDataLabs.BuyersAgent.Flood;
using Microsoft.AspNetCore.Http;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class FloodEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Address1 = "123 Fake Street, Brisbane QLD";
    private const string Address2 = "456 Main Road, Mount Gravatt QLD";
    
    private readonly HttpClient _client = factory.CreateClient();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task FloodLookup_ReturnsResults_WithUnknownRisk()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = Address1 },
                new FloodLookupItem { Address = Address2 }
            ]
        };
        
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        FloodLookupResponse? body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Results.Count);

        FloodLookupResult firstResult = body.Results[0];
        Assert.Equal(Address1, firstResult.Address);
        Assert.Equal(FloodRisk.Unknown, firstResult.Risk);

        FloodLookupResult secondResult = body.Results[1];
        Assert.Equal(Address2, secondResult.Address);
        Assert.Equal(FloodRisk.Unknown, secondResult.Risk);
    }

    [Fact]
    public async Task FloodLookup_EmptyList_ReturnsValidationProblem()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties = []
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(_jsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("properties"));
    }
}