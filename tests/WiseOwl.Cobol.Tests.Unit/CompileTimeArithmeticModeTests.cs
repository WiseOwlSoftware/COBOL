// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Expressions;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB1592, PB5575 — the compile-time arithmetic MODE is the edition's: 2002 PRESCRIBES standard arithmetic
/// (§2002:7.2.5.2 GR1), a 32-digit standard intermediate data item whose every result is TRUNCATED to 32 significant
/// digits (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5) in the range 10^-999 through 10^1000 - 10^968
/// (§2002:8.8.1.3.1.1); 2014 says "as if ARITHMETIC IS STANDARD-DECIMAL were in effect at compile time"
/// (§2014:7.3.5.2 GR1), the SDIDI decimal engine (34 digits, decimal128 range, §2023:8.8.1.5.2) with the standard-decimal
/// default intermediate rounding NEAREST-AWAY-FROM-ZERO (§2023:11.9.11.2 GR3 a); from 2023 the mode is the documented
/// System.Decimal one (§2023:7.3.6.3 GR2; docs/CONFORMANCE.md DOC-A.1-29). Every expected value below is derived from
/// those rules by hand, then §2023:7.3.6.3 GR3 (truncate the final result to its integer part), never read off the
/// compiler.
/// </summary>
public sealed class CompileTimeArithmeticModeTests
{
    private static (CompileTimeExpressionEvaluator.CtNumber? Result, CompileTimeExpressionEvaluatorTests.CollectingDiag Diag)
        Eval(string text, int edition) => CompileTimeExpressionEvaluatorTests.Eval(text, edition: edition);

    /// <summary>The ONE selection, read through the ONE per-edition behaviour register.</summary>
    [Theory]
    [InlineData(2002, false, false)]
    [InlineData(2014, false, true)]
    [InlineData(2023, true, true)]
    public void Selection_IsTheEditionsMode(int edition, bool implementorDefined, bool standardDecimal)
    {
        Assert.Equal(implementorDefined,
            DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticImplementorDefined, edition));
        Assert.Equal(standardDecimal,
            DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticIsStandardDecimal, edition));
        Assert.Same(implementorDefined ? CompileTimeArithmetic.SystemDecimal
                : standardDecimal ? CompileTimeArithmetic.StandardDecimal : CompileTimeArithmetic.Standard2002,
            CompileTimeArithmetic.For(EditionInfo.Of(edition)));
    }

    /// <summary>Expressions whose value DIFFERS between the two modes, each derived from its mode's rule:
    /// <list type="bullet">
    /// <item><c>9999999999999999999999999999 * 10</c> — the exact product 99999999999999999999999999990 has 29
    /// digits: within the SDIDI's 34 (2002/2014), beyond System.Decimal's 7.9E28 (2023: refused, SR2).</item>
    /// <item><c>0.0000000000000000000000000015 / 2 * 10000000000000000000000000000</c> — 1.5E-27 / 2 = 7.5E-28 is
    /// exact in the SDIDI, ×1E28 = 7.5, GR3 → 7; System.Decimal holds 28 places, so 7.5E-28 is a tie that rounds
    /// to the even digit, 8E-28, ×1E28 = 8.</item>
    /// </list></summary>
    [Theory]
    [InlineData("9999999999999999999999999999 * 10", 2002, "99999999999999999999999999990")]
    [InlineData("9999999999999999999999999999 * 10", 2014, "99999999999999999999999999990")]
    [InlineData("9999999999999999999999999999 * 10", 2023, null)]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2002, "7")]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2014, "7")]
    [InlineData("0.0000000000000000000000000015 / 2 * 10000000000000000000000000000", 2023, "8")]
    public void ModeDependentValues_FollowTheEditionsMode(string expr, int edition, string? expected)
    {
        var (r, diag) = Eval(expr, edition);
        if (expected is null)
        {
            Assert.Null(r);
            Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                               && x.Message.Contains(".NET decimal evaluation range", StringComparison.Ordinal)
                                               && x.Message.Contains("SR2", StringComparison.Ordinal));
            return;
        }
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
        Assert.False(r.Value.WasSingleLiteral);
    }

    /// <summary>Expressions every mode agrees on. 1/3 = 0.333…3 (32 digits truncated; 34 rounded) x3 = 0.999…9 → 0;
    /// -7/2 = -3.5 exactly → -3.</summary>
    [Theory]
    [InlineData("1 / 3 * 3", "0")]
    [InlineData("-7 / 2", "-3")]
    public void ModeIndependentValues_AgreeAtEveryEdition(string expr, string expected)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (r, diag) = Eval(expr, edition);
            Assert.Empty(diag.Reports);
            Assert.Equal(expected, r!.Value.Literal);
        }
    }

    /// <summary>Division TRUNCATES to 32 digits at 2002 (§2002:8.8.1.3.5) and ROUNDS to 34 from 2014. 2/3 = 0.66…6 (32 sixes)
    /// x3 = 1.99…98 (33 digits) → 1.99…9 (32 digits) → 1 at 2002; 0.66…67 (34 digits) x3 = 2.000…01 → 2.000…0 → 2 at 2014 and
    /// 2023. 100/7 = 14.28…14 (32 digits) x7 = 99.99…98 → 99 at 2002; 14.28…1429 x7 = 100.000…03 → 100 later.</summary>
    [Theory]
    [InlineData("2 / 3 * 3", 2002, "1")]
    [InlineData("2 / 3 * 3", 2014, "2")]
    [InlineData("2 / 3 * 3", 2023, "2")]
    [InlineData("100 / 7 * 7", 2002, "99")]
    [InlineData("100 / 7 * 7", 2014, "100")]
    [InlineData("100 / 7 * 7", 2023, "100")]
    public void Division_TruncatesAt2002AndRoundsAfter(string expr, int edition, string expected)
    {
        var (r, diag) = Eval(expr, edition);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
    }

    /// <summary>§2002:8.8.1.3.2 / §2002:8.8.1.3.4: each result is cut to 32 digits, so a value one digit past them is lost.
    /// 10^30 x 100 + 9 = 10^32 + 9 has 33 digits, truncated to 10^32; less 10^32 is 0 at 2002 where the
    /// 34-digit engines keep the 9 (the note's repro, kb/Work PB5575).</summary>
    [Theory]
    [InlineData(2002, "0")]
    [InlineData(2014, "9")]
    public void SumPastThirtyTwoDigits_IsTruncatedAt2002(int edition, string expected)
    {
        var (r, diag) = Eval("1000000000000000000000000000000 * 100 + 9 - 1000000000000000000000000000000 * 100", edition);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
    }

    /// <summary>A difference that borrows past the 32nd digit is still TRUNCATED toward zero (§2002:8.8.1.3.3 via §2002:8.8.1.3.2):
    /// 10^30 x 10^30 - 1 is 60 nines exactly, cut to 32 nines, and 32 nines x 10^28 / 10^30 has the integer part 30 nines. A
    /// rounding engine, or a truncation that forgot the borrow, reaches 10^60 and 10^30 (31 digits), as 2014's 34-digit
    /// away-from-zero engine does.</summary>
    [Theory]
    [InlineData(2002, "999999999999999999999999999999")]
    [InlineData(2014, "1000000000000000000000000000000")]
    public void DifferenceBorrowingPastThirtyTwoDigits_IsTruncatedTowardZero(int edition, string expected)
    {
        const string e = "(1000000000000000000000000000000 * 1000000000000000000000000000000 - 1) / 1000000000000000000000000000000";
        var (r, diag) = Eval(e, edition);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
    }

    /// <summary>The standard-decimal default intermediate rounding is NEAREST-AWAY-FROM-ZERO (§2023:11.9.11.2 GR3 a),
    /// observed at a TIE. 1000000000000000000000000000001 x 10005 = 10005000000000000000000000000010005 exactly -
    /// 35 digits, so the 34-digit SDIDI drops a final 5 with nothing after it: away from zero keeps
    /// …0010010, nearest-even would keep …0010000. Subtracting the exact 10^30 x 10005 exposes the choice:
    /// 10010 (away) where nearest-even gives 10000. At 2002 the 35-digit product is truncated to 32 digits, dropping
    /// "005": 10000 (§2002:8.8.1.3.4). (At 2023 the 31-digit operand is outside System.Decimal.)</summary>
    [Theory]
    [InlineData(2002, "10000")]
    [InlineData(2014, "10010")]
    public void IntermediateRounding_IsTruncationAt2002AndAwayFromZeroAfter(int edition, string expected)
    {
        const string expr = "1000000000000000000000000000001 * 10005 - 1000000000000000000000000000000 * 10005";
        var (r, diag) = Eval(expr, edition);
        Assert.Empty(diag.Reports);
        Assert.Equal(expected, r!.Value.Literal);
    }

    /// <summary>§2002:8.8.1.3.1.1: the magnitude is at most 10^1000 - 10^968, and "Underflow is treated as a SIZE ERROR and is
    /// not rounded to zero" (§2002:8.8.1.3.1 r2). Thirty-three 31-digit factors give about 10^1023: refused with the
    /// 2002 range named; thirty-one give about 10^961 and stay in range. Thirty-three divisors give about 10^-1023.</summary>
    [Theory]
    [InlineData(33, '*', false)]
    [InlineData(31, '*', true)]
    [InlineData(33, '/', false)]
    [InlineData(31, '/', true)]
    public void Range_At2002_IsTen999Through10To1000(int operands, char op, bool inRange)
    {
        const string Nines = "9999999999999999999999999999999";
        string expr = op == '*' ? string.Join(" * ", Enumerable.Repeat(Nines, operands))
            : "1 " + string.Concat(Enumerable.Repeat(" / " + Nines, operands));
        var (r, diag) = Eval(expr, 2002);
        bool rangeRefused = diag.Reports.Any(x => x.Message.Contains("10^1000 - 10^968", StringComparison.Ordinal));
        Assert.Equal(!inRange, rangeRefused);
        if (rangeRefused)
        {
            Assert.Null(r);
            Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                               && x.Message.Contains("§2002:8.8.1.3.1.1", StringComparison.Ordinal));
        }
    }

    /// <summary>§2023:7.3.6.3 GR3 makes the final result "an integer numeric literal", and a fixed-point numeric literal
    /// has at most 31 digits (§2023:8.3.3.3.2). Standard arithmetic's 34-digit intermediates can carry a product no
    /// literal can spell — two 31-digit operands give about 1E62 — so the result is refused LOUDLY, never stored as a
    /// 62-digit "literal". At 2023 the same operands never enter System.Decimal's range (SR2).</summary>
    [Fact]
    public void FinalResult_BeyondTheLiteralCapacity_IsRefused()
    {
        const string expr = "9999999999999999999999999999999 * 9999999999999999999999999999999";
        var (r, diag) = Eval(expr, 2002);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Code == CtDiagCode.ArithmeticRule
                                           && x.Message.Contains("62 digits", StringComparison.Ordinal)
                                           && x.Message.Contains("§2023:7.3.6.3 GR3", StringComparison.Ordinal)
                                           && x.Message.Contains("§2023:8.3.3.3.2", StringComparison.Ordinal));

        var (r23, diag23) = Eval(expr, 2023);
        Assert.Null(r23);
        Assert.Contains(diag23.Reports, x => x.Message.Contains("SR2", StringComparison.Ordinal)
                                             && !x.Message.Contains("SR1b", StringComparison.Ordinal));
    }

    /// <summary>The capacity's own edge: a 31-digit final result IS a valid integer literal and is kept (the refusal
    /// above starts at 32).</summary>
    [Fact]
    public void FinalResult_AtTheLiteralCapacity_IsKept()
    {
        var (r, diag) = Eval("9999999999999999999999999999999 + 0", 2002);
        Assert.Empty(diag.Reports);
        Assert.Equal("9999999999999999999999999999999", r!.Value.Literal);

        var (r32, diag32) = Eval("9999999999999999999999999999999 + 1", 2002);   // 10^31: 32 digits
        Assert.Null(r32);
        Assert.Contains(diag32.Reports, x => x.Message.Contains("32 digits", StringComparison.Ordinal));
    }

    /// <summary>A SINGLE numeric literal is a literal, not an arithmetic expression (§2023:7.3.11.4 GR5), so no arithmetic
    /// mode bounds it: a legal 30-digit fixed-point literal (§2023:8.3.3.3.2 allows 31) keeps its value at 2023 too. It
    /// used to be refused there as "exceeding the .NET decimal evaluation range" — a mode that never applied to it.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void SoleLiteral_IsNotBoundByTheArithmeticMode(int edition)
    {
        var (r, diag) = Eval("123456789012345678901234567890", edition);
        Assert.Empty(diag.Reports);
        Assert.True(r!.Value.WasSingleLiteral);
        Assert.Equal("123456789012345678901234567890", r.Value.Literal);
    }

    /// <summary>An operand the mode cannot hold is the §2023:7.3.6.2 SR2 limit, reported as SR2 — not as an SR1 b)
    /// "shall be fixed-point numeric literals" violation it is not (the operand IS a fixed-point literal).</summary>
    [Fact]
    public void OperandBeyondTheModesRange_IsReportedAsSr2()
    {
        var (r, diag) = Eval("123456789012345678901234567890 + 1", 2023);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Message.Contains("SR2", StringComparison.Ordinal)
                                           && !x.Message.Contains("SR1b", StringComparison.Ordinal));

        var (r02, diag02) = Eval("123456789012345678901234567890 + 1", 2002);
        Assert.Empty(diag02.Reports);
        Assert.Equal("123456789012345678901234567891", r02!.Value.Literal);
    }

    /// <summary>An operand literal past the §2023:8.3.3.3.2 digit capacity is refused by that rule, in every mode.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void OperandLiteral_BeyondTheLiteralCapacity_IsRefused(int edition)
    {
        var (r, diag) = Eval("12345678901234567890123456789012 + 1", edition);
        Assert.Null(r);
        Assert.Contains(diag.Reports, x => x.Message.Contains("32 digit positions", StringComparison.Ordinal)
                                           && x.Message.Contains("§2023:8.3.3.3.2", StringComparison.Ordinal));
    }

    /// <summary>Numeric value equality is by VALUE (§2023:7.3.11.3 SR2 redefinition / §2023:7.3.8 relation), so two carrier
    /// spellings of one value — 1 and 1.0 — are equal and hash alike.</summary>
    [Fact]
    public void CarrierEquality_IsByValue()
    {
        Assert.True(CtNumeric.TryParseLiteral("1", out var one));
        Assert.True(CtNumeric.TryParseLiteral("1.0", out var onePointZero));
        var a = CtValue.Numeric(one, "1");
        var b = CtValue.Numeric(onePointZero, "1.0");
        Assert.Equal(a, b);
        Assert.True(a.RelationalEquals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, CtValue.Numeric(new CobolDec(2, 0), "2"));
    }
}
