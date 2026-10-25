// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RECEIVING SPLICE OF A VARIABLE-LENGTH GROUP'S OCCURS DEPENDING BIT TABLE KEEPS THE UNUSED OCCURRENCES AND NOTHING ELSE
/// (kb/Work PB3598; <see cref="OdoTail.KeepUnused"/>). ISO §14.9.25.4 GR9 a) moves "each character position that is not occupied by a
/// corresponding table" and §13.18.38.4 8) a) leaves the occurrences past the count out of the operation, so the receiver keeps the
/// bits from the first unused occurrence to the end of the table at its MAXIMUM extent; the implicit filler past that extent
/// (§8.5.1.6.3 rule 4) occupies no table and is the sender's wherever it lies in the characters the receiver uses.
///
/// <para>The table is <c>B PIC 1(2)</c> then <c>T PIC 1(3) OCCURS 1 TO 4</c>: it starts at bit 2 of the member's first character,
/// ends at bit 14 of its two characters, and the last two bits are the filler. Bytes are the ASCII characters whose bit patterns
/// the expectations spell, so a failure reads as a character.</para>
/// </summary>
public sealed class OdoTailKeepUnusedTests
{
    // Elem 3 bits, up to 4 occurrences, the table's first bit two bits into the member's first character, counted in bits.
    private static readonly OdoTail Table = new(Elem: 3, Max: 4, Lead: 2, PositionsPerCharacter: 8);

    private const string Sender = "j~";   // 01101010 01111110: B 01, T 101 010 011 111, filler 10
    private const string Receiver = "\u0000`";  // 00000000 01100000: B 00, T 000 000 011 000, filler 00

    [Fact]
    public void CountThree_KeepsTheUnusedFourthOccurrence_ButTakesTheSendersFillerPastTheMaximum()
    {
        // used bits 0-10 and the filler (bits 14-15) from the sender, T(4) (bits 11-13) the receiver's 000: 011 000 10.
        Assert.Equal("jb", Table.KeepUnused(Sender, Receiver, 3));
    }

    [Fact]
    public void CountTwo_ExtentEndsOnACharacter_TheSecondCharacterIsTheReceiversOwn()
    {
        Assert.Equal("j`", Table.KeepUnused(Sender, Receiver, 2));
    }

    [Fact]
    public void CountOne_KeepsTheSecondOccurrenceInsideTheFirstCharacter()
    {
        // 5 used bits (01101) and T(2)'s three bits (bits 5-7) the receiver's 000: 0x68; the second character is the receiver's.
        Assert.Equal("h`", Table.KeepUnused(Sender, Receiver, 1));
    }

    [Fact]
    public void CountAtTheMaximum_IsTheSendersWhole()
    {
        Assert.Equal(Sender, Table.KeepUnused(Sender, Receiver, 4));
    }

    [Fact]
    public void AStridedTable_TakesThePadAfterTheLastUsedOccurrenceFromTheSender()
    {
        // 3-bit elements 16 bits apart (an ALIGNED table, §13.18.1.4 GR2), two occurrences: 3 + 16 = 19 bits, three characters.
        var strided = new OdoTail(Elem: 16, Max: 2, PositionsPerCharacter: 8, Trailing: 13);
        // Count 1: the extent is 3 bits, one character; the pad after occurrence 1 (bits 3-7) is the sender's, the second
        // occurrence (bits 16-18, in the third character) is unused and the characters past the extent are the receiver's.
        Assert.Equal("j" + "AB", strided.KeepUnused("jCD", " AB", 1));
    }
}
