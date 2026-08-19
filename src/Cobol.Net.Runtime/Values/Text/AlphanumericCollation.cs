// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A non-native ALPHANUMERIC program collating sequence (an ALPHABET literal phrase, ISO §12.3.7 GR7 k) over
/// the native alphanumeric character set — the 65,536 UTF-16 code units (item 188). DENSE over the Latin-1
/// block the ALPHABET clause can position (a 256-entry position table), ARITHMETIC above it: every code unit
/// ≥ 256 takes a DISTINCT ascending position after the whole positioned block, in native relative order
/// (§12.3.7.4 GR7 1.3) — the alphanumeric twin of <see cref="NationalCollation"/> (fix-queue PB59 /
/// RV-15.15.4-1/-2, AR-15.15.3-2: CHAR carried a pre-PB3 native-ordinal fallback, scanned for the
/// LOWEST-coded member of a shared position where §15.15.4 r2 requires the FIRST character DEFINED, and
/// bounded its domain by the 256-entry block where GR7 1.3 gives the sequence
/// <c>NextFree + (0x10000 − 256)</c> positions). The compiler emits one instance per program as
/// <c>__COLLATE</c>; CHAR (§15.15.4) and ORD (§15.70.4 r1) read it. Comparison paths (relations, SORT/MERGE,
/// indexed keys, MAX/MIN) reach <see cref="Compare"/> through the <see cref="CobolCollation"/> carrier: its
/// <c>c &lt; 256 ? positions[c] : c</c> weight tail is ORDER-EQUIVALENT to <see cref="Weight"/> — both rules place
/// every above-block code unit after every positioned one, strictly increasing in code — so only the functions
/// that expose the position NUMBER itself need the exact arithmetic.
/// </summary>
public sealed class AlphanumericCollation : CobolCollation
{
    private readonly ushort[] _positions;  // native code 0..255 → 0-based collating position (ALSO shares)
    private readonly ushort[] _repByPos;   // per position 0..NextFree−1: the FIRST character DEFINED there (§15.15.4 r2 / GR7 1.6)
    private readonly int _nextFree;        // the first position after the positioned block (§12.3.7.4 GR7 1.3)

    /// <param name="highValue">The sequence's HIGH-VALUE character (§12.3.7.4 GR8 — computed by the binder, which
    /// alone knows the source order the GR8 tie rule needs).</param>
    /// <param name="lowValue">The sequence's LOW-VALUE character (§12.3.7.4 GR9).</param>
    public AlphanumericCollation(ushort[] positions, ushort[] repByPos, int nextFree, char highValue = '\u00ff', char lowValue = '\0')
    {
        _positions = positions;
        _repByPos = repByPos;
        _nextFree = nextFree;
        HighValue = highValue;
        LowValue = lowValue;
    }

    /// <summary>The raw 256-entry position table (the ORD/CHAR arithmetic reads it; comparison goes through
    /// <see cref="Compare"/>).</summary>
    public ushort[] Positions => _positions;

    /// <inheritdoc/>
    public override char HighValue { get; }

    /// <inheritdoc/>
    public override char LowValue { get; }

    /// <summary>
    /// Compare two alphanumeric values under this PROGRAM COLLATING SEQUENCE (ISO §8.8.4.2.7 — "with respect to
    /// the collating sequence of characters specified for the current alphanumeric program collating sequence"):
    /// the shorter operand space-extends on the right (the pad SPACE itself weighs through the sequence), and the
    /// first position whose WEIGHTS differ decides.
    /// <para>⛔ THE COMPARISON WEIGHT'S <c>: c</c> TAIL IS DELIBERATELY NOT THE EXACT §12.3.7.4 GR7 1.3 ARITHMETIC,
    /// AND THAT IS PROVEN SAFE, NOT ASSUMED (fix-queue PB59): the exact position of an above-block unit is
    /// <c>NextFree + (c − 256)</c> (<see cref="Weight"/> — what ORD/CHAR expose), but for COMPARISON only the
    /// ORDER matters, and the two rules are order-equivalent — every tabulated position is &lt; NextFree ≤ 256 ≤
    /// both rules' above-block minimum, and both are strictly increasing in code above the block, so no pair of
    /// characters ever reorders. Keeping the raw table here spares the sort/key/relation pipeline the arithmetic
    /// for zero behavioral gain; a reader tempted to "unify" this arm should re-derive that proof first.</para>
    /// </summary>
    public override int Compare(string? left, string? right)
    {
        left ??= ""; right ??= "";
        int n = Math.Max(left.Length, right.Length);
        for (int i = 0; i < n; i++)
        {
            int a = CompareWeight(i < left.Length ? left[i] : ' ');
            int b = CompareWeight(i < right.Length ? right[i] : ' ');
            if (a != b) return a < b ? -1 : 1;
        }
        return 0;
    }

    private int CompareWeight(char c) => c < _positions.Length ? _positions[c] : c;

    /// <summary>The total number of collating positions: the positioned block plus one DISTINCT position per
    /// code unit above it (ISO §12.3.7.4 GR7 1.3 — never a shared bucket). Under an ALSO collapse the block
    /// has FEWER positions than characters, so this is smaller than 0x10000 (AR-15.15.3-2's both halves:
    /// ordinal 255 legal, 65536 refused, under a three-into-one ALSO alphabet → 65,534 positions).</summary>
    public override int PositionCount => _nextFree + (0x10000 - _positions.Length);

    /// <summary>The 0-based collating position of <paramref name="c"/> — the §15.70.4 r1 ORD arithmetic (the
    /// landed PB3 rule, now the ONE implementation): a positioned code unit's tabulated position, else
    /// <c>NextFree + (c − 256)</c>.</summary>
    public override int Weight(char c) => c < _positions.Length ? _positions[c] : _nextFree + (c - _positions.Length);

    /// <summary>The character at 0-based <paramref name="position"/> — the CHAR inverse (§15.15.4 r2): a
    /// positioned slot returns the FIRST character DEFINED for it (source order — literal-1 of an ALSO group,
    /// GR7 1.6), an above-block position returns its native code unit. Returns −1 outside the sequence.
    /// Exact inverse of <see cref="Weight"/> by construction.</summary>
    public override int CharAt(long position)
    {
        if (position < 0 || position >= PositionCount) return -1;
        if (position < _nextFree) return _repByPos[position];
        return (int)(_positions.Length + (position - _nextFree));
    }
}
