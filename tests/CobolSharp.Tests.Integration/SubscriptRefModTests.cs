using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class SubscriptRefModTests : EndToEndTestBase
{
    [Fact]
    public void Subscript_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 7 TO ITEM(3).
                DISPLAY ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("7", stdout);
    }


    [Fact]
    public void Subscript_MultipleElements()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 5 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ITEM(1).
                MOVE 2 TO ITEM(2).
                MOVE 3 TO ITEM(3).
                DISPLAY ITEM(1) ITEM(2) ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("123", stdout);
    }


    [Fact]
    public void Subscript_InGoToDepending()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GO-TABLE.
               05 GO-SCRIPT PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO GO-SCRIPT(1).
                GO TO P1 P2 P3 DEPENDING ON GO-SCRIPT(1).
                DISPLAY "FALL".
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
    public void Subscript_VariableSubscript_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. VSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO I.
                MOVE 3 TO J.
                MOVE 0 TO ITEM(1).
                MOVE 0 TO ITEM(2).
                MOVE 0 TO ITEM(3).
                MOVE 7 TO ITEM(I).
                MOVE ITEM(I) TO ITEM(J).
                DISPLAY ITEM(1) ITEM(2) ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("707", stdout);
    }


    [Fact]
    public void Subscript_ExpressionSubscript_Addition()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXPRSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 5 TIMES.
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO ITEM(1) ITEM(2) ITEM(3) ITEM(4) ITEM(5).
                MOVE 2 TO I.
                MOVE 7 TO ITEM(I + 1).
                DISPLAY ITEM(1) ITEM(2) ITEM(3) ITEM(4) ITEM(5).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00700", stdout);
    }


    [Fact(Skip = "COBOL-2002: general arithmetic in subscripts not valid in COBOL-85")]
    public void Subscript_ExpressionSubscript_Multiplication()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXPRSUB2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 9 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO ITEM(1) ITEM(2) ITEM(3) ITEM(4)
                          ITEM(5) ITEM(6) ITEM(7) ITEM(8)
                          ITEM(9).
                MOVE 2 TO I.
                MOVE 3 TO J.
                MOVE 5 TO ITEM(I * J).
                DISPLAY ITEM(6).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("5", stdout);
    }


    [Fact]
    public void Subscript_2D_ConstantSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO COL(1, 1).
                MOVE 2 TO COL(1, 2).
                MOVE 3 TO COL(1, 3).
                MOVE 4 TO COL(2, 1).
                MOVE 5 TO COL(2, 2).
                MOVE 6 TO COL(2, 3).
                DISPLAY COL(1, 1) COL(1, 2) COL(1, 3)
                        COL(2, 1) COL(2, 2) COL(2, 3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("123456", stdout);
    }


    [Fact]
    public void Subscript_2D_VariableSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. VSUB2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO I.
                MOVE 3 TO J.
                MOVE 7 TO COL(I, J).
                DISPLAY COL(2, 3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("7", stdout);
    }


    [Fact]
    public void Subscript_3D_ConstantSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB3D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CUBE.
               05 X-DIM OCCURS 2 TIMES.
                  10 Y-DIM OCCURS 2 TIMES.
                     15 Z-ITEM PIC 9 OCCURS 2 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 9 TO Z-ITEM(2, 1, 2).
                DISPLAY Z-ITEM(2, 1, 2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }


    [Fact]
    public void Subscript_RedefinesWithOccurs()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REDSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 A OCCURS 3 TIMES.
                  10 F1 PIC X(4).
                  10 F2 REDEFINES F1 PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "1234" TO F1(2).
                DISPLAY F2(2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1234", stdout);
    }


    [Fact]
    public void Subscript_GroupOccursChild()
    {
        // Subscripted reference to a child of an OCCURS group.
        // The step size must be the group's element size (VAL + FLAG = 2),
        // not the child's size (VAL = 1). Without this, VAL(2) reads
        // offset 1 instead of offset 2.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPOCC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 3 TIMES.
                  10 VAL PIC 9.
                  10 FLAG PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO VAL(1).
                MOVE "A" TO FLAG(1).
                MOVE 2 TO VAL(2).
                MOVE "B" TO FLAG(2).
                MOVE 3 TO VAL(3).
                MOVE "C" TO FLAG(3).
                DISPLAY VAL(1) FLAG(1) VAL(2) FLAG(2) VAL(3) FLAG(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1A2B3C", stdout);
    }

    // ── Reference Modification ──


    [Fact]
    public void RefMod_ConstantStartLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                DISPLAY FIELD(3:4).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDEF", stdout);
    }


    [Fact]
    public void RefMod_WithSubscript()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC X(4) OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO ITEM(2).
                DISPLAY ITEM(2)(2:2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("BC", stdout);
    }


    [Fact]
    public void RefMod_VariableStart()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                MOVE 4 TO I.
                DISPLAY FIELD(I:3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DEF", stdout);
    }


    [Fact]
    public void RefMod_RestOfField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDE" TO FIELD.
                DISPLAY FIELD(3:).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDE", stdout);
    }


    [Fact]
    public void RefMod_ExpressionStartLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                DISPLAY FIELD(2 + 1:4 - 1).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDE", stdout);
    }


    [Fact]
    public void RefMod_2DSubscriptWithRefMod()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC X(4) OCCURS 2 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO COL(2, 1).
                DISPLAY COL(2, 1)(3:2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CD", stdout);
    }


    [Fact]
    public void Subscript_Comp3Array()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP3SUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM PIC 9(4) COMP-3 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234 TO ITEM(2).
                DISPLAY ITEM(2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1234", stdout);
    }

    // ── GO TO DEPENDING ──


    [Fact]
    public void Search_SimpleMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES
                  INDEXED BY IDX.
            01 IDX-VAL PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                SET IDX TO 1.
                SEARCH ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = 3
                        DISPLAY "FOUND 3"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 3", stdout);
    }


    [Fact]
    public void Search_NotFound()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES
                  INDEXED BY IDX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                SET IDX TO 1.
                SEARCH ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = 9
                        DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NOT FOUND", stdout);
    }


    [Fact]
    public void Search_MultiFieldWhen()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 VAL PIC 9.
                  10 FLAG PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO VAL(1).
                MOVE "N" TO FLAG(1).
                MOVE 1 TO VAL(2).
                MOVE "Y" TO FLAG(2).
                MOVE 2 TO VAL(3).
                MOVE "Y" TO FLAG(3).
                SET IDX TO 1.
                SEARCH ROW
                    AT END DISPLAY "NOT FOUND"
                    WHEN VAL(IDX) = 1 AND FLAG(IDX) = "Y"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }


    [Fact]
    public void Search_ChildOfOccursGroup_DifferentSizes()
    {
        // OCCURS group with children of different sizes (A=2, B=3, group=5).
        // Step size must be 5 (group), not 2 (A) or 3 (B).
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPSRCH.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 A PIC X(2).
                  10 B PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "BB" TO A(2).
                MOVE "CC" TO A(3).
                SET IDX TO 1.
                SEARCH ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = "BB"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }


    [Fact]
    public void Search_ChildOfOccursGroup_MultiFieldWhen()
    {
        // Two child-of-OCCURS references in the same WHEN condition.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPMF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 A PIC X(2).
                  10 B PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "111" TO B(1).
                MOVE "BB" TO A(2).
                MOVE "222" TO B(2).
                MOVE "CC" TO A(3).
                MOVE "333" TO B(3).
                SET IDX TO 1.
                SEARCH ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = "BB" AND B(IDX) = "222"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }


    [Fact]
    public void SearchAll_ChildOfOccursGroup()
    {
        // SEARCH ALL with child-of-OCCURS group, ensuring the linear
        // lowering path also uses the correct group step size.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPSA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES.
                  10 A PIC X(2).
                  10 B PIC X(3).
            01 IDX PIC 9.
            01 K PIC X(2).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "BB" TO A(2).
                MOVE "CC" TO A(3).
                MOVE "BB" TO K.
                SEARCH ALL ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }

    // ── SEARCH with multi-dimensional OCCURS (outer index via PERFORM) ──


    [Fact]
    public void Search_2D_OuterIndexFromPerform()
    {
        // SEARCH only iterates the innermost dimension (COL).
        // Outer dimension (ROW) is driven by PERFORM VARYING.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. S2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL OCCURS 3 TIMES
                     INDEXED BY J.
                     15 A PIC X(2).
                     15 B PIC X(3).
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1).
                MOVE "BB" TO A(1, 2).
                MOVE "CC" TO A(1, 3).
                MOVE "DD" TO A(2, 1).
                MOVE "EE" TO A(2, 2).
                MOVE "FF" TO A(2, 3).
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    SET J TO 1
                    SEARCH COL
                        AT END DISPLAY "MISS " I
                        WHEN A(I, J) = "EE"
                            DISPLAY "FOUND " I " 2"
                    END-SEARCH
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("MISS 1", lines[0]);
        Assert.Equal("FOUND 2 2", lines[1]);
    }


    [Fact]
    public void Search_3D_OuterIndicesFromPerform()
    {
        // 3D OCCURS: SEARCH iterates innermost (COL), outer two via nested PERFORM.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. S3D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 PLANE OCCURS 2 TIMES.
                  10 ROW OCCURS 2 TIMES.
                     15 COL OCCURS 2 TIMES
                        INDEXED BY C.
                        20 A PIC X(2).
                        20 B PIC X(3).
            01 P PIC 9.
            01 R PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1, 1).
                MOVE "BB" TO A(1, 1, 2).
                MOVE "CC" TO A(1, 2, 1).
                MOVE "DD" TO A(1, 2, 2).
                MOVE "EE" TO A(2, 1, 1).
                MOVE "FF" TO A(2, 1, 2).
                MOVE "GG" TO A(2, 2, 1).
                MOVE "HH" TO A(2, 2, 2).
                PERFORM VARYING P FROM 1 BY 1 UNTIL P > 2
                    PERFORM VARYING R FROM 1 BY 1 UNTIL R > 2
                        SET C TO 1
                        SEARCH COL
                            AT END DISPLAY "MISS"
                            WHEN A(P, R, C) = "GG"
                                DISPLAY "FOUND " P " " R " 1"
                        END-SEARCH
                    END-PERFORM
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // Misses for (1,1), (1,2), (2,1), then found at (2,2,1)
        Assert.Equal("MISS", lines[0]);
        Assert.Equal("MISS", lines[1]);
        Assert.Equal("MISS", lines[2]);
        Assert.Equal("FOUND 2 2 1", lines[3]);
    }


    [Fact]
    public void SearchAll_2D_OuterIndexFromPerform()
    {
        // SEARCH ALL with 2D OCCURS, outer index via PERFORM.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL OCCURS 3 TIMES.
                     15 A PIC X(2).
                     15 B PIC X(3).
            01 I PIC 9.
            01 J PIC 9.
            01 K PIC X(2).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1).
                MOVE "BB" TO A(1, 2).
                MOVE "CC" TO A(1, 3).
                MOVE "DD" TO A(2, 1).
                MOVE "EE" TO A(2, 2).
                MOVE "FF" TO A(2, 3).
                MOVE "EE" TO K.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    SEARCH ALL COL
                        AT END DISPLAY "MISS " I
                        WHEN A(I, J) = K
                            DISPLAY "FOUND " I " " J
                    END-SEARCH
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("MISS 1", lines[0]);
        Assert.Equal("FOUND 2 2", lines[1]);
    }

    // ── SEARCH ALL ──


    [Fact]
    public void SearchAll_ExactMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 3 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 3", stdout);
    }


    [Fact]
    public void SearchAll_NotFound()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 9 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NOT FOUND", stdout);
    }


    [Fact]
    public void SearchAll_FirstElement()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 1 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 1", stdout);
    }


    [Fact]
    public void SearchAll_LastElement()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 5 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 5", stdout);
    }

    // ── STRING ──

}
