using AmmonsDataLabs.BuyersAgent.Screening.Api.Configuration;
using System.Text.Json.Serialization;
using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Endpoints;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add ProblemDetails support (RFC 7807)
builder.Services.AddProblemDetailsWithTracing(builder.Environment);

// Configure JSON to accept string enum values
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<FloodDataOptions>(
    builder.Configuration.GetSection(FloodDataOptions.SectionName));

var useGisProvider = builder.Configuration.GetValue<bool>("Flood:UseGisProvider");

if (useGisProvider)
{
    // GIS-based flood data provider with geocoding and zone lookup
    builder.Services.AddSingleton<IGeocodingService, DummyGeocodingService>();
    builder.Services.AddSingleton<IFloodZoneDataLoader, NdjsonFloodZoneDataLoader>();
    builder.Services.AddSingleton<IFloodZoneIndex, BccFloodZoneIndex>();
    builder.Services.AddScoped<IFloodDataProvider, GisFloodDataProvider>();
}
else
{
    // Simple pattern-based flood data provider (default)
    builder.Services.AddScoped<IFloodDataProvider, SimpleFloodDataProvider>();
}

builder.Services.AddScoped<IFloodScreeningService, FloodScreeningService>();

var app = builder.Build();

app.UseProblemDetailsExceptionHandler(app.Environment);

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Text("OK"))
    .WithName("Health")
    .WithOpenApi();

app.MapFloodEndpoints();

app.Run();

// Make Program accessible to tests
public partial class Program { }