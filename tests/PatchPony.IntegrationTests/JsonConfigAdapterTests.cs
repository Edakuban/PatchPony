using PatchPony.Core.Configuration;
using PatchPony.Infrastructure.Configuration;

namespace PatchPony.IntegrationTests;

public sealed class JsonConfigAdapterTests
{
    [Fact]
    public void Validate_AcceptsStrictJsonAndValidatesAgainstAServerSchema()
    {
        var adapter = new JsonConfigAdapter(new JsonSchemaCatalog([new JsonSchemaRegistration("profile.v1", """{ "type": "object", "required": ["name"], "additionalProperties": false, "properties": { "name": { "type": "string" } } }""")]));

        var valid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{\"name\":\"PatchPony\"}"u8.ToArray(), "profile.v1"));
        var schemaInvalid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{\"extra\":true}"u8.ToArray(), "profile.v1"));

        Assert.True(valid.IsSuccess);
        Assert.True(valid.Value!.IsValid);
        Assert.True(schemaInvalid.IsSuccess);
        Assert.False(schemaInvalid.Value!.IsValid);
        Assert.Equal("json.schema.invalid", Assert.Single(schemaInvalid.Value.Issues).Code);
    }

    [Fact]
    public void Validate_RejectsMalformedAndUnsupportedJsonInputs()
    {
        var adapter = new JsonConfigAdapter();

        var malformed = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{\"name\":}"u8.ToArray()));
        var wrongFormat = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, "name: pony"u8.ToArray()));
        var unknownSchema = new JsonConfigAdapter(new JsonSchemaCatalog([])).Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray(), "missing.v1"));

        Assert.True(malformed.IsSuccess);
        Assert.False(malformed.Value!.IsValid);
        Assert.Equal("json.invalid", Assert.Single(malformed.Value.Issues).Code);
        Assert.False(wrongFormat.IsSuccess);
        Assert.Equal("config.adapter.format_mismatch", wrongFormat.Error.Code);
        Assert.False(unknownSchema.IsSuccess);
        Assert.Equal("config.schema.unsupported", unknownSchema.Error.Code);
    }
}