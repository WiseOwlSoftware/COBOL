// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY MODE OF CobolLexer IS TOTAL, AND A LEXER ERROR IS A DIAGNOSTIC (LexerErrorListener), NEVER A CONSOLE LINE
/// (kb/Work PB4233). PICMODE had no rule for the separator period, so <c>01 X PIC.</c> matched nothing there: ANTLR's default listener printed
/// "token recognition error" on the console, the period was dropped, the mode stayed open, and the next line's
/// <c>X(3)</c> became the picture of an entry the source left without one — the program compiled and ran. The front end
/// now reports every lexer error into the compilation's diagnostics (<see cref="LexerErrorListener"/>, COBOL5704), and
/// this test holds the lexer to reporting none on the suite's sources, where a mode that is not total shows first.
/// </summary>
public sealed class LexerTotalityDriftTests
{
    [Fact]
    public void LexingTheSuiteSources_ReportsNoLexerError()
    {
        var sources = LexerDfaCacheDriftTests.SuiteSources();
        Assert.True(sources.Count > 1000, $"only {sources.Count} suite sources found — the probe is blind");
        var offenders = new List<string>();
        foreach (var (path, text) in sources)
        {
            var (errors, _) = Lex(text);
            if (errors.Count > 0 && offenders.Count < 10) offenders.Add($"{path}: {errors[0]}");
        }
        Assert.True(offenders.Count == 0,
            "the lexer matched no rule in these suite sources — a mode of CobolLexer.g4 is not total, so its text is "
            + "dropped and the compile fails with COBOL5704:\n" + string.Join("\n", offenders));
    }

    /// <summary>A separator period where the picture character-string should be ends PICMODE as the DOT token, so the
    /// parser sees the clause without its character-string and the text after the period is lexed as what it is.</summary>
    [Theory]
    [InlineData("01 X PIC.\n    X(3) VALUE 'ABC'.\n")]
    [InlineData("01 X PICTURE IS.\n01 Y PIC X.\n")]
    [InlineData("01 X PIC IS. ")]
    [InlineData("01 X PIC.")]
    public void ASeparatorPeriodAfterPic_IsTheDotToken_AndEndsPicmode(string text)
    {
        var (errors, tokens) = Lex(text);
        Assert.Empty(errors);
        int pic = tokens.FindIndex(t => t.Type == CobolLexer.PIC);
        Assert.Equal(CobolLexer.DOT, tokens[pic + 1].Type);
        // The mode ended with the period: the word after it is lexed in DEFAULT mode, not as a picture.
        Assert.True(pic + 2 >= tokens.Count || tokens[pic + 2].Type != CobolLexer.PIC_STRING,
            $"'{tokens[^1].Text}' after the separator period was lexed as a picture character-string");
    }

    /// <summary>A period a picture character follows stays inside the picture character-string: the separator-period
    /// rule takes only what PIC_STRING cannot.</summary>
    [Theory]
    [InlineData("01 X PIC 9.99.\n", "9.99")]
    [InlineData("01 X PIC .99.\n", ".99")]
    [InlineData("01 X PIC 999999999999..\n", "999999999999.")]
    public void APeriodInsideThePicture_StaysInThePictureString(string text, string picture)
    {
        var (errors, tokens) = Lex(text);
        Assert.Empty(errors);
        int pic = tokens.FindIndex(t => t.Type == CobolLexer.PIC);
        Assert.Equal(CobolLexer.PIC_STRING, tokens[pic + 1].Type);
        Assert.Equal(picture, tokens[pic + 1].Text);
        Assert.Equal(CobolLexer.DOT, tokens[pic + 2].Type);
    }

    /// <summary>What the lexer reports reaches the diagnostics as COBOL5704, at the position the line map gives it.</summary>
    [Fact]
    public void ALexerError_IsReportedAsCobol5704()
    {
        var lexer = new CobolLexer(new AntlrInputStream("A"));
        var listener = LexerErrorListener.AttachTo(lexer);
        Assert.Single(lexer.ErrorListeners);   // ANTLR's console listener is gone
        listener.SyntaxError(TextWriter.Null, lexer, 0, 3, 7, "token recognition error at: '.'", null!);
        var bag = new DiagnosticBag();
        listener.ReportInto(bag, "p.cob", SourceLineMap.Identity("p.cob", 5));
        var d = Assert.Single(bag.Diagnostics);
        Assert.Equal(DiagnosticCatalog.LexerTextUnrecognized.Code, d.Code);
        Assert.True(d.IsError);
        Assert.Contains("token recognition error at: '.'", d.Message);
        Assert.Equal("p.cob(3,8)", d.Location.ToString());   // line 3, 0-based column 7
    }

    private static (List<string> Errors, List<IToken> Tokens) Lex(string text)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        var sink = new Collector();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(sink);
        var tokens = new List<IToken>();
        for (var t = lexer.NextToken(); t.Type != TokenConstants.EOF; t = lexer.NextToken())
            if (t.Channel == Lexer.DefaultTokenChannel) tokens.Add(t);
        return (sink.Errors, tokens);
    }

    private sealed class Collector : IAntlrErrorListener<int>
    {
        public List<string> Errors { get; } = [];

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
            => Errors.Add($"{line}:{charPositionInLine} {msg}");
    }
}
