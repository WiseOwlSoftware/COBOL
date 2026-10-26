#!/usr/bin/env python3
"""impact_lane.py — THE LOOP RECORDS AN IMPACT MAP FOR EVERY MAIN COMMIT IT ADOPTS (kb/Work PB3663; DESIGN-orchestrator-loop.md
section 4.10; DESIGN-test-build-ci.md section 3.13).

  impact_lane.py request <sha> [--repo R] [--coord C]   record <sha>, or queue it behind the running recording
  impact_lane.py work          [--repo R] [--coord C]   the detached worker `request` starts (never started by hand)
  impact_lane.py status        [--coord C]              the lane's state, one line
  impact_lane.py measure       [--repo R] [--since ISO] how many gates' plans a map served (the owner's measured bar)
  impact_lane.py --self-test

WHY (owner 2026-10-24 ~17:10 PDT, attended session 0a5c7c77: "Yes, record the impact map automatically"). Under batched
gating (kb/Work PB2515) leg 1 is the only local run, and without a map it holds the change's own tests plus the cheapest
slice of everything: gate 000004-20261024T221346Z (PB2974) ran 36,802 of 38,134 Unit cases and 10,207 of 18,394
Conformance cases in leg 1 and could still leave a case the change reaches to CI (PB3666's bounce). No map had ever
been recorded: the on-demand rule (kb/Work PB1709, 2026-09-28) dates from when every gate ran the whole population and
the map only ordered it, and nothing ever asked for one.

THE RULE, pinned by `ImpactMapLaneDriftTests`:
  * `adopt_main.py adopt` calls `request` once per call, for the commit the checkout is at when the adoption moved it
    (ADOPTED) or found it already there (CURRENT); a KEPT adoption and a dry run request nothing. One call, one
    decision: start a recording, queue behind the running one, or skip.
  * ONE RECORDING RUNS AT A TIME, in a DETACHED worker (scripts/detached.py: the supervisor's restart, a unit's end and
    a session's kill do not end it) that holds the recorder's own gate slot at BelowNormal, so it never blocks a unit
    or the landing lane. A request while one runs COALESCES: `pending` is the newest requested commit and replaces an
    older one, so a stale commit never queues and the worker records, after the running one, only the newest.
  * SKIPPED when a map for the commit exists, when the map that serves it (impact_store.nearest) differs from it in
    documentation only ("documentation-only drift"), and when a recording of that very commit already FAILED (the next
    adoption of another commit tries again, so a commit that cannot be recorded does not loop).
  * LOUD ON FAILURE: a recording that ends without `=== IMPACT MAP: RECORDED` (the recorder's FAILED line, no verdict
    line, a dead worker) posts the line to the operator's mailbox (once per commit) and shows in every later adoption
    line until a recording succeeds. A missing map is never silent.
  * BOUNDED: after a RECORDED verdict the worker prunes the store to the newest `impact_store.KEEP_NEWEST` maps plus the
    map serving each live worktree's base, and removes the recorder's work directories (a failed recording's is kept for
    its diagnosis until the next one ends).
State is `<coord>/impact-lane.json`, written under `coord.locked` (milliseconds); the recordings' logs are
`<coord>/logs/impact-map-<sha9>-<stamp>.log`.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import subprocess
import sys
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))      # first: our mailbox.py, not the standard library's
import coord  # noqa: E402
sys.path.append(str(HERE.parents[1] / "scripts"))   # scripts/, for detached and standard_streams
sys.path.append(str(HERE.parents[1] / "scripts" / "spec"))   # scripts/spec, for impact_store
from detached import spawn_detached  # noqa: E402  (THE one way a process outlives its caller, kb/Work PB3536)
import impact_store  # noqa: E402  (the store: where maps live, which one serves a base, how it is bounded)
from standard_streams import ensure_utf8  # noqa: E402

STATE = "impact-lane.json"
LOCK = "impact-lane.lock"
RECORDER = pathlib.PurePosixPath("scripts/spec/record_impact_map.py")
VERDICT_PREFIX = "=== IMPACT MAP: "
RECORDED, FAILED = "RECORDED", "FAILED"
WORKER_MARK = "impact_lane.py"   # what the worker's command line names, so a reused pid is not mistaken for it

Say = Callable[[str], None]
#: A recording: (repo, sha, log path) -> (exit code, the recorder's verdict line or ""). The self-test passes a fake.
Record = Callable[[pathlib.Path, str, pathlib.Path], tuple[int, str]]


def now_iso() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


def load(cdir: pathlib.Path) -> dict[str, Any]:
    st = coord.read_json(cdir / STATE, {})
    return st if isinstance(st, dict) else {}


def save(cdir: pathlib.Path, st: dict[str, Any]) -> None:
    coord.write_json(cdir / STATE, st)


def recorder_present(repo: pathlib.Path) -> bool:
    return (repo / RECORDER).is_file()


def worker_alive(st: dict[str, Any]) -> bool:
    """The registered worker is a live Python whose command line names this script (a pid the OS reused for another
    program after the worker died carries another command line; an unreadable command line reads as alive, as
    land_lane.runner_alive does, so a live worker is never started beside)."""
    w = st.get("worker")
    pid = w.get("pid") if isinstance(w, dict) else None
    if not isinstance(pid, int):
        return False
    name = coord.process_name(pid)
    if not name or "python" not in name.lower():
        return False
    cmd = coord.process_command(pid)
    return cmd == "" if not cmd else WORKER_MARK in cmd


def skip_reason(store: pathlib.Path, repo: pathlib.Path, sha: str, st: dict[str, Any]) -> str | None:
    """Why `sha` is not recorded, or None to record it. The first reason is only a statement that the commit is served;
    `served` (below) says whether a queued older commit is served by it too."""
    if impact_store.map_file(store, sha).exists():
        return f"a map for {sha[:9]} exists"
    near = impact_store.nearest(store, sha, repo)
    if near is not None and not near.drift:
        return f"map {near.sha[:9]} differs from {sha[:9]} in documentation only"
    fail = st.get("failure")
    if isinstance(fail, dict) and fail.get("sha") == sha:
        return (f"a recording of {sha[:9]} already FAILED ({fail.get('line', '')[:120]}); the next adoption of another "
                f"commit tries again")
    return None


def served(reason: str) -> bool:
    """A skip reason that says a map SERVES the commit (as opposed to a failure that says nothing does)."""
    return "FAILED" not in reason


def status_line(cdir: pathlib.Path) -> str:
    st = load(cdir)
    bits = []
    if st.get("current"):
        bits.append(f"recording {st['current']['sha'][:9]}")
    if st.get("pending"):
        bits.append(f"{st['pending'][:9]} queued")
    fail = st.get("failure")
    if isinstance(fail, dict):
        bits.append(f"LAST RECORDING FAILED at {fail.get('sha', '?')[:9]}: {fail.get('line', '')[:160]} (log {fail.get('log', '?')})")
    return "; ".join(bits) if bits else "idle"


def request(repo: pathlib.Path, cdir: pathlib.Path, sha: str, *, start: Callable[[list[str], pathlib.Path], int] = spawn_detached,
            alive: Callable[[dict[str, Any]], bool] = worker_alive, store: pathlib.Path | None = None) -> str:
    """The lane's one decision for an adopted commit. Returns the words the adoption line carries: what was started,
    queued or skipped, and the standing failure if one is unanswered. Never raises: a lane that cannot decide says so
    in the line (the loop goes on; a missing map is reported, not fatal)."""
    try:
        if not recorder_present(repo):
            return f"impact map: no recorder in this checkout ({RECORDER})"
        notes: list[str] = []
        explicit = store   # a trial store (--store) rides into the worker; the default is the repository's shared one
        store = store or impact_store.default_store(repo)
        with coord.locked(cdir, LOCK):
            st = load(cdir)
            running = alive(st)
            if st.get("worker") and not running:
                # A worker that died mid-recording leaves `current`: that recording FAILED, loudly, once.
                dead = st.get("current")
                st["worker"] = st["current"] = None
                if isinstance(dead, dict):
                    fail(cdir, st, repo, dead["sha"], "the worker died during the recording", pathlib.Path(dead.get("log", "")),
                         say=notes.append)
            why = skip_reason(store, repo, sha, st)
            if why is not None:
                if served(why):
                    st["pending"] = None   # a queued older commit is served by whatever serves this newer one
                msg = f"skipped ({why})"
            elif st.get("current", {}) and st["current"].get("sha") == sha and running:
                msg = f"recording {sha[:9]} is running"
            elif running:
                replaced = st.get("pending")
                st["pending"] = sha
                msg = (f"{sha[:9]} queued behind the recording of {st['current']['sha'][:9]}"
                       + (f" (replacing {replaced[:9]})" if replaced and replaced != sha else "")) if st.get("current") else f"{sha[:9]} queued"
            else:
                st["pending"] = sha
                log = cdir / "logs" / "impact-lane-worker.log"
                pid = start([sys.executable, str(pathlib.Path(__file__).resolve()), "work", "--repo", str(repo),
                             "--coord", str(cdir), *(["--store", str(explicit)] if explicit else [])], log)
                st["worker"] = {"pid": pid, "started": now_iso()}
                msg = f"started the recorder for {sha[:9]} (worker pid {pid})"
            save(cdir, st)
        standing = status_line(cdir)
        return f"impact map: {msg}" + "".join(f"; {n}" for n in notes) + (f"; {standing}" if "FAILED" in standing else "")
    except (OSError, SystemExit, ValueError, subprocess.SubprocessError) as e:
        return f"impact map: the lane could not decide for {sha[:9]} ({e}); no recording was started"


def fail(cdir: pathlib.Path, st: dict[str, Any], repo: pathlib.Path, sha: str, line: str, log: pathlib.Path,
         say: Say = lambda text: print(text, flush=True)) -> None:
    """Record one failed recording in `st` (the caller saves it) and tell the operator, once per commit. The line is the
    recorder's `=== IMPACT MAP: FAILED … ===` verdict, or the reason there is none. `say` receives what it reports: the
    worker prints it to its log; `request` must print nothing, its caller's stdout being adopt_main's JSON."""
    st["failure"] = {"sha": sha, "line": line, "log": str(log), "at": now_iso()}
    st["last"] = {"sha": sha, "outcome": FAILED, "line": line, "at": now_iso()}
    say(f"impact-lane: {line if line.startswith(VERDICT_PREFIX) else f'{VERDICT_PREFIX}{FAILED} {sha[:9]} - {line} ==='}")
    try:
        import mailbox  # noqa: PLC0415  (THE writer of a message; our mailbox.py is first on sys.path)
        refs = [str(log)] if log and log.exists() else [str(cdir / STATE)]
        mailbox.send(cdir, to="operator", kind="info", subject=f"impact map FAILED for {sha[:9]}",
                     body=f"The loop's impact-map recording of main commit {sha} failed: {line}. The next adopted commit "
                          f"tries again; until a recording succeeds every adoption line says so. Log: {log}."[:mailbox.BODY_MAX],
                     refs=refs, sender="loop", unless_pending=True, repo=repo)
    except Exception as e:  # noqa: BLE001  (the report must not end the worker; the state file and the log still say it)
        say(f"impact-lane: the failure could not be posted to the operator's mailbox ({e})")


def real_record(repo: pathlib.Path, sha: str, log: pathlib.Path, store: pathlib.Path | None = None) -> tuple[int, str]:
    """Run the recorder (the checkout's own script) at BelowNormal and return its exit code and verdict line. `store`
    is a trial store; None records into the repository's shared one."""
    log.parent.mkdir(parents=True, exist_ok=True)
    flags = {"creationflags": subprocess.BELOW_NORMAL_PRIORITY_CLASS} if os.name == "nt" else {}
    with open(log, "w", encoding="utf-8", errors="replace", newline="\n") as fh:
        rc = subprocess.run([sys.executable, str(repo / RECORDER), "--commit", sha, "--priority", "BelowNormal",
                             *(["--store", str(store)] if store else [])],
                            cwd=repo, stdout=fh, stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL, **flags).returncode
    verdict = ""
    for line in reversed(log.read_text(encoding="utf-8", errors="replace").splitlines()):
        if line.startswith(VERDICT_PREFIX):
            verdict = line.strip()
            break
    return rc, verdict


def work(repo: pathlib.Path, cdir: pathlib.Path, *, record: Record | None = None, store: pathlib.Path | None = None,
         bases: Callable[[pathlib.Path], list[str]] = impact_store.live_bases) -> int:
    """The worker: record the pending commit, then whatever was queued meanwhile (the newest only), then exit. Each
    cycle re-decides under the lock, so a commit served by a map recorded since it was queued is skipped."""
    record = record or (lambda r, s, log, trial=store: real_record(r, s, log, trial))
    store = store or impact_store.default_store(repo)
    with coord.locked(cdir, LOCK):
        st = load(cdir)
        st["worker"] = {"pid": os.getpid(), "started": now_iso()}
        save(cdir, st)
    while True:
        with coord.locked(cdir, LOCK):
            st = load(cdir)
            sha, st["pending"] = st.get("pending"), None
            if not sha:
                st["worker"] = st["current"] = None
                save(cdir, st)
                return 0
            why = skip_reason(store, repo, sha, st)
            if why is not None:
                print(f"impact-lane: {sha[:9]} skipped ({why})", flush=True)
                save(cdir, st)
                continue
            log = cdir / "logs" / f"impact-map-{sha[:9]}-{dt.datetime.now():%Y%m%dT%H%M%S}.log"
            st["current"] = {"sha": sha, "started": now_iso(), "log": str(log)}
            save(cdir, st)
        print(f"impact-lane: recording {sha} (log {log})", flush=True)
        try:
            rc, verdict = record(repo, sha, log)
        except Exception as e:  # noqa: BLE001  (a recorder that cannot even start is a FAILED recording)
            rc, verdict = 1, f"{VERDICT_PREFIX}FAILED — the recorder did not start: {e} ==="
        ok = verdict.startswith(f"{VERDICT_PREFIX}{RECORDED}") and rc == 0
        with coord.locked(cdir, LOCK):
            st = load(cdir)
            st["current"] = None
            if ok:
                st["failure"] = None
                st["last"] = {"sha": sha, "outcome": RECORDED, "line": verdict, "at": now_iso()}
                print(f"impact-lane: {verdict}", flush=True)
            else:
                fail(cdir, st, repo, sha, verdict or f"the recorder ended with exit {rc} and no verdict line", log)
            save(cdir, st)
        try:
            if ok:
                gone = impact_store.prune(store, repo, bases(repo))
                if gone:
                    print(f"impact-lane: pruned {len(gone)} map file(s): {', '.join(gone)}", flush=True)
            impact_store.clean_work(store, repo, sha, recorded=ok)
        except Exception as e:  # noqa: BLE001  (a store that cannot be tidied is reported, never a lost recording)
            print(f"impact-lane: the store could not be tidied ({e})", flush=True)


def worktrees(repo: pathlib.Path) -> list[pathlib.Path]:
    p = subprocess.run(["git", "worktree", "list", "--porcelain"], cwd=repo, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    return [pathlib.Path(ln[len("worktree "):]) for ln in p.stdout.splitlines() if ln.startswith("worktree ")]


def measure(repo: pathlib.Path, since: str = "", roots: list[pathlib.Path] | None = None) -> str:
    """THE OWNER'S MEASURED BAR (kb/Work PB3663 bar 4): over the gates' plans of every worktree (their
    `TestResults/build-local/<run>/plan.json` records `map_state`), how many were SERVED by a map (exact,
    documentation-only drift, nearest) against how many fell back to no attribution (none, or the retired `stale`).
    `since` is an ISO date or time: runs started before it are not counted (a run directory names its start)."""
    cutoff = since.replace("-", "").replace(":", "") if since else ""
    counts: dict[str, int] = {}
    for wt in roots if roots is not None else worktrees(repo):
        for plan in sorted((wt / "TestResults" / "build-local").glob("*/plan.json")):
            stamp = plan.parent.name.split("-", 1)[-1]   # 000004-20261024T221346Z -> 20261024T221346Z
            if cutoff and stamp < cutoff:
                continue
            try:
                state = str(json.loads(plan.read_text(encoding="utf-8")).get("map_state") or "none")
            except (OSError, ValueError):
                continue
            counts[state] = counts.get(state, 0) + 1
    total = sum(counts.values())
    with_map = sum(n for k, n in counts.items() if k in ("exact", "documentation-only drift", "nearest"))
    detail = ", ".join(f"{k} {n}" for k, n in sorted(counts.items())) or "no gate plans"
    return (f"impact map use: {total} gate plan(s){f' since {since}' if since else ''}: {detail}; "
            f"served by a map: {with_map}/{total}" + (f" = {100 * with_map / total:.0f} %" if total else ""))


def self_test() -> int:
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    fails: list[str] = []

    def check(label: str, got: object, want: object) -> None:
        ok = got == want
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + ("" if ok else f"  (got {got!r}, want {want!r})"))
        if not ok:
            fails.append(label)

    tmp = scratch_dir("impact-lane-")
    try:
        repo = ScratchRepo.init(tmp / "repo")
        repo.commit("base", {RECORDER.as_posix(): "# the recorder\n", "src/A.cs": "a1\n", "docs/d.md": "d1\n"})
        s0 = repo.git("rev-parse", "HEAD")
        repo.commit("code", {"src/A.cs": "a2\n"})
        s1 = repo.git("rev-parse", "HEAD")
        repo.commit("docs", {"docs/d.md": "d2\n"})
        s1d = repo.git("rev-parse", "HEAD")
        repo.commit("more code", {"src/A.cs": "a3\n"})
        s2 = repo.git("rev-parse", "HEAD")
        repo.commit("even more code", {"src/A.cs": "a4\n"})
        s3 = repo.git("rev-parse", "HEAD")
        cdir = tmp / "coord"
        (cdir / "logs").mkdir(parents=True)
        store = tmp / "store"
        store.mkdir()
        started: list[list[str]] = []
        live = {"yes": False}

        def fake_start(argv: list[str], log: pathlib.Path) -> int:
            started.append(argv)
            live["yes"] = True
            return 4242

        def fake_alive(_st: dict[str, Any]) -> bool:
            return live["yes"]

        def ask(sha: str) -> str:
            return request(repo.path, cdir, sha, start=fake_start, alive=fake_alive, store=store)

        # 1. nothing recorded: the first request starts exactly one worker, for the newest commit
        msg = ask(s1)
        check("the first request starts one detached worker for the commit", (len(started), "started the recorder for" in msg, s1[:9] in msg), (1, True, True))
        check("... whose argv is `work` with the repo and the coordination directory (and a trial store, when one was named)",
              (started[0][2], started[0][3:]),
              ("work", ["--repo", str(repo.path), "--coord", str(cdir), "--store", str(store)]))
        loop_started: list[list[str]] = []
        (tmp / "coord2").mkdir()
        request(repo.path, tmp / "coord2", s0, start=lambda argv, _log: loop_started.append(argv) or 1, alive=lambda _s: False)
        check("... and the loop's own request names no store: the repository's shared one",
              (len(loop_started), "--store" in loop_started[0]), (1, False))
        check("... and the state names the pending commit and the worker", (load(cdir)["pending"], load(cdir)["worker"]["pid"]), (s1, 4242))
        # 2. COALESCE: a request while one runs queues the newest, replacing the older, and starts nothing
        st = load(cdir)
        st["pending"], st["current"] = None, {"sha": s1, "started": now_iso(), "log": str(cdir / "logs" / "x.log")}
        save(cdir, st)
        msg = ask(s2)
        check("a request while a recording runs queues behind it, starting no second worker", (len(started), load(cdir)["pending"]), (1, s2))
        msg = ask(s3)
        check("a later request REPLACES the queued one: a stale commit never queues", (len(started), load(cdir)["pending"], f"replacing {s2[:9]}" in msg), (1, s3, True))
        check("a request for the commit being recorded changes nothing", ("is running" in ask(s1), load(cdir)["pending"]), (True, s3))
        # 3. the worker records the running one's successor: only the NEWEST queued, once, after the first ends
        recorded_order: list[str] = []

        def fake_record(_repo: pathlib.Path, sha: str, log: pathlib.Path) -> tuple[int, str]:
            recorded_order.append(sha)
            impact_store.map_file(store, sha).write_bytes(b"x")
            log.parent.mkdir(parents=True, exist_ok=True)
            log.write_text("recorded\n", encoding="utf-8")
            return 0, f"{VERDICT_PREFIX}RECORDED {sha} (1 tests, 1.0 s) ==="

        st = load(cdir)
        st["current"] = None
        save(cdir, st)
        pruned: list[list[str]] = []
        rc = work(repo.path, cdir, record=fake_record, store=store, bases=lambda r: pruned.append([]) or [])
        check("the worker records the newest queued commit and then exits", (rc, recorded_order), (0, [s3]))
        st = load(cdir)
        check("... leaving the lane idle with the success recorded", (st["worker"], st["current"], st["pending"], st["failure"], st["last"]["outcome"]),
              (None, None, None, None, RECORDED))
        check("... and the store pruned after a RECORDED verdict", len(pruned), 1)
        # 4. SKIPS: a map exists; a map differs from the commit in documentation only; the same rule at request and at pickup
        live["yes"] = False
        check("a commit with a map is skipped", "skipped (a map for" in ask(s3), True)
        check("... and starts no worker", len(started), 1)
        repo.commit("docs after the map", {"docs/d.md": "d3\n"})
        s4 = repo.git("rev-parse", "HEAD")
        msg = ask(s4)
        check("a documentation-only adoption records nothing", ("documentation only" in msg, len(started)), (True, 1))
        # 5. FAILURE: loud (the line, the mailbox, every later adoption line), once per commit, retried only on another commit
        repo.commit("code again", {"src/A.cs": "a5\n"})
        s5 = repo.git("rev-parse", "HEAD")
        repo.commit("code once more", {"src/A.cs": "a6\n"})
        s6 = repo.git("rev-parse", "HEAD")

        def bad_record(_repo: pathlib.Path, sha: str, log: pathlib.Path) -> tuple[int, str]:
            log.parent.mkdir(parents=True, exist_ok=True)
            log.write_text("the build broke\n", encoding="utf-8")
            return 1, f"{VERDICT_PREFIX}FAILED — the recording build failed (logs {log}) ==="

        ask(s5)
        rc = work(repo.path, cdir, record=bad_record, store=store, bases=lambda r: [])
        st = load(cdir)
        check("a recording that ends FAILED records the failure with its line", (rc, st["failure"]["sha"], "the recording build failed" in st["failure"]["line"]), (0, s5, True))
        import mailbox  # noqa: PLC0415  (this directory's, first on sys.path)
        posted = mailbox.listing(cdir, "operator")
        check("... posts it to the operator's mailbox, once", (len(posted), posted[0]["subject"] if posted else ""), (1, f"impact map FAILED for {s5[:9]}"))
        live["yes"] = False
        n = len(started)
        msg = ask(s5)
        check("the same commit is not retried: it would loop", ("already FAILED" in msg, len(started)), (True, n))
        check("... and the adoption line carries the standing failure", "LAST RECORDING FAILED" in msg, True)
        msg = ask(s6)
        check("the next adoption of another commit tries again", ("started the recorder for" in msg, len(started), "LAST RECORDING FAILED" in msg), (True, n + 1, True))
        work(repo.path, cdir, record=fake_record, store=store, bases=lambda r: [])
        check("... and a success clears the failure", (load(cdir)["failure"], status_line(cdir)), (None, "idle"))
        # 6. a recorder with no verdict line, an exit code other than 0 after a RECORDED line, and one that cannot start
        repo.commit("code 7", {"src/A.cs": "a7\n"})
        s7 = repo.git("rev-parse", "HEAD")

        def no_verdict(_repo: pathlib.Path, _sha: str, _log: pathlib.Path) -> tuple[int, str]:
            return 0, ""

        def exit_one(_repo: pathlib.Path, _sha: str, _log: pathlib.Path) -> tuple[int, str]:
            return 1, f"{VERDICT_PREFIX}RECORDED x ==="

        def cannot_start(_repo: pathlib.Path, _sha: str, _log: pathlib.Path) -> tuple[int, str]:
            raise OSError("no python")

        for label, rec in (("no verdict line", no_verdict), ("a nonzero exit", exit_one), ("an exception", cannot_start)):
            st = load(cdir)
            st.update({"pending": s7, "failure": None})
            save(cdir, st)
            work(repo.path, cdir, record=rec, store=store, bases=lambda r: [])
            check(f"a recorder with {label} is a FAILED recording", load(cdir)["failure"]["sha"], s7)
        # 7. a worker that died mid-recording: the next request reports its recording FAILED and starts a new worker
        repo.commit("code 8", {"src/A.cs": "a8\n"})
        s8 = repo.git("rev-parse", "HEAD")
        st = load(cdir)
        st.update({"worker": {"pid": 1, "started": now_iso()}, "pending": None, "failure": None,
                   "current": {"sha": s7, "started": now_iso(), "log": str(cdir / "logs" / "dead.log")}})
        save(cdir, st)
        before = len(started)
        request(repo.path, cdir, s8, start=fake_start, alive=lambda _s: False, store=store)
        check("a dead worker's recording is reported FAILED and a new worker starts", (load(cdir)["failure"]["sha"], len(started) - before), (s7, 1))
        # 8. a checkout with no recorder, and a lane that cannot decide, say so and never raise
        (repo.path / RECORDER).unlink()
        check("a checkout without the recorder starts nothing and says so", "no recorder in this checkout" in ask(s8), True)
        elsewhere = tmp / "not-a-repo"
        (elsewhere / RECORDER.parent).mkdir(parents=True)
        (elsewhere / RECORDER).write_text("#", encoding="utf-8")
        check("a lane that cannot decide reports it, never raises",
              "could not decide" in request(elsewhere, cdir, s8, start=fake_start, alive=fake_alive), True)
        # 9. measure: the share of gate plans a map served, per run directory's start
        wt = tmp / "wt"
        for run, state in (("000001-20261024T100000Z", "none"), ("000002-20261025T100000Z", "exact"), ("000003-20261025T110000Z", "nearest"),
                           ("000004-20261025T120000Z", "stale"), ("000005-20261025T130000Z", "documentation-only drift")):
            d = wt / "TestResults" / "build-local" / run
            d.mkdir(parents=True)
            (d / "plan.json").write_text(json.dumps({"map_state": state}), encoding="utf-8")
        check("measure counts the gate plans a map served", measure(repo.path, roots=[wt]).endswith("served by a map: 3/5 = 60 %"), True)
        check("... from a date on", measure(repo.path, since="2026-10-25", roots=[wt]).endswith("served by a map: 3/4 = 75 %"), True)
        check("... and says so when there are no plans", measure(repo.path, roots=[tmp / "none"]).endswith("served by a map: 0/0"), True)
    finally:
        remove_tree(tmp)
    print("impact_lane self-test: all checks OK" if not fails else f"FAILED: {len(fails)} check(s): {fails}")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("cmd", nargs="?", choices=("request", "work", "status", "measure"))
    ap.add_argument("sha", nargs="?")
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--since", default="")
    ap.add_argument("--store", type=pathlib.Path, help="a TRIAL store instead of the repository's shared one (the "
                                                     "recorder's own --store); the loop never passes it")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    if a.cmd == "request":
        if not a.sha:
            ap.error("request names the commit to record")
        print(request(repo, cdir, a.sha, store=a.store))
        return 0
    if a.cmd == "work":
        return work(repo, cdir, store=a.store)
    if a.cmd == "status":
        print(f"impact map lane: {status_line(cdir)}")
        return 0
    if a.cmd == "measure":
        print(measure(repo, a.since))
        return 0
    ap.error("name a command")
    return 2


if __name__ == "__main__":
    ensure_utf8(line_buffering=True)
    sys.exit(main())
