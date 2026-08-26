using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record AuthorizedPilotProject(string Id, string DisplayName);
public sealed record AuthorizedPilotProjectList(IReadOnlyList<AuthorizedPilotProject> Projects);

[McpServerToolType]
public sealed class ProjectCatalogMcpTools(PilotSourceCatalog sources, ProjectToolAuthorizationService authorization, IHttpContextAccessor httpContextAccessor, ICorrelationContext correlations)
{
    [McpServerTool(Name = "projects.list", Title = "List authorized pilot projects", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Lists only the identifiers and display names of pilot projects authorized for the current caller. It never exposes local paths, repository URLs, credentials, or project contents.")]
    public CallToolResult List()
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        var decision = authorization.AuthorizeCatalog(user, new ProjectToolAccessRequest("projects.list", new Dictionary<string, string?>()));
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);

        var response = new AuthorizedPilotProjectList(
            sources.List()
                .Where(project => ProjectToolAuthorizationService.HasProjectAccess(user, project.Id))
                .Select(project => new AuthorizedPilotProject(project.Id, project.DisplayName))
                .ToArray());
        return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response) }], StructuredContent = JsonSerializer.SerializeToElement(response) };
    }
}