// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FAULT'S TAIL STAYS WITH THE UNIT THE FAULT IS IN, UP TO ITS PERIOD OR WHERE THE STRUCTURE AROUND IT RESUMES
/// (kb/Work PB4173, PB4134; the recovery of <c>PB3843</c> generalized, <see cref="ParseRecoveryTailTests"/>).
/// <para>PB4173: an operand REFUSED inside its phrase (<c>OCCURS "A" TIMES</c>, <c>RECORD CONTAINS "A" CHARACTERS</c>,
/// <c>PERFORM "A" TIMES … END-PERFORM</c>) returns the rule that failed, and the phrase's remaining words were met by
/// the enclosing list as a clause or a statement of their own: a false COBOL1941, COBOL1970 or COBOL0306 for each. The
/// recovery now discards the rest of the entry or sentence, as it does for a construct closed early.</para>
/// <para>PB4134: the discard stops at the unit's period, but a unit may lack it, and then the period it ran to belonged
/// to the structure around the unit (<c>END PROGRAM A.</c>, <c>IDENTIFICATION DIVISION.</c>, <c>END DECLARATIVES.</c>, a
/// section header, <c>PROCEDURE DIVISION.</c> after a data entry): a true second fault in the eaten tokens was lost. The
/// discard also stops where the parse of some rule around the unit, completed, takes the next two tokens.</para>
/// </summary>
public sealed class ParseRecoveryUnitTailTests
{
    /// <summary>A refused operand in a data entry, a file description and a statement, with the words of its phrase after
    /// it: exactly one COBOL5506, naming the operand.</summary>
    [Theory]
    [InlineData("", "05 E PIC X OCCURS \"A\" TIMES.", "", "\"A\"")]
    [InlineData("", "05 E PIC X OCCURS \"A\" INDEXED BY IY.", "", "\"A\"")]
    [InlineData("", "", "RECORD CONTAINS \"A\" CHARACTERS", "\"A\"")]
    [InlineData("", "", "LINAGE IS \"A\" LINES", "\"A\"")]
    [InlineData("", "", "BLOCK CONTAINS \"A\" RECORDS", "\"A\"")]
    [InlineData("PERFORM \"A\" TIMES DISPLAY \"X\" END-PERFORM.", "", "", "\"A\"")]
    [InlineData("SET IX TO \"A\" DISPLAY \"X\".", "", "", "\"A\"")]
    [InlineData("COMPUTE W-N = 1 + \"A\" ON SIZE ERROR DISPLAY \"E\" END-COMPUTE.", "", "", "\"A\"")]
    [InlineData("CALL \"P\" RETURNING 5 ON EXCEPTION DISPLAY \"E\" END-CALL.", "", "", "5")]
    public void ARefusedOperand_DrawsOnlyItsOwnDiagnostic(string statements, string workingStorage, string f1Clauses, string operand)
    {
        string entries = workingStorage.Length == 0 ? "" : "01 TB.\n   " + workingStorage;
        var errors = RecoverySources.Errors(RecoverySources.Program(statements, entries, f1Clauses));
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("COBOL5506", errors[0]);
        Assert.Contains(operand, errors[0]);
    }

    /// <summary>The discard ends at the unit's period: the next entry and the next sentence are parsed afresh, so a
    /// second fault in either is a second diagnostic.</summary>
    [Fact]
    public void TheNextEntryAndTheNextSentence_AreParsedAfresh()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "PERFORM \"A\" TIMES DISPLAY \"X\" END-PERFORM. MOVE 1 W-N.",
            "01 TB.\n   05 E PIC X OCCURS \"A\" TIMES.\n01 UB PIC X VALUE IS."));
        Assert.True(errors.Count == 4, $"expected four diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("'\"A\"'", errors[0]);
        Assert.Contains("COBOL", errors[1]);
        Assert.Contains("'\"A\"'", errors[2]);
        Assert.Contains("'W-N'", errors[3]);
    }

    /// <summary>The missing-token fault has no tail: the recovery conjures the token and the statement goes on, so a
    /// second fault after it in the SAME sentence is still a second diagnostic.</summary>
    [Fact]
    public void AMissingToken_DoesNotDiscardTheRestOfTheSentence()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program("MOVE 1 W-N MOVE 2 W-K."));
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("'W-N'", errors[0]);
        Assert.Contains("'W-K'", errors[1]);
    }

    private const string Header = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {0}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(4) VALUE 0.
        PROCEDURE DIVISION.

        """;

    /// <summary>A faulted sentence without its period (the stray END-ADD is the first fault), then a malformed
    /// <c>END PROGRAM</c> header, which is the second: the discard ran through it to the header's period.</summary>
    [Fact]
    public void AFaultedSentenceWithoutItsPeriod_StopsAtEndProgram()
    {
        string source = string.Format(Header, "PRTAIL1")
            + "MAIN-PARA.\n    IF W-N = 1 MOVE 1 TO W-N END-ADD\nEND PROGRAM 123.\n";
        var errors = RecoverySources.Errors(source);
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("COBOL0306", errors[0]);
        Assert.Contains("'123'", errors[1]);
    }

    /// <summary>The next program's header, the end of the declaratives, the next section's header and the next division
    /// are where the structure around the faulted unit resumes: the second fault after each is reported, the first is
    /// not repeated.</summary>
    [Theory]
    // a program after the faulted last sentence of the first: its header, then a fault in it
    [InlineData("MAIN-PARA.\n    IF W-N = 1 MOVE 1 TO W-N END-ADD\nEND PROGRAM PRTAIL2.\nIDENTIFICATION DIVISION.\nPROGRAM-ID. PRTAIL3.\n"
        + "PROCEDURE DIVISION.\n    MOVE 1 W-N.\nEND PROGRAM PRTAIL3.\n", "COBOL0306", "'W-N'")]
    // the end of the declaratives, then a fault in the main paragraph
    [InlineData("DECLARATIVES.\nDS SECTION.\n    USE AFTER STANDARD ERROR PROCEDURE ON INPUT.\nDP.\n"
        + "    IF W-N = 1 MOVE 1 TO W-N END-ADD\nEND DECLARATIVES.\nMAIN-PARA.\n    MOVE 1 W-N.\n", "COBOL0306", "'W-N'")]
    // the next section's header, then a fault in its paragraph
    [InlineData("MAIN-PARA.\n    IF W-N = 1 MOVE 1 TO W-N END-ADD\nSEC2 SECTION.\nPARA2.\n    MOVE 1 W-N.\n", "COBOL0306", "'W-N'")]
    public void AFaultedSentenceWithoutItsPeriod_StopsWhereTheStructureResumes(string procedure, string first, string second)
    {
        string source = string.Format(Header, "PRTAIL2") + procedure;
        var errors = RecoverySources.Errors(source);
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains(first, errors[0]);
        Assert.Contains(second, errors[1]);
    }

    /// <summary>A nested program after the host's faulted last sentence (which lacks its period): the nested program's
    /// header is where the structure resumes, and the fault is the one diagnostic.</summary>
    [Fact]
    public void AFaultedSentenceWithoutItsPeriod_StopsAtANestedProgramHeader()
    {
        string source = string.Format(Header, "PRHOST")
            + "MAIN-PARA.\n    IF W-N = 1 MOVE 1 TO W-N END-ADD\nIDENTIFICATION DIVISION.\nPROGRAM-ID. PRNEST.\n"
            + "PROCEDURE DIVISION.\n    DISPLAY \"N\".\nEND PROGRAM PRNEST.\nEND PROGRAM PRHOST.\n";
        var errors = RecoverySources.Errors(source);
        Assert.True(errors.Count == 1, $"expected one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("COBOL0306", errors[0]);
    }

    /// <summary>A data entry that lacks its period, then the procedure division: the division header is where the
    /// structure resumes, so the entry's refused operand is one fault and the procedure division is parsed.</summary>
    [Fact]
    public void ARefusedOperandInAnEntryWithoutItsPeriod_StopsAtTheNextDivision()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PRTAIL4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 E PIC X OCCURS "A" TIMES
            PROCEDURE DIVISION.
                MOVE 1 W-N.

            """;
        var errors = RecoverySources.Errors(source);
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("'\"A\"'", errors[0]);
        Assert.Contains("'W-N'", errors[1]);
    }

    /// <summary>A paragraph header cannot be told from the last operand of the sentence and its period, so a faulted
    /// sentence without its period still discards through the header's period, as it did; the sentences after the header
    /// are parsed afresh.</summary>
    [Fact]
    public void AMissingPeriodBeforeAParagraphHeader_CostsTheHeaderNotTheNextFault()
    {
        string source = string.Format(Header, "PRTAIL5")
            + "MAIN-PARA.\n    IF W-N = 1 MOVE 1 TO W-N END-ADD\nNEXT-PARA.\n    MOVE 1 W-N.\n";
        var errors = RecoverySources.Errors(source);
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("COBOL0306", errors[0]);
        Assert.Contains("'W-N'", errors[1]);
    }

    /// <summary>The drift guard: the units whose tail the discard keeps are read from the ATN (the rules that end at a
    /// separator period), so an entry or paragraph the grammar adds is one the day it is added. The sentence and the
    /// entries of the data division are among them.</summary>
    [Fact]
    public void ThePeriodTerminatedUnits_AreReadFromTheGrammar()
    {
        var parser = new CobolParserCore(new CommonTokenStream(new CobolLexer(new AntlrInputStream(""))));
        var units = CobolErrorStrategy.PeriodTerminatedUnits(parser);
        Assert.Contains("sentence", units);
        Assert.Contains("dataDescriptionEntry", units);
        Assert.DoesNotContain("statement", units);
        Assert.DoesNotContain("ifStatement", units);
    }
}
