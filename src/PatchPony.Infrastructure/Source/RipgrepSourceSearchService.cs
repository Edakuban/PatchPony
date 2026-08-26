using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Source;

public sealed record SourceSearchMatch(string RelativePath, int LineNumber, string LineText);

public sealed record SourceSearchResult(IReadOnlyList<SourceSearchMatch> Matches, bool IsTruncated);

public sealed class RipgrepSourceSearchService
{
    private const int MaximumQueryCharacters = 256;
    private const int MaximumCandidateFiles = 200;
    private const long MaximumCandidateFileBytes = 1_048_576;
    private const int MaximumResults = 100;
    private const int MaximumMatchesPerFile = 20;
    private const int MaximumOutputBytes = 256 * 1024;
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(10);

    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathAccessService access;
    private readonly string executable;

    public RipgrepSourceSearchService(ProjectPathResolver resolver, ProjectPathAccessService access, string executable = "rg")
    {
        this.resolver = resolver;
        this.access = access;
        this.executable = executable;
    }

    public async Task<Result<SourceSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query) || query.Length > MaximumQueryCharacters)
        {
            return Result<SourceSearchResult>.Failure(new DomainError("search.invalid_query", "The search query must contain between 1 and 256 characters."));
        }

        var candidates = CollectCandidates(cancellationToken);
        if (!candidates.IsSuccess)
        {
            return Result<SourceSearchResult>.Failure(candidates.Error);
        }

        if (candidates.Value!.Files.Count == 0)
        {
            return Result<SourceSearchResult>.Success(new SourceSearchResult([], candidates.Value.IsTruncated));
        }

        var arguments = new List<string>
        {
            "--json",
            "--fixed-strings",
            "--line-number",
            "--no-heading",
            "--color=never",
            $"--max-count={MaximumMatchesPerFile}",
            "--max-filesize=1M",
            "--max-columns=4096",
            "--"
        };
        arguments.Add(query);
        arguments.AddRange(candidates.Value.Files);

        var command = await RunRipgrepAsync(candidates.Value.RootPath, arguments, cancellationToken);
        if (!command.IsSuccess)
        {
            return Result<SourceSearchResult>.Failure(command.Error);
        }

        if (command.Value!.ExitCode is not 0 and not 1)
        {
            return Result<SourceSearchResult>.Failure(new DomainError("search.unavailable", "The controlled source search failed."));
        }

        try
        {
            var output = command.Value.StandardOutput;
            if (command.Value.OutputWasTruncated)
            {
                var lastCompleteLine = output.LastIndexOf('\n');
                output = lastCompleteLine < 0 ? string.Empty : output[..lastCompleteLine];
            }

            var matches = ParseMatches(output, candidates.Value.Files, out var reachedResultLimit);
            return Result<SourceSearchResult>.Success(new SourceSearchResult(
                matches,
                candidates.Value.IsTruncated || command.Value.OutputWasTruncated || reachedResultLimit));
        }
        catch (JsonException)
        {
            return Result<SourceSearchResult>.Failure(new DomainError("search.unavailable", "The controlled source search returned an invalid response."));
        }
    }

    private Result<CandidateFiles> CollectCandidates(CancellationToken cancellationToken)
    {
        var root = resolver.Resolve(".");
        if (!root.IsSuccess || !Directory.Exists(root.Value!.FullPath))
        {
            return Result<CandidateFiles>.Failure(new DomainError("search.unavailable", "The project checkout is unavailable."));
        }

        var files = new List<string>();
        var state = new CandidateState();
        var collected = CollectFromDirectory(root.Value.FullPath, string.Empty, files, state, cancellationToken);
        return collected.IsSuccess
            ? Result<CandidateFiles>.Success(new CandidateFiles(root.Value.FullPath, files, state.IsTruncated))
            : Result<CandidateFiles>.Failure(collected.Error);
    }

    private Result CollectFromDirectory(string directory, string relativeDirectory, List<string> files, CandidateState state, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var fullPath in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (state.ExaminedEntries == MaximumCandidateFiles)
                {
                    state.IsTruncated = true;
                    return Result.Success();
                }

                state.ExaminedEntries++;
                var name = Path.GetFileName(fullPath);
                var relativePath = string.IsNullOrEmpty(relativeDirectory) ? name : $"{relativeDirectory}/{name}";

                if (Directory.Exists(fullPath))
                {
                    var authorizedDirectory = access.ResolveDirectoryAndAuthorize(relativePath);
                    if (!authorizedDirectory.IsSuccess || IsReparsePoint(fullPath))
                    {
                        continue;
                    }

                    var nested = CollectFromDirectory(authorizedDirectory.Value!.FullPath, authorizedDirectory.Value.RelativePath, files, state, cancellationToken);
                    if (!nested.IsSuccess || state.IsTruncated)
                    {
                        return nested;
                    }

                    continue;
                }

                var authorizedFile = access.ResolveAndAuthorize(relativePath, ProjectPathAccess.Read);
                if (!authorizedFile.IsSuccess || !File.Exists(fullPath))
                {
                    continue;
                }

                if (new FileInfo(authorizedFile.Value!.FullPath).Length <= MaximumCandidateFileBytes)
                {
                    files.Add(authorizedFile.Value.RelativePath);
                }
            }

            return Result.Success();
        }
        catch (IOException)
        {
            return Result.Failure(new DomainError("search.unavailable", "The project checkout is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(new DomainError("search.unavailable", "The project checkout is unavailable."));
        }
    }

    private async Task<Result<RipgrepCommandResult>> RunRipgrepAsync(string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment.Clear();
        startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(SearchTimeout);
            var outputTask = ReadLimitedAsync(process.StandardOutput, timeout.Token);
            var errorTask = DrainAsync(process.StandardError, timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            await errorTask;
            return Result<RipgrepCommandResult>.Success(new RipgrepCommandResult(process.ExitCode, output.Text, output.WasTruncated));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return Result<RipgrepCommandResult>.Failure(new DomainError("search.timeout", "The controlled source search timed out."));
        }
        catch (Exception)
        {
            TryKill(process);
            return Result<RipgrepCommandResult>.Failure(new DomainError("search.unavailable", "The controlled source search is unavailable."));
        }
    }

    private static IReadOnlyList<SourceSearchMatch> ParseMatches(string output, IReadOnlyCollection<string> allowedFiles, out bool reachedResultLimit)
    {
        var allowed = new HashSet<string>(allowedFiles, StringComparer.Ordinal);
        var matches = new List<SourceSearchMatch>();
        var matchesPerFile = new Dictionary<string, int>(StringComparer.Ordinal);
        reachedResultLimit = false;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "match")
            {
                continue;
            }

            var data = root.GetProperty("data");
            var path = data.GetProperty("path").GetProperty("text").GetString()?.Replace('\\', '/') ?? string.Empty;
            if (!allowed.Contains(path))
            {
                continue;
            }

            var lineText = data.GetProperty("lines").GetProperty("text").GetString()?.TrimEnd('\r', '\n') ?? string.Empty;
            var lineNumber = data.GetProperty("line_number").GetInt32();
            matches.Add(new SourceSearchMatch(path, lineNumber, lineText));
            matchesPerFile[path] = matchesPerFile.GetValueOrDefault(path) + 1;
            if (matchesPerFile[path] == MaximumMatchesPerFile || matches.Count == MaximumResults)
            {
                reachedResultLimit = true;
                break;
            }
        }

        return matches;
    }

    private static async Task<(string Text, bool WasTruncated)> ReadLimitedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[1024];
        var builder = new StringBuilder();
        var retainedBytes = 0;
        var wasTruncated = false;

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            for (var index = 0; index < read; index++)
            {
                var characterBytes = Encoding.UTF8.GetByteCount(buffer[index].ToString());
                if (retainedBytes + characterBytes > MaximumOutputBytes)
                {
                    wasTruncated = true;
                    continue;
                }

                builder.Append(buffer[index]);
                retainedBytes += characterBytes;
            }
        }

        return (builder.ToString(), wasTruncated);
    }

    private static async Task DrainAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[1024];
        while (await reader.ReadAsync(buffer.AsMemory(), cancellationToken) != 0)
        {
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void TryKill(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }

    private sealed record CandidateFiles(string RootPath, IReadOnlyList<string> Files, bool IsTruncated);

    private sealed record RipgrepCommandResult(int ExitCode, string StandardOutput, bool OutputWasTruncated);

    private sealed class CandidateState
    {
        public int ExaminedEntries { get; set; }

        public bool IsTruncated { get; set; }
    }
}
