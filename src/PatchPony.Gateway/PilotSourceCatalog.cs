using System.Text.RegularExpressions;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Manifests;
using PatchPony.Infrastructure.Paths;
using PatchPony.Infrastructure.Source;

namespace PatchPony.Gateway;

public sealed class PilotSourceOptions
{
    public const string SectionName = "PatchPony:PilotSources";
    public PilotSourceProjectOptions[] Projects { get; init; } = [];

    public void Validate()
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var project in Projects)
        {
            if (!PilotSourceProjectOptions.IdPattern.IsMatch(project.Id) || !identifiers.Add(project.Id) || !Path.IsPathFullyQualified(project.CheckoutRoot) || !Path.IsPathFullyQualified(project.ManifestFile))
            {
                throw new InvalidOperationException("Pilot source configuration is invalid.");
            }
        }
    }
}

public sealed class PilotSourceProjectOptions
{
    internal static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    public string Id { get; init; } = string.Empty;
    public string CheckoutRoot { get; init; } = string.Empty;
    public string ManifestFile { get; init; } = string.Empty;
}

public sealed record PilotSourceProject(string Id, SourceReader Reader, RipgrepSourceSearchService Search);

public sealed class PilotSourceCatalog
{
    private readonly IReadOnlyDictionary<string, PilotSourceProject> projects;

    public PilotSourceCatalog(PilotSourceOptions options)
    {
        var catalog = new Dictionary<string, PilotSourceProject>(StringComparer.Ordinal);
        foreach (var configured in options.Projects)
        {
            if (!Directory.Exists(configured.CheckoutRoot) || !File.Exists(configured.ManifestFile))
            {
                throw new InvalidOperationException("A configured pilot source is unavailable.");
            }

            var manifest = new ProjectManifestLoader().Load(File.ReadAllText(configured.ManifestFile));
            if (!manifest.IsSuccess || !string.Equals(manifest.Value!.Project.Id, configured.Id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Pilot source configuration is invalid.");
            }

            var policy = ProjectPathPolicy.Create(manifest.Value.Paths);
            if (!policy.IsSuccess)
            {
                throw new InvalidOperationException("Pilot source configuration is invalid.");
            }

            var resolver = new ProjectPathResolver(configured.CheckoutRoot);
            var access = new ProjectPathAccessService(resolver, policy.Value!);
            catalog.Add(configured.Id, new PilotSourceProject(configured.Id, new SourceReader(access), new RipgrepSourceSearchService(resolver, access)));
        }

        projects = catalog;
    }

    public bool TryGet(string projectId, out PilotSourceProject project) => projects.TryGetValue(projectId, out project!);
}