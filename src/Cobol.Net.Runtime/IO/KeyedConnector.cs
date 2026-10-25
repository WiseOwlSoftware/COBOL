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
/// every keyed verb branches on: the connector's ACCESS MODE, and the one LIFECYCLE both organizations share because
/// they drive one engine (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1; kb/Work PB2753): the §14.9.27.4 OPEN
/// arms (<see cref="OpenCore"/>), the store's attach and adoption, the CLOSE's promotion and the §9.1.15 release
/// (<see cref="CloseCore"/>), and the coherent statement entry. A sequential-organization connector has no access
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
    /// <remarks>⛔ THE KEYED ORGANIZATIONS' §14.9.27.4 GR10 ANSWER: the paged store's COMMITTED META, read from pages 0
    /// and 1 (<see cref="MetaPage.Describes"/>, docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.9), records the
    /// physical file's §9.1.6 fixed file attributes, so the comparison is those attributes against
    /// <see cref="FileConnector.DeclaredAttributes"/> (<see cref="FixedFileAttributes.Conflicts"/> — the one
    /// place the comparison is written). RELATIVE and INDEXED share this method because they share the format;
    /// what differs between them is only what <see cref="FileConnector.DeclaredKeys"/> supplies, which is where
    /// that difference belongs.
    /// <para>⛔ A FILE WITH NO READABLE META IS A CONFLICT, NOT "attributes not recorded". Its layout is unknown, so its
    /// records cannot be located either, and reading it anyway is the silent misread this rule exists to prevent — a
    /// plain sequential file, or a whole-store file of format 5, opened through a RELATIVE or INDEXED description
    /// delivers rubbish with status '00' if nothing refuses it. A ZERO-BYTE file is the one exception
    /// (<see cref="StoreFormat.Empty"/>): it holds no records, so it can misread nothing and states no attribute
    /// to contradict.</para>
    /// <para>The read is ONE bounded auxiliary open of two pages, and it replaces the catalog sidecar's open
    /// one-for-one (kb/Work PB802). It cannot ride the connector's own adoption, because GR25 ("If the execution of the
    /// OPEN statement is unsuccessful, the file is not affected") puts this check BEFORE <c>OpenCore</c>, whose
    /// creation arms truncate. A host refusal of that read answers <see cref="StoreFormat.Empty"/>, and cannot lose a
    /// '39' in favour of a successful OPEN: the connector's own handle asks for at least what the auxiliary read asks
    /// a few statements later, inside <c>OpenCore</c>, where the failure becomes '37' or '30'.</para>
    /// <para>A paged store whose meta pages carry the format's magic but whose committed slots both fail
    /// (<see cref="StoreFormat.Damaged"/>) is the store's own format, damaged: not a conflict, so the OPEN goes on to
    /// the adoption rule, which answers §9.1.13.6 item 1's '30' (2.2.1.6 R6 step 1).</para></remarks>
    protected override bool FixedAttributeConflict() =>
        MetaPage.Describes(HostPath, out var recorded) switch
        {
            StoreFormat.Empty or StoreFormat.Damaged => false,
            StoreFormat.Described => recorded!.Conflicts(DeclaredAttributes, KeyCheck),
            _ => true,   // Foreign — not a store this build can interpret
        };

    // ── The ISO §9.1.15 FILE LOCK, and the handle the store's pages travel through ───────────────────────────

    /// <summary>⛔ THE HANDLE THAT IS THIS CONNECTOR'S §9.1.15 FILE LOCK — held for the life of the OPEN, and
    /// the ONE handle the store's pages are read and written through (kb/Work PB771).
    /// <para><i>"The successful opening of a file establishes a file lock for the applicable sharing rules,
    /// thereby preventing other run units from opening that file with incompatible sharing rules"</i>
    /// (§9.1.15 3)) — and a host share mode is the only thing a .NET process can say to another run unit, which
    /// means a connector that holds no handle says nothing.</para>
    /// <para>⛔ AND IT HAS TO BE THE SAME HANDLE THE STORE TRAVELS THROUGH. A second handle taken for a page read or
    /// write would ask the operating environment for access this connector's own <see cref="FileShare.None"/> forbids,
    /// and the OPEN the standard ALLOWED would fail — kb/Work PB713's defect. That is why the run unit's page medium
    /// reads and writes through the handles of its connectors and nothing else (<see cref="RunUnitPageMedium"/>).</para>
    /// <para>⛔ WHY <see cref="FileMode.Open"/> AND NEVER <c>Create</c>, even for <c>OPEN OUTPUT</c>: the store's
    /// creation is its one truncation (<see cref="PagedFile.Create"/>); a creating MODE on the handle would make the
    /// truncation a property of when the handle happened to be taken.</para></summary>
    private FileStream? _store;

    /// <inheritdoc/>
    /// <remarks>The store handle <see cref="TakeFileLock"/> took — null before the OPEN body takes it, for an
    /// OPTIONAL file that is not present (there is no physical file to lock), and after the CLOSE.</remarks>
    internal sealed override FileStream? FileLock => _store;

    /// <inheritdoc/>
    /// <remarks>⛔ THE ENGINE READS THE FILE BEFORE IT WRITES IT, so every writable mode reads — <c>OPEN EXTEND</c>
    /// included, where <see cref="FileLockPosture.AccessOf"/>'s mode-derived floor says <see cref="FileAccess.Write"/>
    /// alone: the adoption rule reads the meta pages (2.2.1.6 R6), and §14.9.51.4 GR29 a)'s release number is
    /// <i>"one greater than the highest relative record number existing in the physical file"</i>, a fact of the
    /// records already there. Stating it here is what lets the registry widen a SIBLING's file lock by the access this
    /// handle really takes (kb/Work PB771).</remarks>
    internal override FileAccess HostAccess(FileOpenMode mode) =>
        mode is FileOpenMode.Input ? FileAccess.Read : FileAccess.ReadWrite;

    /// <summary>Take the file lock: the connector's own handle on the physical file, carrying
    /// <see cref="FileConnector.HostShare"/> — the posture the registry derived and handed down immediately
    /// before this body ran. Called by <see cref="OpenCore"/> on every arm that has a physical file to hold, and by
    /// that arm ONLY: an arm with no file (an absent OPTIONAL one on INPUT) locks nothing.
    /// <para><paramref name="create"/> is §14.9.27.4 GR17/GR18's creation — the OUTPUT arm and the absent
    /// OPTIONAL I-O/EXTEND arms — and is the only difference between the arms, because the store's own
    /// truncation is <see cref="PagedFile.Create"/>'s. <paramref name="access"/> is <see cref="Lock"/>'s.</para></summary>
    private void TakeFileLock(bool create, FileAccess access)
    {
        _store?.Dispose();   // no arm takes it twice; belt-and-braces so a future one cannot leak
        // ⛔ THE CONNECTOR'S OWN RE-OPEN TAKES BACK THE HANDLE ITS CLOSE UNDER COMMIT CONTROL KEPT when that handle is the
        // one this OPEN would take — the same access and the same share mode, so the same §9.1.15 file lock (review finding
        // R1-1): the transaction's liveness byte and the file lock never leave the run unit, and no other run unit's OPEN
        // or dead-owner step can come between them (2.2.1.10). The kept handle is the store's medium already, so only the
        // connector's fields change. ⛔ NEVER FOR A CREATION: an OPEN that creates the file takes a handle of its own and makes the
        // new store through it beside the kept one (Recreate), so that a creation which is refused leaves the kept state as the
        // CLOSE left it (§14.9.27.4 GR25; kb/Work PB3493).
        if (!create && _kept is { Handle: { } kept, Adopted: false } && kept.Share == HostShare && kept.Stream.CanWrite == (access != FileAccess.Read))
        {
            _kept = _kept with { Adopted = true };
            (_store, _handle, _regions) = (kept.Stream, kept.Handle, kept.Regions);
        }
        else
        {
            var taken = HostFile.OpenConnectorStore(HostPath, create ? FileMode.OpenOrCreate : FileMode.Open, access, HostShare);
            _store = taken;
            // Taken ONCE: the store's pages are read and written positionally through it (never through the stream's
            // buffer, kb/Work PB2660), and it is the handle the store mutex and this connector's record locks are held by.
            _handle = taken.SafeFileHandle;
            // Writable is what the host granted this handle, never re-derived from the open mode (kb/Work PB2758).
            _regions = HostRegionLocks.Open(_handle, HostPath, writable: taken.CanWrite);
        }
        // ⛔ The engine writes at every statement, so it serializes against EVERY other run unit's connector — a
        // SHARING WITH READ ONLY writer against the INPUT reader §9.1.15 2) admits beside it (2.2.1.7, finding R2-6) —
        // and only SHARING WITH NO OTHER runs without the mutex.
        _coherent = FileLockPosture.AdmitsAnotherRunUnit(HostShare);
    }

    /// <summary>Give the file lock back — §9.1.15, <i>"The file lock is removed by an explicit or implicit CLOSE
    /// statement executed for that file connector"</i>. Runs from <see cref="CloseCore"/>'s finally (whatever the
    /// promotion did) and from <see cref="AbandonOpen"/>.</summary>
    private void ReleaseFileLock()
    {
        // The field is cleared whatever the disposal throws (a final flush refused on an exhausted medium — the
        // disposal still closes the host handle), so a later CLOSE or re-OPEN never meets a disposed handle.
        try { _regions?.Dispose(); }   // the region locks first, while the handle they were taken through is still open
        finally
        {
            try { _store?.Dispose(); }
            finally { _store = null; _handle = null; _regions = null; _coherent = false; }
        }
    }

    // ── The lifecycle, written once for both organizations (kb/Work PB2753) ──────────────────────────────────

    /// <summary>The store this connector is attached to — the run unit's ONE store for the physical file
    /// (kb/Work PB143) — or a placeholder before the OPEN attaches and after the CLOSE detaches.</summary>
    private protected abstract KeyedStore AttachedStore { get; }

    /// <summary>Attach the organization's store for <see cref="FileConnector.HostPath"/> (the table's live one, or a
    /// private one for a standalone connector), <paramref name="create"/> for <c>OPEN OUTPUT</c>'s new, EMPTY store
    /// (<see cref="StoreTable.AttachCreated{T}"/>, kb/Work PB754).</summary>
    private protected abstract KeyedStore AttachStore(bool create);

    /// <summary>The host's identity of the physical file this connector's handle is open on (<see cref="HostFile.IdentityOf"/>), or
    /// null with no handle: what the run unit's table tells one physical file at the path from another by (<see cref="StoreTable"/>,
    /// kb/Work PB3533). Asked once per attachment.</summary>
    private protected PhysicalFileIdentity? HandleIdentity => _handle is { } handle ? HostFile.IdentityOf(handle) : null;

    /// <summary>A new, EMPTY store of this organization that no table and no connector holds yet: the store an <c>OPEN OUTPUT</c>
    /// over a file its connector's CLOSE under commit control kept is created in beside the kept one (<see cref="Recreate"/>).</summary>
    private protected abstract KeyedStore NewStore();

    /// <summary>Make <paramref name="created"/>, a store of <see cref="NewStore"/> whose file has been created, the store of the
    /// run unit's table for <see cref="FileConnector.HostPath"/> (<see cref="StoreTable.AttachCreated{T}"/>) and this connector's.</summary>
    private protected abstract void AttachCreated(KeyedStore created);

    /// <summary>Forget the store the connector was attached to (it holds a placeholder again).</summary>
    private protected abstract void ForgetStore();

    /// <summary>The organization's position state reset at the start of every OPEN (§14.9.27.4 GR14's key of reference
    /// and position are set by the arm that succeeds).</summary>
    private protected abstract void ResetPosition();

    /// <summary>The organization's last step of a successful OPEN arm in <paramref name="mode"/> — §14.9.27.4 GR14's and
    /// GR15's positioning; the failing I-O status (a torn page met while positioning: '30'), or null.</summary>
    private protected abstract string? Opened(FileOpenMode mode);

    /// <summary>Whether this connector is attached to <see cref="AttachedStore"/> (and its handle to the store's medium).</summary>
    private bool _attached;

    /// <summary>The paged store of the run unit's store this connector is attached to, or null while it is not attached
    /// or the file holds no store: the seam of DESIGN-runtime-library.md 2.2.1.11's witnesses (in docs/rearchitecture;
    /// kb/Work PB3336), which read its <see cref="PagedFile.Counters"/> and drop its cache cold.</summary>
    internal PagedFile? PagedStore => _attached ? AttachedStore.File : null;

    /// <summary>The file position indicator's validity bit (§9.1.11; §14.9.41.4 GR7 clears it), the one piece of the
    /// position both organizations keep alike.</summary>
    private protected bool FpiValid { get; set; }

    /// <summary>The keyed OPEN body (§14.9.27). GR14: INPUT/I-O position the file position indicator at the start of
    /// the key of reference (the organization's <see cref="Opened"/>); GR15: EXTEND positions after the highest; GR17: an
    /// absent OPTIONAL file on I-O/EXTEND is created ('05'); §9.1.13.6 item 5: an absent NON-optional file on
    /// INPUT/I-O/EXTEND is '35' — the already-open '41' guard, the attempted-mode bookkeeping, the position reset, the
    /// ONE presence probe behind <paramref name="presence"/>, GR3's authority '37' and the '37'/'30' exception mapping
    /// live on <see cref="FileConnector.Open"/>.
    /// <para>The store is adopted or created under the store mutex (2.2.1.7, R6): a damaged store — both committed
    /// slots fail — or a refused read, write or sync is §9.1.13.6 item 1's '30', and the OPEN is unsuccessful
    /// (<see cref="AbandonOpen"/> detaches what it attached).</para></summary>
    protected sealed override string OpenCore(FileOpenMode mode, FilePresence presence)
    {
        ResetPosition();
        (_begunByOpen, _openChangedFile, _fileRemoved) = (null, false, false);
        // Table 18's availability axis; GR3 has already turned an Unauthorized probe into '37' upstream, so
        // a refusal can never be read here as "unavailable" (kb/Work PB323).
        bool exists = presence is FilePresence.Present;
        string status = FileStatusCode.Success;
        switch (mode)
        {
            case FileOpenMode.Input:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    OptionalAbsent = true;               // positioned "not present" (§14.9.27 GR13)
                    // NO file lock: §9.1.15 establishes one on a PHYSICAL FILE, and there is none here. What a CLOSE under commit
                    // control kept belonged to a file that is not there any more (removed or renamed away outside the run unit): the
                    // OPEN handed it to the registry before this body (RetireWhatWasKeptOfAnotherFile), so nothing of its store
                    // describes this one and this OPEN attaches to none of it.
                    AttachStore(create: false);          // no file, no records
                    _attached = true;
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                if ((Lock(create: false) ?? Attach(create: false)) is { } refusedInput) return refusedInput;   // §9.1.15 — the lock, before the store is read through it
                break;
            case FileOpenMode.Output:
                // §14.9.27.4 GR18 — OUTPUT creates the physical file: a new, EMPTY store (kb/Work PB754). Under commit control an
                // EXISTING file is captured first (R4, through this OPEN's own handle) and an absent one's creation journaled
                // before the handle creates it (R1): 2.2.2.14.
                var refusedOutput = exists
                    ? Lock(create: true) ?? JournalCapture() ?? Attach(create: true)
                    : JournalCreation() ?? LockCreating() ?? Attach(create: true);
                if (refusedOutput is not null) return refusedOutput;
                break;
            case FileOpenMode.IO:
            case FileOpenMode.Extend:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    // Created as if OPEN OUTPUT + CLOSE (§14.9.27 GR17): the file the lock just created has no bytes,
                    // and the adoption of a file of no bytes through a writable handle creates the empty store.
                    if ((JournalCreation() ?? LockCreating()) is { } refusedCreated) return refusedCreated;
                    // The file the kept state belonged to is not there (it was removed or renamed away outside the run unit): the
                    // file this OPEN created is a different one, and nothing of the kept store describes it. The OPEN handed the
                    // kept state to the registry before this body (RetireWhatWasKeptOfAnotherFile), whole, so a refused handle
                    // leaves it as the CLOSE left it (§14.9.27.4 GR25) and the COMMIT still reaches it.
                    if (Attach(create: false) is { } failedCreated) return failedCreated;
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                if ((Lock(create: false) ?? Attach(create: false)) is { } refused) return refused;
                break;
        }
        FpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        if (Opened(mode) is { } unpositioned) return unpositioned;
        CarryKeptOver();   // the OPEN succeeded: what a CLOSE under commit control kept is this OPEN's now
        (_begunByOpen, _openChangedFile) = (null, false);   // the journal this OPEN began is the run unit's transaction now
        return status;
    }

    // ── The whole file's changes under commit control: the run unit's transaction journal (2.2.2.14; kb/Work PB3492) ──

    /// <summary>The run unit's transaction journal on the file, when THIS OPEN began it (<see cref="JournalCreation"/>,
    /// <see cref="JournalCapture"/>), until the OPEN ends: an unsuccessful OPEN that did not affect the file withdraws it
    /// (<see cref="AbandonOpen"/>).</summary>
    private SequentialJournal? _begunByOpen;

    /// <summary>Whether this OPEN changed the physical file: its handle created the file, or its creation replaced the store
    /// (<see cref="KeyedStore.FileReplaced"/>). An unsuccessful OPEN that did is not withdrawn: its journal stands, and item 8's
    /// ROLLBACK (§14.6.13.1.3 8), the statement's '30' being fatal) restores the file from it.</summary>
    private bool _openChangedFile;

    /// <summary>⛔ A CREATION UNDER COMMIT CONTROL IS JOURNALED BEFORE THE FILE EXISTS (2.2.2.2 (e), 2.2.2.5 R1, 2.2.2.14): an OPEN that
    /// creates an absent file — OUTPUT, or I-O or EXTEND of an OPTIONAL one (§14.9.27.4 GR17, GR18) — begins the run unit's
    /// transaction on it with no file at its start (START <c>A</c>), under the entry mutex (<see cref="FileConnector.EnterPhysicalFile"/>),
    /// so its ROLLBACK removes the file: §14.9.36.4 GR3 restores <i>"the state they were in at the execution of the last COMMIT
    /// statement or the start of the run unit"</i>, and that state had no file. A transaction that stands has journaled what it
    /// needs already. Returns null; throws on a refusal (the OPEN's '30', the file not created).</summary>
    private string? JournalCreation()
    {
        if (!CommitControlled || Journal is not null) return null;
        _begunByOpen = Transactions.Begin(HostPath, SequentialJournal.StartState.Absent, ofKeyedFile: true);
        return null;
    }

    /// <summary>⛔ THE RUN UNIT'S ROLLBACK REMOVED THE FILE A CREATION SINCE THE LAST COMMIT MADE, AND THIS CONNECTOR STAYED OPEN
    /// (§14.9.36.4 GR3, <i>"restored to the state they were in at the execution of the last COMMIT statement or the start of the
    /// run unit"</i>, a state with no file; DESIGN-runtime-library.md 2.2.2.14; kb/Work PB3492, review finding R1-2; the
    /// sequential organization's twin is <c>SequentialConnector</c>'s own): the connector keeps its open mode on no file
    /// (<see cref="LeaveTheRemovedFile"/>). A READ or START reads it as an absent OPTIONAL file (<see cref="FileConnector.OptionalAbsent"/>:
    /// '10' at end, '23' for a key), a REWRITE or DELETE finds no record, and its next WRITE creates the file again, as its OPEN
    /// did, beginning a new transaction with no file at its start (<see cref="WriteCreatingTheFileAgain"/>).</summary>
    private bool _fileRemoved;

    /// <inheritdoc cref="_fileRemoved"/>
    private protected bool FileRemovedByRollback => _fileRemoved;

    /// <summary>The ROLLBACK is about to remove the file this OPEN connector stands on (<c>FileRegistry.KeyedJournalData</c>), its run
    /// unit's transaction in it rolled back already: the store is left and the handle given back (a Windows handle not opened
    /// for deletion refuses the removal), the Table 19 column suspended (<see cref="FileConnector.SuspendRunUnitFileLock"/>), and
    /// the connector, still open, holds the absent file's placeholder store (OpenCore's absent OPTIONAL INPUT arm).</summary>
    internal void LeaveTheRemovedFile()
    {
        if (!IsOpen || _fileRemoved) return;
        try
        {
            try { _ = Detach(); }
            finally { ReleaseFileLock(); }
        }
        finally
        {
            SuspendRunUnitFileLock();
            (_fileRemoved, OptionalAbsent) = (true, true);   // the file position indicator stays as it was (§14.9.36.4 GR3 leaves the open mode)
            AttachStore(create: false);   // no file, no records
            _attached = true;
        }
    }

    /// <summary>⛔ A WRITE THROUGH A CONNECTOR WHOSE FILE THE ROLLBACK REMOVED (<see cref="_fileRemoved"/>) CREATES IT AGAIN, AS ITS OPEN
    /// DID (§14.9.27.4 GR17, GR18; the sequential twin is <c>SequentialConnector.BeginAppending</c>), then runs
    /// <paramref name="write"/> against the file it created, inside the statement of its store (2.2.1.7: a connector with no file
    /// entered none). Each organization's WRITE calls it at its last check before the record is released, so a WRITE the
    /// absent file refuses (its record rules, its keys, its locks) creates nothing.</summary>
    private protected string WriteCreatingTheFileAgain(Func<string> write)
    {
        if (CreateTheFileAgain() is { } refused) return Status = refused;
        using (BeginStatement()) return write();
    }

    /// <summary>The creation of <see cref="WriteCreatingTheFileAgain"/>, under the entry mutex: the journal begun with no file at its
    /// start (START <c>A</c>, <see cref="JournalCreation"/>), the creating handle taken and the empty store created
    /// (<see cref="LockCreating"/>, <see cref="Attach"/>), and the Table 19 column published again. A file another run unit made at
    /// the path meanwhile is not this WRITE's to replace: '30' (§9.1.13.6), as for any refusal. An unsuccessful creation leaves the
    /// connector on no file, the journal withdrawn when the file was not created and otherwise left to item 8's ROLLBACK, which
    /// removes it. Returns the failing I-O status, or null.</summary>
    private string? CreateTheFileAgain()
    {
        string? refused;
        RegionLocks? entry = null;
        try
        {
            if (CommitControlled) entry = Transactions.HoldEntryMutex(HostPath);
            _ = Detach();   // the placeholder
            (_begunByOpen, _openChangedFile) = (null, false);
            refused = HostFile.Probe(HostPath) is FilePresence.Absent
                ? JournalCreation() ?? LockCreating() ?? Attach(create: true)
                : FileStatusCode.PermanentError;
            if (refused is null && !ResumeRunUnitFileLock()) refused = FileStatusCode.PermanentError;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { refused = FileStatusCode.PermanentError; }
        try
        {
            if (refused is null)
            {
                (_fileRemoved, OptionalAbsent) = (false, false);
                return null;
            }
            if (_begunByOpen is { } begun && !_openChangedFile) Transactions.Withdraw(begun);
            try { _ = Detach(); }
            catch (IOException) { }
            finally { ReleaseFileLock(); }
            AttachStore(create: false);
            _attached = true;
            return refused;
        }
        finally
        {
            (_begunByOpen, _openChangedFile) = (null, false);
            entry?.Dispose();
        }
    }

    /// <summary>The creating arms' file lock (<see cref="Lock"/>): a handle taken with the creating mode creates the file, which
    /// this OPEN has then changed (<see cref="_openChangedFile"/>).</summary>
    private string? LockCreating()
    {
        var refused = Lock(create: true);
        if (refused is null) _openChangedFile = true;
        return refused;
    }

    /// <summary>⛔ THE CAPTURE OF AN OPEN OUTPUT OF AN EXISTING FILE UNDER COMMIT CONTROL (2.2.2.5 R4, 2.2.2.14; kb/Work PB3492): the
    /// keyed store's transaction cannot undo the creation (§14.9.27.4 GR18: <i>"After the creation of the file, the file contains no
    /// records"</i>), whose pages hold the before-images, so the file's bytes are copied — as they stand, the run unit's uncommitted
    /// changes and their UNDO pages included — through THIS OPEN's own handle into the journal's before file, synced, and the
    /// <c>K</c> record synced, all before the creation runs (<see cref="Attach"/>, <see cref="Recreate"/>). Under the entry mutex,
    /// with no other connector's handle on the file (Table 19's OUTPUT row). Nothing for a transaction already replaced (a second
    /// OPEN OUTPUT, or one after a DELETE FILE). Returns null; throws on a refusal (the OPEN's '30', the file unaffected).</summary>
    private string? JournalCapture()
    {
        if (!CommitControlled) return null;
        var journal = Journal;
        if (journal is { Replaced: true }) return null;
        journal ??= _begunByOpen = Transactions.Begin(HostPath, SequentialJournal.StartState.Present, ofKeyedFile: true);
        journal.Capture(SequentialJournal.CaptureVerb.OpenOutput, _store!);
        return null;
    }

    /// <summary>The handle a ROLLBACK of the file's journal restores it through (<c>FileRegistry.RollbackKeyed</c>): this connector's
    /// own when it is open and writable, else the one its CLOSE kept, if writable; null when it holds none.</summary>
    internal FileStream? WritableHandle => _store is { CanWrite: true } own ? own : _kept?.WritableStream;

    /// <summary>⛔ The ROLLBACK of the file's journal restored the physical file from its before file (2.2.2.14): the run unit's store
    /// of it adopts it again through <paramref name="through"/> and rolls back the transaction the restored file holds
    /// (<see cref="KeyedStore.AdoptRestored"/>) — through this connector's handle when it is open, else through what its CLOSE kept
    /// (<see cref="KeptFile.AdoptRestored"/>). A connector attached to no store (no connector of the run unit holds the file: its
    /// OPEN OUTPUT was refused after the creation replaced the file) rolls the restored file's transaction back through a store
    /// made for that alone, which no table holds.</summary>
    internal StoreStatus AdoptRestored(FileStream through)
    {
        if (_regions is null && _kept is { } kept) return kept.AdoptRestored(through);
        var store = _attached ? AttachedStore : NewStore();
        return store.AdoptRestored(through.SafeFileHandle, ReferenceEquals(_store, through) ? _regions : null, HostPath,
            Restoring, HoldingsOrder);
    }

    /// <inheritdoc/>
    /// <remarks>The handle goes off the kept store, the transaction's liveness byte with it (<see cref="KeptFile.GiveBack"/>); on
    /// Unix its shared lock refuses the probe's exclusive one too. Taken back (<see cref="RetakeKeptHandle"/>: attached to the
    /// store again, the liveness byte held through it) unless the file is removed; once it is, the transaction lived in the file
    /// the journal's before file now holds (2.2.2.14), so the store has nothing of it left to promote and it ends there
    /// (<see cref="KeyedStore.ForgetTransaction"/>); a ROLLBACK rolls it back in the restored file. The attachment stays until the
    /// COMMIT or ROLLBACK.</remarks>
    internal override RemovalGiveBack? GiveBackForRemoval()
    {
        if (_kept is not { Adopted: false } kept) return null;
        var givenBack = new KeptGivenBack(this, kept);
        if (kept.Handle is not { } handle) return givenBack;
        _kept = kept with { Handle = null };
        try { GiveBack(handle); }
        catch
        {
            givenBack.Dispose();
            throw;
        }
        return givenBack;
    }

    /// <summary>What <see cref="GiveBackForRemoval"/> gave back of <paramref name="kept"/>, the kept state as the CLOSE left it.</summary>
    private sealed class KeptGivenBack(KeyedConnector owner, KeptFile kept) : RemovalGiveBack
    {
        internal override void SuspendFileLock() => kept.FileLock?.Suspend();

        private protected override void TakeBack()
        {
            if (kept.Handle is { } handle) owner.RetakeKeptHandle(handle, unpublished: null);   // no OPEN handle took the record locks: Attach moves them
            if (kept.FileLock is { } fileLock && !fileLock.Resume() && owner._kept is { } now) owner._kept = now with { FileLock = null };
        }

        private protected override void Landed()
        {
            kept.FileLock?.Dispose();
            if (owner._kept is { } now) owner._kept = now with { FileLock = null };
            kept.Store.ForgetTransaction();
        }
    }

    /// <summary>The file lock of an arm that has a physical file (<see cref="TakeFileLock"/>), taken only for a declaration
    /// the physical format can hold (<see cref="FormatHolds"/>): one it cannot is §14.9.27.4 GR10's fixed file attribute
    /// conflict, '39', answered before any file is touched (GR25, <i>"If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected"</i>). Returns the failing I-O status, or null.</summary>
    private string? Lock(bool create)
    {
        if (!FormatHolds()) return FileStatusCode.FixedAttributeConflict;
        // While the run unit's transaction on the file STANDS and a handle its CLOSE under commit control kept is WRITABLE,
        // the re-OPEN's handle is writable whatever its mode: it is the handle the COMMIT or ROLLBACK writes through once
        // the kept one is given back, and the one the liveness byte moves to (2.2.1.10), and an INPUT re-OPEN takes the
        // same handle back (TakeFileLock). With no transaction there is nothing to write, so the handle takes the access
        // of its own mode (HostAccess) and no more (review finding R2-1): a writable handle under an INPUT re-OPEN would be
        // refused by the host share mode of the OPENs of other run units that Table 19 admits beside an INPUT opener
        // (§9.1.15). The handle that stands writable while the transaction does leaves the one case a share mode cannot say, an
        // INPUT opener that owes its transaction's writes, to the OPEN that meets it: a SHARING WITH READ ONLY request is
        // refused by the share mode, with its column held, and made again with FileLockPosture.WhenOwedWritesAreAdmitted
        // (FileConnector.Open, kb/Work PB3482), which the connector's column says by owing writes (FileConnector.OwesWrites, kb/Work PB3499).
        var access = HandleAccess(Mode);
        try { TakeFileLock(create, access); }
        catch (IOException refusal) when (HostFile.IsSharingRefusal(refusal) && _kept is { Handle: { } kept, Adopted: false })
        {
            // The handle this connector kept at its CLOSE under commit control may refuse the new one's share mode (§9.1.15
            // speaks of OTHER run units, never of the connector's own re-OPEN), and the host's refusal does not say whose
            // handle refused: the kept one's, or another run unit's, which the kept one admitted. So the HANDLE is given back
            // and the new one tried again — the attachment, and with it the run unit's transaction, stays (review finding
            // R1-1), and the store takes the transaction's liveness byte again through the new handle at the attach
            // (KeyedStore.HoldLivenessThrough). ⛔ A re-OPEN that is unsuccessful after that — the refusal persists because it
            // is another run unit's, or the attach or the positioning fails — must not cost the connector what the CLOSE kept
            // (§14.9.27.4 GR25; §14.9.7.4 GR4): AbandonOpen takes the handle again as it was (RetakeKeptHandle). Any other
            // IOException leaves the kept handle where it is.
            _kept = _kept with { Handle = null };
            _givenBack = kept;
            GiveBack(kept);
            TakeFileLock(create, access);
        }
        return null;
    }

    /// <summary>⛔ THE ACCESS THE HANDLE OF AN OPEN IN <paramref name="mode"/> TAKES, in ONE place for the two questions that depend
    /// on it (kb/Work PB3499): <see cref="Lock"/>, which opens the handle with it, and <see cref="WillTakeWritableHandle"/>, which
    /// the file lock publishes as "owes writes" when the mode is INPUT (<c>FileConnector.OwesWrites</c>). While the run unit's transaction on the file STANDS and the
    /// handle a CLOSE under commit control kept is WRITABLE, the re-OPEN's handle is writable whatever its mode
    /// (<see cref="Lock"/>'s determination); otherwise it takes <see cref="HostAccess"/> of its own mode and no more.
    /// <para>⛔ So is it while the run unit's journal on the file stands REPLACED (2.2.2.14; kb/Work PB3492): its ROLLBACK restores the
    /// file from the before file through a handle of the run unit, and an INPUT re-OPEN's read-only handle could neither serve as
    /// that one nor admit another beside it.</para></summary>
    private FileAccess HandleAccess(FileOpenMode mode) =>
        _kept?.Handle is { Stream.CanWrite: true } && AttachedStore.Transaction != 0 || Journal is { Replaced: true }
            ? FileAccess.ReadWrite : HostAccess(mode);

    /// <inheritdoc/>
    private protected sealed override bool WillTakeWritableHandle(FileOpenMode mode) =>
        HandleAccess(mode).HasFlag(FileAccess.Write);

    /// <summary>The handle this OPEN took from <see cref="_kept"/> to try a share mode it refused (<see cref="Lock"/>), until
    /// the OPEN succeeds (<see cref="CarryKeptOver"/>) or <see cref="AbandonOpen"/> takes it back.</summary>
    private KeptHandle? _givenBack;

    /// <summary>⛔ PUT THE HANDLE A CLOSE UNDER COMMIT CONTROL KEPT BACK, as <see cref="GiveBack"/> left it (kb/Work PB3483): the
    /// re-OPEN gave it away to take another, and the re-OPEN is unsuccessful, so §14.9.27.4 GR25's <i>"the file is not
    /// affected"</i> means the kept state is what the CLOSE left — the file lock (§9.1.15, §14.9.7.4 GR4) and the
    /// transaction's liveness byte (2.2.1.10), which a handle that is not there holds for nobody, so that the next writable
    /// OPEN of another run unit rolls back a transaction whose owner lives. The handle is taken with the access and share
    /// mode it had and attached to the store exactly as an OPEN attaches its own (<see cref="Attach"/>), then handed back to
    /// <see cref="_kept"/>. Runs after this OPEN's own handle is given back, which would otherwise refuse it.
    /// <para>⛔ The record locks the CLOSE kept (§14.9.6.4 GR9) are part of that state: the retaken handle's endpoint becomes
    /// the one they are published through (<see cref="KeptFile.Endpoint"/>) — <see cref="Attach"/> moves to it those the given-back
    /// endpoint still names, and <paramref name="unpublished"/>, the ones <see cref="AbandonOpen"/> took off this OPEN's own
    /// handle, are published through it again (<see cref="PhysicalFileTable.Republish"/>). A retake the HOST refuses leaves them in
    /// this run unit's table alone, until the COMMIT or the ROLLBACK; one whose store the MEDIUM refuses to adopt again keeps the
    /// handle and carries them all the same (<see cref="CarryKeptHoldings"/>, kb/Work PB3497).</para>
    /// <para>The instant between the give-back and this retake cannot be closed on a host whose share modes are per handle: a
    /// handle that refuses the new one is a handle the new one cannot be beside. Another run unit that opened in that instant
    /// holds a handle the kept share mode refuses, so the retake is refused too and the kept state is left without its
    /// handle (the OPEN's own unsuccessful status stands). The Table 19 column lock has no such instant
    /// (<see cref="TakeKeptFileLockOver"/>).</para></summary>
    private void RetakeKeptHandle(KeptHandle gone, List<string>? unpublished)
    {
        if (_kept is null) return;   // the kept state ended meanwhile (ReleaseKept): there is nothing to put it into
        bool retaken = false;
        FileStream? stream = null;
        try
        {
            stream = HostFile.OpenConnectorStore(HostPath, FileMode.Open, gone.Access, gone.Share);
            var handle = stream.SafeFileHandle;
            (_store, _handle, _regions, _coherent) = (stream, handle, HostRegionLocks.Open(handle, HostPath, writable: stream.CanWrite),
                FileLockPosture.AdmitsAnotherRunUnit(gone.Share));
            stream = null;   // the connector's now
            // ⛔ THE HANDLE IS RETAKEN WHETHER OR NOT THE MEDIUM LETS THE STORE BE ADOPTED AGAIN (kb/Work PB3497): the giving
            // back detached the store's last handle, so Attach reads its pages anew, and a medium that refuses that read
            // (§9.1.13.6 item 1) fails the Attach before it carries the holdings across. What the CLOSE kept is a handle's —
            // the share mode that is the file lock, the transaction's liveness byte, the record locks — and none of it
            // depends on a page read, so the unsuccessful OPEN leaves it standing all the same (§14.9.27.4 GR25; §14.9.6.4
            // GR9): without the liveness byte another run unit's writable OPEN would roll back a transaction whose owner
            // lives, and without the endpoint its READ of a record the CLOSE kept locked would answer '00'. The store stays
            // unadopted (no file) until the connector's next OPEN reads it again.
            if (Attach(create: false) is not null)
                InStoreMutex(() =>
                {
                    CarryKeptHoldings(AttachedStore);
                    return StoreStatus.Ok;
                });
            var regions = _regions;
            if (unpublished is { Count: > 0 } && Physical is { } physical)
                InStoreMutex(() =>
                {
                    PhysicalFileTable.Republish(physical, unpublished, regions);
                    return StoreStatus.Ok;
                });
            _kept = _kept with { Handle = new KeptHandle(_store, _handle, regions, gone.Access, gone.Share), Endpoint = regions };
            (_store, _handle, _regions, _coherent) = (null, null, null, false);
            retaken = true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        finally
        {
            stream?.Dispose();
            if (!retaken)
            {
                // The retake is refused (the window) or its attach failed: the handle goes off the store again.
                try { _ = Detach(); }
                catch (IOException) { }
                finally { ReleaseFileLock(); }
            }
        }
    }

    /// <summary>Whether the physical format can hold this connector's declaration (docs/CONFORMANCE.md DOC-A.1-129): its
    /// key descriptors within the meta page (<see cref="MetaPage.Fits"/>, 2.2.1.4), and each organization's own bounds.</summary>
    private protected virtual bool FormatHolds() => MetaPage.Fits(DeclaredAttributes);

    /// <summary>Attach the per-physical-file store (kb/Work PB143) and open it through this connector's handle: the
    /// LIVE store when another connector of the run unit holds this host open (its trees are the truth — never re-read,
    /// but opened again through a writable handle when it had none, which fences), else a new one adopted from the
    /// file (2.2.1.6 R6) or, for <paramref name="create"/> — <c>OPEN OUTPUT</c> — created empty, held by no other
    /// connector. Returns the failing I-O status, or null.</summary>
    private string? Attach(bool create)
    {
        if (create && _kept is not null) return Recreate();
        // ⛔ THE ATTACHMENT A CLOSE UNDER COMMIT CONTROL KEPT CARRIES OVER TO THE CONNECTOR'S RE-OPEN (review finding
        // R1-1): the run unit's store — and in it the run unit's transaction on the file — stays this connector's, never a
        // fresh one with no records and no transaction; a handle the re-OPEN adopted is on its medium already.
        var store = _kept is null ? AttachStore(create) : AttachedStore;
        _attached = true;
        if (_kept is not { Adopted: true }) AttachHandleTo(store);
        var opened = InStoreMutex(() =>
        {
            var status = create
                ? store.Create(DeclaredAttributes, _coherent, HoldingsOrder)
                : store.Open(StoreHandleWritable, _coherent, DeclaredAttributes, HoldingsOrder);
            if (status != StoreStatus.Ok) return status;
            // R6 step 5, the DEAD-OWNER step of a writable OPEN (2.2.1.6, 2.2.1.10): after the adoption and its fence. It
            // never touches this run unit's own transaction.
            if (StoreHandleWritable && (status = store.RollBackDeadOwners(_regions!, Restoring)) != StoreStatus.Ok)
                return status;
            // The run unit's open transaction keeps its liveness byte through this handle from here: the kept one's moves,
            // or one given back with it is taken again, inside this mutex hold, so no other run unit's probe ever finds it
            // free (2.2.1.10).
            CarryKeptHoldings(store);
            return status;
        });
        if (create) _openChangedFile |= store.FileReplaced;
        return Failed(opened);
    }

    /// <summary>⛔ THE HOST-LEVEL HOLDINGS A CLOSE UNDER COMMIT CONTROL KEPT MOVE TO THIS CONNECTOR'S HANDLE (§14.9.6.4 GR9; §14.9.7.4
    /// GR4; 2.2.1.10): the run unit's open transaction keeps its liveness byte through this handle from here — the kept one's
    /// moves, or one given back with it is taken again — and so do the record locks the CLOSE kept: they move to the new
    /// handle's endpoint, before the kept one is given back and takes every byte it holds with it. The caller holds the store
    /// mutex, so no other run unit's probe, lock test or draw finds a byte free in between. Neither step reads the store's
    /// pages: they hold with the handle alone, which is why <see cref="RetakeKeptHandle"/> takes them whether or not the medium
    /// lets the store be adopted again.</summary>
    private void CarryKeptHoldings(KeyedStore store)
    {
        store.HoldLivenessThrough(_regions!);
        if (_kept is { Adopted: false } carried && Physical is { } physical)
            PhysicalFileTable.Rehome(physical, carried.Endpoint, _regions!);
    }

    /// <summary>⛔ <c>OPEN OUTPUT</c> OVER THE FILE ITS CONNECTOR'S CLOSE UNDER COMMIT CONTROL KEPT (§14.9.27.4 GR18, <i>"the
    /// successful execution of the OPEN statement creates the file"</i>; GR25, <i>"If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected"</i>; kb/Work PB3493): the new, EMPTY store is created in a store of its own,
    /// through this OPEN's handle, BESIDE the kept attachment, and what the CLOSE kept ends only once the creation has landed.
    /// The creation is undoable (<see cref="PagedFile.Create"/> puts every page it wrote back and answers no store), so an
    /// OUTPUT the medium refuses leaves the kept transaction, its record locks, its file lock and its handle as the CLOSE left
    /// them, and the next COMMIT or ROLLBACK still reaches the file. A creation that landed ends the transaction with the file
    /// it was in (the new file has no records and holds nothing), so the kept state is given back then and the created store
    /// becomes the run unit's for the path. Two refusals leave the previous file gone all the same, and the kept state ends with
    /// it while the OPEN answers '30' (<see cref="KeyedStore.FileReplaced"/>): the final sync's, after the truncation, and a
    /// refusal whose UNDO the medium refused too (review finding R1-1) — kept, the transaction would stand over a store the
    /// file no longer holds, and its COMMIT would promote it beside the creation's slot and answer success over a file R6 reads
    /// as the empty store.
    /// <para>⛔ Ending the kept state does not lose the transaction (2.2.2.14; kb/Work PB3492): the OPEN CAPTURED the file before the
    /// creation (<see cref="JournalCapture"/>), so the before file holds the transaction as it stood, and a ROLLBACK restores the
    /// file and rolls it back there (<see cref="AdoptRestored"/>); a COMMIT makes the replacement permanent.</para>
    /// <para>Not inside the store mutex: giving the kept handle back takes it through the kept endpoint, a different open file
    /// description from this OPEN's own.</para></summary>
    private string? Recreate()
    {
        // This OPEN's handle is on the kept store until the creation has landed, as a re-OPEN's is, so that AbandonOpen takes it off
        // and moves the transaction's liveness byte and the record locks back in the one way it does for every unsuccessful re-OPEN.
        AttachHandleTo(AttachedStore);
        var created = NewStore();
        AttachHandleTo(created);
        var status = InStoreMutex(() => created.Create(DeclaredAttributes, _coherent, HoldingsOrder));
        _openChangedFile |= created.FileReplaced;
        if (status != StoreStatus.Ok)
        {
            if (created.FileReplaced) ReleaseKept();
            return Failed(status);
        }
        ReleaseKept();
        AttachCreated(created);
        _attached = true;
        return Failed(InStoreMutex(() => StoreHandleWritable ? created.RollBackDeadOwners(_regions!, Restoring) : StoreStatus.Ok));
    }

    /// <summary>Attach this connector's handle (its §9.1.15 file lock) to <paramref name="store"/> with its input-output areas as the
    /// page cache's floor (§12.4.5.14.3 GR1) - the ONE place the keyed lifecycle sizes them (kb/Work PB3333).</summary>
    private void AttachHandleTo(KeyedStore store) =>
        store.AttachHandle(_handle!, StoreHandleWritable, HostFile.InputOutputAreaBuffer(InputOutputAreas) / PageHeader.PageSize);

    /// <summary>Leave the store: this connector's handle off its medium, and the table's attachment given back.
    /// <para>⛔ UNDER THE STORE MUTEX (kb/Work PB3335): when this was the store's last writable handle and a reader stays,
    /// the store is opened again as the reader's (<see cref="KeyedStore.DetachHandle"/>, 2.2.1.6 R6 in memory), and that
    /// adoption reads pages 0, 1 and 2 — which another run unit's statement may be writing. 2.2.1.7 puts every adoption
    /// <i>"inside the store mutex"</i>; this connector's handle is still open here, so it holds the mutex for it.
    /// Returns that adoption's status, which the CLOSE answers (2.2.1.16's refused READ row: '30').</para></summary>
    private StoreStatus Detach()
    {
        if (!_attached) return StoreStatus.Ok;
        var store = AttachedStore;
        var status = _handle is { } handle && _regions is { } regions
            ? InStoreMutex(() =>
            {
                store.LivenessGoneWith(regions);   // the endpoint is disposed next (ReleaseFileLock)
                return store.DetachHandle(handle);
            })
            : StoreStatus.Ok;
        // A failed re-OPEN after a CLOSE under commit control leaves the attachment the CLOSE kept as it was (§14.9.27.4
        // GR25, "the file is not affected"): it is still the kept one's, until the COMMIT or ROLLBACK.
        if (_kept is not null) return status;
        SharedStores?.Detach(HostPath, store);
        _attached = false;
        ForgetStore();
        return status;
    }

    /// <inheritdoc/>
    protected sealed override void AbandonOpen()
    {
        // ⛔ A journal this OPEN began for its creation or capture is WITHDRAWN when the file was not affected (§14.9.27.4 GR25,
        // "If the execution of the OPEN statement is unsuccessful, the file is not affected"; 2.2.2.14): no transaction began on
        // it, and a kept transaction stands as the CLOSE left it. One whose file the OPEN changed stands for item 8's ROLLBACK.
        if (_begunByOpen is { } begun && !_openChangedFile) Transactions.Withdraw(begun);
        (_begunByOpen, _openChangedFile) = (null, false);
        // A re-OPEN that ADOPTED the handle its CLOSE under commit control kept gives it back to the kept state unchanged,
        // its liveness byte with it (§14.9.27.4 GR25); the kept column lock this OPEN replaced goes back to the kept state
        // (and its old column) in FileConnector.Open, after this (TryRestoreKeptFileLock).
        if (_kept is { Adopted: true } adopted)
        {
            _kept = adopted with { Adopted = false };
            (_store, _handle, _regions, _coherent) = (null, null, null, false);
            return;
        }
        // One that took a handle of its own beside the kept one moves the transaction's liveness byte, and the record locks
        // Attach moved to it, back to the kept handle before its own goes.
        if (_kept is { Handle: { } kept } && _attached)
            InStoreMutex(() =>
            {
                AttachedStore.HoldLivenessThrough(kept.Regions);
                if (Physical is { } physical && _regions is { } mine) PhysicalFileTable.Rehome(physical, mine, kept.Regions);
                return StoreStatus.Ok;
            });
        // A kept handle given back early has no endpoint to move them to while this OPEN's own handle is on the file: they
        // leave the host with that handle, as the liveness byte does (the share-mode window, KeyedStore.LivenessGoneWith), but
        // they stay this run unit's, in its table, until the COMMIT or the ROLLBACK (§14.9.27.4 GR25, §14.9.6.4 GR9; review
        // finding R1-2) — and are published again through the kept handle when it is taken again below (RetakeKeptHandle).
        List<string>? unpublished = null;
        if (_kept is { Handle: null } && Physical is { } stale && _regions is { } own)
            unpublished = PhysicalFileTable.Unpublish(stale, own);
        // The OPEN already answers its own failure; a refused re-adoption here leaves the store's fence owed
        // (KeyedStore.DetachHandle), which is all it can change.
        try
        {
            try { _ = Detach(); }
            finally { ReleaseFileLock(); }
        }
        finally
        {
            // A re-OPEN that gave the kept handle away to try another share mode is unsuccessful: the kept state stands as the
            // CLOSE left it (§14.9.27.4 GR25), now that this OPEN's own handle is off the file.
            if (_givenBack is { } gone)
            {
                _givenBack = null;
                RetakeKeptHandle(gone, unpublished);
            }
        }
    }

    /// <summary>The keyed CLOSE body (ISO §14.9.6): through a writable handle the store's PROMOTION (2.2.1.6 R5) — the
    /// work meta made a committed slot, synced — under the store mutex, so the next OPEN verifies the retirement's few
    /// pages and nothing else; the records themselves were written by the statements that changed them. The
    /// run-unit-termination <c>CloseAll</c> reaches it too, which keyed chains (RL208A) depend on. The not-open '42'
    /// guard lives on <see cref="FileConnector.Close"/>.</summary>
    protected sealed override string CloseCore()
    {
        // A refused promotion maps to '30' on FileConnector.Close (§9.1.13.6 item 1 — the ONE mapping), which ends
        // the open mode either way; ModeKnown then stays true for the USE-declarative scoping. The records survive it:
        // page 2 already names the last completed statement's trees (2.2.1.6), so the next OPEN adopts them.
        // OptionalAbsent (the FPI's "not present" state) survives the CLOSE — §14.9.6.4 GR6 says the file position
        // indicator is unchanged; the next OPEN resets it (FileConnector.Open) — kb/Work PB140.
        var detached = StoreStatus.Ok;
        try
        {
            if (!OptionalAbsent && StoreHandleWritable && AttachedStore.File is { Writable: true } file
                && InStoreMutex(file.Close) != StoreStatus.Ok)
                throw new IOException($"the medium refused the CLOSE's promotion of '{HostPath}'");
        }
        finally
        {
            // ⛔ UNDER COMMIT CONTROL THE CLOSE KEEPS THE FILE LOCK, AND THE HANDLE THAT IS IT (§9.1.15: "The file lock is
            // removed by an explicit or implicit CLOSE statement executed for that file connector, except where the file is
            // subject to an active APPLY COMMIT clause."; §14.9.7.4 GR4, "closed and not reopened prior to the COMMIT
            // statement, the file locks are released"; DESIGN-runtime-library.md 2.2.1.10). The rule is the clause's, not
            // the transaction's (review finding R1-2): a connector that only READ keeps it as one that changed records
            // does. The connector's handle, its region endpoint — through which the transaction's liveness byte may be
            // held — its attachment to the store and its Table 19 run-unit file lock (the column bytes a host without
            // mandatory share modes publishes) stay, so a ROLLBACK after the CLOSE can still restore through it and no
            // other run unit opens the file against the kept lock, until the next COMMIT or ROLLBACK, or the run unit's
            // end, releases them (ReleaseKept) or the connector's own re-OPEN carries them on (CarryKeptOver).
            if (CloseKeepsLocks)
            {
                _kept = new KeptFile(new KeptHandle(_store!, _handle!, _regions!, _store!.CanWrite ? FileAccess.ReadWrite : FileAccess.Read, HostShare),
                    Adopted: false, KeepRunUnitFileLock(), _regions!, HostPath, HostFile.IdentityOf(_handle!), Physical, LockOwner, AttachedStore, SharedStores,
                    Restoring, HoldingsOrder) { OpenColumn = KeptOpenColumn };
                (_store, _handle, _regions, _coherent) = (null, null, null, false);
            }
            else
            {
                // §9.1.15 — "The file lock is removed by an explicit or implicit CLOSE statement executed for that
                // file connector", whatever the promotion did (kb/Work PB771); the store is left first, while the handle
                // its medium reads through is still open.
                try { detached = Detach(); }
                finally { ReleaseFileLock(); }
            }
        }
        // The last writable connector's CLOSE re-adopts the store as its INPUT siblings' (KeyedStore.DetachHandle); a
        // refused read there is this CLOSE's permanent error (§9.1.13.6 item 1; 2.2.1.16's refused READ row), mapped to
        // '30' by FileConnector.Close like a refused promotion. The connector is closed either way.
        if (detached != StoreStatus.Ok)
            throw new IOException($"the medium refused the reader's re-adoption of '{HostPath}' at the CLOSE");
        _fileRemoved = false;
        ModeKnown = false;   // 9.1.4 - after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    /// <summary>The I-O status a store operation's failure answers when the statement is not a record WRITE or REWRITE —
    /// an OPEN's adoption or creation, a DELETE — in either organization: §9.1.13.6 item 1's '30' (<i>"A permanent error
    /// exists and no further information is available concerning the input-output operation"</i>) for every failure, the
    /// boundary of the physical file included. The boundary's '24' is §9.1.13.5 item 4's <i>"An attempt is made to write
    /// outside the externally-defined boundaries of a physical relative or indexed file"</i> — an attempt to WRITE a
    /// record — and §14.9.10.4 gives DELETE no boundary condition, so a DELETE whose copy-on-write pages the host refuses
    /// to grow the file for is the permanent error and never the invalid key condition (docs/CONFORMANCE.md
    /// DOC-A.1-107). Null on success.</summary>
    private protected static string? Failed(StoreStatus status) =>
        status == StoreStatus.Ok ? null : FileStatusCode.PermanentError;

    /// <summary>The I-O status a record WRITE's or REWRITE's store failure answers, in either organization: '24' for the
    /// boundary of the physical file (§9.1.13.5 item 4; §14.9.51.4 GR33 b) for a relative file, GR42 d) for an indexed
    /// one; 2.2.1.9; DOC-A.1-107), otherwise <see cref="Failed"/>'s '30'; null on success.</summary>
    private protected static string? RecordWriteFailed(StoreStatus status) =>
        status == StoreStatus.Boundary ? FileStatusCode.BoundaryViolation : Failed(status);

    /// <summary>Whether this statement's coherent entry already read page 2 (the registry's statement around the
    /// verb), so the change's own statement need not read it again.</summary>
    private protected bool Entered => AttachedStore.StatementDepth > 0;

    /// <inheritdoc/>  (a keyed FPI is a key VALUE plus a validity bit; §14.9.41.4 GR7 clears the bit too)
    protected sealed override void InvalidateFilePosition()
    {
        FpiValid = false;
        base.InvalidateFilePosition();   // → '46' on the next sequential READ (§9.1.13.7 item 6 a))
    }

    /// <summary>An unsuccessful START whose status is the invalid key condition's '23'
    /// (§9.1.13.5 item 3): §14.9.41.4 GR7's invalidation plus that value.</summary>
    private protected string StartFail() => Unsuccessful(FileStatusCode.RecordNotFound);

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

    // ── Commit control: APPLY COMMIT, COMMIT and ROLLBACK on the engine (DESIGN-runtime-library.md 2.2.1.10; kb/Work PB3337)

    // (FileConnector.CommitControlled: a connector under commit control keeps each change's before-image and HOLDINGS in the
    // run unit's transaction on the physical file, TransactionFor; a second connector of the same run unit on the file that no
    // APPLY COMMIT names logs nothing, D.2.5's independent file connectors, and meets the transaction's holdings like any
    // other run unit's.)

    /// <summary>A ROLLBACK's view of the organization's trees (<see cref="IRestoringTrees"/>), which the dead-owner step,
    /// the lock test's rollback of a dead holder and a COMMIT's reversal use as well. ⛔ It must stand on its own: a CLOSE under
    /// commit control keeps it with the state it keeps (<see cref="KeptFile.Restoring"/>), which outlives the connector's
    /// attachment to that store, so an organization takes what it needs of the file from the file it restores, never from the
    /// connector's store field (kb/Work PB3494).</summary>
    private protected abstract RestoringTreesFactory Restoring { get; }

    /// <summary>⛔ The order of the UNDO and HOLDINGS trees the file is opened under (<see cref="HoldingsKeyOrder"/>; kb/Work
    /// PB3485): the organization's own key orders, so two keys the file's collating sequence makes equal are one holding.</summary>
    private protected abstract IKeyOrder HoldingsOrder { get; }

    /// <summary>The record identity — the HOLDINGS tree's bytes — of a §9.1.16 record lock identity string
    /// (<see cref="FileConnector.LastReadRecordId"/>'s form); null when it names no record the file can hold.</summary>
    private protected abstract byte[]? IdentityOf(string recordId);

    /// <inheritdoc/>
    /// <remarks>⛔ THE ONE DECISION that a CLOSE keeps the file lock, the handle that is it and the record locks published
    /// through it (§14.9.6.4 GR9; <see cref="CloseCore"/>, <c>FileRegistry.SharedClose</c>): the connector is under commit
    /// control and has a handle to keep (an absent OPTIONAL INPUT file has none, and so has nothing to keep).</remarks>
    internal sealed override bool CloseKeepsLocks =>
        CommitControlled && _store is not null && _handle is not null && _regions is not null;

    /// <summary>⛔ WHAT A CLOSE UNDER COMMIT CONTROL KEPT (<see cref="CloseCore"/>; <see cref="KeptFile"/>), until the next COMMIT or
    /// ROLLBACK or the run unit's end (<see cref="ReleaseKept"/>), or the connector's next successful OPEN of the SAME physical
    /// file, which carries it on (<see cref="CarryKeptOver"/>). It belongs to the physical file, not to this connector: an
    /// ASSIGN USING that associates the connector with another file (<c>FileConnector.Associate</c>) or a replacement of the
    /// connector hands it to the registry (<see cref="RetireKept"/>).</summary>
    private KeptFile? _kept;

    /// <summary>Give a kept handle back (<see cref="KeptFile.GiveBack"/>) off the store this connector is attached to.</summary>
    private void GiveBack(KeptHandle kept) => KeptFile.GiveBack(AttachedStore, kept);

    /// <summary>Give back everything a CLOSE under commit control kept (<see cref="KeptFile.Release"/>) and the connector's own
    /// attachment to the store, so the connector, which is closed, holds nothing of it. Also an <c>OPEN OUTPUT</c>'s, which
    /// creates the file anew, once the creation has landed (<see cref="Recreate"/>).</summary>
    internal void ReleaseKept()
    {
        if (_kept is not { } kept) return;
        _kept = null;
        try { kept.Release(); }
        finally
        {
            _attached = false;
            ForgetStore();
        }
    }

    /// <summary>⛔ HAND THE KEPT STATE OVER TO THE REGISTRY (kb/Work PB3494, PB3576): the connector is displaced by its element's next
    /// activation, or its next OPEN associates it with another physical file (§12.4.5.3 GR3 b), or the path of its OPEN no longer
    /// names the file it was kept on, and what its CLOSE kept belongs to the file it was kept on, not to the connector — the
    /// COMMIT, the ROLLBACK and the run unit's end still reach it there (<c>FileRegistry.Retire</c>), and the connector holds
    /// nothing of it any more. Nothing when it kept nothing. The ONE hand-over: every path that parts a connector from its kept
    /// state without ending it comes here.</summary>
    internal void RetireKept()
    {
        if (_kept is not { } kept) return;
        (_kept, _attached) = (null, false);
        ForgetStore();
        (KeptSink ?? throw new InvalidOperationException(
            $"'{HostPath}' holds a kept state but its connector was not registered through a FileRegistry — a registration defect (kb/Work PB3576)"))(kept);
    }

    /// <summary>Where <see cref="RetireKept"/> hands the kept state: the registry that registered this connector, which keeps it for
    /// the COMMIT, the ROLLBACK and the run unit's end (<c>FileRegistry._retired</c>). Set at registration; a connector no registry
    /// holds is never under commit control and keeps nothing.</summary>
    internal Action<KeptFile>? KeptSink { get; set; }

    /// <summary>Whether the kept state is of a physical file at another path than <see cref="FileConnector.HostPath"/> now: the
    /// connector was associated with another file since its CLOSE, and the kept one is still there, under its own path, for the
    /// COMMIT and the ROLLBACK (<see cref="RetireKept"/>). A kept file the SAME path no longer names (removed or renamed away
    /// outside the run unit) is the OPEN's to hand over the same way (<see cref="RetireWhatWasKeptOfAnotherFile"/>).</summary>
    internal bool KeptIsOfAnotherPath => _kept is { } kept && !HostFile.PhysicalFileComparer.Equals(kept.HostPath, HostPath);

    /// <inheritdoc/>
    /// <remarks>⛔ THE DECISION OF kb/Work PB3493 FOR A KEPT FILE THAT IS NOT THERE, made where the path names ANOTHER physical
    /// file or none (kb/Work PB3533, PB3576): the kept state belonged to the file that was removed or renamed away outside the run
    /// unit, nothing of its store describes the file the path names now, and the OPEN is an ordinary OPEN of that file, or of none.
    /// The kept state RETIRES (<see cref="RetireKept"/>) as it does where the connector is associated with another file
    /// (<c>FileRegistry.Associate</c>, kb/Work PB3494): its transaction, record locks and file lock stay the COMMIT's and the
    /// ROLLBACK's in the physical file they belong to, which the run unit's tables hold apart from the file now at the path
    /// (<see cref="StoreTable"/>, <c>PhysicalFileTable.For</c>), so a renamed file's changes are committed, not abandoned. It
    /// retires whatever this OPEN goes on to answer: §14.9.27.4 GR25's <i>"the file is not affected"</i> is about the file the path
    /// names, and the kept one is not it. A connector that inherited the kept state of the connector it replaced
    /// (<see cref="InheritKept"/>) meets the same question here.</remarks>
    private protected sealed override void RetireWhatWasKeptOfAnotherFile(FilePresence presence)
    {
        if (_kept is { } kept && (presence is FilePresence.Absent || !kept.IsOfTheFileAt(HostPath))) RetireKept();
    }

    /// <summary>The connector's re-OPEN SUCCEEDED (<see cref="OpenCore"/>): what its CLOSE under commit control kept is this
    /// OPEN's now — the attachment and the transaction's liveness byte were carried on by <see cref="Attach"/>, so the kept
    /// handle, unless the OPEN adopted it, and any file lock left are given back (§14.9.7.4 GR4's <i>"closed and not
    /// reopened"</i>: a reopened file's lock is the new OPEN's).</summary>
    private void CarryKeptOver()
    {
        _givenBack = null;   // the handle this OPEN gave away stays away: the OPEN succeeded through its replacement
        if (_kept is not { } kept) return;
        _kept = null;
        try
        {
            // Nothing is published through a kept endpoint a successful re-OPEN reached Attach with (Attach moved it all). An OPEN
            // that never attached - an absent OPTIONAL file, a file the path no longer names - holds no kept state here: the OPEN
            // retired it to the registry before its body (RetireWhatWasKeptOfAnotherFile), so it ends with the COMMIT.
            if (!kept.Adopted && Physical is { } physical) PhysicalFileTable.ReleaseThrough(physical, kept.Endpoint);
            if (kept.Handle is { } handle && !kept.Adopted) GiveBack(handle);
        }
        finally { kept.FileLock?.Dispose(); }
    }

    /// <summary>⛔ TAKE OVER what a connector this one replaced kept at its CLOSE under commit control (review finding R1-1;
    /// <c>FileRegistry.ReclaimRetired</c>): the replacement is the same SELECT's next activation (§12.4.6.3.4 GR5 keeps the
    /// APPLY COMMIT clause active across the CANCEL), so its OPEN is a re-OPEN of the file the kept state belongs to - the
    /// attachment and the transaction in it, the handle, the Table 19 file lock and the record locks published through the
    /// endpoint - and every rule written for that re-OPEN (<see cref="TakeFileLock"/>, <see cref="Attach"/>,
    /// <see cref="CarryKeptOver"/>, <see cref="AbandonOpen"/>) applies to it unchanged. Only a replacement under commit
    /// control, of the same organization and on the same physical file, can inherit; anything else leaves the kept state with
    /// the registry, for the COMMIT, the ROLLBACK or the run unit's end. Returns whether it was taken.</summary>
    internal bool InheritKept(KeptFile retired)
    {
        if (!CommitControlled || _kept is not null || _attached || !HostFile.PhysicalFileComparer.Equals(HostPath, retired.HostPath)
            || !InheritStore(retired.Store))
            return false;
        (_kept, _attached) = (retired, true);
        return true;
    }

    /// <summary>Hold the run unit's store <paramref name="store"/> a retired kept state is attached to (<see cref="InheritKept"/>):
    /// the organization's reference to it, which <see cref="ForgetStore"/> clears. False when the store is another organization's.</summary>
    private protected abstract bool InheritStore(KeyedStore store);

    /// <inheritdoc/>
    private protected sealed override bool HoldsKeptHandle => _kept?.Handle is not null;

    /// <inheritdoc/>
    private protected sealed override bool KeptHandleExcludesEveryOpen =>
        _kept?.Handle is { } kept && !FileLockPosture.AdmitsAnotherRunUnit(kept.Share);

    /// <inheritdoc/>
    /// <remarks>The kept handle's own share mode refuses the probe, so it is given back for the instant of the question and,
    /// when another handle does hold the file, taken again before the OPEN answers '61' (§14.9.27.4 GR25); when none does,
    /// it stays away and the OPEN's end gives the kept state back or takes the handle again, as for any re-OPEN that swapped
    /// handles (<see cref="Lock"/>).</remarks>
    private protected sealed override bool HeldByAnotherHandle()
    {
        if (_kept is not { Handle: { } kept, Adopted: false }) return ProbeHeldByAnother();
        _kept = _kept with { Handle = null };
        _givenBack = kept;
        GiveBack(kept);
        if (!ProbeHeldByAnother()) return false;
        _givenBack = null;
        RetakeKeptHandle(kept, unpublished: null);   // no OPEN handle took the record locks: Attach moves them
        return true;
    }

    /// <inheritdoc/>
    private protected sealed override RunUnitFileLock? KeptFileLock => _kept?.FileLock;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEPT COLUMN LOCK IS NEVER FREE WHILE ITS OWNER LIVES: the re-OPEN's own column REPLACES it on the same
    /// hold (<see cref="RunUnitFileLock.Amend"/>, which publishes the new column before it gives the old one back), and an
    /// unsuccessful OPEN puts the old one back (<see cref="TryRestoreKeptFileLock"/>), so no other run unit's OPEN against the
    /// kept column is admitted in an instant between (kb/Work PB3483; the column-lock twin of the share-mode window that
    /// <see cref="Lock"/> keeps as short as the host allows).</remarks>
    private protected sealed override void TakeKeptFileLockOver() => _kept = _kept! with { FileLock = null };

    /// <inheritdoc/>
    private protected sealed override bool TryRestoreKeptFileLock(RunUnitFileLock held)
    {
        // Nothing to put it back into once the kept state ended: an OPEN OUTPUT whose creation LANDED (Recreate, ReleaseKept) — one
        // that was refused leaves the kept state in place, and this puts the column back into it (kb/Work PB3493).
        if (_kept is null || !held.Revert()) return false;
        _kept = _kept with { FileLock = held };
        return true;
    }

    /// <summary>What this connector's record statement adds to its page statement under commit control (2.2.1.10), or null
    /// when no active APPLY COMMIT clause names it: the record's <paramref name="identity"/>, its
    /// <paramref name="beforeImage"/> (null: the statement creates the record) and the HOLDINGS keys of the unique alternate
    /// key values the change frees.</summary>
    private protected KeyedStore.TransactionWork? TransactionFor(byte[] identity, byte[]? beforeImage,
        Func<IReadOnlyList<byte[]>>? freed = null) =>
        CommitControlled && _regions is { Writable: true } regions
            ? new KeyedStore.TransactionWork(regions, identity, beforeImage, freed?.Invoke() ?? [])
            : null;

    /// <summary>⛔ THE LOCK TEST OF THE TRANSACTIONS' HOLDINGS (DESIGN-runtime-library.md 2.2.1.10, "The lock TEST, for every
    /// writer and every reader not IGNORING LOCK"; findings R2-3, R2-4, R4-1): when page 2's holdings root is not empty, each
    /// HOLDINGS key the statement would touch — the record's identity, and for a WRITE or REWRITE each unique alternate key
    /// value it would insert — is looked up, at DOC-A.1-104's point (after the invalid key conditions, which ignore locks,
    /// §14.9.51.4 GR33/GR42; before the record is touched). No entry: the identity is nobody's, and no rollback will ever
    /// touch it. This run unit's own transaction through a connector under commit control: proceed (§14.9.30.4 GR8's
    /// re-access). Any other transaction — another run unit's, or this run unit's through a connector no APPLY COMMIT names —
    /// has its owner's liveness byte probed NOW: held, '51' (§9.1.16's implementor-specified circumstance, DOC-A.1-152), and
    /// the statement's RETRY phrase applies (§14.9.51.4 GR16, <i>"for the case where resources needed to write a record are
    /// locked by another run unit"</i>); free, the owner died, and its transaction is rolled back HERE, under the mutex this
    /// statement holds, before its own change. <paramref name="reevaluate"/> is then true — as after any RETRY wait — and the
    /// caller evaluates the statement's conditions again from the invalid key conditions on, against the file as it is now
    /// ('22' where the rollback re-created the identity or re-inserted the value). A holding is a page of the file, so the
    /// answer does not depend on the instant a death fell. Returns the failing status ('51', a RETRY's exhaustion status, or
    /// '30' for a torn page or a refused rollback), or null.</summary>
    /// <para><paramref name="holdingKeys"/> is asked only when a holding can exist, so a statement on a file no
    /// transaction holds anything of builds no key.</para>
    private protected string? HoldingsGate(Func<IReadOnlyList<byte[]>> holdingKeys, out bool reevaluate)
    {
        reevaluate = false;
        var store = AttachedStore;
        if (store.File is not { } file || file.Meta.HoldingsRoot.IsNone || _regions is not { } regions) return null;
        var keys = holdingKeys();
        int attempts = 0;
        bool rolledBack = false;
        string status = FileRegistry.RetryLoop(() =>
        {
            attempts++;
            foreach (var key in keys)
                while (true)
                {
                    if (store.HolderOf(key, out ulong owner) != StoreStatus.Ok) return RetryAttempt.InRunUnit(FileStatusCode.PermanentError);
                    if (owner == 0 || owner == store.Transaction && CommitControlled) break;
                    if (owner == store.Transaction) return RetryAttempt.InRunUnit(FileStatusCode.RecordLocked);
                    if (!KeyedStore.MayRollBack(regions, owner)) return RetryAttempt.OutsideRunUnit(FileStatusCode.RecordLocked);
                    // A dead owner's holding: an INPUT connector writes nothing and reads the file as it stands, dead
                    // transaction included (2.2.1.10: D.2.5's tolerated read); a writer rolls the owner back here.
                    if (!file.Writable) break;
                    if (store.RollBack(owner, Restoring) != StoreStatus.Ok) return RetryAttempt.InRunUnit(FileStatusCode.PermanentError);
                    rolledBack = true;
                }
            return RetryAttempt.InRunUnit(FileStatusCode.Success);
        }, StatementRetry.Kind, StatementRetry.Amount, waiter: this);
        reevaluate = rolledBack || attempts > 1;
        return status == FileStatusCode.Success ? null : status;
    }

    /// <summary>The holdings test of a READ not IGNORING LOCK (2.2.1.10): the record <paramref name="recordId"/> names, by its
    /// identity (<see cref="HoldingsGate"/>).</summary>
    internal string? ReadHoldingsGate(string recordId, out bool reevaluate)
    {
        reevaluate = false;
        return IdentityOf(recordId) is { } identity ? HoldingsGate(() => [TransactionKeys.Record(identity)], out reevaluate) : null;
    }

    /// <inheritdoc/>
    internal override bool HoldingsMayExist => AttachedStore.File is { } file && !file.Meta.HoldingsRoot.IsNone;

    /// <summary>The run unit's transaction on this connector's physical file, if this connector is attached to its store.</summary>
    internal ulong OpenTransaction => TransactionStore?.Transaction ?? 0;

    /// <summary>The run unit's store of the physical file this connector is attached to (its transaction, and its place in the
    /// run unit's order of open stores, <see cref="KeyedStore.OpenOrder"/>, which COMMIT and ROLLBACK follow), or null while it is
    /// not attached: what <c>FileRegistry.TransactionHolders</c> tells one file's transaction from another's by (kb/Work PB3533).</summary>
    internal KeyedStore? TransactionStore => _attached ? AttachedStore : null;

    /// <summary>The witnesses' seam (<see cref="KeyedStore.ReleaseLivenessForWitness"/>).</summary>
    internal void ReleaseLivenessForWitness()
    {
        if (_attached) AttachedStore.ReleaseLivenessForWitness();
    }

    /// <summary>⛔ COMMIT the run unit's transaction on this connector's physical file (§14.9.7.4 GR3; 2.2.1.10), under the
    /// store mutex through this connector's handle, or — a connector that is closed — through the one it kept at its CLOSE
    /// (<see cref="KeptFile.CommitTransaction"/>).</summary>
    internal CommitOutcome CommitTransaction() =>
        _regions is null && _kept is { } kept ? kept.CommitTransaction() : InStoreMutex(() => AttachedStore.Commit(Restoring, HoldingsOrder));

    /// <summary>⛔ ROLLBACK the run unit's transaction on this connector's physical file (§14.9.36.4 GR3; 2.2.1.10): the
    /// statement's entry (which unwinds an unlanded COMMIT's claim, R5's restart rule) and the ROLLBACK statement, under the
    /// store mutex, or — a connector that is closed — through the handle it kept (<see cref="KeptFile.RollbackTransaction"/>).</summary>
    internal StoreStatus RollbackTransaction() =>
        _regions is null && _kept is { } kept ? kept.RollbackTransaction() : InStoreMutex(() => AttachedStore.Rollback(Restoring, HoldingsOrder));

    // ── Coherence ACROSS RUN UNITS (ISO §9.1.15, §9.1.16; kb/Work PB2660) ────────────────────────────────────

    /// <summary>The connector's own handle — <see cref="FileLock"/>'s, taken once — through which the store's pages are
    /// read and written POSITIONALLY and through which the store mutex and the record locks are held
    /// (<see cref="HostRegionLocks"/>). Null whenever <see cref="FileLock"/> is.</summary>
    private SafeFileHandle? _handle;

    /// <summary>This connector's endpoint on the host's region locks (<see cref="RegionLocks"/>, kb/Work PB2936): made
    /// with <see cref="_handle"/> by <see cref="TakeFileLock"/>, disposed — every byte it holds given back — by
    /// <see cref="ReleaseFileLock"/>. Null whenever <see cref="_handle"/> is.</summary>
    private RegionLocks? _regions;

    /// <summary>⛔ Whether this connector's statements must be SERIALIZED against another run unit's — every one of them
    /// COHERENT (<see cref="BeginStatement"/>): it runs inside the store mutex, starting from the file as it is now. The
    /// engine writes at every statement, so this is every sharing mode that admits ANY other run unit's connector
    /// (<see cref="FileLockPosture.AdmitsAnotherRunUnit"/>, docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7 and
    /// 2.2.1.13; kb/Work PB3333): the INPUT reader §9.1.15 2) admits beside a READ ONLY writer would otherwise meet a
    /// half-written path.</summary>
    private bool _coherent;

    /// <summary>Whether the host granted the connector's handle write access (kb/Work PB2758).</summary>
    private bool StoreHandleWritable => _regions?.Writable ?? false;

    /// <inheritdoc/>
    internal override RegionLocks? RecordLocks => _regions;

    /// <summary>⛔ ENTER A COHERENT STATEMENT (kb/Work PB2660). For a connector another run unit may write
    /// (<see cref="_coherent"/>): take the store's cross-run-unit MUTEX (<see cref="HostRegionLocks.StoreMutexByte"/>)
    /// — waiting for another run unit's statement to finish — and read page 2, adopting it when another run unit's
    /// statement moved it (2.2.1.7). Every governed record statement runs inside one (<c>FileRegistry</c>), so its
    /// record-lock check, its operation, its lock actions and the pages it writes are ONE step to every other run unit —
    /// which is what makes §9.1.16's <i>"While locked by a given file connector, a record is not accessible to another
    /// file connector in the same or a different run unit"</i> enforceable, and what makes §14.9.30.4 GR21's <i>"the
    /// first existing record in the physical file"</i> the file's and not a snapshot's. Inert (<c>default</c>) for every
    /// other connector — and for a statement on a connector whose PERMANENT ERROR is in effect (§9.1.13.1, <i>"The
    /// permanent error condition remains in effect for all subsequent input-output operations on the file"</i>): the
    /// verb answers it first (<see cref="FileConnector.PermanentErrorReplay"/>), and the technique this processor provides
    /// (docs/CONFORMANCE.md DOC-A.1-105) says such a statement <i>"does not touch the physical file"</i>, so it neither
    /// waits for the store mutex nor reads page 2 (kb/Work PB3336 review R1-2).</summary>
    internal override StoreStatement BeginStatement()
    {
        if (!_coherent || _handle is null || PermanentErrorInEffect is not null) return default;
        Enter(AttachedStore);
        return new StoreStatement(this);
    }

    /// <summary>⛔ A HOST FAILURE OF A STATEMENT'S OWN ENTRY IS A PERMANENT ERROR, NOT AN EXCEPTION (kb/Work PB2842).
    /// Taking the mutex and reading page 2 are host I/O, and §9.1.13.6 1)'s <i>"A permanent error exists and no further
    /// information is available concerning the input-output operation"</i> ('30') is what the standard gives such a
    /// failure. The statement is entered all the same, holding what it took: the status is recorded
    /// (<see cref="FileConnector.SetStatus"/>, which starts the permanent error of §9.1.13.1, <i>"The permanent error
    /// condition remains in effect for all subsequent input-output operations on the file"</i>) and the statement's own
    /// body ends at the replay every verb makes first (<see cref="FileConnector.PermanentErrorReplay"/>) — so READ and
    /// START invalidate the file position indicator as they do for any permanent error. A failure that is not the
    /// host's is not the file's and escapes as before, with the entry undone. (The entry allocated the whole store
    /// before the keyed store engine, so an allocation the host refused was a host failure too; it reads one page now,
    /// and that reason left with the whole-store model, kb/Work PB3335.)</summary>
    private static bool IsHostStoreFailure(Exception failure) =>
        failure is IOException or UnauthorizedAccessException;

    private void Enter(KeyedStore store)
    {
        if (store.StatementDepth++ > 0) return;   // the run unit is already inside the mutex
        try
        {
            bool exclusive = _regions!.Writable;
            store.MutexHolder = HostRegionLocks.HoldStoreMutex(_regions);
            // 2.2.1.7's coherent entry: page 2 read — one page — and adopted when another run unit's statement moved it
            // (its roots, its ledger, the page cache dropped); a writer also takes the mints and, under an EXCLUSIVE
            // hold, resolves a PROMOTING flag. An INPUT connector entering the store a writable sibling opened holds the
            // mutex shared and leaves the flag to the next exclusive entry (PagedFile.Enter's exclusive rule). A store
            // with no file (an absent OPTIONAL file read INPUT) has nothing to enter.
            if (store.File is { } file && file.Enter(exclusive) != StoreStatus.Ok) SetStatus(FileStatusCode.PermanentError);
            // The DEAD-OWNER step (2.2.1.10, finding R3-1): an exclusive entry through a writable handle, once the entry
            // resolved a PROMOTING flag (so a COMMIT's fate is settled before its transaction is probed), rolls back
            // every transaction whose owner's liveness byte is free before the statement proceeds.
            else if (exclusive && store.RollBackDeadOwners(_regions, Restoring) != StoreStatus.Ok)
                SetStatus(FileStatusCode.PermanentError);
        }
        catch (Exception failure) when (IsHostStoreFailure(failure))
        {
            SetStatus(FileStatusCode.PermanentError);
        }
        catch
        {
            Leave(store);
            throw;
        }
    }

    /// <summary>Run <paramref name="body"/> — an OPEN's adoption or creation, a CLOSE's promotion, a COMMIT or a ROLLBACK —
    /// inside the store mutex when this connector is coherent (2.2.1.7: <i>"runs every record statement, and its OPEN's
    /// adoption rule (R6), inside the store mutex"</i>; <see cref="HostRegionLocks.InStoreMutex"/>); a NO OTHER connector has
    /// nobody to exclude.</summary>
    private T InStoreMutex<T>(Func<T> body) => HostRegionLocks.InStoreMutex(_coherent ? _regions : null, body);

    /// <summary>Leave the statement; the outermost leave gives the mutex back. The statement's verb wrote its own pages
    /// and page 2 before it answered (2.2.1.7: each change is one statement of the page layer, run inside this one), so
    /// nothing is left to write when it ends.</summary>
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
    /// (re-reading page 2 if it changed) before its next attempt. Nothing has been written when a conflict is
    /// being retried — every conflict check precedes its operation (kb/Work PB338).</summary>
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
}

/// <summary>⛔ ONE STATEMENT'S HOLD ON A KEYED STORE that another run unit may write (kb/Work PB2660) — returned by
/// <see cref="FileConnector.BeginStatement"/> and inert (<c>default</c>) for every connector that cannot meet such a
/// writer. <c>FileRegistry</c> opens one around each governed record statement (<c>using</c>), and its disposal ends the
/// statement; a RETRY wait lets the mutex go through <see cref="FileConnector.WaitOutsideStatement"/>. It carries no
/// completion step: the verb inside it wrote its pages and page 2 before it answered (2.2.1.7; kb/Work PB3333, PB3335).</summary>
internal readonly struct StoreStatement : IDisposable
{
    private readonly KeyedConnector? _owner;

    internal StoreStatement(KeyedConnector owner) => _owner = owner;

    /// <summary>Leave the statement (the outermost leave gives the mutex back).</summary>
    public void Dispose() => _owner?.EndStatement();
}
