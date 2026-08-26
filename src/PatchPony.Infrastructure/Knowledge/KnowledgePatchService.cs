using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;

namespace PatchPony.Infrastructure.Knowledge;

/// <summary>Builds a validated Markdown proposal in memory. Persisting it is deliberately deferred to the controlled session workflow.</summary>
public sealed class KnowledgePatchService(KnowledgeFrontmatterParser? frontmatter = null)
{
    public const int MaximumDocumentBytes = 128 * 1024;
    private static readonly KnowledgeVaultContentPolicy ContentPolicy = new();
    private static readonly Regex FrontmatterField = new("^(?<key>[a-z][a-z0-9_-]{0,63}):", RegexOptions.CultureInvariant);
    private readonly KnowledgeFrontmatterParser frontmatter = frontmatter ?? new KnowledgeFrontmatterParser();

    public Result<KnowledgePatchProposal> Propose(string currentContent, KnowledgePatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentContent);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var contract = request.ValidateContract();
        if (!contract.IsSuccess) return Result<KnowledgePatchProposal>.Failure(contract.Error);
        var contentAccess = ContentPolicy.ValidateReadablePath(request.Path);
        if (!contentAccess.IsSuccess) return Result<KnowledgePatchProposal>.Failure(contentAccess.Error);
        if (Encoding.UTF8.GetByteCount(currentContent) > MaximumDocumentBytes) return Result<KnowledgePatchProposal>.Failure(new DomainError("knowledge.patch.too_large", "The Markdown document exceeds the patch size limit."));
        if (!HashesMatch(currentContent, request.ExpectedSha256)) return Result<KnowledgePatchProposal>.Failure(new DomainError("knowledge.patch.source_hash_mismatch", "The Markdown document changed before the patch could be prepared."));

        var content = Normalize(currentContent);
        var applied = new List<string>();
        foreach (var operation in request.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result<string> result = operation switch
            {
                SetKnowledgeFrontmatterField set => SetFrontmatter(content, set.Key, set.Value),
                RemoveKnowledgeFrontmatterField remove => RemoveFrontmatter(content, remove.Key),
                ReplaceKnowledgeMarkdownSection section => ReplaceSection(content, section.Heading, section.Content),
                AppendKnowledgeMarkdown append => Result<string>.Success(Append(content, append.Content)),
                _ => Result<string>.Failure(new DomainError("knowledge.patch.invalid", "The patch operation is not supported."))
            };
            if (!result.IsSuccess) return Result<KnowledgePatchProposal>.Failure(result.Error);
            content = result.Value!;
            applied.Add(OperationName(operation));
        }

        if (Encoding.UTF8.GetByteCount(content) > MaximumDocumentBytes) return Result<KnowledgePatchProposal>.Failure(new DomainError("knowledge.patch.too_large", "The proposed Markdown document exceeds the patch size limit."));
        var validation = frontmatter.Validate(content, request.FrontmatterSchemaId, cancellationToken);
        if (!validation.IsSuccess) return Result<KnowledgePatchProposal>.Failure(validation.Error);
        if (!validation.Value!.Report.IsValid) return Result<KnowledgePatchProposal>.Failure(new DomainError("knowledge.patch.frontmatter_invalid", "The proposed Markdown frontmatter is invalid."));

        return Result<KnowledgePatchProposal>.Success(new KnowledgePatchProposal(request.Path, content, Sha256(content), applied));
    }

    public static string Sha256(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(content))));

    private static Result<string> SetFrontmatter(string content, string key, string value)
    {
        var (lines, start, end) = FrontmatterBounds(content);
        var encoded = JsonSerializer.Serialize(value);
        if (start < 0)
        {
            return Result<string>.Success($"---\n{key}: {encoded}\n---\n{content}");
        }
        var matches = Enumerable.Range(start + 1, end - start - 1).Where(index => string.Equals(FrontmatterField.Match(lines[index]).Groups["key"].Value, key, StringComparison.Ordinal)).ToArray();
        if (matches.Length > 1) return Result<string>.Failure(new DomainError("knowledge.patch.ambiguous_frontmatter", "The frontmatter field occurs more than once."));
        if (matches.Length == 1) lines[matches[0]] = $"{key}: {encoded}";
        else lines.Insert(end, $"{key}: {encoded}");
        return Result<string>.Success(string.Join('\n', lines));
    }

    private static Result<string> RemoveFrontmatter(string content, string key)
    {
        var (lines, start, end) = FrontmatterBounds(content);
        if (start < 0) return Result<string>.Failure(new DomainError("knowledge.patch.frontmatter_missing", "The Markdown document has no frontmatter to update."));
        var matches = Enumerable.Range(start + 1, end - start - 1).Where(index => string.Equals(FrontmatterField.Match(lines[index]).Groups["key"].Value, key, StringComparison.Ordinal)).ToArray();
        if (matches.Length == 0) return Result<string>.Failure(new DomainError("knowledge.patch.frontmatter_field_missing", "The requested frontmatter field does not exist."));
        if (matches.Length > 1) return Result<string>.Failure(new DomainError("knowledge.patch.ambiguous_frontmatter", "The frontmatter field occurs more than once."));
        lines.RemoveAt(matches[0]);
        return Result<string>.Success(string.Join('\n', lines));
    }

    private static Result<string> ReplaceSection(string content, string heading, string replacement)
    {
        var lines = Normalize(content).Split('\n').ToList();
        var headingIndex = lines.FindIndex(line => string.Equals(line, heading, StringComparison.Ordinal));
        if (headingIndex < 0) return Result<string>.Failure(new DomainError("knowledge.patch.section_missing", "The requested Markdown section does not exist."));
        var level = heading.TakeWhile(character => character == '#').Count();
        var end = lines.FindIndex(headingIndex + 1, line => line.StartsWith('#') && line.TakeWhile(character => character == '#').Count() <= level);
        if (end < 0) end = lines.Count;
        lines.RemoveRange(headingIndex + 1, end - headingIndex - 1);
        var replacementLines = Normalize(replacement).Split('\n');
        lines.InsertRange(headingIndex + 1, replacementLines);
        return Result<string>.Success(string.Join('\n', lines));
    }

    private static string Append(string content, string addition) => Normalize(content).TrimEnd('\n') + "\n\n" + Normalize(addition).Trim('\n') + "\n";
    private static (List<string> Lines, int Start, int End) FrontmatterBounds(string content)
    {
        var lines = Normalize(content).Split('\n').ToList();
        if (lines.Count == 0 || lines[0].TrimStart('\uFEFF') != "---") return (lines, -1, -1);
        var end = lines.FindIndex(1, line => line is "---" or "...");
        return end < 0 ? (lines, -1, -1) : (lines, 0, end);
    }
    private static string Normalize(string content) => content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private static bool HashesMatch(string content, string expected) => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sha256(content)), Encoding.ASCII.GetBytes(expected.ToLowerInvariant()));
    private static string OperationName(KnowledgePatchOperation operation) => operation switch { SetKnowledgeFrontmatterField => "frontmatter.set", RemoveKnowledgeFrontmatterField => "frontmatter.remove", ReplaceKnowledgeMarkdownSection => "section.replace", AppendKnowledgeMarkdown => "markdown.append", _ => "unknown" };
}