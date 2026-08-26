using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

public sealed class SandboxCommandOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxCommands";
    public SandboxCommandOptionsEntry[] Commands { get; init; } = [];

    public SandboxCommandCatalog CreateCatalog(ISandboxRunnerCatalog runners) => new(runners, Commands.Select(command =>
        new SandboxCommandDefinition(command.Id, command.RunnerId, command.Executable, command.Arguments)));
}

public sealed class SandboxCommandOptionsEntry
{
    public string Id { get; init; } = string.Empty;
    public string RunnerId { get; init; } = string.Empty;
    public string Executable { get; init; } = string.Empty;
    public string[] Arguments { get; init; } = [];
}