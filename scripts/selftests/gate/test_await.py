#!/usr/bin/env python3
"""`--await` and a detached gate's follower (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import datetime as dt
import json
import os
import re
import subprocess
import sys
import time
from detached import kill_tree, pid_alive, spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536), THE one tree kill (PB4072), THE one liveness probe (PB4008)
from gate_slot import (ExclusiveLock, GateSlots, Heartbeat, QueueAbandoned, Slot,  # noqa: E402
                       gate_class_from_env)
from pathlib import Path

import fixtures
import selftest_cases
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
from gate_runs import (AWAIT_PENDING, AWAIT_PENDING_EXIT, DIED, RUN_LABEL, RUN_ROOT, STARTING, VERDICT_FILE,
                       gate_phase, gate_running, launch_and_follow, lock_of_run, lock_running, read_pid, read_spawn,
                       resolve_run, watch, write_pid, write_spawn, _record_exit)
from gate_verdict import BUSY, EXIT_BUSY, NOT_RUN_CAUSES, VERDICT, not_run, read_verdict
from gate_host import Host
REPO = gate_mod.REPO


def case_00_await_reads_verdict_file_line_exit_code(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "await"
    d.mkdir()
    done = d / "done"
    done.mkdir()
    (done / VERDICT_FILE).write_text(json.dumps({"verdict": "RED", "line": f"{VERDICT}RED — planted ===",
                                                 "exit_code": 1}), encoding="utf-8", newline="\n")
    live = d / "live"
    live.mkdir()
    write_pid(live, os.getpid())  # a pid that is ALIVE for the whole arm (this process)
    dead = d / "dead"             # a gate that died with no verdict: its pid since taken by a live process (this
    dead.mkdir()                  # one), the very case Windows and Linux both produce when they reuse a pid
    write_pid(dead, os.getpid())
    stamp_now = f"{dt.datetime.now(dt.timezone.utc):%Y%m%dT%H%M%SZ}"
    fresh = d / f"000090-{stamp_now}"   # a gate that has not taken its lock yet, so wrote no pid (kb/Work PB3707)
    fresh.mkdir()
    spawned = d / f"000091-{stamp_now}"  # ... and one whose launcher recorded the live child it spawned
    spawned.mkdir()
    write_spawn(spawned, os.getpid())
    child = subprocess.Popen([sys.executable, "-c", "pass"])
    child.wait()
    orphan = d / f"000092-{stamp_now}"   # ... a young run whose spawned child is gone: died before its lock
    orphan.mkdir()
    write_spawn(orphan, child.pid)
    aged = d / "000093-20200101T000000Z"  # ... a run with no pid, no verdict, long past the start-up window
    aged.mkdir()
    bogus = d / "000094-20261399T000000Z"  # ... and one whose name's stamp is no date at all: no age, never starting
    bogus.mkdir()
    wlock = d / "worktree.lock"
    with ExclusiveLock.try_take(wlock, f"gate {d} (pid 7, since T{RUN_LABEL}{live})"):   # the gate that runs `live`
        got = {n: watch(d / n, 0, lock_running(d / n, wlock))
               for n in ("done", "live", "dead", fresh.name, spawned.name, orphan.name, aged.name, "missing")}
        other = watch(dead, 0, lock_running(dead, wlock))   # the lock is held, but by another run's gate
    phases = {n: gate_phase(d / n, None) for n in (fresh.name, spawned.name, orphan.name, aged.name, bogus.name)}
    expect("--await reads the verdict file (its line and exit code), PENDING (its own exit, never BUSY's) while the "
        "run's gate holds the gate lock (or is STARTING: spawned moments ago, its child alive, no lock yet), NOT RUN "
        "[verdict] (exit 2, BRANCH) when it DIED without a verdict EVEN WHEN gate.pid names a live process (a "
        "reused pid) or another run's gate holds the lock, and when a run with no pid is past the start-up window or "
        "its spawned child is gone, NOT RUN [run] (exit 2, ENVIRONMENT) with no run directory",
        got["done"] == (1, f"{VERDICT}RED — planted ===")
        and got["live"][0] == got[fresh.name][0] == got[spawned.name][0] == AWAIT_PENDING_EXIT != EXIT_BUSY
        and got["live"][1].startswith(AWAIT_PENDING) and "STARTING" in got[fresh.name][1]
        and phases == {fresh.name: STARTING, spawned.name: STARTING, orphan.name: DIED, aged.name: DIED, bogus.name: DIED}
        and got[orphan.name][0] == got[aged.name][0] == 2 and "DIED" in got[orphan.name][1]
        and "DIED" in got[aged.name][1]
        and got["dead"][0] == other[0] == 2 and read_verdict(got["dead"][1][len(VERDICT):]) == not_run("verdict")
        and "DIED" in got["dead"][1] and "DIED" in other[1]
        and got["missing"][0] == 2 and read_verdict(got["missing"][1][len(VERDICT):]) == not_run("run"), str(got))
    expect("a run directory outside any worktree's run root, or of a worktree that is gone, has no gate lock to ask (no "
        "gate can run there); a run of the repository's own checkout asks that checkout's gate lock",
        lock_of_run(d / "dead") is None and lock_of_run(root / "gone" / RUN_ROOT / "r") is None
        and lock_of_run(REPO / RUN_ROOT / "r") == Host(REPO).lock_path(),
        f"{lock_of_run(d / 'dead')} {lock_of_run(root / 'gone' / RUN_ROOT / 'r')}")
    cwd = Path.cwd()
    os.chdir(live)   # a reader elsewhere: the handed name means the worktree's run, never one of the reader's own
    try:
        from_elsewhere = [watch(r, 0, gate_running(r))[0]
                          for r in (resolve_run(Path("await") / "done", w) for w in (root, None))]
        os.chdir(d)
        relative_here = watch(r := resolve_run("done", None), 0, gate_running(r))[0]
    finally:
        os.chdir(cwd)
    expect("a relative run name resolves against the worktree that gated it, wherever the reader runs; an absolute one "
        "is itself; with no worktree, the current directory",
        resolve_run(done, root / "elsewhere") == done and from_elsewhere == [1, 2] and relative_here == 1
        and resolve_run("TestResults/build-local/r", root / "wt") == root / "wt" / "TestResults" / "build-local" / "r",
        f"{from_elsewhere} {relative_here}")
    expect("a record with no exit code takes it from its verdict through the one verdict reader",
        [_record_exit({"verdict": v}) for v in ("LEG 1 ONLY (batched gating, PB2515): GREEN", "GREEN", BUSY, "NOT RUN [driver]", "RED")]
        == [0, 0, EXIT_BUSY, 2, 1])
    plock = root / "planted-gate.lock"
    def planted_gate(run: Path, go: Path | None = None) -> list[str]:
        wait = f"gof = pathlib.Path({str(go)!r})\nwhile not gof.exists(): time.sleep(0.05)\n" if go else ""
        return [sys.executable, "-c", "import json, os, pathlib, sys, time\n"
                f"sys.path.insert(0, {str(REPO / 'scripts')!r})\nimport gate as g\n"
                f"r = pathlib.Path({str(run)!r})\n"
                f"lock = g.ExclusiveLock.try_take(pathlib.Path({str(plock)!r}), "
                f"'gate x (pid %d, since T{RUN_LABEL}%s)' % (os.getpid(), r))\n"
                "g.write_pid(r, os.getpid())\nprint('PLANTED-GATE-OUTPUT', flush=True)\n"
                f"{wait}"
                f"line = {VERDICT!r} + 'RED — planted ==='\n"
                "(r / 'verdict.json').write_text(json.dumps({'verdict': 'x', 'line': line, 'exit_code': 1}))\n"
                "lock.release()\nprint(line, flush=True)\n"]
    d = root / "follow"
    d.mkdir()
    heard: list[str] = []
    runs = iter([d / "r1"])
    def make() -> Path:
        r = next(runs)
        r.mkdir()
        return r
    code = launch_and_follow(make, planted_gate, heard.append)
    expect("build-local's gate runs DETACHED and is followed: its output echoed, the caller's exit code the gate's, "
        "the verdict line last",
        code == 1 and "PLANTED-GATE-OUTPUT" in heard and heard[-1].startswith(f"{VERDICT}RED — planted")
        and read_pid(d / "r1") is not None, str(heard))
    stillborn = d / f"000002-{dt.datetime.now(dt.timezone.utc):%Y%m%dT%H%M%SZ}"
    def make_stillborn() -> Path:
        stillborn.mkdir()
        return stillborn
    said: list[str] = []
    code = launch_and_follow(make_stillborn, lambda r: [sys.executable, "-c", "pass"], said.append)
    named = re.search(r"\(pid (\d+), run directory", " ".join(said))
    expect("the launcher records the child it spawned, and a spawned child that ends before taking the gate lock is a "
        "gate that DIED (NOT RUN [verdict], exit 2) the moment it ends, however young the run is",
        code == 2 and named is not None and read_spawn(stillborn) == int(named.group(1))
        and gate_phase(stillborn, None) == DIED and "DIED" in said[-1], f"{code} {said} {read_spawn(stillborn)}")
    refused: list[str] = []
    def unmade() -> Path:
        raise TimeoutError("the run root's lock stayed held")
    code = launch_and_follow(unmade, planted_gate, refused.append)
    expect("a run directory that cannot be made (an unwritable run root, a hung holder of its lock) still ends in a "
        "verdict line, NOT RUN [launch], exit 2, and starts no gate",
        code == 2 and refused[-1].startswith(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — ")
        and "PLANTED-GATE-OUTPUT" not in refused, str(refused))
    go, run2 = d / "go", d / "r2"
    follower = subprocess.Popen([sys.executable, "-c", "\n".join([
        "import pathlib, sys",
        f"sys.path.insert(0, {str(REPO / 'scripts')!r})",
        "import gate as g",
        f"run = pathlib.Path({str(run2)!r})",
        "def make():",
        "    run.mkdir()",
        "    return run",
        f"sys.exit(g.launch_and_follow(make, lambda r: {planted_gate(run2, go)!r}, print))"])],
        stdout=subprocess.DEVNULL, start_new_session=os.name != "nt")
    t = time.monotonic()
    while read_pid(run2) is None and time.monotonic() - t < 120:  # a hang guard: the arm names what it never saw
        time.sleep(0.05)
    gate_pid = read_pid(run2)
    kill_tree(follower.pid)
    follower.wait()
    survived = gate_pid is not None and pid_alive(gate_pid)
    go.touch()
    handed = watch(run2, 120, lock_running(run2, plock))
    expect("a detached gate OUTLIVES its follower's tree kill and its verdict is read from the run directory",
        survived and handed == (1, f"{VERDICT}RED — planted ==="),
        f"survived {survived}; {handed}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
