#!/usr/bin/env python3
"""external_corpus.py — THE ONE RULE for obtaining the git-ignored GnuCOBOL corpus in a tree (kb/Work PB2611).

    python scripts/external_corpus.py ensure [--repo <tree>]   # fetch when absent; exit 1 on a failed fetch
    python scripts/external_corpus.py --self-test    # the rule's arms, the fetch's own -SelfTest, the real scripts

⚖ The corpus is GPL-3.0, never committed, and lives per WORKTREE (and per clone) in `tests/external/gnucobol/`
(kb/Work PB209, PB277). `scripts/fetch-gnucobol-tests.ps1` is the fetch itself (pinned 3.2, sha256-verified,
extraction staged so a failed fetch never empties the tree, PB897); THIS module is the rule every caller applies
around it:

  1. the corpus is PRESENT when its marker `tests/external/gnucobol/tests/testsuite.src` exists — then nothing runs;
  2. otherwise the fetch script runs in that tree;
  3. a fetch that exits non-zero, or that exits 0 and still leaves no marker, is ONE named red,
     `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`: the population scripts/corpus_sweep.py's self-test and the
     GnuCOBOL differential measure is then unmeasured, and every red they show is attributable to it.

⛔ WHY ONE MODULE. Battery #89 (2026-10-08) read NOT GREEN at a green tree because `scripts/battery.sh` had no fetch at
all while the gate driver had one; the impact-map recorder, the Linux gate and the cloud session hook each carried a
fourth, fifth and sixth spelling of the rule. Every caller now imports `ensure` (python) or runs `ensure` (shell), and
this module's `--self-test` fails when any other script invokes the fetch script directly. CI's two fetch steps are the
one recorded exception: a fresh checkout never holds the corpus, and the step's own exit status is the red.

Exit codes of `ensure`: 0 present or fetched · 1 FETCH FAILED.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]

#: The corpus is present exactly when this exists (what the differential reads and the population drift test lists).
MARKER = Path("tests/external/gnucobol/tests/testsuite.src")
#: The fetch itself, run with cwd = the tree it fills (the script resolves its destination from its own location).
FETCH_SCRIPT = Path("scripts/fetch-gnucobol-tests.ps1")
#: THE red, named the same in every gate that applies the rule.
FETCH_FAILED = "EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED"

#: Runs the fetch command in the tree and returns its exit code. The default spawns it attached to this console; a
#: caller that logs or prioritises its children passes its own.
Runner = Callable[[list[str], Path], int]


def fetch_command(tree: Path) -> list[str]:
    return ["pwsh", "-NoProfile", "-File", str(tree / FETCH_SCRIPT)]


def _attached(cmd: list[str], cwd: Path) -> int:
    return subprocess.run(cmd, cwd=cwd).returncode


def present(tree: Path) -> bool:
    return (tree / MARKER).exists()


def ensure(tree: Path, say: Callable[[str], None] = print, run: Runner = _attached) -> list[str]:
    """Apply the rule to `tree`: fetch the corpus when it is absent. Returns the red reasons ([] or [FETCH_FAILED])."""
    if present(tree):
        return []
    say("=== EXTERNAL CORPUS: absent in this tree — fetching (GPL, git-ignored, never committed) ===")
    try:
        rc: int | str = run(fetch_command(tree), tree)
    except OSError as exc:  # pwsh itself missing: the fetch did not run, which is a failed fetch, never a crash
        rc = f"could not start: {exc}"
    if rc != 0 or not present(tree):
        cause = f"exit {rc}" if rc != 0 else f"exit 0 but no {MARKER.as_posix()}"
        say(f"=== EXTERNAL CORPUS: FETCH FAILED ({cause}; the FETCH FAILED line above names the cause) — the "
            "population reds of corpus_sweep.py --self-test and the GnuCOBOL differential's `corpus absent` are ATTRIBUTABLE "
            "TO IT, and the run is RED because it could not measure that population ===")
        return [FETCH_FAILED]
    say("=== EXTERNAL CORPUS: fetched ===")
    return []


# ── the fetch's own gate (kb/Work PB897) ─────────────────────────────────────────────────────────────────────────
# The fetch carried three faults that together made the population gate RED in every fresh worktree, permanently: it
# DELETED the corpus before an extraction it then failed to perform; it passed the archive as a drive-letter path, which GNU
# tar reads as `host:path` and Windows' bsdtar accepts; and Git-for-Windows' GNU tar cannot read a `.tar.xz` at all. Which
# tar was first on PATH decided whether the corpus existed, per machine, so the population reds became "the known worktree
# shape" every brief told agents to ignore: a red everyone is told to ignore is a gate that has stopped gating. Its
# `-SelfTest` drives `Invoke-CorpusExtraction`, the REAL function the real fetch calls, against a synthetic `.tar.xz` in a
# temporary sandbox (no network, no repository state), the FAILURE branches included; the self-test below runs it and
# reads the individual check lines, since a self-test quietly reduced to its happy path would still print PASS. A missing
# PowerShell is a LOUD failure, never a skip: pwsh is how CI runs the fetch, so a host without it cannot fetch either.

#: The fetch self-test's named checks, each one of PB897's faults driven.
FETCH_CHECKS = (
    "a .tar.xz fixture can be produced",                       # fault 3: some tar here speaks xz
    "a good archive extracts and swaps in",                    # fault 2, and fault 1 on a GNU-tar host
    "the PREVIOUS corpus survived the failed extraction",      # fault 2: the whole of it
    "the corpus survived the missing-member failure",
    "no staging directory is left behind",
)
#: The tar invocations of the fetch script, by the variables that hold the tar it chose and the one it made the fixture
#: with. Keyed on the INVOCATIONS, not the file: the prose that explains why `--force-local` is wrong names it.
TAR_CALL = ("$tarExe ", "$mkTar ")
#: The files that may name the fetch script outside a comment: the rule and the fetch itself.
FETCH_OWNERS = ("scripts/external_corpus.py", FETCH_SCRIPT.as_posix())
SCRIPT_KINDS = (".py", ".sh", ".ps1", ".psm1")


def fetch_self_test(repo: Path = REPO) -> tuple[str, int | str]:
    """The fetch script's `-SelfTest`: (its output, its exit code, or why it could not start)."""
    try:
        r = subprocess.run([*fetch_command(repo), "-SelfTest"], cwd=repo, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
    except OSError as exc:
        return "", f"could not start pwsh: {exc} — the fetch is a PowerShell instrument, so this host cannot fetch either"
    return r.stdout + r.stderr, r.returncode


def tracked_scripts(repo: Path = REPO) -> list[str]:
    """The tracked script files under scripts/ (`git ls-files`: an untracked scratch file is never judged)."""
    out = subprocess.run(["git", "-c", "core.quotePath=false", "ls-files", "-z", "--", "scripts/"], cwd=repo,
                         capture_output=True, text=True, encoding="utf-8", check=True).stdout
    return sorted(p for p in out.split("\0") if p.endswith(SCRIPT_KINDS))


def fetch_invocations(repo: Path = REPO) -> tuple[set[str], list[str]]:
    """(the owners that name the fetch script outside a comment, every other script line that does): ONE fetch rule in
    ONE place (kb/Work PB2611), so every gate, the battery, the Linux gate and the cloud hook obtain the corpus the same
    way. Battery #89 read NOT GREEN at a green tree because battery.sh had no fetch while the gate driver had one."""
    name = FETCH_SCRIPT.name
    seen: set[str] = set()
    strays: list[str] = []
    for rel in tracked_scripts(repo):
        for line in (repo / rel).read_text(encoding="utf-8", errors="replace").splitlines():
            if name not in line or line.lstrip().startswith("#"):
                continue
            if rel in FETCH_OWNERS:
                seen.add(rel)
            else:
                strays.append(f"{rel}: {line.strip()}")
    return seen, strays


def tar_invocations(repo: Path = REPO) -> list[str]:
    """The fetch script's tar invocation lines."""
    text = (repo / FETCH_SCRIPT).read_text(encoding="utf-8")
    return [x.strip() for x in text.splitlines() if any(c in x for c in TAR_CALL)]


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────


def self_test() -> int:
    with tempfile.TemporaryDirectory(prefix="external-corpus-selftest-") as tmp:
        return _self_test(Path(tmp))


def _self_test(root: Path) -> int:
    results: list[bool] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append(ok)
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def planted(name: str, *, has: bool, rc: int | None = None, creates: bool = False, raises: bool = False):
        tree = root / name
        tree.mkdir()
        if has:
            (tree / MARKER).mkdir(parents=True)
        calls: list[tuple[list[str], Path]] = []
        said: list[str] = []

        def run(cmd: list[str], cwd: Path) -> int:
            calls.append((cmd, cwd))
            if raises:
                raise FileNotFoundError("planted: pwsh is not on PATH")
            if creates:
                (cwd / MARKER).mkdir(parents=True)
            return rc if rc is not None else 0

        return ensure(tree, said.append, run), calls, said, tree

    reds, calls, said, _ = planted("present", has=True)
    arm("a present corpus runs NO fetch and is not red", reds == [] and calls == [] and said == [], f"{reds} {calls}")

    reds, calls, said, tree = planted("fetched", has=False, rc=0, creates=True)
    arm("an absent corpus runs the fetch script once, in that tree, and is not red when it lands",
        reds == [] and len(calls) == 1 and calls[0][1] == tree and calls[0][0][-1] == str(tree / FETCH_SCRIPT),
        f"{reds} {calls}")

    reds, calls, said, _ = planted("exit1", has=False, rc=1)
    arm("a fetch that exits non-zero is the named FETCH FAILED red",
        reds == [FETCH_FAILED] and any("FETCH FAILED (exit 1" in s for s in said), f"{reds} {said}")

    reds, calls, said, _ = planted("exit0-empty", has=False, rc=0, creates=False)
    arm("a fetch that exits 0 but leaves no marker is the named FETCH FAILED red, never a green",
        reds == [FETCH_FAILED] and any("exit 0 but no" in s for s in said), f"{reds} {said}")

    reds, calls, said, _ = planted("no-pwsh", has=False, raises=True)
    arm("a fetch that cannot start (no pwsh) is the named FETCH FAILED red, never a crash",
        reds == [FETCH_FAILED] and any("could not start" in s for s in said), f"{reds} {said}")

    # THE FETCH ITSELF (kb/Work PB897, PB4395: until then ExternalCorpusFetchTests, a compiler test, held these three).
    out, rc = fetch_self_test()
    missing = [c for c in FETCH_CHECKS if c not in out]
    arm("the fetch's own -SelfTest passes and still drives each of PB897's faults, the failure branches included: the "
        "extraction never destroys a corpus it cannot replace",
        rc == 0 and not missing and "=== FETCH SELF-TEST: PASS" in out, f"exit {rc}; checks no longer driven {missing}\n{out}")
    seen, strays = fetch_invocations()
    arm("only the rule invokes the fetch script: no other script under scripts/ names it outside a comment (kb/Work "
        "PB2611), and the scan sees the rule's own reference",
        "scripts/external_corpus.py" in seen and not strays, f"seen {sorted(seen)}; strays {strays}")
    tar = tar_invocations()
    arm("the fetch never passes --force-local to tar (bsdtar refuses it, so it moves PB897's failure to the other "
        "machine), and the scan sees at least two tar invocations", len(tar) >= 2 and not any("--force-local" in x for x in tar),
        tar)

    ok = all(results)
    print(f"external_corpus SELF-TEST: {'PASS' if ok else 'FAIL'} ({sum(results)}/{len(results)} arms)")
    return 0 if ok else 1


def main(argv: list[str] | None = None) -> int:
    ap =argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    e = sub.add_parser("ensure", help="fetch the corpus into the tree when it is absent; exit 1 on a failed fetch")
    e.add_argument("--repo", type=Path, default=REPO, help="the tree to fill (default: this script's repository)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd != "ensure":
        ap.error("name a command: ensure, or --self-test")
    tree = a.repo.resolve()
    was_present = present(tree)
    reds = ensure(tree, lambda s: print(s, flush=True))
    if reds:
        print(reds[0])
        return 1
    how = "present" if was_present else "fetched, now present"
    print(f"=== EXTERNAL CORPUS: {how} at {(tree / MARKER).as_posix()} ===")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
