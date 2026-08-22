using PatchPony.Core.Common;

using PatchPony.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
