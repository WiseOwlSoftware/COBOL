#!/usr/bin/env python3
# SELF-TEST-POPULATION: tree — it walks the tracked markdown files with git over the whole repository
# SELF-TEST-CLOCK: real — --min-age compares branch-touch and dispatch-ledger ages (git commit times) with the clock
"""Cases of plan_wave.py on fabricated notes, clusters and reports (ported from scripts/orchestrator/test_plan_wave.py,
kb/Work PB5428): work awaiting landing is never re-planned, finishers come first and name their predecessor, clusters
rank by rows claimed, a second group on a file is a same-file successor, the model routing rules, the budget fill, that
the groups render through the real dispatch-spec template and pass check_practices.py's same-file rule; that the fix
lane's whole plan equals the golden the pre-campaign planner wrote; and the campaign lane (--cluster, kb/Work PB2120).
The cases run in name order over ONE world (`fixtures.plant`, bound by case_000), as the sequential script did.
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/plan_wave/test_planner.py   (no build, no git; the
campaign section needs the tools/claude-skills submodule, and its absence is a FAIL)."""
import selftest_cases

pw = selftest_cases.subject(__file__)   # scripts/orchestrator/plan_wave.py
import fixtures  # noqa: E402
import contextlib  # noqa: E402
import dataclasses  # noqa: E402
import io  # noqa: E402
import json  # noqa: E402
import os  # noqa: E402
import pathlib  # noqa: E402
import re  # noqa: E402
import subprocess  # noqa: E402
import sys  # noqa: E402
import tempfile  # noqa: E402
import coord  # noqa: E402
import inflight as inb  # noqa: E402
import clusters as clusters_mod  # noqa: E402
import prune_worktrees as prune  # noqa: E402
import git_batch  # noqa: E402
import prune_worktrees  # noqa: E402
import live_tools  # noqa: E402
import work  # noqa: E402
import member_index  # noqa: E402
import r2_collect  # noqa: E402
import file_census_notes  # noqa: E402
import gate_slot  # noqa: E402
from types import SimpleNamespace  # noqa: E402
from reports import Report, Review, load_reports, parse_report, parse_review, report_index, same_commit, status_line  # noqa: E402
from inflight import (Claim, InFlight, IndexPolicy, branch_replays, changed_files, classify_inflight, collision,
                      collision_hits, collision_set, declared_sets, hold, inflight_file_sets, inflight_units,
                      unlanded_branch_notes)
from clusters import (Note, add_closed_notes, branch_lead_notes, campaign_clusters, closed_by_branch,
                      declare_fix_lane, fold_unsited, lift_tiers, load_fix_clusters, load_notes, notes_cap,
                      predicted_turns, ready_notes_view, turn_budget)
from groups import (Group, as_group_json, attempts, body_of, campaign_requirements, group_cost, group_requirements,
                    landing_cost, requirements_text, DRAIN_CLAUSE, FINDINGS_ANSWER, FINISH_FIRST, branch_resume,
                    finisher_pred, half_clause, join_preds, shared_rules, CARRIED_CLASSES, LANDED_RULE, RESUME_RULE,
                    UNLANDED_RULE)
from dispatch_ledger import (held_by_dispatch, partition_record, partition_summary, record_dispatch,
                             record_partition, release_dispatch)
from scratch_repo import ScratchRepo, isolate, scratch_dir  # noqa: E402
from fixtures import cl, collided, note, on_members, report, returned, rules_capped, running, check_practices, once_rule, mds  # noqa: E402

# the world's state: bound by case_000 from fixtures.plant(), read by every later case
CLASS, CLUSTERS, F_BIND, F_EMIT, F_G4, F_OO, HALF, LIVE_RULES, REPORTS, RULES, TMP, UNLANDED, WORK, notes, reports = None, None, None, None, None, None, None, None, None, None, None, None, None, None, None


def case_000_wrapped_inventory_rows_counted(expect):
    global CLASS, CLUSTERS, F_BIND, F_EMIT, F_G4, F_OO, HALF, LIVE_RULES, REPORTS, RULES, TMP, UNLANDED, WORK, notes, reports
    w = fixtures.plant()
    CLASS = w["CLASS"]
    CLUSTERS = w["CLUSTERS"]
    F_BIND = w["F_BIND"]
    F_EMIT = w["F_EMIT"]
    F_G4 = w["F_G4"]
    F_OO = w["F_OO"]
    HALF = w["HALF"]
    LIVE_RULES = w["LIVE_RULES"]
    REPORTS = w["REPORTS"]
    RULES = w["RULES"]
    TMP = w["TMP"]
    UNLANDED = w["UNLANDED"]
    WORK = w["WORK"]
    notes = w["notes"]
    reports = w["reports"]
    expect.equal("wrapped inventory_rows counted", len(notes["PB6"].rows), 10)
    expect.equal("report header ids only", sorted(r.lead for r in reports if "PB9999" in r.notes), [])


def case_001_report_parse(expect):
    global r3
    r3 = next(r for r in reports if r.lead == "PB3")
    expect.equal("report parse", (r3.status, r3.branch, r3.head, r3.not_started, r3.worktree),
          ("SPLIT", "worktree-wf_bbb-2", "2222222bb", True, r"E:\COBOL\.claude\worktrees\wf_bbb-2"))
    expect.equal("status from **Status:** line", next(r for r in reports if r.slug == "w1013c").status, "DONE")


def case_002_the_planner_refuses_a_damaged_note_by_na(expect):
    global stop
    # A damaged note leaves the plan as silently as it left the register unless the planner's own reader refuses it (kb/Work
    # PB3654): a note named PB<n>.md whose fence is not its first line stops the plan, an index is skipped.
    DAMAGED = TMP / "damaged-register"
    DAMAGED.mkdir()
    (DAMAGED / "PB98.md").write_text("`---\n`id: PB98\n`---\n", encoding="utf-8", newline="\n")
    (DAMAGED / "PBindex.md").write_text("an index, not a note\n", encoding="utf-8", newline="\n")
    try:
        load_notes(DAMAGED, {})
        unreadable = "the planner read a register holding a damaged note"
    except SystemExit as stop:
        unreadable = str(stop)
    expect.equal("the planner refuses a damaged note by name", ("PB98.md" in unreadable, "PBindex" in unreadable),
          (True, False))


def case_003_awaiting_landing(expect):
    global G, g, order, p
    p = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100)
    G = {tuple(g.notes): g for g in p["groups"]}
    if "-v" in sys.argv:
        for g in p["groups"]:
            print(g.letter, g.after, g.kind, g.model, g.notes, g.file)
    order = [g.notes for g in p["groups"]]

    # 1. work on an unlanded branch with a report is not planned again; a branch naming no open note is surfaced
    expect.equal("awaiting landing", p["awaiting_landing"], {"worktree-wf_aaa-1": ["PB1"]})
    expect.equal("PB1 not planned", any("PB1" in g.notes for g in p["groups"]), False)
    expect.equal("unlanded branch without note", p["unlanded_without_note"], ["worktree-wf_eee-1"])


def case_004_finisher_order(expect):
    global FIN
    # 2. finishers first: PB3 (split, landed predecessor) absorbs the notes of BOTH clusters on its file (the cap), then
    #    the half PB4, then the branch-only PB13
    FIN = ("PB3", "PB6", "PB7", "PB12")
    expect.equal("finisher order", order[:3], [list(FIN), ["PB4"], ["PB13"]])
    expect.equal("finisher pred names report and branch",
          all(s in G[FIN].pred for s in ("w1014b-PB3-report.md", "worktree-wf_bbb-2", "LANDED")), True)
    expect.equal("half pred", "status: half" in G[("PB4",)].pred, True)
    expect.equal("half pred names its unlanded branch", "worktree-wf_fff-3" in G[("PB4",)].pred, True)
    expect.equal("half note planned once", sum("PB4" in g.notes for g in p["groups"]), 1)
    expect.equal("branch-only pred", "worktree-wf_ddd-9" in G[("PB13",)].pred, True)

    # 3. clusters by rows claimed: PB2+PB5 (2+1) first, the 1-row ones next
    expect.equal("cluster order", (order[3], order[-1]), (["PB2", "PB5", "PB14"], ["PB10"]))


def case_005_one_step_per_file_the_second_cluster_on(expect):
    # 4. ONE STEP PER FILE (kb/Work PB4396): a second cluster on a file is that file's group's work, absorbed into it (PB14,
    #    0 rows, ranks last and joins PB2+PB5 on F_BIND), never a same-file successor
    g25 = G[("PB2", "PB5", "PB14")]
    expect.equal("one step per file: the second cluster on a file joins its group, no successor",
          (g25.after, [g.notes for g in p["groups"] if "PB14" in g.notes], any(g.after for g in p["groups"])),
          ("", [["PB2", "PB5", "PB14"]], False))

    # 5. routing
    expect.equal("default sonnet", g25.model, "sonnet")
    expect.equal("oo file -> opus", G[("PB8",)].model, "opus")
    expect.equal(".g4 -> opus", G[("PB11",)].model, "opus")
    expect.equal("open design question -> opus", G[("PB9",)].model, "opus")
    expect.equal("failed twice -> opus", (G[("PB10",)].model, "2 times" in G[("PB10",)].why_model), ("opus", True))
    expect.equal("never fable", {g.model for g in p["groups"]} <= set(RULES["routing"]["allowed"]), True)


def case_006_tight_budget_first_group_its_landing(expect):
    global one, p2
    # 6. budget fill: a tight budget takes the first finisher only; its successor's predecessor rule holds
    one = group_cost(G[("PB3", "PB6", "PB7", "PB12")].model, 4, RULES) + landing_cost(RULES)
    p2 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=one + 0.01)
    expect.equal("tight budget: first group + its landing", [g.notes for g in p2["groups"]], [["PB3", "PB6", "PB7", "PB12"]])
    expect.equal("points include the landing", round(p2["points"], 6), round(one, 6))


def case_007_a_group_that_does_not_fit_is_skipped_a_s(expect):
    global p3
    small = group_cost("sonnet", 1, RULES) + landing_cost(RULES)
    p3 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=small + 0.01)
    expect.equal("a group that does not fit is skipped, a smaller later one taken", [g.notes for g in p3["groups"]], [["PB4"]])
    expect.equal("successor never without predecessor", any(g.after for g in p3["groups"]), False)


def case_008_max_groups(expect):
    global p4
    p4 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
                 max_groups=2)
    expect.equal("max groups", len(p4["groups"]), 2)
    expect.equal("no train: the plan carries no train count (kb/Work PB3023)", "trains" in p, False)


def case_009_split_on_an_unlanded_branch_is_a_finishe(expect):
    global p5
    # 6b. A SPLIT REPORT ON AN UNLANDED BRANCH IS A FINISHER, NOT "AWAITING LANDING" (kb/Work PB2575): a `land` unit lands
    #     only DONE branches, so holding it stranded the work (waves 1020 D and 1032 A). Its pred cherry-picks the branch.
    #     A DONE report that names a note NOT started still waits for its landing. Own reports, so check 8's golden holds.
    SPLIT_REPORTS = TMP / "reports-split"
    SPLIT_REPORTS.mkdir()
    # The notes a report answers are the notes its group was DISPATCHED with (PB3473): wave 1020's group E held PB7 and PB12.
    SPLIT_COORD = TMP / "coord-split"
    SPLIT_COORD.mkdir()
    record_dispatch(SPLIT_COORD, "1020", [{"letter": "e", "notes": ["PB7", "PB12"], "files": ["src/X/Emitter.cs"]}],
                       lambda i: False)
    (SPLIT_REPORTS / "w1020d-PB5-report.md").write_text(
        "SPLIT\n# PB5 group\n**Status:** SPLIT at the turn cap · **worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_ggg-4` · "
        "**branch:** `worktree-wf_ggg-4` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    (SPLIT_REPORTS / "w1020e-PB7-report.md").write_text(
        "DONE\n# PB7 group (PB12 NOT started)\n**branch:** `worktree-wf_hhh-5` · **HEAD:** `4444444dd`\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(SPLIT_REPORTS)
    SPLIT_CLASS = {"worktree-wf_ggg-4": "UNLANDED", "worktree-wf_hhh-5": "UNLANDED"}
    p5 = pw.plan(notes, CLUSTERS, HALF, load_reports(SPLIT_REPORTS, SPLIT_COORD), lambda b: SPLIT_CLASS.get(b, "ABSENT"), {}, RULES,
                 budget_points=100)
    g5 = next((g for g in p5["groups"] if "PB5" in g.notes), None)
    expect.equal("split on an unlanded branch is a finisher", (g5.kind if g5 else None, "PB5" in str(p5["awaiting_landing"])),
          ("finisher", False))
    expect.equal("its pred cherry-picks the unlanded branch",
          bool(g5) and all(s in g5.pred for s in ("worktree-wf_ggg-4", "UNLANDED", "cherry-pick")), True)
    expect.equal("done with a note not started still awaits landing", p5["awaiting_landing"].get("worktree-wf_hhh-5"),
          ["PB7", "PB12"])


def case_010_the_review_stamps_parse(expect):
    global RV_REPORTS, rv_reports
    # 6b'. A DONE BRANCH WHOSE REVIEW STAMPED FINDINGS IS A FINISHER (kb/Work PB3027): the land unit never fixes a review
    #      finding, so the finisher's pred names the findings file and the answer-by-id rule. A CLEAN stamp still awaits
    #      landing; a placeholder `Review:` line stamps nothing.
    RV_REPORTS = TMP / "reports-review"
    RV_REPORTS.mkdir()
    (RV_REPORTS / "w1062e-PB5-report.md").write_text(
        "# PB5 report\n**Status:** DONE · **branch:** `worktree-wf_rv-1` · **HEAD:** `6666666ff`\n"
        "**Review:** FINDINGS 2 w1062e-review-r1 6666666ff E:/s/review-w1062-e.jsonl\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    (RV_REPORTS / "w1062f-PB7-report.md").write_text(
        "# PB7 report\n**Status:** DONE · **branch:** `worktree-wf_rv-2` · **HEAD:** `7777777aa`\n"
        "**Review:** CLEAN w1062f-review-r1 7777777aa\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    rv_reports = load_reports(RV_REPORTS)
    expect.equal("the review stamps parse", [(r.review.verdict, r.review.findings) if r.review else None for r in rv_reports],
          [("FINDINGS", 2), ("CLEAN", 0)])


def case_011_a_findings_stamp_is_a_finisher_whose_pre(expect):
    p5r = pw.plan(notes, CLUSTERS, HALF, rv_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
    g5r = next((g for g in p5r["groups"] if "PB5" in g.notes), None)
    expect.equal("a FINDINGS stamp is a finisher whose pred names the findings file and the answer rule",
          (g5r.kind if g5r else None, bool(g5r) and all(s in g5r.pred for s in ("review-w1062-e.jsonl", "no_change_needed",
                                                                                  "git merge worktree-wf_rv-1"))),
          ("finisher", True))
    expect.equal("a CLEAN stamp awaits landing", p5r["awaiting_landing"].get("worktree-wf_rv-2"), ["PB7"])
    expect.equal("a placeholder Review line stamps nothing", parse_review("**Review:** stamped by the branch reviewer"), None)


def case_012_an_o11_header_s_branch_field_wins_over_t(expect):
    # 6b''. THE BRANCH IS THE HEADER'S FIELD, NEVER A WORD OF THE REVIEW STAMP (kb/Work PB3077): review_findings.py
    #       stamps the review one line under the Status line, ABOVE the Branch field of an O11 header, and its reviewer
    #       label `land-review-<branch>-r1` carries the branch name with a suffix. The header's first `worktree-…` word
    #       was that label: a branch that does not exist, ABSENT, so every reviewed branch read as landed, and wave
    #       1064's review finishers (2026-10-11) were first told to merge `worktree-wf_aba5c7ac-b61-1-r1`.
    (RV_REPORTS / "w1062g-PB5-report.md").write_text(
        "Status: DONE\n**Review:** FINDINGS 2 land-review-worktree-wf_rv-3-r1 8888888bb E:/s/review-worktree-wf_rv-3.jsonl\n"
        "Worktree: E:\\COBOL\\.claude\\worktrees\\wf_rv-3\nBranch: worktree-wf_rv-3\nHEAD: 8888888bb\nBase: 1111111aa\n"
        "# PB5 report\n\n## What landed\n", encoding="utf-8", newline="\n")
    (RV_REPORTS / "w1062h-PB7-report.md").write_text(
        "# PB7 report\n**Status:** DONE · **branch:** `worktree-wf_rv-4` · **HEAD:** `9999999cc`\n"
        "**Review:** FINDINGS 1 land-review-worktree-wf_rv-4-r1 9999999cc E:/s/review-worktree-wf_rv-4.jsonl\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    (RV_REPORTS / "w1062i-PB12-report.md").write_text(
        "Status: DONE\n**Review:** FINDINGS 1 land-review-worktree-wf_rv-5-r1 aaaaaaa11 E:/s/r.jsonl\n"
        "# PB12 report — the work of branch worktree-wf_rv-5 (a header with no Branch field)\n\n## X\n", encoding="utf-8", newline="\n")
    rv2 = {r.slug: r for r in load_reports(RV_REPORTS)}
    expect.equal("an O11 header's Branch field wins over the review stamp's reviewer label above it",
          (rv2["w1062g"].branch, rv2["w1062g"].worktree, rv2["w1062g"].head),
          ("worktree-wf_rv-3", "E:\\COBOL\\.claude\\worktrees\\wf_rv-3", "8888888bb"))
    expect.equal("a template header's **branch:** field wins over the label below it", rv2["w1062h"].branch, "worktree-wf_rv-4")
    expect.equal("with no field, the first branch word OUTSIDE the stamp is the branch", rv2["w1062i"].branch, "worktree-wf_rv-5")
    expect.equal("the stamp still parses beside the field", (rv2["w1062g"].review.verdict, rv2["w1062g"].review.findings),
          ("FINDINGS", 2))


def case_013_mid_prose_branch_is_not_a_field_with_no(expect):
    # The field is read at a FIELD POSITION only (PB3077 review round 1, R1-1): header prose says "this branch: every …"
    # in real reports (w1043x-PB2806, w1055z-PB2426), and an unanchored `branch:` rule took `every` as the branch — ABSENT,
    # the same harm — in a header with no field or with that prose above the field.
    (RV_REPORTS / "w1062j-PB5-report.md").write_text(
        "Status: DONE\n**Review:** FINDINGS 1 land-review-worktree-wf_rv-6-r1 bbbbbbb22 E:/s/r.jsonl\n"
        "# PB5 report — the work of branch worktree-wf_rv-6; on this branch: every file is LF\n\n## X\n", encoding="utf-8", newline="\n")
    (RV_REPORTS / "w1062k-PB7-report.md").write_text(
        "Status: DONE\n# PB7 report — on this branch: the reader takes the field\nBranch: worktree-wf_rv-7\nHEAD: ccccccc33\n"
        "\n## X\n", encoding="utf-8", newline="\n")
    rv3 = {r.slug: r for r in load_reports(RV_REPORTS)}
    expect.equal("mid-prose `branch:` is not a field: with no field the branch word wins, never the prose", rv3["w1062j"].branch,
          "worktree-wf_rv-6")
    expect.equal("mid-prose `branch:` above the Branch field does not pre-empt it", rv3["w1062k"].branch, "worktree-wf_rv-7")


def case_014_a_report_answers_its_lead_and_its_dispat(expect):
    global mn_reports
    # 6b'''0. A REPORT ANSWERS THE NOTES ITS GROUP WAS DISPATCHED WITH, NEVER THE IDS ITS HEADER MENTIONS (kb/Work PB3473,
    #        PB3152). Wave 1135 held the top-ranked note back because the header of a DONE report on an unlanded branch
    #        said "Wave 1134's plan held PB3337 ..." (it went to `awaiting landing` under that branch), and wave 1076 planned
    #        a FINDINGS report's review finisher under the open note its header mentioned. Each header below MENTIONS an open
    #        note above its first `## ` heading (a mention in a section body was always ignored, so a fixture that puts it
    #        there witnesses nothing); the ledger group of each names only its own lead.
    MN_REPORTS = TMP / "reports-mention"
    MN_REPORTS.mkdir()
    MN_COORD = TMP / "coord-mention"
    MN_COORD.mkdir()
    record_dispatch(MN_COORD, "1136", [{"letter": "a", "notes": ["PB13"], "files": ["src/X/Zero.cs"]},
                                          {"letter": "b", "notes": ["PB14"], "files": ["src/X/Binder.cs"]}], lambda i: False)
    (MN_REPORTS / "w1136a-PB13-report.md").write_text(
        "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_mn-1` · **HEAD:** `6a6a6a6aa`\n"
        "**Review:** FINDINGS 1 w1136a-review-r1 6a6a6a6aa E:/s/review-mn-a.jsonl\n\n"
        "Wave 1136 also held PB10 (the top of the list) and PB9, which this branch does not touch.\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    (MN_REPORTS / "w1136b-PB14-report.md").write_text(
        "# PB14 report\n**Status:** DONE · **branch:** `worktree-wf_mn-2` · **HEAD:** `7b7b7b7bb`\n"
        "**Review:** CLEAN w1136b-review-r1 7b7b7b7bb\n\nThis plan held PB11 on a file the branch does not edit.\n\n"
        "## Reproduced?\n", encoding="utf-8", newline="\n")
    returned(MN_REPORTS)
    mn_reports = load_reports(MN_REPORTS, MN_COORD)
    expect.equal("a report answers its lead and its dispatch group's notes, not the ids its header mentions",
          {r.slug: r.notes for r in mn_reports}, {"w1136a": ["PB13"], "w1136b": ["PB14"]})
    expect.equal("a report no ledger group knows answers its lead alone",
          {r.slug: r.notes for r in load_reports(MN_REPORTS)}, {"w1136a": ["PB13"], "w1136b": ["PB14"]})


def case_015_a_mentioned_note_is_not_awaiting_landing(expect):
    pmn = pw.plan(notes, CLUSTERS, HALF, mn_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
    expect.equal("a mentioned note is not awaiting landing under the mentioning report's branch",
          pmn["awaiting_landing"], {"worktree-wf_mn-2": ["PB14"]})
    expect.equal("every mentioned note is planned in a group of its own cluster",
          {i: any(i in g.notes for g in pmn["groups"]) for i in ("PB10", "PB9", "PB11")},
          {"PB10": True, "PB9": True, "PB11": True})
    expect.equal("a FINDINGS report's finisher is planned under its own lead alone, not under a note its header mentions",
          sorted((g.kind, tuple(g.notes)) for g in pmn["groups"] if g.kind == "finisher" and {"PB13", "PB10", "PB9"} & set(g.notes)),
          [("finisher", ("PB13",))])
    expect.equal("a mention is no attempt at the mentioned note (choose_model escalates on attempts)",
          (attempts("PB10", mn_reports), attempts("PB13", mn_reports)), (0, 1))


def case_016_before_the_register_is_read_a_report_ans(expect):
    global CL_COORD, CL_REPORTS, _Shown, cl_reports
    # 6b'''00. A NOTE THE BRANCH CLOSES BEYOND ITS DISPATCH (branch review R1-1 of the PB3473 fix). An implementer routinely
    #         closes a note its group was not dispatched with ("PB3383 (+ PB925)"): the ledger and the report's file name
    #         name only PB13, so without the register the DONE branch waiting to land held PB13 alone and PB10, which the
    #         branch already flipped to `landed`, was planned again (a double dispatch). The branch's own kb/Work records
    #         what it finished: PB10 is flipped, PB9 is only touched (still open on the branch), PB13 is the lead.
    class _Shown:
        def __init__(self, rc, out=""):
            self.returncode, self.stdout = rc, out


    CL_REPORTS = TMP / "reports-closes"
    CL_REPORTS.mkdir()
    CL_COORD = TMP / "coord-closes"
    CL_COORD.mkdir()
    record_dispatch(CL_COORD, "1137", [{"letter": "a", "notes": ["PB13"], "files": ["src/X/Zero.cs"]}], lambda i: False)
    (CL_REPORTS / "w1137a-PB13-report.md").write_text(
        "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_cl-1` · **HEAD:** `8c8c8c8cc`\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(CL_REPORTS)
    cl_reports = load_reports(CL_REPORTS, CL_COORD)
    expect.equal("before the register is read a report answers its lead alone", [r.notes for r in cl_reports], [["PB13"]])


def case_017_a_branch_closes_the_notes_it_flipped_to(expect):
    global _branch_git, _cl_git
    def _branch_git(branch, status):
        """A fake git for `branch`, whose register changed the notes of `status` ({id: the status ON the branch}) and the
    source file Zero.cs. A diff asked for any other range or pathspec than `origin/main...<branch> -- kb/Work` lists
    nothing, so a closed_by_branch that passes the wrong range or drops the pathspec finds no note and the checks go red."""
        def git(*args, cwd=None):
            if args[:3] == ("diff", "--name-only", "--no-renames"):
                if args[3:] != (f"origin/main...{branch}", "--", "kb/Work"):
                    return _Shown(0, "")
                return _Shown(0, "".join(f"kb/Work/{nid}.md\n" for nid in status) + "kb/Work/README.md\nsrc/X/Zero.cs\n")
            for nid, st in status.items():
                if args == ("show", f"{branch}:kb/Work/{nid}.md"):
                    return _Shown(0, f"---\nid: {nid}\nstatus: {st}\narea: runtime\n---\n\n# {nid}\n")
            return _Shown(128)
        return git


    _cl_git = _branch_git("worktree-wf_cl-1", {"PB13": "landed", "PB10": "retired", "PB9": "open"})


    expect.equal("a branch closes the notes it flipped to a terminal status, not the ones it only touched",
          closed_by_branch("worktree-wf_cl-1", _cl_git), ["PB10", "PB13"])


def case_018_a_finished_report_on_an_unlanded_branch(expect):
    add_closed_notes(cl_reports, lambda b: "UNLANDED", _cl_git, lambda a, b: False)
    expect.equal("a finished report on an unlanded branch answers its lead, its group and what the branch closes",
          [r.notes for r in cl_reports], [["PB13", "PB10"]])


def case_019_the_note_the_branch_closed_is_awaiting_l(expect):
    pcl = pw.plan(notes, CLUSTERS, HALF, cl_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
    expect.equal("the note the branch closed is awaiting landing with it, never planned a second time",
          (pcl["awaiting_landing"], any("PB10" in g.notes or "PB13" in g.notes for g in pcl["groups"])),
          ({"worktree-wf_cl-1": ["PB13", "PB10"]}, False))
    expect.equal("a note the branch only touched is still planned", any("PB9" in g.notes for g in pcl["groups"]), True)


def case_020_a_landed_branch_adds_nothing_its_notes_a(expect):
    cl_landed = load_reports(CL_REPORTS, CL_COORD)
    add_closed_notes(cl_landed, lambda b: "LANDED", _cl_git, lambda a, b: False)
    expect.equal("a landed branch adds nothing (its notes are closed on main)", [r.notes for r in cl_landed], [["PB13"]])


def case_021_a_split_report_on_an_unlanded_branch_ans(expect):
    global cl_split
    # 6b'''00 (R2). THE SAME HOLD FOR AN UNFINISHED BRANCH (branch review R2-1, R2-2). A SPLIT report's finisher takes the
    #         branch's commits, which carry the notes the branch already closed: they belong to that finisher's group, or
    #         each is planned again as a cluster group that redoes the work the finisher cherry-picks. And a finisher
    #         CONTINUES one branch: the closed notes can take the group past the cap, and a second chunk would be a second
    #         agent re-answering the first one's review findings on the same branch.
    CL_SPLIT = TMP / "reports-closes-split"
    CL_SPLIT.mkdir()
    (CL_SPLIT / "w1137a-PB13-report.md").write_text(
        "# PB13 report\nStatus: SPLIT at the turn cap\n**branch:** `worktree-wf_cl-1` · **HEAD:** `8c8c8c8cc`\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(CL_SPLIT)
    cl_split = load_reports(CL_SPLIT, CL_COORD)
    add_closed_notes(cl_split, lambda b: "UNLANDED", _cl_git, lambda a, b: False)
    expect.equal("a SPLIT report on an unlanded branch answers what the branch closes too", [r.notes for r in cl_split],
          [["PB13", "PB10"]])


def case_022_the_split_finisher_takes_the_closed_note(expect):
    pcs = pw.plan(notes, CLUSTERS, HALF, cl_split, lambda b: "UNLANDED", {}, RULES, budget_points=100)
    expect.equal("the SPLIT finisher takes the closed note, which no cluster group plans a second time",
          sorted((g.kind, tuple(g.notes)) for g in pcs["groups"] if {"PB13", "PB10"} & set(g.notes)),
          [("finisher", ("PB10", "PB13"))])


def case_023_a_findings_branch_that_closed_more_notes(expect):
    CAP_RULES = rules_capped(RULES, 2)
    CAP_REPORTS = TMP / "reports-closes-cap"
    CAP_REPORTS.mkdir()
    CAP_COORD = TMP / "coord-closes-cap"
    CAP_COORD.mkdir()
    record_dispatch(CAP_COORD, "1138", [{"letter": "a", "notes": ["PB13", "PB14"], "files": ["src/X/Zero.cs"]}],
                       lambda i: False)
    (CAP_REPORTS / "w1138a-PB13-report.md").write_text(
        "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_cap-1` · **HEAD:** `9d9d9d9dd`\n"
        "**Review:** FINDINGS 1 w1138a-review-r1 9d9d9d9dd E:/s/review-cap-a.jsonl\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(CAP_REPORTS)
    cap_reports = load_reports(CAP_REPORTS, CAP_COORD)
    add_closed_notes(cap_reports, lambda b: "UNLANDED",
                        _branch_git("worktree-wf_cap-1", {"PB13": "landed", "PB14": "landed", "PB10": "retired"}),
                        lambda a, b: False)
    pcap = pw.plan(notes, CLUSTERS, HALF, cap_reports, lambda b: "UNLANDED", {}, CAP_RULES, budget_points=100)
    expect.equal("a FINDINGS branch that closed more notes than the cap has ONE review finisher, not one per chunk",
          sorted((g.kind, tuple(g.notes)) for g in pcap["groups"] if {"PB13", "PB14", "PB10"} & set(g.notes)),
          [("finisher", ("PB10", "PB13", "PB14"))])


def case_024_the_successor_s_report_answers_what_it_c(expect):
    global pr_reports
    # 6b'''00 (PB3508). A NOTE THE PREDECESSOR CLOSES IS NEVER ITS SUCCESSOR'S. fix-pb3504 (the successor) merged
    #         fix-pb3489 (the predecessor) at its reviewed head, so PB3489's flip to `landed` was in fix-pb3504's diff from
    #         main too. Credited to fix-pb3504's newer DONE report, PB3489 read `awaiting landing` with it, and PB3489's
    #         review finisher was never planned while the survey held fix-pb3504 for those findings: wave 1143 planned
    #         nothing. Here the predecessor's FINDINGS report (PB13) keeps its finisher and the successor holds PB14 alone.
    PR_REPORTS = TMP / "reports-closes-pred"
    PR_REPORTS.mkdir()
    PR_COORD = TMP / "coord-closes-pred"
    PR_COORD.mkdir()
    record_dispatch(PR_COORD, "1141", [{"letter": "e", "notes": ["PB13"], "files": ["src/X/Zero.cs"]},
                                          {"letter": "g", "notes": ["PB14"], "files": ["src/X/Zero.cs"]}], lambda i: False)
    (PR_REPORTS / "w1141e-PB13-report.md").write_text(
        "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_pr-1` · **HEAD:** `5e5e5e5ee`\n"
        "**Review:** FINDINGS 1 w1141e-review-r1 5e5e5e5ee E:/s/review-pr-e.jsonl\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    (PR_REPORTS / "w1141g-PB14-report.md").write_text(
        "# PB14 report\n**Status:** DONE · **branch:** `worktree-wf_pr-2` · **HEAD:** `6f6f6f6ff`\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(PR_REPORTS)
    pr_reports = load_reports(PR_REPORTS, PR_COORD)
    _pr_pred = _branch_git("worktree-wf_pr-1", {"PB13": "landed"})
    _pr_succ = _branch_git("worktree-wf_pr-2", {"PB13": "landed", "PB14": "landed"})


    def _pr_git(*args, cwd=None):
        pred = _pr_pred(*args)
        return pred if pred.returncode == 0 and pred.stdout else _pr_succ(*args)


    # the one ancestry fact: the predecessor's branch is contained in the successor's (the planner's graph, kb/Work PB3582)
    add_closed_notes(pr_reports, lambda b: "UNLANDED", _pr_git,
                        lambda a, b: (a, b) == ("worktree-wf_pr-1", "worktree-wf_pr-2"))
    expect.equal("the successor's report answers what it closed itself, not what its contained predecessor closed",
          sorted((r.lead, tuple(r.notes)) for r in pr_reports), [("PB13", ("PB13",)), ("PB14", ("PB14",))])


def case_025_the_predecessor_s_review_finisher_is_pla(expect):
    ppr = pw.plan(notes, CLUSTERS, HALF, pr_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
    expect.equal("the predecessor's review finisher is planned and the successor awaits landing with its own note alone",
          (ppr["awaiting_landing"], sorted((g.kind, tuple(g.notes)) for g in ppr["groups"] if {"PB13", "PB14"} & set(g.notes))),
          ({"worktree-wf_pr-2": ["PB14"]}, [("finisher", ("PB13",))]))


def case_026_an_unseen_lead_is_read_from_its_own_bran(expect):
    global DR, DR_CARRIER, DR_CLASS, dr_notes, dr_reports, first, hdr, name
    # 6b'''. THE DRAIN AND THE UNSEEN LEAD (kb/Work PB3150). Wave 1075's drain plan (`--max-groups 2`) put the SPLIT of
    #        w1074a first, whose finisher would have started PB3101 (never started) and cherry-picked `…-e99-1` again
    #        though `…-e99-4` merged it, and never planned license-product-name's review finisher: its lead PB3146 is
    #        process_only and existed only on its branch. --drain plans the finishers of STARTED work only; a FINDINGS
    #        report whose lead the fix lane cannot see gets its finisher in every mode, decided by the same rule as an
    #        open note (its newest report naming it, on any branch), and a DONE branch that merely contains it is named in
    #        its pred, never a reason to drop it (review R1-1, R1-2).
    DR = TMP / "reports-drain"
    DR.mkdir()
    DR_COORD = TMP / "coord-drain"      # wave 1074's group A held PB5 and PB12 (its report says PB12 was not started)
    DR_COORD.mkdir()
    record_dispatch(DR_COORD, "1074", [{"letter": "a", "notes": ["PB5", "PB12"], "files": ["src/X/Binder.cs"]}],
                       lambda i: False)
    for name, first, hdr in (
            ("w1074a-PB5-report.md", "SPLIT", "# PB5 group (PB12 not started)\n**branch:** `worktree-wf_dr-1` · **HEAD:** `1010101aa`"),
            ("w1074b-PB7-report.md", "Status: DONE", "**Review:** FINDINGS 1 w1074b-review-r1 2020202bb E:/s/review-w1074-b.jsonl\n"
                                                     "Branch: worktree-wf_dr-2\nHEAD: 2020202bb"),
            ("w1074c-PB6-report.md", "SPLIT", "# PB6 group\n**branch:** `worktree-wf_dr-3` · **HEAD:** `3030303cc`"),
            ("w1074d-PB10-report.md", "SPLIT", "# PB10 group\n**branch:** `worktree-wf_dr-4` · **HEAD:** `4040404dd`"),
            ("w1074z-PB77-report.md", "Status: DONE", "**Review:** FINDINGS 1 enqueue-review-owner-task-r1 5050505ee "
                                                      "E:/s/review-owner-task.jsonl\nBranch: owner-task\nHEAD: 5050505ee"),
            ("w1074y-PB79-report.md", "Status: DONE", "**Review:** FINDINGS 1 enqueue-review-r1 6060606ff E:/s/r79.jsonl\n"
                                                      "Branch: worktree-wf_dr-5\nHEAD: 6060606ff")):
        (DR / name).write_text(f"{first}\n{hdr}\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    DR_CLASS = {"worktree-wf_dr-1": "UNLANDED", "worktree-wf_dr-2": "UNLANDED", "worktree-wf_dr-3": "UNLANDED",
                "worktree-wf_dr-4": "LANDED", "owner-task": "UNLANDED", "worktree-wf_dr-5": "UNLANDED",
                "worktree-wf_dr-6": "UNLANDED", "worktree-wf_dr-7": "UNLANDED"}
    DR_CARRIER = {"worktree-wf_dr-1": "worktree-wf_dr-2", "worktree-wf_dr-5": "worktree-wf_dr-2",
                  "worktree-wf_dr-6": "worktree-wf_dr-2"}
    returned(DR)
    dr_reports = load_reports(DR, DR_COORD)


    def _show(*args, cwd=None):
        if args[:2] == ("show", "owner-task:kb/Work/PB77.md"):
            return _Shown(0, '---\ntitle: "PB77 — an owner task"\nid: PB77\nkind: defect\nstatus: landed\narea: process\n'
                             "process_only: true\n---\n\n# PB77\n")
        return _Shown(128)


    dr_notes = dict(notes)
    dr_notes.update(branch_lead_notes(dr_reports, dr_notes, {"wrong_answer": 8}, _show))
    expect.equal("an unseen lead is read from its own branch, and only there", sorted(set(dr_notes) - set(notes)), ["PB77"])


def case_027_drain_only_the_finishers_of_started_work(expect):
    global DR_UNLANDED, dr_plan, drain, pd
    dr_notes["PB79"] = Note("PB79", "open", "process", "PB79 — carried", [], "", 0)
    # unlanded_branch_notes is unfiltered (every id the subjects name); plan() keeps its leads (review R2-1)
    DR_UNLANDED = {"owner-task": ["PB77"], "worktree-wf_dr-6": ["PB11"], "worktree-wf_dr-7": ["PB9"]}


    def dr_plan(drain):
        return pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                       budget_points=100, drain=drain, carrier=DR_CARRIER.get)


    pd = dr_plan(True)
    expect.equal("drain: only the finishers of started work, two whose claims meet as ONE group (kb/Work PB4396)",
          sorted(sorted(g.notes) for g in pd["groups"]), [["PB6", "PB7"], ["PB77"], ["PB79"], ["PB9"]])
    expect.equal("drain: every group is a finisher whose pred says DRAIN",
          all(g.kind == "finisher" and "DRAIN" in g.pred for g in pd["groups"]), True)
    expect.equal("drain: a SPLIT branch another DONE branch merged waits, with its carrier named",
          {i: "worktree-wf_dr-2" in pd["waiting"].get(i, "") for i in ("PB5", "PB12", "PB11")},
          {"PB5": True, "PB12": True, "PB11": True})
    expect.equal("drain: a SPLIT whose branch landed left only unstarted work", "never started" in pd["waiting"].get("PB10", ""), True)
    expect.equal("drain: the clusters' notes wait, counted", ("PB2" in pd["drain_not_started"], "PB4" in pd["drain_not_started"]),
          (True, True))
    expect.equal("an unseen lead's branch with a finisher is no judgment row", pd["unlanded_without_note"], [])
    # kb/Work PB3311 hole 1: a FINDINGS report's branch with a live owner of its OWN (prune_worktrees.owned: a resumed
    # branch whose tip moved past the reviewed head, a live agent's worktree, a dispatch out) is being finished: no second
    # finisher, in either mode, for an open note (PB7, branch wf_dr-2) and an unseen lead (PB79, wf_dr-5) alike, with the
    # reason named. The same plan with neither branch owned (nobody working on it: the stamp covers the tip) plans both.
    for drain in (False, True):
        for owned, planned in (({"worktree-wf_dr-2", "worktree-wf_dr-5"}, False), (set(), True)):
            pf1 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                          budget_points=100, drain=drain, carrier=DR_CARRIER.get, owned=owned)
            expect.equal(f"{'drain' if drain else 'normal'}: a FINDINGS branch {'not ' if planned else ''}owned "
                  f"{'plans' if planned else 'waits, no'} finisher (PB3311)",
                  [(any(i in g.notes for g in pf1["groups"]), "IN FLIGHT" in pf1["waiting"].get(i, ""))
                   for i in ("PB7", "PB79")], [(planned, not planned)] * 2)
            if not planned:
                expect.equal(f"{'drain' if drain else 'normal'}: the waiting reason names the branch and the answers in progress",
                      all(w in pf1["waiting"].get("PB7", "") for w in ("worktree-wf_dr-2", "being answered")), True)
        # a SPLIT report's branch IN FLIGHT is a different arm (kb/Work PB2974): untouched by this rule
        pf2 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                      budget_points=100, drain=False, carrier=DR_CARRIER.get, owned={"worktree-wf_dr-3"})
        expect.equal("a SPLIT report's branch IN FLIGHT is the SPLIT arm's (PB2974), not this rule's",
              any("PB6" in g.notes for g in pf2["groups"]), True)


def case_028_block(expect):
    global drain, head
    # kb/Work PB3394 (wave 1127's A, B and C): a FINDINGS stamp makes a finisher only when it COVERS the branch tip
    # (`review_covers`: prune_worktrees.Coverage.review over the survey in plan_wave.main, the survey's own rule; the
    # end-to-end case on a real git history is prune_worktrees.py --self-test's findings-moved and findings-stamped). A stamp
    # at an older head is WAITING FOR REVIEW, named in `waiting`, never a group, in either mode. Without a survey the
    # report's own HEAD stands for the tip: a report rewritten after the review (HEAD past the stamp) is not covered.
    SV = TMP / "reports-stale-stamp"
    SV.mkdir()
    for head, stamp in (("314d06415aa", "c168dd762798"), ("c168dd762798", "c168dd762798")):
        (SV / "w1127a-PB7-report.md").write_text(
            f"Status: DONE\n**Review:** FINDINGS 1 w1127a-review-r1 {stamp} E:/s/review-w1127-a.jsonl\n"
            f"Branch: worktree-wf_dr-2\nHEAD: {head}\n\n## X\n", encoding="utf-8", newline="\n")
        for drain in (False, True):
            for covers in (None, lambda _r: False, lambda _r: True):
                covered = covers(None) if covers else head == stamp
                psv = pw.plan(dr_notes, CLUSTERS, HALF, load_reports(SV), lambda b: DR_CLASS.get(b, "ABSENT"), {}, RULES,
                              budget_points=100, drain=drain, review_covers=covers)
                g7 = [g.pred for g in psv["groups"] if "PB7" in g.notes]
                expect.equal(f"PB3394: HEAD {head[:4]}, stamp {stamp[:4]}, {'drain' if drain else 'normal'}, review_covers "
                      f"{'default' if covers is None else covered}: {'a finisher' if covered else 'WAITING FOR REVIEW'}",
                      (bool(g7) and g7[0].startswith("FINISH-FIRST: the branch review of w1127a"),
                       psv["waiting"].get("PB7", "").startswith("WAITING FOR REVIEW")),
                      (covered, not covered))


def case_029_block(expect):
    global g79
    _dr_tips = {"worktree-wf_dr-2": "2020202bb", "worktree-wf_dr-5": "6060606ff"}
    _dr_rep = {r.lead: r for r in report_index(DR, lambda _i: False)}
    # The one ancestry fact of this scenario, as contained() reads it (the survey's graph, kb/Work PB3582): 6060606ff is a
    # commit of 2020202bb's history, and no origin/main exists.
    _dr_graph = git_batch.Ancestry.of({"2020202bb": ("6060606ff",), "6060606ff": ()})
    for carrier_state, pb7_planned in (("IN FLIGHT", False), ("WAITING TO LAND", True)):
        rows = [prune_worktrees.Row(b, "UNLANDED", [], [], 1, None, None, 0, 999.0, "", report=_dr_rep[lead], state=st,
                                    tip=_dr_tips[b])
                for b, lead, st in (("worktree-wf_dr-2", "PB7", carrier_state), ("worktree-wf_dr-5", "PB79", "DECISION"))]
        prune_worktrees.contained(rows, _dr_graph, [], None)
        survey_owned = prune_worktrees.owned(rows)
        pr = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                     budget_points=100, carrier=DR_CARRIER.get, owned=survey_owned)
        g79 = next((g for g in pr["groups"] if g.notes == ["PB79"]), None)
        expect.equal(f"R1-1: a FINDINGS branch contained in a {carrier_state} carrier reads IN FLIGHT, is not owned, and gets "
              f"its finisher naming the carrier; the carrier's own note {'is planned' if pb7_planned else 'waits'}",
              (rows[1].state, rows[1].carrier, "worktree-wf_dr-5" in survey_owned, "PB79" in pr["waiting"],
               bool(g79) and "contained in worktree-wf_dr-2" in g79.pred,
               any("PB7" in g.notes for g in pr["groups"]), "being answered" in pr["waiting"].get("PB7", "")),
              ("IN FLIGHT", "worktree-wf_dr-2", False, False, True, pb7_planned, not pb7_planned))


def case_030_pb3404_a_carried_findings_branch_whose_c(expect):
    global pa
    # PB3404, ONE FINDINGS CASE, ONE OWNER: when the carrier's own CLEAN review answers the carried branch's findings
    # (prune_worktrees.answered_by_carrier: the uncapped case, whose findings the successor's review took over), the planner
    # plans no second finisher for them; it says who answers, and plans one again if the carrier does not land
    pa = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                 budget_points=100, carrier=DR_CARRIER.get, owned=set(),
                 answered=lambda rep: "worktree-wf_dr-2" if rep.branch == "worktree-wf_dr-5" else None)
    expect.equal("PB3404: a carried FINDINGS branch whose carrier's review answers them gets no finisher and names who does",
          (any(g.notes == ["PB79"] for g in pa["groups"]), "answered by worktree-wf_dr-2's own review" in pa["waiting"].get("PB79", ""),
           any("PB77" in g.notes for g in pa["groups"])), (False, True, True))


def case_031_every_mode_an_unseen_lead_s_findings_get(expect):
    global g79, mode, pn
    pn = dr_plan(False)
    expect.equal("every mode: an unseen lead's FINDINGS gets its finisher", any(g.notes == ["PB77"] for g in pn["groups"]), True)
    # R1-2: PB79's branch is contained in worktree-wf_dr-2, PB7's DONE branch, whose report never answers PB79's findings:
    # the carried FINDINGS branch still gets its finisher (the drop was silent: no group, no waiting entry, no judgment row),
    # which merges PB79's own branch only and is told the carrier may land its commits first.
    for mode, pm in (("every mode", pn), ("drain", pd)):
        g79 = next((g for g in pm["groups"] if g.notes == ["PB79"]), None)
        expect.equal(f"{mode}: a carried unseen-lead FINDINGS gets its finisher, its pred naming the carrier (R1-2)",
              bool(g79) and ("git merge worktree-wf_dr-5" in g79.pred, "contained in worktree-wf_dr-2" in g79.pred,
                             "never worktree-wf_dr-2" in g79.pred, "review_findings.py show E:/s/r79.jsonl" in g79.pred),
              (True, True, True, True))


def case_032_every_mode_a_carried_split_s_finisher_me(expect):
    g5n = next((g for g in pn["groups"] if "PB5" in g.notes), None)
    expect.equal("every mode: a carried SPLIT's finisher merges its carrier, never cherry-picks it",
          bool(g5n) and "git merge worktree-wf_dr-2" in g5n.pred and "never cherry-pick worktree-wf_dr-1" in g5n.pred,
          True)
    expect.equal("without --drain nothing waits on the drain", (pn["drain_not_started"], any("DRAIN" in g.pred for g in pn["groups"])),
          ([], False))


def case_033_block(expect):
    global drain, mode, want
    # R1-1: the review finisher of owner-task's FINDINGS works on its OWN branch, and its later report about the same
    # unseen lead supersedes the FINDINGS report, as for an open note: a SPLIT is resumed from its own branch (never the
    # findings answered again from scratch, its branch never a judgment row); an unreviewed DONE awaits landing; a newer
    # FINDINGS stamp on the finisher's branch gets the next finisher from that report.
    SU = TMP / "reports-successor"
    SU.mkdir()
    (SU / "w1074z-PB77-report.md").write_text(
        "Status: DONE\n**Review:** FINDINGS 1 enqueue-review-owner-task-r1 5050505ee E:/s/review-owner-task.jsonl\n"
        "Branch: owner-task\nHEAD: 5050505ee\n\n## X\n", encoding="utf-8", newline="\n")
    SU_CLASS = {"owner-task": "UNLANDED", "worktree-wf_f-1": "UNLANDED"}
    for successor, carried, want in (
            ("Status: SPLIT\nBranch: worktree-wf_f-1\nHEAD: 7070707aa", {}, "SPLIT"),
            ("Status: DONE\nBranch: worktree-wf_f-1\nHEAD: 7070707aa", {"owner-task": "worktree-wf_f-1"}, "DONE"),
            ("Status: DONE\n**Review:** FINDINGS 2 enqueue-review-wf_f-1-r1 7070707aa E:/s/review-wf_f-1.jsonl\n"
             "Branch: worktree-wf_f-1\nHEAD: 7070707aa", {"owner-task": "worktree-wf_f-1"}, "FINDINGS")):
        (SU / "w1075a-PB77-report.md").write_text(successor + "\n\n## X\n", encoding="utf-8", newline="\n")
        for drain in (False, True):
            ps = pw.plan(dr_notes, CLUSTERS, HALF, load_reports(SU), lambda b: SU_CLASS.get(b, "ABSENT"),
                         {"owner-task": ["PB77"], "worktree-wf_f-1": ["PB77"]}, RULES, budget_points=100, drain=drain,
                         carrier=carried.get)
            g77 = [g for g in ps["groups"] if "PB77" in g.notes]
            mode = f"{want} successor{' (drain)' if drain else ''}"
            if want == "SPLIT":
                expect.equal(f"{mode}: the finisher resumes the successor's branch, never the stale FINDINGS report (R1-1)",
                      [("w1075a" in g.pred, "worktree-wf_f-1" in g.pred, "review-owner-task.jsonl" in g.pred) for g in g77],
                      [(True, True, False)])
                # R2-1: the superseded FINDINGS branch names the unseen lead, which a newer report covers: skipped, as an
                # open note's stale branch is, never a judgment row (unlanded_branch_notes once kept only open notes)
                expect.equal(f"{mode}: neither the successor's nor the superseded FINDINGS branch is a judgment row (R2-1)",
                      [b for b in ("worktree-wf_f-1", "owner-task") if b in ps["unlanded_without_note"]], [])
            elif want == "DONE":
                expect.equal(f"{mode}: an unreviewed successor awaits landing, no finisher (R1-1)",
                      (g77, ps["awaiting_landing"].get("worktree-wf_f-1")), ([], ["PB77"]))
            else:
                expect.equal(f"{mode}: the successor's own FINDINGS gets the next finisher (R1-1)",
                      [("review-wf_f-1.jsonl" in g.pred, "git merge worktree-wf_f-1" in g.pred) for g in g77], [(True, True)])


def case_034_held_unanswered_groups_only_terminal_not(expect):
    global HD, name, nid, when
    # 6c. A NOTE AN UNREPORTED DISPATCH HOLDS IS NOT PLANNED AGAIN, IN EITHER LANE (kb/Work PB2806): the fix lane read no
    #     ledger, and wave 1044 re-planned seven of the eight groups another session's recorded, unlaunched wave 1045 held.
    #     A report written since the dispatch answers it (steps 1 and 2 govern), a terminal note is never held, and a
    #     retired hand entry (kb/Work PB3186) is no dispatch, however fresh.
    HD = TMP / "held"
    HD_REPORTS = HD / "reports"
    HD_REPORTS.mkdir(parents=True)
    record_dispatch(HD, "1045", [{"letter": "a", "notes": ["PB5", "PB2"], "files": ["src/X.cs"]},
                                    {"letter": "b", "notes": ["PB8"], "files": ["src/Y.cs"]},
                                    {"letter": "c", "notes": ["PB9"], "files": ["src/Z.cs"]}], lambda i: False)
    HD_AT = {g["letter"]: g["at"] for g in coord.read_json(HD / coord.DISPATCH_LEDGER, {})["groups"]}
    for name, nid, when in (("w1045b-PB8-report.md", "PB8", HD_AT["b"] + 60), ("w1031c-PB9-report.md", "PB9", HD_AT["c"] - 3600)):
        (HD_REPORTS / name).write_text(f"DONE\n# {nid} group\n**branch:** `worktree-wf_held-{nid}` · **HEAD:** `5555555ee`\n\n"
                                       "## Reproduced?\n", encoding="utf-8", newline="\n")
        __import__("os").utime(HD_REPORTS / name, (when, when))
    LEDGER_DOC = coord.read_json(HD / coord.DISPATCH_LEDGER, {})
    LEDGER_DOC["groups"].append({"wave": "hand", "letter": "", "notes": ["PB10"], "files": ["src/W.cs"], "hand": True,
                                 "at": HD_AT["a"]})
    coord.write_json(HD / coord.DISPATCH_LEDGER, LEDGER_DOC)
    expect.equal("held: unanswered groups only, terminal notes and retired hand entries never",
          held_by_dispatch(HD, load_reports(HD_REPORTS), lambda i: i == "PB2"),
          {"PB5": "dispatch w1045a", "PB9": "dispatch w1045c"})


def case_035_a_held_note_is_planned_in_no_group_and_w(expect):
    global drain, mode
    p6 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
                 held={"PB5": "dispatch w1045a"})
    expect.equal("a held note is planned in no group and waits naming its dispatch",
          (any("PB5" in g.notes for g in p6["groups"]), "w1045a" in p6["waiting"].get("PB5", "")), (False, True))
    expect.equal("its cluster's other notes are still planned", any("PB2" in g.notes for g in p6["groups"]), True)
    # R2-1 (kb/Work PB3150): an UNSEEN lead (PB77, process_only, its FINDINGS on owner-task) is held exactly as an open note
    # is. Wave 1075 dispatched its review finisher, which has not reported: the next plan, normal or --drain, must not
    # plan a second finisher on the same findings (the hold once read `open_ids`, so PB77 was planned again).
    for drain in (False, True):
        ph = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                     budget_points=100, drain=drain, carrier=DR_CARRIER.get, held={"PB77": "dispatch w1075a", "PB7": "dispatch w1075b"})
        mode = "drain" if drain else "normal"
        expect.equal(f"{mode}: a held unseen lead and a held open note both wait naming their dispatch, neither planned (R2-1)",
              [(any(i in g.notes for g in ph["groups"]), f"held by dispatch {w}" in ph["waiting"].get(i, ""))
               for i, w in (("PB77", "w1075a"), ("PB7", "w1075b"))], [(False, True), (False, True)])
        expect.equal(f"{mode}: the held unseen lead's branch is no judgment row (R2-1)", "owner-task" in ph["unlanded_without_note"],
              False)


def case_036_an_unseen_lead_that_needs_the_owner_s_fr(expect):
    # The frontier-model hold reads the leads too: an unseen lead whose `frontier_model:` names Mythos waits.
    fm_notes = dict(dr_notes)
    fm_notes["PB77"] = dataclasses.replace(dr_notes["PB77"], frontier_model="mythos")
    pf = pw.plan(fm_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                 budget_points=100, carrier=DR_CARRIER.get)
    expect.equal("an unseen lead that needs the owner's frontier-model approval waits (R2-1)",
          (any("PB77" in g.notes for g in pf["groups"]), "runs on mythos" in pf["waiting"].get("PB77", "")), (False, True))


def case_037_a_body_that_names_mythos_beside_an_appro(expect):
    # THE FIELD DECIDES, NEVER THE PROSE (kb/Work PB3941): PB3869's body recorded the owner's approval "in chat (Mythos session
    # ef566665): \"Approve fixing PB3869\"", and the body regex that decided the hold until then held that owner-approved
    # wrong-answer fix out of every wave. A note with no field is planned exactly as it would be with no such prose.
    fp_notes = dict(dr_notes)
    fp_notes["PB77"] = dataclasses.replace(dr_notes["PB77"], body='The owner APPROVED the fix, in chat (Mythos session '
                                           'ef566665): "Approve fixing PB77; hand it to the operator." Fable reviewed it.')
    pfp, pbase = (pw.plan(n, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                          budget_points=100, carrier=DR_CARRIER.get) for n in (fp_notes, dr_notes))
    expect.equal("a body that names Mythos beside an approval, with no frontier_model field, holds nothing (PB3941)",
          ([g.notes for g in pfp["groups"]], pfp["waiting"].get("PB77")), ([g.notes for g in pbase["groups"]],
                                                                          pbase["waiting"].get("PB77")))


def case_038_a_branch_naming_no_lead_is_a_judgment_ro(expect):
    # unlanded_branch_notes is unfiltered: an id that is no lead (a landed note, a typo) leaves its branch a judgment row
    pj = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"),
                 {**DR_UNLANDED, "worktree-wf_dr-8": ["PB4242"]}, RULES, budget_points=100, carrier=DR_CARRIER.get)
    expect.equal("a branch naming no lead is a judgment row, its non-lead ids never planned",
          ("worktree-wf_dr-8" in pj["unlanded_without_note"], any("PB4242" in g.notes for g in pj["groups"])), (True, False))


def case_039_the_lead_of_a_split_report_is_read_from(expect):
    global first, hdr, name, sp_notes, sp_reports
    # 6b''''. STARTED WORK IS FINISHED WHICHEVER LANE PLANNED IT (kb/Work PB3699). Wave 20261031's groups A, C and D (SPLIT
    #         reports on committed branches, CHECK and UNLANDED) answered campaign notes, which are `process_only`, so no
    #         cluster of the fix lane holds them: `plan_wave.py --drain` listed their branches as judgment rows and planned
    #         nothing. A SPLIT / NOT STARTED report on a branch that is not on main is started work in every mode.
    SP = TMP / "reports-unseen-split"
    SP.mkdir()
    for name, first, hdr in (
            ("w2031a-PB80-report.md", "SPLIT", "# PB80 group\n**branch:** `worktree-wf_sp-1` · **HEAD:** `1010101aa`"),
            ("w2031c-PB81-report.md", "SPLIT", "# PB81 group\n**branch:** `worktree-wf_sp-2` · **HEAD:** `2020202bb`"),
            ("w2031d-PB82-report.md", "NOT STARTED", "# PB82 group\n**branch:** `worktree-wf_sp-3` · **HEAD:** `3030303cc`"),
            ("w2031e-PB83-report.md", "SPLIT", "# PB83 group (its note landed)\n**branch:** `worktree-wf_sp-4` · **HEAD:** `4040404dd`"),
            ("w2031f-PB84-report.md", "SPLIT", "# PB84 group (its branch is on main)\n**branch:** `worktree-wf_sp-5` · **HEAD:** `5050505ee`"),
            ("w2031g-PB85-report.md", "SPLIT", "# PB85 group (its note exists only on the branch)\n**branch:** `worktree-wf_sp-6` · **HEAD:** `6060606ff`")):
        (SP / name).write_text(f"{first}\n{hdr}\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    sp_reports = load_reports(SP)
    sp_notes = {n: Note(n, st, "process", n, [], "", 0) for n, st in
                (("PB80", "open"), ("PB81", "open"), ("PB82", "open"), ("PB83", "landed"), ("PB84", "open"))}
    sp_notes.update(branch_lead_notes(sp_reports, sp_notes, {"wrong_answer": 8},
                                         lambda *a, cwd=None: _Shown(0, "---\nid: PB85\nstatus: open\narea: process\n---\n# PB85\n")
                                         if a[:2] == ("show", "worktree-wf_sp-6:kb/Work/PB85.md") else _Shown(128)))
    expect.equal("the lead of a SPLIT report is read from its own branch when main does not have it (PB3699)",
          sorted(set(sp_notes) - {"PB80", "PB81", "PB82", "PB83", "PB84"}), ["PB85"])


def case_040_block(expect):
    global drain
    SP_CLASS = {"worktree-wf_sp-1": "CHECK", "worktree-wf_sp-2": "UNLANDED", "worktree-wf_sp-3": "UNLANDED",
                "worktree-wf_sp-4": "UNLANDED", "worktree-wf_sp-5": "LANDED", "worktree-wf_sp-6": "UNLANDED"}
    SP_UNLANDED = {"worktree-wf_sp-1": ["PB80"], "worktree-wf_sp-2": ["PB81"], "worktree-wf_sp-3": ["PB82"],
                   "worktree-wf_sp-4": ["PB83"], "worktree-wf_sp-6": ["PB85"]}
    for drain in (False, True):
        sp = pw.plan(sp_notes, [], [], sp_reports, lambda b: SP_CLASS.get(b, "ABSENT"), SP_UNLANDED, RULES, budget_points=100,
                     drain=drain)
        expect.equal(f"{'drain' if drain else 'normal'}: a SPLIT or NOT STARTED report on a CHECK or UNLANDED branch gets a "
              f"FINISH-FIRST group though no cluster holds its note; a landed note or a landed branch gets none (PB3699)",
              (sorted((g.kind, tuple(g.notes), "FINISH-FIRST" in g.pred) for g in sp["groups"]),
               sp["unlanded_without_note"]),
              ([("finisher", ("PB80",), True), ("finisher", ("PB81",), True), ("finisher", ("PB82",), True),
                ("finisher", ("PB85",), True)], ["worktree-wf_sp-4"]))


def case_041_unlanded_branch_notes_names_every_id_an(expect):
    # ...and unlanded_branch_notes itself keeps every id the subjects name: it once kept only the open notes, so a
    # superseded FINDINGS branch naming only its unseen lead (owner-task: PB77) reached plan() empty, a judgment row.
    _saved = (prune_worktrees.main_names, prune_worktrees.classify)
    prune_worktrees.main_names = lambda: set()
    prune_worktrees.classify = lambda b, _names, _graph: ("UNLANDED",)
    # the subjects come from the ONE graph of the local branches (kb/Work PB3582), never a `git log` per branch
    _ub_graph = git_batch.Ancestry.of(
        {"o2": ("o1",), "o1": (), "x1": ()}, names={"owner-task": "o2", "worktree-wf_x": "x1"},
        subjects={"o2": "PB77: answer review R1-1", "o1": "PB77: an owner task", "x1": "PB2, PB11: fix"})
    try:
        expect.equal("unlanded_branch_notes names every id, an unseen lead's too (R2-1)",
              unlanded_branch_notes({"main", "owner-task", "worktree-wf_x"}, _ub_graph),
              {"owner-task": ["PB77"], "worktree-wf_x": ["PB2", "PB11"]})
    finally:
        prune_worktrees.main_names, prune_worktrees.classify = _saved


def case_042_pb2974_a_split_report_s_three_notes_are(expect):
    global UN, UN_CL, UN_NOTES, UN_REPORTS, un_plan
    # 6d. A BRANCH'S WORK IS ONE UNIT (kb/Work PB2974). Wave 1053's SPLIT reports each covered one branch carrying several
    #     notes; wave 1055's finishers were recorded in the dispatch ledger under only some of each branch's notes, and the
    #     wave 1056 plan made a second finisher, over the SAME branch, for the notes the ledger did not name. A report's
    #     notes now wait together while an unanswered dispatch holds any of them, through the real ledger and reader.
    UN = TMP / "unit"
    UN_REPORTS = UN / "reports"
    UN_REPORTS.mkdir(parents=True)
    UN_NOTES = {n: Note(n, "open", "tooling", f"{n} — a defect in tooling", [], "", 8) for n in
                ("PB301", "PB302", "PB303", "PB304", "PB311", "PB312", "PB313")}
    UN_CL = [cl("scripts/a.py", "PB301", "PB302", "PB303"), cl("scripts/b.py", "PB304", "PB311", "PB312", "PB313")]
    record_dispatch(UN, "1053", [{"letter": "b", "notes": ["PB301", "PB302", "PB303"], "files": ["scripts/a.py"]}],
                       lambda i: False)
    (UN_REPORTS / "w1053b-PB301-report.md").write_text(
        "SPLIT\n# w1053b\n**branch:** `worktree-wf_un-1` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    returned(UN_REPORTS)


    def un_plan(held, unlanded=None, drain=False, reports_dir=UN_REPORTS):
        return pw.plan(UN_NOTES, UN_CL, [], load_reports(reports_dir, UN), lambda b: "UNLANDED", unlanded or {}, RULES,
                       budget_points=100, held=held, drain=drain)


    un_all = un_plan({})
    expect.equal("PB2974: a SPLIT report's three notes are ONE finisher over its branch",
          [(g.kind, g.notes, g.continues) for g in un_all["groups"] if "PB301" in g.notes],
          [("finisher", ["PB301", "PB302", "PB303"], ["worktree-wf_un-1"])])


def case_043_pb2974_the_ledger_holds_the_one_note_its(expect):
    global drain, mode
    # wave 1055's finisher: the ledger names PB302 alone (a chunk of the report's notes), and it has not reported
    record_dispatch(UN, "1055", [{"letter": "a", "notes": ["PB302"], "files": ["scripts/a.py"]}], lambda i: False)
    un_held = held_by_dispatch(UN, load_reports(UN_REPORTS, UN), lambda i: False)
    expect.equal("PB2974: the ledger holds the one note its dispatch named", un_held, {"PB302": "dispatch w1055a"})
    for drain in (False, True):
        mode = "drain" if drain else "normal"
        un_p = un_plan(un_held, drain=drain)
        expect.equal(f"PB2974 {mode}: no finisher for the report's other notes while one of them is held",
              [g.notes for g in un_p["groups"] if {"PB301", "PB302", "PB303"} & set(g.notes)], [])
        expect.equal(f"PB2974 {mode}: the held note names its dispatch, the others name the held note and its dispatch",
              ("held by dispatch w1055a" in un_p["waiting"]["PB302"],
               [("PB302 of branch worktree-wf_un-1 is held by dispatch w1055a" in un_p["waiting"][i]) for i in ("PB301", "PB303")]),
              (True, [True, True]))
    # ... only a note the report owns holds it: another cluster's notes are planned as before
    expect.equal("PB2974: notes of no held report are planned beside it",
          sorted(i for g in un_plan(un_held)["groups"] for i in g.notes), ["PB304", "PB311", "PB312", "PB313"])


def case_044_pb2974_once_the_dispatch_has_reported_no(expect):
    # ... and the dispatch's own report releases the report's notes together (the finisher group takes all three again)
    (UN_REPORTS / "w1055a-PB302-report.md").write_text(
        "SPLIT\n# w1055a\n**branch:** `worktree-wf_un-2` · **HEAD:** `4444444dd`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    returned(UN_REPORTS)
    expect.equal("PB2974: once the dispatch has reported nothing is held", held_by_dispatch(UN, load_reports(UN_REPORTS, UN),
                                                                                    lambda i: False), {})


def case_045_pb2974_an_unlanded_branch_with_a_held_no(expect):
    global UB
    # the UNLANDED-branch arm: a branch whose commit subjects name three notes is one unit as well, and past the cap too
    UB = {"worktree-wf_ub-1": ["PB311", "PB312", "PB313"]}
    ub_p = un_plan({"PB312": "dispatch w1055b"}, unlanded=UB, reports_dir=UN / "none")
    expect.equal("PB2974: an unlanded branch with a held note plans no finisher for its other notes",
          ([g.notes for g in ub_p["groups"] if {"PB311", "PB313"} & set(g.notes)],
           [("PB312 of branch worktree-wf_ub-1 is held by dispatch w1055b" in ub_p["waiting"][i]) for i in ("PB311", "PB313")]),
          ([], [True, True]))


def case_046_pb2974_an_unlanded_branch_s_notes_past_t(expect):
    UB_MANY = {f"PB{n}": Note(f"PB{n}", "open", "tooling", f"PB{n} — a defect in tooling", [], "", 8)
               for n in range(400, 400 + notes_cap(RULES) + 2)}
    ub_ids = sorted(UB_MANY, key=pw.work.id_order)
    ub_many = pw.plan(UB_MANY, [cl("scripts/c.py", *ub_ids)], [], [], lambda b: "UNLANDED", {"worktree-wf_ub-2": ub_ids}, RULES,
                      budget_points=1000)
    expect.equal("PB2974: an unlanded branch's notes past the cap stay ONE finisher over the branch, never a cluster beside it",
          [(g.kind, g.notes, g.continues) for g in ub_many["groups"]], [("finisher", ub_ids, ["worktree-wf_ub-2"])])


def case_047_pb2974_r1_1_a_half_note_and_an_open_note(expect):
    global BU_X, bu_plan
    # THE BRANCH, NOT THE ARM, IS THE UNIT (branch review R1-1): a half note and an open note on one unlanded branch, and a
    # SPLIT report beside unreported notes its branch carries, are ONE finisher over the branch, and one held note holds all
    BU_NOTES = {n: Note(n, "open", "tooling", f"{n} — a defect in tooling", [], "", 8)
                for n in ("PB501", "PB502", "PB601", "PB602", "PB605")}
    BU_CL = [cl("scripts/d.py", "PB502")]
    BU_HALF = [cl("scripts/d.py", "PB501")]
    BU_X = {"worktree-wf_x-1": ["PB501", "PB502"]}


    def bu_plan(held, unlanded, reports=(), clusters=BU_CL, half=BU_HALF, drain=False):
        return pw.plan(BU_NOTES, clusters, half, list(reports), lambda b: "UNLANDED", unlanded, RULES, budget_points=100,
                       held=held, drain=drain)


    expect.equal("PB2974 (R1-1): a half note and an open note of one unlanded branch are ONE finisher over it",
          [(g.kind, g.notes, g.continues) for g in bu_plan({}, BU_X)["groups"]],
          [("finisher", ["PB501", "PB502"], ["worktree-wf_x-1"])])


def case_048_pb2974_r1_1_the_half_note_waits_with_the(expect):
    bu_h = bu_plan({"PB502": "dispatch w1055a"}, BU_X)
    expect.equal("PB2974 (R1-1): the half note waits with the branch's held note, naming its dispatch",
          ([g.notes for g in bu_h["groups"]], "PB502 of branch worktree-wf_x-1 is held by dispatch w1055a" in bu_h["waiting"]["PB501"]),
          ([], True))
    expect.equal("PB2974 (R1-1): a half note of a branch no note of which is held is a finisher with the half record and the branch",
          [("FINISH-FIRST: PB501 is `status: half`" in g.pred, "worktree-wf_x-1" in g.pred) for g in bu_plan({}, BU_X)["groups"]],
          [(True, True)])


def case_049_pb2974_r1_1_a_report_s_finisher_takes_th(expect):
    global BU_CL2, BU_Y, bu_reps
    BU_REP = UN / "bu-reports"
    BU_REP.mkdir()
    (BU_REP / "w1056a-PB601-report.md").write_text(
        "SPLIT\n# w1056a\n**branch:** `worktree-wf_y-1` · **HEAD:** `5555555ee`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    returned(BU_REP)
    BU_Y = {"worktree-wf_y-1": ["PB601", "PB602", "PB605"]}
    BU_CL2 = [cl("scripts/e.py", "PB601", "PB602", "PB605")]
    bu_reps = load_reports(BU_REP, UN)
    bu_r = bu_plan({}, BU_Y, bu_reps, BU_CL2, [])
    expect.equal("PB2974 (R1-1): a report's finisher takes the unreported notes its own branch carries, ONE finisher",
          [(g.kind, g.notes, g.continues, "PB602, PB605" in g.pred) for g in bu_r["groups"]],
          [("finisher", ["PB601", "PB602", "PB605"], ["worktree-wf_y-1"], True)])


def case_050_pb2974_r1_1_and_a_held_unreported_note_o(expect):
    bu_rh = bu_plan({"PB602": "dispatch w1055a"}, BU_Y, bu_reps, BU_CL2, [])
    expect.equal("PB2974 (R1-1): ... and a held unreported note of that branch holds the report's note too",
          ([g.notes for g in bu_rh["groups"]], "PB602 of branch worktree-wf_y-1 is held by dispatch w1055a" in bu_rh["waiting"]["PB601"],
           "PB602 of branch worktree-wf_y-1 is held by dispatch w1055a" in bu_rh["waiting"]["PB605"]), ([], True, True))


def case_051_pb2974_r1_1_the_drain_plans_the_report_s(expect):
    bu_rd = bu_plan({}, BU_Y, bu_reps, BU_CL2, [], drain=True)
    expect.equal("PB2974 (R1-1): the drain plans the report's finisher over the branch's notes as well",
          [g.notes for g in bu_rd["groups"]], [["PB601", "PB602", "PB605"]])


def case_052_renders_through_the_template(expect):
    global CFG77, FILES, e, gjson, specs
    # 7. the group JSON renders through the real template and passes check_practices' same-file rule
    gjson = [as_group_json(g, "77", notes, "COBOL0001-COBOL0003") for g in p["groups"]]
    CFG77 = {"wave": "77", "base": "abc", "scratch": str(TMP), "stop_file": str(coord.fleet_stop(TMP, "w77"))}
    FILES = lambda g: sorted(mds.group_claim(g, lambda ids: Claim()).files)  # noqa: E731  (a fixture note declares no files)
    try:
        specs = [mds.render(CFG77, g, FILES(g)) for g in gjson]
        rendered = True
    except (KeyError, IndexError) as e:
        specs, rendered = [], f"template placeholder missing: {e}"
    expect.equal("renders through the template", rendered, True)


def case_053_a_rendered_spec_names_the_global_stop(expect):
    global spec_file
    # 7b. THE STOP IS SCOPED (kb/Work PB2483): every spec names the owner's global stop and this fleet's own, and passes
    #     check_practices' rendered-spec rule; a groups file without a fleet stop renders nothing
    spec_file = TMP / "msg-w77-x.txt"
    spec_file.write_text(specs[0] if specs else "", encoding="utf-8", newline="\n")
    expect.equal("a rendered spec names the global stop", str(coord.global_stop()) in (specs or [""])[0], True)
    expect.equal("a rendered spec names the fleet's own stop", str(coord.fleet_stop(TMP, "w77")) in (specs or [""])[0], True)
    expect.equal("a rendered spec passes check_practices", [m for m in check_practices.check(spec_file, check_practices.SPEC)
                                                     if "report" not in m], [])


def case_054_a_rendered_spec_s_report_is_in_coord_rep(expect):
    # 7c. THE ONE REPORTS DIRECTORY (kb/Work PB2980): the report path is coord.reports_dir() whatever the wave's scratch
    #     (here TMP), and check_practices refuses a spec that writes its report under the scratch instead
    report_line = next((l for l in (specs or [""])[0].splitlines() if l.startswith("Report:")), "")
    expect.equal("a rendered spec's report is in coord.reports_dir(), not the wave's scratch",
          (str(coord.reports_dir()) in report_line, str(TMP) in report_line), (True, False))
    expect.equal("check_practices accepts the rendered report path",
          bool(re.search(check_practices.RENDERED_REPORT, report_line)), True)
    expect.equal("check_practices refuses a report under the wave's scratch",
          bool(re.search(check_practices.RENDERED_REPORT, f"Report: {TMP}\\reports\\w77a-PB1-report.md")), False)


def case_055_every_rendered_scratch_line_is_wave_scra(expect):
    # 7c1. AN AGENT WRITES IN ITS OWN DIRECTORY (kb/Work PB4466, MANDATORY-PRACTICES P16). Expected from the rule: the rendered
    #      `Scratch:` line is the wave's scratch plus the group's slug, so two groups of one wave never share one; check_practices
    #      refuses a line that is the bare wave scratch or lacks the P16 statement, and refuses an instruction surface that sends
    #      an agent to the harness's session scratchpad (the real tree has none; each arm is seen refused on a plant).
    scratch_lines = [next((l for l in s.splitlines() if l.startswith("Scratch:")), "") for s in specs]
    expect.equal("every rendered Scratch line is <wave scratch>\\<slug>\\, one directory per group",
          ([l.split()[1] for l in scratch_lines], len({l.split()[1] for l in scratch_lines}) == len(scratch_lines)),
          ([f"{TMP}\\{g['slug']}\\" for g in gjson], True))
    expect.equal("check_practices accepts a rendered Scratch line",
          [bool(re.search(check_practices.RENDERED_SCRATCH, l)) for l in scratch_lines], [True] * len(scratch_lines))
    expect.equal("... and refuses the bare wave scratch, a line without the P16 statement, and a missing line",
          [bool(re.search(check_practices.RENDERED_SCRATCH, t)) for t in
           (f"Scratch:  {TMP}\\   (YOURS ALONE, MANDATORY-PRACTICES P16, x)", f"Scratch:  {TMP}\\w77a\\", "Report: x")], [False] * 3)
    expect.equal("the template states the line the renderer fills",
          bool(re.search(check_practices.TEMPLATE_SCRATCH, mds.TEMPLATE.read_text(encoding="utf-8"))), True)
    expect.equal("... a bare scratchpad line is refused on a surface; the session scratchpad, a test file and a code outside the surfaces pass",
          ([len(check_practices.bare_scratchpad_lines(".claude/agents/x.md", t)) for t in
            ("write it under the scratchpad", "Write to the Scratchpad", "never the session scratchpad")],
           len(check_practices.bare_scratchpad_lines("scripts/hooks/x.py", "(into the scratchpad)")),
           check_practices.bare_scratchpad_lines("scripts/hooks/test_x.py", "the scratchpad"),
           check_practices.bare_scratchpad_lines("scripts/spec/x.py", "the scratchpad")),
          ([1, 1, 0], 1, [], []))
    expect.equal("... the hook that refuses a call naming the session scratchpad is exempt: it names the folder to refuse it",
          check_practices.bare_scratchpad_lines("scripts/hooks/own_scratch.py", "names the scratchpad"), [])


def case_056_the_review_lane_s_prompts_are_pinned_to(expect):
    _lane = (coord.REPO / "scripts" / "orchestrator" / "review_lane.py").read_text(encoding="utf-8")
    _lane_pins = check_practices.CODE_PROMPTS["scripts/orchestrator/review_lane.py"]
    expect.equal("the review lane's prompts are pinned to the rolling wave's reviewer wording (PB4473): the real source passes, and a lane "
          "prompt that drops the scratch sentence, or one of its two callers, is refused",
          [all(re.search(p, t) for p in _lane_pins) for t in
           (_lane, _lane.replace("YOUR SCRATCH DIRECTORY is", "YOUR FOLDER is"),
            _lane.replace("+ scratch_line(r) +", "+", 1), _lane.replace("            + scratch_line(r)\n", "", 1))],
          [True, False, False, False])
    try:
        mds.render({k: v for k, v in CFG77.items() if k != "stop_file"}, gjson[0], FILES(gjson[0]))
        expect.equal("no stop_file, no spec", "rendered", "KeyError")
    except KeyError:
        expect.equal("no stop_file, no spec", True, True)
    expect.equal("a fleet stop is STOP-<scope>", coord.fleet_stop(pathlib.Path("S"), "loop").name, "STOP-loop")


def case_057_a_rendered_spec_s_turn_cap_is_the_implem(expect):
    global IMPL_CAP, spec0
    # 7c2. THE TURN CAP HAS ONE HOME (kb/Work PB3701). Expected from the rule: the implementer's cap is the `maxTurns` of
    #      .claude/agents/cobol-implementer.md (the number the harness enforces); a rendered spec says exactly it, in both
    #      places the template names it, and check_practices refuses a spec that says another, a statement elsewhere that
    #      differs from its role's maxTurns, a statement whose text is gone, and a spec template that types a number.
    IMPL_CAP = coord.agent_turn_cap("cobol-implementer")
    spec0 = (specs or [""])[0]
    expect.equal("a rendered spec's turn cap is the implementer's maxTurns, at both places",
          (f"\nTurn cap {IMPL_CAP} — at the cap" in spec0, f"of the {IMPL_CAP}-turn cap" in spec0, "{turn_cap}" in spec0),
          (True, True, False))
    expect.equal("check_practices accepts a rendered spec whose Turn cap is the maxTurns",
          [m for m in check_practices.check(spec_file, [check_practices.RENDERED_TURN_CAP]) if "Turn cap" in m],
          [])


def case_058_and_refuses_it_when_it_says_220_the_reti(expect):
    spec_file.write_text(spec0.replace(f"Turn cap {IMPL_CAP} —", f"Turn cap {IMPL_CAP - 180} —"), encoding="utf-8", newline="\n")
    expect.equal("... and refuses it when it says 220 (the retired figure)",
          len(check_practices.check(spec_file, [check_practices.RENDERED_TURN_CAP])), 1)


def case_059_a_rendered_spec_states_no_sentence_twice(expect):
    spec_file.write_text(spec0, encoding="utf-8", newline="\n")
    # 7c3. A SPEC SAYS EACH RULE ONCE (kb/Work PB3777). Expected from the rule: the implementer's instruction file is ONE file
    #      whose paragraphs state each rule once, so a rendered spec repeats no sentence of SENTENCE_MIN characters or more and
    #      check_practices refuses a spec that does (the spec and brief it replaced carried the checkpoint protocol, the gate,
    #      the DEVLOG entry, I9, the citation rule and the STOP rule twice).
    expect.equal("a rendered spec states no sentence twice", once_rule.repeated_sentences(spec0), [])


def case_060_and_the_sentence_this_test_repeats_is_on(expect):
    global twice
    twice = "orientation (grep/read) was 46 % of every implementer's tokens in waves 45–57, re-deriving what earlier waves had already learned."
    expect.equal("... and the sentence this test repeats is one the spec states", twice in " ".join(spec0.split()), True)


def case_061_and_check_practices_refuses_a_spec_that(expect):
    spec_file.write_text(spec0 + "\n" + twice + "\n", encoding="utf-8", newline="\n")
    expect.equal("... and check_practices refuses a spec that states a sentence twice",
          [m.startswith("PB3777: states twice: orientation") for m in check_practices.check(spec_file, []) if "PB3777" in m], [True])


def case_062_pb4005_a_one_file_group_s_spec_ends_the(expect):
    spec_file.write_text(spec0, encoding="utf-8", newline="\n")
    # kb/Work PB4005: a one-file group ends both "THE GROUP IS ONE ROOT:" and "CODE SITES the notes name:" with its file, so the
    #      lone path is a "sentence" the spec states twice. It names data in two roles and states no rule: the once-check passes
    #      it (waves 20261751 and 20261755 rendered RED on PB4031's group, whose one file is this 62-character path).
    ONE = "src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs"
    g1 = as_group_json(dataclasses.replace(p["groups"][0], file=ONE, files=[ONE], members={}), "77", notes, "COBOL0001-COBOL0003")
    one_spec = TMP / "msg-w77-one.txt"
    one_spec.write_text(mds.render(CFG77, g1, FILES(g1)), encoding="utf-8", newline="\n")
    one_text = " ".join(one_spec.read_text(encoding="utf-8").split())
    expect.equal("PB4005: a one-file group's spec ends the root and the sites with its long path, and the once-check passes it",
          (len(ONE) + 1 >= once_rule.SENTENCE_MIN, f"ONE ROOT: {ONE}. " in one_text, f"notes name: {ONE}. " in one_text,
           [m for m in check_practices.check(one_spec, []) if "PB3777" in m]), (True, True, True, []))
    expect.equal("every statement of a cap in the tree matches its role's maxTurns", check_practices.turn_cap_copies(), [])


def case_063_a_copy_that_differs_a_copy_whose_text_is(expect):
    global p5, rel
    CAPREPO = TMP / "caprepo"
    for rel in {c[0] for c in check_practices.CAP_COPIES} | {f".claude/skills/workstream/templates/{b}" for b in check_practices.NUMBERLESS_CAP_BRIEFS} | set(check_practices.NUMBERLESS_CAP_FILES):
        (CAPREPO / rel).parent.mkdir(parents=True, exist_ok=True)
        (CAPREPO / rel).write_bytes((coord.REPO / rel).read_bytes())
    p5 = CAPREPO / ".claude/skills/workstream/templates/MANDATORY-PRACTICES.md"
    p5.write_text(p5.read_text(encoding="utf-8").replace(f"implementer {IMPL_CAP} (raised from", "implementer 220 (raised from"),
                  encoding="utf-8", newline="\n")
    fb = CAPREPO / ".claude/skills/workstream/templates/finisher-brief.md"
    fb.write_text(fb.read_text(encoding="utf-8").replace("Turn cap", "Cap of"), encoding="utf-8", newline="\n")
    brief = CAPREPO / ".claude/skills/workstream/templates/dispatch-spec-implementer.md"
    brief.write_text(brief.read_text(encoding="utf-8") + "\nA HARD CAP of 220 turns.\n", encoding="utf-8", newline="\n")
    rules_copy = CAPREPO / check_practices.NUMBERLESS_CAP_FILES[0]
    rules_copy.write_text(rules_copy.read_text(encoding="utf-8").replace("turn_budget_fraction of the implementer's turn cap", "turn_budget_fraction of the 220-turn cap"), encoding="utf-8", newline="\n")
    drift = check_practices.turn_cap_copies(CAPREPO)
    expect.equal("a copy that differs, a copy whose text is gone, a typed number in the spec template and a typed cap in the rules file are each refused",
          (any(f"states 220 turns for cobol-implementer, whose maxTurns is {IMPL_CAP}" in m for m in drift),
           any("finisher-brief.md: no statement matching" in m for m in drift),
           any("dispatch-spec-implementer.md: types a turn cap" in m for m in drift),
           any("model_rules.json: types a turn cap ('220-turn cap')" in m for m in drift), len(drift)), (True, True, True, True, 4))


def case_064_the_real_workstream_overlay_orders_no_ba(expect):
    # 7c4. THE LOOP UNITS LOAD THE OVERLAY ONLY (kb/Work PB3780). Expected from the rule: every unit loads the workstream overlay and
    #      re-reads it on each of about 45 turns, so it must not order its base skill's load, must stay under SKILL_MAX_BYTES and
    #      must send a reader only to reference files that exist (the base's by its full submodule path, its own by references/<file>);
    #      the overlays loaded for one job each (gate, review, spec-lookup) still name their base. Each arm is seen refused on a plant.
    WS_SKILL = (coord.REPO / ".claude/skills/workstream/SKILL.md").read_text(encoding="utf-8")
    expect.equal("the real workstream overlay orders no base load, fits its size and names only files that exist",
          check_practices.overlay_problems("workstream", WS_SKILL), [])
    expect.equal("... an overlay that orders its base's load by the Skill tool, or by a Read of its SKILL.md, is refused",
          [len(check_practices.overlay_problems("workstream", WS_SKILL + t)) for t in
           ("\nInvoke `brent-tools:agent-fleet` (Skill tool) before reading on.", "\nRead `tools/claude-skills/skills/agent-fleet/SKILL.md` instead.")], [1, 1])
    expect.equal("... an overlay over its size is refused", len(check_practices.overlay_problems(
        "workstream", WS_SKILL + "x" * check_practices.SKILL_MAX_BYTES)), 1)
    expect.equal("... an overlay naming a reference file that does not exist (its own, or the base's) is refused",
          [len(check_practices.overlay_problems("workstream", WS_SKILL + t)) for t in
           ("\nSee references/no-such-file.md.", "\nSee tools/claude-skills/skills/agent-fleet/references/no-such-file.md.")], [1, 1])
    expect.equal("... a gate, review or spec-lookup overlay still has to name its base skill",
          (check_practices.overlay_problems("gate", "no base named here"), check_practices.overlay_problems("gate", "Invoke `brent-tools:test-gate`")),
          (["gate overlay does not invoke its brent-tools base skill first"], []))


def case_065_the_real_instruction_surfaces_carry_no_r(expect):
    TRACKED_SURFACES = [*live_tools.tracked(coord.REPO, (".md", ".py", ".js", ".yml", ".json", ".txt")),
                        *(f.relative_to(coord.REPO).as_posix() for f in (coord.REPO / ".github").rglob("*") if f.is_file()), "CLAUDE.md"]
    TRACKED_SURFACES = [rel for rel in TRACKED_SURFACES if rel.startswith(check_practices.GATING_SURFACES)]
    expect.equal("the real instruction surfaces carry no retired gating wording",
          [m for rel in TRACKED_SURFACES for m in check_practices.retired_gating_lines(rel, (coord.REPO / rel).read_text(encoding="utf-8"))], [])
    expect.equal("... each retired phrase is refused on a surface; a PB4328 line and a negation pass; a code file is no surface",
          ([len(check_practices.retired_gating_lines(".github/X.md", p)) for p in
            ("verdict LEG 1 ONLY (batched gating): GREEN", "tier 0a", "batched gating", "the impact map", "runs in leg 1")],
           check_practices.retired_gating_lines(".claude/skills/x.md", "the impact map ended with PB4328"),
           check_practices.retired_gating_lines("CLAUDE.md", "There is no impact map, no tier"),
           check_practices.retired_gating_lines("scripts/gate.py", "LEG 1 ONLY")),
          ([1, 1, 1, 1, 1], [], [], []))
    expect.equal("the real instruction surfaces send no agent to the session scratchpad (kb/Work PB4466)",
          [m for rel in live_tools.tracked(coord.REPO, (".md", ".py", ".js", ".yml", ".json", ".txt"))
           for m in check_practices.bare_scratchpad_lines(rel, (coord.REPO / rel).read_text(encoding="utf-8"))], [])
    # 7d. A HOST-I-O GROUP RUNS CI'S LINUX LEGS AT ITS OWN GATE (kb/Work PB3381; MANDATORY-PRACTICES I8). Expected from the
    #     rule: a file under src/WiseOwl.Cobol.Runtime/IO/ (either separator) makes a group host-I-O, a compiler or other runtime
    #     file does not; the file set is the group's file_set list, else (absent or empty) what its notes declare; a
    #     host-I-O spec says run the unit and conformance legs from WSL after the Windows gate, any other says the
    #     per-change gate runs nothing on Linux (the landing runs no Linux gate: kb/Work PB4327); check_practices refuses a spec whose paragraph is not its group's.
    expect.equal("host_io: a file under src/WiseOwl.Cobol.Runtime/IO/ sets it, either separator",
          (mds.host_io(["src/WiseOwl.Cobol.Runtime/IO/IndexedConnector.cs"]), mds.host_io(["src\\WiseOwl.Cobol.Runtime\\IO\\Sharing\\X.cs"])),
          (True, True))
    expect.equal("host_io: a compiler file, another runtime folder, a look-alike folder and no file do not set it",
          (mds.host_io(["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs"]), mds.host_io(["src/WiseOwl.Cobol.Runtime/Values/CobolDec.cs"]),
           mds.host_io(["src/WiseOwl.Cobol.Runtime/IOX/A.cs"]), mds.host_io([])), (False, False, False, False))


def case_066_group_claim_the_file_set_list_with_its_m(expect):
    asked = []
    expect.equal("group_claim: the file_set list (with its member_set) when it has files, else the notes' declared set",
          (mds.group_claim({"notes": "PB1", "file_set": ["src/A.cs"], "member_set": {"src/A.cs": ["M"]}},
                           lambda ids: asked.append(ids) or Claim.of({"src/N.cs"})).member_lists(),
           sorted(mds.group_claim({"notes": "PB1, PB2", "file_set": []},
                                  lambda ids: asked.append(ids) or Claim.of({"src/WiseOwl.Cobol.Runtime/IO/N.cs"})).files),
           sorted(mds.group_claim({"notes": "PB3"}, lambda ids: asked.append(ids) or Claim()).files), asked),
          ({"src/A.cs": ["M"]}, ["src/WiseOwl.Cobol.Runtime/IO/N.cs"], [], [["PB1", "PB2"], ["PB3"]]))


def case_067_a_host_i_o_group_s_spec_runs_the_linux_u(expect):
    global CMG, IOG, cm_spec, io_spec
    IOG = dict(gjson[0], letter="IO", file_set=["src/WiseOwl.Cobol.Runtime/IO/IndexedConnector.cs"])
    CMG = dict(gjson[0], letter="CM", file_set=["src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs"])
    io_spec, cm_spec = (mds.render(CFG77, g, FILES(g)) for g in (IOG, CMG))
    expect.equal("a host-I-O group's spec runs the Linux unit and conformance legs at its gate, blocking on the verdict",
          ("HOST-I-O GROUP" in io_spec, "bash scripts/linux-gate.sh --legs unit,conformance --nice" in io_spec,
           "=== LINUX GATE: " in io_spec, "skip it and report" in io_spec), (True, True, True, False))
    expect.equal("any other group's spec says the per-change gate runs nothing on Linux (kb/Work PB4327)",
          ("HOST-I-O GROUP" in cm_spec, "Report `LINUX GATE: not part of the per-change gate (PB4327)`" in cm_spec),
          (False, True))


def case_068_both_rendered_specs_pass_check_practices(expect):
    global cm_file, io_file
    io_file, cm_file = TMP / "msg-w77-io.txt", TMP / "msg-w77-cm.txt"
    io_file.write_text(io_spec, encoding="utf-8", newline="\n")
    cm_file.write_text(cm_spec, encoding="utf-8", newline="\n")
    expect.equal("both rendered specs pass check_practices",
          [[m for m in check_practices.check(f, check_practices.SPEC) if "report" not in m] for f in (io_file, cm_file)], [[], []])


def case_069_check_practices_finds_each_spec_s_linux(expect):
    global iofile
    iofile = TMP / "io-groups.json"
    iofile.write_text(__import__("json").dumps(dict(CFG77, groups=[IOG, CMG])), encoding="utf-8", newline="\n")
    expect.equal("check_practices finds each spec's LINUX GATE paragraph its group's",
          check_practices.specs_with_wrong_linux_gate(iofile, [io_file, cm_file]), [])


def case_070_check_practices_refuses_a_host_i_o_group(expect):
    swapped = TMP / "swapped"
    swapped.mkdir()
    (swapped / "msg-w77-io.txt").write_text(cm_spec, encoding="utf-8", newline="\n")
    (swapped / "msg-w77-cm.txt").write_text(io_spec, encoding="utf-8", newline="\n")
    expect.equal("check_practices refuses a host-I-O group's spec without the host-I-O paragraph, and the reverse",
          check_practices.specs_with_wrong_linux_gate(iofile, [swapped / "msg-w77-io.txt", swapped / "msg-w77-cm.txt"]),
          ["msg-w77-io.txt (a host-I-O group)", "msg-w77-cm.txt (no host-I-O file)"])
    try:
        coord.fleet_stop(TMP, "../x")
        expect.equal("a scope with a path in it is refused", "accepted", "ValueError")
    except ValueError:
        expect.equal("a scope with a path in it is refused", True, True)
    expect.equal("lead is the note with most rows", next(j["lead"] for j in gjson if j["letter"] == G[("PB3", "PB6", "PB7", "PB12")].letter), "PB6")


def case_071_check_practices_o2_same_file_rule(expect):
    global gfile
    gfile = TMP / "groups.json"
    gfile.write_text(__import__("json").dumps({"wave": "77", "scratch": "X", "base": "abc", "stop_file": CFG77["stop_file"],
                                              "groups": gjson}), encoding="utf-8", newline="\n")
    expect.equal("check_practices O2 same-file rule", check_practices.claims_meet(gfile), [])


def case_072_check_practices_o2_is_a_shared_member_ru(expect):
    global MEMBER_GROUPS
    MEMBER_GROUPS = TMP / "groups-members.json"


    def member_groups(*member_sets):
        """A groups file of groups A, B, ... on one primary file, each claiming the members given (None: the whole file)."""
        gs = [{"letter": chr(65 + k), "root": "src/X/Binder.cs", **({"member_set": {"src/X/Binder.cs": ms}} if ms else {})}
              for k, ms in enumerate(member_sets)]
        MEMBER_GROUPS.write_text(__import__("json").dumps({"groups": gs}), encoding="utf-8", newline="\n")
        return check_practices.claims_meet(MEMBER_GROUPS)


    expect.equal("check_practices O2 is a shared-MEMBER rule (kb/Work PB4452): different members of one primary file pass, and so "
          "does a group claiming the file whole; only one member both edit is refused",
          (member_groups(["Binder.Bind"], ["Binder.Sync"]), member_groups(["Binder.Bind"], ["Bind"]),
           member_groups(["Binder.Bind"], None)),
          ([], ["A+B on src/X/Binder.cs#Binder.Bind"], []))


def case_073_check_practices_o2_refuses_two_groups_ed(expect):
    MEMBER_GROUPS.write_text(__import__("json").dumps({"groups": [
        {"letter": "A", "root": "src/X/Binder.cs", "member_set": {"src/X/Binder.cs": ["Binder.Bind"]}},
        {"letter": "A2", "root": "src/X/Binder.cs", "after": "A", "member_set": {"src/X/Binder.cs": ["Binder.Bind"]}}]}),
                             encoding="utf-8", newline="\n")
    expect.equal("check_practices O2 refuses two groups editing one member even when one declares `after:` the other (no successor "
          "exception, kb/Work PB4396)", check_practices.claims_meet(MEMBER_GROUPS),
          ["A+A2 on src/X/Binder.cs#Binder.Bind"])


def case_074_check_practices_o2_two_groups_claiming_o(expect):
    MEMBER_GROUPS.write_text(__import__("json").dumps({"groups": [{"letter": "A", "root": "src/X/Binder.cs"},
                                                                  {"letter": "B", "root": "src/X/Binder.cs"}]}),
                             encoding="utf-8", newline="\n")
    expect.equal("check_practices O2: two groups claiming one file WHOLE run side by side (multiple editors per file, kb/Work "
          "PB4452, owner 2026-11-02)", check_practices.claims_meet(MEMBER_GROUPS), [])


def case_075_check_practices_o2_reads_the_whole_claim(expect):
    def claim_groups(*claims):
        """A groups file of groups A, B, ... each with the file_set and member_set given (the planner writes both)."""
        gs = [{"letter": chr(65 + k), "file_set": fs, **({"member_set": ms} if ms else {})} for k, (fs, ms) in enumerate(claims)]
        MEMBER_GROUPS.write_text(__import__("json").dumps({"groups": gs}), encoding="utf-8", newline="\n")
        return check_practices.claims_meet(MEMBER_GROUPS)


    BIND_A = ([F_BIND, "src/X/Shared.cs"], {F_BIND: ["Binder.Bind"]})
    BIND_B = ([F_BIND, "src/X/Shared.cs"], {F_BIND: ["Binder.Sync"]})
    EMIT_B = ([F_EMIT, "src/X/Shared.cs"], {F_EMIT: ["Emitter.Emit"]})
    expect.equal("check_practices O2 reads the WHOLE claim by member (kb/Work PB3696, PB4452): a secondary file both groups claim "
          "whole is no meeting, and a member both name on it is",
          (claim_groups(BIND_A, BIND_B), claim_groups(BIND_A, EMIT_B),
           claim_groups(([F_BIND, "src/X/Shared.cs"], {F_BIND: ["Binder.Bind"], "src/X/Shared.cs": ["Shared.Go"]}),
                        ([F_BIND, "src/X/Shared.cs"], {F_BIND: ["Binder.Sync"], "src/X/Shared.cs": ["Shared.Go"]}))),
          ([], [], ["A+B on src/X/Shared.cs#Shared.Go"]))


def case_076_default_plan_equals_the_pre_campaign_gol(expect):
    global JSON, fp
    # 8. THE FIX LANE WITHOUT --cluster IS UNCHANGED (kb/Work PB2120): the whole plan of this fixture, every group's
    #    rendered JSON included, equals the golden that the planner wrote BEFORE the campaign lane existed (committed
    #    alone in the PB2120 checkpoint before plan_wave.py changed). `--write-golden` rewrites it; only a deliberate
    #    change to the fix lane does that, and says so in its commit.
    GOLDEN = fixtures.HERE / "testdata" / "plan_wave_default.json"


    def fingerprint(pl, letter_json):
        return {"groups": [{"letter": g.letter, "after": g.after, "kind": g.kind, "model": g.model, "why": g.why_model,
                            "notes": g.notes, "file": g.file, "files": g.files, "pred": g.pred, "cost": round(g.cost, 9),
                            "json": letter_json.get(g.letter)} for g in pl["groups"]],
                "skipped": [[g.letter, g.after, g.notes] for g in pl["skipped"]],
                "awaiting_landing": pl["awaiting_landing"], "points": round(pl["points"], 9),
                "unlanded_without_note": pl["unlanded_without_note"]}


    JSON = __import__("json")
    fp = {"full": fingerprint(p, {j["letter"]: j for j in gjson}), "tight": fingerprint(p2, {}),
          "small": fingerprint(p3, {}), "max2": fingerprint(p4, {})}
    # the fixture's temporary directory is the one varying input: it is spelled <TMP> in the golden, and every path
    # separator as `/`, so the same golden holds on Windows and Linux
    fp_text = (JSON.dumps(fp, indent=1, ensure_ascii=False).replace(JSON.dumps(str(TMP))[1:-1], "<TMP>")
               .replace("\\\\", "/") + "\n")
    if "--write-golden" in sys.argv:
        GOLDEN.write_text(fp_text, encoding="utf-8", newline="\n")
        print(f"wrote {GOLDEN}")
    expect.equal("default plan equals the pre-campaign golden", fp_text == GOLDEN.read_text(encoding="utf-8"), True)


def case_077_cluster_members_open_any_kind_harm_flags(expect):
    global CW, citems, e, fc, rel, view
    # 9. A CAMPAIGN (--cluster, kb/Work PB2120): the open notes naming the cluster, harm flags or not, in blocked_by order;
    #    clustered by the sites they name anywhere in the tree; a dependent runs after its blocker's group (`after:`); a
    #    dependent whose blockers sit in two chains, a note held by the owner, and a frontier-model note wait.
    sys.path.insert(0, str(coord.REPO / "scripts" / "spec"))
    import work  # noqa: E402

    CW = TMP / "campaign" / "kb" / "Work"
    CW.mkdir(parents=True)
    for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "scripts/gamma.py", "tests/Delta.Tests/DeltaTests.cs"):
        (TMP / "campaign" / rel).parent.mkdir(parents=True, exist_ok=True)
        (TMP / "campaign" / rel).write_text("// fixture\n", encoding="utf-8", newline="\n")


    def cnote(nid, cluster=("PB901",), blocked_by=(), body="", status="open", kind="defect", frontier_model=""):
        bb = ", ".join(blocked_by)
        fm = f"frontier_model: {frontier_model}\n" if frontier_model else ""
        (CW / f"{nid}.md").write_text(
            f'---\ntitle: "{nid} — campaign step"\nid: {nid}\nkind: {kind}\nstatus: {status}\narea: build/ci\n'
            f"wrong_answer: false\nprocess_only: true\n{fm}"
            f"blocked_by: [{bb}]\ncluster: [{', '.join(cluster)}]\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8", newline="\n")


    cnote("PB901", body="**Sites:**\n- `src/Lib/Alpha.cs`\n\nSever it from the old engine.")
    cnote("PB902", body="**Code site.** scripts/gamma.py (its legacy legs)\n\nRewrite them.")
    cnote("PB903", blocked_by=("PB901",), body="### Code site\n\n`src/Lib/Beta.cs:12-40`\n\n### Fix\n\nThen delete it.")
    cnote("PB904", blocked_by=("PB901", "PB902"), body="- **Code site:** tests/Delta.Tests/DeltaTests.cs#Rename\n\nThen rename.")
    # PB905 names no site in a site block: what its prose discusses, and its Gate: line, declare nothing (kb/Work PB3242)
    cnote("PB905", blocked_by=("PB903",), body="Then sweep the docs; it discusses `src/Lib/Alpha.cs` and scripts/gamma.py.\n\n"
                                               "**Gate:** `python scripts/gamma.py --check`")
    cnote("PB906", body="Author = Mythos 5.1, only after the owner's explicit\napproval for this dispatch.", kind="analysis",
          frontier_model="mythos")
    cnote("PB907", cluster=("OTHER",), body="**Sites:**\n- `src/Lib/Alpha.cs`")
    cnote("PB908", status="landed", body="**Sites:**\n- `src/Lib/Alpha.cs`")
    cnote("PB909", status="owner", body="Waits for the owner.")
    cnote("PB910", cluster=("PB901", "OTHER"), body="**Sites:**\n- `scripts/gamma.py#main` (main)")
    citems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(CW.glob("*.md"))]
    view = work.cluster_order(citems, "PB901")
    expect.equal("cluster members: open, any kind, harm flags or not, in blocked_by order",
          [(r["id"], r["depth"], r["ready"]) for r in view["notes"]],
          [("PB901", 0, True), ("PB902", 0, True), ("PB906", 0, True), ("PB909", 0, False), ("PB910", 0, True), ("PB903", 1, False),
           ("PB904", 1, False), ("PB905", 2, False)])
    expect.equal("a waiting note names what it waits on", {r["id"]: r["waiting_on"] for r in view["notes"] if not r["ready"]},
          {"PB909": ["status: owner"], "PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
    expect.equal("named counts the landed member too", view["named"], 9)
    expect.equal("a landed cluster has no open note", work.cluster_order(citems, "PB908")["notes"], [])

    try:
        fc = load_fix_clusters()
    except SystemExit as e:  # the campaign clusters by fix_clusters.py's real index: a missing submodule is a FAIL, not a skip
        fc = None
        expect.equal("tools/claude-skills submodule present (git submodule update --init tools/claude-skills)", str(e), "")


def case_078_computed_file_set_reads_a_note_s_addenda(expect):
    global g
    # A NOTE'S BODY IS ITS FILE AND ITS ADDENDA (kb/Work PB5110, review R1-1): the landing check's computed file set reads a
    # **Sites:** block that only an addendum carries, so a reopened note claims the files its reopening names
    ADD_REPO = TMP / "addenda-repo"
    (ADD_REPO / "kb" / "Work" / "PB9101").mkdir(parents=True)
    (ADD_REPO / "kb" / "Work" / "PB9101.md").write_text("---" + chr(10) + "id: PB9101" + chr(10) + "---" + chr(10) + "# PB9101" + chr(10),
                                                        encoding="utf-8", newline=chr(10))
    (ADD_REPO / "kb" / "Work" / "PB9101" / "2026-11-11-reopen.md").write_text(
        "**Sites:** `scripts/addendum_only.py`" + chr(10), encoding="utf-8", newline=chr(10))
    expect.equal("computed_file_set reads a note's addenda: a site named only in an addendum is claimed",
          "scripts/addendum_only.py" in inb.computed_file_set(ADD_REPO, ["PB9101"], IndexPolicy(TMP / "no-cache-add", regenerate=False)).files,
          True)
    if fc:
        cnotes = load_notes(CW, {"wrong_answer": 8})
        ctexts = {i: (CW / f"{i}.md").read_text(encoding="utf-8") for i in cnotes}
        # THE declared set (declared_sets) on a policy that refuses to build an index: no note here names a member, so the
        # index is never read (a fix-lane or sites-only note declares its sites without one)
        CDECL = declared_sets(TMP / "campaign", IndexPolicy(TMP / "no-cache", regenerate=False))
        copen, chalf, cdeps, cwait = campaign_clusters(view, cnotes, ctexts, RULES, fc, TMP / "campaign", CDECL)
        # PB904 waits on PB901 (Alpha.cs) and PB902 (gamma.py): their two clusters merge, so PB904 can follow ONE chain
        expect.equal("campaign clusters by declared sites, one depth at a time; a dependent's blocker clusters merge",
              [(c["depth"], c["file"], [n["id"] for n in c["notes"]]) for c in copen],
              [(0, "scripts/gamma.py", ["PB902", "PB910", "PB901"]), (0, "", ["PB906"]),
               (1, "src/Lib/Beta.cs", ["PB903"]), (1, "tests/Delta.Tests/DeltaTests.cs", ["PB904"]), (2, "", ["PB905"])])
        expect.equal("the merged cluster orients on both files", copen[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])
        expect.equal("deps are the open in-cluster blockers", cdeps, {"PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
        expect.equal("held by the owner: waiting before planning", cwait, {"PB909": "waiting on status: owner"})
        cp = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=cdeps)
        CG = {tuple(g.notes): g for g in cp["groups"]}
        expect.equal("campaign groups and their successor chain",
              [(g.letter, g.after, g.notes, g.blocked_by) for g in cp["groups"]],
              [("A", "", ["PB902", "PB910", "PB901"], []), ("A2", "A", ["PB903"], ["PB901"]),
               ("A3", "A2", ["PB904"], ["PB901", "PB902"]), ("A4", "A3", ["PB905"], ["PB903"])])
        expect.equal("a frontier-model note waits for the owner", sorted(cp["waiting"]), ["PB906"])
        expect.equal("the frontier-model reason", "approval" in cp["waiting"].get("PB906", ""), True)
        expect.equal("the dependency is in the rendered body",
              ("PB903",) in CG and "DEPENDENCY SUCCESSOR" in body_of(CG[("PB903",)], cnotes), True)
        expect.equal("a fix-lane body never says it", any("DEPENDENCY SUCCESSOR" in body_of(g, notes) for g in p["groups"]), False)
        cg = [as_group_json(g, "78", cnotes, "COBOL0001-COBOL0003") for g in cp["groups"]]
        cfile = TMP / "campaign-groups.json"
        cfile.write_text(JSON.dumps({"wave": "78", "scratch": "X", "base": "abc", "groups": cg}), encoding="utf-8", newline="\n")
        expect.equal("campaign groups pass check_practices' one-step-per-file rule", check_practices.claims_meet(cfile), [])
        for g in cg:
            mds.render(dict(CFG77, wave="78"), g, FILES(g))
        only_b = group_cost("opus", 1, RULES) + landing_cost(RULES)
        cp2 = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=only_b * 1.5, deps=cdeps)
        expect.equal("a dependent is never chosen without its blocker's group",
              all(g.after in {c.letter for c in cp2["groups"]} for g in cp2["groups"] if g.after), True)


def case_079_cluster_order_tier_first_a_blocker_takes(expect):
    global bb, f, fp, free, nid, own, rel, site
    # 9b. THE OWNER'S TIER RANKS A CAMPAIGN FIRST (kb/Work PB3423): wave 1129's dry plan gave five of its eight groups to
    #     untiered PB3353 members because the planner ranked by rows and harm alone. Here the untiered notes carry the
    #     harm (wrong_answer) and the tiered ones none, so without the tier they would plan first.
    TW = TMP / "tiers" / "kb" / "Work"
    TW.mkdir(parents=True)
    for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "scripts/gamma.py", "tests/Delta.Tests/DeltaTests.cs"):
        (TMP / "tiers" / rel).parent.mkdir(parents=True, exist_ok=True)
        (TMP / "tiers" / rel).write_text("// fixture\n", encoding="utf-8", newline="\n")


    def tnote(nid, tier=None, blocked_by=(), site="", wrong=False):
        # one subsystem per note: a note with no site block (PB944, PB948) is UNKNOWN and is planned alone on its
        # subsystem (kb/Work PB3263, section 9g), which a shared area would turn into one chain and hide the tier order
        (TW / f"{nid}.md").write_text(
            f'---\ntitle: "{nid} — tiered step"\nid: {nid}\nkind: defect\nstatus: open\narea: runtime/{nid.lower()}\n'
            f"wrong_answer: {'true' if wrong else 'false'}\nprocess_only: true\n"
            f"blocked_by: [{', '.join(blocked_by)}]\ncluster: [PB940, PB949]\n" +
            (f'tier: ["PB940:{tier}", "PB949:{9 - tier}"]\n' if tier else "") +
            f"---\n\n# {nid}\n\n" + (f"**Sites:**\n- `{site}`\n" if site else "Prose only.\n"), encoding="utf-8", newline="\n")


    tnote("PB941", site="src/Lib/Alpha.cs", wrong=True)
    tnote("PB942", site="scripts/gamma.py", wrong=True)
    tnote("PB943", tier=1, site="src/Lib/Beta.cs")
    tnote("PB944", tier=1)
    tnote("PB945", tier=2, site="tests/Delta.Tests/DeltaTests.cs")
    tnote("PB947", tier=1, blocked_by=("PB948",))
    tnote("PB948")  # untiered, but it blocks a tier-1 note: its effective tier is 1
    titems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(TW.glob("*.md"))]
    tview = work.cluster_order(titems, "PB940")
    expect.equal("cluster_order: tier first (a blocker takes the lowest tier it blocks), then depth, untiered last",
          [(r["id"], r["tier"], r["depth"]) for r in tview["notes"]],
          [("PB943", 1, 0), ("PB944", 1, 0), ("PB948", 1, 0), ("PB947", 1, 1), ("PB945", 2, 0), ("PB941", None, 0),
           ("PB942", None, 0)])
    expect.equal("the tiered register is well-formed", work.topology_problems(titems), [])
    expect.equal("a tier is scoped to its campaign: the same notes in PB949 rank by that campaign's tiers",
          [(r["id"], r["tier"]) for r in work.cluster_order(titems, "PB949")["notes"]][:5],
          [("PB945", 7), ("PB943", 8), ("PB944", 8), ("PB948", 8), ("PB947", 8)])
    expect.equal("lift_tiers: a cluster waiting on another lowers it, transitively; an unrelated cluster keeps its tier",
          (lambda cs: (lift_tiers(cs, {"X2": ["Y1"], "Y1": ["Z1"]}), [c["tier"] for c in cs])[1])(
              [{"notes": [{"id": "X1"}, {"id": "X2"}], "tier": 1}, {"notes": [{"id": "Y1"}], "tier": None},
               {"notes": [{"id": "Z1"}], "tier": 3}, {"notes": [{"id": "W1"}], "tier": None}]),
          [1, 1, 1, None])
    if fc:
        tnotes = load_notes(TW, {"wrong_answer": 8})
        ttexts = {i: (TW / f"{i}.md").read_text(encoding="utf-8") for i in tnotes}
        TDECL = declared_sets(TMP / "tiers", IndexPolicy(TMP / "no-cache-tiers", regenerate=False))
        topen, thalf, tdeps, twait = campaign_clusters(tview, tnotes, ttexts, RULES, fc, TMP / "tiers", TDECL)
        expect.equal("campaign clusters carry the tier", {tuple(n["id"] for n in c["notes"]): c["tier"] for c in topen},
              {("PB941",): None, ("PB942",): None, ("PB943",): 1, ("PB944",): 1, ("PB945",): 2, ("PB947",): 1,
               ("PB948",): 1})
        tp = pw.plan(tnotes, topen, thalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=tdeps)
        expect.equal("the plan letters tier 1 first, its successor after its blocker, then tier 2, then the untiered",
              [(g.letter, g.after, g.notes) for g in tp["groups"]],
              [("A", "", ["PB943"]), ("B", "", ["PB944"]), ("C", "", ["PB948"]), ("C2", "C", ["PB947"]),
               ("D", "", ["PB945"]), ("E", "", ["PB941"]), ("F", "", ["PB942"])])
        # THE DRIFT TEST: under a group cap, no group of a later tier is chosen while a READY group of an earlier one waits
        capped = pw.plan(tnotes, topen, thalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=tdeps, max_groups=4)
        rank = {r["id"]: work.tier_rank(r["tier"]) for r in tview["notes"]}
        worst_chosen = max(rank[i] for g in capped["groups"] for i in g.notes)
        expect.equal("a group cap spends its slots on tier 1 before any untiered group, however much harm it carries",
              ([g.notes for g in capped["groups"]],
               all(min(rank[i] for i in g.notes) >= worst_chosen for g in capped["skipped"] if not g.after)),
              ([["PB943"], ["PB944"], ["PB948"], ["PB947"]], True))
        # THE LIFT, THROUGH campaign_clusters AND plan (review R1-2 of PB3423: deleting the call kept every check green).
        # At a cap of 2, Alpha.cs's three notes split [PB961, PB962] and [PB963]; Beta.cs's tier-1 PB964 shares a cluster
        # with the untiered PB965, whose blocker PB963 sits alone and untiered. Unlifted, that cluster ranks after the
        # tier-1 dependent cluster, which then waits ("blocker PB963 is not planned"); lifted, it ranks before it and the
        # dependents follow the one Alpha.cs chain.
        LW = TMP / "lift" / "kb" / "Work"
        LW.mkdir(parents=True)
        for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs"):
            (TMP / "lift" / rel).parent.mkdir(parents=True, exist_ok=True)
            (TMP / "lift" / rel).write_text("// fixture\n", encoding="utf-8", newline="\n")
        for nid, tier, bb, site in (("PB961", 0, "", "Alpha"), ("PB962", 0, "", "Alpha"), ("PB963", 0, "", "Alpha"),
                                    ("PB964", 1, "PB961", "Beta"), ("PB965", 0, "PB963", "Beta")):
            (LW / f"{nid}.md").write_text(
                f'---\ntitle: "{nid} — lift step"\nid: {nid}\nkind: defect\nstatus: open\narea: runtime/io\n'
                f"wrong_answer: false\nprocess_only: true\nblocked_by: [{bb}]\n"
                f"cluster: [PB960]\n" + (f'tier: ["PB960:{tier}"]\n' if tier else "") +
                f"---\n\n# {nid}\n\n**Sites:**\n- `src/Lib/{site}.cs`\n", encoding="utf-8", newline="\n")
        litems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(LW.glob("*.md"))]
        lnotes = load_notes(LW, {"wrong_answer": 8})
        ltexts = {i: (LW / f"{i}.md").read_text(encoding="utf-8") for i in lnotes}
        LRULES = rules_capped(RULES, 2)
        lopen, lhalf, ldeps, _ = campaign_clusters(work.cluster_order(litems, "PB960"), lnotes, ltexts, LRULES, fc,
                                                      TMP / "lift", declared_sets(TMP / "lift", IndexPolicy(
                                                          TMP / "no-cache-lift", regenerate=False)))
        expect.equal("campaign_clusters lifts the untiered blocker's cluster to its tier-1 dependent's",
              [([n["id"] for n in c["notes"]], c["tier"], c["depth"]) for c in lopen],
              [(["PB961", "PB962"], 1, 0), (["PB963"], 1, 0), (["PB964", "PB965"], 1, 1)])
        # planned at the live group size, the two Alpha.cs clusters are one group (one step per file, kb/Work PB4396), and
        # the dependent Beta.cs cluster follows it: a DEPENDENCY successor, on files the group does not claim
        lp = pw.plan(lnotes, lopen, lhalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=ldeps)
        expect.equal("so the mixed dependent cluster follows the one Alpha.cs group instead of waiting",
              ([(g.letter, g.after, g.notes) for g in lp["groups"]], lp["waiting"]),
              ([("A", "", ["PB961", "PB962", "PB963"]), ("A2", "A", ["PB964", "PB965"])], {}))
        # at the 2-note size the third Alpha.cs note runs BESIDE group A as a group of its own (multiple editors per file,
        # kb/Work PB4452: no member of it meets A's), never as overflow that waits a wave; the dependents of both wait
        lp2 = pw.plan(lnotes, lopen, lhalf, [], lambda b: "ABSENT", {}, LRULES, budget_points=100, deps=ldeps)
        expect.equal("PB4452: past the group size the rest of the Alpha.cs family runs beside group A, not as overflow",
              ([(g.letter, g.notes, g.overflow) for g in lp2["groups"]], sorted(lp2["waiting"]),
               "ONTO group A's branch" in lp2["waiting"].get("PB963", "")),
              ([("A", ["PB961", "PB962"], []), ("B", ["PB963"], [])], ["PB964", "PB965"], False))
        # ... and the side group FILLS before a third one opens (review w20261832a R1-1): five one-note clusters on one
        # file claimed whole, at a 3-note size, are two groups of 3 and 2, never 3 + 1 + 1
        FW = TMP / "fill" / "kb" / "Work"
        FW.mkdir(parents=True)
        for nid in ("PB971", "PB972", "PB973", "PB974", "PB975"):
            (FW / f"{nid}.md").write_text(chr(10).join(["---", f'title: "{nid} t"', f"id: {nid}", "kind: defect", "status: open",
                                                        "area: runtime/io", "wrong_answer: false", "process_only: true", "---",
                                                        "", f"# {nid}", "", "**Sites:**", "- `src/Lib/Alpha.cs`", ""]),
                                          encoding="utf-8", newline="\n")
        fnotes = load_notes(FW, {"wrong_answer": 8})
        fclusters = [{"notes": [{"id": i}], "file": "src/Lib/Alpha.cs", "files": ["src/Lib/Alpha.cs"], "members": {}}
                     for i in ("PB971", "PB972", "PB973", "PB974", "PB975")]
        fp = pw.plan(fnotes, fclusters, [], [], lambda b: "ABSENT", {}, rules_capped(RULES, 3), budget_points=100)
        expect.equal("PB4452: a file family past one implementer's size fills a second group before a third opens",
              ([g.notes for g in fp["groups"]], fp["waiting"]),
              ([["PB971", "PB972", "PB973"], ["PB974", "PB975"]], {}))
        # the home a dependent group joins (review w20261832a R2-2, R3-1): never a group on its file in ANOTHER dependency
        # chain (it runs beside, after its own blocker), and always its own blocker's group when that one is on its file,
        # successor or not (one step per file)
        DW = TMP / "deps-home" / "kb" / "Work"
        DW.mkdir(parents=True)
        dsite = {"PB971": "src/Lib/Alpha.cs", "PB972": "src/Lib/Beta.cs", "PB973": "src/Lib/Alpha.cs"}
        for nid, f in dsite.items():
            (DW / f"{nid}.md").write_text(chr(10).join(["---", f'title: "{nid} t"', f"id: {nid}", "kind: defect", "status: open",
                                                        "area: runtime/io", "wrong_answer: false", "process_only: true", "---",
                                                        "", f"# {nid}", "", "**Sites:**", f"- `{f}`", ""]),
                                          encoding="utf-8", newline="\n")
        dnotes = load_notes(DW, {"wrong_answer": 8})
        dclusters = [{"notes": [{"id": i}], "file": dsite[i], "files": [dsite[i]], "members": {}} for i in dsite]
        drules = json.loads(json.dumps(RULES))
        drules["wave"]["links_per_chain"] = 99
        other = pw.plan(dnotes, dclusters, [], [], lambda b: "ABSENT", {}, drules, budget_points=100, deps={"PB973": ["PB972"]})
        own = pw.plan(dnotes, dclusters, [], [], lambda b: "ABSENT", {}, drules, budget_points=100,
                      deps={"PB972": ["PB971"], "PB973": ["PB971"]})
        expect.equal("PB4452: a dependent group runs after its own blocker, beside a group on its file in another chain; and joins its "
              "own blocker's group on its file even when that group has a successor",
              ([(g.letter, g.after, g.notes) for g in other["groups"]], other["waiting"],
               [(g.letter, g.after, g.notes) for g in own["groups"]], own["waiting"]),
              ([("A", "", ["PB971"]), ("B", "", ["PB972"]), ("B2", "B", ["PB973"])], {},
               [("A", "", ["PB971", "PB973"]), ("A2", "A", ["PB972"])], {}))
        # ... and a group with NO blocker never joins a dependency SUCCESSOR on its file (review w20261832a R4-1): it runs
        # beside, where the successor itself waits on the one-link-per-wave cap
        fsite = {"PB971": "src/Lib/Alpha.cs", "PB972": "src/Lib/Beta.cs", "PB973": "src/Lib/Beta.cs"}
        for nid, f in fsite.items():
            (DW / f"{nid}.md").write_text(chr(10).join(["---", f'title: "{nid} t"', f"id: {nid}", "kind: defect", "status: open",
                                                        "area: runtime/io", "wrong_answer: false", "process_only: true", "---",
                                                        "", f"# {nid}", "", "**Sites:**", f"- `{f}`", ""]),
                                          encoding="utf-8", newline="\n")
        free = pw.plan(load_notes(DW, {"wrong_answer": 8}),
                       [{"notes": [{"id": i}], "file": fsite[i], "files": [fsite[i]], "members": {}} for i in fsite],
                       [], [], lambda b: "ABSENT", {}, {**drules, "wave": {**drules["wave"], "links_per_chain": 1}},
                       budget_points=100, deps={"PB972": ["PB971"]})
        expect.equal("PB4452: a group with no blocker runs beside a dependency successor on its file, never joins it to wait",
              ([(g.letter, g.after, g.notes) for g in free["groups"]], sorted(free["waiting"])),
              ([("A", "", ["PB971"]), ("B", "", ["PB973"])], ["PB972"]))


def case_080_pb3638_a_tier_1_group_heads_its_file_s_w(expect):
    # 9c. THE PLACEMENT ORDER IS THE TIER, AND A WAVE RUNS ONE LINK PER CHAIN (kb/Work PB3638, the owner's fix-cycle bar).
    #     Wave 20261029 chained two untiered FINISHERS (PB3112, PB2878) ahead of the tier-1 cluster PB3612 on one file,
    #     because every finisher was placed before every cluster, and ran all three links in one wave: PB3612 started two
    #     hours in. Here F_BIND holds the cluster (PB2, PB5) and the tier-1 PB14; the base fixture's finishers are
    #     untiered. Under one step per file (kb/Work PB4396) the untiered cluster is the tier-1 group's work, never a link.
    tp1 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
                  tiers={"PB14": 1})
    expect.equal("PB3638: a tier-1 group heads its file's work and is placed before every untiered finisher",
          [(g.letter, g.after, g.notes) for g in tp1["groups"]][:3],
          [("A", "", ["PB14", "PB2", "PB5"]), ("B", "", list(FIN)), ("C", "", ["PB4"])])
    expect.equal("PB4396: the untiered cluster on that file joins the tier-1 group, no successor",
          [(g.letter, g.after) for g in tp1["groups"] if {"PB2", "PB5"} & set(g.notes)], [("A", "")])


def case_081_pb3638_a_finisher_takes_its_notes_tier_i(expect):
    tp2 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
                  tiers={"PB13": 1, "PB14": 2})
    expect.equal("PB3638: a finisher takes its notes' tier; inside one tier the finishers keep their order",
          [g.notes for g in tp2["groups"]][:4], [["PB13"], ["PB14", "PB2", "PB5"], list(FIN), ["PB4"]])
    expect.equal("PB3638: the fix lane passes no tiers, so its order is the one check 2 holds", [g.notes for g in p["groups"]][:3],
          [list(FIN), ["PB4"], ["PB13"]])
    expect.equal("PB3638: model_rules.json states the owner's one link per chain", LIVE_RULES["wave"]["links_per_chain"], 1)
    # THE IMPLEMENTER'S DEFAULT MODEL HAS ONE HOME (kb/Work PB3868). Expected from the owner's decision of 2026-10-27: the
    # implementer runs Sonnet unless model_rules.json's routing.opus_when sends a group to Opus, and the default is the `model` of
    # .claude/agents/cobol-implementer.md, which the harness reads when a group names no model; model_rules.json states no copy
    # (the retired routing.default and wave.implementer_model), so the planner, the ledger and the harness cannot disagree.
    expect.equal("PB3868: the implementer's default model is Sonnet, from its agent definition", coord.agent_model("cobol-implementer"), "sonnet")
    expect.equal("PB3868: the default is a model the routing allows", coord.agent_model("cobol-implementer") in LIVE_RULES["routing"]["allowed"], True)
    expect.equal("PB3868: model_rules.json states no second copy of the default",
          ("default" in LIVE_RULES["routing"], "implementer_model" in LIVE_RULES["wave"]), (False, False))


def case_082_pb3868_a_definition_with_no_model_is_ref(expect):
    AGDIR = TMP / "agents-pb3868"
    AGDIR.mkdir()
    (AGDIR / "cobol-implementer.md").write_text("---" + chr(10) + "name: cobol-implementer" + chr(10) + "maxTurns: 400" + chr(10) + "---" + chr(10) + "body" + chr(10), encoding="utf-8", newline=chr(10))
    try:
        coord.agent_model("cobol-implementer", AGDIR)
        expect.equal("PB3868: a definition with no model is refused, never defaulted", "returned a model", "ValueError")
    except ValueError:
        expect.equal("PB3868: a definition with no model is refused, never defaulted", True, True)


def case_083_pb3638_under_the_live_rules_no_dispatche(expect):
    tp3 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, LIVE_RULES,
                  budget_points=100, tiers={"PB14": 1})
    expect.equal("PB3638: under the live rules no dispatched group follows another, and one file's work is one group",
          ([g.notes for g in tp3["groups"] if g.after], ["PB14", "PB2", "PB5"] in [g.notes for g in tp3["groups"]],
           [i for i in ("PB2", "PB5", "PB14") if i in tp3["waiting"]]), ([], True, []))


def case_084_block(expect):
    global cap
    # kb/Work PB3712: a deferred link's reason says what the wave does with its predecessor. Only a DEPENDENCY chain has
    # links now (kb/Work PB4396): three notes on three files, each blocked by the one before (A, A2, A3); under the live one
    # link per chain, A runs, A2 is deferred behind a group "which this wave runs", and A3 follows A2, which this wave does
    # NOT run: its reason may not say it does. With the head cut (the budget fits none, or the group cap is 0) neither link
    # is behind a group this wave runs.
    CHAIN3 = [{**cl("src/X/ChainA.cs", "PB6"), "depth": 0}, {**cl("src/X/ChainB.cs", "PB7"), "depth": 1},
              {**cl("src/X/ChainC.cs", "PB5"), "depth": 2}]
    CHAIN3_DEPS = {"PB7": ["PB6"], "PB5": ["PB7"]}
    for what, budget, cap, head_runs in (("the head runs", 100, None, True), ("the budget cuts the head", 0.0, None, False),
                                         ("the group cap cuts the head", 100, 0, False)):
        pc = pw.plan(notes, CHAIN3, [], [], lambda b: "ABSENT", {}, LIVE_RULES, budget_points=budget, max_groups=cap,
                     deps=CHAIN3_DEPS)
        say = {i: ("which this wave runs" in pc["waiting"][i], "does not run either" in pc["waiting"][i])
               for i in ("PB7", "PB5")}
        expect.equal(f"PB3712: {what}: a deferred link says its predecessor runs only if the wave runs it",
              ([g.letter for g in pc["groups"]], say),
              (["A"] if head_runs else [], {"PB7": (head_runs, not head_runs), "PB5": (False, True)}))


def case_085_pb4396_seven_notes_on_one_file_are_one_g(expect):
    global SEVEN
    # 9d. ONE STEP PER FILE (kb/Work PB4396, owner 2026-11-01: "One step per file"). Expected values from the note's
    #     acceptance: a planted register of 7 notes on ONE file plans ONE group (7 notes fit the turn budget: 65 + 20.5 x 7
    #     = 208.5 turns, under 0.75 x the implementer's cap); when the predicted turns exceed the budget, the group takes what
    #     fits and the rest is its OVERFLOW, which waits for the next wave to plan it ONTO the group's branch: these notes
    #     all edit the one member Seven.Run (`inflight.hold`), so no second group may edit it beside (check_practices.py O2,
    #     no `after:` exception; kb/Work PB4452: work sharing only the FILE would run beside instead).
    SEVEN = [on_members("src/X/Seven.cs", ["Seven.Run"], f"PB{n}") for n in (6, 7, 8, 9, 10, 11, 12)]
    s7 = pw.plan(notes, SEVEN, [], [], lambda b: "ABSENT", {}, LIVE_RULES, budget_points=100)
    expect.equal("PB4396: seven notes on one file are ONE group under the live turn budget",
          ([sorted(g.notes, key=pw.work.id_order) for g in s7["groups"]], s7["waiting"], notes_cap(LIVE_RULES) >= 7,
           predicted_turns(7, LIVE_RULES) <= turn_budget(LIVE_RULES)),
          ([[f"PB{n}" for n in (6, 7, 8, 9, 10, 11, 12)]], {}, True, True))


def case_086_pb4396_past_the_turn_budget_the_group_ta(expect):
    global s7c
    s7c = pw.plan(notes, SEVEN, [], [], lambda b: "ABSENT", {}, rules_capped(LIVE_RULES, 3), budget_points=100)
    expect.equal("PB4396: past the turn budget the group takes what fits and the rest is its overflow, never a second group",
          ([len(g.notes) for g in s7c["groups"]], [len(g.overflow) for g in s7c["groups"]],
           sorted(i for i in s7c["waiting"] if "one step per file (kb/Work PB4396)" in s7c["waiting"][i])
           == sorted(s7c["groups"][0].overflow) if s7c["groups"] else None),
          ([3], [4], True))
    expect.equal("PB4396: the overflow's reason names the group whose branch the next wave continues",
          all("ONTO group A's branch" in s7c["waiting"][i] for i in s7c["groups"][0].overflow), True)


def case_087_pb4396_the_overflow_reaches_groups_json(expect):
    OVF_JSON = as_group_json(s7c["groups"][0], "9400", notes, "COBOL0001-COBOL0003")
    expect.equal("PB4396: the overflow reaches groups.json (the ledger's record) and the spec body",
          (OVF_JSON.get("overflow") == s7c["groups"][0].overflow, "OVERFLOW CHAIN" in OVF_JSON["body"]), (True, True))


def case_088_pb4944_a_campaign_s_measured_group_size(expect):
    # kb/Work PB4944: a campaign whose groups always ran over plans at the size its measured pace fits (group_sizes.campaign_cap,
    # passed as `group_size`), under the live turn budget's seven, and the overflow's reason names the measurement, not turns
    s7m = pw.plan(notes, SEVEN, [], [], lambda b: "ABSENT", {}, LIVE_RULES, budget_points=100,
                  group_size=(2, "campaign PB6's latest 3 groups all ran past their 60-minute limit"))
    expect.equal("PB4944: a campaign's measured group size holds the group below the turn cap, and its overflow says why",
          ([len(g.notes) for g in s7m["groups"]], [len(g.overflow) for g in s7m["groups"]],
           all("over the 2 notes a group holds, because campaign PB6's latest 3 groups" in s7m["waiting"][i]
               for i in s7m["groups"][0].overflow) if s7m["groups"] else None),
          ([2], [5], True))


def case_089_pb4396_an_overflow_whose_group_this_wave(expect):
    s7b = pw.plan(notes, SEVEN, [], [], lambda b: "ABSENT", {}, rules_capped(LIVE_RULES, 3), budget_points=0.0)
    expect.equal("PB4396: an overflow whose group this wave does not run says so",
          all("does not run that group either" in s7b["waiting"][i] for i in ("PB6", "PB7", "PB8", "PB9", "PB10", "PB11", "PB12")
              if i in s7b["waiting"] and "PB4396" in s7b["waiting"][i]) and any("PB4396" in w for w in s7b["waiting"].values()),
          True)
    # A finisher and a cluster whose claims meet are one group, the finisher's (the absorption that plan_fill did by primary
    # file is the partition's one rule now): check 2's FIN is PB3's finisher with both F_EMIT clusters.
    expect.equal("PB4396: a finisher absorbs the clusters whose claims meet its own, whole",
          (G[FIN].kind, sum(1 for g in p["groups"] if {"PB6", "PB7", "PB12"} & set(g.notes))), ("finisher", 1))


def case_090_campaign_requirements_the_bullets_of_the(expect):
    global REQ_NOTE
    # 9a. A CAMPAIGN'S STANDING REQUIREMENTS (kb/Work PB3359): the bullets of the campaign lead note's `## Standing
    #     requirements` reach every group's spec body, check_practices.py and the branch reviewer's prompt. The owner's
    #     requirement lived only in a wave unit's hand edit of groups.json, and the reviewer never saw it.
    REQ_NOTE = ("# PB950\n\nprose\n\n## Standing requirements\n\nintro line, no bullet\n\n"
                "- First requirement, with commas, and a 'quote'.\n"
                "- Second requirement wraps\n  over two lines.\n\n## Next section\n\n- not a requirement\n")
    expect.equal("campaign_requirements: the bullets of the section, wrapped ones joined, the next heading ends it",
          campaign_requirements(REQ_NOTE),
          ["First requirement, with commas, and a 'quote'.", "Second requirement wraps over two lines."])
    expect.equal("campaign_requirements: CRLF reads the same", campaign_requirements(REQ_NOTE.replace("\n", "\r\n")),
          campaign_requirements(REQ_NOTE))
    expect.equal("campaign_requirements: a note with no such section has none", campaign_requirements("# PB1\n\n- a bullet\n"), [])


def case_091_a_group_is_bound_by_every_campaign_its_n(expect):
    global REQS, RQDIR, bound, member_of, rg, rg_other
    REQS = campaign_requirements(REQ_NOTE)
    # A group is bound by the campaigns its NOTES belong to (group_requirements), never by the planning mode (review R1-1):
    # a fix-lane wave that plans a campaign member, or a campaign finisher, carries the requirement as a --cluster wave does.
    RQDIR = TMP / "req-notes"
    RQDIR.mkdir()
    (RQDIR / "PB950.md").write_text(REQ_NOTE, encoding="utf-8", newline="\n")
    (RQDIR / "PB951.md").write_text("# PB951\n\n## Standing requirements\n\n- Second campaign's own rule.\n", encoding="utf-8", newline="\n")
    (RQDIR / "PB952.md").write_text("# PB952\n\nno standing requirements here\n", encoding="utf-8", newline="\n")
    rg, rg_other = p["groups"][0], p["groups"][1]
    member_of = {rg.notes[0]: ["PB950", "PB951"], rg_other.notes[0]: ["PB952"]}
    bound = group_requirements([rg, rg_other], member_of, RQDIR)
    expect.equal("a group is bound by every campaign its notes belong to, one that holds the section", bound[0], {"PB950": REQS, "PB951": ["Second campaign's own rule."]})
    expect.equal("a member of a campaign whose lead note has no section is bound by nothing", bound[1], {})
    expect.equal("a note naming no campaign (no cluster, a scalar cluster, an unknown note) is bound by nothing",
          group_requirements([rg], {rg.notes[0]: "PB950"}, RQDIR) + group_requirements([rg], {}, RQDIR), [{}, {}])


def case_092_a_requirement_added_in_a_lead_note_s_add(expect):
    # Review R2-1: an owner requirement recorded as an ADDENDUM of a running campaign's lead note reaches the brief.
    (RQDIR / "PB953.md").write_text("# PB953\n\n## Standing requirements\n\n- Lead rule.\n", encoding="utf-8", newline="\n")
    (RQDIR / "PB953").mkdir()
    (RQDIR / "PB953" / "2026-11-12-owner.md").write_text("## Standing requirements\n\n- Owner addition.\n", encoding="utf-8", newline="\n")
    expect.equal("a requirement added in a lead note's addendum reaches the group (R2-1)",
          group_requirements([rg], {rg.notes[0]: ["PB953"]}, RQDIR), [{"PB953": ["Lead rule.", "Owner addition."]}])
    expect.equal("a campaign lead naming no note file binds nothing", group_requirements([rg], {rg.notes[0]: ["PB999"]}, RQDIR), [{}])
    expect.equal("`always` (the campaign a --cluster wave runs) binds every group, before the notes' own, once",
          group_requirements([rg, rg_other], member_of, RQDIR, ["PB950"]),
          [{"PB950": REQS, "PB951": ["Second campaign's own rule."]}, {"PB950": REQS}])


def case_093_a_requirement_reaches_the_group_s_spec_b(expect):
    global rj
    rj = as_group_json(rg, "77", notes, "COBOL0001-COBOL0003", bound[0])
    expect.equal("a requirement reaches the group's spec body, verbatim and numbered, under ONE OWNER REQUIREMENTS lead-in per campaign",
          (all(f"({i}) {r}" in rj["body"] for i, r in enumerate(REQS, 1)), "OWNER REQUIREMENTS (campaign PB950" in rj["body"],
           "OWNER REQUIREMENTS (campaign PB951" in rj["body"], rj["body"].count("OWNER REQUIREMENT")), (True, True, True, 2))
    expect.equal("the group entry carries its requirements for the practices check and the reviewer", rj["requirements"], bound[0])
    expect.equal("a plan with no campaign requirements says none",
          ("OWNER REQUIREMENT" in as_group_json(rg, "77", notes, "COBOL0001-COBOL0003")["body"],
           as_group_json(rg, "77", notes, "COBOL0001-COBOL0003")["requirements"]), (False, {}))


def case_094_the_rendered_spec_carries_every_requirem(expect):
    global rfile, rspec
    rspec = TMP / "msg-w77-req.txt"
    rspec.write_text(mds.render(CFG77, rj, FILES(rj)), encoding="utf-8", newline="\n")
    rfile = TMP / "req-groups.json"
    rfile.write_text(JSON.dumps(dict(CFG77, groups=[rj])), encoding="utf-8", newline="\n")
    expect.equal("the rendered spec carries every requirement (check_practices.py finds none missing)",
          check_practices.specs_missing_requirements(rfile, [rspec]), [])
    # Review R1-1 of wave 20261709: the lead-in used to be repeated before EVERY requirement, so a campaign group with two or
    # more requirements rendered a spec the PB3777 once-check refused and make_dispatch_specs.py could not dispatch the wave.
    expect.equal("a spec rendered for a campaign group with two or more requirements states no sentence twice (PB3777)",
          (len(REQS) >= 2, once_rule.repeated_sentences(rspec.read_text(encoding="utf-8")),
           [m for m in check_practices.check(rspec, []) if "PB3777" in m]), (True, [], []))


def case_095_and_the_once_check_still_refuses_a_spec(expect):
    rdup = TMP / "msg-w77-dup.txt"
    rdup.write_text(rspec.read_text(encoding="utf-8") + "\n" + requirements_text("PB950", REQS) + "\n", encoding="utf-8", newline="\n")
    expect.equal("... and the once-check still refuses a spec that states one campaign's requirements twice",
          [m.startswith("PB3777: states twice: the branch reviewer raises a finding") for m in check_practices.check(rdup, []) if "PB3777" in m], [True])


def case_096_pb4419_an_absorbed_second_findings_finis(expect):
    global ga
    # kb/Work PB4419: wave 20261815's group A absorbed two finishers of FINDINGS-reviewed branches (Group.absorb, PB4396),
    # each pred ending with the answer-by-id rule, so the spec stated it twice and the PB3777 once-check refused the wave.
    # Expected from the rule (a spec says each rule once): the joined pred names BOTH findings files and states the rule ONCE.
    two_rev = [r for r in load_reports(RV_REPORTS) if r.review and r.review.verdict == "FINDINGS"][:1]
    (RV_REPORTS / "w1062g-PB9-report.md").write_text(
        "# PB9 report\n**Status:** DONE · **branch:** `worktree-wf_rv-9` · **HEAD:** `9999999ab`\n"
        "**Review:** FINDINGS 1 w1062g-review-r1 9999999ab E:/s/review-w1062-g.jsonl\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    two_rev += [r for r in load_reports(RV_REPORTS) if r.lead == "PB9"]
    ga = dataclasses.replace(rg, kind="finisher", pred=finisher_pred(two_rev[0], two_rev[0].branch, "UNLANDED", ["PB5"]))
    ga.absorb(dataclasses.replace(rg_other, kind="finisher", pred=finisher_pred(two_rev[1], two_rev[1].branch, "UNLANDED", ["PB9"])))
    expect.equal("PB4419: an absorbed second FINDINGS finisher names both findings files and states the answer rule once",
          (len(two_rev), "review-w1062-e.jsonl" in ga.pred, "review-w1062-g.jsonl" in ga.pred, ga.pred.count(FINDINGS_ANSWER),
           ga.pred.endswith(FINDINGS_ANSWER)), (2, True, True, 1, True))


def case_097_and_its_rendered_spec_passes_the_pb3777(expect):
    fspec = TMP / "msg-w77-two-findings.txt"
    fj = as_group_json(ga, "77", notes, "COBOL0001-COBOL0003")
    fspec.write_text(mds.render(CFG77, fj, FILES(fj)), encoding="utf-8", newline="\n")
    expect.equal("... and its rendered spec passes the PB3777 once-check", [m for m in check_practices.check(fspec, []) if "PB3777" in m], [])


def case_098_pb4419_joining_any_two_kinds_of_pred_sta(expect):
    global PRED_KINDS, _REPLAY, _pred_report, both, got, pa
    # Review R1-1 of PB4419's first branch: FINDINGS_ANSWER was the only rule joined once, but EVERY pred builder writes rules the
    # same for each branch, so two groups of any other kinds said them twice. Expected from the once rule (a spec says each
    # sentence once, and a join loses nothing but a copy): joining ANY two kinds of pred, a kind with itself included, leaves
    # no sentence twice, every sentence either had still there, and every note either names.
    def _pred_report(n, review=None):
        return Report(pathlib.Path(f"E:/s/w1x{n}-PB9{n}-report.md"), 1, f"w1x{n}", f"PB9{n}", "DONE", [f"PB9{n}"], False,
                      f"worktree-k{n}", f"E:/w/k{n}", f"abc{n}0000", review)


    _REPLAY = SimpleNamespace(by=(("worktree-held", 2),), fresh=("abcdef123456",), carrier=None)
    PRED_KINDS = {   # one builder call each; n names the branch, so two calls never share a fact
        "branch with no report": lambda n: finisher_pred(None, f"worktree-k{n}", "UNLANDED", [f"PB9{n}"]),
        "... and replayed commits": lambda n: finisher_pred(None, f"worktree-k{n}", "UNLANDED", [f"PB9{n}"], replay=_REPLAY),
        "half note": lambda n: FINISH_FIRST + half_clause([f"PB9{n}"]),
        "half note on a branch": lambda n: FINISH_FIRST + half_clause([f"PB9{n}"]) + " " + branch_resume(f"worktree-k{n}", [f"PB9{n}"]),
        "UNLANDED report": lambda n: finisher_pred(_pred_report(n), f"worktree-k{n}", "UNLANDED", [f"PB9{n}"]),
        "... with replayed commits": lambda n: finisher_pred(_pred_report(n), f"worktree-k{n}", "CHECK", [f"PB9{n}"], replay=_REPLAY),
        "... carried": lambda n: finisher_pred(_pred_report(n), f"worktree-k{n}", "UNLANDED", [f"PB9{n}"], carried_by=f"worktree-c{n}"),
        "LANDED report": lambda n: finisher_pred(_pred_report(n), f"worktree-k{n}", "LANDED", [f"PB9{n}"]),
        "FINDINGS report": lambda n: finisher_pred(_pred_report(n, Review("FINDINGS", 1, f"w1x{n}-review-r1", f"abc{n}0000", f"E:/s/rv{n}.jsonl")),
                                                    f"worktree-k{n}", "UNLANDED", [f"PB9{n}"]),
        "FINDINGS report, carried": lambda n: finisher_pred(_pred_report(n, Review("FINDINGS", 1, f"w1x{n}-review-r1", f"abc{n}0000", f"E:/s/rv{n}.jsonl")),
                                                 f"worktree-k{n}", "UNLANDED", [f"PB9{n}"], carried_by=f"worktree-c{n}"),
    }
    bad_joins = []
    for ka, mk_a in PRED_KINDS.items():
        for kb_, mk_b in PRED_KINDS.items():
            pa, pb_ = mk_a(1), mk_b(2)
            both = join_preds(pa, pb_)
            had = {s for s in once_rule.sentences(pa + " " + pb_) if len(s) >= once_rule.SENTENCE_MIN and " " in s}
            got = {s for s in once_rule.sentences(both) if len(s) >= once_rule.SENTENCE_MIN and " " in s}
            if once_rule.repeated_sentences(both) or had != got or not all(b in both for b in ("PB91", "PB92")):
                bad_joins.append((ka, kb_))
    expect.equal("PB4419: joining any two kinds of pred states every sentence once and loses none", (len(PRED_KINDS), bad_joins), (10, []))
    expect.equal("... and a joined FINDINGS pred ends with the answer rule, whatever it was joined with",
          [k for k, mk in PRED_KINDS.items() if k.startswith("FINDINGS")
           and not join_preds(PRED_KINDS["branch with no report"](1), mk(2)).endswith(FINDINGS_ANSWER)], [])
    expect.equal("... and every rule a builder declares is stated by some kind of pred above, so the drift test reaches it",
          [r[:40] for r in shared_rules() if r != DRAIN_CLAUSE and not any(r in mk(1) for mk in PRED_KINDS.values())], [])


def case_099_pb4419_r1_1_a_check_or_unlanded_branch_s(expect):
    global one
    # Review R1-1 of wave 20261821 b: moving a rule to the end of a joined pred must leave it covering the class its fact names. Expected
    # from the planner's own arms: finisher_pred appends UNLANDED_RULE for each class in CARRIED_CLASSES (CHECK, an unlanded fix to
    # existing files, is carried like UNLANDED), so the fact says "is <class>" and the rule, alone or joined, names that class; a LANDED
    # branch gets LANDED_RULE and never the cherry-pick rule. The text has no doubled space whatever the replay (None had left one).
    bad_cover = []
    for cls in CARRIED_CLASSES:
        for rep_ in (None, _pred_report(1)):
            for rp in (None, _REPLAY):
                one = finisher_pred(rep_, "worktree-k1", cls, ["PB91"], replay=rp)
                joined = join_preds(one, PRED_KINDS["LANDED report"](2))
                if not (UNLANDED_RULE in joined or RESUME_RULE in joined) or (rep_ and (f"is {cls}" not in joined or cls not in UNLANDED_RULE)):
                    bad_cover.append((cls, rep_ is None, rp is None))
    expect.equal("PB4419 R1-1: a CHECK or UNLANDED branch's fact and the cherry-pick rule that follows it, joined or not, name the same class",
          (CARRIED_CLASSES, bad_cover), (("UNLANDED", "CHECK"), []))
    expect.equal("... and a LANDED branch gets the landed rule and never the cherry-pick rule",
          (LANDED_RULE in PRED_KINDS["LANDED report"](1), UNLANDED_RULE in PRED_KINDS["LANDED report"](1)), (True, False))
    expect.equal("... and no kind of pred, nor any two joined, carries a doubled space",
          [k for k, mk in PRED_KINDS.items() if "  " in mk(1) or "  " in join_preds(mk(1), mk(2))], [])


def case_100_pb4419_r1_2_a_joined_pred_whose_facts_re(expect):
    # Review R1-2: a joined pred ends with the rules, after the "cherry-pick ONLY those" of a replayed branch, so each cherry-pick rule says
    # itself that the restriction qualifies it.
    _rep_joined = join_preds(PRED_KINDS["... and replayed commits"](1), PRED_KINDS["... with replayed commits"](2))
    expect.equal("PB4419 R1-2: a joined pred whose facts restrict a branch to ONLY some commits ends with rules that honor the restriction",
          (_rep_joined.count("cherry-pick ONLY those"), _rep_joined.endswith(UNLANDED_RULE),
           all("restricts it to ONLY some commits" in r for r in (RESUME_RULE, UNLANDED_RULE))), (2, True, True))


def case_101_pb4419_a_group_that_absorbed_another_joi(expect):
    # The clauses of the FINISHED group (Group.close_pred): the CONTINUATION onto the finished branches it joined and, under
    # --drain, the DRAIN clause. A group that absorbs another used to carry each of its own, so both were stated twice.
    cg1 = dataclasses.replace(rg, kind="finisher", pred=PRED_KINDS["UNLANDED report"](1), joins={}, continues=[])
    cg2 = dataclasses.replace(rg_other, kind="finisher", pred=PRED_KINDS["UNLANDED report"](2), joins={}, continues=[])
    cg1.join(_pred_report(3), ["a.cs"])
    cg2.join(_pred_report(3), ["b.cs"])
    cg2.join(_pred_report(4), ["c.cs"])
    cg1.absorb(cg2)
    cg1.close_pred(True)
    expect.equal("PB4419: a group that absorbed another joins each finished branch once, each file shared once, and says CONTINUATION and DRAIN once",
          (sorted(cg1.joins), cg1.joins["worktree-k3"][1], cg1.continues, cg1.pred.count("CONTINUATION ("), cg1.pred.count("DRAIN ("),
           once_rule.repeated_sentences(cg1.pred), "worktree-k3" in cg1.pred and "worktree-k4" in cg1.pred),
          (["worktree-k3", "worktree-k4"], ["a.cs", "b.cs"], ["worktree-k3", "worktree-k4"], 1, 1, [], True))


def case_102_pb4419_the_drain_plan_s_absorbed_finishe(expect):
    # The planner end to end: the drain plan's group of two finishers whose claims meet (PB4396), and the fix lane's two groups on
    # the files of one finished branch, state no sentence twice.
    dr_pair = next(g for g in pd["groups"] if sorted(g.notes) == ["PB6", "PB7"])
    expect.equal("PB4419: the drain plan's absorbed finisher group states DRAIN once and no sentence twice",
          (dr_pair.pred.count("DRAIN ("), once_rule.repeated_sentences(dr_pair.pred)), (1, []))
    expect.equal("join_preds drops a sentence stated twice that no builder declared a rule, and collapses whitespace",
          join_preds("A  b.", "", "Do the one thing the old way, then stop and read the report in full.", "C.",
                     "Do the one thing the old way, then stop and read the report in full."),
          "A b. Do the one thing the old way, then stop and read the report in full. C.")


def case_103_a_spec_rendered_without_them_is_refused(expect):
    nospec = TMP / "msg-w77-req-without.txt"
    nospec.write_text(mds.render(CFG77, gjson[0], FILES(gjson[0])), encoding="utf-8", newline="\n")
    expect.equal("a spec rendered without them is refused, naming each requirement",
          check_practices.specs_missing_requirements(rfile, [nospec]),
          [(nospec.name, r) for r in REQS + ["Second campaign's own rule."]])
    expect.equal("a groups file that declares none refuses nothing", check_practices.specs_missing_requirements(gfile, [nospec]), [])


def case_104_each_spec_is_checked_against_its_own_gro(expect):
    # a spec named for another group is checked against THAT group's requirements only (make_dispatch_specs.py's name)
    other_j = dict(as_group_json(rg_other, "77", notes, "COBOL0004-COBOL0006"), letter="Z")
    rj_named = dict(rj, letter="Y")
    mfile = TMP / "req-groups-two.json"
    mfile.write_text(JSON.dumps(dict(CFG77, groups=[rj_named, other_j])), encoding="utf-8", newline="\n")
    yspec = TMP / "msg-w77-y.txt"
    yspec.write_text(mds.render(CFG77, rj_named, FILES(rj_named)), encoding="utf-8", newline="\n")
    zspec = TMP / "msg-w77-z.txt"
    zspec.write_text(mds.render(CFG77, other_j, FILES(other_j)), encoding="utf-8", newline="\n")
    expect.equal("each spec is checked against its own group's requirements, not every group's",
          check_practices.specs_missing_requirements(mfile, [yspec, zspec]), [])


def case_105_mandatory_practices_i11_carries_the_owne(expect):
    global OWNER_SENTENCE
    # The owner's SORT and file I/O test sentence (2026-10-17 ~22:25) has ONE home, MANDATORY-PRACTICES I11, which every
    # rendered spec and finisher brief points at (kb/Work PB3369). A campaign lead's `## Standing requirements` holds
    # campaign-specific cases only: a copy of the sentence in a lead is a second home that drifts from the first.
    OWNER_SENTENCE = "out test cases for sort and file i/o must cover"
    I11_ROW = next(l for l in (coord.REPO / ".claude" / "skills" / "workstream" / "templates" / "MANDATORY-PRACTICES.md")
                   .read_text(encoding="utf-8").splitlines() if l.startswith("| I11 |"))
    expect.equal("MANDATORY-PRACTICES I11 carries the owner's SORT and file I/O sentence", OWNER_SENTENCE in " ".join(I11_ROW.lower().split()), True)


def case_106_no_campaign_lead_note_s_standing_require(expect):
    lead_copies = sorted(f.stem for f in (coord.REPO / "kb" / "Work").glob("PB*.md")
                         if any(OWNER_SENTENCE in " ".join(r.lower().split()) or "abnormal exception cases" in r
                                for r in campaign_requirements(f.read_text(encoding="utf-8"))))
    expect.equal("no campaign lead note's Standing requirements restates the owner's SORT and file I/O sentence (kb/Work PB3369)",
          lead_copies, [])


def case_107_the_pb3330_note_states_the_membership_ru(expect):
    global real_reqs
    # the lead note the operator reads says what the planner does: binding by membership (review R2-1), never by the --cluster mode
    real_reqs = campaign_requirements((coord.REPO / "kb" / "Work" / "PB3330.md").read_text(encoding="utf-8"))
    lead_text = (coord.REPO / "kb" / "Work" / "PB3330.md").read_text(encoding="utf-8")
    expect.equal("the PB3330 note states the membership rule the planner implements, not the superseded --cluster-only rule",
          ("groups.group_requirements" in lead_text, "Read by `plan_wave.py --cluster" in lead_text), (True, False))
    expect.equal("the PB3330 campaign note still carries the keyed engine's own cases, so the binding test below is not vacuous",
          len(real_reqs) > 0, True)


def case_108_a_real_pb3330_member_planned_by_a_fix_la(expect):
    # a fix-lane plan reaches them through the notes' cluster lists: PB3330's members are bound outside a --cluster wave
    real_items = work.load()
    member = next(i["id"] for i in real_items if isinstance(i.get("cluster"), list) and "PB3330" in i["cluster"] and i["id"] != "PB3330")
    fg = Group(kind="cluster", notes=[member], file="", files=[], letter="Q", model="sonnet", why_model="default")
    expect.equal("a real PB3330 member planned by a fix-lane wave (no --cluster) is bound by the campaign's own requirements",
          group_requirements([fg], {i.get("id"): i.get("cluster") for i in real_items}, coord.REPO / "kb" / "Work"),
          [{"PB3330": real_reqs}])


def case_109_plan_wave_py_binds_groups_by_their_notes(expect):
    # the planner writes them per group into groups.json and the args file, and the rolling wave reads them into the
    # REVIEWER's prompt and names the group's dispatch spec there: the halves are pinned in source, since the workflow has
    # no process API to run here
    PW_SRC = (fixtures.HERE / "plan_wave.py").read_text(encoding="utf-8")
    WF_SRC = (coord.REPO / ".claude" / "skills" / "workstream" / "templates" / "wf_rolling_wave.js").read_text(encoding="utf-8")
    REVIEW_FN = WF_SRC[WF_SRC.index("function reviewBranch("):WF_SRC.index("function fixFindings(")]
    expect.equal("plan_wave.py binds groups by their notes' campaigns, never by the planning mode",
          ("group_requirements(p[\"groups\"]" in PW_SRC, "campaign_requirements(lead_note" in PW_SRC), (True, False))
    expect.equal("plan_wave.py carries each group's requirements into the args file",
          '"after", "requirements")' in PW_SRC, True)
    expect.equal("wf_rolling_wave.js reads the group's requirements into the reviewer prompt",
          ("g.requirements" in WF_SRC, "args.requirements" in WF_SRC), (True, False))
    expect.equal("the branch reviewer's prompt carries the requirements and names the group's dispatch spec",
          ("reviewerRequirements(g)" in REVIEW_FN, "msg-w${W}-${g.letter.toLowerCase()}.txt" in REVIEW_FN), (True, True))
    expect.equal("the review finisher is pointed at the same dispatch spec (its body carries the requirements)",
          "msg-w${W}-${g.letter.toLowerCase()}.txt" in WF_SRC[WF_SRC.index("function fixFindings("):], True)


def case_110_two_chains_wait(expect):
    global after5, lone
    # 9b. plan() alone: a dependent whose blockers sit in two chains (they did not merge: over the cap) waits, and so does
    #     one whose blocker is planned in no group; a dependent on one blocker becomes its chain's successor
    dep = cl("src/X/Dep.cs", "PB11")
    dep["depth"] = 1
    lone = cl("src/X/Lone.cs", "PB10")
    lone["depth"] = 1
    after5 = cl("src/X/After.cs", "PB13")
    after5["depth"] = 1
    dp = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), dep, lone, after5], [], [], lambda b: "ABSENT", {},
                 RULES, budget_points=100, deps={"PB11": ["PB5", "PB9"], "PB10": ["PB99"], "PB13": ["PB5"]})
    expect.equal("two chains wait", "different successor chains" in dp["waiting"].get("PB11", ""), True)
    expect.equal("an unplanned blocker waits", "PB99 is not planned" in dp["waiting"].get("PB10", ""), True)
    expect.equal("one blocker: its chain's successor", [(g.letter, g.after, g.notes) for g in dp["groups"]],
          [("A", "", ["PB5"]), ("B", "", ["PB9"]), ("A2", "A", ["PB13"])])


def case_111_a_finisher_keeps_its_own_notes_and_runs(expect):
    # a finisher never absorbs a dependent cluster on its file: the dependent's blocker is planned after the finisher,
    # so absorbing it would hold the finisher itself back
    dep12 = cl(F_EMIT, "PB12")
    dep12["depth"] = 1
    fp_ = pw.plan(notes, [cl(F_BIND, "PB5"), cl(F_EMIT, "PB3"), dep12], [], [r3], lambda b: "LANDED", {}, RULES,
                  budget_points=100, deps={"PB12": ["PB5"]})
    expect.equal("a finisher keeps its own notes and runs", [(g.kind, g.notes) for g in fp_["groups"]][:1], [("finisher", ["PB3"])])


def case_112_with_nothing_in_flight_all_three_run(expect):
    global free
    # 9c. the file-set partition (owner, kb/Work PB2118 question 5, 2026-10-07): a campaign group whose files meet an
    #     in-flight train's file set waits, with the collision named, and so does every group that follows it; a group
    #     that meets nothing runs. Planted collision first: without `busy` the same groups all run.
    free = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT", {}, RULES,
                   budget_points=100, deps={"PB13": ["PB5"]})
    expect.equal("with nothing in flight all three run", [g.notes for g in free["groups"]], [["PB5"], ["PB9"], ["PB13"]])


def case_113_a_group_holding_against_running_work_the(expect):
    bp = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT",
                 {}, RULES, budget_points=100, deps={"PB13": ["PB5"]},
                 busy={"worktree-train-a": running({F_BIND, "src/Other.cs"}, "worktree-train-a", {F_BIND: ["Binder.Bind"]})})
    expect.equal("a group holding against RUNNING work (the same member) waits, the hold named",
          "same-member collision with in-flight work worktree-train-a [RUNNING] (" + F_BIND + "#Binder.Bind)" in bp["waiting"].get("PB5", ""),
          True)
    expect.equal("its successor waits for it", "PB5 is not planned" in bp["waiting"].get("PB13", ""), True)
    expect.equal("a group that meets nothing runs", [g.notes for g in bp["groups"]], [["PB9"]])


def case_114_fix_lane_a_group_meeting_only_a_finished(expect):
    global FG, FIN, fl
    # PB2807: ONE rule for both lanes. The fix lane passes `busy` too (no deps): running work defers a group, a FINISHED
    # branch (a DONE report, no review findings) makes it that branch's CONTINUATION (merge it first, no `after:`), and the
    # group's own work never collides (the branch its pred continues; a dispatch of its own notes).
    FIN = Report(TMP / "w1090c-PB66-report.md", 1090, "w1090c", "PB66", "DONE", ["PB66"], False, "worktree-done",
                    "E:/w/done", "abc1234")
    fl = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), on_members("src/X/Other.cs", ["Other.Run"], "PB11")],
                 [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                 busy={"worktree done (worktree-done)": InFlight({F_BIND}, "worktree-done", finished=FIN),
                       "dispatched w1091a (PB9)": InFlight({"src/X/Misc.cs"}, notes={"PB9"}),
                       "dispatched w1091b (PB40)": InFlight({"src/X/Other.cs"}, notes={"PB40"},
                                                              members={"src/X/Other.cs": frozenset({"Other.Run"})})})
    FG = {tuple(g.notes): g for g in fl["groups"]}
    expect.equal("fix lane: a group meeting only a FINISHED branch is planned as its continuation, no after:",
          (("PB5",) in FG, FG.get(("PB5",)) and FG[("PB5",)].after, FG.get(("PB5",)) and FG[("PB5",)].continues),
          (True, "", ["worktree-done"]))
    expect.equal("... its pred says merge that branch first and names the shared file",
          all(t in FG[("PB5",)].pred for t in ("CONTINUATION", "git merge", "worktree-done", "w1090c", F_BIND)), True)


def case_115_pb4396_two_groups_on_the_files_of_one_fi(expect):
    global fl2
    # kb/Work PB4396: JOIN IN-FLIGHT WORK. Two groups on two files of ONE finished branch are one continuation of it, never
    # two branches behind it: the first takes the branch's claim, so the second meets it; the record reaches groups.json.
    fl2 = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9")], [], [], lambda b: "ABSENT", {}, RULES,
                  budget_points=100,
                  busy={"worktree done (worktree-done)": InFlight({F_BIND, "src/X/Misc.cs"}, "worktree-done", finished=FIN)})
    expect.equal("PB4396: two groups on the files of one finished branch are ONE continuation of it, its record in groups.json",
          ([(g.notes, g.continues) for g in fl2["groups"]],
           [as_group_json(g, "9401", notes, "COBOL0001-COBOL0003").get("continues") for g in fl2["groups"]]),
          ([(["PB5", "PB9"], ["worktree-done"])], [["worktree-done"]]))


def case_116_pb4419_two_fix_lane_groups_planned_onto(expect):
    # kb/Work PB4419: the fix lane planned both groups ONTO the finished branch; each carried its own CONTINUATION, and the
    # group that absorbed the other said it, and every sentence of it, twice.
    fl2_pred = fl2["groups"][0].pred
    expect.equal("PB4419: two fix-lane groups planned onto one finished branch say CONTINUATION once and no sentence twice",
          (fl2_pred.count("CONTINUATION ("), once_rule.repeated_sentences(fl2_pred)), (1, []))
    expect.equal("fix lane: a dispatch of the group's OWN notes never defers it (the hold already decided them)",
          ("PB9",) in FG, True)
    expect.equal("fix lane: a group meeting another dispatch's RUNNING group waits, naming it",
          ("PB11" in fl["waiting"], "in-flight work dispatched w1091b" in fl["waiting"].get("PB11", "")), (True, True))


def case_117_a_finisher_never_collides_with_the_branc(expect):
    fin_own = pw.plan(notes, [cl(F_EMIT, "PB3")], [], [r3], lambda b: "UNLANDED", {}, RULES, budget_points=100,
                      busy={"worktree pred (" + str(r3.branch) + ")": InFlight({F_EMIT}, r3.branch)})
    expect.equal("a finisher never collides with the branch its own pred continues (its predecessor's worktree)",
          [(g.kind, g.notes) for g in fin_own["groups"]][:1], [("finisher", ["PB3"])])


def case_118_classify_inflight_waiting_to_land_contin(expect):
    CI_META = {"worktree a (worktree-done)": {"branch": "worktree-done", "rev": "x"},
               "worktree b (worktree-split)": {"branch": "worktree-split", "rev": "y"},
               "worktree c (worktree-findings)": {"branch": "worktree-findings", "rev": "z"},
               "worktree d (worktree-live)": {"branch": "worktree-live", "rev": "v"},
               "branch worktree-landed": {"branch": "worktree-landed", "rev": "worktree-landed"},
               "dispatched w1092a (PB67)": {"notes": ["PB67"], "at": 1.0, "slug": "w1092a"}}
    SPL = dataclasses.replace(FIN, path=TMP / "w1090d-PB68-report.md", slug="w1090d", lead="PB68", status="SPLIT",
                              notes=["PB68"], branch="worktree-split")
    FND = dataclasses.replace(FIN, path=TMP / "w1090e-PB69-report.md", slug="w1090e", lead="PB69", notes=["PB69"],
                              branch="worktree-findings", review=Review("FINDINGS", 2, "r", "abc1234", "f.jsonl"))
    # the survey's lifecycle states (prune_worktrees.lifecycle) decide; a branch the survey does not name is running
    CI_STATES = {"worktree-done": "WAITING TO LAND", "worktree-split": "DECISION", "worktree-findings": "DECISION",
                 "worktree-live": "IN FLIGHT", "worktree-landed": "LANDED"}
    ci = classify_inflight({o: {"src/A.cs"} for o in CI_META}, CI_META, [FIN, SPL, FND], CI_STATES, {"worktree-live": ["PB70"]})
    expect.equal("classify_inflight: WAITING TO LAND continues; DECISION, IN FLIGHT, a dispatch (answered ones never reach it: "
          "inflight_file_sets drops them, PB2795) and an unnamed owner run; LANDED is gone",
          {o: (x.finished.slug if x.finished else x.state) for o, x in ci.items()},
          {"worktree a (worktree-done)": "w1090c", "worktree b (worktree-split)": "DECISION",
           "worktree c (worktree-findings)": "DECISION", "worktree d (worktree-live)": "IN FLIGHT",
           "dispatched w1092a (PB67)": "DISPATCHED, NO REPORT YET"})
    expect.equal("classify_inflight: a branch's notes are its newest report's ids and its commit subjects' ids",
          (ci["worktree b (worktree-split)"].notes, ci["worktree d (worktree-live)"].notes), ({"PB68"}, {"PB70"}))


def case_119_a_branch_working_on_the_group_s_own_note(expect):
    global own
    own = pw.plan(notes, [cl("src/X/Misc.cs", "PB9")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                  busy={"worktree e (worktree-old)": InFlight({"src/X/Misc.cs"}, "worktree-old", {"PB9"}, state="DECISION")})
    expect.equal("a branch working on the group's OWN note (an earlier attempt) never defers it",
          [g.notes for g in own["groups"]], [["PB9"]])


def case_120_pb4396_r1_2_the_next_wave_plans_an_overf(expect):
    # kb/Work PB4396 review R1-2: THE NEXT WAVE OF AN OVERFLOW CHAIN. Wave N (s7c above) gave group A three notes of
    # src/X/Seven.cs and left four as its overflow; A finished, was reviewed CLEAN, and the survey holds its branch IN FLIGHT
    # for that overflow (prune_worktrees.chain_links, from the dispatch ledger's chain record). Wave N+1 reads the survey as
    # plan_wave.main does (inflight.survey_states) and must plan the overflow ONTO A's branch: read as running work, the
    # overflow's group would wait for A and A for the overflow, every wave, with nothing red.
    OVF_A = s7c["groups"][0]
    OVF_REPORT = dataclasses.replace(FIN, path=TMP / "w9400a-PB6-report.md", wave=9400, slug="w9400a", lead=OVF_A.notes[0],
                                     notes=list(OVF_A.notes), branch="worktree-seven", worktree="E:/w/seven")
    OVF_ROWS = [prune.Row("worktree-seven", "UNLANDED", [], [], 3, "E:/w/seven", None, 0, 600.0, "x", state="WAITING TO LAND",
                          report=prune.IndexedReport(OVF_REPORT.path, "DONE", OVF_REPORT.lead, False, 600.0, finished=True))]
    prune.chain_links(OVF_ROWS, [prune.ChainRecord("w9400a", tuple(OVF_A.notes), (), tuple(OVF_A.overflow))])
    OVF_OWNER = "worktree seven (worktree-seven)"


    def next_wave(states):
        busy = classify_inflight({OVF_OWNER: {"src/X/Seven.cs"}}, {OVF_OWNER: {"branch": "worktree-seven", "rev": "x"}},
                                 [OVF_REPORT], states, {})
        busy[OVF_OWNER].members = {"src/X/Seven.cs": frozenset({"Seven.Run"})}   # as inflight_units reads its diff
        return pw.plan(notes, [c for c in SEVEN if c["notes"][0]["id"] in OVF_A.overflow], [], [], lambda b: "ABSENT", {},
                       rules_capped(LIVE_RULES, 3), budget_points=100, busy=busy)


    OVF_NEXT = next_wave(inb.survey_states(OVF_ROWS))
    expect.equal("PB4396 R1-2: the next wave plans an overflow ONTO the chain link the survey holds for it, never waits on it",
          ((OVF_ROWS[0].state, OVF_ROWS[0].overflow), [(g.notes, g.continues) for g in OVF_NEXT["groups"]],
           sorted(i for i, w in OVF_NEXT["waiting"].items() if "in-flight work" in w)),
          (("IN FLIGHT", tuple(OVF_A.overflow)), [(list(OVF_A.overflow[:3]), ["worktree-seven"])], []))
    expect.equal("PB4396 R1-2: ... and the arm can fail: read as the bare IN FLIGHT, every overflow note waits on the link",
          sorted((i for i, w in next_wave({r.name: r.state for r in OVF_ROWS})["waiting"].items() if "worktree-seven" in w),
                 key=pw.work.id_order), sorted(OVF_A.overflow, key=pw.work.id_order))
    expect.equal("the one collision rule, path separators folded",
          collided(Claim.of(["src" + chr(92) + "A.cs"]),
                                 {"t": Claim.of({"src/A.cs"}), "u": Claim.of({"src/B.cs"})}), ["t (src/A.cs)"])


def case_121_a_dispatched_worktree_with_no_commit_is(expect):
    global NL, _R, body, nid, rel
    # 9d. the in-flight set reads sources a branch CLASSIFICATION cannot drop (PB2118 Draft 7, the sixth refuter's
    #     partition lead): a dispatched worktree with no commit (classifies MERGED) whose agent has edited a file, an
    #     unlanded branch that only edits existing files (classifies LANDED), and a dispatched group with no worktree yet
    #     are all in flight; a branch landed by content and a dispatched group whose notes are all landed are not.
    #     A fake git (prune_worktrees.git's shape) stands in for the repository, so no git runs.
    NL = chr(10)


    class _R:
        def __init__(self, out="", rc=0):
            self.stdout, self.returncode = out, rc


    # every agent worktree has the harness's local settings modified (kb/Work PB3465): both fake worktrees do
    SETTINGS_FILE = ".claude/settings.local.json"


    STATUS_ARGS = ("--no-optional-locks", "status", "--porcelain", "-z", "--no-renames", "--untracked-files=all")
    DIFF_TREE_ARGS = ("diff-tree", "--stdin", "--always", "--root", "-r", "--no-renames", "--name-only")
    # What each (commit, parent) pair changes: origin/main is m0; a worktree's or branch's merge base with it x0.
    FAKE_PAIRS = {("aaa", "m0"): [], ("bbb", "b0"): ["src/Existing.cs"], ("bbb", "m0"): ["src/Existing.cs"],
                  ("ccc", "c0"): ["src/Landed.cs"], ("ccc", "m0"): []}


    def fake_git(*args, cwd=None, stdin=None):
        """The in-flight reader's git, as it asks it since kb/Work PB3582: one status per worktree, one listing of the
    unmerged branches, one merge base per commit and ONE diff-tree for every pair."""
        key = (str(cwd).replace(chr(92), "/"), args)
        if key == ("/r", DIFF_TREE_ARGS):
            return _R("".join(f"{c}{NL}" + "".join(f"{n}{NL}" for n in FAKE_PAIRS[(c, p)])
                              for c, p in (ln.split() for ln in stdin.splitlines())))
        table = {
            ("/r", ("worktree", "list", "--porcelain")): _R(NL.join([
                "worktree /r", "HEAD m0", "branch refs/heads/main", "",
                "worktree /w/fresh", "HEAD aaa", "branch refs/heads/worktree-fresh", "",
                "worktree /w/edits", "HEAD bbb", "branch refs/heads/worktree-edits", ""])),
            ("/w/fresh", STATUS_ARGS): _R(chr(0).join([" M src/Fresh.cs", " M " + SETTINGS_FILE, "?? STATUS.md", ""])),
            ("/w/edits", STATUS_ARGS): _R(chr(0).join([" M " + SETTINGS_FILE, ""])),
            ("/r", ("for-each-ref", "--no-merged=refs/remotes/origin/main", "--format=%(refname:short) %(objectname)",
                    "refs/heads")): _R(NL.join(["worktree-fresh aaa", "worktree-edits bbb", "squashed ccc"])),
            ("/r", ("rev-parse", "--verify", "--quiet", "refs/remotes/origin/main^{commit}")): _R("m0" + NL),
            ("/r", ("merge-base", "m0", "aaa")): _R("m0" + NL),
            ("/r", ("merge-base", "m0", "bbb")): _R("b0" + NL),
            ("/r", ("merge-base", "m0", "ccc")): _R("c0" + NL),
        }
        if key not in table:
            raise AssertionError(f"unexpected git call {key}")
        return table[key]


    LEDGER = TMP / "coord"
    LEDGER.mkdir()
    coord.write_json(LEDGER / coord.DISPATCH_LEDGER, {"groups": [
        {"wave": "1040", "letter": "a", "notes": ["PB70"], "files": ["src/Dispatched.cs"]},
        {"wave": "1039", "letter": "b", "notes": ["PB71"], "files": ["src/Done.cs"]}]})
    fly = inflight_file_sets(git=fake_git, repo=pathlib.Path("/r"), coord_dir=LEDGER,
                                terminal=lambda i: i == "PB71")
    flat = {f for fs in fly.values() for f in fs}
    expect.equal("a dispatched worktree with no commit is in flight (its uncommitted edit)", "src/Fresh.cs" in flat, True)
    expect.equal("an unlanded branch that only edits existing files is in flight", "src/Existing.cs" in flat, True)
    expect.equal("a dispatched group with no worktree yet is in flight", "src/Dispatched.cs" in flat, True)
    expect.equal("a branch landed by content is not", "src/Landed.cs" in flat, False)
    expect.equal("a dispatched group whose notes all landed is not", "src/Done.cs" in flat, False)
    expect.equal("STATUS.md is never a product file", "STATUS.md" in flat, False)
    expect.equal("the harness's settings file is in no owner's set, so a finisher continuing one worktree meets no other on it "
          "(kb/Work PB3465: wave 1134 held PB3337, PB3460 and PB1278 on it)",
          (SETTINGS_FILE in flat, collided(Claim.of([SETTINGS_FILE, "src/Mine.cs"]),
                                                        {o: Claim.of(fs) for o, fs in fly.items()})), (False, []))
    expect.equal("coord.NEVER_WORK: the settings file, STATUS.md and everything under TestResults/ are never work; a path that "
          "merely contains one of them is",
          [coord.is_work(x) for x in (SETTINGS_FILE, SETTINGS_FILE.replace("/", chr(92)), "STATUS.md",
                                      "TestResults/a/b.trx", "src/STATUS.md", ".claude/settings.json", "docs/TestResults.md")],
          [False, False, False, False, True, True, True])
    expect.equal("coord.porcelain_work keeps only the work lines of `git status --porcelain` (a rename by its new path)",
          coord.porcelain_work([" M " + SETTINGS_FILE, "?? STATUS.md", " M src/A.cs", "R  src/Old.cs -> STATUS.md",
                                "R  STATUS.md -> src/New.cs", ""]),
          [" M src/A.cs", "R  STATUS.md -> src/New.cs"])
    expect.equal("the planner's own checkout is not in flight", any("main" in k for k in fly), False)
    expect.equal("the collision names the dispatched owner",
          collided(Claim.of(["src/Dispatched.cs"]), {o: Claim.of(fs) for o, fs in fly.items()}),
          ["dispatched w1040a (PB70) (src/Dispatched.cs)"])

    # 9e. the group's file set (kb/Work PB3242; PB2118 Draft 8, the seventh refuter's J2): its notes' DECLARED sets, the
    #     structured sites (a bare type name in prose, "GammaState's", declares nothing) plus the callers the member index
    #     computes for the note (an injected function here; member_index.py's own self-test drives the computation), plus
    #     the primary file; and the dispatch ledger has STRUCTURED writers only (kb/Work PB3186):
    #     record_dispatch upserts on (wave, letter) and drops the retired hand entries, and make_dispatch_specs.py records
    #     every group it renders with its `file_set` list (or the set computed from its notes when a hand-built groups
    #     file carries none).
    if fc:
        C2 = TMP / "campaign2"
        for rel in ("src/Lib/Alpha.cs", "src/Lib/GammaState.cs", "src/Lib/Beta.cs", "src/Lib/Caller.cs"):
            (C2 / rel).parent.mkdir(parents=True, exist_ok=True)
            (C2 / rel).write_text("// fixture" + NL, encoding="utf-8", newline="\n")
        W2 = C2 / "kb" / "Work"
        W2.mkdir(parents=True)
        for nid, body in (("PB950", f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}{NL}Fold GammaState's flags into it."),
                          ("PB951", f"**Sites:**{NL}- `src/Lib/Beta.cs`{NL}{NL}Move its members (callers computed).")):
            (W2 / f"{nid}.md").write_text(
                f'---{NL}title: "{nid} — step"{NL}id: {nid}{NL}kind: analysis{NL}status: open{NL}area: architecture{NL}'
                f"wrong_answer: false{NL}process_only: true{NL}blocked_by: []{NL}cluster: [PB950]{NL}---"
                f"{NL}{NL}# {nid}{NL}{NL}{body}{NL}", encoding="utf-8", newline="\n")
        items2 = [dict(work.parse_frontmatter(q.read_text(encoding="utf-8")), _file=q.name) for q in sorted(W2.glob("*.md"))]
        view2 = work.cluster_order(items2, "PB950")
        notes2 = load_notes(W2, {"wrong_answer": 8})
        texts2 = {i: (W2 / f"{i}.md").read_text(encoding="utf-8") for i in notes2}
        D2 = declared_sets(C2, IndexPolicy(TMP / "no-cache", regenerate=False))
        o2, _, _, _ = campaign_clusters(view2, notes2, texts2, RULES, fc, C2,
                                           lambda text: D2(text) | (Claim.of({"src/Lib/Caller.cs"}) if "PB951" in text
                                                                             else Claim()))
        files2 = {n["id"]: c["files"] for c in o2 for n in c["notes"]}
        expect.equal("a bare type name in prose declares nothing (PB3242)", "src/Lib/GammaState.cs" in files2["PB950"], False)
        expect.equal("a note's computed callers are in its group's file set", "src/Lib/Caller.cs" in files2["PB951"], True)
        expect.equal("callers never steer the clustering (the primary file is a named site)",
              sorted(c["file"] for c in o2), ["src/Lib/Alpha.cs", "src/Lib/Beta.cs"])
        expect.equal("the collision check sees a caller", collided(Claim.of(files2["PB951"]),
                                                        {"train t": Claim.of({"src/Lib/Caller.cs"})}),
              ["train t (src/Lib/Caller.cs)"])


def case_122_a_retired_hand_entry_is_no_dispatch_not(expect):
    global HL
    HL = TMP / "coord-hand"
    HL.mkdir()
    coord.write_json(HL / coord.DISPATCH_LEDGER, {"groups": [
        {"wave": "hand", "letter": "", "label": "cobol-clerk: x", "hand": True, "notes": ["PB961"], "files": ["src/Junk.cs"],
         "at": 1.0},
        {"wave": "1060", "letter": "A", "notes": ["PB962"], "files": ["src/Old.cs"], "at": 2.0}]})
    expect.equal("a retired hand entry is no dispatch: not in flight, not held, no wave number",
          ({f for fs in inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs},
           held_by_dispatch(HL, [], lambda i: False), pw.alloc.peek("wave", TMP / "no-devlog", HL)),
          ({"src/Old.cs"}, {"PB962": "dispatch w1060a"}, 1061))


def case_123_record_dispatch_drops_the_retired_hand_e(expect):
    global LG, again, first
    first = record_dispatch(HL, "1061", [{"letter": "A", "notes": ["PB960"], "files": ["src" + chr(92) + "Hand.cs"]},
                                            {"letter": "B", "notes": ["PB963"], "files": ["src/B.cs"]}], lambda i: False)
    again = record_dispatch(HL, "1061", [{"letter": "a", "notes": ["PB960"], "files": ["src/Hand.cs", "src/More.cs"]}],
                               lambda i: False)
    LG = coord.read_json(HL / coord.DISPATCH_LEDGER, {})["groups"]
    expect.equal("record_dispatch drops the retired hand entry and keeps the other waves", [(g["wave"], g["letter"]) for g in LG],
          [("1060", "A"), ("1061", "B"), ("1061", "a")])
    expect.equal("record_dispatch UPSERTS on (wave, letter): the re-recorded group replaces its entry, its files exact, POSIX",
          [g["files"] for g in LG if g["wave"] == "1061" and g["letter"].upper() == "A"], [["src/Hand.cs", "src/More.cs"]])
    expect.equal("each entry carries its report slug", [g["slug"] for g in first + again], ["w1061a", "w1061b", "w1061a"])
    # PB3392: a re-rendered spec keeps its group's dispatch time (`at`, the order the landing check and `answers` read)
    expect.equal("PB3392: the group recorded again keeps its first `at`; the group beside it keeps its own",
          (again[0]["at"] == first[0]["at"], first[0]["at"] < first[1]["at"]), (True, True))


def case_124_pb3392_a_re_rendered_early_dispatch_is_s(expect):
    global HA
    HA = TMP / "coord-at"
    HA.mkdir()
    coord.write_json(HA / coord.DISPATCH_LEDGER, {"groups": [
        {"wave": "1118", "letter": "A5", "notes": ["PB3330"], "files": ["src/Engine.cs"], "slug": "w1118a5", "at": 5.0},
        {"wave": "1126", "letter": "C", "notes": ["PB1587"], "files": [], "slug": "w1126c", "at": 14.0}]})
    rerender = record_dispatch(HA, "1118", [{"letter": "A5", "notes": ["PB3330", "PB3331"], "files": ["src/Engine.cs", "src/More.cs"]},
                                               {"letter": "A7", "notes": ["PB3332"], "files": ["src/New.cs"]}], lambda i: False)
    AT = {(g["wave"], g["letter"]): g for g in coord.read_json(HA / coord.DISPATCH_LEDGER, {})["groups"]}
    expect.equal("PB3392: a re-rendered early dispatch is still EARLIER than a later wave's group, though its files and notes grew",
          (AT[("1118", "A5")]["at"], AT[("1118", "A5")]["files"], AT[("1118", "A5")]["notes"], AT[("1118", "A5")]["at"] < AT[("1126", "C")]["at"]),
          (5.0, ["src/Engine.cs", "src/More.cs"], ["PB3330", "PB3331"], True))
    expect.equal("PB3392: a NEW group of an existing wave takes the time of its first recording; other waves' entries are untouched",
          (AT[("1118", "A7")]["at"] > 14.0, AT[("1126", "C")]["at"], [g["at"] for g in rerender] == [AT[("1118", "A5")]["at"], AT[("1118", "A7")]["at"]]),
          (True, 14.0, True))


def case_125_pb3392_a_group_that_shares_no_note_with(expect):
    different = record_dispatch(HA, "1118", [{"letter": "A5", "notes": ["PB9999"], "files": ["src/Other.cs"]}], lambda i: False)
    expect.equal("PB3392: a group that shares no note with the entry under its key is a DIFFERENT dispatch and takes this recording's time",
          different[0]["at"] > 14.0, True)


def case_126_pb4190_each_group_records_the_lane_that(expect):
    # PB4190: the dispatching lane is recorded with the group, so a stall reaches the lane that can act on it
    HX = TMP / "coord-lane"
    HX.mkdir()
    record_dispatch(HX, "1119", [{"letter": "A", "notes": ["PB3340"], "files": ["src/L.cs"]},
                                 {"letter": "B", "notes": ["PB3341"], "files": ["src/M.cs"]}], lambda i: False, lane="mythos")
    record_dispatch(HX, "1120", [{"letter": "A", "notes": ["PB3342"], "files": ["src/N.cs"]}], lambda i: False, lane="operator")
    record_dispatch(HX, "1119", [{"letter": "A", "notes": ["PB3340"], "files": ["src/L.cs"]}], lambda i: False, lane="operator")
    record_dispatch(HX, "1121", [{"letter": "A", "notes": ["PB3343"], "files": ["src/O.cs"]}], lambda i: False)
    LX = {(g["wave"], g["letter"]): g.get("lane") for g in coord.ledger_groups(HX)}
    expect.equal("PB4190: each group records the lane that dispatched it, none when unrecorded; the other lane's re-render of "
          "a group keeps its first lane",
          LX, {("1119", "A"): "mythos", ("1119", "B"): "mythos", ("1120", "A"): "operator", ("1121", "A"): None})
    expect.equal("a recorded group is in flight before its agent edits a file, until its notes land",
          ({f for fs in inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs}
           >= {"src/Hand.cs", "src/More.cs"},
           inflight_file_sets(coord_dir=HL, terminal=lambda i: True, with_git=False)), (True, {}))


def case_127_an_answered_group_its_report_exists_spli(expect):
    global HL_REPORTS, hl_report
    HL_REPORTS = TMP / "reports-hl"
    HL_REPORTS.mkdir()


    def hl_report(name, status, when=None, sub=""):
        """A real report file (the answered rule reads its mtime), `when` seconds since the epoch or now."""
        path = HL_REPORTS / sub / name
        path.parent.mkdir(exist_ok=True)
        path.write_text(f"Status: {status}\n# {name}\n**branch:** `worktree-wf_hl`\n\n## X\n", encoding="utf-8", newline="\n")
        if when is not None:
            os.utime(path, (when, when))
        return parse_report(path)


    HL_AT = {g["letter"]: g["at"] for g in LG}
    expect.equal("an ANSWERED group (its report exists, SPLIT residue included) holds no declared file, for every reader (PB2795)",
          {f for fs in inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False,
                                             reports=[hl_report("w1061b-PB963-report.md", "SPLIT", HL_AT["B"] + 60)]).values()
           for f in fs} & {"src/B.cs", "src/Hand.cs"}, {"src/Hand.cs"})


def case_128_a_reviewer_s_stamp_on_another_dispatch_s(expect):
    # kb/Work PB3311 hole 2 (with PB3152's second-predicate witness): ONE predicate (`answers`) says "a dispatch returned"
    # for the notes' hold AND the group's file set: the group's OWN report (its slug), written at or after the dispatch.
    # A reviewer's stamp on ANOTHER dispatch's report names the same note and bumps its mtime past the dispatch, and a
    # header that merely mentions the note says nothing about it: neither answers (the dispatch is the review finisher
    # `w1101ar1` of PB3198, whose original report `w1101a` the branch reviewer re-stamped after the dispatch).
    RV = TMP / "coord-review"
    RV.mkdir()
    record_dispatch(RV, "1101", [{"letter": "AR1", "notes": ["PB3198"], "files": ["src/Review.cs"]}], lambda i: False)
    RV_AT = coord.read_json(RV / coord.DISPATCH_LEDGER, {})["groups"][0]["at"]


    def rv_state(*reports):
        held = held_by_dispatch(RV, list(reports), lambda i: False)
        files = {f for fs in inflight_file_sets(coord_dir=RV, terminal=lambda i: False, with_git=False,
                                                   reports=list(reports)).values() for f in fs}
        return held, files


    STAMPED = hl_report("w1101a-PB3198-report.md", "DONE", RV_AT + 600)           # the reviewed report, stamped after
    MENTION = hl_report("w1102z-PB3203-report.md", "DONE", RV_AT + 600)           # its header names PB3198 below
    EARLIER = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT - 3600, "earlier")      # an earlier dispatch's, reused slug
    RETURNED = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT + 900)        # the review finisher's own report
    HELD = ({"PB3198": "dispatch w1101ar1"}, {"src/Review.cs"})
    expect.equal("a reviewer's stamp on another dispatch's report, a header that mentions the note, and an earlier dispatch's "
          "report of the slug answer nothing: held and in flight by both readers (PB3311)",
          (rv_state(STAMPED), rv_state(MENTION), rv_state(EARLIER)), (HELD, HELD, HELD))
    expect.equal("the group's own report written since the dispatch answers it for both readers (PB3311)",
          rv_state(STAMPED, RETURNED), ({}, set()))
    expect.equal("a report whose file is gone answers nothing (the safe side of a double dispatch)",
          rv_state(dataclasses.replace(RETURNED, path=HL_REPORTS / "gone-PB3198-report.md")), HELD)


def case_129_an_entry_whose_notes_all_landed_is_dropp(expect):
    record_dispatch(HL, "1062", [], lambda i: i == "PB962")
    expect.equal("an entry whose notes all landed is dropped at the next record",
          [g["wave"] for g in coord.read_json(HL / coord.DISPATCH_LEDGER, {})["groups"]], ["1061", "1061"])


def case_130_make_dispatch_specs_records_every_render(expect):
    global MDS
    MDS = TMP / "coord-mds"
    MDS.mkdir()
    MCFG = {"wave": "1070", "groups": [{"letter": "A", "slug": "w1070a", "notes": "PB970, PB971", "file_set": ["src/M.cs"],
                                        "member_set": {"src/M.cs": ["T.M"]}},
                                       {"letter": "B", "slug": "w1070b", "notes": "PB972"}]}
    mds.record(MCFG, MDS, terminal=lambda i: False, claim_of=lambda ids: Claim.of({f"src/{i}.cs" for i in ids}))
    expect.equal("make_dispatch_specs records every rendered group: its file_set list and member claims, else the set computed "
          "from its notes",
          [(g["slug"], g["notes"], g["files"], g["members"])
           for g in coord.read_json(MDS / coord.DISPATCH_LEDGER, {})["groups"]],
          [("w1070a", ["PB970", "PB971"], ["src/M.cs"], {"src/M.cs": ["T.M"]}), ("w1070b", ["PB972"], ["src/PB972.cs"], {})])


def case_131_and_the_in_flight_enumeration_reads_a_re(expect):
    LM = {}
    expect.equal("... and the in-flight enumeration reads a recorded group's member claims back (kb/Work PB3670)",
          [(inflight_file_sets(coord_dir=MDS, terminal=lambda i: False, with_git=False, meta=LM) and None),
           LM["dispatched w1070a (PB970, PB971)"]["members"]], [None, {"src/M.cs": ["T.M"]}])
    expect.equal("the planner's groups file carries the file set as a LIST", gjson[0]["file_set"], sorted(p["groups"][0].files))


def case_132_meta_carries_a_dispatch_entry_s_notes_ti(expect):
    # 9f. (PB2118 Draft 9) the landing check orders two branches by dispatch time: the in-flight enumeration reports each
    #     dispatch entry's notes and time through `meta`; and the planner never plans on a stale member index
    META = {}
    inflight_file_sets(coord_dir=HL, terminal=lambda i: i != "PB960", with_git=False, meta=META)
    expect.equal("meta carries a dispatch entry's notes, time and slug",
          [(m["notes"], m["at"], m["slug"]) for m in META.values()], [(["PB960"], again[0]["at"], "w1061a")])


def case_133_the_planner_refuses_a_tree_no_member_ind(expect):
    STALE = TMP / "stale-repo"
    (STALE / "src").mkdir(parents=True)
    (STALE / "src" / "A.cs").write_text("// a" + NL, encoding="utf-8", newline="\n")
    ScratchRepo.init(STALE).commit("c")
    try:
        declared_sets(STALE, IndexPolicy(TMP / "no-cache", regenerate=False))("**Moves or changes:** `N.T.M`")
        expect.equal("the planner refuses a tree no member index describes", "planned", "refused")
    except SystemExit:
        expect.equal("the planner refuses a tree no member index describes", "refused", "refused")


def case_134_one_wave_s_groups_never_tie_and_keep_let(expect):
    # 9g. (PB2118 Draft 10, the ninth refuter) every changed-file read passes --no-renames (the fake git above refuses a
    #     diff without it, so 9d already holds the enumeration to it); one wave's groups are recorded strictly apart in
    #     letter order, so no two tie (N4); and an aborted dispatch is released by wave, group or note, never by a clock (N5)
    RL = TMP / "release"
    RL.mkdir()
    record_dispatch(RL, "1050", [{"letter": "a", "notes": ["PB80"], "files": ["src/X.cs"]},
                                    {"letter": "b", "notes": ["PB81"], "files": ["src/Y.cs"]},
                                    {"letter": "c", "notes": ["PB82"], "files": ["src/Z.cs"]}], terminal=lambda i: False)
    ATS = [g["at"] for g in coord.read_json(RL / coord.DISPATCH_LEDGER, {})["groups"]]
    expect.equal("one wave's groups never tie and keep letter order", ATS == sorted(ATS) and len(set(ATS)) == 3, True)
    expect.equal("release by group", [g["letter"] for g in release_dispatch(RL, "1050b")], ["b"])
    expect.equal("release by note", [g["letter"] for g in release_dispatch(RL, "PB82")], ["c"])
    expect.equal("release by wave", [g["letter"] for g in release_dispatch(RL, "1050")], ["a"])
    expect.equal("nothing left to release", release_dispatch(RL, "1050"), [])
    expect.equal("changed_files passes --no-renames",
          changed_files(lambda *a, **k: _R(NL.join(a) + NL) if "--no-renames" in a else _R(""), "x...y"),
          {"diff", "--name-only", "--no-renames", "x...y"})


def case_135_range_names_lists_every_path_of_the_rang(expect):
    # kb/Work PB3791: range_names is the ONE question, every path it lists; changed_files is its work paths
    RANGE = lambda *a, **k: _R(NL.join([".claude/settings.local.json", "src/A.cs", "TestResults/x.trx", "STATUS.md"]) + NL)   # noqa: E731
    expect.equal("range_names lists every path of the range, the paths no landing carries too",
          inb.range_names(RANGE, "x...y"), {".claude/settings.local.json", "src/A.cs", "TestResults/x.trx", "STATUS.md"})
    expect.equal("changed_files is range_names without those paths", changed_files(RANGE, "x...y"), {"src/A.cs"})


def case_136_a_blocker_that_landed_leaves_its_depende(expect):
    global TOPO, probs
    # 10. whether a note is blocked is DERIVED (kb/Work PB3389): work.waiting_on is the one reading, so a blocker that lands
    # changes the dependent's answer with no edit to the dependent, and `check` refuses a stored `blocked:` flag, which could
    # only go stale. Also work.py check's other topology rules: an unknown blocker, a cycle
    TOPO = [{"_file": "T1.md", "id": "T1", "status": "open", "blocked_by": ["T9"], "cluster": []},
            {"_file": "T9.md", "id": "T9", "status": "landed", "blocked_by": [], "cluster": []},
            {"_file": "T2.md", "id": "T2", "status": "open", "blocked_by": ["nobody"], "cluster": []},
            {"_file": "T3.md", "id": "T3", "status": "open", "blocked_by": ["T4"], "cluster": ["T3"]},
            {"_file": "T4.md", "id": "T4", "status": "open", "blocked_by": ["T3"], "cluster": "T3"},
            {"_file": "T5.md", "id": "T5", "status": "open", "blocked_by": ["T3"], "cluster": []},
            {"_file": "T6.md", "id": "T6", "status": "open", "blocked_by": ["T7"], "cluster": ["C"]},
            {"_file": "T7.md", "id": "T7", "status": "open", "blocked_by": ["T6"], "cluster": ["C"]},
            {"_file": "T8.md", "id": "T8", "status": "open", "blocked": False, "blocked_by": [], "cluster": []},
            {"_file": "T10.md", "id": "T10", "status": "owner", "blocked_by": [], "cluster": []},
            {"_file": "T11.md", "id": "T11", "status": "blocked", "blocked_by": ["T9"], "cluster": []},
            {"_file": "T12.md", "id": "T12", "status": "landed", "blocked_by": ["T3"], "cluster": []}]
    TOPO_BY_ID = {i["id"]: i for i in TOPO}
    probs = work.topology_problems(TOPO)
    expect.equal("a blocker that landed leaves its dependent unblocked, with no edit to the dependent",
          work.waiting_on(TOPO_BY_ID["T1"], TOPO_BY_ID), [])
    expect.equal("an open blocker holds its dependent, a landed one does not",
          [work.waiting_on(TOPO_BY_ID[n], TOPO_BY_ID) for n in ("T5", "T2", "T3")], [["T3"], ["nobody"], ["T4"]])
    expect.equal("a hold on something that is no note is a status: owner or blocked, whatever the blockers",
          [work.waiting_on(TOPO_BY_ID[n], TOPO_BY_ID) for n in ("T10", "T11", "T8")], [["status: owner"], ["status: blocked"], []])
    expect.equal("a stored blocked flag is refused, true or false",
          [s.split(":")[0] for s in probs if "`blocked:` is stored" in s], ["T8.md"])
    expect.equal("the planner's cluster view waits on what waiting_on says",
          [(r["id"], r["waiting_on"]) for r in work.cluster_order(TOPO + [dict(TOPO_BY_ID["T5"], id="T13", cluster=["Q"]),
                                                                           dict(TOPO_BY_ID["T10"], id="T14", cluster=["Q"])], "Q")["notes"]],
          [("T13", ["T3"]), ("T14", ["status: owner"])])


def case_137_the_fix_lane_reads_the_open_harmed_notes(expect):
    global FL, bb, flags, kind, nid, status, view
    # the fix lane's population is the ready notes (kb/Work PB3389) the fix lane works (work.fixable, kb/Work PB4951): the
    # planner hands fix_clusters.py a directory of the harmed, not process_only notes that wait on nothing, whatever the kind
    FL = TMP / "fix-lane-register"
    FL.mkdir()
    for nid, status, bb, kind, flags in (("PB801", "open", "", "defect", "wrong_answer: true"),
                                         ("PB802", "open", "PB801", "defect", "wrong_answer: true"),
                                         ("PB803", "open", "PB804", "defect", "crashes: true"),
                                         ("PB804", "landed", "", "defect", "wrong_answer: true"),
                                         ("PB805", "owner", "", "defect", "wrong_answer: true"),
                                         ("PB806", "half", "", "defect", "under_rejects: true"),
                                         ("PB807", "open", "PB802", "defect", "wrong_answer: true"),
                                         ("PB808", "open", "", "analysis", "wrong_answer: true"),
                                         ("PB809", "open", "", "decision", "wrong_answer: false"),
                                         ("PB810", "open", "", "defect", f"under_rejects: true{NL}process_only: true"),
                                         ("PB816", "open", "", "analysis", "wrong_answer: false")):
        (FL / f"{nid}.md").write_text(f"---{NL}id: {nid}{NL}kind: {kind}{NL}status: {status}{NL}area: x{NL}{flags}{NL}"
                                      f"blocked_by: [{bb}]{NL}---{NL}b{NL}", encoding="utf-8", newline="")
    view = ready_notes_view("open", TMP / "fix-lane-view", FL)
    expect.equal("the fix lane reads the open harmed notes that wait on nothing: a landed blocker is no hold, an open one, an owner "
          "status and a chain are; a harmed analysis is in, an unharmed decision or analysis and a process_only defect are not",
          sorted(p.stem for p in view.iterdir()), ["PB801", "PB803", "PB808"])


def case_138_a_ready_note_s_copy_in_the_fix_lane_s_vi(expect):
    # the view's copy of a note carries its addenda, the one file fix_clusters.py reads (kb/Work PB5110, review R1-1)
    (FL / "PB801").mkdir()
    (FL / "PB801" / "2026-11-11-reopen.md").write_text("addendum-only paragraph" + NL, encoding="utf-8", newline="")
    expect.equal("a ready note's copy in the fix lane's view carries its addenda",
          "addendum-only paragraph" in (ready_notes_view("open", TMP / "fix-lane-view-add", FL) / "PB801.md").read_text(encoding="utf-8"), True)


def case_139_a_chain_s_ready_root_joins_the_fix_lane(expect):
    global PQ, extra, nid, pv
    (FL / "PB801" / "2026-11-11-reopen.md").unlink()
    (FL / "PB801").rmdir()
    # a ready process_only prerequisite of a harmed note joins the view with the harm it owes (kb/Work PB4769); one with no
    # harmed dependent stays hidden, and the register's own file is untouched
    PQ = TMP / "prereq-register"
    PQ.mkdir()
    for nid, extra in (("PB811", f"process_only: true{NL}process_only: true"), ("PB812", f"process_only: false{NL}wrong_answer: true{NL}blocked_by: [PB813]"),
                       ("PB813", f"process_only: true{NL}blocked_by: [PB811]"), ("PB814", "process_only: true")):
        (PQ / f"{nid}.md").write_text(f"---{NL}id: {nid}{NL}kind: defect{NL}status: open{NL}area: x{NL}{extra}{NL}---{NL}b{NL}",
                                      encoding="utf-8", newline="")
    pv = ready_notes_view("open", TMP / "prereq-view", PQ)
    expect.equal("a chain's ready root joins the fix lane's view; a waiting link and a process_only note with no harmed dependent "
          "do not", sorted(p.stem for p in pv.iterdir()), ["PB811"])


def case_140_the_view_s_copy_owes_the_harmed_note_s_h(expect):
    global fm
    fm = (pv / "PB811.md").read_text(encoding="utf-8")
    expect.equal("the view's copy owes the harmed note's harm and is no longer process_only",
          ("wrong_answer: true" in fm, "process_only: false" in fm, "process_only: true" in fm), (True, True, False))
    expect.equal("the register's file is untouched", "wrong_answer" in (PQ / "PB811.md").read_text(encoding="utf-8"), False)
    expect.equal("an earlier view is replaced, never merged into", sorted(p.stem for p in ready_notes_view("half", view, FL).iterdir()),
          ["PB806"])
    expect.equal("unknown blocker", any(s.startswith("T2.md: blocked_by names 'nobody'") for s in probs), True)
    expect.equal("cycle", any("cycle among open notes: T3 -> T4 -> T3" in s for s in probs), True)
    expect.equal("cluster must be a list", any(s.startswith("T4.md: cluster must be a list") for s in probs), True)
    expect.equal("a second cycle", any("cycle among open notes: T6 -> T7 -> T6" in s for s in probs), True)
    expect.equal("exactly those five", len(probs), 5)
    expect.equal("a cycle never reads as ready", [(r["id"], r["depth"], r["ready"]) for r in work.cluster_order(TOPO, "C")["notes"]],
          [("T6", None, False), ("T7", None, False)])
    expect.equal("a scalar cluster is no member (no substring match)", [r["id"] for r in work.cluster_order(TOPO, "T3")["notes"]],
          ["T3"])


def case_141_a_malformed_entry_a_campaign_named_twice(expect):
    global TIERS
    # a tier (kb/Work PB3423) is a list of CAMPAIGN:N entries naming the note's own campaigns once; anything else is
    # reported, and only the well-formed entries are read
    TIERS = [{"_file": f"U{k}.md", "id": f"U{k}", "status": "open", "blocked_by": [], "cluster": cl, "tier": t}
             for k, (t, cl) in enumerate([(["C:1"], ["C"]), (["C:12", "D:2"], ["C", "D"]), (["C:0"], ["C"]),
                                           (["C:x"], ["C"]), ("1", ["C"]), (["C:1", "C:2"], ["C"]), (["D:2"], ["C"]),
                                           (["C"], ["C"])])]
    expect.equal("a malformed entry, a campaign named twice, or a campaign the note is not in, is a problem",
          sorted(s.split(":")[0] for s in work.topology_problems(TIERS)), ["U2.md", "U3.md", "U4.md", "U5.md", "U6.md", "U7.md"])
    expect.equal("tier_of reads the well-formed entry for that campaign only",
          [work.tier_of(i, "C") for i in TIERS] + [work.tier_of(TIERS[1], "D"), work.tier_of(TIERS[6], "D")],
          [1, 12, None, None, None, 1, None, None, 2, 2])


def case_142_work_sites_problems_an_open_note_in_a_cl(expect):
    global NEW
    # an open note filed into a cluster from work.SITES_REQUIRED_FROM on declares a site block (kb/Work PB3263): a list of
    # paths, or `**Sites:** none (why)`; an older note, a decision, a terminal note and a note in no cluster are exempt
    NEW = work.SITES_REQUIRED_FROM
    SITED = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": k, "status": s, "cluster": cl, "_body": b}
             for n, k, s, cl, b in [
                 (NEW, "defect", "open", ["C"], "Prose names `src/A.cs` only."),
                 (NEW + 1, "defect", "open", ["C"], "**Sites:**\n- `src/A.cs`\n"),
                 (NEW + 2, "analysis", "half", ["C"], "**Sites:** none (it changes no code file)\n"),
                 (NEW + 3, "decision", "open", ["C"], "An owner question."),
                 (NEW + 4, "defect", "landed", ["C"], "Landed, no sites."),
                 (NEW + 5, "defect", "open", [], "In no cluster."),
                 (NEW - 1, "defect", "open", ["C"], "Filed before the rule."),
                 (NEW + 6, "analysis", "owner", ["C"], "Held by the owner, no sites.")]]
    expect.equal("work.sites_problems: an open note in a cluster from the cutover on with no site block is refused, prose paths "
          "and all; a block naming none is a declaration",
          [s.split(":")[0] for s in work.sites_problems(SITED)], [f"PB{NEW}.md", f"PB{NEW + 6}.md"])
    expect.equal("... and the refusal names the rule and the planner's UNPARTITIONED plan",
          all(t in work.sites_problems(SITED)[0] for t in ("**Sites:**", "UNPARTITIONED", "PB3263")), True)
    expect.equal("note_sites says whether a block is present, whatever it names",
          [work.note_sites(i["_body"]).declared for i in SITED[:3]], [False, True, True])


def case_143_a_glob_is_read_as_a_glob_without_a_tree(expect):
    global SWEEP
    # A SITE BLOCK THAT NAMES SOMETHING MUST DECLARE IT (kb/Work PB3721): a glob is a sweep over the tracked files it matches,
    # claimed whole; a block that names files the reader cannot claim declares nothing, and a note held to a block is refused
    SWEEP = "**Sites:** every `scripts/spec/work*.py` that reads the register, and `scripts/spec/work.py` (`load`)."
    expect.equal("a glob is read as a glob without a tree, and claims no file there",
          (work.note_sites(SWEEP).globs, work.note_sites(SWEEP).paths, work.note_sites(SWEEP).members),
          (("scripts/spec/work*.py",), ("scripts/spec/work.py",), {"scripts/spec/work.py": ("load",)}))


def case_144_with_a_tree_the_glob_claims_every_tracke(expect):
    SWEPT = work.note_sites(SWEEP, repo=coord.REPO)
    expect.equal("with a tree, the glob claims every tracked file it matches, and a sweep claims each file WHOLE (it outranks a member)",
          ("scripts/spec/work.py" in SWEPT.paths, "scripts/spec/work_inventory.py" in SWEPT.paths, SWEPT.members),
          (True, True, {}))
    expect.equal("a glob matching nothing tracked expands to nothing; placeholders and `...` are still no site",
          (work.expand_globs(("scripts/nothing-here/**/*.zz",), coord.REPO),
           work.note_sites("**Sites:** `src/<P>/X.cs`, `src/.../Y.cs`, `src/{A,B}/Z.cs`").globs), ((), ()))
    expect.equal("`none (why)` is a declaration, in the label form and the heading form",
          [work.note_sites(t).no_code for t in ("**Sites:** none (it changes no code file)", "- **Code site.** None - a finding",
                                                  "## Code site\n\nNone, that is the finding.\n", "**Sites:** `src/A.cs`")],
          [True, True, True, False])


def case_145_a_held_note_whose_block_names_files_it_c(expect):
    global gp
    GUARD = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": "defect", "status": "open", "cluster": ["C"], "_body": b}
             for n, b in [(NEW, "**Sites:** `kb/Work/PB1.md` and `conformance-suite/cases/x.cob`."),
                          (NEW + 1, "**Sites:** none (a golden only)"),
                          (NEW + 2, "**Sites:** every `scripts/spec/work*.py` that reads the register"),
                          (NEW + 3, "**Sites:** `scripts/spec/work.py` (`register_problems`) and `scripts/nothing-here/**/*.zz`"),
                          (NEW + 4, "**Sites:** `scripts/nothing-here/**/*.zz`"),
                          (NEW + 5, "**Sites:** `docs/A.md`"),
                          (NEW + 6, "**Sites:** `.claude/skills/workstream/check_practices.py` (`check`)")]]
    gp = work.sites_problems(GUARD)
    expect.equal("a held note whose block names files it cannot claim is refused, a `none (why)`, a live glob, a path under a root "
          "(the .claude process code included) are not, and a dead glob is named whether or not a path stands beside it",
          [s.split(":")[0] for s in gp], [f"PB{NEW}.md", f"PB{NEW + 3}.md", f"PB{NEW + 4}.md"])
    expect.equal("... and the refusal says what the planner does with such a note and how to declare it",
          (all(t in gp[0] for t in ("UNPARTITIONED", "PB3721", "none (why)")), "matches no tracked file" in gp[1],
           "matches no tracked file" in gp[2]), (True, True, True))


def case_146_work_sites_problems_an_open_note_of_a_cl(expect):
    # A NOTE OF A CAMPAIGN NEEDS ITS SITE BLOCK WHATEVER ITS ID (kb/Work PB3640). The campaign is the cluster a `tier:` entry
    # names: wave 20261030 planned PB3628 and PB3275 side by side because PB3275, a PB1957 note filed before the cutover, had
    # no block, and both rewrote one rule of prune_worktrees.lifecycle. The register holds no list of running campaigns.
    OLD = NEW - 100
    CAMP = [{"_file": f"PB{n}.md", "id": f"PB{n}", "kind": k, "status": s, "cluster": cl, "tier": t, "_body": b}
            for n, k, s, cl, t, b in [
                (OLD, "defect", "open", ["K"], ["K:2"], "Tiered, no block."),
                (OLD + 1, "defect", "open", ["K"], [], "A member the owner left untiered, no block."),
                (OLD + 2, "defect", "open", ["K", "Z"], [], "**Sites:** `src/A.cs`\n"),
                (OLD + 3, "defect", "open", ["Z"], [], "In a cluster no note tiers, filed before the cutover."),
                (OLD + 4, "decision", "open", ["K"], ["K:1"], "An owner question."),
                (OLD + 5, "defect", "landed", ["K"], ["K:3"], "Landed, no block."),
                (OLD + 6, "defect", "open", ["K"], ["K:3"], "**Sites:** none (it changes no code file)\n")]]
    expect.equal("work.sites_problems: an open note of a cluster some note tiers needs a block whatever its id; a decision, a "
          "terminal note, a declaration and a cluster no note tiers are as before",
          [s.split(":")[0] for s in work.sites_problems(CAMP)], [f"PB{OLD}.md", f"PB{OLD + 1}.md"])
    expect.equal("... and the refusal names the campaign",
          all("campaign cluster(s) K" in s and "PB3640" in s for s in work.sites_problems(CAMP)), True)
    expect.equal("work.campaign_leads: the clusters the register's tier entries name, malformed entries left out",
          work.campaign_leads(CAMP + TIERS), {"K", "C", "D"})


def case_147_a_planted_r3_note_on_binderdriver_cs_whi(expect):
    global REAL, r3_plant
    # 11. the register's R3 check no longer reads DESIGN-external-repository §12 (kb/Work PB4452): whether a wave's edits meet
    #     running work's is the planner's, by member (inflight.hold), recomputed at every plan.
    REAL = work.load()


    def r3_plant(pid, site):
        body = (NL + "**Wave kind:** extract. **Model:** Opus." + NL + NL + "**Sites:**" + NL + "- `" + site + "`" + NL + NL
                + "**Blocked by:** PB2118, PB2417." + NL)
        return {"_file": pid + ".md", "id": pid, "title": pid + " — Extract: planted", "status": "open",
                "blocked_by": ["PB2118", "PB2417"], "cluster": ["PB2119"], "_body": body}


    # kb/Work PB4452: a planted R3 note on a file the slices once reserved (BinderDriver.cs held the whole PB2293 campaign)
    # waits for no slice, and the register says nothing of one.
    expect.equal("a planted R3 note on BinderDriver.cs, which an open slice edits, is refused for nothing: the register reads no "
          "slice file set",
          [m for m in work.r3_program_problems(REAL + [r3_plant("PB9991", "src/WiseOwl.Cobol.Compiler/Binding/BinderDriver.cs")])
           if m.startswith("PB9991")], [])


def case_148_a_planted_r3_note_naming_only_a_generate(expect):
    global G4, GEN
    # PB2197 (the R1 landing's CI red): the existence rule reads the COMMITTED tree, so a build output that exists in a built
    # checkout and not in CI's never decides it; a note naming a generated parser file must name the grammar it comes from.
    GEN = "src/WiseOwl.Cobol.Frontend/Generated/CobolParserCore.cs"
    G4 = "src/WiseOwl.Cobol.Frontend/Grammar/CobolParserCore.g4"
    gen_only = [m for m in work.r3_program_problems(REAL + [r3_plant("PB9995", GEN)]) if m.startswith("PB9995")]
    expect.equal("a planted R3 note naming only a generated file is refused (build output, no grammar)",
          any("build output" in m for m in gen_only), True)
    expect.equal("a generated file is never reported missing, built checkout or not", any("does not exist" in m for m in gen_only), False)


def case_149_a_planted_r3_note_naming_the_generated_f(expect):
    with_g4 = r3_plant("PB9996", GEN)
    with_g4["_body"] += "- `" + G4 + "`" + NL
    expect.equal("a planted R3 note naming the generated file AND its grammar passes the build-output rule",
          any("build output" in m for m in work.r3_program_problems(REAL + [with_g4]) if m.startswith("PB9996")), False)
    expect.equal("an untracked path is missing even if a local file has that name",
          work.named_missing_paths("`src/WiseOwl.Cobol.Frontend/obj/project.assets.json`"), ["src/WiseOwl.Cobol.Frontend/obj/project.assets.json"])


def case_150_the_edges_file_s_removers_open_notes_who(expect):
    global EDGES, NOTES, edges_note
    # PB2417 review (R1-1, R1-2): the register's side of the architecture edges file's removers, on a planted file and notes.
    def edges_note(pid, status, removes):
        return {"_file": pid + ".md", "id": pid, "status": status, "_body": NL + "**Removes (planted):** " + removes + NL}


    EDGES = {"tolerated": [{"from": "P.Low", "to": "P.Mid", "removedBy": ["PB1"], "plan": "PB1",
                            "pairs": [{"from": "P.Low.A", "to": "P.Mid.B", "removedBy": ["PB1"]}]},
                           {"from": "P.Mid", "to": "P.High", "removedBy": ["PB2"], "plan": "PB2, then PB3", "pairs": []}]}
    NOTES = [edges_note("PB1", "open", "`P.Low → P.Mid` 1"), edges_note("PB2", "open", "`P.Mid → P.High` 1"),
             edges_note("PB3", "open", "nothing")]
    expect.equal("the edges file's removers: open notes whose Removes lines name their rows pass",
          work.edges_remover_problems(NOTES, EDGES), [])
    expect.equal("a remover the register lacks (a pair's), and a plan's missing note",
          [m.split(": ", 1)[1] for m in work.edges_remover_problems(NOTES[1:], EDGES)],
          ["tolerated row P.Low -> P.Mid names PB1 as a remover, which kb/Work does not hold",
           "tolerated row P.Low -> P.Mid: its plan names PB1, which kb/Work does not hold"])
    expect.equal("a landed remover removes nothing",
          any("PB1 is landed" in m for m in work.edges_remover_problems([edges_note("PB1", "landed", "`P.Low → P.Mid` 1")] + NOTES[1:], EDGES)),
          True)
    expect.equal("a census-only row's own remover is checked like a pair's (its Removes line names another row)",
          any("PB2's **Removes** line does not name the row (`P.Mid → P.High`)" in m
              for m in work.edges_remover_problems([NOTES[0], edges_note("PB2", "open", "`P.Low → P.Mid` 1"), NOTES[2]], EDGES)),
          True)
    expect.equal("a row whose name is a prefix of another's is not named by the longer one",
          any("PB1's **Removes** line does not name the row" in m
              for m in work.edges_remover_problems([edges_note("PB1", "open", "`P.Low → P.Mid.Sub` 1")] + NOTES[1:], EDGES)),
          True)
    expect.equal("the live edges file's removers are open notes that name their rows", work.edges_remover_problems(REAL), [])


def case_151_a_census_only_row_s_landed_last_remover(expect):
    global LANDED_PB2
    # PB2417 review R2-1, the PB4319 shape: a census-only row (one seen only by the census, or one whose remover's landing
    # took its last walked pair) whose every remover landed can leave the file only through a census recorded in the
    # landing, so the refusal names that path and never "delete it from removedBy", which would leave a row no note removes.
    LANDED_PB2 = [NOTES[0], edges_note("PB2", "landed", "`P.Mid → P.High` 1"), NOTES[2]]
    census_only = [m for m in work.edges_remover_problems(LANDED_PB2, EDGES) if "PB2 is landed" in m]
    expect.equal("a census-only row's landed last remover is refused, and the refusal names the census landing",
          len(census_only) == 1 and work.EDGES_CENSUS_LANDING in census_only[0] and "delete it from" not in census_only[0], True)


def case_152_a_census_only_row_s_landed_remover_besid(expect):
    global TWO
    TWO = {"tolerated": [dict(EDGES["tolerated"][1], removedBy=["PB2", "PB4"], plan="PB2")]}
    expect.equal("a census-only row's landed remover beside an open one is deleted from removedBy",
          any("PB2 is landed" in m and "delete it from" in m
              for m in work.edges_remover_problems(LANDED_PB2 + [edges_note("PB4", "open", "`P.Mid → P.High` 1")], TWO)), True)
    expect.equal("a pair's landed remover is deleted from removedBy (the ratchet takes a vanished pair)",
          any("PB1 is landed" in m and "delete it from" in m and work.EDGES_RATCHET in m
              for m in work.edges_remover_problems([edges_note("PB1", "landed", "`P.Low → P.Mid` 1")] + NOTES[1:], EDGES)), True)


def case_153_the_ratchet_work_py_names_is_the_drift_t(expect):
    global got, gp, head, lead, name, text, want
    _ratchet_cs = (fixtures.HERE.parents[1] / "tests" / "WiseOwl.Cobol.Tests.Unit" / "ArchitectureEdgesDriftTests.cs").read_text(encoding="utf-8")
    expect.equal("the ratchet work.py names is the drift test's: the switch its RatchetCommand sets, and that constant",
          bool(re.search(r'RatchetCommand =\s*"WISEOWL_COBOL_ARCH_EDGES_RATCHET=1 ', _ratchet_cs))
          and work.EDGES_RATCHET.startswith("WISEOWL_COBOL_ARCH_EDGES_RATCHET=1 ")
          and "ArchitectureEdgesDriftTests.RatchetCommand" in work.EDGES_RATCHET, True)

    # PB2562: a train label is a taken wave number, because a wave's first train is labelled with the wave number.
    with tempfile.TemporaryDirectory() as td:
        tdir = pathlib.Path(td)
        (tdir / "DEVLOG").mkdir()                                    # one file per entry (kb/Work PB3025)
        (tdir / "DEVLOG" / "0002-train-1036.md").write_text("## Entry 2 — Train 1036 (wave 1032: D, E)" + NL, encoding="utf-8", newline="\n")
        (tdir / "DEVLOG" / "0001-wave-1034.md").write_text("## Entry 1 — wave 1034 dispatched" + NL, encoding="utf-8", newline="\n")
        expect.equal("the wave number counts a train label above every wave number", pw.alloc.peek("wave", tdir, tdir), 1037)
        # PB2806: a wave recorded in the dispatch ledger with no report yet (another session's) is a taken number too
        record_dispatch(tdir, "1045", [{"letter": "a", "notes": ["PB5"], "files": ["src/X.cs"]}], lambda i: False)
        expect.equal("the wave number counts a dispatched wave with no report", pw.alloc.peek("wave", tdir, tdir), 1046)

    # PB3181: THE ONE "FINISHED REPORT" PREDICATE (Report.finished) and its `--finished` arm, which the rolling wave asks
    # before it reviews a return; the header's HEAD and BASE fields are read field-first.
    with tempfile.TemporaryDirectory() as td:
        fdir = pathlib.Path(td)
        full = "0123456789abcdef0123456789abcdef01234567"
        for name, text in (("w9f1a-PB1-report.md", "Status: DONE\nBranch: b\nHEAD: 1111111\nBase: 2222222\n"),
                           ("w9f1b-PB1-report.md", "**Status:** DISCHARGED · **branch:** `b` · **base:** `2222222` · "
                                                   "**HEAD:** `3333333`\n"),
                           ("w9f1c-PB1-report.md", f"DISCHARGED\nBase {full}  Head {full[:9]}\n"),
                           ("w9f1d-PB1-report.md", "Status: DISCHARGED\nBase: 2222222\n"),
                           ("w9f1e-PB1-report.md", "Status: SPLIT\nHEAD: 1111111\n"),
                           ("w9f1f-PB1-report.md", "\n**Review:** CLEAN r1 1111111\n# a report with no status line\n"
                                                   "Branch: b · head 1111111\n"),
                           ("w9f1g-PB1-report.md", "Status: DONE\nBranch: b · base 2222222 (reviewed head 4444444 merged in)"
                                                   " · head 5555555\n")):
            (fdir / name).write_text(text + "\n## Rule\nHEAD: 9999999 below the header binds nothing\n", encoding="utf-8",
                                     newline="\n")
        got = {n[4]: (r.status, r.head, r.base, r.finished) for n in sorted(p.name for p in fdir.iterdir())
               if (r := parse_report(fdir / n))}
        expect.equal("Report.finished: DONE, or DISCHARGED with a HEAD past its base; never SPLIT, no status, head == base or none",
              {k: v[3] for k, v in got.items()},
              {"a": True, "b": True, "c": False, "d": False, "e": False, "f": False, "g": True})
        expect.equal("the template's base and HEAD fields are read", got["b"][1:3], ("3333333", "2222222"))
        expect.equal("a field-position head beats a head named in prose before it (w1080f's header)", got["g"][1], "5555555")
        expect.equal("an abbreviated head names its full base: one commit (same_commit)", same_commit(full[:9], full), True)
        for letter, want in (("a", 0), ("b", 0), ("c", 1), ("d", 1), ("f", 1)):
            expect.equal(f"--finished w9f1{letter}: exit {want}", pw.main(["--finished", str(fdir / f"w9f1{letter}-PB1-report.md")]),
                  want)
        expect.equal("--finished on a missing report: exit 2", pw.main(["--finished", str(fdir / "w9f1z-PB1-report.md")]), 2)
        (fdir / "notes.md").write_text("Status: DONE\n", encoding="utf-8", newline="\n")
        expect.equal("--finished on a name that is not a fix-lane report: exit 2", pw.main(["--finished", str(fdir / "notes.md")]), 2)
        expect.equal("status_line finds the bare word and its index; None without one",
              (status_line(["# title", "DONE"]), status_line(["# title", "Branch: b"])), ((1, "DONE"), None))

    # PB3536: A GATE HANDED TO THE WAVE. A GATE-PENDING report names the run directory of the gate its agent left running at
    # its turn cap; the report is FINISHED only when that gate's verdict file is GREEN for the report's HEAD and measured
    # that commit's own tree (Report.gate_state, through gate' one reader); a gate still running holds the note
    # (no finisher re-runs it), and any other end plans a finisher.
    with tempfile.TemporaryDirectory() as td:
        gdir = pathlib.Path(td)
        checkout = ScratchRepo(coord.REPO)   # read only: the commit and tree this checkout is at
        head, tree = checkout.git("rev-parse", "HEAD"), checkout.git("rev-parse", "HEAD^{tree}")
        ended = subprocess.Popen([sys.executable, "-c", "pass"])
        ended.wait()

        import gate  # noqa: PLC0415  (scripts/: the gate's own reader and its lock)

        def gate_run(name, rec=None, pid=None):
            # a run that names a gate pid sits in a real worktree: the reader asks THAT worktree's gate lock (review R1-1 of
            # wave 20261104: a pid is reused, the lock is not), so the arm can hold it
            wt = ScratchRepo.init(gdir / "wts" / name).path if pid is not None else None
            run = (wt / gate.RUN_ROOT if wt else gdir / "runs") / name
            run.mkdir(parents=True)
            if rec is not None:
                (run / "verdict.json").write_text(json.dumps({"verdict": "x", "line": "=== BUILD-LOCAL GATE: x ===", **rec}),
                                                  encoding="utf-8", newline="\n")
            if pid is not None:
                (run / "gate.pid").write_text(json.dumps({"pid": pid}), encoding="utf-8", newline="\n")
            return run

        green = {"exit_code": 0, "head": head, "tree": tree, "tree_end": tree}
        runs = {"a": gate_run("green", green), "b": gate_run("red", {**green, "exit_code": 1}),
                "c": gate_run("running", pid=os.getpid()), "d": gate_run("died", pid=ended.pid),
                "e": gate_run("other-tree", {**green, "tree": "f" * 40, "tree_end": "f" * 40}),
                "f": gate_run("edited-while-gated", {**green, "tree_end": "e" * 40}),
                "g": gate_run("other-head", {**green, "head": "0" * 40}),
                "j": gate_run("pid-reused", pid=os.getpid())}   # no verdict, gate.pid names a LIVE process, nobody holds the lock
        # run "c" is the one gate that runs: its worktree's gate lock is held, its label naming the run (what `Gate.run` writes)
        c_lock = gate.ExclusiveLock.try_take(
            gate.Host(runs["c"].parents[2]).lock_path(),
            f"gate {runs['c'].parents[2]} (pid 7, since T{gate.RUN_LABEL}{runs['c']})")
        for letter, run in runs.items():
            (gdir / f"w9g1{letter}-PB1-report.md").write_text(
                f"Status: GATE-PENDING\nBranch: b\nHEAD: {head[:12]}\nGate run: {run}\n\n## Gate\n", encoding="utf-8",
                newline="\n")
        (gdir / "w9g1h-PB1-report.md").write_text(f"**Status:** GATE-PENDING · **HEAD:** `{head[:12]}`\n", encoding="utf-8",
                                                  newline="\n")
        # review R1-1 of wave 1152: an agent returns its run RELATIVE to its worktree (`TestResults/build-local/<run>`),
        # and the planner reads it from elsewhere: the report's Worktree field resolves it
        wt_i = gdir / "worktrees" / "wf_gp-i"
        (wt_i / "TestResults" / "build-local" / "r").mkdir(parents=True)
        (wt_i / "TestResults" / "build-local" / "r" / "verdict.json").write_text(
            json.dumps({"verdict": "x", "line": "=== BUILD-LOCAL GATE: x ===", **green}), encoding="utf-8", newline="\n")
        (gdir / "w9g1i-PB1-report.md").write_text(
            f"Status: GATE-PENDING\nWorktree: {wt_i}\nBranch: b\nHEAD: {head[:12]}\nGate run: TestResults/build-local/r\n\n"
            "## Gate\n", encoding="utf-8", newline="\n")
        got = {n[4]: (r.status, r.gate_state, r.finished) for n in sorted(p.name for p in gdir.glob("*.md"))
               if (r := parse_report(gdir / n))}
        expect.equal("PB3536: a GATE-PENDING report is finished only when its gate run is GREEN for its HEAD and that commit's "
              "tree; RUNNING while the gate holds its worktree's gate lock; ENDED on a red, a death (even when gate.pid names "
              "a live process: a reused pid), another tree or head, or no run named; a run named relative to the report's "
              "worktree is that worktree's",
              got, {"a": ("GATE-PENDING", "GREEN", True), "b": ("GATE-PENDING", "ENDED", False),
                    "c": ("GATE-PENDING", "RUNNING", False), "d": ("GATE-PENDING", "ENDED", False),
                    "e": ("GATE-PENDING", "ENDED", False), "f": ("GATE-PENDING", "ENDED", False),
                    "g": ("GATE-PENDING", "ENDED", False), "h": ("GATE-PENDING", "ENDED", False),
                    "j": ("GATE-PENDING", "ENDED", False), "i":("GATE-PENDING", "GREEN", True)})
        expect.equal("PB3536: --finished answers a GATE-PENDING report from its gate run (GREEN 0, running 1, red 1)",
              [pw.main(["--finished", str(gdir / f"w9g1{x}-PB1-report.md")]) for x in "acb"], [0, 1, 1])
        # review R1-2 of wave 1152, end to end: a HARNESS worktree (a clean commit plus the harness's edit of its TRACKED
        # .claude/settings.local.json) whose gate's record carries the tree the gate's own Host.measured_tree measures
        # there: the report at that HEAD is GREEN, so the wave reviews it and no finisher re-runs the gate.
        hw = gdir / "worktrees" / "wf_gp-harness"
        (hw / ".claude").mkdir(parents=True)
        (hw / ".claude" / "settings.local.json").write_text("{}\n", encoding="utf-8", newline="\n")
        (hw / "work.txt").write_text("work\n", encoding="utf-8", newline="\n")
        harness = ScratchRepo.init(hw)
        harness.git("add", "-A")
        harness.git("add", "-f", "--", ".claude/settings.local.json")   # a user's global excludes may name it
        harness.git("commit", "-q", "-m", "work")
        (hw / ".claude" / "settings.local.json").write_text('{"permissions": {}}\n', encoding="utf-8", newline="\n")
        hhead = harness.git("rev-parse", "HEAD")
        htree = gate.Host(hw).measured_tree()
        hrun = hw / "TestResults" / "build-local" / "20261023T000000Z-abcdef"
        hrun.mkdir(parents=True)
        (hrun / "verdict.json").write_text(json.dumps({"verdict": "GREEN", "line": "=== BUILD-LOCAL GATE: GREEN — x ===",
                                                       "exit_code": 0, "head": hhead, "tree": htree, "tree_end": htree}),
                                           encoding="utf-8", newline="\n")
        (gdir / "w9g2a-PB1-report.md").write_text(
            f"Status: GATE-PENDING\nWorktree: {hw}\nBranch: b\nHEAD: {hhead[:12]}\n"
            "Gate run: TestResults/build-local/20261023T000000Z-abcdef\n\n## Gate\n", encoding="utf-8", newline="\n")
        hr = parse_report(gdir / "w9g2a-PB1-report.md")
        expect.equal("PB3536 R1-2: a harness worktree's GREEN gate at its HEAD (the tracked settings file edited, nothing else) is "
              "GREEN and finished, its run named relative to that worktree",
              (bool(htree), hr and hr.gate_state, hr and hr.finished), (True, "GREEN", True))
        (gdir / "w9g2a-PB1-report.md").unlink()
        # the planner: a note whose agent handed over a gate that still runs waits for it; one whose gate ended gets a
        # finisher; one whose gate is GREEN is finished work, never re-planned
        grep = gdir / "plan-reports"
        grep.mkdir()
        for name, lead, run in (("w1150a-PB5-report.md", "PB5", runs["c"]), ("w1150b-PB10-report.md", "PB10", runs["b"]),
                                ("w1150c-PB11-report.md", "PB11", runs["a"])):
            (grep / name).write_text(f"Status: GATE-PENDING\nBranch: worktree-wf_gp-{lead}\nHEAD: {head[:12]}\n"
                                     f"Gate run: {run}\n\n## Gate\n", encoding="utf-8", newline="\n")
        pg = pw.plan(notes, CLUSTERS, HALF, load_reports(grep), lambda b: "UNLANDED", {}, RULES, budget_points=100)
        gp = {i: [g for g in pg["groups"] if i in g.notes] for i in ("PB5", "PB10", "PB11")}
        expect.equal("PB3536: the planner waits on a handed gate that still runs, gives a finisher to one that ended RED, and "
              "re-plans none whose gate is GREEN",
              ("still running" in pg["waiting"].get("PB5", ""), not gp["PB5"],
               bool(gp["PB10"]) and gp["PB10"][0].kind == "finisher", not gp["PB11"]),
              (True, True, True, True))
        c_lock.release()


def case_154_1_a_note_whose_only_mention_of_cite_py_i(expect):
    global DDECL, DR, GATE_ONLY, rel
    # 10. A NOTE'S DECLARED FILE SET IS ITS STRUCTURED SITES, READ BY ONE READER (kb/Work PB3242; DESIGN-architecture-review
    #     §8.7). The planner once declared every path a note's prose mentioned (fix_clusters' scored reading), so the R2
    #     and PB3160 campaigns planned zero groups: 57 of PB1754's 63 ready notes DISCUSS plan_wave.py and landing_check.py,
    #     and every PB3160 slice's Gate: line names cite.py.
    DR = TMP / "declared"
    for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs", "scripts/gamma.py",
                "scripts/spec/cite.py", "src/Model/DataItem.cs"):
        (DR / rel).parent.mkdir(parents=True, exist_ok=True)
        (DR / rel).write_text("// fixture" + NL, encoding="utf-8", newline="\n")
    DDECL = declared_sets(DR, IndexPolicy(TMP / "no-cache", regenerate=False))
    sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
    import member_index  # noqa: E402
    GATE_ONLY = (f"**Sites:**{NL}- `scripts/gamma.py`{NL}{NL}Probe it with `python scripts/spec/cite.py --check 14.9 \"x\"`."
                 f"{NL}{NL}**Gate:** `python scripts/spec/cite.py --check 14.9 \"x\"` and `pwsh scripts/build-local.ps1`{NL}")
    expect.equal("(1) a note whose only mention of cite.py is a Gate: line and prose declares no cite.py",
          ("scripts/spec/cite.py" in DDECL(GATE_ONLY).files, sorted(DDECL(GATE_ONLY).files)), (False, ["scripts/gamma.py"]))


def case_155_2_a_sites_path_a_file_member_a_file_line(expect):
    global EVERY_SHAPE, SITES, shape, text, want
    EVERY_SHAPE = (f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}- `src/Lib/Beta.cs#Run` (Run){NL}"
                   f"- `tests/Delta.Tests/DeltaTests.cs:12-40`{NL}- scripts/gamma.py (main){NL}"
                   f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}{NL}"
                   f"**Fix.** It also touches `src/Model/DataItem.cs` in prose.{NL}")
    SITES = ("scripts/gamma.py", "src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs")
    expect.equal("(2) a Sites path, a file#member, a file:line and a scripts/ path each declare their file; a census member id's "
          "parameter type (PB2423) and a path in prose declare nothing", work.note_sites(EVERY_SHAPE).paths, SITES)
    expect.equal("(2) the census member id is a name, its return type dropped",
          work.note_sites(EVERY_SHAPE).names, ("N.OoBinder.Err(N.Model.DataItem)",))
    for shape, text in (("a bold paragraph label", f"**Code site.** src/Lib/Alpha.cs#Run (the arm){NL}{NL}**Fix.** x"),
                        ("a heading section", f"## Code sites{NL}{NL}`src/Lib/Alpha.cs:7`, and more:{NL}{NL}"
                                              f"### Detail{NL}`src/Lib/Beta.cs`{NL}## Fix{NL}`scripts/gamma.py`"),
                        ("a list-item label", f"- **Code site:** src/Lib/Alpha.cs#Run{NL}- **Fix:** scripts/gamma.py"),
                        ("a label alone over a loose list", f"**Sites:**{NL}{NL}- `src/Lib/Alpha.cs`{NL}{NL}Prose: "
                                                            f"scripts/gamma.py")):
        want = ("src/Lib/Alpha.cs", "src/Lib/Beta.cs") if shape == "a heading section" else ("src/Lib/Alpha.cs",)
        expect.equal(f"(2) the site block as {shape} ends where the block ends", work.note_sites(text).paths, want)
    # Branch review R1-1 (PB3670): the binding rule's site form (R3: the file, a `:line` optional, then a parenthesis of code
    # spans) names MEMBERS, so a note filed by it partitions on them; a `:line` alone claims the file whole; and every text
    # that states the rule quotes the one example note_sites reads members from, so the rule and the parser cannot drift
    # apart again (R3 once prescribed `(file:line)` files only, which the member parser reads as whole files).
    expect.equal("R1-1: R3's site form names its members, with or without the :line; a :line alone claims the file whole",
          (work.note_sites(work.SITES_EXAMPLE).members, work.note_sites("**Sites:** `src/Lib/Beta.cs:12` (`Beta.Run`)").members,
           work.note_sites("**Sites:** `src/Lib/Beta.cs:12`").members),
          ({"src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs": ("DataBinder.Bind", "DataBinder.Sync")},
           {"src/Lib/Beta.cs": ("Beta.Run",)}, {}))
    for doc in (".claude/skills/workstream/templates/MANDATORY-PRACTICES.md",
                ".claude/skills/workstream/templates/registrar-brief.md"):
        expect.equal(f"R1-1: {doc} states the site form by quoting work.SITES_EXAMPLE",
              work.SITES_EXAMPLE in (coord.REPO / doc).read_text(encoding="utf-8"), True)
    expect.equal("R1-1: work.SITES_RULE (what `work.py check` prints) quotes it too", work.SITES_EXAMPLE in work.SITES_RULE, True)
    expect.equal("(2) a placeholder, a glob and a bare root are no sites",
          work.note_sites(f"**Sites:**{NL}- `src/<Project>/X.cs`{NL}- `tests/**/char_*.txt`{NL}- `src/`{NL}").paths, ())
    expect.equal("(2) the roots are model_rules.json's campaign roots, never a literal",
          work.campaign_domain().roots, tuple(RULES["campaign"]["src"]))


def case_156_3_the_member_index_s_declared_set_of_a_n(expect):
    EMPTY_INDEX = {"commit": "c", "uses": {}, "types": {}, "declared": {}, "emitted": {}, "_simple": {}}
    fs_mi = member_index.note_file_set(EVERY_SHAPE, DR, index=lambda: EMPTY_INDEX, drifted=lambda: {})
    expect.equal("(3) the member index's declared set of a note with a name: its sites (the name unknown to this index)",
          (tuple(fs_mi["sites"]), tuple(fs_mi["files"]), fs_mi["unknown"]), (SITES, SITES, ["N.OoBinder.Err(N.Model.DataItem)"]))


def case_157_3_the_planner_the_member_index_and_the_r(expect):
    global citems
    SITES_ONLY = EVERY_SHAPE.replace(f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}", "")
    expect.equal("(3) the planner, the member index and the register read the same sites for the same note",
          (tuple(sorted(DDECL(SITES_ONLY).files)), tuple(member_index.note_file_set(SITES_ONLY, DR)["sites"]),
           work.note_sites(SITES_ONLY).paths), (SITES, SITES, SITES))
    if fc:
        CD = TMP / "declared-campaign" / "kb" / "Work"
        CD.mkdir(parents=True)
        (CD / "PB990.md").write_text(f"---{NL}id: PB990{NL}status: open{NL}cluster: [PB990]{NL}---{NL}{NL}{SITES_ONLY}",
                                     encoding="utf-8", newline="\n")
        citems = [dict(work.parse_frontmatter((CD / "PB990.md").read_text(encoding="utf-8")), _file="PB990.md")]
        cn = load_notes(CD, {"wrong_answer": 8})
        o3, _, _, _ = campaign_clusters(work.cluster_order(citems, "PB990"), cn,
                                           {"PB990": (CD / "PB990.md").read_text(encoding="utf-8")}, RULES, fc, DR, DDECL)
        expect.equal("(3) the planner's campaign group: its primary file is a declared site, its files exactly the declared set",
              (o3[0]["file"] in SITES, tuple(o3[0]["files"])), (True, SITES))


def case_158_a_fix_lane_cluster_s_collision_set_is_it(expect):
    global FL
    FL = TMP / "declared-fix" / "kb" / "Work"
    FL.mkdir(parents=True)
    (FL / "PB991.md").write_text(f"---{NL}id: PB991{NL}---{NL}{NL}{GATE_ONLY}", encoding="utf-8", newline="\n")
    FLC = [{"file": "src/Lib/Alpha.cs", "files": ["src/Lib/Alpha.cs", "scripts/spec/cite.py"], "notes": [{"id": "PB991"}]}]
    declare_fix_lane(FLC, DDECL, FL)
    expect.equal("a fix-lane cluster's collision set is its notes' declared sets plus its primary file, never fix_clusters' "
          "prose sites", FLC[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])


def case_159_r1_2_an_r2_fix_lane_defect_with_r2_membe(expect):
    global e
    # Branch review R1-2: an R2 defect routed to the fix lane (its wave kind work.FIX_LANE_WAVE, with r2_members) is a
    # fix-lane note: the fix lane's plan declares its sites without reading the member index, so it never regenerates
    # one (DDECL's policy has no cache and refuses to regenerate: a read would raise StaleIndex).
    (FL / "PB992.md").write_text(f"---{NL}id: PB992{NL}---{NL}{NL}r2_members: N.Beta.Run, N.Alpha.Go{NL}{NL}"
                                 f"**Wave kind:** {work.FIX_LANE_WAVE}. **Model:** the fix lane's routing.{NL}{NL}"
                                 f"**Sites:**{NL}- `src/Lib/Beta.cs:10-20` (Beta.Run){NL}", encoding="utf-8", newline="\n")
    FLD = [{"file": "src/Lib/Beta.cs", "files": ["src/Lib/Beta.cs"], "notes": [{"id": "PB992"}]}]
    try:
        declare_fix_lane(FLD, DDECL, FL)
        expect.equal("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index", FLD[0]["files"],
              ["src/Lib/Beta.cs"])
    except Exception as e:   # noqa: BLE001 — any read of the index (StaleIndex, a git call on a fixture) is the red
        expect.equal("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index",
              f"read the index: {type(e).__name__}: {e}",
              ["src/Lib/Beta.cs"])


def case_160_r1_2_the_fix_lane_s_wave_kind_is_one_of(expect):
    sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
    import r2_collect  # noqa: E402
    import file_census_notes  # noqa: E402
    expect.equal("R1-2: the fix lane's wave kind is one of the R2 collector's wave kinds (one constant, work.FIX_LANE_WAVE)",
          work.FIX_LANE_WAVE in r2_collect.WAVES, True)


def case_161_r1_1_the_step_files_line_file_census_not(expect):
    # Branch review R1-1: the line file_census_notes writes naming the files ONE STEP edits is a site block, so a step file
    # outside the census finding's Sites (PB2300's IntrinsicCatalog.cs) is declared; the reader reads the writer's line.
    STEP = f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}{NL}{file_census_notes.step_files_line(['src/Lib/Alpha.cs', 'src/Lib/Beta.cs'])}{NL}"
    expect.equal("R1-1: the step-files line file_census_notes writes declares its files (reader = planner)",
          (work.note_sites(STEP).paths, tuple(sorted(DDECL(STEP).files))), (("src/Lib/Alpha.cs", "src/Lib/Beta.cs"),) * 2)


def case_162_4_no_module_outside_the_reader_scans_a_n(expect):
    global TREE, note_path_scanners
    def note_path_scanners(sources: dict[str, str]) -> list[str]:
        """(4) the modules that scan text for paths with a regex over the campaign roots (`(?:src|tests…)/`) or call
    fix_clusters' prose scorer (`.sites(`): only the register's reader (scripts/spec/work.py) may read a note's paths.
    The other regex shapes read documents that are not notes: drift_rules.py a C# drift test's string literals,
    audit_evidence_supersession.py the evidence records. A test file's fixtures are no reader."""
        alts = "|".join(sorted({re.escape(x) for r in RULES["campaign"]["src"] for x in (r, re.escape(r))}))
        shape = re.compile(r"\((?:\?:)?(?:" + alts + r")\||\.sites\(")
        exempt = {"scripts/spec/work.py", "scripts/spec/drift_rules.py", "scripts/spec/audit_evidence_supersession.py"}
        return sorted(p for p, t in sources.items()
                      if p not in exempt and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


    TREE = {p.relative_to(coord.REPO).as_posix(): p.read_text(encoding="utf-8", errors="replace")
            for root in ("scripts", ".claude/skills") for p in (coord.REPO / root).rglob("*.py")}
    expect.equal("(4) no module outside the reader scans a note's text for paths", note_path_scanners(TREE), [])


def case_163_4_the_scan_fails_on_a_planted_copy_of_th(expect):
    PLANT = "SITE_PATH = re.compile(r\"`((?" + ":src|tests)/[^`]+)\")"
    expect.equal("(4) the scan fails on a planted copy of the old whole-text SITE_PATH and on a prose-scorer call",
          note_path_scanners({"scripts/arch/planted.py": PLANT, "scripts/orchestrator/p2.py": "fc" + ".sites(text, idx, exts)",
                              "scripts/orchestrator/fine.py": "x = 1"}),
          ["scripts/arch/planted.py", "scripts/orchestrator/p2.py"])


def case_164_fold_unsited_the_clusters_stay_first_and(expect):
    global fu, unsited
    # 12. THE PARTITION MEETS ON MEMBERS, AND AN EMPTY SET HOLDS NOTHING (kb/Work PB3670, the owner's top priority,
    #     2026-10-24, superseding PB3263's subsystem rule). Two claims meet on a file only when either holds it whole or a
    #     member of one matches a member of the other (scripts/arch/source_units.py); no file is exempt (PB5110: one file per entry)
    #     so two entries never meet; an EMPTY set (no note declares a site block and no primary file was located, or a dispatch recorded
    #     with no files) meets nothing, in a wave and in flight alike, and the plan names it UNPARTITIONED. Wave w20261032's
    #     groups A (234 files) and B (222) held the owner-approved fuzz fixes PB3664 and PB3665 on files neither touched.
    def unsited(*ids):
        return {"file": "", "harm": 1, "files": [], "notes": [{"id": i, "area": "", "harm": 1, "title": ""} for i in ids]}


    # AN UNSITED READY DEFECT IS A GROUP OF ITS OWN (kb/Work PB4566): fix_clusters lists a note whose sites resolve to no
    # indexed source file under `unsited`, and the planner read `clusters` alone, so PB3600 (sited on the construct catalogue) and
    # PB3957 (reserved-words.json) were planned by no wave for two steered waves while the ledger counted them as groups.
    fu = fold_unsited({"clusters": [cl("src/X/Misc2.cs", "PB11")], "unsited": ["PB9"]}, notes)
    expect.equal("fold_unsited: the clusters stay first and each unsited note becomes a one-note group carrying its own harm, area "
          "and title; the result has no `unsited` list a reader could drop",
          ([c["file"] for c in fu["clusters"]], fu["clusters"][1]["notes"], fu["clusters"][1]["harm"], sorted(fu)),
          (["src/X/Misc2.cs", ""], [{"id": "PB9", "area": notes["PB9"].area, "harm": notes["PB9"].harm,
                                      "title": notes["PB9"].title[:140]}], notes["PB9"].harm, ["clusters"]))


def case_165_and_the_folded_note_is_planned_as_a_grou(expect):
    fu_plan = pw.plan(notes, fu["clusters"], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
    expect.equal("... and the folded note is PLANNED as a group of its own beside the cluster",
          sorted(g.notes for g in fu_plan["groups"]), [["PB11"], ["PB9"]])


def case_166_collision_a_whole_file_meets_anything_on(expect):
    global C
    C = Claim.of
    expect.equal("collision: a whole file meets anything on it; different members never meet; one member (qualified or bare) "
          "meets; an empty set meets nothing",
          (collision(C({"a.cs"}), C({"a.cs", "b.cs"}, {"a.cs": ["T.M"]})),
           collision(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"}, {"a.cs": ["T.N"]})),
           collision(C({"a.cs"}, {"a.cs": ["M"]}), C({"a.cs"}, {"a.cs": ["T.M", "T.N"]})),
           collision(C(set()), C({"a.cs"})), collision(C({"a.cs"}), C(set()))),
          ("a.cs", None, "a.cs#M", None, None))
    expect.equal("collision: no file is exempt (kb/Work PB5110): a note file both claim whole meets, where one file per entry "
          "(a construct, an addendum) never does because two entries are two files",
          (collision(C({"kb/Work/PB9.md"}), C({"kb/Work/PB9.md"})),
           collision(C({"kb/Work/PB9/2026-11-10-a.md"}), C({"kb/Work/PB9/2026-11-10-b.md"}))), ("kb/Work/PB9.md", None))
    expect.equal("Claim union: a file stays whole when either side claims it whole; members unite otherwise",
          ((C({"a.cs"}, {"a.cs": ["M"]}) | C({"a.cs"}, {"a.cs": ["N"]})).member_lists(),
           (C({"a.cs"}, {"a.cs": ["M"]}) | C({"a.cs"})).member_lists()),
          ({"a.cs": ["M", "N"]}, {}))
    expect.equal("collision_set: the primary file is whole unless every note declares a site block and one names it by member",
          (collision_set("a.cs", [C({"a.cs"}, {"a.cs": ["M"]})]).member_lists(),
           collision_set("a.cs", [C({"a.cs"}, {"a.cs": ["M"]}), Claim(sited=False)]).member_lists(),
           collision_set("a.cs", [C({"b.cs"}, {"b.cs": ["M"]})]).member_lists(),
           collision_set("", [Claim(sited=False)]).files),
          ({"a.cs": ["M"]}, {}, {"b.cs": ["M"]}, frozenset()))


def case_167_in_a_wave_two_groups_on_different_member(expect):
    mw = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5"), on_members(F_BIND, ["Binder.Sync"], "PB9"),
                         on_members(F_BIND, ["Sync"], "PB10"), cl(F_BIND, "PB11")],
                 [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
    expect.equal("in a wave two groups on DIFFERENT MEMBERS of one primary file run side by side; one on the same member is that "
          "group's work; one on the whole file joins the first with room and never unites them (multiple editors per "
          "file, kb/Work PB4452)",
          ([(g.letter, g.after, g.notes) for g in mw["groups"]], mw["waiting"]),
          ([("A", "", ["PB5", "PB11"]), ("B", "", ["PB9", "PB10"])], {}))


def case_168_and_without_the_whole_file_note_the_two(expect):
    global mw2
    mw2 = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5"), on_members(F_BIND, ["Binder.Sync"], "PB9"),
                          on_members(F_BIND, ["Sync"], "PB10")],
                  [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
    expect.equal("... and without the whole-file note the two member families stay side by side",
          [(g.letter, g.after, g.notes) for g in mw2["groups"]], [("A", "", ["PB5"]), ("B", "", ["PB9", "PB10"])])


def case_169_block(expect):
    global held, label, want
    # kb/Work PB3696: the in-wave rule asks the in-flight rule's question (collision) over EVERY file both groups claim, not
    # the primary file alone: two groups on different members of one primary file that both claim a third file whole, and two
    # on different primary files that share one, are one group (PB4396); the same pair with nothing shared still runs side by
    # side
    SHARED = "src/X/Shared.cs"


    def two_files(primary, members, secondary, nid):
        return {"file": primary, "harm": 1, "files": [primary, secondary], "members": {primary: members},
                "notes": [{"id": nid, "area": "", "harm": 1, "title": ""}]}


    for label, pair, want in (
            ("on different members of one primary file that both claim a third file whole",
             [two_files(F_BIND, ["Binder.Bind"], SHARED, "PB5"), two_files(F_BIND, ["Binder.Sync"], SHARED, "PB9")],
             [("A", "", ["PB5", "PB9"])]),
            ("on different primary files that share a secondary file whole",
             [two_files(F_BIND, ["Binder.Bind"], SHARED, "PB5"), two_files(F_EMIT, ["Emitter.Emit"], SHARED, "PB9")],
             [("A", "", ["PB5", "PB9"])]),
            ("on different members of one primary file that share nothing else (the control)",
             [two_files(F_BIND, ["Binder.Bind"], "src/X/A.cs", "PB5"), two_files(F_BIND, ["Binder.Sync"], "src/X/B.cs", "PB9")],
             [("A", "", ["PB5"]), ("B", "", ["PB9"])])):
        sw = pw.plan(notes, pair, [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
        meets = len(want) == 1
        expect.equal(f"in a wave two groups {label} are planned {'as one group' if meets else 'side by side'} "
              f"(PB3696, PB4396)", [(g.letter, g.after, g.notes) for g in sw["groups"]], want)
        held = pw.plan(notes, pair[1:], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                       busy={"worktree w (worktree-w)": InFlight(set(pair[0]["files"]), "worktree-w", {"PB70"}, state="IN FLIGHT",
                                                                    members={F_BIND: frozenset(pair[0]["members"][F_BIND])})})
        expect.equal(f"... and RUNNING work holds nothing there (PB4452): the pair groups by `collision` (a whole shared file) "
              f"and holds only on a shared MEMBER, which none of the three shares: {label}",
              (bool(collision(clusters_mod.cluster_claim(pair[0]), clusters_mod.cluster_claim(pair[1]))),
               bool(hold(clusters_mod.cluster_claim(pair[0]), clusters_mod.cluster_claim(pair[1]))),
               bool(held["waiting"])), (meets, False, False))


def case_170_a_hold_s_plan_records_what_it_shared_the(expect):
    global mh
    # kb/Work PB3695: real owner labels carry parentheses too (`worktree <name> (<branch>)`, `dispatched <slug> (PB547)`), so
    # what a hold SHARED is recorded as data (plan()'s `collided`), never parsed back out of its prose: the owners' branch
    # names and note ids are no "most named file"
    mh = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB9"), on_members(F_EMIT, ["Emitter.Emit"], "PB6")], [], [],
                 lambda b: "ABSENT", {}, RULES, budget_points=100,
                 busy={"worktree wf_x (worktree-wf_x)": InFlight({F_BIND}, "worktree-wf_x", {"PB70"}, state="RUNNING",
                                                                    members={F_BIND: frozenset({"Binder.Bind"})}),
                       "dispatched w9a (PB547)": InFlight({F_EMIT}, notes={"PB547"}, state="DISPATCHED, NO REPORT YET",
                                                             members={F_EMIT: frozenset({"Emitter.Emit"})})})
    expect.equal("a hold's plan records what it shared: the file and member per owner, as data (PB3695)",
          (mh["collided"], sorted(mh["waiting"])), ({"PB9": [F_BIND + "#Binder.Bind"], "PB6": [F_EMIT + "#Emitter.Emit"]}, ["PB6", "PB9"]))


def case_171_the_partition_s_measurement_records_the(expect):
    global MREC
    MREC = partition_record("9", mw2["groups"], {**mw2["waiting"], **mh["waiting"]}, mh["collided"], 1_800_000_000.0)
    expect.equal("the partition's measurement records the same-file pairs run side by side and every hold with its reason",
          (MREC["groups"], MREC["same_file_parallel"], MREC["held_in_wave"], sorted(MREC["held_by_collision"]),
           MREC["collided_on"]),
          (2, ["A+B on src/X/Binder.cs"], [], ["PB6", "PB9"],
           {"PB9": [F_BIND + "#Binder.Bind"], "PB6": [F_EMIT + "#Emitter.Emit"]}))


def case_172_and_partition_summary_reads_it_back_per(expect):
    MCOORD = TMP / "coord-measure"
    MCOORD.mkdir()
    record_partition(MCOORD, MREC)
    expect.equal("... and --partition-summary reads it back per day, naming the colliding members and files, never an owner's "
          "branch or note",
          partition_summary(MCOORD), ["2027-01-15: 1 waves, 2 groups, 1 same-file pairs side by side, 2 notes held by a "
                                         "collision (most named: src/X/Binder.cs#Binder.Bind x1, src/X/Emitter.cs#Emitter.Emit x1)"])


def case_173_in_a_wave_a_group_with_an_empty_set_runs(expect):
    global uw
    uw = pw.plan(notes, [cl(F_BIND, "PB5"), unsited("PB9"), unsited("PB10"), unsited("PB8"), cl("src/X/Misc2.cs", "PB11")],
                 [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
    expect.equal("in a wave a group with an EMPTY set runs beside every other group: it holds nothing and nothing holds it",
          [(g.letter, g.after, g.notes) for g in uw["groups"]],
          [("A", "", ["PB5"]), ("B", "", ["PB9"]), ("C", "", ["PB10"]), ("D", "", ["PB8"]), ("E", "", ["PB11"])])


def case_174_in_flight_a_dispatch_recorded_with_no_fi(expect):
    ub = pw.plan(notes, [unsited("PB9"), unsited("PB8"), cl("src/X/Misc2.cs", "PB11")], [], [], lambda b: "ABSENT", {},
                 RULES, budget_points=100,
                 busy={"dispatched w1117c (PB5)": InFlight(set(), notes={"PB5"}, state="DISPATCHED, NO REPORT YET"),
                       "worktree done (worktree-done)": InFlight({"src/Y/Engine.cs"}, "worktree-done", {"PB12"}, FIN,
                                                                    "WAITING TO LAND")})
    expect.equal("in flight a dispatch recorded with no files holds nothing, and an EMPTY group waits on nothing and continues "
          "nothing",
          ([(g.notes, g.continues) for g in ub["groups"]], ub["waiting"]),
          ([(["PB9"], []), (["PB8"], []), (["PB11"], [])], {}))
    expect.equal("the spec body names an UNPARTITIONED group and the Sites paragraph (files and members) to add",
          all(t in body_of(ub["groups"][0], notes) for t in ("UNPARTITIONED", "holds nothing", "**Sites:**", "members")),
          True)
    expect.equal("a group with a file set is never named UNPARTITIONED", "UNPARTITIONED" in body_of(uw["groups"][0], notes),
          False)
    expect.equal("every spec body, sited or not, asks its implementer to add to a note's Sites what its diff changes beyond them "
          "(kb/Work PB3640: a declared set is a prediction)",
          [("SITES FOLLOW THE DIFF" in body_of(g, notes)) for g in (uw["groups"][0], ub["groups"][0])], [True, True])


def case_175_in_flight_a_group_meeting_a_running_bran(expect):
    mb = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB9"), on_members(F_BIND, ["Binder.Sync"], "PB11")], [], [],
                 lambda b: "ABSENT", {}, RULES, budget_points=100,
                 busy={"worktree w (worktree-w)": InFlight({F_BIND}, "worktree-w", {"PB70"}, state="IN FLIGHT",
                                                              members={F_BIND: frozenset({"Binder.Bind"})})})
    expect.equal("in flight a group meeting a running branch on a MEMBER waits, the member named; one on another member of the "
          "same file runs",
          ([g.notes for g in mb["groups"]], f"({F_BIND}#Binder.Bind)" in mb["waiting"].get("PB9", "")), ([["PB11"]], True))
    expect.equal("the spec names the members a group claims, so its implementer keeps its edits inside them",
          f"{F_BIND} (Binder.Sync)" in body_of(mb["groups"][0], notes), True)
    expect.equal("the groups file carries the member claims make_dispatch_specs.py records in the ledger",
          as_group_json(mb["groups"][0], "9", notes, "X")["member_set"], {F_BIND: ["Binder.Sync"]})


def case_176_a_finisher_s_set_is_what_the_branch_it_c(expect):
    ufin = pw.plan(notes, [unsited("PB3")], [], [r3], lambda b: "UNLANDED", {}, RULES, budget_points=100,
                   busy={f"worktree pred ({r3.branch})": InFlight({F_EMIT, "src/X/New.cs"}, r3.branch,
                                                                     members={F_EMIT: frozenset({"Emitter.Emit"})})})
    expect.equal("a finisher's set is what the branch it continues changes, by the units its diff touches: planned and recorded "
          "with those files and members",
          [(g.kind, g.notes, g.files, g.members) for g in ufin["groups"]],
          [("finisher", ["PB3"], [F_EMIT, "src/X/New.cs"], {F_EMIT: ["Emitter.Emit"]})])


def case_177_a_dispatch_s_meta_notes_are_its_notes(expect):
    global SLI
    SLI = classify_inflight({"dispatched w1300a (PB8)": {"src/S.cs"}},
                               {"dispatched w1300a (PB8)": {"notes": ["PB8"], "slug": "w1300a"}}, [], {})
    expect.equal("a dispatch's meta notes are its notes", SLI["dispatched w1300a (PB8)"].notes, {"PB8"})


def case_178_and_an_empty_group_never_meets_it(expect):
    us = pw.plan(notes, [unsited("PB9"), unsited("PB11")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                 busy=SLI)
    expect.equal("... and an EMPTY group never meets it", (sorted(i for g in us["groups"] for i in g.notes), sorted(us["waiting"])),
          (["PB11", "PB9"], []))


def case_179_classify_inflight_a_dispatch_s_recorded(expect):
    DM = classify_inflight({"dispatched w1300a (PB70)": {F_BIND}},
                              {"dispatched w1300a (PB70)": {"notes": ["PB70"], "slug": "w1300a",
                                                            "members": {F_BIND: ["Binder.Bind"]}}}, [], {})
    expect.equal("classify_inflight: a dispatch's recorded members are its claim", DM["dispatched w1300a (PB70)"].claim.member_lists(),
          {F_BIND: ["Binder.Bind"]})


def case_180_inflight_units_a_committed_body_edit_and(expect):
    global BINDER, UMETA, UOWN, URAW, urepo
    # THE UNITS A RUNNING BRANCH TOUCHES are read from its actual diff (inflight_units, kb/Work PB3670), end to end on a
    # scratch repository: a branch that edits Binder.Bind's body holds Bind and nothing else of the file; a file only one
    # side holds is never read; an uncommitted edit in a worktree adds its units; an untracked file is whole.
    UR = scratch_dir("plan-wave-units-")
    urepo = ScratchRepo.init(UR / "repo")
    BINDER = "class Binder\n{\n    void Bind() { int a = 1; }\n\n    void Sync() { int b = 2; }\n}\n"
    urepo.commit("base", {"src/Binder.cs": BINDER, "src/Solo.cs": "class Solo { }\n"})
    urepo.git("update-ref", "refs/remotes/origin/main", "HEAD")
    uwt = urepo.worktree(UR / "wt", branch="worktree-units")
    urepo.commit("edit Bind", {"src/Binder.cs": BINDER.replace("int a = 1;", "int a = 3;"),
                               "src/Solo.cs": "class Solo { int x; }\n"}, cwd=uwt.path)
    urepo.write({"src/Binder.cs": BINDER.replace("int a = 1;", "int a = 3;").replace("}\n}", "}\n\n    void Tail() { }\n}"),
                 "src/New.cs": "class New { }\n"}, cwd=uwt.path)


    def ugit(*args, cwd=None, stdin=None):
        return urepo.run(*args, cwd=cwd, input=stdin)


    UMETA = {}
    URAW = inflight_file_sets(git=ugit, repo=urepo.path, coord_dir=UR, with_git=True, meta=UMETA)
    UBUSY = classify_inflight(URAW, UMETA, [], {})
    UOWN = next(o for o in UBUSY if "worktree-units" in o)
    inflight_units(UBUSY, UMETA, [C({"src/Binder.cs", "src/New.cs"})], ugit, urepo.path)
    expect.equal("inflight_units: a committed body edit and an uncommitted appended member are their units; an untracked file "
          "and a file no other claim holds are whole (unread)",
          (UBUSY[UOWN].files, UBUSY[UOWN].claim.member_lists()),
          ({"src/Binder.cs", "src/Solo.cs", "src/New.cs"}, {"src/Binder.cs": ["Binder.<own>", "Binder.Bind", "Binder.Sync",
                                                                              "Binder.Tail"]}))
    expect.equal("... so a group claiming another member of the file meets it, and one claiming no touched member does not",
          (collision(C({"src/Binder.cs"}, {"src/Binder.cs": ["Bind"]}), UBUSY[UOWN].claim),
           collision(C({"src/Binder.cs"}, {"src/Binder.cs": ["Other"]}), UBUSY[UOWN].claim)),
          ("src/Binder.cs#Bind", None))


def case_181_inflight_units_a_git_failure_leaves_ever(expect):
    def failing_git(*args, cwd=None, stdin=None):
        """git that fails every batched read (diff-tree): an unmeasured unit must never read as "touches nothing"."""
        r = urepo.run(*args, cwd=cwd, input=stdin)
        return subprocess.CompletedProcess(r.args, 1, "", "planted failure") if args[:1] == ("diff-tree",) else r


    UFAIL = classify_inflight(URAW, UMETA, [], {})
    UERR = io.StringIO()
    with contextlib.redirect_stderr(UERR):
        inflight_units(UFAIL, UMETA, [C({"src/Binder.cs", "src/New.cs"})], failing_git, urepo.path)
    expect.equal("inflight_units: a git failure leaves every file whole, so the owner meets every claim on its files",
          (UFAIL[UOWN].members, collision(C({"src/Binder.cs"}, {"src/Binder.cs": ["Other"]}), UFAIL[UOWN].claim)),
          ({}, "src/Binder.cs"))
    expect.equal("... and the unmeasured read is WARNED (a whole file holds nothing, PB4452), never silent",
          ("units not measured" in UERR.getvalue(), hold(C({"src/Binder.cs"}, {"src/Binder.cs": ["Bind"]}), UFAIL[UOWN].claim)),
          (True, None))


def case_182_pb3786_a_shared_set_within_the_bound_goe(expect):
    global MERGED, M_REF, M_SOME, midmerge_members
    # kb/Work PB3786: A WORKTREE IN THE MIDDLE OF A MERGE (every file main changed shows as uncommitted and shared) used to put
    # its whole uncommitted shared set on `git diff HEAD`'s command line; one such worktree (3,331 paths) crashed every plan
    # with WinError 206. The uncommitted arm now follows the committed arm's rule (inflight.bounded_pathspec: over
    # PATHSPEC_CHARS, no pathspec at all) and keeps only the shared files' hunks. Planted: 300 uncommitted files with 120-character
    # names (over the 32,767-character Windows limit, which crashed the unguarded call there) beside one edit of Binder.cs.
    MR = scratch_dir("plan-wave-midmerge-")
    mrepo = ScratchRepo.init(MR / "repo")
    MERGED = {"src/generated/" + "d" * 90 + f"_{k:04d}.cs": "class M" + str(k) + " { }" + chr(10)
              for k in range(300)}
    mrepo.commit("base", {"src/Binder.cs": BINDER, **MERGED})
    mrepo.git("update-ref", "refs/remotes/origin/main", "HEAD")
    mwt = mrepo.worktree(MR / "wt", branch="worktree-midmerge")
    mrepo.write({**{f: t.replace("{ }", "{ int x; }") for f, t in MERGED.items()},
                 "src/Binder.cs": BINDER.replace("int a = 1;", "int a = 3;")}, cwd=mwt.path)
    MDIFFS = []


    def mgit(*args, cwd=None, stdin=None):
        if args[:1] == ("diff",) and "HEAD" in args:
            MDIFFS.append(list(args))
        return mrepo.run(*args, cwd=cwd, input=stdin)


    def midmerge_members(files):
        """The worktree's members of the claimed files and the `git diff HEAD` command lines the plan ran."""
        meta: dict = {}
        busy = classify_inflight(inflight_file_sets(git=mgit, repo=mrepo.path, coord_dir=MR, with_git=True, meta=meta),
                                 meta, [], {})
        MDIFFS.clear()
        inflight_units(busy, meta, [C({*files, "src/Binder.cs"})], mgit, mrepo.path)
        return busy[next(o for o in busy if "worktree-midmerge" in o)].claim.member_lists(), [a[a.index("--"):] for a in MDIFFS]


    M_SOME = sorted(MERGED)[:60]
    M_REF, M_REF_CALLS = midmerge_members(M_SOME)
    expect.equal("PB3786: a shared set within the bound goes to `git diff HEAD` as a pathspec, and the worktree's units are read",
          (M_REF_CALLS == [["--", "src/Binder.cs", *M_SOME]], M_REF["src/Binder.cs"], len(M_REF)),
          (True, ["Binder.Bind"], len(M_SOME) + 1))


def case_183_pb3786_over_the_bound_the_same_read_runs(expect):
    saved_bound = inb.PATHSPEC_CHARS
    inb.PATHSPEC_CHARS = 1000
    try:
        M_SMALL, M_SMALL_CALLS = midmerge_members(M_SOME)
    finally:
        inb.PATHSPEC_CHARS = saved_bound
    expect.equal("PB3786: over the bound the same read runs with NO pathspec and gives the same members as the pathspec form",
          (M_SMALL_CALLS, M_SMALL == M_REF), ([["--"]], True))


def case_184_pb3786_the_real_mid_merge_size_300_files(expect):
    M_ALL, M_ALL_CALLS = midmerge_members(MERGED)
    expect.equal("PB3786: the real mid-merge size (300 files, over the 32,767-character command-line limit) plans: no pathspec, "
          "every shared file read, the units of the 60 files of the reference the same",
          (len(" ".join(MERGED)) > 32767, M_ALL_CALLS, len(M_ALL), {f: M_ALL[f] for f in M_REF} == M_REF),
          (True, [["--"]], len(MERGED) + 1, True))


def case_185_block(expect):
    global DRY, before, flags, kind
    # kb/Work PB3803: A DRY RUN WRITES NOTHING to the coordination directory, whichever path it plans by: the fix lane
    # (run_fix_clusters wrote clusters-<status>.json and its notes view), a campaign (clusters-campaign-<lead>.json) and a
    # regenerated member index (IndexPolicy.record_dir). The same plan without --dry-run records them, so the test is not
    # vacuous, and its table is the dry run's (apart from the header's DRY RUN mark). fix_clusters.py is a stand-in (the
    # submodule is not checked out in CI) and the campaign's own inputs are planted; the plan itself is the real one.
    DRY = TMP / "dry-run"
    DRY.mkdir()
    FAKE_FIX = DRY / "fake_fix_clusters.py"
    FAKE_FIX.write_text("import json, pathlib, sys\n"
                        "pathlib.Path(sys.argv[sys.argv.index('--json') + 1]).write_text(json.dumps({'clusters': []}))\n",
                        encoding="utf-8", newline="\n")
    (DRY / "reports").mkdir()
    DRY_MARK = "(DRY RUN: nothing reserved or written) "


    def listing(root):
        """{relative path: (mtime_ns, bytes)} of every file under `root`."""
        return {str(f.relative_to(root)): (f.stat().st_mtime_ns, f.read_bytes()) for f in sorted(root.rglob("*")) if f.is_file()}


    def dry_coord(name):
        """A coordination directory holding a previous plan's views, so a rewrite shows as a changed byte or mtime."""
        d = DRY / name
        (d / "member-index").mkdir(parents=True)
        for view in ("clusters-open.json", "clusters-half.json", "clusters-campaign-PB9.json", "member-index/old.members.json.gz"):
            (d / view).write_text("previous plan", encoding="utf-8", newline="\n")
        return d


    def plan_into(d, *flags):
        """main() of a plan with the planted inputs against coordination directory `d`: (exit code, its printed table)."""
        saved = (pw.work.cluster_order, pw.campaign_clusters, pw.load_fix_clusters, clusters_mod.FIX_CLUSTERS)
        pw.work.cluster_order = lambda items, lead: {"named": 1, "notes": [{"id": "PB1", "tier": 1}]}
        pw.campaign_clusters = lambda *_a, **_k: ([], [], {}, {})
        pw.load_fix_clusters = lambda: None
        clusters_mod.FIX_CLUSTERS = FAKE_FIX
        out = io.StringIO()
        try:
            with contextlib.redirect_stdout(out):
                rc = pw.main(["--wave", "9803", "--budget-points", "3", "--no-branches", "--allow-stale", "--coord", str(d),
                              "--reports", str(DRY / "reports"), *flags])
        finally:
            pw.work.cluster_order, pw.campaign_clusters, pw.load_fix_clusters, clusters_mod.FIX_CLUSTERS = saved
        return rc, out.getvalue()


    for kind, flags in (("fix lane", []), ("campaign (--cluster)", ["--cluster", "PB9"])):
        dry, real = dry_coord("dry-" + kind[:3]), dry_coord("real-" + kind[:3])
        before = listing(dry)
        dry_rc, dry_table = plan_into(dry, "--dry-run", *flags)
        real_rc, real_table = plan_into(real, "--scratch", str(DRY / "scratch"), *flags)
        expect.equal(f"PB3803: a --dry-run of the {kind} plan leaves every file of the coordination directory unchanged",
              (dry_rc, listing(dry) == before), (0, True))
        expect.equal(f"PB3803: ... the same plan without --dry-run records its views there and prints the dry run's table",
              (real_rc, listing(real) != before, real_table.replace(DRY_MARK, "") == dry_table.replace(DRY_MARK, "")),
              (0, True, True))


def case_186_pb3803_a_policy_with_a_record_dir_regene(expect):
    # A member index the plan must regenerate: with a record_dir the cache is only READ and the index recorded there; without
    # one it is recorded into the cache (member_index.current's `recorder`), exactly as before.
    DECL_CALLS = []
    saved_mi = (member_index.current, member_index.record, member_index.note_file_set)
    member_index.current = lambda repo, cache, regen, recorder=None: (cache, regen, recorder(repo, cache) if recorder else None)
    member_index.record = lambda repo, out_dir=None: out_dir
    member_index.note_file_set = lambda text, repo, index, drifted: (DECL_CALLS.append(index()),
                                                                      {"files": [], "members": {}, "sited": True})[1]
    try:
        declared_sets(DRY, IndexPolicy.of(DRY / "coord", record_dir=DRY / "made" / "member-index"))("a note")
        declared_sets(DRY, IndexPolicy.of(DRY / "coord"))("a note")
    finally:
        member_index.current, member_index.record, member_index.note_file_set = saved_mi
    expect.equal("PB3803: a policy with a record_dir regenerates the member index there and reads the coordination cache; without "
          "one the index is recorded by member_index's own default (into the cache)",
          [(c[0].relative_to(DRY).as_posix(), c[2] and c[2].relative_to(DRY).as_posix()) for c in DECL_CALLS],
          [("coord/member-index", "made/member-index"), ("coord/member-index", None)])


def case_187_pb3746_a_fix_lane_plan_beside_a_campaign(expect):
    global branch, lead, slug
    # kb/Work PB3746: A FIX-LANE WAVE AND A CAMPAIGN SHARE ONE COORDINATION DIRECTORY. After PB3699 a normal plan also
    # finishes a campaign's SPLIT branches (their notes are `process_only`: no fix-lane cluster holds them), so one directory
    # holds the campaign's reports, its dispatch-ledger entry and its running branches beside the fix lane's clusters. Planted
    # in one directory: a campaign branch (PB90, SPLIT, DECISION) holding F_CAMP; a campaign branch whose worktree a live
    # agent holds (PB94); a campaign group dispatched and not yet answered (PB93, in the ledger) holding F_HELD; and the fix
    # lane's clusters on F_CAMP (PB91), F_HELD (PB93, PB95) and a free file (PB92). Through the real ledger,
    # inflight_file_sets and held_by_dispatch the one plan finishes each campaign branch ONCE, plans no fix-lane group beside
    # the branch on its file (the group FOLLOWS the finisher: a successor), holds the dispatched campaign's note and the file's
    # other notes, and plans the free file's group beside everything.
    CB = TMP / "coord-both"
    CB_REPORTS = CB / "reports"
    CB_REPORTS.mkdir(parents=True)
    F_CAMP, F_HELD, F_FREE = "src/X/CampaignOwned.cs", "src/X/CampaignDispatched.cs", "src/X/Free.cs"
    for slug, lead, branch in (("w2050a", "PB90", "worktree-cb-1"), ("w2050b", "PB94", "worktree-cb-2")):
        (CB_REPORTS / f"{slug}-{lead}-report.md").write_text(
            f"SPLIT\n# {lead} group\n**Status:** SPLIT at the turn cap · **branch:** `{branch}` · **HEAD:** `6060606aa`\n\n"
            "## Reproduced?\n", encoding="utf-8", newline="\n")
    record_dispatch(CB, "2049", [{"letter": "a", "notes": ["PB93"], "files": [F_HELD], "members": {F_HELD: ["Held.Run"]}}],
                    lambda i: False)
    returned(CB_REPORTS)
    cb_notes = {n: Note(n, "open", area, n, [], "", 8) for n, area in
                (("PB90", "process"), ("PB94", "process"), ("PB91", "tooling"), ("PB92", "tooling"), ("PB93", "process"),
                 ("PB95", "tooling"))}
    cb_reports = load_reports(CB_REPORTS, CB)
    cb_meta = {}
    cb_raw = inflight_file_sets(coord_dir=CB, terminal=lambda i: False, with_git=False, meta=cb_meta)
    CB_BRANCH = "worktree cb-1 (worktree-cb-1)"
    cb_raw[CB_BRANCH] = {F_CAMP}
    cb_meta[CB_BRANCH] = {"notes": ["PB90"], "branch": "worktree-cb-1"}
    cb_busy = classify_inflight(cb_raw, cb_meta, cb_reports, {CB_BRANCH: "DECISION"}, {})
    cb_plan = pw.plan(cb_notes, [cl(F_CAMP, "PB91"), on_members(F_HELD, ["Held.Run"], "PB93", "PB95"), cl(F_FREE, "PB92")], [], cb_reports,
                      lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=cb_busy,
                      held=held_by_dispatch(CB, cb_reports, lambda i: False), locked={"worktree-cb-2"})
    cb_groups = {n: g for g in cb_plan["groups"] for n in g.notes}
    expect.equal("PB3746: a fix-lane plan beside a campaign's directory finishes the campaign's SPLIT branch once (never in a cluster "
          "group), and its notes held by a live worktree or an unanswered campaign dispatch wait",
          ([n for g in cb_plan["groups"] for n in g.notes if n in ("PB90", "PB94", "PB93")], cb_groups["PB90"].kind,
           sorted(n for n in cb_plan["waiting"] if n in ("PB94", "PB93", "PB95"))),
          (["PB90"], "finisher", ["PB93", "PB94", "PB95"]))
    expect.equal("PB3746: the fix lane's group on the file the campaign branch holds joins that finisher (kb/Work PB4396); the "
          "free file's group is planned beside everything",
          (cb_groups["PB91"] is cb_groups["PB90"], cb_groups["PB90"].after, cb_groups["PB92"].after, "PB92" in cb_plan["waiting"]),
          (True, "", "", False))

    # kb/Work PB4452: A SHARED FILE HOLDS NOTHING; ONLY AN EDIT TO THE SAME MEMBER DOES (owner 2026-11-02: "Non overlapping
    # edits to the same file typically will merge without conflict"). The open external-repository slices were once reserved
    # WHOLE against every restructuring wave, and held all 22 open steps of the PB2293 campaign behind a file (BinderDriver.cs)
    # the running slices never edited. A group is held by RUNNING work only where a member of one matches a member of the
    # other (inflight.hold); `collision` stays the rule of GROUPING and of joining a finished branch.
    expect.equal("hold: only a member of one that matches a member of the other holds; a shared file, a file either side claims "
          "whole, two whole claims, an empty set and a whole note file hold nothing",
          (hold(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"}, {"a.cs": ["T.M"]})),
           hold(C({"a.cs"}, {"a.cs": ["M"]}), C({"a.cs"}, {"a.cs": ["T.M", "T.N"]})),
           hold(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"}, {"a.cs": ["T.N"]})),
           hold(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"})),
           hold(C({"a.cs"}), C({"a.cs"}, {"a.cs": ["T.M"]})),
           hold(C({"a.cs"}), C({"a.cs"})),
           hold(C(set()), C({"a.cs"}, {"a.cs": ["T.M"]})),
           hold(C({"kb/Work/PB9.md"}), C({"kb/Work/PB9.md"}))),
          ("a.cs#T.M", "a.cs#M", None, None, None, None, None, None))
    expect.equal("hold: a whole file on one side does not hide a member that meets on another shared file; the first meeting by "
          "name is the label",
          hold(C({"a.cs", "b.cs"}, {"b.cs": ["U.X"]}), C({"a.cs", "b.cs"}, {"a.cs": ["T.M"], "b.cs": ["U.X"]})), "b.cs#U.X")
    expect.equal("hold vs collision: the same pair meets by collision (one step in a wave) on a whole claim and holds nothing",
          (collision(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"})), hold(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"}))),
          ("a.cs", None))
    expect.equal("collision_hits names each owner's meeting by the rule it is given (hold, for the owners that hold a group)",
          (inb.collision_hits(C({"a.cs"}, {"a.cs": ["T.M"]}), {"o1": C({"a.cs"}), "o2": C({"a.cs"}, {"a.cs": ["T.M"]})}),
           inb.collision_hits(C({"a.cs"}, {"a.cs": ["T.M"]}), {"o1": C({"a.cs"}), "o2": C({"a.cs"}, {"a.cs": ["T.M"]})}, hold)),
          ([("o1", "a.cs"), ("o2", "a.cs#T.M")], [("o2", "a.cs#T.M")]))


def case_188_block(expect):
    global label
    RUN_SYNC = {"worktree w (worktree-w)": InFlight({F_BIND}, "worktree-w", {"PB70"}, state="RUNNING",
                                                       members={F_BIND: frozenset({"Binder.Sync"})})}
    RUN_WHOLE = {"worktree w (worktree-w)": InFlight({F_BIND}, "worktree-w", {"PB70"}, state="RUNNING")}
    for label, group, busy_, held_want in (
            ("on ANOTHER MEMBER of the file running work edits", on_members(F_BIND, ["Binder.Bind"], "PB5"), RUN_SYNC, False),
            ("on the SAME MEMBER running work edits", on_members(F_BIND, ["Binder.Sync"], "PB5"), RUN_SYNC, True),
            ("on the WHOLE file (a site named whole, an index-found caller) beside running work that edits one member",
             cl(F_BIND, "PB5"), RUN_SYNC, False),
            ("on one member beside running work whose diff could not be read by member (changed whole)",
             on_members(F_BIND, ["Binder.Sync"], "PB5"), RUN_WHOLE, False)):
        hp = pw.plan(notes, [group], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100, busy=busy_)
        expect.equal(f"PB4452: a group {label} is {'HELD, the member named' if held_want else 'planned beside it'}",
              (sorted(hp["waiting"]), [g.notes for g in hp["groups"]], hp["collided"]),
              ((["PB5"], [], {"PB5": [F_BIND + "#Binder.Sync"]}) if held_want else ([], [["PB5"]], {})))


def case_189_pb4452_a_finished_branch_on_the_file_is(expect):
    hf = pw.plan(notes, [on_members(F_BIND, ["Binder.Sync"], "PB5")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                 busy={"worktree done (worktree-done)": InFlight({F_BIND}, "worktree-done", finished=FIN)})
    expect.equal("PB4452: a FINISHED branch on the file is still the group's to continue, whole file or member (PB4396): "
          "grouping is `collision`'s, the hold is only for running work",
          ([(g.notes, g.continues) for g in hf["groups"]], hf["waiting"]), ([(["PB5"], ["worktree-done"])], {}))
    expect.equal("PB4452: no reservation exists: no slice_reservation, SLICE_OWNER, InFlight.reserves, work.slice_file_set, "
          "work.open_slices or work.restructuring to read",
          ([hasattr(inb, n) for n in ("slice_reservation", "SLICE_OWNER")],
           [f.name for f in dataclasses.fields(InFlight) if f.name == "reserves"],
           [hasattr(work, n) for n in ("slice_file_set", "open_slices", "restructuring")]),
          ([False, False], [], [False, False, False]))


def case_190_a_dispatch_recorded_with_no_files_stays(expect):
    UL = TMP / "coord-unknown"
    UL.mkdir()
    coord.write_json(UL / coord.DISPATCH_LEDGER, {"groups": [{"wave": "1117", "letter": "c", "notes": ["PB5"], "files": []}]})
    UM = {}
    expect.equal("a dispatch recorded with no files stays in flight: its set empty (it meets nothing), its notes known",
          (inflight_file_sets(coord_dir=UL, terminal=lambda i: False, with_git=False, meta=UM),
           UM.get("dispatched w1117c (PB5)", {}).get("notes")),
          ({"dispatched w1117c (PB5)": set()}, ["PB5"]))


def case_191_block(expect):
    global C3114, FAKE_SPECS, S3114, before, kind, main3114, state3114, stop
    # kb/Work PB3114: A PLAN THAT DOES NOT END GREEN CHANGES NO SHARED STATE, end to end through main() on a scratch
    # coordination directory (its own allocator and dispatch ledger), with the plan itself fixed (one group) so only main's
    # ordering is under test. (a) A drive-relative --scratch or --stop-file (what Git Bash makes of an unquoted Windows
    # path) is refused before anything: exit 2, nothing reserved, recorded or written. (b) A RED practices check (a stand-in
    # make_dispatch_specs.py that exits 1) reserves no code and records nothing, and its one call was the --no-record pass.
    # (c) GREEN through the real make_dispatch_specs.py: the codes are reserved after the check, the spec carries them, the
    # ledger holds the group and the args file is written. (d) The renderer alone records nothing on RED.
    C3114 = TMP / "coord-3114"
    C3114.mkdir()
    (C3114 / "inflight-groups.json").write_text('{"groups": []}\n', encoding="utf-8", newline="\n")
    for kind in ("open", "half"):
        (C3114 / f"clusters-{kind}.json").write_text('{"clusters": []}\n', encoding="utf-8", newline="\n")
    (TMP / "reports-3114").mkdir()
    FAKE_SPECS = TMP / "fake_make_dispatch_specs.py"
    FAKE_SPECS.write_text("import pathlib, sys\npathlib.Path(sys.argv[1]).with_name('fake-calls.txt').open('a').write("
                          "' '.join(sys.argv[2:]) + chr(10))\nsys.exit(1)\n", encoding="utf-8", newline="\n")


    def main3114(scratch, stop, specs, wave="9114"):
        saved_plan, saved_specs = pw.plan, pw.MAKE_SPECS
        pw.plan = lambda *_a, **_k: {"groups": [Group("cluster", ["PB3114"], "scripts/orchestrator/plan_wave.py",
                                                      ["scripts/orchestrator/plan_wave.py"], letter="A", model="opus")],
                                     "points": 0.1, "awaiting_landing": {}, "waiting": {}, "collided": {}, "unlanded_without_note": [],
                                     "drain_not_started": [], "skipped": []}
        pw.MAKE_SPECS = specs
        try:
            return pw.main(["--wave", wave, "--budget-points", "3", "--no-branches", "--coord", str(C3114),
                            "--reports", str(TMP / "reports-3114"), "--clusters-json", str(C3114 / "clusters-open.json"),
                            "--half-clusters-json", str(C3114 / "clusters-half.json"), "--scratch", str(scratch),
                            "--stop-file", str(stop)])
        finally:
            pw.plan, pw.MAKE_SPECS = saved_plan, saved_specs


    def state3114():
        return (pw.alloc.peek("code", coord.REPO, C3114), coord.ledger_groups(C3114))


    before = state3114()
    S3114 = TMP / "scratch-3114"
    for scratch, stop in (("E:COBOL-coordscratch", S3114 / "STOP-loop"), (S3114, "E:COBOL-coordscratchSTOP-loop"),
                          (S3114, S3114 / "STOP")):
        expect.equal(f"PB3114 (a): --scratch {scratch} --stop-file {stop} is refused first, nothing reserved, recorded or written",
              (main3114(scratch, stop, pw.MAKE_SPECS), state3114(), S3114.exists()), (2, before, False))
    expect.equal("PB3114 (b): a RED check reserves no code, records nothing and writes no args file; its one render recorded nothing",
          (main3114(S3114, S3114 / "STOP-loop", FAKE_SPECS), state3114(), (S3114 / "wf-args-w9114.json").exists(),
           (S3114 / "fake-calls.txt").read_text(encoding="utf-8").split(chr(10))[0].split()[:1]),
          (1, before, False, ["--no-record"]))


def case_192_pb3114_c_green_reserves_the_group_s_code(expect):
    rc3114 = main3114(S3114, S3114 / "STOP-loop", pw.MAKE_SPECS)
    after_code, after_ledger = state3114()
    spec3114 = (S3114 / "msg-w9114-a.txt").read_text(encoding="utf-8")
    expect.equal("PB3114 (c): GREEN reserves the group's codes after the check, renders them and records the group",
          (rc3114, after_code - before[0], pw.alloc.render("code", before[0], 3) in spec3114,
           [(g["wave"], g["letter"], g["notes"]) for g in after_ledger], (S3114 / "wf-args-w9114.json").is_file()),
          (0, 3, True, [("9114", "A", ["PB3114"])], True))


def case_193_pb3700_a_a_red_check_with_wave_next_rese(expect):
    global N3114, next_before
    release_dispatch(C3114, "9114")
    # kb/Work PB3700: `--wave next` takes its number from the allocator (alloc.py wave) and RESERVES it only once the specs
    # check GREEN, like the codes: a RED check leaves it free, a GREEN one renders the spec under it and the next plan, or a
    # hand dispatch asking the allocator, gets the one after
    N3114 = TMP / "scratch-3114-next"
    next_before = pw.alloc.peek("wave", coord.REPO, C3114)
    expect.equal("PB3700 (a): a RED check with --wave next reserves no wave number",
          (main3114(N3114, N3114 / "STOP-loop", FAKE_SPECS, "next"), pw.alloc.peek("wave", coord.REPO, C3114)),
          (1, next_before))


def case_194_pb3700_b_a_green_check_with_wave_next_re(expect):
    rc_next = main3114(N3114, N3114 / "STOP-loop", pw.MAKE_SPECS, "next")
    expect.equal("PB3700 (b): a GREEN check with --wave next reserves the number it rendered; the next asker gets the one after",
          (rc_next, (N3114 / f"msg-w{next_before}-a.txt").is_file(), pw.alloc.peek("wave", coord.REPO, C3114),
           [g["wave"] for g in coord.ledger_groups(C3114)]),
          (0, True, next_before + 1, [str(next_before)]))


def case_195_pb3700_c_a_hand_dispatch_takes_its_wave(expect):
    release_dispatch(C3114, str(next_before))
    expect.equal("PB3700 (c): a hand dispatch takes its wave number from the same allocator, past the planner's",
          pw.alloc.allocate("wave", 1, coord.REPO, C3114), (next_before + 1, next_before + 1))


def case_196_pb3114_d_make_dispatch_specs_py_records(expect):
    # a hand-planned groups file whose practices check is RED: a standing requirement its spec body does not carry (PB3359)
    g3114 = json.loads((S3114 / "groups.json").read_text(encoding="utf-8"))["groups"][0]
    gred = TMP / "groups-3114-red.json"
    gred.write_text(json.dumps({"wave": "9115", "scratch": str(S3114), "base": "abc", "stop_file": str(S3114 / "STOP-x"),
                                "groups": [{**g3114, "letter": "B", "slug": "w9115b",
                                            "requirements": {"PB1": ["a requirement no rendered spec carries (3114)"]}}]}),
                    encoding="utf-8", newline="\n")
    expect.equal("PB3114 (d): make_dispatch_specs.py records nothing when its practices check is RED",
          (mds.main([str(gred), "--coord", str(C3114)]), coord.ledger_groups(C3114)), (1, []))


def case_197_pb3528_two_split_branches_sharing_a_file(expect):
    global DL_BUSY, DL_REPORTS, branch, lead, slug
    # PB3528: two SPLIT branches whose file sets share a file. Each finisher met the OTHER branch (a DECISION owner, read
    # RUNNING) in the partition and waited, so neither was ever planned. A branch another group of this same wave
    # finishes is planned work: the two finishers chain, the later one a successor (`after:`) of the earlier.
    DL_REPORTS = TMP / "reports-3528"
    DL_REPORTS.mkdir()
    for slug, lead, branch in (("w1141a", "PB5", "worktree-dl-1"), ("w1141b", "PB9", "worktree-dl-2")):
        (DL_REPORTS / f"{slug}-{lead}-report.md").write_text(
            f"SPLIT\n# {lead} group\n**Status:** SPLIT at the turn cap · **branch:** `{branch}` · **HEAD:** `5555555ee`\n\n"
            "## Reproduced?\n", encoding="utf-8", newline="\n")
    returned(DL_REPORTS)
    DL_BUSY = {"worktree dl-1 (worktree-dl-1)": InFlight({F_BIND, "docs/CONFORMANCE.md"}, "worktree-dl-1", {"PB5"},
                                                            state="DECISION"),
               "worktree dl-2 (worktree-dl-2)": InFlight({"src/X/Misc.cs", "docs/CONFORMANCE.md"}, "worktree-dl-2",
                                                            {"PB9"}, state="DECISION")}
    pdl = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9")], [], load_reports(DL_REPORTS),
                  lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=DL_BUSY)
    dl = {n: g for g in pdl["groups"] for n in g.notes}
    expect.equal("PB3528: two SPLIT branches sharing a file are both finished, by ONE finisher continuing both (kb/Work PB4396)",
          (sorted(dl), dl.get("PB9") is dl.get("PB5"), dl["PB5"].kind if "PB5" in dl else None,
           sorted(dl["PB5"].continues) if "PB5" in dl else None, dl["PB5"].after if "PB5" in dl else None),
          (["PB5", "PB9"], True, "finisher", ["worktree-dl-1", "worktree-dl-2"], ""))
    expect.equal("PB3528: neither waits on the other", sorted(n for n in pdl["waiting"] if n in ("PB5", "PB9")), [])


def case_198_pb3528_reports_tied_on_wave_and_mtime_ar(expect):
    # ... and on every host alike: the two reports tie on wave and modification time (returned() above), so only their
    # NAME orders them; left to the directory's enumeration order (the name on NTFS, a hash order on ext4) the successor
    # was PB5's finisher under the Linux gate and PB9's on Windows. A reversed listing must not change the order read.
    _glob = pathlib.Path.glob
    try:
        pathlib.Path.glob = lambda self, pattern: reversed(list(_glob(self, pattern)))
        reversed_listing = [r.path.name for r in load_reports(DL_REPORTS)]
    finally:
        pathlib.Path.glob = _glob
    expect.equal("PB3528: reports tied on wave and mtime are read in name order, whatever order the directory lists them in",
          (reversed_listing, [r.path.name for r in load_reports(DL_REPORTS)]),
          (["w1141a-PB5-report.md", "w1141b-PB9-report.md"], ["w1141a-PB5-report.md", "w1141b-PB9-report.md"]))


def case_199_pb3528_an_undecided_branch_this_wave_doe(expect):
    # ... and a branch no group of this wave finishes still holds a group that meets it (the partition is unchanged)
    pdl2 = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                   busy={"worktree dl-3 (worktree-dl-3)": running({F_BIND}, "worktree-dl-3", {F_BIND: ["Binder.Bind"]},
                                                                    notes={"PB9"}, state="DECISION")})
    expect.equal("PB3528: an undecided branch this wave does not finish still holds a group on its files",
          ([g.notes for g in pdl2["groups"]], "PB5" in pdl2["waiting"]), ([], True))


def case_200_pb3528_r1_1_a_one_sided_meeting_between(expect):
    # ... and a ONE-SIDED meeting still chains (branch review R1-1): PB5's group meets PB9's branch on a file PB5's notes
    # own (its primary file) while PB9's group never meets PB5's branch; whichever is placed first, the other follows it.
    DL1_BUSY = {"worktree dl-1 (worktree-dl-1)": InFlight({F_BIND}, "worktree-dl-1", {"PB5"}, state="DECISION"),
                "worktree dl-2 (worktree-dl-2)": InFlight({"src/X/Misc.cs", "docs/CONFORMANCE.md"}, "worktree-dl-2",
                                                             {"PB9"}, state="DECISION")}
    pdl3 = pw.plan(notes, [cl("docs/CONFORMANCE.md", "PB5"), cl("src/X/Misc.cs", "PB9")], [], load_reports(DL_REPORTS),
                   lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=DL1_BUSY)
    dl3 = {n: g for g in pdl3["groups"] for n in g.notes}
    expect.equal("PB3528 R1-1: a one-sided meeting between two finishers makes them ONE group; neither runs beside the other",
          (sorted(dl3), dl3.get("PB5") is dl3.get("PB9"), len(pdl3["groups"])), (["PB5", "PB9"], True, 1))


def case_201_pb4352_two_finishers_each_meeting_a_bran(expect):
    # PB4352: ... and a branch is read as the branch its work LANDS THROUGH. Two finished branches each carry a SPLIT
    # predecessor their review finisher merged (54d-26 carried 54d-2, 54d-29 carried 54d-25 and 54d-4); the survey reads
    # each carried branch IN FLIGHT, and each one's files met the OTHER carrier's finisher. Keyed on the exact branch, the
    # exemption held both finishers on a branch that lands only through the other finisher, and `--drain` planned 0 groups.
    CARRIED = {"worktree-dl-1b": "worktree-dl-1", "worktree-dl-2b": "worktree-dl-2"}
    DL4_BUSY = {**DL_BUSY,
                "worktree dl-1b (worktree-dl-1b)": InFlight({"src/X/Misc.cs"}, "worktree-dl-1b", set(), state="IN FLIGHT"),
                "worktree dl-2b (worktree-dl-2b)": InFlight({F_BIND}, "worktree-dl-2b", set(), state="IN FLIGHT")}
    pdl4 = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9")], [], load_reports(DL_REPORTS),
                   lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=DL4_BUSY, carrier=CARRIED.get)
    dl4 = {n: g for g in pdl4["groups"] for n in g.notes}
    expect.equal("PB4352: two finishers each meeting a branch the OTHER one's branch carries are both planned, as ONE group",
          (sorted(dl4), sorted(n for n in pdl4["waiting"] if n in ("PB5", "PB9")), dl4.get("PB5") is dl4.get("PB9"),
           len(pdl4["groups"])), (["PB5", "PB9"], [], True, 1))
    expect.equal("PB4352: a finisher's claim includes what the branch it continues CARRIES (the carried branch's files)",
          sorted(dl4["PB5"].claim.files) if "PB5" in dl4 else None, sorted({F_BIND, "docs/CONFORMANCE.md", "src/X/Misc.cs"}))


def case_202_pb4352_a_branch_carried_by_one_this_wave(expect):
    # ... and a carried branch whose carrier no group of this wave finishes still holds a group that meets it
    pdl5 = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
                   busy={"worktree dl-3b (worktree-dl-3b)": running({F_BIND}, "worktree-dl-3b", {F_BIND: ["Binder.Bind"]},
                                                                      state="IN FLIGHT")},
                   carrier={"worktree-dl-3b": "worktree-dl-3"}.get)
    expect.equal("PB4352: a branch carried by one this wave does not finish still holds a group on its files",
          ([g.notes for g in pdl5["groups"]], "worktree-dl-3b" in pdl5["waiting"].get("PB5", "")), ([], True))


def case_203_pb4352_r1_1_a_branch_on_main_has_no_carr(expect):
    global FR_REPORTS, fr_carrier, fr_classify
    # ... and a branch whose tip is ON MAIN carries no work, so it has no carrier (branch review R1-1): a live agent's
    # worktree before its first commit is an ancestor of every finished branch that merged main, and read through the real
    # `branch_carrier` it was the work of the newest one, whose FINDINGS finisher was then exempt from the agent's files.
    FR_REPORTS = TMP / "reports-4352"
    FR_REPORTS.mkdir()
    (FR_REPORTS / "w1150a-PB9-report.md").write_text(
        "# PB9 report\n**Status:** DONE · **branch:** `worktree-fd-1` · **HEAD:** `6666666ff`\n"
        "**Review:** FINDINGS 1 r1 6666666ff E:/s/review-fd-1.jsonl\n\n## Reproduced?\n",
        encoding="utf-8", newline="\n")
    returned(FR_REPORTS)
    FR_ON_MAIN = {"worktree-fresh"}                                       # tip == origin/main: classify says MERGED
    fr_classify = lambda b: "MERGED" if b in FR_ON_MAIN else "UNLANDED"   # noqa: E731
    fr_asked = []


    def fr_is_ancestor(a: str, c: str) -> bool:
        """git merge-base --is-ancestor answers yes for an on-main tip against any branch that merged main."""
        fr_asked.append((a, c))
        return a in FR_ON_MAIN


    fr_carrier = inb.branch_carrier(load_reports(FR_REPORTS), fr_classify, fr_is_ancestor)
    expect.equal("PB4352 R1-1: a branch on main has no carrier, and git is not asked", (fr_carrier("worktree-fresh"), fr_asked),
          (None, []))


def case_204_pb4352_r1_1_a_live_worktree_on_main_stil(expect):
    pfr = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5"), on_members(F_BIND, ["Binder.Bind"], "PB9")], [],
                  load_reports(FR_REPORTS), fr_classify, {}, RULES, budget_points=100, carrier=fr_carrier,
                  busy={"worktree fresh (worktree-fresh)": running({F_BIND}, "worktree-fresh", {F_BIND: ["Binder.Bind"]},
                                                                    state="IN FLIGHT")})
    expect.equal("PB4352 R1-1: a live worktree on main still holds a finisher on the files it edits",
          ([g.notes for g in pfr["groups"]], sorted(n for n in pfr["waiting"] if n in ("PB5", "PB9"))),
          ([], ["PB5", "PB9"]))


def case_205_pb3625_reports_are_read_oldest_first_by(expect):
    global O_REPORTS, branch, d, k, name, order, status, t, when
    # PB3625: THE REPORT ORDER IS A CLOCK, NEVER A WAVE NUMBER. The loop numbers its waves by date (20261024) and the Mythos
    # lane serially (1157), so ordering by the integer wave ranked every date-numbered report above every serial one: a
    # SPLIT report of the old branch (w20261024a, 15:12) stayed PB5's newest beside a DONE report (w1157a, 19:49) of a branch
    # that CONTAINS it, and the planner planned a finisher that would redo work already on the other branch. The report
    # written later is the newest in either direction; the wave number only breaks a tie (the same-time reports above).
    O_REPORTS = {k: TMP / f"reports-3625{k}" for k in ("a", "b")}
    for k, d in O_REPORTS.items():
        d.mkdir()
        for name, status, branch in (("w20261024a-PB5-report.md", "SPLIT", "worktree-old-1"),
                                     ("w1157a-PB5-report.md", "DONE", "worktree-new-2")):
            (d / name).write_text(f"{status}\n# PB5 group\n**Status:** {status} · **branch:** `{branch}` · **HEAD:** `6666666ff`\n\n"
                                  "## Reproduced?\n", encoding="utf-8", newline="\n")
        for name, when in (("w20261024a-PB5-report.md", 1_000_000_000), ("w1157a-PB5-report.md", 1_000_003_600)):
            # "a": the serial DONE is written an hour AFTER the date-numbered SPLIT; "b": an hour BEFORE it
            t = when if k == "a" else 1_000_000_000 + 1_000_003_600 - when
            os.utime(d / name, (t, t))
    order = {k: [r.path.name for r in load_reports(d)] for k, d in O_REPORTS.items()}
    expect.equal("PB3625: reports are read oldest first by write time, whichever lane's wave number is larger",
          order, {"a": ["w20261024a-PB5-report.md", "w1157a-PB5-report.md"],
                  "b": ["w1157a-PB5-report.md", "w20261024a-PB5-report.md"]})


def case_206_pb3625_a_later_serial_wave_done_report_s(expect):
    po = {k: pw.plan(notes, [cl(F_BIND, "PB5")], [], load_reports(d), lambda b: "UNLANDED", {}, RULES,
                     budget_points=100) for k, d in O_REPORTS.items()}
    expect.equal("PB3625: a later serial-wave DONE report supersedes the date-wave SPLIT report: no finisher of the SPLIT branch",
          [b for g in po["a"]["groups"] if g.kind == "finisher" and "PB5" in g.notes for b in g.continues], [])
    expect.equal("PB3625: the converse holds: a SPLIT report written after the DONE one is the note's newest and gets its finisher",
          [b for g in po["b"]["groups"] if g.kind == "finisher" and "PB5" in g.notes for b in g.continues], ["worktree-old-1"])


def case_207_pb3170_a_x_whose_four_commits_y_replays(expect):
    global RG, RG_GRAPH, i, x_commits
    # PB3170: A BRANCH ANOTHER BRANCH REPLAYS IS NEVER PLANNED FROM. The planner's own FINISH-FIRST pred tells a SPLIT
    # successor to CHERRY-PICK its predecessor, so the successor holds the work as other commits and is no descendant of
    # it (`branch_carrier` reads ancestry of DONE branches only). Wave 1079 planned PB3127 from w1077b's branch `…-28e-2`
    # although `…-3a0-2` (w1078b, SPLIT, planned in the same wave) replayed all four of its commits.
    # (a) THE RULE, on real commits (a scratch repository, the cherry-picks `git cherry` matches by patch id): x holds four
    # commits; y cherry-picks them and adds one; z cherry-picks two; q is cut from x with one of its own; w is unrelated.
    isolate()
    RG = ScratchRepo.init(TMP / "repo-3170")
    RG.commit("base", {"a.txt": "base\n"})
    RG.git("update-ref", git_batch.BASE, "HEAD")
    RG.git("switch", "-q", "-c", "x")
    for i in range(1, 5):
        RG.commit(f"x{i}", {f"x{i}.txt": f"{i}\n"})
    x_commits = RG.git("rev-list", "--reverse", f"{git_batch.BASE}..x").split()
    RG.git("switch", "-q", "-c", "y", "main")
    RG.git("cherry-pick", *x_commits)
    RG.commit("y5", {"y5.txt": "5\n"})
    y5 = RG.git("rev-parse", "HEAD")
    RG.git("switch", "-q", "-c", "z", "main")
    RG.git("cherry-pick", *x_commits[:2])
    RG.git("switch", "-q", "-c", "q", "x")
    RG.commit("q1", {"q1.txt": "q\n"})
    RG.git("switch", "-q", "-c", "w", "main")
    RG.commit("w1", {"w1.txt": "w\n"})
    w1 = RG.git("rev-parse", "HEAD")
    RG_ORDER = ["x", "y", "z", "q", "w"]
    RG_GRAPH = git_batch.Ancestry.read(RG.path, RG_ORDER)
    found = prune.replays(RG_ORDER, RG_ORDER, RG_ORDER.index, RG_GRAPH)
    expect.equal("PB3170 (a): x, whose four commits y replays by cherry-pick and q holds as ancestors, is carried (by the branch "
          "that replays the most, the later of two ties); it has no commit of its own beyond them",
          (found["x"].carrier, found["x"].fresh, found["x"].by), ("q", (), (("q", 4), ("y", 4))))
    expect.equal("PB3170 (a): z, two of x's commits cherry-picked, is carried too, by every larger UNCARRIED branch (x is carried)",
          (found["z"].carrier, found["z"].fresh, [b for b, _ in found["z"].by]), ("q", (), ["q", "y"]))
    expect.equal("PB3170 (a): y is NOT carried: its fifth commit is in no larger branch (fresh), and q replays the other four",
          (found["y"].carrier, found["y"].fresh, found["y"].by), (None, (y5,), (("q", 4),)))
    expect.equal("PB3170 (a): the largest of the family, q, carries nothing: exactly one branch still does the work",
          (found["q"].carrier, len(found["q"].fresh), found["q"].by), (None, 5, ()))
    expect.equal("PB3170 (a): an unrelated branch is replayed by nobody", (found["w"].carrier, found["w"].fresh, found["w"].by),
          (None, (w1,), ()))


def case_208_pb3170_a_two_branches_holding_the_same_c(expect):
    # the tie: two branches holding the same work cannot each carry the other; the later report's carries the earlier's
    RG.git("switch", "-q", "-c", "x2", "x")
    tie = prune.replays(["x", "x2"], ["x", "x2"], {"x": 0, "x2": 1}.get, git_batch.Ancestry.read(RG.path, ["x", "x2"]))
    expect.equal("PB3170 (a): two branches holding the same commits: the later report carries the earlier, never both ways",
          (tie["x"].carrier, tie["x2"].carrier), ("x2", None))


def case_209_pb3651_x_is_replayed_in_full_by_a_larger(expect):
    global TB_ORDER, br, extra, i
    # PB3651: the tie is on the NUMBER of x's commits each carrier replays, and then on the LATER report, never on the
    # carrier's size: lg (an EARLIER report) holds x's four commits and three of its own (seven patches), mid (a LATER
    # report) holds them and one of its own (five). Both are uncarried and both replay all four, so x rides on mid.
    for br, extra in (("lg", 3), ("mid", 1)):
        RG.git("switch", "-q", "-c", br, "main")
        RG.git("cherry-pick", *x_commits)
        for i in range(extra):
            RG.commit(f"{br}{i}", {f"{br}{i}.txt": f"{i}\n"})
    TB_ORDER = ["x", "lg", "mid"]
    tb = prune.replays(TB_ORDER, TB_ORDER, TB_ORDER.index, git_batch.Ancestry.read(RG.path, TB_ORDER))
    expect.equal("PB3651: x is replayed in full by a larger earlier carrier and a smaller later one: the LATER report carries it",
          (tb["x"].carrier, [n for n, _ in tb["x"].by]), ("mid", ["lg", "mid"]))
    expect.equal("PB3651: ... and the two uncarried branches carry nothing", (tb["lg"].carrier, tb["mid"].carrier), (None, None))


def case_210_pb3651_with_the_reports_order_reversed_t(expect):
    tb_rev = prune.replays(TB_ORDER, TB_ORDER, ["x", "mid", "lg"].index, git_batch.Ancestry.read(RG.path, TB_ORDER))
    expect.equal("PB3651: ... with the reports' order reversed the larger branch is the later report, and carries x",
          tb_rev["x"].carrier, "lg")


def case_211_pb3170_a_in_a_successor_chain_every_carr(expect):
    global br
    # the successor CHAIN (review R1-1): c-x holds four commits; a-mid (a later report) cherry-picks them; b-top (the latest)
    # cherry-picks a-mid and adds one. a-mid is carried by b-top, so it gets no finisher: c-x's carrier is b-top, never the
    # carried middle link a-mid. The names are chosen so that the name order and the report order disagree (the old rule
    # broke the tie on NAME, and a-mid sorts before b-top).
    for br, picks in (("c-x", x_commits), ("a-mid", x_commits)):
        RG.git("switch", "-q", "-c", br, "main")
        RG.git("cherry-pick", *picks)
    mid_commits = RG.git("rev-list", "--reverse", f"{git_batch.BASE}..a-mid").split()
    RG.git("switch", "-q", "-c", "b-top", "main")
    RG.git("cherry-pick", *mid_commits)
    RG.commit("t5", {"t5.txt": "5" + chr(10)})
    CH_ORDER = ["c-x", "a-mid", "b-top"]
    chain = prune.replays(CH_ORDER, CH_ORDER, CH_ORDER.index, git_batch.Ancestry.read(RG.path, CH_ORDER))
    expect.equal("PB3170 (a): in a successor chain every carried link rides on the TOP branch, never on a carried middle link",
          [(chain[b].carrier, chain[b].fresh == ()) for b in CH_ORDER], [("b-top", True), ("b-top", True), (None, False)])
    expect.equal("PB3170 (a): ... and `by` names only branches that have a finisher",
          [[n for n, _ in chain[b].by] for b in CH_ORDER], [["b-top"], ["b-top"], []])


def case_212_pb3170_b_x_is_replayed_by_y_when_y_has_a(expect):
    # (b) THROUGH THE PLANNER'S OWN WIRING (branch_replays: the reports' order is the rank, ABANDONED carries nothing)
    rg_reports = [Report(pathlib.Path(f"{slug}-{lead}-report.md"), int(slug[1:5]), slug, lead, "SPLIT", [lead], False,
                            branch, None, "7777777aa")   # oldest first: x's report, then y's
                  for slug, lead, branch in (("w1077b", "PB6", "x"), ("w1078b", "PB5", "y"))]
    rep_all = branch_replays(rg_reports, lambda b: "UNLANDED", {}, RG_GRAPH, set())
    rep_ab = branch_replays(rg_reports, lambda b: "UNLANDED", {}, RG_GRAPH, {"y"})
    rep_land = branch_replays(rg_reports, lambda b: "LANDED" if b == "y" else "UNLANDED", {}, RG_GRAPH, set())
    expect.equal("PB3170 (b): x is replayed by y when y has a later report (y's own fifth commit is not x's business)",
          (rep_all("x").carrier, rep_all("x").fresh), ("y", ()))
    expect.equal("PB3170 (b): an ABANDONED branch carries nothing, and a LANDED one is no unlanded branch to carry",
          (rep_ab("x").carrier, rep_ab("x").by, rep_land("x").carrier), (None, (), None))


def case_213_pb3170_b_abandon_records_agrees_with_aba(expect):
    # abandon_records is abandon_record for a list, in one pass over the notes
    AB = TMP / "Work-3170"
    AB.mkdir()
    (AB / "PB901.md").write_text("`old-a` is ABANDONED, superseded by `new-a`.\nnot abandoned: old-b\n", encoding="utf-8", newline="\n")
    (AB / "PB902.md").write_text("nothing here\n`old-c` abandoned (wave 1, SPLIT).\n", encoding="utf-8", newline="\n")
    ab_names = ["old-a", "new-a", "old-b", "old-c", "none"]
    expect.equal("PB3170 (b): abandon_records agrees with abandon_record for each branch, in one pass",
          prune.abandon_records(ab_names, AB), {b: n for b in ab_names if (n := prune.abandon_record(b, AB))})
    expect.equal("PB3170 (b): ... and names the note that records it, never a branch a line merely mentions",
          prune.abandon_records(ab_names, AB), {"old-a": "PB901", "old-c": "PB902"})


def case_214_pb3170_c_without_the_rule_both_split_bra(expect):
    global B28, B3A, FULL, before, continuing, plan3170
    # (c) THE PLAN. PB6's newest report (w1077b) is a SPLIT on `…-28e-2`; PB5's (w1078b, later) a SPLIT on `…-3a0-2`.
    R_REPORTS = TMP / "reports-3170"
    R_REPORTS.mkdir()
    B28, B3A = "worktree-r-28e-2", "worktree-r-3a0-2"


    def r3170(slug, lead, status, branch, when):
        p = R_REPORTS / f"{slug}-{lead}-report.md"
        p.write_text(f"{status}\n# {lead} group\n**Status:** {status} · **branch:** `{branch}` · **HEAD:** `7777777aa`\n\n"
                     "## Reproduced?\n", encoding="utf-8", newline="\n")
        os.utime(p, (when, when))


    def plan3170(carrier_status, replay=None, abandoned=None, unlanded=None, clusters=None, held=None):
        for p in R_REPORTS.glob("*.md"):
            p.unlink()
        r3170("w1077b", "PB6", "SPLIT", B28, 1_000_000_000)
        r3170("w1078b", "PB5", carrier_status, B3A, 1_000_003_600)
        return pw.plan(notes, clusters or [cl(F_BIND, "PB5", "PB6")], [], load_reports(R_REPORTS), lambda b: "UNLANDED",
                       unlanded or {}, RULES, budget_points=100, replay=replay, abandoned=abandoned, held=held)


    def continuing(p, branch):
        return [sorted(g.notes) for g in p["groups"] if branch in g.continues]


    FULL = {B28: prune.Replay(B3A, (), ((B3A, 4),)), B3A: prune.Replay(None, ("f" * 12,), ())}
    before = plan3170("SPLIT")
    expect.equal("PB3170 (c): WITHOUT the rule both SPLIT branches are finished (the defect: the same four commits land twice; "
          "one finisher continues both, one step per file)",
          (continuing(before, B28), continuing(before, B3A)), ([["PB5", "PB6"]], [["PB5", "PB6"]]))


def case_215_pb3170_c_a_split_branch_another_split_br(expect):
    after = plan3170("SPLIT", replay=FULL.get)
    expect.equal("PB3170 (c): a SPLIT branch another SPLIT branch replays gets no finisher; its note rides on the carrier's group",
          (continuing(after, B28), continuing(after, B3A)), ([], [["PB5", "PB6"]]))
    expect.equal("PB3170 (c): ... and the carrier's pred names the branch it carries",
          [("ride on this branch" in g.pred, B28 in g.pred) for g in after["groups"] if B3A in g.continues], [(True, True)])


def case_216_pb3170_c_the_carrier_done_it_lands_on_it(expect):
    done = plan3170("DONE", replay=FULL.get)
    expect.equal("PB3170 (c): the carrier DONE (it lands on its own): the carried note waits for it, with the reason, no finisher",
          (continuing(done, B28), "PB6" in done["waiting"] and B3A in done["waiting"]["PB6"]), ([], True))


def case_217_pb3170_c_a_branch_only_partly_replayed_k(expect):
    part = plan3170("SPLIT", replay={B28: prune.Replay(None, ("abc123def456",), ((B3A, 3),)), B3A: FULL[B3A]}.get)
    expect.equal("PB3170 (c): a branch only PARTLY replayed keeps its finisher, whose pred names only the commits no other branch holds",
          (continuing(part, B28), [("ONLY" in g.pred, "abc123def" in g.pred, B3A in g.pred) for g in part["groups"]
                                   if B28 in g.continues]), ([["PB5", "PB6"]], [(True, True, True)]))


def case_218_pb3170_c_an_abandoned_branch_is_never_pl(expect):
    gone = plan3170("SPLIT", abandoned={B28})
    expect.equal("PB3170 (c): an ABANDONED branch is never planned from: no finisher continues it, its note is planned from main",
          (continuing(gone, B28), any("PB6" in g.notes and not g.continues for g in gone["groups"]) or
           any("PB6" in g.notes for g in gone["groups"] if B3A in g.continues)), ([], True))


def case_219_pb3170_c_the_sibling_arm_a_report_less_u(expect):
    # the unlanded-branch arm (a branch with commits naming a note and NO report), and the half-note arm, are the siblings
    two = pw.plan(notes, [cl(F_EMIT, "PB7")], [], [], lambda b: "UNLANDED", {"b-1": ["PB7"], "b-2": ["PB7"]}, RULES,
                  budget_points=100, replay={"b-1": prune.Replay("b-2", (), (("b-2", 2),))}.get)
    expect.equal("PB3170 (c): the sibling arm: a report-less unlanded branch another branch replays gets no finisher either",
          [g.continues for g in two["groups"]], [["b-2"]])


def case_220_pb3170_c_and_the_half_note_arm_resumes_f(expect):
    twoh = pw.plan(notes, [], [cl("src/X/Half.cs", "PB4")], [], lambda b: "UNLANDED", {"b-1": ["PB4"], "b-2": ["PB4"]}, RULES,
                   budget_points=100, replay={"b-1": prune.Replay("b-2", (), (("b-2", 2),))}.get)
    expect.equal("PB3170 (c): ... and the half-note arm resumes from the replaying branch only",
          [g.continues for g in twoh["groups"]], [["b-2"]])


def case_221_pb3170_x_pb2974_a_dispatch_holding_the_c(expect):
    global hold
    # (d) THE TWO RULES COMPOSE: a rider (PB3170) belongs to the replaying branch's UNIT (kb/Work PB2974). A dispatch holding
    # the carrier's note holds the rider too; the carrier's one finisher takes the rider AND the unreported notes its own
    # branch carries, naming the replayed commits once; a report-less branch's one finisher names its fresh commits.
    hold = plan3170("SPLIT", replay=FULL.get, held={"PB5": "dispatch w1080a"})
    expect.equal("PB3170 x PB2974: a dispatch holding the carrier's note holds the rider on it: no finisher over either branch",
          (continuing(hold, B28), continuing(hold, B3A)), ([], []))
    expect.equal("PB3170 x PB2974: ... and the rider waits naming the held note and its dispatch",
          ("PB5" in hold["waiting"].get("PB6", ""), "w1080a" in hold["waiting"].get("PB6", "")), (True, True))


def case_222_pb3170_x_pb2974_the_carrier_s_one_finish(expect):
    global both
    PART3A = {B28: FULL[B28], B3A: prune.Replay(None, ("fedcba987654",), (("worktree-r-zz", 1),))}
    both = plan3170("SPLIT", replay=PART3A.get, unlanded={B3A: ["PB7"]}, clusters=[cl(F_BIND, "PB5", "PB6", "PB7")])
    expect.equal("PB3170 x PB2974: the carrier's ONE finisher takes its note, the rider and its branch's unreported note",
          (continuing(both, B28), continuing(both, B3A), [g.kind for g in both["groups"]]),
          ([], [["PB5", "PB6", "PB7"]], ["finisher"]))
    expect.equal("PB3170 x PB2974: ... its pred names the rider and the replayed commits, the latter exactly once",
          [("ride on this branch" in g.pred, g.pred.count("cherry-pick ONLY"), "fedcba987" in g.pred, "PB7" in g.pred)
           for g in both["groups"]], [(True, 1, True, True)])


def case_223_pb3170_x_pb2974_a_report_less_branch_par(expect):
    global lone
    lone = pw.plan(notes, [cl(F_EMIT, "PB7")], [], [], lambda b: "UNLANDED", {"b-1": ["PB7"]}, RULES, budget_points=100,
                   replay={"b-1": prune.Replay(None, ("0123456789ab",), (("b-2", 1),))}.get)
    expect.equal("PB3170 x PB2974: a report-less branch partly replayed is ONE finisher naming only its fresh commits",
          [(g.continues, "cherry-pick ONLY" in g.pred, "012345678" in g.pred) for g in lone["groups"]],
          [(["b-1"], True, True)])


def case_224_block(expect):
    global drain, mode, own
    # PB3653: A LIVE AGENT'S LOCK HOLDS A BRANCH'S UNIT, a touch alone does not (prune_worktrees.locked is the lock-only half of
    # `owned`). A hand-started agent on a SPLIT branch leaves no ledger entry, so the dispatch hold (6d above) cannot see it.
    LK = "worktree-wf_un-1"   # w1053b's branch (6d): SPLIT report, notes PB301 and PB303 (PB302 is w1055a's, on another branch); nothing is held
    for drain in (False, True):
        mode = "drain" if drain else "normal"
        lk = pw.plan(UN_NOTES, UN_CL, [], load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                     budget_points=100, drain=drain, locked={LK})
        expect.equal(f"PB3653 {mode}: a SPLIT branch whose worktree a live agent LOCKS gets no finisher for any of its notes",
              [g.notes for g in lk["groups"] if {"PB301", "PB303"} & set(g.notes)], [])
        expect.equal(f"PB3653 {mode}: ... each note waits naming the branch and the lock",
              [("locked by a live agent" in lk["waiting"].get(i, "") and LK in lk["waiting"].get(i, ""))
               for i in ("PB301", "PB303")], [True, True])
        own = pw.plan(UN_NOTES, UN_CL, [], load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                      budget_points=100, drain=drain, owned={LK})
        expect.equal(f"PB3653 {mode}: a branch merely OWNED (a touch inside --min-age, the state of one whose agent just returned) "
              "is still finished: only a lock or a dispatch holds the SPLIT arm",
              [(g.notes, g.continues) for g in own["groups"] if "PB301" in g.notes],
              [(["PB301", "PB303", "PB302"], [LK, "worktree-wf_un-2"])])
    expect.equal("PB3653: another branch's lock holds nothing here",
          [g.notes for g in pw.plan(UN_NOTES, UN_CL, [], load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                                    budget_points=100, locked={"worktree-wf_other-1"})["groups"] if "PB301" in g.notes],
          [["PB301", "PB303", "PB302"]])


def case_225_pb3653_and_an_unlanded_branch_with_no_re(expect):
    lk_ub = pw.plan(UN_NOTES, UN_CL, [], [], lambda b: "UNLANDED", UB, RULES, budget_points=100, locked={"worktree-wf_ub-1"})
    expect.equal("PB3653: ... and an unlanded branch with no report is held by its lock too (one hold loop for every arm)",
          ([g.notes for g in lk_ub["groups"] if {"PB311", "PB312", "PB313"} & set(g.notes)],
           ["locked by a live agent" in lk_ub["waiting"].get(i, "") for i in ("PB311", "PB312", "PB313")]),
          ([], [True, True, True]))


def case_226_pb3653_two_unfinished_reports_about_one(expect):
    global TWO, lead, q, slug, when
    # PB3653's residual: two DIFFERENT unfinished reports about one branch are one finisher group, the newest report's. A
    # note is decided by its newest report, so PB5's (w1090a) and PB6's (w1090b) both name the branch, and they planned two.
    TWO = TMP / "reports-two-on-one"
    TWO.mkdir()
    for slug, lead, when in (("w1090a", "PB5", 1_000_000_000), ("w1090b", "PB6", 1_000_003_600)):
        q = TWO / f"{slug}-{lead}-report.md"
        q.write_text(f"SPLIT\n# {lead} group\n**Status:** SPLIT · **branch:** `worktree-two-1` · **HEAD:** `7777777aa`\n\n"
                     "## Reproduced?\n", encoding="utf-8", newline="\n")
        os.utime(q, (when, when))
    two_on_one = pw.plan(notes, [cl(F_BIND, "PB5", "PB6")], [], load_reports(TWO), lambda b: "UNLANDED", {}, RULES,
                         budget_points=100)
    expect.equal("PB3653: two unfinished reports about one branch plan ONE finisher over it, naming the newest report",
          [(g.kind, sorted(g.notes), g.continues, "w1090b" in g.pred) for g in two_on_one["groups"]],
          [("finisher", ["PB5", "PB6"], ["worktree-two-1"], True)])


def case_227_pb3653_r1_1_an_unfinished_report_s_finis(expect):
    global lead, q, slug, status, when
    # Branch review R1-1: the merge target is the newest UNFINISHED report. A newer DONE report about another lead on the same
    # branch is no target: the finisher's pred would say "w1090b returned DONE for PB5" and name the wrong report.
    TDN = TMP / "reports-two-done-newest"
    TDN.mkdir()
    for slug, lead, status, when in (("w1090a", "PB5", "SPLIT", 1_000_000_000), ("w1090b", "PB6", "DONE", 1_000_003_600)):
        q = TDN / f"{slug}-{lead}-report.md"
        q.write_text(f"{status}\n# {lead} group\n**Status:** {status} · **branch:** `worktree-two-1` · **HEAD:** `7777777aa`\n\n"
                     "## Reproduced?\n", encoding="utf-8", newline="\n")
        os.utime(q, (when, when))
    two_done = pw.plan(notes, [cl(F_BIND, "PB5", "PB6")], [], load_reports(TDN), lambda b: "UNLANDED", {}, RULES,
                       budget_points=100)
    expect.equal("PB3653 R1-1: an unfinished report's finisher is not merged into a newer DONE report of its branch (pred names the SPLIT one)",
          [(g.kind, sorted(g.notes), "w1090a" in g.pred, "w1090b" in g.pred) for g in two_done["groups"]],
          [("finisher", ["PB5"], True, False)])


def case_228_pb3476_a_head_field_that_gives_no_sha_se(expect):
    global HD
    # PB3476: THE COMMIT A REPORT DESCRIBES is its header's HEAD, else the head its review stamp names; the one parser
    # (parse_report) is read by the branch survey, the landing lane and the landing measure.
    HD = TMP / "reports-head"
    HD.mkdir()
    STAMP = "**Review:** CLEAN w1136c-review-r1 9d9980b0e922"


    def head_of(name, header):
        q = HD / name
        q.write_text(header + "\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
        return parse_report(q).head


    expect.equal("PB3476: a HEAD field that gives no sha ('see the trailing commit') takes the head of the review stamp",
          head_of("w1136c-PB3471-report.md",
                  f"Status: DONE\n{STAMP}\n**Branch:** worktree-x-1\n**HEAD:** see the trailing commit (code at `b4c53e5a`)\n"),
          "9d9980b0e922")
    expect.equal("PB3476: ... a header with a HEAD sha keeps it, the stamp notwithstanding",
          head_of("w1136d-PB3472-report.md", f"Status: DONE\n{STAMP}\n**Branch:** worktree-x-2\n**HEAD:** 1234567abc\n"),
          "1234567abc")
    expect.equal("PB5334: the combined `Base/Head: <base> / <head>` field gives the HEAD its SECOND sha (w20262000c's header)",
          head_of("w1136f-PB3474-report.md", "Status: DONE\nBranch: worktree-x-4\n"
                  "Base/Head: 74bd6713166c0e8a247df36e8d25ab1dacdd4689 / 0a97e37a1c0b64c44342abcacde61ae5b0d33f21\n"),
          "0a97e37a1c0b64c44342abcacde61ae5b0d33f21")
    expect.equal("PB5334: ... and the BASE its first",
          parse_report(HD / "w1136f-PB3474-report.md").base, "74bd6713166c0e8a247df36e8d25ab1dacdd4689")
    expect.equal("PB3476: ... and a header with neither describes no commit",
          head_of("w1136e-PB3473-report.md", "Status: DONE\n**Branch:** worktree-x-3\n**HEAD:** see the trailing commit\n"),
          None)


def case_229_pb3474_no_module_outside_coord_py_tests(expect):
    # PB3474: THE NEVER-WORK RULE HAS ONE HOME, coord.NEVER_WORK (coord.is_work, coord.NEVER_WORK_PATHSPECS). A module that
    # tests a path against STATUS.md, TestResults/ or the settings file in its own way is a second copy of it: prune_worktrees'
    # classify() kept one (with a `tools/` exclusion NEVER_WORK lacks, which hid every file added under tools/), and the
    # gate's former impact analysis another.
    def never_work_copies(sources):
        """The modules (not coord.py, the owner, nor a test file: its fixtures name these paths) that compare a path with
    one of coord.NEVER_WORK's entries or test its prefix by hand."""
        shape = re.compile(r"""startswith\(\s*\(?\s*['"](?:TestResults/|STATUS\.md)"""
                           r"""|[!=]=\s*['"](?:STATUS\.md|\.claude/settings\.local\.json)['"]"""
                           r"""|['"](?:STATUS\.md|\.claude/settings\.local\.json)['"]\s*[!=]=""")
        return sorted(p for p, t in sources.items() if p != "scripts/orchestrator/coord.py"
                      and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


    expect.equal("PB3474: no module outside coord.py tests a path against coord.NEVER_WORK by hand", never_work_copies(TREE), [])
    expect.equal("PB3474: the scan fails on the planted shapes of the old copies",
          never_work_copies({"scripts/a.py": "f for f in names if not f.startswith(('TestResults/', 'tools/')) and f != 'STATUS.md'",
                             "scripts/b.py": "{p for p in out if p and p != \"STATUS.md\"}",
                             "scripts/c.py": "if path == '.claude/settings.local.json': continue",
                             "scripts/orchestrator/coord.py": "p == 'STATUS.md'", "scripts/test_d.py": "p != 'STATUS.md'",
                             "scripts/e.py": "(repo / 'STATUS.md').write_text(x)"}),
          ["scripts/a.py", "scripts/b.py", "scripts/c.py"])


def case_230_pb3652_only_prune_worktrees_calls_replay(expect):
    # PB3652: ONE RULE FOR WHO CARRIES WHOM. The planner's branch_replays and the survey's contained() read the same
    # function (prune_worktrees.replay_view); a second call of `replays` from anywhere else is a second reading of its inputs.
    def replay_callers(sources):
        shape = re.compile(r"\breplays\(")
        return sorted(p for p, t in sources.items() if p != "scripts/prune_worktrees.py"
                      and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


    expect.equal("PB3652: only prune_worktrees calls `replays` (the planner and the survey go through replay_view)",
          replay_callers(TREE), [])
    expect.equal("PB3652: the scan fails on a planted direct call",
          replay_callers({"scripts/orchestrator/x.py": "prune_worktrees.replays(branches, carriers, rank, graph)"}),
          ["scripts/orchestrator/x.py"])


def case_231_pb5100_a_planted_cap_of_3_with_an_idle_p(expect):
    def pool_state(cap, held=0, waiting=0):
        d = pathlib.Path("slots")
        return gate_slot.SlotState(d, gate_slot.GateSettings(d), cap, {k: f"gate {k}" for k in range(1, held + 1)},
                                   [(k, "implementer", f"w{k}") for k in range(1, waiting + 1)])


    expect.equal("PB5100: a planted cap of 3 with an idle pool yields 3 implementers", pw.wave_concurrency(pool_state(3))[0], 3)
    expect.equal("PB5100: the cap in force is the ceiling (5 -> 5, 2 -> 2)",
          (pw.wave_concurrency(pool_state(5))[0], pw.wave_concurrency(pool_state(2))[0]), (5, 2))
    expect.equal("PB5303: slots held and tickets waiting at planning time do not reduce the concurrency (cap 3, 2 held, 1 waiting -> 3)",
          (pw.wave_concurrency(pool_state(3, held=1))[0], pw.wave_concurrency(pool_state(3, held=2, waiting=1))[0],
           pw.wave_concurrency(pool_state(3, held=3, waiting=4))[0]), (3, 3, 3))
    expect.equal("PB5303: a cap of 0 still leaves one worker, so the wave progresses", pw.wave_concurrency(pool_state(0))[0], 1)
    expect.equal("PB5303: the reason names the cap and says planning-time holds do not reduce it",
          "cap in force (3)" in pw.wave_concurrency(pool_state(3, held=2))[1]
          and "not reduced by the slots held" in pw.wave_concurrency(pool_state(3, held=2))[1], True)
    expect.equal("PB5100: model_rules.json carries no hand-set wave concurrency", "concurrency" in LIVE_RULES["wave"], False)
    expect.equal("PB5100: the planner passes the derived value, never a rules key",
          "rules[" + chr(34) + "wave" + chr(34) + "][" + chr(34) + "concurrency" + chr(34) + "]"
          in pathlib.Path(pw.__file__).read_text(encoding="utf-8"), False)


def case_232_pb5100_plan_main_reads_the_gate_pool_bef(expect):
    # PB5100 R1-1: the pool is read BEFORE the first reservation, so a malformed slot settings file raises while the plan
    # still changes no shared state (PB3114). Expected: in plan_main the pool read precedes alloc.allocate and the ledger render.
    _pm = pathlib.Path(pw.__file__).read_text(encoding="utf-8").split("def plan_main", 1)[1]
    expect.equal("PB5100: plan_main reads the gate pool before it reserves a wave number or codes",
          0 < _pm.index("GateSlots.for_repo") < _pm.index("alloc.allocate("), True)
    expect.equal("PB5100: plan_main reads the gate pool before the recording render",
          _pm.index("GateSlots.for_repo") < _pm.index("rc := render(gjson)") if "rc := render(gjson)" in _pm
          else _pm.index("GateSlots.for_repo") < _pm.index("rc = render(gjson)"), True)


if __name__ == "__main__":
    sys.exit(selftest_cases.run(sys.modules[__name__]))
