using PatchPony.Core.Sandbox;

namespace PatchPony.Core.Tests;

public sealed class SandboxCommandCatalogTests
{
    [Fact]
    public void Resolve_MapsARegisteredCommandToFixedRunnerExecutableAndArguments()
    {
        var runners = Runners();
        var catalog = new SandboxCommandCatalog(runners,
        [
            new SandboxCommandDefinition("test.billing.unit", "dotnet-test-v1", "/usr/bin/dotnet", ["test", "tests/Billing.Tests.csproj", "--no-restore"])
        ]);

        var command = catalog.Resolve("test.billing.unit");
        var unknown = catalog.Resolve("test.unknown");

        Assert.True(command.IsSuccess);
        Assert.Equal("dotnet-test-v1", command.Value!.Runner.Id);
        Assert.Equal("/usr/bin/dotnet", command.Value.Executable);
        Assert.Equal(["test", "tests/Billing.Tests.csproj", "--no-restore"], command.Value.Arguments);
        Assert.False(unknown.IsSuccess);
        Assert.Equal("sandbox.command.unsupported", unknown.Error.Code);
    }

    [Fact]
    public void Constructor_RejectsShellsAndUnknownRunners()
    {
        var runners = Runners();

        Assert.Throws<ArgumentException>(() => new SandboxCommandCatalog(runners,
        [
            new SandboxCommandDefinition("test.shell", "dotnet-test-v1", "/bin/sh", ["-c", "anything"])
        ]));
        Assert.Throws<ArgumentException>(() => new SandboxCommandCatalog(runners,
        [
            new SandboxCommandDefinition("test.unknown", "missing-runner", "/usr/bin/dotnet", ["test"])
        ]));
    }

    private static SandboxRunnerCatalog Runners() => new(
    [
        new SandboxRunnerImage("dotnet-test-v1", "registry.example.test/patchpony/dotnet-test@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)
    ]);
}