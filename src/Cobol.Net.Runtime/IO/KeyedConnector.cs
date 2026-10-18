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
    /// truncation is <see cref="PagedFile.Create"/>'s.</para></summary>
    private void TakeFileLock(bool create)
    {
        var taken = HostFile.OpenConnectorStore(HostPath, create ? FileMode.OpenOrCreate : FileMode.Open,
            HostAccess(Mode), HostShare);
        _store?.Dispose();   // no arm takes it twice; belt-and-braces so a future one cannot leak
        _store = taken;
        // Taken ONCE: the store's pages are read and written positionally through it (never through the stream's
        // buffer, kb/Work PB2660), and it is the handle the store mutex and this connector's record locks are held by.
        _handle = taken.SafeFileHandle;
        // Writable is what the host granted this handle, never re-derived from the open mode (kb/Work PB2758).
        _regions = HostRegionLocks.Open(_handle, HostPath, writable: taken.CanWrite);
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
                    // NO file lock: §9.1.15 establishes one on a PHYSICAL FILE, and there is none here.
                    AttachStore(create: false);          // no file, no records
                    _attached = true;
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                if ((Lock(create: false) ?? Attach(create: false)) is { } refusedInput) return refusedInput;   // §9.1.15 — the lock, before the store is read through it
                break;
            case FileOpenMode.Output:
                // §14.9.27.4 GR18 — OUTPUT creates the physical file: a new, EMPTY store (kb/Work PB754)
                if ((Lock(create: true) ?? Attach(create: true)) is { } refusedOutput) return refusedOutput;
                break;
            case FileOpenMode.IO:
            case FileOpenMode.Extend:
                if (!exists)
                {
                    if (!IsOptional) return FileStatusCode.FileNotFound;
                    // Created as if OPEN OUTPUT + CLOSE (§14.9.27 GR17): the file the lock just created has no bytes,
                    // and the adoption of a file of no bytes through a writable handle creates the empty store.
                    if ((Lock(create: true) ?? Attach(create: false)) is { } refusedCreated) return refusedCreated;
                    status = FileStatusCode.OptionalFileNotFound;
                    break;
                }
                if ((Lock(create: false) ?? Attach(create: false)) is { } refused) return refused;
                break;
        }
        FpiValid = mode is FileOpenMode.Input or FileOpenMode.IO;
        return Opened(mode) ?? status;
    }

    /// <summary>The file lock of an arm that has a physical file (<see cref="TakeFileLock"/>), taken only for a declaration
    /// the physical format can hold (<see cref="FormatHolds"/>): one it cannot is §14.9.27.4 GR10's fixed file attribute
    /// conflict, '39', answered before any file is touched (GR25, <i>"If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected"</i>). Returns the failing I-O status, or null.</summary>
    private string? Lock(bool create)
    {
        if (!FormatHolds()) return FileStatusCode.FixedAttributeConflict;
        TakeFileLock(create);
        return null;
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
        var store = AttachStore(create);
        _attached = true;
        store.AttachHandle(_handle!, StoreHandleWritable,
            HostFile.InputOutputAreaBuffer(InputOutputAreas) / PageHeader.PageSize);
        var opened = InStoreMutex(() => create
            ? store.Create(DeclaredAttributes, _coherent)
            : store.Open(StoreHandleWritable, _coherent, DeclaredAttributes));
        return Failed(opened);
    }

    /// <summary>Leave the store: this connector's handle off its medium, and the table's attachment given back.
    /// <para>⛔ UNDER THE STORE MUTEX (kb/Work PB3335): when this was the store's last writable handle and a reader stays,
    /// the store is opened again as the reader's (<see cref="KeyedStore.DetachHandle"/>, 2.2.1.6 R6 in memory), and that
    /// adoption reads pages 0, 1 and 2 — which another run unit's statement may be writing. 2.2.1.7 puts every adoption
    /// <i>"inside the store mutex"</i>; this connector's handle is still open here, so it holds the mutex for it.</para></summary>
    private void Detach()
    {
        if (!_attached) return;
        if (_handle is { } handle) InStoreMutex(() => AttachedStore.DetachHandle(handle));
        SharedStores?.Detach(HostPath);
        _attached = false;
        ForgetStore();
    }

    /// <inheritdoc/>
    protected sealed override void AbandonOpen()
    {
        try { Detach(); }
        finally { ReleaseFileLock(); }
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
        try
        {
            if (!OptionalAbsent && StoreHandleWritable && AttachedStore.File is { Writable: true } file
                && InStoreMutex(file.Close) != StoreStatus.Ok)
                throw new IOException($"the medium refused the CLOSE's promotion of '{HostPath}'");
        }
        finally
        {
            // §9.1.15 — "The file lock is removed by an explicit or implicit CLOSE statement executed for that
            // file connector", whatever the promotion did (kb/Work PB771); the store is left first, while the handle
            // its medium reads through is still open.
            try { Detach(); }
            finally { ReleaseFileLock(); }
        }
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
            store.MutexHolder = HoldStoreMutex(exclusive);
            // 2.2.1.7's coherent entry: page 2 read — one page — and adopted when another run unit's statement moved it
            // (its roots, its ledger, the page cache dropped); a writer also takes the mints and, under an EXCLUSIVE
            // hold, resolves a PROMOTING flag. An INPUT connector entering the store a writable sibling opened holds the
            // mutex shared and leaves the flag to the next exclusive entry (PagedFile.Enter's exclusive rule). A store
            // with no file (an absent OPTIONAL file read INPUT) has nothing to enter.
            if (store.File is { } file && file.Enter(exclusive) != StoreStatus.Ok) SetStatus(FileStatusCode.PermanentError);
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

    /// <summary>Run <paramref name="body"/> — an OPEN's adoption or creation, a CLOSE's promotion — inside the store mutex
    /// when this connector is coherent (2.2.1.7: <i>"runs every record statement, and its OPEN's adoption rule (R6),
    /// inside the store mutex"</i>); a NO OTHER connector has nobody to exclude.</summary>
    private T InStoreMutex<T>(Func<T> body)
    {
        if (!_coherent || _regions is null) return body();
        var holder = HoldStoreMutex(exclusive: _regions.Writable);
        try { return body(); }
        finally { holder?.Release(HostRegionLocks.StoreMutexByte); }
    }

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

    /// <summary>The store mutex, held for the length of one statement — exclusive through a writable handle, shared
    /// through an <c>OPEN INPUT</c> connector's read-only one (a reader never changes the store, and Linux refuses
    /// an exclusive lock through a read-only descriptor; <see cref="RegionLocks.Hold"/>). Null when the host
    /// cannot carry the lock, and the run unit then has no protection against another's writer but the file lock
    /// (docs/CONFORMANCE.md DOC-A.1-75).</summary>
    private RegionLocks? HoldStoreMutex(bool exclusive) =>
        _regions!.Hold(HostRegionLocks.StoreMutexByte, exclusive, wait: true) == RegionLockResult.Ok ? _regions : null;

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
