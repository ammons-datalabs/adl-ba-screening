using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Configuration;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddProblemDetailsWithTracing(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                if (environment.IsDevelopment() && context.Exception != null)
                    context.ProblemDetails.Extensions["exceptionDetails"] = context.Exception.ToString();
            };
        });

        return services;
    }

    public static IApplicationBuilder UseProblemDetailsExceptionHandler(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var problemDetails = new ProblemDetails
                {
                    Instance = context.Request.Path,
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Detail = environment.IsDevelopment()
                        ? exception?.Message
                        : "Please try again later or contact support",
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                };

                context.Response.StatusCode = problemDetails.Status.Value;

                await context.Response.WriteAsJsonAsync(problemDetails, options: null,
                    contentType: "application/problem+json");
            });
        });

        return app;
    }
}