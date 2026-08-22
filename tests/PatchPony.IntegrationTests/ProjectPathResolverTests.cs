using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ProjectPathResolverTests
{
    [Fact]
    public void Resolve_CanonicalizesAPathInsideTheCheckout()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var resolver = new ProjectPathResolver(root);

            var result = resolver.Resolve("docs/./guide.md");

            Assert.True(result.IsSuccess);
            Assert.Equal("docs/guide.md", result.Value!.RelativePath);
            Assert.Equal(Path.Combine(root, "docs", "guide.md"), result.Value.FullPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_RejectsRootedPathsWithoutExposingTheCheckoutRoot()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var resolver = new ProjectPathResolver(root);

            var result = resolver.Resolve(Path.Combine(root, "secrets.txt"));

            Assert.False(result.IsSuccess);
            Assert.Equal("path.invalid", result.Error.Code);
            Assert.DoesNotContain(root, result.Error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("docs/../../outside.txt")]
    [InlineData("docs\\guide.md")]
    public void Resolve_RejectsTraversalAndAlternativeSeparatorsBeforeResolution(string input)
    {
        var root = CreateTemporaryRoot();
        try
        {
            var result = new ProjectPathResolver(root).Resolve(input);

            Assert.False(result.IsSuccess);
            Assert.Equal("path.invalid", result.Error.Code);
            Assert.DoesNotContain(root, result.Error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_RejectsASymlinkThatLeavesTheCheckout()
    {
        var root = CreateTemporaryRoot();
        var outside = CreateTemporaryRoot();
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(root, "outside-link"), outside);
            var resolver = new ProjectPathResolver(root);

            var result = resolver.Resolve("outside-link/secrets.txt");

            Assert.False(result.IsSuccess);
            Assert.Equal("path.symlink_outside", result.Error.Code);
            Assert.DoesNotContain(root, result.Error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(outside, result.Error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public void Resolve_AllowsASymlinkWhoseFinalTargetStaysInsideTheCheckout()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var documents = Directory.CreateDirectory(Path.Combine(root, "docs"));
            Directory.CreateSymbolicLink(Path.Combine(root, "docs-link"), documents.FullName);

            var result = new ProjectPathResolver(root).Resolve("docs-link/guide.md");

            Assert.True(result.IsSuccess);
            Assert.Equal("docs-link/guide.md", result.Value!.RelativePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}