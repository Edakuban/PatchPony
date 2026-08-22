using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Projects;

public readonly record struct ProjectId(Guid Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
}

public sealed record Project(ProjectId Id, string ManifestId, string Name, DateTimeOffset CreatedAt)
{
    public static Result<Project> Create(ProjectId id, string name, DateTimeOffset createdAt) =>
        Create(id, name, name, createdAt);

    public static Result<Project> Create(ProjectId id, string manifestId, string name, DateTimeOffset createdAt)
    {
        if (id.Value == Guid.Empty)
        {
            return Result<Project>.Failure(DomainError.Validation("A project identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(manifestId) || manifestId.Length > 63)
        {
            return Result<Project>.Failure(DomainError.Validation("Project manifest identifier must contain between 1 and 63 characters."));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            return Result<Project>.Failure(DomainError.Validation("Project name must contain between 1 and 120 characters."));
        }

        return Result<Project>.Success(new Project(id, manifestId.Trim(), name.Trim(), createdAt));
    }
}

public sealed record RepositoryRegistration(ProjectId ProjectId, Uri RemoteUri, string DefaultBranch)
{
    private static readonly Regex DefaultBranchPattern = new("^(?!/)(?!.*//)(?!.*(?:^|/)\\.\\.(?:/|$))[A-Za-z0-9._/-]{1,255}$", RegexOptions.CultureInvariant);

    public static Result<RepositoryRegistration> Create(ProjectId projectId, Uri remoteUri, string defaultBranch)
    {
        if (projectId.Value == Guid.Empty || !remoteUri.IsAbsoluteUri || (remoteUri.Scheme != Uri.UriSchemeHttps && remoteUri.Scheme != Uri.UriSchemeSsh) || !DefaultBranchPattern.IsMatch(defaultBranch))
        {
            return Result<RepositoryRegistration>.Failure(DomainError.Validation("Repository registration is incomplete."));
        }

        return Result<RepositoryRegistration>.Success(new RepositoryRegistration(projectId, remoteUri, defaultBranch.Trim()));
    }
}