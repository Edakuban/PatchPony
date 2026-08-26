using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Source;

public sealed record SourceReadLine(int Number, string Text);

public sealed record SourceReadResult(string RelativePath, IReadOnlyList<SourceReadLine> Lines, bool IsTruncated);

public sealed class SourceReader(ProjectPathAccessService access)
{
    private const int MaximumFileBytes = 128 * 1024;
    private const int MaximumLines = 500;
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(5);
    private static readonly UTF8Encoding Utf8WithoutReplacement = new(false, true);

    public Result<SourceReadResult> Read(string relativePath)
    {
        var authorizedFile = access.ResolveAndAuthorize(relativePath, ProjectPathAccess.Read);
        if (!authorizedFile.IsSuccess)
        {
            return Result<SourceReadResult>.Failure(authorizedFile.Error);
        }

        if (!File.Exists(authorizedFile.Value!.FullPath))
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.not_found", "The requested source file was not found."));
        }

        try
        {
            var info = new FileInfo(authorizedFile.Value.FullPath);
            if (info.Length > MaximumFileBytes)
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.too_large", "The requested source file exceeds the configured byte limit."));
            }

            var bytes = File.ReadAllBytes(authorizedFile.Value.FullPath);
            if (bytes.Length > MaximumFileBytes)
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.too_large", "The requested source file exceeds the configured byte limit."));
            }

            if (bytes.Contains((byte)0))
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.binary", "The requested source file is binary and cannot be returned as text."));
            }

            var text = Utf8WithoutReplacement.GetString(bytes).TrimStart('\uFEFF');
            var allLines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            var lines = allLines.Take(MaximumLines)
                .Select((line, index) => new SourceReadLine(index + 1, line))
                .ToArray();

            return Result<SourceReadResult>.Success(new SourceReadResult(
                authorizedFile.Value.RelativePath,
                lines,
                allLines.Length > MaximumLines));
        }
        catch (DecoderFallbackException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.invalid_encoding", "The requested source file is not valid UTF-8 text."));
        }
        catch (IOException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.unavailable", "The requested source file is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.unavailable", "The requested source file is unavailable."));
        }
    }

    public async Task<Result<SourceReadResult>> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var authorizedFile = access.ResolveAndAuthorize(relativePath, ProjectPathAccess.Read);
        if (!authorizedFile.IsSuccess)
        {
            return Result<SourceReadResult>.Failure(authorizedFile.Error);
        }

        if (!File.Exists(authorizedFile.Value!.FullPath))
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.not_found", "The requested source file was not found."));
        }

        try
        {
            if (new FileInfo(authorizedFile.Value.FullPath).Length > MaximumFileBytes)
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.too_large", "The requested source file exceeds the configured byte limit."));
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ReadTimeout);
            var bytes = await File.ReadAllBytesAsync(authorizedFile.Value.FullPath, timeout.Token);
            if (bytes.Length > MaximumFileBytes)
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.too_large", "The requested source file exceeds the configured byte limit."));
            }

            if (bytes.Contains((byte)0))
            {
                return Result<SourceReadResult>.Failure(new DomainError("source.binary", "The requested source file is binary and cannot be returned as text."));
            }

            var allLines = Utf8WithoutReplacement.GetString(bytes).TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            var lines = allLines.Take(MaximumLines).Select((line, index) => new SourceReadLine(index + 1, line)).ToArray();
            return Result<SourceReadResult>.Success(new SourceReadResult(authorizedFile.Value.RelativePath, lines, allLines.Length > MaximumLines));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.timeout", "The controlled source read timed out."));
        }
        catch (DecoderFallbackException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.invalid_encoding", "The requested source file is not valid UTF-8 text."));
        }
        catch (IOException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.unavailable", "The requested source file is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<SourceReadResult>.Failure(new DomainError("source.unavailable", "The requested source file is unavailable."));
        }
    }
}
