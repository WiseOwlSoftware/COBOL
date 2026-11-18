// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// A specific position in a source file (absolute offset + line/column).
/// </summary>
public readonly struct SourceLocation : IEquatable<SourceLocation>
{
    /// <summary>Path or name of the source file.</summary>
    public string FileName { get; }

    /// <summary>Zero-based absolute character offset within the source text.</summary>
    public int Position { get; }

    /// <summary>Zero-based line number within the source file.</summary>
    public int Line { get; }

    /// <summary>Zero-based column offset within the line.</summary>
    public int Column { get; }

    /// <summary>The 1-based RESULTANT line this location was located from (the line of the text the parser read, after COPY,
    /// REPLACE and the reference-format conversion), or 0 when it was not located from a resultant position. Set only by
    /// <see cref="SourceLineMap.Locate(int, int, string)"/>, the one mapping from a resultant position to a physical one, and
    /// meaningful only in <see cref="Frame"/>: a copybook copied twice gives two resultant lines one physical line, and this
    /// says which of them was meant (DESIGN-listing.md section 6.3).</summary>
    public int ResultantLine { get; }

    /// <summary>The <see cref="SourceLineMap.Frame"/> of the map that located this location, or 0 when none did. A text stage
    /// that runs before the last numbers its lines in a frame of its own, so a reader of <see cref="ResultantLine"/> (the
    /// source listing) uses it only when the frame is the one it places lines in.</summary>
    public int Frame { get; }

    public SourceLocation(string fileName, int position, int line, int column)
    {
        FileName = fileName;
        Position = position;
        Line = line;
        Column = column;
    }

    /// <summary>This location as located from 1-based RESULTANT <paramref name="resultantLine"/> of the map whose
    /// <see cref="SourceLineMap.Frame"/> is <paramref name="frame"/>.</summary>
    public SourceLocation LocatedFrom(int resultantLine, int frame) => new(this, resultantLine, frame);

    private SourceLocation(SourceLocation at, int resultantLine, int frame)
    {
        FileName = at.FileName;
        Position = at.Position;
        Line = at.Line;
        Column = at.Column;
        ResultantLine = resultantLine;
        Frame = frame;
    }

    /// <summary>
    /// Display as "file(line,col)" using 1-based line/column for human readability.
    /// </summary>
    public override string ToString() => $"{FileName}({Line + 1},{Column + 1})";

    public bool Equals(SourceLocation other) =>
        Position == other.Position && FileName == other.FileName;

    public override bool Equals(object? obj) => obj is SourceLocation other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(FileName, Position);
}
