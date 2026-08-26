using PatchPony.Core.Knowledge;

namespace PatchPony.Core.Tests;

public sealed class KnowledgeVaultContentPolicyTests
{
    [Theory]
    [InlineData(".obsidian/plugins/community/main.js", "knowledge.content.protected_path")]
    [InlineData(".obsidian/snippets/theme.css", "knowledge.content.protected_path")]
    [InlineData("automation/deploy.ps1", "knowledge.content.executable_disallowed")]
    [InlineData("tools/run.sh", "knowledge.content.executable_disallowed")]
    public void ValidateReadablePath_BlocksProtectedAndExecutableContent(string path, string code)
    {
        var result = new KnowledgeVaultContentPolicy().ValidateReadablePath(path);
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }

    [Theory]
    [InlineData("notes/guide.md")]
    [InlineData("media/diagram.png")]
    [InlineData(".obsidian/app.json")]
    public void ValidateReadablePath_AllowsNormalKnowledgeAndNonExecutableSettings(string path)
    {
        Assert.True(new KnowledgeVaultContentPolicy().ValidateReadablePath(path).IsSuccess);
    }
}