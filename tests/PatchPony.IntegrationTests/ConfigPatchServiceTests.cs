using System.Security.Cryptography;
using System.Text;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Configuration;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ConfigPatchServiceTests
{
    [Fact]
    public void Apply_ReplacesOnlyTheExactHashPinnedConfigurationDocument()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var target = Path.Combine(root, "settings.json");
            var source = Encoding.UTF8.GetBytes("{\"name\":\"before\"}");
            var replacement = Encoding.UTF8.GetBytes("{\"name\":\"after\"}");
            File.WriteAllBytes(target, source);
            var service = new ConfigPatchService(new ProjectPathResolver(root), CreateWritablePolicy());

            var applied = service.Apply(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement));

            Assert.True(applied.IsSuccess);
            Assert.Equal("settings.json", applied.Value!.TargetPath);
            Assert.Equal(Sha256(source), applied.Value.SourceSha256);
            Assert.Equal(Sha256(replacement), applied.Value.ResultSha256);
            Assert.Equal(replacement, File.ReadAllBytes(target));
            Assert.Empty(Directory.GetFiles(root, ".patchpony-*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_RejectsStaleHashesAndLeavesTheSourceUntouched()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var target = Path.Combine(root, "settings.yaml");
            var source = Encoding.UTF8.GetBytes("name: before\n");
            File.WriteAllBytes(target, source);
            var service = new ConfigPatchService(new ProjectPathResolver(root), CreateWritablePolicy());

            var applied = service.Apply(new ConfigPatchRequest("settings.yaml", new string('0', 64), ConfigDocumentFormat.Yaml, Encoding.UTF8.GetBytes("name: after\n")));

            Assert.False(applied.IsSuccess);
            Assert.Equal("patch.source_hash_mismatch", applied.Error.Code);
            Assert.Equal(source, File.ReadAllBytes(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_EnforcesBoundedSourceAndReplacementSizesBeforeWriting()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var target = Path.Combine(root, "settings.xml");
            var tooLarge = Encoding.UTF8.GetBytes("<" + new string('x', 31) + "/>");
            File.WriteAllBytes(target, tooLarge);
            var service = new ConfigPatchService(new ProjectPathResolver(root), CreateWritablePolicy(), maximumDocumentBytes: 32);

            var sourceTooLarge = service.Apply(new ConfigPatchRequest("settings.xml", Sha256(tooLarge), ConfigDocumentFormat.Xml, "<x/>"u8.ToArray()));
            Assert.False(sourceTooLarge.IsSuccess);
            Assert.Equal("patch.source_too_large", sourceTooLarge.Error.Code);

            var source = "<x/>"u8.ToArray();
            File.WriteAllBytes(target, source);
            var replacementTooLarge = service.Apply(new ConfigPatchRequest("settings.xml", Sha256(source), ConfigDocumentFormat.Xml, tooLarge));
            Assert.False(replacementTooLarge.IsSuccess);
            Assert.Equal("patch.replacement_too_large", replacementTooLarge.Error.Code);
            Assert.Equal(source, File.ReadAllBytes(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_RejectsUnsafePathsAndMismatchedDocumentFormats()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = "<x/>"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "settings.xml"), source);
            var service = new ConfigPatchService(new ProjectPathResolver(root), CreateWritablePolicy());

            var traversal = service.Apply(new ConfigPatchRequest("../settings.xml", Sha256(source), ConfigDocumentFormat.Xml, source));
            var mismatch = service.Apply(new ConfigPatchRequest("settings.xml", Sha256(source), ConfigDocumentFormat.Json, source));

            Assert.False(traversal.IsSuccess);
            Assert.Equal("path.invalid", traversal.Error.Code);
            Assert.False(mismatch.IsSuccess);
            Assert.Equal("patch.format_mismatch", mismatch.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Restore_DoesNotOverwriteAChangeMadeAfterThePatch()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var target = Path.Combine(root, "settings.json");
            var source = "{\"name\":\"before\"}"u8.ToArray();
            var replacement = "{\"name\":\"patched\"}"u8.ToArray();
            var concurrentChange = "{\"name\":\"newer\"}"u8.ToArray();
            File.WriteAllBytes(target, source);
            var service = new ConfigPatchService(new ProjectPathResolver(root), CreateWritablePolicy());
            var request = new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement);
            var applied = service.Apply(request);
            File.WriteAllBytes(target, concurrentChange);

            var restored = service.Restore(applied.Value!, source);

            Assert.True(applied.IsSuccess);
            Assert.False(restored.IsSuccess);
            Assert.Equal("patch.rollback_conflict", restored.Error.Code);
            Assert.Equal(concurrentChange, File.ReadAllBytes(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void Apply_EnforcesManifestWritableAndForbiddenPathRules()
    {
        var root = CreateTemporaryRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "config", "secrets"));
            Directory.CreateDirectory(Path.Combine(root, "src"));
            var source = "{\"enabled\":false}"u8.ToArray();
            var replacement = "{\"enabled\":true}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "config", "settings.json"), source);
            File.WriteAllBytes(Path.Combine(root, "config", "secrets", "credentials.json"), source);
            File.WriteAllBytes(Path.Combine(root, "src", "application.json"), source);
            var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(
                ["**"],
                ["config/**"],
                ["config/secrets/**", ".env", "**/.env", "**/.env.*"])).Value!;
            var service = new ConfigPatchService(new ProjectPathResolver(root), policy);

            var allowed = service.Apply(new ConfigPatchRequest("config/settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement));
            var outsideWritable = service.Apply(new ConfigPatchRequest("src/application.json", Sha256(source), ConfigDocumentFormat.Json, replacement));
            var forbidden = service.Apply(new ConfigPatchRequest("config/secrets/credentials.json", Sha256(source), ConfigDocumentFormat.Json, replacement));

            Assert.True(allowed.IsSuccess);
            Assert.False(outsideWritable.IsSuccess);
            Assert.Equal("path.not_writable", outsideWritable.Error.Code);
            Assert.False(forbidden.IsSuccess);
            Assert.Equal("path.forbidden", forbidden.Error.Code);
            Assert.Equal(source, File.ReadAllBytes(Path.Combine(root, "src", "application.json")));
            Assert.Equal(source, File.ReadAllBytes(Path.Combine(root, "config", "secrets", "credentials.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    private static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static ProjectPathPolicy CreateWritablePolicy() =>
        ProjectPathPolicy.Create(new ProjectManifestPaths(
            ["**"],
            ["**"],
            [".env", "**/.env", "**/.env.*"])).Value!;
    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}