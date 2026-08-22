using System.Net;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PatchPony.IntegrationTests;

public sealed class GatewayHealthEndpointTests(WebApplicationFactory<global::Program> factory)
    : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsOk(string path)
    {
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    }}
