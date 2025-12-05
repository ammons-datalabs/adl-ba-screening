using AmmonsDataLabs.BuyersAgent.Flood;

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

        return app;
    }
}