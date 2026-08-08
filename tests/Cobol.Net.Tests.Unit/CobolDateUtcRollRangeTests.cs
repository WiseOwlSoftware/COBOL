// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §15.40.4 r2 UTC roll against the integer-date-form range (kb/Work R25) — the EMIT-side sibling of
/// PB23's analyzer-side crash: every argument individually legal (the §15.40.3 r4/r5 screens pass), and the
/// r2 adjustment then carries the DATE outside 1..3,067,671 (§15.5.2). Before the fix,
/// <c>FORMATTED-DATETIME("YYYYMMDDThhmmssZ", 3067671, 86399, -1439)</c> threw a raw CLR
/// <c>ArgumentOutOfRangeException</c> out of <c>Epoch.AddDays</c>, and the low-end mirror emitted year 1600
/// (§15.3.1.3 requires "greater than 1600"). §15.3 permits only EC-ARGUMENT-FUNCTION or the default.
/// <para>⚠ These run with EC checking OFF, so the error surfaces as the §15.3 default "" — the RAISE is
/// pinned end-to-end by the golden <c>r25_utc_roll_date_range</c>.</para>
/// </summary>
public sealed class CobolDateUtcRollRangeTests
{
    [Fact]
    public void RollPastTheMaxIntegerDate_IsTheDefault_NotACrash()
    {
        // 86399 s + 1439 min westward = past midnight of 9999-12-31 → integer date 3,067,672: no form.
        Assert.Equal("", CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 3067671, 86399, 0, -1439, true));
    }

    [Fact]
    public void RollBelowDayOne_IsTheDefault_NotYear1600()
    {
        // 0 s − 1439 min eastward = before midnight of 1601-01-01 → integer date 0: §15.3.1.3 bars 1600.
        Assert.Equal("", CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 1, 0, 0, 1439, true));
    }

    [Fact]
    public void LegalRollsAcrossTheBoundary_StillEmit()
    {
        // One day inside each end: the same offsets roll INTO range and must keep emitting.
        Assert.Equal("99991231T235859Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 3067670, 86399, 0, -1439, true));
        Assert.Equal("16010101T000100Z",
            CobolDate.FormattedDatetime("YYYYMMDDThhmmssZ", 2, 0, 0, 1439, true));
    }

    [Fact]
    public void FormattedTime_WithADateBearingFormat_ReachesTheSameGuard()
    {
        // FORMATTED-TIME shares EmitFormatted with day = 1; a combined format's eastward roll hits day 0.
        Assert.Equal("", CobolDate.FormattedTime("YYYYMMDDThhmmssZ", 0, 0, 1439, true));
    }

    [Fact]
    public void TimeOnlyFormats_RollFreely_TheGuardIsDateGated()
    {
        // A time-only format never reads the day (the §15.41 normal case) — the roll must stay unguarded.
        Assert.Equal("000100Z", CobolDate.FormattedTime("hhmmssZ", 0, 0, 1439, true));
    }
}
