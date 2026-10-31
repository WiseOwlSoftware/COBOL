// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE FILES TWO LANDINGS BOTH EDIT ARE MERGED BY DRIVER, AND ONE FUNCTION MERGES MAIN IN (kb/Work PB3610;
/// docs/rearchitecture/DESIGN-test-build-ci.md §3.17.2 step 2). <c>scripts/merge_drivers.py</c> keeps a registry
/// (<c>DRIVERS</c>) of the catalogues a landing appends to — <c>constructs.json</c>, the work notes — of the runtime ABI pin, which
/// <c>scripts/abi_pin.py</c> measures, and of the public-API baselines, merged as SETS of symbol lines (kb/Work PB3727;
/// docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.9); <c>.gitattributes</c> repeats its patterns, and <c>--check</c> holds the two
/// together. <c>enqueue.py land</c> merges main in through <c>merge_drivers.merge</c>, so two branches that each added an
/// entry are no conflict; the review's coverage rule (<c>prune_worktrees.mechanical</c>) reads the same function, so a
/// merge the drivers made reads as mechanical and a hand resolution does not. PROSE HAS NO DRIVER, ON PURPOSE (kb/Work
/// PB3643): <c>merge_drivers.py --hunks</c> replays the merges in history and classifies each conflict hunk, and the
/// design documents' conflicts are REVISIONS of one paragraph, never two appended sections.
/// </summary>
/// <remarks>
/// Until PB3610 these landings stopped at CONFLICT and cost a finisher cycle each (the landing logs counted
/// <c>tests/conformance/2002/manifest.json</c> fifteen times, <c>negative/manifest.json</c> thirteen,
/// <c>RuntimeAbiPinDriftTests.cs</c> four, and <c>wiseowl/diagnostics.json</c> four, until kb/Work PB4147 made it one file per
/// test). The facts hold the structure, not the
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
            "publicapi: baseline_duplicates names a symbol listed twice in one file",
            "publicapi: is_symbol_baseline reads the SYMBOL_SET_DRIVER row's pattern",
            "git: a public-API baseline both branches changed merges through its driver as a set",
            "git: a branch cut with the old attributes (merge=union) merges a baseline through the registry's driver",
            "patterns: a driver's pattern names a path as git does",
            "hunks: two insertions at one place are an INSERTION hunk",
            "hunks: the history replay counts a merge both parents changed",
            "hunks: a merge that took one parent's file whole is counted",
            "git: WITHOUT the drivers the same merge conflicts (the premise)",
            "git: merge() brings main in through the drivers",
            "git: merge-tree with config_args reproduces the merge's tree",
            "tree: merged_tree is that merge's tree",
            "tree: an entry main already holds beside its own merges to main's tree",
            "git: keep leaves a conflicting merge IN PROGRESS",
            "pin: both branches raised the major 17 -> 18: the merge makes it 19",
            "pin: a settle that fails aborts the merge, the branch unchanged, and is NOT a conflict",
            "pin: keep (a finisher) leaves a merge whose pin could not be settled IN PROGRESS",
            "coverage: is_drivers_merge accepts the merge with its pin settled",
            "coverage: a merge whose pin is MEASURED (its header names UNMEASURED only in prose)",
            "conflicts: the dry run lists nothing where the drivers merge",
            "conflicts: a keyed file whose driver DECLINES (one entry both sides changed differently) is listed",
            "conflicts: a ref git cannot read is None",
            "queue: an entry that conflicts on the entries ahead that stay is foreseen",
            "queue: the simulation touches no worktree",
            "queue: the simulation merges as GitHub's merge group does, with no driver",
            "conflicts: the landing logs' CONFLICT lines name their files",
            "new driver: a branch that gives a pattern a driver main's attributes do not yet give it AND changes a file",
            "new driver: the same file back at main's bytes passes",
            "new driver: a changed file whose driver main's attributes already assign, and a file no pattern governs",
            "new driver: a main that moved since the branch was cut is not the branch's change",
            "new driver: a pattern main re-pointed since the branch was cut",
            "new driver: where the checkout has no such ref (CI's has no origin/main) the answer is a REASON",
            "check: the registry and this repository's .gitattributes agree",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At(AbiPin), [
            "merge: a counter both sides raised by one is raised by two",
            "unmeasured: a pin whose header names UNMEASURED only in its prose is measured",
            "settle: the merged counters are written into the props and RuntimeAbi.cs BEFORE the measurement",
            "settle: a measurement that fails, or writes nothing, is the reason",
            "failure: a build output another process holds (MSB3027, MSB3021), a full disk and a command that cannot start are MERGE_BLOCKED",
            "failure: MSBuild's transient copy-retry WARNING (MSB3026, which carries the held-file phrases) is no cause",
            "settle: an owned file that lacks the line the settle rewrites is the merged tree's (MERGE_FAILED)",
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

    /// <summary>
    /// The registry and <c>.gitattributes</c> agree both ways, every driver's pattern names a tracked file, and A NEW DRIVER
    /// LANDS ALONE (kb/Work PB3825; DESIGN-orchestrator-loop.md section 16.9): <c>--check</c> also refuses a branch that
    /// changes a file a <c>DRIVERS</c> pattern governs while origin/main's <c>.gitattributes</c> does not yet give the pattern
    /// that driver, unless the file is main's bytes (the landing merges main in through main's registry, so such a file met
    /// every later main change as a line-merge conflict, PB3727). A checkout without origin/main says so
    /// (<c>NOTE ... NOT CHECKED</c>); the rule's arms are the self-test's, declared above.
    /// </summary>
    [Fact]
    public void TheDriverRegistry_AgreesWithGitattributes()
    {
        var r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.At(Drivers), "--check");
        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== MERGE DRIVERS: GREEN ===", StringComparison.Ordinal),
            $"`merge_drivers.py --check` found the registry and .gitattributes apart, or a branch that changes a file of a NEW driver (a new driver lands alone, leave its files as origin/main has them; exit {r.ExitCode}): add the pattern to both.\n{r.Stdout}\n{r.Stderr}");
        string source = File.ReadAllText(TestRepo.At(Drivers));
        Assert.Contains("landing, unread = new_driver_files(HERE.parent)", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Main is merged in through the drivers at every site that merges it: the landing (<c>enqueue.merge_main</c>), the
    /// review's coverage rule (<c>prune_worktrees.mechanical</c>) and the predecessor-merge instructions the planner's groups
    /// (<c>scripts/orchestrator/groups.py</c>: <c>finisher_pred</c>, <c>continuation_pred</c>) and the
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
        string prune = File.ReadAllText(TestRepo.At("scripts/prune_worktrees.py"));
        Assert.Contains("merge_drivers.is_drivers_merge(", prune, StringComparison.Ordinal);
        // The survey's LANDED proof merges main and the branch the way the landing does (kb/Work PB3713): a plain
        // `git merge-tree` conflicts in a keyed catalogue the drivers merge by key, so a branch already on main never read LANDED.
        Assert.True(prune.Contains("merge_drivers.merged_tree(", StringComparison.Ordinal) && !prune.Contains("'merge-tree'", StringComparison.Ordinal),
            "scripts/prune_worktrees.py runs its own `git merge-tree`: prove a landing with merge_drivers.merged_tree, the one definition of a clean merge's tree (kb/Work PB3713)");
        Assert.Contains("verdict_line.conflict_outcome(files)", enqueue, StringComparison.Ordinal);
        string lane = File.ReadAllText(TestRepo.At("scripts/orchestrator/land_lane.py"));
        Assert.Contains("merge_drivers.merge_conflicts(", lane, StringComparison.Ordinal);
        Assert.Contains("verdict_line.outcome_files(", lane, StringComparison.Ordinal);
        Assert.True(!lane.Contains("\"merge-tree\"", StringComparison.Ordinal) && !lane.Contains("CONFLICT in ", StringComparison.Ordinal),
            "scripts/orchestrator/land_lane.py runs its own `git merge-tree` or parses `CONFLICT in` itself: foresee the merge with " +
            "merge_drivers.merge_conflicts and read the outcome with verdict_line.outcome_files (kb/Work PB3640 review R1-1, R1-3, PB3714)");
        foreach (string rel in new[] { "scripts/orchestrator/groups.py", ".claude/skills/workstream/templates/wf_rolling_wave.js",
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
    /// landing lane retries exit 16 once none does. Only a pull request that ENTERS the queue holds (<c>queue_facts.PullRequest.entering</c>:
    /// a bounced, conflicting or red one re-enters only through a fresh land; review R1-1). The self-tests drive every arm, including one branch that changes the
    /// sample file of EVERY driver pattern; this holds the wiring and that those arms stay declared.
    /// </summary>
    [Fact]
    public void TheLanding_WaitsForAnOpenPullRequestThatSharesADriverFile()
    {
        string enqueue = File.ReadAllText(TestRepo.At("scripts/orchestrator/enqueue.py"));
        Assert.Contains("driver_file_holders(wt, base, branch, gh=gh)", enqueue, StringComparison.Ordinal);
        Assert.Contains("merge_drivers.covered_by_driver(p)", enqueue, StringComparison.Ordinal);
        Assert.Contains("queue_facts.read(gh)", enqueue, StringComparison.Ordinal);
        Assert.Contains("known[n].entering", enqueue, StringComparison.Ordinal);
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
            "driver files: a pull request the queue BOUNCED, that CONFLICTS with main or whose required check FAILED holds nothing",
            "land: once the open pull request has merged, or when it was bounced out of the queue",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/orchestrator/queue_facts.py"), [
            "a pull request ENTERS the merge queue when it is in it or has auto-merge on",
        ]);
        SelfTestArmSource.AssertDeclares(TestRepo.At("scripts/orchestrator/land_lane.py"), [
            "exit 16 (an open pull request changes a merge-driver file the branch changes too): a retry-when-free class",
        ]);
    }

    /// <summary>
    /// ⛔ NO REGISTRY OF THE CONFORMANCE SUITE IS ONE SHARED FILE (kb/Work PB4147): WiseOwl's refusals and compile options
    /// are one file per test (<c>conformance-suite/wiseowl/diagnostics/&lt;id&gt;.json</c>, <c>options/</c>), so two landings
    /// that each add a refusal add two files and the merge queue, which runs no driver, merges them. The single
    /// <c>diagnostics.json</c> held 61 of the landing lane's 79 driver-file holds (exit 16). This keeps it from coming back:
    /// no file sits beside the registries' directories under <c>conformance-suite/wiseowl/</c>, every registry file is one
    /// test's own <c>&lt;id&gt;.json</c>, and no merge driver governs a path under <c>conformance-suite/</c> (a driver
    /// there would serialise landings again). The generator refuses a file that is not a registry entry
    /// (<c>generate.py --self-test</c>).
    /// </summary>
    [Fact]
    public void TheSuiteRegistries_AreOneFilePerTest_AndNoDriverGovernsThem()
    {
        string wiseowl = TestRepo.Suite("wiseowl");
        Assert.Equal(["cases", "diagnostics", "options"], Directory.EnumerateFileSystemEntries(wiseowl).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Empty(Directory.EnumerateFiles(wiseowl));
        foreach (string registry in new[] { "diagnostics", "options" })
            foreach (string file in Directory.EnumerateFileSystemEntries(Path.Combine(wiseowl, registry)))
            {
                string name = Path.GetFileName(file);
                Assert.True(File.Exists(file) && name.EndsWith(".json", StringComparison.Ordinal) && name.Length > ".json".Length && name[0] != '.',
                    $"conformance-suite/wiseowl/{registry}/{name} is not a `<test id>.json` file: a registry directory holds one file per test and nothing else");
            }
        foreach (string line in File.ReadLines(TestRepo.At(".gitattributes")).Where(l => l.Contains("merge=cobol-", StringComparison.Ordinal)))
            Assert.False(line.TrimStart().StartsWith("conformance-suite/", StringComparison.Ordinal),
                $".gitattributes gives a conformance-suite path a merge driver ({line.Trim()}): a registry of the suite is one file per entry, never a shared file a driver merges (kb/Work PB4147)");
    }

    /// <summary>
    /// ⛔ THE TRACEABILITY INVENTORY IS ONE FILE PER ROW, AND NO DRIVER GOVERNS IT (kb/Work PB4148): the 4,214-row
    /// <c>traceability-inventory.json</c> was the second-largest cause of landing holds after the suite's refusal registry,
    /// because the merge queue runs no driver and so admitted one row-closing pull request at a time. The rows are
    /// <c>tests/version-matrix/inventory/&lt;rule-id&gt;.json</c> now (<c>SpecTraceabilityInventoryDriftTests</c> holds the
    /// directory's shape); this keeps a driver, or an attribute line, from naming the inventory again.
    /// </summary>
    [Fact]
    public void TheInventory_HasNoMergeDriver()
    {
        foreach (string line in File.ReadLines(TestRepo.At(".gitattributes")).Where(l => !l.TrimStart().StartsWith('#')))
        {
            Assert.False(line.Contains("tests/version-matrix/inventory", StringComparison.Ordinal)
                || line.Contains("traceability-inventory", StringComparison.Ordinal)
                || line.Contains("cobol-keyed-inventory", StringComparison.Ordinal),
                $".gitattributes names the traceability inventory ({line.Trim()}): its rows are one file each, never a shared file a driver merges (kb/Work PB4148)");
        }

        string drivers = File.ReadAllText(TestRepo.At(Drivers));
        Assert.DoesNotContain("cobol-keyed-inventory", drivers, StringComparison.Ordinal);
    }

    /// <summary>The pin is a measured file nobody hand-edits: it holds exactly the four lines the test reads and never
    /// UNMEASURED on a committed tree (a merge settles it before its commit).</summary>
    [Fact]
    public void TheAbiPin_IsMeasuredOnTheTree()
    {
        string[] values = [.. File.ReadAllLines(TestRepo.Src("WiseOwl.Cobol.Runtime", "RuntimeAbi.pin")).Where(l => l.Length > 0 && l[0] != '#')];
        Assert.Equal(["runtime-major", "runtime-hash", "call-abi", "boundary-hash"], values.Select(l => l.Split(' ')[0]));
        Assert.DoesNotContain(values, l => l.Contains("UNMEASURED", StringComparison.Ordinal));
    }
}
