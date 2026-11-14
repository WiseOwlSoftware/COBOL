#!/usr/bin/env python3
"""Cases of land_lane.py: THE QUEUE WATCH: each pull request the lane queued, one writer per field, a hung GitHub call, the class of a merge-queue red (ported from the self-test of scripts/orchestrator/land_lane.py, DESIGN-orchestrator-loop.md
section 16.2, kb/Work PB5434). The cases run in name order over ONE world (`fixtures.world()`, bound by case_000), as the
sequential self-test did: a case reads what an earlier one planted (the module globals below).
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_queue_watch.py"""
from __future__ import annotations

import sys
import selftest_cases
from standard_streams import ensure_utf8

import fixtures
from enqueue import EXIT_NOT_JUMPED
from scratch_repo import ScratchRepo, remove_tree
from typing import Any
import dataclasses
import datetime as dt
import json
import pathlib
from fixtures import Fake, finish, fresh, row

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
ALERT = ll.ALERT
ALERTED = ll.ALERTED
DONE = ll.DONE
HOLD_BOUNCE = ll.HOLD_BOUNCE
IN_QUEUE = ll.IN_QUEUE
LEFT = ll.LEFT
MAIN = ll.MAIN
MERGED = ll.MERGED
NOT_QUEUED = ll.NOT_QUEUED
REQUEUE = ll.REQUEUE
REQUEUE_BOUNCE = ll.REQUEUE_BOUNCE
RERUN = ll.RERUN
RedFacts = ll.RedFacts
STUCK = ll.STUCK
WATCHING = ll.WATCHING
WATCH_DONE = ll.WATCH_DONE
WatchEffects = ll.WatchEffects
agent_work = ll.agent_work
attempts = ll.attempts
bounce_step = ll.bounce_step
cause_note = ll.cause_note
classify = ll.classify
classify_red = ll.classify_red
collect = ll.collect
hold_alarms = ll.hold_alarms
lane_dir = ll.lane_dir
name_hold = ll.name_hold
now_utc = ll.now_utc
open_causes_now = ll.open_causes_now
reap = ll.reap
red_facts = ll.red_facts
release_hold = ll.release_hold
status_lines = ll.status_lines
update = ll.update
watch_step = ll.watch_step
watch_text = ll.watch_text

# the world's state: bound by the cases in order, read by the later ones
acts = answered = bounce_dir = c14 = c15 = enq = f14 = f15 = f19 = facts = fetch = fetched = gate = green_gate = lm = n18 = n_of = plant_bounce = planted = pr = pr18 = pr19 = queue_facts = quiet = rec_ = red_gate = reopen_q = rerun_q = root = ruleset = running_gate = stuck_gate = t = tp = watch = None


def case_000_queue_watch_under_a_stop_it_reads_but_acts_on_no(expect):
    global acts, c14, f14, facts, green_gate, n_of, pr, queue_facts, red_gate, root, running_gate, stuck_gate, t, watch
    root = fixtures.world()

    # 14. THE QUEUE WATCH (kb/Work PB3527): each pull request the lane queued, followed into the merge queue from a
    #     stubbed reader of GitHub's facts, on a planted clock; PR #385's shape is a ci-gate check-run in progress
    #     whose job completed at 18:46:46Z.
    import queue_facts  # noqa: PLC0415
    t = lambda hm: dt.datetime.fromisoformat(f"2026-10-22T{hm}:00+00:00")   # noqa: E731
    job_done = "2026-10-22T18:46:46Z"
    stuck_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552758, 39834070835,
                                      job_status="completed", job_completed_at=job_done)
    running_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552759, 39834070835,
                                        job_status="in_progress")
    green_gate = queue_facts.CheckRun("ci-gate", "COMPLETED", "SUCCESS", 120043423624, 39834070835, job_done)
    red_gate = queue_facts.CheckRun("ci-gate", "COMPLETED", "FAILURE", 120043423625, 39834070836, job_done)

    def pr(number: int, *checks, in_queue: bool = False, head: str = "z1", others: tuple = ()) -> Any:
        return queue_facts.PullRequest(number, f"https://github.com/o/r/pull/{number}", head, in_queue,
                                       "2026-10-22T18:44:50Z", "BLOCKED", "", tuple(checks), "o/r", others=others)

    c14 = fresh("watch")
    names = ["q", "m", "rerun", "reopen", "alert", "red", "closed", "held", "drop", "qm"]
    f14 = Fake([row(f"w14{n}", "WAITING TO LAND", "z0") for n in names])
    f14.tick(c14)
    for n in names:
        finish(c14, f"w14{n}", 0, "ENQUEUED", end_head="z1")
    f14.live.clear()
    f14.rows = [row(f"w14{n}", "IN FLIGHT", "z1") for n in names]
    f14.rows[1]["content"] = "MERGED"                      # merged by the queue, the survey still calls it fresh
    n_of = {f"w14{n}": 380 + i for i, n in enumerate(names)}

    def facts(**over) -> dict[str, Any]:
        base = {b: pr(n_of[b], stuck_gate) for b in n_of
                if b not in ("w14q", "w14m", "w14red", "w14closed", "w14drop", "w14qm")}
        base.update(w14q=pr(n_of["w14q"], green_gate, in_queue=True), w14red=pr(n_of["w14red"], red_gate),
                    w14drop=pr(n_of["w14drop"], green_gate, in_queue=True),
                    w14qm=pr(n_of["w14qm"], green_gate, in_queue=True))
        base.update(over)
        return {k: v for k, v in base.items() if v is not None}

    def watch(b: str) -> dict[str, Any]:
        return next(a for a in attempts(c14) if a["branch"] == b).get("watch") or {}

    def acts(b: str) -> list[str]:
        """What the watch did for branch b, in order: its recorded stalls (each act is measured), and an alert
        on a pull request whose number is unknown (closed), which has no record to write."""
        return [e[5] if e[0] == "measure" else e[0] for e in f14.effects
                if (e[0] == "measure" and e[2] == b) or (e[0] == "alert" and f"({b})" in e[1] and "#?" in e[1])]

    (lane_dir(c14).parent / "STOP").write_text("", encoding="utf-8", newline="\n")
    f14.prs, f14.now = facts(w14held=pr(n_of["w14held"], stuck_gate)), t("18:57")
    f14.tick(c14)
    held_said = list(f14.said)
    (lane_dir(c14).parent / "STOP").unlink()
    expect("queue watch: under a stop it reads but acts on nothing (no re-run, no requeue, no alert)",
           not f14.effects and any("NOT acted on" in s for s in held_said), f"{f14.effects} {held_said}")


def case_001_queue_watch_github_s_facts_unobserved_this_tick(expect):
    f14.prs, f14.now = None, t("18:58")
    f14.tick(c14)
    expect("queue watch: GitHub's facts unobserved this tick: nothing is read and nothing is done",
           not f14.effects and any("not observed" in s for s in f14.said), str(f14.said))


def case_002_queue_watch_a_pull_request_in_the_queue_reads_qu(expect):

    f14.prs, f14.now = facts(w14closed=None), t("18:50")
    f14.tick(c14)
    expect("queue watch: a pull request in the queue reads QUEUED and the watch does NOT end (kb/Work PB3772: it "
           "can leave the queue unmerged); one whose branch's work is on main ends it MERGED; a required check that "
           "failed ends it LEFT (BOUNCED: the finisher's); none acted on",
           watch("w14q").get("state") == IN_QUEUE and IN_QUEUE not in WATCH_DONE
           and watch("w14q").get("queued_at") == "2026-10-22T18:50:00+00:00"
           and watch("w14m").get("state") == MERGED
           and watch("w14red").get("state") == LEFT and not acts("w14q") and not acts("w14m") and not acts("w14red"),
           f"{watch('w14q')} {watch('w14m')} {watch('w14red')} {f14.effects}")
    expect("queue watch: a stuck check-run inside the grace is only watched; a pull request with no open pull "
           "request and its work not on main ALERTS at once (closed by hand)",
           watch("w14rerun").get("state") == WATCHING and not watch("w14rerun").get("reads")
           and watch("w14closed").get("state") == ALERTED
           and acts("w14closed") == ["alert"], f"{watch('w14rerun')} {watch('w14closed')} {f14.effects}")


def case_003_queue_watch_past_the_grace_read_1_of_a_stuck_che(expect):
    f14.prs, f14.now = facts(w14closed=None, w14rerun=pr(n_of["w14rerun"], running_gate)), t("18:57")
    f14.tick(c14)
    expect("queue watch: past the grace, read 1 of a stuck check-run re-runs its workflow run and records the "
           "stall (cause stuck-check-run, read 1); a check whose job still runs is not stalled",
           ("rerun", "o/r", 39834070835) in f14.effects and acts("w14reopen") == [RERUN]
           and ("measure", n_of["w14reopen"], "w14reopen", 1, STUCK, RERUN) in f14.effects
           and not acts("w14rerun") and watch("w14reopen")["reads"][0]["act"] == RERUN, str(f14.effects))


def case_004_queue_watch_stuck_then_cleared_by_the_re_run_the(expect):
    f14.prs, f14.now = facts(w14closed=None, w14rerun=pr(n_of["w14rerun"], green_gate, in_queue=True)), t("19:05")
    f14.tick(c14)
    expect("queue watch: stuck, then cleared by the re-run: the pull request enters the queue (QUEUED, still "
           "watched); a stuck one only 8 min after its re-run is still inside the grace",
           watch("w14rerun").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 1,
           f"{watch('w14rerun')} {watch('w14reopen')}")


def case_005_queue_watch_read_2_still_stuck_one_grace_after_t(expect):
    global reopen_q, rerun_q
    rerun_q = pr(n_of["w14rerun"], green_gate, in_queue=True)
    reopen_q = pr(n_of["w14reopen"], green_gate, in_queue=True)
    f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q), t("19:08")
    f14.tick(c14)
    expect("queue watch: read 2 (still stuck one grace after the re-run) closes, reopens and re-queues the pull "
           "request, recorded as read 2",
           ("requeue", "o/r", n_of["w14reopen"]) in f14.effects and acts("w14reopen") == [RERUN, REQUEUE]
           and ("measure", n_of["w14reopen"], "w14reopen", 2, STUCK, REQUEUE) in f14.effects, str(f14.effects))


def case_006_queue_watch_stuck_then_cleared_by_the_reopen_in(expect):
    f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q), t("19:12")
    f14.tick(c14)
    expect("queue watch: stuck, then cleared by the reopen: in the queue, no third read",
           watch("w14reopen").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 2,
           str(watch("w14reopen")))


def case_007_queue_watch_a_third_stalled_read_alerts_the_oper(expect):
    f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q), t("19:19")
    f14.tick(c14)
    f14.now = t("19:45")
    f14.tick(c14)
    alerts = [e for e in f14.effects if e[0] == "alert" and "(w14alert)" in e[1]]
    expect("queue watch: a third stalled read ALERTS the operator (one mailbox question naming the pull request and "
           "the remedies tried) and the watch acts no more, however long it stays out",
           acts("w14alert") == [RERUN, REQUEUE, ALERT] and len(alerts) == 1
           and f"PR #{n_of['w14alert']}" in alerts[0][1] and "re-run" in alerts[0][2]
           and watch("w14alert").get("state") == ALERTED and len(watch("w14alert")["reads"]) == 3,
           f"{acts('w14alert')} {watch('w14alert')}")
    expect("queue watch: status and the survey's NOT QUEUED row say the watch's state, when it read, that an "
           "ended watch reads nothing again, and each read",
           any("w14alert" in ln and "queue watch ALERTED" in ln and "read 1 stuck-check-run: rerun done" in ln
               and "the watch has ENDED" in ln and "read at 2026-10-22T19:19:00+00:00" in ln
               for ln in status_lines(c14))
           and "has ENDED" not in watch_text(next(a for a in attempts(c14) if a["branch"] == "w14q"))
           and "read 2 stuck-check-run: requeue done" in watch_text(watch_holder := next(
               a for a in attempts(c14) if a["branch"] == "w14alert")), str(watch_holder))


def case_008_queue_watch_a_pull_request_queued_and_then_out_o(expect):
    # kb/Work PB3772: PR #593's shape. QUEUED at 19:45 (the last reading in the queue), then out of it with auto-merge
    # OFF, its ci-gate green and no failed run: the queue dropped it (a ruleset toggle, a dequeue). The watch that
    # ended at QUEUED never saw it; this one waits the grace from the last reading in the queue, never re-runs a
    # check whose work passed, requeues, and reads it in the queue again. Its sibling qm is merged by the queue.
    dropped = queue_facts.PullRequest(n_of["w14drop"], "https://github.com/o/r/pull/389", "z1", False, "", "CLEAN",
                                      "", (green_gate,), "o/r")
    f14.rows[9]["content"] = "MERGED"
    f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q, w14drop=dropped, w14qm=None), t("19:50")
    f14.tick(c14)
    inside = (watch("w14drop").get("state"), acts("w14drop"), watch("w14qm").get("state"))
    f14.now = t("19:56")
    f14.tick(c14)
    expect("queue watch: a pull request QUEUED and then out of the queue with auto-merge off and no failed run is "
           "WATCHING inside the grace counted from its last reading in the queue, and past it read 1 is the "
           "REQUEUE (no re-run of a check that passed), cause not-queued, saying it left the queue; one the queue "
           "MERGED ends the watch MERGED (kb/Work PB3772)",
           inside == (WATCHING, [], MERGED) and acts("w14drop") == [REQUEUE]
           and ("requeue", "o/r", n_of["w14drop"]) in f14.effects
           and ("measure", n_of["w14drop"], "w14drop", 1, NOT_QUEUED, REQUEUE) in f14.effects
           and "left the merge queue" in watch("w14drop")["why"], f"{inside} {acts('w14drop')} {watch('w14drop')}")


def case_009_queue_watch_the_requeue_put_it_back_in_the_queue(expect):
    f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q,
                             w14drop=pr(n_of["w14drop"], green_gate, in_queue=True), w14qm=None), t("19:57")
    f14.tick(c14)
    expect("queue watch: the requeue put it back in the queue: QUEUED again, and watched on",
           watch("w14drop").get("state") == IN_QUEUE and watch("w14drop").get("queued_at") == "2026-10-22T19:57:00+00:00"
           and len(watch("w14drop")["reads"]) == 1, str(watch("w14drop")))


def case_010_queue_watch_a_check_run_whose_job_the_jobs_api_c(expect):
    global enq
    unread_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552760, 39834070835)
    enq = {"end_head": "z1", "ended": "2026-10-22T18:45:00Z"}
    early, late = (watch_step(enq, "IN FLIGHT", pr(397, unread_gate), t(hm)) for hm in ("18:50", "18:56"))
    expect("queue watch: a check-run whose job the jobs API could not read never holds the watch for ever: its "
           "clock is the enqueue, and past the grace it is a stalled read (not-queued) that names the unread job",
           early.state == WATCHING and not early.act and late.act == RERUN and late.cause == NOT_QUEUED
           and "could not be read" in late.why, f"{early} {late}")


def case_011_queue_watch_with_no_check_run_at_all_the_clock_s(expect):
    noq = watch_step({"end_head": "z1", "ended": "2026-10-22T18:45:00Z"}, "IN FLIGHT", pr(399), t("18:56"))
    expect("queue watch: with no check-run at all the clock starts at the enqueue, and read 1 goes straight to the "
           "requeue (there is no run to re-run); a new head on the pull request ends the watch LEFT",
           noq.act == REQUEUE and noq.cause == NOT_QUEUED
           and watch_step({"end_head": "z1"}, "IN FLIGHT", pr(398, head="z9"), t("19:00")).state == LEFT, str(noq))


def case_012_queue_watch_a_required_check_with_no_check_run_y(expect):
    audits = lambda status, done="", job="": queue_facts.CheckRun(   # noqa: E731
        "audits", status, "SUCCESS" if done else "", 120100000001, 40969292151, done, job_status=job)
    long_audits = watch_step(enq, "IN FLIGHT", pr(777, others=(audits("IN_PROGRESS", job="in_progress"),)), t("19:10"))
    audits_ended = [watch_step(enq, "IN FLIGHT", pr(777, others=(audits("COMPLETED", "2026-10-22T19:05:00Z"),)), t(hm))
                    for hm in ("19:10", "19:16")]
    expect("queue watch: a required check with no check-run yet because a job it needs still runs (PR #777's audits, "
           "25 min after the enqueue) is WATCHING with no remedy, never the requeue that cancels the run (kb/Work "
           "PB4052); once that work ends the grace counts from ITS end, and read 1 re-runs its workflow run",
           long_audits.state == WATCHING and not long_audits.act and "audits still running" in long_audits.why
           and audits_ended[0].state == WATCHING and not audits_ended[0].act
           and audits_ended[1].act == RERUN and audits_ended[1].run == 40969292151, f"{long_audits} {audits_ended}")


def case_013_queue_watch_a_pull_request_that_conflicts_with_m(expect):
    dirty = watch_step(enq, "IN FLIGHT", dataclasses.replace(pr(396, green_gate), merge_state="DIRTY"), t("19:30"))
    expect("queue watch: a pull request that conflicts with main (merge state DIRTY), its checks green, ends the "
           "watch LEFT (BOUNCED, the finisher's) with no remedy: a re-run or a reopen cannot clear a conflict "
           "(kb/Work PB3527 review R1-5)",
           dirty.state == LEFT and not dirty.act and "DIRTY" in dirty.why, str(dirty))


def case_014_one_writer_per_field_a_collect_between_the_tick(expect):
    global c15, f15, planted

    # 12b. ONE WRITER PER FIELD (kb/Work PB3527 review R1-3): the tick reads the attempts before its slow survey (git
    #      fetch, the GraphQL read, the jobs API); the enqueue unit's `collect` that lands in between is kept, and the
    #      watch's own field is written beside it. And a runner that writes its rc after reap read the record is
    #      never marked DIED.
    c15 = fresh("race")
    f15 = Fake([row("w15", "WAITING TO LAND", "y0")])
    f15.tick(c15)
    finish(c15, "w15", 0, "ENQUEUED", end_head="y1")
    f15.live.clear()
    f15.rows = [row("w15", "IN FLIGHT", "y1")]
    f15.prs, f15.now = {"w15": pr(415, running_gate, head="y1")}, t("18:50")
    planted = f15.survey
    f15.survey = lambda repo, cdir: (collect(cdir, ["w15"], say=lambda s: None), planted(repo, cdir))[1]
    f15.tick(c15)
    raced = next(a for a in attempts(c15) if a["branch"] == "w15")
    expect("one writer per field: a `collect` between the tick's read and its queue watch's write is kept (collected "
           "stays true, nothing asks the enqueue unit again), and the watch's state is written beside it",
           raced.get("collected") is True and (raced.get("watch") or {}).get("state") == WATCHING
           and not agent_work(c15, t("18:51")).reasons, f"{raced} {agent_work(c15, t('18:51')).reasons}")


def case_015_one_writer_per_field_a_runner_that_wrote_its_rc(expect):
    f15.survey = planted
    f15.rows = [row("w15b", "WAITING TO LAND", "x0")]
    f15.prs = None
    f15.tick(c15)
    stale = next(a for a in attempts(c15) if a["branch"] == "w15b")
    late = next(a for a in attempts(c15) if a["branch"] == "w15b")
    update(late, {"rc": 0, "verdict": "ENQUEUED", "ended": now_utc().isoformat(), "end_head": "x1"})
    reap(c15, [stale], lambda a: False, say=lambda s: None)
    expect("one writer per field: a runner that wrote its rc after reap read the record is not marked DIED (the "
           "fresh record decides, under the record's lock)",
           "outcome" not in stale and stale.get("rc") == 0
           and "outcome" not in next(a for a in attempts(c15) if a["branch"] == "w15b"), str(stale))


def case_016_a_remedy_github_does_not_answer_gh_s_timeout_is(expect):
    global ruleset

    # 12c. A HUNG GITHUB CALL NEVER STOPS THE TICK (kb/Work PB3527 review R1-4): the real remedy (enqueue.rerun_check)
    #      on a gh that gives no answer (real_gh's timeout raises ruleset.Unobserved) is a FAILED read, recorded and
    #      measured; the tick still launches the branch WAITING TO LAND; the next stalled read escalates.
    import enqueue  # noqa: PLC0415
    import ruleset  # noqa: PLC0415

    def silent(args: list[str]) -> tuple[int, str, str]:
        raise ruleset.Unobserved(f"`gh {' '.join(args[:2])}` gave no answer in 120 s")
    c16 = fresh("hang")
    f16 = Fake([row("w16", "WAITING TO LAND", "v0")])
    f16.tick(c16)
    finish(c16, "w16", 0, "ENQUEUED", end_head="v1")
    f16.live.clear()
    f16.rows = [row("w16", "IN FLIGHT", "v1"), row("w16next", "WAITING TO LAND", "u0")]
    f16.prs, f16.now = {"w16": pr(416, stuck_gate, head="v1")}, t("18:57")
    recorded: list[tuple] = []
    f16.watch_effects = lambda: WatchEffects(
        rerun=lambda repo, run: enqueue.rerun_check(repo, run, gh=silent),
        requeue=lambda repo, number, head: enqueue.requeue(repo, number, head, gh=silent,
                                                           state=lambda: (ruleset.ACTIVE, "x")),
        alert=lambda subject, body, ref: recorded.append(("alert", subject)),
        measure=lambda number, b, read, cause, act, ok: recorded.append(("measure", number, read, act, ok)),
        red_facts=lambda att, pr_: RedFacts(unobserved="not asked: no bounce here"),
        measure_red=lambda number, b, run, cls, why, act, ok, cases, cause: recorded.append(("red", number, cls)),
        open_causes=lambda: {}, cause_note=lambda cases: "")
    try:
        launched = f16.tick(c16)
        hung = None
    except ruleset.Unobserved as exc:
        launched, hung = [], exc
    reads16 = (next(a for a in attempts(c16) if a["branch"] == "w16").get("watch") or {}).get("reads") or []
    f16.rows = [row("w16", "IN FLIGHT", "v1")]
    f16.now = t("19:08")
    f16.tick(c16)
    reads16b = (next(a for a in attempts(c16) if a["branch"] == "w16").get("watch") or {}).get("reads") or []
    expect("a remedy GitHub does not answer (gh's timeout) is a FAILED read, recorded and measured, and the tick "
           "still launches the branch WAITING TO LAND; the next stalled read escalates to the requeue, never "
           "repeats the re-run",
           hung is None and launched == ["w16next"] and [r["ok"] for r in reads16] == [False]
           and "GitHub gave no answer" in reads16[0]["detail"] and ("measure", 416, 1, RERUN, False) in recorded
           and [r["act"] for r in reads16b] == [RERUN, REQUEUE], f"{hung} {launched} {reads16b} {recorded}")


def case_017_classify_red_results_unread_or_no_record_is_uncl(expect):
    global lm, rec_, tp

    # 18. THE CLASS OF A MERGE-QUEUE RED (kb/Work PB4271, PB4331; the rule of CLAUDE.md Testing). First the one decision, pure: each
    #     class from planted facts. Then the facts read from a worktree (a planted gate record and a planted bounce's
    #     results, the fetch planted: no GitHub). Then the queue watch: an INFRA or RUNNER red re-queued once with no
    #     agent, a second red (or the same one again) left to the finisher, a MERGE red left at once, a failed
    #     re-queue never silent, and a re-queued branch no enqueue-unit work while its watch runs.
    import landing_measure as lm  # noqa: PLC0415
    import test_population as tp  # noqa: PLC0415
    rec_ = RedFacts(run=9001, conclusion="failure", record="000001-r", listed=frozenset({"A.Pass", "A.Skip"}),
                    passed=frozenset({"A.Pass"}))
    cls_of = {name: classify_red(f)[0] for name, f in {
        "unobserved": RedFacts(unobserved="GitHub unreachable"),
        "timed out": dataclasses.replace(rec_, conclusion="timed_out", failed=frozenset({"A.Pass"})),
        "no test failed": rec_,
        "no record": RedFacts(run=9001, conclusion="failure", failed=frozenset({"A.Pass"}), no_record="none covers"),
        "inert": RedFacts(run=9001, conclusion="failure", failed=frozenset({"B.New"}), record="000001-r", test_free="inert"),
        "tooling": RedFacts(run=9001, conclusion="failure", failed=frozenset({"B.New"}), record="000001-r",
                            test_free="tooling"),
        "skipped": dataclasses.replace(rec_, failed=frozenset({"A.Pass", "A.Skip"})),
        "absent": dataclasses.replace(rec_, failed=frozenset({"A.Pass", "B.New"})),
        "ran green": dataclasses.replace(rec_, failed=frozenset({"A.Pass"}))}.items()}
    expect("classify_red: results unread or no record is UNCLASSIFIED; a timed-out run or one where no test failed is "
           "INFRA; red cases that all ran GREEN in the record, or an inert or a tooling-lane record, are RUNNER; a red case the record "
           "listed but did not run is GENUINE; one the record lacks is MERGE (kb/Work PB4271, PB4331)",
           cls_of == {"unobserved": lm.UNCLASSIFIED, "timed out": lm.INFRA, "no test failed": lm.INFRA,
                      "no record": lm.UNCLASSIFIED, "inert": lm.RUNNER, "tooling": lm.RUNNER, "skipped": lm.GENUINE, "absent": lm.MERGE,
                      "ran green": lm.RUNNER}, str(cls_of))


def case_018_red_facts_results_of_an_older_bounce_are_never_t(expect):
    global answered, bounce_dir, fetch, fetched, gate, plant_bounce, pr18
    import gate  # noqa: PLC0415
    wt18 = root / "wt-w18"
    rec_dir = wt18 / gate.RUN_ROOT / "000001-20261031T000000Z"
    rec_dir.mkdir(parents=True)
    (rec_dir / "verdict.json").write_text(json.dumps({
        "schema": gate.RECORD_SCHEMA, "verdict": "GREEN", "line": "=== planted ===", "exit_code": 0, "head": "h18",
        "tree": "T", "head_tree": "T", "lane": "full"}), encoding="utf-8", newline="\n")
    tp.synthetic_trx(rec_dir / "Unit.trx", [("A.Pass", ["Passed"]), ("A.Skip", ["NotExecuted"])])
    bounce_dir = wt18 / gate.CI_BOUNCE_RESULTS

    def plant_bounce(number: int, cases: list[tuple[str, list[str]]], conclusion: str = "failure") -> None:
        remove_tree(bounce_dir)
        (bounce_dir / "test-results-unit").mkdir(parents=True)
        (bounce_dir / queue_facts.BOUNCE_SOURCE).write_text(
            f"GitHub Actions run 9001 (merge group of pull request #{number}, {conclusion}, x); planted\n",
            encoding="utf-8", newline="\n")
        tp.synthetic_trx(bounce_dir / "test-results-unit" / "u.trx", cases)
    att18 = {"branch": "w18", "worktree": str(wt18), "end_head": "h18"}
    pr18 = queue_facts.PullRequest(893, "u", "h18", False, "", "BLOCKED", "FAILED_CHECKS at 2026-10-31T10:00:00Z",
                                   (green_gate,), "o/r")
    fetched = []

    answered = [9001]   # the run the planted fetch says the directory holds now (0: its download failed)

    def fetch(b: str, d: pathlib.Path) -> queue_facts.BounceFetch:
        fetched.append(b)
        return queue_facts.BounceFetch("ci bounce: planted", answered[0])
    plant_bounce(893, [("A.Pass", ["Failed"]), ("C.Other", ["Passed"])])
    ran_green = red_facts(att18, pr18, fetch)
    plant_bounce(1, [("A.Pass", ["Failed"])])
    other_pr = red_facts(att18, pr18, fetch)
    plant_bounce(893, [("A.Skip", ["Failed"])])
    reused_dir = rec_dir.parent / "000002-20261031T010000Z"
    reused_dir.mkdir()
    (reused_dir / "verdict.json").write_text(json.dumps({
        **json.loads((rec_dir / "verdict.json").read_text(encoding="utf-8")), "reused_from": rec_dir.name}),
        encoding="utf-8", newline="\n")
    reused = red_facts(att18, pr18, fetch)
    (rec_dir / "Unit.trx").unlink()
    pruned = red_facts(att18, pr18, fetch)
    gone = red_facts({**att18, "worktree": str(root / "no-such-worktree")}, pr18, fetch)
    # An EARLIER bounce's results of the same pull request (run 9001, a reused pull request keeps its number) still
    # sit in the directory when the NEW red's results cannot be downloaded (the fetch answers run 0), or when the
    # fetch answers another run than the one the directory's marker names (kb/Work PB4271, review w20261808a r1).
    plant_bounce(893, [("A.Pass", ["Failed"])])
    answered[0] = 0
    stale = red_facts(att18, pr18, fetch)
    answered[0] = 9003
    other_run = red_facts(att18, pr18, fetch)
    answered[0] = 9001
    expect("red_facts: results of an OLDER bounce are never the new red's: a fetch that could not download the new run's "
           "results (it answers run 0) or answers a run the directory's marker does not name leaves the red "
           "UNCLASSIFIED, the finisher's, with the fetch's own line, never classified RUNNER and re-queued on another "
           "run's cases (kb/Work PB4271)",
           stale.unobserved == other_run.unobserved == "ci bounce: planted" and not stale.failed and not other_run.failed
           and classify_red(stale)[0] == classify_red(other_run)[0] == lm.UNCLASSIFIED,
           f"{stale} {other_run}")
    expect("red_facts: the bounce's red cases (by name) beside the gate record that covers the queued head, the run a "
           "REUSED record names read for its cases; results of another pull request, a record whose results are gone "
           "and a worktree that is gone are said, never guessed (kb/Work PB4271)",
           ran_green.run == 9001 and ran_green.failed == {"A.Pass"} and ran_green.record == rec_dir.name
           and classify_red(ran_green)[0] == lm.RUNNER
           and other_pr.unobserved == "ci bounce: planted" and classify_red(other_pr)[0] == lm.UNCLASSIFIED
           and reused.record == rec_dir.name and classify_red(reused)[0] == lm.GENUINE
           and not pruned.record and "keeps no test results" in pruned.no_record
           and classify_red(pruned)[0] == lm.UNCLASSIFIED and "gone" in gone.unobserved and fetched == ["w18"] * 6,
           f"{ran_green} {other_pr} {reused} {pruned} {gone} {fetched}")


def case_019_red_facts_a_composed_verdict_is_read_through_eve(expect):
    global bounce_dir

    # A COMPOSED verdict (kb/Work PB5099): its evidence is the run that retried the red cases AND the earlier run of the
    # same code state whose passes it counted, so a bounce's red case that only the earlier run listed is read there.
    wt18c = root / "wt-w18c"
    first_run = wt18c / gate.RUN_ROOT / "000001-20261031T000000Z"
    retry_run = first_run.parent / "000002-20261031T010000Z"
    first_run.mkdir(parents=True)
    retry_run.mkdir()
    (first_run / "verdict.json").write_text(json.dumps({
        "schema": gate.RECORD_SCHEMA, "verdict": "RED", "line": "=== planted ===", "exit_code": 1, "head": "h18",
        "tree": "T", "head_tree": "T", "lane": "full"}), encoding="utf-8", newline="\n")
    (retry_run / "verdict.json").write_text(json.dumps({
        "schema": gate.RECORD_SCHEMA, "verdict": "GREEN", "line": "=== planted ===", "exit_code": 0, "head": "h18",
        "tree": "T", "head_tree": "T", "tree_end": "T", "lane": "full", "composed_from": [first_run.name]}),
        encoding="utf-8", newline="\n")
    tp.synthetic_trx(first_run / "Unit.trx", [("A.Pass", ["Passed"]), ("A.Retried", ["Failed"])])
    tp.synthetic_trx(retry_run / "Unit.trx", [("A.Retried", ["Passed"])])
    att18c = {"branch": "w18c", "worktree": str(wt18c), "end_head": "h18"}
    bounce_dir = wt18c / gate.CI_BOUNCE_RESULTS
    plant_bounce(893, [("A.Pass", ["Failed"])])
    composed = red_facts(att18c, pr18, fetch)
    expect("red_facts: a composed verdict is read through every run that holds its evidence (gate.measuring_runs): a red "
           "case that ran GREEN only in the earlier run it composed with is RUNNER, not absent from the branch's "
           "population (kb/Work PB5099)",
           composed.record == retry_run.name and composed.listed == {"A.Pass", "A.Retried"}
           and classify_red(composed)[0] == lm.RUNNER, str(composed))


def case_020_queue_watch_an_infra_red_is_re_queued_once_by_th(expect):
    global n18

    c18 = fresh("reds")
    names18 = ["infra", "merge", "same", "fails"]
    f18 = Fake([row(f"w18{n}", "WAITING TO LAND", "k0") for n in names18])
    f18.tick(c18)
    for n in names18:
        finish(c18, f"w18{n}", 0, "ENQUEUED", end_head="k1")
    f18.live.clear()
    collect(c18, [f"w18{n}" for n in names18], say=lambda s: None)
    f18.rows = [row(f"w18{n}", "ENQUEUED" if n == "fails" else "BOUNCED", "k1") for n in names18]
    n18 = {f"w18{n}": 890 + i for i, n in enumerate(names18)}

    def bounced(b: str, removed: str) -> Any:
        return queue_facts.PullRequest(n18[b], "u", "k1", False, "", "BLOCKED", removed, (green_gate,), "o/r")
    infra = RedFacts(run=7001, conclusion="failure")                    # no test failed
    merge = dataclasses.replace(rec_, run=7002, failed=frozenset({"B.New"}))
    f18.reds = {"w18infra": infra, "w18merge": merge, "w18same": infra, "w18fails": infra}
    r1 = "FAILED_CHECKS at 2026-10-31T10:00:00Z"
    in_queue = queue_facts.PullRequest(n18["w18fails"], "u", "k1", True, "", "BLOCKED", "", (green_gate,), "o/r")
    f18.prs = {**{b: bounced(b, r1) for b in n18 if b != "w18fails"}, "w18fails": in_queue}
    f18.now = t("19:00")
    f18.tick(c18)
    first = {b: dict(next(a for a in attempts(c18) if a["branch"] == b).get("watch") or {}) for b in n18}
    work_after_requeue = agent_work(c18).reasons
    requeues = [e for e in f18.effects if e[0] == "requeue"]
    f18.prs = {"w18infra": bounced("w18infra", "FAILED_CHECKS at 2026-10-31T10:30:00Z"),   # a second red
               "w18same": bounced("w18same", r1), "w18fails": in_queue}                    # the same one again
    f18.reds["w18infra"] = dataclasses.replace(infra, run=7003)
    f18.now = t("19:30")
    f18.tick(c18)
    later = {b: next(a for a in attempts(c18) if a["branch"] == b).get("watch") or {} for b in ("w18infra", "w18same")}
    f18.watch_effects = lambda: dataclasses.replace(
        Fake.watch_effects(f18), requeue=lambda repo, number, head: (False, "a run on the head is in progress"))
    f18.prs = {"w18fails": bounced("w18fails", r1)}
    f18.rows[-1] = row("w18fails", "BOUNCED", "k1")
    f18.tick(c18)
    failed_watch = next(a for a in attempts(c18) if a["branch"] == "w18fails").get("watch") or {}
    reds18 = [e for e in f18.effects if e[0] == "red"]
    expect("queue watch: an INFRA red is re-queued ONCE by the lane (no agent: the branch is no enqueue-unit work while "
           "its watch runs), recorded with its class; a MERGE red ends the watch BOUNCED with its class, the "
           "finisher's; a second red of the re-queued landing, or the same bounce read again, is never re-queued; "
           "a re-queue GitHub refuses ends the watch BOUNCED, said (kb/Work PB4271, PB4331)",
           first["w18infra"].get("state") == WATCHING and "INFRA" in first["w18infra"].get("why", "")
           and first["w18merge"].get("state") == LEFT and "MERGE" in first["w18merge"].get("why", "")
           and len(requeues) == 2 and {e[2] for e in requeues} == {n18["w18infra"], n18["w18same"]}
           and work_after_requeue == ["bounced: w18merge"]
           and later["w18infra"].get("state") == LEFT and "second red" in later["w18infra"].get("why", "")
           and later["w18same"].get("state") == LEFT and "did not take" in later["w18same"].get("why", "")
           and failed_watch.get("state") == LEFT and "re-queue FAILED" in failed_watch.get("why", "")
           and len([e for e in f18.effects if e[0] == "requeue"]) == 2
           and ("red", n18["w18infra"], "w18infra", 7001, lm.INFRA, "requeue", True) in reds18
           and ("red", n18["w18infra"], "w18infra", 7003, lm.INFRA, "finisher", True) in reds18
           and ("red", n18["w18merge"], "w18merge", 7002, lm.MERGE, "finisher", True) in reds18
           and ("red", n18["w18fails"], "w18fails", 7001, lm.INFRA, "requeue", False) in reds18,
           f"{first} {later} {failed_watch} {work_after_requeue} {f18.effects}")


def case_021_queue_watch_a_runner_red_whose_red_case_was_alre(expect):
    global f19, pr19

    # ONE RE-RUN PER CAUSE (kb/Work PB5483): a RUNNER red whose red case another landing already re-queued, with no
    # fix landed, is HELD (never re-queued), recorded as hold-for-fix and the operator alerted; the same red with no
    # such history is re-queued; an INFRA red (no red case, no key) is re-queued once per landing as before.
    flaky = dataclasses.replace(rec_, run=7101, failed=frozenset({"A.Pass"}))
    pr19 = queue_facts.PullRequest(990, "u", "k1", False, "", "BLOCKED", "FAILED_CHECKS at 2026-11-13T02:00:00Z",
                                   (green_gate,), "o/r")
    pure_hold = bounce_step({}, pr19, lm.RUNNER, "w", flaky.failed, frozenset({"A.Pass"}))
    pure_fresh = bounce_step({}, pr19, lm.RUNNER, "w", flaky.failed, frozenset())
    pure_infra = bounce_step({}, pr19, lm.INFRA, "w", frozenset(), frozenset({"A.Pass"}))
    c19 = fresh("held")
    f19 = Fake([row("w19", "WAITING TO LAND", "k0")])
    f19.tick(c19)
    finish(c19, "w19", 0, "ENQUEUED", end_head="k1")
    f19.live.clear()
    collect(c19, ["w19"], say=lambda s: None)
    f19.rows = [row("w19", "BOUNCED", "k1")]
    f19.reds = {"w19": flaky}
    f19.held = {"A.Pass": {"held_at": None, "notes": [], "requeued": 1}}
    f19.prs = {"w19": queue_facts.PullRequest(990, "u", "k1", False, "", "BLOCKED",
                                              "FAILED_CHECKS at 2026-11-13T02:00:00Z", (green_gate,), "o/r")}
    f19.now = t("19:00")
    f19.tick(c19)
    w19 = next(a for a in attempts(c19) if a["branch"] == "w19").get("watch") or {}
    rcls = rec_ if lm.RUNNER == classify_red(flaky)[0] else None
    expect("queue watch: a RUNNER red whose red case was already re-queued once (on any landing) with no fix landed "
           "is HELD: never re-queued, recorded hold-for-fix, the watch BOUNCED with HELD on its line and the operator "
           "alerted to land the root fix with --fix-first; with no such history it is re-queued, and an INFRA red "
           "(no red case) is re-queued once per landing as before (kb/Work PB5483)",
           pure_hold.state == LEFT and pure_hold.act == HOLD_BOUNCE and "HELD" in pure_hold.why
           and pure_fresh.act == REQUEUE_BOUNCE and pure_infra.act == REQUEUE_BOUNCE and rcls is not None
           and w19.get("state") == LEFT and "HELD" in w19.get("why", "")
           and not [e for e in f19.effects if e[0] == "requeue"]
           and ("red", 990, "w19", 7101, lm.RUNNER, lm.HOLD_FOR_FIX, True) in f19.effects
           and any(e[0] == "alert" and "--fix-first" in e[2] for e in f19.effects),
           f"{pure_hold} {pure_fresh} {pure_infra} {w19} {f19.effects}")


def case_022_hold_releases_kb_work_pb5483_review_r1_1_r1_4_a(expect):
    global quiet
    hold_alert = next((e for e in f19.effects if e[0] == "alert" and "held" in e[1].lower()), ("", "", ""))
    # THE RELEASES (review R1-1): the PB5478 case. Its fix lands the ordinary way (no fix-first); the note the hold
    # names reads landed on origin/main, so the next read of the open causes releases it, recorded; the operator's
    # release refuses a cause that is not open; a hold older than HOLD_ALARM_H is alerted again, once per window.
    c20 = fresh("releases")
    lm.record_red(c20 / lm.FILE, 1607, "b1607", 43550577247, lm.RUNNER, "flake", "requeue", True,
                  "2026-11-13T01:00:00Z", cases=("A.Pass",))
    lm.record_red(c20 / lm.FILE, 1607, "b1607", 43553292493, lm.RUNNER, "flake", lm.HOLD_FOR_FIX, True,
                  "2026-11-13T02:00:00Z", cases=("A.Pass",), cause="PB5478")
    lm.record_red(c20 / lm.FILE, 1610, "b1610", 43560000001, lm.RUNNER, "other", "requeue", True,
                  "2026-11-13T02:30:00Z", cases=("B.Other",))
    lm.record(c20 / lm.FILE, {"pr": 1611, "branch": "fix-pb5478", "merged": "2026-11-13T03:00:00Z"})   # an ordinary fix
    quiet = []
    still = open_causes_now(c20, lambda note: False, quiet.append)
    released = open_causes_now(c20, lambda note: note == "PB5478", quiet.append)
    rels = lm.read_all(c20 / lm.RELEASES)
    typo = release_hold(c20, "A.Pas", "typo", quiet.append)
    by_hand = release_hold(c20, "B.Other", "fixed by #1612", quiet.append)
    after_hand = lm.open_causes(lm.read_all(c20 / lm.FILE), lm.read_all(c20 / lm.RELEASES))
    noon = dt.datetime(2026, 11, 13, 14, 0, tzinfo=dt.timezone.utc)
    aged = {"A.Pass": {"held_at": "2026-11-13T01:00:00Z"}, "C.Young": {"held_at": "2026-11-13T13:00:00Z"},
            "D.Requeued": {"held_at": None}}
    alarms = (hold_alarms(aged, {}, noon, lm.HOLD_ALARM_H), hold_alarms(aged, {"A.Pass": "2026-11-13T13:00:00Z"},
                                                                         noon, lm.HOLD_ALARM_H))
    expect("hold releases (kb/Work PB5483 review R1-1, R1-4): a hold names its note (PB5478) and its alert says so; a "
           "fix landed the ordinary way releases it once that note reads landed on origin/main, the release recorded; "
           "the operator's release-hold releases an open cause and refuses one that is not; a hold older than "
           "HOLD_ALARM_H is alerted again, once per window; exit 17 (a refused jump, the pull request queued at the "
           "back) is a DONE landing, watched, never STOPPED",
           "PB5478" in hold_alert[2] and set(still) == {"A.Pass", "B.Other"} and set(released) == {"B.Other"}
           and [(r["case"], r["by"]) for r in rels] == [("A.Pass", lm.NOTE_LANDED)]
           and typo == 2 and by_hand == 0 and after_hand == {}
           and alarms == (["A.Pass"], []) and classify({"rc": EXIT_NOT_JUMPED}) == DONE,
           f"{hold_alert} {still} {released} {rels} {typo} {by_hand} {after_hand} {alarms}")


def case_023_review_round_2_kb_work_pb5483_a_runner_red_of_mo(expect):
    # Review round 2. R2-1: a RUNNER red of more than FLAKE_CASES cases is no flake (never re-run, never held, the
    # finisher's), even when one of its cases is an open cause. R2-2: the hold's note is the ONE open note whose title or
    # Sites line names the test; a mere mention is none, two claimants are none. R2-3: a hold with no note gains one
    # filed later, on the next read, and that note's landing releases it; `name-hold` attaches one by hand.
    wide = frozenset(f"W.Case{i}" for i in range(lm.FLAKE_CASES + 1)) | {"A.Pass"}
    too_many = bounce_step({}, pr19, lm.RUNNER, "w", wide, frozenset({"A.Pass"}))
    small = bounce_step({}, pr19, lm.RUNNER, "w", frozenset({"A.Pass", "W.Case0"}), frozenset({"A.Pass"}))
    nrepo = ScratchRepo.init(root / "wt-notes")

    def nfile(nid: str, title: str, body: str, status: str = "open") -> str:
        return f"---\nid: {nid}\nstatus: {status}\ntitle: \"{nid} - {title}\"\n---\n\n{body}\n"
    nrepo.commit("notes", {
        "kb/Work/PB61.md": nfile("PB61", "the flake of CompilesRedefines", "**Sites:** tests/Unit/Foo.cs"),
        "kb/Work/PB62.md": nfile("PB62", "a doc note", "An example: CompilesRedefines shows the rule."),
        "kb/Work/PB63.md": nfile("PB63", "landed long ago", "**Sites:** Foo.ReadsSequentialFile", "landed")})
    nrepo.git("update-ref", MAIN, "HEAD")
    named_one = cause_note(nrepo.path, ("Unit.Foo.CompilesRedefines",))
    mention_only = cause_note(nrepo.path, ("Unit.Foo.ReadsSequentialFile",))
    nrepo.commit("a second claimant", {"kb/Work/PB64.md": nfile("PB64", "CompilesRedefines again", "x")})
    nrepo.git("update-ref", MAIN, "HEAD")
    ambiguous = cause_note(nrepo.path, ("Unit.Foo.CompilesRedefines",))
    c21 = fresh("late-note")
    lm.record_red(c21 / lm.FILE, 1, "b1", 21, lm.RUNNER, "flake", "requeue", True, "2026-11-13T01:00:00Z", cases=("Q.Case",))
    lm.record_red(c21 / lm.FILE, 2, "b2", 22, lm.RUNNER, "flake", lm.HOLD_FOR_FIX, True, "2026-11-13T02:00:00Z",
                  cases=("Q.Case",))                                           # held with no note
    filed: list[str] = []
    first_read = open_causes_now(c21, lambda n: False, quiet.append, lambda cases: "".join(filed))
    filed.append("PB5500")                                                     # the operator files the note
    second_read = open_causes_now(c21, lambda n: False, quiet.append, lambda cases: "".join(filed))
    landed_read = open_causes_now(c21, lambda n: n == "PB5500", quiet.append, lambda cases: "")
    c22 = fresh("name-hold")
    lm.record_red(c22 / lm.FILE, 1, "b1", 31, lm.RUNNER, "flake", "requeue", True, "2026-11-13T01:00:00Z", cases=("R.Case",))
    named_by_hand = (name_hold(c22, "R.Case", "PB77", quiet.append), name_hold(c22, "R.Cas", "PB77", quiet.append),
                     name_hold(c22, "R.Case", "nonsense", quiet.append))
    by_hand_released = open_causes_now(c22, lambda n: n == "PB77", quiet.append)
    expect("review round 2 (kb/Work PB5483): a RUNNER red of more than FLAKE_CASES cases is never re-run or held "
           "(the finisher's) while a small one meeting an open cause is held; the hold's note is the one open note "
           "whose title or Sites line names the test (a mention, a landed note or two claimants are none); a hold "
           "with no note gains one filed later and is released when it lands; name-hold attaches one by hand",
           too_many.state == LEFT and too_many.act == "" and "no flake" in too_many.why and small.act == HOLD_BOUNCE
           and named_one == "PB61" and mention_only == "" and ambiguous == ""
           and first_read["Q.Case"]["notes"] == [] and second_read["Q.Case"]["notes"] == ["PB5500"]
           and "Q.Case" not in landed_read
           and [(r["case"], r["by"]) for r in lm.read_all(c21 / lm.RELEASES)] == [("Q.Case", lm.NOTE_LANDED)]
           and named_by_hand == (0, 2, 2) and "R.Case" not in by_hand_released,
           f"{too_many} {small} {named_one!r} {mention_only!r} {ambiguous!r} {first_read} {second_read} "
           f"{landed_read} {named_by_hand} {by_hand_released}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
