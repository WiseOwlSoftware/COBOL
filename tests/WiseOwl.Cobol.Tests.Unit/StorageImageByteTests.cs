// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ A CHARACTER BECOMES A BYTE AT AN IN-MEMORY BYTE CHANNEL IN EXACTLY ONE PLACE, AND A CHARACTER WITH NO BYTE
/// IMAGE IS NEVER SILENTLY TRUNCATED (kb/Work PB547). An alphanumeric item holds a UTF-16 code unit (docs/CONFORMANCE.md
/// DOC-A.1-31, -33), one byte per position at every byte channel (DOC-A.1-209, ISO/IEC 1989:2023 §8.1.2 1): the storage
/// image a REDEFINES view reads (§13.18.44.4 GR1: one storage area) must give every character the same number of bytes,
/// so a character above U+00FF other than HIGH-VALUE reads as <c>?</c> (0x3F) and sets EC-DATA-CONVERSION (DOC-A.1-46:
/// "visible under checking, never silent"), the disposition decided for CONVERT's byte legs. Before PB547 the national,
/// bit and numeric views cast the code unit to a byte: U+20AC read as 0xAC, so a national view of "€ABC" held U+AC41 and
/// a write back through it turned the euro into U+00AC. Code units are spelled as numbers, never as characters, so no
/// editor or tool can fold one into another.</summary>
public sealed class StorageImageByteTests
{
    private static string Units(params int[] codeUnits) => new([.. codeUnits.Select(u => (char)u)]);

    private static readonly string EuroAbc = Units(0x20AC, 'A', 'B', 'C');

    /// <summary>Every code unit above U+00FF other than HIGH-VALUE has no byte image and answers the substitute; the 257
    /// that do (U+0000-U+00FF and U+FFFF) answer their own byte — one law over the whole 65,536-unit repertoire.</summary>
    [Fact]
    public void ToByte_IsTheLaw_OverTheWholeRepertoire()
    {
        for (int c = 0; c <= 0xFFFF; c++)
        {
            char ch = (char)c;
            byte expected = StorageByte.HasByte(ch) ? (byte)c : (byte)'?';
            Assert.Equal(expected, StorageByte.ToByte(ch));
        }
        Assert.Equal(0xFF, StorageByte.ToByte(NativeCollatingSequence.HighValue));
        Assert.Equal(0xFF, StorageByte.ToByte((char)0xFF));
        Assert.Equal((byte)'?', StorageByte.ToByte((char)0x20AC));
    }

    /// <summary>The national view of an alphanumeric item holding a wide character: the euro's byte is '?', so the pair
    /// 3F 41 is U+3F41 (not U+AC41) and EC-DATA-CONVERSION is set while checking is enabled.</summary>
    [Fact]
    public void NatReadWindow_ReadsAWideCharacterAsTheSubstituteByte_AndSetsDataConversion()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.DataConversionChecking = true;
            Assert.Equal(Units(0x3F41, 0x4243), CobolBits.NatReadWindow(EuroAbc, 0, 2));
            Assert.Equal("EC-DATA-CONVERSION", ExceptionState.LastName);
            Assert.False(ExceptionState.LastFatal);   // Table 13: EC-DATA-CONVERSION is nonfatal
        });
    }

    /// <summary>Checking off: the same substitute, nothing recorded — the condition is raised only while enabled
    /// (§14.6.13.1.1), the value is the same either way.</summary>
    [Fact]
    public void NatReadWindow_WithCheckingOff_SubstitutesAndRaisesNothing()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.DataConversionChecking = false;
            Assert.Equal(Units(0x3F41, 0x4243), CobolBits.NatReadWindow(EuroAbc, 0, 2));
            Assert.Null(ExceptionState.LastName);
        });
    }

    /// <summary>A write back through the national view restores the bytes it read, so the euro becomes the '?' its
    /// storage image was (DOC-A.1-33's decided disposition) — and no longer the unrelated U+00AC.</summary>
    [Fact]
    public void NatReadWindow_ThenNatWriteWindow_RoundTripsTheSubstitutedImage()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            string national = CobolBits.NatReadWindow(EuroAbc, 0, 2);
            Assert.Equal("?ABC", CobolBits.NatWriteWindow("????", 0, 2, national));
        });
    }

    /// <summary>A bit write into a byte holding a wide character reads that byte through the same funnel: the euro's
    /// image is '?' (0011 1111), and setting bit 1 gives 0111 1111 = U+007F, with the condition recorded.</summary>
    [Fact]
    public void BitWindowWrite_ReadsTheWideCharacterAsTheSubstituteByte()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.DataConversionChecking = true;
            Assert.Equal(Units(0x7F), CobolBits.WriteWindow(Units(0x20AC), 1, "1"));
            Assert.Equal("EC-DATA-CONVERSION", ExceptionState.LastName);
        });
    }

    /// <summary>The bit view and the whole-image national decode read through the same funnel.</summary>
    [Fact]
    public void BitAndWholeImageReaders_SubstituteAndSetDataConversion()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.DataConversionChecking = true;
            // '?' is 0x3F = 0011 1111.
            Assert.Equal("00111111", CobolBits.ReadWindow(Units(0x20AC), 0, 8));
            Assert.Equal("EC-DATA-CONVERSION", ExceptionState.LastName);

            ExceptionState.Clear();
            Assert.Equal(Units(0x3F41), CobolBits.NatFromStorage(Units(0x20AC, 'A')));
            Assert.Equal("EC-DATA-CONVERSION", ExceptionState.LastName);
        });
    }

    /// <summary>The characters that DO have a byte image raise nothing — U+0000-U+00FF and the HIGH-VALUE character (the
    /// byte 0xFF): the common case stays silent and exact, which is what keeps <c>MOVE HIGH-VALUES TO REC</c> followed
    /// by a national, bit or binary view free of spurious conditions.</summary>
    [Fact]
    public void ImageReaders_RaiseNothingForACharacterWithAByteImage()
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            ExceptionState.DataConversionChecking = true;
            Assert.Equal(Units(0xFFFF, 0xFFFF), CobolBits.NatReadWindow(Units(0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF), 0, 2));
            Assert.Equal(Units(0x00FF), CobolBits.NatReadWindow(Units(0x00, 0xFF), 0, 1));
            Assert.Equal(Units(0xE9 << 8 | 'A'), CobolBits.NatReadWindow(Units(0xE9, 'A'), 0, 1));
            Assert.Equal("11111111", CobolBits.ReadWindow(Units(0xFF), 0, 8));
            Assert.Null(ExceptionState.LastName);
        });
    }
}
