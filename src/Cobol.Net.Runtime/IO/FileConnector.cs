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
        protected set { _status = value; EverAccessed = true; }
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

    /// <summary>True when the connector participates in file sharing (§9.1.15 — set by the registry when the
    /// SELECT declares SHARING/LOCK MODE or an OPEN carries a SHARING/RETRY phrase): its physical streams open
    /// with <see cref="FileShare.ReadWrite"/> so the §9.1.13.9 Table-19 registry — not the OS handle — is the
    /// sharing arbiter. An unshared connector keeps the default exclusive-writer OS posture, byte-for-byte.</summary>
    public bool SharedStreams { get; internal set; }

    /// <summary>True between a successful OPEN and the matching CLOSE.</summary>
    public abstract bool IsOpen { get; }

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
    // previous op be a successful READ (else '43'). Both reset at OPEN; every non-READ op clears the pair.

    /// <summary>True after an unsuccessful READ/START with no reposition since — the next sequential READ is '46'.</summary>
    protected bool LastReadUnsuccessful;

    /// <summary>True when the immediately-previous operation was a successful READ (the '43' gate).</summary>
    protected bool PrevOpWasSuccessfulRead;

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

    /// <summary>Record that a READ terminated in the record-operation-conflict condition (I-O status 51,
    /// §9.1.13.8): the READ is UNSUCCESSFUL — so a following sequential-access REWRITE/DELETE is '43'
    /// (§14.9.35 GR5 / §14.9.10 GR2) — but the file position indicator is UNCHANGED (§14.9.30 GR10a), so the
    /// read-position '46' poison (<see cref="LastReadUnsuccessful"/>) is NOT set and the READ may be retried.</summary>
    internal void NoteReadLockConflict() => PrevOpWasSuccessfulRead = false;

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
        LastReadUnsuccessful = false;
        PrevOpWasSuccessfulRead = false;
        try { return Status = OpenCore(mode); }
        catch (UnauthorizedAccessException) { return Status = FileStatusCode.PermissionDenied; }
        catch (IOException) { return Status = FileStatusCode.PermanentError; }
    }

    /// <summary>CLOSE the file (ISO §14.9.6): the not-open guard ('42', §9.1.13.7 item 2), then the
    /// organization's <see cref="CloseCore"/>. Sets and returns the status.</summary>
    public string Close()
    {
        if (!IsOpen) return Status = FileStatusCode.FileNotOpen;
        return Status = CloseCore();
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
