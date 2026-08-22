using System.Text;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;
using PatchPony.Infrastructure.Skills;

namespace PatchPony.IntegrationTests;

public sealed class ProjectSkillCatalogTests
{
    [Fact]
    public void ListAndRead_ReturnOnlyAllowedSkillFilesWithoutExecutingThem()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteSkill(root, "release-notes", "# Release notes\n\nNever execute this content.");
            WriteSkill(root, "private", "# Private");
            WriteSkill(root, "Invalid_Id", "# Ignored");
            Directory.CreateDirectory(Path.Combine(root, ".patchpony", "skills", "missing-file"));
            var catalog = CreateCatalog(root);

            var listed = catalog.List();
            var read = catalog.Read("release-notes");

            Assert.True(listed.IsSuccess);
            Assert.Equal(["release-notes"], listed.Value!.Select(skill => skill.Id));
            Assert.True(read.IsSuccess);
            Assert.Equal("# Release notes\n\nNever execute this content.", read.Value!.Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_EnforcesPolicyAndInputValidation()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteSkill(root, "private", "# Private");
            var catalog = CreateCatalog(root);

            var forbidden = catalog.Read("private");
            var invalidId = catalog.Read("../private");

            Assert.Equal("path.forbidden", forbidden.Error.Code);
            Assert.Equal("skills.invalid_id", invalidId.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Read_RejectsSkillFilesOverTheConfiguredLimit()
    {
        var root = CreateTemporaryRoot();
        try
        {
            WriteSkill(root, "large", new string('a', (64 * 1024) + 1));
            var catalog = CreateCatalog(root);

            var result = catalog.Read("large");

            Assert.False(result.IsSuccess);
            Assert.Equal("skills.too_large", result.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ProjectSkillCatalog CreateCatalog(string root)
    {
        var policy = ProjectPathPolicy.Create(new ProjectManifestPaths(
            [".patchpony/skills/**"],
            [],
            [".patchpony/skills/private/**", ".env", "**/.env"])).Value!;
        var resolver = new ProjectPathResolver(root);
        return new ProjectSkillCatalog(resolver, new ProjectPathAccessService(resolver, policy));
    }

    private static void WriteSkill(string root, string id, string content)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, ".patchpony", "skills", id));
        File.WriteAllText(Path.Combine(directory.FullName, "SKILL.md"), content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
