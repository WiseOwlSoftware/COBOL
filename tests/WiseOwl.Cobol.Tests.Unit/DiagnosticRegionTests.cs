// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHO ANSWERS FOR THE TEXT OF AN EXPANSION (kb/Work PB4469; ISO §12.3.8.4 GR5 — an expansion is the skeleton's text with the formals
/// replaced; ISO §9.3.12 — it is treated in all respects as a class that is not parameterized). The group binds the text of every expansion
/// it makes, and an expansion placed in an assembly of its own is compiled apart, which binds the same text again. The group therefore
/// withdraws the warnings it raised while binding that text, and the facts that decide WHICH are these: the token an expansion's text is
/// made of names its <see cref="DiagnosticRegion"/>, the cursor carries it through every pass that replays a captured position, and a
/// warning that the user's own text raised too, or that a second expansion still standing raised, is not withdrawn.
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

    [Fact]
    public void ACapturedCursor_KeepsItsRegion_WhenAPostBuildPassPositionsAtItAgain()
    {
        var region = new DiagnosticRegion("HOLD-BOX");
        var edition = new EditionContext(2023);
        DiagnosticCursor captured;
        using (edition.At(new ExpansionToken(Token(8, 4), region))) captured = edition.Cursor;
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

    [Fact]
    public void AWarningOnlyAnExpansionRaised_IsWithdrawn_AndOneTheUsersOwnTextRaisedToo_Stays()
    {
        var hold = new DiagnosticRegion("HOLD-BOX");
        var edition = new EditionContext(2023);
        using (edition.At(new ExpansionToken(Token(8, 4), hold)))
        {
            edition.Warning("COBOL0001", "only the expansion");
            edition.Warning("COBOL0002", "both");
        }
        using (edition.At(Token(8, 4)))   // the same position, the same words, no region: one fact, owned by the user's text
            edition.Warning("COBOL0002", "both");
        edition.WithdrawWarningsOf(hold);
        Assert.Contains("warning COBOL0002: both", Assert.Single(edition.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public void AWarningTwoExpansionsRaised_StaysUntilBothAreWithdrawn()
    {
        var one = new DiagnosticRegion("ONE");
        var two = new DiagnosticRegion("TWO");
        var edition = new EditionContext(2023);
        foreach (var region in new[] { one, two })
            using (edition.At(new ExpansionToken(Token(3, 4), region)))
                edition.Warning("COBOL0003", "a line no formal touches");
        Assert.Single(edition.Warnings);   // one fact, one report (AddOnce)
        edition.WithdrawWarningsOf(one);
        Assert.Single(edition.Warnings);   // the second expansion still stands
        edition.WithdrawWarningsOf(two);
        Assert.Empty(edition.Warnings);
        using (edition.At(Token(3, 4)))   // withdrawn means forgotten: the same fact can be reported again
            edition.Warning("COBOL0003", "a line no formal touches");
        Assert.Single(edition.Warnings);
    }

    [Fact]   // The next token an expansion's text gains is attributed automatically only if nothing builds one that names no region.
    public void NoTokenOfAnExpansionsTextIsBuiltWithoutItsRegion()
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Src(), "WiseOwl.Cobol.Compiler", "Oo", "OoExpansion.cs"));
        Assert.DoesNotContain("new CommonToken(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnErrorOfAnExpansionsText_IsNeverWithdrawn()
    {
        var region = new DiagnosticRegion("HOLD-BOX");
        var edition = new EditionContext(2023);
        using (edition.At(new ExpansionToken(Token(8, 4), region)))
            edition.Error("COBOL0004", "an error stops the compilation before the expansion's own compilation runs");
        edition.WithdrawWarningsOf(region);
        Assert.Single(edition.Diagnostics);
    }
}
