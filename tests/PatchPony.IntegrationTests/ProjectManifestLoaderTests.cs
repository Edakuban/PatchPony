using PatchPony.Infrastructure.Manifests;

namespace PatchPony.IntegrationTests;

public sealed class ProjectManifestLoaderTests
{
    private readonly ProjectManifestLoader loader = new();

    [Fact]
    public void Load_AcceptsTheVersionOneManifest()
    {
        var result = loader.Load(ValidManifest);

        Assert.True(result.IsSuccess);
        Assert.Equal("patchpony", result.Value!.Project.Id);
        Assert.Equal("https://github.com/Edakuban/PatchPony.git", result.Value.Repository.RemoteUri.AbsoluteUri);
        Assert.Equal(["src/**", "docs/**"], result.Value.Paths.Readable);
    }

    [Fact]
    public void Load_RejectsUnknownFields()
    {
        var result = loader.Load(ValidManifest + "\nunknown: value\n");

        Assert.False(result.IsSuccess);
        Assert.Equal("manifest.invalid", result.Error.Code);
    }

    [Fact]
    public void Load_RejectsPathTraversal()
    {
        var manifest = ValidManifest.Replace("    - src/**", "    - ../secrets/**");

        var result = loader.Load(manifest);

        Assert.False(result.IsSuccess);
        Assert.Equal("manifest.invalid", result.Error.Code);
    }

    [Fact]
    public void Load_RejectsYamlAliases()
    {
        var manifest = ValidManifest.Replace("  readable:", "  readable: &readable").Replace("  writable: []", "  writable: *readable");

        var result = loader.Load(manifest);

        Assert.False(result.IsSuccess);
        Assert.Equal("manifest.invalid", result.Error.Code);
    }

    private const string ValidManifest = """
        schemaVersion: 1
        project:
          id: patchpony
          displayName: PatchPony
        repository:
          remoteUrl: https://github.com/Edakuban/PatchPony.git
          defaultBranch: main
        paths:
          readable:
            - src/**
            - docs/**
          writable: []
          forbidden:
            - .git/**
            - .env
        """;
}
