using System.Security.Claims;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record ProjectToolAccessRequest(string Tool, IReadOnlyDictionary<string, string?> Parameters);

public sealed record ProjectToolAccessDecision(string ProjectId, string Tool, string RequiredScope);

public sealed class ProjectToolAuthorizationService(IAccessDecisionAudit audit, ICorrelationContext correlations)
{
    private static readonly Regex ProjectIdPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly IReadOnlyDictionary<string, ToolRequirement> Tools =
        new Dictionary<string, ToolRequirement>(StringComparer.Ordinal)
        {
            ["project.tree"] = new(PatchPonyScopes.ProjectRead, [], ["path", "depth"]),
            ["skills.list"] = new(PatchPonyScopes.SkillsRead, [], []),
            ["skills.read"] = new(PatchPonyScopes.SkillsRead, ["id"], ["id"]),
            ["source.search"] = new(PatchPonyScopes.SourceRead, ["query"], ["query"]),
            ["source.read"] = new(PatchPonyScopes.SourceRead, ["path"], ["path"]),
            ["knowledge.tree"] = new(PatchPonyScopes.KnowledgeRead, [], ["path", "depth"]),
            ["knowledge.search"] = new(PatchPonyScopes.KnowledgeRead, ["query"], ["query"]),
            ["knowledge.read"] = new(PatchPonyScopes.KnowledgeRead, ["path"], ["path"]),
            ["knowledge.links"] = new(PatchPonyScopes.KnowledgeRead, ["path"], ["path"]),
            ["config.validate"] = new(PatchPonyScopes.ConfigRead, ["sessionId", "path", "format"], ["sessionId", "path", "format", "schemaId"]),
            ["config.patch"] = new(PatchPonyScopes.ConfigWrite, ["sessionId", "path", "format", "expectedSourceSha256", "replacementBase64"], ["sessionId", "path", "format", "expectedSourceSha256", "replacementBase64", "schemaId"], 1_398_104, PatchPonyRoles.ConfigEditor),
            ["tests.list"] = new(PatchPonyScopes.TestsRun, ["sessionId"], ["sessionId"]),
            ["tests.run"] = new(PatchPonyScopes.TestsRun, ["sessionId", "commandId"], ["sessionId", "commandId"], 128),
            ["tests.result"] = new(PatchPonyScopes.TestsRun, ["sessionId", "executionId"], ["sessionId", "executionId"])
        };

    public Result<ProjectToolAccessDecision> Authorize(ClaimsPrincipal user, string projectId, ProjectToolAccessRequest request)
    {
        var requiredScope = Tools.TryGetValue(request.Tool, out var knownTool) ? knownTool.RequiredScope : null;
        Result<ProjectToolAccessDecision> result;

        if (!ProjectIdPattern.IsMatch(projectId) || knownTool is null)
        {
            result = Deny();
        }
        else if (!HasProjectAccess(user, projectId) || !PatchPonyAuthorization.HasScope(user, knownTool.RequiredScope) || (knownTool.RequiredProjectRole is not null && !PatchPonyAuthorization.HasProjectRole(user, projectId, knownTool.RequiredProjectRole)))
        {
            result = Deny();
        }
        else if (request.Parameters.Keys.Any(parameter => !knownTool.AllowedParameters.Contains(parameter, StringComparer.Ordinal)) ||
                 knownTool.RequiredParameters.Any(parameter => !request.Parameters.TryGetValue(parameter, out var value) || string.IsNullOrWhiteSpace(value)) ||
                 request.Parameters.Any(parameter => parameter.Value is { } value && value.Length > knownTool.MaximumParameterLength))
        {
            result = Deny();
        }
        else
        {
            result = Result<ProjectToolAccessDecision>.Success(new ProjectToolAccessDecision(projectId, request.Tool, knownTool.RequiredScope));
        }

        audit.Record(new AccessDecisionAuditEvent(
            DateTimeOffset.UtcNow,
            correlations.Current.Value,
            "policy.tool",
            result.IsSuccess ? "allowed" : "rejected",
            AccessDecisionAudit.Subject(user),
            AccessDecisionAudit.AuthenticationMode(user),
            "POST",
            $"{ApiV1Endpoints.Prefix}/projects/{{projectId}}/access",
            ProjectIdPattern.IsMatch(projectId) ? projectId : null,
            Tools.ContainsKey(request.Tool) ? request.Tool : null,
            requiredScope));

        return result;
    }

    public static bool HasProjectAccess(ClaimsPrincipal user, string projectId) =>
        user.Claims
            .Where(claim => claim.Type is "project" or "projects")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(projectId, StringComparer.Ordinal);

    private static Result<ProjectToolAccessDecision> Deny() =>
        Result<ProjectToolAccessDecision>.Failure(new DomainError("authorization.forbidden", "The requested project tool access is not permitted."));

    private sealed record ToolRequirement(string RequiredScope, string[] RequiredParameters, string[] AllowedParameters, int MaximumParameterLength = 1_024, string? RequiredProjectRole = null);
}