using PatchPony.Core.Sandbox;

namespace PatchPony.Core.Tests;

public sealed class SandboxRunnerCatalogTests
{
    [Fact]
    public void Resolve_ReturnsOnlyRegisteredDigestPinnedImages()
    {
        var image = new SandboxRunnerImage("dotnet-test-v1", "registry.example.test/patchpony/dotnet-test@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true);
        var catalog = new SandboxRunnerCatalog([image]);

        var resolved = catalog.Resolve("dotnet-test-v1");
        var unknown = catalog.Resolve("shell-latest");

        Assert.True(resolved.IsSuccess);
        Assert.Equal(image, resolved.Value);
        Assert.False(unknown.IsSuccess);
        Assert.Equal("sandbox.runner.unsupported", unknown.Error.Code);
    }

    [Theory]
    [InlineData("registry.example.test/patchpony/dotnet-test:latest")]
    [InlineData("registry.example.test/patchpony/dotnet-test@sha256:ABCDEF")]
    public void Constructor_RejectsNonDigestPinnedOrMalformedImages(string imageReference)
    {
        Assert.Throws<ArgumentException>(() => new SandboxRunnerCatalog([new SandboxRunnerImage("dotnet-test-v1", imageReference, true)]));
    }

    [Fact]
    public void Constructor_RejectsRunnersWithoutExplicitRootlessCompatibility()
    {
        var image = "registry.example.test/patchpony/dotnet-test@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        Assert.Throws<ArgumentException>(() => new SandboxRunnerCatalog([
            new SandboxRunnerImage("dotnet-test-v1", image, false)]));
    }
    [Fact]
    public void Constructor_RejectsDuplicateRunnerIds()
    {
        var image = "registry.example.test/patchpony/dotnet-test@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        Assert.Throws<ArgumentException>(() => new SandboxRunnerCatalog([
            new SandboxRunnerImage("dotnet-test-v1", image, true),
            new SandboxRunnerImage("dotnet-test-v1", image, true)]));
    }
}