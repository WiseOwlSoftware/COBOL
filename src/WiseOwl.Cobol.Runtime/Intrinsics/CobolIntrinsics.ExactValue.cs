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
/// 1989:2023 §2023:8.8.1.3), and §2023:15.4.1 makes the value of a function with an equivalent arithmetic expression "an implementor-defined
/// approximation of the value of that expression". CLAUDE.md rule 1 sends that latitude to GnuCOBOL, which evaluates these
/// functions from the exact argument values (3.2.0: <c>VARIANCE (P, P - 1)</c> of a 39-digit P is 0.25, <c>ORD-MAX (P - 1, P, 2.0E0)</c>
/// is 2, <c>MOD (P, 2 ** 10)</c> is 999). The SDIDI bodies of <c>CobolIntrinsics.Dec.cs</c> took each argument as its 34-digit image
/// and rounded each step — a 34-digit mean is no approximation of a variance at all, since a mean off by δ adds exactly δ² — and
/// stay the STANDARD-DECIMAL bodies, where §2023:15.4.1 1) makes the 34-digit SDIDI evaluation the value.</para>
/// <para>WHAT IS ROUNDED, ONCE. A computed value (a sum, a difference, a remainder, a quotient) is rounded to the SDIDI's 34 digits in
/// the native intermediate rounding mode; a selection returns the content of the selected argument itself, every digit of it
/// (§2023:15.59.4 1), §2023:15.63.4 1), §2023:15.61.1: a float item's 41-digit expansion included, kb/Work PB4730); an ordinal is exact. The SUM,
/// RANGE, MEAN, MIDRANGE, MEDIAN, MOD and REM bodies are exact at any size the arbitrary-precision form holds (|value| &lt; 10^6145);
/// VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE take the wide form first and, where their work leaves it, the same identity on the
/// arbitrary-precision form (kb/Work PB4750); none of them evaluates an SDIDI image.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>The position of the greatest (<paramref name="greatest"/>) or least exact value; the leftmost of equal values
    /// (§2023:15.59.4, §2023:15.63.4, §2023:15.71.4 3), §2023:15.72.4), compared by algebraic value (§2023:15.71.4 2): "according to the rules for simple
    /// conditions", §2023:8.8.4.2.4).</summary>
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

    /// <summary>ORD-MAX (§2023:15.71.4): the ordinal position of the argument having the greatest value.</summary>
    public static long OrdMaxExact(params CobolExact[] xs) => SelectExact(xs, greatest: true, "ORD-MAX") + 1;

    /// <summary>ORD-MIN (§2023:15.72.4): the ordinal position of the argument having the least value.</summary>
    public static long OrdMinExact(params CobolExact[] xs) => SelectExact(xs, greatest: false, "ORD-MIN") + 1;

    /// <summary>MAX (§2023:15.59.4): the content of the argument having the greatest value.</summary>
    public static CobolExact MaxExact(params CobolExact[] xs) => xs[SelectExact(xs, greatest: true, "MAX")];

    /// <summary>MIN (§2023:15.63.4): the content of the argument having the least value.</summary>
    public static CobolExact MinExact(params CobolExact[] xs) => xs[SelectExact(xs, greatest: false, "MIN")];

    /// <summary>MEDIAN (§2023:15.61.4) on the SDIDI lane: <see cref="MedianWhole"/>, with an exact mean of two fixed-point middles rounded once
    /// to the SDIDI's 34 digits in <paramref name="mode"/>.</summary>
    public static CobolExact MedianExact(CobolRounding mode, params CobolExact[] xs) => Median(xs, mode);

    /// <summary>MEDIAN (§2023:15.61.4) EXACTLY, never cut to the SDIDI's 34 digits: the lane of a list that aligns an argument of unknown
    /// bound (kb/Work PB4792, review R1-1), whose value the Int128 body held whole before the alignment could leave it.</summary>
    public static CobolExact MedianWhole(params CobolExact[] xs) => Median(xs, null);

    /// <summary>⛔ MEDIAN (§2023:15.61.4), ONE BODY FOR BOTH LANES (kb/Work PB4827, PB4859, PB4891). An odd count returns the middle argument whole,
    /// as its own kind (§2023:15.61.4 1)). An even count is the mean of the two middle values (§2023:15.61.4 2)), a new value: when a MIDDLE value
    /// is binary64 (<see cref="CobolExact.TryBinary64"/>: a float item's content or an engine's approximation, written or returned by a nested
    /// selection, which only the run time knows) the mean is evaluated in binary64 (§2023:15.4.1: "an implementor-defined approximation of the value
    /// of that expression"; docs/CONFORMANCE.md DOC-A.1-92b), as <see cref="MedianReal"/> forms it for a count the binder knows, and is itself a
    /// binary64 of the kind its middles have (<see cref="CobolExact.OfBinary64Result"/>); when both middles are fixed-point it is their exact mean, a binary64 elsewhere
    /// in the list being no operand of it, rounded once in <paramref name="sdidi"/> when the lane returns the SDIDI. Equal values keep their
    /// written order, so the middle values are the ones <c>FunctionValueField.Median</c> picks for DISPLAY.</summary>
    private static CobolExact Median(CobolExact[] xs, CobolRounding? sdidi)
    {
        RequireArguments(xs.Length, "MEDIAN");
        int[] order = [.. Enumerable.Range(0, xs.Length)];
        Array.Sort(order, (i, j) => CobolExact.Compare(xs[i], xs[j]) is var c and not 0 ? c : i.CompareTo(j));
        CobolExact high = xs[order[xs.Length / 2]];
        if (xs.Length % 2 != 0) return high;
        CobolExact low = xs[order[xs.Length / 2 - 1]];
        bool lowBinary64 = low.TryBinary64(out double lowDouble), highBinary64 = high.TryBinary64(out double highDouble);
        if (lowBinary64 || highBinary64)
            return CobolExact.OfBinary64Result(MedianReal(
                lowBinary64 ? lowDouble : FloatResultant.FromExact(low, CobolRounding.NearestEven, single: false),
                highBinary64 ? highDouble : FloatResultant.FromExact(high, CobolRounding.NearestEven, single: false)),
                low.IsFloatContent || high.IsFloatContent);
        CobolExact mean = HalfSum(low, high);
        return sdidi is { } mode ? CobolExact.FromDec(mean.ToDecRounded(mode)) : mean;
    }

    /// <summary>⛔ THE BINARY64 EVALUATION OF AN ARITHMETIC-FAMILY BODY (kb/Work PB4923; docs/CONFORMANCE.md DOC-A.1-92b): when ANY argument
    /// is binary64 (<see cref="CobolExact.TryBinary64"/>: a float item's content or an approximation, written, or returned by a nested
    /// selection, which only the run time knows) the function's equivalent arithmetic expression is evaluated in binary64
    /// (§2023:15.4.1: "an implementor-defined approximation of the value of that expression"), as the float lane evaluates it for a
    /// list whose kinds the binder knows, by <paramref name="real"/> over each argument's binary64. The value is a binary64 of the
    /// kind its arguments have (<see cref="CobolExact.OfBinary64Result"/>). False, with nothing evaluated, when every argument is
    /// fixed-point: the body is then exact. SUM, MEAN, RANGE, MIDRANGE and MEDIAN share this one reading.
    /// <para><paramref name="floatFamily"/> is true for a function of the §2023:15.4.1 floating-math family (VARIANCE, STANDARD-DEVIATION,
    /// PRESENT-VALUE, ANNUITY: <c>IntrinsicCarrierRules.Binary64ResultIsApproximation</c>, kb/Work PB4979): its binary64 value is always an
    /// engine approximation and lands by its shortest round-trip decimal, never a float item's content that lands by its exact expansion.</para></summary>
    private static bool TryBinary64Body(CobolExact[] xs, Func<double[], double> real, out CobolExact value, bool floatFamily = false)
    {
        value = default;
        if (!xs.Any(x => x.TryBinary64(out _))) return false;
        value = CobolExact.OfBinary64Result(real([.. xs.Select(x => x.AsBinary64())]), !floatFamily && xs.Any(x => x.IsFloatContent));
        return true;
    }

    /// <summary>MIDRANGE (§2023:15.62.4): <c>(FUNCTION MAX + FUNCTION MIN) / 2</c>, rounded once; the binary64 mean of the two when an
    /// argument is binary64 (<see cref="TryBinary64Body"/>).</summary>
    public static CobolExact MidrangeExact(CobolRounding mode, params CobolExact[] xs) => Midrange(xs, mode);

    /// <summary>MIDRANGE (§2023:15.62.4) exactly (see <see cref="MedianWhole"/>).</summary>
    public static CobolExact MidrangeWhole(params CobolExact[] xs) => Midrange(xs, null);

    private static CobolExact Midrange(CobolExact[] xs, CobolRounding? sdidi)
    {
        if (xs.Length > 0 && TryBinary64Body(xs, MidrangeReal, out CobolExact real)) return real;
        CobolExact mean = HalfSum(xs[SelectExact(xs, greatest: true, "MIDRANGE")], xs[SelectExact(xs, greatest: false, "MIDRANGE")]);
        return sdidi is { } mode ? CobolExact.FromDec(mean.ToDecRounded(mode)) : mean;
    }

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

    /// <summary>SUM (§2023:15.88.4): the exact sum of the arguments at their common scale, rounded once; the binary64 sum when an
    /// argument is binary64 (<see cref="TryBinary64Body"/>).</summary>
    public static CobolExact SumExact(CobolRounding mode, params CobolExact[] xs) => Sum(xs, mode);

    /// <summary>SUM (§2023:15.88.4) exactly (see <see cref="MedianWhole"/>): 10000 and .9999999999999999999999999999999 stored at five places
    /// are 10000.99999, not the 10001.00000 of the sum's 34-digit image.</summary>
    public static CobolExact SumWhole(params CobolExact[] xs) => Sum(xs, null);

    private static CobolExact Sum(CobolExact[] xs, CobolRounding? sdidi)
    {
        RequireArguments(xs.Length, "SUM");
        if (TryBinary64Body(xs, SumReal, out CobolExact real)) return real;
        CobolExact total = Total(xs);
        return sdidi is { } mode ? CobolExact.FromDec(total.ToDecRounded(mode)) : total;
    }

    /// <summary>RANGE (§2023:15.76.4): <c>FUNCTION MAX − FUNCTION MIN</c>, at the two values' common scale, rounded once; the binary64
    /// difference when an argument is binary64 (<see cref="TryBinary64Body"/>).</summary>
    public static CobolExact RangeExact(CobolRounding mode, params CobolExact[] xs) => Range(xs, mode);

    /// <summary>RANGE (§2023:15.76.4) exactly (see <see cref="MedianWhole"/>).</summary>
    public static CobolExact RangeWhole(params CobolExact[] xs) => Range(xs, null);

    private static CobolExact Range(CobolExact[] xs, CobolRounding? sdidi)
    {
        if (xs.Length > 0 && TryBinary64Body(xs, RangeReal, out CobolExact real)) return real;
        CobolExact max = xs[SelectExact(xs, greatest: true, "RANGE")], min = xs[SelectExact(xs, greatest: false, "RANGE")];
        CobolExact difference = CobolExact.Sub(max, min);
        return sdidi is { } mode ? CobolExact.FromDec(difference.ToDecRounded(mode)) : difference;
    }

    /// <summary>MEAN (§2023:15.60.4 1): <c>(argument-11 + … + argument-1n) / n</c>, the exact sum divided ONCE — the SDIDI quotient every
    /// lane forms (kb/Work PB3860, PB4075; <see cref="MeanWide"/> is the same quotient over a wide sum); the binary64 mean when an
    /// argument is binary64 (<see cref="TryBinary64Body"/>).</summary>
    public static CobolExact MeanExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "MEAN");
        return TryBinary64Body(xs, MeanReal, out CobolExact real)
            ? real
            : CobolExact.FromDec(CobolExact.Quotient(Total(xs), CobolExact.From(xs.Length, 0), mode, jam: false));
    }

    /// <summary>MOD (§2023:15.64.4): <c>argument-1 − argument-2 × FUNCTION INTEGER (argument-1 / argument-2)</c>, the floored modulus of the
    /// exact values (the renderer passes the §2023:15.3 type-6 integers; <see cref="ModWide"/>), rounded once. A zero divisor is §2023:15.64.3
    /// r2's one raise site (<see cref="ModZeroDivisor"/>), shared with every other carrier's body.</summary>
    public static CobolDec ModExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(ModZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? ModWide(v[0], v[1]).ToDecRounded(s, mode)
            : CobolExact.Remainder(a, b, floored: true).ToDecRounded(mode);
    }

    /// <summary>REM (§2023:15.77.4): <c>argument-1 − argument-2 × FUNCTION INTEGER-PART (argument-1 / argument-2)</c>, the truncated
    /// remainder of the exact values (<see cref="RemWide"/>), rounded once. A zero divisor is §2023:15.77.3 r2's one raise site
    /// (<see cref="RemZeroDivisor"/>).</summary>
    public static CobolDec RemExact(CobolRounding mode, CobolExact a, CobolExact b)
    {
        if (b.Sign == 0) return CobolDec.From(RemZeroDivisor(), 0);
        return CobolExact.TryAlign([a, b], out CobolWide[] v, out int s)
            ? RemWide(v[0], v[1]).ToDecRounded(s, mode)
            : CobolExact.Remainder(a, b, floored: false).ToDecRounded(mode);
    }

    /// <summary>⛔ VARIANCE (§2023:15.98.4 1) OF THE EXACT ARGUMENTS, DIVIDED ONCE (kb/Work PB4111): the mean of the squared deviations from
    /// the EXACT mean. With every argument aligned to the common scale s as an integer Xᵢ and shifted by the first, dᵢ = Xᵢ − X₁ (the
    /// variance of a list does not move with it, and the shift makes the differences small exactly where the expression cancels),
    /// Σ(xᵢ − mean)² / n is algebraically <c>(n·Σdᵢ² − (Σdᵢ)²) / (n²·10^(2s))</c>: one numerator and one denominator on the wide form,
    /// divided once (<see cref="CobolWide.QuotientDec(int, CobolWide, int, CobolRounding, bool)"/>). The 34-digit mean of <see cref="VarianceDec"/> is no approximation of it
    /// under native arithmetic: a mean off by δ adds exactly δ² (the cross term Σ(xᵢ − m) is 0), so VARIANCE (P, P − 1) of a 39-digit
    /// P answered 10^10 for 0.25, and VARIANCE of 10^30, 10^30 + 1, 10^30 + 1 answered 0.2222223333 for 2/9. Deviations whose squares
    /// leave the 77 digits take the same identity on the arbitrary-precision form (<see cref="VarianceBig"/>).
    /// <para>⛔ THE BINARY64 EVALUATION WHEN AN ARGUMENT IS BINARY64 (kb/Work PB4979; docs/CONFORMANCE.md DOC-A.1-92b): a nested selection that returned
    /// a float item's content is binary64, a run-time fact (<see cref="TryBinary64Body"/>), so the variance is formed in binary64 as
    /// <c>VARIANCE (F M)</c> forms it, and is itself an approximation (the floating-math family's value, which lands by its shortest round-trip decimal).</para></summary>
    public static CobolExact VarianceExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §2023:15.3 — at least one argument (kb/Work PB257)
        return TryBinary64Body(xs, Variance, out CobolExact real, floatFamily: true) ? real : CobolExact.FromDec(VarianceOfExact(xs, mode));
    }

    /// <summary>The variance of arguments that are all fixed-point, exactly and rounded once (<see cref="VarianceExact"/>'s fixed-point arm).</summary>
    private static CobolDec VarianceOfExact(CobolExact[] xs, CobolRounding mode) =>
        TryVariance(xs, mode, out CobolDec variance) ? variance : VarianceBig(xs, mode);

    /// <summary>The identity of <see cref="VarianceOfExact"/> where a step leaves the wide form: the same numerator
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

    /// <summary>STANDARD-DEVIATION (§2023:15.86.4 1): <c>(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))</c>, the two bodies composed,
    /// so §2023:15.84.4's square root is the ONE SQRT body's (<see cref="SqrtDec"/>). The variance rounded once at 34 digits before the
    /// root is harmless: the square root's condition number is ½.
    /// <para>A binary64 argument (<see cref="TryBinary64Body"/>) takes the binary64 body, <c>StandardDeviation (F M)</c>'s (kb/Work PB4979).</para></summary>
    public static CobolExact StdDevExact(CobolRounding mode, params CobolExact[] xs)
    {
        RequireArguments(xs.Length, "STANDARD-DEVIATION");
        return TryBinary64Body(xs, StandardDeviation, out CobolExact real, floatFamily: true)
            ? real
            : CobolExact.FromDec(SqrtDec(mode, VarianceOfExact(xs, mode)));
    }

    /// <summary>⛔ PRESENT-VALUE (§2023:15.74.4 1) OF THE EXACT ARGUMENTS, DIVIDED ONCE (kb/Work PB4111): <c>Σ aₖ / (1 + rate)^k</c>, k = 1…n,
    /// with the discount base written as the reduced fraction <c>1 + rate = p / q</c> and the amounts aligned to their common scale t
    /// as integers Aₖ, is <c>Σ Aₖ·q^k·p^(n−k) / (10^t·p^n)</c>: one numerator (Horner, <c>Nₖ = Nₖ₋₁·p + Aₖ·q^k</c>) and one
    /// denominator on the wide form, rounded once. §2023:15.74.3 r2 (rate &gt; −1, so p &gt; 0) is screened on the exact rate through the ONE
    /// raise site (<see cref="PresentValueDomain"/>). The 34-digit term quotients of <see cref="PresentValueDec"/> answered
    /// PRESENT-VALUE (0, P, 1 − P) = 0 for 1 and PRESENT-VALUE (3, 10^33 + 1, −4·10^33) = 0.3 for 0.25. A discount whose powers leave
    /// the wide form (many periods at a rate of many places) takes the same identity on the arbitrary-precision form (<see cref="PresentValueBig"/>).
    /// <para>⛔ A BINARY64 RATE OR AMOUNT (<see cref="TryBinary64Body"/>, kb/Work PB4979; docs/CONFORMANCE.md DOC-A.1-92b) takes the binary64 body
    /// <c>PRESENT-VALUE (F M)</c> takes, over the discount base <c>1 + rate</c> formed on the rate's own carrier and then narrowed
    /// (kb/Work PB1000): the base is this exact <c>1 + rate</c> or, for a binary64 rate, the binary64 sum.</para></summary>
    public static CobolExact PresentValueExact(CobolRounding mode, CobolExact rate, params CobolExact[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        CobolExact discount = CobolExact.Add(CobolExact.From(1, 0), rate);   // 1 + rate, exact
        if (discount.Sign <= 0) return CobolExact.From(PresentValueDomain(rate.Image.ToDouble()), 0);   // §2023:15.74.3 r2
        if (TryBinary64Body([rate, .. amounts], list => PresentValue(discount.AsBinary64(), list[1..]), out CobolExact real, floatFamily: true)) return real;
        return CobolExact.FromDec(TryPresentValue(rate, amounts, mode, out CobolDec value) ? value : PresentValueBig(discount, amounts, mode));
    }

    /// <summary>⛔ ANNUITY (§2023:15.9.4) WITH A RATE THAT MAY BE A NESTED SELECTION OF A FLOAT ITEM (kb/Work PB4979; docs/CONFORMANCE.md DOC-A.1-92b).
    /// Argument-1 is a number, so a nested <c>MAX (F M2)</c> returns F's binary64 content when F is the greatest, a run-time fact
    /// (<see cref="CobolExact.TryBinary64"/>): the value is then <c>ANNUITY (F n)</c>'s binary64 (<see cref="Annuity(double, double)"/>, the
    /// domain screen §2023:15.9.3 r2/r3 through the same raise site as <see cref="AnnuityDec"/>), an approximation
    /// (<see cref="CobolExact.FromApproximation"/>: the floating-math family's value). Otherwise the SDIDI evaluation of <see cref="AnnuityDec"/>, which takes
    /// <paramref name="periods"/> as the unbounded SDIDI integer the §2023:15.3 type-6 position intake left (kb/Work PB637).</summary>
    public static CobolExact AnnuityExact(CobolRounding mode, CobolExact rate, CobolDec periods)
    {
        if (!rate.TryBinary64(out double binaryRate)) return CobolExact.FromDec(AnnuityDec(mode, rate.Image, periods));
        double n = periods.ToDouble();
        return binaryRate < 0 || periods.Sig <= 0
            ? CobolExact.From(AnnuityDomain(binaryRate, n), 0)
            : CobolExact.FromApproximation(Annuity(binaryRate, n));
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
