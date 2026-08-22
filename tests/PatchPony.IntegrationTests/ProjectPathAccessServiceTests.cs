using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ProjectPathAccessServiceTests
{
    [Fact]
    public void ResolveAndAuthorize_RequiresBothSafeResolutionAndManifestReadPermission()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(
                ["src/**"],
                [],
                ["**/.env"])).Value!;
            var service = new ProjectPathAccessService(new ProjectPathResolver(root), policy);

            var allowed = service.ResolveAndAuthorize("src/Program.cs", ProjectPathAccess.Read);
            var denied = service.ResolveAndAuthorize("docs/guide.md", ProjectPathAccess.Read);
            var unsafePath = service.ResolveAndAuthorize("src\\Program.cs", ProjectPathAccess.Read);

            Assert.True(allowed.IsSuccess);
            Assert.Equal("path.not_readable", denied.Error.Code);
            Assert.Equal("path.invalid", unsafePath.Error.Code);
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
