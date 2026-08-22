using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Manifests;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Cli;

public static class CliApplication
{
    private static readonly UTF8Encoding Utf8WithoutReplacement = new(false, true);

    public static async Task<int> RunAsync(string[] arguments, TextWriter output, TextWriter error)
    {
        if (arguments is ["project", "validate", string manifestPath] && !string.IsNullOrWhiteSpace(manifestPath))
        {
            var manifest = LoadManifest(manifestPath);
            if (!manifest.IsSuccess)
            {
                await error.WriteLineAsync($"Manifest validation failed: {manifest.Error.Code}");
                return 1;
            }

            await output.WriteLineAsync($"Manifest valid: {manifest.Value!.Project.Id}");
            return 0;
        }

        if (arguments is ["project", "diagnose", string checkoutRoot, string diagnoseManifestPath] && !string.IsNullOrWhiteSpace(checkoutRoot) && !string.IsNullOrWhiteSpace(diagnoseManifestPath))
        {
            var manifest = LoadManifest(diagnoseManifestPath);
            if (!manifest.IsSuccess)
            {
                await error.WriteLineAsync($"Manifest validation failed: {manifest.Error.Code}");
                return 1;
            }

            try
            {
                var policy = ProjectPathPolicy.Create(manifest.Value!.Paths);
                if (!policy.IsSuccess)
                {
                    await error.WriteLineAsync($"Project policy failed: {policy.Error.Code}");
                    return 1;
                }

                var resolver = new ProjectPathResolver(checkoutRoot);
                var access = new ProjectPathAccessService(resolver, policy.Value!);
                var tree = new ProjectTreeService(resolver, access).List();
                if (!tree.IsSuccess)
                {
                    await error.WriteLineAsync($"Read-only diagnosis failed: {tree.Error.Code}");
                    return 1;
                }

                await output.WriteLineAsync($"Project: {manifest.Value.Project.Id}");
                await output.WriteLineAsync("Read-only checkout: verified");
                await output.WriteLineAsync($"Visible tree entries: {tree.Value!.Entries.Count}");
                await output.WriteLineAsync($"Tree truncated: {tree.Value.IsTruncated.ToString().ToLowerInvariant()}");
                await output.WriteLineAsync($"Oversized entries omitted: {tree.Value.HasOversizedEntries.ToString().ToLowerInvariant()}");
                return 0;
            }
            catch (ArgumentException)
            {
                await error.WriteLineAsync("Read-only diagnosis failed: path.invalid");
                return 1;
            }
        }

        await output.WriteLineAsync("Usage:");
        await output.WriteLineAsync("  patchpony project validate <manifest-file>");
        await output.WriteLineAsync("  patchpony project diagnose <checkout-root> <manifest-file>");
        return arguments.Length == 0 ? 0 : 2;
    }

    private static Result<ProjectManifest> LoadManifest(string manifestPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(manifestPath);
            return new ProjectManifestLoader().Load(Utf8WithoutReplacement.GetString(bytes));
        }
        catch (DecoderFallbackException)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest is not valid UTF-8 text."));
        }
        catch (IOException)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.unavailable", "The project manifest is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.unavailable", "The project manifest is unavailable."));
        }
    }
}