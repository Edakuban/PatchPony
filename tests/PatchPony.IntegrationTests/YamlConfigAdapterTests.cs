using PatchPony.Core.Configuration;
using PatchPony.Infrastructure.Configuration;

namespace PatchPony.IntegrationTests;

public sealed class YamlConfigAdapterTests
{
    [Fact]
    public void Validate_AcceptsSafeYamlAndValidatesAgainstAServerSchema()
    {
        var adapter = new YamlConfigAdapter(new JsonSchemaCatalog([new JsonSchemaRegistration("profile.v1", """{ "type": "object", "required": ["name"], "properties": { "name": { "type": "string" } } }""")]));

        var valid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, "name: PatchPony\n"u8.ToArray(), "profile.v1"));
        var schemaInvalid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, "count: 1\n"u8.ToArray(), "profile.v1"));

        Assert.True(valid.IsSuccess);
        Assert.True(valid.Value!.IsValid);
        Assert.True(schemaInvalid.IsSuccess);
        Assert.False(schemaInvalid.Value!.IsValid);
        Assert.Equal("yaml.schema.invalid", Assert.Single(schemaInvalid.Value.Issues).Code);
    }

    [Theory]
    [InlineData("item: &shared value\n", "yaml.anchor_disallowed")]
    [InlineData("item: *shared\n", "yaml.alias_disallowed")]
    [InlineData("---\nname: first\n---\nname: second\n", "yaml.multiple_documents")]
    public void Validate_RejectsAnchorsAliasesAndMultipleDocuments(string yaml, string expectedCode)
    {
        var result = new YamlConfigAdapter().Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, System.Text.Encoding.UTF8.GetBytes(yaml)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsValid);
        Assert.Equal(expectedCode, result.Value.Issues.Single().Code);
    }

    [Fact]
    public void Validate_RejectsFormatMismatchAndUnknownSchema()
    {
        var wrongFormat = new YamlConfigAdapter().Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray()));
        var unknownSchema = new YamlConfigAdapter(new JsonSchemaCatalog([])).Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, "name: pony\n"u8.ToArray(), "missing.v1"));

        Assert.False(wrongFormat.IsSuccess);
        Assert.Equal("config.adapter.format_mismatch", wrongFormat.Error.Code);
        Assert.False(unknownSchema.IsSuccess);
        Assert.Equal("config.schema.unsupported", unknownSchema.Error.Code);
    }
}