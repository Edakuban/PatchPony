using System.Text;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeVaultServiceTests
{
    private const string CommitId = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void List_ReturnsBoundedVaultTreeWithItsRevision()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "guide.md", "# Guide");
            WriteText(root, "notes/ideas.md", "# Ideas");
            var service = new KnowledgeVaultService(Path.GetDirectoryName(root)!);

            var result = service.List(checkout);

            Assert.True(result.IsSuccess);
            Assert.Equal(CommitId, result.Value!.Revision.CommitId);
            Assert.Contains(result.Value.Entries, entry => entry is { RelativePath: "guide.md", Kind: KnowledgeVaultTreeEntryKind.File });
            Assert.Contains(result.Value.Entries, entry => entry is { RelativePath: "notes", Kind: KnowledgeVaultTreeEntryKind.Directory });
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    [Fact]
    public void ReadMarkdown_RejectsNonMarkdownAndLimitsLines()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "many.md", string.Join('\n', Enumerable.Range(1, 501).Select(number => $"line {number}")));
            WriteText(root, "script.js", "alert('no');");
            var service = new KnowledgeVaultService(Path.GetDirectoryName(root)!);

            var read = service.ReadMarkdown(checkout, "many.md");
            var nonMarkdown = service.ReadMarkdown(checkout, "script.js");

            Assert.True(read.IsSuccess);
            Assert.Equal(500, read.Value!.Lines.Count);
            Assert.True(read.Value.IsTruncated);
            Assert.Equal((500, "line 500"), (read.Value.Lines[^1].Number, read.Value.Lines[^1].Text));
            Assert.Equal("vault.not_markdown", nonMarkdown.Error.Code);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    [Fact]
    public void Search_UsesCaseInsensitiveLiteralMatchingWithPerFileLimit()
    {
        var root = CreateRoot(out var checkout);
        try
        {
            WriteText(root, "matches.md", string.Join('\n', Enumerable.Repeat("Needle a.*", 25)));
            WriteText(root, "ignored.txt", "Needle a.*");
            var service = new KnowledgeVaultService(Path.GetDirectoryName(root)!);

            var result = service.Search(checkout, "needle a.*");

            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value!.Matches.Count);
            Assert.All(result.Value.Matches, match => Assert.Equal("matches.md", match.RelativePath));
            Assert.True(result.Value.IsTruncated);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

    [Theory]
    [InlineData("../guide.md")]
    [InlineData("guide.txt")]
    public void ReadMarkdown_RejectsUnsafeOrUnsupportedPaths(string path)
    {
        var root = CreateRoot(out var checkout);
        try
        {
            var result = new KnowledgeVaultService(Path.GetDirectoryName(root)!).ReadMarkdown(checkout, path);

            Assert.False(result.IsSuccess);
            Assert.True(new[] { "path.invalid", "vault.not_markdown" }.Contains(result.Error.Code));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(root)!, recursive: true);
        }
    }

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
