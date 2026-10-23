// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST MEDIUM OF THE SEQUENTIAL JOURNAL (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.12; kb/Work PB3340): every
/// operation the journal's protocol makes on the operating environment — the lock directory, its holder, the transient
/// record and before files, the restore of the physical file, every sync and every directory sync — as ONE seam, so a test
/// medium can log each operation, REFUSE the n-th, DIE at it, or lose any subset of the writes no sync made durable, and
/// re-run the executable model's rows (<c>scripts/models/sequential_journal_model.py</c>) over real files, framings and
/// checksums. The production medium is <see cref="HostMedium"/>, whose every role is a <see cref="HostFile"/> role, as
/// <c>Store.RunUnitPageMedium</c> is the production <c>Store.IPageMedium</c>; a refusal throws what the host throws
/// (<see cref="IOException"/>, <see cref="UnauthorizedAccessException"/>), and the caller maps it.
/// <para>Positions are explicit: every read and write names its offset, so the operation log a test keeps is the whole
/// truth of what reached the operating environment, and nothing depends on a stream's position.</para>
/// </summary>
internal interface IHostMedium
{
    /// <summary>Is the physical file at <paramref name="path"/> present (<see cref="HostFile.Probe"/>)?</summary>
    FilePresence Probe(string path);

    /// <summary>Create the lock directory (<see cref="HostFile.CreateLockDirectory"/>); one that exists is left as it is.</summary>
    void CreateLockDirectory(string directory);

    /// <summary>The names in <paramref name="directory"/> that begin with <paramref name="prefix"/>; none when the directory does
    /// not exist. A directory this process may not list throws <see cref="UnauthorizedAccessException"/>.</summary>
    IReadOnlyList<string> List(string directory, string prefix);

    /// <summary>Make <paramref name="directory"/>'s entries durable (<see cref="HostFile.SyncDirectory"/>).</summary>
    void SyncDirectory(string directory);

    /// <summary>Publish and hold the lock file <paramref name="name"/> in <paramref name="directory"/>
    /// (<see cref="SidecarLockFiles.Publish"/>); null when the host cannot hold a lock there. Disposing the holder retires its
    /// file.</summary>
    IDisposable? PublishHolder(string directory, string name);

    /// <summary>Is the holder of the lock file <paramref name="path"/> alive (<see cref="HostFile.ProbeLockFile"/>)?</summary>
    HostFile.LockFileState ProbeHolder(string path);

    /// <summary>Create the transient file <paramref name="path"/> (<see cref="HostFile.OpenJournalFile"/>, a new name).</summary>
    Stream CreateTransient(string path);

    /// <summary>Open the existing transient file <paramref name="path"/>, or null when it is not there.</summary>
    Stream? OpenTransient(string path, bool writable);

    /// <summary>A writable handle on the physical file <paramref name="hostPath"/> for a restore
    /// (<see cref="HostFile.OpenRestoreHandle"/>): the existing file, or a new one when <paramref name="create"/>.</summary>
    Stream OpenRestore(string hostPath, bool create);

    /// <summary>The length of <paramref name="file"/> in bytes.</summary>
    long Length(Stream file);

    /// <summary>Read up to <paramref name="destination"/>'s length from <paramref name="file"/> at <paramref name="offset"/>;
    /// the count read (fewer only at the end of the file).</summary>
    int Read(Stream file, long offset, Span<byte> destination);

    /// <summary>Write <paramref name="source"/> to <paramref name="file"/> at <paramref name="offset"/>.</summary>
    void Write(Stream file, long offset, ReadOnlySpan<byte> source);

    /// <summary>Truncate or extend <paramref name="file"/> to <paramref name="length"/> bytes.</summary>
    void SetLength(Stream file, long length);

    /// <summary>Make every byte written to <paramref name="file"/> durable (<c>FlushFileBuffers</c> / <c>fsync</c>).</summary>
    void Sync(Stream file);

    /// <summary>Remove the transient file <paramref name="path"/> so that no tester meets it DELETE-PENDING
    /// (<see cref="SidecarLockFiles.Retire"/>'s rule); a file that is not there is no error.</summary>
    void DeleteTransient(string path);

    /// <summary>Remove the physical file <paramref name="hostPath"/>; a file that is not there is no error.</summary>
    void DeleteFile(string hostPath);

    /// <summary>Retire the lock directory when it is empty (<see cref="SidecarLockFiles.RetireDirectory"/>).</summary>
    void RetireDirectory(string directory);
}

/// <summary>The production <see cref="IHostMedium"/>: the operating environment, through <see cref="HostFile"/>'s roles and
/// <see cref="SidecarLockFiles"/>' rules.</summary>
internal sealed class HostMedium : IHostMedium
{
    /// <summary>The one production medium.</summary>
    internal static HostMedium Instance { get; } = new();

    private HostMedium() { }

    public FilePresence Probe(string path) => HostFile.Probe(path);

    public void CreateLockDirectory(string directory) => HostFile.CreateLockDirectory(directory);

    public IReadOnlyList<string> List(string directory, string prefix)
    {
        try { return [.. Directory.EnumerateFileSystemEntries(directory, prefix + "*").Select(p => Path.GetFileName(p))]; }
        catch (DirectoryNotFoundException) { return []; }
    }

    public void SyncDirectory(string directory) => HostFile.SyncDirectory(directory);

    public IDisposable? PublishHolder(string directory, string name) => SidecarLockFiles.Publish(directory, _ => name);

    public HostFile.LockFileState ProbeHolder(string path) => HostFile.ProbeLockFile(path);

    public Stream CreateTransient(string path) => HostFile.OpenJournalFile(path, create: true, writable: true);

    public Stream? OpenTransient(string path, bool writable)
    {
        try { return HostFile.OpenJournalFile(path, create: false, writable); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public Stream OpenRestore(string hostPath, bool create) => HostFile.OpenRestoreHandle(hostPath, create);

    public long Length(Stream file) => file.Length;

    public int Read(Stream file, long offset, Span<byte> destination)
    {
        file.Position = offset;
        int total = 0;
        while (total < destination.Length)
        {
            int n = file.Read(destination[total..]);
            if (n == 0) break;
            total += n;
        }
        return total;
    }

    public void Write(Stream file, long offset, ReadOnlySpan<byte> source)
    {
        file.Position = offset;
        file.Write(source);
    }

    public void SetLength(Stream file, long length) => file.SetLength(length);

    public void Sync(Stream file) => HostFile.FileOf(file).Flush(flushToDisk: true);

    public void DeleteTransient(string path) => SidecarLockFiles.RetireOrThrow(path);

    public void DeleteFile(string hostPath)
    {
        try { File.Delete(hostPath); }
        catch (DirectoryNotFoundException) { }
    }

    public void RetireDirectory(string directory) => SidecarLockFiles.RetireDirectory(directory);
}
