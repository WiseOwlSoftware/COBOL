// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The drift guard for the conformance suite's two runners (DESIGN-conformance-suite.md §5, §9; kb/Work PB3176): the
/// in-process runner (<see cref="ConformanceSuiteRunner"/>) and <c>conformance-suite/harness/run.py</c> driving the
/// <c>cobol</c> command through the WiseOwl adapter give EVERY test of the per-edition suites the same verdict, and
/// run exactly the same tests; and the two output normalizations, <see cref="CutRunner.Normalize"/> and
/// <c>run.py</c>'s <c>normalize()</c>, both map every case of the one shared fixture
/// (<c>conformance-suite/harness/normalization-fixture.json</c>) to its stated result; and the WiseOwl adapter runs a
/// program under the same default locales as <see cref="CutRunner"/>.
/// </summary>
/// <remarks>
/// <para><b>Why both runners exist and why they must agree.</b> <c>run.py</c> is what any compiler runs (Python standard
/// library only, z/OS included); the in-process runner is WiseOwl's fast one, compiling through the compiled-program
/// cache. Each states the verdict logic once in its own language, and each reads the adapter's diagnostic pattern and
/// the profile its own way; a difference between them would make WiseOwl's own gate green on a suite a user's
/// <c>run.py</c> run reports red. These tests run both on the same generated suites and name every test whose verdict
/// differs, with both reasons.</para>
/// <para><b>The verdict comparison is PARTITIONED</b> (<see cref="BothRunnersVerdictDriftTestsBase{TSlot}"/>;
/// <see cref="TestPartitioning"/>; kb/Work PB4325): <c>run.py</c> starts a <c>cobol</c> process per compile and one per
/// run, so its cost grows with the suite, and as ONE test it was the longest case of every run (166 s local, 352 s on
/// Windows CI) and set the floor of the Conformance leg's wall. Each partition runs its even share of the suite's
/// tests; the class below holds the whole-population assertions that no partition can make.</para>
/// </remarks>
public sealed class ConformanceSuiteRunnerDriftTests
{
    /// <summary>The two runners run exactly the same tests: the tests of the generator's per-edition suites (what
    /// <c>run.py</c> runs) are the wiseowl manifest's in-suite tests (what the in-process runner judges), edition by
    /// edition. A WHOLE-population assertion, so it lives here and not on the partitioned base, where each partition
    /// would repeat it. It starts no process: both sides are read from the one generation
    /// (<see cref="SuiteManifest.Generated"/>).</summary>
    [Fact]
    public void BothRunners_RunExactlyTheSameTests()
    {
        var theirs = RunPyHarness.GeneratedSuites(SuiteManifest.Generated)
            .SelectMany(s => s.Ids.Select(id => (s.Edition, Id: id)))
            .ToHashSet();
        Assert.NotEmpty(theirs);
        var ours = RunPyHarness.InSuiteTests().Select(t => (t.Edition, t.Id)).ToHashSet();
        Assert.NotEmpty(ours);

        var onlyOurs = ours.Except(theirs).Order().ToList();
        var onlyTheirs = theirs.Except(ours).Order().ToList();
        Assert.True(onlyOurs.Count == 0 && onlyTheirs.Count == 0,
            "the two runners run different tests: the wiseowl manifest's in-suite tests and the per-edition suites "
            + $"disagree. Only in-process: {string.Join(", ", onlyOurs.Select(t => $"{t.Edition}/{t.Id}"))}. "
            + $"Only run.py: {string.Join(", ", onlyTheirs.Select(t => $"{t.Edition}/{t.Id}"))}.");
    }

    /// <summary>The two runners run a program under the same default locales: <see cref="CutRunner.RunExit"/> pins
    /// both to <see cref="CutRunner.LocalePin"/>, and the WiseOwl adapter's <c>environment</c> must set the same pin
    /// for <c>run.py</c>, or a host's regional setting would reach one runner's verdict and not the other's
    /// (docs/CONFORMANCE.md DOC-A.1-118: the defaults are the environment's).</summary>
    [Fact]
    public void BothRunners_RunAProgramUnderTheSameDefaultLocales()
    {
        string adapter = Path.Combine(SuiteManifest.Root, "adapters", ConformanceSuite.Compiler + ".json");
        using var doc = JsonDocument.Parse(File.ReadAllText(adapter));
        Assert.True(doc.RootElement.TryGetProperty("environment", out JsonElement environment),
            $"{adapter} sets no environment, so run.py runs a program under the host's default locales");
        foreach (string variable in new[] { WiseOwl.Cobol.Runtime.LocaleState.UserDefaultVariable, WiseOwl.Cobol.Runtime.LocaleState.SystemDefaultVariable })
        {
            Assert.True(environment.TryGetProperty(variable, out JsonElement value) && value.GetString() == CutRunner.LocalePin,
                $"{adapter} must set {variable} to {CutRunner.LocalePin}, the pin of CutRunner.RunExit");
        }
    }

    [Fact]
    public void BothNormalizations_MapTheSharedFixtureToItsStatedResults()
    {
        string fixture = TestRepo.At("conformance-suite", "harness", "normalization-fixture.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(fixture));
        var cases = doc.RootElement.GetProperty("cases").EnumerateArray()
            .Select(c => (Name: c.GetProperty("name").GetString()!, Input: c.GetProperty("input").GetString()!,
                Normalized: c.GetProperty("normalized").GetString()!))
            .ToList();
        Assert.NotEmpty(cases);

        // Python: run.py's own normalize(), imported from the harness, over the same inputs. json.dumps escapes every
        // non-ASCII character, so the console's encoding cannot alter what comes back.
        const string Code = "import json, sys; sys.path.insert(0, sys.argv[2]); import run; "
            + "cases = json.load(open(sys.argv[1], encoding='utf-8'))['cases']; "
            + "print(json.dumps([run.normalize(c['input']) for c in cases]))";
        ProcessObservation python = PythonInstrument.Run(ObservationBudget.OneProcess, "-c", Code, fixture, Path.GetDirectoryName(ConformanceSuite.Harness)!);
        Assert.True(python.ExitCode == 0, $"run.py's normalize() could not be run on the fixture:\n{python.Stdout}\n{python.Stderr}");
        string[] pythonResults = JsonSerializer.Deserialize<string[]>(python.Stdout.Trim())!;
        Assert.Equal(cases.Count, pythonResults.Length);

        var wrong = new List<string>();
        for (int i = 0; i < cases.Count; i++)
        {
            var (name, input, normalized) = cases[i];
            string csharp = CutRunner.Normalize(input);
            if (csharp != normalized) wrong.Add($"  CutRunner.Normalize, '{name}': {JsonSerializer.Serialize(csharp)}");
            if (pythonResults[i] != normalized) wrong.Add($"  run.py normalize(), '{name}': {JsonSerializer.Serialize(pythonResults[i])}");
        }
        Assert.True(wrong.Count == 0,
            $"the output normalizations differ from the shared fixture {fixture} (each line: the normalization, the "
            + $"case, what it produced):\n{string.Join("\n", wrong)}");
    }
}

/// <summary>
/// Both runners give THIS PARTITION'S share of the suite's tests the same verdict (kb/Work PB3176, PB4325): the
/// in-process runner judges each test, <c>run.py</c> runs the same tests through the <c>cobol</c> command, and every
/// test whose two verdicts differ, or that only one side ran, is named with both reasons.
/// </summary>
/// <remarks>
/// <para>⛔ THIS FAMILY IS PARTITIONED (<see cref="TestPartitioning"/>; kb/Work PB4325, step 2 of PB4323). It was one
/// <c>[Fact]</c> that ran <c>run.py</c> over every edition's suite, one observation per edition and the editions in
/// series: 109 to 232 s on the local gate host, 352 s on Windows CI, the longest case of every run, which made CI's
/// "rest" shard 2.2 times (Linux) to 1.4 times (Windows) longer than its other cases need. <see cref="Partitions"/>
/// classes run in parallel, each holding its even share of the rows (<see cref="TestPartitionAudit"/> refuses a lost
/// row); a partition runs <c>run.py</c> once per edition its share names, the edition's tests given by <c>--test</c>,
/// each observation bounded by the work it does (<see cref="ObservationBudget.ForChildren"/>, kb/Work PB3108).</para>
/// <para>The row set is the wiseowl manifest's in-suite tests, <c>(edition, id)</c>. That it equals the set <c>run.py</c>
/// runs is asserted ONCE, by <see cref="ConformanceSuiteRunnerDriftTests.BothRunners_RunExactlyTheSameTests"/> (an
/// inherited <c>[Fact]</c> would run in every partition); each partition still asserts that <c>run.py</c> ran exactly
/// the tests it was asked to, so a test <c>run.py</c> cannot find reads "only in-process" instead of passing.</para>
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class BothRunnersVerdictDriftTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count: the suite's in-suite tests (89 in 2026-10) in eighths, about 11 a partition, so the
    /// longest observation is a few <c>run.py</c> rounds instead of the whole suite's. The stride gives each partition
    /// an eighth of the rows whatever the suite's size, so the suite grows with no edit here (kb/Work PB4146).</summary>
    public const int Partitions = 8;

    [PartitionedRowSource(nameof(Tests))]
    public static IEnumerable<object[]> AllTests() =>
        RunPyHarness.InSuiteTests().Select(t => new object[] { t.Edition, t.Id });

    /// <summary>This partition's share of the suite's in-suite tests.</summary>
    public static IEnumerable<object[]> Tests() => TestPartitioning.SliceRows<TSlot>(AllTests(), Partitions);

    [Fact]
    public void BothRunners_GiveThisPartitionsSuiteTestsTheSameVerdict()
    {
        string cli = TestRepo.SrcBin("WiseOwl.Cobol.Cli", OperatingSystem.IsWindows() ? "cobol.exe" : "cobol");
        Assert.True(File.Exists(cli),
            $"{cli} does not exist: the Conformance project builds the cobol command (its ProjectReference to "
            + "WiseOwl.Cobol.Cli) so that run.py can drive it");
        var share = Tests().Select(r => (Edition: (string)r[0], Id: (string)r[1])).ToList();
        Assert.NotEmpty(share);

        string scratch = ScratchDirectory.Create("suitedrift");
        try
        {
            var differentTests = new List<string>();
            var differing = new List<string>();
            foreach (var byEdition in share.GroupBy(t => t.Edition, StringComparer.Ordinal))
            {
                string edition = byEdition.Key;
                string[] ids = [.. byEdition.Select(t => t.Id)];
                Dictionary<string, SuiteResult> theirs = RunPyHarness.Run(cli, SuiteManifest.Generated, edition, ids,
                    Path.Combine(scratch, "results", edition));
                var ours = ids.ToDictionary(id => id,
                    id => ConformanceSuiteRunner.Judge(edition, SuiteManifest.Manifest.Editions[edition][id]),
                    StringComparer.Ordinal);

                var onlyOurs = ours.Keys.Except(theirs.Keys).Order(StringComparer.Ordinal).ToList();
                var onlyTheirs = theirs.Keys.Except(ours.Keys).Order(StringComparer.Ordinal).ToList();
                if (onlyOurs.Count > 0 || onlyTheirs.Count > 0)
                    differentTests.Add($"  {edition}: only in-process: {string.Join(", ", onlyOurs)}. "
                        + $"Only run.py: {string.Join(", ", onlyTheirs)}.");
                differing.AddRange(ours.Where(o => theirs.ContainsKey(o.Key) && o.Value.Verdict != theirs[o.Key].Verdict)
                    .OrderBy(o => o.Key, StringComparer.Ordinal)
                    .Select(o => $"  {edition}/{o.Key}: in-process {o.Value.Verdict} ({o.Value.Reason}); "
                        + $"run.py {theirs[o.Key].Verdict} ({theirs[o.Key].Reason})"));
            }
            Assert.True(differentTests.Count == 0,
                "the two runners ran different tests, though this partition asked run.py for the in-process side's:\n"
                + string.Join("\n", differentTests));
            Assert.True(differing.Count == 0,
                $"{differing.Count} test(s) of the suite have different verdicts in the two runners:\n{string.Join("\n", differing)}");
        }
        finally { ScratchDirectory.Remove(scratch); }
    }
}

/// <summary>What the partitioned verdict comparison and the whole-population test share: the suite's in-suite tests,
/// the generator's per-edition suites, and one <c>run.py</c> observation.</summary>
internal static class RunPyHarness
{
    /// <summary>The processes run.py starts for one test, at most: an accepted test's compile and run, or a flagged
    /// test's control compile and its flagged compile (run.py's <c>accept</c> and <c>flagged</c>); a test NOT APPLICABLE
    /// starts none.</summary>
    private const int ProcessesPerTest = 2;

    /// <summary>run.py's parallelism, stated here so its budget and its run agree: half the logical processors, at most
    /// eight (run.py's own default).</summary>
    private static readonly int Jobs = Math.Clamp(Environment.ProcessorCount / 2, 1, 8);

    /// <summary>The wiseowl manifest's in-suite tests, <c>(edition, id)</c>, in a stable order: the population the
    /// in-process runner judges and <c>run.py</c> must run.</summary>
    internal static IEnumerable<(string Edition, string Id)> InSuiteTests() =>
        SuiteManifest.Manifest.Editions.OrderBy(e => e.Key, StringComparer.Ordinal)
            .SelectMany(e => e.Value.Values.Where(t => t.InSuite).OrderBy(t => t.Id, StringComparer.Ordinal)
                .Select(t => (Edition: e.Key, t.Id)));

    /// <summary>The generator's per-edition suites, each with the ids of its tests: the population run.py runs (a suite
    /// is <c>suites/&lt;edition&gt;/manifest.json</c>, an edition being all digits; <c>suites/wiseowl</c> is the
    /// WiseOwl manifest, not a suite).</summary>
    internal static IEnumerable<(string Edition, IReadOnlyList<string> Ids)> GeneratedSuites(string suites)
    {
        foreach (string dir in Directory.EnumerateDirectories(suites).Order(StringComparer.Ordinal))
        {
            string edition = Path.GetFileName(dir);
            string manifest = Path.Combine(dir, "manifest.json");
            if (!edition.All(char.IsAsciiDigit) || !File.Exists(manifest)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
            yield return (edition,
                [.. doc.RootElement.GetProperty("tests").EnumerateArray().Select(t => t.GetProperty("id").GetString()!)]);
        }
    }

    /// <summary>
    /// run.py over the tests <paramref name="ids"/> of ONE edition's suite, within the budget of the work it does
    /// (kb/Work PB3108): one harness unit for run.py itself and one per round of its at most
    /// <see cref="ProcessesPerTest"/> processes per test. One observation per edition and partition, so a retry re-runs
    /// one partition's share of one edition and no observation covers the whole suite. Until PB3108 one run.py process
    /// ran every edition under one program's 120 s, and went RED on load alone at least eight times between 2026-10-18
    /// and 2026-10-22 (its 78 processes take 30 s on a quiet host). Returns what run.py reported, by test id.
    /// </summary>
    internal static Dictionary<string, SuiteResult> Run(string cli, string suites, string edition,
        IReadOnlyList<string> ids, string results)
    {
        int jobs = Math.Min(Jobs, ids.Count);
        var budget = ObservationBudget.ForChildren(ProcessesPerTest * ids.Count, jobs,
            $"run.py over {ids.Count} test(s) of the {edition} suite (at most {ProcessesPerTest} processes each, "
            + $"{jobs} at a time)");
        var args = new List<string>
        {
            "--compiler", ConformanceSuite.Compiler, "--set", $"cobol={cli}",
            "--suites", suites, "--editions", edition, "--jobs", jobs.ToString(CultureInfo.InvariantCulture),
            "--out", results,
        };
        foreach (string id in ids)
        {
            args.Add("--test");
            args.Add(id);
        }
        ProcessObservation run = PythonInstrument.Run(budget, ConformanceSuite.Harness, [.. args]);
        // 0 GREEN, 1 RED (a FAIL: the in-process runner must then say FAIL too); 2 is a usage or configuration error.
        Assert.True(run.ExitCode is 0 or 1,
            $"run.py could not run the {edition} suite (exit {run.ExitCode}):\n{run.Stdout}\n{run.Stderr}");
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(results, "results.json")));
        var ran = doc.RootElement.GetProperty("tests").EnumerateArray()
            .Select(t => (Edition: t.GetProperty("edition").GetString()!, Id: t.GetProperty("id").GetString()!,
                Result: new SuiteResult(t.GetProperty("verdict").GetString()!, t.GetProperty("reason").GetString()!)))
            .ToList();
        Assert.True(ran.All(t => t.Edition == edition),
            $"run.py --editions {edition} reported tests of another edition: "
            + string.Join(", ", ran.Where(t => t.Edition != edition).Select(t => $"{t.Edition}/{t.Id}")));
        return ran.ToDictionary(t => t.Id, t => t.Result, StringComparer.Ordinal);
    }
}

// ⛔ THE EIGHT PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover the in-suite tests
// exactly once, each partition its even share; a deleted partition class drops an eighth of the suite from the
// verdict comparison without it.

/// <summary>Runner-drift partition 0 of <see cref="BothRunnersVerdictDriftTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class BothRunnersVerdictDriftTests_P0 : BothRunnersVerdictDriftTestsBase<Slot0>;

/// <summary>Runner-drift partition 1.</summary>
public sealed class BothRunnersVerdictDriftTests_P1 : BothRunnersVerdictDriftTestsBase<Slot1>;

/// <summary>Runner-drift partition 2.</summary>
public sealed class BothRunnersVerdictDriftTests_P2 : BothRunnersVerdictDriftTestsBase<Slot2>;

/// <summary>Runner-drift partition 3.</summary>
public sealed class BothRunnersVerdictDriftTests_P3 : BothRunnersVerdictDriftTestsBase<Slot3>;

/// <summary>Runner-drift partition 4.</summary>
public sealed class BothRunnersVerdictDriftTests_P4 : BothRunnersVerdictDriftTestsBase<Slot4>;

/// <summary>Runner-drift partition 5.</summary>
public sealed class BothRunnersVerdictDriftTests_P5 : BothRunnersVerdictDriftTestsBase<Slot5>;

/// <summary>Runner-drift partition 6.</summary>
public sealed class BothRunnersVerdictDriftTests_P6 : BothRunnersVerdictDriftTestsBase<Slot6>;

/// <summary>Runner-drift partition 7.</summary>
public sealed class BothRunnersVerdictDriftTests_P7 : BothRunnersVerdictDriftTestsBase<Slot7>;
