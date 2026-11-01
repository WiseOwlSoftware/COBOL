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
/// list every tolerated pair with the open kb/Work notes that remove it, and carry each tolerated row's walk count exactly.
/// </summary>
/// <remarks>
/// <para>
/// A pair the file does not list fails even when its row's count does not grow (PB2118 Draft 8, J1: a row-level count
/// let a pair added inside a tolerated row through with no remover), and a pair the walk no longer measures fails too,
/// so the file is the code's current dependency picture and only ever shrinks. The census record is the second
/// observer over the same file (<see cref="ArchCensusDriftTests.TheNewestRecord_CrossesNoLayerTheEdgesFileDoesNotAllowOrTolerate"/>).
/// </para>
/// <para>
/// Two tool modes run this same checker, never a second copy of it. RATCHET (<c>WISEOWL_COBOL_ARCH_EDGES_RATCHET=1</c>,
/// <see cref="ArchitectureEdges.Ratchet"/>) writes the falls the measurement shows — a vanished pair deleted, a count
/// lowered, the file keyed to the newest census record — and then asserts like the gate does; it never adds a pair or
/// a row, raises a count or names a remover, so a new dependency stays red after it (the file was seeded once, by
/// PB2417; DESIGN-architecture-review §8.1 "The ratchet and the removers"). WHAT-IF
/// (<c>WISEOWL_COBOL_ARCH_EDGES_WHATIF=&lt;map.json&gt;</c>, <see cref="WhatIf"/>) re-keys the pairs by a planned wave's
/// move map and cut list before they aggregate and fails on any row the wave would grow; a Move/rename or Extract
/// wave's brief records that answer before dispatch.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ArchitectureEdgesDriftTests
{
    internal static readonly string EdgesFile = TestRepo.Docs("rearchitecture", "architecture-edges.json");

    internal const string RatchetCommand =
        "WISEOWL_COBOL_ARCH_EDGES_RATCHET=1 dotnet test tests/WiseOwl.Cobol.Tests.Unit --no-build --filter "
        + "FullyQualifiedName~ArchitectureEdgesDriftTests.EveryProductDependency";

    internal const string NewDependencyAdvice =
        "A new cross-layer dependency (a pair or row the file does not list, or a grown count) is a layering defect: depend "
        + "on a lower layer instead. The ratchet never admits one; tolerating it is a decision, made by hand in the change "
        + "that files the kb/Work note removing it and names that note (DESIGN-architecture-review §8.1). A pair or count "
        + "that fell is removed work: ratchet the file (" + RatchetCommand + ").";

    private static readonly Regex TerminalStatuses = new(@"^TERMINAL_STATUSES = \((?<list>[^)]*)\)", RegexOptions.Multiline | RegexOptions.NonBacktracking);

    private static readonly Regex Quoted = new("\"(?<s>[^\"]+)\"", RegexOptions.NonBacktracking);

    private static readonly Regex StatusLine = new(@"^status:\s*(?<s>\S+)", RegexOptions.Multiline | RegexOptions.NonBacktracking);

    private static readonly Regex RemovesLine = new(@"^\*\*Removes\b[^\n]*", RegexOptions.Multiline | RegexOptions.NonBacktracking);

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

        if (Environment.GetEnvironmentVariable("WISEOWL_COBOL_ARCH_EDGES_RATCHET") == "1")
        {
            edges = edges.Ratchet(m.Pairs, censusEdges, record.RootElement.GetProperty("commit").GetString()!);
            File.WriteAllText(EdgesFile, edges.ToJson());
        }

        WhatIf? whatIf = Environment.GetEnvironmentVariable("WISEOWL_COBOL_ARCH_EDGES_WHATIF") is { Length: > 0 } map
            ? WhatIf.Load(map)
            : null;
        List<string> failures = [.. edges.CheckWalk(m.Pairs, m.Namespaces, censusEdges, whatIf), .. edges.RemoverDefects(KbNote, RegisterTerminalStatuses())];
        Assert.True(failures.Count == 0,
            $"the architecture walk disagrees with docs/rearchitecture/architecture-edges.json ({failures.Count}"
            + (whatIf is null ? "" : ", under the what-if") + "):\n  " + string.Join("\n  ", failures.Take(200))
            + (failures.Count > 200 ? $"\n  … and {failures.Count - 200} more" : "")
            + "\n" + NewDependencyAdvice);
    }

    /// <summary>The kb/Work note a remover names, or null when kb/Work does not hold it.</summary>
    private static RemoverNote? KbNote(string id)
    {
        string path = TestRepo.At("kb", "Work", id + ".md");
        if (!File.Exists(path))
        {
            return null;
        }

        string text = File.ReadAllText(path);
        return new RemoverNote(StatusLine.Match(text) is { Success: true } s ? s.Groups["s"].Value : "",
            string.Join("\n", RemovesLine.Matches(text).Select(r => r.Value)));
    }

    /// <summary>The register's terminal statuses, read from their one home (<c>scripts/spec/work.py</c>
    /// <c>TERMINAL_STATUSES</c>), never restated here.</summary>
    private static HashSet<string> RegisterTerminalStatuses()
    {
        Match list = TerminalStatuses.Match(File.ReadAllText(TestRepo.At("scripts", "spec", "work.py")));
        Assert.True(list.Success, "scripts/spec/work.py no longer declares TERMINAL_STATUSES = (…): the edges file's remover check reads it there");
        var statuses = Quoted.Matches(list.Groups["list"].Value).Select(q => q.Groups["s"].Value).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(statuses);
        return statuses;
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

    /// <summary>The planted input of the self-tests: four layers (low, mid, high and a composition root), one tolerated
    /// row low -> mid holding the one pair A -> B that PB1 removes, and the clean measurement that file describes.</summary>
    private static readonly ArchitectureEdges Planted = new(1, "planted", "c0ffee000000",
        [new EdgesLayer("low", ["P.Low", "P.Low.*"]), new EdgesLayer("mid", ["P.Mid"]), new EdgesLayer("high", ["P.High"]), new EdgesLayer("root", ["P"])],
        ["root"], [],
        [["mid", "low"], ["high", "mid"], ["high", "low"]],
        [new EdgesRow("P.Low", "P.Mid", 1, 1, ["PB1"], "planted", [new EdgesPair("P.Low.A", "P.Mid.B", ["PB1"])])]);

    private static readonly MeasuredPair[] PlantedClean =
    [
        new("P.Mid.B", "P.Mid", "P.Low.A", "P.Low"),
        new("P.High.C", "P.High", "P.Mid.B", "P.Mid"),
        new("P.High.C", "P.High", "P.Low.Deep.D", "P.Low.Deep"),
        new("P.Low.A", "P.Low", "P.Mid.B", "P.Mid"),
    ];

    private static readonly HashSet<string> PlantedNamespaces = ["P.Low", "P.Low.Deep", "P.Mid", "P.High", "P"];

    /// <summary>The checker fails on a planted cross-layer edge, a planted growth inside a tolerated row, a planted pair
    /// with no remover, a census-only row with no remover, an unplaced namespace, a dead allowance and a vanished pair —
    /// and passes the clean input — so a green run of the walk is evidence (brent-tools:architecture-audit Phase 3: seen
    /// failing before it lands).</summary>
    [Fact]
    public void TheChecker_FailsOnEveryPlantedViolation_AndPassesTheCleanInput()
    {
        ArchitectureEdges file = Planted;
        MeasuredPair[] clean = PlantedClean;
        var namespaces = PlantedNamespaces;
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
        // a row the census alone sees (no walked pair: an inlined const) names its own removers, and fails with none
        var censusOnly = file with { Tolerated = [.. file.Tolerated, new EdgesRow("P.Mid", "P.High", 1, 0, ["PB2"], "planted", [])] };
        Assert.Empty(censusOnly.CheckWalk(clean, namespaces, noCensus));
        Assert.Contains((file with { Tolerated = [.. file.Tolerated, new EdgesRow("P.Mid", "P.High", 1, 0, [], "planted", [])] })
            .CheckWalk(clean, namespaces, noCensus), f => f.Contains("seen only by the census", StringComparison.Ordinal));
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

    /// <summary>THE RATCHET ONLY FALLS (kb/Work PB2417, branch review R1-1; §8.1 "The ratchet and the removers"): run on a
    /// measurement that adds a pair, swaps one pair for another inside a row (the reviewer's plant: the from type already
    /// has a pair in the row, so a reach rule would have named its remover), adds a row, or grows a census count, the
    /// ratchet writes none of it — the new pair is not listed, gets no remover and stays red, and the grown count stays
    /// red at the census recording — while it writes every fall: a vanished pair, a lowered count, a row both observers
    /// read zero, the census key.</summary>
    [Fact]
    public void TheRatchet_OnlyFalls_AndNeverAdmitsAPairARowOrAGrownCount()
    {
        var census = new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 1 };
        const string Next = "c0ffee000001";

        // a pair added inside the row, from a type that already has one there
        MeasuredPair[] added = [.. PlantedClean, new("P.Low.A", "P.Low", "P.Mid.New", "P.Mid")];
        var afterAdd = Planted.Ratchet(added, census, Next);
        Assert.Equal(Planted.Tolerated[0].Pairs, Assert.Single(afterAdd.Tolerated).Pairs);
        Assert.Equal(1, afterAdd.Tolerated[0].WalkUses);
        var addFailures = afterAdd.CheckWalk(added, PlantedNamespaces, census);
        Assert.Contains(addFailures, f => f.Contains("P.Low.A -> P.Mid.New is a pair the tolerated row", StringComparison.Ordinal));
        Assert.Contains(addFailures, f => f.Contains("grew", StringComparison.Ordinal));

        // the reviewer's plant: a move removes A -> B and adds A -> New in the same row, so neither count moves
        MeasuredPair[] swapped = [.. PlantedClean.Where(p => p.From != "P.Low.A"), new("P.Low.A", "P.Low", "P.Mid.New", "P.Mid")];
        var afterSwap = Planted.Ratchet(swapped, census, Next);
        EdgesRow row = Assert.Single(afterSwap.Tolerated);
        Assert.Empty(row.Pairs);                    // the vanished pair goes, the new one never arrives
        Assert.Equal(0, row.WalkUses);
        Assert.Equal<string>(["PB1"], row.RemovedBy);       // the census still reads the row: it keeps its removers, no new one
        Assert.Contains(afterSwap.CheckWalk(swapped, PlantedNamespaces, census),
            f => f.Contains("P.Low.A -> P.Mid.New is a pair the tolerated row", StringComparison.Ordinal));
        Assert.Equal(afterSwap.ToJson(), afterSwap.Ratchet(swapped, census, Next).ToJson());
        Assert.DoesNotContain(afterSwap.Tolerated.SelectMany(r => r.Pairs), p => p.RemovedBy.Count == 0);

        // a row the file does not tolerate, in either observer
        MeasuredPair[] newRow = [.. PlantedClean, new("P.Low.A", "P.Low", "P.High.C", "P.High")];
        var withNewEdge = new Dictionary<(string, string), int>(census) { [("P.Mid", "P.High")] = 1 };
        var afterRow = Planted.Ratchet(newRow, withNewEdge, Next);
        Assert.Single(afterRow.Tolerated);
        Assert.Contains(afterRow.CheckWalk(newRow, PlantedNamespaces, withNewEdge), f => f.Contains("a new cross-layer dependency", StringComparison.Ordinal));
        Assert.Contains(afterRow.CheckCensus(Next, withNewEdge), f => f.Contains("no tolerated row admits", StringComparison.Ordinal));

        // census growth is not absorbed at the next recording
        var grown = new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 2 };
        var afterGrowth = Planted.Ratchet(PlantedClean, grown, Next);
        Assert.Equal(1, Assert.Single(afterGrowth.Tolerated).Uses);
        Assert.Contains(afterGrowth.CheckCensus(Next, grown), f => f.Contains("grew in the census", StringComparison.Ordinal));

        // the falls: a lowered census count is written and the file is keyed to the new record
        var lowered = new Dictionary<(string, string), int> { [("P.Low", "P.Mid")] = 2 };
        var afterFall = (Planted with { Tolerated = [Planted.Tolerated[0] with { Uses = 3 }] }).Ratchet(PlantedClean, lowered, Next);
        Assert.Equal(2, Assert.Single(afterFall.Tolerated).Uses);
        Assert.Equal(Next, afterFall.Census);
        Assert.Empty(afterFall.CheckCensus(Next, lowered));
        Assert.Empty(afterFall.CheckWalk(PlantedClean, PlantedNamespaces, lowered));

        // a row whose last pair vanished and whose census count is zero is deleted
        MeasuredPair[] removed = [.. PlantedClean.Where(p => p.From != "P.Low.A")];
        var noCensus = new Dictionary<(string, string), int>();
        var afterRemoval = Planted.Ratchet(removed, noCensus, Next);
        Assert.Empty(afterRemoval.Tolerated);
        Assert.Empty(afterRemoval.CheckWalk(removed, PlantedNamespaces, noCensus));
    }

    /// <summary>The register's side of the removers (§8.1): a remover is a kb/Work note, it is not terminal, and its
    /// Removes line names the row — for a pair's remover and a census-only row's alike — and every note a plan names is
    /// a note kb/Work holds.</summary>
    [Fact]
    public void TheRemovers_AreOpenNotesWhoseRemovesLineNamesTheRow()
    {
        var open = new RemoverNote("open", "**Removes (planted):** `P.Low → P.Mid` 1");
        var terminal = new HashSet<string>(["landed", "retired"]);
        Assert.Empty(Planted.RemoverDefects(id => id == "PB1" ? open : null, terminal));
        Assert.Contains(Planted.RemoverDefects(_ => null, terminal), f => f.Contains("PB1 as a remover, which kb/Work does not hold", StringComparison.Ordinal));
        Assert.Contains(Planted.RemoverDefects(_ => open with { Status = "landed" }, terminal), f => f.Contains("PB1 is landed", StringComparison.Ordinal));
        Assert.Contains(Planted.RemoverDefects(_ => open with { Removes = "**Removes:** `P.Low → P.High` 1" }, terminal),
            f => f.Contains("does not name the row", StringComparison.Ordinal));
        var censusOnly = Planted with { Tolerated = [new EdgesRow("P.Mid", "P.High", 1, 0, ["PB2"], "PB2, then PB3", [])] };
        var failures = censusOnly.RemoverDefects(id => id == "PB2" ? open : null, terminal);
        Assert.Contains(failures, f => f.Contains("PB2's **Removes** line does not name the row (P.Mid → P.High)", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("its plan names PB3, which kb/Work does not hold", StringComparison.Ordinal));
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
