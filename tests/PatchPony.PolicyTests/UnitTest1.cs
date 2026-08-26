using System.Security.Claims;
using PatchPony.Core.Common;
using PatchPony.Gateway;

namespace PatchPony.PolicyTests;

public sealed class NegativePolicyMatrixTests
{
    [Theory]
    [MemberData(nameof(RejectedToolRequests))]
    public void ProjectToolAuthorization_RejectsEveryDisallowedCombination(
        string scenario,
        string projectId,
        string tool,
        IReadOnlyDictionary<string, string?> parameters,
        string[] projects,
        string[] scopes)
    {
        var audit = new RecordingAudit();
        var correlations = new CorrelationContext();
        using var correlationScope = correlations.BeginScope(CorrelationId.New());
        var authorization = new ProjectToolAuthorizationService(audit, correlations);
        var principal = Principal(projects, scopes);

        var result = authorization.Authorize(principal, projectId, new ProjectToolAccessRequest(tool, parameters));

        Assert.False(result.IsSuccess, scenario);
        Assert.Equal("authorization.forbidden", result.Error.Code);
        var recorded = Assert.Single(audit.Events);
        Assert.Equal("policy.tool", recorded.Category);
        Assert.Equal("rejected", recorded.Outcome);
        Assert.Equal("POST", recorded.Method);
        Assert.DoesNotContain(parameters.Values.Where(value => value is not null).Cast<string>(), value =>
            string.Equals(recorded.ProjectId, value, StringComparison.Ordinal) ||
            string.Equals(recorded.Tool, value, StringComparison.Ordinal));
    }


    [Fact]
    public void ProjectToolAuthorization_AllowsConfigPatchesOnlyForTheAssignedConfigEditorProject()
    {
        var audit = new RecordingAudit();
        var correlations = new CorrelationContext();
        using var scope = correlations.BeginScope(CorrelationId.New());
        var authorization = new ProjectToolAuthorizationService(audit, correlations);
        var parameters = Parameters(
            ("sessionId", Guid.NewGuid().ToString("N")),
            ("path", "config/settings.json"),
            ("format", "json"),
            ("expectedSourceSha256", new string('a', 64)),
            ("replacementBase64", "e30="));
        var principal = Principal(["demo-project", "other-project"], [PatchPonyScopes.ConfigWrite], ["demo-project:config-editor"]);

        var allowed = authorization.Authorize(principal, "demo-project", new ProjectToolAccessRequest("config.patch", parameters));
        var denied = authorization.Authorize(principal, "other-project", new ProjectToolAccessRequest("config.patch", parameters));

        Assert.True(allowed.IsSuccess);
        Assert.False(denied.IsSuccess);
        Assert.Equal("authorization.forbidden", denied.Error.Code);
    }
    public static TheoryData<string, string, string, IReadOnlyDictionary<string, string?>, string[], string[]> RejectedToolRequests => new()
    {
        { "authenticated user without project claim", "demo-project", "source.read", Parameters(("path", "src/Program.cs")), [], [PatchPonyScopes.SourceRead] },
        { "n8n service account outside its project allowlist", "other-project", "source.read", Parameters(("path", "src/Program.cs")), ["demo-project"], [PatchPonyScopes.SourceRead] },
        { "caller lacks the tool scope", "demo-project", "source.read", Parameters(("path", "src/Program.cs")), ["demo-project"], [PatchPonyScopes.KnowledgeRead] },
        { "unknown tool identifier", "demo-project", "process.start", Parameters(("command", "cmd.exe")), ["demo-project"], [PatchPonyScopes.SourceRead] },
        { "forbidden tool parameter", "demo-project", "source.read", Parameters(("shell", "cmd.exe")), ["demo-project"], [PatchPonyScopes.SourceRead] },
        { "missing mandatory tool parameter", "demo-project", "source.read", Parameters(), ["demo-project"], [PatchPonyScopes.SourceRead] },
        { "oversized tool parameter", "demo-project", "source.read", Parameters(("path", new string('x', 1_025))), ["demo-project"], [PatchPonyScopes.SourceRead] },
        { "manipulated project identifier", "../other-project", "source.read", Parameters(("path", "src/Program.cs")), ["demo-project"], [PatchPonyScopes.SourceRead] }
    };

    private static ClaimsPrincipal Principal(string[] projects, string[] scopes, string[]? projectRoles = null)
    {
        var claims = new List<Claim>
        {
            new("sub", "policy-test-user"),
            new("roles", PatchPonyRoles.ServiceN8n)
        };
        claims.AddRange(projects.Select(project => new Claim("project", project)));
        claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));
        claims.AddRange((projectRoles ?? []).Select(role => new Claim("project_role", role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static IReadOnlyDictionary<string, string?> Parameters(params (string Name, string Value)[] values) =>
        values.ToDictionary(value => value.Name, value => (string?)value.Value, StringComparer.Ordinal);

    private sealed class RecordingAudit : IAccessDecisionAudit
    {
        public List<AccessDecisionAuditEvent> Events { get; } = [];

        public void Record(AccessDecisionAuditEvent auditEvent) => Events.Add(auditEvent);

        public IReadOnlyList<AccessDecisionAuditEvent> GetRecent(int maximumCount) => Events.Take(maximumCount).ToArray();
    }
}