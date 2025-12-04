using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Geo;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds geocoding services to the service collection based on configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGeocoding(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeocodingOptions>(configuration.GetSection(GeocodingOptions.SectionName));

        var provider = configuration.GetValue<string>($"{GeocodingOptions.SectionName}:Provider") ?? "Dummy";

        switch (provider.ToUpperInvariant())
        {
            case "AZUREMAPS":
                services.Configure<AzureMapsOptions>(configuration.GetSection(AzureMapsOptions.SectionName));
                services.AddHttpClient<IGeocodingService, AzureMapsGeocodingService>();
                break;

            case "FILE":
                services.Configure<FileGeocodingOptions>(configuration.GetSection(FileGeocodingOptions.SectionName));
                services.AddSingleton<IGeocodingService, FileGeocodingService>();
                break;

            // case "DUMMY":
            default:
                services.AddSingleton<IGeocodingService, DummyGeocodingService>();
                break;
        }

        return services;
    }
}
