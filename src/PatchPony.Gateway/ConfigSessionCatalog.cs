using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Configuration;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Configuration;
using PatchPony.Infrastructure.Manifests;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Gateway;

public sealed class ConfigSessionOptions
{
    public const string SectionName = "PatchPony:ConfigSessions";
    public ConfigSessionEntryOptions[] Sessions { get; init; } = [];

    public void Validate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Sessions)
        {
            if (!Guid.TryParseExact(entry.SessionId, "N", out _) || !PilotSourceProjectOptions.IdPattern.IsMatch(entry.ProjectId) ||
                !ids.Add(entry.SessionId) || !Path.IsPathFullyQualified(entry.WorktreeRoot) || !Path.IsPathFullyQualified(entry.ManifestFile))
            {
                throw new InvalidOperationException("Config session configuration is invalid.");
            }
        }
    }
}

public sealed class ConfigSessionEntryOptions
{
    public string SessionId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string WorktreeRoot { get; init; } = string.Empty;
    public string ManifestFile { get; init; } = string.Empty;
}

public sealed record ConfigValidateCommand(string Path, string Format, string? SchemaId = null);
public sealed record ConfigPatchCommand(string Path, string Format, string ExpectedSourceSha256, string ReplacementBase64, string? SchemaId = null);

public sealed class ConfigSessionCatalog
{
    private const int MaximumDocumentBytes = ConfigPatchService.DefaultMaximumDocumentBytes;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly IReadOnlyDictionary<(string ProjectId, string SessionId), ConfigSession> sessions;

    public ConfigSessionCatalog(ConfigSessionOptions options)
    {
        var mapped = new Dictionary<(string, string), ConfigSession>();
        foreach (var configured in options.Sessions)
        {
            if (!Directory.Exists(configured.WorktreeRoot) || !File.Exists(configured.ManifestFile))
            {
                throw new InvalidOperationException("A configured config session is unavailable.");
            }

            var manifest = new ProjectManifestLoader().Load(File.ReadAllText(configured.ManifestFile));
            var policy = manifest.IsSuccess ? ProjectPathPolicy.Create(manifest.Value!.Paths) : Result<ProjectPathPolicy>.Failure(manifest.Error);
            if (!manifest.IsSuccess || !string.Equals(manifest.Value!.Project.Id, configured.ProjectId, StringComparison.Ordinal) || !policy.IsSuccess)
            {
                throw new InvalidOperationException("Config session configuration is invalid.");
            }

            var resolver = new ProjectPathResolver(configured.WorktreeRoot);
            var adapters = new ConfigAdapterCatalog([new JsonConfigAdapter(), new YamlConfigAdapter(), new XmlConfigAdapter()]);
            var patches = new ConfigPatchService(resolver, policy.Value!);
            var session = new ConfigSession(resolver, policy.Value!, adapters, new ValidatedConfigPatchService(patches, adapters, resolver, policy.Value!));
            if (!mapped.TryAdd((configured.ProjectId, configured.SessionId), session))
            {
                throw new InvalidOperationException("Config session configuration is invalid.");
            }
        }

        sessions = mapped;
    }

    public Result<ConfigValidationReport> Validate(string projectId, string sessionId, ConfigValidateCommand command, CancellationToken cancellationToken = default)
    {
        if (!TryGet(projectId, sessionId, out var session)) return SessionNotFound<ConfigValidationReport>();
        if (!TryFormat(command.Format, out var format)) return Result<ConfigValidationReport>.Failure(DomainError.Validation("A supported configuration document format is required."));
        var target = ResolveForRead(session, command.Path);
        if (!target.IsSuccess) return Result<ConfigValidationReport>.Failure(target.Error);

        try
        {
            var bytes = File.ReadAllBytes(target.Value!.FullPath);
            if (bytes.Length > MaximumDocumentBytes) return Result<ConfigValidationReport>.Failure(new DomainError("config.too_large", "The configuration document exceeds the tool limit."));
            _ = StrictUtf8.GetString(bytes);
            return session.Adapters.Validate(new ConfigValidationRequest(format, bytes, command.SchemaId), cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.invalid_utf8", "Configuration documents must be UTF-8."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.read_failed", "The configuration document could not be safely read."));
        }
    }

    public Result<ValidatedConfigPatchResult> Patch(string projectId, string sessionId, ConfigPatchCommand command, CancellationToken cancellationToken = default)
    {
        if (!TryGet(projectId, sessionId, out var session)) return SessionNotFound<ValidatedConfigPatchResult>();
        if (!TryFormat(command.Format, out var format)) return Result<ValidatedConfigPatchResult>.Failure(DomainError.Validation("A supported configuration document format is required."));
        if (command.ReplacementBase64.Length > 1_398_104) return Result<ValidatedConfigPatchResult>.Failure(new DomainError("patch.replacement_too_large", "The replacement document exceeds the patch size limit."));

        try
        {
            var replacement = Convert.FromBase64String(command.ReplacementBase64);
            if (replacement.Length > MaximumDocumentBytes) return Result<ValidatedConfigPatchResult>.Failure(new DomainError("patch.replacement_too_large", "The replacement document exceeds the patch size limit."));
            _ = StrictUtf8.GetString(replacement);
            return session.Patches.Apply(new ConfigPatchRequest(command.Path, command.ExpectedSourceSha256, format, replacement, command.SchemaId), cancellationToken);
        }
        catch (FormatException)
        {
            return Result<ValidatedConfigPatchResult>.Failure(DomainError.Validation("The replacement document must be base64-encoded UTF-8."));
        }
        catch (DecoderFallbackException)
        {
            return Result<ValidatedConfigPatchResult>.Failure(DomainError.Validation("The replacement document must be valid UTF-8."));
        }
    }

    private static Result<ResolvedProjectPath> ResolveForRead(ConfigSession session, string path)
    {
        var resolved = session.Resolver.Resolve(path);
        if (!resolved.IsSuccess) return resolved;
        var authorization = session.Policy.Authorize(resolved.Value!.RelativePath, ProjectPathAccess.Read);
        if (!authorization.IsSuccess) return Result<ResolvedProjectPath>.Failure(authorization.Error);
        if (!File.Exists(resolved.Value.FullPath)) return Result<ResolvedProjectPath>.Failure(new DomainError("config.not_found", "The configuration document does not exist."));
        return Result<ResolvedProjectPath>.Success(resolved.Value);
    }

    private bool TryGet(string projectId, string sessionId, out ConfigSession session) => sessions.TryGetValue((projectId, sessionId), out session!);
    private static bool TryFormat(string value, out ConfigDocumentFormat format) => Enum.TryParse(value, ignoreCase: true, out format) && Enum.IsDefined(format);
    private static Result<T> SessionNotFound<T>() => Result<T>.Failure(new DomainError("session.not_found", "The requested config session is not available."));
    private sealed record ConfigSession(ProjectPathResolver Resolver, ProjectPathPolicy Policy, ConfigAdapterCatalog Adapters, ValidatedConfigPatchService Patches);
}