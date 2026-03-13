using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using Xunit;

namespace CobolSharp.Tests.Unit.Parsing;

public class SubscriptRefModTests
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
    public void Parse_SingleSubscript()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TABLE.
               05 WS-ITEM PIC X(10) OCCURS 5 TIMES.
            PROCEDURE DIVISION.
                DISPLAY WS-ITEM(1).
                STOP RUN.
            """);

        var display = (DisplayStatement)ast.Programs[0].Procedure!.Statements[0];
        var id = Assert.IsType<IdentifierExpression>(display.Operands[0]);
        Assert.Equal("WS-ITEM", id.Name);
        Assert.True(id.HasSubscripts);
        Assert.Single(id.Subscripts!);
    }

    [Fact]
    public void Parse_MultipleSubscripts()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TABLE.
               05 WS-ROW OCCURS 3 TIMES.
                  10 WS-COL PIC 9 OCCURS 4 TIMES.
            PROCEDURE DIVISION.
                DISPLAY WS-COL(2, 3).
                STOP RUN.
            """);

        var display = (DisplayStatement)ast.Programs[0].Procedure!.Statements[0];
        var id = Assert.IsType<IdentifierExpression>(display.Operands[0]);
        Assert.True(id.HasSubscripts);
        Assert.Equal(2, id.Subscripts!.Count);
    }

    [Fact]
    public void Parse_ReferenceModification()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(20).
            PROCEDURE DIVISION.
                DISPLAY WS-NAME(1:5).
                STOP RUN.
            """);

        var display = (DisplayStatement)ast.Programs[0].Procedure!.Statements[0];
        var id = Assert.IsType<IdentifierExpression>(display.Operands[0]);
        Assert.Equal("WS-NAME", id.Name);
        Assert.True(id.HasRefMod);
        Assert.NotNull(id.RefModStart);
        Assert.NotNull(id.RefModLength);
    }

    [Fact]
    public void Parse_ReferenceModification_NoLength()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(20).
            PROCEDURE DIVISION.
                DISPLAY WS-NAME(3:).
                STOP RUN.
            """);

        var display = (DisplayStatement)ast.Programs[0].Procedure!.Statements[0];
        var id = Assert.IsType<IdentifierExpression>(display.Operands[0]);
        Assert.True(id.HasRefMod);
        Assert.NotNull(id.RefModStart);
        Assert.Null(id.RefModLength); // omitted length
    }

    [Fact]
    public void Parse_Paragraph_IdentifiedCorrectly()
    {
        var ast = Parse("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TEST5.
            PROCEDURE DIVISION.
                PERFORM PROCESS-DATA.
                STOP RUN.
            PROCESS-DATA.
                DISPLAY "Processing".
            """);

        Assert.Single(ast.Programs[0].Procedure!.Paragraphs);
        Assert.Equal("PROCESS-DATA", ast.Programs[0].Procedure!.Paragraphs[0].Name);
        Assert.Equal(2, ast.Programs[0].Procedure!.Statements.Count); // PERFORM + STOP RUN
    }
}
