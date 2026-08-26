using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Infrastructure.Git;

/// <summary>Maps a project to its server-owned base checkout directory.</summary>
public sealed class BaseCheckoutPathResolver : ISessionBaseCheckoutResolver
{
    private readonly string storageRoot;

    public BaseCheckoutPathResolver(string storageRoot)
    {
        if (!Path.IsPathFullyQualified(storageRoot))
        {
            throw new ArgumentException("The checkout storage root must be an absolute server-side path.", nameof(storageRoot));
        }

        this.storageRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storageRoot));
    }

    public Result<string> Resolve(ProjectId projectId)
    {
        if (projectId.Value == Guid.Empty)
        {
            return Result<string>.Failure(DomainError.Validation("A project identifier is required."));
        }

        var path = Path.Combine(storageRoot, projectId.Value.ToString("N"));
        var relative = Path.GetRelativePath(storageRoot, path);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return Result<string>.Failure(new DomainError("session.base_checkout_unavailable", "The configured base checkout is unavailable."));
        }

        return Directory.Exists(path)
            ? Result<string>.Success(path)
            : Result<string>.Failure(new DomainError("session.base_checkout_unavailable", "The configured base checkout is unavailable."));
    }
}