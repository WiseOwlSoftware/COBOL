// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Collation;
using WiseOwl.Cobol.Runtime.Collation.Cldr;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Collation;

/// <summary>
/// The fixed cost of building a locale's table (kb/Work PB107: ~6 ms and 10.5 MB for Spanish's THREE rules). Guarded by
/// STRUCTURE, not by a clock (kb/Work PB1590): the allocation of one build, which is what the cost was made of — a scan
/// of the 41,724-element base pool into hash sets, a root-by-root numbering of every weight line, a delegate call per
/// element to re-weight the pool, and the pool copied into a list and out of it twice.
/// </summary>
public sealed class CldrBuildCostTests
{
    private static long AllocatedByABuild(string tag)
    {
        var selection = CldrLocaleLoader.ResolveCollation(tag);
        for (int i = 0; i < 3; i++) CldrTailoringBuilder.Build(selection, "cost-" + tag);        // JIT, the base table's cached roots
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 5; i++) CldrTailoringBuilder.Build(selection, "cost-" + tag);
        return (GC.GetAllocatedBytesForCurrentThread() - before) / 5;
    }

    /// <summary>Three rules over a 41,724-element pool: the new pool (≈1 MB) and the copied mapping dictionaries are
    /// what a build must allocate; it was 10.5 MB.</summary>
    [Theory]
    [InlineData("es")]
    [InlineData("da")]
    [InlineData("vi")]
    public void ABuild_AllocatesTheNewPoolAndMappings_NotACopyOfEverythingSeveralTimes(string tag)
    {
        long bytes = AllocatedByABuild(tag);
        Assert.True(bytes < 4 * 1024 * 1024, $"{tag}: {bytes / 1024} KB per build");
    }

    /// <summary>A collation with no rules is the root table itself, built from nothing.</summary>
    [Fact]
    public void ACollationWithNoRules_BuildsNothing()
    {
        long bytes = AllocatedByABuild("de");
        Assert.True(bytes < 64 * 1024, $"de: {bytes / 1024} KB per build");
        Assert.Same(CollationTable.Root, CldrTailoringBuilder.Build(CldrLocaleLoader.ResolveCollation("de"), "cost-de").Table);
    }

    /// <summary>The numbering is sparse and the re-weighting is a lookup, so two builds of one collation give tables
    /// that order every pair of a corpus the same way — and the first of them agrees with a build of the same rules
    /// through the whole engine path.</summary>
    [Fact]
    public void RepeatedBuilds_GiveOneOrder()
    {
        var selection = CldrLocaleLoader.ResolveCollation("vi");
        var a = CldrTailoringBuilder.Build(selection, "cost-a");
        var b = CldrTailoringBuilder.Build(selection, "cost-b");
        var ca = new Collator(a.Table, a.Options);
        var cb = new Collator(b.Table, b.Options);
        string[] corpus = ["a", "à", "ả", "ã", "á", "ạ", "ă", "ằ", "ắ", "ặ", "â", "ầ", "ấ", "ậ", "d", "đ", "e", "ê", "o", "ô", "ơ", "u", "ư", "ba", "bà", "bả"];
        foreach (string x in corpus)
            foreach (string y in corpus)
                Assert.Equal(Math.Sign(ca.Compare(x, y)), Math.Sign(cb.Compare(x, y)));
        Assert.Equal(CollationEngine.ForLocale("vi").Compare("ạ", "ă"), ca.Compare("ạ", "ă"));
    }
}
