using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Projects;

public sealed record RepositoryRevision
{
    private static readonly Regex CommitPattern = new("^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$", RegexOptions.CultureInvariant);

    private RepositoryRevision(string commitId)
    {
        CommitId = commitId;
    }

    public string CommitId { get; }

    public static Result<RepositoryRevision> Create(string commitId)
    {
        if (string.IsNullOrWhiteSpace(commitId) || !CommitPattern.IsMatch(commitId.Trim()))
        {
            return Result<RepositoryRevision>.Failure(new DomainError("checkout.revision_invalid", "The checkout did not resolve to a valid immutable Git commit identifier."));
        }

        return Result<RepositoryRevision>.Success(new RepositoryRevision(commitId.Trim().ToLowerInvariant()));
    }
}

public sealed record BaseCheckout(ProjectId ProjectId, RepositoryRevision Revision, bool Created);

public interface IBaseCheckoutService
{
    Task<Result<BaseCheckout>> EnsureAsync(
        Project project,
        RepositoryRegistration repository,
        CancellationToken cancellationToken = default);
}