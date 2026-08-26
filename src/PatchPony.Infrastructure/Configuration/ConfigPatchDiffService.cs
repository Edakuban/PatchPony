using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Configuration;

/// <summary>Creates bounded, redacted unified-diff previews for hash-pinned configuration patches.</summary>
public sealed partial class ConfigPatchDiffService
{
    public const int DefaultMaximumOutputBytes = 64 * 1024;
    public const int DefaultMaximumChangedLines = 200;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathPolicy policy;
    private readonly int maximumDocumentBytes;
    private readonly int maximumOutputBytes;
    private readonly int maximumChangedLines;

    public ConfigPatchDiffService(
        ProjectPathResolver resolver,
        ProjectPathPolicy policy,
        int maximumDocumentBytes = ConfigPatchService.DefaultMaximumDocumentBytes,
        int maximumOutputBytes = DefaultMaximumOutputBytes,
        int maximumChangedLines = DefaultMaximumChangedLines)
    {
        if (maximumDocumentBytes <= 0 || maximumOutputBytes <= 0 || maximumChangedLines <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDocumentBytes), "Diff limits must be positive.");
        }

        this.resolver = resolver;
        this.policy = policy;
        this.maximumDocumentBytes = maximumDocumentBytes;
        this.maximumOutputBytes = maximumOutputBytes;
        this.maximumChangedLines = maximumChangedLines;
    }

    public Result<ConfigPatchDiff> Create(ConfigPatchRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contract = request.ValidateContract();
        if (!contract.IsSuccess)
        {
            return Result<ConfigPatchDiff>.Failure(contract.Error);
        }

        if (request.ReplacementUtf8.Length > maximumDocumentBytes)
        {
            return Result<ConfigPatchDiff>.Failure(new DomainError("patch.replacement_too_large", "The replacement document exceeds the patch size limit."));
        }

        var resolved = resolver.Resolve(request.TargetPath);
        if (!resolved.IsSuccess)
        {
            return Result<ConfigPatchDiff>.Failure(resolved.Error);
        }

        var target = resolved.Value!;
        var authorization = policy.Authorize(target.RelativePath, ProjectPathAccess.Write);
        if (!authorization.IsSuccess)
        {
            return Result<ConfigPatchDiff>.Failure(authorization.Error);
        }

        if (!File.Exists(target.FullPath))
        {
            return Result<ConfigPatchDiff>.Failure(new DomainError("patch.target_not_found", "The patch target does not exist in the session worktree."));
        }

        try
        {
            if ((File.GetAttributes(target.FullPath) & FileAttributes.ReparsePoint) != 0)
            {
                return Result<ConfigPatchDiff>.Failure(new DomainError("patch.target_unsafe", "A patch target must not be a symbolic link or reparse point."));
            }

            var info = new FileInfo(target.FullPath);
            if (info.Length > maximumDocumentBytes)
            {
                return Result<ConfigPatchDiff>.Failure(new DomainError("patch.source_too_large", "The source document exceeds the patch size limit."));
            }

            var source = File.ReadAllBytes(target.FullPath);
            if (source.Length > maximumDocumentBytes)
            {
                return Result<ConfigPatchDiff>.Failure(new DomainError("patch.source_too_large", "The source document exceeds the patch size limit."));
            }

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(request.ExpectedSourceSha256), SHA256.HashData(source)))
            {
                return Result<ConfigPatchDiff>.Failure(DomainError.Conflict("patch.source_hash_mismatch", "The patch source checksum no longer matches the target file."));
            }

            var oldText = StrictUtf8.GetString(source);
            var newText = StrictUtf8.GetString(request.ReplacementUtf8.Span);
            return Result<ConfigPatchDiff>.Success(Build(target.RelativePath, oldText, newText));
        }
        catch (DecoderFallbackException)
        {
            return Result<ConfigPatchDiff>.Failure(new DomainError("patch.diff.invalid_utf8", "A configuration diff requires valid UTF-8 content."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<ConfigPatchDiff>.Failure(new DomainError("patch.io_failed", "The patch target could not be safely read."));
        }
    }

    private ConfigPatchDiff Build(string relativePath, string oldText, string newText)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);
        var totalChangedLines = oldLines.Count + newLines.Count;
        var content = new StringBuilder();
        var truncated = false;
        var emittedChangedLines = 0;

        Append("--- a/" + relativePath);
        Append("+++ b/" + relativePath);
        Append($"@@ -1,{oldLines.Count} +1,{newLines.Count} @@");
        foreach (var line in oldLines)
        {
            AppendChanged("-" + Redact(line));
        }

        foreach (var line in newLines)
        {
            AppendChanged("+" + Redact(line));
        }

        if (truncated)
        {
            Append("[... diff truncated ...]");
        }

        return new ConfigPatchDiff(relativePath, content.ToString(), truncated, totalChangedLines);

        void AppendChanged(string line)
        {
            if (truncated || emittedChangedLines >= maximumChangedLines)
            {
                truncated = true;
                return;
            }

            if (!Append(line))
            {
                truncated = true;
                return;
            }

            emittedChangedLines++;
        }

        bool Append(string line, bool ignoreLimits = false)
        {
            var rendered = line + "\n";
            if (!ignoreLimits && Encoding.UTF8.GetByteCount(content.ToString()) + Encoding.UTF8.GetByteCount(rendered) > maximumOutputBytes)
            {
                return false;
            }

            content.Append(rendered);
            return true;
        }
    }

    private static List<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').ToList();

    private static string Redact(string line)
    {
        var redacted = SensitiveAssignment().Replace(line, match => match.Groups["prefix"].Value + "[REDACTED]");
        return SensitiveXmlElement().Replace(redacted, match => match.Groups["open"].Value + "[REDACTED]" + match.Groups["close"].Value);
    }

    [GeneratedRegex("(?ix)(?<prefix>[\"'']?\\b(?:authorization|access[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?key|password|secret|token|private[_-]?key)\\b[\"'']?\\s*(?:=|:)\\s*)(?:\\\"[^\\\"]*\\\"|''[^'']*''|[^\\s,;&}\\]]+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex SensitiveAssignment();

    [GeneratedRegex("(?ix)(?<open><(?:authorization|access[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?key|password|secret|token|private[_-]?key)\\b[^>]*>)(?<value>.*?)(?<close></(?:authorization|access[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?key|password|secret|token|private[_-]?key)\\s*>)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex SensitiveXmlElement();
}
