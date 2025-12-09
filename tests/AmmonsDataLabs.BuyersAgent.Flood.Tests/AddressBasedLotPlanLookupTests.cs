using System.Text;
using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class AddressBasedLotPlanLookupTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger<AddressBasedLotPlanLookup> _logger;

    public AddressBasedLotPlanLookupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lot-plan-lookup-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _logger = NullLogger<AddressBasedLotPlanLookup>.Instance;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AddressBasedLotPlanLookup(null!, _logger));
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var options = CreateOptions();

        Assert.Throws<ArgumentNullException>(() =>
            new AddressBasedLotPlanLookup(options, null!));
    }

    [Fact]
    public void FindLotPlan_ReturnsNearestLotPlan_WhenWithinDistance()
    {
        CreateAddressFile(
            new AddressRecord("1SP123456", -27.470, 153.020),
            new AddressRecord("2RP654321", -27.480, 153.030));

        var lookup = CreateLookup();

        // Point very close to first address
        var result = lookup.FindLotPlan(-27.4701, 153.0201);

        Assert.Equal("1SP123456", result);
    }

    [Fact]
    public void FindLotPlan_ReturnsNull_WhenNoAddressWithinMaxDistance()
    {
        CreateAddressFile(
            new AddressRecord("1SP123456", -27.470, 153.020));

        var lookup = CreateLookup();

        // Point far from all addresses (roughly 1km away)
        var result = lookup.FindLotPlan(-27.480, 153.030, maxDistanceMetres: 40);

        Assert.Null(result);
    }

    [Fact]
    public void FindLotPlan_RespectsMaxDistanceParameter()
    {
        CreateAddressFile(
            new AddressRecord("1SP123456", -27.470, 153.020));

        var lookup = CreateLookup();

        // Point about 100m away - should fail with 40m limit
        var resultNarrow = lookup.FindLotPlan(-27.4709, 153.020, maxDistanceMetres: 40);
        Assert.Null(resultNarrow);

        // Same point with 200m limit - should succeed
        var resultWide = lookup.FindLotPlan(-27.4709, 153.020, maxDistanceMetres: 200);
        Assert.Equal("1SP123456", resultWide);
    }

    [Fact]
    public void FindLotPlan_ReturnsNull_WhenAddressFileNotFound()
    {
        var options = Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            AddressesFile = "nonexistent.ndjson"
        });

        var lookup = new AddressBasedLotPlanLookup(options, _logger);

        var result = lookup.FindLotPlan(-27.470, 153.020);

        Assert.Null(result);
    }

    [Fact]
    public void FindLotPlan_SkipsEmptyLines()
    {
        var filePath = Path.Combine(_tempDir, "addresses.ndjson");
        var sb = new StringBuilder();
        sb.AppendLine(); // Empty line
        sb.AppendLine(JsonSerializer.Serialize(new { lot_plan = "1SP123456", latitude = -27.470, longitude = 153.020 }));
        sb.AppendLine("   "); // Whitespace line
        File.WriteAllText(filePath, sb.ToString());

        var lookup = CreateLookup();

        var result = lookup.FindLotPlan(-27.4701, 153.0201);

        Assert.Equal("1SP123456", result);
    }

    [Fact]
    public void FindLotPlan_SkipsRecordsWithMissingFields()
    {
        var filePath = Path.Combine(_tempDir, "addresses.ndjson");
        var sb = new StringBuilder();
        // Missing lot_plan
        sb.AppendLine(JsonSerializer.Serialize(new { latitude = -27.470, longitude = 153.020 }));
        // Missing latitude
        sb.AppendLine(JsonSerializer.Serialize(new { lot_plan = "2RP111111", longitude = 153.020 }));
        // Missing longitude
        sb.AppendLine(JsonSerializer.Serialize(new { lot_plan = "3RP222222", latitude = -27.470 }));
        // Valid record
        sb.AppendLine(JsonSerializer.Serialize(new { lot_plan = "4SP333333", latitude = -27.480, longitude = 153.030 }));
        File.WriteAllText(filePath, sb.ToString());

        var lookup = CreateLookup();

        // Should only find the valid record
        var result = lookup.FindLotPlan(-27.4801, 153.0301);

        Assert.Equal("4SP333333", result);
    }

    [Fact]
    public void FindLotPlan_HandlesInvalidJson_WithoutThrowing()
    {
        var filePath = Path.Combine(_tempDir, "addresses.ndjson");
        var sb = new StringBuilder();
        sb.AppendLine("this is not valid json");
        sb.AppendLine(JsonSerializer.Serialize(new { lot_plan = "1SP123456", latitude = -27.470, longitude = 153.020 }));
        sb.AppendLine("{ broken json");
        File.WriteAllText(filePath, sb.ToString());

        var lookup = CreateLookup();

        // Should still find the valid record
        var result = lookup.FindLotPlan(-27.4701, 153.0201);

        Assert.Equal("1SP123456", result);
    }

    [Fact]
    public void FindLotPlan_SelectsClosestAddress_WhenMultipleWithinRange()
    {
        CreateAddressFile(
            new AddressRecord("FAR_AWAY", -27.4705, 153.0205),   // ~50m away
            new AddressRecord("CLOSEST", -27.4700, 153.0200),    // ~0m away
            new AddressRecord("MEDIUM", -27.4702, 153.0202));    // ~25m away

        var lookup = CreateLookup();

        var result = lookup.FindLotPlan(-27.4700, 153.0200, maxDistanceMetres: 100);

        Assert.Equal("CLOSEST", result);
    }

    private IOptions<FloodDataOptions> CreateOptions()
    {
        return Options.Create(new FloodDataOptions
        {
            DataRoot = _tempDir,
            AddressesFile = "addresses.ndjson"
        });
    }

    private AddressBasedLotPlanLookup CreateLookup()
    {
        return new AddressBasedLotPlanLookup(CreateOptions(), _logger);
    }

    private void CreateAddressFile(params AddressRecord[] addresses)
    {
        var filePath = Path.Combine(_tempDir, "addresses.ndjson");
        var sb = new StringBuilder();
        foreach (var addr in addresses)
        {
            sb.AppendLine(JsonSerializer.Serialize(new
            {
                lot_plan = addr.LotPlan,
                latitude = addr.Latitude,
                longitude = addr.Longitude
            }));
        }
        File.WriteAllText(filePath, sb.ToString());
    }

    private sealed record AddressRecord(string LotPlan, double Latitude, double Longitude);
}
