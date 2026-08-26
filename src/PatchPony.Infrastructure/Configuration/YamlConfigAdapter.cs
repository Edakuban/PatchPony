using System.Text;
using System.Text.Json;
using Json.Schema;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace PatchPony.Infrastructure.Configuration;

/// <summary>Strict YAML parsing with zero permitted anchors/aliases and optional JSON Schema validation.</summary>
public sealed class YamlConfigAdapter(IJsonSchemaResolver? schemas = null) : IConfigDocumentAdapter
{
    public const int MaximumDocumentBytes = 1024 * 1024;
    public const int MaximumNodes = 100_000;
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly IDeserializer Deserializer = new DeserializerBuilder().WithDuplicateKeyChecking().Build();
    private static readonly ISerializer JsonSerializer = new SerializerBuilder().JsonCompatible().Build();

    public ConfigDocumentFormat Format => ConfigDocumentFormat.Yaml;

    public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Format != Format)
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.adapter.format_mismatch", "The YAML adapter only accepts YAML documents."));
        }

        if (request.Utf8Content.Length > MaximumDocumentBytes)
        {
            return Result<ConfigValidationReport>.Success(Invalid("yaml.too_large", "The YAML document exceeds the parser limit."));
        }

        try
        {
            var yaml = StrictUtf8.GetString(request.Utf8Content.Span);
            EnforceSafeYamlEvents(yaml, cancellationToken);
            var value = Deserializer.Deserialize<object>(yaml);
            var json = JsonSerializer.Serialize(value);
            using var document = JsonDocument.Parse(json);
            cancellationToken.ThrowIfCancellationRequested();

            if (request.SchemaId is null)
            {
                return Result<ConfigValidationReport>.Success(new ConfigValidationReport(Format, true, []));
            }

            if (schemas is null)
            {
                return Result<ConfigValidationReport>.Failure(new DomainError("config.schema.unsupported", "No YAML schema catalog is configured."));
            }

            var schema = schemas.Resolve(request.SchemaId);
            if (!schema.IsSuccess)
            {
                return Result<ConfigValidationReport>.Failure(schema.Error);
            }

            var evaluation = schema.Value!.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            return Result<ConfigValidationReport>.Success(evaluation.IsValid
                ? new ConfigValidationReport(Format, true, [])
                : Invalid("yaml.schema.invalid", "The YAML document does not satisfy the selected schema."));
        }
        catch (YamlSafetyException exception)
        {
            return Result<ConfigValidationReport>.Success(Invalid(exception.Code, exception.Message));
        }
        catch (YamlException)
        {
            return Result<ConfigValidationReport>.Success(Invalid("yaml.invalid", "The configuration document is not valid YAML."));
        }
        catch (DecoderFallbackException)
        {
            return Result<ConfigValidationReport>.Success(Invalid("yaml.invalid", "The configuration document is not valid UTF-8 YAML."));
        }
        catch (JsonException)
        {
            return Result<ConfigValidationReport>.Success(Invalid("yaml.invalid", "The YAML document cannot be represented safely as JSON data."));
        }
    }

    private static void EnforceSafeYamlEvents(string yaml, CancellationToken cancellationToken)
    {
        var parser = new Parser(new StringReader(yaml));
        var documents = 0;
        var nodes = 0;
        while (parser.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (parser.Current)
            {
                case DocumentStart:
                    if (++documents > 1)
                    {
                        throw new YamlSafetyException("yaml.multiple_documents", "Only one YAML document is allowed.");
                    }

                    break;
                case AnchorAlias:
                    throw new YamlSafetyException("yaml.alias_disallowed", "YAML aliases are not allowed.");
                case NodeEvent node:
                    if (++nodes > MaximumNodes)
                    {
                        throw new YamlSafetyException("yaml.too_complex", "The YAML document exceeds the node limit.");
                    }

                    if (!node.Anchor.IsEmpty)
                    {
                        throw new YamlSafetyException("yaml.anchor_disallowed", "YAML anchors are not allowed.");
                    }

                    if (!node.Tag.IsEmpty)
                    {
                        throw new YamlSafetyException("yaml.tag_disallowed", "Explicit YAML tags are not allowed.");
                    }

                    break;
            }
        }
    }

    private static ConfigValidationReport Invalid(string code, string message) =>
        new(ConfigDocumentFormat.Yaml, false, [new ConfigValidationIssue(code, ConfigValidationSeverity.Error, message)]);

    private sealed class YamlSafetyException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}