// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE EXPRESSION OF A BYTE-RANGE LOCK ON WINDOWS: <c>LockFileEx</c> / <c>UnlockFileEx</c> on a RANGE of a file
/// handle, read here once for both of its users — <see cref="HandleRegionLocks"/> (one byte of a connector's own handle:
/// a record lock, the store mutex, a transaction's liveness byte) and <see cref="HandleColumnLockHost"/> (the width of a
/// Table 19 column's range, kb/Work PB3482). The locks are owned by the HANDLE, conflict across handles of one process
/// exactly as across processes, are MANDATORY for the bytes they cover (harmless: every range
/// <see cref="HostRegionLocks"/> names lies at 2^62 or beyond, far past the end of any file), and go with the handle when
/// it closes. A handle's own hold conflicts with its own exclusive request (<see cref="RegionLocks.ExclusiveNeedsWritableHandle"/>:
/// a shared hold cannot be tested through the handle that holds it), so a holder that asks about a range it holds a byte of
/// asks around that byte.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsByteRangeLocks
{
    private const uint LockfileFailImmediately = 0x1;
    private const uint LockfileExclusiveLock = 0x2;
    private const int ErrorLockViolation = 33;
    private const uint FileTypeDisk = 1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool LockFileEx(SafeFileHandle file, uint flags, uint reserved, uint bytesLow,
        uint bytesHigh, ref NativeOverlapped overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UnlockFileEx(SafeFileHandle file, uint reserved, uint bytesLow, uint bytesHigh,
        ref NativeOverlapped overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(SafeFileHandle file);

    private static NativeOverlapped At(long offset) => new()
    {
        OffsetLow = unchecked((int)(uint)offset),
        OffsetHigh = (int)(offset >> 32),
    };

    /// <summary>Is <paramref name="handle"/> a disk file — the only kind whose bytes another run unit can damage and
    /// that carries byte-range locks (a device or a pipe does not)?</summary>
    internal static bool IsDiskFile(SafeFileHandle handle) => GetFileType(handle) == FileTypeDisk;

    /// <summary>Lock <paramref name="length"/> bytes from <paramref name="offset"/> through <paramref name="handle"/>,
    /// exclusively or shared, waiting for a conflicting holder when <paramref name="wait"/>. A conflicting holder is
    /// <see cref="RegionLockResult.Held"/>; anything but a lock violation (an overlapped handle's ERROR_IO_PENDING, a
    /// filesystem without byte-range locks) means this handle cannot carry the lock — never that someone holds it.</summary>
    internal static RegionLockResult Lock(SafeFileHandle handle, long offset, long length, bool exclusive, bool wait)
    {
        var at = At(offset);
        uint flags = (exclusive ? LockfileExclusiveLock : 0) | (wait ? 0 : LockfileFailImmediately);
        if (LockFileEx(handle, flags, 0, (uint)length, (uint)(length >> 32), ref at)) return RegionLockResult.Ok;
        return Marshal.GetLastPInvokeError() == ErrorLockViolation ? RegionLockResult.Held : RegionLockResult.Unavailable;
    }

    /// <summary>Unlock exactly the range a <see cref="Lock"/> took. A range not held is the documented no-op
    /// (ERROR_NOT_LOCKED).</summary>
    internal static void Unlock(SafeFileHandle handle, long offset, long length)
    {
        var at = At(offset);
        _ = UnlockFileEx(handle, 0, (uint)length, (uint)(length >> 32), ref at);
    }
}
