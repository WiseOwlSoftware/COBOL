#!/usr/bin/env python3
"""Self-test for plan_wave.py on fabricated notes, clusters and reports: work awaiting landing is never re-planned,
finishers come first and name their predecessor, clusters rank by rows claimed, a second group on a file is a
same-file successor, the model routing rules, the budget fill (with landings and successors), and that the groups
render through the real dispatch-spec template and pass check_practices.py's same-file rule; that the fix lane's whole
plan equals the golden the pre-campaign planner wrote; and the campaign lane (--cluster, kb/Work PB2120): cluster
selection whatever the harm flags, the blocked_by order, site clustering per depth, the `after:` derivation, the
waits (blockers in two chains, the owner, a frontier model), and work.py check's topology rules.
Run: python scripts/orchestrator/test_plan_wave.py   (no build, no git; the campaign section needs the
tools/claude-skills submodule, and its absence is a FAIL)."""
import dataclasses
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import plan_wave as pw  # noqa: E402
sys.path.insert(0, str(HERE.parent))
from scratch_repo import ScratchRepo, isolate, scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
import prune_worktrees as prune  # noqa: E402  (scripts/: the branch survey; the replay rule is its, kb/Work PB3170)
import git_batch  # noqa: E402  (scripts/: the batched git reads, kb/Work PB3582)

sys.path.insert(0, str(coord.REPO / ".claude" / "skills" / "workstream"))
import check_practices  # noqa: E402
import make_dispatch_specs as mds  # noqa: E402

RULES = coord.rules()
TMP = scratch_dir("plan-wave-test-")
WORK = TMP / "Work"
WORK.mkdir()
REPORTS = TMP / "reports"
REPORTS.mkdir()
fails, checked = [], []


def check(name, got, want):
    checked.append(name)
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def note(nid, status="open", area="runtime", rows=0, body="", wrapped=False):
    rl = ", ".join(f'"SR-{nid}-{k}"' for k in range(rows))
    inv = f"inventory_rows: [{rl}]" if not wrapped else f"inventory_rows: [{rl},\n  \"SR-{nid}-x\"]"
    (WORK / f"{nid}.md").write_text(
        f'---\ntitle: "{nid} — a defect in {area}"\nid: {nid}\nkind: defect\nstatus: {status}\narea: {area}\n'
        f"wrong_answer: true\n{inv}\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8", newline="\n")


def returned(reports_dir):
    """Every report in `reports_dir` is written a minute after its dispatch was recorded (`answers` bounds a report by
    the dispatch's `at`, and a file's mtime may trail the clock by a filesystem tick)."""
    later = pathlib.Path(reports_dir).stat().st_mtime + 60
    for p in pathlib.Path(reports_dir).glob("*-report.md"):
        os.utime(p, (later, later))


def report(name, first_line, header):
    (REPORTS / name).write_text(f"{first_line}\n{header}\n\n## Reproduced?\nPB9999 is mentioned only below the header.\n",
                                encoding="utf-8", newline="\n")


# Notes. PB1/PB2 sit on an unlanded branch; PB3 was split by a landed predecessor; PB4 is half; PB5..PB12 open.
for n in ("PB1", "PB2", "PB3"):
    note(n, rows=2)
note("PB4", status="half", rows=1)
note("PB5", rows=1)
note("PB6", rows=9, wrapped=True)       # a wrapped list: the one frontmatter reader must still count it (9 + 1)
note("PB7", rows=3)
note("PB8", area="oo/classes", rows=1)
note("PB9", rows=1, body="The design question is open: which seam owns this.")
note("PB10", rows=1)
note("PB11", rows=1)
note("PB12", rows=4)
note("PB13", rows=0)
note("PB14", rows=0)
note("PB20", status="landed", rows=5)

report("w1014a-PB1-report.md", "DONE", "# PB1 group\n**branch:** `worktree-wf_aaa-1` · **HEAD:** `1111111aa`")
report("w1014b-PB3-report.md", "SPLIT",
       "# PB3 group (PB3 NOT started)\n**worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_bbb-2` · **branch:** "
       "`worktree-wf_bbb-2` · **HEAD:** `2222222bb`")
report("w1012c-PB10-report.md", "DONE", "# PB10 first attempt\n**branch:** `worktree-wf_ccc-1`")
report("w1013c-PB10-report.md", "**Status:** DONE · PB10 partial", "# PB10 second attempt\n**branch:** `worktree-wf_ccc-2`")

CLASS = {"worktree-wf_aaa-1": "UNLANDED", "worktree-wf_bbb-2": "LANDED", "worktree-wf_ccc-1": "ABSENT",
         "worktree-wf_ccc-2": "MERGED"}
F_BIND, F_EMIT, F_G4, F_OO = ("src/X/Binder.cs", "src/X/Emitter.cs", "src/F/Grammar/Core.g4", "src/X/Oo/Table.cs")


def cl(file, *ids, harm=1):
    return {"file": file, "harm": harm, "files": [file], "notes": [{"id": i, "area": "", "harm": 1, "title": ""} for i in ids]}


CLUSTERS = [cl(F_BIND, "PB1", "PB2", "PB5"), cl(F_EMIT, "PB6", "PB7", "PB3"), cl(F_EMIT, "PB12"),
            cl(F_OO, "PB8"), cl(F_G4, "PB11"), cl("src/X/Misc.cs", "PB9"), cl("src/X/Other.cs", "PB10"),
            cl("src/X/Zero.cs", "PB13"), cl(F_BIND, "PB14")]
HALF = [cl("src/X/Half.cs", "PB4")]
UNLANDED = {"worktree-wf_aaa-1": ["PB1"], "worktree-wf_ddd-9": ["PB13"], "worktree-wf_eee-1": [],
            "worktree-wf_fff-3": ["PB4"]}  # a killed wave left WIP for the half note PB4

notes = pw.load_notes(WORK, {"wrong_answer": 8})
reports = pw.load_reports(REPORTS)
check("wrapped inventory_rows counted", len(notes["PB6"].rows), 10)
check("report header ids only", sorted(r.lead for r in reports if "PB9999" in r.notes), [])
r3 = next(r for r in reports if r.lead == "PB3")
check("report parse", (r3.status, r3.branch, r3.head, r3.not_started, r3.worktree),
      ("SPLIT", "worktree-wf_bbb-2", "2222222bb", True, r"E:\COBOL\.claude\worktrees\wf_bbb-2"))
check("status from **Status:** line", next(r for r in reports if r.slug == "w1013c").status, "DONE")

p = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100)
G = {tuple(g.notes): g for g in p["groups"]}
if "-v" in sys.argv:
    for g in p["groups"]:
        print(g.letter, g.after, g.kind, g.model, g.notes, g.file)
order = [g.notes for g in p["groups"]]

# 1. work on an unlanded branch with a report is not planned again; a branch naming no open note is surfaced
check("awaiting landing", p["awaiting_landing"], {"worktree-wf_aaa-1": ["PB1"]})
check("PB1 not planned", any("PB1" in g.notes for g in p["groups"]), False)
check("unlanded branch without note", p["unlanded_without_note"], ["worktree-wf_eee-1"])

# 2. finishers first: PB3 (split, landed predecessor) absorbs the notes of BOTH clusters on its file (the cap), then
#    the half PB4, then the branch-only PB13
FIN = ("PB3", "PB6", "PB7", "PB12")
check("finisher order", order[:3], [list(FIN), ["PB4"], ["PB13"]])
check("finisher pred names report and branch",
      all(s in G[FIN].pred for s in ("w1014b-PB3-report.md", "worktree-wf_bbb-2", "LANDED")), True)
check("half pred", "status: half" in G[("PB4",)].pred, True)
check("half pred names its unlanded branch", "worktree-wf_fff-3" in G[("PB4",)].pred, True)
check("half note planned once", sum("PB4" in g.notes for g in p["groups"]), 1)
check("branch-only pred", "worktree-wf_ddd-9" in G[("PB13",)].pred, True)

# 3. clusters by rows claimed: PB2+PB5 (2+1) first, the 1-row ones next, PB14 (0 rows) last
check("cluster order", (order[3], order[-1]), (["PB2", "PB5"], ["PB14"]))
# 4. a second cluster on a file is that file's same-file successor
g14, g25 = G[("PB14",)], G[("PB2", "PB5")]
check("successor", (g14.after, g14.letter), (g25.letter, g25.letter + "2"))

# 5. routing
check("default sonnet", G[("PB2", "PB5")].model, "sonnet")
check("oo file -> opus", G[("PB8",)].model, "opus")
check(".g4 -> opus", G[("PB11",)].model, "opus")
check("open design question -> opus", G[("PB9",)].model, "opus")
check("failed twice -> opus", (G[("PB10",)].model, "2 times" in G[("PB10",)].why_model), ("opus", True))
check("never fable", {g.model for g in p["groups"]} <= set(RULES["routing"]["allowed"]), True)

# 6. budget fill: a tight budget takes the first finisher only; its successor's predecessor rule holds
one = pw.group_cost(G[("PB3", "PB6", "PB7", "PB12")].model, 4, RULES) + pw.landing_cost(RULES)
p2 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=one + 0.01)
check("tight budget: first group + its landing", [g.notes for g in p2["groups"]], [["PB3", "PB6", "PB7", "PB12"]])
check("points include the landing", round(p2["points"], 6), round(one, 6))
small = pw.group_cost("sonnet", 1, RULES) + pw.landing_cost(RULES)
p3 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=small + 0.01)
check("a group that does not fit is skipped, a smaller later one taken", [g.notes for g in p3["groups"]], [["PB4"]])
check("successor never without predecessor", any(g.after for g in p3["groups"]), False)
p4 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             max_groups=2)
check("max groups", len(p4["groups"]), 2)
check("no train: the plan carries no train count (kb/Work PB3023)", "trains" in p, False)

# 6b. A SPLIT REPORT ON AN UNLANDED BRANCH IS A FINISHER, NOT "AWAITING LANDING" (kb/Work PB2575): a `land` unit lands
#     only DONE branches, so holding it stranded the work (waves 1020 D and 1032 A). Its pred cherry-picks the branch.
#     A DONE report that names a note NOT started still waits for its landing. Own reports, so check 8's golden holds.
SPLIT_REPORTS = TMP / "reports-split"
SPLIT_REPORTS.mkdir()
# The notes a report answers are the notes its group was DISPATCHED with (PB3473): wave 1020's group E held PB7 and PB12.
SPLIT_COORD = TMP / "coord-split"
SPLIT_COORD.mkdir()
pw.record_dispatch(SPLIT_COORD, "1020", [{"letter": "e", "notes": ["PB7", "PB12"], "files": ["src/X/Emitter.cs"]}],
                   lambda i: False)
(SPLIT_REPORTS / "w1020d-PB5-report.md").write_text(
    "SPLIT\n# PB5 group\n**Status:** SPLIT at the turn cap · **worktree:** `E:\\COBOL\\.claude\\worktrees\\wf_ggg-4` · "
    "**branch:** `worktree-wf_ggg-4` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
(SPLIT_REPORTS / "w1020e-PB7-report.md").write_text(
    "DONE\n# PB7 group (PB12 NOT started)\n**branch:** `worktree-wf_hhh-5` · **HEAD:** `4444444dd`\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
returned(SPLIT_REPORTS)
SPLIT_CLASS = {"worktree-wf_ggg-4": "UNLANDED", "worktree-wf_hhh-5": "UNLANDED"}
p5 = pw.plan(notes, CLUSTERS, HALF, pw.load_reports(SPLIT_REPORTS, SPLIT_COORD), lambda b: SPLIT_CLASS.get(b, "ABSENT"), {}, RULES,
             budget_points=100)
g5 = next((g for g in p5["groups"] if "PB5" in g.notes), None)
check("split on an unlanded branch is a finisher", (g5.kind if g5 else None, "PB5" in str(p5["awaiting_landing"])),
      ("finisher", False))
check("its pred cherry-picks the unlanded branch",
      bool(g5) and all(s in g5.pred for s in ("worktree-wf_ggg-4", "UNLANDED", "cherry-pick")), True)
check("done with a note not started still awaits landing", p5["awaiting_landing"].get("worktree-wf_hhh-5"),
      ["PB7", "PB12"])
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
rv_reports = pw.load_reports(RV_REPORTS)
check("the review stamps parse", [(r.review.verdict, r.review.findings) if r.review else None for r in rv_reports],
      [("FINDINGS", 2), ("CLEAN", 0)])
p5r = pw.plan(notes, CLUSTERS, HALF, rv_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
g5r = next((g for g in p5r["groups"] if "PB5" in g.notes), None)
check("a FINDINGS stamp is a finisher whose pred names the findings file and the answer rule",
      (g5r.kind if g5r else None, bool(g5r) and all(s in g5r.pred for s in ("review-w1062-e.jsonl", "no_change_needed",
                                                                              "merge_drivers.py merge worktree-wf_rv-1"))),
      ("finisher", True))
check("a CLEAN stamp awaits landing", p5r["awaiting_landing"].get("worktree-wf_rv-2"), ["PB7"])
check("a placeholder Review line stamps nothing", pw.parse_review("**Review:** stamped by the branch reviewer"), None)
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
rv2 = {r.slug: r for r in pw.load_reports(RV_REPORTS)}
check("an O11 header's Branch field wins over the review stamp's reviewer label above it",
      (rv2["w1062g"].branch, rv2["w1062g"].worktree, rv2["w1062g"].head),
      ("worktree-wf_rv-3", "E:\\COBOL\\.claude\\worktrees\\wf_rv-3", "8888888bb"))
check("a template header's **branch:** field wins over the label below it", rv2["w1062h"].branch, "worktree-wf_rv-4")
check("with no field, the first branch word OUTSIDE the stamp is the branch", rv2["w1062i"].branch, "worktree-wf_rv-5")
check("the stamp still parses beside the field", (rv2["w1062g"].review.verdict, rv2["w1062g"].review.findings),
      ("FINDINGS", 2))
# The field is read at a FIELD POSITION only (PB3077 review round 1, R1-1): header prose says "this branch: every …"
# in real reports (w1043x-PB2806, w1055z-PB2426), and an unanchored `branch:` rule took `every` as the branch — ABSENT,
# the same harm — in a header with no field or with that prose above the field.
(RV_REPORTS / "w1062j-PB5-report.md").write_text(
    "Status: DONE\n**Review:** FINDINGS 1 land-review-worktree-wf_rv-6-r1 bbbbbbb22 E:/s/r.jsonl\n"
    "# PB5 report — the work of branch worktree-wf_rv-6; on this branch: every file is LF\n\n## X\n", encoding="utf-8", newline="\n")
(RV_REPORTS / "w1062k-PB7-report.md").write_text(
    "Status: DONE\n# PB7 report — on this branch: the reader takes the field\nBranch: worktree-wf_rv-7\nHEAD: ccccccc33\n"
    "\n## X\n", encoding="utf-8", newline="\n")
rv3 = {r.slug: r for r in pw.load_reports(RV_REPORTS)}
check("mid-prose `branch:` is not a field: with no field the branch word wins, never the prose", rv3["w1062j"].branch,
      "worktree-wf_rv-6")
check("mid-prose `branch:` above the Branch field does not pre-empt it", rv3["w1062k"].branch, "worktree-wf_rv-7")

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
pw.record_dispatch(MN_COORD, "1136", [{"letter": "a", "notes": ["PB13"], "files": ["src/X/Zero.cs"]},
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
mn_reports = pw.load_reports(MN_REPORTS, MN_COORD)
check("a report answers its lead and its dispatch group's notes, not the ids its header mentions",
      {r.slug: r.notes for r in mn_reports}, {"w1136a": ["PB13"], "w1136b": ["PB14"]})
check("a report no ledger group knows answers its lead alone",
      {r.slug: r.notes for r in pw.load_reports(MN_REPORTS)}, {"w1136a": ["PB13"], "w1136b": ["PB14"]})
pmn = pw.plan(notes, CLUSTERS, HALF, mn_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
check("a mentioned note is not awaiting landing under the mentioning report's branch",
      pmn["awaiting_landing"], {"worktree-wf_mn-2": ["PB14"]})
check("every mentioned note is planned in a group of its own cluster",
      {i: any(i in g.notes for g in pmn["groups"]) for i in ("PB10", "PB9", "PB11")},
      {"PB10": True, "PB9": True, "PB11": True})
check("a FINDINGS report's finisher is planned under its own lead alone, not under a note its header mentions",
      sorted((g.kind, tuple(g.notes)) for g in pmn["groups"] if g.kind == "finisher" and {"PB13", "PB10", "PB9"} & set(g.notes)),
      [("finisher", ("PB13",))])
check("a mention is no attempt at the mentioned note (choose_model escalates on attempts)",
      (pw.attempts("PB10", mn_reports), pw.attempts("PB13", mn_reports)), (0, 1))
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
pw.record_dispatch(CL_COORD, "1137", [{"letter": "a", "notes": ["PB13"], "files": ["src/X/Zero.cs"]}], lambda i: False)
(CL_REPORTS / "w1137a-PB13-report.md").write_text(
    "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_cl-1` · **HEAD:** `8c8c8c8cc`\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
returned(CL_REPORTS)
cl_reports = pw.load_reports(CL_REPORTS, CL_COORD)
check("before the register is read a report answers its lead alone", [r.notes for r in cl_reports], [["PB13"]])


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


check("a branch closes the notes it flipped to a terminal status, not the ones it only touched",
      pw.closed_by_branch("worktree-wf_cl-1", _cl_git), ["PB10", "PB13"])
pw.add_closed_notes(cl_reports, lambda b: "UNLANDED", _cl_git, lambda a, b: False)
check("a finished report on an unlanded branch answers its lead, its group and what the branch closes",
      [r.notes for r in cl_reports], [["PB13", "PB10"]])
pcl = pw.plan(notes, CLUSTERS, HALF, cl_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
check("the note the branch closed is awaiting landing with it, never planned a second time",
      (pcl["awaiting_landing"], any("PB10" in g.notes or "PB13" in g.notes for g in pcl["groups"])),
      ({"worktree-wf_cl-1": ["PB13", "PB10"]}, False))
check("a note the branch only touched is still planned", any("PB9" in g.notes for g in pcl["groups"]), True)
cl_landed = pw.load_reports(CL_REPORTS, CL_COORD)
pw.add_closed_notes(cl_landed, lambda b: "LANDED", _cl_git, lambda a, b: False)
check("a landed branch adds nothing (its notes are closed on main)", [r.notes for r in cl_landed], [["PB13"]])
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
cl_split = pw.load_reports(CL_SPLIT, CL_COORD)
pw.add_closed_notes(cl_split, lambda b: "UNLANDED", _cl_git, lambda a, b: False)
check("a SPLIT report on an unlanded branch answers what the branch closes too", [r.notes for r in cl_split],
      [["PB13", "PB10"]])
pcs = pw.plan(notes, CLUSTERS, HALF, cl_split, lambda b: "UNLANDED", {}, RULES, budget_points=100)
check("the SPLIT finisher takes the closed note, which no cluster group plans a second time",
      sorted((g.kind, tuple(g.notes)) for g in pcs["groups"] if {"PB13", "PB10"} & set(g.notes)),
      [("finisher", ("PB10", "PB13"))])

CAP_RULES = json.loads(json.dumps(RULES))
CAP_RULES["wave"]["max_notes_per_group"] = 2
CAP_REPORTS = TMP / "reports-closes-cap"
CAP_REPORTS.mkdir()
CAP_COORD = TMP / "coord-closes-cap"
CAP_COORD.mkdir()
pw.record_dispatch(CAP_COORD, "1138", [{"letter": "a", "notes": ["PB13", "PB14"], "files": ["src/X/Zero.cs"]}],
                   lambda i: False)
(CAP_REPORTS / "w1138a-PB13-report.md").write_text(
    "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_cap-1` · **HEAD:** `9d9d9d9dd`\n"
    "**Review:** FINDINGS 1 w1138a-review-r1 9d9d9d9dd E:/s/review-cap-a.jsonl\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
returned(CAP_REPORTS)
cap_reports = pw.load_reports(CAP_REPORTS, CAP_COORD)
pw.add_closed_notes(cap_reports, lambda b: "UNLANDED",
                    _branch_git("worktree-wf_cap-1", {"PB13": "landed", "PB14": "landed", "PB10": "retired"}),
                    lambda a, b: False)
pcap = pw.plan(notes, CLUSTERS, HALF, cap_reports, lambda b: "UNLANDED", {}, CAP_RULES, budget_points=100)
check("a FINDINGS branch that closed more notes than the cap has ONE review finisher, not one per chunk",
      sorted((g.kind, tuple(g.notes)) for g in pcap["groups"] if {"PB13", "PB14", "PB10"} & set(g.notes)),
      [("finisher", ("PB10", "PB13", "PB14"))])
# 6b'''00 (PB3508). A NOTE THE PREDECESSOR CLOSES IS NEVER ITS SUCCESSOR'S. fix-pb3504 (the successor) merged
#         fix-pb3489 (the predecessor) at its reviewed head, so PB3489's flip to `landed` was in fix-pb3504's diff from
#         main too. Credited to fix-pb3504's newer DONE report, PB3489 read `awaiting landing` with it, and PB3489's
#         review finisher was never planned while the survey held fix-pb3504 for those findings: wave 1143 planned
#         nothing. Here the predecessor's FINDINGS report (PB13) keeps its finisher and the successor holds PB14 alone.
PR_REPORTS = TMP / "reports-closes-pred"
PR_REPORTS.mkdir()
PR_COORD = TMP / "coord-closes-pred"
PR_COORD.mkdir()
pw.record_dispatch(PR_COORD, "1141", [{"letter": "e", "notes": ["PB13"], "files": ["src/X/Zero.cs"]},
                                      {"letter": "g", "notes": ["PB14"], "files": ["src/X/Zero.cs"]}], lambda i: False)
(PR_REPORTS / "w1141e-PB13-report.md").write_text(
    "# PB13 report\n**Status:** DONE · **branch:** `worktree-wf_pr-1` · **HEAD:** `5e5e5e5ee`\n"
    "**Review:** FINDINGS 1 w1141e-review-r1 5e5e5e5ee E:/s/review-pr-e.jsonl\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
(PR_REPORTS / "w1141g-PB14-report.md").write_text(
    "# PB14 report\n**Status:** DONE · **branch:** `worktree-wf_pr-2` · **HEAD:** `6f6f6f6ff`\n\n## Reproduced?\n",
    encoding="utf-8", newline="\n")
returned(PR_REPORTS)
pr_reports = pw.load_reports(PR_REPORTS, PR_COORD)
_pr_pred = _branch_git("worktree-wf_pr-1", {"PB13": "landed"})
_pr_succ = _branch_git("worktree-wf_pr-2", {"PB13": "landed", "PB14": "landed"})


def _pr_git(*args, cwd=None):
    pred = _pr_pred(*args)
    return pred if pred.returncode == 0 and pred.stdout else _pr_succ(*args)


# the one ancestry fact: the predecessor's branch is contained in the successor's (the planner's graph, kb/Work PB3582)
pw.add_closed_notes(pr_reports, lambda b: "UNLANDED", _pr_git,
                    lambda a, b: (a, b) == ("worktree-wf_pr-1", "worktree-wf_pr-2"))
check("the successor's report answers what it closed itself, not what its contained predecessor closed",
      sorted((r.lead, tuple(r.notes)) for r in pr_reports), [("PB13", ("PB13",)), ("PB14", ("PB14",))])
ppr = pw.plan(notes, CLUSTERS, HALF, pr_reports, lambda b: "UNLANDED", {}, RULES, budget_points=100)
check("the predecessor's review finisher is planned and the successor awaits landing with its own note alone",
      (ppr["awaiting_landing"], sorted((g.kind, tuple(g.notes)) for g in ppr["groups"] if {"PB13", "PB14"} & set(g.notes))),
      ({"worktree-wf_pr-2": ["PB14"]}, [("finisher", ("PB13",))]))
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
pw.record_dispatch(DR_COORD, "1074", [{"letter": "a", "notes": ["PB5", "PB12"], "files": ["src/X/Binder.cs"]}],
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
dr_reports = pw.load_reports(DR, DR_COORD)


def _show(*args, cwd=None):
    if args[:2] == ("show", "owner-task:kb/Work/PB77.md"):
        return _Shown(0, '---\ntitle: "PB77 — an owner task"\nid: PB77\nkind: defect\nstatus: landed\narea: process\n'
                         "process_only: true\n---\n\n# PB77\n")
    return _Shown(128)


dr_notes = dict(notes)
dr_notes.update(pw.branch_lead_notes(dr_reports, dr_notes, {"wrong_answer": 8}, _show))
check("an unseen lead is read from its own branch, and only there", sorted(set(dr_notes) - set(notes)), ["PB77"])
dr_notes["PB79"] = pw.Note("PB79", "open", "process", "PB79 — carried", [], "", 0)
# unlanded_branch_notes is unfiltered (every id the subjects name); plan() keeps its leads (review R2-1)
DR_UNLANDED = {"owner-task": ["PB77"], "worktree-wf_dr-6": ["PB11"], "worktree-wf_dr-7": ["PB9"]}


def dr_plan(drain):
    return pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                   budget_points=100, drain=drain, carrier=DR_CARRIER.get)


pd = dr_plan(True)
check("drain: only the finishers of started work", sorted(sorted(g.notes) for g in pd["groups"]),
      [["PB6"], ["PB7"], ["PB77"], ["PB79"], ["PB9"]])
check("drain: every group is a finisher whose pred says DRAIN",
      all(g.kind == "finisher" and "DRAIN" in g.pred for g in pd["groups"]), True)
check("drain: a SPLIT branch another DONE branch merged waits, with its carrier named",
      {i: "worktree-wf_dr-2" in pd["waiting"].get(i, "") for i in ("PB5", "PB12", "PB11")},
      {"PB5": True, "PB12": True, "PB11": True})
check("drain: a SPLIT whose branch landed left only unstarted work", "never started" in pd["waiting"].get("PB10", ""), True)
check("drain: the clusters' notes wait, counted", ("PB2" in pd["drain_not_started"], "PB4" in pd["drain_not_started"]),
      (True, True))
check("an unseen lead's branch with a finisher is no judgment row", pd["unlanded_without_note"], [])
# kb/Work PB3311 hole 1: a FINDINGS report's branch with a live owner of its OWN (prune_worktrees.owned: a resumed
# branch whose tip moved past the reviewed head, a live agent's worktree, a dispatch out) is being finished: no second
# finisher, in either mode, for an open note (PB7, branch wf_dr-2) and an unseen lead (PB79, wf_dr-5) alike, with the
# reason named. The same plan with neither branch owned (nobody working on it: the stamp covers the tip) plans both.
for drain in (False, True):
    for owned, planned in (({"worktree-wf_dr-2", "worktree-wf_dr-5"}, False), (set(), True)):
        pf1 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                      budget_points=100, drain=drain, carrier=DR_CARRIER.get, owned=owned)
        check(f"{'drain' if drain else 'normal'}: a FINDINGS branch {'not ' if planned else ''}owned "
              f"{'plans' if planned else 'waits, no'} finisher (PB3311)",
              [(any(i in g.notes for g in pf1["groups"]), "IN FLIGHT" in pf1["waiting"].get(i, ""))
               for i in ("PB7", "PB79")], [(planned, not planned)] * 2)
        if not planned:
            check(f"{'drain' if drain else 'normal'}: the waiting reason names the branch and the answers in progress",
                  all(w in pf1["waiting"].get("PB7", "") for w in ("worktree-wf_dr-2", "being answered")), True)
    # a SPLIT report's branch IN FLIGHT is a different arm (kb/Work PB2974): untouched by this rule
    pf2 = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                  budget_points=100, drain=False, carrier=DR_CARRIER.get, owned={"worktree-wf_dr-3"})
    check("a SPLIT report's branch IN FLIGHT is the SPLIT arm's (PB2974), not this rule's",
          any("PB6" in g.notes for g in pf2["groups"]), True)
# kb/Work PB3394 (wave 1127's A, B and C): a FINDINGS stamp makes a finisher only when it COVERS the branch tip
# (`review_covers`: prune_worktrees.review_coverage over the survey in plan_wave.main, the survey's own rule; the
# end-to-end case on a real git history is prune_worktrees.py --self-test's findings-moved and findings-stamped). A stamp
# at an older head is WAITING FOR REVIEW, named in `waiting`, never a group, in either mode. Without a survey the
# report's own HEAD stands for the tip: a report rewritten after the review (HEAD past the stamp) is not covered.
SV = TMP / "reports-stale-stamp"
SV.mkdir()
for head, stamp in (("314d06415aa", "11f7b8ab0c0e"), ("11f7b8ab0c0e", "11f7b8ab0c0e")):
    (SV / "w1127a-PB7-report.md").write_text(
        f"Status: DONE\n**Review:** FINDINGS 1 w1127a-review-r1 {stamp} E:/s/review-w1127-a.jsonl\n"
        f"Branch: worktree-wf_dr-2\nHEAD: {head}\n\n## X\n", encoding="utf-8", newline="\n")
    for drain in (False, True):
        for covers in (None, lambda _r: False, lambda _r: True):
            covered = covers(None) if covers else head == stamp
            psv = pw.plan(dr_notes, CLUSTERS, HALF, pw.load_reports(SV), lambda b: DR_CLASS.get(b, "ABSENT"), {}, RULES,
                          budget_points=100, drain=drain, review_covers=covers)
            g7 = [g.pred for g in psv["groups"] if "PB7" in g.notes]
            check(f"PB3394: HEAD {head[:4]}, stamp {stamp[:4]}, {'drain' if drain else 'normal'}, review_covers "
                  f"{'default' if covers is None else covered}: {'a finisher' if covered else 'WAITING FOR REVIEW'}",
                  (bool(g7) and g7[0].startswith("FINISH-FIRST: the branch review of w1127a"),
                   psv["waiting"].get("PB7", "").startswith("WAITING FOR REVIEW")),
                  (covered, not covered))
# PB3311 review R1-1: the survey reads a FINDINGS branch IN FLIGHT ALSO when a carrier merely contains it
# (prune_worktrees.contained: it lands through that branch), and that says nothing about who answers ITS findings. The
# survey as plan_wave.main reads it, end to end: wf_dr-5 (PB79's FINDINGS branch, DECISION by lifecycle) is an ancestor
# of wf_dr-2 (PB7's branch, the carrier). With the carrier locked by a live agent (IN FLIGHT, owned) PB7 waits and the
# carried PB79 still gets its R1-2 finisher, its pred naming the carrier; with the carrier finished (WAITING TO LAND)
# both are planned. Before the fix the bare state made PB79 wait on an answer nobody was writing.
import prune_worktrees  # noqa: E402
_dr_tips = {"worktree-wf_dr-2": "2020202bb", "worktree-wf_dr-5": "6060606ff"}
_dr_rep = {r.lead: r for r in prune_worktrees.report_index(DR, lambda _i: False)}
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
    check(f"R1-1: a FINDINGS branch contained in a {carrier_state} carrier reads IN FLIGHT, is not owned, and gets "
          f"its finisher naming the carrier; the carrier's own note {'is planned' if pb7_planned else 'waits'}",
          (rows[1].state, rows[1].carrier, "worktree-wf_dr-5" in survey_owned, "PB79" in pr["waiting"],
           bool(g79) and "contained in worktree-wf_dr-2" in g79.pred,
           any("PB7" in g.notes for g in pr["groups"]), "being answered" in pr["waiting"].get("PB7", "")),
          ("IN FLIGHT", "worktree-wf_dr-2", False, False, True, pb7_planned, not pb7_planned))
pn = dr_plan(False)
check("every mode: an unseen lead's FINDINGS gets its finisher", any(g.notes == ["PB77"] for g in pn["groups"]), True)
# R1-2: PB79's branch is contained in worktree-wf_dr-2, PB7's DONE branch, whose report never answers PB79's findings:
# the carried FINDINGS branch still gets its finisher (the drop was silent: no group, no waiting entry, no judgment row),
# which merges PB79's own branch only and is told the carrier may land its commits first.
for mode, pm in (("every mode", pn), ("drain", pd)):
    g79 = next((g for g in pm["groups"] if g.notes == ["PB79"]), None)
    check(f"{mode}: a carried unseen-lead FINDINGS gets its finisher, its pred naming the carrier (R1-2)",
          bool(g79) and ("merge_drivers.py merge worktree-wf_dr-5" in g79.pred, "contained in worktree-wf_dr-2" in g79.pred,
                         "never worktree-wf_dr-2" in g79.pred, "review_findings.py show E:/s/r79.jsonl" in g79.pred),
          (True, True, True, True))
g5n = next((g for g in pn["groups"] if "PB5" in g.notes), None)
check("every mode: a carried SPLIT's finisher merges its carrier, never cherry-picks it",
      bool(g5n) and "merge_drivers.py merge worktree-wf_dr-2" in g5n.pred and "cherry-pick its unlanded" not in g5n.pred, True)
check("without --drain nothing waits on the drain", (pn["drain_not_started"], any("DRAIN" in g.pred for g in pn["groups"])),
      ([], False))
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
        ps = pw.plan(dr_notes, CLUSTERS, HALF, pw.load_reports(SU), lambda b: SU_CLASS.get(b, "ABSENT"),
                     {"owner-task": ["PB77"], "worktree-wf_f-1": ["PB77"]}, RULES, budget_points=100, drain=drain,
                     carrier=carried.get)
        g77 = [g for g in ps["groups"] if "PB77" in g.notes]
        mode = f"{want} successor{' (drain)' if drain else ''}"
        if want == "SPLIT":
            check(f"{mode}: the finisher resumes the successor's branch, never the stale FINDINGS report (R1-1)",
                  [("w1075a" in g.pred, "worktree-wf_f-1" in g.pred, "review-owner-task.jsonl" in g.pred) for g in g77],
                  [(True, True, False)])
            # R2-1: the superseded FINDINGS branch names the unseen lead, which a newer report covers: skipped, as an
            # open note's stale branch is, never a judgment row (unlanded_branch_notes once kept only open notes)
            check(f"{mode}: neither the successor's nor the superseded FINDINGS branch is a judgment row (R2-1)",
                  [b for b in ("worktree-wf_f-1", "owner-task") if b in ps["unlanded_without_note"]], [])
        elif want == "DONE":
            check(f"{mode}: an unreviewed successor awaits landing, no finisher (R1-1)",
                  (g77, ps["awaiting_landing"].get("worktree-wf_f-1")), ([], ["PB77"]))
        else:
            check(f"{mode}: the successor's own FINDINGS gets the next finisher (R1-1)",
                  [("review-wf_f-1.jsonl" in g.pred, "merge_drivers.py merge worktree-wf_f-1" in g.pred) for g in g77], [(True, True)])

# 6c. A NOTE AN UNREPORTED DISPATCH HOLDS IS NOT PLANNED AGAIN, IN EITHER LANE (kb/Work PB2806): the fix lane read no
#     ledger, and wave 1044 re-planned seven of the eight groups another session's recorded, unlaunched wave 1045 held.
#     A report written since the dispatch answers it (steps 1 and 2 govern), a terminal note is never held, and a
#     retired hand entry (kb/Work PB3186) is no dispatch, however fresh.
HD = TMP / "held"
HD_REPORTS = HD / "reports"
HD_REPORTS.mkdir(parents=True)
pw.record_dispatch(HD, "1045", [{"letter": "a", "notes": ["PB5", "PB2"], "files": ["src/X.cs"]},
                                {"letter": "b", "notes": ["PB8"], "files": ["src/Y.cs"]},
                                {"letter": "c", "notes": ["PB9"], "files": ["src/Z.cs"]}], lambda i: False)
HD_AT = {g["letter"]: g["at"] for g in coord.read_json(HD / pw.DISPATCH_LEDGER, {})["groups"]}
for name, nid, when in (("w1045b-PB8-report.md", "PB8", HD_AT["b"] + 60), ("w1031c-PB9-report.md", "PB9", HD_AT["c"] - 3600)):
    (HD_REPORTS / name).write_text(f"DONE\n# {nid} group\n**branch:** `worktree-wf_held-{nid}` · **HEAD:** `5555555ee`\n\n"
                                   "## Reproduced?\n", encoding="utf-8", newline="\n")
    __import__("os").utime(HD_REPORTS / name, (when, when))
LEDGER_DOC = coord.read_json(HD / pw.DISPATCH_LEDGER, {})
LEDGER_DOC["groups"].append({"wave": "hand", "letter": "", "notes": ["PB10"], "files": ["src/W.cs"], "hand": True,
                             "at": HD_AT["a"]})
coord.write_json(HD / pw.DISPATCH_LEDGER, LEDGER_DOC)
check("held: unanswered groups only, terminal notes and retired hand entries never",
      pw.held_by_dispatch(HD, pw.load_reports(HD_REPORTS), lambda i: i == "PB2"),
      {"PB5": "dispatch w1045a", "PB9": "dispatch w1045c"})
p6 = pw.plan(notes, CLUSTERS, HALF, reports, lambda b: CLASS.get(b, "ABSENT"), UNLANDED, RULES, budget_points=100,
             held={"PB5": "dispatch w1045a"})
check("a held note is planned in no group and waits naming its dispatch",
      (any("PB5" in g.notes for g in p6["groups"]), "w1045a" in p6["waiting"].get("PB5", "")), (False, True))
check("its cluster's other notes are still planned", any("PB2" in g.notes for g in p6["groups"]), True)
# R2-1 (kb/Work PB3150): an UNSEEN lead (PB77, process_only, its FINDINGS on owner-task) is held exactly as an open note
# is. Wave 1075 dispatched its review finisher, which has not reported: the next plan, normal or --drain, must not
# plan a second finisher on the same findings (the hold once read `open_ids`, so PB77 was planned again).
for drain in (False, True):
    ph = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
                 budget_points=100, drain=drain, carrier=DR_CARRIER.get, held={"PB77": "dispatch w1075a", "PB7": "dispatch w1075b"})
    mode = "drain" if drain else "normal"
    check(f"{mode}: a held unseen lead and a held open note both wait naming their dispatch, neither planned (R2-1)",
          [(any(i in g.notes for g in ph["groups"]), f"held by dispatch {w}" in ph["waiting"].get(i, ""))
           for i, w in (("PB77", "w1075a"), ("PB7", "w1075b"))], [(False, True), (False, True)])
    check(f"{mode}: the held unseen lead's branch is no judgment row (R2-1)", "owner-task" in ph["unlanded_without_note"],
          False)
# The frontier-model hold reads the leads too: an unseen lead whose body needs Mythos under the owner's approval waits.
fm_notes = dict(dr_notes)
fm_notes["PB77"] = dataclasses.replace(dr_notes["PB77"], body="This needs Mythos, under the owner's per-dispatch approval.")
pf = pw.plan(fm_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"), DR_UNLANDED, RULES,
             budget_points=100, carrier=DR_CARRIER.get)
check("an unseen lead that needs the owner's frontier-model approval waits (R2-1)",
      (any("PB77" in g.notes for g in pf["groups"]), "owner's approval" in pf["waiting"].get("PB77", "")), (False, True))
# unlanded_branch_notes is unfiltered: an id that is no lead (a landed note, a typo) leaves its branch a judgment row
pj = pw.plan(dr_notes, CLUSTERS, HALF, dr_reports, lambda b: DR_CLASS.get(b, "ABSENT"),
             {**DR_UNLANDED, "worktree-wf_dr-8": ["PB4242"]}, RULES, budget_points=100, carrier=DR_CARRIER.get)
check("a branch naming no lead is a judgment row, its non-lead ids never planned",
      ("worktree-wf_dr-8" in pj["unlanded_without_note"], any("PB4242" in g.notes for g in pj["groups"])), (True, False))
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
    check("unlanded_branch_notes names every id, an unseen lead's too (R2-1)",
          pw.unlanded_branch_notes({"main", "owner-task", "worktree-wf_x"}, _ub_graph),
          {"owner-task": ["PB77"], "worktree-wf_x": ["PB2", "PB11"]})
finally:
    prune_worktrees.main_names, prune_worktrees.classify = _saved

# 6d. A BRANCH'S WORK IS ONE UNIT (kb/Work PB2974). Wave 1053's SPLIT reports each covered one branch carrying several
#     notes; wave 1055's finishers were recorded in the dispatch ledger under only some of each branch's notes, and the
#     wave 1056 plan made a second finisher, over the SAME branch, for the notes the ledger did not name. A report's
#     notes now wait together while an unanswered dispatch holds any of them, through the real ledger and reader.
UN = TMP / "unit"
UN_REPORTS = UN / "reports"
UN_REPORTS.mkdir(parents=True)
UN_NOTES = {n: pw.Note(n, "open", "tooling", f"{n} — a defect in tooling", [], "", 8) for n in
            ("PB301", "PB302", "PB303", "PB304", "PB311", "PB312", "PB313")}
UN_CL = [cl("scripts/a.py", "PB301", "PB302", "PB303"), cl("scripts/b.py", "PB304", "PB311", "PB312", "PB313")]
pw.record_dispatch(UN, "1053", [{"letter": "b", "notes": ["PB301", "PB302", "PB303"], "files": ["scripts/a.py"]}],
                   lambda i: False)
(UN_REPORTS / "w1053b-PB301-report.md").write_text(
    "SPLIT\n# w1053b\n**branch:** `worktree-wf_un-1` · **HEAD:** `3333333cc`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
returned(UN_REPORTS)


def un_plan(held, unlanded=None, drain=False, reports_dir=UN_REPORTS):
    return pw.plan(UN_NOTES, UN_CL, [], pw.load_reports(reports_dir, UN), lambda b: "UNLANDED", unlanded or {}, RULES,
                   budget_points=100, held=held, drain=drain)


un_all = un_plan({})
check("PB2974: a SPLIT report's three notes are ONE finisher over its branch",
      [(g.kind, g.notes, g.continues) for g in un_all["groups"] if "PB301" in g.notes],
      [("finisher", ["PB301", "PB302", "PB303"], ["worktree-wf_un-1"])])
# wave 1055's finisher: the ledger names PB302 alone (a chunk of the report's notes), and it has not reported
pw.record_dispatch(UN, "1055", [{"letter": "a", "notes": ["PB302"], "files": ["scripts/a.py"]}], lambda i: False)
un_held = pw.held_by_dispatch(UN, pw.load_reports(UN_REPORTS, UN), lambda i: False)
check("PB2974: the ledger holds the one note its dispatch named", un_held, {"PB302": "dispatch w1055a"})
for drain in (False, True):
    mode = "drain" if drain else "normal"
    un_p = un_plan(un_held, drain=drain)
    check(f"PB2974 {mode}: no finisher for the report's other notes while one of them is held",
          [g.notes for g in un_p["groups"] if {"PB301", "PB302", "PB303"} & set(g.notes)], [])
    check(f"PB2974 {mode}: the held note names its dispatch, the others name the held note and its dispatch",
          ("held by dispatch w1055a" in un_p["waiting"]["PB302"],
           [("PB302 of branch worktree-wf_un-1 is held by dispatch w1055a" in un_p["waiting"][i]) for i in ("PB301", "PB303")]),
          (True, [True, True]))
# ... only a note the report owns holds it: another cluster's notes are planned as before
check("PB2974: notes of no held report are planned beside it",
      sorted(i for g in un_plan(un_held)["groups"] for i in g.notes), ["PB304", "PB311", "PB312", "PB313"])
# ... and the dispatch's own report releases the report's notes together (the finisher group takes all three again)
(UN_REPORTS / "w1055a-PB302-report.md").write_text(
    "SPLIT\n# w1055a\n**branch:** `worktree-wf_un-2` · **HEAD:** `4444444dd`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
returned(UN_REPORTS)
check("PB2974: once the dispatch has reported nothing is held", pw.held_by_dispatch(UN, pw.load_reports(UN_REPORTS, UN),
                                                                                lambda i: False), {})
# the UNLANDED-branch arm: a branch whose commit subjects name three notes is one unit as well, and past the cap too
UB = {"worktree-wf_ub-1": ["PB311", "PB312", "PB313"]}
ub_p = un_plan({"PB312": "dispatch w1055b"}, unlanded=UB, reports_dir=UN / "none")
check("PB2974: an unlanded branch with a held note plans no finisher for its other notes",
      ([g.notes for g in ub_p["groups"] if {"PB311", "PB313"} & set(g.notes)],
       [("PB312 of branch worktree-wf_ub-1 is held by dispatch w1055b" in ub_p["waiting"][i]) for i in ("PB311", "PB313")]),
      ([], [True, True]))
UB_MANY = {f"PB{n}": pw.Note(f"PB{n}", "open", "tooling", f"PB{n} — a defect in tooling", [], "", 8)
           for n in range(400, 400 + RULES["wave"]["max_notes_per_group"] + 2)}
ub_ids = sorted(UB_MANY, key=pw.work.id_order)
ub_many = pw.plan(UB_MANY, [cl("scripts/c.py", *ub_ids)], [], [], lambda b: "UNLANDED", {"worktree-wf_ub-2": ub_ids}, RULES,
                  budget_points=1000)
check("PB2974: an unlanded branch's notes past the cap stay ONE finisher over the branch, never a cluster beside it",
      [(g.kind, g.notes, g.continues) for g in ub_many["groups"]], [("finisher", ub_ids, ["worktree-wf_ub-2"])])
# THE BRANCH, NOT THE ARM, IS THE UNIT (branch review R1-1): a half note and an open note on one unlanded branch, and a
# SPLIT report beside unreported notes its branch carries, are ONE finisher over the branch, and one held note holds all
BU_NOTES = {n: pw.Note(n, "open", "tooling", f"{n} — a defect in tooling", [], "", 8)
            for n in ("PB501", "PB502", "PB601", "PB602", "PB605")}
BU_CL = [cl("scripts/d.py", "PB502")]
BU_HALF = [cl("scripts/d.py", "PB501")]
BU_X = {"worktree-wf_x-1": ["PB501", "PB502"]}


def bu_plan(held, unlanded, reports=(), clusters=BU_CL, half=BU_HALF, drain=False):
    return pw.plan(BU_NOTES, clusters, half, list(reports), lambda b: "UNLANDED", unlanded, RULES, budget_points=100,
                   held=held, drain=drain)


check("PB2974 (R1-1): a half note and an open note of one unlanded branch are ONE finisher over it",
      [(g.kind, g.notes, g.continues) for g in bu_plan({}, BU_X)["groups"]],
      [("finisher", ["PB501", "PB502"], ["worktree-wf_x-1"])])
bu_h = bu_plan({"PB502": "dispatch w1055a"}, BU_X)
check("PB2974 (R1-1): the half note waits with the branch's held note, naming its dispatch",
      ([g.notes for g in bu_h["groups"]], "PB502 of branch worktree-wf_x-1 is held by dispatch w1055a" in bu_h["waiting"]["PB501"]),
      ([], True))
check("PB2974 (R1-1): a half note of a branch no note of which is held is a finisher with the half record and the branch",
      [("FINISH-FIRST: PB501 is `status: half`" in g.pred, "worktree-wf_x-1" in g.pred) for g in bu_plan({}, BU_X)["groups"]],
      [(True, True)])
BU_REP = UN / "bu-reports"
BU_REP.mkdir()
(BU_REP / "w1056a-PB601-report.md").write_text(
    "SPLIT\n# w1056a\n**branch:** `worktree-wf_y-1` · **HEAD:** `5555555ee`\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
returned(BU_REP)
BU_Y = {"worktree-wf_y-1": ["PB601", "PB602", "PB605"]}
BU_CL2 = [cl("scripts/e.py", "PB601", "PB602", "PB605")]
bu_reps = pw.load_reports(BU_REP, UN)
bu_r = bu_plan({}, BU_Y, bu_reps, BU_CL2, [])
check("PB2974 (R1-1): a report's finisher takes the unreported notes its own branch carries, ONE finisher",
      [(g.kind, g.notes, g.continues, "PB602, PB605" in g.pred) for g in bu_r["groups"]],
      [("finisher", ["PB601", "PB602", "PB605"], ["worktree-wf_y-1"], True)])
bu_rh = bu_plan({"PB602": "dispatch w1055a"}, BU_Y, bu_reps, BU_CL2, [])
check("PB2974 (R1-1): ... and a held unreported note of that branch holds the report's note too",
      ([g.notes for g in bu_rh["groups"]], "PB602 of branch worktree-wf_y-1 is held by dispatch w1055a" in bu_rh["waiting"]["PB601"],
       "PB602 of branch worktree-wf_y-1 is held by dispatch w1055a" in bu_rh["waiting"]["PB605"]), ([], True, True))
bu_rd = bu_plan({}, BU_Y, bu_reps, BU_CL2, [], drain=True)
check("PB2974 (R1-1): the drain plans the report's finisher over the branch's notes as well",
      [g.notes for g in bu_rd["groups"]], [["PB601", "PB602", "PB605"]])

# 7. the group JSON renders through the real template and passes check_practices' same-file rule
gjson = [pw.as_group_json(g, "77", notes, "COBOLNET0001-COBOLNET0003") for g in p["groups"]]
CFG77 = {"wave": "77", "base": "abc", "scratch": str(TMP), "stop_file": str(coord.fleet_stop(TMP, "w77"))}
FILES = lambda g: sorted(mds.group_claim(g, lambda ids: pw.Claim()).files)  # noqa: E731  (a fixture note declares no files)
try:
    specs = [mds.render(CFG77, g, FILES(g)) for g in gjson]
    rendered = True
except (KeyError, IndexError) as e:
    specs, rendered = [], f"template placeholder missing: {e}"
check("renders through the template", rendered, True)
# 7b. THE STOP IS SCOPED (kb/Work PB2483): every spec names the owner's global stop and this fleet's own, and passes
#     check_practices' rendered-spec rule; a groups file without a fleet stop renders nothing
spec_file = TMP / "msg-w77-x.txt"
spec_file.write_text(specs[0] if specs else "", encoding="utf-8", newline="\n")
check("a rendered spec names the global stop", str(coord.global_stop()) in (specs or [""])[0], True)
check("a rendered spec names the fleet's own stop", str(coord.fleet_stop(TMP, "w77")) in (specs or [""])[0], True)
check("a rendered spec passes check_practices", [m for m in check_practices.check(spec_file, check_practices.SPEC)
                                                 if "report" not in m], [])
# 7c. THE ONE REPORTS DIRECTORY (kb/Work PB2980): the report path is coord.reports_dir() whatever the wave's scratch
#     (here TMP), and check_practices refuses a spec that writes its report under the scratch instead
report_line = next((l for l in (specs or [""])[0].splitlines() if l.startswith("Report:")), "")
check("a rendered spec's report is in coord.reports_dir(), not the wave's scratch",
      (str(coord.reports_dir()) in report_line, str(TMP) in report_line), (True, False))
check("check_practices accepts the rendered report path",
      bool(re.search(check_practices.RENDERED_REPORT, report_line)), True)
check("check_practices refuses a report under the wave's scratch",
      bool(re.search(check_practices.RENDERED_REPORT, f"Report: {TMP}\\reports\\w77a-PB1-report.md")), False)
try:
    mds.render({k: v for k, v in CFG77.items() if k != "stop_file"}, gjson[0], FILES(gjson[0]))
    check("no stop_file, no spec", "rendered", "KeyError")
except KeyError:
    check("no stop_file, no spec", True, True)
check("a fleet stop is STOP-<scope>", coord.fleet_stop(pathlib.Path("S"), "loop").name, "STOP-loop")
# 7d. A HOST-I-O GROUP RUNS CI'S LINUX LEGS AT ITS OWN GATE (kb/Work PB3381; MANDATORY-PRACTICES I8). Expected from the
#     rule: a file under src/Cobol.Net.Runtime/IO/ (either separator) makes a group host-I-O, a compiler or other runtime
#     file does not; the file set is the group's file_set list, else (absent or empty) what its notes declare; a
#     host-I-O spec says run the unit and conformance legs from WSL after the Windows gate, any other says the
#     targeted Linux half runs inside its own gate (the landing runs no Linux gate: kb/Work PB3612); check_practices refuses a spec whose paragraph is not its group's.
check("host_io: a file under src/Cobol.Net.Runtime/IO/ sets it, either separator",
      (mds.host_io(["src/Cobol.Net.Runtime/IO/IndexedConnector.cs"]), mds.host_io(["src\\Cobol.Net.Runtime\\IO\\Sharing\\X.cs"])),
      (True, True))
check("host_io: a compiler file, another runtime folder, a look-alike folder and no file do not set it",
      (mds.host_io(["src/Cobol.Net.Compiler/Binding/DataBinder.cs"]), mds.host_io(["src/Cobol.Net.Runtime/Values/CobolDec.cs"]),
       mds.host_io(["src/Cobol.Net.Runtime/IOX/A.cs"]), mds.host_io([])), (False, False, False, False))
asked = []
check("group_claim: the file_set list (with its member_set) when it has files, else the notes' declared set",
      (mds.group_claim({"notes": "PB1", "file_set": ["src/A.cs"], "member_set": {"src/A.cs": ["M"]}},
                       lambda ids: asked.append(ids) or pw.Claim.of({"src/N.cs"})).member_lists(),
       sorted(mds.group_claim({"notes": "PB1, PB2", "file_set": []},
                              lambda ids: asked.append(ids) or pw.Claim.of({"src/Cobol.Net.Runtime/IO/N.cs"})).files),
       sorted(mds.group_claim({"notes": "PB3"}, lambda ids: asked.append(ids) or pw.Claim()).files), asked),
      ({"src/A.cs": ["M"]}, ["src/Cobol.Net.Runtime/IO/N.cs"], [], [["PB1", "PB2"], ["PB3"]]))
IOG = dict(gjson[0], letter="IO", file_set=["src/Cobol.Net.Runtime/IO/IndexedConnector.cs"])
CMG = dict(gjson[0], letter="CM", file_set=["src/Cobol.Net.Compiler/Binding/DataBinder.cs"])
io_spec, cm_spec = (mds.render(CFG77, g, FILES(g)) for g in (IOG, CMG))
check("a host-I-O group's spec runs the Linux unit and conformance legs at its gate, blocking on the verdict",
      ("HOST-I-O GROUP" in io_spec, "bash scripts/linux-gate.sh --legs unit,conformance --nice" in io_spec,
       "=== LINUX GATE: " in io_spec, "skip it and report" in io_spec), (True, True, True, False))
check("any other group's spec says the targeted Linux half is inside its own gate (kb/Work PB3612)",
      ("HOST-I-O GROUP" in cm_spec, "report `LINUX GATE: inside your gate (the targeted Linux half)`" in cm_spec),
      (False, True))
io_file, cm_file = TMP / "msg-w77-io.txt", TMP / "msg-w77-cm.txt"
io_file.write_text(io_spec, encoding="utf-8", newline="\n")
cm_file.write_text(cm_spec, encoding="utf-8", newline="\n")
check("both rendered specs pass check_practices",
      [[m for m in check_practices.check(f, check_practices.SPEC) if "report" not in m] for f in (io_file, cm_file)], [[], []])
iofile = TMP / "io-groups.json"
iofile.write_text(__import__("json").dumps(dict(CFG77, groups=[IOG, CMG])), encoding="utf-8", newline="\n")
check("check_practices finds each spec's LINUX GATE paragraph its group's",
      check_practices.specs_with_wrong_linux_gate(iofile, [io_file, cm_file]), [])
swapped = TMP / "swapped"
swapped.mkdir()
(swapped / "msg-w77-io.txt").write_text(cm_spec, encoding="utf-8", newline="\n")
(swapped / "msg-w77-cm.txt").write_text(io_spec, encoding="utf-8", newline="\n")
check("check_practices refuses a host-I-O group's spec without the host-I-O paragraph, and the reverse",
      check_practices.specs_with_wrong_linux_gate(iofile, [swapped / "msg-w77-io.txt", swapped / "msg-w77-cm.txt"]),
      ["msg-w77-io.txt (a host-I-O group)", "msg-w77-cm.txt (no host-I-O file)"])
try:
    coord.fleet_stop(TMP, "../x")
    check("a scope with a path in it is refused", "accepted", "ValueError")
except ValueError:
    check("a scope with a path in it is refused", True, True)
check("lead is the note with most rows", next(j["lead"] for j in gjson if j["letter"] == G[("PB3", "PB6", "PB7", "PB12")].letter), "PB6")
gfile = TMP / "groups.json"
gfile.write_text(__import__("json").dumps({"wave": "77", "scratch": "X", "base": "abc", "stop_file": CFG77["stop_file"],
                                          "groups": gjson}), encoding="utf-8", newline="\n")
check("check_practices O2 same-file rule", check_practices.same_file_without_successor(gfile), [])
MEMBER_GROUPS = TMP / "groups-members.json"


def member_groups(*member_sets):
    """A groups file of groups A, B, ... on one primary file, each claiming the members given (None: the whole file)."""
    gs = [{"letter": chr(65 + k), "root": "src/X/Binder.cs", **({"member_set": {"src/X/Binder.cs": ms}} if ms else {})}
          for k, ms in enumerate(member_sets)]
    MEMBER_GROUPS.write_text(__import__("json").dumps({"groups": gs}), encoding="utf-8", newline="\n")
    return check_practices.same_file_without_successor(MEMBER_GROUPS)


check("check_practices O2 is a shared-MEMBER rule (kb/Work PB3670): different members of one primary file pass; one "
      "member, or the whole file, refused without `after:`",
      (member_groups(["Binder.Bind"], ["Binder.Sync"]), member_groups(["Binder.Bind"], ["Bind"]),
       member_groups(["Binder.Bind"], None)),
      ([], ["A+B on src/X/Binder.cs"], ["A+B on src/X/Binder.cs"]))

# 8. THE FIX LANE WITHOUT --cluster IS UNCHANGED (kb/Work PB2120): the whole plan of this fixture, every group's
#    rendered JSON included, equals the golden that the planner wrote BEFORE the campaign lane existed (committed
#    alone in the PB2120 checkpoint before plan_wave.py changed). `--write-golden` rewrites it; only a deliberate
#    change to the fix lane does that, and says so in its commit.
GOLDEN = HERE / "testdata" / "plan_wave_default.json"


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
check("default plan equals the pre-campaign golden", fp_text == GOLDEN.read_text(encoding="utf-8"), True)

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


def cnote(nid, cluster=("PB901",), blocked_by=(), body="", status="open", kind="defect"):
    bb = ", ".join(blocked_by)
    (CW / f"{nid}.md").write_text(
        f'---\ntitle: "{nid} — campaign step"\nid: {nid}\nkind: {kind}\nstatus: {status}\narea: build/ci\n'
        f"wrong_answer: false\nprocess_only: true\nblocked: {'true' if blocked_by and status != 'landed' else 'false'}\n"
        f"blocked_by: [{bb}]\ncluster: [{', '.join(cluster)}]\n---\n\n# {nid}\n\n{body}\n", encoding="utf-8", newline="\n")


cnote("PB901", body="**Sites:**\n- `src/Lib/Alpha.cs`\n\nSever it from the old engine.")
cnote("PB902", body="**Code site.** scripts/gamma.py (its legacy legs)\n\nRewrite them.")
cnote("PB903", blocked_by=("PB901",), body="### Code site\n\n`src/Lib/Beta.cs:12-40`\n\n### Fix\n\nThen delete it.")
cnote("PB904", blocked_by=("PB901", "PB902"), body="- **Code site:** tests/Delta.Tests/DeltaTests.cs#Rename\n\nThen rename.")
# PB905 names no site in a site block: what its prose discusses, and its Gate: line, declare nothing (kb/Work PB3242)
cnote("PB905", blocked_by=("PB903",), body="Then sweep the docs; it discusses `src/Lib/Alpha.cs` and scripts/gamma.py.\n\n"
                                           "**Gate:** `python scripts/gamma.py --check`")
cnote("PB906", body="Author = Mythos 5.1, only after the owner's explicit\napproval for this dispatch.", kind="analysis")
cnote("PB907", cluster=("OTHER",), body="**Sites:**\n- `src/Lib/Alpha.cs`")
cnote("PB908", status="landed", body="**Sites:**\n- `src/Lib/Alpha.cs`")
cnote("PB909", status="owner", body="Waits for the owner.")
cnote("PB910", cluster=("PB901", "OTHER"), body="**Sites:**\n- `scripts/gamma.py#main` (main)")
citems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(CW.glob("*.md"))]
view = work.cluster_order(citems, "PB901")
check("cluster members: open, any kind, harm flags or not, in blocked_by order",
      [(r["id"], r["depth"], r["ready"]) for r in view["notes"]],
      [("PB901", 0, True), ("PB902", 0, True), ("PB906", 0, True), ("PB909", 0, False), ("PB910", 0, True), ("PB903", 1, False),
       ("PB904", 1, False), ("PB905", 2, False)])
check("a waiting note names what it waits on", {r["id"]: r["waiting_on"] for r in view["notes"] if not r["ready"]},
      {"PB909": ["status: owner"], "PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
check("named counts the landed member too", view["named"], 9)
check("a landed cluster has no open note", work.cluster_order(citems, "PB908")["notes"], [])

try:
    fc = pw.load_fix_clusters()
except SystemExit as e:  # the campaign clusters by fix_clusters.py's real index: a missing submodule is a FAIL, not a skip
    fc = None
    check("tools/claude-skills submodule present (git submodule update --init tools/claude-skills)", str(e), "")
if fc:
    cnotes = pw.load_notes(CW, {"wrong_answer": 8})
    ctexts = {i: (CW / f"{i}.md").read_text(encoding="utf-8") for i in cnotes}
    # THE declared set (declared_sets) on a policy that refuses to build an index: no note here names a member, so the
    # index is never read (a fix-lane or sites-only note declares its sites without one)
    CDECL = pw.declared_sets(TMP / "campaign", pw.IndexPolicy(TMP / "no-cache", regenerate=False))
    copen, chalf, cdeps, cwait = pw.campaign_clusters(view, cnotes, ctexts, RULES, fc, TMP / "campaign", CDECL)
    # PB904 waits on PB901 (Alpha.cs) and PB902 (gamma.py): their two clusters merge, so PB904 can follow ONE chain
    check("campaign clusters by declared sites, one depth at a time; a dependent's blocker clusters merge",
          [(c["depth"], c["file"], [n["id"] for n in c["notes"]]) for c in copen],
          [(0, "scripts/gamma.py", ["PB902", "PB910", "PB901"]), (0, "", ["PB906"]),
           (1, "src/Lib/Beta.cs", ["PB903"]), (1, "tests/Delta.Tests/DeltaTests.cs", ["PB904"]), (2, "", ["PB905"])])
    check("the merged cluster orients on both files", copen[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])
    check("deps are the open in-cluster blockers", cdeps, {"PB903": ["PB901"], "PB904": ["PB901", "PB902"], "PB905": ["PB903"]})
    check("held by the owner: waiting before planning", cwait, {"PB909": "waiting on status: owner"})
    cp = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=cdeps)
    CG = {tuple(g.notes): g for g in cp["groups"]}
    check("campaign groups and their successor chain",
          [(g.letter, g.after, g.notes, g.blocked_by) for g in cp["groups"]],
          [("A", "", ["PB902", "PB910", "PB901"], []), ("A2", "A", ["PB903"], ["PB901"]),
           ("A3", "A2", ["PB904"], ["PB901", "PB902"]), ("A4", "A3", ["PB905"], ["PB903"])])
    check("a frontier-model note waits for the owner", sorted(cp["waiting"]), ["PB906"])
    check("the frontier-model reason", "approval" in cp["waiting"].get("PB906", ""), True)
    check("the dependency is in the rendered body",
          ("PB903",) in CG and "DEPENDENCY SUCCESSOR" in pw.body_of(CG[("PB903",)], cnotes), True)
    check("a fix-lane body never says it", any("DEPENDENCY SUCCESSOR" in pw.body_of(g, notes) for g in p["groups"]), False)
    cg = [pw.as_group_json(g, "78", cnotes, "COBOLNET0001-COBOLNET0003") for g in cp["groups"]]
    cfile = TMP / "campaign-groups.json"
    cfile.write_text(JSON.dumps({"wave": "78", "scratch": "X", "base": "abc", "groups": cg}), encoding="utf-8", newline="\n")
    check("campaign groups pass check_practices' successor rule", check_practices.same_file_without_successor(cfile), [])
    for g in cg:
        mds.render(dict(CFG77, wave="78"), g, FILES(g))
    only_b = pw.group_cost("opus", 1, RULES) + pw.landing_cost(RULES)
    cp2 = pw.plan(cnotes, copen, chalf, [], lambda b: "ABSENT", {}, RULES, budget_points=only_b * 1.5, deps=cdeps)
    check("a dependent is never chosen without its blocker's group",
          all(g.after in {c.letter for c in cp2["groups"]} for g in cp2["groups"] if g.after), True)

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
        f"wrong_answer: {'true' if wrong else 'false'}\nprocess_only: true\nblocked: {'true' if blocked_by else 'false'}\n"
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
check("cluster_order: tier first (a blocker takes the lowest tier it blocks), then depth, untiered last",
      [(r["id"], r["tier"], r["depth"]) for r in tview["notes"]],
      [("PB943", 1, 0), ("PB944", 1, 0), ("PB948", 1, 0), ("PB947", 1, 1), ("PB945", 2, 0), ("PB941", None, 0),
       ("PB942", None, 0)])
check("the tiered register is well-formed", work.topology_problems(titems), [])
check("a tier is scoped to its campaign: the same notes in PB949 rank by that campaign's tiers",
      [(r["id"], r["tier"]) for r in work.cluster_order(titems, "PB949")["notes"]][:5],
      [("PB945", 7), ("PB943", 8), ("PB944", 8), ("PB948", 8), ("PB947", 8)])
check("lift_tiers: a cluster waiting on another lowers it, transitively; an unrelated cluster keeps its tier",
      (lambda cs: (pw.lift_tiers(cs, {"X2": ["Y1"], "Y1": ["Z1"]}), [c["tier"] for c in cs])[1])(
          [{"notes": [{"id": "X1"}, {"id": "X2"}], "tier": 1}, {"notes": [{"id": "Y1"}], "tier": None},
           {"notes": [{"id": "Z1"}], "tier": 3}, {"notes": [{"id": "W1"}], "tier": None}]),
      [1, 1, 1, None])
if fc:
    tnotes = pw.load_notes(TW, {"wrong_answer": 8})
    ttexts = {i: (TW / f"{i}.md").read_text(encoding="utf-8") for i in tnotes}
    TDECL = pw.declared_sets(TMP / "tiers", pw.IndexPolicy(TMP / "no-cache-tiers", regenerate=False))
    topen, thalf, tdeps, twait = pw.campaign_clusters(tview, tnotes, ttexts, RULES, fc, TMP / "tiers", TDECL)
    check("campaign clusters carry the tier", {tuple(n["id"] for n in c["notes"]): c["tier"] for c in topen},
          {("PB941",): None, ("PB942",): None, ("PB943",): 1, ("PB944",): 1, ("PB945",): 2, ("PB947",): 1,
           ("PB948",): 1})
    tp = pw.plan(tnotes, topen, thalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=tdeps)
    check("the plan letters tier 1 first, its successor after its blocker, then tier 2, then the untiered",
          [(g.letter, g.after, g.notes) for g in tp["groups"]],
          [("A", "", ["PB943"]), ("B", "", ["PB944"]), ("C", "", ["PB948"]), ("C2", "C", ["PB947"]),
           ("D", "", ["PB945"]), ("E", "", ["PB941"]), ("F", "", ["PB942"])])
    # THE DRIFT TEST: under a group cap, no group of a later tier is chosen while a READY group of an earlier one waits
    capped = pw.plan(tnotes, topen, thalf, [], lambda b: "ABSENT", {}, RULES, budget_points=100, deps=tdeps, max_groups=4)
    rank = {r["id"]: work.tier_rank(r["tier"]) for r in tview["notes"]}
    worst_chosen = max(rank[i] for g in capped["groups"] for i in g.notes)
    check("a group cap spends its slots on tier 1 before any untiered group, however much harm it carries",
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
            f"wrong_answer: false\nprocess_only: true\nblocked: {'true' if bb else 'false'}\nblocked_by: [{bb}]\n"
            f"cluster: [PB960]\n" + (f'tier: ["PB960:{tier}"]\n' if tier else "") +
            f"---\n\n# {nid}\n\n**Sites:**\n- `src/Lib/{site}.cs`\n", encoding="utf-8", newline="\n")
    litems = [dict(work.parse_frontmatter(p.read_text(encoding="utf-8")), _file=p.name) for p in sorted(LW.glob("*.md"))]
    lnotes = pw.load_notes(LW, {"wrong_answer": 8})
    ltexts = {i: (LW / f"{i}.md").read_text(encoding="utf-8") for i in lnotes}
    LRULES = JSON.loads(JSON.dumps(RULES))
    LRULES["wave"]["max_notes_per_group"] = 2
    lopen, lhalf, ldeps, _ = pw.campaign_clusters(work.cluster_order(litems, "PB960"), lnotes, ltexts, LRULES, fc,
                                                  TMP / "lift", pw.declared_sets(TMP / "lift", pw.IndexPolicy(
                                                      TMP / "no-cache-lift", regenerate=False)))
    check("campaign_clusters lifts the untiered blocker's cluster to its tier-1 dependent's",
          [([n["id"] for n in c["notes"]], c["tier"], c["depth"]) for c in lopen],
          [(["PB961", "PB962"], 1, 0), (["PB963"], 1, 0), (["PB964", "PB965"], 1, 1)])
    lp = pw.plan(lnotes, lopen, lhalf, [], lambda b: "ABSENT", {}, LRULES, budget_points=100, deps=ldeps)
    check("so the mixed dependent cluster follows the one Alpha.cs chain instead of waiting",
          ([(g.letter, g.after, g.notes) for g in lp["groups"]], lp["waiting"]),
          ([("A", "", ["PB961", "PB962"]), ("A2", "A", ["PB963"]), ("A3", "A2", ["PB964", "PB965"])], {}))

# 9a. A CAMPAIGN'S STANDING REQUIREMENTS (kb/Work PB3359): the bullets of the campaign lead note's `## Standing
#     requirements` reach every group's spec body, check_practices.py and the branch reviewer's prompt. The owner's
#     requirement lived only in a wave unit's hand edit of groups.json, and the reviewer never saw it.
REQ_NOTE = ("# PB950\n\nprose\n\n## Standing requirements\n\nintro line, no bullet\n\n"
            "- First requirement, with commas, and a 'quote'.\n"
            "- Second requirement wraps\n  over two lines.\n\n## Next section\n\n- not a requirement\n")
check("campaign_requirements: the bullets of the section, wrapped ones joined, the next heading ends it",
      pw.campaign_requirements(REQ_NOTE),
      ["First requirement, with commas, and a 'quote'.", "Second requirement wraps over two lines."])
check("campaign_requirements: CRLF reads the same", pw.campaign_requirements(REQ_NOTE.replace("\n", "\r\n")),
      pw.campaign_requirements(REQ_NOTE))
check("campaign_requirements: a note with no such section has none", pw.campaign_requirements("# PB1\n\n- a bullet\n"), [])
REQS = pw.campaign_requirements(REQ_NOTE)
# A group is bound by the campaigns its NOTES belong to (group_requirements), never by the planning mode (review R1-1):
# a fix-lane wave that plans a campaign member, or a campaign finisher, carries the requirement as a --cluster wave does.
RQDIR = TMP / "req-notes"
RQDIR.mkdir()
(RQDIR / "PB950.md").write_text(REQ_NOTE, encoding="utf-8", newline="\n")
(RQDIR / "PB951.md").write_text("# PB951\n\n## Standing requirements\n\n- Second campaign's own rule.\n", encoding="utf-8", newline="\n")
(RQDIR / "PB952.md").write_text("# PB952\n\nno standing requirements here\n", encoding="utf-8", newline="\n")
rg, rg_other = p["groups"][0], p["groups"][1]
member_of = {rg.notes[0]: ["PB950", "PB951"], rg_other.notes[0]: ["PB952"]}
bound = pw.group_requirements([rg, rg_other], member_of, RQDIR)
check("a group is bound by every campaign its notes belong to, one that holds the section", bound[0], {"PB950": REQS, "PB951": ["Second campaign's own rule."]})
check("a member of a campaign whose lead note has no section is bound by nothing", bound[1], {})
check("a note naming no campaign (no cluster, a scalar cluster, an unknown note) is bound by nothing",
      pw.group_requirements([rg], {rg.notes[0]: "PB950"}, RQDIR) + pw.group_requirements([rg], {}, RQDIR), [{}, {}])
check("a campaign lead naming no note file binds nothing", pw.group_requirements([rg], {rg.notes[0]: ["PB999"]}, RQDIR), [{}])
check("`always` (the campaign a --cluster wave runs) binds every group, before the notes' own, once",
      pw.group_requirements([rg, rg_other], member_of, RQDIR, ["PB950"]),
      [{"PB950": REQS, "PB951": ["Second campaign's own rule."]}, {"PB950": REQS}])
rj = pw.as_group_json(rg, "77", notes, "COBOLNET0001-COBOLNET0003", bound[0])
check("a requirement reaches the group's spec body, one OWNER REQUIREMENT sentence each, per campaign",
      (all(r in rj["body"] and "OWNER REQUIREMENT (campaign PB950" in rj["body"] for r in REQS),
       "OWNER REQUIREMENT (campaign PB951" in rj["body"], rj["body"].count("OWNER REQUIREMENT")), (True, True, 3))
check("the group entry carries its requirements for the practices check and the reviewer", rj["requirements"], bound[0])
check("a plan with no campaign requirements says none",
      ("OWNER REQUIREMENT" in pw.as_group_json(rg, "77", notes, "COBOLNET0001-COBOLNET0003")["body"],
       pw.as_group_json(rg, "77", notes, "COBOLNET0001-COBOLNET0003")["requirements"]), (False, {}))
rspec = TMP / "msg-w77-req.txt"
rspec.write_text(mds.render(CFG77, rj, FILES(rj)), encoding="utf-8", newline="\n")
rfile = TMP / "req-groups.json"
rfile.write_text(JSON.dumps(dict(CFG77, groups=[rj])), encoding="utf-8", newline="\n")
check("the rendered spec carries every requirement (check_practices.py finds none missing)",
      check_practices.specs_missing_requirements(rfile, [rspec]), [])
nospec = TMP / "msg-w77-req-without.txt"
nospec.write_text(mds.render(CFG77, gjson[0], FILES(gjson[0])), encoding="utf-8", newline="\n")
check("a spec rendered without them is refused, naming each requirement",
      check_practices.specs_missing_requirements(rfile, [nospec]),
      [(nospec.name, r) for r in REQS + ["Second campaign's own rule."]])
check("a groups file that declares none refuses nothing", check_practices.specs_missing_requirements(gfile, [nospec]), [])
# a spec named for another group is checked against THAT group's requirements only (make_dispatch_specs.py's name)
other_j = dict(pw.as_group_json(rg_other, "77", notes, "COBOLNET0004-COBOLNET0006"), letter="Z")
rj_named = dict(rj, letter="Y")
mfile = TMP / "req-groups-two.json"
mfile.write_text(JSON.dumps(dict(CFG77, groups=[rj_named, other_j])), encoding="utf-8", newline="\n")
yspec = TMP / "msg-w77-y.txt"
yspec.write_text(mds.render(CFG77, rj_named, FILES(rj_named)), encoding="utf-8", newline="\n")
zspec = TMP / "msg-w77-z.txt"
zspec.write_text(mds.render(CFG77, other_j, FILES(other_j)), encoding="utf-8", newline="\n")
check("each spec is checked against its own group's requirements, not every group's",
      check_practices.specs_missing_requirements(mfile, [yspec, zspec]), [])
# The owner's SORT and file I/O test sentence (2026-10-17 ~22:25) has ONE home, MANDATORY-PRACTICES I11, which every
# rendered spec and finisher brief points at (kb/Work PB3369). A campaign lead's `## Standing requirements` holds
# campaign-specific cases only: a copy of the sentence in a lead is a second home that drifts from the first.
OWNER_SENTENCE = "out test cases for sort and file i/o must cover"
I11_ROW = next(l for l in (coord.REPO / ".claude" / "skills" / "workstream" / "templates" / "MANDATORY-PRACTICES.md")
               .read_text(encoding="utf-8").splitlines() if l.startswith("| I11 |"))
check("MANDATORY-PRACTICES I11 carries the owner's SORT and file I/O sentence", OWNER_SENTENCE in " ".join(I11_ROW.lower().split()), True)
lead_copies = sorted(f.stem for f in (coord.REPO / "kb" / "Work").glob("PB*.md")
                     if any(OWNER_SENTENCE in " ".join(r.lower().split()) or "abnormal exception cases" in r
                            for r in pw.campaign_requirements(f.read_text(encoding="utf-8"))))
check("no campaign lead note's Standing requirements restates the owner's SORT and file I/O sentence (kb/Work PB3369)",
      lead_copies, [])
# the lead note the operator reads says what the planner does: binding by membership (review R2-1), never by the --cluster mode
real_reqs = pw.campaign_requirements((coord.REPO / "kb" / "Work" / "PB3330.md").read_text(encoding="utf-8"))
lead_text = (coord.REPO / "kb" / "Work" / "PB3330.md").read_text(encoding="utf-8")
check("the PB3330 note states the membership rule the planner implements, not the superseded --cluster-only rule",
      ("plan_wave.group_requirements" in lead_text, "Read by `plan_wave.py --cluster" in lead_text), (True, False))
check("the PB3330 campaign note still carries the keyed engine's own cases, so the binding test below is not vacuous",
      len(real_reqs) > 0, True)
# a fix-lane plan reaches them through the notes' cluster lists: PB3330's members are bound outside a --cluster wave
real_items = work.load()
member = next(i["id"] for i in real_items if isinstance(i.get("cluster"), list) and "PB3330" in i["cluster"] and i["id"] != "PB3330")
fg = pw.Group(kind="cluster", notes=[member], file="", files=[], letter="Q", model="sonnet", why_model="default")
check("a real PB3330 member planned by a fix-lane wave (no --cluster) is bound by the campaign's own requirements",
      pw.group_requirements([fg], {i.get("id"): i.get("cluster") for i in real_items}, coord.REPO / "kb" / "Work"),
      [{"PB3330": real_reqs}])
# the planner writes them per group into groups.json and the args file, and the rolling wave reads them into the
# REVIEWER's prompt and names the group's dispatch spec there: the halves are pinned in source, since the workflow has
# no process API to run here
PW_SRC = (HERE / "plan_wave.py").read_text(encoding="utf-8")
WF_SRC = (coord.REPO / ".claude" / "skills" / "workstream" / "templates" / "wf_rolling_wave.js").read_text(encoding="utf-8")
REVIEW_FN = WF_SRC[WF_SRC.index("function reviewBranch("):WF_SRC.index("function fixFindings(")]
check("plan_wave.py binds groups by their notes' campaigns, never by the planning mode",
      ("group_requirements(p[\"groups\"]" in PW_SRC, "campaign_requirements(lead_note" in PW_SRC), (True, False))
check("plan_wave.py carries each group's requirements into the args file",
      '"after", "requirements")' in PW_SRC, True)
check("wf_rolling_wave.js reads the group's requirements into the reviewer prompt",
      ("g.requirements" in WF_SRC, "args.requirements" in WF_SRC), (True, False))
check("the branch reviewer's prompt carries the requirements and names the group's dispatch spec",
      ("reviewerRequirements(g)" in REVIEW_FN, "msg-w${W}-${g.letter.toLowerCase()}.txt" in REVIEW_FN), (True, True))
check("the review finisher is pointed at the same dispatch spec (its body carries the requirements)",
      "msg-w${W}-${g.letter.toLowerCase()}.txt" in WF_SRC[WF_SRC.index("function fixFindings("):], True)

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
check("two chains wait", "different successor chains" in dp["waiting"].get("PB11", ""), True)
check("an unplanned blocker waits", "PB99 is not planned" in dp["waiting"].get("PB10", ""), True)
check("one blocker: its chain's successor", [(g.letter, g.after, g.notes) for g in dp["groups"]],
      [("A", "", ["PB5"]), ("B", "", ["PB9"]), ("A2", "A", ["PB13"])])
# a finisher never absorbs a dependent cluster on its file: the dependent's blocker is planned after the finisher,
# so absorbing it would hold the finisher itself back
dep12 = cl(F_EMIT, "PB12")
dep12["depth"] = 1
fp_ = pw.plan(notes, [cl(F_BIND, "PB5"), cl(F_EMIT, "PB3"), dep12], [], [r3], lambda b: "LANDED", {}, RULES,
              budget_points=100, deps={"PB12": ["PB5"]})
check("a finisher keeps its own notes and runs", [(g.kind, g.notes) for g in fp_["groups"]][:1], [("finisher", ["PB3"])])

# 9c. the file-set partition (owner, kb/Work PB2118 question 5, 2026-10-07): a campaign group whose files meet an
#     in-flight train's file set waits, with the collision named, and so does every group that follows it; a group
#     that meets nothing runs. Planted collision first: without `busy` the same groups all run.
free = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT", {}, RULES,
               budget_points=100, deps={"PB13": ["PB5"]})
check("with nothing in flight all three run", [g.notes for g in free["groups"]], [["PB5"], ["PB9"], ["PB13"]])
bp = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), after5], [], [], lambda b: "ABSENT", {}, RULES,
             budget_points=100, deps={"PB13": ["PB5"]},
             busy={"worktree-train-a": pw.InFlight({F_BIND, "src/Other.cs"}, "worktree-train-a")})
check("a group colliding with RUNNING work waits, the collision named",
      "file-set collision with in-flight work worktree-train-a [RUNNING] (" + F_BIND + ")" in bp["waiting"].get("PB5", ""),
      True)
check("its successor waits for it", "PB5 is not planned" in bp["waiting"].get("PB13", ""), True)
check("a group that meets nothing runs", [g.notes for g in bp["groups"]], [["PB9"]])
# PB2807: ONE rule for both lanes. The fix lane passes `busy` too (no deps): running work defers a group, a FINISHED
# branch (a DONE report, no review findings) makes it that branch's CONTINUATION (merge it first, no `after:`), and the
# group's own work never collides (the branch its pred continues; a dispatch of its own notes).
FIN = pw.Report(TMP / "w1090c-PB66-report.md", 1090, "w1090c", "PB66", "DONE", ["PB66"], False, "worktree-done",
                "E:/w/done", "abc1234")
fl = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9"), cl("src/X/Other.cs", "PB11")], [], [],
             lambda b: "ABSENT", {}, RULES, budget_points=100,
             busy={"worktree done (worktree-done)": pw.InFlight({F_BIND}, "worktree-done", finished=FIN),
                   "dispatched w1091a (PB9)": pw.InFlight({"src/X/Misc.cs"}, notes={"PB9"}),
                   "dispatched w1091b (PB40)": pw.InFlight({"src/X/Other.cs"}, notes={"PB40"})})
FG = {tuple(g.notes): g for g in fl["groups"]}
check("fix lane: a group meeting only a FINISHED branch is planned as its continuation, no after:",
      (("PB5",) in FG, FG.get(("PB5",)) and FG[("PB5",)].after, FG.get(("PB5",)) and FG[("PB5",)].continues),
      (True, "", ["worktree-done"]))
check("... its pred says merge that branch first and names the shared file",
      all(t in FG[("PB5",)].pred for t in ("CONTINUATION", "merge_drivers.py merge", "worktree-done", "w1090c", F_BIND)), True)
check("fix lane: a dispatch of the group's OWN notes never defers it (the hold already decided them)",
      ("PB9",) in FG, True)
check("fix lane: a group meeting another dispatch's RUNNING group waits, naming it",
      ("PB11" in fl["waiting"], "in-flight work dispatched w1091b" in fl["waiting"].get("PB11", "")), (True, True))
fin_own = pw.plan(notes, [cl(F_EMIT, "PB3")], [], [r3], lambda b: "UNLANDED", {}, RULES, budget_points=100,
                  busy={"worktree pred (" + str(r3.branch) + ")": pw.InFlight({F_EMIT}, r3.branch)})
check("a finisher never collides with the branch its own pred continues (its predecessor's worktree)",
      [(g.kind, g.notes) for g in fin_own["groups"]][:1], [("finisher", ["PB3"])])
CI_META = {"worktree a (worktree-done)": {"branch": "worktree-done", "rev": "x"},
           "worktree b (worktree-split)": {"branch": "worktree-split", "rev": "y"},
           "worktree c (worktree-findings)": {"branch": "worktree-findings", "rev": "z"},
           "worktree d (worktree-live)": {"branch": "worktree-live", "rev": "v"},
           "branch worktree-landed": {"branch": "worktree-landed", "rev": "worktree-landed"},
           "dispatched w1092a (PB67)": {"notes": ["PB67"], "at": 1.0, "slug": "w1092a"}}
SPL = dataclasses.replace(FIN, path=TMP / "w1090d-PB68-report.md", slug="w1090d", lead="PB68", status="SPLIT",
                          notes=["PB68"], branch="worktree-split")
FND = dataclasses.replace(FIN, path=TMP / "w1090e-PB69-report.md", slug="w1090e", lead="PB69", notes=["PB69"],
                          branch="worktree-findings", review=pw.Review("FINDINGS", 2, "r", "abc1234", "f.jsonl"))
# the survey's lifecycle states (prune_worktrees.lifecycle) decide; a branch the survey does not name is running
CI_STATES = {"worktree-done": "WAITING TO LAND", "worktree-split": "DECISION", "worktree-findings": "DECISION",
             "worktree-live": "IN FLIGHT", "worktree-landed": "LANDED"}
ci = pw.classify_inflight({o: {"src/A.cs"} for o in CI_META} | {"the open external-repository slices": {"src/S.cs"}},
                          CI_META, [FIN, SPL, FND], CI_STATES, {"worktree-live": ["PB70"]})
check("classify_inflight: WAITING TO LAND continues; DECISION, IN FLIGHT, a dispatch (answered ones never reach it: "
      "inflight_file_sets drops them, PB2795) and an unnamed owner run; LANDED is gone",
      {o: (x.finished.slug if x.finished else x.state) for o, x in ci.items()},
      {"worktree a (worktree-done)": "w1090c", "worktree b (worktree-split)": "DECISION",
       "worktree c (worktree-findings)": "DECISION", "worktree d (worktree-live)": "IN FLIGHT",
       "dispatched w1092a (PB67)": "DISPATCHED, NO REPORT YET", "the open external-repository slices": "RUNNING"})
check("classify_inflight: a branch's notes are its newest report's ids and its commit subjects' ids",
      (ci["worktree b (worktree-split)"].notes, ci["worktree d (worktree-live)"].notes), ({"PB68"}, {"PB70"}))
own = pw.plan(notes, [cl("src/X/Misc.cs", "PB9")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
              busy={"worktree e (worktree-old)": pw.InFlight({"src/X/Misc.cs"}, "worktree-old", {"PB9"}, state="DECISION")})
check("a branch working on the group's OWN note (an earlier attempt) never defers it",
      [g.notes for g in own["groups"]], [["PB9"]])
check("the one collision rule, path separators folded",
      pw.file_set_collisions(pw.Claim.of(["src" + chr(92) + "A.cs"]),
                             {"t": pw.Claim.of({"src/A.cs"}), "u": pw.Claim.of({"src/B.cs"})}), ["t (src/A.cs)"])

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
coord.write_json(LEDGER / pw.DISPATCH_LEDGER, {"groups": [
    {"wave": "1040", "letter": "a", "notes": ["PB70"], "files": ["src/Dispatched.cs"]},
    {"wave": "1039", "letter": "b", "notes": ["PB71"], "files": ["src/Done.cs"]}]})
fly = pw.inflight_file_sets(git=fake_git, repo=pathlib.Path("/r"), coord_dir=LEDGER,
                            terminal=lambda i: i == "PB71")
flat = {f for fs in fly.values() for f in fs}
check("a dispatched worktree with no commit is in flight (its uncommitted edit)", "src/Fresh.cs" in flat, True)
check("an unlanded branch that only edits existing files is in flight", "src/Existing.cs" in flat, True)
check("a dispatched group with no worktree yet is in flight", "src/Dispatched.cs" in flat, True)
check("a branch landed by content is not", "src/Landed.cs" in flat, False)
check("a dispatched group whose notes all landed is not", "src/Done.cs" in flat, False)
check("STATUS.md is never a product file", "STATUS.md" in flat, False)
check("the harness's settings file is in no owner's set, so a finisher continuing one worktree meets no other on it "
      "(kb/Work PB3465: wave 1134 held PB3337, PB3460 and PB1278 on it)",
      (SETTINGS_FILE in flat, pw.file_set_collisions(pw.Claim.of([SETTINGS_FILE, "src/Mine.cs"]),
                                                    {o: pw.Claim.of(fs) for o, fs in fly.items()})), (False, []))
check("coord.NEVER_WORK: the settings file, STATUS.md and everything under TestResults/ are never work; a path that "
      "merely contains one of them is",
      [coord.is_work(x) for x in (SETTINGS_FILE, SETTINGS_FILE.replace("/", chr(92)), "STATUS.md",
                                  "TestResults/a/b.trx", "src/STATUS.md", ".claude/settings.json", "docs/TestResults.md")],
      [False, False, False, False, True, True, True])
check("coord.porcelain_work keeps only the work lines of `git status --porcelain` (a rename by its new path)",
      coord.porcelain_work([" M " + SETTINGS_FILE, "?? STATUS.md", " M src/A.cs", "R  src/Old.cs -> STATUS.md",
                            "R  STATUS.md -> src/New.cs", ""]),
      [" M src/A.cs", "R  STATUS.md -> src/New.cs"])
check("the planner's own checkout is not in flight", any("main" in k for k in fly), False)
check("the collision names the dispatched owner",
      pw.file_set_collisions(pw.Claim.of(["src/Dispatched.cs"]), {o: pw.Claim.of(fs) for o, fs in fly.items()}),
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
            f"wrong_answer: false{NL}process_only: true{NL}blocked: false{NL}blocked_by: []{NL}cluster: [PB950]{NL}---"
            f"{NL}{NL}# {nid}{NL}{NL}{body}{NL}", encoding="utf-8", newline="\n")
    items2 = [dict(work.parse_frontmatter(q.read_text(encoding="utf-8")), _file=q.name) for q in sorted(W2.glob("*.md"))]
    view2 = work.cluster_order(items2, "PB950")
    notes2 = pw.load_notes(W2, {"wrong_answer": 8})
    texts2 = {i: (W2 / f"{i}.md").read_text(encoding="utf-8") for i in notes2}
    D2 = pw.declared_sets(C2, pw.IndexPolicy(TMP / "no-cache", regenerate=False))
    o2, _, _, _ = pw.campaign_clusters(view2, notes2, texts2, RULES, fc, C2,
                                       lambda text: D2(text) | (pw.Claim.of({"src/Lib/Caller.cs"}) if "PB951" in text
                                                                         else pw.Claim()))
    files2 = {n["id"]: c["files"] for c in o2 for n in c["notes"]}
    check("a bare type name in prose declares nothing (PB3242)", "src/Lib/GammaState.cs" in files2["PB950"], False)
    check("a note's computed callers are in its group's file set", "src/Lib/Caller.cs" in files2["PB951"], True)
    check("callers never steer the clustering (the primary file is a named site)",
          sorted(c["file"] for c in o2), ["src/Lib/Alpha.cs", "src/Lib/Beta.cs"])
    check("the collision check sees a caller", pw.file_set_collisions(pw.Claim.of(files2["PB951"]),
                                                    {"train t": pw.Claim.of({"src/Lib/Caller.cs"})}),
          ["train t (src/Lib/Caller.cs)"])
HL = TMP / "coord-hand"
HL.mkdir()
coord.write_json(HL / pw.DISPATCH_LEDGER, {"groups": [
    {"wave": "hand", "letter": "", "label": "cobol-clerk: x", "hand": True, "notes": ["PB961"], "files": ["src/Junk.cs"],
     "at": 1.0},
    {"wave": "1060", "letter": "A", "notes": ["PB962"], "files": ["src/Old.cs"], "at": 2.0}]})
check("a retired hand entry is no dispatch: not in flight, not held, no wave number",
      ({f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs},
       pw.held_by_dispatch(HL, [], lambda i: False), pw.next_wave(TMP / "no-devlog", [], HL)),
      ({"src/Old.cs"}, {"PB962": "dispatch w1060a"}, 1061))
first = pw.record_dispatch(HL, "1061", [{"letter": "A", "notes": ["PB960"], "files": ["src" + chr(92) + "Hand.cs"]},
                                        {"letter": "B", "notes": ["PB963"], "files": ["src/B.cs"]}], lambda i: False)
again = pw.record_dispatch(HL, "1061", [{"letter": "a", "notes": ["PB960"], "files": ["src/Hand.cs", "src/More.cs"]}],
                           lambda i: False)
LG = coord.read_json(HL / pw.DISPATCH_LEDGER, {})["groups"]
check("record_dispatch drops the retired hand entry and keeps the other waves", [(g["wave"], g["letter"]) for g in LG],
      [("1060", "A"), ("1061", "B"), ("1061", "a")])
check("record_dispatch UPSERTS on (wave, letter): the re-recorded group replaces its entry, its files exact, POSIX",
      [g["files"] for g in LG if g["wave"] == "1061" and g["letter"].upper() == "A"], [["src/Hand.cs", "src/More.cs"]])
check("each entry carries its report slug", [g["slug"] for g in first + again], ["w1061a", "w1061b", "w1061a"])
# PB3392: a re-rendered spec keeps its group's dispatch time (`at`, the order the landing check and `answers` read)
check("PB3392: the group recorded again keeps its first `at`; the group beside it keeps its own",
      (again[0]["at"] == first[0]["at"], first[0]["at"] < first[1]["at"]), (True, True))
HA = TMP / "coord-at"
HA.mkdir()
coord.write_json(HA / pw.DISPATCH_LEDGER, {"groups": [
    {"wave": "1118", "letter": "A5", "notes": ["PB3330"], "files": ["src/Engine.cs"], "slug": "w1118a5", "at": 5.0},
    {"wave": "1126", "letter": "C", "notes": ["PB1587"], "files": [], "slug": "w1126c", "at": 14.0}]})
rerender = pw.record_dispatch(HA, "1118", [{"letter": "A5", "notes": ["PB3330", "PB3331"], "files": ["src/Engine.cs", "src/More.cs"]},
                                           {"letter": "A7", "notes": ["PB3332"], "files": ["src/New.cs"]}], lambda i: False)
AT = {(g["wave"], g["letter"]): g for g in coord.read_json(HA / pw.DISPATCH_LEDGER, {})["groups"]}
check("PB3392: a re-rendered early dispatch is still EARLIER than a later wave's group, though its files and notes grew",
      (AT[("1118", "A5")]["at"], AT[("1118", "A5")]["files"], AT[("1118", "A5")]["notes"], AT[("1118", "A5")]["at"] < AT[("1126", "C")]["at"]),
      (5.0, ["src/Engine.cs", "src/More.cs"], ["PB3330", "PB3331"], True))
check("PB3392: a NEW group of an existing wave takes the time of its first recording; other waves' entries are untouched",
      (AT[("1118", "A7")]["at"] > 14.0, AT[("1126", "C")]["at"], [g["at"] for g in rerender] == [AT[("1118", "A5")]["at"], AT[("1118", "A7")]["at"]]),
      (True, 14.0, True))
different = pw.record_dispatch(HA, "1118", [{"letter": "A5", "notes": ["PB9999"], "files": ["src/Other.cs"]}], lambda i: False)
check("PB3392: a group that shares no note with the entry under its key is a DIFFERENT dispatch and takes this recording's time",
      different[0]["at"] > 14.0, True)
check("a recorded group is in flight before its agent edits a file, until its notes land",
      ({f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False).values() for f in fs}
       >= {"src/Hand.cs", "src/More.cs"},
       pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: True, with_git=False)), (True, {}))
HL_REPORTS = TMP / "reports-hl"
HL_REPORTS.mkdir()


def hl_report(name, status, when=None, sub=""):
    """A real report file (the answered rule reads its mtime), `when` seconds since the epoch or now."""
    path = HL_REPORTS / sub / name
    path.parent.mkdir(exist_ok=True)
    path.write_text(f"Status: {status}\n# {name}\n**branch:** `worktree-wf_hl`\n\n## X\n", encoding="utf-8", newline="\n")
    if when is not None:
        os.utime(path, (when, when))
    return pw.parse_report(path)


HL_AT = {g["letter"]: g["at"] for g in LG}
check("an ANSWERED group (its report exists, SPLIT residue included) holds no declared file, for every reader (PB2795)",
      {f for fs in pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: False, with_git=False,
                                         reports=[hl_report("w1061b-PB963-report.md", "SPLIT", HL_AT["B"] + 60)]).values()
       for f in fs} & {"src/B.cs", "src/Hand.cs"}, {"src/Hand.cs"})
# kb/Work PB3311 hole 2 (with PB3152's second-predicate witness): ONE predicate (`answers`) says "a dispatch returned"
# for the notes' hold AND the group's file set: the group's OWN report (its slug), written at or after the dispatch.
# A reviewer's stamp on ANOTHER dispatch's report names the same note and bumps its mtime past the dispatch, and a
# header that merely mentions the note says nothing about it: neither answers (the dispatch is the review finisher
# `w1101ar1` of PB3198, whose original report `w1101a` the branch reviewer re-stamped after the dispatch).
RV = TMP / "coord-review"
RV.mkdir()
pw.record_dispatch(RV, "1101", [{"letter": "AR1", "notes": ["PB3198"], "files": ["src/Review.cs"]}], lambda i: False)
RV_AT = coord.read_json(RV / pw.DISPATCH_LEDGER, {})["groups"][0]["at"]


def rv_state(*reports):
    held = pw.held_by_dispatch(RV, list(reports), lambda i: False)
    files = {f for fs in pw.inflight_file_sets(coord_dir=RV, terminal=lambda i: False, with_git=False,
                                               reports=list(reports)).values() for f in fs}
    return held, files


STAMPED = hl_report("w1101a-PB3198-report.md", "DONE", RV_AT + 600)           # the reviewed report, stamped after
MENTION = hl_report("w1102z-PB3203-report.md", "DONE", RV_AT + 600)           # its header names PB3198 below
EARLIER = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT - 3600, "earlier")      # an earlier dispatch's, reused slug
RETURNED = hl_report("w1101ar1-PB3198-report.md", "DONE", RV_AT + 900)        # the review finisher's own report
HELD = ({"PB3198": "dispatch w1101ar1"}, {"src/Review.cs"})
check("a reviewer's stamp on another dispatch's report, a header that mentions the note, and an earlier dispatch's "
      "report of the slug answer nothing: held and in flight by both readers (PB3311)",
      (rv_state(STAMPED), rv_state(MENTION), rv_state(EARLIER)), (HELD, HELD, HELD))
check("the group's own report written since the dispatch answers it for both readers (PB3311)",
      rv_state(STAMPED, RETURNED), ({}, set()))
check("a report whose file is gone answers nothing (the safe side of a double dispatch)",
      rv_state(dataclasses.replace(RETURNED, path=HL_REPORTS / "gone-PB3198-report.md")), HELD)
pw.record_dispatch(HL, "1062", [], lambda i: i == "PB962")
check("an entry whose notes all landed is dropped at the next record",
      [g["wave"] for g in coord.read_json(HL / pw.DISPATCH_LEDGER, {})["groups"]], ["1061", "1061"])
MDS = TMP / "coord-mds"
MDS.mkdir()
MCFG = {"wave": "1070", "groups": [{"letter": "A", "slug": "w1070a", "notes": "PB970, PB971", "file_set": ["src/M.cs"],
                                    "member_set": {"src/M.cs": ["T.M"]}},
                                   {"letter": "B", "slug": "w1070b", "notes": "PB972"}]}
mds.record(MCFG, MDS, terminal=lambda i: False, claim_of=lambda ids: pw.Claim.of({f"src/{i}.cs" for i in ids}))
check("make_dispatch_specs records every rendered group: its file_set list and member claims, else the set computed "
      "from its notes",
      [(g["slug"], g["notes"], g["files"], g["members"])
       for g in coord.read_json(MDS / pw.DISPATCH_LEDGER, {})["groups"]],
      [("w1070a", ["PB970", "PB971"], ["src/M.cs"], {"src/M.cs": ["T.M"]}), ("w1070b", ["PB972"], ["src/PB972.cs"], {})])
LM: dict = {}
check("... and the in-flight enumeration reads a recorded group's member claims back (kb/Work PB3670)",
      [(pw.inflight_file_sets(coord_dir=MDS, terminal=lambda i: False, with_git=False, meta=LM) and None),
       LM["dispatched w1070a (PB970, PB971)"]["members"]], [None, {"src/M.cs": ["T.M"]}])
check("the planner's groups file carries the file set as a LIST", gjson[0]["file_set"], sorted(p["groups"][0].files))
# 9f. (PB2118 Draft 9) the landing check orders two branches by dispatch time: the in-flight enumeration reports each
#     dispatch entry's notes and time through `meta`; and the planner never plans on a stale member index
META: dict = {}
pw.inflight_file_sets(coord_dir=HL, terminal=lambda i: i != "PB960", with_git=False, meta=META)
check("meta carries a dispatch entry's notes, time and slug",
      [(m["notes"], m["at"], m["slug"]) for m in META.values()], [(["PB960"], again[0]["at"], "w1061a")])
STALE = TMP / "stale-repo"
(STALE / "src").mkdir(parents=True)
(STALE / "src" / "A.cs").write_text("// a" + NL, encoding="utf-8", newline="\n")
ScratchRepo.init(STALE).commit("c")
try:
    pw.declared_sets(STALE, pw.IndexPolicy(TMP / "no-cache", regenerate=False))("**Moves or changes:** `N.T.M`")
    check("the planner refuses a tree no member index describes", "planned", "refused")
except SystemExit:
    check("the planner refuses a tree no member index describes", "refused", "refused")

# 9g. (PB2118 Draft 10, the ninth refuter) every changed-file read passes --no-renames (the fake git above refuses a
#     diff without it, so 9d already holds the enumeration to it); one wave's groups are recorded strictly apart in
#     letter order, so no two tie (N4); and an aborted dispatch is released by wave, group or note, never by a clock (N5)
RL = TMP / "release"
RL.mkdir()
pw.record_dispatch(RL, "1050", [{"letter": "a", "notes": ["PB80"], "files": ["src/X.cs"]},
                                {"letter": "b", "notes": ["PB81"], "files": ["src/Y.cs"]},
                                {"letter": "c", "notes": ["PB82"], "files": ["src/Z.cs"]}], terminal=lambda i: False)
ATS = [g["at"] for g in coord.read_json(RL / pw.DISPATCH_LEDGER, {})["groups"]]
check("one wave's groups never tie and keep letter order", ATS == sorted(ATS) and len(set(ATS)) == 3, True)
check("release by group", [g["letter"] for g in pw.release_dispatch(RL, "1050b")], ["b"])
check("release by note", [g["letter"] for g in pw.release_dispatch(RL, "PB82")], ["c"])
check("release by wave", [g["letter"] for g in pw.release_dispatch(RL, "1050")], ["a"])
check("nothing left to release", pw.release_dispatch(RL, "1050"), [])
check("changed_files passes --no-renames",
      pw.changed_files(lambda *a, **k: _R(NL.join(a) + NL) if "--no-renames" in a else _R(""), "x...y"),
      {"diff", "--name-only", "--no-renames", "x...y"})

# 10. work.py check holds blocked to blocked_by: a stale flag, an unknown blocker, a cycle
TOPO = [{"_file": "T1.md", "id": "T1", "status": "open", "blocked": True, "blocked_by": ["T9"], "cluster": []},
        {"_file": "T9.md", "id": "T9", "status": "landed", "blocked": False, "blocked_by": [], "cluster": []},
        {"_file": "T2.md", "id": "T2", "status": "open", "blocked": True, "blocked_by": ["nobody"], "cluster": []},
        {"_file": "T3.md", "id": "T3", "status": "open", "blocked": True, "blocked_by": ["T4"], "cluster": ["T3"]},
        {"_file": "T4.md", "id": "T4", "status": "open", "blocked": True, "blocked_by": ["T3"], "cluster": "T3"},
        {"_file": "T5.md", "id": "T5", "status": "open", "blocked": False, "blocked_by": ["T3"], "cluster": []},
        {"_file": "T6.md", "id": "T6", "status": "open", "blocked": True, "blocked_by": ["T7"], "cluster": ["C"]},
        {"_file": "T7.md", "id": "T7", "status": "open", "blocked": True, "blocked_by": ["T6"], "cluster": ["C"]}]
probs = work.topology_problems(TOPO)
check("stale blocked flag", any(s.startswith("T1.md: blocked is True") for s in probs), True)
check("unknown blocker", any(s.startswith("T2.md: blocked_by names 'nobody'") for s in probs), True)
check("cycle", any("cycle among open notes: T3 -> T4 -> T3" in s for s in probs), True)
check("cluster must be a list", any(s.startswith("T4.md: cluster must be a list") for s in probs), True)
check("an open blocker needs the flag", any(s.startswith("T5.md: blocked is False") for s in probs), True)
check("a second cycle", any("cycle among open notes: T6 -> T7 -> T6" in s for s in probs), True)
check("exactly those six", len(probs), 6)
check("a cycle never reads as ready", [(r["id"], r["depth"], r["ready"]) for r in work.cluster_order(TOPO, "C")["notes"]],
      [("T6", None, False), ("T7", None, False)])
check("a scalar cluster is no member (no substring match)", [r["id"] for r in work.cluster_order(TOPO, "T3")["notes"]],
      ["T3"])
# a tier (kb/Work PB3423) is a list of CAMPAIGN:N entries naming the note's own campaigns once; anything else is
# reported, and only the well-formed entries are read
TIERS = [{"_file": f"U{k}.md", "id": f"U{k}", "status": "open", "blocked": False, "blocked_by": [], "cluster": cl, "tier": t}
         for k, (t, cl) in enumerate([(["C:1"], ["C"]), (["C:12", "D:2"], ["C", "D"]), (["C:0"], ["C"]),
                                       (["C:x"], ["C"]), ("1", ["C"]), (["C:1", "C:2"], ["C"]), (["D:2"], ["C"]),
                                       (["C"], ["C"])])]
check("a malformed entry, a campaign named twice, or a campaign the note is not in, is a problem",
      sorted(s.split(":")[0] for s in work.topology_problems(TIERS)), ["U2.md", "U3.md", "U4.md", "U5.md", "U6.md", "U7.md"])
check("tier_of reads the well-formed entry for that campaign only",
      [work.tier_of(i, "C") for i in TIERS] + [work.tier_of(TIERS[1], "D"), work.tier_of(TIERS[6], "D")],
      [1, 12, None, None, None, 1, None, None, 2, 2])
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
check("work.sites_problems: an open note in a cluster from the cutover on with no site block is refused, prose paths "
      "and all; a block naming none is a declaration",
      [s.split(":")[0] for s in work.sites_problems(SITED)], [f"PB{NEW}.md", f"PB{NEW + 6}.md"])
check("... and the refusal names the rule and the planner's UNPARTITIONED plan",
      all(t in work.sites_problems(SITED)[0] for t in ("**Sites:**", "UNPARTITIONED", "PB3263")), True)
check("note_sites says whether a block is present, whatever it names",
      [work.note_sites(i["_body"]).declared for i in SITED[:3]], [False, True, True])

# 11. the slices' file set reads ALL of DESIGN-external-repository §12 (PB2118 Draft 7, the sixth refuter's H4): the
#     Callers column, every name in a span (`X.Y : Z`), a member mapped to the file that declares it, tests/ and docs/
#     paths; a parameter list and a one-word name are not sites. A synthetic repository first, then the refuter's
#     four plants against the real register, each of which must FAIL the register's slice rule.
SR = TMP / "slicerepo"
(SR / "docs" / "rearchitecture").mkdir(parents=True)
(SR / "src" / "B").mkdir(parents=True)
(SR / "tests" / "T").mkdir(parents=True)
(SR / "docs" / "rearchitecture" / "DESIGN-external-repository.md").write_text(NL.join([
    "## 12. Changes", "", "| Today | Change | Callers updated in the same change |", "|---|---|---|",
    "| `Session.Repo : GroupRepo` | `: Resolver` | `DriverX.Bind`, `CheckPairs` |",
    "| `Rec(Kind, Name)` | `+ Target` | `Target` |",
    "| `OldThing` | **landed (kb/Work PB1):** done | `LandedCaller` |",
    "| tests (`tests/T/HostTests.cs`) | rewritten | `docs/NOTES.md` |",
    "", "## 13. Next", ""]), encoding="utf-8", newline="\n")
for rel, text in {"src/B/Session.cs": "public sealed class Session { public GroupRepo Repo { get; } }",
                  "src/B/GroupRepo.cs": "internal sealed class GroupRepo { }",
                  "src/B/DriverX.cs": "public static class DriverX { public static void Bind() { } }",
                  "src/B/Checks.cs": NL.join(["static class Checks", "{", "    private static void CheckPairs(int a) { }", "}"]),
                  "src/B/Rec.cs": "public sealed record Rec(int K);",
                  "src/B/Kind.cs": "public enum Kind { A }",
                  "src/B/Target.cs": NL.join(["class Holder", "{", "    public int Target { get; set; }", "}"]),
                  "src/B/LandedCaller.cs": "class LandedCaller { }",
                  "tests/T/HostTests.cs": "public class HostTests { }",
                  "docs/NOTES.md": "notes"}.items():
    (SR / rel).write_text(text, encoding="utf-8", newline="\n")
# a build output and a stray file name the same type: the slice set reads the COMMITTED tree (PB1957, 2026-10-07)
(SR / "src" / "B" / "Generated").mkdir(parents=True)
(SR / "src" / "B" / "Generated" / "SessionParser.cs").write_text("class Session { }", encoding="utf-8", newline="\n")
ScratchRepo.init(SR).git("add", "-A", "--", ":!src/B/Generated")
(SR / "src" / "B" / "Stray.cs").write_text("class GroupRepo { }", encoding="utf-8", newline="\n")
um = []
sset = work.slice_file_set(SR, unmapped=um)
check("an untracked file declaring a slice name is not in the slice set", "src/B/Stray.cs" in sset, False)
check("a build output declaring a slice name is not in the slice set", "src/B/Generated/SessionParser.cs" in sset, False)
check("the Today column's first name", "src/B/Session.cs" in sset, True)
check("the second name of a `X.Y : Z` span", "src/B/GroupRepo.cs" in sset, True)
check("the Callers column (a type)", "src/B/DriverX.cs" in sset, True)
check("the Callers column (a member, mapped to its declaring file)", "src/B/Checks.cs" in sset, True)
check("a tests/ path and a docs/ path", {"tests/T/HostTests.cs", "docs/NOTES.md"} <= sset, True)
check("a parameter list names no site", "src/B/Kind.cs" in sset, False)
check("a one-word member name is prose, reported", ("src/B/Target.cs" in sset, any(u.startswith("Target") for u in um)),
      (False, True))
check("a landed row is history", "src/B/LandedCaller.cs" in sset, False)

REAL = work.load()
REAL_SET = work.slice_file_set()


def r3_plant(pid, site):
    body = (NL + "**Wave kind:** extract. **Model:** Opus." + NL + NL + "**Sites:**" + NL + "- `" + site + "`" + NL + NL
            + "**Blocked by:** PB2118, PB2417." + NL)
    return {"_file": pid + ".md", "id": pid, "title": pid + " — Extract: planted", "status": "open", "blocked": True,
            "blocked_by": ["PB2118", "PB2417"], "cluster": ["PB2119"], "_body": body}


for pid, site in (("PB9991", "src/Cobol.Net.Compiler/Binding/BindSession.cs"),
                  ("PB9992", "src/Cobol.Net.Compiler/Binding/GroupRepository.cs"),
                  ("PB9993", "src/Cobol.Net.Compiler/Oo/OoConformance.cs"),
                  ("PB9994", "tests/Cobol.Net.Tests.Conformance/NonCobolActivatorReturnTests.cs")):
    hit = [m for m in work.r3_program_problems(REAL + [r3_plant(pid, site)], REAL_SET) if m.startswith(pid)]
    check(f"a planted R3 note on {site.rsplit('/', 1)[-1]} must wait for an open slice", bool(hit), True)

# PB2197 (the R1 landing's CI red): the existence rule reads the COMMITTED tree, so a build output that exists in a built
# checkout and not in CI's never decides it; a note naming a generated parser file must name the grammar it comes from.
GEN = "src/Cobol.Net.Frontend/Generated/CobolParserCore.cs"
G4 = "src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4"
gen_only = [m for m in work.r3_program_problems(REAL + [r3_plant("PB9995", GEN)], REAL_SET) if m.startswith("PB9995")]
check("a planted R3 note naming only a generated file is refused (build output, no grammar)",
      any("build output" in m for m in gen_only), True)
check("a generated file is never reported missing, built checkout or not", any("does not exist" in m for m in gen_only), False)
with_g4 = r3_plant("PB9996", GEN)
with_g4["_body"] += "- `" + G4 + "`" + NL
check("a planted R3 note naming the generated file AND its grammar passes the build-output rule",
      any("build output" in m for m in work.r3_program_problems(REAL + [with_g4], REAL_SET) if m.startswith("PB9996")), False)
check("an untracked path is missing even if a local file has that name",
      work.named_missing_paths("`src/Cobol.Net.Frontend/obj/project.assets.json`"), ["src/Cobol.Net.Frontend/obj/project.assets.json"])

# PB2562: a train label is a taken wave number, because a wave's first train is labelled with the wave number.
with tempfile.TemporaryDirectory() as td:
    tdir = pathlib.Path(td)
    (tdir / "DEVLOG").mkdir()                                    # one file per entry (kb/Work PB3025)
    (tdir / "DEVLOG" / "0002-train-1036.md").write_text("## Entry 2 — Train 1036 (wave 1032: D, E)" + NL, encoding="utf-8", newline="\n")
    (tdir / "DEVLOG" / "0001-wave-1034.md").write_text("## Entry 1 — wave 1034 dispatched" + NL, encoding="utf-8", newline="\n")
    check("next_wave counts a train label above every wave number", pw.next_wave(tdir, []), 1037)
    # PB2806: a wave recorded in the dispatch ledger with no report yet (another session's) is a taken number too
    pw.record_dispatch(tdir, "1045", [{"letter": "a", "notes": ["PB5"], "files": ["src/X.cs"]}], lambda i: False)
    check("next_wave counts a dispatched wave with no report", pw.next_wave(tdir, [], tdir), 1046)

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
           if (r := pw.parse_report(fdir / n))}
    check("Report.finished: DONE, or DISCHARGED with a HEAD past its base; never SPLIT, no status, head == base or none",
          {k: v[3] for k, v in got.items()},
          {"a": True, "b": True, "c": False, "d": False, "e": False, "f": False, "g": True})
    check("the template's base and HEAD fields are read", got["b"][1:3], ("3333333", "2222222"))
    check("a field-position head beats a head named in prose before it (w1080f's header)", got["g"][1], "5555555")
    check("an abbreviated head names its full base: one commit (same_commit)", pw.same_commit(full[:9], full), True)
    for letter, want in (("a", 0), ("b", 0), ("c", 1), ("d", 1), ("f", 1)):
        check(f"--finished w9f1{letter}: exit {want}", pw.main(["--finished", str(fdir / f"w9f1{letter}-PB1-report.md")]),
              want)
    check("--finished on a missing report: exit 2", pw.main(["--finished", str(fdir / "w9f1z-PB1-report.md")]), 2)
    (fdir / "notes.md").write_text("Status: DONE\n", encoding="utf-8", newline="\n")
    check("--finished on a name that is not a fix-lane report: exit 2", pw.main(["--finished", str(fdir / "notes.md")]), 2)
    check("status_line finds the bare word and its index; None without one",
          (pw.status_line(["# title", "DONE"]), pw.status_line(["# title", "Branch: b"])), ((1, "DONE"), None))

# PB3536: A GATE HANDED TO THE WAVE. A GATE-PENDING report names the run directory of the gate its agent left running at
# its turn cap; the report is FINISHED only when that gate's verdict file is GREEN for the report's HEAD and measured
# that commit's own tree (Report.gate_state, through run_gate_legs' one reader); a gate still running holds the note
# (no finisher re-runs it), and any other end plans a finisher.
with tempfile.TemporaryDirectory() as td:
    gdir = pathlib.Path(td)
    checkout = ScratchRepo(coord.REPO)   # read only: the commit and tree this checkout is at
    head, tree = checkout.git("rev-parse", "HEAD"), checkout.git("rev-parse", "HEAD^{tree}")
    ended = subprocess.Popen([sys.executable, "-c", "pass"])
    ended.wait()

    def gate_run(name, rec=None, pid=None):
        run = gdir / "runs" / name
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
            "g": gate_run("other-head", {**green, "head": "0" * 40})}
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
           if (r := pw.parse_report(gdir / n))}
    check("PB3536: a GATE-PENDING report is finished only when its gate run is GREEN for its HEAD and that commit's "
          "tree; RUNNING while the gate's process lives; ENDED on a red, a death, another tree or head, or no run named; "
          "a run named relative to the report's worktree is that worktree's",
          got, {"a": ("GATE-PENDING", "GREEN", True), "b": ("GATE-PENDING", "ENDED", False),
                "c": ("GATE-PENDING", "RUNNING", False), "d": ("GATE-PENDING", "ENDED", False),
                "e": ("GATE-PENDING", "ENDED", False), "f": ("GATE-PENDING", "ENDED", False),
                "g": ("GATE-PENDING", "ENDED", False), "h": ("GATE-PENDING", "ENDED", False),
                "i": ("GATE-PENDING", "GREEN", True)})
    check("PB3536: --finished answers a GATE-PENDING report from its gate run (GREEN 0, running 1, red 1)",
          [pw.main(["--finished", str(gdir / f"w9g1{x}-PB1-report.md")]) for x in "acb"], [0, 1, 1])
    # review R1-2 of wave 1152, end to end: a HARNESS worktree (a clean commit plus the harness's edit of its TRACKED
    # .claude/settings.local.json) whose gate's record carries the tree the gate's own Host.measured_tree measures
    # there: the report at that HEAD is GREEN, so the wave reviews it and no finisher re-runs the gate.
    import run_gate_legs  # noqa: PLC0415
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
    htree = run_gate_legs.Host(hw).measured_tree()
    hrun = hw / "TestResults" / "build-local" / "20261023T000000Z-abcdef"
    hrun.mkdir(parents=True)
    (hrun / "verdict.json").write_text(json.dumps({"verdict": "GREEN", "line": "=== BUILD-LOCAL GATE: GREEN — x ===",
                                                   "exit_code": 0, "head": hhead, "tree": htree, "tree_end": htree}),
                                       encoding="utf-8", newline="\n")
    (gdir / "w9g2a-PB1-report.md").write_text(
        f"Status: GATE-PENDING\nWorktree: {hw}\nBranch: b\nHEAD: {hhead[:12]}\n"
        "Gate run: TestResults/build-local/20261023T000000Z-abcdef\n\n## Gate\n", encoding="utf-8", newline="\n")
    hr = pw.parse_report(gdir / "w9g2a-PB1-report.md")
    check("PB3536 R1-2: a harness worktree's GREEN gate at its HEAD (the tracked settings file edited, nothing else) is "
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
    pg = pw.plan(notes, CLUSTERS, HALF, pw.load_reports(grep), lambda b: "UNLANDED", {}, RULES, budget_points=100)
    gp = {i: [g for g in pg["groups"] if i in g.notes] for i in ("PB5", "PB10", "PB11")}
    check("PB3536: the planner waits on a handed gate that still runs, gives a finisher to one that ended RED, and "
          "re-plans none whose gate is GREEN",
          ("still running" in pg["waiting"].get("PB5", ""), not gp["PB5"],
           bool(gp["PB10"]) and gp["PB10"][0].kind == "finisher", not gp["PB11"]),
          (True, True, True, True))

# 10. A NOTE'S DECLARED FILE SET IS ITS STRUCTURED SITES, READ BY ONE READER (kb/Work PB3242; DESIGN-architecture-review
#     §8.7). The planner once declared every path a note's prose mentioned (fix_clusters' scored reading), so the R2
#     and PB3160 campaigns planned zero groups: 57 of PB1754's 63 ready notes DISCUSS plan_wave.py and landing_check.py,
#     and every PB3160 slice's Gate: line names cite.py.
DR = TMP / "declared"
for rel in ("src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs", "scripts/gamma.py",
            "scripts/spec/cite.py", "src/Model/DataItem.cs"):
    (DR / rel).parent.mkdir(parents=True, exist_ok=True)
    (DR / rel).write_text("// fixture" + NL, encoding="utf-8", newline="\n")
DDECL = pw.declared_sets(DR, pw.IndexPolicy(TMP / "no-cache", regenerate=False))
sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
import member_index  # noqa: E402
GATE_ONLY = (f"**Sites:**{NL}- `scripts/gamma.py`{NL}{NL}Probe it with `python scripts/spec/cite.py --check 14.9 \"x\"`."
             f"{NL}{NL}**Gate:** `python scripts/spec/cite.py --check 14.9 \"x\"` and `pwsh scripts/build-local.ps1`{NL}")
check("(1) a note whose only mention of cite.py is a Gate: line and prose declares no cite.py",
      ("scripts/spec/cite.py" in DDECL(GATE_ONLY).files, sorted(DDECL(GATE_ONLY).files)), (False, ["scripts/gamma.py"]))
EVERY_SHAPE = (f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}- `src/Lib/Beta.cs#Run` (Run){NL}"
               f"- `tests/Delta.Tests/DeltaTests.cs:12-40`{NL}- scripts/gamma.py (main){NL}"
               f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}{NL}"
               f"**Fix.** It also touches `src/Model/DataItem.cs` in prose.{NL}")
SITES = ("scripts/gamma.py", "src/Lib/Alpha.cs", "src/Lib/Beta.cs", "tests/Delta.Tests/DeltaTests.cs")
check("(2) a Sites path, a file#member, a file:line and a scripts/ path each declare their file; a census member id's "
      "parameter type (PB2423) and a path in prose declare nothing", work.note_sites(EVERY_SHAPE).paths, SITES)
check("(2) the census member id is a name, its return type dropped",
      work.note_sites(EVERY_SHAPE).names, ("N.OoBinder.Err(N.Model.DataItem)",))
for shape, text in (("a bold paragraph label", f"**Code site.** src/Lib/Alpha.cs#Run (the arm){NL}{NL}**Fix.** x"),
                    ("a heading section", f"## Code sites{NL}{NL}`src/Lib/Alpha.cs:7`, and more:{NL}{NL}"
                                          f"### Detail{NL}`src/Lib/Beta.cs`{NL}## Fix{NL}`scripts/gamma.py`"),
                    ("a list-item label", f"- **Code site:** src/Lib/Alpha.cs#Run{NL}- **Fix:** scripts/gamma.py"),
                    ("a label alone over a loose list", f"**Sites:**{NL}{NL}- `src/Lib/Alpha.cs`{NL}{NL}Prose: "
                                                        f"scripts/gamma.py")):
    want = ("src/Lib/Alpha.cs", "src/Lib/Beta.cs") if shape == "a heading section" else ("src/Lib/Alpha.cs",)
    check(f"(2) the site block as {shape} ends where the block ends", work.note_sites(text).paths, want)
check("(2) a placeholder, a glob and a bare root are no sites",
      work.note_sites(f"**Sites:**{NL}- `src/<Project>/X.cs`{NL}- `tests/**/char_*.txt`{NL}- `src/`{NL}").paths, ())
check("(2) the roots are model_rules.json's campaign roots, never a literal",
      work.campaign_domain().roots, tuple(RULES["campaign"]["src"]))
EMPTY_INDEX = {"commit": "c", "uses": {}, "types": {}, "declared": {}, "emitted": {}, "_simple": {}}
fs_mi = member_index.note_file_set(EVERY_SHAPE, DR, index=lambda: EMPTY_INDEX, drifted=lambda: {})
check("(3) the member index's declared set of a note with a name: its sites (the name unknown to this index)",
      (tuple(fs_mi["sites"]), tuple(fs_mi["files"]), fs_mi["unknown"]), (SITES, SITES, ["N.OoBinder.Err(N.Model.DataItem)"]))
SITES_ONLY = EVERY_SHAPE.replace(f"- `M:N.OoBinder.Err(N.Model.DataItem)~System.String @ src/.../OoBinder.cs:1783`{NL}", "")
check("(3) the planner, the member index and the register read the same sites for the same note",
      (tuple(sorted(DDECL(SITES_ONLY).files)), tuple(member_index.note_file_set(SITES_ONLY, DR)["sites"]),
       work.note_sites(SITES_ONLY).paths), (SITES, SITES, SITES))
if fc:
    CD = TMP / "declared-campaign" / "kb" / "Work"
    CD.mkdir(parents=True)
    (CD / "PB990.md").write_text(f"---{NL}id: PB990{NL}status: open{NL}cluster: [PB990]{NL}---{NL}{NL}{SITES_ONLY}",
                                 encoding="utf-8", newline="\n")
    citems = [dict(work.parse_frontmatter((CD / "PB990.md").read_text(encoding="utf-8")), _file="PB990.md")]
    cn = pw.load_notes(CD, {"wrong_answer": 8})
    o3, _, _, _ = pw.campaign_clusters(work.cluster_order(citems, "PB990"), cn,
                                       {"PB990": (CD / "PB990.md").read_text(encoding="utf-8")}, RULES, fc, DR, DDECL)
    check("(3) the planner's campaign group: its primary file is a declared site, its files exactly the declared set",
          (o3[0]["file"] in SITES, tuple(o3[0]["files"])), (True, SITES))
FL = TMP / "declared-fix" / "kb" / "Work"
FL.mkdir(parents=True)
(FL / "PB991.md").write_text(f"---{NL}id: PB991{NL}---{NL}{NL}{GATE_ONLY}", encoding="utf-8", newline="\n")
FLC = [{"file": "src/Lib/Alpha.cs", "files": ["src/Lib/Alpha.cs", "scripts/spec/cite.py"], "notes": [{"id": "PB991"}]}]
pw.declare_fix_lane(FLC, DDECL, FL)
check("a fix-lane cluster's collision set is its notes' declared sets plus its primary file, never fix_clusters' "
      "prose sites", FLC[0]["files"], ["scripts/gamma.py", "src/Lib/Alpha.cs"])
# Branch review R1-2: an R2 defect routed to the fix lane (its wave kind work.FIX_LANE_WAVE, with r2_members) is a
# fix-lane note: the fix lane's plan declares its sites without reading the member index, so it never regenerates
# one (DDECL's policy has no cache and refuses to regenerate: a read would raise StaleIndex).
(FL / "PB992.md").write_text(f"---{NL}id: PB992{NL}---{NL}{NL}r2_members: N.Beta.Run, N.Alpha.Go{NL}{NL}"
                             f"**Wave kind:** {work.FIX_LANE_WAVE}. **Model:** the fix lane's routing.{NL}{NL}"
                             f"**Sites:**{NL}- `src/Lib/Beta.cs:10-20` (Beta.Run){NL}", encoding="utf-8", newline="\n")
FLD = [{"file": "src/Lib/Beta.cs", "files": ["src/Lib/Beta.cs"], "notes": [{"id": "PB992"}]}]
try:
    pw.declare_fix_lane(FLD, DDECL, FL)
    check("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index", FLD[0]["files"],
          ["src/Lib/Beta.cs"])
except Exception as e:   # noqa: BLE001 — any read of the index (StaleIndex, a git call on a fixture) is the red
    check("R1-2: an R2 fix-lane defect with r2_members declares its sites and reads no index",
          f"read the index: {type(e).__name__}: {e}",
          ["src/Lib/Beta.cs"])
sys.path.insert(0, str(coord.REPO / "scripts" / "arch"))
import r2_collect  # noqa: E402
import file_census_notes  # noqa: E402
check("R1-2: the fix lane's wave kind is one of the R2 collector's wave kinds (one constant, work.FIX_LANE_WAVE)",
      work.FIX_LANE_WAVE in r2_collect.WAVES, True)
# Branch review R1-1: the line file_census_notes writes naming the files ONE STEP edits is a site block, so a step file
# outside the census finding's Sites (PB2300's IntrinsicCatalog.cs) is declared; the reader reads the writer's line.
STEP = f"**Sites:**{NL}- `src/Lib/Alpha.cs`{NL}{NL}{file_census_notes.step_files_line(['src/Lib/Alpha.cs', 'src/Lib/Beta.cs'])}{NL}"
check("R1-1: the step-files line file_census_notes writes declares its files (reader = planner)",
      (work.note_sites(STEP).paths, tuple(sorted(DDECL(STEP).files))), (("src/Lib/Alpha.cs", "src/Lib/Beta.cs"),) * 2)


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
check("(4) no module outside the reader scans a note's text for paths", note_path_scanners(TREE), [])
PLANT = "SITE_PATH = re.compile(r\"`((?" + ":src|tests)/[^`]+)\")"
check("(4) the scan fails on a planted copy of the old whole-text SITE_PATH and on a prose-scorer call",
      note_path_scanners({"scripts/arch/planted.py": PLANT, "scripts/orchestrator/p2.py": "fc" + ".sites(text, idx, exts)",
                          "scripts/orchestrator/fine.py": "x = 1"}),
      ["scripts/arch/planted.py", "scripts/orchestrator/p2.py"])

# 12. THE PARTITION MEETS ON MEMBERS, AND AN EMPTY SET HOLDS NOTHING (kb/Work PB3670, the owner's top priority,
#     2026-10-24, superseding PB3263's subsystem rule). Two claims meet on a file only when either holds it whole or a
#     member of one matches a member of the other (scripts/arch/source_units.py); a file a merge driver merges by key
#     never meets; an EMPTY set (no note declares a site block and no primary file was located, or a dispatch recorded
#     with no files) meets nothing, in a wave and in flight alike, and the plan names it UNPARTITIONED. Wave w20261032's
#     groups A (234 files) and B (222) held the owner-approved fuzz fixes PB3664 and PB3665 on files neither touched.
def unsited(*ids):
    return {"file": "", "harm": 1, "files": [], "notes": [{"id": i, "area": "", "harm": 1, "title": ""} for i in ids]}


def on_members(file, members, *ids):
    """A cluster on `file` claiming only `members` of it."""
    return {**cl(file, *ids), "members": {file: list(members)}}


C = pw.Claim.of
check("collision: a whole file meets anything on it; different members never meet; one member (qualified or bare) "
      "meets; an empty set meets nothing",
      (pw.collision(C({"a.cs"}), C({"a.cs", "b.cs"}, {"a.cs": ["T.M"]})),
       pw.collision(C({"a.cs"}, {"a.cs": ["T.M"]}), C({"a.cs"}, {"a.cs": ["T.N"]})),
       pw.collision(C({"a.cs"}, {"a.cs": ["M"]}), C({"a.cs"}, {"a.cs": ["T.M", "T.N"]})),
       pw.collision(C(set()), C({"a.cs"})), pw.collision(C({"a.cs"}), C(set()))),
      ("a.cs", None, "a.cs#M", None, None))
check("collision: a file a merge driver merges by key (the diagnostics catalogue, a work note) never meets",
      pw.collision(C({"conformance-suite/wiseowl/diagnostics.json", "kb/Work/PB9.md"}),
                   C({"conformance-suite/wiseowl/diagnostics.json", "kb/Work/PB9.md"})), None)
check("Claim union: a file stays whole when either side claims it whole; members unite otherwise",
      ((C({"a.cs"}, {"a.cs": ["M"]}) | C({"a.cs"}, {"a.cs": ["N"]})).member_lists(),
       (C({"a.cs"}, {"a.cs": ["M"]}) | C({"a.cs"})).member_lists()),
      ({"a.cs": ["M", "N"]}, {}))
check("collision_set: the primary file is whole unless every note declares a site block and one names it by member",
      (pw.collision_set("a.cs", [C({"a.cs"}, {"a.cs": ["M"]})]).member_lists(),
       pw.collision_set("a.cs", [C({"a.cs"}, {"a.cs": ["M"]}), pw.Claim(sited=False)]).member_lists(),
       pw.collision_set("a.cs", [C({"b.cs"}, {"b.cs": ["M"]})]).member_lists(),
       pw.collision_set("", [pw.Claim(sited=False)]).files),
      ({"a.cs": ["M"]}, {}, {"b.cs": ["M"]}, frozenset()))
mw = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB5"), on_members(F_BIND, ["Binder.Sync"], "PB9"),
                     on_members(F_BIND, ["Sync"], "PB10"), cl(F_BIND, "PB11")],
             [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
check("in a wave two groups on DIFFERENT MEMBERS of one primary file run side by side; one on the same member "
      "follows it; one on the whole file follows both, so it waits for a later wave (two chains)",
      ([(g.letter, g.after, g.notes) for g in mw["groups"]],
       "different successor chains" in mw["waiting"].get("PB11", "")),
      ([("A", "", ["PB5"]), ("B", "", ["PB9"]), ("B2", "B", ["PB10"])], True))
MREC = pw.partition_record("9", mw["groups"], {**mw["waiting"], "PB70": pw.COLLISION_WAIT + "worktree w [IN FLIGHT] "
                                                                         "(src/X/Binder.cs#Binder.Bind): it waits"},
                           1_800_000_000.0)
check("the partition's measurement records the same-file pairs run side by side and every hold with its reason",
      (MREC["groups"], MREC["same_file_parallel"], MREC["held_in_wave"], list(MREC["held_by_collision"])),
      (3, ["A+B on src/X/Binder.cs"], ["PB11"], ["PB70"]))
MCOORD = TMP / "coord-measure"
MCOORD.mkdir()
pw.record_partition(MCOORD, MREC)
check("... and --partition-summary reads it back per day, naming the colliding member",
      pw.partition_summary(MCOORD), ["2027-01-15: 1 waves, 3 groups, 1 same-file pairs side by side, 1 notes held by a "
                                     "collision (most named: src/X/Binder.cs#Binder.Bind x1)"])
uw = pw.plan(notes, [cl(F_BIND, "PB5"), unsited("PB9"), unsited("PB10"), unsited("PB8"), cl("src/X/Misc2.cs", "PB11")],
             [], [], lambda b: "ABSENT", {}, RULES, budget_points=100)
check("in a wave a group with an EMPTY set runs beside every other group: it holds nothing and nothing holds it",
      [(g.letter, g.after, g.notes) for g in uw["groups"]],
      [("A", "", ["PB5"]), ("B", "", ["PB9"]), ("C", "", ["PB10"]), ("D", "", ["PB8"]), ("E", "", ["PB11"])])
ub = pw.plan(notes, [unsited("PB9"), unsited("PB8"), cl("src/X/Misc2.cs", "PB11")], [], [], lambda b: "ABSENT", {},
             RULES, budget_points=100,
             busy={"dispatched w1117c (PB5)": pw.InFlight(set(), notes={"PB5"}, state="DISPATCHED, NO REPORT YET"),
                   "worktree done (worktree-done)": pw.InFlight({"src/Y/Engine.cs"}, "worktree-done", {"PB12"}, FIN,
                                                                "WAITING TO LAND")})
check("in flight a dispatch recorded with no files holds nothing, and an EMPTY group waits on nothing and continues "
      "nothing",
      ([(g.notes, g.continues) for g in ub["groups"]], ub["waiting"]),
      ([(["PB9"], []), (["PB8"], []), (["PB11"], [])], {}))
check("the spec body names an UNPARTITIONED group and the Sites paragraph (files and members) to add",
      all(t in pw.body_of(ub["groups"][0], notes) for t in ("UNPARTITIONED", "holds nothing", "**Sites:**", "members")),
      True)
check("a group with a file set is never named UNPARTITIONED", "UNPARTITIONED" in pw.body_of(uw["groups"][0], notes),
      False)
mb = pw.plan(notes, [on_members(F_BIND, ["Binder.Bind"], "PB9"), on_members(F_BIND, ["Binder.Sync"], "PB11")], [], [],
             lambda b: "ABSENT", {}, RULES, budget_points=100,
             busy={"worktree w (worktree-w)": pw.InFlight({F_BIND}, "worktree-w", {"PB70"}, state="IN FLIGHT",
                                                          members={F_BIND: frozenset({"Binder.Bind"})})})
check("in flight a group meeting a running branch on a MEMBER waits, the member named; one on another member of the "
      "same file runs",
      ([g.notes for g in mb["groups"]], f"({F_BIND}#Binder.Bind)" in mb["waiting"].get("PB9", "")), ([["PB11"]], True))
check("the spec names the members a group claims, so its implementer keeps its edits inside them",
      f"{F_BIND} (Binder.Sync)" in pw.body_of(mb["groups"][0], notes), True)
check("the groups file carries the member claims make_dispatch_specs.py records in the ledger",
      pw.as_group_json(mb["groups"][0], "9", notes, "X")["member_set"], {F_BIND: ["Binder.Sync"]})
ufin = pw.plan(notes, [unsited("PB3")], [], [r3], lambda b: "UNLANDED", {}, RULES, budget_points=100,
               busy={f"worktree pred ({r3.branch})": pw.InFlight({F_EMIT, "src/X/New.cs"}, r3.branch,
                                                                 members={F_EMIT: frozenset({"Emitter.Emit"})})})
check("a finisher's set is what the branch it continues changes, by the units its diff touches: planned and recorded "
      "with those files and members",
      [(g.kind, g.notes, g.files, g.members) for g in ufin["groups"]],
      [("finisher", ["PB3"], [F_EMIT, "src/X/New.cs"], {F_EMIT: ["Emitter.Emit"]})])
SLI = pw.classify_inflight({"the open external-repository slices": {"src/S.cs"}},
                           {"the open external-repository slices": {"notes": ["PB8"]}}, [], {})
check("an owner's meta notes (the slices' own) are its notes", SLI["the open external-repository slices"].notes, {"PB8"})
us = pw.plan(notes, [unsited("PB9"), unsited("PB11")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
             busy=SLI)
check("... and an EMPTY group never meets it", (sorted(i for g in us["groups"] for i in g.notes), sorted(us["waiting"])),
      (["PB11", "PB9"], []))
DM = pw.classify_inflight({"dispatched w1300a (PB70)": {F_BIND}},
                          {"dispatched w1300a (PB70)": {"notes": ["PB70"], "slug": "w1300a",
                                                        "members": {F_BIND: ["Binder.Bind"]}}}, [], {})
check("classify_inflight: a dispatch's recorded members are its claim", DM["dispatched w1300a (PB70)"].claim.member_lists(),
      {F_BIND: ["Binder.Bind"]})

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


UMETA: dict = {}
URAW = pw.inflight_file_sets(git=ugit, repo=urepo.path, coord_dir=UR, with_git=True, meta=UMETA)
UBUSY = pw.classify_inflight(URAW, UMETA, [], {})
UOWN = next(o for o in UBUSY if "worktree-units" in o)
pw.inflight_units(UBUSY, UMETA, [C({"src/Binder.cs", "src/New.cs"})], ugit, urepo.path)
check("inflight_units: a committed body edit and an uncommitted appended member are their units; an untracked file "
      "and a file no other claim holds are whole (unread)",
      (UBUSY[UOWN].files, UBUSY[UOWN].claim.member_lists()),
      ({"src/Binder.cs", "src/Solo.cs", "src/New.cs"}, {"src/Binder.cs": ["Binder.<own>", "Binder.Bind", "Binder.Sync",
                                                                          "Binder.Tail"]}))
check("... so a group claiming another member of the file meets it, and one claiming no touched member does not",
      (pw.collision(C({"src/Binder.cs"}, {"src/Binder.cs": ["Bind"]}), UBUSY[UOWN].claim),
       pw.collision(C({"src/Binder.cs"}, {"src/Binder.cs": ["Other"]}), UBUSY[UOWN].claim)),
      ("src/Binder.cs#Bind", None))

# kb/Work PB3456: THE SLICES' RESERVATION HOLDS ONLY THE RESTRUCTURING PROGRAM'S GROUPS (R69 §2, "R2 and every R3
# wave"), whichever campaign plans them, and reads OPEN (RESERVED) while nobody runs a slice. Wave 1132 (campaign
# PB3353) held PB1278 and the owner's tier-2 chain (PB3337, PB3338, PB3340) on docs/CONFORMANCE.md for four slices no
# worktree, branch or dispatch held, and printed them RUNNING.
check("restructuring: a PB1754 or PB2119 note is the program's, also when a fix campaign shares it; no other is",
      [work.restructuring({"cluster": c}) for c in (["PB1754"], ["PB2119"], ["PB2119", "PB3353"], ["PB3353"], [],
                                                    "PB2119", None)],
      [True, True, True, False, False, False, False])
SLICE_ITEMS = [{"id": "PB2100", "title": "PB2100 — external repository slice 4: open", "status": "open"},
               {"id": "PB2097", "title": "PB2097 — external repository slice 1: landed", "status": "landed"},
               {"id": "PB9", "status": "open", "cluster": ["PB2119", "PB3353"]},
               {"id": "PB11", "status": "open", "cluster": ["PB3353"]},
               {"id": "PB12", "status": "landed", "cluster": ["PB1754"]}]
check("open_slices: the open slice notes only, in id order", work.open_slices(SLICE_ITEMS), ["PB2100"])


def no_slice_set():
    raise AssertionError("the slice file set was read for a plan that reserves nothing")


RES = pw.slice_reservation(SLICE_ITEMS, "PB3353", lambda: {"docs/CONFORMANCE.md"})
check("slice_reservation: a campaign's plan reserves the set, naming the open slices and the open restructuring "
      "notes it holds (never a fix campaign's own, never a landed one)",
      RES, ({"docs/CONFORMANCE.md"}, {"notes": ["PB2100"], "reserves": ["PB9"]}))
check("slice_reservation: none (and the set unread) for the fix lane, with no open slice, or with no open "
      "restructuring note",
      (pw.slice_reservation(SLICE_ITEMS, None, no_slice_set),
       pw.slice_reservation([i for i in SLICE_ITEMS if i["id"] != "PB2100"], "PB3353", no_slice_set),
       pw.slice_reservation([i for i in SLICE_ITEMS if i["id"] != "PB9"], "PB3353", no_slice_set)),
      (None, None, None))
RSV = pw.classify_inflight({pw.SLICE_OWNER: RES[0]}, {pw.SLICE_OWNER: RES[1]}, [], {})
check("classify_inflight: a reservation nobody runs reads OPEN (RESERVED), never RUNNING, and keeps what it reserves",
      (RSV[pw.SLICE_OWNER].state, RSV[pw.SLICE_OWNER].reserves, RSV[pw.SLICE_OWNER].notes),
      ("OPEN (RESERVED)", {"PB9"}, {"PB2100"}))
RSV_RUN = pw.classify_inflight({pw.SLICE_OWNER: RES[0], "dispatched w1200a (PB2100)": {"src/S.cs"}},
                               {pw.SLICE_OWNER: RES[1],
                                "dispatched w1200a (PB2100)": {"notes": ["PB2100"], "slug": "w1200a"}}, [], {})
check("... and RUNNING, naming the work, once in-flight work holds one of its slices",
      RSV_RUN[pw.SLICE_OWNER].state, "RUNNING (dispatched w1200a (PB2100))")


def on_conformance(file, nid):
    return {**cl(file, nid), "files": [file, "docs/CONFORMANCE.md"]}


rp = pw.plan(notes, [on_conformance("src/X/Other.cs", "PB11"), on_conformance("src/X/Misc.cs", "PB9")], [], [],
             lambda b: "ABSENT", {}, RULES, budget_points=100, busy=RSV)
check("a fix campaign's group on a reserved file is planned; a restructuring note's group on it waits, the "
      "reservation named OPEN (RESERVED)",
      ([g.notes for g in rp["groups"]], sorted(rp["waiting"]),
       "the open external-repository slices [OPEN (RESERVED)] (docs/CONFORMANCE.md)" in rp["waiting"].get("PB9", "")),
      ([["PB11"]], ["PB9"], True))
UL = TMP / "coord-unknown"
UL.mkdir()
coord.write_json(UL / pw.DISPATCH_LEDGER, {"groups": [{"wave": "1117", "letter": "c", "notes": ["PB5"], "files": []}]})
UM: dict = {}
check("a dispatch recorded with no files stays in flight: its set empty (it meets nothing), its notes known",
      (pw.inflight_file_sets(coord_dir=UL, terminal=lambda i: False, with_git=False, meta=UM),
       UM.get("dispatched w1117c (PB5)", {}).get("notes")),
      ({"dispatched w1117c (PB5)": set()}, ["PB5"]))

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


def main3114(scratch, stop, specs):
    saved_plan, saved_specs = pw.plan, pw.MAKE_SPECS
    pw.plan = lambda *_a, **_k: {"groups": [pw.Group("cluster", ["PB3114"], "scripts/orchestrator/plan_wave.py",
                                                     ["scripts/orchestrator/plan_wave.py"], letter="A", model="opus")],
                                 "points": 0.1, "awaiting_landing": {}, "waiting": {}, "unlanded_without_note": [],
                                 "drain_not_started": [], "skipped": []}
    pw.MAKE_SPECS = specs
    try:
        return pw.main(["--wave", "9114", "--budget-points", "3", "--no-branches", "--coord", str(C3114),
                        "--reports", str(TMP / "reports-3114"), "--clusters-json", str(C3114 / "clusters-open.json"),
                        "--half-clusters-json", str(C3114 / "clusters-half.json"), "--scratch", str(scratch),
                        "--stop-file", str(stop)])
    finally:
        pw.plan, pw.MAKE_SPECS = saved_plan, saved_specs


def state3114():
    return (pw.alloc.peek("code", coord.REPO, C3114), pw.ledger_groups(C3114))


before = state3114()
S3114 = TMP / "scratch-3114"
for scratch, stop in (("E:COBOL-coordscratch", S3114 / "STOP-loop"), (S3114, "E:COBOL-coordscratchSTOP-loop"),
                      (S3114, S3114 / "STOP")):
    check(f"PB3114 (a): --scratch {scratch} --stop-file {stop} is refused first, nothing reserved, recorded or written",
          (main3114(scratch, stop, pw.MAKE_SPECS), state3114(), S3114.exists()), (2, before, False))
check("PB3114 (b): a RED check reserves no code, records nothing and writes no args file; its one render recorded nothing",
      (main3114(S3114, S3114 / "STOP-loop", FAKE_SPECS), state3114(), (S3114 / "wf-args-w9114.json").exists(),
       (S3114 / "fake-calls.txt").read_text(encoding="utf-8").split(chr(10))[0].split()[:1]),
      (1, before, False, ["--no-record"]))
rc3114 = main3114(S3114, S3114 / "STOP-loop", pw.MAKE_SPECS)
after_code, after_ledger = state3114()
spec3114 = (S3114 / "msg-w9114-a.txt").read_text(encoding="utf-8")
check("PB3114 (c): GREEN reserves the group's codes after the check, renders them and records the group",
      (rc3114, after_code - before[0], pw.alloc.render("code", before[0], 3) in spec3114,
       [(g["wave"], g["letter"], g["notes"]) for g in after_ledger], (S3114 / "wf-args-w9114.json").is_file()),
      (0, 3, True, [("9114", "A", ["PB3114"])], True))
pw.release_dispatch(C3114, "9114")
# a hand-planned groups file whose practices check is RED: a standing requirement its spec body does not carry (PB3359)
g3114 = json.loads((S3114 / "groups.json").read_text(encoding="utf-8"))["groups"][0]
gred = TMP / "groups-3114-red.json"
gred.write_text(json.dumps({"wave": "9115", "scratch": str(S3114), "base": "abc", "stop_file": str(S3114 / "STOP-x"),
                            "groups": [{**g3114, "letter": "B", "slug": "w9115b",
                                        "requirements": {"PB1": ["a requirement no rendered spec carries (3114)"]}}]}),
                encoding="utf-8", newline="\n")
check("PB3114 (d): make_dispatch_specs.py records nothing when its practices check is RED",
      (mds.main([str(gred), "--coord", str(C3114)]), pw.ledger_groups(C3114)), (1, []))

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
DL_BUSY = {"worktree dl-1 (worktree-dl-1)": pw.InFlight({F_BIND, "docs/CONFORMANCE.md"}, "worktree-dl-1", {"PB5"},
                                                        state="DECISION"),
           "worktree dl-2 (worktree-dl-2)": pw.InFlight({"src/X/Misc.cs", "docs/CONFORMANCE.md"}, "worktree-dl-2",
                                                        {"PB9"}, state="DECISION")}
pdl = pw.plan(notes, [cl(F_BIND, "PB5"), cl("src/X/Misc.cs", "PB9")], [], pw.load_reports(DL_REPORTS),
              lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=DL_BUSY)
dl = {n: g for g in pdl["groups"] for n in g.notes}
check("PB3528: two SPLIT branches sharing a file are both finished, the second a successor of the first",
      (sorted(dl), dl["PB9"].after if "PB9" in dl else None, dl["PB5"].kind if "PB5" in dl else None),
      (["PB5", "PB9"], dl["PB5"].letter if "PB5" in dl else "?", "finisher"))
check("PB3528: neither waits on the other", sorted(n for n in pdl["waiting"] if n in ("PB5", "PB9")), [])
# ... and on every host alike: the two reports tie on wave and modification time (returned() above), so only their
# NAME orders them; left to the directory's enumeration order (the name on NTFS, a hash order on ext4) the successor
# was PB5's finisher under the Linux gate and PB9's on Windows. A reversed listing must not change the order read.
_glob = pathlib.Path.glob
try:
    pathlib.Path.glob = lambda self, pattern: reversed(list(_glob(self, pattern)))
    reversed_listing = [r.path.name for r in pw.load_reports(DL_REPORTS)]
finally:
    pathlib.Path.glob = _glob
check("PB3528: reports tied on wave and mtime are read in name order, whatever order the directory lists them in",
      (reversed_listing, [r.path.name for r in pw.load_reports(DL_REPORTS)]),
      (["w1141a-PB5-report.md", "w1141b-PB9-report.md"], ["w1141a-PB5-report.md", "w1141b-PB9-report.md"]))
# ... and a branch no group of this wave finishes still holds a group that meets it (the partition is unchanged)
pdl2 = pw.plan(notes, [cl(F_BIND, "PB5")], [], [], lambda b: "ABSENT", {}, RULES, budget_points=100,
               busy={"worktree dl-3 (worktree-dl-3)": pw.InFlight({F_BIND}, "worktree-dl-3", {"PB9"}, state="DECISION")})
check("PB3528: an undecided branch this wave does not finish still holds a group on its files",
      ([g.notes for g in pdl2["groups"]], "PB5" in pdl2["waiting"]), ([], True))
# ... and a ONE-SIDED meeting still chains (branch review R1-1): PB5's group meets PB9's branch on a file PB5's notes
# own (its primary file) while PB9's group never meets PB5's branch; whichever is placed first, the other follows it.
DL1_BUSY = {"worktree dl-1 (worktree-dl-1)": pw.InFlight({F_BIND}, "worktree-dl-1", {"PB5"}, state="DECISION"),
            "worktree dl-2 (worktree-dl-2)": pw.InFlight({"src/X/Misc.cs", "docs/CONFORMANCE.md"}, "worktree-dl-2",
                                                         {"PB9"}, state="DECISION")}
pdl3 = pw.plan(notes, [cl("docs/CONFORMANCE.md", "PB5"), cl("src/X/Misc.cs", "PB9")], [], pw.load_reports(DL_REPORTS),
               lambda b: "UNLANDED", {}, RULES, budget_points=100, busy=DL1_BUSY)
dl3 = {n: g for g in pdl3["groups"] for n in g.notes}
check("PB3528 R1-1: a one-sided meeting between two finishers chains them; neither runs beside the other",
      (sorted(dl3), {g.after for g in dl3.values() if g.after} <= {g.letter for g in dl3.values()},
       sum(1 for g in dl3.values() if g.after) == 1),
      (["PB5", "PB9"], True, True))

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
order = {k: [r.path.name for r in pw.load_reports(d)] for k, d in O_REPORTS.items()}
check("PB3625: reports are read oldest first by write time, whichever lane's wave number is larger",
      order, {"a": ["w20261024a-PB5-report.md", "w1157a-PB5-report.md"],
              "b": ["w1157a-PB5-report.md", "w20261024a-PB5-report.md"]})
po = {k: pw.plan(notes, [cl(F_BIND, "PB5")], [], pw.load_reports(d), lambda b: "UNLANDED", {}, RULES,
                 budget_points=100) for k, d in O_REPORTS.items()}
check("PB3625: a later serial-wave DONE report supersedes the date-wave SPLIT report: no finisher of the SPLIT branch",
      [b for g in po["a"]["groups"] if g.kind == "finisher" and "PB5" in g.notes for b in g.continues], [])
check("PB3625: the converse holds: a SPLIT report written after the DONE one is the note's newest and gets its finisher",
      [b for g in po["b"]["groups"] if g.kind == "finisher" and "PB5" in g.notes for b in g.continues], ["worktree-old-1"])

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
check("PB3170 (a): x, whose four commits y replays by cherry-pick and q holds as ancestors, is carried (by the branch "
      "that replays the most, the later of two ties); it has no commit of its own beyond them",
      (found["x"].carrier, found["x"].fresh, found["x"].by), ("q", (), (("q", 4), ("y", 4))))
check("PB3170 (a): z, two of x's commits cherry-picked, is carried too, by every larger UNCARRIED branch (x is carried)",
      (found["z"].carrier, found["z"].fresh, [b for b, _ in found["z"].by]), ("q", (), ["q", "y"]))
check("PB3170 (a): y is NOT carried: its fifth commit is in no larger branch (fresh), and q replays the other four",
      (found["y"].carrier, found["y"].fresh, found["y"].by), (None, (y5,), (("q", 4),)))
check("PB3170 (a): the largest of the family, q, carries nothing: exactly one branch still does the work",
      (found["q"].carrier, len(found["q"].fresh), found["q"].by), (None, 5, ()))
check("PB3170 (a): an unrelated branch is replayed by nobody", (found["w"].carrier, found["w"].fresh, found["w"].by),
      (None, (w1,), ()))
# the tie: two branches holding the same work cannot each carry the other; the later report's carries the earlier's
RG.git("switch", "-q", "-c", "x2", "x")
tie = prune.replays(["x", "x2"], ["x", "x2"], {"x": 0, "x2": 1}.get, git_batch.Ancestry.read(RG.path, ["x", "x2"]))
check("PB3170 (a): two branches holding the same commits: the later report carries the earlier, never both ways",
      (tie["x"].carrier, tie["x2"].carrier), ("x2", None))
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
check("PB3170 (a): in a successor chain every carried link rides on the TOP branch, never on a carried middle link",
      [(chain[b].carrier, chain[b].fresh == ()) for b in CH_ORDER], [("b-top", True), ("b-top", True), (None, False)])
check("PB3170 (a): ... and `by` names only branches that have a finisher",
      [[n for n, _ in chain[b].by] for b in CH_ORDER], [["b-top"], ["b-top"], []])
# (b) THROUGH THE PLANNER'S OWN WIRING (branch_replays: the reports' order is the rank, ABANDONED carries nothing)
rg_reports = [pw.Report(pathlib.Path(f"{slug}-{lead}-report.md"), int(slug[1:5]), slug, lead, "SPLIT", [lead], False,
                        branch, None, "7777777aa")   # oldest first: x's report, then y's
              for slug, lead, branch in (("w1077b", "PB6", "x"), ("w1078b", "PB5", "y"))]
rep_all = pw.branch_replays(rg_reports, lambda b: "UNLANDED", {}, RG_GRAPH, set())
rep_ab = pw.branch_replays(rg_reports, lambda b: "UNLANDED", {}, RG_GRAPH, {"y"})
rep_land = pw.branch_replays(rg_reports, lambda b: "LANDED" if b == "y" else "UNLANDED", {}, RG_GRAPH, set())
check("PB3170 (b): x is replayed by y when y has a later report (y's own fifth commit is not x's business)",
      (rep_all("x").carrier, rep_all("x").fresh), ("y", ()))
check("PB3170 (b): an ABANDONED branch carries nothing, and a LANDED one is no unlanded branch to carry",
      (rep_ab("x").carrier, rep_ab("x").by, rep_land("x").carrier), (None, (), None))
# abandon_records is abandon_record for a list, in one pass over the notes
AB = TMP / "Work-3170"
AB.mkdir()
(AB / "PB901.md").write_text("`old-a` is ABANDONED, superseded by `new-a`.\nnot abandoned: old-b\n", encoding="utf-8", newline="\n")
(AB / "PB902.md").write_text("nothing here\n`old-c` abandoned (wave 1, SPLIT).\n", encoding="utf-8", newline="\n")
ab_names = ["old-a", "new-a", "old-b", "old-c", "none"]
check("PB3170 (b): abandon_records agrees with abandon_record for each branch, in one pass",
      prune.abandon_records(ab_names, AB), {b: n for b in ab_names if (n := prune.abandon_record(b, AB))})
check("PB3170 (b): ... and names the note that records it, never a branch a line merely mentions",
      prune.abandon_records(ab_names, AB), {"old-a": "PB901", "old-c": "PB902"})

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
    return pw.plan(notes, clusters or [cl(F_BIND, "PB5", "PB6")], [], pw.load_reports(R_REPORTS), lambda b: "UNLANDED",
                   unlanded or {}, RULES, budget_points=100, replay=replay, abandoned=abandoned, held=held)


def continuing(p, branch):
    return [sorted(g.notes) for g in p["groups"] if branch in g.continues]


FULL = {B28: prune.Replay(B3A, (), ((B3A, 4),)), B3A: prune.Replay(None, ("f" * 12,), ())}
before = plan3170("SPLIT")
check("PB3170 (c): WITHOUT the rule both SPLIT branches get a finisher (the defect: the same four commits land twice)",
      (continuing(before, B28), continuing(before, B3A)), ([["PB6"]], [["PB5"]]))
after = plan3170("SPLIT", replay=FULL.get)
check("PB3170 (c): a SPLIT branch another SPLIT branch replays gets no finisher; its note rides on the carrier's group",
      (continuing(after, B28), continuing(after, B3A)), ([], [["PB5", "PB6"]]))
check("PB3170 (c): ... and the carrier's pred names the branch it carries",
      [("ride on this branch" in g.pred, B28 in g.pred) for g in after["groups"] if B3A in g.continues], [(True, True)])
done = plan3170("DONE", replay=FULL.get)
check("PB3170 (c): the carrier DONE (it lands on its own): the carried note waits for it, with the reason, no finisher",
      (continuing(done, B28), "PB6" in done["waiting"] and B3A in done["waiting"]["PB6"]), ([], True))
part = plan3170("SPLIT", replay={B28: prune.Replay(None, ("abc123def456",), ((B3A, 3),)), B3A: FULL[B3A]}.get)
check("PB3170 (c): a branch only PARTLY replayed keeps its finisher, whose pred names only the commits no other branch holds",
      (continuing(part, B28), [("ONLY" in g.pred, "abc123def" in g.pred, B3A in g.pred) for g in part["groups"]
                               if B28 in g.continues]), ([["PB6"]], [(True, True, True)]))
gone = plan3170("SPLIT", abandoned={B28})
check("PB3170 (c): an ABANDONED branch is never planned from: no finisher continues it, its note is planned from main",
      (continuing(gone, B28), any("PB6" in g.notes and not g.continues for g in gone["groups"]) or
       any("PB6" in g.notes for g in gone["groups"] if B3A in g.continues)), ([], True))
# the unlanded-branch arm (a branch with commits naming a note and NO report), and the half-note arm, are the siblings
two = pw.plan(notes, [cl(F_EMIT, "PB7")], [], [], lambda b: "UNLANDED", {"b-1": ["PB7"], "b-2": ["PB7"]}, RULES,
              budget_points=100, replay={"b-1": prune.Replay("b-2", (), (("b-2", 2),))}.get)
check("PB3170 (c): the sibling arm: a report-less unlanded branch another branch replays gets no finisher either",
      [g.continues for g in two["groups"]], [["b-2"]])
twoh = pw.plan(notes, [], [cl("src/X/Half.cs", "PB4")], [], lambda b: "UNLANDED", {"b-1": ["PB4"], "b-2": ["PB4"]}, RULES,
               budget_points=100, replay={"b-1": prune.Replay("b-2", (), (("b-2", 2),))}.get)
check("PB3170 (c): ... and the half-note arm resumes from the replaying branch only",
      [g.continues for g in twoh["groups"]], [["b-2"]])

# (d) THE TWO RULES COMPOSE: a rider (PB3170) belongs to the replaying branch's UNIT (kb/Work PB2974). A dispatch holding
# the carrier's note holds the rider too; the carrier's one finisher takes the rider AND the unreported notes its own
# branch carries, naming the replayed commits once; a report-less branch's one finisher names its fresh commits.
hold = plan3170("SPLIT", replay=FULL.get, held={"PB5": "dispatch w1080a"})
check("PB3170 x PB2974: a dispatch holding the carrier's note holds the rider on it: no finisher over either branch",
      (continuing(hold, B28), continuing(hold, B3A)), ([], []))
check("PB3170 x PB2974: ... and the rider waits naming the held note and its dispatch",
      ("PB5" in hold["waiting"].get("PB6", ""), "w1080a" in hold["waiting"].get("PB6", "")), (True, True))
PART3A = {B28: FULL[B28], B3A: prune.Replay(None, ("fedcba987654",), (("worktree-r-zz", 1),))}
both = plan3170("SPLIT", replay=PART3A.get, unlanded={B3A: ["PB7"]}, clusters=[cl(F_BIND, "PB5", "PB6", "PB7")])
check("PB3170 x PB2974: the carrier's ONE finisher takes its note, the rider and its branch's unreported note",
      (continuing(both, B28), continuing(both, B3A), [g.kind for g in both["groups"]]),
      ([], [["PB5", "PB6", "PB7"]], ["finisher"]))
check("PB3170 x PB2974: ... its pred names the rider and the replayed commits, the latter exactly once",
      [("ride on this branch" in g.pred, g.pred.count("cherry-pick ONLY"), "fedcba987" in g.pred, "PB7" in g.pred)
       for g in both["groups"]], [(True, 1, True, True)])
lone = pw.plan(notes, [cl(F_EMIT, "PB7")], [], [], lambda b: "UNLANDED", {"b-1": ["PB7"]}, RULES, budget_points=100,
               replay={"b-1": prune.Replay(None, ("0123456789ab",), (("b-2", 1),))}.get)
check("PB3170 x PB2974: a report-less branch partly replayed is ONE finisher naming only its fresh commits",
      [(g.continues, "cherry-pick ONLY" in g.pred, "012345678" in g.pred) for g in lone["groups"]],
      [(["b-1"], True, True)])

# PB3653: A LIVE AGENT'S LOCK HOLDS A BRANCH'S UNIT, a touch alone does not (prune_worktrees.locked is the lock-only half of
# `owned`). A hand-started agent on a SPLIT branch leaves no ledger entry, so the dispatch hold (6d above) cannot see it.
LK = "worktree-wf_un-1"   # w1053b's branch (6d): SPLIT report, notes PB301 and PB303 (PB302 is w1055a's, on another branch); nothing is held
for drain in (False, True):
    mode = "drain" if drain else "normal"
    lk = pw.plan(UN_NOTES, UN_CL, [], pw.load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                 budget_points=100, drain=drain, locked={LK})
    check(f"PB3653 {mode}: a SPLIT branch whose worktree a live agent LOCKS gets no finisher for any of its notes",
          [g.notes for g in lk["groups"] if {"PB301", "PB303"} & set(g.notes)], [])
    check(f"PB3653 {mode}: ... each note waits naming the branch and the lock",
          [("locked by a live agent" in lk["waiting"].get(i, "") and LK in lk["waiting"].get(i, ""))
           for i in ("PB301", "PB303")], [True, True])
    own = pw.plan(UN_NOTES, UN_CL, [], pw.load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                  budget_points=100, drain=drain, owned={LK})
    check(f"PB3653 {mode}: a branch merely OWNED (a touch inside --min-age, the state of one whose agent just returned) "
          "is still finished: only a lock or a dispatch holds the SPLIT arm",
          [(g.notes, g.continues) for g in own["groups"] if "PB301" in g.notes], [(["PB301", "PB303"], [LK])])
check("PB3653: another branch's lock holds nothing here",
      [g.notes for g in pw.plan(UN_NOTES, UN_CL, [], pw.load_reports(UN_REPORTS, UN), lambda b: "UNLANDED", {}, RULES,
                                budget_points=100, locked={"worktree-wf_other-1"})["groups"] if "PB301" in g.notes],
      [["PB301", "PB303"]])
lk_ub = pw.plan(UN_NOTES, UN_CL, [], [], lambda b: "UNLANDED", UB, RULES, budget_points=100, locked={"worktree-wf_ub-1"})
check("PB3653: ... and an unlanded branch with no report is held by its lock too (one hold loop for every arm)",
      ([g.notes for g in lk_ub["groups"] if {"PB311", "PB312", "PB313"} & set(g.notes)],
       ["locked by a live agent" in lk_ub["waiting"].get(i, "") for i in ("PB311", "PB312", "PB313")]),
      ([], [True, True, True]))

# PB3653's residual: two DIFFERENT unfinished reports about one branch are one finisher group, the newest report's. A
# note is decided by its newest report, so PB5's (w1090a) and PB6's (w1090b) both name the branch, and they planned two.
TWO = TMP / "reports-two-on-one"
TWO.mkdir()
for slug, lead, when in (("w1090a", "PB5", 1_000_000_000), ("w1090b", "PB6", 1_000_003_600)):
    q = TWO / f"{slug}-{lead}-report.md"
    q.write_text(f"SPLIT\n# {lead} group\n**Status:** SPLIT · **branch:** `worktree-two-1` · **HEAD:** `7777777aa`\n\n"
                 "## Reproduced?\n", encoding="utf-8", newline="\n")
    os.utime(q, (when, when))
two_on_one = pw.plan(notes, [cl(F_BIND, "PB5", "PB6")], [], pw.load_reports(TWO), lambda b: "UNLANDED", {}, RULES,
                     budget_points=100)
check("PB3653: two unfinished reports about one branch plan ONE finisher over it, naming the newest report",
      [(g.kind, sorted(g.notes), g.continues, "w1090b" in g.pred) for g in two_on_one["groups"]],
      [("finisher", ["PB5", "PB6"], ["worktree-two-1"], True)])

# Branch review R1-1: the merge target is the newest UNFINISHED report. A newer DONE report about another lead on the same
# branch is no target: the finisher's pred would say "w1090b returned DONE for PB5" and name the wrong report.
TDN = TMP / "reports-two-done-newest"
TDN.mkdir()
for slug, lead, status, when in (("w1090a", "PB5", "SPLIT", 1_000_000_000), ("w1090b", "PB6", "DONE", 1_000_003_600)):
    q = TDN / f"{slug}-{lead}-report.md"
    q.write_text(f"{status}\n# {lead} group\n**Status:** {status} · **branch:** `worktree-two-1` · **HEAD:** `7777777aa`\n\n"
                 "## Reproduced?\n", encoding="utf-8", newline="\n")
    os.utime(q, (when, when))
two_done = pw.plan(notes, [cl(F_BIND, "PB5", "PB6")], [], pw.load_reports(TDN), lambda b: "UNLANDED", {}, RULES,
                   budget_points=100)
check("PB3653 R1-1: an unfinished report's finisher is not merged into a newer DONE report of its branch (pred names the SPLIT one)",
      [(g.kind, sorted(g.notes), "w1090a" in g.pred, "w1090b" in g.pred) for g in two_done["groups"]],
      [("finisher", ["PB5"], True, False)])

# PB3476: THE COMMIT A REPORT DESCRIBES is its header's HEAD, else the head its review stamp names; the one parser
# (parse_report) is read by the branch survey, the landing lane and the landing measure.
HD = TMP / "reports-head"
HD.mkdir()
STAMP = "**Review:** CLEAN w1136c-review-r1 bf1cfafc7981"


def head_of(name, header):
    q = HD / name
    q.write_text(header + "\n\n## Reproduced?\n", encoding="utf-8", newline="\n")
    return pw.parse_report(q).head


check("PB3476: a HEAD field that gives no sha ('see the trailing commit') takes the head of the review stamp",
      head_of("w1136c-PB3471-report.md",
              f"Status: DONE\n{STAMP}\n**Branch:** worktree-x-1\n**HEAD:** see the trailing commit (code at `ac4f3be8`)\n"),
      "bf1cfafc7981")
check("PB3476: ... a header with a HEAD sha keeps it, the stamp notwithstanding",
      head_of("w1136d-PB3472-report.md", f"Status: DONE\n{STAMP}\n**Branch:** worktree-x-2\n**HEAD:** 1234567abc\n"),
      "1234567abc")
check("PB3476: ... and a header with neither describes no commit",
      head_of("w1136e-PB3473-report.md", "Status: DONE\n**Branch:** worktree-x-3\n**HEAD:** see the trailing commit\n"),
      None)


# PB3474: THE NEVER-WORK RULE HAS ONE HOME, coord.NEVER_WORK (coord.is_work, coord.NEVER_WORK_PATHSPECS). A module that
# tests a path against STATUS.md, TestResults/ or the settings file in its own way is a second copy of it: prune_worktrees'
# classify() kept one (with a `tools/` exclusion NEVER_WORK lacks, which hid every file added under tools/), and
# impacted_tests.changed_files another.
def never_work_copies(sources):
    """The modules (not coord.py, the owner, nor a test file: its fixtures name these paths) that compare a path with
    one of coord.NEVER_WORK's entries or test its prefix by hand."""
    shape = re.compile(r"""startswith\(\s*\(?\s*['"](?:TestResults/|STATUS\.md)"""
                       r"""|[!=]=\s*['"](?:STATUS\.md|\.claude/settings\.local\.json)['"]"""
                       r"""|['"](?:STATUS\.md|\.claude/settings\.local\.json)['"]\s*[!=]=""")
    return sorted(p for p, t in sources.items() if p != "scripts/orchestrator/coord.py"
                  and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


check("PB3474: no module outside coord.py tests a path against coord.NEVER_WORK by hand", never_work_copies(TREE), [])
check("PB3474: the scan fails on the planted shapes of the old copies",
      never_work_copies({"scripts/a.py": "f for f in names if not f.startswith(('TestResults/', 'tools/')) and f != 'STATUS.md'",
                         "scripts/b.py": "{p for p in out if p and p != \"STATUS.md\"}",
                         "scripts/c.py": "if path == '.claude/settings.local.json': continue",
                         "scripts/orchestrator/coord.py": "p == 'STATUS.md'", "scripts/test_d.py": "p != 'STATUS.md'",
                         "scripts/e.py": "(repo / 'STATUS.md').write_text(x)"}),
      ["scripts/a.py", "scripts/b.py", "scripts/c.py"])


# PB3652: ONE RULE FOR WHO CARRIES WHOM. The planner's branch_replays and the survey's contained() read the same
# function (prune_worktrees.replay_view); a second call of `replays` from anywhere else is a second reading of its inputs.
def replay_callers(sources):
    shape = re.compile(r"\breplays\(")
    return sorted(p for p, t in sources.items() if p != "scripts/prune_worktrees.py"
                  and not pathlib.PurePosixPath(p).name.startswith("test_") and shape.search(t))


check("PB3652: only prune_worktrees calls `replays` (the planner and the survey go through replay_view)",
      replay_callers(TREE), [])
check("PB3652: the scan fails on a planted direct call",
      replay_callers({"scripts/orchestrator/x.py": "prune_worktrees.replays(branches, carriers, rank, graph)"}),
      ["scripts/orchestrator/x.py"])

for f in fails:
    print("FAIL:", f)
print(f"plan_wave self-test: {len(checked) - len(fails)}/{len(checked)} checks OK")
sys.exit(1 if fails else 0)
