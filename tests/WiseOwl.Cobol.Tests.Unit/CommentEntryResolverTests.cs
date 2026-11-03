// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB4281 (docs/CONFORMANCE.md D-CE2): a COBOL-85 comment-entry paragraph is decided on the division the COMPILED
/// text stands in. The §6.5 conversion runs before conditional compilation, COPY, REPLACE and the <c>&gt;&gt;COBOL-WORDS</c>
/// stage, so where its own reading of the division is not certain (a conditional group whose branches disagree, a
/// <c>&gt;&gt;COBOL-WORDS</c> directive) it CARRIES the candidate (<see cref="CommentEntryCarrier"/>) and
/// <see cref="CommentEntryResolver"/> decides it after those stages. These tests drive the stages in their order:
/// conversion, conditional compilation, resolver.
/// </summary>
public sealed class CommentEntryResolverTests
{
    /// <summary>The finished text of a FREE-form <paramref name="source"/>: converted, conditional compilation applied,
    /// comment-entry candidates decided through <paramref name="words"/>.</summary>
    private static string[] Finished(string source, CobolWordsMap? words = null, DiagnosticBag? bag = null)
    {
        var gates = new ReferenceFormatDiagnostics(2023, true, bag ?? new DiagnosticBag());
        var converted = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, gates, "t.cob",
            initialFixed: false, out _);
        string compiled = ConditionalCompilationProcessor.Process(converted.Text, LeftDirectives, dialectLevel: 2023, permissive: true);
        var resolved = CommentEntryResolver.Resolve(new MappedText(compiled, converted.Lines), words ?? CobolWordsMap.Empty, gates);
        return resolved.Text.Split('\n');
    }

    private static readonly IReadOnlySet<string> LeftDirectives = WiseOwl.Cobol.Frontend.Frontend.LeftDirectives;

    private static string[] Converted(string source, bool fixedForm = false)
        => ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, null, "t.cob", initialFixed: fixedForm, out _).Text.Split('\n');

    [Fact] // the case the conversion is sure of is decided there, as before: agreeing branches leave no carrier
    public void AgreeingBranches_AreDecidedByTheConversion_NoCarrier()
    {
        string[] lines = Converted("ID DIVISION.\n>>IF X IS DEFINED\nPROGRAM-ID. P.\n>>ELSE\nPROGRAM-ID. P.\n>>END-IF\nAUTHOR. Pat O'Brien.\n");
        Assert.Equal("AUTHOR. .", lines[6]);
        Assert.DoesNotContain(lines, CommentEntryCarrier.Is);
    }

    [Fact] // disagreeing branches: the conversion cannot tell, so it carries the candidate, its entry text with it
    public void DisagreeingBranches_CarryTheCandidate()
    {
        string[] lines = Converted("ID DIVISION.\n>>IF X IS DEFINED\nPROGRAM-ID. P.\n>>ELSE\nPROCEDURE DIVISION.\n>>END-IF\nAUTHOR. Pat O'Brien.\n");
        Assert.True(CommentEntryCarrier.Is(lines[6]));
        var carried = CommentEntryCarrier.Decode(lines[6]);
        Assert.True(carried.IsHeader);
        Assert.False(carried.IsFixed);
        Assert.Equal("AUTHOR. Pat O'Brien.", carried.Line.Text);
        Assert.Equal(7, carried.Line.Number);
    }

    [Fact] // PB4281 item 1: the SELECTED branch enters the identification division, so the paragraph is a comment-entry
    public void SelectedBranchEnteringTheIdentificationDivision_MakesTheCandidateACommentEntry()
    {
        string[] lines = Finished(">>DEFINE X AS 1\nIDENTIFICATION DIVISION.\n>>IF X IS DEFINED\nPROGRAM-ID. P.\n>>ELSE\nPROCEDURE DIVISION.\n>>END-IF\n"
            + "AUTHOR. Pat O'Brien.\nPROCEDURE DIVISION.\n");
        Assert.Contains("AUTHOR. .", lines);            // the header, the apostrophe of the entry discarded
        Assert.DoesNotContain(lines, l => l.Contains("Brien", StringComparison.Ordinal));
    }

    [Fact] // … and the branch that leaves it makes the same words a paragraph of the program
    public void SelectedBranchLeavingTheIdentificationDivision_MakesTheCandidateAParagraph()
    {
        string[] lines = Finished("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>IF X IS DEFINED\nID DIVISION.\n>>ELSE\nPROCEDURE DIVISION.\n>>END-IF\n"
            + "REMARKS. DISPLAY \"HI\".\n");
        Assert.Contains("REMARKS. DISPLAY \"HI\".", lines);
        Assert.DoesNotContain(lines, CommentEntryCarrier.Is);
    }

    [Fact] // PB4281 item 3: DIVISION respelled by >>COBOL-WORDS — the converter cannot read DIV, so it carries; the map decides
    public void RespelledDivisionWord_IsReadThroughTheWordMap()
    {
        const string source = ">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"\nIDENTIFICATION DIV.\nPROGRAM-ID. P.\nPROCEDURE DIV.\nREMARKS. DISPLAY \"HI\".\n";
        string[] converted = Converted(source);
        Assert.True(CommentEntryCarrier.Is(converted[4]));
        var map = CobolWordsMap.FromDirectiveText(">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"\n")!;
        string[] lines = Finished(source, map);
        Assert.Contains("REMARKS. DISPLAY \"HI\".", lines);   // PROCEDURE DIV left the identification division
    }

    [Fact] // … and the same synonym keeps a real comment-entry a comment-entry
    public void RespelledDivisionWord_KeepsACommentEntryInTheIdentificationDivision()
    {
        const string source = ">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"\nIDENTIFICATION DIV.\nPROGRAM-ID. P.\nAUTHOR. Pat O'Brien.\nPROCEDURE DIV.\n";
        var map = CobolWordsMap.FromDirectiveText(">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"\n")!;
        string[] lines = Finished(source, map);
        Assert.Contains("AUTHOR. .", lines);
        Assert.DoesNotContain(lines, l => l.Contains("Brien", StringComparison.Ordinal));
    }

    /// <summary>The finished text of a FIXED-form <paramref name="source"/> (see <see cref="Finished"/>).</summary>
    private static (string[] Converted, string[] Lines, int ConvertedCount) FinishedFixed(string source)
    {
        var gates = new ReferenceFormatDiagnostics(85, true, new DiagnosticBag());
        var converted = ReferenceFormatProcessor.NormalizeToFreeFormMapped(source, gates, "t.cob", initialFixed: true, out _);
        string compiled = ConditionalCompilationProcessor.Process(converted.Text, LeftDirectives, dialectLevel: 85, permissive: true);
        var resolved = CommentEntryResolver.Resolve(new MappedText(compiled, converted.Lines), CobolWordsMap.Empty, gates);
        Assert.Equal(converted.Lines.Length, resolved.Lines.Length);
        return (converted.Text.Split('\n'), resolved.Text.Split('\n'), converted.Lines.Length);
    }

    private const string FixedGroup = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P.\n       >>IF X IS DEFINED\n       ID DIVISION.\n"
        + "       >>ELSE\n       PROCEDURE DIVISION.\n       >>END-IF\n";

    [Fact] // a fixed-form candidate is a header and the Area-B lines of its entry (a continuation line included): decided together
    public void FixedFormCandidate_ThatIsACommentEntry_IsDiscardedWithItsEntry()
    {
        var (converted, lines, _) = FinishedFixed("       >>DEFINE X AS 1\n" + FixedGroup
            + "       AUTHOR. Pat O'Brien,\n           AND A \"SECOND LINE\n      -    OF IT.\n       INSTALLATION. SOMEWHERE.\n");
        Assert.Equal(5, converted.Count(CommentEntryCarrier.Is));   // AUTHOR, its two further lines, INSTALLATION, and the blank line after the text's last newline
        Assert.Contains("AUTHOR. .", lines.Select(l => l.Trim()));
        Assert.Contains("INSTALLATION. .", lines.Select(l => l.Trim()));
        Assert.DoesNotContain(lines, l => l.Contains("Brien", StringComparison.Ordinal) || l.Contains("SECOND", StringComparison.Ordinal));
    }

    [Fact] // … and the same lines are the program's own where the division is none: converted as the ordinary lines they are
    public void FixedFormCandidate_ThatIsProgramText_IsConvertedLikeAnyOtherLine()
    {
        var (_, lines, _) = FinishedFixed(FixedGroup
            + "       REMARKS. DISPLAY \"A\".\n           DISPLAY \"B\"\n      -    .\n       INSTALLATION. DISPLAY \"C\".\n");
        string[] trimmed = [.. lines.Select(l => l.Trim())];
        Assert.Contains("REMARKS. DISPLAY \"A\".", trimmed);
        Assert.Contains("DISPLAY \"B\".", trimmed);   // the continuation line joined the line before it (§6.5 6)), its slot left empty
        Assert.Contains("INSTALLATION. DISPLAY \"C\".", trimmed);
        Assert.DoesNotContain(lines, CommentEntryCarrier.Is);
    }

    [Fact] // a text with no carrier is the very text it was given
    public void NoCarrier_PassesThroughUntouched()
    {
        var text = MappedText.Identity("PROGRAM-ID. P.\nPROCEDURE DIVISION.\n", "t.cob");
        Assert.Same(text, CommentEntryResolver.Resolve(text, CobolWordsMap.Empty, null));
    }

    [Fact] // the carrier keeps a line's tab-expanded text and its column map, so a diagnostic names the column the user edits
    public void Carrier_RoundTripsALineAndItsColumnMap()
    {
        var physical = PhysicalLines.Read("\tAUTHOR. X\n", ccvsIndicators: false).Lines[0];
        Assert.NotNull(physical.Columns);
        var back = CommentEntryCarrier.Decode(CommentEntryCarrier.Encode(isHeader: true, isFixed: true, ccvsIndicators: true, physical));
        Assert.True(back.IsHeader);
        Assert.True(back.IsFixed);
        Assert.True(back.CcvsIndicators);
        Assert.Equal(physical.Text, back.Line.Text);
        Assert.Equal(physical.Number, back.Line.Number);
        for (int i = 0; i <= physical.Text.Length + 2; i++)
            Assert.Equal(PhysicalLine.ColumnOf(physical.Columns, i), PhysicalLine.ColumnOf(back.Line.Columns, i));
    }

    [Theory] // a stage that damaged a carrier broke the contract: the compilation stops, it does not guess
    [InlineData("*>")]
    [InlineData("HF0x;;text")]
    [InlineData("XF0;;text")]
    [InlineData("HF07")]
    public void DamagedCarrier_Throws(string damage)
    {
        string line = CommentEntryCarrier.Prefix + damage;
        var text = MappedText.Identity(line, "t.cob");
        Assert.Throws<InvalidOperationException>(() => CommentEntryResolver.Resolve(text, CobolWordsMap.Empty, null));
    }

    [Fact] // a free-form candidate that is program text is read through the compilation's reference-format diagnostics
    public void CandidateThatIsProgramText_ReportsThroughTheGates()
    {
        var bag = new DiagnosticBag();
        string[] lines = Finished("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>IF X IS DEFINED\nID DIVISION.\n>>ELSE\nPROCEDURE DIVISION.\n>>END-IF\n"
            + "REMARKS. DISPLAY \"A\" *> trailing comment\n", bag: bag);
        Assert.Contains("REMARKS. DISPLAY \"A\"", lines.Select(l => l.TrimEnd()));   // the inline comment was removed, §6.5 3)
    }
}
