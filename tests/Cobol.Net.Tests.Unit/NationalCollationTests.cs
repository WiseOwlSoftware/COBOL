// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The SPARSE national-collation math (P10 Step 4; ISO §12.3.7 GR7 k over the native NATIONAL set — the
/// 65,536 UTF-16 code units, D-N1): specified characters take their tabulated positions; every UNSPECIFIED
/// code unit takes a DISTINCT ascending position above the highest specified one, in native relative order
/// (k3), computed arithmetically by <see cref="NationalCollation.Weight"/>; <see cref="NationalCollation.CharAt"/>
/// is its exact inverse (the §15.16.4 CHAR-NATIONAL contract, incl. the rule-2 first-character-defined
/// representative for an ALSO-shared position).
/// </summary>
public sealed class NationalCollationTests
{
    /// <summary>ALPHABET R FOR NATIONAL IS N"CBA" — the alphabet_national golden's sequence:
    /// C→0, B→1, A→2; NextFree = 3.</summary>
    private static NationalCollation Reversed() =>
        new(codes: [(ushort)'A', (ushort)'B', (ushort)'C'],
            positions: [2, 1, 0],
            repByPos: [(ushort)'C', (ushort)'B', (ushort)'A'],
            nextFree: 3);

    [Fact]
    public void Weight_SpecifiedCharacters_TakeTabulatedPositions()
    {
        var nat = Reversed();
        Assert.Equal(0, nat.Weight('C'));
        Assert.Equal(1, nat.Weight('B'));
        Assert.Equal(2, nat.Weight('A'));
    }

    /// <summary>GR7 k3: an unspecified code unit c weighs NextFree + (c − |specified below c|) — distinct,
    /// ascending, native relative order. '@' (0x40, below all three specified) → 3 + 64; 'D' (0x44, above all
    /// three) → 3 + 68 − 3 = 68 (a permutation of an initial native run leaves the tail unchanged).</summary>
    [Fact]
    public void Weight_UnspecifiedCharacters_AscendAboveTheSpecifiedBlock()
    {
        var nat = Reversed();
        Assert.Equal(3 + 0x40, nat.Weight('@'));          // no specified code below 0x40
        Assert.Equal(0x44, nat.Weight('D'));              // all three specified codes below
        Assert.Equal(3 + ' ', nat.Weight(' '));           // the golden's ORD-SP leg: 3 + 32 = 35
        Assert.Equal(3 + 0xFFFF - 3, nat.Weight('\uffff'));
        Assert.True(nat.Weight('D') < nat.Weight('E'));   // native relative order preserved (k3)
    }

    [Fact]
    public void CharAt_IsTheExactInverseOfWeight()
    {
        var nat = Reversed();
        // The specified block (§15.16.4 r1/r2).
        Assert.Equal('C', (char)nat.CharAt(0));
        Assert.Equal('B', (char)nat.CharAt(1));
        Assert.Equal('A', (char)nat.CharAt(2));
        // Unspecified positions round-trip: CharAt(Weight(c)) == c for a sample spread.
        foreach (char c in new[] { '\u0000', ' ', '@', 'D', 'z', '\u0100', '\u4e16', '\ufffe', '\uffff' })
            Assert.Equal(c, (char)nat.CharAt(nat.Weight(c)));
        // The sequence bounds (§15.16.3 r2 — outside → no character).
        Assert.Equal(0x10000, nat.PositionCount);          // 3 specified + 65,533 unspecified
        Assert.Equal(-1, nat.CharAt(-1));
        Assert.Equal(-1, nat.CharAt(0x10000));
    }

    /// <summary>An ALSO group shares ONE position (GR7 k6); CharAt returns the FIRST character defined for it
    /// (§15.16.4 r2). Sequence: X ALSO Y (position 0), then Z (position 1) — NextFree = 2.</summary>
    [Fact]
    public void AlsoGroup_SharesOnePosition_CharAtReturnsTheFirstDefined()
    {
        var nat = new NationalCollation(
            codes: [(ushort)'X', (ushort)'Y', (ushort)'Z'],
            positions: [0, 0, 1],
            repByPos: [(ushort)'X', (ushort)'Z'],
            nextFree: 2);
        Assert.Equal(nat.Weight('X'), nat.Weight('Y'));    // one shared weight
        Assert.Equal('X', (char)nat.CharAt(0));            // the first character defined for the position
        Assert.Equal('Z', (char)nat.CharAt(1));
        // Two specified codes collapse into one position: the total shrinks accordingly.
        Assert.Equal(2 + 0x10000 - 3, nat.PositionCount);
        // CobolString.Compare rides the same weights: X == Y under the sequence, both below Z.
        Assert.Equal(0, CobolString.Compare("X", "Y", nat));
        Assert.True(CobolString.Compare("Z", "Y", nat) > 0);
        // The shorter operand extends with the national space, which weighs through the sequence (§8.8.4.2.1).
        Assert.Equal(0, CobolString.Compare("X", "X ", nat));
    }
}
