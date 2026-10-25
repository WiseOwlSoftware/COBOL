// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE HOST MEDIUM A RUN UNIT'S SEQUENTIAL FILES AND THEIR TRANSACTION JOURNALS ARE WRITTEN THROUGH
/// (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.12; kb/Work PB3340): the roles of <see cref="HostFile"/> that change a
/// file or a name — the connector's own read and write handles, the bookkeeping handle, the journal's transient files, the
/// lock directory, its holders and its entry mutex, the removal of a file and the sync of a directory — made instance-scoped
/// on the run unit (<see cref="FileRegistry.Medium"/>), exactly as <c>Store.RunUnitPageMedium</c> is the production
/// <c>Store.IPageMedium</c> of the keyed store.
/// <para>WHY A SEAM. The journal's five write-ahead rules (2.2.2.5) are promises about what a process death, a refused write
/// or sync, and a power loss leave on the medium, at EVERY operation. The executable model
/// (<c>scripts/models/sequential_journal_model.py</c>) holds the protocol; this seam lets the same rows run over real bytes,
/// framings and checksums: the test medium logs every operation, REFUSES the n-th, DIES at it, or loses every write not
/// synced, and the streams it hands out count their writes, syncs and truncations as operations
/// (<see cref="FileStream.Flush(bool)"/> is the sync). The production medium is <see cref="HostMedium.Production"/>, which is
/// <see cref="HostFile"/> and nothing else, so <c>SharedExtendOpenDriftTests</c>' ban on every other host-path open under
/// <c>Runtime/IO</c> keeps the seam complete, and <c>SequentialJournalSeamDriftTests</c> keeps the sequential connector and
/// the journal from reaching past it.</para>
/// </summary>
internal interface IHostMedium
{
    /// <summary><see cref="HostFile.OpenConnectorStream"/>: a connector's own read or read-write handle.</summary>
    FileStream OpenConnectorStream(string hostPath, FileMode mode, FileAccess access, FileShare share, FileOptions options);

    /// <summary><see cref="HostFile.OpenConnectorWriteStream"/>: a connector's own write stream.</summary>
    Stream OpenConnectorWriteStream(string hostPath, FileMode mode, FileAccess access, FileShare share);

    /// <summary><see cref="HostFile.OpenAuxiliary"/>: a short-lived bookkeeping handle.</summary>
    FileStream OpenAuxiliary(string hostPath, FileMode mode, FileAccess access);

    /// <summary><see cref="HostFile.OpenTransient"/>: a journal's record or before file.</summary>
    FileStream OpenTransient(string path, FileMode mode, FileAccess access);

    /// <summary>Remove the physical file <paramref name="hostPath"/> (a DELETE FILE, a rollback of a creation). Throws what
    /// <see cref="File.Delete"/> throws: a removal the host refuses is the statement's to report.</summary>
    void DeleteFile(string hostPath);

    /// <summary><see cref="HostFile.SyncDirectory"/>.</summary>
    void SyncDirectory(string directory);

    /// <summary>The names in <paramref name="directory"/> that begin with <paramref name="prefix"/>; empty when the directory
    /// does not exist. Throws <see cref="UnauthorizedAccessException"/> for a directory this process may not list.</summary>
    IReadOnlyList<string> ListNames(string directory, string prefix);

    /// <summary><see cref="SidecarLockFiles.Publish"/> of a holder whose published name is <paramref name="name"/>, answering the
    /// publication's <see cref="RegionLockResult"/> and not a bare "no holder", because its two ways to fail are different findings
    /// (kb/Work PB3632, PB3675). <see cref="RegionLockResult.Ok"/>: published, in <paramref name="holder"/>.
    /// <see cref="RegionLockResult.Unavailable"/>: the host takes no lock here (a filesystem that does not hold the advisory lock,
    /// or no directory beside which to hold one). <see cref="RegionLockResult.Held"/>: every try lost its race, a failure to
    /// publish that says nothing about the host. <paramref name="holder"/> is null unless <c>Ok</c>.</summary>
    RegionLockResult PublishHolder(string directory, string name, out SidecarLockFiles.LockFileHolder? holder);

    /// <summary><see cref="HostFile.ProbeLockFile"/>.</summary>
    HostFile.LockFileState ProbeHolder(string path);

    /// <summary><see cref="SidecarLockFiles.Retire"/>: a lock-directory file removed so that no prober meets it delete-pending.</summary>
    void RetireName(string path);

    /// <summary><see cref="SidecarLockFiles.RetireDirectory"/>: the lock directory removed once it is empty.</summary>
    void RetireDirectory(string directory);

    /// <summary><see cref="HostRegionLocks.EntryMutex"/>: the endpoint a sequential file's entry mutex is held through.</summary>
    RegionLocks EntryMutex(string hostPath);
}

/// <summary>The production <see cref="IHostMedium"/>: <see cref="HostFile"/>, <see cref="SidecarLockFiles"/> and
/// <see cref="HostRegionLocks"/>, unchanged.</summary>
internal sealed class HostMedium : IHostMedium
{
    /// <summary>The one production medium; it holds no state.</summary>
    internal static readonly HostMedium Production = new();

    private HostMedium() { }

    public FileStream OpenConnectorStream(string hostPath, FileMode mode, FileAccess access, FileShare share, FileOptions options) =>
        HostFile.OpenConnectorStream(hostPath, mode, access, share, options);

    public Stream OpenConnectorWriteStream(string hostPath, FileMode mode, FileAccess access, FileShare share) =>
        HostFile.OpenConnectorWriteStream(hostPath, mode, access, share);

    public FileStream OpenAuxiliary(string hostPath, FileMode mode, FileAccess access) => HostFile.OpenAuxiliary(hostPath, mode, access);

    public FileStream OpenTransient(string path, FileMode mode, FileAccess access) => HostFile.OpenTransient(path, mode, access);

    public void DeleteFile(string hostPath) => File.Delete(hostPath);

    public void SyncDirectory(string directory) => HostFile.SyncDirectory(directory);

    public IReadOnlyList<string> ListNames(string directory, string prefix)
    {
        try { return [.. Directory.EnumerateFileSystemEntries(directory, prefix + "*").Select(p => Path.GetFileName(p))]; }
        catch (DirectoryNotFoundException) { return []; }
    }

    public RegionLockResult PublishHolder(string directory, string name, out SidecarLockFiles.LockFileHolder? holder) =>
        SidecarLockFiles.Publish(directory, _ => name, out holder);

    public HostFile.LockFileState ProbeHolder(string path) => HostFile.ProbeLockFile(path);

    public void RetireName(string path) => SidecarLockFiles.Retire(path);

    public void RetireDirectory(string directory) => SidecarLockFiles.RetireDirectory(directory);

    public RegionLocks EntryMutex(string hostPath) => HostRegionLocks.EntryMutex(hostPath);
}
