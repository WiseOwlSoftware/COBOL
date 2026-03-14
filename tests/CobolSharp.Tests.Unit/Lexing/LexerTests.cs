using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using Xunit;

namespace CobolSharp.Tests.Unit.Lexing;

public class LexerTests
{
    private List<Token> Lex(string input)
    {
        var source = SourceText.From(input);
        var diagnostics = new DiagnosticBag();
        var lexer = new Lexer(source, diagnostics);
        return lexer.Tokenize();
    }

    [Fact]
    public void Lex_EmptyInput_ReturnsEof()
    {
        var tokens = Lex("");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.EndOfFile, tokens[0].Kind);
    }

    [Fact]
    public void Lex_StringLiteral_DoubleQuoted()
    {
        var tokens = Lex("\"Hello, World!\"");
        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("Hello, World!", tokens[0].Value);
    }

    [Fact]
    public void Lex_StringLiteral_SingleQuoted()
    {
        var tokens = Lex("'Hello'");
        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("Hello", tokens[0].Value);
    }

    [Fact]
    public void Lex_StringLiteral_EscapedQuotes()
    {
        var tokens = Lex("\"He said \"\"hi\"\"\"");
        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("He said \"hi\"", tokens[0].Value);
    }

    [Fact]
    public void Lex_IntegerLiteral()
    {
        // 42 is in the level-number range (1-49), but it's lexed as IntegerLiteral.
        // Context-sensitive level number recognition happens in the parser.
        var tokens = Lex("42");
        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.IsType<long>(tokens[0].Value);
        Assert.Equal(42L, (long)tokens[0].Value!);
    }

    [Fact]
    public void Lex_DecimalLiteral()
    {
        var tokens = Lex("3.14");
        Assert.Equal(TokenKind.DecimalLiteral, tokens[0].Kind);
        Assert.Equal(3.14m, tokens[0].Value);
    }

    [Fact]
    public void Lex_Period()
    {
        var tokens = Lex(".");
        Assert.Equal(TokenKind.Period, tokens[0].Kind);
    }

    [Fact]
    public void Lex_Operators()
    {
        var tokens = Lex("+ - * / ** = < > <= >=");
        Assert.Equal(TokenKind.Plus, tokens[0].Kind);
        Assert.Equal(TokenKind.Minus, tokens[1].Kind);
        Assert.Equal(TokenKind.Multiply, tokens[2].Kind);
        Assert.Equal(TokenKind.Divide, tokens[3].Kind);
        Assert.Equal(TokenKind.Power, tokens[4].Kind);
        Assert.Equal(TokenKind.Equals, tokens[5].Kind);
        Assert.Equal(TokenKind.LessThan, tokens[6].Kind);
        Assert.Equal(TokenKind.GreaterThan, tokens[7].Kind);
        Assert.Equal(TokenKind.LessThanOrEqual, tokens[8].Kind);
        Assert.Equal(TokenKind.GreaterThanOrEqual, tokens[9].Kind);
    }

    [Fact]
    public void Lex_Keywords_CaseInsensitive()
    {
        var tokens = Lex("display DISPLAY Display");
        Assert.Equal(TokenKind.DisplayKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.DisplayKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.DisplayKeyword, tokens[2].Kind);
    }

    [Fact]
    public void Lex_HyphenatedKeywords()
    {
        var tokens = Lex("PROGRAM-ID END-IF WORKING-STORAGE");
        Assert.Equal(TokenKind.ProgramIdKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.EndIfKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.WorkingStorageKeyword, tokens[2].Kind);
    }

    [Fact]
    public void Lex_Identifier()
    {
        var tokens = Lex("WS-COUNTER MY-VAR");
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("WS-COUNTER", tokens[0].Text);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
    }

    [Fact]
    public void Lex_LevelNumber_LexedAsIntegerLiteral()
    {
        // Level numbers are lexed as IntegerLiteral; the parser handles context.
        var tokens = Lex("01 77 88");
        Assert.Equal(TokenKind.IntegerLiteral, tokens[0].Kind);
        Assert.Equal(1L, tokens[0].Value);
        Assert.Equal(TokenKind.IntegerLiteral, tokens[1].Kind);
        Assert.Equal(77L, tokens[1].Value);
        Assert.Equal(TokenKind.IntegerLiteral, tokens[2].Kind);
        Assert.Equal(88L, tokens[2].Value);
    }

    [Fact]
    public void Lex_FreeFormComment_Skipped()
    {
        var tokens = Lex("DISPLAY *> this is a comment\n\"Hello\"");
        Assert.Equal(TokenKind.DisplayKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.StringLiteral, tokens[1].Kind);
    }

    [Fact]
    public void Lex_FigurativeConstants()
    {
        var tokens = Lex("ZERO ZEROS ZEROES SPACE SPACES");
        Assert.Equal(TokenKind.ZeroKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.ZeroKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.ZeroKeyword, tokens[2].Kind);
        Assert.Equal(TokenKind.SpaceKeyword, tokens[3].Kind);
        Assert.Equal(TokenKind.SpaceKeyword, tokens[4].Kind);
    }

    [Fact]
    public void Lex_PicKeyword_Aliases()
    {
        // PIC and PICTURE are both recognized as PicKeyword
        // After PIC, lexer enters PictureString mode, so test them separately
        var tokens1 = Lex("PICTURE IS X(10)");
        Assert.Equal(TokenKind.PicKeyword, tokens1[0].Kind);
        Assert.Equal(TokenKind.PictureString, tokens1[1].Kind);
        Assert.Equal("X(10)", tokens1[1].Text);

        var tokens2 = Lex("PIC 9(5)V99");
        Assert.Equal(TokenKind.PicKeyword, tokens2[0].Kind);
        Assert.Equal(TokenKind.PictureString, tokens2[1].Kind);
        Assert.Equal("9(5)V99", tokens2[1].Text);
    }

    [Fact]
    public void Lex_PictureString_WithIS()
    {
        var tokens = Lex("PIC IS X(20)");
        Assert.Equal(TokenKind.PicKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.PictureString, tokens[1].Kind);
        Assert.Equal("X(20)", tokens[1].Text);
    }

    [Fact]
    public void Lex_ScopeTerminators()
    {
        var tokens = Lex("END-ADD END-SUBTRACT END-MULTIPLY END-DIVIDE END-COMPUTE END-CALL");
        Assert.Equal(TokenKind.EndAddKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.EndSubtractKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.EndMultiplyKeyword, tokens[2].Kind);
        Assert.Equal(TokenKind.EndDivideKeyword, tokens[3].Kind);
        Assert.Equal(TokenKind.EndComputeKeyword, tokens[4].Kind);
        Assert.Equal(TokenKind.EndCallKeyword, tokens[5].Kind);
    }

    [Fact]
    public void Lex_HexLiteral()
    {
        var tokens = Lex("X\"48454C4C4F\"");
        Assert.Equal(TokenKind.HexLiteral, tokens[0].Kind);
        Assert.Equal("48454C4C4F", tokens[0].Value);
    }

    [Fact]
    public void Lex_GobackKeyword()
    {
        var tokens = Lex("GOBACK");
        Assert.Equal(TokenKind.GobackKeyword, tokens[0].Kind);
    }

    [Fact]
    public void Lex_ThenKeyword()
    {
        var tokens = Lex("THEN");
        Assert.Equal(TokenKind.ThenKeyword, tokens[0].Kind);
    }

    [Fact]
    public void Lex_InOfKeywords()
    {
        var tokens = Lex("WS-NAME IN WS-RECORD OF WS-FILE");
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal(TokenKind.InKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[2].Kind);
        Assert.Equal(TokenKind.OfKeyword, tokens[3].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[4].Kind);
    }

    [Fact]
    public void Lex_CompKeyword_Aliases()
    {
        var tokens = Lex("COMP COMPUTATIONAL BINARY COMP-3 PACKED-DECIMAL");
        Assert.Equal(TokenKind.CompKeyword, tokens[0].Kind);
        Assert.Equal(TokenKind.CompKeyword, tokens[1].Kind);
        Assert.Equal(TokenKind.CompKeyword, tokens[2].Kind);
        Assert.Equal(TokenKind.Comp3Keyword, tokens[3].Kind);
        Assert.Equal(TokenKind.Comp3Keyword, tokens[4].Kind);
    }

    [Fact]
    public void Lex_MinimalCobolProgram()
    {
        string input = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HELLO.
            PROCEDURE DIVISION.
                DISPLAY "Hello, World!".
                STOP RUN.
            """;

        var tokens = Lex(input);
        var kinds = tokens.Select(t => t.Kind).ToList();

        Assert.Contains(TokenKind.IdentificationKeyword, kinds);
        Assert.Contains(TokenKind.DivisionKeyword, kinds);
        Assert.Contains(TokenKind.ProgramIdKeyword, kinds);
        Assert.Contains(TokenKind.ProcedureKeyword, kinds);
        Assert.Contains(TokenKind.DisplayKeyword, kinds);
        Assert.Contains(TokenKind.StringLiteral, kinds);
        Assert.Contains(TokenKind.StopKeyword, kinds);
        Assert.Contains(TokenKind.RunKeyword, kinds);
    }
}
