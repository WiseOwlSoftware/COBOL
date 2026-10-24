// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Globalization;
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE TRANSACTION JOURNAL OF A SEQUENTIAL FILE UNDER APPLY COMMIT (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2;
/// kb/Work PB3340; the owner's decision of 2026-10-19, "option a approved"): ONE run unit's open unit of work on ONE physical
/// file F, kept in THREE transient files in F's lock directory (<see cref="SidecarLockFiles.LockDirectoryOf"/>) —
/// <c>txn-&lt;id&gt;.live</c>, the HOLDER, whose lock is the owner's liveness on every host; <c>txn-&lt;id&gt;.journal</c>, the
/// RECORD, readable by every run unit while its owner lives; and <c>txn-&lt;id&gt;.before</c>, the BEFORE FILE a capture copies F
/// into. F itself stays byte-identical to what a program with no transactions writes (2.2.2.4: a record sequential file plain
/// bytes, a line sequential file plain text).
/// <para><b>Two phases (2.2.2.2).</b> In the BASE phase the state at the last COMMIT is <see cref="CommittedLength"/> L — F's
/// length at the transaction's FIRST append, read then and never at the begin (2.2.2.3) — and the first image of each record
/// REWRITTEN that is not the transaction's own append. The first OPEN OUTPUT of an existing file or DELETE FILE CAPTURES F as
/// it then stands (the before file) and ends the base phase: from then on the file is REPLACED and nothing more is journaled.
/// A transaction whose first change is a CREATION is replaced from the start, with no before file (START <c>A</c>), and its
/// rollback removes the file.</para>
/// <para><b>The record (2.2.2.4), little-endian.</b> A HEADER of <see cref="JournalHeaderBytes"/> bytes — magic <c>WOLJ</c>, the format
/// (4), the transaction id (8), the START (<c>P</c> present, <c>A</c> absent), three bytes of zero, a CRC-32C of the 20 before
/// it — then RECORDS, each its kind byte, its payload and a CRC-32C of both: <c>L</c> (the length 8), at most one; <c>R</c>
/// (ordinal 8, offset 8, length 4, the bytes), one per rewritten record of the base phase; <c>K</c> (<c>O</c> or <c>D</c>, the
/// before file's length 8), at most one; and <c>D</c> (<c>C</c> committed, <c>O</c> open again after a reversal, <c>R</c>
/// rolled back), the LAST whole one of which is the transaction's decision. A torn tail — a short record or a failed
/// checksum — ends the records: each record is synced before the change it precedes, so F is as the records before it say.
/// A torn header is litter with nothing to undo (R1: the header is synced before the first change).</para>
/// <para><b>The write-ahead rules (2.2.2.5)</b>, held by <c>scripts/models/sequential_journal_model.py</c> at every operation
/// and by this class's unit witnesses over the fault-injecting <see cref="IHostMedium"/>: R1 the header and the lock directory
/// synced before the first change (<see cref="Begin"/>); R2 an <c>R</c> record synced before its overwrite
/// (<see cref="NoteImage"/>); R3 the <c>L</c> record synced before the first append and nothing for later ones
/// (<see cref="NoteFirstAppend"/>); R4 the before file, its sync, the directory sync, then the <c>K</c> record and its sync,
/// and only then F truncated or removed (<see cref="Capture"/>); R5 a refused sync of a record handed to the operating
/// environment is fatal — the statement answers '30' and item 8's ROLLBACK follows (the caller's).</para>
/// <para><b>Who runs what.</b> The OWNER — the registry of the run unit whose transaction it is — begins it, writes its
/// records, commits it (<see cref="Commit"/>) and rolls it back (<see cref="Rollback"/>). ANY run unit's ENTRY STEP
/// (<see cref="Enter"/>) at a writable OPEN, a DELETE FILE or a covered connector's first change reads every live
/// transaction's holdings from its record and SETTLES every dead one (2.2.2.8): litter deleted, a decided one retired, an open
/// one rolled back through the entering run unit's own handle on F. Every step that changes F or the journal's names runs under
/// the ENTRY MUTEX (<see cref="HostRegionLocks.EntryMutexByte"/>) the caller holds.</para>
/// </summary>
internal sealed class SequentialJournal
{
    // ── The wire values: two runtime versions must find each other's journals ─────────────────────────────────────

    /// <summary>⛔ A WIRE VALUE: the prefix of every file of a transaction in the lock directory.</summary>
    internal const string NamePrefix = "txn-";

    /// <summary>⛔ A WIRE VALUE: the holder's suffix.</summary>
    private const string HolderSuffix = ".live";

    /// <summary>⛔ A WIRE VALUE: the record's suffix.</summary>
    private const string RecordSuffix = ".journal";

    /// <summary>⛔ A WIRE VALUE: the before file's suffix.</summary>
    private const string BeforeSuffix = ".before";

    /// <summary>⛔ A WIRE VALUE: the record's format.</summary>
    private const uint Format = 1;

    /// <summary>⛔ A WIRE VALUE: the length of the record's header.</summary>
    internal const int JournalHeaderBytes = 24;

    /// <summary>⛔ A WIRE VALUE: the record's magic.</summary>
    private static ReadOnlySpan<byte> Magic => "WOLJ"u8;

    private const byte LengthKind = (byte)'L', ImageKind = (byte)'R', CaptureKind = (byte)'K', DecisionKind = (byte)'D';

    /// <summary>The bytes of a record's checksum.</summary>
    private const int ChecksumBytes = 4;

    /// <summary>What F was at the transaction's first change (2.2.2.4's START).</summary>
    internal enum StartState : byte
    {
        /// <summary><c>P</c>: F was present; the base phase begins.</summary>
        Present = (byte)'P',

        /// <summary><c>A</c>: F was absent, and the first change creates it; replaced from the start.</summary>
        Absent = (byte)'A',
    }

    /// <summary>A transaction's decision, as its last whole <c>D</c> record says (none: <see cref="Open"/>).</summary>
    internal enum Decision : byte
    {
        /// <summary>No decision, or <c>O</c>: open again after a reversal (2.2.2.6 region 3).</summary>
        Open = (byte)'O',

        /// <summary><c>C</c>: committed; its retirement is owed.</summary>
        Committed = (byte)'C',

        /// <summary><c>R</c>: rolled back; its retirement is owed, and it is never rolled back again.</summary>
        RolledBack = (byte)'R',
    }

    /// <summary>The verb that captured F (2.2.2.4's <c>K</c> record).</summary>
    internal enum CaptureVerb : byte
    {
        /// <summary><c>O</c>: an OPEN OUTPUT of an existing file.</summary>
        OpenOutput = (byte)'O',

        /// <summary><c>D</c>: a DELETE FILE.</summary>
        DeleteFile = (byte)'D',
    }

    /// <summary>One <c>R</c> record: the first image of a rewritten record.</summary>
    internal readonly record struct Image(long Ordinal, long Offset, byte[] Bytes);

    /// <summary>A record as it stands on the medium (<see cref="ReadRecord"/>): the START, the <c>L</c>, the images, the capture
    /// and the decision.</summary>
    internal sealed record Contents(StartState Start, long? CommittedLength, IReadOnlyList<Image> Images, CaptureVerb? Capture,
        long CapturedLength, Decision Decision)
    {
        /// <summary>The file is REPLACED: every byte of it is the transaction's own (2.2.2.2).</summary>
        internal bool Replaced => Start == StartState.Absent || Capture is not null;
    }

    // ── The owner's transaction ──────────────────────────────────────────────────────────────────────────────────

    private readonly IHostMedium _medium;
    private readonly string _directory;
    private SidecarLockFiles.LockFileHolder? _holder;
    private FileStream? _record;
    private readonly HashSet<long> _imaged = [];

    private SequentialJournal(IHostMedium medium, string hostPath, string directory, int id, StartState start,
        SidecarLockFiles.LockFileHolder holder, FileStream record)
    {
        (_medium, HostPath, _directory, Id, Start, _holder, _record) = (medium, hostPath, directory, id, start, holder, record);
        Replaced = start == StartState.Absent;
    }

    /// <summary>The physical file's absolute host path.</summary>
    internal string HostPath { get; }

    /// <summary>The transaction's id: its files' names carry it.</summary>
    internal int Id { get; }

    /// <summary>What F was at the first change.</summary>
    internal StartState Start { get; }

    /// <summary>Whether the file is REPLACED: a creation, or a capture since (2.2.2.2).</summary>
    internal bool Replaced { get; private set; }

    /// <summary>L, the committed length the transaction's first append read (2.2.2.5 R3); null until it appends.</summary>
    internal long? CommittedLength { get; private set; }

    /// <summary>⛔ THE UNACKNOWLEDGED COMMIT (2.2.2.6 region 3): the decision's sync was refused and so was its reversal, so the
    /// transaction's fate on the medium is unknown. The only statements this run unit then runs on F are item 8's ROLLBACK and
    /// the termination path's, each of which repeats the reversal first (<see cref="Rollback"/>).</summary>
    internal bool Unacknowledged { get; private set; }

    /// <summary>The lock directory of the physical file at <paramref name="hostPath"/>.</summary>
    internal static string DirectoryOf(string hostPath) => SidecarLockFiles.LockDirectoryOf(hostPath);

    private static string HolderName(int id) => NamePrefix + id.ToString(CultureInfo.InvariantCulture) + HolderSuffix;
    private static string RecordName(int id) => NamePrefix + id.ToString(CultureInfo.InvariantCulture) + RecordSuffix;
    private static string BeforeName(int id) => NamePrefix + id.ToString(CultureInfo.InvariantCulture) + BeforeSuffix;

    /// <summary>The directory F's own name lives in, synced when F is created or removed.</summary>
    private static string FileDirectoryOf(string hostPath) => Path.GetDirectoryName(Path.GetFullPath(hostPath))!;

    /// <summary>⛔ BEGIN a transaction on <paramref name="hostPath"/> (2.2.2.3, 2.2.2.5 R1): the lowest id with neither a holder nor
    /// a record (<see cref="DrawId"/>; the entry step just settled every dead one under the same hold), the holder published and
    /// held, the record created, the header written and SYNCED, the lock directory synced — before the first byte of F changes or
    /// F is created. The caller holds the entry mutex and has run <see cref="Enter"/>. A refusal leaves no file behind and
    /// throws: the statement answers '30'.</summary>
    internal static SequentialJournal Begin(IHostMedium medium, string hostPath, StartState start)
    {
        string directory = DirectoryOf(hostPath);
        int id = DrawId(medium, directory);
        var holder = medium.PublishHolder(directory, HolderName(id))
            ?? throw new IOException($"the host cannot hold a transaction's holder in '{directory}' (DOC-A.1-75)");
        FileStream? record = null;
        try
        {
            record = medium.OpenTransient(Path.Combine(directory, RecordName(id)), FileMode.CreateNew, FileAccess.ReadWrite);
            Span<byte> header = stackalloc byte[JournalHeaderBytes];
            Magic.CopyTo(header);
            BinaryPrimitives.WriteUInt32LittleEndian(header[4..], Format);
            BinaryPrimitives.WriteInt64LittleEndian(header[8..], id);
            header[16] = (byte)start;
            BinaryPrimitives.WriteUInt32LittleEndian(header[20..], PageHeader.Crc32C(header[..20]));
            record.Write(header);
            record.Flush(flushToDisk: true);
            medium.SyncDirectory(directory);
        }
        catch
        {
            record?.Dispose();
            medium.RetireName(Path.Combine(directory, RecordName(id)));
            holder.Dispose();
            throw;
        }
        return new SequentialJournal(medium, Path.GetFullPath(hostPath), directory, id, start, holder, record);
    }

    /// <summary>The lowest non-negative id with neither a holder nor a record in <paramref name="directory"/> (2.2.2.4): a dead
    /// transaction's files were settled by the entry step just before, under the same hold of the entry mutex, so the smallest
    /// id is free once the directory is clean, and no mint is needed because the files ARE the transaction's entry.</summary>
    private static int DrawId(IHostMedium medium, string directory)
    {
        var taken = new HashSet<int>();
        foreach (string name in medium.ListNames(directory, NamePrefix))
            if (IdOf(name) is int id && (name.EndsWith(HolderSuffix, StringComparison.Ordinal) || name.EndsWith(RecordSuffix, StringComparison.Ordinal)))
                taken.Add(id);
        int next = 0;
        while (taken.Contains(next)) next++;
        return next;
    }

    /// <summary>The id a transaction file's name carries, or null for a name that is not one.</summary>
    private static int? IdOf(string name)
    {
        if (!name.StartsWith(NamePrefix, StringComparison.Ordinal)) return null;
        int dot = name.IndexOf('.', NamePrefix.Length);
        return dot > NamePrefix.Length
            && int.TryParse(name.AsSpan(NamePrefix.Length, dot - NamePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int id)
            ? id : null;
    }

    /// <summary>⛔ R3 — THE FIRST APPEND WRITES L; NO APPEND WRITES MORE (2.2.2.5). <paramref name="length"/> is F's length read
    /// through the writer's handle after the lock test found no other live transaction (2.2.2.3), so it is the committed length.
    /// The <c>L</c> record is appended and SYNCED before the record is written; a later append, or one in the replaced phase,
    /// writes nothing. Throws on a refusal (R5: '30', fatal).</summary>
    internal void NoteFirstAppend(long length)
    {
        if (Replaced || CommittedLength is not null) return;
        Span<byte> payload = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(payload, length);
        Append(LengthKind, payload);
        CommittedLength = length;   // handed over: the record may be on the medium, so the transaction's rollback truncates to it
        _record!.Flush(flushToDisk: true);
    }

    /// <summary>Whether a REWRITE of the record numbered <paramref name="ordinal"/> at <paramref name="offset"/> needs its image
    /// journaled (2.2.2.5 R2): the base phase only, a record that is not the transaction's own append (every offset before its
    /// first append, one below L after it), and the first REWRITE of that record only.</summary>
    internal bool NeedsImage(long ordinal, long offset) =>
        !Replaced && (CommittedLength is not { } committed || offset < committed) && !_imaged.Contains(ordinal);

    /// <summary>⛔ R2 — THE IMAGE BEFORE THE OVERWRITE (2.2.2.5): the record's current <paramref name="bytes"/> at
    /// <paramref name="offset"/> appended as an <c>R</c> record and SYNCED before the overwrite. The ordinal is claimed as soon as
    /// the record is handed over (it may be on the medium). Throws on a refusal (R5: '30', fatal; the overwrite never ran).</summary>
    internal void NoteImage(long ordinal, long offset, ReadOnlySpan<byte> bytes)
    {
        byte[] payload = new byte[20 + bytes.Length];
        BinaryPrimitives.WriteInt64LittleEndian(payload, ordinal);
        BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(8), offset);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), bytes.Length);
        bytes.CopyTo(payload.AsSpan(20));
        Append(ImageKind, payload);
        _imaged.Add(ordinal);
        _record!.Flush(flushToDisk: true);
    }

    /// <summary>⛔ R4 — THE CAPTURE (2.2.2.5): under the entry mutex, with no other connector's handle on F, F's bytes are copied
    /// through <paramref name="data"/> (the kept handle) into the before file, the before file SYNCED, the lock directory synced,
    /// THEN the <c>K</c> record appended and SYNCED — and only then may the caller truncate F (an OPEN OUTPUT) or remove it (a
    /// DELETE FILE). A COPY and never a rename: a handle follows the file it was opened on, so a rename would leave the file lock
    /// on the before file and F's name unguarded (2.2.2.4). A refusal before the <c>K</c> is handed over deletes the before
    /// file (F is intact) and throws; one after it throws with the transaction REPLACED, since the record may be on the medium
    /// (R5: fatal, item 8's rollback reads the before file, which holds what F still holds).</summary>
    internal void Capture(CaptureVerb verb, FileStream data)
    {
        if (Replaced) return;
        string before = Path.Combine(_directory, BeforeName(Id));
        bool handed = false;
        try
        {
            long length;
            using (var copy = _medium.OpenTransient(before, FileMode.Create, FileAccess.ReadWrite))
            {
                long position = data.Position;
                data.Position = 0;
                data.CopyTo(copy);
                data.Position = position;
                length = copy.Length;
                copy.Flush(flushToDisk: true);
            }
            _medium.SyncDirectory(_directory);
            Span<byte> payload = stackalloc byte[9];
            payload[0] = (byte)verb;
            BinaryPrimitives.WriteInt64LittleEndian(payload[1..], length);
            Append(CaptureKind, payload);
            handed = true;
            _record!.Flush(flushToDisk: true);
        }
        catch
        {
            if (handed) Replaced = true;
            else _medium.RetireName(before);
            throw;
        }
        Replaced = true;
    }

    /// <summary>Append one record — its kind, its payload and their checksum — to the owner's record. Not synced: the caller
    /// syncs it at the point its rule names.</summary>
    private void Append(byte kind, ReadOnlySpan<byte> payload)
    {
        byte[] unit = new byte[1 + payload.Length + ChecksumBytes];
        unit[0] = kind;
        payload.CopyTo(unit.AsSpan(1));
        BinaryPrimitives.WriteUInt32LittleEndian(unit.AsSpan(1 + payload.Length), PageHeader.Crc32C(unit.AsSpan(0, 1 + payload.Length)));
        _record!.Seek(0, SeekOrigin.End);
        _record.Write(unit);
    }

    private static void AppendDecision(FileStream record, Decision decision)
    {
        Span<byte> unit = stackalloc byte[2 + ChecksumBytes];
        unit[0] = DecisionKind;
        unit[1] = (byte)decision;
        BinaryPrimitives.WriteUInt32LittleEndian(unit[2..], PageHeader.Crc32C(unit[..2]));
        record.Seek(0, SeekOrigin.End);
        record.Write(unit);
    }

    /// <summary>⛔ COMMIT (2.2.2.6), in 2.2.1.10's three regions. (1) F synced through <paramref name="data"/> (null when F is absent:
    /// a DELETE FILE removed it) — the caller flushed the writer's buffer — and F's directory synced, so a file the transaction
    /// created or removed is created or removed on the medium too; (2) THE DECISION: <c>D C</c> appended and the record synced;
    /// (3) the retirement. A refusal before the decision is handed over is <see cref="CommitOutcome.Refused"/> (region 1: the
    /// transaction is intact, the COMMIT is unsuccessful and item 8's ROLLBACK restores from the journal). A refused sync of the
    /// handed-over decision (region 3) REVERSES it — <c>D O</c> appended and synced, and the caller then rolls back
    /// (<see cref="CommitOutcome.Reversed"/>) — or, when the reversal is refused too, marks the transaction
    /// <see cref="Unacknowledged"/> and touches nothing (<see cref="CommitOutcome.Unacknowledged"/>). A decision synced is
    /// <see cref="CommitOutcome.Committed"/> whatever becomes of the retirement: a journal left reading <c>C</c> is retired by the
    /// next entry, or by its own owner's next entry step.</summary>
    internal CommitOutcome Commit(FileStream? data)
    {
        var record = _record!;
        try
        {
            data?.Flush(flushToDisk: true);
            _medium.SyncDirectory(FileDirectoryOf(HostPath));
            AppendDecision(record, Decision.Committed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return CommitOutcome.Refused; }
        try { record.Flush(flushToDisk: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try
            {
                AppendDecision(record, Decision.Open);
                record.Flush(flushToDisk: true);
            }
            catch (Exception again) when (again is IOException or UnauthorizedAccessException)
            {
                Unacknowledged = true;
                return CommitOutcome.Unacknowledged;
            }
            return CommitOutcome.Reversed;
        }
        try { Retire(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }   // region 2: the next entry completes it
        return CommitOutcome.Committed;
    }

    /// <summary>⛔ THE OWNER'S ROLLBACK (2.2.2.7) under the entry mutex the caller holds: an unacknowledged COMMIT's reversal first
    /// (<see cref="Unacknowledged"/>: <c>D O</c> appended and synced, or nothing at all is touched), then the one idempotent
    /// sequence of <see cref="RollBack"/> from what the record says, through <paramref name="data"/>. Returns false when the
    /// medium refused a step: the rollback is OWED, the transaction stays open and its holder held, and the next COMMIT,
    /// ROLLBACK or termination runs it again before anything else.</summary>
    internal bool Rollback(IJournalData data)
    {
        if (Retired) return true;
        try
        {
            if (Unacknowledged)
            {
                AppendDecision(_record!, Decision.Open);
                _record!.Flush(flushToDisk: true);
                Unacknowledged = false;
            }
            var contents = ReadRecord(_record!) ?? throw new IOException($"the record of transaction {Id} on '{HostPath}' could not be read back");
            RollBack(_medium, HostPath, _directory, Id, contents, data, _record!);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
        // Rolled back: the record reads R, so a retirement the medium refuses is completed by the next entry (2.2.2.7 (3)).
        try { Retire(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return true;
    }

    /// <summary>The retirement (2.2.2.6 (3), 2.2.2.7 (4)): the before file, the record and the holder deleted in that order and
    /// the lock directory synced; the directory goes with its last file (<see cref="SidecarLockFiles.LockFileHolder"/>).</summary>
    private void Retire()
    {
        try
        {
            _medium.RetireName(Path.Combine(_directory, BeforeName(Id)));
            _record?.Dispose();
            _record = null;
            _medium.RetireName(Path.Combine(_directory, RecordName(Id)));
            _medium.SyncDirectory(_directory);
        }
        finally
        {
            // The holder goes whatever the deletions did: the decision is on the medium, so a dead holder's leftovers are the
            // next entry's to retire, while a holder kept alive would read as a live transaction to every other run unit.
            _record?.Dispose();
            _record = null;
            _holder?.Dispose();
            _holder = null;
        }
    }

    /// <summary>Whether the transaction is over: committed or rolled back and retired.</summary>
    internal bool Retired => _holder is null;

    /// <summary>⛔ A DEATH, FOR A WITNESS: the holder and the record are closed as the operating environment closes a dead
    /// process's handles — the holder's name is left for the next entry to find dead — and nothing is retired.</summary>
    internal void AbandonAsADeathDoes()
    {
        _record?.Dispose();
        _record = null;
        _holder?.DropAsADeathDoes();
        _holder = null;
    }

    // ── The rollback sequence, run by the owner or by whoever settles a dead owner's journal ─────────────────────────

    /// <summary>⛔ THE ONE ROLLBACK SEQUENCE (2.2.2.7), idempotent at every step, run by the OWNER (<see cref="Rollback"/>) or by the
    /// entering run unit for a dead owner (<see cref="Settle"/>): (1) a whole <c>K</c> — F is replaced by the before file, created
    /// if absent and truncated if present, through <paramref name="data"/>; a before file with no whole <c>K</c> is litter (F was
    /// never truncated, R4) and deleted; (2) START <c>P</c> — every image written back at its offset (one at or past F's end is
    /// left to the truncation), F truncated to L when the record has one (F is never shorter than it: only an appending
    /// transaction's rollback truncates, to its own L, and two transactions never append at once), F synced and its directory
    /// synced; START <c>A</c> — F removed (the owner's handle released first) and its directory synced; (3) <c>D R</c> appended
    /// and synced, so a journal a power loss resurrects after its before file went is retired and never rolled back again.
    /// The retirement is the caller's.</summary>
    private static void RollBack(IHostMedium medium, string hostPath, string directory, int id, Contents contents, IJournalData data,
        FileStream record)
    {
        string before = Path.Combine(directory, BeforeName(id));
        if (contents.Capture is not null)
        {
            var f = data.Writable(create: true);
            using var copy = medium.OpenTransient(before, FileMode.Open, FileAccess.Read);
            if (copy.Length != contents.CapturedLength)
                throw new IOException($"the before file of transaction {id} on '{hostPath}' holds {copy.Length} bytes where its capture recorded {contents.CapturedLength}");
            f.SetLength(0);
            f.Position = 0;
            copy.CopyTo(f);
        }
        else medium.RetireName(before);
        if (contents.Start == StartState.Present)
        {
            var f = data.Writable(create: false);
            foreach (var image in contents.Images)
            {
                if (image.Offset >= f.Length) continue;
                f.Position = image.Offset;
                f.Write(image.Bytes);
            }
            if (contents.CommittedLength is long committed)
            {
                if (f.Length < committed)
                    throw new IOException($"'{hostPath}' holds {f.Length} bytes, fewer than the committed length {committed} transaction {id} recorded: a truncation would extend it");
                f.SetLength(committed);
            }
            f.Flush(flushToDisk: true);
            medium.SyncDirectory(FileDirectoryOf(hostPath));
        }
        else
        {
            data.Release();
            medium.DeleteFile(hostPath);
            medium.SyncDirectory(FileDirectoryOf(hostPath));
        }
        AppendDecision(record, Decision.RolledBack);
        record.Flush(flushToDisk: true);
    }

    /// <summary>Read a record as it stands (2.2.2.4): null for a header that is short, of another magic or format, or whose
    /// checksum fails — litter with nothing to undo; otherwise its contents up to the first torn record.</summary>
    internal static Contents? ReadRecord(Stream record)
    {
        record.Position = 0;
        var reader = new BufferedStream(record, 64 * 1024);
        Span<byte> header = stackalloc byte[JournalHeaderBytes];
        if (!Fill(reader, header) || !header[..4].SequenceEqual(Magic)
            || BinaryPrimitives.ReadUInt32LittleEndian(header[4..]) != Format
            || BinaryPrimitives.ReadUInt32LittleEndian(header[20..]) != PageHeader.Crc32C(header[..20])
            || header[16] is not ((byte)StartState.Present or (byte)StartState.Absent))
            return null;
        var start = (StartState)header[16];
        long? committed = null;
        var images = new List<Image>();
        CaptureVerb? capture = null;
        long captured = 0;
        var decision = Decision.Open;
        while (NextRecord(reader) is { } unit)
        {
            var payload = unit.AsSpan(1, unit.Length - 1 - ChecksumBytes);
            switch (unit[0])
            {
                case LengthKind: committed ??= BinaryPrimitives.ReadInt64LittleEndian(payload); break;
                case ImageKind:
                    images.Add(new Image(BinaryPrimitives.ReadInt64LittleEndian(payload), BinaryPrimitives.ReadInt64LittleEndian(payload[8..]),
                        payload[20..].ToArray()));
                    break;
                case CaptureKind:
                    capture ??= (CaptureVerb)payload[0];
                    captured = BinaryPrimitives.ReadInt64LittleEndian(payload[1..]);
                    break;
                case DecisionKind: decision = (Decision)payload[0]; break;
            }
        }
        return new Contents(start, committed, images, capture, captured, decision);
    }

    /// <summary>The next whole record, or null at the end or at a torn one (a short record, an unknown kind, a failed checksum).</summary>
    private static byte[]? NextRecord(Stream reader)
    {
        int kind = reader.ReadByte();
        int payload;
        switch (kind)
        {
            case LengthKind: payload = 8; break;
            case CaptureKind: payload = 9; break;
            case DecisionKind: payload = 1; break;
            case ImageKind:
                Span<byte> head = stackalloc byte[20];
                if (!Fill(reader, head)) return null;
                int length = BinaryPrimitives.ReadInt32LittleEndian(head[16..]);
                if (length < 0) return null;
                byte[] image = new byte[1 + 20 + length + ChecksumBytes];
                image[0] = ImageKind;
                head.CopyTo(image.AsSpan(1));
                return Fill(reader, image.AsSpan(21)) && Whole(image) ? image : null;
            default: return null;
        }
        byte[] unit = new byte[1 + payload + ChecksumBytes];
        unit[0] = (byte)kind;
        return Fill(reader, unit.AsSpan(1)) && Whole(unit) ? unit : null;
    }

    /// <summary>Read exactly <paramref name="into"/>'s length, or false at the end of the record (a torn tail).</summary>
    private static bool Fill(Stream reader, Span<byte> into) =>
        reader.ReadAtLeast(into, into.Length, throwOnEndOfStream: false) == into.Length;

    private static bool Whole(byte[] unit) =>
        BinaryPrimitives.ReadUInt32LittleEndian(unit.AsSpan(unit.Length - ChecksumBytes)) == PageHeader.Crc32C(unit.AsSpan(0, unit.Length - ChecksumBytes));

    // ── The entry step: live holdings read, dead transactions settled ───────────────────────────────────────────────

    /// <summary>A live transaction's holdings as its record says them (2.2.2.3): the WHOLE file once replaced (or while its
    /// header is not yet readable — a transaction being born, or a record this process may not read), the end of the file always,
    /// every rewritten record's offset, and every offset at or past L once it has appended. <see cref="Id"/> is the transaction's
    /// id in the lock directory (<see cref="NoId"/> when the directory itself cannot be listed): the lock test tells by it a
    /// holding of the tester's own run unit, which no wait can release (<see cref="Holdings.Conflict"/>).</summary>
    internal readonly record struct Holding(int Id, bool Whole, long? CommittedLength, IReadOnlySet<long> RewrittenOffsets,
        IReadOnlySet<long> RewrittenOrdinals)
    {
        /// <summary>Whether a READ not IGNORING LOCK or a REWRITE of the record at <paramref name="offset"/> meets this holding.</summary>
        internal bool HoldsRecordAt(long offset) =>
            Whole || RewrittenOffsets.Contains(offset) || (CommittedLength is long committed && offset >= committed);

        /// <summary>Whether a backward READ of the record numbered <paramref name="ordinal"/>, which lies before
        /// <paramref name="before"/>, meets this holding: a rewritten record is told by its ordinal, and the appended records past
        /// L can lie before <paramref name="before"/> only when L does.</summary>
        internal bool HoldsRecordBefore(long ordinal, long before) =>
            Whole || RewrittenOrdinals.Contains(ordinal) || (CommittedLength is long committed && committed < before);
    }

    /// <summary>The id of a holding whose transaction is not known (a lock directory this process may not list).</summary>
    internal const int NoId = -1;

    /// <summary>What the entry step found: the holdings of every live transaction on the file other than the caller's own.</summary>
    internal sealed class Holdings(IReadOnlyList<Holding> live)
    {
        /// <summary>No live transaction other than the caller's.</summary>
        internal static readonly Holdings None = new([]);

        /// <summary>The live transactions' holdings.</summary>
        internal IReadOnlyList<Holding> Live { get; } = live;

        /// <summary>Whether any other transaction lives: it holds the end of the file, so a WRITE answers '51' (2.2.2.3).</summary>
        internal bool Any => Live.Count > 0;

        /// <summary>The '61' a writable OPEN or a DELETE FILE meets (2.2.2.8): any live transaction for an OPEN OUTPUT or a DELETE
        /// FILE (<paramref name="exclusive"/>), a replaced one for any other writable OPEN; null when the OPEN may proceed.</summary>
        internal string? RefusesOpen(bool exclusive) =>
            Live.Any(h => exclusive || h.Whole) ? FileStatusCode.FileSharingConflict : null;

        /// <summary>Whether a READ not IGNORING LOCK or a REWRITE of the record at <paramref name="offset"/> meets a holding.</summary>
        internal bool HoldsRecordAt(long offset) => Live.Any(h => h.HoldsRecordAt(offset));

        /// <summary>⛔ THE LOCK TEST'S ANSWER (2.2.2.3: "held, '51' (RETRY applies)"): null when no holding <paramref name="held"/>
        /// picks lives; otherwise the record operation conflict '51' (§9.1.13.8 1)), its holder OUTSIDE the run unit — a holder a
        /// RETRY phrase waits for (§14.7.9.3 GR1 to GR3) — unless every such holding is the run unit's own transaction
        /// <paramref name="runUnitsOwn"/>, met through a connector no APPLY COMMIT clause names, which cannot end while the
        /// statement runs: a RETRY makes no wait for it, and FOREVER ends with the deadlock '52' (A.1 item 109).</summary>
        internal RetryAttempt? Conflict(Func<Holding, bool> held, int? runUnitsOwn)
        {
            bool any = false, outside = false;
            foreach (var holding in Live)
                if (held(holding))
                {
                    any = true;
                    outside |= holding.Id != runUnitsOwn;
                }
            return !any ? null
                : outside ? RetryAttempt.OutsideRunUnit(FileStatusCode.RecordLocked) : RetryAttempt.InRunUnit(FileStatusCode.RecordLocked);
        }
    }

    /// <summary>⛔ THE ENTRY STEP (2.2.2.8) on the physical file <paramref name="hostPath"/>: the lock directory listed for
    /// transaction files, and for every id but <paramref name="ownId"/> (the caller's own transaction, when the caller is a
    /// connector under commit control): its holder held → a live transaction, whose holdings are read from its record; its
    /// holder free → SETTLED from its record (<see cref="Settle"/>) under the entry mutex — <paramref name="mutex"/> when the
    /// caller holds it already, otherwise taken for that alone. A holder this process cannot observe, or a live record it cannot
    /// read, holds the WHOLE file: a refusal, never a silent proceed (2.2.2.7: '51' or '61' until the owner or an administrator
    /// removes the directory). A file NOT under APPLY COMMIT pays one listing of a directory that usually does not exist.</summary>
    internal static Holdings Enter(IHostMedium medium, string hostPath, int? ownId, RegionLocks? mutex)
    {
        string directory = DirectoryOf(hostPath);
        IReadOnlyList<string> names;
        try { names = medium.ListNames(directory, NamePrefix); }
        catch (UnauthorizedAccessException) { return new Holdings([WholeFileOf(NoId)]); }
        if (names.Count == 0) return Holdings.None;
        var ids = new SortedSet<int>();
        foreach (string name in names)
            if (IdOf(name) is int id && id != ownId) ids.Add(id);
        var live = new List<Holding>();
        List<int>? dead = null;
        foreach (int id in ids)
        {
            switch (medium.ProbeHolder(Path.Combine(directory, HolderName(id))))
            {
                case HostFile.LockFileState.Live:
                    live.Add(HoldingOf(medium, id, Path.Combine(directory, RecordName(id))));
                    break;
                case HostFile.LockFileState.Unobservable:
                    live.Add(WholeFileOf(id));
                    break;
                default:
                    (dead ??= []).Add(id);
                    break;
            }
        }
        if (dead is not null) SettleUnderMutex(medium, hostPath, directory, dead, mutex);
        return live.Count == 0 ? Holdings.None : new Holdings(live);
    }

    private static readonly IReadOnlySet<long> EmptyOffsets = new HashSet<long>();

    /// <summary>The holding of the transaction <paramref name="id"/> whose record cannot be read: the whole file.</summary>
    private static Holding WholeFileOf(int id) => new(id, true, null, EmptyOffsets, EmptyOffsets);

    /// <summary>A live transaction's holding, read from its record; a record not yet readable (the transaction is being born, or
    /// this process may not read it) holds the whole file.</summary>
    private static Holding HoldingOf(IHostMedium medium, int id, string recordPath)
    {
        try
        {
            using var record = medium.OpenTransient(recordPath, FileMode.Open, FileAccess.Read);
            if (ReadRecord(record) is not { } contents) return WholeFileOf(id);
            return new Holding(id, contents.Replaced, contents.CommittedLength, contents.Images.Select(i => i.Offset).ToHashSet(),
                contents.Images.Select(i => i.Ordinal).ToHashSet());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return WholeFileOf(id); }
    }

    /// <summary>Settle the dead transactions <paramref name="ids"/> under the entry mutex: <paramref name="held"/> when the caller
    /// holds it, otherwise taken (waiting) for that alone and given back.</summary>
    private static void SettleUnderMutex(IHostMedium medium, string hostPath, string directory, List<int> ids, RegionLocks? held)
    {
        RegionLocks? taken = null;
        try
        {
            if (held is null)
            {
                taken = medium.EntryMutex(hostPath);
                if (taken.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true) != RegionLockResult.Ok)
                    throw new IOException($"the entry mutex of '{hostPath}' cannot be held here (DOC-A.1-75)");
            }
            foreach (int id in ids)
            {
                // Probed again under the mutex: an owner retiring its own transaction, or another settler, may have finished it.
                var state = medium.ProbeHolder(Path.Combine(directory, HolderName(id)));
                if (state is HostFile.LockFileState.Live or HostFile.LockFileState.Unobservable) continue;
                Settle(medium, hostPath, directory, id);
            }
        }
        finally { taken?.Dispose(); }
    }

    /// <summary>⛔ SETTLE a dead transaction (2.2.2.8), whoever runs it: no record, or a torn header → litter (the holder, a before
    /// file, the record: deleted); the decision <c>C</c> or <c>R</c> → the retirement completed; open → the rollback of
    /// <see cref="RollBack"/> through this run unit's own short-lived writable handle on F, then the retirement. The caller holds
    /// the entry mutex. Throws when the medium refuses a step: the journal stays and the next entry runs the same sequence.</summary>
    private static void Settle(IHostMedium medium, string hostPath, string directory, int id)
    {
        string recordPath = Path.Combine(directory, RecordName(id));
        Contents? contents = null;
        FileStream? record = null;
        try
        {
            try { record = medium.OpenTransient(recordPath, FileMode.Open, FileAccess.ReadWrite); }
            catch (FileNotFoundException) { }
            if (record is not null) contents = ReadRecord(record);
            if (contents is { Decision: Decision.Open } open)
            {
                using var data = new AuxiliaryData(medium, hostPath);
                RollBack(medium, hostPath, directory, id, open, data, record!);
            }
        }
        finally { record?.Dispose(); }
        medium.RetireName(Path.Combine(directory, BeforeName(id)));
        medium.RetireName(recordPath);
        medium.RetireName(Path.Combine(directory, HolderName(id)));
        medium.SyncDirectory(directory);
        medium.RetireDirectory(directory);
    }

    /// <summary>The physical file as a dead owner's settle reaches it: a short-lived writable handle of the entering run unit,
    /// opened for the purpose (2.2.2.7: <i>"the entering run unit's own writable handle, opened for the purpose, when the owner is
    /// dead"</i>) and closed with the settle.</summary>
    private sealed class AuxiliaryData(IHostMedium medium, string hostPath) : IJournalData, IDisposable
    {
        private FileStream? _handle;

        public FileStream Writable(bool create) =>
            _handle ??= medium.OpenAuxiliary(hostPath, create ? FileMode.OpenOrCreate : FileMode.Open, FileAccess.ReadWrite);

        public void Release()
        {
            _handle?.Dispose();
            _handle = null;
        }

        public void Dispose() => Release();
    }
}

/// <summary>The physical file as a ROLLBACK reaches it (DESIGN-runtime-library.md 2.2.2.7): the owner's kept handle — or one
/// opened for the purpose when a DELETE FILE closed it — or the entering run unit's own handle for a dead owner's journal.</summary>
internal interface IJournalData
{
    /// <summary>A writable handle on F; <paramref name="create"/> creates F when it is absent (the restore of a capture).</summary>
    FileStream Writable(bool create);

    /// <summary>Close the handle before F is removed (a Windows handle not opened for deletion blocks the removal).</summary>
    void Release();
}
