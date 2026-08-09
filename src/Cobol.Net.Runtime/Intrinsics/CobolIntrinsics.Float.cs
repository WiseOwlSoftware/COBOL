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
    public static double Sqrt(double x) => Math.Sqrt(x);     // §15.84 — argument ≥ 0 (rule 1); negative → NaN → 0
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
        if (rate <= -1) return PresentValueDomain(rate);
        double pv = 0;
        for (int i = 0; i < amounts.Length; i++) pv += amounts[i] / Math.Pow(1 + rate, i + 1);
        return pv;
    }

    // ── Statistics over doubles (ISO §15.86 / §15.98) ─────────────────────────────────────────────────────────

    /// <summary>VARIANCE (§15.98.4): the mean of the squared deviations from the arguments' arithmetic mean.</summary>
    public static double Variance(params double[] xs)
    {
        if (xs.Length == 0) return 0;                       // EC-ARGUMENT default — at least one argument required
        double mean = 0;
        foreach (double x in xs) mean += x;
        mean /= xs.Length;
        double sum = 0;
        foreach (double x in xs) sum += (x - mean) * (x - mean);
        return sum / xs.Length;
    }

    /// <summary>STANDARD-DEVIATION (§15.86.4): the square root of the VARIANCE of the same arguments.</summary>
    public static double StandardDeviation(params double[] xs) => Math.Sqrt(Variance(xs));

    // ── RANDOM (ISO §15.75) ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>ONE current pseudo-random sequence per run unit (§15.75.3): the first argument-less reference uses
    /// an implementor-defined seed (rule 4 — here the .NET time-derived default); a seeded reference REPLACES the
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
