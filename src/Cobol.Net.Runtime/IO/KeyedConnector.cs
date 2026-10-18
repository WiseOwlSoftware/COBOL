// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>The ACCESS MODE of a keyed file connector (ISO/IEC 1989:2023 §12.4.5.5, ACCESS MODE clause). The
/// ordinals mirror the compiler's <c>FileAccessMode</c> enum — registration passes the raw int.</summary>
public enum KeyedAccess
{
    /// <summary>ACCESS SEQUENTIAL — records in ascending RRN / key-of-reference order (§12.4.5.5.3 GR2).</summary>
    Sequential = 0,
    /// <summary>ACCESS RANDOM — records selected by key value (§9.1.8.3).</summary>
    Random = 1,
    /// <summary>ACCESS DYNAMIC — both, chosen per statement form (§9.1.8.4).</summary>
    Dynamic = 2,
}

/// <summary>
/// The shared base of the KEYED connectors — RELATIVE (§9.1.7.3) and INDEXED (§9.1.7.4) — carrying the one fact
/// every keyed verb branches on: the connector's ACCESS MODE. A sequential-organization connector has no access
/// mode to carry (§12.4.5.5.2 SR2 — <i>"The DYNAMIC and RANDOM phrases shall not be specified for a
/// sequential file"</i>), which is why this sits below <see cref="FileConnector"/> rather than on it.
/// That premise is now ENFORCED rather than assumed: until kb/Work PB692 the rule had no check anywhere and
/// <c>ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM</c> compiled and ran, so this class's reason for
/// existing was a comment about source the compiler accepted. <c>DataBinder.BindFileControl</c> screens it on
/// the file control entry (COBOLNET1858, every edition), which is what makes "a sequential-organization
/// connector has no access mode" a fact about every program that reaches the runtime.
///
/// ⛔ INVARIANT (kb/Work PB325) — <b>the OPEN MODE never SELECTS a keyed verb's branch; it is only ever
/// SCREENED.</b> Every branch the standard draws inside a keyed verb is drawn on the ACCESS MODE:
/// §14.9.51.4 GR29 a)/b) (relative WRITE: consecutive release vs. the staged RELATIVE KEY), GR38/GR39 (indexed
/// WRITE: the ascending-prime-key requirement vs. "in any order"), §14.9.35.4 GR5 vs. GR21/GR22/GR23 and
/// §14.9.10.4 GR2 vs. GR3/GR4 (REWRITE/DELETE target: the last-read record vs. the key item's). The OPEN MODE
/// enters only as the permission test that follows — §14.9.27.4 GR8's Table 20, whose unsuccessful cells
/// §9.1.13.7 items 8 and 9 name ('48' for WRITE, '49' for REWRITE/DELETE) — and item 8's own two arms are
/// themselves selected BY the access mode: a) sequential ⇒ extend or output, b) random or dynamic ⇒ I-O or
/// output.
///
/// Folding the open mode into a branch predicate ("sequential access <i>or</i> extend mode") therefore inverts
/// the dependency and makes the runtime's answer depend on a bind-time screen holding: §14.9.27.3 SR2 confines
/// EXTEND to sequential access, so a random- or dynamic-access connector open in the extend mode is a state the
/// SOURCE cannot legally reach — but the runtime must still answer '48' for it (Table 20 leaves the
/// Random/Extend and Dynamic/Extend WRITE cells blank), not divert into the sequential-release branch and
/// succeed. That divergence was PB325.
/// </summary>
public abstract class KeyedConnector : FileConnector
{
    protected KeyedConnector(string hostPath, int recordWidth, KeyedAccess access, int varyMin, int varyMax)
        : base(hostPath, recordWidth, varyMin, varyMax) => Access = access;

    /// <summary>The connector's ACCESS MODE (§12.4.5.5) — the sole discriminator of every keyed verb's branch;
    /// see the type remarks for why the open mode is never part of one.</summary>
    protected KeyedAccess Access { get; }

    /// <summary>Whether this run unit validates the INDEXED key table (<see cref="FileRegistry.KeyCheckVariable"/>
    /// — <c>COBOLNET_KEYCHECK=OFF</c> turns it off). Handed down by the registry at registration, exactly as
    /// <see cref="FileConnector.SharedStores"/> is, so a connector never has to ask an ambient run unit which
    /// registry it belongs to. A standalone connector (no registry) validates, the safe default.</summary>
    internal bool KeyCheck { get; set; } = true;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEYED ORGANIZATIONS' §14.9.27.4 GR10 ANSWER: the store's recorded attributes — the paged store's
    /// COMMITTED META (<see cref="MetaPage.Describes"/>) for a RELATIVE file, the whole store's HEADER for an INDEXED file
    /// until it moves to the engine (kb/Work PB3334) — are the physical file's §9.1.6 fixed file attributes
    /// (<see cref="RecordedAttributes"/>), so the comparison is those attributes against
    /// <see cref="FileConnector.DeclaredAttributes"/> (<see cref="FixedFileAttributes.Conflicts"/> — the one
    /// place the comparison is written). RELATIVE and INDEXED share this method because they share the format;
    /// what differs between them is only what <see cref="FileConnector.DeclaredKeys"/> supplies, which is where
    /// that difference belongs.
    /// <para>⛔ A STORE WITH NO READABLE HEADER IS A CONFLICT, NOT "attributes not recorded". Its layout is
    /// unknown, so its frames cannot be located either, and reading it anyway is the silent misread this rule
    /// exists to prevent — a plain sequential file opened through a RELATIVE description delivers rubbish with
    /// status '00' if nothing refuses it. A ZERO-BYTE file is the one exception
    /// (<see cref="StoreFormat.Empty"/>): it holds no records, so it can misread nothing and states no attribute
    /// to contradict.</para>
    /// <para>The header read is ONE bounded auxiliary open, and it replaces the catalog sidecar's open
    /// one-for-one — the file count went down by one and the open count did not go up (kb/Work PB802). It cannot
    /// ride the connector's own store load, because GR25 ("If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected") puts this check BEFORE <c>OpenCore</c>, whose creation arms
    /// truncate.</para>
    /// <para>⛔ A HOST REFUSAL OF THAT READ CANNOT SILENTLY SKIP THE CHECK, and the reason is structural rather
    /// than lucky: <see cref="RecordFraming.ReadHeader"/> answers <see cref="StoreFormat.Empty"/> (no conflict)
    /// on an I/O failure, but it asks for the LEAST a handle can — <c>HostFile.OpenAuxiliary</c>, read access,
    /// share <see cref="FileShare.ReadWrite"/> — and the connector's own store handle (<see cref="TakeFileLock"/>)
    /// asks for at least that a few statements later inside <c>OpenCore</c>, where the failure PROPAGATES to <c>FileConnector.Open</c>'s catch and
    /// becomes '37' or '30'. So any state that refuses the header read refuses the store load too, and the OPEN
    /// is unsuccessful either way; the swallow can lose a '39' only in favour of another unsuccessful status,
    /// never in favour of a successful OPEN over a store this connector could not interpret. Answering the
    /// authority statuses here instead would be a SECOND place §9.1.13.6 item 1's '30' is decided, which
    /// <c>FileConnector.Open</c>'s catch is the one place for.</para></remarks>
    /// <para>A paged store whose meta pages carry the format's magic but whose committed slots both fail
    /// (<see cref="StoreFormat.Damaged"/>) is the store's own format, damaged: not a conflict, so the OPEN goes on to
    /// the adoption rule, which answers §9.1.13.6 item 1's '30' (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.6
    /// R6 step 1).</para></remarks>
    protected override bool FixedAttributeConflict() =>
        RecordedAttributes(out var recorded) switch
        {
            StoreFormat.Empty or StoreFormat.Damaged => false,
            StoreFormat.Described => recorded!.Conflicts(DeclaredAttributes, KeyCheck),
            _ => true,   // Foreign — not a store this build can interpret
        };

    /// <summary>The §9.1.6 attributes the physical file records, read through one bounded auxiliary open — the
    /// organization's store format answers (<see cref="FixedAttributeConflict"/>).</summary>
    private protected abstract StoreFormat RecordedAttributes(out FixedFileAttributes? recorded);


    // ── The ISO §9.1.15 FILE LOCK, for the organizations whose store is a whole-file rewrite ────────────────

    /// <summary>⛔ THE HANDLE THAT IS THIS CONNECTOR'S §9.1.15 FILE LOCK — held for the life of the OPEN, and
    /// the ONE stream the store is loaded from and persisted through (kb/Work PB771).
    /// <para><i>"The successful opening of a file establishes a file lock for the applicable sharing rules,
    /// thereby preventing other run units from opening that file with incompatible sharing rules"</i>
    /// (§9.1.15 3)) — and a host share mode is the only thing a .NET process can say to another run unit, which
    /// means a connector that holds no handle says nothing. RELATIVE and INDEXED held none: their store was
    /// loaded at the OPEN and rewritten at the CLOSE through short-lived <c>HostFile.OpenAuxiliary</c> handles
    /// fixed at <see cref="FileShare.ReadWrite"/>, so <c>SHARING WITH NO OTHER</c> — <i>"exclusive access to a
    /// physical file"</i>, §9.1.15 1) — protected an indexed or relative file from nothing outside the run unit,
    /// and the CLOSE then truncated the file and rewrote it from the OPEN's snapshot, discarding another run
    /// unit's records with '00' reported on both sides. Measured, on both organizations.</para>
    /// <para>⛔ AND IT HAS TO BE THE SAME HANDLE THE STORE TRAVELS THROUGH. A second handle taken for the load
    /// or the persist would ask the operating environment for access this connector's own
    /// <see cref="FileShare.None"/> forbids, and the OPEN the standard ALLOWED would fail — kb/Work PB713's
    /// defect, re-opened by its own cure. That is why the load and the persist (<see cref="Load"/>,
    /// <see cref="Persist"/>) read and write positionally through THIS handle and nothing else.</para>
    /// <para>⛔ WHY <see cref="FileMode.Open"/> AND NEVER <c>Create</c>, even for <c>OPEN OUTPUT</c>: the handle
    /// outlives every persist, and <see cref="Persist"/> truncates as part of the rewrite. A creating MODE on the
    /// handle would make the truncation a property of when the handle happened to be taken.</para></summary>
    private FileStream? _store;

    /// <inheritdoc/>
    /// <remarks>The store handle <see cref="TakeFileLock"/> took — null before the OPEN body takes it, for an
    /// OPTIONAL file that is not present (there is no physical file to lock), and after the CLOSE. The
    /// organizations' load and persist go through it and nowhere else.</remarks>
    internal sealed override FileStream? FileLock => _store;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEYED STORE IS REWRITTEN WHOLE, so every writable mode READS the physical file before it
    /// writes it — <c>OPEN EXTEND</c> included, where <see cref="FileLockPosture.AccessOf"/>'s mode-derived
    /// floor says <see cref="FileAccess.Write"/> alone. §14.9.51.4 GR29 a) is why the load is not optional for
    /// an extend: the release number is <i>"one greater than the highest relative record number existing in the
    /// physical file"</i>, which is a fact of the records already there. Stating it here is what lets the
    /// registry widen a SIBLING's file lock by the access this handle really takes (kb/Work PB771).</remarks>
    internal override FileAccess HostAccess(FileOpenMode mode) =>
        mode is FileOpenMode.Input ? FileAccess.Read : FileAccess.ReadWrite;

    /// <summary>Take the file lock: the connector's own handle on the physical file, carrying
    /// <see cref="FileConnector.HostShare"/> — the posture the registry derived and handed down immediately
    /// before this body ran. Called by each organization's <c>OpenCore</c> on every arm that has a physical file
    /// to hold, and by that arm ONLY: an arm with no file (an absent OPTIONAL one on INPUT) locks nothing.
    /// <para><paramref name="create"/> is §14.9.27.4 GR17/GR18's creation — the OUTPUT arm and the absent
    /// OPTIONAL I-O/EXTEND arms — and is the only difference between the arms, because the store's own
    /// truncation is <see cref="Persist"/>'s.</para>
    /// <para>The same OPEN allocates this connector's input-output areas (ISO §12.4.5.14.3 GR1; <see cref="AllocateAreas"/>,
    /// kb/Work PB643, PB2693).</para></summary>
    /// <returns>The handle, so the arm that took it writes its store through that value rather than through a
    /// nullable field it has to re-assert.</returns>
    protected FileStream TakeFileLock(bool create)
    {
        // The areas first: an allocation that fails (a RESERVE the host cannot commit) must fail before a handle is
        // taken, because nothing would dispose a handle that never reached _store (train 1045 review).
        AllocateAreas();
        var taken = HostFile.OpenConnectorStore(HostPath, create ? FileMode.OpenOrCreate : FileMode.Open,
            HostAccess(Mode), HostShare);
        _store?.Dispose();   // no arm takes it twice; belt-and-braces so a future one cannot leak
        _store = taken;
        // Taken ONCE: the store is read and written positionally through it (never through the stream's buffer,
        // kb/Work PB2660), and it is the handle the store mutex and this connector's record locks are held by.
        _handle = taken.SafeFileHandle;
        // Writable is what the host granted this handle, never re-derived from the open mode (kb/Work PB2758).
        _regions = HostRegionLocks.Open(_handle, HostPath, writable: taken.CanWrite);
        _coherent = CoherentUnder(HostShare);
        return taken;
    }

    /// <summary>Give the file lock back — §9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
    /// statement executed for that file connector"</i>. Runs from each organization's <c>CloseCore</c> finally
    /// (whatever the persist did) and from <see cref="AbandonOpen"/>.</summary>
    protected void ReleaseFileLock()
    {
        // The field is cleared whatever the disposal throws (a final flush refused on an exhausted medium — the
        // disposal still closes the host handle), so a later CLOSE or re-OPEN never meets a disposed handle.
        try { _regions?.Dispose(); }   // the region locks first, while the handle they were taken through is still open
        finally
        {
            try { _store?.Dispose(); }
            finally { _store = null; _handle = null; _regions = null; _coherent = false; ReleaseAreas(); }
        }
    }

    /// <summary>Allocate this connector's input-output areas (§12.4.5.14.3 GR1) — the first step of
    /// <see cref="TakeFileLock"/>, so an allocation the run unit cannot make fails before a handle is taken. The
    /// organization says what the areas are: the whole store's transfer buffer (INDEXED), the page cache's floor (the
    /// engine, RELATIVE).</summary>
    private protected abstract void AllocateAreas();

    /// <summary>Give the input-output areas back — the last step of <see cref="ReleaseFileLock"/>.</summary>
    private protected abstract void ReleaseAreas();


    /// <inheritdoc/>
    protected override void AbandonOpen() => ReleaseFileLock();

    /// <summary>⛔ THE RECORD'S OWN RULES, WRITTEN ONCE FOR EVERY KEYED WRITE AND REWRITE (kb/Work PB2751): the size
    /// (§14.9.51.4 GR14 / §14.9.35.4 GR20, '44': <i>"If this rule is violated, the execution of the WRITE statement is
    /// unsuccessful and the I-O status of the write file connector is set to '44'"</i>), then a character with no
    /// byte image in the file's coded character set ('91', docs/CONFORMANCE.md DOC-A.1-110). Each statement asks it
    /// after its open-mode rule (and REWRITE's GR5 '43') and BEFORE any key or invalid key rule, which is the order
    /// the general rules state them in (DOC-A.1-104); the organizations used to ask it in four different places,
    /// two of them after the key rules. Returns the failing status, or null with the record as stored.</summary>
    private protected string? RecordRulesFailure(string image, int length, out string stored)
    {
        if (Stored(image, length) is not { } record)
        {
            stored = "";
            return FileStatusCode.RecordSizeViolation;
        }
        stored = record;
        return RecordHasCharacterWithoutByteImage(record) ? FileStatusCode.CharacterWithoutByteImage : null;
    }

    // ── Coherence ACROSS RUN UNITS (ISO §9.1.15, §9.1.16; kb/Work PB2660) ────────────────────────────────────

    /// <summary>The connector's own handle — <see cref="FileLock"/>'s, taken once — through which the store is read
    /// and written POSITIONALLY and through which the store mutex and the record locks are held
    /// (<see cref="HostRegionLocks"/>). Null whenever <see cref="FileLock"/> is.</summary>
    private SafeFileHandle? _handle;

    /// <summary>This connector's endpoint on the host's region locks (<see cref="RegionLocks"/>, kb/Work PB2936): made
    /// with <see cref="_handle"/> by <see cref="TakeFileLock"/>, disposed — every byte it holds given back — by
    /// <see cref="ReleaseFileLock"/>. Null whenever <see cref="_handle"/> is.</summary>
    private RegionLocks? _regions;

    /// <summary>⛔ Whether this connector's statements must be SERIALIZED against another run unit's — every one of them
    /// COHERENT (<see cref="BeginStatement"/>): it runs inside the store mutex, starting from the file as it is now and
    /// leaving the file as the statement left the records. The ENGINE the connector drives decides which other run
    /// units matter (<see cref="CoherentUnder"/>, docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7 and 2.2.1.13;
    /// kb/Work PB3333): the paged engine, which writes at every statement, against ANY other run unit's connector
    /// (<see cref="FileLockPosture.AdmitsAnotherRunUnit"/>, every sharing mode but NO OTHER — the INPUT reader §9.1.15 2)
    /// admits beside a READ ONLY writer would otherwise meet a half-written path); the whole store, which persists at
    /// CLOSE under the mutex, against another WRITER only (<see cref="FileLockPosture.AdmitsAnotherWriter"/>, which only
    /// <c>SHARING WITH ALL OTHER</c> admits).</summary>
    private bool _coherent;

    /// <summary>Whether a connector whose file lock carries <paramref name="share"/> is coherent (<see cref="_coherent"/>):
    /// the question the organization's engine asks of the posture.</summary>
    private protected abstract bool CoherentUnder(FileShare share);

    /// <summary>Whether this connector's statements run inside the store mutex (<see cref="_coherent"/>).</summary>
    private protected bool Coherent => _coherent;

    /// <summary>The connector's own handle (<see cref="FileLock"/>'s), through which its store's pages or image travel;
    /// null whenever <see cref="FileLock"/> is.</summary>
    private protected SafeFileHandle? StoreHandle => _handle;

    /// <summary>Whether the host granted the connector's handle write access (kb/Work PB2758).</summary>
    private protected bool StoreHandleWritable => _regions?.Writable ?? false;

    /// <summary>The store this connector is attached to (the run unit's ONE store for the physical file,
    /// kb/Work PB143).</summary>
    private protected abstract KeyedStore AttachedStore { get; }

    /// <inheritdoc/>
    internal override RegionLocks? RecordLocks => _regions;

    /// <summary>⛔ ENTER A COHERENT STATEMENT (kb/Work PB2660). For a connector another run unit may write
    /// (<see cref="_coherent"/>): take the store's cross-run-unit MUTEX (<see cref="HostRegionLocks.StoreMutexByte"/>)
    /// — waiting for another run unit's statement to finish — and, when the file's generation stamp differs from
    /// the one the store reflects, RELOAD the store from the file. Every governed record statement runs inside one
    /// (<c>FileRegistry</c>), so its record-lock check, its operation, its lock actions and the persist of what it
    /// changed are ONE step to every other run unit — which is what makes §9.1.16's <i>"While locked by a given
    /// file connector, a record is not accessible to another file connector in the same or a different run unit"</i>
    /// enforceable, and what makes §14.9.30.4 GR21's <i>"the first existing record in the physical file"</i> the
    /// file's and not a snapshot's. Inert (<c>default</c>) for every other connector.</summary>
    internal override StoreStatement BeginStatement()
    {
        if (!_coherent || _handle is null) return default;
        Enter(AttachedStore);
        return new StoreStatement(this);
    }

    /// <summary>⛔ A HOST FAILURE OF A STATEMENT'S OWN ENTRY IS A PERMANENT ERROR, NOT AN EXCEPTION (kb/Work PB2842).
    /// Taking the mutex, reading the generation stamp and reloading the store are all host I/O, and §9.1.13.6 1)'s
    /// <i>"A permanent error exists and no further information is available concerning the input-output
    /// operation"</i> ('30') is what the standard gives such a failure — the answer OPEN's catch already gives the
    /// same reload (a file longer than <see cref="RecordFraming.MaxStoreBytes"/>, <see cref="ReadImage"/>) and
    /// <see cref="CompleteStatement"/> gives the same statement's persist. The statement is entered all the same,
    /// holding what it took: the status is recorded (<see cref="FileConnector.SetStatus"/>, which starts the permanent
    /// error of §9.1.13.1, <i>"The permanent error condition remains in effect for all subsequent input-output
    /// operations on the file"</i>) and the statement's own body ends at the replay every verb makes first
    /// (<see cref="FileConnector.PermanentErrorReplay"/>) — so READ and START invalidate the file position indicator as
    /// they do for any permanent error, and nothing reads the stale store. A failure that is not the host's is not
    /// the file's and escapes as before, with the entry undone.</summary>
    private static bool IsHostStoreFailure(Exception failure) =>
        failure is IOException or UnauthorizedAccessException or OutOfMemoryException;

    private void Enter(KeyedStore store)
    {
        if (store.StatementDepth++ > 0) return;   // the run unit is already inside the mutex
        try
        {
            store.MutexHolder = HoldStoreMutex(exclusive: _regions!.Writable);
            if (!EnterStore()) SetStatus(FileStatusCode.PermanentError);
        }
        catch (Exception failure) when (IsHostStoreFailure(failure))
        {
            // The statement changed nothing, and a failed entry leaves the store as the engine's contract says — never
            // half-reloaded and trusted — so this statement's permanent error is the whole damage.
            EnterStoreFailed();
            SetStatus(FileStatusCode.PermanentError);
        }
        catch
        {
            Leave(store);
            throw;
        }
    }

    /// <summary>The engine's half of a coherent statement's entry, inside the mutex: bring the store to the physical
    /// file as it is now — the whole store reloads when the file's generation stamp moved, the paged engine reads page 2
    /// and adopts it (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7). False is §9.1.13.6 item 1's '30'.</summary>
    private protected abstract bool EnterStore();

    /// <summary>The engine's repair when <see cref="EnterStore"/> threw a host failure.</summary>
    private protected virtual void EnterStoreFailed() { }

    /// <summary>The engine's half of a coherent statement's completion, inside the mutex: the status the statement ends
    /// with. The whole store persists what the statement changed; the paged engine's record statements wrote their own
    /// pages and page 2 already, before they answered (2.2.1.7), so it has nothing left to write.</summary>
    private protected abstract string CompleteStore(string status);

    /// <summary>Run <paramref name="body"/> — an OPEN's adoption or creation, a CLOSE's promotion — inside the store mutex
    /// when this connector is coherent (2.2.1.7: <i>"runs every record statement, and its OPEN's adoption rule (R6),
    /// inside the store mutex"</i>); a NO OTHER connector has nobody to exclude.</summary>
    private protected T InStoreMutex<T>(Func<T> body)
    {
        if (!_coherent || _regions is null) return body();
        var holder = HoldStoreMutex(exclusive: _regions.Writable);
        try { return body(); }
        finally { holder?.Release(HostRegionLocks.StoreMutexByte); }
    }

    /// <summary>Finish the statement's work on the physical file, under the mutex, so the next statement of ANY run unit
    /// sees it (<see cref="CompleteStore"/>). A write the medium refuses is the statement's §9.1.13.6 item 1 permanent
    /// error ('30'), reported by THIS statement rather than by a later CLOSE. Returns the status the statement ends
    /// with.</summary>
    internal string CompleteStatement(string status)
    {
        if (AttachedStore.StatementDepth != 1) return status;
        try
        {
            return CompleteStore(status);
        }
        catch (Exception failure) when (IsHostStoreFailure(failure))
        {
            SetStatus(FileStatusCode.PermanentError);
            return FileStatusCode.PermanentError;
        }
    }

    /// <summary>Leave the statement; the outermost leave gives the mutex back.</summary>
    internal void EndStatement() => Leave(AttachedStore);

    private static void Leave(KeyedStore store)
    {
        if (--store.StatementDepth > 0) return;
        store.StatementDepth = 0;
        store.MutexHolder?.Release(HostRegionLocks.StoreMutexByte);
        store.MutexHolder = null;
    }

    /// <summary>⛔ WAIT OUTSIDE THE MUTEX. A statement that meets another run unit's record lock and carries a RETRY
    /// phrase waits for that run unit to give the lock back (§14.7.9.3) — which it can only do by executing a
    /// statement of its own, inside this same mutex. So the wait lets go of the mutex, and the statement re-enters
    /// (re-reading the file if it changed) before its next attempt. Nothing has been committed when a conflict is
    /// being retried — every conflict check precedes its operation (kb/Work PB338) — so there is nothing to persist
    /// first.</summary>
    internal override void WaitOutsideStatement(Action wait)
    {
        var store = AttachedStore;
        if (store.StatementDepth == 0 || store.MutexHolder is null) { wait(); return; }
        int depth = store.StatementDepth;
        store.StatementDepth = 1;
        Leave(store);
        try { wait(); }
        finally
        {
            Enter(store);
            store.StatementDepth = depth;
        }
    }

    /// <summary>The store mutex, held for the length of one statement — exclusive through a writable handle, shared
    /// through an <c>OPEN INPUT</c> connector's read-only one (a reader never changes the store, and Linux refuses
    /// an exclusive lock through a read-only descriptor; <see cref="RegionLocks.Hold"/>). Null when the host
    /// cannot carry the lock, and the run unit then has no protection against another's writer but the file lock
    /// (docs/CONFORMANCE.md DOC-A.1-75). <paramref name="exclusive"/> is true for a writer's whole-store persist,
    /// which excludes a reader too; the statement's own mutex follows the connector's access.</summary>
    private protected RegionLocks? HoldStoreMutex(bool exclusive) =>
        _regions!.Hold(HostRegionLocks.StoreMutexByte, exclusive, wait: true) == RegionLockResult.Ok ? _regions : null;

}

/// <summary>⛔ ONE STATEMENT'S HOLD ON A KEYED STORE that another run unit may write (kb/Work PB2660) — returned by
/// <see cref="FileConnector.BeginStatement"/> and inert (<c>default</c>) for every connector that cannot meet such a
/// writer. <c>FileRegistry</c> opens one around each governed record statement, ends the statement through
/// <see cref="Complete"/> (which persists what the statement changed and reports a refused persist as the
/// statement's status); a RETRY wait lets the mutex go through <see cref="FileConnector.WaitOutsideStatement"/>.</summary>
internal readonly struct StoreStatement : IDisposable
{
    private readonly KeyedConnector? _owner;

    internal StoreStatement(KeyedConnector owner) => _owner = owner;

    /// <summary>Persist what the statement changed; returns the status the statement ends with.</summary>
    public string Complete(string status) => _owner is { } owner ? owner.CompleteStatement(status) : status;

    /// <summary>Leave the statement (the outermost leave gives the mutex back).</summary>
    public void Dispose() => _owner?.EndStatement();
}