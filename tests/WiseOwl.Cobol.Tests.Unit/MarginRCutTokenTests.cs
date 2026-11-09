// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A TOKEN THE MARGIN CUT IN TWO IS NAMED EVEN WHEN THE PROGRAM STILL PARSES (kb/Work PB4999; ISO §2023:6.3.1 "Margin R is
/// immediately to the right of the rightmost character position of the program-text area", Annex A.1 item 158: position 72
/// here). The text past margin R is not program text, so <c>COMPUTE X = Y ** 0.7</c> with its <c>7</c> at position 73 is read
/// <c>COMPUTE X = Y ** 0.</c>, which is a valid program of another meaning. COBOL6688 is the warning; COBOL5788
/// (<see cref="MarginRCutNoteTests"/>) is the note beside a syntax error, and a line holds one of the two, never both. Every
/// position is COMPUTED from the layout the test writes (position 72 is the last the program-text area holds).
/// </summary>
public sealed class MarginRCutTokenTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_MarginRToken_");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private const string Warning = "COBOL6688";
    private const string Note = "COBOL5788";

    private static readonly string[] Head =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. MARGINRT.",
        "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.",
        "       01 Y PIC 9V9(4) VALUE 4.",
        "       01 X PIC 9(3)V9(4).",
        "       01 WS-REPLY PIC X(8).",
        "       PROCEDURE DIVISION.",
        "       MAIN-P.",
    ];

    /// <summary><c>Y ** 0</c> then <c>.</c> at position 72 and <c>7</c> at position 73: the program text ends in <c>0.</c>.</summary>
    private static readonly string CutNumber = "           COMPUTE X = Y ** 0".PadRight(71) + ".7";

    private static readonly string[] Tail = ["           DISPLAY X.", "           STOP RUN."];

    private static string Program(IEnumerable<string> lines) => string.Join('\n', lines) + '\n';

    private List<Diagnostic> Compile(string source, InitialReferenceFormat format = InitialReferenceFormat.Fixed)
    {
        string path = Path.Combine(_dir, "main.cob");
        File.WriteAllText(path, source);
        var frontend = new CnFrontend { InitialFormat = format };
        frontend.AddCopySearchPath(_dir);
        var bag = new DiagnosticBag();
        frontend.Parse(path, bag);
        return [.. bag.Diagnostics];
    }

    private static List<string> Of(List<Diagnostic> diagnostics, string code) =>
        [.. diagnostics.Where(d => d.Code == code).Select(d => d.ToString())];

    [Fact] // the PB4999 repro: the number 0.7 reads 0. and the program parses, so the warning is the only evidence
    public void ANumberCutAtMarginR_ThatStillParses_IsNamed()
    {
        var diagnostics = Compile(Program([.. Head, CutNumber, .. Tail]));

        string warning = Assert.Single(Of(diagnostics, Warning));
        Assert.Contains("main.cob(10,73): warning", warning);                // the first position past the program-text area
        Assert.Contains("it ends at position 72", warning);
        Assert.Contains("'7' (positions 73 to 73)", warning);
        Assert.Contains("Shorten the line to end at position 72", warning);
        Assert.Empty(Of(diagnostics, Note));                                 // no syntax error: the note is not given
        Assert.DoesNotContain(diagnostics, d => d.IsError);
    }

    [Fact] // a data name the margin cut in two: the parse takes the short name, only the warning says why
    public void AWordCutAtMarginR_ThatStillParses_IsNamedWithItsToken()
    {
        string cutWord = "           MOVE 1 TO ".PadRight(65) + "WS-REPLY.";           // WS-REPL holds positions 66 to 72
        var diagnostics = Compile(Program([.. Head, cutWord, "           STOP RUN."]));

        string warning = Assert.Single(Of(diagnostics, Warning));
        Assert.Contains("main.cob(10,73): warning", warning);
        Assert.Contains("The token 'WS-REPL' ends at position 72", warning);
        Assert.Contains("'Y.' (positions 73 to 74)", warning);
    }

    [Fact] // a sequence number or tag SET OFF by a space is the ordinary use of positions 73-80
    public void ATagAfterASpace_IsNotACut()
    {
        string tagged = "           COMPUTE X = Y ** 0.7".PadRight(72) + "REV0042";
        var diagnostics = Compile(Program([.. Head, tagged, .. Tail]));

        Assert.Empty(Of(diagnostics, Warning));
        Assert.Empty(Of(diagnostics, Note));
    }

    [Fact] // PB4372's recurrence rule: a tag-shaped word on two lines is a column convention even where it abuts the code
    public void ATagThatRecurs_IsNotACut()
    {
        string first = "           COMPUTE X = Y ** 0.7".PadRight(71) + ".T1";
        string second = "           DISPLAY X".PadRight(71) + ".T2";
        var diagnostics = Compile(Program([.. Head, first, second, "           STOP RUN."]));

        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // a cut beside a syntax error is COBOL5788's, named once: the screen never adds a second warning for the line
    public void ACutBesideASyntaxError_IsNamedByTheNoteOnly()
    {
        string cutInsideWord = "           SEND TO LINAGE-COUNTER FROM WS-MSG RETURNING WS-REPLY".PadRight(65) + "END-SEND.";
        var diagnostics = Compile(Program([.. Head, cutInsideWord, "           STOP RUN."]));

        Assert.Single(Of(diagnostics, Note));
        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // free form is not column-bounded (§2023:6.4): nothing is cut
    public void FreeForm_HasNoMarginR()
    {
        var diagnostics = Compile(Program([.. Head, CutNumber, .. Tail]), InitialReferenceFormat.Free);

        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // the rest of an inline comment lies past margin R by definition (§2023:6.3.7.3), so it is no token's cut
    public void TheRestOfAnInlineComment_IsNotACut()
    {
        string commented = "           DISPLAY X *> shown at the end".PadRight(71) + "x" + "yz";
        var diagnostics = Compile(Program([.. Head, commented, "           STOP RUN."]));

        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // a comment line is cut by definition and has no tokens
    public void ACommentLine_IsNotACut()
    {
        string comment = "      * a comment line that runs on".PadRight(71) + "past margin R";
        var diagnostics = Compile(Program([.. Head, comment, .. Tail]));

        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // a line the compiler never read (a false >>IF branch) holds no token, so nothing on it was cut
    public void ALineInAnOmittedBranch_IsNotACut()
    {
        var diagnostics = Compile(Program([.. Head, "       >>IF NEVER-DEFINED IS DEFINED", CutNumber, "       >>END-IF", .. Tail]));

        Assert.Empty(Of(diagnostics, Warning));
    }

    [Fact] // a cut inside library text names the LIBRARY TEXT's file and line
    public void ACutInLibraryText_NamesTheCopybook()
    {
        File.WriteAllText(Path.Combine(_dir, "CUT.cpy"), CutNumber + "\n");
        var diagnostics = Compile(Program([.. Head, "           COPY CUT.", .. Tail]));

        string warning = Assert.Single(Of(diagnostics, Warning));
        Assert.Contains("CUT.cpy(1,73): warning", warning);
    }
}
