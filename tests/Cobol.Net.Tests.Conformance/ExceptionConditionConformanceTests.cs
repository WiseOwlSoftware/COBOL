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

    [Fact]   // §14.9.18 GR + §14.6.13.1.3: a FATAL condition staged by the MAIN program's GOBACK RAISING has the
             // run-unit boundary as its activator — the boundary default terminates abnormally.
    public void GobackRaisingFatal_InMainProgram_TerminatesAtRunUnitBoundary()
        => AssertFatal("""
            >>TURN EC-SIZE CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ECT031.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "BEFORE".
                GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
            """, "EC-SIZE-OVERFLOW", "BEFORE");

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

    /// <summary>The EC words stay legal USER-DEFINED words at every edition (the cobolWord continuity
    /// guarantee — the version-matrix INV-1 invariant for the newly-tokenized RAISE/RESUME/STATEMENT/
    /// CONDITION/EC/RAISING).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void EcWords_RemainUserDefinedWords_AtEveryEdition(int edition)
    {
        var (ok, diags) = EditionHarness.Compile("""
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
            """, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", diags)}");
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
}
