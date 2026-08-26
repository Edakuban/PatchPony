using System.Security.Cryptography;
using System.Text;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ConfigSessionCatalogTests
{
    [Fact]
    public void ServerConfiguredSession_ValidatesAndPatchesOnlyItsOwnWorktree()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var configDirectory = Path.Combine(root, "config");
            Directory.CreateDirectory(configDirectory);
            var source = "{\"enabled\":false}"u8.ToArray();
            var replacement = "{\"enabled\":true}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(configDirectory, "settings.json"), source);
            var manifest = Path.Combine(root, "project.yaml");
            File.WriteAllText(manifest, ManifestYaml("demo-project"));
            var sessionId = Guid.NewGuid().ToString("N");
            var catalog = new ConfigSessionCatalog(new ConfigSessionOptions
            {
                Sessions = [new ConfigSessionEntryOptions { SessionId = sessionId, ProjectId = "demo-project", WorktreeRoot = root, ManifestFile = manifest }]
            });

            var validation = catalog.Validate("demo-project", sessionId, new ConfigValidateCommand("config/settings.json", "json"));
            var patch = catalog.Patch("demo-project", sessionId, new ConfigPatchCommand("config/settings.json", "json", Sha256(source), Convert.ToBase64String(replacement)));
            var foreign = catalog.Validate("demo-project", Guid.NewGuid().ToString("N"), new ConfigValidateCommand("config/settings.json", "json"));

            Assert.True(validation.IsSuccess);
            Assert.True(validation.Value!.IsValid);
            Assert.True(patch.IsSuccess);
            Assert.False(patch.Value!.WasRolledBack);
            Assert.Equal(replacement, File.ReadAllBytes(Path.Combine(configDirectory, "settings.json")));
            Assert.False(foreign.IsSuccess);
            Assert.Equal("session.not_found", foreign.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string ManifestYaml(string id) => $"""
schemaVersion: 1
project:
  id: {id}
  displayName: Demo
repository:
  remoteUrl: https://github.com/example/demo.git
  defaultBranch: main
paths:
  readable:
    - config/**
  writable:
    - config/**
  forbidden:
    - .env
    - '**/.env'
    - '**/.env.*'
""";

    private static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}