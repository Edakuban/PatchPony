using System.Text;
using System.Xml;
using System.Xml.Schema;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;

namespace PatchPony.Infrastructure.Configuration;

public interface IXmlSchemaResolver
{
    Result<XmlSchemaSet> Resolve(string schemaId);
}

public sealed record XmlSchemaRegistration(string Id, string SchemaXml);

/// <summary>Fixed server-side XSD catalog. Schemas are parsed and compiled during composition.</summary>
public sealed class XmlSchemaCatalog : IXmlSchemaResolver
{
    private readonly IReadOnlyDictionary<string, XmlSchemaSet> schemas;

    public XmlSchemaCatalog(IEnumerable<XmlSchemaRegistration> registrations)
    {
        var mapped = new Dictionary<string, XmlSchemaSet>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            if (string.IsNullOrWhiteSpace(registration.Id) || registration.Id.Length > 128 || registration.Id.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-')))
            {
                throw new ArgumentException("Schema identifiers must be bounded server-side identifiers.", nameof(registrations));
            }

            var schemas = new XmlSchemaSet { XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(registration.SchemaXml), CreateSecureSettings());
            schemas.Add(null, reader);
            schemas.Compile();
            if (!mapped.TryAdd(registration.Id, schemas))
            {
                throw new ArgumentException($"Only one XML schema may be registered for {registration.Id}.", nameof(registrations));
            }
        }

        this.schemas = mapped;
    }

    public Result<XmlSchemaSet> Resolve(string schemaId) =>
        schemas.TryGetValue(schemaId, out var schema)
            ? Result<XmlSchemaSet>.Success(schema)
            : Result<XmlSchemaSet>.Failure(new DomainError("config.schema.unsupported", "The requested XML schema is not registered."));

    internal static XmlReaderSettings CreateSecureSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = XmlConfigAdapter.MaximumDocumentBytes,
        MaxCharactersFromEntities = 0
    };
}

/// <summary>Bounded XML parsing with optional validation against a server-registered XSD.</summary>
public sealed class XmlConfigAdapter(IXmlSchemaResolver? schemas = null) : IConfigDocumentAdapter
{
    public const int MaximumDocumentBytes = 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public ConfigDocumentFormat Format => ConfigDocumentFormat.Xml;

    public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Format != Format)
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.adapter.format_mismatch", "The XML adapter only accepts XML documents."));
        }

        if (request.Utf8Content.Length > MaximumDocumentBytes)
        {
            return Result<ConfigValidationReport>.Success(Invalid("xml.too_large", "The XML document exceeds the parser limit."));
        }

        try
        {
            var xml = StrictUtf8.GetString(request.Utf8Content.Span);
            if (xml.Contains("<!DOCTYPE", StringComparison.Ordinal))
            {
                return Result<ConfigValidationReport>.Success(Invalid("xml.dtd_disallowed", "DTD declarations and external entities are not allowed."));
            }

            var issues = new List<ConfigValidationIssue>();
            var settings = XmlSchemaCatalog.CreateSecureSettings();
            if (request.SchemaId is not null)
            {
                if (schemas is null)
                {
                    return Result<ConfigValidationReport>.Failure(new DomainError("config.schema.unsupported", "No XML schema catalog is configured."));
                }

                var schema = schemas.Resolve(request.SchemaId);
                if (!schema.IsSuccess)
                {
                    return Result<ConfigValidationReport>.Failure(schema.Error);
                }

                settings.ValidationType = ValidationType.Schema;
                settings.Schemas = schema.Value!;
                settings.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints;
                settings.ValidationEventHandler += (_, arguments) => issues.Add(new ConfigValidationIssue(
                    "xml.schema.invalid",
                    arguments.Severity == XmlSeverityType.Warning ? ConfigValidationSeverity.Warning : ConfigValidationSeverity.Error,
                    "The XML document does not satisfy the selected schema.",
                    arguments.Exception?.LineNumber,
                    arguments.Exception?.LinePosition));
            }

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Result<ConfigValidationReport>.Success(new ConfigValidationReport(Format, !issues.Any(issue => issue.Severity == ConfigValidationSeverity.Error), issues));
        }
        catch (XmlException exception)
        {
            return Result<ConfigValidationReport>.Success(new ConfigValidationReport(Format, false,
                [new ConfigValidationIssue("xml.invalid", ConfigValidationSeverity.Error, "The configuration document is not valid XML.", exception.LineNumber, exception.LinePosition)]));
        }
        catch (DecoderFallbackException)
        {
            return Result<ConfigValidationReport>.Success(Invalid("xml.invalid", "The configuration document is not valid UTF-8 XML."));
        }
    }

    private static ConfigValidationReport Invalid(string code, string message) =>
        new(ConfigDocumentFormat.Xml, false, [new ConfigValidationIssue(code, ConfigValidationSeverity.Error, message)]);
}