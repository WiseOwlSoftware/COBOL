// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONE READER SPLITS A CI WORKFLOW INTO JOBS AND STEPS, AND NO TEST READS THE SHAPE ITSELF (kb/Work PB3819): every drift
/// test that audits <c>.github/workflows/*.yml</c> or a composite action's <c>steps:</c> asks <see cref="CiWorkflow"/> for
/// the jobs, their steps and their comments, and no other file under <c>tests/</c> carries a job-key regex or a
/// <c>jobs:</c> anchor of its own.
/// </summary>
/// <remarks>
/// Before PB3819 ten classes split the one workflow their own ways: over the whole file (the <c>on:</c> triggers read as
/// jobs), with a step list read at one fixed depth, with a banner attributed to the job above it, with comments kept or dropped inside a step, with a different set
/// of characters in a job id. A workflow edit one reader tolerated another misread, and a drift test then passed
/// vacuously or went red on a legal change. The reader's own arms are held on a fixture workflow below; the sweep holds
/// every other test file off the shape.
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class CiWorkflowDriftTests
{
    /// <summary>The test sources the sweep reads: every C# file under <c>tests/</c> but the reader and this class (the
    /// rule's owners, which name the patterns to forbid; kb/Work PB3531).</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"],
        Except: ["tests/_shared/CiWorkflow.cs", "tests/WiseOwl.Cobol.Tests.Unit/CiWorkflowDriftTests.cs"],
        Kinds: [".cs"]);

    /// <summary>A job key matched by a regex of the test's own (<c>^  (?&lt;id&gt;…</c>, <c>^  (?=[A-Za-z0-9_-]+:</c>,
    /// <c>^  ([A-Za-z0-9_-]+):</c>), or a hand-found <c>jobs:</c> anchor (<c>IndexOf("\njobs:</c>, <c>== "jobs:"</c>,
    /// <c>StartsWith("jobs:"</c>), or a hand-found <c>steps:</c> key (<c>== "steps:"</c>): a step splitter that reads the
    /// first list at one fixed depth misses every job whose list sits elsewhere.</summary>
    private static readonly Regex OwnSplitter = new(
        @"\^ {2}\((?:\?(?:<\w+>|=|:))?\[A-Za-z0-9_|IndexOf\(""\\njobs:|==\s*""jobs:""|StartsWith\(""jobs:""|==\s*""steps:""",
        RegexOptions.NonBacktracking);

    /// <summary>The lines of <paramref name="text"/> that read the job shape themselves.</summary>
    internal static IReadOnlyList<string> Splitters(string path, string text) =>
        [.. text.ReplaceLineEndings("\n").Split('\n')
            .Select((line, i) => (line, at: i + 1))
            .Where(l => !l.line.TrimStart().StartsWith("//", StringComparison.Ordinal) && OwnSplitter.IsMatch(l.line))
            .Select(l => $"{path}:{l.at}: {l.line.Trim()}")];

    [Fact]
    public void NoTestSplitsAWorkflowIntoJobsItself()
    {
        var files = TrackedTree.Files(TestSources);
        Assert.True(files.Count > 50, $"the sweep read {files.Count} test source(s): it reads nothing");
        var offenders = files.SelectMany(f => Splitters(f, File.ReadAllText(TestRepo.At(f.Split('/'))))).ToList();
        Assert.True(offenders.Count == 0,
            "a test splits a CI workflow into jobs with a reader of its own; ask CiWorkflow (tests/_shared/CiWorkflow.cs) "
            + "for the jobs and steps instead (kb/Work PB3819):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheSweepFiresOnEachSplitterShapeItForbids()
    {
        Assert.Single(Splitters("a.cs", "var k = new Regex(@\"^  (?<id>[A-Za-z0-9_-]+):\");"));
        Assert.Single(Splitters("a.cs", "Regex.Split(w, @\"^  (?=[A-Za-z0-9_-]+:\\s*$)\")"));
        Assert.Single(Splitters("a.cs", "var j = new Regex(@\"^  ([A-Za-z0-9_-]+):\\s*$\");"));
        Assert.Single(Splitters("a.cs", "int jobs = lf.IndexOf(\"\\njobs:\\n\", StringComparison.Ordinal);"));
        Assert.Single(Splitters("a.cs", "int s = Array.FindIndex(w, l => l.TrimEnd() == \"jobs:\");"));
        Assert.Single(Splitters("a.cs", "int s = Array.FindIndex(w, l => l.StartsWith(\"jobs:\", StringComparison.Ordinal));"));
        Assert.Single(Splitters("a.cs", "int key = code.FindIndex(l => l.Trim() == \"steps:\");"));
        Assert.Empty(Splitters("a.cs", "// ^  (?<id>[A-Za-z0-9_-]+): was the old reader"));
        Assert.Empty(Splitters("a.cs", "foreach (CiJob job in CiWorkflow.Jobs(workflow)) { }"));
    }

    // ── the reader's own arms, on a fixture workflow ─────────────────────────────────────────────────────────────────

    private const string Fixture = """
        name: fixture
        on:
          push:
            branches: [main]
          pull_request:
          merge_group:

        # a top-level comment above jobs
        jobs:
          # ── the first job ──
          first:
            runs-on: ubuntu-26.04
            steps:
              - uses: actions/checkout@v7
                with:
                  continue-on-error: true
              # a comment above the build step
              - name: Build
                continue-on-error: true   # housekeeping
                run: |
                  set -e
                  # a shell comment inside the script

                  dotnet build x.sln
              - id: last
                run: echo done

          # ── the second job: its banner names --list-tests ──
          second.job:
            runs-on: windows-2025-vs2026
            needs: [first]
            steps:
            - run: echo same-indent list
            - run: echo second item
          third:
            runs-on: ${{ matrix.os }}

        permissions:
          contents: read
        """;

    [Fact]
    public void TheJobs_AreTheKeysUnderJobs_NotTheTriggers()
    {
        var jobs = CiWorkflow.Jobs(Fixture);
        Assert.Equal(["first", "second.job", "third"], jobs.Select(j => j.Id));
        Assert.True(CiWorkflow.HasJobs(Fixture));
        Assert.False(CiWorkflow.HasJobs("runs:\n  steps:\n    - run: x\n"));
        // The key after the jobs block (`permissions:`) is not a job's line.
        Assert.DoesNotContain("permissions", jobs[^1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ABannerDocumentsTheJobBelowIt_AndNeverTheJobAbove()
    {
        var jobs = CiWorkflow.Jobs(Fixture);
        Assert.Contains("the second job", string.Concat(jobs[1].Banner), StringComparison.Ordinal);
        Assert.DoesNotContain("--list-tests", jobs[0].Text, StringComparison.Ordinal);
        Assert.Contains("the first job", string.Concat(jobs[0].Banner), StringComparison.Ordinal);
        Assert.DoesNotContain("# ──", jobs[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ACommentInsideAJobStaysInItsLines_AndCodeDropsIt()
    {
        CiJob first = CiWorkflow.Jobs(Fixture)[0];
        Assert.Contains("# a comment above the build step", first.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("# a comment above the build step", first.Code, StringComparison.Ordinal);
        Assert.Contains("dotnet build x.sln", first.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSteps_AreTheItemsOfTheStepsList_WithAMultiLineRunBlockWhole()
    {
        CiJob first = CiWorkflow.Jobs(Fixture)[0];
        IReadOnlyList<CiStep> steps = first.Steps;
        Assert.Equal(3, steps.Count);
        Assert.Equal(["actions/checkout@v7", "Build", "last"], steps.Select(s => s.Label));

        CiStep build = steps[1];
        Assert.Contains("# a shell comment inside the script", build.Text, StringComparison.Ordinal);
        Assert.Contains("dotnet build x.sln", build.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("echo done", build.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("a comment above the build step", build.Text, StringComparison.Ordinal);
        Assert.Equal("|", build.Value("run"));
    }

    [Fact]
    public void AKeyDeeperThanTheStepIsNotTheSteps()
    {
        CiStep checkout = CiWorkflow.Jobs(Fixture)[0].Steps[0];
        Assert.Contains("continue-on-error: true", checkout.Text, StringComparison.Ordinal);   // under `with:`
        Assert.Null(checkout.Value("continue-on-error"));
        Assert.Equal("true", CiWorkflow.Jobs(Fixture)[0].Steps[1].Value("continue-on-error"));   // its own, comment stripped
    }

    [Fact]
    public void AJobsOwnKeys_AreItsOwn_NotItsStepsOrItsMatrix()
    {
        var jobs = CiWorkflow.Jobs(Fixture);
        Assert.Equal("ubuntu-26.04", jobs[0].Value("runs-on"));
        Assert.Equal("[first]", jobs[1].Value("needs"));
        Assert.Equal("${{ matrix.os }}", jobs[2].Value("runs-on"));
        Assert.Null(jobs[0].Value("uses"));
        Assert.Empty(jobs[2].Steps);
    }

    [Fact]
    public void AStepListAtTheKeysOwnIndentation_IsRead()
    {
        CiJob second = CiWorkflow.Jobs(Fixture)[1];
        Assert.Equal(2, second.Steps.Count);
        Assert.Equal("echo second item", second.Steps[1].Value("run"));
    }

    [Fact]
    public void ACompositeActionsStepsAreRead_AsTheJobsAre()
    {
        const string Action = """
            name: x
            runs:
              using: composite
              steps:
                - uses: actions/cache@v5
                - run: echo hi
                  shell: bash
            """;
        IReadOnlyList<CiStep> steps = CiWorkflow.Steps(Action);
        Assert.Equal(2, steps.Count);
        Assert.Equal("bash", steps[1].Value("shell"));
    }

    [Fact]
    public void CrLfLineEndings_ReadTheSameAsLf()
    {
        var lf = CiWorkflow.Jobs(Fixture);
        var crlf = CiWorkflow.Jobs(Fixture.ReplaceLineEndings("\r\n"));
        Assert.Equal(lf.Select(j => j.Text), crlf.Select(j => j.Text));
    }

    [Fact]
    public void AWorkflowTheReaderCannotReadIsRefused_NeverReadAsEmpty()
    {
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Jobs("on:\n  push:\n"));
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Jobs("jobs:\n"));
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Jobs("jobs:\n    orphan: 1\n"));
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Job(Fixture, "absent"));
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Steps("steps: [a, b]\n"));
        Assert.Throws<InvalidDataException>(() => CiWorkflow.Steps("steps:\n  key: value\n"));
    }
}
