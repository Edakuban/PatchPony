using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class GitPublicationTemplateTests
{
    [Fact]
    public void Create_UsesOnlyServerOwnedIdentifiersAndTheExistingSessionBranch()
    {
        var project = Project.Create(ProjectId.New(), "patchpony", "PatchPony", DateTimeOffset.UtcNow).Value!;
        var job = Job.Create(JobId.New(), project.Id, "config.patch", DateTimeOffset.UtcNow).Value!;
        var session = Session.Create(SessionId.New(), project.Id, job.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)).Value!;

        var template = GitPublicationTemplate.Create(project, job, session);

        Assert.True(template.IsSuccess);
        Assert.Equal($"patchpony/session/{session.Id.Value:N}", template.Value!.BranchName);
        Assert.Contains($"PatchPony-Job: {job.Id.Value:N}", template.Value.CommitMessage, StringComparison.Ordinal);
        Assert.Contains($"PatchPony-Session: {session.Id.Value:N}", template.Value.CommitMessage, StringComparison.Ordinal);
    }
}