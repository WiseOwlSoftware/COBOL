// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO;

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
    /// <remarks>A kept handle on a physical file the path no longer names (removed or renamed away outside the run unit, another
    /// file made at the path) is given back: the OPEN is an ordinary OPEN of the file at the path (kb/Work PB3533's rule). Unlike
    /// the keyed connector's kept state, nothing of the transaction lives in the handle: the journal is the registry's
    /// (<see cref="SequentialTransactions"/>), so there is nothing to retire to it.</remarks>
    private protected override void RetireWhatWasKeptOfAnotherFile(FilePresence presence)
    {
        if (presence is FilePresence.Present && HoldsKeptHandle && HostFile.IdentityOf(_held!.SafeFileHandle) is { } kept
            && HostFile.IdentityAt(HostPath) is { } now && kept != now)
            ReleaseKept();
    }

    /// <inheritdoc/>
    /// <remarks>Nothing of the transaction lives in the handle (the journal is the registry's, <see cref="SequentialTransactions"/>),
    /// so taking it back (<see cref="TakeHeld"/>, with the access and share mode it had) restores the file lock its share mode is,
    /// and the Table 19 one is resumed beside it (kb/Work PB3492's sibling).</remarks>
    internal override RemovalGiveBack? GiveBackForRemoval()
    {
        if (!_kept) return null;
        var givenBack = new HeldGivenBack(this, _held is { } held ? (held.CanWrite ? FileAccess.ReadWrite : FileAccess.Read, _heldShare) : null,
            _keptFileLock);
        try { GiveBackHeld(); }
        catch
        {
            givenBack.Dispose();
            throw;
        }
        return givenBack;
    }

    /// <summary>What <see cref="GiveBackForRemoval"/> gave back: the kept handle's access and share mode (null: none was held) and
    /// the kept Table 19 file lock.</summary>
    private sealed class HeldGivenBack(SequentialConnector owner, (FileAccess Access, FileShare Share)? held, RunUnitFileLock? fileLock)
        : RemovalGiveBack
    {
        internal override void SuspendFileLock() => fileLock?.Suspend();

        private protected override void TakeBack()
        {
            if (held is { } h && owner._kept && owner._held is null)
                try { owner.TakeHeld(FileMode.Open, h.Access, h.Share); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }   // the window: the kept state stays without its handle
            if (fileLock is not null && !fileLock.Resume() && ReferenceEquals(owner._keptFileLock, fileLock)) owner._keptFileLock = null;
        }

        private protected override void Landed()
        {
            fileLock?.Dispose();
            if (ReferenceEquals(owner._keptFileLock, fileLock)) owner._keptFileLock = null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>The entry step itself is <see cref="FileConnector.EnterPhysicalFile"/>'s, the same for every organization.</remarks>
    private protected override void LeaveEntry()
    {
        base.LeaveEntry();
        _adoptedKept = false;
    }

    /// <summary>⛔ The covered OPEN's journal step (2.2.2.2), under the entry mutex: an OPEN OUTPUT of an EXISTING file begins the
    /// transaction if none stands (START <c>P</c>) and CAPTURES the file while the transaction is in its base phase (R4) — the
    /// truncation is the writer's, after it (<see cref="HeldWriteStream"/>); an OPEN that CREATES the file (OUTPUT of an absent
    /// file, EXTEND or I-O of an absent OPTIONAL one) begins the transaction with no file at its start (START <c>A</c>). Throws
    /// on a refusal: the OPEN answers '30' with the file unaffected. A journal this OPEN begins is <see cref="FileConnector._begunByOpen"/>
    /// until the OPEN ends, withdrawn by an unsuccessful one that did not change the file (<see cref="AbandonOpen"/>).</summary>
    private void BeginCoveredOpen(FileOpenMode mode, bool exists)
    {
        _fileRemoved = false;
        var journal = Journal;
        if (exists)
        {
            if (mode is not FileOpenMode.Output) return;
            journal ??= _begunByOpen = Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
            journal.Capture(SequentialJournal.CaptureVerb.OpenOutput, HeldStream(FileMode.Open));
        }
        else if (mode is FileOpenMode.Output || IsOptional && mode is FileOpenMode.Extend or FileOpenMode.IO)
            journal ??= _begunByOpen = Transactions.Begin(HostPath, SequentialJournal.StartState.Absent);
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

    /// <summary>Open the covered connector's handle on the medium. A handle taken <see cref="FileMode.CreateNew"/> made the file, which
    /// the OPEN that took it has then changed (<see cref="FileConnector._openChangedFile"/>); one the path already names is refused
    /// (<see cref="HostFile.IsAlreadyExists"/>).</summary>
    private FileStream TakeHeld(FileMode mode, FileAccess access, FileShare share)
    {
        _held = Medium.OpenConnectorStream(HostPath, mode, access, share, FileOptions.None);
        _heldShare = share;
        if (mode is FileMode.CreateNew) _openChangedFile = true;
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
    /// repositioning stream (<see cref="HostFile.WriteStreamOf"/>, the one decision). <see cref="FileMode.CreateNew"/> is a creation
    /// decided on an absent presence probe (an OUTPUT, or an EXTEND of an absent OPTIONAL file): the handle refuses a file made since
    /// (kb/Work PB3635), where every other mode adopts or creates the file.</summary>
    private Stream HeldWriteStream(FileMode mode)
    {
        var held = HeldStream(mode is FileMode.CreateNew ? FileMode.CreateNew : FileMode.OpenOrCreate);
        if (mode == FileMode.Create)
        {
            held.SetLength(0);
            held.Position = 0;
            _openChangedFile = true;   // the file's bytes are the OPEN's now: its journal stands for item 8's ROLLBACK
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
    /// live transaction holds the end: '51') and the header, then the <c>L</c> record (<see cref="BeginAppending"/>) — or, when a
    /// ROLLBACK removed the file under it, begins one with no file at its start and creates the file again as its OPEN did; one
    /// whose transaction stands writes <c>L</c> at its first append and nothing after. A connector whose file lock admits another
    /// run unit's writer (<c>SHARING WITH ALL OTHER</c>) meets every other live transaction's END holding: '51'. Every lock test
    /// is <see cref="JournalLockTest"/>'s, the statement's RETRY phrase applying. Returns the failing status, or null; throws on
    /// a refusal (R5: '30'). <paramref name="release"/> is what the caller holds until its record is RELEASED to the operating
    /// environment (<see cref="UncoveredChangeGuard"/>), null when nothing is.</summary>
    private string? JournalAppend(out IDisposable? release)
    {
        release = null;
        bool sharing = FileLockPosture.AdmitsAnotherWriter(HostShare);
        if (!CommitControlled) return sharing ? UncoveredChangeGuard(EveryHolding, out release) : null;
        var journal = Journal;
        if (journal is null || _fileRemoved) return JournalLockTest(journal?.Id, EveryHolding, underMutex: true, BeginAppending);
        if (sharing && JournalLockTest(journal.Id, EveryHolding, underMutex: false) is { } held) return held;
        if (!journal.Replaced && journal.CommittedLength is null)
        {
            _writer!.Flush();
            journal.NoteFirstAppend(_held!.Length);
        }
        return null;
    }

    /// <summary>Every live transaction holds the end of the file (2.2.2.3), so every holding meets a WRITE.</summary>
    private static readonly Func<SequentialJournal.Holding, bool> EveryHolding = static _ => true;

    /// <summary>The covered WRITE's first change, under the entry mutex its lock test holds (<see cref="JournalAppend"/>): the state
    /// at the last COMMIT had no file (a ROLLBACK removed it, 2.2.2.7) — the transaction begins with no file at its start and the
    /// WRITE creates the file again, as its OPEN did (<see cref="CreateTheFileAgain"/>); otherwise the transaction begins and its
    /// <c>L</c> is read at this first append. Returns the failing I-O status, or null; throws on a refusal (R5: '30').</summary>
    private string? BeginAppending()
    {
        if (_fileRemoved) return CreateTheFileAgain();
        var journal = Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
        _writer!.Flush();
        journal.NoteFirstAppend(_held!.Length);
        return null;
    }

    /// <summary>⛔ THE WRITE THAT CREATES THE FILE AGAIN AFTER THE RUN UNIT'S ROLLBACK REMOVED IT (§14.9.36.4 GR3, <i>"restored to the
    /// state they were in at the execution of the last COMMIT statement or the start of the run unit"</i>, a state with no file;
    /// DESIGN-runtime-library.md 2.2.2.14; kb/Work PB3492, PB3635; the keyed twin is <c>KeyedConnector.CreateTheFileAgain</c>). The
    /// ROLLBACK released this connector's file lock of a file not open at the last COMMIT (§9.1.15) and reversed only this run
    /// unit's own changes, so a file another run unit has made at the path since is protected from this one (§9.1.17): the WRITE
    /// is refused '30' (§9.1.13.6 item 1), never appended to it. The presence probe and the creating handle are one decision
    /// under the entry mutex the lock test holds, and the handle is opened <see cref="FileMode.CreateNew"/>, so a file made
    /// between the probe and the open (by a run unit whose OPEN takes no entry mutex) is refused rather than adopted.
    /// <para>An unsuccessful creation leaves the connector on no file (<see cref="_fileRemoved"/> stays set). The journal this WRITE
    /// began is WITHDRAWN when the file was not created (2.2.2.14: the file is as the journal's start says,
    /// <see cref="SequentialTransactions.Withdraw"/>); otherwise it is left to item 8's ROLLBACK (§14.6.13.1.3 8; the '30' is
    /// fatal), which removes the file this WRITE made. Returns the failing I-O status, or null.</para></summary>
    private string? CreateTheFileAgain()
    {
        SequentialJournal? begun = null;
        bool created = false;
        string? refused = null;
        try
        {
            if (HostFile.Probe(HostPath) is not FilePresence.Absent) return FileStatusCode.PermanentError;
            if (Journal is null) begun = Transactions.Begin(HostPath, SequentialJournal.StartState.Absent);
            _ = TakeHeld(FileMode.CreateNew, FileAccess.ReadWrite, HostShare);   // refuses a file made since the probe
            created = true;
            _writer = OpenWriter(FileMode.Append);   // over the handle just taken: HeldStream answers this OPEN's own
            if (!ResumeRunUnitFileLock()) refused = FileStatusCode.PermanentError;   // R5: the Table 19 column cannot be published again
        }
        catch (IOException e) { refused = FileStatusCode.ForWriteFailure(e); }   // '34' / '30'
        catch (UnauthorizedAccessException) { refused = FileStatusCode.PermanentError; }
        if (refused is null)
        {
            _fileRemoved = false;
            return null;
        }
        if (created) ((IJournalData)this).Release();   // the file this WRITE made stands for item 8's ROLLBACK; the connector leaves it
        else if (begun is not null) Transactions.Withdraw(begun);
        return refused;
    }

    /// <summary>⛔ THE OVERWRITE'S STEP (2.2.2.3, 2.2.2.5 R2), before <see cref="OverwriteInPlace(Stream, long, ReadOnlySpan{byte}, string, bool)"/>
    /// writes the record of <paramref name="length"/> bytes at <paramref name="anchor"/>: a covered connector whose run unit has no
    /// transaction begins one under the entry mutex (another live transaction holding the record: '51'); in the base phase the
    /// record's current bytes are journaled and synced before they are overwritten, the first time the record is rewritten. A
    /// connector whose file lock admits another run unit's writer meets every other live transaction's holding of the record:
    /// '51'. Every lock test is <see cref="JournalLockTest"/>'s, the statement's RETRY phrase applying. Returns the failing
    /// status, or null; throws on a refusal (R5: '30'). <paramref name="release"/> is <see cref="JournalAppend"/>'s.</summary>
    private string? JournalImage(Stream stream, long anchor, int length, out IDisposable? release)
    {
        release = null;
        bool sharing = FileLockPosture.AdmitsAnotherWriter(HostShare);
        bool HoldsTheRecord(SequentialJournal.Holding h) => h.HoldsRecordAt(anchor);
        if (!CommitControlled) return sharing ? UncoveredChangeGuard(HoldsTheRecord, out release) : null;
        var journal = Journal;
        if (journal is null)
        {
            if (JournalLockTest(null, HoldsTheRecord, underMutex: true,
                    () =>
                    {
                        journal = Transactions.Begin(HostPath, SequentialJournal.StartState.Present);
                        return null;
                    }) is { } refused)
                return refused;
        }
        else if (sharing && JournalLockTest(journal.Id, HoldsTheRecord, underMutex: false) is { } held)
            return held;
        if (!journal!.NeedsImage(_readOrdinal, anchor)) return null;
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
    /// The lock test is <see cref="JournalLockTest"/>'s, the statement's RETRY phrase applying. Null when the READ may proceed.
    /// The caller asks it after the record-lock conflict check (DOC-A.1-104's point).</summary>
    internal string? ReadHoldingsGate(bool previous)
    {
        if (!FileLockPosture.AdmitsAnotherWriter(HostShare) || HostPath.Length == 0 || _reader is null) return null;
        long at = _lineSequential ? _lineByteOffset : _readOffset;
        long ordinal = previous ? TargetReadOrdinal(previous) : 0;
        return JournalLockTest(Journal?.Id,
            previous ? h => h.HoldsRecordBefore(ordinal, at) : h => h.HoldsRecordAt(at), underMutex: false);
    }

    /// <summary>⛔ THE JOURNAL ARM OF THE LOCK TEST, UNDER THE STATEMENT'S RETRY PHRASE (2.2.2.3: <i>"held, '51' (RETRY
    /// applies)"</i>; §14.7.9.3 GR4 b): a record operation conflict is attempted again as GR1 to GR3 specify, and an attempt the
    /// holder permits makes the statement successful). Each attempt is one entry step (<see cref="SequentialJournal.Enter"/>,
    /// <paramref name="ownId"/> the connector's own transaction, which it never meets): a dead owner's transaction is settled,
    /// and a live one's holding that <paramref name="held"/> picks refuses the attempt (<see cref="SequentialJournal.Holdings.Conflict"/>:
    /// one of the run unit's own transactions, met through a connector no APPLY COMMIT names, is not waited for). With
    /// <paramref name="underMutex"/> the attempt holds the entry mutex — and <paramref name="proceed"/>, the first change, runs
    /// under it, so no other transaction begins between the test and the change — and gives it back before any wait, so a
    /// waiting statement never keeps another run unit's entry step out. Returns '51' (a RETRY's exhaustion status: '51', or
    /// FOREVER's '52' for the run unit's own holding), the status <paramref name="proceed"/> answered when the first change was
    /// refused (no conflict, so no RETRY: <see cref="CreateTheFileAgain"/>'s '30'), or null when the statement proceeds; throws on
    /// a refusal (R5: '30').</summary>
    private string? JournalLockTest(int? ownId, Func<SequentialJournal.Holding, bool> held, bool underMutex, Func<string?>? proceed = null) =>
        JournalLockTest(ownId, held, underMutex ? EntryMutexUse.ForTheAttempt : EntryMutexUse.None, proceed, out _);

    /// <summary>How one attempt of the journal lock test holds the entry mutex.</summary>
    private enum EntryMutexUse
    {
        /// <summary>Not at all: a covered connector whose own transaction stands (no other can begin beside it), or a READ.</summary>
        None,

        /// <summary>For the attempt and the first change it runs, then given back (a covered connector's first change).</summary>
        ForTheAttempt,

        /// <summary>For the attempt and, on success, KEPT for the caller until its record is released (<see cref="UncoveredChangeGuard"/>);
        /// where this host cannot hold it, the attempt proceeds without it (DOC-A.1-75's waiting hold).</summary>
        UntilTheRelease,
    }

    private string? JournalLockTest(int? ownId, Func<SequentialJournal.Holding, bool> held, EntryMutexUse use, Func<string?>? proceed,
        out RegionLocks? kept)
    {
        int? runUnitsOwn = Transactions.Of(HostPath)?.Id;
        RegionLocks? keep = null;
        string status = FileRegistry.RetryLoop(() =>
        {
            var mutex = use switch
            {
                EntryMutexUse.ForTheAttempt => Transactions.HoldEntryMutex(HostPath),
                EntryMutexUse.UntilTheRelease => Transactions.TryHoldEntryMutex(HostPath),
                _ => null,
            };
            try
            {
                if (SequentialJournal.Enter(Medium, HostPath, ownId, mutex).Conflict(held, runUnitsOwn) is { } conflict) return conflict;
                if (proceed?.Invoke() is { } refused) return RetryAttempt.InRunUnit(refused);   // a refusal that is no conflict: the statement's own status
                if (use == EntryMutexUse.UntilTheRelease) (keep, mutex) = (mutex, null);
                return RetryAttempt.InRunUnit(FileStatusCode.Success);
            }
            finally { mutex?.Dispose(); }   // given back before any wait and on a refusal, so a waiting statement keeps no one out
        }, StatementRetry.Kind, StatementRetry.Amount, waiter: this);
        kept = keep;
        return status == FileStatusCode.Success ? null : status;
    }

    /// <summary>⛔ THE CHANGE OF A CONNECTOR NO APPLY COMMIT NAMES, UNDER A FILE LOCK THAT ADMITS ANOTHER RUN UNIT'S WRITER (2.2.2.3;
    /// review finding R1-1 of kb/Work PB3340): its lock test AND its change are one step under the entry mutex, which
    /// <paramref name="release"/> holds until the caller has RELEASED the record to the operating environment (§14.9.51.4 GR12,
    /// §14.9.35.4 GR4). A test made outside it left a gap: another run unit's covered WRITE could begin its transaction and read
    /// its committed length L between this test and this write, so the record landed at or past L, outside every holding, and
    /// that transaction's ROLLBACK (or the settle after its death) truncated it away: a record of a connector with no transaction,
    /// which GR19 (<i>"the added records follow the records present in the physical file when it was opened"</i>) and GR12 keep.
    /// Under the mutex a transaction begins either before the test, which then meets its END holding ('51', the RETRY phrase
    /// applying), or after the release, whose bytes its L then counts. A REWRITE is the same step: a transaction that begins
    /// before its release cannot image the record ahead of this connector's bytes, so its rollback never writes the change away.</summary>
    private string? UncoveredChangeGuard(Func<SequentialJournal.Holding, bool> held, out IDisposable? release)
    {
        string? status = JournalLockTest(null, held, EntryMutexUse.UntilTheRelease, proceed: null, out var kept);
        release = kept;
        return status;
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
    /// with it, and the connector, which stays in its open mode, notes the removal (<see cref="_fileRemoved"/>). Its Table 19
    /// column goes too (<see cref="FileConnector.SuspendRunUnitFileLock"/>, kb/Work PB3492's sibling sweep): where the host
    /// publishes it through a handle on the file (a SHARING WITH ALL OTHER or READ ONLY connector on Windows) that handle refused
    /// the removal, and the ROLLBACK answered '30'; the WRITE that creates the file again publishes it again.</remarks>
    void IJournalData.Release()
    {
        try
        {
            try { _writer?.Dispose(); }
            finally { _reader?.Dispose(); }
        }
        finally
        {
            if (IsOpen)
            {
                _fileRemoved = true;
                SuspendRunUnitFileLock();
            }
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
