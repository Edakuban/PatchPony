using System.Security.Cryptography;
using System.Text;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Configuration;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ConfigPatchDiffServiceTests
{
    [Fact]
    public void Create_ReturnsARedactedUnifiedDiffForTheHashPinnedPatch()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = Encoding.UTF8.GetBytes("{\"apiKey\":\"old-secret\",\"enabled\":false}");
            var replacement = Encoding.UTF8.GetBytes("{\"apiKey\":\"new-secret\",\"enabled\":true}");
            File.WriteAllBytes(Path.Combine(root, "settings.json"), source);
            var service = CreateService(root);

            var result = service.Create(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement));

            Assert.True(result.IsSuccess);
            Assert.Equal("settings.json", result.Value!.TargetPath);
            Assert.Contains("--- a/settings.json", result.Value.Content, StringComparison.Ordinal);
            Assert.Contains("+++ b/settings.json", result.Value.Content, StringComparison.Ordinal);
            Assert.Contains("[REDACTED]", result.Value.Content, StringComparison.Ordinal);
            Assert.DoesNotContain("old-secret", result.Value.Content, StringComparison.Ordinal);
            Assert.DoesNotContain("new-secret", result.Value.Content, StringComparison.Ordinal);
            Assert.Equal(source, File.ReadAllBytes(Path.Combine(root, "settings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_EnforcesLineAndByteLimitsAndReportsTruncation()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = Encoding.UTF8.GetBytes("one\ntwo\nthree\n");
            var replacement = Encoding.UTF8.GetBytes("four\nfive\nsix\n");
            File.WriteAllBytes(Path.Combine(root, "settings.json"), source);
            var service = new ConfigPatchDiffService(
                new ProjectPathResolver(root),
                WritablePolicy(),
                maximumOutputBytes: 512,
                maximumChangedLines: 2);

            var result = service.Create(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement));

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.IsTruncated);
            Assert.Equal(8, result.Value.TotalChangedLines);
            Assert.True(Encoding.UTF8.GetByteCount(result.Value.Content) <= 512);
            Assert.DoesNotContain("+four", result.Value.Content, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_RejectsForbiddenTargetsBeforeReadingTheirContent()
    {
        var root = CreateTemporaryRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "config", "secrets"));
            var source = "{\"password\":\"never-disclose\"}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "config", "secrets", "settings.json"), source);
            var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(["**"], ["config/**"], ["config/secrets/**"])).Value!;
            var service = new ConfigPatchDiffService(new ProjectPathResolver(root), policy);

            var result = service.Create(new ConfigPatchRequest("config/secrets/settings.json", Sha256(source), ConfigDocumentFormat.Json, source));

            Assert.False(result.IsSuccess);
            Assert.Equal("path.forbidden", result.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ConfigPatchDiffService CreateService(string root) => new(new ProjectPathResolver(root), WritablePolicy());

    private static ProjectPathPolicy WritablePolicy() =>
        ProjectPathPolicy.Create(new ProjectManifestPaths(["**"], ["**"], [".env", "**/.env", "**/.env.*"])).Value!;

    private static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}