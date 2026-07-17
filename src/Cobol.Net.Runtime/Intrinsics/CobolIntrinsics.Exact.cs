// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Family F2 — exact numeric / integer / statistics / NUMVAL intrinsics (ISO §15; deep-dive D1: exact-numeric
/// functions stay base-10 EXACT as unscaled <see cref="Int128"/> values at a known decimal scale — never double,
/// never decimal). The "<c>…Scaled</c>" variadic entries take arguments ALREADY ALIGNED by the emitter to one
/// common scale (the same Align machinery the arithmetic verbs use, ISO §8.8.1), so value comparison/arithmetic on
/// the unscaled longs IS comparison/arithmetic on the algebraic values.
/// <para><b>Standard arithmetic (ISO §15.4.1 r1 / §8.8.1.5, P10 Step 12).</b> Under ARITHMETIC IS STANDARD /
/// STANDARD-DECIMAL a function with an equivalent arithmetic expression must return EXACTLY that expression's
/// SDIDI-evaluated value. This family already satisfies it, so the mode needs no routing here: every EAE step of
/// MOD/REM (a − b×q with |b×q| ≤ |a|, ≤31 digits), MAX/MIN/RANGE (compare/subtract), SUM (≤32-digit sums per
/// step), MEDIAN/MIDRANGE (the ×10/×5 halving trick keeps the /2 exact), ABS/SIGN/INTEGER/INTEGER-PART/
/// FRACTION-PART is EXACT here AND exact in a 34-digit SDIDI (§8.8.1.5.2 — an exact ≤34-digit result never
/// rounds), so the two evaluations are digit-identical. The ONE recorded residue: a result whose exact form
/// exceeds 34 significant digits (FACTORIAL of 31–33; a SUM chain past 34 digits) stays exact-Int128 here where
/// the SDIDI evaluation would round each step to 34 digits — a divergence in the direction of MORE precision,
/// undetectable at bind time (argument values are runtime data) and recorded in COBOLNET_NUMERIC_DESIGN §D3
/// rather than staged. MEAN's inexact division is evaluated in SDIDI form by the emitter (IntrinsicRenderer).</para>
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
        if (scale <= 0) return v * Pow10.AsWide(-scale);           // already an integer (negative scale = P-trailing zeros)
        Int128 d = Pow10.AsWide(scale);
        Int128 q = v / d;
        return v < 0 && v % d != 0 ? q - 1 : q;
    }

    /// <summary>INTEGER-PART (§15.49.4): the integer part of the argument (truncation toward zero), scale 0.</summary>
    public static Int128 Truncate(Int128 v, int scale) => scale <= 0 ? v * Pow10.AsWide(-scale) : v / Pow10.AsWide(scale);

    /// <summary>ABS (§15.7.4, COBOL-2014+): the absolute value, at the argument's own scale.</summary>
    public static Int128 AbsScaled(Int128 v) => v < 0 ? -v : v;

    /// <summary>FRACTION-PART (§15.42.4, COBOL-2002+): <c>argument − FUNCTION INTEGER-PART(argument)</c> — the
    /// fractional part with the argument's sign, at the argument's own scale.</summary>
    public static Int128 FractionPart(Int128 v, int scale) => scale <= 0 ? 0 : v % Pow10.AsWide(scale);

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
        Int128 r = scale >= frac ? unscaled * Pow10.AsWide(scale - frac) : unscaled / Pow10.AsWide(frac - scale);
        if (neg) r = -r;
        return r > long.MaxValue ? long.MaxValue : r < long.MinValue ? long.MinValue : (long)r;
    }

    /// <summary>NUMVAL-F (§15.69, COBOL-2014+): the floating NUMVAL — a signed mantissa (with an optional decimal
    /// point) and an optional <c>E±exponent</c> (1..4 exponent digits). Parsed exactly to (unscaled, effective
    /// scale) then rescaled to the emitter's working <paramref name="scale"/> (native arithmetic ⇒ the §15.69.4 r2
    /// approximation license). Malformed content → the §15.3 default 0. Leading/trailing spaces (and, in the value
    /// path, any interior space) are ignored (rule 5); TEST-NUMVAL-F enforces exact placement.</summary>
    public static long NumvalF(string text, int scale, bool commaMode = false)
    {
        char dec = commaMode ? ',' : '.';
        string s = text.Replace(" ", "");
        if (s.Length == 0)
            return Exceptions.ExceptionState.ArgumentError("NUMVAL-F argument is empty (§15.69.3)");
        bool neg = false;
        int ei = s.IndexOfAny(['E', 'e']);
        string mant = ei < 0 ? s : s[..ei];
        string exps = ei < 0 ? "" : s[(ei + 1)..];
        if (mant.StartsWith('+')) mant = mant[1..];
        else if (mant.StartsWith('-')) { neg = true; mant = mant[1..]; }
        Int128 unscaled = 0; int frac = -1, digits = 0;
        foreach (char c in mant)
        {
            if (char.IsAsciiDigit(c)) { unscaled = unscaled * 10 + (c - '0'); digits++; if (frac >= 0) frac++; continue; }
            if (c == dec && frac < 0) { frac = 0; continue; }
            return Exceptions.ExceptionState.ArgumentError($"NUMVAL-F mantissa '{text}' is malformed (§15.69.3)");
        }
        if (digits == 0) return Exceptions.ExceptionState.ArgumentError($"NUMVAL-F '{text}' has no significand digit (§15.69.3)");
        if (frac < 0) frac = 0;
        int exp = 0;
        if (exps.Length > 0)
        {
            bool eneg = false;
            // §15.69.3 makes the sign after E mandatory ({+|-}, not bracketed) — TEST-NUMVAL-F enforces it too.
            if (exps.StartsWith('+')) exps = exps[1..];
            else if (exps.StartsWith('-')) { eneg = true; exps = exps[1..]; }
            else return Exceptions.ExceptionState.ArgumentError($"NUMVAL-F exponent '{text}' lacks the required sign after 'E' (§15.69.3)");
            if (exps.Length is 0 or > 4 || !exps.All(char.IsAsciiDigit))
                return Exceptions.ExceptionState.ArgumentError($"NUMVAL-F exponent '{text}' is malformed (§15.69.3)");
            exp = int.Parse(exps) * (eneg ? -1 : 1);                   // 1..4 exponent digits (§15.69.3)
        }
        int shift = scale + exp - frac;                               // the final decimal shift of the unscaled mantissa
        Int128 r = shift >= 0 ? unscaled * Pow10.AsWide(shift) : unscaled / Pow10.AsWide(-shift);
        if (neg) r = -r;
        return r > long.MaxValue ? long.MaxValue : r < long.MinValue ? long.MinValue : (long)r;
    }

    /// <summary>TEST-NUMVAL-F (§15.95, COBOL-2014+): 0 if the string conforms to the NUMVAL-F format; else the
    /// 1-based position of the first character in error (an embedded space inside the significand ⇒ the first
    /// non-space after it, r b.1; a native significand longer than 31 digits ⇒ the 32nd significand digit, r b.2);
    /// else (all spaces, empty, or a valid-but-incomplete string like <c>" +."</c>) LENGTH+1 (r c).</summary>
    public static long TestNumvalF(string text, bool commaMode = false)
    {
        char dec = commaMode ? ',' : '.';
        int n = text.Length, i = 0, sig = 0; bool anyDigit = false, sawDot = false, pendingSpace = false;
        int Pos() => i + 1;
        while (i < n && text[i] == ' ') i++;                          // leading spaces (r5)
        if (i < n && (text[i] == '+' || text[i] == '-')) i++;
        while (i < n && text[i] == ' ') i++;                          // spaces before the first digit (r5)
        for (; i < n; i++)                                            // significand: { digit [ . [digit] ] | . digit }
        {
            char c = text[i];
            if (char.IsAsciiDigit(c))
            {
                if (pendingSpace) return Pos();                       // r b.1 — first non-space after an interior space
                anyDigit = true; if (++sig > 31) return Pos();        // r b.2 — the 32nd significand digit
                continue;
            }
            if (c == dec && !sawDot) { if (pendingSpace) return Pos(); sawDot = true; continue; }
            if (c == ' ') { pendingSpace = true; continue; }          // trailing/interior space — decided by what follows
            break;                                                    // 'E' or a bad char — leave it to the exponent scan
        }
        if (!anyDigit) return n + 1;                                  // r c — no significand digit (incl. all-space / " +.")
        pendingSpace = false;
        if (i < n && (text[i] == 'E' || text[i] == 'e'))
        {
            i++;
            while (i < n && text[i] == ' ') i++;
            if (i >= n) return n + 1;                                 // r c — dangling E
            if (text[i] == '+' || text[i] == '-') i++; else return Pos();   // a sign is required after E (§15.69.3)
            while (i < n && text[i] == ' ') i++;
            int ed = 0;
            while (i < n && char.IsAsciiDigit(text[i])) { if (++ed > 4) return Pos(); i++; }   // n = 1..4 digits
            if (ed == 0) return n + 1;                                // r c — E± with no exponent digit
        }
        while (i < n && text[i] == ' ') i++;                          // trailing spaces (r5)
        return i == n ? 0 : Pos();                                    // any leftover char is in error
    }

    /// <summary>
    /// NUMVAL-C (§15.68): like NUMVAL with a currency string and grouping separators. The currency string —
    /// argument-2, or the SPECIAL-NAMES / default currency the BINDER injected when argument-2 is omitted
    /// (§15.68.3 rule 3) — is removed wherever it appears (leading/trailing spaces of argument-2 ignored, rule 2);
    /// grouping separators (',' normally; '.' under DECIMAL-POINT IS COMMA, rule 4d) are ignored (§15.68.4 rule 2);
    /// then the remainder parses exactly as NUMVAL (sign / CR / DB, rule 3).
    /// </summary>
    public static long NumvalC(string text, string currency, int scale, bool commaMode = false, bool anycase = false)
    {
        char group = commaMode ? '.' : ',';
        string cur = currency.Trim();
        // ANYCASE (§15.68.3 r4f): the currency match is performed as if both sides were lowercased per
        // LOWER-CASE — an ordinal-ignore-case removal realizes that correspondence for the invariant set.
        string s = cur.Length == 0 ? text
            : text.Replace(cur, "", anycase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        s = s.Replace(group.ToString(), "", StringComparison.Ordinal);
        // The currency may sit between the sign and the digits ("- $ 890.05"): removing it can leave interior
        // spaces after the sign, which Numval's sign-strip + TrimStart already ignores (§15.68.3 r4a's
        // space-strings around the currency).
        return Numval(s, scale, commaMode);
    }

    // ── The §15.93/§15.94 TEST validators — position-reporting scanners beside their value parsers ────────────

    /// <summary>TEST-NUMVAL (§15.93.4): 0 when argument-1 conforms to the §15.67.3 NUMVAL formats (r1a); else
    /// the 1-based ordinal position of the first character in error (r1b — an embedded space after the first
    /// digit reports the first NON-space character following it, sub-note 1: <c>"0 1"</c> → 3; the
    /// <paramref name="digitCap"/>+1-th digit reports its own position, sub-notes 2/4 — 31 native, 34
    /// standard-decimal [standard-binary's 35 rides the P12/P13 STANDARD-BINARY wave]); else — no specific
    /// character in error: zero-length, only spaces, or valid-but-incomplete like <c>" +."</c> —
    /// LENGTH+1 (r1c). A dedicated scanner beside <see cref="Numval"/> (the TestNumvalF discipline —
    /// positions are ordinal in the ORIGINAL string, so remove-and-delegate cannot work).</summary>
    public static long TestNumval(string text, bool commaMode = false, int digitCap = 31)
    {
        char dec = commaMode ? ',' : '.';
        int n = text.Length, i = 0, digits = 0;
        bool anyDigit = false, sawDot = false, leadSign = false;
        while (i < n && text[i] == ' ') i++;                          // leading space-string
        if (i < n && text[i] is '+' or '-') { leadSign = true; i++; } // format-A leading sign
        while (i < n && text[i] == ' ') i++;                          // spaces before the first digit (r2)
        for (; i < n; i++)                                            // { digit [ dec [digit] ] | dec digit }
        {
            char c = text[i];
            if (char.IsAsciiDigit(c))
            {
                anyDigit = true;
                if (++digits > digitCap) return i + 1;                // r1b sub-notes 2/4 — the cap+1-th digit
                continue;
            }
            if (c == dec && !sawDot) { sawDot = true; continue; }
            break;                                                    // the trailing region (or an error)
        }
        // No digit anywhere: a scan that BROKE on a real character reports that character (r1b — e.g. a
        // misplaced sign); a scan that ran off the end with only valid-but-incomplete content (" +.",
        // all-spaces, zero-length) is the r1c LENGTH+1 leg.
        if (!anyDigit) return i < n ? i + 1 : n + 1;
        while (i < n && text[i] == ' ') i++;                          // spaces before a trailing sign
        if (i < n)
        {
            char c = text[i];
            // Format-B trailing sign / CR / DB (any case, §15.67.3 r1) — only when no leading sign was taken
            // (the two formats are ALTERNATIVES).
            if (!leadSign && c is '+' or '-') i++;
            else if (!leadSign && i + 1 < n
                     && ((c is 'C' or 'c' && text[i + 1] is 'R' or 'r')
                         || (c is 'D' or 'd' && text[i + 1] is 'B' or 'b'))) i += 2;
            else return i + 1;                                        // r1b — first char in error ("0 1" → 3)
        }
        while (i < n && text[i] == ' ') i++;                          // trailing space-string
        return i == n ? 0 : i + 1;
    }

    /// <summary>TEST-NUMVAL-C (§15.94.4): the §15.93.4-shaped verdict over the §15.68.3 NUMVAL-C formats —
    /// <c>[sp] [sign] [sp] [currency] [sp] digits[,digits]… [dec [digits]] [sp]</c> (format A, sign BEFORE the
    /// currency) or <c>[sp] [currency] [sp] digits… [sp] [sign|CR|DB] [sp]</c> (format B, trailing sign). The
    /// currency (argument-2, or the binder-injected compilation-unit currency, §15.68.3 r3) matches character
    /// for character (r4a) — case-folded under ANYCASE (r4f); it appears at most once, BEFORE the digits (no
    /// trailing-currency or currency-then-sign form). Grouping separators are ARBITRARY-length digit groups
    /// (r4a's <c>digit [, digit]…</c> — no 3-digit constraint; <c>"1,23,4.5"</c> conforms) and are illegal
    /// after the decimal separator; DECIMAL-POINT IS COMMA SWAPS the two roles (r4d). Verdicts: 0 (r1a) /
    /// first-error position (r1b, same sub-notes as TEST-NUMVAL) / LENGTH+1 (r1c).</summary>
    public static long TestNumvalC(string text, string currency, bool commaMode = false, bool anycase = false,
        int digitCap = 31)
    {
        char dec = commaMode ? ',' : '.';
        char group = commaMode ? '.' : ',';
        string cur = currency.Trim();
        var cmp = anycase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int n = text.Length, i = 0, digits = 0;
        bool anyDigit = false, sawDot = false, leadSign = false, sawCur = false;
        bool AtCurrency() => cur.Length > 0 && !sawCur
            && i + cur.Length <= n && text.AsSpan(i, cur.Length).Equals(cur, cmp);
        while (i < n && text[i] == ' ') i++;                          // leading space-string
        if (i < n && text[i] is '+' or '-') { leadSign = true; i++; } // format-A sign — BEFORE the currency
        while (i < n && text[i] == ' ') i++;
        if (AtCurrency()) { sawCur = true; i += cur.Length; }         // the at-most-once currency (r4a)
        while (i < n && text[i] == ' ') i++;                          // spaces after the currency
        for (; i < n; i++)                                            // digit groups + one decimal separator
        {
            char c = text[i];
            if (char.IsAsciiDigit(c))
            {
                anyDigit = true;
                if (++digits > digitCap) return i + 1;                // r1b sub-notes 2/4
                continue;
            }
            if (c == group && !sawDot && anyDigit) continue;          // grouping only BEFORE the decimal (r2 of §15.68.4)
            if (c == dec && !sawDot) { sawDot = true; continue; }
            break;
        }
        if (!anyDigit) return i < n ? i + 1 : n + 1;                  // r1b at the offending char, else r1c
        while (i < n && text[i] == ' ') i++;
        if (i < n)
        {
            char c = text[i];
            if (!leadSign && c is '+' or '-') i++;                    // format-B trailing sign
            else if (!leadSign && i + 1 < n
                     && ((c is 'C' or 'c' && text[i + 1] is 'R' or 'r')
                         || (c is 'D' or 'd' && text[i + 1] is 'B' or 'b'))) i += 2;
            else return i + 1;                                        // r1b
        }
        while (i < n && text[i] == ' ') i++;
        return i == n ? 0 : i + 1;
    }
}
