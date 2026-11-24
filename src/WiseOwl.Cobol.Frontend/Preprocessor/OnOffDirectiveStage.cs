// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// The ONE collection loop of an <c>{ ON | OFF }</c> toggle directive whose ON is un-underlined (<c>&gt;&gt;LEAP-SECOND</c>
/// §2023:7.3.17.2, <c>&gt;&gt;PROPAGATE</c> §2023:7.3.21.2, <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> §2023:7.3.23.2; kb/Work PB2156,
/// census R0-0054): each line the directive parse recognizes as the directive becomes one toggle event, in line order,
/// and the line is blanked, line-count preserving (the <c>&gt;&gt;TURN</c> H3 discipline). The PUSH/POP program of the
/// same text is replayed over the events (§2023:7.3.20 / §2023:7.3.22; kb/Work PB941).
/// <para>⛔ It takes NO diagnostic channel: the EDITION question was answered at the directive-recognition point
/// (<c>CompilerDirectiveCatalog</c>, kb/Work PB725), the OPERAND question by the row's <c>directiveOperand</c> column
/// (COBOL1911, kb/Work PB794) and the PLACEMENT question by <c>DirectivePlacementPass</c> (kb/Work PB1378), so a malformed
/// operand was reported there and adds no event here. Each directive's own stage keeps its keyword, its registry row
/// and its event type; the loop is this one.</para>
/// </summary>
internal static class OnOffDirectiveStage
{
    /// <summary>Collect the toggles of the directive <paramref name="keyword"/> (registry row <paramref name="row"/>)
    /// from <paramref name="text"/>, building each with <paramref name="toggle"/> from its 1-based line and its ON
    /// (written, or implied by the bare directive) versus OFF, and blank the directive lines.</summary>
    public static (string Text, DirectiveTimeline<TEvent> Events) Collect<TEvent>(
        string text, string keyword, string row, Func<int, bool, TEvent> toggle, IReadOnlyList<DirectiveStackOp>? stackOps)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, DirectiveTimeline<TEvent>.Empty);
        var lines = text.Split('\n');
        var events = new DirectiveEventLog<TEvent>();
        for (int i = 0; i < lines.Length; i++)
        {
            // The ONE compiler-directive line parse (kb/Work PB794): it removes the §2023:7.3.3 SR3/SR4 inline comment,
            // so `>>LEAP-SECOND ON *> on` folds ON instead of drawing a malformed-operand error.
            if (!CompilerDirectiveLine.TryParse(lines[i], keyword, out string operand)) continue;
            if (CompilerDirectiveCatalog.TryOperandWord(keyword, operand, out string word)
                && word is "" or "ON" or "OFF")
                events.Add(row, i + 1, toggle(i + 1, word != "OFF"));   // ON is un-underlined, so the bare directive selects it
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), events.ToTimeline(stackOps ?? []));
    }
}
