// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// WHERE PROGRAM TEXT STANDS in its physical line (kb/Work PB1801): the text a classifier hands the
/// <see cref="LogicalLineBuilder"/> — the program-text area of a fixed-form line, a whole free-form line — begins at
/// index <see cref="Index"/> of the EXPANDED line of physical line <see cref="Line"/>, whose <see cref="Columns"/>
/// (null when it held neither a tab nor a supplementary-plane character, so an index IS a position) lead back to the
/// character positions the file holds. The one place an index of that text becomes a physical column: a diagnostic's
/// (<see cref="Col"/>) and a resultant line's origin spans (<see cref="Spans"/>), so the two can never disagree, and
/// the one column unit is the CHARACTER POSITION (<see cref="CharacterPositions"/>, kb/Work PB2983).
/// </summary>
internal readonly record struct TextSite(int Line, int Index, ColumnMap? Columns)
{
    /// <summary>The site of the text from its index <paramref name="i"/> on.</summary>
    public TextSite At(int i) => this with { Index = Index + i };

    /// <summary>The 0-based physical column of the character at index <paramref name="i"/> of the text — the column a
    /// diagnostic names: its character position in the line as the file holds it (a TAB counts one column, as in the
    /// user's editor, and a supplementary-plane character counts one, not two).</summary>
    public int Col(int i) => PhysicalLine.ColumnOf(Columns, Index + i);

    /// <summary>A one-span origin of text at <paramref name="i"/>, written at resultant column
    /// <paramref name="resultColumn"/>.</summary>
    public OriginSpan Span(string file, int resultColumn, int i)
        => new(resultColumn, new SourceOrigin(file, Line), Col(i));

    /// <summary>The origin spans of the <paramref name="length"/> characters of the text from <paramref name="i"/>
    /// on, written at resultant column <paramref name="resultColumn"/>: one span, or on a line that held a tab or a
    /// supplementary-plane character a new one wherever the physical columns stop running one for one (a tab's
    /// expansion all maps to the tab, and the second unit of a supplementary character to the character).</summary>
    public List<OriginSpan> Spans(string file, int resultColumn, int i, int length)
    {
        var spans = new List<OriginSpan> { Span(file, resultColumn, i) };
        if (Columns is null) return spans;
        for (int k = 1; k < length; k++)
            if (Col(i + k) != Col(i + k - 1) + 1)
                spans.Add(Span(file, resultColumn + k, i + k));
        return spans;
    }
}
