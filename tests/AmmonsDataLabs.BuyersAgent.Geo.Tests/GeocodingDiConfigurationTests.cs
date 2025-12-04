using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class GeocodingDiConfigurationTests
{
    [Fact]
    public void AddGeocoding_DefaultProvider_RegistersDummyGeocodingService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<DummyGeocodingService>(geocodingService);
    }

    [Fact]
    public void AddGeocoding_DummyProvider_RegistersDummyGeocodingService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", "Dummy" }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<DummyGeocodingService>(geocodingService);
    }

    [Fact]
    public void AddGeocoding_FileProvider_RegistersFileGeocodingService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", "File" },
            { "FileGeocoding:FilePath", "/tmp/test.json" }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<FileGeocodingService>(geocodingService);
    }

    [Fact]
    public void AddGeocoding_AzureMapsProvider_RegistersAzureMapsGeocodingService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", "AzureMaps" },
            { "AzureMaps:SubscriptionKey", "test-key" }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<AzureMapsGeocodingService>(geocodingService);
    }

    [Theory]
    [InlineData("DUMMY")]
    [InlineData("dummy")]
    [InlineData("Dummy")]
    public void AddGeocoding_ProviderNameCaseInsensitive_Dummy(string providerName)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", providerName }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<DummyGeocodingService>(geocodingService);
    }

    [Theory]
    [InlineData("AZUREMAPS")]
    [InlineData("azuremaps")]
    [InlineData("AzureMaps")]
    public void AddGeocoding_ProviderNameCaseInsensitive_AzureMaps(string providerName)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", providerName },
            { "AzureMaps:SubscriptionKey", "test-key" }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<AzureMapsGeocodingService>(geocodingService);
    }

    [Fact]
    public void AddGeocoding_UnknownProvider_FallsBackToDummy()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            { "Geocoding:Provider", "SomeUnknownProvider" }
        });
        var services = new ServiceCollection();

        services.AddGeocoding(configuration);

        var provider = services.BuildServiceProvider();
        var geocodingService = provider.GetService<IGeocodingService>();

        Assert.NotNull(geocodingService);
        Assert.IsType<DummyGeocodingService>(geocodingService);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
