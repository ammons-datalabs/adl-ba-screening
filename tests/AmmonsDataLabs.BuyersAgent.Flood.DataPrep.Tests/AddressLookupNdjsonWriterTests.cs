using System.Text.Json;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class AddressLookupNdjsonWriterTests
{
    [Fact]
    public void Write_ProducesOneJsonPerLine()
    {
        var records = new[]
        {
            new AddressLookupRecord { LotPlan = "1RP84382", Plan = "RP84382" },
            new AddressLookupRecord { LotPlan = "3GTP102995", Plan = "GTP102995" }
        };

        using var writer = new StringWriter();
        AddressLookupNdjsonWriter.Write(records, writer);

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("1RP84382", lines[0]);
        Assert.Contains("3GTP102995", lines[1]);
    }

    [Fact]
    public void Write_UsesSnakeCasePropertyNames()
    {
        var records = new[]
        {
            new AddressLookupRecord
            {
                LotPlan = "3GTP102995",
                Plan = "GTP102995",
                UnitNumber = "3",
                HouseNumber = "241",
                CorridorName = "HORIZON",
                CorridorSuffixCode = "DR",
                Suburb = "WESTLAKE"
            }
        };

        using var writer = new StringWriter();
        AddressLookupNdjsonWriter.Write(records, writer);

        var json = writer.ToString();
        Assert.Contains("\"lot_plan\":", json);
        Assert.Contains("\"unit_number\":", json);
        Assert.Contains("\"house_number\":", json);
        Assert.Contains("\"corridor_name\":", json);
        Assert.Contains("\"corridor_suffix_code\":", json);
    }

    [Fact]
    public void Write_OmitsNullFields()
    {
        var records = new[]
        {
            new AddressLookupRecord
            {
                LotPlan = "1RP84382",
                Plan = "RP84382",
                HouseNumber = "117"
                // UnitNumber is null
                // HouseNumberSuffix is null
            }
        };

        using var writer = new StringWriter();
        AddressLookupNdjsonWriter.Write(records, writer);

        var json = writer.ToString();
        Assert.DoesNotContain("\"unit_number\"", json);
        Assert.DoesNotContain("\"house_number_suffix\"", json);
        Assert.Contains("\"house_number\":\"117\"", json);
    }

    [Fact]
    public void Write_IncludesCoordinates()
    {
        var records = new[]
        {
            new AddressLookupRecord
            {
                LotPlan = "3GTP102995",
                Plan = "GTP102995",
                Latitude = -27.549067,
                Longitude = 152.911153
            }
        };

        using var writer = new StringWriter();
        AddressLookupNdjsonWriter.Write(records, writer);

        var json = writer.ToString();
        Assert.Contains("\"latitude\":", json);
        Assert.Contains("\"longitude\":", json);

        // Parse and verify values
        var doc = JsonDocument.Parse(json);
        Assert.Equal(-27.549067, doc.RootElement.GetProperty("latitude").GetDouble(), 6);
        Assert.Equal(152.911153, doc.RootElement.GetProperty("longitude").GetDouble(), 6);
    }

    [Fact]
    public void Write_CanBeReadByFileGeocodingService()
    {
        // This test verifies the output format is compatible with FileGeocodingService
        var records = new[]
        {
            new AddressLookupRecord
            {
                LotPlan = "3GTP102995",
                Plan = "GTP102995",
                UnitNumber = "3",
                HouseNumber = "241",
                CorridorName = "HORIZON",
                CorridorSuffixCode = "DR",
                Suburb = "WESTLAKE",
                Latitude = -27.549067,
                Longitude = 152.911153,
                NormalizedAddress = "3/241 Horizon Drive, Westlake"
            }
        };

        using var writer = new StringWriter();
        AddressLookupNdjsonWriter.Write(records, writer);

        var json = writer.ToString().Trim();
        var doc = JsonDocument.Parse(json);

        // These are the fields FileGeocodingService expects
        Assert.Equal("3GTP102995", doc.RootElement.GetProperty("lot_plan").GetString());
        Assert.Equal("3", doc.RootElement.GetProperty("unit_number").GetString());
        Assert.Equal("241", doc.RootElement.GetProperty("house_number").GetString());
        Assert.Equal("HORIZON", doc.RootElement.GetProperty("corridor_name").GetString());
        Assert.Equal("DR", doc.RootElement.GetProperty("corridor_suffix_code").GetString());
        Assert.Equal("WESTLAKE", doc.RootElement.GetProperty("suburb").GetString());
    }
}