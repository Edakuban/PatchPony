using System.Text;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeVaultLinkServiceTests
{
    private const string CommitId = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void ResolveLinks_ResolvesWikiAndMarkdownLinksWithoutFollowingExternalUrls()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "Guide.md", "# Guide");
            WriteText(root, "notes/linked.md", "[[Guide#intro|The Guide]]\n[Guide](../Guide.md#install)\n[Web](https://example.test/docs)\n![Image](image.png)");
            var links = CreateService(root).ResolveLinks(checkout, "notes/linked.md");

            Assert.True(links.IsSuccess);
            Assert.Equal(CommitId, links.Value!.Revision.CommitId);
            Assert.Collection(links.Value.Links,
                link => Assert.Equal((KnowledgeVaultLinkKind.Wiki, "Guide.md", "intro", false), (link.Kind, link.ResolvedPath, link.Fragment, link.IsExternal)),
                link => Assert.Equal((KnowledgeVaultLinkKind.Markdown, "Guide.md", "install", false), (link.Kind, link.ResolvedPath, link.Fragment, link.IsExternal)),
                link => Assert.Equal((KnowledgeVaultLinkKind.Markdown, null, null, true), (link.Kind, link.ResolvedPath, link.Fragment, link.IsExternal)));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    [Fact]
    public void FindBacklinks_ReturnsAllInternalReferencesToTarget()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "Guide.md", "# Guide");
            WriteText(root, "a.md", "[[Guide]]");
            WriteText(root, "notes/b.md", "[Guide](../Guide.md#install)");
            WriteText(root, "external.md", "[Elsewhere](https://example.test/Guide.md)");
            var result = CreateService(root).FindBacklinks(checkout, "Guide");

            Assert.True(result.IsSuccess);
            Assert.Equal("Guide.md", result.Value!.TargetPath);
            Assert.Equal([("a.md", 1, KnowledgeVaultLinkKind.Wiki, (string?)null), ("notes/b.md", 1, KnowledgeVaultLinkKind.Markdown, "install")], result.Value.Backlinks.Select(link => (link.SourcePath, link.LineNumber, link.Kind, link.Fragment)));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    [Fact]
    public void ResolveLinks_LeavesUnsafeOrUnknownInternalTargetsUnresolved()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "source.md", "[Escape](../../outside.md)\n[[Unknown]]");
            var result = CreateService(root).ResolveLinks(checkout, "source.md");

            Assert.True(result.IsSuccess);
            Assert.All(result.Value!.Links, link => Assert.Null(link.ResolvedPath));
            Assert.All(result.Value.Links, link => Assert.False(link.IsExternal));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    private static KnowledgeVaultLinkService CreateService(string root) => new(new KnowledgeVaultService(Path.GetDirectoryName(root)!));

    private static string CreateRoot(out KnowledgeSourceCheckout checkout)
    {
        var storage = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        var sourceId = KnowledgeSourceId.New();
        var root = Path.Combine(storage, sourceId.Value.ToString("N"));
        Directory.CreateDirectory(root);
        checkout = new KnowledgeSourceCheckout(sourceId, RepositoryRevision.Create(CommitId).Value!, Created: true);
        return root;
    }

    private static void WriteText(string root, string relativePath, string text)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
