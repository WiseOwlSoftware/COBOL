// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ CONFORMING SOURCE PAYS NOTHING FOR THE BARE-VERB QUESTION (kb/Work PB4283, branch review round 1, R1-1). A reservation-gated
/// statement verb alone in its sentence (<c>GOBACK.</c>) is a legal statement, and the parse asks, to take it as a statement,
/// whether another sentence names the word as a procedure (<c>CobolParserCoreBase.NamedAsAProcedureElsewhere</c>). The
/// evidence is gathered once per word, and a token that is itself a bare verb sentence is no reference without a read, so a
/// program of N <c>GOBACK.</c> paragraphs costs no speculative read at all. The witness is the count of reads the parser made
/// (a structural guard, never a wall-clock limit): the first version read the sentence of every other GOBACK for each
/// GOBACK, quadratic in them (800 paragraphs took 36 s).
/// </summary>
public sealed class BareVerbSentenceEvidenceTests
{
    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void ProgramOfBareGobackParagraphs_MakesNoSpeculativeRead(int edition)
    {
        var text = "PROCEDURE DIVISION.\n" + string.Concat(Enumerable.Range(1, 60).Select(n => $"P{n}.\n    GOBACK.\n"));
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        var parser = new CobolParserCore(new CommonTokenStream(lexer)) { Edition = EditionInfo.Of(edition, false) };
        parser.RemoveErrorListeners();
        var tree = parser.procedureDivision();
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
        Assert.Equal(60, tree.GetText().Split("GOBACK", StringSplitOptions.None).Length - 1);
        Assert.Equal(0, parser.ProcedureNameReadsMade);
    }

    /// <summary>The evidence still decides: one reference elsewhere (<c>PERFORM GOBACK.</c>) is found once, whatever the number
    /// of bare sentences asking.</summary>
    [Fact]
    public void AReferenceElsewhere_IsFoundWithOneReadPerNonBareSentence()
    {
        var text = "PROCEDURE DIVISION.\nMAIN-P.\n    PERFORM GOBACK.\n    STOP RUN.\n"
                   + string.Concat(Enumerable.Range(1, 20).Select(n => $"Q{n}.\n    GOBACK.\n"));
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        var parser = new CobolParserCore(new CommonTokenStream(lexer)) { Edition = EditionInfo.Of(2023, false) };
        parser.RemoveErrorListeners();
        parser.procedureDivision();
        Assert.True(parser.ProcedureNameReadsMade == 1,
            $"the evidence is gathered once for the word, got {parser.ProcedureNameReadsMade} reads");
    }
}
