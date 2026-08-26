using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

/// <summary>One server-owned forbidden pair of exact configuration values.</summary>
public sealed record ConfigurationConflictRule(
    string ProjectManifestId,
    ConfigurationEnvironment Environment,
    string LeftKey,
    string LeftValue,
    string RightKey,
    string RightValue)
{
    public static Result<ConfigurationConflictRule> Create(string projectManifestId, ConfigurationEnvironment environment, string leftKey, string leftValue, string rightKey, string rightValue)
    {
        if (!IsProjectId(projectManifestId) || !IsKey(leftKey) || !IsKey(rightKey) || leftKey == rightKey || !IsValue(leftValue) || !IsValue(rightValue))
            return Result<ConfigurationConflictRule>.Failure(new DomainError("config.conflict_policy.invalid", "The configuration conflict policy is invalid."));
        return Result<ConfigurationConflictRule>.Success(new ConfigurationConflictRule(projectManifestId, environment, leftKey, leftValue, rightKey, rightValue));
    }

    private static bool IsProjectId(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 63 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    private static bool IsKey(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.StartsWith("/", StringComparison.Ordinal) && !value.Contains((char)92) && !value.Contains("..", StringComparison.Ordinal) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or '/');
    private static bool IsValue(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t');
}

/// <summary>Checks a key-validated configuration request against configured forbidden value pairs.</summary>
public static class ConfigurationChangeConflictDetector
{
    public static Result Validate(EnvironmentConfigurationChangeRequest request, ConfigurationChangeKeyValidation keyValidation, IReadOnlyList<ConfigurationConflictRule> rules)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(keyValidation);
        ArgumentNullException.ThrowIfNull(rules);
        if (keyValidation.Targets.Count != request.ChangeRequest.Targets.Count || !keyValidation.Targets.Select(target => target.Key).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(request.ChangeRequest.Targets.Select(target => target.Reference).OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
            return Result.Failure(new DomainError("config.conflict_input.invalid", "The configuration conflict input is invalid."));

        var applicable = rules.Where(rule => rule.ProjectManifestId == request.ChangeRequest.ProjectManifestId && rule.Environment == request.Environment).ToArray();
        if (applicable.Any(rule => rule is null) || applicable.Select(rule => (rule.LeftKey, rule.LeftValue, rule.RightKey, rule.RightValue)).Distinct().Count() != applicable.Length)
            return Result.Failure(new DomainError("config.conflict_policy.invalid", "The configuration conflict policy is invalid."));

        var requested = request.ChangeRequest.Targets.ToDictionary(target => target.Reference, target => target.RequestedValue!, StringComparer.Ordinal);
        foreach (var rule in applicable)
        {
            if (requested.TryGetValue(rule.LeftKey, out var left) && requested.TryGetValue(rule.RightKey, out var right) && left == rule.LeftValue && right == rule.RightValue)
                return Result.Failure(new DomainError("config.values.conflict", "The requested configuration values conflict."));
        }
        return Result.Success();
    }
}