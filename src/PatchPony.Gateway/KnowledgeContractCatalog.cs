using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public enum KnowledgeTreeEntryKind { File, Directory }
public sealed record KnowledgeTreeEntryContract(string Path, KnowledgeTreeEntryKind Kind, long? SizeBytes);
public sealed record KnowledgeTreeContract(string Revision, IReadOnlyList<KnowledgeTreeEntryContract> Entries, bool IsTruncated);
public sealed record KnowledgeSearchMatchContract(string Path, int Line, string Text);
public sealed record KnowledgeSearchContract(string Revision, IReadOnlyList<KnowledgeSearchMatchContract> Matches, bool IsTruncated);
public sealed record KnowledgeReadContract(string Revision, string Path, IReadOnlyList<string> Lines, bool IsTruncated);
public sealed record KnowledgeLinkContract(string SourcePath, int Line, string Kind, string? TargetPath, string? Fragment, bool IsExternal);
public sealed record KnowledgeLinksContract(string Revision, IReadOnlyList<KnowledgeLinkContract> Links, bool IsTruncated);

/// <summary>Stable, source-independent knowledge read contract. It is deliberately unavailable until a Vault is registered and mapped.</summary>
public sealed class KnowledgeContractCatalog
{
    public Result<KnowledgeTreeContract> Tree(string projectId, string? path, int? depth) => Unavailable<KnowledgeTreeContract>();
    public Result<KnowledgeSearchContract> Search(string projectId, string query) => Unavailable<KnowledgeSearchContract>();
    public Result<KnowledgeReadContract> Read(string projectId, string path) => Unavailable<KnowledgeReadContract>();
    public Result<KnowledgeLinksContract> Links(string projectId, string path) => Unavailable<KnowledgeLinksContract>();

    private static Result<T> Unavailable<T>() => Result<T>.Failure(new DomainError("knowledge.unavailable", "No knowledge vault is registered for this project."));
}