// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB2739: "which division is this text in" is ONE rule (<see cref="DivisionCursor"/>), read off text-words wherever
/// they begin (ISO §6.3.1: the program-text area has no Area A / Area B line), shared by the fixed-form converter's line
/// walk and the COPY driver's walk over logical text.
/// </summary>
public sealed class DivisionCursorTests
{
    private static string[] Convert(string fixedFormText, bool startsInIdentification = true)
        => ReferenceFormatProcessor.NormalizeToFreeFormMapped(fixedFormText, null, "t.cob", initialFixed: true, out _,
            startsInIdentificationDivision: startsInIdentification).Text.Split('\n');

    [Theory] // a division header is a division header in Area B too (column 12), and so is one split across two lines
    [InlineData("000300     PROCEDURE DIVISION.\n")]
    [InlineData("000300     PROCEDURE\n000310         DIVISION.\n")]
    [InlineData("000300 procedure division.\n")]
    public void DivisionHeader_AnywhereInTheProgramTextArea_LeavesTheIdentificationDivision(string header)
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n" + header
                                 + "000400 REMARKS.\n000500     DISPLAY \"X\".\n");
        Assert.Contains(lines, l => l.Trim() == "REMARKS.");        // an ordinary paragraph, header untouched
        Assert.Contains(lines, l => l.Trim() == "DISPLAY \"X\".");   // its statement was not discarded as a comment-entry
    }

    [Fact] // the header's own spelling does not matter: ID DIVISION re-enters, so a comment-entry paragraph is one again
    public void IdDivisionHeader_AnywhereInTheProgramTextArea_EntersTheIdentificationDivision()
    {
        string[] lines = Convert("000100     PROCEDURE DIVISION.\n000200       ID DIVISION.\n000300 REMARKS. TEXT\n000400     MORE TEXT\n");
        Assert.Equal("REMARKS. .", lines[2]);
        Assert.Equal("", lines[3]);
    }

    [Theory] // §11.2.1: the header is optional, so the unit's first paragraph enters the division — a word that merely
             // ENDS in -ID is a user's name (CUSTOMER-ID), not that paragraph
    [InlineData("MOVE 2 TO CUSTOMER-ID.", false)]
    [InlineData("PROGRAM-ID. P.", true)]
    [InlineData("CLASS-ID. C.", true)]
    [InlineData("METHOD-ID. M.", true)]
    [InlineData("FUNCTION-ID. F.", true)]
    [InlineData("INTERFACE-ID. I.", true)]
    public void OnlyAnIdentificationParagraph_EntersTheDivision_NotAWordEndingInId(string statement, bool enters)
    {
        string[] lines = Convert("000100 PROCEDURE DIVISION.\n000200     " + statement + "\n000300 REMARKS. TEXT\n000400     MORE TEXT\n");
        if (enters)
        {
            Assert.Equal("REMARKS. .", lines[2]);
            Assert.Equal("", lines[3]);
        }
        else
        {
            Assert.Equal("REMARKS. TEXT", lines[2].Trim());
            Assert.Equal("MORE TEXT", lines[3].Trim());
        }
    }

    [Fact] // the free text of a comment-entry is commentary, not words of the program: a DIVISION named in it moves nothing
    public void CommentEntryBody_NamingADivision_DoesNotLeaveTheIdentificationDivision()
    {
        string[] lines = Convert("000100 AUTHOR. THE PROCEDURE DIVISION IS FAR AWAY\n000200     THE DATA DIVISION TOO\n000300 REMARKS. MORE\n");
        Assert.Equal("AUTHOR. .", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("REMARKS. .", lines[2]);
    }

    [Fact] // a literal holding a division header is a literal, not a header
    public void DivisionHeader_InsideALiteral_IsNotOne()
    {
        string[] lines = Convert("000100     DISPLAY \"PROCEDURE DIVISION.\".\n000200 REMARKS. TEXT\n");
        Assert.Equal("REMARKS. .", lines[1]);
    }

    [Fact] // the cursor carries across the blocks of one text: a COPY after a directive line is judged by the text before it
    public void Cursor_CarriesItsStateAcrossTheBlocksOfOneText()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        string block1 = "PROCEDURE DIVISION.\nMAIN-PARA.";
        Assert.False(cursor.InIdentificationDivisionAt(block1, block1.Length));
        cursor.Finish(block1);
        string block2 = "PERFORM X.\nCOPY BOOK.";
        Assert.False(cursor.InIdentificationDivisionAt(block2, block2.IndexOf("COPY", StringComparison.Ordinal)));
    }

    [Theory] // the operands of a COPY or REPLACE statement are not text that stands in a division (kb/Work PB4241): the
             // pseudo-text names a division; the text did not. The statement runs to its separator period outside the
             // pseudo-text (a period inside it ends nothing), across lines
    [InlineData("COPY BOOK REPLACING ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.")]
    [InlineData("COPY BOOK\n    REPLACING ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.")]
    [InlineData("copy book replacing ==A== by ==X. PROCEDURE DIVISION== .\nREMARKS.")]
    [InlineData("REPLACE ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.")]
    [InlineData("COPY BOOK REPLACING DIVISION BY X.\nREMARKS.")]
    public void CopyOrReplaceStatement_IsNotReadAsText(string text)
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true, CobolWordsMap.Empty);   // the finished text's reader
        Assert.True(cursor.InIdentificationDivisionAt(text, text.Length));
    }

    [Theory] // kb/Work PB4281: … but a reader of the text AS WRITTEN knows a replacing action can write or take away the very
             // words it reads (§7.2.4.4 2)), so a statement that names one makes the division unknown
    [InlineData("COPY BOOK REPLACING ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.")]
    [InlineData("COPY BOOK\n    REPLACING ==A== BY ==PROCEDURE DIVISION==.\nREMARKS.")]
    [InlineData("REPLACE ==PROCEDURE DIVISION== BY ==X==.\nREMARKS.")]
    [InlineData("COPY BOOK REPLACING DIVISION BY X.\nREMARKS.")]
    [InlineData("REPLACE ==ID== BY ==XX==.\nREMARKS.")]
    [InlineData("REPLACE ==A== BY ==PROGRAM-ID. P.==.\nREMARKS.")]
    public void CopyOrReplaceStatement_NamingAWordTheCursorReads_MakesTheDivisionUnknown(string text)
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        Assert.Null(cursor.InIdentificationDivisionAt(text, text.Length));
    }

    [Theory] // … and one that names none moves nothing and costs nothing
    [InlineData("COPY BOOK REPLACING ==A== BY ==B==.\nREMARKS.")]
    [InlineData("REPLACE ==:PFX:== BY ==WS==.\nREMARKS.")]
    [InlineData("COPY CUSTOMER-ID.\nREMARKS.")]
    public void CopyOrReplaceStatement_NamingNoSuchWord_MovesNothing(string text)
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        Assert.True(cursor.InIdentificationDivisionAt(text, text.Length));
    }

    [Fact] // the statement ends at its period: the text after it is read again
    public void TextAfterACopyStatement_IsRead()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        Assert.False(cursor.InIdentificationDivisionAt("COPY BOOK. PROCEDURE DIVISION.", 30));
    }

    [Fact] // line by line, as the converters feed it: the state of a statement carries across lines
    public void CopyStatement_CarriesAcrossLines()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        cursor.Read("COPY BOOK");
        cursor.Read("REPLACING ==A== BY ==X==.");
        Assert.True(cursor.InIdentificationDivision);
        cursor.Read("PROCEDURE DIVISION.");
        Assert.False(cursor.InIdentificationDivision);
        var naming = new DivisionCursor(startsInIdentificationDivision: true);
        naming.Read("COPY BOOK");
        naming.Read("REPLACING ==PROCEDURE DIVISION== BY ==X==.");
        Assert.Null(naming.InIdentificationDivision);
    }

    [Fact] // the library text's own end state is adopted
    public void Adopt_TakesTheLibraryTextsDivision()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        var library = new DivisionCursor(startsInIdentificationDivision: true);
        library.Finish("PROCEDURE DIVISION.");
        cursor.Adopt(library);
        Assert.False(cursor.InIdentificationDivision);
    }

    [Fact] // §6.5: a header continued onto a second line (fixed form: hyphen in column 7) is ONE word once joined, so it
           // leaves the division the same as one written whole; each half read alone would not
    public void ContinuedDivisionHeader_LeavesTheIdentificationDivision()
    {
        const string header = "000300     PROCEDURE DIVI\n000310-    SION.\n";
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n" + header
                                 + "000400 REMARKS.\n000500     DISPLAY \"X\".\n");
        Assert.Contains(lines, l => l.Trim() == "REMARKS.");
        Assert.Contains(lines, l => l.Trim() == "DISPLAY \"X\".");
    }

    [Theory] // R1-1: a blank line or a comment line is logically discarded (§6.5 2)) and the continuation line after it still
             // joins the latest logical line (§6.5 6) b)), so the cursor must not read that line before the join
    [InlineData("")]
    [InlineData("      ")]
    [InlineData("      *> a floating comment line")]
    [InlineData("000350*  a fixed comment line")]
    public void ContinuedDivisionHeader_AcrossADiscardedLine_LeavesTheIdentificationDivision(string between)
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n000300     PROCEDURE DIVI\n"
                                 + between + "\n000400-    SION.\n000500 REMARKS.\n000600     DISPLAY \"X\".\n");
        Assert.Contains(lines, l => l.Trim() == "REMARKS.");
        Assert.Contains(lines, l => l.Trim() == "DISPLAY \"X\".");
    }

    [Fact] // a COPY statement's REPLACING operand in the SOURCE text, read by the converter, moves nothing: it makes the division
           // unknown (a replacing action can rewrite the words the cursor reads), the paragraphs after it are carried, and the
           // finished text — the statement passed over — says they stand in the IDENTIFICATION DIVISION
    public void CopyReplacingOperandNamingADivision_InTheSourceText_LeavesTheDivisionAlone()
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n"
                                 + "000300 COPY BOOK REPLACING ==PROCEDURE DIVISION== BY ==X==.\n"
                                 + "000400 INSTALLATION. \"UNCLOSED HERE.\n000500 SECURITY. MORE\n");
        Assert.True(CommentEntryCarrier.Is(lines[3]));
        Assert.True(CommentEntryCarrier.Is(lines[4]));
        string[] finished = CommentEntryResolver.Resolve(MappedText.Identity(string.Join('\n', lines), "t.cob"),
            CobolWordsMap.Empty, null).Text.Text.Split('\n');
        Assert.Equal("INSTALLATION. .", finished[3]);
        Assert.Equal("SECURITY. .", finished[4]);
    }

    [Theory] // kb/Work PB4241, PB4281: §6.5 converts BEFORE conditional compilation, so which branch of a >>IF the text keeps is
             // not known to the converter. A division word in a group's branches counts only when every branch (the one no
             // condition selects included, when there is no >>ELSE or >>WHEN OTHER) ends in the same division; otherwise
             // which division the text is in depends on a condition and the cursor says NOT KNOWN (null): the converter
             // carries a comment-entry candidate it cannot place (CommentEntryCarrier) for CommentEntryResolver to decide.
             // Lines are separated by '|'
    [InlineData(false, ">>IF X IS DEFINED|IDENTIFICATION DIVISION.|>>END-IF", null)]   // the omitted branch enters: it may be the selected one
    [InlineData(true, ">>IF X IS DEFINED|PROCEDURE DIVISION.|>>END-IF", null)]         // … or leaves: the SELECTED branch may be it (R1-3)
    [InlineData(true, ">>IF X|PROCEDURE DIVISION USING A.|>>ELSE|PROCEDURE DIVISION.|>>END-IF", false)]   // both leave
    [InlineData(false, ">>IF X|ID DIVISION.|>>ELSE|IDENTIFICATION DIVISION.|>>END-IF", true)]            // both enter
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|PROCEDURE DIVISION.|>>WHEN OTHER|PROCEDURE DIVISION.|>>END-EVALUATE", false)]
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|PROCEDURE DIVISION.|>>WHEN 2|PROCEDURE DIVISION.|>>END-EVALUATE", null)]   // no OTHER
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|PROCEDURE DIVISION.|>>WHEN OTHERV|PROCEDURE DIVISION.|>>END-EVALUATE", null)]   // OTHERV is no default (R1-2)
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|IDENTIFICATION DIVISION.|>>WHEN OTHER|ID DIVISION.|>>END-EVALUATE", true)]   // all agree, OTHER is the default
    [InlineData(false, ">>IF A|>>IF B|IDENTIFICATION DIVISION.|>>END-IF|>>ELSE|IDENTIFICATION DIVISION.|>>END-IF", null)]
    [InlineData(true, ">>END-IF|>>ELSE|PROCEDURE DIVISION.", false)]   // a phrase with no group open moves nothing
    [InlineData(true, ">>IF X|PROCEDURE DIVISION.|>>ELSE|ID DIVISION.|>>END-IF|PROCEDURE DIVISION.", false)]   // a division word after the group settles it again
    public void ConditionalGroup_CountsOnlyWhenEveryBranchAgrees(bool startsInIdentification, string lines, bool? expected)
    {
        var cursor = new DivisionCursor(startsInIdentification);
        foreach (string line in lines.Split('|')) cursor.Read(line);
        Assert.Equal(expected, cursor.InIdentificationDivision);
    }

    [Fact] // kb/Work PB4281: a >>COBOL-WORDS directive may respell DIVISION (or any word the cursor reads), which a reader of the
           // words AS WRITTEN cannot learn: from there on it knows no division, whatever it reads
    public void CobolWordsDirective_MakesTheDivisionUnknown_ForAReaderOfTheWordsAsWritten()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        cursor.Read(">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"");
        Assert.Null(cursor.InIdentificationDivision);
        cursor.Read("PROCEDURE DIV.");
        Assert.Null(cursor.InIdentificationDivision);
        cursor.Read("PROCEDURE DIVISION.");
        Assert.Null(cursor.InIdentificationDivision);   // a spelling it does read cannot make it sure: it cannot tell the others
    }

    [Theory] // kb/Work PB4281 (review finding R1-1): only a directive that names a word the cursor reads (§7.3.10.2: an option keyword
             // and its literals) leaves the division unknown, as a REPLACE statement that names one does; the rest changes nothing it reads
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOW\"", false)]
    [InlineData("RESERVE \"ZQX\"", false)]
    [InlineData("UNDEFINE \"STOP\"", false)]
    [InlineData("SUBSTITUTE \"COUNT\" BY \"TALLY-IT\"", false)]
    [InlineData("EQUATE \"DISPLAY\", WITH \"SHOW\"", false)]               // a separator comma is a space (§8.3.5 2)
    [InlineData("EQUATE X\"41\" WITH \"SHOW\"", false)]                      // a literal's prefix is no name
    [InlineData("EQUATE \"DIVISION\" WITH \"DIV\"", true)]                   // the word to respell
    [InlineData("EQUATE \"DIV\" WITH \"division\"", true)]                   // the word written, in any letter case
    [InlineData("EQUATE \"IDENT\" WITH \"IDENTIFICATION\"", true)]
    [InlineData("EQUATE \"ID\" WITH \"IDENT\"", true)]
    [InlineData("UNDEFINE \"PROGRAM-ID\"", true)]                            // the paragraph that begins an identification division
    [InlineData("EQUATE LITERAL-VAR WITH \"SHOW\"", true)]                   // a compilation-variable-name: its value is not known here (§7.3.11.4 1))
    public void CobolWordsDirective_MakesTheDivisionUnknown_OnlyWhenItNamesAWordTheCursorReads(string operand, bool unknown)
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: false);
        cursor.Read(">>COBOL-WORDS " + operand);
        Assert.Equal(unknown, cursor.InIdentificationDivision is null);
        if (!unknown) Assert.False(cursor.InIdentificationDivision);
    }

    [Fact] // the merged driver's blocks end at a directive line, so the driver tells its cursor of the directive itself
    public void NoteCobolWordsDirective_MakesTheDivisionUnknown_ButNotForAReaderOfTheFinishedText()
    {
        var driver = new DivisionCursor(startsInIdentificationDivision: true);
        driver.NoteCobolWordsDirective("EQUATE \"DIVISION\" WITH \"DIV\"");
        Assert.Null(driver.InIdentificationDivision);
        var finished = new DivisionCursor(startsInIdentificationDivision: true, CobolWordsMap.Empty);
        finished.NoteCobolWordsDirective("EQUATE \"DIVISION\" WITH \"DIV\"");
        Assert.True(finished.InIdentificationDivision);
    }

    [Fact] // … and a library text that held the directive casts the same doubt on the text it was spliced into
    public void Adopt_TakesTheLibraryTextsDoubtAboutRespelledWords()
    {
        var cursor = new DivisionCursor(startsInIdentificationDivision: true);
        var library = new DivisionCursor(startsInIdentificationDivision: true);
        library.Read(">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"");
        cursor.Adopt(library);
        cursor.Read("PROCEDURE DIVISION.");
        Assert.Null(cursor.InIdentificationDivision);
    }

    [Theory] // kb/Work PB4281: given the group's word map the cursor reads the finished text as the lexer will (ISO §7.3.10.4 GR2:
             // a synonym is used wherever the word it stands for is; GR4: a substituted word is a user word)
    [InlineData("EQUATE \"DIVISION\" WITH \"DIV\"", true, "PROCEDURE DIV.", false)]
    [InlineData("EQUATE \"IDENTIFICATION\" WITH \"IDENT\"", false, "IDENT DIVISION.", true)]
    [InlineData("SUBSTITUTE \"DIVISION\" BY \"DIV\"", true, "PROCEDURE DIVISION.", true)]   // DIVISION is a user word now: it moves nothing
    [InlineData("SUBSTITUTE \"DIVISION\" BY \"DIV\"", true, "PROCEDURE DIV.", false)]
    public void GivenTheGroupsWordMap_TheCursorReadsRespelledWords(string directive, bool startsInIdentification, string line, bool expected)
    {
        var map = CobolWordsMap.FromDirectiveText(">>COBOL-WORDS " + directive + "\n")!;
        var cursor = new DivisionCursor(startsInIdentification, map);
        cursor.Read(line);
        Assert.Equal(expected, cursor.InIdentificationDivision);
    }

    [Fact] // the omitted branch of a >>IF spells IDENTIFICATION DIVISION: which division the text is in after >>END-IF depends on
           // the condition, so the converter CARRIES the REMARKS paragraph it cannot place — and CommentEntryResolver, on the
           // compiled text (the branch is gone), finds a paragraph of the PROCEDURE DIVISION (kb/Work PB4241, PB4281)
    public void OmittedBranchSpellingAnIdentificationDivision_DoesNotMakeLaterRemarksAComment()
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n000300 PROCEDURE DIVISION.\n"
                                 + "000400 >>IF NEVER IS DEFINED\n000500 IDENTIFICATION DIVISION.\n000600 >>END-IF\n"
                                 + "000700 REMARKS.\n000800     DISPLAY \"X\".\n");
        Assert.True(CommentEntryCarrier.Is(lines[6]));   // REMARKS.
        Assert.True(CommentEntryCarrier.Is(lines[7]));   // DISPLAY "X". — a line of the candidate until it is decided
        string compiled = string.Join('\n', lines.Select((l, i) => i is 3 or 4 or 5 ? "" : l));   // conditional compilation omits the branch
        var resolved = CommentEntryResolver.Resolve(MappedText.Identity(compiled, "t.cob"), CobolWordsMap.Empty, null);
        string[] finished = resolved.Text.Text.Split('\n');
        Assert.Contains(finished, l => l.Trim() == "REMARKS.");
        Assert.Contains(finished, l => l.Trim() == "DISPLAY \"X\".");
    }
}
