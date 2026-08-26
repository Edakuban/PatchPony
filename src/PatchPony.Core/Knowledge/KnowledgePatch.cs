using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Knowledge;

public abstract record KnowledgePatchOperation;
public sealed record SetKnowledgeFrontmatterField(string Key, string Value) : KnowledgePatchOperation;
public sealed record RemoveKnowledgeFrontmatterField(string Key) : KnowledgePatchOperation;
public sealed record ReplaceKnowledgeMarkdownSection(string Heading, string Content) : KnowledgePatchOperation;
public sealed record AppendKnowledgeMarkdown(string Content) : KnowledgePatchOperation;

/// <summary>A bounded semantic proposal, never a raw diff or filesystem write request.</summary>
public sealed record KnowledgePatchRequest(string Path, string ExpectedSha256, IReadOnlyList<KnowledgePatchOperation> Operations, string? FrontmatterSchemaId = null)
{
    private static readonly Regex FieldName = new("^[a-z][a-z0-9_-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex Heading = new("^#{1,6} [^\\r\\n]{1,160}$", RegexOptions.CultureInvariant);

    public Result ValidateContract()
    {
        if (string.IsNullOrWhiteSpace(Path) || Path.Length > 512 || !Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || !IsSha256(ExpectedSha256) || Operations is not { Count: > 0 and <= 8 })
            return Result.Failure(new DomainError("knowledge.patch.invalid", "The knowledge patch contract is invalid."));
        if (FrontmatterSchemaId is { Length: > 128 } || FrontmatterSchemaId?.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-')) == true)
            return Result.Failure(new DomainError("knowledge.patch.invalid", "The frontmatter schema reference is invalid."));

        foreach (var operation in Operations)
        {
            var valid = operation switch
            {
                SetKnowledgeFrontmatterField set => FieldName.IsMatch(set.Key) && IsSafeScalar(set.Value, 2_048),
                RemoveKnowledgeFrontmatterField remove => FieldName.IsMatch(remove.Key),
                ReplaceKnowledgeMarkdownSection section => Heading.IsMatch(section.Heading) && IsMarkdown(section.Content, 16 * 1024),
                AppendKnowledgeMarkdown append => IsMarkdown(append.Content, 16 * 1024),
                _ => false
            };
            if (!valid) return Result.Failure(new DomainError("knowledge.patch.invalid", "The knowledge patch contains an unsupported or unsafe operation."));
        }
        return Result.Success();
    }

    private static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static bool IsSafeScalar(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(character => character is '\r' or '\n' or '\0' || char.IsControl(character));
    private static bool IsMarkdown(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Contains('\0');
}

public sealed record KnowledgePatchProposal(string Path, string Content, string Sha256, IReadOnlyList<string> AppliedOperations);