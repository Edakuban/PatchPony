using System.Text;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;
using PatchPony.Infrastructure.Source;

namespace PatchPony.IntegrationTests;

public sealed class SourceReaderTests
{
    [Fact]
    public void Read_ReturnsNumberedUtf8LinesOnlyForAllowedFiles()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteText(root, "src/Program.cs", "// Grüß Gott\nConsole.WriteLine(\"hello\");");
            WriteText(root, "docs/private.md", "not allowed");
            var reader = CreateReader(root);

            var allowed = reader.Read("src/Program.cs");
            var denied = reader.Read("docs/private.md");

            Assert.True(allowed.IsSuccess);
            Assert.Equal("src/Program.cs", allowed.Value!.RelativePath);
            Assert.Equal([(1, "// Grüß Gott"), (2, "Console.WriteLine(\"hello\");")], allowed.Value.Lines.Select(line => (line.Number, line.Text)));
            Assert.Equal("path.not_readable", denied.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_EnforcesTheLineLimit()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteText(root, "src/ManyLines.cs", string.Join('\n', Enumerable.Range(1, 501).Select(number => $"line {number}")));

            var result = CreateReader(root).Read("src/ManyLines.cs");

            Assert.True(result.IsSuccess);
            Assert.Equal(500, result.Value!.Lines.Count);
            Assert.Equal((500, "line 500"), (result.Value.Lines[^1].Number, result.Value.Lines[^1].Text));
            Assert.True(result.Value.IsTruncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("binary", "source.binary")]
    [InlineData("invalid-utf8", "source.invalid_encoding")]
    [InlineData("large", "source.too_large")]
    public void Read_RejectsBinaryInvalidEncodingAndOversizedFiles(string kind, string expectedCode)
    {
        var root = CreateTemporaryRoot();
        try
        {
            var path = Path.Combine(root, "src", "Unsafe.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var content = kind switch
            {
                "binary" => new byte[] { 1, 0, 2 },
                "invalid-utf8" => new byte[] { 0xC3, 0x28 },
                "large" => new byte[(128 * 1024) + 1],
                _ => throw new InvalidOperationException()
            };
            if (kind == "large")
            {
                Array.Fill(content, (byte)'a');
            }

            File.WriteAllBytes(path, content);
            var result = CreateReader(root).Read("src/Unsafe.cs");

            Assert.False(result.IsSuccess);
            Assert.Equal(expectedCode, result.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static SourceReader CreateReader(string root)
    {
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(["src/**"], [], [".env", "**/.env"])).Value!;
        var resolver = new ProjectPathResolver(root);
        return new SourceReader(new ProjectPathAccessService(resolver, policy));
    }

    private static void WriteText(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
