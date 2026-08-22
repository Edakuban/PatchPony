using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using PatchPony.Core.Application;
using PatchPony.Core.Common;
using PatchPony.Gateway;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<RuntimeMcpTools>();
builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
builder.Services.AddSingleton<RuntimeStatusService>();
var gatewayLimits = new GatewayRequestLimits();
gatewayLimits.Validate();
builder.Services.AddSingleton(gatewayLimits);
var app = builder.Build();
app.UseMiddleware<PatchPony.Gateway.CorrelationMiddleware>();
app.UseMiddleware<GatewayRequestLimitsMiddleware>();

app.MapGet("/", () => Results.Ok(new { service = "PatchPony Gateway" }));
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapMcp("/mcp");
app.MapApiV1();

app.Run();

public partial class Program { }
