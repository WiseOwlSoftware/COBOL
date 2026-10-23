// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE WINDOWS HOST OF <see cref="RunUnitFileLock"/> (kb/Work PB3482): Table 19 as a lock between run units on the one
/// host whose share modes are mandatory and per access. ISO §9.1.15's file lock <i>"prevent[s] other run units from
/// opening that file with incompatible sharing rules"</i>, and a Windows share mode can say that only as far as an
/// access can: <see cref="FileShare.Read"/> refuses every writer, so a connector under SHARING WITH READ ONLY that
/// meets an INPUT opener which still owes its transaction's writes (a handle open for writing, 2.2.1.10 in
/// docs/rearchitecture/DESIGN-runtime-library.md) is refused by the HOST although Table 19 prints <i>Normal open</i> — the
/// wrong answer '61' this class exists to correct. With the columns published it is the table that judges, and the share
/// mode yields exactly where the table says it must (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>).
/// <para><b>The mechanism.</b> A holder opens a read handle of its own (<see cref="HostFile.OpenColumnHolder"/>) and
/// PUBLISHES its column by holding exclusively one byte of that column's range
/// (<see cref="HostRegionLocks.ColumnRangeStart"/>), chosen at random: Windows locks belong to a handle and conflict with
/// every OTHER handle, so "does any holder but me hold this column" is a test of the whole range with the holder's own
/// byte left out — two shared requests, one each side of it — and a shared request conflicts only with an exclusive hold,
/// so two testers never exclude each other. It publishes first and tests second, exactly as the Linux host does
/// (<see cref="OfdColumnLockHost"/>): two OPENs at the same instant cannot both pass, because whichever publishes second
/// finds the first (both may be refused, which is a refused OPEN and no violated lock). The lock dies with the handle, so
/// a run unit that is killed releases it.</para>
/// <para><b>SHARING WITH NO OTHER publishes nothing</b> (<see cref="RunUnitFileLock.Outcome.Unavailable"/>): its own handle
/// is <see cref="FileShare.None"/>, which refuses this host's read handle, and refuses every other open — a column
/// would add nothing the share mode does not already say. A holder that meets such a connector is refused: its read
/// handle is refused by the share mode, which is Table 19's NO OTHER column (every request row refuses it).</para>
/// <para><b>A superseding request</b> (<see cref="IColumnLockHost.Establish"/>'s <c>supersedes</c>, a kept lock's
/// re-OPEN: kb/Work PB3483) publishes through the superseded hold's OWN handle, as the Linux host reuses its description:
/// the new byte is held beside the old one, the test leaves both out, and the old byte is unlocked only once the new
/// column stands — no instant in which the file is unlocked. ⚠ The one exception is a re-OPEN under SHARING WITH NO
/// OTHER, whose own <see cref="FileShare.None"/> handle the holder's handle would refuse: after the test passes the holder
/// gives way (<see cref="RunUnitFileLock.Outcome.Held"/>, a hold that publishes nothing), and the instant between that and the connector's
/// own handle is the share-mode window <c>KeyedConnector.RetakeKeptHandle</c> names for the kept handle, which a host
/// whose share modes are per handle cannot close.</para>
/// </summary>
internal sealed class HandleColumnLockHost : IColumnLockHost
{
    internal static readonly HandleColumnLockHost Instance = new();

    /// <summary>How often a publication chooses another byte after a collision with a holder or a tester (a tester's
    /// shared request conflicts with an exclusive attempt for the instant it lasts). Exhausting them is a host that cannot
    /// be trusted here, not a refusal.</summary>
    private const int PublishAttempts = 64;

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
    {
        hold = null;
        // A witness may force this host on a machine that is not Windows: it meets "this host cannot hold the lock here".
        if (!OperatingSystem.IsWindows()) return RunUnitFileLock.Outcome.Unavailable;
        var previous = supersedes is Published { Handle: not null } live ? live : null;
        if (own == ExistingSharingColumn.NoOtherAnyMode)
            return previous is null ? RunUnitFileLock.Outcome.Unavailable : GiveWay(previous, refused, out hold);

        SafeFileHandle handle;
        if (previous is not null) handle = previous.Handle!;
        else
            try { handle = HostFile.OpenColumnHolder(hostPath); }
            // A holder that excludes readers (SHARING WITH NO OTHER, or a foreign process that does): Table 19's first
            // column, which every request row refuses.
            catch (IOException e) when (HostFile.IsSharingRefusal(e)) { return RunUnitFileLock.Outcome.Refused; }
            // An OPEN has only I-O statuses as outcomes (SharedExtendOpenDriftTests): a file this host cannot reach for it
            // (absent, refused, a device name) is a lock it cannot hold here, never an escaping exception.
            catch (IOException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (UnauthorizedAccessException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (ArgumentException) { return RunUnitFileLock.Outcome.Unavailable; }
            catch (NotSupportedException) { return RunUnitFileLock.Outcome.Unavailable; }

        bool kept = false;
        long added = -1;
        try
        {
            if (previous is null && !WindowsByteRangeLocks.IsDiskFile(handle)) return RunUnitFileLock.Outcome.Unavailable;   // only a disk file is locked
            long publishedByte;
            if (previous is not null && previous.Own == own) publishedByte = previous.Byte;   // the column stands as it is
            else
            {
                publishedByte = Publish(handle, own);
                if (publishedByte < 0) return RunUnitFileLock.Outcome.Unavailable;
                added = publishedByte;
            }

            (ExistingSharingColumn, long)[] ownBytes = previous is null || previous.Own == own
                ? [(own, publishedByte)]
                : [(own, publishedByte), (previous.Own, previous.Byte)];
            foreach (var column in refused)
                if (OthersHold(handle, column, ownBytes, whenUnobservable: true)) return RunUnitFileLock.Outcome.Refused;

            if (previous is null) hold = new Published(handle, own, publishedByte);
            else
            {
                // The replacement stands; only now does the column it replaced go.
                if (previous.Own != own) WindowsByteRangeLocks.Unlock(handle, previous.Byte, 1);
                previous.Move(own, publishedByte);
                hold = previous;
            }
            kept = true;
            return RunUnitFileLock.Outcome.Held;
        }
        finally
        {
            if (!kept)
            {
                // A superseded hold stays as it was: only the byte this request added is given back — unlocked
                // explicitly before a handle of this request's own is closed (kb/Work PB3549: never left to the close).
                if (added >= 0) WindowsByteRangeLocks.Unlock(handle, added, 1);
                if (previous is null) handle.Dispose();
            }
        }
    }

    /// <summary>Hold exclusively one byte of <paramref name="own"/>'s range through <paramref name="handle"/>, chosen at
    /// random, and answer it; -1 when this host cannot (the attempts ran out, or the handle carries no lock).</summary>
    [SupportedOSPlatform("windows")]
    private static long Publish(SafeFileHandle handle, ExistingSharingColumn own)
    {
        long ownStart = HostRegionLocks.ColumnRangeStart(own);
        for (int attempt = 0; attempt < PublishAttempts; attempt++)
        {
            // A tester's shared request over the range lasts microseconds: the first retries yield, the later ones wait
            // a millisecond, so a run of tries cannot all fall inside one tester's instant.
            if (attempt > 0) { if (attempt < 8) Thread.Yield(); else Thread.Sleep(1); }
            long candidate = ownStart + Random.Shared.NextInt64(HostRegionLocks.ColumnRangeWidth);
            switch (WindowsByteRangeLocks.Lock(handle, candidate, 1, exclusive: true, wait: false))
            {
                case RegionLockResult.Ok: return candidate;
                case RegionLockResult.Unavailable: return -1;
            }
        }
        return -1;
    }

    /// <summary>A superseding request under SHARING WITH NO OTHER (see the class remarks): test every column it refuses
    /// through the superseded hold, its own byte left out, and only when no other holder stands give that hold back — its
    /// handle would refuse the connector's own <see cref="FileShare.None"/> one — answering <see cref="RunUnitFileLock.Outcome.Held"/>
    /// with a hold that publishes nothing (<see cref="RunUnitFileLock.Revert"/> publishes the old column again through a
    /// handle of its own). A refusal leaves the superseded hold exactly as it was.</summary>
    [SupportedOSPlatform("windows")]
    private static RunUnitFileLock.Outcome GiveWay(Published previous, IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        (ExistingSharingColumn, long)[] ownBytes = [(previous.Own, previous.Byte)];
        foreach (var column in refused)
            if (OthersHold(previous.Handle!, column, ownBytes, whenUnobservable: true)) return RunUnitFileLock.Outcome.Refused;
        previous.Dispose();
        hold = new Published(null, ExistingSharingColumn.NoOtherAnyMode, -1);
        return RunUnitFileLock.Outcome.Held;
    }

    /// <summary>Does a holder OTHER than the one whose bytes are <paramref name="ownBytes"/> hold <paramref name="column"/>?
    /// The holder's own bytes in the column's range are left out: shared requests over the stretches between them.
    /// <paramref name="whenUnobservable"/> is the answer for a range this host cannot carry the request over: the
    /// establishment refuses (a holder this host cannot observe is a holder), and the question that would let a share mode
    /// yield does not (an unknown is never a reason to admit).</summary>
    [SupportedOSPlatform("windows")]
    private static bool OthersHold(SafeFileHandle handle, ExistingSharingColumn column,
        ReadOnlySpan<(ExistingSharingColumn Column, long Byte)> ownBytes, bool whenUnobservable)
    {
        long start = HostRegionLocks.ColumnRangeStart(column);
        long end = start + HostRegionLocks.ColumnRangeWidth;
        Span<long> skipped = stackalloc long[ownBytes.Length];
        int count = 0;
        foreach (var (ownColumn, ownByte) in ownBytes)
            if (ownColumn == column) skipped[count++] = ownByte;
        skipped = skipped[..count];
        skipped.Sort();
        long from = start;
        foreach (long ownByte in skipped)
        {
            if (RangeIsHeld(handle, from, ownByte - from, whenUnobservable)) return true;
            from = ownByte + 1;
        }
        return RangeIsHeld(handle, from, end - from, whenUnobservable);
    }

    /// <summary>Does another handle hold an exclusive byte of the range? A shared request for the whole range, released at
    /// once; an empty range holds nothing; a host that cannot carry the request answers <paramref name="whenUnobservable"/>.</summary>
    [SupportedOSPlatform("windows")]
    private static bool RangeIsHeld(SafeFileHandle handle, long start, long length, bool whenUnobservable)
    {
        if (length <= 0) return false;
        var taken = WindowsByteRangeLocks.Lock(handle, start, length, exclusive: false, wait: false);
        if (taken == RegionLockResult.Ok) WindowsByteRangeLocks.Unlock(handle, start, length);
        return taken == RegionLockResult.Held || (taken == RegionLockResult.Unavailable && whenUnobservable);
    }

    /// <summary>The published column: disposing it unlocks its byte and closes the handle. Idempotent. A superseding request
    /// moves it to its own column (<see cref="Move"/>); a hold with no handle publishes nothing (a re-OPEN under SHARING
    /// WITH NO OTHER, <see cref="GiveWay"/>).</summary>
    private sealed class Published(SafeFileHandle? handle, ExistingSharingColumn own, long publishedByte) : IPublishedColumn
    {
        /// <summary>The holder's handle, or null once given back (or when it publishes nothing).</summary>
        internal SafeFileHandle? Handle { get; private set; } = handle;

        /// <summary>The column published now.</summary>
        internal ExistingSharingColumn Own { get; private set; } = own;

        /// <summary>The byte of <see cref="Own"/>'s range the handle holds exclusively.</summary>
        internal long Byte { get; private set; } = publishedByte;

        /// <summary>A superseding request published <paramref name="column"/> at <paramref name="at"/> and gave the old byte back.</summary>
        internal void Move(ExistingSharingColumn column, long at) => (Own, Byte) = (column, at);

        public bool HoldsAHandle => Handle is { IsClosed: false };

        public bool AnotherHolds(ExistingSharingColumn column) =>
            Handle is { IsClosed: false } held && OperatingSystem.IsWindows()
            && OthersHold(held, column, [(Own, Byte)], whenUnobservable: false);

        /// <summary>Unlock the published byte, THEN close the handle (kb/Work PB3549): Windows documents that the time it
        /// takes to unlock a closed handle's locks "depends upon available system resources", so a successor must not be
        /// refused for a column already given back.</summary>
        public void Dispose()
        {
            if (Handle is { IsClosed: false } held && OperatingSystem.IsWindows()) WindowsByteRangeLocks.Unlock(held, Byte, 1);
            Handle?.Dispose();
            Handle = null;
        }
    }
}
