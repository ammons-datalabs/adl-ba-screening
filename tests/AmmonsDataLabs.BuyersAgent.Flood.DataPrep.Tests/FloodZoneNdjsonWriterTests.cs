using System.Text;
using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class FloodZoneNdjsonWriterTests
{
    [Fact]
    public void Write_WritesOneJsonPerLine()
    {
        var poly = GeoFactory.CreatePolygon(
            new GeoPoint(0, 0),
            new GeoPoint(0, 1),
            new GeoPoint(1, 1),
            new GeoPoint(1, 0));

        var zone = new FloodZone
        {
            Id = "z1",
            Risk = FloodRisk.High,
            Geometry = poly
        };

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);

        FloodZoneNdjsonWriter.Write([zone], writer);
        writer.Flush();
        ms.Position = 0;

        var text = new StreamReader(ms).ReadToEnd();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);

        var json = JsonDocument.Parse(lines[0]).RootElement;

        Assert.Equal("z1", json.GetProperty("id").GetString());
        Assert.Equal("High", json.GetProperty("risk").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("polygonWkbBase64").GetString()));
    }
}
