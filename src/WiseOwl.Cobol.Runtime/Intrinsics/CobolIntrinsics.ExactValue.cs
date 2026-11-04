// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE NATIVE SDIDI LANE OVER EXACT ARGUMENTS (kb/Work PB4111, PB4568; docs/CONFORMANCE.md DOC-A.1-123): the bodies that evaluate
/// a function under NATIVE arithmetic when its value is returned on the SDIDI — beside an argument the SDIDI carries (a
/// floating-point literal, a power), an argument expression the exact wide lane could not take, a binary64 beside an exact
/// selection — and VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE in every native call. Each takes its arguments as their EXACT
/// values (<see cref="CobolExact"/>), forms the value of the function's equivalent arithmetic expression from them on the 256-bit
/// wide form, and rounds it ONCE into the returned-value carrier; a selection compares the exact values
/// (<see cref="CobolExact.Compare"/>).
/// <para>WHY. Under native arithmetic the method of evaluating "all integer and numeric functions" is the implementor's (ISO/IEC
/// 1989:2023 §8.8.1.3), and §15.4.1 makes the value of a function with an equivalent arithmetic expression "an implementor-defined
/// approximation of the value of that expression". CLAUDE.md rule 1 sends that latitude to GnuCOBOL, which evaluates these
/// functions from the exact argument values (3.2.0: <c>VARIANCE (P, P - 1)</c> of a 39-digit P is 0.25, <c>ORD-MAX (P - 1, P, 2.0E0)</c>
/// is 2, <c>MOD (P, 2 ** 10)</c> is 999). The SDIDI bodies of <c>CobolIntrinsics.Dec.cs</c> took each argument as its 34-digit image
/// and rounded each step — a 34-digit mean is no approximation of a variance at all, since a mean off by δ adds exactly δ² — and
/// stay the STANDARD-DECIMAL bodies, where §15.4.1 1) makes the 34-digit SDIDI evaluation the value.</para>
/// <para>WHAT IS ROUNDED, ONCE. A computed value (a sum, a difference, a remainder, a quotient) is rounded to the SDIDI's 34 digits in
/// the native intermediate rounding mode; a selection returns the content of the selected argument (<see cref="CobolExact.ToDec"/>);
/// an ordinal is exact. Where the arguments cannot be aligned on the wide form, or a step of the work leaves its 77 digits, the body
/// evaluates the SDIDI images with the SDIDI body instead — DOC-A.1-123's determination for an intermediate past 77 digits.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>The position of the greatest (<paramref name="greatest"/>) or least exact value; the leftmost of equal values
    /// (§15.59.4, §15.63.4, §15.71.4 3), §15.72.4), compared by algebraic value (§15.71.4 2): "according to the rules for simple
    /// conditions", §8.8.4.2.4).</summary>
    private static int SelectExact(CobolExact[] xs, bool greatest, string function)
    {
        RequireArguments(xs.Length, function);
        int at = 0;
        for (int i = 1; i < xs.Length; i++)
        {
            int order = CobolExact.Compare(xs[i], xs[at]);
            if (greatest ? order > 0 : order < 0) at = i;
        }
        return at;
    }

    /// <summary>The SDIDI images of <paramref name="xs"/>, for the SDIDI body a body takes past the wide form.</summary>
    private static CobolDec[] Images(CobolExact[] xs) => Array.ConvertAll(xs, x => x.Image);

    /// <summary>ORD-MAX (§15.71.4): the ordinal position of the argument having the greatest value.</summary>
    public static long OrdMaxExact(params CobolExact[] xs) => SelectExact(xs, greatest: true, "ORD-MAX") + 1;

    /// <summary>ORD-MIN (§15.72.4): the ordinal position of the argument having the least value.</summary>
    public static long OrdMinExact(params CobolExact[] xs) => SelectExact(xs, greatest: false, "ORD-MIN") + 1;

    /// <summary>MAX (§15.59.4): the content of the argument having the greatest value.</summary>
    public static CobolDec MaxExact(CobolRounding mode, params CobolExact[] xs) => xs[SelectExact(xs, greatest: true, "MAX")].ToDec(mode);

    /// <summary>MIN (§15.63.4): the content of the argument having the least value.</summary>
    public static CobolDec MinExact(CobolRounding mode, params CobolExact[] xs) => xs[SelectExact(xs, greatest: false, "MIN")].ToDec(mode);

    /// <summary>MEDIAN (§15.61.4): the middle value in sorted order for an odd count, the mean of the two middle values for an even
    /// one.</summary>
    public static CobolDec MedianExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        var sorted = (CobolExact[])xs.Clone();
        Array.Sort(sorted, CobolExact.Compare);
        int mid = sorted.Length / 2;
        return sorted.Length % 2 != 0 ? sorted[mid].ToDec(mode) : HalfSum(sorted[mid - 1], sorted[mid], mode);
    }

    /// <summary>MIDRANGE (§15.62.4): <c>(FUNCTION MAX + FUNCTION MIN) / 2</c>.</summary>
    public static CobolDec MidrangeExact(CobolRounding mode, params CobolExact[] xs) =>
        HalfSum(xs[SelectExact(xs, greatest: true, "MIDRANGE")], xs[SelectExact(xs, greatest: false, "MIDRANGE")], mode);

    /// <summary>(<paramref name="a"/> + <paramref name="b"/>) / 2 at the two values' common scale, rounded once.</summary>
    private static CobolDec HalfSum(CobolExact a, CobolExact b, CobolRounding mode) =>
        CobolExact.TryAlign([a, b], out CobolWide[] v, out int s) && CobolWide.TryAdd(v[0], v[1], out CobolWide sum)
            ? sum.QuotientDec(s, 2, mode)
            : CobolDec.Div(CobolDec.Add(a.Image, b.Image, mode), CobolDec.From(2, 0), mode);

    /// <summary>The exact sum of the aligned values, or false past the wide form.</summary>
    private static bool TrySum(CobolWide[] aligned, out CobolWide sum)
    {
        sum = default;
        foreach (CobolWide x in aligned)
            if (!CobolWide.TryAdd(sum, x, out sum)) return false;
        return true;
    }

    /// <summary>SUM (§15.88.4): the exact sum of the arguments at their common scale, rounded once.</summary>
    public static CobolDec SumExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "SUM");
        return CobolExact.TryAlign(xs, out CobolWide[] v, out int s) && TrySum(v, out CobolWide sum)
            ? sum.ToDecRounded(s, mode)
            : SumDec(mode, Images(xs));
    }

    /// <summary>RANGE (§15.76.4): <c>FUNCTION MAX − FUNCTION MIN</c>, at the two values' common scale, rounded once.</summary>
    public static CobolDec RangeExact(CobolRounding mode, params CobolExact[] xs)
    {
        CobolExact max = xs[SelectExact(xs, greatest: true, "RANGE")], min = xs[SelectExact(xs, greatest: false, "RANGE")];
        return CobolExact.TryAlign([max, min], out CobolWide[] v, out int s) && CobolWide.TryAdd(v[0], CobolWide.Negate(v[1]), out CobolWide range)
            ? range.ToDecRounded(s, mode)
            : CobolDec.Sub(max.Image, min.Image, mode);
    }

    /// <summary>MEAN (§15.60.4 1): <c>(argument-11 + … + argument-1n) / n</c>, the exact sum divided ONCE — the SDIDI quotient every
    /// lane forms (kb/Work PB3860, PB4075; <see cref="MeanWide"/> is the same quotient over a wide sum).</summary>
    public static CobolDec MeanExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEAN");
        return CobolExact.TryAlign(xs, out CobolWide[] v, out int s) && TrySum(v, out CobolWide sum)
            ? sum.QuotientDec(s, xs.Length, mode)
            : MeanDec(mode, Images(xs));
    }

    /// <summary>MOD (§15.64.4): <c>argument-1 − argument-2 × FUNCTION INTEGER (argument-1 / argument-2)</c>, the floored modulus of the
    /// exact values (the renderer passes the §15.3 type-6 integers; <see cref="ModWide"/>), rounded once. A zero divisor is §15.64.3
    /// r2's one raise site (<see cref="ModZeroDivisor"/>), shared with every other carrier's body.</summary>
    public static CobolDec ModExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(ModZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? ModWide(v[0], v[1]).ToDecRounded(s, mode)
            : ModDec(mode, a.Image, b.Image);
    }

    /// <summary>REM (§15.77.4): <c>argument-1 − argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2)</c>, the truncated
    /// remainder of the exact values (<see cref="RemWide"/>), rounded once. A zero divisor is §15.77.3 r2's one raise site
    /// (<see cref="RemZeroDivisor"/>).</summary>
    public static CobolDec RemExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(RemZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? RemWide(v[0], v[1]).ToDecRounded(s, mode)
            : RemDec(mode, a.Image, b.Image);
    }

    /// <summary>⛔ VARIANCE (§15.98.4 1) OF THE EXACT ARGUMENTS, DIVIDED ONCE (kb/Work PB4111): the mean of the squared deviations from
    /// the EXACT mean. With every argument aligned to the common scale s as an integer Xᵢ and shifted by the first, dᵢ = Xᵢ − X₁ (the
    /// variance of a list does not move with it, and the shift makes the differences small exactly where the expression cancels),
    /// Σ(xᵢ − mean)² / n is algebraically <c>(n·Σdᵢ² − (Σdᵢ)²) / (n²·10^(2s))</c>: one numerator and one denominator on the wide form,
    /// divided once (<see cref="CobolWide.TryQuotientDec"/>). The 34-digit mean of <see cref="VarianceDec"/> is no approximation of it
    /// under native arithmetic: a mean off by δ adds exactly δ² (the cross term Σ(xᵢ − m) is 0), so VARIANCE (P, P − 1) of a 39-digit
    /// P answered 10^10 for 0.25, and VARIANCE of 10^30, 10^30 + 1, 10^30 + 1 answered 0.2222223333 for 2/9. Deviations whose squares
    /// leave the 77 digits are the SDIDI body's (their spread then dwarfs the 34-digit mean's error).</summary>
    public static CobolDec VarianceExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §15.3 — at least one argument (kb/Work PB257)
        return TryVariance(xs, mode, out CobolDec variance) ? variance : VarianceDec(mode, Images(xs));
    }

    private static bool TryVariance(CobolExact[] xs, CobolRounding mode, out CobolDec variance)
    {
        variance = default;
        if (!CobolExact.TryAlign(xs, out CobolWide[] v, out int s)) return false;
        CobolWide sum = default, squares = default, n = CobolWide.From(xs.Length);
        foreach (CobolWide x in v)
        {
            if (!CobolWide.TryAdd(x, CobolWide.Negate(v[0]), out CobolWide d)
                || !CobolWide.TryAdd(sum, d, out sum)
                || !CobolWide.TryMul(d, d, out CobolWide square)
                || !CobolWide.TryAdd(squares, square, out squares)) return false;
        }
        return CobolWide.TryMul(n, squares, out CobolWide scaled)
            && CobolWide.TryMul(sum, sum, out CobolWide sumSquared)
            && CobolWide.TryAdd(scaled, CobolWide.Negate(sumSquared), out CobolWide numerator)
            && numerator.TryQuotientDec(2 * s, CobolWide.Mul(n, n), mode, out variance);
    }

    /// <summary>STANDARD-DEVIATION (§15.86.4 1): <c>(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))</c>, the two bodies composed,
    /// so §15.84.4's square root is the ONE SQRT body's (<see cref="SqrtDec"/>). The variance rounded once at 34 digits before the
    /// root is harmless: the square root's condition number is ½.</summary>
    public static CobolDec StdDevExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "STANDARD-DEVIATION");
        return SqrtDec(mode, VarianceExact(mode, xs));
    }

    /// <summary>⛔ PRESENT-VALUE (§15.74.4 1) OF THE EXACT ARGUMENTS, DIVIDED ONCE (kb/Work PB4111): <c>Σ aₖ / (1 + rate)^k</c>, k = 1…n,
    /// with the discount base written as the reduced fraction <c>1 + rate = p / q</c> and the amounts aligned to their common scale t
    /// as integers Aₖ, is <c>Σ Aₖ·q^k·p^(n−k) / (10^t·p^n)</c>: one numerator (Horner, <c>Nₖ = Nₖ₋₁·p + Aₖ·q^k</c>) and one
    /// denominator on the wide form, rounded once. §15.74.3 r2 (rate &gt; −1, so p &gt; 0) is screened on the exact rate through the ONE
    /// raise site (<see cref="PresentValueDomain"/>). The 34-digit term quotients of <see cref="PresentValueDec"/> answered
    /// PRESENT-VALUE (0, P, 1 − P) = 0 for 1 and PRESENT-VALUE (3, 10^33 + 1, −4·10^33) = 0.3 for 0.25. A discount whose powers leave
    /// the wide form (many periods at a rate of many places) is the SDIDI body's, which screens the same rule on the same rate.</summary>
    public static CobolDec PresentValueExact(CobolRounding mode, CobolExact rate, params CobolExact[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        return TryPresentValue(rate, amounts, mode, out CobolDec value)
            ? value
            : PresentValueDec(mode, rate.Image, Images(amounts));
    }

    private static bool TryPresentValue(CobolExact rate, CobolExact[] amounts, CobolRounding mode, out CobolDec value)
    {
        value = default;
        int a = Math.Max(0, rate.Scale);
        if (a > CobolWide.MaxDigits - 1 || !rate.TryAt(a, out CobolWide r)) return false;
        CobolWide q = CobolWide.PowerOfTen(a);
        if (!CobolWide.TryAdd(q, r, out CobolWide p)) return false;
        if (p.Sign <= 0)
        {
            value = CobolDec.From(PresentValueDomain(rate.Image.ToDouble()), 0);
            return true;
        }
        CobolWide common = CobolWide.Gcd(p, q);
        p = CobolWide.DivRem(p, common, out _);
        q = CobolWide.DivRem(q, common, out _);
        if (!CobolExact.TryAlign(amounts, out CobolWide[] v, out int t)) return false;
        CobolWide numerator = default, qPower = CobolWide.From(1);
        foreach (CobolWide amount in v)
        {
            if (!CobolWide.TryMul(qPower, q, out qPower)
                || !CobolWide.TryMul(numerator, p, out CobolWide carried)
                || !CobolWide.TryMul(amount, qPower, out CobolWide term)
                || !CobolWide.TryAdd(carried, term, out numerator)) return false;
        }
        return CobolWide.TryPow(p, amounts.Length, out CobolWide denominator)
            && numerator.TryQuotientDec(t, denominator, mode, out value);
    }
}
