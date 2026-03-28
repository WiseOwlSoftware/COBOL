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
    public void Integer_NegativeValue_ReturnsFloor()
    {
        // INTEGER(-1.5) should return -2 per ISO spec (greatest integer <= argument)
        Assert.Equal(-2m, IntrinsicFunctions.Integer(-1.5m));
    }

    [Fact]
    public void Mod_MixedSign_ReturnsFloorBasedModulo()
    {
        // MOD(-11, 5) = -11 - 5 * floor(-11/5) = -11 - 5*(-3) = -11 + 15 = 4
        Assert.Equal(4m, IntrinsicFunctions.Mod(-11m, 5m));
    }

    [Fact]
    public void Dispatch_CurrentDate()
    {
        var result = IntrinsicFunctions.Call("CURRENT-DATE", Array.Empty<object>());
        Assert.IsType<string>(result);
    }

    // ═══════════════════════════════════════════════════
    // New tests for bug fixes and missing implementations
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Random_ReturnsValueInRange()
    {
        var result = IntrinsicFunctions.Random();
        Assert.True(result >= 0m && result < 1m, $"RANDOM returned {result}, expected [0, 1)");
    }

    [Fact]
    public void Random_WithSeed_IsDeterministic()
    {
        var result1 = IntrinsicFunctions.Random(42m);
        var result2 = IntrinsicFunctions.Random(42m);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void DayToYyyyddd_Converts()
    {
        // 26100 = YY=26, DDD=100 -> with default window (50), YY=26 < 50 -> 2026100
        var result = IntrinsicFunctions.DayToYyyyddd(26100m);
        Assert.Equal(2026100m, result);
    }

    [Fact]
    public void DayToYyyyddd_OldYear()
    {
        // 99001 = YY=99, DDD=001 -> with default window (50), YY=99 >= 50 -> 1999001
        var result = IntrinsicFunctions.DayToYyyyddd(99001m);
        Assert.Equal(1999001m, result);
    }

    [Fact]
    public void SecondsPastMidnight_ReturnsReasonableValue()
    {
        var result = IntrinsicFunctions.SecondsPastMidnight();
        // Should be between 0 and 86400 (seconds in a day)
        Assert.True(result >= 0m && result < 86400m,
            $"SECONDS-PAST-MIDNIGHT returned {result}, expected [0, 86400)");
    }

    [Fact]
    public void E_ReturnsEulersNumber()
    {
        var result = IntrinsicFunctions.E();
        Assert.True(result > 2.718m && result < 2.719m, $"E returned {result}");
    }

    [Fact]
    public void DateToYyyymmdd_OneArg_UsesDefaults()
    {
        // 260313 = YY=26, MMDD=0313 -> with default window (50), YY=26 < 50 -> 20260313
        var result = IntrinsicFunctions.DateToYyyymmdd(260313m);
        Assert.Equal(20260313m, result);
    }

    [Fact]
    public void DateToYyyymmdd_OneArg_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("DATE-TO-YYYYMMDD", new object[] { 260313m });
        Assert.Equal(20260313m, result);
    }

    [Fact]
    public void YearToYyyy_OneArg_UsesDefaults()
    {
        // YY=26, default window=50 -> 26 < 50 -> 2026
        var result = IntrinsicFunctions.YearToYyyy(26m);
        Assert.Equal(2026m, result);
    }

    [Fact]
    public void YearToYyyy_OneArg_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("YEAR-TO-YYYY", new object[] { 26m });
        Assert.Equal(2026m, result);
    }

    [Fact]
    public void Numval_ParsesSignedDecimal()
    {
        Assert.Equal(-123.45m, IntrinsicFunctions.NumericValue(" -123.45 "));
    }

    [Fact]
    public void Numval_ParsesCrSuffix()
    {
        Assert.Equal(-100m, IntrinsicFunctions.NumericValue("100CR"));
    }

    [Fact]
    public void Numval_ParsesDbSuffix()
    {
        Assert.Equal(-200m, IntrinsicFunctions.NumericValue("200DB"));
    }

    [Fact]
    public void Numval_ParsesTrailingSign()
    {
        Assert.Equal(-50m, IntrinsicFunctions.NumericValue("50-"));
    }

    [Fact]
    public void NumvalC_ParsesCurrencyWithGrouping()
    {
        Assert.Equal(-1234.56m, IntrinsicFunctions.NumericValueC("$1,234.56CR"));
    }

    [Fact]
    public void NumvalC_DefaultCurrency()
    {
        Assert.Equal(99.99m, IntrinsicFunctions.NumericValueC("$99.99"));
    }

    [Fact]
    public void NumvalC_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("NUMVAL-C", new object[] { "$1,234.56CR", "$" });
        Assert.Equal(-1234.56m, result);
    }

    [Fact]
    public void MaxString_ReturnsLexMax()
    {
        Assert.Equal("cherry", IntrinsicFunctions.MaxString("apple", "cherry", "banana"));
    }

    [Fact]
    public void MinString_ReturnsLexMin()
    {
        Assert.Equal("apple", IntrinsicFunctions.MinString("cherry", "apple", "banana"));
    }

    [Fact]
    public void MaxString_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("MAX", new object[] { "apple", "cherry", "banana" });
        Assert.Equal("cherry", result);
    }

    [Fact]
    public void MinString_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("MIN", new object[] { "cherry", "apple", "banana" });
        Assert.Equal("apple", result);
    }

    [Fact]
    public void OrdMaxString_ReturnsPosition()
    {
        // "cherry" is at index 1 (1-based: position 2)
        Assert.Equal(2m, IntrinsicFunctions.OrdMaxString("apple", "cherry", "banana"));
    }

    [Fact]
    public void OrdMinString_ReturnsPosition()
    {
        // "apple" is at index 1 (1-based: position 2)
        Assert.Equal(2m, IntrinsicFunctions.OrdMinString("cherry", "apple", "banana"));
    }

    [Fact]
    public void Trim_Leading_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TRIM", new object[] { "  Hello  ", "LEADING" });
        Assert.Equal("Hello  ", result);
    }

    [Fact]
    public void Trim_Trailing_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TRIM", new object[] { "  Hello  ", "TRAILING" });
        Assert.Equal("  Hello", result);
    }

    [Fact]
    public void Trim_Default_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TRIM", new object[] { "  Hello  " });
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Substitute_MultiplePairs()
    {
        var result = IntrinsicFunctions.Substitute("Hello World Foo", "World", "Earth", "Foo", "Bar");
        Assert.Equal("Hello Earth Bar", result);
    }

    [Fact]
    public void Substitute_MultiplePairs_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("SUBSTITUTE",
            new object[] { "Hello World Foo", "World", "Earth", "Foo", "Bar" });
        Assert.Equal("Hello Earth Bar", result);
    }

    [Fact]
    public void Concat_IsAliasForConcatenate()
    {
        var result = IntrinsicFunctions.Call("CONCAT", new object[] { "Hello", " ", "World" });
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void WhenCompiled_Returns21Chars()
    {
        string result = IntrinsicFunctions.WhenCompiled();
        Assert.Equal(21, result.Length);
    }

    [Fact]
    public void Random_ViaDispatch_NoArgs()
    {
        var result = IntrinsicFunctions.Call("RANDOM", Array.Empty<object>());
        Assert.IsType<decimal>(result);
        var val = (decimal)result;
        Assert.True(val >= 0m && val < 1m);
    }

    [Fact]
    public void Random_ViaDispatch_WithSeed()
    {
        var result1 = IntrinsicFunctions.Call("RANDOM", new object[] { 42m });
        var result2 = IntrinsicFunctions.Call("RANDOM", new object[] { 42m });
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void DayToYyyyddd_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("DAY-TO-YYYYDDD", new object[] { 26100m });
        Assert.Equal(2026100m, result);
    }

    [Fact]
    public void E_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("E", Array.Empty<object>());
        Assert.IsType<decimal>(result);
        Assert.True((decimal)result > 2.718m);
    }

    [Fact]
    public void SecondsPastMidnight_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("SECONDS-PAST-MIDNIGHT", Array.Empty<object>());
        Assert.IsType<decimal>(result);
        var val = (decimal)result;
        Assert.True(val >= 0m && val < 86400m);
    }

    // ═══════════════════════════════════════════════════
    // Trig functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Acos_One_ReturnsZero() => Assert.Equal(0m, IntrinsicFunctions.Acos(1m));

    [Fact]
    public void Asin_Zero_ReturnsZero() => Assert.Equal(0m, IntrinsicFunctions.Asin(0m));

    [Fact]
    public void Atan_Zero_ReturnsZero() => Assert.Equal(0m, IntrinsicFunctions.Atan(0m));

    [Fact]
    public void Cos_Zero_ReturnsOne() => Assert.Equal(1m, IntrinsicFunctions.Cos(0m));

    [Fact]
    public void Sin_Zero_ReturnsZero() => Assert.Equal(0m, IntrinsicFunctions.Sin(0m));

    [Fact]
    public void Tan_Zero_ReturnsZero() => Assert.Equal(0m, IntrinsicFunctions.Tan(0m));

    [Fact]
    public void Log_E_ReturnsOne()
    {
        var result = IntrinsicFunctions.Log(IntrinsicFunctions.E());
        Assert.True(result > 0.9999m && result < 1.0001m, $"LOG(E) = {result}");
    }

    [Fact]
    public void Log10_100_ReturnsTwo()
    {
        var result = IntrinsicFunctions.Log10(100m);
        Assert.True(result > 1.9999m && result < 2.0001m, $"LOG10(100) = {result}");
    }

    [Fact]
    public void Exp_Zero_ReturnsOne()
    {
        var result = IntrinsicFunctions.Exp(0m);
        Assert.Equal(1m, result);
    }

    [Fact]
    public void Exp10_Two_Returns100()
    {
        var result = IntrinsicFunctions.Exp10(2m);
        Assert.True(result > 99.9m && result < 100.1m, $"EXP10(2) = {result}");
    }

    [Fact]
    public void Acos_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("ACOS", new object[] { 1m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Asin_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("ASIN", new object[] { 0m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Atan_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("ATAN", new object[] { 0m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Cos_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("COS", new object[] { 0m });
        Assert.Equal(1m, result);
    }

    [Fact]
    public void Sin_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("SIN", new object[] { 0m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Tan_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TAN", new object[] { 0m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Log_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("LOG", new object[] { IntrinsicFunctions.E() });
        Assert.True(result > 0.9999m && result < 1.0001m);
    }

    [Fact]
    public void Log10_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("LOG10", new object[] { 100m });
        Assert.True(result > 1.9999m && result < 2.0001m);
    }

    [Fact]
    public void Exp_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("EXP", new object[] { 0m });
        Assert.Equal(1m, result);
    }

    [Fact]
    public void Exp10_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("EXP10", new object[] { 2m });
        Assert.True(result > 99.9m && result < 100.1m);
    }

    // ═══════════════════════════════════════════════════
    // Numeric math functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Rem_TruncatesZero() => Assert.Equal(1m, IntrinsicFunctions.Rem(10m, 3m));

    [Fact]
    public void Rem_NegativeIsTruncation()
    {
        // REM(-11, 5) = -11 - 5 * truncate(-11/5) = -11 - 5*(-2) = -11 + 10 = -1
        Assert.Equal(-1m, IntrinsicFunctions.Rem(-11m, 5m));
    }

    [Fact]
    public void Rem_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("REM", new object[] { 10m, 3m });
        Assert.Equal(1m, result);
    }

    [Fact]
    public void IntegerPart_PositiveFraction() => Assert.Equal(3m, IntrinsicFunctions.IntegerPart(3.7m));

    [Fact]
    public void IntegerPart_NegativeFraction() => Assert.Equal(-3m, IntrinsicFunctions.IntegerPart(-3.7m));

    [Fact]
    public void IntegerPart_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("INTEGER-PART", new object[] { 3.7m });
        Assert.Equal(3m, result);
    }

    [Fact]
    public void FractionPart_Positive()
    {
        var result = IntrinsicFunctions.FractionPart(3.7m);
        Assert.True(result > 0.69m && result < 0.71m, $"FRACTION-PART(3.7) = {result}");
    }

    [Fact]
    public void FractionPart_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("FRACTION-PART", new object[] { 3.7m });
        Assert.True(result > 0.69m && result < 0.71m);
    }

    // ═══════════════════════════════════════════════════
    // Aggregate functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Midrange_ReturnsAvgOfMinMax() => Assert.Equal(5m, IntrinsicFunctions.Midrange(1m, 3m, 9m, 7m));

    [Fact]
    public void Midrange_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("MIDRANGE", new object[] { 1m, 3m, 9m, 7m });
        Assert.Equal(5m, result);
    }

    [Fact]
    public void Variance_Simple()
    {
        // Mean=2, deviations: 1,0,1 -> variance=2/3
        var result = IntrinsicFunctions.Variance(1m, 2m, 3m);
        Assert.True(result > 0.666m && result < 0.668m, $"VARIANCE(1,2,3) = {result}");
    }

    [Fact]
    public void Variance_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("VARIANCE", new object[] { 1m, 2m, 3m });
        Assert.True(result > 0.666m && result < 0.668m);
    }

    [Fact]
    public void StandardDeviation_Simple()
    {
        var result = IntrinsicFunctions.StandardDeviation(1m, 2m, 3m);
        Assert.True(result > 0.81m && result < 0.82m, $"STANDARD-DEVIATION(1,2,3) = {result}");
    }

    [Fact]
    public void StandardDeviation_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("STANDARD-DEVIATION", new object[] { 1m, 2m, 3m });
        Assert.True(result > 0.81m && result < 0.82m);
    }

    [Fact]
    public void OrdMax_Numeric_ReturnsPosition()
    {
        // Max is 9 at index 1 (1-based position 2)
        Assert.Equal(2m, IntrinsicFunctions.OrdMax(3m, 9m, 1m, 7m));
    }

    [Fact]
    public void OrdMax_Numeric_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("ORD-MAX", new object[] { 3m, 9m, 1m, 7m });
        Assert.Equal(2m, result);
    }

    [Fact]
    public void OrdMin_Numeric_ReturnsPosition()
    {
        // Min is 1 at index 2 (1-based position 3)
        Assert.Equal(3m, IntrinsicFunctions.OrdMin(3m, 9m, 1m, 7m));
    }

    [Fact]
    public void OrdMin_Numeric_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("ORD-MIN", new object[] { 3m, 9m, 1m, 7m });
        Assert.Equal(3m, result);
    }

    // ═══════════════════════════════════════════════════
    // String functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void ByteLength_AsciiString() => Assert.Equal(5m, IntrinsicFunctions.ByteLength("Hello"));

    [Fact]
    public void ByteLength_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("BYTE-LENGTH", new object[] { "Hello" });
        Assert.Equal(5m, result);
    }

    // ═══════════════════════════════════════════════════
    // Date/Time functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void DayOfInteger_Roundtrip()
    {
        // INTEGER-OF-DATE(20260313) -> integer, then DAY-OF-INTEGER -> YYYYDDD
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        decimal dayResult = IntrinsicFunctions.DayOfInteger(intDate);
        // 2026, day 72 (March 13 is day 72)
        Assert.Equal(2026072m, dayResult);
    }

    [Fact]
    public void DayOfInteger_ViaDispatch()
    {
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        var result = IntrinsicFunctions.Call("DAY-OF-INTEGER", new object[] { intDate });
        Assert.Equal(2026072m, result);
    }

    [Fact]
    public void IntegerOfDay_Roundtrip()
    {
        decimal intDay = IntrinsicFunctions.IntegerOfDay(2026072m);
        decimal dayBack = IntrinsicFunctions.DayOfInteger(intDay);
        Assert.Equal(2026072m, dayBack);
    }

    [Fact]
    public void IntegerOfDay_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("INTEGER-OF-DAY", new object[] { 2026072m });
        Assert.True(result > 0m);
    }

    // ═══════════════════════════════════════════════════
    // Financial functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void PresentValue_SingleAmount()
    {
        // PV = 110 / (1 + 0.1) = 100
        var result = IntrinsicFunctions.PresentValue(0.1m, 110m);
        Assert.True(result > 99.9m && result < 100.1m, $"PRESENT-VALUE = {result}");
    }

    [Fact]
    public void PresentValue_ViaDispatch()
    {
        var result = (decimal)IntrinsicFunctions.Call("PRESENT-VALUE", new object[] { 0.1m, 110m });
        Assert.True(result > 99.9m && result < 100.1m);
    }

    // ═══════════════════════════════════════════════════
    // COBOL-2002+ functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void FindString_Found()
    {
        Assert.Equal(7m, IntrinsicFunctions.FindString("Hello World", "World"));
    }

    [Fact]
    public void FindString_NotFound()
    {
        Assert.Equal(0m, IntrinsicFunctions.FindString("Hello World", "xyz"));
    }

    [Fact]
    public void FindString_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("FIND-STRING", new object[] { "Hello World", "World" });
        Assert.Equal(7m, result);
    }

    [Fact]
    public void TestDateYyyymmdd_ValidDate()
    {
        Assert.Equal(0m, IntrinsicFunctions.TestDateYyyymmdd(20260313m));
    }

    [Fact]
    public void TestDateYyyymmdd_InvalidDate()
    {
        Assert.Equal(1m, IntrinsicFunctions.TestDateYyyymmdd(20261399m));
    }

    [Fact]
    public void TestDateYyyymmdd_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TEST-DATE-YYYYMMDD", new object[] { 20260313m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TestDayYyyyddd_ValidDay()
    {
        Assert.Equal(0m, IntrinsicFunctions.TestDayYyyyddd(2026072m));
    }

    [Fact]
    public void TestDayYyyyddd_InvalidDay()
    {
        Assert.Equal(1m, IntrinsicFunctions.TestDayYyyyddd(2026400m));
    }

    [Fact]
    public void TestDayYyyyddd_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TEST-DAY-YYYYDDD", new object[] { 2026072m });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TestNumval_ValidNumber()
    {
        Assert.Equal(0m, IntrinsicFunctions.TestNumval("  123.45  "));
    }

    [Fact]
    public void TestNumval_InvalidNumber()
    {
        Assert.Equal(1m, IntrinsicFunctions.TestNumval("abc"));
    }

    [Fact]
    public void TestNumval_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TEST-NUMVAL", new object[] { "123.45" });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TestNumvalC_ValidCurrency()
    {
        Assert.Equal(0m, IntrinsicFunctions.TestNumvalC("$1,234.56"));
    }

    [Fact]
    public void TestNumvalC_InvalidCurrency()
    {
        Assert.Equal(1m, IntrinsicFunctions.TestNumvalC("not-a-number"));
    }

    [Fact]
    public void TestNumvalC_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("TEST-NUMVAL-C", new object[] { "$99.99" });
        Assert.Equal(0m, result);
    }

    [Fact]
    public void NumvalF_ParsesFloat()
    {
        Assert.Equal(1.5m, IntrinsicFunctions.NumvalF("1.5E0"));
    }

    [Fact]
    public void NumvalF_ParsesNegativeExponent()
    {
        var result = IntrinsicFunctions.NumvalF("1.5E-1");
        Assert.True(result > 0.149m && result < 0.151m, $"NUMVAL-F(1.5E-1) = {result}");
    }

    [Fact]
    public void NumvalF_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("NUMVAL-F", new object[] { "1.5E0" });
        Assert.Equal(1.5m, result);
    }

    [Fact]
    public void CombinedDatetime_CombinesValues()
    {
        // integerDate * 1000000 + time
        Assert.Equal(1000001000000m + 120000m, IntrinsicFunctions.CombinedDatetime(1000001m, 120000m));
    }

    [Fact]
    public void CombinedDatetime_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("COMBINED-DATETIME", new object[] { 1000001m, 120000m });
        Assert.Equal(1000001000000m + 120000m, result);
    }

    [Fact]
    public void BooleanOfInteger_ConvertsToBase2()
    {
        Assert.Equal("00001010", IntrinsicFunctions.BooleanOfInteger(10m, 8m));
    }

    [Fact]
    public void BooleanOfInteger_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("BOOLEAN-OF-INTEGER", new object[] { 10m, 8m });
        Assert.Equal("00001010", result);
    }

    [Fact]
    public void IntegerOfBoolean_ParsesBinaryString()
    {
        Assert.Equal(10m, IntrinsicFunctions.IntegerOfBoolean("1010"));
    }

    [Fact]
    public void IntegerOfBoolean_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("INTEGER-OF-BOOLEAN", new object[] { "1010" });
        Assert.Equal(10m, result);
    }

    [Fact]
    public void FormattedCurrentDate_ReturnsIsoString()
    {
        string result = IntrinsicFunctions.FormattedCurrentDate("");
        Assert.True(result.Length > 10, $"FORMATTED-CURRENT-DATE returned '{result}'");
    }

    [Fact]
    public void FormattedCurrentDate_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("FORMATTED-CURRENT-DATE", Array.Empty<object>());
        Assert.IsType<string>(result);
    }

    [Fact]
    public void FormattedDate_ReturnsDateString()
    {
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        string result = IntrinsicFunctions.FormattedDate("", intDate);
        Assert.Equal("2026-03-13", result);
    }

    [Fact]
    public void FormattedDate_ViaDispatch()
    {
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        var result = IntrinsicFunctions.Call("FORMATTED-DATE", new object[] { intDate });
        Assert.Equal("2026-03-13", result);
    }

    [Fact]
    public void FormattedTime_ReturnsTimeString()
    {
        string result = IntrinsicFunctions.FormattedTime("", 143045m);
        Assert.Equal("14:30:45", result);
    }

    [Fact]
    public void FormattedTime_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("FORMATTED-TIME", new object[] { 143045m });
        Assert.Equal("14:30:45", result);
    }

    [Fact]
    public void HighestAlgebraic_TwoDigits()
    {
        Assert.Equal(99m, IntrinsicFunctions.HighestAlgebraic(2m));
    }

    [Fact]
    public void HighestAlgebraic_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("HIGHEST-ALGEBRAIC", new object[] { 2m });
        Assert.Equal(99m, result);
    }

    [Fact]
    public void LowestAlgebraic_TwoDigits()
    {
        Assert.Equal(-99m, IntrinsicFunctions.LowestAlgebraic(2m));
    }

    [Fact]
    public void LowestAlgebraic_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("LOWEST-ALGEBRAIC", new object[] { 2m });
        Assert.Equal(-99m, result);
    }

    [Fact]
    public void ModuleName_ReturnsString()
    {
        string result = IntrinsicFunctions.ModuleName();
        Assert.NotNull(result);
    }

    [Fact]
    public void ModuleName_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("MODULE-NAME", Array.Empty<object>());
        Assert.IsType<string>(result);
    }

    // ═══════════════════════════════════════════════════
    // LOCALE-COMPARE (§15.55)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void LocaleCompare_LessThan()
    {
        Assert.Equal(-1m, IntrinsicFunctions.LocaleCompare("A", "B"));
    }

    [Fact]
    public void LocaleCompare_GreaterThan()
    {
        Assert.Equal(1m, IntrinsicFunctions.LocaleCompare("B", "A"));
    }

    [Fact]
    public void LocaleCompare_Equal()
    {
        Assert.Equal(0m, IntrinsicFunctions.LocaleCompare("ABC", "ABC"));
    }

    [Fact]
    public void LocaleCompare_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("LOCALE-COMPARE", new object[] { "A", "B" });
        Assert.Equal(-1m, result);
    }

    // ═══════════════════════════════════════════════════
    // LOCALE-DATE (§15.56)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void LocaleDate_ReturnsNonEmptyString()
    {
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        string result = IntrinsicFunctions.LocaleDate(intDate);
        // Locale-specific format, so just check it contains the date components
        Assert.NotNull(result);
        Assert.True(result.Length > 0, "LOCALE-DATE returned empty string");
        Assert.Contains("2026", result);
    }

    [Fact]
    public void LocaleDate_ViaDispatch()
    {
        decimal intDate = IntrinsicFunctions.IntegerOfDate(20260313m);
        var result = IntrinsicFunctions.Call("LOCALE-DATE", new object[] { intDate });
        Assert.IsType<string>(result);
        Assert.Contains("2026", (string)result);
    }

    // ═══════════════════════════════════════════════════
    // LOCALE-TIME (§15.57)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void LocaleTime_ReturnsNonEmptyString()
    {
        string result = IntrinsicFunctions.LocaleTime(143045m);
        Assert.NotNull(result);
        Assert.True(result.Length > 0, "LOCALE-TIME returned empty string");
        // Locale-dependent: may be "14:30:45" or "2:30:45 PM" etc.
        Assert.Contains("30", result);
        Assert.Contains("45", result);
    }

    [Fact]
    public void LocaleTime_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("LOCALE-TIME", new object[] { 143045m });
        Assert.IsType<string>(result);
        Assert.Contains("30", (string)result);
    }

    // ═══════════════════════════════════════════════════
    // LOCALE-TIME-FROM-SECONDS (§15.58)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void LocaleTimeFromSeconds_ReturnsNonEmptyString()
    {
        // 52245 seconds = 14 hours, 30 minutes, 45 seconds
        string result = IntrinsicFunctions.LocaleTimeFromSeconds(52245m);
        Assert.NotNull(result);
        Assert.True(result.Length > 0, "LOCALE-TIME-FROM-SECONDS returned empty string");
        // Locale-dependent: may be "14:30:45" or "2:30:45 PM" etc.
        Assert.Contains("30", result);
        Assert.Contains("45", result);
    }

    [Fact]
    public void LocaleTimeFromSeconds_Midnight()
    {
        string result = IntrinsicFunctions.LocaleTimeFromSeconds(0m);
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void LocaleTimeFromSeconds_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("LOCALE-TIME-FROM-SECONDS", new object[] { 52245m });
        Assert.IsType<string>(result);
        Assert.Contains("30", (string)result);
    }

    // ═══════════════════════════════════════════════════
    // STANDARD-COMPARE (§15.87)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void StandardCompare_LessThan()
    {
        Assert.Equal(-1m, IntrinsicFunctions.StandardCompare("A", "B"));
    }

    [Fact]
    public void StandardCompare_GreaterThan()
    {
        Assert.Equal(1m, IntrinsicFunctions.StandardCompare("B", "A"));
    }

    [Fact]
    public void StandardCompare_Equal()
    {
        Assert.Equal(0m, IntrinsicFunctions.StandardCompare("ABC", "ABC"));
    }

    [Fact]
    public void StandardCompare_CaseSensitive()
    {
        // Ordinal: 'A' (65) < 'a' (97)
        Assert.Equal(-1m, IntrinsicFunctions.StandardCompare("A", "a"));
    }

    [Fact]
    public void StandardCompare_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("STANDARD-COMPARE", new object[] { "A", "B" });
        Assert.Equal(-1m, result);
    }

    // ═══════════════════════════════════════════════════
    // DISPLAY-OF (§15.28) — approximation, returns string as-is
    // ═══════════════════════════════════════════════════

    [Fact]
    public void DisplayOf_ReturnsInput()
    {
        Assert.Equal("Hello", IntrinsicFunctions.DisplayOf("Hello"));
    }

    [Fact]
    public void DisplayOf_EmptyString()
    {
        Assert.Equal("", IntrinsicFunctions.DisplayOf(""));
    }

    [Fact]
    public void DisplayOf_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("DISPLAY-OF", new object[] { "Hello" });
        Assert.Equal("Hello", result);
    }

    // ═══════════════════════════════════════════════════
    // NATIONAL-OF (§15.66) — approximation, returns string as-is
    // ═══════════════════════════════════════════════════

    [Fact]
    public void NationalOf_ReturnsInput()
    {
        Assert.Equal("Hello", IntrinsicFunctions.NationalOf("Hello"));
    }

    [Fact]
    public void NationalOf_EmptyString()
    {
        Assert.Equal("", IntrinsicFunctions.NationalOf(""));
    }

    [Fact]
    public void NationalOf_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("NATIONAL-OF", new object[] { "Hello" });
        Assert.Equal("Hello", result);
    }

    // ═══════════════════════════════════════════════════
    // CHAR-NATIONAL (§15.16)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void CharNational_ReturnsChar()
    {
        Assert.Equal("A", IntrinsicFunctions.CharNational(65m));
    }

    [Fact]
    public void CharNational_Space()
    {
        Assert.Equal(" ", IntrinsicFunctions.CharNational(32m));
    }

    [Fact]
    public void CharNational_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("CHAR-NATIONAL", new object[] { 65m });
        Assert.Equal("A", result);
    }

    // ═══════════════════════════════════════════════════
    // CONVERT (§15.19) — encoding conversion
    // ═══════════════════════════════════════════════════

    [Fact]
    public void ConvertEncoding_AsciiToUtf8_PreservesAscii()
    {
        Assert.Equal("Hello", IntrinsicFunctions.ConvertEncoding("Hello", "ASCII", "UTF-8"));
    }

    [Fact]
    public void ConvertEncoding_UnknownEncoding_ReturnsInput()
    {
        Assert.Equal("Hello", IntrinsicFunctions.ConvertEncoding("Hello", "NONEXISTENT", "ALSO-FAKE"));
    }

    [Fact]
    public void ConvertEncoding_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("CONVERT", new object[] { "Hello", "ASCII", "UTF-8" });
        Assert.Equal("Hello", result);
    }

    // ═══════════════════════════════════════════════════
    // BASECONVERT (§15.10)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Baseconvert_HexToDecimal()
    {
        Assert.Equal("255", IntrinsicFunctions.Baseconvert("FF", 16m, 10m));
    }

    [Fact]
    public void Baseconvert_DecimalToHex()
    {
        Assert.Equal("FF", IntrinsicFunctions.Baseconvert("255", 10m, 16m));
    }

    [Fact]
    public void Baseconvert_DecimalToBinary()
    {
        Assert.Equal("1010", IntrinsicFunctions.Baseconvert("10", 10m, 2m));
    }

    [Fact]
    public void Baseconvert_BinaryToDecimal()
    {
        Assert.Equal("10", IntrinsicFunctions.Baseconvert("1010", 2m, 10m));
    }

    [Fact]
    public void Baseconvert_OctalToDecimal()
    {
        Assert.Equal("8", IntrinsicFunctions.Baseconvert("10", 8m, 10m));
    }

    [Fact]
    public void Baseconvert_DecimalToOctal()
    {
        Assert.Equal("10", IntrinsicFunctions.Baseconvert("8", 10m, 8m));
    }

    [Fact]
    public void Baseconvert_Zero()
    {
        Assert.Equal("0", IntrinsicFunctions.Baseconvert("0", 10m, 16m));
    }

    [Fact]
    public void Baseconvert_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("BASECONVERT", new object[] { "FF", 16m, 10m });
        Assert.Equal("255", result);
    }

    // ═══════════════════════════════════════════════════
    // EXCEPTION-* functions — no exception returns empty string
    // ═══════════════════════════════════════════════════

    [Fact]
    public void ExceptionFile_ReturnsEmpty()
    {
        Assert.Equal("", IntrinsicFunctions.ExceptionFile());
    }

    [Fact]
    public void ExceptionFile_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("EXCEPTION-FILE", Array.Empty<object>());
        Assert.Equal("", result);
    }

    [Fact]
    public void ExceptionLocation_ReturnsEmpty()
    {
        Assert.Equal("", IntrinsicFunctions.ExceptionLocation());
    }

    [Fact]
    public void ExceptionLocation_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("EXCEPTION-LOCATION", Array.Empty<object>());
        Assert.Equal("", result);
    }

    [Fact]
    public void ExceptionStatement_ReturnsEmpty()
    {
        Assert.Equal("", IntrinsicFunctions.ExceptionStatement());
    }

    [Fact]
    public void ExceptionStatement_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("EXCEPTION-STATEMENT", Array.Empty<object>());
        Assert.Equal("", result);
    }

    [Fact]
    public void ExceptionStatus_ReturnsEmpty()
    {
        Assert.Equal("", IntrinsicFunctions.ExceptionStatus());
    }

    [Fact]
    public void ExceptionStatus_ViaDispatch()
    {
        var result = IntrinsicFunctions.Call("EXCEPTION-STATUS", Array.Empty<object>());
        Assert.Equal("", result);
    }
}
