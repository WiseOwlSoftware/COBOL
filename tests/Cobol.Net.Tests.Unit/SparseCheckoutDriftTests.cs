// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A CI JOB THAT CHECKS OUT ONLY SOME PATHS LISTS EVERY REPOSITORY FILE ITS SCRIPTS REACH (kb/Work PB3787):
/// every job of <c>.github/workflows/build-and-test.yml</c> whose checkout has a <c>sparse-checkout</c> list (the
/// <c>supersede</c> job of kb/Work PB3769 and <c>dotnet-sdk-cache</c> today) runs on a tree without the rest, so each
/// file its steps name, each file a local action it uses names, and each repository module a Python script it runs
/// imports as it starts (transitively) must be inside that list; the job sets <c>sparse-checkout-cone-mode: false</c>,
/// the mode the list is read in. <c>supersede</c>'s step is <c>continue-on-error</c>, so a missing
/// <c>scripts/standard_streams.py</c> made it cancel nothing while the run stayed green and no test failed (kb/Work
/// PB3637 added the file by hand). <see cref="SparseCheckoutAudit"/> reads the closure; the arms below hold the real
/// workflow and the audit's own failure branches.
/// </summary>
/// <remarks>
/// A function body's import is lazy and runs only when the function does; the audit follows what a script imports as it
/// starts, as <c>cancel_superseded.py --self-test</c> reproduced in kb/Work PB3787 (the reviewer's <c>git archive</c> of
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
        Assert.Contains(jobs, j => j.Id == "dotnet-sdk-cache");
        Assert.Empty(SparseCheckoutAudit.Problems(jobs, ReadRepo));
    }

    [Fact]
    public void TheSupersedeJobAndTheSdkCacheJob_ReachTheFilesTheyRun_AndNothingLazy()
    {
        IReadOnlyList<SparseCheckoutAudit.SparseJob> jobs = RealJobs();
        string[] supersede = [.. SparseCheckoutAudit.Reach(jobs.Single(j => j.Id == "supersede"), ReadRepo).Select(r => r.Path)];
        foreach (string file in new[] { "scripts/orchestrator/cancel_superseded.py", "scripts/orchestrator/queue_facts.py",
                     "scripts/orchestrator/ruleset.py", "scripts/standard_streams.py" })
            Assert.Contains(file, supersede);
        // standard_streams.live_python imports live_tools inside the function: lazy, on the path of no CI job.
        Assert.DoesNotContain("scripts/live_tools.py", supersede);

        string[] cache = [.. SparseCheckoutAudit.Reach(jobs.Single(j => j.Id == "dotnet-sdk-cache"), ReadRepo).Select(r => r.Path)];
        Assert.Contains(".github/actions/setup-dotnet/action.yml", cache);
        Assert.Contains("scripts/install-dotnet-sdk.sh", cache);
    }

    /// <summary>The acceptance: the real workflow without <c>scripts/standard_streams.py</c> in the supersede list is
    /// flagged, naming the file and the script that imports it.</summary>
    [Fact]
    public void TheRealSupersedeJob_WithoutStandardStreams_IsFlagged()
    {
        string wf = File.ReadAllText(TestRepo.At(Workflow.Split('/'))).ReplaceLineEndings("\n");
        const string Entry = "\n            scripts/standard_streams.py\n";
        Assert.Contains(Entry, wf, StringComparison.Ordinal);
        string without = wf.Replace(Entry, "\n", StringComparison.Ordinal);
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
    public void ALazyImport_ADocstring_AndAComment_AreNotFollowed_ButAClassBodyAndAnIfBlockAre()
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
        Assert.Equal(["tools/run.py", "class_mod.py", "if_mod.py"], reached);
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
