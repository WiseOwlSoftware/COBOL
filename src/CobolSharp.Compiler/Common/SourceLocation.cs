// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Common;

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

    public SourceLocation(string fileName, int position, int line, int column)
    {
        FileName = fileName;
        Position = position;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Display as "file(line,col)" using 1-based line/column for human readability.
    /// </summary>
    public override string ToString() => $"{FileName}({Line + 1},{Column + 1})";

    public bool Equals(SourceLocation other) =>
        Position == other.Position && FileName == other.FileName;

    public override bool Equals(object? obj) => obj is SourceLocation other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(FileName, Position);

    public static bool operator ==(SourceLocation left, SourceLocation right) => left.Equals(right);
    public static bool operator !=(SourceLocation left, SourceLocation right) => !left.Equals(right);
}
