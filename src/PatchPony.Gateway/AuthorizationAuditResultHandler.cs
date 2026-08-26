using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed class AuthorizationAuditResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var audit = context.RequestServices.GetRequiredService<IAccessDecisionAudit>();
        var correlations = context.RequestServices.GetRequiredService<ICorrelationContext>();
        audit.Record(new AccessDecisionAuditEvent(
            DateTimeOffset.UtcNow,
            correlations.Current.Value,
            "policy",
            authorizeResult.Succeeded ? "allowed" : "rejected",
            AccessDecisionAudit.Subject(context.User),
            AccessDecisionAudit.AuthenticationMode(context.User),
            context.Request.Method,
            context.Request.Path,
            null,
            null,
            null));

        await fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}