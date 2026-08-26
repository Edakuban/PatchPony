using System.Text.Json;
using Json.Schema;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;

namespace PatchPony.Infrastructure.Configuration;

public interface IJsonSchemaResolver
{
    Result<JsonSchema> Resolve(string schemaId);
}

public sealed record JsonSchemaRegistration(string Id, string SchemaJson);

/// <summary>Fixed server-side JSON schema catalog. Schema text is parsed at composition time.</summary>
public sealed class JsonSchemaCatalog : IJsonSchemaResolver
{
    private readonly IReadOnlyDictionary<string, JsonSchema> schemas;

    public JsonSchemaCatalog(IEnumerable<JsonSchemaRegistration> registrations)
    {
        var mapped = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            if (string.IsNullOrWhiteSpace(registration.Id) || registration.Id.Length > 128 || registration.Id.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-')))
            {
                throw new ArgumentException("Schema identifiers must be bounded server-side identifiers.", nameof(registrations));
            }

            if (!mapped.TryAdd(registration.Id, JsonSchema.FromText(registration.SchemaJson)))
            {
                throw new ArgumentException($"Only one JSON schema may be registered for {registration.Id}.", nameof(registrations));
            }
        }

        schemas = mapped;
    }

    public Result<JsonSchema> Resolve(string schemaId) =>
        schemas.TryGetValue(schemaId, out var schema)
            ? Result<JsonSchema>.Success(schema)
            : Result<JsonSchema>.Failure(new DomainError("config.schema.unsupported", "The requested JSON schema is not registered."));
}

/// <summary>Bounded JSON parsing and optional validation against a server-registered schema.</summary>
public sealed class JsonConfigAdapter(IJsonSchemaResolver? schemas = null) : IConfigDocumentAdapter
{
    public const int MaximumDocumentBytes = 1024 * 1024;
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64
    };

    public ConfigDocumentFormat Format => ConfigDocumentFormat.Json;

    public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Format != Format)
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.adapter.format_mismatch", "The JSON adapter only accepts JSON documents."));
        }

        if (request.Utf8Content.Length > MaximumDocumentBytes)
        {
            return Result<ConfigValidationReport>.Success(Invalid("json.too_large", "The JSON document exceeds the parser limit."));
        }

        try
        {
            using var document = JsonDocument.Parse(request.Utf8Content, DocumentOptions);
            cancellationToken.ThrowIfCancellationRequested();
            if (request.SchemaId is null)
            {
                return Result<ConfigValidationReport>.Success(new ConfigValidationReport(Format, true, []));
            }

            if (schemas is null)
            {
                return Result<ConfigValidationReport>.Failure(new DomainError("config.schema.unsupported", "No JSON schema catalog is configured."));
            }

            var schema = schemas.Resolve(request.SchemaId);
            if (!schema.IsSuccess)
            {
                return Result<ConfigValidationReport>.Failure(schema.Error);
            }

            var evaluation = schema.Value!.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            return Result<ConfigValidationReport>.Success(evaluation.IsValid
                ? new ConfigValidationReport(Format, true, [])
                : Invalid("json.schema.invalid", "The JSON document does not satisfy the selected schema."));
        }
        catch (JsonException)
        {
            return Result<ConfigValidationReport>.Success(Invalid("json.invalid", "The configuration document is not valid JSON."));
        }
    }

    private static ConfigValidationReport Invalid(string code, string message) =>
        new(ConfigDocumentFormat.Json, false, [new ConfigValidationIssue(code, ConfigValidationSeverity.Error, message)]);
}