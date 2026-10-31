// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers;

namespace WiseOwl.Cobol.Runtime.Collation;

/// <summary>
/// Canonical decomposition (Unicode Normalization Form D) computed from the collation table's OWN data — the
/// canonical decomposition mappings and combining classes of the UCD version the table was generated from — so the
/// engine's notion of canonical equivalence never depends on the Unicode version of the host's ICU. (That dependence
/// is not hypothetical: a Windows-bundled ICU that predates Unicode 16 leaves U+1ADB and U+10D6A unordered, and the
/// CLDR conformance test then fails on this host while passing on a newer one.)
/// <para>The engine normalizes only when it must (UTS #10 S1.1): a text with a NON-STARTER may need canonical
/// REORDERING and its precomposed bases must decompose so their marks take part in it; at
/// <see cref="CollationStrength.Identical"/> the tie-break compares NFD forms, so any decomposable character counts.
/// Everything else is walked as-is — the derived table's explicit mapping of a precomposed character equals its
/// decomposition's element sequence by construction (the CLDR/UCA data is canonically closed, and
/// <see cref="CollationTable.Rebuild"/> closes every tailored table, whichever front-end built it).</para>
/// <para>NFD here means: replace every code point by its full canonical decomposition (Hangul syllables through the
/// arithmetic L V T mapping), then stable-sort every maximal run of non-starters by combining class (The Unicode
/// Standard §3.11, Canonical Ordering Algorithm). No composition step — collation never needs NFC.</para>
/// <para>The working arrays are pooled, so a normalization allocates nothing but the string it returns — and not even
/// that in the span form, which a comparison uses (kb/Work PB107: the table-driven NFD cost 394 B on accented text
/// against the host's 32 B).</para>
/// </summary>
internal static class Normalizer
{
    /// <summary>Does <paramref name="text"/> need normalizing before it is walked? True when it holds a non-starter
    /// (reordering may apply); with <paramref name="forIdentical"/> also when it holds any decomposable code point.</summary>
    public static bool NeedsNfd(ReadOnlySpan<char> text, CollationTable table, bool forIdentical)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < (char)0xC0) continue;                                   // below U+00C0 nothing decomposes or combines
            int cp = c;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(c, text[++i]);
            if (table.IsNonStarter(cp)) return true;
            if (forIdentical && (CollationTable.IsHangulSyllable(cp) || table.TryGetCanonicalDecomposition(cp, out _))) return true;
        }
        return false;
    }

    /// <summary>The NFD of <paramref name="text"/> under the table's Unicode data. Unpaired surrogates pass through
    /// as themselves.</summary>
    public static string ToNfd(ReadOnlySpan<char> text, CollationTable table) => ToNfd(text, table, origin: null);

    /// <summary>The NFD of <paramref name="text"/>, and in <paramref name="origin"/> (when given) the char index of
    /// <paramref name="text"/> each char of the result came from: a character's decomposition all comes from it, and the
    /// canonical reordering carries each origin with its mark — what a caller needs to map a position of the walked text
    /// back to the text it was given (<see cref="Collator.PrefixShapeOf"/>).</summary>
    public static string ToNfd(ReadOnlySpan<char> text, CollationTable table, List<int>? origin)
    {
        var work = Decompose(text, table, withOrigin: origin is not null);
        try
        {
            if (origin is not null)
                for (int k = 0; k < work.Count; k++)
                {
                    origin.Add(work.From![k]);
                    if (work.Cps[k] > 0xFFFF) origin.Add(work.From[k]);           // both units of a surrogate pair
                }
            return string.Create(work.Utf16Length(), work, static (span, w) => w.WriteTo(span));
        }
        finally { work.Return(); }
    }

    /// <summary>The NFD of <paramref name="text"/> as a span: in <paramref name="scratch"/> when it fits (a caller's
    /// <c>stackalloc</c> — the common, short text), else in a pooled array returned in <paramref name="rented"/>, which
    /// the caller hands to <see cref="Return"/> when it is done with the span. Allocates nothing on the heap either way;
    /// <see cref="ToNfd(ReadOnlySpan{char}, CollationTable)"/> is the form that must outlive the call.</summary>
    public static ReadOnlySpan<char> ToNfd(ReadOnlySpan<char> text, CollationTable table, Span<char> scratch, out char[]? rented)
    {
        var work = Decompose(text, table, withOrigin: false);
        try
        {
            int length = work.Utf16Length();
            Span<char> destination;
            if (length <= scratch.Length) { rented = null; destination = scratch[..length]; }
            else { rented = ArrayPool<char>.Shared.Rent(length); destination = rented.AsSpan(0, length); }
            work.WriteTo(destination);
            return destination;
        }
        finally { work.Return(); }
    }

    /// <summary>Give back what the span form of <see cref="ToNfd(ReadOnlySpan{char}, CollationTable, Span{char}, out char[])"/> rented.</summary>
    public static void Return(char[]? rented)
    {
        if (rented is not null) ArrayPool<char>.Shared.Return(rented);
    }

    /// <summary>The code points of <paramref name="text"/> fully decomposed and canonically ordered (the Canonical
    /// Ordering Algorithm: a stable insertion sort of every run of non-starters by combining class), in pooled arrays —
    /// with the char index each came from when <paramref name="withOrigin"/>.</summary>
    private static Decomposed Decompose(ReadOnlySpan<char> text, CollationTable table, bool withOrigin)
    {
        var d = new Decomposed(text.Length + 8, withOrigin);
        try
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int cp = c, at = i;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    cp = char.ConvertToUtf32(c, text[++i]);
                if (CollationTable.IsHangulSyllable(cp))
                {
                    int n = CollationTable.DecomposeHangul(cp, out int l, out int v, out int t);
                    d.Add(l, 0, at);
                    d.Add(v, 0, at);
                    if (n == 3) d.Add(t, 0, at);
                }
                else if (table.TryGetCanonicalDecomposition(cp, out var parts))
                {
                    foreach (int x in parts) d.Add(x, (byte)table.CombiningClass(x), at);
                }
                else d.Add(cp, (byte)table.CombiningClass(cp), at);
            }
            d.CanonicalOrder();
            return d;
        }
        catch
        {
            d.Return();
            throw;
        }
    }

    /// <summary>The pooled working arrays of one decomposition: the code points, their combining classes and (when
    /// asked) the char index of the source each came from.</summary>
    private struct Decomposed
    {
        public int[] Cps;
        public byte[] Ccc;
        public int[]? From;
        public int Count;

        public Decomposed(int capacity, bool withOrigin)
        {
            Cps = ArrayPool<int>.Shared.Rent(capacity);
            Ccc = ArrayPool<byte>.Shared.Rent(capacity);
            From = withOrigin ? ArrayPool<int>.Shared.Rent(capacity) : null;
            Count = 0;
        }

        public void Add(int codePoint, byte combiningClass, int origin)
        {
            if (Count == Cps.Length) Grow();
            Cps[Count] = codePoint;
            Ccc[Count] = combiningClass;
            if (From is not null) From[Count] = origin;
            Count++;
        }

        private void Grow()
        {
            int size = Cps.Length * 2;
            Cps = Resize(Cps, size);
            Ccc = Resize(Ccc, size);
            if (From is not null) From = Resize(From, size);
        }

        private readonly T[] Resize<T>(T[] old, int size)
        {
            var bigger = ArrayPool<T>.Shared.Rent(size);
            old.AsSpan(0, Count).CopyTo(bigger);
            ArrayPool<T>.Shared.Return(old);
            return bigger;
        }

        public readonly void CanonicalOrder()
        {
            for (int i = 1; i < Count; i++)
            {
                byte ci = Ccc[i];
                if (ci == 0) continue;
                int j = i;
                while (j > 0 && Ccc[j - 1] > ci)
                {
                    (Cps[j], Cps[j - 1]) = (Cps[j - 1], Cps[j]);
                    (Ccc[j], Ccc[j - 1]) = (Ccc[j - 1], Ccc[j]);
                    if (From is not null) (From[j], From[j - 1]) = (From[j - 1], From[j]);
                    j--;
                }
            }
        }

        /// <summary>The length in UTF-16 units: two for a supplementary code point, one for anything else (an unpaired
        /// surrogate stays the one unit it was).</summary>
        public readonly int Utf16Length()
        {
            int n = Count;
            for (int k = 0; k < Count; k++)
                if (Cps[k] > 0xFFFF) n++;
            return n;
        }

        public readonly void WriteTo(Span<char> destination)
        {
            int at = 0;
            for (int k = 0; k < Count; k++)
            {
                int cp = Cps[k];
                if (cp > 0xFFFF)
                {
                    destination[at++] = (char)(0xD7C0 + (cp >> 10));
                    destination[at++] = (char)(0xDC00 + (cp & 0x3FF));
                }
                else destination[at++] = (char)cp;       // a BMP character, or an unpaired surrogate unit as itself
            }
        }

        public readonly void Return()
        {
            ArrayPool<int>.Shared.Return(Cps);
            ArrayPool<byte>.Shared.Return(Ccc);
            if (From is not null) ArrayPool<int>.Shared.Return(From);
        }
    }
}
