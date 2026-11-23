// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// Family F2-R — the FLOATING-POINT argument overloads of the exact numeric/statistics intrinsics
/// (fix-queue PB2).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THESE EXIST. The F2 family computes in exact base-10 <see cref="Int128"/> at a known decimal scale,
/// which is right and is the design (deep-dive D1). But a floating-point ARGUMENT is legal for every one of these
/// functions — ISO §2023:15.7.3 AR1 and its siblings require class NUMERIC, and a COMP-2 item is class numeric — and
/// there was no path for one. The emitter dispatched on the FUNCTION's family (<c>sig.Float</c>) rather than on
/// the ARGUMENT's type, so a double expression was handed to an <c>Int128</c> parameter and the user saw a raw
/// Roslyn error escape the compiler:
/// </para>
/// <code>
///   01 D USAGE COMP-2 VALUE -3.5.
///       COMPUTE R = FUNCTION ABS(D)
///   error: backend compilation failed (generated C# at F1.g.cs):
///     (43,69): error CS1503: Argument 1: cannot convert from 'double' to 'System.Int128'
/// </code>
/// <para>
/// Ten of the eleven functions probed failed that way. That is worse than a wrong answer: legal COBOL produced an
/// INTERNAL failure, phrased in the generated C# the user never asked to see.
/// </para>
/// <para>
/// <b>Why a double body is the CONFORMING answer here, not a shortcut.</b> Under native arithmetic — the default —
/// §2023:15.4.1 makes "the characteristics and representation of the returned value … defined by the implementor", and
/// each of these functions is defined by an equivalent arithmetic expression evaluated in that same native
/// arithmetic. With a floating-point argument the EAE's own operands are binary64, so evaluating it in binary64 IS
/// the equivalent expression. Nothing exact is being given up: there was no exact value to preserve once the
/// argument arrived as a float.
/// </para>
/// <para>
/// ⛔ <b>THESE ARE NOT OVERLOADS, AND THE FIRST ATTEMPT PROVED WHY.</b> Sharing the F2 names looked ideal: the
/// emitter's float path already renders each argument through <c>Dbl()</c> and calls <c>sig.RuntimeMethod</c> by
/// name, so one dispatch line would have routed everything with no second naming scheme. It does not compile.
/// <c>Int128</c> has no implicit conversion from <see cref="double"/>, but an integer LITERAL converts implicitly
/// to BOTH — so <c>FUNCTION MAX(5 7)</c> emitted <c>MaxScaled(5, 7)</c> and C# reported
/// <c>CS0121: The call is ambiguous between 'MaxScaled(params Int128[])' and 'MaxScaled(params double[])'</c>,
/// breaking six previously-green corpus programs that had nothing to do with floats.
/// <para>
/// So the real bodies carry a <c>…Real</c> name — but by a CONVENTION, not a table: <c>XxxScaled</c> becomes
/// <c>XxxReal</c>, and any other name gains a <c>Real</c> suffix. One string transform in
/// <c>IntrinsicRenderer.RealMethod</c>, and <c>IntrinsicRealArgDriftTests</c> asserts every exact method
/// reachable with a real argument has its counterpart — so the convention is enforced rather than remembered.
/// </para>
/// <para>
/// ⛔ <b>NO BODY HERE TAKES A §2023:15.3 TYPE-6 (INTEGER) ARGUMENT AS A DOUBLE</b> (kb/Work PB651). An integer has no
/// binary64-ness: the renderer's one argument entry (<c>IntrinsicRenderer.RawArg</c>) takes a type-6 operand's
/// INTEGER through the one type-6 intake before any lane is chosen, so a float there no longer moves a call to this
/// lane, and the integer-only functions' bodies (the date family, the TEST- validators, the windowing trio, MOD) were
/// deleted rather than kept as unreachable code. A mixed row keeps its body for its type-10 position:
/// <see cref="CombinedDatetimeReal"/> is reached by a float argument-2, and its argument-1 arrives an integer.
/// </para>
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>§2023:15.7 ABS — the absolute value of a floating-point argument.</summary>
    public static double AbsReal(double v) => Math.Abs(v);

    /// <summary>§2023:15.81 SIGN — −1 / 0 / +1. Mirrors the exact body: only a value that IS zero returns 0, so a
    /// negative zero reports 0 rather than −1 (IEEE −0.0 compares equal to 0.0).</summary>
    public static long SignOfReal(double v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    /// <summary>§2023:15.44 INTEGER — the greatest integer not greater than the argument (a FLOOR, so −3.5 gives −4).</summary>
    public static double FloorReal(double v) => Math.Floor(v);

    /// <summary>§2023:15.49 INTEGER-PART — truncation toward zero (so −3.5 gives −3, unlike §2023:15.44).</summary>
    public static double TruncateReal(double v) => Math.Truncate(v);

    /// <summary>§2023:15.42 FRACTION-PART — argument minus its integer part, keeping the argument's sign.</summary>
    public static double FractionPartReal(double v) => v - Math.Truncate(v);

    /// <summary>§2023:15.77 REM — the TRUNCATED remainder, whose result takes the sign of argument-1
    /// (§2023:15.77.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2))</c>).
    /// ⚠ Distinct from MOD exactly as the two exact bodies are: REM(−7, 3) is −1 where MOD(−7, 3) is 2. MOD has no
    /// binary64 body: both its positions are §2023:15.3 type 6, so the renderer takes their integers first and the call
    /// never reaches this lane (kb/Work PB651). Shares <see cref="RemZeroDivisor"/> with the exact carrier (one raise
    /// site per rule, fix-queue PB32), and computes C#'s <c>%</c> on the two doubles — IEC 60559's exact truncated
    /// remainder, which IS §2023:15.77.4's value; the literal <c>a − (b × truncate(a / b))</c> cancels catastrophically
    /// once <c>b·⌊a/b⌋</c> passes 2⁵³ (fix-queue PB65's sweep).</summary>
    public static double RemReal(double a, double b) => b == 0 ? RemZeroDivisor() : a % b;

    /// <summary>§2023:15.59 MAX — the leftmost argument with the greatest value (the one selection rule, kb/Work PB2635).</summary>
    public static double MaxReal(params ReadOnlySpan<double> xs) => xs[ExtremeIndex(new ValueOrder<double>(xs), true, "MAX")];

    /// <summary>§2023:15.63 MIN — the leftmost argument with the least value.</summary>
    public static double MinReal(params ReadOnlySpan<double> xs) => xs[ExtremeIndex(new ValueOrder<double>(xs), false, "MIN")];

    // ⛔ THE binary64 LIST BODIES FORM THEIR EQUIVALENT ARITHMETIC EXPRESSION IN THE binary64 INTERMEDIATE (kb/Work
    // PB1581), so a sum or a half-sum that leaves binary64's range is §2023:14.7.5 case 5 — EC-SIZE-OVERFLOW, or
    // EC-SIZE-UNDERFLOW for a nonzero value rounded to zero — through the ONE check the checked native operators
    // and FUNCTION EXP use (CobolFloat.InBinary64Range), never a silent ±Infinity: SUM(C C) over a COMP-2 C
    // holding 1.0E+308 printed Infinity. A list with a NON-FINITE argument keeps its DOC-A.1-70 disposition
    // (ListInRange), exactly as a non-finite operand does.

    /// <summary>A list body's binary64 value, range-checked when every argument is finite.</summary>
    private static double ListInRange(double r, ReadOnlySpan<double> xs, bool nonzero, string function)
    {
        foreach (double x in xs) if (!double.IsFinite(x)) return r;
        return InBinary64Range(r, nonzero, function);
    }

    /// <summary>The half-sum <c>(a + b) / 2</c> a list body forms (MIDRANGE's EAE, MEDIAN's even-count middle), each
    /// operation range-checked.</summary>
    private static double HalfSumInRange(double a, double b, ReadOnlySpan<double> xs, string function)
    {
        double s = ListInRange(a + b, xs, nonzero: false, function);
        return ListInRange(s / 2.0, xs, nonzero: s != 0, function);
    }

    /// <summary>§2023:15.88 SUM.</summary>
    public static double SumReal(params ReadOnlySpan<double> xs)
    {
        RequireArguments(xs.Length, "SUM");
        double t = 0;
        foreach (double x in xs) t += x;
        return ListInRange(t, xs, nonzero: false, "SUM");
    }

    /// <summary>§2023:15.76 RANGE — MAX minus MIN.</summary>
    public static double RangeReal(params ReadOnlySpan<double> xs)
    {
        RequireArguments(xs.Length, "RANGE");
        return ListInRange(MaxReal(xs) - MinReal(xs), xs, nonzero: false, "RANGE");
    }

    /// <summary>§2023:15.61 MEDIAN — the middle value of the sorted arguments; the mean of the two middle values when
    /// the count is even (§2023:15.61.4).</summary>
    public static double MedianReal(params ReadOnlySpan<double> xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        double[] s = [.. xs];
        Array.Sort(s);
        int m = s.Length / 2;
        return (s.Length & 1) == 1 ? s[m] : HalfSumInRange(s[m - 1], s[m], xs, "MEDIAN");
    }

    /// <summary>§2023:15.62 MIDRANGE — the mean of the greatest and least arguments.</summary>
    public static double MidrangeReal(params ReadOnlySpan<double> xs)
    {
        RequireArguments(xs.Length, "MIDRANGE");
        return HalfSumInRange(MaxReal(xs), MinReal(xs), xs, "MIDRANGE");
    }

    /// <summary>§2023:15.60 MEAN — the arithmetic mean of the arguments.</summary>
    public static double MeanReal(params ReadOnlySpan<double> xs)
    {
        RequireArguments(xs.Length, "MEAN");
        double t = 0;
        foreach (double x in xs) t += x;
        _ = ListInRange(t, xs, nonzero: false, "MEAN");                  // the EAE's sum, then its quotient
        return ListInRange(t / xs.Length, xs, nonzero: t != 0, "MEAN");
    }

    /// <summary>§2023:15.71 ORD-MAX — the 1-based ORDINAL POSITION of the greatest argument, leftmost on a tie
    /// (§2023:15.71.4 RV3). Returns a position, never a value, so the count — not the arguments — bounds the result.</summary>
    public static double OrdMaxReal(params ReadOnlySpan<double> xs) => ExtremeIndex(new ValueOrder<double>(xs), true, "ORD-MAX") + 1;

    /// <summary>
    /// §2023:15.17 COMBINED-DATETIME — <c>argument-1 + (argument-2 / 100000)</c> exactly as §2023:15.17.4 RV1 writes it.
    /// </summary>
    /// <remarks>
    /// Argument-2 is "in standard numeric time form" (§2023:15.17.3 AR2) and §2023:15.6 types it <c>Num2</c>, so it may
    /// legitimately be a floating-point item — which is why this body is needed even though argument-1 is an
    /// integer date. The exact twin in <c>CobolDate</c> expresses the same expression as a scale shift
    /// (<c>date × 10^(scale+5) + secUnscaled</c> read at scale+5), and the two agree by construction.
    /// <para>⛔ AND IT ASKS THE ONE §2023:15.17.3 SCREEN, like its two exact-carrier twins (kb/Work PB620). This body
    /// had no argument rule at all, so a COMP-2 argument-2 of 86400 computed a fabricated 1.8639999999999 in
    /// silence where the identical VALUE in a fixed-point item terminated the run unit with
    /// EC-ARGUMENT-FUNCTION — a rule about a VALUE answering differently for each CARRIER, which is the PB32
    /// shape. The substituted result is §2023:15.3's documented default 0, the one both other carriers return, and
    /// the LEAP-SECOND state reaches here for the same reason it reaches them: §2023:7.3.17.4 GR4/GR5 is what sets
    /// the bound.</para>
    /// </remarks>
    public static double CombinedDatetimeReal(double integerDate, double seconds, bool leapSecond = false) =>
        CobolDate.CombinedDatetimeOutOfRangeReal(integerDate, seconds, leapSecond)
            ? 0
            : integerDate + (seconds / 100000.0);

    /// <summary>§2023:15.72 ORD-MIN — the 1-based ordinal position of the least argument, leftmost on a tie.</summary>
    public static double OrdMinReal(params ReadOnlySpan<double> xs) => ExtremeIndex(new ValueOrder<double>(xs), false, "ORD-MIN") + 1;
}
