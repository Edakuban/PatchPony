using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigurationChangeKeyValidatorTests
{
    [Fact]
    public void Validate_AcceptsOnlyAllowlistedTypedValues()
    {
        var request = Request(ConfigurationEnvironment.Staging, [Target("config/retries", "3"), Target("config/log-level", "warning")]);
        var policies = new[]
        {
            Policy(ConfigurationEnvironment.Staging, "config/retries", ConfigurationValueType.Integer, minimum: 0, maximum: 5),
            Policy(ConfigurationEnvironment.Staging, "config/log-level", ConfigurationValueType.Enum, values: ["debug", "warning", "error"])
        };

        var result = ConfigurationChangeKeyValidator.Validate(request, policies);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Targets.Count);
        Assert.Equal(ConfigurationValueType.Integer, result.Value.Targets[0].ValueType);
    }

    [Theory]
    [InlineData("config/unknown", "x", "config.key.unknown")]
    [InlineData("config/retries", "9", "config.value.invalid")]
    public void Validate_RejectsUnknownKeysAndOutOfRangeValues(string key, string value, string errorCode)
    {
        var request = Request(ConfigurationEnvironment.Development, [Target(key, value)]);
        var policies = new[] { Policy(ConfigurationEnvironment.Development, "config/retries", ConfigurationValueType.Integer, minimum: 0, maximum: 5) };

        var result = ConfigurationChangeKeyValidator.Validate(request, policies);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Error.Code);
    }

    [Fact]
    public void Validate_FailsClosedForMissingOrAmbiguousPolicies()
    {
        var request = Request(ConfigurationEnvironment.Production, [Target("config/retries", "2")]);
        var policy = Policy(ConfigurationEnvironment.Production, "config/retries", ConfigurationValueType.Integer, minimum: 0, maximum: 5);

        Assert.Equal("config.key_policy.unconfigured", ConfigurationChangeKeyValidator.Validate(request, []).Error.Code);
        Assert.Equal("config.key_policy.unconfigured", ConfigurationChangeKeyValidator.Validate(request, [policy, policy]).Error.Code);
    }

    private static EnvironmentConfigurationChangeRequest Request(ConfigurationEnvironment environment, IReadOnlyList<RequestedChangeTarget> targets)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: TicketChannel.ChangeRequest).Value!, "patchpony").Value!;
        var change = TicketChangeRequest.Create(ticket, RequestedChangeKind.Configuration, targets).Value!;
        return EnvironmentConfigurationChangeRequest.Create(change, environment).Value!;
    }

    private static RequestedChangeTarget Target(string key, string value) => RequestedChangeTarget.Create(key, value).Value!;
    private static KnownConfigurationKeyPolicy Policy(ConfigurationEnvironment environment, string key, ConfigurationValueType type, long? minimum = null, long? maximum = null, IReadOnlyList<string>? values = null) => KnownConfigurationKeyPolicy.Create("patchpony", environment, key, type, minimum, maximum, allowedValues: values).Value!;
}