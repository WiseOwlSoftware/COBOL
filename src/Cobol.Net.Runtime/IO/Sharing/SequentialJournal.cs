// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Globalization;
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Runtime.IO;

/// <summary>What a sequential transaction's physical file was at its begin (docs/rearchitecture/DESIGN-runtime-library.md
/// 2.2.2.4: the header's START). ⛔ A WIRE VALUE.</summary>
internal enum JournalStart : byte
{
    /// <summary>The file was present: the BASE phase, undone by the images and the committed length.</summary>
    Present = (byte)'P',
    /// <summary>The file was absent (the first change created it): REPLACED from the start, undone by removing the file.</summary>
    Absent = (byte)'A',
}

/// <summary>The verb of a CAPTURE (2.2.2.2 (c) and (d)): the record <c>K</c>'s kind. ⛔ A WIRE VALUE.</summary>
internal enum CaptureKind : byte
{
    /// <summary>An OPEN OUTPUT of an existing file, which truncates it (§14.9.27.4 GR18).</summary>
    OpenOutput = (byte)'O',
    /// <summary>A DELETE FILE (§14.9.10.2, format 2), which removes it.</summary>
    DeleteFile = (byte)'D',
}

/// <summary>A transaction's decision, the record <c>D</c> (2.2.2.6, 2.2.2.7): the LAST whole one is the decision. ⛔ A WIRE VALUE.</summary>
internal enum JournalDecision : byte
{
    /// <summary>No decision, or an unacknowledged COMMIT reversed (<c>D O</c>): the transaction is open.</summary>
    Open = (byte)'O',
    /// <summary>Committed: the retirement is all that is left.</summary>
    Committed = (byte)'C',
    /// <summary>Rolled back: the retirement is all that is left, and the journal is never rolled back again.</summary>
    RolledBack = (byte)'R',
}

/// <summary>The phase of a live transaction (2.2.2.2).</summary>
internal enum JournalPhase
{
    /// <summary>The file is the committed file with appends past its committed length and overwrites below it.</summary>
    Base,
    /// <summary>Every byte of the file is the transaction's own: after a capture, or from a creation.</summary>
    Replaced,
}

/// <summary>The first image of one rewritten record (the record <c>R</c>): its ordinal, its byte offset and its bytes.</summary>
internal readonly record struct RewriteImage(long Ordinal, long Offset, byte[] Bytes);

/// <summary>A capture (the record <c>K</c>): its verb and the before file's length.</summary>
internal readonly record struct JournalCapture(CaptureKind Kind, long Length);

/// <summary>What a transaction's record says, as <see cref="SequentialJournal.Parse"/> reads it: its header and every whole
/// record before the first torn one (2.2.2.4).</summary>
internal sealed record JournalContent(ulong Id, JournalStart Start, long? CommittedLength, IReadOnlyList<RewriteImage> Images,
    JournalCapture? Capture, JournalDecision Decision)
{
    /// <summary>Is the file REPLACED (2.2.2.2): created by the transaction, or captured?</summary>
    internal bool Replaced => Start == JournalStart.Absent || Capture is not null;

    /// <summary>Does the transaction HOLD the record at <paramref name="ordinal"/> (byte offset <paramref name="offset"/>):
    /// the whole file once replaced, every ordinal it rewrote, and every record at or past its committed length (its own
    /// appends) — 2.2.2.3's (R), (END) and (ALL)?</summary>
    internal bool Holds(long ordinal, long offset) =>
        Replaced || CommittedLength is { } length && offset >= length
        || (_rewritten ??= [.. Images.Select(image => image.Ordinal)]).Contains(ordinal);

    /// <summary>The ordinals the transaction rewrote, built at the first lock test that asks (a transaction may hold millions).</summary>
    private HashSet<long>? _rewritten;
}

/// <summary>Another transaction on the file, as a statement's lock test meets it (2.2.2.3): its record's content, or null when
/// the transaction is being born (a holder with no header yet) or its record cannot be read.</summary>
internal readonly record struct OtherTransaction(ulong Id, JournalContent? Content);

/// <summary>What a statement asks of the other transactions on its file (2.2.2.3, 2.2.2.8).</summary>
internal enum JournalAccess
{
    /// <summary>A writable OPEN in the EXTEND or I-O mode.</summary>
    Open,
    /// <summary>An OPEN OUTPUT or a DELETE FILE, which needs no other transaction on the file at all.</summary>
    Exclusive,
    /// <summary>A WRITE: an append.</summary>
    Write,
    /// <summary>A READ not IGNORING LOCK, or a REWRITE, of one record.</summary>
    Record,
}

/// <summary>The answer of the journal's lock test (2.2.2.3).</summary>
internal enum JournalConflict
{
    /// <summary>Nothing another transaction holds: the statement proceeds.</summary>
    None,
    /// <summary>A record or the append point another transaction holds: I-O status '51' (§9.1.13.8 1)), RETRY applying.</summary>
    RecordLocked,
    /// <summary>The file another transaction holds: I-O status '61' (§9.1.13.9 1)) for an OPEN, '62' for a DELETE FILE.</summary>
    FileLocked,
}

/// <summary>A physical file as a rollback restores it (2.2.2.7): through the owner's kept handle, or through a writable handle the
/// settling run unit opens for the purpose (<see cref="RestoreTarget"/>).</summary>
internal abstract class JournalTarget
{
    /// <summary>A writable handle on the file, or null when the file is absent.</summary>
    internal abstract Stream? Writable();

    /// <summary>Create the absent file and answer its writable handle.</summary>
    internal abstract Stream Create();

    /// <summary>Close every handle this target holds on the file and remove it.</summary>
    internal abstract void Remove();
}

/// <summary>The target of a rollback run by a run unit that holds no handle on the file (a dead owner's, settled by the next
/// entry): its handle opened through the medium on demand and closed when the settle ends.</summary>
internal sealed class RestoreTarget(IHostMedium medium, string hostPath) : JournalTarget, IDisposable
{
    private Stream? _handle;

    internal override Stream? Writable() =>
        _handle ??= medium.Probe(hostPath) == FilePresence.Absent ? null : medium.OpenRestore(hostPath, create: false);

    internal override Stream Create() => _handle = medium.OpenRestore(hostPath, create: true);

    internal override void Remove()
    {
        _handle?.Dispose();
        _handle = null;
        medium.DeleteFile(hostPath);
    }

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }
}

/// <summary>
/// ⛔ THE SEQUENTIAL ORGANIZATION'S TRANSACTION JOURNAL (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2; kb/Work PB3340):
/// one run unit's open unit of work on one physical file F under an active APPLY COMMIT clause, kept in THREE transient files
/// in F's lock directory (<see cref="SidecarLockFiles.LockDirectoryOf"/>) so a COMMIT, a ROLLBACK, abnormal termination and a
/// KILLED or power-lost run unit all leave F at the state of the last COMMIT or at the state the COMMIT made durable, never a
/// third (§9.1.18.3 to §9.1.18.5, §14.9.7.4 GR3, §14.9.36.4 GR3): <c>txn-&lt;id&gt;.live</c>, the HOLDER, whose lock is the
/// owner's liveness; <c>txn-&lt;id&gt;.journal</c>, the RECORD, readable by every other run unit; and <c>txn-&lt;id&gt;.before</c>,
/// the BEFORE FILE, F's bytes copied whole at a capture. F itself stays byte-identical to a file written with no transactions.
/// <para>The protocol is the executable model's (<c>scripts/models/sequential_journal_model.py</c>, 2.2.2.12), operation for
/// operation, through one <see cref="IHostMedium"/>: the five write-ahead rules (2.2.2.5) — R1 the header synced before the
/// first change, R2 a rewritten record's image synced before the overwrite, R3 the committed length synced before the first
/// append and nothing for a later one, R4 the capture's order (the before file, its sync, the directory's, then the
/// <c>K</c> record and its sync, and only then the truncation or removal), R5 a refused sync of a handed-over record fatal —
/// COMMIT's three regions (2.2.2.6), the idempotent ROLLBACK (2.2.2.7) and the settle the next entry runs (2.2.2.8). Every
/// method that changes the medium is called with the ENTRY MUTEX held (<see cref="EntryMutex"/>) except the record appends of
/// R2 and R3, which only the owner makes.</para>
/// <para><b>The record's format</b> (little-endian): a 24-byte HEADER — magic <c>WOLJ</c>, format 1 (2), the START (1), zero (1),
/// the transaction id (8), zero (4), and the CRC-32C of the 20 bytes before it (4) — then RECORDS, each its kind, its payload and
/// the CRC-32C of the two (4): <c>L</c> the committed length (8); <c>R</c> the ordinal (8), the offset (8), the length (4) and the
/// bytes; <c>K</c> the capture's kind (1) and the before file's length (8); <c>D</c> the decision (1). A torn tail — a short
/// record, a failed CRC, an unknown kind — ENDS the records: its step had not changed F when the death fell. ⛔ A WIRE VALUE: two
/// runtime versions settle each other's journals.</para>
/// </summary>
internal sealed class SequentialJournal
{
    private const uint Magic = 0x4A4C4F57;   // "WOLJ" little-endian
    private const ushort Format = 1;
    internal const int HeaderLength = 24;
    private const int ChecksumBytes = 4;
    private const string NamePrefix = "txn-";
    private const string LiveSuffix = ".live";
    private const string RecordSuffix = ".journal";
    private const string BeforeSuffix = ".before";

    /// <summary>The bytes a capture copies at a time.</summary>
    private const int CopyChunk = 64 * 1024;

    private readonly IHostMedium _medium;
    private readonly string _directory;
    private readonly IDisposable _holder;
    private readonly Stream _record;
    private long _recordLength;
    private readonly HashSet<long> _journaled = [];

    private SequentialJournal(IHostMedium medium, string hostPath, string directory, ulong id, JournalStart start,
        IDisposable holder, Stream record, long recordLength)
    {
        _medium = medium;
        HostPath = hostPath;
        _directory = directory;
        Id = id;
        Start = start;
        Phase = start == JournalStart.Present ? JournalPhase.Base : JournalPhase.Replaced;
        _holder = holder;
        _record = record;
        _recordLength = recordLength;
    }

    /// <summary>The physical file.</summary>
    internal string HostPath { get; }

    /// <summary>The transaction id, the number its three files carry.</summary>
    internal ulong Id { get; }

    /// <summary>The header's START.</summary>
    internal JournalStart Start { get; }

    /// <summary>The phase (2.2.2.2).</summary>
    internal JournalPhase Phase { get; private set; }

    /// <summary>The committed length L, written at the transaction's first append (R3); null until it appends.</summary>
    internal long? CommittedLength { get; private set; }

    /// <summary>The decision this owner last made durable: <see cref="JournalDecision.Committed"/> once a COMMIT's decision is
    /// synced, so a retirement it could not finish is completed by the owner's next entry (2.2.2.6 region 2).</summary>
    internal JournalDecision Decision { get; private set; } = JournalDecision.Open;

    // ── Names ────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static string Name(ulong id, string suffix) => NamePrefix + id.ToString(CultureInfo.InvariantCulture) + suffix;

    private static string LivePath(string directory, ulong id) => Path.Combine(directory, Name(id, LiveSuffix));

    private static string RecordPath(string directory, ulong id) => Path.Combine(directory, Name(id, RecordSuffix));

    private static string BeforePath(string directory, ulong id) => Path.Combine(directory, Name(id, BeforeSuffix));

    /// <summary>The transaction ids <paramref name="directory"/> names: every <c>txn-&lt;id&gt;.*</c> file, live or left over.</summary>
    internal static IReadOnlyList<ulong> Ids(IHostMedium medium, string directory)
    {
        var ids = new SortedSet<ulong>();
        foreach (string name in medium.List(directory, NamePrefix))
        {
            int dot = name.IndexOf('.', NamePrefix.Length);
            if (dot > NamePrefix.Length
                && ulong.TryParse(name.AsSpan(NamePrefix.Length, dot - NamePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out ulong id))
                ids.Add(id);
        }
        return [.. ids];
    }

    // ── The record's format ──────────────────────────────────────────────────────────────────────────────────────────────

    private static byte[] Header(ulong id, JournalStart start)
    {
        var header = new byte[HeaderLength];
        BinaryPrimitives.WriteUInt32LittleEndian(header, Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), Format);
        header[6] = (byte)start;
        BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(8), id);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(20), PageHeader.Crc32C(header.AsSpan(0, 20)));
        return header;
    }

    /// <summary>A record: its kind, <paramref name="payload"/> and the CRC-32C of the two.</summary>
    private static byte[] Record(byte kind, ReadOnlySpan<byte> payload)
    {
        var record = new byte[1 + payload.Length + ChecksumBytes];
        record[0] = kind;
        payload.CopyTo(record.AsSpan(1));
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(1 + payload.Length), PageHeader.Crc32C(record.AsSpan(0, 1 + payload.Length)));
        return record;
    }

    private static byte[] LengthRecord(long length)
    {
        Span<byte> payload = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(payload, length);
        return Record((byte)'L', payload);
    }

    private static byte[] ImageRecord(long ordinal, long offset, ReadOnlySpan<byte> image)
    {
        var payload = new byte[20 + image.Length];
        BinaryPrimitives.WriteInt64LittleEndian(payload, ordinal);
        BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(8), offset);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), image.Length);
        image.CopyTo(payload.AsSpan(20));
        return Record((byte)'R', payload);
    }

    private static byte[] CaptureRecord(CaptureKind kind, long length)
    {
        Span<byte> payload = stackalloc byte[9];
        payload[0] = (byte)kind;
        BinaryPrimitives.WriteInt64LittleEndian(payload[1..], length);
        return Record((byte)'K', payload);
    }

    private static byte[] DecisionRecord(JournalDecision decision) => Record((byte)'D', [(byte)decision]);

    /// <summary>Read a record file's bytes: its header, or null when the header is short or torn (a header that never synced:
    /// litter with nothing to undo, R1), and every whole record before the first torn one.</summary>
    internal static JournalContent? Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic
            || BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]) != Format
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]) != PageHeader.Crc32C(bytes[..20]))
            return null;
        var start = (JournalStart)bytes[6];
        if (start is not (JournalStart.Present or JournalStart.Absent)) return null;
        ulong id = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);
        long? length = null;
        JournalCapture? capture = null;
        var decision = JournalDecision.Open;
        var images = new List<RewriteImage>();
        int at = HeaderLength;
        while (at < bytes.Length)
        {
            int payload = bytes[at] switch
            {
                (byte)'L' => 8,
                (byte)'K' => 9,
                (byte)'D' => 1,
                (byte)'R' when at + 21 <= bytes.Length => 20 + BinaryPrimitives.ReadInt32LittleEndian(bytes[(at + 17)..]),
                _ => -1,
            };
            if (payload < 0 || at + 1 + payload + ChecksumBytes > bytes.Length) break;   // a torn or unknown tail
            var whole = bytes.Slice(at, 1 + payload);
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[(at + 1 + payload)..]) != PageHeader.Crc32C(whole)) break;
            var body = whole[1..];
            switch (whole[0])
            {
                case (byte)'L': length = BinaryPrimitives.ReadInt64LittleEndian(body); break;
                case (byte)'K': capture = new JournalCapture((CaptureKind)body[0], BinaryPrimitives.ReadInt64LittleEndian(body[1..])); break;
                case (byte)'D': decision = (JournalDecision)body[0]; break;
                default:
                    images.Add(new RewriteImage(BinaryPrimitives.ReadInt64LittleEndian(body), BinaryPrimitives.ReadInt64LittleEndian(body[8..]),
                        body[20..].ToArray()));
                    break;
            }
            at += 1 + payload + ChecksumBytes;
        }
        return new JournalContent(id, start, length, images, capture, decision);
    }

    /// <summary>Read transaction <paramref name="id"/>'s record from the medium, or null when it has none (or its header is torn).</summary>
    internal static JournalContent? Read(IHostMedium medium, string directory, ulong id)
    {
        using var record = medium.OpenTransient(RecordPath(directory, id), writable: false);
        if (record is null) return null;
        var bytes = new byte[checked((int)medium.Length(record))];
        int n = medium.Read(record, 0, bytes);
        return Parse(bytes.AsSpan(0, n));
    }

    // ── The owner's statements ───────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>BEGIN a transaction on <paramref name="hostPath"/> (R1), with the entry mutex held and the entry step run: the id
    /// drawn (the lowest no file in the lock directory carries), the holder published, the record created and its header
    /// written and SYNCED, and the lock directory synced — all before the first byte of the file changes. A refusal removes what
    /// it made and throws what the host threw ('30' at the statement, the file unchanged).</summary>
    internal static SequentialJournal Begin(IHostMedium medium, string hostPath, JournalStart start)
    {
        string directory = SidecarLockFiles.LockDirectoryOf(hostPath);
        medium.CreateLockDirectory(directory);
        ulong id = 0;
        foreach (ulong taken in Ids(medium, directory))
        {
            if (taken != id) break;
            id++;
        }
        IDisposable? holder = null;
        Stream? record = null;
        try
        {
            holder = medium.PublishHolder(directory, Name(id, LiveSuffix))
                ?? throw new IOException($"The host cannot hold a transaction's lock in '{directory}'.");
            record = medium.CreateTransient(RecordPath(directory, id));
            byte[] header = Header(id, start);
            medium.Write(record, 0, header);
            medium.Sync(record);
            medium.SyncDirectory(directory);
            return new SequentialJournal(medium, hostPath, directory, id, start, holder, record, header.Length);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            record?.Dispose();
            try { medium.DeleteTransient(RecordPath(directory, id)); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }   // litter: a torn header, reaped by the next entry
            holder?.Dispose();
            throw;
        }
    }

    /// <summary>Append <paramref name="record"/> to the record file and sync it when <paramref name="sync"/>. The record length
    /// advances once the write is handed over, so a refused SYNC leaves the record claimed (it may be on the medium) and a
    /// refused WRITE leaves its place to the next record.</summary>
    private void Append(byte[] record, bool sync = true)
    {
        _medium.Write(_record, _recordLength, record);
        _recordLength += record.Length;
        if (sync) _medium.Sync(_record);
    }

    /// <summary>R3: before the transaction's FIRST append, write the committed length <paramref name="fileLength"/> — F's length
    /// read through the kept handle after the lock test found no other live transaction — and SYNC it; a later append, or one
    /// in the REPLACED phase, writes nothing. Throws on a refusal (R5: fatal; the append never ran, so F is right either way).</summary>
    internal void NoteAppend(long fileLength)
    {
        if (Phase != JournalPhase.Base || CommittedLength is not null) return;
        Append(LengthRecord(fileLength));
        CommittedLength = fileLength;
    }

    /// <summary>Does a REWRITE of the record <paramref name="ordinal"/> at <paramref name="offset"/> need its image journaled (R2):
    /// in the BASE phase, a record that is not the transaction's own append, not journaled yet?</summary>
    internal bool NeedsImage(long ordinal, long offset) =>
        Phase == JournalPhase.Base && (CommittedLength is not { } length || offset < length) && !_journaled.Contains(ordinal);

    /// <summary>R2: before a REWRITE overwrites the record <paramref name="ordinal"/> in place, write its first image
    /// <paramref name="image"/> (at byte <paramref name="offset"/>) and SYNC it; a record that needs none (<see cref="NeedsImage"/>)
    /// writes nothing. Throws on a refusal (R5: fatal; the overwrite never ran).</summary>
    internal void NoteRewrite(long ordinal, long offset, ReadOnlySpan<byte> image)
    {
        if (!NeedsImage(ordinal, offset)) return;
        byte[] record = ImageRecord(ordinal, offset, image);
        _medium.Write(_record, _recordLength, record);
        _recordLength += record.Length;
        _journaled.Add(ordinal);   // handed over: the image may be on the medium, so the ordinal stays claimed
        _medium.Sync(_record);
    }

    /// <summary>R4: CAPTURE the file before an OPEN OUTPUT truncates it or a DELETE FILE removes it, in the BASE phase, with the
    /// entry mutex held and no other connector's handle on the file: the before file created and F's bytes copied into it
    /// through <paramref name="file"/> (the kept handle), the before file synced, the lock directory synced, THEN the <c>K</c>
    /// record written and synced. Only after this returns may the caller truncate or remove F. The phase is REPLACED from here,
    /// also when the <c>K</c> record's sync is refused (R5: it may be on the medium, so nothing more is journaled and item 8's
    /// rollback follows); a refusal before the <c>K</c> is handed over deletes the before file (litter) and leaves the phase.</summary>
    internal void Capture(CaptureKind kind, Stream file)
    {
        if (Phase != JournalPhase.Base) return;
        string beforePath = BeforePath(_directory, Id);
        bool handed = false;
        try
        {
            long length;
            using (var before = _medium.CreateTransient(beforePath))
            {
                length = _medium.Length(file);
                var chunk = new byte[(int)Math.Min(CopyChunk, Math.Max(length, 1))];
                for (long at = 0; at < length;)
                {
                    int n = _medium.Read(file, at, chunk.AsSpan(0, (int)Math.Min(chunk.Length, length - at)));
                    if (n == 0) throw new IOException($"'{HostPath}' ended at byte {at} of the {length} its capture copies.");
                    _medium.Write(before, at, chunk.AsSpan(0, n));
                    at += n;
                }
                _medium.Sync(before);
            }
            _medium.SyncDirectory(_directory);
            byte[] record = CaptureRecord(kind, length);
            _medium.Write(_record, _recordLength, record);
            _recordLength += record.Length;
            handed = true;
            _medium.Sync(_record);
        }
        catch (Exception e) when (!handed && e is IOException or UnauthorizedAccessException)
        {
            try { _medium.DeleteTransient(beforePath); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }   // litter the next settle reaps
            throw;
        }
        finally
        {
            if (handed) Phase = JournalPhase.Replaced;
        }
    }

    /// <summary>The outcome of <see cref="Commit"/> (2.2.2.6).</summary>
    internal enum CommitOutcome
    {
        /// <summary>Region 2: the decision synced; committed, whatever became of the retirement.</summary>
        Committed,
        /// <summary>Region 1: refused before the decision was handed over; the transaction is intact, and the caller rolls it
        /// back (item 8's ROLLBACK, '30').</summary>
        Refused,
        /// <summary>Region 3, reversed: the decision's sync was refused and <c>D O</c> made "not committed" durable; the caller
        /// rolls the transaction back ('30').</summary>
        Reversed,
        /// <summary>Region 3, unknown: the reversal was refused too; the medium decides, the caller marks the file and touches it
        /// only through a ROLLBACK that first repeats the reversal (<see cref="Reverse"/>).</summary>
        Unknown,
    }

    /// <summary>COMMIT (2.2.2.6): (1) F synced through <paramref name="file"/> (null when F is absent: a DELETE FILE removed it) and
    /// its directory synced; (2) THE DECISION, <c>D C</c> written and synced; (3) on success the retirement, whose refusal the
    /// owner's next entry or the next run unit's completes. The regions are <see cref="CommitOutcome"/>'s.</summary>
    internal CommitOutcome Commit(Stream? file)
    {
        bool handed = false;
        try
        {
            if (file is not null) _medium.Sync(file);
            _medium.SyncDirectory(FileDirectory);
            byte[] record = DecisionRecord(JournalDecision.Committed);
            _medium.Write(_record, _recordLength, record);
            _recordLength += record.Length;
            handed = true;
            _medium.Sync(_record);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!handed) return CommitOutcome.Refused;
            return Reverse() ? CommitOutcome.Reversed : CommitOutcome.Unknown;
        }
        Decision = JournalDecision.Committed;
        TryFinishRetirement();
        return CommitOutcome.Committed;
    }

    /// <summary>Make "not committed" durable after an unacknowledged COMMIT (2.2.2.6 region 3): <c>D O</c> written and synced.
    /// False when the host refused it.</summary>
    internal bool Reverse()
    {
        try
        {
            Append(DecisionRecord(JournalDecision.Open));
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>The owner's ROLLBACK (2.2.2.7), with the entry mutex held: the one settle sequence, run on its own journal
    /// through <paramref name="target"/> (its kept handle). Throws when the host refuses a step: the rollback is then OWED, the
    /// holder still held, and the owner's next COMMIT, ROLLBACK or termination runs it again.</summary>
    internal void Rollback(JournalTarget target)
    {
        Settle(_medium, HostPath, _directory, Id, target, this);
        Decision = JournalDecision.RolledBack;
    }

    /// <summary>Complete a retirement a COMMIT's region 2 could not finish; false when the host refused it again.</summary>
    internal bool TryFinishRetirement()
    {
        try
        {
            Retire(_medium, _directory, Id, this);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>Is the retirement finished: the decision made and the holder given back?</summary>
    internal bool Retired { get; private set; }

    /// <summary>The directory the physical file is in, synced at COMMIT and ROLLBACK so a creation or removal is durable.</summary>
    private string FileDirectory => Path.GetDirectoryName(Path.GetFullPath(HostPath))!;

    // ── The settle, the entry step and the lock test ─────────────────────────────────────────────────────────────────────

    /// <summary>Finish transaction <paramref name="id"/> on <paramref name="hostPath"/> from what its record says, whoever runs it
    /// (2.2.2.7, 2.2.2.8), with the entry mutex held: no record or a torn header is litter, a decision is a retirement to
    /// complete, and an open transaction is rolled back through <paramref name="target"/> and then retired.
    /// <paramref name="owner"/> is the journal when its own owner settles it, null for a dead owner's.</summary>
    private static void Settle(IHostMedium medium, string hostPath, string directory, ulong id, JournalTarget target,
        SequentialJournal? owner)
    {
        var content = Read(medium, directory, id);
        if (content is { Decision: JournalDecision.Open })
            Restore(medium, hostPath, directory, id, content, target, owner);
        Retire(medium, directory, id, owner);
    }

    /// <summary>The rollback's steps (1) to (3) (2.2.2.7): a whole capture restores F from the before file, and a before file with
    /// none is litter; START P re-applies every image below F's length and truncates to L when the transaction appended, then
    /// syncs F and its directory; START A removes F and syncs its directory; then <c>D R</c> is written and synced, so the
    /// journal is never rolled back again. Every step is idempotent: a death between two leaves the journal reading open.</summary>
    private static void Restore(IHostMedium medium, string hostPath, string directory, ulong id, JournalContent content,
        JournalTarget target, SequentialJournal? owner)
    {
        string fileDirectory = Path.GetDirectoryName(Path.GetFullPath(hostPath))!;
        string beforePath = BeforePath(directory, id);
        if (content.Capture is { } capture)
        {
            using var before = medium.OpenTransient(beforePath, writable: false)
                ?? throw new InvalidDataException($"Journal {id} of '{hostPath}' names a capture whose before file is missing.");
            if (medium.Length(before) != capture.Length)
                throw new InvalidDataException($"Journal {id} of '{hostPath}' names a before file of {capture.Length} bytes; it holds {medium.Length(before)}.");
            var file = target.Writable() ?? target.Create();
            medium.SetLength(file, 0);
            var chunk = new byte[(int)Math.Min(CopyChunk, Math.Max(capture.Length, 1))];
            for (long at = 0; at < capture.Length;)
            {
                int n = medium.Read(before, at, chunk.AsSpan(0, (int)Math.Min(chunk.Length, capture.Length - at)));
                if (n == 0) throw new InvalidDataException($"The before file of journal {id} ended at byte {at}.");
                medium.Write(file, at, chunk.AsSpan(0, n));
                at += n;
            }
        }
        else medium.DeleteTransient(beforePath);   // a copy whose K never synced: F was never truncated (R4)
        if (content.Start == JournalStart.Present)
        {
            var file = target.Writable()
                ?? throw new InvalidDataException($"Journal {id} says '{hostPath}' was present at its begin, and it is absent.");
            long current = medium.Length(file);
            foreach (var image in content.Images)
                if (image.Offset < current) medium.Write(file, image.Offset, image.Bytes);
            if (content.CommittedLength is { } length)
            {
                // Only an appending transaction truncates, to its own L, and two transactions never append at once (2.2.2.3).
                if (current < length)
                    throw new InvalidDataException($"'{hostPath}' holds {current} bytes, fewer than journal {id}'s committed length {length}.");
                medium.SetLength(file, length);
            }
            medium.Sync(file);
            medium.SyncDirectory(fileDirectory);
        }
        else if (medium.Probe(hostPath) != FilePresence.Absent)
        {
            target.Remove();   // the state at the last COMMIT had no file (§14.9.27.4 GR17's creation undone)
            medium.SyncDirectory(fileDirectory);
        }
        var record = medium.OpenTransient(RecordPath(directory, id), writable: true)
            ?? throw new InvalidDataException($"Journal {id} of '{hostPath}' vanished during its rollback.");
        using (record)
        {
            // An owner writes at the end of what it handed over; a settler at the end of what the medium holds.
            long at = owner?._recordLength ?? medium.Length(record);
            byte[] decision = DecisionRecord(JournalDecision.RolledBack);
            medium.Write(record, at, decision);
            if (owner is not null) owner._recordLength = at + decision.Length;
            medium.Sync(record);
        }
    }

    /// <summary>The retirement (2.2.2.6 (3)): the before file, the record and a dead owner's holder deleted in that order and the
    /// lock directory synced, so the deletions are durable before a live owner gives its holder back (the holder's disposal
    /// retires its file and the directory with its last file); a holder a power loss resurrects with no record beside it is
    /// litter the next entry reaps. A directory a sibling's CLOSE retired between the deletions and the sync has no entries left
    /// to make durable.</summary>
    private static void Retire(IHostMedium medium, string directory, ulong id, SequentialJournal? owner)
    {
        medium.DeleteTransient(BeforePath(directory, id));
        owner?._record.Dispose();
        medium.DeleteTransient(RecordPath(directory, id));
        if (owner is null) medium.DeleteTransient(LivePath(directory, id));
        try { medium.SyncDirectory(directory); }
        catch (DirectoryNotFoundException) { }
        if (owner is null) medium.RetireDirectory(directory);
        else
        {
            owner._holder.Dispose();
            owner.Retired = true;
        }
    }

    /// <summary>⛔ THE ENTRY STEP (2.2.2.8), with the entry mutex held: list the file's transactions and, for each that is not one
    /// of <paramref name="own"/>, probe its holder — held, a live transaction the statement's lock test then meets (answered);
    /// free, SETTLED from its record through <paramref name="target"/> (a dead owner's rollback, or the retirement it did not
    /// finish). An own journal whose COMMIT could not finish its retirement is finished here. A holder or a directory this
    /// process may not observe is a live transaction with no readable content: the lock test refuses, loudly (2.2.2.7).</summary>
    internal static IReadOnlyList<OtherTransaction> Enter(IHostMedium medium, string hostPath, IReadOnlyCollection<SequentialJournal> own,
        Func<JournalTarget> target)
    {
        string directory = SidecarLockFiles.LockDirectoryOf(hostPath);
        var live = new List<OtherTransaction>();
        IReadOnlyList<ulong> ids;
        try { ids = Ids(medium, directory); }
        catch (UnauthorizedAccessException) { return [new OtherTransaction(ulong.MaxValue, null)]; }
        foreach (ulong id in ids)
        {
            var mine = own.FirstOrDefault(j => j.Id == id && !j.Retired);
            if (mine is not null)
            {
                if (mine.Decision != JournalDecision.Open) mine.TryFinishRetirement();
                continue;
            }
            switch (medium.ProbeHolder(LivePath(directory, id)))
            {
                case HostFile.LockFileState.Live:
                    live.Add(new OtherTransaction(id, ReadOrNull(medium, directory, id)));
                    break;
                case HostFile.LockFileState.Unobservable:
                    live.Add(new OtherTransaction(id, null));
                    break;
                default:
                    var restore = target();
                    try { Settle(medium, hostPath, directory, id, restore, owner: null); }
                    finally { (restore as IDisposable)?.Dispose(); }
                    break;
            }
        }
        return live;
    }

    /// <summary>The lock test's view of the OTHER live transactions on the file, with no mutex held (2.2.2.8: the entry of a WRITE,
    /// READ or REWRITE under a posture that admits another writer): the live ones answered, and whether any is DEAD — which the
    /// caller then settles under the mutex (<see cref="Enter"/>) before the statement proceeds.</summary>
    internal static IReadOnlyList<OtherTransaction> Observe(IHostMedium medium, string hostPath, IReadOnlyCollection<SequentialJournal> own,
        out bool anyDead)
    {
        anyDead = false;
        string directory = SidecarLockFiles.LockDirectoryOf(hostPath);
        var live = new List<OtherTransaction>();
        IReadOnlyList<ulong> ids;
        try { ids = Ids(medium, directory); }
        catch (UnauthorizedAccessException) { return [new OtherTransaction(ulong.MaxValue, null)]; }
        foreach (ulong id in ids)
        {
            if (own.Any(j => j.Id == id && !j.Retired)) continue;
            switch (medium.ProbeHolder(LivePath(directory, id)))
            {
                case HostFile.LockFileState.Live: live.Add(new OtherTransaction(id, ReadOrNull(medium, directory, id))); break;
                case HostFile.LockFileState.Unobservable: live.Add(new OtherTransaction(id, null)); break;
                default: anyDead = true; break;
            }
        }
        return live;
    }

    private static JournalContent? ReadOrNull(IHostMedium medium, string directory, ulong id)
    {
        try { return Read(medium, directory, id); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
    }

    /// <summary>⛔ THE JOURNAL ARM OF THE LOCK TEST (2.2.2.3): what <paramref name="access"/> meets among the OTHER live
    /// transactions <paramref name="others"/> — a WRITE meets any one ('51', the end of the file held by every live transaction);
    /// a READ or REWRITE of the record <paramref name="ordinal"/> at <paramref name="offset"/> meets one that holds it ('51');
    /// an OPEN OUTPUT or a DELETE FILE meets any one ('61'), a writable OPEN only a REPLACED one ('61'). A transaction whose
    /// content cannot be read (being born, or unobservable) holds everything.</summary>
    internal static JournalConflict Test(IReadOnlyList<OtherTransaction> others, JournalAccess access, long ordinal = 0, long offset = 0)
    {
        foreach (var other in others)
        {
            switch (access)
            {
                case JournalAccess.Exclusive: return JournalConflict.FileLocked;
                case JournalAccess.Write: return JournalConflict.RecordLocked;
                case JournalAccess.Open when other.Content is not { Replaced: false }: return JournalConflict.FileLocked;
                case JournalAccess.Record when other.Content is not { } content || content.Holds(ordinal, offset):
                    return JournalConflict.RecordLocked;
            }
        }
        return JournalConflict.None;
    }

    // ── The entry mutex ──────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ENTRY MUTEX of a physical file (2.2.2.4; <see cref="HostRegionLocks.EntryMutexByte"/>), held exclusively
    /// through a lock file in its lock directory on every host, waiting for another run unit's hold. Null when the host cannot
    /// hold it there (a directory this process may not write): the caller then has no journal to begin either, and a covered
    /// change answers '30' (R1 refused).</summary>
    internal static IDisposable? EntryMutex(string hostPath)
    {
        var endpoint = SidecarRegionLocks.WithoutHandle(hostPath);
        if (endpoint.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true) == RegionLockResult.Ok) return endpoint;
        endpoint.Dispose();
        return null;
    }
}
