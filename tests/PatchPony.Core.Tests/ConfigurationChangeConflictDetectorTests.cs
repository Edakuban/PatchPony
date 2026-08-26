using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigurationChangeConflictDetectorTests
{
    [Fact]
    public void Validate_RejectsConfiguredForbiddenValuePairs()
    {
        var request = Request([Target("config/auth-mode", "disabled"), Target("config/public-access", "true")]);
        var validation = Validation(request);
        var rule = ConfigurationConflictRule.Create("patchpony", ConfigurationEnvironment.Production, "config/auth-mode", "disabled", "config/public-access", "true").Value!;

        var result = ConfigurationChangeConflictDetector.Validate(request, validation, [rule]);

        Assert.False(result.IsSuccess);
        Assert.Equal("config.values.conflict", result.Error.Code);
    }

    [Fact]
    public void Validate_AcceptsNonConflictingValuesAndRejectsUnvalidatedInput()
    {
        var request = Request([Target("config/auth-mode", "required"), Target("config/public-access", "true")]);
        var validation = Validation(request);
        var rule = ConfigurationConflictRule.Create("patchpony", ConfigurationEnvironment.Production, "config/auth-mode", "disabled", "config/public-access", "true").Value!;

        Assert.True(ConfigurationChangeConflictDetector.Validate(request, validation, [rule]).IsSuccess);
        Assert.Equal("config.conflict_input.invalid", ConfigurationChangeConflictDetector.Validate(request, new ConfigurationChangeKeyValidation([]), [rule]).Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request(IReadOnlyList<RequestedChangeTarget> targets)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        return EnvironmentConfigurationChangeRequest.Create(TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, targets).Value!, ConfigurationEnvironment.Production).Value!;
    }

    private static ConfigurationChangeKeyValidation Validation(EnvironmentConfigurationChangeRequest request) => new(request.ChangeRequest.Targets.Select(target => new ValidatedConfigurationChangeTarget(target.Reference, ConfigurationValueType.Enum)).ToArray());
    private static RequestedChangeTarget Target(string key, string value) => RequestedChangeTarget.Create(key, value).Value!;
}