// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE PADDING OF AN ELEMENT OF A DYNAMIC-CAPACITY BIT TABLE IS NOT AN IMPLICIT FILLER ITEM, AND IT IS NOT STORED
/// (kb/Work PB3598, the owner may overturn it; <c>docs/CONFORMANCE.md</c> USAGE BIT). An element of <c>05 T PIC 1(3) USAGE BIT
/// OCCURS DYNAMIC</c> is out-of-line storage of its own — ISO §8.5.1.9.1 3): "table data items are not required to be
/// contiguous in physical memory. The size, location, and time of physical allocation of a dynamic-capacity table are
/// implementor-defined" — so §8.5.1.6.3's filler, which advances alignment "to a required natural boundary for the next item within that
/// group", has no next item to advance to: the five bits that round a 3-bit element up to its one character are the rounding of
/// its length (§15.14.4 4: "When argument-1 does not occupy an integral number of bytes, the returned value is rounded to the
/// next larger integer value"), the same rounding a level 77 or level 01 elementary 3-bit item has, which §8.5.1.6.3's NOTE says generates no filler. A group
/// MOVE carries the table's elements, not those bits (§14.6.9.2, which applies to a dynamic-capacity table: "Correspondingly
/// numbered elements are moved according to the rules of the MOVE statement"), so no operation of the language can write them.
///
/// <para>The witness is INITIALIZE ... WITH FILLER, the one statement that writes every implicit filler item: it sets the
/// one-bit item's filler (stored: the group's first character is the bit 1 and its seven filler positions 1000000, 0xC0) and the
/// elements' three positions, and leaves the five bits that pad each element zero (0x80), where a stored pad would read back
/// 0x9F. It is a unit test, not a conformance case: the expectation is the implementor's determination, never the standard's.</para>
/// </summary>
public sealed class DynamicBitElementPadTests
{
    /// <summary>Dynamic-capacity tables are a COBOL-2014 introduction.</summary>
    private const int Edition = 2014;

    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB3598PAD.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 G1.
                  05 A1 PIC 1 USAGE BIT.
                  05 T1 PIC 1(3) USAGE BIT OCCURS DYNAMIC CAPACITY IN C1.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   MOVE B"001" TO T1 (1)
                   MOVE B"011" TO T1 (2)
                   INITIALIZE G1 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
                   DISPLAY "G1 [" G1 "]"
                   STOP RUN.
        """;

    [Fact]
    public void InitializeWithFiller_WritesTheOneBitItemsFiller_NotTheElementPadding()
    {
        var (ok, output, detail) = new WiseOwlCobolCompiler(Edition).CompileAndRun(Source);
        Assert.True(ok, detail);
        // A1 = 1 then its filler 1000000 (0xC0); each element 100 (B"1" left-justified in three positions) then five zero bits (0x80).
        Assert.Contains("G1 [À\u0080\u0080]", output);
    }
}
