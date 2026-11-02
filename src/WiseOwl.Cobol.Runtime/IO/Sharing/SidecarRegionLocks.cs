// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ THE REGION LOCKS OF A HOST WITH NO OPEN-FILE-DESCRIPTION LOCKS — macOS above all (kb/Work PB2936). ISO §9.1.16:
/// <i>"While locked by a given file connector, a record is not accessible to another file connector in the same or a
/// different run unit"</i>; §9.1.13.8 1): the access answers I-O status 51. Before this class a record lock and the
/// store mutex of a shared RELATIVE or INDEXED file had no expression off Linux and Windows, so two run units sharing
/// such a file under <c>SHARING WITH ALL OTHER</c> both read '00' for a record one of them held, wrote over each other's
/// store and lost updates, silently.
/// <para><b>The mechanism is <see cref="SidecarLockFiles"/>'s, with a different NAME.</b> A "byte" is held by publishing a
/// lock file in the physical file's lock directory — the one <see cref="SidecarColumnLockHost"/> publishes its columns
/// in — named <c>r&lt;offset as 16 hex digits&gt;-&lt;S|X&gt;-&lt;guid&gt;</c>: the byte, the mode (<c>S</c> shared,
/// <c>X</c> exclusive) and a name no other holder shares. (⛔ A WIRE VALUE: two runtime versions must find each other.)
/// Classic <c>fcntl</c> record locks cannot stand in: they belong to the process and are dropped when ANY descriptor the
/// process holds on the file closes, so a second connector's CLOSE in the same run unit would release the first's locks;
/// and <c>fcntl</c> is variadic, which Apple's arm64 ABI passes differently from a fixed P/Invoke signature.</para>
/// <para><b>Publish, then test, as <see cref="RunUnitFileLock"/> does.</b> A hold publishes its file and then looks for
/// another LIVE holder of the same byte that conflicts with it — any holder for an exclusive hold, an exclusive holder
/// for a shared one — skipping only its own file. Two exclusive requests at the same instant cannot both pass, because
/// whichever completes its publication second finds the first; both may be refused (each saw the other), which is a
/// refused statement and not a violated lock, and a waiting request retries after a jittered pause, so one of them
/// goes through. A refused or unavailable hold leaves no file behind.</para>
/// <para><b>Dead holders refuse nobody, and holders this process cannot observe refuse everybody.</b> A file whose
/// lock nobody holds is a killed run unit's leftover and is swept by whoever finds it; a file the process may not open
/// is <see cref="HostFile.LockFileState.Unobservable"/> and is neither (kb/Work PB2917): a non-waiting hold reports it
/// <see cref="RegionLockResult.Held"/>, and a WAITING one reports <see cref="RegionLockResult.Unavailable"/> because it
/// cannot wait for a release it cannot see — the run unit then has only its own protection for that statement, the
/// same degradation a directory it may not write has. A lock directory this user may enter but not LIST hides every
/// holder in it and is unobservable as a whole (the train 1056b review: read as a lock the host cannot hold, it gave a
/// READ WITH LOCK the record §9.1.13.8 1) refuses with '51').</para>
/// <para><b>Release and close.</b> A released byte's file is deleted before its descriptor closes
/// (<see cref="SidecarLockFiles.LockFileHolder"/>), the endpoint's disposal releases every byte it still holds, and a
/// run unit that is killed holds nothing — the kernel dropped its locks with its descriptors.</para>
/// <para><b>Parking (kb/Work PB4387).</b> An endpoint made by <see cref="ParkingForPath"/> does not retire a byte it releases: the
/// holder's file is renamed to <c>i&lt;offset as 16 hex digits&gt;-&lt;guid&gt;</c> with its lock still held, a name no
/// <see cref="Look"/> reads, so it holds against no one, and the next hold of the byte renames it back and then looks, which is the
/// publish-then-test protocol over a file already published. For the one holder that takes and gives back one byte per statement
/// for the life of a connector. Its first publication sweeps the dead idle files of the byte (a killed holder leaves one), and
/// its disposal retires the parked file and, with the last file, the lock directory.</para>
/// </summary>
internal sealed class SidecarRegionLocks : RegionLocks
{
    /// <summary>The host of the lock-file expression.</summary>
    internal static readonly IRegionLockHost Host = new SidecarHost();

    private sealed class SidecarHost : IRegionLockHost
    {
        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) => new SidecarRegionLocks(hostPath, writable, parkReleased: false);
    }

    /// <summary>An endpoint on the physical file at <paramref name="hostPath"/> that is held through no handle on it — the
    /// one kind of endpoint that serves a file that may not exist (<see cref="HostRegionLocks.EntryMutexByte"/>, kb/Work
    /// PB3340). Writable: a lock file holds an exclusive byte whatever the access to the physical file
    /// (<see cref="ExclusiveNeedsWritableHandle"/>).</summary>
    internal static RegionLocks ForPath(string hostPath) => new SidecarRegionLocks(hostPath, writable: true, parkReleased: false);

    /// <summary>⛔ AN ENDPOINT THAT PARKS WHAT IT RELEASES (kb/Work PB4387): like <see cref="ForPath"/>, but a byte released is not
    /// retired, it is RENAMED to an idle name no prober reads and kept, its lock still held, until the byte is held again (one
    /// rename back) or the endpoint is disposed. For a holder who takes and gives back the SAME byte once per statement for the
    /// life of a connector (<see cref="HostRegionLocks.KeptEntryMutex"/>): a publication and a retirement of the file and of the
    /// lock directory per statement is what this replaces.</summary>
    internal static RegionLocks ParkingForPath(string hostPath) => new SidecarRegionLocks(hostPath, writable: true, parkReleased: true);

    /// <summary>The lock directory, or null for a path no lock directory can be named beside (every operation is then
    /// <see cref="RegionLockResult.Unavailable"/>).</summary>
    private readonly string? _directory;

    private readonly Dictionary<long, Held> _holds = [];

    /// <summary>Whether a released byte is parked (<see cref="ParkingForPath"/>) rather than retired.</summary>
    private readonly bool _parkReleased;

    /// <summary>The bytes released and parked, by offset: holders whose files carry the idle name (<see cref="IdleName"/>), their
    /// locks still held. Always empty for an endpoint that does not park.</summary>
    private readonly Dictionary<long, Parked> _parked = [];

    private bool _disposed;

    private readonly record struct Held(SidecarLockFiles.LockFileHolder Holder, bool Exclusive, Guid Id);

    private readonly record struct Parked(SidecarLockFiles.LockFileHolder Holder, Guid Id);

    private enum Conflict { None, Live, Unobservable }

    private SidecarRegionLocks(string hostPath, bool writable, bool parkReleased) : base(writable)
    {
        _parkReleased = parkReleased;
        try { _directory = SidecarLockFiles.LockDirectoryOf(hostPath); }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
        catch (PathTooLongException) { }
    }

    /// <summary>A lock file's name carries no mode a lock file could not hold, so a shared hold needs no read-only
    /// handle and an exclusive one no writable handle.</summary>
    internal override bool ExclusiveNeedsWritableHandle => false;

    /// <summary>The prefix of every file holding <paramref name="offset"/>: <c>r</c> and the offset as 16 hex digits.</summary>
    private static string Prefix(long offset) => $"r{offset:x16}-";

    /// <summary>⛔ A WIRE VALUE: the name of a holder file that holds <paramref name="offset"/> (<see cref="Prefix"/>, the mode, the id).</summary>
    private static string HeldName(long offset, bool exclusive, Guid id) => $"{Prefix(offset)}{(exclusive ? 'X' : 'S')}-{id:N}";

    /// <summary>The prefix of every PARKED file of <paramref name="offset"/> (<see cref="ParkingForPath"/>): <c>i</c> and the offset
    /// as 16 hex digits. No <see cref="Look"/> reads it, so a parked file holds nothing against anyone, and no older runtime
    /// reads it either.</summary>
    private static string IdlePrefix(long offset) => $"i{offset:x16}-";

    /// <summary>⛔ A WIRE VALUE: the name a released byte's file is parked under (<see cref="IdlePrefix"/> and the id).</summary>
    private static string IdleName(long offset, Guid id) => $"{IdlePrefix(offset)}{id:N}";

    /// <summary>Give back the holder of <paramref name="offset"/> that was never (or is no longer) a hold: parked under its idle
    /// name when this endpoint parks and the host renames it, else closed and retired.</summary>
    private void GiveBack(long offset, SidecarLockFiles.LockFileHolder holder, Guid id)
    {
        if (_parkReleased && !_disposed && holder.Rename(IdleName(offset, id))) _parked[offset] = new Parked(holder, id);
        else holder.Dispose();
    }

    /// <summary>The parked holder of <paramref name="offset"/> named a hold in this mode again, or null (nothing parked, or the host
    /// refused the rename: the parked holder is then closed, and the caller publishes).</summary>
    private Held? TakeParked(long offset, bool exclusive)
    {
        if (!_parked.Remove(offset, out var parked)) return null;
        if (parked.Holder.Rename(HeldName(offset, exclusive, parked.Id))) return new Held(parked.Holder, exclusive, parked.Id);
        parked.Holder.Dispose();
        return null;
    }

    /// <summary>Retire the parked files of <paramref name="offset"/> whose holders are dead (a run unit killed while it kept one),
    /// which nothing else reads: a live one is another endpoint's and is left alone. Asked once per publication of a parking
    /// endpoint, so a killed run unit's litter is swept by the next one to publish and never grows.</summary>
    private void SweepIdle(long offset)
    {
        try
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(_directory!, IdlePrefix(offset) + "*").ToList())
                SidecarLockFiles.Observe(path);
        }
        catch (DirectoryNotFoundException) { }       // no holder has ever published here
        catch (UnauthorizedAccessException) { }      // a directory this user may enter but not list: nothing to sweep
    }

    internal override RegionLockResult Hold(long offset, bool exclusive, bool wait)
    {
        if (_disposed || _directory is null) return RegionLockResult.Unavailable;
        try
        {
            if (_holds.TryGetValue(offset, out var mine))
            {
                if (mine.Exclusive || !exclusive) return RegionLockResult.Ok;   // held at least as strongly already
                Release(offset);                                                 // shared -> exclusive: give back, then take
            }
            bool lastRoundWasTheExtraOne = false;
            for (int pause = 0; ; pause++)
            {
                // ⛔ THE EXTRA ROUND IS OWED TO EVERY LOST ROUND, NOT ONCE PER CALL (kb/Work PB3676): only a lost round that
                // follows ANOTHER lost round with nothing in between is refused it. A round that saw a live holder (and
                // waited for it) is progress, so the next lost round beside a holder that has since left earns its own.
                bool extraRoundSpent = lastRoundWasTheExtraOne;
                lastRoundWasTheExtraOne = false;
                // After a conflict, look before publishing again so a waiting request does not churn lock files.
                if (pause > 0)
                {
                    var plain = Look(offset, exclusive, ownName: null);
                    if (plain == Conflict.Unobservable) return wait ? RegionLockResult.Unavailable : RegionLockResult.Held;
                    if (plain == Conflict.Live) { if (!wait) return RegionLockResult.Held; Backoff(pause); continue; }
                }
                // ⛔ A PARKED HOLDER IS NAMED A HOLDER AGAIN, NOT PUBLISHED AGAIN (kb/Work PB4387): one rename, after which the
                // look below is the publish-then-test protocol exactly as for a fresh file.
                SidecarLockFiles.LockFileHolder? holder;
                Guid published = default;
                RegionLockResult publication;
                if (TakeParked(offset, exclusive) is { } reused)
                {
                    (holder, published, publication) = (reused.Holder, reused.Id, RegionLockResult.Ok);
                }
                else
                {
                    if (_parkReleased && pause == 0) SweepIdle(offset);
                    publication = SidecarLockFiles.Publish(_directory, id => HeldName(offset, exclusive, published = id), out holder);
                }
                if (publication == RegionLockResult.Unavailable) return RegionLockResult.Unavailable;   // a host that takes no lock here sees no holder
                if (publication == RegionLockResult.Held)
                {
                    // ⛔ EVERY TRY LOST ITS RACE (kb/Work PB3632): no hold of ours, but the holders already published are still
                    // seen, so a conflicting one is a conflict ('Held', a wait goes on waiting) and not a lock this host cannot
                    // carry; only with nothing in the way is the hold Unavailable.
                    switch (Look(offset, exclusive, ownName: null))
                    {
                        case Conflict.Live:
                            if (!wait) return RegionLockResult.Held;
                            Backoff(pause);
                            continue;
                        case Conflict.Unobservable:
                            return wait ? RegionLockResult.Unavailable : RegionLockResult.Held;
                        default:
                            // A WAITING hold publishes ONE more round before it gives up: the holder that made its tries lose
                            // (its rmdir of the last file) may have let go during the round, so a lock that was free by the
                            // time we looked is not a lock this host cannot carry (review R1-1). Two lost rounds in a row
                            // with nothing in the way are a host that cannot carry it, so the wait stays bounded.
                            if (wait && !extraRoundSpent) { lastRoundWasTheExtraOne = true; continue; }
                            return RegionLockResult.Unavailable;
                    }
                }
                switch (Look(offset, exclusive, holder!.Name))
                {
                    case Conflict.None:
                        _holds[offset] = new Held(holder, exclusive, published);
                        return RegionLockResult.Ok;
                    case Conflict.Unobservable:
                        GiveBack(offset, holder, published);
                        return wait ? RegionLockResult.Unavailable : RegionLockResult.Held;
                    default:
                        GiveBack(offset, holder, published);
                        if (!wait) return RegionLockResult.Held;
                        Backoff(pause);
                        continue;
                }
            }
        }
        catch (IOException) { return RegionLockResult.Unavailable; }
        catch (UnauthorizedAccessException) { return RegionLockResult.Unavailable; }
        catch (ArgumentException) { return RegionLockResult.Unavailable; }
        catch (NotSupportedException) { return RegionLockResult.Unavailable; }
    }

    internal override RegionLockResult HeldByAnother(long offset)
    {
        if (_disposed || _directory is null) return RegionLockResult.Unavailable;
        try
        {
            string? own = _holds.TryGetValue(offset, out var mine) ? mine.Holder.Name : null;
            return Look(offset, exclusive: true, own) == Conflict.None ? RegionLockResult.Ok : RegionLockResult.Held;
        }
        catch (IOException) { return RegionLockResult.Unavailable; }
        catch (UnauthorizedAccessException) { return RegionLockResult.Unavailable; }
    }

    internal override void Release(long offset)
    {
        if (_holds.Remove(offset, out var held)) GiveBack(offset, held.Holder, held.Id);
    }

    public override void Dispose()
    {
        _disposed = true;
        foreach (var held in _holds.Values) held.Holder.Dispose();
        _holds.Clear();
        foreach (var parked in _parked.Values) parked.Holder.Dispose();
        _parked.Clear();
    }

    /// <summary>Is there a holder of <paramref name="offset"/>, other than the file <paramref name="ownName"/>, that a
    /// request in this mode conflicts with: any holder for an exclusive request, an exclusive holder for a shared one?
    /// A dead holder's file is swept on the way and conflicts with nothing.</summary>
    private Conflict Look(long offset, bool exclusive, string? ownName)
    {
        string prefix = Prefix(offset);
        IEnumerable<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(_directory!, prefix + "*").ToList(); }
        catch (DirectoryNotFoundException) { return Conflict.None; }   // no holder has ever published here, or the last one closed
        catch (UnauthorizedAccessException) { return Conflict.Unobservable; }   // a directory this user may enter but not list hides every holder
        Conflict found = Conflict.None;
        foreach (string path in entries)
        {
            string name = Path.GetFileName(path);
            if (string.Equals(name, ownName, StringComparison.Ordinal)) continue;
            bool otherExclusive = name.Length > prefix.Length && name[prefix.Length] == 'X';
            if (!exclusive && !otherExclusive) continue;   // shared beside shared
            switch (SidecarLockFiles.Observe(path))
            {
                case HostFile.LockFileState.Live: return Conflict.Live;
                case HostFile.LockFileState.Unobservable: found = Conflict.Unobservable; break;
            }
        }
        return found;
    }

    /// <summary>The pause between two attempts of a waiting hold: jittered, so two requests that refused each other
    /// at the same instant do not retry in step.</summary>
    private static void Backoff(int attempt)
    {
        if (BackoffObserver.Value is { } observer) { observer(attempt); return; }
        Thread.Sleep(Random.Shared.Next(1, 3 + Math.Min(attempt, 18)));
    }

    /// <summary>Test seam: when set, the pause of a waiting hold is REPORTED (the attempt number) instead of performed, so a
    /// test lets a holder go BETWEEN two attempts of one waiting hold without a thread or a timed sleep (the precedent of
    /// <see cref="SidecarLockFiles.PauseObserver"/>, kb/Work PB3676). An <see cref="AsyncLocal{T}"/>: only the calling test's
    /// own flow observes.</summary>
    internal static readonly AsyncLocal<Action<int>?> BackoffObserver = new();
}
