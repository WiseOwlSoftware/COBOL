#!/usr/bin/env python3
"""git_merge.py — THE ONE MERGE OF A REF INTO A BRANCH, and the dry runs of it, all plain git (kb/Work PB5110).

    python scripts/git_merge.py --self-test

⛔ NO CUSTOM MERGE DRIVER, ANYWHERE (owner, 2026-11-10: "I continue to fail to understand why we need our custom merge drivers.
Multiple developers submit changed files all the time. Sometimes those changes affect common files. Git merges them when
there's no conflict and raises a merge error to be resolved when there is a conflict. No custom merge drivers are required.
This seems a far better design."). This module replaced `merge_drivers.py` (kb/Work PB3610), whose drivers merged four shared
files by key because two branches that each added an entry beside the same neighbour made git stop. GitHub's merge queue
runs plain git with no driver, so the drivers could not protect a landing there and every branch sharing a driver-covered
file with an open pull request had to wait (kb/Work PB3642). The shared registries are one file per ENTRY now, so two
branches never edit one file for an addition and plain git merges everything, here and in the merge queue alike: the
work notes' addenda (scripts/spec/work.py `read_addenda`), the construct catalogue (scripts/spec/construct_store.py), the
traceability inventory (inventory_store.py) and the refusal registry are each a directory of files. The runtime ABI pin and
the public-API baselines change only with the compiler surface; a conflict in them is the finisher's, resolved by
regenerating (`scripts/abi_pin.py settle` writes the pin and its two numbers; RS0016 and RS0017 refuse a wrong baseline,
and `RuntimeAbiPinDriftTests` a wrong pin).

THE MERGE (`merge`): `git merge --no-commit --no-ff`, then the merge commit. It answers with a `Merged`, which tells THREE ends
apart (kb/Work PB3673): the branch now contains the ref (`clean`), files left in conflict (`conflicts`: a finisher resolves
them), and a merge git refused to start or to commit (`incomplete`: a lock, a full disk, a worktree it would overwrite; the
host's, a retry's). Both non-clean ends abort the merge and leave the branch unchanged; `keep` (a finisher) leaves a
conflicting merge in progress. THE DRY RUNS (`probe`, `merge_conflicts`, `merged_tree`, `foresee_queue`) are `git merge-tree
--write-tree`, which touches no worktree and no index.
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys
from typing import NamedTuple

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import abi_pin  # noqa: E402  (the Failure a refused merge is classified by, gate.NOT_RUN_CAUSES, kb/Work PB3673)
from standard_streams import ensure_utf8  # noqa: E402

NL = chr(10)


def git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    """`git <args>` in `repo`, output captured."""
    return subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace")


class Merged(NamedTuple):
    """What `merge` made of its ref (kb/Work PB3673): THREE ends, which a caller must tell apart because three different
    people fix them. `clean`: the branch now contains the ref. `conflicts`: the files left in conflict, a finisher's work.
    `incomplete`: git refused to start the merge or to make its commit, with the cause classified by gate
    (abi_pin.MERGE_BLOCKED: the host's, a retry's). Never both a conflict and an incomplete merge. `in_progress`: the merge
    was left in progress for a finisher (`keep`), not aborted."""
    conflicts: tuple[str, ...] = ()
    incomplete: abi_pin.Failure | None = None
    in_progress: bool = False

    @property
    def clean(self) -> bool:
        return not self.conflicts and self.incomplete is None


def _refused(what: str, p: subprocess.CompletedProcess) -> Merged:
    """The Merged of a git command that failed with no file in conflict: the host's refusal (a lock, a full disk, a
    worktree that would be overwritten), with git's own words; never reported as a conflict in some file."""
    return Merged(incomplete=abi_pin.Failure(abi_pin.MERGE_BLOCKED, f"{what} failed: {(p.stderr or p.stdout).strip()}"))


def merge(repo: pathlib.Path, ref: str, message: str, keep: bool = False) -> Merged:
    """MERGE `ref` into the checked-out branch with plain git: `clean` when the branch now contains it (the merge commit
    made); else the files left in conflict (`conflicts`) or why git refused (`incomplete`), with the merge aborted and the
    branch unchanged. A branch that only needs `ref` fast-forwards. `keep` (a finisher): a conflicting merge is left in
    progress instead of aborted, with its conflict markers."""
    if git(repo, "merge-base", "--is-ancestor", ref, "HEAD").returncode == 0:
        return Merged()                                  # the branch already contains `ref`: nothing to merge
    if git(repo, "merge-base", "--is-ancestor", "HEAD", ref).returncode == 0:
        ff = git(repo, "merge", "--ff-only", ref)
        return Merged() if ff.returncode == 0 else _refused(f"git merge --ff-only {ref}", ff)
    started = git(repo, "merge", "--no-commit", "--no-ff", ref)
    if started.returncode != 0:
        files = git(repo, "diff", "--name-only", "--diff-filter=U").stdout.split()
        if not keep:
            git(repo, "merge", "--abort")
        return Merged(tuple(files), in_progress=keep) if files else _refused(f"git merge {ref}", started)   # none: git refused
    done = git(repo, "commit", "--no-edit", "-m", message)
    if done.returncode != 0:
        git(repo, "merge", "--abort")
        return _refused("git commit of the merge", done)
    return Merged()


def merge_conflicts(repo: pathlib.Path | str, ref: str) -> list[str] | None:
    """What `merge` of `ref` into the checked-out HEAD would leave in conflict, computed without touching the worktree or
    the index: `git merge-tree`. [] when it merges clean; else the conflicted files; None when git cannot say (a missing
    ref: merge-tree exits 1 for that too, with no tree on stdout, so the tree line decides)."""
    return probe(repo, ["HEAD", ref])[1]


def probe(repo: pathlib.Path | str, parents: list[str]) -> tuple[str | None, list[str] | None]:
    """THE ONE `git merge-tree --write-tree` (kb/Work PB3723): (the merged tree, []) when `parents` merge clean; (None, the
    conflicted files) when they do not; (None, None) when git cannot say (a missing ref, unrelated histories: merge-tree
    exits 1 for that too, with no tree on stdout, so the tree line decides). The worktree and the index are not touched."""
    r = git(pathlib.Path(repo), "merge-tree", "--write-tree", "--name-only", "--no-messages", *parents)
    lines = r.stdout.splitlines()
    if r.returncode not in (0, 1) or not lines or not re.fullmatch(r"[0-9a-f]{40,64}", lines[0].strip()):
        return None, None
    if r.returncode == 0:
        return lines[0].strip(), []
    return None, [f for f in dict.fromkeys(ln.strip() for ln in lines[1:]) if f]


class Foreseen(NamedTuple):
    """A queued head that conflicts with the queue ahead of it: the files left in conflict, and the entries ahead it
    conflicts with (their labels; empty: with main alone, which GitHub already reads as DIRTY)."""
    files: tuple[str, ...]
    ahead: tuple[str, ...]


def foresee_queue(repo: pathlib.Path | str, base: str, entries: list[tuple[str, str]]) -> dict[str, Foreseen]:
    """THE MERGE QUEUE, SIMULATED (kb/Work PB3723): the queue builds each entry's merge group on main plus the entries
    ahead of it that stay, and REMOVES an entry whose head conflicts there (reason `merge_conflict`, with no failed run
    and no event a reader sees until it has happened). `entries`: (label, head commit) in queue order; `base`: main.
    Each head is merged onto the result of those before it with git's own merge, which is what GitHub's merge group runs.
    An entry that conflicts is skipped, as the queue skips it. Returns {label: Foreseen} for each such entry, so its
    finisher can bring the entries ahead in while they are still queued. A head git cannot merge (an object this repository
    lacks) is left out: it is unknown, never a conflict."""
    out: dict[str, Foreseen] = {}
    result, ahead = base, []
    for label, head in entries:
        tree, files = probe(repo, [result, head])
        if files is None:
            continue
        if tree is None:
            if probe(repo, [base, head])[1]:   # it conflicts with main itself: GitHub reads that DIRTY
                out[label] = Foreseen(tuple(files), ())
                continue
            with_head = [lab for lab, h in ahead if probe(repo, [h, head])[1]]
            out[label] = Foreseen(tuple(files), tuple(with_head or [lab for lab, _ in ahead]))
            continue
        made = git(pathlib.Path(repo), "-c", "user.name=git_merge", "-c", "user.email=git_merge@localhost",
                   "commit-tree", tree, "-p", result, "-p", head, "-m", f"queue simulation: {label}")
        if made.returncode != 0:   # the entries after this one are unknown, never clean: said, and left out
            print(f"git_merge: the queue simulation stopped at {label}: git commit-tree failed "
                  f"({made.stderr.strip()[:160]})", file=sys.stderr)
            break
        result = made.stdout.strip()
        ahead.append((label, head))
    return out


def merged_tree(repo: pathlib.Path | str, parents: list[str]) -> str | None:
    """The tree of git's own merge of `parents` (`git merge-tree --write-tree`), computed without touching the worktree or
    the index; None when it does not merge clean or git cannot say. THE ONE DEFINITION OF A CLEAN MERGE'S TREE (kb/Work
    PB3713): the review's coverage rule (`is_clean_merge`) and the branch survey's LANDED proof
    (prune_worktrees.merges_to_main_unchanged) both read it, so the survey and the landing agree on what merges clean."""
    return probe(repo, parents)[0]


def is_clean_merge(repo: pathlib.Path | str, parents: list[str], tree: str) -> bool:
    """Whether a two-parent commit whose tree is `tree` is what `merge` makes of its parents: git's own clean merge of them
    (merged_tree). This is the review's coverage rule for a merge of main (prune_worktrees.mechanical), so it must be
    exactly the merge's own definition: there is no other."""
    return merged_tree(repo, parents) == tree


def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir, remove_tree  # noqa: PLC0415  (scripts/: the one scratch fixture)

    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    root = scratch_dir("git-merge-selftest-")
    try:
        scratch = ScratchRepo.init(root / "repo")
        repo = scratch.path
        scratch.commit("base", {"a.txt": f"1{NL}2{NL}3{NL}", "b.txt": f"1{NL}2{NL}3{NL}", "kb/Work/PB1.md": "note" + NL})
        scratch.git("switch", "-q", "-c", "topic")
        scratch.commit("topic", {"b.txt": f"1{NL}2{NL}3{NL}4{NL}", "kb/Work/PB1/2026-11-10-t.md": "topic addendum" + NL})
        scratch.git("switch", "-q", "main")
        scratch.commit("main", {"a.txt": f"0{NL}1{NL}2{NL}3{NL}", "kb/Work/PB1/2026-11-10-m.md": "main addendum" + NL})
        scratch.git("switch", "-q", "topic")
        expect("dry run: two branches that each ADD a file beside one note and edit different files merge clean, no driver",
               merge_conflicts(repo, "main") == [], str(merge_conflicts(repo, "main")))
        expect("dry run: a ref git cannot read is None (git cannot say), never []",
               merge_conflicts(repo, "refs/heads/no-such-branch") is None)
        merged = merge(repo, "main", "Merge main (self-test)")
        expect("merge: both branches' files are in the merge commit, one merge, nothing left over",
               merged.clean and (repo / "kb/Work/PB1/2026-11-10-t.md").is_file() and (repo / "kb/Work/PB1/2026-11-10-m.md").is_file()
               and len(scratch.git("log", "--merges", "--format=%h").split()) == 1 and not scratch.git("status", "--porcelain"),
               str(merged))
        expect("tree: merged_tree is that merge's tree, the one definition (kb/Work PB3713); is_clean_merge reads it; a ref git "
               "cannot read is None",
               merged_tree(repo, ["HEAD^1", "HEAD^2"]) == scratch.git("rev-parse", "HEAD^{tree}")
               and is_clean_merge(repo, ["HEAD^1", "HEAD^2"], scratch.git("rev-parse", "HEAD^{tree}"))
               and not is_clean_merge(repo, ["HEAD^1", "HEAD^2"], "0" * 40)
               and merged_tree(repo, ["HEAD", "refs/heads/no-such-branch"]) is None)
        before = scratch.git("rev-parse", "HEAD")
        expect("merge: a ref the branch already contains is no conflict and no new commit (a resumed agent's first merge step)",
               merge(repo, "main", "again").clean and merge(repo, "main~1", "ancestor").clean
               and scratch.git("rev-parse", "HEAD") == before)
        # A real conflict: both branches edit the same line of the same file.
        scratch.git("switch", "-q", "-c", "left", "HEAD")
        scratch.commit("left", {"a.txt": f"0{NL}left{NL}2{NL}3{NL}"})
        scratch.git("switch", "-q", "-c", "right", "HEAD~1")
        scratch.commit("right", {"a.txt": f"0{NL}right{NL}2{NL}3{NL}"})
        tip = scratch.git("rev-parse", "HEAD")
        expect("dry run: a file both sides edited on one line is listed, a real conflict, and the worktree is untouched",
               merge_conflicts(repo, "left") == ["a.txt"] and not scratch.git("status", "--porcelain"), str(merge_conflicts(repo, "left")))
        files = merge(repo, "left", "Merge left (self-test)")
        expect("merge: a conflict names the file; the merge is aborted, the branch unchanged",
               files.conflicts == ("a.txt",) and not files.in_progress and files.incomplete is None
               and scratch.git("rev-parse", "HEAD") == tip and not scratch.git("status", "--porcelain"), str(files))
        kept = merge(repo, "left", "Merge left (self-test)", keep=True)
        in_progress = scratch.run("rev-parse", "-q", "--verify", "MERGE_HEAD").returncode == 0
        marked = "<<<<<<<" in (repo / "a.txt").read_text(encoding="utf-8")
        scratch.run("merge", "--abort")
        expect("merge: keep leaves a conflicting merge IN PROGRESS with its markers, for a finisher to resolve",
               kept.conflicts == ("a.txt",) and kept.in_progress and in_progress and marked and scratch.git("rev-parse", "HEAD") == tip,
               str(kept))
        # A git refusal with no file in conflict is `incomplete`, never a conflict: a ref that is no commit.
        refused = merge(repo, "refs/heads/no-such-branch", "Merge nothing (self-test)")
        expect("merge: a refusal git makes with no file in conflict is `incomplete` (the host's, MERGE_BLOCKED), never a conflict",
               refused.incomplete is not None and refused.incomplete.cause == abi_pin.MERGE_BLOCKED and not refused.conflicts
               and not refused.clean, str(refused))
        # THE QUEUE SIMULATED (kb/Work PB3723): e1 and e2 edit one line of a.txt, e3 another file, e4 that line again, e6 a
        # line of c.txt that main changed since the branches were cut. The queue removes an entry that conflicts on the
        # entries ahead that STAY, so e2 conflicts with e1, e3 stays, e4 conflicts with e1 (e2 is gone), e6 with main alone;
        # a head this repository lacks is unknown, never a conflict.
        qr = ScratchRepo.init(root / "queue")
        qr.commit("base", {"a.txt": f"1{NL}2{NL}3{NL}", "b.txt": f"1{NL}2{NL}3{NL}", "c.txt": f"1{NL}2{NL}3{NL}"})
        for name, path, text in (("e1", "a.txt", f"1{NL}x{NL}3{NL}"), ("e2", "a.txt", f"1{NL}y{NL}3{NL}"),
                                 ("e3", "b.txt", f"1{NL}b{NL}3{NL}"), ("e4", "a.txt", f"1{NL}z{NL}3{NL}"),
                                 ("e6", "c.txt", f"1{NL}n{NL}3{NL}")):
            qr.git("switch", "-q", "-c", name, "main")
            qr.commit(name, {path: text})
            qr.git("switch", "-q", "main")
        qr.commit("main moves", {"c.txt": f"1{NL}m{NL}3{NL}"})
        heads = {n: qr.git("rev-parse", n) for n in ("e1", "e2", "e3", "e4", "e6")}
        order = [(n, heads[n]) for n in ("e1", "e2", "e3", "e4", "e6")] + [("e5", "0" * 40)]
        foreseen = foresee_queue(qr.path, "main", order)
        expect("queue: an entry that conflicts on the entries ahead that stay is foreseen, with the entry it conflicts with "
               "(e2 and e4 with e1, the removed e2 not counted for e4), main alone when none ahead does (e6), and a clean "
               "entry or an unreadable head is not",
               foreseen == {"e2": Foreseen(("a.txt",), ("e1",)), "e4": Foreseen(("a.txt",), ("e1",)),
                            "e6": Foreseen(("c.txt",), ())}, str(foreseen))
        expect("queue: the simulation touches no worktree, and one clean entry of the queue is foreseen as nothing",
               not qr.git("status", "--porcelain") and foresee_queue(qr.path, "main", [("e2", heads["e2"])]) == {},
               qr.git("status", "--porcelain"))
        # Two entries that each ADD a file under one directory (the shape of every registry now) are never a conflict.
        ar = ScratchRepo.init(root / "queue-files")
        ar.commit("base", {"kb/Work/PB1.md": "n" + NL})
        for name in ("k1", "k2"):
            ar.git("switch", "-q", "-c", name, "main")
            ar.commit(name, {f"kb/Work/PB1/2026-11-10-{name}.md": name + NL})
            ar.git("switch", "-q", "main")
        expect("queue: two entries that each add a file beside one note are no conflict (one file per entry)",
               foresee_queue(ar.path, "main", [(n, ar.git("rev-parse", n)) for n in ("k1", "k2")]) == {})
    finally:
        remove_tree(root)
    for f in failures:
        print(f"FAIL: {f}")
    print(f"git_merge self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if "--self-test" not in sys.argv:
        sys.exit(__doc__)
    sys.exit(self_test())
