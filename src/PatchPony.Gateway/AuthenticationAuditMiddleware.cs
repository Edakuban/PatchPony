using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed class AuthenticationAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAccessDecisionAudit audit, ICorrelationContext correlations)
    {
        if (IsExternalRuntimeRoute(context.Request.Path))
        {
            var authenticated = context.User.Identity?.IsAuthenticated == true;
            audit.Record(new AccessDecisionAuditEvent(
                DateTimeOffset.UtcNow,
                correlations.Current.Value,
                "authentication",
                authenticated ? "allowed" : "rejected",
                AccessDecisionAudit.Subject(context.User),
                AccessDecisionAudit.AuthenticationMode(context.User),
                context.Request.Method,
                context.Request.Path,
                null,
                null,
                null));
        }

        await next(context);
    }

    private static bool IsExternalRuntimeRoute(PathString path) =>
        path.StartsWithSegments(ApiV1Endpoints.Prefix, StringComparison.Ordinal) ||
        path.StartsWithSegments("/mcp", StringComparison.Ordinal);
}