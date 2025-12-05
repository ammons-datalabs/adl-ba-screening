using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmmonsDataLabs.BuyersAgent.Flood;
using Microsoft.AspNetCore.Http;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class FloodEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string StreetAddress = "123 Fake Street, Brisbane QLD";
    private const string MainRoadAddress = "456 Main Road, Mount Gravatt QLD";
    private const string UnknownAddress = "789 Unknown Avenue, Sydney NSW";

    private readonly HttpClient _client = factory.CreateClient();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task FloodLookup_StreetAddress_ReturnsLowRisk()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = StreetAddress }]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Results);

        var result = body.Results[0];
        Assert.Equal(StreetAddress, result.Address);
        Assert.Equal(FloodRisk.Low, result.Risk);
        Assert.NotNull(result.Reasons);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task FloodLookup_MainRoadAddress_ReturnsHighRisk()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = MainRoadAddress }]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Results);

        var result = body.Results[0];
        Assert.Equal(MainRoadAddress, result.Address);
        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.NotNull(result.Reasons);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task FloodLookup_UnknownAddress_ReturnsUnknownRisk()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = UnknownAddress }]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Results);

        var result = body.Results[0];
        Assert.Equal(UnknownAddress, result.Address);
        Assert.Equal(FloodRisk.Unknown, result.Risk);
    }

    [Fact]
    public async Task FloodLookup_MultipleAddresses_ReturnsMappedResults()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = StreetAddress },
                new FloodLookupItem { Address = MainRoadAddress },
                new FloodLookupItem { Address = UnknownAddress }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(3, body.Results.Count);

        Assert.Equal(FloodRisk.Low, body.Results[0].Risk);
        Assert.Equal(FloodRisk.High, body.Results[1].Risk);
        Assert.Equal(FloodRisk.Unknown, body.Results[2].Risk);
    }

    [Fact]
    public async Task FloodLookup_ResponseHasNonNullReasons()
    {
        // Arrange
        var payload = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = StreetAddress },
                new FloodLookupItem { Address = MainRoadAddress }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FloodLookupResponse>(_jsonOptions);
        Assert.NotNull(body);

        Assert.All(body.Results, result =>
        {
            Assert.NotNull(result.Address);
            Assert.NotEmpty(result.Address);
            Assert.NotNull(result.Reasons);
        });
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
        var response = await _client.PostAsJsonAsync("/v1/screening/flood/lookup", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(_jsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("properties"));
    }

    [Fact]
    public async Task FloodSummary_ValidAddress_ReturnsSummary()
    {
        // Act
        var response = await _client.GetAsync($"/v1/screening/flood/summary?address={Uri.EscapeDataString(StreetAddress)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<FloodSummary>(_jsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(StreetAddress, summary.Address);
        Assert.Equal("Low", summary.OverallRisk);
        Assert.True(summary.HasFloodInfo);
    }

    [Fact]
    public async Task FloodSummary_HighRiskAddress_ReturnsHighRisk()
    {
        // Act
        var response = await _client.GetAsync($"/v1/screening/flood/summary?address={Uri.EscapeDataString(MainRoadAddress)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<FloodSummary>(_jsonOptions);
        Assert.NotNull(summary);
        Assert.Equal("High", summary.OverallRisk);
        Assert.True(summary.HasFloodInfo);
    }

    [Fact]
    public async Task FloodSummary_UnknownAddress_ReturnsUnknownWithNoFloodInfo()
    {
        // Act
        var response = await _client.GetAsync($"/v1/screening/flood/summary?address={Uri.EscapeDataString(UnknownAddress)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<FloodSummary>(_jsonOptions);
        Assert.NotNull(summary);
        Assert.Equal("Unknown", summary.OverallRisk);
        Assert.False(summary.HasFloodInfo);
    }

    [Fact]
    public async Task FloodSummary_MissingAddress_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/v1/screening/flood/summary");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FloodSummary_EmptyAddress_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/v1/screening/flood/summary?address=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}