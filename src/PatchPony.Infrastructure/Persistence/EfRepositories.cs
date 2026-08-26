using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Infrastructure.Persistence;

public sealed class EfProjectRepository(PatchPonyDbContext database) : IProjectRepository
{
    public async Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default)
    {
        var row = await database.Projects.AsNoTracking().SingleOrDefaultAsync(project => project.Id == id.Value, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Project?> GetByManifestIdAsync(string manifestId, CancellationToken cancellationToken = default)
    {
        var row = await database.Projects.AsNoTracking().SingleOrDefaultAsync(project => project.ManifestId == manifestId, cancellationToken);
        return row is null ? null : ToDomain(row);
    }
    public async Task<RepositoryRegistration?> GetRepositoryAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        var row = await database.Repositories.AsNoTracking().SingleOrDefaultAsync(repository => repository.ProjectId == projectId.Value, cancellationToken);
        return row is null ? null : new RepositoryRegistration(projectId, new Uri(row.RemoteUri, UriKind.Absolute), row.DefaultBranch);
    }

    public async Task AddAsync(Project project, RepositoryRegistration? repository, CancellationToken cancellationToken = default)
    {
        database.Projects.Add(new ProjectRow { Id = project.Id.Value, ManifestId = project.ManifestId, Name = project.Name, CreatedAt = project.CreatedAt });
        if (repository is not null)
        {
            database.Repositories.Add(new RepositoryRow
            {
                ProjectId = repository.ProjectId.Value,
                RemoteUri = repository.RemoteUri.AbsoluteUri,
                DefaultBranch = repository.DefaultBranch
            });
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static Project ToDomain(ProjectRow row) => new(new ProjectId(row.Id), row.ManifestId, row.Name, row.CreatedAt);
}

public sealed class EfKnowledgeSourceRepository(PatchPonyDbContext database) : IKnowledgeSourceRepository
{
    public async Task<KnowledgeSource?> GetAsync(KnowledgeSourceId id, CancellationToken cancellationToken = default)
    {
        var row = await database.KnowledgeSources.AsNoTracking().SingleOrDefaultAsync(source => source.Id == id.Value, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<KnowledgeSource?> GetByProjectAndNameAsync(ProjectId projectId, string name, CancellationToken cancellationToken = default)
    {
        var row = await database.KnowledgeSources.AsNoTracking().SingleOrDefaultAsync(source => source.ProjectId == projectId.Value && source.Name == name, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task AddAsync(KnowledgeSource source, CancellationToken cancellationToken = default)
    {
        database.KnowledgeSources.Add(new KnowledgeSourceRow
        {
            Id = source.Id.Value,
            ProjectId = source.ProjectId.Value,
            Name = source.Name,
            RemoteUri = source.RemoteUri.AbsoluteUri,
            DefaultBranch = source.DefaultBranch,
            RegisteredAt = source.RegisteredAt
        });
        await database.SaveChangesAsync(cancellationToken);
    }

    private static KnowledgeSource ToDomain(KnowledgeSourceRow row) => new(
        new KnowledgeSourceId(row.Id),
        new ProjectId(row.ProjectId),
        row.Name,
        new Uri(row.RemoteUri, UriKind.Absolute),
        row.DefaultBranch,
        row.RegisteredAt);
}
public sealed class EfJobRepository(PatchPonyDbContext database) : IJobRepository
{
    public async Task<Job?> GetAsync(JobId id, CancellationToken cancellationToken = default)
    {
        var row = await database.Jobs.AsNoTracking().SingleOrDefaultAsync(job => job.Id == id.Value, cancellationToken);
        return row is null ? null : Job.Rehydrate(new JobId(row.Id), new ProjectId(row.ProjectId), row.Kind, row.Status, row.CreatedAt);
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        database.Jobs.Add(ToRow(job));
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        database.Jobs.Update(ToRow(job));
        foreach (var change in job.StatusChanges)
        {
            database.JobStatusChanges.Add(new JobStatusChangeRow
            {
                Id = Guid.NewGuid(),
                JobId = change.JobId.Value,
                From = change.From,
                To = change.To,
                OccurredAt = change.OccurredAt
            });
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static JobRow ToRow(Job job) => new()
    {
        Id = job.Id.Value,
        ProjectId = job.ProjectId.Value,
        Kind = job.Kind,
        Status = job.Status,
        CreatedAt = job.CreatedAt
    };
}

public sealed class EfSessionRepository(PatchPonyDbContext database) : ISessionRepository
{
    public async Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        var row = await database.Sessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        database.Sessions.Add(ToRow(session));
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        database.Sessions.Update(ToRow(session));
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetDueForCleanupAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default)
    {
        var rows = await database.Sessions.AsNoTracking()
            .Where(session => session.ExpiresAt <= now && session.Status != SessionStatus.Closed)
            .OrderBy(session => session.ExpiresAt)
            .ThenBy(session => session.Id)
            .Take(maximumCount)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Session>> GetForCrashRecoveryAsync(int maximumCount, CancellationToken cancellationToken = default)
    {
        var rows = await database.Sessions.AsNoTracking()
            .Where(session => session.Status == SessionStatus.Provisioning || session.Status == SessionStatus.Closing)
            .OrderBy(session => session.StatusChangedAt)
            .ThenBy(session => session.Id)
            .Take(maximumCount)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }
    private static Session ToDomain(SessionRow row) => new(
        new SessionId(row.Id),
        new ProjectId(row.ProjectId),
        new JobId(row.JobId),
        row.CreatedAt,
        row.ExpiresAt,
        row.Status,
        row.StatusChangedAt,
        row.FailureCode);

    private static SessionRow ToRow(Session session) => new()
    {
        Id = session.Id.Value,
        ProjectId = session.ProjectId.Value,
        JobId = session.JobId.Value,
        CreatedAt = session.CreatedAt,
        ExpiresAt = session.ExpiresAt,
        Status = session.Status,
        StatusChangedAt = session.StatusChangedAt ?? session.CreatedAt,
        FailureCode = session.FailureCode
    };
}
