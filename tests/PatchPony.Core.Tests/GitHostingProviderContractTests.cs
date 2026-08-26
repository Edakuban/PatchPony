using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class GitHostingProviderContractTests
{
    [Fact]
    public void MergeRequestDraft_RequiresBoundedServerOwnedPublicationFields()
    {
        var valid = new GitMergeRequestDraft(ProjectId.New(), JobId.New(), SessionId.New(), new Uri("https://github.com/edakuban/patchpony"), "patchpony/session/abc", "main", "Patch config", "Validated change.");
        var invalid = valid with { SourceBranch = "main" };
        Assert.True(valid.Validate().IsSuccess);
        Assert.False(invalid.Validate().IsSuccess);
    }
}