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
        var diagnostics = Compile(Program([.. Head, commentLine, "           CALL \"SUBPROG\" USING WS-MSG BY", "           STOP RUN."]));

        Assert.Empty(Notes(diagnostics));
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
