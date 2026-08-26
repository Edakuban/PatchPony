using PatchPony.Core.Sandbox;

namespace PatchPony.Worker;

/// <summary>Server-owned runner registrations. Requests can only select a registered ID in later I9 steps.</summary>
public sealed class SandboxRunnerOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxRunners";
    public SandboxRunnerOptionsEntry[] Runners { get; init; } = [];

    public SandboxRunnerCatalog CreateCatalog() => new(Runners.Select(runner => new SandboxRunnerImage(runner.Id, runner.ImageReference, runner.RootlessCompatible)));
}

public sealed class SandboxRunnerOptionsEntry
{
    public string Id { get; init; } = string.Empty;
    public string ImageReference { get; init; } = string.Empty;
    public bool RootlessCompatible { get; init; }
}