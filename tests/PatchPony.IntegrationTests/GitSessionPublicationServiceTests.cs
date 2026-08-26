using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Git;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class GitSessionPublicationServiceTests
{
    [Fact]
    public async Task Commit_UsesOnlyTheControlledWorktreeBranchHooksAndTemplate()
    {
        var root = Path.Combine(Path.GetTempPath(), "patchpony-publication", Guid.NewGuid().ToString("N"));
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "patchpony", "PatchPony", now).Value!;
        var job = Job.Create(JobId.New(), project.Id, "config.patch", now).Value!;
        var session = Session.Create(SessionId.New(), project.Id, job.Id, now, now.AddHours(1)).Value!.Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!.Transition(SessionStatus.Active, now.AddMinutes(2)).Value!;
        var layouts = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
        var layout = layouts.Resolve(session.Id);
        Directory.CreateDirectory(layout.WorktreeRoot); Directory.CreateDirectory(layout.DisabledHooksPath);
        try
        {
            var template = GitPublicationTemplate.Create(project, job, session).Value!;
            var git = new RecordingGit(template.BranchName);
            var result = await new GitSessionPublicationService(layouts, git).CommitAsync(session, template);
            Assert.True(result.IsSuccess);
            Assert.Contains(git.Commands, command => command.Arguments.SequenceEqual(["-C", layout.WorktreeRoot, "add", "--all"]));
            var commit = git.Commands.Last();
            Assert.Equal(["-c", $"core.hooksPath={layout.DisabledHooksPath}", "-C", layout.WorktreeRoot, "commit", "--no-verify", "-m", template.CommitMessage], commit.Arguments);
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public async Task Push_UsesOnlyRegisteredOriginAndTheServerSessionRefWithoutForce()
    {
        var root = Path.Combine(Path.GetTempPath(), "patchpony-publication", Guid.NewGuid().ToString("N"));
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "patchpony", "PatchPony", now).Value!;
        var job = Job.Create(JobId.New(), project.Id, "config.patch", now).Value!;
        var session = Session.Create(SessionId.New(), project.Id, job.Id, now, now.AddHours(1)).Value!.Transition(SessionStatus.Provisioning, now.AddMinutes(1)).Value!.Transition(SessionStatus.Active, now.AddMinutes(2)).Value!;
        var layouts = new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage"));
        var layout = layouts.Resolve(session.Id); Directory.CreateDirectory(layout.WorktreeRoot); Directory.CreateDirectory(layout.DisabledHooksPath);
        try
        {
            var template = GitPublicationTemplate.Create(project, job, session).Value!;
            var git = new RecordingGit(template.BranchName, "https://github.com/Edakuban/PatchPony.git");
            var repository = RepositoryRegistration.Create(project.Id, new Uri("https://github.com/Edakuban/PatchPony.git"), "main").Value!;
            var result = await new GitSessionPublicationService(layouts, git).PushAsync(session, repository, template);
            Assert.True(result.IsSuccess);
            var push = git.Commands.Last();
            Assert.Equal(["-c", $"core.hooksPath={layout.DisabledHooksPath}", "-C", layout.WorktreeRoot, "push", "--porcelain", "--no-verify", "origin", $"refs/heads/{template.BranchName}:refs/heads/{template.BranchName}"], push.Arguments);
            Assert.DoesNotContain("--force", push.Arguments);
        }
        finally { Directory.Delete(root, true); }
    }
    private sealed class RecordingGit(string branch, string remote = "") : IGitCommandRunner
    {
        public List<GitCommand> Commands { get; } = [];
        public Task<GitCommandResult> RunAsync(GitCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var output = command.Arguments.Contains("branch") ? branch + "\n" : command.Arguments.Contains("status") ? " M appsettings.json\0" : command.Arguments.Contains("remote") ? remote + "\n" : string.Empty;
            return Task.FromResult(new GitCommandResult(0, output, string.Empty));
        }
    }
}