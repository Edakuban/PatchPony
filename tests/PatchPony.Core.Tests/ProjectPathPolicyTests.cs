using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class ProjectPathPolicyTests
{
    [Theory]
    [InlineData("src/PatchPony.Core/Project.cs")]
    [InlineData("README.md")]
    public void Authorize_AllowsReadsMatchingTheReadableGlobs(string path)
    {
        var result = CreatePolicy().Authorize(path, ProjectPathAccess.Read);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(".env")]
    [InlineData("src/.env")]
    [InlineData("docs/.env.local")]
    public void Authorize_ForbiddenGlobsOverrideReadableAndWritableRules(string path)
    {
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(
            ["src/**", "docs/**"],
            ["docs/**"],
            [".env", "**/.env", "**/.env.*"])).Value!;

        var result = policy.Authorize(path, ProjectPathAccess.Read);

        Assert.False(result.IsSuccess);
        Assert.Equal("path.forbidden", result.Error.Code);
    }

    [Fact]
    public void Authorize_RejectsAPathOutsideTheReadableGlobs()
    {
        var result = CreatePolicy().Authorize("docs/guide.md", ProjectPathAccess.Read);

        Assert.False(result.IsSuccess);
        Assert.Equal("path.not_readable", result.Error.Code);
    }

    [Fact]
    public void Authorize_AllowsWritesMatchingTheWritableGlobs()
    {
        var result = CreatePolicy().Authorize("docs/guide.md", ProjectPathAccess.Write);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Authorize_RejectsWritesOutsideTheWritableGlobs()
    {
        var result = CreatePolicy().Authorize("src/PatchPony.Core/Project.cs", ProjectPathAccess.Write);

        Assert.False(result.IsSuccess);
        Assert.Equal("path.not_writable", result.Error.Code);
    }

    private static ProjectPathPolicy CreatePolicy() =>
        ProjectPathPolicy.Create(new ProjectManifestPaths(
            ["src/**", "README.md"],
            ["docs/**"],
            [".env", "**/.env", "**/.env.*"])).Value!;
}
