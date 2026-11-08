// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The X3.23-1985 USE FOR DEBUGGING / DEBUG-ITEM special-register facility at <c>--std 85</c> (VCR Table 7 row
/// 7.17). The '85 debug module was deleted by ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023, so its
/// authoritative behavior is the 1985 standard; WiseOwl COBOL models it (accepted-and-ACTIVE) only at <c>--std 85</c>
/// and rejects it ≥2002 (COBOL0902). Implemented: the ON procedure-name / ALL PROCEDURES trigger leg with the
/// DEBUG-CONTENTS transfer-cause taxonomy (START PROGRAM / SPACES / PERFORM LOOP / FALL THROUGH — DB101A witness)
/// and DEBUG-NAME, and the data-name, ALL REFERENCES OF, cd-name and file-name triggers (docs/CONTROL_FLOW_DESIGN.md D20; the behavior
/// matrix is the <c>X3.23-1985-USE-FOR-DEBUGGING</c> conformance goldens; the file-name trigger is general rule 2, kb/Work PB4811).
/// STAGED loud (COBOL1571): the SORT/MERGE-procedure cause.
/// </summary>
public sealed class DebugFacilityTests
{
    private const string AllProcedures = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DBGALLPROC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 NM   PIC X(30).
        01 CONT PIC X(13).
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG SECTION.
            USE FOR DEBUGGING ON ALL PROCEDURES.
        DBG-BODY.
            MOVE DEBUG-NAME     TO NM.
            MOVE DEBUG-CONTENTS TO CONT.
            DISPLAY "N=" NM "C=" CONT.
        END DECLARATIVES.
        MAIN SECTION.
        P-START.
            PERFORM P-LOOP 2 TIMES.
            GO TO P-END.
        P-LOOP.
            CONTINUE.
        P-END.
            STOP RUN.
        """;

    /// <summary>ALL PROCEDURES fires before each nondeclarative procedure; the DEBUG-CONTENTS taxonomy is the
    /// DB101A-witnessed START PROGRAM / SPACES / PERFORM LOOP set (hand-derived stdout, §X3.23-1985).</summary>
    [Fact]
    public void AllProcedures_At85_FiresWithTheDebugContentsTaxonomy()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(AllProcedures, 85);
        Assert.True(ok, detail);
        // CutRunner normalizes (per-line trailing-space trim), so trim the expected lines' trailing spaces too;
        // NM's INTERNAL padding (before "C=") survives the trim, the DEBUG-CONTENTS taxonomy is the observable.
        string[] lines = stdout.Split('\n');
        Assert.Equal(4, lines.Length);
        // NM is X(30): name left-justified, right-space-padded. CONT is X(13).
        Assert.Equal(("N=" + "P-START".PadRight(30) + "C=START PROGRAM").TrimEnd(), lines[0]);
        Assert.Equal(("N=" + "P-LOOP".PadRight(30) + "C=").TrimEnd(), lines[1]);          // PERFORM iter 1 -> SPACES
        Assert.Equal(("N=" + "P-LOOP".PadRight(30) + "C=PERFORM LOOP").TrimEnd(), lines[2]); // iter 2 -> PERFORM LOOP
        Assert.Equal(("N=" + "P-END".PadRight(30) + "C=").TrimEnd(), lines[3]);            // GO TO -> SPACES
    }

    /// <summary>DEBUG-LINE identifies the CAUSING statement (X3.23-1985; DB101A): START PROGRAM → the subject's
    /// first executable statement (here P-START's <c>PERFORM P-LOOP</c>); a PERFORM trigger → the PERFORM
    /// statement's line on EVERY iteration (DB101A PERF-ITERATION-TEST :611-617); a GO TO trigger → the GO TO
    /// statement's line (DB101A GO-TO-TEST :482-489). Asserted relationally so the absolute source offset is
    /// irrelevant: P-START's PERFORM is the subject's first statement, so all three PERFORM-related DEBUG-LINEs are
    /// equal; the GO TO is exactly one line below.</summary>
    [Fact]
    public void DebugLine_At85_IdentifiesTheCausingStatement()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGLINE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 LN PIC X(6).
            01 CT PIC X(13).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-LINE     TO LN.
                MOVE DEBUG-CONTENTS TO CT.
                DISPLAY "L=[" LN "]C=[" CT "]".
            END DECLARATIVES.
            MAIN SECTION.
            P-START.
                PERFORM P-LOOP 2 TIMES.
                GO TO P-END.
            P-LOOP.
                CONTINUE.
            P-END.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        Assert.Equal(4, lines.Length);
        static (int Line, string Cont) Parse(string s)
        {
            int lb = s.IndexOf("L=[", System.StringComparison.Ordinal) + 3;
            int rb = s.IndexOf(']', lb);
            int cb = s.IndexOf("C=[", System.StringComparison.Ordinal) + 3;
            int cr = s.IndexOf(']', cb);
            return (int.Parse(s[lb..rb].Trim()), s[cb..cr].TrimEnd());
        }
        var p0 = Parse(lines[0]);   // P-START, START PROGRAM
        var p1 = Parse(lines[1]);   // P-LOOP iter 1, SPACES
        var p2 = Parse(lines[2]);   // P-LOOP iter 2, PERFORM LOOP
        var p3 = Parse(lines[3]);   // P-END, GO TO (SPACES)
        Assert.Equal("START PROGRAM", p0.Cont);
        Assert.Equal("", p1.Cont);
        Assert.Equal("PERFORM LOOP", p2.Cont);
        Assert.Equal("", p3.Cont);
        // The PERFORM line = P-START's first statement (START PROGRAM subject line) = the PERFORM cause line on
        // both iterations; the GO TO is exactly the next line (the causing-statement semantics — NOT the subject
        // procedure's own body line, which would make them all equal).
        Assert.Equal(p0.Line, p1.Line);
        Assert.Equal(p0.Line, p2.Line);
        Assert.Equal(p0.Line + 1, p3.Line);
    }

    /// <summary>DEBUG-SUB-1/2/3 render SPACES for a procedure trigger (an unsubscripted reference) at the S9(4)
    /// SIGN LEADING SEPARATE width of FIVE characters; the whole DEBUG-ITEM group image is 86 characters
    /// (6 + 1 + 30 + 1 + 5 + 1 + 5 + 1 + 5 + 1 + 30). Bracketed so CutRunner's trailing-space trim does not eat
    /// the widths.</summary>
    [Fact]
    public void DebugSubAndGroupWidth_At85_ArePinned()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGSUB.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GI PIC X(86).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-ITEM TO GI.
                DISPLAY "GI=[" GI "]".
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        // The whole DEBUG-ITEM group image is exactly 86 characters between the brackets.
        int lb = lines[0].IndexOf('[') + 1, rb = lines[0].LastIndexOf(']');
        Assert.Equal(86, rb - lb);
        // DEBUG-SUB-1 (characters 39 to 43: 6 + 1 + 30 + 1 before it) is five SPACES for a reference that is not subscripted — the
        // S9(4) SIGN LEADING SEPARATE width of general rule 10, read through the group image because spaces are no number.
        Assert.Equal("     ", lines[0].Substring(lb + 38, 5));
    }

    /// <summary>The removal gate: WITH DEBUGGING MODE + USE FOR DEBUGGING are rejected COBOL0902 at ≥2002 (the
    /// '85 debug module deleted 2002 — §2023:8.9 absence).</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void DebugFacility_AtOrAbove2002_RejectsWith0902(int edition)
    {
        var diags = EditionHarness.GetDiagnostics(AllProcedures, edition);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL0902");
    }

    /// <summary>A bare procedure-name subject (ON P-LOOP) fires ONLY for that procedure — P-START / P-END are not
    /// subjects, so only P-LOOP's two PERFORM iterations produce output.</summary>
    [Fact]
    public void OnProcedureName_At85_FiresOnlyForThatProcedure()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGONEPROC.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON P-LOOP.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
                DISPLAY "N=" NM.
            END DECLARATIVES.
            MAIN SECTION.
            P-START.
                PERFORM P-LOOP 2 TIMES.
                GO TO P-END.
            P-LOOP.
                CONTINUE.
            P-END.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        string[] lines = stdout.Split('\n');
        Assert.Equal(2, lines.Length);
        // Only P-LOOP is a subject; DEBUG-NAME left-justified in X(30), trailing spaces trimmed by CutRunner.
        Assert.Equal("N=P-LOOP", lines[0]);
        Assert.Equal("N=P-LOOP", lines[1]);
    }

    /// <summary>WITHOUT WITH DEBUGGING MODE the debugging section is comment-treated (compiled as if comment lines):
    /// it never fires, and the program runs its ordinary body (X3.23-1985 — the conforming switch-absent posture).</summary>
    [Fact]
    public void SwitchAbsent_At85_CommentTreatsTheSection()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGABSENT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
                DISPLAY "DBG " NM.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                DISPLAY "IN TARGET".
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("IN TARGET", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>The data-name subject leg (ON ALL REFERENCES OF identifier) runs the section after the statement that references the item
    /// (X3.23-1985 USE FOR DEBUGGING general rule 5 d): <c>MOVE "Z" TO T</c> is the only statement that references T, so the section runs
    /// once and DEBUG-NAME is T (general rule 13). The behavior matrix is the conformance goldens'
    /// (<c>X3.23-1985-USE-FOR-DEBUGGING</c>); this is the leg's smoke.</summary>
    [Fact]
    public void OnDataName_At85_RunsTheSectionAfterTheReferencingStatement()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGDATA.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF T.
            DBG-BODY.
                DISPLAY DEBUG-NAME.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                MOVE "Z" TO T.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("T", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>The RECORD VARYING DEPENDING ON item of a sort description is read and restored by SORT, RELEASE and RETURN on the statement's behalf, so the
    /// program did not write it: <c>ALL REFERENCES OF DL</c> runs after the explicit <c>MOVE 3 TO DL</c> and after no verb of the sort
    /// (X3.23-1985 USE FOR DEBUGGING general rule 5, "explicitly references"; review round 2, R2-3).</summary>
    [Fact]
    public void DependingItemOfASortDescription_At85_IsReferencedOnlyByTheProgramsOwnStatements()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGVARY.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT S ASSIGN TO "dbgvary.srt".
            DATA DIVISION.
            FILE SECTION.
            SD  S RECORD IS VARYING IN SIZE FROM 2 TO 4 DEPENDING ON DL.
            01  SR.
                02  SK   PIC 99.
                02  SD1  PIC XX.
            WORKING-STORAGE SECTION.
            01 DL PIC 9 VALUE 4.
            01 V PIC X(4).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBL SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF DL.
            DBL-B.
                DISPLAY "DBL".
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                MOVE 3 TO DL.
                SORT S ON ASCENDING KEY SK
                   INPUT PROCEDURE IP OUTPUT PROCEDURE OP.
                DISPLAY "END".
                STOP RUN.
            IP SECTION.
            IP1.
                MOVE "0102" TO SR.
                RELEASE SR.
            OP SECTION.
            OP1.
                RETURN S INTO V AT END DISPLAY "AT END".
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("DBL\nEND", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>In <c>[ALL REFERENCES OF] identifier-1</c> only ALL is a keyword — REFERENCES and OF are optional
    /// words (GnuCOBOL <c>-std=cobol85</c> accepts all four spellings; CCVS DB201A writes <c>ALL ID-2</c>; kb/Work
    /// PB4237). Every spelling is the ONE data-item operand and runs the section after the statement that references it, DEBUG-NAME
    /// T, under WITH DEBUGGING MODE. The binder reads ALL, not REFERENCES, so <c>ALL P1</c> cannot fall to the
    /// bare-name arm and silently debug a paragraph P1 (which is <see cref="AllOperandNamingAProcedure_At85_IsNoDataItem"/>'s
    /// case). The pre-fix grammar refused the first two spellings with COBOL5506.</summary>
    [Theory]
    [InlineData("ALL T", "T")]
    [InlineData("ALL OF T", "T")]
    [InlineData("ALL REFERENCES T", "T")]
    [InlineData("ALL REFERENCES OF T", "T")]
    public void AllIdentifier_EverySpelling_At85_IsTheDataItemOperand(string operand, string name)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGALLSPELL.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON {operand}.
            DBG-BODY.
                DISPLAY DEBUG-NAME.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                MOVE "Z" TO T.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.DoesNotContain("COBOL5506", detail);
        Assert.Equal(name, stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>One USE FOR DEBUGGING declarative (line 18) whose operand the test chooses, over a program with a data
    /// item T, a file SF, a section MAIN and a paragraph P1 — the operand-kind matrix of kb/Work PB4363.</summary>
    private static string OperandProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DBGOPERAND.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SF ASSIGN TO "sf.tmp".
        DATA DIVISION.
        FILE SECTION.
        FD SF.
        01 SR PIC X(5).
        WORKING-STORAGE SECTION.
        01 T PIC X(5).
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG SECTION.
            USE FOR DEBUGGING ON {operand}.
        DBG-BODY.
            DISPLAY DEBUG-NAME.
        END DECLARATIVES.
        MAIN SECTION.
        P1.
            MOVE "Z" TO T.
            STOP RUN.
        """;

    /// <summary>In <c>ALL [REFERENCES OF] identifier-1</c> identifier-1 is a data item, and a word that names a
    /// paragraph or a section is a procedure-name (ISO §2023:14.4.1: "A procedure-name is a word used to refer to a
    /// paragraph or section"), so it identifies none (§2023:8.4.2.1). It is COBOL5848 naming the procedure, never the
    /// COBOL1571 that calls it the legal-but-unmodeled data-item subject (kb/Work PB4363, which pinned the
    /// wrong message for <c>ALL P1</c> before).</summary>
    [Theory]
    [InlineData("ALL P1", "P1")]
    [InlineData("ALL REFERENCES OF P1", "P1")]
    [InlineData("ALL MAIN", "MAIN")]
    [InlineData("ALL P1 OF MAIN", "P1 OF MAIN")]
    public void AllOperandNamingAProcedure_At85_IsNoDataItem(string operand, string name)
    {
        var (ok, diags) = EditionHarness.Compile(OperandProgram(operand), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, $"COBOL5848: declarative section 'DBG': USE FOR DEBUGGING ON ALL REFERENCES OF '{name}' names a paragraph or section");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
        EditionHarness.AssertHasDiagnostic(diags, "(18,");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, operand);
    }

    /// <summary>A word nothing declares, in either operand arm, is the ONE undefined-reference diagnostic
    /// (COBOL1639, ISO §2023:8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource"),
    /// which says the program is wrong and tells the user to check the spelling. COBOL1571 says the program is
    /// right and the compiler incomplete, and it was given for both (kb/Work PB4363). A qualifier under which the
    /// item is not declared is the same refusal with its own text (§2023:8.4.2.2.3 SR1).</summary>
    [Theory]
    [InlineData("ALL NOSUCH", "'NOSUCH' is not defined")]
    [InlineData("ALL REFERENCES OF NOSUCH", "'NOSUCH' is not defined")]
    [InlineData("NOSUCH", "'NOSUCH' is not defined")]
    [InlineData("ALL T OF NOSUCH", "'T OF NOSUCH' does not uniquely identify a data item")]
    [InlineData("T OF NOSUCH", "'T OF NOSUCH' does not uniquely identify a data item")]
    public void OperandNamingNothing_At85_IsTheUndefinedReference(string operand, string text)
    {
        var (ok, diags) = EditionHarness.Compile(OperandProgram(operand), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL1639: " + text);
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
        EditionHarness.AssertHasDiagnostic(diags, "(18,");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, operand);
    }

    /// <summary>The bare operand that IS a file is the file-name subject (X3.23-1985 USE FOR DEBUGGING general rule 2; kb/Work PB4811): it
    /// compiles, and a file-name is not mistaken for an undeclared word by the resolution (kb/Work PB4363's sibling sweep of the bare-name arm).
    /// The program opens no file, so no section runs.</summary>
    [Fact]
    public void BareOperandNamingAFile_At85_IsTheFileNameSubject()
    {
        var (ok, diags) = EditionHarness.Compile(OperandProgram("SF"), 85);
        Assert.True(ok, string.Join("; ", diags));
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1639");
    }

    /// <summary>The bare operand that IS a data item is the data-name subject: it compiles (kb/Work PB4810), and the section runs after the
    /// statement that changes the item (X3.23-1985 USE FOR DEBUGGING general rule 6 c).</summary>
    [Fact]
    public void BareOperandNamingADataItem_At85_IsTheDataNameSubject()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(OperandProgram("T"), 85);
        Assert.True(ok, detail);
        Assert.Equal("T", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>One USE FOR DEBUGGING declarative (line 17) over a program with an input CD, CM-IN, whose record has the data item
    /// IN-Q, and a data item T: the cd-name rows of the operand-kind matrix (kb/Work PB4713).</summary>
    private static string CdOperandProgram(string operand) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DBGCDOPERAND.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 T PIC X(5).
        COMMUNICATION SECTION.
        CD CM-IN FOR INPUT.
        01 IN-SPECS.
            02 IN-Q PIC X(12).
            02 FILLER PIC X(75).
        PROCEDURE DIVISION.
        DECLARATIVES.
        DBG SECTION.
            USE FOR DEBUGGING ON {operand}.
        DBG-BODY.
            DISPLAY DEBUG-NAME.
        END DECLARATIVES.
        MAIN SECTION.
        P1.
            MOVE "Z" TO T.
            STOP RUN.
        """;

    /// <summary>A bare operand that names a cd-name the program declares is the cd-name subject (X3.23-1985 USE FOR DEBUGGING general rule
    /// 8; CCVS DB205A writes <c>ON CM-INQUE CM-OUTQUE</c>): it compiles, and the declared name is not "not defined" (COBOL1639, ISO
    /// §2023:8.4.2.1) — kb/Work PB4713 named it, kb/Work PB4759 models its trigger. The section runs after the ACCEPT ... MESSAGE COUNT that
    /// names the cd-name, DEBUG-NAME CM-IN.</summary>
    [Fact]
    public void BareOperandNamingACdName_At85_IsTheCdNameSubject()
    {
        string src = CdOperandProgram("CM-IN").Replace("MOVE \"Z\" TO T.", "ACCEPT CM-IN MESSAGE COUNT.");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("CM-IN", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>An unknown word beside a declared cd-name is still the undefined reference, and only that word: the cd-name is not
    /// swallowed with it, and it is not excused by sitting in the same phrase.</summary>
    [Fact]
    public void BareOperandsCdNameAndUndeclaredWord_At85_AreEachJudgedOnTheirOwn()
    {
        var (ok, diags) = EditionHarness.Compile(CdOperandProgram("CM-IN NOSUCH"), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL1639: 'NOSUCH' is not defined");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
    }

    /// <summary>A cd-name after ALL is no identifier-1: a cd-name is declared, so the refusal says what it is declared as
    /// (the resolver's file-name wording, now for the cd-name) and never "not defined" (ISO §2023:8.4.2.1; kb/Work PB4713, the
    /// reference resolver's sibling of the bare-name arm).</summary>
    [Theory]
    [InlineData("ALL CM-IN", "COBOL1639: 'CM-IN' is declared as a cd-name")]
    [InlineData("ALL REFERENCES OF CM-IN", "COBOL1639: 'CM-IN' is declared as a cd-name")]
    public void AllOperandNamingACdName_At85_IsDeclaredAsACdName(string operand, string expected)
    {
        var (ok, diags) = EditionHarness.Compile(CdOperandProgram(operand), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, expected);
        EditionHarness.AssertNoDiagnostic(diags, "is not defined");
    }

    /// <summary>A data item of the CD's record is a data item like any other: ALL REFERENCES OF IN-Q compiles (kb/Work PB4810).</summary>
    [Fact]
    public void AllOperandNamingADataItemOfACdRecord_At85_IsTheDataItemSubject()
    {
        var (ok, diags) = EditionHarness.Compile(CdOperandProgram("ALL IN-Q"), 85);
        Assert.True(ok, string.Join("; ", diags));
    }

    /// <summary>A debug operand that names a table element is written WITHOUT its subscripts (the DEBUG-SUB-n items
    /// report them, X3.23-1985; CCVS DB201A writes <c>ON B-LEVEL-1 B-LEVEL-2 B-LEVEL-3</c> and <c>ALL REFERENCES OF
    /// AB1 OF A1</c> over OCCURS items), so it is the data item the trigger watches and not a reference
    /// the §2023:8.4.2.3.3 SR5 "shall be subscripted" screen (COBOL2270) judges: the operand is classified by its
    /// declaration, in both arms (kb/Work PB4363, review round 1), and it compiles (kb/Work PB4810).</summary>
    [Theory]
    [InlineData("ELEM")]
    [InlineData("ALL ELEM")]
    [InlineData("ALL REFERENCES OF ELEM OF TBL")]
    [InlineData("ELEM OF TBL")]
    public void OperandNamingAnUnsubscriptedTableElement_At85_IsTheElementSubject(string operand)
    {
        string src = OperandProgram(operand).Replace("01 T PIC X(5).", "01 T PIC X(5).\n01 TBL.\n    05 ELEM PIC XX OCCURS 3.");
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.True(ok, string.Join("; ", diags));
    }

    /// <summary>X3.23-1985 USE FOR DEBUGGING syntax rules 10 and 12: identifier-1 is written WITHOUT subscripts or indexes and is not
    /// reference modified (COBOL6334) — the trigger watches the item, and DEBUG-SUB-n report the occurrence. A subscripted
    /// operand is a refusal, whichever arm names it.</summary>
    [Theory]
    [InlineData("ELEM (2)")]
    [InlineData("ALL ELEM (2)")]
    [InlineData("T (1:2)")]
    [InlineData("ALL REFERENCES OF T (2:1)")]
    public void OperandSubscriptedOrReferenceModified_At85_IsRefused(string operand)
    {
        string src = OperandProgram(operand).Replace("01 T PIC X(5).", "01 T PIC X(5).\n01 TBL.\n    05 ELEM PIC XX OCCURS 3.");
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL6334: declarative section 'DBG': USE FOR DEBUGGING ON");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, operand);
    }

    /// <summary>X3.23-1985 USE FOR DEBUGGING syntax rule 6: a data item (or cd-name) appears in one USE FOR DEBUGGING statement, once
    /// (COBOL6335) — twice in one statement, or in two statements, each a refusal at the second operand.</summary>
    [Theory]
    [InlineData("T T")]
    [InlineData("T ALL T")]
    public void OperandNamedTwice_At85_IsRefused(string operands)
    {
        var (ok, diags) = EditionHarness.Compile(OperandProgram(operands), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL6335: declarative section 'DBG': USE FOR DEBUGGING ON");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, operands);
    }

    /// <summary>One data item named by two USE statements, in two sections.</summary>
    [Fact]
    public void DataItemInTwoUseStatements_At85_IsRefused()
    {
        string src = OperandProgram("T").Replace("END DECLARATIVES.", "DBG2 SECTION.\n    USE FOR DEBUGGING ON ALL REFERENCES OF T.\nDBG2-BODY.\n    DISPLAY DEBUG-NAME.\nEND DECLARATIVES.");
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOL6335: declarative section 'DBG2': USE FOR DEBUGGING ON ALL REFERENCES OF 'T'");
    }

    /// <summary>General rule 1: "Automatic execution of a debugging section is not caused by a statement appearing in a debugging
    /// section" — the declarative here changes T, which its own USE names, and the section does not run itself (a run would recurse, or
    /// print twice): the one run is MAIN's MOVE.</summary>
    [Fact]
    public void StatementInADebuggingSection_At85_CausesNoSection()
    {
        string src = OperandProgram("T").Replace("DISPLAY DEBUG-NAME.", "DISPLAY DEBUG-NAME.\n    MOVE \"Y\" TO T.");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("T", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>A paragraph-name qualified by a word that is not its section is a procedure-name that identifies
    /// nothing, and is told so in the procedure-name text every other procedure-name operand gets — not as the
    /// undeclared data name the data resolver would call it (the paragraph P1 IS declared; kb/Work PB4363, review
    /// round 1, ISO §2023:8.4.2.2.2 Format 4).</summary>
    [Theory]
    [InlineData("P1 OF NOSEC", "P1 OF NOSEC")]
    [InlineData("ALL P1 OF NOSEC", "P1 OF NOSEC")]
    [InlineData("P1 OF T", "P1 OF T")]
    public void ProcedureNameUnderTheWrongQualifier_At85_NamesNoProcedure(string operand, string written)
    {
        var (ok, diags) = EditionHarness.Compile(OperandProgram(operand), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, $"COBOL1639: USE FOR DEBUGGING ON '{written}' names no procedure");
        EditionHarness.AssertNoDiagnostic(diags, "COBOL1571");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, operand);
    }

    /// <summary>A qualified procedure-name is a procedure-name subject: <c>ON PARA-W OF SEC-A</c> debugs SEC-A's
    /// paragraph and not SEC-B's of the same spelling (ISO §2023:8.4.2.2.2, the paragraph-name qualified by a section-name;
    /// §2023:8.4.2.2.1 rule 6). It was refused as a staged data item, with its qualifier dropped from the message.
    /// Performed once each, the debugging declarative runs once: HITS=1.</summary>
    [Fact]
    public void QualifiedProcedureNameSubject_At85_DebugsOnlyTheQualifiedParagraph()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGQUAL.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 HITS PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON PARA-W OF SEC-A.
            DBG-BODY.
                ADD 1 TO HITS.
            END DECLARATIVES.
            SEC-A SECTION.
            A-START.
                PERFORM PARA-W OF SEC-A.
                PERFORM PARA-W OF SEC-B.
                DISPLAY "HITS=" HITS.
                STOP RUN.
            PARA-W.
                CONTINUE.
            SEC-B SECTION.
            PARA-W.
                CONTINUE.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("HITS=1", stdout.Replace("\r", "").TrimEnd('\n'));
    }

    /// <summary>A SORT/MERGE INPUT/OUTPUT procedure that is also a debug subject is STAGED — rejected loud
    /// COBOL1571 (the SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS cause is not modeled; reject rather than
    /// emit a stale cause).</summary>
    [Fact]
    public void SortProcedureSubject_At85_StagedLoud1571()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGSORT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SF ASSIGN TO "sf.tmp".
            DATA DIVISION.
            FILE SECTION.
            SD SF.
            01 SR PIC X(5).
            WORKING-STORAGE SECTION.
            01 NM PIC X(30).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DBG SECTION.
                USE FOR DEBUGGING ON ALL PROCEDURES.
            DBG-BODY.
                MOVE DEBUG-NAME TO NM.
            END DECLARATIVES.
            MAIN SECTION.
            P1.
                SORT SF ASCENDING KEY SR
                    INPUT PROCEDURE IS FEED
                    OUTPUT PROCEDURE IS DRAIN.
                STOP RUN.
            FEED.
                CONTINUE.
            DRAIN.
                CONTINUE.
            """;
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok);
        // At the INPUT PROCEDURE phrase (line 25), never bare: the check runs after every walker (kb/Work PB2034).
        EditionHarness.AssertHasDiagnostic(diags, "(25,");
        EditionHarness.AssertHasDiagnostic(diags, "COBOL1571: the SORT/MERGE INPUT PROCEDURE");
        EditionHarness.AssertEveryDiagnosticPositioned(diags, nameof(SortProcedureSubject_At85_StagedLoud1571));
    }

    /// <summary>RESUME AT procedure-name is a GO TO (ISO §2023:14.9.33.4 GR3), so it abandons the statements around it and the sections they owe run
    /// first (X3.23-1985 USE FOR DEBUGGING general rules 5 d and 6 c, kb/Work PB4857): the IF around a RAISE that a declarative resumes elsewhere,
    /// and the IF around the RESUME inside the declarative. RESUME AT NEXT STATEMENT continues after the lowest statement, so the IF around the
    /// RAISE completes and runs its section once. The statements are 2002 introductions and the facility is 1985's, so the one program that has
    /// both is the 2002 compile that keeps the removed declarative (--permissive).</summary>
    [Fact]
    public void RaiseAndResume_LandTheSectionsTheAbandonedStatementsOwe()
    {
        const string src = """
            >>TURN EC-USER-RZ CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGRESUME.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 D PIC 9 VALUE 1.
            01 K PIC 9 VALUE 1.
            PROCEDURE DIVISION.
            DECLARATIVES.
            DB0 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF D.
            DB0-B.
                DISPLAY "  DB0".
            HZ SECTION.
                USE AFTER EXCEPTION CONDITION EC-USER-RZ.
            HZ-P.
                DISPLAY "  HANDLER".
                IF K = 1 IF D = 1 RESUME AT M3 END-IF END-IF.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                DISPLAY "A: RESUME AT M3".
                IF D = 1 RAISE EXCEPTION EC-USER-RZ.
                DISPLAY "NOT HERE".
            M2.
                DISPLAY "NOT HERE EITHER".
            M3.
                DISPLAY "M3".
                MOVE 2 TO K.
                DISPLAY "B: RESUME AT NEXT STATEMENT".
                IF D = 1 RAISE EXCEPTION EC-USER-RZ.
                DISPLAY "AFTER B".
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2002, permissive: true);
        Assert.True(ok, detail);
        var lines = stdout.Replace("\r", "").TrimEnd('\n').Split('\n');
        Assert.Equal(["A: RESUME AT M3", "  HANDLER", "  DB0", "  DB0", "M3", "B: RESUME AT NEXT STATEMENT", "  HANDLER", "  DB0", "AFTER B"], lines);
    }

    /// <summary>The pointer verbs hold a based item or an addressed item as a DATA ITEM only: ALLOCATE's based item, SET ADDRESS OF's receiver and
    /// ADDRESS OF's operand are each an operand the statement explicitly references (X3.23-1985 USE FOR DEBUGGING general rule 5, kb/Work
    /// PB4855), so the section of each runs after the statement. The verbs are 2002 introductions and the facility is 1985's, so the one
    /// program that has both is the 2002 compile that keeps the removed declarative (--permissive).</summary>
    [Fact]
    public void PointerVerbOperands_AreReferences()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGPTRREF.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BP BASED.
               02 BX PIC X(4).
            01 BQ BASED.
               02 BZ PIC X(4).
            01 P USAGE POINTER.
            01 AX PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
            DECLARATIVES.
            DB1 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF BP.
            DB1-B.
                DISPLAY "  DB1 " DEBUG-NAME (1:2).
            DB2 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF BQ.
            DB2-B.
                DISPLAY "  DB2 " DEBUG-NAME (1:2).
            DB3 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF AX.
            DB3-B.
                DISPLAY "  DB3 " DEBUG-NAME (1:2).
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                DISPLAY "ALLOCATE BP".
                ALLOCATE BP RETURNING P.
                DISPLAY "SET ADDRESS OF BQ".
                SET ADDRESS OF BQ TO P.
                DISPLAY "SET P TO ADDRESS OF AX".
                SET P TO ADDRESS OF AX.
                DISPLAY "END".
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2002, permissive: true);
        Assert.True(ok, detail);
        var lines = stdout.Replace("\r", "").TrimEnd('\n').Split('\n');
        Assert.Equal(["ALLOCATE BP", "  DB1 BP", "SET ADDRESS OF BQ", "  DB2 BQ", "SET P TO ADDRESS OF AX", "  DB3 AX", "END"], lines);
    }

    /// <summary>A Format-2 table SORT names a WHOLE table and its key data-names without the subscripts of the sorted level (kb/Work PB4855):
    /// each is an operand the statement explicitly references and whose content it changes (X3.23-1985 USE FOR DEBUGGING general rules 5 and
    /// 6 c), and DEBUG-CONTENTS is "the content of the data item referenced" (rule 24 c): every occurrence of the item under the enclosing
    /// occurrence the statement names, in storage order, after the sort. DEBUG-SUB-1 is that enclosing occurrence (rule 14) and DEBUG-SUB-2,
    /// the level the reference ranges over, is spaces. A subscript the statement writes is a reference too (I). A table in a REDEFINES class
    /// (a shared area) is the same. The table SORT is a 2002 introduction and the facility is 1985's, so the one program that has both is the
    /// 2002 compile that keeps the removed declarative (--permissive).</summary>
    [Fact]
    public void TableSortOperands_AreWholeTableReferences()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGTSORT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TB.
               02 ROW OCCURS 2 TIMES.
                  03 E OCCURS 3 TIMES.
                     04 K PIC X.
                     04 V PIC 9.
            01 RB.
               02 RX PIC X(12).
            01 RR REDEFINES RB.
               02 S OCCURS 4 TIMES PIC XXX.
            01 I PIC 9 VALUE 2.
            01 X5 PIC X(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DB1 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF K.
            DB1-B.
                MOVE DEBUG-SUB-2 TO X5.
                DISPLAY "  DB1 " DEBUG-NAME (1:1) " " DEBUG-SUB-1 " [" X5 "] [" DEBUG-CONTENTS (1:4) "]".
            DB2 SECTION.
                USE FOR DEBUGGING ON E.
            DB2-B.
                DISPLAY "  DB2 " DEBUG-NAME (1:1) " " DEBUG-SUB-1 " [" DEBUG-CONTENTS (1:7) "]".
            DB3 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF I.
            DB3-B.
                DISPLAY "  DB3 [" DEBUG-CONTENTS (1:2) "]".
            DB4 SECTION.
                USE FOR DEBUGGING ON S.
            DB4-B.
                DISPLAY "  DB4 [" DEBUG-CONTENTS (1:13) "]".
            DB5 SECTION.
                USE FOR DEBUGGING ON V.
            DB5-B.
                DISPLAY "  DB5 NOT EXPECTED".
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                MOVE "a1b2c3d4e5f6" TO TB.
                MOVE "DDDCCCBBBAAA" TO RX.
                DISPLAY "SORT E (I)".
                SORT E (I) DESCENDING K.
                DISPLAY TB.
                DISPLAY "SORT S".
                SORT S ASCENDING.
                DISPLAY RX.
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2002, permissive: true);
        Assert.True(ok, detail);
        var lines = stdout.Replace("\r", "").TrimEnd('\n').Split('\n');
        Assert.Equal(["SORT E (I)", "  DB1 K +0002 [     ] [fed ]", "  DB2 E +0002 [f6e5d4 ]", "  DB3 [2 ]", "a1b2c3f6e5d4",
            "SORT S", "  DB4 [AAABBBCCCDDD ]", "AAABBBCCCDDD"], lines);
    }

    /// <summary>INITIALIZE of a WHOLE dynamic-capacity table names the table, every current occurrence of it (kb/Work PB4855): identifier-1 is
    /// the one operand the statement explicitly references and stores (X3.23-1985 USE FOR DEBUGGING general rules 5 and 6 c; rule 9 — the
    /// element stores the expansion adds are no references of their own), so each kind of section runs ONCE after the statement, with the
    /// initialized occurrences as DEBUG-CONTENTS (rule 24 c) and no subscript. A table at capacity zero still names the table: the section
    /// runs, its DEBUG-CONTENTS spaces (the empty content, rule 11's space fill), where a table(ALL) argument over no occurrence would raise
    /// EC-ARGUMENT-FUNCTION. OCCURS DYNAMIC is a 2014 introduction, so the program is the 2023 compile that keeps the removed declarative
    /// (--permissive).</summary>
    [Fact]
    public void InitializeWholeDynamicTable_IsOneWholeTableReference()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DBGINDYN.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TB.
               02 T OCCURS DYNAMIC CAPACITY IN TC PIC X.
            01 UB.
               02 U OCCURS DYNAMIC CAPACITY IN UC PIC X.
            01 WB.
               02 W OCCURS DYNAMIC CAPACITY IN WC PIC X.
            01 X5 PIC X(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            DB1 SECTION.
                USE FOR DEBUGGING ON T.
            DB1-B.
                MOVE DEBUG-SUB-1 TO X5.
                DISPLAY "  DB1 " DEBUG-NAME (1:1) " [" X5 "] [" DEBUG-CONTENTS (1:4) "]".
            DB2 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF U.
            DB2-B.
                MOVE DEBUG-SUB-1 TO X5.
                DISPLAY "  DB2 " DEBUG-NAME (1:1) " [" X5 "] [" DEBUG-CONTENTS (1:4) "]".
            DB3 SECTION.
                USE FOR DEBUGGING ON ALL REFERENCES OF W.
            DB3-B.
                DISPLAY "  DB3 " DEBUG-NAME (1:1) " [" DEBUG-CONTENTS (1:4) "]".
            END DECLARATIVES.
            MAIN SECTION.
            M1.
                SET TC TO 3.
                SET UC TO 2.
                DISPLAY "INITIALIZE T".
                INITIALIZE T REPLACING ALPHANUMERIC BY "Z".
                DISPLAY "INITIALIZE U".
                INITIALIZE U REPLACING ALPHANUMERIC BY "Y".
                DISPLAY "INITIALIZE W".
                SET WC TO 0.
                INITIALIZE W.
                DISPLAY "END".
                STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2023, permissive: true);
        Assert.True(ok, detail);
        var lines = stdout.Replace("\r", "").TrimEnd('\n').Split('\n');
        Assert.Equal(["INITIALIZE T", "  DB1 T [     ] [ZZZ ]", "INITIALIZE U", "  DB2 U [     ] [YY  ]", "INITIALIZE W", "  DB3 W [    ]", "END"],
            lines);
    }
}
