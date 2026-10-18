// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>The page layer's test caller and its AUDITOR (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12).
/// <para>The caller writes records into the store's prime tree — the engine's own <see cref="BTree"/> (kb/Work PB3332)
/// under the ordinal order, keyed by the record id (4 bytes big-endian) — one statement per record the way every
/// connector will (PB3333): the coherent entry, the edit in memory, <see cref="PagedFile.Begin"/> sized to exactly the
/// pages the edit will flush, the flush, the root, the completion. A record's value is its text (UTF-8, after a 2-byte
/// length); a record of <c>blobPages</c> k &gt; 0 pads its value to k overflow pages' payload, so its statement writes k
/// overflow pages beside the tree's path.</para>
/// <para>The auditor walks the slots' trees from the RAW medium with its own decoder of 2.2.1.4's INTERNAL, LEAF and
/// OVERFLOW layouts, never through the engine, to check R1 and R2 after the fact.</para></summary>
internal static class StoreKit
{
    /// <summary>Whether page 2 of the physical file at <paramref name="host"/> carries PROMOTING (2.2.1.6 R5), read beside
    /// the run units that hold it open.</summary>
    public static bool PageTwoPromoting(string host)
    {
        using var stream = new FileStream(host, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var page = new byte[PageHeader.PageSize];
        stream.Position = MetaPage.WorkSlot * (long)PageHeader.PageSize;
        stream.ReadExactly(page);
        return MetaPage.Decode(page, MetaPage.WorkSlot) is { Promoting: true };
    }

    /// <summary>A RELATIVE file's attributes (no keys).</summary>
    public static FixedFileAttributes Relative { get; } = new(FixedFileAttributes.Relative, false, 80, 80, []);

    /// <summary>An INDEXED file with a prime key and one alternate key WITH DUPLICATES.</summary>
    public static FixedFileAttributes TwoKeyIndexed { get; } = new(FixedFileAttributes.Indexed, true, 10, 120,
    [
        new([new KeySegment(0, 8)], Duplicates: false, Suppress: null, Collation: ""),
        new([new KeySegment(8, 12)], Duplicates: true, Suppress: "*", Collation: ""),
    ]);

    /// <summary>A store that holds <paramref name="records"/> (each "id=text", a blob of three pages for the first) and was
    /// CLOSEd, over a fresh medium: the PREVIOUS file an <c>OPEN OUTPUT</c> recreates.</summary>
    public static MemoryPageMedium ClosedStore(params string[] records)
    {
        var (medium, _, store) = NewStore();
        for (int i = 0; i < records.Length; i++)
        {
            var (id, text) = (int.Parse(records[i].Split('=')[0]), records[i].Split('=')[1]);
            Assert.Equal(StoreStatus.Ok, Put(store, id, text, blobPages: i == 0 ? 3 : 0));
        }
        Assert.Equal(StoreStatus.Ok, store.Close());
        return medium;
    }

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

    // ── The test caller's statements ────────────────────────────────────────────────────────────────────────────

    /// <summary>WRITE or REWRITE record <paramref name="id"/> as <paramref name="text"/>, its value padded to
    /// <paramref name="blobPages"/> overflow pages: one statement.</summary>
    public static StoreStatus Put(PagedFile store, int id, string text, int blobPages = 0)
    {
        if (store.Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        var tree = Prime(store);
        if (tree.Put(Key(id), Value(text, blobPages)) != StoreStatus.Ok) return StoreStatus.PermanentError;
        return Commit(store, tree);
    }

    /// <summary>DELETE record <paramref name="id"/>: one statement ('30' when it does not exist: the test asks only for
    /// records it wrote).</summary>
    public static StoreStatus Delete(PagedFile store, int id)
    {
        if (store.Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        var tree = Prime(store);
        if (tree.Delete(Key(id), out bool deleted) != StoreStatus.Ok || !deleted) return StoreStatus.PermanentError;
        return Commit(store, tree);
    }

    /// <summary>Every record the store's tree holds, read through stamped pointers; null when a page is torn ('30').</summary>
    public static SortedDictionary<int, string>? ReadAll(PagedFile store)
    {
        var records = new SortedDictionary<int, string>();
        if (Prime(store).First(out var cursor) != StoreStatus.Ok) return null;
        while (cursor.HasCurrent)
        {
            if (cursor.ReadValue(out var value) != StoreStatus.Ok) return null;
            records[(int)KeyOrders.PageOf(cursor.Key)] = Text(value);
            if (cursor.Next() != StoreStatus.Ok) return null;
        }
        return records;
    }

    /// <summary>The records as a "id=text;" string, or "TORN".</summary>
    public static string Show(PagedFile store) =>
        ReadAll(store) is { } r ? string.Concat(r.Select(kv => $"{kv.Key}={kv.Value};")) : "TORN";

    /// <summary>The store's prime tree as its meta names it.</summary>
    public static BTree Prime(PagedFile store) => new(store, KeyOrders.Ordinal, store.Meta.PrimeRoot);

    /// <summary>The key of record <paramref name="id"/>.</summary>
    public static byte[] Key(int id) => KeyOrders.PageKey((uint)id);

    /// <summary>The value of a record of <paramref name="text"/> padded to <paramref name="blobPages"/> overflow pages.</summary>
    public static byte[] Value(string text, int blobPages)
    {
        var utf8 = Encoding.UTF8.GetBytes(text);
        var value = new byte[blobPages == 0 ? 2 + utf8.Length : blobPages * OverflowChain.PayloadBytes];
        BinaryPrimitives.WriteUInt16LittleEndian(value, (ushort)utf8.Length);
        utf8.CopyTo(value, 2);
        return value;
    }

    private static string Text(byte[] value) =>
        Encoding.UTF8.GetString(value, 2, BinaryPrimitives.ReadUInt16LittleEndian(value));

    /// <summary>The statement of a pending edit of the prime tree: begun at exactly the edit's bound (the pages it will
    /// write and the pages it stopped referencing; the entry already run), flushed, its root set, completed. A refused
    /// Begin or completion answers what it answered ('30', or the boundary of 2.2.1.9).</summary>
    public static StoreStatus Commit(PagedFile store, BTree tree)
    {
        if (store.Begin(StatementBound.Of(tree), out var st, entered: true) is var begun && begun != StoreStatus.Ok)
        {
            tree.Discard();
            return begun;
        }
        var flushed = tree.Flush(st!, out var root);
        st!.SetPrimeRoot(root);
        var completed = st.Complete();
        return flushed != StoreStatus.Ok && completed == StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    // ── The auditor: the slots' trees, walked from the raw medium ──────────────────────────────────────────────

    /// <summary>The meta pages 0, 1 and 2 decoded from the medium as it stood just after write <paramref name="seq"/>.</summary>
    public static (MetaPage? A, MetaPage? B, MetaPage? Work) MetasAt(MemoryPageMedium medium, long seq) => (
        MetaPage.Decode(medium.SnapshotAt(MetaPage.SlotA, seq), MetaPage.SlotA),
        MetaPage.Decode(medium.SnapshotAt(MetaPage.SlotB, seq), MetaPage.SlotB),
        MetaPage.Decode(medium.SnapshotAt(MetaPage.WorkSlot, seq), MetaPage.WorkSlot));

    /// <summary>Every page the trees of <paramref name="meta"/> reference (the prime tree with its overflow chains, the
    /// FREE tree, the LEDGER tree — whose own pages are also <paramref name="ledgerPages"/>), read from the medium as it
    /// stood after write <paramref name="seq"/>; a page that does not verify ends that branch of the walk.</summary>
    public static HashSet<uint> Referenced(MemoryPageMedium medium, MetaPage? meta, long seq, out HashSet<uint> ledgerPages)
    {
        var pages = new HashSet<uint>();
        ledgerPages = [];
        if (meta is null) return pages;
        WalkTree(medium, meta.FreeRoot, seq, pages, null);
        WalkTree(medium, meta.LedgerRoot, seq, ledgerPages, null);
        pages.UnionWith(ledgerPages);
        WalkTree(medium, meta.PrimeRoot, seq, pages, null);
        return pages;
    }

    /// <summary>The ledger <paramref name="meta"/> names, decoded from the raw tree (the auditor's own reading); null when
    /// a page of it does not verify.</summary>
    public static SortedDictionary<uint, LedgerEntry>? LedgerOf(MemoryPageMedium medium, MetaPage meta, long seq)
    {
        var entries = new SortedDictionary<uint, LedgerEntry>();
        bool whole = WalkTree(medium, meta.LedgerRoot, seq, [], (key, value) =>
            entries.Add(BinaryPrimitives.ReadUInt32BigEndian(key),
                new LedgerEntry(BinaryPrimitives.ReadUInt64LittleEndian(value!), (LedgerState)value![8])));
        return whole ? entries : null;
    }

    /// <summary>The pages of <paramref name="ledger"/> in <paramref name="state"/>, ascending.</summary>
    public static uint[] Pages(IReadOnlyDictionary<uint, LedgerEntry> ledger, LedgerState state) =>
        ledger.Where(e => e.Value.State == state).Select(e => e.Key).Order().ToArray();

    /// <summary>Walk the tree at <paramref name="root"/> by 2.2.1.4's layouts — INTERNAL: the leftmost child POINTER, then
    /// per key {key length (2), key, child POINTER}; LEAF: per entry {key length (2), value length (4; bit 31: an
    /// OVERFLOW reference of a head POINTER and a total length), key, value} — adding every verified page, the overflow
    /// chains' included, to <paramref name="into"/> and handing each leaf entry to <paramref name="entry"/> (an overflow
    /// value as null); false when a page did not verify.</summary>
    private static bool WalkTree(MemoryPageMedium medium, PagePointer root, long seq, HashSet<uint> into, Action<byte[], byte[]?>? entry)
    {
        if (root.IsNone) return true;
        if (ReadNode(medium, root, seq) is not { } node) return false;
        into.Add(root.Page);
        bool whole = true;
        if (node.Kind == PageKind.Internal)
        {
            foreach (var child in node.Children) whole &= WalkTree(medium, child, seq, into, entry);
            return whole;
        }
        for (int i = 0; i < node.Keys.Count; i++)
        {
            var value = node.Values[i];
            if (value.Inline is null) whole &= WalkChain(medium, value.Head, seq, into);
            entry?.Invoke(node.Keys[i], value.Inline);
        }
        return whole;
    }

    /// <summary>A B+ tree node as the auditor decodes it from the raw medium (2.2.1.4): INTERNAL — the leftmost child
    /// POINTER, then per key {key length (2), key, child POINTER}; LEAF — per entry {key length (2), value length (4; bit
    /// 31: an OVERFLOW reference of a head POINTER and a total length), key, value}. <see cref="UsedBytes"/> is the
    /// body's used length.</summary>
    internal sealed record RawNode(uint Page, PageKind Kind, List<byte[]> Keys, List<PagePointer> Children, List<RawValue> Values, int UsedBytes);

    /// <summary>A leaf value as the auditor decodes it: inline bytes, or an overflow chain's head and total length.</summary>
    internal readonly record struct RawValue(byte[]? Inline, PagePointer Head, uint Total);

    /// <summary>The node <paramref name="pointer"/> names, decoded from the medium as it stood after write
    /// <paramref name="seq"/> (the latest when -1); null when the page does not verify against the stamp.</summary>
    public static RawNode? ReadNode(MemoryPageMedium medium, PagePointer pointer, long seq = -1)
    {
        var page = Verified(medium, pointer, seq < 0 ? medium.LastSeq : seq);
        if (page is null) return null;
        var h = PageHeader.Decode(page);
        Assert.True(h.Kind is PageKind.Internal or PageKind.Leaf, $"page {pointer.Page} is a {h.Kind} page, not a tree node");
        Assert.Equal(0u, h.Next);   // no sibling pointers (2.2.1.4)
        var body = page.AsSpan(PageHeader.Bytes);
        var node = new RawNode(pointer.Page, h.Kind, [], [], [], 0);
        int at = 0;
        if (h.Kind == PageKind.Internal)
        {
            node.Children.Add(PagePointer.Read(body));
            at = PagePointer.Bytes;
        }
        for (int i = 0; i < h.Entries; i++)
        {
            int keyLength = BinaryPrimitives.ReadUInt16LittleEndian(body[at..]);
            if (h.Kind == PageKind.Internal)
            {
                node.Keys.Add(body.Slice(at + 2, keyLength).ToArray());
                at += 2 + keyLength;
                node.Children.Add(PagePointer.Read(body[at..]));
                at += PagePointer.Bytes;
                continue;
            }
            uint word = BinaryPrimitives.ReadUInt32LittleEndian(body[(at + 2)..]);
            at += 6;
            node.Keys.Add(body.Slice(at, keyLength).ToArray());
            at += keyLength;
            int stored = (int)(word & 0x7FFF_FFFF);
            if ((word & 0x8000_0000) != 0)
            {
                Assert.Equal(16, stored);
                node.Values.Add(new RawValue(null, PagePointer.Read(body[at..]), BinaryPrimitives.ReadUInt32LittleEndian(body[(at + 12)..])));
            }
            else node.Values.Add(new RawValue(body.Slice(at, stored).ToArray(), PagePointer.None, (uint)stored));
            at += stored;
        }
        return node with { UsedBytes = at };
    }

    private static bool WalkChain(MemoryPageMedium medium, PagePointer head, long seq, HashSet<uint> into)
    {
        var at = head;
        while (!at.IsNone)
        {
            if (Verified(medium, at, seq) is not { } page) return false;
            into.Add(at.Page);
            var h = PageHeader.Decode(page);
            at = (h.Flags & 1) != 0 ? PagePointer.None
                : new PagePointer(h.Next, BinaryPrimitives.ReadUInt64LittleEndian(page.AsSpan(PageHeader.Bytes)));
        }
        return true;
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
    /// the committed tree does not (its own pages excluded), each with the generation of its last write; RELEASED pages
    /// are referenced by neither tree; FREED pages only by the committed one.</summary>
    public static void AssertR2(MemoryPageMedium medium)
    {
        long seq = medium.LastSeq;
        var (a, b, work) = MetasAt(medium, seq);
        Assert.NotNull(work);
        var committed = MetaPage.Committed(a, b)?.Meta;
        var inCommitted = Referenced(medium, committed, seq, out _);
        var inWork = Referenced(medium, work, seq, out var ownPages);
        var ledger = LedgerOf(medium, work!, seq);
        Assert.NotNull(ledger);
        var expectedFresh = inWork.Except(inCommitted).Except(ownPages).Order().ToArray();
        Assert.Equal(expectedFresh, Pages(ledger!, LedgerState.Fresh));
        foreach (var p in expectedFresh)
            Assert.Equal(PageHeader.Decode(medium.SnapshotAt(p, seq)).Generation, ledger[p].Generation);
        foreach (var p in Pages(ledger, LedgerState.Released))
            Assert.False(inWork.Contains(p) || inCommitted.Contains(p), $"R2: released page {p} is referenced");
        foreach (var p in Pages(ledger, LedgerState.Freed))
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

    /// <summary>The fence's end state (2.2.1.12 R6): both committed slots and page 2 name the adopted state — the
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
