// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.Unicode.Segmentation;

/// <summary>
/// One EXTENDED GRAPHEME CLUSTER of a text (UAX #29 §3 — what a reader perceives as one character: a base letter with
/// its combining marks, a Hangul syllable written as conjoining jamo, an emoji with its modifiers and ZWJ sequence, a
/// regional-indicator pair, CR+LF, an Indic conjunct …): the slice of the source text it occupies
/// (<see cref="Source"/>, <see cref="Start"/>, <see cref="Length"/> in UTF-16 code units), read as
/// <see cref="Span"/> / <see cref="Memory"/> / <see cref="ToString"/>, and its code points
/// (<see cref="EnumerateCodePoints"/> allocation-free, <see cref="CodePoints"/> as an array). Produced by
/// <see cref="GraphemeBreaker"/> / <see cref="GraphemeEnumerator"/>; a value type, no allocation per cluster.
/// </summary>
public readonly struct GraphemeCluster : IEquatable<GraphemeCluster>
{
    /// <summary>A cluster of <paramref name="source"/> from code unit <paramref name="start"/>, <paramref name="length"/> code units long.</summary>
    public GraphemeCluster(string source, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(source);
        if ((uint)start > (uint)source.Length || (uint)length > (uint)(source.Length - start))
            throw new ArgumentOutOfRangeException(nameof(start), $"cluster [{start}, {start + length}) is outside the {source.Length}-code-unit source");
        Source = source;
        Start = start;
        Length = length;
    }

    /// <summary>The text the cluster is a slice of.</summary>
    public string Source { get; }

    /// <summary>The index of the cluster's first UTF-16 code unit in <see cref="Source"/>.</summary>
    public int Start { get; }

    /// <summary>The cluster's length in UTF-16 code units (≥ 1 for every cluster of a non-empty text; a supplementary
    /// character alone is 2, a family emoji ZWJ sequence can be 11 or more).</summary>
    public int Length { get; }

    /// <summary>The index just past the cluster.</summary>
    public int End => Start + Length;

    /// <summary>The cluster's text, without allocating.</summary>
    public ReadOnlySpan<char> Span => Source.AsSpan(Start, Length);

    /// <summary>The cluster's text as memory, without allocating.</summary>
    public ReadOnlyMemory<char> Memory => Source.AsMemory(Start, Length);

    /// <summary>The number of code points in the cluster (an unpaired surrogate counts as one).</summary>
    public int CodePointCount
    {
        get
        {
            int n = 0;
            foreach (var _ in EnumerateCodePoints()) n++;
            return n;
        }
    }

    /// <summary>The cluster's code points, in order — a fresh array; use <see cref="EnumerateCodePoints"/> to avoid the allocation.</summary>
    public int[] CodePoints
    {
        get
        {
            var list = new List<int>(Length);
            foreach (int cp in EnumerateCodePoints()) list.Add(cp);
            return list.ToArray();
        }
    }

    /// <summary>The first code point (the cluster's base, or its Prepend / the first regional indicator …).</summary>
    public int FirstCodePoint => Length == 0 ? -1 : CodePointAt(Source, Start);

    /// <summary>True when the cluster is a single code point — the common case for Latin, Cyrillic, Han …</summary>
    public bool IsSingleCodePoint => Length == 1 || (Length == 2 && char.IsHighSurrogate(Source[Start]) && char.IsLowSurrogate(Source[Start + 1]));

    /// <summary>The code points, one at a time, allocation-free (an unpaired surrogate is yielded as its own code unit value).</summary>
    public CodePointEnumerator EnumerateCodePoints() => new(Source, Start, End);

    /// <summary>The cluster's text as a new string.</summary>
    public override string ToString() => Source.Substring(Start, Length);

    /// <summary>The cluster's Unicode scalar values as <see cref="Rune"/>s (unpaired surrogates become U+FFFD).</summary>
    public IEnumerable<Rune> EnumerateRunes()
    {
        foreach (int cp in EnumerateCodePoints())
            yield return Rune.IsValid(cp) ? new Rune(cp) : Rune.ReplacementChar;
    }

    public bool Equals(GraphemeCluster other) => ReferenceEquals(Source, other.Source) && Start == other.Start && Length == other.Length
        || Span.SequenceEqual(other.Span);

    public override bool Equals(object? obj) => obj is GraphemeCluster c && Equals(c);

    public override int GetHashCode() => string.GetHashCode(Span);

    public static bool operator ==(GraphemeCluster left, GraphemeCluster right) => left.Equals(right);

    public static bool operator !=(GraphemeCluster left, GraphemeCluster right) => !left.Equals(right);

    internal static int CodePointAt(string s, int index)
    {
        char c = s[index];
        return char.IsHighSurrogate(c) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1]) ? char.ConvertToUtf32(c, s[index + 1]) : c;
    }

    /// <summary>An allocation-free code point walker over a slice of a string.</summary>
    public struct CodePointEnumerator
    {
        private readonly string _s;
        private readonly int _end;
        private int _pos;

        internal CodePointEnumerator(string s, int start, int end)
        {
            _s = s;
            _pos = start;
            _end = end;
            Current = 0;
        }

        public int Current { get; private set; }

        public CodePointEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_pos >= _end) return false;
            char c = _s[_pos];
            if (char.IsHighSurrogate(c) && _pos + 1 < _end && char.IsLowSurrogate(_s[_pos + 1]))
            {
                Current = char.ConvertToUtf32(c, _s[_pos + 1]);
                _pos += 2;
            }
            else
            {
                Current = c;
                _pos++;
            }
            return true;
        }
    }
}
