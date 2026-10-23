// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE DIRECTORY SYNC OF THE RUNTIME (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.14; kb/Work PB3340, PB3492),
/// asked only through <see cref="HostFile.SyncDirectory"/>: make a directory's ENTRIES durable on the medium — a file created,
/// renamed or removed in it — as a file's sync makes its bytes durable. The sequential journal needs it at its write-ahead
/// rules R1 and R4 and at COMMIT and ROLLBACK (2.2.2.5 to 2.2.2.7): a created file whose directory entry never reached the
/// medium is absent after a power loss, and a removed one is back. .NET has no API for it, so the per-host determination
/// is here (DESIGN-runtime-library.md 2.2.2.14):
/// <list type="bullet">
/// <item><b>Windows:</b> the directory opened with <c>FILE_FLAG_BACKUP_SEMANTICS</c> (the only way to open a directory) and
/// write access, then <c>FlushFileBuffers</c>, which flushes the directory's own metadata.</item>
/// <item><b>Linux and the other Unixes:</b> <c>open(directory, O_RDONLY)</c> and <c>fsync</c>, the POSIX way to make a
/// directory's entries durable. The two-argument <c>open</c> is safe to declare on every ABI, since its mode argument is read
/// only with <c>O_CREAT</c>. On macOS <c>fsync</c> reaches the drive and not the medium (<c>F_FULLFSYNC</c> goes through the
/// variadic <c>fcntl</c>, which Apple's arm64 ABI passes differently from a fixed signature, the reason
/// <see cref="SidecarLockFiles"/> makes no native call): the determination there is the drive's cache.</item>
/// </list>
/// A refusal throws <see cref="IOException"/> carrying the host's code, which the caller maps (a refused sync is '30').
/// </summary>
internal static class HostDirectory
{
    /// <summary>Make the entries of <paramref name="directory"/> durable (class remarks).</summary>
    internal static void Sync(string directory)
    {
        if (OperatingSystem.IsWindows()) { SyncWindows(directory); return; }
        int fd = NativeOpen(directory, O_RDONLY);
        if (fd < 0) throw Failure("open", directory);
        try
        {
            if (NativeFsync(fd) != 0) throw Failure("fsync", directory);
        }
        finally { _ = NativeClose(fd); }
    }

    private static void SyncWindows(string directory)
    {
        string name = directory.Length >= 260 && !directory.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\" + directory : directory;
        using SafeFileHandle handle = CreateFile(name, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_ALL, IntPtr.Zero, OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (handle.IsInvalid) throw Failure("CreateFile", directory);
        if (!FlushFileBuffers(handle)) throw Failure("FlushFileBuffers", directory);
    }

    /// <summary>The host's refusal as the <see cref="IOException"/> every other host operation throws: its HRESULT the host's code
    /// (an HRESULT on Windows, the raw errno elsewhere, as .NET surfaces them), so <see cref="HostFile.IsMediumBoundary"/> and the
    /// other classifiers read it.</summary>
    private static IOException Failure(string call, string directory)
    {
        int code = Marshal.GetLastPInvokeError();
        string message = $"{call} of the directory '{directory}' was refused: {new Win32Exception(code).Message}";
        bool windows = OperatingSystem.IsWindows();
        if (windows ? code is ERROR_FILE_NOT_FOUND or ERROR_PATH_NOT_FOUND : code == ENOENT)
            return new DirectoryNotFoundException(message);   // gone: the caller decides what a vanished directory means
        return new IOException(message, windows ? unchecked((int)0x80070000) | (code & 0xFFFF) : code);
    }

    private const int ENOENT = 2;
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_PATH_NOT_FOUND = 3;

    // ── Unix ────────────────────────────────────────────────────────────────────────────────────────────────────────────
    private const int O_RDONLY = 0;

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int NativeOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int NativeFsync(int fd);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int NativeClose(int fd);

    // ── Windows ─────────────────────────────────────────────────────────────────────────────────────────────────────────
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_ALL = 0x7;     // read, write and delete: the sync refuses no other handle
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition,
        uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers(SafeFileHandle handle);
}
