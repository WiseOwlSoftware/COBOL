// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ GITHUB'S MERGE QUEUE IS THE LANDING, AND NOTHING OPENS A PULL REQUEST BEFORE THE QUEUE IS ACTIVE (kb/Work PB3024;
/// DESIGN-test-build-ci.md §3.17.6 and DESIGN-test-build-ci.md §3.17.8): <c>.github/workflows/build-and-test.yml</c> runs the matrix on the
/// <c>merge_group</c> event and only the cheap jobs on <c>pull_request</c>; its already-verified short-circuit counts only
/// a run that could have run the matrix, never a pull request's; no tracked file but
/// <c>scripts/orchestrator/enqueue.py</c> opens or queues a pull request, and enqueue.py asks <c>ruleset.py check</c> for
/// an ACTIVE queue before it checks, pushes or opens anything; every gate row enqueue.py runs takes the shared gate slot
/// (kb/Work PB3104: <c>unslotted</c> refuses a row that would not, and its self-test arms are held here by name); and the
/// direct-push landing is gone: its scripts do not
/// exist, the workflow has no landing-branch trigger, and no tracked file outside the history names them; and no landing
/// is held to a COMMITTED RECORD OF ITS OWN TREE (kb/Work PB3107): a merge group is built on every group queued ahead of
/// it, so such a record cannot describe the tree that lands — the arch-oracle baseline is not committed and the landing
/// oracle that refused on it is retired with the direct-push scripts; and every merge-group run starts with the
/// <c>supersede</c> job (kb/Work PB3769), the one job with <c>actions: write</c>, which cancels the merge-group runs the
/// queue has replaced through <c>scripts/orchestrator/cancel_superseded.py</c> (its fail-safe arms held here by name) and
/// never decides a verdict (<c>continue-on-error</c>).
/// </summary>
/// <remarks>
/// <para>The operator's review (2026-10-11 13:39, mailbox 20261011-133923): a pull request runs only CI's cheap jobs, so
/// its <c>ci-gate</c> passes on them; opened before the owner applies the ruleset there is no merge group, and
/// <c>gh pr merge --auto</c> would merge it with no whole-population run. The guard is closed by construction: one
/// script holds the pull-request step and its guard comes first, and this test is red on a brief, unit, script or
/// workflow that opens or queues a pull request any other way. A pull request's green <c>ci-gate</c> is attached to
/// its head commit, so a short-circuit that read the check run would skip the matrix for an untested commit; the
/// workflow reads the workflow RUNS and excludes <c>pull_request</c>.</para>
/// <para>The live ruleset is compared with the committed one by <c>ruleset.py check</c>, whose consumers are the
/// landing itself (enqueue.py refuses on anything but ACTIVE) and the session probe's <c>gate :</c> line — not an
/// xUnit arm: this suite has no dynamic skip, and an offline comparison must read SKIPPED, never green. The retirement
/// arm (kb/Work PB3024's closing change, the first landed through the queue) keeps the former direct-push landing
/// script, the landing lease and the retired train applier out of the tree for good: a current-state file that still
/// names one describes a path main refuses (CLAUDE.md rule 4: no leftover paths).</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class LandingPipelineDriftTests
{
    /// <summary>A step that opens or queues a pull request: the gh CLI's text form, or a Python argument list.</summary>
    private static readonly Regex PullRequestStep = new(
        @"\bgh\s+pr\s+(?:create|merge)\b|[""']pr[""']\s*,\s*[""'](?:create|merge)[""']", RegexOptions.Compiled);

    /// <summary>The files that PERFORM the landing: the whole tree but the paths that describe it (design docs, the work
    /// register, the history, frozen evidence and adjudication records, the spec, and the test sources, as this file
    /// names the patterns). Declared (kb/Work PB3531), so the drift-rule index names this class for each of them. The
    /// knowledge base and the vault's settings are set aside whole: a sweep that read them made the vault a test input
    /// (kb/Work PB2878).</summary>
    private static readonly TreeScope NotNarrative = new(Under: [""],
        Except: ["docs/", "kb/", ".obsidian/", "adjudication/", "specs/", "tests/", "DEVLOG/"]);

    private const string TheGuardedStep = "scripts/orchestrator/enqueue.py";

    /// <summary>The files of <paramref name="tracked"/> (already narrowed to <see cref="NotNarrative"/>) that hold a
    /// pull-request step.</summary>
    internal static IReadOnlyList<string> FilesWithAPullRequestStep(string root, IEnumerable<string> tracked) =>
        [.. tracked.Where(p => File.Exists(Path.Combine(root, p)))
                   .Where(p => PullRequestStep.IsMatch(File.ReadAllText(Path.Combine(root, p))))];

    [Fact]
    public void NoFileButEnqueue_OpensOrQueuesAPullRequest()
    {
        IReadOnlyList<string> tracked = TrackedTree.Files(NotNarrative);
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        Assert.Contains(TheGuardedStep, tracked);
        var offenders = FilesWithAPullRequestStep(TestRepo.Root, tracked).Where(p => p != TheGuardedStep).ToList();
        Assert.True(offenders.Count == 0,
            "these files open or queue a pull request outside scripts/orchestrator/enqueue.py, whose guard refuses "
            + "unless `ruleset.py check` reports the merge queue ACTIVE (kb/Work PB3024): route the step through "
            + $"`enqueue.py land`:\n  {string.Join("\n  ", offenders)}");
    }

    /// <summary>⭐ The sweep's failure branch, fired: a planted brief with a hand PR step is found, by the same function.</summary>
    [Fact]
    public void TheSweep_FindsAPlantedHandStep()
    {
        string dir = ScratchDirectory.Create("landing-pipeline-");
        Directory.CreateDirectory(Path.Combine(dir, "brief"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "brief", "lander.md"), "7. `gh pr create --title T` then `gh pr merge --auto`.");
            File.WriteAllText(Path.Combine(dir, "brief", "tool.py"), "gh([\"pr\", \"merge\", n, \"--auto\"])");
            File.WriteAllText(Path.Combine(dir, "brief", "fine.md"), "land with enqueue.py queue");
            Directory.CreateDirectory(Path.Combine(dir, "docs"));
            File.WriteAllText(Path.Combine(dir, "docs", "design.md"), "the lander runs `gh pr create` (narrative)");
            var found = FilesWithAPullRequestStep(
                dir, new[] { "brief/lander.md", "brief/tool.py", "brief/fine.md", "docs/design.md" }.Where(NotNarrative.Holds));
            Assert.Equal(new[] { "brief/lander.md", "brief/tool.py" }, found);
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>enqueue.py's queue guard comes before every other step: the ACTIVE test precedes the landing checks,
    /// the push and both pull-request calls in its <c>queue</c> function.</summary>
    [Fact]
    public void TheQueueGuard_PrecedesEveryLandingStep()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        int queueDef = source.IndexOf("\ndef queue(", StringComparison.Ordinal);
        Assert.True(queueDef > 0, "enqueue.py has no `def queue(` — the landing step moved; re-point this test");
        string body = source[queueDef..source.IndexOf("\n# ── self-test", queueDef, StringComparison.Ordinal)];
        int guard = body.IndexOf("if st != ruleset.ACTIVE:", StringComparison.Ordinal);
        Assert.True(guard > 0, "enqueue.py's queue() no longer tests the merge queue for ACTIVE");
        foreach (string step in new[] { "checks(sha, base)", "\"push\"", "\"pr\", \"create\"", "\"pr\", \"merge\"" })
        {
            int at = body.IndexOf(step, StringComparison.Ordinal);
            Assert.True(at > guard, $"in enqueue.py's queue(), `{step}` is {(at < 0 ? "missing" : "BEFORE the queue guard")}");
        }
    }

    /// <summary>enqueue.py's <c>land</c> asks for an ACTIVE queue before it merges main in, gates or stamps, and has no
    /// other path onto main: the ruleset binds administrators, so a gate spent while the queue is off could land nothing,
    /// and the former direct-push arm gated for half an hour with nothing holding main (kb/Work PB3023 review R1-1).</summary>
    [Fact]
    public void TheLandGuard_PrecedesTheMergeTheGateAndTheStamp_AndThereIsNoOtherPath()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        int landDef = source.IndexOf("\ndef land(", StringComparison.Ordinal);
        Assert.True(landDef > 0, "enqueue.py has no `def land(` — the landing step moved; re-point this test");
        string body = source[landDef..source.IndexOf("\ndef ", landDef + 1, StringComparison.Ordinal)];
        int guard = body.IndexOf("if st != ruleset.ACTIVE:", StringComparison.Ordinal);
        Assert.True(guard > 0, "enqueue.py's land() no longer tests the merge queue for ACTIVE");
        foreach (string step in new[] { "tested(wt", "merge(wt, branch, base)", "(rc := gate_once(", "(stamp or _stamp)", "queue(wt" })
        {
            int at = body.IndexOf(step, StringComparison.Ordinal);
            Assert.True(at > guard, $"in enqueue.py's land(), `{step}` is {(at < 0 ? "missing" : "BEFORE the queue guard")}");
        }
        Assert.DoesNotContain("push-main", body);
    }

    [Fact]
    public void TheWorkflow_RunsTheMatrixOnTheMergeGroup_AndTheCheapJobsOnAPullRequest()
    {
        string wf = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml")).ReplaceLineEndings("\n");
        string triggers = wf[..wf.IndexOf("\nconcurrency:", StringComparison.Ordinal)];
        Assert.Matches(new Regex(@"^  merge_group:\s*$", RegexOptions.Multiline), triggers);
        Assert.Matches(new Regex(@"^  pull_request:\s*$", RegexOptions.Multiline), triggers);
        Assert.Matches(new Regex(@"if \[ ""\$EVENT"" = ""pull_request"" \]; then\n(?:\s*#[^\n]*\n)*\s*code=false\n"), wf);
        Assert.DoesNotContain("a pull_request / dispatch run is always the full matrix", wf);
        Assert.DoesNotContain("check-runs?check_name=ci-gate", wf);
        Assert.Contains(".event != \"pull_request\" and .conclusion == \"success\"", wf);
    }

    /// <summary>⛔ SUPERSEDED MERGE-GROUP RUNS ARE CANCELLED (kb/Work PB3769; DESIGN-test-build-ci.md §3.17.2 step 4): the
    /// <c>supersede</c> job runs on the <c>merge_group</c> event alone, waits for no job (it is what the replacement
    /// groups' runs start with), runs <c>cancel_superseded.py</c> with this run's id and head commit, is the only job
    /// granted <c>actions: write</c>, never decides a verdict, and is a <c>needs</c> of <c>ci-gate</c>; the script's
    /// arms (every keep, ignore and fail-safe case) and the queue reader's are declared.</summary>
    [Fact]
    public void TheMergeGroupRun_CancelsTheRunsTheQueueSuperseded_AndNothingElse()
    {
        string wf = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml")).ReplaceLineEndings("\n");
        CiJob? supersede = CiWorkflow.Jobs(wf).FirstOrDefault(j => j.Id == "supersede");
        Assert.True(supersede is not null, "build-and-test.yml has no `supersede` job: superseded merge-group runs hold runners");
        string body = supersede.Code;
        Assert.Matches(new Regex(@"^    if: github\.event_name == 'merge_group'$", RegexOptions.Multiline), body);
        Assert.DoesNotMatch(new Regex(@"^    needs:", RegexOptions.Multiline), body);
        Assert.Matches(new Regex(@"^      actions: write\b", RegexOptions.Multiline), body);
        IReadOnlyList<string> fatalSteps = StepsThatCanFailTheJob(supersede, out int stepCount);
        Assert.True(stepCount >= 3, $"the `supersede` job lists {stepCount} steps; the checkout, the sweep and the checkout-failed "
                                    + "notice are three, so the step reader is not measuring the job");
        Assert.True(fatalSteps.Count == 0, "a `supersede` step is not `continue-on-error: true`, so a housekeeping failure "
            + "(a checkout, a runner or network error) fails the job and `ci-gate` (kb/Work PB3820): " + string.Join("; ", fatalSteps));
        Assert.Matches(new Regex(@"^      - id: checkout\n        uses: actions/checkout@", RegexOptions.Multiline), body);
        Assert.Matches(new Regex(@"^        if: steps\.checkout\.outcome == 'success'\n        continue-on-error: true\n        env:",
            RegexOptions.Multiline), body);
        Assert.Matches(new Regex(@"^        if: steps\.checkout\.outcome != 'success'$", RegexOptions.Multiline), body);
        Assert.Contains("run: python3 scripts/orchestrator/cancel_superseded.py --repo \"$REPO\" --run-id \"$RUN_ID\" "
                        + "--sha \"$SHA\"", body, StringComparison.Ordinal);
        Assert.Contains("RUN_ID: ${{ github.run_id }}", body, StringComparison.Ordinal);
        Assert.Contains("SHA: ${{ github.sha }}", body, StringComparison.Ordinal);
        Assert.True(Regex.Matches(wf, @"^\s+actions:\s*write\b", RegexOptions.Multiline).Count == 1,
            "a job other than `supersede` is granted `actions: write`: only the sweep may cancel a run");
        Assert.Matches(new Regex(@"^\[[^\]]*\bsupersede\b"), CiWorkflow.Jobs(wf).Single(j => j.Id == "ci-gate").Value("needs"));

        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "cancel_superseded.py"), new[]
        {
            "a run whose head commit heads a current queue entry is kept",
            "an unfinished merge-group run whose head commit heads no current queue entry is cancelled, queued or in progress",
            "this run itself is never cancelled, whatever its head commit",
            "a completed run is ignored",
            "a pull_request or push run is ignored, its head commit in no entry",
            "a merge-group run of another branch's queue is ignored",
            "an unreadable queue (None) cancels nothing",
            "an empty queue cancels nothing",
            "a queue read that does not hold this run's own head commit cancels nothing (a stale read, or this run is "
                + "itself superseded)",
            "the runs are listed in every unfinished status BEFORE the queue is read (a run created after the read "
                + "would otherwise look superseded)",
            "a cancel GitHub refuses (the run finished in between) is reported as a warning and the sweep goes on",
            "an offline queue read cancels nothing and says so as a warning",
            "a run list GitHub refuses for some statuses judges only the runs it listed, says which statuses it could "
                + "not list, and cancels nothing unseen",
            "every outcome exits 0 (housekeeping never decides a verdict); a usage error exits 2",
        });
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "queue_facts.py"), new[]
        {
            "live_heads: a queue longer than one page, no merge queue on main, a failed call, a malformed answer or an "
                + "offline gh is None (an incomplete read), never a short list",
        });
    }

    /// <summary>The steps of <paramref name="job"/> that lack <c>continue-on-error: true</c> as a key of their own (a
    /// <c>continue-on-error</c> deeper down, a <c>with:</c> value, is not the step's: <see cref="CiStep.Value"/> reads the
    /// step's own keys). Each is named by <see cref="CiStep.Label"/>.</summary>
    internal static IReadOnlyList<string> StepsThatCanFailTheJob(CiJob job, out int stepCount)
    {
        IReadOnlyList<CiStep> steps = job.Steps;
        stepCount = steps.Count;
        return [.. steps.Where(step => step.Value("continue-on-error") != "true").Select(step => step.Label)];
    }

    /// <summary>A job whose body is <paramref name="body"/> (its lines, indented as the workflow indents them).</summary>
    private static CiJob JobOf(string body) => Assert.Single(CiWorkflow.Jobs("jobs:\n  j:\n" + body));

    [Fact]
    public void TheSupersedeStepReader_NamesEveryStepThatCouldFailTheJob()
    {
        const string job = "    steps:\n"
            + "      - id: checkout\n        uses: actions/checkout@v7\n        continue-on-error: true\n        with:\n          submodules: false\n"
            + "      - name: sweep\n        if: steps.checkout.outcome == 'success'\n        continue-on-error: true   # housekeeping\n        run: x\n"
            + "      - name: notice\n        run: echo\n"
            + "      - uses: actions/other@v1\n        with:\n          continue-on-error: true\n";
        IReadOnlyList<string> fatal = StepsThatCanFailTheJob(JobOf(job), out int count);
        Assert.Equal(4, count);
        Assert.Equal(["notice", "actions/other@v1"], fatal);

        Assert.Empty(StepsThatCanFailTheJob(JobOf("    steps:\n      - name: a\n        continue-on-error: true\n        run: x\n"), out int one));
        Assert.Equal(1, one);
        Assert.Equal(["a"], StepsThatCanFailTheJob(JobOf("    steps:\n      - name: a\n        continue-on-error: false\n        run: x\n"), out _));
        Assert.Empty(StepsThatCanFailTheJob(JobOf("    steps:\n"), out int none));
        Assert.Equal(0, none);
    }

    /// <summary>The retired landing's files, by name: the direct-push landing script (both forms), the landing lease
    /// and the train applier (kb/Work PB3023, PB3024), and the landing oracle, which held a landing to a committed
    /// arch-oracle baseline of the branch's own tree (kb/Work PB3107).</summary>
    private static readonly string[] RetiredScripts =
        ["scripts/push-main.sh", "scripts/push-main.ps1", "scripts/orchestrator/landing_lease.py",
         "scripts/orchestrator/train_apply.py", "scripts/orchestrator/landing_oracle.py"];

    /// <summary>The directory that held the committed arch-oracle baseline until kb/Work PB3107 (declared, kb/Work
    /// PB3531).</summary>
    private static readonly TreeScope RetiredOracleRecords = new(Under: ["docs/rearchitecture/evidence/arch-oracle/"]);

    /// <summary>The files that describe the current landing (DESIGN-test-build-ci.md §3.17.8): the whole tree but the
    /// paths that keep the HISTORY, where a retired name is a fact about the past (the DEVLOG, frozen evidence, the
    /// knowledge base with its work register, and adjudication records). This file names the retired scripts to forbid
    /// them, and is set aside by <see cref="FilesNamingARetiredScript"/>. Declared (kb/Work PB3531). The knowledge base
    /// (<c>kb/</c>, with the Obsidian settings in <c>.obsidian/</c>) is set aside whole, as the sibling sweeps set it aside:
    /// a sweep that read every note of it made the vault a test input, and no register-only change could then skip the
    /// test legs (kb/Work PB2878).</summary>
    private static readonly TreeScope Current = new(Under: [""],
        Except: ["DEVLOG/", "docs/rearchitecture/evidence/", "kb/", ".obsidian/", "adjudication/"]);

    private const string ThisFile = "tests/WiseOwl.Cobol.Tests.Unit/LandingPipelineDriftTests.cs";

    /// <summary>The files of <paramref name="tracked"/> (already narrowed to <see cref="Current"/>) but this one that
    /// name a retired landing script.</summary>
    internal static IReadOnlyList<string> FilesNamingARetiredScript(string root, IEnumerable<string> tracked)
    {
        string[] names = [.. RetiredScripts.Select(Path.GetFileName).Select(n => n!)];
        return [.. tracked.Where(p => p != ThisFile)
                          .Where(p => File.Exists(Path.Combine(root, p)))
                          .Where(p => File.ReadAllText(Path.Combine(root, p)) is var text
                                      && names.Any(n => text.Contains(n, StringComparison.Ordinal)))];
    }

    /// <summary>⛔ THE DIRECT-PUSH LANDING IS RETIRED (kb/Work PB3024; DESIGN-test-build-ci.md §3.17.8): main takes only
    /// queued pull requests, so the scripts that pushed it, the lease that serialized them and the <c>ci/**</c> landing
    /// branches are gone, and no current-state file names them.</summary>
    [Fact]
    public void TheDirectPushLanding_IsRetired_AndNothingNamesIt()
    {
        foreach (string path in RetiredScripts)
            Assert.False(File.Exists(TestRepo.At(path.Split('/'))), $"{path} is back: main takes only queued pull requests (kb/Work PB3024)");
        string wf = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml")).ReplaceLineEndings("\n");
        string push = wf[wf.IndexOf("\n  push:\n", StringComparison.Ordinal)..wf.IndexOf("\n  pull_request:\n", StringComparison.Ordinal)];
        Assert.Matches(new Regex(@"^    branches: \[ main \]\s*$", RegexOptions.Multiline), push);
        Assert.DoesNotContain("ci/**", wf);

        IReadOnlyList<string> tracked = TrackedTree.Files(Current);
        Assert.True(tracked.Count >= 1000, $"`git ls-files` reported only {tracked.Count} paths — the sweep would scan nothing");
        Assert.Contains(ThisFile, tracked);
        var offenders = FilesNamingARetiredScript(TestRepo.Root, tracked);
        Assert.True(offenders.Count == 0,
            "these current-state files name a retired landing script (the direct-push script, the landing lease, the "
            + "train applier or the landing oracle; kb/Work PB3024, PB3107): describe the landing as `enqueue.py land` "
            + "into GitHub's merge queue, or, for a past fact that must stay, say 'the former direct-push landing script', "
            + "'the retired train applier' or 'the retired landing oracle' without the file name:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>⛔ NO LANDING IS HELD TO A COMMITTED RECORD OF ITS OWN TREE (kb/Work PB3107). GitHub's merge queue builds
    /// each pull request's group on every group queued ahead of it, in an order it picks after the branch was checked, so
    /// a record of the branch's own tree cannot describe the tree that lands: two branches that each re-recorded the
    /// arch-oracle baseline on one main left two baselines in the stacked tree, and nine merge-group runs of 2026-10-13
    /// failed on it (38355538654 among them). The oracle's baseline is the capture of a change's base commit, made on
    /// demand (scripts/arch/compare_oracle.py), so no such record is tracked.</summary>
    [Fact]
    public void NoTrackedRecord_DescribesOneBranchsTree()
    {
        // The empty answer is the pass; that the enumeration works is proven by this class's whole-tree sweeps.
        IReadOnlyList<string> records = TrackedTree.Files(RetiredOracleRecords);
        Assert.True(records.Count == 0, $"a committed arch-oracle baseline is back ({string.Join(", ", records)}): the "
            + "merge queue lands a tree no branch recorded; compare against the capture of the base commit instead "
            + "(scripts/arch/compare_oracle.py --base, kb/Work PB3107)");
        string capture = File.ReadAllText(TestRepo.Scripts("arch", "capture_oracle.py"));
        Assert.DoesNotContain("--record", capture);
    }

    /// <summary>⭐ The retirement sweep's failure branch, fired: a planted script and skill that name a retired file are
    /// found by the same function; the history paths and this file are not.</summary>
    [Fact]
    public void TheRetirementSweep_FindsAPlantedName()
    {
        string dir = ScratchDirectory.Create("landing-retired-");
        try
        {
            string[] files = ["scripts/x.py", "skill/SKILL.md", "fine.md", "DEVLOG/0001-x.md", "kb/Work/PB1.md",
                              "kb/Compiler/Pipeline.md", ".obsidian/app.json", ThisFile];
            foreach (string f in files)
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dir, f))!);
            File.WriteAllText(Path.Combine(dir, "scripts/x.py"), "# run by push-main.sh");
            File.WriteAllText(Path.Combine(dir, "skill/SKILL.md"), "the lease (`scripts/orchestrator/landing_lease.py`)");
            File.WriteAllText(Path.Combine(dir, "fine.md"), "land with `enqueue.py land`; the former direct-push landing script");
            File.WriteAllText(Path.Combine(dir, "DEVLOG/0001-x.md"), "train_apply.py landed it");
            File.WriteAllText(Path.Combine(dir, "kb/Work/PB1.md"), "push-main.sh held the lease");
            File.WriteAllText(Path.Combine(dir, "kb/Compiler/Pipeline.md"), "push-main.sh held the lease");
            File.WriteAllText(Path.Combine(dir, ".obsidian/app.json"), "push-main.sh");
            File.WriteAllText(Path.Combine(dir, ThisFile), "push-main.sh");
            Assert.Equal(new[] { "scripts/x.py", "skill/SKILL.md" }, FilesNamingARetiredScript(dir, files.Where(Current.Holds)));
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>ONE <c>land</c> OF A BRANCH AT A TIME (kb/Work PB3310): enqueue.py's <c>land</c> takes the worktree's land
    /// lock before it reads the worktree (a second run would find the first's merge half done) and before anything is
    /// merged, gated, stamped or pushed; the loop's landing lane waits on that same lock for the exit it answers; and
    /// the Linux gate answers BUSY, which the gate driver's one reader and enqueue.py already treat as a stop that is
    /// no red. On 2026-10-16 a second land of one branch deleted the first's Linux clone mid-test and the first held a
    /// gate slot for 6 h 40 min.</summary>
    [Fact]
    public void TheLandLock_PrecedesEveryReadOfTheWorktree_AndTheLaneAndTheLinuxGateAnswerIt()
    {
        string source = File.ReadAllText(TestRepo.Scripts("orchestrator", "enqueue.py"));
        int landDef = source.IndexOf("\ndef land(", StringComparison.Ordinal);
        Assert.True(landDef > 0, "enqueue.py has no `def land(` — the landing step moved; re-point this test");
        string body = source[landDef..source.IndexOf("\ndef ", landDef + 1, StringComparison.Ordinal)];
        int lockAt = body.IndexOf("gate_slot.ExclusiveLock.try_take(", StringComparison.Ordinal);
        Assert.True(lockAt > 0, "enqueue.py's land() no longer takes the worktree's land lock (kb/Work PB3310)");
        foreach (string step in new[] { "worktree_state(wt)", "fetch_main(wt)", "merge(wt, branch, base)", "gates(wt)", "(stamp or _stamp)", "queue(wt" })
        {
            int at = body.IndexOf(step, StringComparison.Ordinal);
            Assert.True(at > lockAt, $"in enqueue.py's land(), `{step}` is {(at < 0 ? "missing" : "BEFORE the land lock")}");
        }
        Assert.Contains("EXIT_LAND_BUSY", body);

        string lane = File.ReadAllText(TestRepo.Scripts("orchestrator", "land_lane.py"));
        Assert.Matches(new Regex(@"WAITS = \{[^}]*EXIT_LAND_BUSY", RegexOptions.Singleline), lane);
        Assert.Contains("if rc == EXIT_LAND_BUSY:", lane);

        string linux = File.ReadAllText(TestRepo.Scripts("linux-gate.sh")).ReplaceLineEndings("\n");
        int begin = linux.IndexOf("\nbegin_gate() {", StringComparison.Ordinal);
        Assert.True(begin > 0, "linux-gate.sh has no begin_gate");
        int busy = linux.IndexOf("busy_line \"$other\"", begin, StringComparison.Ordinal);
        int owner = linux.IndexOf("> \"$gate/owner\"", begin, StringComparison.Ordinal);
        Assert.True(busy > begin && owner > busy, "linux-gate.sh's begin_gate must answer BUSY before it registers its own gate");
        // The one BUSY line (kb/Work PB3649): a second gate's refusal and the --probe the landing lane waits by are
        // worded by busy_line, and the probe answers before anything is cloned or any results directory is emptied.
        Assert.Contains("=== LINUX GATE: BUSY — another Linux gate of this tree is running", linux);
        int probeDef = linux.IndexOf("\nprobe_tree() {", StringComparison.Ordinal);
        Assert.True(probeDef > 0 && linux.IndexOf("busy_line \"$other\"", probeDef, StringComparison.Ordinal) > probeDef
                    && linux.IndexOf("probe_tree \"$GATES_ROOT\" \"$tree\"", StringComparison.Ordinal)
                    < linux.IndexOf("rm -rf \"$out\"; mkdir -p \"$out\"", StringComparison.Ordinal),
            "linux-gate.sh's --probe (probe_tree) must word BUSY by busy_line and answer before anything is cloned or emptied");
        Assert.True(linux.IndexOf("rm -rf \"$out\"; mkdir -p \"$out\"", StringComparison.Ordinal)
                    > linux.IndexOf("\nbegin_gate \"$GATES_ROOT\" \"$tree\"", StringComparison.Ordinal),
            "linux-gate.sh empties the tree's results directory before begin_gate: a second run would delete the first's logs");
    }

    [Fact]
    public void TheQueueAndTheRuleset_DeclareEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "enqueue.py"), new[]
        {
            "the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) refuses with exit 8 before anything is checked, pushed or opened",
            "a landing check's refusal stops the queue with its own exit code and opens nothing",
            "an ACTIVE queue pushes the branch, opens one pull request and queues it with --auto",
            "a re-run reuses the branch's open pull request, moves its head and queues it again",
            "GitHub's refusal of the pull request list, the pull request or the queue is exit 9, never 0",
            "HEAD beside origin/main is refused (main brought in first), and HEAD on main is refused before any check",
            "HEAD at origin/main has nothing to land; an empty title or body is a usage error",
            "checks runs the five landing scripts in order, the range only to the two that read one, and maps each "
                + "refusal to its own code: DEVLOG 6, landing check 3, register 6, diagnostic prefix 6, retired names 6, "
                + "unreadable 2",
            // kb/Work PB3389 review R1-1, PB3575 review R1-1 and PB3837: the register and the two rename audits of the
            // merged head, each read by that head's own script
            "the register check and the two rename audits run the worktree's own scripts (exit {code})",
            // kb/Work PB3023: the whole landing step, one script
            "land: the migrating landing (the whole history added as files) titles its pull request from its ONE new "
                + "entry, the landing rule's own (devlog.landing_entries)",
            "land: a dirty worktree is refused (exit 2) before anything is fetched, merged or gated",
            "land: a conflict bringing main in is exit 4: the merge is aborted, the branch unchanged, the report told "
                + "which files, and nothing gated",
            // kb/Work PB3612: the landing reads the record of the implementer's gate, and gates only where nothing else has
            "land: a tip no recorded GREEN local gate covers is exit 14, UNTESTED, before anything is merged",
            "tested_locally: a GREEN gate run of the committed head's own tree with its Linux half GREEN covers the tip",
            "tested_locally: the gated head still covers the tip after main's commits, a clean merge of main and a DEVLOG-only commit",
            "land: the one gate a landing runs (a lane no one gates) runs on the MERGED head",
            // review R1-1: main takes only queued pull requests, so nothing is merged or gated while the queue is off
            "land: the queue not ACTIVE (ABSENT, DRIFT or SKIPPED) is exit 8 before anything is merged, gated, "
                + "stamped or pushed: main takes only queued pull requests (kb/Work PB3023 review R1-1)",
            "land: a tested tip opens one pull request titled from the DEVLOG entry, whose body carries the entry "
                + "and the commits, and queues it, with no gate run by the landing",
            "land: the queued pull request is measured once, in the branch's worktree, with the gates the landing ran itself",
            "land: a lane no one gates, with no recorded gate, gates its tip ONCE",
            "land: a landing check's refusal keeps its own code, with no gate run",
            // review R1-2: the queue merges other pull requests while the landing queues
            "land: main moving while the landing queues is merged in again WITHOUT any gate and the branch is queued "
                + "on the new main",
            "land: a conflict bringing the moved main in while the landing queues is exit 4",
            // kb/Work PB3673: a clean merge of main that could not be completed is no conflict (a finisher's)
            "land: a clean merge of main that could not be completed for a cause in the ENVIRONMENT",
            "land: a clean merge of main that could not be completed because of the merged tree itself",
            "land: the class of a merge that could not be completed is the gate's ONE classification",
            "land: the same arm at the catch-up merge while the landing queues",
            // kb/Work PB3612: the one exception, a true textual merge in a file both sides changed
            "land: bringing main in changed a file the branch also changes (a clean textual merge in shared.txt): the "
                + "one gate runs ONCE on the merged head",
            "land: bringing main in over files the branch does not change runs no gate",
            "merge_overlap: a file a merge driver merges by key (diagnostics.json, a work note) and one no test reads",
            // MANDATORY-PRACTICES I10, L9 (kb/Work PB3027): only a tip its CLEAN review stamp covers lands
            "review: a tip that moved after its CLEAN review only by main's commits, a clean merge of main and a "
                + "DEVLOG-only commit is still reviewed",
            "review: a commit after the CLEAN review that is not mechanical (a finisher's fix, a hand-resolved "
                + "merge) is exit 11 before anything is merged or gated, the report told why",
            "review: a FINDINGS stamp or no report is unreviewed; only a lane I10 does not review, named with "
                + "--lane, lands without a stamp",
            // kb/Work PB3103, PB3104: every gate row at enqueue is a landing gate under the shared gate slot
            "the gate at enqueue is a LANDING gate: its slot is served before waiting implementer gates "
                + "(kb/Work PB3103, gate_slot.py CLASS_ENV)",
            "gate rows: every row of GATES_WINDOWS and GATES_LINUX hands the gate to run_gate_legs.py, which takes the "
                + "shared gate slot (kb/Work PB3104); the landing has no Linux row of its own",
            "gate rows: a planted unslotted row (a bare row, a script that does not hand the gate to the driver, no "
                + "script, a driver that no longer acquires) is refused, and run_gates runs no row (kb/Work PB3104)",
            // kb/Work PB3527: a held gate lock is BUSY, never a red; the queue watch's remedies live in the only file that queues
            "land: a gate BUSY (another gate holds the worktree's gate lock) is exit 12, never a red: no report line, "
                + "no stamp, nothing queued (kb/Work PB3527)",
            "land: a second land of a branch whose first still runs is REFUSED at the land lock, exit 13, naming the "
                + "holder: nothing gated, stamped or reported (kb/Work PB3310); a dry run takes no lock; an early "
                + "outcome releases it",
            "gate rows: each verdict line is read by the gate driver's one reader: BUSY is BUSY (neither a pass nor "
                + "a red), the Linux gate's own BUSY line included (kb/Work PB3310), and a GREEN line with a non-zero "
                + "exit is RED (kb/Work PB3527); a gate that printed no verdict line measured nothing: NOT RUN "
                + "(kb/Work PB3526)",
            // kb/Work PB3526, review R2-1 of wave 1150: a NOT RUN blocks by its cause's class, and main is measured
            "land: a gate NOT RUN for a cause in the ENVIRONMENT (a host with no SDK; or no gate row at all) is exit "
                + "2, never a red",
            "land: a BRANCH-carried NOT RUN (the tree's own gate driver failed) on a main that passes the same gate "
                + "is exit 5 with a BLOCKED line naming the cause and main's reading",
            "land: a BRANCH-carried NOT RUN that main fails the SAME way at the same step (its cause) is exit 2 with "
                + "no report line",
            "land: an UNCLASSIFIED NOT RUN (no known cause) is BRANCH-carried",
            "gate_main: main is measured in a clean checkout of its head",
            "the queue watch's remedies: a re-run of the stuck check's run, then close, reopen and auto-merge again "
                + "(in that order, each naming the repository); refused touching nothing while the queue is not ACTIVE; a "
                + "failed close stops before anything else (kb/Work PB3527)",
            "the queued line `queue` prints is the one its reader parses (the lane records the pull request's number)",
            // kb/Work PB3662: a gate still running owes a verdict; it is exit 12, never the terminal exit 14
            "land: a gate of the worktree still RUNNING (its gate lock is held, no verdict file) is exit 12, GATE "
                + "BUSY, never exit 14",
            // kb/Work PB3145: a merged predecessor's stamped entry, its pull request still open, is a wait (exit 15)
            "land: a branch carrying the stamped entry of a merged predecessor whose pull request is still OPEN into "
                + "main",
            "land: GitHub that cannot list the open pull requests is exit 9",
            "land: once the predecessor's pull request has merged the same head lands",
        });
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts("orchestrator", "ruleset.py"), new[]
        {
            "a live ruleset equal to the committed one, the queue present and no classic protection, is ACTIVE",
            "extra fields GitHub adds to the live ruleset, and its own rule order, are not drift",
            "no ruleset on main is ABSENT, never ACTIVE",
            "a live ruleset that differs (a parameter, a missing rule, enforcement off) is DRIFT with each difference named",
            "the classic protection still on main beside the ruleset is DRIFT",
            "no merge queue reported by GitHub, or a repository setting the queue needs turned off, is DRIFT",
            "no gh, no network or a server error is SKIPPED, never ACTIVE",
            "apply refuses without the owner's go, and with it sets the repository, creates or updates the ruleset by "
                + "name and deletes the classic protection",
            "the committed ruleset requires a pull request, the merge queue with a merge commit and ci-gate, with no bypass",
            // The pair guard (kb/Work PB3024, the first apply's HTTP 422): GitHub accepts three (title, message) pairs.
            "the committed repository settings name a merge-commit pair GitHub accepts",
            "apply refuses MERGE_MESSAGE+PR_BODY (GitHub's invalid_merge_commit_setting_combo, the first apply's 422) "
                + "before any call reaches GitHub",
        });
    }
}
