using System.Globalization;

namespace PatchPony.Worker;

/// <summary>Mandatory resource ceilings for one sandbox container, owned by the worker host.</summary>
public sealed record SandboxResourceLimits(double CpuCount, long MemoryBytes, int PidsLimit, long DiskBytes, TimeSpan ExecutionTimeout)
{
    public string CpuArgument => CpuCount.ToString("0.###", CultureInfo.InvariantCulture);
}

public sealed class SandboxResourceLimitOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxLimits";
    public double CpuCount { get; init; }
    public long MemoryBytes { get; init; }
    public int PidsLimit { get; init; }
    public long DiskBytes { get; init; }
    public TimeSpan ExecutionTimeout { get; init; }

    public SandboxResourceLimits CreateLimits()
    {
        if (CpuCount is < 0.1 or > 8 || MemoryBytes is < 64 * 1024 * 1024 or > 8L * 1024 * 1024 * 1024 ||
            PidsLimit is < 16 or > 1_024 || DiskBytes is < 128L * 1024 * 1024 or > 16L * 1024 * 1024 * 1024 ||
            ExecutionTimeout < TimeSpan.FromSeconds(1) || ExecutionTimeout > TimeSpan.FromHours(1))
        {
            throw new InvalidOperationException("Sandbox resource limits must use bounded CPU, memory, PID, disk and timeout values.");
        }

        return new SandboxResourceLimits(CpuCount, MemoryBytes, PidsLimit, DiskBytes, ExecutionTimeout);
    }
}