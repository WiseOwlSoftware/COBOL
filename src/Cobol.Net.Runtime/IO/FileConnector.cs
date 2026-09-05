// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The shared control logic of every file-organization connector (DESIGN-runtime-library §2.2): the ISO §9.1.13
/// FILE STATUS register, the §14.9.30 GR21 / §14.9.35 GR5 read-position guard pair, the OPEN/CLOSE preamble with
/// its open-mode bookkeeping (§14.9.49.4 GR6b–e USE-declarative mode scoping), the OPTIONAL-file absence flag,
/// the RECORD IS VARYING bounds (§13.18.43 GR9/GR10), and the record-area fit/store shaping. Organization-
/// specific record SELECTION and the org-specific verb surfaces stay on the subclasses
/// (<see cref="SequentialConnector"/> · <see cref="RelativeConnector"/> · <see cref="IndexedConnector"/>) —
/// the base is the TRUE common denominator only; anything the organizations do differently remains theirs.
/// </summary>
public abstract class FileConnector
{
    protected FileConnector(string hostPath, int recordWidth, int varyMin, int varyMax)
    {
        HostPath = hostPath;
        RecordWidth = recordWidth < 1 ? 1 : recordWidth;
        VaryMin = varyMin;
        VaryMax = varyMax;
    }

    /// <summary>The resolved host path of the physical file this connector is ASSOCIATED with — the connector's
    /// half of ISO §12.4.5.3 GR3 ("The ASSIGN clause specifies the association of the file connector referenced by
    /// file-name-1 to a physical file identified by device-name-1, literal-1, or the content of the data item
    /// referenced by data-name-1"). Empty = NOT YET ASSOCIATED, which only a bare <c>ASSIGN USING data-name-1</c>
    /// file can be: it carries no device-name/literal, so nothing identifies a physical file until the first
    /// OPEN/SORT/MERGE runs <see cref="Associate"/>. The setter is private and <see cref="Associate"/> is its ONE
    /// caller — GR3's "the association occurs at the time of execution of an OPEN, SORT, or MERGE statement".</summary>
    public string HostPath { get; private set; }

    /// <summary>
    /// ISO §12.4.5.3 GR3, reached from §14.9.27.4 GR26 — establish the connector-to-physical-file association.
    /// <para>GR3's lead sentence fixes the TIMING: <i>"The association occurs at the time of execution of an OPEN,
    /// SORT, or MERGE statement that referenced file-name-1"</i>, and GR3 b) fixes the VALUE: <i>"When the USING
    /// phrase of the ASSIGN clause is specified, the file connector referenced by file-name-1 is associated with a
    /// physical file identified by the content of the data item referenced by data-name-1 in the runtime element
    /// that executes the OPEN, SORT, or MERGE statement."</i> So this runs at every OPEN/SORT/MERGE, never once at
    /// registration — §9.1.21 exists to let one connector reach different physical files during a run unit, and the
    /// standard's own concepts annex states the consequence (D.19.9.2 NOTE: <i>"The MOVE statements only have an
    /// effect on the dynamic assignment when a subsequent OPEN statement for the file connector is executed."</i>).</para>
    /// <para>⛔ THE ASSOCIATION VALUE IS AN ARGUMENT OF THE STATEMENT, NEVER STATE ON THIS OBJECT (kb/Work PB673).
    /// BOTH arms of GR3 name the element that runs the statement, not the one that registered the connector —
    /// a) <i>"identified by the specification of device-name-1 or the value of literal-1 <b>in the source unit that
    /// specifies</b> the OPEN, SORT, or MERGE statement"</i>, b) <i>"identified by the content of the data item
    /// referenced by data-name-1 <b>in the runtime element that executes</b> the OPEN, SORT, or MERGE
    /// statement"</i> — and one file connector may be described by MANY such elements: an EXTERNAL file connector
    /// is one object per run unit shared by every describing element (§13.18.22.4 GR4 a), whose file control
    /// entries §12.4.5.3 GR1 b requires only to be CONSISTENT (unlike GR1 i's FILE STATUS, which must name the
    /// same external item). So the emitter renders the EXECUTING element's own ASSIGN specification at the
    /// OPEN/SORT/MERGE site and passes it here; a connector-held source answered with the LAST installer's.</para>
    /// <para>One entry point for both arms, never a second mechanism: <paramref name="dynamic"/> selects GR3 b's
    /// content rules (the implementor's §12.4.5.3 GR4 determination, below) over GR3 a's plain literal.</para>
    /// <para>An OPEN of an ALREADY-OPEN connector is unsuccessful at §14.9.27.4 GR2's '41' and GR25's "the file is
    /// not affected", so the association is left standing: re-pointing an open connector would strand its streams
    /// and its physical-file-table registration on the old path.</para>
    /// <para>Returns null when the association stands, or the I-O status of a failed one. The allowable CONTENT of
    /// data-name-1 and the consistency rules are the implementor's (§12.4.5.3 GR4; Annex A.1 items 10 and 73) —
    /// COBOL.NET's determination is in docs/CONFORMANCE.md §7 (DOC-A.1-73): the content with leading and trailing
    /// SPACES removed is the assign target and is then mapped to a host path exactly as literal-1 would be; content
    /// that is empty after that removal, or that carries a control character, names no physical file, so the
    /// association cannot be made and §9.1.13.6 item 2's '31' is the status the standard reserves for exactly this
    /// ("A permanent error exists during execution of an OPEN statement because the content of the data item
    /// referenced by the data-name specified in the USING phrase of the file control entry is not consistent with
    /// the specification for the device-name or literal in the ASSIGN clause of that file control entry").</para>
    /// </summary>
    /// <param name="spec">The EXECUTING element's ASSIGN specification: the content of data-name-1 when
    /// <paramref name="dynamic"/> (GR3 b), else the value of literal-1 / device-name-1 (GR3 a).</param>
    /// <param name="dynamic">True when the file control entry in the executing element writes the USING phrase.</param>
    public string? Associate(string spec, bool dynamic)
    {
        if (IsOpen) return null;                           // §14.9.27.4 GR2/GR25 — an already-open connector is untouched
        if (!dynamic)
        {
            // GR3 a) — literal-1/device-name-1 identify the physical file directly. The value is a source-text
            // constant, so re-resolving it at every OPEN is idempotent for the single-describer case and is what
            // makes the MULTI-describer case right: an EXTERNAL connector registered by one element must still be
            // associated with the file the EXECUTING element's own entry names. §12.4.5.2 SR4 already bars a
            // zero-length literal, so an empty spec here can only be the bare `ASSIGN USING` shape's placeholder —
            // it identifies nothing and leaves the registration's unassociated (empty) host path standing.
            if (spec.Length != 0) HostPath = CobolFile.ResolveHostPath(spec);
            return null;
        }
        string target = spec.Trim(' ');
        foreach (char ch in target)
            if (ch < ' ') return FileStatusCode.AssignNotConsistent;   // '31' §9.1.13.6 item 2
        if (target.Length == 0) return FileStatusCode.AssignNotConsistent;
        HostPath = CobolFile.ResolveHostPath(target);
        return null;
    }

    /// <summary>The record area's width in character positions.</summary>
    protected readonly int RecordWidth;

    /// <summary>The RECORD IS VARYING bounds (ISO §13.18.43 GR9/GR10); (-1,-1) = fixed-length records.</summary>
    protected readonly int VaryMin, VaryMax;

    /// <summary>True when the FD declares RECORD IS VARYING (the store then length-frames records).</summary>
    protected bool IsVarying => VaryMin >= 0;

    /// <summary>The latest I-O status (ISO §9.1.13). "00" until the first operation. ⛔ THE SETTER IS THE ONE
    /// I-O-STATUS ASSIGNMENT PATH, and it records that the connector was accessed (kb/Work PB63 / RV-15.28.4-2):
    /// §9.1.13.1 names CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and WRITE as the statements that set the
    /// I-O status, and §15.28.4 r2a's "never been opened, attempted to be opened, or otherwise attempted to be
    /// accessed" is exactly "no statement has set it" — so a CLOSE or READ on a never-opened connector (status 42 /
    /// 47) IS an attempted access. <see cref="EverAccessed"/> used to be a flag written at two hand-picked sites
    /// (OPEN and DELETE FILE), which made every other verb wrong by default.</summary>
    public string Status
    {
        get => _status;
        protected set
        {
            _status = value;
            EverAccessed = true;
            // §9.1.13.7 3): the '43' DELETE/REWRITE gate holds only when the IMMEDIATELY-previous operation on
            // this connector was a successful READ. Every status assignment IS an operation's outcome
            // (§9.1.13.1's closed statement set — CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK, WRITE), so
            // the gate drops HERE, at the one chokepoint — and the READ terminals re-arm it AFTER their own
            // assignment. Four verbs leaked the gate when each entry cleared it by hand: UNLOCK, DELETE FILE,
            // the already-open '41' OPEN and the sharing '38'/'61' OPEN arms all reported '00' on a following
            // DELETE where the spec requires '43' (kb/Work PB140).
            PrevOpWasSuccessfulRead = false;
        }
    }
    private string _status = FileStatusCode.Success;

    /// <summary>Set the I-O status directly (facade-level conditions: a locked-file OPEN, a REEL/UNIT CLOSE) — an
    /// attempted access like any other status assignment.</summary>
    public void SetStatus(string status) => Status = status;

    /// <summary>The connector has been opened, attempted to be opened, or otherwise attempted to be accessed (ISO
    /// §15.28.4 r2a / §15.29.4 r2a) — FUNCTION EXCEPTION-FILE(connector) returns two spaces until this is true.
    /// Set by the <see cref="Status"/> setter, i.e. by EVERY statement that assigns an I-O status; a never-touched
    /// SELECTed connector stays false.</summary>
    public bool EverAccessed { get; private set; }

    /// <summary>The file-name exactly as spelled in the SELECT clause (ISO §15.28.4 r1c/r2b, §15.29.4 r1c/r2b —
    /// "the file-name exactly as specified in the SELECT clause"), carried from the compiler's FileModel at
    /// registration (kb/Work PB63); the registry KEY is an emit-side namespace and is never displayed.</summary>
    public string SelectName { get; set; } = "";

    /// <summary>True for a SELECT OPTIONAL file (ISO §14.9.27 GR13/GR17).</summary>
    public bool IsOptional { get; set; }

    /// <summary>The registry's per-physical-file record-store table (kb/Work PB143 — §14.9.10.4 GR5's "removed
    /// from the physical file"): the keyed connectors attach their record images through it so every connector
    /// over one host path sees ONE store. Null (a connector constructed outside a registry, e.g. a focused
    /// unit test) keeps the private-store behavior.</summary>
    internal KeyedStoreTable? SharedStores { get; set; }

    /// <summary>True when the connector participates in file sharing (§9.1.15 — set by the registry when the
    /// SELECT declares SHARING/LOCK MODE or an OPEN carries a SHARING/RETRY phrase): its physical streams open
    /// with <see cref="FileShare.ReadWrite"/> so the §9.1.13.9 Table-19 registry — not the OS handle — is the
    /// sharing arbiter. An unshared connector keeps the default exclusive-writer OS posture, byte-for-byte.</summary>
    public bool SharedStreams { get; internal set; }

    /// <summary>True while the connector is in an open mode (ISO §9.1.4): set by a success-family OPEN, cleared
    /// by ANY completed CLOSE (§14.9.6.4 GR8 — and an unsuccessful close does not keep the open mode either).
    /// ⛔ ONE bit with ONE job (kb/Work PB140): the FPI's "optional file not present" state lives on
    /// <see cref="OptionalAbsent"/> and is NOT part of openness — folding it in forced every CloseCore to
    /// mutate the file position indicator §14.9.6.4 GR6 says a CLOSE leaves unchanged.</summary>
    public bool IsOpen => _openMode;
    private bool _openMode;

    /// <summary>The current open mode (meaningful while <see cref="ModeKnown"/>).</summary>
    protected FileOpenMode Mode;

    /// <summary>True while the file is open OR a failed OPEN recorded its attempted mode (ISO §14.9.49.4 GR6b —
    /// "in the process of being opened"); cleared by a successful CLOSE (§9.1.4 — the file is then in no mode).</summary>
    protected bool ModeKnown;

    /// <summary>The open-mode view for USE-declarative mode scoping (ISO §14.9.49.4 GR6b–e): <c>(int)</c> of the
    /// mode while open OR the ATTEMPTED mode of a failed OPEN; −1 after a successful CLOSE / before any OPEN.</summary>
    public int OpenModeView => ModeKnown ? (int)Mode : -1;

    /// <summary>The open mode while the connector IS OPEN (ISO §9.1.4), null otherwise.
    /// ⛔ DELIBERATELY NOT <see cref="OpenModeView"/>: that view also answers with the ATTEMPTED mode of a
    /// FAILED open (§14.9.49.4 GR6b's "in the process of being opened"), which is what USE-declarative mode
    /// scoping needs and is WRONG for any rule that asks whether the connector IS OPEN in a given mode.
    /// §14.9.21.4 GR3 is such a rule -- "the INITIATE statement may be executed only if the corresponding
    /// file connector is open in the extend mode or the output mode" -- and after an UNSUCCESSFUL OPEN OUTPUT the
    /// GR6b view still answers "output", which would let an INITIATE proceed into a connector that is not open.</summary>
    public FileOpenMode? OpenModeIfOpen => _openMode ? Mode : null;

    /// <summary>OPEN INPUT/I-O of an absent OPTIONAL file: the connector is open but no physical store exists
    /// (positioned at EOF / "not present", ISO §14.9.27 GR13; §9.1.13.2 item 5).</summary>
    protected bool OptionalAbsent;

    // Read-position state (ISO §14.9.30 GR21 / §14.9.35 GR5): a sequential READ after an unsuccessful READ with
    // no reposition is itself unsuccessful ('46'); a sequential-access REWRITE/DELETE requires the immediately-
    // previous op be a successful READ (else '43'). The '43' gate drops in the Status SETTER (every outcome is
    // an operation; the READ terminals re-arm it after assigning) — kb/Work PB140's chokepoint. The '46' poison
    // is NOT setter-managed: only a reposition (OPEN / START / a successful READ) clears it — a successful
    // intervening WRITE establishes no next record, so it must survive other verbs' status assignments.

    /// <summary>True after an unsuccessful READ/START with no reposition since — the next sequential READ is '46'.</summary>
    protected bool LastReadUnsuccessful;

    /// <summary>True when the immediately-previous operation was a successful READ (the '43' gate).</summary>
    protected bool PrevOpWasSuccessfulRead;

    /// <summary>A successful READ's terminal: assign the status (the setter drops the '43' gate like every
    /// other outcome) and then RE-ARM it — the ONE place a read outcome may do so (§9.1.13.7 3)). A successful
    /// READ is also a reposition, so the '46' poison clears (§14.9.30 GR21).</summary>
    protected string ReadSucceeded(string status)
    {
        Status = status;
        PrevOpWasSuccessfulRead = true;
        LastReadUnsuccessful = false;
        return status;
    }

    /// <summary>The file position indicator's "optional file not present" state (ISO §9.1.13.2 item 5 /
    /// §14.9.6.4 GR6) — exposed for the registry's REEL/UNIT surface, which must perform NO unit processing
    /// for an absent optional file.</summary>
    internal bool OptionalNotPresent => OptionalAbsent;

    // ── READ preconditions (ISO §14.9.30.4) ─────────────────────────────────────────────────────────────────
    //
    // ⛔ ONE COPY OF EACH READ PRECONDITION, FOR ALL FIVE READ ENTRIES (kb/Work PB336). The open-mode guard,
    // the '46' poison and the absent-OPTIONAL arm are ALL-FORMATS rules of the READ statement, so they belong
    // to the connector base and not to each organization. Written out per connector they were three copies of
    // one rule (SequentialConnector.Read · RelativeConnector.ReadSequential · IndexedConnector.ReadSequential),
    // and all three carried the SAME ordering defect: the absent-OPTIONAL arm was tested BEFORE GR21's '46',
    // so every sequential READ after the first on an absent optional file re-reported '10' — although the arm
    // itself had already armed the '46' the next READ owed. Order here is the rules' own order.

    /// <summary>The open-mode precondition every READ shares (ISO §14.9.30.4 GR2: "The open mode of the file
    /// connector referenced by file-name-1 shall be input or I-O. If it is any other value, the execution of the
    /// READ statement is unsuccessful and the I-O status value for file-name-1 is set to '47'."; §9.1.13.7 item
    /// 7). Returns the failing status, or <see langword="null"/> when the READ may proceed. Assigns nothing —
    /// the caller owns the single <see cref="Status"/> assignment, so the '43' gate drops exactly once.</summary>
    protected string? ReadOpenModeGuard() =>
        !IsOpen || Mode is FileOpenMode.Output or FileOpenMode.Extend ? FileStatusCode.ReadNotOpenForInput : null;

    /// <summary>The preconditions of a SEQUENTIAL READ, in the standard's own order: GR2's open mode ('47');
    /// then ISO §14.9.30.4 GR21's first sentence — "For a sequential READ statement, if the previous READ or
    /// START statement for the file connector was unsuccessful, then the READ statement is unsuccessful and the
    /// I-O status is set to '46'" (§9.1.13.7 item 6) — then the absent-OPTIONAL arm, which §9.1.13.4 item 1 c)
    /// scopes to "a sequential READ statement … attempted FOR THE FIRST TIME on a file described as optional and
    /// the physical file is not present" ('10'). GR21 shall be tested FIRST: it is what makes '10' first-time-
    /// only, because the '10' arm is itself an unsuccessful READ and arms the poison for its successor.
    /// Returns the failing status, or <see langword="null"/> when the READ may proceed.</summary>
    protected string? SequentialReadGuard()
    {
        if (ReadOpenModeGuard() is { } notOpen) return notOpen;                     // '47' GR2
        if (LastReadUnsuccessful) return FileStatusCode.NoValidNextRecord;          // '46' GR21 / §9.1.13.7 6
        // This READ is itself unsuccessful, so it arms GR21 for its successor — that is what makes '10'
        // first-time-only, and why the two arms shall be written in this order.
        if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.AtEnd; }   // '10' §9.1.13.4 1 c)
        return null;
    }

    /// <summary>The absent-OPTIONAL arm of a RANDOM (format-2) READ — ISO §9.1.13.5 item 3 b), "a START or
    /// random READ statement is attempted on a file described as optional and the physical file is not present"
    /// → '23'. TWO deliberate differences from <see cref="SequentialReadGuard"/>, both read off the rule texts:
    /// §14.9.30.4 GR21 opens "For a sequential READ statement", so a random READ never yields '46'; and 3 b)
    /// carries no "first time" qualifier, so an absent optional file answers '23' to EVERY random READ. The
    /// unsuccessful read still arms the poison — §9.1.13.7 item 6 b) ("The preceding READ statement referencing
    /// that file connector was unsuccessful") is not restricted to sequential reads, so the next SEQUENTIAL READ
    /// on a dynamic-access connector is GR21's '46'. Kept separate from <see cref="ReadOpenModeGuard"/> because
    /// the indexed connector establishes its key of reference (GR30/GR31) between the two.</summary>
    protected string? RandomReadAbsentOptionalGuard()
    {
        if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.RecordNotFound; }   // '23' 3 b)
        return null;
    }

    /// <summary>The length of the most recently read record (ISO §13.18.43 GR15 — the frame length on a varying
    /// file, the record width on a fixed one; the RECORD VARYING DEPENDING item receives it after a READ).</summary>
    public int LastReadLength { get; protected set; }

    // ── Record-lock identity (ISO §9.1.16 — record locking applies to EVERY organization; the READ/REWRITE/
    //    WRITE/DELETE lock rules §14.9.30 GR7–GR12 / §14.9.35 GR11–GR12 / §14.9.51 GR10–GR11 / §14.9.10 GR6–GR7
    //    are ALL-FORMATS rules). Each organization names its records for the PhysicalFileTable: the RRN for
    //    relative, the prime record key for indexed, the record's 1-based ordinal position for sequential (the
    //    natural analogue — a relative store's frames ARE ordinals). Empty string = no identified record.

    /// <summary>The lock identity of the record most recently made available by a READ (§9.1.16 — the record a
    /// post-read lock acquisition targets; §14.9.30 GR11c/d).</summary>
    public virtual string LastReadRecordId => "";

    /// <summary>The lock identity of the record a REWRITE/DELETE executed NOW would target (§14.9.35 GR11 /
    /// §14.9.10 GR6 — the pre-operation conflict check; <paramref name="recordImage"/> supplies the key slice
    /// for an indexed random/dynamic target, §14.9.35 GR23 / §14.9.10 GR3).</summary>
    public virtual string MutationTargetRecordId(string recordImage) => "";

    /// <summary>The lock identity of the record released by the most recent successful WRITE (§14.9.51 GR11 —
    /// the WITH LOCK acquisition target).</summary>
    public virtual string LastWrittenRecordId => "";

    // ── OPEN / CLOSE (the shared preamble; ISO §14.9.27 / §14.9.6) ───────────────────────────────────────────

    /// <summary>OPEN the file in <paramref name="mode"/> (ISO §14.9.27 / §9.1.13.4): the already-open guard
    /// ('41', §9.1.13.7 item 1), the attempted-mode bookkeeping (GR6b), the read-position reset, the ONE
    /// presence probe, the GR3 authority answer ('37'), the GR10 attribute comparison ('39') and the GR16 write
    /// capability answer ('37'), then the organization's <see cref="OpenCore"/> under the shared permission/I-O
    /// failure mapping ('37'/'30'). Sets and returns the status.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;
        Mode = mode;
        ModeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        OptionalAbsent = false;
        LastReadUnsuccessful = false;   // OPEN is a reposition; the '43' gate drops in the Status setter
        // ⛔ ONE presence probe per OPEN, taken HERE and handed down to OpenCore (kb/Work PB323). Two things
        // hang on it — GR3's authority answer and GR10's "the file exists" precondition — and every
        // organization's OpenCore needs it again for Table 18, so asking the host once and passing the answer
        // is both the single rule site and the only way the two questions cannot disagree across a racing
        // filesystem. It is HostFile.Probe, not File.Exists: see FilePresence for why two states are not enough.
        // Absent is the pre-probe default only to satisfy definite assignment on the throwing path, where
        // _openMode is false and nothing below reads it.
        var presence = FilePresence.Absent;
        string s;
        try
        {
            presence = HostFile.Probe(HostPath);
            // §14.9.27.4 GR3 — "If the file associated with file-name-1 is present and insufficient authority
            // exists to open the file, the execution of the OPEN statement is unsuccessful, and the I-O status
            // value in the file connector referenced by file-name-1 is set to '37'." (§9.1.13.6 item 6 b)
            // restates it for OPEN and DELETE FILE and adds "The ability to detect this is processor
            // dependent".) THE RULE LIVES HERE, ONCE, ABOVE THE ORGANIZATIONS: a refusal is not evidence of
            // absence, so it must never reach OpenCore's Table-18 arms, which would read it as '35' — or, for
            // an OPTIONAL file, as GR13's successful open over a file that is not there.
            // OUTPUT is excluded, and that exclusion is REQUIRED, not a convenience: GR18 makes OUTPUT a
            // CREATION that never consults presence, §9.1.13.6 item 5's '35' is defined only "for an OPEN
            // statement with the INPUT, I-O, or EXTEND phrase", and a directory the process may write but not
            // list legitimately accepts a new file — probing it answers Unauthorized while the OPEN OUTPUT
            // genuinely succeeds. When an OUTPUT target IS present-and-refused, the creating stream itself
            // raises UnauthorizedAccessException and the catch below returns the same '37'.
            if (presence is FilePresence.Unauthorized && mode is not FileOpenMode.Output)
                return Status = FileStatusCode.PermissionDenied;   // '37' §14.9.27.4 GR3 / §9.1.13.6 item 6 b)
            // §14.9.27.4 GR10 — the fixed-file-attribute comparison, BEFORE any organization work: "During the
            // execution of the OPEN statement when the file connector is matched with the file and the file
            // exists, the attributes of the file connector as specified in the file control paragraph and the
            // file description entry are compared with the fixed file attributes of the file. If the attributes
            // don't match, a file attribute conflict condition occurs, the execution of the OPEN statement is
            // unsuccessful, and the I-O status associated with file-name-1 is set to '39'." An UNSUCCESSFUL
            // OPEN leaves the file unaffected (GR25), so the check has to precede OpenCore, whose
            // OUTPUT/creation arms truncate. OUTPUT is excluded because GR18 makes it a CREATION ("If the OUTPUT
            // phrase is specified, the successful execution of the OPEN statement creates the file"), and §9.1.6
            // fixes a file's attributes at creation — so an OPEN OUTPUT ESTABLISHES them (below) rather than
            // being judged against the previous file's. GR10's own precondition is "the file EXISTS", which is
            // Present and nothing else; a file with no recorded attributes compares against nothing
            // (FixedFileAttributes.Load).
            if (mode is not FileOpenMode.Output && presence is FilePresence.Present
                && FixedFileAttributes.Load(HostPath) is { } recorded && recorded.Conflicts(DeclaredAttributes))
                return Status = FileStatusCode.FixedAttributeConflict;   // '39' §9.1.13.6 item 7
            // §14.9.27.4 GR16 — "If the I-O phrase is specified, the file shall support the input and output
            // statements that are permitted for the organization of that file when opened in the I-O mode. If
            // the file does not support those statements, the I-O status value for file-name-1 is set to '37'
            // and the execution of the OPEN statement is unsuccessful." §9.1.13.6 item 6 a) prices the other
            // write mode the same way: 1. "the EXTEND or OUTPUT phrase is specified but the file will not
            // support write operations", 2. the I-O restatement of GR16.
            // ⛔ THE WRITE CAPABILITY IS AN OPEN-CONTRACT QUESTION, ASKED HERE, ONCE, FOR EVERY ORGANIZATION —
            // never inside whichever arm happens to touch a stream. GR16 names no organization, and Table 20
            // makes it organization-independent: REWRITE sits under the I-O column for sequential, random AND
            // dynamic access, so "supports the statements permitted in the I-O mode" entails write capability
            // for every organization there is. Asking it in the arms is what kb/Work PB328 was — the sequential
            // arm asked it by accident (its I-O and EXTEND streams ARE write opens, so the shared catch below
            // turned a refusal into '37'), while the relative and indexed arms only read their store, so the
            // SAME read-only file answered '37' sequentially and '00' keyed, and the loss surfaced as a '30' at
            // CLOSE on a byte-identical file after READ and REWRITE had both reported success. Asked here, an
            // organization added later inherits the answer instead of re-earning it.
            // INPUT is excluded and the exclusion is REQUIRED, not an optimization: a read-only file supports
            // every statement Table 20 permits in the input mode, so probing write for it would refuse an OPEN
            // the operating environment carries out perfectly. §9.1.13.6 item 6 a) 3. ("the INPUT phrase is
            // specified but the file will not support read operations") is the input mode's own question, and
            // it is already answered eagerly by every organization — the sequential reader and the keyed
            // Attach() both open/read the store, and their UnauthorizedAccessException reaches the same '37'
            // through the catch below.
            // The Present guard is likewise required: an ABSENT target has no capability to probe, and the two
            // modes that reach OpenCore absent both CREATE (GR17's optional I-O/EXTEND, GR18's OUTPUT), where
            // the creating stream's own refusal is the authority answer.
            if (presence is FilePresence.Present && mode is not FileOpenMode.Input && !HostFile.PermitsWrite(HostPath))
                return Status = FileStatusCode.PermissionDenied;   // '37' §14.9.27.4 GR16 / §9.1.13.6 item 6 a)
            s = OpenCore(mode, presence);
        }
        catch (UnauthorizedAccessException) { s = FileStatusCode.PermissionDenied; }
        catch (IOException) { s = FileStatusCode.PermanentError; }
        // §9.1.13.6 item 1's '30' — "a permanent error exists and no further information is available". Since
        // §12.4.5.3 GR3 b) made the host path a RUNTIME VALUE (the content of data-name-1), an arbitrary string can
        // now reach the .NET path APIs, and the shapes they reject outside the IOException family — an embedded
        // separator the platform forbids, a form the platform does not support — must be an I-O status like every
        // other open failure, never an escaping exception. Associate() already screens the empty and control-
        // character content ('31'); this is the residue only the operating environment can judge.
        catch (ArgumentException) { s = FileStatusCode.PermanentError; }
        catch (NotSupportedException) { s = FileStatusCode.PermanentError; }
        _openMode = s[0] == '0';   // a success-family OPEN ('00'/'05'/'07') puts the connector in its open mode
        // §9.1.6: the fixed file attributes "apply to the file at the time it is created". The OPEN statement
        // creates the file in exactly two cases, and this condition is those two: GR18 (OUTPUT always creates)
        // and GR17 (an absent OPTIONAL file opened I-O or EXTEND is created "as if OPEN OUTPUT / CLOSE"). An
        // absent OPTIONAL file opened INPUT is NOT created (Table 18) and is deliberately outside the condition.
        // GR17's own precondition is "If the file is NOT PRESENT", which is Absent and nothing else — an
        // unauthorized I-O/EXTEND target never gets here (it returned '37' above), and an unauthorized OUTPUT
        // target that created successfully is covered by the first disjunct, GR18's creation.
        if (_openMode && (mode is FileOpenMode.Output
            || (presence is FilePresence.Absent && mode is FileOpenMode.IO or FileOpenMode.Extend)))
            FixedFileAttributes.Store(HostPath, DeclaredAttributes);
        return Status = s;
    }

    // ── The connector's declared fixed file attributes (ISO §9.1.6) ──────────────────────────────────────────

    /// <summary>This connector's fixed file attributes AS DECLARED — §14.9.27.4 GR10's "the attributes of the
    /// file connector as specified in the file control paragraph and the file description entry". Assembled
    /// HERE, once, for every organization: the record type and the minimum/maximum logical record size are the
    /// RECORD clause's (§13.18.43 GR9/GR10 for a varying file; a fixed file's single record width for both
    /// bounds). Organizations contribute only what is theirs — <see cref="CatalogOrganization"/> and, for an
    /// indexed file, <see cref="CatalogKeys"/>.</summary>
    public FixedFileAttributes DeclaredAttributes => new(
        CatalogOrganization,
        IsVarying,
        IsVarying ? VaryMin : RecordWidth,
        IsVarying ? VaryMax : RecordWidth,
        CatalogKeys);

    /// <summary>This organization's name in the persisted catalog — §9.1.6's primary attribute and nothing
    /// else, so one of the three values §9.1.6 names ("There are three organizations: sequential, relative, and
    /// indexed"): <see cref="FixedFileAttributes.Sequential"/>, <see cref="FixedFileAttributes.Relative"/> or
    /// <see cref="FixedFileAttributes.Indexed"/>. §9.1.7.2's record-sequential / line-sequential distinction is
    /// §9.1.6's separately listed <i>record delimiter</i>, not a fourth organization, and it is outside the
    /// §14.9.27.4 GR10 validated set (<see cref="FixedFileAttributes.Conflicts"/>).</summary>
    protected abstract string CatalogOrganization { get; }

    /// <summary>Which of §14.9.6.4 GR2's four categories this connector's PHYSICAL FILE falls in — the index
    /// Table 14 (§14.9.6.4 GR3) is read by, and the ONE place COBOL.NET's medium determination is stated in
    /// code. GR2 requires every supported physical file to be in exactly one category; making it an abstract
    /// property makes that total by construction, where it used to be a sentence in a doc comment on one CLOSE
    /// method (kb/Work PB235). See <see cref="PhysicalFileCategory"/> for why the answers are (a) and (d).</summary>
    public abstract PhysicalFileCategory Category { get; }

    /// <summary>This connector's record keys for the catalog (§9.1.6 — prime key, alternate keys, SUPPRESS WHEN,
    /// key collating sequence): index 0 the prime key, 1.. the alternates in declaration order. Only an indexed
    /// file has any; sequential and relative files answer the shared empty list.</summary>
    protected virtual IReadOnlyList<FixedFileAttributes.KeyDescriptor> CatalogKeys => [];

    /// <summary>CLOSE the file (ISO §14.9.6): the not-open guard ('42', §9.1.13.7 item 2), then the
    /// organization's <see cref="CloseCore"/> under the OPEN twin's exception mapping — an OS-level close
    /// failure (a flush that cannot complete, a store that cannot persist) is the §9.1.13.6 item 1 permanent
    /// error '30', where it previously escaped the emitted statement and aborted the run unit (kb/Work
    /// PB140). Successful or not, a completed CLOSE takes the connector out of its open mode (§14.9.6.4 GR8;
    /// the failure left it BOTH associated and open before). Sets and returns the status.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        string s;
        try { s = CloseCore(); }
        catch (UnauthorizedAccessException) { s = FileStatusCode.PermanentError; }
        catch (IOException) { s = FileStatusCode.PermanentError; }
        _openMode = false;
        return Status = s;
    }

    /// <summary>The organization-specific OPEN body: reset org state, create/load the physical store, and
    /// return the resulting status. Runs under the base's '37'/'30' exception mapping; the base has already
    /// recorded the attempted mode, reset the shared position state, and answered §14.9.27.4 GR3 and GR10.
    /// <para><paramref name="presence"/> is the base's ONE <see cref="HostFile.Probe"/> answer — an
    /// implementation must never re-ask the host, and must never call <c>File.Exists</c>, whose two-valued
    /// answer is what kb/Work PB323 was. For every mode but OUTPUT it is <see cref="FilePresence.Present"/> or
    /// <see cref="FilePresence.Absent"/> only: GR3 has already turned <see cref="FilePresence.Unauthorized"/>
    /// into '37' above, so a Table-18 arm may read it as a plain two-state answer. OUTPUT can still see
    /// Unauthorized, and ignores it — GR18 makes OUTPUT a creation that does not consult presence.</para></summary>
    protected abstract string OpenCore(FileOpenMode mode, FilePresence presence);

    /// <summary>The organization-specific CLOSE body (flush/persist/dispose); returns the resulting status.
    /// A successful close must clear <see cref="ModeKnown"/> (§9.1.4 — the file is then in no open mode).</summary>
    protected abstract string CloseCore();

    // ── Record-area shaping ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ §14.9.30.4 GR15's RECORD-AREA CATEGORY — true when the record area is "specified implicitly or
    /// explicitly as national", set by the emitter right after registration for exactly those files (kb/Work
    /// PB327). It decides which SPACE the short-record pad below uses, and nothing else. It is a property of the
    /// FD's record description, not of the physical file, so it is declared once per connector rather than
    /// re-derived per read.</summary>
    public bool NationalRecordArea { get; internal set; }

    /// <summary>Pad (right) or truncate <paramref name="s"/> to exactly <paramref name="width"/> characters —
    /// the ALPHANUMERIC fill (§14.9.30.4 GR15: "a trailing space is defined to be the alphanumeric space
    /// character"). One char is one byte on this channel.</summary>
    protected static string Fit(string s, int width) =>
        s.Length == width ? s : s.Length > width ? s[..width] : s.PadRight(width, ' ');

    /// <summary>Pad/truncate to the record width — the record-AREA image a READ makes available (a shorter
    /// varying record space-fills the area; its true length is <see cref="LastReadLength"/>).
    /// <para>⛔ A NATIONAL RECORD AREA PADS WITH THE NATIONAL SPACE (ISO §14.9.30.4 GR15 — "If the record-area
    /// associated with file-name-1 is specified implicitly or explicitly as national, a trailing space is defined
    /// to be the national space character"; kb/Work PB327). This channel carries one CHARACTER per BYTE, and a
    /// national space is the two bytes 0x00 0x20 (§13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins
    /// two, UTF-16BE), so the pad is written as the pair, aligned to the AREA's own even byte boundary: the
    /// national positions of the area start at even offsets, and an odd short read leaves a half position whose
    /// content §14.9.30.4 GR14/GR15 do not define. Padding the bytes with 0x20 instead would have manufactured
    /// U+2020 characters — the identical trap <c>CobolBits.NatWriteWindow</c> documents on the write side.</para></summary>
    protected string Fit(string s) => FitRecord(s, RecordWidth);

    /// <summary>⛔ THE ONE RECORD-AREA FIT (kb/Work PB327): <see cref="Fit(string,int)"/>'s alphanumeric fill, or
    /// GR15's national one when <see cref="NationalRecordArea"/> — every site that pads a record image to a
    /// declared span routes here, so the two fills cannot diverge. Truncation is identical in both (GR15's
    /// over-length arm truncates "on the right to the maximum size", in bytes).</summary>
    protected string FitRecord(string s, int width)
    {
        if (!NationalRecordArea || s.Length >= width) return Fit(s, width);
        var buf = new char[width];
        s.CopyTo(0, buf, 0, s.Length);
        for (int i = s.Length; i < width; i++) buf[i] = (i & 1) == 0 ? '\0' : ' ';
        return new string(buf);
    }

    /// <summary>⛔ THE ONE TRAILING-SPACE TRIM of a record image on the way OUT to a line-oriented stream — the
    /// inverse of <see cref="FitRecord"/>'s pad, and it has to shed the SAME space (kb/Work PB327). A national
    /// record area's trailing space is the national space, the two bytes 0x00 0x20 (§14.9.30.4 GR15;
    /// §13.18.60.4 GR8 / D-N1), so it sheds in PAIRS from the area's own even byte boundary: a plain
    /// <c>TrimEnd()</c> removed only the 0x20 of the last pair and stopped at its 0x00, leaving an odd-length
    /// record with half a national position on disk (measured: `01 L-REC PIC N(5).` holding N"AB" wrote nine
    /// bytes, `00 41 00 42 00 20 00 20 00`).</summary>
    protected string TrimRecordEnd(string image)
    {
        if (!NationalRecordArea) return image.TrimEnd();
        int n = image.Length & ~1;                                  // whole national positions only
        while (n >= 2 && image[n - 2] == '\0' && image[n - 1] == ' ') n -= 2;
        return image[..n];
    }

    /// <summary>The stored image of a record being written: a varying record keeps exactly its declared length
    /// (ISO §13.18.43 GR13 — truncate/pad the area image to it); a fixed record fills the record width. Returns
    /// null (→ '44') when a varying length violates the declared bounds (GR14 / §14.9.35 GR20).</summary>
    protected string? Stored(string image, int length)
    {
        if (!IsVarying) return Fit(image);
        int len = length >= 0 ? length : image.Length;
        if (len < VaryMin || len > VaryMax) return null;
        return image.Length == len ? image : image.Length > len ? image[..len] : image.PadRight(len, ' ');
    }
}
