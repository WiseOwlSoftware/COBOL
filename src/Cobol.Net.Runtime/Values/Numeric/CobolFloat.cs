// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The floating-point (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) value helpers (numeric design D16). A float
/// elementary item holds a native IEEE <see cref="float"/>/<see cref="double"/>; the arithmetic pipeline evaluates
/// any float-bearing expression in binary64. This class is the boundary between that native-double world and the
/// scaled-integer substrate (<see cref="CobolNum"/>): <see cref="ToScaled"/> lands a double into an unscaled
/// <see cref="Int128"/> at a receiver's scale (so the existing store funnel applies ROUNDED + SIZE ERROR), and
/// <see cref="Display(double)"/> renders the algebraic value for DISPLAY (ISO §14.9.11 GR1 — implementor-defined).
/// </summary>
public static class CobolFloat
{
    /// <summary>DISPLAY of a float item (§14.9.11 GR1, implementor-defined): the .NET shortest round-trippable
    /// decimal in the invariant culture (a leading '-' only when negative; E-notation for extreme magnitudes, as
    /// .NET produces). The ONE float→string path — never a bare <c>.ToString()</c>.</summary>
    public static string Display(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc cref="Display(float)"/>
    public static string Display(double v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>⛔ THE ONE scaled-value→double conversion (kb/Work PB115): the CORRECTLY-ROUNDED double of the
    /// algebraic value <c>unscaled × 10^(−scale)</c>. Two paths, both single-rounded: when the unscaled magnitude
    /// fits 2^53 and |scale| ≤ 22, both operands are EXACT doubles and one IEEE divide/multiply rounds once; past
    /// either bound, the decimal-string round-trip (<see cref="double.Parse(string)"/> is IEEE correctly rounded
    /// in this runtime). It replaced <c>(double)(x) / 10^scale</c> with an emit-time repeated-multiplication
    /// divisor — exact only through 1e22, so at scale ≥ 23 the divisor sat one ulp low and a LEGAL
    /// <c>ASIN(|x| ≤ 1)</c> argument (§15.10.3 r2) arrived above 1.0, evaluated NaN and drew the §15.3
    /// EC-ARGUMENT-FUNCTION where §15.10.4 r1 requires an approximation of the arcsine; <c>CobolDec.ToDouble</c>'s
    /// <c>(double)Sig * Math.Pow(10, Exp)</c> failed independently at other scales. A NEGATIVE scale (a PICTURE-P
    /// trailing-scaled operand, or an SDIDI with a positive exponent) multiplies: 10^(−scale).</summary>
    public static double ScaledToDouble(Int128 unscaled, int scale)
    {
        if (scale == 0) return (double)unscaled;
        Int128 mag = unscaled < 0 ? -unscaled : unscaled;
        if (mag <= (Int128)(1L << 53) && scale is >= -22 and <= 22)
        {
            double v = (double)(long)unscaled;                 // exact: |value| ≤ 2^53
            return scale > 0 ? v / ExactPow10[scale] : v * ExactPow10[-scale];
        }
        // The slow path: one correctly-rounded decimal parse of "<unscaled>E<-scale>".
        return double.Parse(
            unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale).ToString(CultureInfo.InvariantCulture),
            NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>10^0 … 10^22 — every power of ten a double represents EXACTLY.</summary>
    private static readonly double[] ExactPow10 =
    [
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
        1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
    ];

    /// <summary>The checked read of a standard-float SENDING operand (ISO §14.6.13.2 item 3): return the value, but
    /// when EC-DATA-NOT-FINITE checking is enabled and the content is NaN or ±Infinity, raise the fatal
    /// EC-DATA-NOT-FINITE (via <see cref="ExceptionState.FloatNotFiniteError"/>). Always-emitted at the two float
    /// sending-read chokepoints (the numeric-value read and the string-image read), mirroring the always-emitted
    /// <c>CobolString.RefMod</c>: the fast path is a single <c>IsFinite</c> test then an immediate return
    /// (JIT-inlinable), so a directive-free run pays only that test and is byte-behaviour-identical. The four
    /// exemptions (class condition, sign condition, same-usage MOVE, VALIDATE) are realized as a RAW read at those
    /// sites, so this wrap never appears there.</summary>
    public static double Sending(double v)
    {
        if (!double.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <inheritdoc cref="Sending(double)"/>
    public static float Sending(float v)
    {
        if (!float.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <summary>The checked store of a MOVE algebraic value into a SINGLE-precision float receiver (ISO
    /// §14.9.25.4 GR6 d)4.a): cast to <see cref="float"/> and, when a FINITE source overflows the single-precision
    /// exponent range to ±Infinity and EC-DATA-OVERFLOW checking is enabled, raise the fatal EC-DATA-OVERFLOW (via
    /// <see cref="ExceptionState.FloatOverflowError"/>). The <c>double.IsFinite(src)</c> guard keeps a NaN/±Infinity
    /// source out of overflow (that is EC-DATA-NOT-FINITE at the sending read, or the valid §14.6.8.3 GR1 result under
    /// checking OFF). The test is cast-based — never <c>Math.Abs(src) &gt; float.MaxValue</c>, since a double in
    /// (float.MaxValue, ~3.4028235678e38] rounds to a FINITE <c>float.MaxValue</c>, not ±Infinity.
    /// <para>⛔ THIS OVERLOAD CANNOT SEE A DECIMAL SENDER'S MAGNITUDE — use <see cref="StoreChecked(CobolDec,bool)"/>
    /// for one. A binary64 conversion happens before this is called, so an SDIDI past binary64's range arrives
    /// ALREADY ±Infinity and <c>double.IsFinite(src)</c> is false.</para></summary>
    public static float StoreSingleChecked(double src)
    {
        float r = (float)src;
        if (double.IsFinite(src) && float.IsInfinity(r))
            ExceptionState.FloatOverflowError("a MOVE algebraic value overflows the single-precision float receiver (ISO §14.9.25.4 GR6 d)4.a)");
        return r;
    }

    /// <summary>The checked store of a MOVE algebraic value that is a STANDARD-DECIMAL intermediate into a float
    /// receiver (ISO §14.9.25.4 GR6 d)4.a; kb/Work PB271).
    /// <para>⛔ THE TEST IS ON THE ALGEBRAIC VALUE, WHICH MEANS IT MUST HOLD THE <see cref="CobolDec"/> — after
    /// <see cref="CobolDec.ToDouble"/> the magnitude is gone. An SDIDI reaches ±9.999…E+6144, twenty decades past
    /// binary64 and 6100 past binary32, so <c>MOVE 1.0E+400 TO F</c> collapsed to ±Infinity at the conversion and
    /// BOTH float arms then failed to notice: the double arm was a bare cast on the premise that "a double receiver
    /// cannot overflow from a finite double" (true, but the sender reaching it stopped being a finite double), and
    /// <see cref="StoreSingleChecked(double)"/>'s finite-source guard was already false. GR6 d)4.a requires the
    /// fatal EC-DATA-OVERFLOW here: "If the algebraic value of the sending operand is farther from zero than is
    /// permitted by the usage specifications of the receiving data item, the EC-DATA-OVERFLOW exception condition
    /// is set to exist, and the content of the receiving data item is undefined" (cite.py-verified).</para>
    /// <para>A <see cref="CobolDec"/> has no infinity, so <c>IsInfinity</c> on the CORRECTLY-ROUNDED conversion is
    /// exactly "farther from zero than the usage permits" — a value between the receiver's maximum and the next
    /// representable magnitude rounds back to that maximum and is NOT an overflow, which a
    /// <c>&gt; MaxValue</c> comparison would get wrong.</para>
    /// <para>SCOPE. GR6 d)4.a binds a receiver "described with a standard floating-point usage" — FLOAT-BINARY-32
    /// and FLOAT-BINARY-64 here (§13.18.60.4 GR14/GR15). For COMP-1/COMP-2/FLOAT-SHORT/FLOAT-LONG/FLOAT-EXTENDED
    /// §14.6.8.3 rule 1 instead says "the implementor specifies any exception conditions that might be set to
    /// exist during data conversion" (cite.py-verified), and COBOL.NET's determination is the SAME condition —
    /// one rule for every float receiver, published in CONFORMANCE.md. The single-precision arm has raised it for
    /// COMP-1 since D21, so this makes the family consistent rather than adding a second regime.</para></summary>
    public static double StoreChecked(CobolDec src, bool single)
    {
        double r = src.ToDouble();
        if (double.IsInfinity(r) || (single && float.IsInfinity((float)r)))
            ExceptionState.FloatOverflowError("a MOVE algebraic value is farther from zero than the "
                + (single ? "single" : "double") + "-precision float receiver's usage permits "
                + "(ISO §14.9.25.4 GR6 d)4.a)");
        return r;
    }

    /// <summary>Convert a native double to an UNSCALED <see cref="Int128"/> at <paramref name="scale"/> fraction
    /// digits, rounded per <paramref name="mode"/> — the double→scaled-integer landing for a store INTO a fixed-point
    /// receiver (D16). The result then flows through the existing <c>CobolNum.Store</c>/<c>TryStore</c> funnel (whose
    /// rescale is identity, since we land AT the receiver scale — no double-rounding), which applies the digit
    /// capacity + SIZE ERROR check. NaN → 0 (implementor-defined; the resulting 0 is in range and exact, so the
    /// store commits it silently — NO SIZE ERROR / EC-SIZE is raised for a NaN source). ±Infinity and any magnitude
    /// beyond the wide engine SATURATE to <see cref="Int128.MaxValue"/>/<see cref="Int128.MinValue"/> so that
    /// capacity check fires SIZE ERROR reliably — never a silent-wrong store.
    /// <para>⛔ THIS IS THE CHECKED LANDING (kb/Work PB77) — the form for a store whose capacity check RAISES: an
    /// arithmetic statement under ON SIZE ERROR / EC-SIZE checking (§14.7.5 case 3), and every intermediate consumer
    /// with no capacity check downstream (an alignment, an argument), where a huge sentinel is the loud answer. A
    /// TRUNCATING landing — a MOVE (§14.6.8.2 r4: "truncation on either end"), the no-phrase arithmetic store
    /// (§14.6.13.1.3 item 8 — the documented low-order-digits disposition), INVOKE BY CONTENT — takes
    /// <see cref="ToScaledUnchecked"/> instead: it has no check to see the sentinel, and truncating a sentinel stores
    /// garbage (<c>MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)</c> stored 884105727, the low digits of
    /// <c>Int128.MaxValue</c>). The SDIDI carrier's <c>CobolDec.ToUnscaledChecked</c> / <c>ToUnscaled</c> pair is the
    /// same two-form rule (PB74).</para></summary>
    public static Int128 ToScaled(double v, int scale, CobolRounding mode)
    {
        if (double.IsNaN(v)) return Int128.Zero;
        double scaled = v * Pow10.AsDouble(scale);
        // Int128.MaxValue ≈ 1.7014e38 — saturate at/above it (and ±Inf) before the (Int128) cast, whose behavior
        // is otherwise undefined for an out-of-range double.
        if (scaled >= 1.7014118e38) return Int128.MaxValue;
        if (scaled <= -1.7014118e38) return Int128.MinValue;
        return RoundScaled(scaled, mode);
    }

    /// <summary>The UNCHECKED landing of a binary64 into a fixed-point receiver (kb/Work PB77) — a MOVE (§14.6.8.2
    /// r1/r2/r4: the value converted to fixed point, aligned by decimal point, "zero fill or truncation on either
    /// end"), the no-phrase arithmetic store, INVOKE BY CONTENT. Within the Int128 carrier it is <see cref="ToScaled"/>
    /// exactly (the same binary64 product, the same rounding); beyond it the value's exact decimal expansion keeps
    /// supplying the LOW-ORDER digits (<see cref="LowOrderDigits"/>) — never a saturation sentinel, which has no
    /// capacity check downstream to expose it. A non-finite value (NaN, ±Infinity — EC-DATA-NOT-FINITE at the
    /// sending read under checking, §14.6.13.2 item 3; with checking off the receiving operand's disposition is the
    /// implementor's, §14.6.13.1.3 item 8) lands as ZERO: not a number, no digits — the disposition
    /// <see cref="ToScaled"/> already gave NaN.</summary>
    public static Int128 ToScaledUnchecked(double v, int scale, CobolRounding mode)
    {
        if (!double.IsFinite(v)) return Int128.Zero;
        double scaled = v * Pow10.AsDouble(scale);
        if (scaled > -1.7014118e38 && scaled < 1.7014118e38) return RoundScaled(scaled, mode);
        return LowOrderDigits(v, scale, mode);
    }

    private static readonly BigInteger Pow10Big38 = BigInteger.Pow(10, 38);

    /// <summary>The 38 LOW-ORDER digits, sign kept, of a finite binary64's EXACT value at <paramref name="scale"/>
    /// fraction digits, rounded per <paramref name="mode"/> (kb/Work PB77) — the digits a truncating landing keeps of a
    /// value the Int128 carrier cannot hold (the receiver's own store then keeps ITS low-order digits of these, so the
    /// composition is exact whenever the receiver's digit positions fit under 38 minus the landing's excess scale —
    /// which <c>ReceiverContext.WorkingScale</c>'s cap guarantees). A double is ±m·2^e exactly (m &lt; 2^53), so
    /// v·10^scale = ±m·5^scale·2^(e+scale): an integer when e+scale ≥ 0, otherwise a quotient rounded by the ONE
    /// <c>CobolNum.RoundDiv</c> kernel. Cold path — a magnitude at or past 1.7×10^38 at the landing scale — so the
    /// expansion rides <see cref="BigInteger"/> (as BASECONVERT's digit accumulation does); the engine's hot paths
    /// stay native (<c>CobolNum</c>'s design note). Correct for EVERY finite double, so a determination that lands
    /// the exact expansion inside the carrier as well needs only the caller's carrier test removed.</summary>
    public static Int128 LowOrderDigits(double v, int scale, CobolRounding mode)
    {
        long bits = BitConverter.DoubleToInt64Bits(v);
        bool neg = bits < 0;
        int exp = (int)((bits >> 52) & 0x7FF);
        long man = bits & 0xF_FFFF_FFFF_FFFFL;
        if (exp == 0) exp = 1; else man |= 1L << 52;          // subnormal / normal significand
        exp -= 1075;                                          // v = ±man × 2^exp
        BigInteger scaled = new BigInteger(man) * BigInteger.Pow(5, scale);
        if (neg) scaled = -scaled;
        int e2 = exp + scale;
        scaled = e2 >= 0 ? scaled << e2 : CobolNum.RoundDiv(scaled, BigInteger.One << -e2, mode);
        return (Int128)(scaled % Pow10Big38);                 // sign-preserving remainder — the low-order digits
    }

    /// <summary>The in-carrier rounding of a binary64 product to an <see cref="Int128"/> per a COBOL ROUNDED mode —
    /// shared by <see cref="ToScaled"/> and <see cref="ToScaledUnchecked"/> (kb/Work PB77: the two landings differ
    /// ONLY past the carrier).</summary>
    private static Int128 RoundScaled(double scaled, CobolRounding mode)
    {
        double r = mode switch
        {
            CobolRounding.Truncation        => Math.Truncate(scaled),
            CobolRounding.NearestAwayFromZero => Math.Round(scaled, MidpointRounding.AwayFromZero),
            CobolRounding.AwayFromZero      => scaled < 0 ? Math.Floor(scaled) : Math.Ceiling(scaled),
            CobolRounding.NearestEven       => Math.Round(scaled, MidpointRounding.ToEven),
            // NEAREST-TOWARD-ZERO (§14.9.4 GR6): round to the NEAREST value; break an EXACT tie toward zero. NOT
            // MidpointRounding.ToZero — that is DIRECTED rounding (plain truncation of every value: 2.7→2 wrong).
            CobolRounding.NearestTowardZero => NearestTowardZero(scaled),
            CobolRounding.TowardGreater     => Math.Ceiling(scaled),
            CobolRounding.TowardLesser      => Math.Floor(scaled),
            // PROHIBITED: the value is landed truncated here; the emitter gates the STORE with InexactAtScale so an
            // inexact float→fixed transfer raises SIZE ERROR + leaves the receiver unchanged (§14.7.5 r7) before
            // this lands — see CobolFloat.InexactAtScale + the size-error branch in CSharpEmitter.StoreArith.
            CobolRounding.Prohibited        => Math.Truncate(scaled),
            _                               => Math.Truncate(scaled),
        };
        return (Int128)r;
    }

    /// <summary>Round <paramref name="x"/> to the NEAREST integer, breaking an EXACT half tie TOWARD ZERO (COBOL
    /// ROUNDED MODE NEAREST-TOWARD-ZERO, §14.9.4 GR6). A fraction &gt; ½ rounds away from zero; &lt; ½ or an exact ½
    /// truncates toward zero. (.NET's <c>MidpointRounding.ToZero</c> is DIRECTED rounding — it truncates every value,
    /// not just ties — so it is wrong for this mode.)</summary>
    private static double NearestTowardZero(double x)
    {
        double t = Math.Truncate(x);
        return Math.Abs(x - t) > 0.5 ? t + Math.Sign(x) : t;
    }

    /// <summary>True when <paramref name="v"/> carries a nonzero fraction beyond <paramref name="scale"/> digits — an
    /// INEXACT float→fixed transfer that ROUNDED MODE PROHIBITED must reject with SIZE ERROR / EC-SIZE-TRUNCATION,
    /// leaving the receiver unchanged (ISO §14.7.5 r7). Judged at the double level (a double's fraction can extend
    /// past any fixed decimal guard count). NaN/±Inf are handled by the store's saturation path, not here.</summary>
    public static bool InexactAtScale(double v, int scale)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        double s = v * Pow10.AsDouble(scale);
        return s != Math.Truncate(s);
    }

}
