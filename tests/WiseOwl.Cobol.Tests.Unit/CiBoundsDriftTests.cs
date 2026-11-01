// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ CI IS BOUNDED, AND ITS CACHES ARE SAVED FROM MAIN ONLY (kb/Work PB4227, PB4324; DESIGN-test-build-ci.md §1.4):
/// every job of every workflow under <c>.github/workflows/</c> states a <c>timeout-minutes</c> (a hung step otherwise holds
/// a merge-queue slot for GitHub's six hours); every <c>actions/checkout</c> step and every
/// <c>./.github/actions/setup-toolchain</c> step states one of at most five minutes; every job's first step is a checkout
/// that is retried once with the same <c>with:</c> block; no step saves a cache from a branch ref (<c>actions/cache@</c>
/// restores AND saves, so a merge group, whose ref is new every time, would save a copy nothing reads again), no cache save
/// follows a <c>continue-on-error</c> step that produced what it saves without requiring that step's success (a failed
/// <c>dotnet restore</c> would otherwise store a partial package set under the exact key, kb/Work PB4348), and the
/// toolchain action caches Python's tool-cache directory with the rest, restored before <c>actions/setup-python</c>.
/// </summary>
/// <remarks>
/// Merge-group run 41226218068 (pull request #893, 2026-10-31) sat 45 minutes in <c>actions/checkout</c> of a job whose code
/// was fine, and the queue removed the pull request. The workflow had no <c>timeout-minutes</c> at all. The reader is
/// <see cref="CiWorkflow"/> (kb/Work PB3819); each audit below has a fixture arm that fires on the shape it forbids.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class CiBoundsDriftTests
{
    private static readonly TreeScope Workflows = new(Under: [".github/workflows/"], Kinds: [".yml", ".yaml"]);
    private static readonly TreeScope Actions = new(Under: [".github/actions/"], Kinds: [".yml"]);

    /// <summary>The longest a job may run: the nightly workflow's bound (kb/Work PB4332); a pull-request job is far below it.</summary>
    private const int MaxJobMinutes = 60;

    /// <summary>The longest a checkout or toolchain step may run before it is killed and (the checkout) retried.</summary>
    private const int MaxStepMinutes = 5;

    private const string ToolchainAction = "./.github/actions/setup-toolchain";
    private const string MainOnly = "github.ref == 'refs/heads/main'";

    private static string Read(string path) => File.ReadAllText(TestRepo.At(path.Split('/')));

    private static bool IsCheckout(CiStep step) => step.Value("uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true;

    private static bool IsBounded(CiStep step) => IsCheckout(step) || step.Value("uses") == ToolchainAction;

    private static bool InRange(string? value, int max) => int.TryParse(value, out int n) && n >= 1 && n <= max;

    /// <summary>The step's <c>with:</c> block, comments dropped: what the retry must repeat.</summary>
    private static string WithBlock(CiStep step) =>
        string.Join('\n', step.CodeLines.SkipWhile(l => l.Trim() != "with:").Select(l => l.TrimEnd()));

    /// <summary>Jobs that state no <c>timeout-minutes</c> of 1 to <see cref="MaxJobMinutes"/> minutes.</summary>
    internal static (int Jobs, List<string> Offenders) JobTimeoutOffenders(string file, string workflow)
    {
        var offenders = new List<string>();
        IReadOnlyList<CiJob> jobs = CiWorkflow.Jobs(workflow);
        foreach (CiJob job in jobs.Where(j => !InRange(j.Value("timeout-minutes"), MaxJobMinutes)))
            offenders.Add($"  {file}: job `{job.Id}` states no `timeout-minutes` of 1 to {MaxJobMinutes} (it is `{job.Value("timeout-minutes") ?? "absent"}`)");
        return (jobs.Count, offenders);
    }

    /// <summary>Checkout and toolchain steps that state no <c>timeout-minutes</c> of 1 to <see cref="MaxStepMinutes"/>.</summary>
    internal static (int Steps, List<string> Offenders) StepTimeoutOffenders(string file, string workflow)
    {
        var offenders = new List<string>();
        int seen = 0;
        foreach (CiJob job in CiWorkflow.Jobs(workflow))
            foreach (CiStep step in job.Steps.Where(IsBounded))
            {
                seen++;
                if (!InRange(step.Value("timeout-minutes"), MaxStepMinutes))
                    offenders.Add($"  {file}: job `{job.Id}` step `{step.Label}` states no `timeout-minutes` of 1 to {MaxStepMinutes}");
            }
        return (seen, offenders);
    }

    /// <summary>Jobs whose first two steps are not a checkout that continues on error and the same checkout, run only when
    /// the first did not succeed (<c>if: steps.&lt;id&gt;.outcome != 'success'</c>), with the same <c>with:</c> block.</summary>
    internal static List<string> CheckoutRetryOffenders(string file, string workflow)
    {
        var offenders = new List<string>();
        foreach (CiJob job in CiWorkflow.Jobs(workflow))
        {
            IReadOnlyList<CiStep> steps = job.Steps;
            if (steps.Count < 2 || !IsCheckout(steps[0]) || !IsCheckout(steps[1]))
            {
                offenders.Add($"  {file}: job `{job.Id}` does not begin with a checkout and its one retry");
                continue;
            }
            string? id = steps[0].Value("id");
            if (id is null || steps[0].Value("continue-on-error") != "true")
                offenders.Add($"  {file}: job `{job.Id}`'s first checkout needs an `id` and `continue-on-error: true`, or its failure ends the job before the retry");
            else if (steps[1].Value("if")?.Contains($"steps.{id}.outcome != 'success'", StringComparison.Ordinal) != true)
                offenders.Add($"  {file}: job `{job.Id}`'s retry checkout is not `if: steps.{id}.outcome != 'success'`");
            if (WithBlock(steps[0]) != WithBlock(steps[1]))
                offenders.Add($"  {file}: job `{job.Id}`'s retry checkout does not repeat the first checkout's `with:` block");
        }
        return offenders;
    }

    /// <summary>Cache steps that save from a branch: <c>actions/cache@</c> (restore and save) anywhere, and an
    /// <c>actions/cache/save@</c> whose own <c>if</c>, or its job's, does not require the main ref. <paramref name="steps"/> are
    /// one job's (or a composite action's) steps; <paramref name="jobIf"/> is the job's own <c>if</c>.</summary>
    internal static IEnumerable<string> CacheSaveOffenders(string where, IEnumerable<CiStep> steps, string? jobIf)
    {
        foreach (CiStep step in steps)
        {
            string uses = step.Value("uses") ?? "";
            if (uses.StartsWith("actions/cache@", StringComparison.Ordinal))
                yield return $"  {where}: step `{step.Label}` is `{uses}`, which saves on every ref: restore with `actions/cache/restore@` and save from the toolchain-cache job";
            else if (uses.StartsWith("actions/cache/save@", StringComparison.Ordinal)
                     && step.Value("if")?.Contains(MainOnly, StringComparison.Ordinal) != true
                     && jobIf?.Contains(MainOnly, StringComparison.Ordinal) != true)
                yield return $"  {where}: step `{step.Label}` saves a cache without requiring `{MainOnly}` in its `if` or its job's";
        }
    }

    internal static List<string> WorkflowCacheOffenders(string file, string workflow) =>
        [.. CiWorkflow.Jobs(workflow).SelectMany(j => CacheSaveOffenders($"{file}: job `{j.Id}`", j.Steps, j.Value("if")))];

    private static bool IsCacheStep(CiStep step) => step.Value("uses")?.StartsWith("actions/cache/", StringComparison.Ordinal) == true;

    private static bool IsCacheSave(CiStep step) => step.Value("uses")?.StartsWith("actions/cache/save@", StringComparison.Ordinal) == true;

    /// <summary>Cache saves whose producer continues on error and is not tested. The producer is the nearest earlier step
    /// that is not itself a cache step (the one that wrote what the save stores); when it is <c>continue-on-error</c>, the
    /// save's <c>if</c> must require <c>steps.&lt;its id&gt;.outcome == 'success'</c>. A failed producer otherwise leaves a
    /// partial set that the save stores under the exact key, and a run that hits an exact key never saves it again
    /// (kb/Work PB4348). <paramref name="steps"/> are one job's (or a composite action's) steps.</summary>
    internal static IEnumerable<string> UntestedProducerOffenders(string where, IReadOnlyList<CiStep> steps)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (!IsCacheSave(steps[i]))
                continue;
            int p = i - 1;
            while (p >= 0 && IsCacheStep(steps[p]))
                p--;
            if (p < 0 || steps[p].Value("continue-on-error") != "true")
                continue;
            string? id = steps[p].Value("id");
            if (id is null)
                yield return $"  {where}: step `{steps[i].Label}` saves what `{steps[p].Label}` produced, which continues on error and has no `id` to test";
            else if (steps[i].Value("if")?.Contains($"steps.{id}.outcome == 'success'", StringComparison.Ordinal) != true)
                yield return $"  {where}: step `{steps[i].Label}` saves what `{steps[p].Label}` produced without requiring `steps.{id}.outcome == 'success'`";
        }
    }

    /// <summary>What keeps the toolchain action from caching Python's tool-cache directory: it must restore (and save) the
    /// directory the manifest step names, and restore before <c>actions/setup-python</c> looks for it.</summary>
    internal static List<string> PythonCacheOffenders(string action)
    {
        var offenders = new List<string>();
        List<CiStep> steps = [.. CiWorkflow.Steps(action)];
        int restore = steps.FindIndex(s => s.Value("uses")?.StartsWith("actions/cache/restore@", StringComparison.Ordinal) == true);
        int python = steps.FindIndex(s => s.Value("uses")?.StartsWith("actions/setup-python@", StringComparison.Ordinal) == true);
        if (restore < 0 || python < 0 || restore > python)
            offenders.Add("  the cache restore does not come before actions/setup-python: Python is downloaded in every job");
        foreach (CiStep step in steps.Where(s => s.Value("uses")?.StartsWith("actions/cache/", StringComparison.Ordinal) == true))
            if (!step.Text.Contains("steps.manifest.outputs.python-dir", StringComparison.Ordinal))
                offenders.Add($"  step `{step.Label}` does not cache Python's directory (`steps.manifest.outputs.python-dir`)");
        if (!steps.Any(s => s.Text.Contains("$TOOL_CACHE/Python/", StringComparison.Ordinal)))
            offenders.Add("  the manifest step names no `$TOOL_CACHE/Python/<version>` directory, the one actions/setup-python finds");
        if (!steps.Any(s => s.Value("uses")?.StartsWith("actions/cache/save@", StringComparison.Ordinal) == true
                            && s.Value("if")?.Contains(MainOnly, StringComparison.Ordinal) == true))
            offenders.Add($"  no `actions/cache/save@` step requires `{MainOnly}`: a queue branch would save a copy nothing reads again");
        return offenders;
    }

    [Fact]
    public void EveryJob_StatesATimeout()
    {
        var offenders = new List<string>();
        int jobs = 0;
        foreach (string file in TrackedTree.Files(Workflows))
        {
            var (n, found) = JobTimeoutOffenders(file, Read(file));
            jobs += n;
            offenders.AddRange(found);
        }
        Assert.True(offenders.Count == 0, "a CI job with no time box holds a merge-queue slot until GitHub's limit when a step hangs "
            + "(kb/Work PB4227: merge-group run 41226218068 sat 45 minutes in a checkout):\n" + string.Join("\n", offenders));
        Assert.True(jobs >= 15, $"the audit read {jobs} job(s) under .github/workflows: the reader or the workflows moved");
    }

    [Fact]
    public void EveryCheckoutAndToolchainStep_StatesAShortTimeout()
    {
        var offenders = new List<string>();
        int steps = 0;
        foreach (string file in TrackedTree.Files(Workflows))
        {
            var (n, found) = StepTimeoutOffenders(file, Read(file));
            steps += n;
            offenders.AddRange(found);
        }
        Assert.True(offenders.Count == 0, "a checkout or toolchain step with no short time box hangs its whole job (kb/Work PB4324):\n"
            + string.Join("\n", offenders));
        Assert.True(steps >= 30, $"the audit read {steps} checkout and toolchain step(s): the reader or the workflows moved");
    }

    [Fact]
    public void EveryJob_RetriesItsCheckoutOnce()
    {
        var offenders = new List<string>();
        foreach (string file in TrackedTree.Files(Workflows))
            offenders.AddRange(CheckoutRetryOffenders(file, Read(file)));
        Assert.True(offenders.Count == 0, "a checkout that fails once fails the job and the queue removes a branch whose code is fine "
            + "(kb/Work PB4324):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void NoCacheIsSavedFromABranch()
    {
        var offenders = new List<string>();
        int saves = 0;
        foreach (string file in TrackedTree.Files(Workflows))
        {
            string text = Read(file);
            offenders.AddRange(WorkflowCacheOffenders(file, text));
            saves += CiWorkflow.Jobs(text).SelectMany(j => j.Steps).Count(s => s.Value("uses")?.StartsWith("actions/cache/save@", StringComparison.Ordinal) == true);
        }
        foreach (string file in TrackedTree.Files(Actions))
        {
            IReadOnlyList<CiStep> steps = CiWorkflow.Steps(Read(file));
            offenders.AddRange(CacheSaveOffenders(file, steps, null));
            saves += steps.Count(s => s.Value("uses")?.StartsWith("actions/cache/save@", StringComparison.Ordinal) == true);
        }
        Assert.True(offenders.Count == 0, "a cache saved from a branch ref is never read again (a merge group's ref is new every time) and "
            + "fills the 10 GB budget that main's own copies need (kb/Work PB4324):\n" + string.Join("\n", offenders));
        Assert.True(saves >= 3, $"the audit read {saves} cache save step(s): the reader or the workflows moved");
    }

    [Fact]
    public void NoCacheSavesWhatAFailedStepLeftBehind()
    {
        var offenders = new List<string>();
        int saves = 0;
        foreach (string file in TrackedTree.Files(Workflows))
            foreach (CiJob job in CiWorkflow.Jobs(Read(file)))
            {
                offenders.AddRange(UntestedProducerOffenders($"{file}: job `{job.Id}`", job.Steps));
                saves += job.Steps.Count(IsCacheSave);
            }
        foreach (string file in TrackedTree.Files(Actions))
            offenders.AddRange(UntestedProducerOffenders(file, CiWorkflow.Steps(Read(file))));
        Assert.True(offenders.Count == 0, "a cache saved after its producing step failed stores a partial set under the exact key, and no "
            + "later run saves that key again (kb/Work PB4348):\n" + string.Join("\n", offenders));
        Assert.True(saves >= 2, $"the audit read {saves} cache save step(s) under .github/workflows: the reader or the workflows moved");
    }

    [Fact]
    public void ThePythonToolCacheIsPartOfTheToolchainCache()
    {
        var offenders = PythonCacheOffenders(Read(".github/actions/setup-toolchain/action.yml"));
        Assert.True(offenders.Count == 0, "actions/setup-python downloads Python 3.15 from GitHub in every job (48.9 s in each of the "
            + "eight Windows jobs) unless the toolchain cache holds its directory (kb/Work PB4324):\n" + string.Join("\n", offenders));
    }

    /// <summary>The design pointer this class's summary carries must lead to the paragraph that pins this class: the
    /// summary reaches every implementer through <c>scripts/drift_rules.py</c>, and a pointer to the wrong section (it named
    /// §3.17, the landing pipeline, while the paragraph sat in §1.4; kb/Work PB4349) sends each of them to the wrong place.</summary>
    [Fact]
    public void TheSummaryCitesTheDesignSectionThatPinsThisClass()
    {
        string source = Read("tests/WiseOwl.Cobol.Tests.Unit/CiBoundsDriftTests.cs");
        var (doc, number) = CitedDesignSection(source);
        string section = DesignSection(Read($"docs/rearchitecture/{doc}"), number);
        Assert.True(section.Contains(nameof(CiBoundsDriftTests), StringComparison.Ordinal),
            $"the summary cites {doc} §{number}, which does not mention {nameof(CiBoundsDriftTests)}: cite the section that holds the paragraph "
            + "(kb/Work PB4349), or add the paragraph there");
        Assert.Contains("CI is bounded", section, StringComparison.Ordinal);

        // The reader fires on the shape it forbids: a pointer to another section, and a section that does not exist.
        const string Doc = "# D\n\n## 1 One\n\ntext Pinned\n\n### 1.1 Sub\n\nnested\n\n## 2 Two\n\nother\n";
        Assert.Contains("Pinned", DesignSection(Doc, "1"), StringComparison.Ordinal);
        Assert.Contains("nested", DesignSection(Doc, "1"), StringComparison.Ordinal);
        Assert.DoesNotContain("other", DesignSection(Doc, "1"), StringComparison.Ordinal);
        Assert.DoesNotContain("Pinned", DesignSection(Doc, "2"), StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => DesignSection(Doc, "3"));
        Assert.Equal(("DESIGN-x.md", "3.17"), CitedDesignSection("(kb/Work PB1; DESIGN-x.md §3.17): text"));
    }

    /// <summary>The <c>DESIGN-*.md §N.M</c> pointer in <paramref name="source"/>'s first such citation.</summary>
    internal static (string Doc, string Number) CitedDesignSection(string source)
    {
        Match m = Regex.Match(source, @"(DESIGN-[\w-]+\.md) §(\d+(?:\.\d+)*)");
        return m.Success ? (m.Groups[1].Value, m.Groups[2].Value)
            : throw new InvalidOperationException("the summary cites no DESIGN-*.md §N.M section");
    }

    /// <summary>The text of the heading numbered <paramref name="number"/> and everything below it, up to the next heading of
    /// the same or a shallower level.</summary>
    internal static string DesignSection(string document, string number)
    {
        string[] lines = document.Split('\n');
        var heading = new Regex(@"^(#+) (\d+(?:\.\d+)*)[ .]");
        int start = Array.FindIndex(lines, l => heading.Match(l) is { Success: true } m && m.Groups[2].Value == number);
        if (start < 0)
            throw new InvalidOperationException($"the document has no heading numbered {number}");
        int depth = heading.Match(lines[start]).Groups[1].Length;
        int end = Array.FindIndex(lines, start + 1, l => heading.Match(l) is { Success: true } m && m.Groups[1].Length <= depth);
        return string.Join('\n', lines[start..(end < 0 ? lines.Length : end)]);
    }

    // ── each audit fires on the shape it forbids, on a fixture ───────────────────────────────────────────────────────

    private const string Build = """
        jobs:
          build:
            runs-on: ubuntu-26.04
            timeout-minutes: 15
            steps:
              - id: checkout
                uses: actions/checkout@v7
                timeout-minutes: 5
                continue-on-error: true  # one retry follows
                with:
                  submodules: false
                  # a comment only the first carries
                  fetch-depth: 0
              - if: steps.checkout.outcome != 'success'
                uses: actions/checkout@v7
                timeout-minutes: 5
                with:
                  submodules: false
                  fetch-depth: 0
              - name: Setup the toolchain
                uses: ./.github/actions/setup-toolchain
                timeout-minutes: 5
              - uses: actions/cache/restore@v6
              - if: ${{ github.ref == 'refs/heads/main' }}
                uses: actions/cache/save@v6
        """;

    private const string Warm = """
        jobs:
          warm:
            if: github.ref == 'refs/heads/main' && github.event_name == 'push'
            runs-on: ubuntu-26.04
            timeout-minutes: 20
            steps:
              - uses: actions/cache/save@v6
        """;

    private static string Without(string text, string part) => text.Replace(part, "", StringComparison.Ordinal);

    private static string With(string text, string from, string to) => text.Replace(from, to, StringComparison.Ordinal);

    [Fact]
    public void TheAudits_FireOnEveryShapeTheyForbid()
    {
        // The job's timeout.
        var (jobs, none) = JobTimeoutOffenders("w.yml", Build + "\n" + Warm.Replace("jobs:\n", "", StringComparison.Ordinal));
        Assert.Equal((2, 0), (jobs, none.Count));
        Assert.Single(JobTimeoutOffenders("w.yml", Without(Build, "    timeout-minutes: 15\n")).Offenders);
        Assert.Single(JobTimeoutOffenders("w.yml", With(Build, "timeout-minutes: 15", "timeout-minutes: 0")).Offenders);
        Assert.Single(JobTimeoutOffenders("w.yml", With(Build, "timeout-minutes: 15", "timeout-minutes: 361")).Offenders);
        Assert.Single(JobTimeoutOffenders("w.yml", With(Build, "timeout-minutes: 15", "timeout-minutes: ${{ vars.T }}")).Offenders);

        // The checkout and toolchain steps' timeouts: a job's own is not a step's.
        var (steps, quick) = StepTimeoutOffenders("w.yml", Build);
        Assert.Equal((3, 0), (steps, quick.Count));
        Assert.Equal(3, StepTimeoutOffenders("w.yml", With(Build, "        timeout-minutes: 5", "        timeout-minutes: 6")).Offenders.Count);
        Assert.Single(StepTimeoutOffenders("w.yml", Without(Build, "uses: ./.github/actions/setup-toolchain\n        timeout-minutes: 5\n")
            .Replace("- name: Setup the toolchain\n", "- name: Setup the toolchain\n        uses: ./.github/actions/setup-toolchain\n", StringComparison.Ordinal)).Offenders);

        // The checkout and its one retry.
        Assert.Empty(CheckoutRetryOffenders("w.yml", Build));
        Assert.Contains(CheckoutRetryOffenders("w.yml", Warm), o => o.Contains("`warm`", StringComparison.Ordinal));
        string retry = "      - if: steps.checkout.outcome != 'success'\n        uses: actions/checkout@v7\n        timeout-minutes: 5\n        with:\n          submodules: false\n          fetch-depth: 0\n";
        Assert.Contains(retry, Build, StringComparison.Ordinal);
        Assert.Single(CheckoutRetryOffenders("w.yml", Without(Build, retry)));
        Assert.Single(CheckoutRetryOffenders("w.yml", With(Build, "if: steps.checkout.outcome != 'success'", "if: always()")));
        Assert.Single(CheckoutRetryOffenders("w.yml", Without(Build, "        continue-on-error: true  # one retry follows\n")));
        Assert.Single(CheckoutRetryOffenders("w.yml", With(Build, "          fetch-depth: 0\n      - name: Setup", "          fetch-depth: 1\n      - name: Setup")));

        // Caches saved from a branch.
        Assert.Empty(WorkflowCacheOffenders("w.yml", Build));
        Assert.Empty(WorkflowCacheOffenders("w.yml", Warm));
        Assert.Single(WorkflowCacheOffenders("w.yml", With(Build, "actions/cache/restore@v6", "actions/cache@v6")));
        Assert.Single(WorkflowCacheOffenders("w.yml", With(Build, "- if: ${{ github.ref == 'refs/heads/main' }}\n", "- name: save\n")));
        Assert.Single(WorkflowCacheOffenders("w.yml", With(Warm, "github.ref == 'refs/heads/main' && ", "")));

        // A save after a producer that continues on error: tested, untested, a producer with no id, and a second producer.
        const string Produce = """
            jobs:
              warm:
                steps:
                  - id: restore
                    continue-on-error: true
                    run: dotnet restore
                  - uses: actions/cache/restore@v6
                  - if: ${{ steps.restore.outcome == 'success' }}
                    uses: actions/cache/save@v6
                  - run: fetch
                  - uses: actions/cache/save@v6
            """;
        IReadOnlyList<CiStep> StepsOf(string text) => CiWorkflow.Jobs(text)[0].Steps;
        Assert.Empty(UntestedProducerOffenders("w.yml", StepsOf(Produce)));
        Assert.Single(UntestedProducerOffenders("w.yml", StepsOf(With(Produce, "steps.restore.outcome == 'success'", "always()"))));
        Assert.Single(UntestedProducerOffenders("w.yml", StepsOf(With(Produce, "- id: restore", "- name: restore"))));
        Assert.Single(UntestedProducerOffenders("w.yml", StepsOf(With(Produce, "- run: fetch", "- continue-on-error: true\n        run: fetch"))));

        // Python's directory in the toolchain cache.
        const string Action = """
            runs:
              using: composite
              steps:
                - id: manifest
                  run: echo "python-dir=$TOOL_CACHE/Python/$python"
                - id: cache
                  uses: actions/cache/restore@v6
                  with:
                    path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}
                - uses: actions/setup-python@v7
                - if: ${{ github.ref == 'refs/heads/main' }}
                  uses: actions/cache/save@v6
                  with:
                    path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}
            """;
        Assert.Empty(PythonCacheOffenders(Action));
        const string PythonStep = "    - uses: actions/setup-python@v7\n";
        const string RestoreStep = "    - id: cache\n      uses: actions/cache/restore@v6\n      with:\n        path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}\n";
        Assert.Contains(RestoreStep + PythonStep, Action, StringComparison.Ordinal);
        Assert.Single(PythonCacheOffenders(With(Action, RestoreStep + PythonStep, PythonStep + RestoreStep)));
        Assert.Contains(PythonCacheOffenders(With(Action, "python-dir", "dir")), o => o.Contains("python-dir", StringComparison.Ordinal));
        Assert.Single(PythonCacheOffenders(With(Action, "github.ref == 'refs/heads/main'", "true")));
        Assert.NotEmpty(PythonCacheOffenders(With(Action, "actions/cache/restore@v6", "actions/cache/other@v6")));
    }
}
