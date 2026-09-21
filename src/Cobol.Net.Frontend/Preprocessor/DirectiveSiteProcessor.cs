// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>WHERE one compiler-directive line was written, in the FINAL line frame — so a rule about a
/// directive's POSITION can be decided against the parser's token lines.</summary>
/// <param name="Line">The 1-based resultant-text line, directly comparable to a token's <c>Start.Line</c>
/// (the <c>&gt;&gt;TURN</c> anchoring discipline — hazard H3).</param>
/// <param name="Word">The compiler-directive word, upper-cased (§7.3.3 SR6 / §8.12).</param>
public readonly record struct DirectiveSite(int Line, string Word);

/// <summary>
/// The POSITION-RULED compiler directives (ISO §7.3): the three whose syntax rules are about WHERE the directive
/// is written rather than what it does —
/// <list type="bullet">
/// <item>§7.3.25.3 SR5 — "A TURN directive shall not be specified within an exception processing PERFORM statement."</item>
/// <item>§7.3.22.3 SR4 — "The PUSH directive shall not be specified within an exception checking PERFORM statement."</item>
/// <item>§7.3.20.3 SR4 — "The POP directive shall not be specified within an exception checking PERFORM statement."</item>
/// </list>
/// This stage records each one's <see cref="DirectiveSite"/> on the FINAL text, which is the only frame in which
/// a directive line number and a parser token line number are the same number (COPY expansion is already done;
/// every stage from here on is line-count preserving). The binder's ONE lexical-containment predicate then
/// decides all three bans — owner decision D20 (2026-07-19): a flat ban, a suppressible conformance warning, and
/// <b>one shared predicate for TURN/PUSH/POP</b>, never a second region-partitioned mechanism (kb/Work PB595).
///
/// <para>It also CONSUMES the two lines no other stage owns. <c>&gt;&gt;PUSH</c> and <c>&gt;&gt;POP</c> were
/// blanked by the conditional-compilation driver, which runs BEFORE COPY expansion has settled the line frame and
/// therefore cannot say which resultant line a directive ended on; they are now left in the text by that driver
/// (<c>Frontend.LeftDirectives</c>) and blanked here instead, at the point where their position is knowable. The
/// directive-state SEMANTICS of §7.3.20 / §7.3.22 (save and restore the directive state) remain unimplemented —
/// unchanged by this stage, which alters no behaviour beyond recording the sites — and this is where they land.
/// <c>&gt;&gt;TURN</c>'s line is NOT blanked here: <see cref="TurnDirectiveProcessor"/> owns that directive's
/// parse, its syntax rules and its blanking, and this stage runs just before it.</para>
/// </summary>
public static class DirectiveSiteProcessor
{
    /// <summary>The directive words whose syntax rules are about the directive's POSITION. ⛔ ONE set: a fourth
    /// such rule is one entry here, and the binder's predicate covers it with no new mechanism.</summary>
    public static readonly IReadOnlySet<string> PositionRuled =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TURN", "PUSH", "POP" };

    /// <summary>Of those, the ones NO dedicated stage owns, so this stage consumes the line (blank, never
    /// delete — line-count preserving, hazard H3).</summary>
    private static readonly IReadOnlySet<string> Consumed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PUSH", "POP" };

    /// <summary>Record the position-ruled directive sites on <paramref name="text"/> and blank the lines this
    /// stage consumes. Line-count preserving.</summary>
    public static (string Text, IReadOnlyList<DirectiveSite> Sites) Process(string text)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, []);
        var lines = text.Split('\n');
        List<DirectiveSite>? sites = null;
        bool blanked = false;
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794) — the indicator's optional space and the
            // trailing inline comment are its rules, not this stage's.
            if (!CompilerDirectiveLine.TryParse(lines[i], out var directive)) continue;
            if (!PositionRuled.Contains(directive.Word)) continue;
            (sites ??= []).Add(new DirectiveSite(i + 1, directive.Word));
            if (!Consumed.Contains(directive.Word)) continue;
            lines[i] = "";   // blank, never delete — line-count preserving (H3)
            blanked = true;
        }
        if (sites is null) return (text, []);
        return (blanked ? string.Join('\n', lines) : text, sites);
    }
}
