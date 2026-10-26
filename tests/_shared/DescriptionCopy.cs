// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⛔ A SECOND HANDLE ON THE SAME OPEN FILE — the deterministic stand-in for a <c>fork</c> in another thread that has not
/// reached its <c>exec</c> (kb/Work PB3549). A forked child holds a copy of every descriptor of its parent until its
/// <c>exec</c> closes the close-on-exec ones; for those milliseconds the parent's CLOSE of a descriptor does not end the
/// open file DESCRIPTION, so a lock owned by the description (a Linux open-file-description lock) outlives the close. A
/// <c>dup(2)</c> of the descriptor is exactly that copy, made on demand: Linux <c>dup</c>, Windows <c>DuplicateHandle</c>
/// (the same file object, whose byte-range locks Windows unlocks only once its last handle is closed). Another host has
/// no handle-owned locks to outlive anything, and answers null.
/// </summary>
internal static class DescriptionCopy
{
    /// <summary>A copy of <paramref name="handle"/> on the same open file description (the caller disposes it), or null on
    /// a host that has no such copy.</summary>
    public static SafeFileHandle? Of(SafeFileHandle handle)
    {
        bool added = false;
        try
        {
            handle.DangerousAddRef(ref added);
            if (OperatingSystem.IsLinux())
            {
                int fd = Dup((int)handle.DangerousGetHandle());
                return fd < 0 ? throw new IOException($"dup failed: errno {Marshal.GetLastPInvokeError()}") : new SafeFileHandle(fd, ownsHandle: true);
            }
            if (OperatingSystem.IsWindows())
            {
                var process = GetCurrentProcess();
                return DuplicateHandle(process, handle.DangerousGetHandle(), process, out var copy, 0, false, DuplicateSameAccess)
                    ? new SafeFileHandle(copy, ownsHandle: true)
                    : throw new IOException($"DuplicateHandle failed: error {Marshal.GetLastPInvokeError()}");
            }
            return null;
        }
        finally { if (added) handle.DangerousRelease(); }
    }

    private const uint DuplicateSameAccess = 2;

    [DllImport("libc", EntryPoint = "dup", SetLastError = true)]
    private static extern int Dup(int fd);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(IntPtr sourceProcess, IntPtr source, IntPtr targetProcess, out IntPtr target,
        uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);
}
