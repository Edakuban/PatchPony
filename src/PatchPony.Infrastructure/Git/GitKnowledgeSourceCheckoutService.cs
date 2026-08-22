using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Git;

public sealed class GitKnowledgeSourceCheckoutService : IKnowledgeSourceCheckoutService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly string storageRoot;
    private readonly IGitCommandRunner git;

    public GitKnowledgeSourceCheckoutService(string storageRoot, IGitCommandRunner? git = null)
    {
        if (!Path.IsPathFullyQualified(storageRoot))
        {
            throw new ArgumentException("The knowledge checkout storage root must be an absolute server-side path.", nameof(storageRoot));
        }

        this.storageRoot = Path.GetFullPath(storageRoot);
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 16 * 1024);
    }

    public async Task<Result<KnowledgeSourceCheckout>> EnsureAsync(KnowledgeSource source, CancellationToken cancellationToken = default)
    {
        if (source.Id.Value == Guid.Empty || source.ProjectId.Value == Guid.Empty)
        {
            return Result<KnowledgeSourceCheckout>.Failure(DomainError.Validation("Knowledge source registration is incomplete."));
        }

        var checkoutPath = Path.Combine(storageRoot, source.Id.Value.ToString("N"));
        if (!Directory.Exists(checkoutPath))
        {
            Directory.CreateDirectory(storageRoot);
            var clone = await RunAsync(null, ["clone", "--no-checkout", "--filter=blob:none", "--", source.RemoteUri.AbsoluteUri, checkoutPath], cancellationToken);
            if (!clone.IsSuccess)
            {
                return Result<KnowledgeSourceCheckout>.Failure(clone.Error);
            }

            return await CheckoutAndResolveAsync(source, checkoutPath, created: true, cancellationToken);
        }

        var remoteUrl = await RunAsync(checkoutPath, ["remote", "get-url", "origin"], cancellationToken);
        if (!remoteUrl.IsSuccess)
        {
            return Result<KnowledgeSourceCheckout>.Failure(remoteUrl.Error);
        }

        if (!string.Equals(remoteUrl.Value!.StandardOutput.Trim(), source.RemoteUri.AbsoluteUri, StringComparison.Ordinal))
        {
            return Result<KnowledgeSourceCheckout>.Failure(new DomainError("checkout.remote_mismatch", "The existing checkout belongs to a different registered knowledge source."));
        }

        return await CheckoutAndResolveAsync(source, checkoutPath, created: false, cancellationToken);
    }

    private async Task<Result<KnowledgeSourceCheckout>> CheckoutAndResolveAsync(KnowledgeSource source, string checkoutPath, bool created, CancellationToken cancellationToken)
    {
        var refSpec = $"+refs/heads/{source.DefaultBranch}:refs/remotes/origin/{source.DefaultBranch}";
        var fetch = await RunAsync(checkoutPath, ["fetch", "--no-tags", "--prune", "origin", refSpec], cancellationToken);
        if (!fetch.IsSuccess)
        {
            return Result<KnowledgeSourceCheckout>.Failure(fetch.Error);
        }

        var checkout = await RunAsync(checkoutPath, ["checkout", "--detach", "--force", $"refs/remotes/origin/{source.DefaultBranch}"], cancellationToken);
        if (!checkout.IsSuccess)
        {
            return Result<KnowledgeSourceCheckout>.Failure(checkout.Error);
        }

        var resolved = await RunAsync(checkoutPath, ["rev-parse", "--verify", "HEAD^{commit}"], cancellationToken);
        if (!resolved.IsSuccess)
        {
            return Result<KnowledgeSourceCheckout>.Failure(resolved.Error);
        }

        var revision = RepositoryRevision.Create(resolved.Value!.StandardOutput);
        return revision.IsSuccess
            ? Result<KnowledgeSourceCheckout>.Success(new KnowledgeSourceCheckout(source.Id, revision.Value!, created))
            : Result<KnowledgeSourceCheckout>.Failure(revision.Error);
    }

    private async Task<Result<GitCommandResult>> RunAsync(string? workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var result = await git.RunAsync(new GitCommand(workingDirectory, arguments), cancellationToken);
            return result.ExitCode == 0
                ? Result<GitCommandResult>.Success(result)
                : Result<GitCommandResult>.Failure(new DomainError("checkout.git_failed", "The controlled Git operation failed."));
        }
        catch (TimeoutException)
        {
            return Result<GitCommandResult>.Failure(new DomainError("checkout.timeout", "The controlled Git operation timed out."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<GitCommandResult>.Failure(new DomainError("checkout.git_failed", "The controlled Git operation failed."));
        }
    }
}
