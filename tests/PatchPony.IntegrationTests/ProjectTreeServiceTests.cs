using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ProjectTreeServiceTests
{
    [Fact]
    public void List_ReturnsOnlyAllowedEntriesAndMarksOversizedFiles()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteFile(root, "src/Program.cs", "class Program { }");
            WriteFile(root, "src/nested/Feature.cs", "class Feature { }");
            WriteFile(root, ".env", "secret");
            WriteFile(root, "large.bin", new string('x', 101));
            var tree = CreateTree(root, maximumFileBytes: 100);

            var result = tree.List();

            Assert.True(result.IsSuccess);
            Assert.Contains(result.Value!.Entries, entry => entry is { RelativePath: "src", Kind: ProjectTreeEntryKind.Directory });
            Assert.Contains(result.Value.Entries, entry => entry is { RelativePath: "src/Program.cs", Kind: ProjectTreeEntryKind.File, SizeBytes: 17 });
            Assert.Contains(result.Value.Entries, entry => entry is { RelativePath: "src/nested/Feature.cs", Kind: ProjectTreeEntryKind.File });
            Assert.DoesNotContain(result.Value.Entries, entry => entry.RelativePath == ".env");
            Assert.DoesNotContain(result.Value.Entries, entry => entry.RelativePath == "large.bin");
            Assert.True(result.Value.HasOversizedEntries);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void List_StopsAtTheConfiguredDepth()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteFile(root, "src/nested/Feature.cs", "class Feature { }");
            var tree = CreateTree(root, maximumDepth: 1);

            var result = tree.List();

            Assert.True(result.IsSuccess);
            Assert.Contains(result.Value!.Entries, entry => entry.RelativePath == "src");
            Assert.DoesNotContain(result.Value.Entries, entry => entry.RelativePath == "src/nested");
            Assert.True(result.Value.IsTruncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void List_StopsAtTheConfiguredEntryCount()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteFile(root, "one.txt", "1");
            WriteFile(root, "two.txt", "2");
            WriteFile(root, "three.txt", "3");
            var tree = CreateTree(root, maximumEntries: 2);

            var result = tree.List();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Entries.Count);
            Assert.True(result.Value.IsTruncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ProjectTreeService CreateTree(
        string root,
        int maximumDepth = 5,
        int maximumEntries = 500,
        long maximumFileBytes = 1_048_576)
    {
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(["**"], [], [".env", "**/.env"])).Value!;
        var resolver = new ProjectPathResolver(root);
        var access = new ProjectPathAccessService(resolver, policy);
        return new ProjectTreeService(resolver, access, maximumDepth, maximumEntries, maximumFileBytes);
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
