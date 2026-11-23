#!/usr/bin/env python3
"""The push-time licence-stamp scan (kb/Work PB6136): no commit a push would publish adds a per-copy licence stamp.

WHY. The bought PDFs stamp every page with the purchaser's name and the download date, and the repository is public.
`scripts/spec/verify_publishable.py` `check_tree` holds the TREE to its `tree_shapes()`, but only inside the gate, and a
branch is pushed at every checkpoint (CLAUDE.md rule 7), gated or not. On 2026-11-23 a branch pushed a commit whose
OCR text carried the stamp; the gate went RED on it afterwards and the branch was force-pushed clean, but GitHub still
serves the commit by its SHA (the first incident, kb/Work PB4814, took a history rewrite and a GitHub Support purge).
So the check runs where the publication happens, on the push, over what the push publishes: the ADDED lines of every
commit not yet on the remote, and those commits' messages, searched for the same shapes (imported, never copied).

TWO CALLERS, ONE SCAN (`scan`):
  - `scripts/hooks/forbidden_commands.py` rule 2c, for a `git push` an agent runs through Claude Code: it refuses the
    tool call before git runs.
  - git's own pre-push hook, for every other push from this repository: `.githooks/pre-push` runs this file with git's
    arguments (`<remote name> <remote url>`) and its stdin (`<local ref> <local sha> <remote ref> <remote sha>` lines),
    and exit 1 stops the push. It runs only where `git config --local core.hooksPath .githooks` is set; the
    session-start TOOLING check (`scripts/hooks/tooling_check.py`) sets it when it is not.

A finding names the shape, the file and line, and the commit; it never prints the matched text, which would put the
name into a transcript or a terminal log. A git that cannot be read is a refusal (fails closed).

    python scripts/hooks/git_pre_push.py <remote> <url> < refs       # as git runs it

Its cases are scripts/selftests/hooks/git_pre_push/test_pre_push.py (planted pushes over scratch repositories, and the
tracked wrapper stopping a real `git push`).
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams

ZERO = re.compile(r"^0+$")
#: How long one git read may take before the scan refuses: a guard against a hung git, never a time expectation.
GIT_LIMIT_S = 300


def _git(repo: pathlib.Path, *args: str) -> str | None:
    """git's stdout for `git -C repo args` (paths unquoted, as UTF-8); None when git fails or cannot be started."""
    try:
        r = subprocess.run(["git", "-C", str(repo), "-c", "core.quotepath=off", *args], capture_output=True,
                           timeout=GIT_LIMIT_S)
    except (OSError, subprocess.SubprocessError):
        return None
    return r.stdout.decode("utf-8", "replace") if r.returncode == 0 else None


#: The ref selectors a push's own flags publish (`--all`/`--branches` the branches, `--mirror` every ref, `--tags`).
SELECTORS = ("--all", "--branches", "--tags")


def scan(repo: pathlib.Path, sources: list[str], remote: str | None = "origin") -> list[str] | None:
    """Every stamp shape (verify_publishable.tree_shapes) on an ADDED line of a commit reachable from `sources` and not
    from the remote-tracking refs of `remote` (all remotes' when None), or in such a commit's message: one finding per
    shape, file and commit, naming them and never the matched text. [] when none; None when git cannot be read. A merge
    commit is read against its first parent, so what a merge adds is read too."""
    if any(s.startswith("-") and s not in SELECTORS for s in sources):
        return None   # a source git would read as an option: nothing this scan can judge
    if _git(repo, "rev-parse", "--git-dir") is None:
        return None
    # a source that names no commit publishes nothing: git refuses the whole push (`src refspec … does not match any`)
    sources = [s for s in sources if s in SELECTORS or _git(repo, "rev-parse", "--verify", "-q", s + "^{commit}")]
    if not sources:
        return []
    import verify_publishable  # noqa: PLC0415  (THE stamp shapes; imported only when a push is judged)
    shapes = [(re.compile(p), what) for p, what in verify_publishable.tree_shapes()]
    since = ["--not", f"--remotes={remote}" if remote else "--remotes"]
    diff = _git(repo, "log", "-p", "-U0", "--no-color", "--no-ext-diff", "--no-textconv", "--diff-merges=first-parent",
                "--format=%x00%H", *sources, *since, "--")
    messages = _git(repo, "log", "--format=%H%x00%B%x01", *sources, *since, "--")
    if diff is None or messages is None:
        return None
    found: dict[tuple[str, str, str], int] = {}
    commit = path = ""
    header, line_no = False, 0
    for line in diff.split("\n"):
        if line.startswith("\0"):
            commit, header = line[1:].strip(), False
        elif line.startswith("diff --git ") or line.startswith("diff --cc "):
            header, path = True, ""
        elif header and line.startswith("+++ "):
            path = line[4:].removeprefix("b/")
        elif line.startswith("@@"):
            header = False
            m = re.search(r"\+(\d+)", line)
            line_no = int(m.group(1)) if m else 0
        elif not header and line.startswith("+"):
            for rx, what in shapes:
                if rx.search(line[1:]):
                    found.setdefault((what, f"{path}:{line_no}", commit[:12]), 0)
            line_no += 1
    for record in messages.split("\x01"):
        sha, _, body = record.strip("\n").partition("\0")
        for rx, what in shapes:
            if sha and rx.search(body):
                found.setdefault((what, "the commit message", sha[:12]), 0)
    return [f"{what}: {where} in commit {sha}" for what, where, sha in found]


REMEDY = ("remove it from the file (and the message), then rebuild the commits that carry it (`git commit --amend`, or "
          "a new branch from origin/main with the clean change) so no unpushed commit holds it, and push again. Never "
          "push it to read it back: GitHub serves a pushed commit by its SHA after any force-push (kb/Work PB6136, "
          "PB4814).")


def pre_push(repo: pathlib.Path, argv: list[str], stdin: str) -> tuple[int, str]:
    """git's pre-push hook: (exit code, what to print). `argv` is git's (`<remote name> <url>`), `stdin` its ref
    lines. Every local sha but a deletion's is scanned against the remote's tracking refs (every remote's when the push
    names a URL, not a remote)."""
    remote = argv[0] if argv else ""
    names = _git(repo, "remote")
    if names is None:
        return 1, "pre-push (kb/Work PB6136): git cannot list this repository's remotes, so the push is refused unscanned."
    shas = [w[1] for w in (ln.split() for ln in stdin.splitlines()) if len(w) == 4 and not ZERO.match(w[1])]
    findings = scan(repo, shas, remote if remote in names.split() else None)
    if findings is None:
        return 1, "pre-push (kb/Work PB6136): git cannot read the commits this push publishes, so it is refused unscanned."
    if findings:
        return 1, ("pre-push (kb/Work PB6136): this push would publish a per-copy licence stamp to a PUBLIC "
                   "repository:\n" + "\n".join(f"  {f}" for f in findings) + "\n" + REMEDY)
    return 0, ""


def main() -> int:
    from standard_streams import ensure_utf8  # noqa: PLC0415
    ensure_utf8()
    code, said = pre_push(pathlib.Path.cwd(), sys.argv[1:], sys.stdin.read())
    if said:
        sys.stderr.write(said + "\n")
    return code


if __name__ == "__main__":
    sys.exit(main())
