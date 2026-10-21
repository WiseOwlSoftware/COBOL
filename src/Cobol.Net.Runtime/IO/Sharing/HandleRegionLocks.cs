// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE REGION LOCKS THAT ARE A PROPERTY OF THE CONNECTOR'S OWN OPEN HANDLE (kb/Work PB2660): the locks are taken
/// through the handle that IS the connector's §9.1.15 file lock (<c>KeyedConnector</c>'s store; the handle beneath a
/// <c>SequentialConnector</c>'s reader or writer, kb/Work PB2692), never through a second handle on the path: a second
/// handle would have to be admitted by the connector's own share mode, and every other open of a host path is
/// <c>HostFile</c>'s (kb/Work PB713, PB771). Both expressions are owned by the HANDLE (the open file description on
/// Linux, the file handle on Windows), so two connectors of one run unit, and two run units in one process, conflict
/// exactly as two processes do, and the locks go with the handle when it closes.
/// <list type="bullet">
/// <item><b>Linux (x86-64, arm64)</b> — <see cref="OfdRegionLocks"/>.</item>
/// <item><b>Windows</b> — <see cref="WindowsByteRangeLocks"/> (<c>LockFileEx</c>/<c>UnlockFileEx</c>), per handle and
/// MANDATORY for the bytes they cover, which is harmless here because every byte <see cref="HostRegionLocks"/> names
/// lies at 2^62 or beyond.</item>
/// </list>
/// On a host that has neither (macOS), <see cref="HostRegionLocks.HostOfThisProcess"/> is not this host, and a witness
/// that forces it there meets <see cref="RegionLockResult.Unavailable"/> from every operation.
/// </summary>
internal sealed class HandleRegionLocks(SafeFileHandle handle, bool writable) : RegionLocks(writable)
{
    /// <summary>The host of the per-handle expressions.</summary>
    internal static readonly IRegionLockHost Host = new HandleHost();

    private sealed class HandleHost : IRegionLockHost
    {
        public RegionLocks Open(SafeFileHandle handle, string hostPath, bool writable) => new HandleRegionLocks(handle, writable);
    }

    internal override bool ExclusiveNeedsWritableHandle => !OperatingSystem.IsWindows();

    internal override RegionLockResult Hold(long offset, bool exclusive, bool wait)
    {
        if (handle.IsClosed) return RegionLockResult.Unavailable;
        if (OperatingSystem.IsWindows()) return WindowsByteRangeLocks.Lock(handle, offset, 1, exclusive, wait);
        return OfdRegionLocks.Available ? OfdRegionLocks.Hold(handle, offset, exclusive, wait) : RegionLockResult.Unavailable;
    }

    internal override RegionLockResult HeldByAnother(long offset)
    {
        if (handle.IsClosed) return RegionLockResult.Unavailable;
        if (OperatingSystem.IsWindows())
        {
            var taken = WindowsByteRangeLocks.Lock(handle, offset, 1, exclusive: true, wait: false);
            if (taken == RegionLockResult.Ok) WindowsByteRangeLocks.Unlock(handle, offset, 1);
            return taken;
        }
        return OfdRegionLocks.Available ? OfdRegionLocks.HeldByAnother(handle, offset) : RegionLockResult.Unavailable;
    }

    internal override void Release(long offset)
    {
        if (handle.IsClosed) return;   // a closed handle's locks went with it
        if (OperatingSystem.IsWindows()) WindowsByteRangeLocks.Unlock(handle, offset, 1);
        else if (OfdRegionLocks.Available) OfdRegionLocks.Release(handle, offset, 1);
    }

    /// <summary>The handle's locks go with the handle, which the connector closes.</summary>
    public override void Dispose() { }
}
