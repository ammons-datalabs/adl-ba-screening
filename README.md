# adl-ba-screening

Buyer's Agent property screening API for Ammons Data Labs. Provides flood risk assessment and other property screening services for Queensland properties.

[![CI](https://github.com/ammons-datalabs/adl-ba-screening/actions/workflows/ci.yml/badge.svg)](https://github.com/ammons-datalabs/adl-ba-screening/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/ammons-datalabs/adl-ba-screening/branch/main/graph/badge.svg)](https://codecov.io/gh/ammons-datalabs/adl-ba-screening)
## Projects

- **AmmonsDataLabs.BuyersAgent.Flood** - Flood screening domain (contracts + DTOs)
- **AmmonsDataLabs.BuyersAgent.Screening.Api** - Minimal API exposing screening endpoints
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

# Run API locally
dotnet run --project src/AmmonsDataLabs.BuyersAgent.Screening.Api
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
    { "address": "123 Fake Street, Brisbane QLD" },
    { "address": "456 Main Road, Mount Gravatt QLD" },
    { "address": "10 Oxley Creek Road, Oxley QLD" }
  ]
}
```

**Response (200 OK):**

```json
{
  "results": [
    {
      "address": "123 Fake Street, Brisbane QLD",
      "risk": "Low",
      "reasons": ["No known flood indicators (demo rule - pending real flood data)."]
    },
    {
      "address": "456 Main Road, Mount Gravatt QLD",
      "risk": "High",
      "reasons": ["Near major road (demo rule - pending real flood data)."]
    },
    {
      "address": "10 Oxley Creek Road, Oxley QLD",
      "risk": "Medium",
      "reasons": ["Near waterway (demo rule - pending real flood data)."]
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

## Project Structure

```
adl-ba-screening/
├── src/
│   ├── AmmonsDataLabs.BuyersAgent.Flood/           # Domain contracts + DTOs
│   │   ├── FloodRisk.cs
│   │   ├── FloodLookupRequest.cs
│   │   ├── FloodLookupResponse.cs
│   │   ├── FloodLookupResult.cs
│   │   ├── FloodLookupItem.cs
│   │   ├── IFloodScreeningService.cs
│   │   └── IFloodDataProvider.cs
│   └── AmmonsDataLabs.BuyersAgent.Screening.Api/   # Minimal API
│       ├── Endpoints/
│       ├── Services/
│       │   ├── FloodScreeningService.cs
│       │   └── SimpleFloodDataProvider.cs
│       └── Program.cs
├── tests/
│   ├── AmmonsDataLabs.BuyersAgent.Flood.Tests/
│   └── AmmonsDataLabs.BuyersAgent.Screening.Api.Tests/
└── .github/
    └── workflows/
        └── ci.yml
```

## Architecture

```
FloodEndpoints
      │
      ▼
IFloodScreeningService
      │
      ▼
FloodScreeningService
      │
      ▼
IFloodDataProvider
      │
      ├── SimpleFloodDataProvider (v0 demo rules)
      └── QldFloodDataProvider (future - real data)
```

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

## Development

### Swagger UI

When running in Development mode, Swagger UI is available at:

```
http://localhost:5136/swagger
```

### HTTP Client

The project includes an `Api.http` file for testing endpoints in Rider/Visual Studio.

## Flood Risk Levels

| Risk | Description |
|------|-------------|
| `Low` | Low flood risk |
| `Medium` | Medium flood risk |
| `High` | High flood risk |
| `Unknown` | Risk level could not be determined |

## Current Implementation (v0)

The current `SimpleFloodDataProvider` uses address pattern matching as a temporary stand-in for real flood data:

| Pattern in Address | Risk |
|-------------------|------|
| Main Rd, Main Road, Motorway, Highway | High |
| Ck, Creek, River | Medium |
| (no match) | Low |

This will be replaced with `QldFloodDataProvider` when real council/GIS flood data is integrated.

## Future Screening Types

The API is designed to support additional screening types:

- Position assessment (heuristic-based scoring)

## License

MIT License - see LICENSE file for details