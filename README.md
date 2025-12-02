# adl-ba-screening

Buyer's Agent property screening API for Ammons Data Labs. Provides flood risk assessment and other property screening services for Queensland properties.

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
      "reasons": ["No flood zone found at this location (GIS)."]
    },
    {
      "address": "118 Fernberg Road, Paddington QLD 4064",
      "risk": "Low",
      "proximity": "Near",
      "distanceMetres": 2.49,
      "reasons": ["Location is 2.5m from Low likelihood flood zone (GIS)."]
    },
    {
      "address": "3/241 Horizon Drive, Westlake QLD 4074",
      "risk": "Low",
      "proximity": "Near",
      "distanceMetres": 7.68,
      "reasons": ["Location is 7.7m from Low likelihood flood zone (GIS)."]
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
      +---> IGeocodingService (address -> coordinates)
      |           |
      |           +-- AzureMapsGeocodingService (production)
      |           +-- FileGeocodingService (development)
      |           +-- StubGeocodingService (testing)
      |
      +---> IFloodDataProvider (coordinates -> flood risk)
                  |
                  +-- GisFloodDataProvider
                            |
                            v
                      IFloodZoneIndex (spatial queries)
                            |
                            +-- BccFloodZoneIndex (NDJSON + R-tree)
```

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

- **Point-based geocoding**: The system checks flood risk at a single geocoded point (address centroid). For multi-dwelling developments, the centroid may be outside flood zones even when lot boundaries intersect them. See `KnownLimitationsTests.cs` for documented examples.
- **No cadastral data**: Accurate lot-boundary flood assessment would require cadastral (lot boundary) data from QSpatial.

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

- Cadastral data integration for lot boundary flood assessment
- Additional screening types (bushfire, erosion, etc.)
- Position assessment (heuristic-based scoring)

## License

MIT License - see LICENSE file for details
