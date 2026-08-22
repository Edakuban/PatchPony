using PatchPony.Core.Common;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Application;

public sealed class ProjectApplicationService(IProjectRepository projects)
{
    public async Task<Result<Project>> RegisterAsync(
        Project project,
        RepositoryRegistration? repository,
        CancellationToken cancellationToken = default)
    {
        var existing = await projects.GetAsync(project.Id, cancellationToken);
        if (existing is not null)
        {
            return Result<Project>.Failure(new DomainError("project.already_exists", "A project with this identifier already exists."));
        }

        if (repository is not null && repository.ProjectId != project.Id)
        {
            return Result<Project>.Failure(DomainError.Validation("The repository registration must belong to the project."));
        }

        await projects.AddAsync(project, repository, cancellationToken);
        return Result<Project>.Success(project);
    }
}
