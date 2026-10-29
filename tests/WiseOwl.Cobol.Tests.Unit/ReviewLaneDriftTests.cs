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
/// every gate and in CI; this holds that no arm of it was deleted.</para>
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

    [Fact]
    public void TheReviewLane_DeclaresEveryArm_OfItsSelfTest()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "review_lane.py"), new[]
        {
            "a branch WAITING FOR REVIEW starts ONE reviewer, and no other state does",
            "the reviewer runs the role's own definition in a headless session, the prompt on stdin",
            "no agent starts under {what}",
            "no agent starts under a budget hold (the lane spends the loop's quota)",
            "a survey older than SURVEY_MAX_AGE_MIN launches nothing",
            "a branch that moved since the survey is left for the next tick",
            "at most max-agents agents run at once, in survey order",
            "a wave reviewer's `start` at the head (no verdict, young) is a lease: no second reviewer",
            "the same `start` past the reviewer ceiling is a dead reviewer's: the lane reviews",
            "a verdict at the head in any findings file means reviewed: nothing starts",
            "a report younger than REPORT_GRACE_MIN waits: a rolling wave's reviewer may be about to write its `start`",
            "a verdict in the lane's own file with no stamp in the report is stamped by the lane, not reviewed again",
            "the next tick reaps it DIED, mends the cut-off append and starts the SAME round again, with no hand repair",
            "the restart's prompt lists what the dead reviewer recorded and does not record it again",
            "a tear before more lines is the operator's: a HOLD, nothing started, the file untouched",
            "a branch that moved past a reviewed head is round 2, a RE-REVIEW of the diff since that head",
            "after ATTEMPT_LIMIT reviewers with no verdict at one head the lane HOLDS, starts none, and asks for the unit",
            "an exit-4 stop and a DIRTY bounce each start a conflict finisher; a bounce for failed checks does not",
            "the lane collects the exit-4 attempt it started a finisher for",
            "ONE finisher per head: the same conflict at the same head starts none",
            "a finisher that ended (here DIED) with the head where it found it is a HOLD for the enqueue unit",
            "never ticked and a branch waits: the unit ticks it once",
            "an agent past its CEILING is killed and recorded STALLED",
            "a loop stop ends a running agent, recorded STOPPED",
        });
    }
}
