// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F1 — the floating-point math intrinsics (ISO §15; COBOLNET_INTRINSICS deep-dive D1: these compute in
/// <see cref="double"/> under the §15.4.1 native-arithmetic license — "the value returned is an implementor-defined
/// approximation of the value of [the equivalent arithmetic] expression"). Out-of-domain arguments produce NaN here
/// and quantize to the EC-ARGUMENT-FUNCTION default result 0 in <see cref="CobolIntrinsics.FromDouble"/> (§15.3).
/// <para><b>Standard arithmetic (ISO §15.4.1, P10 Step 12).</b> The trig/log/SQRT/E/PI rows stay conforming under
/// ARITHMETIC IS STANDARD / STANDARD-DECIMAL: their returned-value rules are prose approximations, NOT equivalent
/// arithmetic expressions, so the value is implementor-defined in EVERY mode (§15.4.1 last paragraph), and a
/// double result entering a standard-decimal expression converts through the one §8.8.1.5.2 r1 operand
/// conversion (<c>CobolDec.FromDouble</c>). ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION carry
/// EAEs with inexact divisions that §15.4.1 r1 requires be evaluated in SDIDI form — those four are staged LOUD
/// under the standard modes at bind time (COBOLNET0899 'arithmetic-standard-intrinsic', IntrinsicBinder) until
/// CobolDec evaluations land.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    // ── The argument-1 VALUE domains (ISO §15.3 rule 14; kb/Work PB952) ────────────────────────────────────────

    /// <summary>The rational bounds a §15.x.3 "The value of argument-1 shall be …" rule states for the §15.4.1
    /// binary64 family — the whole set, read off the standard (<c>IntrinsicArgumentDomainDriftTests</c> keeps the
    /// catalog's <c>Domain</c> column equal to it).</summary>
    public enum ArgumentDomain
    {
        /// <summary>[−1, +1] — ACOS §15.8.3 rule 2, ASIN §15.10.3 rule 2.</summary>
        ClosedUnit,
        /// <summary>[0, ∞) — SQRT §15.84.3 rule 2 ("zero or positive"), ANNUITY §15.9.3 rule 2.</summary>
        NonNegative,
        /// <summary>(0, ∞) — LOG §15.55.3 rule 2, LOG10 §15.56.3 rule 2 ("greater than zero").</summary>
        Positive,
        /// <summary>(−1, ∞) — PRESENT-VALUE §15.74.3 rule 2 ("greater than −1").</summary>
        AboveMinusOne,
    }

    /// <summary>⛔ THE ONE argument-domain SCREEN of the binary64 family, and it runs on the EXACT operand, BEFORE the
    /// binary64 conversion (kb/Work PB952). §15.3 rule 14: "If the evaluation of an argument results in an incorrect
    /// value for that argument … the EC-ARGUMENT-FUNCTION exception condition is set to exist" — a rule about the
    /// ARGUMENT'S value, and the binary64 the body computes on is not that value. The bodies used to decide it
    /// after the conversion, two ways and both partial: ACOS/ASIN by the NaN <c>Math.Acos</c> returns past ±1,
    /// SQRT/LOG/LOG10 by an explicit test on the double (PB246). A correctly-rounded conversion moves a value
    /// ACROSS a bound: <c>1.000000000000000001</c> (PIC 9V9(18), held exactly) becomes 1.0, so under armed checking
    /// <c>ASIN</c> answered π/2 and raised nothing; <c>-0.99999999999999999999</c> becomes −1.0, so PRESENT-VALUE
    /// raised on a legal rate; an SDIDI's <c>ToDouble</c> underflows a tiny negative to −0.0, which SQRT's
    /// <c>x &lt; 0</c> admitted. So each carrier is screened on its own terms: an exact scaled integer
    /// (<see cref="DomainScaled"/>), the SDIDI (<see cref="DomainDec"/>), or a value that IS a binary64 already —
    /// a float operand, whose double is its exact value (<see cref="DomainReal"/>).</summary>
    /// <returns>The binary64 the body computes on; when the argument is outside the domain, the condition is
    /// raised (fatal under checking) and NaN is returned, so the body's result is NaN and <see cref="RealResult"/>
    /// / <see cref="FromDouble"/> hand back the §15.3 documented default exactly as for every other rejected
    /// argument.</returns>
    public static double DomainScaled(Int128 unscaled, int scale, ArgumentDomain domain, string function, string rule) =>
        Admits(domain, CompareScaled(unscaled, scale, -1), (int)Int128.Sign(unscaled),
               CompareScaled(unscaled, scale, 1))
            ? CobolFloat.ScaledToDouble(unscaled, scale)
            : DomainViolation(domain, function, rule);

    /// <inheritdoc cref="DomainScaled"/>
    public static double DomainDec(CobolDec value, ArgumentDomain domain, string function, string rule) =>
        Admits(domain, Math.Sign(CobolDec.Compare(value, MinusOneDec)), Math.Sign(value.Sig.CompareTo(Int128.Zero)),
               Math.Sign(CobolDec.Compare(value, OneDec)))
            ? value.ToDouble()
            : DomainViolation(domain, function, rule);

    /// <inheritdoc cref="DomainScaled"/>
    public static double DomainReal(double value, ArgumentDomain domain, string function, string rule) =>
        !double.IsNaN(value) && Admits(domain, value.CompareTo(-1d), Math.Sign(value),
               value.CompareTo(1d))
            ? value
            : DomainViolation(domain, function, rule);

    private static readonly CobolDec OneDec = CobolDec.From(1, 0);
    private static readonly CobolDec MinusOneDec = CobolDec.From(-1, 0);

    /// <summary>The domain decided from the argument's sign against −1, 0 and +1 — the only bounds the rules state.</summary>
    private static bool Admits(ArgumentDomain domain, int vsMinusOne, int vsZero, int vsOne) => domain switch
    {
        ArgumentDomain.ClosedUnit => vsMinusOne >= 0 && vsOne <= 0,
        ArgumentDomain.NonNegative => vsZero >= 0,
        ArgumentDomain.Positive => vsZero > 0,
        ArgumentDomain.AboveMinusOne => vsMinusOne > 0,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "no such argument domain"),
    };

    /// <summary>sign(<c>unscaled × 10^−scale</c> − <paramref name="bound"/>) for a bound of ±1, EXACTLY: against
    /// 10^scale when the scale is non-negative (a magnitude past 10^38 at scale ≥ 39 is below 1); a negative scale
    /// is an integer multiple of ten, so only zero lies inside (−10, 10).</summary>
    private static int CompareScaled(Int128 unscaled, int scale, int bound)
    {
        if (scale < 0) return unscaled == 0 ? -bound : Math.Sign(unscaled.CompareTo(Int128.Zero));
        if (scale > 38) return -bound;
        Int128 unit = Pow10.AsWide(scale);
        return Math.Sign(unscaled.CompareTo(bound > 0 ? unit : -unit));
    }

    private static double DomainViolation(ArgumentDomain domain, string function, string rule)
    {
        Exceptions.ExceptionState.ArgumentError($"{function} argument-1 shall be {domain switch
        {
            ArgumentDomain.ClosedUnit => "greater than or equal to -1 and less than or equal to +1",
            ArgumentDomain.NonNegative => "greater than or equal to zero",
            ArgumentDomain.Positive => "greater than zero",
            _ => "greater than -1",
        }} (ISO {rule})");
        return double.NaN;
    }

    // ── Trigonometric / logarithmic (each returns the §15.x.4 equivalent-expression value) ────────────────────
    // ⛔ NO DOMAIN TEST IN ANY BODY BELOW. ACOS / ASIN / SQRT / LOG / LOG10 carry §15.x.3 rule-2 VALUE domains and
    // every one is decided by the screen above, on the exact operand, before the value reaches the body
    // (IntrinsicRenderer.FloatBody applies it from the catalog row's Domain column). A second test here, on the
    // double, would be the partial copy PB952 removed — and for a legal argument rounded onto a bound it would
    // raise where the rule does not.
    public static double Acos(double x) => Math.Acos(x);     // §15.8.4 r1 — arccos in [0, π]
    public static double Asin(double x) => Math.Asin(x);     // §15.10.4 r1 — arcsin in [−π/2, π/2]
    public static double Atan(double x) => Math.Atan(x);     // §15.11 — arctan in (−π/2, π/2)
    public static double Cos(double x) => Math.Cos(x);       // §15.20
    public static double Sin(double x) => Math.Sin(x);       // §15.82
    public static double Tan(double x) => Math.Tan(x);       // §15.89
    /// <summary>SQRT (§15.84) on the NATIVE carrier.</summary>
    /// <remarks><para>§15.84.3 <b>rule 2</b> ("The value of argument-1 shall be zero or positive") is decided by the
    /// <see cref="DomainScaled"/> screen on the exact operand (kb/Work PB952 — the explicit <c>x &lt; 0</c> test
    /// PB246 put here was the right rule on the wrong value: an SDIDI's <c>ToDouble</c> underflows a tiny negative
    /// argument to −0.0, which it admitted).</para>
    /// <para>⛔ §15.84.4 <b>rule 4</b> — "When native arithmetic is in effect, the returned value is the
    /// ABSOLUTE VALUE of the approximation of the square root of argument-1" (kb/Work PB246). IEC 60559 mandates
    /// <c>sqrt(−0) = −0</c> precisely so that sqrt is NOT the absolute value there, while <c>|−0.0|</c> is +0.0.
    /// −0.0 is a LEGAL argument-1 (rule 2 admits zero) that a COMP-1/COMP-2 item reaches by ordinary underflow,
    /// and the receiver-less/float arm renders it unquantized through <c>CobolFloat.Display</c>, which is
    /// IEEE-faithful and prints the sign. <c>Math.Abs</c> IS rule 4, and it is the only input on which it does
    /// any work.</para>
    /// <para>The APPROXIMATION half needs nothing: <c>Math.Sqrt</c> is IEC 60559 correctly-rounded, and SQRT's
    /// catalog row carries <c>Codomain.None</c> correctly — §15.84.4 states no bound, so the
    /// <c>FromDoubleBounded</c> clamp must not apply.</para></remarks>
    public static double Sqrt(double x) => Math.Abs(Math.Sqrt(x));   // §15.84.4 r4 — the ABSOLUTE value (|−0.0| = +0.0)
    public static double Log(double x) => Math.Log(x);       // §15.55 — domain: the screen (§15.55.3 r2)
    public static double Log10(double x) => Math.Log10(x);   // §15.56 — domain: the screen (§15.56.3 r2)
    public static double Exp(double x) => Math.Exp(x);       // §15.34 — e ** argument (COBOL-2002+)
    public static double Exp10(double x) => Math.Pow(10, x); // §15.35 — 10 ** argument (COBOL-2002+)

    /// <summary>E (§15.27, COBOL-2002+): the value of e — 2.718281828459045235360287… approximated in double.</summary>
    public static double E() => Math.E;

    /// <summary>PI (§15.73, COBOL-2002+): the value of π — 3.141592653589793238462643… approximated in double.</summary>
    public static double Pi() => Math.PI;

    // ── Financial (ISO §15.9 / §15.74) ─────────────────────────────────────────────────────────────────────────

    /// <summary>The ONE §15.9.3 r2/r3 raise site (both carriers funnel here — the PB32 one-site-per-rule
    /// discipline): a negative rate or a non-positive period count is an incorrect argument VALUE, so
    /// EC-ARGUMENT-FUNCTION per §15.3 (documented default 0). Fix-queue PB65 — the guards were absent while
    /// the same file's Log/Log10 carried theirs, and ANNUITY(-0.5 3) returned +0.0714 silently.</summary>
    internal static long AnnuityDomain(double rate, double periods) =>
        rate < 0
            ? Exceptions.ExceptionState.ArgumentError($"ANNUITY argument-1 {rate} is not greater than or equal to zero (§15.9.3 r2)")
            : Exceptions.ExceptionState.ArgumentError($"ANNUITY argument-2 {periods} is not a positive integer (§15.9.3 r3)");

    /// <summary>The ONE §15.74.3 r2 raise site (both carriers).</summary>
    internal static long PresentValueDomain(double rate) =>
        Exceptions.ExceptionState.ArgumentError($"PRESENT-VALUE argument-1 {rate} is not greater than -1 (§15.74.3 r2)");

    /// <summary>ANNUITY (§15.9.4): rate 0 ⇒ <c>1 / periods</c> (rule 1); else
    /// <c>rate / (1 − (1 + rate)^(−periods))</c> (rule 2). Domain: §15.9.3 r2 (rate ≥ 0) and r3
    /// (periods a positive integer — integrality is the upstream IntArg latitude; positivity is checked here).</summary>
    public static double Annuity(double rate, double periods) =>
        periods <= 0 ? AnnuityDomain(rate, periods)          // r2 (rate ≥ 0) is the screen's (kb/Work PB952)
        : rate == 0 ? 1d / periods : rate / (1 - Math.Pow(1 + rate, -periods));

    /// <summary>PRESENT-VALUE (§15.74.4): <c>Σ amountᵢ / (1 + rate)^i</c>, i = 1..n. Domain: §15.74.3 r2
    /// (rate &gt; −1) — a rate at or below −1 zeroes or inverts the discount base and the sum is undefined.</summary>
    public static double PresentValue(double rate, params double[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        // §15.74.3 r2 (rate > −1) is the argument-domain screen's (DomainScaled, kb/Work PB952): a legal rate
        // just above −1 converts to −1.0, and a test here would raise on it.
        double pv = 0;
        for (int i = 0; i < amounts.Length; i++) pv += amounts[i] / Math.Pow(1 + rate, i + 1);
        return pv;
    }

    // ── Statistics over doubles (ISO §15.86 / §15.98) ─────────────────────────────────────────────────────────

    /// <summary>VARIANCE (§15.98.4): the mean of the squared deviations from the arguments' arithmetic mean.</summary>
    public static double Variance(params double[] xs)
    {
        RequireArguments(xs.Length, "VARIANCE");            // §15.3 — at least one argument (kb/Work PB257)
        double mean = 0;
        foreach (double x in xs) mean += x;
        mean /= xs.Length;
        double sum = 0;
        foreach (double x in xs) sum += (x - mean) * (x - mean);
        return sum / xs.Length;
    }

    /// <summary>STANDARD-DEVIATION (§15.86.4 r1): the equivalent arithmetic expression is literally
    /// <c>(FUNCTION SQRT (FUNCTION VARIANCE (argument-list)))</c>, so it is written here as the two FUNCTION
    /// bodies composed — <see cref="Sqrt"/>, not a bare <c>Math.Sqrt</c>, so §15.84.3 r2's domain guard and
    /// §15.84.4 r4's absolute value are inherited from the ONE SQRT body rather than re-decided (kb/Work
    /// PB246/PB257).</summary>
    public static double StandardDeviation(params double[] xs)
    {
        RequireArguments(xs.Length, "STANDARD-DEVIATION");
        return Sqrt(Variance(xs));
    }

    // ── RANDOM (ISO §15.75) ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>RANDOM with no argument (§15.75.3 rule 5): the next number of the CURRENT sequence; 0 ≤ r &lt; 1
    /// (§15.75.4 rule 1). The sequence is RUN-UNIT state (§15.75.3 rule 4 scopes the implementor seed to "the first
    /// reference to this function in the run unit") and lives on <see cref="RunUnit.Random"/> — a process-global
    /// static here let a second run unit in one process continue the first's seeded sequence (kb/Work PB307).
    /// NOTE: the legacy oracle instead news a throwaway generator per seeded call — both satisfy the 0 ≤ r &lt; 1
    /// NIST range checks, but the spec form (one current sequence that seeded calls restart and argument-less calls
    /// continue) is implemented here per the scout brief §4.1.</summary>
    public static double Random() => RunUnit.Current.Random.Next();

    /// <summary>RANDOM (seed) (§15.75.3 rules 2/3): starts a NEW sequence from the seed and returns its first
    /// value. Same seed ⇒ same sequence on a given implementation (§15.75.4 rule 2 — per-process determinism is
    /// what NIST IF131A exercises; hazard H7). Seeds 0..32767 must yield distinct sequences (rule 3) — the .NET
    /// generator satisfies this for the whole int range. ⛔ A NEGATIVE seed violates r2 ("zero or a positive
    /// integer") and raises EC-ARGUMENT-FUNCTION (fix-queue PB65 — the old mask folded it onto a positive seed
    /// and RANDOM(-5) silently aliased RANDOM(0x7FFFFFFB &amp; …)); the mask remains only as the documented
    /// wide-seed mapping for legal values beyond the generator's int range.</summary>
    /// <remarks>
    /// ⛔ THE SEED IS A <b>TOTAL</b> §15 INTEGER ARGUMENT, AND THE <c>Int128</c> CARRIER IS THAT CLAIM
    /// (kb/Work PB636 — see <c>IntrinsicRenderer.AsIntWide</c>, whose pairing guard reads this signature).
    /// §15.75.3 r2 constrains the SIGN and nothing else — "If argument-1 is specified, it shall be zero or a
    /// positive integer" — and §15.75.4 r3 makes the distinct-sequence subset a FLOOR, not a domain: "This
    /// subset shall include the values from 0 through at least 32767", so a seed outside that subset is still a
    /// legal argument whose sequence r1/r2 define. §15.3's closing paragraph raises EC-ARGUMENT-FUNCTION only on
    /// "an incorrect value for that argument … according to the rules specified in the function definition", and
    /// no rule here makes a large seed incorrect. The parameter was <c>long</c>, so the renderer's narrowing
    /// intake screened the argument first: <c>FUNCTION RANDOM(S)</c> with <c>S PIC 9(19)</c> holding
    /// 9,999,999,999,999,999,999 terminated the run unit under <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING
    /// ON</c> and, with checking off, substituted the ARGUMENT 0 and ran the seed-0 sequence without a word.
    /// <para>The reduction to the generator's state is unchanged and stays the documented one
    /// (<c>docs/CONFORMANCE.md</c> row DOC-A.1-145 under §15.75.4 r3): the low 31 bits,
    /// <c>seed AND 0x7FFFFFFF</c>. It is the identity on 0..2³¹−1 — so the required 0..32 767 floor is injective
    /// with a wide margin — and folds every wider seed onto that window, which r3 permits: the subset yielding
    /// DISTINCT sequences need only include the floor, not exhaust the domain.</para>
    /// </remarks>
    public static double Random(Int128 seed)
    {
        if (seed < 0)
        {
            Exceptions.ExceptionState.ArgumentError($"RANDOM argument-1 {seed} is not zero or a positive integer (§15.75.3 r2)");
            return 0;
        }
        return RunUnit.Current.Random.Restart((int)(seed & 0x7FFFFFFF));
    }
}
