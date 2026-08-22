using System.Net;
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
}
