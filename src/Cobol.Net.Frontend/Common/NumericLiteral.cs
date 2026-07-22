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
}
