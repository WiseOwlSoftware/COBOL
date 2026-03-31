using Xunit;
using CobolSharp.Runtime.Terminal;

namespace CobolSharp.Tests.Unit.Terminal;

public sealed class CrtStatusMapperTests
{
    [Fact]
    public void Enter_Returns0000()
    {
    }

    [Fact]
    public void Escape_Returns0001()
    {
    }

    [Fact]
    public void Function3_Returns1003()
    {
    }

    [Fact]
    public void ValidationError_Returns9001()
    {
    }
}
