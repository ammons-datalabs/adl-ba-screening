using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class AzureMapsGeocodingService(HttpClient httpClient, IOptions<AzureMapsOptions> options)
    : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly AzureMapsOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new GeocodingResult
            {
                Query = address ?? string.Empty,
                Status = GeocodingStatus.Error,
                Provider = "AzureMaps"
            };

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url =
                $"{_options.BaseUrl}/search/address/json?api-version=1.0&subscription-key={_options.SubscriptionKey}&query={encodedAddress}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new GeocodingResult
                {
                    Query = address,
                    Status = GeocodingStatus.Error,
                    Provider = "AzureMaps"
                };

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<AzureMapsSearchResponse>(json, JsonOptions);

            if (searchResponse?.Results is null || searchResponse.Results.Length == 0)
                return new GeocodingResult
                {
                    Query = address,
                    Status = GeocodingStatus.NotFound,
                    Provider = "AzureMaps"
                };

            var firstResult = searchResponse.Results[0];

            return new GeocodingResult
            {
                Query = address,
                NormalizedAddress = firstResult.Address?.FreeformAddress,
                Location = new GeoPoint(firstResult.Position.Lat, firstResult.Position.Lon),
                Status = GeocodingStatus.Success,
                Provider = "AzureMaps"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.Error,
                Provider = "AzureMaps"
            };
        }
    }

    // Response DTOs for Azure Maps Search API
    private sealed record AzureMapsSearchResponse
    {
        public AzureMapsSearchResult[] Results { get; init; } = [];
    }

    [UsedImplicitly]
    private sealed record AzureMapsSearchResult
    {
        public AzureMapsPosition Position { get; init; } = new();
        public AzureMapsAddress? Address { get; init; }
    }

    private sealed record AzureMapsPosition
    {
        public double Lat { get; init; }
        public double Lon { get; init; }
    }

    [UsedImplicitly]
    private sealed record AzureMapsAddress
    {
        public string FreeformAddress { get; init; } = string.Empty;
    }
}