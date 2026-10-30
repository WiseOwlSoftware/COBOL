// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB3245 — the token a literal its line ends inside becomes. The general formats close every alphanumeric,
/// boolean and national literal (ISO §8.3.3.2.2, §8.3.3.4.2, §8.3.3.5.2) and a literal may stay open at a line's end
/// only when a continuation line continues it (§6.3.5 1), §6.4.2), which the reference-format stage joins before the
/// lexer runs. The lexer makes the rest the literal's OWN token (CobolLexer.g4 STR_BODY): its kind and prefix kept, its
/// extent the rest of the line less trailing spaces and one trailing separator period (OnDelimitedLiteral), so the
/// parser reads one literal and the statement still ends where the period says. Before it, the opening delimiter fell to
/// ANY_CHAR and the content lexed as words — the parser's unnamed COBOL0001 cascade.
/// </summary>
public sealed class UnterminatedLiteralTokenTests
{
    private static List<IToken> Tokens(string src)
    {
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream(src)));
        stream.Fill();
        return [.. stream.GetTokens().Where(t => t.Type != TokenConstants.EOF && t.Type != CobolLexer.WS)];
    }

    private static string Name(IToken t) => CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString();

    /// <summary>Each delimited-literal kind, both quotation symbols: one token of the literal's own type, ending before the
    /// trailing separator period (and trailing spaces), which is the DOT token after it, at its own column.</summary>
    [Theory]
    [InlineData("DISPLAY \"HELLO.", "STRINGLIT", "\"HELLO", 14)]
    [InlineData("DISPLAY \"HELLO.   ", "STRINGLIT", "\"HELLO", 14)]
    [InlineData("DISPLAY 'IT''S.", "STRINGLIT", "'IT''S", 14)]
    [InlineData("DISPLAY \"AB\"\".", "STRINGLIT", "\"AB\"\"", 13)]   // a doubled delimiter is content, not a close
    [InlineData("DISPLAY N\"AB.", "NATLIT", "N\"AB", 12)]
    [InlineData("DISPLAY NX'0041.", "NATLIT", "NX'0041", 15)]
    [InlineData("DISPLAY X'41.", "HEXLIT", "X'41", 12)]
    [InlineData("DISPLAY B\"01.", "BOOLLIT", "B\"01", 12)]
    [InlineData("DISPLAY BX\"F.", "BOOLLIT", "BX\"F", 12)]
    [InlineData("DISPLAY \".", "STRINGLIT", "\"", 9)]                // the opening delimiter alone is kept
    public void UnterminatedLiteral_IsItsKindsToken_AndGivesBackTheSeparatorPeriod(
        string src, string type, string text, int dotColumn)
    {
        var tokens = Tokens(src);
        Assert.Equal(["DISPLAY", type, "DOT"], tokens.Select(Name));
        Assert.Equal(text, tokens[1].Text);
        Assert.Equal(8, tokens[1].Column);
        Assert.Equal(dotColumn, tokens[2].Column);
    }

    /// <summary>The literal runs to the END OF ITS LINE and no further: a period inside it is content, and the next line is
    /// lexed afresh.</summary>
    [Fact]
    public void UnterminatedLiteral_EndsAtItsLine()
    {
        var tokens = Tokens("MOVE \"A. B TO X\nSTOP RUN.");
        Assert.Equal(["MOVE", "STRINGLIT", "STOP", "RUN", "DOT"], tokens.Select(Name));
        Assert.Equal("\"A. B TO X", tokens[1].Text);
        Assert.Equal(2, tokens[2].Line);
        Assert.Equal(0, tokens[2].Column);
    }

    /// <summary>A literal the line closes is the closed literal, whatever follows it: maximal munch prefers it, and the
    /// action cuts nothing — the closing delimiter's own separator context stays SeparatorRule's (§8.3.5 5)).</summary>
    [Theory]
    [InlineData("DISPLAY \"AB\".", "\"AB\"")]
    [InlineData("DISPLAY \"A.\".", "\"A.\"")]
    [InlineData("DISPLAY 'IT''S'.", "'IT''S'")]
    [InlineData("DISPLAY \"\".", "\"\"")]
    public void ClosedLiteral_IsUnchanged(string src, string text)
    {
        var tokens = Tokens(src);
        Assert.Equal(["DISPLAY", "STRINGLIT", "DOT"], tokens.Select(Name));
        Assert.Equal(text, tokens[1].Text);
    }

    /// <summary>The ONE report (LiteralScreen.ReportUnterminated): each open literal once, at its own token, under
    /// COBOL5563, naming the closing quotation symbol it lacks; a closed literal is not reported, and the verdict is false
    /// exactly when something was.</summary>
    [Fact]
    public void ReportUnterminated_NamesEachOpenLiteralOnce()
    {
        var reported = new List<(IToken Token, string Code, string Message)>();
        bool clean = LiteralScreen.ReportUnterminated(Tokens("DISPLAY \"OK\" 'IT''S\nDISPLAY N\"AB.\nDISPLAY \"DONE\"."),
            (t, d, m) => reported.Add((t, d.Code, m)));
        Assert.False(clean);
        Assert.Equal([(1, 13), (2, 8)], reported.Select(r => (r.Token.Line, r.Token.Column)));
        Assert.All(reported, r => Assert.Equal("COBOL5563", r.Code));
        Assert.Contains("closing apostrophe (')", reported[0].Message);
        Assert.Contains("closing quotation mark (\")", reported[1].Message);
        Assert.True(LiteralScreen.ReportUnterminated(Tokens("DISPLAY \"OK\"."), (_, _, _) => Assert.Fail("reported")));
    }

    /// <summary>A fixed-form debugging line is a COMMENT unless its unit declares WITH DEBUGGING MODE (kb/Work PB1705):
    /// an open literal on it is no literal, so nothing is reported; with the clause the line is source and it is.</summary>
    [Theory]
    [InlineData("SOURCE-COMPUTER. IBM-PC.", 0)]
    [InlineData("SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.", 1)]
    public void ReportUnterminated_SkipsADebuggingLineThatIsAComment(string paragraph, int expected)
    {
        string text = string.Join("\n",
            "PROGRAM-ID. P.", paragraph, ReferenceFormatProcessor.DebugLineCarrier + "DISPLAY \"OPEN", "STOP RUN.") + "\n";
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream(text)));
        DebuggingLineRewriter.Rewrite(stream);
        int count = 0;
        LiteralScreen.ReportUnterminated(stream.GetTokens(), (_, _, _) => count++);
        Assert.Equal(expected, count);
    }
}
