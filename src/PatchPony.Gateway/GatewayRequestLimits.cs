using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record GatewayRequestLimits(
    int MaximumRequestTargetCharacters = 4_096,
    int MaximumBodyBytes = 64 * 1024,
    int MaximumResponseBytes = 256 * 1024,
    int MaximumParallelRequests = 8)
{
    public void Validate()
    {
        if (MaximumRequestTargetCharacters is < 128 or > 16_384 || MaximumBodyBytes is < 1_024 or > 1_048_576 ||
            MaximumResponseBytes is < 1_024 or > 4_194_304 || MaximumParallelRequests is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(GatewayRequestLimits), "Gateway limits are outside the supported safe range.");
        }
    }
}

public sealed class GatewayRequestLimitsMiddleware(RequestDelegate next, GatewayRequestLimits limits)
{
    private readonly SemaphoreSlim gate = new(limits.MaximumParallelRequests, limits.MaximumParallelRequests);

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlations)
    {
        if (!IsManagedApi(context.Request.Path))
        {
            await next(context);
            return;
        }

        if ((context.Request.Path.Value?.Length ?? 0) + (context.Request.QueryString.Value?.Length ?? 0) > limits.MaximumRequestTargetCharacters)
        {
            await GatewayErrorAdapter.WriteHttpErrorAsync(context, new DomainError("request.target_too_large", "The request target exceeds the configured limit."), correlations);
            return;
        }

        if (context.Request.ContentLength > limits.MaximumBodyBytes)
        {
            await GatewayErrorAdapter.WriteHttpErrorAsync(context, new DomainError("request.body_too_large", "The request body exceeds the configured limit."), correlations);
            return;
        }

        if (!await gate.WaitAsync(0, context.RequestAborted))
        {
            await GatewayErrorAdapter.WriteHttpErrorAsync(context, new DomainError("request.concurrency_limited", "Too many concurrent API requests are active."), correlations);
            return;
        }

        var originalBody = context.Response.Body;
        await using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;
        try
        {
            await next(context);
            if (bufferedBody.Length > limits.MaximumResponseBytes)
            {
                await GatewayErrorAdapter.WriteHttpErrorAsync(context, new DomainError("response.too_large", "The response exceeds the configured limit."), correlations);
            }

            bufferedBody.Position = 0;
            await bufferedBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
            gate.Release();
        }
    }

    private static bool IsManagedApi(PathString path) =>
        path.StartsWithSegments("/mcp") || path.StartsWithSegments(ApiV1Endpoints.Prefix);
}
