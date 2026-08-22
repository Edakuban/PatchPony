using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class JobLifecycleTests
{
    [Fact]
    public void TransitionTo_AllowsTheDeclaredLifecycle()
    {
        var job = CreateJob();
        var now = DateTimeOffset.UtcNow;

        var transitions = new[]
        {
            JobStatus.Triaging,
            JobStatus.Ready,
            JobStatus.Planning,
            JobStatus.Planned,
            JobStatus.Implementing,
            JobStatus.Validating,
            JobStatus.ReviewReady,
            JobStatus.Committed,
            JobStatus.MergeRequestCreated,
            JobStatus.Closed
        };

        foreach (var target in transitions)
        {
            var result = job.TransitionTo(target, now);

            Assert.True(result.IsSuccess, result.Error.Message);
        }

        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(transitions.Length, job.StatusChanges.Count);
    }

    [Fact]
    public void TransitionTo_RejectsAnUndeclaredTransition()
    {
        var job = CreateJob();

        var result = job.TransitionTo(JobStatus.Implementing, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("job.transition.invalid", result.Error.Code);
        Assert.Empty(job.StatusChanges);
    }

    [Fact]
    public void TransitionTo_RejectsTransitionsFromTerminalStatus()
    {
        var job = CreateJob();
        var now = DateTimeOffset.UtcNow;
        Assert.True(job.Fail(now).IsSuccess);

        var result = job.TransitionTo(JobStatus.Triaging, now.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("job.transition.invalid", result.Error.Code);
        Assert.Equal(JobStatus.Failed, job.Status);
    }

    [Fact]
    public void Create_RejectsAnEmptyProjectIdentifier()
    {
        var result = Job.Create(JobId.New(), default, "ticket-triage", DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid", result.Error.Code);
    }

    private static Job CreateJob()
    {
        var result = Job.Create(JobId.New(), ProjectId.New(), "ticket-triage", DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess, result.Error.Message);
        return result.Value!;
    }
}
