// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.93/§15.94 TEST-NUMVAL/-C position-reporting scanners (P11 Step 6) — the legs the conformance
/// golden <c>intrinsics_test_validators</c> cannot reach: the DECIMAL-POINT IS COMMA role swap (§15.67.3 r5
/// substitutes for NUMVAL; §15.68.3 r4d SWAPS both separator roles for NUMVAL-C), the SDIDI 34-digit cap
/// (§15.93.4 r1b sub-note 4 — the golden's compilation unit is native-arithmetic), and the b)-vs-c)-leg
/// discrimination for digit-free strings. Verdict values hand-derived in PHASE-11-scout-notes.md
/// (spec:validators).
/// </summary>
public sealed class TestNumvalScannerTests
{
    [Fact]
    public void CommaMode_SwapsSeparatorRoles()
    {
        // NUMVAL under DECIMAL-POINT IS COMMA (§15.67.3 r5): the comma IS the decimal separator...
        Assert.Equal(0, CobolIntrinsics.TestNumval("1,5", commaMode: true));
        // ...and the period is no longer a NUMVAL character at all.
        Assert.Equal(2, CobolIntrinsics.TestNumval("1.5", commaMode: true));
        // NUMVAL-C (§15.68.3 r4d): BOTH roles swap — '.' groups, ',' is the decimal point.
        Assert.Equal(0, CobolIntrinsics.TestNumvalC("$1.234,56", "$", commaMode: true));
        // A grouping separator after the decimal separator is not part of the format (§15.68.4 r2 scope):
        // in "$1,23.4" under comma mode the ',' is the decimal, so the '.' at position 6 is first in error.
        Assert.Equal(6, CobolIntrinsics.TestNumvalC("$1,23.4", "$", commaMode: true));
    }

    [Fact]
    public void DigitCap_FollowsArithmeticMode()
    {
        string d40 = new('1', 40);
        Assert.Equal(32, CobolIntrinsics.TestNumval(d40));                 // r1b sub-note 2 — native 31-digit cap
        Assert.Equal(35, CobolIntrinsics.TestNumval(d40, digitCap: 34));   // r1b sub-note 4 — SDIDI 34-digit cap
        Assert.Equal(0, CobolIntrinsics.TestNumval(new string('1', 34), digitCap: 34));
    }

    [Fact]
    public void NoDigit_BLegAtOffendingChar_CLegAtEnd()
    {
        // A digit-free scan that BROKE on a real character is r1b at that character ('$-123': the sign
        // may not follow the currency — neither §15.68.3 format)...
        Assert.Equal(2, CobolIntrinsics.TestNumvalC("$-123", "$"));
        // ...while a valid-but-incomplete prefix that ran off the end is r1c LENGTH+1 (a bare currency).
        Assert.Equal(2, CobolIntrinsics.TestNumvalC("$", "$"));
        Assert.Equal(4, CobolIntrinsics.TestNumval(" +."));
    }

    [Fact]
    public void AnycaseCurrency_FoldsPerR4f()
    {
        Assert.Equal(1, CobolIntrinsics.TestNumvalC("usd 12", "USD"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalC("usd 12", "USD", anycase: true));
        // The value parser mirrors the fold (§15.94.1 — TEST verifies what NUMVAL-C would accept).
        Assert.Equal((Int128)12, CobolIntrinsics.NumvalCDec("usd 12", "USD", anycase: true).ToUnscaled(0, CobolRounding.Truncation));
    }
}
