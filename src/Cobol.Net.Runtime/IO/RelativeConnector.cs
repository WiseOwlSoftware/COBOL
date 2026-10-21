// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native RELATIVE-organization file connector (ISO/IEC 1989:2023 §9.1.7.3): a sparse 1-based slot model —
/// each record is identified by its relative record number (RRN); slots between records "do not exist" for READ.
/// The behavioral state machine ports the legacy <c>RelativeFileHandler</c> (NIST RL-suite-proven) onto the
/// character-image substrate: the file position indicator (§9.1.11) is a slot number + an inclusive flag (set by
/// OPEN/START, advanced exclusively by READ), '46' poisoning after an unsuccessful READ/START (§14.9.30 GR21 /
/// §9.1.13.7 item 6), and the '43' previous-op-was-READ rule for sequential-access REWRITE/DELETE (§14.9.35 GR5 /
/// §14.9.10 GR2). Records cross this boundary as their character image (a <see cref="string"/>).
/// <para>⛔ THE RECORDS LIVE IN THE KEYED STORE ENGINE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1, kb/Work
/// PB3333): one B+ tree keyed by the relative record number (<see cref="RelativeStore"/>), every statement reading the
/// pages it touches and writing the pages it changes, then page 2 — so the file is, after every statement, in the state
/// its last completed statement left it, whatever happens to the run unit. Nothing is loaded at OPEN and nothing is
/// persisted at CLOSE; the statuses and their order (docs/CONFORMANCE.md DOC-A.1-104) are this connector's, and only the
/// medium changed.</para>
/// </summary>
public sealed class RelativeConnector : KeyedConnector
{
    private readonly int _keyDigits;   // RELATIVE KEY digit capacity (0 = no RELATIVE KEY clause)

    /// <summary>The attached PER-PHYSICAL-FILE store (kb/Work PB143): every connector over one host path sees ONE
    /// tree, so a DELETE through one is a deletion for all. A placeholder until OPEN attaches (and again after CLOSE
    /// detaches). Every change goes through the store's own <c>Insert</c>/<c>Replace</c>/<c>Delete</c>, each one
    /// statement of the page layer; there is no second way to change it (kb/Work PB739).</summary>
    private RelativeStore _st = new();

    private long _fpi;                   // file position indicator: the current slot (§9.1.11); its validity is FpiValid
    /// <summary>Which arm of §14.9.30.4 GR21's relative selection governs the next sequential READ: true while
    /// the file position indicator "was established by a prior successful OPEN or START statement" (rule b — the
    /// indicator is INCLUSIVE and the direction is not consulted), false once a READ established it (rule c —
    /// exclusive, and the direction selects). ⛔ ONE FIELD ANSWERS THIS ONE QUESTION (kb/Work PB343): a second
    /// `_positioner` char that distinguished OPEN from START lived here in lockstep with this flag and existed
    /// only to enforce the INDEXED block's after-OPEN carve-out on a relative file. Nothing in this
    /// organization's rules asks OPEN-or-START.</summary>
    private bool _inclusive;
    private long _pendingKey;            // the RELATIVE KEY item's value, set by the compiler before keyed verbs
    private long _lastReleasedSlot;      // highest RRN THIS connector has released since OPEN (§14.9.51 GR29a's "ascending")
    private long _lastSlot;              // last slot read/written — the GR25/GR29a store-back + seq REWRITE/DELETE target


    /// <summary>The RRN of the record last made available / released — the §14.9.30 GR25 and §14.9.51 GR29a
    /// MOVE-back source the generated code stores into the RELATIVE KEY item.</summary>
    public long LastSlot => _lastSlot;

    /// <summary>Stage the RELATIVE KEY item's value for the next keyed operation (§14.9.30 GR29, §14.9.35 GR21,
    /// §14.9.51 GR29b, §14.9.10 GR4). The compiler decodes the TYPED key field — never raw bytes.</summary>
    public void SetPendingKey(long rrn) => _pendingKey = rrn;

    // ── Record-lock identity (ISO §9.1.16) — a relative record's identity IS its RRN ─────────────────────────

    /// <inheritdoc/>
    public override string LastReadRecordId =>
        _lastSlot > 0 ? _lastSlot.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (sequential access targets the last-read slot, §14.9.35.4 GR5 / §14.9.10.4 GR2;
    /// random/dynamic the slot named by the RELATIVE KEY item, §14.9.35.4 GR21 / §14.9.10.4 GR4 — the
    /// ACCESS MODE alone selects the target, see <see cref="KeyedConnector"/>)
    protected override string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) => Access == KeyedAccess.Sequential
        ? SequentialMutationTarget
        : _pendingKey > 0 ? _pendingKey.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (Write sets <see cref="_lastSlot"/> to the released RRN on success, §14.9.51 GR29)
    public override string LastWrittenRecordId => LastReadRecordId;

    // RECORD IS VARYING: each record's frame carries its exact length, so a varying record persists at the length it
    // was written (§13.18.43 GR13) and reports it on READ (GR15); WRITE/REWRITE outside the VaryMin/VaryMax bounds is
    // the GR14/§14.9.35 GR20 '44'.

    public RelativeConnector(string hostPath, int recordWidth, KeyedAccess access, int relativeKeyDigits,
        int varyMin = -1, int varyMax = -1)
        : base(hostPath, recordWidth, access, varyMin, varyMax)
    {
        _keyDigits = relativeKeyDigits;
    }

    /// <inheritdoc/>
    /// <remarks>The RELATIVE KEY item's digit count is deliberately NOT a catalog attribute: §12.4.5.13.3
    /// SR3 puts that item OUTSIDE the record ("shall not be defined in a record description entry
    /// subordinate to the associated file-name"), it is a property of the referencing program rather than of the
    /// physical file, and §9.1.6's list does not name it. A too-small RELATIVE KEY item is the '14' the
    /// connector already reports (§9.1.13.4 item 2), not a file attribute conflict.</remarks>
    protected override string DeclaredOrganization => FixedFileAttributes.Relative;

    /// <summary>§14.9.6.4 GR2 d) — <i>"A file with organization other than sequential, that resides on a mass
    /// storage device."</i> The category is settled by the ORGANIZATION alone, so it needs no medium
    /// determination (kb/Work PB235).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonSequential;

    // ── The engine (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1) ─────────────────────────────────────

    /// <inheritdoc/>
    private protected override KeyedStore AttachedStore => _st;

    /// <inheritdoc/>
    private protected override KeyedStore AttachStore(bool create) =>
        _st = SharedStores is { } table
            ? create ? table.AttachCreated<RelativeStore>(HostPath) : table.Attach<RelativeStore>(HostPath)
            : new RelativeStore();

    /// <inheritdoc/>
    private protected override void ForgetStore() => _st = new RelativeStore();

    /// <inheritdoc/>
    private protected override RestoringTreesFactory Restoring => RelativeStore.Restoring;

    /// <inheritdoc/>
    private protected override IKeyOrder HoldingsOrder => HoldingsKeyOrder.RecordNumbers;

    /// <inheritdoc/>  (a relative record's lock identity is its record number in decimal, <see cref="LastReadRecordId"/>)
    private protected override byte[]? IdentityOf(string recordId) =>
        long.TryParse(recordId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long rrn)
        && rrn > 0 ? RelativeStore.Key(rrn) : null;

    // ── OPEN (ISO §14.9.27; the arms are KeyedConnector.OpenCore's) ─────────────────────────────────────────────

    /// <inheritdoc/>
    private protected override void ResetPosition()
    {
        _pendingKey = 0;
        _lastSlot = 0;
        _lastReleasedSlot = 0;
    }

    /// <inheritdoc/>
    /// <remarks>§14.9.27 GR14 — the file position indicator is 1 on INPUT/I-O, an INCLUSIVE indicator (§14.9.30.4 GR21
    /// rule b). ⛔ NOTHING IS CAPTURED HERE FOR EXTEND (kb/Work PB739). The extend release number used to be measured
    /// once, at the OPEN, into a private base — and §14.9.51.4 GR29 a) says in the same breath that under sharing "the
    /// record numbers are not necessarily consecutive", which can only be true of a number taken from the file AT THE
    /// RELEASE. Two connectors extending one shared file both captured 2 and the second overwrote the first record.
    /// <see cref="NextSequentialSlot"/> reads the live tree instead; §14.9.27 GR15's positioning IS that reading, not a
    /// saved base.</remarks>
    private protected override string? Opened(FileOpenMode mode)
    {
        _fpi = 1;
        _inclusive = true;
        return null;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> — the first existing slot at/after the FPI (inclusive after
    /// OPEN/START, exclusive after a READ; §14.9.30 GR21 relative rules b/c). No such slot → at end '10' (GR24);
    /// an RRN whose significant digits exceed the RELATIVE KEY item's size → '14', at end (GR21 rule d).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; the compiler edition-gates the phrase — the ONE
    /// <c>IBoundRead</c> arm of <c>VersionConformancePass</c>, COBOLNET0900). On THIS organization it selects a
    /// record by exactly the same §14.9.30.4 GR21 rules a READ NEXT does — see
    /// <see cref="SelectSequentialSlot"/> for why the after-OPEN at-end carve-out is the INDEXED block's alone
    /// (kb/Work PB343) and why the selection therefore takes no edition parameter.</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    /// <summary>ISO §14.9.30.4 GR21's "When the file is a relative file" SELECTION — rules b)/c): the file
    /// position indicator set by a prior OPEN or START selects "the first existing record that is selected …
    /// regardless of whether NEXT or PREVIOUS is specified" (the inclusive bound), one set by a prior READ
    /// selects the first existing relative key number greater than it under NEXT and less than it under
    /// PREVIOUS. A null <paramref name="slot"/> is rule e)'s at-end condition; with <paramref name="withValue"/> the
    /// record's frame bytes come with it, from the same walk. Each rule is ONE SEEK of the tree (O(log n) pages, kb/Work
    /// PB2755: the map it replaced walked every key from the lowest).
    /// <para>⛔ SELECTION ONLY — IT COMMITS NOTHING (kb/Work PB338). Splitting it out of the read is what lets
    /// <see cref="PeekSequentialRecordId"/> answer GR9's "the record identified for access" BEFORE the position
    /// moves, without a second copy of these rules living in the peek.</para></summary>
    private StoreStatus SelectSequentialSlot(bool previous, bool withValue, out long? slot, out byte[]? value)
    {
        // ⛔ RULE b) IS DIRECTION-BLIND, AND IT IS THE WHOLE RULE FOR AN INCLUSIVE INDICATOR (kb/Work PB343).
        // §14.9.30.4 GR21, "When the file is a relative file": "b) If the file position indicator was
        // established by a prior successful OPEN or START statement, the first existing record that is selected
        // is made available, regardless of whether NEXT or PREVIOUS is specified." One seek serves both
        // directions, so `previous` is not consulted at all here: the answer is the first existing record at or
        // after the indicator. That is well defined for every way an inclusive indicator can arise —
        // §14.9.41.4 GR9 a)/b), GR11 and GR12 only ever set a RELATIVE file's indicator to "the relative record
        // number of" an EXISTING record (a comparison satisfied by none is GR9 c)'s invalid key, which
        // invalidates it), so after a START the started-at record itself is the answer in either direction; the
        // one inclusive indicator that can name an empty slot is §14.9.27.4 GR14's OPEN INPUT/I-O "set to 1"
        // over a file whose lowest RRN is higher, and rule b's "first existing record" is then that lowest RRN
        // whichever direction was written.
        // ⛔ THE INDEXED AFTER-OPEN CARVE-OUT DOES NOT REACH THIS ORGANIZATION. GR21 rule d) 3 — "If no such
        // record is found or PREVIOUS is specified and the previous operation on the file was an OPEN
        // statement, the at end condition exists" — is written in the "When the file is an indexed file" block
        // alone, and Annex E.2 item 22, the amendment that put it there, is INFORMATIVE (Annex E is headed
        // "(informative)") and names no organization. Enforcing it here returned '10' from a READ PREVIOUS
        // immediately after OPEN INPUT where rule b) owes the first record. Because the relative block's rule
        // b) reads identically in 2002, 2014 and 2023, this selection takes NO edition parameter —
        // docs/COBOLNET_FILES_DESIGN.md, "READ PREVIOUS is not COBOL-85"; VERSION_CHANGE_REFERENCE row 29 is
        // the indexed leg only.
        // Rule c) — an indicator established by a prior successful READ is EXCLUSIVE and the direction selects:
        // "the first existing record in the physical file whose relative key number is greater than the file
        // position indicator if NEXT is specified or implied or is less than the file position indicator if
        // PREVIOUS is specified is selected".
        var relation = _inclusive ? SeekRelation.GreaterOrEqual            // rule b) — ascending, direction ignored
            : previous ? SeekRelation.LessThan : SeekRelation.GreaterThan;  // rule c)
        return _st.Seek(_fpi, relation, withValue, out slot, out value);
    }

    /// <inheritdoc/>
    /// <remarks>The relative organization's §14.9.30.4 GR9 pre-read conflict target: the RRN
    /// <see cref="SelectSequentialSlot"/> names. The remaining precondition beyond
    /// <c>SequentialReadReachesRetrieval</c> is a valid file position indicator (a failed START leaves
    /// none — §14.9.41.4 GR7). A torn page names no record; the READ itself then answers '30'.</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && FpiValid
        && SelectSequentialSlot(previous, withValue: false, out var s, out _) == StoreStatus.Ok && s is { } slot
            ? slot.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";

    /// <inheritdoc/>
    /// <remarks>§14.9.30.4 GR29 — a random READ positions to the RRN staged in the RELATIVE KEY item by
    /// <see cref="SetPendingKey"/>; a slot that does not exist is the invalid key condition and holds no lock.
    /// <paramref name="keyIndex"/> and <paramref name="recordImage"/> are the indexed organization's key
    /// selectors and carry nothing here (GR29 names only the RELATIVE KEY item).</remarks>
    public override string PeekRandomReadRecordId(int keyIndex, string recordImage, RecordExtents? recordExtents)
    {
        _ = keyIndex; _ = recordImage;
        return ReadOpenModeGuard() is null && !OptionalAbsent
            && _st.Get(_pendingKey, out var value) == StoreStatus.Ok && value is not null
            ? _pendingKey.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";
    }

    private string ReadSequential(out string image, bool previous)
    {
        image = BlankRecordArea();
        if (SequentialReadGuard() is { } pre) return Unsuccessful(pre);   // '47'/'46'/'10' — FileConnector
        if (!FpiValid) return Unsuccessful(FileStatusCode.NoValidNextRecord);   // '46' §9.1.13.7 6a (failed START)

        if (SelectSequentialSlot(previous, withValue: true, out var found, out var value) is not StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);              // a torn page: '30' §9.1.13.6 item 1
        if (found is not { } s) return Unsuccessful(FileStatusCode.AtEnd);   // §14.9.30 GR24b
        if (_keyDigits > 0 && s.ToString(System.Globalization.CultureInfo.InvariantCulture).Length > _keyDigits)
        {
            // §14.9.30 GR21 relative rule d: significant digits of the RRN exceed the RELATIVE KEY item → '14',
            // the at-end condition, FPI invalidated.
            return Unsuccessful(FileStatusCode.RelativeKeyOverflow);
        }
        if (Decoded(value) is not { } read) return Unsuccessful(FileStatusCode.PermanentError);
        _fpi = s; FpiValid = true; _inclusive = false;   // GR21 rule f + rule c's exclusive bound
        _lastSlot = s;
        NoteRecordRead(read.Image, read.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(read.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    /// <summary>Format-2 random READ (§14.9.30 GR29): the FPI takes the RELATIVE KEY item's value (staged by
    /// <see cref="SetPendingKey"/>); no such record → invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(out string image)
    {
        image = BlankRecordArea();
        if (ReadOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);            // '47' §14.9.30.4 GR2
        if (RandomReadAbsentOptionalGuard() is { } absent) return Unsuccessful(absent);  // '23' §9.1.13.5 3 b)
        if (_st.Get(_pendingKey, out var value) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);            // a torn page: '30' §9.1.13.6 item 1
        if (value is null) return Unsuccessful(FileStatusCode.RecordNotFound);   // '23' §9.1.13.5 3a
        if (Decoded(value) is not { } rec) return Unsuccessful(FileStatusCode.PermanentError);
        _fpi = _pendingKey; FpiValid = true; _inclusive = false;
        _lastSlot = _pendingKey;
        NoteRecordRead(rec.Image, rec.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(rec.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    /// <summary>The record a tree value holds, in the native character set (§13.18.13.4 GR6 a)); null for a value this
    /// writer cannot have produced — the torn page it is ('30').</summary>
    private StoredFrame? Decoded(byte[]? value) => value is null ? null : RecordFraming.DecodeFrame(value, 0, CodeSet);

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>⛔ THE HIGHEST RELATIVE RECORD NUMBER PERMITTED FOR A RELATIVE FILE — ISO §14.9.51.4 GR29 b)'s
    /// <i>"If the relative key data item contains a value that is less than 1 or greater than the highest relative
    /// record number permitted for the file, the execution of the WRITE statement is unsuccessful, and the I O
    /// status for the write file connector is set to '34'"</i>. The implementor's number (Annex A.1 item 107,
    /// docs/CONFORMANCE.md <c>DOC-A.1-107</c>): 2 147 483 647 — the §12.4.5.13 key item's domain, a determination the
    /// engine's tree (which would hold 2^63 keys) need not change (2.2.1.9). A key above it is not a relative record
    /// number this implementation permits at all; a key below it is one leaf entry however sparse the file.</summary>
    public const long HighestRelativeRecordNumber = int.MaxValue;

    /// <summary>WRITE (§14.9.51.4 GR29): sequential access releases consecutive RRNs (OUTPUT from 1, EXTEND from
    /// highest+1); RRN digit overflow of the key item → invalid key '24' (GR29a/GR33c). Random/dynamic writes the
    /// slot staged in the key item: occupied → '22' (GR33a), key &lt; 1 or above
    /// <see cref="HighestRelativeRecordNumber"/> → permanent error '34' (GR29b). A record whose pages would take the
    /// store past the boundary of the physical file (the format's 2^32 pages, or the host's maximum file size) →
    /// invalid key '24' (GR33 b), 2.2.1.9); a medium that refuses the statement's pages → '30' (§9.1.13.6 item 1), the
    /// file as the previous statement left it; a record holding a character with no byte image → '91'
    /// (<see cref="FileConnector.RecordHasCharacterWithoutByteImage"/>). Open-mode legality per §9.1.13.7 item 8 ('48').
    /// <para>⛔ THE RULES ARE TESTED IN THE ORDER THE GENERAL RULES STATE THEM (docs/CONFORMANCE.md DOC-A.1-104;
    /// kb/Work PB2751): the open mode (GR3, '48'), the record size (GR14, '44') and the record's content ('91'),
    /// both through <see cref="KeyedConnector.RecordRulesFailure"/>, and only then the release rules of GR29 and the
    /// invalid key conditions of GR33. The key rules used to come first, so a too-short record written to an
    /// occupied slot answered '22' and ran the INVALID KEY phrase where GR14's '44' is a logic error.</para></summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is written
    /// (determination D-FRA (v); kb/Work PB1053), stored with it.</param>
    public string Write(string image, int length = -1, RecordExtents? extents = null)
    {
        // §14.9.51.4 GR29 a) "If the access mode of the write file connector is sequential…" vs. GR29 b)
        // "If the access mode … is random or dynamic…": the ACCESS MODE alone selects the release rule
        // (the open mode only picks the STARTING RRN inside GR29 a) — 1 for output, highest+1 for extend).
        // ⛔ The open mode is NOT a disjunct here (kb/Work PB325): a random- or dynamic-access connector open
        // in the extend mode is illegal source (§14.9.27.3 SR2) but a REACHABLE runtime state, and Table 20
        // leaves its WRITE cell blank — item 8 b) below is what it must answer, not GR29 a)'s append.
        if (PermanentErrorReplay() is { } stuck) return Status = stuck;   // §9.1.13.1 — a permanent error in effect (kb/Work PB1541)
        bool sequentialRelease = Access == KeyedAccess.Sequential;
        if (sequentialRelease)
        {
            if (!IsOpen || Mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' §9.1.13.7 8a
        }
        // §9.1.13.7 8 b) — "If the access mode is dynamic or random, the file connector is not open in the
        // I-O or output mode"; Table 20's Random/Extend and Dynamic/Extend WRITE cells are blank.
        else if (!IsOpen || Mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' §9.1.13.7 8b
        if (RecordRulesFailure(image, length, out string rec) is { } badRecord)
            return Status = badRecord;                                     // '44' §14.9.51.4 GR14, then '91'
        long slot;
        if (sequentialRelease)
        {
            if (NextSequentialSlot(out slot) is { } torn) return Status = torn;
            if (_keyDigits > 0 && slot.ToString(System.Globalization.CultureInfo.InvariantCulture).Length > _keyDigits)
                return Status = FileStatusCode.BoundaryViolation;          // '24' §14.9.51.4 GR29 a) / GR33 c)
        }
        else
        {
            slot = _pendingKey;
            if (slot is < 1 or > HighestRelativeRecordNumber)
                return Status = FileStatusCode.PermanentBoundary;          // '34' §14.9.51.4 GR29 b)
            if (_st.Get(slot, out var present) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
            if (present is not null) return Status = FileStatusCode.DuplicateKey;   // '22' §14.9.51.4 GR33 a)
        }
        var frame = RecordFraming.EncodeFrame(Framed(rec, image, extents), 0, CodeSet);
        // The transactions' HOLDINGS (2.2.1.10): after the invalid key conditions (GR33, which ignore locks), before the
        // record is touched — a record number another transaction deleted is still its holding.
        if (HoldingsGate(() => [TransactionKeys.Record(RelativeStore.Key(slot))], out bool reevaluate) is { } locked)
            return Status = locked;                                        // '51' (RETRY: §14.9.51.4 GR16)
        if (reevaluate) return Write(image, length, extents);              // against the file a rollback restored
        if (RefusedAtCommit() is { } lockRefused) return Status = lockRefused;   // '54' / '53': the lock request is the last check (kb/Work PB2848)
        var written = _st.Insert(slot, frame, Entered, out bool inserted, TransactionFor(RelativeStore.Key(slot), beforeImage: null));
        if (RecordWriteFailed(written) is { } refused) return Status = refused;   // '24' §14.9.51.4 GR33 b) / '30' §9.1.13.6 1)
        if (!inserted) return Status = FileStatusCode.DuplicateKey;        // '22' — the tree declined (GR33 a))
        if (sequentialRelease) _lastReleasedSlot = slot;
        _lastSlot = slot;                                                  // GR29 a) — MOVEd back into the key item
        return Status = FileStatusCode.Success;
    }

    /// <summary>The relative record number the NEXT sequential-access release takes — ISO §14.9.51.4
    /// GR29 a): <i>"If the open mode of the write file connector is output, the first record released after
    /// the OPEN is 1. If the open mode is extend, the first record released after the OPEN is assigned a
    /// record number that is one greater than the highest relative record number existing in the physical
    /// file. Subsequent records released have relative record numbers that are ascending ordinal numbers.
    /// If the physical file is shared and the open mode is extend, the record numbers are not necessarily
    /// consecutive. Otherwise, they are consecutive."</i>
    /// <para>ONE expression answers all four sentences, which is why there is no captured base any more
    /// (kb/Work PB739). The tree's LAST key is the highest RRN existing in the physical file AT THIS MOMENT,
    /// so OPEN OUTPUT (which empties the store) yields 1, OPEN EXTEND yields highest+1, an unshared file
    /// yields consecutive numbers because nothing else releases between two of this connector's writes, and
    /// a SHARED file yields the non-consecutive-but-ascending sequence the rule's own last two sentences
    /// describe — the other connector's release has already raised the highest record number.</para>
    /// <para><c>_lastReleasedSlot</c> is the "ascending" clamp, and it is not redundant: a connector open
    /// I-O may DELETE the top record (§14.9.10.4 GR5) while this one is extending, which lowers the
    /// highest EXISTING number below one this connector already used. The record numbers it releases still
    /// have to ascend.</para>
    /// Returns '30' for a torn page, or null.</summary>
    private string? NextSequentialSlot(out long slot)
    {
        slot = 0;
        if (_st.Edge(last: true, out var highest) != StoreStatus.Ok) return FileStatusCode.PermanentError;
        slot = Math.Max(highest ?? 0, _lastReleasedSlot) + 1;
        return null;
    }

    /// <summary>REWRITE (§14.9.35): open mode must be I-O ('49', §9.1.13.7 item 9). Sequential access replaces
    /// the prior READ's record (no prior successful READ → '43', GR5); random/dynamic replaces the slot named by
    /// the key item (absent → '23', GR21). The FPI is unaffected (GR13). A replacing record whose pages would take
    /// the store past the boundary of the physical file is §9.1.13.5 item 4's "attempt … to write outside the
    /// externally-defined boundaries" — '24', the WRITE's own boundary; a medium that refuses the pages, '30'; one
    /// holding a character with no byte image, '91'.</summary>
    /// <param name="extents">The replacing record's EXTENT TABLE (D-FRA (v); kb/Work PB1053) — a relative record is
    /// replaced whole (GR18), so its table is replaced with it.</param>
    public string Rewrite(string image, int length = -1, RecordExtents? extents = null)
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        // §14.9.35 GR18 — a relative record's size MAY differ from the replaced record's; GR20 still bounds it.
        // ⛔ IN THE GENERAL RULES' ORDER (DOC-A.1-104, kb/Work PB2751): GR5's '43', then GR20's size '44' and the
        // content's '91', and only then the record the statement replaces — GR21's '23' for random/dynamic access.
        bool sequential = Access == KeyedAccess.Sequential;   // §14.9.35.4 GR5 vs. GR21 — the ACCESS MODE alone
        if (sequential && !wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43' GR5
        if (RecordRulesFailure(image, length, out string rec) is { } badRecord)
            return Status = badRecord;                                                          // '44' GR20, then '91'
        long slot = sequential ? _lastSlot : _pendingKey;
        // GR21 for random/dynamic access; for sequential access GR5's "the record that was accessed by the READ
        // statement", which another connector may have deleted since (§14.9.10.4 GR5: it "can no longer be
        // accessed") — the record does not exist, '23', as the indexed organization answers (DOC-A.1-104 (4)).
        if (_st.Get(slot, out var present) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
        if (present is null) return Status = FileStatusCode.RecordNotFound;                     // '23'
        var frame = RecordFraming.EncodeFrame(Framed(rec, image, extents), 0, CodeSet);
        if (HoldingsGate(() => [TransactionKeys.Record(RelativeStore.Key(slot))], out bool reevaluate) is { } locked)
            return Status = locked;                                        // '51' (2.2.1.10)
        if (reevaluate) return Rewrite(image, length, extents);            // against the file a rollback restored
        if (RefusedAtCommit() is { } lockRefused) return Status = lockRefused;   // '54' / '53': the lock request is the last check (kb/Work PB2848)
        var replaced = _st.Replace(slot, frame, Entered, out bool done, TransactionFor(RelativeStore.Key(slot), present));
        if (RecordWriteFailed(replaced) is { } refused) return Status = refused;   // '24' (§9.1.13.5 4) / '30' (§9.1.13.6 1)
        return Status = done ? FileStatusCode.Success : FileStatusCode.RecordNotFound;
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode must be I-O ('49', GR1). Sequential access removes the prior
    /// READ's record ('43' without one, GR2); random/dynamic removes the slot named by the key item (absent →
    /// invalid key '23', GR4). The FPI is unaffected (GR9). A medium that refuses the statement's pages → '30'.</summary>
    public string Delete()
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        bool sequential = Access == KeyedAccess.Sequential;   // §14.9.10.4 GR2 vs. GR4 — the ACCESS MODE alone
        if (sequential && !wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43'
        // GR4's invalid key for random/dynamic access; for sequential access GR2's "the record that was accessed by
        // that READ statement", which another connector may have deleted since — '23', not a success over nothing
        // (kb/Work PB2751; the indexed organization's answer, DOC-A.1-104 (4)).
        long slot = sequential ? _lastSlot : _pendingKey;
        if (slot < 1) return Status = FileStatusCode.RecordNotFound;
        // GR4's invalid key first ('23', which ignores locks), then the transactions' HOLDINGS (2.2.1.10); under commit
        // control the DELETE holds the record number, which stays held after the record is gone.
        if (_st.Get(slot, out var present) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
        if (present is null) return Status = FileStatusCode.RecordNotFound;
        if (HoldingsGate(() => [TransactionKeys.Record(RelativeStore.Key(slot))], out bool reevaluate) is { } locked)
            return Status = locked;                                        // '51'
        if (reevaluate) return Delete();                                   // against the file a rollback restored
        var deleted = _st.Delete(slot, Entered, out bool done, TransactionFor(RelativeStore.Key(slot), present));
        if (Failed(deleted) is { } refused) return Status = refused;       // '30' §9.1.13.6 item 1, the boundary too
        return Status = done ? FileStatusCode.Success : FileStatusCode.RecordNotFound;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR9): a NUMERIC comparison over RRNs against <paramref name="operand"/> (the
    /// RELATIVE KEY item's value, GR10) — forward search for =, &gt;, &gt;=; REVERSE search for &lt;, &lt;= (the
    /// first record satisfying the comparison searching the file in reverse order, GR9b). Not satisfied → invalid
    /// key '23' with the FPI invalidated (GR7/GR9c). Open mode must be input or I-O (GR1 → '47'). Each comparison is
    /// one SEEK of the tree, whose key order IS the numeric order of the record numbers (2.2.1.4).</summary>
    public string Start(string op, long operand)
    {
        if (StartOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                           // '23' §14.9.41 GR5
        var relation = op switch
        {
            "==" => SeekRelation.Equal,
            ">" => SeekRelation.GreaterThan,
            ">=" => SeekRelation.GreaterOrEqual,
            "<" => SeekRelation.LessThan,
            "<=" => SeekRelation.LessOrEqual,
            _ => throw new ArgumentException($"'{op}' is not a START relational operator", nameof(op)),
        };
        // A key below 1 names no record: = and the reverse comparisons find none, and the forward ones start below the
        // lowest record number, where the seek from 0 starts too.
        if (operand < 1 && relation is SeekRelation.Equal) return StartFail();
        if (_st.Seek(operand, relation, withValue: false, out var found, out _) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);           // a torn page: '30' §9.1.13.6 item 1
        return found is { } f ? StartAt(f) : StartFail();
    }

    /// <summary>START FIRST/LAST (COBOL-2002+; §14.9.41 GR11/GR12): the lowest / highest existing RRN; an empty
    /// (or absent optional) file → invalid key '23'.</summary>
    public string StartFirstLast(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();
        if (_st.Edge(last, out var edge) != StoreStatus.Ok) return Unsuccessful(FileStatusCode.PermanentError);
        return edge is { } e ? StartAt(e) : StartFail();
    }

    private string StartAt(long slot)
    {
        _fpi = slot; FpiValid = true; _inclusive = true;
        return StartPositioned();
    }
}
