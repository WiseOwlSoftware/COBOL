// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §14.6.10 1) — AN OVERLAPPING MOVE'S RESULT IS UNDEFINED, SO WISEOWL COBOL FOLLOWS GNUCOBOL'S, AND THIS PINS
/// THE ALGORITHM (kb/Work PB1910, PB1907; docs/CONFORMANCE.md §3 D-OVL2; CLAUDE.md rule 1).
/// <para>Every expected image below was MEASURED with GnuCOBOL 3.2.0 (<c>cobc -std=cobol2014</c>, ASCII sign
/// convention unless a case says IBM) on a program that overlays the receiver on the sender with <c>REDEFINES</c> and
/// dumps the area byte by byte. The standard defines no value (Annex A.2 item 36), so no clause supplies these: they
/// are the documented choice. The receiver's storage image is the argument and the result; the sender's is laid under
/// it at <c>receiverAt</c>, the receiver's first byte minus the sender's.</para>
/// </summary>
public sealed class OverlappingMoveTests
{
    private static NumProfile Zoned(int digits, int scale, bool signed, NumericSign sign = NumericSign.TrailingOverpunch,
        SignEncoding enc = SignEncoding.Ascii) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = sign, SignEncoding = enc,
        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned,
        StorageLength = 0,
    };

    private static NumProfile Packed(int digits, int scale, bool signed) => new()
    {
        Digits = digits, FractionDigits = scale, Signed = signed, SignKind = NumericSign.BinaryMinus,
        Truncation = NumericTruncation.PackedDecimal, ByteForm = NumericByteForm.Packed,
        StorageLength = digits / 2 + 1,
    };

    private static string Bytes(params int[] b) => new(b.Select(x => (char)x).ToArray());

    /// <summary>The storage area after a MOVE: the sender's image followed by sixteen periods, as the probe program
    /// laid it out; the receiver is the window at <paramref name="at"/>.</summary>
    private static string Area(string sender) => sender + new string('.', 16);

    [Fact]
    public void DisplayToPacked_HeadOfSender_ReadsClearedBytes()
    {
        // 9(6) holding 123456; PIC 9(5) PACKED-DECIMAL redefining its first three bytes: libcob gives 00456, not 23456.
        string area = Area("123456");
        string receiver = OverlappingMove.DisplayToPacked("123456", area.Substring(0, 3), 0, Zoned(6, 0, false), Packed(5, 0, false));
        Assert.Equal(Bytes(0x00, 0x45, 0x6F), receiver);
    }

    [Fact]
    public void DisplayToPacked_TailOfSender_ReadsWhatItJustWrote()
    {
        // The same sender, the receiver over its LAST three bytes: 23300 (not 23000, "clear then convert"; not 23456):
        // the second pair is formed from the byte the first pair has just written (0x23 << 4 = 0x30).
        string area = Area("123456");
        string receiver = OverlappingMove.DisplayToPacked("123456", area.Substring(3, 3), 3, Zoned(6, 0, false), Packed(5, 0, false));
        Assert.Equal(Bytes(0x23, 0x30, 0x0F), receiver);
    }

    [Fact]
    public void DisplayToDisplay_ReceiverRightOfTheSendersDigits_SmearsTheFirstDigit()
    {
        // 9(6) holding 123456; PIC 9(4) over bytes 4 to 7: each digit written is the next one read, so 3333.
        string area = Area("123456");
        string receiver = OverlappingMove.DisplayToDisplay("123456", area.Substring(3, 4), 3, Zoned(6, 0, false), Zoned(4, 0, false));
        Assert.Equal("3333", receiver);
    }

    [Fact]
    public void DisplayToDisplay_ReceiverOverTheTail_ReadsZeros()
    {
        // The golden's NUM-MOVE shape: PIC 9(3) over the last three digits of 9(4) holding 1234.
        string receiver = OverlappingMove.DisplayToDisplay("1234", "234", 1, Zoned(4, 0, false), Zoned(3, 0, false));
        Assert.Equal("000", receiver);
    }

    [Fact]
    public void DisplayToDisplay_SignedSender_PunchesTheSignOntoWhateverTheSignByteHolds()
    {
        // S9(4) holding -1234 ("123t"); S9(5) over bytes 2 to 6. libcob reads the sign first (negative), stores, then
        // punches the sender's last byte AND the receiver's last byte negative: "01p1p".
        string area = Area("123t");
        string receiver = OverlappingMove.DisplayToDisplay("123t", area.Substring(1, 5), 1, Zoned(4, 0, true), Zoned(5, 0, true));
        Assert.Equal("01p1p", receiver);
    }

    [Fact]
    public void DisplayToDisplay_IbmConvention_PunchesBothSignsWithTheIbmCharacters()
    {
        // The same move under the IBM convention (GnuCOBOL -fsign=EBCDIC): -1234 is "123M"; the signs punched onto
        // the zeros are the IBM negative zero, '}'.
        string area = Area("123M");
        string receiver = OverlappingMove.DisplayToDisplay("123M", area.Substring(1, 5), 1,
            Zoned(4, 0, true, enc: SignEncoding.Ibm), Zoned(5, 0, true, enc: SignEncoding.Ibm));
        Assert.Equal("01}1}", receiver);
    }

    [Fact]
    public void AlphanumericToDisplay_ReceiverRightOfTheSendersDigits_SmearsLikeTheNumericSender()
    {
        // X(6) holding "123456"; PIC 9(4) over bytes 4 to 7: the alphanumeric scan finds the sign, the digit count
        // and the first digit before it writes, and then writes each digit it reads back: 3333.
        string area = Area("123456");
        string receiver = OverlappingMove.AlphanumericToDisplay("123456", area.Substring(3, 4), 3, Zoned(4, 0, false), false);
        Assert.Equal("3333", receiver);
    }

    [Fact]
    public void PackedToDisplay_ReadsTheSignFromTheSendersLastByteAfterTheStore()
    {
        // S9(7) PACKED-DECIMAL holding -1234567 (12 34 56 7D); S9(4) SIGN LEADING SEPARATE over its first five bytes.
        // The digits are unpacked first (4567), but the sign nibble is read after the store from the byte the receiver
        // has overwritten with '7': positive, so "+4567" where the snapshot gives "-4567".
        string sender = Bytes(0x12, 0x34, 0x56, 0x7D);
        string receiver = OverlappingMove.PackedToDisplay(sender, Area(sender).Substring(0, 5), 0, Packed(7, 0, true),
            Zoned(4, 0, true, NumericSign.LeadingSeparate));
        Assert.Equal("+4567", receiver);
    }

    [Fact]
    public void DisplayToPacked_SignedSenderUnderTheIbmConvention_StoresTheSenderSignThenRepunchesIt()
    {
        // S9(6) holding -123456 under the IBM convention is "12345O"; S9(5) PACKED-DECIMAL over bytes 3 to 5. The sign is
        // read first (negative) and the sender's sign byte un-punched, the receiver cleared and written, the sender's
        // last byte re-punched negative, and the receiver's sign nibble set from the sign read first (D): 20 00 6D.
        string area = Area("12345O");
        string receiver = OverlappingMove.DisplayToPacked("12345O", area.Substring(2, 3), 2,
            Zoned(6, 0, true, enc: SignEncoding.Ibm), Packed(5, 0, true));
        Assert.Equal(Bytes(0x20, 0x00, 0x6D), receiver);
    }
}
