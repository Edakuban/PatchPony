using PatchPony.Core.Application;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task CreateAndTransitionJob_PersistsTheAggregateThroughTheRepository()
    {
        var projects = new InMemoryProjectRepository();
        var jobs = new InMemoryJobRepository();
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;
        var projectService = new ProjectApplicationService(projects);
        var jobService = new JobApplicationService(projects, jobs);

        Assert.True((await projectService.RegisterAsync(project, null)).IsSuccess);

        var created = await jobService.CreateAsync(project.Id, "triage", DateTimeOffset.UtcNow);
        var transitioned = await jobService.TransitionAsync(created.Value!.Id, JobStatus.Triaging, DateTimeOffset.UtcNow);

        Assert.True(created.IsSuccess);
        Assert.True(transitioned.IsSuccess);
        Assert.Equal(JobStatus.Triaging, jobs.Jobs[created.Value.Id].Status);
        Assert.Single(jobs.Jobs[created.Value.Id].StatusChanges);
    }

    [Fact]
    public async Task CreateJob_RejectsUnknownProject()
    {
        var service = new JobApplicationService(new InMemoryProjectRepository(), new InMemoryJobRepository());

        var result = await service.CreateAsync(ProjectId.New(), "triage", DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("project.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateSession_RejectsJobFromAnotherProject()
    {
        var projects = new InMemoryProjectRepository();
        var jobs = new InMemoryJobRepository();
        var sessions = new InMemorySessionRepository();
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;
        var otherProjectId = ProjectId.New();
        var job = Job.Create(JobId.New(), otherProjectId, "triage", DateTimeOffset.UtcNow).Value!;
        await projects.AddAsync(project, null);
        await jobs.AddAsync(job);
        var service = new SessionApplicationService(projects, jobs, sessions);

        var result = await service.CreateAsync(project.Id, job.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("job.not_found", result.Error.Code);
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<ProjectId, Project> projects = [];

        public Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(projects.GetValueOrDefault(id));

        public Task<RepositoryRegistration?> GetRepositoryAsync(ProjectId projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<RepositoryRegistration?>(null);

        public Task AddAsync(Project project, RepositoryRegistration? repository, CancellationToken cancellationToken = default)
        {
            projects.Add(project.Id, project);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        public Dictionary<JobId, Job> Jobs { get; } = [];

        public Task<Job?> GetAsync(JobId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Jobs.GetValueOrDefault(id));

        public Task AddAsync(Job job, CancellationToken cancellationToken = default)
        {
            Jobs.Add(job.Id, job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
        {
            Jobs[job.Id] = job;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySessionRepository : ISessionRepository
    {
        public Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Session?>(null);

        public Task AddAsync(Session session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
