using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class AzureMapsGeocodingServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task GeocodeAsync_ValidAddress_ReturnsSuccessWithLocation()
    {
        var mockResponse = new AzureMapsSearchResponse
        {
            Results =
            [
                new AzureMapsSearchResult
                {
                    Position = new AzureMapsPosition { Lat = -27.4710, Lon = 153.0234 },
                    Address = new AzureMapsAddress { FreeformAddress = "1 William St, Brisbane City QLD 4000" }
                }
            ]
        };

        var service = CreateServiceWithResponse(HttpStatusCode.OK, mockResponse);

        var result = await service.GeocodeAsync("1 William St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal(-27.4710, result.Location.Value.Latitude, precision: 4);
        Assert.Equal(153.0234, result.Location.Value.Longitude, precision: 4);
        Assert.Equal("1 William St, Brisbane City QLD 4000", result.NormalizedAddress);
        Assert.Equal("AzureMaps", result.Provider);
    }

    [Fact]
    public async Task GeocodeAsync_NoResults_ReturnsNotFound()
    {
        var mockResponse = new AzureMapsSearchResponse { Results = [] };

        var service = CreateServiceWithResponse(HttpStatusCode.OK, mockResponse);

        var result = await service.GeocodeAsync("Nonexistent Address, Nowhere");

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Location);
        Assert.Equal("AzureMaps", result.Provider);
    }

    [Fact]
    public async Task GeocodeAsync_EmptyAddress_ReturnsError()
    {
        var service = CreateServiceWithResponse(HttpStatusCode.OK, new AzureMapsSearchResponse { Results = [] });

        var result = await service.GeocodeAsync("");

        Assert.Equal(GeocodingStatus.Error, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_NullAddress_ReturnsError()
    {
        var service = CreateServiceWithResponse(HttpStatusCode.OK, new AzureMapsSearchResponse { Results = [] });

        var result = await service.GeocodeAsync(null!);

        Assert.Equal(GeocodingStatus.Error, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_ApiReturnsError_ReturnsError()
    {
        var service = CreateServiceWithResponse(HttpStatusCode.InternalServerError, "Server Error");

        var result = await service.GeocodeAsync("1 William St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Error, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_ApiReturnsUnauthorized_ReturnsError()
    {
        var service = CreateServiceWithResponse(HttpStatusCode.Unauthorized, "Invalid API key");

        var result = await service.GeocodeAsync("1 William St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Error, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_HttpException_ReturnsError()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var options = Options.Create(new AzureMapsOptions { SubscriptionKey = "test-key" });
        var service = new AzureMapsGeocodingService(httpClient, options);

        var result = await service.GeocodeAsync("1 William St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Error, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var service = CreateServiceWithResponse(HttpStatusCode.OK, new AzureMapsSearchResponse { Results = [] });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GeocodeAsync("1 William St, Brisbane QLD", cts.Token));
    }

    [Fact]
    public async Task GeocodeAsync_MultipleResults_ReturnsFirstResult()
    {
        var mockResponse = new AzureMapsSearchResponse
        {
            Results =
            [
                new AzureMapsSearchResult
                {
                    Position = new AzureMapsPosition { Lat = -27.4710, Lon = 153.0234 },
                    Address = new AzureMapsAddress { FreeformAddress = "First Result" }
                },
                new AzureMapsSearchResult
                {
                    Position = new AzureMapsPosition { Lat = -27.5000, Lon = 153.0500 },
                    Address = new AzureMapsAddress { FreeformAddress = "Second Result" }
                }
            ]
        };

        var service = CreateServiceWithResponse(HttpStatusCode.OK, mockResponse);

        var result = await service.GeocodeAsync("1 William St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal(-27.4710, result.Location.Value.Latitude, precision: 4);
        Assert.Equal("First Result", result.NormalizedAddress);
    }

    private static AzureMapsGeocodingService CreateServiceWithResponse<T>(HttpStatusCode statusCode, T content)
    {
        var json = content is string s ? s : JsonSerializer.Serialize(content, JsonOptions);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var options = Options.Create(new AzureMapsOptions { SubscriptionKey = "test-key" });

        return new AzureMapsGeocodingService(httpClient, options);
    }

    // Response DTOs for mocking Azure Maps API responses
    private sealed record AzureMapsSearchResponse
    {
        public AzureMapsSearchResult[] Results { get; init; } = [];
    }

    private sealed record AzureMapsSearchResult
    {
        public AzureMapsPosition Position { get; init; } = new();
        public AzureMapsAddress Address { get; init; } = new();
    }

    private sealed record AzureMapsPosition
    {
        public double Lat { get; init; }
        public double Lon { get; init; }
    }

    private sealed record AzureMapsAddress
    {
        public string FreeformAddress { get; init; } = string.Empty;
    }
}
