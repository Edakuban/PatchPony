using System.Diagnostics;
using System.Text.Json;
using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Queue;
using PatchPony.Core.Sandbox;
using PatchPony.Core.Sessions;
using PatchPony.Worker;

namespace PatchPony.IntegrationTests;

public sealed class SandboxWorkerApiTests
{
    [Fact]
    public void Submit_StartsOneRegisteredCommandAndRejectsReplayOrForeignWorkers()
    {
        var key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var authenticator = new WorkerClaimAuthenticator(key);
        var now = DateTimeOffset.UtcNow;
        var claim = new JobClaim(Guid.NewGuid(), JobId.New(), "worker-a", now.AddMinutes(5));
        var proof = authenticator.Issue(claim, now).Value!;
        var commands = Commands();
        var launcher = new RecordingLauncher();
        var api = new SandboxWorkerApi(authenticator, new WorkerIdentity("worker-a"), commands, launcher);
        var request = new SandboxJobRequest(proof, SessionId.New(), "test.billing.unit");
        var foreign = new SandboxWorkerApi(authenticator, new WorkerIdentity("worker-b"), commands, launcher);

        var accepted = api.Submit(request);
        var replay = api.Submit(request);
        var denied = foreign.Submit(request);

        Assert.True(accepted.IsSuccess);
        Assert.Single(launcher.Requests);
        Assert.Equal(claim.ClaimId, accepted.Value!.ClaimId);
        Assert.False(replay.IsSuccess);
        Assert.Equal("sandbox.request.replayed", replay.Error.Code);
        Assert.False(denied.IsSuccess);
        Assert.Equal("worker_claim.invalid", denied.Error.Code);
    }

    [Fact]
    public void DockerLauncher_UsesMandatoryContainerHardeningOptions()
    {
        var profile = Path.GetTempFileName();
        try
        {
            var starter = new RecordingProcessStarter();
            var watchdog = new RecordingWatchdog();
            var worktrees = new FixedWorktreeResolver("/server/sessions/pp-session-abc/worktree");
            var output = new RecordingOutputCollector();
            var limits = new SandboxResourceLimits(1.5, 536_870_912, 128, 1_073_741_824, TimeSpan.FromMinutes(5));
            var terminator = new RecordingTerminator();
            var cleaner = new RecordingCleaner();
            var launcher = new DockerSandboxContainerLauncher(starter, new SandboxSecurityProfile(profile, "patchpony-sandbox"), limits, watchdog, worktrees, output, new SandboxOutputLimits(4_096), terminator, cleaner);
            var command = Commands().Resolve("test.billing.unit").Value!;

            var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var result = launcher.Start(new SandboxContainerLaunchRequest(executionId, SessionId.New(), command));

            Assert.True(result.IsSuccess);
            Assert.Equal("docker", starter.FileName);
            Assert.Equal(["run", "--rm", "--name", "patchpony-sandbox-11111111111111111111111111111111", "--user", "65532:65532", "--read-only", "--mount", "type=bind,src=/server/sessions/pp-session-abc/worktree,dst=/workspace,rw,bind-propagation=rprivate", "--workdir", "/workspace", "--cpus", "1.5", "--memory", "536870912", "--pids-limit", "128", "--storage-opt", "size=1073741824", "--network", "none", "--cap-drop=ALL", "--security-opt", "no-new-privileges:true", "--security-opt", $"seccomp={profile}", "--security-opt", "apparmor=patchpony-sandbox", command.Runner.ImageReference, "/usr/bin/dotnet", "test", "--no-restore"], starter.Arguments);
            Assert.Same(starter.Process, watchdog.Process);
            Assert.Equal(limits.ExecutionTimeout, watchdog.Timeout);
            Assert.True(starter.RedirectStandardOutput);
            Assert.True(starter.RedirectStandardError);
            Assert.Same(starter.Process, output.Process);
            Assert.Equal(4_096, output.MaxBytesPerStream);
            Assert.Equal(limits.ExecutionTimeout, output.Timeout);
        }
        finally
        {
            File.Delete(profile);
        }
    }

    [Fact]
    public void DockerLauncher_CancellationTerminatesProcessAndForcesNamedContainerCleanup()
    {
        var profile = Path.GetTempFileName();
        try
        {
            var starter = new RecordingProcessStarter();
            var terminator = new RecordingTerminator();
            var cleaner = new RecordingCleaner();
            var output = new PendingOutputCollector();
            var launcher = new DockerSandboxContainerLauncher(
                starter, new SandboxSecurityProfile(profile, "patchpony-sandbox"),
                new SandboxResourceLimits(1, 536_870_912, 128, 1_073_741_824, TimeSpan.FromMinutes(5)),
                new RecordingWatchdog(), new FixedWorktreeResolver("/server/sessions/pp-session-abc/worktree"),
                output, new SandboxOutputLimits(4_096), terminator, cleaner);
            var executionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            using var cancellation = new CancellationTokenSource();

            var result = launcher.Start(new SandboxContainerLaunchRequest(executionId, SessionId.New(), Commands().Resolve("test.billing.unit").Value!), cancellation.Token);
            cancellation.Cancel();

            Assert.True(result.IsSuccess);
            Assert.Same(starter.Process, terminator.Process);
            Assert.Equal("patchpony-sandbox-22222222222222222222222222222222", Assert.Single(cleaner.ContainerNames));
            output.Complete();
        }
        finally
        {
            File.Delete(profile);
        }
    }
    [Fact]
    public void DockerLauncher_RefusesToStartWhenTheServerWorktreeIsUnavailable()
    {
        var profile = Path.GetTempFileName();
        try
        {
            var starter = new RecordingProcessStarter();
            var launcher = new DockerSandboxContainerLauncher(
                starter,
                new SandboxSecurityProfile(profile, "patchpony-sandbox"),
                new SandboxResourceLimits(1, 536_870_912, 128, 1_073_741_824, TimeSpan.FromMinutes(5)),
                new RecordingWatchdog(),
                new FixedWorktreeResolver(new DomainError("sandbox.workspace.unavailable", "unavailable")), new RecordingOutputCollector(), new SandboxOutputLimits(4_096), new RecordingTerminator(), new RecordingCleaner());

            var result = launcher.Start(new SandboxContainerLaunchRequest(Guid.NewGuid(), SessionId.New(), Commands().Resolve("test.billing.unit").Value!));

            Assert.False(result.IsSuccess);
            Assert.Equal("sandbox.workspace.unavailable", result.Error.Code);
            Assert.Null(starter.FileName);
        }
        finally
        {
            File.Delete(profile);
        }
    }
    [Fact]
    public void SandboxResourceLimits_RequireBoundedCpuMemoryPidsDiskAndTimeout()
    {
        var limits = new SandboxResourceLimitOptions
        {
            CpuCount = 1.5,
            MemoryBytes = 536_870_912,
            PidsLimit = 128,
            DiskBytes = 1_073_741_824,
            ExecutionTimeout = TimeSpan.FromMinutes(5)
        }.CreateLimits();

        Assert.Equal("1.5", limits.CpuArgument);
        Assert.Throws<InvalidOperationException>(() => new SandboxResourceLimitOptions
        {
            CpuCount = 0,
            MemoryBytes = 536_870_912,
            PidsLimit = 128,
            DiskBytes = 1_073_741_824,
            ExecutionTimeout = TimeSpan.FromMinutes(5)
        }.CreateLimits());
    }
    [Fact]
    public async Task SandboxResultRecorder_PersistsBoundedOutputAndArtifactMetadataBesideTheWorktree()
    {
        var root = Path.Combine(Path.GetTempPath(), $"patchpony-sandbox-{Guid.NewGuid():N}");
        var worktree = Path.Combine(root, "worktree");
        var artifacts = Path.Combine(worktree, ".patchpony-artifacts");
        Directory.CreateDirectory(artifacts);
        await File.WriteAllTextAsync(Path.Combine(artifacts, "report.txt"), "passed");
        try
        {
            var output = new SandboxProcessOutput("all tests passed", string.Empty, false, false, false, 0);
            var launch = new SandboxContainerLaunch(42, Commands().Resolve("test.billing.unit").Value!, new SandboxProcessOutputCapture(Task.FromResult(output)));
            var acceptance = new SandboxJobAcceptance(Guid.NewGuid(), Guid.NewGuid(), SessionId.New(), "test.billing.unit", launch, DateTimeOffset.UtcNow);
            var recorder = new SandboxExecutionResultRecorder(new FixedWorktreeResolver(worktree));

            recorder.Track(acceptance);

            var destination = Path.Combine(root, "sandbox-results", $"{acceptance.ExecutionId:N}.json");
            for (var attempt = 0; attempt < 40 && !File.Exists(destination); attempt++) await Task.Delay(25);
            var persisted = JsonSerializer.Deserialize<SandboxExecutionResult>(await File.ReadAllTextAsync(destination));

            Assert.NotNull(persisted);
            Assert.Equal(SandboxTestOutcome.Passed, persisted!.Outcome);
            Assert.Equal(0, persisted.Output.ExitCode);
            var artifact = Assert.Single(persisted.Artifacts);
            Assert.Equal("report.txt", artifact.RelativePath);
            Assert.Equal(6, artifact.LengthBytes);
            Assert.Equal("284d1e8c4918248233df17642bbb940c001e1fa856c18aab86ba6dbe7813eb13", artifact.Sha256);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void RootlessDockerOptions_FailClosedWhenTheDaemonIsNotRootless()
    {
        new RootlessDockerOptions().Verify(new FixedRootlessProbe(true));
        Assert.Throws<InvalidOperationException>(() => new RootlessDockerOptions().Verify(new FixedRootlessProbe(false)));
    }
    [Fact]
    public void SandboxOutputLimits_RequireBoundedBytesPerStream()
    {
        Assert.Equal(4_096, new SandboxOutputLimitOptions { MaxBytesPerStream = 4_096 }.CreateLimits().MaxBytesPerStream);
        Assert.Throws<InvalidOperationException>(() => new SandboxOutputLimitOptions { MaxBytesPerStream = 4_095 }.CreateLimits());
    }
    [Fact]
    public void SandboxSecurityOptions_RequireAnExistingAbsoluteSeccompProfile()
    {
        var profile = Path.GetTempFileName();
        try
        {
            var valid = new SandboxSecurityOptions { SeccompProfilePath = profile, AppArmorProfile = "patchpony-sandbox" }.CreateProfile();
            Assert.Equal(Path.GetFullPath(profile), valid.SeccompProfilePath);
            Assert.Throws<InvalidOperationException>(() => new SandboxSecurityOptions { SeccompProfilePath = "relative.json", AppArmorProfile = "patchpony-sandbox" }.CreateProfile());
            Assert.Throws<InvalidOperationException>(() => new SandboxSecurityOptions { SeccompProfilePath = profile, AppArmorProfile = "invalid profile" }.CreateProfile());
        }
        finally
        {
            File.Delete(profile);
        }
    }

    private static ISandboxCommandCatalog Commands()
    {
        var runners = new SandboxRunnerCatalog([
            new SandboxRunnerImage("dotnet-test-v1", "registry.example.test/patchpony/dotnet-test@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]);
        return new SandboxCommandCatalog(runners, [
            new SandboxCommandDefinition("test.billing.unit", "dotnet-test-v1", "/usr/bin/dotnet", ["test", "--no-restore"])]);
    }

    private sealed class RecordingLauncher : ISandboxContainerLauncher
    {
        public List<SandboxContainerLaunchRequest> Requests { get; } = [];
        public Result<SandboxContainerLaunch> Start(SandboxContainerLaunchRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Result<SandboxContainerLaunch>.Success(new SandboxContainerLaunch(42, request.Command));
        }
    }

    private sealed class RecordingProcessStarter : IDockerProcessStarter
    {
        public string? FileName { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; } = [];
        public Process? Start(ProcessStartInfo startInfo)
        {
            FileName = startInfo.FileName;
            Arguments = startInfo.ArgumentList.ToArray();
            RedirectStandardOutput = startInfo.RedirectStandardOutput;
            RedirectStandardError = startInfo.RedirectStandardError;
            Process = System.Diagnostics.Process.GetCurrentProcess();
            return Process;
        }

        public Process? Process { get; private set; }
        public bool RedirectStandardOutput { get; private set; }
        public bool RedirectStandardError { get; private set; }
    }

    private sealed class RecordingOutputCollector : ISandboxProcessOutputCollector
    {
        public Process? Process { get; private set; }
        public long? MaxBytesPerStream { get; private set; }
        public TimeSpan? Timeout { get; private set; }

        public SandboxProcessOutputCapture Capture(Process process, long maxBytesPerStream, TimeSpan timeout)
        {
            Process = process;
            MaxBytesPerStream = maxBytesPerStream;
            Timeout = timeout;
            return new SandboxProcessOutputCapture(Task.FromResult(new SandboxProcessOutput(string.Empty, string.Empty, false, false, false, 0)));
        }
    }
    private sealed class FixedRootlessProbe(bool rootless) : IDockerRootlessProbe
    {
        public bool IsRootless() => rootless;
    }
    private sealed class PendingOutputCollector : ISandboxProcessOutputCollector
    {
        private readonly TaskCompletionSource<SandboxProcessOutput> completion = new();
        public SandboxProcessOutputCapture Capture(Process process, long maxBytesPerStream, TimeSpan timeout) => new(completion.Task);
        public void Complete() => completion.TrySetResult(new SandboxProcessOutput(string.Empty, string.Empty, false, false, false, 0));
    }

    private sealed class RecordingTerminator : ISandboxProcessTerminator
    {
        public Process? Process { get; private set; }
        public void Terminate(Process process) => Process = process;
    }

    private sealed class RecordingCleaner : IDockerContainerCleaner
    {
        public List<string> ContainerNames { get; } = [];
        public void Remove(string containerName) => ContainerNames.Add(containerName);
    }
    private sealed class FixedWorktreeResolver : ISandboxSessionWorktreeResolver
    {
        private readonly Result<SandboxSessionWorktree> result;

        public FixedWorktreeResolver(string hostPath) => result = Result<SandboxSessionWorktree>.Success(new SandboxSessionWorktree(hostPath));
        public FixedWorktreeResolver(DomainError error) => result = Result<SandboxSessionWorktree>.Failure(error);
        public Result<SandboxSessionWorktree> Resolve(SessionId sessionId) => result;
    }
    private sealed class RecordingWatchdog : ISandboxProcessWatchdog
    {
        public Process? Process { get; private set; }
        public TimeSpan? Timeout { get; private set; }
        public Action? OnTimeout { get; private set; }

        public void Watch(Process process, TimeSpan timeout, Action? onTimeout = null)
        {
            Process = process;
            Timeout = timeout;
            OnTimeout = onTimeout;
        }
    }
}