"""WHAT IS IN FLIGHT, AND WHETHER A CLAIM MEETS IT (kb/Work PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md
section 16.3; the partition of section 9, kb/Work PB2807, PB3670): the in-flight work's file sets (inflight_file_sets:
every worktree's and unlanded branch's changed files, committed or not, and every dispatch-ledger group no report
answers), the units an owner's actual diff touches in a shared file (inflight_units), the planner's reading of that
set by the branch survey's lifecycle states (classify_inflight), the open external-repository slices' reservation
(slice_reservation), the local branches and their classification as the planner reads them (local_branches,
git_branch_classifier, branch_carrier, branch_replays, unlanded_branch_notes), and the ONE meeting rule of two declared
sets (Claim, declared_sets, computed_file_set, collision_set, collision: two claims meet only on a file either claims
whole, or on a member of one that matches a member of the other).

Its readers: the wave planner (plan_wave.py), the landing check's guarantee (landing_check.py), the spec renderer
(make_dispatch_specs.py: the declared set of a group it records), check_practices.py (two groups of one wave whose
claims meet need `after:`) and the branch survey's in-flight slugs (prune_worktrees.in_flight_slugs); the member
index (scripts/arch/member_index.py) computes the declared sets declared_sets reads. In the import DAG of section 16.10
(scripts/script_layering.py) it imports
reports and, lazily, the branch survey's git facts (prune_worktrees: classify, main_names, branch_graph, replay_view,
its git runner).
"""
from __future__ import annotations

import dataclasses
import functools
import pathlib
import re
import sys
from concurrent.futures import ThreadPoolExecutor
from typing import TYPE_CHECKING, Any, Callable, Collection, Iterable, Mapping, Sequence

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
sys.path.insert(0, str(HERE.parent / "arch"))
import coord  # noqa: E402
import git_batch  # noqa: E402  (scripts/: the batched git reads, one process for many objects, kb/Work PB3582)
import merge_drivers  # noqa: E402  (scripts/: covered_by_driver, the files merged by key, which never meet)
import source_units  # noqa: E402  (scripts/arch/: the units of a file, the units a diff touches, the name match)
import work  # noqa: E402  (the slices' file set and the restructuring program's notes)
from reports import Report, answers, ledger_slug  # noqa: E402  (the report reader: "a dispatch returned")
if TYPE_CHECKING:
    from prune_worktrees import Replay  # noqa: E402  (imported lazily at run time: it imports this module back)

REPO = coord.REPO

PB = re.compile(r"\bPB\d+\b")


LANDED_CLASSES = {"MERGED", "LANDED", "ABSENT"}


def local_branches() -> tuple[set[str], "git_batch.Ancestry"]:
    """(every local branch, ONE graph of them beyond origin/main): what the planner's classifier, its carrier rule,
    add_closed_notes and unlanded_branch_notes all read, from one listing and one graph read (kb/Work PB3582), never a
    `rev-parse`, a `merge-base` or a `git log` per branch or per pair of branches."""
    import prune_worktrees  # noqa: PLC0415
    local = set(prune_worktrees.git("for-each-ref", "refs/heads", "--format=%(refname:short)").stdout.split())
    return local, prune_worktrees.branch_graph(sorted(local))


def git_branch_classifier(local: set[str], graph: "git_batch.Ancestry") -> Callable[[str], str]:
    """branch -> MERGED | LANDED | CHECK | UNLANDED | ABSENT, by prune_worktrees' content test (reused, not forked), over
    the local branches and their graph (local_branches)."""
    import prune_worktrees  # noqa: PLC0415  (git work at import time is none, but the grep below is: only on demand)
    names: list[set[str]] = []
    cache: dict[str, str] = {}

    def classify(branch: str) -> str:
        if branch not in cache:
            if branch not in local:
                cache[branch] = "ABSENT"
            else:
                names or names.append(prune_worktrees.main_names())
                cache[branch] = prune_worktrees.classify(branch, names[0], graph)[0]
        return cache[branch]
    return classify


def branch_carrier(reports: list[Report], classify: Callable[[str], str],
                   is_ancestor: Callable[[str, str], bool]) -> Callable[[str], str | None]:
    """branch -> the branch of a FINISHED report (Report.finished), not on main, that contains it (`is_ancestor`: the
    local branches' graph, git_batch.Ancestry.is_ancestor, never a `merge-base` per pair, kb/Work PB3582), or None.
    Unfinished work that a finished branch merged lands WITH that branch: a SPLIT predecessor's branch carries no clean
    stamp at its tip, so it never lands alone (prune_worktrees.contained: an unreviewed checkpoint lands through the
    branch that carries it, kb/Work PB3381). Only a finished predecessor reviewed CLEAN lands on its own, and this reads
    finished branches only as carriers. Wave 1074's group A split on `…-e99-1`, A2 merged it into `…-e99-4`, and a
    finisher that cherry-picked `…-e99-1` onto main again would redo work `…-e99-4` already carries (kb/Work PB3150)."""
    done = list(dict.fromkeys(r.branch for r in reversed(reports) if r.branch and r.finished))
    cache: dict[str, str | None] = {}

    def carried(branch: str) -> str | None:
        if branch not in cache:
            cache[branch] = next((c for c in done if c != branch and classify(c) not in LANDED_CLASSES and
                                  is_ancestor(branch, c)), None)
        return cache[branch]
    return carried


def branch_replays(reports: list[Report], classify: Callable[[str], str], unlanded: Mapping[str, Any],
                   graph: "git_batch.Ancestry", abandoned: set[str]) -> Callable[[str], "Replay | None"]:
    """branch -> how far OTHER unlanded branches replay its commits (prune_worktrees.Replay), or None for a branch the
    rule did not read. THE ONE RULE for "this branch's work is carried elsewhere" (kb/Work PB3170): `branch_carrier`
    sees only a DONE branch that CONTAINS another, but the planner's own FINISH-FIRST pred tells every SPLIT successor
    to CHERRY-PICK its predecessor (never merge it), so the successor holds the predecessor's work as other commits and
    is no descendant of it: wave 1079 planned PB3127 from `…-28e-2`, whose four commits `…-3a0-2` (another SPLIT
    report's branch, planned in the same wave) replayed. A branch carries work when it is unlanded and has a report of
    ANY status; a branch recorded ABANDONED carries nothing, and is no source either. `rank` is the position of the
    branch's newest report in `reports` (oldest first), so of two branches that hold the same work the later report's
    carries it."""
    import prune_worktrees  # noqa: PLC0415
    rank = {r.branch: i for i, r in enumerate(reports) if r.branch}
    ranked = {b: i for b, i in rank.items() if classify(b) not in LANDED_CLASSES}
    return prune_worktrees.replay_view(ranked, unlanded, abandoned, graph).get   # the survey reads the same view


def unlanded_branch_notes(local: set[str], graph: "git_batch.Ancestry") -> dict[str, list[str]]:
    """{UNLANDED or CHECK branch: [every note id its commit subjects name]} over every local branch. CHECK (a C# name
    the branch declares is missing from main) is unlanded work until someone reads it: wave 1017's group B branch,
    which only edited existing files, classified CHECK, and its WIP would have been planned over from main.
    Unfiltered: plan() keeps the ids among its LEADS, the open notes and the unseen FINDINGS leads alike, because only
    plan() knows the latter (kb/Work PB3150 review R2-1)."""
    import prune_worktrees  # noqa: PLC0415
    names = prune_worktrees.main_names()
    out = {}
    for b in sorted(local - {"main"}):   # the subjects are the graph's (local_branches), never a `git log` per branch
        if prune_worktrees.classify(b, names, graph)[0] not in ("UNLANDED", "CHECK"):
            continue
        subjects = "\n".join(graph.subject(c) for c in graph.beyond(graph.sha(b) or b))
        out[b] = sorted(set(PB.findall(subjects)), key=lambda s: int(s[2:]))
    return out


def work_names(paths: Iterable[str]) -> set[str]:
    """The paths that are work (coord.is_work: never STATUS.md, the harness's settings or TestResults/)."""
    return {s.strip() for s in paths if s.strip() and coord.is_work(s.strip())}


def range_names(git: Callable[..., Any], *revs: str, cwd: Any = None) -> set[str]:
    """THE ONE QUESTION `git diff --name-only --no-renames <revs>` asks, every path it lists, NEVER_WORK ones too (kb/Work
    PB3791). A moved file counts at BOTH paths: git's default rename detection lists only the NEW path, so the old one
    (the path a declared set and every other branch name) dropped out and an R3 `git mv` of an undeclared file passed
    (PB2118 Draft 10, the ninth refuter's K1). A caller that needs the paths no landing carries (the landing check's
    arm 0) reads this set; one that needs the work (changed_files) filters it, so a range is asked about once."""
    out = git("diff", "--name-only", "--no-renames", *revs, **({} if cwd is None else {"cwd": cwd})).stdout
    return {s.strip() for s in out.splitlines() if s.strip()}


def changed_files(git: Callable[..., Any], *revs: str, cwd: Any = None) -> set[str]:
    """THE changed-file reader of the partition (the planner's admission and the landing check): the work paths of
    range_names (coord.is_work: never STATUS.md, the harness's settings or TestResults/)."""
    return work_names(range_names(git, *revs, cwd=cwd))


def _still_differs(git: Callable[..., Any], repo: Any, revs: Sequence[str]) -> list[tuple[set[str], str]]:
    """[(the files each commit of `revs` changed since it left origin/main AND that still differ from origin/main, that
    merge base)]: a branch landed by content (squash, cherry-pick) drops out; one whose edits are not on main stays,
    however it classifies. One `merge-base` per commit and ONE `git diff-tree --stdin` for every (since-the-merge-base,
    against main) pair (git_batch.diff_names, kb/Work PB3582), never two `git diff`s per worktree and branch. No
    origin/main, or no merge base, reads empty (base ''), as the two diffs did when they failed. The base is where
    inflight_units reads the units each change touches from (kb/Work PB3670)."""
    main = git("rev-parse", "--verify", "--quiet", "refs/remotes/origin/main^{commit}", cwd=repo).stdout.strip()
    if not main or not revs:
        return [(set(), "") for _ in revs]
    with ThreadPoolExecutor(max_workers=8) as pool:   # subprocess-bound: the merge bases run side by side
        bases = list(pool.map(lambda rev: git("merge-base", main, rev, cwd=repo).stdout.strip(), revs))
    pairs = [(rev, x) for rev, b in zip(revs, bases) if b for x in (b, main)]
    names = iter(git_batch.diff_names(repo, pairs, git=git))
    out = []
    for b in bases:
        if not b:
            out.append((set(), ""))
            continue
        since, against = work_names(next(names)), work_names(next(names))
        out.append((since & against, b))
    return out


def inflight_file_sets(*, git: Callable[..., Any] | None = None, repo: pathlib.Path = REPO,
                       coord_dir: pathlib.Path | None = None, terminal: Callable[[str], bool] | None = None,
                       with_git: bool = True, meta: dict[str, dict] | None = None,
                       reports: Sequence[Report] = ()) -> dict[str, set[str]]:
    """{in-flight owner: the files it changes}: THE in-flight file set of the partition (module doc), read from
    sources that cannot miss started work, never from a branch classification (PB2118 Draft 7):
      1. every worktree but this checkout: `_still_differs(HEAD)` plus its uncommitted and untracked files (an agent's
         first edits precede its first commit);
      2. every local branch with no worktree whose tip is not on origin/main: `_still_differs(branch)`;
      3. every group in the dispatch ledger (<coord>/inflight-groups.json, read through coord.ledger_groups) with a note that
         is not terminal and that no report in `reports` ANSWERS (`answers`: a report whose slug is the group's,
         `w<wave><letter>`, written at or after the dispatch: its agent returned, so its work is its branch's, which sources 1-2 read by their actual diff, and a SPLIT
         group's unstarted residue holds no file; kb/Work PB2795): the files the group was planned on, which exist
         before the agent has a worktree or a commit. The planner, the landing check and the survey's in-flight slugs
         all pass their reports, so the three read one rule. Its writers are structured (kb/Work PB3186): the planner's record of a planned wave and the spec
         renderer's record of every group it renders (record_dispatch, through make_dispatch_specs.py), each with the
         group's exact file set and member claims; a group recorded with none is an owner with an EMPTY set, which
         meets nothing (collision, kb/Work PB3670), never dropped. An attended dispatch made without a rendered group (a direct Agent call) is in flight
         from its worktree's first edit (sources 1-2), and the landing check is the guarantee behind it.
    `git` has prune_worktrees.git's shape (*args, cwd=); `terminal(note_id)` says whether a note is landed or retired.
    `meta`, when given, receives per owner what the planner (classify_inflight) and the landing check
    (landing_check.py) need: a worktree's or branch's `rev`, `path` and `branch` (and a worktree's `uncommitted`
    files), its tip `sha`, its merge `base` with origin/main and its `committed` files (inflight_units' inputs), a
    dispatch entry's `notes`, `at`, `slug` and recorded `members`. The per-worktree git work runs on a thread pool (it is
    subprocess-bound: the ninth refuter measured 64 s, about 2 s a worktree, for 33 worktrees run one after another)."""
    out: dict[str, set[str]] = {}
    if with_git:
        if git is None:
            import prune_worktrees  # noqa: PLC0415
            git = prune_worktrees.git
        here = str(repo).replace(chr(92), "/").rstrip("/").lower()
        with_tree, trees = set(), []
        blocks = git("worktree", "list", "--porcelain", cwd=repo).stdout.split(chr(10) * 2)
        for blk in [b for b in blocks if b.strip()]:
            path = re.search(r"^worktree (.+)$", blk, re.M).group(1).strip()
            br = re.search(r"^branch refs/heads/(.+)$", blk, re.M)
            head = re.search(r"^HEAD ([0-9a-f]+)$", blk, re.M)
            if br:
                with_tree.add(br.group(1))
            if path.replace(chr(92), "/").rstrip("/").lower() != here:
                trees.append((path, br.group(1) if br else None, head.group(1) if head else ""))

        def uncommitted(t: tuple[str, str | None, str]) -> set[str]:
            """Its uncommitted and untracked files from ONE `git status` (kb/Work PB3582: it was a `diff HEAD` and an
            `ls-files --others`), without the optional index write, which would race the agent working there."""
            out = git("--no-optional-locks", "status", "--porcelain", "-z", "--no-renames", "--untracked-files=all",
                      cwd=t[0]).stdout
            return work_names(e[3:] for e in out.split(chr(0)) if len(e) > 3)

        # every local branch with no worktree whose tip is not on origin/main, with its tip, from ONE listing (no
        # `merge-base --is-ancestor` per branch); with no origin/main the listing fails and no branch is listed, as
        # each branch's two diffs then read empty
        listed = git("for-each-ref", "--no-merged=refs/remotes/origin/main", "--format=%(refname:short) %(objectname)",
                     "refs/heads", cwd=repo)
        loose_branches = [(b, sha) for b, _, sha in (ln.partition(" ") for ln in listed.stdout.splitlines()
                                                    if listed.returncode == 0)
                          if b != "main" and b not in with_tree and sha]
        with ThreadPoolExecutor(max_workers=8) as pool:
            tree_loose = list(pool.map(uncommitted, trees))
        revs = list(dict.fromkeys([h for _, _, h in trees if h] + [sha for _, sha in loose_branches]))
        differs = dict(zip(revs, _still_differs(git, repo, revs)))
        tree_rows = [(path, br, differs.get(head, (set(), ""))[0] if head else set(), lo, head if meta is not None else "")
                     for (path, br, head), lo in zip(trees, tree_loose)]
        branch_rows = [(b, sha, differs.get(sha, (set(), ""))[0]) for b, sha in loose_branches]
        for path, br, files, loose, head in tree_rows:
            if files | loose:
                owner = f"worktree {pathlib.PurePath(path).name} ({br or 'detached'})"
                out[owner] = files | loose
                if meta is not None:   # `sha` and `base` are what inflight_units diffs (kb/Work PB3670)
                    meta[owner] = {"rev": head, "uncommitted": loose, "path": path, "branch": br, "sha": head,
                                   "base": differs.get(head, (set(), ""))[1], "committed": files}
        for b, sha, files in branch_rows:
            if files:
                out[f"branch {b}"] = files
                if meta is not None:
                    meta[f"branch {b}"] = {"rev": b, "uncommitted": set(), "path": None, "branch": b, "sha": sha,
                                           "base": differs.get(sha, (set(), ""))[1], "committed": files}
    for g in coord.ledger_groups(coord_dir or coord.coord_dir()):
        if (terminal is not None and all(terminal(i) for i in g.get("notes", []))) or any(answers(r, g) for r in reports):
            continue
        # A group recorded with NO files is in flight all the same, with its notes (its own notes are never held twice:
        # plan() reads them), and its EMPTY set meets nothing (collision, kb/Work PB3670); a recorded group's `members`
        # are the members it claims in its files (a file with none is claimed whole)
        owner = f"dispatched {ledger_slug(g)} ({', '.join(g.get('notes', [])[:3])})"
        out[owner] = {f.replace(chr(92), "/") for f in g.get("files", [])}
        if meta is not None:
            meta[owner] = {"notes": list(g.get("notes", [])), "at": g.get("at", 0.0), "slug": ledger_slug(g),
                           "members": dict(g.get("members") or {})}
    return out


@dataclasses.dataclass(frozen=True)
class IndexPolicy:
    """WHICH member index a declared file set is computed on: THE index of the tree (member_index.current: a committed
    or cached record whose treeKey is the tree's, else one regenerated into `cache_dir`, else, with `regenerate` off, a
    refusal). One policy for every reader, passed explicitly (kb/Work PB3203): the planner's admission and the landing
    check's guarantee are the same quantity, and the landing check's copy once read the newest COMMITTED index, which
    cannot see a file or type a landing added (PB2118 Draft 9, the eighth refuter's K1 (3)). Regenerating costs a
    solution build plus the census host (110 s measured on a cold worktree, 2026-10-16), paid at most once per tree.
    `record_dir` is where a regeneration is written INSTEAD of `cache_dir` (the cache is then only read): a dry run, which
    writes nothing to the coordination directory, regenerates into its own temporary one (kb/Work PB3803)."""
    cache_dir: pathlib.Path
    regenerate: bool = True
    record_dir: pathlib.Path | None = None

    @classmethod
    def of(cls, coord_dir: pathlib.Path, regenerate: bool = True, record_dir: pathlib.Path | None = None) -> "IndexPolicy":
        """The coordination directory's cache, `<coord>/member-index` (DESIGN-architecture-review §8.7)."""
        return cls(coord_dir / "member-index", regenerate, record_dir)


@dataclasses.dataclass(frozen=True)
class Claim:
    """A DECLARED SET as the partition reads it (DESIGN-architecture-review §8.7; kb/Work PB3670): the `files` a group,
    a note or an in-flight owner changes and, per file, the `members` it changes there (scripts/arch/source_units.py's
    units: a C# member, a Python function, a Markdown section); a file with no `members` entry is claimed WHOLE. `sited`
    is False when a note behind it declares no site block (work.NoteSites.declared): its set is only what the planner
    located for it, so a group holding one claims its primary file whole (collision_set). Union (`|`) keeps a file
    whole when either side claims it whole."""
    files: frozenset[str] = frozenset()
    members: Mapping[str, frozenset[str]] = dataclasses.field(default_factory=dict, hash=False)
    sited: bool = True

    def __or__(self, other: "Claim") -> "Claim":
        files = self.files | other.files
        members = {f: frozenset().union(*(c.members[f] for c in (self, other) if f in c.files))
                   for f in files if all(f in c.members for c in (self, other) if f in c.files)}
        return Claim(files, members, self.sited and other.sited)

    @classmethod
    def of(cls, files: Iterable[str], members: Mapping[str, Iterable[str]] | None = None, sited: bool = True) -> "Claim":
        """A claim from repo-relative paths (either separator) and {path: member names} (a groups file's `member_set`,
        a ledger entry's `members`); a member entry for a file outside `files` is ignored."""
        fs = frozenset(f.replace(chr(92), "/") for f in files)
        ms = {f.replace(chr(92), "/"): frozenset(v) for f, v in (members or {}).items()}
        return cls(fs, {f: v for f, v in ms.items() if f in fs}, sited)

    def member_lists(self) -> dict[str, list[str]]:
        """{file: sorted member names}, the JSON form a groups file and the dispatch ledger record."""
        return {f: sorted(v) for f, v in sorted(self.members.items())}


def declared_sets(repo: pathlib.Path, policy: IndexPolicy) -> Callable[[str], Claim]:
    """THE declared set of a note, as a function of its text (DESIGN-architecture-review §8.7; kb/Work PB3242,
    PB3203, PB3670): its STRUCTURED sites (work.note_sites, the one reader), the members they name in a file, and every
    caller the member index finds of the names it moves or changes (member_index.note_file_set), on the index `policy`
    names. The index and the drifted files are read once, and only when a note needs them (a note that names what it
    moves or changes, or a restructuring note): a fix-lane plan whose notes declare only sites never builds one. A
    missing or refused index is a refusal."""
    import member_index  # noqa: PLC0415  (scripts/arch/, on the path above: this script's own, whatever tree it measures)

    @functools.cache
    def index() -> dict:
        return member_index.current(repo, policy.cache_dir, policy.regenerate,
                                    recorder=(lambda r, _cache: member_index.record(r, policy.record_dir))
                                    if policy.record_dir else None)

    @functools.cache
    def drifted() -> dict[str, str]:
        return member_index.drifted_files(index(), repo)

    def claim(text: str) -> Claim:
        fs = member_index.note_file_set(text, repo, index, drifted)
        return Claim.of(fs["files"], fs["members"], fs["sited"])

    return claim


def computed_file_set(repo: pathlib.Path, ids: list[str], policy: IndexPolicy) -> Claim:
    """The declared set of these notes computed NOW (declared_sets over their kb/Work files, united): the set of work
    the dispatch ledger never recorded (landing_check.py reads its `files`) and of a rendered group whose groups file
    carries no `file_set` (make_dispatch_specs.py records its files and members). A note that is not a kb/Work file adds
    nothing."""
    declared = declared_sets(repo, policy)
    out = Claim()
    for i in ids:
        if (f := repo / "kb" / "Work" / f"{i}.md").is_file():
            out = out | declared(f.read_text(encoding="utf-8", errors="replace"))
    return out


def collision_set(primary: str, declared: list[Claim]) -> Claim:
    """A group's declared set, the one the partition checks (kb/Work PB3242, PB3670): its notes' declared sets united,
    plus its primary file, for the fix lane and a campaign alike. The primary file (fix_clusters' scored reading of the
    notes' prose, or a campaign cluster's site) is claimed WHOLE unless every note of the group declares a site block
    and one of them names it: then the notes' own reading (a member or the whole file) decides. The fix lane's
    prose-scored sites (fix_clusters' `files`) rank and group its notes and never enter it otherwise."""
    out = functools.reduce(lambda a, b: a | b, declared, Claim())
    if primary and (not out.sited or primary not in out.files):
        out = out | Claim(frozenset({primary}))
    return out


def unit_meet(f: str, mine: Claim, theirs: Claim) -> str | None:
    """Whether two claims meet on the file `f` both hold, and on what: `f` when either claims it whole, `f#<member>`
    when a member of one matches a member of the other (source_units.match: `collision` meets `inflight.collision`,
    a type meets its members, a section its subsections), None when their members are disjoint. A file a merge driver
    merges by key (merge_drivers.covered_by_driver: constructs.json, the work
    notes) never meets: two landings both editing it merge by entry, the rule enqueue.py's re-gate already reads
    (kb/Work PB3610, PB3612)."""
    if merge_drivers.covered_by_driver(f):
        return None
    a, b = mine.members.get(f), theirs.members.get(f)
    if a is None or b is None:
        return f
    hit = source_units.meet(a, b)
    return f"{f}#{hit}" if hit else None


def collision(mine: Claim, theirs: Claim) -> str | None:
    """THE meeting rule of the partition (kb/Work PB2118 question 5, PB3670), or None when the two never meet: the
    label of what they share, the first shared file (by name) on which they meet (unit_meet: a member of it, or the
    whole file). Two groups on DIFFERENT MEMBERS of one file never meet: git's three-way merge combines them, and the
    landing's merge conflict, when one still happens, is repaired then (the owner, 2026-10-24: "Developers do this all
    the time"). An EMPTY set meets NOTHING (kb/Work PB3670, superseding PB3263's "unknown meets its subsystem"): a group
    none of whose notes declares a site block, or a dispatch the ledger recorded with no files, holds nothing and is
    held by nothing; the plan and its spec name it UNPARTITIONED (unpartitioned), its implementer adds the `**Sites:**`
    paragraph in its first checkpoint, and from its first edit its worktree's actual diff is in flight like any other.
    Paths are repo-relative POSIX."""
    for f in sorted(mine.files & theirs.files):
        if hit := unit_meet(f, mine, theirs):
            return hit
    return None


def collision_hits(mine: Claim, busy: Mapping[str, Claim]) -> list[tuple[str, str]]:
    """The in-flight owners a group meets, each with what they share, as (owner, hit), by THE one rule (collision):
    `mine` is the group's claim, `busy` each owner's. `hit` is a file or `file#member`, never prose."""
    return [(owner, hit) for owner, theirs in sorted(busy.items()) if (hit := collision(mine, theirs))]


def file_set_collisions(mine: Claim, busy: Mapping[str, Claim]) -> list[str]:
    """The same owners as the text of a hold reason, each '<owner> (<what they share>)' (collision_hits). The text is
    for a person: the partition's measurement records the hits themselves (partition_record), never parses this."""
    return [f"{owner} ({hit})" for owner, hit in collision_hits(mine, busy)]


@dataclasses.dataclass
class InFlight:
    """One owner of in-flight work as the planner's partition reads it (classify_inflight, kb/Work PB2807): the files
    it changes and the members it changes in them (`members`, kb/Work PB3670: a dispatch's recorded member claims, or
    the units a worktree's or branch's actual diff touches, inflight_units; a file with no entry is changed WHOLE),
    its branch (a worktree's or an unlanded branch's), the notes it works on (a dispatch's notes; a branch's newest
    report's ids and its commit subjects' ids), its lifecycle state (the branch survey's, or the dispatch's), and the
    FINISHED report that names its branch when it is finished work. A group whose claim meets a finished owner
    CONTINUES it; one whose claim meets any other owner waits. `reserves` marks a RESERVATION, not work: the file set
    of open notes nobody need be running (the external-repository slices, slice_reservation), held only from the
    groups holding one of these notes; None holds every group."""
    files: set[str]
    branch: str | None = None
    notes: set[str] = dataclasses.field(default_factory=set)
    finished: Report | None = None
    state: str = "RUNNING"
    reserves: set[str] | None = None
    members: dict[str, frozenset[str]] = dataclasses.field(default_factory=dict)

    @property
    def claim(self) -> Claim:
        return Claim.of(self.files, self.members)


#: The branch survey's lifecycle states (prune_worktrees.lifecycle, MANDATORY-PRACTICES O10) of FINISHED work: a
#: finished report covers the tip and nobody is working on it, so a group on its files continues it (merges it first).
CONTINUED_STATES = ("WAITING FOR REVIEW", "WAITING TO LAND", "ENQUEUED", "NOT QUEUED")


#: ... and the state whose work is on main already: nothing in flight.
GONE_STATES = ("LANDED",)


def classify_inflight(busy: dict[str, set[str]], meta: dict[str, dict], reports: list[Report],
                      states: dict[str, str], branch_notes: dict[str, list[str]] | None = None) -> dict[str, InFlight]:
    """THE planner's reading of the in-flight set (inflight_file_sets and its `meta`), ONE rule for the fix lane and
    a campaign (kb/Work PB2807), keyed on the branch survey's lifecycle `states` ({branch: state},
    prune_worktrees.live_survey, the one lifecycle rule, the open pull requests included):
      - a worktree or unlanded branch the survey reads WAITING FOR REVIEW, WAITING TO LAND or ENQUEUED is FINISHED
        work (CONTINUED_STATES): it carries the newest report whose branch field names it, and a group meeting it is
        planned as its CONTINUATION (plan(): merge it first);
      - one the survey reads LANDED is dropped (its work is on main);
      - a dispatch-ledger group a report answers never reaches it (inflight_file_sets drops it at source 3, the one
        rule the landing check shares), so every dispatch owner here is DISPATCHED, NO REPORT YET;
      - everything else is RUNNING, and a group meeting it waits: a dispatch with no report yet, a branch IN FLIGHT
        (locked by a live agent, touched within the survey's --min-age, a dispatch's or an in-flight branch's), one
        STOPPED or BOUNCED at its landing (its finisher works on it next), one undecided (DECISION: unlanded work no
        session has decided, MANDATORY-PRACTICES O10; the collision names it, so the operator decides it), or a
        detached worktree or owner the survey does not name;
      - a RESERVATION (its meta's `reserves`, slice_reservation: the open external-repository slices) is no work of its
        own: it reads OPEN (RESERVED) while no other owner works on one of its notes, and RUNNING (naming that owner)
        once one does, so a plan never calls a slice nobody runs RUNNING (kb/Work PB3456).
    `branch_notes` ({branch: the note ids its commit subjects name}, unlanded_branch_notes) and each branch's newest
    report's `notes` (answered_notes, plus what an unlanded branch closes, add_closed_notes) are the branch's notes, plus any `notes` its meta names (the external-repository slices' own notes):
    plan() never defers a group for work on its OWN notes. A dispatch's `members` are the member claims its ledger
    entry recorded; a worktree's or branch's are read from its actual diff afterwards (inflight_units, kb/Work PB3670),
    and until then every file is changed whole."""
    by_branch: dict[str, Report] = {}
    for r in reports:   # oldest first: the last write is the newest
        if r.branch:
            by_branch[r.branch] = r
    out: dict[str, InFlight] = {}
    for owner, files in busy.items():
        m = meta.get(owner, {})
        if m.get("slug"):
            out[owner] = InFlight(set(files), notes=set(m.get("notes", [])), state="DISPATCHED, NO REPORT YET",
                                  members=dict(Claim.of(files, m.get("members")).members))
            continue
        if "reserves" in m:   # its state is read below, from the work on its notes
            out[owner] = InFlight(set(files), notes=set(m.get("notes", [])), reserves=set(m["reserves"]))
            continue
        branch = m.get("branch")
        state = states.get(branch or "", "RUNNING")
        if state in GONE_STATES:
            continue
        rep = by_branch.get(branch or "")
        notes = set(rep.notes if rep else []) | set((branch_notes or {}).get(branch or "", [])) | set(m.get("notes", []))
        done = rep if state in CONTINUED_STATES and rep and rep.finished else None
        out[owner] = InFlight(set(files), branch, notes, done, state)
    for x in out.values():
        if x.reserves is not None:
            carriers = sorted(o for o, y in out.items() if y.reserves is None and y.notes & x.notes)
            x.state = f"RUNNING ({'; '.join(carriers)})" if carriers else "OPEN (RESERVED)"
    return out


_DIFF_FILE = re.compile(r"^diff --git a/(?:.+) b/(.+)$", re.M)


#: The longest pathspec inflight_units passes on one command line (Windows caps a command line at 32,767 characters);
#: past it the diff reads every file and keeps the shared ones.
PATHSPEC_CHARS = 20000


def bounded_pathspec(files: Iterable[str]) -> list[str]:
    """`files` sorted as a pathspec for one git command line, or [] (the whole diff: the caller keeps the files it
    wants, shared_hunks) when they would pass PATHSPEC_CHARS. THE one rule of both diff arms of inflight_units, the
    committed and the uncommitted (kb/Work PB3786: a worktree in the middle of a merge holds every file main changed as
    uncommitted, 3,331 paths, and the unbounded arm died with WinError 206)."""
    spec = sorted(set(files))
    return spec if sum(len(f) + 1 for f in spec) <= PATHSPEC_CHARS else []


def split_patch(patch: str) -> dict[str, list[tuple[int, int, int, int]]]:
    """{file: its hunks} of a unified diff (renames off: the `b/` path names the file; a quoted path is skipped, and the
    file then reads whole)."""
    heads = list(_DIFF_FILE.finditer(patch))
    return {h.group(1): source_units.hunks(patch[h.end():heads[k + 1].start() if k + 1 < len(heads) else len(patch)])
            for k, h in enumerate(heads)}


def shared_hunks(patch: str, shared: Collection[str]) -> dict[str, list[tuple[int, int, int, int]]]:
    """split_patch of `patch`, kept to the `shared` files: a diff read without a pathspec (bounded_pathspec) names every
    changed file, and a file nobody else holds is never read."""
    return {f: hs for f, hs in split_patch(patch).items() if f in shared}


def inflight_units(busy: Mapping[str, InFlight], meta: Mapping[str, dict], claims: Iterable[Claim],
                   git: Callable[..., Any], repo: pathlib.Path = REPO) -> None:
    """Give each worktree's and unlanded branch's InFlight the units its ACTUAL diff touches in every file it shares
    with another claim (kb/Work PB3670), in place: `members[f]` for each such file whose versions the unit reader reads
    (scripts/arch/source_units.py `touched`: both sides of every hunk), and no entry (the whole file) for one added,
    deleted, untracked or unreadable. A file no other owner and no candidate group (`claims`) holds can meet nothing,
    so it is never read. The reads are batched (GitBatchDriftTests, kb/Work PB3582): ONE `git diff-tree --stdin -p -U0`
    over every (tip, merge base) pair, ONE `git cat-file --batch-check` and ONE `git cat-file --batch` for every version
    read, and one `git diff -U0 HEAD` per worktree with an uncommitted shared file (the one `git status` per worktree
    inflight_file_sets already runs is the same question kind). A git failure leaves every file whole: an unmeasured
    unit is never read as "touches nothing"."""
    seen: set[str] = set()
    shared: set[str] = set()
    for fs in [x.files for x in busy.values()] + [c.files for c in claims]:
        shared |= seen & fs
        seen |= fs
    rows = [(o, x, meta[o]) for o, x in busy.items() if meta.get(o, {}).get("sha") and x.files & shared]
    if not rows:
        return
    try:
        committed = [(o, x, m) for o, x, m in rows if m.get("base") and set(m.get("committed", ())) & shared]
        spec = bounded_pathspec(f for _, _, m in committed for f in set(m["committed"]) & shared)
        patches = git_batch.diff_patches(repo, [(m["sha"], m["base"]) for _, _, m in committed], *spec, git=git)
        hunks_of = {o: shared_hunks(p, shared) for (o, _, _), p in zip(committed, patches)}
        loose_of: dict[str, dict[str, list[tuple[int, int, int, int]]]] = {}
        for o, x, m in rows:
            if (lf := set(m.get("uncommitted", ())) & shared) and m.get("path"):
                r = git("diff", "-U0", "--no-renames", "HEAD", "--", *bounded_pathspec(lf), cwd=m["path"])
                if r.returncode != 0:
                    raise git_batch.GitError(f"git diff HEAD in {m['path']} failed: {r.stderr.strip()[:200]}")
                loose_of[o] = shared_hunks(r.stdout, lf)
        specs = sorted({f"{m['base']}:{f}" for o, _, m in committed for f in hunks_of[o]}
                       | {f"{m['sha']}:{f}" for o, _, m in committed for f in hunks_of[o]}
                       | {f"{m['sha']}:{f}" for o, _, m in rows for f in loose_of.get(o, {})})
        oids = dict(zip(specs, git_batch.object_ids(repo, specs, git=git)))
        texts = {oid: data.decode("utf-8", "replace") for oid, data in git_batch.blobs(repo, oids.values())}
    except git_batch.GitError:
        return

    def version(rev: str, f: str) -> str | None:
        return texts.get(oids.get(f"{rev}:{f}") or "")

    for o, x, m in rows:
        members: dict[str, frozenset[str]] = {}
        for f in sorted(x.files & shared):
            parts: list[frozenset[str] | None] = []
            if f in set(m.get("committed", ())):
                hs = hunks_of.get(o, {}).get(f)
                parts.append(None if hs is None else source_units.touched(f, version(m["base"], f), version(m["sha"], f), hs))
            if f in set(m.get("uncommitted", ())):
                hs = loose_of.get(o, {}).get(f)
                work_file = pathlib.Path(m["path"]) / f if m.get("path") else None
                new = work_file.read_text(encoding="utf-8", errors="replace") if work_file and work_file.is_file() else None
                parts.append(None if hs is None else source_units.touched(f, version(m["sha"], f), new, hs))
            if parts and all(p is not None for p in parts):
                members[f] = frozenset().union(*parts)
        x.members = members


#: The in-flight owner the open external-repository slices' reservation is filed under.
SLICE_OWNER = "the open external-repository slices"


def slice_reservation(items: list[dict], campaign: str | None,
                      file_set: Callable[[], set[str]] = work.slice_file_set) -> tuple[set[str], dict] | None:
    """The open external-repository slices' RESERVATION for one plan, as (its file set, its classify_inflight meta), or
    None when there is none to make. R69 §2 reserves DESIGN-external-repository §12's in-flight set (`file_set`,
    work.slice_file_set) from the restructuring program's waves only, "R2 and every R3 wave" (work.restructuring),
    whichever campaign proposes them: a fix campaign's group is never held for slices nobody runs (kb/Work PB3456:
    PB3353's tier 1 and its tier-2 chain waited on docs/CONFORMANCE.md for them), and the fix lane, which plans no
    restructuring wave, never reads the set (DESIGN-architecture-review §8.7, set (b)). The meta names the slices (the
    owner's notes, whose work in flight makes the reservation RUNNING) and `reserves`, the open restructuring notes
    whose groups it holds; every file of the set is reserved whole."""
    slices = work.open_slices(items)
    reserved = sorted((i["id"] for i in items if work.restructuring(i) and i.get("status") not in work.TERMINAL_STATUSES),
                      key=work.id_order)
    if not campaign or not slices or not reserved:
        return None
    return file_set(), {"notes": slices, "reserves": reserved}
