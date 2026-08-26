using PatchPony.Core.Common;
using PatchPony.Core.Queue;
using PatchPony.Core.Sandbox;
using PatchPony.Worker;

var builder = Host.CreateApplicationBuilder(args);
var rootlessDocker = builder.Configuration.GetSection(RootlessDockerOptions.SectionName).Get<RootlessDockerOptions>() ?? new RootlessDockerOptions();
builder.Services.AddSingleton(rootlessDocker);
builder.Services.AddSingleton<IDockerRootlessProbe, DockerRootlessProbe>();

var sandboxOutput = builder.Configuration.GetSection(SandboxOutputLimitOptions.SectionName).Get<SandboxOutputLimitOptions>() ?? new SandboxOutputLimitOptions();
builder.Services.AddSingleton(sandboxOutput);
builder.Services.AddSingleton(services => sandboxOutput.CreateLimits());
builder.Services.AddSingleton<ISandboxProcessOutputCollector, SandboxProcessOutputCollector>();

var sandboxWorkspace = builder.Configuration.GetSection(SandboxWorkspaceOptions.SectionName).Get<SandboxWorkspaceOptions>() ?? new SandboxWorkspaceOptions();
builder.Services.AddSingleton(sandboxWorkspace);
builder.Services.AddSingleton(services => sandboxWorkspace.CreateLayoutResolver());
builder.Services.AddSingleton<ISandboxSessionWorktreeResolver, SandboxSessionWorktreeResolver>();
builder.Services.AddSingleton<ISandboxExecutionResultRecorder, SandboxExecutionResultRecorder>();

var sandboxLimits = builder.Configuration.GetSection(SandboxResourceLimitOptions.SectionName).Get<SandboxResourceLimitOptions>() ?? new SandboxResourceLimitOptions();
builder.Services.AddSingleton(sandboxLimits);
builder.Services.AddSingleton(services => sandboxLimits.CreateLimits());
builder.Services.AddSingleton<ISandboxProcessTerminator, SandboxProcessTerminator>();
builder.Services.AddSingleton<ISandboxProcessWatchdog, SandboxProcessWatchdog>();
builder.Services.AddSingleton<IDockerContainerCleaner, DockerContainerCleaner>();

var sandboxSecurity = builder.Configuration.GetSection(SandboxSecurityOptions.SectionName).Get<SandboxSecurityOptions>() ?? new SandboxSecurityOptions();
builder.Services.AddSingleton(sandboxSecurity);
builder.Services.AddSingleton(services => sandboxSecurity.CreateProfile());

var sandboxCommands = builder.Configuration.GetSection(SandboxCommandOptions.SectionName).Get<SandboxCommandOptions>() ?? new SandboxCommandOptions();
builder.Services.AddSingleton(sandboxCommands);
builder.Services.AddSingleton<PatchPony.Core.Sandbox.ISandboxCommandCatalog>(services => sandboxCommands.CreateCatalog(services.GetRequiredService<PatchPony.Core.Sandbox.ISandboxRunnerCatalog>()));

var sandboxRunners = builder.Configuration.GetSection(SandboxRunnerOptions.SectionName).Get<SandboxRunnerOptions>() ?? new SandboxRunnerOptions();
builder.Services.AddSingleton(sandboxRunners);
builder.Services.AddSingleton<PatchPony.Core.Sandbox.ISandboxRunnerCatalog>(_ => sandboxRunners.CreateCatalog());

var workerAuthentication = builder.Configuration
    .GetSection(WorkerAuthenticationOptions.SectionName)
    .Get<WorkerAuthenticationOptions>() ?? new WorkerAuthenticationOptions();

builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
builder.Services.AddSingleton(workerAuthentication.CreateIdentity());
builder.Services.AddSingleton<WorkerClaimAuthenticator>(_ => workerAuthentication.CreateClaimAuthenticator());
builder.Services.AddSingleton<IDockerProcessStarter, ProcessDockerProcessStarter>();
builder.Services.AddSingleton<ISandboxContainerLauncher, DockerSandboxContainerLauncher>();
builder.Services.AddSingleton<ISandboxWorkerApi, SandboxWorkerApi>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
