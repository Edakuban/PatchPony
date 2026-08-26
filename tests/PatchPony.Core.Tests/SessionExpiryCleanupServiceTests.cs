using PatchPony.Core.Application;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class SessionExpiryCleanupServiceTests
{
    [Fact]
    public async Task Run_ExpiresDiscardsAndClosesDueActiveSessions()
    {
        var now = DateTimeOffset.UtcNow;
        var session = CreateActiveSession(now.AddHours(-2), now.AddHours(-1));
        var repository = new InMemorySessionRepository(session);
        var discarder = new RecordingDiscarder();
        var service = new SessionExpiryCleanupService(repository, discarder, new FixedCheckoutResolver(Path.GetTempPath()));

        var result = await service.RunAsync(now, 10);

        Assert.Equal(1, result.Examined);
        Assert.Equal([session.Id], result.CleanedSessionIds);
        Assert.Empty(result.Failures);
        Assert.Equal(SessionStatus.Closing, discarder.Discarded!.Status);
        Assert.Equal(SessionStatus.Closed, repository.Sessions[session.Id].Status);
    }

    [Fact]
    public async Task Run_LeavesTheSessionClosingForARetryWhenDiscardFails()
    {
        var now = DateTimeOffset.UtcNow;
        var session = CreateActiveSession(now.AddHours(-2), now.AddHours(-1));
        var repository = new InMemorySessionRepository(session);
        var service = new SessionExpiryCleanupService(repository, new RecordingDiscarder(Result.Failure(new DomainError("session.discard.failed", "failed"))), new FixedCheckoutResolver(Path.GetTempPath()));

        var result = await service.RunAsync(now, 10);

        Assert.Empty(result.CleanedSessionIds);
        Assert.Equal("session.discard.failed", Assert.Single(result.Failures).ErrorCode);
        Assert.Equal(SessionStatus.Closing, repository.Sessions[session.Id].Status);
    }

    [Fact]
    public async Task Recovery_ClosesAnInterruptedProvisioningSessionAfterDiscard()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now.AddHours(-1), now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(-1)).Value!;
        var repository = new InMemorySessionRepository(session);
        var discarder = new RecordingDiscarder();
        var service = new SessionCrashRecoveryService(repository, discarder, new FixedCheckoutResolver(Path.GetTempPath()));

        var result = await service.RunAsync(now, 10);

        Assert.Equal([session.Id], result.RecoveredSessionIds);
        Assert.Empty(result.Failures);
        Assert.Equal(SessionStatus.Closing, discarder.Discarded!.Status);
        Assert.Equal(SessionStatus.Closed, repository.Sessions[session.Id].Status);
    }

    [Fact]
    public async Task Recovery_LeavesClosingSessionsForARetryWhenDiscardFails()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), now.AddHours(-1), now.AddHours(1)).Value!
            .Transition(SessionStatus.Provisioning, now.AddMinutes(-2)).Value!
            .Transition(SessionStatus.Closing, now.AddMinutes(-1)).Value!;
        var repository = new InMemorySessionRepository(session);
        var service = new SessionCrashRecoveryService(repository, new RecordingDiscarder(Result.Failure(new DomainError("session.discard.failed", "failed"))), new FixedCheckoutResolver(Path.GetTempPath()));

        var result = await service.RunAsync(now, 10);

        Assert.Empty(result.RecoveredSessionIds);
        Assert.Equal("session.discard.failed", Assert.Single(result.Failures).ErrorCode);
        Assert.Equal(SessionStatus.Closing, repository.Sessions[session.Id].Status);
    }
    private static Session CreateActiveSession(DateTimeOffset createdAt, DateTimeOffset expiresAt) =>
        Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), createdAt, expiresAt).Value!
            .Transition(SessionStatus.Provisioning, createdAt.AddMinutes(1)).Value!
            .Transition(SessionStatus.Active, createdAt.AddMinutes(2)).Value!;

    private sealed class InMemorySessionRepository(params Session[] sessions) : ISessionRepository
    {
        public Dictionary<SessionId, Session> Sessions { get; } = sessions.ToDictionary(session => session.Id);

        public Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default) => Task.FromResult(Sessions.GetValueOrDefault(id));
        public Task AddAsync(Session session, CancellationToken cancellationToken = default) { Sessions[session.Id] = session; return Task.CompletedTask; }
        public Task UpdateAsync(Session session, CancellationToken cancellationToken = default) { Sessions[session.Id] = session; return Task.CompletedTask; }
        public Task<IReadOnlyList<Session>> GetDueForCleanupAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Session>>(Sessions.Values.Where(session => session.ExpiresAt <= now && session.Status != SessionStatus.Closed).Take(maximumCount).ToList());
        public Task<IReadOnlyList<Session>> GetForCrashRecoveryAsync(int maximumCount, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Session>>(Sessions.Values.Where(session => session.Status is SessionStatus.Provisioning or SessionStatus.Closing).Take(maximumCount).ToList());
    }

    private sealed class RecordingDiscarder(Result? outcome = null) : ISessionWorkspaceDiscarder
    {
        public Session? Discarded { get; private set; }

        public Task<Result> DiscardAsync(Session session, string baseCheckoutRoot, CancellationToken cancellationToken = default)
        {
            Discarded = session;
            return Task.FromResult(outcome ?? Result.Success());
        }
    }

    private sealed class FixedCheckoutResolver(string path) : ISessionBaseCheckoutResolver
    {
        public Result<string> Resolve(ProjectId projectId) => Result<string>.Success(path);
    }
}