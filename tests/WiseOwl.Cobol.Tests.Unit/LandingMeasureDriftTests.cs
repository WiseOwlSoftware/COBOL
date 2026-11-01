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
/// the harness's fixed per-process budget; <c>scripts/self_test_arms.py</c> holds its arms by name (kb/Work PB4395), so
/// an arm deleted from the self-test (which would still exit 0) is a red there. This test measures nothing and times
/// nothing.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LandingMeasureDriftTests
{
    /// <summary>The files that may not name the retired writer: the whole tree but the narrative paths (docs, the work
    /// register, the history, tests) and the replacement, which names the script it replaced once, in its docstring.
    /// Declared (kb/Work PB3531), so the drift-rule index names this class for each of them. The knowledge base is set
    /// aside whole: a sweep that read it made the vault a test input (kb/Work PB2878).</summary>
    private static readonly TreeScope NotNarrative = new(Under: [""],
        Except: ["docs/", "kb/", "adjudication/", "specs/", "tests/", "DEVLOG/", "scripts/orchestrator/landing_measure.py"]);

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
        Assert.Contains("measure=lambda wt, branch, pr: measure_landing(wt, branch, pr, None, 0)", source);
        Assert.Contains("landing_measure.record_enqueued(", source);
    }
}
