// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>The transaction witnesses' view of the three trees of DESIGN-runtime-library.md 2.2.1.10 (in docs/rearchitecture;
/// kb/Work PB3337), walked through the trees themselves and never through the engine's own enumeration, so a witness that
/// asks it checks the engine rather than repeating it.</summary>
internal static class TransactionTrees
{
    /// <summary>⛔ The drift test of 2.2.1.12 (finding R5-2): the HOLDINGS tree's key set equals the union of the UNDO
    /// trees' key sets, on the file as <paramref name="reg"/>'s store last saw it.</summary>
    public static void AssertHoldingsAreTheUndoKeys(FileRegistry reg, string name = "F")
    {
        var file = reg.PagedStoreOf(name)!;
        var holdings = Keys(file, file.Meta.HoldingsRoot);
        var undo = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (_, value) in Entries(file, file.Meta.TransactionsRoot))
            undo.UnionWith(Keys(file, TransactionKeys.PointerOf(value)));
        Assert.Equal(undo, holdings);
    }

    /// <summary>Nothing of any transaction is left in the file as <paramref name="reg"/>'s store last saw it: the
    /// TRANSACTIONS and HOLDINGS trees are empty.</summary>
    public static void AssertNoTransactionLeft(FileRegistry reg, string name = "F")
    {
        var meta = reg.PagedStoreOf(name)!.Meta;
        Assert.True(meta.HoldingsRoot.IsNone, "the HOLDINGS tree is empty once no transaction is open");
        Assert.True(meta.TransactionsRoot.IsNone, "the TRANSACTIONS tree is empty once no transaction is open");
    }

    private static SortedSet<string> Keys(PagedFile file, PagePointer root) =>
        new(Entries(file, root).Select(e => Convert.ToHexString(e.Key)), StringComparer.Ordinal);

    private static List<(byte[] Key, byte[] Value)> Entries(PagedFile file, PagePointer root)
    {
        var entries = new List<(byte[], byte[])>();
        if (root.IsNone) return entries;
        Assert.Equal(StoreStatus.Ok, new BTree(file, KeyOrders.Ordinal, root).First(out var cursor));
        while (cursor.HasCurrent)
        {
            Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var value));
            entries.Add((cursor.Key, value));
            Assert.Equal(StoreStatus.Ok, cursor.Next());
        }
        return entries;
    }
}
