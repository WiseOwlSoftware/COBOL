#!/usr/bin/env python3
"""landing_check.py — the file-set partition's GUARANTEE, checked where every landing passes
(scripts/orchestrator/enqueue.py checks, exit 3).

WHY (kb/Work PB2118 Drafts 9-10; DESIGN-architecture-review §8.7). The owner made the mechanical file-set partition the
ONLY gate between the R3 restructuring waves (cluster PB2119) and the fix lane's trains (PB2118 question 5). The
planner (plan_wave.py --cluster) admits a wave on its COMPUTED file set; that set is an ADMISSION ESTIMATE, and git's
merge is the authority on a conflict (the owner, 2026-11-02, kb/Work PB4452: "Non overlapping edits to the same file
typically will merge without conflict"). This is the second line: before a landing reaches main it first refuses a
path no landing carries (0: coord.NEVER_WORK, above all the harness's tracked `.claude/settings.local.json`, which
`git add -A` stages) and a public-API baseline that lists a symbol twice (0b: RS0025, in the landing's own tree,
kb/Work PB3694), then compares what the landing ACTUALLY changed with

  1. every OTHER in-flight branch's actual changes (inflight.inflight_file_sets: worktrees, unlanded branches, the
     dispatch ledger's declared sets), when the shared file is R3 work on either side and the two EDITS MEET: a member
     of one matches a member of the other (inflight.hold, on the units each diff touches, inflight.inflight_units;
     kb/Work PB4452). A shared file whose edits fall in different members, or a file either side changes whole (added,
     deleted, unreadable, a dispatch's declared file with no recorded members), stops nothing: git's merge combines
     them. A meeting STOPS NOTHING either (FIRST READY LANDS, owner 2026-11-02, kb/Work PB4452): the landing proceeds
     and the check names the other branch, which merges main after this one lands; a real conflict is that branch's
     finisher's (MANDATORY-PRACTICES L12). Ordering by dispatch time let an idle, unfinished branch hold a finished,
     reviewed one for hours (w20261827a).

R3 work is attributed PER COMMIT (PB2703). A train is MIXED: the lander makes one commit per cluster, and some clusters
work on R3 notes while the rest are fix lane or tooling. The landing's R3 work is the commits that work on an R3 note
(the ids leading the subject, or an R3 status the commit flips) and the partition files those commits change; a
fix-lane commit in the same train is fix lane, so its files do not make a shared file R3 (1).
A squashed commit that names both kinds is R3 whole. Each commit is charged only
with what it ITSELF introduced (commit_work, PB3180): a non-merge commit its diff against its parent; a MERGE commit
the files and the note statuses that differ from EVERY parent (its conflict resolutions and evil-merge edits) plus the
ids leading its own subject, never its first-parent diff, which carries the other parent's whole history (a merged
predecessor's commits, attributed one by one by the same walk, or main's own landings in a `Merge origin/main`).

Draft 10 (the ninth refuter's K1-K3). Every diff is read with --no-renames (inflight.changed_files), so a moved file
counts at its old path too. An in-flight branch is R3 work because its DISPATCH says so, never by its commit wording
alone (implementers commit `WIP checkpoint: …`), and it is bound to ONE dispatch (identity, kb/Work PB3186): its
fix-lane REPORT (the newest whose branch field names it) and that report's ledger group, else the ids that lead its
commit subjects or whose status it flips, else the ONE ledger group that best covers its actual diff (best_group). The
union of every ledger group its diff met made a fix-lane worktree on a file one R3 group also declared R3 work, and with
the scraped hand entries the dispatch guard once wrote (six of 303-910 files) it stopped 23 fix-lane landings on
2026-10-14/15. The ledger is read through coord.ledger_groups, which ignores those retired entries. A dispatch
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
else whose edits meet this landing's is named in a warning and never stops it (first ready lands, kb/Work PB4452).

Fix lane against fix lane is never stopped: fix-lane implementers share files by design, and the merge queue merges
them (at worst through a conflict finisher, MANDATORY-PRACTICES L12). The partition's domain is the campaign's code roots (model_rules.json `campaign`: src, tests, scripts,
.github, docs, minus exclude_dir) and only CODE files there (its extensions but .md): a document both sides edit is a
text merge the rebase resolves, and kb/Work and DEVLOG/ are every landing's bookkeeping.

Usage:
    python scripts/orchestrator/landing_check.py [--rev HEAD] [--base origin/main] [--coord DIR]
    python scripts/orchestrator/landing_check.py --self-test
Exit 0: the landing may proceed (warnings name branches whose edits meet it: they merge main after it lands). Exit 1:
STOP (each reason printed): a never-carried path or a duplicate public-API symbol, which only a new commit clears.
"""
from __future__ import annotations

import argparse
import pathlib
import re
import subprocess
import sys
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import git_batch  # noqa: E402  (scripts/: the batched git reads, one process for many objects, kb/Work PB3582)
from git_batch import Ancestry  # noqa: E402
import inflight  # noqa: E402  (inflight_file_sets: THE in-flight enumeration; hold: THE meeting of two edits)
from reports import Report, ledger_slug, load_reports  # noqa: E402  (THE fleet-report reader)
import work  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

PB = re.compile(r"\bPB\d+\b")
LEADING_IDS = re.compile(r"^((?:PB\d+[\s+,&-]*)+)", re.M)   # "PB1939+PB2004: ..." — the notes a commit works on
ID_RANGE = re.compile(r"\bPB(\d+)-PB(\d+)\b")                # "PB2478-PB2483: ..." — every id of the range
MAX_RANGE = 64
STATUS_LINE = re.compile(r"^([-+])status:[ ]*(\S*)")   # a frontmatter status line removed or added by a patch
WORK_DIR = "kb/Work/"


def _git(repo: pathlib.Path) -> Callable[..., Any]:
    def run(*args: str, cwd: Any = repo, stdin: str | None = None) -> subprocess.CompletedProcess:
        return subprocess.run(["git", "-C", str(cwd), *args], input=stdin, capture_output=True, text=True,
                              encoding="utf-8", errors="replace")
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
    """The notes whose `status` the diff `git diff <revs>` changes (flips_in, over ONE patch of the register)."""
    return flips_in(git("diff", "--no-renames", "-U0", *revs, "--", WORK_DIR).stdout)


def flips_in(patch: str) -> set[str]:
    """The notes whose `status` a -U0 patch of the register changes. ONE patch of the register, not two `git show`s per
    changed note (the ninth refuter's N1: 400 shows were 17 of the check's 24 s on a real range). A note's status
    changed when its patch removes one `status:` line and adds a different one; a note FILED there has no removed line,
    so it is not worked on there."""
    nid, was = None, {}
    for line in patch.splitlines():
        if line.startswith("diff --git "):
            stem = pathlib.PurePosixPath(line.rsplit(" b/", 1)[-1]).stem
            nid = stem if PB.fullmatch(stem) else None
        elif nid and (m := STATUS_LINE.match(line)):
            was.setdefault(nid, {})[m.group(1)] = m.group(2)
    return {n for n, s in was.items() if "-" in s and s["-"] != s.get("+")}


def work_notes(git: Callable[..., Any], graph: Ancestry, base: str, rev: str) -> set[str]:
    """The notes the LANDING `base..rev` works on: the ids that lead a commit subject (the lander re-commits one
    `PB…: …` commit per cluster) and the notes whose `status` it changes (a landing flips its note in the commit that
    lands it, CLAUDE.md rule 8). Never every id a message mentions: a design commit that names the notes it re-plans
    works on none of them. An IN-FLIGHT branch's identity never comes from here alone (identity(), K2). One COMMIT's
    notes are commit_work's, never this over `sha^..sha` (PB3180). The subjects are the graph's (`graph` excludes
    `base` and holds `rev`), never a `git log` per branch (kb/Work PB3582)."""
    return (subject_ids("\n".join(graph.subject(c) for c in graph.beyond(rev)))
            | status_flips(git, f"{base}...{rev}"))


def commit_work(repo: pathlib.Path, git: Callable[..., Any], graph: Ancestry, rev: str) -> list[tuple[set[str], set[str]]]:
    """THE per-commit attribution (PB2703, PB3180): (the notes it works on, the files it changed) for every commit
    of `base..rev`, oldest first, each charged only with what the commit ITSELF introduced. A non-merge commit is its
    diff against its one parent (a root commit's against the empty tree). A MERGE commit is what differs from EVERY
    parent — the files (combined-diff semantics, as `git diff-tree -c` lists them: its conflict resolutions and
    evil-merge edits) and the notes whose `status` it changes against each parent — plus the ids leading its OWN
    subject. Never its first-parent diff: that carries the other parent's whole history (a merged predecessor's
    commits, which this same walk already attributes one by one, or main's own landings in a `Merge origin/main`), and
    charging it to the merge smeared one commit's R3 note across other commits' files (the PB3154 false stop).
    The commits, parents and subjects are the graph's (`graph` excludes the base), and every (commit, parent) pair's
    files and register patch come from ONE `git diff-tree --stdin` each (git_batch, kb/Work PB3582): never a `log -1`
    and two diffs per commit and parent."""
    commits = list(reversed(graph.newest_first(graph.beyond(rev))))
    sides = {sha: graph.parents(sha) or (git_batch.EMPTY_TREE,) for sha in commits}
    pairs = [(sha, p) for sha in commits for p in sides[sha]]
    names = iter(git_batch.diff_names(repo, pairs, git=git))
    patches = iter(git_batch.diff_patches(repo, pairs, WORK_DIR, git=git))
    out = []
    for sha in commits:
        each = [(inflight.work_names(next(names)), flips_in(next(patches))) for _ in sides[sha]]
        notes = subject_ids(graph.subject(sha)) | set.intersection(*(f for _, f in each))
        out.append((notes, set.intersection(*(n for n, _ in each))))
    return out




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


#: A public-API baseline (Microsoft.CodeAnalysis.PublicApiAnalyzers): `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` or any
#: other `PublicAPI.<kind>.txt` anywhere under src/.
SYMBOL_BASELINE = re.compile(r"src/(?:[^/]+/)*PublicAPI\.[^/]*\.txt")


def is_symbol_baseline(path: str) -> bool:
    """Whether `path` (repository-relative, `/`-separated) is a public-API baseline."""
    return SYMBOL_BASELINE.fullmatch(path) is not None


def baseline_duplicates(files: dict[str, str]) -> list[str]:
    """The symbols the baselines `files` ({path: text}) list twice, which Microsoft.CodeAnalysis.PublicApiAnalyzers fails
    the build on as RS0025 (kb/Work PB3694): a symbol line twice in one file, or one plain line in two files of one directory
    (Shipped and Unshipped are one project's API). A `*REMOVED*X` line is a different line from the Shipped `X` it removes,
    so that pair is no finding. A line is compared without its CR; a blank line and a `#` header line are no symbol. Sorted,
    one finding per symbol and place, each naming the file(s)."""
    by_dir: dict[str, dict[str, set[str]]] = {}
    found: list[str] = []
    for path, text in sorted(files.items()):
        seen: set[str] = set()
        for line in (ln.rstrip(chr(13)) for ln in text.split(chr(10))):
            if not line or line.startswith("#"):
                continue
            if line in seen:
                found.append(f"{path}: {line} is listed twice")
            seen.add(line)
        directory = str(pathlib.PurePosixPath(path).parent)
        for line in seen:
            by_dir.setdefault(directory, {}).setdefault(line, set()).add(path)
    for owners in by_dir.values():
        for line, paths in owners.items():
            if len(paths) > 1:
                found.append(f"{' and '.join(sorted(paths))}: {line} is listed in both")
    return sorted(found)


def baseline_texts(repo: pathlib.Path, git: Callable[..., Any], tree: str) -> dict[str, str]:
    """{path: text} of every public-API baseline (is_symbol_baseline) in `tree`, from one `git ls-tree -r`
    and ONE batched blob read (git_batch.blobs), however many baselines the tree holds."""
    listed = git("ls-tree", "-r", "-z", tree).stdout.split(chr(0))
    paths = {}   # {path: blob id}: an entry reads `<mode> blob <id>\t<path>`
    for entry in filter(None, listed):
        meta, _, path = entry.partition(chr(9))
        if is_symbol_baseline(path):
            paths[path] = meta.split()[2]
    blob = dict(git_batch.blobs(repo, paths.values()))
    return {path: blob[oid].decode("utf-8", "replace") for path, oid in paths.items()}


def baseline_stops(repo: pathlib.Path, rev: str, git: Callable[..., Any]) -> list[str]:
    """The public-API baselines of the landing's own tree that list a symbol twice, or in both PublicAPI.Shipped.txt and
    PublicAPI.Unshipped.txt (kb/Work PB3694): RS0025 fails the Release build on one. Plain git keeps a symbol both sides
    added at different places twice (kb/Work PB5110: no merge driver sorts the baselines as a set any more); this reads
    what the branch's head holds, which no local test of a branch ever read.

    What it does NOT read is GitHub's merge group's own merge. That merge runs no driver (PB3642) and keeps a symbol both
    sides added at different places twice, but it merges the pushed head with the main of the moment it is made. Every
    caller presents a head that already contains origin/main (enqueue.queue refuses any other, exit 2; the landing lane
    checks from the merge base, which the head contains), so a `git merge-tree` of the two is the head's own tree and
    reads nothing. Only a main that advances after the push makes the merge differ, and no check before the push can
    read a commit that does not exist yet: CI's merge-group run is the net for it (kb/Work PB3694, Residual)."""
    tree = git("rev-parse", "--verify", "-q", f"{rev}^{{tree}}").stdout.strip()
    dups = baseline_duplicates(baseline_texts(repo, git, tree)) if tree else []
    return ([f"the landing's tree lists a public-API symbol twice, which fails the Release build (RS0025): "
             f"{'; '.join(dups[:5])}{' …' if len(dups) > 5 else ''} — keep each symbol once in the baselines "
             f"(after `git merge origin/main`, delete the second line of each), commit, and re-gate"]
            if dups else [])


def check(repo: pathlib.Path, base: str, rev: str, coord_dir: pathlib.Path, items: list[dict], rules: dict[str, Any],
          git: Callable[..., Any] | None = None, r3_lead: str = work.R3_PROGRAM,
          reports: list[Report] = ()) -> tuple[list[str], list[str]]:
    """(stops, warnings) for landing `rev` onto `base`. `reports` are the fix-lane reports (reports.load_reports),
    which bind an in-flight branch to its own dispatch first (identity)."""
    git = git or _git(repo)
    graph = Ancestry.read(repo, [rev], base)   # THE ancestry of this check: the landing, then every in-flight branch
    inside = coord.campaign(rules).partitioned   # the partition's domain: a code file under a campaign root
    r3 = {i.get("id") for i in items if r3_lead in (i.get("cluster") or [])}
    status = {i.get("id"): i.get("status") for i in items}
    terminal = lambda n: status.get(n) in work.TERMINAL_STATUSES  # noqa: E731
    touched = inflight.range_names(git, f"{base}...{rev}")   # THE one read of the range (kb/Work PB3791): arm 0 and `mine`
    mine = {p for p in inflight.work_names(touched) if inside(p)}
    notes = work_notes(git, graph, base, rev)
    # R3 work is attributed PER COMMIT (PB2703): a landing can be MIXED — a branch that merged a predecessor, a train
    # before kb/Work PB3023 — some commits R3 notes, the rest fix lane or tooling — so the R3 work is the commits that
    # work on an R3 note and the partition files THEY change, never the whole landing's diff, and a merge commit is
    # charged only with what it introduced (commit_work, PB3180). A squashed commit naming both kinds is R3 whole.
    mine_r3_files = set()
    for worked, changed in commit_work(repo, git, graph, rev):
        if worked & r3:
            mine_r3_files |= {p for p in changed if inside(p)}
    full_ledger = coord.ledger_groups(coord_dir)   # THE reader: a retired hand entry is no dispatch (PB3186)
    ledger = [g for g in full_ledger if not all(terminal(i) for i in g.get("notes", []))]
    stops, warnings = [], []
    # 0. a path no landing carries (coord.NEVER_WORK: the harness's tracked per-machine settings, the checkpoint file, the
    # gate's results). `git add -A` and `git commit -a` stage the harness's edits of .claude/settings.local.json, and a
    # landing that carried them would make every checkout whose copy is locally modified refuse `merge --ff-only` and
    # grant every session the broadened permissions (branch review R2-1 of PB3389; the same shape as round 2 of
    # kb/Work PB1563, which the DEVLOG recorded and nothing prevented).
    # (`touched` is the range's names unfiltered; `mine` is its work paths: the range is asked about once, PB3791.)
    if leaked := sorted(p for p in touched if not coord.is_work(p)):
        stops.append(f"carries {', '.join(leaked[:5])}{' …' if len(leaked) > 5 else ''}, which no landing carries "
                     f"(coord.NEVER_WORK: the harness's per-machine settings, the checkpoint file, the gate's results): "
                     f"restore each to its content on {base} (`git checkout {base} -- <path>`), commit, and re-gate")
    # 0b. a public-API baseline that lists a symbol twice (kb/Work PB3694): RS0025 fails the Release build
    stops += baseline_stops(repo, rev, git)

    meta: dict[str, dict] = {}
    # an answered dispatch (its report exists) is read through its branch, never its declared set (PB2795)
    busy = inflight.inflight_file_sets(git=git, repo=repo, coord_dir=coord_dir, meta=meta, terminal=terminal,
                                        reports=reports)
    # ONE more read for every in-flight branch git can read; one it cannot (an unborn worktree's zero HEAD) has no
    # commit beyond the base, so depends() reads it as blocking nothing, as an empty worktree
    revs = [m["rev"] for m in meta.values() if m.get("rev")]
    known = graph.names(*revs)
    graph.extend(r for r in revs if known[r])

    def contained(their_rev: str, files: set[str]) -> set[str]:
        """The files whose version on `their_rev` IS this landing's: a lander that re-committed or patched a member's
        work carries it by content, not by commit (the retired train applier, kb/Work PB3023). Both sides' blob ids from ONE
        `git cat-file --batch-check` (git_batch.object_ids)."""
        ordered = sorted(files)
        ids = git_batch.object_ids(repo, [f"{r}:{f}" for r in (rev, their_rev) for f in ordered], git=git)
        return {f for f, mine_id, their_id in zip(ordered, ids, ids[len(ordered):]) if mine_id == their_id}

    report_of: dict[str, Report] = {}            # branch / worktree key -> its NEWEST report
    for r in reports:                                      # oldest first: the last write is the newest
        report_of.update({k: r for k in (r.branch, _key(r.worktree)) if k})
    by_slug = {ledger_slug(g): g for g in full_ledger}

    def identity(m: dict, files: set[str]) -> set[str]:
        """The notes an in-flight branch works on, bound to ONE dispatch by
        the first of (kb/Work PB3186; never by its WIP commit wording alone, K2; never the union of every ledger group
        its diff meets, which made a fix-lane worktree R3 work when one overlapping group was):
          1. its REPORT (the newest whose branch field or worktree names it): that report's ledger group (its slug,
             `w<wave><letter>`), else the report's lead;
          2. its COMMIT SUBJECTS: the ids that lead them or whose status it flips;
          3. the ONE ledger group that best covers its actual diff (best_group)."""
        if rep := report_of.get(m.get("branch") or "") or report_of.get(_key(m.get("path"))):
            g = by_slug.get(rep.slug.lower())
            return set(g.get("notes", [])) if g else {rep.lead}
        if subj := work_notes(git, graph, base, m["rev"]):
            return subj
        if g := best_group(files, ledger):
            return set(g.get("notes", []))
        return set()

    def depends(their_rev: str) -> bool:
        """An in-flight branch that blocks nothing (kb/Work PB3381 section H): one with no commit beyond `base` (an
        empty worktree: its loose files are work in progress, not a dispatch's claim), or one that DESCENDS from the
        landing branch: their merge-base is a commit beyond `base`, so it merged some head of this branch (a successor,
        a review finisher, a carrier) and lands on top of it. Never one this landing merged: the caller reads that one
        by its later, uncommitted edits. Read from the graph (no git process, kb/Work PB3582): their merge-base with
        this branch lies beyond `base` exactly when the commits each has beyond `base` meet (a common ancestor beyond
        the base, so every best one descending from it is beyond it too)."""
        if graph.on_base(their_rev):
            return True
        theirs, mine_beyond = graph.beyond(their_rev), graph.beyond(rev)
        return bool(theirs & mine_beyond) and graph.sha(their_rev) not in mine_beyond

    # Who could meet this landing: the in-flight owners that share a code file with it and are R3 work on either side.
    candidates: list[tuple[str, dict, set[str], bool, set[str]]] = []
    for owner, files in sorted(busy.items()):
        m = meta.get(owner, {})
        ancestor = False
        if m.get("rev"):                                   # a worktree or an unlanded branch, by its ACTUAL diff
            if depends(m["rev"]):
                continue                                   # empty, or built on this landing: it lands on top of it
            if graph.sha(m["rev"]) in graph.beyond(rev):   # an ancestor of the landing (not on the base: depends)
                files, ancestor = set(m.get("uncommitted", set())), True   # its commits are in this landing; later edits are its own
            shared = {f for f in mine & files if inside(f)}
            if not shared:
                continue
            their_notes = identity(m, files)
            if not their_notes or their_notes & notes:     # not known to be OTHER work: its re-committed content
                shared -= contained(m["rev"], shared - set(m.get("uncommitted", set())))   # is this landing's (K3)
                if not shared:
                    continue
        else:                                              # an unanswered dispatch: its DECLARED set, started or not
            their_notes = set(m.get("notes", []))
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
        candidates.append((owner, m, shared, ancestor, their_notes))

    # THE MEETING (kb/Work PB4452): a shared FILE stops nothing. The landing's and each candidate's edits in the files they
    # share are read as the units their diffs touch (inflight.inflight_units, the planner's own reader: a dispatch's units
    # are the members its ledger entry recorded), and a candidate is NAMED (a warning, never a stop) where a member of one
    # matches a member of the other (inflight.hold); a file either side changes whole names nothing, and git's merge
    # combines the rest.
    landing = "this landing"
    flights = {o: inflight.InFlight(set(sh), members=dict(inflight.Claim.of(sh, m.get("members")).members))
               for o, m, sh, _, _ in candidates}
    unit_meta = {o: {**m, "committed": set() if anc else set(m.get("committed", ())) & sh,
                     "uncommitted": set(m.get("uncommitted", ())) & sh}
                 for o, m, sh, anc, _ in candidates}
    if candidates:
        every = set().union(*(sh for _, _, sh, _, _ in candidates))
        flights[landing] = inflight.InFlight(every)
        unit_meta[landing] = {"sha": graph.sha(rev), "base": git("merge-base", base, rev).stdout.strip() or base,
                            "committed": every}
        inflight.inflight_units(flights, unit_meta, [], git, repo)
    # FIRST READY LANDS (owner 2026-11-02, kb/Work PB4452): a branch ready to land never waits for another that edits the
    # same member, whenever either was dispatched; an unfinished branch could hold a finished, reviewed one for hours
    # (w20261827a, reviewed CLEAN, held by an idle SPLIT branch). The other branch merges main after this one lands, and a
    # real conflict is a finisher's (MANDATORY-PRACTICES L12). The meeting is named so its owner knows.
    for owner, _m, _, _, _ in candidates:
        if hit := inflight.hold(flights[landing].claim, flights[owner].claim):
            warnings.append(f"edits {hit} with {owner}: this landing proceeds (first ready lands); {owner} merges main "
                            f"after it, and a real conflict goes to its finisher")
    return stops, warnings


# ── self-test: plants a two-branch overlap, by member, in a scratch repository ──────────────────────────────────
def self_test() -> int:
    sys.path.insert(0, str(HERE.parent))
    from scratch_repo import ScratchRepo, isolate, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    isolate()   # the subject's own git (check's port) runs in this process: never on the caller's repository
    root = scratch_dir("landing-check-")
    scratch = ScratchRepo.init(root / "repo")
    repo, cdir = scratch.path, root / "coord"
    cdir.mkdir()
    git = _git(repo)

    def cs(one: str = "", two: str = "") -> str:
        """A C# file of ONE type with two members, `K.One` and `K.Two`: an edit to one touches exactly that unit."""
        return chr(10).join(["class K", "{", f"    void One() {{ /* {one} */ }}", f"    void Two() {{ /* {two} */ }}", "}", ""])

    def write(rel: str, text: str) -> None:
        scratch.write({rel: text})

    for f in ("src/A.cs", "src/B.cs", "src/C.cs", "src/D.cs"):
        write(f, cs())
    write("docs/D.md", "base" + chr(10))
    write("kb/Work/PB9002.md", "---\nid: PB9002\nstatus: open\n---\n")
    write(".claude/settings.local.json", "{}" + chr(10))
    scratch.git("add", "-A")
    scratch.git("add", "-f", ".claude/settings.local.json")   # a machine's global ignore may name it; the real one is tracked
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
    a_set = {"wave": "1", "letter": "a", "notes": ["PB9001"], "files": ["src/A.cs"], "members": {"src/A.cs": ["K.One"]},
             "at": 1_000_100}
    coord.write_json(cdir / coord.DISPATCH_LEDGER, {"groups": [
        a_set, {"wave": "1", "letter": "b", "notes": ["PB9002"], "files": ["src/C.cs"], "at": 1_000_300}]})
    results = []

    def expect(label: str, got: tuple[list[str], list[str]], stops: int, warns: int) -> None:
        ok = (len(got[0]), len(got[1])) == (stops, warns)
        results.append((label, ok, got))

    def ledger(*groups: dict) -> None:
        coord.write_json(cdir / coord.DISPATCH_LEDGER, {"groups": list(groups)})

    def land(name: str, **kw: Any) -> tuple[list[str], list[str]]:
        return check(repo, "origin/main", name, cdir, items, rules, git, **kw)

    # an R3 landing, nothing else in flight (its own dispatch entry is no other work): lands
    branch("r-ok", "PB9001: extract in set", {"src/A.cs": cs(one="moved"), "src/New.cs": cs(), "docs/D.md": "doc" + chr(10)}, 1_000_200)
    expect("an R3 landing (a new file and a doc edit) lands", land("r-ok"), 0, 0)
    scratch.git("branch", "-D", "r-ok")
    # PLANT 1: a two-branch overlap on ONE MEMBER — R3 work (dispatched at 1_000_100) and a fix-lane branch (first commit later)
    branch("r-one", "PB9001: extract", {"src/A.cs": cs(one="moved")}, 1_000_200)
    branch("f-two", "PB9100: fix", {"src/A.cs": cs(one="fixed")}, 1_000_500)
    expect("PLANT 1a: the LATER branch (the fix lane) editing the member in-flight R3 work edits PROCEEDS (first ready lands, a warning) (against the R3 "
           "branch and against its dispatch entry, which recorded the member)", land("f-two"), 0, 2)
    expect("PLANT 1b: the EARLIER branch (the R3 wave) lands too and names the branch whose edits meet it",
           land("r-one"), 0, 1)
    # PB4452: the SAME FILE, ANOTHER MEMBER, merges without conflict: nobody waits
    branch("f-other", "PB9100: fix", {"src/A.cs": cs(two="fixed")}, 1_000_500)
    expect("PB4452a: a fix-lane edit to ANOTHER MEMBER of the file in-flight R3 work edits lands (the file is shared, "
           "no member is)", land("f-other"), 0, 0)
    expect("PB4452b: ... and the R3 wave lands with no warning for it (a stop or warning needs a shared member)",
           land("r-one"), 0, 1)   # f-two still edits One
    scratch.git("branch", "-D", "f-two")
    expect("PB4452c: ... with no other branch on its member, the R3 wave lands clean", land("r-one"), 0, 0)
    ledger(dict(a_set, members={}))
    branch("f-whole", "PB9100: fix", {"src/A.cs": cs(one="fixed")}, 1_000_500)
    expect("PB4452d: a dispatch entry that recorded NO members claims the file whole and holds nothing: the fix-lane edit "
           "is named by the R3 BRANCH that edits the same member (one warning), never by the dispatch entry",
           land("f-whole"), 0, 1)
    ledger(a_set)
    scratch.git("branch", "-D", "f-whole")
    scratch.git("branch", "-D", "f-other")
    # a branch whose commits the landing contains IS the landing: a train built on the R3 branch r-one names only
    #   a later branch on its member in a warning, never r-one itself
    scratch.git("checkout", "-q", "-b", "train", "r-one")
    write("src/F.cs", cs())
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9101: fix in the train", when=1_000_700)
    scratch.git("checkout", "-q", "main")
    branch("f-two", "PB9100: fix", {"src/A.cs": cs(one="fixed")}, 1_000_500)
    expect("a branch whose commits the landing contains is not in flight against it (only f-two, later, is warned)",
           land("train"), 0, 1)
    scratch.git("branch", "-D", "train")
    # fix lane against fix lane: a train merges them (no R3 work on either side)
    scratch.git("branch", "-D", "r-one")
    items[0]["status"] = "landed"   # r-one landed: its dispatch entry is no longer in flight
    branch("f-three", "PB9101: fix", {"src/A.cs": cs(one="fixed too")}, 1_000_600)
    expect("fix-lane branches editing one member with no R3 work in flight pass", land("f-three"), 0, 0)
    # a design commit that NAMES an R3 note it does not work on is not R3 work (its subject does not lead with it)
    items[0]["status"] = "open"
    branch("design", "WIP checkpoint: re-plan PB9001 and PB9002", {"src/B.cs": cs(one="tooling")}, 1_000_750)
    expect("a commit that only mentions an R3 note is not R3 work", land("design"), 0, 0)
    scratch.git("branch", "-D", "design")
    for b in ("f-two", "f-three"):
        scratch.git("branch", "-D", b)

    # ── Draft 10: the ninth refuter's K1 (renames), K2 (identity), K3 (a landing's own members), N2, N5 ──
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

    ledger()
    moved("r-mv2", "PB9001: move", "src/A.cs", "src/A2.cs", 1_000_200)
    branch("f-mv", "PB9100: fix", {"src/A.cs": cs(one="fixed")}, 1_000_500)
    expect("K1b: a fix-lane edit to a path an in-flight R3 branch RENAMED reads the rename as the whole file (--no-renames: "
           "a deletion), and a whole claim holds nothing: lands", land("f-mv"), 0, 0)
    scratch.git("branch", "-D", "r-mv2")
    scratch.git("branch", "-D", "f-mv")

    ledger(a_set)
    worktree("wt-wip", "WIP checkpoint: extract", {"src/A.cs": cs(one="moved"), "src/B.cs": cs(one="caller")}, 1_000_200)
    branch("f-b", "PB9100: fix", {"src/B.cs": cs(one="fixed")}, 1_000_500)
    expect("K2: an R3 worktree whose commits are all `WIP checkpoint:` is R3 work by its dispatch entry: a later "
           "fix-lane edit to a member it changed OUTSIDE its declared set PROCEEDS (first ready lands, a warning)", land("f-b"), 0, 1)
    scratch.git("branch", "-D", "f-b")
    drop("wt-wip")

    worktree("wt-impl", "WIP checkpoint: extract", {"src/A.cs": cs(one="moved")}, 1_000_200)
    branch("r-train", "PB9001: extract", {"src/A.cs": cs(one="moved")}, 1_000_900)
    expect("K3a: a train that re-committed its own implementer's work (recorded dispatch) lands with no warning",
           land("r-train"), 0, 0)
    ledger()
    expect("K3a': ... and so does the same train with the dispatch unrecorded (its content is the landing's)",
           land("r-train"), 0, 0)
    scratch.git("branch", "-D", "r-train")
    branch("r-train2", "PB9001: extract", {"src/A.cs": cs(one="moved, conflict resolved")}, 1_000_900)
    got = land("r-train2")
    expect("K3b: an earlier worktree bound to no dispatch, whose content the landing changed, PROCEEDS (first ready lands, a warning)", got, 0, 1)
    scratch.git("checkout", "-q", "-b", "r-train3", "wt-impl")   # the landing MERGED its implementer's branch (a successor)
    write("src/A.cs", cs(one="moved, conflict resolved"))
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: extract, resolved", when=1_000_900)
    scratch.git("checkout", "-q", "main")
    expect("K3c: an implementer branch the landing contains is the landing's own: lands", land("r-train3"), 0, 0)
    worktree("wt-other", "WIP checkpoint: another", {"src/A.cs": cs(one="someone else")}, 1_000_150)
    expect("K3d: ... and another earlier worktree on the same member is named in a warning; the landing proceeds", land("r-train3"), 0, 1)
    drop("wt-other")
    worktree("wt-two", "WIP checkpoint: another member", {"src/A.cs": cs(two="someone else")}, 1_000_150)
    expect("PB4452e: ... while an earlier worktree on ANOTHER MEMBER of the file does not", land("r-train3"), 0, 0)
    drop("wt-two")
    scratch.git("branch", "-D", "r-train2")
    # kb/Work PB3381 section H: a successor that merged an OLDER head of the landing branch descends from it and lands
    # on top of it, so it never blocks it (wave 1118: -326-13 held PB3333's ea4ab0ec1 and stopped PB3333's reviewed
    # -326-20 three times); an empty worktree (no commit beyond main, a merge in progress) blocks nothing; an
    # independent earlier dispatch on the same member (K3d above) is named in a warning, never a stop.
    scratch.git("checkout", "-q", "-b", "succ-old", "wt-impl")   # merged the landing's OLDER head (wt-impl), then its own edit
    write("src/A.cs", cs(one="the successor's own edit on the shared member"))
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "WIP checkpoint: the successor", when=1_000_950)
    scratch.git("checkout", "-q", "main")
    scratch.git("checkout", "-q", "-b", "pred-new", "wt-impl")   # the landing: the predecessor's newer, reviewed head
    write("src/A.cs", cs(one="the predecessor's review fix"))
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: the review fix", when=1_000_960)
    scratch.git("checkout", "-q", "main")
    # the successor's group was planned with the wave, before any commit of the landing: by dispatch time it is the
    # EARLIER one, which is what stopped the predecessor before section H
    ledger({"wave": "4", "letter": "b", "notes": ["PB9100"], "files": ["src/A.cs"], "members": {"src/A.cs": ["K.One"]},
            "at": 1_000_100})
    hdir = root / "reports-h"
    hdir.mkdir()
    (hdir / "w4b-PB9100-report.md").write_text("Status: DONE" + chr(10) + "Branch: succ-old" + chr(10),
                                               encoding="utf-8", newline=chr(10))
    h_reports = load_reports(hdir)
    expect("H1: a successor holding an OLDER head of the landing branch descends from it: it does not block it",
           land("pred-new", reports=h_reports), 0, 0)
    empty = root / "wt-empty"
    scratch.git("worktree", "add", "-q", "-b", "wt-empty", str(empty), "origin/main")
    (empty / "src" / "A.cs").write_text(cs(one="a merge in progress, nothing committed"), encoding="utf-8", newline=chr(10))
    expect("H2: a worktree with no commit beyond main (its edits uncommitted) blocks nothing",
           land("pred-new", reports=h_reports), 0, 0)
    drop("wt-empty")
    worktree("wt-indep", "WIP checkpoint: independent", {"src/A.cs": cs(one="independent work")}, 1_000_150)
    expect("H3: ... while an INDEPENDENT earlier dispatch on the same member is named in a warning; the landing proceeds",
           land("pred-new", reports=h_reports), 0, 1)
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
    worktree("wt-fix", "WIP checkpoint: fix", {"src/B.cs": cs(one="fix-lane wip"), "src/C.cs": cs(one="fix-lane wip")},
             1_000_200)
    branch("f-land", "PB9101: a later fix", {"src/C.cs": cs(one="a later fix")}, 1_000_500)
    expect("PB3186a: a fix-lane worktree on a file an R3 group ALSO declares binds to the group covering its diff "
           "(PB9100): fix lane against fix lane passes", land("f-land"), 0, 0)
    drop("wt-fix")
    worktree("wt-rep", "WIP checkpoint: fix", {"src/A.cs": cs(one="wip"), "src/B.cs": cs(one="wip"),
                                                "src/D.cs": cs(one="wip")}, 1_000_200)
    branch("f-d", "PB9101: a later fix", {"src/D.cs": cs(one="a later fix")}, 1_000_500)
    expect("PB3186b: a WIP worktree whose diff the R3 group covers best is R3 work (K2): a later edit to its member PROCEEDS (first ready lands, a warning)",
           land("f-d"), 0, 1)
    rdir = root / "reports"
    rdir.mkdir(exist_ok=True)
    (rdir / "w3b-PB9100-report.md").write_text("Status: DONE" + chr(10) + "Branch: wt-rep" + chr(10), encoding="utf-8",
                                               newline=chr(10))
    expect("PB3186c: ... but its REPORT binds it first, to its own group (w3b, fix lane): passes",
           land("f-d", reports=load_reports(rdir)), 0, 0)
    drop("wt-rep")
    scratch.git("branch", "-D", "f-d")
    ledger({"wave": "hand", "letter": "", "hand": True, "notes": ["PB9001"], "files": ["src/C.cs"],
            "members": {"src/C.cs": ["K.One"]}, "at": 1_000_000},
           {"wave": "x", "letter": "a", "notes": ["PB9002"], "files": ["src/C.cs"], "members": {"src/C.cs": ["K.One"]},
            "at": 1_000_000})
    expect("PB3186d: a retired hand entry and a non-numeric wave are no dispatch: a fix-lane landing on their files passes",
           land("f-land"), 0, 0)
    scratch.git("branch", "-D", "f-land")

    ledger(dict(a_set, files=["src/A.cs", "src/C.cs"], members={"src/A.cs": ["K.One"], "src/C.cs": ["K.One"]}))
    worktree("wt-start", "WIP checkpoint: part 1", {"src/A.cs": cs(one="moved")}, 1_000_200)
    branch("f-c", "PB9100: fix", {"src/C.cs": cs(one="fixed")}, 1_000_500)
    expect("N2: a STARTED R3 wave keeps its untouched declared members: a later fix-lane edit to one PROCEEDS (first ready lands, a warning)",
           land("f-c"), 0, 1)
    ledger(dict(a_set, files=["src/A.cs", "src/C.cs"], members={}))
    expect("PB4452f: ... but a declared file the dispatch recorded no members for is claimed whole, and holds nothing",
           land("f-c"), 0, 0)
    ledger(dict(a_set, files=["src/A.cs", "src/C.cs"], members={"src/A.cs": ["K.One"], "src/C.cs": ["K.One"]}))
    (rdir / "w1a-PB9001-report.md").write_text("Status: SPLIT" + chr(10), encoding="utf-8", newline=chr(10))
    expect("PB2795: ... until its agent RETURNS: a group its report answers (SPLIT residue included) is read through "
           "its branch, so its untouched declared files are free", land("f-c", reports=load_reports(rdir)), 0, 0)
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
    commits("mixed", ("PB9001: extract in set", {"src/A.cs": cs(one="moved")}, 1_000_200),
            ("PB9100: fix-lane cluster", {"src/B.cs": cs(one="fixed")}, 1_000_250))
    branch("f-early", "PB9101: earlier fix", {"src/B.cs": cs(one="earlier fix")}, 1_000_050)
    expect("PB2703a: a mixed train's fix-lane file shared with an EARLIER in-flight fix-lane branch lands",
           land("mixed"), 0, 0)
    scratch.git("branch", "-D", "f-early")
    branch("f-early2", "PB9101: earlier fix", {"src/A.cs": cs(one="earlier fix")}, 1_000_050)
    expect("PB2703b: ... while its R3 commit's member shared with an EARLIER in-flight branch PROCEEDS (first ready lands, a warning)", land("mixed"), 0, 1)
    scratch.git("branch", "-D", "f-early2")
    scratch.git("branch", "-D", "mixed")
    flipped = [dict(i, status="landed") if i["id"] == "PB9002" else i for i in items]   # as the landing's tree reads
    commits("r-self", ("PB9002: delete, and land the note", {"src/C.cs": cs(one="deleted"), "kb/Work/PB9002.md":
                                                             "---\nid: PB9002\nstatus: landed\n---\n"}, 1_000_400))
    expect("PB2703c: R3 work that flips its own note to landed keeps its (now terminal) ledger entry as its own: lands",
           check(repo, "origin/main", "r-self", cdir, flipped, rules, git), 0, 0)
    scratch.git("branch", "-D", "r-self")
    commits("squashed", ("PB9001+PB9100: both kinds in one", {"src/A.cs": cs(one="moved"), "src/B.cs": cs(one="fix")},
                         1_000_200))
    branch("f-early3", "PB9101: earlier fix", {"src/B.cs": cs(one="earlier fix")}, 1_000_050)
    expect("PB2703d: ONE commit naming an R3 id and a fix-lane id is R3 whole: its fix-lane file's member shared with an "
           "EARLIER in-flight fix-lane branch PROCEEDS (first ready lands, a warning)", land("squashed"), 0, 1)
    scratch.git("branch", "-D", "f-early3")
    scratch.git("branch", "-D", "squashed")
    ledger()

    # ── PB3180:a MERGE commit is attributed only what it introduced (the files and statuses that differ from EVERY
    #    parent), never the diff its first parent sees, which carries the other parent's whole history ──
    commits("pred", ("WIP checkpoint: land PB9002 in the register", {"kb/Work/PB9002.md":
                                                                    "---\nid: PB9002\nstatus: landed\n---\n"}, 1_001_000),
            ("PB9100: fix-lane edit", {"src/B.cs": cs(one="fixed by the predecessor")}, 1_001_010))
    scratch.git("checkout", "-q", "-b", "fin", "origin/main")
    scratch.git("merge", "-q", "--no-ff", "pred", "-m", "Merge pred for the review finisher", when=1_001_020)
    scratch.git("checkout", "-q", "main")
    branch("f-early4", "PB9101: earlier fix", {"src/B.cs": cs(one="earlier fix")}, 1_000_050)
    expect("PB3180a: a branch that MERGES a predecessor (first parent main) is not charged with the predecessor's diff: "
           "its R3 status flip (no partition file) and its fix-lane code edit stay apart, so an earlier fix-lane branch "
           "on that member is no R3 meeting: lands", land("fin"), 0, 0)
    for b in ("fin", "pred", "f-early4"):
        scratch.git("branch", "-D", b)

    ledger(a_set)
    commits("r-mrg", ("WIP checkpoint: the R3 side", {"src/B.cs": cs(one="r3 side")}, 1_001_100))
    commits("side", ("PB9100: the other side", {"src/B.cs": cs(one="other side")}, 1_001_110))

    def resolve(name: str, text: str) -> None:
        scratch.git("checkout", "-q", name)
        scratch.run("merge", "-q", "--no-commit", "side")   # unchecked: the conflict is the case
        write("src/B.cs", text)
        if text != cs(one="other side"):
            write("src/C.cs", cs(one="an evil-merge edit neither parent made"))
        scratch.git("add", "-A")
        scratch.git("commit", "-qm", "PB9001: merge the other side, conflict resolved", when=1_001_120)
        scratch.git("checkout", "-q", "main")
    scratch.git("branch", "r-theirs", "r-mrg")
    resolve("r-mrg", cs(one="resolved by hand"))
    branch("f-bc", "PB9101: earlier fix", {"src/B.cs": cs(one="earlier fix"), "src/C.cs": cs(one="earlier fix")}, 1_000_050)
    got = land("r-mrg")
    expect("PB3180b: an R3 merge whose conflict resolution (and evil-merge edit) changes members an EARLIER in-flight branch "
           "edits PROCEEDS (first ready lands, a warning)", got, 0, 1)
    scratch.git("branch", "-D", "r-mrg")
    resolve("r-theirs", cs(one="other side"))
    expect("PB3180b': ... while a resolution that takes the other parent's version introduces nothing (that parent's "
           "own commit is attributed by its own notes): lands", land("r-theirs"), 0, 0)
    for b in ("r-theirs", "side", "f-bc"):
        scratch.git("branch", "-D", b)

    # R2-1 of PB3389: a landing that changes a NEVER_WORK path (the harness edits the tracked settings file in every
    # worktree) STOPS, whatever else it carries; the same fix without it lands
    ledger()
    branch("s-set", "PB9100: a fix that also committed the harness's settings edit",
           {"src/B.cs": cs(one="fixed"), ".claude/settings.local.json": '{"permissions": {}}' + chr(10)}, 1_000_950)
    got = land("s-set")
    expect("a landing that carries .claude/settings.local.json STOPS, naming it",
           ([s for s in got[0] if ".claude/settings.local.json" in s and "NEVER_WORK" in s], got[1]), 1, 0)
    scratch.git("branch", "-D", "s-set")
    branch("s-clean", "PB9100: the same fix without it", {"src/B.cs": cs(one="fixed")}, 1_000_960)
    expect("... and the same fix without it lands", land("s-clean"), 0, 0)
    scratch.git("branch", "-D", "s-clean")
    # kb/Work PB3791: `check` asks git for the range's names ONCE, whether the landing carries a NEVER_WORK path (arm 0 reads
    # the names unfiltered) or not (`mine` reads them filtered); a second `git diff --name-only` of the range returning
    # fails this arm
    for name, edits in (("s-count-set", {"src/B.cs": cs(one="fixed"), ".claude/settings.local.json": "{}" + chr(10)}),
                        ("s-count-clean", {"src/B.cs": cs(one="fixed")})):
        branch(name, "PB9100: a fix", edits, 1_000_970)
        asked: list[tuple[str, ...]] = []

        def counting(*args: str, **kw: Any) -> Any:
            asked.append(args)
            return git(*args, **kw)
        check(repo, "origin/main", name, cdir, items, rules, counting)
        reads = [a for a in asked if a[:1] == ("diff",) and "--name-only" in a and f"origin/main...{name}" in a]
        expect(f"PB3791: the range of a landing {'carrying a NEVER_WORK path ' if 'set' in name else ''}is asked about once",
               ([] if len(reads) == 1 else [f"{len(reads)} reads: {reads}"], []), 0, 0)
        scratch.git("branch", "-D", name)

    # kb/Work PB3694: a public-API baseline that lists a symbol twice fails the Release build (RS0025). The landing's own tree
    # lists it twice, or in both Shipped and Unshipped. (The merge GitHub's merge group makes of a head and a main that moved
    # after the push is not readable before the push: baseline_stops says so.) Its own repository, so main's commits do not
    # move the cases above.
    apis = ScratchRepo.init(root / "apis")
    shipped, unshipped = "src/Lib/PublicAPI.Shipped.txt", "src/Lib/PublicAPI.Unshipped.txt"

    def api(*symbols: str) -> str:
        return "".join(line + chr(10) for line in ("#nullable enable", *symbols))
    apis.commit("base", {shipped: api("A.A", "B.B"), unshipped: api("C.C", "E.E")}, when=1_002_000)
    apis.git("update-ref", "refs/remotes/origin/main", "HEAD")
    for name, edits in (("dup", {unshipped: api("C.C", "E.E", "E.E")}),
                        ("both", {unshipped: api("A.A", "C.C", "E.E")}),
                        ("removed", {unshipped: api("*REMOVED*B.B", "C.C", "E.E", "Z.Z")})):
        apis.git("checkout", "-q", "-b", name, "origin/main")
        apis.commit(f"a baseline edit: {name}", edits, when=1_002_100)
        apis.git("checkout", "-q", "main")
    empty = root / "coord-apis"
    empty.mkdir()
    apis_git = _git(apis.path)

    def baselines(name: str, needle: str = "") -> list[str]:
        """The landing's stops; with `needle`, the one stop that holds it ([] when it is not the only stop)."""
        stops = check(apis.path, "origin/main", name, empty, [], rules, apis_git)[0]
        return [s for s in stops if needle in s] if not needle or len(stops) == 1 else [*stops, "(more than one stop)"]
    expect("PB3694: a baseline that lists a symbol twice STOPS, naming the file and the symbol",
           (baselines("dup", f"(RS0025): {unshipped}: E.E is listed twice"), []), 1, 0)
    expect("PB3694: a symbol in both PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt STOPS",
           (baselines("both", f"{shipped} and {unshipped}: A.A is listed in both"), []), 1, 0)
    expect("PB3694: a `*REMOVED*` line beside the Shipped symbol it removes is no duplicate: lands",
           (baselines("removed"), []), 0, 0)
    base_sha = scratch.git("rev-parse", "main")
    commits("f-behind", ("PB9100: a fix-lane branch", {"src/B.cs": cs(one="fixed")}, 1_001_200))
    write("src/C.cs", cs(one="main's own R3 landing"))
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "PB9001: an R3 landing on main", when=1_001_210)
    scratch.git("update-ref", "refs/remotes/origin/main", "HEAD")
    scratch.git("checkout", "-q", "f-behind")
    scratch.git("merge", "-q", "--no-ff", "origin/main", "-m", "Merge origin/main into f-behind", when=1_001_220)
    scratch.git("checkout", "-q", "main")
    walked = commit_work(repo, git, Ancestry.read(repo, ["f-behind"], "origin/main"), "f-behind")
    expect("PB3180c: a `Merge origin/main` commit carrying main's R3 landing is not charged to the branch: no commit of it "
           "works on an R3 note or carries main's file",
           ([] if all(not (n & {"PB9001"}) and "src/C.cs" not in fs for n, fs in walked) else [walked], []), 0, 0)
    scratch.git("branch", "-D", "f-behind")
    scratch.git("checkout", "-q", "--orphan", "rootless")
    scratch.git("rm", "-rq", "--cached", ".")
    write("src/Root.cs", "// a root commit" + chr(10))
    scratch.git("add", "src/Root.cs")
    scratch.git("commit", "-qm", "PB9001: a root commit", when=1_001_300)
    scratch.git("checkout", "-q", "-f", "main")
    walked = commit_work(repo, git, Ancestry.read(repo, ["rootless"], "origin/main"), "rootless")
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
    if a.self_test:
        return self_test()
    repo = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                                       check=True).stdout.strip())
    cdir = coord.coord_dir(a.coord)
    stops, warnings = check(repo, a.base, a.rev, cdir, work.load(repo / "kb" / "Work"),
                            coord.rules(), reports=load_reports(coord.reports_dir(a.coord)))
    for w in warnings:
        print(f"⚠ landing check: {w}")
    for s in stops:
        print(f"⛔ landing check: {s}")
    print(f"=== LANDING CHECK: {'STOP' if stops else 'PASS'} ({len(stops)} stop(s), {len(warnings)} warning(s)) ===")
    return 1 if stops else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
