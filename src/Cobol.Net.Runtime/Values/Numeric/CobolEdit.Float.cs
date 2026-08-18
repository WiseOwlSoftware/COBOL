// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using System.Text;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The FLOATING-POINT NUMERIC-EDITED form (ISO/IEC 1989:2023 §13.18.40.4 GR13 b — a PICTURE whose two parts are
/// separated by the symbol <c>E</c>: a significand that is a numeric or fixed numeric-edited string with no
/// floating insertion and no zero suppression, and an exponent <c>+9</c>…<c>+9999</c>) — data-model design D21,
/// kb/Work PB66. The item's storage is its character image (category numeric-edited, class alphanumeric); its
/// VALUE channel is EXACT DECIMAL: a store normalizes the sending value by integer arithmetic so the significand's
/// most significant digit is nonzero (§14.6.8.4 GR1) and edits it into the mask (Table 7: simple, special and
/// fixed insertion for the significand, none for the exponent; rule 8 for zero), and a de-editing read
/// (§14.9.25.4 GR5) yields a <see cref="CobolDec"/> — a significand and its own power of ten — never a
/// <c>double</c> (a 36-digit significand, §13.18.40.3 SR15, does not round-trip binary64).
/// </summary>
public static partial class CobolEdit
{
    /// <summary>The parsed two-part structure of a floating-point numeric-edited PICTURE (dot-canonical: a
    /// DECIMAL-POINT IS COMMA mask is swapped on the way in and its image swapped back on the way out).</summary>
    /// <param name="SigPattern">The significand's expanded character-string, sign symbol included, in dot-canonical form.</param>
    /// <param name="SigDigits">The '9' positions of the significand (§13.18.40.3 SR15: 1..36).</param>
    /// <param name="SigScale">The '9' positions right of the significand's '.' (0 when there is none).</param>
    /// <param name="SigSign">The significand's fixed-insertion sign symbol ('+', '-') or '\0' when unsigned.</param>
    /// <param name="ExpDigits">The '9' positions of the exponent (§13.18.40.4 GR13 b: 1..4).</param>
    public readonly record struct FloatMask(string SigPattern, int SigDigits, int SigScale, char SigSign, int ExpDigits)
    {
        /// <summary>The largest exponent magnitude the mask can hold (10^ExpDigits − 1).</summary>
        public int MaxExp => Pow10Int(ExpDigits) - 1;

        /// <summary>The whole item's character length: the significand's positions + 'E' + the exponent's sign + digits.</summary>
        public int Length => SigPattern.Length + 1 + 1 + ExpDigits;

        /// <summary>Parse an EXPANDED floating-point numeric-edited picture (repeats unrolled, uppercased — the
        /// analyzer's <c>EditMask</c>). The analyzer has already validated the form (§13.18.40.4 GR13 b, Table 10 row
        /// E); this reads the structure it guaranteed.</summary>
        public static FloatMask Parse(string picture, bool commaMode = false)
        {
            if (commaMode) picture = SwapSeparators(picture);
            int e = picture.IndexOf('E');
            if (e < 0) throw new ArgumentException($"not a floating-point numeric-edited picture: {picture}", nameof(picture));
            string sig = picture[..e];
            string exp = picture[(e + 1)..];
            char sign = sig.Length > 0 && sig[0] is '+' or '-' ? sig[0] : '\0';
            int digits = 0, scale = 0; bool afterPoint = false;
            foreach (char c in sig)
            {
                if (c == '9') { digits++; if (afterPoint) scale++; }
                else if (c == '.') afterPoint = true;
            }
            int expDigits = exp.Count(c => c == '9');
            return new FloatMask(sig, digits, scale, sign, expDigits);
        }
    }

    /// <summary>The disposition of a floating-point edited store — the caller (MOVE vs arithmetic) decides what
    /// each means (§14.9.25.4 GR6 item 4 vs §14.7.5 cases 3/4).</summary>
    public enum FloatStoreOutcome { Ok, Overflow, Underflow }

    /// <summary>The MOVE store (ISO §14.9.25.4 GR6 item 4): a value farther from zero than the mask permits sets
    /// EC-DATA-OVERFLOW (fatal when the statement has it enabled) and — the content being "undefined" — stores the
    /// PINNED saturated image (all-nines significand at the maximum exponent, the value's sign; docs/CONFORMANCE.md);
    /// a value nearer to zero than the smallest nonzero the mask can hold "is treated as zero" — the rule-8 zero image,
    /// no exception. <paramref name="value"/> × 10^−<paramref name="valueScale"/> is the sending value.</summary>
    public static string FormatFloatMove(Int128 value, int valueScale, string picture, bool blankWhenZero = false, bool commaMode = false)
        => FormatFloatMoveCore((BigInteger)value, -valueScale, picture, blankWhenZero, commaMode);

    /// <summary>The MOVE store of a <see cref="CobolDec"/>-carried sender (a standard-decimal intermediate or another
    /// floating-point edited item's de-edited value).</summary>
    public static string FormatFloatMove(CobolDec value, string picture, bool blankWhenZero = false, bool commaMode = false)
        => FormatFloatMoveCore((BigInteger)value.Sig, value.Exp, picture, blankWhenZero, commaMode);

    /// <summary>The MOVE store of a binary64 sender — through the shortest round-trip decimal (<see cref="CobolDec.FromDouble"/>).</summary>
    public static string FormatFloatMove(double value, string picture, bool blankWhenZero = false, bool commaMode = false)
        => FormatFloatMove(CobolDec.FromDouble(value), picture, blankWhenZero, commaMode);

    private static string FormatFloatMoveCore(BigInteger sig, int exp10, string picture, bool blankWhenZero, bool commaMode)
    {
        var m = FloatMask.Parse(picture, commaMode);
        string image = FormatFloatCore(sig, exp10, m, blankWhenZero, out var outcome);
        if (outcome == FloatStoreOutcome.Overflow)
            ExceptionState.FloatOverflowError($"the value {sig}E{exp10} is farther from zero than the picture {picture} permits");
        return commaMode ? SwapSeparators(image) : image;
    }

    /// <summary>The ARITHMETIC store (ISO §14.7.5 cases 3 and 4 — both the size error condition, receiver unchanged):
    /// false when the value is farther from zero OR nearer to zero than the mask permits (the caller raises the size
    /// error and leaves the receiver alone), else the edited image in <paramref name="image"/>.</summary>
    public static bool TryFormatFloat(Int128 value, int valueScale, string picture, out string image, bool blankWhenZero = false, bool commaMode = false)
        => TryFormatFloatCore((BigInteger)value, -valueScale, picture, out image, blankWhenZero, commaMode);

    /// <inheritdoc cref="TryFormatFloat(Int128, int, string, out string, bool, bool)"/>
    public static bool TryFormatFloat(CobolDec value, string picture, out string image, bool blankWhenZero = false, bool commaMode = false)
        => TryFormatFloatCore((BigInteger)value.Sig, value.Exp, picture, out image, blankWhenZero, commaMode);

    /// <inheritdoc cref="TryFormatFloat(Int128, int, string, out string, bool, bool)"/>
    public static bool TryFormatFloat(double value, string picture, out string image, bool blankWhenZero = false, bool commaMode = false)
        => TryFormatFloat(CobolDec.FromDouble(value), picture, out image, blankWhenZero, commaMode);

    private static bool TryFormatFloatCore(BigInteger sig, int exp10, string picture, out string image, bool blankWhenZero, bool commaMode)
    {
        var m = FloatMask.Parse(picture, commaMode);
        string img = FormatFloatCore(sig, exp10, m, blankWhenZero, out var outcome);
        image = commaMode ? SwapSeparators(img) : img;
        return outcome == FloatStoreOutcome.Ok;
    }

    /// <summary>The image of the mask's positive extreme (all-nines significand at the maximum exponent) — the value
    /// FUNCTION HIGHEST-ALGEBRAIC names (§15.43.4 r2) and the pinned overflow content.</summary>
    public static string FloatExtremeImage(string picture, bool negative, bool commaMode = false)
    {
        var m = FloatMask.Parse(picture, commaMode);
        string img = RenderFloat(m, negative, new string('9', m.SigDigits), m.MaxExp);
        return commaMode ? SwapSeparators(img) : img;
    }

    /// <summary>The core: normalize (§14.6.8.4 GR1) by exact integer arithmetic to the mask's significand digit
    /// count, decide the outcome against the exponent's capacity, render (§13.18.40.5 Table 7 + rule 8).</summary>
    private static string FormatFloatCore(BigInteger sig, int exp10, in FloatMask m, bool blankWhenZero, out FloatStoreOutcome outcome)
    {
        outcome = FloatStoreOutcome.Ok;
        if (sig.IsZero)
            return blankWhenZero ? new string(' ', m.Length) : RenderFloat(m, negative: false, new string('0', m.SigDigits), 0);
        bool negative = sig.Sign < 0;
        BigInteger a = BigInteger.Abs(sig);
        int d = DigitCount(a);
        // S = a × 10^k with exactly SigDigits digits (leading digit nonzero by construction; a division truncates —
        // §14.6.8.4 GR2 → §13.18.40's alignment / truncation rules); the exponent that keeps the value:
        // value = a × 10^exp10 = (S × 10^−SigScale) × 10^E  ⇒  E = d + exp10 + SigScale − SigDigits.
        int k = m.SigDigits - d;
        BigInteger s = k >= 0 ? a * BigInteger.Pow(10, k) : BigInteger.Divide(a, BigInteger.Pow(10, -k));
        int e = d + exp10 + m.SigScale - m.SigDigits;
        int maxExp = m.MaxExp;
        if (e > maxExp)
        {
            outcome = FloatStoreOutcome.Overflow;
            return RenderFloat(m, negative, new string('9', m.SigDigits), maxExp);   // the pinned saturated image
        }
        if (e < -maxExp)
        {
            outcome = FloatStoreOutcome.Underflow;
            return blankWhenZero ? new string(' ', m.Length) : RenderFloat(m, negative: false, new string('0', m.SigDigits), 0);
        }
        return RenderFloat(m, negative, s.ToString().PadLeft(m.SigDigits, '0'), e);
    }

    /// <summary>Render a normalized significand digit string and exponent into the mask (dot-canonical).</summary>
    private static string RenderFloat(in FloatMask m, bool negative, string sigDigits, int e)
    {
        var sb = new StringBuilder(m.Length);
        int next = 0;
        foreach (char c in m.SigPattern)
        {
            switch (c)
            {
                case '9': sb.Append(sigDigits[next++]); break;
                case '+': sb.Append(negative ? '-' : '+'); break;   // Table 8 fixed insertion
                case '-': sb.Append(negative ? '-' : ' '); break;
                case 'B': sb.Append(' '); break;                    // simple insertion
                case '.': case ',': case '0': case '/': sb.Append(c); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('E');
        sb.Append(e < 0 ? '-' : '+');
        sb.Append(Math.Abs(e).ToString().PadLeft(m.ExpDigits, '0'));
        return sb.ToString();
    }

    /// <summary>DE-EDIT a floating-point numeric-edited image (ISO §14.9.25.4 GR5): the significand's digit positions
    /// and sign, the 'E', the exponent's sign and digits, back to the exact value <c>±S × 10^(E − SigScale)</c>. Content
    /// that is not a possible result of any editing operation on the item (§14.6.13.2 rule 4) raises
    /// EC-DATA-INCOMPATIBLE — fatal when the statement has it enabled — and otherwise contributes zero for a
    /// non-digit (the tolerant direction the zoned/packed decoders take).</summary>
    public static CobolDec DeEditFloat(string image, string picture, bool commaMode = false)
    {
        var m = FloatMask.Parse(picture, commaMode);
        if (commaMode) image = SwapSeparators(image);
        bool incompatible = false, negative = false;
        BigInteger s = BigInteger.Zero;
        int i = 0;
        char At() => i < image.Length ? image[i] : '\0';
        foreach (char c in m.SigPattern)
        {
            char ch = At(); i++;
            switch (c)
            {
                case '9':
                    s = s * 10 + (char.IsAsciiDigit(ch) ? ch - '0' : 0);   // a non-digit contributes zero at its position
                    if (!char.IsAsciiDigit(ch)) incompatible = true;
                    break;
                case '+':
                    if (ch == '-') negative = true; else if (ch != '+') incompatible = true;
                    break;
                case '-':
                    if (ch == '-') negative = true; else if (ch != ' ') incompatible = true;
                    break;
                case 'B': if (ch != ' ') incompatible = true; break;
                default: if (ch != c) incompatible = true; break;   // '.', ',', '0', '/'
            }
        }
        if (At() != 'E') incompatible = true; i++;
        char es = At(); i++;
        bool expNeg = es == '-';
        if (es is not ('+' or '-')) incompatible = true;
        int e = 0;
        for (int k = 0; k < m.ExpDigits; k++)
        {
            char ch = At(); i++;
            if (char.IsAsciiDigit(ch)) e = e * 10 + (ch - '0'); else incompatible = true;
        }
        if (incompatible)
            ExceptionState.DataIncompatibleError($"the content '{image}' is not a possible result of editing into {picture}");
        if (expNeg) e = -e;
        // The significand's digits fit Int128 (≤ 36 digits, SR15).
        Int128 sig = (Int128)s;
        return new CobolDec(negative ? -sig : sig, e - m.SigScale);
    }

    private static int DigitCount(BigInteger a) => a.IsZero ? 1 : a.ToString().Length;

    private static int Pow10Int(int n) { int r = 1; for (int i = 0; i < n; i++) r *= 10; return r; }
}
