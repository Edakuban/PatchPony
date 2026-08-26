using PatchPony.Core.Common;

namespace PatchPony.Core.Configuration;

public enum ConfigDocumentFormat
{
    Json,
    Yaml,
    Xml
}

public enum ConfigValidationSeverity
{
    Warning,
    Error
}

public sealed record ConfigValidationIssue(string Code, ConfigValidationSeverity Severity, string Message, int? Line = null, int? Column = null);

public sealed record ConfigValidationReport(ConfigDocumentFormat Format, bool IsValid, IReadOnlyList<ConfigValidationIssue> Issues);

/// <summary>A format-specific read-only parser and validator.</summary>
public interface IConfigDocumentAdapter
{
    ConfigDocumentFormat Format { get; }

    Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>SchemaId is an opaque server-selected reference; raw schemas and paths are deliberately excluded.</summary>
public sealed record ConfigValidationRequest(ConfigDocumentFormat Format, ReadOnlyMemory<byte> Utf8Content, string? SchemaId = null)
{
    public Result ValidateContract()
    {
        if (!Enum.IsDefined(Format))
        {
            return Result.Failure(DomainError.Validation("A supported configuration document format is required."));
        }

        if (SchemaId is { Length: > 128 } || SchemaId?.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '_' or '-')) == true)
        {
            return Result.Failure(DomainError.Validation("A schema reference must be a bounded server-side identifier."));
        }

        return Result.Success();
    }
}

/// <summary>Closed, server-composed adapter catalog; no adapter is selected from client input.</summary>
public sealed class ConfigAdapterCatalog
{
    private readonly IReadOnlyDictionary<ConfigDocumentFormat, IConfigDocumentAdapter> adapters;

    public ConfigAdapterCatalog(IEnumerable<IConfigDocumentAdapter> adapters)
    {
        var mapped = new Dictionary<ConfigDocumentFormat, IConfigDocumentAdapter>();
        foreach (var adapter in adapters)
        {
            if (!mapped.TryAdd(adapter.Format, adapter))
            {
                throw new ArgumentException($"Only one adapter may be registered for {adapter.Format}.", nameof(adapters));
            }
        }

        this.adapters = mapped;
    }

    public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default)
    {
        var contract = request.ValidateContract();
        if (!contract.IsSuccess)
        {
            return Result<ConfigValidationReport>.Failure(contract.Error);
        }

        if (!adapters.TryGetValue(request.Format, out var adapter))
        {
            return Result<ConfigValidationReport>.Failure(new DomainError("config.adapter.unsupported", "No server-side adapter is registered for this configuration format."));
        }

        var report = adapter.Validate(request, cancellationToken);
        if (!report.IsSuccess)
        {
            return report;
        }

        return report.Value!.Format == request.Format
            ? report
            : Result<ConfigValidationReport>.Failure(new DomainError("config.adapter.invalid_report", "The configuration adapter returned a mismatched format report."));
    }
}