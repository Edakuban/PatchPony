using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Persistence;

public interface IKnowledgeSourceRepository
{
    Task<KnowledgeSource?> GetAsync(KnowledgeSourceId id, CancellationToken cancellationToken = default);

    Task<KnowledgeSource?> GetByProjectAndNameAsync(ProjectId projectId, string name, CancellationToken cancellationToken = default);

    Task AddAsync(KnowledgeSource source, CancellationToken cancellationToken = default);
}
