// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FILES TWO LANDINGS BOTH EDIT ARE MERGED BY DRIVER, AND ONE FUNCTION MERGES MAIN IN (kb/Work PB3610;
/// docs/rearchitecture/DESIGN-test-build-ci.md §3.17.2 step 2). <c>scripts/merge_drivers.py</c> keeps a registry
/// (<c>DRIVERS</c>) of the catalogues a landing appends to — <c>wiseowl/diagnostics.json</c> and <c>options.json</c>, the
/// traceability inventory, <c>constructs.json</c>, the work notes — of the runtime ABI pin, which
/// <c>scripts/abi_pin.py</c> measures, and of the public-API baselines, merged as SETS of symbol lines (kb/Work PB3727;
/// docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.9); <c>.gitattributes</c> repeats its patterns, and <c>--check</c> holds the two
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
            "publicapi: lines each side added are both kept",
            "publicapi: a line one side removed is gone",
            "publicapi: the header stays first and the rest is sorted",
            "publicapi: the same line added by both is one line",
            "git: a public-API baseline both branches changed merges through its driver as a set",
            "patterns: a driver's pattern names a path as git does",
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
            "conflicts: the one CONFLICT outcome reads back its own files",
            "check: the registry and this repository's .gitattributes agree",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At(AbiPin), [
            "merge: a counter both sides raised by one is raised by two",
            "unmeasured: a pin whose header names UNMEASURED only in its prose is measured",
            "settle: the merged counters are written into the props and RuntimeAbi.cs BEFORE the measurement",
            "settle: a measurement that fails, or writes nothing, is the reason",
            "bump: the named counter rises by one",
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
    /// reader (<c>merge_drivers.conflict_outcome</c> / <c>outcome_files</c>).
    /// </summary>
    [Fact]
    public void EverySiteThatMergesMain_GoesThroughTheDrivers()
    {
        string enqueue = File.ReadAllText(TestRepo.At("scripts/orchestrator/enqueue.py"));
        Assert.Contains("merge_drivers.merge(wt, base,", enqueue, StringComparison.Ordinal);
        Assert.DoesNotContain("\"merge\", \"--no-edit\"", enqueue, StringComparison.Ordinal);
        Assert.Contains("merge_drivers.is_drivers_merge(", File.ReadAllText(TestRepo.At("scripts/prune_worktrees.py")), StringComparison.Ordinal);
        Assert.Contains("merge_drivers.conflict_outcome(files)", enqueue, StringComparison.Ordinal);
        string lane = File.ReadAllText(TestRepo.At("scripts/orchestrator/land_lane.py"));
        Assert.Contains("merge_drivers.merge_conflicts(", lane, StringComparison.Ordinal);
        Assert.Contains("merge_drivers.outcome_files(", lane, StringComparison.Ordinal);
        Assert.True(!lane.Contains("\"merge-tree\"", StringComparison.Ordinal) && !lane.Contains("CONFLICT in ", StringComparison.Ordinal),
            "scripts/orchestrator/land_lane.py runs its own `git merge-tree` or parses `CONFLICT in` itself: foresee the merge with " +
            "merge_drivers.merge_conflicts and read the outcome with merge_drivers.outcome_files (kb/Work PB3640 review R1-1, R1-3)");
        foreach (string rel in new[] { "scripts/orchestrator/plan_wave.py", ".claude/skills/workstream/templates/wf_rolling_wave.js",
                     ".claude/skills/workstream/templates/finisher-brief.md", ".claude/skills/workstream/templates/golden-lane-brief.md" })
        {
            string text = File.ReadAllText(TestRepo.At(rel));
            Assert.True(!text.Contains("`git merge ", StringComparison.Ordinal) && !text.Contains("\\`git merge ", StringComparison.Ordinal),
                $"{rel} tells an agent to run a bare `git merge`: use `python scripts/merge_drivers.py merge <ref>` (kb/Work PB3610)");
            Assert.Contains("merge_drivers.py merge", text, StringComparison.Ordinal);
        }
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
