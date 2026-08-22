using PatchPony.Cli;

namespace PatchPony.IntegrationTests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task ProjectValidate_ReportsAValidManifestWithoutReadingACheckout()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var manifestPath = Path.Combine(root, "project.yaml");
            File.WriteAllText(manifestPath, ValidManifest);
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliApplication.RunAsync(["project", "validate", manifestPath], output, error);

            Assert.Equal(0, exitCode);
            Assert.Equal("Manifest valid: patchpony" + Environment.NewLine, output.ToString());
            Assert.Empty(error.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ProjectDiagnose_UsesReadOnlyTreeAndDoesNotExposeTheCheckoutRoot()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var manifestPath = Path.Combine(root, "project.yaml");
            File.WriteAllText(manifestPath, ValidManifest);
            var sourcePath = Path.Combine(root, "src", "Program.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllText(sourcePath, "Console.WriteLine(\"hello\");");
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await CliApplication.RunAsync(["project", "diagnose", root, manifestPath], output, error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Project: patchpony", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Read-only checkout: verified", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(root, output.ToString(), StringComparison.Ordinal);
            Assert.Empty(error.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UnknownCommand_ReturnsUsageWithoutMutatingAnything()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["project", "delete"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage:", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(error.ToString());
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
          writable: []
          forbidden:
            - .env
        """;

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
