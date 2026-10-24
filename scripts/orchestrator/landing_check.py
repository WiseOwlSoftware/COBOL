#!/usr/bin/env python3
"""landing_check.py — the file-set partition's GUARANTEE, checked where every landing passes
(scripts/orchestrator/enqueue.py checks, exit 3).

WHY (kb/Work PB2118 Drafts 9-10; DESIGN-architecture-review §8.7). The owner made the mechanical file-set partition the
ONLY gate between the R3 restructuring waves (cluster PB2119) and the fix lane's trains (PB2118 question 5). The
planner (plan_wave.py --cluster) admits a wave on its COMPUTED file set: its sites, the callers the member index
finds, the files under its site folders. Three refuter rounds each found a new hole in that computation (hand lists,
then the index's blind spots, then its staleness), so the planner's set is an ADMISSION ESTIMATE; a miss in it must
be caught, never silently landed. This is the second line: before a landing reaches main it compares what the landing
ACTUALLY changed with

  1. its DECLARED file set, for the R3 work it lands: the set the dispatch ledger recorded for those notes when they
     were planned, read from the FULL ledger (a landing flips its own notes to terminal, and that declaration is
     still the authority for it); for a note no entry records that the landing's OWN recorded dispatch closed on the
     way (an implementer fixed it, MANDATORY-PRACTICES I9), that dispatch's recorded set (kb/Work PB3263); and only for
     work no dispatch of the landing records, the set computed now (plan_wave.computed_file_set, on the planner's own
     index policy, THE index of this tree: kb/Work PB3203). A file an R3 commit changed that existed on main and lies
     outside that set STOPS the landing, the stop naming the arm the set came from: the wave touched a file nothing
     protected. A consulted group recorded with NO files declared nothing: the set is UNKNOWN (plan_wave.collision),
     so such a file is named in a warning instead, and check 2 still applies to it;
  2. every OTHER in-flight branch's actual changes (plan_wave.inflight_file_sets: worktrees, unlanded branches, the
     dispatch ledger's declared sets), when the shared file is R3 work on either side. A shared file stops the LATER
     of the two (by dispatch time: the ledger's `at`, else the branch's first commit): it re-plans and rebases onto
     the earlier after that lands. The earlier one lands, and the check names the branch that must rebase.

R3 work is attributed PER COMMIT (PB2703). A train is MIXED: the lander makes one commit per cluster, and some clusters
work on R3 notes while the rest are fix lane or tooling. The landing's R3 work is the commits that work on an R3 note
(the ids leading the subject, or an R3 status the commit flips) and the partition files those commits change; a
fix-lane commit in the same train is fix lane, so its files neither leave the R3 set (1) nor make a shared file R3 (2).
A squashed commit that names both kinds is R3 whole: it stops rather than land silently. Each commit is charged only
with what it ITSELF introduced (commit_work, PB3180): a non-merge commit its diff against its parent; a MERGE commit
the files and the note statuses that differ from EVERY parent (its conflict resolutions and evil-merge edits) plus the
ids leading its own subject, never its first-parent diff, which carries the other parent's whole history (a merged
predecessor's commits, attributed one by one by the same walk, or main's own landings in a `Merge origin/main`).

Draft 10 (the ninth refuter's K1-K3). Every diff is read with --no-renames (plan_wave.changed_files), so a moved file
counts at its old path too. An in-flight branch is R3 work because its DISPATCH says so, never by its commit wording
alone (implementers commit `WIP checkpoint: …`), and it is bound to ONE dispatch (identity, kb/Work PB3186): its
fix-lane REPORT (the newest whose branch field names it) and that report's ledger group, else the ids that lead its
commit subjects or whose status it flips, else the ONE ledger group that best covers its actual diff (best_group). The
union of every ledger group its diff met made a fix-lane worktree on a file one R3 group also declared R3 work, and with
the scraped hand entries the dispatch guard once wrote (six of 303-910 files) it stopped 23 fix-lane landings on
2026-10-14/15. The ledger is read through plan_wave.ledger_groups, which ignores those retired entries. A dispatch
entry stays in flight with its whole declared set, started or not, until its notes are terminal or its report answers it
(its agent returned: its work is then its branch's actual diff, kb/Work PB2795). A branch this landing
contains (it merged it) is the landing's own: only its later, uncommitted edits count; and so is a file whose version
on another branch IS the landing's (the same work re-committed). Two kinds of in-flight work never block a landing
(kb/Work PB3381 section H): a branch that DESCENDS from the landing branch (its merge-base with it lies beyond
`base`: it merged some head of it, so it is a same-file successor, a review finisher or a carrier of this landing,
and it lands on top of it: wave 1118's successor -326-13 merged an OLDER head of PB3333 and so "shared" six files with
PB3333's reviewed branch, stopping its own predecessor's landing three times with both gates GREEN), and a worktree
whose branch has no commit beyond `base` (a finisher cut from main that has not committed: its loose files are a merge
or an edit in progress, and its dispatch, when the ledger records one, is read through its declared set). Anything
else that shares a file and was dispatched earlier stops it: the partition orders only INDEPENDENT dispatches that
touch the same primary file, and the stop names how it was identified and how to bind work it failed to recognise.

Fix lane against fix lane is never stopped: fix-lane implementers share files by design, and the merge queue merges
them (at worst through a conflict finisher, MANDATORY-PRACTICES L12). The partition's domain is the campaign's code roots (model_rules.json `campaign`: src, tests, scripts,
.github, docs, minus exclude_dir) and only CODE files there (its extensions but .md): a document both sides edit is a
text merge the rebase resolves, and kb/Work and DEVLOG/ are every landing's bookkeeping.

Usage:
    python scripts/orchestrator/landing_check.py [--rev HEAD] [--base origin/main] [--coord DIR]
    python scripts/orchestrator/landing_check.py --self-test
Exit 0: the landing may proceed (warnings name branches that must rebase). Exit 1: STOP (each reason printed).
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys
import tempfile
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import plan_wave  # noqa: E402  (inflight_file_sets: THE in-flight enumeration; computed_file_set: THE declared set)
import work  # noqa: E402

PB = re.compile(r"\bPB\d+\b")
LEADING_IDS = re.compile(r"^((?:PB\d+[\s+,&-]*)+)", re.M)   # "PB1939+PB2004: ..." — the notes a commit works on
ID_RANGE = re.compile(r"\bPB(\d+)-PB(\d+)\b")                # "PB2478-PB2483: ..." — every id of the range
MAX_RANGE = 64
STATUS_LINE = re.compile(r"^([-+])status:[ ]*(\S*)")   # a frontmatter status line removed or added by a patch
WORK_DIR = "kb/Work/"
EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"   # git's empty tree: what a root commit's diff is taken against


def _git(repo: pathlib.Path) -> Callable[..., Any]:
    def run(*args: str, cwd: Any = repo) -> subprocess.CompletedProcess:
        return subprocess.run(["git", "-C", str(cwd), *args], capture_output=True, text=True, encoding="utf-8",
                              errors="replace")
    return run


def subject_ids(subjects: str) -> set[str]:
    """The note ids that LEAD the commit subjects, ranges expanded (`PB2478-PB2483:` is six notes, not two)."""
    out = set()
    for m in LEADING_IDS.findall(subjects):
        out |= set(PB.findall(m))
        out |= {f"PB{n}" for a, b in ID_RANGE.findall(m) if 0 <= int(b) - int(a) <= MAX_RANGE
                for n in range(int(a), int(b) + 1)}
    return out


def status_flips(git: Callable[..., Any], *revs: str) -> set[str]:
    """The notes whose `status` the diff `git diff <revs>` changes. ONE patch of the register, not two `git show`s per
    changed note (the ninth refuter's N1: 400 shows were 17 of the check's 24 s on a real range). A note's status
    changed when its patch removes one `status:` line and adds a different one; a note FILED there has no removed line,
    so it is not worked on there."""
    nid, was = None, {}
    for line in git("diff", "--no-renames", "-U0", *revs, "--", WORK_DIR).stdout.splitlines():
        if line.startswith("diff --git "):
            stem = pathlib.PurePosixPath(line.rsplit(" b/", 1)[-1]).stem
            nid = stem if PB.fullmatch(stem) else None
        elif nid and (m := STATUS_LINE.match(line)):
            was.setdefault(nid, {})[m.group(1)] = m.group(2)
    return {n for n, s in was.items() if "-" in s and s["-"] != s.get("+")}


def work_notes(git: Callable[..., Any], base: str, rev: str) -> set[str]:
    """The notes the LANDING `base..rev` works on: the ids that lead a commit subject (the lander re-commits one
    `PB…: …` commit per cluster) and the notes whose `status` it changes (a landing flips its note in the commit that
    lands it, CLAUDE.md rule 8). Never every id a message mentions: a design commit that names the notes it re-plans
    works on none of them. An IN-FLIGHT branch's identity never comes from here alone (identity(), K2). One COMMIT's
    notes are commit_work's, never this over `sha^..sha` (PB3180)."""
    return subject_ids(git("log", "--format=%s", f"{base}..{rev}").stdout) | status_flips(git, f"{base}...{rev}")


def commit_work(git: Callable[..., Any], base: str, rev: str) -> list[tuple[set[str], set[str]]]:
    """THE per-commit attribution (PB2703, PB3180): (the notes it works on, the files it changed) for every commit
    of `base..rev`, oldest first, each charged only with what the commit ITSELF introduced. A non-merge commit is its
    diff against its one parent (a root commit's against the empty tree). A MERGE commit is what differs from EVERY
    parent — the files (combined-diff semantics, as `git diff-tree -c` lists them: its conflict resolutions and
    evil-merge edits) and the notes whose `status` it changes against each parent — plus the ids leading its OWN
    subject. Never its first-parent diff: that carries the other parent's whole history (a merged predecessor's
    commits, which this same walk already attributes one by one, or main's own landings in a `Merge origin/main`), and
    charging it to the merge smeared one commit's R3 note across other commits' files (the PB3154 false stop)."""
    out = []
    for line in git("rev-list", "--reverse", "--parents", f"{base}..{rev}").stdout.splitlines():
        sha, *parents = line.split()
        sides = parents or [EMPTY_TREE]
        notes = subject_ids(git("log", "-1", "--format=%s", sha).stdout)
        notes |= set.intersection(*(status_flips(git, p, sha) for p in sides))
        out.append((notes, set.intersection(*(plan_wave.changed_files(git, p, sha) for p in sides))))
    return out


def first_commit_time(git: Callable[..., Any], base: str, rev: str) -> float | None:
    out = git("log", "--reverse", "--format=%ct", f"{base}..{rev}").stdout.split()
    return float(out[0]) if out else None


def _key(path: str | None) -> str:
    return (path or "").replace(chr(92), "/").rstrip("/").lower()


def best_group(files: set[str], groups: list[dict[str, Any]]) -> dict[str, Any] | None:
    """The ONE ledger group that best covers an in-flight branch's actual diff `files` (kb/Work PB3186): the most of
    its files among the group's declared files, then the smallest declared set (the most specific claim), then the
    latest dispatch. None when no group declares any of them. Never the union of every overlapping group: a fix-lane
    worktree on a file an R3 group also declares is the fix-lane group's, and the union made it R3 work."""
    scored = [(len(files & {f.replace(chr(92), "/") for f in g.get("files", [])}), g) for g in groups]
    return max(((n, g) for n, g in scored if n),
               key=lambda ng: (ng[0], -len(ng[1].get("files", [])), ng[1].get("at", 0.0)), default=(0, None))[1]


def check(repo: pathlib.Path, base: str, rev: str, coord_dir: pathlib.Path, items: list[dict], rules: dict[str, Any],
          declared_for: Callable[[list[str]], set[str]], git: Callable[..., Any] | None = None,
          r3_lead: str = work.R3_PROGRAM, reports: list[plan_wave.Report] = ()) -> tuple[list[str], list[str]]:
    """(stops, warnings) for landing `rev` onto `base`. `declared_for(note ids)` computes a declared set the dispatch
    ledger does not hold; `reports` are the fix-lane reports (plan_wave.load_reports), which bind an in-flight branch
    to its own dispatch first (identity)."""
    git = git or _git(repo)
    inside = coord.campaign(rules).partitioned   # the partition's domain: a code file under a campaign root
    r3 = {i.get("id") for i in items if r3_lead in (i.get("cluster") or [])}
    status = {i.get("id"): i.get("status") for i in items}
    terminal = lambda n: status.get(n) in work.TERMINAL_STATUSES  # noqa: E731
    mine = {p for p in plan_wave.changed_files(git, f"{base}...{rev}") if inside(p)}
    notes = work_notes(git, base, rev)
    # R3 work is attributed PER COMMIT (PB2703): a landing can be MIXED — a branch that merged a predecessor, a train
    # before kb/Work PB3023 — some commits R3 notes, the rest fix lane or tooling — so the R3 work is the commits that
    # work on an R3 note and the partition files THEY change, never the whole landing's diff, and a merge commit is
    # charged only with what it introduced (commit_work, PB3180). A squashed commit naming both kinds is R3 whole.
    r3_notes, mine_r3_files = set(), set()
    for worked, changed in commit_work(git, base, rev):
        if hit := worked & r3:
            r3_notes |= hit
            mine_r3_files |= {p for p in changed if inside(p)}
    mine_r3 = sorted(r3_notes, key=lambda s: int(s[2:]))
    full_ledger = plan_wave.ledger_groups(coord_dir)   # THE reader: a retired hand entry is no dispatch (PB3186)
    ledger = [g for g in full_ledger if not all(terminal(i) for i in g.get("notes", []))]
    my_at = min([g["at"] for g in ledger if "at" in g and set(g.get("notes", [])) & notes]
                + [first_commit_time(git, base, rev) or float("inf")])
    stops, warnings = [], []

    if mine_r3:   # 1. the declared set
        # From the FULL ledger, terminal or not: the declaration recorded when the work was planned is the authority
        # for the landing that makes those notes terminal (`work.load` reads the LANDING's tree, where its own notes
        # are already landed, PB2703). A note no entry records falls back to the set computed now.
        # An R3 note no entry records that THIS LANDING'S OWN recorded dispatch closed on the way (its implementer fixed
        # it, MANDATORY-PRACTICES I9) is that dispatch's work: it is checked against that group's recorded set, never a
        # set computed now from sites that predate the group's own new files (kb/Work PB3263: PB3334's engine branch,
        # which also closed PB2222 and PB2342, stopped on four engine files on 2026-10-18). Only work no dispatch of
        # this landing records falls back to the set computed now.
        recorded = [g for g in full_ledger if set(g.get("notes", [])) & r3_notes]
        unrecorded = sorted(r3_notes - {n for g in recorded for n in g.get("notes", [])}, key=lambda s: int(s[2:]))
        carriers = [g for g in full_ledger if unrecorded and g not in recorded and set(g.get("notes", [])) & notes]
        consulted = recorded + carriers
        declared = {f.replace(chr(92), "/") for g in consulted for f in g.get("files", [])}
        computed = unrecorded if unrecorded and not carriers else []
        declared |= declared_for(computed) if computed else set()
        arms = ([f"recorded in the dispatch ledger for {', '.join(plan_wave.ledger_slug(g) for g in recorded)}"]
                if recorded else []) + \
               ([f"the landing's own dispatch {', '.join(plan_wave.ledger_slug(g) for g in carriers)}, which closed "
                 f"{', '.join(unrecorded)} on the way"] if carriers else []) + \
               ([f"computed now from the site blocks of {', '.join(computed)}, which no dispatch of this landing "
                 f"records"] if computed else [])
        existed = {p for p in mine_r3_files if git("cat-file", "-e", f"{base}:{p}").returncode == 0}
        outside = sorted(existed - declared)
        listed = f"{', '.join(outside[:8])}{' …' if len(outside) > 8 else ''}"
        # a group recorded with NO files declared nothing: its set is UNKNOWN (plan_wave.collision), so a file outside
        # the rest cannot be judged here; it is named, and arm 2 still checks it against every in-flight branch
        if outside and (unknown := [plan_wave.ledger_slug(g) for g in consulted if not g.get("files")]):
            warnings.append(f"R3 work ({', '.join(mine_r3)}) changed {len(outside)} file(s) its declared file set "
                            f"({'; '.join(arms)}) does not name: {listed} — the set is UNKNOWN, not checked: "
                            f"{', '.join(unknown)} recorded no file set (kb/Work PB3263), so the planner never "
                            f"partitioned these files; the in-flight check below still applies")
        elif outside:
            stops.append(f"R3 work ({', '.join(mine_r3)}) changed {len(outside)} file(s) outside its declared file set "
                         f"({'; '.join(arms)}): {listed} — re-plan: "
                         + ("the recorded set missed them (the planner's estimate: its notes' sites and the member "
                            "index's callers; if the member index missed a caller, file a kb/Work note)"
                            if not computed else
                            "the notes' site blocks do not name them (add them to each note's **Sites:**, or bind the "
                            "work to its dispatch)")
                         + ", and check them against the in-flight work before landing")

    meta: dict[str, dict] = {}
    # an answered dispatch (its report exists) is read through its branch, never its declared set (PB2795)
    busy = plan_wave.inflight_file_sets(git=git, repo=repo, coord_dir=coord_dir, meta=meta, terminal=terminal,
                                        reports=reports)

    def contained(their_rev: str, files: set[str]) -> set[str]:
        """The files whose version on `their_rev` IS this landing's: a lander that re-committed or patched a member's
        work carries it by content, not by commit (the retired train applier, kb/Work PB3023)."""
        return {f for f in files if git("rev-parse", "-q", "--verify", f"{rev}:{f}").stdout.strip()
                == git("rev-parse", "-q", "--verify", f"{their_rev}:{f}").stdout.strip()}

    report_of: dict[str, plan_wave.Report] = {}            # branch / worktree key -> its NEWEST report
    for r in reports:                                      # oldest first: the last write is the newest
        report_of.update({k: r for k in (r.branch, _key(r.worktree)) if k})
    by_slug = {plan_wave.ledger_slug(g): g for g in full_ledger}

    def identity(m: dict, files: set[str]) -> tuple[set[str], float, str]:
        """(the notes an in-flight branch works on, its dispatch time, how that is known), bound to ONE dispatch by
        the first of (kb/Work PB3186; never by its WIP commit wording alone, K2; never the union of every ledger group
        its diff meets, which made a fix-lane worktree R3 work when one overlapping group was):
          1. its REPORT (the newest whose branch field or worktree names it): that report's ledger group (its slug,
             `w<wave><letter>`), else the report's lead;
          2. its COMMIT SUBJECTS: the ids that lead them or whose status it flips;
          3. the ONE ledger group that best covers its actual diff (best_group)."""
        how = ""
        if rep := report_of.get(m.get("branch") or "") or report_of.get(_key(m.get("path"))):
            g = by_slug.get(rep.slug.lower())
            their, how = (set(g.get("notes", [])) if g else {rep.lead}), f"report {rep.path.name}"
        elif subj := work_notes(git, base, m["rev"]):
            their, how = subj, "commit subjects"
        elif g := best_group(files, ledger):
            their, how = set(g.get("notes", [])), f"dispatch-ledger group {plan_wave.ledger_slug(g)}"
        else:
            their = set()
        at = min([g["at"] for g in ledger if "at" in g and set(g.get("notes", [])) & their]
                 + [first_commit_time(git, base, m["rev"]) or float("inf")])
        return their, at, how or "nothing"

    def depends(their_rev: str) -> bool:
        """An in-flight branch that blocks nothing (kb/Work PB3381 section H): one with no commit beyond `base` (an
        empty worktree: its loose files are work in progress, not a dispatch's claim), or one that DESCENDS from the
        landing branch: their merge-base is a commit beyond `base`, so it merged some head of this branch (a successor,
        a review finisher, a carrier) and lands on top of it. Never one this landing merged: the caller reads that one
        by its later, uncommitted edits."""
        if git("merge-base", "--is-ancestor", their_rev, base).returncode == 0:
            return True
        mb = git("merge-base", their_rev, rev).stdout.strip()
        return bool(mb) and git("merge-base", "--is-ancestor", mb, base).returncode != 0 \
            and git("merge-base", "--is-ancestor", their_rev, rev).returncode != 0

    for owner, files in sorted(busy.items()):
        m = meta.get(owner, {})
        if m.get("rev"):                                   # a worktree or an unlanded branch, by its ACTUAL diff
            if depends(m["rev"]):
                continue                                   # empty, or built on this landing: it lands on top of it
            if git("merge-base", "--is-ancestor", m["rev"], rev).returncode == 0:
                files = set(m.get("uncommitted", set()))   # its commits are in this landing; later edits are its own
            shared = {f for f in mine & files if inside(f)}
            if not shared:
                continue
            their_notes, their_at, how = identity(m, files)
            if not their_notes or their_notes & notes:     # not known to be OTHER work: its re-committed content
                shared -= contained(m["rev"], shared - set(m.get("uncommitted", set())))   # is this landing's (K3)
                if not shared:
                    continue
        else:                                              # an unanswered dispatch: its DECLARED set, started or not
            their_notes, their_at, how = set(m.get("notes", [])), m.get("at", 0.0), "dispatch ledger"
            if their_notes & notes:
                continue                                   # this landing's own dispatch entry
            shared = {f for f in mine & files if inside(f)}
            if not shared:
                continue
        if not shared & mine_r3_files and not their_notes & r3:
            continue                                       # fix lane against fix lane is never stopped: the merge
                                                           # queue merges them, at worst through a conflict finisher
                                                           # (PB2703: per file — only a file an R3 commit changed is
                                                           # R3 here; PB3186: their notes are ONE dispatch's)
        names = f"{', '.join(sorted(shared)[:5])}{' …' if len(shared) > 5 else ''}"
        if their_at < my_at:
            unbound = (" It is " + (f"{', '.join(sorted(their_notes)[:4])} work by its {how}" if how != "nothing"
                                    else "bound to no dispatch: no report names its branch, no commit subject leads "
                                         "with its notes and no dispatch-ledger group declares its files") +
                       "; if it IS this landing's own work, merge it into this branch (a merged branch is this "
                       "landing's) and re-run.") if m.get("rev") else ""
            stops.append(f"shares {names} with {owner}, dispatched earlier: this landing is the LATER branch — it "
                         f"waits, re-plans and rebases onto {owner} after that lands.{unbound}")
        else:
            warnings.append(f"shares {names} with {owner}, dispatched later: this landing proceeds; {owner} must "
                            f"re-plan and rebase onto it")
    return stops, warnings


# ── self-test: plants an out-of-set edit and a two-branch overlap in a scratch repository ───────────────────────
def self_test() -> int:
    sys.path.insert(0, str(HERE.parent))
    from scratch_repo import ScratchRepo, isolate  # noqa: PLC0415  (scripts/: the one scratch-repository fixture)
    isolate()   # the subject's own git (check's port) runs in this process: never on the caller's repository
    root = pathlib.Path(tempfile.mkdtemp(prefix="landing-check-"))
    scratch = ScratchRepo.init(root / "repo")
    repo, cdir = scratch.path, root / "coord"
    cdir.mkdir()
    git = _git(repo)

    def write(rel: str, text: str) -> None:
        scratch.write({rel: text})

    for f in ("src/A.cs", "src/B.cs", "src/C.cs", "docs/D.md"):
        write(f, "// base\n")
    write("kb/Work/PB9002.md", "---\nid: PB9002\nstatus: open\n---\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "base", when=1_000_000)
    scratch.git("update-ref", "refs/remotes/origin/main", "HEAD")

    def branch(name: str, msg: str, edits: dict[str, str], when: int) -> None:
        scratch.git("checkout", "-q", "-b", name, "origin/main")
        for f, t in edits.items():
            write(f, t)
        scratch.git("add", "-A")
        scratch.git("commit", "-qm", msg, when=when)
        scratch.git("checkout", "-q", "main")

    items = [{"id": "PB9001", "status": "open", "cluster": ["PB2119"]},
             {"id": "PB9002", "status": "open", "cluster": ["PB2119"]},
             {"id": "PB9100", "status": "open", "cluster": []},
             {"id": "PB9101", "status": "open", "cluster": []}]
    rules = {"campaign": {"src": ["src", "docs"], "ext": ".cs,.md", "exclude_dir": []}}
    coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": [
        {"wave": "1", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs"], "at": 1_000_100},
        {"wave": "1", "letter": "b", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_300}]})
    never = lambda ids: set()  # noqa: E731
    results = []

    def expect(label: str, got: tuple[list[str], list[str]], stops: int, warns: int) -> None:
        ok = (len(got[0]), len(got[1])) == (stops, warns)
        results.append((label, ok, got))

    # in-set R3 work, nothing else in flight: lands
    branch("r-ok", "PB9001: extract in set", {"src/A.cs": "// moved\n", "src/New.cs": "// new\n",
                                               "docs/D.md": "doc\n"}, 1_000_200)
    expect("an R3 landing inside its declared set (a new file and a doc edit) lands",
           check(repo, "origin/main", "r-ok", cdir, items, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "r-ok")
    # PLANT 1: an out-of-set edit (src/B.cs existed on main and is not declared)
    branch("r-out", "PB9001: extract, one file too many", {"src/A.cs": "// moved\n", "src/B.cs": "// caller\n"},
           1_000_200)
    expect("PLANT 1: an R3 landing that edits a file outside its declared set STOPS",
           check(repo, "origin/main", "r-out", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "r-out")
    # PLANT 2: a two-branch overlap — R3 work (dispatched at 1_000_100) and a fix-lane branch (first commit later)
    branch("r-one", "PB9001: extract", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("f-two", "PB9100: fix", {"src/A.cs": "// fixed\n"}, 1_000_500)
    expect("PLANT 2a: the LATER branch (the fix-lane train) sharing a file with in-flight R3 work STOPS (against "
           "the R3 branch and against its dispatch entry)",
           check(repo, "origin/main", "f-two", cdir, items, rules, never, git), 2, 0)
    expect("PLANT 2b: the EARLIER branch (the R3 wave) lands and names the branch that must rebase",
           check(repo, "origin/main", "r-one", cdir, items, rules, never, git), 0, 1)
    # a branch whose commits the landing contains IS the landing: a train built on the R3 branch r-one names only
    #   f-two (dispatched later) as a branch to rebase, never r-one itself
    scratch.git("checkout", "-q", "-b", "train", "r-one")
    write("src/F.cs", "// a fix-lane file the train adds\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9101: fix in the train", when=1_000_700)
    scratch.git("checkout", "-q", "main")
    expect("a branch whose commits the landing contains is not in flight against it",
           check(repo, "origin/main", "train", cdir, items, rules, never, git), 0, 1)
    scratch.git("branch", "-D", "train")
    # fix lane against fix lane: a train merges them (no R3 work on either side)
    scratch.git("branch", "-D", "r-one")
    items[0]["status"] = "landed"   # r-one landed: its dispatch entry is no longer in flight
    branch("f-three", "PB9101: fix", {"src/A.cs": "// fixed too\n"}, 1_000_600)
    expect("fix-lane branches sharing a file with no R3 work in flight pass",
           check(repo, "origin/main", "f-three", cdir, items, rules, never, git), 0, 0)
    # a declared set computed now when the ledger never recorded the work (a hand dispatch)
    # a design commit that NAMES an R3 note it does not work on is not R3 work (its subject does not lead with it)
    branch("design", "WIP checkpoint: re-plan PB9001 and PB9002", {"src/B.cs": "// tooling\n"}, 1_000_750)
    expect("a commit that only mentions an R3 note is not R3 work",
           check(repo, "origin/main", "design", cdir, items, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "design")
    # a hand wave whose subject names no note but which flips its note's status IS R3 work (the status flip)
    branch("r-hand", "Hand wave: extract", {"src/B.cs": "// hand\n",
                                             "kb/Work/PB9002.md": "---\nid: PB9002\nstatus: landed\n---\n"}, 1_000_800)
    coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": []})
    expect("an unrecorded R3 landing is checked against the set computed now (outside it: STOP)",
           check(repo, "origin/main", "r-hand", cdir, items, rules, lambda ids: {"src/C.cs"}, git), 1, 0)
    expect("... and inside it: lands",
           check(repo, "origin/main", "r-hand", cdir, items, rules, lambda ids: {"src/B.cs"}, git), 0, 0)
    for b in ("f-two", "f-three", "r-hand"):
        scratch.git("branch", "-D", b)

    # ── Draft 10: the ninth refuter's K1 (renames), K2 (identity), K3 (a landing's own members), N2, N5 ──
    items[0]["status"] = "open"
    def ledger(*groups: dict) -> None:
        coord.write_json(cdir / plan_wave.DISPATCH_LEDGER, {"groups": list(groups)})

    def moved(name: str, msg: str, old: str, new: str, when: int) -> None:
        scratch.git("checkout", "-q", "-b", name, "origin/main")
        scratch.git("mv", old, new)
        scratch.git("commit", "-qm", msg, when=when)
        scratch.git("checkout", "-q", "main")

    def worktree(name: str, msg: str, edits: dict[str, str], when: int) -> pathlib.Path:
        wt = scratch.worktree(root / name, branch=name, start="origin/main")
        wt.commit(msg, edits, when=when)
        return wt.path

    def drop(name: str) -> None:
        scratch.git("worktree", "remove", "--force", str(root / name))
        scratch.git("branch", "-D", name)

    a_set = {"wave": "2", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs"], "at": 1_000_100}
    ledger(a_set)
    moved("r-mv", "PB9001: move", "src/B.cs", "src/Moved.cs", 1_000_200)
    expect("K1a: an R3 `git mv` of a file outside its declared set STOPS (the OLD path counts)",
           check(repo, "origin/main", "r-mv", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "r-mv")
    ledger()
    moved("r-mv2", "PB9001: move", "src/A.cs", "src/A2.cs", 1_000_200)
    branch("f-mv", "PB9100: fix", {"src/A.cs": "// fixed\n"}, 1_000_500)
    expect("K1b: a fix-lane edit to a path an in-flight R3 branch RENAMED STOPS (only the rename shares it)",
           check(repo, "origin/main", "f-mv", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "r-mv2")
    scratch.git("branch", "-D", "f-mv")

    ledger(a_set)
    worktree("wt-wip", "WIP checkpoint: extract", {"src/A.cs": "// moved\n", "src/B.cs": "// caller\n"}, 1_000_200)
    branch("f-b", "PB9100: fix", {"src/B.cs": "// fixed\n"}, 1_000_500)
    expect("K2: an R3 worktree whose commits are all `WIP checkpoint:` is R3 work by its dispatch entry: a later "
           "fix-lane edit to a file it changed OUTSIDE its declared set STOPS",
           check(repo, "origin/main", "f-b", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "f-b")
    drop("wt-wip")

    worktree("wt-impl", "WIP checkpoint: extract", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("r-train", "PB9001: extract", {"src/A.cs": "// moved\n"}, 1_000_900)
    expect("K3a: a train that re-committed its own implementer's work (recorded dispatch) lands with no warning",
           check(repo, "origin/main", "r-train", cdir, items, rules, never, git), 0, 0)
    ledger()
    expect("K3a': ... and so does the same train with the dispatch unrecorded (its content is the landing's)",
           check(repo, "origin/main", "r-train", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 0, 0)
    scratch.git("branch", "-D", "r-train")
    branch("r-train2", "PB9001: extract", {"src/A.cs": "// moved, conflict resolved\n"}, 1_000_900)
    got = check(repo, "origin/main", "r-train2", cdir, items, rules, lambda ids: {"src/A.cs"}, git)
    expect("K3b: an earlier worktree bound to no dispatch, whose content the landing changed, STOPS", got, 1, 0)
    expect("K3b': ... and the stop says how to bind it", ([s for s in got[0] if "merge it into this branch" in s], []),
           1, 0)
    scratch.git("checkout", "-q", "-b", "r-train3", "wt-impl")   # the landing MERGED its implementer's branch (a successor)
    write("src/A.cs", "// moved, conflict resolved\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: extract, resolved", when=1_000_900)
    scratch.git("checkout", "-q", "main")
    expect("K3c: an implementer branch the landing contains is the landing's own: lands",
           check(repo, "origin/main", "r-train3", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 0, 0)
    worktree("wt-other", "WIP checkpoint: another", {"src/A.cs": "// someone else\n"}, 1_000_150)
    expect("K3d: ... and another earlier worktree on the same file still STOPS it",
           check(repo, "origin/main", "r-train3", cdir, items, rules, lambda ids: {"src/A.cs"}, git), 1, 0)
    drop("wt-other")
    scratch.git("branch", "-D", "r-train2")
    # kb/Work PB3381 section H: a successor that merged an OLDER head of the landing branch descends from it and lands
    # on top of it, so it never blocks it (wave 1118: -326-13 held PB3333's 3dadec3a3 and stopped PB3333's reviewed
    # -326-20 three times); an empty worktree (no commit beyond main, a merge in progress) blocks nothing; an
    # independent earlier dispatch on the same file (K3d above) still stops it.
    scratch.git("checkout", "-q", "-b", "succ-old", "wt-impl")   # merged the landing's OLDER head (wt-impl), then its own edit
    write("src/A.cs", "// the successor's own edit on the shared file\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "WIP checkpoint: the successor", when=1_000_950)
    scratch.git("checkout", "-q", "main")
    scratch.git("checkout", "-q", "-b", "pred-new", "wt-impl")   # the landing: the predecessor's newer, reviewed head
    write("src/A.cs", "// the predecessor's review fix\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: the review fix", when=1_000_960)
    scratch.git("checkout", "-q", "main")
    # the successor's group was planned with the wave, before any commit of the landing: by dispatch time it is the
    # EARLIER one, which is what stopped the predecessor before section H
    ledger({"wave": "4", "letter": "b", "notes": ["PB9100"], "files": ["src/A.cs"], "at": 1_000_100})
    hdir = root / "reports-h"
    hdir.mkdir()
    (hdir / "w4b-PB9100-report.md").write_text("Status: DONE" + chr(10) + "Branch: succ-old" + chr(10),
                                               encoding="utf-8", newline=chr(10))
    h_reports = plan_wave.load_reports(hdir)
    expect("H1: a successor holding an OLDER head of the landing branch descends from it: it does not block it",
           check(repo, "origin/main", "pred-new", cdir, items, rules, lambda ids: {"src/A.cs"}, git,
                 reports=h_reports), 0, 0)
    empty = root / "wt-empty"
    scratch.git("worktree", "add", "-q", "-b", "wt-empty", str(empty), "origin/main")
    (empty / "src" / "A.cs").write_text("// a merge in progress, nothing committed\n", encoding="utf-8", newline="\n")
    expect("H2: a worktree with no commit beyond main (its edits uncommitted) blocks nothing",
           check(repo, "origin/main", "pred-new", cdir, items, rules, lambda ids: {"src/A.cs"}, git,
                 reports=h_reports), 0, 0)
    drop("wt-empty")
    worktree("wt-indep", "WIP checkpoint: independent", {"src/A.cs": "// independent work\n"}, 1_000_150)
    expect("H3: ... while an INDEPENDENT earlier dispatch on the same file still STOPS it",
           check(repo, "origin/main", "pred-new", cdir, items, rules, lambda ids: {"src/A.cs"}, git,
                 reports=h_reports), 1, 0)
    drop("wt-indep")
    ledger()
    for b in ("succ-old", "pred-new"):
        scratch.git("branch", "-D", b)
    drop("wt-impl")
    scratch.git("branch", "-D", "r-train3")

    # ── PB3186: a worktree is bound to ONE dispatch (its report, its commit subjects, else the one ledger group that
    #    best covers its diff), never the union of every group its diff meets; a retired hand entry is no dispatch ──
    r3_ab = {"wave": "3", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs", "src/B.cs"], "at": 1_000_100}
    fix_bc = {"wave": "3", "letter": "b", "notes": ["PB9100"], "files": ["src/B.cs", "src/C.cs"], "at": 1_000_150}
    ledger(r3_ab, fix_bc)
    worktree("wt-fix", "WIP checkpoint: fix", {"src/B.cs": "// fix-lane wip\n", "src/C.cs": "// fix-lane wip\n"},
             1_000_200)
    branch("f-land", "PB9101: a later fix", {"src/C.cs": "// a later fix\n"}, 1_000_500)
    expect("PB3186a: a fix-lane worktree on a file an R3 group ALSO declares binds to the group covering its diff "
           "(PB9100): fix lane against fix lane passes",
           check(repo, "origin/main", "f-land", cdir, items, rules, never, git), 0, 0)
    drop("wt-fix")
    worktree("wt-rep", "WIP checkpoint: fix", {"src/A.cs": "// wip\n", "src/B.cs": "// wip\n",
                                                "src/D.cs": "// wip\n"}, 1_000_200)
    branch("f-d", "PB9101: a later fix", {"src/D.cs": "// a later fix\n"}, 1_000_500)
    expect("PB3186b: a WIP worktree whose diff the R3 group covers best is R3 work (K2): a later edit it shares STOPS",
           check(repo, "origin/main", "f-d", cdir, items, rules, never, git), 1, 0)
    rdir = root / "reports"
    rdir.mkdir(exist_ok=True)
    (rdir / "w3b-PB9100-report.md").write_text("Status: DONE" + chr(10) + "Branch: wt-rep" + chr(10), encoding="utf-8",
                                               newline=chr(10))
    expect("PB3186c: ... but its REPORT binds it first, to its own group (w3b, fix lane): passes",
           check(repo, "origin/main", "f-d", cdir, items, rules, never, git,
                 reports=plan_wave.load_reports(rdir)), 0, 0)
    drop("wt-rep")
    scratch.git("branch", "-D", "f-d")
    ledger({"wave": "hand", "letter": "", "hand": True, "notes": ["PB9001"], "files": ["src/C.cs"], "at": 1_000_000},
           {"wave": "x", "letter": "a", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_000})
    expect("PB3186d: a retired hand entry and a non-numeric wave are no dispatch: a fix-lane landing on their files passes",
           check(repo, "origin/main", "f-land", cdir, items, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "f-land")

    ledger(dict(a_set, files=["src/A.cs", "src/C.cs"]))
    worktree("wt-start", "WIP checkpoint: part 1", {"src/A.cs": "// moved\n"}, 1_000_200)
    branch("f-c", "PB9100: fix", {"src/C.cs": "// fixed\n"}, 1_000_500)
    expect("N2: a STARTED R3 wave keeps its untouched declared files: a later fix-lane edit to one STOPS",
           check(repo, "origin/main", "f-c", cdir, items, rules, never, git), 1, 0)
    (rdir / "w2a-PB9001-report.md").write_text("Status: SPLIT" + chr(10), encoding="utf-8", newline=chr(10))
    expect("PB2795: ... until its agent RETURNS: a group its report answers (SPLIT residue included) is read through "
           "its branch, so its untouched declared files are free",
           check(repo, "origin/main", "f-c", cdir, items, rules, never, git,
                 reports=plan_wave.load_reports(rdir)), 0, 0)
    scratch.git("branch", "-D", "f-c")
    drop("wt-start")

    # ── PB2703: a MIXED train (one commit per cluster: R3 and fix lane) is attributed per commit ──
    def commits(name: str, *steps: tuple[str, dict[str, str], int]) -> None:
        scratch.git("checkout", "-q", "-b", name, "origin/main")
        for msg, edits, when in steps:
            for f, t in edits.items():
                write(f, t)
            scratch.git("add", "-A")
            scratch.git("commit", "-qm", msg, when=when)
        scratch.git("checkout", "-q", "main")

    ledger(a_set, {"wave": "2", "letter": "b", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_300})
    commits("mixed", ("PB9001: extract in set", {"src/A.cs": "// moved\n"}, 1_000_200),
            ("PB9100: fix-lane cluster", {"src/B.cs": "// fixed\n"}, 1_000_250))
    expect("PB2703a: a mixed train's fix-lane commit editing a file outside the R3 set lands",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 0, 0)
    branch("f-early", "PB9101: earlier fix", {"src/B.cs": "// earlier fix\n"}, 1_000_050)
    expect("PB2703b: ... its fix-lane file shared with an EARLIER in-flight fix-lane branch lands",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "f-early")
    branch("f-early2", "PB9101: earlier fix", {"src/A.cs": "// earlier fix\n"}, 1_000_050)
    expect("PB2703b': ... while its R3 commit's file shared with an EARLIER in-flight branch STOPS",
           check(repo, "origin/main", "mixed", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "f-early2")
    scratch.git("branch", "-D", "mixed")
    flipped = [dict(i, status="landed") if i["id"] == "PB9002" else i for i in items]   # as the landing's tree reads
    commits("r-self", ("PB9002: delete, and land the note", {"src/C.cs": "// deleted\n", "kb/Work/PB9002.md":
                                                             "---\nid: PB9002\nstatus: landed\n---\n"}, 1_000_400))
    expect("PB2703c: R3 work that flips its own note to landed keeps its (now terminal) ledger declaration: lands",
           check(repo, "origin/main", "r-self", cdir, flipped, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "r-self")
    commits("squashed", ("PB9001+PB9100: both kinds in one", {"src/A.cs": "// moved\n", "src/B.cs": "// fix\n"},
                         1_000_200))
    expect("PB2703d: ONE commit naming an R3 id and a fix-lane id is R3 whole: its file outside the set STOPS",
           check(repo, "origin/main", "squashed", cdir, items, rules, never, git), 1, 0)
    scratch.git("branch", "-D", "squashed")
    ledger()

    # ── PB3263: the 2026-10-18 15:28 shape. A recorded group (PB3334's engine, here PB9200) whose commits ALSO close two
    #    R3 notes no entry records (PB2222, PB2342; here PB9003, PB9004) is checked against ITS recorded set, never a
    #    set computed now from sites that predate its new files; only a file that existed at the base can stop it; and
    #    a group recorded with NO files is UNKNOWN: named in a warning, never "collides with nothing" ──
    items_e = items + [{"id": "PB9003", "status": "open", "cluster": ["PB2119"]},
                       {"id": "PB9004", "status": "open", "cluster": ["PB2119"]},
                       {"id": "PB9200", "status": "open", "cluster": []}]
    engine = {"wave": "5", "letter": "a", "notes": ["PB9200"], "files": ["src/A.cs", "src/Engine.cs"], "at": 1_000_100}
    ledger(engine)
    commits("eng", ("PB9200+PB9003+PB9004: the engine, two R3 notes closed on the way",
                    {"src/A.cs": "// engine\n", "src/Engine.cs": "// new\n", "src/Other.cs": "// new, undeclared\n"},
                    1_000_200))
    got = check(repo, "origin/main", "eng", cdir, items_e, rules, lambda ids: {"src/B.cs"}, git)
    expect("PB3263a: R3 notes the landing's own recorded group closed on the way are checked against THAT group's set, "
           "never the set computed now (which would stop on src/A.cs): lands", got, 0, 0)
    scratch.git("branch", "-D", "eng")
    commits("eng-out", ("PB9200+PB9003+PB9004: the engine, and a file nothing declared",
                        {"src/A.cs": "// engine\n", "src/B.cs": "// outside\n", "src/Other.cs": "// new\n"}, 1_000_200))
    got = check(repo, "origin/main", "eng-out", cdir, items_e, rules, lambda ids: {"src/B.cs"}, git)
    expect("PB3263b: ... a file that existed at the base and lies outside it STOPS", got, 1, 0)
    expect("PB3263b': ... naming that file only (a new file never stops) and the arm it came from, the landing's own "
           "dispatch",
           ([s for s in got[0] if "src/B.cs" in s and "src/Other.cs" not in s and "own dispatch w5a" in s
             and "member_index hole" not in s], []), 1, 0)
    ledger(dict(engine, files=[]))
    got = check(repo, "origin/main", "eng-out", cdir, items_e, rules, lambda ids: {"src/B.cs"}, git)
    expect("PB3263c: a group recorded with NO files is UNKNOWN: a warning naming it, never a stop", got, 0, 1)
    expect("PB3263c': ... the warning says UNKNOWN and names the group", ([w for w in got[1] if "UNKNOWN" in w
                                                                          and "w5a" in w], []), 1, 0)
    ledger()
    got = check(repo, "origin/main", "eng-out", cdir, items_e, rules, lambda ids: {"src/A.cs"}, git)
    expect("PB3263d: with no dispatch of the landing recorded, the set is computed now, and the stop says so",
           ([s for s in got[0] if "computed now from the site blocks of PB9003, PB9004" in s], []), 1, 0)
    scratch.git("branch", "-D", "eng-out")

    # ── PB3180:a MERGE commit is attributed only what it introduced (the files and statuses that differ from EVERY
    #    parent), never the diff its first parent sees, which carries the other parent's whole history ──
    commits("pred", ("WIP checkpoint: land PB9002 in the register", {"kb/Work/PB9002.md":
                                                                    "---\nid: PB9002\nstatus: landed\n---\n"}, 1_001_000),
            ("PB9100: fix-lane edit", {"src/B.cs": "// fixed by the predecessor\n"}, 1_001_010))
    scratch.git("checkout", "-q", "-b", "fin", "origin/main")
    scratch.git("merge", "-q", "--no-ff", "pred", "-m", "Merge pred for the review finisher", when=1_001_020)
    scratch.git("checkout", "-q", "main")
    expect("PB3180a: a branch that MERGES a predecessor (first parent main) is not charged with the predecessor's diff: "
           "its R3 status flip (no partition file) and its fix-lane code edit stay apart: lands",
           check(repo, "origin/main", "fin", cdir, items, rules, never, git), 0, 0)
    for b in ("fin", "pred"):
        scratch.git("branch", "-D", b)

    ledger(a_set)
    commits("r-mrg", ("WIP checkpoint: the R3 side", {"src/B.cs": "// r3 side\n"}, 1_001_100))
    commits("side", ("PB9100: the other side", {"src/B.cs": "// other side\n"}, 1_001_110))
    def resolve(name: str, text: str) -> None:
        scratch.git("checkout", "-q", name)
        scratch.run("merge", "-q", "--no-commit", "side")   # unchecked: the conflict is the case
        write("src/B.cs", text)
        if text != "// other side\n":
            write("src/C.cs", "// an evil-merge edit neither parent made\n")
        scratch.git("add", "-A")
        scratch.git("commit", "-qm", "PB9001: merge the other side, conflict resolved", when=1_001_120)
        scratch.git("checkout", "-q", "main")
    scratch.git("branch", "r-theirs", "r-mrg")
    resolve("r-mrg", "// resolved by hand\n")
    got = check(repo, "origin/main", "r-mrg", cdir, items, rules, never, git)
    expect("PB3180b: an R3 merge whose conflict resolution (and evil-merge edit) changes files outside its declared set "
           "STOPS", got, 1, 0)
    expect("PB3180b': ... naming the resolved file and the evil-merge file",
           ([s for s in got[0] if "src/B.cs" in s and "src/C.cs" in s], []), 1, 0)
    scratch.git("branch", "-D", "r-mrg")
    resolve("r-theirs", "// other side\n")
    expect("PB3180b'': ... while a resolution that takes the other parent's version introduces nothing (that parent's "
           "own commit is attributed by its own notes): lands",
           check(repo, "origin/main", "r-theirs", cdir, items, rules, never, git), 0, 0)
    for b in ("r-theirs", "side"):
        scratch.git("branch", "-D", b)

    base_sha = scratch.git("rev-parse", "main")
    commits("f-behind", ("PB9100: a fix-lane branch", {"src/B.cs": "// fixed\n"}, 1_001_200))
    write("src/C.cs", "// main's own R3 landing, outside PB9001's declared set\n")
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: an R3 landing on main", when=1_001_210)
    scratch.git("update-ref", "refs/remotes/origin/main", "HEAD")
    scratch.git("checkout", "-q", "f-behind")
    scratch.git("merge", "-q", "--no-ff", "origin/main", "-m", "Merge origin/main into f-behind", when=1_001_220)
    scratch.git("checkout", "-q", "main")
    expect("PB3180c: a `Merge origin/main` commit carrying main's R3 landing is not charged to the branch: lands",
           check(repo, "origin/main", "f-behind", cdir, items, rules, never, git), 0, 0)
    scratch.git("branch", "-D", "f-behind")
    scratch.git("checkout", "-q", "--orphan", "rootless")
    scratch.git("rm", "-rq", "--cached", ".")
    write("src/Root.cs", "// a root commit\n")
    scratch.git("add", "src/Root.cs")
    scratch.git("commit", "-qm", "PB9001: a root commit", when=1_001_300)
    scratch.git("checkout", "-q", "-f", "main")
    walked = commit_work(git, "origin/main", "rootless")
    expect("PB3180d: a root commit is its diff against the empty tree",
           ([] if walked == [({"PB9001"}, {"src/Root.cs"})] else [walked], []), 0, 0)
    scratch.git("branch", "-D", "rootless")
    scratch.git("reset", "-q", "--hard", base_sha)
    scratch.git("update-ref", "refs/remotes/origin/main", base_sha)
    ledger()

    ids = sorted(subject_ids("PB9000-PB9002: a range" + chr(10) + "PB9100+PB9101: two"))
    expect("N5: a subject range names every note in it",
           ([] if ids == ["PB9000", "PB9001", "PB9002", "PB9100", "PB9101"] else [ids], []), 0, 0)
    bad = [r for r in results if not r[1]]
    for label, ok, got in results:
        print(f"  {'ok ' if ok else 'BAD'} {label}" + ("" if ok else f": {got}"))
    print(f"=== LANDING CHECK SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--rev", default="HEAD")
    ap.add_argument("--base", default="origin/main")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    repo = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                                       check=True).stdout.strip())
    cdir = coord.coord_dir(a.coord)
    policy = plan_wave.IndexPolicy.of(cdir)   # the planner's policy: THE index of this tree (kb/Work PB3203)
    stops, warnings = check(repo, a.base, a.rev, cdir, work.load(repo / "kb" / "Work"),
                            coord.rules(), lambda ids: plan_wave.computed_file_set(repo, ids, policy),
                            reports=plan_wave.load_reports(coord.reports_dir(a.coord)))
    for w in warnings:
        print(f"⚠ landing check: {w}")
    for s in stops:
        print(f"⛔ landing check: {s}")
    print(f"=== LANDING CHECK: {'STOP' if stops else 'PASS'} ({len(stops)} stop(s), {len(warnings)} warning(s)) ===")
    return 1 if stops else 0


if __name__ == "__main__":
    sys.exit(main())
