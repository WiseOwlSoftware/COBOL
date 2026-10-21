// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE LINUX HOST OF <see cref="RunUnitFileLock"/> (kb/Work PB833): each column is one byte of the physical file,
/// published as a SHARED open-file-description lock through a read-only descriptor of its own
/// (<see cref="OfdRegionLocks"/>), at <c>2^62 +</c> the column's <see cref="RunUnitFileLock.WireNumber"/>. Publishing
/// first and then asking <c>F_OFD_GETLK</c> — which never reports a lock the asking description holds itself — is what
/// makes two simultaneous OPENs unable to pass each other without a mutex, and the lock dies with its descriptor, so a
/// run unit that is killed releases it.
/// </summary>
internal sealed class OfdColumnLockHost : IColumnLockHost
{
    internal static readonly OfdColumnLockHost Instance = new();

    private static readonly int ColumnCount = Enum.GetValues<ExistingSharingColumn>().Length;

    /// <summary>The byte a column is published on. ⛔ A WIRE VALUE (see <see cref="RunUnitFileLock.WireNumber"/>).</summary>
    private static long ByteOf(ExistingSharingColumn column) => HostRegionLocks.RegionBase + RunUnitFileLock.WireNumber(column);

    public RunUnitFileLock.Outcome Establish(string hostPath, ExistingSharingColumn own,
        IReadOnlyList<ExistingSharingColumn> refused, IDisposable? supersedes, out IDisposable? hold)
    {
        hold = null;
        // A superseded hold's description is reused: F_OFD_GETLK never reports a lock the asking description holds itself, so
        // the test already ignores the old column, and the new one is published beside it before the old is given back.
        var previous = supersedes is Held { Region: not null } live ? live : null;
        var region = previous?.Region ?? OfdRegionLocks.Open(hostPath);
        if (region is null) return RunUnitFileLock.Outcome.Unavailable;
        bool kept = false;
        try
        {
            switch (OfdRegionLocks.Hold(region, ByteOf(own), exclusive: false, wait: false))
            {
                case RegionLockResult.Held: return RunUnitFileLock.Outcome.Refused;   // someone holds our byte exclusively: not our protocol, not ours to override
                case RegionLockResult.Unavailable: return RunUnitFileLock.Outcome.Unavailable;
            }

            foreach (var column in refused)
            {
                switch (OfdRegionLocks.HeldByAnother(region, ByteOf(column)))
                {
                    case RegionLockResult.Held: return RunUnitFileLock.Outcome.Refused;
                    case RegionLockResult.Unavailable: return RunUnitFileLock.Outcome.Unavailable;
                }
            }
            if (previous is null) hold = new Held(region, own);
            else
            {
                if (previous.Own != own) OfdRegionLocks.Release(region, ByteOf(previous.Own), 1);
                previous.Own = own;
                hold = previous;
            }
            kept = true;
            return RunUnitFileLock.Outcome.Held;
        }
        finally
        {
            if (!kept)
            {
                // A superseded description stays as it was: only the byte this request added is given back.
                if (previous is null) Give(region);
                else if (previous.Own != own) OfdRegionLocks.Release(region, ByteOf(own), 1);
            }
        }
    }

    /// <summary>Unlock every protocol byte, THEN close the description (see <see cref="OfdRegionLocks.Release"/>).</summary>
    private static void Give(SafeFileHandle region)
    {
        OfdRegionLocks.Release(region, HostRegionLocks.RegionBase, ColumnCount);
        region.Dispose();
    }

    /// <summary>The published column: closing the description releases every region it holds. Idempotent.</summary>
    private sealed class Held(SafeFileHandle region, ExistingSharingColumn own) : IDisposable
    {
        private SafeFileHandle? _region = region;

        /// <summary>The description, or null once given back.</summary>
        internal SafeFileHandle? Region => _region;

        /// <summary>The column published on it now; a superseding request moves it.</summary>
        internal ExistingSharingColumn Own { get; set; } = own;

        public void Dispose()
        {
            if (_region is { } held) Give(held);
            _region = null;
        }
    }
}
