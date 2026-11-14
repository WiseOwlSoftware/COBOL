#!/usr/bin/env python3
"""The planner's fixtures (DESIGN-orchestrator-loop.md section 16.2; kb/Work PB5428): the scratch world its cases plant,
built by `plant()` (the directories, the fabricated notes and reports, the clusters) and the helpers that write them.
No case and no expectation lives here."""
import selftest_cases

pw = selftest_cases.subject(__file__)   # scripts/orchestrator/plan_wave.py: puts scripts/orchestrator/ on sys.path
import json  # noqa: E402
import os  # noqa: E402
import pathlib  # noqa: E402
import sys  # noqa: E402
import coord  # noqa: E402
import work  # noqa: E402
from reports import load_reports  # noqa: E402
from inflight import InFlight, collision, collision_hits, hold  # noqa: E402
from clusters import load_notes, notes_cap  # noqa: E402
from scratch_repo import scratch_dir  # noqa: E402

sys.path.insert(0, str(coord.REPO / ".claude" / "skills" / "workstream"))   # the public-skills checkers the specs meet
import check_practices  # noqa: E402
import once_rule  # noqa: E402
import make_dispatch_specs as mds  # noqa: E402

HERE = pathlib.Path(pw.__file__).resolve().parent


def rules_capped(rules, n):
    """A copy of `rules` whose group size (clusters.notes_cap, kb/Work PB4396) is `n` notes: the turn budget set half a
    note's turns past n notes' prediction, whatever the implementer's turn cap is."""
    out = json.loads(json.dumps(rules))
    w = out["wave"]
    w["turn_budget_fraction"] = (w["turns_per_group"] + w["turns_per_note"] * (n + 0.5)) / coord.agent_turn_cap(
        "cobol-implementer")
    return out


def collided(mine, busy):
    """The whole-file collision hits as text, each '<owner> (<what they share>)'."""
    return [f"{o} ({h})" for o, h in collision_hits(mine, busy)]


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


def cl(file, *ids, harm=1):
    return {"file": file, "harm": harm, "files": [file], "notes": [{"id": i, "area": "", "harm": 1, "title": ""} for i in ids]}


def on_members(file, members, *ids):
    """A cluster on `file` claiming only `members` of it."""
    return {**cl(file, *ids), "members": {file: list(members)}}


def running(files, branch, members=None, **kw):
    """RUNNING work that edits `members` ({file: names}) of `files`: a hold needs a member on both sides (inflight.hold)."""
    return InFlight(set(files), branch, members={f: frozenset(m) for f, m in (members or {}).items()}, **kw)


def plant():
    """The planner's world: the scratch directories, the fabricated notes and reports, the clusters.
    Returns the state the cases read, by name."""
    global CLASS, CLUSTERS, F_BIND, F_EMIT, F_G4, F_OO, HALF, LIVE_RULES, REPORTS, RULES, TMP, UNLANDED, WORK, notes, reports
    LIVE_RULES = coord.rules()   # the rules as model_rules.json states them (the PB3638 block reads the owner's one link)
    RULES = coord.rules()
    # The chain mechanics (letters A2, A3, `after:`, a successor's continuation) are what a wave dispatches when it runs more
    # than one link of a chain; the owner's rule runs one (wave.links_per_chain, kb/Work PB3638), so the fixtures below,
    # which test the chains themselves, plan every link, and the PB3638 block tests the live value.
    RULES["wave"]["links_per_chain"] = 99


    TMP = scratch_dir("plan-wave-test-")
    WORK = TMP / "Work"
    WORK.mkdir()
    REPORTS = TMP / "reports"
    REPORTS.mkdir()


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


    CLUSTERS = [cl(F_BIND, "PB1", "PB2", "PB5"), cl(F_EMIT, "PB6", "PB7", "PB3"), cl(F_EMIT, "PB12"),
                cl(F_OO, "PB8"), cl(F_G4, "PB11"), cl("src/X/Misc.cs", "PB9"), cl("src/X/Other.cs", "PB10"),
                cl("src/X/Zero.cs", "PB13"), cl(F_BIND, "PB14")]
    HALF = [cl("src/X/Half.cs", "PB4")]
    UNLANDED = {"worktree-wf_aaa-1": ["PB1"], "worktree-wf_ddd-9": ["PB13"], "worktree-wf_eee-1": [],
                "worktree-wf_fff-3": ["PB4"]}  # a killed wave left WIP for the half note PB4

    notes = load_notes(WORK, {"wrong_answer": 8})
    reports = load_reports(REPORTS)
    return {"CLASS": CLASS, "CLUSTERS": CLUSTERS, "F_BIND": F_BIND, "F_EMIT": F_EMIT, "F_G4": F_G4, "F_OO": F_OO, "HALF": HALF, "LIVE_RULES": LIVE_RULES, "REPORTS": REPORTS, "RULES": RULES, "TMP": TMP, "UNLANDED": UNLANDED, "WORK": WORK, "notes": notes, "reports": reports}
