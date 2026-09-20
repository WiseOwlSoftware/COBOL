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
    // ── Trigonometric / logarithmic (each returns the §15.x.4 equivalent-expression value) ────────────────────
    public static double Acos(double x) => Math.Acos(x);     // §15.8 — arccos in [0, π]; |x|>1 → NaN → 0
    public static double Asin(double x) => Math.Asin(x);     // §15.10 — arcsin in [−π/2, π/2]
    public static double Atan(double x) => Math.Atan(x);     // §15.11 — arctan in (−π/2, π/2)
    public static double Cos(double x) => Math.Cos(x);       // §15.20
    public static double Sin(double x) => Math.Sin(x);       // §15.82
    public static double Tan(double x) => Math.Tan(x);       // §15.89
    /// <summary>SQRT (§15.84) on the NATIVE carrier. TWO rules meet in this one line and each was missing
    /// (kb/Work PB246).</summary>
    /// <remarks><para>⛔ §15.84.3 <b>rule 2</b> — "The value of argument-1 shall be zero or positive" — is a
    /// VALUE constraint, so §15.3 rule 14 makes it EC-ARGUMENT-FUNCTION at run time, exactly as Log/Log10 two
    /// lines below decide theirs AT THE BODY. This was a bare <c>Math.Sqrt</c> whose only detector was the NaN
    /// artifact, and its comment named rule 1 (the CLASS rule) for the rule 2 obligation. The artifact is not
    /// total: <c>NumericRenderer.Real</c> converts an SDIDI operand through <c>CobolDec.ToDouble</c>, which is
    /// <c>(double)Sig * Math.Pow(10, Exp)</c>, so for <c>Exp ≤ −324</c> the power underflows to +0.0 and a
    /// NEGATIVE argument arrives as −0.0 — and <c>Math.Sqrt(-0.0)</c> is −0.0, not NaN, which
    /// <c>RealResult</c>/<c>FromDouble</c> (both screening <c>IsNaN</c> alone) then pass straight through.
    /// The explicit guard closes the domain by construction rather than by artifact.</para>
    /// <para>⛔ §15.84.4 <b>rule 4</b> — "When native arithmetic is in effect, the returned value is the
    /// ABSOLUTE VALUE of the approximation of the square root of argument-1" — was not implemented at all, only
    /// unreachable-by-argument. The phrase is not decoration: IEC 60559 mandates <c>sqrt(−0) = −0</c> precisely
    /// so that sqrt is NOT the absolute value there, while <c>|−0.0|</c> is +0.0. −0.0 is a LEGAL argument-1
    /// (rule 2 admits zero) that a COMP-1/COMP-2 item reaches by ordinary underflow, and the receiver-less/float
    /// arm renders it unquantized through <c>CobolFloat.Display</c>, which is IEEE-faithful and prints the sign.
    /// <c>Math.Abs</c> IS rule 4, and it is the only input on which it does any work.</para>
    /// <para>The APPROXIMATION half needs nothing: <c>Math.Sqrt</c> is IEC 60559 correctly-rounded, and SQRT's
    /// catalog row carries <c>Codomain.None</c> correctly — §15.84.4 states no bound, so the
    /// <c>FromDoubleBounded</c> clamp must not apply.</para></remarks>
    public static double Sqrt(double x) =>
        x < 0 ? Exceptions.ExceptionState.ArgumentError("SQRT argument-1 shall be zero or positive (ISO §15.84.3 rule 2)")
              : Math.Abs(Math.Sqrt(x));                  // §15.84.4 r4 — the ABSOLUTE value (|−0.0| = +0.0)
    // §15.55.3 r2 / §15.56.3 r2: the argument domain is > 0. A ≤ 0 argument is a real ARGUMENT-rule violation → raise
    // EC-ARGUMENT-FUNCTION at the body (§15.3 default 0 when checking off — the long result widens to double), NOT the
    // saturating −∞ that FromDouble now returns for a legal EXP overflow (CA24). ArgumentError throws when checking on.
    public static double Log(double x) => x <= 0 ? Exceptions.ExceptionState.ArgumentError("LOG argument must be > 0 (ISO §15.55.3 r2)") : Math.Log(x);
    public static double Log10(double x) => x <= 0 ? Exceptions.ExceptionState.ArgumentError("LOG10 argument must be > 0 (ISO §15.56.3 r2)") : Math.Log10(x);
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
        rate < 0 || periods <= 0 ? AnnuityDomain(rate, periods)
        : rate == 0 ? 1d / periods : rate / (1 - Math.Pow(1 + rate, -periods));

    /// <summary>PRESENT-VALUE (§15.74.4): <c>Σ amountᵢ / (1 + rate)^i</c>, i = 1..n. Domain: §15.74.3 r2
    /// (rate &gt; −1) — a rate at or below −1 zeroes or inverts the discount base and the sum is undefined.</summary>
    public static double PresentValue(double rate, params double[] amounts)
    {
        RequireArguments(amounts.Length, "PRESENT-VALUE");
        if (rate <= -1) return PresentValueDomain(rate);
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

    /// <summary>ONE current pseudo-random sequence per run unit (§15.75.3): the first argument-less reference uses
    /// an implementor-defined seed (rule 4 — <c>docs/CONFORMANCE.md#DOC-A.1-144</c> is that determination and it is
    /// PER-PROCESS OS ENTROPY: the parameterless <see cref="System.Random"/> is .NET's shared xoshiro256** seeded
    /// from OS entropy, so there is no fixed seed and a sequence is reproducible only from an explicit
    /// <c>FUNCTION RANDOM(seed)</c>. This comment said "the .NET time-derived default" until 2026-09-03 — a
    /// pre-.NET-6 description of a different mechanism, and the register is the one that is right); a seeded
    /// reference REPLACES the
    /// sequence (rule 3). NOTE: the legacy oracle instead news a throwaway generator per seeded call — both satisfy
    /// the 0 ≤ r &lt; 1 NIST range checks, but the spec form (one current sequence that seeded calls restart and
    /// argument-less calls continue) is implemented here per the scout brief §4.1.</summary>
    private static Random _random = new();

    /// <summary>RANDOM with no argument (§15.75.3 rule 5): the next number of the CURRENT sequence; 0 ≤ r &lt; 1
    /// (§15.75.4 rule 1).</summary>
    public static double Random() => _random.NextDouble();

    /// <summary>RANDOM (seed) (§15.75.3 rules 2/3): starts a NEW sequence from the seed and returns its first
    /// value. Same seed ⇒ same sequence on a given implementation (§15.75.4 rule 2 — per-process determinism is
    /// what NIST IF131A exercises; hazard H7). Seeds 0..32767 must yield distinct sequences (rule 3) — the .NET
    /// generator satisfies this for the whole int range. ⛔ A NEGATIVE seed violates r2 ("zero or a positive
    /// integer") and raises EC-ARGUMENT-FUNCTION (fix-queue PB65 — the old mask folded it onto a positive seed
    /// and RANDOM(-5) silently aliased RANDOM(0x7FFFFFFB & …)); the mask remains only as the documented
    /// wide-seed mapping for legal values beyond the generator's int range.</summary>
    public static double Random(long seed)
    {
        if (seed < 0)
        {
            Exceptions.ExceptionState.ArgumentError($"RANDOM argument-1 {seed} is not zero or a positive integer (§15.75.3 r2)");
            return 0;
        }
        _random = new Random((int)(seed & 0x7FFFFFFF));
        return _random.NextDouble();
    }
}
