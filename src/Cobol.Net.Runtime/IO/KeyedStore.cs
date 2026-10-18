// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>⭐ THE RUN UNIT'S HANDLE ON ONE PHYSICAL FILE'S KEYED STORE ENGINE
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1, kb/Work PB3333, PB3334): ONE paged store (<see cref="PagedFile"/>, its page cache, its ledger) opened through the
/// handles of the run unit's connectors on the file (<see cref="RunUnitPageMedium"/>), and its trees, which the
/// organization's view names (<see cref="RelativeStore"/>: one tree keyed by the relative record number;
/// <see cref="IndexedStore"/>: the prime tree and one tree per alternate key). Every connector of the run unit over the path
/// attaches to it (<see cref="StoreTable"/>, kb/Work PB143), so a sibling's change is visible at once. Nothing is
/// loaded at OPEN and nothing is persisted at CLOSE: a statement reads the pages it touches and writes the pages it
/// changes, then page 2 (2.2.1.6), in ONE statement of the page layer however many trees it changes
/// (<see cref="Change"/>).
/// <para>It also carries what the run unit knows about the CROSS-RUN-UNIT MUTEX of its physical file (kb/Work PB2660;
/// ISO §9.1.15, <i>"Multiple paths of access may exist in the same runtime element, contained elements, separate runtime
/// elements within the same run unit, or runtime elements in different run units"</i>): whether a statement of this run
/// unit is already inside it (<see cref="StatementDepth"/>) and through which endpoint it is held
/// (<see cref="MutexHolder"/>). Within the run unit the store IS the truth (kb/Work PB143); across run units the file is,
/// and the store follows it through page 2 (2.2.1.7).</para></summary>
internal abstract class KeyedStore
{
    /// <summary>Test seam, per async flow (the <see cref="FileRegistry.PauseObserver"/> precedent): wraps the run unit's
    /// medium of every store made in the flow, so a case can refuse, tear or kill at any page write through a real
    /// connector of either organization (2.2.1.12's fault-injecting medium).</summary>
    internal static readonly AsyncLocal<Func<IPageMedium, IPageMedium>?> MediumObserver = new();

    private readonly RunUnitPageMedium _handles = new();
    private readonly IPageMedium _medium;
    private int _cachePages = PageCache.DefaultPages;
    private bool _coherent;

    protected KeyedStore() => _medium = MediumObserver.Value?.Invoke(_handles) ?? _handles;

    /// <summary>How many statements of this run unit are inside the store's cross-run-unit mutex. A run unit is one
    /// thread of control, so a nested entry re-enters rather than deadlocking against itself.</summary>
    public int StatementDepth { get; set; }

    /// <summary>The endpoint holding the store's cross-run-unit mutex while <see cref="StatementDepth"/> is non-zero —
    /// null when no statement is inside it, or when the host cannot carry the lock.</summary>
    internal RegionLocks? MutexHolder { get; set; }

    /// <summary>The paged store, or null while the file holds no store: an absent OPTIONAL file opened INPUT, or a present
    /// file of no bytes opened INPUT (both hold no records, §14.9.27.4 GR13).</summary>
    public PagedFile? File { get; private set; }

    /// <summary>Attach a connector's handle (its §9.1.15 file lock) and its input-output areas, the page cache's floor
    /// (§12.4.5.14.3 GR1: <i>"If the RESERVE clause is specified, the number of input-output areas allocated is equal to
    /// the value of integer-1"</i>; 2.2.1.13).</summary>
    public void AttachHandle(SafeFileHandle handle, bool writable, int areaPages)
    {
        _handles.Attach(handle, writable);
        _cachePages = Math.Max(_cachePages, areaPages);
        File?.ReserveCache(areaPages);
    }

    /// <summary>The OPEN's adoption rule (2.2.1.6 R6), run by the caller under the store mutex. A live store is the truth
    /// for every connector of the run unit (kb/Work PB143) and is not re-read — unless it was opened through no writable
    /// handle and this connector's is writable, when the store is opened again through it, which FENCES as every
    /// writable OPEN does. A file of no bytes holds no store yet: a writable connector creates the empty one (three meta
    /// pages, §14.9.27.4 GR17's creation), a reader reads no records.</summary>
    public StoreStatus Open(bool writable, bool coherent, FixedFileAttributes attributes)
    {
        _coherent = coherent;
        if (File is { } live && (live.Writable || !writable)) return StoreStatus.Ok;
        if (File is null && _medium.PageCount == 0) return writable ? Create(attributes, coherent) : StoreStatus.Ok;
        var status = PagedFile.Open(_medium, writable, coherent, out var opened, _cachePages);
        if (status == StoreStatus.Ok) File = opened;
        return status;
    }

    /// <summary><c>OPEN OUTPUT</c>'s creation (§14.9.27.4 GR18, <i>"After the creation of the file, the file contains no
    /// records"</i>), and the creation of a file of no bytes: <see cref="PagedFile.Create"/> through the connector's own
    /// handle, the one truncation.</summary>
    public StoreStatus Create(FixedFileAttributes attributes, bool coherent)
    {
        _coherent = coherent;
        File = null;
        var status = PagedFile.Create(_medium, attributes, coherent, out var created, _cachePages);
        if (status == StoreStatus.Ok) File = created;
        return status;
    }

    /// <summary>Detach a connector's handle. The last detach leaves the store with no file; a store left with readers
    /// only is opened again as a reader's (R6 in memory, nothing written), so no reader's statement writes through a
    /// handle that cannot. The caller holds the store mutex (2.2.1.7). Returns the reader's adoption status: a refused
    /// read leaves the store on the meta it holds, which names the last completed statement's trees, and a reader's
    /// statement never writes (<see cref="PagedFile.Enter"/>'s reader entry).</summary>
    public StoreStatus DetachHandle(SafeFileHandle handle)
    {
        _handles.Detach(handle);
        if (!_handles.HasHandle) { File = null; return StoreStatus.Ok; }
        if (File is not { Writable: true } || _handles.HasWritable) return StoreStatus.Ok;
        var status = PagedFile.Open(_medium, writable: false, _coherent, out var reader, _cachePages);
        if (status == StoreStatus.Ok) File = reader;
        return status;
    }

    /// <summary>The trees one statement edited, each with the root it sets in the meta: <see cref="PrimeRoot"/> for the
    /// prime (relative: record-number) tree, an alternate key's index for its tree.</summary>
    internal readonly record struct TreeChange(BTree Tree, int Root)
    {
        /// <summary>The <see cref="Root"/> of the prime (relative: record-number) tree.</summary>
        public const int PrimeRoot = -1;
    }

    /// <summary>What a statement's in-memory edit did: the page layer's status, whether the change applies (false: the
    /// tree declined it — a key already present, a key absent — and nothing is written), and every tree it edited.</summary>
    internal readonly record struct Edit(StoreStatus Status, bool Done, IReadOnlyList<TreeChange> Trees);

    /// <summary>⛔ ONE CHANGE IS ONE STATEMENT OF THE PAGE LAYER, HOWEVER MANY TREES IT CHANGES (2.2.1.6; 2.2.1.4, <i>"An
    /// INDEXED record is n + 1 entries, one per tree"</i>). The statement's entry first, unless the caller's coherent entry
    /// already read page 2 for it (<paramref name="entered"/>), so every tree the edit builds is rooted in the file as it is
    /// now; then <paramref name="edit"/> edits the trees in memory, handed the file and the first release ordinal the
    /// statement mints (<see cref="PagedFile.ReleaseMint"/>; <paramref name="ordinals"/> of them, §14.9.51.4 GR40); then
    /// <see cref="PagedFile.Begin"/> sized to exactly the pages the edits write (its claim, its reservation), every edit
    /// flushed, every root set and the statement completed — page 2 last. A torn page or a refused write answers '30' and
    /// a boundary '24' (<see cref="StoreStatus.Boundary"/>), with page 2 naming the previous statement's trees (R1) and
    /// every pending edit discarded.</summary>
    private protected StoreStatus Change(bool entered, int ordinals, Func<PagedFile, ulong, Edit> edit, out bool done)
    {
        done = false;
        var file = File ?? throw new InvalidOperationException("a keyed store with no file changes nothing");
        if (!entered && file.Enter() is not StoreStatus.Ok and var refusedEntry) return refusedEntry;
        var change = edit(file, file.ReleaseMint);
        if (change.Status != StoreStatus.Ok || !change.Done)
        {
            Discard(change.Trees);
            return change.Status;
        }
        int pages = 0;
        foreach (var t in change.Trees) pages += t.Tree.PagesToWrite;
        var status = file.Begin(pages, out var statement, ordinals: ordinals, entered: true);
        if (status != StoreStatus.Ok)
        {
            Discard(change.Trees);
            return status;
        }
        var flushed = StoreStatus.Ok;
        foreach (var (tree, root) in change.Trees)
        {
            if (flushed != StoreStatus.Ok) { tree.Discard(); continue; }
            flushed = tree.Flush(statement!, out var pointer);
            if (flushed != StoreStatus.Ok) continue;
            if (root == TreeChange.PrimeRoot) statement!.SetPrimeRoot(pointer);
            else statement!.SetAlternateRoot(root, pointer);
        }
        var completed = statement!.Complete();
        if (flushed == StoreStatus.Ok && completed == StoreStatus.Ok)
        {
            done = true;
            return StoreStatus.Ok;
        }
        return completed == StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    private static void Discard(IReadOnlyList<TreeChange> trees)
    {
        foreach (var t in trees) t.Tree.Discard();
    }

    /// <summary>The record a cursor stands on, in two parts: its key and, when <paramref name="withValue"/>, its value
    /// bytes (null with no current entry).</summary>
    private protected static StoreStatus Current(BTree.Cursor cursor, bool withValue, out byte[]? key, out byte[]? value)
    {
        key = null;
        value = null;
        if (!cursor.HasCurrent) return StoreStatus.Ok;
        key = cursor.Key;
        if (!withValue) return StoreStatus.Ok;
        var status = cursor.ReadValue(out var read);
        value = read;
        return status;
    }
}

/// <summary>⭐ THE RUN UNIT'S RELATIVE STORE FOR ONE PHYSICAL FILE — the keyed store engine's view of it
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1, kb/Work PB3333): ONE B+ tree keyed by the relative record
/// number — 8 bytes big-endian, so the ordinal byte order IS numeric order (2.2.1.4, <i>"A RELATIVE record is one
/// entry"</i>) — whose value is the record's frame (<see cref="RecordFraming.EncodeFrame"/>, the one codec).
/// <para>The sparse key space costs nothing (kb/Work PB1192): an empty slot is no entry, and a record at a key in the
/// billions is one leaf. Every positional question — the first record at or after a position, the last before it, the
/// highest record number — is a SEEK on the tree, O(log n) pages, where the map it replaced walked every key from the
/// lowest (kb/Work PB2755).</para></summary>
internal sealed class RelativeStore : KeyedStore
{
    // ── Reads ───────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The frame bytes of record <paramref name="rrn"/>; null when the file holds none.</summary>
    public StoreStatus Get(long rrn, out byte[]? value)
    {
        value = null;
        if (File is null || rrn < 1) return StoreStatus.Ok;
        return Tree().TryGet(Key(rrn), out value);
    }

    /// <summary>The record the SEEK <paramref name="relation"/> <paramref name="probe"/> finds (a probe below 1 seeks from
    /// 0, below every record number): its number, and its frame bytes when <paramref name="withValue"/>; null when no
    /// record satisfies it.</summary>
    public StoreStatus Seek(long probe, SeekRelation relation, bool withValue, out long? rrn, out byte[]? value)
    {
        rrn = null;
        value = null;
        if (File is null) return StoreStatus.Ok;
        var status = Tree().Seek(Key(Math.Max(probe, 0)), relation, out var cursor);
        return status == StoreStatus.Ok ? Number(cursor, withValue, out rrn, out value) : status;
    }

    /// <summary>The lowest (<paramref name="last"/> false) or highest record number the file holds; null when none — the
    /// highest is §14.9.51.4 GR29 a)'s <i>"the highest relative record number existing in the physical file"</i>.</summary>
    public StoreStatus Edge(bool last, out long? rrn)
    {
        rrn = null;
        if (File is null) return StoreStatus.Ok;
        var tree = Tree();
        var status = last ? tree.Last(out var cursor) : tree.First(out cursor);
        return status == StoreStatus.Ok ? Number(cursor, withValue: false, out rrn, out _) : status;
    }

    private static StoreStatus Number(BTree.Cursor cursor, bool withValue, out long? rrn, out byte[]? value)
    {
        var status = Current(cursor, withValue, out var key, out value);
        rrn = key is null ? null : (long)BinaryPrimitives.ReadUInt64BigEndian(key);
        return status;
    }

    // ── Changes: one statement each ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Release record <paramref name="rrn"/> (<paramref name="done"/> false when it exists: nothing changes).</summary>
    public StoreStatus Insert(long rrn, byte[] value, bool entered, out bool done) =>
        OneTree(tree => (tree.Insert(Key(rrn), value, out bool d), d), entered, out done);

    /// <summary>Replace record <paramref name="rrn"/> (<paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Replace(long rrn, byte[] value, bool entered, out bool done) =>
        OneTree(tree => (tree.Replace(Key(rrn), value, out bool d), d), entered, out done);

    /// <summary>Remove record <paramref name="rrn"/> (§14.9.10.4 GR5, <i>"the identified record has been logically removed
    /// from the physical file and can no longer be accessed"</i>; <paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Delete(long rrn, bool entered, out bool done) =>
        OneTree(tree => (tree.Delete(Key(rrn), out bool d), d), entered, out done);

    private StoreStatus OneTree(Func<BTree, (StoreStatus Status, bool Done)> edit, bool entered, out bool done) =>
        Change(entered, ordinals: 0, (_, _) =>
        {
            var tree = Tree();
            var (status, applied) = edit(tree);
            return new Edit(status, applied, [new TreeChange(tree, TreeChange.PrimeRoot)]);
        }, out done);

    private BTree Tree() => new(File!, KeyOrders.Ordinal, File!.Meta.PrimeRoot);

    private static byte[] Key(long rrn) => KeyOrders.RecordNumberKey((ulong)rrn);
}

/// <summary>The key orders an INDEXED connector's trees are searched and changed under, chosen once at its OPEN
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, <i>"Keys are BYTES in the file's channel character set"</i>):
/// the prime key's order, and per alternate key its own order (<see cref="Alternates"/>, which decides equality — its
/// SUPPRESS WHEN value, a changed key, a duplicate) and the COMPOSITE order of its tree's entries (<see cref="Trees"/>:
/// the key's order, then the release ordinal).</summary>
internal sealed record IndexedKeyOrders(IKeyOrder Prime, IKeyOrder[] Alternates, IKeyOrder[] Trees);

/// <summary>⭐ THE RUN UNIT'S INDEXED STORE FOR ONE PHYSICAL FILE — the keyed store engine's view of it
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.3 decision 3 and 2.2.1.4, kb/Work PB3334): the PRIME tree (the
/// prime key's channel bytes → the record's frame, which carries its ordinal vector) at root[0], and per alternate key a
/// COMPOSITE tree ((the alternate key's bytes, its 8-byte big-endian release ordinal) → the prime key's bytes) whose root
/// follows that key's descriptor in the meta page. A record is n + 1 entries, one per tree — none in an alternate tree
/// whose key equals its SUPPRESS WHEN value (§12.4.5.6.4 GR6) — and every change to them is ONE statement of the page
/// layer (<see cref="KeyedStore.Change"/>). The release mint (§14.9.51.4 GR40) is the meta page's
/// (<see cref="PagedFile.ReleaseMint"/>), so a WRITE in either run unit takes the next ordinal of the FILE.
/// <para>⛔ THE FILE'S KEY TABLE IS THE TREES' (DOC-A.1-129): a store has exactly one alternate tree per alternate key its
/// meta page records (<see cref="AlternateTrees"/>) and every frame carries one ordinal per recorded key
/// (<see cref="FrameSlots"/>). Under the <c>COBOLNET_KEYCHECK</c> opt-out a connector whose description declares a
/// different key table drives these trees by key NUMBER: a declared alternate key with no tree provides no access path,
/// and a tree the description does not declare is not maintained by that connector's statements.</para></summary>
internal sealed class IndexedStore : KeyedStore
{
    /// <summary>The alternate trees the file holds — one per alternate key its meta page records.</summary>
    public int AlternateTrees => File?.Meta.AlternateRoots.Count ?? 0;

    /// <summary>The release ordinals each record's frame carries: one per key the meta page records (the prime key's
    /// first, §14.9.30.4 GR26, §14.9.35.4 GR24; kb/Work PB341).</summary>
    public int FrameSlots => File?.Meta.Attributes.Keys.Count ?? 1;

    /// <summary>The tree of key <paramref name="key"/> (<see cref="TreeChange.PrimeRoot"/> the prime key, else an
    /// alternate key's index) under <paramref name="orders"/>; null when the file holds no store, or no such tree.</summary>
    public BTree? Tree(int key, IndexedKeyOrders orders)
    {
        if (File is not { } file) return null;
        if (key == TreeChange.PrimeRoot) return new BTree(file, orders.Prime, file.Meta.PrimeRoot);
        return key < file.Meta.AlternateRoots.Count ? new BTree(file, orders.Trees[key], file.Meta.AlternateRoots[key]) : null;
    }

    /// <summary>The frame of the record whose prime key is <paramref name="prime"/>; null when the file holds none.</summary>
    public StoreStatus Get(IndexedKeyOrders orders, ReadOnlySpan<byte> prime, out byte[]? frame)
    {
        frame = null;
        return Tree(TreeChange.PrimeRoot, orders) is { } tree ? tree.TryGet(prime, out frame) : StoreStatus.Ok;
    }

    /// <summary>A cursor of tree <paramref name="key"/> positioned by <paramref name="relation"/> against
    /// <paramref name="probe"/> — with <paramref name="prefix"/> the leading positions of each entry's KEY PART (a START's
    /// partial key, §14.9.41.4 GR17 c) to e); an alternate tree's whole key value, which leaves out the ordinal) — or null
    /// when there is no such tree.</summary>
    public StoreStatus Seek(int key, IndexedKeyOrders orders, ReadOnlySpan<byte> probe, SeekRelation relation, bool prefix,
        out BTree.Cursor? cursor)
    {
        cursor = null;
        if (Tree(key, orders) is not { } tree) return StoreStatus.Ok;
        var status = tree.Seek(probe, relation, out var c, prefix);
        cursor = c;
        return status;
    }

    /// <summary>A cursor on the first (<paramref name="last"/> false) or last entry of tree <paramref name="key"/>; null
    /// when there is no such tree.</summary>
    public StoreStatus Edge(int key, IndexedKeyOrders orders, bool last, out BTree.Cursor? cursor)
    {
        cursor = null;
        if (Tree(key, orders) is not { } tree) return StoreStatus.Ok;
        var status = last ? tree.Last(out var c) : tree.First(out c);
        cursor = c;
        return status;
    }

    /// <summary>The key part and the release ordinal of an alternate tree's entry key (2.2.1.4).</summary>
    public static (byte[] KeyPart, ulong Ordinal) Split(byte[] entry) =>
        (entry[..^CompositeKeyOrder.OrdinalBytes],
         BinaryPrimitives.ReadUInt64BigEndian(entry.AsSpan(entry.Length - CompositeKeyOrder.OrdinalBytes)));

    // ── Changes: one statement each, n + 1 entries ──────────────────────────────────────────────────────────────

    /// <summary>⛔ RELEASE a record (§14.9.51.4 GR40): its frame under its prime key and, for every alternate key with a
    /// value in <paramref name="alternates"/> (null: the key's SUPPRESS WHEN value, §12.4.5.6.4 GR6, or a key the file has
    /// no tree for), the entry (that value, the ONE fresh ordinal) → the prime key — n + 1 inserts in one statement, the
    /// ordinal minted with it. <paramref name="frameOf"/> builds the frame from the ordinal; <paramref name="done"/> is
    /// false when the prime key is present (nothing changes).</summary>
    public StoreStatus Release(bool entered, IndexedKeyOrders orders, byte[] prime, Func<ulong, byte[]> frameOf,
        IReadOnlyList<byte[]?> alternates, out bool done) =>
        Change(entered, ordinals: 1, (_, ordinal) =>
        {
            var trees = new List<TreeChange>(alternates.Count + 1);
            var primeTree = Tree(TreeChange.PrimeRoot, orders)!;
            trees.Add(new TreeChange(primeTree, TreeChange.PrimeRoot));
            var status = primeTree.Insert(prime, frameOf(ordinal), out bool inserted);
            if (status != StoreStatus.Ok || !inserted) return new Edit(status, false, trees);
            for (int i = 0; i < alternates.Count && i < AlternateTrees; i++)
            {
                if (alternates[i] is not { } value) continue;
                var tree = Tree(i, orders)!;
                trees.Add(new TreeChange(tree, i));
                status = tree.Insert(CompositeKeyOrder.Compose(value, ordinal), prime, out bool _);
                if (status != StoreStatus.Ok) return new Edit(status, false, trees);
            }
            return new Edit(StoreStatus.Ok, true, trees);
        }, out done);

    /// <summary>⛔ REPLACE a record (§14.9.35.4 GR24): its prime entry's frame and, per alternate key the REWRITE changed
    /// (<paramref name="moves"/>), the old entry deleted (null: there was none — the old value was suppressed) and the new
    /// one inserted under the ONE fresh ordinal when <paramref name="ordinals"/> mints it (null: the new value is
    /// suppressed). A key the REWRITE did not change keeps its entry and its place among its duplicates (GR24 a)) because
    /// nothing touches its tree. <paramref name="done"/> is false when the record does not exist.</summary>
    public StoreStatus Replace(bool entered, IndexedKeyOrders orders, byte[] prime, Func<ulong, byte[]> frameOf, int ordinals,
        IReadOnlyList<(int Key, byte[]? Old, byte[]? New)> moves, out bool done) =>
        Change(entered, ordinals, (_, ordinal) =>
        {
            var trees = new List<TreeChange>(moves.Count + 1);
            var primeTree = Tree(TreeChange.PrimeRoot, orders)!;
            trees.Add(new TreeChange(primeTree, TreeChange.PrimeRoot));
            var status = primeTree.Replace(prime, frameOf(ordinal), out bool replaced);
            if (status != StoreStatus.Ok || !replaced) return new Edit(status, false, trees);
            foreach (var (key, old, now) in moves)
            {
                if (key >= AlternateTrees) continue;
                var tree = Tree(key, orders)!;
                trees.Add(new TreeChange(tree, key));
                if (old is not null && (status = tree.Delete(old, out bool _)) != StoreStatus.Ok) return new Edit(status, false, trees);
                if (now is not null
                    && (status = tree.Insert(CompositeKeyOrder.Compose(now, ordinal), prime, out bool _)) != StoreStatus.Ok)
                    return new Edit(status, false, trees);
            }
            return new Edit(StoreStatus.Ok, true, trees);
        }, out done);

    /// <summary>⛔ REMOVE a record (§14.9.10.4 GR5): its prime entry and every alternate entry it has
    /// (<paramref name="entries"/>, each an alternate tree's whole entry key) — n + 1 deletes in one statement.
    /// <paramref name="done"/> is false when the record does not exist.</summary>
    public StoreStatus Remove(bool entered, IndexedKeyOrders orders, byte[] prime, IReadOnlyList<(int Key, byte[] Entry)> entries,
        out bool done) =>
        Change(entered, ordinals: 0, (_, _) =>
        {
            var trees = new List<TreeChange>(entries.Count + 1);
            var primeTree = Tree(TreeChange.PrimeRoot, orders)!;
            trees.Add(new TreeChange(primeTree, TreeChange.PrimeRoot));
            var status = primeTree.Delete(prime, out bool deleted);
            if (status != StoreStatus.Ok || !deleted) return new Edit(status, false, trees);
            foreach (var (key, entry) in entries)
            {
                if (key >= AlternateTrees) continue;
                var tree = Tree(key, orders)!;
                trees.Add(new TreeChange(tree, key));
                if ((status = tree.Delete(entry, out bool _)) != StoreStatus.Ok) return new Edit(status, false, trees);
            }
            return new Edit(StoreStatus.Ok, true, trees);
        }, out done);
}
