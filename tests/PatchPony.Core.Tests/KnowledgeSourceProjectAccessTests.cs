using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class KnowledgeSourceProjectAccessTests
{
    [Fact]
    public void Authorize_EnforcesSeparateVaultPathsPerProject()
    {
        var source = KnowledgeSourceId.New();
        var project = ProjectId.New();
        var access = KnowledgeSourceProjectAccess.Create(source, project, ["teams/patchpony/**", "shared/*.md"], ["teams/patchpony/drafts/**"]).Value!;

        Assert.True(access.Authorize("teams/patchpony/runbook.md", KnowledgePathAccess.Read).IsSuccess);
        Assert.True(access.Authorize("teams/patchpony/drafts/proposal.md", KnowledgePathAccess.Write).IsSuccess);
        Assert.Equal("knowledge.path.not_readable", access.Authorize("teams/vocavid/notes.md", KnowledgePathAccess.Read).Error.Code);
        Assert.Equal("knowledge.path.not_writable", access.Authorize("shared/guide.md", KnowledgePathAccess.Write).Error.Code);
    }

    [Fact]
    public void Catalog_FailsClosedForMissingPoliciesAndUnsafePaths()
    {
        var source = KnowledgeSourceId.New();
        var project = ProjectId.New();
        Assert.False(KnowledgeSourceProjectAccess.Create(source, project, ["../private/**"], []).IsSuccess);
        var catalog = new KnowledgeSourceAccessCatalog([]);

        Assert.Equal("knowledge.access_policy.missing", catalog.Authorize(source, project, "guide.md", KnowledgePathAccess.Read).Error.Code);
    }
}