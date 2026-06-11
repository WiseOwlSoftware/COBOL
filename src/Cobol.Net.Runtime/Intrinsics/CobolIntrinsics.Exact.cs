// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F2 — exact numeric / integer / statistics / NUMVAL intrinsics (ISO §15; deep-dive D1: exact-numeric
/// functions stay base-10 EXACT as unscaled <see cref="Int128"/> values at a known decimal scale — never double,
/// never decimal). The "<c>…Scaled</c>" variadic entries take arguments ALREADY ALIGNED by the emitter to one
/// common scale (the same Align machinery the arithmetic verbs use, ISO §8.8.1), so value comparison/arithmetic on
/// the unscaled longs IS comparison/arithmetic on the algebraic values.
/// </summary>
public static partial class CobolIntrinsics
{
    // ── Integer functions (ISO §15.36 / §15.44 / §15.49 / §15.81) ─────────────────────────────────────────────

    /// <summary>FACTORIAL (§15.36.4): 0 ⇒ 1; n ⇒ n × (n−1)!. Computed in <see cref="Int128"/>: 33! ≈ 8.68e36 fits
    /// (Int128.Max ≈ 1.70e38); 34! is the first overflow — the deep-dive's original "33! overflows" boundary was
    /// off by one (verified analytically; doc fixed in the same change set). A negative argument violates §15.36.3
    /// rule 1 and a 34+ argument overflows the carrier — both yield the EC-ARGUMENT-FUNCTION default result 0
    /// (§15.3, checking disabled).</summary>
    public static Int128 Factorial(long n)
    {
        if (n is < 0 or > 33)                                // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError($"FACTORIAL argument {n} violates §15.36.3 rule 1 (negative) or overflows the Int128 carrier (> 33)");
        Int128 r = 1;
        for (long i = 2; i <= n; i++) r *= i;
        return r;
    }

    /// <summary>SIGN (§15.81.4, COBOL-2002+): −1 / 0 / +1 by the argument's algebraic sign (scale-independent —
    /// the unscaled carrier has the value's sign).</summary>
    public static long SignOf(Int128 v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    /// <summary>INTEGER (§15.44.4): the greatest integer ≤ the argument (floor). <paramref name="v"/> is the
    /// unscaled value at <paramref name="scale"/> fraction digits; the result is a scale-0 integer.</summary>
    public static Int128 Floor(Int128 v, int scale)
    {
        if (scale <= 0) return v * Pow10I(-scale);           // already an integer (negative scale = P-trailing zeros)
        Int128 d = Pow10I(scale);
        Int128 q = v / d;
        return v < 0 && v % d != 0 ? q - 1 : q;
    }

    /// <summary>INTEGER-PART (§15.49.4): the integer part of the argument (truncation toward zero), scale 0.</summary>
    public static Int128 Truncate(Int128 v, int scale) => scale <= 0 ? v * Pow10I(-scale) : v / Pow10I(scale);

    /// <summary>ABS (§15.7.4, COBOL-2014+): the absolute value, at the argument's own scale.</summary>
    public static Int128 AbsScaled(Int128 v) => v < 0 ? -v : v;

    /// <summary>FRACTION-PART (§15.42.4, COBOL-2002+): <c>argument − FUNCTION INTEGER-PART(argument)</c> — the
    /// fractional part with the argument's sign, at the argument's own scale.</summary>
    public static Int128 FractionPart(Int128 v, int scale) => scale <= 0 ? 0 : v % Pow10I(scale);

    // ── MOD / REM (ISO §15.64 / §15.77) — over scale-ALIGNED unscaled values ──────────────────────────────────

    /// <summary>MOD (§15.64.4): <c>a − b × FUNCTION INTEGER(a / b)</c> — the floored modulus (the spec NOTE's sign
    /// table: −11 MOD 5 = 4, 11 MOD −5 = −4). Operands aligned to one scale, result at that scale (0 for the §15.64.3
    /// integer arguments). A zero divisor violates rule 2 → EC-ARGUMENT default 0 (§15.3).</summary>
    public static Int128 ModScaled(Int128 a, Int128 b)
    {
        if (b == 0)                                          // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError("MOD with a zero divisor (§15.64.3 rule 2)");
        Int128 q = a / b;                                    // truncating quotient of the ALIGNED values
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;        // → floor (FUNCTION INTEGER of the true ratio)
        return a - b * q;
    }

    /// <summary>REM (§15.77.4): <c>a − b × FUNCTION INTEGER-PART(a / b)</c> — the truncated remainder (sign follows
    /// the dividend). Operands aligned to one scale, result at that scale. Zero divisor → 0 (§15.3 EC default).</summary>
    public static Int128 RemScaled(Int128 a, Int128 b) => b == 0
        ? Exceptions.ExceptionState.ArgumentError("REM with a zero divisor (§15.77.3 rule 2)")   // raise point / §15.3 default
        : a % b;   // C# % truncates toward zero — exactly INTEGER-PART

    // ── Variadic statistics over scale-ALIGNED unscaled values (ISO §15.59–§15.63, §15.71–72, §15.76, §15.88) ──

    /// <summary>MAX (§15.59.4): the greatest argument value, at the common scale.</summary>
    public static Int128 MaxScaled(params Int128[] xs)
    {
        Int128 m = xs[0];
        foreach (var x in xs) if (x > m) m = x;
        return m;
    }

    /// <summary>MIN (§15.63.4): the least argument value, at the common scale.</summary>
    public static Int128 MinScaled(params Int128[] xs)
    {
        Int128 m = xs[0];
        foreach (var x in xs) if (x < m) m = x;
        return m;
    }

    /// <summary>SUM (§15.88.4): Σ arguments, at the common scale (exact in Int128).</summary>
    public static Int128 SumScaled(params Int128[] xs)
    {
        Int128 s = 0;
        foreach (var x in xs) s += x;
        return s;
    }

    /// <summary>RANGE (§15.76.4): <c>FUNCTION MAX − FUNCTION MIN</c>, at the common scale.</summary>
    public static Int128 RangeScaled(params Int128[] xs) => MaxScaled(xs) - MinScaled(xs);

    /// <summary>MEDIAN (§15.61.4): odd count ⇒ the middle of the sorted arguments (rule 1); even count ⇒ the mean
    /// of the two middles, <c>(b + c) / 2</c> (rule 2). Returned at scale common+1 — the ×10 makes the halving
    /// EXACT in both branches (odd: middle × 10; even: (b + c) × 5), so no rounding decision is buried here.</summary>
    public static Int128 MedianScaled(params Int128[] xs)
    {
        var sorted = (Int128[])xs.Clone();
        Array.Sort(sorted);
        int mid = sorted.Length / 2;
        return sorted.Length % 2 != 0 ? sorted[mid] * 10 : (sorted[mid - 1] + sorted[mid]) * 5;
    }

    /// <summary>MIDRANGE (§15.62.4): <c>(MAX + MIN) / 2</c> — returned at scale common+1 ((max+min) × 5, exact).</summary>
    public static Int128 MidrangeScaled(params Int128[] xs) => (MaxScaled(xs) + MinScaled(xs)) * 5;

    /// <summary>ORD-MAX (§15.71.4): the 1-based ordinal position of the greatest argument; ties take the FIRST
    /// occurrence (strictly-greater update — the legacy-proven rule the NIST goldens encode).</summary>
    public static long OrdMax(params Int128[] xs)
    {
        Int128 m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (xs[i] > m) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    /// <summary>ORD-MIN (§15.72.4): the 1-based ordinal position of the least argument; ties take the FIRST.</summary>
    public static long OrdMin(params Int128[] xs)
    {
        Int128 m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (xs[i] < m) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    // ── MAX/MIN/ORD-MAX/ORD-MIN over all-alphanumeric arguments (category-polymorphic, §15.59.3 r2 / §15.63.3) ──

    /// <summary>MAX with all-alphanumeric arguments: the greatest per the alphanumeric collation (ordinal —
    /// the native sequence <c>CobolString.Compare</c> realizes); the returned value IS the selected string.</summary>
    public static string MaxString(params string[] xs)
    {
        string m = xs[0];
        foreach (var x in xs) if (string.CompareOrdinal(x, m) > 0) m = x;
        return m;
    }

    /// <summary>MIN with all-alphanumeric arguments — the least per the alphanumeric collation.</summary>
    public static string MinString(params string[] xs)
    {
        string m = xs[0];
        foreach (var x in xs) if (string.CompareOrdinal(x, m) < 0) m = x;
        return m;
    }

    /// <summary>ORD-MAX over alphanumeric arguments (§15.71): 1-based position of the greatest; tie = first.</summary>
    public static long OrdMaxString(params string[] xs)
    {
        string m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (string.CompareOrdinal(xs[i], m) > 0) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    /// <summary>ORD-MIN over alphanumeric arguments (§15.72): 1-based position of the least; tie = first.</summary>
    public static long OrdMinString(params string[] xs)
    {
        string m = xs[0];
        long idx = 1;
        for (int i = 1; i < xs.Length; i++) if (string.CompareOrdinal(xs[i], m) < 0) { m = xs[i]; idx = i + 1; }
        return idx;
    }

    // ── NUMVAL / NUMVAL-C (ISO §15.67 / §15.68) ───────────────────────────────────────────────────────────────

    /// <summary>
    /// NUMVAL (§15.67): parse the human-formatted numeric string to its value, returned as the unscaled long at
    /// the compile-time <paramref name="scale"/> the emitter requested (≥ 6 — the hazard-H1 floor; parsing to
    /// (unscaled, actual-scale) then rescaling is hazard H2's discipline). The §15.67.3 formats: optional leading
    /// sign OR trailing sign/CR/DB (case-insensitive), spaces ignored leading/trailing and before the first digit
    /// (rule 2), one decimal separator — '.' normally, ',' under DECIMAL-POINT IS COMMA
    /// (<paramref name="commaMode"/>, rule 5). Malformed content → the EC-ARGUMENT-FUNCTION default 0 (§15.3).
    /// </summary>
    public static long Numval(string text, int scale, bool commaMode = false)
    {
        char dec = commaMode ? ',' : '.';
        string s = text.Trim();
        if (s.Length == 0)                                   // EC-ARGUMENT-FUNCTION raise point / §15.3 default 0
            return Exceptions.ExceptionState.ArgumentError("NUMVAL argument is empty (§15.67.3 — at least one digit required)");
        bool neg = false;
        // Trailing CR/DB (uppercase, lowercase, or mixed — §15.67.3 rule 1).
        if (s.Length >= 2 && (s.EndsWith("CR", StringComparison.OrdinalIgnoreCase)
                              || s.EndsWith("DB", StringComparison.OrdinalIgnoreCase)))
        {
            neg = true;
            s = s[..^2].TrimEnd();
        }
        // ONE leading sign (spaces between sign and digits are "before the first digit" — ignored, rule 2)…
        if (s.StartsWith('+')) s = s[1..].TrimStart();
        else if (s.StartsWith('-')) { neg = true; s = s[1..].TrimStart(); }
        // …or ONE trailing sign (format 2). Both present is malformed; the lenient double-strip mirrors the
        // legacy parser and yields the same values for conforming inputs.
        if (s.EndsWith('+')) s = s[..^1].TrimEnd();
        else if (s.EndsWith('-')) { neg = !neg; s = s[..^1].TrimEnd(); }

        Int128 unscaled = 0;
        int frac = -1, digits = 0;
        foreach (char c in s)
        {
            if (char.IsAsciiDigit(c))
            {
                unscaled = unscaled * 10 + (c - '0');
                digits++;
                if (frac >= 0) frac++;
                continue;
            }
            if (c == dec && frac < 0) { frac = 0; continue; }
            // Malformed content — the EC-ARGUMENT-FUNCTION raise point / §15.3 default 0.
            return Exceptions.ExceptionState.ArgumentError($"NUMVAL argument '{text}' violates the §15.67.3 formats (unexpected character '{c}')");
        }
        if (digits == 0)                                     // the formats require at least one digit
            return Exceptions.ExceptionState.ArgumentError($"NUMVAL argument '{text}' has no digits (§15.67.3)");
        if (frac < 0) frac = 0;
        // Rescale (unscaled, frac) → the requested scale. Widening is exact; narrowing truncates (the requested
        // scale already carries the ≥ 6 working floor, and the receiver's own store rounds/truncates once more).
        Int128 r = scale >= frac ? unscaled * Pow10I(scale - frac) : unscaled / Pow10I(frac - scale);
        if (neg) r = -r;
        return r > long.MaxValue ? long.MaxValue : r < long.MinValue ? long.MinValue : (long)r;
    }

    /// <summary>
    /// NUMVAL-C (§15.68): like NUMVAL with a currency string and grouping separators. The currency string —
    /// argument-2, or the SPECIAL-NAMES / default currency the BINDER injected when argument-2 is omitted
    /// (§15.68.3 rule 3) — is removed wherever it appears (leading/trailing spaces of argument-2 ignored, rule 2);
    /// grouping separators (',' normally; '.' under DECIMAL-POINT IS COMMA, rule 4d) are ignored (§15.68.4 rule 2);
    /// then the remainder parses exactly as NUMVAL (sign / CR / DB, rule 3).
    /// </summary>
    public static long NumvalC(string text, string currency, int scale, bool commaMode = false)
    {
        char group = commaMode ? '.' : ',';
        string cur = currency.Trim();
        string s = cur.Length == 0 ? text : text.Replace(cur, "", StringComparison.Ordinal);
        s = s.Replace(group.ToString(), "", StringComparison.Ordinal);
        // The currency may sit between the sign and the digits ("- $ 890.05"): removing it can leave interior
        // spaces after the sign, which Numval's sign-strip + TrimStart already ignores (§15.68.3 r4a's
        // space-strings around the currency).
        return Numval(s, scale, commaMode);
    }
}
