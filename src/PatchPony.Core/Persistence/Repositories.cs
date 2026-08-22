using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default);

    Task<Project?> GetByManifestIdAsync(string manifestId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Project?>(null);

    Task<RepositoryRegistration?> GetRepositoryAsync(ProjectId projectId, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, RepositoryRegistration? repository, CancellationToken cancellationToken = default);
}

public interface IJobRepository
{
    Task<Job?> GetAsync(JobId id, CancellationToken cancellationToken = default);

    Task AddAsync(Job job, CancellationToken cancellationToken = default);

    Task UpdateAsync(Job job, CancellationToken cancellationToken = default);
}

public interface ISessionRepository
{
    Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default);

    Task AddAsync(Session session, CancellationToken cancellationToken = default);
}
