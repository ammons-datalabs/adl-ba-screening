# adl-ba-screening

Buyer's Agent property screening API for Ammons Data Labs. Provides flood risk assessment and other property screening services for Queensland properties.

## Projects

- **AmmonsDataLabs.BuyersAgent.Flood** - Flood screening domain (contracts + DTOs)
- **AmmonsDataLabs.BuyersAgent.Screening.Api** - Minimal API exposing screening endpoints
- **tests/** - Unit and integration tests

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/api/screening/flood/lookup` | Batch flood risk lookup |

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

## API Usage

### Health Check

```http
GET http://localhost:5136/health
```

### Flood Lookup

```http
POST http://localhost:5136/api/screening/flood/lookup
Content-Type: application/json

{
  "properties": [
    { "address": "123 Fake Street, Brisbane QLD" },
    { "address": "456 Main Road, Mount Gravatt QLD" }
  ]
}
```

**Response (200 OK):**

```json
{
  "results": [
    {
      "address": "123 Fake Street, Brisbane QLD",
      "risk": "Unknown",
      "reasons": ["Flood screening not yet implemented."]
    },
    {
      "address": "456 Main Road, Mount Gravatt QLD",
      "risk": "Unknown",
      "reasons": ["Flood screening not yet implemented."]
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
│   │   └── IFloodScreeningService.cs
│   └── AmmonsDataLabs.BuyersAgent.Screening.Api/   # Minimal API
│       ├── Endpoints/
│       ├── Services/
│       └── Program.cs
└── tests/
    ├── AmmonsDataLabs.BuyersAgent.Flood.Tests/     # Domain unit tests
    └── AmmonsDataLabs.BuyersAgent.Screening.Api.Tests/  # API integration tests
```

## Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
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
| `Unknown` | Risk level could not be determined |
| `Low` | Low flood risk |
| `Medium` | Medium flood risk |
| `High` | High flood risk |

## Future Screening Types

The API is designed to support additional screening types:

- Position assessment

## License

MIT License - see LICENSE file for details