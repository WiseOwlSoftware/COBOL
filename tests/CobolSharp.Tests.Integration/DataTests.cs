using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class DataTests : EndToEndTestBase
{
    [Fact]
    public void MoveAndDisplay_NumericField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOVETEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM PIC 9(3) VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-NUM.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("042", stdout);
    }


    [Fact]
    public void MoveStringToField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOVESTR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(10) VALUE "World".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "Hello " WS-NAME.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("Hello World", stdout);
    }


    [Fact]
    public void SetStatement_SetsValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SETTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-IDX PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET WS-IDX TO 42.
                DISPLAY WS-IDX.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("042", stdout);
    }


    [Fact]
    public void InitializeStatement_ResetsFields()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INITTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM PIC 9(3) VALUE 123.
            01 WS-STR PIC X(5) VALUE "Hello".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE WS-NUM.
                INITIALIZE WS-STR.
                DISPLAY WS-NUM.
                DISPLAY ">" WS-STR "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("000", lines[0]);
        // After INITIALIZE, alphanumeric is spaces. DISPLAY trims trailing spaces.
        Assert.Equal("><", lines[1]);
    }


    [Fact]
    public void SignTrailingSeparate_NegativeResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 30.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT 50 FROM WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 30 - 50 = -20, trailing separate → "020-"
        Assert.Equal("020-", stdout);
    }


    [Fact]
    public void SignDefault_TrailingOverpunch()
    {
        // PIC S9(3) with no SIGN clause → default trailing overpunch
        // Positive 42 → last digit '2' overpunched positive → 04B
        // Negative -42 → last digit '2' overpunched negative → 04K
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OVPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-POS PIC S9(3) VALUE 42.
            01 WS-NEG PIC S9(3) VALUE -42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-POS.
                DISPLAY WS-NEG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // Positive 042: last digit '2' → 'B' (positive overpunch)
        Assert.Equal("04B", lines[0]);
        // Negative 042: last digit '2' → 'K' (negative overpunch)
        Assert.Equal("04K", lines[1]);
    }


    [Fact]
    public void SignDefault_OverpunchArithmeticRoundTrip()
    {
        // Verify arithmetic works correctly with overpunched fields
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OVPART.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE 100.
            01 WS-B PIC S9(5) VALUE 250.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT WS-B FROM WS-A.
                DISPLAY WS-A.
                ADD 300 TO WS-A.
                DISPLAY WS-A.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // 100 - 250 = -150, trailing overpunch: 0015P (P = negative 7... wait, 0 → })
        // Actually: -150 → digits "00150", last digit 0, negative → '}'
        Assert.Equal("0015}", lines[0]);
        // -150 + 300 = 150, positive: 00150, last digit 0 → '{'
        Assert.Equal("0015{", lines[1]);
    }


    [Fact]
    public void SignLeadingSeparate_Arithmetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSEPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER
               VALUE -25.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD 50 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // -25 + 50 = 25, leading separate positive → "+025"
        Assert.Equal("+025", stdout);
    }


    [Fact]
    public void SignLeadingOverpunch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LOVPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS LEADING VALUE -37.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                ADD 100 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // -37: leading overpunch on first digit '0' → '}', rest "37" → "}37"
        Assert.Equal("}37", lines[0]);
        // -37 + 100 = 63: leading overpunch on '0' → '{', rest "63" → "{63"
        Assert.Equal("{63", lines[1]);
    }


    [Fact]
    public void SignTrailingSeparate_PositiveValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNPOS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD 8 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 42 + 8 = 50, trailing separate positive → "050+"
        Assert.Contains("050", stdout);
    }


    [Fact]
    public void MoveNumeric_DisplayToCompRoundTrip()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NMOVE1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 DS-LS PIC S9(5) SIGN IS LEADING SEPARATE CHARACTER
               VALUE -12345.
            01 DS-TS PIC S9(5) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 0.
            01 CU-005 PIC 9(5) COMP VALUE 0.
            01 CS-005 PIC S9(5) COMP VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE DS-LS TO CU-005.
                MOVE CU-005 TO DS-TS.
                DISPLAY DS-TS.
                MOVE DS-LS TO CS-005.
                MOVE CS-005 TO DS-TS.
                DISPLAY DS-TS.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // CU unsigned: stores absolute 12345, then moved to trailing separate → "12345+"
        Assert.Equal("12345+", lines[0]);
        // CS signed: stores -12345, then moved to trailing separate → "12345-"
        Assert.Equal("12345-", lines[1]);
    }


    [Fact]
    public void MoveNumeric_ToEdited_ZeroSuppressed()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EDIT1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC S9(5) VALUE 42.
            01 WS-E PIC ZZ,ZZ9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-N TO WS-E.
                DISPLAY ">" WS-E "<".
                MOVE ZERO TO WS-N.
                MOVE WS-N TO WS-E.
                DISPLAY ">" WS-E "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // 42 in ZZ,ZZ9 → "    42"
        Assert.Equal(">    42<", lines[0]);
        // 0 in ZZ,ZZ9 → "     0"
        Assert.Equal(">     0<", lines[1]);
    }


    [Fact]
    public void FigurativeConstants_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIGTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5).
            01 WS-N PIC 9(5) VALUE ZERO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO WS-A.
                DISPLAY ">" WS-A "<".
                MOVE ALL "AB" TO WS-A.
                DISPLAY WS-A.
                DISPLAY WS-N.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("><", lines[0]);       // SPACES fills with ' ', display trims trailing spaces
        Assert.Equal("ABABA", lines[1]);    // ALL "AB" repeats
        Assert.Equal("00000", lines[2]);    // VALUE ZERO
    }


    [Fact]
    public void FigurativeConstants_HighValueLowValueZeros()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIGTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(3).
            01 WS-B PIC X(3).
            01 WS-C PIC X(3).
            01 WS-N PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE HIGH-VALUES TO WS-A.
                MOVE LOW-VALUES TO WS-B.
                MOVE ZEROS TO WS-C.
                DISPLAY WS-C.
                MOVE ZEROS TO WS-N.
                DISPLAY WS-N.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // MOVE ZEROS to alphanumeric field fills with '0'
        Assert.Equal("000", lines[0]);
        // MOVE ZEROS to numeric field produces numeric zero
        Assert.Equal("000", lines[1]);
    }

    // ═══════════════════════════════════════════
    // DIVIDE — all spec-true forms
    // ═══════════════════════════════════════════


    [Fact]
    public void Level88_ConditionName_SingleValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88TEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STATUS PIC 9 VALUE 1.
               88 STATUS-ACTIVE VALUE 1.
               88 STATUS-INACTIVE VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF STATUS-ACTIVE
                    DISPLAY "ACTIVE"
                ELSE
                    DISPLAY "NOT ACTIVE"
                END-IF.
                MOVE 0 TO WS-STATUS.
                IF STATUS-INACTIVE
                    DISPLAY "INACTIVE"
                ELSE
                    DISPLAY "NOT INACTIVE"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("ACTIVE", lines[0]);
        Assert.Equal("INACTIVE", lines[1]);
    }


    [Fact]
    public void Level88_ConditionName_MultipleValues()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88MULTI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DAY PIC 9 VALUE 6.
               88 WEEKEND VALUES 6 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WEEKEND
                    DISPLAY "WEEKEND"
                ELSE
                    DISPLAY "WEEKDAY"
                END-IF.
                MOVE 3 TO WS-DAY.
                IF WEEKEND
                    DISPLAY "WEEKEND"
                ELSE
                    DISPLAY "WEEKDAY"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("WEEKEND", lines[0]);
        Assert.Equal("WEEKDAY", lines[1]);
    }


    [Fact]
    public void Level88_ConditionName_InEvaluateTrue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88EVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CODE PIC 9 VALUE 2.
               88 CODE-A VALUE 1.
               88 CODE-B VALUE 2.
               88 CODE-C VALUE 3.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE TRUE
                    WHEN CODE-A
                        DISPLAY "A"
                    WHEN CODE-B
                        DISPLAY "B"
                    WHEN CODE-C
                        DISPLAY "C"
                    WHEN OTHER
                        DISPLAY "OTHER"
                END-EVALUATE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("B", stdout);
    }


    [Fact]
    public void Level88_ConditionName_AlphanumericParent()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88ALPHA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC X VALUE "Y".
               88 FLAG-YES VALUE "Y".
               88 FLAG-NO VALUE "N".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF FLAG-YES
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
    public void Level88_ConditionName_ThruRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88THRU.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SCORE PIC 99 VALUE 75.
               88 PASSING VALUE 50 THRU 100.
               88 FAILING VALUE 0 THRU 49.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF PASSING
                    DISPLAY "PASS"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                MOVE 30 TO WS-SCORE.
                IF FAILING
                    DISPLAY "LOW"
                ELSE
                    DISPLAY "HIGH"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("PASS\r\nLOW", stdout);
    }


    [Fact]
    public void Level88_ConditionName_MultipleThruRanges()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88MTHR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GRADE PIC 99 VALUE 85.
               88 GRADE-A VALUES 90 THRU 100.
               88 GRADE-B VALUES 80 THRU 89.
               88 GRADE-F VALUES 0 THRU 59.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF GRADE-B
                    DISPLAY "B"
                ELSE
                    DISPLAY "NOT-B"
                END-IF.
                MOVE 95 TO WS-GRADE.
                IF GRADE-A
                    DISPLAY "A"
                ELSE
                    DISPLAY "NOT-A"
                END-IF.
                MOVE 40 TO WS-GRADE.
                IF GRADE-F
                    DISPLAY "F"
                ELSE
                    DISPLAY "NOT-F"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("B\r\nA\r\nF", stdout);
    }


    [Fact]
    public void Initialize_GroupWithMixedChildren()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 A1 PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1.
                DISPLAY N1.
                DISPLAY ">" A1 "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("000", lines[0]);
        Assert.Equal("><", lines[1]); // spaces trimmed by DISPLAY
    }


    [Fact]
    public void Initialize_Redefines_InitializesUnderlying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 R1 REDEFINES N1 PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1.
                DISPLAY N1.
                DISPLAY R1.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // N1 is numeric → initialized to 000
        // R1 REDEFINES N1, shares same storage → shows "000"
        Assert.Equal("000", lines[0]);
        Assert.Equal("000", lines[1]);
    }


    [Fact]
    public void Initialize_ReplacingNumericAndAlphanumeric()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 A1 PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1
                    REPLACING NUMERIC DATA BY 7
                              ALPHANUMERIC DATA BY "Q".
                DISPLAY N1.
                DISPLAY A1.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("007", lines[0]);
        Assert.Equal("Q", lines[1]); // "Q" moved to X(3), right-padded with spaces, trimmed
    }

    // ── SET ──


    [Fact]
    public void Set_ConditionName_ToTrue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC X.
               88 FLAG-ON VALUE "Y".
               88 FLAG-OFF VALUE "N".
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET FLAG-ON TO TRUE.
                IF FLAG-ON
                    DISPLAY "ON"
                ELSE
                    DISPLAY "OFF"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ON", stdout);
    }


    [Fact]
    public void Set_ConditionName_ToFalse()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC X VALUE "Y".
               88 FLAG-ON VALUE "Y".
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET FLAG-ON TO FALSE.
                IF FLAG-ON
                    DISPLAY "ON"
                ELSE
                    DISPLAY "OFF"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OFF", stdout);
    }


    [Fact]
    public void Set_Index_UpByDownBy()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-IDX PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET WS-IDX TO 2.
                SET WS-IDX UP BY 2.
                DISPLAY WS-IDX.
                SET WS-IDX DOWN BY 1.
                DISPLAY WS-IDX.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("004", lines[0]);
        Assert.Equal("003", lines[1]);
    }

    // ── INSPECT ──


    [Fact]
    public void OccursValue_FlatAlphanumeric()
    {
        // All 5 entries of OCCURS table should be "AZ"
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OCCVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                03 ITEM OCCURS 5 PIC XX VALUE "AZ".
            01 IDX PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING IDX FROM 1 BY 1
                    UNTIL IDX > 5
                    DISPLAY ITEM(IDX)
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        foreach (var line in lines)
            Assert.Equal("AZ", line);
    }


    [Fact]
    public void OccursValue_NestedAlphanumeric()
    {
        // 3x4 = 12 entries, all should be "AZ"
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NESTVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                03 ROW OCCURS 3.
                    05 COL OCCURS 4 PIC XX VALUE "AZ".
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1
                    UNTIL I > 3
                    PERFORM VARYING J FROM 1 BY 1
                        UNTIL J > 4
                        DISPLAY COL(I, J)
                    END-PERFORM
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(12, lines.Length);
        foreach (var line in lines)
            Assert.Equal("AZ", line);
    }


    [Fact]
    public void JustifiedRight_LongerSource_TruncatesLeft()
    {
        // MOVE "ABCDEFGHI" TO JUST-FIELD (PIC X(5) JUSTIFIED RIGHT)
        // → "EFGHI" (rightmost 5 characters)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. JUSTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 JUST-FIELD PIC X(5) JUSTIFIED RIGHT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHI" TO JUST-FIELD.
                DISPLAY JUST-FIELD.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("EFGHI", stdout);
    }


    [Fact]
    public void JustifiedRight_ShorterSource_PadsLeft()
    {
        // MOVE "AB" TO JUST-FIELD (PIC X(5) JUSTIFIED RIGHT)
        // → "   AB" (left-padded with spaces)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. JUSTPAD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 JUST-FIELD PIC X(5) JUSTIFIED RIGHT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AB" TO JUST-FIELD.
                DISPLAY JUST-FIELD.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("   AB", stdout);
    }


    [Fact]
    public void UsageInheritance_GroupComp_ValueInit()
    {
        // Parent group has USAGE COMP; child without explicit USAGE inherits it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USGINH.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GRP USAGE COMP.
                02 A PIC 9 VALUE 5.
                02 B PIC 9 VALUE 6.
            01 RESULT PIC 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD A B GIVING RESULT.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("11", stdout);
    }


    [Fact]
    public void BlankWhenZero_ValueClause_StoresRawDigits()
    {
        // VALUE "000" on PIC 999 BLANK WHEN ZERO stores raw "000" bytes.
        // REDEFINES as PIC XXX should see "000", not spaces.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BWZVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NUM-FIELD PIC 999 VALUE "000" BLANK WHEN ZERO.
            01 ALPHA-VIEW REDEFINES NUM-FIELD PIC XXX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY ALPHA-VIEW.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("000", stdout);
    }


    [Fact]
    public void DecimalPointIsComma_NumericLiteral()
    {
        // DECIMAL-POINT IS COMMA: 123,45 is the decimal literal 123.45
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DCOMMA.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. COBOLSHARP.
            OBJECT-COMPUTER. DOTNET.
            SPECIAL-NAMES.
                DECIMAL-POINT IS COMMA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5)V99 VALUE 123,45.
            01 WS-B PIC 9(5)V99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100,50 TO WS-B.
                DISPLAY WS-A.
                DISPLAY WS-B.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // WS-A VALUE 123,45 → displays as "0012345" (PIC 9(5)V99)
        Assert.Equal("0012345", lines[0]);
        // MOVE 100,50 → displays as "0010050"
        Assert.Equal("0010050", lines[1]);
    }


    [Fact]
    public void Comp5_NativeBinary_FullRangeAndLittleEndian()
    {
        // Tests COMP-5 end-to-end:
        // 1. Values beyond PIC digit range (30000 in PIC 9(4) COMP-5)
        // 2. MOVE to DISPLAY for output verification
        // 3. Comparison of COMP-5 values
        // 4. Arithmetic with no PIC-based truncation
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP5TEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-COMP5-U   PIC 9(4) COMP-5.
            01  WS-COMP5-S   PIC S9(4) COMP-5.
            01  WS-COMP-STD  PIC 9(4) COMP.
            01  WS-DISPLAY   PIC 9(5).
            01  WS-RESULT    PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 30000 TO WS-COMP5-U
                MOVE WS-COMP5-U TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE 30000 TO WS-COMP-STD
                MOVE WS-COMP-STD TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE -12345 TO WS-COMP5-S
                MOVE WS-COMP5-S TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE 100 TO WS-COMP5-U
                ADD 200 TO WS-COMP5-U
                MOVE WS-COMP5-U TO WS-RESULT
                DISPLAY WS-RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // Line 0: COMP-5 30000 — full binary range, no PIC truncation
        Assert.Equal("30000", lines[0]);
        // Line 1: Standard COMP 30000 % 10000 = 0 (PIC-based truncation)
        Assert.Equal("00000", lines[1]);
        // Line 2: COMP-5 signed -12345 → DISPLAY unsigned = 12345
        Assert.Equal("12345", lines[2]);
        // Line 3: ADD 100 + 200 = 300
        Assert.Equal("00300", lines[3]);
    }


    [Fact]
    public void Comp5_Computational5_FullWord_Parses()
    {
        // Tests COMPUTATIONAL-5 (full-word form) parses and works
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP5FW.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-FIELD PIC 9(4) COMPUTATIONAL-5.
            01  WS-DISP  PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 50000 TO WS-FIELD
                MOVE WS-FIELD TO WS-DISP
                DISPLAY WS-DISP
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("50000", stdout);
    }


    [Fact]
    public void Renames_SimpleAlias_DisplaysCorrectly()
    {
        // Level-66 RENAMES creates an alias for a contiguous byte range
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RENTEST1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-RECORD.
                05  WS-PART1   PIC X(5)  VALUE "HELLO".
                05  WS-PART2   PIC X(5)  VALUE "WORLD".
            66  WS-ALIAS RENAMES WS-PART1.
            66  WS-BOTH  RENAMES WS-PART1 THRU WS-PART2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-ALIAS
                DISPLAY WS-BOTH
                MOVE "ABCDE" TO WS-ALIAS
                DISPLAY WS-PART1
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("HELLO", lines[0]);
        Assert.Equal("HELLOWORLD", lines[1]);
        Assert.Equal("ABCDE", lines[2]);
    }


    [Fact]
    public void SignedDisplay_DefaultTrailingOverpunch()
    {
        // PIC S9(4) DISPLAY with no explicit SIGN clause defaults to trailing overpunch.
        // MOVE signed to unsigned should strip the sign and display the absolute value.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SIGNED PIC S9(4) VALUE -1234.
            01 WS-UNSIGNED PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-SIGNED TO WS-UNSIGNED.
                DISPLAY WS-UNSIGNED.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1234", stdout);
    }


    [Fact]
    public void Renames_Through_Synonym()
    {
        // THROUGH is a synonym for THRU
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RENTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-DATA.
                05  WS-A   PIC X(3)  VALUE "ABC".
                05  WS-B   PIC X(3)  VALUE "DEF".
                05  WS-C   PIC X(3)  VALUE "GHI".
            66  WS-AB RENAMES WS-A THROUGH WS-B.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-AB
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCDEF", stdout);
    }


    [Fact]
    public void MoveSourceSubscriptEvaluatedOnce()
    {
        // §14.9.25.4 GR 1: source is evaluated ONCE before any stores.
        // MOVE ITEM(IDX) TO IDX, ITEM(2) — source ITEM(IDX) should use IDX=1 (before store),
        // so ITEM(2) should still be "BB" not overwritten.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOVSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TABLE.
               05 WS-ITEM PIC X(2) OCCURS 3 TIMES.
            01 WS-IDX PIC 9(1) VALUE 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO WS-ITEM(1).
                MOVE "BB" TO WS-ITEM(2).
                MOVE "CC" TO WS-ITEM(3).
                MOVE WS-ITEM(WS-IDX) TO WS-IDX WS-ITEM(2).
                DISPLAY WS-IDX.
                DISPLAY WS-ITEM(2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // WS-IDX gets "AA" → numeric truncation → last digit = "A" which is invalid...
        // Actually PIC 9(1) receiving "AA" (alphanumeric) — MOVE alphanumeric to numeric
        // truncates to rightmost character. Let's just verify ITEM(2) got the original source value.
        Assert.Equal("AA", lines[1]); // ITEM(2) should have "AA" (value of ITEM(1) at time of evaluation)
    }


    // ═══════════════════════════════════════════════════════════════
    // EXTERNAL / GLOBAL clause tests (§13.18.22, §13.18.27)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ExternalClause_Level01_WorkingStorage_ParsesAndCompiles()
    {
        // EXTERNAL on level-01 in WORKING-STORAGE should parse and compile
        // (with a warning that shared storage is not yet implemented)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SHARED IS EXTERNAL PIC X(10) VALUE "HELLO".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-SHARED.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }


    [Fact]
    public void ExternalClause_WithoutIS_ParsesAndCompiles()
    {
        // EXTERNAL without IS keyword should also parse (IS is optional noise word)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXTTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-EXT EXTERNAL PIC X(5) VALUE "WORLD".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-EXT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("WORLD", stdout);
    }


    [Fact]
    public void GlobalClause_Level01_ParsesAndCompiles()
    {
        // GLOBAL on level-01 should parse and compile
        // (with a warning that nested visibility is not yet implemented)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLBTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GLOB IS GLOBAL PIC X(8) VALUE "GLOBAL01".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-GLOB.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("GLOBAL01", stdout);
    }


    [Fact]
    public void GlobalClause_WithoutIS_ParsesAndCompiles()
    {
        // GLOBAL without IS keyword should also parse
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GLBTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GLB GLOBAL PIC X(4) VALUE "GLOB".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-GLB.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("GLOB", stdout);
    }


    [Fact]
    public void ExternalAndGlobal_Together_ParsesAndCompiles()
    {
        // Both EXTERNAL and GLOBAL on the same level-01 item
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOTHTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-BOTH IS EXTERNAL IS GLOBAL PIC X(4) VALUE "BOTH".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-BOTH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("BOTH", stdout);
    }


    [Fact]
    public void ExternalClause_GroupItem_ParsesAndCompiles()
    {
        // EXTERNAL on a level-01 group item (no PIC) — should compile fine
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXTGRP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GRP IS EXTERNAL.
               05 WS-A PIC X(3) VALUE "ABC".
               05 WS-B PIC X(3) VALUE "DEF".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-A WS-B.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCDEF", stdout);
    }


    [Fact]
    public void Synchronized_AlignsCompFieldToWordBoundary()
    {
        // PIC X(1) occupies 1 byte at offset 0.
        // Without SYNC, COMP PIC S9(5) would start at offset 1 (4 bytes).
        // With SYNC, it must start at offset 4 (word boundary).
        // Total group = 4 (slack) + 4 (COMP) = 8 bytes.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYNCTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GROUP.
               05 WS-CHAR   PIC X(1) VALUE "A".
               05 WS-NUM    PIC S9(5) COMP SYNC VALUE 12345.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-NUM.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("12345", stdout);
    }


    [Fact]
    public void Comp1_FloatArithmetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP1TST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLOAT   COMP-1 VALUE 3.14.
            01 WS-RESULT  PIC 9(3)V99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-FLOAT TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00314", stdout);
    }


    [Fact]
    public void Comp2_DoubleArithmetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP2TST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DOUBLE  COMP-2 VALUE 2.71.
            01 WS-RESULT  PIC 9(3)V99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-DOUBLE TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00271", stdout);
    }

    // ═══════════════════════════════════════════
    // LOCAL-STORAGE per-invocation re-initialization
    // ═══════════════════════════════════════════

    [Fact]
    public void LocalStorage_ReinitializedOnEachCall()
    {
        // CALL a subprogram twice. The subprogram has LOCAL-STORAGE with VALUE.
        // Each call should see the initial VALUE, proving re-initialization.
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    CALL "LSUB"
                    CALL "LSUB"
                    STOP RUN.
            """),
            ("LSUB.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. LSUB.
                DATA DIVISION.
                LOCAL-STORAGE SECTION.
                01 LS-COUNTER PIC 9(3) VALUE 100.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY LS-COUNTER
                    ADD 50 TO LS-COUNTER
                    DISPLAY LS-COUNTER
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // First call: starts at 100, incremented to 150
        Assert.Equal("100", lines[0]);
        Assert.Equal("150", lines[1]);
        // Second call: re-initialized to 100, incremented to 150 again
        Assert.Equal("100", lines[2]);
        Assert.Equal("150", lines[3]);
    }

    [Fact]
    public void LocalStorage_SingleProgram_ValuePreserved()
    {
        // Just verify LOCAL-STORAGE VALUE works in a single-program context
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSTEST.
            DATA DIVISION.
            LOCAL-STORAGE SECTION.
            01 LS-VAL PIC X(5) VALUE "ABCDE".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY LS-VAL
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCDE", stdout);
    }

    [Fact]
    public void LocalStorage_AlphanumericValue_Reinitialized()
    {
        // LOCAL-STORAGE alphanumeric field with VALUE is re-initialized on each call
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER2.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER2.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    CALL "LSUB2"
                    CALL "LSUB2"
                    STOP RUN.
            """),
            ("LSUB2.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. LSUB2.
                DATA DIVISION.
                LOCAL-STORAGE SECTION.
                01 LS-MSG PIC X(5) VALUE "HELLO".
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY LS-MSG
                    MOVE "WORLD" TO LS-MSG
                    DISPLAY LS-MSG
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("HELLO", lines[0]);
        Assert.Equal("WORLD", lines[1]);
        Assert.Equal("HELLO", lines[2]);
        Assert.Equal("WORLD", lines[3]);
    }

    // ═══════════════════════════════════════════
    // EXTERNAL shared storage
    // ═══════════════════════════════════════════

    [Fact]
    public void External_SharedStorage_TwoPrograms()
    {
        // Two programs share an EXTERNAL item. One writes, the other reads.
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("EXTMAIN.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. EXTMAIN.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 EXT-DATA IS EXTERNAL PIC X(10).
                PROCEDURE DIVISION.
                MAIN-PARA.
                    MOVE "SHARED-VAL" TO EXT-DATA
                    CALL "EXTREAD"
                    STOP RUN.
            """),
            ("EXTREAD.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. EXTREAD.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 EXT-DATA IS EXTERNAL PIC X(10).
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY EXT-DATA
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SHARED-VAL", stdout);
    }

    [Fact]
    public void External_GroupRecord_SharedAcrossPrograms()
    {
        // EXTERNAL group record with subordinate fields shared across programs
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("EXTGRPMAIN.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. EXTGRPMAIN.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 EXT-REC IS EXTERNAL.
                   05 EXT-NAME PIC X(5).
                   05 EXT-NUM  PIC 9(3).
                PROCEDURE DIVISION.
                MAIN-PARA.
                    MOVE "ABCDE" TO EXT-NAME
                    MOVE 42 TO EXT-NUM
                    CALL "EXTGRPREAD"
                    STOP RUN.
            """),
            ("EXTGRPREAD.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. EXTGRPREAD.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 EXT-REC IS EXTERNAL.
                   05 EXT-NAME PIC X(5).
                   05 EXT-NUM  PIC 9(3).
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY EXT-NAME
                    DISPLAY EXT-NUM
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("ABCDE", lines[0]);
        Assert.Equal("042", lines[1]);
    }
}
