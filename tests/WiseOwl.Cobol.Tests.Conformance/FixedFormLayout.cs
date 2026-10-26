// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// Lays a program out in FIXED-form reference format, for a test whose subject is a literal (or any token) too long
/// for one line. ISO §6.1 3) a) bounds a FREE-form line at 255 character positions ("ranging from a minimum of 0 to a
/// maximum of 255"; kb/Work PB1496, COBOL2653), so an 8,192-position literal written on one free-form line is not
/// a program the standard allows — the tests that build one programmatically must continue it, as fixed form does
/// (§6.3.5 / §6.5 4) and 8)): the continued line fills to margin R and ends in the floating literal continuation
/// indicator, and the continuation line resumes the literal at a quotation symbol. §6.1 2) b): after logical conversion
/// the line may be as long as it
/// likes, which is exactly what the literal-length rule (§8.3.3.2.3 SR1 and its siblings) is asked about.
/// </summary>
internal static class FixedFormLayout
{
    private const int TextStart = 7;    // 0-based index of position 8, the first program-text position
    private const int MarginR = 72;     // 0-based length of a line filled to position 72

    /// <summary>The program <paramref name="logicalProgram"/> (one logical line per <c>\n</c>, indentation kept) as a
    /// fixed-form source that starts with its own <c>&gt;&gt;SOURCE FORMAT FIXED</c> directive, so no reference-format
    /// detection is involved. With <paramref name="classic"/> the layout is COBOL-85's own: no directive (the
    /// <c>&gt;&gt;</c> facility is a COBOL-2002 introduction) and a literal continued by the HYPHEN in position 7 of the
    /// continuation line (the fixed continuation indicator of §6.2.2, whose use §6.2.2 NOTE 1 calls obsolete from 2023; the continuation of §6.3.5 1)): the continued
    /// line is filled to position 72 with no closing quotation symbol and the next resumes the literal at a quotation
    /// symbol in position 12 — the one continuation a program targeting COBOL-85 can write (kb/Work PB1615).</summary>
    public static string Source(string logicalProgram, bool classic = false)
    {
        var sb = new StringBuilder(classic ? "" : ">>SOURCE FORMAT FIXED\n");
        foreach (string logical in logicalProgram.Split('\n'))
            foreach (string physical in LayLine(logical.TrimEnd(), classic))
                sb.Append(physical).Append('\n');
        return sb.ToString();
    }

    private static IEnumerable<string> LayLine(string text, bool classic)
    {
        int indent = text.Length - text.TrimStart().Length;
        var lines = new List<string>();
        string cur = new(' ', TextStart + indent);
        bool empty = true;
        foreach (string token in Tokenize(text.TrimStart()))
        {
            if (cur.Length + (empty ? 0 : 1) + token.Length <= MarginR)
            {
                cur += (empty ? "" : " ") + token;
                empty = false;
                continue;
            }
            if (!empty)
            {
                lines.Add(cur);
                cur = new string(' ', TextStart + 4);
                empty = true;
            }
            if (cur.Length + token.Length <= MarginR)
            {
                cur += token;
                empty = false;
                continue;
            }

            // A token longer than a line is a literal: fill the line to margin R and continue it with the FLOATING
            // literal continuation indicator — the opening quotation symbol and a hyphen end the line (§6.2.3.1, §6.5
            // 4)), and the next line begins with the quotation symbol (§6.2.3.2 SR6, §6.5 8)). It is the one form that
            // continues EVERY literal kind: a national literal may be continued only so (§6.3.5 2)), and the fixed
            // indicator is obsolete at 2023 (Annex F.2 item 4). A continued line never ends in content that is a
            // quotation symbol, which would read as the first half of a doubled one.
            char quote = token[token.IndexOfAny(['"', '\''])];
            string rest = token;
            while (cur.Length + rest.Length > MarginR)
            {
                if (classic)
                {
                    // §6.3.5 1) (fixed continuation indicator): fill to position 72, no closing quotation symbol; the continuation line carries the
                    // hyphen in position 7 and resumes the literal at a quotation symbol in position 12.
                    int fill = MarginR - cur.Length;
                    lines.Add(cur + rest[..fill]);
                    rest = rest[fill..];
                    cur = "      -" + new string(' ', TextStart + 4 - 7) + quote;
                    continue;
                }
                int take = MarginR - cur.Length - 2;   // two positions for the indicator
                if (rest[take - 1] == quote) take--;
                lines.Add(cur + rest[..take] + quote + "-");
                rest = rest[take..];
                cur = new string(' ', TextStart + 4) + quote;
            }
            cur += rest;
            empty = false;
        }
        lines.Add(cur);
        return lines;
    }

    /// <summary>The space-separated tokens of a line, a quoted literal being one token whatever it holds.</summary>
    private static IEnumerable<string> Tokenize(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == ' ') { i++; continue; }
            int j = i;
            char quote = '\0';
            while (j < s.Length && (s[j] != ' ' || quote != '\0'))
            {
                if (quote == '\0' && s[j] is '"' or '\'') quote = s[j];
                else if (quote != '\0' && s[j] == quote) quote = '\0';
                j++;
            }
            yield return s[i..j];
            i = j;
        }
    }
}
