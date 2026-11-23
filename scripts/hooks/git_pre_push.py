#!/usr/bin/env python3
"""The push-time licence-stamp scan (kb/Work PB6136): no commit a push would publish adds a per-copy licence stamp.

WHY. The bought PDFs stamp every page with the purchaser's name and the download date, and the repository is public.
`scripts/spec/verify_publishable.py` `check_tree` holds the TREE to its `tree_shapes()`, but only inside the gate, and a
branch is pushed at every checkpoint (CLAUDE.md rule 7), gated or not. On 2026-11-23 a branch pushed a commit whose
OCR text carried the stamp; the gate went RED on it afterwards and the branch was force-pushed clean, but GitHub still
serves the commit by its SHA (the first incident, kb/Work PB4814, took a history rewrite and a GitHub Support purge).
So the check runs where the publication happens, on the push, over what the push publishes: every file a commit not yet
on the remote adds or changes, read whole at that commit, those commits' messages, and the messages of the annotated
tags the push sends, each read by the gate's own matcher (`verify_publishable.text_stamp_hits`, never a copy).

TWO CALLERS, ONE SCAN (`scan`):
  - `scripts/hooks/forbidden_commands.py` rule 2c, for a `git push` an agent runs through Claude Code: it refuses the
    tool call before the command line runs, so it also refuses a push that follows a git call creating or moving
    commits on the same line (that line's commits do not exist yet when it scans).
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

from standard_streams import ensure_utf8  # noqa: E402

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


#: The ref selectors a push's own flags publish (`--all`/`--branches` the branches, `--mirror` every ref, `--tags` the
#: tags), and `--follow-tags` (the annotated tags that point into what the push publishes; `push.followTags` too).
SELECTORS = ("--all", "--branches", "--tags", "--follow-tags")


def _tag_objects(repo: pathlib.Path, sources: list[str], published: set[str]) -> list[str] | None:
    """The annotated tag objects a push of `sources` publishes: a source that names one (`git push origin v1`, or the tag
    object's sha on git's pre-push stdin), every annotated tag for `--tags` and `--all` (`--mirror`), and for
    `--follow-tags` or `push.followTags` each annotated tag whose commit is in `published`. None when git cannot be read."""
    tags: list[str] = []
    for s in sources:
        if s not in SELECTORS and (_git(repo, "cat-file", "-t", s) or "").strip() == "tag":
            tags.append((_git(repo, "rev-parse", s) or "").strip())
    follow = "--follow-tags" in sources or (_git(repo, "config", "--bool", "--get", "push.followTags") or "").strip() == "true"
    if follow or "--tags" in sources or "--all" in sources:
        refs = _git(repo, "for-each-ref", "--format=%(objecttype) %(objectname) %(*objectname)", "refs/tags")
        if refs is None:
            return None
        for kind, sha, *peeled in (ln.split() for ln in refs.splitlines() if ln.strip()):
            if kind == "tag" and ("--tags" in sources or "--all" in sources or (peeled and peeled[0] in published)):
                tags.append(sha)
    return list(dict.fromkeys(t for t in tags if t))


def _tag_message_hits(repo: pathlib.Path, tag: str, text_stamp_hits) -> list[tuple[str, str]] | None:
    """(what, tag object) for each stamp shape in the message of annotated tag `tag`, and of any tag it points at."""
    hits: list[tuple[str, str]] = []
    seen: set[str] = set()
    while tag and tag not in seen:
        seen.add(tag)
        raw = _git(repo, "cat-file", "tag", tag)
        if raw is None:
            return None
        header, _, body = raw.partition("\n\n")
        hits += [(what, tag) for what, _line in text_stamp_hits(body)]
        fields = dict(ln.split(" ", 1) for ln in header.splitlines() if " " in ln)
        tag = fields.get("object", "").strip() if fields.get("type", "").strip() == "tag" else ""
    return hits


def scan(repo: pathlib.Path, sources: list[str], remote: str | None = "origin") -> list[str] | None:
    """Every stamp shape (verify_publishable.text_stamp_hits, THE matcher the gate's check_tree uses) in a file a commit
    reachable from `sources` and not from the remote-tracking refs of `remote` (all remotes' when None) adds or changes,
    in such a commit's message, and in the message of an annotated tag the push publishes (`_tag_objects`). A changed
    file is read WHOLE at that commit, as the gate reads it, so a shape that crosses a line break, or that an added line
    completes against an unchanged one, is found. One finding per shape, file and commit, naming them and never the
    matched text. [] when none; None when git cannot be read. A merge commit is read against its first parent, so what
    a merge adds is read too."""
    if any(s.startswith("-") and s not in SELECTORS for s in sources):
        return None   # a source git would read as an option: nothing this scan can judge
    if _git(repo, "rev-parse", "--git-dir") is None:
        return None
    # a source that names no commit publishes nothing: git refuses the whole push (`src refspec … does not match any`)
    sources = [s for s in sources if s in SELECTORS or _git(repo, "rev-parse", "--verify", "-q", s + "^{commit}")]
    if not sources:
        return []
    import git_batch  # noqa: PLC0415  (scripts/: the one batched blob reader)
    import verify_publishable  # noqa: PLC0415  (THE stamp matcher; imported only when a push is judged)
    revs = [s for s in sources if s != "--follow-tags"]
    since = ["--not", f"--remotes={remote}" if remote else "--remotes"]
    raw = _git(repo, "log", "--raw", "--no-abbrev", "--no-renames", "--diff-merges=first-parent", "--format=%x00%H",
               *revs, *since, "--") if revs else ""
    messages = _git(repo, "log", "--format=%H%x00%B%x01", *revs, *since, "--") if revs else ""
    if raw is None or messages is None:
        return None
    changed: list[tuple[str, str, str]] = []   # (commit, path, blob of the path at that commit)
    commit = ""
    for line in raw.split("\n"):
        if line.startswith("\0"):
            commit = line[1:].strip()
        elif line.startswith(":"):
            meta, _, path = line[1:].partition("\t")
            parts = meta.split()
            if len(parts) >= 5 and not parts[4].startswith("D") and parts[1] != "160000" and not ZERO.match(parts[3]):
                changed.append((commit, path, parts[3]))
    found: dict[tuple[str, str, str], int] = {}
    try:
        hits = {sha: verify_publishable.stamp_hits(body) for sha, body in git_batch.blobs(repo, (b for _, _, b in changed))}
    except git_batch.GitError:
        return None
    for commit, path, blob in changed:
        for what, line_no in hits.get(blob, []):
            found.setdefault((what, f"{path}:{line_no}", f"commit {commit[:12]}"), 0)
    published: set[str] = set()
    for record in messages.split("\x01"):
        sha, _, body = record.strip("\n").partition("\0")
        if sha:
            published.add(sha)
            for what, _line in verify_publishable.text_stamp_hits(body):
                found.setdefault((what, "the commit message", f"commit {sha[:12]}"), 0)
    tags = _tag_objects(repo, sources, published)
    if tags is None:
        return None
    for tag in tags:
        tag_hits = _tag_message_hits(repo, tag, verify_publishable.text_stamp_hits)
        if tag_hits is None:
            return None
        for what, obj in tag_hits:
            found.setdefault((what, "the annotated tag message", f"tag object {obj[:12]}"), 0)
    return [f"{what}: {where} in {obj}" for what, where, obj in found]


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
    code, said = pre_push(pathlib.Path.cwd(), sys.argv[1:], sys.stdin.read())
    if said:
        sys.stderr.write(said + "\n")
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
