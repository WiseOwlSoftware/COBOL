// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⭐ The per-PHYSICAL-FILE record stores (kb/Work PB143; ISO §14.9.10.4 GR5 — "the identified record has been
/// logically removed from THE PHYSICAL FILE and can no longer be accessed"). Every keyed connector used to load
/// a PRIVATE snapshot at OPEN and persist its WHOLE view at CLOSE, so a record DELETEd through one connector
/// stayed readable through another over the same host path, and the CLOSE ORDER decided which view survived on
/// disk — silent undeletion / silent data loss. Now the record images live HERE, keyed by resolved host path
/// (the same key the <see cref="PhysicalFileTable"/> arbitrates sharing and locks by): the FIRST opener loads
/// from disk, later openers ATTACH to the live store (never reload — the in-memory store IS the truth while any
/// connector holds it), every mutation is instantly visible to every attached connector, any CLOSE persists the
/// ONE shared state (order no longer matters), and the LAST detach drops the entry so a later OPEN re-reads the
/// disk. Position/key state (FPI, key of reference, sequential-WRITE slot, GR38 high-key) stays per-CONNECTOR.
/// Two SELECTs to one ASSIGN target need no SHARING clause to reach this, so the store is unconditional for the
/// keyed organizations; sequential connectors keep their OS-backed streams (the file system is their shared
/// store). Owned by the <see cref="FileRegistry"/>; cleared at run-unit Reset.
/// </summary>
internal sealed class KeyedStoreTable
{
    private sealed class Entry
    {
        public required object Store;
        public int Attached;
    }

    private readonly Dictionary<string, Entry> _byHost = new(HostFile.PhysicalFileComparer);   // kb/Work PB2748

    /// <summary>Attach to the RELATIVE store for <paramref name="host"/>: the live store when one exists (its tree is
    /// the truth — nothing is re-read), else a new store with no file, which the attaching connector opens through its
    /// own handle (<see cref="RelativeStore.Open"/>; a failed OPEN detaches again).</summary>
    public RelativeStore AttachRelative(string host) => Attach(host, () => new RelativeStore());

    /// <summary>Attach to the INDEXED store for <paramref name="host"/> — same contract.</summary>
    public IndexedStore AttachIndexed(string host, Action<IndexedStore> loadFirst)
        => Attach(host, () => { var s = new IndexedStore(); loadFirst(s); return s; });

    /// <summary>⛔ ATTACH TO A STORE THAT IS BEING CREATED — <c>OPEN OUTPUT</c>, §14.9.27.4 GR18: a new, EMPTY store
    /// for <paramref name="host"/> that no other connector of this run unit holds (kb/Work PB754).
    /// <para>Emptying a store is a mutation every attached connector would see instantly (kb/Work PB143), so it is
    /// only ever done HERE, where the precondition that makes it safe is checked rather than remembered: Table 19
    /// refuses every OUTPUT open beside any other open connector — §9.1.13.9 1) e), <i>"An attempt is made to open a
    /// physical file in the output mode and the physical file is currently open by another file connector"</i> —
    /// and <c>FileRegistry.OpenShared</c> runs that arbitration BEFORE the connector's OPEN body. An entry that
    /// still exists here therefore means the arbitration was bypassed, and the answer is a loud defect, never a
    /// silent emptying of records another connector is reading (the analogous impossible state in
    /// <see cref="Attach{T}"/> is answered the same way).</para></summary>
    public T AttachCreated<T>(string host) where T : KeyedStore, new()
    {
        if (_byHost.TryGetValue(host, out var e))
            throw new InvalidOperationException(
                $"physical file '{host}' is being created by OPEN OUTPUT while {e.Attached} other connector(s) of this "
                + "run unit hold it open; Table 19 refuses that OPEN (§9.1.13.9 1) e)), so reaching here is an "
                + "arbitration defect (kb/Work PB754)");
        var store = new T();
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    private T Attach<T>(string host, Func<T> create) where T : KeyedStore
    {
        if (_byHost.TryGetValue(host, out var e))
        {
            if (e.Store is not T live)
                throw new InvalidOperationException(
                    $"physical file '{host}' is open under two different organizations ({e.Store.GetType().Name} vs {typeof(T).Name}) — a compiler/registration defect (kb/Work PB143)");
            e.Attached++;
            return live;
        }
        var store = create();
        _byHost[host] = new Entry { Store = store, Attached = 1 };
        return store;
    }

    /// <summary>Detach one connector from <paramref name="host"/>'s store; the LAST detach drops the entry so a
    /// later OPEN reloads from disk. Unbalanced detaches are ignored (a failed OPEN never attached).</summary>
    public void Detach(string host)
    {
        if (!_byHost.TryGetValue(host, out var e)) return;
        if (--e.Attached <= 0) _byHost.Remove(host);
    }

    /// <summary>Run-unit start hygiene.</summary>
    public void Clear() => _byHost.Clear();
}

/// <summary>⭐ What every keyed record store of a run unit knows about the CROSS-RUN-UNIT MUTEX of its physical file
/// (kb/Work PB2660; ISO §9.1.15, <i>"Multiple paths of access may exist in the same runtime element, contained elements,
/// separate runtime elements within the same run unit, or runtime elements in different run units"</i>): whether a
/// statement of this run unit is already inside it (<see cref="StatementDepth"/>) and through which endpoint it is held
/// (<see cref="MutexHolder"/>). Within the run unit the store IS the truth (kb/Work PB143); across run units the file
/// is, and each organization's store says how it follows the file (<see cref="RelativeStore"/> through the paged
/// engine's page 2, <see cref="IndexedStore"/> through its whole-store generation stamp).</summary>
internal abstract class KeyedStore
{
    /// <summary>How many statements of this run unit are inside the store's cross-run-unit mutex. A run unit is one
    /// thread of control, so a nested entry re-enters rather than deadlocking against itself.</summary>
    public int StatementDepth { get; set; }

    /// <summary>The endpoint holding the store's cross-run-unit mutex while <see cref="StatementDepth"/> is non-zero —
    /// null when no statement is inside it, or when the host cannot carry the lock.</summary>
    internal RegionLocks? MutexHolder { get; set; }
}

/// <summary>⭐ THE RUN UNIT'S RELATIVE STORE FOR ONE PHYSICAL FILE — the keyed store engine's handle on it
/// (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1, kb/Work PB3333): ONE paged store (<see cref="PagedFile"/>, its
/// page cache, its ledger) and ONE B+ tree keyed by the relative record number — 8 bytes big-endian, so the ordinal byte
/// order IS numeric order (2.2.1.4, <i>"A RELATIVE record is one entry"</i>) — whose value is the record's frame
/// (<see cref="RecordFraming.EncodeFrame"/>, the one codec). Every connector of the run unit over the path attaches to it
/// (<see cref="KeyedStoreTable"/>, kb/Work PB143), so a sibling's change is visible at once: there is one cache and one
/// tree. Nothing is loaded at OPEN and nothing is persisted at CLOSE: a statement reads the pages it touches and writes
/// the pages it changes, then page 2 (2.2.1.6).
/// <para>The sparse key space costs nothing (kb/Work PB1192): an empty slot is no entry, and a record at a key in the
/// billions is one leaf. Every positional question — the first record at or after a position, the last before it, the
/// highest record number — is a SEEK on the tree, O(log n) pages, where the map it replaced walked every key from the
/// lowest (kb/Work PB2755).</para></summary>
internal sealed class RelativeStore : KeyedStore
{
    /// <summary>Test seam, per async flow (the <c>IndexedConnector.HostFaultObserver</c> precedent): wraps the run unit's
    /// medium of every store made in the flow, so a case can refuse, tear or kill at any page write through a real
    /// connector (2.2.1.12's fault-injecting medium).</summary>
    internal static readonly AsyncLocal<Func<IPageMedium, IPageMedium>?> MediumObserver = new();

    private readonly RunUnitPageMedium _handles = new();
    private readonly IPageMedium _medium;
    private int _cachePages = PageCache.DefaultPages;
    private bool _coherent;

    public RelativeStore() => _medium = MediumObserver.Value?.Invoke(_handles) ?? _handles;

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
        if (File is null && _medium.Length == 0) return writable ? Create(attributes, coherent) : StoreStatus.Ok;
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
    /// handle that cannot.</summary>
    public void DetachHandle(SafeFileHandle handle)
    {
        _handles.Detach(handle);
        if (!_handles.HasHandle) { File = null; return; }
        if (File is { Writable: true } && !_handles.HasWritable
            && PagedFile.Open(_medium, writable: false, _coherent, out var reader, _cachePages) == StoreStatus.Ok)
            File = reader;
    }

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
        return status == StoreStatus.Ok ? Current(cursor, withValue, out rrn, out value) : status;
    }

    /// <summary>The lowest (<paramref name="last"/> false) or highest record number the file holds; null when none — the
    /// highest is §14.9.51.4 GR29 a)'s <i>"the highest relative record number existing in the physical file"</i>.</summary>
    public StoreStatus Edge(bool last, out long? rrn)
    {
        rrn = null;
        if (File is null) return StoreStatus.Ok;
        var tree = Tree();
        var status = last ? tree.Last(out var cursor) : tree.First(out cursor);
        return status == StoreStatus.Ok ? Current(cursor, withValue: false, out rrn, out _) : status;
    }

    private static StoreStatus Current(BTree.Cursor cursor, bool withValue, out long? rrn, out byte[]? value)
    {
        rrn = null;
        value = null;
        if (!cursor.HasCurrent) return StoreStatus.Ok;
        rrn = (long)BinaryPrimitives.ReadUInt64BigEndian(cursor.Key);
        return withValue ? cursor.ReadValue(out value!) : StoreStatus.Ok;
    }

    // ── Changes: one statement each ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Release record <paramref name="rrn"/> (<paramref name="done"/> false when it exists: nothing changes).</summary>
    public StoreStatus Insert(long rrn, byte[] value, bool entered, out bool done) =>
        Change(tree => (tree.Insert(Key(rrn), value, out bool d), d), entered, out done);

    /// <summary>Replace record <paramref name="rrn"/> (<paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Replace(long rrn, byte[] value, bool entered, out bool done) =>
        Change(tree => (tree.Replace(Key(rrn), value, out bool d), d), entered, out done);

    /// <summary>Remove record <paramref name="rrn"/> (§14.9.10.4 GR5, <i>"the identified record has been logically removed
    /// from the physical file and can no longer be accessed"</i>; <paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Delete(long rrn, bool entered, out bool done) =>
        Change(tree => (tree.Delete(Key(rrn), out bool d), d), entered, out done);

    /// <summary>⛔ ONE CHANGE IS ONE STATEMENT OF THE PAGE LAYER (2.2.1.6): the tree edited in memory, then
    /// <see cref="PagedFile.Begin"/> sized to exactly the pages the edit writes (its claim, its reservation), the edit
    /// flushed, the root set and the statement completed — page 2 last. <paramref name="entered"/> says the caller's
    /// coherent entry already read page 2 for this statement. A torn page or a refused write answers '30' and a
    /// boundary '24' (<see cref="StoreStatus.Boundary"/>), with page 2 naming the previous statement's tree (R1).</summary>
    private StoreStatus Change(Func<BTree, (StoreStatus Status, bool Done)> edit, bool entered, out bool done)
    {
        var file = File ?? throw new InvalidOperationException("a relative store with no file changes nothing");
        var tree = Tree();
        (var status, done) = edit(tree);
        if (status != StoreStatus.Ok || !done) return status;
        status = file.Begin(StatementBound.Of(tree), out var statement, entered: entered);
        if (status != StoreStatus.Ok)
        {
            tree.Discard();
            done = false;
            return status;
        }
        var flushed = tree.Flush(statement!, out var root);
        statement!.SetPrimeRoot(root);
        var completed = statement.Complete();
        if (flushed == StoreStatus.Ok && completed == StoreStatus.Ok) return StoreStatus.Ok;
        done = false;
        return completed == StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    private BTree Tree() => new(File!, KeyOrders.Ordinal, File!.Meta.PrimeRoot);

    private static byte[] Key(long rrn) => KeyOrders.RecordNumberKey((ulong)rrn);
}

/// <summary>One stored indexed record: its character image and its PER-KEY release ordinals — lifted out of
/// <see cref="IndexedConnector"/> when the store became shared (kb/Work PB143).
/// <para>⛔ THE RELEASE ORDINAL IS PER KEY OF REFERENCE, NOT PER RECORD (kb/Work PB341). ISO §14.9.30.4 GR26
/// names the retrieval order of duplicates under "an alternate record key that IS THE KEY OF REFERENCE", and
/// §14.9.35.4 GR24 a) — "When the value of a specific alternate record key is not changed, the order of
/// retrieval when that key is the key of reference remains unchanged" — makes each key's order independent of
/// every other key's: a REWRITE repositions the record ONLY in the duplicate sets of the keys it actually
/// changed (GR24 b). One number per record could not express that, so a REWRITE that changed one alternate key
/// silently reordered every OTHER alternate key's duplicate sequence. <see cref="Ordinals"/> is therefore a
/// VECTOR: slot 0 the prime key (assigned once at release and never re-stamped — a prime key value cannot
/// change, §14.9.35.4 GR22/GR23 identify the record BY it — so it doubles as the record's release order in the
/// physical file), slot <c>i + 1</c> the i-th alternate key.</para></summary>
internal sealed class KeyedRec
{
    public string Image = "";
    /// <summary>The EXTENT TABLE the record was released with (determination D-FRA (v); kb/Work PB1053) — null when
    /// the record carries none. Replaced with <see cref="Image"/>, never apart from it.</summary>
    public RecordExtents? Extents;
    /// <summary>The record's release ordinal under each key — slot 0 the prime key, slot <c>i + 1</c> the i-th alternate
    /// (§14.9.30.4 GR26, §14.9.35.4 GR24; kb/Work PB341) — persisted with the record (kb/Work PB2693). A change of its
    /// LENGTH goes through <see cref="IndexedStore.GrowOrdinals"/>, which counts the bytes.</summary>
    public long[] Ordinals = [];
}

/// <summary>The shared INDEXED record store: the records plus the release-ordinal mint — shared so a WRITE
/// through one connector takes the next GLOBAL ordinal and §14.9.30.4 GR26's duplicate-alternate retrieval
/// order holds across connectors.</summary>
internal sealed class IndexedStore : KeyedStore
{
    private readonly List<KeyedRec> _recs = [];

    /// <summary>Bumped by EVERY change to the records — the mutators are the only writers. With
    /// <see cref="PersistedVersion"/>, <see cref="Generation"/> and <see cref="VersionAtEntry"/>, what the WHOLE STORE
    /// knows about its agreement with the physical file (kb/Work PB2660): this run unit's copy of the file, trusted while
    /// the file's generation stamp is the one it last loaded or persisted. Deleted with the whole-store model when the
    /// organization moves to the keyed store engine (kb/Work PB3334, PB3335).</summary>
    public long Version { get; private set; }

    /// <summary>The <see cref="Version"/> at which the store last agreed with the physical file: set by the load
    /// and by every persist, so <c>Version != PersistedVersion</c> is exactly "this run unit holds records the
    /// file does not".</summary>
    public long PersistedVersion { get; set; }

    /// <summary>The generation stamp of the physical file image the store last loaded or persisted.</summary>
    public ulong Generation { get; set; }

    /// <summary>The <see cref="Version"/> when the outermost statement entered — so the statement persists only
    /// what IT changed.</summary>
    public long VersionAtEntry { get; set; }

    /// <summary>Record that the records changed — called by every mutator, and by nothing else.</summary>
    private void Mutated() => Version++;

    /// <summary>The records, in load order — the persisted order. READ-ONLY to the connector: every mutation goes through <see cref="Add"/>, <see cref="Replace"/>,
    /// <see cref="Remove"/> or <see cref="Clear"/>, which keep <see cref="RecordBytes"/> true (kb/Work PB1192).</summary>
    public IReadOnlyList<KeyedRec> Recs => _recs;

    public long NextOrdinal = 1;

    /// <summary>The bytes the records' frames occupy in the persisted store (<see cref="RecordFraming.FrameBytes"/>
    /// summed) — maintained by the mutators, so §14.9.51.4 GR42 d)'s boundary test at the WRITE is O(1)
    /// (Annex A.1 item 107).</summary>
    public long RecordBytes { get; private set; }

    /// <summary>The frame bytes of one stored record — <see cref="RecordFraming.FrameBytes(int, RecordExtents?, int)"/>
    /// of its image, its extent table and its <paramref name="ordinalSlots"/> release ordinals (kb/Work PB2693).</summary>
    public static long FrameBytes(string image, RecordExtents? extents, int ordinalSlots) =>
        RecordFraming.FrameBytes(image.Length, extents, ordinalSlots);

    /// <summary>Grow a record's release-ordinal vector to <paramref name="slots"/> — a connector of this run unit that
    /// declares keys the record predates re-stamps one (§14.9.35.4 GR24 b)) — keeping <see cref="RecordBytes"/>
    /// true. The new slots read 0, as a short vector's missing ones do.</summary>
    public void GrowOrdinals(KeyedRec rec, int slots)
    {
        if (rec.Ordinals.Length >= slots) return;
        RecordBytes += FrameBytes(rec.Image, rec.Extents, slots) - FrameBytes(rec.Image, rec.Extents, rec.Ordinals.Length);
        Array.Resize(ref rec.Ordinals, slots);
    }


    /// <summary>Release a record into the store (a WRITE, or the OPEN's load).</summary>
    public void Add(KeyedRec rec)
    {
        Mutated();
        _recs.Add(rec);
        RecordBytes += FrameBytes(rec.Image, rec.Extents, rec.Ordinals.Length);
    }

    /// <summary>Replace a stored record's content in place (§14.9.35 REWRITE) — its image and extent table
    /// together, never apart (D-FRA (v)).</summary>
    public void Replace(KeyedRec rec, string image, RecordExtents? extents)
    {
        RecordBytes += FrameBytes(image, extents, rec.Ordinals.Length) - FrameBytes(rec.Image, rec.Extents, rec.Ordinals.Length);
        Mutated();
        rec.Image = image;
        rec.Extents = extents;
    }

    /// <summary>Remove a record (§14.9.10 DELETE); false when it was not in the store.</summary>
    public bool Remove(KeyedRec rec)
    {
        if (!_recs.Remove(rec)) return false;
        Mutated();
        RecordBytes -= FrameBytes(rec.Image, rec.Extents, rec.Ordinals.Length);
        return true;
    }

    /// <summary>Empty the store (OPEN OUTPUT, the absent-OPTIONAL creation, and the OPEN's reload).</summary>
    public void Clear()
    {
        Mutated();
        _recs.Clear();
        RecordBytes = 0;
    }
}
