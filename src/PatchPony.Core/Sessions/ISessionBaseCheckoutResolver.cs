using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Sessions;

/// <summary>Resolves the base checkout from server-side deployment configuration.</summary>
public interface ISessionBaseCheckoutResolver
{
    Result<string> Resolve(ProjectId projectId);
}