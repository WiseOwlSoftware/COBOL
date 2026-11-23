// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#nullable enable
using Xunit;
using Xunit.Abstractions;

namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>THE ORDER OF A RUN (kb/Work PB4328; DESIGN-test-build-ci.md section 3.14.3): the test collections whose
/// classes the gate names FIRST (the file <see cref="FirstVariable"/> names: the classes red at the worktree's previous
/// gate or in the merge-queue run that bounced it, written by <c>scripts/gate_legs.py</c> <c>Legs.first</c>), then every
/// collection longest first by the committed weights (a collection weighs the sum of its classes' weights, a class the
/// file does not name the default), ties in discovery order. Longest first keeps a long collection from starting last
/// and running alone at the end; the first classes put a re-gate's likely reds where its log shows them first. It
/// ORDERS, it never selects: every collection it is handed is returned exactly once (<see cref="GateOrder"/>).</summary>
internal sealed class GateOrderer(string assemblyKey, TestWeights weights, IReadOnlySet<string> first)
{
    /// <summary>The variable naming the first-classes file, one class per line (<c>scripts/gate_host.py</c> <c>FIRST_CLASSES</c>,
    /// the same name; its prefix is one <c>scripts/test_population.py</c> scrubs, so only the gate hands it to a host).</summary>
    public const string FirstVariable = "WISEOWL_COBOL_GATE_FIRST";

    public IReadOnlySet<string> First => first;

    /// <summary>The orderer of <paramref name="environment"/>: the first classes its file names (none when the variable
    /// is unset or empty). Throws when the file it names cannot be read: the executor then keeps xunit's order.</summary>
    public static GateOrderer For(string assemblyKey, TestWeights weights, Func<string, string?> environment)
    {
        string? path = environment(FirstVariable);
        HashSet<string> named = string.IsNullOrEmpty(path)
            ? []
            : File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0).ToHashSet(StringComparer.Ordinal);
        return new GateOrderer(assemblyKey, weights, named);
    }

    /// <summary>The collection orderer of one run's cases: each collection's rank from the classes of its cases.</summary>
    public ITestCollectionOrderer Collections(IEnumerable<ITestCase> cases)
    {
        var classes = new Dictionary<Guid, HashSet<string>>();
        foreach (ITestCase c in cases)
        {
            Guid id = c.TestMethod.TestClass.TestCollection.UniqueID;
            if (!classes.TryGetValue(id, out HashSet<string>? set))
            {
                classes[id] = set = new HashSet<string>(StringComparer.Ordinal);
            }

            set.Add(GateShard.ClassOf(c));
        }

        return new GateCollectionOrderer(classes.ToDictionary(e => e.Key, e => Rank(e.Value)));
    }

    /// <summary>A collection's sort key: first-named before the rest, then heavier before lighter.</summary>
    internal (bool Later, long Lighter) Rank(IEnumerable<string> classes)
    {
        var all = classes.ToList();
        long tenths = all.Sum(c => weights.Tenths.TryGetValue((assemblyKey, c), out long w) ? w : weights.DefaultTenths);
        return (!all.Any(first.Contains), -tenths);
    }
}

/// <summary>Orders collections by their rank (<see cref="GateOrderer.Rank"/>); equal ranks keep xunit's order.</summary>
internal sealed class GateCollectionOrderer(IReadOnlyDictionary<Guid, (bool Later, long Lighter)> ranks) : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        GateOrder.Permutation(testCollections, cs => cs
            .OrderBy(c => ranks.TryGetValue(c.UniqueID, out var r) ? r : (true, 0L)));
}
