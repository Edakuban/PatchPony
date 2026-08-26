using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketCommentTests
{
    [Fact]
    public void Create_RendersBoundedOutcomeAndHttpsLinks()
    {
        var result = TicketComment.Create("Merge Request erstellt.", [new TicketCommentLink("Merge Request", new Uri("https://github.com/Edakuban/PatchPony/pull/42"))]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Merge Request erstellt.\n\nLinks:\n- [Merge Request](https://github.com/Edakuban/PatchPony/pull/42)", result.Value!.RenderMarkdown());
    }

    [Fact]
    public void Create_RejectsUnsafeLinks()
    {
        var result = TicketComment.Create("Ergebnis", [new TicketCommentLink("Local", new Uri("http://localhost:5000/result"))]);

        Assert.False(result.IsSuccess);
        Assert.Equal("ticket.comment.invalid", result.Error.Code);
    }
}