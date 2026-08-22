using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PatchPony.Infrastructure.Manifests;

public sealed class ProjectManifestLoader
{
    public const int MaximumManifestBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithDuplicateKeyChecking()
        .Build();

    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    public Result<ProjectManifest> Load(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest must not be empty."));
        }

        if (Encoding.UTF8.GetByteCount(yaml) > MaximumManifestBytes)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.too_large", "The project manifest exceeds the 64 KiB limit."));
        }

        ManifestDocument document;
        try
        {
            RejectUnsupportedYamlFeatures(yaml);
            document = Deserializer.Deserialize<ManifestDocument>(yaml);
        }
        catch (YamlException)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest is not valid YAML."));
        }
        catch (InvalidOperationException)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest uses an unsupported YAML feature."));
        }

        using var instance = JsonDocument.Parse(JsonSerializer.Serialize(document, JsonOptions));
        var evaluation = Schema.Value.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!evaluation.IsValid)
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest does not satisfy schema version 1."));
        }

        if (!Uri.TryCreate(document.Repository!.RemoteUrl, UriKind.Absolute, out var remoteUri))
        {
            return Result<ProjectManifest>.Failure(new DomainError("manifest.invalid", "The project manifest has no absolute repository URL."));
        }

        return Result<ProjectManifest>.Success(new ProjectManifest(
            document.SchemaVersion,
            new ProjectManifestProject(document.Project!.Id!, document.Project.DisplayName!),
            new ProjectManifestRepository(remoteUri, document.Repository.DefaultBranch!),
            new ProjectManifestPaths(
                document.Paths!.Readable!,
                document.Paths.Writable!,
                document.Paths.Forbidden!)));
    }

    private static void RejectUnsupportedYamlFeatures(string yaml)
    {
        var documentCount = 0;
        var parser = new Parser(new StringReader(yaml));
        while (parser.MoveNext())
        {
            switch (parser.Current)
            {
                case DocumentStart:
                    documentCount++;
                    if (documentCount > 1)
                    {
                        throw new InvalidOperationException("Multiple YAML documents are not supported.");
                    }

                    break;
                case AnchorAlias:
                    throw new InvalidOperationException("YAML aliases are not supported.");
                case NodeEvent node when !node.Anchor.IsEmpty || !node.Tag.IsEmpty:
                    throw new InvalidOperationException("YAML anchors and tags are not supported.");
            }
        }
    }

    private static JsonSchema LoadSchema()
    {
        var assembly = typeof(ProjectManifestLoader).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("patchpony-project.schema.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The embedded project-manifest schema is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    private sealed class ManifestDocument
    {
        public int SchemaVersion { get; init; }
        public ManifestProject? Project { get; init; }
        public ManifestRepository? Repository { get; init; }
        public ManifestPaths? Paths { get; init; }
    }

    private sealed class ManifestProject
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
    }

    private sealed class ManifestRepository
    {
        public string? RemoteUrl { get; init; }
        public string? DefaultBranch { get; init; }
    }

    private sealed class ManifestPaths
    {
        public List<string>? Readable { get; init; }
        public List<string>? Writable { get; init; }
        public List<string>? Forbidden { get; init; }
    }
}
