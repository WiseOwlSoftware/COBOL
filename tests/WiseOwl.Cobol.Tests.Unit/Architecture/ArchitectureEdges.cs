// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>One layer of the architecture: its id and the namespaces it holds (a trailing <c>.*</c> admits every
/// namespace below the prefix).</summary>
internal sealed record EdgesLayer(string Id, IReadOnlyList<string> Namespaces);

/// <summary>An assembly outside the graph by rule, with the reason.</summary>
internal sealed record EdgesExclusion(string Assembly, string Reason);

/// <summary>One tolerated type pair of a tolerated row and the kb/Work notes that remove it.</summary>
internal sealed record EdgesPair(string From, string To, IReadOnlyList<string> RemovedBy);

/// <summary>A cross-layer namespace edge the layers do not allow, tolerated until its removers land: the census's
/// count (<see cref="Uses"/>), the walk's (<see cref="WalkUses"/>), every type pair the walk measures in it with its
/// removers, and the union of those removers (<see cref="RemovedBy"/>).</summary>
internal sealed record EdgesRow(string From, string To, int Uses, int WalkUses, IReadOnlyList<string> RemovedBy, string Plan,
    IReadOnlyList<EdgesPair> Pairs);

/// <summary>
/// THE ARCHITECTURE EDGES FILE, <c>docs/rearchitecture/architecture-edges.json</c> (kb/Work PB2417;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.1): the ONE place the layers and their allowed edges are
/// written. A cross-layer dependency is allowed if and only if its layer pair is listed; an edge inside a layer is
/// free; every product namespace is placed; a tolerated row lists every type pair it holds, each with the notes that
/// remove it, and its two observers' counts only ever fall. <see cref="CheckWalk"/> is the per-gate observer (the
/// metadata walk), <see cref="CheckCensus"/> the per-wave one (the census record); both read this one file.
/// </summary>
internal sealed record ArchitectureEdges(
    int Schema,
    string Rule,
    string Census,
    IReadOnlyList<EdgesLayer> Layers,
    IReadOnlyList<string> CompositionRoots,
    IReadOnlyList<EdgesExclusion> Excluded,
    IReadOnlyList<IReadOnlyList<string>> Allowed,
    IReadOnlyList<EdgesRow> Tolerated)
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ArchitectureEdges Load(string path) =>
        JsonSerializer.Deserialize<ArchitectureEdges>(File.ReadAllText(path), Json)
        ?? throw new InvalidDataException($"{path} is not an architecture edges file");

    public string ToJson() => JsonSerializer.Serialize(this, Json).ReplaceLineEndings("\n") + "\n";

    /// <summary>The layer a namespace is placed in, or null when the file does not place it. A namespace two layers
    /// claim is a defect of the file (<see cref="FileDefects"/>).</summary>
    public string? LayerOf(string ns)
    {
        foreach (EdgesLayer layer in Layers)
        {
            if (layer.Namespaces.Any(pattern => Matches(pattern, ns)))
            {
                return layer.Id;
            }
        }

        return null;
    }

    /// <summary>Whether an edge between two placed layers needs no row: the same layer, or a listed pair.</summary>
    public bool Allows(string fromLayer, string toLayer) =>
        fromLayer == toLayer || Allowed.Any(p => p[0] == fromLayer && p[1] == toLayer);

    /// <summary>The defects of the file alone: a layer, pair or row that is malformed, duplicated or contradicts
    /// another part of the file.</summary>
    public IReadOnlyList<string> FileDefects()
    {
        var failures = new List<string>();
        var ids = Layers.Select(l => l.Id).ToList();
        failures.AddRange(ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => $"layer '{g.Key}' is declared twice"));
        foreach (IReadOnlyList<string> pair in Allowed)
        {
            if (pair.Count != 2 || !ids.Contains(pair[0]) || !ids.Contains(pair[1]) || pair[0] == pair[1])
            {
                failures.Add($"allowed pair [{string.Join(", ", pair)}] does not name two different layers of the file");
            }
        }

        failures.AddRange(Allowed.GroupBy(p => (p[0], p[1])).Where(g => g.Count() > 1)
            .Select(g => $"allowed pair [{g.Key.Item1}, {g.Key.Item2}] is listed twice"));
        failures.AddRange(CompositionRoots.Where(r => !ids.Contains(r)).Select(r => $"composition root '{r}' is no layer"));
        failures.AddRange(Tolerated.GroupBy(r => (r.From, r.To)).Where(g => g.Count() > 1)
            .Select(g => $"tolerated row {g.Key.From} -> {g.Key.To} is listed twice"));
        foreach (EdgesRow row in Tolerated)
        {
            string name = $"tolerated row {row.From} -> {row.To}";
            if (LayerOf(row.From) is not { } from || LayerOf(row.To) is not { } to)
            {
                failures.Add($"{name} names a namespace no layer places");
                continue;
            }

            if (Allows(from, to))
            {
                failures.Add($"{name} is an edge the layers allow ({from} -> {to}): delete the row");
            }

            if (row.Uses == 0 && row.WalkUses == 0)
            {
                failures.Add($"{name} measures zero in both observers: delete the row (it is removed)");
            }

            if (row.Pairs.Count != row.WalkUses)
            {
                failures.Add($"{name} lists {row.Pairs.Count} pair(s) but its walkUses is {row.WalkUses}");
            }

            failures.AddRange(row.Pairs.GroupBy(p => (p.From, p.To)).Where(g => g.Count() > 1)
                .Select(g => $"{name} lists the pair {g.Key.From} -> {g.Key.To} twice"));
            foreach (EdgesPair pair in row.Pairs.Where(p => p.RemovedBy.Count == 0))
            {
                failures.Add($"{name}: the pair {pair.From} -> {pair.To} has no remover — file a kb/Work note that removes it "
                             + "and name it in the pair's removedBy (DESIGN-architecture-review §8.1: a pair no note removes blocks the file)");
            }

            var union = row.Pairs.SelectMany(p => p.RemovedBy).Distinct().Order(StringComparer.Ordinal).ToList();
            if (!union.SequenceEqual(row.RemovedBy.Order(StringComparer.Ordinal)))
            {
                failures.Add($"{name}: removedBy [{string.Join(", ", row.RemovedBy)}] is not the union of its pairs' removers "
                             + $"[{string.Join(", ", union)}]");
            }
        }

        return failures;
    }

    /// <summary>
    /// The walk's checks, over the namespaced type pairs it measured (after an optional what-if: <paramref name="moves"/>
    /// re-keys an outermost type to another namespace, <paramref name="cuts"/> removes pairs, before the pairs
    /// aggregate). <paramref name="productNamespaces"/> is every namespace an assembly declares a product type in;
    /// <paramref name="censusEdges"/> is the census's cross-namespace counts (the other observer of a dead allowance).
    /// </summary>
    public IReadOnlyList<string> CheckWalk(IReadOnlyCollection<MeasuredPair> measured, IReadOnlySet<string> productNamespaces,
        IReadOnlyDictionary<(string From, string To), int> censusEdges, WhatIf? whatIf = null)
    {
        var failures = new List<string>(FileDefects());
        IReadOnlyCollection<MeasuredPair> pairs = whatIf is null ? measured : whatIf.Apply(measured);
        var namespaces = new HashSet<string>(productNamespaces, StringComparer.Ordinal);
        namespaces.UnionWith(pairs.SelectMany(p => new[] { p.FromNamespace, p.ToNamespace }));

        foreach (string ns in namespaces.Order(StringComparer.Ordinal).Where(ns => LayerOf(ns) is null))
        {
            failures.Add($"the product namespace {ns} is placed in no layer: add it to the layer its folder belongs to");
        }

        foreach (EdgesLayer layer in Layers)
        {
            foreach (string pattern in layer.Namespaces.Where(p => !namespaces.Any(ns => Matches(p, ns))))
            {
                failures.Add($"layer '{layer.Id}' names {pattern}, which no product assembly declares a type in: delete it");
            }
        }

        foreach (string ns in namespaces.Order(StringComparer.Ordinal))
        {
            var placing = Layers.Where(l => l.Namespaces.Any(p => Matches(p, ns))).Select(l => l.Id).ToList();
            if (placing.Count > 1)
            {
                failures.Add($"the namespace {ns} is placed in {placing.Count} layers ({string.Join(", ", placing)})");
            }
        }

        var rows = Tolerated.ToDictionary(r => (r.From, r.To));
        var measuredPerRow = new Dictionary<(string, string), HashSet<(string, string)>>();
        var measuredLayerPairs = new HashSet<(string, string)>();
        foreach (MeasuredPair p in pairs)
        {
            if (LayerOf(p.FromNamespace) is not { } from || LayerOf(p.ToNamespace) is not { } to || from == to)
            {
                continue;
            }

            measuredLayerPairs.Add((from, to));
            if (Allows(from, to))
            {
                continue;
            }

            var key = (p.FromNamespace, p.ToNamespace);
            if (!rows.TryGetValue(key, out EdgesRow? row))
            {
                failures.Add($"{p.From} -> {p.To} crosses {from} -> {to}, which the layers do not allow and no tolerated row "
                             + $"({p.FromNamespace} -> {p.ToNamespace}) admits: a new cross-layer dependency");
                continue;
            }

            if (!row.Pairs.Any(r => r.From == p.From && r.To == p.To))
            {
                failures.Add($"{p.From} -> {p.To} is a pair the tolerated row {row.From} -> {row.To} does not list: a tolerated "
                             + "row never gains a pair (its removers do not remove it)");
            }

            (measuredPerRow.TryGetValue(key, out var set) ? set : measuredPerRow[key] = []).Add((p.From, p.To));
        }

        foreach (EdgesRow row in Tolerated)
        {
            int walked = measuredPerRow.TryGetValue((row.From, row.To), out var set) ? set.Count : 0;
            if (walked > row.WalkUses)
            {
                failures.Add($"tolerated row {row.From} -> {row.To} grew: the walk measures {walked} pair(s) against walkUses {row.WalkUses}");
            }

            if (whatIf is not null)
            {
                continue;   // a what-if answers what a move does; the rows it shrinks are its point
            }

            foreach (EdgesPair listed in row.Pairs.Where(l => set is null || !set.Contains((l.From, l.To))))
            {
                failures.Add($"tolerated row {row.From} -> {row.To} lists {listed.From} -> {listed.To}, which the walk no longer "
                             + "measures: delete the pair and lower walkUses (the ratchet only falls)");
            }
        }

        foreach (IReadOnlyList<string> pair in Allowed.Where(p => p.Count == 2 && !CompositionRoots.Contains(p[0])))
        {
            bool census = censusEdges.Any(e => LayerOf(e.Key.From) == pair[0] && LayerOf(e.Key.To) == pair[1] && e.Value > 0);
            if (whatIf is null && !census && !measuredLayerPairs.Contains((pair[0], pair[1])))
            {
                failures.Add($"allowed pair [{pair[0]}, {pair[1]}] measures zero in both observers and is from no composition "
                             + "root: a dead allowance is deleted, never kept");
            }
        }

        return failures;
    }

    /// <summary>The census's checks over the newest record's namespace edges (<c>namespaceEdges</c>): every cross-layer
    /// edge is allowed, or tolerated by a row whose <see cref="EdgesRow.Uses"/> is that count — the record the file is
    /// keyed to (<see cref="Census"/>) is the newest one.</summary>
    public IReadOnlyList<string> CheckCensus(string newestRecord, IReadOnlyDictionary<(string From, string To), int> censusEdges)
    {
        var failures = new List<string>();
        if (newestRecord != Census)
        {
            failures.Add($"the edges file's uses are keyed to census {Short(Census)} but the newest record is "
                         + $"{Short(newestRecord)}: re-seed the file in the change that records the census");
        }

        var rows = Tolerated.ToDictionary(r => (r.From, r.To));
        foreach (((string from, string to), int count) in censusEdges.OrderBy(e => e.Key.From, StringComparer.Ordinal)
                     .ThenBy(e => e.Key.To, StringComparer.Ordinal))
        {
            if (LayerOf(from) is not { } fromLayer || LayerOf(to) is not { } toLayer)
            {
                failures.Add($"the census edge {from} -> {to} names a namespace no layer places");
                continue;
            }

            if (Allows(fromLayer, toLayer))
            {
                continue;
            }

            if (!rows.TryGetValue((from, to), out EdgesRow? row))
            {
                failures.Add($"the census edge {from} -> {to} ({count}) crosses {fromLayer} -> {toLayer}, which the layers do "
                             + "not allow and no tolerated row admits");
            }
            else if (count > row.Uses)
            {
                failures.Add($"tolerated row {from} -> {to} grew in the census: {count} against uses {row.Uses}");
            }
            else if (newestRecord == Census && count != row.Uses)
            {
                failures.Add($"tolerated row {from} -> {to} carries uses {row.Uses} but census {Short(Census)} measures {count}");
            }
        }

        foreach (EdgesRow row in Tolerated.Where(r => newestRecord == Census && r.Uses > 0 && !censusEdges.ContainsKey((r.From, r.To))))
        {
            failures.Add($"tolerated row {row.From} -> {row.To} carries uses {row.Uses} but census {Short(Census)} measures none");
        }

        return failures;
    }

    private static string Short(string sha) => sha[..Math.Min(12, sha.Length)];

    private static bool Matches(string pattern, string ns) =>
        pattern.EndsWith(".*", StringComparison.Ordinal)
            ? ns.StartsWith(pattern[..^1], StringComparison.Ordinal)
            : pattern == ns;
}

/// <summary>A dependency the walk measured, with each end's namespace.</summary>
internal readonly record struct MeasuredPair(string From, string FromNamespace, string To, string ToNamespace);

/// <summary>
/// THE WHAT-IF (DESIGN-architecture-review §8.1): a move map (outermost type → the namespace it moves to) and a cut list
/// (type pairs an Extract removes), applied to the measured pairs before they aggregate, so the same checker that
/// ratchets the rows answers what a planned wave would do to them.
/// </summary>
internal sealed record WhatIf(IReadOnlyDictionary<string, string> Moves, IReadOnlyList<IReadOnlyList<string>> Cuts)
{
    public static WhatIf Load(string path) =>
        JsonSerializer.Deserialize<WhatIf>(File.ReadAllText(path), ArchitectureEdges.Json)
        ?? throw new InvalidDataException($"{path} is not a what-if file ({{\"moves\": {{type: namespace}}, \"cuts\": [[from, to]]}})");

    public IReadOnlyCollection<MeasuredPair> Apply(IReadOnlyCollection<MeasuredPair> pairs)
    {
        var cut = Cuts.Select(c => (c[0], c[1])).ToHashSet();
        return [.. pairs
            .Where(p => !cut.Contains((p.From, p.To)))
            .Select(p => p with
            {
                FromNamespace = Moves.TryGetValue(p.From, out string? f) ? f : p.FromNamespace,
                ToNamespace = Moves.TryGetValue(p.To, out string? t) ? t : p.ToNamespace,
            })
            .Where(p => p.FromNamespace != p.ToNamespace)];
    }
}
