// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE COMMENT-ENTRY CANDIDATES A PREVIOUS RUN OF THE TEXT STAGES PROVED TO BE PROGRAM TEXT (docs/CONFORMANCE.md D-CE2;
/// kb/Work PB4281, review finding R1-1). A candidate the logical conversion cannot place is carried as an inert slot
/// (<see cref="CommentEntryCarrier"/>) through conditional compilation, COPY and REPLACE, and decided afterwards
/// (<see cref="CommentEntryResolver"/>). A candidate that proves to be a COMMENT-ENTRY is exactly what those stages should
/// not have read. A candidate that proves to be PROGRAM TEXT — a paragraph of the procedure division named REMARKS —
/// is exactly what they should have: its COPY statements, its REPLACE statements and the replacing in force over its
/// words, its compiler directives. The stages cannot be run on text whose reading they decide, so the compilation
/// iterates: <c>Frontend</c> runs the text stages, the resolver names the candidates that are program text, and the stages
/// run again with those lines converted as the ordinary program text they are — to a fixed point, which is reached at once
/// when no candidate is program text (the usual case, and every program with no candidate) and at most once more per
/// candidate otherwise (every run adds a candidate to the pins or ends).
/// <para>A pin is the PHYSICAL line that opens the candidate paragraph: the file and the line number in it. A line of a
/// library text a COPY statement brings in twice is pinned for both inclusions only if both were program text
/// (<see cref="CommentEntryResolver"/>); the converters consult a pin only for a line whose division they cannot tell, never
/// for one in a division they know.</para>
/// </summary>
public sealed class CommentEntryPins
{
    /// <summary>No candidate is known to be program text.</summary>
    public static readonly CommentEntryPins None = new([]);

    private readonly HashSet<(string File, int Line)> _lines;

    private CommentEntryPins(HashSet<(string File, int Line)> lines) => _lines = lines;

    /// <summary>Whether the candidate that opens at <paramref name="line"/> of <paramref name="file"/> is known to be
    /// program text.</summary>
    internal bool Contains(string file, int line) => _lines.Count > 0 && _lines.Contains((file, line));

    /// <summary>These pins and <paramref name="programText"/>; this same instance when <paramref name="programText"/> adds
    /// none — which is how the caller learns it has reached the fixed point.</summary>
    internal CommentEntryPins With(IEnumerable<(string File, int Line)> programText)
    {
        HashSet<(string File, int Line)>? grown = null;
        foreach (var key in programText)
            if (!_lines.Contains(key)) (grown ??= [.. _lines]).Add(key);
        return grown is null ? this : new CommentEntryPins(grown);
    }
}
