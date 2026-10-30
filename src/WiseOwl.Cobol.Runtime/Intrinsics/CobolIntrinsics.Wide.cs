// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE EXACT INTRINSIC FAMILY ON THE EXACT WIDE INTERMEDIATE (kb/Work PB3953; docs/CONFORMANCE.md DOC-A.1-123): the bodies
/// of ABS, SIGN, INTEGER, INTEGER-PART, FRACTION-PART, MOD, REM, MAX, MIN, ORD-MAX, ORD-MIN, SUM, RANGE, MEAN, MEDIAN and MIDRANGE
/// over <see cref="CobolWide"/> values, for a call whose argument is an arithmetic expression whose exact value passes the
/// <see cref="Int128"/> carrier (a nested product of 39 to 77 digits). They are the <c>…Scaled</c> bodies of
/// <c>CobolIntrinsics.Exact.cs</c> and <c>CobolIntrinsics.Select.cs</c> on the wider carrier, with the same contracts: the
/// variadic entries take arguments ALREADY ALIGNED by the emitter to one common scale, and the unary ones take the
/// argument's own scale.
/// <para>WHY THEY EXIST. Under native arithmetic the method of evaluating "all integer and numeric functions" is the
/// implementor's (ISO/IEC 1989:2023 §8.8.1.3), and so, by §15.4.1, are "the characteristics and representation of the
/// returned value". For that latitude CLAUDE.md rule 1 makes GnuCOBOL the answer, and GnuCOBOL evaluates a function's argument
/// expression into an arbitrary-precision temporary, so <c>FUNCTION ABS (BIG * BIG * K3)</c> of 18-, 18- and 3-digit items is
/// every digit of the 39-digit product. The argument used to reach these functions lowered to the 34-digit SDIDI by
/// round-to-odd, and the function answered from that image: ABS showed <c>…100000</c> for <c>…000999</c> and
/// <c>MOD (BIG * BIG * K3, 1000)</c> was 0 for 999.</para>
/// <para>Every constructive step is <see cref="CobolWide"/>'s own, which raises EC-SIZE-OVERFLOW rather than wrap (§14.7.5
/// case 5, A.1 item 179); the compiler routes a call here only when the arguments' digit bounds prove the work fits
/// <see cref="CobolWide.MaxDigits"/> (<c>IntrinsicCarrierRules.WideLaneDigitBound</c>), so that raise is never reached on a
/// value the SDIDI would have carried.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    private static int CompareAligned(CobolWide a, CobolWide b) => CobolWide.Compare(a, 0, b, 0);   // one scale: the emitter aligned them

    /// <summary>The value at <paramref name="scale"/> cut to an integer toward zero, with the cut fraction (the dividend's sign).
    /// A non-positive scale is already an integer: the trailing P positions are multiplied in.</summary>
    private static CobolWide IntegerPart(CobolWide v, int scale, out CobolWide fraction)
    {
        if (scale <= 0)
        {
            fraction = default;
            return CobolWide.Up(v, -scale);
        }
        return CobolWide.DivRem(v, CobolWide.PowerOfTen(scale), out fraction);
    }

    /// <summary>§15.3 type 6 on the exact wide carrier: the argument's integer at scale 0, behind the same integrality test every
    /// carrier's intake applies (<see cref="RequireIntegral"/>; with checking off the fraction truncates toward zero,
    /// DOC-A.1-124). The integer stays wide, so MOD and REM see every digit of a 39-digit dividend.</summary>
    public static CobolWide IntegerArgWide(CobolWide v, int scale)
    {
        CobolWide integer = IntegerPart(v, scale, out CobolWide fraction);
        RequireIntegral(fraction.Sign != 0, () => v.ToDouble(scale).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return integer;
    }

    /// <summary>ABS (§15.7.4): the magnitude, at the argument's own scale. Unlike the <see cref="Int128"/> body there is no
    /// unrepresentable magnitude: the wide form is sign and magnitude.</summary>
    public static CobolWide AbsWide(CobolWide v) => CobolWide.Abs(v);

    /// <summary>SIGN (§15.81.4): −1, 0 or +1.</summary>
    public static long SignWide(CobolWide v) => v.Sign;

    /// <summary>INTEGER (§15.44.4): the greatest integer less than or equal to the argument, at scale 0.</summary>
    public static CobolWide FloorWide(CobolWide v, int scale)
    {
        CobolWide integer = IntegerPart(v, scale, out CobolWide fraction);
        return fraction.Sign < 0 ? CobolWide.Sub(integer, CobolWide.From(1)) : integer;
    }

    /// <summary>INTEGER-PART (§15.49.4): the integer part of the argument (truncation toward zero), at scale 0.</summary>
    public static CobolWide TruncateWide(CobolWide v, int scale) => IntegerPart(v, scale, out _);

    /// <summary>FRACTION-PART (§15.42.4): <c>argument − FUNCTION INTEGER-PART (argument)</c>, at the argument's own scale.</summary>
    public static CobolWide FractionPartWide(CobolWide v, int scale)
    {
        if (scale <= 0) return default;
        IntegerPart(v, scale, out CobolWide fraction);
        return fraction;
    }

    /// <summary>MOD (§15.64.4): <c>a − b × FUNCTION INTEGER (a / b)</c>, the floored modulus, over aligned values. A zero divisor
    /// is §15.64.3 r2's one raise site (<see cref="ModZeroDivisor"/>), shared with every other carrier's body.</summary>
    public static CobolWide ModWide(CobolWide a, CobolWide b)
    {
        if (b.Sign == 0) return CobolWide.From(ModZeroDivisor());
        CobolWide.DivRem(a, b, out CobolWide remainder);
        // The truncated remainder has the dividend's sign; the floored one has the divisor's (−11 MOD 5 = 4, 11 MOD −5 = −4).
        return remainder.Sign != 0 && remainder.Sign != b.Sign ? CobolWide.Add(remainder, b) : remainder;
    }

    /// <summary>REM (§15.77.4): <c>a − b × FUNCTION INTEGER-PART (a / b)</c>, the truncated remainder, over aligned values.</summary>
    public static CobolWide RemWide(CobolWide a, CobolWide b)
    {
        if (b.Sign == 0) return CobolWide.From(RemZeroDivisor());
        CobolWide.DivRem(a, b, out CobolWide remainder);
        return remainder;
    }

    /// <summary>The position of the greatest (<paramref name="greatest"/>) or least aligned value; the leftmost of equal values
    /// (§15.59.4 r2, §15.63.4 r2, §15.71.4, §15.72.4).</summary>
    private static int SelectWide(CobolWide[] xs, bool greatest, string function)
    {
        RequireArguments(xs.Length, function);
        int at = 0;
        for (int i = 1; i < xs.Length; i++)
            if (greatest ? CompareAligned(xs[i], xs[at]) > 0 : CompareAligned(xs[i], xs[at]) < 0) at = i;
        return at;
    }

    /// <summary>MAX (§15.59.4): the content of the argument having the greatest value, at the common scale.</summary>
    public static CobolWide MaxWide(params CobolWide[] xs) => xs[SelectWide(xs, greatest: true, "MAX")];

    /// <summary>MIN (§15.63.4): the content of the argument having the least value, at the common scale.</summary>
    public static CobolWide MinWide(params CobolWide[] xs) => xs[SelectWide(xs, greatest: false, "MIN")];

    /// <summary>ORD-MAX (§15.71.4): the ordinal position of the argument having the greatest value.</summary>
    public static long OrdMaxWide(params CobolWide[] xs) => SelectWide(xs, greatest: true, "ORD-MAX") + 1;

    /// <summary>ORD-MIN (§15.72.4): the ordinal position of the argument having the least value.</summary>
    public static long OrdMinWide(params CobolWide[] xs) => SelectWide(xs, greatest: false, "ORD-MIN") + 1;

    /// <summary>SUM (§15.88.4): the sum of the aligned arguments, left to right.</summary>
    public static CobolWide SumWide(params CobolWide[] xs)
    {
        RequireArguments(xs.Length, "SUM");
        CobolWide sum = default;
        foreach (CobolWide x in xs) sum = CobolWide.Add(sum, x);
        return sum;
    }

    /// <summary>RANGE (§15.76.4): <c>FUNCTION MAX − FUNCTION MIN</c>, at the common scale.</summary>
    public static CobolWide RangeWide(params CobolWide[] xs)
    {
        RequireArguments(xs.Length, "RANGE");
        return CobolWide.Sub(MaxWide(xs), MinWide(xs));
    }

    /// <summary>⛔ MEAN (§15.60.4 r1) OVER THE EXACT WIDE SUM, ONE SDIDI QUOTIENT (kb/Work PB4075, PB3860): the arguments' exact sum
    /// divided by their count and rounded ONCE to the SDIDI's 34 digits (<see cref="CobolWide.QuotientDec"/>) in
    /// <paramref name="mode"/> — the value <see cref="MeanDec"/> forms for an SDIDI list, and the value every receiver then
    /// rounds once (§14.7.4.3). The arguments arrive ALREADY ALIGNED by the emitter to the common <paramref name="scale"/>. Lowering
    /// each argument to its round-to-odd image first and dividing that sum rounded twice: MEAN (BIG * BIG * K3, 1) of 18-, 18- and
    /// 3-digit items showed …000000000000100000 where one rounding of the exact …000500 is …000000000000000000.</summary>
    public static CobolDec MeanWide(CobolRounding mode, int scale, params CobolWide[] xs)
    {
        RequireArguments(xs.Length, "MEAN");
        return SumWide(xs).QuotientDec(scale, xs.Length, mode);
    }

    /// <summary>MEDIAN (§15.61.4): the middle of the sorted arguments for an odd count, the mean of the two middles for an even
    /// one — returned at the common scale PLUS ONE, so the halving is exact in both branches (odd: × 10; even: the sum × 5), as
    /// <see cref="MedianScaled"/> does.</summary>
    public static CobolWide MedianWide(params CobolWide[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        var sorted = (CobolWide[])xs.Clone();
        Array.Sort(sorted, CompareAligned);
        int mid = sorted.Length / 2;
        return sorted.Length % 2 != 0
            ? CobolWide.Up(sorted[mid], 1)
            : CobolWide.Mul(CobolWide.Add(sorted[mid - 1], sorted[mid]), CobolWide.From(5));
    }

    /// <summary>MIDRANGE (§15.62.4): <c>(MAX + MIN) / 2</c>, returned at the common scale plus one ((max + min) × 5, exact).</summary>
    public static CobolWide MidrangeWide(params CobolWide[] xs)
    {
        RequireArguments(xs.Length, "MIDRANGE");
        return CobolWide.Mul(CobolWide.Add(MaxWide(xs), MinWide(xs)), CobolWide.From(5));
    }
}
