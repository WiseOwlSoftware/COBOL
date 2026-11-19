// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>A test's way to a bound data item by the name its fixture wrote. The data-names are the symbol table's
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1), which answers a written reference, not a spelling; a test that only
/// needs "the item the fixture declared as NAME" walks the forest in declaration order instead.</summary>
internal static class ForestItems
{
    /// <summary>The first data item of <paramref name="forest"/>, in declaration order (each root in source order, then its
    /// subordinates, then the level-66 RENAMES entries it carries), whose name is <paramref name="name"/> (the fixture's own spelling, compared case-insensitively).</summary>
    public static DataItem ItemNamed(this ForestSlice forest, string name) =>
        Walk(forest.Roots).FirstOrDefault(i => string.Equals(i.CobolName, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"the fixture declares no data item '{name}'");

    private static IEnumerable<DataItem> Walk(IEnumerable<DataItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var sub in Walk(item.Children)) yield return sub;
            foreach (var alias in item.Renames66) yield return alias;
        }
    }
}
