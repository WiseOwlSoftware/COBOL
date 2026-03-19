// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Common;

/// <summary>
/// A contiguous region in source text, defined by start position and length.
/// </summary>
/// <param name="Start">Zero-based absolute character offset of the span's first character.</param>
/// <param name="Length">Number of characters in the span.</param>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>Exclusive end position (Start + Length).</summary>
    public int End => Start + Length;

    /// <summary>Creates a span from inclusive start and exclusive end positions.</summary>
    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    /// <summary>Tests whether a zero-based position falls within this span (exclusive of <see cref="End"/>).</summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>Tests whether this span and <paramref name="other"/> share at least one position.</summary>
    public bool OverlapsWith(TextSpan other) => Start < other.End && other.Start < End;

    /// <summary>Formats as a half-open interval, e.g. "[0..5)".</summary>
    public override string ToString() => $"[{Start}..{End})";
}
