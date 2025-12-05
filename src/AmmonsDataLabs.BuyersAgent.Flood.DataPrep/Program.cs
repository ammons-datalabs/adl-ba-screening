using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

if (args.Length < 1) return ShowHelp();

var command = args[0].ToLowerInvariant();

return command switch
{
    "zones" => RunZonesCommand(args[1..]),
    "metrics" => RunMetricsCommand(args[1..]),
    "addresses" => RunAddressesCommand(args[1..]),
    _ => ShowHelp()
};

static int ShowHelp()
{
    Console.WriteLine("AmmonsDataLabs.BuyersAgent.Flood.DataPrep");
    Console.WriteLine();
    Console.WriteLine("Converts BCC flood data into NDJSON format for the screening API.");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  zones      Convert flood zone GeoJSON/Shapefile to NDJSON");
    Console.WriteLine("  metrics    Convert parcel metrics Parquet to NDJSON");
    Console.WriteLine("  addresses  Extract address-to-lotplan mappings from parcel GeoJSON");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- zones <input_path> <output_ndjson_path> [--type extents|risk]");
    Console.WriteLine("  dotnet run -- metrics <parquet_path> <output_dir>");
    Console.WriteLine("  dotnet run -- addresses <parcel_geojson_path> <output_ndjson_path>");
    Console.WriteLine();
    Console.WriteLine("Full BCC workflow (from raw/ to processed/):");
    Console.WriteLine();
    Console.WriteLine("  # 1. Process flood risk zones (for GIS-based lookup)");
    Console.WriteLine("  dotnet run -- zones raw/flood-awareness-flood-risk-overall.geojson \\");
    Console.WriteLine("                       processed/flood-risk.ndjson --type risk");
    Console.WriteLine();
    Console.WriteLine("  # 2. Process flood extents (optional, for coverage analysis)");
    Console.WriteLine("  dotnet run -- zones raw/flood-awareness-extents.geojson \\");
    Console.WriteLine("                       processed/flood-extents.ndjson --type extents");
    Console.WriteLine();
    Console.WriteLine("  # 3. Process parcel metrics (for Tier 1 parcel/plan lookup)");
    Console.WriteLine("  dotnet run -- metrics raw/flood-awareness-property-parcel-metrics.parquet \\");
    Console.WriteLine("                        processed/");
    Console.WriteLine("    -> Creates: parcel-metrics.ndjson, plan-metrics.ndjson");
    Console.WriteLine();
    Console.WriteLine("  # 4. Process addresses (for geocoding/address lookup)");
    Console.WriteLine("  dotnet run -- addresses raw/property-boundaries-parcel.geojson \\");
    Console.WriteLine("                          processed/addresses.ndjson");
    Console.WriteLine();
    Console.WriteLine("Expected output structure:");
    Console.WriteLine("  data/flood/bcc/");
    Console.WriteLine("    flood-risk.ndjson       <- FloodData:OverallRiskFile");
    Console.WriteLine("    flood-extents.ndjson    <- FloodData:ExtentsFile");
    Console.WriteLine("    parcel-metrics.ndjson   <- FloodData:BccParcelMetricsParcelFile");
    Console.WriteLine("    plan-metrics.ndjson     <- FloodData:BccParcelMetricsPlanFile");
    Console.WriteLine("    addresses.ndjson        <- FileGeocoding:FilePath");
    return 1;
}

static int RunZonesCommand(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: zones command requires input_path and output_ndjson_path");
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
    else
        zones = ShapefileFloodZoneReader.Read(inputPath, attributes =>
        {
            var id = attributes.TryGetValue("OBJECTID", out var objId) ? objId?.ToString() ?? "unknown" : "unknown";
            var likelihood = attributes.TryGetValue("LIKELIHOOD", out var raw) ? raw?.ToString() : null;
            var risk = FloodLikelihoodMapper.Map(likelihood);
            return (id, risk);
        });

    var zoneList = zones.ToList();
    Console.WriteLine($"Found {zoneList.Count} flood zones");

    // Show risk distribution
    var riskCounts = zoneList.GroupBy(z => z.Risk).OrderBy(g => g.Key);
    foreach (var group in riskCounts) Console.WriteLine($"  {group.Key}: {group.Count()}");

    Console.WriteLine($"Writing NDJSON: {outputPath}");

    using var file = File.CreateText(outputPath);
    FloodZoneNdjsonWriter.Write(zoneList, file);

    Console.WriteLine("Done.");
    return 0;
}

static int RunMetricsCommand(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: metrics command requires parquet_path and output_dir");
        return 1;
    }

    var parquetPath = args[0];
    var outputDir = args[1];

    if (!File.Exists(parquetPath))
    {
        Console.Error.WriteLine($"Error: Parquet file not found: {parquetPath}");
        return 1;
    }

    Directory.CreateDirectory(outputDir);

    Console.WriteLine($"Reading Parquet: {parquetPath}");

    var result = ParquetMetricsDataPrep.Run(parquetPath);

    Console.WriteLine($"Found {result.ParcelMetrics.Count} parcel records");
    Console.WriteLine($"Found {result.PlanMetrics.Count} plan records");

    // Write parcel metrics
    var parcelPath = Path.Combine(outputDir, "parcel-metrics.ndjson");
    Console.WriteLine($"Writing parcel metrics: {parcelPath}");
    using (var parcelFile = File.CreateText(parcelPath))
    {
        BccParcelMetricsNdjsonWriter.Write(result.ParcelMetrics, parcelFile);
    }

    // Write plan metrics
    var planPath = Path.Combine(outputDir, "plan-metrics.ndjson");
    Console.WriteLine($"Writing plan metrics: {planPath}");
    using (var planFile = File.CreateText(planPath))
    {
        BccParcelMetricsNdjsonWriter.Write(result.PlanMetrics, planFile);
    }

    Console.WriteLine("Done.");
    return 0;
}

static int RunAddressesCommand(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: addresses command requires parcel_geojson_path and output_ndjson_path");
        return 1;
    }

    var geoJsonPath = args[0];
    var outputPath = args[1];

    if (!File.Exists(geoJsonPath))
    {
        Console.Error.WriteLine($"Error: GeoJSON file not found: {geoJsonPath}");
        return 1;
    }

    Console.WriteLine($"Reading parcel GeoJSON: {geoJsonPath}");

    var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

    Console.WriteLine($"Found {records.Count} parcel records");

    // Show some statistics
    var withAddress = records.Count(r => !string.IsNullOrEmpty(r.NormalizedAddress));
    var withUnit = records.Count(r => !string.IsNullOrEmpty(r.UnitNumber));
    Console.WriteLine($"  With address: {withAddress}");
    Console.WriteLine($"  With unit number: {withUnit}");

    Console.WriteLine($"Writing NDJSON: {outputPath}");

    using var file = File.CreateText(outputPath);
    AddressLookupNdjsonWriter.Write(records, file);

    Console.WriteLine("Done.");
    return 0;
}