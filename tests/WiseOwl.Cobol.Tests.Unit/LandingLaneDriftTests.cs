// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE LOOP'S LANDING LANE LANDS, BESIDE THE UNIT, AND THE ENQUEUE UNIT DOES NOT (kb/Work PB3103, PB3639;
/// MANDATORY-PRACTICES O11): <c>orchestrate.ps1</c> starts <c>land_lane.py tick</c> as a BACKGROUND process from its one
/// timer <c>Step-Lane</c>, called in <c>Invoke-Unit</c>, in <c>Wait-Until</c> after its STOP check and between units, and
/// <c>enqueue.md</c> step 3 reads <c>land_lane.py status</c> and lands nothing.
/// </summary>
/// <remarks>
/// <para>The design is DESIGN-orchestrator-loop.md section 4.9. The tick is never awaited on the unit's event loop. The
/// lane spends no tokens, so it ticks for as long as the supervisor lives, unit or no unit (kb/Work PB3639): as each unit
/// starts and on a period while it runs, in <c>Wait-Until</c> (a budget hold and a failure backoff tick; the STOP check
/// comes first, so no tick starts once STOP exists) and at the loop top between units. The summary above is the text
/// <c>drift_rules.py</c> prints before an edit of <c>orchestrate.ps1</c>, so it names every call site the test requires.</para>
/// <para>Before the lane a branch reviewed CLEAN landed only when the loop's NEXT enqueue unit started, after its whole
/// wave ended: wave 1069's group D was stamped CLEAN at 12:34 PDT and E and F by 13:05, and all three idled while G and
/// H ran; then no implementer ran while the enqueue unit gated and waited on the merge queue. The owner chose the
/// supervisor lane (2026-10-12, "Supervisor lane (Recommended)"). Two drifts would bring the wait back without a red
/// anywhere else: a supervisor that no longer starts the lane (branches wait for an enqueue unit next_unit.py no longer
/// starts for them), and an enqueue.md step 3 that lands again (two landing paths racing for one branch). A third would
/// stall the unit: a tick awaited on the event loop, where its 30-second survey would block the stream read, the startup
/// watchdog and the STOP check (kb/Work PB2590's defect, for the checkpoint).</para>
/// <para>The lane's own decisions (which exit is retried, which waits for the enqueue unit, the halt, the stops) are
/// <c>land_lane.py --self-test</c>'s, which the self-test runner discovers and runs in every gate and in CI.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LandingLaneDriftTests
{
    /// <summary>A command that lands a branch: <c>enqueue.py land</c>, however it is spelled out.</summary>
    private static readonly Regex LandCommand = new(@"enqueue\.py\s+land\b", RegexOptions.Compiled);

    /// <summary>A sentence that still names the enqueue unit (or a finisher's re-run of <c>enqueue.py</c>) as the way a
    /// branch lands: the wording the lane retired (review R2-1). "lands nothing" is the current pipeline's own sentence.</summary>
    private static readonly Regex RetiredLandingWording = new(
        @"\benqueue unit (?:then |only )?(?:lands|re-runs)\b(?! nothing)|landed by the enqueue unit|re-runs? enqueue\.py"
        + @"|(?:finished|DONE) work is the enqueue unit's", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>The files that describe the current pipeline: the whole tree but the paths that record history
    /// (CLAUDE.md rule 6) and the test sources (this file names the pattern). Declared (kb/Work PB3531), so the
    /// drift-rule index names this class for each of them. The knowledge base is set aside whole: a sweep that read
    /// it made the vault a test input (kb/Work PB2878).</summary>
    private static readonly TreeScope Current = new(Under: [""],
        Except: ["DEVLOG/", "kb/", "adjudication/", "specs/", "tests/"]);

    /// <summary>The files of <paramref name="tracked"/> (already narrowed to <see cref="Current"/>) with a line in the
    /// retired wording, as <c>path:line</c>.</summary>
    internal static IReadOnlyList<string> RetiredWordingSites(string root, IEnumerable<string> tracked) =>
        [.. tracked.Where(p => File.Exists(Path.Combine(root, p)))
                   .SelectMany(p => File.ReadLines(Path.Combine(root, p))
                       .Select((line, i) => (line, i))
                       .Where(x => RetiredLandingWording.IsMatch(x.line))
                       .Select(x => $"{p}:{x.i + 1}"))];

    /// <summary>Step 3 of an enqueue unit prompt: from its <c>3.</c> line to the next top-level step.</summary>
    internal static string Step3(string unitPrompt)
    {
        Match m = Regex.Match(unitPrompt, @"^3\. .*?(?=^\d+[a-z]?\. )", RegexOptions.Multiline | RegexOptions.Singleline);
        return m.Success ? m.Value : "";
    }

    /// <summary>The body of a PowerShell function: from its <c>function Name</c> line to the next top-level function.</summary>
    internal static string FunctionBody(string script, string name)
    {
        Match m = Regex.Match(script, @"^function " + Regex.Escape(name) + @"\b.*?(?=^function |\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        return m.Success ? m.Value : "";
    }

    /// <summary>THE QUEUE WATCH (kb/Work PB3527; DESIGN-orchestrator-loop.md section 4.9 step 4): a landing that ends
    /// ENQUEUED has only handed its pull request to the queue, and PR #385 waited 50 minutes outside it behind a
    /// check-run that never resolved. <c>land_lane.py --self-test</c> drives the watch over stubbed GitHub facts
    /// (queued, merged, stuck then cleared by the re-run, stuck then cleared by the reopen, the alert, a stop), and
    /// <c>queue_facts.py --self-test</c> the one reader of those facts; this holds every arm still declared, and that the
    /// lane retries a GATE BUSY (exit 12) once the worktree's gate lock is free.</summary>
    [Fact]
    public void TheQueueWatch_AndItsReader_DeclareEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "land_lane.py"), new[]
        {
            "queue watch: under a stop it reads but acts on nothing (no re-run, no requeue, no alert)",
            "queue watch: GitHub's facts unobserved this tick: nothing is read and nothing is done",
            // kb/Work PB3772: a pull request in the queue is read again on every tick (it can leave the queue unmerged)
            "queue watch: a pull request in the queue reads QUEUED and the watch does NOT end (kb/Work PB3772: it "
                + "can leave the queue unmerged); one whose branch's work is on main ends it MERGED; a required check "
                + "that failed ends it LEFT (BOUNCED: the finisher's); none acted on",
            "queue watch: a stuck check-run inside the grace is only watched; a pull request with no open pull "
                + "request and its work not on main ALERTS at once (closed by hand)",
            "queue watch: past the grace, read 1 of a stuck check-run re-runs its workflow run and records the "
                + "stall (cause stuck-check-run, read 1); a check whose job still runs is not stalled",
            "queue watch: stuck, then cleared by the re-run: the pull request enters the queue (QUEUED, still watched)",
            "queue watch: read 2 (still stuck one grace after the re-run) closes, reopens and re-queues the pull "
                + "request, recorded as read 2",
            "queue watch: stuck, then cleared by the reopen: in the queue, no third read",
            "queue watch: a third stalled read ALERTS the operator (one mailbox question naming the pull request and "
                + "the remedies tried) and the watch acts no more, however long it stays out",
            "queue watch: status and the survey's NOT QUEUED row say the watch's state, when it read, that an ended "
                + "watch reads nothing again, and each read",
            "queue watch: a pull request QUEUED and then out of the queue with auto-merge off and no failed run is "
                + "WATCHING inside the grace counted from its last reading in the queue",
            "queue watch: the requeue put it back in the queue: QUEUED again, and watched on",
            "queue watch: with no check-run at all the clock starts at the enqueue",
            "queue watch: a check-run whose job the jobs API could not read never holds the watch for ever",
            // kb/Work PB4052: ci-gate has no check-run while a job it needs runs; that is work running, never a stall
            "queue watch: a required check with no check-run yet because a job it needs still runs (PR #777's audits, "
                + "25 min after the enqueue) is WATCHING with no remedy, never the requeue that cancels the run",
            "exit 12 while the gate lock is held: not relaunched, not the enqueue unit's work",
            "exit 12 once the gate lock is free: retried at the same head",
            "wait_free for exit 12 reads the gate driver's own lock of the worktree",
            // kb/Work PB3662: a gate that is still running (its gate lock held) is exit 12's wait too
            "exit 12 for a gate that is still running: the lane waits at that head while the worktree's gate lock is "
                + "held",
            // kb/Work PB3145: a merged predecessor's open pull request is a wait, not a stop
            "exit 15 (a merged predecessor's pull request is still open): a retry-when-free class, never the enqueue "
                + "unit's",
        });
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "queue_facts.py"), new[]
        {
            "auto-merge on is NOT in the queue",
            "a required check-run in progress whose Actions job completed is STUCK (PR #385's propagation fault)",
            "only an unresolved check-run asks the jobs API (a resolved one, required or not, never does)",
            "a job the jobs API cannot read leaves the check-run unknown, never stuck",
            // kb/Work PB4052: a reopen's run supersedes the run it cancelled, whose failed ci-gate is no red
            "a superseded run's check-runs are history (kb/Work PB4052): the newer run of the same workflow on the head "
                + "decides, so the cancelled run's FAILED ci-gate is no red",
            "offline, a gh failure or a malformed answer is None (a non-observation), never {}",
        });
    }

    [Fact]
    public void TheSupervisor_StartsTheLane_InTheBackground_BesideEveryUnit()
    {
        string script = File.ReadAllText(TestRepo.Scripts("orchestrator", "orchestrate.ps1"));
        string start = FunctionBody(script, "Start-LaneTick");
        Assert.True(start.Length > 0, "orchestrate.ps1 has no `function Start-LaneTick`: the landing lane is not started (kb/Work PB3103)");
        Assert.Contains("'land_lane.py'", start, StringComparison.Ordinal);
        Assert.Contains("'tick'", start, StringComparison.Ordinal);
        // The supervisor's ONE background starter (kb/Work PB3638 moved the lane tick, the checkpoint and the progress
        // tick onto it): the tick goes through it, and it starts a process without ever waiting for it.
        string background = FunctionBody(script, "Start-PyBackground");
        Assert.True(start.Contains("Start-PyBackground", StringComparison.Ordinal)
                    && !start.Contains("WaitForExit", StringComparison.Ordinal)
                    && background.Contains("[System.Diagnostics.Process]::Start(", StringComparison.Ordinal)
                    && !background.Contains("WaitForExit", StringComparison.Ordinal),
            "Start-LaneTick must start the tick as a background process (Start-PyBackground) and never wait for it: the "
            + "event loop reads the unit's stream, ticks the startup watchdog and checks STOP (kb/Work PB2590)");

        // ONE lane timer, Step-Lane, which the supervisor calls wherever it waits or loops (kb/Work PB3639): the lane spends
        // no tokens, so it ticks whether or not an LLM unit runs.
        string step = FunctionBody(script, "Step-Lane");
        Assert.True(step.Length > 0, "orchestrate.ps1 has no `function Step-Lane`: the lane has no timer of its own (kb/Work PB3639)");
        Assert.Matches(@"\bStart-LaneTick\b", step);
        Assert.Contains("LandLaneSeconds", step, StringComparison.Ordinal);

        string unit = FunctionBody(script, "Invoke-Unit");
        Assert.True(unit.Length > 0, "orchestrate.ps1 has no `function Invoke-Unit`; re-point this test");
        int calls = Regex.Matches(unit, @"^\s*Step-Lane\b", RegexOptions.Multiline).Count;
        Assert.True(calls >= 2, $"Invoke-Unit steps the lane {calls} time(s): it must tick as the unit starts and on "
            + "-LandLaneSeconds while it runs");

        // The waits with no unit: a budget hold and a failure backoff sleep in Wait-Until, and the loop between two units
        // steps the lane itself. Without these the lane stops whenever no unit runs (the stall survey's LANE_TICK_UNIT_BOUND).
        string wait = FunctionBody(script, "Wait-Until");
        Assert.True(wait.Length > 0, "orchestrate.ps1 has no `function Wait-Until`; re-point this test");
        Assert.Matches(@"\bStep-Lane\b", wait);
        Assert.True(wait.IndexOf("Test-Stop", StringComparison.Ordinal) < wait.IndexOf("Step-Lane", StringComparison.Ordinal),
            "Wait-Until must check STOP before it steps the lane: no tick starts once STOP exists (kb/Work PB3639)");
        string loop = script[script.LastIndexOf("while ($true) {", StringComparison.Ordinal)..];
        Assert.Matches(@"\{ Step-Lane \}", loop);
        Assert.Matches(@"\[int\]\$LandLaneSeconds\s*=\s*[1-9]\d*", script);
    }

    [Fact]
    public void EnqueueUnitStep3_ReadsTheLane_AndLandsNothingItself()
    {
        string step3 = Step3(File.ReadAllText(TestRepo.Scripts("orchestrator", "units", "enqueue.md")));
        Assert.True(step3.Length > 0, "scripts/orchestrator/units/enqueue.md has no step 3; re-point this test");
        Assert.Contains("land_lane.py status", step3, StringComparison.Ordinal);
        Assert.DoesNotMatch(LandCommand, step3);
        // A stop resolved with no new commit leaves the head unchanged and the lane never relaunches it there by itself:
        // step 3 must name the unit's way to land it again (review R1-1), or that branch waits forever.
        Assert.Contains("land_lane.py rearm", step3, StringComparison.Ordinal);
    }

    /// <summary>No doc, role definition, brief or script comment still says the enqueue unit lands a branch or a finisher
    /// re-runs <c>enqueue.py</c>: an agent that reads one waits for a landing nobody performs (review R2-1, whose sites
    /// the round-1 sweep's narrower pattern could not match).</summary>
    [Fact]
    [Trait("Sweep", "tree")]
    public void NoCurrentFile_SaysTheEnqueueUnitLands()
    {
        IReadOnlyList<string> tracked = TrackedTree.Files(Current);
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        IReadOnlyList<string> sites = RetiredWordingSites(TestRepo.Root, tracked);
        Assert.True(sites.Count == 0,
            "these lines still say the enqueue unit lands a branch or a finisher re-runs enqueue.py; the landing lane "
            + "lands (land_lane.py, kb/Work PB3103) and the enqueue unit re-arms (`land_lane.py rearm`):\n  "
            + string.Join("\n  ", sites));
    }

    /// <summary>The scripts that could write or read a landing's verdict line, set apart from the line's one home
    /// (<c>scripts/verdict_line.py</c>, kb/Work PB3714). Declared (kb/Work PB3531), so the drift-rule index names this
    /// class for each of them.</summary>
    private static readonly TreeScope VerdictLineUsers = new(Under: ["scripts/"], Except: ["scripts/verdict_line.py"],
        Kinds: [".py", ".ps1", ".sh"]);

    /// <summary>The verdict line's prefix spelled in code: at the start of a string literal (<c>"=== ENQUEUE: "</c>,
    /// <c>f'=== ENQUEUE: {o}'</c>) or at the start of a line inside one (<c>\n=== ENQUEUE</c>, <c>{NL}=== ENQUEUE</c>,
    /// the way a self-test plants a landing log). A comment, a docstring or a message that NAMES the line in backticks is
    /// prose and does not match.</summary>
    private static readonly Regex VerdictLiteral = new(@"([""']|\\n|\{NL\}|\{chr\(10\)\})=== ENQUEUE", RegexOptions.Compiled);

    /// <summary>The files of <paramref name="tracked"/> (already narrowed to <see cref="VerdictLineUsers"/>) with a line
    /// that spells the verdict line's prefix in a string literal, as <c>path:line</c>.</summary>
    internal static IReadOnlyList<string> VerdictLiteralSites(string root, IEnumerable<string> tracked) =>
        [.. tracked.Where(p => File.Exists(Path.Combine(root, p)))
                   .SelectMany(p => File.ReadLines(Path.Combine(root, p))
                       .Select((line, i) => (line, i))
                       .Where(x => VerdictLiteral.IsMatch(x.line))
                       .Select(x => $"{p}:{x.i + 1}"))];

    /// <summary>⭐ THE VERDICT LINE HAS ONE HOME (kb/Work PB3714): the prefix, the writer (<c>render</c>), the readers
    /// (<c>parse</c>, <c>outcomes</c>) and the CONFLICT outcome live in <c>scripts/verdict_line.py</c>, and
    /// <c>enqueue.py</c>, <c>land_lane.py</c> and <c>merge_drivers.py</c> go through it. Before it, the prefix was a
    /// literal in each of the three and a change of the line's shape had to be made in all of them with no test to say
    /// one was missed. A script that spells the prefix in a string literal is a fourth copy.</summary>
    [Fact]
    [Trait("Sweep", "tree")]
    public void NoScriptSpellsTheVerdictLine_OutsideItsOneHome()
    {
        IReadOnlyList<string> tracked = TrackedTree.Files(VerdictLineUsers);
        Assert.True(tracked.Count >= 50, $"`git ls-files` reported only {tracked.Count} scripts — the sweep would scan nothing");
        Assert.Contains("scripts/orchestrator/enqueue.py", tracked);
        Assert.True(File.Exists(TestRepo.Scripts("verdict_line.py")), "scripts/verdict_line.py is the verdict line's one home");
        IReadOnlyList<string> sites = VerdictLiteralSites(TestRepo.Root, tracked);
        Assert.True(sites.Count == 0,
            "these lines spell the landing's `=== ENQUEUE: ` verdict line in a string literal; write it with "
            + "verdict_line.render and read it with verdict_line.parse / outcomes (kb/Work PB3714):\n  "
            + string.Join("\n  ", sites));
    }

    /// <summary>⭐ The verdict-literal sweep's failure branch, fired: each spelling of the prefix in a literal is found by
    /// the same function, and prose that names the line in backticks is not.</summary>
    [Fact]
    public void TheVerdictLiteralSweep_FindsEachPlantedSpelling()
    {
        string dir = ScratchDirectory.Create("verdict-line-");
        try
        {
            File.WriteAllLines(Path.Combine(dir, "writer.py"),
            [
                "VERDICT = \"=== ENQUEUE: \"",
                "say(f'=== ENQUEUE: {outcome} ({branch}) ===')",
                "log = 'land lane\\n=== ENQUEUE: ENQUEUED (b) ==='",
                "log = f\"land lane: attempt 1{NL}=== ENQUEUE: CONFLICT in x — merge aborted (b) ===\"",
            ]);
            File.WriteAllLines(Path.Combine(dir, "prose.py"),
            [
                "# the landing ends on `=== ENQUEUE: <OUTCOME> (<branch>) ===` (P2)",
                "\"\"\"Every outcome ends on ONE line `=== ENQUEUE: <OUTCOME> … ===` for a caller to block on.\"\"\"",
            ]);
            Assert.Equal(new[] { "writer.py:1", "writer.py:2", "writer.py:3", "writer.py:4" },
                VerdictLiteralSites(dir, new[] { "writer.py", "prose.py" }));
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>⭐ The retired-wording sweep's failure branch, fired: each retired phrasing is found by the same
    /// function, and the current pipeline's sentences and a history path are not.</summary>
    [Fact]
    public void TheRetiredWordingSweep_FindsEachPlantedPhrasing()
    {
        string dir = ScratchDirectory.Create("landing-lane-");
        Directory.CreateDirectory(Path.Combine(dir, "brief"));
        Directory.CreateDirectory(Path.Combine(dir, "kb"));
        try
        {
            File.WriteAllLines(Path.Combine(dir, "brief", "stale.md"),
            [
                "the branch is only then landed by the enqueue unit.",
                "which fixes the branch and re-runs enqueue.py.",
                "its finished work is the enqueue unit's.",
                "the enqueue unit lands it.",
            ]);
            File.WriteAllLines(Path.Combine(dir, "brief", "current.md"),
            [
                "the lane never relaunches it and the enqueue unit lands nothing itself.",
                "its enqueue unit reviews it, its lane lands it.",
                "the enqueue unit's `rearm` lands it again.",
            ]);
            File.WriteAllText(Path.Combine(dir, "kb", "note.md"), "before PB3103 the enqueue unit lands it.");
            var sites = RetiredWordingSites(dir, new[] { "brief/stale.md", "brief/current.md", "kb/note.md" }.Where(Current.Holds));
            Assert.Equal(new[] { "brief/stale.md:1", "brief/stale.md:2", "brief/stale.md:3", "brief/stale.md:4" }, sites);
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>⭐ The step-3 reader's failure branch, fired: a planted prompt whose step 3 lands is caught, and the
    /// later steps (the clerk's own <c>enqueue.py land --lane clerk</c>, step 5) are outside it.</summary>
    [Fact]
    public void TheStep3Reader_CatchesAPlantedLanding_AndStopsAtStep4()
    {
        const string planted = "UNIT: enqueue.\n1. Load.\n2. Review.\n3. LAND. For each, `python scripts/orchestrator/"
            + "enqueue.py land <branch>`.\n4. FINISHERS.\n5. LEADS: the clerk lands with `enqueue.py land <b> --lane clerk`.\n";
        string step3 = Step3(planted);
        Assert.StartsWith("3. LAND.", step3, StringComparison.Ordinal);
        Assert.DoesNotContain("4. FINISHERS", step3, StringComparison.Ordinal);
        Assert.Matches(LandCommand, step3);
        Assert.DoesNotMatch(LandCommand, Step3(planted.Replace("enqueue.py land <branch>", "land_lane.py status",
            StringComparison.Ordinal)));
    }
}
