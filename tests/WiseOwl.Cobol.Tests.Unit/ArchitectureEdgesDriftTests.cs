// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.Unit.Architecture;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ NO PRODUCT DEPENDENCY CROSSES A LAYER THE ARCHITECTURE EDGES FILE DOES NOT ALLOW OR TOLERATE, AND A TOLERATED ROW
/// NEVER GROWS (kb/Work PB2417; docs/rearchitecture/DESIGN-architecture-review.md §8.1 "The drift test"): a metadata walk
/// over the built product assemblies (<see cref="TypeReferenceWalk"/>) measures every (outermost authored type,
/// outermost product type) pair, and <c>docs/rearchitecture/architecture-edges.json</c> — the ONE place the layers and
/// their allowed edges are written — must place every product namespace, allow or tolerate every cross-layer pair,
/// list every tolerated pair with the kb/Work notes that remove it, and carry each tolerated row's walk count exactly.
/// </summary>
/// <remarks>
/// <para>
/// A pair the file does not list fails even when its row's count does not grow (PB2118 Draft 8, J1: a row-level count
/// let a pair added inside a tolerated row through with no remover), and a pair the walk no longer measures fails too,
/// so the file is the code's current dependency picture and only ever shrinks. The census record is the second
/// observer over the same file (<see cref="ArchCensusDriftTests.TheNewestRecord_CrossesNoLayerTheEdgesFileDoesNotAllowOrTolerate"/>).
/// </para>
/// <para>
/// Two tool modes run this same checker, never a second copy of it. SEED (<c>WISEOWL_COBOL_ARCH_EDGES_SEED=1</c>)
/// rewrites every tolerated row from the walk and the newest census record (<c>uses</c>, <c>walkUses</c>, its pairs, a
/// listed pair keeping its removers), and then asserts like the gate does, so a pair it adds with no remover is red
/// until <c>python scripts/arch/edges.py --assign</c> names one. WHAT-IF (<c>WISEOWL_COBOL_ARCH_EDGES_WHATIF=&lt;map.json&gt;</c>,
/// <see cref="WhatIf"/>) re-keys the pairs by a planned wave's move map and cut list before they aggregate and fails on
/// any row the wave would grow; a Move/rename or Extract wave's brief records that answer before dispatch.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ArchitectureEdgesDriftTests
{
    internal static readonly string EdgesFile = TestRepo.Docs("rearchitecture", "architecture-edges.json");

    internal const string SeedCommand =
        "WISEOWL_COBOL_ARCH_EDGES_SEED=1 dotnet test tests/WiseOwl.Cobol.Tests.Unit --no-build --filter "
        + "FullyQualifiedName~ArchitectureEdgesDriftTests.EveryProductDependency, then `python scripts/arch/edges.py --assign`";

    private static readonly Regex SolutionProject = new(
        "^Project\\(\"\\{[0-9A-Fa-f-]+\\}\"\\)\\s*=\\s*\"(?<name>WiseOwl\\.Cobol\\.[^\"]+)\",\\s*\"(?<path>(?<dir>src[\\\\/][^\"]+)[\\\\/][^\"\\\\/]+\\.csproj)\"",
        RegexOptions.Multiline | RegexOptions.NonBacktracking);

    private static readonly Regex AssemblyNameElement = new("<AssemblyName>(?<name>[^<]+)</AssemblyName>", RegexOptions.NonBacktracking);

    private static readonly Lazy<Measurement> Measured = new(Measure);

    [Fact]
    public void EveryProductDependency_IsAllowedOrATolerated_ListedPair()
    {
        Measurement m = Measured.Value;
        using JsonDocument record = CensusRecord.Newest();
        var censusEdges = CensusRecord.NamespaceEdges(record.RootElement);
        var edges = ArchitectureEdges.Load(EdgesFile);

        if (Environment.GetEnvironmentVariable("WISEOWL_COBOL_ARCH_EDGES_SEED") == "1")
        {
            edges = Seed(edges, m.Pairs, censusEdges, record.RootElement.GetProperty("commit").GetString()!);
            File.WriteAllText(EdgesFile, edges.ToJson());
        }

        WhatIf? whatIf = Environment.GetEnvironmentVariable("WISEOWL_COBOL_ARCH_EDGES_WHATIF") is { Length: > 0 } map
            ? WhatIf.Load(map)
            : null;
        var failures = edges.CheckWalk(m.Pairs, m.Namespaces, censusEdges, whatIf);
        Assert.True(failures.Count == 0,
            $"the architecture walk disagrees with docs/rearchitecture/architecture-edges.json ({failures.Count}"
            + (whatIf is null ? "" : ", under the what-if") + "):\n  " + string.Join("\n  ", failures.Take(200))
            + (failures.Count > 200 ? $"\n  … and {failures.Count - 200} more" : "")
            + "\nA new cross-layer dependency is a layering defect: depend on a lower layer instead (DESIGN-architecture-review §8.1). "
            + "A pair the walk no longer measures is removed work: re-seed the file (" + SeedCommand + ").");
    }

    /// <summary>The walk applies the census's population rule: every type both name agrees on generated-ness, and the
    /// walk reads every type the newest census record lists that the built assemblies still define (a type added or
    /// deleted since the record is drift the next census records, not a disagreement of rules).</summary>
    [Fact]
    public void TheWalk_AppliesTheCensusPopulationRule()
    {
        Measurement m = Measured.Value;
        using JsonDocument record = CensusRecord.Newest();
        var census = CensusRecord.GeneratedFlags(record.RootElement);
        var walked = m.Walk.Types;
        var common = census.Keys.Where(walked.ContainsKey).ToList();
        Assert.True(common.Count * 10 >= census.Count * 9,
            $"the walk reads only {common.Count} of the census's {census.Count} types: the walk and the census do not read the same population");
        var disagree = common.Where(t => census[t] != walked[t].Generated)
            .Select(t => $"{t}: census {(census[t] ? "generated" : "authored")}, walk {(walked[t].Generated ? "generated" : "authored")}")
            .ToList();
        Assert.True(disagree.Count == 0,
            $"the walk's generated rule disagrees with the census's on {disagree.Count} type(s):\n  " + string.Join("\n  ", disagree.Take(50)));
    }

    /// <summary>The checker fails on a planted cross-layer edge, a planted growth inside a tolerated row, a planted pair
    /// with no remover, an unplaced namespace, a dead allowance and a vanished pair — and passes the clean input — so a
    /// green run of the walk is evidence (brent-tools:architecture-audit Phase 3: seen failing before it lands).</summary>
    [Fact]
    public void TheChecker_FailsOnEveryPlantedViolation_AndPassesTheCleanInput()
    {
        ArchitectureEdges file = new(1, "planted", "c0ffee000000",
            [new EdgesLayer("low", ["P.Low", "P.Low.*"]), new EdgesLayer("mid", ["P.Mid"]), new EdgesLayer("high", ["P.High"]), new EdgesLayer("root", ["P"])],
            ["root"], [],
            [["mid", "low"], ["high", "mid"], ["high", "low"]],
            [new EdgesRow("P.Low", "P.Mid", 1, 1, ["PB1"], "planted", [new EdgesPair("P.Low.A", "P.Mid.B", ["PB1"])])]);
        MeasuredPair[] clean =
        [
            new("P.Mid.B", "P.Mid", "P.Low.A", "P.Low"),
            new("P.High.C", "P.High", "P.Mid.B", "P.Mid"),
            new("P.High.C", "P.High", "P.Low.Deep.D", "P.Low.Deep"),
            new("P.Low.A", "P.Low", "P.Mid.B", "P.Mid"),
        ];
        var namespaces = new HashSet<string>(["P.Low", "P.Low.Deep", "P.Mid", "P.High", "P"]);
        var noCensus = new Dictionary<(string, string), int>();

        Assert.Empty(file.CheckWalk(clean, namespaces, noCensus));

        // a planted edge: low -> high is neither allowed nor tolerated
        Assert.Contains(file.CheckWalk([.. clean, new("P.Low.A", "P.Low", "P.High.C", "P.High")], namespaces, noCensus),
            f => f.Contains("a new cross-layer dependency", StringComparison.Ordinal));
        // a planted growth: a second pair inside the tolerated row
        var grown = file.CheckWalk([.. clean, new("P.Low.Deep.D", "P.Low.Deep", "P.Mid.B", "P.Mid"), new("P.Low.E", "P.Low", "P.Mid.B", "P.Mid")],
            namespaces, noCensus);
        Assert.Contains(grown, f => f.Contains("does not list", StringComparison.Ordinal));
        Assert.Contains(grown, f => f.Contains("grew", StringComparison.Ordinal));
        // a planted pair with no remover
        var orphan = file with { Tolerated = [new EdgesRow("P.Low", "P.Mid", 1, 1, [], "planted", [new EdgesPair("P.Low.A", "P.Mid.B", [])])] };
        Assert.Contains(orphan.CheckWalk(clean, namespaces, noCensus), f => f.Contains("has no remover", StringComparison.Ordinal));
        // a product namespace no layer places
        Assert.Contains(file.CheckWalk(clean, new HashSet<string>(namespaces) { "Q" }, noCensus),
            f => f.Contains("placed in no layer", StringComparison.Ordinal));
        // a dead allowance: no pair from mid to low in either observer
        Assert.Contains(file.CheckWalk([.. clean.Where(p => p.From != "P.Mid.B")], namespaces, noCensus),
            f => f.Contains("dead allowance", StringComparison.Ordinal));
        // the census observer alone keeps an allowance alive
        Assert.Empty(file.CheckWalk([.. clean.Where(p => p.From != "P.Mid.B")], namespaces,
            new Dictionary<(string, string), int> { [("P.Mid", "P.Low")] = 1 }));
        // a vanished pair: the ratchet only falls, and the file says so
        Assert.Contains(file.CheckWalk([.. clean.Where(p => p.From != "P.Low.A")], namespaces, noCensus),
            f => f.Contains("no longer measures", StringComparison.Ordinal));
        // the what-if: moving B down to low dissolves the row's pair and grows nothing; moving A up re-keys it onto an unlisted row
        Assert.Empty(file.CheckWalk(clean, namespaces, noCensus, new WhatIf(new Dictionary<string, string> { ["P.Mid.B"] = "P.Low" }, [])));
        Assert.Contains(file.CheckWalk(clean, namespaces, noCensus, new WhatIf(new Dictionary<string, string> { ["P.High.C"] = "P.Low" }, [])),
            f => f.Contains("does not list", StringComparison.Ordinal));
        // a cut removes the pair an Extract ends
        Assert.Empty(file.CheckWalk(clean, namespaces, noCensus, new WhatIf(new Dictionary<string, string>(), [["P.Low.A", "P.Mid.B"]])));
        // the census observer: a grown row and an edge no row admits
        Assert.Contains(file.CheckCensus("c0ffee000000", new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 2 }),
            f => f.Contains("grew in the census", StringComparison.Ordinal));
        Assert.Contains(file.CheckCensus("c0ffee000000", new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 1, [("P.Mid", "P.High")] = 1 }),
            f => f.Contains("no tolerated row admits", StringComparison.Ordinal));
        Assert.Empty(file.CheckCensus("c0ffee000000", new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 1, [("P.Mid", "P.Low")] = 3 }));
    }

    /// <summary>The tolerated rows rewritten from the measurement: every cross-layer namespace edge the layers do not
    /// allow that either observer measures, with the census's count, the walk's count and the walk's pairs; a pair
    /// the file already lists keeps its removers, a new one arrives with none (<c>edges.py --assign</c> names them).</summary>
    private static ArchitectureEdges Seed(ArchitectureEdges edges, IReadOnlyCollection<MeasuredPair> pairs,
        IReadOnlyDictionary<(string From, string To), int> censusEdges, string census)
    {
        bool Tolerable(string from, string to) =>
            edges.LayerOf(from) is { } f && edges.LayerOf(to) is { } t && !edges.Allows(f, t);

        var old = edges.Tolerated.ToDictionary(r => (r.From, r.To));
        var walked = pairs.Where(p => Tolerable(p.FromNamespace, p.ToNamespace))
            .GroupBy(p => (p.FromNamespace, p.ToNamespace))
            .ToDictionary(g => g.Key, g => g.Select(p => (p.From, p.To)).Distinct().ToList());
        var keys = walked.Keys.Concat(censusEdges.Where(e => Tolerable(e.Key.From, e.Key.To)).Select(e => e.Key)).Distinct()
            .OrderBy(k => k.Item1, StringComparer.Ordinal).ThenBy(k => k.Item2, StringComparer.Ordinal);
        var rows = new List<EdgesRow>();
        foreach (var key in keys)
        {
            old.TryGetValue(key, out EdgesRow? before);
            var listed = (before?.Pairs ?? []).ToDictionary(p => (p.From, p.To), p => p.RemovedBy);
            var rowPairs = (walked.TryGetValue(key, out var w) ? w : [])
                .OrderBy(p => p.From, StringComparer.Ordinal).ThenBy(p => p.To, StringComparer.Ordinal)
                .Select(p => new EdgesPair(p.From, p.To, listed.TryGetValue(p, out var by) ? by : []))
                .ToList();
            rows.Add(new EdgesRow(key.Item1, key.Item2, censusEdges.GetValueOrDefault(key), rowPairs.Count,
                [.. rowPairs.SelectMany(p => p.RemovedBy).Distinct().Order(StringComparer.Ordinal)],
                before?.Plan ?? "", rowPairs));
        }

        return edges with { Census = census, Tolerated = rows };
    }

    private static Measurement Measure()
    {
        var edges = ArchitectureEdges.Load(EdgesFile);
        var excluded = edges.Excluded.Select(e => e.Assembly).ToHashSet(StringComparer.Ordinal);
        var projects = SolutionProject.Matches(File.ReadAllText(TestRepo.At("WiseOwl.Cobol.sln")))
            .Select(m => (Name: m.Groups["name"].Value, Path: TestRepo.At(m.Groups["path"].Value.Replace('\\', '/')),
                Directory: TestRepo.At(m.Groups["dir"].Value.Replace('\\', '/'))))
            .Where(p => !excluded.Contains(p.Name))
            .Select(p => (p.Name, p.Directory,
                Assembly: AssemblyNameElement.Match(File.ReadAllText(p.Path)) is { Success: true } a ? a.Groups["name"].Value : p.Name))
            .ToList();
        Assert.NotEmpty(projects);
        var directoryOf = projects.ToDictionary(p => p.Assembly, p => p.Directory, StringComparer.Ordinal);
        var walk = TypeReferenceWalk.Read([.. projects.Select(p => Path.Combine(AppContext.BaseDirectory, p.Assembly + ".dll"))],
            assembly => directoryOf[assembly]);
        var measured = walk.Pairs
            .Select(p => new MeasuredPair(p.From, walk.NamespaceOf(p.From), p.To, walk.NamespaceOf(p.To)))
            .ToList();
        var namespaces = walk.Types.Values.Select(t => t.Namespace).ToHashSet(StringComparer.Ordinal);
        return new Measurement(walk, measured, namespaces);
    }

    private sealed record Measurement(TypeReferenceWalk Walk, IReadOnlyCollection<MeasuredPair> Pairs, IReadOnlySet<string> Namespaces);
}
