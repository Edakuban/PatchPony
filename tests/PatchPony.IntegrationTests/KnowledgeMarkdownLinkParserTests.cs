using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeMarkdownLinkParserTests
{
    [Fact]
    public void Parse_CanonicalizesRelativeTargetsAndRejectsEscapesOrPlatformPaths()
    {
        var read = new KnowledgeVaultReadResult(
            RepositoryRevision.Create("0123456789abcdef0123456789abcdef01234567").Value!,
            "notes/current.md",
            [new KnowledgeVaultLine(1, "[[../Guide#intro|Guide]] [Child](Child#install) [Self](#here) [Escape](../../outside.md) [Windows](C:\\secret.md) [Query](Guide.md?raw=1)")],
            IsTruncated: false);
        var files = new HashSet<string>(["Guide.md", "notes/Child.md", "notes/current.md"], StringComparer.OrdinalIgnoreCase);

        var links = KnowledgeMarkdownLinkParser.Parse(read, files, 20, out var isTruncated);

        Assert.False(isTruncated);
        Assert.Equal(["Guide.md", "notes/Child.md", "notes/current.md", null, null, null], links.Select(link => link.ResolvedPath));
        Assert.All(links, link => Assert.False(link.IsExternal));
    }

    [Fact]
    public void Parse_LeavesExternalLinksUnresolvedWithoutDereferencingThem()
    {
        var read = new KnowledgeVaultReadResult(
            RepositoryRevision.Create("0123456789abcdef0123456789abcdef01234567").Value!,
            "index.md",
            [new KnowledgeVaultLine(3, "[Web](https://example.test/a#part) [Mail](mailto:help@example.test)")],
            IsTruncated: false);

        var links = KnowledgeMarkdownLinkParser.Parse(read, new HashSet<string>(["index.md"]), 20, out var isTruncated);

        Assert.False(isTruncated);
        Assert.All(links, link =>
        {
            Assert.True(link.IsExternal);
            Assert.Null(link.ResolvedPath);
        });
        Assert.Equal("part", links[0].Fragment);
        Assert.Null(links[1].Fragment);
    }

    [Fact]
    public void Parse_StopsAtTheConfiguredLinkLimit()
    {
        var read = new KnowledgeVaultReadResult(
            RepositoryRevision.Create("0123456789abcdef0123456789abcdef01234567").Value!,
            "index.md",
            [new KnowledgeVaultLine(1, "[[one]] [[two]]")],
            IsTruncated: false);

        var links = KnowledgeMarkdownLinkParser.Parse(read, new HashSet<string>(["index.md", "one.md", "two.md"]), 1, out var isTruncated);

        Assert.True(isTruncated);
        Assert.Single(links);
        Assert.Equal("one.md", links[0].ResolvedPath);
    }
}