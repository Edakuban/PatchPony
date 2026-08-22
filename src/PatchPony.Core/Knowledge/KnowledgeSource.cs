using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Knowledge;

public readonly record struct KnowledgeSourceId(Guid Value)
{
    public static KnowledgeSourceId New() => new(Guid.NewGuid());
}

public sealed record KnowledgeSource(
    KnowledgeSourceId Id,
    ProjectId ProjectId,
    string Name,
    Uri RemoteUri,
    string DefaultBranch,
    DateTimeOffset RegisteredAt)
{
    private static readonly Regex NamePattern = new("^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant);
    private static readonly Regex DefaultBranchPattern = new("^(?!/)(?!.*//)(?!.*(?:^|/)\\.\\.(?:/|$))[A-Za-z0-9._/-]{1,255}$", RegexOptions.CultureInvariant);

    public static Result<KnowledgeSource> Create(
        KnowledgeSourceId id,
        ProjectId projectId,
        string name,
        Uri remoteUri,
        string defaultBranch,
        DateTimeOffset registeredAt)
    {
        if (id.Value == Guid.Empty || projectId.Value == Guid.Empty || !NamePattern.IsMatch(name) ||
            !remoteUri.IsAbsoluteUri || (remoteUri.Scheme != Uri.UriSchemeHttps && remoteUri.Scheme != Uri.UriSchemeSsh) ||
            !DefaultBranchPattern.IsMatch(defaultBranch))
        {
            return Result<KnowledgeSource>.Failure(DomainError.Validation("Knowledge source registration is incomplete."));
        }

        return Result<KnowledgeSource>.Success(new KnowledgeSource(id, projectId, name, remoteUri, defaultBranch.Trim(), registeredAt));
    }
}

public sealed record KnowledgeSourceCheckout(KnowledgeSourceId SourceId, RepositoryRevision Revision, bool Created);

public interface IKnowledgeSourceCheckoutService
{
    Task<Result<KnowledgeSourceCheckout>> EnsureAsync(KnowledgeSource source, CancellationToken cancellationToken = default);
}
