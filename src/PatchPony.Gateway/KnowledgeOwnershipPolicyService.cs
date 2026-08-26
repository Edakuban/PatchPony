using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;

namespace PatchPony.Gateway;

public sealed class KnowledgeOwnershipOptions
{
    public const string SectionName = "PatchPony:KnowledgeOwnership";
    public KnowledgeOwnershipEntry[] Entries { get; init; } = [];
}

public sealed class KnowledgeOwnershipEntry
{
    public string ProjectId { get; init; } = string.Empty;
    public string PathPattern { get; init; } = string.Empty;
    public string[] Owners { get; init; } = [];
    public string[] Reviewers { get; init; } = [];
}

/// <summary>Reads only server configuration; no caller may supply owners or reviewers.</summary>
public sealed class KnowledgeOwnershipPolicyService(IConfiguration configuration)
{
    public Result<KnowledgeAreaReviewRequirement> Resolve(string projectId, string path)
    {
        try
        {
            var configured = configuration.GetSection(KnowledgeOwnershipOptions.SectionName).Get<KnowledgeOwnershipOptions>() ?? new KnowledgeOwnershipOptions();
            var policy = new KnowledgeAreaOwnershipPolicy(configured.Entries.Select(entry => new KnowledgeAreaOwnershipRule(entry.ProjectId, entry.PathPattern, entry.Owners, entry.Reviewers)));
            return policy.Resolve(projectId, path);
        }
        catch (ArgumentException)
        {
            return Result<KnowledgeAreaReviewRequirement>.Failure(new DomainError("knowledge.ownership.invalid", "The knowledge ownership configuration is invalid."));
        }
    }
}