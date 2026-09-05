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

    /// <summary>The resolved host path of the physical file (for DELETE FILE, ISO §14.9.10 Format 2).</summary>
    public string HostPath { get; }

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
    /// ('41', §9.1.13.7 item 1), the attempted-mode bookkeeping (GR6b), the read-position reset, then the
    /// organization's <see cref="OpenCore"/> under the shared permission/I-O failure mapping ('37'/'30').
    /// Sets and returns the status.</summary>
    public string Open(FileOpenMode mode)
    {
        if (IsOpen) return Status = FileStatusCode.FileAlreadyOpen;
        Mode = mode;
        ModeKnown = true;   // a FAILED open still records the attempted mode (GR6b "being opened")
        OptionalAbsent = false;
        LastReadUnsuccessful = false;   // OPEN is a reposition; the '43' gate drops in the Status setter
        // §14.9.27.4 GR10 — the fixed-file-attribute comparison, BEFORE any organization work: "During the
        // execution of the OPEN statement when the file connector is matched with the file and the file exists,
        // the attributes of the file connector as specified in the file control paragraph and the file
        // description entry are compared with the fixed file attributes of the file. If the attributes don't
        // match, a file attribute conflict condition occurs, the execution of the OPEN statement is
        // unsuccessful, and the I-O status associated with file-name-1 is set to '39'." An UNSUCCESSFUL OPEN
        // leaves the file unaffected (GR25), so the check has to precede OpenCore, whose OUTPUT/creation arms
        // truncate. OUTPUT is excluded because GR18 makes it a CREATION ("If the OUTPUT phrase is specified, the
        // successful execution of the OPEN statement creates the file"), and §9.1.6 fixes a file's attributes at
        // creation — so an OPEN OUTPUT ESTABLISHES them (below) rather than being judged against the previous
        // file's. A file with no recorded attributes compares against nothing (FixedFileAttributes.Load).
        bool existed = File.Exists(HostPath);
        if (mode is not FileOpenMode.Output && existed
            && FixedFileAttributes.Load(HostPath) is { } recorded && recorded.Conflicts(DeclaredAttributes))
            return Status = FileStatusCode.FixedAttributeConflict;   // '39' §9.1.13.6 item 7
        string s;
        try { s = OpenCore(mode); }
        catch (UnauthorizedAccessException) { s = FileStatusCode.PermissionDenied; }
        catch (IOException) { s = FileStatusCode.PermanentError; }
        _openMode = s[0] == '0';   // a success-family OPEN ('00'/'05'/'07') puts the connector in its open mode
        // §9.1.6: the fixed file attributes "apply to the file at the time it is created". The OPEN statement
        // creates the file in exactly two cases, and this condition is those two: GR18 (OUTPUT always creates)
        // and GR17 (an absent OPTIONAL file opened I-O or EXTEND is created "as if OPEN OUTPUT / CLOSE"). An
        // absent OPTIONAL file opened INPUT is NOT created (Table 18) and is deliberately outside the condition.
        if (_openMode && (mode is FileOpenMode.Output || (!existed && mode is FileOpenMode.IO or FileOpenMode.Extend)))
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

    /// <summary>The organization-specific OPEN body: reset org state, probe/create/load the physical store, and
    /// return the resulting status. Runs under the base's '37'/'30' exception mapping; the base has already
    /// recorded the attempted mode and reset the shared position state.</summary>
    protected abstract string OpenCore(FileOpenMode mode);

    /// <summary>The organization-specific CLOSE body (flush/persist/dispose); returns the resulting status.
    /// A successful close must clear <see cref="ModeKnown"/> (§9.1.4 — the file is then in no open mode).</summary>
    protected abstract string CloseCore();

    // ── Record-area shaping ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Pad (right) or truncate <paramref name="s"/> to exactly <paramref name="width"/> characters.</summary>
    protected static string Fit(string s, int width) =>
        s.Length == width ? s : s.Length > width ? s[..width] : s.PadRight(width, ' ');

    /// <summary>Pad/truncate to the record width — the record-AREA image a READ makes available (a shorter
    /// varying record space-fills the area; its true length is <see cref="LastReadLength"/>).</summary>
    protected string Fit(string s) => Fit(s, RecordWidth);

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
