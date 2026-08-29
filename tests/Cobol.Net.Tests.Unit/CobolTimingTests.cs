// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// CONTINUE AFTER's runtime lanes (ISO §14.9.9.4 GR1; kb/Work PB138) — the legs no stdout golden can pin:
/// the NON-FINITE screen (`(long)double.NaN` saturates to 0, so a NaN interval used to silently skip the
/// suspension where §14.6.13.2 item 3 names EC-DATA-NOT-FINITE), the raise REPORT the emitted §14.6.13.1.4
/// dispatch consumes, and the exact-lane truncation contract.
/// </summary>
public sealed class CobolTimingTests
{
    [Fact]
    public void NaNInterval_CheckingOff_NoSuspension_NoReport()
    {
        var sw = Stopwatch.StartNew();
        bool raised = CobolTiming.ContinueAfter(double.NaN, checkLessThanZero: false);
        sw.Stop();
        Assert.False(raised);
        Assert.True(sw.ElapsedMilliseconds < 500, $"NaN interval suspended {sw.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void NaNInterval_DataNotFiniteChecking_IsTheFatal()
    {
        var ru = new RunUnit();
        var saved = RunUnit.Current;   // the ambient accessor is thread-static; run against a fresh unit
        try
        {
            ru.Exceptions.FloatNotFiniteChecking = true;
            Assert.Throws<CobolNet.Runtime.Exceptions.CobolFatalException>(
                () => ru.Exceptions.FloatNotFiniteError("probe"));
            Assert.Equal("EC-DATA-NOT-FINITE", ru.Exceptions.LastName);
        }
        finally { _ = saved; }
    }

    [Fact]
    public void NegativeInterval_Checked_ReportsTheRaise_AndSetsTheStatus()
    {
        bool raised = CobolTiming.ContinueAfter(-0.5, checkLessThanZero: true);
        Assert.True(raised);   // the emitted site dispatches §14.6.13.1.4 on this report
    }

    [Fact]
    public void ExactLane_TruncatedZero_DoesNotSuspend_EvenWhenTheDoubleImageIsOne()
    {
        // 0.99999999999999999 (17 nines) converts to exactly 1.0 in binary64 — the sign value — while the
        // exact truncation the emitter computes in the value's own domain is 0 (GR1's implicit COMPUTE
        // without ROUNDED). The suspension must follow the EXACT value.
        var sw = Stopwatch.StartNew();
        bool raised = CobolTiming.ContinueAfterExact(1.0, truncatedSeconds: 0, checkLessThanZero: true);
        sw.Stop();
        Assert.False(raised);
        Assert.True(sw.ElapsedMilliseconds < 500, $"a truncated-zero interval suspended {sw.ElapsedMilliseconds} ms");
    }
}
