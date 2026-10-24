// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE CLASSIFICATION DECIDES THE INERT LANE, AND EVERY LINUX TEST RUN PROVES IT (kb/Work PB2878;
/// DESIGN-test-build-ci.md §3.18): CI's <c>changes</c> job, the local gate (<c>scripts/run_gate_legs.py</c>) and the
/// Linux gate (<c>scripts/linux-gate.sh</c>) each decide whether a change skips the test legs by calling
/// <c>scripts/inert_lane.py</c>, and no path list of their own; every Linux step of CI that runs <c>dotnet test</c>, and
/// the Linux gate's test legs, run under its inotify watch; and every INERT and READ entry names a tracked path.
/// </summary>
/// <remarks>
/// <para>
/// The lane exists because a change no test reads cannot turn a test red (owner 2026-10-09: "Should this require a full
/// CI run? I would think not"), and it is sound only while that "no test reads" is TRUE. The doc-only regex CI carried
/// before it was not a classification: it skipped the matrix for <c>docs/</c> and <c>kb/Work/</c>, which twenty-odd
/// tests read, so a kb/Work-only branch could land behind green CI and turn the next landing red (kb/Work PB3603). Two
/// lists of "what no test reads" drift apart, and a list nobody proves drifts from the tests; so there is one list, and
/// the watch proves it on every Linux test run: a test that opens an inert path is a RED naming the path.
/// </para>
/// <para>
/// What this class holds is the WIRING, which a refactor of a workflow step or a gate script would silently drop:
/// a <c>dotnet test</c> step that loses its watch stops proving the classification, and a runner that grows its own
/// path list stops agreeing with the others (the CI invariant, kb/Work PB1957). The classification's logic and the
/// watch's firing are <c>inert_lane.py --self-test</c>'s arms, which the self-test runner runs in every gate and CI.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class InertLaneDriftTests
{
    private const string Classifier = "scripts/inert_lane.py";

    private static readonly string Workflow = File.ReadAllText(TestRepo.At(".github", "workflows", "build-and-test.yml"));

    /// <summary>The jobs of the workflow, each as (its id, its text).</summary>
    private static IEnumerable<(string Id, string Text)> Jobs()
    {
        string jobs = Workflow[Workflow.IndexOf("\njobs:\n", StringComparison.Ordinal)..];
        foreach (string part in Regex.Split(jobs, @"^  (?=[A-Za-z0-9_-]+:\s*$)", RegexOptions.Multiline).Skip(1))
        {
            yield return (part[..part.IndexOf(':')], part);
        }
    }

    [Fact]
    public void TheChangesJob_DecidesTheMatrixThroughTheOneClassification_AndCarriesNoPathList()
    {
        string changes = Assert.Single(Jobs(), j => j.Id == "changes").Text;
        Assert.Contains($"python3 {Classifier} classify --base", changes, StringComparison.Ordinal);
        // A regex of repository paths is the doc-only list this replaced: one more copy of "what no test reads".
        Assert.False(Regex.IsMatch(changes, @"grep -[A-Za-z]*E[A-Za-z]* ""\$IGNORE""|IGNORE='|\^\((DEVLOG|docs|kb)/"),
            "the `changes` job carries a path list of its own again; the inert lane has ONE classification, "
            + $"{Classifier} (kb/Work PB2878)");
    }

    [Fact]
    public void EveryLinuxDotnetTestStep_RunsUnderTheInertWatch()
    {
        var linux = Jobs().Where(j => Regex.IsMatch(j.Text, @"^\s+runs-on:\s*ubuntu", RegexOptions.Multiline)).ToList();
        var steps = linux.SelectMany(j => Regex.Matches(j.Text, @"^.*\bdotnet test tests/.*$", RegexOptions.Multiline)
            .Select(m => (j.Id, Line: m.Value.Trim()))).ToList();
        Assert.True(steps.Count >= 4,
            $"found only {steps.Count} `dotnet test` step(s) in the Linux jobs: the scan is broken, not the workflow");
        var unwatched = steps.Where(s => !s.Line.Contains($"{Classifier} watch -- dotnet test", StringComparison.Ordinal))
            .Select(s => $"{s.Id}: {s.Line}").ToList();
        Assert.True(unwatched.Count == 0,
            "a Linux `dotnet test` step runs outside the inert watch, so a test that starts reading an inert path is "
            + "seen by no one (kb/Work PB2878): " + string.Join(" | ", unwatched));
    }

    [Fact]
    public void TheGates_DecideTheLaneThroughTheOneClassification_AndTheLinuxGateWatchesItsTestLegs()
    {
        string linuxGate = File.ReadAllText(TestRepo.Scripts("linux-gate.sh"));
        Assert.Contains("scripts/inert_lane.py\" classify", linuxGate, StringComparison.Ordinal);
        Assert.Contains("scripts/inert_lane.py watch", linuxGate, StringComparison.Ordinal);
        string driver = File.ReadAllText(TestRepo.Scripts("run_gate_legs.py"));
        Assert.Contains("import inert_lane", driver, StringComparison.Ordinal);
        Assert.Contains("inert_lane.CLASSIFICATION.classify(", driver, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryInertAndReadEntry_NamesATrackedPath()
    {
        var run = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("inert_lane.py"), "check");
        Assert.True(run.Stdout.Contains("=== INERT LANE CHECK: GREEN", StringComparison.Ordinal),
            $"inert_lane.py check is not GREEN:\n{run.Stdout}\n{run.Stderr}");
    }
}
