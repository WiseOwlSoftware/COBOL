// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A non-native NATIONAL collating sequence (an <c>ALPHABET … FOR NATIONAL</c> literal phrase, ISO §12.3.7 GR7 k)
/// over the native national character set — the 65,536 UTF-16 code units, one per national position (D-N1;
/// §8.5.1.4 makes each code element its own character position). SPARSE: only the SPECIFIED characters are
/// tabulated; every unspecified code unit takes a DISTINCT ascending position above the highest specified one, in
/// native (code-unit) relative order (GR7 k3), computed arithmetically — never a dense 65,536-entry table. The
/// compiler emits one instance per program as <c>__COLLATE_NAT</c> (the national twin of the alphanumeric
/// <c>__COLLATE</c> weights); it drives national relation/condition-name comparisons (§12.3.6 GR11 /
/// §8.8.4.2.9), CHAR-NATIONAL (§15.16.4), and ORD over a national argument (§15.70.4 r2).
/// </summary>
public sealed class NationalCollation
{
    private readonly ushort[] _codes;      // the specified code units, sorted ascending by code (binary-search key)
    private readonly ushort[] _positions;  // parallel: each specified code's 0-based collating position (ALSO shares)
    private readonly ushort[] _repByPos;   // per specified position: the FIRST character defined there (§15.16.4 r2)
    private readonly int _nextFree;        // the first position after the specified block (GR7 k3)

    public NationalCollation(ushort[] codes, ushort[] positions, ushort[] repByPos, int nextFree)
    {
        _codes = codes;
        _positions = positions;
        _repByPos = repByPos;
        _nextFree = nextFree;
    }

    /// <summary>The total number of collating positions: the specified block plus one DISTINCT position per
    /// unspecified code unit (ISO §12.3.7 GR7 k3 — never a shared bucket).</summary>
    public int PositionCount => _nextFree + (0x10000 - _codes.Length);

    /// <summary>The 0-based collating position of <paramref name="c"/>: a specified character's tabulated
    /// position, else <c>NextFree + (c − |specified codes below c|)</c> — the GR7 k3 ascending-native-order
    /// placement above the specified block.</summary>
    public int Weight(char c)
    {
        int i = Array.BinarySearch(_codes, (ushort)c);
        if (i >= 0) return _positions[i];
        int insertion = ~i;                       // = the number of specified codes below c
        return _nextFree + (c - insertion);
    }

    /// <summary>The character at 0-based <paramref name="position"/> — the CHAR-NATIONAL inverse (§15.16.4): a
    /// specified position returns the FIRST character defined for it (rule 2 — deterministic, implementor item 22);
    /// an unspecified position returns the rank-th unspecified code unit in ascending code order (GR7 k3).
    /// Returns −1 when the position is outside the sequence.</summary>
    public int CharAt(long position)
    {
        if (position < 0 || position >= PositionCount) return -1;
        if (position < _nextFree) return _repByPos[position];
        long rank = position - _nextFree;         // rank among the unspecified code units, ascending
        int prev = -1;
        for (int i = 0; i <= _codes.Length; i++)
        {
            int next = i < _codes.Length ? _codes[i] : 0x10000;
            int gap = next - prev - 1;             // unspecified code units strictly between prev and next
            if (rank < gap) return prev + 1 + (int)rank;
            rank -= gap;
            prev = next;
        }
        return -1;                                 // unreachable: PositionCount bounds the walk
    }
}
