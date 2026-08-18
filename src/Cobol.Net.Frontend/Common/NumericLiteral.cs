// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Common;

/// <summary>Which decimal-separator rule a numeric literal violated, if any (ISO/IEC 1989:2023 §12.3.7 GR14a /
/// §8.3.3.3.2). Returned by <see cref="NumericLiteral.Normalize"/> so each caller can route the diagnostic to its
/// own channel with its own code — the algorithm stays a single pure function.</summary>
public enum NumericSeparatorIssue
{
    /// <summary>The separator matched the active mode — no violation.</summary>
    None,

    /// <summary>Under DECIMAL-POINT IS COMMA the literal used a '.' where the comma is the decimal separator.</summary>
    DecimalPointUnderCommaMode,

    /// <summary>Without DECIMAL-POINT IS COMMA the literal used a ',' decimal separator (only '.' is admitted).</summary>
    CommaWithoutCommaMode,
}

/// <summary>
/// The ONE numeric-literal normalizer (ISO/IEC 1989:2023 §12.3.7 GR14a: under DECIMAL-POINT IS COMMA "the
/// character written in numeric literals to represent the decimal separator shall be the comma"; §8.3.3.3.2
/// admits ONLY the decimal point as the separator in a fixed-point literal). It converts a numeric literal's
/// source text to the canonical dot-decimal form the whole decode pipeline consumes and reports — via the returned
/// <see cref="NumericSeparatorIssue"/>, not by emitting anything itself — whether the source used the wrong
/// separator for the active mode.
///
/// This is the algorithm that used to live inline in <c>DataBinder.NormalizeNumericLiteral</c>; it is lifted to
/// the Frontend so BOTH the compiler binder (which keeps emitting COBOLNET0895 with its own descriptor) AND the
/// compile-time expression evaluator (which routes the issue to a frontend code) share ONE normalization rule —
/// no second, drifting normalizer (singular-pattern).
/// </summary>
public static class NumericLiteral
{
    /// <summary>Normalize <paramref name="text"/> to canonical dot-decimal, reporting any §12.3.7 GR14a separator
    /// violation for the active <paramref name="decimalPointIsComma"/> mode through <paramref name="issue"/>. The
    /// returned text is always well-formed dot-decimal (a violation is normalized too, so downstream decode stays
    /// well-formed while the caller emits the diagnostic).</summary>
    public static string Normalize(string text, bool decimalPointIsComma, out NumericSeparatorIssue issue)
    {
        if (decimalPointIsComma)
        {
            issue = text.Contains('.') ? NumericSeparatorIssue.DecimalPointUnderCommaMode : NumericSeparatorIssue.None;
            return text.Replace(',', '.');
        }
        issue = text.Contains(',') ? NumericSeparatorIssue.CommaWithoutCommaMode : NumericSeparatorIssue.None;
        return text.Replace(',', '.');
    }

    /// <summary>True when <paramref name="text"/> (canonical dot-decimal) has the FLOATING-POINT numeric literal form
    /// of ISO §8.3.3.3.3: a fixed-point significand (optional sign, digits, at most one decimal point), the letter E,
    /// and an optionally signed integer exponent — <c>1.5E+3</c>, <c>-2.5E-2</c>. Fixed-point text and any
    /// non-numeric operand return false. (Whether the significand carries the decimal point the standard requires is
    /// the lexer's question; a text of this shape only ever arrives from a numeric-literal token.)</summary>
    public static bool IsFloatingPointForm(string text)
    {
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        if (e <= 0 || e == t.Length - 1) return false;
        return IsFixedPointForm(t[..e], allowPoint: true) && IsFixedPointForm(t[(e + 1)..], allowPoint: false);

        static bool IsFixedPointForm(string s, bool allowPoint)
        {
            int i = 0;
            if (i < s.Length && s[i] is '+' or '-') i++;
            bool digit = false, point = false;
            for (; i < s.Length; i++)
            {
                if (char.IsAsciiDigit(s[i])) { digit = true; continue; }
                if (s[i] == '.' && allowPoint && !point) { point = true; continue; }
                return false;
            }
            return digit;
        }
    }

    /// <summary>The EXACT value of a floating-point numeric literal (ISO §8.3.3.3.3 GR5: "the algebraic product of
    /// the value of its significand and the quantity derived by raising ten to the power of the exponent") as a
    /// canonical fixed-point literal text — <c>1.5E+3</c> → <c>1500</c>, <c>-1.234E-5</c> → <c>-0.00001234</c>,
    /// <c>0.0E+0</c> → <c>0.0</c>. The ONE expansion the VALUE-clause pipeline applies when a floating-point literal
    /// seeds a FIXED-POINT numeric subject (§13.18.63.3 SR2 — a numeric literal whose value shall be representable
    /// exactly, checked downstream on the expanded text). <paramref name="text"/> shall satisfy
    /// <see cref="IsFloatingPointForm"/>.</summary>
    public static string ExpandFloatingPoint(string text)
    {
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        string sig = t[..e], exps = t[(e + 1)..];
        bool neg = sig.StartsWith('-');
        sig = sig.TrimStart('+', '-');
        int exp = int.Parse(exps, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
        int dot = sig.IndexOf('.');
        string digits = dot < 0 ? sig : sig.Remove(dot, 1);
        int scale = dot < 0 ? 0 : sig.Length - dot - 1;
        int newScale = scale - exp;                       // digits × 10^-scale × 10^exp = digits × 10^-(scale - exp)
        string body;
        if (newScale <= 0) body = digits + new string('0', -newScale);
        else if (newScale >= digits.Length) body = "0." + new string('0', newScale - digits.Length) + digits;
        else body = digits[..^newScale] + "." + digits[^newScale..];
        // canonical: no superfluous leading integer zeros (keep one before a point or alone), no trailing zeros
        // stripped (the fraction digits are the literal's scale — a VALUE 1.50E+0 keeps its two decimals like 1.50)
        int ip = body.IndexOf('.') is >= 0 and var d ? d : body.Length;
        int lead = 0;
        while (lead < ip - 1 && body[lead] == '0') lead++;
        body = body[lead..];
        return neg ? "-" + body : body;
    }
}
