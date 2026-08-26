using System.Security.Cryptography;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Configuration;

/// <summary>Applies bounded, hash-pinned full-document replacements inside a resolved worktree.</summary>
public sealed class ConfigPatchService
{
    public const int DefaultMaximumDocumentBytes = 1024 * 1024;

    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathPolicy policy;
    private readonly int maximumDocumentBytes;

    public ConfigPatchService(ProjectPathResolver resolver, ProjectPathPolicy policy, int maximumDocumentBytes = DefaultMaximumDocumentBytes)
    {
        if (maximumDocumentBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDocumentBytes), "The maximum document size must be positive.");
        }

        this.resolver = resolver;
        this.policy = policy;
        this.maximumDocumentBytes = maximumDocumentBytes;
    }

    public Result<ConfigPatchResult> Apply(ConfigPatchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contract = request.ValidateContract();
        if (!contract.IsSuccess)
        {
            return Result<ConfigPatchResult>.Failure(contract.Error);
        }

        if (request.ReplacementUtf8.Length > maximumDocumentBytes)
        {
            return Result<ConfigPatchResult>.Failure(new DomainError("patch.replacement_too_large", "The replacement document exceeds the patch size limit."));
        }

        var resolved = resolver.Resolve(request.TargetPath);
        if (!resolved.IsSuccess)
        {
            return Result<ConfigPatchResult>.Failure(resolved.Error);
        }

        var target = resolved.Value!;
        var authorization = policy.Authorize(target.RelativePath, ProjectPathAccess.Write);
        if (!authorization.IsSuccess)
        {
            return Result<ConfigPatchResult>.Failure(authorization.Error);
        }

        if (!MatchesFormat(target.RelativePath, request.Format))
        {
            return Result<ConfigPatchResult>.Failure(new DomainError("patch.format_mismatch", "The target file extension does not match the expected configuration format."));
        }

        if (!File.Exists(target.FullPath))
        {
            return Result<ConfigPatchResult>.Failure(new DomainError("patch.target_not_found", "The patch target does not exist in the session worktree."));
        }

        try
        {
            if ((File.GetAttributes(target.FullPath) & FileAttributes.ReparsePoint) != 0)
            {
                return Result<ConfigPatchResult>.Failure(new DomainError("patch.target_unsafe", "A patch target must not be a symbolic link or reparse point."));
            }

            var info = new FileInfo(target.FullPath);
            if (info.Length > maximumDocumentBytes)
            {
                return Result<ConfigPatchResult>.Failure(new DomainError("patch.source_too_large", "The source document exceeds the patch size limit."));
            }

            var source = File.ReadAllBytes(target.FullPath);
            if (source.Length > maximumDocumentBytes)
            {
                return Result<ConfigPatchResult>.Failure(new DomainError("patch.source_too_large", "The source document exceeds the patch size limit."));
            }

            var sourceHash = ToSha256(source);
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(request.ExpectedSourceSha256), SHA256.HashData(source)))
            {
                return Result<ConfigPatchResult>.Failure(DomainError.Conflict("patch.source_hash_mismatch", "The patch source checksum no longer matches the target file."));
            }

            cancellationToken.ThrowIfCancellationRequested();
            WriteAtomically(target.FullPath, request.ReplacementUtf8.Span);
            var resultHash = ToSha256(request.ReplacementUtf8.Span);
            return Result<ConfigPatchResult>.Success(new ConfigPatchResult(
                target.RelativePath,
                sourceHash,
                resultHash,
                source.Length,
                request.ReplacementUtf8.Length));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<ConfigPatchResult>.Failure(new DomainError("patch.io_failed", "The patch target could not be safely updated."));
        }
    }


    /// <summary>Restores the captured source only if the target still contains this patch result.</summary>
    public Result Restore(ConfigPatchResult appliedPatch, ReadOnlyMemory<byte> originalUtf8, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (originalUtf8.Length > maximumDocumentBytes || !string.Equals(ToSha256(originalUtf8.Span), appliedPatch.SourceSha256, StringComparison.Ordinal))
        {
            return Result.Failure(new DomainError("patch.rollback.invalid_source", "The captured rollback source is invalid."));
        }

        var resolved = resolver.Resolve(appliedPatch.TargetPath);
        if (!resolved.IsSuccess)
        {
            return Result.Failure(resolved.Error);
        }

        var target = resolved.Value!;
        var authorization = policy.Authorize(target.RelativePath, ProjectPathAccess.Write);
        if (!authorization.IsSuccess)
        {
            return authorization;
        }

        try
        {
            if (!File.Exists(target.FullPath) || (File.GetAttributes(target.FullPath) & FileAttributes.ReparsePoint) != 0)
            {
                return Result.Failure(new DomainError("patch.rollback.unavailable", "The patch target is unavailable for rollback."));
            }

            var current = File.ReadAllBytes(target.FullPath);
            if (current.Length > maximumDocumentBytes)
            {
                return Result.Failure(new DomainError("patch.rollback.unavailable", "The patch target is unavailable for rollback."));
            }

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(appliedPatch.ResultSha256), SHA256.HashData(current)))
            {
                return Result.Failure(DomainError.Conflict("patch.rollback_conflict", "The patch target changed after validation and was not overwritten."));
            }

            WriteAtomically(target.FullPath, originalUtf8.Span);
            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result.Failure(new DomainError("patch.rollback.failed", "The invalid patch could not be safely reverted."));
        }
    }
    private static bool MatchesFormat(string relativePath, ConfigDocumentFormat format)
    {
        var extension = Path.GetExtension(relativePath);
        return format switch
        {
            ConfigDocumentFormat.Json => string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase),
            ConfigDocumentFormat.Yaml => string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase),
            ConfigDocumentFormat.Xml => string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static void WriteAtomically(string targetPath, ReadOnlySpan<byte> replacement)
    {
        var directory = Path.GetDirectoryName(targetPath) ?? throw new IOException("The patch target has no parent directory.");
        var temporary = Path.Combine(directory, $".patchpony-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(replacement);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static string ToSha256(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}