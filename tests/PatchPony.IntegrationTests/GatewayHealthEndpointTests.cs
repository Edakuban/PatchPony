using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PatchPony.Gateway;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PatchPony.IntegrationTests;

public sealed class GatewayHealthEndpointTests : IClassFixture<WebApplicationFactory<global::Program>>, IDisposable
{
    private readonly WebApplicationFactory<global::Program> factory;
    private readonly WebApplicationFactory<global::Program> authenticatedFactory;
    private readonly HttpClient client;

    public GatewayHealthEndpointTests(WebApplicationFactory<global::Program> factory)
    {
        this.factory = factory;
        authenticatedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PatchPony:Auth:DevelopmentPassword"] = "default-test-password"
                }));
        });
        client = authenticatedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "default-test-password");
    }

    public void Dispose()
    {
        client.Dispose();
        authenticatedFactory.Dispose();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsOk(string path)
    {
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_DoesNotAllowAnyOriginByDefault()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/status");
        request.Headers.Add("Origin", "https://untrusted.example.test");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_AllowsOnlyTheExplicitOriginForPreflight()
    {
        await using var corsFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("PatchPony:Cors:AllowedOrigins:0", "https://console.example.test"));
        Assert.Equal(["https://console.example.test"], corsFactory.Services.GetRequiredService<GatewayCorsOptions>().AllowedOrigins);
        using var corsClient = corsFactory.CreateClient();
        using var allowedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/v1/runtime/status");
        allowedRequest.Headers.Add("Origin", "https://console.example.test");
        allowedRequest.Headers.Add("Access-Control-Request-Method", "GET");
        allowedRequest.Headers.Add("Access-Control-Request-Headers", "X-Correlation-ID");
        using var allowed = await corsClient.SendAsync(allowedRequest);

        using var deniedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/v1/runtime/status");
        deniedRequest.Headers.Add("Origin", "https://untrusted.example.test");
        deniedRequest.Headers.Add("Access-Control-Request-Method", "GET");
        using var denied = await corsClient.SendAsync(deniedRequest);

        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.Equal("https://console.example.test", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(allowed.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Contains("GET", allowed.Headers.GetValues("Access-Control-Allow-Methods").Single(), StringComparison.Ordinal);
        Assert.Contains("X-Correlation-ID", allowed.Headers.GetValues("Access-Control-Allow-Headers").Single(), StringComparison.OrdinalIgnoreCase);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }
    [Fact]
    public async Task McpEndpoint_AcceptsAStreamableHttpToolsListRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}", Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("runtime.status", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
    [Fact]
    public async Task McpRuntimeStatusTool_ReturnsTheReadOnlyRuntimeMode()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"runtime.status\",\"arguments\":{}}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("read-only", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
    [Fact]
    public async Task RestRuntimeStatus_UsesTheSameCoreHandlerAndCorrelationContext()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/status");
        request.Headers.Add("X-Correlation-ID", "n8n-run-42");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("n8n-run-42", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains("\"mode\":\"read-only\"", body, StringComparison.Ordinal);
        Assert.Contains("\"correlationId\":\"n8n-run-42\"", body, StringComparison.Ordinal);
    }
    [Fact]
    public async Task RestAndMcp_ReturnTheSameDomainErrorCodeForInvalidCorrelationIds()
    {
        using var restRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/correlations/invalid%21");
        restRequest.Headers.Add("X-Correlation-ID", "shared-error-42");
        using var restResponse = await client.SendAsync(restRequest);
        var restBody = await restResponse.Content.ReadAsStringAsync();

        using var mcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"runtime.validate_correlation\",\"arguments\":{\"correlationId\":\"invalid!\"}}}", Encoding.UTF8, "application/json")
        };
        mcpRequest.Headers.Add("X-Correlation-ID", "shared-error-42");
        mcpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        mcpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var mcpResponse = await client.SendAsync(mcpRequest);
        var mcpBody = await mcpResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, restResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mcpResponse.StatusCode);
        Assert.Contains("\"code\":\"validation.invalid\"", restBody, StringComparison.Ordinal);
        Assert.Contains("validation.invalid", mcpBody, StringComparison.Ordinal);
        Assert.Contains("\"correlationId\":\"shared-error-42\"", restBody, StringComparison.Ordinal);
        Assert.Contains("shared-error-42", mcpBody, StringComparison.Ordinal);
        Assert.Contains("\"isError\":true", mcpBody, StringComparison.Ordinal);
    }
    [Fact]
    public async Task ManagedEndpoints_RejectOversizedRequestBodiesBeforeProtocolHandling()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(new string('x', (64 * 1024) + 1), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Correlation-ID", "body-limit-42");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("request.body_too_large", body, StringComparison.Ordinal);
        Assert.Contains("body-limit-42", body, StringComparison.Ordinal);
    }
    [Fact]
    public async Task VersionedApi_DisclosesTheStableV1ContractOnlyUnderItsPrefix()
    {
        using var versioned = await client.GetAsync("/api/v1");
        using var unversioned = await client.GetAsync("/api");

        Assert.Equal(HttpStatusCode.OK, versioned.StatusCode);
        Assert.Contains("\"version\":\"v1\"", await versioned.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, unversioned.StatusCode);
    }

    [Fact]
    public async Task McpToolsList_ExposesStableRuntimeToolContracts()
    {
        using var document = await GetMcpResponseDocument("tools/list", "{}");
        var tools = document.RootElement.GetProperty("result").GetProperty("tools")
            .EnumerateArray()
            .ToDictionary(tool => tool.GetProperty("name").GetString()!, StringComparer.Ordinal);

        Assert.Equal(["config.patch", "config.validate", "knowledge.links", "knowledge.read", "knowledge.search", "knowledge.tree", "projects.list", "runtime.status", "runtime.validate_correlation", "source.read", "source.search", "tests.list", "tests.result", "tests.run"], tools.Keys.OrderBy(name => name, StringComparer.Ordinal));

        var configValidate = tools["config.validate"];
        Assert.Equal("Validate a session configuration document", configValidate.GetProperty("title").GetString());
        AssertReadOnlyIdempotent(configValidate);
        Assert.Equal("string", configValidate.GetProperty("inputSchema").GetProperty("properties").GetProperty("projectId").GetProperty("type").GetString());

        var configPatch = tools["config.patch"];
        Assert.Equal("Patch and validate a session configuration document", configPatch.GetProperty("title").GetString());
        Assert.False(configPatch.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        Assert.False(configPatch.GetProperty("annotations").GetProperty("destructiveHint").GetBoolean());
        var testsList = tools["tests.list"];
        Assert.Equal("List registered session tests", testsList.GetProperty("title").GetString());
        AssertReadOnlyIdempotent(testsList);
        var testsRun = tools["tests.run"];
        Assert.False(testsRun.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());
        var testsResult = tools["tests.result"];
        AssertReadOnlyIdempotent(testsResult);
        AssertReadOnlyIdempotent(tools["knowledge.tree"]);
        AssertReadOnlyIdempotent(tools["knowledge.search"]);
        AssertReadOnlyIdempotent(tools["knowledge.read"]);
        AssertReadOnlyIdempotent(tools["knowledge.links"]);
        var projectsList = tools["projects.list"];
        Assert.Equal("List authorized pilot projects", projectsList.GetProperty("title").GetString());
        AssertReadOnlyIdempotent(projectsList);
        var status = tools["runtime.status"];
        Assert.Equal("PatchPony runtime status", status.GetProperty("title").GetString());
        AssertReadOnlyIdempotent(status);
        Assert.Equal("object", status.GetProperty("inputSchema").GetProperty("type").GetString());
        Assert.Empty(status.GetProperty("inputSchema").GetProperty("properties").EnumerateObject());
        Assert.All(tools.Values, tool => Assert.False(tool.TryGetProperty("outputSchema", out _)));

        var validateCorrelation = tools["runtime.validate_correlation"];
        Assert.Equal("Validate correlation identifier", validateCorrelation.GetProperty("title").GetString());
        AssertReadOnlyIdempotent(validateCorrelation);
        var inputSchema = validateCorrelation.GetProperty("inputSchema");
        Assert.Equal("object", inputSchema.GetProperty("type").GetString());
        Assert.Equal("string", inputSchema.GetProperty("properties").GetProperty("correlationId").GetProperty("type").GetString());
        Assert.Equal(["correlationId"], inputSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
    }

    [Fact]
    public async Task McpProjectsList_ReturnsOnlyAuthorizedPilotProjectMetadata()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var manifest = Path.Combine(repositoryRoot, "integrations", "pilot-projects", "patchpony.yaml");
        await using var sourceFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.UseSetting("PatchPony:Auth:DevelopmentPassword", "projects-test-password");
            builder.UseSetting("PatchPony:Auth:DevelopmentProjects", "patchpony");
            builder.UseSetting("PatchPony:PilotSources:Projects:0:Id", "patchpony");
            builder.UseSetting("PatchPony:PilotSources:Projects:0:CheckoutRoot", repositoryRoot);
            builder.UseSetting("PatchPony:PilotSources:Projects:0:ManifestFile", manifest);
        });
        using var sourceClient = sourceFactory.CreateClient();
        sourceClient.DefaultRequestHeaders.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "projects-test-password");

        using var response = await sourceClient.SendAsync(SendMcpToolCall(sourceClient, "projects.list", "{}"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("patchpony", body, StringComparison.Ordinal);
        Assert.Contains("PatchPony", body, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutRoot", body, StringComparison.Ordinal);
        Assert.DoesNotContain("remoteUrl", body, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task McpSourceRead_ReturnsAProjectBoundCitationAndRejectsForbiddenFiles()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var manifest = Path.Combine(repositoryRoot, "integrations", "pilot-projects", "patchpony.yaml");
        await using var sourceFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.UseSetting("PatchPony:Auth:DevelopmentPassword", "source-test-password");
            builder.UseSetting("PatchPony:Auth:DevelopmentProjects", "patchpony");
            builder.UseSetting("PatchPony:PilotSources:Projects:0:Id", "patchpony");
            builder.UseSetting("PatchPony:PilotSources:Projects:0:CheckoutRoot", repositoryRoot);
            builder.UseSetting("PatchPony:PilotSources:Projects:0:ManifestFile", manifest);
        });
        using var sourceClient = sourceFactory.CreateClient();
        sourceClient.DefaultRequestHeaders.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "source-test-password");

        using var allowed = await sourceClient.SendAsync(SendMcpToolCall(sourceClient, "source.read", "{\"projectId\":\"patchpony\",\"path\":\"README.md\"}"));
        using var forbidden = await sourceClient.SendAsync(SendMcpToolCall(sourceClient, "source.read", "{\"projectId\":\"patchpony\",\"path\":\".env\"}"));
        var allowedBody = await allowed.Content.ReadAsStringAsync();
        var forbiddenBody = await forbidden.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Contains("patchpony", allowedBody, StringComparison.Ordinal);
        Assert.Contains("README.md", allowedBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, forbidden.StatusCode);
        Assert.Contains("path.forbidden", forbiddenBody, StringComparison.Ordinal);
    }

    private static HttpRequestMessage SendMcpToolCall(HttpClient sourceClient, string tool, string arguments)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent($"{{\"jsonrpc\":\"2.0\",\"id\":77,\"method\":\"tools/call\",\"params\":{{\"name\":\"{tool}\",\"arguments\":{arguments}}}}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }
    private async Task<JsonDocument> GetMcpResponseDocument(string method, string parameters)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent($"{{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"{method}\",\"params\":{parameters}}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var json = content.Split('\n').Single(line => line.StartsWith("data: ", StringComparison.Ordinal))["data: ".Length..];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(json);
    }

    private static void AssertReadOnlyIdempotent(JsonElement tool)
    {
        var annotations = tool.GetProperty("annotations");
        Assert.True(annotations.GetProperty("readOnlyHint").GetBoolean());
        Assert.True(annotations.GetProperty("idempotentHint").GetBoolean());
        Assert.False(annotations.GetProperty("destructiveHint").GetBoolean());
        Assert.False(annotations.GetProperty("openWorldHint").GetBoolean());
    }
    [Fact]
    public async Task McpInitialize_ConformsToTheStreamableHttpInitializationContract()
    {
        const string parameters = """{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"patchpony-conformance","version":"1.0.0"}}""";

        using var document = await GetMcpResponseDocument("initialize", parameters);
        var result = document.RootElement.GetProperty("result");

        Assert.Equal("2025-03-26", result.GetProperty("protocolVersion").GetString());
        Assert.True(result.GetProperty("capabilities").TryGetProperty("tools", out _));
        Assert.True(result.GetProperty("capabilities").TryGetProperty("logging", out _));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("serverInfo").GetProperty("name").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("serverInfo").GetProperty("version").GetString()));
    }
    [Fact]
    public async Task OpenApiDocument_DescribesTheVersionedRestContract()
    {
        using var response = await client.GetAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PatchPony REST API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", document.RootElement.GetProperty("info").GetProperty("version").GetString());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1", out var apiInfo));
        Assert.True(paths.TryGetProperty("/api/v1/runtime/status", out var runtimeStatus));
        Assert.True(paths.TryGetProperty("/api/v1/runtime/identity", out var runtimeIdentity));
        Assert.True(paths.TryGetProperty("/api/v1/projects/{projectId}/access", out var projectAccess));
        Assert.True(paths.TryGetProperty("/api/v1/runtime/correlations/{correlationId}", out var correlationValidation));
        Assert.Equal("ApiV1Info", apiInfo.GetProperty("get").GetProperty("operationId").GetString());
        Assert.Equal("RuntimeStatus", runtimeStatus.GetProperty("get").GetProperty("operationId").GetString());
        Assert.Equal("RuntimeIdentity", runtimeIdentity.GetProperty("get").GetProperty("operationId").GetString());
        Assert.Equal("ProjectToolAccessCheck", projectAccess.GetProperty("post").GetProperty("operationId").GetString());
        Assert.Equal("RuntimeCorrelationValidation", correlationValidation.GetProperty("get").GetProperty("operationId").GetString());
        Assert.False(paths.TryGetProperty("/mcp", out _));
    }
    [Fact]
    public async Task SwaggerUi_IsDeniedOutsideDevelopment()
    {
        await using var productionFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("environment", "Production"));
        using var productionClient = productionFactory.CreateClient();
        using var response = await productionClient.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        await using var developmentFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("environment", "Development"));
        using var developmentClient = developmentFactory.CreateClient();
        using var response = await developmentClient.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task RuntimeIdentity_RequiresTheConfiguredDevelopmentPassword()
    {
        await using var localFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PatchPony:Auth:DevelopmentPassword"] = "local-test-password"
                }));
        });
        using var localClient = localFactory.CreateClient();

        using var missingPassword = await localClient.GetAsync("/api/v1/runtime/identity");
        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        invalidRequest.Headers.Add("X-PatchPony-Development-Password", "wrong-password");
        using var invalidPassword = await localClient.SendAsync(invalidRequest);
        using var invalidBearerRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        invalidBearerRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        using var invalidBearer = await localClient.SendAsync(invalidBearerRequest);
        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        validRequest.Headers.Add("X-PatchPony-Development-Password", "local-test-password");
        using var validPassword = await localClient.SendAsync(validRequest);
        using var identity = JsonDocument.Parse(await validPassword.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, missingPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidBearer.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validPassword.StatusCode);
        Assert.Equal("local-developer", identity.RootElement.GetProperty("subject").GetString());
        Assert.Equal("development-password", identity.RootElement.GetProperty("authenticationMode").GetString());
        Assert.Equal([PatchPonyRoles.CodeReader, PatchPonyRoles.IssuePlanner, PatchPonyRoles.KnowledgeReader, PatchPonyRoles.Reviewer], identity.RootElement.GetProperty("roles").EnumerateArray().Select(value => value.GetString()).OrderBy(value => value, StringComparer.Ordinal));
        Assert.Contains(PatchPonyScopes.SourceRead, identity.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()));
        Assert.DoesNotContain(PatchPonyScopes.ConfigWrite, identity.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()));
    }
[Fact]
    public void JwtBearer_UsesTheConfiguredOidcAuthorityAndAudience()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PatchPony:Auth:Oidc:Authority"] = "https://identity.example.test",
                ["PatchPony:Auth:Oidc:Audience"] = "patchpony-api",
                ["PatchPony:Auth:Oidc:RequireHttpsMetadata"] = "true"
            })
            .Build();
        var authentication = configuration.GetSection(PatchPonyAuthenticationOptions.SectionName).Get<PatchPonyAuthenticationOptions>();

        Assert.NotNull(authentication);
        Assert.Equal("https://identity.example.test", authentication.Oidc.Authority);
        Assert.Equal("patchpony-api", authentication.Oidc.Audience);
        Assert.True(authentication.Oidc.RequireHttpsMetadata);
    }
    [Fact]
    public async Task RuntimeIdentity_AuthenticatesTheConfiguredN8nServiceAccount()
    {
        await using var serviceFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PatchPony:Auth:N8n:Token"] = "n8n-test-token"
            })));
        using var serviceClient = serviceFactory.CreateClient();

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        invalidRequest.Headers.Add(N8nServiceAuthenticationHandler.HeaderName, "wrong-token");
        using var invalidToken = await serviceClient.SendAsync(invalidRequest);
        using var duplicateRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        duplicateRequest.Headers.Add(N8nServiceAuthenticationHandler.HeaderName, "n8n-test-token");
        duplicateRequest.Headers.Add(N8nServiceAuthenticationHandler.HeaderName, "second-token");
        using var duplicateToken = await serviceClient.SendAsync(duplicateRequest);
        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/identity");
        validRequest.Headers.Add(N8nServiceAuthenticationHandler.HeaderName, "n8n-test-token");
        using var validToken = await serviceClient.SendAsync(validRequest);
        using var identity = JsonDocument.Parse(await validToken.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validToken.StatusCode);
        Assert.Equal("service-n8n", identity.RootElement.GetProperty("subject").GetString());
        Assert.Equal("service-token", identity.RootElement.GetProperty("authenticationMode").GetString());
        Assert.Equal([PatchPonyRoles.ServiceN8n], identity.RootElement.GetProperty("roles").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains(PatchPonyScopes.KnowledgeRead, identity.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()));
        Assert.DoesNotContain(PatchPonyScopes.KnowledgeWrite, identity.RootElement.GetProperty("scopes").EnumerateArray().Select(value => value.GetString()));
    }
    [Fact]
    public async Task RoleScopePolicies_GrantReadOnlyScopesButNotWriteScopes()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("roles", PatchPonyRoles.ConfigEditor)],
            "test"));
        var transformed = await new ReadOnlyRoleScopeClaimsTransformation().TransformAsync(principal);
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();

        var canReadConfig = await authorization.AuthorizeAsync(transformed, PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.ConfigRead));
        var canWriteConfig = await authorization.AuthorizeAsync(transformed, PatchPonyAuthorization.ScopePolicy(PatchPonyScopes.ConfigWrite));
        var hasConfigEditorRole = await authorization.AuthorizeAsync(transformed, PatchPonyAuthorization.RolePolicy(PatchPonyRoles.ConfigEditor));

        Assert.True(canReadConfig.Succeeded);
        Assert.False(canWriteConfig.Succeeded);
        Assert.True(hasConfigEditorRole.Succeeded);
    }
    [Fact]
    public async Task ProjectToolAccess_RequiresExactProjectScopeAndPermittedParameters()
    {
        await using var localFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PatchPony:Auth:DevelopmentPassword"] = "local-test-password",
                    ["PatchPony:Auth:DevelopmentProjects"] = "demo-project"
                }));
        });
        using var localClient = localFactory.CreateClient();

        using var allowedRequest = CreateProjectAccessRequest("demo-project", "source.read", "{\"path\":\"src/Program.cs\"}");
        using var allowed = await localClient.SendAsync(allowedRequest);
        using var forbiddenProjectRequest = CreateProjectAccessRequest("other-project", "source.read", "{\"path\":\"src/Program.cs\"}");
        using var forbiddenProject = await localClient.SendAsync(forbiddenProjectRequest);
        using var forbiddenParameterRequest = CreateProjectAccessRequest("demo-project", "source.read", "{\"shell\":\"cmd.exe\"}");
        using var forbiddenParameter = await localClient.SendAsync(forbiddenParameterRequest);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenProject.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenParameter.StatusCode);
    }

    [Fact]
    public async Task ConfigValidateRest_UsesOnlyTheServerConfiguredSessionWorktree()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "config"));
        var manifest = Path.Combine(root, "project.yaml");
        var sessionId = Guid.NewGuid().ToString("N");
        try
        {
            var source = "{\"enabled\":true}"u8.ToArray();
            var replacement = "{\"enabled\":false}"u8.ToArray();
            File.WriteAllBytes(Path.Combine(root, "config", "settings.json"), source);
            File.WriteAllText(manifest, """
schemaVersion: 1
project:
  id: demo-project
  displayName: Demo
repository:
  remoteUrl: https://github.com/example/demo.git
  defaultBranch: main
paths:
  readable: [config/**]
  writable: [config/**]
  forbidden: [.env, '**/.env', '**/.env.*']
""");
            await using var configFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.UseSetting("PatchPony:Auth:DevelopmentPassword", "config-test-password");
                builder.UseSetting("PatchPony:Auth:DevelopmentProjects", "demo-project");
                builder.UseSetting("PatchPony:Auth:DevelopmentConfigEditorProjects", "demo-project");
                builder.UseSetting("PatchPony:ConfigSessions:Sessions:0:SessionId", sessionId);
                builder.UseSetting("PatchPony:ConfigSessions:Sessions:0:ProjectId", "demo-project");
                builder.UseSetting("PatchPony:ConfigSessions:Sessions:0:WorktreeRoot", root);
                builder.UseSetting("PatchPony:ConfigSessions:Sessions:0:ManifestFile", manifest);
            });
            using var configClient = configFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/demo-project/sessions/{sessionId}/config/validate")
            {
                Content = new StringContent("{\"path\":\"config/settings.json\",\"format\":\"json\"}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "config-test-password");

            using var response = await configClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"isValid\":true", body, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task AccessDecisionAudit_ReportsAllowedAndRejectedProjectToolDecisionsWithoutSecretsOrParameterValues()
    {
        await using var localFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PatchPony:Auth:DevelopmentPassword"] = "local-test-password",
                    ["PatchPony:Auth:DevelopmentProjects"] = "demo-project"
                }));
        });
        using var localClient = localFactory.CreateClient();

        using var allowedRequest = CreateProjectAccessRequest("demo-project", "source.read", "{\"path\":\"src/Program.cs\"}");
        using var rejectedRequest = CreateProjectAccessRequest("demo-project", "source.read", "{\"shell\":\"cmd.exe\"}");
        using var allowed = await localClient.SendAsync(allowedRequest);
        using var rejected = await localClient.SendAsync(rejectedRequest);
        using var auditRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/audit/access");
        auditRequest.Headers.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "local-test-password");
        using var audit = await localClient.SendAsync(auditRequest);
        var auditJson = await audit.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.Contains("policy.tool", auditJson, StringComparison.Ordinal);
        Assert.Contains("allowed", auditJson, StringComparison.Ordinal);
        Assert.Contains("rejected", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("local-test-password", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("src/Program.cs", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("cmd.exe", auditJson, StringComparison.Ordinal);
    }
    private static HttpRequestMessage CreateProjectAccessRequest(string projectId, string tool, string parameters)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/access")
        {
            Content = new StringContent($"{{\"tool\":\"{tool}\",\"parameters\":{parameters}}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "local-test-password");
        return request;
    }
    [Fact]
    public async Task RateLimit_RejectsExcessApiRequestsWithRetryAfterAndKeepsHealthUnrestricted()
    {
        await using var limitedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PatchPony:Auth:DevelopmentPassword"] = "rate-limit-password"
                }));
        });
        using var limitedClient = limitedFactory.CreateClient();

        async Task<HttpResponseMessage> SendStatusAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runtime/status");
            request.Headers.Add(DevelopmentPasswordAuthenticationHandler.HeaderName, "rate-limit-password");
            return await limitedClient.SendAsync(request);
        }

        var permitted = new List<HttpResponseMessage>();
        for (var request = 0; request < 60; request++)
        {
            permitted.Add(await SendStatusAsync());
        }

        using var rejected = await SendStatusAsync();
        using var health = await limitedClient.GetAsync("/health/live");
        var rejectedBody = await rejected.Content.ReadAsStringAsync();

        Assert.All(permitted, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("60", rejected.Headers.RetryAfter?.Delta?.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("request.rate_limited", rejectedBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        foreach (var response in permitted)
        {
            response.Dispose();
        }
    }
    [Fact]
    public async Task DefaultDeny_RejectsUnauthenticatedApiAndMcpRequestsButKeepsHealthPublic()
    {
        using var anonymousClient = factory.CreateClient();
        using var api = await anonymousClient.GetAsync("/api/v1/runtime/status");
        using var health = await anonymousClient.GetAsync("/health/live");
        using var mcp = await anonymousClient.PostAsync("/mcp", new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mcp.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
