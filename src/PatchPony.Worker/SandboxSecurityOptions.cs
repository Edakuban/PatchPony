using System.Text.RegularExpressions;

namespace PatchPony.Worker;

/// <summary>Host-owned mandatory security profiles for every sandbox container.</summary>
public sealed record SandboxSecurityProfile(string SeccompProfilePath, string AppArmorProfile);

public sealed class SandboxSecurityOptions
{
    public const string SectionName = "PatchPony:Worker:SandboxSecurity";
    private static readonly Regex AppArmorProfilePattern = new("^[a-z0-9][a-z0-9._-]{0,127}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public string SeccompProfilePath { get; init; } = string.Empty;
    public string AppArmorProfile { get; init; } = string.Empty;

    public SandboxSecurityProfile CreateProfile()
    {
        if (!Path.IsPathFullyQualified(SeccompProfilePath) || !File.Exists(SeccompProfilePath) || !AppArmorProfilePattern.IsMatch(AppArmorProfile))
        {
            throw new InvalidOperationException("Sandbox security configuration requires an existing absolute seccomp profile and a valid AppArmor profile name.");
        }

        return new SandboxSecurityProfile(Path.GetFullPath(SeccompProfilePath), AppArmorProfile);
    }
}