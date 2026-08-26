using PatchPony.Core.Knowledge;
using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgePatchServiceTests
{
    [Fact]
    public void Propose_AppliesBoundedSemanticFrontmatterAndSectionOperations()
    {
        var source = "---\ntitle: Before\n---\n# Install\nOld instructions\n## Details\nReplaced with its parent section\n# Support\nKeep this\n";
        var request = new KnowledgePatchRequest("guides/install.md", KnowledgePatchService.Sha256(source),
        [
            new SetKnowledgeFrontmatterField("title", "Installation"),
            new ReplaceKnowledgeMarkdownSection("# Install", "New instructions"),
            new AppendKnowledgeMarkdown("See [[Support]].")
        ]);

        var result = new KnowledgePatchService().Propose(source, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("guides/install.md", result.Value!.Path);
        Assert.Contains("title: \"Installation\"", result.Value.Content, StringComparison.Ordinal);
        Assert.Contains("# Install\nNew instructions\n# Support\nKeep this", result.Value.Content, StringComparison.Ordinal);
        Assert.Contains("See [[Support]].", result.Value.Content, StringComparison.Ordinal);
        Assert.Equal(["frontmatter.set", "section.replace", "markdown.append"], result.Value.AppliedOperations);
    }

    [Fact]
    public void Propose_RejectsStaleContentAndProtectedPaths()
    {
        var source = "# Guide\n";
        var stale = new KnowledgePatchRequest("guide.md", new string('0', 64), [new AppendKnowledgeMarkdown("More")]);
        var protectedPath = new KnowledgePatchRequest(".obsidian/plugins/example/readme.md", KnowledgePatchService.Sha256(source), [new AppendKnowledgeMarkdown("More")]);

        var staleResult = new KnowledgePatchService().Propose(source, stale);
        var protectedResult = new KnowledgePatchService().Propose(source, protectedPath);

        Assert.Equal("knowledge.patch.source_hash_mismatch", staleResult.Error.Code);
        Assert.Equal("knowledge.content.protected_path", protectedResult.Error.Code);
    }

    [Fact]
    public void Propose_RejectsInvalidResultingFrontmatterAndMissingSections()
    {
        var unsafeFrontmatter = "---\nbase: &base value\ncopy: *base\n---\n# Guide\n";
        var invalid = new KnowledgePatchRequest("guide.md", KnowledgePatchService.Sha256(unsafeFrontmatter), [new AppendKnowledgeMarkdown("More")]);
        var missing = new KnowledgePatchRequest("guide.md", KnowledgePatchService.Sha256("# Guide\n"), [new ReplaceKnowledgeMarkdownSection("# Missing", "New")]);

        var invalidResult = new KnowledgePatchService().Propose(unsafeFrontmatter, invalid);
        var missingResult = new KnowledgePatchService().Propose("# Guide\n", missing);

        Assert.Equal("knowledge.patch.frontmatter_invalid", invalidResult.Error.Code);
        Assert.Equal("knowledge.patch.section_missing", missingResult.Error.Code);
    }
}