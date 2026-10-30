#!/usr/bin/env python3
"""detached.py — THE ONE WAY a script starts a process that must OUTLIVE ITS CALLER (kb/Work PB3536, PB3103, PB3117).

    from detached import spawn_detached
    pid = spawn_detached([sys.executable, "tool.py", ...], log_path, cwd=repo)
    python scripts/detached.py --self-test

Two callers need it: the loop's landing lane starts each `enqueue.py land` so that a supervisor tick that ends does not
end a landing mid-gate (kb/Work PB3103), and the gate driver starts every gate so that the agent or session that asked
for it can end without ending the gate (scripts/run_gate_legs.py, kb/Work PB3536). Measured 2026-10-22 (wave 1150
group A's review finisher): a workflow agent's return KILLS the process tree of every background command it started,
so its gate (run `20261022T225440Z-2797b8`) died mid-audit with no verdict, and the wave's only path was a new finisher
re-running the same gate at the same head. The harness kills by walking the process TREE (the agent's shell is in no
job object: `IsProcessInJob` is false there), so a process the caller started directly dies with it, whatever its
creation flags.

THE MECHANISM, both platforms: a short-lived LAUNCHER (this file, `--launch`) starts the target and exits at once, so
the target's parent is gone before anyone walks the caller's tree, and no walk from the caller reaches it.
  * Windows: the target gets CREATE_NO_WINDOW (a new console with no window, which every descendant inherits; never
    DETACHED_PROCESS, under which each console program it starts — git, pwsh, dotnet — opens a console WINDOW of its own
    that takes the desktop's focus, kb/Work PB3117) and CREATE_NEW_PROCESS_GROUP (no Ctrl+C from the caller's console),
    and CREATE_BREAKAWAY_FROM_JOB where the caller's job allows it (a job that refuses breakaway keeps it: a job kill
    still ends it, and that is said in the launcher's answer).
  * POSIX: the target starts a new session (no signal to the caller's process group reaches it) and is re-parented to
    init when the launcher exits.
The target's stdin is the null device and its stdout and stderr are APPENDED to `log`."""
from __future__ import annotations

import json
import os
import pathlib
import signal
import subprocess
import sys
import time
from standard_streams import ensure_utf8

#: The launcher's one answer line: `DETACHED <json>` with the target's pid and whether it left the caller's job.
ANSWER = "DETACHED "


def _launch(spec: dict) -> int:
    """The launcher: start the target as the module doc says, print its pid, exit."""
    log = pathlib.Path(spec["log"])
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open("ab") as out:
        kw: dict = {"stdin": subprocess.DEVNULL, "stdout": out, "stderr": subprocess.STDOUT, "close_fds": True,
                    "cwd": spec.get("cwd")}
        left_job = True
        if os.name == "nt":
            base = subprocess.CREATE_NO_WINDOW | subprocess.CREATE_NEW_PROCESS_GROUP
            try:
                p = subprocess.Popen(spec["argv"], creationflags=base | subprocess.CREATE_BREAKAWAY_FROM_JOB, **kw)
            except OSError:  # the caller's job refuses breakaway: the target stays in it
                p = subprocess.Popen(spec["argv"], creationflags=base, **kw)
                left_job = False
        else:
            p = subprocess.Popen(spec["argv"], start_new_session=True, **kw)
    print(ANSWER + json.dumps({"pid": p.pid, "left_job": left_job}), flush=True)
    return 0


def spawn_detached(argv: list[str], log: pathlib.Path, cwd: str | os.PathLike | None = None) -> int:
    """Start `argv` DETACHED from the caller (module doc), its output appended to `log`; returns its pid. Raises
    OSError when the launcher gives no pid (the target did not start)."""
    spec = {"argv": [str(a) for a in argv], "log": str(log), "cwd": str(cwd) if cwd is not None else None}
    flags = {"creationflags": subprocess.CREATE_NO_WINDOW} if os.name == "nt" else {}
    p = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "--launch", json.dumps(spec)],
                       stdin=subprocess.DEVNULL, capture_output=True, text=True, encoding="utf-8", errors="replace",
                       **flags)
    line = next((ln for ln in p.stdout.splitlines() if ln.startswith(ANSWER)), None)
    if p.returncode != 0 or line is None:
        raise OSError(f"the detached launcher did not start {argv[0]} (exit {p.returncode}): "
                      f"{(p.stdout + p.stderr).strip()[-400:]}")
    return int(json.loads(line[len(ANSWER):])["pid"])


def pid_alive(pid: int) -> bool:
    """Is process `pid` RUNNING? THE ONE liveness probe of the scripts (kb/Work PB4008): it asks the operating system
    whether the process has EXITED, never whether a process listing still names it. On Windows an exited process whose
    object another handle keeps (a `Popen` not yet closed) is signaled, and `WaitForSingleObject(h, 0)` says so; a
    snapshot can still list it under its image name, which once read as alive. On Linux a zombie still answers
    `kill(pid, 0)` and its /proc entry; it holds nothing, so it counts as dead."""
    if os.name == "nt":
        import ctypes  # noqa: PLC0415
        k32 = ctypes.WinDLL("kernel32", use_last_error=True)
        k32.OpenProcess.restype = ctypes.c_void_p
        h = k32.OpenProcess(0x00100000 | 0x1000, False, pid)  # SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION
        if not h:
            return False
        try:
            return k32.WaitForSingleObject(ctypes.c_void_p(h), 0) != 0  # 0 = WAIT_OBJECT_0: it has exited
        finally:
            k32.CloseHandle(ctypes.c_void_p(h))
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    try:
        with open(f"/proc/{pid}/stat", encoding="ascii") as fh:
            return fh.read().split(") ", 1)[1][:1] != "Z"
    except OSError:
        return True


def kill_tree(pid: int) -> None:
    """Kill `pid` and every process its TREE reaches, as the agent harness does when an agent returns: on Windows
    `taskkill /T /F` walks the tree by parent id; on POSIX the tree is `pid`'s process GROUP, so `pid` must lead one
    (start it with `start_new_session=True`, as the launcher here does). A `pid` already gone is no error. A LIVE `pid`
    that leads no group is the caller's defect, never "already gone": it is killed alone and ChildProcessError says so,
    since whatever it started lives on (kb/Work PB4072: the review lane started its reviewer with a plain Popen, this
    swallowed the ProcessLookupError, the hung reviewer was never killed on Linux, and the self-test that should have
    caught it waited out two 600 s sleeps and passed)."""
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(pid), "/T", "/F"], capture_output=True)
        return
    try:
        os.killpg(pid, signal.SIGKILL)
    except ProcessLookupError:
        if not pid_alive(pid):
            return
        os.kill(pid, signal.SIGKILL)
        raise ChildProcessError(f"kill_tree: process {pid} leads no process group (it was started without "
                                "start_new_session), so it alone was killed and what it started may live on "
                                "(kb/Work PB4072)") from None


def _wait_for(predicate, what: str, limit_s: float = 120.0) -> None:
    """Wait for `predicate`; a hang guard, never a verdict: the arm that needs it names `what` it never saw."""
    t = time.monotonic()
    while not predicate():
        if time.monotonic() - t > limit_s:
            raise TimeoutError(f"never saw {what}")
        time.sleep(0.05)


def self_test() -> int:
    from scratch_repo import remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    root = scratch_dir("detached-selftest-")
    survivor = None
    try:
        # 1. a detached process OUTLIVES a tree kill of its caller (the agent harness's kill, kb/Work PB3536): a caller
        #    process starts it, the caller's whole tree is killed, and the target still runs and still writes its log.
        mark, log, go = root / "target.pid", root / "target.log", root / "go"
        target = ("import os, pathlib, time\n"
                  f"pathlib.Path({str(mark)!r}).write_text(str(os.getpid()), encoding='utf-8')\n"
                  f"g = pathlib.Path({str(go)!r})\n"
                  "while not g.exists(): time.sleep(0.05)\n"
                  "print('outlived its caller', flush=True)\n")
        caller = subprocess.Popen(
            [sys.executable, "-c",
             "import sys, time\n"
             f"sys.path.insert(0, {str(pathlib.Path(__file__).resolve().parent)!r})\n"
             "from detached import spawn_detached\n"
             f"spawn_detached([sys.executable, '-c', {target!r}], __import__('pathlib').Path({str(log)!r}))\n"
             "time.sleep(3600)\n"],
            start_new_session=os.name != "nt")
        _wait_for(mark.exists, "the detached target's pid file")
        survivor = int(mark.read_text(encoding="utf-8"))
        kill_tree(caller.pid)
        caller.wait()
        alive_after_kill = pid_alive(survivor)
        go.touch()
        _wait_for(lambda: not pid_alive(survivor), "the target's exit")
        expect("a detached process outlives a tree kill of its caller and keeps writing its log",
               alive_after_kill and "outlived its caller" in log.read_text(encoding="utf-8", errors="replace"),
               f"alive {alive_after_kill}; log {log.read_text(encoding='utf-8', errors='replace')!r}")
        survivor = None

        # 2. a target that cannot start is the caller's error, never a pid of nothing
        try:
            spawn_detached([str(root / "no-such-program.exe")], root / "never.log")
            refused = False
        except OSError:
            refused = True
        expect("a target that cannot start raises OSError (the launcher's answer is missing)", refused)

        # 3. a launch opens no window (kb/Work PB3117): the target OWNS a console with no visible window, so every
        #    console program it starts (enqueue.py, git, pwsh, dotnet) shares it. Under DETACHED_PROCESS the target has
        #    no console (code page 0), and each program it starts gets a console of its own, a window that takes focus.
        if os.name == "nt":
            log3 = root / "console-probe.log"
            spawn_detached([sys.executable, "-c", "import ctypes; k = ctypes.windll.kernel32; u = ctypes.windll.user32; "
                            "h = k.GetConsoleWindow(); print('console-cp', k.GetConsoleCP(), 'visible', "
                            "1 if (h and u.IsWindowVisible(h)) else 0, flush=True)"], log3)
            _wait_for(lambda: "console-cp" in (log3.read_text(encoding="utf-8", errors="replace")
                                               if log3.exists() else ""), "the console probe's line")
            words = log3.read_text(encoding="utf-8", errors="replace").split()
            expect("a detached target owns a console with no visible window, so nothing it starts opens a window",
                   words[:4:2] == ["console-cp", "visible"] and words[1] != "0" and words[3] == "0", " ".join(words))

        # 4. kill_tree reaches the whole tree of a process that leads its own group, and never passes silently over one
        #    that leads none (kb/Work PB4072): POSIX only, where the tree is the process group.
        if os.name != "nt":
            sleeper = [sys.executable, "-c", "import time; time.sleep(600)"]
            lead = subprocess.Popen(sleeper, start_new_session=True)
            kill_tree(lead.pid)
            led_rc = lead.wait()
            plain = subprocess.Popen(sleeper)
            try:
                kill_tree(plain.pid)
                said = ""
            except ChildProcessError as exc:
                said = str(exc)
            plain_rc = plain.wait()
            try:
                kill_tree(plain.pid)
                gone = True
            except ChildProcessError:
                gone = False
            expect("kill_tree kills a process that leads its own group; one started without a session is killed alone "
                   "and ChildProcessError names the caller's defect (it never returns as if the process were gone); a "
                   "process already gone is no error",
                   led_rc == -signal.SIGKILL and plain_rc == -signal.SIGKILL and "leads no process group" in said and gone,
                   f"led {led_rc}, plain {plain_rc}, said {said!r}, gone {gone}")
    finally:
        if survivor is not None and pid_alive(survivor):
            kill_tree(survivor)
        remove_tree(root)
    print(f"=== detached.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:2] == ["--self-test"]:
        sys.exit(self_test())
    if len(sys.argv) == 3 and sys.argv[1] == "--launch":
        sys.exit(_launch(json.loads(sys.argv[2])))
    print(__doc__)
    sys.exit(2)
