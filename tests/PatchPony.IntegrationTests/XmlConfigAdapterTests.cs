using PatchPony.Core.Configuration;
using PatchPony.Infrastructure.Configuration;

namespace PatchPony.IntegrationTests;

public sealed class XmlConfigAdapterTests
{
    [Fact]
    public void Validate_AcceptsXmlAndValidatesAgainstAServerXsd()
    {
        var adapter = new XmlConfigAdapter(new XmlSchemaCatalog([new XmlSchemaRegistration("profile.v1", Schema)]));

        var valid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, "<profile><name>PatchPony</name></profile>"u8.ToArray(), "profile.v1"));
        var schemaInvalid = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, "<profile><count>1</count></profile>"u8.ToArray(), "profile.v1"));

        Assert.True(valid.IsSuccess);
        Assert.True(valid.Value!.IsValid);
        Assert.True(schemaInvalid.IsSuccess);
        Assert.False(schemaInvalid.Value!.IsValid);
        Assert.Equal("xml.schema.invalid", Assert.Single(schemaInvalid.Value.Issues).Code);
    }

    [Fact]
    public void Validate_RejectsDtdAndExternalEntityDocumentsWithoutReadingTheirTarget()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var secretPath = Path.Combine(root, "not-to-be-read.txt");
        File.WriteAllText(secretPath, "external-entity-secret");
        try
        {
            var uri = new Uri(secretPath).AbsoluteUri;
            var dtd = new XmlConfigAdapter().Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, System.Text.Encoding.UTF8.GetBytes("<!DOCTYPE profile><profile/>")));
            var xxe = new XmlConfigAdapter().Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, System.Text.Encoding.UTF8.GetBytes($"<!DOCTYPE profile [<!ENTITY xxe SYSTEM '{uri}'>]><profile>&xxe;</profile>")));

            Assert.True(dtd.IsSuccess);
            Assert.False(dtd.Value!.IsValid);
            Assert.Equal("xml.dtd_disallowed", Assert.Single(dtd.Value.Issues).Code);
            Assert.True(xxe.IsSuccess);
            Assert.False(xxe.Value!.IsValid);
            Assert.Equal("xml.dtd_disallowed", Assert.Single(xxe.Value.Issues).Code);
            Assert.DoesNotContain("external-entity-secret", xxe.Value.Issues.Single().Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void Validate_RejectsMalformedWrongFormatAndUnknownSchema()
    {
        var adapter = new XmlConfigAdapter();

        var malformed = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, "<profile>"u8.ToArray()));
        var wrongFormat = adapter.Validate(new ConfigValidationRequest(ConfigDocumentFormat.Json, "{}"u8.ToArray()));
        var unknownSchema = new XmlConfigAdapter(new XmlSchemaCatalog([])).Validate(new ConfigValidationRequest(ConfigDocumentFormat.Xml, "<profile/>"u8.ToArray(), "missing.v1"));

        Assert.True(malformed.IsSuccess);
        Assert.False(malformed.Value!.IsValid);
        Assert.Equal("xml.invalid", Assert.Single(malformed.Value.Issues).Code);
        Assert.False(wrongFormat.IsSuccess);
        Assert.Equal("config.adapter.format_mismatch", wrongFormat.Error.Code);
        Assert.False(unknownSchema.IsSuccess);
        Assert.Equal("config.schema.unsupported", unknownSchema.Error.Code);
    }

    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="profile">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="name" type="xs:string" />
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;
}