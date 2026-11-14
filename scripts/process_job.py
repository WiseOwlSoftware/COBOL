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

from standard_streams import ensure_utf8  # this directory's: UTF-8 standard streams, called first in __main__

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
    _k32.WaitForMultipleObjects.argtypes = [wintypes.DWORD, ctypes.POINTER(wintypes.HANDLE), wintypes.BOOL,
                                            wintypes.DWORD]
    _k32.WaitForMultipleObjects.restype = wintypes.DWORD
    _SYNCHRONIZE, _PROCESS_QUERY_LIMITED_INFORMATION = 0x00100000, 0x1000
    _WAIT_OBJECT_0, _WAIT_TIMEOUT, _MAXIMUM_WAIT_OBJECTS = 0x0, 0x102, 64
    _k32.GetCurrentProcess.restype = wintypes.HANDLE
    _k32.IsProcessInJob.argtypes = [wintypes.HANDLE, wintypes.HANDLE, ctypes.POINTER(wintypes.BOOL)]
    _k32.IsProcessInJob.restype = wintypes.BOOL
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
        """The pids of the tree still running (the root's included while it runs). Running is `detached.pid_alive`'s,
        the scripts' one liveness probe: a zombie holds nothing and is dead (kb/Work PB5206). On Linux the group's members
        are read from /proc and each is asked; `killpg(pgid, 0)` alone answers for a zombie too, and a killed orphan
        stays one until whatever adopted it reaps it (a CI runner's subreaper reaps late: the merge group of #1475 read
        the killed grandchild as still in the tree). Where there is no /proc the group signal is all there is."""
        if not IS_WINDOWS:
            from detached import pid_alive, proc_stat  # noqa: PLC0415  (the one liveness probe and stat parse)
            pgid = self.popen.pid
            if not os.path.isdir("/proc/self"):
                try:
                    os.killpg(pgid, 0)
                except (ProcessLookupError, PermissionError):
                    return []
                return [pgid]
            members = []
            for entry in os.listdir("/proc"):
                if not entry.isdigit():
                    continue
                stat = proc_stat(int(entry))   # None: it exited between the listing and the read
                if stat is not None and stat[1] == pgid and pid_alive(int(entry)):
                    members.append(int(entry))
            return sorted(members)
        if self._job is None:
            return []
        ids = _PROCESS_ID_LIST()
        _check(_k32.QueryInformationJobObject(self._job, _JobObjectBasicProcessIdList, ctypes.byref(ids),
                                              ctypes.sizeof(ids), None))
        return [int(ids.ProcessIdList[i]) for i in range(ids.NumberOfProcessIdsInList)]

    def kill(self) -> None:
        """Kill the root and every process of its tree, orphaned or not, and return only once EVERY one of them is gone
        (kb/Work PB5500): a caller may then read, move or delete the files the tree wrote. A member still running
        `KILL_BOUND_S` after the kill is the defect this module exists for: OSError naming its pid. Idempotent.

        Gone is not what the kill call or the job's pid list says. TerminateJobObject and killpg only REQUEST the end,
        and Windows drops a process from the job's pid list BEFORE it closes that process's handles: on a loaded host an
        orphan that inherited the root's output file still held it after `alive()` read empty (18 of 50). A process
        object is signaled only once its handle table is run down, so on Windows the kill opens a SYNCHRONIZE handle to
        every member BEFORE terminating the job and waits on each; on POSIX a member's descriptors are closed before it
        is a zombie, which `alive()` already reads as dead (kb/Work PB5206)."""
        deadline = time.monotonic() + self.KILL_BOUND_S
        if IS_WINDOWS:
            if self._job is not None:
                self._terminate_and_await(deadline)
        else:
            try:
                os.killpg(self.popen.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        try:
            self.popen.wait(max(0.0, deadline - time.monotonic()))
        except subprocess.TimeoutExpired:  # a root the kill did not reach is the defect this module exists for
            raise OSError(f"process_job: pid {self.popen.pid} survived its tree's kill") from None
        if not IS_WINDOWS:
            while survivors := self.alive():
                if time.monotonic() >= deadline:
                    raise OSError(f"process_job: pids {survivors} of {self.popen.pid}'s tree survived its kill")
                time.sleep(0.01)

    KILL_BOUND_S = 30.0

    def _terminate_and_await(self, deadline: float) -> None:
        """Windows: hold every member, terminate the job, and wait until each held process object is signaled. The
        members are re-read after each pass, so a process a member started between the hold and the terminate is
        caught while it is still listed."""
        waited: set[int] = set()
        while True:
            held = self._hold_members(waited)
            _k32.TerminateJobObject(self._job, 1)
            if not held:
                return
            try:
                self._await(held, deadline)
            finally:
                for h in held.values():
                    _k32.CloseHandle(h)
            waited.update(held)

    def _hold_members(self, skip: set[int]) -> dict[int, int]:
        """A SYNCHRONIZE handle to each listed member not in `skip`; a pid that is gone, or reused by a process outside
        the job between the listing and the open, is not held (it is no member)."""
        held: dict[int, int] = {}
        for pid in self.alive():
            if pid in skip:
                continue
            h = _k32.OpenProcess(_SYNCHRONIZE | _PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
            if not h:
                continue
            member = wintypes.BOOL()
            if not _k32.IsProcessInJob(h, self._job, ctypes.byref(member)):
                err = ctypes.WinError(ctypes.get_last_error())
                _k32.CloseHandle(h)
                for other in held.values():
                    _k32.CloseHandle(other)
                raise err
            if member.value:
                held[pid] = h
            else:
                _k32.CloseHandle(h)
        return held

    @staticmethod
    def _await(held: dict[int, int], deadline: float) -> None:
        """Wait until every held process object is signaled; OSError naming the pids still running at `deadline`."""
        items = list(held.items())
        for i in range(0, len(items), _MAXIMUM_WAIT_OBJECTS):
            chunk = items[i:i + _MAXIMUM_WAIT_OBJECTS]
            arr = (wintypes.HANDLE * len(chunk))(*(h for _, h in chunk))
            ms = int(max(0.0, deadline - time.monotonic()) * 1000)
            rc = _k32.WaitForMultipleObjects(len(chunk), arr, True, ms)
            if rc == _WAIT_TIMEOUT:
                survivors = [pid for pid, h in chunk
                             if _k32.WaitForMultipleObjects(1, (wintypes.HANDLE * 1)(h), True, 0) == _WAIT_TIMEOUT]
                raise OSError(f"process_job: pids {survivors} of a tree survived its kill")
            if not _WAIT_OBJECT_0 <= rc < _WAIT_OBJECT_0 + len(chunk):
                raise ctypes.WinError(ctypes.get_last_error())

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

    from detached import pid_alive, proc_stat  # this directory's: the one liveness probe (kb/Work PB4008)

    if sys.platform.startswith("linux"):
        # Adopt this test's orphans and do not reap them until the end (kb/Work PB5206): a killed orphan then stays a
        # zombie, as under a CI runner's late-reaping subreaper, so the zombie case runs on every host, not only on one
        # whose init is slow. PR_SET_CHILD_SUBREAPER = 36.
        import ctypes  # noqa: PLC0415
        if ctypes.CDLL(None, use_errno=True).prctl(36, 1, 0, 0, 0) != 0:
            fails.append("prctl(PR_SET_CHILD_SUBREAPER) failed")

    def orphan_tree(d: str, name: str) -> tuple[list[str], str]:
        """A root that starts a sleeping grandchild (which inherits the root's stdout), writes its pid and sleeps on."""
        pidfile = os.path.join(d, name + ".pid")
        sleeper = [sys.executable, "-c", "import time; time.sleep(600)"]
        return ([sys.executable, "-c", "import subprocess, time; p = subprocess.Popen(" + repr(sleeper)
                 + f", stdin=subprocess.DEVNULL); open({pidfile!r}, 'w').write(str(p.pid)); time.sleep(600)"],
                pidfile)

    def read_pid(pidfile: str, job: "ProcessJob") -> int:
        """The grandchild's pid once the root has written it (no wall clock: a root that never writes is a hang)."""
        while not (os.path.exists(pidfile) and open(pidfile, encoding="utf-8").read().strip()):
            if job.popen.poll() is not None:
                raise OSError(f"process_job self-test: the root exited ({job.popen.returncode}) before it wrote "
                              f"{pidfile}")
            time.sleep(0.02)
        return int(open(pidfile, encoding="utf-8").read().strip())

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
                check("kill reaches the orphaned grandchild (taskkill /T cannot), and returns once it is gone",
                      pid_alive(grand), False)
                check("nothing of the tree is left after kill", job.alive(), [])
                if sys.platform.startswith("linux"):
                    stat = proc_stat(grand)
                    check("the killed orphan is an unreaped zombie here, the case alive() must call dead",
                          stat[0] if stat else "gone", "Z")
                    if stat:
                        os.waitpid(grand, 0)   # reap it now that the case is checked (ours only as its subreaper)

        # A root still running past its wait is TimeoutExpired, and the block's end kills it and its child. The
        # grandchild inherited out2.txt as its stdout, so the file is free only once the WHOLE tree is gone: before
        # kb/Work PB5500 kill() returned when the root was reaped and the grandchild could still hold the file
        # (merge group 43558438384: WinError 32 deleting out2.txt; 16 of 50 on a loaded host).
        out2 = os.path.join(d, "out2.txt")
        hold, pidfile2 = orphan_tree(d, "held")
        with open(out2, "w", encoding="utf-8", newline="\n") as out:
            job = ProcessJob.start(hold, stdout=out, stderr=out, stdin=subprocess.DEVNULL)
        with job:
            grand = read_pid(pidfile2, job)
            try:
                job.wait(1.0)
                timed_out = False
            except subprocess.TimeoutExpired:
                timed_out = True
            root = job.pid
        check("a root past its wait is TimeoutExpired", timed_out, True)
        check("the block's end kills a still-running root", pid_alive(root), False)
        check("the block's end returns once the grandchild is gone too", pid_alive(grand), False)
        try:
            os.remove(out2)
            freed = True
        except PermissionError as e:
            freed = f"{e}"
        check("the tree's output file is free once the block ends", freed, True)
        if sys.platform.startswith("linux") and proc_stat(grand):
            os.waitpid(grand, 0)

        # kill() returns only once every member is gone, and fails LOUDLY naming one the kill did not end. The tree's
        # kill primitive is stubbed to end the root alone, so the grandchild survives it: the pre-PB5500 kill()
        # returned normally here, the root being reaped.
        tree, pidfile3 = orphan_tree(d, "survivor")
        with open(os.path.join(d, "out3.txt"), "w", encoding="utf-8", newline="\n") as out:
            job = ProcessJob.start(tree, stdout=out, stderr=out, stdin=subprocess.DEVNULL)
        with job:
            grand = read_pid(pidfile3, job)
            job.KILL_BOUND_S = 0.5   # the survivor never dies, so the bound is reached on any host
            if IS_WINDOWS:
                real = _k32.TerminateJobObject
                _k32.TerminateJobObject = lambda _job, _code: (job.popen.kill(), True)[1]
            else:
                real = os.killpg
                os.killpg = lambda pg, sig: os.kill(pg, sig) if sig else real(pg, sig)
            try:
                job.kill()
                raised = "kill() returned with a member still running"
            except OSError as e:
                raised = str(grand) in str(e) or f"the error does not name pid {grand}: {e}"
            finally:
                if IS_WINDOWS:
                    _k32.TerminateJobObject = real
                else:
                    os.killpg = real
            check("a member the kill does not end fails kill() loudly, naming its pid", raised, True)
            del job.KILL_BOUND_S
        check("the real kill then ends the survivor", pid_alive(grand), False)
        if sys.platform.startswith("linux") and proc_stat(grand):
            os.waitpid(grand, 0)

    if IS_WINDOWS:
        bind_current()
        bind_current()
        check("bind_current is idempotent and holds one job", _CURRENT_JOB is not None, True)
    print(f"process_job self-test: {'GREEN' if not fails else 'RED — ' + ', '.join(fails)}")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(_self_test())
    print(__doc__)
    sys.exit(2)
