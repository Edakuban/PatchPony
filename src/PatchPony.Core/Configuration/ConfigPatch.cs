using PatchPony.Core.Common;

namespace PatchPony.Core.Configuration;

/// <summary>
/// A deterministic replacement of one existing configuration document. The caller supplies
/// the exact SHA-256 of the source file so stale plans cannot overwrite newer changes.
/// </summary>
public sealed record ConfigPatchRequest(
    string TargetPath,
    string ExpectedSourceSha256,
    ConfigDocumentFormat Format,
    ReadOnlyMemory<byte> ReplacementUtf8,
    string? SchemaId = null)
{
    public Result ValidateContract()
    {
        if (string.IsNullOrWhiteSpace(TargetPath) || TargetPath.Length > 4_096)
        {
            return Result.Failure(DomainError.Validation("A bounded target path is required."));
        }

        if (!Enum.IsDefined(Format))
        {
            return Result.Failure(DomainError.Validation("A supported configuration document format is required."));
        }

        if (SchemaId is { Length: > 128 } || SchemaId?.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-')) == true)
        {
            return Result.Failure(DomainError.Validation("A schema reference must be a bounded server-side identifier."));
        }

        if (ExpectedSourceSha256.Length != 64 || ExpectedSourceSha256.Any(character => !((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'))))
        {
            return Result.Failure(DomainError.Validation("The expected source checksum must be a lowercase SHA-256 hex value."));
        }

        return Result.Success();
    }
}

public sealed record ConfigPatchResult(
    string TargetPath,
    string SourceSha256,
    string ResultSha256,
    long SourceBytes,
    long ResultBytes);
/// <summary>A bounded, redacted preview for one hash-pinned configuration replacement.</summary>
public sealed record ConfigPatchDiff(
    string TargetPath,
    string Content,
    bool IsTruncated,
    int TotalChangedLines);
/// <summary>Result of a patch that was immediately parser- and schema-validated.</summary>
public sealed record ValidatedConfigPatchResult(
    ConfigPatchResult Patch,
    ConfigValidationReport Validation,
    bool WasRolledBack);