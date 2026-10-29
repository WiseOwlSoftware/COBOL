// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A CI JOB THAT CHECKS OUT ONLY SOME PATHS LISTS EVERY REPOSITORY FILE ITS SCRIPTS REACH (kb/Work PB3787):
/// every job of <c>.github/workflows/build-and-test.yml</c> whose checkout has a <c>sparse-checkout</c> list (the
/// <c>supersede</c> job of kb/Work PB3769, <c>toolchain-cache</c> and <c>ci-gate</c> of kb/Work PB4007 today, and the
/// toolchain-freshness workflow) runs on a tree without the rest, so each
/// file its steps name, each file a local action it uses names, and each repository module a Python script it runs
/// imports anywhere in its text (transitively, a function body's lazy import included) must be inside that list; the job sets <c>sparse-checkout-cone-mode: false</c>,
/// the mode the list is read in. <c>supersede</c>'s step is <c>continue-on-error</c>, so a missing
/// <c>scripts/standard_streams.py</c> made it cancel nothing while the run stayed green and no test failed (kb/Work
/// PB3637 added the file by hand). <see cref="SparseCheckoutAudit"/> reads the closure; the arms below hold the real
/// workflow and the audit's own failure branches.
/// </summary>
/// <remarks>
/// A function body's import is lazy and runs when the function does, on the job's path as often as not (the repository's
/// scripts hold 176 of them, <c>main()</c> importing <c>enqueue</c>, <c>devlog</c> and the like), so the audit follows every
/// import statement of a reached script wherever it stands (kb/Work PB3810): an over-approximated closure fails loudly in
/// this test, an under-approximated one fails silently on the runner. The first shape was
/// <c>cancel_superseded.py --self-test</c> as kb/Work PB3787 reproduced it (the reviewer's <c>git archive</c> of
/// <c>scripts/orchestrator</c> alone fails with <c>ModuleNotFoundError: No module named 'standard_streams'</c>).
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class SparseCheckoutDriftTests
{
    private const string Workflow = ".github/workflows/build-and-test.yml";

    private static readonly TreeScope Workflows = new(Under: [".github/workflows/"], Kinds: [".yml", ".yaml"]);

    private static string? ReadRepo(string path)
    {
        string full = TestRepo.At(path.Split('/'));
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }

    private static Func<string, string?> Repo(params (string Path, string Text)[] files) =>
        path => files.Where(f => f.Path == path).Select(f => f.Text).FirstOrDefault();

    /// <summary>A workflow with one job, <paramref name="id"/>, whose checkout lists <paramref name="list"/>.</summary>
    private static IReadOnlyList<SparseCheckoutAudit.SparseJob> OneJob(string id, string[] list, string steps, bool coneOff = true) =>
        SparseCheckoutAudit.SparseJobs(SparseCheckoutAudit.Jobs("w.yml",
            "jobs:\n"
            + $"  {id}:\n    runs-on: ubuntu-26.04\n    steps:\n      - uses: actions/checkout@v7\n        with:\n          sparse-checkout: |\n"
            + string.Concat(list.Select(p => $"            {p}\n"))
            + (coneOff ? "          sparse-checkout-cone-mode: false\n" : "")
            + steps + "\n"));

    private static IReadOnlyList<SparseCheckoutAudit.SparseJob> RealJobs(string? workflowText = null) =>
        SparseCheckoutAudit.SparseJobs(workflowText is null
            ? [.. TrackedTree.Files(Workflows).SelectMany(f => SparseCheckoutAudit.Jobs(f, File.ReadAllText(TestRepo.At(f.Split('/')))))]
            : SparseCheckoutAudit.Jobs(Workflow, workflowText));

    [Fact]
    public void EverySparseCheckoutJob_ListsEveryRepositoryFileItReaches()
    {
        IReadOnlyList<SparseCheckoutAudit.SparseJob> jobs = RealJobs();
        // The population: a reader that found no sparse job would pass over nothing.
        int declared = TrackedTree.Files(Workflows).Sum(f =>
            File.ReadAllLines(TestRepo.At(f.Split('/'))).Count(l => l.TrimStart().StartsWith("sparse-checkout:", StringComparison.Ordinal)));
        Assert.True(declared >= 2, "the workflows declare fewer than two sparse-checkout lists: the reader or the workflow moved");
        Assert.Equal(declared, jobs.Count);
        Assert.Contains(jobs, j => j.Id == "supersede");
        Assert.Contains(jobs, j => j.Id == "toolchain-cache");
        IReadOnlyList<string> problems = SparseCheckoutAudit.Problems(jobs, ReadRepo);
        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }

    [Fact]
    public void TheSupersedeJobAndTheToolchainCacheJob_ReachTheFilesTheyRun_TheLazyImportsIncluded()
    {
        IReadOnlyList<SparseCheckoutAudit.SparseJob> jobs = RealJobs();
        string[] supersede = [.. SparseCheckoutAudit.Reach(jobs.Single(j => j.Id == "supersede"), ReadRepo).Select(r => r.Path)];
        foreach (string file in new[] { "scripts/orchestrator/cancel_superseded.py", "scripts/orchestrator/queue_facts.py",
                     "scripts/orchestrator/ruleset.py", "scripts/standard_streams.py" })
            Assert.Contains(file, supersede);
        // standard_streams.live_python imports live_tools inside the function: lazy, and reached all the same (kb/Work PB3810).
        Assert.Contains("scripts/live_tools.py", supersede);

        string[] cache = [.. SparseCheckoutAudit.Reach(jobs.Single(j => j.Id == "toolchain-cache"), ReadRepo).Select(r => r.Path)];
        Assert.Contains(".github/actions/setup-toolchain/action.yml", cache);
        Assert.Contains("scripts/install-tool.sh", cache);
        Assert.Contains("toolchain.env", cache);
        Assert.Contains("scripts/toolchain.py", cache);
    }

    /// <summary>The acceptance: the real workflow without <c>scripts/standard_streams.py</c> in the supersede list is
    /// flagged, naming the file and the script that imports it.</summary>
    [Fact]
    public void TheRealSupersedeJob_WithoutStandardStreams_IsFlagged()
    {
        string wf = File.ReadAllText(TestRepo.At(Workflow.Split('/'))).ReplaceLineEndings("\n");
        const string Entry = "\n            scripts/standard_streams.py\n";
        // The supersede job's list is the file's first; the toolchain-cache and ci-gate lists name the file too.
        int at = wf.IndexOf(Entry, StringComparison.Ordinal);
        Assert.True(at > wf.IndexOf("\n  supersede:", StringComparison.Ordinal) && at < wf.IndexOf("\n  audits:", StringComparison.Ordinal),
            "the first sparse list naming scripts/standard_streams.py is no longer the supersede job's");
        string without = wf[..at] + "\n" + wf[(at + Entry.Length)..];
        IReadOnlyList<string> problems = SparseCheckoutAudit.Problems(RealJobs(without), ReadRepo);
        string problem = Assert.Single(problems);
        Assert.Contains("job `supersede`", problem, StringComparison.Ordinal);
        Assert.Contains("`scripts/standard_streams.py`", problem, StringComparison.Ordinal);
        Assert.Contains("scripts/orchestrator/", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void ATransitiveImport_LeftOutOfTheList_IsFlaggedWithItsChain()
    {
        var repo = Repo(
            ("tools/run.py", "import json\nimport mid  # noqa\n"),
            ("tools/mid.py", "from leaf import thing\n"),
            ("leaf.py", "thing = 1\n"));
        var jobs = OneJob("j", ["tools"], "      - run: python3 tools/run.py");
        string problem = Assert.Single(SparseCheckoutAudit.Problems(jobs, repo));
        Assert.Contains("`leaf.py` (reached from tools/mid.py)", problem, StringComparison.Ordinal);

        Assert.Empty(SparseCheckoutAudit.Problems(OneJob("j", ["tools", "leaf.py"], "      - run: python3 tools/run.py"), repo));
    }

    [Fact]
    public void TheScriptTheJobRuns_MustBeInTheListToo()
    {
        var repo = Repo(("tools/run.py", "pass\n"), ("other/x.py", "pass\n"));
        string problem = Assert.Single(SparseCheckoutAudit.Problems(OneJob("j", ["other"], "      - run: python3 tools/run.py"), repo));
        Assert.Contains("`tools/run.py` (reached from w.yml job `j`)", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void ADocstring_AndAComment_AreNotFollowed_ButALazyImportAClassBodyAndAnIfBlockAre()
    {
        var repo = Repo(
            ("tools/run.py",
                "\"\"\"Usage:\n    import docstring_mod\n\"\"\"\n"
                + "# import comment_mod\n"
                + "def f(\n    a,\n):\n    import lazy_mod\n    return a\n"
                + "async def g():\n    from lazy2 import x\n"
                + "class C:\n    import class_mod\n    def m(self):\n        import method_mod\n"
                + "if True:\n    import if_mod\n"),
            ("docstring_mod.py", ""), ("comment_mod.py", ""), ("lazy_mod.py", ""), ("lazy2.py", ""),
            ("class_mod.py", ""), ("method_mod.py", ""), ("if_mod.py", ""));
        string[] reached = [.. SparseCheckoutAudit.Reach(OneJob("j", ["tools"], "      - run: python3 tools/run.py").Single(), repo).Select(r => r.Path)];
        Assert.Equal(["tools/run.py", "lazy_mod.py", "lazy2.py", "class_mod.py", "method_mod.py", "if_mod.py"], reached);
    }

    /// <summary>The acceptance of kb/Work PB3810: a repository module a script imports inside <c>main()</c> and the
    /// sparse list leaves out is flagged with the script that imports it; listing it clears the job.</summary>
    [Fact]
    public void ALazilyImportedModule_LeftOutOfTheList_IsFlaggedWithItsChain()
    {
        var repo = Repo(
            ("tools/run.py", "def main():\n    import coord\n    coord.go()\n"),
            ("coord.py", "pass\n"));
        string problem = Assert.Single(SparseCheckoutAudit.Problems(OneJob("j", ["tools"], "      - run: python3 tools/run.py"), repo));
        Assert.Contains("`coord.py` (reached from tools/run.py)", problem, StringComparison.Ordinal);

        Assert.Empty(SparseCheckoutAudit.Problems(OneJob("j", ["tools", "coord.py"], "      - run: python3 tools/run.py"), repo));
    }

    /// <summary>The reader of kb/Work PB3810: the jobs are the keys beneath <c>jobs:</c>, never the <c>on:</c> triggers
    /// above it, on the real workflow and on a planted one.</summary>
    [Fact]
    public void TheJobReader_SeesTheKeysBeneathJobsAndNotTheTriggers()
    {
        string[] real = [.. SparseCheckoutAudit.Jobs(Workflow, File.ReadAllText(TestRepo.At(Workflow.Split('/')))).Select(j => j.Id)];
        Assert.Contains("supersede", real);
        Assert.Contains("toolchain-cache", real);
        Assert.DoesNotContain("push", real);
        Assert.DoesNotContain("pull_request", real);
        Assert.DoesNotContain("merge_group", real);

        string[] planted = [.. SparseCheckoutAudit.Jobs("w.yml",
            "on:\n  push:\n    branches: [main]\n  merge_group:\njobs:\n  only:\n    runs-on: ubuntu-26.04\n    steps:\n      - run: true\n").Select(j => j.Id)];
        Assert.Equal(["only"], planted);
    }

    [Fact]
    public void RelativeImports_Packages_AndSubmodules_AreResolved()
    {
        var repo = Repo(
            ("pkg/run.py", "from . import sibling\nfrom .sub import deep\nfrom helpers import (\n    one,\n    two as t,\n)\nimport pkg2.mod\n"),
            ("pkg/sibling.py", ""), ("pkg/sub/__init__.py", ""), ("pkg/sub/deep.py", ""),
            ("helpers/__init__.py", ""), ("helpers/one.py", ""), ("helpers/two.py", ""),
            ("pkg2/__init__.py", ""), ("pkg2/mod.py", ""));
        string[] reached = [.. SparseCheckoutAudit.Reach(OneJob("j", ["x"], "      - run: python3 pkg/run.py").Single(), repo).Select(r => r.Path).Order()];
        Assert.Equal(["helpers/__init__.py", "helpers/one.py", "helpers/two.py", "pkg/run.py", "pkg/sibling.py", "pkg/sub/__init__.py",
            "pkg/sub/deep.py", "pkg2/__init__.py", "pkg2/mod.py"], reached);
    }

    [Fact]
    public void ALocalActionAndTheScriptsItRuns_AreReached_AndAMentionInAComment_IsNot()
    {
        var repo = Repo(
            (".github/actions/setup/action.yml", "runs:\n  steps:\n    - run: bash scripts/install.sh # not scripts/commented.sh\n    # bash scripts/commented-line.sh\n"),
            ("scripts/install.sh", "# see scripts/other.sh\nsource scripts/lib.sh\n"),
            ("scripts/lib.sh", "echo hi\n"), ("scripts/other.sh", ""), ("scripts/commented.sh", ""), ("scripts/commented-line.sh", ""));
        var jobs = OneJob("j", [".github/actions"], "      # scripts/step-comment.sh\n      - uses: ./.github/actions/setup");
        string[] problems = [.. SparseCheckoutAudit.Problems(jobs, repo)];
        Assert.Equal(2, problems.Length);
        Assert.Contains(problems, p => p.Contains("`scripts/install.sh` (reached from .github/actions/setup/action.yml)", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("`scripts/lib.sh` (reached from scripts/install.sh)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("scripts/a.py", "scripts/a.py", true)]
    [InlineData("scripts", "scripts/a.py", true)]
    [InlineData("scripts/", "scripts/sub/a.py", true)]
    [InlineData("scripts", "scriptsx/a.py", false)]
    [InlineData("scripts/*.py", "scripts/a.py", true)]
    [InlineData("scripts/*.py", "scripts/sub/a.py", false)]
    [InlineData("scripts/**/*.py", "scripts/sub/deep/a.py", true)]
    [InlineData("/global.json", "global.json", true)]
    [InlineData("global.json", "src/global.json", true)]
    [InlineData("/global.json", "src/global.json", false)]
    [InlineData("scripts/a.py", "scripts/b.py", false)]
    public void ASparsePattern_CoversAsGitReadsItWithoutConeMode(string pattern, string path, bool covered) =>
        Assert.Equal(covered, SparseCheckoutAudit.Covers([pattern], path));

    [Fact]
    public void ACone_OrANegation_IsRefused_AndAJobWithoutASparseListIsIgnored()
    {
        var repo = Repo(("a.py", ""));
        string cone = Assert.Single(SparseCheckoutAudit.Problems(OneJob("j", ["a.py"], "      - run: python3 a.py", coneOff: false), repo));
        Assert.Contains("sparse-checkout-cone-mode: false", cone, StringComparison.Ordinal);
        string negation = Assert.Single(SparseCheckoutAudit.Problems(OneJob("j", ["a.py", "!b.py"], "      - run: python3 a.py"), repo));
        Assert.Contains("negation", negation, StringComparison.Ordinal);
        Assert.Empty(SparseCheckoutAudit.SparseJobs(SparseCheckoutAudit.Jobs("w.yml",
            "jobs:\n  full:\n    runs-on: ubuntu-26.04\n    steps:\n      - uses: actions/checkout@v7\n      - run: python3 a.py\n")));
    }
}
