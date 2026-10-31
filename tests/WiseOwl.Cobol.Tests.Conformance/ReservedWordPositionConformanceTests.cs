// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// Per-edition enforcement of the screen/report allowlist-band reserved words through the POSITION-AWARE §8.9
/// funnel (P2.8 W2 — the RW104A adversarial review): a band context-keyword token (COLUMN, COL, SCREEN, BIT,
/// DEFAULT, LENGTH, NATIONAL are the table-backed ones) rejects with COBOL0901 (ISO §8.3.2.1 rule 1 /
/// §8.3.2.1 r1: "Reserved words shall not be used as user-defined words or system-names") ONLY when it occupies a provable
/// user-word position — the data entry-name slot (§13.16), a paragraph/section definition (§14.4.2/§14.4.3),
/// the SELECT file-name (§12.4.5.1), a program-name site (§11.10.2). KEYWORD occurrences that the permissive
/// grammar binds into optional entry-name slots — the report-group COLUMN clause (§13.18.14), the RW104A
/// false-reject the former blanket token-type exclusion parked — stay unflagged at every edition. Severity
/// routes through the ONE <c>EditionSeverityPolicy</c>: a word an edition ADDED to §8.9 is <c>Removed</c> (error
/// strict, warning permissive — the 0901 band contract), while a word reserved at this edition AND at every older
/// one is <c>NotYetIntroduced</c>, an error on BOTH axes, and the parser's reservation gate therefore holds under
/// <c>--permissive</c> too (kb/Work PB792).
/// </summary>
public sealed class ReservedWordPositionConformanceTests
{
    // ── The RW104A shape: a report-group COLUMN clause is a KEYWORD use, never a user word ─────────────────

    /// <summary>A minimal RW104A-style report program: the report-group <c>COLUMN 27</c> clause keyword
    /// (§13.18.14) parses into the optional report-group entry-name slot under the permissive grammar.</summary>
    private const string ReportColumnProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPCOL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RWPCOLF".
        DATA DIVISION.
        FILE SECTION.
        FD RPT
            REPORT IS R-1.
        REPORT SECTION.
        RD R-1 PAGE LIMIT 60.
        01 DET TYPE DETAIL.
            03 LINE 1.
                05 COLUMN 27 PIC X(4) VALUE "MARK".
                05 COLUMN NUMBER 70 PIC X(5) VALUE "PAGE ".
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RPT.
            INITIATE R-1.
            GENERATE DET.
            TERMINATE R-1.
            CLOSE RPT.
            STOP RUN.
        """;

    /// <summary>COLUMN is §8.9-reserved at EVERY edition, yet its report-group CLAUSE use is a keyword use —
    /// the position-aware funnel must not flag it anywhere (the RW104A/CCVS-85 no-false-reject guarantee),
    /// on either severity axis.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReportGroupColumnClause_KeywordUse_NoReservedWordDiagnostic(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ReportColumnProgram, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOL0901");
    }

    // ── The data entry-name slot (§13.16) ────────────────────────────────────────────────────────────────────

    /// <summary>A data item NAMED with the band word SCREEN (reserved 2002+ per reserved-words.json).</summary>
    private const string ScreenDataItemProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSCR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 SCREEN PIC X(3) VALUE "ABC".
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY SCREEN.
            STOP RUN.
        """;

    /// <summary>SCREEN was NOT reserved in X3.23-1985 — a data item so named is a conforming 85 program
    /// (the continuity leg of the interval).</summary>
    [Fact]
    public void DataItemNamedScreen_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOL0901");
    }

    /// <summary>2002+ reserves SCREEN (§8.9 via reserved-words.json row SCREEN): the entry-name use is a
    /// provable user-word position, so strict rejects with 0901 naming the word.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void DataItemNamedScreen_2002Plus_Strict_Rejected0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(ScreenDataItemProgram, edition);
        Assert.False(ok, $"--std {edition} strict must reject a data item named with a reserved word");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0901");
        EditionHarness.AssertHasDiagnostic(diags, "'SCREEN'");
    }

    /// <summary>The same compile PERMISSIVE keeps the pre-reservation semantics and downgrades 0901 to a
    /// warning (the §10 #1 migration posture — <c>EditionContext.Removed</c> severity contract).</summary>
    [Fact]
    public void DataItemNamedScreen_2002Permissive_WarnsAndCompiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 2002, permissive: true);
        Assert.True(ok, $"--std 2002 --permissive: {string.Join("; ", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOL0901");
    }

    /// <summary>COLUMN has been reserved CONTINUOUSLY since 1985 (reserved-words.json row COLUMN) — naming a
    /// data item COLUMN violates §8.3.2.1 rule 1 even at --std 85, though the same word as a report CLAUSE
    /// keyword is fine (the position distinction in one word).</summary>
    [Fact]
    public void DataItemNamedColumn_Rejected0901_EvenAt85()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPCDN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 COLUMN PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY COLUMN.
                STOP RUN.
            """, 85);
        Assert.False(ok, "--std 85 strict must reject a data item named COLUMN (reserved since 1985)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COLUMN'");
    }

    // ── Paragraph definitions (§14.4.3) and the SELECT file-name (§12.4.5.1) ────────────────────────────────

    /// <summary>A paragraph NAMED with the band word COL (reserved 2002+).</summary>
    private const string ColParagraphProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPPAR.
        PROCEDURE DIVISION.
        MAIN.
            PERFORM COL.
            STOP RUN.
        COL.
            DISPLAY "C".
        """;

    /// <summary>COL was not reserved in 1985 — the paragraph name is conforming at 85.</summary>
    [Fact]
    public void ParagraphNamedCol_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ColParagraphProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOL0901");
    }

    /// <summary>2002 reserved COL: the paragraph DEFINITION is a provable user-word position — strict rejects,
    /// permissive warns and runs with the pre-reservation semantics.</summary>
    [Fact]
    public void ParagraphNamedCol_2002_StrictRejects_PermissiveWarns()
    {
        var (ok, diags) = EditionHarness.Compile(ColParagraphProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a paragraph named COL");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COL'");

        var (okP, errorsP, warningsP) = EditionHarness.CompileFull(ColParagraphProgram, 2002, permissive: true);
        Assert.True(okP, $"--std 2002 --permissive: {string.Join("; ", errorsP)}");
        EditionHarness.AssertHasDiagnostic(warningsP, "COBOL0901");
    }

    /// <summary>A file NAMED with the band word DEFAULT (reserved 2002+) in its SELECT clause.</summary>
    private const string DefaultFileProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSEL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT DEFAULT ASSIGN TO "RWPSELF".
        DATA DIVISION.
        FILE SECTION.
        FD DEFAULT.
        01 REC PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT DEFAULT.
            MOVE "HELLO" TO REC.
            WRITE REC.
            CLOSE DEFAULT.
            STOP RUN.
        """;

    /// <summary>DEFAULT was not reserved in 1985 — the file-name is conforming at 85.</summary>
    [Fact]
    public void SelectFileNamedDefault_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(DefaultFileProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOL0901");
    }

    /// <summary>2002 reserved DEFAULT (the OPTIONS-paragraph family): the SELECT file-name is a provable
    /// user-word position — strict rejects with 0901.</summary>
    [Fact]
    public void SelectFileNamedDefault_2002_Strict_Rejected0901()
    {
        var (ok, diags) = EditionHarness.Compile(DefaultFileProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a file named DEFAULT");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0901");
        EditionHarness.AssertHasDiagnostic(diags, "'DEFAULT'");
    }

    /// <summary>The full NIST RW104A program (the review's namesake) still compiles at --std 85 with NO
    /// reserved-word diagnostic — the report COLUMN clause keyword stays a keyword.</summary>
    [Fact]
    public void Rw104a_At85_NoReservedWordDiagnostic()
    {
        var (ok, diags) = EditionHarness.CompileNist("RW104A", 85);
        Assert.True(ok, $"RW104A --std 85: {string.Join("; ", diags)}");
    }

    // ── The W2 adversarial-review coverage additions (DEVLOG 595): the programName and section-name slots
    // end-to-end with a BAND token (the unit facts used IDENTIFIER names, which the funnel checks
    // position-blind anyway). (The non-ISO linkageProcedureParameter arm is deleted, kb/Work PB1252.)

    /// <summary>A program NAMED with a band word: user-definable at 85, 0901 at 2002+ strict (§8.3.2.1 r1;
    /// SCREEN reserved 2002 — the A.4.2 module word).</summary>
    [Fact]
    public void ProgramNamedScreen_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCREEN.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOL0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a program named SCREEN");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOL0901");
    }

    /// <summary>A SECTION defined with a band word (BIT reserved 2002): 85 clean, 2002 strict 0901.</summary>
    [Fact]
    public void SectionNamedBit_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPSEC1.
            PROCEDURE DIVISION.
            BIT SECTION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOL0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a section named BIT");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOL0901");
    }

    // ── The KEYWORD slot that BORROWS cobolWord (kb/Work PB693) ─────────────────────────

    /// <summary>The VALIDATE-STATUS clause's ON phrase (ISO §13.18.62.2). FORMAT, CONTENT and RELATION are
    /// UNDERLINED in the printed general format — KEYWORDS (§5.2.2), not user-defined words — inside a CHOICE
    /// INDICATOR group (§5.2.6.4: one or more, each at most once, any order), so all three may be written.</summary>
    private const string ValidateStatusOnPhraseProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPVSON.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-REC.
           05 WS-A PIC X(4).
        01 WS-MSG PIC X(30) VALIDATE-STATUS IS "ERR" WHEN NO ERROR
           ON FORMAT CONTENT RELATION FOR WS-A.
        PROCEDURE DIVISION.
            DISPLAY "UNREACHABLE".
            STOP RUN.
        """;

    /// <summary>⛔ THE POSITION DISTINCTION THE §8.9 RESERVATION GATE MUST RESPECT. FORMAT is §8.9-reserved
    /// from 2002 AND has a lexer token, so the gate removes its <c>cobolWord</c> alternative there; the ON-phrase
    /// slot borrowed <c>cobolWord</c> to match it, which turned this legal clause into a parse error carrying a
    /// FALSE "'FORMAT' … cannot be used as a user-defined word". The word is a KEYWORD here, so the answer is
    /// the declined-facility COBOL1708 (Annex A.4.14 — the VALIDATE facility's support is not claimed) and
    /// NO §8.9 diagnostic at all. The <c>declined-validate-status</c> negative case is the corpus twin.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ValidateStatusOnPhrase_FormatIsAKeywordUse_DeclinedNever0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(ValidateStatusOnPhraseProgram, edition);
        Assert.False(ok, $"--std {edition} must decline the VALIDATE-STATUS clause");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL1708");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL0901");
    }

    /// <summary>The same clause under <c>--permissive</c>: FORMAT is a word COBOL-2002 ADDED to §8.9 (r85 false),
    /// so its verdict is <c>Removed</c>, the migration mode HAS a pre-reservation reading to restore and the gate
    /// stands down — the decline stays a warning and the program compiles. (The gate is not blanket-off under
    /// <c>--permissive</c>: a word reserved at this edition and at every older one keeps it — kb/Work PB792.)
    /// </summary>
    [Fact]
    public void ValidateStatusOnPhrase_2002Permissive_WarnsAndCompiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ValidateStatusOnPhraseProgram, 2002, permissive: true);
        Assert.True(ok, $"--std 2002 --permissive: {string.Join("; ", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOL1708");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOL0901");
    }

    /// <summary>ONE occurrence of the word, ONE report (kb/Work PB693). A REFERENCE to a reservation-gated
    /// word cannot parse, so the §8.9 answer comes from the syntax error's report (<c>CobolErrorStrategy</c>'s
    /// <c>ReservedWordUse</c>) rather than the bound-tree funnel — and ANTLR can raise TWO syntax errors on the one
    /// offending token (the prediction failure and the strategy's recovery message). Naming both printed the identical
    /// sentence twice; §8.3.2.1 rule 1 is violated once by one occurrence, and it is reported once.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReservedWordReference_IsNamedExactlyOnce(int edition)
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPONCE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FORMAT PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FORMAT.
                STOP RUN.
            """, edition);
        Assert.False(ok, $"--std {edition} strict must reject a reference to the reserved word FORMAT");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0901");
        Assert.Equal(1, diags.Count(d => d.Contains("COBOL0901", StringComparison.Ordinal)
                                         && d.Contains("'FORMAT'", StringComparison.Ordinal)));
    }

    // ── One reserved-word reference, one diagnostic (kb/Work PB3248) ───────────────────────────────────────────

    /// <summary>⛔ A REFERENCE TO A RESERVED VERB IS ONE §8.9 VIOLATION, REPORTED ONCE (kb/Work PB3248). ISO §8.3.2.1 1):
    /// "Reserved words shall not be used as user-defined words or system-names" — one occurrence breaks it once. RECEIVE
    /// and SEND (reserved at 85 and 2023) and VALIDATE (reserved from 2002) also BEGIN a statement, so the parser's
    /// recovery from the COBOL0901 used to resume AT the word, parse it as that statement and refuse the token after it:
    /// <c>DISPLAY RECEIVE.</c> drew COBOL0901 and then "COBOL0001: unexpected '.'". The recovery now consumes the word
    /// the report named, as the operand it stood for. Each statement below is refused with exactly ONE diagnostic, the
    /// COBOL0901 naming the word.
    /// <para>The GOBACK, VALIDATE, RAISE and FREE rows (branch review R2-1) are words whose statement needs few or no
    /// operands, so a statement PARSES as a prefix of what follows them (<c>GOBACK</c> in <c>MOVE GOBACK TO W-X</c>,
    /// <c>VALIDATE W-N</c> in <c>ADD VALIDATE W-N TO W-N</c>): the recovery must not resume at them, because the token
    /// after that prefix (TO, '=') can follow no statement — it drew "unexpected 'TO'" / "unexpected '='".</para></summary>
    [Theory]
    [InlineData(2023, "DISPLAY RECEIVE.", "RECEIVE")]
    [InlineData(85, "DISPLAY RECEIVE.", "RECEIVE")]
    [InlineData(2023, "DISPLAY SEND.", "SEND")]
    [InlineData(2002, "DISPLAY VALIDATE.", "VALIDATE")]
    [InlineData(2023, "MOVE W-X TO RECEIVE.", "RECEIVE")]
    [InlineData(2023, "MOVE RECEIVE TO W-X.", "RECEIVE")]
    [InlineData(2023, "ADD RECEIVE TO W-N.", "RECEIVE")]
    [InlineData(2023, "IF RECEIVE = 1 DISPLAY \"X\" END-IF.", "RECEIVE")]
    [InlineData(2023, "DISPLAY RECEIVE W-X.", "RECEIVE")]
    [InlineData(2023, "MOVE GOBACK TO W-X.", "GOBACK")]
    [InlineData(2023, "IF GOBACK = 1 DISPLAY \"X\" END-IF.", "GOBACK")]
    [InlineData(2023, "ADD VALIDATE W-N TO W-N.", "VALIDATE")]
    [InlineData(2023, "MOVE RAISE W-X TO W-Y.", "RAISE")]
    [InlineData(2023, "MOVE FREE W-X TO W-Y.", "FREE")]
    // Inside an inline PERFORM's UNTIL condition, with more condition text after the word (review R3-1 of the
    // PB4133/PB4134/PB4173 branch): the condition's own operands and the PERFORM's statements are not a second fault.
    [InlineData(2023, "PERFORM UNTIL RECEIVE W-X = 1 DISPLAY \"X\" END-PERFORM.", "RECEIVE")]
    [InlineData(2023, "PERFORM UNTIL SEND W-X = 1 DISPLAY \"X\" END-PERFORM.", "SEND")]
    [InlineData(2023, "PERFORM UNTIL VALIDATE W-X = 1 DISPLAY \"X\" END-PERFORM.", "VALIDATE")]
    [InlineData(2023, "PERFORM UNTIL W-N = 1 OR RECEIVE W-X = 1 DISPLAY \"X\" END-PERFORM.", "RECEIVE")]
    [InlineData(2023, "PERFORM WITH TEST AFTER UNTIL RECEIVE W-X = 1 DISPLAY \"X\" END-PERFORM.", "RECEIVE")]
    public void AReservedVerbWrittenAsAnOperand_DrawsOneDiagnostic(int edition, string statement, string word)
    {
        var (ok, diags) = EditionHarness.Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB3248R.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W-X PIC X(4).
                   01 W-Y PIC X(4).
                   01 W-N PIC 9(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       {statement}
                       DISPLAY "AFTER".
                       STOP RUN.

               """, edition);
        Assert.False(ok, $"'{word}' is reserved at --std {edition}: the reference is refused");
        Assert.True(diags.Count == 1 && diags[0].Contains("COBOL0901", StringComparison.Ordinal)
                                     && diags[0].Contains($"'{word}'", StringComparison.Ordinal),
            $"expected exactly the one COBOL0901 naming '{word}', got:\n{string.Join("\n", diags)}");
    }

    /// <summary>⛔ THE OTHER ARM: A RESERVED VERB THAT CORRECTLY BEGINS THE NEXT STATEMENT IS READ AS THAT STATEMENT
    /// (kb/Work PB3248, branch review R1-1). After an incomplete statement (<c>MOVE W-X TO</c>, its receiver missing) the
    /// syntax error falls AT the next statement's verb, and the report names it as the word's §8.9 violation just as
    /// it does an operand (a user-defined word stands there: the MOVE's missing receiver). Consuming that verb as an operand resumed the parse inside the correct statement that follows
    /// and refused one of ITS words: <c>RECEIVE FROM W-X GIVING W-Y W-N</c> drew "unexpected 'GIVING'", <c>SEND TO W-X
    /// FROM W-Y RETURNING W-N END-SEND</c> "unexpected 'FROM'", and <c>RESUME AT NEXT STATEMENT</c> was told to write
    /// SENTENCE. The recovery resumes at the verb when the statement it begins parses, so the source's one error is
    /// reported once. The last row is the arm's control: a reserved verb whose statement does NOT parse
    /// (<c>SEND TO W-X.</c>, no FROM phrase) is still consumed, and still draws one diagnostic. Each row asserts exactly
    /// one diagnostic, at the verb's line.</summary>
    [Theory]
    [InlineData("MOVE W-X TO", "RECEIVE FROM W-X GIVING W-Y W-N", "DISPLAY \"A\".", "RECEIVE")]
    [InlineData("MOVE W-X TO", "SEND TO W-X FROM W-Y RETURNING W-N END-SEND", "DISPLAY \"A\".", "SEND")]
    [InlineData("IF W-N = 1 MOVE W-X TO", "SEND TO W-X FROM W-Y RETURNING W-N END-SEND", "END-IF.", "SEND")]
    [InlineData("MOVE W-X TO", "RESUME AT NEXT STATEMENT", "DISPLAY \"A\".", "RESUME")]
    [InlineData("MOVE W-X TO", "VALIDATE W-X", "DISPLAY \"A\".", "VALIDATE")]
    [InlineData("MOVE W-X TO", "SEND TO W-X.", "DISPLAY \"A\".", "SEND")]
    public void AReservedVerbBeginningTheNextStatement_DrawsOneDiagnostic(
        string incomplete, string statement, string rest, string word)
    {
        var (ok, diags) = EditionHarness.Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB3248N.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W-X PIC X(4).
                   01 W-Y PIC X(4).
                   01 W-N PIC 9(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       {incomplete}
                       {statement}
                       {rest}
                       STOP RUN.

               """, 2023);
        Assert.False(ok, "the MOVE has no receiver: the source is refused");
        Assert.True(diags.Count == 1 && diags[0].Contains($"'{word}'", StringComparison.Ordinal)
                                     && diags[0].Contains("(11,", StringComparison.Ordinal),
            $"expected exactly one diagnostic, at '{word}' on line 11, got:\n{string.Join("\n", diags)}");
    }

    // ── A reserved word in a procedure-name slot (kb/Work PB4206) ─────────────────────────────────────────────────

    /// <summary>⛔ A RESERVED VERB WRITTEN AS A PROCEDURE-NAME IS NAMED BY COBOL0901, IN EVERY PROCEDURE-NAME SLOT (kb/Work
    /// PB4206). A procedure-name is a user-defined word (ISO §8.3.2.2; §14.4.1 "A procedure-name is a word used to refer to
    /// a paragraph or section"), so §8.3.2.1 1) "Reserved words shall not be used as user-defined words or system-names"
    /// holds there. RECEIVE, SEND (reserved at 85, 2002 and 2023) and VALIDATE (from 2002) also begin a statement, and the
    /// slot had no arm for a reserved word: <c>PERFORM RECEIVE.</c> parsed as an inline PERFORM whose RECEIVE statement
    /// refused the period, <c>GO TO SEND.</c> as the 1985 target-less GO TO followed by a SEND statement, each a COBOL0001
    /// at the period that never named the word. Every PERFORM format, both ends of THRU, both GO TO formats.
    /// <para>With the procedure DEFINED under the word, the source has one fault per word, so exactly one diagnostic:
    /// the COBOL0901 (the definition and the reference are the same word, reported once). With no such procedure,
    /// §14.9.28.3 SR12 / §8.4.2.1 are broken as well, and the only other diagnostic is that rule's COBOL1639 — never a
    /// parse error.</para></summary>
    [Theory]
    [InlineData(2023, "PERFORM RECEIVE.", "RECEIVE")]
    [InlineData(85, "PERFORM RECEIVE.", "RECEIVE")]
    [InlineData(2023, "PERFORM SEND.", "SEND")]
    [InlineData(2002, "PERFORM VALIDATE.", "VALIDATE")]
    [InlineData(2023, "PERFORM RECEIVE 2 TIMES.", "RECEIVE")]
    [InlineData(2023, "PERFORM RECEIVE UNTIL W-N = 1.", "RECEIVE")]
    [InlineData(2023, "PERFORM RECEIVE VARYING W-N FROM 1 BY 1 UNTIL W-N > 2.", "RECEIVE")]
    [InlineData(2023, "PERFORM RECEIVE THRU P-END.", "RECEIVE")]
    [InlineData(2023, "PERFORM P-END THRU SEND.", "SEND")]
    [InlineData(2023, "GO TO RECEIVE.", "RECEIVE")]
    [InlineData(85, "GO TO SEND.", "SEND")]
    [InlineData(2023, "GO TO RECEIVE P-END DEPENDING ON W-N.", "RECEIVE")]
    [InlineData(85, "GO TO P-END SEND DEPENDING ON W-N.", "SEND")]
    public void AReservedVerbWrittenAsAProcedureName_IsNamedByCobol0901(int edition, string statement, string word)
    {
        foreach (bool defined in new[] { true, false })
        {
            string procedure = defined ? $"""
                       {word}.
                           DISPLAY "W".
                   """ : "";
            var (ok, diags) = EditionHarness.Compile($"""
                       IDENTIFICATION DIVISION.
                       PROGRAM-ID. PB4206R.
                       DATA DIVISION.
                       WORKING-STORAGE SECTION.
                       01 W-N PIC 9(4) VALUE 1.
                       PROCEDURE DIVISION.
                       MAIN-P.
                           {statement}
                           STOP RUN.
                   {procedure}
                       P-END.
                           DISPLAY "E".

                   """, edition);
            string all = string.Join("\n", diags);
            Assert.False(ok, $"'{word}' is reserved at --std {edition}: the procedure-name is refused (defined: {defined})");
            Assert.True(diags.Count(d => d.Contains("COBOL0901", StringComparison.Ordinal)
                                         && d.Contains($"'{word}'", StringComparison.Ordinal)) == 1,
                $"expected exactly one COBOL0901 naming '{word}' (defined: {defined}), got:\n{all}");
            var others = diags.Where(d => !d.Contains("COBOL0901", StringComparison.Ordinal)).ToList();
            if (defined)
                Assert.True(others.Count == 0, $"the defined procedure leaves one fault, got:\n{all}");
            else
                Assert.True(others.All(d => d.Contains("COBOL1639", StringComparison.Ordinal)
                                            && d.Contains($"'{word}'", StringComparison.Ordinal)),
                    $"no procedure is named '{word}': only SR12's COBOL1639 may stand beside the COBOL0901, got:\n{all}");
        }
    }

    // ── A reserved word where no user-defined word stands is its keyword, misplaced (kb/Work PB4132) ──────────────

    /// <summary>⛔ §8.3.2.1 1) IS BROKEN BY A USE AS A USER-DEFINED WORD, SO COBOL0901 NAMES ONLY SUCH A USE (kb/Work
    /// PB4132). ISO §8.3.2.1 1): "Reserved words shall not be used as user-defined words or system-names." Where §8.9
    /// reserves a reservation-gated word the token IS its keyword, and a syntax error on it is that rule's violation only
    /// where a user-defined word could stand. RETRY after <c>ADVANCING ON LOCK</c> stands where the READ has ended and no
    /// statement begins with a user word: it begins the retry-phrase, the third alternative of the one bracket ISO
    /// §14.9.30.2 Format 1 prints for ADVANCING ON LOCK, IGNORING LOCK and retry-phrase (ISO §5.2.6.2: "one of the
    /// alternatives contained within the brackets may be explicitly specified"), and
    /// the READ's own refusal (COBOL2269) names it. It used to draw COBOL0901, which the syntax-error listener wrote over
    /// every report on such a word. The stray scope terminators (END-DISPLAY and its three gated siblings) are swept in
    /// <c>ParseRecoveryTailTests</c>.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ARetryPhraseAfterAdvancingOnLock_IsTheReadsRefusal_Never0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB4132R.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT SQF ASSIGN TO "pb4132r.dat"
                           ORGANIZATION IS SEQUENTIAL.
                   DATA DIVISION.
                   FILE SECTION.
                   FD SQF.
                   01 SQ-REC PIC X(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       OPEN INPUT SQF.
                       READ SQF NEXT RECORD ADVANCING ON LOCK RETRY 3 TIMES
                           AT END CONTINUE
                       END-READ.
                       CLOSE SQF.
                       STOP RUN.

               """, edition);
        Assert.False(ok, $"--std {edition}: two alternatives of one bracket are refused");
        Assert.True(diags.Count == 1 && diags[0].Contains("COBOL2269", StringComparison.Ordinal)
                                     && diags[0].Contains("'RETRY'", StringComparison.Ordinal),
            $"expected exactly the one COBOL2269 at 'RETRY', got:\n{string.Join("\n", diags)}");
    }

    /// <summary>THE OTHER ARM: where a user-defined word DOES stand, the reserved word is refused as one (kb/Work PB693,
    /// PB4132). Each of the three readings that admit a user-defined word at the token keeps COBOL0901: an operand of
    /// the statement still open (<c>DISPLAY CONSTANT</c>), the next operand of a list the statement before the token
    /// would continue (<c>MOVE 1 TO W-N CONSTANT W-K</c>, whose receiving list the parse had already left), and a
    /// paragraph header a missing separator period would begin (<c>STOP RUN CONSTANT.</c>).
    /// <para>At the start of a sentence the header's name is the user-defined word that stands there (kb/Work PB4132
    /// review R1-1, <c>CobolParserCoreBase.procedureHeaderHere</c>). Where the header is written (ISO §14.4.3: "A
    /// paragraph consists of a paragraph-name followed by a separator period") the funnel names the reserved
    /// paragraph-name, a scope terminator included: <c>END-DISPLAY.</c> matches the paragraph format and no other, and
    /// a COBOL-85 paragraph so named, compiled at an edition that reserves the word, is refused for its name. Where the
    /// period does not follow, the word is its keyword and the report is made at it, never at the next word as a
    /// missing period: <c>CONSTANT MOVE 1 TO W-N.</c> here, and a stray terminator's COBOL0306
    /// (<c>ParseRecoveryTailTests</c>).</para></summary>
    [Theory]
    [InlineData("DISPLAY CONSTANT.")]
    [InlineData("MOVE 1 TO W-N CONSTANT W-K.")]
    [InlineData("MOVE 1 TO W-N CONSTANT.")]
    [InlineData("STOP RUN CONSTANT.")]
    [InlineData("CONSTANT MOVE 1 TO W-N.")]
    [InlineData("MOVE 1 TO W-N. CONSTANT MOVE 2 TO W-K.")]
    [InlineData("MOVE 1 TO W-N. CONSTANT.")]
    [InlineData("MOVE 1 TO W-N. END-DISPLAY.", "END-DISPLAY")]
    [InlineData("MOVE 1 TO W-N. END-ACCEPT.", "END-ACCEPT", 2014)]
    [InlineData("MOVE 1 TO W-N. END-SEND.", "END-SEND", 2023)]
    public void AReservedWordWhereAUserWordStands_Draws0901(string statement, string word = "CONSTANT", int edition = 2002)
    {
        var (ok, diags) = EditionHarness.Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB4132U.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W-N PIC 9(4).
                   01 W-K PIC 9(4).
                   PROCEDURE DIVISION.
                   MAIN-P.
                       {statement}
                       STOP RUN.

               """, edition);
        Assert.False(ok, $"{word} is reserved at --std {edition}: the use is refused");
        Assert.True(diags.Count == 1 && diags[0].Contains("COBOL0901", StringComparison.Ordinal)
                                     && diags[0].Contains($"'{word}'", StringComparison.Ordinal),
            $"expected exactly the one COBOL0901 naming '{word}', got:\n{string.Join("\n", diags)}");
    }
}
