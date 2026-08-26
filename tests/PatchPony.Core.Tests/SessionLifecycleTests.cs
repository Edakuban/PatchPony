using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class SessionLifecycleTests
{
    [Fact]
    public void Session_AllowsTheProvisioningActiveClosingLifecycle()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), createdAt, createdAt.AddHours(1)).Value!;

        var provisioning = session.Transition(SessionStatus.Provisioning, createdAt.AddMinutes(1));
        var active = provisioning.Value!.Transition(SessionStatus.Active, createdAt.AddMinutes(2));
        var closing = active.Value!.Transition(SessionStatus.Closing, createdAt.AddMinutes(3));
        var closed = closing.Value!.Transition(SessionStatus.Closed, createdAt.AddMinutes(4));

        Assert.True(provisioning.IsSuccess);
        Assert.True(active.IsSuccess);
        Assert.True(closing.IsSuccess);
        Assert.True(closed.IsSuccess);
        Assert.Equal(SessionStatus.Closed, closed.Value!.Status);
        Assert.True(closed.Value.IsTerminal);
    }

    [Fact]
    public void Session_RejectsDurationsOverTheServerMaximum()
    {
        var createdAt = DateTimeOffset.UtcNow;

        var result = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), createdAt, createdAt.Add(SessionLimits.MaximumDuration).AddTicks(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("session.duration.exceeded", result.Error.Code);
    }
    [Fact]
    public void Session_RejectsInvalidOrRepeatedTransitions()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), createdAt, createdAt.AddHours(1)).Value!;

        var repeated = session.Transition(SessionStatus.Created, createdAt.AddMinutes(1));
        var invalid = session.Transition(SessionStatus.Active, createdAt.AddMinutes(1));

        Assert.False(repeated.IsSuccess);
        Assert.Equal("session.transition.noop", repeated.Error.Code);
        Assert.False(invalid.IsSuccess);
        Assert.Equal("session.transition.invalid", invalid.Error.Code);
    }

    [Fact]
    public void Session_ExpiresOnlyAfterItsConfiguredExpiryAndFailsWithSafeCode()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(1);
        var session = Session.Create(SessionId.New(), ProjectId.New(), JobId.New(), createdAt, expiresAt).Value!;
        var active = session.Transition(SessionStatus.Provisioning, createdAt.AddMinutes(1)).Value!
            .Transition(SessionStatus.Active, createdAt.AddMinutes(2)).Value!;

        var earlyExpiry = active.Transition(SessionStatus.Expired, expiresAt.AddTicks(-1));
        var failed = active.Transition(SessionStatus.Failed, createdAt.AddMinutes(3), "worktree.create_failed");
        var unsafeFailure = active.Transition(SessionStatus.Failed, createdAt.AddMinutes(3), "host path C:\\secret");

        Assert.False(earlyExpiry.IsSuccess);
        Assert.Equal("session.expiry.not_reached", earlyExpiry.Error.Code);
        Assert.True(failed.IsSuccess);
        Assert.Equal("worktree.create_failed", failed.Value!.FailureCode);
        Assert.False(unsafeFailure.IsSuccess);
        Assert.Equal("validation.invalid", unsafeFailure.Error.Code);
    }
}