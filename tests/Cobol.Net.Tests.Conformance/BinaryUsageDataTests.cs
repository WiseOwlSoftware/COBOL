// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// USAGE BINARY-CHAR / -SHORT / -LONG / -DOUBLE [SIGNED|UNSIGNED] (ISO §13.18.60.4 GR12/GR21) — Phase 4
/// M2-DATA-1. PICTURE-less native two's-complement integers of 1/2/4/8 bytes (SIGNED default; UNSIGNED widens
/// the positive range) on the COMP-5 BinaryCapacity discipline. The happy-path end-to-end behavior (all four
/// widths, DISPLAY, arithmetic) rides the <c>binary_usage</c> conformance golden; the byte-width wrap arithmetic
/// is unit-tested in <c>BinaryCapacityTests</c>. This locks the COMPOSITION the golden does not: an OUT-OF-RANGE
/// arithmetic result actually WRAPS (signed) or stays in the wider UNSIGNED range end-to-end — proving the
/// emitter threads the byte-width truncation discipline into a real store, not just the in-range display width.
/// </summary>
public sealed class BinaryUsageDataTests
{
    [Fact]
    public void BinaryCapacity_SignedWraps_UnsignedWidens_EndToEnd()
    {
        // COMPUTE routes the product through CobolNum.Store with the receiver's BinaryCapacity NumProfile. The
        // same value 200 (= 100 * 2) is stored into a SIGNED BINARY-CHAR (range −128..127 — 200 wraps to
        // 200 − 256 = −56) and an UNSIGNED BINARY-CHAR (range 0..255 — 200 is in range). Implied DISPLAY
        // width 3 (§13.18.60.4 GR12): −56 renders "-056", 200 renders "200".
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BINWRAP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BC  BINARY-CHAR.
            01 BCU BINARY-CHAR UNSIGNED.
            01 N   PIC 9(3) VALUE 100.
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE BC = N * 2.
                COMPUTE BCU = N * 2.
                DISPLAY "S=" BC " U=" BCU.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("S=-056 U=200", stdout);
    }
}
