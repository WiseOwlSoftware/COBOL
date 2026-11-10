#!/usr/bin/env python3
"""process_job.py — THE ONE WAY a script holds a WHOLE PROCESS TREE it can stop (kb/Work PB5098).

    from process_job import ProcessJob, bind_current
    with ProcessJob.start(argv, cwd=..., env=..., stdout=fh, stderr=fh) as job:
        rc = job.wait(timeout_s)        # subprocess.TimeoutExpired past it
        job.kill()                      # the root AND every process it started, orphaned or not
        job.alive()                     # the pids still in the tree once the root has exited
    bind_current()                      # Windows: this process joins a kill-on-close job (gate_slot's tree)
    python scripts/process_job.py --self-test

⛔ WHY. On Windows a kill reaches one process: `subprocess.run(timeout=)` killed a self-test's `pwsh` and nothing it
had started, so on 2026-11-09 21 `orchestrate.ps1` test supervisors ran on for an hour after their gates, and on
2026-11-10 one held the runner's stdout pipe, so `subprocess.run`'s post-kill `communicate()` (no timeout on Windows)
waited on it and the gate's HANG detector itself hung 87 minutes (run 000001 of fix-same-file-hold). `taskkill /T` walks
the tree by PARENT id, so it cannot reach a grandchild whose parent has already exited.

THE MECHANISM.
  * Windows: the root starts SUSPENDED, joins a fresh Job object with KILL_ON_JOB_CLOSE, and only then resumes, so no
    process it starts can escape the job (the job grants no breakaway: detached.py's CREATE_BREAKAWAY_FROM_JOB is refused
    inside it, and its target stays in the tree). `kill` is TerminateJobObject; closing the handle (the `with` block's
    end, or this process dying) kills whatever is still in it. Nested jobs are supported (Windows 8+), so a tree inside
    gate_slot's own job works.
  * POSIX: the root leads a new session (`start_new_session`); `kill` is `killpg(SIGKILL)` and `alive` probes the group.
Callers give the child FILES, never pipes, for its output: a pipe an orphan still holds blocks its reader, a file never
does."""
from __future__ import annotations

import os
import signal
import subprocess
import sys
import time

IS_WINDOWS = os.name == "nt"

if IS_WINDOWS:
    import ctypes
    from ctypes import wintypes

    class _IO_COUNTERS(ctypes.Structure):
        _fields_ = [(n, ctypes.c_ulonglong) for n in ("ReadOperationCount", "WriteOperationCount",
                                                      "OtherOperationCount", "ReadTransferCount",
                                                      "WriteTransferCount", "OtherTransferCount")]

    class _BASIC_LIMIT(ctypes.Structure):
        _fields_ = [("PerProcessUserTimeLimit", ctypes.c_int64), ("PerJobUserTimeLimit", ctypes.c_int64),
                    ("LimitFlags", wintypes.DWORD), ("MinimumWorkingSetSize", ctypes.c_size_t),
                    ("MaximumWorkingSetSize", ctypes.c_size_t), ("ActiveProcessLimit", wintypes.DWORD),
                    ("Affinity", ctypes.c_size_t), ("PriorityClass", wintypes.DWORD),
                    ("SchedulingClass", wintypes.DWORD)]

    class _EXTENDED_LIMIT(ctypes.Structure):
        _fields_ = [("BasicLimitInformation", _BASIC_LIMIT), ("IoInfo", _IO_COUNTERS),
                    ("ProcessMemoryLimit", ctypes.c_size_t), ("JobMemoryLimit", ctypes.c_size_t),
                    ("PeakProcessMemoryUsed", ctypes.c_size_t), ("PeakJobMemoryUsed", ctypes.c_size_t)]

    _PID_LIST_MAX = 1024

    class _PROCESS_ID_LIST(ctypes.Structure):
        _fields_ = [("NumberOfAssignedProcesses", wintypes.DWORD), ("NumberOfProcessIdsInList", wintypes.DWORD),
                    ("ProcessIdList", ctypes.c_size_t * _PID_LIST_MAX)]

    _JobObjectBasicProcessIdList = 3
    _JobObjectExtendedLimitInformation = 9
    _JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    _CREATE_SUSPENDED = 0x00000004
    _PROCESS_TERMINATE, _PROCESS_SET_QUOTA, _PROCESS_SUSPEND_RESUME = 0x0001, 0x0100, 0x0800

    _k32 = ctypes.WinDLL("kernel32", use_last_error=True)
    _k32.CreateJobObjectW.argtypes = [wintypes.LPVOID, wintypes.LPCWSTR]
    _k32.CreateJobObjectW.restype = wintypes.HANDLE
    _k32.SetInformationJobObject.argtypes = [wintypes.HANDLE, ctypes.c_int, wintypes.LPVOID, wintypes.DWORD]
    _k32.SetInformationJobObject.restype = wintypes.BOOL
    _k32.QueryInformationJobObject.argtypes = [wintypes.HANDLE, ctypes.c_int, wintypes.LPVOID, wintypes.DWORD,
                                               wintypes.LPVOID]
    _k32.QueryInformationJobObject.restype = wintypes.BOOL
    _k32.AssignProcessToJobObject.argtypes = [wintypes.HANDLE, wintypes.HANDLE]
    _k32.AssignProcessToJobObject.restype = wintypes.BOOL
    _k32.TerminateJobObject.argtypes = [wintypes.HANDLE, wintypes.UINT]
    _k32.TerminateJobObject.restype = wintypes.BOOL
    _k32.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    _k32.OpenProcess.restype = wintypes.HANDLE
    _k32.CloseHandle.argtypes = [wintypes.HANDLE]
    _k32.CloseHandle.restype = wintypes.BOOL
    _k32.GetCurrentProcess.restype = wintypes.HANDLE
    _ntdll = ctypes.WinDLL("ntdll")
    _ntdll.NtResumeProcess.argtypes = [wintypes.HANDLE]
    _ntdll.NtResumeProcess.restype = ctypes.c_long

    def _check(ok) -> None:
        if not ok:
            raise ctypes.WinError(ctypes.get_last_error())

    def _new_job() -> int:
        """A fresh Job object whose last handle's close kills every process in it; its handle is not inheritable."""
        job = _k32.CreateJobObjectW(None, None)
        _check(job)
        info = _EXTENDED_LIMIT()
        info.BasicLimitInformation.LimitFlags = _JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        try:
            _check(_k32.SetInformationJobObject(job, _JobObjectExtendedLimitInformation, ctypes.byref(info),
                                                ctypes.sizeof(info)))
        except OSError:
            _k32.CloseHandle(job)
            raise
        return job


_CURRENT_JOB = None  # kept for the life of the process: closing it would kill the tree it holds


def bind_current() -> None:
    """Windows: put THIS process in a kill-on-close Job object, so every process it starts from now on is in the job
    and dies with it (gate_slot's slot holder). Idempotent. POSIX: nothing — the caller's own mechanism applies."""
    global _CURRENT_JOB
    if not IS_WINDOWS or _CURRENT_JOB is not None:
        return
    job = _new_job()
    _check(_k32.AssignProcessToJobObject(job, _k32.GetCurrentProcess()))
    _CURRENT_JOB = job


class ProcessJob:
    """One root process and everything it starts, stopped as one (module docstring). Use it as a context manager: the
    block's end kills whatever of the tree is still alive, so nothing it started outlives its caller's step."""

    def __init__(self, popen: subprocess.Popen, job=None):
        self.popen, self._job = popen, job

    @classmethod
    def start(cls, argv: list[str], **kw) -> "ProcessJob":
        """Start `argv` as the root of a held tree; `kw` are subprocess.Popen's (give stdout/stderr FILES)."""
        if not IS_WINDOWS:
            return cls(subprocess.Popen(argv, start_new_session=True, **kw))
        job = _new_job()
        try:
            p = subprocess.Popen(argv, creationflags=kw.pop("creationflags", 0) | _CREATE_SUSPENDED, **kw)
        except BaseException:
            _k32.CloseHandle(job)
            raise
        handle = _k32.OpenProcess(_PROCESS_TERMINATE | _PROCESS_SET_QUOTA | _PROCESS_SUSPEND_RESUME, False, p.pid)
        try:
            _check(handle)
            _check(_k32.AssignProcessToJobObject(job, handle))
            if _ntdll.NtResumeProcess(handle) != 0:
                raise OSError(f"process_job: NtResumeProcess failed for pid {p.pid}")
        except BaseException:
            p.kill()
            p.wait()
            _k32.CloseHandle(job)
            raise
        finally:
            if handle:
                _k32.CloseHandle(handle)
        return cls(p, job)

    @property
    def pid(self) -> int:
        return self.popen.pid

    def wait(self, timeout: float | None = None) -> int:
        """The root's exit code; subprocess.TimeoutExpired when it is still running after `timeout` seconds."""
        return self.popen.wait(timeout)

    def alive(self) -> list[int]:
        """The pids of the tree still running (the root's included while it runs). POSIX knows only whether the group
        has a member, so it answers [the group's id] or []."""
        if not IS_WINDOWS:
            try:
                os.killpg(self.popen.pid, 0)
            except (ProcessLookupError, PermissionError):
                return []
            return [self.popen.pid]
        if self._job is None:
            return []
        ids = _PROCESS_ID_LIST()
        _check(_k32.QueryInformationJobObject(self._job, _JobObjectBasicProcessIdList, ctypes.byref(ids),
                                              ctypes.sizeof(ids), None))
        return [int(ids.ProcessIdList[i]) for i in range(ids.NumberOfProcessIdsInList)]

    def kill(self) -> None:
        """Kill the root and every process of its tree, orphaned or not; then reap the root. Idempotent."""
        if IS_WINDOWS:
            if self._job is not None:
                _k32.TerminateJobObject(self._job, 1)
        else:
            try:
                os.killpg(self.popen.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        try:
            self.popen.wait(30)
        except subprocess.TimeoutExpired:  # a root the kill did not reach is the defect this module exists for
            raise OSError(f"process_job: pid {self.popen.pid} survived its tree's kill") from None

    def close(self) -> None:
        """Kill what is left and release the job."""
        self.kill()
        if IS_WINDOWS and self._job is not None:
            _k32.CloseHandle(self._job)
            self._job = None

    def __enter__(self) -> "ProcessJob":
        return self

    def __exit__(self, *exc) -> None:
        self.close()


# ── self-test ───────────────────────────────────────────────────────────────────────────────────────────────────

def _self_test() -> int:
    import tempfile
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        ok = got == want
        print(f"{'PASS' if ok else 'FAIL'}: {name}" + ("" if ok else f" — got {got!r}, want {want!r}"))
        if not ok:
            fails.append(name)

    from detached import pid_alive  # this directory's: the one liveness probe (kb/Work PB4008)

    def until_dead(pid: int) -> None:
        """Wait for `pid` to die, with no limit: a tree the kill does not end is the hang detector's (no wall clock)."""
        while pid_alive(pid):
            time.sleep(0.1)

    with tempfile.TemporaryDirectory(prefix="process-job-") as d:
        pidfile = os.path.join(d, "grandchild.pid")
        # The root starts a grandchild that sleeps, writes its pid, and EXITS at once: the grandchild is an orphan,
        # the shape taskkill /T cannot reach and the one that hung the runner on 2026-11-10.
        sleeper = [sys.executable, "-c", "import time; time.sleep(600)"]
        root_code = ("import subprocess, sys; p = subprocess.Popen(" + repr(sleeper) + ", stdin=subprocess.DEVNULL); "
                     f"open({pidfile!r}, 'w').write(str(p.pid))")
        with open(os.path.join(d, "out.txt"), "w", encoding="utf-8", newline="\n") as out:
            job = ProcessJob.start([sys.executable, "-c", root_code], stdout=out, stderr=out, stdin=subprocess.DEVNULL)
            with job:
                rc = job.wait()
                check("the root exits while its orphaned grandchild runs on", rc, 0)
                grand = int(open(pidfile, encoding="utf-8").read().strip())  # written before the root exited
                check("the orphan is still in the tree after its parent exited", bool(job.alive()), True)
                job.kill()
                until_dead(grand)
                check("kill reaches the orphaned grandchild (taskkill /T cannot)", pid_alive(grand), False)
                check("nothing of the tree is left after kill", job.alive(), [])

        # A root still running past its wait is TimeoutExpired, and the block's end kills it and its child.
        with open(os.path.join(d, "out2.txt"), "w", encoding="utf-8", newline="\n") as out:
            hold = [sys.executable, "-c", "import subprocess, sys, time; subprocess.Popen(" + repr(sleeper)
                    + ", stdin=subprocess.DEVNULL); time.sleep(600)"]
            with ProcessJob.start(hold, stdout=out, stderr=out, stdin=subprocess.DEVNULL) as job:
                try:
                    job.wait(1.0)
                    timed_out = False
                except subprocess.TimeoutExpired:
                    timed_out = True
                root = job.pid
            check("a root past its wait is TimeoutExpired", timed_out, True)
            check("the block's end kills a still-running root", pid_alive(root), False)

    if IS_WINDOWS:
        bind_current()
        bind_current()
        check("bind_current is idempotent and holds one job", _CURRENT_JOB is not None, True)
    print(f"process_job self-test: {'GREEN' if not fails else 'RED — ' + ', '.join(fails)}")
    return 1 if fails else 0


if __name__ == "__main__":
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(_self_test())
    print(__doc__)
    sys.exit(2)
