#!/usr/bin/env python3
"""Every local branch and worktree, by CONTENT against origin/main and by LIFECYCLE (who acts on it next); with --apply,
remove the ones whose work is on main, and the ones abandoned by a recorded decision.

    python scripts/prune_worktrees.py                      # dry run: the lifecycle table, nothing touched
    python scripts/prune_worktrees.py --brief              # the verdict line and only the rows someone must act on
    python scripts/prune_worktrees.py --json               # the same survey as JSON (the operator tick reads it)
    python scripts/prune_worktrees.py --apply [--abandon <branch> ...]
    python scripts/prune_worktrees.py --self-test          # a scratch repository with one branch in every state

⛔ THE OWNER'S RULE (2026-10-08 00:00 PDT, kb/Work PB2600): "We should never have unused branches left around. We use a
branch to test a change. If the change is accepted, we eventually merge the branch to main and delete the branch. If the
change is not accepted, we either fix it, then merge, or decide it's not the right solution and typically delete the
branch if we decide to abandon that approach." So every branch is in exactly one LIFECYCLE state, and each state names
who acts next:

  IN FLIGHT        a live dispatch owns it: its worktree is locked by an agent whose process is alive (the harness
                   writes `claude agent <id> (pid N)`; a lock with no pid was placed by hand and counts as live), a
                   dispatch-ledger wave group still in flight names it (`<coord>/inflight-groups.json`, the planner's
                   ledger, read through plan_wave.inflight_file_sets; the branch name starts with the group's slug
                   `w<wave><letter>`), or it was touched in the last --min-age minutes (its HEAD reflog or STATUS.md).
                   Nobody touches it.
                   A branch with NO clean stamp at its tip (no report, a FINDINGS stamp, or a finished report WAITING
                   FOR REVIEW) whose tip is contained in a fix-lane branch that is IN FLIGHT, waiting or STOPPED, or whose
                   own review ended FINDINGS (finisher_due: the planner plans its finisher, kb/Work PB3267) (a
                   branch a review finisher merged, an unfinished or unreviewed predecessor; contained()) is IN FLIGHT
                   too: an unreviewed checkpoint never lands alone, it lands through that branch (its row names it,
                   `carrier`), and reads LANDED once it has. A branch reviewed CLEAN at its tip keeps its own
                   lifecycle, contained or not: a same-file predecessor reviewed CLEAN is WAITING TO LAND and lands at
                   once (kb/Work PB3381). A WAITING TO LAND branch that carries ANOTHER note's unanswered FINDINGS is
                   IN FLIGHT, waiting on that branch (`waits_on`, carrying_findings(): read from the findings file's
                   last verdict, review_findings.last_verdict, never from a report's stamp; a verdict that is not the round
                   after the last review finisher is the successor's own review's, which releases the branch once it is
                   CLEAN over that head: review_answers, kb/Work PB3404), and so is every WAITING TO
                   LAND branch a TORN findings file names or that contains a head it names. Only the first kind has a live
                   owner of its own (owned(): what the planner's FINDINGS arm waits on; locked(): its lock-only half, a
                   live agent holding the worktree, which the planner's finisher units wait on, kb/Work PB3653).
                   A branch is landed ONCE (kb/Work PB3693, carrying_landing_branch()): a WAITING TO LAND branch whose
                   predecessor is itself WAITING TO LAND or ENQUEUED is IN FLIGHT waiting on it (`waits_on`) until that
                   pull request is on main, and a WAITING TO LAND branch inside an ENQUEUED one lands through it
                   (`carrier`).
                   An UNFINISHED (SPLIT, NOT STARTED) branch another branch carries is IN FLIGHT too, not a decision
                   (kb/Work PB3652): contained in a branch above, or REPLAYED by one (every commit of it is held, by
                   cherry-pick or identity, by an outranking branch nobody carries; replay_view, the planner's own
                   rule, so the survey and the planner never disagree on WHO carries whom).
  A FINISHED BRANCH is decided by ONE rule of three coverages, never by a report's age (kb/Work PB3181, PB3125):
    1. its newest fix-lane report (`w<wave><letter>-PB<lead>-report.md`, coord.py REPORT_NAME, under
       coord.reports_dir()) is FINISHED (plan_wave.Report.finished: `Status: DONE`, or DISCHARGED with a HEAD past its
       base) and COVERS the tip: its HEAD is the tip, or every commit after it is mechanical (main merged in cleanly,
       the DEVLOG stamp: `unreviewed_since`, the one coverage rule the review stamp and `enqueue.py land` read). The
       report's HEAD is its header's HEAD field, else the head its review stamp names (plan_wave.parse_report: the
       reviewer stamps the head it reviewed, kb/Work PB3476). A report naming neither, or followed by a commit no
       report describes (an agent resumed after writing it), covers nothing: DECISION, with the count;
    2. its header's review stamp COVERS the tip (`stamp_covers`, the one rule the planner's FINDINGS arm reads too,
       kb/Work PB3394): the stamp names the tip, or an older head the same coverage reaches; CLEAN and FINDINGS alike;
    3. the landing lane's newest attempt AT THE TIP (land_lane.view: its head, or the end head its run left).
  WAITING FOR REVIEW  coverage 1 holds, but no review stamp covers the tip (none, or one at an older head followed by
                   commits that are not mechanical; plan_wave.REVIEW, written by review_findings.py stamp): ONE
                   read-only cobol-reviewer reviews it (the `review` skill's branch form, kb/Work PB3027; a diff-only
                   re-review of the commits since an older stamp, PB3381). The landing step never reviews and never
                   fixes a finding. A FINDINGS stamp at an older head is unreviewed work too, never a finisher's: the
                   planner reads the same coverage (kb/Work PB3394).
  WAITING TO LAND  coverages 1 and 2 hold and no attempt stopped at this tip (none ran here, one is running, or its exit
                   is one the lane retries by itself: 3 once landing_check.py passes, 2 or 9 once, 8 after the halt; or
                   the enqueue unit re-armed it): the loop's landing lane runs `enqueue.py land <branch>` on it
                   (land_lane.py, kb/Work PB3103, PB3023), one branch at a time, the most harmful lead first (`--brief`
                   lists them in that order, kb/Work PB2981's ranking). (A stamp that says FINDINGS and covers the tip is
                   a DECISION row: the next wave's finisher answers them, plan_wave.py.)
  STOPPED          coverages 1 and 2 hold and the lane's attempt at this tip STOPPED (land_lane's class: a conflict 4, a
                   red gate 5, a DEVLOG refusal 6, UNREVIEWED 11, UNTESTED 14, a repeated exit 2 or 9, a runner that DIED twice): the
                   lane never relaunches it at this head; the enqueue unit sends it to its finisher (or reviewer), whose
                   new tip is reviewed and landed, or re-arms a stop resolved with no commit. The evidence names the
                   attempt and land_lane.ACTION's owner. A branch it contains with no clean stamp at its tip lands
                   through it (IN FLIGHT).
  ENQUEUED         its pull request is IN GitHub's merge queue (the queue's own entries name it): the queue tests it in a
                   merge group and lands it (kb/Work PB3024); offline, the lane's exit-0 attempt at the tip says the
                   same. Nobody touches it.
  NOT QUEUED       its pull request is open at the tip and was handed to the queue (auto-merge on, or the lane's attempt
                   at the tip ended ENQUEUED) but is NOT in it: GitHub queues it only once its required checks pass, and
                   on 2026-10-22 PR #385 waited 50 minutes behind a check-run that never resolved (kb/Work PB3527). The
                   row says how long it has waited, what GitHub says (the merge state, each required check-run and, while
                   one is unresolved, its job's own status) and the landing lane's QUEUE WATCH (land_lane.py), which
                   re-runs a stuck check, then closes, reopens and re-queues the pull request, then alerts the operator.
                   The first minutes after an enqueue read NOT QUEUED too, while the pull request's own run is still
                   running: the elapsed time and the watch say which it is. So does an exit-0 attempt at the tip whose
                   pull request is no longer open while its work is not on main (closed without merging), whenever
                   GitHub was observed; unobserved, the lane's record reads ENQUEUED. Auto-merge on at an older head than
                   the tip (a finisher's fix committed since) is not a hand-off of the tip, whatever that head's health:
                   one that conflicts with main, failed a required check or was removed promises nothing, and a healthy
                   one promises only its own commits. That pull request is the old landing's, and the branch reads by
                   its report again (kb/Work PB3275, PB3628).
  BOUNCED          its pull request is open but the queue REMOVED it (a red merge group, a conflict, a timeout: the
                   reason is GitHub's), or a REQUIRED check FAILED on its head before the queue took it (kb/Work PB3096,
                   PB3527: it can never enter the queue at that head), or it CONFLICTS with main (merge state DIRTY:
                   no re-run or reopen clears it), and its head is still the branch's tip and no commit was pushed
                   after the removal (the head is read at the removal, not by headRefOid alone, which a push moves,
                   kb/Work PB3275): the enqueue unit dispatches ONE finisher
                   with the reason named. A finisher that fixes the branch returns DONE and lands nothing: its new tip
                   is reviewed again, then the landing lane lands it. A bounce resolved with no commit (infrastructure,
                   a red already on main) is re-armed by the enqueue unit (`land_lane.py rearm`, kb/Work PB3103), and
                   the lane lands it again at the same head. A tip that moved past the bounced head is the fix, and
                   reads by its report again.
                   The pull-request states come from ONE reader of GitHub's facts (scripts/orchestrator/queue_facts.py:
                   one GraphQL read, the merge queue's entries included, plus the jobs API for an unresolved required
                   check-run); offline, unauthenticated or with no gh it is a non-observation and the lifecycle is
                   git-only (never a claim that nothing is queued).
  LANDED           its work is on main by content (MERGED or LANDED below) and its worktree holds nothing uncommitted:
                   `--apply` deletes it.
  DECISION         everything else: a person or the operator decides — land it (via a finisher when SPLIT), or ABANDON it
                   with the decision recorded in its kb/Work note — and then it is deleted. Each row prints its evidence.

The CONTENT class (why this exists, 2026-09-30): `git merge-base --is-ancestor` clears only branches whose commits are
literally in main. A train re-applies, rebases and conflict-resolves each cluster's commits, so a LANDED cluster's branch
is never an ancestor and 40+ branches and 19 worktrees piled up from earlier waves. The test that does decide it is
content, and it is derived on each run (a merge-queue landing is a merge commit, so from PB3023 on a branch's
commits are ancestors of main and MERGED decides; the rest stays for the branches trains landed before it):

  MERGED   the branch tip is an ancestor of origin/main.
  LANDED   not an ancestor, but MERGING it into origin/main would change nothing: `git merge-tree --write-tree
           origin/main <branch>`, through the merge drivers the landing merges with (merge_drivers.merged_tree, kb/Work
           PB3713), merges cleanly to origin/main's own tree. That is the one proof (train 1039b review):
           the earlier test (every added file and every declared C# name on main) called a branch that only MODIFIED
           existing files LANDED, so an unlanded method-body fix would have been deleted.
           Also LANDED when its own merge would change main but its work reached main as OTHER commits (carried(), kb/Work
           PB2708): a commit of the retired train applier on origin/main brought in exactly its head (a lander that edits a cluster in
           the train changes the merge), or every one of its commits is patch-equivalent to one on main (`git cherry`, a
           finisher's cherry-pick) and it declares no name main lacks. The row's evidence says which.
  CHECK    every added file exists, but merging it would still change main: an unlanded fix to existing files, a
           train's re-resolved landing, a rename or a deliberate redesign (the PB1683 derived test filter was replaced
           on 2026-09-28) all look alike. The C# names it declared that main lacks are listed as evidence: read them,
           and the branch's kb/Work notes, before removing.
  UNLANDED an added file is absent from origin/main: preserved work that never landed (PB1210's golden, found this way).
           Never removed.

--apply unlocks and removes the worktree, then deletes the branch, of exactly the LANDED rows: never an IN FLIGHT branch,
never a worktree with an uncommitted edit or an untracked file, never UNLANDED or CHECK (owner 2026-10-07: "I do not want
to accidentally lose useful work"). The classification IS that protection; nothing is archived (owner 2026-10-08 00:52
PDT: "Archiving to a bundle serves no use and wastes disk space"). A branch that is not landed is deleted only when it
is ABANDONED: `--apply --abandon <branch>` removes a clean, idle DECISION row only when a kb/Work note already records
the decision (a line where the word ABANDONED directly follows the branch's name, with the reason; the word abandons
that branch, never a successor the line names). Each removed branch's `origin/<branch>`
goes with it unless it holds commits the local branch did not (drop_remote); --apply first drops the remote-tracking refs whose
branch origin no longer holds (prune_stale_remote_refs, kb/Work PB3409). --include-locked also takes a landed
worktree locked by hand (no pid). A DETACHED worktree (a read-only fleet's pin)
has no branch: it is listed with its lifecycle and never removed here. On Windows a worktree can refuse to delete
("Invalid argument", "Access is denied") because the C# language server (csharp-ls) loaded an analyzer DLL from it:
stop csharp-ls and re-run.
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime as dt
import json
import os
import pathlib
import re
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from typing import TYPE_CHECKING, Callable, Iterable

REPO = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / 'scripts' / 'orchestrator'))
import coord  # noqa: E402  (the coordination directory, the live-process probe)
import git_batch  # noqa: E402  (scripts/: THE batched git reads, one process for many objects, kb/Work PB3582)
import merge_drivers  # noqa: E402  (scripts/: the merge drivers, THE definition of a clean merge of main, kb/Work PB3610)
from git_batch import Ancestry  # noqa: E402
import queue_facts  # noqa: E402  (THE reader of GitHub's facts about the open pull requests and the merge queue)
from queue_facts import PullRequest  # noqa: E402
if TYPE_CHECKING:
    from plan_wave import Review  # noqa: E402  (imported lazily at run time: plan_wave imports this module back)
    from land_lane import Lane  # noqa: E402  (likewise: land_lane imports this module back)

DECL = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|override|readonly|async|virtual|unsafe|new)\s+)*'
                  r'(?:class|record|struct|enum|interface)\s+(?:struct\s+)?(\w+)')
METH = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|override|readonly|async|virtual|unsafe)\s+)+'
                  r'[\w<>\[\],.?()\s]+?\s+(\w+)\s*\(')
TEST = re.compile(r'^\+\s*public (?:async )?(?:void|Task)\s+(\w+)\s*\(')
LANDED_CONTENT = ('MERGED', 'LANDED')
STATES = ('IN FLIGHT', 'WAITING FOR REVIEW', 'WAITING TO LAND', 'STOPPED', 'ENQUEUED', 'NOT QUEUED', 'BOUNCED', 'LANDED',
          'DECISION')
LOCK_PID = re.compile(r'\(pid (\d+)\)')
#: live_survey's default `prs`: read the pull requests here (a caller that already read them passes its read, None
#: included: an unobserved read is not re-read into a different answer on the same pass).
UNREAD = object()


def git(*args, cwd=REPO, stdin: str | None = None):
    return subprocess.run(['git', '-C', str(cwd), *args], input=stdin, capture_output=True, text=True, encoding='utf-8',
                          errors='replace')


def absent_from(ref: str, paths: list[str], repo=REPO) -> list[str]:
    """The paths `ref` does not hold, from ONE `git cat-file --batch-check` (one output line per input line, `<ref>:<path>
    missing` for an absent one). One `cat-file -e` per path spawned a process per added file: a branch that splits the
    DEVLOG into 1,984 entry files (kb/Work PB3025) held the whole survey past five minutes (kb/Work PB3027, found there)."""
    if not paths:
        return []
    out = git('cat-file', '--batch-check', cwd=repo, stdin=''.join(f'{ref}:{p}\n' for p in paths)).stdout.splitlines()
    if len(out) != len(paths):
        raise RuntimeError(f'git cat-file --batch-check answered {len(out)} lines for {len(paths)} paths')
    return [p for p, line in zip(paths, out) if line.endswith(' missing')]


def main_names(repo=REPO):
    out = git('grep', '-h', '-I', '-E', '-o', r'[A-Za-z_][A-Za-z0-9_]{3,}', 'origin/main', '--', 'src', 'tests', 'scripts', cwd=repo).stdout
    return set(out.split())


def merges_to_main_unchanged(branch, graph: Ancestry, repo=REPO) -> bool:
    """Would merging `branch` into origin/main leave main's tree exactly as it is? THE MERGE IS THE LANDING'S OWN,
    through the merge drivers (merge_drivers.merged_tree; kb/Work PB3713): a keyed catalogue another carrier already put
    the branch's entry into merges by key, where a plain `git merge-tree` conflicts and the branch never read LANDED. Any
    git failure (unrelated histories, a conflict a driver declines, a missing ref) is a NO: an unproven landing is
    never LANDED. Main's tree is the graph's, read once a pass."""
    tree = graph.base_tree()
    return bool(tree) and merge_drivers.merged_tree(repo, ['origin/main', branch]) == tree


def uncommitted(path) -> int:
    """The worktree's uncommitted paths that are work (coord.NEVER_WORK aside, kb/Work PB3465). A status git cannot
    produce (a corrupt index, a dubious-ownership refusal) counts as ONE dirty path: an unmeasured worktree is never
    removed (train 1039b review)."""
    r = git('status', '--porcelain', '--', '.', *coord.NEVER_WORK_PATHSPECS, cwd=path)
    if r.returncode != 0:
        return 1
    return len([l for l in r.stdout.splitlines() if l.strip()])


# A TRAIN CARRIED IT (kb/Work PB2708). A lander that edits a cluster in the train makes the branch's own merge change
# main, so the merge test alone reads CHECK: after train 1042b landed, three of its members read WAITING TO LAND (the next
# land unit would have re-carried them) and seven read DECISION; after train 1053, branch-records-r1716 (53Y) read
# WAITING TO LAND. The record of what a train took is its MANIFEST (`<coord>/scratch/train<label>-manifest.json`: per
# cluster its branch, base and head) plus, on origin/main, the train's commits: the retired train applier gave each
# cluster's commit a `Train-Cluster: <cluster>` trailer, and the train ends in a `Train <label>: ...` commit. A member whose
# trailer commit is in its train's range (train_clusters: back from the train's last commit to the member's base or to
# another train's commit, whichever is nearer, since trains reuse cluster letters) was brought in, its diff is in main's
# history, and deleting the branch loses nothing even where a later commit withdrew the fix. The commit BODY is not the
# record: landers rewrite it (train 1053's 53A and 53G kept only the trailer). A member with no trailer commit (a train
# before the train applier, or a lander that dropped the trailer: train 1053's 53P) stays a hand decision.
_trains: dict[tuple[str, str], dict[str, list[tuple[str, str]]]] = {}


def train_heads(repo=REPO, coord_dir: pathlib.Path | None = None) -> dict[str, list[tuple[str, str]]]:
    """{branch: [(the head a LANDED train took, the evidence)]}: the members of every train manifest whose train's
    `Train <label>:` commit is on origin/main and whose `Train-Cluster:` trailer commit is in that train's range. Read
    once per repository and coordination directory per process."""
    cdir = pathlib.Path(coord_dir) if coord_dir else coord.coord_path()
    key = (str(repo), str(cdir))
    if key in _trains:
        return _trains[key]
    final: dict[str, str] = {}             # train label -> its newest `Train <label>:` commit on origin/main
    for line in git('log', 'origin/main', '--format=%H %s', cwd=repo).stdout.splitlines():
        sha, _, subject = line.partition(' ')
        if (m := re.match(r'Train (\S+?):', subject)) and m.group(1) not in final:
            final[m.group(1)] = sha
    heads: dict[str, list[tuple[str, str]]] = {}
    ranges: dict[tuple[str, str], dict[str, str]] = {}
    for f in sorted((cdir / 'scratch').glob('train*-manifest.json')) if (cdir / 'scratch').is_dir() else ():
        label = f.name[len('train'):-len('-manifest.json')]
        if label not in final:
            continue
        try:
            members = coord.read_json(f, []) or []
        except ValueError:
            continue
        for m in members if isinstance(members, list) else ():
            b, h, c, base = (str(m.get(k) or '') for k in ('branch', 'head', 'cluster', 'base')) if isinstance(m, dict) else ('',) * 4
            if not (b and h and c and base):
                continue
            if (base, label) not in ranges:
                ranges[(base, label)] = train_clusters(base, final[label], label, repo)
            if sha := ranges[(base, label)].get(c):
                heads.setdefault(b, []).append((h, f'carried by train {label} as cluster {c} ({sha}), which took its '
                                                   f'head {h[:12]}'))
    _trains[key] = heads
    return heads


def train_clusters(base: str, last: str, label: str, repo=REPO) -> dict[str, str]:
    """{cluster: its `Train-Cluster:` trailer commit} of ONE train: walking back along the first parent from the train's
    last commit to the member's base or to the nearest commit of ANOTHER train (`Train <other>:`), whichever comes first.
    A member's base can predate an earlier train (the wave-1055 branches were cut before train 1056 landed) and every
    train reuses the letters A, B, C, so the whole base..last range credited train 1055's members with train 1056's B to F
    (2026-10-11): a member its lander ejected would read LANDED by another train's commit, and --apply delete it."""
    out = git('log', '--first-parent', f'{base}..{last}',
              '--format=%h%x00%s%x00%(trailers:key=Train-Cluster,valueonly)%x01', cwd=repo).stdout
    found: dict[str, str] = {}
    for rec in out.split('\x01'):
        sha, _, rest = rec.strip().partition('\x00')
        subject, _, value = rest.partition('\x00')
        if (m := re.match(r'Train (\S+?):', subject)) and m.group(1) != label:
            break
        if value.strip():
            found.setdefault(value.strip(), sha)
    return found


def carried(branch, lost, graph: Ancestry, repo=REPO, coord_dir: pathlib.Path | None = None) -> str:
    """How a branch's work reached main as OTHER commits, or '' (kb/Work PB2708): a landed train took exactly its head
    (train_heads), or every one of its commits is patch-equivalent to a commit on main (`git cherry`: a finisher
    cherry-picked them, the five SPLIT predecessors of 2026-10-10) and it declares no name main lacks. A branch that
    moved past the head a train took is not carried (its later commits never were), and a cherry-picked branch with a
    name main lacks stays a DECISION (`worktree-wf_0cae47bc-172-2`: HolderState, StateOf)."""
    head = graph.sha(branch) or ''
    for took, evidence in train_heads(repo, coord_dir).get(branch, []):
        if head and head.startswith(took):
            return evidence
    marks = git('cherry', 'origin/main', branch, cwd=repo)
    signs = [line[:1] for line in marks.stdout.splitlines() if line.strip()]
    if marks.returncode == 0 and signs and set(signs) == {'-'} and not lost:
        return f'every one of its {len(signs)} commit(s) is patch-equivalent to a commit on main (git cherry)'
    return ''


@dataclasses.dataclass(frozen=True)
class Replay:
    """How far OTHER unlanded branches replay one branch's commits (`replays`, kb/Work PB3170). `carrier`: the branch
    that replays the most of them (a tie: the later report), among the outranking branches that are carried by nobody,
    when those together replay EVERY commit (the branch's work rides on them; None otherwise). `fresh`: this branch's
    commits (oldest first) that no outranking branch holds, in or out of a carried branch. `by`: (branch, how many of
    this branch's commits it replays) for each outranking UNCARRIED branch that replays any (a carried one has no
    finisher to name)."""
    carrier: str | None
    fresh: tuple[str, ...]
    by: tuple[tuple[str, int], ...]


def replays(branches: list[str], carriers: list[str], rank: Callable[[str], int], graph: Ancestry) -> dict[str, Replay]:
    """{branch: Replay} for each of `branches`, by PATCH EQUIVALENCE of the commits each holds beyond origin/main: a
    commit is replayed on another branch when that branch holds the commit itself (a merge of it) or one with the same
    patch id (a cherry-pick, which is what a planner's SPLIT successor does). `carriers` are the branches allowed to
    carry work (the planner passes the unlanded branches that have a report); `rank` orders them for the tie.

    ONE ORDER, NO CYCLES: a branch is carried only by branches that OUTRANK it, by (how many distinct patches it holds,
    then rank, then name). Two branches that hold the same work cannot each carry the other, and the largest of a family
    that replay one another carries nothing, so exactly one of them still does the work. A branch with no patch of its
    own (every commit a merge or empty) has nothing to carry and is never carried. One batched read of the patch ids
    of every commit (git_batch.patch_ids), never a `git cherry` per pair of branches (kb/Work PB3582)."""
    names = sorted(set(branches) | set(carriers))
    commits = {b: [c for c in reversed(graph.newest_first(graph.beyond(graph.sha(b) or b))) if len(graph.parents(c)) <= 1]
               for b in names}
    pid = git_batch.patch_ids(graph.repo, [c for cs in commits.values() for c in cs])
    held = {b: frozenset(pid[c] for c in cs if c in pid) for b, cs in commits.items()}

    def key(b: str) -> tuple[int, int, str]:
        return len(held[b]), rank(b), b

    # Highest key first, so every branch that outranks one is settled before it is read. A branch's carrier is always a
    # branch carried by NOBODY (one that has a finisher), never a middle link of a successor chain (each SPLIT successor
    # cherry-picks its predecessor: c-x, then a-mid, then b-top): naming a carried link strands the note on a branch
    # that never lands on its own (review R1-1). Every patch of a carried branch is held by an uncarried one: the
    # highest-keyed branch is uncarried, and a carried branch's patches are held by branches above it, by induction.
    settled: dict[str, Replay] = {}
    for b in sorted(names, key=key, reverse=True):
        mine = [c for c in commits[b] if c in pid]
        above = [c for c in carriers if c != b and held[c] and key(c) > key(b)]
        everywhere = frozenset().union(*(held[c] for c in above))
        fresh = tuple(x for x in mine if pid[x] not in everywhere)
        counted = [(c, sum(1 for x in mine if pid[x] in held[c])) for c in above if settled[c].carrier is None]
        by = tuple(sorted(((c, n) for c, n in counted if n), key=lambda t: (-t[1], t[0])))
        best = max(by, key=lambda t: (t[1], rank(t[0]), t[0]), default=None)   # most replayed, then the LATER report
        settled[b] = Replay(best[0] if mine and not fresh and best else None, fresh, by)
    return {b: settled[b] for b in branches}


def replay_view(ranked: dict[str, int], unlanded: Iterable[str], abandoned: set[str], graph: Ancestry) -> dict[str, Replay]:
    """`replays` over the branches both of its readers mean (kb/Work PB3170, PB3652): the PLANNER (plan_wave.branch_replays:
    which branch gets a finisher) and the SURVEY (contained(): which branch is a DECISION row). One function builds the
    inputs, so the two cannot disagree on who carries whom. `ranked`: {branch: the position of its newest report,
    oldest first} for each UNLANDED branch that has a report: the branches allowed to carry work, the later report
    winning a tie. `unlanded`: the other unlanded branches (commits naming a note, no report). A branch a kb/Work note
    records ABANDONED (`abandoned`, abandon_records) carries nothing and is no source."""
    carriers = [b for b in ranked if b not in abandoned]
    return replays(sorted(set(carriers) | {b for b in unlanded if b not in abandoned}), carriers,
                   lambda b: ranked.get(b, -1), graph)


def branch_graph(branches, repo=REPO) -> Ancestry:
    """THE ancestry every classify() of one pass reads (git_batch.Ancestry: one resolve and one `git log` for every
    branch, never a `merge-base` per branch, kb/Work PB3582)."""
    return Ancestry.read(repo, branches)


def classify(branch, names, graph: Ancestry, repo=REPO, coord_dir: pathlib.Path | None = None):
    """(content class, added files absent from main, declared C# names main lacks, how a LANDED row's work reached main
    when its own merge would still change main: carried(), else ''). `graph` holds the branch (branch_graph)."""
    if graph.on_base(graph.sha(branch) or branch):
        return 'MERGED', [], [], ''
    # ANY added file absent from main counts (2026-10-04: a branch that added only scripts/ and docs/ was classified
    # LANDED and would have been deleted), except the paths that are never work (coord.is_work: the one list, kb/Work
    # PB3465, PB3474). The `tools/` exclusion this once carried named the claude-skills submodule, whose gitlink is
    # never an ADDED path of a branch cut from main, and also hid every added file of tools/ArchCensus, tools/impact and
    # tools/rr: a branch adding only those read CHECK with no added file named, not UNLANDED (kb/Work PB3474).
    absent = absent_from('origin/main', [
        f for f in git('diff', '--name-only', '--diff-filter=A', 'origin/main...' + branch, cwd=repo).stdout.splitlines()
        if f and coord.is_work(f)], repo)
    # The names the branch's NET change declares (merge base to tip), never each commit's: a name one WIP commit added
    # and a later commit deleted is not lost (kb/Work PB2708: 908-1's RunsOfEqual, added in fb4c17001, gone at its head).
    declared = set()
    for line in git('diff', '-U0', 'origin/main...' + branch, '--', '*.cs', cwd=repo).stdout.split('\n'):
        for rx in (DECL, METH, TEST):
            m = rx.match(line)
            if m and len(m.group(1)) > 3:
                declared.add(m.group(1))
    lost = sorted(n for n in declared if n not in names)
    if absent:
        return 'UNLANDED', absent, lost, ''
    if merges_to_main_unchanged(branch, graph, repo):
        return 'LANDED', absent, lost, ''
    how = carried(branch, lost, graph, repo, coord_dir)
    return ('LANDED' if how else 'CHECK'), absent, lost, how


@dataclasses.dataclass
class Tree:
    path: str
    branch: str | None       # None: a detached worktree
    lock: str | None         # None: not locked; '' : locked with no reason; else the lock reason
    head: str


def worktrees(repo=REPO) -> list[Tree]:
    """Every linked worktree but the main checkout, with its branch (None when detached) and its lock reason."""
    found = []
    for blk in [b for b in git('worktree', 'list', '--porcelain', cwd=repo).stdout.split('\n\n') if b.strip()][1:]:
        path = re.search(r'^worktree (.+)$', blk, re.M).group(1)
        br = re.search(r'^branch refs/heads/(.+)$', blk, re.M)
        lk = re.search(r'^locked(?: (.*))?$', blk, re.M)
        head = re.search(r'^HEAD ([0-9a-f]+)$', blk, re.M)
        found.append(Tree(path, br.group(1) if br else None, (lk.group(1) or '') if lk else None, head.group(1) if head else ''))
    return found


def worktree_git_dir(path: str | pathlib.Path) -> pathlib.Path:
    """A linked worktree's own git directory, read from its `.git` file (`gitdir: <path>`, absolute or relative to the
    worktree) with no git process (kb/Work PB3582); the `.git` directory of a checkout that has one; else the worktree
    itself, whose `logs/HEAD` then simply does not exist (age_minutes ignores a missing path)."""
    dot = pathlib.Path(path) / '.git'
    if dot.is_dir():
        return dot
    try:
        text = dot.read_text(encoding='utf-8').strip()
    except OSError:
        return pathlib.Path(path)
    if not text.startswith('gitdir:'):
        return pathlib.Path(path)
    gitdir = pathlib.Path(text[len('gitdir:'):].strip())
    return gitdir if gitdir.is_absolute() else (pathlib.Path(path) / gitdir).resolve()


def live_lock(reason: str | None, alive: Callable[[int], str | None] = coord.process_name) -> bool:
    """Is this lock held by a live agent? A harness lock names its agent's pid, which must be a live claude process (a
    reused pid is some other image: verify-pid-before-kill, 2026-10-07); a lock with no pid was placed by hand and is
    honoured as live."""
    if reason is None:
        return False
    m = LOCK_PID.search(reason)
    if not m:
        return True
    name = alive(int(m.group(1)))
    return bool(name) and 'claude' in name.lower()


def age_minutes(*paths: pathlib.Path) -> float:
    """Minutes since the newest of `paths` changed (a missing path is ignored; none present = 0, i.e. fresh)."""
    stamps = []
    for p in paths:
        try:
            stamps.append(p.stat().st_mtime)
        except OSError:
            pass
    return (time.time() - max(stamps)) / 60 if stamps else 0.0


@dataclasses.dataclass
class Row:
    name: str                       # the branch, or '(detached) <path name>'
    content: str                    # MERGED | LANDED | CHECK | UNLANDED | DETACHED
    absent: list[str]
    lost: list[str]
    ahead: int
    worktree: str | None
    lock: str | None
    dirty: int
    age_min: float                  # minutes since its last activity (branch reflog, worktree HEAD log, STATUS.md)
    last_commit: str
    report: Report | None = None    # the newest fix-lane report that names it
    state: str = ''
    why: str = ''
    how: str = ''                   # a LANDED row whose own merge would change main: how its work got there (carried)
    tip: str = ''                   # the branch's tip sha (a review stamp covers exactly one head)
    review_holds: bool = False      # its review stamp (CLEAN or FINDINGS) names an older head and every commit since
                                    # is mechanical (unreviewed_since: main's, a clean merge of main, DEVLOG-only)
    report_since: list[str] | None = None   # unreviewed_since(the report's HEAD, tip): [] = the report covers the tip,
                                            # None = it names no head of this branch (kb/Work PB3181)
    carrier: str = ''               # contained(): the branch it lands THROUGH; its IN FLIGHT is that branch's, and
                                    # nobody works on THIS one (owned() excludes it; kb/Work PB3311 review R1-1)
    waits_on: str = ''              # carrying_findings(): the branch whose unanswered FINDINGS it carries, or that a
                                    # TORN findings file holding it names first; it lands after that one is answered
                                    # (or the file repaired), and nobody works on THIS one either (owned() excludes it)


def mechanical(commits: list[str], graph: Ancestry, repo=REPO) -> set[str]:
    """The commits (beyond the graph's base) that change nothing a reviewer reads: a merge whose tree IS the clean merge
    of its two parents THROUGH THE MERGE DRIVERS, the runtime ABI pin's measurement aside (no hand resolution: what
    `enqueue.py land`'s own merge of origin/main makes; merge_drivers.is_drivers_merge, kb/Work PB3610), or a commit that
    changes only DEVLOG/ (an entry, its stamp). Parents and trees come from the graph; every one-parent commit's files
    from ONE `git diff-tree --stdin` (git_batch.diff_names); a merge costs its one `git merge-tree` (kb/Work PB3582)."""
    out = set()
    for c in commits:
        parents = graph.parents(c)
        if len(parents) == 2 and merge_drivers.is_drivers_merge(repo, parents, graph.tree(c)):
            out.add(c)
    singles = [c for c in commits if len(graph.parents(c)) == 1]
    for c, paths in zip(singles, git_batch.diff_names(repo, [(c, graph.parents(c)[0]) for c in singles])):
        if paths and all(p.startswith('DEVLOG/') for p in paths):
            out.add(c)
    return out


def unreviewed_since(reviewed: str, tip: str, *, repo=REPO, base: str = git_batch.BASE,
                     graph: Ancestry | None = None) -> list[str] | None:
    """THE REVIEW'S COVERAGE (MANDATORY-PRACTICES I10, L9; kb/Work PB3027, PB3023), one rule read by this survey and by
    `enqueue.py land`: the commits of `tip` after the `reviewed` head that no reviewer read, newest first — every commit
    not on `base` but a mechanical one (above). [] = the review still covers the tip; None = `reviewed` is not a commit
    of `tip` (unreadable, or another history), so it covers nothing. `graph`: the pass's Ancestry over `base` holding
    `tip` (the survey's, read once for every row); else one is read here for `tip` alone."""
    if not tip:
        return None
    if graph is None:
        try:
            graph = Ancestry.read(repo, [tip], base)
        except git_batch.GitError:   # a tip git cannot read: no review covers it
            return None
    elif graph.base != base:
        raise ValueError(f'unreviewed_since: the graph excludes {graph.base}, not {base}')
    full, head = graph.sha(reviewed), graph.sha(tip)
    if not full or not head:
        return None
    if not graph.is_ancestor(full, head):
        return None
    since = graph.newest_first(graph.beyond(head) - graph.beyond(full))
    skip = mechanical(since, graph, repo)
    return [c for c in since if c not in skip]


def review_holds(rv: Review, tip: str | None, *, repo=REPO, graph: Ancestry | None = None) -> bool:
    """A review stamp at an OLDER head still holds at `tip`: every commit since its head is mechanical
    (unreviewed_since == []: main merged in cleanly, the DEVLOG stamp). False when it names the tip itself."""
    return bool(tip) and not rv.covers(tip) and unreviewed_since(rv.sha, tip, repo=repo, graph=graph) == []


def stamp_covers(rv: Review, tip: str | None, *, repo=REPO, graph: Ancestry | None = None) -> bool:
    """THE REVIEW STAMP'S COVERAGE OF A BRANCH TIP (coverage 2 of the module doc), one rule for both verdicts and both
    readers: lifecycle() here (a CLEAN stamp that covers is WAITING TO LAND, a FINDINGS stamp that covers is the next
    wave's finisher) and the planner's FINDINGS arm (plan_wave.plan `review_covers`, kb/Work PB3394). The stamp names
    the tip, or it holds there (review_holds). A stamp at an older head followed by real commits covers nothing: those
    commits are unreviewed, so the branch is WAITING FOR REVIEW, a diff-only re-review (kb/Work PB3381), and a
    finisher there would answer findings the branch has already answered (wave 1127 planned three)."""
    return bool(tip) and (rv.covers(tip) or review_holds(rv, tip, repo=repo, graph=graph))


def report_since(head: str | None, tip: str | None, *, repo=REPO, graph: Ancestry | None = None) -> list[str] | None:
    """THE REPORT'S COVERAGE OF A BRANCH TIP, the one reader (kb/Work PB3446): the commits of `tip` after the report's
    HEAD that no report describes (unreviewed_since: the review's coverage rule, mechanical commits aside), newest
    first. [] = the report covers the tip; None = it names no HEAD, the branch has no tip, or the HEAD is no commit of
    the tip's history, so it describes nothing. lifecycle() reads it off the row the survey measured (`Row.report_since`)
    and the planner through `Coverage.report`: both ask this function, so they never read one report two ways."""
    return unreviewed_since(head, tip, repo=repo, graph=graph) if head and tip else None


class Coverage:
    """What the tips THIS survey measured say about a branch's two coverages, for the planner (plan_wave.main): the
    review stamp's (`review`, the FINDINGS arm, kb/Work PB3394) and the report's (`report`, the stale-stamp arm, kb/Work
    PB3446), each the very function lifecycle() asks (stamp_covers, report_since), so the planner and lifecycle() never
    disagree on one branch. A branch the survey has no row for has no tip, and nothing covers it. Every answer reads ONE
    graph of the measured tips, read at the first question that needs it (kb/Work PB3582)."""

    def __init__(self, rows: list[Row], repo=REPO):
        self.repo = repo
        self.tips = {r.name: r.tip for r in rows}
        self._graph: Ancestry | None = None

    def graph(self) -> Ancestry:
        if self._graph is None:
            self._graph = Ancestry.read(self.repo, [t for t in self.tips.values() if t])
        return self._graph

    def review(self, rv: Review | None, branch: str | None) -> bool:
        """The stamp covers the branch's tip: it names it, or it holds there (stamp_covers)."""
        tip = self.tips.get(branch or '')
        if not rv or not tip:
            return False
        return rv.covers(tip) or stamp_covers(rv, tip, repo=self.repo, graph=self.graph())

    def report(self, head: str | None, branch: str | None) -> bool:
        """The report whose HEAD is `head` covers the branch's tip (report_since == [])."""
        tip = self.tips.get(branch or '')
        return bool(head and tip) and report_since(head, tip, repo=self.repo, graph=self.graph()) == []


@dataclasses.dataclass
class Report:
    name: str                       # the file name, w<wave><letter>-PB<lead>-report.md
    status: str                     # DONE | SPLIT | DISCHARGED | BLOCKED | NOT STARTED | '' (plan_wave.parse_report)
    lead: str                       # its lead note
    lead_terminal: bool             # the lead note is landed or retired: the work reached main some other way
    age_min: float                  # minutes since the report was written
    header: str = dataclasses.field(default='', repr=False)
    review: Review | None = None    # the header's branch-review stamp (plan_wave.parse_review, kb/Work PB3027)
    head: str | None = None         # the header's HEAD: the branch tip the report describes (plan_wave.HEAD)
    finished: bool = False          # plan_wave.Report.finished: DONE, or DISCHARGED with a HEAD past its base


def lane_latest(row: Row, lane: Lane | None) -> tuple[dict, str] | None:
    """The landing lane's newest attempt for the branch and its class (land_lane.effective), or None."""
    return next(((a, c) for a, c in (lane.latest if lane else ()) if a.get('branch') == row.name), None)


def at_tip(row: Row, att: dict) -> bool:
    """The attempt ran at the branch's tip: its head, or the end head its run left (main merged in, the DEVLOG stamp)."""
    return any(row.tip and h and (row.tip.startswith(h) or h.startswith(row.tip))
               for h in (att.get('head'), att.get('end_head')))


def not_queued(row: Row, pr: PullRequest, lane: Lane | None, now: dt.datetime) -> tuple[str, str]:
    """NOT QUEUED (module doc): how long the pull request has waited since it was handed to the queue (auto-merge
    enabled, else the lane's ENQUEUED attempt ended), what GitHub says, and the landing lane's queue watch."""
    import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back, lazily)
    latest = lane_latest(row, lane)
    att = latest[0] if latest and at_tip(row, latest[0]) else None
    since = pr.auto_merge_at or (att or {}).get('ended') or ''
    try:
        waited = f'{(now - dt.datetime.fromisoformat(since.replace("Z", "+00:00"))).total_seconds() / 60:.0f} min'
    except ValueError:
        waited = 'an unknown time'
    return 'NOT QUEUED', (f'pull request #{pr.number} ({pr.url}) was handed to the merge queue {waited} ago '
                          f'({since or "?"}) and is NOT in it: {queue_facts.describe(pr)}; '
                          + (land_lane.watch_text(att) if att else
                             'no landing-lane attempt at this tip watches it (the lane watches only its own landings)'))


def landing_state(row: Row, rep: Report, rv: Review, lane: Lane | None, observed: bool = False,
                  pr: PullRequest | None = None) -> tuple[str, str]:
    """A report-covered, review-CLEAN-covered branch, by the landing lane's newest attempt AT THIS TIP (its head, or the
    end head its run left: main merged in, the DEVLOG stamp) — the third coverage of lifecycle() (kb/Work PB3181,
    PB3125). The lane's own classes (land_lane.classify/effective) and its own ACTION text decide; its status text is
    never parsed. A STOPPED attempt is never WAITING TO LAND: the lane does not relaunch a stop at the same head, so
    only a new head (its finisher's) or the enqueue unit's re-arm lands it. `observed`: GitHub's open pull requests
    were read (an exit-0 attempt with none open is NOT QUEUED, closed; unobserved, the lane's record says ENQUEUED).
    `pr`: the branch's open pull request when it is open at ANOTHER head than the tip (lifecycle answers one at the tip
    first), so the evidence of the exit-0 arm names it instead of claiming that none is open."""
    since = '' if rv.covers(row.tip) else ' (later commits mechanical: main merged in, the DEVLOG stamp)'
    clean = f'{rep.name}: Status {rep.status}, Review CLEAN at {rv.sha[:12]} ({rv.reviewer}){since}'
    waiting = ('WAITING TO LAND', clean + '; the landing lane lands it (enqueue.py land)')
    latest = lane_latest(row, lane)
    if latest is None:
        return waiting
    import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back, lazily)
    att, cls = latest
    if cls == 'running':
        return 'WAITING TO LAND', clean + f'; being landed (attempt {att["id"]}, pid {att.get("pid")})'
    if not at_tip(row, att):
        return waiting                                  # an attempt at another head: this tip was never tried
    rearm = (lane.rearms or {}).get(row.name) if lane else None
    tried = f'landing attempt {att["id"]} {land_lane.outcome(att)} ({att.get("verdict") or "no verdict line"})'
    if rearm and rearm.get('head') == row.tip:
        return 'WAITING TO LAND', (f'{clean}; {tried}, re-armed by the enqueue unit at this head '
                                   f'({rearm.get("why") or "no reason given"}): the lane lands it again')
    if cls == land_lane.STOPPED:
        return 'STOPPED', (f'{clean}; {tried}: {land_lane.ACTION[land_lane.STOPPED]}'
                           + (' [collected: the enqueue unit has dispatched its owner]' if att.get('collected') else ''))
    if cls == land_lane.DONE and observed:
        # lifecycle() read every OPEN pull request first: one at this tip in the queue is ENQUEUED and one out of it NOT
        # QUEUED there, so an observed read that reaches here holds NO open pull request for a branch whose work is not
        # on main: it was closed without merging (kb/Work PB3527; the lane's queue watch alerts the operator).
        open_at = (f'its pull request #{pr.number} is open at {pr.head[:12]}, not at this tip ({pr.url}; was its head '
                   f'moved by hand?)' if pr else
                   'no pull request is open for it and its work is not on main (closed by hand?)')
        return 'NOT QUEUED', f'{tried} at this tip, but {open_at}; {land_lane.watch_text(att)}'
    if cls == land_lane.DONE:
        return 'ENQUEUED', (f'{tried} at this tip: its pull request went to the merge queue (GitHub not observed: '
                            'the lane\'s record says so): it lands when the queue merges it')
    return 'WAITING TO LAND', f'{clean}; {tried}: {land_lane.ACTION[cls]}'


def stamp_held(row: Row) -> bool:
    """The row's review stamp covers its tip (stamp_covers, measured by survey(): the stamp names the tip, or it holds
    there)."""
    rv = row.report.review if row.report else None
    return bool(rv) and (rv.covers(row.tip) or row.review_holds)


def findings_open(row: Row) -> bool:
    """A finished report on a branch not on main whose review stamp says FINDINGS and covers the tip (stamp_held): the
    findings are unanswered, whatever the report's age. THE one statement of it: finisher_due reads it, and lifecycle()
    reads it before its stale-report arm, so an old report with findings never reads "land it" (kb/Work PB3118)."""
    rep = row.report
    return bool(row.content not in LANDED_CONTENT and rep and rep.finished
                and rep.review and rep.review.verdict == 'FINDINGS' and stamp_held(row))


def finisher_due(row: Row) -> bool:
    """A finished branch, not on main, whose branch review stamped FINDINGS at its tip: the planner owns its finisher
    (plan_wave.plan's FINDINGS arm, the same stamp_covers test), so the branch lands once that finisher has answered
    the findings, and everything it contains lands with it (contained(), kb/Work PB3267). Its row is a DECISION:
    nothing else decides it."""
    return row.state == 'DECISION' and findings_open(row)


def lifecycle(row: Row, *, in_flight_slugs: set[str], min_age: int, stale_hours: float = 24,
              live: Callable[[str | None], bool] = live_lock,
              prs: dict[str, PullRequest] | None = None, lane: Lane | None = None,
              now: dt.datetime | None = None) -> tuple[str, str]:
    """THE lifecycle rule (module doc), as a pure function of what the survey measured: (state, the evidence). `lane`:
    the landing lane's view (land_lane.view: each branch's newest attempt and its class, the pending re-arms). `prs`:
    queue_facts.read's answer, None when GitHub was not observed (the git-only lifecycle). `now`:
    the time a NOT QUEUED row's wait is measured to (default: now)."""
    if live(row.lock):
        return 'IN FLIGHT', f'worktree locked by a live agent ({row.lock or "locked by hand"})'
    pr = (prs or {}).get(row.name)
    if pr and row.content not in LANDED_CONTENT:
        if pr.in_queue:
            return 'ENQUEUED', f'pull request #{pr.number} is in the merge queue ({pr.url}): it lands when the queue merges it'
        at_head = not row.tip or row.tip == pr.head
        finisher = ('ONE finisher fixes the branch and returns DONE; its new tip is reviewed before enqueue.py land '
                    'takes it (kb/Work PB3023)')
        if at_head and (red := pr.red):
            return 'BOUNCED', (f'pull request #{pr.number}\'s required check {red.name} FAILED on its head before the '
                               f'merge queue took it ({red.conclusion.lower()}, workflow run {red.run}; {pr.url}), so it '
                               f'cannot enter the queue at this head (kb/Work PB3096, PB3527): {finisher}')
        if at_head and pr.conflicts:
            return 'BOUNCED', (f'pull request #{pr.number} conflicts with main (merge state DIRTY; {pr.url}): GitHub '
                               f'cannot queue it until the branch brings main in, which no re-run or reopen does '
                               f'(kb/Work PB3527 review R1-5; MANDATORY-PRACTICES L12): {finisher}')
        if pr.bounced and at_head:
            return 'BOUNCED', (f'pull request #{pr.number} was removed from the merge queue ({pr.bounced}; {pr.url}): '
                               f'{finisher}')
        import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back, lazily)
        latest = lane_latest(row, lane)
        # A pull request is handed to the queue AT ONE HEAD: auto-merge on is GitHub's promise to queue THAT head, and
        # with auto-merge off only the lane's ENQUEUED attempt at this tip says it was handed. So NOT QUEUED reads only
        # a pull request AT THE BRANCH'S TIP: a pull-request state is read against the head it was observed at (kb/Work
        # PB3275, PB3628). One at an older head (a finisher's fix committed locally, the push still to come) is the OLD
        # landing's, whatever that head's health. A head that cannot be queued (it conflicts with main, a required
        # check failed on it, the queue removed it) promises nothing (PB3275: a finisher's tip on a CONFLICTING
        # auto-merge pull request read ENQUEUED, then NOT QUEUED, for ever); a healthy one promises only ITS commits,
        # never the tip's, so waiting on it would land the old head and leave the fix for a second landing. The branch
        # reads by its report again: WAITING FOR REVIEW until the tip is reviewed, then WAITING TO LAND, and
        # `enqueue.py land` pushes the tip onto the same pull request and queues it (the queue watch reads the moved
        # head as a new landing: watch_step). PB3628's witness: PR #467's old head read NOT QUEUED, which neither the
        # lane's candidates nor the enqueue unit acts on, and stranded a re-armed, reviewed-CLEAN tip for 13 h.
        if at_head and (pr.auto_merge_at or (latest and latest[1] == land_lane.DONE and at_tip(row, latest[0]))):
            return not_queued(row, pr, lane, now or dt.datetime.now(dt.timezone.utc))
    slug = next((s for s in sorted(in_flight_slugs) if re.match(re.escape(s) + r'(?![a-z0-9])', row.name.lower())), None)
    if slug:
        return 'IN FLIGHT', f'dispatch ledger: group {slug} is still in flight'
    fresh = row.age_min < min_age
    touched = f'touched {row.age_min:.0f} min ago (under --min-age {min_age})'
    if row.content == 'DETACHED':
        return ('IN FLIGHT', touched) if fresh else (
            'DECISION', f'detached worktree at {row.last_commit}, idle {row.age_min / 60:.1f} h'
            + (f', {row.dirty} uncommitted path(s)' if row.dirty else '') + ': remove it when no fleet reads it')
    if row.content in LANDED_CONTENT and not fresh and not row.dirty:
        return 'LANDED', f'{row.content}{f" ({row.how})" if row.how else ""}: its work is on main; --apply deletes it'
    rep = row.report
    if row.content not in LANDED_CONTENT and rep and rep.finished:
        # A FINISHED report (plan_wave.Report.finished) decides the branch only where it COVERS the tip: its HEAD is the
        # tip, or every commit after it is mechanical (unreviewed_since, the coverage rule of the review stamp and of
        # `enqueue.py land`). Never the report's age (kb/Work PB3181: enqueue.py's merge and DEVLOG stamp postdate it).
        if rep.lead_terminal:
            return 'DECISION', (f'{rep.name}: {rep.status}, but {rep.lead} is already landed: superseded (its work '
                                'reached main through another branch); confirm nothing unique remains, then delete it; '
                                + evidence(row))
        if rep.age_min > stale_hours * 60 and not findings_open(row):   # an old report WITH findings is the finisher's, below
            return 'DECISION', (f'{rep.name}: {rep.status} {rep.age_min / 60:.0f} h ago and never landed (over '
                                f'{stale_hours:g} h): land it or record why not; ' + evidence(row))
        uncovered = ('the report names no HEAD and carries no review stamp, so it describes no commit of the branch'
                     if not rep.head else
                     f'the report\'s HEAD {rep.head[:12]} is not a commit of this branch' if row.report_since is None else
                     f'{len(row.report_since)} commit(s) since the report\'s HEAD {rep.head[:12]} that no report '
                     f'describes (newest {row.report_since[0][:12]}): a resumed agent rewrites its report'
                     if row.report_since else '')
        if uncovered:
            return ('IN FLIGHT', touched) if fresh else ('DECISION', f'{rep.name}: {rep.status}, but {uncovered}; '
                                                         + evidence(row))
        rv = rep.review
        covered = stamp_held(row)
        if covered and rv.verdict == 'CLEAN':
            return landing_state(row, rep, rv, lane, observed=prs is not None, pr=pr)
        if covered and rv.verdict == 'FINDINGS':
            late = (f' (the report is {rep.age_min / 60:.0f} h old: the finisher is overdue, the landing is not)'
                    if rep.age_min > stale_hours * 60 else '')
            return 'DECISION', (f'{rep.name}: {rep.status}, but its branch review found {rv.findings} finding(s) '
                                f'({rv.file}): a finisher answers them, and the next wave plans it (plan_wave.py, '
                                f'kb/Work PB3027){late}; ' + evidence(row))
        unreviewed = ('no review stamp' if rv is None else
                      f'Review {rv.verdict} at {rv.sha[:12]}, but the branch is at {row.tip[:12] or "?"}')
        return 'WAITING FOR REVIEW', (f'{rep.name}: Status {rep.status}, {unreviewed}: one cobol-reviewer reviews it (the '
                                      'review skill\'s branch form, kb/Work PB3027)')
    if fresh:
        return 'IN FLIGHT', touched
    return 'DECISION', evidence(row)


def evidence(row: Row) -> str:
    ev = [row.content + (f' ({len(row.absent)} added file(s) absent from main, e.g. {row.absent[0]})' if row.absent else '')
          + (f' (names not on main: {" ".join(row.lost[:5])})' if row.content == 'CHECK' else ''),
          f'{row.ahead} commit(s) ahead', f'idle {row.age_min / 60:.1f} h', f'last: {row.last_commit}']
    if row.dirty:
        ev.append(f'{row.dirty} uncommitted path(s) in its worktree')
    if row.lock is not None:
        ev.append(f'stale lock ({row.lock})')
    rep = row.report
    ev.append(f'{rep.name}: {rep.status or "no status"} ({rep.lead} {"landed" if rep.lead_terminal else "open"})'
              if rep else 'no fix-lane report names it')
    return '; '.join(ev)


def note_terminal(work_dir: pathlib.Path | None = None) -> Callable[[str], bool]:
    """`terminal(note id)`: the note is landed or retired (work.py's one frontmatter reader and status set)."""
    sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
    import work  # noqa: PLC0415
    status = {i.get('id'): i.get('status') for i in (work.load(work_dir) if work_dir else work.load())}
    return lambda n: status.get(n) in work.TERMINAL_STATUSES


def report_index(reports_dir: pathlib.Path | None, terminal: Callable[[str], bool]) -> list[Report]:
    """Every fix-lane report, oldest first, through plan_wave's reader (coord.REPORT_NAME, the status line), reused and never
    forked: a suffixed name is invisible here exactly as it is to the planner (kb/Work PB2602). The header (up to the
    first `## ` heading, as plan_wave.parse_report reads it) is where a report names its branch and worktree."""
    if not reports_dir or not reports_dir.is_dir():
        return []
    import plan_wave  # noqa: PLC0415  (it imports this module back, lazily: no cycle at import time)
    out = []
    for r in plan_wave.load_reports(reports_dir):
        header = plan_wave.header_lines(r.path.read_text(encoding='utf-8', errors='replace').splitlines())
        out.append(Report(r.path.name, r.status, r.lead, terminal(r.lead), age_minutes(r.path), '\n'.join(header),
                          r.review, r.head, r.finished))
    return out


BRANCH_FIELD = re.compile(r'(?i)\bbranch\b[\s:*`(]*([A-Za-z0-9_][\w.\-/]*[\w])')


def newest_report(reports: list[Report], branch: str | None, path: str | None) -> Report | None:
    """The newest report about this branch. A header with a `branch` field (`Branch: x`, `**branch:** `x``, `(branch x)`)
    is about exactly the branch(es) it names: a header that ALSO names another branch, e.g. as its base or its
    predecessor, is not about that one (2026-10-08: w1037r's report names landing-lease as its base). A header with no
    branch field is about any branch it names as a whole word, or whose worktree directory it names (either slash)."""
    word = re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])') if branch else None
    tree = re.compile(r'worktrees[\\/]+' + re.escape(pathlib.PurePath(path).name) + r'(?![\w.\-])') if path else None
    hit = None
    for rep in reports:
        named = set(BRANCH_FIELD.findall(rep.header))
        if (branch in named) if named else any(p and p.search(rep.header) for p in (word, tree)):
            hit = rep
    return hit


def in_flight_slugs(coord_dir: pathlib.Path | None, terminal: Callable[[str], bool],
                    reports_dir: pathlib.Path | None = None) -> set[str]:
    """The `w<wave><letter>` slugs of the dispatch-ledger wave groups still in flight, by the planner's own liveness rule
    (plan_wave.inflight_file_sets, source 3: a group with a note that is not landed or retired and that no report in
    `reports_dir` answers, kb/Work PB2795), read from its `meta` (each entry's recorded slug), never parsed back out of
    an owner label. A dispatch made without a rendered group has no ledger entry (kb/Work PB3186); its worktree is
    caught by its live lock or its recency instead."""
    import plan_wave  # noqa: PLC0415
    meta: dict[str, dict] = {}
    plan_wave.inflight_file_sets(coord_dir=coord_dir, with_git=False, terminal=terminal, meta=meta,
                                 reports=plan_wave.load_reports(reports_dir) if reports_dir else ())
    return {m['slug'] for m in meta.values() if m.get('slug')}


def live_survey(repo=REPO, *, coord_dir: pathlib.Path | None = None, reports_dir: pathlib.Path | None = None,
                terminal: Callable[[str], bool] | None = None, min_age: int = 180, stale_hours: float = 24,
                live: Callable[[str | None], bool] = live_lock, gh=None,
                prs: dict[str, PullRequest] | None | object = UNREAD) -> list[Row]:
    """THE lifecycle as every production reader takes it: this module's main, the landing lane (land_lane.survey) and
    the planner's file-set partition (plan_wave.main) all call this, never `survey` with arguments of their own, so
    each reads the fix-lane reports, the dispatch ledger's in-flight slugs and the OPEN PULL REQUESTS the same way. A
    reader that surveyed without the pull requests could never see ENQUEUED from GitHub or BOUNCED at all, and would
    plan over a branch the merge queue rejected as if it were still queued (kb/Work PB2807, branch review R1-2).
    `gh`: queue_facts.read's runner (a self-test fake). `prs`: the pull requests the caller already read with
    queue_facts.read on this pass (the landing lane's tick, whose queue watch reads the same facts; None when that read
    was not observed), else (UNREAD) read here. The caller fetches origin first when it needs a current main."""
    terminal = terminal or note_terminal()
    rdir = reports_dir or coord.reports_dir(str(coord_dir) if coord_dir else None)
    return survey(repo, reports=report_index(rdir, terminal), slugs=in_flight_slugs(coord_dir, terminal, rdir),
                  min_age=min_age, stale_hours=stale_hours, live=live, coord_dir=coord_dir,
                  prs=queue_facts.read(gh) if prs is UNREAD else prs)


def survey(repo=REPO, *, prs: dict[str, PullRequest] | None, reports: list[Report] = (),
           slugs: set[str] = frozenset(), min_age: int = 180, stale_hours: float = 24,
           live: Callable[[str | None], bool] = live_lock, coord_dir: pathlib.Path | None = None) -> list[Row]:
    """Every local branch but main, and every detached worktree, measured and given its lifecycle state. `prs`:
    {branch: its open pull request into main} (queue_facts.read), or None when GitHub was not observed (the git-only
    lifecycle); REQUIRED so no caller reads a git-only lifecycle by omission; production readers call live_survey, and
    a self-test passes its own (None is an explicit offline, {} an observed "no pull request is open")."""
    names = main_names(repo)
    trees = worktrees(repo)
    by_branch = {t.branch: t for t in trees if t.branch}
    common = pathlib.Path(git('rev-parse', '--git-common-dir', cwd=repo).stdout.strip())
    if not common.is_absolute():
        common = pathlib.Path(repo) / common

    # Every branch's tip and last-commit line from ONE `git for-each-ref`, and the ancestry every row reads (MERGED, the
    # count ahead, the review's coverage, contained(), carrying_findings()) from ONE graph: never a `rev-parse`,
    # `log -1`, `rev-list --count` or `merge-base` per branch (kb/Work PB3582: they were most of the 3,048 git processes
    # of this script's self-test).
    refs = {}
    for line in git('for-each-ref', 'refs/heads',
                    '--format=%(refname:short)%00%(objectname)%00%(objectname:short) %(committerdate:short) %(subject)',
                    cwd=repo).stdout.splitlines():
        b, tip, last = line.split('\0', 2)
        if b != 'main':
            refs[b] = (tip, last[:90])
    graph = branch_graph(list(refs), repo)
    reports = list(reports)
    detached = [t for t in trees if not t.branch]
    # every other name a row asks the graph about, resolved in the same single process as a batch
    graph.names(*{x for r in reports for x in (r.head, r.review.sha if r.review else None) if x},
                *{t.head for t in detached if t.head})

    def measure(b: str) -> Row:
        tip, last = refs[b]
        kind, absent, lost, how = classify(b, names, graph, repo, coord_dir)
        t = by_branch.get(b)
        stamps = [common / 'logs' / 'refs' / 'heads' / b]
        dirty = 0
        if t:
            stamps += [worktree_git_dir(t.path) / 'logs' / 'HEAD', pathlib.Path(t.path) / 'STATUS.md']
            dirty = uncommitted(t.path)
        return Row(b, kind, absent, lost, len(graph.beyond(tip)), t.path if t else None, t.lock if t else None, dirty,
                   age_minutes(*stamps), last, newest_report(reports, b, t.path if t else None), how=how, tip=tip)

    shown = {}
    valid = sorted({sha for t in detached if t.head and (sha := graph.sha(t.head))})
    if valid:   # every detached worktree's last-commit line from ONE `git log --no-walk`; an unreadable head reads ''
        for line in git('log', '--no-walk=unsorted', '--format=%H %h %cs', *valid, cwd=repo).stdout.splitlines():
            sha, _, rest = line.partition(' ')
            shown[sha] = rest

    def measure_detached(t: Tree) -> Row:
        dirty = uncommitted(t.path)
        last = shown.get(graph.sha(t.head) or '', '') if t.head else '?'
        return Row(f'(detached) {pathlib.PurePath(t.path).name}', 'DETACHED', [], [], 0, t.path, t.lock, dirty,
                   age_minutes(worktree_git_dir(t.path) / 'logs' / 'HEAD', pathlib.Path(t.path) / 'STATUS.md'), last)

    with ThreadPoolExecutor(max_workers=16) as pool:   # subprocess-bound: ~1-3 s a branch, 35 s for 29 one by one
        rows = list(pool.map(measure, refs)) + list(pool.map(measure_detached, detached))
    import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back; its attempt reader, never its text)
    lane = land_lane.view(pathlib.Path(coord_dir) if coord_dir else coord.coord_path())
    for r in rows:
        rv = r.report.review if r.report else None
        if rv:
            r.review_holds = review_holds(rv, r.tip, repo=repo, graph=graph)
        if r.content not in LANDED_CONTENT and r.report and r.report.finished and r.report.head and r.tip:
            r.report_since = report_since(r.report.head, r.tip, repo=repo, graph=graph)
        r.state, r.why = lifecycle(r, in_flight_slugs=set(slugs), min_age=min_age, stale_hours=stale_hours, live=live,
                                   prs=prs, lane=lane)
    contained(rows, graph, reports, pathlib.Path(repo) / 'kb' / 'Work')
    import review_findings  # noqa: PLC0415  (scripts/orchestrator: the one reader of a findings file, kb/Work PB3381)
    scratch = (pathlib.Path(coord_dir) if coord_dir else coord.coord_path()) / 'scratch'
    stamped = [r.review.file for r in reports if r.review and r.review.file]
    found = review_findings.unanswered(review_findings.findings_files(scratch, also=stamped))
    for v in found:   # said on every survey (session-probe, the landing lane's log) until the operator repairs it
        if v['torn']:
            print(f'prune_worktrees: a findings file is TORN and holds every branch it names (review R5-1, R6-1): {v["torn"]}; '
                  f'the operator: {review_findings.remedy(pathlib.Path(v["file"]), v["line"])}', file=sys.stderr)
    carrying_findings(rows, graph, found, reports)
    carrying_landing_branch(rows, graph)
    return rows


#: The states whose branch has an owner and lands, so a branch it contains that cannot land by itself lands through
#: it. STOPPED is one (kb/Work PB3125): its landing attempt stopped, the enqueue unit's finisher owns it.
CARRIER_STATES = ('IN FLIGHT', 'WAITING FOR REVIEW', 'WAITING TO LAND', 'STOPPED', 'ENQUEUED', 'NOT QUEUED', 'BOUNCED')


#: The states of a branch with NO clean review stamp at its tip that contained() can read as carried: a finished report
#: still WAITING FOR REVIEW (no stamp, or a stamp at an older head), and a DECISION, whatever its report says or whether
#: it has one (kb/Work PB3386). Every other state keeps its own lifecycle inside a carrier: a CLEAN stamp at the tip (WAITING TO LAND,
#: STOPPED) or a pull request (ENQUEUED, BOUNCED) is that branch's own landing, which nothing postpones (kb/Work PB3381).
CARRIED_STATES = ('DECISION', 'WAITING FOR REVIEW')


def contained(rows: list[Row], graph: Ancestry, reports: list[Report], work_dir: pathlib.Path | None) -> None:
    """A branch that lands THROUGH another one is IN FLIGHT, not a decision, not a review and not a second landing: its
    tip is an ancestor of the tip of a fix-lane branch (one a report names) that is IN FLIGHT, waiting or STOPPED
    (CARRIER_STATES) or whose review ended FINDINGS (finisher_due: the planner owns its finisher, and everything it
    contains lands with that branch, kb/Work PB3267), and it carries NO clean stamp at its tip (CARRIED_STATES: a DECISION, or a
    finished report WAITING FOR REVIEW), so it cannot land by itself: an unreviewed checkpoint
    never lands alone (kb/Work PB3381 section G). That is a branch a review finisher merged (kb/Work PB3027: the wave's
    finisher rewrites the report for its own branch; a later wave's finisher writes a new report and leaves the
    FINDINGS-stamped one behind), an unfinished predecessor its successor merged, and a DONE predecessor whose own
    review never ended (its reviewer hit its ceiling: NO-RESULT), which its successor's review covered with its own
    (wf_rolling_wave.js predecessorEnd 'unreviewed'). Reading that one WAITING FOR REVIEW sent a second reviewer over
    content already reviewed, whose findings would then hold the CLEAN successor (review R1-2).
    A branch reviewed CLEAN at its tip keeps its own lifecycle, contained or not (kb/Work PB3381, owner 2026-10-18:
    "land the clean links as they come"): a same-file predecessor reviewed CLEAN is WAITING TO LAND and lands at once,
    its successor's pull request merging on top, where until then it read IN FLIGHT and waited for its successor (the
    train era's hold: wave 1118 kept six DONE branches of one chain off main for seven hours). Once the carrier lands,
    the content test reads a carried branch LANDED and --apply deletes it. Two names for one commit carry nothing.

    AN UNFINISHED (SPLIT, NOT STARTED) BRANCH IS CARRIED TOO (kb/Work PB3652): the planner reads one a finished branch
    contains (plan_wave.branch_carrier) or that another branch REPLAYS (plan_wave.branch_replays: the planner's own
    FINISH-FIRST pred tells a SPLIT successor to CHERRY-PICK its predecessor, so the successor is no descendant of it)
    as carried: under --drain it waits for the carrier, and otherwise its finisher's pred names the carrier (as for a
    carried FINDINGS branch). Either way the work lands through the carrier, and the survey listed it as a DECISION row
    regardless: the predecessor of a merge stayed a
    DECISION beside its carrier, and a cherry-picked one until its successor landed. Its row now names its carrier by
    the planner's own rule (`replay_view`, the same function): a branch every commit of which an outranking, uncarried
    branch holds lands through that branch. The replay arm reads only a DECISION row the planner plans from (no report,
    or an unfinished one; never a FINDINGS stamp: its finisher answers the findings), and the carrier may be in any
    state: the planner owns its finisher, so the carried row is no decision either.

    THE REPORT DECIDES NOTHING HERE (kb/Work PB3386): the ancestry arm carries every DECISION row, whatever its report
    says, because a report describes the branch and the carrier's tip contains the branch's commits, so they land with the
    carrier and its review covers them. A DONE report 30 h old, one a later commit has outrun, and one whose lead note
    already landed each read DECISION (land it or abandon it) beside a carrier that lands the same commits. The arm's
    first form skipped every DECISION that had a report unless its review ended FINDINGS, and PB3652 added the
    unfinished ones, so a SPLIT predecessor its successor merged read DECISION until then."""
    import plan_wave  # noqa: PLC0415  (it imports this module back, lazily: no cycle at import time)
    carriers = [c for c in rows if c.report and c.tip and (c.state in CARRIER_STATES or finisher_due(c))]
    candidates = []
    replayable = set()   # the candidates the replay arm reads too: the rows the planner plans a finisher from
    for r in rows:
        if not r.tip or r.dirty or r.content in LANDED_CONTENT or r.state not in CARRIED_STATES:
            continue
        candidates.append(r)
        # the replay arm is the planner's own: it plans a finisher only from a DECISION with no report or an unfinished
        # one (never from a FINDINGS stamp, whose finisher answers the findings), so only those rows are read for it
        findings = bool(r.report and r.report.review and r.report.review.verdict == 'FINDINGS')
        if r.state == 'DECISION' and not findings and (r.report is None or r.report.status in plan_wave.FINISH_STATUSES):
            replayable.add(r.name)
    if not candidates:
        return
    # NO git process per carrier or pair: the survey's graph (git_batch.Ancestry, kb/Work PB3582; the pairwise
    # `merge-base --is-ancestor` was 1,746 of the 4,636 git processes of this script's self-test, kb/Work PB3583). A
    # candidate is not MERGED (LANDED_CONTENT, above: classify() reads the graph's `on_base`), so its tip is no ancestor
    # of origin/main, and it is an ancestor of a carrier's tip exactly when it is one of the commits that tip has beyond
    # origin/main (graph.beyond). With no origin/main (a repository that never fetched one) the graph excludes nothing:
    # every commit of the carrier's tip.
    replay = survey_replays(rows, reports, graph, work_dir) if replayable else {}
    by_name = {r.name: r for r in rows}
    for r in candidates:
        c = next((c for c in carriers if c.tip != r.tip and r.tip in graph.beyond(c.tip)), None)
        if c:
            r.state, r.carrier = 'IN FLIGHT', c.name
            r.why = f'contained in {c.name} ({c.state}): its work lands through that branch; ' + evidence(r)
        elif r.name in replayable and (rp := replay.get(r.name)) and rp.carrier:
            r.state, r.carrier = 'IN FLIGHT', rp.carrier
            r.why = (f'replayed by {rp.carrier} ({by_name[rp.carrier].state}): every commit of it is on that branch, '
                     f'patch-equivalent or identical, so its work lands through it and the planner plans no finisher '
                     f'of its own (kb/Work PB3170, PB3652); ' + evidence(r))


def survey_replays(rows: list[Row], reports: list[Report], graph: Ancestry,
                   work_dir: pathlib.Path | None) -> dict[str, Replay]:
    """{branch: Replay} for the survey's unlanded branches, by the planner's own function (replay_view; kb/Work PB3652).
    A branch with a report is ranked by the position of its newest report in `reports` (oldest first), as the planner
    ranks it; a branch with no report is carried but carries nothing. `work_dir` (kb/Work): where an ABANDONED branch
    is recorded, which carries nothing and is no source (None: no register to read)."""
    pos = {id(rep): i for i, rep in enumerate(reports)}
    unlanded = [r for r in rows if r.tip and r.content not in LANDED_CONTENT]
    ranked = {r.name: pos[id(r.report)] for r in unlanded if r.report and id(r.report) in pos}
    if not ranked:
        return {}   # nobody can carry
    names = [r.name for r in unlanded if r.content in ('UNLANDED', 'CHECK')]
    abandoned = set(abandon_records(sorted(set(ranked) | set(names)), work_dir)) if work_dir and work_dir.is_dir() else set()
    return replay_view(ranked, names, abandoned, graph)


def findings_lead(v: dict, reports: list[Report], rows: list[Row]) -> str:
    """The note whose branch review recorded the last verdict `v` (review_findings.last_verdict): the report whose stamp
    names its file; else, for a wave's file (`review-w<wave>-<letter>.jsonl`), that group's report
    (`w<wave><letter>-PB<lead>-report.md`), which a review finisher rewrites for its own branch but never renames; else
    the report of the branch the verdict reviewed. '' when none is known: then no branch is exempt as the same note's."""
    import review_findings  # noqa: PLC0415
    path = os.path.normcase(os.path.abspath(v['file']))
    for rep in reports:
        if rep.review and rep.review.file and os.path.normcase(os.path.abspath(rep.review.file)) == path:
            return rep.lead
    m = review_findings.WAVE_FINDINGS.match(pathlib.Path(v['file']).name)
    hit = next((rep for rep in reports if rep.name.startswith(f'w{m["wave"]}{m["letter"]}-')), None) if m else None
    if hit:
        return hit.lead
    branch = v['branch'] or next(iter(v.get('branches') or []), '')   # a torn reading names its branches instead
    row = next((r for r in rows if branch and r.name == branch and r.report), None)
    return row.report.lead if row else ''


def review_answers(v: dict, r: Row, graph: Ancestry) -> bool:
    """Does `r`'s own review answer the findings of the reading `v` (kb/Work PB3404)? See findings_answered_by."""
    return findings_answered_by(v, r.report.review if r.report else None, graph)


def answered_by_carrier(findings_review, carrier_review, graph: Ancestry) -> bool:
    """The planner's side of the same rule (kb/Work PB3404): are the FINDINGS a report's stamp names (`findings_review`,
    whose findings file is read for its last verdict, the one reader) answered by the CLEAN review of the branch that
    carries it (`carrier_review`)? Then a finisher of the carried branch would fix again what the carrier's own review
    and finisher fixed, and the planner plans none."""
    import review_findings  # noqa: PLC0415  (scripts/orchestrator)
    if not findings_review or not findings_review.file:
        return False
    v = review_findings.last_verdict(pathlib.Path(findings_review.file))
    return bool(v) and findings_answered_by(v, carrier_review, graph)


def findings_answered_by(v: dict, rv, graph: Ancestry) -> bool:
    """Does the review `rv` (plan_wave.Review, or None) answer the findings of the reading `v` (kb/Work PB3404)? ONE FINDINGS CASE, ONE OWNER
    (review_findings.capped): a CAPPED verdict (the round after the last review finisher) belongs to its branch's
    next-wave finisher, which no review of a successor answers; an UNCAPPED one had a review finisher dispatched that
    came back without answering, and the successor that merged the branch is reviewed over the whole branch, the
    predecessor's commits as its own (wf_rolling_wave.js predecessorScope, case 'unanswered'), so a CLEAN review of
    a head that contains the verdict's head answers them. A torn reading answers nothing."""
    if v['torn'] or v['capped'] or not rv or rv.verdict != 'CLEAN' or not v['head']:
        return False
    return any(c.startswith(v['head']) for c in graph.beyond(graph.sha(rv.sha) or rv.sha))


def carrying_findings(rows: list[Row], graph: Ancestry, findings: list[dict] = (), reports: list[Report] = ()) -> None:
    """A WAITING TO LAND branch that carries ANOTHER note's unanswered review findings waits for them (kb/Work PB3381):
    its tip contains, not yet on main, the head at which a branch review's LAST verdict is FINDINGS with blocking
    findings (`findings`: review_findings.unanswered over review_findings.findings_files, the ONE reader the rolling
    wave's predecessorEnd reads too), and that review is of a different note (findings_lead). That is a same-file
    successor of a predecessor whose review ended FINDINGS: at the round cap, or with a review finisher that came back
    without answering them (BLOCKED, SPLIT, STALLED, killed). EACH CASE HAS ONE OWNER (kb/Work PB3404, review_answers):
    a CAPPED verdict's findings belong to the predecessor's next-wave finisher (plan_wave.py plans it), and the
    successor's own CLEAN stamp says nothing about them: landing it would put them on main. It reads IN FLIGHT, waiting
    on the branch that review stamped (`waits_on`), and is WAITING TO LAND again once a later round of that review
    records CLEAN or that head is on main. An UNCAPPED verdict's findings belong to the SUCCESSOR's review, which the
    wave told to cover the predecessor's commits as its own, so a successor CLEAN over a head that contains the
    verdict's head is not held: waiting for the predecessor's finisher to fix again what the successor's finisher fixed
    held finished work behind the slower of the two. The FINDINGS file decides, never a
    report's stamp (review R3-1): a review finisher rewrites the group's one report for its own branch with no Review
    line whatever it returns, so a hold read from the stamp released the successor exactly when nobody had answered
    them. A finisher of the SAME note carries the findings on purpose, to answer them, and lands.
    A TORN findings file (its reading's `torn`; review R5-1, R6-1: the survey lists the files by name, and any content
    problem, or a file it cannot open, is a tear) holds every WAITING TO LAND branch it names and every one whose tip
    contains, not yet on main, a head it names, the same note's included: no verdict is read from it. A file that names
    no branch (it could not be read, or no line of it is a record) holds the branches of the note whose review it
    records (findings_lead: the stamp naming it, or its wave name) and every branch containing their tips. The held
    row's `why` names the tear and the repair."""
    if not findings:
        return
    import review_findings  # noqa: PLC0415  (scripts/orchestrator: the repair command a torn file's row names)
    holds = []   # (reading, its note, the branches it holds by name, the heads it holds by containment)
    for v in findings:
        lead = findings_lead(v, list(reports), rows)
        if v['torn']:
            names = set(v['branches']) or {x.name for x in rows if lead and x.report and x.report.lead == lead}
            heads = [*v['heads'], *(x.tip for x in rows if x.name in names and x.tip)]
        else:
            names, heads = set(), [v['head']]
        holds.append((v, lead, names, [h for h in heads if h]))
    for r in rows:
        if r.state != 'WAITING TO LAND' or not r.tip or not r.report:
            continue
        unlanded = graph.beyond(r.tip)
        hit = next(((v, lead, names) for v, lead, names, heads in holds
                    if (v['torn'] or not (lead and lead == r.report.lead)) and not review_answers(v, r, graph)
                    and (r.name in names or any(c.startswith(h) for h in heads for c in unlanded))), None)
        if not hit:
            continue
        v, lead, names = hit
        r.state = 'IN FLIGHT'
        if v['torn']:
            r.waits_on = next(iter([*v['branches'], *sorted(names)]), pathlib.Path(v['file']).name)
            r.why = (f'carries {r.waits_on} ({lead or "note unknown"}), whose findings file {v["file"]} is TORN '
                     f'({v["torn"]}): a torn file holds every branch it names, whatever its records say, and no writer '
                     f'appends to it until the operator mends it ({review_findings.remedy(pathlib.Path(v["file"]), v["line"])}) '
                     '(kb/Work PB3381 review R5-1, R6-1); ' + evidence(r))
        else:
            r.waits_on = v['branch']
            r.why = (f'carries {v["branch"]} ({lead or "note unknown"}), whose review FINDINGS ({v["blocking"]}) at '
                     f'{v["head"][:12]} are unanswered (round {v["round"]} of {v["file"]}, its last verdict): it '
                     'lands once that note\'s finisher has answered them and landed it (kb/Work PB3381); ' + evidence(r))


def carrying_landing_branch(rows: list[Row], graph: Ancestry) -> None:
    """A branch is landed ONCE (kb/Work PB3693): of two branches where one CONTAINS the other's tip, not yet on main, and
    both have a landing of their own (WAITING TO LAND, or ENQUEUED: a pull request in the merge queue), exactly one
    lands the contained work. contained() lets a predecessor reviewed CLEAN land on its own (kb/Work PB3381), so the
    survey must also keep its carrier from landing the same commits again, with the predecessor's DEVLOG entry folded
    into the carrier's (the PB3665 landing of 2026-10-24: two pull requests, two merge bases, a PublicAPI line twice).
      * a WAITING TO LAND carrier whose predecessor is WAITING TO LAND or ENQUEUED is IN FLIGHT, waiting on it
        (`waits_on`), and is WAITING TO LAND again once the predecessor is on main: its landing then merges main first,
        has ONE merge base, and stamps only its own entry (`enqueue.open_predecessors` and `devlog.stamp` hold the same
        rule at the landing itself);
      * a WAITING TO LAND branch contained in an ENQUEUED one lands through it (`carrier`): the queued pull request
        already carries its commits and stamped its entry.
    A STOPPED, BOUNCED or NOT QUEUED branch has a finisher to wait for, and holds nothing. The states read are the ones
    the survey assigned BEFORE this function changed any (a chain A, B, C holds C behind B and B behind A alike)."""
    landing = [r for r in rows if r.state in ('WAITING TO LAND', 'ENQUEUED') and r.tip]
    held = []
    for r in (x for x in landing if x.state == 'WAITING TO LAND'):
        carried_by = next((q for q in landing if q.state == 'ENQUEUED' and q.tip != r.tip and r.tip in graph.beyond(q.tip)), None)
        pred = next((p for p in landing if p.tip != r.tip and p.tip in graph.beyond(r.tip)), None)
        if carried_by or pred:
            held.append((r, carried_by, pred))
    for r, carried_by, pred in held:
        r.state = 'IN FLIGHT'
        if carried_by:
            r.carrier = carried_by.name
            r.why = (f'contained in {carried_by.name} (ENQUEUED): its pull request already carries these commits and '
                     'its stamped entry, so this branch lands through it and reads LANDED once it merges '
                     '(kb/Work PB3693); ' + evidence(r))
        else:
            r.waits_on = pred.name
            r.why = (f'carries {pred.name} ({pred.state}), which lands on its own: this branch lands once that pull '
                     'request is on main, so its landing has one merge base and stamps only its own DEVLOG entry '
                     '(kb/Work PB3693); ' + evidence(r))


def owned(rows: list[Row]) -> set[str]:
    """The branches with a live owner of their OWN: IN FLIGHT by lifecycle() (a live agent's lock, its dispatch still
    out, a touch inside --min-age, which is how a resumed branch whose tip moved past its report reads), never by
    contained(). A contained branch reads IN FLIGHT because its work lands through its carrier, and the carrier may be
    any finished branch that merged it (a same-file successor of ANOTHER note, MANDATORY-PRACTICES O2): that says
    nothing about who works on THIS branch. The planner's FINDINGS arm reads this set (plan_wave.plan `owned`, kb/Work
    PB3311): the bare state made a carried FINDINGS branch wait for an answer nobody was writing (review R1-1)."""
    return {r.name for r in rows if r.state == 'IN FLIGHT' and not r.carrier and not r.waits_on}


def locked(rows: list[Row], live: Callable[[str | None], bool] = live_lock) -> set[str]:
    """The LOCK-ONLY half of owned() (kb/Work PB3653): the branches whose worktree a live agent holds (live_lock, the
    one liveness rule lifecycle() reads first). owned() folds three signals into one set: this lock, a dispatch-ledger
    group still out, and a touch inside --min-age. The planner's finisher arm needs them apart. A ledger dispatch
    leaves its own record (plan_wave.held_by_dispatch), and a just-touched SPLIT branch is the normal state of one
    whose agent has just returned (holding it would stall every finisher for --min-age), but a lock is someone working
    IN that worktree right now with no record anywhere else, e.g. an agent the operator started by hand on a SPLIT
    branch: a finisher planned over it would cherry-pick the same commits twice (plan_wave.plan's branch-unit hold,
    kb/Work PB2974). `live`: the survey's liveness probe (a self-test's fake)."""
    return {r.name for r in rows if live(r.lock)}


def verdict(rows: list[Row]) -> str:
    n = {s: sum(1 for r in rows if r.state == s) for s in STATES}
    return (f'=== BRANCH LIFECYCLE: {len(rows)} · {n["IN FLIGHT"]} in flight · {n["WAITING FOR REVIEW"]} waiting for '
            f'review · {n["WAITING TO LAND"]} waiting to land · {n["STOPPED"]} stopped · {n["ENQUEUED"]} enqueued · '
            f'{n["NOT QUEUED"]} not queued · {n["BOUNCED"]} bounced · '
            f'{n["LANDED"]} landed (--apply deletes) · {n["DECISION"]} need a DECISION'
            + (' (owner rule 2026-10-08, kb/Work PB2600: land it or abandon it in its kb/Work note, then delete) ===' if n['DECISION'] else ' ==='))


def note_rank(work_dir: pathlib.Path | None = None) -> Callable[[str], tuple[int, int]]:
    """`rank(note id)`: a sort key, most urgent first: minus the note's harm weight (plan_wave.harm_weights, the
    planner's weights), then a compiler note (0) before a process-only one (1). A note main does not hold yet (filed on
    its own branch) ranks as harmless process work."""
    sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
    import work  # noqa: PLC0415
    import plan_wave  # noqa: PLC0415
    weights = plan_wave.harm_weights()
    items = {i.get('id'): i for i in (work.load(work_dir) if work_dir else work.load())}

    def rank(note: str) -> tuple[int, int]:
        i = items.get(note)
        if i is None:
            return (0, 1)
        return (-sum(w for k, w in weights.items() if i.get(k) is True), 1 if i.get('process_only') is True else 0)
    return rank


def landing_order(rows: list[Row], rank: Callable[[str], tuple[int, int]]) -> list[Row]:
    """The WAITING TO LAND rows in the order the landing lane lands them, one at a time (kb/Work PB2981's ranking, kept
    by PB3023): by their report's lead note (`rank`: the most harmful first, a compiler fix before process work), then
    the OLDEST report first, then the name."""
    return sorted((r for r in rows if r.state == 'WAITING TO LAND'),
                  key=lambda r: (*rank(r.report.lead), -r.report.age_min, r.name))


#: What may stand between a branch's name and the word ABANDONED in its record: markup and punctuation, one
#: parenthetical ("(wave 1077 group E, SPLIT)"), and the copula or the noun the record puts there ("branch `x` is
#: ABANDONED"). Any other word, above all another branch's name, means the word is about something else; so does
#: `not`, which is why "not abandoned" (the opposite decision) cannot match.
RECORD_GAP = r'(?:[\s`*_:,;"\'’]|\([^()]*\)|\b(?:is|was|are|now|branch|worktree)\b)*'


def branch_name(branch: str) -> re.Pattern[str]:
    """`branch` as a whole word: no name character (word, `.`, `-`) on either side, nor a path separator before it."""
    return re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])')


def abandon_record(branch: str, work_dir: pathlib.Path) -> str | None:
    """The kb/Work note that records the decision to abandon `branch`: a line where the word ABANDONED directly
    follows the branch (as a whole word), with only `RECORD_GAP` between them. A record names a successor too ("`old`
    ABANDONED, superseded by `new`", or "`new` carries it, so `old` is ABANDONED"), and that line must not pass for a
    decision to abandon `new`. The rule reads the line alone: a version that asked the survey which other names were
    branches stopped refusing once `old` itself was deleted, because the next survey no longer knew `old` (kb/Work
    PB3161, review finding R1-1). None when no note says so (the owner's rule: the decision is recorded BEFORE the
    delete)."""
    return abandon_records([branch], work_dir).get(branch)


def abandon_records(branches: list[str], work_dir: pathlib.Path) -> dict[str, str]:
    """{branch: the kb/Work note that records its abandonment} for each of `branches` that has one: abandon_record's
    rule for a whole list in ONE pass over the notes (the planner asks it of every unlanded branch, kb/Work PB3170, and
    most notes never say the word)."""
    records = {b: re.compile(branch_name(b).pattern + RECORD_GAP + r'\babandoned\b', re.I) for b in branches}
    found: dict[str, str] = {}
    for p in sorted(work_dir.glob('*.md')):
        text = p.read_text(encoding='utf-8', errors='replace')
        if 'abandoned' not in text.lower():
            continue
        lines = text.splitlines()
        for b, record in records.items():
            if b not in found and any(record.search(l) for l in lines):
                found[b] = p.stem
    return found


def prune_stale_remote_refs(*, repo=REPO, say: Callable[[str], None] = print) -> int:
    """Drop the remote-tracking refs whose branch is gone from origin (`git remote prune origin`), so that
    `refs/remotes/origin/*` says what origin holds before drop_remote reads it (kb/Work PB3409). GitHub deletes a merged
    pull request's head branch itself (`delete_branch_on_merge`, ruleset.py), and nothing pruned the tracking refs: 178
    were listed beside 21 on origin, and a `git push --delete` of one failed with `remote ref does not exist`, which
    --apply reported as an error beside a real one. Returns how many were dropped. A prune that fails (no network, a
    refused credential) is said and drops nothing: the deletes that follow then report their own real failures."""
    def tracking() -> set[str]:
        return set(git('for-each-ref', '--format=%(refname)', 'refs/remotes/origin/', cwd=repo).stdout.split())
    before = tracking()
    res = git('remote', 'prune', 'origin', cwd=repo)
    if res.returncode != 0:
        say(f'could not prune the stale origin refs: {res.stderr.strip()[:120]}')
        return 0
    dropped = len(before - tracking())
    if dropped:
        say(f'pruned {dropped} remote-tracking ref(s) whose branch origin no longer holds')
    return dropped


def drop_remote(branch: str, tip: str, *, repo=REPO, say: Callable[[str], None] = print) -> None:
    """Delete `origin/<branch>` with the local branch just removed, when it holds nothing the local one did not (its
    commit is the local tip or an ancestor of it). A squash landing leaves the pushed branch on origin, and nothing else
    ever deleted one (kb/Work PB2708: `note-budget-borrow`, `r1-pb2118`). A remote that moved on is kept, said so. It
    reads `refs/remotes/origin/<branch>` as origin's state, which apply() made true with prune_stale_remote_refs: a
    branch origin already deleted has no ref here, so a failing push is a real failure (PB3409)."""
    remote = git('rev-parse', '--verify', '--quiet', f'refs/remotes/origin/{branch}', cwd=repo).stdout.strip()
    if not remote:
        return
    if not tip or git('merge-base', '--is-ancestor', remote, tip, cwd=repo).returncode != 0:
        say(f'kept origin/{branch}: it holds commits the local branch did not ({remote[:12]})')
        return
    res = git('push', '-q', 'origin', '--delete', branch, cwd=repo)
    say(f'deleted origin/{branch}' if res.returncode == 0 else
        f'could not delete origin/{branch}: {res.stderr.strip()[:120]}')


def apply(rows: list[Row], *, repo=REPO, include_locked: bool = False, abandon: list[str] = (), min_age: int = 180,
          work_dir: pathlib.Path = REPO / 'kb' / 'Work', say: Callable[[str], None] = print) -> int:
    """Remove the LANDED rows; with --include-locked also a landed, clean, idle worktree that is IN FLIGHT only because
    it was locked by hand (no pid); and each branch named by --abandon that is a clean, idle DECISION row whose kb/Work
    note records the decision to abandon it (abandon_record). Nothing is archived: the classification IS the protection
    (owner 2026-10-08 00:52 PDT, kb/Work PB2600: "Archiving to a bundle serves no use and wastes disk space").
    Returns the count removed."""
    def hand_locked(r: Row) -> bool:
        return r.lock == '' and r.content in LANDED_CONTENT and not r.dirty and r.age_min >= min_age
    doomed = [r for r in rows if r.content != 'DETACHED' and (
        r.state == 'LANDED' or (include_locked and r.state == 'IN FLIGHT' and hand_locked(r)))]
    by_name = {r.name: r for r in rows}
    for b in abandon:
        r = by_name.get(b)
        if r is None or r.state != 'DECISION' or r.dirty or r.content == 'DETACHED':
            say(f'not abandoned: {b} is {"no branch" if r is None else r.state + (" with uncommitted edits" if r and r.dirty else "")}'
                ' (only a clean DECISION row can be abandoned)')
        elif not (note := abandon_record(b, work_dir)):
            say(f'not abandoned: no kb/Work note records the decision to abandon {b} (a line where the word ABANDONED '
                f'directly follows its name, with the reason); record it there first')
        else:
            say(f'abandoning {b}: decision recorded in kb/Work/{note}.md')
            doomed.append(r)
    removed = 0
    prune_stale_remote_refs(repo=repo, say=say)
    for r in doomed:
        if r.worktree:
            # re-measured at the moment of removal: an agent may have started writing since the survey
            if uncommitted(r.worktree):
                say(f'skipped (dirty worktree): {r.name}')
                continue
            if r.lock is not None:
                git('worktree', 'unlock', r.worktree, cwd=repo)
            res = git('worktree', 'remove', '--force', r.worktree, cwd=repo)
            if res.returncode != 0:
                say(f'could not remove the worktree of {r.name}: {res.stderr.strip()[:120]} (stop csharp-ls and re-run)')
                continue
        tip = git('rev-parse', '--verify', '--quiet', f'refs/heads/{r.name}', cwd=repo).stdout.strip()
        res = git('branch', '-D', r.name, cwd=repo)
        if res.returncode != 0:
            say(f'could not delete the branch {r.name}: {res.stderr.strip()[:120]}')
            continue
        removed += 1
        drop_remote(r.name, tip, repo=repo, say=say)
    git('worktree', 'prune', cwd=repo)
    say(f'removed {removed} branches and their worktrees')
    return removed


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--apply', action='store_true', help='remove the LANDED rows (and each --abandon branch)')
    ap.add_argument('--abandon', action='append', default=[], metavar='BRANCH',
                    help='with --apply: also remove this DECISION branch, once a kb/Work note records it ABANDONED')
    ap.add_argument('--include-locked', action='store_true', help='also take a LANDED worktree locked by hand (no pid)')
    ap.add_argument('--min-age', type=int, default=180, help='a branch touched in the last N minutes is IN FLIGHT (default 180)')
    ap.add_argument('--stale-hours', type=float, default=24,
                    help='a DONE report older than this that never landed needs a DECISION (default 24)')
    ap.add_argument('--reports', type=pathlib.Path, help='the fix-lane reports directory (default coord.reports_dir())')
    ap.add_argument('--brief', action='store_true',
                    help='the verdict line and the rows someone must act on: WAITING FOR REVIEW, WAITING TO LAND (in '
                         'landing order), STOPPED, NOT QUEUED (with how long it has waited), BOUNCED and DECISION')
    ap.add_argument('--json', action='store_true', help='the survey as JSON: every row with its state and evidence')
    ap.add_argument('--no-fetch', action='store_true')
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if not a.no_fetch:
        git('fetch', '-q', 'origin')
    rows = live_survey(reports_dir=a.reports, min_age=a.min_age, stale_hours=a.stale_hours)
    order = landing_order(rows, note_rank())
    if a.json:
        print(json.dumps({'verdict': verdict(rows), 'rows': [dataclasses.asdict(r) for r in rows],
                          'landing_order': [r.name for r in order]}, indent=1))
        return 0
    for st in STATES:
        for r in order if st == 'WAITING TO LAND' else sorted((r for r in rows if r.state == st), key=lambda r: r.name):
            if a.brief and st in ('IN FLIGHT', 'ENQUEUED', 'LANDED'):
                continue
            print(f'{st:15} {r.name}{" [worktree]" if r.worktree and r.content != "DETACHED" else ""} — {r.why}')
    print(verdict(rows))
    if not a.apply:
        if not a.brief:
            print('dry run: nothing removed. --apply removes the LANDED rows; --apply --abandon <branch> a DECISION row '
                  'whose kb/Work note records it ABANDONED.')
        return 0
    apply(rows, include_locked=a.include_locked, abandon=a.abandon, min_age=a.min_age)
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    """A scratch repository with one branch in every lifecycle state, surveyed and then pruned (--apply)."""
    from scratch_repo import ScratchRepo, ScratchRepoError, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    fails = []
    check = lambda ok, what: None if ok else fails.append(what)  # noqa: E731
    tmp = scratch_dir('prune-selftest-')
    try:
        isolate()      # never act on the caller's repository (kb/Work PB1719)
        # Every fixture command is CHECKED (ScratchRepoError carries git's message): an unchecked command that failed
        # while the fixture was built left a branch where the fixture never meant it, and a lifecycle FAIL on that
        # branch was the only trace (merge-group run 39193754708: `lane-free` read MERGED, its own commit absent).
        scratch = ScratchRepo.init(tmp / 'repo')
        repo = scratch.path

        def commit(path, text, msg, cwd=repo):
            scratch.write({path: text}, cwd)
            scratch.git('add', path, cwd=cwd)
            scratch.git('commit', '-q', '-m', msg, cwd=cwd)

        commit('src/A.cs', 'class Alpha {}\n', 'base')
        # kb/Work PB3713: a keyed catalogue (merge_drivers merges it by key) the LANDED proof merges through the drivers
        keyed = lambda *ks: '{\n' + ',\n'.join(f' "{k}": {{\n  "refuse-at": [\n   "85"\n  ]\n }}' for k in ks) + '\n}\n'  # noqa: E731
        commit('.gitattributes', 'd.json merge=cobol-keyed-object\n', 'the driver attribute')
        commit('d.json', keyed('a', 'c'), 'the catalogue')
        scratch.origin(tmp / 'origin.git')
        scratch.git('push', '-q', 'origin', 'main')
        scratch.git('fetch', '-q', 'origin')
        # merged: an ancestor of main. landed-by-content: re-applied on main as a different commit.
        scratch.git('branch', 'merged')
        scratch.git('switch', '-q', '-c', 'landed-content')
        commit('src/B.cs', 'class Bravo {}\n', 'bravo')
        scratch.git('switch', '-q', 'main')
        commit('src/B.cs', 'class Bravo {}\n', 'bravo, as the train re-applied it')
        scratch.git('push', '-q', 'origin', 'main')
        scratch.git('fetch', '-q', 'origin')
        # `key-landed` appended `x` to the catalogue; another carrier put `x` on main beside its own `y`: a PLAIN
        # merge-tree conflicts there, the drivers merge it to main's own tree (kb/Work PB3713)
        scratch.git('switch', '-q', '-c', 'key-landed', 'main')
        commit('d.json', keyed('a', 'c', 'x'), 'key-landed')
        scratch.git('switch', '-q', 'main')
        commit('d.json', keyed('a', 'c', 'y', 'x'), 'main holds x, beside its own y')
        scratch.git('push', '-q', 'origin', 'main')
        scratch.git('fetch', '-q', 'origin')
        # unlanded with no report (DECISION), unlanded with a DONE report (WAITING TO LAND), a SPLIT report (DECISION),
        # a DONE report whose lead note is already landed (superseded: DECISION), a DONE report 30 h old (stale:
        # DECISION), a DONE report followed by a commit no report describes (resumed since: DECISION, whatever the
        # report's age, kb/Work PB3181), a ledger slug, a fresh one
        main_at = scratch.git('rev-parse', 'main')
        for b, f in (('orphan', 'src/C.cs'), ('done-branch', 'src/D.cs'), ('split-branch', 'src/E.cs'),
                     ('w9999a-ledger', 'src/F.cs'), ('fresh', 'src/G.cs'), ('superseded', 'src/H.cs'),
                     ('stale-done', 'src/I.cs'), ('resumed', 'src/J.cs'),
                     # THE BRANCH REVIEW (kb/Work PB3027): DONE with no stamp, a FINDINGS stamp at its tip, a CLEAN
                     # stamp at a head the branch has since moved past; `pred` carries no report and lands through
                     # `succ`, which was cut from it and is DONE and reviewed CLEAN
                     ('unreviewed', 'src/N.cs'), ('review-findings', 'src/O.cs'), ('reviewed-moved', 'src/S.cs'),
                     ('pred', 'src/T.cs'), ('fnd-pred', 'src/W.cs'), ('reviewed-stamped', 'src/Y.cs'),
                     # kb/Work PB3394: a FINDINGS stamp at a head the branch moved past (its answers committed after
                     # the review), and one followed only by the DEVLOG stamp
                     ('findings-moved', 'src/FM.cs'), ('findings-stamped', 'src/FS.cs'), ('findings-resumed', 'src/FR.cs'),
                     # ONE RULE FOR A FINISHED BRANCH (kb/Work PB3181, PB3125): the report's coverage, the review's,
                     # and the landing lane's newest attempt at the tip. `lane-pred` carries no report and lands
                     # through `lane-stopped`, cut from it; the other `lane-*` are each one attempt's outcome; a
                     # DISCHARGED report with branch work, one whose HEAD is its base, and a DONE report naming no HEAD
                     ('lane-pred', 'src/LP.cs'), ('lane-free', 'src/LF.cs'), ('lane-done', 'src/LD.cs'),
                     ('lane-running', 'src/LR.cs'), ('discharged', 'src/DS.cs'), ('discharged-empty', 'src/DE.cs'),
                     ('no-head', 'src/NH.cs'), ('no-head-bare', 'src/NB.cs')):
            scratch.git('switch', '-q', '-c', b, 'main')
            commit(f, f'class {b.replace("-", "_").title()}X {{}}\n', b)
        scratch.git('switch', '-q', '-c', 'lane-stopped', 'lane-pred')
        commit('src/LS.cs', 'class LaneStoppedX {}\n', 'lane-stopped')
        # kb/Work PB3693: a branch is landed ONCE. `wpred` and `wsucc` (cut from it) are both reviewed CLEAN, so both are
        # WAITING TO LAND, and the successor waits for the predecessor's pull request instead of folding its entry.
        # `qpred` was reviewed CLEAN and `qsucc`, cut from it, is already in the merge queue: the queued pull request
        # carries `qpred`'s commits, so `qpred` lands through it.
        for b, f, base in (('wpred', 'src/WP.cs', 'main'), ('wsucc', 'src/WS.cs', 'wpred'), ('qpred', 'src/QP.cs', 'main'),
                           ('qsucc', 'src/QS.cs', 'qpred')):
            scratch.git('switch', '-q', '-c', b, base)
            commit(f, f'class {b.title()}X {{}}\n', b)
        once_at = {b: scratch.git('rev-parse', b) for b in ('wpred', 'wsucc', 'qpred', 'qsucc')}
        # kb/Work PB3652: an UNFINISHED (SPLIT) branch another branch carries is no decision. `rp-pred` is replayed by
        # cherry-pick (the planner's FINISH-FIRST pred) in `rp-succ`; `rp-part` has one commit `rp-partc` lacks;
        # `rp-gone` is replayed by `rp-dead`, which a kb/Work note records ABANDONED (a dead branch carries nothing);
        # `an-pred` is an ANCESTOR of `an-succ` (a finished branch awaiting its review)
        scratch.git('switch', '-q', '-c', 'rp-pred', 'main')
        commit('src/RP.cs', 'class RpPredX {}\n', 'rp-pred')
        rp_pred = scratch.git('rev-parse', 'rp-pred')
        scratch.git('switch', '-q', '-c', 'rp-succ', 'main')
        scratch.git('cherry-pick', rp_pred)
        commit('src/RS.cs', 'class RpSuccX {}\n', 'rp-succ')
        scratch.git('switch', '-q', '-c', 'rp-part', 'main')
        commit('src/RQ.cs', 'class RpPartOneX {}\n', 'rp-part one')
        rp_part_one = scratch.git('rev-parse', 'rp-part')
        commit('src/RR.cs', 'class RpPartTwoX {}\n', 'rp-part two')
        scratch.git('switch', '-q', '-c', 'rp-partc', 'main')
        scratch.git('cherry-pick', rp_part_one)
        commit('src/RT.cs', 'class RpPartcX {}\n', 'rp-partc')
        scratch.git('switch', '-q', '-c', 'rp-gone', 'main')
        commit('src/RG.cs', 'class RpGoneX {}\n', 'rp-gone')
        rp_gone = scratch.git('rev-parse', 'rp-gone')
        scratch.git('switch', '-q', '-c', 'rp-dead', 'main')
        scratch.git('cherry-pick', rp_gone)
        commit('src/RD.cs', 'class RpDeadX {}\n', 'rp-dead')
        scratch.git('switch', '-q', '-c', 'an-pred', 'main')
        commit('src/AP.cs', 'class AnPredX {}\n', 'an-pred')
        scratch.git('switch', '-q', '-c', 'an-succ', 'an-pred')
        commit('src/AS.cs', 'class AnSuccX {}\n', 'an-succ')
        # kb/Work PB3386: a branch is carried whatever its own report says. `cw-pred` (SPLIT), `cw-stale` (a DONE report
        # 30 h old), `cw-moved` (a DONE report, then a commit it does not describe) and `cw-lost` (a DONE report whose lead
        # note is already landed) are each an ancestor of `cw-succ`, DONE and reviewed CLEAN at its tip (WAITING TO LAND)
        scratch.git('switch', '-q', '-c', 'cw-pred', 'main')
        commit('src/CP.cs', 'class CwPredX {}\n', 'cw-pred')
        scratch.git('switch', '-q', '-c', 'cw-stale', 'cw-pred')
        commit('src/CS.cs', 'class CwStaleX {}\n', 'cw-stale')
        scratch.git('switch', '-q', '-c', 'cw-moved', 'cw-stale')
        commit('src/CM.cs', 'class CwMovedX {}\n', 'cw-moved')
        cw_moved_at = scratch.git('rev-parse', 'cw-moved')
        commit('src/CM.cs', 'class CwMovedX { int AfterTheReport; }\n', 'a commit no report describes')
        scratch.git('switch', '-q', '-c', 'cw-lost', 'cw-moved')
        commit('src/CL.cs', 'class CwLostX {}\n', 'cw-lost')
        scratch.git('switch', '-q', '-c', 'cw-succ', 'cw-lost')
        commit('src/CU.cs', 'class CwSuccX {}\n', 'cw-succ')
        scratch.git('switch', '-q', 'main')
        scratch.write({'kb/Work/PB9.md': '`rp-dead` is ABANDONED, superseded by nothing.\n'})
        lane_at = {b: scratch.git('rev-parse', b) for b in ('lane-stopped', 'lane-free', 'lane-done', 'lane-running',
                                                                  'discharged', 'discharged-empty', 'no-head', 'no-head-bare')}
        resumed_at = scratch.git('rev-parse', 'resumed')
        scratch.git('switch', '-q', 'resumed')
        commit('src/J.cs', 'class ResumedX { int AfterTheReport; }\n', 'a commit no report describes')
        reviewed_at = scratch.git('rev-parse', 'reviewed-moved')
        scratch.git('switch', '-q', 'reviewed-moved')
        commit('src/S.cs', 'class ReviewedMovedX { int AfterTheReview; }\n', 'a commit after the review')
        # `reviewed-stamped`: CLEAN at a head, then only the DEVLOG stamp enqueue.py makes: still reviewed (kb/Work PB3023)
        stamped_at = scratch.git('rev-parse', 'reviewed-stamped')
        scratch.git('switch', '-q', 'reviewed-stamped')
        commit('DEVLOG/0042-the-fix.md', '## Entry 42 — 2026-10-11 20:00 PDT — the fix\n\nWhy.\n', 'DEVLOG 42: stamp')
        fmoved_at = scratch.git('rev-parse', 'findings-moved')
        scratch.git('switch', '-q', 'findings-moved')
        commit('src/FM.cs', 'class FindingsMovedX { int R1_1Answered; }\n', 'review R1-1: answered after the review')
        # kb/Work PB3446: a FINDINGS stamp at the report's HEAD, then a commit NO report describes (a resumed agent)
        fresumed_at = scratch.git('rev-parse', 'findings-resumed')
        scratch.git('switch', '-q', 'findings-resumed')
        commit('src/FR.cs', 'class FindingsResumedX { int AfterTheReport; }\n', 'a commit no report describes')
        fstamped_at = scratch.git('rev-parse', 'findings-stamped')
        scratch.git('switch', '-q', 'findings-stamped')
        commit('DEVLOG/0043-the-findings.md', '## Entry 43 — 2026-10-11 21:00 PDT — findings\n\nWhy.\n', 'DEVLOG 43: stamp')
        scratch.git('switch', '-q', '-c', 'succ', 'pred')
        commit('src/V.cs', 'class SuccessorX {}\n', 'succ')
        # `fnd-pred` was reviewed FINDINGS; a later wave's finisher `fnd-fix` merged it and was reviewed CLEAN
        scratch.git('switch', '-q', '-c', 'fnd-fix', 'fnd-pred')
        commit('src/W2.cs', 'class FindingsFixedX {}\n', 'the findings answered')
        # kb/Work PB3381: `cpred` was reviewed CLEAN and `csucc`, its same-file successor cut from it, is not reviewed
        # yet: the predecessor lands at once, never held behind its successor. `xpred` ended FINDINGS at the cap and
        # `xsucc` (another note) merged it and was reviewed CLEAN: it carries xpred's unanswered findings, so it waits.
        for b, f in (('cpred', 'src/CP.cs'), ('xpred', 'src/XP.cs')):
            scratch.git('switch', '-q', '-c', b, 'main')
            commit(f, f'class {b.title()}X {{}}\n', b)
        # Review R1-2: `upred` is DONE with NO stamp (its reviewer never returned: NO-RESULT) and `usucc`, its successor,
        # merged it and was reviewed CLEAN over the whole branch: an unreviewed checkpoint never lands alone, so `upred`
        # lands through `usucc` (IN FLIGHT, carried), never WAITING FOR REVIEW for a second reviewer.
        scratch.git('switch', '-q', '-c', 'upred', 'main')
        commit('src/UP.cs', 'class UpredX {}\n', 'upred')
        # Review R3-1: `rpred` was reviewed FINDINGS (its findings file says so) and its review finisher came back
        # BLOCKED with NO commit, its branch `rpred-fix` fast-forwarded to rpred's head, after REWRITING the group's one
        # report for `rpred-fix` with no Review line, as its contract says. `rsucc` (another note) merged rpred and was
        # reviewed CLEAN: no report stamps rpred's findings any more, yet rsucc carries them, so it waits.
        scratch.git('switch', '-q', '-c', 'rpred', 'main')
        commit('src/RP.cs', 'class RpredX {}\n', 'rpred')
        # Review R4-1 (the reviewer's probe): `tpred` ended FINDINGS at the round cap (round 3), and a round-4 review
        # was killed mid-append of its first finding, leaving a torn line. `tsucc` (another note) merged tpred and was
        # reviewed CLEAN. The tear answers nothing, so tsucc still carries tpred's unanswered findings and waits.
        scratch.git('switch', '-q', '-c', 'tpred', 'main')
        commit('src/TP.cs', 'class TpredX {}\n', 'tpred')
        scratch.git('branch', 'rpred-fix', 'rpred')
        scratch.git('switch', '-q', '-c', 'rsucc', 'rpred')
        commit('src/RS.cs', 'class RsuccX {}\n', 'rsucc')
        # kb/Work PB3267: a FINDINGS-stamped branch is a CARRIER (the planner owns its finisher), whatever its
        # predecessors' own report: `gpred` (no report) lands with `gsucc` (FINDINGS at its tip, DECISION), and a
        # FINDINGS predecessor `hpred` lands with `hsucc`, a plain successor not yet reviewed (WAITING FOR REVIEW)
        for b in ('gpred', 'hpred'):
            scratch.git('switch', '-q', '-c', b, 'main')
            commit(f'src/{b.title()}.cs', f'class {b.title()}X {{}}\n', b)
        for b, base, f in (('csucc', 'cpred', 'src/CS.cs'), ('xsucc', 'xpred', 'src/XS.cs'), ('usucc', 'upred', 'src/US.cs'),
                           ('tsucc', 'tpred', 'src/TS.cs'), ('gsucc', 'gpred', 'src/GS.cs'), ('hsucc', 'hpred', 'src/HS.cs')):
            scratch.git('switch', '-q', '-c', b, base)
            commit(f, f'class {b.title()}X {{}}\n', b)
        tips = {b: scratch.git('rev-parse', b) for b in ('done-branch', 'review-findings', 'succ', 'fnd-pred', 'fnd-fix',
                                                                'unreviewed', 'reviewed-moved', 'cpred', 'csucc',
                                                                'xpred', 'xsucc', 'upred', 'usucc', 'pred', 'rpred',
                                                                'rsucc', 'tpred', 'tsucc', 'gsucc', 'hpred', 'hsucc', 'findings-moved')}
        # modified-only: an unlanded change to an EXISTING file that declares no new name (train 1039b review: the
        # name test called it LANDED, and --apply would have deleted it)
        scratch.git('switch', '-q', '-c', 'modified-only', 'main')
        commit('src/A.cs', 'class Alpha { int Fixed() => 1; }\n', 'a method-body fix')
        # THE WORK REACHED MAIN AS OTHER COMMITS (kb/Work PB2708). `carried`: train 9999 (its manifest, its trailer
        # commit, its `Train 9999:` commit) took its head and the lander edited it in the train, so its own merge would
        # change main; `carried-moved`: the same, but the branch got a commit after the head the train took;
        # `unlanded-train`: named with its head by the manifest of train 7777, which never landed; `add-del`: a name one
        # commit added and the next deleted; `picked`: its commit cherry-picked onto main, which then edited the file;
        # `picked-lost`: the same, but main renamed the class it declares
        landed_coord = tmp / 'coord-landed'
        (landed_coord / 'scratch').mkdir(parents=True)
        manifest: list[dict] = []

        def train_commit(branch, path, text, cluster):
            tip = scratch.git('rev-parse', branch)
            base = scratch.git('merge-base', branch, 'main')      # where the branch was cut, as the train applier recorded
            manifest.append({'cluster': cluster, 'branch': branch, 'base': base, 'head': tip})
            # the body a lander rewrote: only the trailer survives (train 1053's 53A and 53G)
            commit(path, text, f'{cluster} PB1: the cluster as the lander edited it\n\nTrain-Cluster: {cluster}\n')
        scratch.git('switch', '-q', '-c', 'unlanded-train', 'main')
        commit('src/U.cs', 'class UniformUnlanded {}\n', 'unlanded-train')
        (landed_coord / 'scratch' / 'train7777-manifest.json').write_text(json.dumps(
            [{'cluster': 'K', 'branch': 'unlanded-train', 'base': scratch.git('rev-parse', 'main'),
              'head': scratch.git('rev-parse', 'unlanded-train')}]), encoding='utf-8', newline="\n")
        for b, f, name in (('carried', 'src/K.cs', 'KiloCarried'), ('carried-moved', 'src/M.cs', 'MikeMoved'),
                           ('picked', 'src/P.cs', 'PapaPicked'), ('picked-lost', 'src/Q.cs', 'QuebecLost'),
                           # kb/Work PB3474: a file added under tools/ is work, not the claude-skills submodule's gitlink
                           ('tools-only', 'tools/Extra/New.cs', 'ToolsOnly')):
            scratch.git('switch', '-q', '-c', b, 'main')
            commit(f, f'class {name} {{}}\n', b)
        scratch.git('switch', '-q', '-c', 'add-del', 'main')
        commit('src/A.cs', 'class Alpha {}\nclass TransientName {}\n', 'a WIP name')
        commit('src/A.cs', 'class Alpha { int Kept() => 2; }\n', 'the WIP name deleted, a body fix kept')
        # `ejected` changes only an existing file (no added file to read UNLANDED by), so the train record decides it
        scratch.git('switch', '-q', '-c', 'ejected', 'main')
        commit('src/A.cs', 'class Alpha { int Ejected() => 3; }\n', 'a method-body fix its train ejected')
        scratch.git('switch', '-q', 'main')
        # an EARLIER train reusing the letters lands after train 9999's members were cut (train 1056 before train 1055,
        # 2026-10-11), so their base..last range holds its K and E; `ejected` is train 9999's cluster E in the manifest,
        # but its lander ejected it: no E commit in train 9999, and the earlier train's E must not carry it
        commit('src/K0.cs', 'class KiloEarlier {}\n', 'K PB2: an earlier train\'s cluster K\n\nTrain-Cluster: K\n')
        commit('src/E0.cs', 'class EchoEarlier {}\n', 'E PB3: an earlier train\'s cluster E\n\nTrain-Cluster: E\n')
        commit('DEVLOG/9998-train.md', 'train 9998\n', 'Train 9998: DEVLOG 0, the earlier train\'s last commit')
        train_commit('carried', 'src/K.cs', 'class KiloCarried { int EditedInTrain; }\n', 'K')
        k9999 = scratch.git('log', '-1', '--format=%h')
        train_commit('carried-moved', 'src/M.cs', 'class MikeMoved { int EditedInTrain; }\n', 'M')
        manifest.append({'cluster': 'E', 'branch': 'ejected', 'base': scratch.git('merge-base', 'ejected', 'main'),
                         'head': scratch.git('rev-parse', 'ejected')})
        commit('DEVLOG/9999-train.md', 'train 9999\n', 'Train 9999: DEVLOG 1, the train\'s last commit')
        (landed_coord / 'scratch' / 'train9999-manifest.json').write_text(json.dumps(manifest), encoding='utf-8', newline="\n")
        for b, f, text in (('picked', 'src/P.cs', 'class PapaPicked { int LaterOnMain; }\n'),
                           ('picked-lost', 'src/Q.cs', 'class QuebecRenamed {}\n')):
            scratch.git('cherry-pick', b)
            commit(f, text, f'main edits the file {b} added')
        scratch.git('switch', '-q', 'carried-moved')
        commit('src/M.cs', 'class MikeMoved { int AfterTheTrain; }\n', 'a commit after the head the train took')
        # a pushed branch that lands is deleted on origin too; a remote that moved on is kept
        scratch.git('push', '-q', 'origin', 'landed-content')
        scratch.git('switch', '-q', '-c', 'tmp-remote', 'merged')
        commit('src/R.cs', 'class RomeoRemoteOnly {}\n', 'a commit only the remote holds')
        scratch.git('push', '-q', 'origin', 'tmp-remote:merged')
        scratch.git('switch', '-q', 'main')
        scratch.git('branch', '-D', 'tmp-remote')
        scratch.git('push', '-q', 'origin', 'main')
        scratch.git('fetch', '-q', 'origin')
        # `lane-stopped` got what `enqueue.py land` commits before its gate: origin/main merged in cleanly, the DEVLOG
        # stamp; its attempt then stopped at that tip with exit 5 (GATE RED). The attempts are written by the lane's own
        # writer (land_lane.launch) and completed in its runner's record shape (run_attempt: rc, verdict, end_head).
        scratch.git('switch', '-q', 'lane-stopped')
        scratch.git('merge', '-q', '--no-edit', 'origin/main')
        commit('DEVLOG/0043-lane.md', '## Entry 43 — 2026-10-15 12:00 PDT — lane\n\nWhy.\n', 'DEVLOG 43: stamp the entry')
        scratch.git('switch', '-q', 'main')
        import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back)

        def attempt(branch: str, head: str, rc: int | None, verdict: str = '', end_head: str | None = None) -> None:
            rec = land_lane.launch(landed_coord, repo, {'name': branch, 'tip': head, 'worktree': str(repo)}, 'self-test',
                                   lambda argv, log: 4242)
            if rc is not None:
                land_lane.update(rec, {'rc': rc, 'verdict': verdict, 'ended': '2026-10-15T19:00:00+00:00',
                                       'end_head': end_head or head})
        attempt('lane-stopped', lane_at['lane-stopped'], 5, 'GATE RED (Linux) on x',
                scratch.git('rev-parse', 'lane-stopped'))
        attempt('lane-free', lane_at['lane-free'], 3, 'NOT QUEUED (exit 3, above)')
        attempt('lane-done', lane_at['lane-done'], 0, 'ENQUEUED')
        attempt('qsucc', once_at['qsucc'], 0, 'ENQUEUED')
        attempt('lane-running', lane_at['lane-running'], None)
        attempt('done-branch', 'an-older-head', 5, 'GATE RED (Windows) on an-older-head')   # not at this tip: ignored
        wt = tmp / 'worktrees'
        # worktrees: dirty-but-merged (DECISION), locked by a live agent, locked by a dead pid (LANDED), detached
        scratch.git('worktree', 'add', '-q', '-b', 'dirty-merged', str(wt / 'dirty-merged'), 'main')
        (wt / 'dirty-merged' / 'scratch.txt').write_text('uncommitted\n', encoding='utf-8', newline="\n")
        scratch.git('worktree', 'add', '-q', '-b', 'agent-live', str(wt / 'agent-live'), 'main')
        scratch.git('worktree', 'lock', '--reason', 'claude agent live (pid 101)', str(wt / 'agent-live'))
        scratch.git('worktree', 'add', '-q', '-b', 'agent-dead', str(wt / 'agent-dead'), 'main')
        scratch.git('worktree', 'lock', '--reason', 'claude agent dead (pid 102)', str(wt / 'agent-dead'))
        scratch.git('worktree', 'add', '-q', '--detach', str(wt / 'pin'), 'main')
        # unreadable: a landed worktree whose status git cannot produce (a corrupt index) holding an untracked file
        scratch.git('worktree', 'add', '-q', '-b', 'unreadable', str(wt / 'unreadable'), 'main')
        (wt / 'unreadable' / 'precious.txt').write_text('uncommitted\n', encoding='utf-8', newline="\n")
        (repo / '.git' / 'worktrees' / 'unreadable' / 'index').write_bytes(b'not an index')
        rdir = tmp / 'reports'
        rdir.mkdir()
        hours = lambda h: time.time() - h * 3600  # noqa: E731
        # a FINDINGS stamp names its findings file where the review wrote it (the coordination scratch below): the
        # survey reads every stamped path, and one that cannot be opened is a tear (review R6-1)
        at_scratch = lambda rel: (landed_coord / 'scratch' / rel).as_posix()  # noqa: E731
        for name, text, h in (
                # the attended shape (kb/Work PB2602): Status, Worktree (a Windows path), Branch, HEAD, Base
                ('w9998a-PB1-report.md', 'Status: DONE\nWorktree: E:\\x\\.claude\\worktrees\\done-wt\n'
                                         f'Branch: `done-branch`\nHEAD: {tips["done-branch"][:9]}\nBase: 1234567\n'
                                         f'**Review:** CLEAN w9998a-review-r1 {tips["done-branch"]}\n', 10),
                ('w9998k-PB12-report.md', f'Status: DONE\nBranch: unreviewed · head {tips["unreviewed"][:12]}\n', 10),
                ('w9998l-PB13-report.md', f'Status: DONE\nBranch: review-findings\nHEAD: {tips["review-findings"]}\n'
                                          f'**Review:** FINDINGS 2 w9998l-review-r1 {tips["review-findings"]} '
                                          'E:/s/review-w9998-l.jsonl\n', 10),
                # rewritten for the commit after the review: the report covers the tip, the stamp does not
                ('w9998m-PB14-report.md', f'Status: DONE\nBranch: reviewed-moved\nHEAD: {tips["reviewed-moved"]}\n'
                                          f'**Review:** CLEAN w9998m-review-r1 {reviewed_at[:12]}\n', 10),
                ('w9998n-PB15-report.md', f'Status: DONE\nBranch: succ\nHEAD: {tips["succ"]}\n'
                                          f'**Review:** CLEAN w9998n-r1 {tips["succ"]}\n', 10),
                ('w9998q-PB17-report.md', f'Status: DONE\nBranch: reviewed-stamped\nHEAD: {stamped_at[:12]}\n'
                                          f'**Review:** CLEAN w9998q-review-r1 {stamped_at[:12]}\n', 10),
                # kb/Work PB3394: rewritten for the answers committed after the FINDINGS review (the report covers the
                # tip, the stamp does not), and a FINDINGS stamp followed only by the DEVLOG stamp (it still covers)
                ('w9992a-PB50-report.md', f'Status: DONE\nBranch: findings-moved\nHEAD: {tips["findings-moved"]}\n'
                                          f'**Review:** FINDINGS 1 w9992a-r1 {fmoved_at[:12]} E:/s/review-w9992-a.jsonl\n', 10),
                # 30 h old, over --stale-hours: an old report WITH findings is still the finisher's, never "land it" (PB3118)
                ('w9992b-PB51-report.md', f'Status: DONE\nBranch: findings-stamped\nHEAD: {fstamped_at[:12]}\n'
                                          f'**Review:** FINDINGS 1 w9992b-r1 {fstamped_at[:12]} E:/s/review-w9992-b.jsonl\n', 30),
                ('w9992c-PB52-report.md', f'Status: DONE\nBranch: findings-resumed\nHEAD: {fresumed_at[:12]}\n'
                                          f'**Review:** FINDINGS 1 w9992c-r1 {fresumed_at[:12]} E:/s/review-w9992-c.jsonl\n', 10),
                ('w9998o-PB16-report.md', f'Status: DONE\nBranch: fnd-pred\nHEAD: {tips["fnd-pred"]}\n'
                                          f'**Review:** FINDINGS 1 w9998o-r1 {tips["fnd-pred"]} {at_scratch("review-w9998-o.jsonl")}\n', 10),
                ('w9998p-PB16-report.md', f'Status: DONE\nBranch: fnd-fix\nHEAD: {tips["fnd-fix"]}\n'
                                          f'**Review:** CLEAN w9998p-r1 {tips["fnd-fix"]}\n', 10),
                ('w9997a-PB30-report.md', f'Status: DONE\nBranch: cpred\nHEAD: {tips["cpred"]}\n'
                                          f'**Review:** CLEAN w9997a-r1 {tips["cpred"]}\n', 10),
                ('w9997b-PB31-report.md', f'Status: DONE\nBranch: csucc\nHEAD: {tips["csucc"]}\n', 10),
                ('w9997c-PB32-report.md', f'Status: DONE\nBranch: xpred\nHEAD: {tips["xpred"]}\n'
                                          f'**Review:** FINDINGS 1 w9997c-r3 {tips["xpred"]} {at_scratch("w9997/review-w9997-c.jsonl")}\n', 10),
                ('w9997d-PB33-report.md', f'Status: DONE\nBranch: xsucc\nHEAD: {tips["xsucc"]}\n'
                                          f'**Review:** CLEAN w9997d-r1 {tips["xsucc"]}\n', 10),
                ('w9997e-PB34-report.md', f'Status: DONE\nBranch: upred\nHEAD: {tips["upred"]}\n', 10),
                ('w9997f-PB35-report.md', f'Status: DONE\nBranch: usucc\nHEAD: {tips["usucc"]}\n'
                                          f'**Review:** CLEAN w9997f-r1 {tips["usucc"]}\n', 10),
                # review R3-1: the finisher's rewrite of rpred's report (BLOCKED, its own branch, no Review line)
                ('w9996a-PB40-report.md', f'Status: BLOCKED\nBranch: rpred-fix\nHEAD: {tips["rpred"]}\n\n'
                                          '## Review findings\n- R1-1 not answered: blocked\n', 10),
                ('w9996b-PB41-report.md', f'Status: DONE\nBranch: rsucc\nHEAD: {tips["rsucc"]}\n'
                                          f'**Review:** CLEAN w9996b-r1 {tips["rsucc"]}\n', 10),
                # review R4-1: tpred's report keeps its capped FINDINGS stamp; tsucc is reviewed CLEAN
                ('w9993a-PB42-report.md', f'Status: DONE\nBranch: tpred\nHEAD: {tips["tpred"]}\n'
                                          f'**Review:** FINDINGS 1 w9993a-r3 {tips["tpred"]} {at_scratch("w9993/review-w9993-a.jsonl")}\n', 10),
                ('w9993b-PB43-report.md', f'Status: DONE\nBranch: tsucc\nHEAD: {tips["tsucc"]}\n'
                                          f'**Review:** CLEAN w9993b-r1 {tips["tsucc"]}\n', 10),
                # kb/Work PB3267: gsucc ended FINDINGS (a finisher is due) and carries gpred, which has no report;
                # hpred ended FINDINGS and hsucc, which merged it, is finished and not reviewed yet
                ('w9991a-PB44-report.md', f'Status: DONE\nBranch: gsucc\nHEAD: {tips["gsucc"]}\n'
                                          f'**Review:** FINDINGS 1 w9991a-r1 {tips["gsucc"]} E:/s/review-w9991-a.jsonl\n', 10),
                ('w9991b-PB45-report.md', f'Status: DONE\nBranch: hpred\nHEAD: {tips["hpred"]}\n'
                                          f'**Review:** FINDINGS 1 w9991b-r1 {tips["hpred"]} E:/s/review-w9991-b.jsonl\n', 10),
                ('w9991c-PB46-report.md', f'Status: DONE\nBranch: hsucc\nHEAD: {tips["hsucc"]}\n', 10),
                # kb/Work PB3181, PB3125: the report, the review and the landing lane's attempt at the tip
                ('w9998r-PB18-report.md', f'Status: DONE\nBranch: lane-stopped · head {lane_at["lane-stopped"][:12]}\n'
                                          f'**Review:** CLEAN w9998r-r1 {lane_at["lane-stopped"]}\n', 10),
                ('w9998s-PB19-report.md', f'Status: DONE\nBranch: lane-free\nHEAD: {lane_at["lane-free"]}\n'
                                          f'**Review:** CLEAN w9998s-r1 {lane_at["lane-free"]}\n', 10),
                ('w9998t-PB20-report.md', f'Status: DONE\nBranch: lane-done\nHEAD: {lane_at["lane-done"]}\n'
                                          f'**Review:** CLEAN w9998t-r1 {lane_at["lane-done"]}\n', 10),
                ('w9998u-PB21-report.md', f'Status: DONE\nBranch: lane-running\nHEAD: {lane_at["lane-running"]}\n'
                                          f'**Review:** CLEAN w9998u-r1 {lane_at["lane-running"]}\n', 10),
                ('w9998v-PB22-report.md', f'**Status:** DISCHARGED · **branch:** `discharged` · **base:** `{main_at[:9]}` · '
                                          f'**HEAD:** `{lane_at["discharged"][:9]}`\n'
                                          f'**Review:** CLEAN w9998v-r1 {lane_at["discharged"]}\n', 10),
                ('w9998w-PB23-report.md', f'Status: DISCHARGED\nBranch: discharged-empty · base {main_at} · head {main_at}\n'
                                          f'**Review:** CLEAN w9998w-r1 {lane_at["discharged-empty"]}\n', 10),
                # kb/Work PB3476: a header whose HEAD gives no sha takes the head its review stamp names; with no stamp
                # either it describes no commit of the branch
                ('w9998x-PB24-report.md', 'Status: DONE\nBranch: no-head\n**HEAD:** see the trailing commit\n'
                                          f'**Review:** CLEAN w9998x-r1 {lane_at["no-head"]}\n', 10),
                ('w9998y-PB25-report.md', 'Status: DONE\nBranch: no-head-bare\n**HEAD:** see the trailing commit\n', 10),
                # kb/Work PB3652: SPLIT branches another branch carries (replayed by cherry-pick, or an ancestor of a
                # finished one), and the ones that stay decisions (partly replayed, replayed by an ABANDONED branch)
                # kb/Work PB3693: both ends of a chain reviewed CLEAN, and a CLEAN predecessor inside a queued successor
                *((f'w9989{n}-PB7{i}-report.md', f'Status: DONE\nBranch: {b}\nHEAD: {once_at[b]}\n'
                                                 f'**Review:** CLEAN w9989{n}-r1 {once_at[b]}\n', 10)
                  for i, (n, b) in enumerate((('a', 'wpred'), ('b', 'wsucc'), ('c', 'qpred'), ('d', 'qsucc')))),
                ('w9990a-PB60-report.md', 'SPLIT\nBranch: rp-pred\n', 10),
                ('w9990b-PB61-report.md', 'SPLIT\nBranch: rp-succ\n', 10),
                ('w9990c-PB62-report.md', 'SPLIT\nBranch: rp-part\n', 10),
                ('w9990d-PB63-report.md', 'SPLIT\nBranch: rp-partc\n', 10),
                ('w9990e-PB64-report.md', 'SPLIT\nBranch: rp-gone\n', 10),
                ('w9990f-PB65-report.md', 'SPLIT\nBranch: rp-dead\n', 10),
                ('w9990g-PB66-report.md', 'SPLIT\nBranch: an-pred\n', 10),
                ('w9990h-PB67-report.md', f'Status: DONE\nBranch: an-succ\nHEAD: {scratch.git("rev-parse", "an-succ")}\n', 10),
                # kb/Work PB3386: every DECISION that is an ancestor of a finished branch lands through it, whatever its report
                ('w9990i-PB68-report.md', 'SPLIT\nBranch: cw-pred\n', 10),
                ('w9990j-PB69-report.md', f'Status: DONE\nBranch: cw-stale\nHEAD: {scratch.git("rev-parse", "cw-stale")}\n', 30),
                ('w9990k-PB70-report.md', f'Status: DONE\nBranch: cw-moved\nHEAD: {cw_moved_at}\n', 10),
                ('w9990l-PB4-report.md', f'Status: DONE\nBranch: cw-lost\nHEAD: {scratch.git("rev-parse", "cw-lost")}\n', 10),
                ('w9990m-PB71-report.md', f'Status: DONE\nBranch: cw-succ\nHEAD: {scratch.git("rev-parse", "cw-succ")}\n'
                                          f'**Review:** CLEAN w9990m-r1 {scratch.git("rev-parse", "cw-succ")}\n', 10),
                ('w9998b-PB2-report.md', 'SPLIT\nBranch: split-branch\n', 10),
                ('w9998c-PB3-report-v2.md', 'Status: DONE\nBranch: orphan\n', 10),        # suffixed: invisible
                ('w9998d-PB4-report.md', '**Status:** DONE · **branch:** `superseded`\n', 10),
                ('w9998e-PB5-report.md', 'DONE\n**worktree:** `E:/x/.claude/worktrees/stale-wt` · branch stale-done\n', 30),
                ('w9998f-PB6-report.md', f'Status: DONE\nBranch: resumed\nHEAD: {resumed_at}\n', 10),
                ('w9998g-PB7-report.md', 'Status: DONE\nBranch: other\n\n## Notes\nits predecessor was orphan\n', 10),
                # a header whose branch field names another branch is not about the one it names as its base
                ('w9998h-PB9-report.md', 'Status: DONE\nWorktree: E:\\x\\worktrees\\zz (branch zz)\nBase: orphan\n', 10),
                # PB2708: a carried branch's lead note is still open, so without the train record it read WAITING TO LAND
                ('w9998i-PB10-report.md', 'Status: DONE\nBranch: carried\n', 10),
                ('w9998j-PB11-report.md', 'Status: DONE\nBranch: carried-moved\n', 11)):
            (rdir / name).write_text(text, encoding='utf-8', newline="\n")
            os.utime(rdir / name, (hours(h), hours(h)))
        # THE FINDINGS FILES, the one source of unanswered findings (kb/Work PB3381 review R3-1), written by the
        # review's own tool where each wave keeps them: xpred's (capped), fnd-pred's (answered by its own note's later
        # finisher), rpred's (its finisher came back BLOCKED with no commit), and pred's, whose FINDINGS a later round
        # answered CLEAN, so succ, which contains it, carries nothing. An R2 shard sharing the name lives two levels
        # down (`<coord>/scratch/r2-<sha>/batch-<n>`), outside the survey's search by name.
        import review_findings  # noqa: PLC0415  (scripts/orchestrator)
        fscratch = landed_coord / 'scratch'

        def review_file(path: pathlib.Path, rounds: list[tuple[str, str, list[str]]], cap: int = 0) -> None:
            for k, (branch, head, severities) in enumerate(rounds, 1):
                review_findings.start(path, k, branch, head, f'w-self-test-r{k}')
                for n, sev in enumerate(severities, 1):
                    review_findings.finding(path, k, json.dumps({'file': 'src/X.cs', 'line': n, 'category': 'correctness',
                                                                 'summary': 's', 'severity': sev, 'failure_scenario': 'x'}))
                review_findings.verdict(path, k, review_findings.BLOCKING_AT_CAP if k == cap else review_findings.BLOCKING_DEFAULT)
        review_file(fscratch / 'w9997' / 'review-w9997-c.jsonl', [('xpred', tips['xpred'], ['high'])], cap=1)
        review_file(fscratch / 'review-w9998-o.jsonl', [('fnd-pred', tips['fnd-pred'], ['high'])])
        review_file(fscratch / 'w9996' / 'review-w9996-a.jsonl', [('rpred', tips['rpred'], ['high', 'low'])])
        review_file(fscratch / 'w9995' / 'review-w9995-a.jsonl', [('pred', tips['pred'], ['high']), ('pred', tips['pred'], [])])
        # review R4-1: tpred's three rounds, the third capped, then a round-4 finding torn mid-append
        tfile = fscratch / 'w9993' / 'review-w9993-a.jsonl'
        review_file(tfile, [('tpred', tips['tpred'], ['high'])] * 3, cap=3)
        review_findings.start(tfile, 4, 'tpred', tips['tpred'], 'w-self-test-r4')
        tear_at = len(tfile.read_text(encoding='utf-8').splitlines()) + 1
        with tfile.open('a', encoding='utf-8', newline='\n') as f:
            f.write('{"kind": "finding", "round": 4, "file": "src/X.cs", "li')
        # a torn file naming a branch and head this repository lacks: the survey says it, holds nothing else, and goes on
        # (every check below runs)
        torn = fscratch / 'w9994' / 'review-w9994-t.jsonl'
        review_findings.start(torn, 1, 'gone', '0abcdef', 'w9994t-r1')
        with torn.open('a', encoding='utf-8', newline='\n') as f:
            f.write('{"kind": "verdict", "round": 1, "bra')
        (fscratch / 'r2-0abc' / 'batch-1').mkdir(parents=True)
        (fscratch / 'r2-0abc' / 'batch-1' / 'review-cli--code--f1.jsonl').write_text('{"type": "read", "file": "x.cs"}\n', encoding='utf-8', newline='\n')
        # age everything but `fresh` past --min-age: the branch reflogs, each worktree's HEAD log
        common = repo / '.git'
        for p in list((common / 'logs').rglob('*')):
            if p.is_file() and p.name != 'fresh':
                os.utime(p, (hours(10), hours(10)))
        for d in (common / 'worktrees').iterdir():
            os.utime(d / 'logs' / 'HEAD', (hours(10), hours(10)))
        os.utime(common / 'logs' / 'refs' / 'heads' / 'stale-done', (hours(40), hours(40)))  # idle since before its report
        alive = {101: 'claude.exe', 102: None}
        live = lambda reason: live_lock(reason, alive=lambda pid: alive.get(pid))  # noqa: E731
        terminal = lambda n: n == 'PB4'  # noqa: E731
        rows = {r.name: r for r in survey(repo, reports=report_index(rdir, terminal), slugs={'w9999a'}, min_age=180,
                                          stale_hours=24, live=live, coord_dir=landed_coord, prs=None)}
        want = {'merged': 'LANDED', 'landed-content': 'LANDED', 'key-landed': 'LANDED', 'orphan': 'DECISION', 'done-branch': 'WAITING TO LAND',
                'split-branch': 'DECISION', 'w9999a-ledger': 'IN FLIGHT', 'fresh': 'IN FLIGHT', 'superseded': 'DECISION',
                'stale-done': 'DECISION', 'resumed': 'DECISION',
                'dirty-merged': 'DECISION', 'agent-live': 'IN FLIGHT', 'agent-dead': 'LANDED', '(detached) pin': 'DECISION',
                'modified-only': 'DECISION', 'unreadable': 'DECISION',
                'carried': 'LANDED', 'carried-moved': 'DECISION', 'add-del': 'DECISION', 'picked': 'LANDED',
                'picked-lost': 'DECISION', 'tools-only': 'DECISION', 'unlanded-train': 'DECISION', 'ejected': 'DECISION',
                'unreviewed': 'WAITING FOR REVIEW', 'review-findings': 'DECISION', 'reviewed-moved': 'WAITING FOR REVIEW',
                'pred': 'IN FLIGHT', 'succ': 'WAITING TO LAND', 'fnd-pred': 'IN FLIGHT', 'fnd-fix': 'WAITING TO LAND',
                'reviewed-stamped': 'WAITING TO LAND',
                'lane-stopped': 'STOPPED', 'lane-pred': 'IN FLIGHT', 'lane-free': 'WAITING TO LAND',
                'lane-done': 'ENQUEUED', 'lane-running': 'WAITING TO LAND', 'discharged': 'WAITING TO LAND',
                'discharged-empty': 'DECISION', 'no-head': 'WAITING TO LAND', 'no-head-bare': 'DECISION',
                'cpred': 'WAITING TO LAND', 'csucc': 'WAITING FOR REVIEW', 'xpred': 'IN FLIGHT', 'xsucc': 'IN FLIGHT',
                'upred': 'IN FLIGHT', 'usucc': 'WAITING TO LAND',
                'rpred': 'IN FLIGHT', 'rpred-fix': 'IN FLIGHT', 'rsucc': 'WAITING TO LAND',
                'tpred': 'IN FLIGHT', 'tsucc': 'IN FLIGHT',
                'gpred': 'IN FLIGHT', 'gsucc': 'DECISION', 'hpred': 'IN FLIGHT', 'hsucc': 'WAITING FOR REVIEW',
                'findings-moved': 'WAITING FOR REVIEW', 'findings-stamped': 'DECISION',
                'rp-pred': 'IN FLIGHT', 'rp-succ': 'DECISION', 'rp-part': 'DECISION', 'rp-partc': 'DECISION',
                'rp-gone': 'DECISION', 'rp-dead': 'DECISION', 'an-pred': 'IN FLIGHT', 'an-succ': 'WAITING FOR REVIEW',
                'cw-pred': 'IN FLIGHT', 'cw-stale': 'IN FLIGHT', 'cw-moved': 'IN FLIGHT', 'cw-lost': 'IN FLIGHT',
                'cw-succ': 'WAITING TO LAND',
                'findings-resumed': 'DECISION', 'wpred': 'WAITING TO LAND', 'wsucc': 'IN FLIGHT',
                'qpred': 'IN FLIGHT', 'qsucc': 'ENQUEUED'}
        for name, state in want.items():
            got = rows.get(name)
            check(got is not None and got.state == state, f'{name}: want {state}, got {got.state if got else "no row"} ({got.why if got else ""})')
        # kb/Work PB3693: a branch is landed ONCE
        check((rows['wsucc'].waits_on, rows['wsucc'].carrier, 'which lands on its own' in rows['wsucc'].why,
               rows['wpred'].waits_on, rows['wpred'].carrier) == ('wpred', '', True, '', ''),
              f'a WAITING TO LAND successor waits for its WAITING TO LAND predecessor, which lands on its own: '
              f'{rows["wsucc"].state} {rows["wsucc"].why}')
        check((rows['qpred'].carrier, rows['qpred'].waits_on, 'contained in qsucc (ENQUEUED)' in rows['qpred'].why,
               rows['qsucc'].carrier) == ('qsucc', '', True, ''),
              f'a WAITING TO LAND predecessor inside an ENQUEUED successor lands through it: {rows["qpred"].why}')
        check('wsucc' not in [r.name for r in landing_order(list(rows.values()), lambda note: (0, 1))]
              and 'wpred' in [r.name for r in landing_order(list(rows.values()), lambda note: (0, 1))],
              'the landing lane is offered the predecessor, never its waiting successor')
        # ONE RULE FOR A FINISHED BRANCH (kb/Work PB3181, PB3125): coverage, never age; the lane's attempt at the tip
        check('exit 5' in rows['lane-stopped'].why and 'step 4 finisher' in rows['lane-stopped'].why
              and 'later commits mechanical' in rows['lane-stopped'].why,
              f'a stopped attempt at a tip of mechanical commits after the report is STOPPED, its owner named: '
              f'{rows["lane-stopped"].why}')
        check('contained in lane-stopped (STOPPED)' in rows['lane-pred'].why,
              f'a predecessor of a STOPPED branch lands through it: {rows["lane-pred"].why}')
        check('exit 3' in rows['lane-free'].why and 'landing_check.py passes' in rows['lane-free'].why,
              f'an exit-3 attempt waits with the lane\'s retry rule named: {rows["lane-free"].why}')
        check('exit 0' in rows['lane-done'].why, f'an exit-0 attempt at the tip reads ENQUEUED offline: {rows["lane-done"].why}')
        check('being landed' in rows['lane-running'].why, f'a running attempt is said: {rows["lane-running"].why}')
        check('the landing lane lands it' in rows['done-branch'].why and 'an-older' not in rows['done-branch'].why,
              f'an attempt at another head does not decide this tip: {rows["done-branch"].why}')
        check('DISCHARGED' in rows['discharged-empty'].why and 'Review CLEAN' not in rows['discharged-empty'].why,
              f'a DISCHARGED report whose HEAD is its base is not finished: {rows["discharged-empty"].why}')
        check('names no HEAD' in rows['no-head-bare'].why,
              f'a report naming no HEAD and carrying no review stamp covers nothing: {rows["no-head-bare"].why}')
        check('Review CLEAN at' in rows['no-head'].why and rows['no-head'].report.head == lane_at['no-head'],
              f'a report whose HEAD gives no sha is covered by the head its CLEAN stamp names (PB3476): '
              f'{rows["no-head"].why}')
        check('1 commit(s) since the report' in rows['resumed'].why,
              f'a commit after the report that no report describes is a DECISION, counted: {rows["resumed"].why}')
        check(rows['landed-content'].content == 'LANDED', 'a branch re-applied on main classifies LANDED by content')
        check('no fix-lane report' in rows['orphan'].why,
              'a suffixed report name is invisible (coord.REPORT_NAME), and a name below the header binds nothing')
        check('SPLIT' in rows['split-branch'].why, 'a DECISION row names its report status')
        check('already landed' in rows['superseded'].why, 'a DONE report whose lead note landed is superseded')
        check('never landed' in rows['stale-done'].why, 'a DONE report over --stale-hours is a DECISION, said so')
        check('uncommitted' in rows['dirty-merged'].why, 'a dirty landed worktree is a DECISION, with the reason')
        check('no review stamp' in rows['unreviewed'].why, 'a DONE branch with no review stamp waits for its reviewer')
        check('but the branch is at' in rows['reviewed-moved'].why, 'a stamp at an older head is unreviewed work, said so')
        check('2 finding(s)' in rows['review-findings'].why and 'finisher' in rows['review-findings'].why,
              'a FINDINGS stamp is a finisher DECISION, with the findings file')
        # kb/Work PB3394 (wave 1127's shape): the planner reads THIS survey's coverage (Coverage.review), so a FINDINGS
        # stamp at a head the branch moved past is WAITING FOR REVIEW to both and gets no finisher; one followed only by
        # the DEVLOG stamp still covers its tip, and gets its finisher
        import plan_wave  # noqa: PLC0415  (it imports this module back)
        check('Review FINDINGS at' in rows['findings-moved'].why and 'finding(s)' in rows['findings-stamped'].why,
              f'a FINDINGS stamp at an older head is unreviewed work; one followed only by the DEVLOG stamp covers: '
              f'{rows["findings-moved"].why} / {rows["findings-stamped"].why}')
        coverage = Coverage(list(rows.values()), repo=repo)
        preps = plan_wave.load_reports(rdir)
        pnotes = {n: plan_wave.Note(n, 'open', 'process', n, [], '', 0) for n in ('PB50', 'PB51', 'PB52')}
        plan = plan_wave.plan(pnotes, [{'file': f'src/{f}.cs', 'files': [], 'harm': 0, 'notes': [{'id': n}]}
                                       for n, f in (('PB50', 'FM'), ('PB51', 'FS'), ('PB52', 'FR'))], [], preps,
                              lambda b: rows[b].content if b in rows else 'ABSENT', {}, coord.rules(), 100,
                              review_covers=lambda rep: coverage.review(rep.review, rep.branch),
                              report_covers=lambda rep: coverage.report(rep.head, rep.branch))
        fins = {i: g.pred for g in plan['groups'] if g.kind == 'finisher' for i in g.notes}
        check(('PB50' in fins, plan['waiting'].get('PB50', '').startswith('WAITING FOR REVIEW'),
               'FINISH-FIRST: the branch review of w9992b' in fins.get('PB51', ''), 'PB51' in plan['waiting'])
              == (False, True, True, False),
              f'the planner plans no finisher for a FINDINGS stamp at an older head (WAITING FOR REVIEW, as the survey '
              f'reads it) and one for a stamp that covers the tip: groups {fins}, waiting {plan["waiting"]}')
        # kb/Work PB3446 (the drift test): the planner names WAITING FOR REVIEW for a FINDINGS-stamped branch exactly where
        # lifecycle() reads it WAITING FOR REVIEW; a report followed by a commit no report describes is a DECISION there
        # and RESUMED here (no reviewer is ever dispatched for it), and neither is a finisher
        said = {n: ('WAITING FOR REVIEW' if plan['waiting'].get(n, '').startswith('WAITING FOR REVIEW') else
                    'RESUMED' if plan['waiting'].get(n, '').startswith('RESUMED') else 'planned' if n in fins else '?')
                for n in ('PB50', 'PB51', 'PB52')}
        check(said == {'PB50': 'WAITING FOR REVIEW', 'PB51': 'planned', 'PB52': 'RESUMED'}
              and all((rows[b].state == 'WAITING FOR REVIEW') == (said[n] == 'WAITING FOR REVIEW')
                      for n, b in (('PB50', 'findings-moved'), ('PB51', 'findings-stamped'), ('PB52', 'findings-resumed'))),
              f'the planner and lifecycle() read one FINDINGS branch one way (PB3446): planner {said}, survey '
              f'{[rows[b].state for b in ("findings-moved", "findings-stamped", "findings-resumed")]}')
        check('1 commit(s) since the report' in rows['findings-resumed'].why,
              f'a FINDINGS report followed by a commit no report describes is a DECISION: {rows["findings-resumed"].why}')
        # kb/Work PB3118: the survey's promise "a finisher answers them, and the next wave plans it" holds for a lead the
        # fix lane's clusters do not hold (a `process_only` note, or one only its branch has), and for a report over
        # --stale-hours (findings-stamped is 30 h old): the DECISION row says the finisher is due, not "land it", and the
        # planner, asked on the same rows with NO cluster holding PB51, plans that finisher
        check('next wave plans it' in rows['findings-stamped'].why and 'never landed' not in rows['findings-stamped'].why
              and 'finisher is overdue' in rows['findings-stamped'].why,
              f'an old report with unanswered findings is the finisher\'s, not "land it": {rows["findings-stamped"].why}')
        check(finisher_due(rows['findings-stamped']) and not finisher_due(rows['stale-done']),
              'finisher_due and the stale-report arm read the same FINDINGS rule (findings_open)')
        unseen_plan = plan_wave.plan(pnotes, [], [], preps, lambda b: rows[b].content if b in rows else 'ABSENT', {},
                                     coord.rules(), 100,
                                     review_covers=lambda rep: coverage.review(rep.review, rep.branch),
                                     report_covers=lambda rep: coverage.report(rep.head, rep.branch))
        check([g.notes for g in unseen_plan['groups'] if g.kind == 'finisher'] == [['PB51']],
              f'every DECISION row that promises the next wave plans a finisher is planned one, with no cluster holding '
              f'its lead: {[(g.kind, g.notes) for g in unseen_plan["groups"]]}')
        check('contained in succ' in rows['pred'].why, f'a predecessor lands through its successor: {rows["pred"].why}')
        # kb/Work PB3652: the survey reads the planner's own carrier rule (replay_view) and ancestry for a SPLIT branch too
        check((rows['rp-pred'].carrier, 'replayed by rp-succ (DECISION)' in rows['rp-pred'].why,
               rows['rp-succ'].carrier, rows['rp-part'].carrier, rows['rp-partc'].carrier, rows['rp-gone'].carrier)
              == ('rp-succ', True, '', '', '', ''),
              f'a SPLIT branch another SPLIT branch replays by cherry-pick is carried, not a decision; a partly '
              f'replayed one and one replayed by an ABANDONED branch are decisions: {rows["rp-pred"].why}')
        check((rows['an-pred'].carrier, 'contained in an-succ (WAITING FOR REVIEW)' in rows['an-pred'].why)
              == ('an-succ', True),
              f'a SPLIT branch that a finished branch contains lands through it (the planner\'s branch_carrier): '
              f'{rows["an-pred"].why}')
        cw = ('cw-pred', 'cw-stale', 'cw-moved', 'cw-lost')
        check(all(rows[b].carrier == 'cw-succ' and 'contained in cw-succ (WAITING TO LAND)' in rows[b].why for b in cw),
              f'a SPLIT, a stale, a resumed and a superseded DECISION that a branch WAITING TO LAND contains land through '
              f'it, whatever their reports say (kb/Work PB3386): {[(b, rows[b].carrier, rows[b].why) for b in cw]}')
        check('contained in fnd-fix' in rows['fnd-pred'].why,
              f'a FINDINGS branch its finisher merged lands through the finisher: {rows["fnd-pred"].why}')
        # owned(): IN FLIGHT by the branch's OWN lifecycle only; a contained branch names its carrier and is not owned
        # (kb/Work PB3311 review R1-1: the planner's FINDINGS arm waits only on an owned branch)
        live_set = owned(list(rows.values()))
        check((rows['fnd-pred'].carrier, rows['pred'].carrier, rows['lane-pred'].carrier, rows['fresh'].carrier)
              == ('fnd-fix', 'succ', 'lane-stopped', ''), 'contained() names the carrier; a fresh branch has none')
        check(live_set == {n for n, s in want.items() if s == 'IN FLIGHT'} - {'fnd-pred', 'pred', 'lane-pred', 'xpred', 'xsucc', 'upred', 'rpred', 'rpred-fix', 'rsucc', 'tpred', 'tsucc', 'gpred', 'hpred', 'rp-pred', 'an-pred', 'cw-pred', 'cw-stale', 'cw-moved', 'cw-lost', 'wsucc', 'qpred'},
              f'owned = IN FLIGHT by its own lifecycle (lock, ledger, touch), never by containment: {sorted(live_set)}')
        # kb/Work PB3653: locked() is the LOCK-ONLY half of owned(): a live agent's lock, never a ledger group or a
        # recent touch (which owned() folds in), and never a lock whose pid is dead
        lock_set = locked(list(rows.values()), live)
        check(lock_set == {'agent-live'} and lock_set <= live_set
              and {'fresh', 'w9999a-ledger'} <= live_set and not ({'fresh', 'w9999a-ledger'} & lock_set),
              f'locked = the branches a live agent LOCKS; owned also holds the ledger and the touch: {sorted(lock_set)}')
        # kb/Work PB3381: no hold. A predecessor reviewed CLEAN keeps its own lifecycle inside an unreviewed successor;
        # a successor carrying another note's unanswered FINDINGS waits on that branch; a finisher of the SAME note
        # (fnd-fix, above) carries them to answer them and lands
        check('the landing lane lands it' in rows['cpred'].why and not rows['cpred'].carrier,
              f'a predecessor reviewed CLEAN lands at once, not through its successor: {rows["cpred"].why}')
        check((rows['xsucc'].waits_on, 'carries xpred (PB32)' in rows['xsucc'].why, rows['xpred'].carrier)
              == ('xpred', True, 'xsucc'),
              f'a successor carrying its predecessor\'s unanswered FINDINGS waits on it: {rows["xsucc"].why}')
        # review R3-1: the hold is read from the FINDINGS FILE, never a report's stamp: rpred's finisher came back BLOCKED
        # with no commit and rewrote the report for its own branch with no Review line, so no report shows the findings
        # any more. kb/Work PB3404, ONE OWNER PER CASE: rpred's verdict is UNCAPPED (a review finisher followed it), so
        # rsucc's own review, CLEAN over a head that contains rpred's, answers it and rsucc lands; xsucc's predecessor
        # ended CAPPED (no finisher followed), whose findings belong to xpred's next-wave finisher, so xsucc waits
        # (above). review_answers pins the rule at each of its edges.
        check((rows['rsucc'].waits_on, rows['rsucc'].carrier, rows['rpred'].carrier, 'the landing lane lands it' in rows['rsucc'].why)
              == ('', '', 'rsucc', True),
              f'a successor CLEAN over its predecessor\'s UNCAPPED findings is not held: {rows["rsucc"].state} {rows["rsucc"].why}')
        graph_now = branch_graph([r.tip for r in rows.values() if r.tip], repo)
        uncapped = {'torn': '', 'capped': False, 'head': tips['rpred']}
        check([review_answers(v, rows[b], graph_now) for v, b in (
                  (uncapped, 'rsucc'), ({**uncapped, 'capped': True}, 'rsucc'), ({**uncapped, 'torn': 'cut'}, 'rsucc'),
                  ({**uncapped, 'head': tips['xpred']}, 'rsucc'), ({**uncapped, 'head': ''}, 'rsucc'),
                  (uncapped, 'rpred'), (uncapped, 'orphan'))]
              == [True, False, False, False, False, False, False],
              'review_answers: only an uncapped, untorn verdict whose head the row\'s CLEAN review contains is answered')
        # the planner's side reads the same rule from the findings file itself (answered_by_carrier): rpred's uncapped
        # file is answered by rsucc's CLEAN review, xpred's capped one is not, a stamp naming no file answers nothing
        import plan_wave  # noqa: PLC0415
        stamp_of = lambda b, f: plan_wave.Review('FINDINGS', 1, 'w-self-test', tips[b], str(f) if f else None)  # noqa: E731
        rs_review = rows['rsucc'].report.review
        check([answered_by_carrier(stamp_of('rpred', fscratch / 'w9996' / 'review-w9996-a.jsonl'), rs_review, graph_now),
               answered_by_carrier(stamp_of('xpred', fscratch / 'w9997' / 'review-w9997-c.jsonl'), rows['xsucc'].report.review,
                                   graph_now),
               answered_by_carrier(stamp_of('rpred', None), rs_review, graph_now),
               answered_by_carrier(stamp_of('rpred', fscratch / 'w9996' / 'review-w9996-a.jsonl'), None, graph_now)]
              == [True, False, False, False],
              'answered_by_carrier: the planner reads the findings file\'s last verdict through the survey\'s rule')
        # review R4-1, R5-1: a torn file holds every branch it names and every branch containing a head it names. tpred's
        # file is torn by a round-4 append after its capped round 3, so tsucc is HELD (never WAITING TO LAND), and its
        # why names the tear and the repair
        check((rows['tsucc'].state, rows['tsucc'].waits_on, 'carries tpred (PB42)' in rows['tsucc'].why,
               f'review-w9993-a.jsonl:{tear_at} (byte ' in rows['tsucc'].why and 'not JSON' in rows['tsucc'].why,
               'review_findings.py repair ' in rows['tsucc'].why) == ('IN FLIGHT', 'tpred', True, True, True),
              f'a successor whose predecessor\'s findings file is torn is held, the tear named: '
              f'{rows["tsucc"].state} {rows["tsucc"].why}')
        # kb/Work PB3267, both directions of one rule: a branch whose review ended FINDINGS is a carrier, because the
        # planner owns its finisher. A predecessor with no report of its own lands with it (gpred: IN FLIGHT, carried,
        # never a DECISION row for the operator to decide by hand) and the FINDINGS branch itself stays the DECISION
        # row its finisher answers; a FINDINGS predecessor inside a plain, unreviewed successor (hpred) is carried by
        # it, is not owned, and so keeps its finisher (plan_wave's FINDINGS arm), while hsucc keeps reading its review
        check((rows['gpred'].carrier, 'contained in gsucc (DECISION)' in rows['gpred'].why, rows['gsucc'].carrier)
              == ('gsucc', True, ''),
              f'a predecessor of a FINDINGS-stamped branch lands through it: {rows["gpred"].why}')
        check((rows['hpred'].carrier, rows['hsucc'].carrier) == ('hsucc', ''),
              f'a FINDINGS predecessor inside an unreviewed successor is carried by it: {rows["hpred"].why}')
        # review R5-1, THE PROPERTY at representative cuts (review_findings.py --self-test proves it at every byte):
        # the survey's own reader (unanswered over findings_files) and carrying_findings, re-run on tsucc for each body
        # of tpred's findings file. Expected results from the rule: a cut inside a record holds tsucc; among them the
        # swallowed-record cut of the reviewer's probe case 3 (round 2's FINDINGS verdict lost its newline and round
        # 3's start was glued on; the round-4 reader read round 1 CLEAN and released tsucc) and a cut inside a "§"
        # (the round-4 reader raised UnicodeDecodeError out of the survey); a cut at a record boundary reads exactly
        # its records' verdict (round 1 CLEAN: tsucc lands; round 2 FINDINGS: held).
        kept = tfile.read_bytes()

        def body(name: str, rounds: list[tuple[str, list[str]]], then: bytes = b'', capped_at: int = 0) -> bytes:
            p = tmp / 'cuts' / f'review-{name}.jsonl'
            for k, (summary, severities) in enumerate(rounds, 1):
                review_findings.start(p, k, 'tpred', tips['tpred'], f'w-cut-r{k}')
                for n, sev in enumerate(severities, 1):
                    review_findings.finding(p, k, json.dumps({'file': 'src/X.cs', 'line': n, 'category': 'c', 'severity': sev,
                                                              'summary': summary, 'failure_scenario': 'x'},
                                                             ensure_ascii=False))
                review_findings.verdict(p, k, review_findings.BLOCKING_AT_CAP if k == capped_at else review_findings.BLOCKING_DEFAULT)
            return p.read_bytes() + then

        two = body('two', [('s', []), ('s', ['high'])], capped_at=2)
        glued = json.dumps({'kind': 'start', 'round': 3, 'branch': 'tpred', 'head': tips['tpred'],
                            'reviewer': 'w-cut-r3'}).encode() + b'\n'
        sect = json.dumps({'kind': 'finding', 'round': 2, 'summary': '§14.9 wrong'}, ensure_ascii=False).encode()
        one = body('one', [('s', [])])
        cuts = (('probe case 3: round 2 FINDINGS lost its newline, round 3 start glued on', two[:-1] + glued, 'IN FLIGHT', True),
                ('round 2 FINDINGS lost its newline', two[:-1], 'IN FLIGHT', True),
                ('inside a "§" of a round-2 finding', body('sect', [('§ 1', [])]) + json.dumps(
                    {'kind': 'start', 'round': 2, 'branch': 'tpred', 'head': tips['tpred'], 'reviewer': 'w-cut-r2'}).encode()
                 + b'\n' + sect[:sect.index('§'.encode()) + 1], 'IN FLIGHT', True),
                ('at a record boundary after round 1 CLEAN', one, 'WAITING TO LAND', False),
                ('at a record boundary after a CAPPED round 2 FINDINGS', two, 'IN FLIGHT', False),
                # kb/Work PB3404: the same cut after an UNCAPPED round 2 (a review finisher followed it): tsucc's own
                # CLEAN review answers it
                ('at a record boundary after an UNCAPPED round 2 FINDINGS', body('uncapped', [('s', []), ('s', ['high'])]),
                 'WAITING TO LAND', False),
                # review R6-1: discovery is BY NAME, so a file the survey cannot read, or whose first line is no record,
                # is still listed and read TORN. Each is made at the file's FIRST read (where the round-5 discovery
                # opened it and dropped it): a BOM (an operator's hand edit through Windows PowerShell 5.1), a read
                # denied (pathlib's open raising PermissionError) and a real directory in the file's place. A file that
                # names no branch holds the branches of the note whose review it records (tpred, by its wave name) and
                # every branch containing their tips
                ('with a UTF-8 byte-order mark', review_findings.BOM + two, 'IN FLIGHT', True),
                ('unreadable: opening it raises PermissionError', two, 'IN FLIGHT', True),
                ('unreadable: a directory in its place', None, 'IN FLIGHT', True))
        real_open = pathlib.Path.open

        def deny(self, *a, **k):
            if self == tfile:
                raise PermissionError(13, 'Permission denied', str(self))
            return real_open(self, *a, **k)

        for what, cut, want_state, torn_cut in cuts:
            if cut is None:
                tfile.unlink()
                tfile.mkdir()
            else:
                tfile.write_bytes(cut)
            cut_rows = [dataclasses.replace(r) for r in rows.values()]
            ts = next(r for r in cut_rows if r.name == 'tsucc')
            ts.state, ts.waits_on, ts.why = 'WAITING TO LAND', '', ''
            if 'PermissionError' in what:
                pathlib.Path.open = deny
            try:
                carrying_findings(cut_rows, branch_graph([r.tip for r in cut_rows if r.tip], repo),
                                  review_findings.unanswered(review_findings.findings_files(fscratch)),
                                  report_index(rdir, terminal))
                got = (ts.state, ts.waits_on, 'TORN' in ts.why)
            except Exception as e:  # noqa: BLE001  (the round-4 reader raised here; a raise is a failed check, never a crash)
                got = (f'RAISED {e!r}', '', False)
            finally:
                pathlib.Path.open = real_open
                if cut is None:
                    tfile.rmdir()
            check(got == (want_state, 'tpred' if want_state == 'IN FLIGHT' else '', torn_cut),
                  f'tpred\'s findings file cut {what}: tsucc reads {want_state}{" (TORN)" if torn_cut else ""}: {got} {ts.why}')
        tfile.write_bytes(kept)
        check(not rows['succ'].waits_on and 'unanswered' not in rows['succ'].why,
              f'a FINDINGS round that a later round answered CLEAN holds nothing: {rows["succ"].why}')
        check((rows['upred'].carrier, 'contained in usucc (WAITING TO LAND)' in rows['upred'].why) == ('usucc', True),
              f'a finished predecessor with no clean stamp at its tip lands through its successor, never alone and '
              f'never reviewed twice (review R1-2): {rows["upred"].why}')
        check(set(rows) == set(want), f'every branch and detached worktree has a row: {sorted(set(rows) ^ set(want))}')
        check(rows['modified-only'].content == 'CHECK', 'a branch that only modifies an existing file is not LANDED')
        check(rows['unreadable'].dirty > 0, 'a worktree whose status git cannot produce counts as dirty')
        check(f'carried by train 9999 as cluster K ({k9999})' in rows['carried'].why,
              f'a train-carried branch names its own train\'s commit ({k9999}), not an earlier train\'s: {rows["carried"].why}')
        check(rows['ejected'].content == 'CHECK' and not rows['ejected'].how,
              f'an ejected member is not carried by an earlier train\'s commit of its letter: {rows["ejected"].why}')
        check(rows['carried-moved'].content == 'CHECK', 'a branch that moved past the head its train took is not carried')
        check('TransientName' not in rows['add-del'].lost,
              f'a name one commit added and the next deleted is not lost: {rows["add-del"].lost}')
        check(rows['tools-only'].content == 'UNLANDED' and rows['tools-only'].absent == ['tools/Extra/New.cs'],
              f'a branch that adds only a file under tools/ is UNLANDED work, its file named, not an unnamed CHECK (kb/Work PB3474): '
              f'{rows["tools-only"].content} {rows["tools-only"].absent}')
        check('patch-equivalent' in rows['picked'].why, f'a cherry-picked branch says so: {rows["picked"].why}')
        check(rows['picked-lost'].content == 'CHECK' and rows['picked-lost'].lost == ['QuebecLost'],
              f'a cherry-picked branch with a name main lacks stays CHECK: {rows["picked-lost"].lost}')
        # THE PULL-REQUEST STATES (kb/Work PB3023, PB3024, PB3527): ONE reader (queue_facts.read); a PR in the queue is
        # ENQUEUED; one handed to the queue but not in it (auto-merge on, or the lane's ENQUEUED attempt at the tip) is
        # NOT QUEUED with how long it has waited; one the queue removed, or whose required check FAILED before the queue,
        # still at the branch's tip, is BOUNCED with the reason; a bounced PR whose branch moved on (the finisher's fix)
        # reads by its report again; offline it is a non-observation (None)
        ci = lambda job, status, concl='': {'databaseId': job, 'name': 'ci-gate', 'status': status,  # noqa: E731
                                           'conclusion': concl or None, 'completedAt': '2026-10-22T18:46:46Z'}
        fake_gh = queue_facts.fake([
            queue_facts.node(17, 'done-branch', tips['done-branch']),
            queue_facts.node(18, 'succ', tips['succ'], removed='FAILED_CHECKS'),
            queue_facts.node(19, 'fnd-fix', 'an-older-head', removed='FAILED_CHECKS'),
            queue_facts.node(20, 'unreviewed', 'x', auto='2026-10-22T18:44:50Z', removed='FAILED_CHECKS',
                             runs=[ci(201, 'IN_PROGRESS')]),
            queue_facts.node(21, 'lane-done', lane_at['lane-done']),
            queue_facts.node(22, 'discharged', lane_at['discharged'], auto='2026-10-22T18:44:50Z',
                             runs=[ci(221, 'COMPLETED', 'FAILURE')])],
            entries=[17], jobs={201: {'status': 'completed', 'completed_at': '2026-10-22T18:46:46Z'}})
        prs = queue_facts.read(fake_gh)
        then = dt.datetime(2026, 10, 22, 19, 35, 50, tzinfo=dt.timezone.utc)
        pr_rows = {r.name: r for r in survey(repo, reports=report_index(rdir, terminal), slugs={'w9999a'}, min_age=180,
                                             stale_hours=24, live=live, coord_dir=landed_coord, prs=prs)}
        got = {b: pr_rows[b].state for b in ('done-branch', 'succ', 'fnd-fix', 'unreviewed', 'pred', 'merged',
                                             'lane-done', 'discharged')}
        check(got == {'done-branch': 'ENQUEUED', 'succ': 'BOUNCED', 'fnd-fix': 'WAITING TO LAND',
                      'unreviewed': 'WAITING FOR REVIEW', 'pred': 'IN FLIGHT', 'merged': 'LANDED',
                      'lane-done': 'NOT QUEUED', 'discharged': 'BOUNCED'}
              and 'FAILED_CHECKS' in pr_rows['succ'].why and '#17' in pr_rows['done-branch'].why
              and 'ci-gate FAILED' in pr_rows['discharged'].why,
              f'the merge queue states: a PR in the queue is ENQUEUED, one handed to it but not in it NOT QUEUED, a '
              f'removed one or one whose required check failed, at the tip, BOUNCED with its reason, a moved tip reads by '
              f'its report (`unreviewed`: auto-merge is on at the OLD head `x`, which is not a hand-off of the tip, '
              f'kb/Work PB3628), a predecessor of a bounced branch lands through it: {got}')
        dirty = {**prs, 'lane-done': dataclasses.replace(prs['lane-done'], merge_state='DIRTY')}
        conflicted = lifecycle(pr_rows['lane-done'], in_flight_slugs=set(), min_age=180, live=live, prs=dirty,
                               lane=land_lane.view(landed_coord), now=then)
        check(conflicted[0] == 'BOUNCED' and 'conflicts with main (merge state DIRTY' in conflicted[1],
              f'a pull request that conflicts with main (merge state DIRTY) at the tip is BOUNCED, the finisher\'s: no '
              f're-run or reopen clears a conflict (kb/Work PB3527 review R1-5): {conflicted}')
        # A PULL-REQUEST STATE IS READ AGAINST THE HEAD IT WAS OBSERVED AT (kb/Work PB3275 variant 3): `succ` is removed
        # by the queue with its headRefOid AT the tip, so BOUNCED; the same pull request after a finisher PUSHED a commit
        # (the removal is followed by a commit in the timeline, and headRefOid is the tip again) reads by its report
        # (CLEAN: WAITING TO LAND), not BOUNCED for ever. Through the one reader, queue_facts.read.
        pushed = queue_facts.read(queue_facts.fake([queue_facts.node(18, 'succ', tips['succ'], removed='FAILED_CHECKS',
                                                                     pushed=2)]))
        after_push = lifecycle(pr_rows['succ'], in_flight_slugs=set(), min_age=180, live=live, prs=pushed,
                               lane=land_lane.view(landed_coord), now=then)
        before_push = lifecycle(pr_rows['succ'], in_flight_slugs=set(), min_age=180, live=live,
                                prs=queue_facts.read(queue_facts.fake([queue_facts.node(
                                    18, 'succ', tips['succ'], removed='FAILED_CHECKS')])),
                                lane=land_lane.view(landed_coord), now=then)
        check(before_push[0] == 'BOUNCED' and after_push[0] == 'WAITING TO LAND' and pushed['succ'].head == tips['succ'],
              f'a bounced pull request whose fixed tip was pushed (headRefOid is the tip again, a commit follows the removal '
              f'in the timeline) reads by its report, never BOUNCED for ever: before {before_push}, after {after_push}')
        # The second half (variant 1) and kb/Work PB3628, one rule: auto-merge on is GitHub's promise to queue the pull
        # request at ITS head, never at the tip. `fnd-fix`'s finisher tip (reviewed CLEAN) is not the head of its pull
        # request #19, so whatever that head's health (DIRTY, a failed required check, removed by the queue with
        # auto-merge still on, or healthy with its check still running) the tip reads by its report and the lane lands
        # it: waiting on a healthy old head would land that head and leave the fix for a second landing, and NOT QUEUED
        # is a state no reader acts on for a tip (PR #467 stranded 13 h). The `healthy` expectation was NOT QUEUED when
        # PB3275 landed alone; the reconciled rule reads it by its report like the others.
        stuck_head = {'dirty': queue_facts.node(19, 'fnd-fix', 'an-older-head', auto='2026-10-22T18:44:50Z', state='DIRTY'),
                      'red': queue_facts.node(19, 'fnd-fix', 'an-older-head', auto='2026-10-22T18:44:50Z',
                                              runs=[ci(191, 'COMPLETED', 'FAILURE')]),
                      'removed': queue_facts.node(19, 'fnd-fix', 'an-older-head', auto='2026-10-22T18:44:50Z',
                                                  removed='FAILED_CHECKS'),
                      'healthy': queue_facts.node(19, 'fnd-fix', 'an-older-head', auto='2026-10-22T18:44:50Z',
                                                  runs=[ci(191, 'IN_PROGRESS')])}
        past_head = {k: lifecycle(pr_rows['fnd-fix'], in_flight_slugs=set(), min_age=180, live=live,
                                  prs=queue_facts.read(queue_facts.fake([n])), lane=land_lane.view(landed_coord),
                                  now=then)[0]
                     for k, n in stuck_head.items()}
        check(past_head == dict.fromkeys(stuck_head, 'WAITING TO LAND'),
              f'a tip past the head of an auto-merge pull request reads by its report whatever that head\'s health '
              f'(PB3275, PB3628): {past_head}')
        # the same pull request AT the tip, auto-merge on and its check-run stuck: handed to the queue, not in it
        at_tip_prs = queue_facts.read(queue_facts.fake(
            [queue_facts.node(20, 'unreviewed', tips['unreviewed'], auto='2026-10-22T18:44:50Z',
                              runs=[ci(201, 'IN_PROGRESS')])],
            jobs={201: {'status': 'completed', 'completed_at': '2026-10-22T18:46:46Z'}}))
        nq = lifecycle(pr_rows['unreviewed'], in_flight_slugs=set(), min_age=180, live=live, prs=at_tip_prs, now=then)
        check(nq[0] == 'NOT QUEUED' and 'handed to the merge queue 51 min ago (2026-10-22T18:44:50Z)' in nq[1]
              and 'ci-gate in_progress, its job completed 2026-10-22T18:46:46Z' in nq[1] and 'merge state BLOCKED' in nq[1],
              f'a NOT QUEUED row says how long it has waited, the merge state and the stuck check-run\'s job: {nq}')
        # kb/Work PB3628: a pull request at an OLDER head than the tip is the old landing's, never a hand-off of the tip.
        # `discharged` is reviewed CLEAN at its tip; its pull request sits at an older head with auto-merge on, a red
        # required check and a conflict (PR #467's shape: the finisher's fix committed locally, the push still to come).
        # It must read WAITING TO LAND, re-armed or not, and the lane's candidates must hold it.
        old_head = queue_facts.read(queue_facts.fake([
            queue_facts.node(22, 'discharged', 'an-older-head', auto='2026-10-22T18:44:50Z', state='DIRTY',
                             runs=[ci(221, 'COMPLETED', 'FAILURE')]),
            queue_facts.node(21, 'lane-done', 'an-older-head')]))
        stranded = lifecycle(pr_rows['discharged'], in_flight_slugs=set(), min_age=180, live=live, prs=old_head,
                             lane=land_lane.view(landed_coord), now=then)
        check(stranded[0] == 'WAITING TO LAND',
              f'a pull request at an older head than the tip (auto-merge on, red, conflicting) is not NOT QUEUED or '
              f'BOUNCED: the branch reads by its report and the lane lands the tip: {stranded}')
        disc_tip = pr_rows['discharged'].tip
        land_lane.rearm(landed_coord, 'discharged', 'finisher 2 fixed the bounce', say=lambda s: None,
                        tip_for=lambda wt: disc_tip)
        try:
            lane_now = land_lane.view(landed_coord)
            rearmed = lifecycle(pr_rows['discharged'], in_flight_slugs=set(), min_age=180, live=live, prs=old_head,
                                lane=lane_now, now=then)
            pending = land_lane.rearms(landed_coord)
            held = land_lane.candidates([{'name': 'discharged', 'state': rearmed[0], 'tip': disc_tip, 'worktree': 'wt',
                                          'why': '', 'lead': None}], [], {}, pending)
        finally:
            land_lane.rearm_path(landed_coord, 'discharged').unlink(missing_ok=True)
        check(rearmed[0] == 'WAITING TO LAND' and [r['name'] for r in held] == ['discharged'],
              f'a re-armed branch whose pull request sits at an older head is a landing-lane candidate (PB3628: it read '
              f'NOT QUEUED, which no reader acts on, and waited for hours): {rearmed} {held}')
        moved = lifecycle(pr_rows['lane-done'], in_flight_slugs=set(), min_age=180, live=live, prs=old_head,
                          lane=land_lane.view(landed_coord), now=then)
        check(moved[0] == 'NOT QUEUED' and 'pull request #21 is open at an-older-hea, not at this tip' in moved[1]
              and 'no pull request is open' not in moved[1],
              f'an exit-0 attempt at the tip whose pull request sits at another head says so, never "none is open": {moved}')
        check(prs['lane-done'].auto_merge_at == '' and 'exit 0' not in pr_rows['lane-done'].why
              and 'queue watch' in pr_rows['lane-done'].why,
              f'with auto-merge off, the lane\'s ENQUEUED attempt at the tip is what says the PR was handed to the queue, '
              f'and the row names the lane\'s queue watch: {pr_rows["lane-done"].why}')
        # THE ONE PRODUCTION SURVEY (kb/Work PB2807 review R1-2): live_survey reads the open pull requests itself, so
        # every reader (main, the landing lane, the planner's partition) sees the queue states; and no production
        # reader calls survey directly with arguments of its own (the planner once omitted `prs` and never saw BOUNCED)
        live_rows = {r.name: r.state for r in live_survey(repo, coord_dir=landed_coord, reports_dir=rdir,
                                                           terminal=terminal, live=live, gh=fake_gh)}
        check({b: live_rows.get(b) for b in ('done-branch', 'succ', 'fnd-fix')}
              == {'done-branch': 'ENQUEUED', 'succ': 'BOUNCED', 'fnd-fix': 'WAITING TO LAND'},
              f'live_survey reads the pull requests (ENQUEUED, BOUNCED): {live_rows}')
        direct = [p.name for p in (REPO / 'scripts' / 'orchestrator').glob('*.py')
                  if re.search(r'prune_worktrees\.survey\(', p.read_text(encoding='utf-8'))]
        check(not direct, f'production readers call prune_worktrees.live_survey, never survey: {direct}')

        def offline_gh(args):
            import ruleset  # noqa: PLC0415
            raise ruleset.Unobserved('no network')
        offline_rows = {r.name: r.state for r in live_survey(repo, coord_dir=landed_coord, reports_dir=rdir,
                                                              terminal=terminal, live=live, gh=offline_gh)}
        check(queue_facts.read(offline_gh) is None and offline_rows.get('lane-done') == 'ENQUEUED'
              and offline_rows.get('done-branch') == 'WAITING TO LAND',
              f'offline the pull-request states are unobserved and the lifecycle is git-only: {offline_rows}')
        none_open = {r.name: r for r in survey(repo, reports=report_index(rdir, terminal), slugs={'w9999a'}, min_age=180,
                                               stale_hours=24, live=live, coord_dir=landed_coord, prs={})}
        check(none_open['lane-done'].state == 'NOT QUEUED' and 'no pull request is open' in none_open['lane-done'].why,
              f'observed with no pull request open, an exit-0 attempt at the tip is NOT QUEUED (closed), never ENQUEUED: '
              f'{none_open["lane-done"].state} {none_open["lane-done"].why}')
        # THE LANDING ORDER (kb/Work PB2981's ranking, kept by PB3023): the WAITING TO LAND rows, the most harmful lead
        # first, a compiler fix before process work at equal harm, the oldest report first; nothing else
        def waiting(name, lead, age):
            return Row(name, 'CHECK', [], [], 1, None, None, 0, 999.0, '',
                       Report(f'w1a-{lead}-report.md', 'DONE', lead, False, age), 'WAITING TO LAND')
        ranks = {'PB1': (-8, 0), 'PB2': (-4, 0), 'PB3': (0, 0), 'PB4': (0, 1)}
        eight = [waiting(f'b{i}', lead, age) for i, (lead, age) in enumerate(
            (('PB4', 500), ('PB3', 10), ('PB3', 90), ('PB1', 5), ('PB4', 50), ('PB2', 7), ('PB3', 30), ('PB4', 900)))]
        order = landing_order(eight + [dataclasses.replace(eight[0], name='landed-row', state='LANDED')],
                              lambda n: ranks.get(n, (0, 1)))
        check([r.name for r in order] == ['b3', 'b5', 'b2', 'b6', 'b1', 'b7', 'b0', 'b4'],
              f'the landing order is the most harmful lead first, compiler before process, oldest first: '
              f'{[r.name for r in order]}')
        check(verdict(list(rows.values())).startswith('=== BRANCH LIFECYCLE: 77 · 23 in flight · 6 waiting for review · '
                                                      '13 waiting to land · 1 stopped · 2 enqueued · 0 not queued · '
                                                      '0 bounced · 6 landed'),
              f'verdict line: {verdict(list(rows.values()))}')
        # the pure rule: a lock with no pid is honoured; a ledger slug matches only at a word boundary
        r0 = dataclasses.replace(rows['orphan'], lock='')
        check(lifecycle(r0, in_flight_slugs=set(), min_age=180)[0] == 'IN FLIGHT', 'a hand lock (no pid) is live')
        r1 = dataclasses.replace(rows['orphan'], name='w99990-other')
        check(lifecycle(r1, in_flight_slugs={'w9999'}, min_age=180, live=live)[0] == 'DECISION', 'slug w9999 does not bind w99990-…')
        # the in-flight slugs are the ledger entries' recorded slugs (kb/Work PB3186): a retired hand entry has none
        sdir = scratch_dir('slugs-')
        (sdir / 'inflight-groups.json').write_text(json.dumps({'groups': [
            {'wave': '9998', 'letter': 'B', 'slug': 'w9998b', 'notes': ['PB1'], 'files': ['src/F.cs'], 'at': 1.0},
            {'wave': '9998', 'letter': 'C', 'notes': ['PB2'], 'files': ['src/G.cs'], 'at': 1.0},
            {'wave': 'hand', 'letter': '', 'hand': True, 'notes': ['PB3'], 'files': ['src/H.cs'], 'at': 1.0}]}),
            encoding='utf-8', newline='\n')
        check(in_flight_slugs(sdir, lambda n: False) == {'w9998b', 'w9998c'},
              f'in-flight slugs: {in_flight_slugs(sdir, lambda n: False)}')
        # a group its report answers (it returned, SPLIT included) is no longer in flight by slug (kb/Work PB2795)
        (srep := sdir / 'reports').mkdir()
        (srep / 'w9998b-PB1-report.md').write_text('# PB1\n\n**Status:** SPLIT\n', encoding='utf-8', newline='\n')
        check(in_flight_slugs(sdir, lambda n: False, srep) == {'w9998c'},
              f'an answered group is not in flight: {in_flight_slugs(sdir, lambda n: False, srep)}')
        # --apply removes exactly the LANDED rows plus the --abandon branches whose decision a kb/Work note records,
        # archives nothing (owner 2026-10-08 00:52 PDT), and loses nothing else
        notes = tmp / 'Work'
        notes.mkdir()
        # the record names a successor before and after the word: neither passes for a decision to abandon it
        (notes / 'PB8.md').write_text('---\nid: PB8\n---\nsplit-branch carries the rest, so the orphan branch: ABANDONED '
                                      '2026-10-08, the approach was replaced by PB9 on split-branch.\nsplit-branch: not '
                                      'abandoned, a finisher takes it.\n', encoding='utf-8', newline="\n")
        # kb/Work PB3409: GitHub deleted the merged head branch of `picked` behind the tracking ref (nothing prunes it)
        origin_git = tmp / 'origin.git'
        scratch.git('push', '-q', 'origin', 'picked')
        scratch.git('fetch', '-q', 'origin')
        scratch.git('update-ref', '-d', 'refs/heads/picked', cwd=origin_git)
        check(bool(git('rev-parse', '--verify', '--quiet', 'refs/remotes/origin/picked', cwd=repo).stdout.strip()),
              'the stale tracking ref of a branch origin deleted is the fixture of the apply below')
        out = []
        n = apply(list(rows.values()), repo=repo, abandon=['orphan', 'split-branch', 'fresh', 'dirty-merged'],
                  work_dir=notes, say=out.append)
        left = set(scratch.git('branch', '--format=%(refname:short)').split())
        check(n == 7, f'--apply removed {n}, want 7 (6 LANDED + orphan): {out}')
        check(not {'merged', 'landed-content', 'agent-dead', 'orphan', 'carried', 'picked', 'key-landed'} & left, f'LANDED/abandoned branches remain: {sorted(left)}')
        check({b for b, s in want.items() if s != 'LANDED' and b != 'orphan' and not b.startswith('(')} <= left,
              f'a branch neither landed nor recorded abandoned was removed: {sorted(left)}')
        check(any('no kb/Work note records' in l and 'split-branch' in l for l in out),
              'an --abandon with no recorded decision is refused, said so')
        check(any('fresh is IN FLIGHT' in l for l in out) and any('uncommitted' in l for l in out),
              'an --abandon of an IN FLIGHT or dirty branch is refused, said so')
        # the guard reads the line alone: with `orphan` already deleted (nothing in the survey names it), the record
        # that abandoned it still does not abandon the branch it names (review finding R1-1)
        check(abandon_record('orphan', notes) == 'PB8' and abandon_record('split-branch', notes) is None,
              'a record abandons the branch the word follows, not the successor it names, with no survey to ask')
        shapes = tmp / 'Shapes'
        shapes.mkdir()
        (shapes / 'PB1.md').write_text(
            '**2026-10-10, lander: branch `old-a` (wave 1, SPLIT) ABANDONED,** superseded by `new-a`.\n'
            '`new-b` carries it, so `old-b` is ABANDONED.\n'
            '`old-c` (successor `new-c`) ABANDONED: replaced.\n'
            'Branch `old-d` is not abandoned: `new-d` ABANDONED instead.\n', encoding='utf-8', newline='\n')
        got = {b: abandon_record(b, shapes) for b in ('old-a', 'new-a', 'old-b', 'new-b', 'old-c', 'new-c', 'old-d', 'new-d')}
        check(got == {'old-a': 'PB1', 'new-a': None, 'old-b': 'PB1', 'new-b': None, 'old-c': 'PB1', 'new-c': None,
                      'old-d': None, 'new-d': 'PB1'}, f'record shapes: {got}')
        check(not list(tmp.rglob('*.bundle')), 'nothing was archived')
        check((wt / 'dirty-merged' / 'scratch.txt').exists(), 'an uncommitted file survived')
        check((wt / 'unreadable' / 'precious.txt').exists(), 'a file in an unmeasurable worktree survived')
        heads = set(scratch.git('ls-remote', '--heads', 'origin').split())
        check('refs/heads/landed-content' not in heads and any('deleted origin/landed-content' in l for l in out),
              f'a landed branch is deleted on origin too: {out}')
        check('refs/heads/merged' in heads and any('kept origin/merged' in l for l in out),
              f'a remote branch holding commits the local one did not is kept: {out}')
        # kb/Work PB3409: a branch origin already deleted is no failure: apply pruned its tracking ref first, so no
        # `could not delete` line stands beside the real ones, and origin/* says what origin holds
        check(not any('could not delete' in l for l in out)
              and not git('rev-parse', '--verify', '--quiet', 'refs/remotes/origin/picked', cwd=repo).stdout.strip()
              and any('pruned 1 remote-tracking ref' in l for l in out),
              f'a branch origin already deleted reports no error and loses its tracking ref: {out}')
        # ... and a delete origin REFUSES still reports (the real failure the old noise buried), its ref kept
        scratch.git('branch', 'denied', 'main')
        scratch.git('push', '-q', 'origin', 'denied')
        scratch.git('fetch', '-q', 'origin')
        scratch.git('config', 'receive.denyDeletes', 'true', cwd=origin_git)
        denied = []
        drop_remote('denied', scratch.git('rev-parse', 'denied'), repo=repo, say=denied.append)
        check(len(denied) == 1 and denied[0].startswith('could not delete origin/denied:')
              and bool(git('rev-parse', '--verify', '--quiet', 'refs/remotes/origin/denied', cwd=repo).stdout.strip()),
              f'a delete origin refuses is reported as the failure it is, the tracking ref kept: {denied}')
    except ScratchRepoError as e:
        fails.append(f'the fixture could not be built, so nothing above it was measured: {e}')
    finally:
        remove_tree(tmp)
    for f in fails:
        print(f'FAIL  {f}')
    print(f'=== PRUNE_WORKTREES SELF-TEST: {"GREEN" if not fails else f"RED ({len(fails)})"} ===')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')   # the rows carry '·' and '—'; session-probe reads them as UTF-8
    sys.exit(main())
