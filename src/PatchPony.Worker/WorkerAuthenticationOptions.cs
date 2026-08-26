using PatchPony.Core.Queue;

namespace PatchPony.Worker;

public sealed class WorkerAuthenticationOptions
{
    public const string SectionName = "PatchPony:Worker";

    public string Id { get; init; } = string.Empty;

    public string ClaimSigningKey { get; init; } = string.Empty;

    public WorkerIdentity CreateIdentity()
    {
        if (string.IsNullOrWhiteSpace(Id) || Id.Length > 255)
        {
            throw new InvalidOperationException("PatchPony:Worker:Id must contain at most 255 characters.");
        }

        return new WorkerIdentity(Id);
    }

    public WorkerClaimAuthenticator CreateClaimAuthenticator()
    {
        try
        {
            return new WorkerClaimAuthenticator(Convert.FromBase64String(ClaimSigningKey));
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("PatchPony:Worker:ClaimSigningKey must be Base64 encoded.", exception);
        }
    }
}

public sealed record WorkerIdentity(string Id);