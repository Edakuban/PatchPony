using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Paths;

public sealed class ProjectPathAccessService(ProjectPathResolver resolver, ProjectPathPolicy policy)
{
    public Result<ResolvedProjectPath> ResolveAndAuthorize(string relativePath, ProjectPathAccess access)
    {
        var resolved = resolver.Resolve(relativePath);
        if (!resolved.IsSuccess)
        {
            return Result<ResolvedProjectPath>.Failure(resolved.Error);
        }

        var authorization = policy.Authorize(resolved.Value!.RelativePath, access);
        return authorization.IsSuccess
            ? Result<ResolvedProjectPath>.Success(resolved.Value)
            : Result<ResolvedProjectPath>.Failure(authorization.Error);
    }

    public Result<ResolvedProjectPath> ResolveDirectoryAndAuthorize(string relativePath)
    {
        var resolved = resolver.Resolve(relativePath);
        if (!resolved.IsSuccess)
        {
            return Result<ResolvedProjectPath>.Failure(resolved.Error);
        }

        var authorization = policy.Authorize($"{resolved.Value!.RelativePath}/", ProjectPathAccess.Read);
        return authorization.IsSuccess
            ? Result<ResolvedProjectPath>.Success(resolved.Value)
            : Result<ResolvedProjectPath>.Failure(authorization.Error);
    }
}