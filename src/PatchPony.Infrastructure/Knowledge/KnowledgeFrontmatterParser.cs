using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using PatchPony.Infrastructure.Configuration;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PatchPony.Infrastructure.Knowledge;

public sealed record KnowledgeFrontmatterResult(bool HasFrontmatter, ConfigValidationReport Report);

/// <summary>Extracts and validates only an initial YAML frontmatter block. It does not read files or interpret Markdown body content.</summary>
public sealed class KnowledgeFrontmatterParser(IJsonSchemaResolver? schemas = null)
{
    public const int MaximumFrontmatterBytes = 32 * 1024;
    public const int MaximumFrontmatterLines = 200;
    public const int MaximumYamlDepth = 12;

    private readonly YamlConfigAdapter yaml = new(schemas);

    public Result<KnowledgeFrontmatterResult> Validate(string markdown, string? schemaId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        cancellationToken.ThrowIfCancellationRequested();
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        if (lines.Length == 0 || !string.Equals(lines[0].TrimStart('\uFEFF'), "---", StringComparison.Ordinal))
        {
            return Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(false, ValidReport()));
        }

        var closingLine = -1;
        for (var index = 1; index < lines.Length && index <= MaximumFrontmatterLines; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (lines[index] is "---" or "...")
            {
                closingLine = index;
                break;
            }
        }
        if (closingLine < 0)
        {
            return Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(true, Invalid("frontmatter.unterminated", "The YAML frontmatter must end within the configured line limit.")));
        }

        var content = string.Join('\n', lines[1..closingLine]);
        if (Encoding.UTF8.GetByteCount(content) > MaximumFrontmatterBytes)
        {
            return Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(true, Invalid("frontmatter.too_large", "The YAML frontmatter exceeds the parser limit.")));
        }

        try
        {
            EnforceDepth(content, cancellationToken);
        }
        catch (YamlException)
        {
            return Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(true, Invalid("frontmatter.invalid", "The YAML frontmatter is not valid YAML.")));
        }
        catch (FrontmatterSafetyException exception)
        {
            return Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(true, Invalid(exception.Code, exception.Message)));
        }

        var validation = yaml.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Yaml, Encoding.UTF8.GetBytes(content), schemaId), cancellationToken);
        return validation.IsSuccess
            ? Result<KnowledgeFrontmatterResult>.Success(new KnowledgeFrontmatterResult(true, validation.Value!))
            : Result<KnowledgeFrontmatterResult>.Failure(validation.Error);
    }

    private static void EnforceDepth(string content, CancellationToken cancellationToken)
    {
        var parser = new Parser(new StringReader(content));
        var depth = 0;
        while (parser.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (parser.Current)
            {
                case MappingStart or SequenceStart:
                    if (++depth > MaximumYamlDepth) throw new FrontmatterSafetyException("frontmatter.too_deep", "The YAML frontmatter exceeds the nesting limit.");
                    break;
                case MappingEnd or SequenceEnd:
                    depth--;
                    break;
            }
        }
    }

    private static ConfigValidationReport ValidReport() => new(ConfigDocumentFormat.Yaml, true, []);
    private static ConfigValidationReport Invalid(string code, string message) => new(ConfigDocumentFormat.Yaml, false, [new ConfigValidationIssue(code, ConfigValidationSeverity.Error, message)]);
    private sealed class FrontmatterSafetyException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}