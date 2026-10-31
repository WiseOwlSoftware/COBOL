// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
        cursor.Read("REPLACING ==PROCEDURE DIVISION== BY ==X==.");
        Assert.True(cursor.InIdentificationDivision);
        cursor.Read("PROCEDURE DIVISION.");
        Assert.False(cursor.InIdentificationDivision);
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

    [Fact] // a COPY statement's REPLACING operand in the SOURCE text, read by the converter, moves nothing
    public void CopyReplacingOperandNamingADivision_InTheSourceText_LeavesTheDivisionAlone()
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n"
                                 + "000300 COPY BOOK REPLACING ==PROCEDURE DIVISION== BY ==X==.\n"
                                 + "000400 INSTALLATION. \"UNCLOSED HERE.\n000500 SECURITY. MORE\n");
        Assert.Equal("INSTALLATION. .", lines[3]);
        Assert.Equal("SECURITY. .", lines[4]);
    }

    [Theory] // kb/Work PB4241: §6.5 converts BEFORE conditional compilation, so which branch of a >>IF the text keeps is not
             // known to the converter. A division word in a group's branches counts only when every branch (the one no
             // condition selects included, when there is no >>ELSE or >>WHEN OTHER) ends in the same division; otherwise the
             // text is in the division it was in before the group. Lines are separated by '|'
    [InlineData(false, ">>IF X IS DEFINED|IDENTIFICATION DIVISION.|>>END-IF", false)]   // the omitted branch enters: nothing moves
    [InlineData(true, ">>IF X IS DEFINED|PROCEDURE DIVISION.|>>END-IF", true)]          // … or leaves
    [InlineData(true, ">>IF X|PROCEDURE DIVISION USING A.|>>ELSE|PROCEDURE DIVISION.|>>END-IF", false)]   // both leave
    [InlineData(false, ">>IF X|ID DIVISION.|>>ELSE|IDENTIFICATION DIVISION.|>>END-IF", true)]            // both enter
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|PROCEDURE DIVISION.|>>WHEN OTHER|PROCEDURE DIVISION.|>>END-EVALUATE", false)]
    [InlineData(true, ">>EVALUATE X|>>WHEN 1|PROCEDURE DIVISION.|>>WHEN 2|PROCEDURE DIVISION.|>>END-EVALUATE", true)]   // no OTHER
    [InlineData(false, ">>IF A|>>IF B|IDENTIFICATION DIVISION.|>>END-IF|>>ELSE|IDENTIFICATION DIVISION.|>>END-IF", false)]
    [InlineData(true, ">>END-IF|>>ELSE|PROCEDURE DIVISION.", false)]   // a phrase with no group open moves nothing
    public void ConditionalGroup_CountsOnlyWhenEveryBranchAgrees(bool startsInIdentification, string lines, bool expected)
    {
        var cursor = new DivisionCursor(startsInIdentification);
        foreach (string line in lines.Split('|')) cursor.Read(line);
        Assert.Equal(expected, cursor.InIdentificationDivision);
    }

    [Fact] // the omitted branch of a >>IF spells IDENTIFICATION DIVISION: the REMARKS paragraph after >>END-IF is a paragraph
    public void OmittedBranchSpellingAnIdentificationDivision_DoesNotMakeLaterRemarksAComment()
    {
        string[] lines = Convert("000100 IDENTIFICATION DIVISION.\n000200 PROGRAM-ID. P.\n000300 PROCEDURE DIVISION.\n"
                                 + "000400 >>IF NEVER IS DEFINED\n000500 IDENTIFICATION DIVISION.\n000600 >>END-IF\n"
                                 + "000700 REMARKS.\n000800     DISPLAY \"X\".\n");
        Assert.Contains(lines, l => l.Trim() == "REMARKS.");
        Assert.Contains(lines, l => l.Trim() == "DISPLAY \"X\".");
    }
}
