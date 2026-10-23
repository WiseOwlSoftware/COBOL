// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;

namespace CobolNet.Tests.Unit.Journal;

/// <summary>How a <see cref="FaultingHostMedium"/> answers the operation it was told to fault.</summary>
internal enum MediumFault
{
    /// <summary>The host refuses the operation (an <see cref="IOException"/>); the run unit continues.</summary>
    Refuse,
    /// <summary>The run unit is KILLED at the operation: its handles and locks go, its unsynced writes stay in the operating
    /// environment's cache.</summary>
    Die,
    /// <summary>The power fails at the operation, and every write and directory entry no sync made durable is LOST.</summary>
    PowerLossLosingAll,
    /// <summary>The power fails at the operation, and every unsynced write and directory entry SURVIVES (the other end of
    /// "every file keeps a prefix of its unsynced writes").</summary>
    PowerLossKeepingAll,
    /// <summary>The power fails, the data file keeps its unsynced writes and the journal's files lose theirs: the case each
    /// write-ahead rule exists for (a change on the medium whose record is not).</summary>
    PowerLossKeepingTheFileLosingTheJournal,
    /// <summary>The power fails, the journal's files keep their unsynced writes and the data file loses its.</summary>
    PowerLossKeepingTheJournalLosingTheFile,
}

/// <summary>The run unit's end at a fault: the death or the power loss <see cref="FaultingHostMedium"/> injected.</summary>
internal sealed class MediumDeathException(MediumFault fault) : Exception($"the run unit ended at a {fault}")
{
    public MediumFault Fault { get; } = fault;
}

/// <summary>
/// ⛔ THE DETERMINISTIC FAULT-INJECTING HOST MEDIUM (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.12; kb/Work PB3340): the
/// real operating environment (<see cref="HostMedium"/>) under a log of every operation that changes or syncs the medium, which
/// REFUSES the n-th, KILLS the run unit at it, or LOSES POWER at it — the three modes of the executable model
/// <c>scripts/models/sequential_journal_model.py</c>, re-run over real files, framings and checksums. Reads are never faulted
/// (the model's are not).
/// <para>A kill closes every handle and holder this medium opened WITHOUT retiring anything (the kernel drops a dead
/// process's locks and leaves its files). A power loss is a kill followed by the medium's own restoration of what the
/// medium durably held, file by file: a file whose unsynced state is LOST is present only if a directory sync recorded it
/// present, with its last synced content; a file whose unsynced state is KEPT stays as the operating environment holds it.
/// Which files keep it is the fault's (<see cref="MediumFault"/>): none, all, the data file only, or the journal only.</para>
/// </summary>
internal sealed class FaultingHostMedium(int faultAt = -1, MediumFault fault = MediumFault.Refuse) : IHostMedium
{
    private readonly IHostMedium _host = HostMedium.Instance;
    private readonly List<IDisposable> _opened = [];
    private readonly Dictionary<Stream, string> _paths = [];

    /// <summary>What the medium durably holds of one path: whether its directory entry is durable, and its synced content.</summary>
    private sealed class Durable
    {
        public bool EntryDurable;
        public bool Present;
        public byte[]? Content;
    }

    private readonly Dictionary<string, Durable> _durable = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The operations made so far, in order.</summary>
    public List<string> Log { get; } = [];

    /// <summary>How many faultable operations ran.</summary>
    public int Operations { get; private set; }

    /// <summary>Did the fault fire?</summary>
    public bool Fired { get; private set; }

    /// <summary>Record that <paramref name="path"/> already exists durably, with its current content (a file the scenario set up).</summary>
    public void TreatAsDurable(string path)
    {
        var d = Track(path);
        d.EntryDurable = true;
        d.Present = File.Exists(path);
        d.Content = d.Present ? ReadShared(path) : null;
    }

    private Durable Track(string path)
    {
        path = Path.GetFullPath(path);
        if (!_durable.TryGetValue(path, out var d)) _durable[path] = d = new Durable();
        return d;
    }

    private void Op(string what)
    {
        Log.Add(what);
        int n = Operations++;
        if (n != faultAt) return;
        Fired = true;
        switch (fault)
        {
            case MediumFault.Refuse: throw new IOException($"refused by the host: {what}");
            case MediumFault.Die:
                Kill();
                throw new MediumDeathException(fault);
            default:
                Kill();
                LosePower(fault switch
                {
                    MediumFault.PowerLossKeepingAll => _ => true,
                    MediumFault.PowerLossKeepingTheFileLosingTheJournal => path => !InLockDirectory(path),
                    MediumFault.PowerLossKeepingTheJournalLosingTheFile => InLockDirectory,
                    _ => _ => false,
                });
                throw new MediumDeathException(fault);
        }
    }

    /// <summary>Close every handle and holder this medium opened, retiring nothing.</summary>
    public void Kill()
    {
        foreach (var d in _opened) d.Dispose();
        _opened.Clear();
    }

    /// <summary>The medium after a power loss: a path <paramref name="keep"/> keeps holds what the operating environment held
    /// (its unsynced writes and directory entry survive); any other holds what a sync made durable — present only when its
    /// entry was synced present, with its last synced content.</summary>
    private void LosePower(Func<string, bool> keep)
    {
        foreach (var (path, d) in _durable)
        {
            bool present = File.Exists(path);
            bool kept = keep(path);
            bool survives = kept ? present : d.EntryDurable && d.Present;
            if (!survives)
            {
                if (present) File.Delete(path);
                continue;
            }
            if (!kept) File.WriteAllBytes(path, d.Content ?? []);
        }
    }

    /// <summary>Is <paramref name="path"/> one of the journal's transient files (in a lock directory) rather than a data file?</summary>
    private static bool InLockDirectory(string path) =>
        Path.GetFileName(Path.GetDirectoryName(path))?.StartsWith(".wolk-", StringComparison.Ordinal) == true;

    private T Opened<T>(T d) where T : IDisposable
    {
        _opened.Add(d);
        return d;
    }

    public FilePresence Probe(string path) => _host.Probe(path);

    public void CreateLockDirectory(string directory)
    {
        Op($"mkdir {Path.GetFileName(directory)}");
        _host.CreateLockDirectory(directory);
    }

    public IReadOnlyList<string> List(string directory, string prefix) => _host.List(directory, prefix);

    public void SyncDirectory(string directory)
    {
        Op($"syncdir {Path.GetFileName(directory)}");
        _host.SyncDirectory(directory);
        string full = Path.GetFullPath(directory);
        foreach (var (path, d) in _durable)
        {
            if (!string.Equals(Path.GetDirectoryName(path), full, StringComparison.OrdinalIgnoreCase)) continue;
            d.EntryDurable = true;
            d.Present = File.Exists(path);
            if (!d.Present) d.Content = null;
        }
    }

    public IDisposable? PublishHolder(string directory, string name)
    {
        Op($"publish {name}");
        string path = Path.Combine(directory, name);
        Track(path);
        string pending = Path.Combine(directory, "pending-" + Guid.NewGuid().ToString("N"));
        var stream = Opened(HostFile.OpenLockHolder(pending));
        File.Move(pending, path);
        return new Holder(this, stream, path, directory);
    }

    /// <summary>A published holder: disposing it retires its file as <see cref="SidecarLockFiles"/>' holder does; a kill only
    /// closes its stream.</summary>
    private sealed class Holder(FaultingHostMedium medium, FileStream stream, string path, string directory) : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (_done) return;
            _done = true;
            medium._opened.Remove(stream);
            stream.Dispose();
            // The production holder's retirement swallows a refusal (SidecarLockFiles.LockFileHolder): a name left behind is a
            // dead holder the next entry reaps. A death or a power loss is no refusal and ends the run unit.
            try { medium.DeleteTransient(path); }
            catch (IOException) { return; }
            medium._host.RetireDirectory(directory);
        }
    }

    public HostFile.LockFileState ProbeHolder(string path) => _host.ProbeHolder(path);

    public Stream CreateTransient(string path)
    {
        Op($"create {Path.GetFileName(path)}");
        var s = Opened(_host.CreateTransient(path));
        Track(path);
        _paths[s] = Path.GetFullPath(path);
        return s;
    }

    public Stream? OpenTransient(string path, bool writable)
    {
        var s = _host.OpenTransient(path, writable);
        if (s is null) return null;
        _paths[s] = Path.GetFullPath(path);
        return Opened(s);
    }

    public Stream OpenRestore(string hostPath, bool create)
    {
        if (create) Op($"create {Path.GetFileName(hostPath)}");
        var s = Opened(_host.OpenRestore(hostPath, create));
        Track(hostPath);
        _paths[s] = Path.GetFullPath(hostPath);
        return s;
    }

    /// <summary>A writable handle on a data file the scenario holds as the connector's kept handle (not faulted: the OPEN is the
    /// scenario's, before the protocol under test).</summary>
    public Stream OpenData(string hostPath)
    {
        var s = Opened(_host.OpenRestore(hostPath, create: !File.Exists(hostPath)));
        Track(hostPath);
        _paths[s] = Path.GetFullPath(hostPath);
        return s;
    }

    public long Length(Stream file) => _host.Length(file);

    public int Read(Stream file, long offset, Span<byte> destination) => _host.Read(file, offset, destination);

    public void Write(Stream file, long offset, ReadOnlySpan<byte> source)
    {
        // A COMMIT's decision: the six-byte record D C (SequentialJournal's format) — the operation before which no COMMIT of
        // this run has been handed to the operating environment.
        if (source.Length == 6 && source[0] == (byte)'D' && source[1] == (byte)'C' && NameOf(file).EndsWith(".journal", StringComparison.Ordinal))
            Decisions.Add(Operations);
        Op($"write {NameOf(file)}@{offset}+{source.Length}");
        _host.Write(file, offset, source);
    }

    /// <summary>The operation numbers at which a COMMIT's decision record was written.</summary>
    public List<int> Decisions { get; } = [];

    public void SetLength(Stream file, long length)
    {
        Op($"setlength {NameOf(file)}={length}");
        _host.SetLength(file, length);
    }

    public void Sync(Stream file)
    {
        Op($"sync {NameOf(file)}");
        _host.Sync(file);
        if (_paths.TryGetValue(file, out string? path))
        {
            var d = Track(path);
            d.Content = ReadShared(path);
        }
    }

    public void DeleteTransient(string path)
    {
        Op($"delete {Path.GetFileName(path)}");
        _host.DeleteTransient(path);
    }

    public void DeleteFile(string hostPath)
    {
        Op($"delete {Path.GetFileName(hostPath)}");
        _host.DeleteFile(hostPath);
    }

    public void RetireDirectory(string directory) => _host.RetireDirectory(directory);

    /// <summary>A file's bytes, read beside every handle the protocol holds on it (each shares read, write and delete).</summary>
    internal static byte[] ReadShared(string path)
    {
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[s.Length];
        s.ReadExactly(bytes);
        return bytes;
    }

    private string NameOf(Stream file) => _paths.TryGetValue(file, out string? p) ? Path.GetFileName(p) : "?";
}
