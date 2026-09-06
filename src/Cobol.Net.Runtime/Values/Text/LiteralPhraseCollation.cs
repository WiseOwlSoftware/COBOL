// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE implementation of ISO §12.3.7.4 GR7 k — an <c>ALPHABET</c> LITERAL PHRASE's collating sequence and
/// coded character set — over a native character set of 65,536 characters (the UTF-16 code units: the native
/// ALPHANUMERIC repertoire and, per D-N1/§8.5.1.4, the native NATIONAL one alike). Both classes' sequences obey
/// the SAME six sub-rules, so both classes' carriers are this class; <see cref="AlphanumericCollation"/> and
/// <see cref="NationalCollation"/> are its two named arms and add nothing but their identity and their
/// §12.3.7.4 GR8/GR9 defaults.
/// <para>⛔ WHY IT IS SPARSE. The clause positions the characters it NAMES; everything else follows in native
/// relative order (GR7 k3). Tabulating only the named characters keeps the emitted carrier proportional to the
/// SOURCE — a dense 65,536-entry array would be emitted into every generated program for a handful of remapped
/// characters — while still admitting any character of the repertoire. The alphanumeric arm used to be DENSE
/// over a 256-entry Latin-1 block and masked every operand with <c>&amp; 0xFF</c>, so <c>ALPHABET A IS 305 THRU
/// 300</c> — legal source naming U+012B..U+0130 — silently reversed <c>'+'</c>…<c>'0'</c> instead
/// (kb/Work PB770 leg f).</para>
/// <para>⛔ WHY THERE IS STILL AN O(1) FAST PATH. <see cref="Weight"/> is on the comparison hot path (every
/// relation under a PROGRAM COLLATING SEQUENCE, every SORT/MERGE key character, every indexed-key compare), so a
/// binary search per character would be a real cost. The constructor materializes <c>_fast</c> — the weight of
/// every code unit from 0 through the HIGHEST SPECIFIED one — from the sparse arrays, and above that block every
/// code unit is unspecified, so its weight is the closed form <c>NextFree + (c − |specified|)</c>. That is a
/// CACHE of the one rule, never a second rule: <c>_fast</c> is filled by the same GR7 k3 arithmetic the closed
/// form uses. A typical alphabet names only Latin-1 characters, so <c>_fast</c> is 256 ints.</para>
/// </summary>
public abstract class LiteralPhraseCollation : CobolCollation
{
    /// <summary>The number of characters in the native character set (D-N1: the 65,536 UTF-16 code units, for
    /// the alphanumeric and the national repertoire alike — §12.3.7.3 SR14 b1/c1's ordinal bound and SR14 b4/c4's
    /// character-count bound, and the divisor of GR7 k3's tail).</summary>
    public const int Repertoire = 0x10000;

    private readonly ushort[] _codes;      // the SPECIFIED code units, sorted ascending by code (the search key)
    private readonly ushort[] _positions;  // parallel to _codes: each specified code's 0-based position (ALSO shares one)
    private readonly ushort[] _repByPos;   // per specified position: the FIRST character defined there (§15.15.4 r2 / §15.16.4 r2)
    private readonly int _nextFree;        // the first position after the specified block (§12.3.7.4 GR7 k3)
    private readonly int[] _fast;          // code → weight, for every code up to the highest SPECIFIED one (the cache)

    /// <param name="codes">The specified code units, ascending.</param>
    /// <param name="positions">Parallel to <paramref name="codes"/>: 0-based collating positions.</param>
    /// <param name="repByPos">Per position 0..<paramref name="nextFree"/>−1: the first character defined there.</param>
    /// <param name="nextFree">The first position after the specified block.</param>
    /// <param name="highValue">The sequence's HIGH-VALUE character (§12.3.7.4 GR8 — computed by the binder, which
    /// alone knows the source order the GR8 tie rule needs).</param>
    /// <param name="lowValue">The sequence's LOW-VALUE character (§12.3.7.4 GR9).</param>
    protected LiteralPhraseCollation(ushort[] codes, ushort[] positions, ushort[] repByPos, int nextFree,
        char highValue, char lowValue)
    {
        _codes = codes;
        _positions = positions;
        _repByPos = repByPos;
        _nextFree = nextFree;
        HighValue = highValue;
        LowValue = lowValue;

        // The cache: every code unit from 0 through the highest SPECIFIED one. Below the block a code is either
        // tabulated (its position) or unspecified (GR7 k3: NextFree + the count of unspecified codes below it).
        int blockLength = codes.Length == 0 ? 0 : codes[^1] + 1;
        _fast = new int[blockLength];
        for (int c = 0, i = 0, unspecified = 0; c < blockLength; c++)
        {
            if (i < codes.Length && codes[i] == c) _fast[c] = positions[i++];
            else _fast[c] = nextFree + unspecified++;
        }
    }

    /// <inheritdoc/>
    public override char HighValue { get; }

    /// <inheritdoc/>
    public override char LowValue { get; }

    /// <summary>
    /// Compare two values under this sequence (ISO §8.8.4.2.7 for the alphanumeric arm — "with respect to the
    /// collating sequence of characters specified for the current alphanumeric program collating sequence" — and
    /// §8.8.4.2.9 for the national one): the shorter operand space-extends on the right (§8.8.4.2.1 — the pad
    /// SPACE itself weighs through the sequence), and the first position whose WEIGHTS differ decides.
    /// </summary>
    public override int Compare(string? left, string? right)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            int a = Weight(i < left.Length ? left[i] : ' ');
            int b = Weight(i < right.Length ? right[i] : ' ');
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    /// <summary>The total number of collating positions: the specified block plus one DISTINCT position per
    /// unspecified character (ISO §12.3.7.4 GR7 k3 — never a shared bucket). Under an ALSO collapse the block has
    /// FEWER positions than characters, so this is smaller than <see cref="Repertoire"/>.</summary>
    public override int PositionCount => _nextFree + (Repertoire - _codes.Length);

    /// <summary>The 0-based collating position of <paramref name="c"/> — the ORD arithmetic (§15.70.4 r1/r2): a
    /// specified character's tabulated position, else <c>NextFree + (c − |specified codes below c|)</c>, the
    /// §12.3.7.4 GR7 k3 ascending-native-order placement above the specified block. Above the cached block every
    /// specified code is below <paramref name="c"/>, so the count is the whole specified set.</summary>
    public override int Weight(char c) => c < _fast.Length ? _fast[c] : _nextFree + (c - _codes.Length);

    /// <summary>The character at 0-based <paramref name="position"/> — the CHAR / CHAR-NATIONAL inverse
    /// (§15.15.4 r2 / §15.16.4 r2): a specified position returns the FIRST character defined for it (source
    /// order — literal-1 of an ALSO group, GR7 k6); an unspecified position returns the rank-th unspecified code
    /// unit in ascending code order (GR7 k3). Returns −1 outside the sequence. Exact inverse of
    /// <see cref="Weight"/> by construction.</summary>
    public override int CharAt(long position)
    {
        if (position < 0 || position >= PositionCount) return -1;
        if (position < _nextFree) return _repByPos[position];
        long rank = position - _nextFree;         // rank among the unspecified code units, ascending
        int prev = -1;
        for (int i = 0; i <= _codes.Length; i++)
        {
            int next = i < _codes.Length ? _codes[i] : Repertoire;
            int gap = next - prev - 1;             // unspecified code units strictly between prev and next
            if (rank < gap) return prev + 1 + (int)rank;
            rank -= gap;
            prev = next;
        }
        return -1;                                 // unreachable: PositionCount bounds the walk
    }
}
