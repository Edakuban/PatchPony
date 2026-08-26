using PatchPony.Infrastructure.Configuration;
using PatchPony.Infrastructure.Knowledge;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeFrontmatterParserTests
{
    [Fact]
    public void Validate_AcceptsInitialFrontmatterAndOptionalServerSchema()
    {
        var schemas = new JsonSchemaCatalog([new JsonSchemaRegistration("knowledge.page.v1", """{ "type":"object", "required":["title"], "properties":{"title":{"type":"string"}} }""")]);
        var result = new KnowledgeFrontmatterParser(schemas).Validate("---\ntitle: Start\ntags: [guide]\n---\n# Body", "knowledge.page.v1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasFrontmatter);
        Assert.True(result.Value.Report.IsValid);
    }

    [Fact]
    public void Validate_RejectsUnsafeYamlAndUnterminatedBlocks()
    {
        var aliases = new KnowledgeFrontmatterParser().Validate("---\nbase: &base value\ncopy: *base\n---\n# Body");
        var unterminated = new KnowledgeFrontmatterParser().Validate("---\ntitle: Start\n# Body");

        Assert.False(aliases.Value!.Report.IsValid);
        Assert.Equal("yaml.anchor_disallowed", aliases.Value.Report.Issues[0].Code);
        Assert.False(unterminated.Value!.Report.IsValid);
        Assert.Equal("frontmatter.unterminated", unterminated.Value.Report.Issues[0].Code);
    }

    [Fact]
    public void Validate_RejectsExcessiveNestingAndLeavesBodyWithoutFrontmatterUntouched()
    {
        var nested = string.Concat(Enumerable.Range(0, KnowledgeFrontmatterParser.MaximumYamlDepth + 1).Select(index => new string(' ', index * 2) + $"level{index}:\n"));
        var tooDeep = new KnowledgeFrontmatterParser().Validate($"---\n{nested}{new string(' ', (KnowledgeFrontmatterParser.MaximumYamlDepth + 1) * 2)}value: ok\n---");
        var absent = new KnowledgeFrontmatterParser().Validate("# Plain Markdown\n---\nnot frontmatter");

        Assert.False(tooDeep.Value!.Report.IsValid);
        Assert.Equal("frontmatter.too_deep", tooDeep.Value.Report.Issues[0].Code);
        Assert.False(absent.Value!.HasFrontmatter);
        Assert.True(absent.Value.Report.IsValid);
    }

    [Fact]
    public void Validate_RejectsFrontmatterAboveTheByteLimit()
    {
        var content = "---\ntitle: " + new string('x', KnowledgeFrontmatterParser.MaximumFrontmatterBytes) + "\n---\n# Body";
        var result = new KnowledgeFrontmatterParser().Validate(content);

        Assert.False(result.Value!.Report.IsValid);
        Assert.Equal("frontmatter.too_large", result.Value.Report.Issues[0].Code);
    }
}
