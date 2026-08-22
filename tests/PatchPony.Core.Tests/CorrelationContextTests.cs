using PatchPony.Core.Common;

namespace PatchPony.Core.Tests;

public sealed class CorrelationContextTests
{
    [Fact]
    public void Scope_ExposesAndRestoresTheActiveCorrelationIdentifier()
    {
        var context = new CorrelationContext();
        var outer = CorrelationId.Create("outer-42").Value!;
        var inner = CorrelationId.Create("inner-42").Value!;

        using (context.BeginScope(outer))
        {
            Assert.Equal(outer, context.Current);

            using (context.BeginScope(inner))
            {
                Assert.Equal(inner, context.Current);
            }

            Assert.Equal(outer, context.Current);
        }

        Assert.Throws<InvalidOperationException>(() => _ = context.Current);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not valid")]
    public void Create_RejectsInvalidIdentifiers(string value)
    {
        var result = CorrelationId.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid", result.Error.Code);
    }
}
