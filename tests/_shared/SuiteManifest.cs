// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>One test of the conformance suite at one edition, as the generator's manifest states it
/// (<c>conformance-suite/harness/generate.py#entry_for</c>; DESIGN-conformance-suite.md §4, §5).</summary>
/// <param name="Id">The test's id: its file's base name.</param>
/// <param name="Clause">The clause folder it is filed under.</param>
/// <param name="Source">Its program, relative to the suite root.</param>
/// <param name="SourceFormat"><c>fixed</c> or <c>free</c>.</param>
/// <param name="Files">The files it declares (copybooks, data files), relative to the suite root.</param>
/// <param name="Requires">The Annex A.4 modules and A.3 elements it needs.</param>
/// <param name="Words">Its words that are none of the standard's at this edition (what a profile's extra reserved
/// words are checked against, ISO §2023:4.2.10).</param>
/// <param name="Expect"><c>accept</c> or <c>flag</c>.</param>
/// <param name="ImplementorDefined">An accepted test's Annex A.1 items whose value its output depends on.</param>
/// <param name="ExpectedOutput">An accepted test's expected output per A.1 value key (<c>""</c> when it has none).</param>
/// <param name="Line">A flagged test's marked line.</param>
/// <param name="Control">A flagged test's control: the accepted twin with the violation removed.</param>
/// <param name="InSuite">Whether the per-edition suites carry it (the rest are WiseOwl's own tests).</param>
/// <param name="Options">A WiseOwl test's WiseOwl compile options (<c>conformance-suite/wiseowl/options/</c>), by
/// option: <c>sign-encoding</c>, <c>numeric-display</c>, <c>flag-extensions</c>, <c>repository-update</c>; empty for every other test.</param>
/// <param name="Build">A multi-unit WiseOwl test's compilation steps in order (the test's own source among them, where its
/// header's <c>build</c> puts it); null for a test that is one compilation (DESIGN-external-repository §17.1).</param>
/// <param name="Servers">A WiseOwl test's message servers in the header's order: the run units the runner starts, each serving its name, before
/// the test runs (DESIGN-message-control.md §0 Q9); null for a test that starts none.</param>
internal sealed record SuiteTest(
    string Id, string Clause, string Source, string SourceFormat, IReadOnlyList<string> Files,
    IReadOnlyList<string> Requires, IReadOnlyList<string> Words, string Expect, IReadOnlyList<int> ImplementorDefined,
    IReadOnlyDictionary<string, string> ExpectedOutput, int Line, string? Control, bool InSuite,
    IReadOnlyDictionary<string, string> Options, IReadOnlyList<SuiteBuildStep>? Build, IReadOnlyList<SuiteServer>? Servers);

/// <summary>One compilation step of a multi-unit test (<c>conformance-suite/harness/generate.py#entry_for</c>, the manifest's
/// <c>build</c>).</summary>
/// <param name="File">The declared unit this step compiles, relative to the suite root; null for the step that compiles the
/// test's own source.</param>
/// <param name="Output">The output's base name (the assembly's file name without <c>.dll</c>); null for the test's own
/// source, whose output is the test's id.</param>
/// <param name="Options">The step's WiseOwl compile options (<c>wiseowl/options/</c>, keyed <c>&lt;id&gt;/&lt;file&gt;</c>).</param>
internal sealed record SuiteBuildStep(string? File, string? Output, IReadOnlyDictionary<string, string> Options);

/// <summary>One message server of a test (<c>conformance-suite/harness/generate.py#entry_for</c>, the manifest's <c>servers</c>): a declared unit whose main
/// program the runner starts as a concurrent run unit serving <paramref name="Name"/>.</summary>
/// <param name="Name">The server name as the header writes it (compared as a server name is: trailing spaces removed, without regard to case).</param>
/// <param name="File">The declared unit, relative to the suite root.</param>
/// <param name="Output">The assembly's base name, the file's stem.</param>
/// <param name="Options">The unit's WiseOwl compile options (<c>wiseowl/options/</c>, keyed <c>&lt;id&gt;/&lt;file&gt;</c>).</param>
internal sealed record SuiteServer(string Name, string File, string Output, IReadOnlyDictionary<string, string> Options);

/// <summary>One of WiseOwl COBOL's refusals (<c>conformance-suite/wiseowl/diagnostics/</c>, carried into the
/// wiseowl manifest): the editions at which WiseOwl refuses the program, and the code or message substring it
/// reports.</summary>
internal sealed record SuiteRefusal(
    string Id, string Source, string SourceFormat, IReadOnlyList<string> Files, IReadOnlyList<string> RefuseAt,
    string Expect, IReadOnlyDictionary<string, string> Options);

/// <summary>
/// ⛔ THE CONFORMANCE SUITE'S <c>wiseowl</c> MANIFEST, AS EVERY TEST PROJECT READS IT (DESIGN-conformance-suite.md §4,
/// §5; kb/Work PB3176, PB3177): every test of both trees at each edition it names, and WiseOwl's refusals. The generator
/// is the ONE reader of the test headers (§10), so no test reads a header (which edition a test is accepted at, which
/// refusals a program carries) any other way.
/// <para><b>Whose generation.</b> Under the gate, the suite is the build graph's GENERATED TARGET
/// <c>conformance-suite</c> (scripts/build_targets.json; kb/Work PB5231): the gate generates it once, keys the
/// Conformance target on the DIGEST of that output, and hands the output directory to the host in
/// <see cref="HandedVariable"/>; the host reads exactly that directory, so the key a pass is recorded under is the digest
/// of the suite the host ran. With no such variable (a hand run, CI) the host generates its own, once per test process,
/// by <c>conformance-suite/harness/generate.py --out</c> into a directory of the process's own. A refused tree throws,
/// with the generator's refusals; a handed directory that holds no manifest throws; a missing interpreter is a loud
/// failure, never a skip (<see cref="PythonInstrument"/>).</para>
/// </summary>
internal static class SuiteManifest
{
    /// <summary><c>conformance-suite/</c>: the suite root every manifest path is relative to.</summary>
    internal static string Root { get; } = TestRepo.Suite();

    /// <summary>The generator, the one reader of the test headers: the program of the <c>conformance-suite</c> target.</summary>
    internal static string Generator { get; } = TestRepo.Suite("harness", "generate.py");

    /// <summary>The variable the gate hands the <c>conformance-suite</c> target's output directory in
    /// (scripts/build_targets.json <c>env</c>; the BUILD TARGETS audit requires the dependent assembly to name it).</summary>
    internal const string HandedVariable = "WISEOWL_COBOL_GATE_SUITE";

    /// <summary>What this host records in the harness observation log (<see cref="HarnessLog"/>) when no suite was handed
    /// and it generated its own (scripts/buildgraph.py <c>GeneratedTarget.own_mark</c>): a gate whose Conformance host
    /// log holds it is RED and records no pass, so INSIDE the gate the host always runs the suite its key names
    /// (kb/Work PB5231 R1-2). A hand run or CI, which records no pass, generates its own and says so.</summary>
    internal const string OwnGenerationMark = "GENERATED-OWN  conformance-suite";

    private static readonly Lazy<string> s_generated =
        new(GenerateOnce, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<WiseOwlManifest> s_manifest =
        new(() => WiseOwlManifest.Parse(File.ReadAllText(Path.Combine(Generated, "wiseowl", "manifest.json"))),
            LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The generator's output for this test process: <c>wiseowl/manifest.json</c> and the per-edition suites
    /// (<c>&lt;edition&gt;/manifest.json</c>) that <c>run.py</c> runs. The gate's, when it handed one
    /// (<see cref="HandedVariable"/>, never written here); otherwise generated once and kept until the test host ends,
    /// which removes it (<see cref="ScratchDirectory"/>). Either way every test that reads the manifest or runs
    /// <c>run.py</c> over the suites, whatever its partition, shares the one generation (about 13 s) instead of making
    /// its own (kb/Work PB4325).</summary>
    internal static string Generated => s_generated.Value;

    /// <summary>The wiseowl manifest: every test of both trees at each edition it names, and the refusals.</summary>
    internal static WiseOwlManifest Manifest => s_manifest.Value;

    /// <summary>The gate's generation when it handed one, else the generator run once into a directory of this host's
    /// own; throws, with the generator's own output, when it refuses the tree, and when a handed directory holds no
    /// wiseowl manifest.</summary>
    private static string GenerateOnce()
    {
        string? handed = Environment.GetEnvironmentVariable(HandedVariable);
        if (!string.IsNullOrEmpty(handed))
        {
            if (!File.Exists(Path.Combine(handed, "wiseowl", "manifest.json")))
                throw new InvalidOperationException(
                    $"{HandedVariable} names {handed}, which holds no wiseowl/manifest.json: the gate handed this host a "
                    + "conformance suite that is not there, so there is nothing to run");
            return handed;
        }
        string dir = ScratchDirectory.Create("WiseOwlCobol_Suite_");
        HarnessLog.Record($"{OwnGenerationMark} :: {dir} :: no {HandedVariable} was handed, so this host generates its "
            + "own suite: a run no gate keys");
        ProcessObservation r = PythonInstrument.Run(ObservationBudget.OneProcess, Generator, "--out", dir);
        if (r.ExitCode != 0)
            throw new InvalidOperationException(
                $"conformance-suite/harness/generate.py refused the suite (exit {r.ExitCode}), so no manifest exists "
                + $"to run:\n{r.Stdout}\n{r.Stderr}");
        return dir;
    }

    /// <summary>The theory rows of the suite's tests: <c>(edition, id)</c> for every test at every edition the
    /// wiseowl manifest names it. No sentinel row: a manifest that holds no test is a red, never a pass over
    /// nothing.</summary>
    internal static IEnumerable<object[]> Tests() =>
        Manifest.Editions.SelectMany(e => e.Value.Keys.Order(StringComparer.Ordinal).Select(id => new object[] { e.Key, id }));

    /// <summary>The theory rows of the refusals: <c>(id, edition)</c> for every edition each refusal names.</summary>
    internal static IEnumerable<object[]> Refusals() =>
        Manifest.Refusals.Values.OrderBy(r => r.Id, StringComparer.Ordinal)
            .SelectMany(r => r.RefuseAt.Select(e => new object[] { r.Id, e }));

}

/// <summary>The generator's <c>suites/wiseowl/manifest.json</c>: edition → id → test, and id → refusal.</summary>
internal sealed record WiseOwlManifest(
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, SuiteTest>> Editions,
    IReadOnlyDictionary<string, SuiteRefusal> Refusals)
{
    /// <summary>Read the manifest's JSON. A field the generator always writes is read with
    /// <see cref="JsonElement.GetProperty(string)"/>, so a manifest of another shape throws rather than run a test
    /// with a default in its place.</summary>
    internal static WiseOwlManifest Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var editions = new Dictionary<string, IReadOnlyDictionary<string, SuiteTest>>(StringComparer.Ordinal);
        foreach (JsonProperty edition in doc.RootElement.GetProperty("editions").EnumerateObject())
        {
            var tests = new Dictionary<string, SuiteTest>(StringComparer.Ordinal);
            foreach (JsonElement t in edition.Value.EnumerateArray())
            {
                var test = Test(t);
                if (!tests.TryAdd(test.Id, test))
                    throw new InvalidOperationException($"the wiseowl manifest names {test.Id} twice at {edition.Name}");
            }
            editions[edition.Name] = tests;
        }

        var refusals = new Dictionary<string, SuiteRefusal>(StringComparer.Ordinal);
        foreach (JsonProperty r in doc.RootElement.GetProperty("diagnostics").EnumerateObject())
        {
            refusals[r.Name] = new SuiteRefusal(r.Name, r.Value.GetProperty("source").GetString()!,
                r.Value.GetProperty("source-format").GetString()!, Strings(r.Value.GetProperty("files")),
                Strings(r.Value.GetProperty("refuse-at")), r.Value.GetProperty("expect").GetString()!,
                Map(r.Value.GetProperty("options")));
        }
        return new WiseOwlManifest(editions, refusals);
    }

    private static SuiteTest Test(JsonElement t)
    {
        string expect = t.GetProperty("expect").GetString()!;
        bool accepted = expect switch
        {
            "accept" => true,
            "flag" => false,
            _ => throw new InvalidOperationException($"the wiseowl manifest's expect '{expect}' is neither accept nor flag"),
        };
        return new SuiteTest(
            t.GetProperty("id").GetString()!, t.GetProperty("clause").GetString()!, t.GetProperty("source").GetString()!,
            t.GetProperty("source-format").GetString()!, Strings(t.GetProperty("files")), Strings(t.GetProperty("requires")),
            Strings(t.GetProperty("words")), expect,
            accepted ? [.. t.GetProperty("implementor-defined").EnumerateArray().Select(i => i.GetInt32())] : [],
            accepted
                ? t.GetProperty("expected-output").EnumerateObject().ToDictionary(o => o.Name, o => o.Value.GetString()!, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal),
            accepted ? 0 : t.GetProperty("line").GetInt32(),
            accepted ? null : t.GetProperty("control").GetString()!,
            t.GetProperty("in-suite").GetBoolean(), Map(t.GetProperty("options")),
            t.TryGetProperty("build", out JsonElement build) ? [.. build.EnumerateArray().Select(Step)] : null,
            t.TryGetProperty("servers", out JsonElement servers) ? [.. servers.EnumerateArray().Select(Server)] : null);
    }

    private static SuiteServer Server(JsonElement server) => new(
        server.GetProperty("name").GetString()!, server.GetProperty("file").GetString()!, server.GetProperty("output").GetString()!,
        Map(server.GetProperty("options")));

    private static SuiteBuildStep Step(JsonElement step) => new(
        step.GetProperty("file").GetString(), step.GetProperty("output").GetString(), Map(step.GetProperty("options")));

    private static IReadOnlyDictionary<string, string> Map(JsonElement map) =>
        map.EnumerateObject().ToDictionary(o => o.Name, o => o.Value.GetString()!, StringComparer.Ordinal);

    private static IReadOnlyList<string> Strings(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetString()!)];
}
