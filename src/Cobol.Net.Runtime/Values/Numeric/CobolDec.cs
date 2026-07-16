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
            return sig * Pow10.AsWide(shift);
        }
        var (q, rem, den) = DivRemPow10(Sig, -shift);
        return RoundFromRemainder(q, rem, den, sticky: false, mode);
    }

    /// <summary>The value as a <see cref="double"/> (the float-context bridge, e.g. exponentiation).</summary>
    public double ToDouble() => (double)Sig * Math.Pow(10, Exp);

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
        return new CobolDec(negative ? -sig : sig, exp);
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

    private static (Int128 Q, Int128 Rem, Int128 Den) DivRemPow10(Int128 v, int n)
    {
        Int128 den = Pow10.AsWide(Math.Min(n, 38));
        if (n > 38) return (0, v == 0 ? 0 : 1, 2);   // far below precision: quotient 0, inexact marker
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
