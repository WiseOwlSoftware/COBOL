// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>⛔ WHICH PHYSICAL FILE A HANDLE OR A PATH REACHES, IN THE HOST'S OWN TERMS (kb/Work PB3533): the volume and the file's
/// number on it — <c>st_dev</c> and <c>st_ino</c> on Linux, the volume serial number and the 128-bit file id on Windows
/// (<c>FILE_ID_INFO</c>, which ReFS needs: its file ids do not fit the 64-bit index). Two handles reach one physical file exactly
/// when their identities are equal. A path names a file only at the instant it is asked: a file removed or renamed away and
/// another made at the same path is another physical file under the same path, which <see cref="HostFile.PhysicalFileComparer"/>
/// (the path's identity, kb/Work PB2748) cannot see. Asked through <see cref="HostFile.IdentityOf"/> and
/// <see cref="HostFile.IdentityAt"/>, never directly.
/// <para>The number of a file that is gone may be given to a new one, so two identities taken at different times name one file
/// only while a handle keeps the first one alive — which a kept handle, or the descriptor of the column lock kept with it, does
/// (DESIGN-runtime-library.md 2.2.1.10).</para></summary>
internal readonly record struct PhysicalFileIdentity(ulong Volume, UInt128 File);

/// <summary>
/// The host's answers to "which physical file is this?" (<see cref="PhysicalFileIdentity"/>) — Linux <c>statx</c> and Windows
/// <c>GetFileInformationByHandleEx(FileIdInfo)</c> — and the one <c>statx</c> binding of the runtime, whose file type
/// <see cref="OfdRegionLocks"/> reads as well (<see cref="IsRegularFile"/>). Every answer is null where the host cannot give one:
/// a process shape this binding was not written for (macOS, a 32-bit or other-architecture Linux), a libc without <c>statx</c>
/// (before glibc 2.28), a file system that keeps no file ids, or a path that names nothing; the caller then knows the file by
/// its path alone, as it did before (kb/Work PB2748).
/// </summary>
internal static class HostFileIdentity
{
    // ── Linux: statx(2), whose struct layout is the same on every architecture ──────────────────────────────────────────
    private const int AT_FDCWD = -100;
    private const int AT_EMPTY_PATH = 0x1000;
    private const uint STATX_TYPE = 0x1;
    private const uint STATX_INO = 0x100;
    private const int StatxBytes = 256;          // sizeof(struct statx); the kernel fills the first 144
    private const int StatxMaskOffset = 0;       // stx_mask
    private const int StatxModeOffset = 28;      // stx_mode: after mask, blksize, attributes, nlink, uid, gid
    private const int StatxInodeOffset = 32;     // stx_ino
    private const int StatxDevMajorOffset = 136; // stx_dev_major, then stx_dev_minor
    private const int StatxDevMinorOffset = 140;
    private const int S_IFMT = 0xF000;
    private const int S_IFREG = 0x8000;

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int NativeStatx(int dirfd, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags,
        uint mask, byte[] buffer);

    /// <summary>The ABI the Linux binding was written for, asked once (the same process shape as <see cref="OfdRegionLocks"/>'s).</summary>
    private static readonly bool LinuxAbi =
        OperatingSystem.IsLinux() && Environment.Is64BitProcess
        && RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64;

    /// <summary><c>statx</c> of <paramref name="path"/> relative to <paramref name="dirfd"/> (<paramref name="flags"/> as the call
    /// takes them), or null when the call fails or this libc has none.</summary>
    private static byte[]? Statx(int dirfd, string path, int flags, uint mask)
    {
        if (!LinuxAbi) return null;
        try
        {
            var buffer = new byte[StatxBytes];
            return NativeStatx(dirfd, path, flags, mask, buffer) == 0 ? buffer : null;
        }
        catch (EntryPointNotFoundException) { return null; }   // a libc without statx (before glibc 2.28)
        catch (DllNotFoundException) { return null; }          // no libc under this name
    }

    /// <summary>Whether descriptor <paramref name="fd"/> is a REGULAR file (not a device node, a FIFO or a terminal) — the
    /// question <see cref="OfdRegionLocks.Open"/> asks before it locks anything; false when it cannot be told.</summary>
    internal static bool IsRegularFile(int fd) =>
        Statx(fd, "", AT_EMPTY_PATH, STATX_TYPE) is { } status
        && (BitConverter.ToUInt16(status, StatxModeOffset) & S_IFMT) == S_IFREG;

    /// <summary>The identity in a <c>statx</c> answer, when it carries the inode (<c>STATX_INO</c> in <c>stx_mask</c>; the device
    /// is always filled).</summary>
    private static PhysicalFileIdentity? FromStatx(byte[]? status) =>
        status is not null && (BitConverter.ToUInt32(status, StatxMaskOffset) & STATX_INO) != 0
            ? new PhysicalFileIdentity(
                ((ulong)BitConverter.ToUInt32(status, StatxDevMajorOffset) << 32) | BitConverter.ToUInt32(status, StatxDevMinorOffset),
                BitConverter.ToUInt64(status, StatxInodeOffset))
            : null;

    // ── Windows: the volume serial number and the 128-bit file id ────────────────────────────────────────────────────────
    private const int FileIdInfoClass = 18;   // FILE_INFO_BY_HANDLE_CLASS.FileIdInfo
    private const uint FILE_SHARE_ALL = 0x1 | 0x2 | 0x4;   // read, write, delete: the identity open refuses no other handle
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    /// <summary>FILE_ID_INFO: a 64-bit volume serial number and the 16 bytes of FILE_ID_128, spelled as two 64-bit halves so the
    /// layout is the native one (a <see cref="UInt128"/> field would be aligned to 16 bytes and land at the wrong offset).</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FileIdInfo
    {
        public ulong VolumeSerialNumber;
        public ulong FileIdLow;
        public ulong FileIdHigh;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle file, int infoClass, out FileIdInfo info, int size);

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition,
        uint flags, IntPtr template);

    private static PhysicalFileIdentity? WindowsIdentity(SafeFileHandle handle) =>
        GetFileInformationByHandleEx(handle, FileIdInfoClass, out var info, Marshal.SizeOf<FileIdInfo>())
            ? new PhysicalFileIdentity(info.VolumeSerialNumber, new UInt128(info.FileIdHigh, info.FileIdLow))
            : null;   // a file system that keeps no file ids answers no identity

    // ── The two questions ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The identity of the physical file <paramref name="handle"/> is open on, or null where the host cannot tell.</summary>
    internal static PhysicalFileIdentity? Of(SafeFileHandle handle)
    {
        if (handle.IsInvalid || handle.IsClosed) return null;
        if (OperatingSystem.IsWindows()) return WindowsIdentity(handle);
        bool added = false;
        try
        {
            handle.DangerousAddRef(ref added);
            return FromStatx(Statx((int)handle.DangerousGetHandle(), "", AT_EMPTY_PATH, STATX_INO));
        }
        finally { if (added) handle.DangerousRelease(); }
    }

    /// <summary>The identity of the physical file <paramref name="path"/> names NOW, or null when it names none or the host
    /// cannot tell. ⛔ It refuses no other handle and is refused by none: on Windows the open asks for NO access (a share mode
    /// arbitrates only read, write and delete access), so a holder under <see cref="FileShare.None"/> — the connector's own
    /// kept handle under SHARING WITH NO OTHER among them — neither refuses it nor is disturbed by it; on Linux nothing is
    /// opened.</summary>
    internal static PhysicalFileIdentity? At(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            string name = path.Length >= 260 && !path.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\" + path : path;
            using var probe = CreateFile(name, 0, FILE_SHARE_ALL, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            return probe.IsInvalid ? null : WindowsIdentity(probe);
        }
        return FromStatx(Statx(AT_FDCWD, path, 0, STATX_INO));
    }
}
