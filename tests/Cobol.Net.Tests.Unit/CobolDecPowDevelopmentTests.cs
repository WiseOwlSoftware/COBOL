// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §8.8.1.5.4 r2e DEVELOPMENT (owner decision D-C, 2026-08-30; kb/Work PB167). r2e leaves the equivalent
/// arithmetic expression for a non-integer exponent to the implementor but constrains how it is developed:
/// "Operands used in the development of that value shall be in SDIDI form. All additions, subtractions,
/// multiplications and divisions performed in the development of the result shall be performed in accordance
/// with the corresponding rules in ISO/IEC 60559:2020" (cite.py-verified). Before D-C the development went
/// through IEEE binary64 — <c>Math.Pow</c> on the non-integer arm and a <c>Math.Log10</c> decomposition on the
/// past-loop-bound integer escape — so it satisfied neither shall, delivered ~17 significant digits, and
/// answered the SAME mathematical value differently depending on which of the two arms the operands reached.
///
/// <para>WHAT IS PINNED HERE. (1) The structural fact — no binary64 bridge is reachable from the development
/// at all. (2) The identities the choice of equivalent expression BUYS: at |operand-2| = ½ the expression is
/// <c>FUNCTION SQRT(operand-1)</c>, whose standard-decimal value §15.84.4 r2 fixes EXACTLY, so <c>b ** 0.5</c>
/// and <c>SQRT(b)</c> agree digit-for-digit by construction. (3) Agreement with the true value to the digits an
/// SDIDI development can carry — NOT 34: §8.8.1.5.2 rounds every operation to 34 digits, so a development of N
/// operations accumulates N roundings. The published determination (CONFORMANCE.md §7) is an r2e-conforming
/// SDIDI development, and the tolerances below are stated in those terms, never as "34 correct digits".</para>
/// </summary>
public sealed class CobolDecPowDevelopmentTests
{
    private const CobolRounding Mode = CobolRounding.NearestAwayFromZero;

    /// <summary>⛔ THE DRIFT GUARD. A binary64 transcendental anywhere in <c>CobolDec</c> is a step of the
    /// development that is not an SDIDI operation — which is exactly what r2e's two shalls forbid, and exactly
    /// what the file contained.
    /// <para>⛔ WHAT IT PROVES, AND WHAT IT DOES NOT (kb/Work PB270). It proves ONE thing: none of the
    /// spellings below appears in <c>CobolDec.cs</c>'s non-comment text. It is NOT equivalent to "no binary64
    /// is reachable from <c>Pow</c>" — reachability is a property of the CALL GRAPH and no substring scan can
    /// decide it, and the comment here used to claim that equivalence outright. Two premises it does not
    /// enforce: that a binary64 step would be spelled one of these ways (`Math.Log2(` slips through
    /// `Math.Log10(`+`Math.Log(`; `using M = System.Math;` slips through all of them), and that the
    /// development lives entirely in this one file — <c>CobolIntrinsics.RealArgs.cs</c> already contains
    /// <c>CobolDec.FromDouble(Math.Pow(…))</c>, legitimately, as the §8.8.1.3 NATIVE arm, in a file this scan
    /// does not read. What the scan IS is a tripwire on the shapes that actually shipped and were deleted,
    /// widened to the near-miss spellings; the reachability audit is the sweep recorded in kb/Work PB167, and
    /// the value pins below are the guard with teeth — a binary64 reintroduction gives
    /// 1.0618366495807028 where <see cref="PastLoopBoundEscape_KeepsTheNegativeBasesParitySign"/> needs
    /// 1.061836649543504…, whichever file it sits in.</para>
    /// <para>⛔ <c>ToDouble</c>/<c>FromDouble</c> ARE DELIBERATELY NOT BANNED. Both are <c>CobolDec</c>'s own
    /// §8.8.1.5.1 conversion API, defined in this file by necessity and called from six sites outside it;
    /// banning their spelling would ban the type's public surface, not a development step.</para>
    /// <para>Whole-line comments are stripped first — the file's forensic remarks NAME the calls they
    /// replaced, and a guard that cannot tell code from prose would force the history out of the
    /// file.</para></summary>
    [Fact]
    public void NoBinary64TranscendentalIsSpelledInTheR2eDevelopmentFile()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "Values", "Numeric", "CobolDec.cs"));
        string src = string.Join('\n', lines.Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
        foreach (string banned in new[]
                 {
                     // the two shapes that shipped and were deleted (Math.Pow on the non-integer arm, a
                     // Math.Log10 decomposition on the escape) …
                     "Math.Pow(", "Math.Log10(", "Math.Log(", "Math.Exp(", "Math.Sqrt(",
                     // … and the near-miss spellings a substring scan of only those five lets through.
                     "Math.Log2(", "Math.Cbrt(", "Math.ScaleB(",
                     "double.Pow(", "double.Log(", "double.Log10(", "double.Exp(", "double.Sqrt(",
                 })
            Assert.False(src.Contains(banned, StringComparison.Ordinal),
                $"CobolDec.cs spells {banned} — the §8.8.1.5.4 r2e development must stay in SDIDI form "
                + "(operands in SDIDI form; every add/sub/mul/div an ISO/IEC 60559:2020 decimal operation)");
        // The one premise that CAN be made mechanical: the type is not partial, so "this file" is the whole
        // type — the scan's scope is the development's scope for anything declared on CobolDec itself.
        Assert.DoesNotContain("partial", string.Join('\n', lines.Where(l => l.Contains("struct CobolDec", StringComparison.Ordinal))),
            StringComparison.Ordinal);
    }

    // ── The identities the chosen equivalent expression buys ─────────────────────────────────────────────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(7)]
    public void HalfExponent_IsExactlyTheSquareRoot(int b)
    {
        // §8.8.1.5.4 r2e's chosen expression at ½ IS §15.84.4 r2's SQRT — so this is equality, not closeness.
        var value = new CobolDec(b, 0);
        Assert.Equal(CobolDec.Sqrt(value, Mode), CobolDec.Pow(value, CobolDec.From(5, 1), Mode));
    }

    [Fact]
    public void MinusHalfExponent_IsR3sReciprocalOfTheSquareRoot()
    {
        // §8.8.1.5.4 r3: operand-2 < 0 ⇒ 1 / (operand-1 ** ABS(operand-2)).
        var two = new CobolDec(2, 0);
        Assert.Equal(CobolDec.Div(new CobolDec(1, 0), CobolDec.Sqrt(two, Mode), Mode),
            CobolDec.Pow(two, CobolDec.From(-5, 1), Mode));
    }

    /// <summary>⛔ §8.8.1.5.4 r3 FOR EVERY NEGATIVE EXPONENT, NOT ONLY −½ (kb/Work PB266). r3 is not
    /// implementor latitude — r2's latitude is scoped "When the value of operand-2 is greater than zero" —
    /// and it is an EQUIVALENCE, not a tolerance: "the result shall be equivalent to the evaluation of the
    /// arithmetic expression (1 / (operand-1 ** FUNCTION ABS (operand-2)))". Under §8.8.1.5.2's per-operation
    /// 34-digit rounding that expression is a division whose divisor is this same development at |operand-2|,
    /// and it is a DIFFERENT value from exp(−|p| × ln b): before the reciprocal was hoisted to one place,
    /// 2 ** −0.25 answered …4762332146 where 1 / (2 ** 0.25) is …4762332141. The assertion is exact equality
    /// on the SDIDI — the same thing the language's own `=` does to two intermediates — because a tolerance
    /// cannot see a 34th-digit divergence and an identity is not an accuracy claim.
    /// <para>⚠ Honouring r3 costs accuracy here and the standard asks for it anyway: the true 34-digit
    /// 2^−¼ is …4762332149, so the reciprocal form is 8 ulp out where the direct form was 3.</para></summary>
    [Theory]
    [InlineData("2", "0.25")]
    [InlineData("7", "0.125")]
    [InlineData("10", "0.3")]
    [InlineData("3", "1.5")]
    [InlineData("2", "2.5")]
    [InlineData("0.5", "0.75")]
    [InlineData("2", "0.5")]        // the SQRT arm — r3 reaches it through the same one reciprocal
    [InlineData("2", "3")]          // an INTEGER exponent inside the loop bound
    [InlineData("1.0000001", "500000")]   // …at the loop bound
    [InlineData("1.0000001", "600001")]   // …and past it, where r3 used to be discontinuous
    public void NegativeExponent_IsR3sReciprocalOfTheSameDevelopment(string b, string e)
    {
        var bb = Parse(b);
        var positive = Parse(e);
        var negative = new CobolDec(-positive.Sig, positive.Exp);
        Assert.Equal(CobolDec.Div(new CobolDec(1, 0), CobolDec.Pow(bb, positive, Mode), Mode),
            CobolDec.Pow(bb, negative, Mode));
    }

    /// <summary>⛔ THE EXPONENT PAST THE LONG RANGE IS THE WRITTEN ONE (kb/Work PB267). <c>TryIntegerValue</c>
    /// clamps its magnitude to <see cref="long.MaxValue"/> for the loop-bound test, and the past-bound escape
    /// used to rebuild the exponent from that clamp — so every exponent past 2⁶³−1 was silently replaced by
    /// 9223372036854775807 and the answer was for a different expression than the one written. Here the base
    /// is 1 + 10⁻³³ (the closest SDIDI value to one) and the exponent 10²⁰: the value is
    /// exp(10²⁰ × ln(1 + 10⁻³³)) = 1.000000000000100000000000005, where the clamped probe gave
    /// 1.0000000000000092 — equal in the first thirteen fraction digits, so it stored a silent 1.0000000000000.
    /// <para>⚠ This case is also why the disposition cannot be "a clamped exponent with |base| ≠ 1 is out of
    /// range": the value is perfectly representable, and that screen is the spurious size error PB145
    /// removed.</para></summary>
    [Fact]
    public void PastLongRangeExponent_UsesTheOperand_NotTheClampedBoundProbe()
    {
        // The true value to 34 significant digits (independently computed at 80): the development lands on it
        // EXACTLY, so this is equality on the SDIDI and not a tolerance.
        var got = CobolDec.Pow(Parse("1.000000000000000000000000000000001"), Parse("1.0E+20"), Mode);
        Assert.Equal(0, CobolDec.Compare(Parse("1.000000000000100000000000005000000"), got));
        // …and the clamped probe's answer, exp(9223372036854775807 × 10⁻³³), is NOT this value.
        Assert.NotEqual(0, CobolDec.Compare(Parse("1.0000000000000092233720368547758"), got));
    }

    /// <summary>The same escape's two out-of-range dispositions, by §8.8.1.5.2 r2's own names. With the
    /// clamped probe both were wrong: 1.0000000000000001 ** 10²⁵ answered a finite 3.68E+400 (out of range
    /// only for the receiver, under a different name) and 0.9999999999999999 ** 10²⁵ answered a silent
    /// 2.71E−401 where the value is below the decimal128 range entirely.</summary>
    [Theory]
    [InlineData("1.0000000000000001", "EC-SIZE-OVERFLOW")]
    [InlineData("0.9999999999999999", "EC-SIZE-UNDERFLOW")]
    public void PastLongRangeExponent_OutOfRange_TakesR2sOwnName(string b, string ec)
    {
        var ex = Assert.Throws<CobolSizeError>(() => CobolDec.Pow(Parse(b), Parse("1.0E+25"), Mode));
        Assert.Equal(ec, ex.EcName);
    }

    /// <summary>⛔ THE NEAR-UNIT LOGARITHM BAND (kb/Work PB269). The three-square-root argument reduction
    /// carries u = m^(1/8) at 34 SIGNIFICANT digits about 1, so <c>u − 1</c> ≈ δ/8 cancels away log₁₀(1/δ) of
    /// them before the series runs: measured 17.05 correct digits at the worst point of |m−1| ≤ ¼, and
    /// EXACTLY ZERO at m = 1 + 10⁻³³, where the first root rounds straight back to 1 and ln m came out 0 — so
    /// (1 + 10⁻³³) ** 10³⁰ answered a flat 1 against a true 1.0010005001667…. The series over δ has no
    /// cancellation to lose (m − 1 is EXACT in this band) and measures 32.74 digits at the same worst point.
    /// The published determination called this degradation "inherent to the equivalent expression"; it was
    /// this coding of it, and CONFORMANCE.md §7 now says so.</summary>
    /// <remarks>The expected values are the MATHEMATICAL ones, independently computed to 80 digits and quoted
    /// to 34 — never read back from this implementation. Before the near-unit arm these four measured 3.0,
    /// 26.4, 27.4 and 26.6 correct digits; they now measure 33.2, 33.5, 34.5 and 32.4.</remarks>
    [Theory]
    [InlineData("1.000000000000000000000000000000001", "1.0E+30", "1.001000500166708341668055753993058", 30)]
    [InlineData("1.0000000000000001", "1000000", "1.000000000100000000004999995000167", 30)]
    [InlineData("0.9999999999999999", "1000000", "0.9999999999000000000049999949998333", 30)]
    [InlineData("1.00001", "1000000", "22025.36450639133265027512688109265", 30)]
    public void NearUnitBase_KeepsTheLogarithmsDigits(string b, string e, string expected, int digits)
        => AssertSignificantDigits(expected, CobolDec.Pow(Parse(b), Parse(e), Mode), digits);

    // ── The development's accuracy, stated as what it can carry ──────────────────────────────────────────────

    /// <summary>The true values below are the mathematical ones (independently computed to 60 digits, not read
    /// back from this implementation). The assertion is agreement to 30 significant digits — comfortably inside
    /// the ~31 the development measured, and comfortably outside the ~17 the binary64 predecessor reached, so
    /// this test both catches a regression to binary64 and refuses to claim the 34 r2e cannot promise.</summary>
    [Theory]
    [InlineData("2", "0.25", "1.189207115002721066717499970560475")]
    [InlineData("10", "0.3", "1.995262314968879601352455396739535")]
    [InlineData("2", "1.5", "2.828427124746190097603377448419396")]
    [InlineData("7", "0.125", "1.275373106858454085386009750458792")]
    [InlineData("0.5", "0.75", "0.594603557501360533358749985280237")]
    [InlineData("2", "-0.25", "0.840896415253714543031125476233214")]
    public void NonIntegerExponent_AgreesWithTheTrueValueToThirtyDigits(string b, string e, string expected)
    {
        var got = CobolDec.Pow(Parse(b), Parse(e), Mode);
        AssertSignificantDigits(expected, got, 30);
    }

    /// <summary>The past-loop-bound INTEGER escape now enters the SAME development (there used to be two, and
    /// they disagreed), and a negative base keeps the sign its exponent's exact parity gives it — the old
    /// <c>PowByLogs</c> took log10|b| and never restored it, so an ODD exponent answered POSITIVE
    /// (measured at HEAD before D-C: (−1.0000001) ** 600001 = +1.06183664958…).</summary>
    [Fact]
    public void PastLoopBoundEscape_KeepsTheNegativeBasesParitySign()
    {
        var b = Parse("-1.0000001");
        var odd = CobolDec.Pow(b, new CobolDec(600_001, 0), Mode);
        var even = CobolDec.Pow(b, new CobolDec(600_002, 0), Mode);
        Assert.True(odd.Sig < 0, $"an odd exponent owes a negative result, got {odd.Sig}");
        Assert.True(even.Sig > 0, $"an even exponent owes a positive result, got {even.Sig}");
        AssertSignificantDigits("-1.061836649543504535719183183009635", odd, 25);
        AssertSignificantDigits("1.061836755727169490069636754927953", even, 25);
    }

    private static CobolDec Parse(string text)
    {
        Assert.True(CobolNet.Common.NumericLiteral.TryParseExact(text, out Int128 sig, out int exp));
        return CobolDec.FromParsed(sig, exp, CobolRounding.NearestEven);
    }

    /// <summary>Assert <paramref name="got"/> matches <paramref name="expected"/>'s leading
    /// <paramref name="digits"/> significant decimal digits: |got/expected − 1| &lt; 10^−(digits−1), evaluated
    /// on the SDIDI itself so the comparison never passes through a binary64 of its own.</summary>
    private static void AssertSignificantDigits(string expected, CobolDec got, int digits)
    {
        var want = Parse(expected);
        var rel = CobolDec.Div(CobolDec.Sub(got, want, CobolRounding.NearestEven), want, CobolRounding.NearestEven);
        var tol = new CobolDec(1, -(digits - 1));
        var absRel = rel.Sig < 0 ? new CobolDec(-rel.Sig, rel.Exp) : rel;
        Assert.True(CobolDec.Compare(absRel, tol) < 0,
            $"relative error {absRel.Sig}E{absRel.Exp} exceeds 1E-{digits - 1} (expected ≈{expected})");
    }
}
