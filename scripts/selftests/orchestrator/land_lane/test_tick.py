#!/usr/bin/env python3
"""Cases of land_lane.py: the tick: planted surveys, the attempts' lifecycle, the stops, the lock, the lane's view (ported from the self-test of scripts/orchestrator/land_lane.py, DESIGN-orchestrator-loop.md
section 16.2, kb/Work PB5434). The cases run in name order over ONE world (`fixtures.world()`, bound by case_000), as the
sequential self-test did: a case reads what an earlier one planted (the module globals below).
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_tick.py"""
from __future__ import annotations

import sys
import selftest_cases
from standard_streams import ensure_utf8

import fixtures
from detached import spawn_detached
from enqueue import EXIT_GATE_BUSY
from enqueue import EXIT_LAND_BUSY
from enqueue import EXIT_PREDECESSOR_QUEUED
from enqueue import PR_LIST_FIELDS
from enqueue import land_lock_path
from gate_slot import ExclusiveLock
from scratch_repo import ScratchRepo
import coord
import datetime as dt
import json
import os
import pathlib
from fixtures import Fake, finish, fresh, row

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
ATTEMPTS = ll.ATTEMPTS
FREE = ll.FREE
HALT = ll.HALT
MAIN = ll.MAIN
REARM = ll.REARM
SURVEY = ll.SURVEY
SURVEY_MAX_AGE_MIN = ll.SURVEY_MAX_AGE_MIN
TICK_LOCK = ll.TICK_LOCK
agent_work = ll.agent_work
attempts = ll.attempts
classify = ll.classify
collect = ll.collect
lane_dir = ll.lane_dir
now_utc = ll.now_utc
row_dicts = ll.row_dicts
show_conflict_brief = ll.show_conflict_brief
status_lines = ll.status_lines
tick = ll.tick
wait_free = ll.wait_free

# the world's state: bound by the cases in order, read by the later ones
c = c3b = c6 = c8 = f = f3b = f6 = gate = github = lock_wt = planted = prune_worktrees = queue_facts = root = None


def case_000_planted_survey_the_waiting_to_land_rows_start_in(expect):
    global c, f, root
    root = fixtures.world()

    # 1. planted survey rows: exactly the WAITING TO LAND rows start, in survey order; a second tick starts none.
    c = fresh("survey")
    f = Fake([row("w1a", "WAITING TO LAND", "a1"), row("w1b", "WAITING FOR REVIEW", "b1"),
              row("w1c", "WAITING TO LAND", "c1"), row("w1d", "ENQUEUED", "d1"), row("w1e", "IN FLIGHT", "e1")])
    expect("planted survey: the WAITING TO LAND rows start, in survey order, and nothing else", f.tick(c) == ["w1a", "w1c"],
           str(f.started))


def case_001_the_tick_writes_the_survey_it_decided_from_surve(expect):
    s = coord.read_json(lane_dir(c) / SURVEY, {})
    expect("the tick writes the survey it decided from (survey.json)",
           [r["name"] for r in s.get("rows", [])] == ["w1a", "w1b", "w1c", "w1d", "w1e"], str(s))
    expect("a second tick while both run starts nothing (being landed)", f.tick(c) == [], str(f.started))


def case_002_agent_work_the_row_waiting_for_review_is_the_rev(expect):
    w = agent_work(c)
    expect("agent work: the row WAITING FOR REVIEW is the review lane's, not the enqueue unit's (kb/Work PB3608), "
           "and the runs in flight are nobody's", w.reasons == [], str(w.reasons))


def case_003_the_survey_s_rows_map_to_the_tick_s_dicts_a_repo(expect):
    global planted, prune_worktrees

    # 1b. the real mapping from the survey's rows (the Fake above stands in for it): a row's report reaches the
    # review lane as its file's PATH, which it stats and reads (kb/Work PB4055: the mapping read a field the
    # survey's report lacked, and every tick on main crashed)
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports this module back, lazily)
    from reports import IndexedReport  # noqa: PLC0415
    rep_path = root / "reports" / "w1a-PB1-report.md"
    planted = [prune_worktrees.Row("w1a", "CHECK", [], [], 1, str(root / "wt-w1a"), None, 0, 5.0, "",
                                   IndexedReport(rep_path, "DONE", "PB1", False, 5.0), "WAITING FOR REVIEW",
                                   tip="a1"),
               prune_worktrees.Row("w1b", "UNLANDED", [], [], 1, None, None, 0, 5.0, "", None, "DECISION")]
    mapped = row_dicts(planted, None)
    expect("the survey's rows map to the tick's dicts: a reported row carries its report's path and lead, an "
           "unreported one None for both",
           [(d["name"], d["report"], d["lead"], d["conflicts"]) for d in mapped]
           == [("w1a", str(rep_path), "PB1", False), ("w1b", None, None, False)], str(mapped))


def case_004_with_github_observed_a_pull_request_github_canno(expect):
    global queue_facts
    import queue_facts  # noqa: PLC0415
    dirty = {"w1a": queue_facts.PullRequest(1, "u1", "a1", False, merge_state="DIRTY"),
             "w1b": queue_facts.PullRequest(2, "u2", "b1", True, merge_state="DIRTY")}
    expect("... with GitHub observed, a pull request GitHub cannot merge (DIRTY, not queued) is a conflict, a queued "
           "one is not (PullRequest.conflicts read through the real mapping)",
           [d["conflicts"] for d in row_dicts(planted, dirty)] == [True, False], str(row_dicts(planted, dirty)))


def case_005_and_a_queued_head_the_queue_s_simulation_finds_c(expect):
    foreseen_row = prune_worktrees.Row("w1c", "CHECK", [], [], 1, None, None, 0, 5.0, "", None, "BOUNCED", tip="c1",
                                       foreseen=(("w1a", "a1"),))
    expect("... and a queued head the queue's simulation finds conflicting with an entry ahead of it (kb/Work PB3723) "
           "is a conflict too, naming that entry's branch and head for the brief",
           [(d["conflicts"], d["foreseen"]) for d in row_dicts([foreseen_row], {})]
           == [(True, [{"branch": "w1a", "head": "a1"}])], str(row_dicts([foreseen_row], {})))


def case_006_exit_5_is_never_relaunched_at_the_same_head(expect):

    # 2. exit 5 (MERGE NOT COMPLETED, BRANCH-CARRIED) is never relaunched at the same head; a new head (a finisher's commit) is eligible.
    f.rows[0] = row("w1a", "DECISION", "a2")                  # enqueue merged main in: the survey no longer lists it
    finish(c, "w1a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on a2", end_head="a2")
    f.live.discard(40001)
    f.rows[0] = row("w1a", "WAITING TO LAND", "a2")           # even listed again at that head
    expect("exit 5 is never relaunched at the same head", f.tick(c) == [] and f.tick(c) == [], str(f.started))


def case_007_a_stopped_attempt_is_the_enqueue_unit_s_agent_wo(expect):
    w = agent_work(c)
    expect("a stopped attempt is the enqueue unit's agent work until collected",
           any(r.startswith("stopped landings: w1a (exit 5)") for r in w.reasons), str(w.reasons))
    expect("status names the stopped attempt and its finisher", any(
        ln.startswith("STOPPED") and "w1a" in ln and "step 4 finisher" in ln for ln in status_lines(c)),
        "\n".join(status_lines(c)))


def case_008_collect_clears_it_from_the_agent_work_and_it_sta(expect):
    collect(c, ["w1a"], say=lambda s: None)
    expect("collect clears it from the agent work, and it stays unlaunched at that head",
           not any("w1a" in r for r in agent_work(c).reasons) and f.tick(c) == [], str(agent_work(c).reasons))


def case_009_a_row_the_survey_reads_stopped_is_not_launched_a(expect):
    f.rows[0] = row("w1a", "STOPPED", "a2")                   # the survey's own reading of that stop (PB3125)
    expect("a row the survey reads STOPPED is not launched, and once collected is no agent work again",
           f.tick(c) == [] and not any("w1a" in r for r in agent_work(c).reasons), str(agent_work(c).reasons))


def case_010_a_new_head_is_eligible_again(expect):
    f.rows[0] = row("w1a", "WAITING TO LAND", "a3")           # the finisher's fix, reviewed CLEAN
    expect("a new head is eligible again", f.tick(c) == ["w1a"], str(f.started))


def case_011_exit_3_is_a_stop_never_retried_at_the_same_head(expect):

    # 3. exit 3 (the landing check's STOP: a never-carried path or a duplicate public-API symbol; an edit meeting
    #    another branch's no longer stops, kb/Work PB4452 first ready lands): only a new commit clears it, so it is a
    #    STOP, the enqueue unit's (a finisher), never retried at the same head.
    c3 = fresh("check-stop")
    f3 = Fake([row("w2a", "WAITING TO LAND", "h1")])
    f3.tick(c3)
    finish(c3, "w2a", 3, "NOT QUEUED (exit 3, above)", end_head="h2")
    f3.live.clear()
    f3.rows[0] = row("w2a", "IN FLIGHT", "h2")
    f3.free = True
    expect("exit 3 is a STOP: never retried at the same head, and it is the enqueue unit's",
           f3.tick(c3) == [] and f3.asked == [] and any("w2a" in r for r in agent_work(c3).reasons),
           f"{f3.started} {f3.asked} {agent_work(c3).reasons}")


def case_012_exit_12_while_the_gate_lock_is_held_not_relaunch(expect):
    global c3b, f3b

    # 3b. exit 12 (GATE BUSY: another gate held the worktree's gate lock, kb/Work PB3527): never the enqueue unit's
    #     and never a red; retried at the same head once the lock is free.
    c3b = fresh("busy")
    f3b = Fake([row("w2b", "WAITING TO LAND", "g1")])
    f3b.tick(c3b)
    finish(c3b, "w2b", EXIT_GATE_BUSY, "GATE BUSY (Windows) on g2 — another gate holds the worktree's gate lock",
           end_head="g2")
    f3b.live.clear()
    f3b.rows[0] = row("w2b", "IN FLIGHT", "g2")
    expect("exit 12 while the gate lock is held: not relaunched, not the enqueue unit's work, and status says the "
           "lane retries it once the lock is free",
           f3b.tick(c3b) == [] and agent_work(c3b).reasons == []
           and any("w2b" in ln and "gate lock is free" in ln for ln in status_lines(c3b)), str(status_lines(c3b)))


def case_013_exit_12_once_the_gate_lock_is_free_retried_at_th(expect):
    f3b.free = True
    expect("exit 12 once the gate lock is free: retried at the same head", f3b.tick(c3b) == ["w2b"], str(f3b.started))


def case_014_wait_free_for_exit_12_reads_the_gate_driver_s_ow(expect):
    global gate, lock_wt
    lock_wt = ScratchRepo.init(root / "wt-lock").path
    import gate  # noqa: PLC0415

    held = ExclusiveLock.try_take(gate.Host(lock_wt).lock_path(), "gate (pid 356316)")
    try:
        while_held = wait_free(EXIT_GATE_BUSY, str(lock_wt))
    finally:
        held.release()
    after = wait_free(EXIT_GATE_BUSY, str(lock_wt))
    expect("wait_free for exit 12 reads the gate driver's own lock of the worktree: held names its holder, then free",
           while_held[0] is False and "pid 356316" in while_held[1] and after == (True, ""), f"{while_held} {after}")


def case_015_exit_12_for_a_gate_that_is_still_running_the_lan(expect):
    # exit 12 for the branch's OWN gate not having written its verdict yet (kb/Work PB3662): the lane launched the
    # landing beside the implementer's running gate (a resume merged main in, the gate started, the survey read the
    # new tip WAITING TO LAND). The same head must relaunch once the run writes its verdict, with no `rearm`.
    c3c = fresh("owed")
    f3c = Fake([row("w2c", "WAITING TO LAND", "o1")])
    f3c.wait = wait_free
    gate_wt = ScratchRepo.init(root / "wt-w2c").path
    owed_run = gate_wt / gate.RUN_ROOT / "000005-20261024T225004Z"
    owed_run.mkdir(parents=True)
    # The implementer's gate holds the worktree's gate lock until it has written its verdict. Its gate.pid names THIS
    # process, which stays alive afterwards: a process id says nothing about the gate (review R1-1: Windows reuses a
    # dead gate's), so the lane relaunches the moment the lock is free, whatever that pid file still names.
    gate.write_pid(owed_run, os.getpid())
    gate_held = ExclusiveLock.try_take(gate.Host(gate_wt).lock_path(),
                                       f"gate {gate_wt} (pid 356318, since T{gate.RUN_LABEL}{owed_run})")
    try:
        f3c.tick(c3c)
        finish(c3c, "w2c", EXIT_GATE_BUSY, "GATE BUSY — the gate run 000005-20261024T225004Z of this worktree is "
               "still running", end_head="o1")
        f3c.live.clear()
        waiting = f3c.tick(c3c)
        waiting_said = " | ".join(f3c.said)
        owed_now = wait_free(EXIT_GATE_BUSY, str(gate_wt))
    finally:
        gate_held.release()                                    # the gate ended (or died): no verdict file, pid alive
    relaunched = f3c.tick(c3c)
    expect("exit 12 for a gate that is still running: the lane waits at that head while the worktree's gate lock is "
           "held (wait_free names the run it writes), and relaunches the SAME head once the lock is free, with no "
           "rearm and no enqueue-unit work, however alive its gate.pid process still reads (kb/Work PB3662, "
           "review R1-1)",
           waiting == [] and "000005-20261024T225004Z" in owed_now[1] and owed_now[0] is False
           and "still waits on" in waiting_said and relaunched == ["w2c"] and agent_work(c3c).reasons == []
           and not (lane_dir(c3c) / REARM).exists(), f"{waiting} {waiting_said} {owed_now} {relaunched}")


def case_016_wait_free_for_exit_13_reads_enqueue_py_s_own_lan(expect):
    # exit 13 (kb/Work PB3310): another `land` of the branch runs; the lane waits on the worktree's land lock, which
    # is enqueue.py's own (land_lock_path), exactly as it waits on the gate driver's for exit 12.
    land_held = ExclusiveLock.try_take(land_lock_path(lock_wt), "enqueue.py land x (pid 356317)")
    try:
        land_while_held = wait_free(EXIT_LAND_BUSY, str(lock_wt))
    finally:
        land_held.release()
    land_after = wait_free(EXIT_LAND_BUSY, str(lock_wt))
    expect("wait_free for exit 13 reads enqueue.py's own land lock of the worktree: held names its holder, then free; "
           "the exit is a retry-when-free class, never the enqueue unit's",
           land_while_held[0] is False and "pid 356317" in land_while_held[1] and land_after == (True, "")
           and classify({"rc": EXIT_LAND_BUSY}) == FREE, f"{land_while_held} {land_after}")


def case_017_exit_15_a_merged_predecessor_s_pull_request_is_s(expect):
    global github
    # exit 15 (kb/Work PB3145): the branch merged a predecessor whose pull request (#701) is still open, so it adds the
    # predecessor's stamped entry beside its own. The lane waits on enqueue.py's own reader (open_predecessors) and
    # relaunches the SAME head once no open pull request owns an entry the branch carries, with no rearm.
    pw = ScratchRepo.init(root / "wt-w2d")
    (pw.path / "DEVLOG").mkdir()
    (pw.path / "DEVLOG" / "0001-pb1-base.md").write_text(
        "## Entry 1 — 2026-10-01 09:00 PDT — PB1: base\n\nWhy.\n", encoding="utf-8", newline="\n")
    pw.git("add", "DEVLOG")
    pw.git("commit", "--quiet", "-m", "base")
    pw.origin(root / "origin-w2d.git")
    pw.git("push", "--quiet", "origin", "main")
    pw.git("switch", "--quiet", "-c", "w2d")
    (pw.path / "DEVLOG" / "0050-pb9-the-predecessor.md").write_text(
        "## Entry 50 — 2026-10-24 10:00 PDT — PB9: the predecessor\n\nWhy.\n", encoding="utf-8", newline="\n")
    pw.git("add", "DEVLOG")
    pw.git("commit", "--quiet", "-m", "the merged predecessor's stamped entry")
    predecessor_oid = pw.git("rev-parse", "HEAD")
    (pw.path / "own.txt").write_text("the branch's own work", encoding="utf-8", newline="\n")
    pw.git("add", "own.txt")
    pw.git("commit", "--quiet", "-m", "the branch's own work")
    pred_listing = {"number": 701, "headRefName": "pred", "headRefOid": predecessor_oid,
                    "files": [{"path": "DEVLOG/0050-pb9-the-predecessor.md"}]}
    github = {"open": [pred_listing], "fail": False}

    def planted_gh(args: list[str]) -> tuple[int, str, str]:
        """GitHub as enqueue.py reads it: the listing of the open pull requests and queue_facts.read's two calls, each
        open pull request with auto-merge on."""
        import queue_facts  # noqa: PLC0415
        if args[:2] in (["repo", "view"], ["api", "graphql"]):
            return (1, "", "rate limited") if github["fail"] else queue_facts.fake([
                queue_facts.node(pr["number"], pr["headRefName"], pr["headRefOid"],
                                 auto="t")
                for pr in github["open"]])(args)
        assert args[:2] == ["pr", "list"] and PR_LIST_FIELDS in args, args
        return (1, "", "rate limited") if github["fail"] else (0, json.dumps(github["open"]), "")
    c3d = fresh("predecessor")
    f3d = Fake([row("w2d", "WAITING TO LAND", "p1")])
    f3d.wait = lambda rc, wt_: wait_free(rc, wt_, gh=planted_gh)
    f3d.rows[0]["worktree"] = str(pw.path)
    f3d.tick(c3d)
    finish(c3d, "w2d", EXIT_PREDECESSOR_QUEUED, "PREDECESSOR QUEUED — the branch carries the stamped DEVLOG entries of "
           "the open pull request(s) #701 beside its own", end_head="p1")
    f3d.live.clear()
    pred_waiting = f3d.tick(c3d)
    pred_said = " | ".join(f3d.said)
    pred_open = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
    github["fail"] = True
    pred_unread = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
    github["fail"] = False
    github["open"] = [{"number": 702, "headRefName": "w2d", "headRefOid": predecessor_oid}]   # the branch's own pull request
    pred_own_only = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
    github["open"] = []                                       # #701 merged into main (or closed): nothing is open
    pred_relaunched = f3d.tick(c3d)
    expect("exit 15 (a merged predecessor's pull request is still open): a retry-when-free class, never the enqueue "
           "unit's; the lane waits at that head while enqueue.py's reader finds the pull request open (naming it), "
           "an unreadable GitHub is not free, the branch's own pull request is none; it relaunches the SAME head "
           "once none is open, with no rearm (kb/Work PB3145)",
           classify({"rc": EXIT_PREDECESSOR_QUEUED}) == FREE and pred_waiting == []
           and "still waits on" in pred_said and pred_open == (False, "open: #701")
           and pred_unread[0] is False and "rate limited" in pred_unread[1] and pred_own_only == (True, "")
           and pred_relaunched == ["w2d"] and agent_work(c3d).reasons == []
           and not (lane_dir(c3d) / REARM).exists(), f"{pred_waiting} {pred_said} {pred_open} {pred_unread} "
                                                       f"{pred_own_only} {pred_relaunched}")


def case_018_exit_9_is_retried_once_and_a_second_exit_2_at_th(expect):

    # 4. exit 2 / 9 retried ONCE at the same head; the second is STOPPED (the handoff's).
    c4 = fresh("once")
    f4 = Fake([row("w3a", "WAITING TO LAND", "k1")])
    f4.tick(c4)
    finish(c4, "w3a", 9, "NOT QUEUED (exit 9, above)")
    f4.live.clear()
    retried = f4.tick(c4)
    finish(c4, "w3a", 2, "NOT QUEUED — main moved under every one of 3 catch-up merges; re-run")
    f4.live.clear()
    expect("exit 9 is retried once, and a second exit 2 at that head is not", retried == ["w3a"] and f4.tick(c4) == [],
           str(f4.started))
    expect("the repeated exit is STOPPED: the enqueue unit's", any(
        r.startswith("stopped landings: w3a") for r in agent_work(c4).reasons), str(agent_work(c4).reasons))


def case_019_a_launch_and_its_retry_in_one_stopped_clock_tick(expect):

    # 4b. a branch's attempts are ordered by their NUMBERS, never the clock (kb/Work PB3536): with the clock STOPPED,
    #     a launch and its retry in one clock tick are two records, in launch order. A stamp-named id gave both ONE
    #     file name, and the retry's record overwrote the first.
    c4b = fresh("one-tick")
    stopped_at = dt.datetime(2026, 10, 23, 2, 21, 9, 187360, dt.timezone.utc)
    real_now = ll.now_utc
    ll.now_utc = lambda: stopped_at
    try:
        f4b = Fake([row("w3b", "WAITING TO LAND", "m1")])
        first4b = f4b.tick(c4b)
        finish(c4b, "w3b", 9, "NOT QUEUED (exit 9, above)")
        f4b.live.clear()
        again4b = f4b.tick(c4b)
    finally:
        ll.now_utc = real_now
    (lane_dir(c4b) / ATTEMPTS / "20261022T225440187360Z-w3b.json").write_text(
        json.dumps({"id": "20261022T225440187360Z-w3b", "branch": "w3b", "head": "m0", "rc": 2}),
        encoding="utf-8", newline="\n")   # an attempt named before the numbers
    atts4b = [(a["id"], a.get("rc")) for a in attempts(c4b)]
    expect("a launch and its retry in one stopped clock tick are two attempts, ordered by their numbers; one named "
           "before the numbers is the oldest",
           first4b == ["w3b"] and again4b == ["w3b"] and [i for i, _ in atts4b] == [
               "20261022T225440187360Z-w3b", "000001-20261023T022109Z-w3b", "000002-20261023T022109Z-w3b"]
           and [rc for _, rc in atts4b] == [2, 9, None], str(atts4b))


def case_020_a_runner_gone_with_no_exit_code_is_reaped_died_a(expect):

    # 5. a runner that died with no rc is DIED, then retried once.
    c5 = fresh("died")
    f5 = Fake([row("w4a", "WAITING TO LAND", "m1")])
    f5.tick(c5)
    f5.live.clear()
    expect("a runner gone with no exit code is reaped DIED and retried once at its head",
           f5.tick(c5) == ["w4a"] and any(a.get("outcome") == "DIED" for a in attempts(c5)),
           f"{f5.started} {[a.get('outcome') for a in attempts(c5)]}")


def case_021_exit_8_stops_all_attempts_none_starts_the_other(expect):
    global c6, f6

    # 6. exit 8 halts every attempt and names the operator; clear-halt re-arms the lane.
    c6 = fresh("halt")
    f6 = Fake([row("w5a", "WAITING TO LAND", "q1")])
    f6.tick(c6)
    finish(c6, "w5a", 8, "QUEUE NOT ACTIVE (ruleset: DRIFT — x)")
    f6.live.clear()
    pathlib.Path(attempts(c6)[0]["log"]).parent.mkdir(parents=True, exist_ok=True)
    pathlib.Path(attempts(c6)[0]["log"]).write_text("", encoding="utf-8", newline="\n")
    f6.rows.append(row("w5b", "WAITING TO LAND", "r1"))
    expect("exit 8 stops all attempts: none starts, the other ready branch included", f6.tick(c6) == [] and
           (lane_dir(c6) / HALT).exists(), f"{f6.started} {f6.said}")


def case_022_exit_8_is_named_to_the_operator_one_mailbox_mess(expect):
    import mailbox  # noqa: PLC0415  (this directory's: the sessions' mailbox)
    inbox_files = list(mailbox.inbox(c6, "operator").glob("*.json"))
    expect("exit 8 is named to the operator (one mailbox message)", len(inbox_files) == 1,
           f"{inbox_files} {f6.said}")
    expect("a later tick while halted still starts nothing", f6.tick(c6) == [], str(f6.started))


def case_023_after_clear_halt_both_branches_start_the_halted(expect):
    (lane_dir(c6) / HALT).unlink()
    expect("after clear-halt both branches start, the halted one again at its head",
           sorted(f6.tick(c6)) == ["w5a", "w5b"], str(f6.started))


def case_024_stop_unit_alone_the_soft_cap_the_waiting_to_land(expect):

    # 7. The loop's STOP, the hard cap's pair (STOP-UNIT with the loop's fleet stop) and the global stop: nothing
    #    starts, yet the tick still reaps and surveys.
    for name, paths in (("STOP", [lambda c: c / "STOP"]),
                        ("the hard cap's STOP-UNIT and fleet stop",
                         [lambda c: coord.stop_unit(str(c)), lambda c: coord.fleet_stop(c / "scratch", "loop")]),
                        ("global STOP", [lambda c: coord.global_stop(str(c))])):
        cs = fresh(f"stop-{name.split()[0]}-{len(paths)}")
        fs = Fake([row("w6a", "WAITING TO LAND", "s1")])
        for path in paths:
            path(cs).parent.mkdir(parents=True, exist_ok=True)
            path(cs).write_text("", encoding="utf-8", newline="\n")
        expect(f"{name}: no attempt starts, and the survey is still written",
               fs.tick(cs) == [] and (lane_dir(cs) / SURVEY).exists(), str(fs.said))
    # 7b. STOP-UNIT alone is the SOFT context cap's signal to the running unit (kb/Work PB3402): the lane lands on.
    cu = fresh("stop-unit-soft")
    fu = Fake([row("w6b", "WAITING TO LAND", "s2")])
    coord.stop_unit(str(cu)).write_text("", encoding="utf-8", newline="\n")
    expect("STOP-UNIT alone (the soft cap): the WAITING TO LAND branch starts", fu.tick(cu) == ["w6b"], str(fu.said))


def case_025_enqueued_at_this_head_never_relaunched(expect):
    global c8

    # 8. ENQUEUED: nothing more at that head; its leads and record are the enqueue unit's until collected.
    c8 = fresh("enqueued")
    f8 = Fake([row("w7a", "WAITING TO LAND", "t1")])
    f8.tick(c8)
    finish(c8, "w7a", 0, "ENQUEUED", end_head="t2")
    f8.live.clear()
    f8.rows[0] = row("w7a", "WAITING TO LAND", "t2")
    expect("ENQUEUED at this head: never relaunched", f8.tick(c8) == [], str(f8.started))
    expect("an ENQUEUED attempt is agent work (leads, the landing record) until collected",
           agent_work(c8).reasons == ["landed by the lane, leads and records to complete: w7a"],
           str(agent_work(c8).reasons))


def case_026_collected_no_agent_work_is_left(expect):
    collect(c8, ["w7a"], say=lambda s: None)
    expect("collected: no agent work is left", agent_work(c8).reasons == [], str(agent_work(c8).reasons))


def case_027_a_survey_older_than_survey_max_age_min_is_report(expect):
    old = coord.read_json(lane_dir(c8) / SURVEY, {})
    old["at"] = (now_utc() - dt.timedelta(minutes=SURVEY_MAX_AGE_MIN + 5)).isoformat()
    coord.write_json(lane_dir(c8) / SURVEY, old)
    expect("a survey older than SURVEY_MAX_AGE_MIN is reported stale (the lane is not ticking)",
           agent_work(c8).survey_age_min > SURVEY_MAX_AGE_MIN and "STALE" in status_lines(c8)[0], status_lines(c8)[0])


def case_028_an_attempt_file_that_is_not_a_record_is_said_by(expect):

    # 9. one tick at a time: a held tick lock makes a second tick a no-op.
    c9 = fresh("lock")
    f9 = Fake([row("w8a", "WAITING TO LAND", "u1")])
    held = ExclusiveLock.try_take(lane_dir(c9) / TICK_LOCK, "another tick")
    try:
        expect("a second tick while one holds the lock does nothing", f9.tick(c9) == [] and
               "already running" in " ".join(f9.said), str(f9.said))
    finally:
        held.release()
    (lane_dir(c9) / ATTEMPTS).mkdir(parents=True, exist_ok=True)
    (lane_dir(c9) / ATTEMPTS / "torn.json").write_text("{not json", encoding="utf-8", newline="\n")
    f9.tick(c9)
    expect("an attempt file that is not a record is said by the tick and by status, never dropped in silence",
           any("torn.json" in s and "not an attempt record" in s for s in f9.said)
           and any(ln.startswith("UNREADABLE") and "torn.json" in ln for ln in status_lines(c9)),
           f"{f9.said} {status_lines(c9)}")


def case_029_conflict_brief_a_queued_branch_whose_head_confli(expect):
    # ... and a QUEUED pull request whose head conflicts with an entry queued ahead of it (the survey's `foreseen`,
    #     kb/Work PB3723): origin/main merges clean, the entry ahead does not, so the brief is against that entry's
    #     head, saying it is queued ahead and how its head is brought in
    qbr = ScratchRepo.init(root / "wt-queue")
    qbr.commit("base", {"scripts/q.py": "q = 0\n"})
    qbr.git("update-ref", MAIN, "HEAD")
    qbr.commit("the entry queued ahead", {"scripts/q.py": "q = 1\n"})
    ahead_head = qbr.git("rev-parse", "HEAD")
    qbr.git("reset", "-q", "--hard", MAIN)
    qbr.commit("the queued branch", {"scripts/q.py": "q = 2\n"})
    c17c = fresh("foreseen")
    f17c = Fake([{"name": "wq", "state": "BOUNCED", "tip": qbr.git("rev-parse", "HEAD"), "worktree": str(qbr.path),
                  "why": "", "lead": None, "conflicts": True, "foreseen": [{"branch": "wahead", "head": ahead_head}]}])
    f17c.tick(c17c)
    foreseen_out: list[str] = []
    expect("conflict brief: a queued branch whose head conflicts with an entry ahead of it is briefed against THAT "
           "head, not origin/main (which merges clean): the file, the entry's name, and the merge command for its head",
           show_conflict_brief(c17c, "wq", lambda *a, cwd=None: qbr.run(*a, cwd=cwd), foreseen_out.append) == 0
           and "Bringing wahead" in "".join(foreseen_out) and "leaves 1 file(s) in conflict: scripts/q.py" in "".join(foreseen_out)
           and f"`git merge {ahead_head}`" in "".join(foreseen_out) and "--keep" not in "".join(foreseen_out)
           and "QUEUED ahead of this one" in "".join(foreseen_out) and "the entry queued ahead" in "".join(foreseen_out),
           "".join(foreseen_out))

    # 13. the runner's launch (no window, outliving the tick) is scripts/detached.py's, with its own self-test
    #     arms (kb/Work PB3117, PB3536): the tick starts every runner through it.
    expect("the tick starts every runner through the one detached launch (scripts/detached.py)",
           tick.__kwdefaults__["start"] is spawn_detached)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
