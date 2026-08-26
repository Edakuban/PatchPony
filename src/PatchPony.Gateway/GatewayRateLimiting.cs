using System.Security.Claims;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record GatewayRateLimitOptions(
    int ApiPermitLimit = 60,
    int McpPermitLimit = 30,
    int WindowSeconds = 60,
    int MaximumTrackedPartitions = 1_000)
{
    public const string SectionName = "PatchPony:RateLimiting";

    public void Validate()
    {
        if (ApiPermitLimit is < 1 or > 10_000 || McpPermitLimit is < 1 or > 10_000 || WindowSeconds is < 1 or > 3_600 ||
            MaximumTrackedPartitions is < 64 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(nameof(GatewayRateLimitOptions), "Rate limit values are outside the supported safe range.");
        }
    }
}

public sealed class GatewayRateLimitStore(GatewayRateLimitOptions limits)
{
    private readonly object sync = new();
    private readonly Dictionary<string, FixedWindowCounter> counters = new(StringComparer.Ordinal);
    private DateTimeOffset lastPrunedAt = DateTimeOffset.MinValue;

    public bool TryAcquire(HttpContext context, int permitLimit, DateTimeOffset now)
    {
        var partitionKey = PartitionKey(context);
        var window = TimeSpan.FromSeconds(limits.WindowSeconds);

        lock (sync)
        {
            PruneExpired(now, window);
            if (!counters.TryGetValue(partitionKey, out var counter))
            {
                if (counters.Count >= limits.MaximumTrackedPartitions)
                {
                    return false;
                }

                counter = new FixedWindowCounter(now);
                counters.Add(partitionKey, counter);
            }

            if (now - counter.WindowStartedAt >= window)
            {
                counter.WindowStartedAt = now;
                counter.Count = 0;
            }

            if (counter.Count >= permitLimit)
            {
                return false;
            }

            counter.Count++;
            return true;
        }
    }

    private void PruneExpired(DateTimeOffset now, TimeSpan window)
    {
        if (now - lastPrunedAt < window)
        {
            return;
        }

        foreach (var key in counters.Where(entry => now - entry.Value.WindowStartedAt >= window).Select(entry => entry.Key).ToArray())
        {
            counters.Remove(key);
        }

        lastPrunedAt = now;
    }

    private static string PartitionKey(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(subject))
        {
            return $"subject:{subject}";
        }

        return $"connection:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private sealed class FixedWindowCounter(DateTimeOffset windowStartedAt)
    {
        public DateTimeOffset WindowStartedAt { get; set; } = windowStartedAt;

        public int Count { get; set; }
    }
}

public sealed class GatewayRateLimitMiddleware(RequestDelegate next, GatewayRateLimitStore store, GatewayRateLimitOptions limits)
{
    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlations)
    {
        var permitLimit = GetPermitLimit(context.Request.Path);
        if (permitLimit is null)
        {
            await next(context);
            return;
        }

        if (!store.TryAcquire(context, permitLimit.Value, DateTimeOffset.UtcNow))
        {
            await GatewayErrorAdapter.WriteHttpErrorAsync(
                context,
                new DomainError("request.rate_limited", "Too many requests were received. Retry after the advertised interval."),
                correlations);
            context.Response.Headers.RetryAfter = limits.WindowSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return;
        }

        await next(context);
    }

    private int? GetPermitLimit(PathString path) =>
        path.StartsWithSegments("/mcp") ? limits.McpPermitLimit :
        path.StartsWithSegments(ApiV1Endpoints.Prefix) ? limits.ApiPermitLimit :
        null;
}