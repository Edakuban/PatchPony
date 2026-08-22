using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;
using PatchPony.Infrastructure.Source;

namespace PatchPony.IntegrationTests;

public sealed class RipgrepSourceSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_UsesLiteralSearchAndReturnsOnlyPolicyAllowedFiles()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteFile(root, "src/Allowed.cs", "first line\nneedle a.*\nlast line\n");
            WriteFile(root, "docs/Denied.md", "needle aZ\n");
            WriteFile(root, "src/.env", "needle a.*\n");
            var search = CreateSearch(root);

            var result = await search.SearchAsync("needle a.*");

            Assert.True(result.IsSuccess);
            var match = Assert.Single(result.Value!.Matches);
            Assert.Equal("src/Allowed.cs", match.RelativePath);
            Assert.Equal(2, match.LineNumber);
            Assert.Equal("needle a.*", match.LineText);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SearchAsync_EnforcesTheFixedPerFileMatchLimit()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteFile(root, "src/Many.cs", string.Join('\n', Enumerable.Repeat("needle", 25)));
            var search = CreateSearch(root);

            var result = await search.SearchAsync("needle");

            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value!.Matches.Count);
            Assert.True(result.Value.IsTruncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SearchAsync_RejectsAnEmptyOrOversizedQuery()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var search = CreateSearch(root);

            var empty = await search.SearchAsync(" ");
            var oversized = await search.SearchAsync(new string('x', 257));

            Assert.Equal("search.invalid_query", empty.Error.Code);
            Assert.Equal("search.invalid_query", oversized.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static RipgrepSourceSearchService CreateSearch(string root)
    {
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(
            ["src/**"],
            [],
            [".env", "**/.env", "**/.env.*"])).Value!;
        var resolver = new ProjectPathResolver(root);
        return new RipgrepSourceSearchService(resolver, new ProjectPathAccessService(resolver, policy));
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
