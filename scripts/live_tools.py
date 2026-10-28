#!/usr/bin/env python3
"""live_tools.py — THE ONE LIST OF THE TREES WHOSE TRACKED SCRIPTS ARE LIVE TOOLS (kb/Work PB3176, branch review R2-1).

A live tool is a script the gates, the loop or a session run against this tree. These readers need the same answer:
  scripts/self_tests.py    discovers every self-test under ROOTS and runs it in every gate and in CI;
  scripts/line_endings.py  its WRITERS arm holds every text-mode write under ROOTS to an explicit `newline=`;
  scripts/temp_names.py    holds every script under ROOTS to no fixed name in the machine-wide temp directory;
  scripts/scratch_repo.py  its audit holds every self-test under ROOTS to the one scratch-repository fixture (PB3584);
  scripts/standard_streams.py  holds every Python file under ROOTS to the one UTF-8 switch for its streams (PB3637);
  scripts/gate_plan.py     its trx-writer and trx-reader audits read every live tool for the one trx reader (PB3661).
Each kept its own copy until PB3176: when the conformance suite's harness (conformance-suite/harness/, whose generator is
a gate audit; kb/Work PB3174) joined the self-test runner and the WRITERS arm, the temp-name audit's copy stayed at
scripts/ and .claude/, so a fixed temp name committed in the harness passed every gate. A tree added here reaches all
of them at once. And each kept its own `git ls-files` LISTING of the trees (seven spellings, each with its own suffix
filter and its own missing-file rule) until PB3821: `tracked` is the one listing, and SelfTestDiscoveryDriftTests holds
every reader to it.

Not live: `docs/**/evidence/**` and `adjudication/**` hold frozen records that never run against the tree, and
`tools/claude-skills` is a submodule with its own repository and its own checks.
"""
from __future__ import annotations

import subprocess
import sys
from collections.abc import Mapping
from pathlib import Path

from standard_streams import ensure_utf8

#: Each root ends in `/`, so it is both a git pathspec and a `str.startswith` prefix of a repository path.
ROOTS = ("scripts/", ".claude/", "conformance-suite/harness/")


def tracked(repo: Path | str, suffixes: tuple[str, ...] | None = None, env: Mapping[str, str] | None = None) -> list[str]:
    """THE ONE LISTING OF THE LIVE TOOLS (kb/Work PB3821): the sorted repository paths of the files `git ls-files` tracks
    under ROOTS in `repo` that still exist in its working tree (a file deleted there and not yet from the index is no
    tool to read) and, when `suffixes` is given, end with one of them. Every reader of the live trees calls this, and
    none other spells the `git ls-files` listing (SelfTestDiscoveryDriftTests). `env` is the child's environment, `None`
    for this process's; a reader that may run under a git hook passes `scratch_repo.git_env()`, which drops git's
    repository-selection variables (this module cannot import that one, which imports this)."""
    out = subprocess.run(["git", "-C", str(repo), "ls-files", "-z", "--", *ROOTS], capture_output=True, check=True,
                         env=env).stdout.decode("utf-8")
    root = Path(repo)
    return sorted(p for p in out.split(chr(0)) if p and (suffixes is None or p.endswith(suffixes)) and (root / p).is_file())


# ── the self-test: `tracked`'s listing rules, on a scratch repository ─────────────────────────────────────────────────

def self_test() -> int:
    from scratch_repo import ScratchRepo, git_env, scratch_dir
    failures: list[str] = []

    def expect(name: str, got, want) -> None:
        ok = got == want
        print(f"{'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(name)

    base = scratch_dir("live-tools-")
    repo = ScratchRepo.init(base / "repo")
    repo.commit("tools", {
        "scripts/b.py": "", "scripts/a.sh": "", "scripts/sub/c.ps1": "", "scripts/notes.md": "",
        ".claude/hooks/h.py": "", "conformance-suite/harness/g.py": "", "conformance-suite/cases/x.py": "",
        "docs/evidence/e.py": "", "adjudication/a.py": "", "scripts/gone.py": "",
    })
    (repo.path / "scripts" / "gone.py").unlink()  # tracked, deleted from the working tree: no tool to read
    expect("every tracked file under ROOTS, sorted; none outside them, none deleted from the working tree",
           tracked(repo.path),
           [".claude/hooks/h.py", "conformance-suite/harness/g.py", "scripts/a.sh", "scripts/b.py", "scripts/notes.md",
            "scripts/sub/c.ps1"])
    expect("a suffix filter keeps the files that end in one of the suffixes",
           tracked(repo.path, (".py", ".sh")),
           [".claude/hooks/h.py", "conformance-suite/harness/g.py", "scripts/a.sh", "scripts/b.py"])
    expect("a repository path as text lists the same files", tracked(str(repo.path), (".ps1",)), ["scripts/sub/c.ps1"])
    other = ScratchRepo.init(base / "other")
    other.commit("elsewhere", {"scripts/other.py": ""})
    repo.write({"scripts/other.py": ""})  # untracked here, so only the other repository's listing names it
    hooked = {**git_env(), "GIT_DIR": str(other.path / ".git")}  # a caller running under a hook for ANOTHER repository
    expect("the environment is the child's: a leaked GIT_DIR selects another repository, git_env() does not",
           (tracked(repo.path, (".py",), hooked), tracked(repo.path, (".py",), git_env())),
           (["scripts/other.py"], [".claude/hooks/h.py", "conformance-suite/harness/g.py", "scripts/b.py"]))
    expect("this repository's own tools are listed, this file among them",
           "scripts/live_tools.py" in tracked(Path(__file__).resolve().parents[1], (".py",)), True)
    print(f"=== LIVE TOOLS SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    import argparse
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[1])
    ap.add_argument("--self-test", action="store_true", required=True,
                    help="`tracked`'s listing rules on a scratch repository, and on this one")
    ap.parse_args()
    return self_test()


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
