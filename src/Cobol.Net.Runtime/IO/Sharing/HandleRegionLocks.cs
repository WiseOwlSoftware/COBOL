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
/// exactly as two processes do.
/// <list type="bullet">
/// <item><b>Linux (x86-64, arm64)</b> — <see cref="OfdRegionLocks"/>.</item>
/// <item><b>Windows</b> — <see cref="WindowsByteRangeLocks"/> (<c>LockFileEx</c>/<c>UnlockFileEx</c>), per handle and
/// MANDATORY for the bytes they cover, which is harmless here because every byte <see cref="HostRegionLocks"/> names
/// lies at 2^62 or beyond.</item>
/// </list>
/// <para>⛔ EVERY BYTE IS GIVEN BACK EXPLICITLY, NEVER LEFT TO THE HANDLE'S CLOSE (kb/Work PB3549). The endpoint records
/// every hold it took, and <see cref="Dispose"/> unlocks each one while the handle is still open; every owner of an
/// endpoint disposes it before it closes the handle. Closing alone is no release a successor can rely on: a Linux
/// open-file-description lock lives until EVERY descriptor on the description is closed, and a <c>fork</c> in another
/// thread (a <c>CALL "SYSTEM"</c>, any child process the host program starts) holds a copy of every descriptor until its
/// <c>exec</c> closes it (<see cref="OfdRegionLocks.Release"/>); Windows documents that the time it takes to unlock a
/// closed handle's locks "depends upon available system resources". A transaction's liveness byte left to the close was
/// read as a LIVE owner by the next writable OPEN, whose dead-owner step then left the abandoned transaction standing.</para>
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

    /// <summary>Every byte this endpoint holds, and whether exclusively: what <see cref="Dispose"/> unlocks. A hold does
    /// not stack, on this host as on the lock-file host (<see cref="SidecarRegionLocks"/>): a byte held at least as strongly
    /// already is Ok at once, and one <see cref="Release"/> gives it back. A Linux open-file-description lock is that by
    /// nature; a Windows <c>LockFileEx</c> lock would stack, which the map keeps from happening.</summary>
    private readonly Dictionary<long, bool> _held = [];

    private bool _disposed;

    internal override bool ExclusiveNeedsWritableHandle => !OperatingSystem.IsWindows();

    internal override RegionLockResult Hold(long offset, bool exclusive, bool wait)
    {
        if (_disposed || handle.IsClosed) return RegionLockResult.Unavailable;
        bool held, heldExclusively;
        lock (_held) held = _held.TryGetValue(offset, out heldExclusively);
        if (held && (heldExclusively || !exclusive)) return RegionLockResult.Ok;   // held at least as strongly already
        // Shared -> exclusive: a Linux description converts its own lock in one call (and keeps the shared one if another
        // holder refuses it); a Windows exclusive request meets the handle's own shared lock, so it is given back first, as
        // the lock-file host does.
        if (held && OperatingSystem.IsWindows()) Release(offset);
        var result = OperatingSystem.IsWindows() ? WindowsByteRangeLocks.Lock(handle, offset, 1, exclusive, wait)
            : OfdRegionLocks.Available ? OfdRegionLocks.Hold(handle, offset, exclusive, wait)
            : RegionLockResult.Unavailable;
        if (result == RegionLockResult.Ok)
            lock (_held) _held[offset] = exclusive;
        return result;
    }

    internal override RegionLockResult HeldByAnother(long offset)
    {
        if (_disposed || handle.IsClosed) return RegionLockResult.Unavailable;
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
        lock (_held)
            if (!_held.Remove(offset)) return;   // not held through this endpoint: the documented no-op
        Unlock(offset);
    }

    /// <summary>⛔ Unlock every byte this endpoint holds through the handle while it is still open (the class remarks: never
    /// left to the close). Idempotent; the handle is the connector's to close.</summary>
    public override void Dispose()
    {
        long[] held;
        lock (_held)
        {
            if (_disposed) return;
            _disposed = true;
            held = [.. _held.Keys];
            _held.Clear();
        }
        foreach (long offset in held) Unlock(offset);
    }

    private void Unlock(long offset)
    {
        if (handle.IsClosed) return;   // a handle closed under the endpoint (a disposal that threw first): its locks went with it
        if (OperatingSystem.IsWindows()) WindowsByteRangeLocks.Unlock(handle, offset, 1);
        else if (OfdRegionLocks.Available) OfdRegionLocks.Release(handle, offset, 1);
    }
}
