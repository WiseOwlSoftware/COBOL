#!/usr/bin/env python3
"""The gate's runs on disk and its locks: a second gate, the verdict reader, the key of a tree, re-derivation (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
import concurrent.futures
import datetime as dt
import json
import os
import re
import shutil
import subprocess
import sys
import threading
import time
from gate_slot import (ExclusiveLock, GateSlots, Heartbeat, QueueAbandoned, Slot,  # noqa: E402
                       gate_class_from_env)
from pathlib import Path
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)

import fixtures
import selftest_cases
from gate_fake_host import FakeHost
from standard_streams import ensure_utf8
from scratch_repo import ScratchRepo, remove_tree

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
import gate_runs
from gate_verdict import (BRANCH, BUSY, BUSY_READING, ENVIRONMENT, EXIT_BUSY, GREEN_READING, NOT_RUN, NOT_RUN_CAUSES,
                          Outcome, RED_READING, Reading, UNCLASSIFIED, VERDICT, not_run, read_verdict, _NOT_RUN_TAG)
from gate_host import CI_BOUNCE_RESULTS, GATE_CONFIGURATION, Host, ONLY_CASES
Gate = gate_mod.Gate
from gate_runs import (LANDABLE_SCHEMA, LANDING_FIELDS, ORIGINAL_VERDICT_FILE, RECORD_SCHEMA, RUN_DIR, RUN_LABEL,
                       SCHEMA_NOTES, SCHEMA_WHOLE, VERDICT_FILE, live_gate, measuring_runs, predates_landing,
                       read_pid, read_record, record_schema, run_dirs, run_order, run_started, write_pid)
REPO = gate_mod.REPO
from gate_reuse import PassCarry, Rederived


def case_00_second_gate_same_worktree_refused_at_once(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "second"
    d.mkdir()
    lock, go = d / "gate.lock", d / "go"
    holder = subprocess.Popen([sys.executable, "-c", "\n".join([
        "import os, sys, time, pathlib",
        f"sys.path.insert(0, {str(REPO / 'scripts')!r})",
        "from gate_slot import ExclusiveLock",
        f"l = ExclusiveLock.try_take(pathlib.Path({str(lock)!r}), 'first gate (pid %d)' % os.getpid())",
        "print('held' if l else 'NOT HELD', flush=True)",
        f"g = pathlib.Path({str(go)!r})",
        "t = time.monotonic()",
        "while not g.exists() and time.monotonic() - t < 60: time.sleep(0.02)"])],
        stdout=subprocess.PIPE, text=True, cwd=root.parent)
    try:
        first = holder.stdout.readline().strip()
        host2 = FakeHost(d, lock)
        sink.clear()
        o = Gate(host2, d / "runs", say=sink.append).run()
    finally:
        go.touch()
        holder.wait(timeout=30)
    expect("a second gate in the same worktree is REFUSED at once, naming the holder's pid, and takes no slot: its "
        "verdict is BUSY with its own exit code, which the one verdict reader reads as BUSY, never RED (kb/Work "
        "PB3527)",
        first == "held" and o.verdict == BUSY and o.exit_code == EXIT_BUSY and f"pid {holder.pid}" in o.line
        and host2.slots_taken == 0 and not (d / "runs").exists()
        and read_verdict(o.line[len(VERDICT):]) == BUSY_READING, o.line)
    old = "LEG 1 ONLY (batched gating, PB2515)"   # the line of a record a gate before PB4328 wrote
    expect("the one verdict reader: GREEN and an older gate's LEG 1 ONLY: GREEN pass; RED, RED/INCOMPLETE, LEG 1 ONLY: "
        "RED and BUILD FAILED are RED; NOT RUN (this driver's and a hand-run Linux gate's) is NOT RUN with its cause's class, never RED "
        "(kb/Work PB3526), and one naming no known cause is UNCLASSIFIED, BRANCH; a Linux gate's GREEN (legs ...) "
        "passes",
        [read_verdict(v) for v in ("GREEN — x ===", f"{old}: GREEN — x", "RED — x", "RED/INCOMPLETE — x",
                                   f"{old}: RED — x", "BUILD FAILED — x", "NOT RUN [settings] — x",
                                   "NOT RUN [sdk] (no .NET SDK in this Linux environment) ===",
                                   "NOT RUN [disk] (777216 free inodes on the filesystem holding /x) ===",
                                   "NOT RUN [driver] — x", "NOT RUN — x", "NOT RUN [nosuch] — x", "NOT RUNNING",
                                   "GREEN (legs unit) ===", "GREENISH")]
        == [GREEN_READING, GREEN_READING, RED_READING, RED_READING, RED_READING, RED_READING, not_run("settings"),
            not_run("sdk"), not_run("disk"), not_run("driver"), Reading(NOT_RUN, UNCLASSIFIED), Reading(NOT_RUN, UNCLASSIFIED),
            RED_READING, GREEN_READING, RED_READING]
        and NOT_RUN_CAUSES["disk"].carried == ENVIRONMENT
        and NOT_RUN_CAUSES["merge-blocked"].carried == ENVIRONMENT
        and [c.tag for c in NOT_RUN_CAUSES.values() if c.carried == BRANCH]
        == ["driver", "merge-failed", "population", "arguments", "verdict", "unclassified"])
    printers = {f: (REPO / "scripts" / f).read_text(encoding="utf-8")
                for f in ("build-local.ps1", "build-local.sh", "linux-gate.sh")}
    untagged, unknown = [], []
    for f, text in printers.items():
        for ln in text.splitlines():
            at = ln.find("GATE: NOT RUN")
            if at < 0 or ln.lstrip().startswith("#"):
                continue
            tag = _NOT_RUN_TAG.match(ln[at + len("GATE: "):])
            (unknown if tag and tag.group(1) not in NOT_RUN_CAUSES else untagged if not tag else []).append(
                f"{f}: {ln.strip()[:90]}")
    own = re.findall(r'_finish\(out, \w+, (NOT_RUN[^,]*),', (REPO / "scripts" / "gate.py").read_text(
        encoding="utf-8"))
    expect("every NOT RUN verdict a gate prints names a cause NOT_RUN_CAUSES classifies: build-local.ps1, "
        "build-local.sh and linux-gate.sh, and this driver's own (_finish with NOT_RUN_CAUSES[...].verdict)",
        all(printers.values()) and not untagged and not unknown and own
        and all(v.startswith("NOT_RUN_CAUSES[") for v in own), f"untagged={untagged} unknown={unknown} own={own}")
    scratch = ScratchRepo.init(root / "repo")
    scratch.commit("self-test", allow_empty=True)
    repo, linked = scratch.path, scratch.worktree(root / "linked", detach=True).path
    a, b = Host(repo).lock_path(), Host(linked).lock_path()
    expect("the gate lock is per worktree: the main checkout and a linked worktree lock different files",
        a != b and a.parent == (repo / ".git").resolve() and b.parent.parent == (repo / ".git" / "worktrees").resolve(),
        f"{a} vs {b}")
    trx_head = ('<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/'
                'TeamTest/2010"><Results>')
    asked: list[list[str]] = []
    def bounce_gh(args: list[str]) -> tuple[int, str, str]:
        asked.append(list(args))
        if args[:2] == ["repo", "view"]:
            return 0, "o/r\n", ""
        if args[:2] == ["pr", "list"]:
            return 0, "547\n", ""
        if args[0] == "api":
            return 0, json.dumps({"workflow_runs": [{"id": 77, "head_branch": "gh-readonly-queue/main/pr-547-abc123",
                                                     "conclusion": "failure", "created_at": "2026-10-24T10:00:00Z"}]}), ""
        if args[:2] == ["run", "download"]:
            art = Path(args[args.index("--dir") + 1]) / "test-results-Linux-unit"
            art.mkdir(parents=True)
            (art / "unit.trx").write_text(
                trx_head + '<UnitTestResult testId="t1" testName="N.Bounced.Case" duration="00:00:01" outcome="Failed" />'
                '</Results><TestDefinitions><UnitTest name="N.Bounced.Case" id="t1"><TestMethod className="N.Bounced" '
                'name="Case" /></UnitTest></TestDefinitions></TestRun>', encoding="utf-8", newline="\n")
            return 0, "", ""
        return 1, "", "unrouted"
    with_pr = Host(repo, gh=bounce_gh)
    note = with_pr.fetch_bounce_results()
    bounce_reds = red_classes(sorted((repo / CI_BOUNCE_RESULTS).rglob("*.trx")))
    expect("the real Host fetches the failed merge-group run of the checked-out branch's pull request into the worktree, "
        "where its red classes are read to run first (kb/Work PB3644, PB4328)",
        "failed merge-group run 77 of pull request #547" in note and bounce_reds == {"N.Bounced"}
        and ["pr", "list", "--head", "main"] == asked[1][:4], f"{note} {bounce_reds} {asked}")
    Host(repo, gh=lambda args: (1, "", "HTTP 502")).fetch_bounce_results()
    kept = red_classes(sorted((repo / CI_BOUNCE_RESULTS).rglob("*.trx")))
    expect("a GitHub that cannot be asked leaves the fetched results as they were (a non-observation changes nothing)",
        kept == {"N.Bounced"}, str(kept))
    scratch.git("checkout", "--quiet", "--detach")
    asked.clear()
    detached = Host(repo, gh=bounce_gh).fetch_bounce_results()
    scratch.git("checkout", "--quiet", "main")
    expect("a detached HEAD has no pull request: nothing is asked of GitHub",
        not asked and "none (no branch is checked out)" in detached, f"{detached} {asked}")
    shutil.rmtree(repo / "TestResults")   # (a real worktree ignores it; this scratch repo does not, and measures it)
    (repo / ".gitignore").write_text("ignored.txt\n", encoding="utf-8", newline="\n")
    (repo / "tracked.txt").write_text("one\n", encoding="utf-8", newline="\n")
    scratch.commit("tracked")
    real = Host(repo)
    index_before = (repo / ".git" / "index").read_bytes()
    t0 = real.measured_tree()
    (repo / "tracked.txt").write_text("two\n", encoding="utf-8", newline="\n")
    t_edit = real.measured_tree()
    (repo / "tracked.txt").write_text("one\n", encoding="utf-8", newline="\n")
    (repo / "untracked.txt").write_text("new\n", encoding="utf-8", newline="\n")
    t_new = real.measured_tree()
    (repo / "untracked.txt").unlink()
    (repo / "ignored.txt").write_text("noise\n", encoding="utf-8", newline="\n")
    t_ignored = real.measured_tree()
    committed = real.git("rev-parse", "HEAD^{tree}")
    expect("the measured tree is the working tree's content: an edit or an untracked file changes it, an ignored file "
        "does not, a clean tree measures as its commit's tree, and the worktree's index is untouched",
        t0 == committed and t_edit not in (None, t0) and t_new not in (None, t0, t_edit) and t_ignored == t0
        and (repo / ".git" / "index").read_bytes() == index_before, f"{t0} {t_edit} {t_new} {t_ignored} {committed}")
    past = (int(time.time()) - 60) * 1_000_000_000
    tracked, real_index = repo / "tracked.txt", repo / ".git" / "index"
    os.utime(tracked, ns=(past, past))
    scratch.git("add", "--", "tracked.txt")
    os.utime(real_index, ns=(past, past))
    t_racy0 = real.measured_tree()
    tracked.write_text("six\n", encoding="utf-8", newline="\n")      # the same size as "one\n"
    os.utime(tracked, ns=(past, past))
    seen = scratch.git("--no-optional-locks", "status", "--porcelain", "--", "tracked.txt")
    t_racy = real.measured_tree()
    tracked.write_text("one\n", encoding="utf-8", newline="\n")
    expect("an edit in the index's own second (racy git) changes the measured tree: the private index keeps the "
        "real index's mtime, so git re-reads a racily clean entry by content",
        t_racy0 == committed and seen.strip() == "M tracked.txt" and t_racy not in (None, committed),
        f"{t_racy0} {t_racy} {committed} {seen!r}")
    (repo / "ignored.txt").unlink()
    settings = repo / ".claude" / "settings.local.json"
    settings.parent.mkdir()
    settings.write_text("{}\n", encoding="utf-8", newline="\n")
    scratch.git("add", "-f", "--", ".claude/settings.local.json")
    scratch.git("commit", "-q", "-m", "harness settings")
    committed = real.git("rev-parse", "HEAD^{tree}")
    settings.write_text('{"permissions": {}}\n', encoding="utf-8", newline="\n")
    (repo / ".git" / "info").mkdir(exist_ok=True)
    (repo / ".git" / "info" / "exclude").write_text("STATUS.md\n", encoding="utf-8", newline="\n")
    (repo / "STATUS.md").write_text("STATUS-AT: x\n", encoding="utf-8", newline="\n")
    (repo / "TestResults" / "build-local").mkdir(parents=True)
    (repo / "TestResults" / "build-local" / "gate.log").write_text("x\n", encoding="utf-8", newline="\n")
    t_harness = real.measured_tree()
    scratch.git("add", "--", str(settings))
    t_staged = real.measured_tree()
    (repo / "tracked.txt").write_text("three\n", encoding="utf-8", newline="\n")
    t_work = real.measured_tree()
    scratch.git("reset", "-q", "--hard")
    for p in (repo / "STATUS.md", repo / "TestResults"):
        shutil.rmtree(p) if p.is_dir() else p.unlink()
    expect("the measured tree leaves out coord.NEVER_WORK: a harness-edited tracked settings file (on disk, or staged), "
        "STATUS.md and TestResults/ measure as the commit's own tree, and an edit of real work still changes it",
        t_harness == committed and t_staged == committed and t_work not in (None, committed),
        f"{t_harness} {t_staged} {t_work} {committed}")
    def regate(d: Path, **fake) -> tuple[Outcome, FakeHost]:
        host = FakeHost(d, d / "gate.lock", **fake)
        sink.clear()
        return Gate(host, d / "runs", say=sink.append).run(), host
    d = root / "reuse"
    d.mkdir()
    first, _ = regate(d)
    again, h2 = regate(d)
    third, h3 = regate(d)
    rec2 = read_record(d / "runs" / again.line.rsplit("(run ", 1)[1].split(")", 1)[0])
    (root / "names").mkdir()
    (root / "names" / "20261022T225440Z-2797b8").mkdir()   # a run named before the numbers
    ticks = iter([dt.datetime(2026, 10, 23, 2, 21, 9, 187360, dt.timezone.utc)] * 20
                 + [dt.datetime(2026, 10, 23, 1, 0, 0, tzinfo=dt.timezone.utc)] * 3)
    ordered = Gate(FakeHost(d, d / "gate.lock"), root / "names", say=sink.append, now=lambda: next(ticks))
    made = [ordered.new_run().name for _ in range(23)]
    expect("runs are ordered by the number each takes at its start, never by the clock: twenty started in one "
        "stopped instant and three more after the clock turned back are the newest, in the order they started, "
        "and a run named before the numbers is the oldest (the first classes' newest earlier run, the reuse rule's, the "
        "pruning's)",
        [p.name for p in run_dirs(ordered.run_root)] == ["20261022T225440Z-2797b8", *made]
        and [run_order(n)[0] for n in made] == list(range(1, 24)) and all(RUN_DIR.fullmatch(n) for n in made)
        and made[-1].endswith("-20261023T010000Z"), str(made))
    racing = Gate(FakeHost(d, d / "gate.lock"), root / "racing", say=sink.append,
                  now=lambda: dt.datetime(2026, 10, 23, 2, 21, 9, tzinfo=dt.timezone.utc))
    start = threading.Barrier(8)
    took: dict[int, list[int]] = {}
    def race(i: int) -> None:
        start.wait()
        took[i] = [run_order(racing.new_run().name)[0] for _ in range(5)]
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(race, range(8)))
    numbers = sorted(n for ns in took.values() for n in ns)
    expect("gates starting at once each take their own number, under the run root's lock, and each one's runs are "
        "ordered as it made them",
        numbers == list(range(1, 41)) and all(ns == sorted(ns) for ns in took.values())
        and len(run_dirs(racing.run_root)) == 40, str(took))
    livelock = d / "live-gate.lock"
    none_yet = live_gate(livelock)
    dead_run = Gate(FakeHost(d, livelock), root / "live", say=sink.append).new_run()
    write_pid(dead_run, os.getpid())   # a gate that DIED with no verdict, its pid since reused by a live process
    reused = live_gate(livelock)
    with ExclusiveLock.try_take(livelock, f"gate {d} (pid 7, since T{RUN_LABEL}{dead_run})"):
        seen = live_gate(livelock)
    with ExclusiveLock.try_take(livelock, f"gate {d} (pid 8, since T)"):
        starting = live_gate(livelock)
    expect("live_gate: a gate runs exactly while its gate lock is held (the holder's label names the run it writes, "
        "else the label is all there is); a free lock is none, even beside a run with no verdict whose gate.pid "
        "names a live process (a gate that died, its pid reused), and a lock the OS dropped reads free again",
        none_yet is None and reused is None and seen is not None and seen.run == dead_run
        and starting is not None and starting.run is None and "pid 8" in starting.what
        and live_gate(livelock) is None, f"{none_yet} {reused} {seen} {starting}")
    busy_run = root / "busy-run"
    busy_run.mkdir()
    with ExclusiveLock.try_take(livelock, f"gate {d} (pid 9, since T)"):
        busy = Gate(FakeHost(d, livelock), root / "busy", say=sink.append, run_dir=busy_run).run()
    expect("a detached gate that finds the gate lock held ends BUSY without a pid file: a gate.pid proves its writer "
        "took the lock",
        busy.verdict == BUSY and busy.exit_code == EXIT_BUSY and read_pid(busy_run) is None
        and read_record(busy_run) is not None, f"{busy.line} {read_pid(busy_run)}")
    expect("a re-gate of an unchanged tree answers from the recorded GREEN: no audit, slot, build or host runs, the "
        "verdict line says REUSED and names the run that measured it, exit 0",
        first.verdict == "GREEN" and again.verdict == first.verdict and again.exit_code == 0
        and h2.events == [] and h2.slots_taken == 0 and h2.growth == [] and not again.runs and "REUSED (kb/Work PB3536)" in again.line
        and again.reused_from is not None and again.reused_from in again.line
        and rec2 is not None and rec2["reused_from"] == again.reused_from and rec2["exit_code"] == 0,
        f"{again.line} {h2.events}")
    expect("a chain of reuses names the run that MEASURED the tree and its own verdict, never a reuse of a reuse",
        third.reused_from == again.reused_from and third.line.count("REUSED") == 1 and h3.events == [], third.line)
    retooled, hrt = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0")
    expect("a recorded GREEN measured under another toolchain is never REUSED (kb/Work PB5218): the gate runs",
        "REUSED" not in retooled.line and "build" in hrt.events and retooled.verdict == "GREEN", retooled.line)
    changed, hc = regate(d, trees=["tree-2"])
    expect("a re-gate of a CHANGED tree runs the gate (fetch, slot, build, hosts)",
        "REUSED" not in changed.line and hc.events[:3] == ["fetch", "slot", "build"]
        and changed.verdict == "GREEN", f"{changed.line} {hc.events}")
    d = root / "reuse-red"
    d.mkdir()
    regate(d, plant={"red"})
    red_again, hr = regate(d)
    expect("a RED is never reused: the same tree is gated again",
        "REUSED" not in red_again.line and "build" in hr.events, red_again.line)
    d = root / "reuse-old"
    d.mkdir()
    older, _ = regate(d)
    older_run = run_dirs(d / "runs")[-1]
    rec_old = read_record(older_run)
    (older_run / VERDICT_FILE).write_text(json.dumps({**rec_old, "schema": SCHEMA_WHOLE - 1, "leg1_only": True}),
                                          encoding="utf-8", newline="\n")
    remove_tree(d / "store")   # a gate before PB5218 recorded nothing in the store
    after_old, ho = regate(d)
    expect("a GREEN a gate before PB4328 wrote (record schema below SCHEMA_WHOLE: it may be LEG 1 ONLY) never answers a "
        "re-gate of the same tree: the whole population runs",
        older.verdict == "GREEN" and "REUSED" not in after_old.line and "build" in ho.events
        and after_old.verdict == "GREEN", after_old.line)
    d = root / "reuse-moved"
    d.mkdir()
    edited, _ = regate(d, trees=["tree-1", "tree-edited"])
    after_edit, he = regate(d)
    rebased, hb = regate(d, base="1" * 40)
    expect("a gate whose tree changed WHILE it ran is never reused, and another base is no answer to reuse (the reuse rule "
        "reads the base its lane was decided against); a run whose tree moved records no pass, and the same tree under "
        "another base finds every target up to date at its keys (kb/Work PB5218)",
        edited.tree == "tree-1" and edited.tree_end == "tree-edited" and "REUSED" not in after_edit.line
        and not edited.recorded and "build" in he.events and "REUSED" not in rebased.line
        and "EVERY TARGET UP TO DATE" in rebased.line and hb.events == [],
        f"{after_edit.line} | {rebased.line}")
    d = root / "compose"
    d.mkdir()
    red1, _ = regate(d, plant={"red"})
    again_red, ha = regate(d, plant={"red"})
    expect("a retry that is still RED is RED (the carried passes hide nothing, the case runs again alone)",
        again_red.verdict == "RED" and again_red.populations["Conformance"].carried == 5
        and sorted(ha.envs) == ["Conformance"] and "Conformance RED" in again_red.line, again_red.line)
    fix, hf = regate(d)
    fix_rec = read_record(run_dirs(d / "runs")[-1])
    only_text = (run_dirs(d / "runs")[-1] / "only-Conformance.txt").read_text(encoding="utf-8")
    expect("a re-gate of a tree whose earlier run was RED runs ONLY that run's red case: one host, one retry file, the "
        "other assemblies up to date in the store, and the verdict is GREEN with Conformance EXACT",
        red1.verdict == "RED" and fix.verdict == "GREEN" and sorted(hf.envs) == ["Conformance"]
        and only_text == "N.Conf.A\n" and hf.envs["Conformance"].get(ONLY_CASES) is not None
        and fix.populations["Conformance"].exact and sorted(fix.up_to_date) == ["Characterization", "RepoDrift", "Unit"]
        and fix.carried == {"Conformance": 5} and fix.recorded == ["Conformance"]
        and "Conformance 6/6" in fix.line and "5 carried from run(s)" in fix.line and "build" in hf.events,
        f"{fix.line} {sorted(hf.envs)} {only_text!r}")
    expect("the composed record names the runs it composes with and what it carried, writes no growth sample, and "
        "`measuring_runs` resolves it to the runs that hold its evidence",
        fix_rec["composed_from"] == [run_dirs(d / "runs")[1].name, run_dirs(d / "runs")[0].name]
        and fix_rec["carried"] == {"Conformance": 5} and hf.growth == [] and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])]
        == [run_dirs(d / "runs")[-1].name, run_dirs(d / "runs")[1].name, run_dirs(d / "runs")[0].name],
        str(fix_rec["composed_from"]))
    d = root / "compose-cleanup"
    d.mkdir()
    cl1, _ = regate(d, plant={"cleanup-red"})
    cl2, hcl = regate(d, plant={"cleanup-red"})
    expect("a host that went RED with every case Passed (a cleanup failure) leaves no case to retry, so the retry runs the "
        "whole assembly again and a deterministic host failure stays RED (review R1-1)",
        cl1.verdict == "RED" and cl2.verdict == "RED" and sorted(hcl.envs) == ["Conformance"]
        and not (run_dirs(d / "runs")[-1] / "only-Conformance.txt").exists() and ONLY_CASES not in hcl.envs["Conformance"]
        and cl2.populations["Conformance"].carried == 0 and "Conformance RED" in cl2.line, cl2.line)
    d = root / "compose-miscount"
    d.mkdir()
    mc1, _ = regate(d, plant={"red", "log-miscount"})
    mc2, hmc = regate(d, plant={"red", "log-miscount"})
    expect("a red host whose verdict counts a failure its trx does not name (a host-level fault beside a failed case) "
        "runs the whole assembly again, and the carried passes do not turn it GREEN (review R2-1)",
        mc1.verdict == "RED" and mc2.verdict == "RED" and sorted(hmc.envs) == ["Conformance"]
        and mc2.populations["Conformance"].carried == 0, mc2.line)
    d = root / "compose-prune"
    d.mkdir()
    regate(d, plant={"red"})
    for i in range(4):   # four runs that measured nothing (killed, refused) between the RED run and its retry
        (d / "runs" / f"2026012{i}T000000Z-00000{i + 2}").mkdir()
    fixed, _ = regate(d)
    names = [p.name for p in run_dirs(d / "runs")]
    expect("the retry's prune never deletes a run its verdict composes with (review R1-2)",
        fixed.verdict == "GREEN" and bool(fixed.composed_from) and all(n in names for n in fixed.composed_from)
        and len(measuring_runs(run_dirs(d / "runs")[-1])) == 1 + len(fixed.composed_from), f"{fixed.composed_from} {names}")
    d = root / "compose-code"
    d.mkdir()
    regate(d, plant={"red"})
    code, hc2 = regate(d, trees=["tree-2"])
    expect("a re-gate of a tree that differs in a path a test may read runs the WHOLE population: no retry file, "
        "nothing carried, no composition across two keys",
        code.verdict == "GREEN" and sorted(hc2.envs) == sorted(GATED_ASSEMBLIES) and not code.composed_from
        and all(ONLY_CASES not in v for v in hc2.envs.values()) and not any(code.carried.values()), code.line)
    d = root / "compose-toolchain"
    d.mkdir()
    regate(d, plant={"red"})
    tool, ht = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0")
    expect("a run under another toolchain is never composed with: the whole population runs (the toolchain is a facet "
        "of every target's key, kb/Work PB5218)",
        tool.verdict == "GREEN" and sorted(ht.envs) == sorted(GATED_ASSEMBLIES) and not tool.composed_from, tool.line)
    d = root / "compose-moved"
    d.mkdir()
    regate(d, plant={"red"}, trees=["tree-1", "tree-edited"])
    moved, hm = regate(d)
    expect("a run whose tree changed WHILE it ran contributes no pass: the whole population runs",
        moved.verdict == "GREEN" and sorted(hm.envs) == sorted(GATED_ASSEMBLIES) and not moved.composed_from, moved.line)
    d = root / "compose-inert"
    d.mkdir()
    green1, _ = regate(d)
    inert, hi = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
    inert_rec = read_record(run_dirs(d / "runs")[-1])
    expect("a GREEN code-lane run, then a commit that changes only inert paths, is GREEN with NO fetch, slot, build or "
        "host: the audits ran, the line says EVERY TARGET UP TO DATE and names the run whose passes stand, and the "
        "record names them",
        green1.verdict == "GREEN" and inert.verdict == "GREEN" and inert.exit_code == 0 and hi.events == []
        and hi.slots_taken == 0 and hi.audited and not inert.runs and hi.envs == {} and hi.growth == []
        and "EVERY TARGET UP TO DATE (kb/Work PB5207, PB5218)" in inert.line
        and f"run {run_dirs(d / 'runs')[0].name}" in inert.line
        and {u["run"] for u in inert_rec["up_to_date"].values()} == {run_dirs(d / "runs")[0].name}
        and inert_rec["tree"] == "tree-2" and inert_rec["lane"] == "full", f"{inert.line} {hi.events}")
    reused_inert, hri = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
    expect("a composed GREEN is itself the answer for a re-gate of its tree (REUSED), and `measuring_runs` still reaches "
        "the run that measured the tests",
        "REUSED" in reused_inert.line and hri.events == []
        and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])] == [run_dirs(d / "runs")[1].name,
                                                                          run_dirs(d / "runs")[0].name],
        reused_inert.line)
    code_after, hca = regate(d, trees=["tree-3"], keys={"tree-3": "tree-1"})
    expect("an inert-only commit on top of an up-to-date run is up to date through the run that measured the tests, "
        "never through the up-to-date one's own (absent) trx",
        "EVERY TARGET UP TO DATE" in code_after.line and hca.events == []
        and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])]
        == [run_dirs(d / "runs")[-1].name, run_dirs(d / "runs")[0].name], code_after.line)
    d = root / "compose-code-after-green"
    d.mkdir()
    regate(d)
    code_green, hcg = regate(d, trees=["tree-2"])
    expect("a GREEN code-lane run, then a commit that touches one path a test may read, runs EVERYTHING",
        code_green.verdict == "GREEN" and hcg.events[:3] == ["fetch", "slot", "build"]
        and sorted(hcg.envs) == sorted(GATED_ASSEMBLIES) and not code_green.composed_from, code_green.line)
    d = root / "compose-inert-audit"
    d.mkdir()
    regate(d)
    inert_red, hir = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"}, plant={"audit"})
    expect("a composed gate whose audits are RED is RED with nothing run (the audits are the only work it has)",
        inert_red.verdict == "RED" and hir.events == [] and hir.envs == {} and "RENDERED VIEWS RED" in inert_red.line,
        inert_red.line)
    d = root / "compose-inert-red"
    d.mkdir()
    regate(d, plant={"red"})
    inert_retry, hrr = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
    expect("a RED run, then an inert-only commit, retries only the red case: the inert path costs no case",
        inert_retry.verdict == "GREEN" and sorted(hrr.envs) == ["Conformance"]
        and inert_retry.carried == {"Conformance": 5} and sorted(inert_retry.up_to_date) == ["Characterization", "RepoDrift", "Unit"],
        inert_retry.line)
    same = {"tree-2": "tree-1"}   # tree-2 differs from tree-1 only in paths no target reads
    d = root / "target-hang"
    d.mkdir()
    hang, _ = regate(d, plant={"tooling", "audit-after-hosts"})
    hang_rec = read_record(run_dirs(d / "runs")[-1]) or {}
    expect("an audit that turns RED after every test host has exited (a self-test that hung past the last host) stops no "
        "host: the run is RED for the audit alone, its populations are measured EXACT and each target's pass is "
        "recorded in the store at its key",
        hang.verdict == "RED" and "SELF-TESTS HANG" in hang.line and len(hang.populations) == len(GATED_ASSEMBLIES)
        and all(pp.exact for pp in hang.populations.values()) and hang_rec.get("tree_end") == "tree-1"
        and hang_rec.get("audit_reds") == ["SELF-TESTS HANG"] and sorted(hang.recorded) == sorted(GATED_ASSEMBLIES)
        and all(FakeHost(d, d / "gate.lock").store().up_to_date(t, hang.keys[a])
                for a, t in (("Unit", "unit"), ("RepoDrift", "repo-drift"), ("Conformance", "conformance"),
                             ("Characterization", "characterization"))),
        hang.line)
    tool_hang, hth = regate(d, trees=["tree-2"], keys=same, plant={"tooling"})
    expect("a run RED only for a hung self-test, then a tooling-only difference (every key equal): every target is up to "
        "date, so no corpus fetch and no test host; the audits run with the self-tests, and the change touches a script, "
        "so the post-build self-tests are built and run",
        tool_hang.verdict == "GREEN" and hth.envs == {} and not tool_hang.runs and "fetch" not in hth.events
        and hth.built and hth.audited and hth.tooling_seen is True
        and sorted(tool_hang.up_to_date) == sorted(GATED_ASSEMBLIES)
        and f"run {run_dirs(d / 'runs')[0].name}" in tool_hang.line, f"{tool_hang.line} {hth.events}")
    d = root / "target-hang-code"
    d.mkdir()
    regate(d, plant={"tooling", "audit-after-hosts"})
    hang_code, hhc = regate(d, trees=["tree-2"], plant={"tooling"})
    expect("a run RED only for a hung self-test, then a difference in a path every target reads (every key differs): "
        "nothing is up to date or carried and everything runs",
        hang_code.verdict == "GREEN" and hhc.events[:3] == ["fetch", "slot", "build"]
        and sorted(hhc.envs) == sorted(GATED_ASSEMBLIES) and not hang_code.composed_from and not hang_code.up_to_date
        and not any(hang_code.carried.values()), f"{hang_code.line} {hhc.events}")
    d = root / "target-green-docs"
    d.mkdir()
    regate(d)
    docs, hdo = regate(d, trees=["tree-2"], keys=same)
    expect("a GREEN run, then a change no target reads and that touches no script: EVERY TARGET UP TO DATE, the audits "
        "alone run (no corpus fetch, slot, build or host), and the line names each target's pass",
        docs.verdict == "GREEN" and hdo.events == [] and hdo.slots_taken == 0 and hdo.audited and not hdo.built
        and "EVERY TARGET UP TO DATE" in docs.line and all(a in docs.line for a in GATED_ASSEMBLIES), docs.line)
    d = root / "target-unit-only"
    d.mkdir()
    regate(d)
    unit_only, huo = regate(d, trees=["tree-2"], keys={"tree-2": {"Conformance": "tree-1", "Characterization": "tree-1",
                                                            "RepoDrift": "tree-1"}})
    expect("equal conformance and characterization keys with a changed unit key (a records-only change) run Unit ONLY: "
        "one host, the others up to date, and Unit's new pass is recorded",
        unit_only.verdict == "GREEN" and sorted(huo.envs) == ["Unit"] and "build" in huo.events
        and sorted(unit_only.up_to_date) == ["Characterization", "Conformance", "RepoDrift"] and unit_only.recorded == ["Unit"]
        and "Conformance up to date" in unit_only.line and "Unit 4/4 ran" in unit_only.line, unit_only.line)
    d = root / "target-conformance-input"
    d.mkdir()
    regate(d)
    conf_in, hci = regate(d, trees=["tree-2"], keys={"tree-2": {"Unit": "tree-1", "Characterization": "tree-1",
                                                          "RepoDrift": "tree-1"}})
    expect("a changed input of conformance (its key differs) runs Conformance, and only it",
        conf_in.verdict == "GREEN" and sorted(hci.envs) == ["Conformance"] and conf_in.recorded == ["Conformance"],
        conf_in.line)
    shared = root / "shared-store"
    wa, wb = root / "worktree-a", root / "worktree-b"
    wa.mkdir()
    wb.mkdir()
    in_a = Gate(FakeHost(wa, wa / "gate.lock", store_dir=shared), wa / "runs", say=sink.append).run()
    hb = FakeHost(wb, wb / "gate.lock", store_dir=shared)
    sink.clear()
    in_b = Gate(hb, wb / "runs", say=sink.append).run()
    expect("a pass recorded from worktree A is UP TO DATE in worktree B at the same content (one store, the git common "
        "directory's): B starts no slot, build or host, and names A's run",
        in_a.verdict == "GREEN" and in_b.verdict == "GREEN" and hb.events == [] and hb.envs == {}
        and all(u["worktree"] == str(wa) for u in in_b.up_to_date.values()) and "worktree-a" in in_b.line,
        f"{in_b.line} {hb.events}")
    d = root / "target-facets"
    d.mkdir()
    regate(d)
    other_tools, hot = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0", trees=["tree-2"], keys=same)
    other_plat, hop = regate(d, platform="linux", trees=["tree-3"], keys={"tree-3": "tree-1"})
    expect("another toolchain or another platform is another key: every target runs",
        sorted(hot.envs) == sorted(GATED_ASSEMBLIES) and not other_tools.up_to_date
        and sorted(hop.envs) == sorted(GATED_ASSEMBLIES) and not other_plat.up_to_date, f"{other_tools.line} | {other_plat.line}")
    d = root / "target-not-exact"
    d.mkdir()
    dropped, _ = regate(d, plant={"drop"})
    d_red = root / "target-red"
    d_red.mkdir()
    red, _ = regate(d_red, plant={"red"})
    red_store = FakeHost(d_red, d_red / "gate.lock").store()
    expect("a population that is not EXACT, or a red host, records nothing for that target (the others' passes are "
        "recorded)",
        dropped.verdict == "RED" and "Conformance" not in dropped.recorded and "Unit" in dropped.recorded
        and not FakeHost(d, d / "gate.lock").store().up_to_date("conformance", dropped.keys["Conformance"])
        and red.verdict == "RED" and "Conformance" not in red.recorded
        and not red_store.up_to_date("conformance", red.keys["Conformance"]), f"{dropped.line} | {red.line}")
    d = root / "target-audit-and-postbuild"
    d.mkdir()
    both, _ = regate(d, plant={"tooling", "audit-after-hosts", "post-build"})
    after_both, hab = regate(d, trees=["tree-2"], keys=same, plant={"tooling"})
    expect("a run RED for an audit AND the post-build self-tests, every host green (kb/Work PB5205 review R1-1): its "
        "targets' passes stand, and the next gate at their keys starts no host but builds and runs the post-build "
        "self-tests again",
        both.verdict == "RED" and "POST-BUILD SELF-TESTS RED" in both.line and sorted(both.recorded) == sorted(GATED_ASSEMBLIES)
        and after_both.verdict == "GREEN" and hab.built and hab.envs == {} and "post-build self-tests" in hab.events,
        f"{both.line} | {after_both.line} {hab.events}")
    d = root / "target-carry"
    d.mkdir()
    regate(d, plant={"red"})
    carried_retry, hcr = regate(d, trees=["tree-2"], keys=same)
    expect("at a target's key, an earlier RED run of this worktree leaves only its unpassed case to run (kb/Work PB5099); "
        "the targets that passed are up to date",
        carried_retry.verdict == "GREEN" and sorted(hcr.envs) == ["Conformance"]
        and carried_retry.carried == {"Conformance": 5} and sorted(carried_retry.up_to_date) == ["Characterization", "RepoDrift", "Unit"]
        and carried_retry.recorded == ["Conformance"], carried_retry.line)
    keyrepo = ScratchRepo.init(root / "keyrepo")
    planted_tool, code_tool = "scripts/orchestrator/planted_tool.py", "scripts/standard_streams.py"
    files = {"src/A.cs": "class A {}\n", "docs/n.md": "# n\n", "kb/Work/X.md": "x\n", planted_tool: "x = 1\n",
             code_tool: "y = 1\n", "tests/version-matrix/inventory/r.json": "{}\n",
             "tests/WiseOwl.Cobol.Tests.Unit/U.cs": "class U {}\n", "tests/characterization/positive/p.cob": "p\n",
             ".github/workflows/ci.yml": "on: push\n"}
    keyhost = Host(keyrepo.path)
    kfacets = {"platform": "windows", "configuration": GATE_CONFIGURATION, "toolchain": "t1"}
    suite_out = {"conformance-suite": "d" * 64}   # the generated suite's output digest, as a gate's generation gives it
    def keys_after(edit: dict[str, str], facets: dict[str, str] = kfacets,
                   outputs: dict[str, str] = suite_out) -> dict[str, str]:
        keyrepo.commit("edit", {**files, **edit}, allow_empty=True)
        return keyhost.target_keys(keyhost.head_tree() or "", facets, outputs) or {}
    k0 = keys_after({})
    ks = {"docs": keys_after({"docs/n.md": "# edited\n", "kb/Work/X.md": "y\n", planted_tool: "x = 2\n"}),
          "unit-test": keys_after({"tests/WiseOwl.Cobol.Tests.Unit/U.cs": "class U { int x; }\n"}),
          "char": keys_after({"tests/characterization/positive/p.cob": "q\n"}),
          "workflow": keys_after({".github/workflows/ci.yml": "on: pull_request\n"}),
          "inventory": keys_after({"tests/version-matrix/inventory/r.json": '{"v": 2}\n'}),
          "suite-output": keys_after({}, outputs={"conformance-suite": "e" * 64}),
          "src": keys_after({"src/A.cs": "class A { int b; }\n"}),
          "toolchain": keys_after({}, {**kfacets, "toolchain": "t2"})}
    try:
        unresolved = str(keyhost.target_keys(keyhost.head_tree() or "", kfacets))
    except buildgraph.Unresolved as e:
        unresolved = f"Unresolved: {e}"
    expect("the real keys: a docs, kb or tooling-lane edit leaves every key; a Unit test source changes Unit's and "
        "RepoDrift's keys only (a drift test reads every test source), a characterization program Characterization's "
        "and RepoDrift's and not Unit's, a workflow RepoDrift's only; an inventory edit RepoDrift's only (Unit and "
        "Conformance take the generated suite's digest, kb/Work PB5231, PB5230), and another suite digest Unit's and "
        "Conformance's only; src/ and the toolchain change every key; with no record of the suite in the store a "
        "dependent's key is UNRESOLVED",
        len(k0) == 4 and ks["docs"] == k0
        and ks["unit-test"]["Unit"] != k0["Unit"] and ks["unit-test"]["Conformance"] == k0["Conformance"]
        and ks["unit-test"]["Characterization"] == k0["Characterization"] and ks["unit-test"]["RepoDrift"] != k0["RepoDrift"]
        and ks["char"]["Characterization"] != k0["Characterization"] and ks["char"]["Conformance"] == k0["Conformance"]
        and ks["char"]["Unit"] == k0["Unit"] and ks["char"]["RepoDrift"] != k0["RepoDrift"]
        and ks["workflow"]["RepoDrift"] != k0["RepoDrift"]
        and all(ks["workflow"][a] == k0[a] for a in ("Unit", "Conformance", "Characterization"))
        and ks["inventory"]["RepoDrift"] != k0["RepoDrift"]
        and all(ks["inventory"][a] == k0[a] for a in ("Unit", "Conformance", "Characterization"))
        and ks["suite-output"]["Conformance"] != k0["Conformance"] and ks["suite-output"]["Unit"] != k0["Unit"]
        and ks["suite-output"]["RepoDrift"] == k0["RepoDrift"]
        and ks["suite-output"]["Characterization"] == k0["Characterization"]
        and all(ks["src"][a] != k0[a] and ks["toolchain"][a] != k0[a] for a in GATED_ASSEMBLIES)
        and unresolved.startswith("Unresolved: ") and "needs the output digest of conformance-suite" in unresolved,
        f"{ks} {unresolved}")
    grepo = ScratchRepo.init(root / "genrepo")
    grepo.commit("base", {".gitignore": "TestResults/\n__pycache__/\n", "gen/make.py": buildgraph.PLANTED_GENERATOR,
                          "inv/r1.json": '{"id": "r1", "verdict": "GAP"}\n', "src/A.cs": "class A {}\n",
                          "tests/C/T.cs": "class T {}\n", "tests/U/U.cs": "class U {}\n",
                          "tests/H/H.cs": "class H {}\n", "tests/R/R.cs": "class R {}\n"})
    every = ("platform", "configuration", "toolchain")
    ggraph = buildgraph.Graph.of({
        "suite": buildgraph.GeneratedTarget(
            name="suite", inputs=("gen/", "inv/"), exclude=("@inert",), facets=("toolchain",),
            command=("gen/make.py", "--out", buildgraph.OUT), outputs=("suite/manifest.json",),
            env="WISEOWL_COBOL_GATE_SUITE"),
        "unit": buildgraph.TestTarget(name="unit", assembly="Unit", project="tests/U/", inputs=("src/", "tests/U/", "inv/"),
                                      exclude=("@inert",), facets=every),
        "conformance": buildgraph.TestTarget(name="conformance", assembly="Conformance", project="tests/C/",
                                             inputs=("src/", "tests/C/"), exclude=("@inert",), facets=every,
                                             depends=("suite",)),
        "repo-drift": buildgraph.TestTarget(name="repo-drift", assembly="RepoDrift", project="tests/R/",
                                            inputs=("src/", "tests/R/"), exclude=("@inert",), facets=every),
        "characterization": buildgraph.TestTarget(name="characterization", assembly="Characterization",
                                                  project="tests/H/", inputs=("src/", "tests/H/"), exclude=("@inert",),
                                                  facets=every)}, every)
    gstore, gd = root / "gen-store", root / "gen-gates"
    gd.mkdir()
    suite_dir = str(grepo.path / buildgraph.GENERATED_ROOT / "suite")
    def ggate(edit: dict[str, str | None] | None = None, **fake) -> tuple[Outcome, FakeHost]:
        if edit:
            grepo.commit("edit", edit)
        host = FakeHost(gd, gd / "gate.lock", graph=ggraph, repo=grepo.path, store_dir=gstore,
                        trees=[grepo.git("rev-parse", "HEAD^{tree}")], **fake)
        sink.clear()
        return Gate(host, gd / "runs", say=sink.append).run(), host
    g0, hg0 = ggate()
    expect("a gate brings the generated target up to date first (its generator traced and run) and hands its output "
        "directory to Conformance's host and listing, and to no other assembly's; the line names the generation",
        g0.verdict == "GREEN" and g0.generated["suite"]["ran"] and sorted(g0.recorded) == sorted(GATED_ASSEMBLIES)
        and hg0.envs["Conformance"].get("WISEOWL_COBOL_GATE_SUITE") == suite_dir
        and hg0.listing_handed["Conformance"] == {"WISEOWL_COBOL_GATE_SUITE": suite_dir}
        and "WISEOWL_COBOL_GATE_SUITE" not in hg0.envs["Unit"] and "suite generated in" in g0.line,
        f"{g0.line} {hg0.envs.get('Conformance', {}).get('WISEOWL_COBOL_GATE_SUITE')}")
    g1, hg1 = ggate({"inv/r1.json": '{"id": "r1", "verdict": "PASS"}\n'})
    expect("an inventory edit that leaves the generator's output unchanged: the generator re-runs, Conformance's key is "
        "unchanged and Conformance is UP TO DATE (no host); Unit, which reads the inventory, runs",
        g1.verdict == "GREEN" and g1.generated["suite"]["ran"] and g1.keys["Conformance"] == g0.keys["Conformance"]
        and sorted(hg1.envs) == ["Unit"] and "Conformance up to date" in g1.line
        and g1.generated["suite"]["outputs"] == g0.generated["suite"]["outputs"], g1.line)
    g2, hg2 = ggate({"inv/r2.json": '{"id": "r2", "verdict": "GAP"}\n'})
    expect("an inventory edit that changes the generator's output changes Conformance's key: Conformance runs on the new "
        "suite",
        g2.verdict == "GREEN" and g2.keys["Conformance"] != g1.keys["Conformance"] and "Conformance" in hg2.envs
        and "Conformance" in g2.recorded and g2.generated["suite"]["outputs"] != g1.generated["suite"]["outputs"],
        g2.line)
    g3, hg3 = ggate({"gen/make.py": buildgraph.PLANTED_GENERATOR + "# edited\n"})
    expect("a generator source edit re-runs the generator; its output unchanged, every test target is UP TO DATE and the "
        "line says the suite was generated",
        g3.verdict == "GREEN" and g3.generated["suite"]["ran"] and g3.generated["suite"]["key"] != g2.generated["suite"]["key"]
        and hg3.envs == {} and "EVERY TARGET UP TO DATE" in g3.line and "suite generated in" in g3.line, g3.line)
    g4, hg4 = ggate({"docs/n.md": "# a path no target reads: another tree, so no REUSED record answers\n"})
    expect("its inputs unchanged: the generated target is up to date (its record and its outputs on disk agree), nothing runs",
        g4.verdict == "GREEN" and not g4.generated["suite"]["ran"] and "suite up to date" in g4.line and hg4.envs == {},
        g4.line)
    rec_file = buildgraph.Store(gstore)._path("suite", g4.generated["suite"]["key"])
    rec_file.unlink()
    g5, hg5 = ggate({"docs/n.md": "# again\n"})
    expect("a missing dependency record is not up to date: with its outputs still on disk the generator runs again "
        "(no record at its key), and only then is Conformance, at its unchanged key, up to date",
        g5.verdict == "GREEN" and g5.generated["suite"]["ran"] and g5.generated["suite"]["why"] == "no record at its key"
        and rec_file.is_file() and g5.keys["Conformance"] == g4.keys["Conformance"] and hg5.envs == {}, g5.line)
    g6, hg6 = ggate({"tests/C/T.cs": "class T { int x; }\n"}, plant={"tamper-output"})
    expect("a host that changes the generated output it was handed makes the gate RED and records no pass of its target",
        g6.verdict == "RED" and "GENERATED OUTPUT CHANGED" in g6.line and "Conformance" not in g6.recorded
        and not buildgraph.Store(gstore).up_to_date("conformance", g6.keys["Conformance"]), g6.line)
    g6b, _ = ggate({"tests/C/T.cs": "class T { int y; }\n"}, plant={"own-generation"})
    expect("a gate whose Conformance host generated its own suite instead of the handed one is RED and records no pass "
        "of its target (kb/Work PB5231 R1-2)",
        g6b.verdict == "RED" and "GENERATED BY THE HOST" in g6b.line and "Conformance" not in g6b.recorded
        and not buildgraph.Store(gstore).up_to_date("conformance", g6b.keys["Conformance"]), g6b.line)
    g7, _ = ggate({"gen/NONDET": "x\n"})
    remove_tree(grepo.path / buildgraph.GENERATED_ROOT / "suite")
    g8, hg8 = ggate({"docs/n.md": "# the generator's inputs as at g7\n"})
    expect("a nondeterministic generator (planted: the time) is REFUSED when its outputs are generated again at a key: "
        "the gate is RED naming it, and no test host runs",
        g7.verdict == "GREEN" and g8.verdict == "RED" and "GENERATED TARGET RED — Nondeterministic" in g8.line
        and hg8.envs == {} and not hg8.built, g8.line)
    g7_rec = next((r for r in map(read_record, run_dirs(gd / "runs")) if r and r.get("line") == g7.line), {})
    rgate = Gate(FakeHost(gd, gd / "gate.lock", graph=ggraph, repo=grepo.path, store_dir=gstore), gd / "runs",
                 say=sink.append)
    own_refusals: list[str] = []
    own_keys, own_refused = rgate.passes._rederived_keys(g7_rec, own_refusals)
    suite_rec_path = buildgraph.Store(gstore)._path("suite", g7_rec["generated"]["suite"]["key"])
    kept_text = suite_rec_path.read_text(encoding="utf-8")
    suite_rec_path.write_text(kept_text.replace(g7_rec["generated"]["suite"]["outputs"], "f" * 64), encoding="utf-8",
                              newline="\n")
    moved_refusals: list[str] = []
    rgate.passes._rederived_keys(g7_rec, moved_refusals)
    suite_rec_path.write_text(kept_text, encoding="utf-8", newline="\n")
    old_refusals: list[str] = []
    old_keys, old_refused = rgate.passes._rederived_keys({k: v for k, v in g7_rec.items() if k != "generated"}, old_refusals)
    expect("a re-derivation keys Conformance on the digest the run itself was handed; one whose digest the store now "
        "contradicts is refused naming both; a run from before the hand-off has Conformance REFUSED, never stored",
        own_refusals == [] and own_refused == {} and own_keys == g7_rec["keys"]
        and len(moved_refusals) == 1 and g7_rec["generated"]["suite"]["outputs"][:12] in moved_refusals[0]
        and "f" * 12 in moved_refusals[0]
        and old_refusals == [] and sorted(old_refused) == ["Conformance"] and "Conformance" not in (old_keys or {})
        and (old_keys or {}).get("Unit") == g7_rec["keys"]["Unit"],
        f"{own_refusals} {own_refused} | {moved_refusals} | {old_refusals} {old_refused}")
    cyclic = root / "cyclic-targets.json"
    cyclic.write_text(json.dumps({"facets": ["toolchain"], "build": {"inputs": {}}, "targets": {
        "a": {"kind": "generated", "command": ["a.py", "{out}"], "outputs": {"x": ""}, "env": "WISEOWL_COBOL_GATE_A",
              "inputs": {"a.py": ""}, "depends": ["b"]},
        "b": {"kind": "generated", "command": ["b.py", "{out}"], "outputs": {"x": ""}, "env": "WISEOWL_COBOL_GATE_B",
              "inputs": {"b.py": ""}, "depends": ["a"]}}}), encoding="utf-8", newline="\n")
    cd = root / "gate-cycle"
    cd.mkdir()
    chost = FakeHost(cd, cd / "gate.lock")
    chost.graph = lambda: buildgraph.load_targets(cyclic)   # the targets file has a cycle
    sink.clear()
    cyc = Gate(chost, cd / "runs", say=sink.append).run()
    expect("a dependency cycle in the targets file is refused: the gate is RED naming the cycle, and nothing is built or run",
        cyc.verdict == "RED" and "a dependency cycle: a -> b -> a" in cyc.line and not chost.built and chost.envs == {},
        cyc.line)
    def old_stopped(d: Path, keep_listings: bool = False) -> Path:
        regate(d)
        r = run_dirs(d / "runs")[-1]
        rec = read_record(r) or {}
        for k in ("keys", "audit_reds", "up_to_date", "recorded"):
            rec.pop(k, None)
        rec.update(verdict="RED", exit_code=1, reasons=["SELF-TESTS HANG"], populations={}, tree_end=None,
                   line=f"{VERDICT}RED — SELF-TESTS HANG — fail-fast: an audit that ran beside the test hosts was RED "
                        "and STOPPED them: their results are partial and NOT measured ===")
        (r / VERDICT_FILE).write_text(json.dumps(rec), encoding="utf-8", newline="\n")
        remove_tree(d / "store")   # the old gate recorded nothing in the store
        if not keep_listings:
            for f in r.glob("listing-*.txt"):
                f.unlink()
        return r
    def rederive_in(d: Path, r: Path, **fake) -> tuple[Rederived, FakeHost]:
        host = FakeHost(d, d / "gate.lock", **fake)
        return PassCarry(host, d / "runs", say=sink.append).rederive(r), host
    d = root / "rederive-passes"
    d.mkdir()
    r = old_stopped(d)
    rd, _ = rederive_in(d, r)
    PassCarry(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append).write_rederived(r, rd) if rd.record else None
    written, original = read_record(r) or {}, json.loads((r / ORIGINAL_VERDICT_FILE).read_text(encoding="utf-8")) \
        if (r / ORIGINAL_VERDICT_FILE).exists() else {}
    after, hpa = regate(d, trees=["tree-2"], keys=same)
    expect("a stopped run with complete green trx and no listing is RECOVERED from its own files (passes): its record "
        "gains tree_end, keys and `rederived` (with the residue: an edit undone at ANY time before the re-derivation), "
        "stays RED, keeps the original beside it, stores nothing, and the next gate at its keys builds and lists but "
        "starts no test host, then records the passes",
        rd.record is not None and rd.level == "passes" and written.get("tree_end") == "tree-1"
        and len(written.get("keys") or {}) == len(GATED_ASSEMBLIES) and written.get("exit_code") == 1
        and original.get("tree_end") is None
        and "undone at any time before this re-derivation" in written.get("rederived", {}).get("residue", "")
        and not rd.stored and after.verdict == "GREEN" and "build" in hpa.events and hpa.envs == {}
        and sum(after.carried.values()) == sum(map(len, FakeHost.POPULATION.values()))
        and sorted(after.recorded) == sorted(GATED_ASSEMBLIES),
        f"{rd.refusals} {after.line} {hpa.events}")
    d = root / "rederive-green"
    d.mkdir()
    r = old_stopped(d, keep_listings=True)
    rd, _ = rederive_in(d, r)
    PassCarry(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append).write_rederived(r, rd) if rd.record else None
    after, hpg = regate(d, trees=["tree-2"], keys=same)
    expect("a stopped run with complete green trx and its keys' listings is RECOVERED EXACT: the store holds each "
        "target's pass, so the next gate at those keys finds every target up to date with no build and no test host",
        rd.record is not None and rd.level == "stored" and sorted(rd.stored) == sorted(GATED_ASSEMBLIES)
        and after.verdict == "GREEN" and hpg.events == [] and hpg.envs == {} and "EVERY TARGET UP TO DATE" in after.line,
        f"{rd.refusals} {after.line}")
    d = root / "rederive-unfinished"
    d.mkdir()
    r = old_stopped(d)
    (r / "Unit.log").write_text("  Passed N.Unit.A [1 s]\n", encoding="utf-8", newline="\n")   # killed: no verdict line
    unfinished, _ = rederive_in(d, r)
    d = root / "rederive-short"
    d.mkdir()
    r = old_stopped(d)
    synthetic_trx(r / "Conformance.trx", [(n, ["Passed"]) for n in FakeHost.POPULATION["Conformance"][:-1]])
    short, _ = rederive_in(d, r)
    expect("a run with a host that never finished (no `Passed!` line) or a SHORT trx (fewer results than its host's line) "
        "is REFUSED, naming the assembly and what is missing, and nothing is written",
        unfinished.record is None and any("Unit" in x and "never finished" in x for x in unfinished.refusals)
        and short.record is None and any("Conformance" in x and "not complete" in x for x in short.refusals)
        and read_record(r).get("rederived") is None, f"{unfinished.refusals} | {short.refusals}")
    d = root / "rederive-moved"
    d.mkdir()
    r = old_stopped(d)
    moved_at = int(run_started(r) or 0) + 1   # a second after the gate started, by the hosts' own clock
    os.utime(r / "Conformance.log", (moved_at + 1, moved_at + 1))   # the hosts' last write: after the move
    host = FakeHost(d, d / "gate.lock")
    host.reflog = [("head-2", moved_at), ("head-1", 0)]
    moved = PassCarry(host, d / "runs", say=sink.append).rederive(r)
    expect("a run whose HEAD moved while it ran (a reflog entry between its start and its hosts' last write) is REFUSED",
        moved.record is None and any("HEAD moved while the run ran" in x for x in moved.refusals), str(moved.refusals))
    d = root / "rederive-toolchain"
    d.mkdir()
    r = old_stopped(d)
    other, _ = rederive_in(d, r, tools="dotnet-sdk 9.0.100, python 3.12.0")
    d = root / "rederive-reason"
    d.mkdir()
    r = old_stopped(d)
    rec = read_record(r) or {}
    (r / VERDICT_FILE).write_text(json.dumps({**rec, "reasons": ["SELF-TESTS HANG", "Unit POPULATION UNMEASURED (x)"]}),
                                  encoding="utf-8", newline="\n")
    reason, _ = rederive_in(d, r)
    expect("a run recorded under another toolchain, or RED for a reason no audit gave, is REFUSED",
        other.record is None and any("toolchain" in x for x in other.refusals)
        and reason.record is None and any("reasons no audit gave" in x for x in reason.refusals),
        f"{other.refusals} | {reason.refusals}")
    d = root / "record"
    d.mkdir()
    lin, hl = regate(d)
    lrec = read_record(run_dirs(d / "runs")[-1])
    expect("the gate runs nothing on Linux: one slot, no Linux call, no Linux key in the record and no Linux half in "
        "the verdict line (kb/Work PB4327)",
        lin.verdict == "GREEN" and lin.exit_code == 0 and hl.slots_taken == 1
        and not any("linux" in e for e in hl.events) and "linux" not in lrec and "Linux half" not in lin.line
        and lrec["head_tree"] == "tree-1", f"{lin.line} {hl.events} {sorted(lrec)}")
    old_rec = {k: v for k, v in lrec.items() if k not in LANDING_FIELDS}
    expect("the record the gate writes is current for the landing (schema, every landing field, not 'older than the "
        "lander'); one lacking a landing field or of a schema below the landable one is, and names which",
        record_schema(lrec) == RECORD_SCHEMA >= LANDABLE_SCHEMA and all(f in lrec for f in LANDING_FIELDS)
        and predates_landing(lrec) is None
        and "no head_tree field" in (predates_landing(old_rec) or "")
        and "below 3" in (predates_landing({**lrec, "schema": 2}) or "")
        and predates_landing({**lrec, "schema": 3}) is None and record_schema({"schema": True}) == 0
        and record_schema({}) == 0, f"{lrec.get('schema')} {predates_landing(old_rec)}")
    globals_before = (gate_runs.RECORD_SCHEMA, dict(SCHEMA_NOTES))
    try:
        gate_runs.RECORD_SCHEMA = globals_before[0] + 1
        SCHEMA_NOTES[gate_runs.RECORD_SCHEMA] = "PB9999"
        bumped = predates_landing(old_rec) or ""
    finally:
        gate_runs.RECORD_SCHEMA = globals_before[0]
        SCHEMA_NOTES.clear()
        SCHEMA_NOTES.update(globals_before[1])
    expect("the 'predates kb/Work <note>' sentence is derived from SCHEMA_NOTES: every schema from the oldest landable "
        "one to the current has a row, the sentence names the current schema's note, and a bump names its own",
        all(n in SCHEMA_NOTES for n in range(LANDABLE_SCHEMA, RECORD_SCHEMA + 1))
        and f"predates kb/Work {SCHEMA_NOTES[RECORD_SCHEMA]})" in (predates_landing(old_rec) or "")
        and "predates kb/Work PB9999)" in bumped,
        f"{RECORD_SCHEMA} {SCHEMA_NOTES} {predates_landing(old_rec)} | {bumped}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
