using PatchPony.Core.Common;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Application;

public sealed class ProjectRegistrationService(IProjectRepository projects)
{
    public async Task<Result<Project>> RegisterAsync(ProjectManifest manifest, DateTimeOffset registeredAt, CancellationToken cancellationToken = default)
    {
        if (await projects.GetByManifestIdAsync(manifest.Project.Id, cancellationToken) is not null)
        {
            return Result<Project>.Failure(new DomainError("project.already_registered", "A project with this manifest identifier is already registered."));
        }

        var projectResult = Project.Create(ProjectId.New(), manifest.Project.Id, manifest.Project.DisplayName, registeredAt);
        if (!projectResult.IsSuccess)
        {
            return projectResult;
        }

        var repositoryResult = RepositoryRegistration.Create(projectResult.Value!.Id, manifest.Repository.RemoteUri, manifest.Repository.DefaultBranch);
        if (!repositoryResult.IsSuccess)
        {
            return Result<Project>.Failure(repositoryResult.Error);
        }

        await projects.AddAsync(projectResult.Value, repositoryResult.Value, cancellationToken);
        return projectResult;
    }
}