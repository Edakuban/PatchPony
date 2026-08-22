using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Git;

public sealed class GitBaseCheckoutService : IBaseCheckoutService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly string storageRoot;
    private readonly IGitCommandRunner git;

    public GitBaseCheckoutService(string storageRoot, IGitCommandRunner? git = null)
    {
        if (!Path.IsPathFullyQualified(storageRoot))
        {
            throw new ArgumentException("The checkout storage root must be an absolute server-side path.", nameof(storageRoot));
        }

        this.storageRoot = Path.GetFullPath(storageRoot);
        this.git = git ?? new ProcessGitCommandRunner("git", DefaultTimeout, 16 * 1024);
    }

    public async Task<Result<BaseCheckout>> EnsureAsync(
        Project project,
        RepositoryRegistration repository,
        CancellationToken cancellationToken = default)
    {
        if (project.Id.Value == Guid.Empty || repository.ProjectId != project.Id)
        {
            return Result<BaseCheckout>.Failure(DomainError.Validation("The project and repository registration do not match."));
        }

        var checkoutPath = GetCheckoutPath(project.Id);
        if (!Directory.Exists(checkoutPath))
        {
            Directory.CreateDirectory(storageRoot);
            var clone = await RunAsync(null, ["clone", "--no-checkout", "--filter=blob:none", "--", repository.RemoteUri.AbsoluteUri, checkoutPath], cancellationToken);
            if (!clone.IsSuccess)
            {
                return Result<BaseCheckout>.Failure(clone.Error);
            }

            return await CheckoutAndResolveAsync(project.Id, checkoutPath, repository.DefaultBranch, created: true, cancellationToken);
        }

        var remoteUrl = await RunAsync(checkoutPath, ["remote", "get-url", "origin"], cancellationToken);
        if (!remoteUrl.IsSuccess)
        {
            return Result<BaseCheckout>.Failure(remoteUrl.Error);
        }

        if (!string.Equals(remoteUrl.Value!.StandardOutput.Trim(), repository.RemoteUri.AbsoluteUri, StringComparison.Ordinal))
        {
            return Result<BaseCheckout>.Failure(new DomainError("checkout.remote_mismatch", "The existing checkout belongs to a different registered repository."));
        }

        return await CheckoutAndResolveAsync(project.Id, checkoutPath, repository.DefaultBranch, created: false, cancellationToken);
    }

    private async Task<Result<BaseCheckout>> CheckoutAndResolveAsync(ProjectId projectId, string checkoutPath, string defaultBranch, bool created, CancellationToken cancellationToken)
    {
        var checkout = await CheckoutBranchAsync(checkoutPath, defaultBranch, cancellationToken);
        if (!checkout.IsSuccess)
        {
            return Result<BaseCheckout>.Failure(checkout.Error);
        }

        var resolved = await RunAsync(checkoutPath, ["rev-parse", "--verify", "HEAD^{commit}"], cancellationToken);
        if (!resolved.IsSuccess)
        {
            return Result<BaseCheckout>.Failure(resolved.Error);
        }

        var revision = RepositoryRevision.Create(resolved.Value!.StandardOutput);
        return revision.IsSuccess
            ? Result<BaseCheckout>.Success(new BaseCheckout(projectId, revision.Value!, created))
            : Result<BaseCheckout>.Failure(revision.Error);
    }

    private async Task<Result<GitCommandResult>> CheckoutBranchAsync(string checkoutPath, string defaultBranch, CancellationToken cancellationToken)
    {
        var refSpec = $"+refs/heads/{defaultBranch}:refs/remotes/origin/{defaultBranch}";
        var fetch = await RunAsync(checkoutPath, ["fetch", "--no-tags", "--prune", "origin", refSpec], cancellationToken);
        if (!fetch.IsSuccess)
        {
            return fetch;
        }

        return await RunAsync(checkoutPath, ["checkout", "--detach", "--force", $"refs/remotes/origin/{defaultBranch}"], cancellationToken);
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

    private string GetCheckoutPath(ProjectId projectId) =>
        Path.Combine(storageRoot, projectId.Value.ToString("N"));
}