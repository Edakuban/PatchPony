using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Sandbox;

/// <summary>Server-owned static command definition. Arguments cannot be supplied by sandbox requests.</summary>
public sealed record SandboxCommandDefinition(string Id, string RunnerId, string Executable, IReadOnlyList<string> Arguments);
public sealed record SandboxCommand(string Id, SandboxRunnerImage Runner, string Executable, IReadOnlyList<string> Arguments);

public interface ISandboxCommandCatalog
{
    Result<SandboxCommand> Resolve(string commandId);
}

/// <summary>Closed command catalog mapped to registered runner images at composition time.</summary>
public sealed class SandboxCommandCatalog : ISandboxCommandCatalog
{
    private static readonly Regex IdPattern = new("^[a-z][a-z0-9._-]{0,127}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex ExecutablePattern = new("^/[a-z0-9._/-]{1,255}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private readonly IReadOnlyDictionary<string, SandboxCommand> commands;

    public SandboxCommandCatalog(ISandboxRunnerCatalog runners, IEnumerable<SandboxCommandDefinition> definitions)
    {
        var mapped = new Dictionary<string, SandboxCommand>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (!IdPattern.IsMatch(definition.Id) || !ExecutablePattern.IsMatch(definition.Executable) || IsShell(definition.Executable) ||
                definition.Arguments is null || definition.Arguments.Count > 32 || definition.Arguments.Any(argument => string.IsNullOrWhiteSpace(argument) || argument.Length > 512 || argument.Any(character => char.IsControl(character))))
            {
                throw new ArgumentException("Sandbox commands must be bounded static non-shell definitions.", nameof(definitions));
            }

            var runner = runners.Resolve(definition.RunnerId);
            if (!runner.IsSuccess || !mapped.TryAdd(definition.Id, new SandboxCommand(definition.Id, runner.Value!, definition.Executable, definition.Arguments.ToArray())))
            {
                throw new ArgumentException("Sandbox commands must use unique IDs and registered runners.", nameof(definitions));
            }
        }

        commands = mapped;
    }

    public Result<SandboxCommand> Resolve(string commandId) =>
        commands.TryGetValue(commandId, out var command)
            ? Result<SandboxCommand>.Success(command)
            : Result<SandboxCommand>.Failure(new DomainError("sandbox.command.unsupported", "The requested sandbox command is not registered."));

    private static bool IsShell(string executable) => executable is "/bin/sh" or "/bin/bash" or "/usr/bin/sh" or "/usr/bin/bash";
}