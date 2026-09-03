// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The Annex A.4.2 (screen handling) REFUSAL, held from the three directions a negative golden cannot reach
/// (kb/Work PB260).
///
/// <para>The negative corpus proves that each declined construct DRAWS its named diagnostic. It cannot prove
/// (a) that every clause the grammar admits HAS a name — a clause added to <c>CobolScreen.g4</c> with no
/// <see cref="ScreenFacility"/> row would fall silently through to the section's own diagnostic and every
/// witness would still be green; (b) that a diagnostic is ABSENT, which is what the kb/Work R32 no-cascade
/// property is; or (c) that the refusal has a COMPLEMENT — that an ordinary program still compiles and the
/// three screen context-sensitive words are still legal user-defined words. A gate that can only go green is
/// not evidence (feedback_green_gates_arent_evidence, feedback_measure_the_selectors_complement).</para>
/// </summary>
public sealed class ScreenFacilityConstructDriftTests
{
    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Scr_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, CheckOnly: true));
            return (r.Success, r.Success ? [] : [.. r.Errors]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private static bool Has(IEnumerable<string> diags, string needle) =>
        diags.Any(d => d.Contains(needle, StringComparison.OrdinalIgnoreCase));

    /// <summary>⛔ THE DRIFT GATE. Every alternative of the grammar's <c>screenClause</c> rule has a
    /// <see cref="ScreenFacility"/> row, so a clause added to the SCREEN SECTION surface cannot ship
    /// un-named — it would otherwise be swallowed by the section-header diagnostic and no witness would
    /// notice. Read from the <c>.g4</c> text, which is the only place the alternative list exists.</summary>
    [Fact]
    public void EveryScreenClauseAlternative_HasAScreenFacilityRow()
    {
        string g4 = File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolScreen.g4"));
        var m = Regex.Match(g4, @"^screenClause\s*\r?\n(?<body>.*?)^\s*;\s*$",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the screenClause rule was not found in CobolScreen.g4 — the drift gate is blind");
        var alts = Regex.Matches(m.Groups["body"].Value, @"^\s*[:|]\s*([A-Za-z_][A-Za-z0-9_]*)\s*$",
                RegexOptions.Multiline)
            .Select(x => x.Groups[1].Value).ToList();
        Assert.NotEmpty(alts);
        var covered = ScreenFacility.CoveredClauseRules.ToHashSet(StringComparer.Ordinal);
        var missing = alts.Where(a => !covered.Contains(a)).Order().ToList();
        Assert.True(missing.Count == 0,
            "screenClause alternative(s) with no ScreenFacility row — the construct would be refused only by "
            + "the section header, with no name of its own: " + string.Join(", ", missing));
        // The complement: a row for a rule the grammar no longer offers is a stale citation nobody can reach.
        var stale = covered.Where(c => !alts.Contains(c)).Order().ToList();
        Assert.True(stale.Count == 0,
            "ScreenFacility row(s) for rules that are not screenClause alternatives: " + string.Join(", ", stale));
    }

    private const string ScreenProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SCRREF.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-X PIC X.
        SCREEN SECTION.
        01 SG.
           05 SI1 LINE 1 COL 1 PIC X FROM WS-X.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY SG END-DISPLAY.
            STOP RUN.
        """;

    /// <summary>kb/Work R32, kept as an ABSENCE assertion because a negative golden can only assert presence.
    /// A name DECLARED in a refused SCREEN SECTION is not UNDEFINED: the program is rejected for the facility
    /// (COBOLNET1560 for the section, COBOLNET1707 for the screen DISPLAY), never with the §8.4.2.1
    /// "is not defined" verdict (COBOLNET1639), which would send the user hunting a declaration that is right
    /// there. This is what <c>tests/conformance/2023/screen_section_reference.cob</c> used to hold before PB260
    /// refuted its premise (it asserted the program COMPILED).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ScreenNameReference_IsRefusedByFacility_NeverAsUndefined(int edition)
    {
        var (ok, errors) = Compile(ScreenProgram, edition);
        Assert.False(ok, "a SCREEN SECTION program must be REJECTED — Annex A.4.1 admits an optional element's "
            + "syntax only when support is claimed, and docs/CONFORMANCE.md §5 records A.4.2 as Not claimed");
        Assert.True(Has(errors, "COBOLNET1560"), "expected the named screen-facility refusal:\n"
            + string.Join("\n", errors));
        Assert.True(Has(errors, "COBOLNET1707"), "expected the screen DISPLAY (format 2) to be named too — a "
            + "bare `DISPLAY screen-name` is token-identical to the device format and used to PRINT the screen "
            + "record:\n" + string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1639"), "a name declared in the SCREEN SECTION must not also be "
            + "reported as undefined (kb/Work R32):\n" + string.Join("\n", errors));
    }

    /// <summary>⛔ THE FAILING BRANCH, exercised. A program with no screen construct compiles clean and draws
    /// NEITHER code — without this the whole suite could pass with a refusal that fired on every program.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void OrdinaryProgram_DrawsNoScreenRefusal(int edition)
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRNEG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-X.
                STOP RUN.
            """, edition);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1560") || Has(errors, "COBOLNET1707"));
    }

    /// <summary>⛔ THE REGRESSION THIS WAVE ALMOST SHIPPED. `ON EXCEPTION` / `NOT ON EXCEPTION` is spelled the
    /// same on a screen ACCEPT/DISPLAY as on the statements that already own one, and DISPLAY sits inside every
    /// one of their imperative-statement slots — so a free-standing optional `screenExceptionPhrases` on
    /// DISPLAY made the INNER display swallow the ENCLOSING statement's NOT-arm, and the enclosing statement
    /// lost it silently. Four corpus programs went red (delete_file_absent, ec_external_*). The grammar now
    /// binds the exception phrases to the AT/LINE/COLUMN positioning phrase (`screenTail`), which no other
    /// statement's exception arm can begin with; this pins that coupling so it cannot be "simplified" away.
    /// The program must compile AND its own DISPLAYs must draw no screen verdict.</summary>
    [Fact]
    public void NestedDisplayInAnExceptionArm_DoesNotStealTheEnclosingStatementsNotArm()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRNEST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "scrnest.dat" ORGANIZATION SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(10).
            PROCEDURE DIVISION.
            MAIN.
                DELETE FILE F
                    ON EXCEPTION DISPLAY "EXC"
                    NOT ON EXCEPTION DISPLAY "NOEXC"
                END-DELETE.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1707"));
    }

    /// <summary>⛔ THE COMPLEMENT OF THE OPTIONS INITIALIZE REFUSAL. §11.9.10.4 GR1 — "If ALL is specified,
    /// LOCAL-STORAGE, SCREEN, and WORKING-STORAGE apply" — names three targets of which TWO are supported, so
    /// <c>INITIALIZE ALL SECTION TO SPACES</c> is legal source and must stay legal; only the EXPLICIT SCREEN leg
    /// of GR3 is refused (<c>a42-options-initialize-screen</c>). The standard wrote the two as separate rules,
    /// and refusing ALL over a section the program does not even have would be exactly the over-rejection the
    /// A.4.2 selector's excluded-near-miss list called out.</summary>
    [Fact]
    public void OptionsInitializeAll_StaysLegal()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCROIA.
            OPTIONS.
                INITIALIZE ALL SECTION TO SPACES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(3).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1560"));
    }

    /// <summary>The screen-word sweep (kb/Work PB301). ISO §8.10 makes BACKGROUND-COLOR, FOREGROUND-COLOR and
    /// REVERSE-VIDEO CONTEXT-SENSITIVE — reserved only inside a screen description entry (and, for the last, a
    /// SET attribute statement) — so they are legal user-defined words at EVERY edition; §8.9 reserves CRT and
    /// CURSOR from 2002 only, so those are legal user words at COBOL-85. All five were lexer tokens that
    /// <c>cobolWord</c> did not admit, which turned a legal declaration into a parse error. Declining a facility
    /// must not cost the user the words.</summary>
    [Theory]
    [InlineData("BACKGROUND-COLOR", 85)]
    [InlineData("BACKGROUND-COLOR", 2023)]
    [InlineData("FOREGROUND-COLOR", 85)]
    [InlineData("FOREGROUND-COLOR", 2023)]
    [InlineData("REVERSE-VIDEO", 85)]
    [InlineData("REVERSE-VIDEO", 2023)]
    [InlineData("CRT", 85)]
    [InlineData("CURSOR", 85)]
    public void ScreenContextSensitiveWord_IsALegalUserWord(string word, int edition)
    {
        var (ok, errors) = Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRWORD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 {word} PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY {word}.
                STOP RUN.
            """, edition);
        Assert.True(ok, $"'{word}' is a legal user-defined word at COBOL-{edition} (ISO §8.10 context-sensitive "
            + $"/ §8.9 reservation interval) and must not be a parse error:\n{string.Join("\n", errors)}");
    }

    /// <summary>ATTRIBUTE is §8.10 context-sensitive in the SET statement and is NOT a lexer token, so the
    /// SET format-6 arm is recognized by a left-edge text predicate (<c>setAttributeAhead</c>) — which means a
    /// data item actually NAMED <c>ATTRIBUTE</c> walks straight through that predicate. Both directions are
    /// pinned: <c>SET ATTRIBUTE TO 7</c> (the word in the RECEIVER slot, where the predicate must not fire) and
    /// <c>SET IDX TO ATTRIBUTE</c> (the word downstream, where the predicate DOES fire and the alternative must
    /// then fail to match and fall through to the ordinary SET forms). Values are asserted, not merely
    /// compilation — a predicate that stole the statement and bound a no-op would still "compile".</summary>
    [Fact]
    public void AttributeAsAUserWord_KeepsTheOrdinarySetForms()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRATTR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ATTRIBUTE PIC 9(2) VALUE 3.
            01 N PIC 9(2) VALUE 0.
            01 T.
               05 TE PIC X OCCURS 5 TIMES INDEXED BY IDX.
            PROCEDURE DIVISION.
            MAIN.
                SET ATTRIBUTE TO 7.
                SET IDX TO ATTRIBUTE.
                SET N TO 4.
                DISPLAY ATTRIBUTE " " N.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1707"));
    }

    /// <summary>The COBOL-85 complement of the CRT/CURSOR reservation gate: below 2002 those words are not
    /// reserved, so <c>SPECIAL-NAMES. CURSOR IS WS-C.</c> is an ordinary implementor-switch entry and must NOT
    /// be read as the screen module's CURSOR clause. At 2002+ the same text IS the clause and is refused.</summary>
    [Fact]
    public void SpecialNamesCursor_IsASwitchEntryAt85_AndTheScreenClauseAbove()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRCUR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURSOR IS WS-CUR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CUR PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var at85 = Compile(src, 85);
        Assert.False(Has(at85.Errors, "COBOLNET1560"),
            "at COBOL-85 CURSOR is not reserved (§8.9), so this is an implementor-switch entry and carries no "
            + "screen verdict:\n" + string.Join("\n", at85.Errors));
        var at2023 = Compile(src, 2023);
        Assert.True(Has(at2023.Errors, "COBOLNET1560: the SPECIAL-NAMES CURSOR clause"),
            "at 2002+ the same text IS the §12.3.7 CURSOR clause (Annex A.4.2 item 25):\n"
            + string.Join("\n", at2023.Errors));
    }
}
