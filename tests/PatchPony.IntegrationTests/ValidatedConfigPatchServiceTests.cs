using System.Security.Cryptography;
using System.Text;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Configuration;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.IntegrationTests;

public sealed class ValidatedConfigPatchServiceTests
{
    [Fact]
    public void Apply_KeepsAParserAndSchemaValidPatch()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = "{\"name\":\"before\"}"u8.ToArray();
            var replacement = "{\"name\":\"after\"}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "settings.json"), source);
            var service = CreateService(root, ProfileCatalog());

            var result = service.Apply(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, replacement, "profile.v1"));

            Assert.True(result.IsSuccess);
            Assert.False(result.Value!.WasRolledBack);
            Assert.True(result.Value.Validation.IsValid);
            Assert.Equal(replacement, File.ReadAllBytes(Path.Combine(root, "settings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_RollsBackAnInvalidParserResult()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = "{\"name\":\"before\"}"u8.ToArray();
            var invalidReplacement = "{\"name\":}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "settings.json"), source);
            var service = CreateService(root, new ConfigAdapterCatalog([new JsonConfigAdapter()]));

            var result = service.Apply(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, invalidReplacement));

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.WasRolledBack);
            Assert.False(result.Value.Validation.IsValid);
            Assert.Equal("json.invalid", Assert.Single(result.Value.Validation.Issues).Code);
            Assert.Equal(source, File.ReadAllBytes(Path.Combine(root, "settings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_RollsBackASchemaViolation()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var source = "{\"name\":\"before\"}"u8.ToArray();
            var invalidReplacement = "{\"unexpected\":true}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "settings.json"), source);
            var service = CreateService(root, ProfileCatalog());

            var result = service.Apply(new ConfigPatchRequest("settings.json", Sha256(source), ConfigDocumentFormat.Json, invalidReplacement, "profile.v1"));

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.WasRolledBack);
            Assert.False(result.Value.Validation.IsValid);
            Assert.Equal("json.schema.invalid", Assert.Single(result.Value.Validation.Issues).Code);
            Assert.Equal(source, File.ReadAllBytes(Path.Combine(root, "settings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ValidatedConfigPatchService CreateService(string root, ConfigAdapterCatalog adapters)
    {
        var resolver = new ProjectPathResolver(root);
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(["**"], ["**"], [".env", "**/.env", "**/.env.*"])).Value!;
        return new ValidatedConfigPatchService(new ConfigPatchService(resolver, policy), adapters, resolver, policy);
    }

    private static ConfigAdapterCatalog ProfileCatalog() => new([
        new JsonConfigAdapter(new JsonSchemaCatalog([
            new JsonSchemaRegistration("profile.v1", """{ "type": "object", "required": ["name"], "additionalProperties": false, "properties": { "name": { "type": "string" } } }""")]))]);

    private static string Sha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}