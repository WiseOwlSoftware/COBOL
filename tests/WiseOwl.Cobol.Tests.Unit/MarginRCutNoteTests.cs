// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SYNTAX ERROR BESIDE A MARGIN-R CUT NAMES THE CUT (kb/Work PB2555; ISO §6.3.1 "Margin R is immediately to the right of
/// the rightmost character position of the program-text area. The rightmost character position of the program-text area is
/// a fixed position defined by the implementor", Annex A.1 item 158: position 72 here). Text past margin R is not program
/// text, so the reference format drops it, and a statement that ran past the margin then ends early: the parse fails at the
/// NEXT token, or at a word the margin cut in two, and nothing pointed at the truncation. COBOL5788 is that pointer. It is a
/// warning reported BESIDE the error, only where the cut may have been program text: a file's identification tags and a
/// numeric sequence area are the ordinary use of positions 73–80, an inline comment (§6.3.7.3: "All characters following the
/// floating comment indicator up to margin R are comment-text") and a comment line run on past the margin by definition.
/// Every position below is COMPUTED from the layout the test writes (position 72 is the last the program-text area holds),
/// never read off a run.
/// </summary>
public sealed class MarginRCutNoteTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_MarginR_");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private const string NoteCode = "COBOL5788";

    private static readonly string[] Head =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. MARGINR.",
        "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.",
        "       01 WS-MSG PIC X(8) VALUE \"MSG\".",
        "       01 WS-REPLY PIC X(8).",
        "       PROCEDURE DIVISION.",
        "       MAIN-P.",
    ];

    /// <summary>A CALL whose <c>BY</c> ends at position 71, so the statement is complete only if <c>CONTENT WS-REPLY</c>,
    /// written from position 73, is read: the line is cut between two words.</summary>
    private static readonly string CutBetweenWords = "           CALL \"SUBPROG\" USING WS-MSG".PadRight(69) + "BY CONTENT WS-REPLY";

    /// <summary><c>END-SEND</c> begins at position 66, so margin R falls after its seventh character: the program text ends
    /// in <c>END-SEN</c> and <c>D.</c> lies past it.</summary>
    private static readonly string CutInsideWord = "           SEND TO LINAGE-COUNTER FROM WS-MSG RETURNING WS-REPLY".PadRight(65) + "END-SEND.";

    private static string Program(IEnumerable<string> lines) => string.Join('\n', lines) + '\n';

    /// <summary>Every diagnostic the front end reports for <paramref name="source"/>, in the order it reported them.</summary>
    private List<string> Compile(string source, InitialReferenceFormat format = InitialReferenceFormat.Fixed)
    {
        string path = Path.Combine(_dir, "main.cob");
        File.WriteAllText(path, source);
        var frontend = new CnFrontend { InitialFormat = format };
        frontend.AddCopySearchPath(_dir);
        var bag = new DiagnosticBag();
        frontend.Parse(path, bag);
        return [.. bag.Diagnostics.Select(d => d.ToString())];
    }

    private static List<string> Notes(List<string> diagnostics) => diagnostics.Where(d => d.Contains(NoteCode)).ToList();

    private static void AssertStopRunIsTheErrorAfterTheCut(List<string> diagnostics)
    {
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(10,12)") && d.Contains("'STOP'"));
    }

    [Fact] // a statement cut between two words: the parse fails at STOP, and the note names the cut at position 73 of line 9
    public void CutBetweenWords_IsNamedAtPosition73OfItsLine()
    {
        var diagnostics = Compile(Program([.. Head, CutBetweenWords, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(9,73): warning", note);                    // the first position past the program-text area
        Assert.Contains("it ends at position 72", note);
        Assert.Contains("'CONTENT WS-REPLY' (positions 73 to 88)", note);   // 16 characters from position 73
        Assert.Contains("main.cob(10,12) may be this", note);                 // where STOP stands: line 10, position 12
        AssertStopRunIsTheErrorAfterTheCut(diagnostics);
    }

    [Fact] // PB4373: a tab states margin R as the POSITION the format fixes (72), not the column the file holds it at
    public void ALineHoldingATab_StatesMarginRAtPosition72_AndTheColumnTheFileHoldsIt()
    {
        // The tab fills positions 1-8 (DOC-A.1-157: tab stops at 1, 9, 17, ...), so the line is seven characters shorter than
        // its positions: position 73 is the 66th character of the line (column 66), the margin the format fixes is still 72.
        string tabbed = "\t" + CutBetweenWords[8..];
        var diagnostics = Compile(Program([.. Head, tabbed, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(9,66): warning", note);                                  // the column the file holds position 73 at
        Assert.Contains("it ends at position 72 (a tab counts the positions it fills, so the column this warning names, where the file holds the cut, is less than that)", note);
        Assert.Contains("'CONTENT WS-REPLY' (positions 73 to 88)", note);                   // positions, not columns 66 to 81
        Assert.Contains("Shorten the line to end at position 72", note);
        AssertStopRunIsTheErrorAfterTheCut(diagnostics);
    }

    [Fact] // PB4373: a supplementary-plane character is ONE position (kb/Work PB1966), so the cut text counts characters
    public void ACutHoldingASupplementaryCharacter_CountsItsPositionsInCharacters()
    {
        string cut = CutBetweenWords[..72] + "CONTENT WS-\U0001E900";                      // 12 characters from position 73, 13 UTF-16 units
        var diagnostics = Compile(Program([.. Head, cut, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(9,73): warning", note);
        Assert.Contains("(positions 73 to 84)", note);
    }

    [Fact] // PB4373: the quoted cut is shortened at a CHARACTER, never between the two units of a supplementary one
    public void AQuotedCutShortenedAtASupplementaryCharacter_KeepsTheCharacterWhole()
    {
        string cut = CutBetweenWords[..72] + "ABCDEFGHIJKLMNOPQRSTUVW\U0001E900ZZ";        // 26 characters: the 24th position is the astral one
        var diagnostics = Compile(Program([.. Head, cut, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("'ABCDEFGHIJKLMNOPQRSTUVW\U0001E900...' (positions 73 to 98)", note);
    }

    [Fact] // a CAUSE READS BEFORE ITS EFFECT: the cut precedes the error it may explain
    public void TheNote_IsReportedBeforeTheError()
    {
        var diagnostics = Compile(Program([.. Head, CutBetweenWords, "           STOP RUN."]));

        int note = diagnostics.FindIndex(d => d.Contains(NoteCode));
        int error = diagnostics.FindIndex(d => d.Contains("'STOP'"));
        Assert.InRange(note, 0, error - 1);
    }

    [Fact] // a word the margin cut in two: the offending token IS the cut word
    public void CutInsideWord_NamesTheWordTheMarginEnded()
    {
        var diagnostics = Compile(Program([.. Head, CutInsideWord, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(9,73): warning", note);
        Assert.Contains("'D.' (positions 73 to 74)", note);
        Assert.Contains("the word 'END-SEN' ends at position 72", note);
        Assert.Contains(diagnostics, d => d.Contains("error") && d.Contains("main.cob(9,66)") && d.Contains("'END-SEN'"));
    }

    [Fact] // one note per cut, however many errors stand beside it
    public void ACut_IsNotedOnce()
    {
        var diagnostics = Compile(Program([.. Head, CutBetweenWords, "           STOP RUN.", "           STOP RUN."]));

        Assert.Single(Notes(diagnostics));
    }

    [Fact] // a numeric sequence area on every line is the ordinary use of the positions: no note, the error stands
    public void ANumericSequenceArea_IsNotACut()
    {
        string[] lines = [.. Head, CutBetweenWords.PadRight(72)[..72], "           STOP RUN."];
        var diagnostics = Compile(Program(lines.Select((l, i) => l.PadRight(72) + (i + 1).ToString("D8"))));

        Assert.Empty(Notes(diagnostics));
        AssertStopRunIsTheErrorAfterTheCut(diagnostics);
    }

    [Fact] // an identification tag on most lines (program name and card number) is the card-image convention: no note
    public void IdentificationTagsOnMostLines_AreNotACut()
    {
        string[] lines = [.. Head, CutBetweenWords.PadRight(72)[..72], "           STOP RUN."];
        var diagnostics = Compile(Program(lines.Select((l, i) => l.PadRight(72) + "MARG" + i.ToString("D4"))));

        Assert.Empty(Notes(diagnostics));
        AssertStopRunIsTheErrorAfterTheCut(diagnostics);
    }

    [Fact] // §6.3.7.3: what follows margin R of a line that ends in an inline comment is the rest of the comment
    public void ACommentThatRunsPastMarginR_IsNotACut()
    {
        string comment = "           CALL \"SUBPROG\" USING WS-MSG BY".PadRight(60) + " *> a comment that runs well past margin R of this line";
        var diagnostics = Compile(Program([.. Head, comment, "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
        AssertStopRunIsTheErrorAfterTheCut(diagnostics);
    }

    [Fact] // §6.3.7.2: a comment line's text runs "up to margin R", and a comment line is not program text
    public void ACommentLinePastMarginR_IsNotACut()
    {
        string commentLine = "      * a comment line whose text runs on and on and on and on past margin R of the line";
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", commentLine, "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(11,12)") && d.Contains("'STOP'"));
    }

    /// <summary>The line after <c>BY</c>: its program-text area is blank and <c>CONTENT WS-REPLY</c> stands from position 73.</summary>
    private static readonly string OnlyPastMarginR = new string(' ', 72) + "CONTENT WS-REPLY";

    [Fact] // PB4372: a statement written ONLY past margin R leaves a blank line and no token to anchor, yet it is the cut
    public void AStatementWrittenOnlyPastMarginR_IsNamedOnItsOwnLine()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", OnlyPastMarginR, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(10,73): warning", note);                    // line 10, the first position past the area
        Assert.Contains("this line has no program text", note);
        Assert.Contains("'CONTENT WS-REPLY' (positions 73 to 88)", note);
        Assert.Contains("main.cob(11,12) may be this", note);                  // STOP stands on line 11
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(11,12)") && d.Contains("'STOP'"));
    }

    [Fact] // PB4372 x PB4373: a line with no program text that holds a tab states margin R as position 72 and says so
    public void ALineWithNoProgramTextHoldingATab_StatesMarginRAtPosition72()
    {
        // The tab fills positions 1-8 (DOC-A.1-157), so position 73 is the 66th character of the line (column 66).
        string tabbed = "\t" + OnlyPastMarginR[8..];
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", tabbed, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(10,66): warning", note);                                  // the column the file holds position 73 at
        Assert.Contains("this line has no program text", note);
        Assert.Contains("after position 72 (a tab counts the positions it fills, so the column this warning names, where the file holds the cut, is less than that)", note);
        Assert.Contains("'CONTENT WS-REPLY' (positions 73 to 88)", note);
        Assert.Contains("Write the text within positions 8 to 72", note);
    }

    [Fact] // PB4372: every line between the two tokens is asked about, not only the one beside the previous token
    public void SeveralLinesBetweenTheTokens_NameTheCutOnTheOneThatHoldsIt()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", "", "      * a comment line",
            OnlyPastMarginR, "", "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(12,73): warning", note);
        Assert.Contains("main.cob(14,12) may be this", note);
    }

    /// <summary>A DISPLAY with program text AND a cut (<c>UPON CONSOLE</c> from position 73): the line holds a statement.</summary>
    private static readonly string TextAndCut = "           DISPLAY \"TRACE\" WS-MSG".PadRight(72) + "UPON CONSOLE";

    [Fact] // PB4372 (review R1-1): ISO §7.3.16.4 3): a false >>IF branch is omitted, so its tokenless line is no cut's effect
    public void ALineOfAnOmittedConditionalBranch_IsNotAskedAbout()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", "       >>IF TRACING IS DEFINED", TextAndCut,
            "       >>END-IF", "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(13,12)") && d.Contains("'STOP'"));
    }

    [Fact] // PB4372 (review R1-1): a debugging line compiled without debugging mode is a comment line, no cut's effect
    public void ADebuggingLineWithoutDebuggingMode_IsNotAskedAbout()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", "      D" + TextAndCut[7..], "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(11,12)") && d.Contains("'STOP'"));
    }

    [Fact] // PB4372: a sequence number on a line with no program text is still the ordinary use of the positions
    public void ASequenceNumberOnABlankLine_IsNotACut()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY", new string(' ', 72) + "00000010",
            "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(11,12)") && d.Contains("'STOP'"));
    }

    [Fact] // PB4372: tags on a MINORITY of lines are still a convention that recurs, and a tagged blank line counts
    public void TagsOnAMinorityOfLines_AreNotACut()
    {
        string[] lines = [.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY".PadRight(72) + "REV0042", new string(' ', 72) + "REV0042",
            "           STOP RUN."];
        var diagnostics = Compile(Program(lines));

        Assert.Empty(Notes(diagnostics));
        Assert.Single(diagnostics, d => d.Contains("error") && d.Contains("main.cob(11,12)") && d.Contains("'STOP'"));
    }

    [Fact] // PB4372: ONE tag-shaped word is indistinguishable from a one-word tail (only recurrence tells), so the note stays
    public void ALoneTagShapedWord_IsStillNamedAsAPossibleCut()
    {
        var diagnostics = Compile(Program([.. Head, "           CALL \"SUBPROG\" USING WS-MSG BY".PadRight(72) + "REV0042",
            "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("'REV0042' (positions 73 to 79)", note);
        Assert.Contains("may be this", note);
    }

    [Fact] // PB4372 (review R1-2): a tab is ONE column of the file and several positions; the note names POSITIONS 72 and 73
    public void ALineHoldingATab_NamesPositionsNotFileColumns()
    {
        // A tab at the start of the line advances to the next tab stop, position 9 (PhysicalLines, DOC-A.1-157), so the text
        // that follows it begins at position 9 and position 73, the first past the program-text area, is 64 characters on.
        string line = "\t" + "CALL \"SUBPROG\" USING WS-MSG BY".PadRight(64) + "CONTENT WS-REPLY";
        var diagnostics = Compile(Program([.. Head, line, "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("main.cob(9,66): warning", note);                     // 1 column for the tab + 64 characters, then 66
        Assert.Contains("it ends at position 72", note);
        Assert.Contains("'CONTENT WS-REPLY' (positions 73 to 88)", note);
    }

    [Fact] // free form is not column-bounded (§6.4): nothing is cut
    public void FreeForm_HasNoMarginR()
    {
        var diagnostics = Compile(Program([.. Head, CutBetweenWords, "           STOP RUN."]), InitialReferenceFormat.Free);

        Assert.Empty(Notes(diagnostics));
    }

    [Fact] // a cut inside library text names the LIBRARY TEXT's file and line
    public void ACutInLibraryText_NamesTheCopybook()
    {
        File.WriteAllText(Path.Combine(_dir, "CUT.cpy"), CutBetweenWords + "\n");
        var diagnostics = Compile(Program([.. Head, "           COPY CUT.", "           STOP RUN."]));

        string note = Assert.Single(Notes(diagnostics));
        Assert.Contains("CUT.cpy(1,73): warning", note);
    }
}
