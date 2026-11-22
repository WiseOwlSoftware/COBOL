// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⛔ THE ONE PLACE A TEST HOST TIES A CHILD IT STARTS TO ITS OWN LIFETIME (kb/Work PB5057). The two child-process
/// instruments, <see cref="ProcessObserver"/> and <see cref="LiveProcess"/>, each bind the process they start here, the
/// moment it starts.
/// <para>WHY. The only kill of a program child used to be the budget kill inside <see cref="ProcessObserver.Observe"/>
/// and <see cref="LiveProcess.Dispose"/>, and both run in the test host: a host that died first (a gate stopped, a
/// worktree's run torn down, a crashed testhost) left its child running with nothing to stop it. NIST DB205A, run without
/// its MCS definitions, drains an unknown queue for ever (§1985:XIV.3.5.4 GR5c sends control past NO DATA on every
/// RECEIVE), and one such orphan busy-looped a core for over five hours after its worktree was deleted.</para>
/// <para>HOW, on Windows: one job object per host, created with <c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c> and held by the
/// host for its whole life and never closed by it. A process's handles are closed by the operating system when the
/// process ends, however it ends, so the host's death closes the job's last handle and the operating system kills every
/// process in the job; a child's own descendants are in the job too, because a process a job member starts joins it.
/// The window between <c>Process.Start</c> and the assignment is a start-up of the .NET runtime, which the programs under
/// test cannot outrun to start a descendant of their own.</para>
/// <para>NOT on Linux, by decision (kb/Work PB5057's addendum of 2026-11-21): the equivalent, <c>PR_SET_PDEATHSIG</c>, must
/// be set between <c>fork</c> and <c>exec</c>, which <c>Process.Start</c> does not expose, and it fires when the forking
/// THREAD ends, not the process: a test's thread-pool thread retires while its child still runs, which would kill a
/// healthy child mid-test. The Linux runs are CI's merge group and the WSL legs of <c>linux-gate.sh</c>, whose runner or
/// distribution ends every process it started.</para>
/// </summary>
internal static class HostBoundChildren
{
    /// <summary>The host's job, created at the first child and never disposed: its handle closes when the host ends.</summary>
    private static readonly Lazy<KillOnCloseJob?> HostJob =
        new(() => OperatingSystem.IsWindows() ? KillOnCloseJob.Create() : null, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Tie <paramref name="child"/>, just started, to this host's lifetime.</summary>
    public static void Bind(Process child) => HostJob.Value?.Assign(child);
}

/// <summary>A Windows job object whose processes the operating system kills when its last handle closes
/// (<c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c>). <see cref="HostBoundChildren"/> holds one for the host's life;
/// <see cref="Dispose"/> is the host's death in miniature, which is how the drift test proves the binding.</summary>
internal sealed class KillOnCloseJob : IDisposable
{
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JobObjectLimitKillOnJobClose = 0x2000;

    private IntPtr _handle;

    private KillOnCloseJob(IntPtr handle) => _handle = handle;

    /// <summary>Create the job. A job the operating system refuses fails loudly: a host that cannot bind its children
    /// would leak them silently again.</summary>
    public static KillOnCloseJob Create()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("A job object is a Windows mechanism (kb/Work PB5057).");
        IntPtr handle = CreateJobObjectW(IntPtr.Zero, null);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObjectW failed");
        var info = new JobObjectExtendedLimitInformationData
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation { LimitFlags = JobObjectLimitKillOnJobClose },
        };
        if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, ref info,
                (uint)Marshal.SizeOf<JobObjectExtendedLimitInformationData>()))
        {
            int error = Marshal.GetLastWin32Error();
            CloseHandle(handle);
            throw new Win32Exception(error, "SetInformationJobObject(JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE) failed");
        }
        return new KillOnCloseJob(handle);
    }

    /// <summary>Put <paramref name="process"/> in the job. A process that has already exited needs no binding; any other
    /// refusal fails loudly.</summary>
    public void Assign(Process process)
    {
        ObjectDisposedException.ThrowIf(_handle == IntPtr.Zero, this);
        if (AssignProcessToJobObject(_handle, process.Handle)) return;
        int error = Marshal.GetLastWin32Error();
        if (process.HasExited) return;
        throw new Win32Exception(error, $"AssignProcessToJobObject failed for process {process.Id}");
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        CloseHandle(_handle);
        _handle = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformationData
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObjectW(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr job, int infoClass,
        ref JobObjectExtendedLimitInformationData info, uint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
