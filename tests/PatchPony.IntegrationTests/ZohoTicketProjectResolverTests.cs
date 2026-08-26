using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoTicketProjectResolverTests
{
    [Fact]
    public void Resolve_MapsOnlyConfiguredZohoProjectIds()
    {
        var resolver = CreateResolver(("1362699000013318565", "patchpony"));
        var ticket = NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!;

        var result = resolver.Resolve(ticket, "1362699000013318565");

        Assert.True(result.IsSuccess);
        Assert.Equal("patchpony", result.Value!.ProjectManifestId);
    }

    [Fact]
    public void Resolve_FailsClosedForUnmappedOrAmbiguousProjectIds()
    {
        var ticket = NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!;

        Assert.Equal("ticket.project_mapping.unmapped", CreateResolver(("1362699000013318566", "vocavid")).Resolve(ticket, "1362699000013318565").Error.Code);
        Assert.Equal("ticket.project_mapping.ambiguous", CreateResolver(("1362699000013318565", "patchpony"), ("1362699000013318565", "vocavid")).Resolve(ticket, "1362699000013318565").Error.Code);
    }

    private static ZohoTicketProjectResolver CreateResolver(params (string ZohoProjectId, string Project)[] mappings)
    {
        var values = new Dictionary<string, string?>();
        for (var index = 0; index < mappings.Length; index++)
        {
            values[$"PatchPony:Zoho:ProjectMappings:{index}:ZohoProjectId"] = mappings[index].ZohoProjectId;
            values[$"PatchPony:Zoho:ProjectMappings:{index}:ProjectManifestId"] = mappings[index].Project;
        }
        return new ZohoTicketProjectResolver(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}