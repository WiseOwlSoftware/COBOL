#!/usr/bin/env python3
"""Cases of land_lane.py: THE CONFLICT BRIEF: main and the branch both edit a file since their merge base (ported from the self-test of scripts/orchestrator/land_lane.py, DESIGN-orchestrator-loop.md
section 16.2, kb/Work PB5434). The cases run in name order over ONE world (`fixtures.world()`, bound by case_000), as the
sequential self-test did: a case reads what an earlier one planted (the module globals below).
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_conflict_brief.py"""
from __future__ import annotations

import sys
import selftest_cases
from standard_streams import ensure_utf8

import fixtures
from scratch_repo import ScratchRepo
from typing import Any
import pathlib
import verdict_line
from fixtures import Fake, fresh, wait_runners

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py
HERE = ll.HERE
MAIN = ll.MAIN
attempts = ll.attempts
conflict_brief = ll.conflict_brief
show_conflict_brief = ll.show_conflict_brief
status_lines = ll.status_lines
tick = ll.tick

# the world's state: bound by the cases in order, read by the later ones
a17 = brief_calls = brief_file = brief_reports = c17 = c17b = cbr = fixture_git = r17 = root = shown = None


def case_000_conflict_brief_git_s_dry_run_names_the_file_both(expect):
    global brief_reports, cbr, fixture_git, root
    root = fixtures.world()

    # 17. THE CONFLICT BRIEF (kb/Work PB3640): main and the branch both edit scripts/x.py since their merge base;
    #     main also adds a file the branch never touches. The brief names the conflicted file, main's commit on it
    #     (and not the other file), the notes on both sides with their status and newest report.
    import git_merge  # noqa: PLC0415
    from reports import Report  # noqa: PLC0415
    cbr = ScratchRepo.init(root / "wt-brief")
    fixture_git = lambda *args, cwd=None: cbr.run(*args, cwd=cwd)   # noqa: E731  (the self-test's git: the fixture)

    def note(nid: str, status: str, title: str) -> str:
        return f"---\nid: {nid}\nstatus: {status}\ntitle: \"{nid} — {title}\"\n---\n\n# {nid}\n"
    cbr.commit("base", {"scripts/x.py": "a = 1\n", "kb/Work/PB9002.md": note("PB9002", "open", "the branch's fix")})
    merge_base = cbr.git("rev-parse", "HEAD")
    cbr.commit("WIP checkpoint: PB9001 main's side of x", {"scripts/x.py": "a = 2\n", "scripts/y.py": "y = 1\n",
                                                           "kb/Work/PB9001.md": note("PB9001", "landed", "main's fix")})
    cbr.git("update-ref", MAIN, "HEAD")
    cbr.git("reset", "-q", "--hard", merge_base)
    cbr.commit("WIP checkpoint: PB9002 the branch's side of x", {"scripts/x.py": "a = 3\n"})
    brief_reports = [Report(pathlib.Path("w9a-PB9001-report.md"), 9, "w9a", "PB9001", "DONE", ["PB9001"],
                              False, "fix-main", None, "aaaa"),
                     Report(pathlib.Path("w9b-PB9002-report.md"), 9, "w9b", "PB9002", "DONE", ["PB9002"],
                              False, "wbrief", str(cbr.path), "bbbb")]
    expect("conflict brief: git's dry run names the file both sides edited, not the one only main added",
           git_merge.merge_conflicts(cbr.path, MAIN) == ["scripts/x.py"],
           str(git_merge.merge_conflicts(cbr.path, MAIN)))


def case_001_conflict_brief_no_git_call_s_arguments_grow_with(expect):
    global brief_calls
    brief_calls = []

    def counting_git(*args: str, cwd: str | None = None) -> Any:
        brief_calls.append(args)
        return fixture_git(*args, cwd=cwd)
    many = ["scripts/x.py", *(f"tests/many/{n:04}.json" for n in range(2000))]
    conflict_brief("wbrief", str(cbr.path), many, git=counting_git, reports=brief_reports)
    expect("conflict brief: no git call's arguments grow with the conflicted files (763 files overflowed Windows' "
           "command line, kb/Work PB5002)",
           brief_calls and max(len(a) for a in brief_calls) < 20, str(max((len(a) for a in brief_calls), default=0)))


def case_002_conflict_brief_a_conflicted_file_main_renamed_aw(expect):
    ren = ScratchRepo.init(root / "wt-brief-rename")
    ren.commit("base", {"a.txt": "one\ntwo\nthree\nfour\n"})
    ren_base = ren.git("rev-parse", "HEAD")
    ren.commit("PB9003 main renames a to b", {"a.txt": None, "b.txt": "one\ntwo\nthree\nfour\n"})
    ren.commit("PB9004 main rewrites b", {"b.txt": "five\n"})
    ren.git("update-ref", MAIN, "HEAD")
    ren.git("reset", "-q", "--hard", ren_base)
    ren.commit("WIP checkpoint: PB9005 the branch edits a", {"a.txt": "one\ntwo\nthree\nFOUR\n"})
    renamed = conflict_brief("wren", str(ren.path), ["a.txt"], git=lambda *a, cwd=None: ren.run(*a, cwd=cwd),
                             reports=[])
    ren_gained = next((s for s in renamed.split(chr(10) + "## ") if s.startswith("What main gained")), "")
    expect("conflict brief: a conflicted file main renamed away keeps main's rename commit (review w20261932b R1-1: "
           "without a pathspec git pairs the rename and names only the new path)",
           "PB9003 main renames a to b" in ren_gained.split("### a.txt", 1)[-1], renamed)


def case_003_conflict_brief_main_s_commit_on_the_conflicted_f(expect):
    text = conflict_brief("wbrief", str(cbr.path), ["scripts/x.py"], git=fixture_git, reports=brief_reports)
    sections = text.split(chr(10) + "## ")
    gained = next((s for s in sections if s.startswith("What main gained")), "")
    expect("conflict brief: main's commit on the conflicted file under its heading, and no other file's",
           "### scripts/x.py" in gained and "PB9001 main's side of x" in gained and "scripts/y.py" not in gained,
           text)
    expect("conflict brief: the branch's report, and the notes on both sides with their status and newest report",
           "report: w9b-PB9002-report.md (DONE" in text
           and "- PB9002 (open): PB9002 — the branch's fix — newest report: w9b-PB9002-report.md (DONE)" in text
           and "- PB9001 (landed): PB9001 — main's fix — newest report: w9a-PB9001-report.md (DONE)" in text,
           text)


def case_004_conflict_brief_a_note_two_branch_subjects_name_i(expect):
    cbr.commit("WIP checkpoint: PB9002 more of the branch's side", {"scripts/z.py": "z = 1\n"})
    bare = conflict_brief("wbrief", str(cbr.path), ["scripts/x.py"], git=fixture_git, reports=[])
    own_section = next((s for s in bare.split(chr(10) + "## ") if s.startswith("The branch's notes")), "")
    expect("conflict brief: a note two branch subjects name is listed once, not once per subject (PB3640 review "
           "R1-2: the repeats ate the BRIEF_NOTES budget main's notes are read from)",
           own_section.count("- PB9002 (") == 1 and "- PB9001 (landed)" in bare, bare)


def case_005_conflict_brief_the_runner_of_an_exit_4_attempt_w(expect):
    global a17, brief_file, c17, r17
    # ... recorded by the runner on exit 4, pointed to by the status line, printed by `conflict-brief`
    c17 = fresh("conflict")
    fake4 = [sys.executable, "-c",
             f"import sys; sys.path.append({str(HERE.parent)!r}); "  # as enqueue.py starts: its streams in UTF-8
             "from standard_streams import ensure_utf8; ensure_utf8(); "
             f"print({verdict_line.render(verdict_line.conflict_outcome(['scripts/x.py']), 'wbrief')!r}); "
             "raise SystemExit(4)"]
    out: list[str] = []
    r17 = {"name": "wbrief", "state": "WAITING TO LAND", "tip": cbr.git("rev-parse", "HEAD"),
           "worktree": str(cbr.path), "why": "", "lead": None}
    tick(c17, root, survey=lambda repo, cdir: ([r17], ["wbrief"], None), argv_for=lambda row: fake4, say=out.append)
    wait_runners(c17)
    a17 = (attempts(c17) or [{}])[0]
    brief_file = pathlib.Path(a17.get("brief") or root / "absent")
    expect("conflict brief: the runner of an exit-4 attempt writes it beside the records and names it on the attempt",
           a17.get("rc") == 4 and brief_file.is_file() and "### scripts/x.py" in brief_file.read_text(encoding="utf-8"),
           str(a17))
    expect("conflict brief: the STOPPED status line points to `land_lane.py conflict-brief <branch>`",
           any(ln.startswith("STOPPED") and "conflict-brief wbrief" in ln for ln in status_lines(c17)),
           str(status_lines(c17)))


def case_006_conflict_brief_conflict_brief_prints_the_recorde(expect):
    global shown
    shown = []
    expect("conflict brief: `conflict-brief` prints the recorded brief while the branch is at the attempt's end head",
           show_conflict_brief(c17, "wbrief", fixture_git, shown.append) == 0
           and shown == [brief_file.read_text(encoding="utf-8").rstrip()], str(shown))


def case_007_conflict_brief_once_origin_main_moves_conflict_b(expect):
    moved = cbr.git("commit-tree", f"{MAIN}^{{tree}}", "-p", MAIN, "-m", "WIP checkpoint: PB9003 main moves on")
    cbr.git("update-ref", MAIN, moved)
    reshown: list[str] = []
    expect("conflict brief: once origin/main moves, `conflict-brief` computes a fresh brief against the new main, "
           "never the recorded one (PB3640 review R1-4)",
           a17.get("brief_main") and a17.get("brief_main") != moved
           and show_conflict_brief(c17, "wbrief", fixture_git, reshown.append) == 0
           and reshown != shown and f"Bringing origin/main ({moved[:12]})" in "".join(reshown),
           f"{a17.get('brief_main')} {reshown}")


def case_008_conflict_brief_a_bounced_branch_no_attempt_stopp(expect):
    global c17b
    # ... and for a pull request the queue BOUNCED as DIRTY (no attempt stopped): computed now from the survey's row
    c17b = fresh("dirty")
    f17b = Fake([{**r17, "name": "wdirty", "state": "BOUNCED"}])
    f17b.tick(c17b)
    dirty_out: list[str] = []
    expect("conflict brief: a BOUNCED branch no attempt stopped gets one computed against origin/main",
           f17b.started == [] and show_conflict_brief(c17b, "wdirty", fixture_git, dirty_out.append) == 0
           and "leaves 1 file(s) in conflict: scripts/x.py" in "".join(dirty_out), str(dirty_out))


def case_009_conflict_brief_nothing_to_brief_once_origin_main(expect):
    cbr.git("update-ref", MAIN, "HEAD")
    clean_out: list[str] = []
    expect("conflict brief: nothing to brief once origin/main merges clean (exit 1, said)",
           show_conflict_brief(c17b, "wdirty", fixture_git, clean_out.append) == 1
           and "nothing to brief" in "".join(clean_out), str(clean_out))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
