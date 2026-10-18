// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>The page layer's test caller: a two-level tree of its own over a <see cref="PagedFile"/> (kb/Work PB3332's
/// B+ tree does not exist yet), written the way every tree of the engine is written — PATH COPYING to the root
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.6 R1) — and an AUDITOR that walks the slots' trees from the raw
/// medium, never through the engine, to check R1 and R2 after the fact.
/// <para>The tree: the prime root is an INTERNAL page whose body is <c>entries</c> × {record id (4), child POINTER (12)}
/// ascending; a child is a LEAF page whose body is {record id (4), blob POINTER (12), text length (2), UTF-8 text}; a blob
/// is a chain of OVERFLOW pages in <see cref="PageChain"/>'s layout (8 bytes of the next page's generation, then
/// payload). A record whose blob has k pages makes one statement allocate k + 2 pages.</para></summary>
internal static class StoreKit
{
    /// <summary>A RELATIVE file's attributes (no keys).</summary>
    public static FixedFileAttributes Relative { get; } = new(FixedFileAttributes.Relative, false, 80, 80, []);

    /// <summary>An INDEXED file with a prime key and one alternate key WITH DUPLICATES.</summary>
    public static FixedFileAttributes TwoKeyIndexed { get; } = new(FixedFileAttributes.Indexed, true, 10, 120,
    [
        new([new KeySegment(0, 8)], Duplicates: false, Suppress: null, Collation: ""),
        new([new KeySegment(8, 12)], Duplicates: true, Suppress: "*", Collation: ""),
    ]);

    /// <summary>A fresh medium holding a new empty relative store, created through <paramref name="unit"/>.</summary>
    public static (MemoryPageMedium Medium, MemoryPageMedium.Handle Handle, PagedFile Store) NewStore(bool coherent = true, string unit = "A")
    {
        var medium = new MemoryPageMedium();
        var handle = medium.Open(unit);
        Assert.Equal(StoreStatus.Ok, PagedFile.Create(handle, Relative, coherent, out var store));
        return (medium, handle, store!);
    }

    /// <summary>Open the store on <paramref name="medium"/> through a new handle for <paramref name="unit"/>.</summary>
    public static (MemoryPageMedium.Handle Handle, StoreStatus Status, PagedFile? Store) OpenAs(MemoryPageMedium medium, string unit,
        bool writable = true, bool coherent = true)
    {
        var handle = medium.Open(unit);
        var status = PagedFile.Open(handle, writable, coherent, out var store);
        return (handle, status, store);
    }

    // ── The test tree's statements ──────────────────────────────────────────────────────────────────────────────

    /// <summary>WRITE or REWRITE record <paramref name="id"/> as <paramref name="text"/> with a blob of
    /// <paramref name="blobPages"/> pages: one statement.</summary>
    public static StoreStatus Put(PagedFile store, int id, string text, int blobPages = 0)
    {
        if (store.Begin(blobPages + 2, out var st) != StoreStatus.Ok) return StoreStatus.PermanentError;
        if (ReadRoot(store, out var children) != PageCheck.Whole)
        {
            st!.Abandon();
            return StoreStatus.PermanentError;
        }
        var blob = PagePointer.None;
        for (int i = blobPages - 1; i >= 0; i--)
        {
            var body = new byte[PageHeader.BodyBytes];
            BinaryPrimitives.WriteUInt64LittleEndian(body, blob.Generation);
            Encoding.UTF8.GetBytes($"{text}#{i}").CopyTo(body, 8);
            blob = st!.Write(PageKind.Overflow, body, flags: blob.IsNone ? (byte)1 : (byte)0, next: blob.Page);
        }
        var leaf = new byte[PageHeader.BodyBytes];
        BinaryPrimitives.WriteInt32LittleEndian(leaf, id);
        blob.Write(leaf.AsSpan(4));
        var utf8 = Encoding.UTF8.GetBytes(text);
        BinaryPrimitives.WriteUInt16LittleEndian(leaf.AsSpan(16), (ushort)utf8.Length);
        utf8.CopyTo(leaf, 18);
        var leafPointer = st!.Write(PageKind.Leaf, leaf);
        if (children.TryGetValue(id, out var old)) ReleaseRecord(store, st, old);
        children[id] = leafPointer;
        return ReplaceRoot(store, st, children);
    }

    /// <summary>DELETE record <paramref name="id"/>: one statement.</summary>
    public static StoreStatus Delete(PagedFile store, int id)
    {
        if (store.Begin(1, out var st) != StoreStatus.Ok) return StoreStatus.PermanentError;
        if (ReadRoot(store, out var children) != PageCheck.Whole || !children.Remove(id, out var old))
        {
            st!.Abandon();
            return StoreStatus.PermanentError;
        }
        ReleaseRecord(store, st!, old);
        return ReplaceRoot(store, st!, children);
    }

    /// <summary>Every record the store's tree holds, read through stamped pointers; null when a page is torn ('30').</summary>
    public static SortedDictionary<int, string>? ReadAll(PagedFile store)
    {
        if (ReadRoot(store, out var children) != PageCheck.Whole) return null;
        var records = new SortedDictionary<int, string>();
        var page = new byte[PageHeader.PageSize];
        foreach (var (id, child) in children)
        {
            if (store.Read(child, page) != PageCheck.Whole) return null;
            int length = BinaryPrimitives.ReadUInt16LittleEndian(page.AsSpan(PageHeader.Bytes + 16));
            records[id] = Encoding.UTF8.GetString(page, PageHeader.Bytes + 18, length);
            var blob = PagePointer.Read(page.AsSpan(PageHeader.Bytes + 4));
            var blobPage = new byte[PageHeader.PageSize];
            while (!blob.IsNone)
            {
                if (store.Read(blob, blobPage) != PageCheck.Whole) return null;
                var h = PageHeader.Decode(blobPage);
                blob = (h.Flags & 1) != 0 ? PagePointer.None
                    : new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(blobPage.AsSpan(PageHeader.Bytes)));
            }
        }
        return records;
    }

    /// <summary>The records as a "id=text;" string, or "TORN".</summary>
    public static string Show(PagedFile store) =>
        ReadAll(store) is { } r ? string.Concat(r.Select(kv => $"{kv.Key}={kv.Value};")) : "TORN";

    private static PageCheck ReadRoot(PagedFile store, out SortedDictionary<int, PagePointer> children)
    {
        children = [];
        var root = store.Meta.PrimeRoot;
        if (root.IsNone) return PageCheck.Whole;
        var page = new byte[PageHeader.PageSize];
        var check = store.Read(root, page);
        if (check != PageCheck.Whole) return check;
        int entries = PageHeader.Decode(page).Entries;
        for (int i = 0; i < entries; i++)
        {
            int at = PageHeader.Bytes + 16 * i;
            children[BinaryPrimitives.ReadInt32LittleEndian(page.AsSpan(at))] = PagePointer.Read(page.AsSpan(at + 4));
        }
        return PageCheck.Whole;
    }

    private static void ReleaseRecord(PagedFile store, PageStatement st, PagePointer leaf)
    {
        var page = new byte[PageHeader.PageSize];
        Assert.Equal(PageCheck.Whole, store.Read(leaf, page));
        var blob = PagePointer.Read(page.AsSpan(PageHeader.Bytes + 4));
        st.Release(leaf);
        while (!blob.IsNone)
        {
            Assert.Equal(PageCheck.Whole, store.Read(blob, page));
            st.Release(blob);
            var h = PageHeader.Decode(page);
            blob = (h.Flags & 1) != 0 ? PagePointer.None
                : new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(PageHeader.Bytes)));
        }
    }

    private static StoreStatus ReplaceRoot(PagedFile store, PageStatement st, SortedDictionary<int, PagePointer> children)
    {
        if (!store.Meta.PrimeRoot.IsNone) st.Release(store.Meta.PrimeRoot);
        if (children.Count == 0)
        {
            st.SetPrimeRoot(PagePointer.None);
            return st.Complete();
        }
        var body = new byte[PageHeader.BodyBytes];
        int i = 0;
        foreach (var (id, child) in children)
        {
            BinaryPrimitives.WriteInt32LittleEndian(body.AsSpan(16 * i), id);
            child.Write(body.AsSpan(16 * i + 4));
            i++;
        }
        st.SetPrimeRoot(st.Write(PageKind.Internal, body, entries: (ushort)children.Count));
        return st.Complete();
    }

    // ── The auditor: the slots' trees, walked from the raw medium ──────────────────────────────────────────────

    /// <summary>The meta pages 0, 1 and 2 decoded from the medium as it stood just after write <paramref name="seq"/>.</summary>
    public static (MetaPage? A, MetaPage? B, MetaPage? Work) MetasAt(MemoryPageMedium medium, long seq) => (
        MetaPage.Decode(medium.SnapshotAt(MetaPage.SlotA, seq), MetaPage.SlotA),
        MetaPage.Decode(medium.SnapshotAt(MetaPage.SlotB, seq), MetaPage.SlotB),
        MetaPage.Decode(medium.SnapshotAt(MetaPage.WorkSlot, seq), MetaPage.WorkSlot));

    /// <summary>Every page the trees of <paramref name="meta"/> reference (the prime tree with its blobs, the FREE
    /// chain, the LEDGER chain), read from the medium as it stood after write <paramref name="seq"/>; a page that does
    /// not verify ends that branch of the walk.</summary>
    public static HashSet<uint> Referenced(MemoryPageMedium medium, MetaPage? meta, long seq, out HashSet<uint> ledgerChain)
    {
        var pages = new HashSet<uint>();
        ledgerChain = [];
        if (meta is null) return pages;
        WalkChain(medium, meta.FreeRoot, seq, pages);
        WalkChain(medium, meta.LedgerRoot, seq, ledgerChain);
        pages.UnionWith(ledgerChain);
        if (Verified(medium, meta.PrimeRoot, seq) is { } root)
        {
            pages.Add(meta.PrimeRoot.Page);
            int entries = PageHeader.Decode(root).Entries;
            for (int i = 0; i < entries; i++)
            {
                var child = PagePointer.Read(root.AsSpan(PageHeader.Bytes + 16 * i + 4));
                if (Verified(medium, child, seq) is not { } leaf) continue;
                pages.Add(child.Page);
                WalkChain(medium, PagePointer.Read(leaf.AsSpan(PageHeader.Bytes + 4)), seq, pages);
            }
        }
        return pages;
    }

    /// <summary>The ledger page 2's meta names, decoded from the raw chain (the auditor's own reading).</summary>
    public static Ledger? LedgerOf(MemoryPageMedium medium, MetaPage meta, long seq)
    {
        var check = PageChain.Read(meta.LedgerRoot, (p, page) =>
        {
            medium.SnapshotAt(p.Page, seq).CopyTo(page, 0);
            return PageHeader.Check(page, p.Page, p.Generation);
        }, out var payload, out _);
        return check == PageCheck.Whole ? Ledger.Decode(payload) : null;
    }

    private static void WalkChain(MemoryPageMedium medium, PagePointer head, long seq, HashSet<uint> into)
    {
        var at = head;
        while (!at.IsNone && Verified(medium, at, seq) is { } page)
        {
            into.Add(at.Page);
            var h = PageHeader.Decode(page);
            at = (h.Flags & 1) != 0 ? PagePointer.None
                : new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(PageHeader.Bytes)));
        }
    }

    private static byte[]? Verified(MemoryPageMedium medium, PagePointer pointer, long seq)
    {
        if (pointer.IsNone) return null;
        var page = medium.SnapshotAt(pointer.Page, seq);
        return PageHeader.Check(page, pointer.Page, pointer.Generation) == PageCheck.Whole ? page : null;
    }

    /// <summary>R1 over the medium's log from write <paramref name="fromSeq"/> on: no write of a tree page targets a page
    /// that the COMMITTED slot's tree or the WORK slot's tree referenced at the instant of the write (the slots
    /// reconstructed as they stood just before it).</summary>
    public static void AssertR1(MemoryPageMedium medium, long fromSeq = 0)
    {
        foreach (var w in medium.Writes.Where(w => w.Seq > fromSeq && w.Page >= MetaPage.MetaPages && w.Unit != "poke"))
        {
            var (a, b, work) = MetasAt(medium, w.Seq - 1);
            var committed = MetaPage.Committed(a, b)?.Meta;
            var referenced = Referenced(medium, committed, w.Seq - 1, out _);
            referenced.UnionWith(Referenced(medium, work, w.Seq - 1, out _));
            Assert.False(referenced.Contains(w.Page),
                $"R1: write #{w.Seq} by {w.Unit} (generation {w.Generation}) targets page {w.Page}, which a slot's tree referenced");
        }
    }

    /// <summary>R2 on the medium as it stands: page 2's ledger lists as FRESH exactly the pages its tree references that
    /// the committed tree does not (its own chain excluded), each with the generation of its last write; RELEASED pages
    /// are referenced by neither tree; FREED pages only by the committed one.</summary>
    public static void AssertR2(MemoryPageMedium medium)
    {
        long seq = medium.LastSeq;
        var (a, b, work) = MetasAt(medium, seq);
        Assert.NotNull(work);
        var committed = MetaPage.Committed(a, b)?.Meta;
        var inCommitted = Referenced(medium, committed, seq, out _);
        var inWork = Referenced(medium, work, seq, out var ownChain);
        var ledger = LedgerOf(medium, work!, seq);
        Assert.NotNull(ledger);
        var expectedFresh = inWork.Except(inCommitted).Except(ownChain).Order().ToArray();
        Assert.Equal(expectedFresh, ledger!.Pages(LedgerState.Fresh).ToArray());
        foreach (var p in expectedFresh)
            Assert.Equal(PageHeader.Decode(medium.SnapshotAt(p, seq)).Generation, ledger.Entries[p].Generation);
        foreach (var p in ledger.Pages(LedgerState.Released))
            Assert.False(inWork.Contains(p) || inCommitted.Contains(p), $"R2: released page {p} is referenced");
        foreach (var p in ledger.Pages(LedgerState.Freed))
            Assert.True(inCommitted.Contains(p) && !inWork.Contains(p), $"R2: freed page {p} is not a committed page the work tree released");
    }

    /// <summary>R3's corollary over the whole log: no (page, generation) pair of a page reached through a stamped pointer
    /// (every page but the three meta pages) is ever written twice.</summary>
    public static void AssertNoStampReused(MemoryPageMedium medium)
    {
        var seen = new HashSet<(uint, ulong)>();
        foreach (var w in medium.Writes.Where(w => w.Page >= MetaPage.MetaPages && w.Unit != "poke"))
            Assert.True(seen.Add((w.Page, w.Generation)), $"R3: page {w.Page} written twice at generation {w.Generation} (write #{w.Seq} by {w.Unit})");
    }

    /// <summary>R3's other corollary: no mint on page 2 — <c>stamp mint</c>, <c>transaction mint</c>, <c>release mint</c> —
    /// ever decreases across the medium's log.</summary>
    public static void AssertMintsNeverDecrease(MemoryPageMedium medium)
    {
        ulong stamp = 0, transaction = 0, release = 0;
        foreach (var w in medium.Writes.Where(w => w.Page == MetaPage.WorkSlot && w.Unit != "poke"))
        {
            var m = MetaPage.Decode(medium.SnapshotAt(MetaPage.WorkSlot, w.Seq), MetaPage.WorkSlot);
            if (m is null) continue;
            Assert.True(m.StampMint >= stamp, $"stamp mint fell from {stamp} to {m.StampMint} at write #{w.Seq}");
            Assert.True(m.TransactionMint >= transaction, $"transaction mint fell at write #{w.Seq}");
            Assert.True(m.ReleaseMint >= release, $"release mint fell at write #{w.Seq}");
            (stamp, transaction, release) = (m.StampMint, m.TransactionMint, m.ReleaseMint);
        }
    }

    /// <summary>The fence's end state (§2.2.1.12 R6): both committed slots and page 2 name the adopted state — the
    /// committed meta and page 2 carry the same roots, and page 2's base is the committed meta's generation.</summary>
    public static void AssertSlotsAndWorkAgree(MemoryPageMedium medium)
    {
        var (a, b, work) = MetasAt(medium, medium.LastSeq);
        var committed = MetaPage.Committed(a, b)!.Value.Meta;
        Assert.NotNull(work);
        Assert.Equal(committed.Generation, work!.CommittedGeneration);
        Assert.Equal(committed.PrimeRoot, work.PrimeRoot);
        Assert.False(work.Promoting);
    }
}
