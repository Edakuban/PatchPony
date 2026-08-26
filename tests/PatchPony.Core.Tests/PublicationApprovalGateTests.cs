using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class PublicationApprovalGateTests
{
    [Fact]
    public async Task EnsureApproved_FailsClosedUntilTheLatestJobApprovalIsApproved()
    {
        var project = ProjectId.New(); var job = JobId.New();
        var approvals = new FixedApprovals(null);
        var gate = new PublicationApprovalGate([new ProjectPublicationApprovalPolicy(project, true, true)], approvals);
        Assert.False((await gate.EnsureApprovedAsync(project, job, PublicationAction.Push)).IsSuccess);
        approvals.Value = new Approval(Guid.NewGuid(), job, ApprovalDecision.Approved, "reviewer", DateTimeOffset.UtcNow);
        Assert.True((await gate.EnsureApprovedAsync(project, job, PublicationAction.MergeRequest)).IsSuccess);
    }
    private sealed class FixedApprovals(Approval? value) : IApprovalRepository
    {
        public Approval? Value { get; set; } = value;
        public Task<Approval?> GetLatestAsync(JobId jobId, CancellationToken cancellationToken = default) => Task.FromResult(Value);
    }
}