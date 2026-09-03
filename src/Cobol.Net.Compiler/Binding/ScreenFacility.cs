// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR REFUSING ANNEX A.4.2 SCREEN HANDLING (kb/Work PB260) — the module docs/CONFORMANCE.md §5
/// records as <b>Not claimed</b>, and the largest declined module in the standard (27 listed elements, ~150
/// traceability rows).
///
/// <para><b>The licence, and why a refusal rather than a warning.</b> A.4.1: "An implementation shall accept the
/// syntax and provide the functionality for an optional element only when support for that language element is
/// claimed by the implementor." That is a prohibition on ACCEPTING, which is why the whole module is an ERROR
/// here and not the §4.2.6 recognize-and-warn the Annex A.3 processor-dependent facilities get (COBOLNET1578
/// MCS, 1579 COMMIT/ROLLBACK, 1580 VALIDATE). A.4.1 ¶2 — "Any associated syntax rules, general rules, other
/// rules, exception conditions, and I-O status values are also optional, even if not explicitly listed" —
/// extends the licence from the 27 printed items to the clauses' rules and to the EC-SCREEN family.</para>
///
/// <para><b>What this replaced.</b> Before it, ONE of the module's six source shapes was diagnosed — the SCREEN
/// SECTION header, and by a bare <c>"COBOLNET1560"</c> string literal with no descriptor, so the code had no
/// docs/DIAGNOSTICS.md row and the catalog still called its band "earmarked". The other five were a SILENT
/// REINTERPRETATION as a supported format (<c>ACCEPT screen-name</c> and <c>DISPLAY screen-name</c> are
/// token-identical to their DEVICE formats and bound as those — a declined facility that compiled and moved the
/// wrong data), a SILENT ACCEPT (the SPECIAL-NAMES CURSOR / CRT STATUS clauses parsed and no binder read them;
/// the four EC-SCREEN names sit in ExceptionCatalog with no raise site and no reader), or a generic COBOL0001.
/// </para>
///
/// <para><b>The clause table is the mechanism, not a list.</b> Every alternative of the grammar's
/// <c>screenClause</c> rule has a row here naming the clause and its ISO §, and every site passes that name to
/// <see cref="EditionContext.Declined"/> as the <c>seen</c> phrase, so the emitted message OPENS with what the
/// user wrote — <c>COBOLNET1560: the AUTO clause (ISO §13.18.3): A SCREEN SECTION construct …</c> — because a
/// witness that could only assert "some screen thing was refused" would pass for the wrong reason on every one
/// of the nineteen clauses. <c>ScreenFacilityConstructDriftTests</c> reads <c>CobolScreen.g4</c> and fails when
/// an alternative has no row, so a clause added to the grammar cannot ship un-named
/// (feedback_measure_the_selectors_complement).</para>
/// </summary>
internal static class ScreenFacility
{
    /// <summary>One screen construct: the phrase the diagnostic opens with (the <c>seen</c> half handed to
    /// <see cref="EditionContext.Declined"/>), and the ISO clause that defines it. The phrase is the witness
    /// discriminator — a negative golden's <c>.err</c> reads <c>COBOLNET1560: the AUTO clause</c>, which no
    /// other site can produce.</summary>
    private readonly record struct Fact(string Phrase, string Iso);

    /// <summary>Grammar rule name → the construct it recognizes. Keyed by the rule NAME (not the generated rule
    /// INDEX) so the drift test can compare it against the <c>.g4</c> text directly.</summary>
    private static readonly Dictionary<string, Fact> ClauseFacts = new(StringComparer.Ordinal)
    {
        // The screen-only clauses (Annex A.4.2 items 2-7, 12-19, 23, 26, 27 — each optional WHOLE).
        ["screenAutoClause"]            = new("the AUTO clause", "13.18.3"),
        ["screenBackgroundColorClause"] = new("the BACKGROUND-COLOR clause", "13.18.4"),
        ["screenBellClause"]            = new("the BELL clause", "13.18.6"),
        ["screenBlankClause"]           = new("the BLANK clause", "13.18.7"),
        ["screenBlinkClause"]           = new("the BLINK clause", "13.18.9"),
        ["screenColumnClause"]          = new("the COLUMN clause, format 2 (screen item)", "13.18.14"),
        ["screenEraseClause"]           = new("the ERASE clause", "13.18.21"),
        ["screenForegroundColorClause"] = new("the FOREGROUND-COLOR clause", "13.18.23"),
        ["screenFromClause"]            = new("the FROM clause", "13.18.25"),
        ["screenFullClause"]            = new("the FULL clause", "13.18.26"),
        ["screenHighlightClause"]       = new("the HIGHLIGHT clause", "13.18.30"),
        ["screenLineClause"]            = new("the LINE clause, format 2 (screen item)", "13.18.35"),
        ["screenLowlightClause"]        = new("the LOWLIGHT clause", "13.18.36"),
        ["screenRequiredClause"]        = new("the REQUIRED clause", "13.18.47"),
        ["screenReverseVideoClause"]    = new("the REVERSE-VIDEO clause", "13.18.48"),
        ["screenSecureClause"]          = new("the SECURE clause", "13.18.50"),
        ["screenToClause"]              = new("the TO clause", "13.18.56"),
        ["screenUnderlineClause"]       = new("the UNDERLINE clause", "13.18.59"),
        // ⚠ A.4.2 OMITS THE USING CLAUSE, and that is a defect in the standard, not a licence gap. §13.18.61.1:
        // "The USING clause identifies data to be used both as the destination in an ACCEPT screen statement and
        // the source for a DISPLAY screen statement" — it is purely screen and is reachable only from a screen
        // description entry, which item 20 makes optional in BOTH its formats. The licence is item 20 (the entry
        // has no other syntactic context), NOT A.4.1 ¶2, whose "even if not explicitly listed" attaches to the
        // RULES hanging off a listed element and cannot make an unlisted CLAUSE optional. The LINE clause format
        // 2 is omitted from the printed list the same way and rides the same licence.
        ["screenUsingClause"]           = new("the USING clause", "13.18.61"),
        // The clauses a screen description entry SHARES with a data description entry. They are refused HERE
        // because the entry that contains them has no other syntactic context (§13.17 — item 20), never because
        // the clause itself is optional: PICTURE, VALUE, OCCURS, USAGE, SIGN, JUSTIFIED, BLANK WHEN ZERO and
        // GLOBAL are all core language everywhere else, and nothing in this file may be read as declining them.
        ["pictureClause"]               = new("the PICTURE clause of a screen description entry", "13.18.40"),
        ["valueClause"]                 = new("the VALUE clause of a screen description entry", "13.18.63"),
        ["occursClause"]                = new("the OCCURS clause of a screen description entry", "13.18.38"),
        ["usageClause"]                 = new("the USAGE clause of a screen description entry", "13.18.60"),
        ["signClause"]                  = new("the SIGN clause of a screen description entry", "13.18.52"),
        ["justifiedClause"]             = new("the JUSTIFIED clause of a screen description entry", "13.18.32"),
        ["blankWhenZeroClause"]         = new("the BLANK WHEN ZERO clause of a screen description entry", "13.18.8"),
        ["globalClause"]                = new("the GLOBAL clause of a screen description entry", "13.18.27"),
    };

    /// <summary>The rule names the drift test expects to find as <c>screenClause</c> alternatives.</summary>
    internal static IReadOnlyCollection<string> CoveredClauseRules => ClauseFacts.Keys;

    // ── The data / environment division surface — COBOLNET1560 ────────────────────────────────────────────────

    /// <summary>Refuse a SCREEN SECTION (ISO §13.9) and everything in it: the section header, each screen
    /// description entry (§13.17) that names a screen-name (the entry-name clause format 2, §13.18.20), and each
    /// clause written inside one. One diagnostic per DISTINCT clause kind, not per occurrence — a fifty-item
    /// screen record should not print fifty copies of the same refusal, and the witness only needs the kind.
    /// <para>Returns the declared screen-names so the reference sites can suppress the §8.4.2.1 UNDEFINED report:
    /// "declared in a refused section" and "not defined at all" are different verdicts, and only the first is
    /// true here (kb/Work R32 — the differential's <c>syn_screen:221</c> flip came from conflating them).</para>
    /// </summary>
    public static void ReportSection(EditionContext edition, Core.ScreenSectionContext scr, ISet<string> screenNames)
    {
        using (edition.At(scr))
            edition.Declined(DiagnosticCatalog.ScreenFacilityUnsupported,
                "the SCREEN SECTION (ISO §13.9; Annex A.4.2 items 8 and 22)");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in scr.screenDescriptionEntry())
        {
            if (entry.screenName()?.cobolWord()?.GetText() is { } sn) screenNames.Add(sn);
            foreach (var clause in entry.screenDescriptionBody()?.screenClause() ?? [])
                ReportClause(edition, clause, seen);
        }
    }

    /// <summary>Name the ONE clause a <c>screenClause</c> node matched. The rule name is read off the matched
    /// child context, so the table above is the only place a clause's identity is written down.</summary>
    private static void ReportClause(EditionContext edition, Core.ScreenClauseContext clause, HashSet<string> seen)
    {
        if (clause.GetChild(0) is not ParserRuleContext inner) return;
        string rule = Core.ruleNames[inner.RuleIndex];
        if (!ClauseFacts.TryGetValue(rule, out var fact) || !seen.Add(rule)) return;
        using var _ = edition.At(inner);
        edition.Declined(DiagnosticCatalog.ScreenFacilityUnsupported, $"{fact.Phrase} (ISO §{fact.Iso})");
    }

    /// <summary>Refuse the SPECIAL-NAMES CURSOR and CRT STATUS clauses (ISO §12.3.7; Annex A.4.2 item 25). Both
    /// parsed and were read by NO binder at all before this — <c>CRT STATUS IS WS-CRT</c> compiled clean with
    /// zero diagnostics of any kind, which is the exact shape a "documented non-support" verdict may not have.
    /// The CRT status is the §9.2.3 conceptual entity an ACCEPT screen statement sets; with no screen statement
    /// it can never be written, so a program reading it would silently see its VALUE clause content forever.</summary>
    public static void ReportCursorClause(EditionContext edition, Core.CursorClauseContext cur)
    {
        using var _ = edition.At(cur);
        edition.Declined(DiagnosticCatalog.ScreenFacilityUnsupported,
            "the SPECIAL-NAMES CURSOR clause (ISO §12.3.7; Annex A.4.2 item 25) — the cursor position it names "
            + "is set only by an ACCEPT screen statement");
    }

    /// <summary>Refuse the OPTIONS paragraph's <c>INITIALIZE … SCREEN …</c> target (ISO §11.9.10.4 GR3).
    /// <para>⛔ ONLY THE EXPLICIT LEG. GR1 makes <c>ALL</c> imply LOCAL-STORAGE, SCREEN and WORKING-STORAGE, and
    /// two of those three are supported — so <c>INITIALIZE ALL SECTION TO SPACES</c> is legal source and stays
    /// legal. The standard wrote GR1 and GR3 as separate rules; this refusal follows that split exactly.</para>
    /// <para>This was the module's SEVENTH source shape and neither kb/Work PB260 nor the A.4.2 selector's
    /// witness object named it: the clause parsed, <c>OptionsBinder</c> set <c>OptionsSections.Screen</c>, and
    /// nothing anywhere read the flag.</para></summary>
    public static void ReportOptionsInitializeScreen(EditionContext edition, Core.OptionsInitializeSectionContext s)
    {
        using var _ = edition.At(s);
        edition.Declined(DiagnosticCatalog.ScreenFacilityUnsupported,
            "the SCREEN target of the OPTIONS paragraph's INITIALIZE clause (ISO §11.9.10) initializes the "
            + "screen section — write LOCAL-STORAGE and/or WORKING-STORAGE, or ALL, which stays legal because "
            + "§11.9.10.4 GR1's other two targets are supported");
    }

    /// <summary>The CRT STATUS twin of <see cref="ReportCursorClause"/> — same clause, same item 25.</summary>
    public static void ReportCrtStatusClause(EditionContext edition, Core.CrtStatusClauseContext crt)
    {
        using var _ = edition.At(crt);
        edition.Declined(DiagnosticCatalog.ScreenFacilityUnsupported,
            "the SPECIAL-NAMES CRT STATUS clause (ISO §12.3.7; Annex A.4.2 item 25) — the CRT status (§9.2.3) "
            + "is set only by an ACCEPT screen statement");
    }

    // ── The procedure division surface — COBOLNET1707 ─────────────────────────────────────────────────────────

    /// <summary>Refuse ACCEPT format 3 (ISO §14.9.1; Annex A.4.2 item 1). <paramref name="why"/> records HOW the
    /// screen format was recognized, because the two recognitions are not equally obvious: a positioning or
    /// exception phrase is unmistakable, while a bare <c>ACCEPT screen-name-1</c> is token-identical to the
    /// format-1 device ACCEPT and is told apart ONLY by the operand being a screen-name.</summary>
    public static void ReportAcceptScreen(EditionContext edition, string why) =>
        edition.Declined(DiagnosticCatalog.ScreenStatementUnsupported,
            $"the ACCEPT statement's screen format 3 (ISO §14.9.1; Annex A.4.2 item 1) — {why}; it is refused "
            + "rather than re-read as the format-1 device ACCEPT, which would transfer device input into a "
            + "screen record");

    /// <summary>Refuse DISPLAY format 2 (ISO §14.9.11; Annex A.4.2 item 9 — whose printed cross-reference reads
    /// "(14.9.10)", the DELETE statement, a defect in the standard recorded in kb/Work PB260 and re-derived
    /// independently by the A.4.2 selector wave; the element is named unambiguously by its text, so the licence
    /// holds and only the number is wrong).</summary>
    public static void ReportDisplayScreen(EditionContext edition, string why) =>
        edition.Declined(DiagnosticCatalog.ScreenStatementUnsupported,
            $"the DISPLAY statement's screen format 2 (ISO §14.9.11; Annex A.4.2 item 9) — {why}; it is refused "
            + "rather than re-read as the format-1 device DISPLAY, which would print the screen record's "
            + "characters instead of painting a screen");

    /// <summary>Refuse SET format 6 (ISO §14.9.39; Annex A.4.2 item 24) — <c>SET screen-name-1 ATTRIBUTE
    /// {BELL|BLINK|HIGHLIGHT|LOWLIGHT|REVERSE-VIDEO|UNDERLINE} {ON|OFF}</c>. Note the printed format has no
    /// <c>TO</c>; the attribute words are followed directly by ON or OFF.</summary>
    public static void ReportSetAttribute(EditionContext edition, string screenName) =>
        edition.Declined(DiagnosticCatalog.ScreenStatementUnsupported,
            $"the SET statement's screen attribute format 6 (ISO §14.9.39; Annex A.4.2 item 24) applied to "
            + $"'{screenName}'");

    // ⛔ THE EC-SCREEN EXCEPTION-NAMES ARE NOT REFUSED HERE. Annex A.4.2 item 10's six written-name sites
    // — the RAISING phrases of EXIT (§14.9.14), GOBACK (§14.9.18) and the procedure division header
    // (§14.2.1; legality §14.2.2 SR7–SR9), the USE statement (§14.9.49), the WHEN phrase of PERFORM
    // (§14.9.28), RAISE (§14.9.29) and the &gt;&gt;TURN directive (§7.3.25) — are the SAME six every
    // declined module owns, so the refusal lives in the ONE resolution funnel that serves them all:
    // `EcNameResolution.Advise`, over the `DeclinedEcFamilies` table, where this module is one row
    // carrying COBOLNET1707 as its descriptor. This wave shipped a second predicate for that job and the
    // A.4.14/A.4.3 wave shipped the table; the table won (feedback_one_mechanism_per_job).
}
