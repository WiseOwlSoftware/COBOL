#!/usr/bin/env python3
"""compact_notes.py — FOLD A WORK NOTE'S ADDENDA BACK INTO THE NOTE, ONLY WHERE NOTHING ELSE IS IN FLIGHT (kb/Work PB5110, item 6).

    python scripts/orchestrator/compact_notes.py [--apply] [--repo DIR]     # what would fold, and what is held and by whom
    python scripts/orchestrator/compact_notes.py --self-test

⛔ WHY. A finding, lead or landing paragraph added to a note after it is filed is a NEW FILE beside it
(`kb/Work/PB5083/2026-11-10-confirm-85.md`, scripts/spec/work.py `read_addenda`), so two branches never edit one file for an
append and plain git merges both. One file per entry keeps the file count growing, so this command folds the addenda back into
the note itself (the file becomes `work.fold_text`: the note, then each addendum, which is exactly what `work.read_notes`
already returns for it, so the fold leaves the rendered note text unchanged). It can never create a merge conflict, because it
touches a note only when NO other work is in flight on a file it would change:

  * no open pull request changed the note or any of its addenda: each open pull request's head is fetched and diffed against
    its own merge base with origin/main, in git, never GitHub's file list, which stops at 100 files;
  * no live worktree, local branch or dispatched group touches one (`inflight.inflight_file_sets`: committed, uncommitted and
    untracked files of every worktree but this checkout).

A note some other work touches is HELD, with who holds it, and a later run folds it. When the open pull requests cannot be read
(no `gh`, no network) NOTHING folds: an unknown is never a quiet registry. `--apply` writes the folds and deletes the folded
addenda in the working tree; the result lands through `enqueue.py land` like any branch.

The other registries are one file per ENTRY (a construct, an inventory rule, a refusal): a file there IS the entry, with no
addendum to fold into it, so this is the register of notes alone.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
from typing import Any, Callable, Mapping

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import coord  # noqa: E402
import inflight  # noqa: E402  (the in-flight file sets: every worktree, local branch and dispatched group)
import work  # noqa: E402  (scripts/spec: the note reader and its one composition, fold_text)
from standard_streams import ensure_utf8  # noqa: E402

REPO = coord.REPO
PULL_REF = "refs/compact/pr-{}"

Git = Callable[..., Any]   # prune_worktrees.git's shape: (*args, cwd=) -> CompletedProcess


class Unreadable(Exception):
    """The open pull requests could not be read: nothing folds."""


def open_pull_requests(repo: pathlib.Path, git: Git) -> list[tuple[str, str]]:
    """[(label, head sha)] of every open pull request, its head fetched so its commits are in `repo`. Raises Unreadable."""
    try:
        listed = subprocess.run(["gh", "pr", "list", "--state", "open", "--limit", "500", "--json", "number,headRefOid"],
                                cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace")
    except OSError as e:
        raise Unreadable(f"gh could not run: {e}") from e
    if listed.returncode != 0:
        raise Unreadable(f"gh pr list failed: {(listed.stderr or listed.stdout).strip()[:300]}")
    try:
        prs = [(f"PR #{p['number']}", p["headRefOid"], p["number"]) for p in json.loads(listed.stdout)]
    except (ValueError, KeyError, TypeError) as e:
        raise Unreadable(f"gh pr list answered something that is not the pull request list: {e}") from e
    if prs:
        fetched = git("fetch", "-q", "origin", *(f"+refs/pull/{n}/head:{PULL_REF.format(n)}" for _, _, n in prs), cwd=repo)
        if fetched.returncode != 0:
            raise Unreadable(f"git fetch of the pull request heads failed: {(fetched.stderr or fetched.stdout).strip()[:300]}")
    return [(label, sha) for label, sha, _ in prs]


def heads_files(repo: pathlib.Path, git: Git, heads: list[tuple[str, str]], main: str = "origin/main") -> dict[str, set[str]]:
    """{label: the files the head changed since it left `main`}: `git diff --name-only <merge base> <head>` for each, in git.
    A head with no merge base with `main` is Unreadable, never an empty set."""
    out: dict[str, set[str]] = {}
    for label, sha in heads:
        base = git("merge-base", main, sha, cwd=repo)
        if base.returncode != 0 or not base.stdout.strip():
            raise Unreadable(f"{label}: no merge base of {main} and {sha[:10]}")
        out[label] = inflight.range_names(git, base.stdout.strip(), sha, cwd=repo)
    return out


def busy_files(repo: pathlib.Path, git: Git, heads: list[tuple[str, str]], coord_dir: pathlib.Path | None = None,
               ) -> dict[str, set[str]]:
    """{owner: the files it changes}: every open pull request (heads_files) and every in-flight worktree, branch and
    dispatched group (inflight.inflight_file_sets)."""
    busy = heads_files(repo, git, heads)
    busy.update(inflight.inflight_file_sets(git=git, repo=repo, coord_dir=coord_dir))
    return busy


def foldable(work_dir: pathlib.Path) -> dict[str, list[str]]:
    """{note file: its addenda's file names} of every note that has addenda."""
    out: dict[str, list[str]] = {}
    for p in sorted(work_dir.glob("*.md")):
        found, bad = work.read_addenda(work_dir, p.name)
        if bad:
            raise SystemExit("compact: " + "; ".join(bad))
        if found:
            out[p.name] = [n for n, _ in found]
    return out


def plan(repo: pathlib.Path, work_dir: pathlib.Path, busy: Mapping[str, set[str]]) -> tuple[list[str], dict[str, list[str]]]:
    """(the notes that fold, {a held note: the owners that touch it}). A note is held by any owner whose files include the
    note's file or one of its addenda."""
    rel = work_dir.relative_to(repo).as_posix()
    folds, held = [], {}
    for note, addenda in foldable(work_dir).items():
        mine = {f"{rel}/{note}", *(f"{rel}/{note.removesuffix('.md')}/{a}" for a in addenda)}
        # a path under the note's addenda directory counts too: an addendum another branch adds is the same note
        stem = f"{rel}/{note.removesuffix('.md')}/"
        owners = sorted(o for o, files in busy.items() if mine & files or any(f.startswith(stem) for f in files))
        if owners:
            held[note] = owners
        else:
            folds.append(note)
    return folds, held


def apply(work_dir: pathlib.Path, notes: list[str]) -> None:
    """Fold each of `notes`: the note file becomes `work.fold_text`, and its addenda are deleted."""
    for note in notes:
        found, _ = work.read_addenda(work_dir, note)
        path = work_dir / note
        path.write_text(work.fold_text(path.read_text(encoding="utf-8"), [t for _, t in found]), encoding="utf-8", newline=chr(10))
        directory = work.addendum_dir(work_dir, note)
        for name, _ in found:
            (directory / name).unlink()
        directory.rmdir()


def run(repo: pathlib.Path, do_apply: bool, git: Git, say: Callable[[str], None] = print) -> int:
    work_dir = repo / "kb" / "Work"
    if not foldable(work_dir):
        say("compact: no note has addenda; nothing to fold")
        return 0
    try:
        folds, held = plan(repo, work_dir, busy_files(repo, git, open_pull_requests(repo, git)))
    except Unreadable as e:
        say(f"compact: NOTHING FOLDED — {e}")
        return 2
    for note, owners in held.items():
        say(f"HELD  {note}: touched by {'; '.join(owners)}")
    for note in folds:
        say(f"FOLD  {note}")
    if do_apply:
        apply(work_dir, folds)
        say(f"compact: folded {len(folds)} note(s), held {len(held)}")
    else:
        say(f"compact: would fold {len(folds)} note(s), held {len(held)} (--apply writes them)")
    return 0


def self_test() -> int:
    import tempfile  # noqa: PLC0415

    from scratch_repo import ScratchRepo  # noqa: PLC0415  (scripts/: the one scratch fixture)

    failures: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            failures.append(what)

    note = chr(10).join(["---", "id: PB{n}", "kind: decision", "status: open", "area: tooling", "---", "# PB{n}", "", "body", ""])
    with tempfile.TemporaryDirectory(prefix="compact-notes-") as tmp:
        root = pathlib.Path(tmp)
        repo = ScratchRepo.init(root / "repo")
        origin = repo.origin(root / "origin.git")
        files = {f"kb/Work/PB{n}.md": note.format(n=n) for n in (9101, 9102, 9103)}
        files |= {"kb/Work/PB9101/2026-11-10-a.md": "**A.** one" + chr(10), "kb/Work/PB9101/2026-11-11-b.md": "**B.** two" + chr(10),
                  "kb/Work/PB9102/2026-11-10-c.md": "**C.** three" + chr(10)}
        repo.commit("base", files)
        repo.git("push", "-q", "origin", "main")
        repo.git("fetch", "-q", "origin")
        git = lambda *a, cwd=None, stdin=None: repo.run(*a, cwd=cwd, input=stdin)   # noqa: E731
        work_dir = repo.path / "kb" / "Work"
        before = {it["id"]: it["_body"] for it in work.read_notes(work_dir)[0]}

        # A QUIET REGISTRY FOLDS: no open pull request, no other worktree.
        folds, held = plan(repo.path, work_dir, busy_files(repo.path, git, [], coord_dir=root / "coord"))
        check(folds == ["PB9101.md", "PB9102.md"] and not held, f"a quiet registry folds both notes with addenda ({folds}, {held})")

        # AN IN-FLIGHT TOUCH HOLDS: a pull request head that adds an addendum to PB9101, and a worktree that edits PB9102's.
        repo.git("checkout", "-q", "-b", "pr-branch")
        repo.commit("an open pull request adds an addendum", {"kb/Work/PB9101/2026-11-12-d.md": "**D.** four" + chr(10)})
        pr_sha = repo.git("rev-parse", "HEAD")
        repo.git("checkout", "-q", "main")
        wt = repo.worktree(root / "wt", branch="topic", start="main")
        wt.write({"kb/Work/PB9102/2026-11-10-c.md": "**C.** edited" + chr(10)})
        busy = busy_files(repo.path, git, [("PR #1", pr_sha)], coord_dir=root / "coord")
        folds, held = plan(repo.path, work_dir, busy)
        check(folds == [] and sorted(held) == ["PB9101.md", "PB9102.md"], f"an in-flight touch holds its note ({folds}, {held})")
        check("PR #1" in held.get("PB9101.md", []) and held.get("PB9102.md") == ["worktree wt (topic)"], f"each hold names its owner ({held})")
        wt.write({"kb/Work/PB9102/2026-11-10-c.md": None})
        repo.git("worktree", "remove", "--force", str(root / "wt"))
        repo.git("branch", "-D", "topic")
        # a touch on ANOTHER note leaves this one free: only PB9101 is held by the pull request
        folds, held = plan(repo.path, work_dir, busy_files(repo.path, git, [("PR #1", pr_sha)], coord_dir=root / "coord"))
        check(folds == ["PB9102.md"] and list(held) == ["PB9101.md"], f"a touch on one note holds only that note ({folds}, {held})")

        # THE FOLD LEAVES THE RENDERED NOTE UNCHANGED, and removes the addenda.
        apply(work_dir, ["PB9101.md", "PB9102.md"])
        after = {it["id"]: it["_body"] for it in work.read_notes(work_dir)[0]}
        check(after == before and not (work_dir / "PB9101").exists() and not (work_dir / "PB9102").exists(),
              "folding leaves each note's text unchanged and deletes its addenda")
        check(not foldable(work_dir), "nothing is left to fold")

        # AN UNREADABLE PULL REQUEST LIST FOLDS NOTHING: fail closed.
        failures_before = len(failures)
        try:
            heads_files(repo.path, git, [("PR #9", "0" * 40)])
        except Unreadable:
            pass
        else:
            failures.append("a head with no merge base is read as an empty file set")
        check(len(failures) == failures_before, "an unreadable head is Unreadable")
        _ = origin
    for f in failures:
        print(f"FAIL: {f}")
    print(f"compact_notes self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true", help="write the folds (default: say what would fold)")
    ap.add_argument("--repo", default=str(REPO), help="the checkout to compact (default: this repository)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    import prune_worktrees  # noqa: PLC0415  (its git runner: the one the survey and the planner read with)
    return run(pathlib.Path(a.repo), a.apply, prune_worktrees.git)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
