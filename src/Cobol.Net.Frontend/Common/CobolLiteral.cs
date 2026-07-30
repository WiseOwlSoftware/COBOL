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
    /// <summary>The prefix letters a quoted literal may carry: <c>N</c> national (§8.3.3.5), <c>B</c> boolean
    /// (§8.3.3.4), <c>X</c> hexadecimal-format alphanumeric (§8.3.3.2).</summary>
    /// <remarks>
    /// ⛔ ONE LIST, read by both <see cref="IsStringLiteral"/> and <see cref="Decode"/>. It was written down in
    /// both of them, and BOTH copies omitted <c>X</c> — so a hexadecimal literal was neither recognised as a
    /// literal nor decoded as one, and every caller that pairs those two methods (all of them) mishandled it in
    /// two different ways at once. Adding a prefix is now an edit to this string.
    /// </remarks>
    private const string PrefixLetters = "NnBbXx";

    /// <summary>The prefix letter (upper-cased, <c>'\0'</c> when none) and the quoted body of a literal, or
    /// <see langword="null"/> when <paramref name="raw"/> is not a quoted literal at all.</summary>
    private static (char Prefix, string Body)? SplitLiteral(string raw)
    {
        string body = raw;
        char prefix = '\0';
        if (body.Length >= 3 && PrefixLetters.Contains(body[0]) && body[1] is '"' or '\'')
        {
            prefix = char.ToUpperInvariant(body[0]);
            body = body[1..];
        }
        return body.Length >= 2 && body[0] is '"' or '\'' && body[^1] == body[0] ? (prefix, body) : null;
    }

    /// <summary>True when <paramref name="raw"/> is a quoted literal in EITHER ISO delimiter, optionally with an
    /// <c>N</c>/<c>B</c>/<c>X</c> prefix letter. This is the delimiter-agnostic replacement for the former
    /// <c>raw[0] == '"'</c> guards.</summary>
    /// <remarks>
    /// A HEXADECIMAL literal answers true, because §8.3.3.2 makes it one FORM of an alphanumeric literal rather
    /// than a separate kind of thing. Every caller pairs this with <see cref="Decode"/>, so the two agreeing is
    /// what makes <c>VALUE ALL X"41"</c> initialize to <c>AAAA</c> instead of to the characters <c>ALLX</c>.
    /// </remarks>
    public static bool IsStringLiteral(string raw) => SplitLiteral(raw) is not null;

    /// <summary>Decode a <c>STRINGLIT</c> (or an <c>N</c>/<c>B</c>/<c>X</c>-prefixed national, boolean or
    /// HEXADECIMAL literal) to its character value; returns <paramref name="raw"/> unchanged when it is not a
    /// quoted literal. Unwraps either delimiter and collapses a doubled opening delimiter to one embedded
    /// delimiter (§8.3.1.2). Body ported verbatim from the retired <c>EmitText.DecodeCobolString</c> twin.</summary>
    /// <remarks>
    /// ⛔ THE <c>X</c> ARM IS THE FOURTH COPY OF A DISPATCH DA3 FOUND THREE OF, and it belongs here rather than
    /// at the call sites. This decoder handled the <c>N</c> and <c>B</c> prefixes and silently returned a
    /// hexadecimal literal AS ITS OWN SOURCE TEXT, so every caller that did not separately think to call
    /// <see cref="DecodeHex"/> got the characters <c>X"4142"</c> where the standard gives <c>AB</c>. The VALUE
    /// path was exactly such a caller: <c>01 B PIC X(2) VALUE X"4142"</c> initialized the item to <c>X"</c> — the
    /// literal's source text, truncated to the picture — with no diagnostic, while <c>MOVE X"4142" TO B</c>
    /// correctly stored <c>AB</c>. Silent data corruption, and the two paths disagreed with each other.
    /// <para>
    /// Adding a hex arm at <c>ValueInitializer</c> would have made it the fifth copy. §8.3.3.2 makes a
    /// hexadecimal literal one FORM of an alphanumeric literal, not a separate kind of thing, so the decoder that
    /// owns "literal text → characters" owns this too (<c>feedback_one_rule_one_place</c>).
    /// </para>
    /// <para>
    /// ⚠ Delegation is keyed on the parsed prefix from <see cref="SplitLiteral"/>, never on a leading <c>X</c>
    /// alone: <see cref="DecodeHex"/> returns the empty string for anything it does not recognise, so a bare
    /// prefix test would turn an ordinary unquoted word beginning with X — which this method contracts to return
    /// unchanged — into "".
    /// </para>
    /// </remarks>
    public static string Decode(string raw)
    {
        if (SplitLiteral(raw) is not { } lit) return raw;
        if (lit.Prefix == 'X') return DecodeHex(raw);
        char q = lit.Body[0];
        return lit.Body[1..^1].Replace(new string(q, 2), q.ToString());
    }

    /// <summary>Decode an <c>X"…"</c>/<c>X'…'</c> hexadecimal-format alphanumeric literal (ISO §8.3.3.2 —
    /// each pair of hexadecimal digits is one character) to its character value; an odd digit count (a lexer
    /// impossibility for a well-formed token, but tolerated) or a non-hex shape yields the empty string. The
    /// ONE hex decoder (P10 Step 14) — the former <c>OoBinder.OoDecodeMethodNameLiteral</c> inline copy now
    /// routes here, as does the §8.8.3 concatenation fold.</summary>
    public static string DecodeHex(string raw)
    {
        if (raw.Length < 3 || raw[0] is not ('X' or 'x')) return "";
        char q = raw[^1];
        int open = raw.IndexOf(q);
        if (open < 0 || open >= raw.Length - 1) return "";
        string digits = raw[(open + 1)..^1];
        if (digits.Length % 2 != 0) return "";
        var chars = new char[digits.Length / 2];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)Convert.ToInt32(digits.Substring(i * 2, 2), 16);
        return new string(chars);
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
