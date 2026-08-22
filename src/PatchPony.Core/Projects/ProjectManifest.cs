namespace PatchPony.Core.Projects;

public sealed record ProjectManifest(
    int SchemaVersion,
    ProjectManifestProject Project,
    ProjectManifestRepository Repository,
    ProjectManifestPaths Paths);

public sealed record ProjectManifestProject(string Id, string DisplayName);

public sealed record ProjectManifestRepository(Uri RemoteUri, string DefaultBranch);

public sealed record ProjectManifestPaths(
    IReadOnlyList<string> Readable,
    IReadOnlyList<string> Writable,
    IReadOnlyList<string> Forbidden);
