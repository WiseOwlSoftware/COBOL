// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F2-R — the FLOATING-POINT argument overloads of the exact numeric/statistics intrinsics
/// (fix-queue PB2).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THESE EXIST. The F2 family computes in exact base-10 <see cref="Int128"/> at a known decimal scale,
/// which is right and is the design (deep-dive D1). But a floating-point ARGUMENT is legal for every one of these
/// functions — ISO §15.7.3 r1 and its siblings require class NUMERIC, and a COMP-2 item is class numeric — and
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
/// §15.4.1 makes "the characteristics and representation of the returned value … defined by the implementor", and
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
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>§15.7 ABS — the absolute value of a floating-point argument.</summary>
    public static double AbsReal(double v) => Math.Abs(v);

    /// <summary>§15.81 SIGN — −1 / 0 / +1. Mirrors the exact body: only a value that IS zero returns 0, so a
    /// negative zero reports 0 rather than −1 (IEEE −0.0 compares equal to 0.0).</summary>
    public static long SignOfReal(double v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    /// <summary>§15.44 INTEGER — the greatest integer not greater than the argument (a FLOOR, so −3.5 gives −4).</summary>
    public static double FloorReal(double v) => Math.Floor(v);

    /// <summary>§15.49 INTEGER-PART — truncation toward zero (so −3.5 gives −3, unlike §15.44).</summary>
    public static double TruncateReal(double v) => Math.Truncate(v);

    /// <summary>§15.42 FRACTION-PART — argument minus its integer part, keeping the argument's sign.</summary>
    public static double FractionPartReal(double v) => v - Math.Truncate(v);

    /// <summary>§15.64 MOD — the FLOORED modulus, whose result takes the sign of argument-2
    /// (§15.64.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER (argument-1 / argument-2))</c>).</summary>
    public static double ModReal(double a, double b) => b == 0 ? 0 : a - (b * Math.Floor(a / b));

    /// <summary>§15.77 REM — the TRUNCATED remainder, whose result takes the sign of argument-1
    /// (§15.77.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2))</c>).
    /// ⚠ Distinct from <see cref="ModReal(double, double)"/> exactly as the two exact bodies are: REM(−7, 3) is
    /// −1 where MOD(−7, 3) is 2.</summary>
    public static double RemReal(double a, double b) => b == 0 ? 0 : a - (b * Math.Truncate(a / b));

    /// <summary>§15.59 MAX — the greatest argument value.</summary>
    public static double MaxReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max();

    /// <summary>§15.63 MIN — the least argument value.</summary>
    public static double MinReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Min();

    /// <summary>§15.88 SUM.</summary>
    public static double SumReal(params double[] xs)
    {
        double t = 0;
        foreach (double x in xs) t += x;
        return t;
    }

    /// <summary>§15.76 RANGE — MAX minus MIN.</summary>
    public static double RangeReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max() - xs.Min();

    /// <summary>§15.61 MEDIAN — the middle value of the sorted arguments; the mean of the two middle values when
    /// the count is even (§15.61.4).</summary>
    public static double MedianReal(params double[] xs)
    {
        if (xs.Length == 0) return 0;
        double[] s = [.. xs];
        Array.Sort(s);
        int m = s.Length / 2;
        return (s.Length & 1) == 1 ? s[m] : (s[m - 1] + s[m]) / 2.0;
    }

    /// <summary>§15.62 MIDRANGE — the mean of the greatest and least arguments.</summary>
    public static double MidrangeReal(params double[] xs) =>
        xs.Length == 0 ? 0 : (xs.Max() + xs.Min()) / 2.0;

    /// <summary>§15.60 MEAN — the arithmetic mean of the arguments.</summary>
    public static double MeanReal(params double[] xs)
    {
        if (xs.Length == 0) return 0;
        double t = 0;
        foreach (double x in xs) t += x;
        return t / xs.Length;
    }

    /// <summary>§15.71 ORD-MAX — the 1-based ORDINAL POSITION of the greatest argument, leftmost on a tie
    /// (§15.71.4 r3). Returns a position, never a value, so the count — not the arguments — bounds the result.</summary>
    public static double OrdMaxReal(params double[] xs)
    {
        if (xs.Length == 0) return 0;
        int at = 0;
        for (int i = 1; i < xs.Length; i++) if (xs[i] > xs[at]) at = i;
        return at + 1;
    }

    /// <summary>
    /// §15.17 COMBINED-DATETIME — <c>argument-1 + (argument-2 / 100000)</c> exactly as §15.17.4 r1 writes it.
    /// </summary>
    /// <remarks>
    /// Argument-2 is "in standard numeric time form" (§15.17.3 r2) and §15.6 types it <c>Num2</c>, so it may
    /// legitimately be a floating-point item — which is why this body is needed even though argument-1 is an
    /// integer date. The exact twin in <c>CobolDate</c> expresses the same expression as a scale shift
    /// (<c>date × 10^(scale+5) + secUnscaled</c> read at scale+5), and the two agree by construction.
    /// </remarks>
    public static double CombinedDatetimeReal(double integerDate, double seconds) =>
        integerDate + (seconds / 100000.0);

    /// <summary>§15.72 ORD-MIN — the 1-based ordinal position of the least argument, leftmost on a tie.</summary>
    public static double OrdMinReal(params double[] xs)
    {
        if (xs.Length == 0) return 0;
        int at = 0;
        for (int i = 1; i < xs.Length; i++) if (xs[i] < xs[at]) at = i;
        return at + 1;
    }
}
