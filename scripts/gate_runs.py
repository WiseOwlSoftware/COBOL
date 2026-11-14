#!/usr/bin/env python3
"""gate_runs.py - THE GATE'S RUN STORE AND THE AWAIT (kb/Work PB5433, PB3536, PB3707; docs/rearchitecture/DESIGN-orchestrator-loop.md section
16.5): a run's directory, its record, pid, spawn and lock, how a run is named, found, kept and read, and `watch`, the await that
`gate.py --await` and `build-local` follow a detached gate through."""
from __future__ import annotations

import datetime as dt
import json
import os
import re
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
from gate_slot import ExclusiveLock  # noqa: E402
from detached import pid_alive, spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536), THE one tree kill (PB4072), THE one liveness probe (PB4008)
from gate_verdict import BUSY, EXIT_BUSY, GREEN_READING, NOT_RUN, NOT_RUN_CAUSES, VERDICT, read_verdict  # noqa: E402
from gate_host import Host  # noqa: E402


RUN_ROOT = Path("TestResults") / "build-local"


#: A run directory's name: `<n>-<UTC start, to the second>Z`, where n is the run's NUMBER in its run root, one more
#: than every run there, taken under the run root's lock (`Gate.new_run`). THE ORDER OF RUNS IS THE ORDER OF THEIR
#: NUMBERS (`run_order`), and "the newest earlier run" reads it: the first classes, the reuse rule, the
#: pruning. Never the clock's: a stamp orders nothing finer than the clock's resolution, and on a CI runner three runs
#: started in the same MICROSECOND, so a stamp-and-nonce name sorted by its random nonce (kb/Work PB3536, merge-group
#: run 39888287569). A run named before the numbers (`<stamp>Z-<nonce>`) is older than every numbered run, and is still
#: read and pruned.
RUN_DIR = re.compile(r"(?P<n>\d{6,})-\d{8}T\d{6}Z|\d{8}T\d{6}Z-[0-9a-f]{6}")


RUNS_LOCK = "runs.lock"


RUNS_LOCK_WAIT_S = 60.0  # the lock is held for one directory listing and one mkdir; a minute means a hung holder


KEEP_RUNS = 5


VERDICT_FILE = "verdict.json"


#: A run directory's other fixed names (kb/Work PB3536): the gate process's pid, and its output when it runs detached.
PID_FILE = "gate.pid"


#: ⛔ THE STARTING STATE OF A DETACHED GATE (kb/Work PB3707). A run directory exists before its child process has taken the
#: gate lock and written `gate.pid` (interpreter start, imports), and in that window neither the lock nor the pid file says
#: a gate is coming. The launcher therefore records the child it spawned in `gate.spawn` (the child's pid), and the
#: child's first act under the lock (`write_pid`) replaces it with `gate.pid`. A reader (`gate_phase`) decides a run with
#: no pid by THREE facts: the run is younger than START_WINDOW_S (its name's own stamp), and the spawn record, when it
#: has one, names a process that is alive. A pid alone is never trusted (Windows reuses them, kb/Work PB3662 R1-1): the
#: window bounds a reused one to START_WINDOW_S of a wrong "starting".
SPAWN_FILE = "gate.spawn"


#: The longest a spawned gate may take to reach its lock (interpreter start and imports; measured in single seconds, even
#: with three builds running). Past it, a run with no pid and no verdict is a gate that DIED.
START_WINDOW_S = 60.0


RUNNING, STARTING, DIED = "running", "starting", "died"


#: What a detached gate's lock label says before the run directory it writes (`Gate.run`; `live_gate` reads it back).
RUN_LABEL = ", run "


LOG_FILE = "gate.log"


#: `--await`'s answer while the gate still runs, and its default window (under a tool call's 10-minute limit).
AWAIT_PENDING = "=== GATE AWAIT: PENDING"


AWAIT_PENDING_EXIT = 4


AWAIT_WAIT_S = 540


#: The verdict record's schema: 3 adds the measured head, tree (start and end), base, exit code and reuse (PB3536); 4 adds
#: `head_tree` and `lane`, which the landing requires as evidence (PB3612; PB3660). The Linux half's `linux` field that
#: PB3612 also added is gone (kb/Work PB4327: the Linux half left the per-change gate); no landing reads it, so a record
#: that still carries it is read as before. Schema 5 (kb/Work PB4328): the gate runs the whole population, so a record
#: of schema 4 or below may be a `LEG 1 ONLY` one, and the reuse rule (`PassCarry.reusable`) never answers from one; it drops
#: the plan's, the scope's, the identity check's and the timings store's fields, which no reader read.
RECORD_SCHEMA = 5


#: The first schema a gate that ran the WHOLE population writes (kb/Work PB4328): the oldest record `PassCarry.reusable` answers from.
SCHEMA_WHOLE = 5


#: THE NOTE THAT INTRODUCED EACH RECORD SCHEMA, next to the number (kb/Work PB3717): `predates_landing` words "this gate
#: predates kb/Work <note>" from this table, never from a note id typed into the sentence, so the next bump of
#: `RECORD_SCHEMA` cannot leave a refusal naming the previous change. A bump adds its row here (the self-test fails
#: while `RECORD_SCHEMA` has none).
SCHEMA_NOTES = {3: "PB3536", 4: "PB3612", 5: "PB4328"}


#: THE OLDEST RECORD SCHEMA THE LANDING READS AS EVIDENCE, AND THE FIELDS IT REQUIRES (`enqueue.tested_locally`, kb/Work
#: PB3660). The record is written by the BRANCH's driver and read by main's lander, so a field the lander starts to require
#: strands every branch gated before it, and only the record itself can tell that from a dirty tree. Schema 3 was written
#: both before and after PB3612 added `head_tree` (the number was not bumped: PB3660), so for schema 3 the
#: fields themselves are the discriminator; from schema 4 on the number is. Whenever the lander requires a field a record
#: did not carry: raise `RECORD_SCHEMA` and `LANDABLE_SCHEMA` and add the field to `LANDING_FIELDS` (the self-test pins
#: RECORD_SCHEMA >= LANDABLE_SCHEMA, every landing field to a key `Gate._record` writes, and the lander's planted records to
#: the current schema).
LANDABLE_SCHEMA = 3


LANDING_FIELDS = ("head_tree",)


def write_pid(run: Path, pid: int) -> None:
    """The gate's own pid, written UNDER its gate lock; it replaces the launcher's spawn record (`write_spawn`)."""
    (run / PID_FILE).write_text(json.dumps({"pid": pid}) + "\n", encoding="utf-8", newline="\n")
    try:
        (run / SPAWN_FILE).unlink(missing_ok=True)
    except OSError:  # the launcher has the record open this instant (Windows): it lingers, and the pid file outranks it
        pass


def read_pid(run: Path) -> int | None:
    return _read_pid_file(run / PID_FILE)


def write_spawn(run: Path, pid: int) -> None:
    """The launcher's record of the child it spawned for `run`, until the child's `write_pid` replaces it (PB3707)."""
    (run / SPAWN_FILE).write_text(json.dumps({"pid": pid}) + "\n", encoding="utf-8", newline="\n")


def read_spawn(run: Path) -> int | None:
    return _read_pid_file(run / SPAWN_FILE)


def _read_pid_file(path: Path) -> int | None:
    try:
        return int(json.loads(path.read_text(encoding="utf-8"))["pid"])
    except (OSError, ValueError, KeyError, TypeError):
        return None


_RUN_STAMP = re.compile(r"\d{8}T\d{6}Z")


def run_age_s(run: Path, now: float | None = None) -> float | None:
    """Seconds since the run directory was made, read from the UTC stamp its name carries (`Gate.new_run`; both name
    forms of RUN_DIR have one), or None for a name with none."""
    m = _RUN_STAMP.search(run.name)
    if m is None:
        return None
    try:
        made = dt.datetime.strptime(m.group(), "%Y%m%dT%H%M%SZ").replace(tzinfo=dt.timezone.utc).timestamp()
    except ValueError:  # digits that are no date (a hand-made name): no age, so never "starting"
        return None
    return (time.time() if now is None else now) - made


def gate_starting(run: Path, now: float | None = None) -> bool:
    """A detached gate that has been spawned and has not yet taken its lock (kb/Work PB3707): the run has no `gate.pid`, is
    younger than START_WINDOW_S, and its spawn record, when the launcher has written one, names a live process. A run with
    a pid is past starting; one past the window, or whose spawned child is gone, is a gate that DIED before its lock."""
    if read_pid(run) is not None:
        return False
    age = run_age_s(run, now)
    if age is None or not -START_WINDOW_S < age < START_WINDOW_S:   # a stamp from the future is a clock step, not a start
        return False
    spawned = read_spawn(run)
    return spawned is None or pid_alive(spawned)


def read_record(run: Path) -> dict | None:
    """The run's verdict record, or None while it has none (a verdict file is written whole, once, at the end)."""
    try:
        rec = json.loads((run / VERDICT_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    return rec if isinstance(rec, dict) and "verdict" in rec and "line" in rec else None


def record_schema(rec: dict) -> int:
    """A record's schema number; a record written before the field existed (or with a malformed one) is schema 0."""
    value = rec.get("schema")
    return value if isinstance(value, int) and not isinstance(value, bool) else 0


def predates_landing(rec: dict) -> str | None:
    """Why a record cannot be evidence to the landing although its verdict may be GREEN (kb/Work PB3660): it was written by
    a gate older than the lander's reading (schema below `LANDABLE_SCHEMA`, or a field of `LANDING_FIELDS` absent), so a
    "missing field" the lander finds is the gate's age, never the tree's state. None when the record is current."""
    have = record_schema(rec)
    missing = [f for f in LANDING_FIELDS if f not in rec]
    if have >= LANDABLE_SCHEMA and not missing:
        return None
    return (f"it was written by a gate older than this lander reads (record schema {have}, "
            f"{'no ' + '/'.join(missing) + ' field' if missing else 'below ' + str(LANDABLE_SCHEMA)}: the branch's "
            f"scripts/gate.py predates kb/Work {SCHEMA_NOTES[RECORD_SCHEMA]})")


def _record_exit(rec: dict) -> int:
    """A record's exit code; a record written before schema 3 carries none, so it is derived from its verdict through
    the one verdict reader."""
    if isinstance(rec.get("exit_code"), int):
        return rec["exit_code"]
    word = read_verdict(str(rec.get("verdict", ""))).word
    return {GREEN_READING.word: 0, BUSY: EXIT_BUSY, NOT_RUN: 2}.get(word, 1)


def resolve_run(run: str | os.PathLike, worktree: str | os.PathLike | None) -> Path:
    """THE ONE RESOLUTION of a handed gate run's name (kb/Work PB3536, review R1-1): an absolute path is itself; a
    relative one (`TestResults/build-local/<run>`, the form an agent returns) names a run of the WORKTREE that gated it,
    never of the reader's checkout: the rolling wave and the planner read it from the main checkout. With no worktree a
    relative name is the current directory's."""
    p = Path(run)
    return p if p.is_absolute() else (Path(worktree) if worktree else Path.cwd()) / p


def lock_of_run(run: Path) -> Path | None:
    """The gate lock of the worktree a run directory belongs to (`<worktree>/TestResults/build-local/<run>`), or None when
    the run is not under a worktree's run root or that worktree is gone: no gate can run there."""
    wt = run.parent.parent.parent
    if run.parent != wt / RUN_ROOT or not wt.is_dir():
        return None
    try:
        return Host(wt).lock_path()
    except (OSError, subprocess.CalledProcessError):
        return None


def gate_phase(run: Path, live: "LiveGate | None") -> str:
    """WHERE A RUN'S GATE IS (kb/Work PB3662, PB3707), from the worktree's `live_gate` reading (None: no gate lock is
    held): RUNNING when the lock is held by a gate that writes this run (or by one whose label names none), never by a
    process id, which the OS reuses; STARTING when no lock is held for it yet but it was spawned moments ago and its
    child is alive (`gate_starting`); else DIED, a gate that ended or never got its lock, with no verdict."""
    if live is not None and (live.run is None or live.run.name == run.name):
        return RUNNING
    return STARTING if gate_starting(run) else DIED


def gate_running(run: Path) -> Callable[[], bool]:
    """THE READER'S TEST OF "THIS RUN'S GATE IS STILL RUNNING" (kb/Work PB3662, review R1-1 of wave 20261104): the gate
    lock (`live_gate`) is held by a gate that writes this run, never a process id, which the OS reuses: a reader that
    arrives after a reboot or a long death (the planner's survey of a handed-over gate, `--await` from the rolling wave)
    must not read a stranger holding the dead gate's pid as the gate. A run whose gate has not taken its lock yet is
    STARTING (`gate_phase`, kb/Work PB3707) and reads as running for START_WINDOW_S at most; a run from before that
    window, or whose spawned child is gone, is a gate that died."""
    return lock_running(run, lock_of_run(run))


def lock_running(run: Path, lock: Path | None) -> Callable[[], bool]:
    """`gate_running` over a named gate lock (None: no gate can run there)."""
    def running() -> bool:
        return gate_phase(run, live_gate(lock) if lock is not None else None) != DIED
    return running


def watch(run: Path, wait_s: float | None, running: Callable[[], bool], echo: Callable[[str], None] | None = None,
          poll_s: float = 0.5) -> tuple[int, str]:
    """THE ONE WAY TO WAIT FOR A GATE (kb/Work PB3536): its run directory's VERDICT FILE, never a log's tail.
    `running()` says whether the gate that writes the run still runs: `gate_running(run)` for every reader of a gate it
    did not start (the lock, never a reused pid), the follower's own child's liveness for the gate it just launched.
    Returns (exit code, verdict line): the record's own once `verdict.json` is there; NOT RUN [verdict] (2) when the
    gate DIED without one; PENDING (AWAIT_PENDING_EXIT) once `wait_s` passes (None: no limit); NOT
    RUN [run] (2) when there is no such run directory (`resolve_run` makes a worktree-relative name absolute first). `echo` (the follower) is handed the run's `gate.log` line by line as it grows, to its end."""
    log, offset, t = run / LOG_FILE, 0, time.monotonic()

    def drain(final: bool = False) -> None:
        nonlocal offset
        if echo is None:
            return
        try:
            data = log.read_bytes()[offset:]
        except OSError:
            return
        cut = len(data) if final else data.rfind(b"\n") + 1
        for line in data[:cut].decode("utf-8", errors="replace").splitlines():
            echo(line)
        offset += cut

    if not run.is_dir():
        return 2, f"{VERDICT}{NOT_RUN_CAUSES['run'].verdict} — no gate run directory {run} ==="
    while True:
        drain()
        rec = read_record(run)
        pid = read_pid(run)
        if rec is not None:
            # the gate writes its record, then prints the line and exits: the follower echoes its output to the end
            while echo is not None and running():
                drain()
                time.sleep(poll_s)
            drain(final=True)
            return _record_exit(rec), str(rec["line"])
        if not running():
            if (rec := read_record(run)) is not None:  # it wrote the record and ended between the two reads
                continue
            drain(final=True)
            return 2, (f"{VERDICT}{NOT_RUN_CAUSES['verdict'].verdict} — the gate "
                       f"{f'process {pid} ' if pid is not None else ''}DIED with no verdict "
                       f"(run {run}; its output: {log}) ===")
        if wait_s is not None and time.monotonic() - t >= wait_s:
            who = (f"the gate process {pid} is running" if pid is not None else
                   f"no {PID_FILE} names its process yet: it was spawned moments ago and has not taken its gate lock "
                   f"(STARTING, kb/Work PB3707; {START_WINDOW_S:.0f} s at most, then it reads as DIED)")
            return AWAIT_PENDING_EXIT, f"{AWAIT_PENDING} — no verdict in {run} after {wait_s:.0f} s; {who}; await it again ==="
        time.sleep(poll_s)


def launch_and_follow(new_run: Callable[[], Path], argv_for: Callable[[Path], list[str]],
                      say: Callable[[str], None]) -> int:
    """`build-local`'s gate (kb/Work PB3536): make the run directory, start the gate DETACHED in it (scripts/detached.py:
    it outlives this process and whatever kills this process's tree), name it, and follow it to its verdict. Returns the
    gate's exit code. `argv_for(run)` is the gate's command line; the self-test plants its own."""
    try:
        run = new_run()
    except OSError as e:  # an unwritable run root, or a hung holder of its lock (Gate.new_run): still a verdict line
        say(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — the gate's run directory could not be made: {e} ===")
        return 2
    try:
        pid = spawn_detached(argv_for(run), run / LOG_FILE, cwd=REPO)
    except OSError as e:
        say(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — the gate could not be started detached: {e} ===")
        return 2
    # the gate writes its own gate.pid UNDER its lock (`Gate.run`), so a reader that finds a pid file knows the gate took the
    # lock: the launcher writes none, but it records the child it spawned (`write_spawn`, kb/Work PB3707), so a reader in the
    # window before the lock reads a young run with a live child as STARTING (`gate_phase`), never as a gate that died. The
    # run is young from the moment its directory exists (its name's stamp), so no gap lies between the two. The follower
    # alone has watched its child since the spawn, so its liveness is the child's.
    try:
        write_spawn(run, pid)
    except OSError as e:  # the record only shortens a dead child's STARTING; the gate runs and is followed without it
        say(f"build-local: ⚠ the spawned gate's pid could not be recorded in {run} ({e}); a reader reads the run as "
            f"STARTING for {START_WINDOW_S:.0f} s whether or not it dies")
    say(f"build-local: the gate runs DETACHED (pid {pid}, run directory {run}): it outlives this process; "
        f"`python scripts/gate.py --await {run}` reads its verdict (kb/Work PB3536)")
    code, line = watch(run, None, lambda: pid_alive(pid), echo=say)
    if not (run / LOG_FILE).exists() or line not in (run / LOG_FILE).read_text(encoding="utf-8", errors="replace"):
        say(line)  # a verdict the gate's own output does not carry (DIED): the caller's last line is still the verdict
    return code


#: THE ORIGINAL RECORD OF A RE-DERIVED RUN (kb/Work PB5205; `PassCarry.rederive`): the verdict file as its gate wrote it, kept
#: beside the re-derived one and never overwritten.
ORIGINAL_VERDICT_FILE = "verdict.original.json"


def run_started(run: Path) -> float | None:
    """WHEN A RUN'S GATE STARTED, by the filesystem's clock (kb/Work PB5205): the modification time of its `gate.pid`, which
    the gate writes under its lock BEFORE it measures the tree (`Gate.run`). The filesystem's clock, never the run name's
    stamp (the driver's clock, which a self-test moves): the window it opens is compared with the hosts' file times and
    HEAD's reflog, which are the same real clock. None when the run has no pid file."""
    try:
        return (run / PID_FILE).stat().st_mtime
    except OSError:
        return None


def measuring_runs(run: Path) -> list[Path]:
    """THE RUN DIRECTORIES WHOSE TRX FILES ARE THE EVIDENCE OF `run`'s VERDICT (kb/Work PB5099, PB5218): the run that
    measured it (itself, or the one a REUSED record names), the runs its verdict composed with (`Outcome.composed_from`)
    and the run of each target's pass the store showed up to date (`Outcome.up_to_date`, any worktree's), those that
    still exist. The ONE resolution a reader of a gate's test results (the landing lane's classification of a merge
    queue red) goes through, so a composed GREEN is read by the runs that make it GREEN."""
    rec = read_record(run) or {}
    measured = run.parent / str(rec["reused_from"]) if rec.get("reused_from") else run
    rec = read_record(measured) or {}
    names = [measured.name, *(n for n in rec.get("composed_from") or [] if isinstance(n, str))]
    paths = [run.parent / n for n in names]
    # A target the store showed up to date (kb/Work PB5218): its pass's run, in whichever worktree recorded it.
    paths += [Path(u["evidence"]) for u in (rec.get("up_to_date") or {}).values()
              if isinstance(u, dict) and isinstance(u.get("evidence"), str)]
    return [p for p in dict.fromkeys(paths) if p.is_dir()]


def run_order(name: str) -> tuple[int, str]:
    """The order of a run directory's name (RUN_DIR): its number, then the name. A run named before the numbers has
    number 0, so it is older than every numbered run (and such runs are ordered among themselves by their stamps)."""
    m = RUN_DIR.fullmatch(name)
    if m is None:
        raise ValueError(f"{name!r} is not a run directory's name (RUN_DIR)")
    return int(m["n"] or 0), name


def run_dirs(run_root: Path) -> list[Path]:
    """Every run directory in the run root, oldest first (`run_order`): THE listing of a worktree's gate runs, the gate's
    own and the landing's (enqueue.tested_locally) alike."""
    return sorted((p for p in run_root.iterdir() if p.is_dir() and RUN_DIR.fullmatch(p.name)),
                  key=lambda p: run_order(p.name)) if run_root.is_dir() else []


@dataclass(frozen=True)
class LiveGate:
    """A gate that runs in a worktree now: its lock's label (`holder`: the gate's own `gate <repo> (pid N, since T, run R)`)
    and the run it writes (`run`, named in that label; None before the gate has a run directory)."""
    holder: str
    run: Path | None

    @property
    def what(self) -> str:
        """How a message names it: its run, else its lock's label."""
        return f"the gate run {self.run.name}" if self.run is not None else f"a gate ({self.holder or 'no label'})"


def live_gate(lock_path: Path) -> LiveGate | None:
    """THE ONE READER OF "A GATE IS RUNNING HERE" (kb/Work PB3662): the worktree's gate lock is held. `Gate.run` takes it
    before anything else and holds it until its verdict file is written; it is an OS lock the OS drops when its holder
    dies, so it is held exactly while a gate runs: a gate that died without a verdict frees it, and no process id
    (which Windows reuses) is ever trusted. Taken and released at once to read it. None when no gate runs.
    ⛔ THE WINDOW (kb/Work PB3707): a detached gate's run directory exists, and its child is alive, before the child takes
    the lock, so the lock is NOT held for that moment of interpreter start and imports. A reader that must tell a gate
    still starting from one that died asks `gate_phase`, which adds the spawn record and the run's age to this reading."""
    lock = ExclusiveLock.try_take(lock_path, f"live_gate probe (pid {os.getpid()})")
    if lock is not None:
        lock.release()
        return None
    holder = ExclusiveLock.holder(lock_path)
    run = Path(holder.partition(RUN_LABEL)[2].removesuffix(")")) if RUN_LABEL in holder else None
    return LiveGate(holder, run)
