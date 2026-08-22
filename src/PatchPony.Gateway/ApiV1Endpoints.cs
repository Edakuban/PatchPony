using PatchPony.Core.Application;
using PatchPony.Core.Common;
namespace PatchPony.Gateway;

public static class ApiV1Endpoints
{
    public const string Prefix = "/api/v1";

    public static RouteGroupBuilder MapApiV1(this WebApplication app)
    {
        var api = app.MapGroup(Prefix);
        api.MapGet("", () => Results.Ok(new ApiVersionInfo("v1", "PatchPony", "read-only")))
            .WithName("ApiV1Info");
        api.MapGet("/runtime/status", (RuntimeStatusService status) => Results.Ok(status.Get()))
            .WithName("RuntimeStatus");        api.MapGet("/runtime/correlations/{correlationId}", (string correlationId, RuntimeStatusService status, ICorrelationContext correlations) =>
        {
            var result = status.ValidateCorrelation(correlationId);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).WithName("RuntimeCorrelationValidation");
        return api;
    }
}

public sealed record ApiVersionInfo(string Version, string Service, string Mode);
