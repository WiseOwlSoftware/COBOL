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
    /// <summary>
    /// ⛔ THE ONE FLOAT→INTEGER-ARGUMENT LANDING for the §15.3 type-6 family (fix-queue PB21). Every integer-
    /// argument body below funnels through here so a float operand gets the IDENTICAL disposition a fixed-point
    /// one gets from <c>IntrinsicRenderer.AsInt</c> — truncation toward zero, the §15.3 implementor latitude for
    /// a value that is not the integer the rule demands. Two separate conversions would let the same function
    /// answer differently depending on how its argument happened to be stored, which is the receiver-shape class
    /// of defect PB13 closed; one helper makes that impossible rather than merely unlikely.
    /// <para>⚠ THE RANGE GUARD IS NOT DECORATION (PB22's lesson, applied before PB22 lands): a
    /// <c>(long)</c> cast on an out-of-range double is UNDEFINED in C#, so a huge operand would wrap into a
    /// plausible date rather than raising. Out of range ⇒ EC-ARGUMENT-FUNCTION (§15.3), never a wrapped value.
    /// The bound is stated as ±9.2e18 rather than long.MaxValue because the nearest double to long.MaxValue is
    /// ABOVE it — comparing against the exact integer would let the boundary value through the cast.</para>
    /// </summary>
    private static bool TryIntegerArg(double v, string fn, out long n)
    {
        if (double.IsFinite(v) && v > -9.2e18 && v < 9.2e18) { n = (long)Math.Truncate(v); return true; }
        Exceptions.ExceptionState.ArgumentError(
            $"{fn}: the floating-point argument {v} is outside the integer-argument range (ISO §15.3)");
        n = 0;
        return false;
    }

    // ── §15.3 type-6 INTEGER arguments reached with a FLOATING-POINT operand (PB21) ────────────────────────────
    // A COMP-2 item is category numeric (§8.5.2.12 item 2) hence class numeric (§8.5.2.1 Table 2), so
    // `Admissible('i')` ADMITS it — the integer-ness is a VALUE property, not a class the screen can reject on.
    // Without these bodies `IntrinsicRenderer.RenderNum`'s AnyRealArgument dispatch emitted a call to a member
    // that does not exist and the user saw a raw Roslyn CS0117 on conforming source.
    // ⚠ INTEGER-OF-BOOLEAN is DELIBERATELY ABSENT: §15.45.3 r1 requires class BOOLEAN, so PB19's screen rejects a
    // float operand at bind time and a body here would be unreachable code that reads as coverage.
    /// <summary>§15.22 DATE-OF-INTEGER with a floating-point argument.</summary>
    public static long DateOfIntegerReal(double v) => TryIntegerArg(v, "DATE-OF-INTEGER", out long n) ? CobolDate.DateOfInteger(n) : 0;
    /// <summary>§15.24 DAY-OF-INTEGER with a floating-point argument.</summary>
    public static long DayOfIntegerReal(double v) => TryIntegerArg(v, "DAY-OF-INTEGER", out long n) ? CobolDate.DayOfInteger(n) : 0;
    /// <summary>§15.46 INTEGER-OF-DATE with a floating-point argument.</summary>
    public static long IntegerOfDateReal(double v) => TryIntegerArg(v, "INTEGER-OF-DATE", out long n) ? CobolDate.IntegerOfDate(n) : 0;
    /// <summary>§15.47 INTEGER-OF-DAY with a floating-point argument.</summary>
    public static long IntegerOfDayReal(double v) => TryIntegerArg(v, "INTEGER-OF-DAY", out long n) ? CobolDate.IntegerOfDay(n) : 0;
    /// <summary>§15.90 TEST-DATE-YYYYMMDD with a floating-point argument.</summary>
    public static long TestDateYyyymmddReal(double v) => TryIntegerArg(v, "TEST-DATE-YYYYMMDD", out long n) ? CobolDate.TestDateYyyymmdd(n) : 0;
    /// <summary>§15.91 TEST-DAY-YYYYDDD with a floating-point argument.</summary>
    public static long TestDayYyyydddReal(double v) => TryIntegerArg(v, "TEST-DAY-YYYYDDD", out long n) ? CobolDate.TestDayYyyyddd(n) : 0;

    // The Y2K windowing trio (§15.23 / §15.25 / §15.100) — the optional argument-2/argument-3 keep the SAME
    // C#-optional defaults the exact bodies use (50 / the argument-3 = 0 execution-year sentinel), because the
    // renderer emits only the arguments actually written.
    /// <summary>§15.23 DATE-TO-YYYYMMDD with a floating-point argument.</summary>
    public static long DateToYyyymmddReal(double date, double off = 50, double baseYear = 0) =>
        TryIntegerArg(date, "DATE-TO-YYYYMMDD", out long d) && TryIntegerArg(off, "DATE-TO-YYYYMMDD", out long o)
        && TryIntegerArg(baseYear, "DATE-TO-YYYYMMDD", out long b) ? CobolDate.DateToYyyymmdd(d, o, b) : 0;
    /// <summary>§15.25 DAY-TO-YYYYDDD with a floating-point argument.</summary>
    public static long DayToYyyydddReal(double day, double off = 50, double baseYear = 0) =>
        TryIntegerArg(day, "DAY-TO-YYYYDDD", out long d) && TryIntegerArg(off, "DAY-TO-YYYYDDD", out long o)
        && TryIntegerArg(baseYear, "DAY-TO-YYYYDDD", out long b) ? CobolDate.DayToYyyyddd(d, o, b) : 0;
    /// <summary>§15.100 YEAR-TO-YYYY with a floating-point argument.</summary>
    public static long YearToYyyyReal(double yy, double off = 50, double baseYear = 0) =>
        TryIntegerArg(yy, "YEAR-TO-YYYY", out long y) && TryIntegerArg(off, "YEAR-TO-YYYY", out long o)
        && TryIntegerArg(baseYear, "YEAR-TO-YYYY", out long b) ? CobolDate.YearToYyyy(y, o, b) : 0;

    // ── NATIVE EXPONENTIATION (ISO §8.8.1.2 rule 6; fix-queue PB18 + PB28) ──────────────────────────────────────

    /// <summary>
    /// ⛔ THE §8.8.1.2 RULE-6 SCREEN, WRITTEN ONCE FOR EVERY NATIVE ARM (fix-queue PB28).
    /// </summary>
    /// <remarks>
    /// Rule 6 is a GENERAL rule of arithmetic-expression evaluation — its own title is "Native, standard-binary,
    /// and standard-decimal arithmetic" — so it binds native `**` exactly as it binds the SDIDI one, and two of
    /// its three parts are mandatory <c>shall</c> requirements with a named exception condition:
    /// <list type="bullet">
    ///   <item><b>r6a</b> — a zero base shall have an exponent greater than zero, else EC-SIZE-EXPONENTIATION;</item>
    ///   <item><b>r6c</b> — a negative base shall have an integer exponent, else EC-SIZE-EXPONENTIATION.</item>
    /// </list>
    /// <c>CobolDec.Pow</c> has enforced both since it was written; every NATIVE arm went straight to
    /// <c>System.Math.Pow</c> with no rule-6 check at all, so the same program answered differently depending only
    /// on whether an ARITHMETIC clause was present. MEASURED at <c>--std 2023</c> with an <c>ON SIZE ERROR</c>
    /// phrase that did NOT fire: <c>0 ** 0</c> returned <b>1</b> (IEEE's convention, not COBOL's) and
    /// <c>-2 ** 0.5</c> returned <b>0</b> (<c>Math.Pow</c> yields NaN and <c>FromDouble(NaN, ws)</c> quantizes it
    /// to zero). Both are wrong ANSWERS delivered silently. EC-SIZE-EXPONENTIATION is Fatal in Table 14.
    /// <para>⚠ r6b — "if the evaluation yields both a positive and a negative real number, the value returned is
    /// the positive number" — is NOT a screen: it is a selection rule, and it cannot arise here because
    /// <c>Math.Pow</c> and the exact loop below each yield a single value. Checked in this pass rather than left
    /// to be discovered as a fourth leg.</para>
    /// </remarks>
    private static void CheckPowRule6(double b, double e)
    {
        if (b == 0 && e <= 0)
            throw new CobolSizeError(
                $"exponentiation: a zero base requires an exponent greater than zero, not {e} "
                + "(ISO §8.8.1.2 rule 6a)", "EC-SIZE-EXPONENTIATION");
        if (b < 0 && e != Math.Floor(e))
            throw new CobolSizeError(
                $"exponentiation: a negative base ({b}) requires an integer exponent, not {e} "
                + "(ISO §8.8.1.2 rule 6c)", "EC-SIZE-EXPONENTIATION");
    }

    /// <summary>Native <c>**</c> on the FLOATING arm — the §8.8.1.3 implementor-defined approximation, screened by
    /// <see cref="CheckPowRule6"/> first so the two mandatory legs of rule 6 hold on this arm too (PB28).</summary>
    public static double PowNativeReal(double b, double e)
    {
        CheckPowRule6(b, e);
        return Math.Pow(b, e);
    }

    /// <summary>
    /// ⛔ NATIVE <c>**</c> WITH AN INTEGER BASE AND AN INTEGER EXPONENT — EXACT WHEN IT FITS, THE DOCUMENTED
    /// DOUBLE APPROXIMATION WHEN IT DOES NOT (owner decision 2026-08-03; fix-queue PB18).
    /// </summary>
    /// <remarks>
    /// <para>§8.8.1.3 makes native arithmetic implementor-defined, so either technique conforms — but routing an
    /// integer power through <c>System.Math.Pow</c> contradicted our OWN documented technique (numeric design D3,
    /// "the exact Int128 fixed-point engine"): <c>COMPUTE R = 10 ** 30</c> into a <c>PIC 9(31)</c> returned
    /// <c>1000000000000000071935427891953</c> where <see cref="Int128"/> holds 10³⁰ exactly.</para>
    /// <para><b>The owner's decision and the survey behind it.</b> Exact while the result fits the carrier, the
    /// documented double approximation past it — never a size error merely for outgrowing the carrier. That
    /// follows the field: IBM Enterprise COBOL and Micro Focus both fall back to floating point past the fixed
    /// capacity, and GnuCOBOL has no boundary at all (GMP arbitrary precision). The cost is that the technique is
    /// VALUE-dependent, which is deliberate and documented rather than drift.</para>
    /// <para>⚠ SCALE IS WHY THIS ARM IS RESTRICTED TO A SCALE-0 BASE. A scale-<i>s</i> base to the <i>n</i> has
    /// scale <i>s·n</i>, so <c>1.5 ** 30</c> needs ~36 significant digits before a receiver is even considered —
    /// there is no compile-time scale to give the result. A scale-0 base raised to an integer is scale 0 whatever
    /// the exponent, so the result scale is known without knowing the exponent's value. The fractional-base case
    /// keeps the approximation arm.</para>
    /// <para>The overflow fallback quantizes through <see cref="FromDouble"/> rather than casting, so an
    /// out-of-range magnitude SATURATES and stays above the receiver's capacity check instead of wrapping — the
    /// PB13 mechanism, reused rather than re-derived.</para>
    /// </remarks>
    /// <param name="scale">The fraction digits the result is returned at. ⛔ THIS PARAMETER IS WHY THE ARM IS
    /// CORRECT FOR A NEGATIVE EXPONENT, and its absence was a regression caught by probing rather than reasoning:
    /// a first cut returned the exact integer at scale 0 unconditionally, so <c>COMPUTE R = 2 ** -2</c> into a
    /// <c>PIC S9(9)V9(4)</c> gave <b>0.0000</b> where it must give 0.2500 — §8.8.1.2's reciprocal for a negative
    /// exponent is not an integer, and forcing an integer carrier onto it truncates the whole value away. The
    /// exact loop still runs at the true integer scale; only the LANDING uses this scale.</param>
    public static Int128 PowNativeInt(Int128 b, Int128 e, int scale)
    {
        CheckPowRule6((double)b, (double)e);
        if (e >= 0)
        {
            Int128 r = 1, mag = Int128.Abs(b);
            bool fits = true;
            for (Int128 i = 0; i < e && fits; i++)
            {
                if (mag > 1 && Int128.Abs(r) > Int128.MaxValue / mag) { fits = false; break; }
                r *= b;
            }
            // Exact only if the integer result AND its landing at `scale` both stay inside the carrier.
            if (fits && (r == 0 || Int128.Abs(r) <= Int128.MaxValue / Pow10.AsWide(scale)))
                return r * Pow10.AsWide(scale);
        }
        // The documented double approximation: a negative exponent's reciprocal, or an exact result that left
        // the carrier. Quantized through FromDouble rather than cast, so an out-of-range magnitude SATURATES and
        // stays above the receiver's capacity check instead of wrapping (the PB13 mechanism, reused).
        return FromDouble(Math.Pow((double)b, (double)e), scale);
    }

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
    /// (§15.64.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER (argument-1 / argument-2))</c>).
    /// ⛔ The zero-divisor leg calls the SHARED <see cref="ModZeroDivisor"/> rather than carrying its own guard
    /// (fix-queue PB32): this body used to answer <c>b == 0 ? 0 : …</c>, which returned the §15.3 default WITHOUT
    /// ever setting the fatal EC-ARGUMENT-FUNCTION its exact twin sets.</summary>
    public static double ModReal(double a, double b) => b == 0 ? ModZeroDivisor() : a - (b * Math.Floor(a / b));

    /// <summary>§15.77 REM — the TRUNCATED remainder, whose result takes the sign of argument-1
    /// (§15.77.4: <c>argument-1 − (argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2))</c>).
    /// ⚠ Distinct from <see cref="ModReal(double, double)"/> exactly as the two exact bodies are: REM(−7, 3) is
    /// −1 where MOD(−7, 3) is 2. Shares <see cref="RemZeroDivisor"/> with the exact carrier for the same reason.</summary>
    public static double RemReal(double a, double b) => b == 0 ? RemZeroDivisor() : a - (b * Math.Truncate(a / b));

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
