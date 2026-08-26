using PatchPony.Core.Knowledge;

namespace PatchPony.Core.Tests;

public sealed class KnowledgeAreaOwnershipPolicyTests
{
    [Fact]
    public void Resolve_UsesTheMostSpecificAreaWithIndependentPeople()
    {
        var policy = new KnowledgeAreaOwnershipPolicy(
        [
            new KnowledgeAreaOwnershipRule("demo-project", "docs/**", ["documentation-owner"], ["documentation-reviewer"]),
            new KnowledgeAreaOwnershipRule("demo-project", "docs/security/**", ["security-owner"], ["security-reviewer"])
        ]);

        var result = policy.Resolve("demo-project", "docs/security/guide.md");

        Assert.True(result.IsSuccess);
        Assert.Equal("docs/security/**", result.Value!.MatchedPathPattern);
        Assert.Equal(["security-owner"], result.Value.Owners);
        Assert.Equal(["security-reviewer"], result.Value.Reviewers);
    }

    [Fact]
    public void Resolve_FailsClosedForMissingOrAmbiguousRules()
    {
        var policy = new KnowledgeAreaOwnershipPolicy(
        [
            new KnowledgeAreaOwnershipRule("demo-project", "docs/a?c.md", ["alice"], ["bob"]),
            new KnowledgeAreaOwnershipRule("demo-project", "docs/*bc.md", ["carol"], ["dave"])
        ]);

        Assert.Equal("knowledge.ownership.missing", policy.Resolve("demo-project", "notes/guide.md").Error.Code);
        Assert.Equal("knowledge.ownership.ambiguous", policy.Resolve("demo-project", "docs/abc.md").Error.Code);
    }

    [Fact]
    public void Constructor_RejectsOwnerReviewerOverlap()
    {
        Assert.Throws<ArgumentException>(() => new KnowledgeAreaOwnershipPolicy([new KnowledgeAreaOwnershipRule("demo-project", "docs/**", ["alice"], ["ALICE"])]));
    }
}