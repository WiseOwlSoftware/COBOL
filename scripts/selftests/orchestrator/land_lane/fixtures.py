#!/usr/bin/env python3
"""The landing lane's planted worlds (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5434): `world()` makes the scratch
directory every case of a process shares, `fresh` a coordination directory in it, `row` a survey row, `Fake` the planted survey,
recording launcher and liveness oracle (no git, no GitHub, no process), `finish` the runner's own completion of an attempt and
`wait_runners` the wait for every real runner to exit. No case and no expectation lives here."""
from __future__ import annotations

import selftest_cases

from typing import Any
import datetime as dt
import json
import pathlib
import time
from scratch_repo import scratch_dir

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
RedFacts = ll.RedFacts
WatchEffects = ll.WatchEffects
attempts = ll.attempts
finished = ll.finished
now_utc = ll.now_utc
runner_alive = ll.runner_alive
tick = ll.tick
update = ll.update

root: pathlib.Path | None = None   # the process's scratch directory, set by `world()`


def world() -> pathlib.Path:
    """The scratch directory of this process's cases (made once: a case file's cases share it)."""
    global root
    if root is None:
        root = scratch_dir("land-lane-selftest-")
    return root


def fresh(name: str) -> pathlib.Path:
    c = root / name
    (c / "scratch").mkdir(parents=True)
    return c


def row(name: str, state: str, tip: str) -> dict[str, Any]:
    return {"name": name, "state": state, "tip": tip, "worktree": str(root / f"wt-{name}"), "why": "",
            "lead": None}


class Fake:
    """A planted survey, a recording launcher and a liveness oracle: no git, no GitHub, no process."""
    def __init__(self, rows: list[dict[str, Any]]):
        self.rows, self.started, self.live, self.free = rows, [], set(), False
        self.asked: list[str] = []
        self.wait = self.overlap                   # what a retry-when-free exit waits on: planted, or the real one
        self.prs: dict[str, Any] | None = None     # GitHub's facts: unobserved unless a case plants them
        self.now: dt.datetime | None = None
        self.effects: list[tuple] = []
        self.reds: dict[str, RedFacts] = {}        # each bounced branch's planted facts (red_facts)
        self.held: dict[str, dict] = {}            # the open causes (open_causes)

    def survey(self, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list, list[str], dict | None]:
        return self.rows, [r["name"] for r in self.rows if r["state"] == "WAITING TO LAND"], self.prs

    def watch_effects(self) -> WatchEffects:
        rec = self.effects.append
        return WatchEffects(rerun=lambda repo, run: rec(("rerun", repo, run)) or (True, "re-run requested"),
                            requeue=lambda repo, pr, head: rec(("requeue", repo, pr)) or (True, "requeued"),
                            alert=lambda subject, body, ref: rec(("alert", subject, body)),
                            measure=lambda pr, b, read, cause, act, ok: rec(("measure", pr, b, read, cause, act)),
                            red_facts=lambda att, pr: self.reds.get(att["branch"], RedFacts(unobserved="planted")),
                            measure_red=lambda pr, b, run, cls, why, act, ok, cases, cause: rec(("red", pr, b, run, cls, act, ok)),
                            open_causes=lambda: self.held, cause_note=lambda cases: "PB5478")

    def start(self, argv: list[str], log: pathlib.Path) -> int:
        self.started.append(json.loads(pathlib.Path(argv[-1]).read_text(encoding="utf-8"))["branch"])
        pid = 40000 + len(self.started)
        self.live.add(pid)
        return pid

    def alive(self, a: dict[str, Any]) -> bool:
        return a.get("pid") in self.live

    def overlap(self, rc: int | None, wt: str) -> tuple[bool, str]:
        self.asked.append(wt)
        return self.free, "shares scripts/x.py with w1069g (dispatched earlier)"

    def tick(self, cdir: pathlib.Path) -> list[str]:
        before = len(self.started)
        out: list[str] = []
        tick(cdir, root, survey=self.survey, start=self.start, alive=self.alive, wait_free=self.wait,
             effects=self.watch_effects(), now=self.now, say=out.append)
        self.said = out
        return self.started[before:]


def finish(cdir: pathlib.Path, branch: str, rc: int, verdict: str, end_head: str | None = None) -> None:
    """The runner's own completion of the branch's running attempt (the record `run_attempt` writes)."""
    for a in attempts(cdir):
        if a["branch"] == branch and not finished(a):
            update(a, {"rc": rc, "verdict": verdict, "ended": now_utc().isoformat(),
                       "end_head": end_head or a["head"]})


def wait_runners(cdir: pathlib.Path) -> None:
    """Wait until every runner an attempt names has EXITED: bounded by the runner's own exit (its outcome is
    written before it exits), never by a fixed wall-clock ceiling (kb/Work PB3022; PB3640 review R1-5)."""
    while any(runner_alive(a) for a in attempts(cdir)):
        time.sleep(0.2)
