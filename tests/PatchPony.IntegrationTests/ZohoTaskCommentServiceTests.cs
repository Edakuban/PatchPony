using System.Net;
using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoTaskCommentServiceTests
{
    [Fact]
    public async Task PostAsync_UsesConfiguredTaskCommentRouteAndBearerToken()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created);
        using var client = new HttpClient(handler);
        var service = new ZohoTaskCommentService(client, Configuration());
        var comment = TicketComment.Create("Plan bereit.", [new TicketCommentLink("Plan", new Uri("https://patchpony.example/plans/42"))]).Value!;

        var result = await service.PostAsync("1362699000013318565", "1362699000036844130", comment);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://projectsapi.zoho.eu/restapi/portal/hubermedia/projects/1362699000013318565/tasks/1362699000036844130/comments/", handler.Request!.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("access-token", handler.Request.Headers.Authorization.Parameter);
        Assert.Equal("content=Plan+bereit.%0A%0ALinks%3A%0A-+%5BPlan%5D%28https%3A%2F%2Fpatchpony.example%2Fplans%2F42%29", handler.Body);
    }

    [Fact]
    public async Task PostAsync_FailsClosedWithoutConfigurationOrForProviderErrors()
    {
        using var client = new HttpClient(new RecordingHandler(HttpStatusCode.BadGateway));
        var comment = TicketComment.Create("Plan bereit.").Value!;

        var unconfigured = await new ZohoTaskCommentService(client, new ConfigurationBuilder().Build()).PostAsync("1362699000013318565", "1362699000036844130", comment);
        var failed = await new ZohoTaskCommentService(client, Configuration()).PostAsync("1362699000013318565", "1362699000036844130", comment);

        Assert.Equal("zoho.provider.unconfigured", unconfigured.Error.Code);
        Assert.Equal("zoho.provider.request_failed", failed.Error.Code);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PatchPony:Zoho:ApiBaseUri"] = "https://projectsapi.zoho.eu",
        ["PatchPony:Zoho:PortalId"] = "hubermedia",
        ["PatchPony:Zoho:AccessToken"] = "access-token"
    }).Build();

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }
}