using PatchPony.Core.Knowledge;

namespace PatchPony.Core.Tests;

public sealed class KnowledgeAttachmentPolicyTests
{
    [Theory]
    [InlineData("media/diagram.png")]
    [InlineData("docs/brief.PDF")]
    public void Validate_AcceptsOnlySafeAllowlistedAttachmentMetadata(string path)
    {
        var result = new KnowledgeAttachmentPolicy().Validate(new KnowledgeAttachmentMetadata(path, 1024));
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("script.exe", "knowledge.attachment.extension_disallowed")]
    [InlineData("../image.png", "knowledge.attachment.invalid_path")]
    [InlineData("image.svg", "knowledge.attachment.extension_disallowed")]
    public void Validate_FailsClosedForUnsafeOrDisallowedFiles(string path, string errorCode)
    {
        var result = new KnowledgeAttachmentPolicy().Validate(new KnowledgeAttachmentMetadata(path, 1024));
        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Error.Code);
    }

    [Fact]
    public void ValidateCollection_EnforcesAggregateCountAndByteBudgets()
    {
        var policy = new KnowledgeAttachmentPolicy();
        var tooMany = Enumerable.Range(0, KnowledgeAttachmentPolicy.MaximumAttachmentCount + 1).Select(index => new KnowledgeAttachmentMetadata($"media/{index}.png", 1));
        var tooLarge = Enumerable.Range(0, 9).Select(index => new KnowledgeAttachmentMetadata($"media/{index}.pdf", KnowledgeAttachmentPolicy.MaximumFileBytes));

        Assert.Equal("knowledge.attachment.too_many", policy.ValidateCollection(tooMany).Error.Code);
        Assert.Equal("knowledge.attachment.total_too_large", policy.ValidateCollection(tooLarge).Error.Code);
    }
}