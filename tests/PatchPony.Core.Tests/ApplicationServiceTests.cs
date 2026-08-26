using PatchPony.Core.Application;
using PatchPony.Core.Common;
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

    [Fact]
    public async Task DescribeSession_ReturnsThePersistedSessionAndUsesANotFoundError()
    {
        var projects = new InMemoryProjectRepository();
        var jobs = new InMemoryJobRepository();
        var sessions = new InMemorySessionRepository();
        var service = new SessionApplicationService(projects, jobs, sessions);
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)).Value!;
        sessions.Sessions[session.Id] = session;

        var found = await service.DescribeAsync(session.Id);
        var missing = await service.DescribeAsync(SessionId.New());

        Assert.True(found.IsSuccess);
        Assert.Equal(session, found.Value);
        Assert.False(missing.IsSuccess);
        Assert.Equal("session.not_found", missing.Error.Code);
    }
    [Fact]
    public async Task DiscardSession_ClosesOnlyAfterTheWorkspaceWasDiscarded()
    {
        var projects = new InMemoryProjectRepository();
        var jobs = new InMemoryJobRepository();
        var sessions = new InMemorySessionRepository();
        var discarder = new RecordingDiscarder();
        var service = new SessionApplicationService(projects, jobs, sessions, discarder);
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now, now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!
            .Transition(SessionStatus.Active, now.AddMinutes(2)).Value!;
        sessions.Sessions[session.Id] = session;

        var result = await service.DiscardAsync(session.Id, Path.GetTempPath(), now.AddMinutes(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(SessionStatus.Closed, result.Value!.Status);
        Assert.Equal(SessionStatus.Closing, discarder.Discarded!.Status);
        Assert.Equal(SessionStatus.Closed, sessions.Sessions[session.Id].Status);
    }
    [Fact]
    public async Task DiscardSession_AllowsAFailedSessionToBeCleanedUp()
    {
        var projects = new InMemoryProjectRepository();
        var jobs = new InMemoryJobRepository();
        var sessions = new InMemorySessionRepository();
        var service = new SessionApplicationService(projects, jobs, sessions, new RecordingDiscarder());
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now, now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!
            .Transition(SessionStatus.Failed, now.AddMinutes(2), "worktree.create_failed").Value!;
        sessions.Sessions[session.Id] = session;

        var result = await service.DiscardAsync(session.Id, Path.GetTempPath(), now.AddMinutes(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(SessionStatus.Closed, result.Value!.Status);
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

    private sealed class RecordingDiscarder : ISessionWorkspaceDiscarder
    {
        public Session? Discarded { get; private set; }

        public Task<Result> DiscardAsync(Session session, string baseCheckoutRoot, CancellationToken cancellationToken = default)
        {
            Discarded = session;
            return Task.FromResult(Result.Success());
        }
    }
    private sealed class InMemorySessionRepository : ISessionRepository
    {
        public Dictionary<SessionId, Session> Sessions { get; } = [];

        public Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Sessions.GetValueOrDefault(id));

        public Task AddAsync(Session session, CancellationToken cancellationToken = default)
        {
            Sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
        {
            Sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
