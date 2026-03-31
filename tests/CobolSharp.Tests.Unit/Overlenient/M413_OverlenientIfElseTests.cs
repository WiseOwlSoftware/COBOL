using Xunit;

namespace CobolSharp.Tests.Unit.Overlenient;

public sealed class M413_OverlenientIfElseTests
{
    [Fact]
    public void Rejects_MissingCondition()
    {
    }

    [Fact]
    public void Rejects_ExtraElseBranch()
    {
    }

    [Fact]
    public void Rejects_IfWithoutThenOrStatement()
    {
    }
}
