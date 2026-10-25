// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FILES TWO LANDINGS BOTH EDIT ARE MERGED BY DRIVER, AND ONE FUNCTION MERGES MAIN IN (kb/Work PB3610;
/// docs/rearchitecture/DESIGN-test-build-ci.md §3.17.2 step 2). <c>scripts/merge_drivers.py</c> keeps a registry
/// (<c>DRIVERS</c>) of the catalogues a landing appends to — <c>wiseowl/diagnostics.json</c> and <c>options.json</c>, the
/// traceability inventory, <c>constructs.json</c>, the work notes — and of the runtime ABI pin, which
/// <c>scripts/abi_pin.py</c> measures; <c>.gitattributes</c> repeats its patterns, and <c>--check</c> holds the two
/// together. <c>enqueue.py land</c> merges main in through <c>merge_drivers.merge</c>, so two branches that each added an
/// entry are no conflict; the review's coverage rule (<c>prune_worktrees.mechanical</c>) reads the same function, so a
/// merge the drivers made reads as mechanical and a hand resolution does not.
/// </summary>
/// <remarks>
/// Until PB3610 these landings stopped at CONFLICT and cost a finisher cycle each (the landing logs counted
/// <c>tests/conformance/2002/manifest.json</c> fifteen times, <c>negative/manifest.json</c> thirteen,
/// <c>RuntimeAbiPinDriftTests.cs</c> and <c>wiseowl/diagnostics.json</c> four each). The facts hold the structure, not the
/// arithmetic: the two scripts' self-tests run in every gate and in CI (the self-test runner), so these keep the arms
/// declared and the registry, the attributes and the callers one thing.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class MergeDriverDriftTests
{
    private const string Drivers = "scripts/merge_drivers.py";
    private const string AbiPin = "scripts/abi_pin.py";

    /// <summary>Both scripts' self-tests still declare an arm for each rule (held from the source, never run a second
    /// time: the self-test runner runs them, DESIGN-test-build-ci.md section 3.14.10).</summary>
    [Fact]
    public void TheSelfTests_DeclareEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.At(Drivers), [
            "keyed: entries each side ADDED are both kept",
            "keyed: an entry both sides changed DIFFERENTLY is a real conflict",
            "keyed: a raw",
            "keyed rows: an array of objects is keyed by a member",
            "keyed nested: entries at a deeper indentation inside a member are keyed too",
            "note: frontmatter merges per key and the bodies by union",
            "git: WITHOUT the drivers the same merge conflicts (the premise)",
            "git: merge() brings main in through the drivers",
            "git: merge-tree with config_args reproduces the merge's tree",
            "git: keep leaves a conflicting merge IN PROGRESS",
            "pin: both branches raised the major 17 -> 18: the merge makes it 19",
            "pin: a settle that fails aborts the merge and names the pin",
            "coverage: is_drivers_merge accepts the merge with its pin settled",
            "coverage: a merge whose pin is MEASURED (its header names UNMEASURED only in prose)",
            "conflicts: the dry run lists nothing where the drivers merge",
            "conflicts: a keyed file whose driver DECLINES (one entry both sides changed differently) is listed",
            "conflicts: a ref git cannot read is None",
            "conflicts: the landing logs' CONFLICT lines name their files",
            "check: the registry and this repository's .gitattributes agree",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At(AbiPin), [
            "merge: a counter both sides raised by one is raised by two",
            "unmeasured: a pin whose header names UNMEASURED only in its prose is measured",
            "settle: the merged counters are written into the props and RuntimeAbi.cs BEFORE the measurement",
            "settle: a measurement that fails, or writes nothing, is the reason",
            "bump: the named counter rises by one",
        ]);
        // The verdict line's one home (kb/Work PB3714): the CONFLICT outcome's writer and readers live there.
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/verdict_line.py"), [
            "the line is the prefix, the outcome, the branch in parentheses and the terminator",
            "the reader reads back what the writer wrote, outcome and branch",
            "a line that is not a verdict line parses to None",
            "outcomes: every verdict line of a log in order",
            "the CONFLICT outcome reads back its own files",
            "conflict_files: the files the CONFLICT lines of a log name",
        ]);
    }

    /// <summary>The registry and <c>.gitattributes</c> agree both ways, and every driver's pattern names a tracked file.</summary>
    [Fact]
    public void TheDriverRegistry_AgreesWithGitattributes()
    {
        var r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.At(Drivers), "--check");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== MERGE DRIVERS: GREEN ===", StringComparison.Ordinal),
            $"`merge_drivers.py --check` found the registry and .gitattributes apart (exit {r.ExitCode}): add the pattern to both.\n{r.Stdout}\n{r.Stderr}");
    }

    /// <summary>
    /// Main is merged in through the drivers at every site that merges it: the landing (<c>enqueue.merge_main</c>), the
    /// review's coverage rule (<c>prune_worktrees.mechanical</c>) and the predecessor-merge instructions the planner and the
    /// rolling wave give an agent — none spells a bare <c>git merge</c>, which would make a keyed catalogue conflict again.
    /// The landing lane's conflict brief foresees the merge through the same drivers (<c>merge_drivers.merge_conflicts</c>,
    /// kb/Work PB3640 review R1-1): a plain <c>git merge-tree</c> there, with every driver-covered file dropped, read a keyed
    /// file the driver DECLINES (a real conflict) as nothing to brief. Enqueue's CONFLICT outcome has one writer and one
    /// reader (<c>verdict_line.conflict_outcome</c> / <c>outcome_files</c>, the verdict line's one home, kb/Work PB3714).
    /// </summary>
    [Fact]
    public void EverySiteThatMergesMain_GoesThroughTheDrivers()
    {
        string enqueue = File.ReadAllText(TestRepo.At("scripts/orchestrator/enqueue.py"));
        Assert.Contains("merge_drivers.merge(wt, base,", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("\"merge\", \"--no-edit\"", enqueue, StringComparison.Ordinal);
        Assert.Contains("merge_drivers.is_drivers_merge(", File.ReadAllText(TestRepo.At("scripts/prune_worktrees.py")), StringComparison.Ordinal);
        Assert.Contains("verdict_line.conflict_outcome(files)", enqueue, StringComparison.Ordinal);
        string lane = File.ReadAllText(TestRepo.At("scripts/orchestrator/land_lane.py"));
        Assert.Contains("merge_drivers.merge_conflicts(", lane, StringComparison.Ordinal);
        Assert.Contains("verdict_line.outcome_files(", lane, StringComparison.Ordinal);
        Assert.True(!lane.Contains("\"merge-tree\"", StringComparison.Ordinal) && !lane.Contains("CONFLICT in ", StringComparison.Ordinal),
            "scripts/orchestrator/land_lane.py runs its own `git merge-tree` or parses `CONFLICT in` itself: foresee the merge with " +
            "merge_drivers.merge_conflicts and read the outcome with verdict_line.outcome_files (kb/Work PB3640 review R1-1, R1-3, PB3714)");
        foreach (string rel in new[] { "scripts/orchestrator/plan_wave.py", ".claude/skills/workstream/templates/wf_rolling_wave.js",
                     ".claude/skills/workstream/templates/finisher-brief.md", ".claude/skills/workstream/templates/golden-lane-brief.md" })
        {
            string text = File.ReadAllText(TestRepo.At(rel));
            Assert.True(!text.Contains("`git merge ", StringComparison.Ordinal) && !text.Contains("\\`git merge ", StringComparison.Ordinal),
                $"{rel} tells an agent to run a bare `git merge`: use `python scripts/merge_drivers.py merge <ref>` (kb/Work PB3610)");
            Assert.Contains("merge_drivers.py merge", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// ⛔ THE MERGE QUEUE MERGES WITHOUT THE DRIVERS, SO TWO LANDINGS OF ONE DRIVER FILE NEVER QUEUE TOGETHER (kb/Work
    /// PB3642): GitHub builds its merge groups server-side with plain git, so two open pull requests that both change a
    /// keyed catalogue or a work note conflict inside the queue although each merged main through the drivers. <c>enqueue.py
    /// land</c> therefore reads the open pull requests (<c>driver_file_holders</c>, which asks <c>merge_drivers.covered_by_driver</c>,
    /// so a new <c>DRIVERS</c> entry is guarded with no new line) before it stamps or queues, ends exit 16 while one shares
    /// a driver-covered file with the branch, holds the repository's queue-entry lock from that look to the queueing, and the
    /// landing lane retries exit 16 once none does. The self-tests drive every arm, including one branch that changes the
    /// sample file of EVERY driver pattern; this holds the wiring and that those arms stay declared.
    /// </summary>
    [Fact]
    public void TheLanding_WaitsForAnOpenPullRequestThatSharesADriverFile()
    {
        string enqueue = File.ReadAllText(TestRepo.At("scripts/orchestrator/enqueue.py"));
        Assert.Contains("driver_file_holders(wt, base, branch, gh=gh)", enqueue, StringComparison.Ordinal);
        Assert.Contains("merge_drivers.covered_by_driver(p)", enqueue, StringComparison.Ordinal);
        Assert.Contains("take_queue_entry_lock(wt,", enqueue, StringComparison.Ordinal);
        Assert.Contains("EXIT_DRIVER_FILE_QUEUED,", enqueue, StringComparison.Ordinal);
        string lane = File.ReadAllText(TestRepo.At("scripts/orchestrator/land_lane.py"));
        Assert.Contains("EXIT_DRIVER_FILE_QUEUED:", lane, StringComparison.Ordinal);
        Assert.Contains("enqueue.driver_file_holders(", lane, StringComparison.Ordinal);
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/orchestrator/enqueue.py"), [
            "driver files: for EVERY pattern of merge_drivers.DRIVERS, an open pull request that changes the pattern's file",
            "driver files: a pull request changing other files, or the branch's own pull request",
            "driver files: a branch that changes no driver-covered file never asks GitHub",
            "land: a branch whose driver-merged file an open pull request changes too waits: exit 16",
            "land: GitHub that cannot list the open pull requests is exit 9",
            "land: another landing holding the queue-entry lock past the wait is exit 2",
            "land: once the open pull request has merged (none lists the file) the same branch is stamped and queued",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/orchestrator/land_lane.py"), [
            "exit 16 (an open pull request changes a merge-driver file the branch changes too): a retry-when-free class",
        ]);
    }

    /// <summary>The pin is a measured file nobody hand-edits: it holds exactly the four lines the test reads and never
    /// UNMEASURED on a committed tree (a merge settles it before its commit).</summary>
    [Fact]
    public void TheAbiPin_IsMeasuredOnTheTree()
    {
        string[] values = [.. File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "RuntimeAbi.pin")).Where(l => l.Length > 0 && l[0] != '#')];
        Assert.Equal(["runtime-major", "runtime-hash", "call-abi", "boundary-hash"], values.Select(l => l.Split(' ')[0]));
        Assert.DoesNotContain(values, l => l.Contains("UNMEASURED", StringComparison.Ordinal));
    }
}
