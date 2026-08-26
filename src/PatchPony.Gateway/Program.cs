using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using PatchPony.Core.Application;
using PatchPony.Core.Common;
using PatchPony.Gateway;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var authentication = builder.Configuration.GetSection(PatchPonyAuthenticationOptions.SectionName).Get<PatchPonyAuthenticationOptions>() ?? new PatchPonyAuthenticationOptions();
builder.Services.AddSingleton(authentication);
builder.Services.AddAuthentication(PatchPonyAuthenticationDefaults.Scheme)
    .AddPolicyScheme(PatchPonyAuthenticationDefaults.Scheme, "PatchPony user authentication", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey(N8nServiceAuthenticationHandler.HeaderName)
                ? PatchPonyAuthenticationDefaults.N8nServiceScheme
                : context.Request.Headers.TryGetValue("Authorization", out var authorization) && authorization.Any(value => value is not null && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    ? JwtBearerDefaults.AuthenticationScheme
                    : PatchPonyAuthenticationDefaults.DevelopmentPasswordScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = authentication.Oidc.RequireHttpsMetadata;
        if (!string.IsNullOrWhiteSpace(authentication.Oidc.Authority) && !string.IsNullOrWhiteSpace(authentication.Oidc.Audience))
        {
            options.Authority = authentication.Oidc.Authority;
            options.Audience = authentication.Oidc.Audience;
            return;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://unconfigured.patchpony.invalid",
            ValidateAudience = true,
            ValidAudience = "patchpony-unconfigured",
            ValidateIssuerSigningKey = true
        };
    })
    .AddScheme<AuthenticationSchemeOptions, DevelopmentPasswordAuthenticationHandler>(
        PatchPonyAuthenticationDefaults.DevelopmentPasswordScheme,
        _ => { })
    .AddScheme<AuthenticationSchemeOptions, N8nServiceAuthenticationHandler>(
        PatchPonyAuthenticationDefaults.N8nServiceScheme,
        _ => { });
builder.Services.AddTransient<IClaimsTransformation, ReadOnlyRoleScopeClaimsTransformation>();
builder.Services.AddAuthorization(PatchPonyAuthorization.Configure);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationAuditResultHandler>();
var rateLimits = builder.Configuration.GetSection(GatewayRateLimitOptions.SectionName).Get<GatewayRateLimitOptions>() ?? new GatewayRateLimitOptions();
rateLimits.Validate();
builder.Services.AddSingleton(rateLimits);
builder.Services.AddSingleton<GatewayRateLimitStore>();
var cors = builder.Configuration.GetSection(GatewayCorsOptions.SectionName).Get<GatewayCorsOptions>() ?? new GatewayCorsOptions();
cors.Validate();
builder.Services.AddSingleton(cors);
builder.Services.AddCors(options => GatewayCors.Configure(options, cors));
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "PatchPony REST API";
        document.Info.Version = "v1";
        document.Info.Description = "Versioned read-only REST contract for PatchPony clients such as n8n.";
        return Task.CompletedTask;
    });
});
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<RuntimeMcpTools>()
    .WithTools<ProjectSourceMcpTools>()
    .WithTools<KnowledgeMcpTools>()
    .WithTools<ConfigMcpTools>()
    .WithTools<TestMcpTools>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
builder.Services.AddSingleton<RuntimeStatusService>();
builder.Services.AddSingleton<IGatewayLogRedactor, GatewayLogRedactor>();
builder.Services.AddSingleton<IAccessDecisionAudit, AccessDecisionAudit>();
builder.Services.AddSingleton<ZohoWebhookValidator>();
builder.Services.AddSingleton<ZohoTicketNormalizer>();
builder.Services.AddSingleton<ZohoTicketProjectResolver>();
builder.Services.AddSingleton<ZohoTicketCompletenessEvaluator>();
builder.Services.AddSingleton<ZohoTicketTriageService>();
builder.Services.AddSingleton<ZohoInformationRequestService>();
builder.Services.AddHttpClient<ZohoTaskCommentService>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<ZohoTicketChangeRequestService>();
builder.Services.AddSingleton<ConfigurationEnvironmentPolicyService>();
builder.Services.AddSingleton<ConfigurationKeyPolicyService>();
builder.Services.AddSingleton<ConfigurationConflictPolicyService>();
builder.Services.AddSingleton<ConfigurationReviewPolicyService>();
builder.Services.AddSingleton<ConfigurationAutomationProjectGateService>();
builder.Services.AddSingleton<FeatureRequestSourceChangePolicyService>();
builder.Services.AddSingleton<ProjectToolAuthorizationService>();
var testSessions = builder.Configuration.GetSection(TestSessionOptions.SectionName).Get<TestSessionOptions>() ?? new TestSessionOptions();
testSessions.Validate();
builder.Services.AddSingleton(testSessions);
builder.Services.AddSingleton<TestSessionCatalog>();
var configSessions = builder.Configuration.GetSection(ConfigSessionOptions.SectionName).Get<ConfigSessionOptions>() ?? new ConfigSessionOptions();
configSessions.Validate();
builder.Services.AddSingleton(configSessions);
builder.Services.AddSingleton<ConfigSessionCatalog>();
var pilotSources = builder.Configuration.GetSection(PilotSourceOptions.SectionName).Get<PilotSourceOptions>() ?? new PilotSourceOptions();
pilotSources.Validate();
builder.Services.AddSingleton(pilotSources);
builder.Services.AddSingleton<PilotSourceCatalog>();
builder.Services.AddSingleton<KnowledgeContractCatalog>();
var gatewayLimits = new GatewayRequestLimits();
gatewayLimits.Validate();
builder.Services.AddSingleton(gatewayLimits);
var app = builder.Build();
app.UseMiddleware<PatchPony.Gateway.CorrelationMiddleware>();
app.UseMiddleware<GatewayRequestLimitsMiddleware>();
app.UseRouting();
app.UseCors(GatewayCors.PolicyName);
app.UseAuthentication();
app.UseMiddleware<GatewayRateLimitMiddleware>();
app.UseMiddleware<AuthenticationAuditMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PatchPony REST API v1"));
}

app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "PatchPony Gateway" })).AllowAnonymous();
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapMcp("/mcp").RequireAuthorization();
app.MapApiV1();
var openApi = app.MapOpenApi("/openapi/{documentName}.json");
if (app.Environment.IsDevelopment())
{
    openApi.AllowAnonymous();
}

app.Run();

public partial class Program { }
