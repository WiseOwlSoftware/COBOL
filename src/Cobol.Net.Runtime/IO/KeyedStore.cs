// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>What the dead-owner probe of a transaction's liveness byte answers (<see cref="KeyedStore.OwnerOf"/>; 2.2.1.10,
/// kb/Work PB3549).</summary>
internal enum OwnerLiveness
{
    /// <summary>The byte is free: the owner gave it back, or died. Its transaction is the dead-owner step's to roll back.</summary>
    Dead,
    /// <summary>Another holder holds the byte: the owner lives.</summary>
    Alive,
    /// <summary>The host cannot carry the lock here, so nobody can say; read as alive.</summary>
    Unknowable,
}

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
    /// for every connector of the run unit (kb/Work PB143) and is not re-read — unless this connector's handle is writable
    /// and the store was opened through no writable handle, or its writable handles all detached and the reader's
    /// re-adoption was refused (<see cref="_fenceOwed"/>): then the store is opened again through it, which FENCES as
    /// every writable OPEN does. A file of no bytes holds no store yet: a writable connector creates the empty one (three
    /// meta pages, §14.9.27.4 GR17's creation), a reader reads no records.</summary>
    public StoreStatus Open(bool writable, bool coherent, FixedFileAttributes attributes, IKeyOrder holdingsOrder)
    {
        _coherent = coherent;
        if (File is { } live && (!writable || (live.Writable && !_fenceOwed))) return StoreStatus.Ok;
        if (File is null && _medium.Length == 0) return writable ? Create(attributes, coherent, holdingsOrder) : StoreStatus.Ok;
        var status = PagedFile.Open(_medium, writable, coherent, holdingsOrder, out var opened, _cachePages);
        if (status != StoreStatus.Ok) return status;
        File = opened;
        _fenceOwed = false;
        return status;
    }

    /// <summary><c>OPEN OUTPUT</c>'s creation (§14.9.27.4 GR18, <i>"After the creation of the file, the file contains no
    /// records"</i>), and the creation of a file of no bytes: <see cref="PagedFile.Create"/> through the connector's own
    /// handle, the one truncation.</summary>
    public StoreStatus Create(FixedFileAttributes attributes, bool coherent, IKeyOrder holdingsOrder)
    {
        _coherent = coherent;
        File = null;
        _fenceOwed = false;
        var status = PagedFile.Create(_medium, attributes, coherent, holdingsOrder, out var created, out bool replaced, _cachePages);
        File = created;
        FileReplaced = replaced;
        return status;
    }

    /// <summary>Whether the last <see cref="Create"/> left the physical file other than the previous one, successful or not
    /// (<see cref="PagedFile.Create"/>'s <c>replaced</c>): false only for a creation refused and undone, which leaves the
    /// previous file as it was (§14.9.27.4 GR25); true for one that landed, one whose final sync was refused after the
    /// truncation, and one whose undo the medium refused too — the previous store is gone in all three, so what was kept
    /// over it ends with it (<c>KeyedConnector.Recreate</c>; kb/Work PB3493, review finding R1-1).</summary>
    public bool FileReplaced { get; private set; }

    /// <summary>Detach a connector's handle. The last detach leaves the store with no file; a store left with readers
    /// only is opened again as a reader's (R6 in memory, nothing written), so no reader's statement writes through a
    /// handle that cannot. The caller holds the store mutex (2.2.1.7). Returns the reader's adoption status, which the
    /// detaching connector's CLOSE answers: a refused read ('30', 2.2.1.16's refused READ row) leaves the store on the
    /// meta it holds — the last completed statement's trees, which a reader's entry reads and never writes
    /// (<see cref="PagedFile.Enter"/>'s shared entry) — with its fence OWED (<see cref="_fenceOwed"/>), so the run unit's
    /// next writable OPEN opens it again and fences rather than taking it as a live writable store.</summary>
    public StoreStatus DetachHandle(SafeFileHandle handle)
    {
        _handles.Detach(handle);
        if (!_handles.HasHandle)
        {
            File = null;
            _fenceOwed = false;
            return StoreStatus.Ok;
        }
        if (File is not { Writable: true } writer || _handles.HasWritable) return StoreStatus.Ok;
        var status = PagedFile.Open(_medium, writable: false, _coherent, writer.HoldingsOrder, out var reader, _cachePages);
        if (status == StoreStatus.Ok) File = reader;
        else _fenceOwed = true;
        return status;
    }

    /// <summary>⛔ The store's <see cref="File"/> is WRITABLE but no writable handle has opened it since its last writable
    /// handle detached: the reader's re-adoption was refused (<see cref="DetachHandle"/>). The writable state names the
    /// last completed statement's trees, which the readers left keep reading, but no writable OPEN's fence (2.2.1.6 R6)
    /// stands behind it any more — another run unit may have died over the file since — so the next writable OPEN of the
    /// run unit opens the store again, which fences, and clears this.</summary>
    private bool _fenceOwed;

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
    /// every pending edit discarded.
    /// <para>⛔ UNDER COMMIT CONTROL THE SAME STATEMENT KEEPS THE BEFORE-IMAGE AND THE HOLDINGS
    /// (DESIGN-runtime-library.md 2.2.1.10; kb/Work PB3337): with <paramref name="work"/> the statement also inserts the
    /// record's before-image into the run unit's UNDO tree under (R, identity) unless an entry exists (the first image is
    /// the one to restore), a V entry for every unique alternate key value the change frees, and the same keys → the
    /// transaction id into the file's HOLDINGS tree, so the transaction's locks are pages of the file and outlive its
    /// handle. A first change DRAWS the transaction's id by taking its liveness byte (<see cref="Draw"/>) and claims it
    /// with the statement's other mints; a first change refused before its page 2 gives the byte back, since no entry
    /// carries the id (finding R4-2).</para></summary>
    private protected StoreStatus Change(bool entered, int ordinals, Func<PagedFile, ulong, Edit> edit, out bool done,
        TransactionWork? work = null)
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
        ulong drawn = 0;
        Holdings? holdings = null;
        if (work is not null)
        {
            if (Transaction == 0 && (drawn = Draw(file, work.Drawer)) == 0)
            {
                Discard(change.Trees);
                return StoreStatus.PermanentError;
            }
            if (Holdings.Of(file, Transaction == 0 ? drawn : Transaction, firstChange: drawn != 0, work, out holdings)
                is var held && held != StoreStatus.Ok)
            {
                Discard(change.Trees);
                GiveBack(work.Drawer, drawn);
                return held;
            }
        }
        var status = file.Begin(StatementBound.Of([.. change.Trees.Select(t => t.Tree), .. holdings?.Trees ?? []]),
            out var statement, ordinals: ordinals, drawnTransaction: drawn, entered: true);
        if (status != StoreStatus.Ok)
        {
            Discard(change.Trees);
            holdings?.Discard();
            GiveBack(work?.Drawer, drawn);
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
        if (holdings is not null)
        {
            if (flushed == StoreStatus.Ok) flushed = holdings.Flush(statement!);
            else holdings.Discard();
        }
        var completed = statement!.Complete();
        if (flushed == StoreStatus.Ok && completed == StoreStatus.Ok)
        {
            if (drawn != 0) (Transaction, _liveness) = (drawn, work!.Drawer);
            done = true;
            return StoreStatus.Ok;
        }
        GiveBack(work?.Drawer, drawn);
        return completed == StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    private static void Discard(IReadOnlyList<TreeChange> trees)
    {
        foreach (var t in trees) t.Tree.Discard();
    }

    // ── Transactions: APPLY COMMIT, COMMIT and ROLLBACK on the engine (2.2.1.10; kb/Work PB3337) ─────────────────────

    /// <summary>What a record statement through a connector under commit control adds to its page statement: the
    /// endpoint whose writable handle draws (and then holds) the transaction's liveness byte, the record's identity (the
    /// prime key's channel bytes, or the 8-byte relative record number), its BEFORE-IMAGE (null: the statement creates
    /// it), and the HOLDINGS keys of every unique alternate key value the change frees (<see cref="TransactionKeys.Value"/>).</summary>
    internal sealed record TransactionWork(RegionLocks Drawer, byte[] Identity, byte[]? BeforeImage, IReadOnlyList<byte[]> FreedValues);

    /// <summary>⛔ THIS RUN UNIT'S OPEN TRANSACTION ON THE PHYSICAL FILE — one per (run unit, physical file), 0 while none
    /// (2.2.1.10, "A TRANSACTION is one run unit's open unit of work on one physical file"). Its liveness byte is held
    /// through <see cref="_liveness"/> until COMMIT or ROLLBACK ends it.</summary>
    public ulong Transaction { get; private set; }

    /// <summary>⛔ THE STORE'S PLACE IN THE RUN UNIT'S ORDER OF OPEN STORES (<see cref="StoreTable"/>, which stamps it when the
    /// store becomes the run unit's for its physical file; 0 for a store no table holds): the order COMMIT and ROLLBACK take
    /// the run unit's transactions in (2.2.1.10, <i>"Files are committed one at a time, in the order of the run unit's open
    /// stores"</i>; <c>FileRegistry.TransactionHolders</c>). A transaction's store stays in the table from its first change
    /// to its COMMIT or ROLLBACK (a CLOSE under commit control keeps the attachment), so the place is stable for as long as
    /// a transaction stands, whoever holds it: a live connector, or what a CLOSE kept (<see cref="KeptFile"/>).</summary>
    internal long OpenOrder { get; set; }

    /// <summary>The endpoint holding <see cref="Transaction"/>'s liveness byte.</summary>
    private RegionLocks? _liveness;

    /// <summary>The file is MARKED UNACKNOWLEDGED (2.2.1.10 region 3, the reversal refused too; the UNACKNOWLEDGED row of
    /// 2.2.1.15): until a promotion of this run unit lands, every ROLLBACK answers by a promotion's sync.</summary>
    public bool Unacknowledged { get; private set; }

    /// <summary>A ROLLBACK of <see cref="Transaction"/> was refused ('30'): the transaction stands for the next ROLLBACK or
    /// the dead-owner step, and is never committed (<see cref="Commit"/>).</summary>
    private bool _rollbackRefused;

    /// <summary>Whether the run unit owes the file a ROLLBACK of <see cref="Transaction"/> before anything else: one was
    /// refused, or the file is <see cref="Unacknowledged"/>.</summary>
    private bool RollbackOwed => _rollbackRefused || Unacknowledged;

    /// <summary>⛔ DRAW a transaction id (2.2.1.10, finding R4-2): the lowest value from the store's
    /// <see cref="PagedFile.TransactionMint"/> whose liveness byte <paramref name="drawer"/> can take EXCLUSIVELY through
    /// its writable handle — a held byte is a live transaction's or a refused attempt's, and is skipped. 0 when no byte
    /// can be taken (a host that cannot carry the lock at all keeps no liveness, so it draws the mint as it stands).</summary>
    private static ulong Draw(PagedFile file, RegionLocks drawer)
    {
        const int MaxSkips = 1 << 16;
        ulong id = file.TransactionMint;
        for (int skips = 0; skips < MaxSkips; skips++, id++)
            switch (drawer.Hold(HostRegionLocks.TransactionLivenessByte(id), exclusive: true, wait: false))
            {
                case RegionLockResult.Ok: return id;
                case RegionLockResult.Unavailable: return id;
            }
        return 0;
    }

    /// <summary>⛔ THE TRANSACTION'S LIVENESS BYTE IS HELD THROUGH <paramref name="to"/> FROM HERE — the endpoint of the
    /// handle a connector's re-OPEN took after its CLOSE under commit control (2.2.1.10): the byte the handle the CLOSE kept
    /// still holds moves, and a byte that went with a kept handle given back early (one whose share mode refused the new
    /// handle, <see cref="LivenessGoneWith"/>) is taken again. The caller holds the store mutex, so no other run unit's
    /// dead-owner probe, lock test or draw can find the byte free in between. A handle the host lets hold no exclusive lock
    /// (a read-only one where the lock needs a writable handle) holds it SHARED, which refuses every other run unit's
    /// exclusive probe and draw alike (<see cref="OwnerOf"/>, <see cref="Draw"/>).</summary>
    internal void HoldLivenessThrough(RegionLocks to)
    {
        if (Transaction == 0 || ReferenceEquals(_liveness, to)) return;
        long liveness = HostRegionLocks.TransactionLivenessByte(Transaction);
        _liveness?.Release(liveness);
        _liveness = to.Hold(liveness, exclusive: to.CanHoldExclusive, wait: false) == RegionLockResult.Ok ? to : null;
    }

    /// <summary>The liveness byte went with <paramref name="endpoint"/> — a kept handle given back while the transaction
    /// stands (a re-OPEN whose share mode the kept handle refused, a ROLLBACK refused before its handle went): what a death
    /// does to it; <see cref="HoldLivenessThrough"/> takes it again, else the dead-owner step settles it. The endpoint's
    /// disposal, which follows, is what unlocks the byte, explicitly and before its handle closes (<c>KeptFile.GiveBack</c>,
    /// kb/Work PB3549).</summary>
    internal void LivenessGoneWith(RegionLocks endpoint)
    {
        if (ReferenceEquals(_liveness, endpoint)) _liveness = null;
    }

    /// <summary>A first change refused before its page 2 gives the byte it drew back: no entry carries the id.</summary>
    private static void GiveBack(RegionLocks? drawer, ulong drawn)
    {
        if (drawn != 0) drawer?.Release(HostRegionLocks.TransactionLivenessByte(drawn));
    }

    /// <summary>End this run unit's transaction: its liveness byte given back (after the entry's removal and its
    /// promotion — 2.2.1.17, "B removes the entry (statement) and promotes before releasing the byte").</summary>
    private void EndTransaction()
    {
        GiveBack(_liveness, Transaction);
        (Transaction, _liveness, Unacknowledged, _rollbackRefused) = (0, null, false, false);
    }

    /// <summary>The witnesses' seam of a death the operating environment delivers at an instant of ANOTHER run unit's
    /// statement (DESIGN-runtime-library.md 2.2.1.17's round-4 table: "the test lock host releasing A's byte at that
    /// point"): the transaction's liveness byte released as a dead process's handle releases it, and nothing else of the
    /// run unit touched — its holdings are pages of the file, and an idle run unit holds no mutex.</summary>
    internal void ReleaseLivenessForWitness()
    {
        GiveBack(_liveness, Transaction);
        _liveness = null;
    }

    /// <summary>The transaction holding HOLDINGS key <paramref name="key"/> (0: none) — the lookup of the lock TEST (2.2.1.10,
    /// "The lock TEST, for every writer and every reader not IGNORING LOCK"): nothing to read while page 2's holdings root
    /// is empty.</summary>
    internal StoreStatus HolderOf(byte[] key, out ulong owner)
    {
        owner = 0;
        if (File is not { } file || file.Meta.HoldingsRoot.IsNone) return StoreStatus.Ok;
        var status = new BTree(file, file.HoldingsOrder, file.Meta.HoldingsRoot).TryGet(key, out var value);
        if (value is not null) owner = TransactionKeys.TransactionOf(value);
        return status;
    }

    /// <summary>⛔ THE DEAD-OWNER PROBE (2.2.1.10; findings R3-1, R4-1; kb/Work PB3549): transaction <paramref name="id"/>'s
    /// liveness byte asked through <paramref name="prober"/> NOW, and the host's three answers read in this one place — free
    /// is <see cref="OwnerLiveness.Dead"/> (its owner gave the byte back explicitly, or the operating environment released
    /// it with a dead process's handle), held is <see cref="OwnerLiveness.Alive"/>, and a host that cannot carry the lock is
    /// <see cref="OwnerLiveness.Unknowable"/>. A transient host failure is never the third answer: the host asks again
    /// before it answers (<c>OfdRegionLocks.Classify</c>; the lock-file host's bounded publication and observation).</summary>
    internal static OwnerLiveness OwnerOf(RegionLocks prober, ulong id) =>
        prober.HeldByAnother(HostRegionLocks.TransactionLivenessByte(id)) switch
        {
            RegionLockResult.Ok => OwnerLiveness.Dead,
            RegionLockResult.Held => OwnerLiveness.Alive,
            _ => OwnerLiveness.Unknowable,
        };

    /// <summary>Whether the dead-owner step and the lock test may roll transaction <paramref name="id"/> back: only a DEAD
    /// owner's. An owner the host cannot see is treated as alive, so nothing is rolled back on a guess (its holdings answer
    /// '51', and its transaction stands until its owner ends it).</summary>
    internal static bool MayRollBack(RegionLocks prober, ulong id) => OwnerOf(prober, id) switch
    {
        OwnerLiveness.Dead => true,
        OwnerLiveness.Alive or OwnerLiveness.Unknowable => false,
        _ => throw new InvalidOperationException("an owner liveness the dead-owner step does not read"),
    };

    /// <summary>⛔ THE ROLLBACK STATEMENT (§14.9.36.4 GR3; 2.2.1.10): transaction <paramref name="id"/>'s UNDO tree walked
    /// once — every R before-image written back under its identity through the organization's
    /// <paramref name="restoring"/> trees, every key the walk met removed from the HOLDINGS tree, the entry removed, the
    /// UNDO pages released — as ONE statement of the page layer (one generation, atomic under 2.2.1.6). No promotion: lost
    /// to a power loss, the committed state still carries the transaction and the next writable statement or OPEN rolls
    /// it back again; the operation is idempotent (no entry: nothing to do). Run by the owner's ROLLBACK, and by any
    /// writable connector for a DEAD owner (the dead-owner step and the lock test). The caller's entry ran.</summary>
    internal StoreStatus RollBack(ulong id, RestoringTreesFactory restoring)
    {
        var file = File ?? throw new InvalidOperationException("a keyed store with no file rolls nothing back");
        var trees = restoring(file, file.Meta);
        var status = TransactionRetirement.Of(file, file.Meta, id, trees.Restore, out var retirement);
        if (status != StoreStatus.Ok || retirement is null)
        {
            foreach (var (tree, _) in trees.Edited) tree.Discard();
            return status;
        }
        status = file.Begin(StatementBound.Of([.. trees.Edited.Select(t => t.Tree), .. retirement.Trees]), out var statement,
            entered: true);
        if (status != StoreStatus.Ok)
        {
            foreach (var (tree, _) in trees.Edited) tree.Discard();
            retirement.Discard();
            return status;
        }
        var flushed = PagedFile.FlushRestored(trees, statement!);
        if (flushed == StoreStatus.Ok) flushed = retirement.Flush(statement!);
        var completed = statement!.Complete();
        return flushed != StoreStatus.Ok ? StoreStatus.PermanentError : completed;
    }

    /// <summary>⛔ THE DEAD-OWNER STEP (§9.1.18.5: <i>"When a run unit terminates abnormally, all file changes made since
    /// the previous commit or, if none, the beginning of the run unit are rolled back for all files for which there are
    /// active APPLY COMMIT clauses."</i>; 2.2.1.10, finding R3-1): at the coherent entry of every statement through a
    /// writable handle and at a writable OPEN (R6 step 5), under the mutex, once a PROMOTING flag has been resolved — when
    /// page 2's transactions root is not empty, every transaction of ANOTHER run unit whose liveness byte is free is
    /// rolled back before the entering statement proceeds; one whose byte is held is left exactly as it stands.</summary>
    internal StoreStatus RollBackDeadOwners(RegionLocks prober, RestoringTreesFactory restoring)
    {
        if (File is not { Writable: true } file || file.Meta.TransactionsRoot.IsNone) return StoreStatus.Ok;
        var ids = new List<ulong>();
        var status = new BTree(file, KeyOrders.Ordinal, file.Meta.TransactionsRoot).First(out var cursor);
        while (status == StoreStatus.Ok && cursor.HasCurrent)
        {
            ids.Add(TransactionKeys.TransactionOf(cursor.Key));
            status = cursor.Next();
        }
        if (status != StoreStatus.Ok) return status;
        foreach (ulong id in ids)
            if (id != Transaction && MayRollBack(prober, id) && (status = RollBack(id, restoring)) != StoreStatus.Ok)
                return status;
        return StoreStatus.Ok;
    }

    /// <summary>⛔ COMMIT this run unit's transaction on the file (§14.9.7.4 GR3; 2.2.1.10): the promotion that decides it
    /// (<see cref="PagedFile.Commit"/>). Committed, or reversed by a refused sync 2: the transaction is over and its byte
    /// given back. Refused before the slot write: the transaction stands, intact, for the unconditional ROLLBACK
    /// (§14.6.13.1.3 item 8). The reversal refused too: the file is MARKED UNACKNOWLEDGED. The caller holds the mutex.</summary>
    internal CommitOutcome Commit(RestoringTreesFactory reverse)
    {
        if (Transaction == 0) return CommitOutcome.Committed;
        // ⛔ A TRANSACTION A ROLLBACK IS OWED IS NEVER COMMITTED (2.2.1.10; 2.2.1.16's refused ROLLBACK and UNACKNOWLEDGED
        // rows; review finding R1-4): once a ROLLBACK of it was refused, or its COMMIT left the file UNACKNOWLEDGED, the
        // only statements the run unit executes on it are ROLLBACKs (item 8's unconditional one, then the termination
        // path's). A later COMMIT, the program's or the implicit one of a run unit that then ends normally (§9.1.18.5), is
        // refused before it touches the medium, and its caller rolls back again: the changes a ROLLBACK was asked to undo
        // are never made permanent. Nor is a transaction whose file the run unit no longer holds a writable handle on (a
        // re-OPEN refused in its share-mode window), which only the dead-owner step can settle.
        if (RollbackOwed || File is not { Writable: true } file) return CommitOutcome.Refused;
        var outcome = file.Commit(Transaction, reverse);
        if (outcome is CommitOutcome.Committed or CommitOutcome.Reversed) EndTransaction();
        else if (outcome == CommitOutcome.Unacknowledged) Unacknowledged = true;
        return outcome;
    }

    /// <summary>⛔ ROLLBACK this run unit's transaction on the file (§14.9.36.4 GR3, <i>"restored to the state they were in at
    /// the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had previously been
    /// executed"</i>; 2.2.1.10): the ROLLBACK statement, or on a file MARKED UNACKNOWLEDGED the reversal again and a
    /// promotion answered by its sync (<see cref="PagedFile.RollBackUnacknowledged"/>). Success ends the transaction and
    /// gives the byte back; a refusal ('30') — the statement's entry (which unwinds a COMMIT's unlanded claim, R5's restart
    /// rule), the rollback itself, or a store with no handle left — leaves it standing for the next attempt or the
    /// dead-owner step, and never committed (<see cref="Commit"/>). The caller holds the mutex.</summary>
    internal StoreStatus Rollback(RestoringTreesFactory restoring)
    {
        if (Transaction == 0) return StoreStatus.Ok;
        var status = File is not { Writable: true } file ? StoreStatus.PermanentError   // no writable handle left: refused
            : Unacknowledged ? file.RollBackUnacknowledged(Transaction, restoring)
            : file.Enter() != StoreStatus.Ok ? StoreStatus.PermanentError   // the statement's entry (R5's restart rule)
            : RollBack(Transaction, restoring);
        if (status == StoreStatus.Ok) EndTransaction();
        else _rollbackRefused = true;
        return status;
    }

    /// <summary>The UNDO and HOLDINGS edits one record statement under commit control adds (2.2.1.10): the run unit's UNDO
    /// tree with the record's before-image and the V entries, the HOLDINGS tree with the same keys, and the
    /// TRANSACTIONS entry naming the UNDO tree — given a placeholder of the pointer's size before the statement begins, so
    /// its page is counted, and the UNDO tree's real root once that tree is flushed (the leaf is already this edit's, so
    /// the second edit changes it in place).</summary>
    private sealed class Holdings
    {
        private static readonly byte[] Placeholder = TransactionKeys.Pointer(new PagePointer(uint.MaxValue, ulong.MaxValue));

        private readonly ulong _id;
        private readonly BTree _undo, _transactions, _holdings;

        private Holdings(ulong id, BTree undo, BTree transactions, BTree holdings) =>
            (_id, _undo, _transactions, _holdings) = (id, undo, transactions, holdings);

        public BTree[] Trees => [_undo, _transactions, _holdings];

        public static StoreStatus Of(PagedFile file, ulong id, bool firstChange, TransactionWork work, out Holdings? holdings)
        {
            holdings = null;
            var meta = file.Meta;
            var transactions = new BTree(file, KeyOrders.Ordinal, meta.TransactionsRoot);
            var undoRoot = PagePointer.None;
            if (!firstChange)
            {
                var found = transactions.TryGet(TransactionKeys.Transaction(id), out var entry);
                // A live transaction's entry is removed only by its own COMMIT or ROLLBACK, never by another run unit
                // (its byte is held), so a missing one is a file this run unit cannot trust: '30'.
                if (found != StoreStatus.Ok || entry is null) return StoreStatus.PermanentError;
                undoRoot = TransactionKeys.PointerOf(entry);
            }
            var undo = new BTree(file, file.HoldingsOrder, undoRoot);
            var held = new BTree(file, file.HoldingsOrder, meta.HoldingsRoot);
            byte[] owner = TransactionKeys.Transaction(id);
            var status = Hold(undo, held, TransactionKeys.Record(work.Identity), TransactionKeys.BeforeImage(work.BeforeImage), owner);
            foreach (var value in work.FreedValues)
                if (status == StoreStatus.Ok) status = Hold(undo, held, value, [], owner);
            if (status == StoreStatus.Ok && undo.HasPendingChanges)
                status = transactions.Put(TransactionKeys.Transaction(id), Placeholder);
            if (status != StoreStatus.Ok)
            {
                undo.Discard();
                transactions.Discard();
                held.Discard();
                return status;
            }
            holdings = new Holdings(id, undo, transactions, held);
            return StoreStatus.Ok;
        }

        /// <summary>One holding: the UNDO entry unless one exists (the FIRST image is the one to restore) and the HOLDINGS
        /// entry, in the same breath — the two trees carry the same keys (finding R5-2).</summary>
        private static StoreStatus Hold(BTree undo, BTree holdings, byte[] key, byte[] undoValue, byte[] owner)
        {
            var status = undo.Insert(key, undoValue, out _);
            return status == StoreStatus.Ok ? holdings.Insert(key, owner, out _) : status;
        }

        public StoreStatus Flush(PageStatement statement)
        {
            if (_undo.HasPendingChanges)
            {
                if (_undo.Flush(statement, out var undoRoot) is var u && u != StoreStatus.Ok) return Fail(u);
                if (_transactions.Replace(TransactionKeys.Transaction(_id), TransactionKeys.Pointer(undoRoot), out _) is var r
                    && r != StoreStatus.Ok)
                    return Fail(r);
            }
            if (_transactions.Flush(statement, out var transactionsRoot) is var t && t != StoreStatus.Ok) return Fail(t);
            if (_holdings.Flush(statement, out var holdingsRoot) is var h && h != StoreStatus.Ok) return Fail(h);
            statement.SetTransactionsRoot(transactionsRoot);
            statement.SetHoldingsRoot(holdingsRoot);
            return StoreStatus.Ok;
        }

        private StoreStatus Fail(StoreStatus status)
        {
            Discard();
            return status;
        }

        public void Discard()
        {
            foreach (var tree in Trees) tree.Discard();
        }
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
    public StoreStatus Insert(long rrn, byte[] value, bool entered, out bool done, TransactionWork? work = null) =>
        OneTree(tree => (tree.Insert(Key(rrn), value, out bool d), d), entered, out done, work);

    /// <summary>Replace record <paramref name="rrn"/> (<paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Replace(long rrn, byte[] value, bool entered, out bool done, TransactionWork? work = null) =>
        OneTree(tree => (tree.Replace(Key(rrn), value, out bool d), d), entered, out done, work);

    /// <summary>Remove record <paramref name="rrn"/> (§14.9.10.4 GR5, <i>"the identified record has been logically removed
    /// from the physical file and can no longer be accessed"</i>; <paramref name="done"/> false when it does not exist).</summary>
    public StoreStatus Delete(long rrn, bool entered, out bool done, TransactionWork? work = null) =>
        OneTree(tree => (tree.Delete(Key(rrn), out bool d), d), entered, out done, work);

    private StoreStatus OneTree(Func<BTree, (StoreStatus Status, bool Done)> edit, bool entered, out bool done,
        TransactionWork? work) =>
        Change(entered, ordinals: 0, (_, _) =>
        {
            var tree = Tree();
            var (status, applied) = edit(tree);
            return new Edit(status, applied, [new TreeChange(tree, TreeChange.PrimeRoot)]);
        }, out done, work);

    private BTree Tree() => new(File!, KeyOrders.Ordinal, File!.Meta.PrimeRoot);

    /// <summary>A record's identity in the transaction trees and its key in the record-number tree: the 8-byte big-endian
    /// relative record number (2.2.1.4).</summary>
    public static byte[] Key(long rrn) => KeyOrders.RecordNumberKey((ulong)rrn);

    /// <summary>⛔ A ROLLBACK's view of a relative file (2.2.1.10): ONE tree, a before-image written back under its record
    /// number, the ABSENT marker a delete.</summary>
    public static RestoringTreesFactory Restoring { get; } =
        (file, meta) => new RecordNumberRestoring(new BTree(file, KeyOrders.Ordinal, meta.PrimeRoot));

    private sealed class RecordNumberRestoring(BTree tree) : IRestoringTrees
    {
        public StoreStatus Restore(byte[] identity, byte[]? image) =>
            image is null ? tree.Delete(identity, out _) : tree.Put(identity, image);

        public IReadOnlyList<(BTree Tree, int Root)> Edited => [(tree, IRestoringTrees.PrimeRoot)];
    }
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

    /// <summary>The tree of key <paramref name="key"/> (<see cref="KeyedStore.TreeChange.PrimeRoot"/> the prime key, else an
    /// alternate key's index) under <paramref name="orders"/>; null when the file holds no store, or no such tree.</summary>
    public BTree? Tree(int key, IndexedKeyOrders orders)
    {
        if (File is not { } file) return null;
        if (key == TreeChange.PrimeRoot) return new BTree(file, orders.Prime, file.Meta.PrimeRoot);
        return key < file.Meta.AlternateRoots.Count ? new BTree(file, orders.Trees[key], file.Meta.AlternateRoots[key]) : null;
    }

    /// <summary>The frame of the record whose prime key is <paramref name="prime"/>; null when the file holds none.</summary>
    public StoreStatus Get(IndexedKeyOrders orders, ReadOnlySpan<byte> prime, out byte[]? frame) =>
        Get(orders, prime, out _, out frame);

    /// <summary>⛔ The frame of the record whose prime key is <paramref name="prime"/> AND the key image its prime tree holds
    /// it under (<paramref name="stored"/>): under a collating sequence that makes two key images one, the record is named
    /// by the spelling it was WRITTEN with, whichever spelling a statement asked for it by (§12.4.5.12.4 GR1). It is the
    /// record's identity for the transactions' holdings and the §9.1.16 record lock; null with no record.</summary>
    public StoreStatus Get(IndexedKeyOrders orders, ReadOnlySpan<byte> prime, out byte[]? stored, out byte[]? frame)
    {
        stored = frame = null;
        return Tree(TreeChange.PrimeRoot, orders) is { } tree ? tree.TryGet(prime, out stored, out frame) : StoreStatus.Ok;
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
        IReadOnlyList<byte[]?> alternates, out bool done, TransactionWork? work = null) =>
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
        }, out done, work);

    /// <summary>⛔ REPLACE a record (§14.9.35.4 GR24): its prime entry's frame and, per alternate key the REWRITE changed
    /// (<paramref name="moves"/>), the old entry deleted (null: there was none — the old value was suppressed) and the new
    /// one inserted under the ONE fresh ordinal when <paramref name="ordinals"/> mints it (null: the new value is
    /// suppressed). A key the REWRITE did not change keeps its entry and its place among its duplicates (GR24 a)) because
    /// nothing touches its tree. <paramref name="done"/> is false when the record does not exist.</summary>
    public StoreStatus Replace(bool entered, IndexedKeyOrders orders, byte[] prime, Func<ulong, byte[]> frameOf, int ordinals,
        IReadOnlyList<(int Key, byte[]? Old, byte[]? New)> moves, out bool done, TransactionWork? work = null) =>
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
        }, out done, work);

    /// <summary>⛔ REMOVE a record (§14.9.10.4 GR5): its prime entry and every alternate entry it has
    /// (<paramref name="entries"/>, each an alternate tree's whole entry key) — n + 1 deletes in one statement.
    /// <paramref name="done"/> is false when the record does not exist.</summary>
    public StoreStatus Remove(bool entered, IndexedKeyOrders orders, byte[] prime, IReadOnlyList<(int Key, byte[] Entry)> entries,
        out bool done, TransactionWork? work = null) =>
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
        }, out done, work);
    /// <summary>⛔ A ROLLBACK's view of an indexed file (2.2.1.10): the prime tree and the alternate trees under
    /// <paramref name="orders"/>. A before-image is written back under its prime key with its OLD ordinal vector, and the
    /// alternate trees' composite entries are rebuilt from that vector — the record's current entries removed (found from
    /// its current frame) and the image's inserted — so the duplicate order of §14.9.35.4 GR24 is the one at the last
    /// COMMIT (the release mint only ever grows). <paramref name="entriesOf"/> is the connector's: a frame's alternate
    /// entries (its key values and ordinals, suppression applied) given the number of release ordinals the FILE's frames
    /// carry (its meta page's key table, which the file being restored states), null for a frame it cannot decode ('30').</summary>
    public static RestoringTreesFactory Restoring(IndexedKeyOrders orders,
        Func<int, byte[], IReadOnlyList<(int Key, byte[] Entry)>?> entriesOf) =>
        (file, meta) => new IndexedRestoring(file, meta, orders, entriesOf);

    private sealed class IndexedRestoring(PagedFile file, MetaPage meta, IndexedKeyOrders orders,
        Func<int, byte[], IReadOnlyList<(int Key, byte[] Entry)>?> entriesOf) : IRestoringTrees
    {
        private readonly int _frameSlots = meta.Attributes.Keys.Count;
        private readonly BTree _prime = new(file, orders.Prime, meta.PrimeRoot);
        private readonly SortedDictionary<int, BTree> _alternates = [];

        public IReadOnlyList<(BTree Tree, int Root)> Edited =>
            [(_prime, IRestoringTrees.PrimeRoot), .. _alternates.Select(a => (a.Value, a.Key))];

        /// <summary>Alternate key <paramref name="key"/>'s tree, or null for a key the file records no tree for (the
        /// DOC-A.1-129 opt-out).</summary>
        private BTree? Alternate(int key)
        {
            if (key >= meta.AlternateRoots.Count) return null;
            if (!_alternates.TryGetValue(key, out var tree))
                _alternates[key] = tree = new BTree(file, orders.Trees[key], meta.AlternateRoots[key]);
            return tree;
        }

        public StoreStatus Restore(byte[] identity, byte[]? image)
        {
            var status = _prime.TryGet(identity, out var current);
            if (status != StoreStatus.Ok) return status;
            if (current is not null)
            {
                if (entriesOf(_frameSlots, current) is not { } entries) return StoreStatus.PermanentError;
                foreach (var (key, entry) in entries)
                    if (Alternate(key) is { } tree && (status = tree.Delete(entry, out _)) != StoreStatus.Ok) return status;
                if ((status = _prime.Delete(identity, out _)) != StoreStatus.Ok) return status;
            }
            if (image is null) return StoreStatus.Ok;
            if (entriesOf(_frameSlots, image) is not { } restored) return StoreStatus.PermanentError;
            if ((status = _prime.Put(identity, image)) != StoreStatus.Ok) return status;
            foreach (var (key, entry) in restored)
                if (Alternate(key) is { } tree && (status = tree.Insert(entry, identity, out _)) != StoreStatus.Ok) return status;
            return StoreStatus.Ok;
        }
    }
}
