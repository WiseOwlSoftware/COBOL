// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE LOOP'S REVIEW LANE REVIEWS AND FINISHES CONFLICTS BESIDE THE LANDING LANE, AND THE ENQUEUE UNIT STARTS NO REVIEWER
/// (kb/Work PB3608; DESIGN-orchestrator-loop.md section 4.9.1): <c>orchestrate.ps1</c> starts <c>review_lane.py tick</c> as a
/// BACKGROUND process from <c>Start-ReviewTick</c>, which <c>Complete-LaneTick</c> calls the moment a landing-lane tick
/// ends (the review lane reads the survey that tick wrote), and <c>enqueue.md</c> step 2 reads <c>review_lane.py status</c>.
/// </summary>
/// <remarks>
/// <para>Before the lane a branch DONE outside a rolling wave waited for the next enqueue unit to review it; units run one
/// at a time and ran 3,308 s, 4,579 s and over two hours on 2026-10-23, so a branch DONE at 14:00 was still WAITING FOR
/// REVIEW at 16:00. Three drifts would bring the wait back without a red anywhere else: a supervisor that no longer
/// starts the lane, an enqueue unit prompt that dispatches reviewers again (two reviewers racing for one branch), and a
/// tick awaited on the supervisor's event loop (kb/Work PB2590's defect, for the checkpoint).</para>
/// <para>The lane's own decisions (the lease, the restart, the rounds, the cap, the stops, the budget, the conflict
/// finisher, the runner's ceiling) are <c>review_lane.py --self-test</c>'s, which the self-test runner discovers and runs in
/// every gate and in CI; <c>scripts/self_test_arms.py</c> holds that no arm of it was deleted (kb/Work PB4395).</para>
/// </remarks>
public sealed class ReviewLaneDriftTests
{
    [Fact]
    public void TheSupervisor_StartsTheReviewLane_InTheBackground_AfterEveryLandingTick()
    {
        string script = File.ReadAllText(TestRepo.Scripts("orchestrator", "orchestrate.ps1"));
        string start = LandingLaneDriftTests.FunctionBody(script, "Start-ReviewTick");
        Assert.True(start.Length > 0, "orchestrate.ps1 has no `function Start-ReviewTick`: the review lane is not started (kb/Work PB3608)");
        Assert.Contains("'review_lane.py'", start, StringComparison.Ordinal);
        Assert.Contains("'tick'", start, StringComparison.Ordinal);
        Assert.Contains("Resolve-Launch $ClaudeExe", start, StringComparison.Ordinal);
        Assert.True(start.Contains("Start-PyBackground", StringComparison.Ordinal) && !start.Contains("WaitForExit", StringComparison.Ordinal),
            "Start-ReviewTick must start the tick as a background process (Start-PyBackground) and never wait for it: the event "
            + "loop reads the unit's stream, ticks the startup watchdog and checks STOP (kb/Work PB2590)");

        // Chained after the landing tick: it reads the survey that tick wrote, so no second survey is taken.
        string complete = LandingLaneDriftTests.FunctionBody(script, "Complete-LaneTick");
        Assert.Matches(@"\bStart-ReviewTick\b", complete);
        string step = LandingLaneDriftTests.FunctionBody(script, "Step-Lane");
        Assert.Matches(@"\bComplete-ReviewTick\b", step);

        Assert.Matches(@"\[int\]\$ReviewAgents\s*=\s*[1-9]\d*", script);
        Assert.Contains("$ReviewAgents -le 0", start, StringComparison.Ordinal);   // -ReviewAgents 0 runs no review lane
    }

    [Fact]
    public void EnqueueUnitStep2_ReadsTheReviewLane_AndStartsNoReviewer()
    {
        string unit = File.ReadAllText(TestRepo.Scripts("orchestrator", "units", "enqueue.md"));
        Match m = Regex.Match(unit, @"^2\. .*?(?=^\d+[a-z]?\. )", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "scripts/orchestrator/units/enqueue.md has no step 2; re-point this test");
        string step2 = m.Value;
        Assert.Contains("review_lane.py status", step2, StringComparison.Ordinal);
        Assert.Contains("review_lane.py tick", step2, StringComparison.Ordinal);
        Assert.Contains("review_lane.py collect", step2, StringComparison.Ordinal);
        // The retired wording: step 2 dispatched a reviewer for EVERY WAITING FOR REVIEW row.
        Assert.DoesNotContain("REVIEW FIRST", unit, StringComparison.Ordinal);
    }
}
