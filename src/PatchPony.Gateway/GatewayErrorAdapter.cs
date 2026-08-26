using System.Text.Json;
using ModelContextProtocol.Protocol;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record GatewayError(string Code, string Message, string CorrelationId);

public static class GatewayErrorAdapter
{
    public static IResult ToHttpResult(DomainError error, ICorrelationContext correlations) =>
        Results.Json(Create(error, correlations), statusCode: ToHttpStatusCode(error.Code));

    public static CallToolResult ToMcpResult(DomainError error, ICorrelationContext correlations) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = JsonSerializer.Serialize(Create(error, correlations)) }]
    };

    public static async Task WriteHttpErrorAsync(HttpContext context, DomainError error, ICorrelationContext correlations)
    {
        var response = Create(error, correlations);
        context.Response.Clear();
        context.Response.StatusCode = ToHttpStatusCode(error.Code);
        context.Response.ContentType = "application/json";
        context.Response.Headers[CorrelationMiddleware.HeaderName] = response.CorrelationId;
        await context.Response.WriteAsJsonAsync(response);
    }

    private static GatewayError Create(DomainError error, ICorrelationContext correlations) =>
        new(error.Code, error.Message, correlations.Current.Value);

    private static int ToHttpStatusCode(string code) =>
        code == "request.target_too_large" ? StatusCodes.Status414UriTooLong :
        code is "request.concurrency_limited" or "request.rate_limited" ? StatusCodes.Status429TooManyRequests :
        code.EndsWith(".not_found", StringComparison.Ordinal) ? StatusCodes.Status404NotFound :
        code.Contains("forbidden", StringComparison.Ordinal) ? StatusCodes.Status403Forbidden :
        code.Contains("already_", StringComparison.Ordinal) || code.Contains("conflict", StringComparison.Ordinal) || code.Contains("transition", StringComparison.Ordinal) ? StatusCodes.Status409Conflict :
        code.Contains("too_large", StringComparison.Ordinal) || code.Contains("limit", StringComparison.Ordinal) ? StatusCodes.Status413PayloadTooLarge :
        code.EndsWith(".timeout", StringComparison.Ordinal) ? StatusCodes.Status504GatewayTimeout :
        code.EndsWith(".unavailable", StringComparison.Ordinal) || code.EndsWith(".git_failed", StringComparison.Ordinal) ? StatusCodes.Status503ServiceUnavailable :
        StatusCodes.Status400BadRequest;
}