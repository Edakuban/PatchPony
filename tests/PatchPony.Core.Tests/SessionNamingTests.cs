using PatchPony.Core.Sessions;

namespace PatchPony.Core.Tests;

public sealed class SessionNamingTests
{
    [Fact]
    public void Naming_DerivesOnlyStableServerOwnedNamesFromTheSessionIdentifier()
    {
        var id = new SessionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));

        var names = SessionNaming.For(id);

        Assert.Equal("0123456789abcdef0123456789abcdef", names.DirectoryName);
        Assert.Equal("patchpony/session/0123456789abcdef0123456789abcdef", names.BranchName);
        Assert.DoesNotContain("..", names.DirectoryName, StringComparison.Ordinal);
        Assert.DoesNotContain("..", names.BranchName, StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_RejectsAnEmptySessionIdentifier()
    {
        Assert.Throws<ArgumentException>(() => SessionNaming.For(new SessionId(Guid.Empty)));
    }
}