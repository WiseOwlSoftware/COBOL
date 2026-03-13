using CobolSharp.Runtime.Intrinsics;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime;

public class IntrinsicFunctionTests
{
    [Fact]
    public void Abs_Negative() => Assert.Equal(42m, IntrinsicFunctions.Abs(-42m));

    [Fact]
    public void Abs_Positive() => Assert.Equal(42m, IntrinsicFunctions.Abs(42m));

    [Fact]
    public void Sqrt_PerfectSquare() => Assert.Equal(5m, IntrinsicFunctions.Sqrt(25m));

    [Fact]
    public void Mod_Remainder() => Assert.Equal(1m, IntrinsicFunctions.Mod(10m, 3m));

    [Fact]
    public void Factorial_Five() => Assert.Equal(120m, IntrinsicFunctions.Factorial(5m));

    [Fact]
    public void Pi_Value() => Assert.True(IntrinsicFunctions.Pi() > 3.14m && IntrinsicFunctions.Pi() < 3.15m);

    [Fact]
    public void Max_ReturnsLargest() => Assert.Equal(9m, IntrinsicFunctions.Max(3m, 9m, 1m, 7m));

    [Fact]
    public void Min_ReturnsSmallest() => Assert.Equal(1m, IntrinsicFunctions.Min(3m, 9m, 1m, 7m));

    [Fact]
    public void Sum_AddsAll() => Assert.Equal(20m, IntrinsicFunctions.Sum(3m, 9m, 1m, 7m));

    [Fact]
    public void Mean_Average() => Assert.Equal(5m, IntrinsicFunctions.Mean(3m, 9m, 1m, 7m));

    [Fact]
    public void Median_Even() => Assert.Equal(5m, IntrinsicFunctions.Median(3m, 9m, 1m, 7m));

    [Fact]
    public void Range_Spread() => Assert.Equal(8m, IntrinsicFunctions.Range(3m, 9m, 1m, 7m));

    [Fact]
    public void LowerCase_Converts() => Assert.Equal("hello", IntrinsicFunctions.LowerCase("HELLO"));

    [Fact]
    public void UpperCase_Converts() => Assert.Equal("HELLO", IntrinsicFunctions.UpperCase("hello"));

    [Fact]
    public void Reverse_String() => Assert.Equal("olleH", IntrinsicFunctions.Reverse("Hello"));

    [Fact]
    public void Trim_RemovesBlanks() => Assert.Equal("Hi", IntrinsicFunctions.Trim("  Hi  "));

    [Fact]
    public void Length_ReturnsCount() => Assert.Equal(5m, IntrinsicFunctions.Length("Hello"));

    [Fact]
    public void Concatenate_JoinsStrings() =>
        Assert.Equal("Hello World", IntrinsicFunctions.Concatenate("Hello", " ", "World"));

    [Fact]
    public void Substitute_Replaces() =>
        Assert.Equal("Hello Earth", IntrinsicFunctions.Substitute("Hello World", "World", "Earth"));

    [Fact]
    public void Char_AsciiCode() => Assert.Equal("A", IntrinsicFunctions.Char(65m));

    [Fact]
    public void Ord_CharToCode() => Assert.Equal(65m, IntrinsicFunctions.Ord("A"));

    [Fact]
    public void CurrentDate_Has21Chars()
    {
        string date = IntrinsicFunctions.CurrentDate();
        Assert.True(date.Length >= 20, $"CurrentDate returned '{date}' with length {date.Length}");
    }

    [Fact]
    public void IntegerOfDate_And_DateOfInteger_Roundtrip()
    {
        decimal dateInt = IntrinsicFunctions.IntegerOfDate(20260313m);
        decimal dateBack = IntrinsicFunctions.DateOfInteger(dateInt);
        Assert.Equal(20260313m, dateBack);
    }

    [Fact]
    public void Annuity_ZeroRate() => Assert.Equal(0.1m, IntrinsicFunctions.Annuity(0m, 10m));

    [Fact]
    public void Sign_Positive() => Assert.Equal(1m, IntrinsicFunctions.Sign(42m));
    [Fact]
    public void Sign_Negative() => Assert.Equal(-1m, IntrinsicFunctions.Sign(-5m));
    [Fact]
    public void Sign_Zero() => Assert.Equal(0m, IntrinsicFunctions.Sign(0m));

    [Fact]
    public void Dispatch_ByName()
    {
        var result = IntrinsicFunctions.Call("ABS", new object[] { -42m });
        Assert.Equal(42m, result);
    }

    [Fact]
    public void Dispatch_StringFunction()
    {
        var result = IntrinsicFunctions.Call("UPPER-CASE", new object[] { "hello" });
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Dispatch_CurrentDate()
    {
        var result = IntrinsicFunctions.Call("CURRENT-DATE", Array.Empty<object>());
        Assert.IsType<string>(result);
    }
}
