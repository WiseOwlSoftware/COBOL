// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHO ANSWERS FOR THE TEXT OF AN EXPANSION (kb/Work PB4469; ISO §2023:12.3.8.4 GR5 — an expansion is the skeleton's text with the formals
/// replaced; ISO §2023:9.3.12 — it is treated in all respects as a class that is not parameterized). The group binds the text of every expansion
/// it makes, and an expansion placed in an assembly of its own is compiled apart, which binds the same text again. The group therefore
/// withdraws the warnings it raised while binding the text of a skeleton read from the repository that another compilation compiles, and
/// the facts that decide WHICH are these: the token an expansion's text is made of names its <see cref="DiagnosticRegion"/>, the cursor
/// carries it through every pass that replays a captured position, a repository text's line is printed at its EXPANDS entry with the
/// expansion's name (two places of the text are two facts), and the user's own text, a skeleton written in the group, is never
/// withdrawn (kb/Work PB4483).
/// </summary>
public sealed class DiagnosticRegionTests
{
    private static CommonToken Token(int line, int column) => new(CobolLexer.IDENTIFIER) { Line = line, Column = column };

    [Fact]
    public void ATokenOfAnExpansion_PositionsTheCursorInItsRegion_AndAnyOtherTokenInNone()
    {
        var region = new DiagnosticRegion("HOLD-BOX");
        var edition = new EditionContext(2023);
        using (edition.At(new ExpansionToken(Token(8, 4), region)))
        {
            Assert.Same(region, edition.Cursor.Region);
            Assert.Equal((8, 4), (edition.Cursor.Line, edition.Cursor.Column));
            using (edition.At(Token(20, 0)))   // the user's own text, inside the scope of an expansion's
                Assert.Null(edition.Cursor.Region);
            Assert.Same(region, edition.Cursor.Region);
        }
        Assert.False(edition.Cursor.IsSet);
    }

    /// <summary>A region of the text of a skeleton read from the repository, reported at the EXPANDS entry on line 8, column 4 (0-based).</summary>
    private static DiagnosticRegion FromTheRepository(string name) => new(name, new DiagnosticCursor(8, 4));

    [Fact]
    public void ACapturedCursor_KeepsItsRegion_WhenAPostBuildPassPositionsAtItAgain()
    {
        var region = FromTheRepository("HOLD-BOX");
        var edition = new EditionContext(2023);
        DiagnosticCursor captured;
        using (edition.At(new ExpansionToken(Token(8, 40), region))) captured = edition.Cursor;
        using (edition.At(captured))
        {
            Assert.Same(region, edition.Cursor.Region);
            edition.Warning("COBOL3146", "later");
        }
        edition.WithdrawWarningsOf(region);
        Assert.Empty(edition.Warnings);
    }

    [Fact]
    public void AnUnsetCursor_LeavesTheCurrentPositionAndRegionStanding()
    {
        var region = new DiagnosticRegion("HOLD-BOX");
        var edition = new EditionContext(2023);
        using (edition.At(new ExpansionToken(Token(8, 4), region)))
        using (edition.At(default(DiagnosticCursor)))
            Assert.Same(region, edition.Cursor.Region);
    }

    [Fact]   // kb/Work PB4483: a repository text is printed at its entry and names the expansion; withdrawing it leaves the user's own line.
    public void ARepositoryTextsWarning_IsPrintedAtTheEntry_NamingTheExpansion_AndIsWithdrawnAlone()
    {
        var hold = FromTheRepository("HOLD-BOX");
        var edition = new EditionContext(2023) { SourceFile = "client.cob" };
        using (edition.At(new ExpansionToken(Token(8, 40), hold)))
            edition.Warning("COBOL0002", "both");
        using (edition.At(Token(8, 4)))   // the user's own text at the entry's own position, the same words
            edition.Warning("COBOL0002", "both");
        Assert.Equal(["client.cob(8,5): warning COBOL0002: both [in the expansion 'HOLD-BOX']", "client.cob(8,5): warning COBOL0002: both"],
            edition.Warnings);
        edition.WithdrawWarningsOf(hold);
        Assert.Equal(["client.cob(8,5): warning COBOL0002: both"], edition.Warnings);
    }

    [Fact]   // kb/Work PB4483: every place of a repository text prints at the entry; the place in the text keeps two places two facts.
    public void TwoPlacesOfARepositoryText_AreTwoFacts_AndOnePlaceSeenTwiceIsOne()
    {
        var hold = FromTheRepository("HOLD-BOX");
        var edition = new EditionContext(2023) { SourceFile = "client.cob" };
        foreach (int column in new[] { 40, 90, 40 })
            using (edition.At(new ExpansionToken(Token(8, column), hold)))
                edition.Warning("COBOL3146", "a relation of two literals");
        Assert.Equal(2, edition.Warnings.Count);
        Assert.All(edition.Warnings, w => Assert.Equal("client.cob(8,5): warning COBOL3146: a relation of two literals [in the expansion 'HOLD-BOX']", w));
        edition.WithdrawWarningsOf(hold);
        Assert.Empty(edition.Warnings);
        using (edition.At(new ExpansionToken(Token(8, 40), hold)))   // withdrawn means forgotten: the same fact can be reported again
            edition.Warning("COBOL3146", "a relation of two literals");
        Assert.Single(edition.Warnings);
    }

    [Fact]   // kb/Work PB4483: a skeleton written in the group is the user's text; two expansions of it are one fact, and nothing withdraws it.
    public void TheUsersOwnTextInTwoExpansions_IsOneFact_ThatNoWithdrawalRemoves()
    {
        var one = new DiagnosticRegion("ONE");
        var two = new DiagnosticRegion("TWO");
        var edition = new EditionContext(2023) { SourceFile = "lib.cob" };
        foreach (var region in new[] { one, two })
            using (edition.At(new ExpansionToken(Token(3, 4), region)))
                edition.Warning("COBOL0003", "a line no formal touches");
        Assert.Equal(["lib.cob(3,5): warning COBOL0003: a line no formal touches"], edition.Warnings);   // at its own position, unnamed
        edition.WithdrawWarningsOf(one);
        edition.WithdrawWarningsOf(two);
        Assert.Single(edition.Warnings);
    }

    [Fact]   // The next token an expansion's text gains is attributed automatically only if nothing builds one that names no region.
    public void NoTokenOfAnExpansionsTextIsBuiltWithoutItsRegion()
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Src(), "WiseOwl.Cobol.Compiler", "Oo", "OoExpansion.cs"));
        Assert.DoesNotContain("new CommonToken(", text, StringComparison.Ordinal);
    }

    /// <summary>A program text whose tokens are all tokens of <paramref name="region"/>, parsed as an expansion's text is
    /// (<c>OoExpansion</c>: the skeleton's tokens, parsed apart from the written tree). It holds a word with the removed character U+30FB
    /// last (a COBOL-2023 removal) and a zero-length literal (a COBOL-2014 introduction).</summary>
    private static ParserRuleContext TextOf(DiagnosticRegion region, EditionInfo edition)
    {
        string source = "IDENTIFICATION DIVISION.\nPROGRAM-ID. P1.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 ABC・ PIC X.\n"
            + "PROCEDURE DIVISION.\n    DISPLAY \"\".\n    STOP RUN.\n";
        var stream = new CommonTokenStream(new CobolLexer(new AntlrInputStream(source)));
        stream.Fill();
        IList<IToken> tokens = [.. stream.GetTokens().Select(t => (IToken)new ExpansionToken(t, region))];
        return Assert.IsAssignableFrom<ParserRuleContext>(
            FragmentParse.ParseTokens(tokens, edition, CobolWordsMap.Empty, p => p.compilationUnit()));
    }

    /// <summary>⛔ kb/Work PB4649, ISO §2023:8.3.2.1 + Annex B.3 (the word) and §2023:8.3.3.1 (the literal): the two frontend token screens position a
    /// diagnostic of an expansion's text by <see cref="ExpansionToken.CursorOf"/>, as the binder's walkers do, so a text of a repository
    /// skeleton is printed at the EXPANDS entry naming the expansion and the group can withdraw it. A screen that read the token's line and
    /// column alone (<c>sink.At(line, column)</c>) printed a repository text's position, in a file it never was in.</summary>
    [Theory]
    [InlineData(2023, true, "COBOL0902", false)]   // the removed character: a warning under --permissive, the word screen
    [InlineData(2002, false, "COBOL0900", true)]   // the zero-length literal below 2014: an error, the literal screen
    public void AFrontendTokenScreen_AttributesADiagnosticOfAnExpansionsText_ToItsRegion(int level, bool permissive, string code, bool error)
    {
        var region = FromTheRepository("HOLD-BOX");
        var edition = new EditionContext(level, permissive) { SourceFile = "client.cob" };
        var text = TextOf(region, edition.Edition);
        LiteralScreen.Run(text, edition.Edition, edition, gateFormats: true);
        WordCharacterScreen.RunOnTree(text, edition.Edition, edition);
        string line = Assert.Single(error ? edition.Diagnostics : edition.Warnings);
        Assert.StartsWith($"client.cob(8,5): {(error ? "error" : "warning")} {code}:", line, StringComparison.Ordinal);
        Assert.EndsWith("[in the expansion 'HOLD-BOX']", line, StringComparison.Ordinal);
        edition.WithdrawWarningsOf(region);
        Assert.Equal(error ? 1 : 0, edition.Diagnostics.Count + edition.Warnings.Count);   // a warning of the text is withdrawn, an error never
    }

    [Fact]
    public void AnErrorOfAnExpansionsText_IsNeverWithdrawn()
    {
        var region = FromTheRepository("HOLD-BOX");
        var edition = new EditionContext(2023);
        using (edition.At(new ExpansionToken(Token(8, 4), region)))
            edition.Error("COBOL0004", "an error stops the compilation before the expansion's own compilation runs");
        edition.WithdrawWarningsOf(region);
        Assert.Single(edition.Diagnostics);
    }
}
