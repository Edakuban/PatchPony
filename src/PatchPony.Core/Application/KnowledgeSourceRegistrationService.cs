using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Application;

public sealed class KnowledgeSourceRegistrationService(IProjectRepository projects, IKnowledgeSourceRepository sources)
{
    public async Task<Result<KnowledgeSource>> RegisterVaultAsync(
        ProjectId projectId,
        Uri remoteUri,
        string defaultBranch,
        DateTimeOffset registeredAt,
        CancellationToken cancellationToken = default)
    {
        if (await projects.GetAsync(projectId, cancellationToken) is null)
        {
            return Result<KnowledgeSource>.Failure(new DomainError("knowledge_source.project_not_found", "The project must be registered before a knowledge source can be added."));
        }

        const string vaultName = "vault";
        if (await sources.GetByProjectAndNameAsync(projectId, vaultName, cancellationToken) is not null)
        {
            return Result<KnowledgeSource>.Failure(new DomainError("knowledge_source.already_registered", "The project already has a registered knowledge vault."));
        }

        var source = KnowledgeSource.Create(KnowledgeSourceId.New(), projectId, vaultName, remoteUri, defaultBranch, registeredAt);
        if (!source.IsSuccess)
        {
            return source;
        }

        await sources.AddAsync(source.Value!, cancellationToken);
        return source;
    }
}
