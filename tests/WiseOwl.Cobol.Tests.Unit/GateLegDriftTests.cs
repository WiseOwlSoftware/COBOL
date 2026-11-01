// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// THE ARMS OF THIS CLASS are numbered as docs/rearchitecture/DESIGN-test-build-ci.md §3.14.4 numbers them, and each
// is ONE self-contained test method whose name starts with its arm:
//   (2) the orderer runs the named classes first, then longest first, and its output is a permutation of its input
//   (3) gate.py --self-test and test_population.py --self-test declare every arm
//   (4) every test project under tests/ that the gate runs links GateLegs.cs
//   (5) no discovered display name in a gated assembly contains the repository root
//   (6) every `dotnet test` caller outside the gate scrubs WISEOWL_COBOL_GATE_* and the other narrowing channels

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Gate;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE GATE RUNS THE WHOLE POPULATION, AND EVERY WHOLE-ASSEMBLY RUN PROVES IT (kb/Work PB1708, PB1718, PB4328;
/// DESIGN-test-build-ci.md §3.14.3–3.14.4): the one population check (<c>scripts/test_population.py</c>) fires on a
/// dropped, a duplicated and a foreign case; every <c>dotnet test</c> caller under <c>scripts/</c> or
/// <c>.github/workflows/</c> scrubs the gate's channel and the MSBuild VSTest properties from its environment; the test
/// orderer (tests/_shared/GateLegs.cs <c>GateOrderer</c>) only orders, its output checked to be a permutation; every
/// test project under tests/ links GateLegs.cs and names GateTestFramework; and no discovered case carries the
/// repository root.
/// </summary>
/// <remarks>
/// <para>
/// A run narrowed through the ENVIRONMENT is invisible to its own verdict line: <c>VSTestTestCaseFilter</c> (MSBuild
/// imports every environment variable as a property) narrowed Characterization from 33 cases to 31 in the run AND
/// in <c>--list-tests</c>, exit 0 — measured 2026-09-28, and a runsettings file named by <c>RunSettingsFilePath</c>
/// did the same. So the population is always listed SCRUBBED, and every other caller runs scrubbed too; CI's shard
/// variable is the one narrowing that is ever meant to reach a test host, and only where <c>ci-gate</c> checks the
/// shards' union.
/// </para>
/// <para>
/// The arms are numbered as in section 3.14.4: (2) the orderer, (3) the self-tests, (4) the links, (5) no repository
/// root in a name, (6) the scrubbed callers. Arm (4)'s and (5)'s per-assembly halves also run in the Conformance and
/// Characterization assemblies (their own GateLegDriftTests), because each audits the assembly it runs in.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class GateLegDriftTests
{
    /// <summary>The test projects (<c>tests/**/*.csproj</c>), COMMITTED (kb/Work PB3814): never a walk of <c>tests/</c> from
    /// the file system, which enters every running test host's <c>bin/</c> and <c>obj/</c>.</summary>
    private static readonly TreeScope TestProjects = new(Under: ["tests/"], Kinds: [".csproj"]);

    /// <summary>The test sources (<c>tests/**/*.cs</c>), COMMITTED, for the same reason.</summary>
    private static readonly TreeScope TestSources = new(Under: ["tests/"], Kinds: [".cs"]);

    private const string PopulationTool = "test_population.py";
    private const string DriverTool = "gate.py";

    /// <summary>
    /// Arm (3), the population half: <c>test_population.py --self-test</c> passes and still drives every arm by
    /// name — a self-test quietly reduced to its happy path also exits 0. ⛔ This test does not run the self-test
    /// (kb/Work PB2882): the runner (<c>scripts/self_tests.py</c>) runs it in every gate and CI job, and this holds that
    /// each arm is still DECLARED (<see cref="SelfTestArmSource"/>).
    /// </summary>
    [Fact]
    public void Arm3_ThePopulationCheck_DeclaresEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts(PopulationTool), new[]
                 {
                     "short: a dropped case is NEVER RAN",
                     "over: a case twice is RAN TWICE",
                     "shard overlap that KEEPS THE COUNT",
                     "skipped: a NotExecuted-only definition is counted skipped",
                     "definitions, not result names",
                     "a definition the listing never printed is NOT IN THE POPULATION",
                     "stopped early",
                     "two listings that disagree are an ERROR",
                     "an EMPTY population is an ERROR",
                     "`scrubbed <command>` runs the command without a planted gate variable",
                     "red_classes: the class of every definition with a Failed result",
                     "the XML range is stated once, as escapes",
                     "audit-callers flags every unscrubbed shape",
                 });
    }

    /// <summary>
    /// Arm (3), the driver half: <c>gate.py --self-test</c> passes and still drives every arm by name — the
    /// fail-fast stop and its named remainder, the population's dropped, duplicated and skipped cases, the identity
    /// mismatches, the empty leg, the no-plan fallback, the scrubbed all-or-none handshake,
    /// the refused second gate (kb/Work PB1721; DESIGN-test-build-ci.md section 3.14.4), ONE SHAPE for every gate
    /// (kb/Work PB3022: no mode, a slot, the audits ahead of it, fail-fast), the scope
    /// <c>leg1</c> of batched gating (kb/Work PB2515), the coded default when no scope is set: leg 1 only, its verdict
    /// <c>LEG 1 ONLY (batched gating, PB2515)</c>, never a plain GREEN; and the
    /// audits' fail-fast (kb/Work PB2523): a red audit stops the gate before the build, or the legs when it is found
    /// while they run (kb/Work PB4326); and the audits run outside the gate slot, beside the build and the legs, the
    /// script self-tests only for a change that touches tooling (kb/Work PB2524, PB4326); and every gate publishes its
    /// measured durations to the shared timings store a fresh worktree's plan reads (kb/Work PB2527); and a gate
    /// outlives its caller and is gated once per measured tree (kb/Work PB3536): it runs DETACHED and is followed, it
    /// survives a tree kill of its follower, `--await` reads its verdict file (PENDING, a process that DIED: NOT RUN
    /// [verdict]) and resolves a relative run name against the worktree that gated it, and a re-gate of an unchanged
    /// tree (coord.NEVER_WORK left out) answers from a recorded GREEN, never from a RED, another tree, another base or
    /// a narrower scope. ⛔ This test does
    /// not run the self-test (kb/Work PB2882): the runner runs it in every gate and CI job, and this holds that each arm
    /// is still DECLARED (<see cref="SelfTestArmSource"/>); run a second time under the harness's fixed 120 s budget it
    /// took 311 s, retried, in train 1058's lander gate.
    /// </summary>
    [Fact]
    public void Arm3_TheGateDriver_DeclaresEveryArm()
    {
        SelfTestArmSource.AssertDeclares(TestRepo.Scripts(DriverTool), new[]
                 {
                     "a whole planted population is GREEN, every assembly EXACT",
                     "every assembly runs its WHOLE population in one host, handed no narrowing",
                     "a dropped case is NEVER RAN",
                     "a case run twice is RAN TWICE",
                     "a trx definition the listing never printed is NOT IN THE POPULATION",
                     "a skipped case is counted skipped, never as ran",
                     "a host that wrote no trx is UNMEASURED",
                     "a red host makes the gate RED",
                     "a red host's whole output, its failure message included",
                     // kb/Work PB4328: the classes red at the previous run or in the bounce run first
                     "a re-gate hands every host the classes red at the previous run and in the bounce",
                     "an unreadable trx among the first classes' sources is named and KEPT",
                     "a test host's environment is scrubbed",
                     "a population that cannot be listed is NOT RUN [population]",
                     "a red audit (a tracked rendered view) that is known before the slot STOPS the gate before the build",
                     "the corpus fetch runs BEFORE the gate slot is taken, a red audit never queues for one",
                     // kb/Work PB4326: the audits run BESIDE the build and the hosts, and only tooling runs the self-tests
                     "an audit that turns RED while the gate queues for the slot ends the gate before the build",
                     "an audit that is RED only after the test hosts have started STOPS them",
                     "the script self-tests (the audits in TOOLING_ONLY, and the post-build ones) run ONLY when the change touches tooling",
                     "the audits a change that touches no tooling runs are AUDITS less TOOLING_ONLY",
                     "the verdict file records, per assembly, the seconds its host ran and the hosts' effective concurrency",
                     // kb/Work PB2878: the inert lane, a change no test reads
                     "a change in the INERT LANE (kb/Work PB2878) runs the audits and then ends GREEN",
                     "a red audit is RED in the inert lane too",
                     "shared gate settings that turn malformed while the gate queues: NOT RUN",
                     "a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run with --reuse",
                     "a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its output, and no audit runs",
                     "a defect in work beside the critical path is a named RED with its traceback",
                     "ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits beside the build and the hosts and runs the whole population",
                     "a second gate in the same worktree is REFUSED",
                     "its verdict is BUSY with its own exit code, which the one verdict reader reads as BUSY, never RED",
                     "the one verdict reader: GREEN and an older gate's LEG 1 ONLY: GREEN pass",
                     "a defect in the driver still ends in ONE verdict line",
                     // kb/Work PB3526, review R2-1 of wave 1150: every NOT RUN cause classified ONCE, its printers held to it
                     "every NOT RUN verdict a gate prints names a cause NOT_RUN_CAUSES classifies",
                     "the gate lock is per worktree",
                     "the measured tree is the working tree's content",
                     "a re-gate of an unchanged tree answers from the recorded GREEN",
                     "a chain of reuses names the run that MEASURED the tree",
                     "a re-gate of a CHANGED tree runs the gate",
                     "a RED is never reused",
                     "a GREEN a gate before PB4328 wrote (record schema below SCHEMA_WHOLE",
                     "a gate whose tree changed WHILE it ran is never reused",
                     "--await reads the verdict file",
                     // review R1-1 and R1-2 of wave 1152: a handed run's name is its worktree's; the harness's
                     // edit of a tracked settings file is no part of a measured tree
                     "the measured tree leaves out coord.NEVER_WORK",
                     "a relative run name resolves against the worktree that gated it",
                     "a record with no exit code takes it from its verdict through the one verdict reader",
                     "build-local's gate runs DETACHED and is followed",
                     "a detached gate OUTLIVES its follower's tree kill",
                 });
    }

    /// <summary>
    /// ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; DESIGN-test-build-ci.md §3.17.3): the gate takes no
    /// mode, so no brief, skill, script, doc or test may tell anyone to run one — a caller left naming the deleted
    /// lander mode would fail at the parameter binder, and one naming the implementer mode would teach a distinction
    /// that no longer exists. History (the DEVLOG, the kb/Work notes, frozen evidence) keeps its words.
    /// </summary>
    [Fact]
    public void TheGate_HasOneShape_NoCallerNamesAMode()
    {
        var mode = new Regex(@"(?:-Mode|--mode)\s+(?:" + "implementer" + "|" + "lander" + @")\b", RegexOptions.NonBacktracking);
        string[] extensions = [".md", ".py", ".ps1", ".sh", ".js", ".cs", ".yml", ".json"];
        string[] skipped = ["bin", "obj", ".git", "TestResults", "claude-skills", "evidence", "Work", "worktrees"];
        IEnumerable<string> Walk(string dir) => Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir).Where(f => extensions.Contains(Path.GetExtension(f)))
                .Concat(Directory.EnumerateDirectories(dir)
                    .Where(d => !skipped.Contains(Path.GetFileName(d))).SelectMany(Walk))
            : [];
        // Each tree is spelled by its own call, so the drift-rule index names this test for a change under any of them.
        var files = new[] { TestRepo.At(".claude"), TestRepo.At(".github"), TestRepo.Scripts(), TestRepo.Docs(), TestRepo.Tests() }
            .SelectMany(Walk)
            .Concat(Directory.EnumerateFiles(TestRepo.Root, "*.md")
                .Where(f => Path.GetFileName(f) != "STATUS.md"))  // a worktree's gitignored checkpoint (the history, DEVLOG/, is not walked)
            .ToList();
        Assert.Contains(files, f => f.EndsWith(Path.Combine("templates", "MANDATORY-PRACTICES.md"), StringComparison.Ordinal));
        var offenders = files
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => mode.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(TestRepo.Root, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "the gate has ONE shape — `pwsh scripts/build-local.ps1 [-Priority BelowNormal]`, no mode (kb/Work PB3022) — "
            + "but these lines still name one:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// Arm (6): every <c>dotnet test</c> caller under <c>scripts/</c> runs scrubbed, and no CI workflow sets a
    /// variable that narrows a test run.
    /// </summary>
    [Fact]
    public void Arm6_EveryDotnetTestCaller_ScrubsTheGateEnvironment()
    {
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts(PopulationTool), "audit-callers");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== CALLER AUDIT: CLEAN", StringComparison.Ordinal),
            "A `dotnet test` caller can run with the gate's channel (WISEOWL_COBOL_GATE_*) or a VSTest*/"
            + "RunSettingsFilePath variable in its environment, which narrows the run to a PART of its assembly while "
            + "it exits 0 (kb/Work PB1718). Run it as `python scripts/test_population.py scrubbed dotnet test …` "
            + "(a python caller passes `env=scrubbed_env()`), and never set one of those variables in a workflow.\n"
            + $"(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");

        // ⛔ THE SCAN MUST ASSERT ITS OWN POPULATION: the callers §3.14.3 names, plus the gate twins. A scan that
        // stopped seeing them would report a clean tree over nothing.
        foreach (string site in new[]
                 {
                     "scripts/battery.sh", "scripts/gate.py", "scripts/gen-vcr.ps1",
                     "scripts/gen-diagnostics-doc.ps1", "scripts/spec/record_verdicts.py",
                     "scripts/abi_pin.py", ".github/workflows/build-and-test.yml",
                 })
        {
            Assert.True(r.Stdout.Contains("  site  " + site + "\n", StringComparison.Ordinal)
                        || r.Stdout.Contains("  site  " + site + "\r\n", StringComparison.Ordinal),
                $"the caller audit no longer sees {site} — the scan is broken, not the tree clean.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// RED AT THE PREVIOUS GATE MEANS ANY HOST'S (kb/Work PB3155, PB3184, PB4328; DESIGN-test-build-ci.md §3.14.2): every
    /// live tool that writes a trx file into a worktree is a row of <c>test_population.TRX_WRITERS</c>, which says what the gate takes
    /// from its results — its own previous run, the Linux gate's newest run, or nothing and why — and names a results
    /// directory its tool really writes. The gate runs the classes red there first (<c>Gate._first</c>).
    /// </summary>
    [Fact]
    public void EveryTrxWriter_IsASourceOfTheFirstClasses_OrSaysWhyNot()
    {
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts(PopulationTool),
            "audit-trx-writers");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== TRX WRITER AUDIT: CLEAN", StringComparison.Ordinal),
            "A live tool writes test results (a trx file) that the gate does not classify: give it a row in "
            + "scripts/test_population.py TRX_WRITERS (its results directory, and whether its red classes run first at the next "
            + "gate), or correct the row that no longer matches its tool.\n"
            + $"(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");

        // ⛔ THE AUDIT MUST SEE ITS POPULATION: the two gates whose reds the gate reads, and the battery. An audit that
        // stopped seeing them would report a clean tree over nothing.
        foreach (string writer in new[]
                 {
                     "scripts/gate.py  TestResults/build-local", "scripts/linux-gate.sh  TestResults/linux-gate",
                     "scripts/battery.sh  -",
                 })
        {
            Assert.True(r.Stdout.Contains("  writer  " + writer + "  ", StringComparison.Ordinal),
                $"the trx writer audit no longer sees {writer} — the audit is broken, not the tree clean.\n{r.Stdout}");
        }
    }

    /// <summary>
    /// ONE READER OF A TRX FILE (kb/Work PB3661; DESIGN-test-build-ci.md §3.14.2): <c>test_population.trx_root</c>, which
    /// names the file it cannot read. The gate's former planner and the profiler each parsed a trx themselves, so a torn
    /// file ended a gate's plan with a parse error that named no path (run 000005 of wf_1973801d-4fe-29, 2026-10-24).
    /// </summary>
    [Fact]
    public void EveryTrxReader_IsTheOneReader_ThatNamesTheFileItCannotRead()
    {
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts(PopulationTool),
            "audit-trx-readers");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("=== TRX READER AUDIT: CLEAN", StringComparison.Ordinal),
            "A live tool parses a trx file itself: read it through scripts/test_population.py trx_root (it raises "
            + "UnreadableTrx naming the file), so a torn file never fails a gate's plan with an unnamed parse error.\n"
            + $"(exit {r.ExitCode})\n{r.Stdout}{r.Stderr}");

        // ⛔ THE AUDIT MUST SEE ITS POPULATION: the one reader parses XML, and the tools that name the schema (the gate's
        // fixtures, the profiler's namespace) parse nothing.
        foreach (string line in new[]
                 {
                     "  trx-schema  scripts/test_population.py  parses XML",
                     "  trx-schema  scripts/gate.py  no XML parser",
                     "  trx-schema  scripts/profile-test-parallelism.py  no XML parser",
                 })
        {
            Assert.True(r.Stdout.Contains(line, StringComparison.Ordinal),
                $"the trx reader audit no longer reports `{line.Trim()}` — the audit is broken, not the tree clean.\n{r.Stdout}");
        }
    }

    // ── Arm (2): the orderers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A case with a planted display name; only the orderers see it, and nothing serializes it.</summary>
    private sealed class PlantedCase : LongLivedMarshalByRefObject, ITestCase
    {
        /// <summary>xunit's serialization contract (xUnit3001); a planted case is never deserialized.</summary>
        public PlantedCase() => throw new NotSupportedException("a planted case is never deserialized");

        public PlantedCase(string displayName, ITestMethod testMethod)
        {
            DisplayName = displayName;
            TestMethod = testMethod;
        }

        public string DisplayName { get; }
        public string? SkipReason => null;
        public ISourceInformation? SourceInformation { get; set; }
        public ITestMethod TestMethod { get; }
        public object[]? TestMethodArguments => null;
        public Dictionary<string, List<string>> Traits { get; } = new();
        public string UniqueID { get; } = Guid.NewGuid().ToString("N");
        public void Deserialize(IXunitSerializationInfo info) => throw new NotSupportedException();
        public void Serialize(IXunitSerializationInfo info) => throw new NotSupportedException();
    }

    internal void PlantedMethod() { }

    private static ITestMethod MethodIn(ITestCollection collection) =>
        new TestMethod(new TestClass(collection, Reflector.Wrap(typeof(GateLegDriftTests))),
            Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod),
                BindingFlags.Instance | BindingFlags.NonPublic)!));

    /// <summary>THE ORDER (kb/Work PB4328): the collections whose classes the gate names first, then longest first by the
    /// weights (a collection weighs its classes' sum, an unnamed class the default), ties in xunit's order; the first
    /// classes come from the file <see cref="GateOrderer.FirstVariable"/> names, and none when it is unset.</summary>
    [Fact]
    public void Arm2_TheOrderer_RunsTheNamedClassesFirst_ThenLongestFirst_AndReturnsAPermutation()
    {
        var assembly = new TestAssembly(Reflector.Wrap(typeof(GateLegDriftTests).Assembly));
        var light = new TestCollection(assembly, null, "light");
        var heavy = new TestCollection(assembly, null, "heavy");
        var named = new TestCollection(assembly, null, "named");
        var unknown = new TestCollection(assembly, null, "unknown");
        ITestCase Case(ITestCollection collection, Type type) => new PlantedCase(type.Name,
            new TestMethod(new TestClass(collection, Reflector.Wrap(type)),
                Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod), BindingFlags.Instance | BindingFlags.NonPublic)!)));
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            "{\"schema\": 1, \"default_seconds\": 5, \"assemblies\": {\"Unit\": {\""
            + typeof(string).FullName + "\": 10, \"" + typeof(int).FullName + "\": 90, \"" + typeof(long).FullName + "\": 1}}}"));
        var cases = new List<ITestCase>
        {
            Case(light, typeof(string)), Case(heavy, typeof(int)), Case(named, typeof(long)), Case(unknown, typeof(Guid)),
        };
        string file = Path.Combine(ScratchDirectory.Create("gate-first-"),"first-classes.txt");
        File.WriteAllText(file, typeof(long).FullName + "\n\n");
        try
        {
            GateOrderer orderer = GateOrderer.For("Unit", weights, n => n == GateOrderer.FirstVariable ? file : null);
            Assert.Equal(new[] { typeof(long).FullName }, orderer.First);
            var collections = new List<ITestCollection> { light, unknown, heavy, named };
            Assert.Equal(new ITestCollection[] { named, heavy, light, unknown },
                orderer.Collections(cases).OrderTestCollections(collections).ToList());
            GateOrderer unnamed = GateOrderer.For("Unit", weights, _ => null);
            Assert.Empty(unnamed.First);
            Assert.Equal(new ITestCollection[] { heavy, light, unknown, named },
                unnamed.Collections(cases).OrderTestCollections(collections).ToList());
            Assert.ThrowsAny<IOException>(() => GateOrderer.For("Unit", weights, _ => file + ".absent"));
        }
        finally
        {
            ScratchDirectory.Remove(Path.GetDirectoryName(file)!);
        }
    }

    /// <summary>xunit trusts an orderer's output: a dropped case would silently never run. The permutation check
    /// must THROW on every way an order can lose or invent a case (xunit then keeps its own complete order).</summary>
    [Fact]
    public void Arm2_APlantedNonPermutation_Throws()
    {
        object a = new(), b = new(), c = new();
        List<object> input = [a, b, c, a];
        var planted = new (string Arm, Func<IReadOnlyList<object>, IEnumerable<object>> Order)[]
        {
            ("drops one", xs => xs.Skip(1)),
            ("repeats one", xs => xs.Append(b)),
            ("swaps one for a stranger", xs => xs.Take(3).Append(new object())),
            ("collapses a repeat", xs => xs.Distinct().Append(c)),
            ("empty", _ => []),
        };
        foreach (var (arm, order) in planted)
        {
            var ex = Record.Exception(() => GateOrder.Permutation(input, order));
            Assert.True(ex is InvalidOperationException, $"the permutation check let '{arm}' through");
        }

        Assert.Equal(new[] { c, a, b, a }, GateOrder.Permutation(input, xs => [xs[2], xs[0], xs[1], xs[3]]));
    }

    // ── CI's weighted shards (kb/Work PB4329): the shard variable, the weights file and the deal ───────────────────

    [Fact]
    public void Shard_ReadsKOfN_AndRefusesEveryOtherSpelling()
    {
        GateShard? Read(string? value) => GateShard.Read(name => name == GateShard.Variable ? value : null);

        Assert.Null(Read(null));
        Assert.Null(Read(""));                      // an empty value is unset, as Windows cannot hold one
        Assert.Equal(new GateShard(3, 7), Read("3/7"));
        Assert.Equal(new GateShard(1, 1), Read("1/1"));
        Assert.Equal("5/5", Read("5/5")!.ToString());
        foreach (string bad in new[] { "0/7", "8/7", "3/0", "3", "3-7", " 3/7", "3/7 ", "03/7", "-1/7", "3/7/9", "k/N", "1000/1000" })
        {
            var ex = Assert.Throws<GateShardException>(() => Read(bad));
            Assert.Contains($"'{bad}'", ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>The deal of the design (kb/Work PB4329): every class the file names, of every assembly, heaviest first to
    /// the lightest shard so far (ties to the lowest), then this assembly's unnamed classes at the default weight in
    /// ordinal order. Worked by hand: A 50 to shard 1, X 40 (another assembly) to 2, B 30 to 3, C 20 to 3 (load 30), D 10
    /// to 2 (load 40), then New1 to 1 (50, 50, 50: the lowest) and New2 to 2 (55, 50, 50).</summary>
    [Fact]
    public void Shard_Deal_IsLongestFirstOverEveryAssembly_ThenTheUnnamedAtTheDefault()
    {
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            """{"schema": 1, "default_seconds": 5, "assemblies": {"Unit": {"A": 50, "B": 30, "C": 20, "D": 10}, "Conformance": {"X": 40}}}"""));
        Dictionary<string, int> dealt = weights.Deal(3, "Unit", ["D", "New2", "A", "C", "New1", "B", "A"]);

        Assert.Equal(new Dictionary<string, int> { ["A"] = 1, ["B"] = 3, ["C"] = 3, ["D"] = 2, ["New1"] = 1, ["New2"] = 2 },
            dealt.Where(e => e.Key != "X").ToDictionary());
        Assert.False(dealt.ContainsKey("X"), "another assembly's class is dealt, but never named in this one's map");
        Assert.Equal(dealt, weights.Deal(3, "Unit", ["New1", "B", "New2", "C", "D", "A"]));   // the order of discovery is irrelevant
        Assert.Equal(1, weights.Deal(1, "Unit", ["A", "New1"]).Values.Distinct().Single());
        Assert.Throws<ArgumentOutOfRangeException>(() => weights.Deal(0, "Unit", ["A"]));
    }

    /// <summary>Every case runs in exactly one shard: the union of every shard's cases is the input, each once, in its
    /// discovery order, whatever the weights know (a class the file never heard of included).</summary>
    [Fact]
    public void Shard_CasesOf_PartitionsTheCasesByClass_EveryCaseInExactlyOneShard()
    {
        var assembly = new TestAssembly(Reflector.Wrap(typeof(GateLegDriftTests).Assembly));
        var collection = new TestCollection(assembly, null, "planted");
        ITestCase Case(Type type, string name) => new PlantedCase(name,
            new TestMethod(new TestClass(collection, Reflector.Wrap(type)),
                Reflector.Wrap(typeof(GateLegDriftTests).GetMethod(nameof(PlantedMethod), BindingFlags.Instance | BindingFlags.NonPublic)!)));
        var cases = new List<ITestCase>
        {
            Case(typeof(string), "S.One"), Case(typeof(int), "I.One"), Case(typeof(string), "S.Two"),
            Case(typeof(Uri), "U.One"), Case(typeof(Guid), "G.One"), Case(typeof(int), "I.Two"),
        };
        TestWeights weights = TestWeights.Parse(Encoding.UTF8.GetBytes(
            """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"System.String": 9, "System.Int32": 8, "System.Uri": 2}}}"""));

        foreach (int count in new[] { 1, 2, 3, 7 })
        {
            var shards = Enumerable.Range(1, count).Select(k => new GateShard(k, count).CasesOf(cases, "Unit", weights)).ToList();
            var all = shards.SelectMany(s => s).ToList();
            Assert.True(all.Count == cases.Count && cases.All(c => all.Count(o => ReferenceEquals(o, c)) == 1),
                $"{count} shards: not every case exactly once");
            Assert.All(shards, s => Assert.Equal(cases.Where(s.Contains), s));                          // discovery order kept
            Assert.All(shards, s => Assert.True(s.Select(GateShard.ClassOf).Distinct().All(cls =>
                s.Count(c => GateShard.ClassOf(c) == cls) == cases.Count(c => GateShard.ClassOf(c) == cls)),
                "a class is split across shards"));
        }
    }

    [Fact]
    public void Shard_TheWeightsReader_RefusesEveryMalformedFile()
    {
        foreach (var (arm, text, says) in new[]
                 {
                     ("not JSON", "not json", "not JSON"),
                     ("schema 2", """{"schema": 2, "default_seconds": 1, "assemblies": {}}""", "not schema 1"),
                     ("no default", """{"schema": 1, "assemblies": {}}""", "default_seconds"),
                     ("a zero default", """{"schema": 1, "default_seconds": 0, "assemblies": {}}""", "default_seconds"),
                     ("no assemblies", """{"schema": 1, "default_seconds": 1}""", "no assemblies"),
                     ("an assembly not an object", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": []}}""", "'Unit'"),
                     ("a weight not a number", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"A": "9"}}}""", "Unit/A"),
                     ("a negative weight", """{"schema": 1, "default_seconds": 1, "assemblies": {"Unit": {"A": -1}}}""", "negative"),
                 })
        {
            var ex = Record.Exception(() => TestWeights.Parse(Encoding.UTF8.GetBytes(text)));
            Assert.True(ex is InvalidDataException && ex.Message.Contains(says, StringComparison.Ordinal),
                $"{arm}: {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    /// <summary>The committed weights (tests/_shared/test-weights.json, embedded) deal the merge group's matrix evenly:
    /// the heaviest of 7 Windows and of 5 Linux shards carries at most 1.10 times the mean (PIPELINE-REVIEW section 4.2's
    /// "1.05 to 1.10x" over the weights). A regenerated file that deals worse is a design question, not a stale weight.</summary>
    [Fact]
    public void Shard_TheCommittedWeights_DealTheMatrixEvenly()
    {
        TestWeights weights = TestWeights.Embedded();
        Assert.True(weights.Tenths.Count > 500, $"the weights name {weights.Tenths.Count} classes: the file did not load");
        string[] assemblies = [.. weights.Tenths.Keys.Select(k => k.Assembly).Distinct().Order(StringComparer.Ordinal)];
        Assert.Equal(["Characterization", "Conformance", "Unit"], assemblies);
        foreach (int count in new[] { 5, 7 })
        {
            var load = new long[count];
            foreach (string assembly in assemblies)
            {
                var named = weights.Tenths.Where(e => e.Key.Assembly == assembly).ToList();
                Dictionary<string, int> dealt = weights.Deal(count, assembly, named.Select(e => e.Key.Class));
                foreach (var e in named)
                {
                    load[dealt[e.Key.Class] - 1] += e.Value;
                }
            }

            double spread = load.Max() / load.Average();
            Assert.True(spread <= 1.10, $"{count} shards: the heaviest carries {spread:F3}x the mean ({string.Join(", ", load)} tenths)");
        }
    }

    // ── Arm (4): every test project links GateLegs.cs and names the gate's framework ──────────────────────────────

    [Fact]
    public void Arm4_EveryTestProject_LinksGateLegs_AndNamesTheGateFramework()
    {
        string props = File.ReadAllText(TestRepo.At("tests", "Directory.Build.props"));
        Assert.Contains(@"<Compile Include=""$(MSBuildThisFileDirectory)_shared\*.cs""", props, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"<AssemblyAttribute Include=""Xunit\.TestFrameworkAttribute"">\s*"
                                 + @"<_Parameter1>WiseOwl.Cobol\.Gate\.GateTestFramework</_Parameter1>\s*"
                                 + @"<_Parameter2>\$\(AssemblyName\)</_Parameter2>"), props);
        Assert.True(File.Exists(TestRepo.At("tests", "_shared", "GateLegs.cs")), "tests/_shared/GateLegs.cs is gone");

        var testProjects = TrackedTree.Files(TestProjects).Select(rel => TestRepo.At(rel.Split('/')))
            .Where(p => File.ReadAllText(p).Contains("<IsTestProject>true</IsTestProject>", StringComparison.Ordinal))
            .ToList();
        foreach (string gated in new[] { "WiseOwl.Cobol.Tests.Conformance", "WiseOwl.Cobol.Tests.Unit",
                     "WiseOwl.Cobol.Tests.Characterization" })
        {
            Assert.True(testProjects.Any(p => Path.GetFileNameWithoutExtension(p) == gated),
                $"the gated test project {gated} is not found as a test project under tests/");
        }

        var unlinked = testProjects.Where(p => Regex.IsMatch(File.ReadAllText(p),
                @"<Compile\s+Remove=""[^""]*(_shared|GateLegs)", RegexOptions.IgnoreCase))
            .Select(p => Path.GetRelativePath(TestRepo.Root, p)).ToList();
        Assert.True(unlinked.Count == 0, "a test project drops the gate's leg filter (tests/_shared/GateLegs.cs): "
                                         + string.Join(", ", unlinked));

        var ownFramework = TrackedTree.Files(TestSources).Select(rel => TestRepo.At(rel.Split('/')))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\[\s*assembly\s*:\s*(Xunit\.)?TestFramework(Attribute)?\s*\("))
            .Select(f => Path.GetRelativePath(TestRepo.Root, f)).ToList();
        Assert.True(ownFramework.Count == 0, "a test source names its own xunit framework, so its assembly would "
                                             + "name two: " + string.Join(", ", ownFramework));

        GateLegAudit.AssertNamesTheGateFramework(typeof(GateLegDriftTests).Assembly);
    }

    // ── Arm (5): no discovered case carries the repository root ──────────────────────────────────────────────────

    [Fact]
    public void Arm5_NoDiscoveredCase_CarriesTheRepositoryRoot() =>
        GateLegAudit.AssertNoCaseCarriesTheRoot(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly's test host is configured for Server GC (kb/Work PB2526); the Conformance and
    /// Characterization assemblies' own GateLegDriftTests assert the same of theirs.</summary>
    [Fact]
    public void ThisTestHost_IsConfiguredForServerGc() =>
        GateLegAudit.AssertTestHostConfiguredForServerGc(typeof(GateLegDriftTests).Assembly);

    /// <summary>This assembly embeds the weights CI's shards are dealt by (kb/Work PB4329).</summary>
    [Fact]
    public void ThisAssembly_EmbedsTheShardWeights() =>
        GateLegAudit.AssertEmbedsTheWeights(typeof(GateLegDriftTests).Assembly);

    /// <summary>The audit's witness, on EVERY host for EVERY root shape (kb/Work PB1719): a Windows root and a POSIX
    /// root, both matched as text on either OS, and <c>host</c>, this run's own <see cref="TestRepo.Root"/> — the
    /// form that is real where the test runs. It first planted only a Windows root, so on Linux it proved nothing
    /// and went red in CI (run 36533008383) while every Windows gate was green. The host root is resolved inside
    /// the test: as an argument it would put the root in this case's own name, which arm (5) forbids.</summary>
    [Theory]
    [InlineData("windows")]
    [InlineData("posix")]
    [InlineData("host")]
    public void Arm5_TheRootAudit_FiresOnPlantedOffenders(string shape)
    {
        string root = shape switch
        {
            "windows" => @"E:\COBOL-wt\battery87",
            "posix" => "/home/runner/work/COBOL/COBOL",
            "host" => TestRepo.Root,
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "no such root shape"),
        };
        char sep = root.Contains('\\') ? '\\' : '/';
        string path = string.Join(sep, root, "src", "Parsing", "X.cs");
        static string Escaped(string s) => s.Replace("\\", @"\\", StringComparison.Ordinal); // as xunit shows it
        string escapedName = $"N.C.M(path: \"{Escaped(path)}\")";
        string slashName = $"N.C.M(path: \"{path.Replace('\\', '/')}\")";
        string truncatedName = $"N.C.T(path: \"{Escaped(root)[..3]}···\")";   // the root survives only in the argument
        const string cleanName = "N.C.Clean(rel: \"Parsing/X.cs\")";
        var planted = new (string, object[]?)[]
        {
            (escapedName, null),
            (slashName, null),
            (truncatedName, [path]),
            (cleanName, ["Parsing/X.cs"]),
        };

        var offenders = GateLegAudit.RootCarrying(planted, root);

        Assert.Equal(new[] { escapedName, slashName, truncatedName }, offenders);
        Assert.Throws<ArgumentException>(() => GateLegAudit.RootCarrying(planted, sep.ToString()));
    }
}
