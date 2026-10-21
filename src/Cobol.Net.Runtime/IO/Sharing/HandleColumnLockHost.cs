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
/// </summary>
internal sealed class HandleColumnLockHost : IColumnLockHost
{
    internal static readonly HandleColumnLockHost Instance = new();

    /// <summary>How often a publication chooses another byte after a collision with a holder or a tester (a tester's
    /// shared request conflicts with an exclusive attempt for the instant it lasts). Exhausting them is a host that cannot
    /// be trusted here, not a refusal.</summary>
    private const int PublishAttempts = 32;

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, out IDisposable? hold)
    {
        hold = null;
        // A witness may force this host on a machine that is not Windows: it meets "this host cannot hold the lock here".
        if (!OperatingSystem.IsWindows() || own == ExistingSharingColumn.NoOtherAnyMode) return RunUnitFileLock.Outcome.Unavailable;
        SafeFileHandle handle;
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
        try
        {
            if (!WindowsByteRangeLocks.IsDiskFile(handle)) return RunUnitFileLock.Outcome.Unavailable;   // only a disk file is locked
            long ownStart = HostRegionLocks.ColumnRangeStart(own);
            long publishedByte = -1;
            for (int attempt = 0; attempt < PublishAttempts && publishedByte < 0; attempt++)
            {
                long candidate = ownStart + Random.Shared.NextInt64(HostRegionLocks.ColumnRangeWidth);
                switch (WindowsByteRangeLocks.Lock(handle, candidate, 1, exclusive: true, wait: false))
                {
                    case RegionLockResult.Ok: publishedByte = candidate; break;
                    case RegionLockResult.Unavailable: return RunUnitFileLock.Outcome.Unavailable;
                }
            }
            if (publishedByte < 0) return RunUnitFileLock.Outcome.Unavailable;

            foreach (var column in refused)
                if (OthersHold(handle, own, publishedByte, column)) return RunUnitFileLock.Outcome.Refused;
            hold = new Published(handle, own, publishedByte);
            kept = true;
            return RunUnitFileLock.Outcome.Held;
        }
        finally { if (!kept) handle.Dispose(); }
    }

    /// <summary>Does a holder OTHER than the one that published <paramref name="publishedByte"/> in column <paramref name="own"/>
    /// hold <paramref name="column"/>? The holder's own byte is left out of its own column's range: two shared requests,
    /// one each side of it.</summary>
    [SupportedOSPlatform("windows")]
    private static bool OthersHold(SafeFileHandle handle, ExistingSharingColumn own, long publishedByte, ExistingSharingColumn column)
    {
        long start = HostRegionLocks.ColumnRangeStart(column);
        long end = start + HostRegionLocks.ColumnRangeWidth;
        return column == own
            ? RangeIsHeld(handle, start, publishedByte - start) || RangeIsHeld(handle, publishedByte + 1, end - publishedByte - 1)
            : RangeIsHeld(handle, start, end - start);
    }

    /// <summary>Does another handle hold an exclusive byte of the range? A shared request for the whole range, released at
    /// once; an empty range holds nothing. A host that cannot carry the request reads as held (the caller refuses): a holder
    /// this host cannot observe is a holder.</summary>
    [SupportedOSPlatform("windows")]
    private static bool RangeIsHeld(SafeFileHandle handle, long start, long length)
    {
        if (length <= 0) return false;
        var taken = WindowsByteRangeLocks.Lock(handle, start, length, exclusive: false, wait: false);
        if (taken == RegionLockResult.Ok) WindowsByteRangeLocks.Unlock(handle, start, length);
        return taken != RegionLockResult.Ok;
    }

    /// <summary>The published column: closing the handle releases every byte it holds. Idempotent.</summary>
    private sealed class Published(SafeFileHandle handle, ExistingSharingColumn own, long publishedByte) : IPublishedColumn
    {
        private SafeFileHandle? _handle = handle;

        public bool AnotherHolds(ExistingSharingColumn column) =>
            _handle is { IsClosed: false } held && OperatingSystem.IsWindows() && OthersHold(held, own, publishedByte, column);

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }
}
