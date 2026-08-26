using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoWebhookTests
{
    private const string Secret = "zoho-webhook-test-secret-012345678901234567890";
    private const string ZohoProjectId = "1362699000013318565";

    [Fact]
    public async Task Webhook_AcceptsOnlyAValidSignedClosedPayload()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var accepted = await client.SendAsync(CreateRequest("""{"eventType":"task.created","task":{"id":"1362699000036844130","revision":"17","projectId":"1362699000013318565","channel":"bug","title":"Login fails","description":"Steps to reproduce\n1. Open app"}}""", Secret));
        using var extraField = await client.SendAsync(CreateRequest("""{"eventType":"task.created","unexpected":true,"task":{"id":"1362699000036844130","revision":"17","projectId":"1362699000013318565","channel":"bug","title":"Login fails","description":"Steps"}}""", Secret));

        Assert.Equal(System.Net.HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, extraField.StatusCode);
        Assert.Contains("zoho.webhook.payload_invalid", await extraField.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Webhook_RejectsInvalidOrMissingSignaturesWithoutEchoingTicketData()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const string payload = """{"eventType":"task.updated","task":{"id":"1362699000036844130","revision":"18","projectId":"1362699000013318565","channel":"change_request","title":"Private task","description":"customer-secret-data"}}""";
        using var invalid = await client.SendAsync(CreateRequest(payload, "other-secret-012345678901234567890123456"));
        using var missing = new HttpRequestMessage(HttpMethod.Post, "/api/v1/integrations/zoho/tasks") { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        using var missingResponse = await client.SendAsync(missing);
        var invalidBody = await invalid.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingResponse.StatusCode);
        Assert.Contains("zoho.webhook.signature_invalid", invalidBody, StringComparison.Ordinal);
        Assert.DoesNotContain("customer-secret-data", invalidBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Webhook_FailsClosedWhenSecretIsNotConfigured()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(CreateRequest("""{"eventType":"task.created","task":{"id":"1362699000036844130","revision":"17","projectId":"1362699000013318565","channel":"generic_task","title":"Login fails","description":"Steps"}}""", Secret));

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["PatchPony:Zoho:WebhookSecret"] = Secret, ["PatchPony:Zoho:ProjectMappings:0:ZohoProjectId"] = ZohoProjectId, ["PatchPony:Zoho:ProjectMappings:0:ProjectManifestId"] = "patchpony", ["PatchPony:Zoho:CompletenessPolicies:0:ProjectManifestId"] = "patchpony", ["PatchPony:Zoho:CompletenessPolicies:0:MinimumDescriptionLength"] = "1" })));

    private static HttpRequestMessage CreateRequest(string payload, string signingSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/integrations/zoho/tasks") { Content = new ByteArrayContent(bytes) };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        request.Headers.Add(ZohoWebhookValidator.SignatureHeaderName, ZohoWebhookValidator.CreateSignatureForTesting(signingSecret, bytes));
        return request;
    }
}