using PatchPony.Core.Common;
using PatchPony.Core.Application;
using PatchPony.Core.Audit;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task Append_PersistsAValidEventWithoutExposingMutationOperations()
    {
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;
        var writer = new InMemoryAuditWriter();
        var correlations = new CorrelationContext();
        var service = new AuditService(new FixedProjectRepository(project), new EmptyJobRepository(), writer, correlations);
        using var scope = correlations.BeginScope(CorrelationId.Create("correlation-42").Value!);

        var result = await service.AppendAsync(project.Id, null, "job.created", DateTimeOffset.UtcNow, "{\"source\":\"test\"}");

        Assert.True(result.IsSuccess);
        Assert.Single(writer.Events);
        Assert.Equal("job.created", writer.Events[0].EventType);
    }

    [Fact]
    public async Task Append_RejectsInvalidMetadataJson()
    {
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;
        var service = new AuditService(new FixedProjectRepository(project), new EmptyJobRepository(), new InMemoryAuditWriter(), new CorrelationContext());

        var result = await service.AppendAsync(project.Id, null, "job.created", DateTimeOffset.UtcNow, "not-json");

        Assert.False(result.IsSuccess);
        Assert.Equal("audit.metadata.invalid", result.Error.Code);
    }

    private sealed class FixedProjectRepository(Project project) : IProjectRepository
    {
        public Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default) => Task.FromResult(id == project.Id ? project : null);

        public Task<RepositoryRegistration?> GetRepositoryAsync(ProjectId projectId, CancellationToken cancellationToken = default) => Task.FromResult<RepositoryRegistration?>(null);

        public Task AddAsync(Project project, RepositoryRegistration? repository, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyJobRepository : IJobRepository
    {
        public Task<Job?> GetAsync(JobId id, CancellationToken cancellationToken = default) => Task.FromResult<Job?>(null);

        public Task AddAsync(Job job, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Job job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryAuditWriter : IAuditEventWriter
    {
        public List<AuditEvent> Events { get; } = [];

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
