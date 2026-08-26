using System.Security.Claims;
using PatchPony.Core.Application;
using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public static class ApiV1Endpoints
{
    public const string Prefix = "/api/v1";

    public static RouteGroupBuilder MapApiV1(this WebApplication app)
    {
        var api = app.MapGroup(Prefix);
        api.RequireAuthorization();

        api.MapPost("/integrations/zoho/tasks", async (HttpRequest request, ZohoWebhookValidator validator, ZohoTicketNormalizer normalizer, ZohoTicketProjectResolver projects, ZohoTicketCompletenessEvaluator completeness, ZohoTicketTriageService triage, ZohoInformationRequestService informationRequests, CancellationToken cancellationToken) =>
            {
                var validated = await validator.ValidateAsync(request, cancellationToken);
                if (!validated.IsValid)
                {
                    return Results.Json(new { code = validated.ErrorCode }, statusCode: validated.ErrorCode == "zoho.webhook.unconfigured" ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest);
                }

                var normalized = normalizer.Normalize(validated.Payload!, DateTimeOffset.UtcNow);
                if (!normalized.IsSuccess) return Results.Json(new { code = normalized.Error.Code }, statusCode: StatusCodes.Status400BadRequest);
                var mapped = projects.Resolve(normalized.Value!, validated.Payload!.ZohoProjectId);
                if (!mapped.IsSuccess) return Results.Json(new { code = mapped.Error.Code }, statusCode: mapped.Error.Code == "ticket.project_mapping.unconfigured" ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status422UnprocessableEntity);
                var assessment = completeness.Evaluate(mapped.Value!);
                if (!assessment.IsSuccess) return Results.Json(new { code = assessment.Error.Code }, statusCode: StatusCodes.Status503ServiceUnavailable);
                var triageOutput = triage.Create(mapped.Value!, validated.IdempotencyKey!, assessment.Value!);
                if (!triageOutput.IsSuccess) return Results.Json(new { code = triageOutput.Error.Code }, statusCode: StatusCodes.Status400BadRequest);
                if (triageOutput.Value!.Disposition == TicketTriageDisposition.NeedsInformation)
                {
                    var informationRequest = informationRequests.Create(triageOutput.Value);
                    if (!informationRequest.IsSuccess) return Results.Json(new { code = informationRequest.Error.Code }, statusCode: StatusCodes.Status400BadRequest);
                }
                return Results.Accepted(value: new { received = true });
            })
            .AllowAnonymous()
            .WithName("ZohoTaskWebhook");
        api.MapGet("", () => Results.Ok(new ApiVersionInfo("v1", "PatchPony", "read-only")))
            .WithName("ApiV1Info");
        api.MapGet("/runtime/status", (RuntimeStatusService status) => Results.Ok(status.Get()))
            .WithName("RuntimeStatus");
        api.MapGet("/runtime/audit/access", (int? limit, IAccessDecisionAudit audit) => Results.Ok(audit.GetRecent(limit ?? 100)))
            .RequireAuthorization(PatchPonyAuthorization.RolePolicy(PatchPonyRoles.Reviewer))
            .WithName("RecentAccessDecisionAudit");
        api.MapGet("/runtime/identity", (ClaimsPrincipal user) => Results.Ok(new RuntimeIdentity(
                user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
                user.FindFirst("auth_mode")?.Value ?? user.Identity?.AuthenticationType ?? "unknown",
                PatchPonyAuthorization.GetRoles(user),
                PatchPonyAuthorization.GetScopes(user))))
            .RequireAuthorization()
            .WithName("RuntimeIdentity");
        api.MapPost("/projects/{projectId}/access", (
                string projectId,
                ProjectToolAccessRequest request,
                ClaimsPrincipal user,
                ProjectToolAuthorizationService authorization,
                ICorrelationContext correlations) =>
            {
                var result = authorization.Authorize(user, projectId, request);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
            })
            .RequireAuthorization()
            .WithName("ProjectToolAccessCheck");
        api.MapGet("/runtime/correlations/{correlationId}", (string correlationId, RuntimeStatusService status, ICorrelationContext correlations) =>
        {
            var result = status.ValidateCorrelation(correlationId);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).WithName("RuntimeCorrelationValidation");

        api.MapPost("/projects/{projectId}/sessions/{sessionId}/config/validate", (
                string projectId,
                string sessionId,
                ConfigValidateCommand command,
                ClaimsPrincipal user,
                ConfigSessionCatalog sessions,
                ProjectToolAuthorizationService authorization,
                ICorrelationContext correlations,
                CancellationToken cancellationToken) =>
            {
                var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("config.validate", new Dictionary<string, string?>
                {
                    ["sessionId"] = sessionId, ["path"] = command.Path, ["format"] = command.Format, ["schemaId"] = command.SchemaId
                }));
                if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
                var result = sessions.Validate(projectId, sessionId, command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
            })
            .RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.ConfigRead))
            .WithName("ConfigValidate");
        api.MapPost("/projects/{projectId}/sessions/{sessionId}/config/patch", (
                string projectId,
                string sessionId,
                ConfigPatchCommand command,
                ClaimsPrincipal user,
                ConfigSessionCatalog sessions,
                ProjectToolAuthorizationService authorization,
                ICorrelationContext correlations,
                CancellationToken cancellationToken) =>
            {
                var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("config.patch", new Dictionary<string, string?>
                {
                    ["sessionId"] = sessionId, ["path"] = command.Path, ["format"] = command.Format, ["expectedSourceSha256"] = command.ExpectedSourceSha256, ["replacementBase64"] = command.ReplacementBase64, ["schemaId"] = command.SchemaId
                }));
                if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
                var result = sessions.Patch(projectId, sessionId, command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
            })
            .RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.ConfigWrite))
            .WithName("ConfigPatch");
        api.MapGet("/projects/{projectId}/sessions/{sessionId}/tests", (string projectId, string sessionId, ClaimsPrincipal user, TestSessionCatalog sessions, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("tests.list", new Dictionary<string, string?> { ["sessionId"] = sessionId }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = sessions.List(projectId, sessionId);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.TestsRun)).WithName("TestsList");
        api.MapPost("/projects/{projectId}/sessions/{sessionId}/tests/run", (string projectId, string sessionId, TestRunCommand command, ClaimsPrincipal user, TestSessionCatalog sessions, ProjectToolAuthorizationService authorization, ICorrelationContext correlations, CancellationToken cancellationToken) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("tests.run", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["commandId"] = command.CommandId }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = sessions.Run(projectId, sessionId, command, cancellationToken);
            return result.IsSuccess ? Results.Accepted($"{Prefix}/projects/{projectId}/sessions/{sessionId}/tests/{result.Value!.ExecutionId:N}", result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.TestsRun)).WithName("TestsRun");
        api.MapGet("/projects/{projectId}/sessions/{sessionId}/tests/{executionId}", (string projectId, string sessionId, string executionId, ClaimsPrincipal user, TestSessionCatalog sessions, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("tests.result", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["executionId"] = executionId }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = sessions.Result(projectId, sessionId, executionId);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.TestsRun)).WithName("TestsResult");        api.MapGet("/projects/{projectId}/knowledge/tree", (string projectId, string? path, int? depth, ClaimsPrincipal user, KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("knowledge.tree", Optional(("path", path), ("depth", depth?.ToString()))));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = knowledge.Tree(projectId, path, depth);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.KnowledgeRead)).WithName("KnowledgeTree");
        api.MapGet("/projects/{projectId}/knowledge/search", (string projectId, string query, ClaimsPrincipal user, KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("knowledge.search", new Dictionary<string, string?> { ["query"] = query }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = knowledge.Search(projectId, query);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.KnowledgeRead)).WithName("KnowledgeSearch");
        api.MapGet("/projects/{projectId}/knowledge/read", (string projectId, string path, ClaimsPrincipal user, KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("knowledge.read", new Dictionary<string, string?> { ["path"] = path }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = knowledge.Read(projectId, path);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.KnowledgeRead)).WithName("KnowledgeRead");
        api.MapGet("/projects/{projectId}/knowledge/links", (string projectId, string path, ClaimsPrincipal user, KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, ICorrelationContext correlations) =>
        {
            var access = authorization.Authorize(user, projectId, new ProjectToolAccessRequest("knowledge.links", new Dictionary<string, string?> { ["path"] = path }));
            if (!access.IsSuccess) return GatewayErrorAdapter.ToHttpResult(access.Error, correlations);
            var result = knowledge.Links(projectId, path);
            return result.IsSuccess ? Results.Ok(result.Value) : GatewayErrorAdapter.ToHttpResult(result.Error, correlations);
        }).RequireAuthorization(PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.KnowledgeRead)).WithName("KnowledgeLinks");

        return api;
    }
    private static IReadOnlyDictionary<string, string?> Optional(params (string Name, string? Value)[] values) => values.Where(item => item.Value is not null).ToDictionary(item => item.Name, item => item.Value);
}

public sealed record ApiVersionInfo(string Version, string Service, string Mode);

public sealed record RuntimeIdentity(string Subject, string AuthenticationMode, IReadOnlyList<string> Roles, IReadOnlyList<string> Scopes);
