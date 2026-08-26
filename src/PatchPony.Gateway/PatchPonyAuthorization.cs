using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace PatchPony.Gateway;

public static class PatchPonyRoles
{
    public const string CodeReader = "code-reader";
    public const string IssuePlanner = "issue-planner";
    public const string KnowledgeReader = "knowledge-reader";
    public const string KnowledgeEditor = "knowledge-editor";
    public const string ConfigEditor = "config-editor";
    public const string Reviewer = "reviewer";
    public const string ServiceN8n = "service-n8n";

    public static readonly string[] All =
    [
        CodeReader,
        IssuePlanner,
        KnowledgeReader,
        KnowledgeEditor,
        ConfigEditor,
        Reviewer,
        ServiceN8n
    ];
}

public static class PatchPonyScopes
{
    public const string ProjectRead = "project:read";
    public const string SkillsRead = "skills:read";
    public const string KnowledgeRead = "knowledge:read";
    public const string KnowledgeWrite = "knowledge:write";
    public const string SourceRead = "source:read";
    public const string ConfigRead = "config:read";
    public const string ConfigWrite = "config:write";
    public const string TestsRun = "tests:run";
    public const string ChangesWrite = "changes:write";
    public const string GitCommit = "git:commit";
    public const string GitPush = "git:push";
    public const string GitMergeRequest = "git:merge-request";

    public static readonly string[] All =
    [
        ProjectRead,
        SkillsRead,
        KnowledgeRead,
        KnowledgeWrite,
        SourceRead,
        ConfigRead,
        ConfigWrite,
        TestsRun,
        ChangesWrite,
        GitCommit,
        GitPush,
        GitMergeRequest
    ];
}

public static class PatchPonyAuthorization
{
    public static string RolePolicy(string role) => $"role:{role}";

    public static string ScopePolicy(string scope) => $"scope:{scope}";

    public static void Configure(AuthorizationOptions options)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder(PatchPonyAuthenticationDefaults.Scheme)
            .RequireAuthenticatedUser()
            .Build();
        foreach (var role in PatchPonyRoles.All)
        {
            options.AddPolicy(RolePolicy(role), policy => policy.RequireAssertion(context => HasRole(context.User, role)));
        }

        foreach (var scope in PatchPonyScopes.All)
        {
            options.AddPolicy(ScopePolicy(scope), policy => policy.RequireAssertion(context => HasScope(context.User, scope)));
        }
    }

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal user) =>
        user.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .SelectMany(claim => SplitValues(claim.Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<string> GetScopes(ClaimsPrincipal user) =>
        user.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => SplitValues(claim.Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    public static bool HasProjectRole(ClaimsPrincipal user, string projectId, string role) =>
        user.Claims
            .Where(claim => claim.Type is "project_role" or "project_roles")
            .SelectMany(claim => SplitValues(claim.Value))
            .Any(value => string.Equals(value, $"{projectId}:{role}", StringComparison.Ordinal));

    public static bool HasRole(ClaimsPrincipal user, string role) =>
        GetRoles(user).Contains(role, StringComparer.Ordinal);

    public static bool HasScope(ClaimsPrincipal user, string scope) =>
        GetScopes(user).Contains(scope, StringComparer.Ordinal);

    private static IEnumerable<string> SplitValues(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed class ReadOnlyRoleScopeClaimsTransformation : IClaimsTransformation
{
    private static readonly IReadOnlyDictionary<string, string[]> ReadOnlyScopesByRole =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [PatchPonyRoles.CodeReader] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.SkillsRead, PatchPonyScopes.SourceRead, PatchPonyScopes.ConfigRead],
            [PatchPonyRoles.IssuePlanner] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.SkillsRead, PatchPonyScopes.KnowledgeRead, PatchPonyScopes.SourceRead, PatchPonyScopes.ConfigRead],
            [PatchPonyRoles.KnowledgeReader] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.KnowledgeRead],
            [PatchPonyRoles.KnowledgeEditor] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.KnowledgeRead],
            [PatchPonyRoles.ConfigEditor] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.ConfigRead],
            [PatchPonyRoles.Reviewer] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.KnowledgeRead, PatchPonyScopes.SourceRead, PatchPonyScopes.ConfigRead],
            [PatchPonyRoles.ServiceN8n] = [PatchPonyScopes.ProjectRead, PatchPonyScopes.SkillsRead, PatchPonyScopes.KnowledgeRead, PatchPonyScopes.SourceRead, PatchPonyScopes.ConfigRead]
        };

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var existingScopes = PatchPonyAuthorization.GetScopes(principal).ToHashSet(StringComparer.Ordinal);
        var inheritedScopes = PatchPonyAuthorization.GetRoles(principal)
            .Where(ReadOnlyScopesByRole.ContainsKey)
            .SelectMany(role => ReadOnlyScopesByRole[role])
            .Distinct(StringComparer.Ordinal);

        foreach (var scope in inheritedScopes.Where(scope => existingScopes.Add(scope)))
        {
            identity.AddClaim(new Claim("scope", scope));
        }

        return Task.FromResult(principal);
    }
}