using System.Security.Cryptography;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Configuration;

/// <summary>Applies a config patch, validates the exact result, and reverts invalid changes atomically.</summary>
public sealed class ValidatedConfigPatchService
{
    private readonly ConfigPatchService patches;
    private readonly ConfigAdapterCatalog adapters;
    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathPolicy policy;
    private readonly int maximumDocumentBytes;

    public ValidatedConfigPatchService(
        ConfigPatchService patches,
        ConfigAdapterCatalog adapters,
        ProjectPathResolver resolver,
        ProjectPathPolicy policy,
        int maximumDocumentBytes = ConfigPatchService.DefaultMaximumDocumentBytes)
    {
        this.patches = patches;
        this.adapters = adapters;
        this.resolver = resolver;
        this.policy = policy;
        this.maximumDocumentBytes = maximumDocumentBytes;
    }

    public Result<ValidatedConfigPatchResult> Apply(ConfigPatchRequest request, CancellationToken cancellationToken = default)
    {
        var source = CaptureSource(request, cancellationToken);
        if (!source.IsSuccess)
        {
            return Result<ValidatedConfigPatchResult>.Failure(source.Error);
        }

        var applied = patches.Apply(request, cancellationToken);
        if (!applied.IsSuccess)
        {
            return Result<ValidatedConfigPatchResult>.Failure(applied.Error);
        }

        var validation = adapters.Validate(new ConfigValidationRequest(request.Format, request.ReplacementUtf8, request.SchemaId), cancellationToken);
        if (validation.IsSuccess && validation.Value!.IsValid)
        {
            return Result<ValidatedConfigPatchResult>.Success(new ValidatedConfigPatchResult(applied.Value!, validation.Value, false));
        }

        var rollback = patches.Restore(applied.Value!, source.Value!, cancellationToken);
        if (!rollback.IsSuccess)
        {
            return Result<ValidatedConfigPatchResult>.Failure(rollback.Error);
        }

        if (!validation.IsSuccess)
        {
            return Result<ValidatedConfigPatchResult>.Failure(new DomainError("patch.validation_failed", "The patch could not be validated and was reverted."));
        }

        return Result<ValidatedConfigPatchResult>.Success(new ValidatedConfigPatchResult(applied.Value!, validation.Value!, true));
    }

    private Result<byte[]> CaptureSource(ConfigPatchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contract = request.ValidateContract();
        if (!contract.IsSuccess)
        {
            return Result<byte[]>.Failure(contract.Error);
        }

        var resolved = resolver.Resolve(request.TargetPath);
        if (!resolved.IsSuccess)
        {
            return Result<byte[]>.Failure(resolved.Error);
        }

        var target = resolved.Value!;
        var authorization = policy.Authorize(target.RelativePath, ProjectPathAccess.Write);
        if (!authorization.IsSuccess)
        {
            return Result<byte[]>.Failure(authorization.Error);
        }

        try
        {
            if (!File.Exists(target.FullPath) || (File.GetAttributes(target.FullPath) & FileAttributes.ReparsePoint) != 0)
            {
                return Result<byte[]>.Failure(new DomainError("patch.target_not_found", "The patch target does not exist in the session worktree."));
            }

            var source = File.ReadAllBytes(target.FullPath);
            if (source.Length > maximumDocumentBytes)
            {
                return Result<byte[]>.Failure(new DomainError("patch.source_too_large", "The source document exceeds the patch size limit."));
            }

            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(request.ExpectedSourceSha256), SHA256.HashData(source))
                ? Result<byte[]>.Success(source)
                : Result<byte[]>.Failure(DomainError.Conflict("patch.source_hash_mismatch", "The patch source checksum no longer matches the target file."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<byte[]>.Failure(new DomainError("patch.io_failed", "The patch target could not be safely read."));
        }
    }
}