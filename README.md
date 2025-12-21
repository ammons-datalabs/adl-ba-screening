# adl-ba-screening

Buyer's Agent property screening API for Ammons Data Labs. Provides flood risk assessment and other property screening services for Queensland properties.


> **Status: On hold**  
> Development is paused.  
> The repository is kept for reference; issues and PRs are not being actively monitored.


[![CI](https://github.com/ammons-datalabs/adl-ba-screening/actions/workflows/ci.yml/badge.svg)](https://github.com/ammons-datalabs/adl-ba-screening/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/ammons-datalabs/adl-ba-screening/branch/main/graph/badge.svg)](https://codecov.io/gh/ammons-datalabs/adl-ba-screening)

## Projects

- **AmmonsDataLabs.BuyersAgent.Flood** - Flood screening domain (GIS index, data loading, proximity search)
- **AmmonsDataLabs.BuyersAgent.Geo** - Geocoding services (Azure Maps, file-based, stub)
- **AmmonsDataLabs.BuyersAgent.Screening.Api** - Minimal API exposing screening endpoints
- **AmmonsDataLabs.BuyersAgent.Flood.DataPrep** - Tools for processing BCC flood data
- **tests/** - Unit and integration tests

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/v1/screening/flood/lookup` | Batch flood risk lookup |

## Quickstart

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run API locally (uses file-based geocoding by default)
dotnet run --project src/AmmonsDataLabs.BuyersAgent.Screening.Api --environment Development
```

The API will be available at `http://localhost:5136`

## Docker

```bash
# Build the image
docker build -t adl-ba-screening .

# Run the container
docker run --rm -p 8080:8080 adl-ba-screening

# Test it
curl http://localhost:8080/health
```

## API Usage

### Health Check

```http
GET http://localhost:5136/health
```

### Flood Lookup

```http
POST http://localhost:5136/v1/screening/flood/lookup
Content-Type: application/json

{
  "properties": [
    { "address": "117 Fernberg Road, Paddington QLD 4064" },
    { "address": "118 Fernberg Road, Paddington QLD 4064" },
    { "address": "3/241 Horizon Drive, Westlake QLD 4074" }
  ]
}
```

**Response (200 OK):**

```json
{
  "results": [
    {
      "address": "117 Fernberg Road, Paddington QLD 4064",
      "risk": "None",
      "proximity": "None",
      "distanceMetres": null,
      "source": "BccParcelMetrics",
      "scope": "Parcel",
      "reasons": ["BCC parcel metrics indicate no flood risk for 14RP35089."]
    },
    {
      "address": "118 Fernberg Road, Paddington QLD 4064",
      "risk": "Low",
      "proximity": "Inside",
      "distanceMetres": null,
      "source": "BccParcelMetrics",
      "scope": "Parcel",
      "reasons": ["Risk derived from BCC parcel metrics (parcel: 1RP35089).", "Source flags: FLA_1PCT_AEP"]
    },
    {
      "address": "3/241 Horizon Drive, Westlake QLD 4074",
      "risk": "Medium",
      "proximity": "Inside",
      "distanceMetres": null,
      "source": "BccParcelMetrics",
      "scope": "PlanFallback",
      "reasons": ["Risk derived from BCC parcel metrics (plan-level fallback for GTP102995)."]
    }
  ]
}
```

**Error Response (400 Bad Request):**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "properties": ["At least one property is required."]
  }
}
```

## Architecture

```
FloodEndpoints
      |
      v
IFloodScreeningService
      |
      v
FloodScreeningService
      |
      +---> IGeocodingService (address -> coordinates + lotplan)
      |           |
      |           +-- AzureMapsGeocodingService (production - coordinates only)
      |           +-- FileGeocodingService (BCC parcel data - coordinates + lotplan)
      |
      +---> IFloodDataProvider (flood risk lookup)
                  |
                  +-- HybridFloodDataProvider (tiered lookup strategy)
                            |
                            +-- Tier 1: IBccParcelMetricsIndex (lotplan -> precomputed metrics)
                            +-- Tier 2: [NOT IMPLEMENTED] Parcel boundary intersection
                            +-- Tier 3: IFloodZoneIndex (point-buffer spatial queries)
```

## Tiered Flood Lookup Strategy

The `HybridFloodDataProvider` uses a tiered lookup strategy, attempting higher-accuracy
sources first before falling back to less precise methods:

### Tier 1: Precomputed Parcel Metrics (BCC)

**Highest accuracy** - equivalent to BCC FloodWise reports.

- Metrics are precomputed offline by intersecting parcel boundaries with flood extents
- Keyed by Queensland lotplan identifier (e.g., `1RP35089`)
- Falls back to plan-level aggregation when specific lot data unavailable
- Currently available for **Brisbane City Council** only
- Source data: BCC parcel boundaries GeoJSON + flood awareness parquet files

### Tier 2: Runtime Parcel Intersection (Future)

**NOT YET IMPLEMENTED** - reserved for councils outside BCC coverage.

- Would perform runtime intersection of parcel boundary polygon with flood extents
- Intended for **Ipswich, Logan**, and other SEQ councils
- Requires parcel boundary geometry at lookup time
- BCC already has full parcel boundaries in source data, but Tier 1 precomputed
  metrics make runtime intersection unnecessary for BCC coverage

### Tier 3: Point-Buffer Proximity

**Fallback** when Tier 1/2 data is unavailable.

- Uses geocoded centroid with 30m buffer for spatial query against flood zones
- Least accurate due to:
  - Geocoding imprecision (address may resolve to centroid, not actual building)
  - No parcel boundary awareness (misses cases where lot boundary intersects flood zone but centroid doesn't)
- Returns proximity status: `Inside`, `Near` (with distance), or `None`

## Flood Risk Levels

| Risk | Description |
|------|-------------|
| `Unknown` | Risk could not be determined (geocoding failed, data unavailable, etc.) |
| `None` | Location successfully checked and is not in or near any flood zone |
| `Low` | Low likelihood flood zone (1% AEP or less frequent) |
| `Medium` | Medium likelihood flood zone (2-5% AEP) |
| `High` | High likelihood flood zone (>5% AEP) |

## Proximity Status

| Proximity | Description |
|-----------|-------------|
| `Inside` | Location is within a flood zone polygon |
| `Near` | Location is within 50m buffer of a flood zone |
| `None` | Location is not near any flood zone |

## Configuration

### Geocoding Providers

Configure via `appsettings.json` or environment variables:

```json
{
  "Geocoding": {
    "Provider": "File"  // Options: "Dummy", "File", "AzureMaps"
  },
  "FileGeocoding": {
    "FilePath": "Data/addresses.json"
  },
  "AzureMaps": {
    "SubscriptionKey": "your-key-here"
  }
}
```

For Azure Maps in development, use user secrets:
```bash
dotnet user-secrets set "AzureMaps:SubscriptionKey" "your-key-here"
```

### Flood Data

```json
{
  "FloodData": {
    "DataRoot": "/path/to/flood-data",
    "ExtentsFile": "flood-risk.ndjson"
  }
}
```

## Development

### Local Development (File-based Geocoding)

The default development configuration uses file-based geocoding with pre-configured Brisbane addresses in `Data/addresses.json`. No Azure Maps API key required.

```bash
dotnet run --project src/AmmonsDataLabs.BuyersAgent.Screening.Api --environment Development
```

### Swagger UI

When running in Development mode, Swagger UI is available at:

```
http://localhost:5136/swagger
```

### HTTP Client

The project includes an `Api.http` file for testing endpoints in Rider/Visual Studio.

## Data Sources

### Brisbane City Council Flood Data

Flood zone polygons are sourced from BCC open data and processed into NDJSON format with WKB-encoded geometries. The `BccFloodZoneIndex` loads this data into an R-tree spatial index for efficient point-in-polygon queries.

### Geocoding

- **Production**: Azure Maps Search API
- **Development**: File-based lookup (`addresses.json`)
- **Testing**: Stub service returning configurable coordinates

## Known Limitations

- **BCC coverage only (Tier 1)**: Precomputed parcel metrics are currently only available for Brisbane City Council. Properties outside BCC fall back to Tier 3 point-buffer lookup.
- **Tier 3 accuracy**: Point-buffer lookup (used when Tier 1 unavailable) has known limitations:
  - Geocoding may resolve to parcel centroid rather than building location
  - Misses cases where lot boundary intersects flood zone but centroid doesn't
  - Multi-dwelling developments may have centroids outside flood zones even when lot boundaries intersect them
- **AzureMaps geocoding**: Does not return lotplan identifiers, so cannot use Tier 1 lookup. Use `FileGeocodingService` with BCC parcel data for Tier 1 accuracy.

## Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run in Release mode (CI style)
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

## Future Enhancements

- **Tier 2 implementation**: Runtime parcel intersection for Ipswich, Logan, and other SEQ councils
- **Automated data updates**: Pipeline to refresh BCC flood/parcel data as council updates source files
- **Additional screening types**: Bushfire, erosion, subsidence, etc.
- **Position assessment**: Heuristic-based scoring for property positioning

## License

MIT License - see LICENSE file for details
