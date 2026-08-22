using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlations)
    {
        var requested = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlation = CorrelationId.Create(requested);
        var correlationId = correlation.IsSuccess ? correlation.Value! : CorrelationId.New();
        context.Response.Headers[HeaderName] = correlationId.Value;

        using (correlations.BeginScope(correlationId))
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId.Value }))
        {
            await next(context);
        }
    }
}
