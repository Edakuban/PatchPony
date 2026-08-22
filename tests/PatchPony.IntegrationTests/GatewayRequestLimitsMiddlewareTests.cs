using System.Text;
using Microsoft.AspNetCore.Http;
using PatchPony.Core.Common;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class GatewayRequestLimitsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReplacesAnOversizedResponseWithTheCommonErrorContract()
    {
        var correlations = new CorrelationContext();
        var middleware = new GatewayRequestLimitsMiddleware(async context =>
        {
            await context.Response.WriteAsync(new string('x', 1_025));
        }, new GatewayRequestLimits(MaximumResponseBytes: 1_024));
        var context = CreateContext("/api/v1/test");

        using (correlations.BeginScope(new CorrelationId("result-limit-42")))
        {
            await middleware.InvokeAsync(context, correlations);
        }

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Contains("response.too_large", body, StringComparison.Ordinal);
        Assert.Contains("result-limit-42", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_RejectsAnOversizedRequestTarget()
    {
        var correlations = new CorrelationContext();
        var middleware = new GatewayRequestLimitsMiddleware(_ => Task.CompletedTask, new GatewayRequestLimits(MaximumRequestTargetCharacters: 128));
        var context = CreateContext("/api/v1/test");
        context.Request.QueryString = new QueryString("?q=" + new string('x', 200));

        using (correlations.BeginScope(new CorrelationId("target-limit-42")))
        {
            await middleware.InvokeAsync(context, correlations);
        }

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status414UriTooLong, context.Response.StatusCode);
        Assert.Contains("request.target_too_large", body, StringComparison.Ordinal);
    }
    [Fact]
    public async Task InvokeAsync_RejectsASecondConcurrentManagedRequest()
    {
        var correlations = new CorrelationContext();
        var releaseFirstRequest = new TaskCompletionSource();
        var enteredFirstRequest = new TaskCompletionSource();
        var middleware = new GatewayRequestLimitsMiddleware(async _ =>
        {
            enteredFirstRequest.SetResult();
            await releaseFirstRequest.Task;
        }, new GatewayRequestLimits(MaximumParallelRequests: 1));
        var first = CreateContext("/mcp");
        var second = CreateContext("/mcp");

        Task firstInvocation;
        using (correlations.BeginScope(new CorrelationId("first-request")))
        {
            firstInvocation = middleware.InvokeAsync(first, correlations);
        }
        await enteredFirstRequest.Task;
        using (correlations.BeginScope(new CorrelationId("second-request")))
        {
            await middleware.InvokeAsync(second, correlations);
        }
        releaseFirstRequest.SetResult();
        await firstInvocation;

        second.Response.Body.Position = 0;
        var body = await new StreamReader(second.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
        Assert.Contains("request.concurrency_limited", body, StringComparison.Ordinal);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
