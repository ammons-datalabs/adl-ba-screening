using AmmonsDataLabs.BuyersAgent.Flood;
using Microsoft.AspNetCore.Mvc;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Endpoints;

public static class FloodEndpoints
{
    public static IEndpointRouteBuilder MapFloodEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/screening/flood/lookup", async (
                FloodLookupRequest req,
                IFloodScreeningService svc,
                CancellationToken ct) =>
            {
                if (req.Properties.Count == 0)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["properties"] = ["At least one property is required."]
                    });

                var result = await svc.ScreenAsync(req, ct);
                return Results.Ok(result);
            })
            .WithName("FloodLookup")
            .WithOpenApi();

        app.MapGet("/v1/screening/flood/summary", async (
                [FromQuery] string? address,
                IFloodDataProvider floodProvider,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return Results.BadRequest("address query parameter is required");

                var result = await floodProvider.LookupAsync(address, ct);
                var summary = FloodSummaryMapper.FromResult(result);
                return Results.Ok(summary);
            })
            .WithName("FloodSummary")
            .WithOpenApi();

        return app;
    }
}