// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>One comment-entry candidate a run of the text stages proved to be program text: the PHYSICAL line that opens the
/// candidate paragraph, in one INCLUSION of its text (<see cref="CommentEntryPins"/>).</summary>
/// <param name="File">The file the line is in.</param>
/// <param name="Inclusion">Which text of the run the line was read in: <see cref="CommentEntryPins.SourceText"/> for the
/// source text, and for library text the number the COPY driver gave the inclusion (the first library text it brings in
/// is 1, the next 2, in the order it brings them in).</param>
/// <param name="Line">The line number in <paramref name="File"/>.</param>
public readonly record struct CommentEntryPin(string File, int Inclusion, int Line);

/// <summary>
/// ⛔ THE COMMENT-ENTRY CANDIDATES A PREVIOUS RUN OF THE TEXT STAGES PROVED TO BE PROGRAM TEXT (docs/CONFORMANCE.md D-CE2;
/// kb/Work PB4281, PB4487). A candidate the logical conversion cannot place is carried as an inert slot
/// (<see cref="CommentEntryCarrier"/>) through conditional compilation, COPY and REPLACE, and decided afterwards
/// (<see cref="CommentEntryResolver"/>). A candidate that proves to be a COMMENT-ENTRY is exactly what those stages should
/// not have read. A candidate that proves to be PROGRAM TEXT — a paragraph of the procedure division named REMARKS —
/// is exactly what they should have: its COPY statements, its REPLACE statements and the replacing in force over its
/// words, its compiler directives. The stages cannot be run on text whose reading they decide, so the compilation
/// iterates: <c>Frontend</c> runs the text stages, the resolver names the EARLIEST candidate of the finished text that is
/// program text, and the stages run again with that candidate's lines converted as the ordinary program text they are —
/// to a fixed point, which is reached at once when no candidate is program text (the usual case, and every program with
/// no candidate) and otherwise after one more run per candidate that is.
/// <para><b>Why one pin per run, the earliest (kb/Work PB4487).</b> Reading a pinned candidate as program text changes the
/// text AFTER it — its COPY statements bring library text in, its REPLACE statements and directives act on what follows —
/// and never the text before it, so the earliest candidate a run proves to be program text was decided on text that no pin
/// still to come can change, and every candidate before it is a comment-entry for the same reason. A LATER candidate was
/// decided on a text in which that earliest one was still hidden: a <c>COPY</c> in its paragraph may bring in the
/// IDENTIFICATION DIVISION the later one stands in. So a run settles exactly one candidate, and the later ones are decided
/// again by the run that reads it.</para>
/// <para><b>Why a pin names an inclusion.</b> A library text two COPY statements bring in is two texts of the compilation
/// group, and its candidate may stand in an IDENTIFICATION DIVISION in one and in a PROCEDURE DIVISION in the other. The
/// inclusion numbers of a run are given in text order, so every inclusion up to the one the earliest candidate stands in
/// is numbered alike by every run that pins it or a later one: a pin keeps naming the text it was proved in. The
/// converters consult a pin only for a line whose division they cannot tell, never for one in a division they know.</para>
/// </summary>
public sealed class CommentEntryPins
{
    /// <summary>The inclusion number of the source text, which the COPY driver's library texts follow (1, 2, …).</summary>
    public const int SourceText = 0;

    /// <summary>No candidate is known to be program text.</summary>
    public static readonly CommentEntryPins None = new([]);

    private readonly HashSet<CommentEntryPin> _pins;

    private CommentEntryPins(HashSet<CommentEntryPin> pins) => _pins = pins;

    /// <summary>Whether the candidate that opens at <paramref name="line"/> of <paramref name="file"/>, read in inclusion
    /// <paramref name="inclusion"/>, is known to be program text.</summary>
    internal bool Contains(string file, int inclusion, int line) => _pins.Count > 0 && _pins.Contains(new CommentEntryPin(file, inclusion, line));

    /// <summary>These pins and <paramref name="pin"/>. A pinned candidate is read as program text, never carried again, so a
    /// resolver that names one twice has broken the fixed point's contract, and the iteration stops rather than loop.</summary>
    internal CommentEntryPins With(CommentEntryPin pin)
    {
        if (_pins.Contains(pin))
            throw new InvalidOperationException(
                $"the comment-entry candidate at {pin.File} line {pin.Line} (inclusion {pin.Inclusion}) was named program text "
                + "twice: a pinned candidate is read as program text and never carried again (docs/CONFORMANCE.md D-CE2, kb/Work PB4487)");
        return new CommentEntryPins([.. _pins, pin]);
    }
}
