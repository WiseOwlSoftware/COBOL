// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The STANDARD-DECIMAL intermediate data item (SDIDI, ISO §8.8.1.5): an abstract signed decimal floating-point
/// temporary — value = <see cref="Sig"/> × 10^<see cref="Exp"/> — whose results are equivalent to IEC 60559:2020
/// decimal128 (34 significant decimal digits). Every operation computes the EXACT result (a 256-bit scratch for
/// products/quotients) and rounds ONCE to 34 significant digits with the program's INTERMEDIATE ROUNDING mode
/// (§11.9.11: default NEAREST-AWAY-FROM-ZERO; NEAREST-EVEN; TRUNCATION; PROHIBITED ⇒ EC-SIZE-TRUNCATION when
/// inexact — surfaced as <see cref="CobolSizeError"/> until the EC model lands). Fixed-point operands (≤31
/// digits) convert EXACTLY — no rounding on entry. The final transfer into a receiver applies the statement's
/// ROUNDED mode (§14.7 NOTE 1: ROUNDED governs only that transfer).
/// <para>The 256÷128 division uses a simple shift-subtract loop — exact and bounded (≤256 iterations); division
/// is rare enough in COBOL flows that clarity wins until profiling says otherwise (commercial-bar note).</para>
/// </summary>
public readonly record struct CobolDec(Int128 Sig, int Exp)
{
    private static readonly Int128 Limit34 = Pow10.AsWide(34);

    /// <summary>Lift a fixed-point operand (unscaled value + scale) into SDIDI form — exact (≤31 digits always
    /// fits the 34-digit significand, §8.8.1.5.2).</summary>
    public static CobolDec From(Int128 unscaled, int scale) => new(unscaled, -scale);

    /// <summary>Lift an exactly-parsed decimal — significand × 10^<paramref name="exp"/>, the value a NUMVAL-F
    /// argument represents under standard-decimal arithmetic (§15.69.4 r3; fix-queue PB60) — into SDIDI form
    /// through the ONE rounding funnel. A ≤34-digit significand passes exactly (no rounding); the §8.8.1.5.2 r2
    /// range check applies, which <see cref="From"/> may skip only because a fixed-point operand can never leave
    /// the decimal128 range — a 4-digit E-exponent can (10^9999 ⇒ EC-SIZE-OVERFLOW; 10^-9999 rounds onto the
    /// 10^-6176 subnormal quantum under <paramref name="mode"/> and, at zero, EC-SIZE-UNDERFLOW).</summary>
    public static CobolDec FromParsed(Int128 sig, int exp, CobolRounding mode) => Round34(sig, exp, sticky: false, mode);

    /// <summary>FUNCTION E under a standard arithmetic mode — the EXACT §15.27.3 r3 value
    /// (2.718281828459045235360287471352662, the full 34-digit SDIDI significand; kb/Work R18). The compiler
    /// folds FUNCTION E to THIS constant and evaluates EXP's §15.34.4 equivalent arithmetic expression
    /// (FUNCTION E ** argument-1) over it, so the function and its hand-written EAE agree by construction
    /// (§15.4.1 r1).</summary>
    public static readonly CobolDec E = new(Int128.Parse("2718281828459045235360287471352662"), -33);

    /// <summary>FUNCTION PI under a standard arithmetic mode — the EXACT §15.73.3 r3 value
    /// (3.141592653589793238462643383279503; kb/Work R18 — the E sibling, same rule shape).</summary>
    public static readonly CobolDec Pi = new(Int128.Parse("3141592653589793238462643383279503"), -33);

    /// <summary>Lift a FLOATING-POINT operand into SDIDI form — the ISO §8.8.1.5.1 implementor-defined
    /// float→SDIDI conversion: the SHORTEST ROUND-TRIP decimal representation of the IEEE value (.NET "R" —
    /// ≤17 significant digits, so it always fits the 34-digit significand EXACTLY and the conversion itself
    /// never rounds; §8.8.1.5.2 r1's "cannot be expressed exactly" case does not arise). The shortest form
    /// makes decimal-clean float values convert to their decimal identity (a COMP-2 holding 0.1 becomes the
    /// SDIDI 0.1, not 0.1000000000000000055…). An infinite operand exceeds the decimal128 range
    /// (EC-SIZE-OVERFLOW, §8.8.1.5.2 r2); a NaN operand is the IEC 60559 'invalid operation' state
    /// (EC-DATA-INCOMPATIBLE, §8.8.1.5.1).</summary>
    public static CobolDec FromDouble(double d)
    {
        if (double.IsNaN(d))
            throw new CobolSizeError("NaN floating-point operand in standard-decimal arithmetic (ISO §8.8.1.5.1 — "
                + "the IEC 60559 invalid-operation state)", "EC-DATA-INCOMPATIBLE");
        if (double.IsInfinity(d))
            throw new CobolSizeError("infinite floating-point operand exceeds the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
        if (d == 0) return new CobolDec(0, 0);
        // "R" = the shortest decimal string that round-trips the double: [-]digits[.digits][E±dd]. Parsed
        // directly to (significand, power-of-ten) — no decimal intermediary (decimal is 28-digit/limited-range).
        string s = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        bool neg = s[0] == '-';
        int i = neg ? 1 : 0;
        Int128 sig = 0;
        int frac = 0, exp10 = 0;
        bool inFrac = false;
        for (; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '.') { inFrac = true; continue; }
            if (c is 'E' or 'e')
            {
                exp10 = int.Parse(s[(i + 1)..], System.Globalization.CultureInfo.InvariantCulture);
                break;
            }
            sig = sig * 10 + (c - '0');
            if (inFrac) frac++;
        }
        return new CobolDec(neg ? -sig : sig, exp10 - frac);
    }

    /// <summary>The multiplicative identity (the §8.8.1.5.4 r1/r3 constant 1).</summary>
    private static readonly CobolDec One = new(1, 0);

    /// <summary>Exponentiation in standard-decimal arithmetic (ISO §8.8.1.5.4). An INTEGER exponent evaluates by
    /// binary square-and-multiply over <see cref="Mul"/> — for exponents 1–4 this performs exactly the r2a–r2d
    /// equivalent expressions (SDIDI multiplication is commutative with a single per-operation rounding, so
    /// b×(b×b) ≡ (b×b)×b digit-for-digit), and for larger integers it is the r2e implementor-defined equivalent
    /// expression whose every multiplication/division follows the §8.8.1.5.3 IEC 60559 rules; a negative integer
    /// exponent is r3's 1/(b ** |e|) via <see cref="Div"/>. A NON-integer exponent (positive base only —
    /// §8.8.1.2 r6c) is the r2e implementor-defined approximation: IEEE-double pow, converted through the
    /// <see cref="FromDouble"/> operand conversion (§8.8.1.5.2 r1). EC-SIZE-EXPONENTIATION legs: 0 ** 0 (r4),
    /// zero base with a non-positive exponent (§8.8.1.2 r6a), and a negative base with a non-integer exponent
    /// (§8.8.1.2 r6c). Every step is range-checked at the decimal128 bounds (§8.8.1.5.2 r2, via the ONE
    /// <see cref="Round34Wide"/> clamp).</summary>
    public static CobolDec Pow(CobolDec b, CobolDec e, CobolRounding mode)
    {
        bool eInt = TryIntegerValue(e, out long n);
        if (b.Sig == 0)
        {
            // §8.8.1.2 r6a / §8.8.1.5.4 r4: a zero base requires an exponent greater than zero.
            if (e.Sig <= 0)
                throw new CobolSizeError("exponentiation of zero with a non-positive exponent "
                    + "(ISO §8.8.1.2 r6a / §8.8.1.5.4 r4)", "EC-SIZE-EXPONENTIATION");
            return new CobolDec(0, 0);
        }
        if (e.Sig == 0) return One;                              // §8.8.1.5.4 r1: b ** 0 = 1 (b ≠ 0)
        if (b.Sig < 0 && !eInt)
            throw new CobolSizeError("exponentiation of a negative base with a non-integer exponent "
                + "(ISO §8.8.1.2 r6c)", "EC-SIZE-EXPONENTIATION");
        if (eInt)
        {
            // |n| is loop-bounded: past this bound a |base| ≠ 1 is out of the decimal128 range anyway (the
            // adjusted exponent |n·log10|b|| ≥ |n|·(1/34) > 6144), and |base| = 1 resolves by parity.
            // long.MinValue (the TryIntegerValue saturation) has no Math.Abs — saturate to long.MaxValue
            // (identical disposition: it exceeds the loop bound).
            const long LoopBound = 500_000;
            long m = n == long.MinValue ? long.MaxValue : Math.Abs(n);
            if (m > LoopBound)
            {
                if (IsUnitMagnitude(b)) return b.Sig > 0 || m % 2 == 0 ? One : new CobolDec(-1, 0);
                throw new CobolSizeError($"exponentiation result of |exponent| {m} exceeds the decimal128 "
                    + "range (ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
            }
            CobolDec acc = One, sq = b;
            bool first = true;
            while (m > 0)
            {
                if ((m & 1) != 0) { acc = first ? sq : Mul(acc, sq, mode); first = false; }
                m >>= 1;
                if (m > 0) sq = Mul(sq, sq, mode);
            }
            return n < 0 ? Div(One, acc, mode) : acc;            // r3: 1 / (b ** |e|)
        }
        // r2e (non-integer exponent, positive base): implementor-defined — IEEE-double approximation converted
        // through the one float→SDIDI operand conversion.
        return FromDouble(Math.Pow(b.ToDouble(), e.ToDouble()));
    }

    /// <summary>Whether the value is an INTEGER, and its magnitude as a <see cref="long"/> when it fits (a
    /// trailing-zero significand normalizes into the exponent first; a value whose integer form exceeds the
    /// long range reports integer-ness with <paramref name="n"/> saturated — the caller's loop bound rejects
    /// it before use).</summary>
    private static bool TryIntegerValue(CobolDec v, out long n)
    {
        Int128 sig = v.Sig;
        int exp = v.Exp;
        while (exp < 0 && sig % 10 == 0) { sig /= 10; exp++; }   // normalize trailing zeros into the exponent
        if (exp < 0) { n = 0; return false; }
        // Integer: sig × 10^exp. Saturate past long range (the Pow loop bound rejects it).
        Int128 wide = sig;
        for (int i = 0; i < exp && Int128.Abs(wide) <= long.MaxValue; i++) wide *= 10;
        n = Int128.Abs(wide) > long.MaxValue ? (wide < 0 ? long.MinValue : long.MaxValue) : (long)wide;
        return true;
    }

    /// <summary>Whether |value| = 1 (the Pow large-exponent parity shortcut).</summary>
    private static bool IsUnitMagnitude(CobolDec v)
    {
        Int128 sig = Int128.Abs(v.Sig);
        int exp = v.Exp;
        while (exp < 0 && sig % 10 == 0) { sig /= 10; exp++; }
        return sig == 1 && exp == 0;
    }

    public static CobolDec Add(CobolDec a, CobolDec b, CobolRounding mode) => AddSigned(a, b, negateB: false, mode);

    public static CobolDec Sub(CobolDec a, CobolDec b, CobolRounding mode) => AddSigned(a, b, negateB: true, mode);

    private static CobolDec AddSigned(CobolDec a, CobolDec b, bool negateB, CobolRounding mode)
    {
        Int128 bSig = negateB ? -b.Sig : b.Sig;
        if (a.Sig == 0) return Round34(bSig, b.Exp, sticky: false, mode);
        if (bSig == 0) return Round34(a.Sig, a.Exp, sticky: false, mode);

        // Align to the smaller exponent. Shift the higher-exponent significand UP while it fits the wide scratch
        // (38 digits); if the gap is larger, shift the LOWER one DOWN capturing a sticky bit — its dropped digits
        // can only influence the final rounding decision (they are below the result's 34-digit precision).
        (Int128 hiSig, int hiExp, Int128 loSig, int loExp) =
            a.Exp >= b.Exp ? (a.Sig, a.Exp, bSig, b.Exp) : (bSig, b.Exp, a.Sig, a.Exp);
        int gap = hiExp - loExp;
        bool sticky = false;
        int upRoom = 38 - DigitCount(Int128.Abs(hiSig));
        int up = Math.Min(gap, upRoom);
        hiSig *= Pow10.AsWide(up);
        int residual = gap - up;
        if (residual > 0)
        {
            // Down-shift the low operand by the residual, keeping ONE guard digit beyond exactness; the dropped
            // tail folds into sticky.
            (loSig, bool dropped) = ShiftDownSticky(loSig, residual);
            sticky = dropped;
            loExp += residual;
        }
        return Round34(hiSig + loSig, loExp, sticky, mode);
    }

    /// <summary>Multiply: the exact 256-bit product reduces to 34 significant digits (§8.8.1.5).</summary>
    public static CobolDec Mul(CobolDec a, CobolDec b, CobolRounding mode)
    {
        bool negative = (a.Sig < 0) ^ (b.Sig < 0);
        var (hi, lo) = Mul128(UAbs(a.Sig), UAbs(b.Sig));
        return Round34Wide(hi, lo, negative, a.Exp + b.Exp, mode);
    }

    /// <summary>Divide: the dividend pre-scales so the exact quotient carries ≥34 significant digits, the
    /// shift-subtract 256÷128 division yields quotient+remainder, and one rounding lands the SDIDI result.
    /// A zero divisor raises the size error (§14.7.5 case 2 — EC-SIZE-ZERO-DIVIDE territory).</summary>
    public static CobolDec Div(CobolDec a, CobolDec b, CobolRounding mode)
    {
        if (b.Sig == 0) throw new CobolSizeError("divide by zero (standard-decimal)", "EC-SIZE-ZERO-DIVIDE");
        if (a.Sig == 0) return new CobolDec(0, 0);
        bool negative = (a.Sig < 0) ^ (b.Sig < 0);
        UInt128 den = UAbs(b.Sig);

        // Pre-scale the numerator so the integer quotient has 34–36 significant digits.
        int scaleUp = Math.Max(0, 34 + DigitCount(den) - DigitCount(UAbs(a.Sig)) + 1);
        var (hi, lo) = Mul128(UAbs(a.Sig), (UInt128)Pow10.AsWide(Math.Min(scaleUp, 38)));
        var (q, rem) = DivRem256(hi, lo, den);

        // q < 10^37 by construction → fits Int128. Round to 34 digits, folding the division remainder into sticky.
        return Round34Wide(0, q, negative, a.Exp - scaleUp - b.Exp, mode, extraSticky: rem != 0);
    }

    /// <summary>Algebraic comparison (−1/0/+1) — exact: equal orders of magnitude align within the wide range;
    /// different orders decide by magnitude.</summary>
    public static int Compare(CobolDec a, CobolDec b)
    {
        int sa = a.Sig == 0 ? 0 : a.Sig < 0 ? -1 : 1;
        int sb = b.Sig == 0 ? 0 : b.Sig < 0 ? -1 : 1;
        if (sa != sb) return sa.CompareTo(sb);
        if (sa == 0) return 0;
        int oa = DigitCount(Int128.Abs(a.Sig)) + a.Exp;   // order of magnitude (digits left of 10^0)
        int ob = DigitCount(Int128.Abs(b.Sig)) + b.Exp;
        if (oa != ob) return sa > 0 ? oa.CompareTo(ob) : ob.CompareTo(oa);
        // Same order ⇒ aligning to the smaller exponent lands both within 34+|order-gap=0| ≤ 38 digits.
        int e = Math.Min(a.Exp, b.Exp);
        Int128 av = a.Sig * Pow10.AsWide(a.Exp - e), bv = b.Sig * Pow10.AsWide(b.Exp - e);
        return av.CompareTo(bv);
    }

    /// <summary>The value as an unscaled integer at <paramref name="scale"/> fraction digits, rounded with the
    /// RECEIVER's mode (the §14.7 final transfer; feeds the normal store/capacity pipeline).</summary>
    public Int128 ToUnscaled(int scale, CobolRounding mode)
    {
        int shift = Exp + scale;
        if (Sig == 0) return 0;
        if (shift >= 0)
        {
            // Widening: keep only digits a ≤38-digit store could ever use; the store's own capacity rules apply.
            Int128 sig = Sig;
            if (DigitCount(Int128.Abs(sig)) + shift > 38) sig %= Pow10.AsWide(Math.Max(0, 38 - shift));
            if (sig == 0) return 0;                            // a far-out-of-range value keeps no store digits
            return sig * Pow10.AsWide(shift);
        }
        var (q, rem, den) = DivRemPow10(Sig, -shift);
        return RoundFromRemainder(q, rem, den, sticky: false, mode);
    }

    /// <summary>The value as a <see cref="double"/> (the float-context bridge, e.g. exponentiation).</summary>
    public double ToDouble() => (double)Sig * Math.Pow(10, Exp);

    /// <summary>The text image of an SDIDI intermediate used as an intrinsic function's returned value in a
    /// string context (DA2). An SDIDI carries its own exponent, so the fixed-point scale is <c>-Exp</c>; routing
    /// through <see cref="CobolNum.FormatFunctionText"/> rather than formatting here keeps ONE rendering rule for
    /// a function result, whichever arithmetic mode produced it (§8.8.1.5 vs native, ISO §15.4.1).</summary>
    public string ToFunctionText(bool deSign = false) => CobolNum.FormatFunctionText(Sig, -Exp, deSign);

    // ── 34-digit rounding core ───────────────────────────────────────────────────────────────────────────────

    private static CobolDec Round34(Int128 sig, int exp, bool sticky, CobolRounding mode)
    {
        bool negative = sig < 0;
        UInt128 mag = UAbs(sig);
        return Round34Wide(0, mag, negative, exp, mode, extraSticky: sticky);
    }

    /// <summary>Reduce a 256-bit magnitude (<paramref name="hi"/>:<paramref name="lo"/>) to a ≤34-digit SDIDI
    /// significand: divide by 10 until in range, capturing the LAST dropped digit (the round digit) and whether
    /// any earlier dropped digit was nonzero (sticky); then apply the INTERMEDIATE ROUNDING mode (§11.9.11 —
    /// PROHIBITED ⇒ size error when anything was dropped).</summary>
    private static CobolDec Round34Wide(UInt128 hi, UInt128 lo, bool negative, int exp, CobolRounding mode,
        bool extraSticky = false)
    {
        bool sticky = extraSticky;
        int roundDigit = 0;
        while (hi != 0 || lo >= (UInt128)Limit34)
        {
            sticky |= roundDigit != 0;
            (hi, lo, roundDigit) = DivRem10_256(hi, lo);
            exp++;
        }
        Int128 sig = (Int128)lo;
        bool inexact = roundDigit != 0 || sticky;
        if (inexact)
        {
            switch (mode)
            {
                case CobolRounding.Prohibited:
                    // §11.9.11: PROHIBITED + not exactly representable ⇒ EC-SIZE-TRUNCATION, results undefined.
                    throw new CobolSizeError("INTERMEDIATE ROUNDING IS PROHIBITED: inexact standard-decimal intermediate");
                case CobolRounding.Truncation:
                    break;
                case CobolRounding.NearestEven:
                    if (roundDigit > 5 || (roundDigit == 5 && (sticky || sig % 2 != 0))) sig++;
                    break;
                default:   // NearestAwayFromZero — the §11.9.11 r3a default
                    if (roundDigit >= 5) sig++;
                    break;
            }
            if (sig == (Int128)Limit34) { sig /= 10; exp++; }   // 999…9 rounded up → 100…0 × 10
        }
        return Clamp(negative ? -sig : sig, exp, mode);
    }

    // decimal128 range bounds (ISO §8.8.1.5.2 NOTE 2): largest |value| 9.999…9E+6144 (34 nines), smallest
    // positive nonzero (subnormal) 1.0E−6176.
    private const int MaxAdjustedExp = 6144;
    private const int MinExp = -6176;

    /// <summary>The §8.8.1.5.2 r2 decimal128 range check, applied by the ONE rounding funnel to every operation
    /// result: a value whose adjusted exponent exceeds +6144 raises the size error condition with
    /// EC-SIZE-OVERFLOW; a value below the smallest subnormal quantum (10^−6176) re-rounds onto that quantum
    /// (the IEC 60559 subnormal range) under the INTERMEDIATE ROUNDING mode — a nonzero value that rounds to
    /// zero there is too small to be contained and raises EC-SIZE-UNDERFLOW.</summary>
    private static CobolDec Clamp(Int128 sig, int exp, CobolRounding mode)
    {
        if (sig == 0) return new CobolDec(0, 0);
        if (DigitCount(Int128.Abs(sig)) + exp - 1 > MaxAdjustedExp)
            throw new CobolSizeError("standard-decimal intermediate exceeds the decimal128 range "
                + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-OVERFLOW");
        if (exp < MinExp)
        {
            // Re-round onto the 10^−6176 quantum (drop exp − MinExp digits with the true remainder).
            var (q, rem, den) = DivRemPow10(sig, MinExp - exp);
            Int128 r = RoundFromRemainder(q, rem, den, sticky: false, mode);
            if (r == 0)
                throw new CobolSizeError("standard-decimal intermediate is below the decimal128 range "
                    + "(ISO §8.8.1.5.2 r2)", "EC-SIZE-UNDERFLOW");
            return new CobolDec(r, MinExp);
        }
        return new CobolDec(sig, exp);
    }

    private static Int128 RoundFromRemainder(Int128 q, Int128 rem, Int128 den, bool sticky, CobolRounding mode)
    {
        if (rem == 0 && !sticky) return q;
        Int128 absRem2 = Int128.Abs(rem) * 2;
        int sign = q < 0 || rem < 0 ? -1 : 1;
        return mode switch
        {
            CobolRounding.Prohibited => throw new CobolSizeError("PROHIBITED rounding on an inexact transfer"),
            CobolRounding.Truncation => q,
            CobolRounding.AwayFromZero => q + sign,
            CobolRounding.TowardGreater => sign > 0 ? q + 1 : q,
            CobolRounding.TowardLesser => sign < 0 ? q - 1 : q,
            CobolRounding.NearestEven => absRem2 > den || (absRem2 == den && !sticky && q % 2 != 0) ? q + sign : q,
            CobolRounding.NearestTowardZero => absRem2 > den || (absRem2 == den && sticky) ? q + sign : q,
            _ => absRem2 >= den ? q + sign : q,   // NearestAwayFromZero
        };
    }

    // ── wide scratch primitives (256-bit as UInt128 hi:lo) ──────────────────────────────────────────────────

    private static (UInt128 Hi, UInt128 Lo) Mul128(UInt128 a, UInt128 b)
    {
        // Schoolbook over 64-bit limbs via Math.BigMul.
        ulong a0 = (ulong)a, a1 = (ulong)(a >> 64);
        ulong b0 = (ulong)b, b1 = (ulong)(b >> 64);
        UInt128 p00 = (UInt128)Math.BigMul(a0, b0, out ulong p00lo) << 64 | p00lo;
        // p00 = a0*b0 (exact 128); cross terms shift 64; top term shifts 128.
        UInt128 cross1 = (UInt128)a0 * b1;
        UInt128 cross2 = (UInt128)a1 * b0;
        UInt128 top = (UInt128)a1 * b1;

        UInt128 lo = p00;
        UInt128 hi = top;
        UInt128 mid = cross1 + cross2;
        bool midCarry = mid < cross1;                     // 129th bit of the cross sum
        UInt128 midLoPart = mid << 64;
        lo += midLoPart;
        if (lo < midLoPart) hi += 1;
        hi += (mid >> 64) + (midCarry ? (UInt128)1 << 64 : 0);
        return (hi, lo);
    }

    private static (UInt128 Hi, UInt128 Lo, int Digit) DivRem10_256(UInt128 hi, UInt128 lo)
    {
        UInt128 qHi = hi / 10;
        UInt128 rHi = hi % 10;
        // lo with the carried remainder: process as two 64-bit limbs to stay in UInt128 range.
        UInt128 cur1 = (rHi << 64) | (lo >> 64);
        UInt128 q1 = cur1 / 10, r1 = cur1 % 10;
        UInt128 cur0 = (r1 << 64) | (ulong)lo;
        UInt128 q0 = cur0 / 10, r0 = cur0 % 10;
        return (qHi, (q1 << 64) | q0, (int)r0);
    }

    /// <summary>Exact 256 ÷ 128 by binary shift-subtract (≤256 iterations) — clarity over speed until profiled.</summary>
    private static (UInt128 Quotient, UInt128 Remainder) DivRem256(UInt128 hi, UInt128 lo, UInt128 den)
    {
        if (hi == 0) return (lo / den, lo % den);
        UInt128 q = 0, rem = 0;
        for (int i = 255; i >= 0; i--)
        {
            rem <<= 1;
            UInt128 word = i >= 128 ? hi : lo;
            if (((word >> (i & 127)) & 1) != 0) rem |= 1;
            if (rem >= den)
            {
                rem -= den;
                if (i < 128) q |= (UInt128)1 << i;
                // a set quotient bit at i ≥ 128 cannot occur: the caller bounds the quotient below 2^128
            }
        }
        return (q, rem);
    }

    /// <summary>Divide by 10^<paramref name="n"/> keeping the true remainder for the rounding decision. Past the
    /// Int128 carrier (<paramref name="n"/> &gt; 38) the quotient is 0 and the value is a NONZERO remainder that is
    /// strictly BELOW HALF a unit — the marker is <c>(0, ±1, 4)</c>, i.e. rem/den = ¼ carrying the value's sign.
    /// ⛔ It was <c>(0, 1, 2)</c> — EXACTLY HALF — so <see cref="RoundFromRemainder"/>'s NEAREST arms treated a
    /// value 10⁻⁴⁴ units below the target scale as a tie and lifted it to one unit: under STANDARD-DECIMAL
    /// <c>COMPUTE R9 ROUNDED = 10 ** -20</c> stored 0.000000001 into <c>V9(9)</c> (§14.7.4.3 r4 — "the nearest value
    /// that can be represented"; a tie is "two such values equally near", which this is not), and the unsigned
    /// marker turned AWAY-FROM-ZERO / TOWARD-GREATER of a NEGATIVE value toward +∞. The 34-digit significand
    /// makes the shape common (1/10²⁰ is 10³³×10⁻⁵³, 44 places below scale 9); kb/Work PB76.</summary>
    private static (Int128 Q, Int128 Rem, Int128 Den) DivRemPow10(Int128 v, int n)
    {
        Int128 den = Pow10.AsWide(Math.Min(n, 38));
        if (n > 38) return (0, v == 0 ? 0 : v < 0 ? -1 : 1, 4);   // far below precision: below-half inexact marker
        return (v / den, v % den, den);
    }

    private static UInt128 UAbs(Int128 v) => v < 0 ? (UInt128)(-v) : (UInt128)v;

    private static int DigitCount(Int128 mag)
    {
        int n = 1;
        while (mag >= 10) { mag /= 10; n++; }
        return n;
    }

    private static int DigitCount(UInt128 mag)
    {
        int n = 1;
        while (mag >= 10) { mag /= 10; n++; }
        return n;
    }

    private static (Int128 Sig, bool Sticky) ShiftDownSticky(Int128 sig, int n)
    {
        if (n > 38) return (0, sig != 0);
        Int128 den = Pow10.AsWide(n);
        return (sig / den, sig % den != 0);
    }

}
