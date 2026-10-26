#!/usr/bin/env python3
"""rename_diagnostic_prefix.py — the diagnostic code prefix COBOLNET#### is COBOL#### (kb/Work PB3575).

    python scripts/rename_diagnostic_prefix.py              # rewrite the committed tree in place; prints the counts
    python scripts/rename_diagnostic_prefix.py --dry-run    # name the files it would change, change nothing
    python scripts/rename_diagnostic_prefix.py --check      # the gate's audit: exit 1 naming every old-prefix line
    python scripts/rename_diagnostic_prefix.py --self-test

⛔ WHY (owner decision 2026-10-23, kb/Work PB3575, "COBOL####"; restated 2026-10-26 ~10:55 PDT): every diagnostic
the compiler reports carries ONE prefix, COBOL, in ONE number space. The parser's series (COBOL0001, COBOL0100-0107,
COBOL0200-0201, COBOL0300-0313) already used it, and the rest of the compiler's codes (COBOLNET0710 and up) keep
their numbers, so no code is renumbered and the two series cannot collide (none of the renamed codes is below 0400).

⭐ RE-RUNNABLE AND IDEMPOTENT. A branch cut before this rename landed runs it again over its own tree after it merges
main (the finisher brief's CONFLICT arm says so): every rule matches only the old spelling, so a second run changes
nothing, and a run over a tree that holds both spellings converts the old ones only. It rewrites BYTES, never decoded
text, so a file's encoding, line endings and every byte the rules do not match survive unchanged; a file whose first
8,192 bytes hold a NUL is binary (git's heuristic) and is not read. It reads only TRACKED files (`git ls-files`), so the
gitignored corpora and build outputs are excluded structurally.

THE RULES (one table, RULES, in the order they apply), all anchored on the old prefix:
  - a code: the old prefix followed by a digit (or by a Markdown-bold digit) loses its NET, every number kept;
  - a placeholder for a code (nnnn, ####, <n>, an ellipsis) does the same;
  - a regular expression that matches a code (a backslash-d, a [0-9] class, a group) does the same, and the forms that
    named both prefixes collapse to the one (an optional NET group; the alternation old|new|);
  - the prose word naming the code family ("an old-prefix code", "an old-prefix diagnostic", "-class") becomes
    COBOL####, since "a COBOL code" would read as COBOL source code.
Anything else spelled with the old letters is NOT a diagnostic prefix and stays: the docs/COBOLNET_*.md design-doc file
names (an underscore follows), a COBOL program named COBOLNET in a test, the CobolNet namespaces.

WHAT IT NEVER REWRITES (EXCEPT, the one list): history and frozen records, which keep the spelling of their day
(CLAUDE.md rule 6; kb/Work PB785) — DEVLOG/, the applied verdict batches under adjudication/, the frozen evidence under
docs/rearchitecture/evidence/ — and the two files that own the rule: the decision note that quotes the owner's words,
and this script. Inside a file, a witness the traceability inventory records as RETIRED keeps the spelling it was
retired under (PROTECTED, below).

⭐ AN AUDIT, NOT A TEST (`--check`; the gate driver's DIAGNOSTIC PREFIX audit, CI's `audits` job, the battery). A tree
that still spells a code with the old prefix outside EXCEPT is RED, naming each line, so a branch that merged main
without converting itself is red before its build, never in a golden's diff. It is the SAME rules, asked as a
question: a line is an offender exactly when a rewrite would change it, so the rename and its guard cannot disagree.
It is an audit rather than a tree-sweeping Unit test for the reason scripts/conflict_markers.py is (kb/Work PB2878):
a test that reads every tracked file makes every path one a test reads, and the inert lane could then skip nothing.
A broken enumeration (no git, too few tracked paths) is RED, never a clean tree.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]

#: Paths never rewritten and never audited: a directory (ending in `/`) and everything beneath it, or one exact path.
EXCEPT = (
    "DEVLOG/",
    "adjudication/",
    "docs/rearchitecture/evidence/",
    "kb/Work/PB3575.md",
    "scripts/rename_diagnostic_prefix.py",
)

#: An enumeration of fewer tracked paths than this is a broken one (a missing observation is not a negative one).
MIN_TRACKED = 10000

#: (pattern, replacement), applied in this order to the raw bytes of every file in scope.
RULES: tuple[tuple[re.Pattern[bytes], bytes], ...] = (
    # a regex alternation and an optional group that named both prefixes collapse to the one prefix; the optional
    # group only before a digit atom, since `COBOL(?:NET)?_` matches the runtime's environment-variable names
    # (COBOL_*, COBOLNET_CLOCK), which are no diagnostic prefix
    (re.compile(rb"COBOLNET\|COBOL\|"), b"COBOL|"),
    (re.compile(rb"COBOL\(\?:NET\)\?(?=[0-9]|\\+d|\[0-9\])"), b"COBOL"),
    # a code, a Markdown-bold code, a placeholder, or a regex that matches a code
    (re.compile(rb"COBOLNET(?=[0-9]|\*\*[0-9]|nnnn|####|<n>|\xe2\x80\xa6|\\+d|\[0-9\]|\()"), b"COBOL"),
    # the prose word for a code of the family
    (re.compile(rb"COBOLNET(?=(?: |-)(?:code|codes|Code|diagnostic|diagnostics|class)\b)"), b"COBOL####"),
)

#: A file whose first BINARY_PROBE bytes hold a NUL is binary (git's heuristic) and is not rewritten.
BINARY_PROBE = 8192

#: The old code spelling, for counting the distinct codes a run converted.
OLD_CODE = re.compile(rb"COBOLNET([0-9]{4})")

#: ⛔ A witness the traceability inventory records as RETIRED keeps the spelling it was retired under: the mark
#: (`retired-witness: <ref>`, its text read from the inventory schema, the one place it is defined) is what lets
#: `audit_witness_loss.py` excuse the loss of `<ref>`, so re-spelling the ref would turn a recorded retirement back into
#: an unexcused loss. The rename re-spelled 14 witnesses (test names and one anchor that carried a code); they went
#: through `record_verdicts.py` as a batch that retired the old spelling, never as a hand edit of the inventory.
RETIRED_MARK = json.loads((REPO / "tests" / "version-matrix" / "inventory-schema.json").read_text(encoding="utf-8"))[
    "notes"]["retired-witness-mark"].encode("utf-8")
PROTECTED = re.compile(re.escape(RETIRED_MARK) + rb"\s*\S+")


def excluded(path: str) -> bool:
    """Whether the repo-relative `path` is one EXCEPT sets aside."""
    return any(path.startswith(e) if e.endswith("/") else path == e for e in EXCEPT)


def _apply(data: bytes) -> tuple[bytes, int]:
    total = 0
    for pattern, replacement in RULES:
        data, n = pattern.subn(replacement, data)
        total += n
    return data, total


def rewrite(data: bytes) -> tuple[bytes, int]:
    """`data` with every rule applied outside the PROTECTED spans, and the number of substitutions made."""
    parts: list[bytes] = []
    total = pos = 0
    for m in PROTECTED.finditer(data):
        seg, n = _apply(data[pos:m.start()])
        parts += [seg, m.group()]
        total += n
        pos = m.end()
    seg, n = _apply(data[pos:])
    return b"".join(parts + [seg]), total + n


@dataclass
class Result:
    changed: list[str] = field(default_factory=list)
    substitutions: int = 0
    codes: set[bytes] = field(default_factory=set)
    binary: int = 0


def tracked(root: Path) -> list[str]:
    """The tracked paths of the repository at `root`; RAISES when git cannot list them (an empty list would read as a
    tree with nothing to rename)."""
    out = subprocess.run(["git", "ls-files", "-z"], cwd=root, capture_output=True, check=True).stdout
    paths = [p for p in out.decode("utf-8").split("\0") if p]
    if not paths:
        raise SystemExit(f"⛔ `git ls-files` listed no tracked file in {root}")
    return paths


def run(root: Path, paths: list[str], *, write: bool) -> Result:
    r = Result()
    for rel in paths:
        if excluded(rel):
            continue
        f = root / rel
        if not f.is_file():          # a gitlink's checkout (a submodule) or a path absent from the checkout
            continue
        data = f.read_bytes()
        if b"COBOLNET" not in data:
            continue
        if b"\0" in data[:BINARY_PROBE]:
            r.binary += 1
            continue
        new, n = rewrite(data)
        if n:
            r.changed.append(rel)
            r.substitutions += n
            r.codes.update(OLD_CODE.findall(data))
            if write:
                f.write_bytes(new)
    return r


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    old = "COBOL" + "NET"   # built, so the cases below read as the shapes they test
    cases = [
        (f"error {old}1985: x", "error COBOL1985: x", "a code keeps its number"),
        (f"{old}**1520** (GLOBAL", "COBOL**1520** (GLOBAL", "a Markdown-bold code"),
        (f"bare <c>{old}nnnn</c>, {old}####, {old}<n>, {old}…",
         "bare <c>COBOLnnnn</c>, COBOL####, COBOL<n>, COBOL…", "the placeholders"),
        (rf'@"{old}\d{{4}}" "{old}\\d{{4}}" {old}[0-9]{{4}} "{old}(159[7-9])"',
         r'@"COBOL\d{4}" "COBOL\\d{4}" COBOL[0-9]{4} "COBOL(159[7-9])"', "the regular expressions"),
        (rf"(?:{old}|COBOL|CBL)\d{{4}} and COBOL(?:NET)?\d{{4}}", r"(?:COBOL|CBL)\d{4} and COBOL\d{4}",
         "the both-prefix forms collapse to one"),
        (f"a {old} diagnostic, no {old} code, a {old}-class rejection, a {old} Code",
         "a COBOL#### diagnostic, no COBOL#### code, a COBOL####-class rejection, a COBOL#### Code",
         "the prose word for the family"),
        (f"docs/{old}_DESIGN.md, CALL \"{old}\" USING, HELLO FROM {old}.",
         f"docs/{old}_DESIGN.md, CALL \"{old}\" USING, HELLO FROM {old}.", "a name that is no diagnostic prefix stays"),
        (r'new("\"(COBOL(?:NET)?_[A-Z0-9_]*)\"") and ' + old + "_CLOCK",
         r'new("\"(COBOL(?:NET)?_[A-Z0-9_]*)\"") and ' + old + "_CLOCK",
         "an environment-variable name and the regex that matches the names stay"),
    ]
    for before, after, label in cases:
        got, _ = rewrite(before.encode("utf-8"))
        expect(label, got.decode("utf-8"), after)
        again, n = rewrite(got)
        expect(label + " (idempotent: a second run changes nothing)", (again, n), (got, 0))
    crlf = f"line one {old}0899\r\nline two\r\n".encode()
    expect("bytes outside a match survive (CRLF kept)", rewrite(crlf)[0], b"line one COBOL0899\r\nline two\r\n")
    latin = b"caf\xe9 " + old.encode() + b"2014"
    expect("a byte that is not UTF-8 survives", rewrite(latin)[0], b"caf\xe9 COBOL2014")
    mark = RETIRED_MARK.decode()
    notes = f"{old}1934 is the code. {mark} unit:T.X_Is{old}1934 (renamed) and {mark}  unit:T.Y_Is{old}1935."
    expect("a retired witness keeps its spelling; the prose around it does not",
           rewrite(notes.encode())[0].decode(),
           f"COBOL1934 is the code. {mark} unit:T.X_Is{old}1934 (renamed) and {mark}  unit:T.Y_Is{old}1935.")
    expect("history is never rewritten", [excluded(p) for p in
           ("DEVLOG/2600-x.md", "adjudication/a/b.json", "docs/rearchitecture/evidence/x.json", "kb/Work/PB3575.md")],
           [True] * 4)
    expect("a current doc, a note and the source are rewritten", [excluded(p) for p in
           ("docs/CONFORMANCE.md", "kb/Work/PB3576.md", "src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs",
            "docs/rearchitecture/evidence-index.md")], [False] * 4)

    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415

    root = scratch_dir("rename-prefix-")
    (root / "DEVLOG").mkdir()
    (root / "DEVLOG" / "0001-x.md").write_bytes(f"history {old}1234\n".encode())
    (root / "a.cs").write_bytes(f'Error("{old}1234")\n'.encode())
    (root / "b.bin").write_bytes(b"\0" + old.encode() + b"1234")
    (root / "c.md").write_bytes(b"nothing to do\n")
    repo = ScratchRepo.init(root)
    repo.git("add", "-A")
    r = run(root, tracked(root), write=True)
    expect("a run rewrites the source, skips history and binary, counts the codes",
           (r.changed, r.substitutions, sorted(r.codes), r.binary), (["a.cs"], 1, [b"1234"], 1))
    expect("the source now reads the new prefix", (root / "a.cs").read_bytes(), b'Error("COBOL1234")\n')
    expect("the history keeps its spelling", (root / "DEVLOG" / "0001-x.md").read_bytes(),
           f"history {old}1234\n".encode())
    expect("a second run over the tree changes nothing", run(root, tracked(root), write=True).changed, [])
    (root / "late.md").write_bytes(f"clean line\nerror {old}0899: x\n".encode())
    repo.git("add", "-A")
    expect("the audit is RED on an old-prefix line and names it", (check(root, min_tracked=1),
           offenders(root, ["late.md"])), (1, [f"late.md:2: error {old}0899: x"]))
    run(root, tracked(root), write=True)
    expect("the audit is GREEN once the tree is converted", check(root, min_tracked=1), 0)
    expect("an enumeration of too few tracked paths is RED", check(root), 1)
    bare = scratch_dir("rename-prefix-bare-")
    expect("a directory git cannot enumerate is RED", check(bare, min_tracked=1), 1)
    print(f"=== RENAME DIAGNOSTIC PREFIX SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def offenders(root: Path, changed: list[str]) -> list[str]:
    """Every `path:line: text` of the `changed` files that a rewrite would change (the rules work within a line)."""
    found = []
    for rel in changed:
        for n, line in enumerate((root / rel).read_bytes().split(b"\n"), 1):
            if rewrite(line)[1]:
                found.append(f"{rel}:{n}: {line.decode('utf-8', errors='replace').strip()}")
    return found


def check(root: Path, min_tracked: int = MIN_TRACKED) -> int:
    """The audit: 0 when no tracked file outside EXCEPT spells a code with the old prefix, 1 naming every line when one
    does, and 1 when the tree cannot be enumerated or enumerates to fewer than `min_tracked` paths."""
    try:
        paths = tracked(root)
    except (subprocess.CalledProcessError, OSError, SystemExit) as e:
        print(f"=== DIAGNOSTIC PREFIX: RED — the tracked tree of {root} could not be listed ({e}) ===")
        return 1
    if len(paths) < min_tracked:
        print(f"=== DIAGNOSTIC PREFIX: RED — only {len(paths)} tracked path(s) under {root} (at least {min_tracked} "
              "expected): the enumeration is broken, so the audit would read nothing ===")
        return 1
    r = run(root, paths, write=False)
    found = offenders(root, r.changed)
    if found:
        for o in found[:SHOWN]:
            print(f"    {o}")
        if len(found) > SHOWN:
            print(f"    ... and {len(found) - SHOWN} more")
        print(f"=== DIAGNOSTIC PREFIX: RED — {len(found)} line(s) in {len(r.changed)} file(s) spell a diagnostic code "
              "with the former prefix; the codes are COBOL#### with every number kept (kb/Work PB3575). Convert the "
              "tree with `python scripts/rename_diagnostic_prefix.py` ===")
        return 1
    print(f"=== DIAGNOSTIC PREFIX: GREEN — {len(paths)} tracked path(s), none spells a code with the former prefix ===")
    return 0


#: How many offending lines `--check` prints before it summarizes.
SHOWN = 40


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    group = ap.add_mutually_exclusive_group()
    group.add_argument("--dry-run", action="store_true", help="name the files a run would change; change nothing")
    group.add_argument("--check", action="store_true", help="the audit: exit 1 naming every old-prefix line")
    group.add_argument("--self-test", action="store_true")
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.check:
        return check(args.root)
    r = run(args.root, tracked(args.root), write=not args.dry_run)
    for rel in r.changed if args.dry_run else []:
        print(f"    {rel}")
    verb = "would change" if args.dry_run else "changed"
    print(f"=== RENAME DIAGNOSTIC PREFIX: {verb} {len(r.changed)} file(s), {r.substitutions} substitution(s), "
          f"{len(r.codes)} distinct code(s) renamed; {r.binary} binary file(s) set aside ===")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
