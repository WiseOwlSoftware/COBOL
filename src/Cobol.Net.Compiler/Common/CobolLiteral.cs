// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Common;

/// <summary>
/// The ONE COBOL string-literal codec (ISO/IEC 1989:2023 §8.3.1.2 — the quotation-mark (<c>"…"</c>) and
/// apostrophe (<c>'…'</c>) delimiter forms are EQUAL-STANDING; the delimiters are not part of the value, and a
/// doubled OPENING delimiter inside is one embedded delimiter). National / boolean literals (<c>N"…"</c>/<c>B"…"</c>
/// and their apostrophe forms, ISO §8.3.3.5/§8.3.3.4) carry the prefix letter as part of the token. This single
/// codec (rearchitecture PHASE 05, Step 1) replaces the three per-layer <c>DecodeCobolString</c>/<c>DecodeString</c>
/// twins AND — the confirmed silent-miscompile fix — the hard-coded <c>'"'</c>-only guards that used to gate whether
/// to decode, so an apostrophe-delimited <c>VALUE 'x'</c> no longer falls through to raw text (DESIGN-data-model §2.8).
/// </summary>
public static class CobolLiteral
{
    /// <summary>True when <paramref name="raw"/> is a quoted string literal in EITHER ISO delimiter, optionally
    /// with an <c>N</c>/<c>B</c> prefix letter (§8.3.3.4/.5). This is the delimiter-agnostic replacement for the
    /// former <c>raw[0] == '"'</c> guards.</summary>
    public static bool IsStringLiteral(string raw)
    {
        string body = raw;
        if (body.Length >= 3 && body[0] is 'N' or 'n' or 'B' or 'b' && body[1] is '"' or '\'')
            body = body[1..];
        return body.Length >= 2 && body[0] is '"' or '\'' && body[^1] == body[0];
    }

    /// <summary>Decode a <c>STRINGLIT</c> (or an <c>N</c>/<c>B</c>-prefixed national/boolean literal) to its
    /// character value; returns <paramref name="raw"/> unchanged when it is not a quoted literal. Unwraps either
    /// delimiter and collapses a doubled opening delimiter to one embedded delimiter (§8.3.1.2). Body ported
    /// verbatim from the retired <c>EmitText.DecodeCobolString</c> twin.</summary>
    public static string Decode(string raw)
    {
        if (raw.Length >= 3 && raw[0] is 'N' or 'n' or 'B' or 'b' && raw[1] is '"' or '\'')
            raw = raw[1..];
        return raw.Length >= 2 && raw[0] is '"' or '\'' && raw[^1] == raw[0]
            ? raw[1..^1].Replace(new string(raw[0], 2), raw[0].ToString())
            : raw;
    }

    /// <summary>If <paramref name="raw"/> is the figurative <c>ALL "literal"</c> / <c>ALL 'literal'</c> form (a
    /// VALUE / level-88 operand text), the decoded literal; otherwise <see langword="null"/> (e.g. <c>ALL ZEROS</c>,
    /// a figurative word, is handled elsewhere). Tolerant of whether the front-end preserved the space between
    /// <c>ALL</c> and the literal; delimiter-agnostic (the former <c>'"'</c>-only guard was the miscompile).</summary>
    public static string? AllLiteralText(string raw)
    {
        string t = raw.TrimStart();
        if (t.Length < 3 || !t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = t[3..].TrimStart();
        return IsStringLiteral(rest) ? Decode(rest) : null;
    }
}
