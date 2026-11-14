#!/usr/bin/env python3
"""Cases of land_lane.py: the real launch path: a DETACHED runner records its attempt, a reused pid, a runner that died after commit (ported from the self-test of scripts/orchestrator/land_lane.py, DESIGN-orchestrator-loop.md
section 16.2, kb/Work PB5434). The cases run in name order over ONE world (`fixtures.world()`, bound by case_000), as the
sequential self-test did: a case reads what an earlier one planted (the module globals below).
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_detached_runner.py"""
from __future__ import annotations

import sys
import selftest_cases
from standard_streams import ensure_utf8

import fixtures
from scratch_repo import ScratchRepo
import coord
import os
import subprocess
import verdict_line
from fixtures import Fake, fresh, wait_runners

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
attempts = ll.attempts
runner_alive = ll.runner_alive
tick = ll.tick

# the world's state: bound by the cases in order, read by the later ones
head = root = wt = None


def case_000_a_detached_runner_runs_the_attempt_and_records_r(expect):
    global head, root, wt
    root = fixtures.world()

    # 10. the real launch path: a DETACHED runner records rc, the verdict and the end head of its argv.
    c10 = fresh("runner")
    runner_wt = ScratchRepo.init(root / "wt-runner")
    runner_wt.commit("x", allow_empty=True)
    wt, head = runner_wt.path, runner_wt.git("rev-parse", "HEAD")
    fake = [sys.executable, "-c",
            f"print({verdict_line.render('MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on x', 'w9a')!r}); raise SystemExit(5)"]
    r = {"name": "w9a", "state": "WAITING TO LAND", "tip": head, "worktree": str(wt), "why": "", "lead": None}
    out: list[str] = []
    tick(c10, root, survey=lambda repo, cdir: ([r], ["w9a"], None), argv_for=lambda row: fake, say=out.append)
    wait_runners(c10)
    a = (attempts(c10) or [{}])[0]
    expect("a detached runner runs the attempt and records rc 5, the verdict and the end head",
           a.get("rc") == 5 and a.get("verdict") == "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on x" and a.get("end_head") == head,
           f"{a} {out}")


def case_001_a_live_python_process_whose_command_line_cannot(expect):

    # 10b. PB3113: a pid the OS reused. A live Python process holds the recorded pid but is not the runner (its command
    # line does not name the attempt's record), so the attempt is not RUNNING; the process whose command line does
    # name the record is the runner. Both are real processes, started here and ended in the `finally`.
    c10b = fresh("reused-pid")
    rec10b = {"id": "000001-x", "branch": "w10b", "_path": str(c10b / "attempts" / "000001-x.json")}
    # Each sleeper says `ready` once Python runs, and the check waits for it: Popen returns when execve closes the
    # child's close-on-exec pipe, before Linux has set the new image's argument area, so /proc/<pid>/cmdline can still
    # read EMPTY (kb/Work PB3973), and an unreadable command line reads as alive: the stranger would look like a runner.
    sleeper = "import time; print('ready', flush=True); time.sleep(600)"
    stranger = subprocess.Popen([sys.executable, "-c", sleeper], stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                text=True)
    runner = subprocess.Popen([sys.executable, "-c", sleeper, "run", rec10b["_path"]], stdin=subprocess.DEVNULL,
                              stdout=subprocess.PIPE, text=True)
    try:
        for proc in (stranger, runner):
            proc.stdout.readline()
        expect("a live Python process that is not the runner holds the recorded pid (a reused id): not alive; the "
               "process whose command line names the record is",
               not runner_alive({**rec10b, "pid": stranger.pid}) and runner_alive({**rec10b, "pid": runner.pid}),
               f"stranger {runner_alive({**rec10b, 'pid': stranger.pid})} runner {runner_alive({**rec10b, 'pid': runner.pid})}")
    finally:
        for proc in (stranger, runner):
            proc.kill()
            proc.wait()
            proc.stdout.close()
    real_command = coord.process_command
    coord.process_command = lambda pid: ""          # a Python whose command line cannot be read: unknown, so alive
    try:
        unreadable = runner_alive({**rec10b, "pid": os.getpid()})
    finally:
        coord.process_command = real_command
    expect("a live Python process whose command line cannot be read is not called dead (it could be the runner)",
           unreadable)
    expect("a dead pid, no pid and no record path are not alive",
           not runner_alive({**rec10b, "pid": stranger.pid}) and not runner_alive({**rec10b, "pid": None})
           and not runner_alive({"pid": runner.pid}))


def case_002_a_runner_that_died_after_committing_is_retried_f(expect):

    # 11. a runner that died AFTER enqueue.py committed: the survey reads the branch IN FLIGHT at its new tip, and
    #     the lane retries it from its own record (the DIED attempt's end head is the worktree's tip at the reap).
    c11 = fresh("died-after-commit")
    f11 = Fake([{"name": "w9b", "state": "WAITING TO LAND", "tip": "before-merge", "worktree": str(wt), "why": "",
                 "lead": None}])
    f11.tick(c11)
    f11.live.clear()
    f11.rows[0] = {**f11.rows[0], "state": "IN FLIGHT", "tip": head}
    expect("a runner that died after committing is retried from its record at the branch's new tip",
           f11.tick(c11) == ["w9b"] and attempts(c11)[0].get("end_head") == head,
           f"{f11.started} {attempts(c11)[0]}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
