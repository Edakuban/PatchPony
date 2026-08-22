using PatchPony.Core.Common;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class CorrelationMiddlewareTests(WebApplicationFactory<global::Program> factory)
    : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Request_WithValidCorrelationHeader_PreservesItInTheResponse()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationMiddleware.HeaderName, "request-42");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("request-42", response.Headers.GetValues(CorrelationMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Request_WithoutCorrelationHeader_GeneratesOne()
    {
        using var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.TryGetValues(CorrelationMiddleware.HeaderName, out var values));
        Assert.True(CorrelationId.Create(values.Single()).IsSuccess);
    }
}
