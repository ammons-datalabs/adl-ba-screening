using AmmonsDataLabs.BuyersAgent.Screening.Api.Models;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Endpoints;

public static class AnomalyEndpoints
{
    public static IEndpointRouteBuilder MapAnomalyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/screening/flood-anomalies", async (
                [FromBody] FloodAnomalyReport report,
                IFloodAnomalyStore store,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(report.Address))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["address"] = ["Address is required."]
                    });

                await store.AddAsync(report, ct);
                return Results.Accepted();
            })
            .WithName("CreateFloodAnomaly")
            .WithOpenApi();

        app.MapGet("/v1/screening/flood-anomalies", async (
                IFloodAnomalyStore store,
                CancellationToken ct) =>
            {
                var items = await store.GetAllAsync(ct);
                return Results.Ok(items.OrderByDescending(a => a.CreatedUtc));
            })
            .WithName("GetFloodAnomalies")
            .WithOpenApi();

        app.MapDelete("/v1/screening/flood-anomalies/{id}", async (
                string id,
                IFloodAnomalyStore store,
                CancellationToken ct) =>
            {
                var deleted = await store.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteFloodAnomaly")
            .WithOpenApi();

        app.MapDelete("/v1/screening/flood-anomalies", async (
                IFloodAnomalyStore store,
                CancellationToken ct) =>
            {
                await store.ClearAllAsync(ct);
                return Results.NoContent();
            })
            .WithName("ClearFloodAnomalies")
            .WithOpenApi();

        return app;
    }
}
