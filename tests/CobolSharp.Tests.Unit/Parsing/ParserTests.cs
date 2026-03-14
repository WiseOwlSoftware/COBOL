using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using Xunit;

namespace CobolSharp.Tests.Unit.Parsing;

public class ParserTests
{
    private CompilationUnit Parse(string input)
    {
        var source = SourceText.From(input);
        var diagnostics = new DiagnosticBag();
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens, diagnostics);
        return parser.ParseCompilationUnit();
    }

    [Fact]
    public void Parse_MinimalProgram_ExtractsProgramId()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HELLO.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        Assert.Single(ast.Programs);
        Assert.Equal("HELLO", ast.Programs[0].Identification.ProgramId);
    }

    [Fact]
    public void Parse_DisplayStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST1.
            PROCEDURE DIVISION.
                DISPLAY "Hello".
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        Assert.Equal(2, stmts.Count);
        Assert.IsType<DisplayStatement>(stmts[0]);
        Assert.IsType<StopRunStatement>(stmts[1]);

        var display = (DisplayStatement)stmts[0];
        Assert.Single(display.Operands);
        var lit = Assert.IsType<StringLiteralExpression>(display.Operands[0]);
        Assert.Equal("Hello", lit.Value);
    }

    [Fact]
    public void Parse_MoveStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(10).
            PROCEDURE DIVISION.
                MOVE "Alice" TO WS-NAME.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var move = Assert.IsType<MoveStatement>(stmts[0]);
        var src = Assert.IsType<StringLiteralExpression>(move.Source);
        Assert.Equal("Alice", src.Value);
        Assert.Single(move.Targets);
        Assert.Equal("WS-NAME", move.Targets[0].Name);
    }

    [Fact]
    public void Parse_AddStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TOTAL PIC 9(5).
            PROCEDURE DIVISION.
                ADD 10 TO WS-TOTAL.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var add = Assert.IsType<AddStatement>(stmts[0]);
        Assert.Single(add.Operands);
        Assert.Single(add.Targets);
    }

    [Fact]
    public void Parse_WorkingStorage_DataEntries()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(20).
            01 WS-AGE PIC 99.
            77 WS-TOTAL PIC 9(5)V99.
            PROCEDURE DIVISION.
                STOP RUN.
            """);

        var ws = ast.Programs[0].Data!.WorkingStorage!;
        Assert.Equal(3, ws.Entries.Count);

        Assert.Equal("WS-NAME", ws.Entries[0].Name);
        Assert.Equal(1, ws.Entries[0].LevelNumber);

        Assert.Equal("WS-AGE", ws.Entries[1].Name);

        Assert.Equal("WS-TOTAL", ws.Entries[2].Name);
        Assert.Equal(77, ws.Entries[2].LevelNumber);
    }

    [Fact]
    public void Parse_ComputeStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 RESULT PIC 9(5).
            PROCEDURE DIVISION.
                COMPUTE RESULT = 3 + 4 * 2.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var compute = Assert.IsType<ComputeStatement>(stmts[0]);
        Assert.Equal("RESULT", compute.Target.Name);
        // 3 + (4 * 2) due to precedence
        var binExpr = Assert.IsType<BinaryExpression>(compute.Value);
        Assert.Equal(BinaryOperator.Add, binExpr.Operator);
    }

    [Fact]
    public void Parse_IfElseEndIf()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9.
            PROCEDURE DIVISION.
                IF X = 1
                    DISPLAY "One"
                ELSE
                    DISPLAY "Other"
                END-IF.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var ifStmt = Assert.IsType<IfStatement>(stmts[0]);
        Assert.Single(ifStmt.ThenStatements);
        Assert.Single(ifStmt.ElseStatements);
    }

    [Fact]
    public void Parse_GobackStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST7.
            PROCEDURE DIVISION.
                GOBACK.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        Assert.Single(stmts);
        Assert.IsType<GobackStatement>(stmts[0]);
    }

    [Fact]
    public void Parse_IfThenEndIf()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST8.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9.
            PROCEDURE DIVISION.
                IF X = 1 THEN
                    DISPLAY "One"
                END-IF.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var ifStmt = Assert.IsType<IfStatement>(stmts[0]);
        Assert.Single(ifStmt.ThenStatements);
        Assert.Empty(ifStmt.ElseStatements);
    }

    [Fact]
    public void Parse_EvaluateStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST9.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9.
            PROCEDURE DIVISION.
                EVALUATE X
                    WHEN 1
                        DISPLAY "One"
                    WHEN 2
                        DISPLAY "Two"
                    WHEN OTHER
                        DISPLAY "Other"
                END-EVALUATE.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var eval = Assert.IsType<EvaluateStatement>(stmts[0]);
        Assert.Equal(2, eval.WhenClauses.Count);
        Assert.Single(eval.WhenOtherStatements);
    }

    [Fact]
    public void Parse_MultiplyStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST10.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(5).
            PROCEDURE DIVISION.
                MULTIPLY 5 BY X.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var mul = Assert.IsType<MultiplyStatement>(stmts[0]);
        Assert.Single(mul.Targets);
        Assert.Equal("X", mul.Targets[0].Name);
    }

    [Fact]
    public void Parse_SetStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST11.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9(3).
            PROCEDURE DIVISION.
                SET IDX TO 1.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var set = Assert.IsType<SetStatement>(stmts[0]);
        Assert.Single(set.Targets);
        Assert.Equal("IDX", set.Targets[0].Name);
        Assert.Equal(SetAction.To, set.Action);
    }

    [Fact]
    public void Parse_PerformVarying()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST12.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9(3).
            PROCEDURE DIVISION.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 10
                    DISPLAY I
                END-PERFORM.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var perform = Assert.IsType<PerformStatement>(stmts[0]);
        Assert.Null(perform.ParagraphName);
        Assert.NotNull(perform.Varying);
        Assert.Equal("I", perform.Varying!.Identifier.Name);
        Assert.NotNull(perform.Until);
        Assert.Single(perform.Body);
    }

    [Fact]
    public void Parse_PerformOutOfLineUntil()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST13.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                STOP RUN.
            READ-LOOP.
                DISPLAY "reading".
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var perform = Assert.IsType<PerformStatement>(stmts[0]);
        Assert.Equal("READ-LOOP", perform.ParagraphName);
        Assert.NotNull(perform.Until);
        Assert.Empty(perform.Body);
    }

    [Fact]
    public void Parse_ClassCondition_IsNumeric()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST14.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC X(5).
            PROCEDURE DIVISION.
                IF X IS NUMERIC
                    DISPLAY "yes"
                END-IF.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var ifStmt = Assert.IsType<IfStatement>(stmts[0]);
        var cond = Assert.IsType<BinaryExpression>(ifStmt.Condition);
        Assert.Equal(BinaryOperator.Equal, cond.Operator);
    }

    [Fact]
    public void Parse_DivideStatement()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST15.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(5).
            01 R PIC 9(5).
            PROCEDURE DIVISION.
                DIVIDE 10 INTO X REMAINDER R.
                STOP RUN.
            """);

        var stmts = ast.Programs[0].Procedure!.Statements;
        var div = Assert.IsType<DivideStatement>(stmts[0]);
        Assert.Single(div.Targets);
        Assert.NotNull(div.Remainder);
        Assert.Equal("R", div.Remainder!.Name);
    }
}
