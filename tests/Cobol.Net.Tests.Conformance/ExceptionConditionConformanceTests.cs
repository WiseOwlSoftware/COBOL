// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The EC exception-condition model (ISO/IEC 1989:2023 §14.6.13 + §7.3.25 TURN + §14.9.29 RAISE + §14.9.33
/// RESUME + §14.9.49 Format-3 USE + §14.9.18/§14.9.14 RAISING + §15.28–15.33 EXCEPTION-* functions;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12): the per-GR conformance net. Every behavioral test is
/// SPEC-PINNED (the legacy oracle has NO EC model — COBOL-2002+; expected values derive from the cited rules).
/// All EC programs compile at <c>--std 2023</c>; the per-edition gating facts compile at 85 and assert the
/// targeted not-in-this-edition diagnostic (the four-compilers rule).
/// </summary>
public sealed class ExceptionConditionConformanceTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler(2023);

    /// <summary>Compile-and-run on the greenfield compiler at 2023; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"COBOL.NET failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>Compile-and-run; assert ABNORMAL run-unit termination (nonzero exit — the §14.6.13.1.3 #5/#7
    /// fatal default / the settled §18.16 implementor choice) whose stderr names the exception condition.</summary>
    private static void AssertFatal(string source, string ecName, string? expectedStdout = null)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean with stdout:\n{stdout}");
        Assert.Contains(ecName, detail);
        if (expectedStdout is not null) Assert.Equal(expectedStdout, stdout);
    }

    /// <summary>A declaratives-bearing program skeleton (sections after END DECLARATIVES per §14.2.4).</summary>
    private static string Prog(string name, string turn, string env, string ws, string decls, string proc) => $"""
        {turn}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {name}.
        {env}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        DECLARATIVES.
        {decls}
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-PARA.
        {proc}
        """;

    // ── RAISE + TURN scoping (§14.9.29 / §7.3.25 / §14.6.13.1.1) ─────────────────────────────────────────────

    [Fact]   // §14.9.29 GR1 + §14.6.13.1.4: enabled nonfatal RAISE sets the last exception status and the F3
             // declarative handles it; RESUME AT NEXT STATEMENT continues past; the status persists after.
    public void Raise_Enabled_DeclarativeHandles_ResumeNextStatement_StatusPersists()
        => AssertSpec(Prog("ECT001", ">>TURN EC-USER-DEMO CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-DEMO.
            EC-H-P.
                DISPLAY "HANDLER: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-USER-DEMO.
                DISPLAY "AFTER: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """), "HANDLER: EC-USER-DEMO\nAFTER: EC-USER-DEMO");

    [Fact]   // §14.6.13.1.1 (default OFF, §7.3.25.4 GR1) + §14.6.13.1.4 first sentence: with checking not
             // enabled a NONFATAL raise acts as if the exception did not occur — no status, execution continues.
    public void Raise_CheckingOff_Nonfatal_ActsAsContinue()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT002.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-DEMO.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{new string(' ', 31)}]");

    [Fact]   // §7.3.25.4 GR5/GR6: a TURN applies to SUCCEEDING statements until the next toggle — ON…OFF scopes
             // by source order within one paragraph.
    public void Turn_OffMidSource_ScopesBySourceOrder()
        => AssertSpec("""
            >>TURN EC-USER-A CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT003.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-A.
                DISPLAY "S1: " FUNCTION EXCEPTION-STATUS.
                SET LAST EXCEPTION TO OFF.
            >>TURN EC-USER-A CHECKING OFF
                RAISE EXCEPTION EC-USER-A.
                DISPLAY "S2: [" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S1: EC-USER-A\nS2: [{new string(' ', 31)}]");

    [Fact]   // §7.3.25.4 GR2: EC-ALL enables checking for every exception-name (here an EC-USER-* level-3).
    public void Turn_EcAll_CoversEveryName()
        => AssertSpec("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT004.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-X.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "S: EC-USER-X");

    [Fact]   // §7.3.25.4 GR3: a level-2 name enables checking for its level-3 children.
    public void Turn_Level2_CoversItsChildren()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT005.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-Y.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "S: EC-USER-Y");

    [Fact]   // §14.6.13.1.3 #8 (checking NOT enabled, fatal category): implementor-defined — this implementation
             // terminates the run unit loudly (the §1.4 doctrine; recorded in the deep-dive).
    public void Raise_Fatal_CheckingOff_TerminatesLoudly()
        => AssertFatal("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT006.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                RAISE EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-SIZE-OVERFLOW", "BEFORE");

    [Fact]   // §14.6.13.1.3 #5 NOTE 2 + §14.9.33.4 GR2: a declarative's RESUME AT NEXT STATEMENT suppresses the
             // FATAL termination — execution continues after the RAISE.
    public void Raise_Fatal_DeclarativeResumes_SuppressesTermination()
        => AssertSpec(Prog("ECT007", ">>TURN EC-SIZE CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
            EC-H-P.
                DISPLAY "HANDLER".
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "SURVIVED".
                STOP RUN.
            """), "HANDLER\nSURVIVED");

    [Fact]   // §14.9.33.4 GR3: RESUME AT procedure-name transfers control as if a GO TO.
    public void Resume_AtProcedureName_TransfersControl()
        => AssertSpec(Prog("ECT008", ">>TURN EC-USER CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-J.
            EC-H-P.
                RESUME AT ELSEWHERE.
            """, """
                RAISE EXCEPTION EC-USER-J.
                DISPLAY "SKIPPED".
                STOP RUN.
            ELSEWHERE.
                DISPLAY "RESUMED-AT".
                STOP RUN.
            """), "RESUMED-AT");

    [Fact]   // §14.9.49.4 GR3e before GR3g: the level-3 entry is selected over the EC-ALL entry regardless of
             // source order; an unmatched name falls to the EC-ALL tier.
    public void UseF3_SelectionTiers_Level3BeatsEcAll()
        => AssertSpec(Prog("ECT009", ">>TURN EC-USER CHECKING ON", "", "", """
            ALL-H SECTION. USE AFTER EXCEPTION CONDITION EC-ALL.
            ALL-H-P.
                DISPLAY "ALL-HANDLER".
                RESUME AT NEXT STATEMENT.
            SPEC-H SECTION. USE AFTER EC EC-USER-T.
            SPEC-H-P.
                DISPLAY "SPECIFIC-HANDLER".
                RESUME AT NEXT STATEMENT.
            """, """
                RAISE EXCEPTION EC-USER-T.
                RAISE EXCEPTION EC-USER-OTHER.
                DISPLAY "DONE".
                STOP RUN.
            """), "SPECIFIC-HANDLER\nALL-HANDLER\nDONE");

    // ── The EXCEPTION-* functions + WITH LOCATION (§15.28–15.33 / §7.3.25.4 GR7) ─────────────────────────────

    [Fact]   // §15.32.3 r2 + §15.30.3 r2: WITH LOCATION captures the statement name and the three-part location.
    public void WithLocation_CapturesStatementAndLocation()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-USER CHECKING ON WITH LOCATION
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT010.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-L.
                DISPLAY "STMT: " FUNCTION EXCEPTION-STATEMENT.
                DISPLAY "LOC: " FUNCTION EXCEPTION-LOCATION.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Contains("STMT: RAISE", stdout);
        Assert.Contains("LOC: ECT010; MAIN-PARA;", stdout);   // "element; paragraph; line-id" (§15.30.3 r2)
    }

    [Fact]   // §15.32.3 r3: the recorded name comes from Table 12's 'Statement name' column — "GO TO", never the
             // spelled leading token. A subscripted DEPENDING operand raises EC-BOUND-SUBSCRIPT INSIDE the GO TO
             // (§8.4.2.3.4 GR2); the F3 declarative reads the name and RESUMEs past (kb/Work R04).
    public void ExceptionStatement_GoTo_ReturnsTheTable12Name()
        => AssertSpec(Prog("ECT014", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION", "", """
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
            EC-H-P.
                DISPLAY "STMT=[" FUNCTION EXCEPTION-STATEMENT "]".
                RESUME AT NEXT STATEMENT.
            """, """
                GO TO TGT-A TGT-B DEPENDING ON WS-CELL(WS-I).
                DISPLAY "PAST".
                STOP RUN.
            TGT-A.
            TGT-B.
                DISPLAY "WRONG-TARGET".
                STOP RUN.
            """), $"STMT=[{"GO TO",-63}]\nPAST");

    [Fact]   // §15.32.3 r3 + the optional word TO (goToStatement : GO TO? …): `GO PARA.` is the SAME statement
             // kind with no TO token anywhere — the name must still be "GO TO". This spelling is the one that
             // defeats any token-derived name (kb/Work R04: first-token gave "GO"; longest-token-match also
             // gives "GO" because the tokens never spell TO).
    public void ExceptionStatement_GoWithoutTo_StillReturnsGoTo()
        => AssertSpec(Prog("ECT015", ">>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION", "", """
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
            EC-H-P.
                DISPLAY "STMT=[" FUNCTION EXCEPTION-STATEMENT "]".
                RESUME AT NEXT STATEMENT.
            """, """
                GO TGT-A TGT-B DEPENDING ON WS-CELL(WS-I).
                DISPLAY "PAST".
                STOP RUN.
            TGT-A.
            TGT-B.
                DISPLAY "WRONG-TARGET".
                STOP RUN.
            """), $"STMT=[{"GO TO",-63}]\nPAST");

    [Fact]   // §15.32.3 r1 / §15.30.3 r1: WITHOUT the LOCATION phrase no location information is saved —
             // EXCEPTION-STATEMENT is all spaces, EXCEPTION-LOCATION a single space.
    public void WithoutLocation_StatementAndLocationAreSpaces()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT011.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-L.
                DISPLAY "T=[" FUNCTION EXCEPTION-STATEMENT "]".
                DISPLAY "L=[" FUNCTION EXCEPTION-LOCATION "]".
                STOP RUN.
            """, $"T=[{new string(' ', 63)}]\nL=[ ]");

    [Fact]   // §14.9.39 Format 13: SET LAST EXCEPTION TO OFF — the status indicates no exception condition.
    public void SetLastExceptionToOff_ClearsTheStatus()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT012.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-C.
                SET LAST EXCEPTION TO OFF.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{new string(' ', 31)}]");

    // ── The EC-SIZE bridge (§14.7.5 ↔ Table 13; arithmetic without the phrase) ───────────────────────────────

    [Fact]   // §14.7.5 case 2 + Table 13: a zero divisor under enabled EC-SIZE checking raises the PRECISE
             // level-3 name EC-SIZE-ZERO-DIVIDE; the receiver is unchanged; RESUME AT NEXT STATEMENT continues.
    public void SizeError_ZeroDivide_DeclarativeSeesPreciseName_ReceiverUnchanged()
        => AssertSpec(Prog("ECT013", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 5.
            01 WS-B PIC 9(3) VALUE 0.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                DIVIDE WS-A BY WS-B GIVING WS-C.
                DISPLAY "C=" WS-C.
                STOP RUN.
            """), "H: EC-SIZE-ZERO-DIVIDE\nC=111");

    [Fact]   // Table 13 "EC-SIZE-TRUNCATION — significant digits truncated in store": a store overflow under
             // enabled checking latches the truncation name; the receiver keeps its prior value (§14.7.5).
    public void SizeError_StoreTruncation_LatchesTruncationName()
        => AssertSpec(Prog("ECT014", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 900.
            01 WS-B PIC 9(3) VALUE 200.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                ADD WS-A WS-B GIVING WS-C.
                DISPLAY "C=" WS-C.
                STOP RUN.
            """), "H: EC-SIZE-TRUNCATION\nC=111");

    [Fact]   // §14.6.13.1.3 #5/#7: enabled EC-SIZE, no phrase, no qualifying declarative — the run unit
             // terminates abnormally on the fatal condition.
    public void SizeError_NoPhraseNoDeclarative_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT015.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(3) VALUE 5.
            01 WS-B PIC 9(3) VALUE 0.
            01 WS-C PIC 9(3) VALUE 111.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                DIVIDE WS-A BY WS-B GIVING WS-C.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-SIZE-ZERO-DIVIDE", "BEFORE");

    [Fact]   // §14.6.13.1.3 #1 / §14.6.13.1.4 #1: the statement's own ON SIZE ERROR phrase handles the
             // condition — the declarative does NOT run; the last exception status IS set (checking enabled).
    public void SizeError_OnSizeErrorPhrase_WinsOverDeclarative()
        => AssertSpec(Prog("ECT016", ">>TURN EC-SIZE CHECKING ON", "", """
            01 WS-A PIC 9(3) VALUE 900.
            01 WS-B PIC 9(3) VALUE 200.
            01 WS-C PIC 9(3) VALUE 111.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
            EC-H-P.
                DISPLAY "DECL".
                RESUME AT NEXT STATEMENT.
            """, """
                ADD WS-A WS-B GIVING WS-C
                    ON SIZE ERROR DISPLAY "PHRASE"
                END-ADD.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """), "PHRASE\nS: EC-SIZE-TRUNCATION");

    // ── The EC-OVERFLOW bridge (STRING §14.9.43 GR8b / UNSTRING §14.9.48 GR16b) ──────────────────────────────

    [Fact]   // §14.9.43 GR8b: a STRING overflow without the ON OVERFLOW phrase raises EC-OVERFLOW-STRING
             // (nonfatal — execution continues either way, §14.6.13.1.4 #3/#4); the receiver holds the
             // transferred prefix.
    public void StringOverflow_NoPhrase_RaisesEcOverflowString()
        => AssertSpec(Prog("ECT017", ">>TURN EC-OVERFLOW CHECKING ON", "", """
            01 WS-R PIC X(3) VALUE SPACES.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-OVERFLOW.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
            """, """
                STRING "ABCDE" DELIMITED BY SIZE INTO WS-R.
                DISPLAY "R=" WS-R.
                STOP RUN.
            """), "H: EC-OVERFLOW-STRING\nR=ABC");

    // ── The EC-I-O bridge (§9.1.13.1 status → EC) ────────────────────────────────────────────────────────────

    private const string IoEnv = """
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT TF ASSIGN TO "EC-TF".
        """;

    private const string IoFd = """
        FD TF.
        01 TF-REC PIC X(10).
        """;

    /// <summary>An I-O program skeleton: declaratives + FILE SECTION (the Prog skeleton has no file slot).</summary>
    private static string IoProg(string name, string turn, string decls, string proc) => $"""
        {turn}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {name}.
        {IoEnv}
        DATA DIVISION.
        FILE SECTION.
        {IoFd}
        PROCEDURE DIVISION.
        DECLARATIVES.
        {decls}
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-PARA.
            OPEN OUTPUT TF.
            MOVE "HELLO" TO TF-REC.
            WRITE TF-REC.
            CLOSE TF.
            OPEN INPUT TF.
        {proc}
        """;

    [Fact]   // §9.1.13.1: I-O status 10 with enabled EC-I-O checking raises EC-I-O-AT-END (nonfatal '1x'); the
             // F3 declarative selects it; §15.28.4 r1c — EXCEPTION-FILE is the status + the SELECT file-name.
    public void IoAtEnd_NoPhrase_RaisesEcIoAtEnd_ExceptionFile()
        => AssertSpec(IoProg("ECT018", ">>TURN EC-I-O CHECKING ON", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                DISPLAY "F: " FUNCTION EXCEPTION-FILE.
                RESUME AT NEXT STATEMENT.
            """, """
                READ TF.
                READ TF.
                DISPLAY "AFTER-READS".
                CLOSE TF.
                STOP RUN.
            """), "H: EC-I-O-AT-END\nF: 10TF\nAFTER-READS");

    [Fact]   // §15.29.4 r1 — EXCEPTION-FILE-N, the national twin of EXCEPTION-FILE (P10 Step-11 EC-N wave):
             // r1a two national zeros before any EC-I-O condition; r1c the I-O status in national characters +
             // the SELECT file-name converted to the runtime national character set (the ONE NationalOf
             // repertoire projection of the same File() rendering). §15.31.3 r1 — EXCEPTION-LOCATION-N is one
             // national space when the enabling TURN lacked WITH LOCATION. FUNCTION LENGTH (§15.50) pins the
             // character-position counts of the national results.
    public void IoAtEnd_ExceptionFileN_NationalTwin()
        => AssertSpec($"""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT018N.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            WORKING-STORAGE SECTION.
            01 WS-L1 PIC 9.
            01 WS-L2 PIC 9.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "FN: " FUNCTION EXCEPTION-FILE-N.
                MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N) TO WS-L1.
                MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION-N) TO WS-L2.
                DISPLAY "L1: " WS-L1 " L2: " WS-L2.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT TF.
                MOVE "HELLO" TO TF-REC.
                WRITE TF-REC.
                CLOSE TF.
                OPEN INPUT TF.
                DISPLAY "P: " FUNCTION EXCEPTION-FILE-N.
                READ TF.
                READ TF.
                CLOSE TF.
                STOP RUN.
            """, "P: 00\nFN: 10TF\nL1: 4 L2: 1");

    [Fact]   // §9.1.13.1 / §14.6.13.1.4 #1: the statement's AT END phrase covers the '1x' family — the EC
             // declarative is NOT selected.
    public void IoAtEnd_AtEndPhrase_SuppressesDeclarative()
        => AssertSpec(IoProg("ECT019", ">>TURN EC-I-O CHECKING ON", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
            EC-H-P.
                DISPLAY "DECL".
            """, """
                READ TF.
                READ TF AT END DISPLAY "ATEND".
                CLOSE TF.
                STOP RUN.
            """), "ATEND");

    [Fact]   // §9.1.13.1 fatal classes ('3x') + §14.9.49.4 GR12c: an enabled fatal I-O status with no handler
             // terminates the run unit abnormally (checking OFF keeps the historical continue-on-error).
    public void IoPermanentError_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-I-O CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT020.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TF ASSIGN TO "EC-NO-SUCH-FILE".
            DATA DIVISION.
            FILE SECTION.
            FD TF.
            01 TF-REC PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                OPEN INPUT TF.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-I-O-PERMANENT-ERROR", "BEFORE");

    // ── The EC-PROGRAM bridge (CALL/CANCEL, §14.9.4.4 GR3b/GR3f) ─────────────────────────────────────────────

    [Fact]   // §14.9.4.4 GR3b: CALL of an unknown program under enabled EC-PROGRAM checking raises
             // EC-PROGRAM-NOT-FOUND; the declarative's RESUME AT NEXT STATEMENT suppresses the fatal default.
    public void CallNotFound_Enabled_DeclarativeResumes()
        => AssertSpec(Prog("ECT021", ">>TURN EC-PROGRAM CHECKING ON", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                CALL "NOSUCHPROG".
                DISPLAY "SURVIVED".
                STOP RUN.
            """), "H: EC-PROGRAM-NOT-FOUND\nSURVIVED");

    [Fact]   // §14.6.13.1.3 #5/#7: enabled EC-PROGRAM, no phrase, no declarative — fatal termination (every
             // EC-PROGRAM-* is fatal, Table 13).
    public void CallNotFound_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-PROGRAM CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT022.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                CALL "NOSUCHPROG".
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-PROGRAM-NOT-FOUND", "BEFORE");

    [Fact]   // §14.9.4.4 GR3b names a condition DISTINCT from EC-PROGRAM-NOT-FOUND for the NULL case: "If the data
             // item referenced by identifier-1 contains the predefined address NULL, the EC-PROGRAM-PTR-NULL
             // exception condition is set to exist." (GR3g's "invalid program address … undefined" is the NON-null
             // bad-address case.) Fatal per Table 13, so with no handler the run unit terminates — the arm the
             // corpus golden ec_program_ptr_null cannot express, since it must run clean and RESUMEs instead.
    public void CallPointerNull_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-PROGRAM-PTR-NULL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT022P.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PPTR USAGE PROGRAM-POINTER.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                SET PPTR TO NULL.
                CALL PPTR.
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-PROGRAM-PTR-NULL", "BEFORE");

    [Fact]   // §14.9.18.4 GR1b is CONDITIONAL: "If the RAISING phrase is specified, an exception condition is
             // raised in the activating runtime element IF CHECKING FOR THAT EXCEPTION CONDITION IS ENABLED in
             // the activating runtime element". V58MAIN enables none, so the callee's GOBACK RAISING raises
             // NOTHING in it and execution continues after the CALL. GR3 agrees for the main-program half: a
             // GOBACK with no activator "operates as if executing a STOP statement … A RAISING phrase, if
             // specified, is ignored."
             //
             // ⚠ SEPARATELY COMPILED on purpose, and it is the only way this rule is reachable: within ONE
             // compilation group a >>TURN anywhere makes the whole group EC-active, so the CALL site emits its
             // own propagation pickup and the unchecked-activator path is never taken. Two assemblies put the
             // activator genuinely outside the callee's checking state.
             //
             // Before the fix ProgramTable.ApplyPropagationDefault threw for a staged FATAL condition, citing
             // §14.6.13.1.3 #8 — a misapplication, since #8's latitude governs what may happen once a fatal
             // condition EXISTS and GR1b stops it existing in an unchecked activator at all.
    public void GobackRaising_IntoUncheckedActivator_IsNotRaisedThere()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V58MAIN.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "V58SUB".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            """, """
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V58SUB.
            PROCEDURE DIVISION.
            SUB-P.
                DISPLAY "IN-SUB".
                GOBACK RAISING EXCEPTION EC-BOUND-SUBSCRIPT.
            """);
        Assert.True(ok, $"the unchecked activator should continue past the CALL: {detail}" + stdout);
        Assert.Equal("IN-SUB\nAFTER-CALL", stdout);
    }

    [Fact]   // §14.9.23.4 GR7c raises EC-OO-UNIVERSAL only "if checking for it is enabled in BOTH the activated
             // method and the activating runtime element". Here the >>TURN is switched OFF before the CLASS, so
             // only the ACTIVATOR has it enabled — the condition is therefore NOT set, the declarative must not
             // select, and NOTHING may be attributed to EC-OO-UNIVERSAL.
             //
             // The nonconforming crossing still cannot proceed (PIC 9(6) argument into a PIC 9(4) formal, which
             // only a universal receiver can defer to run time), so it stops as a CobolImplementorFatalException
             // — §14.6.13.1.1 NOTE 3 latitude, carrying NO exception-name. The assertion that the message does
             // NOT contain the EC name is the whole point of the test: attributing it would be the bug.
             // Its twin, where checking IS enabled in both, is the corpus golden ec_oo_universal_both.
    public void OoUniversal_EnabledInActivatorOnly_StopsWithoutAttributingTheCondition()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-OO-UNIVERSAL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V55NOTB.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS CUNIVN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE.
            01 C USAGE OBJECT REFERENCE CUNIVN.
            01 W PIC 9(6) VALUE 000007.
            PROCEDURE DIVISION.
            DECLARATIVES.
            H SECTION.
                USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL.
            H-P.
                DISPLAY "HANDLED".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-P.
                INVOKE CUNIVN "NEW" RETURNING C.
                SET O TO C.
                INVOKE O "TAKE" USING W.
                DISPLAY "AFTER".
                STOP RUN.
            END PROGRAM V55NOTB.

            >>TURN EC-OO-UNIVERSAL CHECKING OFF
            IDENTIFICATION DIVISION.
            CLASS-ID. CUNIVN.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. TAKE.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK PIC 9(4).
            PROCEDURE DIVISION USING LK.
            MAIN-P.
                DISPLAY "IN-TAKE".
            END METHOD TAKE.
            END OBJECT.
            END CLASS CUNIVN.
            """);
        Assert.False(ok, $"the nonconforming universal crossing must not proceed; stdout:\n{stdout}");
        Assert.DoesNotContain("EC-OO-UNIVERSAL", detail);
        Assert.DoesNotContain("HANDLED", stdout);
        Assert.DoesNotContain("AFTER", stdout);
    }

    [Fact]   // §14.9.4.4 GR3h + §14.6.13.1.4 #1: the CALL's ON EXCEPTION phrase handles the condition — no
             // termination; the last exception status IS set (checking enabled).
    public void CallNotFound_OnExceptionPhrase_WinsAndStatusSet()
        => AssertSpec("""
            >>TURN EC-PROGRAM CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT023.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "NOSUCHPROG"
                    ON EXCEPTION DISPLAY "PHRASE"
                END-CALL.
                DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """, "PHRASE\nS: EC-PROGRAM-NOT-FOUND");

    // ── The EC-ARGUMENT-FUNCTION bridge (§15.3 / Table 13) ───────────────────────────────────────────────────

    [Fact]   // §15.3: with checking DISABLED an intrinsic argument violation yields the default result 0.
    public void ArgumentFunction_CheckingOff_DefaultResultZero()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT024.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(3) VALUE 999.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-X = FUNCTION LOG(0).
                DISPLAY "X=" WS-X.
                STOP RUN.
            """, "X=000");

    [Fact]   // Table 13 (EC-ARGUMENT-FUNCTION, fatal): with checking ENABLED the same violation raises and —
             // with no handler — terminates the run unit abnormally.
    public void ArgumentFunction_Enabled_NoHandler_FatalTerminates()
        => AssertFatal("""
            >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT025.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(3) VALUE 999.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                COMPUTE WS-X = FUNCTION LOG(0).
                DISPLAY "NEVER".
                STOP RUN.
            """, "EC-ARGUMENT-FUNCTION", "BEFORE");

    [Fact]   // §14.6.13.1.3 #5 NOTE 2: the declarative's RESUME AT NEXT STATEMENT suppresses the fatal
             // termination; the aborted statement's receiver is unchanged.
    public void ArgumentFunction_Enabled_DeclarativeResumes_ReceiverUnchanged()
        => AssertSpec(Prog("ECT026", ">>TURN EC-ARGUMENT-FUNCTION CHECKING ON", "", """
            01 WS-X PIC 9(3) VALUE 999.
            """, """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
            EC-H-P.
                DISPLAY "H: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            """, """
                COMPUTE WS-X = FUNCTION NUMVAL("12X34").
                DISPLAY "X=" WS-X.
                STOP RUN.
            """), "H: EC-ARGUMENT-FUNCTION\nX=999");

    // ── RAISING propagation (GOBACK / EXIT PROGRAM, §14.9.18 / §14.9.14) ─────────────────────────────────────

    [Fact]   // §14.9.18 GR: GOBACK RAISING stages the condition for the ACTIVATOR — raised at the end of the
             // CALL; the caller's F3 declarative selects it and RESUME continues.
    public void GobackRaising_PropagatesToActivator_DeclarativeHandles()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT027.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PROP.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT027S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT027S.
            PROCEDURE DIVISION RAISING EC-USER-PROP.
            SUB-PARA.
                GOBACK RAISING EXCEPTION EC-USER-PROP.
            """, "CAUGHT: EC-USER-PROP\nAFTER-CALL");

    [Fact]   // §14.9.18.2: RAISING LAST EXCEPTION re-stages the callee's last exception status for the activator.
    public void GobackRaisingLastException_PropagatesTheLastStatus()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT028.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-Q.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT028S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT028S.
            PROCEDURE DIVISION.
            SUB-PARA.
                RAISE EXCEPTION EC-USER-Q.
                GOBACK RAISING LAST EXCEPTION.
            """, "CAUGHT: EC-USER-Q\nAFTER-CALL");

    [Fact]   // §14.9.14 GR2: EXIT PROGRAM in a program NOT under the control of a calling runtime element is
             // CONTINUE — "no exception condition is raised even if the RAISING phrase is specified".
    public void ExitProgramRaising_InMainProgram_IsContinue()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT029.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EXIT PROGRAM RAISING EXCEPTION EC-SIZE-OVERFLOW.
                DISPLAY "FELL-THROUGH".
                STOP RUN.
            """, "FELL-THROUGH");

    [Fact]   // §14.9.14 GR3 (the GOBACK rules): EXIT PROGRAM RAISING in a CALLED program stages the condition
             // for the activator exactly as GOBACK RAISING does.
    public void ExitProgramRaising_InCalledProgram_Propagates()
        => AssertSpec("""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT030.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-EXIT.
            EC-H-P.
                DISPLAY "CAUGHT: " FUNCTION EXCEPTION-STATUS.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                CALL "ECT030S".
                DISPLAY "AFTER-CALL".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT030S.
            PROCEDURE DIVISION RAISING EC-USER-EXIT.
            SUB-PARA.
                EXIT PROGRAM RAISING EXCEPTION EC-USER-EXIT.
            """, "CAUGHT: EC-USER-EXIT\nAFTER-CALL");

    [Fact]   // §14.9.18.4 GR3 (the P13 review C13 fix — this test previously pinned the PRE-fix behavior): a
             // GOBACK in a program NOT under the control of a calling runtime element "operates as if executing
             // a STOP statement … A RAISING phrase, if specified, is ignored" — even with checking enabled and a
             // FATAL name. Normal termination, no condition raised (the earlier "run-unit boundary activator"
             // reading has no GR — GR1b's staging applies only to a CALLED program's activator).
    public void GobackRaisingFatal_InMainProgram_IsIgnored_NormalTermination()
        => AssertSpec("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT031.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
            """, "BEFORE");

    // ── Diagnostics (the COBOLNET07xx/08xx band) ─────────────────────────────────────────────────────────────

    [Fact]   // §14.9.29.3 SR1: only a LEVEL-3 exception-name may be raised.
    public void Raise_Level2Name_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT032.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-SIZE.
                STOP RUN.
            """, 2023), "COBOLNET0710");

    [Fact]   // §14.6.13.1 / §7.3.25.3 SR2: an unknown exception-name is rejected.
    public void Raise_UnknownName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT033.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-BOGUS-NAME.
                STOP RUN.
            """, 2023), "COBOLNET0711");

    [Fact]   // §14.9.33.3 SR1: RESUME only in a declarative.
    public void Resume_OutsideDeclaratives_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT034.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RESUME AT NEXT STATEMENT.
                STOP RUN.
            """, 2023), "COBOLNET0712");

    [Fact]   // §14.9.33.3 SR2: RESUME shall not appear in a GLOBAL-phrase declarative.
    public void Resume_InGlobalDeclarative_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT035.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            G-H SECTION. USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON TF.
            G-H-P.
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0713");

    [Fact]   // §14.9.49.3 SR13: FILE only with an exception-name beginning EC-I-O.
    public void UseF3_FileWithNonIoName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT036.
            {IoEnv}
            DATA DIVISION.
            FILE SECTION.
            {IoFd}
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE FILE TF.
            EC-H-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0715");

    [Fact]   // §14.9.49.3 SR14: the same (exception-name, file) pair in two USE statements of one procedure
             // division is rejected.
    public void UseF3_DuplicatePairAcrossSections_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT037.
            PROCEDURE DIVISION.
            DECLARATIVES.
            H1 SECTION. USE AFTER EXCEPTION CONDITION EC-USER-D.
            H1-P.
                CONTINUE.
            H2 SECTION. USE AFTER EXCEPTION CONDITION EC-USER-D.
            H2-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0716");

    [Fact]   // §14.9.18.3 SR2 (¶27403): an EC-USER exception-name in GOBACK/EXIT RAISING shall be specified in
             // the RAISING phrase of the procedure division header.
    public void GobackRaising_EcUserNotInPdHeader_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT047.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GOBACK RAISING EXCEPTION EC-USER-UNDECLARED.
            """, 2023), "COBOLNET0717");

    [Fact]   // §7.3.25.3 SR3: a duplicated (exception-name, file-name) combination within one TURN directive.
    public void Turn_DuplicateName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-SIZE EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT038.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0718");

    [Fact]   // §7.3.25.3 SR4: a file-name only with an EC-I-O… exception-name.
    public void Turn_FileWithNonIoName_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-SIZE SOME-FILE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT039.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023), "COBOLNET0719");

    // ── Per-edition gating (the four-compilers rule: 2002+ constructs diagnosed BY NAME at --std 85) ─────────

    [Fact]
    public void Turn_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            >>TURN EC-ALL CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT040.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 85), "COBOLNET0875");

    [Fact]
    public void Raise_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT041.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION EC-USER-A.
                STOP RUN.
            """, 85), "COBOLNET0876");

    [Fact]
    public void UseF3_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT042.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-USER-A.
            EC-H-P.
                CONTINUE.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                STOP RUN.
            """, 85), "COBOLNET0877");

    [Fact]
    public void SetLastException_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT043.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET LAST EXCEPTION TO OFF.
                STOP RUN.
            """, 85), "COBOLNET0879");

    [Fact]
    public void GobackRaising_At85_DiagnosedWithEdition()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT044.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GOBACK RAISING EXCEPTION EC-USER-A.
            """, 85), "COBOLNET0879");

    private const string EcWordsProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ECT045.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 RAISE PIC 9 VALUE 1.
        01 RESUME PIC 9 VALUE 2.
        01 EC PIC 9 VALUE 3.
        01 CONDITION PIC 9 VALUE 4.
        01 STATEMENT PIC 9 VALUE 5.
        01 RAISING PIC 9 VALUE 6.
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY RAISE RESUME EC CONDITION STATEMENT RAISING.
            STOP RUN.
        """;

    /// <summary>The EC words are legal USER-DEFINED words at COBOL-85 only — the edition that predates the
    /// exception model. (The pre-validator posture said "legal at every edition"; the P2.4 mechanical tables
    /// corrected it TWICE: DEVLOG 578 proved them §8.9-reserved at 2023, and the per-standard source lists
    /// prove them reserved since 2002 — they are NOT among the Annex E.2 item-25 words newly reserved in 2023,
    /// so 2023-only was under-inclusive. DEVLOG 585.)</summary>
    [Fact]
    public void EcWords_RemainUserDefinedWords_At85()
    {
        var (ok, diags) = EditionHarness.Compile(EcWordsProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", diags)}");
    }

    /// <summary>At 2002/2014/2023 the EC words (except the context-sensitive STATEMENT) ARE reserved words —
    /// the P2.4 EditionValidator funnel enforces it per edition: strict rejects with COBOLNET0901.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void EcWords_AsUserWords_2002Plus_Rejected0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(EcWordsProgram, edition);
        Assert.False(ok, $"--std {edition} strict must reject reserved EC words used as user-defined words");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'RAISE'");
    }

    // ── The zero-scaffolding invariant (SSOT §18.16 / deep-dive D9–D10) ──────────────────────────────────────

    [Fact]   // An EC-free program's generated source carries NO exception machinery — no ExceptionState, no
             // __EcDispatch, no Runtime.Exceptions using (checking OFF compiles to NOTHING, §7.3.25.4 GR1).
    public void ZeroScaffolding_EcFreeProgram_EmitsNoExceptionMachinery()
    {
        string dir = CutRunner.NewTempDir("ec0");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. ECT046.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 WS-A PIC 9(3) VALUE 5.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    ADD 1 TO WS-A.
                    DISPLAY WS-A.
                    STOP RUN.
                """);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: 2023));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            string generated = File.ReadAllText(Path.Combine(dir, "prog.g.cs"));
            Assert.DoesNotContain("ExceptionState", generated);
            Assert.DoesNotContain("__EcDispatch", generated);
            Assert.DoesNotContain("__IoCheckEc", generated);
            Assert.DoesNotContain("Runtime.Exceptions", generated);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    // ── The §15.33 width collision (kb/Work R05 — Phase-B F6): r1 fixes EXCEPTION-STATUS at 31 characters
    //    while COBOL-2023 words run to 63 (§8.3.2.1) and the §14.6.13.1.1 open-family suffixes are unbounded.
    //    The r1 width is implemented AS WRITTEN; COBOLNET1636 (Warning) makes the collision visible. ──────────

    /// <summary>44 characters — legal at 2023 only (the 31-char COBOL-2002 word limit rejects it below).</summary>
    private const string LongEc = "EC-USER-ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Fact]   // §15.33.3 r1 IS the width: a 44-character legal name reads back as its 31-character prefix —
             // the truncation is the rule's own, not an implementation choice.
    public void ExceptionStatus_LongUserName_ReturnsThe31CharPrefix()
        => AssertSpec($"""
            >>TURN EC-USER CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT048.
            PROCEDURE DIVISION.
            MAIN-PARA.
                RAISE EXCEPTION {LongEc}.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATUS "]".
                STOP RUN.
            """, $"S=[{LongEc[..31]}]");

    [Fact]   // COBOLNET1636 fires ONCE per spelling even when the name appears at TURN + RAISE + USE (the ONE
             // resolution funnel dedupes), and the program still compiles clean — legal source stays legal.
    public void LongUserName_AdvisedOnce_CompilesClean()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("ECT049",
            $">>TURN {LongEc} CHECKING ON", "", "", $"""
            EC-H SECTION. USE AFTER EXCEPTION CONDITION {LongEc}.
            EC-H-P.
                RESUME AT NEXT STATEMENT.
            """, $"""
                RAISE EXCEPTION {LongEc}.
                STOP RUN.
            """), 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Equal(1, warnings.Count(w => w.Contains("COBOLNET1636")));
    }

    [Fact]   // The advisory rides the FUNNEL, not one verb: a name spelled ONLY in >>TURN — never RAISEd —
             // is still advised (the arm a single-site fix would have missed; feedback_two_arm_dispatch).
    public void LongUserName_TurnOnlySpelling_StillAdvised()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
            >>TURN {LongEc} CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT050.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1636"));
    }

    [Fact]   // Below 2023 the collision cannot arise: the 44-character word itself exceeds the COBOL-2002
             // 31-character word limit — INCLUDING in a >>TURN directive, where the word never reaches the
             // tree-walk funnel. ⛔ This fact FAILED when first written: the evidence ledger's "correctly
             // rejected with COBOLNET1567" was measured on the RAISE spelling only, and the directive path
             // compiled the same word clean (CobolWordRule closed that hole — TURN operands now share the
             // §8.3.2.1 ceiling). No advisory either: 1636 is a 2023-collision message, not a length error.
    public void LongUserName_At2002_TheWordItselfIsRejected()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
            >>TURN {LongEc} CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT051.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a 44-character word must not compile at --std 2002");
        Assert.Contains(errors, e => e.Contains("COBOLNET1567"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1636"));
    }

    [Fact]   // §15.32.3 r1 / §15.30.3 r1 are PER-CONDITION (kb/Work R06): one statement checked for EC-SIZE
             // (WITH LOCATION) *and* EC-BOUND-SUBSCRIPT (without). The subscript raise must answer 63 spaces /
             // one space — under the old per-statement bool, the sibling's WITH LOCATION contaminated it and
             // EXCEPTION-STATEMENT returned "DIVIDE". The zero-divide raise keeps its location operands.
    public void WithLocation_IsPerCondition_NotPerStatement()
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun("""
            >>TURN EC-SIZE CHECKING ON WITH LOCATION
            >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT053.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TAB.
               05 WS-CELL PIC 9 OCCURS 3 TIMES VALUE 1.
            01 WS-I PIC 9 VALUE 5.
            01 WS-D PIC 9 VALUE 0.
            01 WS-R PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            DECLARATIVES.
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-ALL.
            EC-H-P.
                DISPLAY "S=[" FUNCTION EXCEPTION-STATEMENT "] L=[" FUNCTION EXCEPTION-LOCATION "]".
                RESUME AT NEXT STATEMENT.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                DIVIDE WS-CELL(WS-I) BY WS-D GIVING WS-R.
                MOVE 2 TO WS-I.
                DIVIDE WS-CELL(WS-I) BY WS-D GIVING WS-R.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        var lines = stdout.Split('\n');
        Assert.Equal(2, lines.Length);
        // Raise 1 — EC-BOUND-SUBSCRIPT, enabled WITHOUT location: r1's answers, uncontaminated.
        Assert.Equal($"S=[{new string(' ', 63)}] L=[ ]", lines[0]);
        // Raise 2 — EC-SIZE-ZERO-DIVIDE, enabled WITH LOCATION: the Table-12 name + the three-part location.
        Assert.StartsWith($"S=[{"DIVIDE",-63}] L=[ECT053; MAIN-PARA OF MAIN;", lines[1]);
    }

    [Fact]   // kb/Work R07 + §15.32.3 r2 / §15.30.3 r2: GOBACK … RAISING under WITH LOCATION records the
             // Table 12 name ("GOBACK") and the three-part location. SetPropagating's Set was TWO-ARG, so both
             // the returning element's status and what the activator reads answered 63 spaces / one space even
             // with LOCATION on — a silent wrong answer (BoundRaising had no location fields at all, unlike
             // its sibling BoundRaise).
    public void GobackRaising_WithLocation_RecordsStatementAndLocation()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunWith("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054.
            PROCEDURE DIVISION.
            MAIN-P.
                CALL "ECT054S".
                DISPLAY "S=[" FUNCTION EXCEPTION-STATEMENT "]".
                DISPLAY "L=[" FUNCTION EXCEPTION-LOCATION "]".
                STOP RUN.
            """, """
            >>TURN EC-USER CHECKING ON WITH LOCATION
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT054S.
            PROCEDURE DIVISION RAISING EC-USER-R07.
            SUB-P.
                GOBACK RAISING EXCEPTION EC-USER-R07.
            """);
        Assert.True(ok, detail + stdout);
        var lines = stdout.Split('\n');
        Assert.Equal($"S=[{"GOBACK",-63}]", lines[0]);
        Assert.StartsWith("L=[ECT054S; SUB-P;", lines[1]);
    }

    [Fact]   // The hole the funnel closed: USE AFTER EC with a LEVEL-2 name of a 2023-only family (EC-MCS) at
             // --std 2002. The old USE-site copy guarded the introduction gate with Level == 3, so the level-2
             // spelling slipped through un-gated; the funnel gates every level (COBOLNET0878).
    public void UseAfterEc_Level2NameOf2023Family_At2002_Diagnosed()
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Prog("ECT052",
            "", "", "", """
            EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-MCS.
            EC-H-P.
                RESUME AT NEXT STATEMENT.
            """, """
                STOP RUN.
            """), 2002), "COBOLNET0878");
}
