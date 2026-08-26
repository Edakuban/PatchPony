using System.Globalization;
using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum ConfigurationValueType { Boolean, Integer, String, Enum }

public sealed record KnownConfigurationKeyPolicy(
    string ProjectManifestId,
    ConfigurationEnvironment Environment,
    string Key,
    ConfigurationValueType ValueType,
    long? Minimum,
    long? Maximum,
    int? MaximumLength,
    IReadOnlyList<string> AllowedValues)
{
    public static Result<KnownConfigurationKeyPolicy> Create(string projectManifestId, ConfigurationEnvironment environment, string key, ConfigurationValueType valueType, long? minimum = null, long? maximum = null, int? maximumLength = null, IReadOnlyList<string>? allowedValues = null)
    {
        var values = allowedValues ?? [];
        if (!IsProjectId(projectManifestId) || !IsKey(key) || !Enum.IsDefined(valueType) || (minimum is not null && maximum is not null && minimum > maximum) || (maximumLength is not null && maximumLength is < 1 or > 512) || (valueType == ConfigurationValueType.Enum && (values.Count is < 1 or > 32 || values.Any(value => !IsValue(value)) || values.Distinct(StringComparer.Ordinal).Count() != values.Count)) || (valueType != ConfigurationValueType.Enum && values.Count != 0) || (valueType is ConfigurationValueType.Boolean or ConfigurationValueType.String && (minimum is not null || maximum is not null)) || (valueType != ConfigurationValueType.String && maximumLength is not null))
            return Result<KnownConfigurationKeyPolicy>.Failure(new DomainError("config.key_policy.invalid", "The configuration key policy is invalid."));
        return Result<KnownConfigurationKeyPolicy>.Success(new KnownConfigurationKeyPolicy(projectManifestId, environment, key, valueType, minimum, maximum, maximumLength, values.ToArray()));
    }

    private static bool IsProjectId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 63 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    private static bool IsKey(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.StartsWith("/", StringComparison.Ordinal) && !value.Contains((char)92) && !value.Contains("..", StringComparison.Ordinal) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or '/');
    private static bool IsValue(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t');
}

public sealed record ValidatedConfigurationChangeTarget(string Key, ConfigurationValueType ValueType);
public sealed record ConfigurationChangeKeyValidation(IReadOnlyList<ValidatedConfigurationChangeTarget> Targets);

/// <summary>Validates only explicitly allowlisted configuration keys and values for one project environment.</summary>
public static class ConfigurationChangeKeyValidator
{
    public static Result<ConfigurationChangeKeyValidation> Validate(EnvironmentConfigurationChangeRequest request, IReadOnlyList<KnownConfigurationKeyPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policies);
        var applicable = policies.Where(policy => policy.ProjectManifestId == request.ChangeRequest.ProjectManifestId && policy.Environment == request.Environment).ToArray();
        if (applicable.Length == 0 || applicable.Any(policy => policy is null)) return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.key_policy.unconfigured", "No configuration key policy is configured."));
        if (applicable.Select(policy => policy.Key).Distinct(StringComparer.Ordinal).Count() != applicable.Length) return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.key_policy.unconfigured", "No configuration key policy is configured."));

        var validated = new List<ValidatedConfigurationChangeTarget>();
        foreach (var target in request.ChangeRequest.Targets)
        {
            var policy = applicable.SingleOrDefault(candidate => candidate.Key == target.Reference);
            if (policy is null) return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.key.unknown", "The configuration key is not allowlisted."));
            if (target.RequestedValue is null || !ValueMatches(policy, target.RequestedValue)) return Result<ConfigurationChangeKeyValidation>.Failure(new DomainError("config.value.invalid", "The configuration value is invalid."));
            validated.Add(new ValidatedConfigurationChangeTarget(policy.Key, policy.ValueType));
        }
        return Result<ConfigurationChangeKeyValidation>.Success(new ConfigurationChangeKeyValidation(validated));
    }

    private static bool ValueMatches(KnownConfigurationKeyPolicy policy, string value) => policy.ValueType switch
    {
        ConfigurationValueType.Boolean => value is "true" or "false",
        ConfigurationValueType.Integer => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) && (policy.Minimum is null || integer >= policy.Minimum) && (policy.Maximum is null || integer <= policy.Maximum),
        ConfigurationValueType.String => value.Length <= (policy.MaximumLength ?? 512),
        ConfigurationValueType.Enum => policy.AllowedValues.Contains(value, StringComparer.Ordinal),
        _ => false
    };
}