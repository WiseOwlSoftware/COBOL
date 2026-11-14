#!/usr/bin/env python3
"""Cases of land_lane.py: a stop resolved without a new commit, a stranded branch, a merged branch's view (ported from the self-test of scripts/orchestrator/land_lane.py, DESIGN-orchestrator-loop.md
section 16.2, kb/Work PB5434). The cases run in name order over ONE world (`fixtures.world()`, bound by case_000), as the
sequential self-test did: a case reads what an earlier one planted (the module globals below).
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_stranded_branches.py"""
from __future__ import annotations

import sys
import selftest_cases
from standard_streams import ensure_utf8

import fixtures
import coord
from fixtures import Fake, finish, fresh, row

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
agent_work = ll.agent_work
attempts = ll.attempts
candidates = ll.candidates
collect = ll.collect
finished = ll.finished
rearm = ll.rearm
rearm_effect = ll.rearm_effect
rearm_path = ll.rearm_path
status_lines = ll.status_lines

# the world's state: bound by the cases in order, read by the later ones
arms = c12 = c12c = f12 = rec12 = root = said = tips = None


def case_000_a_collected_exit_5_at_an_unchanged_head_and_a_bo(expect):
    global c12, f12, root
    root = fixtures.world()

    # 12. a stop resolved WITHOUT a new commit (review R1-1): the enqueue unit's `rearm` lands it again at the same
    #     head, once; a BOUNCED branch is the enqueue unit's only until it is re-armed or being landed.
    c12 = fresh("rearm")
    f12 = Fake([row("w10a", "WAITING TO LAND", "v1"), row("w10b", "WAITING TO LAND", "x1")])
    f12.tick(c12)
    finish(c12, "w10a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on v1: a red already on main")
    finish(c12, "w10b", 0, "ENQUEUED", end_head="x2")
    f12.live.clear()
    f12.rows = [row("w10a", "DECISION", "v1"), row("w10b", "BOUNCED", "x2")]   # the merge group removed w10b
    collect(c12, ["w10a", "w10b"], say=lambda s: None)
    expect("a collected exit 5 at an unchanged head, and a BOUNCED branch, are not relaunched by themselves",
           f12.tick(c12) == [] and agent_work(c12).reasons == ["bounced: w10b"], f"{f12.started} {agent_work(c12).reasons}")


def case_001_rearm_records_a_finished_collected_exit_5_at_its(expect):
    global said, tips
    tips = {str(root / "wt-w10a"): "v1", str(root / "wt-w10b"): "x2"}
    said = []
    expect("rearm records a finished, collected exit 5 at its worktree's tip",
           rearm(c12, "w10a", "main carries the fix", say=said.append, tip_for=tips.get) == 0
           and coord.read_json(rearm_path(c12, "w10a"), {}).get("head") == "v1", str(said))
    expect("rearm records the BOUNCED branch at its tip, and it stops being the enqueue unit's work",
           rearm(c12, "w10b", "bounce: infrastructure (runner lost)", say=said.append, tip_for=tips.get) == 0
           and agent_work(c12).reasons == [] and any(ln.startswith("REARMED") and "w10b" in ln
                                                     for ln in status_lines(c12)),
           f"{said} {agent_work(c12).reasons}")
    expect("a re-armed branch is landed again at the SAME head, the BOUNCED one included",
           sorted(f12.tick(c12)) == ["w10a", "w10b"] and not rearm_path(c12, "w10a").exists()
           and not rearm_path(c12, "w10b").exists(), f"{f12.started} {f12.said}")


def case_002_rearm_is_refused_while_the_branch_s_attempt_is_r(expect):
    running = [a for a in attempts(c12) if not finished(a)]
    expect("rearm is refused while the branch's attempt is running (nothing written)",
           bool(running) and rearm(c12, "w10a", "x", say=said.append, tip_for=tips.get) == 1
           and not rearm_path(c12, "w10a").exists(), str(said))


def case_003_a_re_arm_buys_one_attempt_the_same_stop_at_the_s(expect):
    finish(c12, "w10a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on v1: main still red")
    f12.live.clear()
    expect("a re-arm buys ONE attempt: the same stop at the same head again is not relaunched",
           "w10a" not in f12.tick(c12), str(f12.started))


def case_004_a_re_arm_whose_branch_moved_is_stale_dropped_and(expect):
    rearm(c12, "w10a", "main carries the fix", say=said.append, tip_for=tips.get)
    f12.rows[0] = row("w10a", "WAITING FOR REVIEW", "v2")                   # a finisher committed after all
    out12 = f12.tick(c12)
    expect("a re-arm whose branch moved is stale: dropped and said (the new tip goes through review first)",
           "w10a" not in out12 and not rearm_path(c12, "w10a").exists()
           and any("stale" in s and "w10a" in s for s in f12.said), f"{out12} {f12.said}")


def case_005_an_unreviewed_exit_11_waits_then_a_reviewer_s_cl(expect):
    c12b = fresh("rearm-unreviewed")
    f12b = Fake([row("w11a", "WAITING TO LAND", "y1")])
    f12b.tick(c12b)
    finish(c12b, "w11a", 11, "UNREVIEWED (the tip is not covered by its CLEAN stamp)")
    f12b.live.clear()
    stopped_first = f12b.tick(c12b) == []
    rearm(c12b, "w11a", "reviewed CLEAN at y1", say=said.append, tip_for=lambda wt: "y1")
    expect("an UNREVIEWED exit 11 waits, then a reviewer's CLEAN stamp plus rearm lands it at the same head",
           stopped_first and f12b.tick(c12b) == ["w11a"], f"{f12b.started} {f12b.said}")


def case_006_a_re_armed_branch_the_survey_reads_waiting_to_la(expect):
    global c12c
    # 12c. kb/Work PB3628: the stranded branch's shape. PR #467 bounced, a finisher fixed the branch locally (a new
    #      tip, reviewed CLEAN) and the enqueue unit re-armed it, but the survey read it NOT QUEUED (auto-merge was on
    #      at the pull request's OLD head), a state no reader acts on, and nothing launched for hours. The survey now
    #      reads such a branch WAITING TO LAND (prune_worktrees' self-test pins that rule on the real lifecycle), and
    #      the lane lands it; a branch the survey does read NOT QUEUED (its pull request AT the tip) stays the queue
    #      watch's even when re-armed, and the status line says so instead of promising a landing.
    c12c = fresh("rearm-not-queued")
    f12c = Fake([row("w12a", "WAITING TO LAND", "p1"), row("w12b", "WAITING TO LAND", "q1")])
    f12c.tick(c12c)
    finish(c12c, "w12a", 0, "ENQUEUED", end_head="p1")      # PR #467: queued at p1, then bounced (ci-gate failed)
    finish(c12c, "w12b", 0, "ENQUEUED", end_head="q1")
    f12c.live.clear()
    collect(c12c, ["w12a", "w12b"], say=lambda s: None)
    f12c.rows = [row("w12a", "WAITING TO LAND", "p2"), row("w12b", "NOT QUEUED", "q1")]   # w12a: the finisher's p2
    rearm(c12c, "w12a", "finisher 2 fixed the bounce, reviewed CLEAN at p2", say=said.append, tip_for=lambda wt: "p2")
    rearm(c12c, "w12b", "re-armed by hand", say=said.append, tip_for=lambda wt: "q1")
    expect("a re-armed branch the survey reads WAITING TO LAND at the finisher's new tip is launched, and one the "
           "survey reads NOT QUEUED (its pull request at the tip, the queue watch's) is not",
           f12c.tick(c12c) == ["w12a"], f"{f12c.started} {f12c.said}")


def case_007_the_status_line_of_a_re_armed_not_queued_branch(expect):
    status = status_lines(c12c)    # after the tick: the survey it wrote is the one the status line reads
    expect("the status line of a re-armed NOT QUEUED branch does not promise a landing, and names why",
           any(ln.startswith("REARMED") and "w12b" in ln and "does NOT land it" in ln and "NOT QUEUED" in ln and "only the operator does" in ln
               for ln in status), "\n".join(status))


def case_008_candidates_launches_a_re_arm_exactly_when_the_st(expect):
    global arms, rec12
    # the one re-arm predicate (rearm_hold, review lead R1-1): every arm that keeps candidates() from launching a
    # re-arm is the arm the status line names, the worktree arm included, and only a launchable one is promised
    rec12 = {"head": "r1"}
    arms = {"launch": row("w12c", "WAITING TO LAND", "r1"), "no worktree": {**row("w12c", "WAITING TO LAND", "r1"),
                                                                          "worktree": ""},
            "queue": row("w12c", "ENQUEUED", "r1"), "moved": row("w12c", "WAITING TO LAND", "r2"), "gone": None,
            "landed": row("w12c", "LANDED", "r1")}
    agree = {k: (bool(candidates([r] if r else [], [], {}, {"w12c": rec12})),
                 rearm_effect(rec12, r) == "the next tick lands it again at this head") for k, r in arms.items()}
    expect("candidates() launches a re-arm exactly when the status line promises it (no worktree: neither)",
           agree == {"launch": (True, True), **{k: (False, False) for k in arms if k != "launch"}}
           and "no worktree" in rearm_effect(rec12, arms["no worktree"]), str(agree))


def case_009_the_status_line_of_a_re_armed_landed_branch_name(expect):
    # 12d. kb/Work PB3609: the lane's view of a MERGED branch. On 2026-10-24 the lane started `enqueue.py land` on a
    #      branch whose pull request had merged 14 minutes before, 0 commits ahead of main: the enqueue unit had
    #      re-armed it before a reboot, and the survey read the merged branch IN FLIGHT (touched inside --min-age), so
    #      no hold applied. The survey now reads it LANDED: the re-arm is DROPPED (not held as "the queue owns it",
    #      which is wrong for a merged branch), and the status line before the drop says why.
    landed_effect = rearm_effect(rec12, arms["landed"])
    expect("the status line of a re-armed LANDED branch names its work on main, never the queue's ownership",
           "on main" in landed_effect and "drops the re-arm" in landed_effect and "queue" not in landed_effect,
           landed_effect)


def case_010_a_re_armed_branch_whose_work_is_on_main_is_not_l(expect):
    c12d = fresh("rearm-landed")
    f12d = Fake([row("w12d", "WAITING TO LAND", "m1"), row("w12e", "WAITING TO LAND", "n1")])
    f12d.tick(c12d)
    for b, h in (("w12d", "m1"), ("w12e", "n1")):
        finish(c12d, b, 0, "ENQUEUED", end_head=h)
    f12d.live.clear()
    collect(c12d, ["w12d", "w12e"], say=lambda s: None)
    f12d.rows = [row("w12d", "LANDED", "m1"), row("w12e", "WAITING TO LAND", "n1")]   # PR of w12d merged
    rearm(c12d, "w12d", "re-armed before the reboot", say=said.append, tip_for=lambda wt: "m1")
    rearm(c12d, "w12e", "re-armed by hand", say=said.append, tip_for=lambda wt: "n1")
    expect("a re-armed branch whose work is on main is not launched and its re-arm is dropped, said; a re-arm of "
           "a branch still to land is untouched",
           f12d.tick(c12d) == ["w12e"] and not rearm_path(c12d, "w12d").exists()
           and any("w12d" in ln and "its work is on main" in ln and "dropped" in ln for ln in f12d.said),
           f"{f12d.started} {f12d.said}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
