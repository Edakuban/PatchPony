using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeLinkImpactAnalyzerTests
{
    [Fact]
    public void AnalyzeRename_ListsEveryInternalBacklinkThatNeedsUpdating()
    {
        var pages = new[]
        {
            new KnowledgeVaultPage("Guide.md", "# Guide\n"),
            new KnowledgeVaultPage("notes/one.md", "[[Guide#install]]\n"),
            new KnowledgeVaultPage("notes/two.md", "[Guide](../Guide.md)\n[External](https://example.test/Guide.md)\n")
        };

        var result = new KnowledgeLinkImpactAnalyzer().AnalyzeRename(pages, "Guide.md", "Handbook.md");

        Assert.True(result.IsSuccess);
        Assert.Equal([("notes/one.md", 1, "install"), ("notes/two.md", 1, (string?)null)], result.Value!.BacklinksRequiringUpdate.Select(link => (link.SourcePath, link.Line, link.Fragment)));
    }

    [Fact]
    public void AnalyzeChange_ReportsOutgoingDeltaAndBrokenIncomingHeadingFragments()
    {
        var pages = new[]
        {
            new KnowledgeVaultPage("Guide.md", "# Install\nOld\n# Keep\n"),
            new KnowledgeVaultPage("index.md", "[[Guide#install]]\n[[Guide#keep]]\n")
        };

        var result = new KnowledgeLinkImpactAnalyzer().AnalyzeChange(pages, "Guide.md", "# Setup\nNew\n# Keep\n[[index]]\n");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AddedLinks);
        Assert.Equal("index.md", result.Value.AddedLinks[0].TargetPath);
        Assert.Equal(2, result.Value.IncomingBacklinks.Count);
        Assert.Single(result.Value.BrokenFragmentBacklinks);
        Assert.Equal("install", result.Value.BrokenFragmentBacklinks[0].Fragment);
    }

    [Fact]
    public void AnalyzeRename_FailsClosedForInvalidCatalogOrTarget()
    {
        var pages = new[] { new KnowledgeVaultPage("Guide.md", "# Guide") };

        var result = new KnowledgeLinkImpactAnalyzer().AnalyzeRename(pages, "Guide.md", ".obsidian/plugins/a.md");

        Assert.False(result.IsSuccess);
        Assert.Equal("knowledge.content.protected_path", result.Error.Code);
    }
}