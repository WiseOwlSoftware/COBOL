// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE NATIVE SDIDI LANE OVER EXACT ARGUMENTS (kb/Work PB4111, PB4568; docs/CONFORMANCE.md DOC-A.1-123): the bodies that evaluate
/// a function under NATIVE arithmetic when its value is returned on the SDIDI — beside an argument the SDIDI carries (a
/// floating-point literal, a power), an argument expression the exact wide lane could not take, a binary64 beside an exact
/// selection — and VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE in every native call. Each takes its arguments as their EXACT
/// values (<see cref="CobolExact"/>), forms the value of the function's equivalent arithmetic expression from them on the 256-bit
/// wide form and, where a step leaves it, on the arbitrary-precision form (kb/Work PB4592), and rounds it ONCE into the returned-value
/// carrier; a selection compares the exact values (<see cref="CobolExact.Compare"/>) and returns the selected one, whole.
/// <para>WHY. Under native arithmetic the method of evaluating "all integer and numeric functions" is the implementor's (ISO/IEC
/// 1989:2023 §8.8.1.3), and §15.4.1 makes the value of a function with an equivalent arithmetic expression "an implementor-defined
/// approximation of the value of that expression". CLAUDE.md rule 1 sends that latitude to GnuCOBOL, which evaluates these
/// functions from the exact argument values (3.2.0: <c>VARIANCE (P, P - 1)</c> of a 39-digit P is 0.25, <c>ORD-MAX (P - 1, P, 2.0E0)</c>
/// is 2, <c>MOD (P, 2 ** 10)</c> is 999). The SDIDI bodies of <c>CobolIntrinsics.Dec.cs</c> took each argument as its 34-digit image
/// and rounded each step — a 34-digit mean is no approximation of a variance at all, since a mean off by δ adds exactly δ² — and
/// stay the STANDARD-DECIMAL bodies, where §15.4.1 1) makes the 34-digit SDIDI evaluation the value.</para>
/// <para>WHAT IS ROUNDED, ONCE. A computed value (a sum, a difference, a remainder, a quotient) is rounded to the SDIDI's 34 digits in
/// the native intermediate rounding mode; a selection returns the content of the selected argument itself, every digit of it
/// (§15.59.4 1), §15.63.4 1), §15.61.1: a float item's 41-digit expansion included, kb/Work PB4730); an ordinal is exact. The SUM,
/// RANGE, MEAN, MIDRANGE, MEDIAN, MOD and REM bodies are exact at any size the arbitrary-precision form holds (|value| &lt; 10^6145);
/// VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE take the wide form first and, where their work leaves it, the same identity on the
/// arbitrary-precision form (kb/Work PB4750); none of them evaluates an SDIDI image.</para>
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

    /// <summary>ORD-MAX (§15.71.4): the ordinal position of the argument having the greatest value.</summary>
    public static long OrdMaxExact(params CobolExact[] xs) => SelectExact(xs, greatest: true, "ORD-MAX") + 1;

    /// <summary>ORD-MIN (§15.72.4): the ordinal position of the argument having the least value.</summary>
    public static long OrdMinExact(params CobolExact[] xs) => SelectExact(xs, greatest: false, "ORD-MIN") + 1;

    /// <summary>MAX (§15.59.4): the content of the argument having the greatest value.</summary>
    public static CobolExact MaxExact(params CobolExact[] xs) => xs[SelectExact(xs, greatest: true, "MAX")];

    /// <summary>MIN (§15.63.4): the content of the argument having the least value.</summary>
    public static CobolExact MinExact(params CobolExact[] xs) => xs[SelectExact(xs, greatest: false, "MIN")];

    /// <summary>MEDIAN (§15.61.4): the middle value in sorted order for an odd count, the mean of the two middle values for an even
    /// one, rounded once to the SDIDI's 34 digits.</summary>
    public static CobolExact MedianExact(CobolRounding mode, params CobolExact[] xs)
    {
        CobolExact median = MedianWhole(xs);
        return xs.Length % 2 != 0 ? median : CobolExact.FromDec(median.ToDecRounded(mode));
    }

    /// <summary>MEDIAN (§15.61.4) over arguments of which at least one is binary64 (a float item's content or an engine's approximation),
    /// <paramref name="binary64"/> saying which (a float item's exact expansion is not marked in its <see cref="CobolExact"/>; kb/Work PB4827,
    /// PB4859). An odd count returns the selected argument whole, as its own kind (§15.61.4 1)). An even count is the mean of the two middle
    /// values (§15.61.4 2)), a new value: when a MIDDLE value is binary64 it is evaluated in binary64 (§15.4.1: "an implementor-defined
    /// approximation of the value of that expression"; docs/CONFORMANCE.md DOC-A.1-92b), as <see cref="MedianReal"/> forms it for a count
    /// the binder knows, and lands as the float item it derives from does (<see cref="CobolExact.FromBinary64"/>); when both middles are
    /// fixed-point it is <see cref="MedianExact"/>'s exact mean, a binary64 elsewhere in the list being no operand of it. Equal values keep
    /// their written order, so the middle pair is the one <c>FunctionValueField.Median</c> picks.</summary>
    public static CobolExact MedianBinary64Exact(CobolRounding mode, bool[] binary64, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        if (xs.Length % 2 != 0) return MedianWhole(xs);
        int[] order = [.. Enumerable.Range(0, xs.Length).OrderBy(i => xs[i], Comparer<CobolExact>.Create(CobolExact.Compare))];
        int low = order[xs.Length / 2 - 1], high = order[xs.Length / 2];
        if (!binary64[low] && !binary64[high]) return MedianExact(mode, xs[low], xs[high]);
        // An approximation remembers its double; a float item's content IS its binary64 expansion, which lands exactly in any mode.
        static double Double(CobolExact x) => x.TryApproximation(out double approximation)
            ? approximation
            : FloatResultant.FromExact(x, CobolRounding.NearestEven, single: false);
        return CobolExact.FromBinary64(MedianReal(Double(xs[low]), Double(xs[high])));
    }

    /// <summary>MEDIAN (§15.61.4) EXACTLY, never cut to the SDIDI's 34 digits: the lane of a list that aligns an argument of unknown
    /// bound (kb/Work PB4792, review R1-1), whose value the Int128 body held whole before the alignment could leave it.</summary>
    public static CobolExact MedianWhole(params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEDIAN");
        var sorted = (CobolExact[])xs.Clone();
        Array.Sort(sorted, CobolExact.Compare);
        int mid = sorted.Length / 2;
        return sorted.Length % 2 != 0 ? sorted[mid] : HalfSum(sorted[mid - 1], sorted[mid]);
    }

    /// <summary>MIDRANGE (§15.62.4): <c>(FUNCTION MAX + FUNCTION MIN) / 2</c>, rounded once.</summary>
    public static CobolDec MidrangeExact(CobolRounding mode, params CobolExact[] xs) =>
        MidrangeWhole(xs).ToDecRounded(mode);

    /// <summary>MIDRANGE (§15.62.4) exactly (see <see cref="MedianWhole"/>).</summary>
    public static CobolExact MidrangeWhole(params CobolExact[] xs) =>
        HalfSum(xs[SelectExact(xs, greatest: true, "MIDRANGE")], xs[SelectExact(xs, greatest: false, "MIDRANGE")]);

    /// <summary>(<paramref name="a"/> + <paramref name="b"/>) / 2 exactly: the sum times 5 at one more fraction digit (halving is exact in
    /// decimal), so a caller rounds it once, or not at all.</summary>
    private static CobolExact HalfSum(CobolExact a, CobolExact b) =>
        CobolExact.Mul(CobolExact.Add(a, b), CobolExact.From(5, 1));

    /// <summary>The exact sum of the values at their common scale, on the wide form and, past it, the arbitrary-precision one.</summary>
    private static CobolExact Total(CobolExact[] xs)
    {
        CobolExact sum = xs[0];
        for (int i = 1; i < xs.Length; i++) sum = CobolExact.Add(sum, xs[i]);
        return sum;
    }

    /// <summary>SUM (§15.88.4): the exact sum of the arguments at their common scale, rounded once.</summary>
    public static CobolDec SumExact(CobolRounding mode, params CobolExact[] xs) => SumWhole(xs).ToDecRounded(mode);

    /// <summary>SUM (§15.88.4) exactly (see <see cref="MedianWhole"/>): 10000 and .9999999999999999999999999999999 stored at five places
    /// are 10000.99999, not the 10001.00000 of the sum's 34-digit image.</summary>
    public static CobolExact SumWhole(params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "SUM");
        return Total(xs);
    }

    /// <summary>RANGE (§15.76.4): <c>FUNCTION MAX − FUNCTION MIN</c>, at the two values' common scale, rounded once.</summary>
    public static CobolDec RangeExact(CobolRounding mode, params CobolExact[] xs) => RangeWhole(xs).ToDecRounded(mode);

    /// <summary>RANGE (§15.76.4) exactly (see <see cref="MedianWhole"/>).</summary>
    public static CobolExact RangeWhole(params CobolExact[] xs)
    {
        CobolExact max = xs[SelectExact(xs, greatest: true, "RANGE")], min = xs[SelectExact(xs, greatest: false, "RANGE")];
        return CobolExact.Sub(max, min);
    }

    /// <summary>MEAN (§15.60.4 1): <c>(argument-11 + … + argument-1n) / n</c>, the exact sum divided ONCE — the SDIDI quotient every
    /// lane forms (kb/Work PB3860, PB4075; <see cref="MeanWide"/> is the same quotient over a wide sum).</summary>
    public static CobolDec MeanExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEAN");
        return CobolExact.Quotient(Total(xs), CobolExact.From(xs.Length, 0), mode, jam: false);
    }

    /// <summary>MOD (§15.64.4): <c>argument-1 − argument-2 × FUNCTION INTEGER (argument-1 / argument-2)</c>, the floored modulus of the
    /// exact values (the renderer passes the §15.3 type-6 integers; <see cref="ModWide"/>), rounded once. A zero divisor is §15.64.3
    /// r2's one raise site (<see cref="ModZeroDivisor"/>), shared with every other carrier's body.</summary>
    public static CobolDec ModExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(ModZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? ModWide(v[0], v[1]).ToDecRounded(s, mode)
            : CobolExact.Remainder(a, b, floored: true).ToDecRounded(mode);
    }

    /// <summary>REM (§15.77.4): <c>argument-1 − argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2)</c>, the truncated
    /// remainder of the exact values (<see cref="RemWide"/>), rounded once. A zero divisor is §15.77.3 r2's one raise site
    /// (<see cref="RemZeroDivisor"/>).</summary>
    public static CobolDec RemExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(RemZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? RemWide(v[0], v[1]).ToDecRounded(s, mode)
            : CobolExact.Remainder(a, b, floored: false).ToDecRounded(mode);
    }

    /// <summary>⛔ VARIANCE (§15.98.4 1) OF THE EXACT ARGUMENTS, DIVIDED ONCE (kb/Work PB4111): the mean of the squared deviations from
    /// the EXACT mean. With every argument aligned to the common scale s as an integer Xᵢ and shifted by the first, dᵢ = Xᵢ − X₁ (the
    /// variance of a list does not move with it, and the shift makes the differences small exactly where the expression cancels),
    /// Σ(xᵢ − mean)² / n is algebraically <c>(n·Σdᵢ² − (Σdᵢ)²) / (n²·10^(2s))</c>: one numerator and one denominator on the wide form,
    /// divided once (<see cref="CobolWide.QuotientDec(int, CobolWide, int, CobolRounding, bool)"/>). The 34-digit mean of <see cref="VarianceDec"/> is no approximation of it
    /// under native arithmetic: a mean off by δ adds exactly δ² (the cross term Σ(xᵢ − m) is 0), so VARIANCE (P, P − 1) of a 39-digit
    /// P answered 10^10 for 0.25, and VARIANCE of 10^30, 10^30 + 1, 10^30 + 1 answered 0.2222223333 for 2/9. Deviations whose squares
    /// leave the 77 digits take the same identity on the arbitrary-precision form (<see cref="VarianceBig"/>).</summary>
    public static CobolDec VarianceExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §15.3 — at least one argument (kb/Work PB257)
        return TryVariance(xs, mode, out CobolDec variance) ? variance : VarianceBig(xs, mode);
    }

    /// <summary>The identity of <see cref="VarianceExact"/> where a step leaves the wide form: the same numerator
    /// <c>n·Σdᵢ² − (Σdᵢ)²</c> and denominator <c>n²</c> formed by <see cref="CobolExact"/>'s operations (the wide form, then the
    /// arbitrary-precision one, kb/Work PB4592), divided once, so a variance of arguments past 77 digits is the exact variance
    /// rounded once and never the SDIDI body's (kb/Work PB4750).</summary>
    private static CobolDec VarianceBig(CobolExact[] xs, CobolRounding mode)
    {
        CobolExact sum = CobolExact.From(0, 0).Working(), squares = sum, first = xs[0].Working();
        foreach (CobolExact x in xs)
        {
            CobolExact d = CobolExact.Sub(x.Working(), first);
            sum = CobolExact.Add(sum, d);
            squares = CobolExact.Add(squares, CobolExact.Mul(d, d));
        }
        CobolExact n = CobolExact.From(xs.Length, 0).Working();
        CobolExact numerator = CobolExact.Sub(CobolExact.Mul(n, squares), CobolExact.Mul(sum, sum));
        return CobolExact.Quotient(numerator, CobolExact.Mul(n, n), mode, jam: false);
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
        if (!CobolWide.TryMul(n, squares, out CobolWide scaled)
            || !CobolWide.TryMul(sum, sum, out CobolWide sumSquared)
            || !CobolWide.TryAdd(scaled, CobolWide.Negate(sumSquared), out CobolWide numerator)) return false;
        variance = numerator.QuotientDec(2 * s, CobolWide.Mul(n, n), 0, mode, jam: false);
        return true;
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
    /// the wide form (many periods at a rate of many places) takes the same identity on the arbitrary-precision form (<see cref="PresentValueBig"/>).</summary>
    public static CobolDec PresentValueExact(CobolRounding mode, CobolExact rate, params CobolExact[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        CobolExact discount = CobolExact.Add(CobolExact.From(1, 0), rate);   // 1 + rate, exact
        if (discount.Sign <= 0) return CobolDec.From(PresentValueDomain(rate.Image.ToDouble()), 0);   // §15.74.3 r2
        return TryPresentValue(rate, amounts, mode, out CobolDec value) ? value : PresentValueBig(discount, amounts, mode);
    }

    /// <summary>The identity of <see cref="PresentValueExact"/> where a step leaves the wide form (many periods at a rate of many
    /// places): <c>Σ aₖ·d^(n−k) / d^n</c> for the discount base d = 1 + rate, the numerator by Horner's rule
    /// (<c>Nₖ = Nₖ₋₁·d + aₖ</c>), formed by <see cref="CobolExact"/>'s operations (the arbitrary-precision form past the wide one,
    /// kb/Work PB4592) and divided once (kb/Work PB4750).</summary>
    private static CobolDec PresentValueBig(CobolExact discount, CobolExact[] amounts, CobolRounding mode)
    {
        discount = discount.Working();
        CobolExact numerator = CobolExact.From(0, 0).Working();
        foreach (CobolExact amount in amounts) numerator = CobolExact.Add(CobolExact.Mul(numerator, discount), amount.Working());
        return CobolExact.Quotient(numerator, CobolExact.Pow(discount, amounts.Length), mode, jam: false);
    }

    private static bool TryPresentValue(CobolExact rate, CobolExact[] amounts, CobolRounding mode, out CobolDec value)
    {
        value = default;
        int a = Math.Max(0, rate.Scale);
        if (a > CobolWide.MaxDigits - 1 || !rate.TryAt(a, out CobolWide r)) return false;
        CobolWide q = CobolWide.PowerOfTen(a);
        if (!CobolWide.TryAdd(q, r, out CobolWide p)) return false;
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
        if (!CobolWide.TryPow(p, amounts.Length, out CobolWide denominator)) return false;
        value = numerator.QuotientDec(t, denominator, 0, mode, jam: false);
        return true;
    }
}
