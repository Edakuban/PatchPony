using PatchPony.Core.Common;
using PatchPony.Core.Configuration;

namespace PatchPony.Core.Tests;

public sealed class ConfigAdapterCatalogTests
{
    [Fact]
    public void Validate_RoutesOnlyToTheServerRegisteredAdapter()
    {
        var json = new RecordingAdapter(ConfigDocumentFormat.Json);
        var yaml = new RecordingAdapter(ConfigDocumentFormat.Yaml);
        var catalog = new ConfigAdapterCatalog([json, yaml]);

        var result = catalog.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray(), "project.config"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsValid);
        Assert.Equal(1, json.Calls);
        Assert.Equal(0, yaml.Calls);
    }

    [Fact]
    public void Validate_RejectsUnregisteredFormatsAndUnsafeSchemaReferences()
    {
        var catalog = new ConfigAdapterCatalog([new RecordingAdapter(ConfigDocumentFormat.Json)]);

        var unregistered = catalog.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, "<x/>"u8.ToArray()));
        var unsafeSchema = catalog.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray(), "../schema"));

        Assert.False(unregistered.IsSuccess);
        Assert.Equal("config.adapter.unsupported", unregistered.Error.Code);
        Assert.False(unsafeSchema.IsSuccess);
        Assert.Equal("validation.invalid", unsafeSchema.Error.Code);
    }

    [Fact]
    public void Catalog_RejectsDuplicateFormatRegistrationAndMismatchedReports()
    {
        Assert.Throws<ArgumentException>(() => new ConfigAdapterCatalog([new RecordingAdapter(ConfigDocumentFormat.Json), new RecordingAdapter(ConfigDocumentFormat.Json)]));
        var catalog = new ConfigAdapterCatalog([new MismatchedAdapter()]);

        var result = catalog.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray()));

        Assert.False(result.IsSuccess);
        Assert.Equal("config.adapter.invalid_report", result.Error.Code);
    }

    private sealed class RecordingAdapter(ConfigDocumentFormat format) : IConfigDocumentAdapter
    {
        public ConfigDocumentFormat Format => format;
        public int Calls { get; private set; }
        public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Result<ConfigValidationReport>.Success(new ConfigValidationReport(request.Format, true, []));
        }
    }

    private sealed class MismatchedAdapter : IConfigDocumentAdapter
    {
        public ConfigDocumentFormat Format => ConfigDocumentFormat.Json;
        public Result<ConfigValidationReport> Validate(ConfigValidationRequest request, CancellationToken cancellationToken = default) =>
            Result<ConfigValidationReport>.Success(new ConfigValidationReport(ConfigDocumentFormat.Xml, true, []));
    }
}