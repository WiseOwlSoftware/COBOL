// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE LANDING PIPELINE IS JUDGED BY THE SAME INSTRUMENT THAT FOUND ITS PROBLEM, AND THAT INSTRUMENT IS RECORDED RIGHT
/// (kb/Work PB3028; DESIGN-test-build-ci.md §3.17.5): <c>scripts/orchestrator/landing_measure.py</c> keeps one record
/// per pull request (dispatch → DONE → review clean → enqueued → on main, each stage against the owner's fix-cycle
/// bar, kb/Work PB3638; its bounces and their causes, the merge-group CI wall
/// and the local gate's seconds), completes it from GitHub, and sets the after column beside the retired train records.
/// It replaced <c>train_measure.py</c>, which is deleted: no tracked file outside the narrative paths may name it, and
/// <c>enqueue.py queue</c> must keep writing the record.
/// </summary>
/// <remarks>
/// The script's own <c>--self-test</c> runs in every gate and in CI through <c>scripts/self_tests.py</c>
/// (<see cref="SelfTestDiscoveryDriftTests"/> holds the discovery), so this class does not run it a second time under
/// the harness's fixed per-process budget; it holds the arms by name, so an arm deleted from the self-test (which would
/// still exit 0) is a red here. The summary decides whether the redesign paid, so a miscounted figure is a wrong
/// decision: each figure's arm is named below. This test measures nothing and times nothing.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LandingMeasureDriftTests
{
    [Fact]
    public void TheLandingMeasurement_DeclaresEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "landing_measure.py"), new[]
        {
            "a record is a patch: later calls fill fields, the first enqueue time stays, and every stamp must follow the last",
            "an inconsistent record is refused: stamps out of order, an unknown cause, causes with no bounces, a bad number",
            "done is each carried change's earliest DONE report by its HEAD commit's time, never a report file's time: a "
                + "finisher keeps its predecessor's, a train's changes come from its Report lines, nothing after the queue or "
                + "already on main counts",
            "local gate seconds are the sum of every verdict.json in the worktree",
            "complete reads the merge time, counts only failed merge-group runs as bounces and takes the CI wall of the "
                + "successful run",
            "summary sets the after column beside the before column, names unattributed bounces and says UNMEASURED for a "
                + "figure it never saw",
            "the headline is the merge queue's rate from two landings and the retired trains' rate before that, each "
                + "saying what its minutes measure",
            "freeze waits for a landing, refuses to rewrite the frozen copy, and the evidence figures are in the evidence file",
            // kb/Work PB3096: a pull request's own red before the queue is measured, never a silent 0
            "complete counts the pull request's own failed runs before the queue on its head branch (pre_queue_reds), and a "
                + "branch with no pull-request run stays UNMEASURED, never 0",
            // kb/Work PB3527: the landing lane's queue watch records each stalled read, never as a bounce
            "a queue stall is recorded once per read with its cause and what the watch did, never as a bounce (complete "
                + "re-reads the bounces and keeps the stalls), a malformed stall is refused, and summary counts the stalls",
            // kb/Work PB3638: the owner's fix-cycle bar is measured per stage, each span against its budget
            "the dispatch and review-clean stamps are read at enqueue from the dispatch ledger and the CLEAN verdict's "
                + "findings record, a stamp out of order stays null, a re-enqueue after a bounce and a re-review is "
                + "bounded by the record's first enqueue, for its DONE and its CLEAN alike (its record is never refused, "
                + "kb/Work PB3710), and summary prints each stage and marks a span over the owner's bar",
        });
    }

    /// <summary>The files that may not name the retired writer: the whole tree but the narrative paths (docs, the work
    /// register, the history, tests) and the replacement, which names the script it replaced once, in its docstring.
    /// Declared (kb/Work PB3531), so the drift-rule index names this class for each of them. The knowledge base and the
    /// vault's settings are set aside whole: a sweep that read them made the vault a test input (kb/Work PB2878).</summary>
    private static readonly TreeScope NotNarrative = new(Under: [""],
        Except: ["docs/", "kb/", ".obsidian/", "adjudication/", "specs/", "tests/", "DEVLOG/", "scripts/orchestrator/landing_measure.py"]);

    /// <summary>The retired writer is gone and nothing calls it: a second writer of the old file would split the
    /// instrument. The narrative paths (docs, the work register, the history, tests) may name it.</summary>
    [Fact]
    public void TheRetiredTrainWriter_IsDeleted_AndNothingNamesIt()
    {
        Assert.False(File.Exists(TestRepo.Scripts("orchestrator", "train_measure.py")),
            "scripts/orchestrator/train_measure.py was replaced by landing_measure.py (kb/Work PB3028): delete it, never keep a second writer");
        IReadOnlyList<string> tracked = TrackedTree.Files(NotNarrative);
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        var offenders = tracked.Where(p => File.Exists(Path.Combine(TestRepo.Root, p)))
                               .Where(p => File.ReadAllText(Path.Combine(TestRepo.Root, p)).Contains("train_measure", StringComparison.Ordinal))
                               .ToList();
        Assert.True(offenders.Count == 0,
            "these files still name the retired `train_measure.py` (kb/Work PB3028): point them at `landing_measure.py`:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>The queue path records the landing itself, so no brief has to remember to.</summary>
    [Fact]
    public void TheQueueStep_WritesTheLandingRecord()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        Assert.Contains("measure=lambda branch, pr: measure_landing(cwd, branch, pr, a.changes, None)", source);
        Assert.Contains("measure=lambda wt, branch, pr, gates_run: measure_landing(wt, branch, pr, None, gates_run)", source);
        Assert.Contains("landing_measure.record_enqueued(", source);
    }
}
