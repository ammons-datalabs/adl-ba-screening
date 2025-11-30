using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

if (args.Length < 2)
{
    Console.WriteLine("AmmonsDataLabs.BuyersAgent.Flood.DataPrep");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run -- <input_path> <output_ndjson_path> [--type extents|risk]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  input_path         Path to the input file (.geojson or .shp)");
    Console.WriteLine("  output_ndjson_path Path for the output NDJSON file");
    Console.WriteLine("  --type             Dataset type: 'extents' or 'risk' (default: risk)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -- flood-awareness-flood-risk-overall.geojson flood-risk.ndjson --type risk");
    Console.WriteLine("  dotnet run -- flood-awareness-extents.geojson flood-extents.ndjson --type extents");
    return 1;
}

var inputPath = args[0];
var outputPath = args[1];
var datasetType = args.Length > 3 && args[2] == "--type" ? args[3] : "risk";

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
    return 1;
}

var isGeoJson = inputPath.EndsWith(".geojson", StringComparison.OrdinalIgnoreCase) ||
                inputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

Console.WriteLine($"Reading {(isGeoJson ? "GeoJSON" : "Shapefile")}: {inputPath}");
Console.WriteLine($"Dataset type: {datasetType}");

IEnumerable<FloodZone> zones;

if (isGeoJson)
{
    zones = datasetType == "extents"
        ? GeoJsonFloodZoneReader.Read(inputPath, attributes =>
        {
            var id = attributes["objectid"]?.ToString() ?? "unknown";
            // Extents dataset has no risk - all polygons are "flood-affected"
            return (id, FloodRisk.Unknown);
        })
        : GeoJsonFloodZoneReader.Read(inputPath, attributes =>
        {
            var id = attributes["objectid"]?.ToString() ?? "unknown";
            var floodRisk = attributes["flood_risk"]?.ToString();
            var risk = FloodLikelihoodMapper.Map(floodRisk);
            return (id, risk);
        });
}
else
{
    zones = ShapefileFloodZoneReader.Read(inputPath, attributes =>
    {
        var id = attributes.TryGetValue("OBJECTID", out var objId) ? objId?.ToString() ?? "unknown" : "unknown";
        var likelihood = attributes.TryGetValue("LIKELIHOOD", out var raw) ? raw?.ToString() : null;
        var risk = FloodLikelihoodMapper.Map(likelihood);
        return (id, risk);
    });
}

var zoneList = zones.ToList();
Console.WriteLine($"Found {zoneList.Count} flood zones");

// Show risk distribution
var riskCounts = zoneList.GroupBy(z => z.Risk).OrderBy(g => g.Key);
foreach (var group in riskCounts)
{
    Console.WriteLine($"  {group.Key}: {group.Count()}");
}

Console.WriteLine($"Writing NDJSON: {outputPath}");

using var file = File.CreateText(outputPath);
FloodZoneNdjsonWriter.Write(zoneList, file);

Console.WriteLine("Done.");
return 0;
