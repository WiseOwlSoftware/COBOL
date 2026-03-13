using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class PictureParserTests
{
    [Fact]
    public void Parse_SimpleNumeric()
    {
        var pic = PictureParser.Parse("9(5)");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(0, pic.DecimalDigits);
        Assert.Equal(5, pic.Size);
        Assert.False(pic.IsSigned);
    }

    [Fact]
    public void Parse_NumericWithDecimal()
    {
        var pic = PictureParser.Parse("9(3)V99");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(3, pic.IntegerDigits);
        Assert.Equal(2, pic.DecimalDigits);
        Assert.Equal(5, pic.Size);
    }

    [Fact]
    public void Parse_SignedNumeric()
    {
        var pic = PictureParser.Parse("S9(5)V99");
        Assert.True(pic.IsSigned);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(2, pic.DecimalDigits);
    }

    [Fact]
    public void Parse_Alphanumeric()
    {
        var pic = PictureParser.Parse("X(10)");
        Assert.Equal(PictureCategory.Alphanumeric, pic.Category);
        Assert.Equal(10, pic.Size);
    }

    [Fact]
    public void Parse_Alphabetic()
    {
        var pic = PictureParser.Parse("A(5)");
        Assert.Equal(PictureCategory.Alphabetic, pic.Category);
        Assert.Equal(5, pic.Size);
    }

    [Fact]
    public void Parse_RepeatedNines()
    {
        var pic = PictureParser.Parse("99999");
        Assert.Equal(PictureCategory.Numeric, pic.Category);
        Assert.Equal(5, pic.IntegerDigits);
        Assert.Equal(5, pic.Size);
    }

    [Fact]
    public void Parse_SimpleX()
    {
        var pic = PictureParser.Parse("XX");
        Assert.Equal(PictureCategory.Alphanumeric, pic.Category);
        Assert.Equal(2, pic.Size);
    }
}
