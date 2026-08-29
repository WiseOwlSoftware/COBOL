// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// The STANDARD-DECIMAL intrinsic bodies — every §15.4.1 r1 function evaluated ON <see cref="CobolDec"/>,
/// the SDIDI (§8.8.1.5.2), with per-operation intermediate rounding, landing at the receiver ONCE.
/// </summary>
/// <remarks>
/// <para>⛔ THE DEFECT THIS PARTIAL REMOVES (kb/Work PB56): under a standard arithmetic mode, a Dec or float
/// operand used to LAND to unscaled Int128 at ws = max(receiver scale, 6) with truncation BEFORE the exact
/// body saw it, so <c>FUNCTION SIGN(A − 0)</c> of a 1e-9 operand returned 0 where §15.81.4 r1a and
/// §15.4.1 r1 ("the returned value shall equal the value of the equivalent arithmetic expression") require
/// +1. Any fixed working scale is the same defect at a smaller epsilon, so the fix is structural: the EAE
/// evaluates here on the SDIDI carrier and no argument is ever quantized. The dispatch is
/// <c>IntrinsicRenderer.RenderNum</c>'s standard-mode arm; an all-fixed-point argument list stays on the
/// exact Int128 family (documented equivalence — <c>COBOLNET_NUMERIC_DESIGN.md</c> D3).</para>
/// <para>The four financial/statistical functions (ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION)
/// were staged LOUD (COBOLNET0899) "until CobolDec evaluations land" — these are those evaluations, so the
/// stage is removed in the same change set. Their EAEs are Add/Sub/Mul/Div/Pow chains, all existing
/// <see cref="CobolDec"/> operations; STANDARD-DEVIATION's square root is a prose approximation
/// (§15.4.1 last ¶ — no EAE), computed in binary64 and converted in per §8.8.1.5.1, exactly like the
/// SQRT/trig/log family's own standard-mode results.</para>
/// <para>Tie and selection rules are the exact family's: MAX/MIN return the LEFTMOST extreme argument
/// (§15.59.4 r2 / §15.63.4 r2), ORD-MAX/ORD-MIN the first extreme ordinal (§15.71.4 r2 / §15.72.4 r2),
/// via strict <see cref="CobolDec.Compare"/> — value comparison needs no scale alignment on this carrier.
/// MOD's zero divisor funnels through the ONE §15.64.3 r2 raise site (<see cref="ModZeroDivisor"/>), the
/// PB32 one-raise-site-per-rule discipline.</para>
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>§15.81 SIGN over the SDIDI — the sign of the significand IS the sign of the value.</summary>
    public static long SignDec(CobolDec a) => a.Sig == 0 ? 0 : a.Sig < 0 ? -1 : 1;

    /// <summary>§15.7 ABS — |argument|, exponent untouched.</summary>
    public static CobolDec AbsDec(CobolDec a) => a.Sig < 0 ? new CobolDec(-a.Sig, a.Exp) : a;

    /// <summary>§15.44 INTEGER — the greatest integer not greater than the argument (floor).</summary>
    public static CobolDec FloorDec(CobolDec a)
    {
        Int128 t = a.ToUnscaled(0, CobolRounding.Truncation);
        // Truncation is toward zero; a negative value with a dropped nonzero fraction floors one lower.
        if (a.Sig < 0 && CobolDec.Compare(CobolDec.From(t, 0), a) != 0) t -= 1;
        return CobolDec.From(t, 0);
    }

    /// <summary>§15.49 INTEGER-PART — truncation toward zero.</summary>
    public static CobolDec TruncDec(CobolDec a) => CobolDec.From(a.ToUnscaled(0, CobolRounding.Truncation), 0);

    /// <summary>§15.42 FRACTION-PART — argument − INTEGER-PART(argument), exact (aligned subtraction).</summary>
    public static CobolDec FractionPartDec(CobolRounding mode, CobolDec a) => CobolDec.Sub(a, TruncDec(a), mode);

    /// <summary>§15.64 MOD — argument-1 − argument-2 × INTEGER(argument-1 / argument-2) (§15.64.4 r1's EAE).
    /// A zero divisor violates §15.64.3 r2 → the one raise site shared with the exact carrier.</summary>
    public static CobolDec ModDec(CobolRounding mode, CobolDec a, CobolDec b)
    {
        if (b.Sig == 0) return CobolDec.From(ModZeroDivisor(), 0);
        // Two EXACT integers within the Int128 carrier take the exact remainder (kb/Work PB69): a native integer
        // power of up to 38 digits arrives here as an exact SDIDI (CobolDec.From keeps the significand), and the
        // SDIDI equivalent-arithmetic-expression rounds its 34-digit products — the exact remainder IS the value
        // §15.64.1 defines ("argument-1 modulo argument-2"), and for every operand pair a program can write it is
        // the §15.64.4 EAE's value as well.
        if (ExactIntegers(a, b, out Int128 ia, out Int128 ib)) return CobolDec.From(ModScaled(ia, ib), 0);
        return CobolDec.Sub(a, CobolDec.Mul(b, FloorDec(CobolDec.Div(a, b, mode)), mode), mode);
    }

    /// <summary>§15.77 REM — argument-1 − argument-2 × INTEGER-PART(argument-1 / argument-2).</summary>
    public static CobolDec RemDec(CobolRounding mode, CobolDec a, CobolDec b)
    {
        if (b.Sig == 0) return CobolDec.From(ModZeroDivisor(), 0);
        if (ExactIntegers(a, b, out Int128 ia, out Int128 ib)) return CobolDec.From(RemScaled(ia, ib), 0);   // kb/Work PB69
        return CobolDec.Sub(a, CobolDec.Mul(b, TruncDec(CobolDec.Div(a, b, mode)), mode), mode);
    }

    /// <summary>Both SDIDIs are integers whose values fit the Int128 carrier — the exact-remainder precondition.</summary>
    private static bool ExactIntegers(CobolDec a, CobolDec b, out Int128 ia, out Int128 ib)
    {
        ia = ib = 0;
        return TryExactInteger(a, out ia) && TryExactInteger(b, out ib);
    }

    private static bool TryExactInteger(CobolDec d, out Int128 v)
    {
        v = 0;
        if (d.Exp < 0)
        {
            // A negative exponent may still be an integer value (…000 significand): reduce it.
            Int128 sig = d.Sig; int exp = d.Exp;
            while (exp < 0 && sig % 10 == 0) { sig /= 10; exp++; }
            if (exp < 0) return false;
            d = new CobolDec(sig, exp);
        }
        if (d.Exp > 38) return false;
        Int128 pow = Pow10.AsWide(d.Exp);
        if (d.Sig != 0 && Int128.Abs(d.Sig) > Int128.MaxValue / pow) return false;
        v = d.Sig * pow;
        return true;
    }

    /// <summary>§15.59 MAX — the leftmost argument with the greatest value (§15.59.4 r1/r2).</summary>
    public static CobolDec MaxDec(params CobolDec[] xs)
    {
        CobolDec best = xs[0];
        for (int i = 1; i < xs.Length; i++)
            if (CobolDec.Compare(xs[i], best) > 0) best = xs[i];
        return best;
    }

    /// <summary>§15.63 MIN — the leftmost argument with the least value (§15.63.4 r1/r2).</summary>
    public static CobolDec MinDec(params CobolDec[] xs)
    {
        CobolDec best = xs[0];
        for (int i = 1; i < xs.Length; i++)
            if (CobolDec.Compare(xs[i], best) < 0) best = xs[i];
        return best;
    }

    /// <summary>§15.71 ORD-MAX — the 1-based ordinal of the first greatest argument (§15.71.4 r1/r2).</summary>
    public static long OrdMaxDec(params CobolDec[] xs)
    {
        int best = 0;
        for (int i = 1; i < xs.Length; i++)
            if (CobolDec.Compare(xs[i], xs[best]) > 0) best = i;
        return best + 1;
    }

    /// <summary>§15.72 ORD-MIN — the 1-based ordinal of the first least argument (§15.72.4 r1/r2).</summary>
    public static long OrdMinDec(params CobolDec[] xs)
    {
        int best = 0;
        for (int i = 1; i < xs.Length; i++)
            if (CobolDec.Compare(xs[i], xs[best]) < 0) best = i;
        return best + 1;
    }

    /// <summary>§15.88 SUM — the §15.88.4 r1 addition chain, per-op intermediate rounding.</summary>
    public static CobolDec SumDec(CobolRounding mode, params CobolDec[] xs)
    {
        CobolDec s = xs[0];
        for (int i = 1; i < xs.Length; i++) s = CobolDec.Add(s, xs[i], mode);
        return s;
    }

    /// <summary>§15.76 RANGE — MAX − MIN (§15.76.4 r1), exact-aligned subtraction.</summary>
    public static CobolDec RangeDec(CobolRounding mode, params CobolDec[] xs) =>
        CobolDec.Sub(MaxDec(xs), MinDec(xs), mode);

    /// <summary>§15.60 MEAN — Σ / n (§15.60.4 r1), the one division in SDIDI form.</summary>
    public static CobolDec MeanDec(CobolRounding mode, params CobolDec[] xs) =>
        CobolDec.Div(SumDec(mode, xs), CobolDec.From(xs.Length, 0), mode);

    /// <summary>§15.62 MIDRANGE — (MAX + MIN) / 2 (§15.62.4 r1).</summary>
    public static CobolDec MidrangeDec(CobolRounding mode, params CobolDec[] xs) =>
        CobolDec.Div(CobolDec.Add(MaxDec(xs), MinDec(xs), mode), CobolDec.From(2, 0), mode);

    /// <summary>§15.61 MEDIAN — the middle value in sorted order, or the mean of the two middles (§15.61.4).</summary>
    public static CobolDec MedianDec(CobolRounding mode, params CobolDec[] xs)
    {
        var sorted = (CobolDec[])xs.Clone();
        Array.Sort(sorted, CobolDec.Compare);
        int n = sorted.Length;
        return (n & 1) == 1
            ? sorted[n / 2]
            : CobolDec.Div(CobolDec.Add(sorted[n / 2 - 1], sorted[n / 2], mode), CobolDec.From(2, 0), mode);
    }

    /// <summary>§15.97 VARIANCE — Σ(xᵢ − mean)² / n (§15.97.4 r1's EAE over MEAN).</summary>
    public static CobolDec VarianceDec(CobolRounding mode, params CobolDec[] xs)
    {
        CobolDec mean = MeanDec(mode, xs);
        CobolDec acc = CobolDec.From(0, 0);
        foreach (CobolDec x in xs)
        {
            CobolDec d = CobolDec.Sub(x, mean, mode);
            acc = CobolDec.Add(acc, CobolDec.Mul(d, d, mode), mode);
        }
        return CobolDec.Div(acc, CobolDec.From(xs.Length, 0), mode);
    }

    /// <summary>SQRT under STANDARD-DECIMAL arithmetic (§15.84.4 r2 — the 34-digit correctly-rounded value;
    /// kb/Work PB116). §15.84.3 r2's "zero or positive" is the runtime's value screen here (the bind-time class
    /// screen cannot see values): a negative argument is EC-ARGUMENT-FUNCTION with the §15.3 default 0.</summary>
    public static CobolDec SqrtDec(CobolRounding mode, CobolDec v)
    {
        if (v.Sig < 0)
            return CobolDec.From(Exceptions.ExceptionState.ArgumentError(
                "SQRT argument-1 shall be zero or positive (ISO §15.84.3 rule 2)"), 0);
        return CobolDec.Sqrt(v, mode);
    }

    /// <summary>§15.85 STANDARD-DEVIATION — the square root of the §15.97 variance. The root itself is a
    /// prose approximation (§15.4.1 last ¶ — no equivalent arithmetic expression), computed in binary64 and
    /// converted in per §8.8.1.5.1, the same channel the SQRT/trig/log family's standard-mode results use.</summary>
    public static CobolDec StdDevDec(CobolRounding mode, params CobolDec[] xs) =>
        CobolDec.Sqrt(VarianceDec(mode, xs), mode);   // §15.86.4 r1's EAE = SQRT(VARIANCE), evaluated in
        // SDIDI form end to end (kb/Work PB116 — it detoured through Math.Sqrt in binary64, ~16 digits).

    /// <summary>§15.9 ANNUITY — rate = 0 → 1/periods; else rate / (1 − (1 + rate)^(−periods)) (§15.9.4 r1/r2).
    /// Domain per §15.9.3 r2/r3, through the SAME raise site as the double carrier (one site per rule).</summary>
    public static CobolDec AnnuityDec(CobolRounding mode, CobolDec rate, long periods)
    {
        if (rate.Sig < 0 || periods <= 0)
            return CobolDec.From(AnnuityDomain(rate.ToDouble(), periods), 0);
        CobolDec one = CobolDec.From(1, 0);
        if (rate.Sig == 0) return CobolDec.Div(one, CobolDec.From(periods, 0), mode);
        CobolDec pow = CobolDec.Pow(CobolDec.Add(one, rate, mode), CobolDec.From(-periods, 0), mode);
        return CobolDec.Div(rate, CobolDec.Sub(one, pow, mode), mode);
    }

    /// <summary>§15.74 PRESENT-VALUE — Σ argument-2ₖ / (1 + argument-1)^k, k = 1…n (§15.74.4 r1).
    /// Domain per §15.74.3 r2 (rate &gt; −1), through the same raise site as the double carrier.</summary>
    public static CobolDec PresentValueDec(CobolRounding mode, CobolDec rate, params CobolDec[] amounts)
    {
        if (CobolDec.Compare(rate, CobolDec.From(-1, 0)) <= 0)
            return CobolDec.From(PresentValueDomain(rate.ToDouble()), 0);
        CobolDec baseFactor = CobolDec.Add(CobolDec.From(1, 0), rate, mode);
        CobolDec acc = CobolDec.From(0, 0);
        for (int k = 0; k < amounts.Length; k++)
            acc = CobolDec.Add(acc, CobolDec.Div(amounts[k], CobolDec.Pow(baseFactor, CobolDec.From(k + 1, 0), mode), mode), mode);
        return acc;
    }
}
