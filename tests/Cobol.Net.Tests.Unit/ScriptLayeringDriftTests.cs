// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PIPELINE SCRIPTS STAY LAYERED (kb/Work PB3726, PB3728; DESIGN-orchestrator-loop.md section 16.10): every module
/// the pipeline-scripts restructure declares (the planner's reports.py, inflight.py, clusters.py, groups.py and
/// dispatch_ledger.py under plan_wave.py, and the modules the later splits of prune_worktrees.py, run_gate_legs.py,
/// enqueue.py and land_lane.py create) imports another declared module only where the import DAG lets it: a module of an
/// earlier tier of a line both stand on, or, from scripts/orchestrator/, the gate's modules of scripts/. The DAG is
/// declared ONCE, in <c>scripts/script_layering.py</c> (<c>LINES</c>, <c>NODES</c>, <c>SPLITS</c>), and must itself be
/// acyclic; each module's imports are read from its syntax tree (module level, inside a function, under
/// <c>TYPE_CHECKING</c>), a declared module not created yet is skipped, a module whose split has not landed stands for
/// the modules its split creates, and an import inside a script's <c>self_test</c> is test code, never a layer.
/// </summary>
/// <remarks>
/// Why: in the seven days to 2026-10-25 the five pipeline scripts took 209 of 1,240 commits, each one doing several jobs
/// that every fix appended to (kb/Work PB3726, PB3723). A split holds only while its modules stay layered: a report
/// reader that imports the planner, or a survey module that imports the command line built on it, is the tangle coming
/// back one import at a time. The audit's <c>--self-test</c> (run by <c>scripts/self_tests.py</c> in every gate and in CI)
/// plants each rule's violation, so a net that went blind would be red there; this sweep runs its <c>--check</c> over
/// the tree.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ScriptLayeringDriftTests
{
    /// <summary>
    /// The files the audit reads: the scripts tree's Python files (declared, kb/Work PB3531, so the drift-rule index names
    /// this class for a change to any of them, a new import included). The modules themselves are named by the audit's
    /// one declaration, which this scope cannot copy; it bounds the count the audit reports.
    /// </summary>
    private static readonly TreeScope ScriptFiles = new(Under: ["scripts/"], Kinds: [".py"]);

    private static readonly Regex Verdict = new(
        @"^=== SCRIPT LAYERING: (?<v>PASS|FAIL) — (?<checked>\d+) declared modules checked, (?<findings>\d+) finding\(s\)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Fact]
    public void EveryDeclaredModule_ImportsOnlyWhatTheDagLetsIt()
    {
        string audit = TestRepo.Scripts("script_layering.py");
        Assert.True(File.Exists(audit), $"the audit is missing: {audit}");
        int files = TrackedTree.Files(ScriptFiles).Count;

        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, audit, "--check");

        Match m = Verdict.Match(r.Stdout);
        Assert.True(m.Success, $"script_layering.py --check printed no verdict (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        Assert.True(r.ExitCode == 0 && m.Groups["v"].Value == "PASS",
            "a pipeline module imports a module the import DAG does not let it import, or the DAG has a cycle. Move the "
            + "code to the module that owns its job, or, when the design changes, change DESIGN-orchestrator-loop.md "
            + "section 16.10 and scripts/script_layering.py's LINES in the same change (kb/Work PB3728):\n"
            + $"{r.Stdout}\n{r.Stderr}");
        int checkedModules = int.Parse(m.Groups["checked"].Value, System.Globalization.CultureInfo.InvariantCulture);
        // A net over nothing is no net: the planner's split (PB3728) put at least its six modules on disk.
        Assert.InRange(checkedModules, 6, files);
    }
}
