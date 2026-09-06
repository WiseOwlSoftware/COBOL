// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native sequential file connector (COBOLNET_DESIGN §8) — the control logic the legacy
/// <c>SequentialFileHandler</c>/<c>FileRuntime</c> proved over the 364-NIST corpus, re-substrated from a byte buffer
/// to the record's <b>character image</b> (a C# <see cref="string"/>). A COBOL record IS a typed value; the only edge
/// where it becomes characters is here, at the on-disk boundary. Two on-disk shapes: <b>line-sequential</b>
/// (newline-framed text — <c>WriteLine</c>/<c>ReadLine</c>) and <b>record-sequential</b> (fixed-width blocks, plus the
/// print-control <c>WRITE … ADVANCING</c> raw stream). The ISO §9.1.13 I-O status codes and the §14.9.30/§14.9.35
/// read-position state machine live on the shared <see cref="FileConnector"/> base; the sequential-specific
/// stream/framing/LINAGE machinery is here.
/// </summary>
public sealed class SequentialConnector : FileConnector
{
    private readonly bool _lineSequential;

    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _afterAdvancing;        // a WRITE … ADVANCING happened → a trailing newline is written at CLOSE

    // The byte offset of the most recently read record's fixed-width block (for the in-place record-sequential
    // REWRITE) and the LOGICAL read offset it derives from. The logical offset counts characters CONSUMED from
    // the reader (Latin1 — one byte per character; a record-sequential file is pure fixed-width blocks): the
    // StreamReader BUFFERS, so BaseStream.Position is the buffer-fill boundary, never the read position —
    // deriving the block start from it corrupted the rewrite target (IX106A REWRITE-TEST-GF-02).
    private long _lastReadBlockStart = -1;
    private long _readOffset;
    // §14.9.30 GR15 / NOTE 3: the unread tail of an over-length LINE-SEQUENTIAL record — the file position indicator
    // "next unread character in the record". The next READ returns this (chunked to the record width) before reading a
    // new physical line, so a program using the GR15 multi-read pattern sees the whole logical line, not silent loss.
    private string? _lineRemainder;
    // §14.9.35.4 GR17 (line-sequential REWRITE in place): the byte anchor + data length of the last-read physical
    // line (the "record being replaced"), and whether that read transferred only PART of the record (an over-length
    // '06' read or a served remainder) — GR17a then makes a REWRITE '44'. Under Latin1 one char == one byte, so
    // _lineByteOffset tracks the physical byte position exactly (the same logical-offset discipline as _readOffset).
    private long _lineByteOffset;
    private long _lastLineStart = -1;
    private int _lastLineBytes;
    private bool _lastReadLinePartial;

    // ── Record-lock identity (ISO §9.1.16 on sequential organization) ────────────────────────────────────────
    // A sequential record's lock identity is its 1-based ORDINAL position in the physical file — two connectors
    // reading the same physical file agree on ordinals, and the successor relationship (§14.9.51 GR17) makes the
    // ordinal stable. Reads count from OPEN (INPUT/I-O always position at the start).
    // ⛔ WRITES DO NOT COUNT FROM ANYWHERE ON THIS CONNECTOR (kb/Work PB739). The ordinal of a released record
    // is a property of the PHYSICAL FILE — §14.9.51.4 GR19 says the records added by two sharing connectors
    // "follow the records present in the physical file", which is every connector's releases and not this
    // one's — so it is minted from the shared PhysicalFileTable.State.ReleasedOrdinal at the moment of the
    // release (ReleaseRecord). A per-connector base plus a per-connector count had both connectors calling
    // their first appended record ordinal 2. Unshared connectors have no shared state and no observable
    // identity: _writeOrdinal stays 0 and LastWrittenRecordId is empty, exactly as before.
    private long _readOrdinal;        // ordinal of the record most recently made available by Read
    private long _writeOrdinal;       // ordinal of the record most recently released BY THIS connector; 0 = none

    // The physical file's release generation (PhysicalFileTable.State.ReleaseGeneration) this connector's
    // read-ahead is coherent with (kb/Work PB753). Set wherever the reader's buffer is known to agree with the
    // medium — when the handle is created (OpenReader), when it is repositioned (SeekToRecord), and when THIS
    // connector is the one that released (NoteRelease) — and compared before every physical frame.
    private long _coherentAt;

    // §13.18.43 GR2 frame offsets of a RECORD VARYING file, index = ordinal − 1. Built LAZILY, on the first
    // backward read only: a forward READ walk must not pay for a facility it never uses, and a fixed-width file
    // never needs it at all (its offsets are arithmetic). The physical frame layout cannot change while a
    // connector is open — §14.9.35.4 GR16 makes a record-sequential REWRITE size-preserving — so one build per
    // OPEN is enough; OpenCore drops it.
    private List<long>? _varyingStarts;

    /// <summary>The ordinal a sequential Read in the given DIRECTION would deliver (the §14.9.30.4 GR9 pre-read
    /// conflict target — knowable BEFORE the read because sequential retrieval moves by exactly one record).
    /// GR21's "When the file is a sequential file" rules decide it: rule b) — the file position indicator
    /// established by a prior OPEN selects "the first existing record … regardless of whether NEXT or PREVIOUS is
    /// specified", so both directions target ordinal 1; rule c) — after a successful READ, the record whose
    /// number is greater than the indicator for NEXT and less than it for PREVIOUS. 0 means no such record exists
    /// (the beginning of the file), which rule e) makes the at-end condition.
    /// <para>⛔ <c>_readOrdinal == 0</c> IS rule b's antecedent ONLY WHILE START HAS NO SEQUENTIAL ARM. The rule
    /// says "established by a prior successful OPEN <b>or START</b> statement", and §14.9.41.3 SR2 does admit a
    /// START on a sequential-organization file ("If the organization of the file referenced by file-name-1 is
    /// sequential, either the FIRST or the LAST phrase shall be specified"). <c>KeyedIoBinder.BindStart</c>
    /// declines that form LOUDLY today (<c>BoundUnsupported</c>, "a later slice"), so OPEN and READ are the only
    /// two ways this connector's indicator is ever set. Whoever lands START FIRST/LAST here shall revisit THIS
    /// method: a START-established indicator is INCLUSIVE, so rule b would then have to select the started-at
    /// record itself in either direction rather than ordinal 1.</para></summary>
    internal long TargetReadOrdinal(bool previous) =>
        !previous || _readOrdinal == 0 ? _readOrdinal + 1 : _readOrdinal - 1;

    /// <inheritdoc/>
    /// <remarks>The record sequential organization's §14.9.30.4 GR9 pre-read conflict target: the ordinal
    /// <see cref="TargetReadOrdinal"/> names, which is knowable without reading because sequential retrieval
    /// moves by exactly one record. The remaining precondition beyond
    /// <c>SequentialReadReachesRetrieval</c> is a live stream.</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && _reader is not null && TargetReadOrdinal(previous) is > 0 and var t
            ? t.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "";   // ordinal 0 is GR21 rule e)'s beginning-of-file — no record, so nothing to conflict with

    /// <inheritdoc/>
    public override string LastReadRecordId =>
        _readOrdinal > 0 ? _readOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <inheritdoc/>  (a sequential REWRITE replaces the record obtained by the last successful READ, §14.9.35)
    public override string MutationTargetRecordId(string recordImage) => LastReadRecordId;

    /// <inheritdoc/>
    public override string LastWrittenRecordId => _writeOrdinal > 0
        ? _writeOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <summary>The RELEASE of a logical record to the operating environment — ISO §14.9.51.4 GR12,
    /// <i>"The successful execution of a WRITE statement releases a logical record to the operating
    /// environment"</i> — and the ONE place a sequential release happens. Every successful WRITE arm ends here.
    /// <para>For a §9.1.15 participant the release is literal: the writer is flushed, so the record is IN the
    /// physical file before the WRITE statement returns. It has to be. The physical file is the medium two
    /// sharing connectors share (the keyed organizations share an in-memory store instead), so a record still
    /// sitting in this connector's buffer is a record the other connector cannot see, cannot count, and — with
    /// <see cref="SharedAppendStream"/> positioning each write at the end as it stands — would be written
    /// straight over. The ordinal is minted from the same shared state, so it names the record's real place in
    /// the file (§9.1.16: <i>"While locked by a given file connector, a record is not accessible to another
    /// file connector"</i> — the lock is only as good as the identity).</para>
    /// <para>A connector holding the ONLY writable handle flushes at CLOSE as it always did: no other writer
    /// can observe the record, and a flush per record on the ordinary WRITE path is a syscall per record for an
    /// answer nobody reads.</para>
    /// <para>⛔ THE TWO HALVES HAVE DIFFERENT ANTECEDENTS, so they are two statements (kb/Work PB740). The
    /// FLUSH is owed whenever another writer may be on the physical file, which is a property of the §9.1.15
    /// file lock this connector's handle carries — <c>SHARING WITH ALL OTHER</c>, or a clause-less connector
    /// the arbiter has admitted alongside a writing sibling, or the same connector after
    /// <see cref="Reposture"/> widened it. The ORDINAL is the §9.1.16 record-lock identity, and a connector
    /// that registered NEITHER a SHARING nor a LOCK MODE clause sets no record locks to identify — §12.4.5.9.4
    /// GR1 b) 2. leaves that case to the implementor (<i>"the type of record locking for that opening … is
    /// defined by the implementor … or specify that the default is no record locking"</i>) and this compiler's
    /// determination is no record locking (<see cref="FileLockMode.None"/>) — so the ordinal is minted only
    /// where the physical file's shared state exists to mint it from. Gating the flush on the ordinal's
    /// antecedent left
    /// a clause-less pair sharing a physical file writing through two buffers, where a mid-record flush of one
    /// buffer can split a record around the other's.</para></summary>
    private void ReleaseRecord()
    {
        if (FileLockPosture.AdmitsAnotherWriter(HostShare))
        {
            _writer!.Flush();                      // GR12 — released, not merely buffered
            NoteRelease();                         // …and now visible, so no sibling's read-ahead may hide it
        }
        if (SharedPhysical is { } shared)
            _writeOrdinal = ++shared.ReleasedOrdinal;   // §9.1.16 identity, minted from the physical file's mint
    }

    /// <summary>⛔ THE ONE PLACE A RELEASE THAT HAS REACHED THE PHYSICAL FILE IS ANNOUNCED TO THE CONNECTORS
    /// READING IT (kb/Work PB753) — the read-side half of the release rule, and it is one rule for both verbs
    /// because the standard writes it once for each in the same words: §14.9.51.4 GR12, <i>"The successful
    /// execution of a WRITE statement releases a logical record to the operating environment"</i>, and
    /// §14.9.35.4 GR4, <i>"The successful execution of the REWRITE statement releases a logical record to the
    /// operating environment"</i>. §9.1.15 3) makes the concurrency legal — <i>"The sharing with all other mode
    /// allows concurrent access to a physical file through other file connectors specifying input, I-O, or
    /// extend mode"</i> — and §14.9.30.4 GR21 c)/d) say what the sibling's next READ owes: the record selected
    /// is <i>"the first existing record in the physical file whose relative key number is greater than the file
    /// position indicator"</i>, and THAT record <i>"is made available in the record area"</i>. A reader serving
    /// characters buffered before this release would make available a record the physical file no longer holds.
    /// <para>⛔ AND IT SETS THIS CONNECTOR'S OWN WATERMARK IN THE SAME BREATH. A connector's own release never
    /// invalidates its own read-ahead: a sequential REWRITE targets the record the last READ delivered, whose
    /// bytes are at or before the file position indicator, so nothing this connector will read again was
    /// touched. Advancing <c>_coherentAt</c> here is what keeps a READ/REWRITE loop on one I-O connector from
    /// re-filling its buffer once per record.</para>
    /// <para>⚠ It is NOT called where a release stays in this connector's own writer buffer. That case is the
    /// FLUSH's antecedent (see <see cref="ReleaseRecord"/>), and announcing bytes that have not reached the
    /// file would send a reader to a half-written frame instead of a whole stale one.</para></summary>
    private void NoteRelease()
    {
        if (Physical is { } physical) _coherentAt = ++physical.ReleaseGeneration;
    }

    /// <summary>⛔ THE READ-SIDE INVALIDATION, at the ONE point every physical read of this connector passes
    /// through (<see cref="NextFrame"/>): if a sibling connector has released a record to this physical file
    /// since this reader's buffer was filled, the buffer is discarded and the reader re-anchored at the file
    /// position indicator, so the next frame comes off the medium as it stands NOW (§14.9.30.4 GR21 c/d —
    /// see <see cref="NoteRelease"/> for the derivation).
    /// <para>The anchor is the LOGICAL offset, never <c>BaseStream.Position</c>: a <see cref="StreamReader"/>
    /// buffers ahead, so the base position is the buffer-fill boundary. That is the same fact
    /// <see cref="SeekToRecord"/> and <see cref="Reposture"/> are built on, and it is why a bare
    /// <c>DiscardBufferedData</c> would be wrong here — it would resume at the fill boundary and skip every
    /// record the buffer had already read ahead of.</para>
    /// <para>An over-length LINE SEQUENTIAL record's unread remainder (§14.9.30.4 GR15 / NOTE 3) is untouched
    /// on purpose: it is part of a record ALREADY made available, and <c>_lineByteOffset</c> is already past
    /// its physical line, so re-anchoring neither loses it nor re-reads it.</para>
    /// <para>⛔ A TRUNCATING SIBLING CANNOT ARISE, so no rule is written for one: every cell of Table 19's two
    /// OUTPUT request rows is <i>Unsuccessful open</i> (§14.9.27.4; §9.1.13.9 1) e) — <i>"An attempt is made to
    /// open a physical file in the output mode and the physical file is currently open by another file
    /// connector"</i>), so nothing may truncate this file while this connector holds it open.</para></summary>
    private void EnsureReaderCoherent()
    {
        if (Physical is not { } physical || physical.ReleaseGeneration == _coherentAt) return;
        _coherentAt = physical.ReleaseGeneration;
        if (_reader is not { BaseStream.CanSeek: true } reader) return;
        reader.BaseStream.Seek(_lineSequential ? _lineByteOffset : _readOffset, SeekOrigin.Begin);
        reader.DiscardBufferedData();
    }

    /// <summary>Re-derive this connector's ISO §9.1.15 file lock while it is open — see
    /// <see cref="FileConnector.Reposture"/> for why the registry calls it and why it shall not escape. A host
    /// share mode is fixed when the handle is created, so "widen" means <b>rebuild the handle</b>, at the same
    /// logical position, with nothing else about the connector disturbed.
    /// <para>The position is already tracked, and not by the base stream: <c>StreamReader</c> buffers ahead, so
    /// <c>BaseStream.Position</c> is the buffer-fill boundary and never the read position — the logical offsets
    /// (<c>_readOffset</c> for the framed readers, <c>_lineByteOffset</c> for LINE SEQUENTIAL; Latin-1, so one
    /// character is one byte) are the file position indicator's byte address, and the fresh reader is seeked
    /// straight to it. Everything derived from it survives untouched: the §14.9.30.4 GR15 unread remainder, the
    /// §14.9.35.4 GR17 REWRITE anchors (absolute file offsets, still valid on the new handle) and the §9.1.16
    /// read ordinal. A writer is FLUSHED first (§14.9.51.4 GR12) and reopened
    /// <see cref="FileMode.Append"/> — the current physical end is exactly where it was, and Append never
    /// truncates, so an <c>OPEN OUTPUT</c> connector keeps the file it has created.</para>
    /// <para>⚠ A writer, or an I-O reader, holds WRITE access, and the outgoing handle's own share mode is what
    /// denies a second one — so those cannot be opened before the old handle is released and the rebuild is
    /// dispose-then-open. If the host refuses the new posture in that window (only a foreign process can cause
    /// it) the OLD posture is reopened, which restores the connector exactly. A reader with no write access
    /// needs no window at all, so it is open-then-dispose. No mode holds both a reader and a writer — INPUT and
    /// I-O have only <c>_reader</c>, OUTPUT and EXTEND only <c>_writer</c> — so a half-repostured connector is
    /// not expressible.</para></summary>
    internal override void Reposture(FileShare share)
    {
        if (share == HostShare) return;
        if (!IsOpen) { base.Reposture(share); return; }
        if (_reader is { } reader)
        {
            long at = _lineSequential ? _lineByteOffset : _readOffset;
            if (Mode == FileOpenMode.IO)
            {
                // WRITE access: the outgoing handle's share mode can forbid the incoming one, so release first.
                reader.Dispose();
                _reader = null;
                _reader = OpenReader(share, at, HostShare);
            }
            else
            {
                var fresh = OpenReader(share, at, null);
                reader.Dispose();
                _reader = fresh;
            }
        }
        else if (_writer is { } writer)
        {
            writer.Flush();                        // GR12 — nothing buffered may cross the handle boundary
            writer.Dispose();
            _writer = null;
            // FileMode.Append for BOTH the OUTPUT and the EXTEND writer: the physical end is where the flushed
            // handle stood, and Append does not truncate, so an OPEN OUTPUT connector keeps what it created.
            _writer = OpenWriter(FileMode.Append, share, HostShare);
        }
        base.Reposture(share);
    }

    /// <summary>⛔ THE ONE SPELLING OF THIS CONNECTOR'S READ HANDLE — the INPUT and I-O arms of
    /// <see cref="OpenCore"/> and the rebuild in <see cref="Reposture"/> are the same stream, and a second
    /// spelling would be a second answer to its encoding, its access and its <see cref="FileOptions"/>. The I-O
    /// arm asks for <see cref="FileAccess.ReadWrite"/> because §14.9.35 GR3's REWRITE writes through this
    /// reader's <c>BaseStream</c>, and takes NO <see cref="FileOptions.SequentialScan"/> for the same reason: it
    /// seeks, and the sequential-access hint asks the host to evict what a seek comes back for.
    /// <para><paramref name="at"/> is the byte the next READ shall deliver, and <paramref name="fallbackShare"/>
    /// the posture to restore when the host refuses <paramref name="share"/> and the outgoing handle is already
    /// gone (null = it is still open, so there is nothing to restore and the refusal propagates to a caller that
    /// still has one).</para></summary>
    private StreamReader OpenReader(FileShare share, long at, FileShare? fallbackShare)
    {
        StreamReader Open(FileShare s) => new(HostFile.OpenConnectorStream(HostPath, FileMode.Open,
            Mode == FileOpenMode.IO ? FileAccess.ReadWrite : FileAccess.Read, s,
            Mode == FileOpenMode.IO ? FileOptions.None : FileOptions.SequentialScan), Encoding.Latin1);
        StreamReader r;
        try { r = Open(share); }
        catch (IOException) when (fallbackShare is { } old) { r = Open(old); }
        if (at != 0) r.BaseStream.Seek(at, SeekOrigin.Begin);
        // A brand-new handle has read nothing, so it agrees with the medium by construction (kb/Work PB753).
        _coherentAt = Physical?.ReleaseGeneration ?? 0;
        return r;
    }

    /// <summary>⛔ THE ONE SPELLING OF THIS CONNECTOR'S WRITE HANDLE — the OUTPUT arm
    /// (<see cref="FileMode.Create"/>), the EXTEND arm (<see cref="FileMode.Append"/>) and the rebuild. The role
    /// decides plain-versus-repositioning from the posture; this decides the newline and the encoding, once.</summary>
    private StreamWriter OpenWriter(FileMode mode, FileShare share, FileShare? fallbackShare)
    {
        StreamWriter Open(FileShare s) => new(HostFile.OpenConnectorWriteStream(HostPath, mode, s),
            Encoding.Latin1) { NewLine = "\r\n" };
        try { return Open(share); }
        catch (IOException) when (fallbackShare is { } old) { return Open(old); }
    }

    /// <summary>The count of records ALREADY IN the physical file, in the framing this connector reads — the
    /// write-ordinal base a sharing-active <c>OPEN EXTEND</c> continues from (§14.9.51.4 GR18: <i>"If there are
    /// records in the physical file, the first record written after the execution of the OPEN statement with
    /// the EXTEND phrase is the successor of the last record in the physical file"</i>; GR19 fixes the
    /// measurement POINT for the shared case: <i>"the added records follow the records present in the physical
    /// file when it was opened"</i>).
    /// <para>⛔ IT IS MEASURED BEFORE THIS OPEN'S OWN WRITER EXISTS, and that ordering is the fix for kb/Work
    /// PB713, not an optimization. The measurement used to run from <c>FileRegistry.SharedOpenAttempt</c> AFTER
    /// <c>FileConnector.Open</c> returned, so it took a SECOND handle on a path this connector already held for
    /// WRITE: the line-sequential arm's <c>File.ReadLines</c> and the varying arm's three-argument
    /// <c>FileStream</c> both request <see cref="FileShare.Read"/>, which does not admit the outstanding Write
    /// access, so the operating environment refused — and, being outside <c>FileConnector.Open</c>'s try, the
    /// refusal escaped the run unit as an unhandled <c>IOException</c> rather than as an I-O status. Both halves
    /// are addressed here: the measurement happens where no second handle exists, and it happens INSIDE the
    /// OPEN body, where §14.9.27.4 GR25's <i>"the file is not affected"</i> and §9.1.13.6 item 1's '30' are the
    /// only outcomes an unreadable file can have. GR19 permits the earlier point because an <c>OPEN EXTEND</c>
    /// writes nothing: the record count at the writer's creation and at the OPEN's completion are the same
    /// number.</para>
    /// <para>The stream is <see cref="HostFile.OpenAuxiliary"/> — the ONE role for a bookkeeping handle, share
    /// <see cref="FileShare.ReadWrite"/> — so the ordering above is belt AND braces: a sibling connector of this
    /// run unit holding the same physical file under §9.1.15 sharing cannot refuse it either.</para></summary>
    private long ExistingRecordCount()
    {
        if (IsVarying)
        {
            // The frame WALK, not ReadStore: FrameStarts seeks over every payload instead of materializing it,
            // so counting an existing file costs one pass and no record storage (ReadStore allocated a string
            // per record purely to take .Count of the list).
            using var fs = HostFile.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);
            return RecordFraming.FrameStarts(fs).Count;
        }
        if (_lineSequential)
        {
            // Counted through the connector's OWN encoding (Latin1 — one byte per character), the same reader
            // shape OpenCore's INPUT arm builds, so "record" here means exactly what a READ of this file would
            // deliver. File.ReadLines would have decoded UTF-8 and allocated a string per line to discard it.
            using var fs = HostFile.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);
            using var r = new StreamReader(fs, Encoding.Latin1);
            long n = 0;
            while (r.ReadLine() is not null) n++;
            return n;
        }
        // Fixed-width record-sequential: arithmetic over the file's length. No handle at all, which is why this
        // arm never showed PB713 (FileInfo.Length is metadata).
        return RecordWidth > 0 ? new FileInfo(HostPath).Length / RecordWidth : 0;
    }

    // A varying file's records are length-framed on disk (the ONE RecordFraming 4-byte little-endian length
    // prefix per record — the same framing the keyed connectors' store uses; the physical format is
    // implementor-defined, §13.18.43 GR2, and only self-consistency matters since producer and consumer run on
    // this connector). WRITE/REWRITE outside the VaryMin/VaryMax bounds is the GR14 '44'; a record-sequential
    // REWRITE must also match the replaced record's size (§14.9.35 GR16).

    // ── LINAGE logical-page state (ISO §13.18.34 / §14.9.51 GR26–28) ─────────────────────────────────────────
    // The LINAGE feature is COUNTER-ONLY on the physical stream: each logical page is contiguous to the next
    // with no additional spacing (§13.18.34 GR8 — no margin blank lines, nothing emitted at page wrap), so the
    // connector adds only the counter machine + the end-of-page flag over the unchanged pending-advance stream.
    // ⛔ THE OPERAND VALUES ARRIVE WITH THE STATEMENT (a LinagePage?, null = the file has no LINAGE clause), and
    // ONE argument serves both operand forms — a literal operand renders a constant, a data-name operand renders
    // the EXECUTING element's field read (§13.18.34 GR6a/GR6b). What the connector keeps is the page MODEL most
    // recently determined, because GR6 says "the value applies to the next logical page"; it keeps no source, or
    // a shared connector would answer with whichever element/activation installed one last (kb/Work PB673).
    private int _pageBody;      // page size — the writable page-body line count (GR2)
    private int _footing;       // footing start (GR3 — footing area = [footing, page size] inclusive); 0 = none
    private int _top, _bottom;  // top/bottom margins (GR4/GR5) — counted into the logical page (GR1), unprinted

    /// <summary>The LINAGE-COUNTER register (ISO §8.4.3.14): the line number at which the device is positioned
    /// within the current page body (§13.18.34 GR7). Only this connector (the I-O control system) modifies it (GR7b).</summary>
    public long LinageCounter { get; private set; }

    /// <summary>The end-of-page condition of the most recent WRITE (ISO §14.9.51 GR26): page overflow (GR26a) or
    /// printing/spacing within the footing area (GR26b). Reset at the start of every counter-advancing write.</summary>
    public bool EndOfPage { get; private set; }

    /// <summary>ISO §13.18.34 GR6 b) 1 — establish the logical page model <i>"at the completion of an OPEN
    /// statement with the OUTPUT phrase"</i>, plus GR7 d)'s counter reset. Called by the registry after a
    /// SUCCESSFUL OPEN OUTPUT, with the page the EXECUTING element's own LINAGE clause evaluates to.</summary>
    public void BeginLinagePage(LinagePage page)
    {
        EvaluateLinage(page);
        LinageCounter = 1;   // GR7d — the counter is set to one at OPEN OUTPUT
        EndOfPage = false;
    }

    /// <summary>Adopt the LINAGE operand values for the (next) logical page (ISO §13.18.34 GR6: at OPEN OUTPUT
    /// completion, during WRITE ADVANCING PAGE, and during a page-overflow WRITE — "the value applies to the next
    /// logical page"). GR6's value rules (page size &gt; 0; 0 &lt; footing ≤ page size — footing 0 here = the phrase
    /// is absent, GR1) violated ⇒ the EC-I-O-LINAGE exception condition (§13.18.34 GR6) — the EC subsystem is a
    /// later slice, so the seam fails LOUD (COBOLNET_DESIGN §1.4), never a silent bad page model.</summary>
    private void EvaluateLinage(LinagePage page)
    {
        var (body, footing, top, bottom) = page;
        if (body <= 0 || footing < 0 || footing > body)
            throw new InvalidOperationException(
                $"EC-I-O-LINAGE: LINAGE values page-size={body}, footing={footing} violate ISO §13.18.34 GR6 "
                + "(page size > 0; 0 < footing <= page size); the EC exception-condition machinery is a later slice");
        (_pageBody, _footing, _top, _bottom) = (body, footing, top, bottom);
    }

    /// <summary>
    /// Advance the LINAGE-COUNTER for one WRITE (ported VERBATIM from the legacy
    /// <c>SequentialFileHandler.AdvanceLinageCounter</c>, proven over the SQ goldens) plus the GR6b page-transition
    /// re-evaluation. <paramref name="lines"/> &lt; 0 = ADVANCING PAGE. Rules:
    /// <list type="bullet">
    /// <item>ADVANCING PAGE resets the counter to 1 (§13.18.34 GR7c1); no observable end-of-page (§14.9.51 SR18
    ///   bars PAGE+EOP in one statement, so the flag stays false).</item>
    /// <item>ADVANCING n adds n (GR7c2); a plain WRITE adds 1 (GR7c3 — the caller passes 1).</item>
    /// <item>Counter past the page body ⇒ page overflow (§14.9.51 GR26a): the device repositions to the FIRST
    ///   line of the next logical page, counter := 1 (GR7c4 — never a modulo carry), overflow end-of-page.</item>
    /// <item>Else, FOOTING specified and counter at/past the footing start ⇒ footing end-of-page (GR26b).</item>
    /// <item>⚖ <b>counter == page body IS AN ADJUDICATED BOUNDARY — do not "correct" either comparison to match
    ///   GR26's printed words.</b> Arm a) as printed fires at counter ≥ page size and arm b) is clamped to
    ///   counter &lt; page size; at counter == page size those cannot both hold with §13.18.34 GR2 (all
    ///   page-size lines "may be written or spaced"), GR3 (the footing area is [footing, page size]
    ///   INCLUSIVE) or GR26's own lead sentence (the lines "do not fit within the current page body"). Under
    ///   the printed arm a) the line NUMBERED page size could never receive a record — an N-line body would
    ///   hold N−1 written lines forever, FOOTING phrase or not. The strict boundary below is
    ///   docs/CONFORMANCE.md §4 "DETERMINATION — the §14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page
    ///   size" (kb/Work PB686), which carries the survey and the NIST SQ201M evidence; it is pinned at the
    ///   boundary by tests/conformance/2023/pb686_linage_gr26_boundary.cob (+ the 85 twin) on BOTH arms of the
    ///   FOOTING dispatch, and by LinageConformanceTests.Gr26ab_CounterEqualsBody_IsFootingEopNotOverflow.</item>
    /// <item>GR6b2/3: at the two page transitions — AFTER the overflow decision was made against the OLD page
    ///   body — re-evaluate the operand values; they apply to the NEXT logical page (§13.18.34 GR6).</item>
    /// </list>
    /// The caller invokes this AFTER the physical write — the AT END-OF-PAGE branch then observes the
    /// post-advance counter (SQ201M's footing lines print the triggering write's line number).
    /// </summary>
    private void AdvanceLinageCounter(int lines, LinagePage page)
    {
        EndOfPage = false;   // reset at the start of every counter-advancing write (the legacy entry reset)
        if (_pageBody <= 0) return;
        if (lines < 0)
        {
            // ADVANCING PAGE: the counter resets to one on the new page (§13.18.34 GR7c1).
            LinageCounter = 1;
            EvaluateLinage(page);   // GR6b2 — values for the NEXT logical page
            return;
        }
        // ADVANCING n (n >= 0) or plain WRITE (n = 1): the counter is incremented (GR7c2/c3).
        LinageCounter += lines;
        if (LinageCounter > _pageBody)
        {
            // Page overflow (§14.9.51 GR26a): the line does not fit in the page body — the device repositions
            // to the first writable line of the succeeding page and the counter resets to 1 (GR7c4).
            // ⚖ STRICT `>`, NOT `>=` — the adjudicated boundary (docs/CONFORMANCE.md §4, kb/Work PB686). GR26a
            // as printed says "equal to or exceeds the page size"; `>=` here would push a record that lands on
            // the LAST body line onto the next page and make that line unwritable forever, against §13.18.34
            // GR2. The doc comment above carries the full derivation and the survey.
            LinageCounter = 1;
            EndOfPage = true;
            EvaluateLinage(page);   // GR6b3 — after the overflow decision against the OLD body; next-page values
        }
        else if (_footing > 0 && LinageCounter >= _footing)
        {
            // Footing-area end-of-page (§14.9.51 GR26b): FOOTING is specified and this WRITE prints or spaces
            // within the footing area (counter at/past the footing start, still within the page body).
            // ⚖ NO UPPER CLAMP — the adjudicated boundary (docs/CONFORMANCE.md §4, kb/Work PB686). GR26b as
            // printed adds "and is less than the page size"; honouring that clamp would exclude the page-size
            // line, which §13.18.34 GR3 places INSIDE the footing area ("between the footing start and the
            // page size, inclusive"). The overflow arm above already took every counter past the body, so
            // reaching here means counter ≤ page body and the clamp has nothing left to exclude but GR3's own
            // last line. IBM Enterprise COBOL documents the footing condition with no upper clamp likewise.
            EndOfPage = true;
        }
    }

    /// <summary>True between a successful OPEN and the matching CLOSE (an absent-OPTIONAL INPUT open counts —
    /// the connector is open at EOF with no physical stream).</summary>
    public SequentialConnector(string hostPath, int recordWidth, bool lineSequential,
        int varyMin = -1, int varyMax = -1)
        : base(hostPath, recordWidth, varyMin, varyMax)
    {
        _lineSequential = lineSequential;
    }

    /// <inheritdoc/>
    /// <remarks>Both §9.1.7.2 types of sequential file — record sequential and line sequential — record this
    /// ONE organization, because §9.1.6 names exactly three ("There are three organizations: sequential,
    /// relative, and indexed") and the delimiter that separates the two types is §9.1.6's SEPARATELY listed
    /// <i>record delimiter</i>, not a fourth organization. The delimiter is deliberately outside the
    /// §14.9.27.4 GR10 validated set — see <see cref="FixedFileAttributes.Conflicts"/>: on a sequential medium
    /// the standard answers a delimiter or record-length disagreement with a SUCCESSFUL completion (§9.1.13.2
    /// item 5's '06', item 3's '04'), not with a refused OPEN, and re-reading a print or report file under a
    /// line-sequential description is exactly the idiom those statuses are for.</remarks>
    protected override string CatalogOrganization => FixedFileAttributes.Sequential;

    /// <summary>§14.9.6.4 GR2 a) — <i>"A file whose input or output medium is such that the concepts of rewind
    /// and units have no meaning."</i> A sequential connector holds one <see cref="FileConnector.HostPath"/> on
    /// mass storage; there is no reel, unit or volume behind it. FORCED, not chosen: §9.1.13.2 item 6 defines
    /// the '07' this connector reports for the NO REWIND and REEL/UNIT phrases as the status of a CLOSE that
    /// <i>"references a physical file on a non-reel/unit medium"</i>, and Table 14 prints symbol g only in the
    /// Non-unit column (kb/Work PB235; documented at `docs/CONFORMANCE.md` §7, A.1 item 24).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonUnit;

    // ── OPEN / CLOSE ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The sequential OPEN body (ISO §14.9.25 / §9.1.13.4) — the shared preamble/guards, the ONE
    /// presence probe behind <paramref name="presence"/>, and §14.9.27.4 GR3's '37' all live on
    /// <see cref="FileConnector.Open"/>.</summary>
    protected override string OpenCore(FileOpenMode mode, FilePresence presence)
    {
        _afterAdvancing = false;
        _lastReadBlockStart = -1;
        _readOffset = 0;
        _lineRemainder = null;
        _lineByteOffset = 0;
        _lastLineStart = -1;
        _lastLineBytes = 0;
        _lastReadLinePartial = false;
        _readOrdinal = 0;
        _varyingStarts = null;   // rebuilt on demand against THIS open's physical file
        // §9.1.16 record-lock identity: this connector has released nothing yet. The MINT lives on the shared
        // physical-file state (kb/Work PB739), and only the OUTPUT and EXTEND arms below touch it — INPUT and
        // I-O release no records through this connector and shall not disturb another connector's numbering.
        _writeOrdinal = 0;
        // Table 18's "file is available" / "file is unavailable" axis. The base has already answered
        // §14.9.27.4 GR3, so on every mode that consults this an Unauthorized probe has become '37' and can
        // never be read here as "unavailable" (kb/Work PB323).
        bool exists = presence is FilePresence.Present;
        {
            switch (mode)
            {
                case FileOpenMode.Input:
                    if (!exists)
                    {
                        if (!IsOptional) return FileStatusCode.FileNotFound;
                        OptionalAbsent = true;                 // positioned at EOF (ISO §9.1.13.2 item 5b)
                        return FileStatusCode.OptionalFileNotFound;
                    }
                    // The §9.1.15 file lock this connector's handle carries is DERIVED — by FileLockPosture,
                    // from the arbitrated sharing mode and the connectors Table 19 has already admitted on this
                    // physical file — and handed down by the registry as HostShare (kb/Work PB740). It is never
                    // decided here: the Table-19 registry is the in-run-unit arbiter and the handle's share mode
                    // is the file lock against OTHER RUN UNITS, and a boolean cannot be both.
                    _reader = OpenReader(HostShare, at: 0, fallbackShare: null);
                    NoticeIfLayoutDisagrees();
                    break;

                case FileOpenMode.Output:
                    // Table 19 admits an incoming EXTEND/I-O against a connector already open in the OUTPUT mode
                    // (its existing-side column group is `extend I-O output`), so the OUTPUT writer takes the
                    // same posture-derived role the EXTEND writer does — a second writer is possible here too
                    // (kb/Work PB740; the anchoring rule itself is PB739's).
                    _writer = OpenWriter(FileMode.Create, HostShare, fallbackShare: null);
                    // OPEN OUTPUT truncates, so the physical file holds no records and the shared mint restarts
                    // at 0. §14.9.51.4 GR17 is the sequential-organization rule for it: "The successor
                    // relationship of a sequential file is established by the order of execution of WRITE
                    // statements when the physical file is created" — a creation starts the relationship over,
                    // so the first record released is ordinal 1. (§9.1.15 item 3 does not admit output as a
                    // concurrent mode, so no other connector is numbering against this file at the time.)
                    if (SharedPhysical is { } outShared) outShared.ReleasedOrdinal = 0;
                    break;

                case FileOpenMode.Extend:
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    // EXTEND is checked for the SAME arithmetic reason as INPUT, and the stakes are higher: a
                    // program that APPENDS to a file whose existing layout disagrees writes a file interleaving
                    // two record layouts, which is permanent corruption rather than a wrong computation.
                    NoticeIfLayoutDisagrees();
                    // ⛔ THE WRITE-ORDINAL BASE IS MEASURED HERE, BEFORE THE WRITER HANDLE EXISTS (kb/Work
                    // PB713). §14.9.51.4 GR19 — "the added records follow the records present in the physical
                    // file when it was opened" — is what makes this point the right one, and an OPEN EXTEND
                    // writes nothing, so the count is the same number the completed OPEN would have seen. See
                    // ExistingRecordCount for why the ORDER, not merely the share mode, is the fix.
                    // What it seeds is the PHYSICAL FILE's mint, not a base of this connector's own (kb/Work
                    // PB739): a second connector extending the same file measures the same physical file and
                    // writes the same number, and from then on the two share one ascending sequence. A file
                    // that is not there holds no records, so the mint is 0.
                    if (SharedPhysical is { } extShared)
                        extShared.ReleasedOrdinal = exists ? ExistingRecordCount() : 0;
                    // ⛔ NOT FileMode.Append THROUGH OpenConnectorStream (kb/Work PB739). .NET's Append seeks
                    // to the end ONCE, at open; under §9.1.15 sharing two connectors then anchor at the same
                    // offset and the later flush lands on top of the earlier record.
                    // HostFile.OpenConnectorWriteStream is the role that knows the difference, and it reads the
                    // difference off the POSTURE — a handle whose file lock admits another writer gets the
                    // repositioning stream — so a clause-less pair the arbiter permits is covered by the same
                    // rule as a SHARING WITH ALL OTHER pair (kb/Work PB740).
                    _writer = OpenWriter(FileMode.Append, HostShare, fallbackShare: null);
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;

                case FileOpenMode.IO:
                    // I-O permits both READ and REWRITE on the one open connector (ISO §14.9.35 GR3 — REWRITE
                    // requires open mode I-O; its format-1 contract replaces the record retrieved by the last
                    // successful READ in place), so the underlying stream must open ReadWrite — Rewrite's
                    // seek-and-write path writes through the reader's BaseStream. An absent non-optional file
                    // is 35; an optional one is created (05).
                    // §14.9.27 GR17: an ABSENT OPTIONAL file opened I-O is CREATED (as if OPEN OUTPUT then CLOSE),
                    // then opened I-O like an existing file — so it always ends with a ReadWrite _reader and the FPI is
                    // effectively 1 (GR14). A first READ then finds no record → AtEnd '10' (§14.9.30 GR21 rule e + GR24),
                    // NOT '47' (which is only for a connector not open in input/I-O, §9.1.13.7 item 7). OPEN still
                    // returns '05'. No OptionalAbsent needed — the file now physically exists (empty).
                    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
                    // create empty, then close — an auxiliary handle, not the connector's stream
                    if (!exists) using (HostFile.OpenAuxiliary(HostPath, FileMode.Create, FileAccess.Write)) { }
                    // ReadWrite access and NO FileOptions.SequentialScan for this mode — both decided once, in
                    // OpenReader, which is also what the reposture rebuild uses.
                    _reader = OpenReader(HostShare, at: 0, fallbackShare: null);
                    NoticeIfLayoutDisagrees();
                    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
                    break;
            }
            // ISO §13.18.34 GR6 b) 1 reads the operand values "at the COMPLETION of an OPEN statement with the
            // OUTPUT phrase", so the page model is established by the registry's OPEN dispatch once this body has
            // succeeded (FileRegistry.SharedOpenAttempt → BeginLinagePage) — with the page the EXECUTING element's
            // LINAGE clause evaluates to, which is why it cannot be read from connector state here (PB673).
            return FileStatusCode.Success;
        }
    }

    /// <summary>Report a FIXED-LENGTH record-sequential file whose byte length is not a whole multiple of its
    /// record length (<see cref="RecordLayoutNotice"/>) — the arithmetic proof that the file's layout and the
    /// record description disagree, raised at OPEN rather than at the trailing '04' a program may never inspect.
    /// Called on every mode that consults an EXISTING file's bytes — INPUT, I-O and EXTEND. OUTPUT is excluded
    /// because it truncates: whatever the file held is discarded, so nothing can disagree.
    /// Excluded, because a differing physical length is NORMAL for them: LINE SEQUENTIAL (records are delimited,
    /// §9.1.13.2) and RECORD IS VARYING (each record carries its own length prefix, so a mismatch is detected
    /// per record — §14.9.30.4 GR14).</summary>
    private void NoticeIfLayoutDisagrees()
    {
        if (_lineSequential || IsVarying) return;
        try
        {
            // No existence test at all — not FileInfo.Exists (which swallows an access error into "no file"
            // exactly as File.Exists does, kb/Work PB323) and not HostFile.Probe (a second syscall for an
            // answer the very next one already gives). FileInfo.Length raises FileNotFoundException for an
            // absent file and UnauthorizedAccessException for a refused one, and BOTH catches below mean the
            // same thing here: no notice. A notice is never worth failing an OPEN over.
            RecordLayoutNotice.CheckFixedLengthFile(HostPath, new FileInfo(HostPath).Length, RecordWidth);
        }
        catch (IOException) { }              // absent, or unreadable — either way, nothing to report
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>The sequential CLOSE body (ISO §14.9.7). A WRITE … ADVANCING stream is terminated with a
    /// trailing newline (matching the legacy print-control behavior) — the not-open guard lives on
    /// <see cref="FileConnector.Close"/>.</summary>
    protected override string CloseCore()
    {
        try
        {
            if (_afterAdvancing) { _writer?.Write("\r\n"); _afterAdvancing = false; }
            _reader?.Dispose();
            _writer?.Dispose();   // a flush failure here maps to '30' on FileConnector.Close (§9.1.13.6 item 1)
        }
        finally
        {
            // Whatever the outcome, the streams never survive the CLOSE — a stale disposed _writer after a
            // reopen-for-INPUT would take the WRITE arms (kb/Work PB140). OptionalAbsent (the FPI's "not
            // present" state) DOES survive — §14.9.6.4 GR6; the next OPEN resets it.
            _reader = null;
            _writer = null;
        }
        ModeKnown = false;   // §9.1.4 — after a successful CLOSE the file is in no open mode
        return FileStatusCode.Success;
    }

    // ── The line sequential character set (ISO Annex A.1 item 115) ───────────────────────────────────────────

    /// <summary>⛔ THE ONE LINE-CHARACTER-SET TEST of a record area, shared by every direction so READ and WRITE
    /// can never disagree about what a line sequential character is (kb/Work PB329): §14.9.30.4 GR16 answers it
    /// with '09' on a successful READ, §14.9.51.4 GR23 and §14.9.35.4 GR17 d) with '71' on an unsuccessful
    /// WRITE / REWRITE. False for a record sequential file — all three rules are organization-scoped ("for a
    /// line sequential file"), and a record sequential record is plain bytes that must round-trip byte-exact.
    /// The set and its Annex A.1 item 115 derivation live in <see cref="LineSequentialCharacterSet"/>.
    /// <para>The subject is the RECORD AREA, which is what all three rules name — not the trimmed image a line
    /// sequential WRITE presents, and not the length-limited prefix a varying record transfers. Trailing area
    /// positions are spaces, which are IN the set, so the wider subject cannot manufacture a status; a character
    /// outside the set anywhere in the area is what the rules ask about.</para></summary>
    private bool RecordAreaOutsideLineCharacterSet(ReadOnlySpan<char> recordArea) =>
        _lineSequential && LineSequentialCharacterSet.HasCharacterOutside(recordArea, NationalRecordArea);

    // ── WRITE ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Plain <c>WRITE record</c> (ISO §14.9.46): line-sequential writes the trimmed image as a line;
    /// record-sequential writes a fixed-width block; a VARYING file writes a length-framed record of
    /// <paramref name="length"/> bytes (the DEPENDING item's content, §13.18.43 GR13a) or the image's own length
    /// (GR13b/c), failing with '44' outside the declared bounds (GR14). Valid only when open OUTPUT/EXTEND
    /// (else 48).</summary>
    public string Write(string image, int length, LinagePage? page)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        // On a PRINT file (this connector has seen print-control advancing) — or a LINAGE file, which is
        // line-oriented from its FIRST write (the FILES deep-dive rule; its plain WRITE acts as AFTER
        // ADVANCING 1, ISO §14.9.51 GR25, and the counter advances by one, §13.18.34 GR7c3) — an omitted
        // ADVANCING phrase still line-advances (a raw fixed-width block would weld onto the previous line). In
        // the pending-advance stream model (each AFTER-write leads with its newline; CLOSE supplies the final
        // one), the write-then-advance shape reproduces the print stream the golden corpus encodes.
        if ((_afterAdvancing || page is not null) && !_lineSequential) return WriteAdvancing(image, 1, before: true, page);
        // §14.9.51.4 GR23: "For a line sequential file, if the record area contains one or more characters that
        // are not in the implementor-defined character set defined for a line sequential file, the execution of
        // the WRITE statement is unsuccessful and the I-O status in the write file connector is set to '71'."
        // Ahead of every shape branch because the rule is organization-scoped, not shape-scoped, and ahead of
        // any stream traffic because §9.1.13.10 item 1 requires the record area (and the medium) to be left
        // unchanged. This arm did not exist before kb/Work PB329 — only REWRITE's GR17 d) twin did.
        if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;
        if (IsVarying)
        {
            int len = length >= 0 ? length : image.Length;
            if (len < VaryMin || len > VaryMax)
                return Status = FileStatusCode.RecordSizeViolation;   // '44' §13.18.43 GR14a
            if (_lineSequential) _writer.WriteLine(TrimRecordEnd(FitRecord(image, len)));
            else { RecordFraming.WritePrefix(_writer, len); _writer.Write(FitRecord(image, len)); }
        }
        else if (_lineSequential) _writer.WriteLine(TrimRecordEnd(image));
        else _writer.Write(Fit(image));
        ReleaseRecord();   // §14.9.51.4 GR12 — released to the operating environment, and numbered there
        _afterAdvancing = false;
        // GR7c3 (§13.18.34): a plain WRITE to a LINAGE file advances the counter by one. Only the
        // line-sequential/varying shapes reach here (the record-sequential LINAGE write rerouted above).
        if (page is { } pg) AdvanceLinageCounter(1, pg);
        return Status = FileStatusCode.Success;
    }

    /// <summary>Print-control <c>WRITE record [BEFORE] [AFTER] ADVANCING {n LINES | PAGE}</c> (ISO §14.9.51.4
    /// GR25): for
    /// AFTER, advance then write the trimmed image; for BEFORE, write then advance. <paramref name="lines"/> = -1
    /// means ADVANCING PAGE (a form feed). The leading/trailing newline structure matches the legacy print stream.</summary>
    public string WriteAdvancing(string image, int lines, bool before, LinagePage? page)
    {
        if (!IsOpen || _writer is null) return Status = FileStatusCode.WriteNotOpenForOutput;
        if (Mode is not (FileOpenMode.Output or FileOpenMode.Extend)) return Status = FileStatusCode.WriteNotOpenForOutput;
        // §14.9.51.4 GR23 again — the SECOND WRITE ARM. GR23 is a property of the FILE, so it binds every entry
        // point a WRITE statement can reach on a line sequential connector, not just the plain-record one; it is
        // tested on the raw record area, ahead of PrintSafe's print-stream mapping.
        if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;
        _afterAdvancing = true;
        string text = PrintSafe(TrimRecordEnd(image));
        if (before) { _writer.Write(text); Advance(lines); }
        else { Advance(lines); _writer.Write(text); }
        // §14.9.51.4 GR12 — "The successful execution of a WRITE statement releases a logical record to the
        // operating environment" — is an ALL FILES rule, so a print-control WRITE releases an ordinal-identified
        // record exactly as the plain one does, and GR11's WITH LOCK needs that identity. Released HERE and not
        // in Write(), which delegates to this method for a print/LINAGE file (kb/Work PB683, which added the
        // count; kb/Work PB739 made it a RELEASE — the flush and the shared mint — in every write arm:
        // three of them then, TWO since kb/Work PB712 deleted `WriteBeforeAndAfter`).
        ReleaseRecord();
        // The LINAGE counter advances as part of the write, AFTER the physical presentation (the legacy
        // ordering): an AT END-OF-PAGE branch then reads the POST-advance counter of the triggering write
        // (§13.18.34 GR7c; SQ201M's footing lines print line 45).
        if (page is { } pg) AdvanceLinageCounter(lines, pg);
        return Status = FileStatusCode.Success;
    }

    /// <summary>The PRINT-stream character mapping: a character above the 7-bit range writes as <c>?</c> — the
    /// implementor-defined runtime print encoding the NIST golden corpus encodes (the legacy print writer's
    /// ASCII fallback: HIGH-VALUE prints as <c>?</c>, NUL passes through — NC107A's figurative-constant
    /// information lines). Applies ONLY to print-control writes; a record (data-file) WRITE keeps its raw
    /// characters — a record image must round-trip through READ byte-exact.</summary>
    private static string PrintSafe(string s)
    {
        if (!s.Any(c => c > '\x7f')) return s;
        var a = s.ToCharArray();
        for (int i = 0; i < a.Length; i++) if (a[i] > '\x7f') a[i] = '?';
        return new string(a);
    }

    // ⛔ `WriteBeforeAndAfter` LIVED HERE AND IS GONE (kb/Work PB712). It was the THIRD write arm, taking a
    // BEFORE amount AND an AFTER amount and advancing — and counting LINAGE — twice, because the grammar spelled
    // the ADVANCING phrase twice. §14.9.51.2 Format 1 prints ONE `ADVANCING` operand (the choice indicators
    // enclose only the words BEFORE and AFTER) and §14.9.51.4 GR25 a) advances the page "the number of lines
    // equal to that value" — one advance, whose PLACEMENT GR25 e)/f) decide. The combined COBOL-2023 form is
    // therefore `WriteAdvancing(…, before: true, …)`, and the end-of-page condition the second
    // `AdvanceLinageCounter` call used to erase (PB686's observation) cannot arise: there is one call.
    private void Advance(int lines)
    {
        if (lines < 0) { _writer!.Write('\f'); return; }   // ADVANCING PAGE
        for (int i = 0; i < lines; i++) _writer!.Write("\r\n");
    }

    // ── READ ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ … {NEXT | PREVIOUS}</c> (ISO §14.9.30 Format 1). Returns the record image
    /// (padded to the record width) in <paramref name="image"/> and <see langword="true"/> on success;
    /// <see langword="false"/> at end-of-file / beginning-of-file (AT END, status 10) or any unsuccessful read.
    /// Sets the status.
    /// <para><paramref name="previous"/> is §14.9.30.4 GR19's read kind, taken from the statement's PREVIOUS
    /// phrase. It selects the record NUMBER (GR21 rules b/c), and nothing else about the read changes — which is
    /// why the reposition below feeds the SAME physical read body rather than a second reader.</para></summary>
    public bool Read(bool previous, out string image)
    {
        image = new string(' ', RecordWidth);
        if (SequentialReadGuard() is { } guard) { Status = guard; return false; }   // '47'/'46'/'10' — FileConnector
        if (_reader is null) { Status = FileStatusCode.ReadNotOpenForInput; return false; }

        // §14.9.30.4 GR21, "When the file is a sequential file": the direction selects the record NUMBER, so a
        // backward read is a REPOSITION to that ordinal followed by the ordinary physical read. Rule e) — "If no
        // record is found that satisfies the above rules, the at end condition exists" — is the target-below-1
        // arm; §14.9.30.4 GR24 then sets '10' and the AT END imperative runs, exactly as it does at EOF.
        if (previous)
        {
            long target = TargetReadOrdinal(true);
            if (target < 1) { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }   // GR21 e
            // §14.9.30.4 GR20 — "If the PREVIOUS phrase is specified, the physical file associated with the file
            // connector referenced by file-name-1 shall be a single reel/unit mass storage file." A stream that
            // cannot be positioned is not one; report the permanent error rather than silently reading something
            // else (the same posture the non-seekable REWRITE arm below takes).
            if (!SeekToOrdinal(target)) { Status = FileStatusCode.PermanentError; return false; }
        }

        // §14.9.30 GR14 (READ) / §9.1.13.2 item 3: a RECORD-sequential physical record whose length is outside the file's
        // min/max record size is a SUCCESSFUL read with status '04' (the record is still delivered). Line-sequential
        // is excluded — its short/long conditions are '06'/'09', never '04'.
        bool shortLong = false;
        bool lineTooLong = false;
        bool lineBadChar = false;
        if (_lineSequential)
        {
            // §14.9.30 GR15: an over-length line-sequential record is truncated on the right to the record width, the
            // READ is SUCCESSFUL with I-O status '06', and the file position indicator references the next unread
            // character IN THE RECORD (NOTE 3) — a subsequent READ continues the remainder up to the line delimiter.
            // _lineRemainder models that FPI: service a pending remainder before reading a new physical line.
            string? line;
            if (_lineRemainder is not null)
            {
                line = _lineRemainder;
                _lineRemainder = null;
                _lastReadLinePartial = true;   // a served remainder is only PART of its physical line (§14.9.35 GR17a)
            }
            else if (NextFrame(out _) is { } physical) line = physical;
            else { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
            if (line.Length > RecordWidth)
            {
                _lineRemainder = line[RecordWidth..];
                image = line[..RecordWidth];
                LastReadLength = RecordWidth;
                lineTooLong = true;
            }
            else { LastReadLength = line.Length; image = Fit(line); }   // §14.9.30.4 GR15 fill — national-aware (kb/Work PB327)
            // §14.9.30.4 GR16: "If the execution of the READ statement is successful but the record area
            // contains one or more characters not in the implementor-defined character set for a line
            // sequential file, the I-O status in the read file connector is set to '09'" (§9.1.13.2 item 7).
            // ⛔ Tested on the DELIVERED record area, which is what GR16 names — so a character in the unread
            // remainder of an over-length line belongs to the READ that DELIVERS it, not to this one. GR16 is
            // stated after GR15 and asks only that the read be successful, so it also lands on a truncated
            // ('06') read; the status arbitration below follows that order (kb/Work PB329).
            lineBadChar = RecordAreaOutsideLineCharacterSet(image);
        }
        else if (NextFrame(out _) is { } data)
        {
            LastReadLength = data.Length;
            image = Fit(data);   // §14.9.30.4 GR14/GR15 fill — national-aware (kb/Work PB327)
            // A VARYING record outside the file's min/max is §14.9.30 GR14's '04'. Fixed-length record
            // sequential: min == max == RecordWidth, so a partial (short) final record is '04' too; a
            // longer-than-max record cannot occur (the frame is read in RecordWidth chunks).
            shortLong = IsVarying ? data.Length < VaryMin || data.Length > VaryMax : data.Length < RecordWidth;
        }
        else { LastReadUnsuccessful = true; Status = FileStatusCode.AtEnd; return false; }
        _readOrdinal++;   // the record just made available is ordinal N+1 (§9.1.16 lock identity)
        ReadSucceeded(lineBadChar ? FileStatusCode.LineRecordInvalidCharRead   // '09' §14.9.30.4 GR16 — stated after GR15, so it wins over '06'
            : lineTooLong ? FileStatusCode.LineRecordTooLong
            : shortLong ? FileStatusCode.RecordLengthShortLong : FileStatusCode.Success);
        return true;
    }

    /// <summary>Position the reader at the first character of record <paramref name="ordinal"/> (1-based) so the
    /// ordinary read body delivers it — the ONE reposition, used by the §14.9.30.4 GR21 backward read.
    /// <para>Returns false when the physical file cannot support it, which §14.9.30.4 GR20 makes the program's
    /// error rather than a behaviour: a stream that cannot seek, or a LINE SEQUENTIAL file — for which
    /// §14.9.30.3 SR7 forbids the PREVIOUS phrase outright, so the binder has already reported COBOLNET1720 and
    /// this arm is the defensive floor under <c>--permissive</c>.</para>
    /// <para>The offset is ARITHMETIC on a fixed-width record-sequential file and comes from the frame index on
    /// a RECORD VARYING one. ⛔ This member answers only WHERE the record starts; the reposition itself is
    /// <see cref="SeekToRecord"/>, the ONE primitive that also resets every derived read fact (kb/Work PB352) —
    /// a second seek-and-discard here would be the same rule written twice.</para></summary>
    private bool SeekToOrdinal(long ordinal)
    {
        if (_lineSequential || ordinal < 1 || _reader is not { BaseStream: { CanSeek: true } stream }) return false;
        long start;
        if (IsVarying)
        {
            _varyingStarts ??= RecordFraming.FrameStarts(stream);
            // A target past the last frame is NOT a positioning failure — it is §14.9.30.4 GR21 e), "no record
            // is found that satisfies the above rules", and the read body reports it as the ordinary at-end
            // once positioned at the end of data. (Reachable: a PREVIOUS on a freshly opened EMPTY varying
            // file targets ordinal 1 by rule b and there is no ordinal 1.) The fixed-width branch below gets
            // this for free, because seeking past the end yields a zero-character read.
            start = ordinal <= _varyingStarts.Count ? _varyingStarts[(int)(ordinal - 1)] : stream.Length;
        }
        else
        {
            if (RecordWidth <= 0) return false;
            start = (ordinal - 1) * RecordWidth;
        }
        SeekToRecord(start, ordinal - 1);   // the shared read body's post-increment then lands on `ordinal`
        return true;
    }

    /// <summary>⛔ THE ONE PHYSICAL FRAMING WALK for the three on-disk shapes a sequential file has — a
    /// newline-delimited line, a 4-byte-length-prefixed VARYING frame, and a fixed-width block. Advances the
    /// reader by exactly ONE physical record, returns its RAW data (neither padded nor truncated) and reports
    /// the record's byte anchor in <paramref name="frameStart"/>; <see langword="null"/> at end-of-data.
    /// <para>It carries no I-O status, no record-area fill and no <see cref="_lineRemainder"/> chunking: those
    /// are §14.9.30's rules and belong to <see cref="Read"/>. Extracted so START's record scan
    /// (<see cref="StartFirstLast"/>) walks the file with the SAME framing the reader uses instead of a second
    /// copy of it (kb/Work PB352) — the byte anchors it needs are exactly the ones REWRITE already
    /// tracks.</para>
    /// <para>⛔ AND IT IS THEREFORE THE ONE PLACE THE READ-AHEAD IS CHECKED AGAINST THE MEDIUM. Being the only
    /// walk is what makes <see cref="EnsureReaderCoherent"/> a property of this connector rather than a
    /// courtesy at a call site: every character this connector ever reads is read below this line (kb/Work
    /// PB753; <c>SharedReadCoherenceDriftTests</c> proves the "only" rather than asserting it).</para></summary>
    private string? NextFrame(out long frameStart)
    {
        EnsureReaderCoherent();   // kb/Work PB753 — a sibling's release since the buffer was filled
        if (_lineSequential)
        {
            frameStart = _lineByteOffset;
            _lastLineStart = _lineByteOffset;                     // byte anchor of the physical line (§14.9.35 GR17 REWRITE)
            string? line = ReadPhysicalLine(out int delimBytes);
            if (line is null) return null;
            _lineByteOffset += line.Length + delimBytes;          // Latin1: chars == bytes (past data + delimiter)
            _lastLineBytes = Math.Min(line.Length, RecordWidth);  // data length of the record being replaced
            _lastReadLinePartial = line.Length > RecordWidth;     // an over-length read transfers only part (GR17a)
            return line;
        }
        // The block start is the LOGICAL offset (characters consumed so far — 1:1 with bytes under Latin1),
        // never BaseStream.Position: the StreamReader buffers ahead, so the base position is the buffer-fill
        // boundary, not the read position.
        frameStart = _readOffset;
        if (IsVarying)
        {
            // Length-framed record: 4-byte LE prefix + payload (see the framing note at the field declarations).
            var pre = new char[4];
            if (FillChars(pre, 4) < 4) return null;
            int len = RecordFraming.PrefixLength(pre);
            var vbuf = new char[len];
            int vn = FillChars(vbuf, len);
            _lastReadBlockStart = _reader!.BaseStream.CanSeek ? _readOffset + 4 : -1;
            _readOffset += 4 + vn;
            return new string(vbuf, 0, vn);
        }
        var buf = new char[RecordWidth];
        int n = FillChars(buf, RecordWidth);
        if (n == 0) return null;
        _lastReadBlockStart = _reader!.BaseStream.CanSeek ? _readOffset : -1;
        _readOffset += n;
        return new string(buf, 0, n);
    }

    /// <summary>Fill exactly <paramref name="count"/> characters (or to end-of-stream): StreamReader.Read may
    /// return fewer than requested at an internal buffer boundary, which is not end-of-data.</summary>
    private int FillChars(char[] buf, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = _reader!.Read(buf, total, count - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    /// <summary>Read one physical line's DATA (up to and consuming a CR / LF / CRLF terminator), matching
    /// <see cref="StreamReader.ReadLine"/> semantics, and report the terminator's byte width (0 at a final line with
    /// no terminator) so the caller can track the physical byte offset for a line-sequential REWRITE (§14.9.35.4
    /// GR17). Returns null only at end-of-file with no characters read. Char-by-char rather than ReadLine because
    /// ReadLine hides the terminator width, which the byte anchor needs (under Latin1 one char == one byte).</summary>
    private string? ReadPhysicalLine(out int delimBytes)
    {
        delimBytes = 0;
        int ci = _reader!.Read();
        if (ci < 0) return null;                                    // EOF, no data
        var sb = new StringBuilder();
        while (true)
        {
            char c = (char)ci;
            if (c == '\n') { delimBytes = 1; break; }
            if (c == '\r')
            {
                if (_reader.Peek() == '\n') { _reader.Read(); delimBytes = 2; } else delimBytes = 1;
                break;
            }
            sb.Append(c);
            ci = _reader.Read();
            if (ci < 0) break;                                      // final line with no terminator
        }
        return sb.ToString();
    }

    // ── REWRITE ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>REWRITE record</c> (ISO §14.9.35): replace the last-read record in place. Valid only
    /// when open I-O (else 49) and the immediately-previous op was a successful READ (else 43). On a varying file
    /// the rewritten record's length (<paramref name="length"/> = the DEPENDING item's content, GR13a, or the
    /// image's own length, GR13b/c) must lie within the declared bounds (GR20 → '44') AND — record sequential —
    /// equal the size of the record being replaced (GR16 → '44'; the in-place frame cannot change size).</summary>
    public string Rewrite(string image, int length = -1)
    {
        if (!IsOpen || Mode != FileOpenMode.IO) return Status = FileStatusCode.DeleteRewriteNotOpenForIO;
        if (!PrevOpWasSuccessfulRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
        int len = IsVarying ? (length >= 0 ? length : image.Length) : RecordWidth;
        if (IsVarying && (len < VaryMin || len > VaryMax))
            return Status = FileStatusCode.RecordSizeViolation;       // '44' §14.9.35 GR20
        if (IsVarying && len != LastReadLength)
            return Status = FileStatusCode.RecordSizeViolation;       // '44' §14.9.35 GR16 (record sequential)
        if (!_lineSequential && _lastReadBlockStart >= 0 && _reader is { BaseStream: { CanSeek: true, CanWrite: true } stream })
            return OverwriteInPlace(stream, _lastReadBlockStart, FitRecord(image, len));
        if (_lineSequential && _lastLineStart >= 0 && _reader is { BaseStream: { CanSeek: true, CanWrite: true } lstream })
        {
            // §14.9.35.4 GR17 (line-sequential REWRITE): (a) a preceding partial ('06') read ⇒ '44'; (d) a record
            // area holding a character outside the line sequential character set ⇒ '71'; (b) a record LONGER than
            // the one being replaced ⇒ '44'; (c) otherwise space-pad the trimmed record to the replaced length and
            // overwrite in place ⇒ '00'. Padding to _lastLineBytes keeps the physical byte span (and the delimiter
            // position) invariant, so the overwrite is exact; the trimmed length matches the line-sequential WRITE model.
            if (_lastReadLinePartial) return Status = FileStatusCode.RecordSizeViolation;                // '44' GR17a / §9.1.13.7 item 4d
            // GR17 d) now routes through the SHARED set instead of the private CR/LF test it carried before
            // kb/Work PB329 — the delimiters are only the two members the framing forces out, and a REWRITE and a
            // WRITE of the identical record area must reach the identical verdict (Annex A.1 item 115).
            if (RecordAreaOutsideLineCharacterSet(image)) return Status = FileStatusCode.LineRecordInvalidChar;   // '71' GR17d
            string content = TrimRecordEnd(Fit(image));
            if (content.Length > _lastLineBytes) return Status = FileStatusCode.RecordSizeViolation;     // '44' GR17b
            content = FitRecord(content, _lastLineBytes);                                                // '00' GR17c (span-invariant)
            return OverwriteInPlace(lstream, _lastLineStart, content);
        }
        // A non-seekable line-sequential / record-sequential REWRITE cannot overwrite in place → report a permanent
        // error so the program's FILE STATUS / declarative path observes it (never a silent no-op).
        return Status = FileStatusCode.PermanentError;
    }

    /// <summary>⛔ THE ONE IN-PLACE OVERWRITE — the record-sequential and the line-sequential REWRITE arms
    /// differ in the rules that VALIDATE the replacement (§14.9.35.4 GR16/GR20 versus GR17 a)–d)) and not at
    /// all in what they then do to the physical file, so the release itself is written once (kb/Work PB753;
    /// it used to be two copies and only one of them could have been given the announcement below).
    /// <para>The write goes through the I-O reader's own base stream (§14.9.35 GR3 — a REWRITE replaces the
    /// record the last READ retrieved, through the one connector) and is FLUSHED, because §14.9.35.4 GR4 says
    /// <i>"The successful execution of the REWRITE statement releases a logical record to the operating
    /// environment"</i>: the '00' this returns is a promise that the record is IN the physical file, not that
    /// it is queued. <see cref="NoteRelease"/> then tells the physical file's other connectors, whose
    /// read-ahead would otherwise keep serving the image this call just superseded.</para>
    /// <para>The base stream is left where it was found. It is the buffer-fill boundary of the
    /// <see cref="StreamReader"/> above it, not the file position indicator, so restoring it is what keeps
    /// THIS connector's own buffered characters valid across its own REWRITE.</para></summary>
    private string OverwriteInPlace(Stream stream, long anchor, string content)
    {
        long resume = stream.Position;
        stream.Seek(anchor, SeekOrigin.Begin);
        byte[] bytes = Encoding.Latin1.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();                 // §14.9.35.4 GR4 — released to the operating environment
        stream.Seek(resume, SeekOrigin.Begin);
        NoteRelease();                  // …and announced, so no sibling reader serves the superseded image
        return Status = FileStatusCode.Success;
    }

    // ── START (ISO §14.9.41, SEQUENTIAL FILES — General rules 1–7 and 20/21) ─────────────────────────────────

    /// <summary>START FIRST / LAST on a sequential-organization file. §14.9.41.3 SR2 makes these the ONLY two
    /// forms the statement has on this organization, and §14.9.41.4 GR20/GR21 state their outcome:
    /// <list type="bullet">
    /// <item>GR20 — <i>"If FIRST is specified, the file position indicator is set to 1 if records exist in the
    /// physical file. If no records exist in the file, or the physical file does not support the ability to
    /// position at the first record, the I-O status value … is set to '23', the invalid key condition exists,
    /// and the execution of the START statement is unsuccessful."</i></item>
    /// <item>GR21 — the LAST twin, positioning at <i>"the record number of the last existing logical
    /// record"</i>.</item>
    /// </list>
    /// GR1's open mode is §9.1.13.7 item 7's '47' (<i>"a READ or START statement … not open in the input or I-O
    /// mode"</i>); GR5's absent OPTIONAL file and both GR20/GR21 failure arms are §9.1.13.5 item 3's '23', whose
    /// clause d) — <i>"a START statement is attempted on a sequential file that has no records or that does not
    /// support the ability to position at the specified record"</i> — is written for exactly this connector.
    /// <para>⛔ AN IMPLEMENTATION THAT CANNOT SEEK STILL OWES AN ANSWER, NEVER AN ABORT: that is the shape of
    /// GR20/GR21's second arm, and it is why this member exists at all (kb/Work PB352 — the statement used to
    /// bind to a loud runtime stage that killed the run unit). Positioning is INCLUSIVE, matching the keyed
    /// connectors: the next sequential READ delivers the record START selected.</para></summary>
    public string StartFirstLast(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                        // GR5 / §9.1.13.5 item 3 b)
        // "or the physical file does not support the ability to position at the first/last record" — a stream
        // that cannot be repositioned. A FileStream always can; a future non-seekable medium answers '23' here
        // rather than mis-positioning.
        if (_reader is not { BaseStream.CanSeek: true }) return StartFail();

        // The scan is the ONE framing walk (NextFrame), so the record boundaries START positions on are exactly
        // the ones READ would deliver — including a short final block ('04') and a line-sequential file's
        // physical lines. FIRST stops at the first frame; LAST walks to end-of-data remembering the last one.
        SeekToRecord(0, 0);
        long foundStart = -1, foundOrdinal = 0, seen = 0;
        while (NextFrame(out long frameStart) is not null)
        {
            seen++;
            foundStart = frameStart;
            foundOrdinal = seen;
            if (!last) break;
        }
        if (foundStart < 0) { SeekToRecord(0, 0); return StartFail(); }   // no records exist — GR20/GR21
        SeekToRecord(foundStart, foundOrdinal - 1);
        LastReadUnsuccessful = false;   // a successful START is a reposition: §14.9.30.4 GR21's '46' poison clears
        return Status = FileStatusCode.Success;
    }

    /// <summary>An unsuccessful START: §14.9.41.4 GR7 — <i>"the file position indicator is set to indicate that
    /// no valid record position has been established"</i>, which on this connector is precisely the state
    /// §9.1.13.7 item 6 a) reads back as '46' on the next sequential READ — and the '23' GR20/GR21 name.</summary>
    private string StartFail()
    {
        InvalidateFilePosition();
        return Status = FileStatusCode.RecordNotFound;
    }

    /// <summary>Reposition the reader at <paramref name="byteOffset"/>, which shall be the START of a record,
    /// and reset every piece of derived read state so the next <see cref="Read"/> behaves as if the file had
    /// been read up to that point: the logical offsets (both framings), the §14.9.30 GR15 unread remainder, the
    /// REWRITE byte anchors (a START is not a READ, so §14.9.35's "record being replaced" no longer exists),
    /// the §9.1.16 record ordinal, which becomes <paramref name="ordinalBefore"/> so the next record read is
    /// numbered correctly, and the physical file's release generation the emptied buffer now agrees with
    /// (kb/Work PB753). <see cref="StreamReader.DiscardBufferedData"/> is required: the reader buffers ahead
    /// of the base stream, so seeking the stream alone would keep serving stale characters.</summary>
    private void SeekToRecord(long byteOffset, long ordinalBefore)
    {
        _reader!.BaseStream.Seek(byteOffset, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        _coherentAt = Physical?.ReleaseGeneration ?? 0;
        _readOffset = byteOffset;
        _lineByteOffset = byteOffset;
        _lineRemainder = null;
        _lastReadBlockStart = -1;
        _lastLineStart = -1;
        _lastLineBytes = 0;
        _lastReadLinePartial = false;
        _readOrdinal = ordinalBefore;
    }

    /// <summary>The AT END condition (ISO §14.9.30): true only at end-of-file (status 10).</summary>
    public bool AtEnd => Status == FileStatusCode.AtEnd;
}
