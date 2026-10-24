// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ A SEQUENTIAL FILE UNDER APPLY COMMIT (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2; kb/Work PB3340). §9.1.18.2:
/// <i>"Apart from sort-merge files and report writer files, any type of file on a mass storage device may be specified"</i> —
/// record sequential, line sequential and the print stream alike (2.2.2.10) — so a covered connector's changes are its run
/// unit's transaction on the physical file (<see cref="SequentialJournal"/>, owned by the registry's
/// <see cref="SequentialTransactions"/>), which COMMIT makes permanent and ROLLBACK, item 8 and an abnormal termination undo.
/// <para><b>The kept handle (2.2.2.4, 2.2.2.9).</b> A covered connector takes ONE read-write host handle at its first OPEN after
/// the last COMMIT or ROLLBACK and KEEPS it — across its CLOSE (§14.9.6.4 GR9: <i>"Except when the file is specified in an APPLY
/// COMMIT clause, the file lock and any record locks associated with the file connector referenced by file-name-1 are released
/// by the execution of the CLOSE statement"</i>) and across a re-OPEN, whose reader or writer is rebuilt over it — until the
/// COMMIT (§14.9.7.4 GR4) or ROLLBACK (§9.1.15) releases it (<see cref="ReleaseKept"/>); a DELETE FILE closes it before the file
/// is removed, and a creation opens it. The capture reads F through it and the restore writes F through it.</para>
/// <para><b>The changes (2.2.2.2).</b> A WRITE appends (<see cref="JournalAppend"/>, R3), a REWRITE overwrites in place
/// (<see cref="JournalImage"/>, R2), an OPEN OUTPUT of an existing file truncates and a DELETE FILE removes it (both CAPTURE,
/// R4: <see cref="BeginCoveredOpen"/>, <c>FileRegistry.DeleteFile</c>), and a creation begins a transaction with no file at its
/// start (START <c>A</c>).</para>
/// </summary>
public sealed partial class SequentialConnector : IJournalData
{
    /// <summary>The run unit's sequential transactions and host medium (<see cref="FileRegistry"/>'s, given at registration).</summary>
    internal SequentialTransactions Transactions { get; init; } = SequentialTransactions.Standalone();

    /// <summary>The run unit's host medium (<see cref="IHostMedium"/>): every handle this connector opens is the medium's.</summary>
    private IHostMedium Medium => Transactions.Medium;

    /// <summary>⛔ Whether an active APPLY COMMIT clause names this connector (§12.4.6.3.4; set by
    /// <c>FileRegistry.RegisterCommitControl</c>, cleared by <c>ReleaseCommitControl</c>).</summary>
    internal bool CommitControlled { get; set; }

    /// <summary>The run unit's open transaction on this connector's physical file, while it is under commit control.</summary>
    private SequentialJournal? Journal => CommitControlled ? Transactions.Of(HostPath) : null;

    /// <summary>The covered connector's host handle: the open connector's, or the one its CLOSE kept (<see cref="_kept"/>).</summary>
    private FileStream? _held;

    /// <summary>The share mode <see cref="_held"/> was taken with.</summary>
    private FileShare _heldShare;

    /// <summary>Whether the connector is CLOSED with what a CLOSE under commit control keeps: <see cref="_held"/> (unless given
    /// back meanwhile), its endpoint and <see cref="_keptFileLock"/>.</summary>
    private bool _kept;

    /// <summary>Whether the OPEN in progress adopted the kept handle (so an unsuccessful OPEN gives it back to the kept state).</summary>
    private bool _adoptedKept;

    /// <summary>The Table 19 run-unit file lock a CLOSE under commit control kept.</summary>
    private RunUnitFileLock? _keptFileLock;

    /// <summary>The entry mutex a covered OPEN holds from its entry step to its end (<see cref="EnterPhysicalFile"/>).</summary>
    private RegionLocks? _entryMutex;

    /// <summary>A ROLLBACK REMOVED the file under this open connector (START <c>A</c>, 2.2.2.7): its next WRITE creates it
    /// again, beginning a new transaction; a READ reads AT END and a REWRITE answers '43'.</summary>
    private bool _fileRemoved;

    /// <inheritdoc/>
    /// <remarks>The connector is under commit control and has a handle to keep (an absent OPTIONAL INPUT file has none).</remarks>
    internal override bool CloseKeepsLocks => CommitControlled && _held is not null;

    /// <inheritdoc/>
    private protected override bool HoldsKeptHandle => _kept && _held is not null;

    /// <inheritdoc/>
    private protected override bool KeptHandleExcludesEveryOpen => HoldsKeptHandle && !FileLockPosture.AdmitsAnotherRunUnit(_heldShare);

    /// <inheritdoc/>
    private protected override RunUnitFileLock? KeptFileLock => _kept ? _keptFileLock : null;

    /// <inheritdoc/>
    private protected override void TakeKeptFileLockOver() => _keptFileLock = null;

    /// <inheritdoc/>
    private protected override bool TryRestoreKeptFileLock(RunUnitFileLock held)
    {
        if (!_kept || !held.Revert()) return false;
        _keptFileLock = held;
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>A covered connector's one handle serves every mode of the transaction, so it is opened READ-WRITE whatever the
    /// mode (2.2.2.4), an INPUT OPEN's included.</remarks>
    private protected override bool WillTakeWritableHandle(FileOpenMode mode) => CommitControlled && (_held?.CanWrite ?? true);

    /// <inheritdoc/>
    /// <remarks>The kept handle's own share mode refuses the probe, so it is given back for the instant of the question and
    /// taken again when another handle does hold the file (the keyed connector's rule, <c>KeyedConnector.HeldByAnotherHandle</c>).</remarks>
    private protected override bool HeldByAnotherHandle()
    {
        if (!HoldsKeptHandle) return ProbeHeldByAnother();
        var (share, access) = (_heldShare, _held!.CanWrite ? FileAccess.ReadWrite : FileAccess.Read);
        GiveBackHeld();
        if (!ProbeHeldByAnother()) return false;
        try { TakeHeld(FileMode.Open, access, share); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }   // the window: the kept state stays without its handle
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>A kept handle on a physical file the path no longer names (removed or renamed away outside the run unit) is
    /// given back: the OPEN is an ordinary OPEN of the file at the path (kb/Work PB3533's rule, the keyed connector's).</remarks>
    private protected override void EndWhatWasKeptOfAnotherFile()
    {
        if (HoldsKeptHandle && HostFile.IdentityOf(_held!.SafeFileHandle) is { } kept && HostFile.IdentityAt(HostPath) is { } now && kept != now)
            ReleaseKept();
    }

    /// <summary>A DELETE FILE's GR15 question under commit control (<c>FileRegistry.DeleteCoveredSequential</c>): is the file
    /// held by another handle than this connector's own kept one (<see cref="HeldByAnotherHandle"/>)?</summary>
    internal bool HeldByAnotherAtDelete() => HeldByAnotherHandle();

    /// <summary>A DELETE FILE under commit control closes the kept handle before the file is removed (2.2.2.4: a Windows handle
    /// not opened for deletion blocks the removal, and after it there is no file to lock); what else the CLOSE kept — the file
    /// lock — stays until the COMMIT or ROLLBACK, and the transaction's (ALL) holding refuses every other opener.</summary>
    internal void GiveBackForRemoval() => GiveBackHeld();

    // ── The entry step (2.2.2.8) ───────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>⛔ AT EVERY WRITABLE OPEN of a sequential file — covered or not, any posture — the lock directory is listed for
    /// transaction journals: a live transaction's holdings refuse the OPEN ('61' to an OPEN OUTPUT while any lives, to any
    /// writable OPEN while a replaced one lives) and a dead one is settled (rolled back, or retired) before the presence probe.
    /// A covered OPEN takes the entry mutex for it and keeps it through its host open, creation or capture
    /// (<see cref="LeaveEntry"/> gives it back); an uncovered one pays one listing and takes the mutex only to settle. An OPEN
    /// INPUT runs no step (it has no writable handle to restore F with, and reads F as it stands).</remarks>
    private protected override string? EnterPhysicalFile(FileOpenMode mode)
    {
        if (mode is FileOpenMode.Input || HostPath.Length == 0) return null;
        if (CommitControlled) _entryMutex = Transactions.HoldEntryMutex(HostPath);
        return SequentialJournal.Enter(Medium, HostPath, Journal?.Id, _entryMutex).RefusesOpen(exclusive: mode is FileOpenMode.Output);
    }

    /// <inheritdoc/>
    private protected override void LeaveEntry()
    {
        _entryMutex?.Dispose();
        _entryMutex = null;
        _adoptedKept = false;
    }

    /// <summary>⛔ The covered OPEN's journal step (2.2.2.2), under the entry mutex: an OPEN OUTPUT of an EXISTING file begins the
    /// transaction if none stands (START <c>P</c>) and CAPTURES the file while the transaction is in its base phase (R4) — the
    /// truncation is the writer's, after it (<see cref="HeldWriteStream"/>); an OPEN that CREATES the file (OUTPUT of an absent
    /// file, EXTEND or I-O of an absent OPTIONAL one) begins the transaction with no file at its start (START <c>A</c>). Throws
    /// on a refusal: the OPEN answers '30' with the file unaffected.</summary>
    private void BeginCoveredOpen(FileOpenMode mode, bool exists)
    {
        _fileRemoved = false;
        var journal = Journal;
        if (exists)
        {
            if (mode is not FileOpenMode.Output) return;
            journal ??= Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
            journal.Capture(SequentialJournal.CaptureVerb.OpenOutput, HeldStream(FileMode.Open));
        }
        else if (mode is FileOpenMode.Output || IsOptional && mode is FileOpenMode.Extend or FileOpenMode.IO)
            journal ??= Transactions.Begin(HostPath, SequentialJournal.StartState.Absent);
    }

    // ── The kept handle ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The covered OPEN's host handle (2.2.2.4): the handle its CLOSE kept, adopted when it was taken with this OPEN's
    /// share mode; otherwise a new READ-WRITE handle (READ alone for an INPUT OPEN the host refuses the write to — a later OPEN
    /// OUTPUT through it is then GR3's '37'), opened in <paramref name="mode"/>, which creates the file for a creation.</summary>
    private FileStream HeldStream(FileMode mode)
    {
        if (_held is { } held)
        {
            if (!_kept) return held;   // this OPEN's own already (the capture took it)
            if (_heldShare == HostShare)
            {
                (_kept, _adoptedKept) = (false, true);
                return held;
            }
            GiveBackHeld();            // a share mode the kept handle was not taken with: the OPEN takes its own
            _kept = false;
        }
        try { return TakeHeld(mode, FileAccess.ReadWrite, HostShare); }
        catch (UnauthorizedAccessException) when (Mode is FileOpenMode.Input)
        {
            return TakeHeld(FileMode.Open, FileAccess.Read, HostShare);
        }
    }

    /// <summary>Open the covered connector's handle on the medium.</summary>
    private FileStream TakeHeld(FileMode mode, FileAccess access, FileShare share)
    {
        _held = Medium.OpenConnectorStream(HostPath, mode, access, share, FileOptions.None);
        _heldShare = share;
        return _held;
    }

    /// <summary><paramref name="stream"/>, positioned at its start: a reader is rebuilt over a kept handle from the file's start.</summary>
    private static FileStream AtStart(FileStream stream)
    {
        stream.Position = 0;
        return stream;
    }

    /// <summary>The covered writer's stream over the kept handle (2.2.2.4): an OUTPUT re-open TRUNCATES it (after the capture,
    /// <see cref="BeginCoveredOpen"/>), an EXTEND re-open writes at its end, and a posture that admits another writer gets the
    /// repositioning stream (<see cref="HostFile.WriteStreamOf"/>, the one decision).</summary>
    private Stream HeldWriteStream(FileMode mode)
    {
        var held = HeldStream(FileMode.OpenOrCreate);
        if (mode == FileMode.Create)
        {
            held.SetLength(0);
            held.Position = 0;
        }
        return HostFile.WriteStreamOf(mode == FileMode.Create ? FileMode.Open : mode, held.CanRead ? FileAccess.ReadWrite : FileAccess.Write,
            _heldShare, (_, _) => held);
    }

    /// <summary>Count through the covered connector's own handle (which refuses another handle under a share mode that admits no
    /// other connector), its position put back; otherwise through a bookkeeping handle of the medium.</summary>
    private T WithCountingStream<T>(Func<Stream, T> count)
    {
        if (CommitControlled && _held is { CanRead: true } held)
        {
            long position = held.Position;
            try { return count(held); }
            finally { held.Position = position; }
        }
        using var fs = Medium.OpenAuxiliary(HostPath, FileMode.Open, FileAccess.Read);
        return count(fs);
    }

    /// <summary>The CLOSE keeps the handle, its endpoint and the run-unit file lock (§14.9.6.4 GR9, §14.9.7.4 GR4).</summary>
    private void KeepHeld()
    {
        _kept = true;
        _keptFileLock = KeepRunUnitFileLock();
    }

    /// <summary>The unsuccessful OPEN's handle: back to the kept state when this OPEN adopted it, otherwise given back.</summary>
    private void AbandonHeld()
    {
        if (_adoptedKept)
        {
            (_kept, _adoptedKept) = (true, false);
            return;
        }
        if (!_kept) GiveBackHeld();
    }

    /// <summary>Give the handle back: its endpoint first (every byte unlocked while the handle is open, kb/Work PB3549), then
    /// the handle.</summary>
    private void GiveBackHeld()
    {
        try { GiveUpRegions(); }
        finally
        {
            _held?.Dispose();
            _held = null;
        }
    }

    /// <summary>⛔ GIVE BACK WHAT A CLOSE UNDER COMMIT CONTROL KEPT (§9.1.15: <i>"the file lock is removed by the COMMIT
    /// statement"</i>; §9.1.18.5 at the run unit's end): the record locks published through the kept endpoint, the handle and
    /// the run-unit file lock. Nothing for a connector that is open (its handle is its OPEN's) or kept nothing.</summary>
    internal void ReleaseKept()
    {
        if (!_kept) return;
        _kept = false;
        try
        {
            if (Physical is { } physical && _regions is { } regions) PhysicalFileTable.ReleaseThrough(physical, regions);
            GiveBackHeld();
        }
        finally
        {
            _keptFileLock?.Dispose();
            _keptFileLock = null;
        }
    }

    // ── The changes ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE APPEND'S STEP (2.2.2.3, 2.2.2.5 R3), the last before a WRITE's bytes reach the writer. A covered connector
    /// whose run unit has no transaction on the file begins one under the entry mutex — the entry step, the lock test (another
    /// live transaction holds the end: '51') and the header, then the <c>L</c> record — or, when a ROLLBACK removed the file
    /// under it, begins one with no file at its start and creates the file again as its OPEN did; one whose transaction stands
    /// writes <c>L</c> at its first append and nothing after. A connector whose file lock admits another run unit's writer
    /// (<c>SHARING WITH ALL OTHER</c>) meets every other live transaction's END holding: '51'. Returns the failing status, or
    /// null; throws on a refusal (R5: '30').</summary>
    private string? JournalAppend()
    {
        bool sharing = FileLockPosture.AdmitsAnotherWriter(HostShare);
        if (!CommitControlled)
            return sharing && SequentialJournal.Enter(Medium, HostPath, null, null).Any ? FileStatusCode.RecordLocked : null;
        var journal = Journal;
        if (journal is null || _fileRemoved)
        {
            using var mutex = Transactions.HoldEntryMutex(HostPath);
            if (SequentialJournal.Enter(Medium, HostPath, journal?.Id, mutex).Any) return FileStatusCode.RecordLocked;
            if (_fileRemoved)
            {
                // The state at the last COMMIT had no file: the WRITE creates it again, as its OPEN did (2.2.2.7).
                journal ??= Transactions.Begin(HostPath, SequentialJournal.StartState.Absent);
                _writer = OpenWriter(FileMode.Append);
                _fileRemoved = false;
                return null;
            }
            journal = Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
            _writer!.Flush();
            journal.NoteFirstAppend(_held!.Length);
            return null;
        }
        if (sharing && SequentialJournal.Enter(Medium, HostPath, journal.Id, null).Any) return FileStatusCode.RecordLocked;
        if (!journal.Replaced && journal.CommittedLength is null)
        {
            _writer!.Flush();
            journal.NoteFirstAppend(_held!.Length);
        }
        return null;
    }

    /// <summary>⛔ THE OVERWRITE'S STEP (2.2.2.3, 2.2.2.5 R2), before <see cref="OverwriteInPlace(Stream, long, ReadOnlySpan{byte}, string, bool)"/>
    /// writes the record of <paramref name="length"/> bytes at <paramref name="anchor"/>: a covered connector whose run unit has no
    /// transaction begins one under the entry mutex (another live transaction holding the record: '51'); in the base phase the
    /// record's current bytes are journaled and synced before they are overwritten, the first time the record is rewritten. A
    /// connector whose file lock admits another run unit's writer meets every other live transaction's holding of the record:
    /// '51'. Returns the failing status, or null; throws on a refusal (R5: '30').</summary>
    private string? JournalImage(Stream stream, long anchor, int length)
    {
        bool sharing = FileLockPosture.AdmitsAnotherWriter(HostShare);
        if (!CommitControlled)
            return sharing && SequentialJournal.Enter(Medium, HostPath, null, null).HoldsRecordAt(anchor) ? FileStatusCode.RecordLocked : null;
        var journal = Journal;
        if (journal is null)
        {
            using var mutex = Transactions.HoldEntryMutex(HostPath);
            if (SequentialJournal.Enter(Medium, HostPath, null, mutex).HoldsRecordAt(anchor)) return FileStatusCode.RecordLocked;
            journal = Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
        }
        else if (sharing && SequentialJournal.Enter(Medium, HostPath, journal.Id, null).HoldsRecordAt(anchor))
            return FileStatusCode.RecordLocked;
        if (!journal.NeedsImage(_readOrdinal, anchor)) return null;
        byte[] image = new byte[length];
        long resume = stream.Position;
        stream.Seek(anchor, SeekOrigin.Begin);
        stream.ReadExactly(image);
        stream.Seek(resume, SeekOrigin.Begin);
        journal.NoteImage(_readOrdinal, anchor, image);
        return null;
    }

    /// <summary>⛔ THE READ'S STEP (2.2.2.3, 2.2.2.8): a READ not IGNORING LOCK through a connector whose file lock admits another
    /// run unit's writer (<c>SHARING WITH ALL OTHER</c>, the one posture under which another transaction can live beside it)
    /// meets every other live transaction's holding of the record it would make available — the whole file once replaced, a
    /// record it rewrote, one it appended — with '51' (§9.1.13.8 1)), and a dead one's is settled before the READ. The record
    /// a forward READ makes available starts at the file position indicator's offset; a backward one's is told by its ordinal.
    /// Null when the READ may proceed. The caller asks it after the record-lock conflict check (DOC-A.1-104's point).</summary>
    internal string? ReadHoldingsGate(bool previous)
    {
        if (!FileLockPosture.AdmitsAnotherWriter(HostShare) || HostPath.Length == 0 || _reader is null) return null;
        var holdings = SequentialJournal.Enter(Medium, HostPath, Journal?.Id, null);
        if (!holdings.Any) return null;
        long at = _lineSequential ? _lineByteOffset : _readOffset;
        bool held = previous ? holdings.HoldsRecordBefore(TargetReadOrdinal(previous), at) : holdings.HoldsRecordAt(at);
        return held ? FileStatusCode.RecordLocked : null;
    }

    // ── What the registry's COMMIT and ROLLBACK ask of the connector (2.2.2.6, 2.2.2.7) ──────────────────────────────

    /// <summary>Release the writer's buffered records to the operating environment for the COMMIT, which syncs them. Throws on a
    /// refusal: the COMMIT is refused in its first region (2.2.2.6), the transaction intact.</summary>
    internal void FlushForCommit() => _writer?.Flush();

    /// <summary>The writer's buffered records before a ROLLBACK, which removes them with the transaction's other appends:
    /// released to the operating environment, or — the medium refusing them — DISCARDED with the writer, which
    /// <see cref="AfterRollback"/> builds again over the restored file. Either way none of them survives the rollback.</summary>
    internal void FlushForRollback()
    {
        try { _writer?.Flush(); }
        catch (IOException)
        {
            _writer = null;            // its buffer is the transaction's own appends; it is never written
            _writerDiscarded = true;
        }
    }

    /// <summary>The ROLLBACK discarded the writer whose buffer the medium refused (<see cref="FlushForRollback"/>).</summary>
    private bool _writerDiscarded;

    /// <summary>The handle the COMMIT syncs the file through; null when the file is not there (a DELETE FILE removed it).</summary>
    internal FileStream? HandleForCommit => _held;

    /// <inheritdoc/>
    /// <remarks>The ROLLBACK writes the file through the kept handle; one a DELETE FILE closed is opened again for the purpose
    /// (and kept, when the connector is closed, until the ROLLBACK's end releases it).</remarks>
    FileStream IJournalData.Writable(bool create)
    {
        if (_held is { CanWrite: true } held) return held;
        GiveBackHeld();
        if (!IsOpen) _kept = true;
        return TakeHeld(create ? FileMode.OpenOrCreate : FileMode.Open, FileAccess.ReadWrite, IsOpen ? HostShare : _heldShare);
    }

    /// <inheritdoc/>
    /// <remarks>The file is about to be removed (a creation rolled back, START <c>A</c>): the writer and reader over the handle go
    /// with it, and the connector, which stays in its open mode, notes the removal (<see cref="_fileRemoved"/>).</remarks>
    void IJournalData.Release()
    {
        try
        {
            try { _writer?.Dispose(); }
            finally { _reader?.Dispose(); }
        }
        finally
        {
            if (IsOpen) _fileRemoved = true;
            (_writer, _reader) = (null, null);
            GiveBackHeld();
        }
    }

    /// <summary>⛔ THE CONNECTOR AFTER ITS RUN UNIT'S ROLLBACK (2.2.2.7; §14.9.36.4 GR3 restores the file and its locks and leaves
    /// the open mode to the implementor): an OUTPUT or EXTEND writer's next WRITE lands at the restored end, or — the file removed
    /// — creates it again; an I-O or INPUT reader keeps its file position indicator and its last-read anchors, its buffer
    /// re-read from the restored file (a removed file reads AT END and a REWRITE on it answers '43'); a print file's logical page
    /// is restored from the snapshot the last COMMIT or the OPEN took.</summary>
    internal void AfterRollback()
    {
        if (!IsOpen) return;
        RestorePageSnapshot();
        if (_fileRemoved)
        {
            _writerDiscarded = false;               // the next WRITE builds the writer when it creates the file again
            if (Mode is FileOpenMode.Input or FileOpenMode.IO)
            {
                OptionalAbsent = true;              // reads AT END ('10')
                PrevOpWasSuccessfulRead = false;    // a REWRITE answers '43'
            }
            return;
        }
        if (_writerDiscarded)
        {
            _writerDiscarded = false;
            _writer = OpenWriter(FileMode.Append);   // over the kept handle, at the restored end
        }
        else if (_writer is not null && _held is { CanSeek: true } held && _writer.BaseStream is FileStream) held.Seek(0, SeekOrigin.End);
        if (_reader is { BaseStream.CanSeek: true } reader)
        {
            reader.BaseStream.Seek(_lineSequential ? _lineByteOffset : _readOffset, SeekOrigin.Begin);
            reader.DiscardBufferedData();
        }
    }

    /// <summary>The COMMIT made the file's state the one a later ROLLBACK restores, the logical page's included.</summary>
    internal void AfterCommit() => TakePageSnapshot();

    // ── The logical page's snapshot (2.2.2.7) ────────────────────────────────────────────────────────────────────

    /// <summary>The LINAGE page state and the print stream's device line, as the bytes the last COMMIT or the OPEN left describe
    /// them.</summary>
    private readonly record struct PageState(long Counter, DeviceLine Device, bool TopMarginPending, int PageBody, int? Footing,
        int Top, int Bottom, bool Broken, bool PrintControl);

    private PageState _pageSnapshot;

    private void TakePageSnapshot() =>
        _pageSnapshot = new PageState(LinageCounter, _device, _topMarginPending, _pageBody, _footing, _top, _bottom, _linagePageBroken, _printControl);

    private void RestorePageSnapshot() =>
        (LinageCounter, _device, _topMarginPending, _pageBody, _footing, _top, _bottom, _linagePageBroken, _printControl) =
        (_pageSnapshot.Counter, _pageSnapshot.Device, _pageSnapshot.TopMarginPending, _pageSnapshot.PageBody, _pageSnapshot.Footing,
            _pageSnapshot.Top, _pageSnapshot.Bottom, _pageSnapshot.Broken, _pageSnapshot.PrintControl);
}
