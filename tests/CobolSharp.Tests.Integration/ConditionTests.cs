using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class ConditionTests : EndToEndTestBase
{
    [Fact]
    public void Compare_NegativeLiteral_EqualAndNotEqual()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEGLITCMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE -8036.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A EQUAL TO -8036
                    DISPLAY "EQ-YES"
                ELSE
                    DISPLAY "EQ-NO"
                END-IF.
                IF WS-A NOT EQUAL TO -8036
                    DISPLAY "NEQ-YES"
                ELSE
                    DISPLAY "NEQ-NO"
                END-IF.
                IF WS-A NOT EQUAL TO -9999
                    DISPLAY "DIFF-YES"
                ELSE
                    DISPLAY "DIFF-NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("EQ-YES", lines[0]);
        Assert.Equal("NEQ-NO", lines[1]);
        Assert.Equal("DIFF-YES", lines[2]);
    }

    // ═══════════════════════════════════════════
    // GIVING forms (ADD and SUBTRACT)
    // ═══════════════════════════════════════════


    [Fact]
    public void ClassCondition_IsNumeric()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSNUM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5) VALUE "12345".
            01 WS-B PIC X(5) VALUE "12A45".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A IS NUMERIC
                    DISPLAY "A-NUM"
                ELSE
                    DISPLAY "A-NOT"
                END-IF.
                IF WS-B IS NUMERIC
                    DISPLAY "B-NUM"
                ELSE
                    DISPLAY "B-NOT"
                END-IF.
                IF WS-B IS NOT NUMERIC
                    DISPLAY "B-NOTNUM"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A-NUM", lines[0]);
        Assert.Equal("B-NOT", lines[1]);
        Assert.Equal("B-NOTNUM", lines[2]);
    }


    [Fact]
    public void ClassCondition_IsAlphabetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSALPHA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5) VALUE "ABCDE".
            01 WS-B PIC X(5) VALUE "abcde".
            01 WS-C PIC X(5) VALUE "AB1DE".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A IS ALPHABETIC
                    DISPLAY "A-ALPHA"
                END-IF.
                IF WS-A IS ALPHABETIC-UPPER
                    DISPLAY "A-UPPER"
                END-IF.
                IF WS-B IS ALPHABETIC-LOWER
                    DISPLAY "B-LOWER"
                END-IF.
                IF WS-C IS NOT ALPHABETIC
                    DISPLAY "C-NOTALPHA"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A-ALPHA", lines[0]);
        Assert.Equal("A-UPPER", lines[1]);
        Assert.Equal("B-LOWER", lines[2]);
        Assert.Equal("C-NOTALPHA", lines[3]);
    }

    // ── NEXT SENTENCE ──


    [Fact]
    public void AbbreviatedRelation_EqualOrBare()
    {
        // IF A = B OR C  →  (A = B) OR (A = C)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B OR C
                    DISPLAY "MATCH"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("MATCH", stdout);
    }


    [Fact]
    public void AbbreviatedRelation_EqualOrBare_NoMatch()
    {
        // IF A = B OR C  →  (A = B) OR (A = C), neither true
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR1N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B OR C
                    DISPLAY "MATCH"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("NO", stdout);
    }


    [Fact]
    public void AbbreviatedRelation_GreaterAndBare()
    {
        // IF A > B AND C  →  (A > B) AND (A > C)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 9.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A > B AND C
                    DISPLAY "BOTH"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("BOTH", stdout);
    }


    [Fact]
    public void AbbreviatedRelation_GreaterAndBare_OneFails()
    {
        // IF A > B AND C  →  (A > B) AND (A > C), second fails
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR2N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A > B AND C
                    DISPLAY "BOTH"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("NOT", stdout);
    }


    [Fact]
    public void AbbreviatedRelation_ExplicitNotRewritten()
    {
        // IF A < B AND B < C  → already explicit, no rewrite
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 5.
            01 C PIC 9 VALUE 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A < B AND B < C
                    DISPLAY "CHAIN"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("CHAIN", stdout);
    }

    // ── FILE I/O ──


    [Fact]
    public void SignCondition_PositiveNegativeZero()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-VAL PIC S9(5) VALUE +100.
            01  WS-NEG PIC S9(5) VALUE -50.
            01  WS-ZER PIC S9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-VAL IS POSITIVE
                    DISPLAY "POS-YES"
                END-IF
                IF WS-NEG IS NEGATIVE
                    DISPLAY "NEG-YES"
                END-IF
                IF WS-ZER IS ZERO
                    DISPLAY "ZERO-YES"
                END-IF
                IF WS-VAL IS NOT NEGATIVE
                    DISPLAY "NOT-NEG"
                END-IF
                IF WS-NEG IS NOT POSITIVE
                    DISPLAY "NOT-POS"
                END-IF
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("POS-YES", lines[0]);
        Assert.Equal("NEG-YES", lines[1]);
        Assert.Equal("ZERO-YES", lines[2]);
        Assert.Equal("NOT-NEG", lines[3]);
        Assert.Equal("NOT-POS", lines[4]);
    }


    [Fact]
    public void NotCondition_NegatesComparison()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NOTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-A PIC 9(3) VALUE 100.
            01  WS-B PIC 9(3) VALUE 200.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF NOT WS-A > WS-B
                    DISPLAY "NOT-GT"
                END-IF
                IF NOT WS-A = WS-B
                    DISPLAY "NOT-EQ"
                END-IF
                IF NOT (WS-A < WS-B)
                    DISPLAY "SHOULD-NOT-PRINT"
                ELSE
                    DISPLAY "PAREN-NOT"
                END-IF
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("NOT-GT", lines[0]);
        Assert.Equal("NOT-EQ", lines[1]);
        Assert.Equal("PAREN-NOT", lines[2]);
    }

    // ═══════════════════════════════════════════
    // User-defined CLASS condition
    // ═══════════════════════════════════════════

    [Fact]
    public void UserDefinedClass_IsMyClass()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDCLASS.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CLASS MY-CLASS IS "ABC".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-YES PIC X(3) VALUE "ABC".
            01 WS-NO  PIC X(3) VALUE "ABD".
            01 WS-MIX PIC X(3) VALUE "AXB".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-YES IS MY-CLASS
                    DISPLAY "YES-IN"
                ELSE
                    DISPLAY "YES-OUT"
                END-IF.
                IF WS-NO IS MY-CLASS
                    DISPLAY "NO-IN"
                ELSE
                    DISPLAY "NO-OUT"
                END-IF.
                IF WS-MIX IS NOT MY-CLASS
                    DISPLAY "MIX-NOT"
                ELSE
                    DISPLAY "MIX-IS"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("YES-IN", lines[0]);
        Assert.Equal("NO-OUT", lines[1]);
        Assert.Equal("MIX-NOT", lines[2]);
    }

    [Fact]
    public void UserDefinedClass_ThruRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UDRANGE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CLASS DIGIT-CLASS IS "0" THRU "9".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM PIC X(3) VALUE "123".
            01 WS-MIX PIC X(3) VALUE "1A3".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-NUM IS DIGIT-CLASS
                    DISPLAY "NUM-YES"
                END-IF.
                IF WS-MIX IS DIGIT-CLASS
                    DISPLAY "MIX-YES"
                ELSE
                    DISPLAY "MIX-NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("NUM-YES", lines[0]);
        Assert.Equal("MIX-NO", lines[1]);
    }

    // ═══════════════════════════════════════════
    // SYMBOLIC CHARACTERS
    // ═══════════════════════════════════════════

    [Fact]
    public void SymbolicCharacters_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMCHAR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS SYM-A IS 66.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLD PIC X(3) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SYM-A TO WS-FLD.
                IF WS-FLD = "A  "
                    DISPLAY "SYM-OK"
                ELSE
                    DISPLAY "SYM-FAIL:" WS-FLD ":"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("SYM-OK", lines[0]);
    }

    // ═══════════════════════════════════════════
    // ALPHABET / PROGRAM COLLATING SEQUENCE
    // ═══════════════════════════════════════════

    [Fact]
    public void CollatingSequence_ReverseOrder()
    {
        // Define an alphabet where "B" sorts before "A"
        // Native: A=65, B=66, so A < B
        // With reversed alphabet: B < A
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COLLSEQ.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86 PROGRAM COLLATING SEQUENCE
                IS MY-ORDER.
            SPECIAL-NAMES.
                ALPHABET MY-ORDER IS
                    "B", "A".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(1) VALUE "A".
            01 WS-B PIC X(1) VALUE "B".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A > WS-B
                    DISPLAY "A-GT-B"
                ELSE
                    DISPLAY "A-NOT-GT"
                END-IF.
                IF WS-B < WS-A
                    DISPLAY "B-LT-A"
                ELSE
                    DISPLAY "B-NOT-LT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // With our custom alphabet: B has weight 0, A has weight 1
        // So A > B (A's weight 1 > B's weight 0)
        Assert.Equal("A-GT-B", lines[0]);
        Assert.Equal("B-LT-A", lines[1]);
    }

}
