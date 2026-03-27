using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class ControlFlowTests : EndToEndTestBase
{
    [Fact]
    public void IfElse_ThenBranch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-X > 3
                    DISPLAY "BIG"
                ELSE
                    DISPLAY "SMALL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("BIG", stdout);
    }


    [Fact]
    public void PerformParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PERFTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-COUNT PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM ADD-ONE.
                PERFORM ADD-ONE.
                PERFORM ADD-ONE.
                DISPLAY WS-COUNT.
                STOP RUN.
            ADD-ONE.
                ADD 1 TO WS-COUNT.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("003", stdout);
    }


    [Fact]
    public void PerformThru_ExecutesParagraphRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRUTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM STEP-A THRU STEP-C.
                DISPLAY WS-RESULT.
                STOP RUN.
            STEP-A.
                MOVE "ABC" TO WS-RESULT.
            STEP-B.
                DISPLAY "In B".
            STEP-C.
                DISPLAY "In C".
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("In B", lines[0]);
        Assert.Equal("In C", lines[1]);
        Assert.Equal("ABC", lines[2]);
    }


    [Fact]
    public void PerformThru_StopsAtTarget()
    {
        // C must NOT execute
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSTOP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 1 TO R.
            B.
                ADD 2 TO R.
            C.
                ADD 4 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("03", stdout);
    }


    [Fact]
    public void PerformThru_NestedThru()
    {
        // Two separate PERFORM THRU ranges
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRNEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                PERFORM C THRU D.
                DISPLAY R.
                STOP RUN.
            A. ADD 1 TO R.
            B. ADD 2 TO R.
            C. ADD 3 TO R.
            D. ADD 4 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("10", stdout);
    }


    [Fact]
    public void PerformThru_MultipleSentences()
    {
        // Paragraph A has two sentences
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSENT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 1 TO R.
                ADD 1 TO R.
            B.
                ADD 2 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("04", stdout);
    }


    [Fact]
    public void PerformThru_SingleParagraph()
    {
        // PERFORM A THRU A is the same as PERFORM A
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSING.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU A.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 5 TO R.
            B.
                ADD 9 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("05", stdout);
    }


    [Fact]
    public void EvaluateStatement_SelectsCorrectBranch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EVALTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CODE PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE WS-CODE
                    WHEN 1
                        DISPLAY "One"
                    WHEN 2
                        DISPLAY "Two"
                    WHEN OTHER
                        DISPLAY "Other"
                END-EVALUATE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Two", stdout);
    }


    [Fact]
    public void Evaluate_SingleSubjectWithRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A
                    WHEN 1
                        MOVE 1 TO RESULT
                    WHEN 2 THRU 4
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("2", stdout);
    }


    [Fact]
    public void Evaluate_MultiSubjectAlsoExactMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            01 B PIC 9 VALUE 5.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 3 ALSO 5
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1", stdout);
    }


    [Fact]
    public void Evaluate_MultiSubjectAlsoWithRanges()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 4.
            01 B PIC 9 VALUE 7.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 1 ALSO 5 THRU 6
                        MOVE 1 TO RESULT
                    WHEN 4 THRU 6 ALSO 7
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("2", stdout);
    }


    [Fact]
    public void Evaluate_PartialMatchMustFail()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 2.
            01 B PIC 9 VALUE 9.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 2 ALSO 5
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }


    [Fact]
    public void Evaluate_MismatchedAlsoArity_FallsToOther()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 2.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 1
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }


    [Fact]
    public void EvaluateTrue_ConditionMatching()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EVTRUE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(3) VALUE 15.
            01 WS-B PIC 9(3) VALUE 10.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE TRUE
                    WHEN WS-A > WS-B
                        MOVE 1 TO RESULT
                    WHEN WS-A = WS-B
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 3 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1", stdout);
    }

    // ═══════════════════════════════════════════
    // PERFORM VARYING / AFTER tests
    // ═══════════════════════════════════════════


    [Fact]
    public void ExitPerform_LeavesLoopEarly()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9.
            01 WS-SUM PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO WS-SUM.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 10
                    ADD I TO WS-SUM
                    IF I = 4
                        EXIT PERFORM
                    END-IF
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 1+2+3+4 = 10
        Assert.Equal("0010", stdout);
    }


    [Fact]
    public void ExitPerform_ExitsOnlyInnermostPerform()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITP2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9.
            01 J PIC 9.
            01 WS-COUNT PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO WS-COUNT.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                    PERFORM VARYING J FROM 1 BY 1 UNTIL J > 5
                        ADD 1 TO WS-COUNT
                        IF J = 2
                            EXIT PERFORM
                        END-IF
                    END-PERFORM
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // For each I (1,2,3): J runs 1,2 → EXIT → 2 iterations each = 6
        Assert.Equal("0006", stdout);
    }


    [Fact]
    public void ExitParagraph_SkipsRemainingStatements()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM TEST-PARA.
                DISPLAY WS-TRACE.
                STOP RUN.
            TEST-PARA.
                MOVE "A" TO WS-TRACE(1:1)
                EXIT PARAGRAPH
                MOVE "B" TO WS-TRACE(2:1).
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT PARAGRAPH skips MOVE "B", so only "A" followed by spaces
        Assert.Equal("A", stdout.TrimEnd());
    }


    [Fact]
    public void ExitParagraph_InsidePerformVarying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9(2) VALUE ZERO.
            01 WS-COUNT PIC 9(2) VALUE ZERO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM COUNT-PARA
                    VARYING I FROM 1 BY 1 UNTIL I > 5.
                DISPLAY WS-COUNT.
                STOP RUN.
            COUNT-PARA.
                ADD 1 TO WS-COUNT
                EXIT PARAGRAPH
                ADD 10 TO WS-COUNT.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT PARAGRAPH skips the ADD 10, so count = 5 (not 55)
        Assert.Equal("05", stdout);
    }


    [Fact]
    public void ExitSection_SkipsRemainingParagraphs()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITSEC1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-PARA.
                PERFORM TEST-SEC
                DISPLAY WS-TRACE
                STOP RUN.
            TEST-SEC SECTION.
            PARA-1.
                MOVE "A" TO WS-TRACE(1:1)
                EXIT SECTION
                MOVE "X" TO WS-TRACE(2:1).
            PARA-2.
                MOVE "B" TO WS-TRACE(3:1).
            AFTER-SEC SECTION.
            AFTER-PARA.
                CONTINUE.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT SECTION in PARA-1 skips rest of PARA-1 and all of PARA-2
        Assert.Equal("A", stdout.TrimEnd());
    }


    [Fact]
    public void ExitSection_PerformSectionStopsAtBoundary()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITSEC2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-PARA.
                PERFORM SEC-A
                DISPLAY WS-TRACE
                STOP RUN.
            SEC-A SECTION.
            PARA-A1.
                MOVE "1" TO WS-TRACE(1:1).
            PARA-A2.
                MOVE "2" TO WS-TRACE(2:1).
            SEC-B SECTION.
            PARA-B1.
                MOVE "3" TO WS-TRACE(3:1).
            """);

        Assert.True(success, $"Failed: {stderr}");
        // PERFORM SEC-A runs PARA-A1 and PARA-A2 only, not PARA-B1
        Assert.Equal("12", stdout.TrimEnd());
    }


    [Fact]
    public void ExitParagraph_InsideNestedPerform()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X VALUE SPACE.
            01 WS-B PIC X VALUE SPACE.
            01 WS-C PIC X VALUE SPACE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM OUTER-PARA
                DISPLAY WS-A WS-B WS-C
                STOP RUN.
            OUTER-PARA.
                MOVE "O" TO WS-A
                PERFORM INNER-PARA
                MOVE "Z" TO WS-C.
            INNER-PARA.
                MOVE "I" TO WS-B
                EXIT PARAGRAPH
                MOVE "X" TO WS-C.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // INNER-PARA: sets B=I, exits, skips X. OUTER continues: sets C=Z.
        Assert.Equal("OIZ", stdout);
    }


    [Fact]
    public void PerformVarying_SumOneToFive()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-I PIC 9(3) VALUE 0.
            01 WS-SUM PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM ADD-PARA VARYING WS-I FROM 1 BY 1
                    UNTIL WS-I > 5.
                DISPLAY WS-SUM.
                STOP RUN.
            ADD-PARA.
                ADD WS-I TO WS-SUM.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00015", stdout);
    }


    [Fact]
    public void PerformUntil_DecrementToZero()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PU1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9(3) VALUE 10.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM DEC-PARA UNTIL WS-N = 0.
                DISPLAY "Done".
                STOP RUN.
            DEC-PARA.
                SUBTRACT 1 FROM WS-N.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Done", stdout);
    }


    [Fact]
    public void PerformVaryingInline_SumOneToFive()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PVI1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-I PIC 9(3) VALUE 0.
            01 WS-SUM PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING WS-I FROM 1 BY 1
                    UNTIL WS-I > 5
                    ADD WS-I TO WS-SUM
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00015", stdout);
    }


    [Fact]
    public void PerformVaryingAfter_InnerUntilTrueImmediately()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 0
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0000", stdout);
    }


    [Fact]
    public void PerformVaryingAfter_Classic2D()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                    AFTER J FROM 1 BY 1 UNTIL J > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0006", stdout);
    }


    [Fact]
    public void PerformVaryingAfter_OuterDependsOnInner()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL WS-COUNT > 3
                    AFTER J FROM 1 BY 1 UNTIL J > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0004", stdout);
    }


    [Fact]
    public void PerformVaryingAfter_ThreeLevel()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 K PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 3
                    AFTER K FROM 1 BY 1 UNTIL K > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0012", stdout);
    }


    [Fact]
    public void EvaluateInsideVarying_MixedBranching()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 WS-SUM PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
                    EVALUATE I
                        WHEN 1
                            ADD 1 TO WS-SUM
                        WHEN 2
                            ADD 1 TO WS-SUM
                        WHEN 3 THRU 4
                            ADD 2 TO WS-SUM
                        WHEN OTHER
                            ADD 3 TO WS-SUM
                    END-EVALUATE
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0009", stdout);
    }


    [Fact]
    public void EvaluateAlsoInsideNestedVarying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EV2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 COUNT1 PIC 9(4) VALUE 0.
            01 COUNT2 PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 3
                    EVALUATE I ALSO J
                        WHEN 1 ALSO 1
                            ADD 1 TO COUNT1
                        WHEN 1 ALSO 2
                            ADD 1 TO COUNT1
                        WHEN 2 ALSO 3
                            ADD 1 TO COUNT2
                        WHEN OTHER
                            CONTINUE
                    END-EVALUATE
                END-PERFORM.
                DISPLAY COUNT1 COUNT2.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00020001", stdout);
    }

    // ═══════════════════════════════════════════
    // SIGN clause tests
    // ═══════════════════════════════════════════


    [Fact]
    public void NextSentence_SkipsRestOfSentence()
    {
        // NEXT SENTENCE jumps to the first statement after the next period.
        // Sentence 1: IF ... NEXT SENTENCE END-IF MOVE "BAD" TO RESULT.
        // Sentence 2: MOVE "GOOD" TO RESULT.  (NEXT SENTENCE lands here)
        // Sentence 3: DISPLAY RESULT STOP RUN.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC 9 VALUE 1.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                IF FLAG = 1
                    NEXT SENTENCE
                END-IF
                MOVE "BAD" TO RESULT.
                MOVE "GOOD" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("GOOD", stdout);
    }


    [Fact]
    public void NextSentence_SkipsMultipleStatements()
    {
        // Sentence 1: NEXT SENTENCE MOVE "BAD1" MOVE "BAD2".
        // Sentence 2 (in SENT-2): MOVE "GOOD" ... DISPLAY ... STOP.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                NEXT SENTENCE
                MOVE "BAD1" TO RESULT
                MOVE "BAD2" TO RESULT.
            SENT-2.
                MOVE "GOOD" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("GOOD", stdout);
    }


    [Fact]
    public void NextSentence_NestedIf()
    {
        // NEXT SENTENCE exits both IFs and jumps past the period.
        // Sentence 1: IF A=1 IF B=1 NEXT SENTENCE END-IF END-IF MOVE "BAD".
        // Sentence 2: MOVE "OK" ... DISPLAY ... STOP.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 1.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                IF A = 1
                    IF B = 1
                        NEXT SENTENCE
                    END-IF
                END-IF
                MOVE "BAD" TO RESULT.
                MOVE "OK" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("OK", stdout);
    }

    // ── ABBREVIATED RELATIONS ──


    [Fact]
    public void GoToDepending_SelectsCorrectTarget()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 P3 DEPENDING ON IDX.
                DISPLAY "FALLTHROUGH".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
                STOP RUN.
            P3.
                DISPLAY "THREE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("TWO", stdout);
    }


    [Fact]
    public void GoToDepending_OutOfRange_FallsThrough()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 DEPENDING ON IDX.
                DISPLAY "DONE".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DONE", stdout);
    }

    // ── IrLocation regression: subscripts and ref-mod in expressions ──
    // These tests lock in the invariant that EmitExpression goes through IrLocation
    // for all data references, not directly to GetStorageLocation.


    [Fact]
    public void If_SubscriptedIdentifier_ComparisonWorks()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ELEM PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO I.
                MOVE 7 TO ELEM(I).
                IF ELEM(I) = 7
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }


    [Fact]
    public void If_RefMod_ComparisonWorks()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFREFMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDE" TO FIELD.
                IF FIELD(2:3) = "BCD"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }


    [Fact]
    public void If_SubscriptedRefMod_ComparisonWorks()
    {
        // Subscripts + ref-mod combined in a comparison
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUBREF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2.
                  10 COL OCCURS 2 PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO COL(2, 1).
                IF COL(2, 1)(3:2) = "CD"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }


    [Fact]
    public void If_VariableRefMod_ComparisonWorks()
    {
        // Variable start/length in ref-mod comparison
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFVARREF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                MOVE 3 TO I.
                MOVE 4 TO J.
                IF FIELD(I:J) = "CDEF"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }


    [Fact]
    public void GoToDepending_FallsIntoNextParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 P3 DEPENDING ON IDX.
                DISPLAY "AFTER-GOTO".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
            P3.
                DISPLAY "THREE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("TWO", lines[0]);
        Assert.Equal("THREE", lines[1]);
    }

    // ── SEARCH (linear) ──


    [Fact]
    public void PerformSection_ExecutesAllParagraphsThenReturns()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SECPERF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM SEC-1.
                DISPLAY "X".
                STOP RUN.
            SEC-1 SECTION.
            P1.
                DISPLAY "A".
            P2.
                DISPLAY "B".
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A", lines[0]);
        Assert.Equal("B", lines[1]);
        Assert.Equal("X", lines[2]);
    }


    [Fact]
    public void GoToSection_JumpsToFirstParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SECGOTO.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO SEC-1.
                DISPLAY "SKIP".
                STOP RUN.
            SEC-1 SECTION.
            P1.
                DISPLAY "A".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("A", stdout);
    }

    // ── IF THEN (optional THEN keyword) ──


    [Fact]
    public void If_Then_OptionalKeyword()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN
                    DISPLAY "YES"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("YES", stdout);
    }


    [Fact]
    public void If_Then_WithElse()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN
                    DISPLAY "YES"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NO", stdout);
    }


    [Fact]
    public void If_Then_NextSentence()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN NEXT SENTENCE.
                DISPLAY "AFTER".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("AFTER", stdout);
    }

    // ═══════════════════════════════════════════
    // Arithmetic Conformance Matrix
    // Tests every allowed cell: statement × operand position × operand kind
    // Ensures GIVING forms accept literals per COBOL-85 spec.
    // ═══════════════════════════════════════════


    [Fact]
    public void Alter_RedirectsGoTo()
    {
        // ALTER changes a GO TO target at runtime.
        // Without ALTER, GOTO-PARA goes to OLD-TARGET which displays "OLD".
        // With ALTER, GOTO-PARA goes to NEW-TARGET which displays "NEW".
        // Both paths end with STOP RUN.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTERTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ALTER GOTO-PARA TO PROCEED TO NEW-TARGET.
                GO TO GOTO-PARA.
            GOTO-PARA.
                GO TO OLD-TARGET.
            OLD-TARGET.
                DISPLAY "OLD PATH"
                STOP RUN.
            NEW-TARGET.
                DISPLAY "NEW PATH"
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NEW PATH", stdout);
    }


    [Fact]
    public void Alter_WithoutProceedTo()
    {
        // ALTER without PROCEED TO (optional per spec)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTSHORT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ALTER GOTO-PARA TO NEW-DEST.
                GO TO GOTO-PARA.
            GOTO-PARA.
                GO TO OLD-DEST.
            OLD-DEST.
                DISPLAY "OLD"
                STOP RUN.
            NEW-DEST.
                DISPLAY "NEW"
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NEW", stdout);
    }

}
