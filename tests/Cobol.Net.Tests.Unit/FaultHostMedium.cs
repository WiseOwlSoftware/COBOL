// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FAULT-INJECTING HOST MEDIUM of docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.12 (kb/Work PB3340): the
/// <see cref="IHostMedium"/> a run unit's sequential files and their transaction journals are written through, over REAL files,
/// which counts every operation that changes the medium — a create or open of a file it may write, a write, a sync
/// (<see cref="FileStream.Flush(bool)"/> with <c>true</c>), a truncation, a removal, a directory sync, a holder published — and
/// at the n-th one REFUSES it (an <see cref="IOException"/>, the process continues), DIES (the run unit's handles and locks go
/// as a killed process's do, its unsynced writes stay in the operating environment, and nothing it runs afterwards touches the
/// medium) or LOSES POWER (as a death, and then every file keeps only what was synced: its last synced bytes, and its name only
/// when a directory sync made the creation or the removal durable). The model (<c>scripts/models/sequential_journal_model.py</c>)
/// holds the protocol over tokens; this re-runs the same rows over bytes, framings and checksums.
/// </summary>
internal sealed class FaultHostMedium : IHostMedium
{
    /// <summary>What happens at the target operation.</summary>
    internal enum Fault { None, Refuse, Die, Power, PowerKeepingData }

    /// <summary>The run unit died at an operation: thrown from that operation and from every one after it, so nothing a dead
    /// run unit's code runs on the way out (a cleanup in a <c>finally</c>) reaches the medium.</summary>
    internal sealed class Death : Exception
    {
        internal Death() : base("the run unit died at a medium operation (the fault-injecting test medium)") { }
    }

    private readonly Fault _fault;
    private readonly int _target;
    private int _operations;
    private bool _dead;
    private readonly List<FileStream> _streams = [];
    private readonly List<SidecarLockFiles.LockFileHolder> _holders = [];
    private readonly List<RegionLocks> _mutexes = [];
    private readonly Dictionary<string, Durable> _durable = new(HostFile.PhysicalFileComparer);

    /// <summary>What the medium holds of one file whatever becomes of the operating environment's cache.</summary>
    private sealed class Durable
    {
        internal bool Exists;
        internal byte[] Bytes = [];
    }

    internal FaultHostMedium(Fault fault = Fault.None, int target = 0) => (_fault, _target) = (fault, target);

    /// <summary>Run before every counted operation with its log entry (<c>kind name</c>), before the fault is considered: the
    /// seam a case uses to run another run unit's statement at an exact point of this one's (2.2.2.12's interleavings over
    /// bytes), or to REFUSE every operation of a kind for as long as it chooses (an <see cref="IOException"/> it throws is the
    /// medium's refusal: a ROLLBACK refused, and its retry refused again).</summary>
    internal Action<string>? BeforeOperation { get; init; }

    /// <summary>Run whenever the run unit asks for the entry mutex of a physical file, before it holds it.</summary>
    internal Action? OnEntryMutex { get; init; }

    /// <summary>The handles opened for a purpose (<see cref="OpenAuxiliary"/>) that are still open: a statement that opened one
    /// closes it, whatever became of the statement.</summary>
    internal int LiveAuxiliaryHandles => _auxiliary.Count(s => !s.Closed);

    private readonly List<FaultFileStream> _auxiliary = [];

    /// <summary>The operations counted so far.</summary>
    internal int Operations => _operations;

    /// <summary>Whether the fault fired.</summary>
    internal bool Fired { get; private set; }

    /// <summary>The lock directory refuses its listing to this process (the host a directory of mode 000 hides it on): every
    /// <see cref="ListNames"/> raises <see cref="UnauthorizedAccessException"/>.</summary>
    internal bool RefusesListing { get; init; }

    /// <summary>Every counted operation, in order: its kind and its file's name.</summary>
    internal List<string> Log { get; } = [];

    /// <summary>Count one operation on <paramref name="path"/>, and fire the fault when it is the target.</summary>
    internal void Operation(string kind, string path)
    {
        if (_dead) throw new Death();
        BeforeOperation?.Invoke($"{kind} {Path.GetFileName(path)}");
        _operations++;
        Log.Add($"{kind} {Path.GetFileName(path)}");
        if (Fired || _operations != _target) return;
        Fired = true;
        switch (_fault)
        {
            case Fault.Refuse: throw new IOException($"the test medium refused operation {_operations} ({kind} {Path.GetFileName(path)})");
            case Fault.Die: Kill(); throw new Death();
            case Fault.Power: Kill(); LosePower(keepData: false); throw new Death();
            case Fault.PowerKeepingData: Kill(); LosePower(keepData: true); throw new Death();
        }
    }

    /// <summary>The run unit's handles and locks go as a killed process's do: every stream closed, every holder's lock dropped
    /// with its name left behind, every entry mutex given up.</summary>
    internal void Kill()
    {
        _dead = true;
        foreach (var holder in _holders) holder.DropAsADeathDoes();
        foreach (var stream in _streams) stream.Dispose();
        foreach (var mutex in _mutexes) mutex.Dispose();
        foreach (var column in _columns) column.Dispose();
    }

    /// <summary>The Table 19 column locks established while <see cref="OwnColumnLocks"/> was installed.</summary>
    private readonly List<IDisposable> _columns = [];

    /// <summary>⛔ A KILL ALSO DROPS THE RUN UNIT'S TABLE 19 COLUMN LOCKS, as a death closes their descriptors. The column lock is
    /// not the medium's (<see cref="RunUnitFileLock"/> establishes it through <see cref="IColumnLockHost"/>), so a run unit that
    /// is to die holds its column locks through this medium while the returned scope is installed
    /// (<see cref="RunUnitFileLock.HostObserver"/>, the pattern of <c>ColumnRequestLog</c>) and <see cref="Kill"/> gives them
    /// back. Without it, a host whose column lock is a lock of its own (an open file description's on Linux) kept the dead run
    /// unit's column, and the next run unit's OPEN met it as a live holder's ('61'), which no death leaves behind. A host whose
    /// share mode is the file lock (Windows, under NO OTHER) takes no column, and the stream <see cref="Kill"/> closes is it.</summary>
    internal IDisposable OwnColumnLocks()
    {
        RunUnitFileLock.HostObserver.Value = inner => new OwningHost(inner, this);
        return new ColumnScope();
    }

    private sealed class ColumnScope : IDisposable
    {
        public void Dispose() => RunUnitFileLock.HostObserver.Value = null;
    }

    private sealed class OwningHost(IColumnLockHost inner, FaultHostMedium medium) : IColumnLockHost
    {
        public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own, bool owesWrites,
            IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
        {
            var outcome = inner.Establish(hostPath, own, owesWrites, refused, supersedes, out hold);
            if (hold is not null && !medium._dead) medium._columns.Add(hold);
            return outcome;
        }
    }

    /// <summary>Every file the medium touched keeps only what was synced (the bytes of its last sync; its name only when a
    /// directory sync made it durable) — or, with <paramref name="keepData"/>, the adversarial subset for a write-ahead rule:
    /// every PHYSICAL file keeps everything the operating environment was handed (its cache reached the medium), while the
    /// journal's own files in the lock directory keep only what was synced.</summary>
    private void LosePower(bool keepData)
    {
        foreach (var (path, durable) in _durable)
        {
            if (keepData && !InLockDirectory(path)) continue;
            if (durable.Exists)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, durable.Bytes);
            }
            else if (File.Exists(path)) File.Delete(path);
        }
    }

    private static bool InLockDirectory(string path) =>
        Path.GetFileName(Path.GetDirectoryName(path))?.StartsWith(".wolk-", StringComparison.Ordinal) == true;

    /// <summary>Begin tracking <paramref name="path"/>: what is on the disk now is what the medium holds of it.</summary>
    private void Track(string path)
    {
        if (_durable.ContainsKey(path)) return;
        bool exists = File.Exists(path);
        _durable[path] = new Durable { Exists = exists, Bytes = exists ? ReadShared(path) : [] };
    }

    private static byte[] ReadShared(string path)
    {
        using var read = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var copy = new MemoryStream();
        read.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>A sync of <paramref name="path"/>: its <paramref name="bytes"/> now are what the medium holds.</summary>
    internal void Synced(string path, byte[] bytes)
    {
        Track(path);
        _durable[path].Bytes = bytes;
    }

    private FileStream Open(string kind, string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
    {
        Track(path);
        if (access != FileAccess.Read) Operation(kind, path);
        else if (_dead) throw new Death();
        var stream = new FaultFileStream(this, path, mode, access, share, options);
        _streams.Add(stream);
        return stream;
    }

    public FileStream OpenConnectorStream(string hostPath, FileMode mode, FileAccess access, FileShare share, FileOptions options) =>
        Open("open", hostPath, mode, access, share, options);

    public Stream OpenConnectorWriteStream(string hostPath, FileMode mode, FileAccess access, FileShare share) =>
        HostFile.WriteStreamOf(mode, access, share, (hostMode, options) => Open("open", hostPath, hostMode, access, share, options));

    public FileStream OpenAuxiliary(string hostPath, FileMode mode, FileAccess access)
    {
        var stream = Open("aux", hostPath, mode, access, FileShare.ReadWrite, FileOptions.None);
        _auxiliary.Add((FaultFileStream)stream);
        return stream;
    }

    public FileStream OpenTransient(string path, FileMode mode, FileAccess access) =>
        Open("transient", path, mode, access, FileShare.ReadWrite | FileShare.Delete, FileOptions.None);

    public void DeleteFile(string hostPath)
    {
        Track(hostPath);
        Operation("delete", hostPath);
        File.Delete(hostPath);
    }

    public void SyncDirectory(string directory)
    {
        Operation("syncdir", directory);
        foreach (var (path, durable) in _durable)
            if (HostFile.PhysicalFileComparer.Equals(Path.GetDirectoryName(path), directory)) durable.Exists = File.Exists(path);
    }

    public IReadOnlyList<string> ListNames(string directory, string prefix)
    {
        if (_dead) throw new Death();
        if (RefusesListing) throw new UnauthorizedAccessException($"the test medium refuses the listing of {directory}");
        return HostMedium.Production.ListNames(directory, prefix);
    }

    public SidecarLockFiles.LockFileHolder? PublishHolder(string directory, string name)
    {
        string path = Path.Combine(directory, name);
        Track(path);
        Operation("publish", path);
        var holder = HostMedium.Production.PublishHolder(directory, name);
        if (holder is not null) _holders.Add(holder);
        return holder;
    }

    public HostFile.LockFileState ProbeHolder(string path)
    {
        if (_dead) throw new Death();
        return HostMedium.Production.ProbeHolder(path);
    }

    public void RetireName(string path)
    {
        Track(path);
        Operation("retire", path);
        HostMedium.Production.RetireName(path);
    }

    public void RetireDirectory(string directory)
    {
        if (_dead) throw new Death();
        HostMedium.Production.RetireDirectory(directory);
    }

    public RegionLocks EntryMutex(string hostPath)
    {
        if (_dead) throw new Death();
        OnEntryMutex?.Invoke();
        var mutex = HostMedium.Production.EntryMutex(hostPath);
        _mutexes.Add(mutex);
        return mutex;
    }

    /// <summary>A real file stream whose every write, sync and truncation is one of the medium's operations.</summary>
    private sealed class FaultFileStream(FaultHostMedium medium, string hostPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        : FileStream(hostPath, mode, access, share, bufferSize: 1, options)
    {
        private readonly string _path = hostPath;

        /// <summary>Set while an overridden member runs, so the base's own calls to another overridden member count once.</summary>
        private bool _inside;

        /// <summary>Whether the handle was closed (disposed, by its owner or by a kill).</summary>
        internal bool Closed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Closed = true;
            base.Dispose(disposing);
        }

        private void Count(string kind)
        {
            if (!_inside) medium.Operation(kind, _path);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Count("write");
            bool outer = !_inside;
            _inside = true;
            try { base.Write(buffer, offset, count); }
            finally { if (outer) _inside = false; }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Count("write");
            bool outer = !_inside;
            _inside = true;
            try { base.Write(buffer); }
            finally { if (outer) _inside = false; }
        }

        public override void WriteByte(byte value) => Write([value], 0, 1);

        public override void Flush(bool flushToDisk)
        {
            if (!flushToDisk) { base.Flush(false); return; }
            Count("sync");
            base.Flush(true);
            medium.Synced(_path, Contents());
        }

        /// <summary>The file's bytes as this handle reads them (the file may be held with a share mode that refuses any other
        /// reader); a write-only handle cannot read, and its file keeps what it held when the medium first met it.</summary>
        private byte[] Contents()
        {
            if (!CanRead) return [];
            long position = Position;
            var bytes = new byte[Length];
            Position = 0;
            ReadExactly(bytes);
            Position = position;
            return bytes;
        }

        public override void SetLength(long value)
        {
            Count("setlength");
            base.SetLength(value);
        }
    }
}
