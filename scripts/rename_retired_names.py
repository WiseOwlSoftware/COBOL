#!/usr/bin/env python3
"""rename_retired_names.py — the code takes the product's name: WiseOwl.Cobol (kb/Work PB3837).

    python scripts/rename_retired_names.py              # rename the committed tree in place: paths, then contents
    python scripts/rename_retired_names.py --dry-run    # name the paths it would move and the files it would change
    python scripts/rename_retired_names.py --check      # the audit: exit 1 naming every retired name, 2 NOT RUN
    python scripts/rename_retired_names.py --self-test

⛔ WHY (owner decision 2026-10-26, kb/Work PB3837): the product is WiseOwl COBOL (NuGet id WiseOwl.COBOL, the
executable `cobol`), and the code takes its name. The names of the two earlier products — the one this compiler was
written as and the legacy byte engine it replaced — are RETIRED outside history: the projects, assemblies, solution and
root namespace are WiseOwl.Cobol (`src/WiseOwl.Cobol.*`, `tests/WiseOwl.Cobol.Tests.*`, `WiseOwl.Cobol.sln`, namespace
`WiseOwl.Cobol.*`), an identifier that carried the old name carries `WiseOwlCobol`, the implementor's environment
variables are WISEOWL_COBOL_* (never COBOL_*: `SwitchStore.Prefix` is COBOL_, the §12.3.7 external-switch family, so
an implementor variable there would be a switch a program can name), the design documents drop the old prefix
(`docs/DESIGN.md`, `docs/REARCHITECTURE_PLAN.md`, …), and prose names the product WiseOwl COBOL.

⭐ RE-RUNNABLE AND IDEMPOTENT, like scripts/rename_diagnostic_prefix.py (kb/Work PB3575), whose rules it applies first
so a branch cut before both renames converts in one run. A branch cut before this rename runs it again after merging
main (the finisher brief's CONFLICT arm says so): every rule matches only a retired spelling, so a second run changes
nothing. It MOVES every tracked path that spells a retired name (`git mv`, a directory at a time; into a directory the
merge already created, a file at a time), then rewrites the BYTES of every tracked file, so a file's encoding, line
endings and every byte the rules do not match survive; a file whose first 8,192 bytes hold a NUL is binary (git's
heuristic) and is not read. It reads only TRACKED files (`git ls-files`).

THE RULES (RULES, in the order they apply; each maps one retired spelling, and the last of each family is total, so a
rewritten file holds no retired name — the self-test asserts it of every case):
  - the repository's LOCATION: a path segment spelled with the legacy product's name (the old checkout, its -pins, -wt
    and -coord siblings, the Claude project folder, the GitHub repository) is the repository under its present name,
    COBOL — the repository was renamed and its checkout is E:/COBOL;
  - the legacy byte engine's projects and driver, deleted from main (PHASE 15 Cut 2): `Legacy.*`, `legacy.dll`; the
    archive pointer docs/rearchitecture/LEGACY-ARCHIVE.md keeps their names at the tag that preserves them;
  - the work register's project tag: wiseowl-cobol;
  - the project, assembly, solution and namespace names: WiseOwl.Cobol; an identifier that carried the name (a test's
    compiler field, a class, a scratch prefix): WiseOwlCobol;
  - a design document's name drops the prefix; every other upper-case prefix (the environment variables) is
    WISEOWL_COBOL_, and the runtime test's pattern for "a variable the runtime reads" names the two prefixes it keeps;
  - prose: WiseOwl COBOL; a lower-case name (a semgrep rule id, a scratch folder, a stamp file): wiseowl-cobol; an
    upper-case word (a COBOL program a test names after the product): WISEOWL.

WHAT IT NEVER RENAMES OR AUDITS (EXCEPT, the one list): history and frozen records, which keep the spelling of their
day (CLAUDE.md rule 6; kb/Work PB785) — DEVLOG/, the applied verdict batches under adjudication/, the frozen evidence
under docs/rearchitecture/evidence/, and the legacy archive pointer, which names the tree at its tag; the files that
own a rule about the names — the two decision notes that quote the owner (PB3575, PB3837), the two rename scripts; and
`.claude/settings.local.json`, the per-machine settings every checkpoint leaves out of its commit (MANDATORY-PRACTICES
L7), so no commit could carry its conversion. Inside a file, a witness the traceability inventory records as RETIRED
keeps the spelling it was retired under (PROTECTED, as in rename_diagnostic_prefix.py).

⭐ AN AUDIT, NOT A TEST (`--check`; the gate driver's RETIRED NAMES audit, CI's `audits` job, the battery, and the
landing check `enqueue.py checks` runs on the MERGED head with that head's own copy of this script, exit 6). A tracked
path or a line of a tracked file outside EXCEPT that spells a retired name (RETIRED, one pattern, case-insensitive) is
RED, naming each; it is an audit for the reason scripts/conflict_markers.py is (kb/Work PB2878): a test that reads
every tracked file makes every path one a test reads. A broken enumeration (no git, too few tracked paths) is NOT RUN,
exit 2 (a failure to every gate), never a clean tree.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

import rename_diagnostic_prefix as diagnostic_prefix
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]

#: Paths never renamed and never audited: a directory (ending in `/`) and everything beneath it, or one exact path.
EXCEPT = (
    "DEVLOG/",
    "adjudication/",
    "docs/rearchitecture/evidence/",
    "docs/rearchitecture/LEGACY-ARCHIVE.md",
    "kb/Work/PB3575.md",
    "kb/Work/PB3837.md",
    "scripts/rename_diagnostic_prefix.py",
    "scripts/rename_retired_names.py",
    ".claude/settings.local.json",
)

#: An enumeration of fewer tracked paths than this is a broken one (a missing observation is not a negative one).
MIN_TRACKED = 10000

#: The two retired names, spelled once (the rules below build every spelling from them, so this file reads as rules).
_OLD = b"Cobol" + b"Net"          # the compiler's first name, in every casing below
_LEGACY = b"Cobol" + b"Sharp"     # the legacy byte engine's product name


def _ci(word: bytes) -> bytes:
    """A case-insensitive spelling of `word` as a regex fragment (one class per letter)."""
    return b"".join(b"[" + bytes([c]).upper() + bytes([c]).lower() + b"]" if chr(c).isalpha() else re.escape(bytes([c]))
                    for c in word)


#: Every retired spelling, case-insensitive: the compiler's first name with or without a separator (but never a word
#: that merely begins with it, `…network`), and the legacy product's name. The audit's one pattern.
RETIRED = re.compile(_ci(b"Cobol") + b"(?:[._-]?" + _ci(b"Net") + b"(?!" + _ci(b"work") + b")|" + _ci(b"Sharp") + b")")

#: The design documents' stems (docs/<prefix>_<stem>.md), the ones in the tree and the ones only prose still names.
DOC_STEMS = (
    b"ARCHITECTURE", b"CONDITIONS_EXCEPTIONS_DESIGN", b"CONDITIONS_EXCEPTIONS", b"CONTROL_FLOW_DESIGN",
    b"DATA_MODEL_DESIGN", b"DESIGN", b"FILES_DESIGN", b"HARNESS_LOG", b"INTERPROGRAM_DESIGN", b"INTRINSICS_DESIGN",
    b"LOCALE_DESIGN", b"NUMERIC_DESIGN", b"OO_DESIGN", b"OO_SLICE_BRIEFS", b"PIPELINE_DESIGN", b"PROJECT_ORG_DESIGN",
    b"REARCHITECTURE_PLAN", b"REDEFINES_DESIGN", b"REPORT_WRITER_DESIGN", b"REPORTS_DESIGN", b"STRING_OPS_DESIGN",
    b"VALIDATION_DESIGN",
)

_UP = _OLD.upper()
_UPL = _LEGACY.upper()
_STEMS = b"|".join(sorted(DOC_STEMS, key=len, reverse=True))

#: (pattern, replacement), applied in this order to the raw bytes of every file in scope, and to every path segment.
RULES: tuple[tuple[re.Pattern[bytes], bytes], ...] = (
    # the repository's location: a path segment named for the legacy product is the repository, renamed COBOL
    (re.compile(rb"(?<=[/\\])" + _LEGACY + rb"(?![A-Za-z0-9_]|\.[A-Za-z])"), b"COBOL"),
    (re.compile(rb"(?<=--)" + _LEGACY + rb"(?![A-Za-z0-9_])"), b"COBOL"),
    # the legacy engine's projects, solution and driver (deleted from main; LEGACY-ARCHIVE.md keeps them at the tag)
    (re.compile(_ci(_LEGACY) + rb"(?=\.(?:CLI|Compiler|Runtime|Tests|IR|CodeGen|sln|git)\b)"), b"Legacy"),
    (re.compile(_LEGACY.lower() + rb"(?=\.(?:dll|exe)\b)"), b"legacy"),
    (re.compile(_UPL + rb"_"), b"WISEOWL_COBOL_"),
    # the work register's project tag, inline or as a YAML list item
    (re.compile(rb"(?<=\[)" + _LEGACY.lower() + rb"(?=[,\]])"), b"wiseowl-cobol"),
    (re.compile(rb"(?m)(^[ \t]*- )" + _LEGACY.lower() + rb"(?=[ \t]*\r?$)"), rb"\1wiseowl-cobol"),
    # the project, assembly, solution and namespace names
    (re.compile(rb"Cobol\.Net(?![a-z])"), b"WiseOwl.Cobol"),
    (re.compile(_OLD + rb"(?=[A-Za-z0-9_])"), b"WiseOwlCobol"),
    (re.compile(_OLD + rb"(?=\.CompileAndRun\b| = new\b)"), b"WiseOwlCobol"),
    (re.compile(_OLD), b"WiseOwl.Cobol"),
    (re.compile(rb"cobolNet(?=[A-Za-z0-9_])"), b"wiseOwlCobol"),
    # the design documents drop the prefix; the environment variables take WISEOWL_COBOL_
    (re.compile(_UP + rb"_(?=(?:" + _STEMS + rb")(?![A-Z0-9_]))"), b""),
    (re.compile(rb"COBOL\(\?:NET\)\?_"), b"(?:WISEOWL_)?COBOL_"),
    (re.compile(_UP + rb"_"), b"WISEOWL_COBOL_"),
    # prose, and the remaining spellings
    (re.compile(rb"COBOL\.NET|Cobol\.NET|COBOL_NET\b"), b"WiseOwl COBOL"),
    (re.compile(_UP), b"WISEOWL"),
    (re.compile(_OLD.lower()), b"wiseowl-cobol"),
    (re.compile(b"Cobolnet(?=[0-9])"), b"Cobol"),
    (re.compile(b"Cobolnet"), b"WiseOwlCobol"),
    (re.compile(_LEGACY + b"|COBOLSharp"), b"WiseOwl COBOL"),
    (re.compile(_UPL), b"WISEOWL_COBOL"),
    (re.compile(_LEGACY.lower()), b"wiseowl-cobol"),
)

#: A file whose first BINARY_PROBE bytes hold a NUL is binary (git's heuristic) and is not rewritten.
BINARY_PROBE = 8192

#: A witness the traceability inventory records as RETIRED keeps the spelling it was retired under (the mark that lets
#: `audit_witness_loss.py` excuse the loss): the same span rename_diagnostic_prefix.py protects.
PROTECTED = diagnostic_prefix.PROTECTED


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
    """`data` with the diagnostic-prefix rules and then every rule here applied outside the PROTECTED spans, and the
    number of substitutions made."""
    data, total = diagnostic_prefix.rewrite(data)
    parts: list[bytes] = []
    pos = 0
    for m in PROTECTED.finditer(data):
        seg, n = _apply(data[pos:m.start()])
        parts += [seg, m.group()]
        total += n
        pos = m.end()
    seg, n = _apply(data[pos:])
    return b"".join(parts + [seg]), total + n


def retired_spans(data: bytes) -> list[re.Match[bytes]]:
    """Every retired spelling in `data` outside the PROTECTED spans."""
    keep = [m.span() for m in PROTECTED.finditer(data)]
    return [m for m in RETIRED.finditer(data) if not any(a <= m.start() < b for a, b in keep)]


def renamed_path(path: str) -> str:
    """`path` with every segment renamed by the same rules (a segment is a name, never a line)."""
    return "/".join(rewrite(seg.encode("utf-8"))[0].decode("utf-8") for seg in path.split("/"))


@dataclass
class Result:
    moved: list[tuple[str, str]] = field(default_factory=list)
    changed: list[str] = field(default_factory=list)
    substitutions: int = 0
    binary: int = 0


def tracked(root: Path) -> list[str]:
    """The tracked paths of the repository at `root`; RAISES when git cannot list them (an empty list would read as a
    tree with nothing to rename)."""
    out = subprocess.run(["git", "ls-files", "-z"], cwd=root, capture_output=True, check=True).stdout
    paths = [p for p in out.decode("utf-8").split("\0") if p]
    if not paths:
        raise SystemExit(f"⛔ `git ls-files` listed no tracked file in {root}")
    return paths


def _git(root: Path, *args: str) -> None:
    subprocess.run(["git", *args], cwd=root, check=True, capture_output=True)


def move(root: Path, *, write: bool) -> list[tuple[str, str]]:
    """Move every tracked path outside EXCEPT whose name spells a retired name: the shallowest renamed segment moves
    as a whole (`git mv` of a directory), or, when its new name already exists (a merge brought the renamed directory
    in beside the old one), each tracked file beneath it moves alone. Returns the (old, new) pairs."""
    moves: list[tuple[str, str]] = []
    pending = sorted(p for p in tracked(root) if not excluded(p) and renamed_path(p) != p)
    while pending:
        parts = pending[0].split("/")
        i = next(k for k, seg in enumerate(parts) if renamed_path(seg) != seg)
        src = "/".join(parts[:i + 1])
        dst = "/".join(parts[:i] + [renamed_path(parts[i])])
        under = [p for p in pending if p == src or p.startswith(src + "/")]
        if (root / dst).exists() and src.lower() != dst.lower():
            for p in under:
                target = renamed_path(p)
                moves.append((p, target))
                if write:
                    (root / target).parent.mkdir(parents=True, exist_ok=True)
                    _git(root, "mv", p, target)
        else:
            moves.append((src, dst))
            if write:
                _git(root, "mv", src, dst)
        if write:
            pending = sorted(p for p in tracked(root) if not excluded(p) and renamed_path(p) != p)
        else:
            pending = [p for p in pending if p not in under]
    return moves


def run(root: Path, paths: list[str], *, write: bool) -> Result:
    r = Result()
    for rel in paths:
        if excluded(rel):
            continue
        f = root / rel
        if not f.is_file():          # a gitlink's checkout (a submodule) or a path absent from the checkout
            continue
        data = f.read_bytes()
        if not RETIRED.search(data) and not diagnostic_prefix.rewrite(data)[1]:
            continue
        if b"\0" in data[:BINARY_PROBE]:
            r.binary += 1
            continue
        new, n = rewrite(data)
        if n:
            r.changed.append(rel)
            r.substitutions += n
            if write:
                f.write_bytes(new)
    return r


def offenders(root: Path, paths: list[str]) -> list[str]:
    """Every tracked path outside EXCEPT that spells a retired name, and every `path:line: text` of a text file that
    does (binary files are not read: a retired name inside one is no line a rule could rename)."""
    found = []
    for rel in paths:
        if excluded(rel):
            continue
        if RETIRED.search(rel.encode("utf-8")):
            found.append(f"{rel}: (the path)")
        f = root / rel
        if not f.is_file():
            continue
        data = f.read_bytes()
        if not RETIRED.search(data) or b"\0" in data[:BINARY_PROBE]:
            continue
        for n, line in enumerate(data.split(b"\n"), 1):
            if retired_spans(line):
                found.append(f"{rel}:{n}: {line.decode('utf-8', errors='replace').strip()[:160]}")
    return found


#: How many offending lines `--check` prints before it summarizes.
SHOWN = 40


def check(root: Path, min_tracked: int = MIN_TRACKED) -> int:
    """The audit: 0 when no tracked path or file outside EXCEPT spells a retired name, 1 naming every one when one
    does, and 2 (NOT RUN, never a pass) when the tree cannot be enumerated or enumerates to fewer than `min_tracked`
    paths: the landing check maps 1 to a refusal a finisher answers and 2 to NOT RUN (enqueue.py CHECKS)."""
    try:
        paths = tracked(root)
    except (subprocess.CalledProcessError, OSError, SystemExit) as e:
        print(f"=== RETIRED NAMES: NOT RUN — the tracked tree of {root} could not be listed ({e}) ===")
        return 2
    if len(paths) < min_tracked:
        print(f"=== RETIRED NAMES: NOT RUN — only {len(paths)} tracked path(s) under {root} (at least {min_tracked} "
              "expected): the enumeration is broken, so the audit would read nothing ===")
        return 2
    found = offenders(root, paths)
    if found:
        for o in found[:SHOWN]:
            print(f"    {o}")
        if len(found) > SHOWN:
            print(f"    ... and {len(found) - SHOWN} more")
        print(f"=== RETIRED NAMES: RED — {len(found)} path(s) or line(s) spell a retired name; the code's name is "
              "WiseOwl.Cobol (kb/Work PB3837). Convert the tree with `python scripts/rename_retired_names.py` ===")
        return 1
    print(f"=== RETIRED NAMES: GREEN — {len(paths)} tracked path(s), none spells a retired name ===")
    return 0


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    old, up, leg = _OLD.decode(), _UP.decode(), _LEGACY.decode()   # built, so the cases read as the shapes they test
    dotted = "Cobol" + ".Net"
    cases = [
        (f"src/{dotted}.Compiler/{dotted}.Compiler.csproj, {dotted}.sln, {dotted}.Tests.Unit",
         "src/WiseOwl.Cobol.Compiler/WiseOwl.Cobol.Compiler.csproj, WiseOwl.Cobol.sln, WiseOwl.Cobol.Tests.Unit",
         "the projects, the solution and the test assemblies"),
        (f"namespace {old}.Binding; using {old}; using global::{old}.Runtime; <RootNamespace>{old}</RootNamespace>",
         "namespace WiseOwl.Cobol.Binding; using WiseOwl.Cobol; using global::WiseOwl.Cobol.Runtime; "
         "<RootNamespace>WiseOwl.Cobol</RootNamespace>", "the root namespace"),
        (f"static readonly {old}Compiler {old} = new(); {old}.CompileAndRun(s); {old}85.Run(); \"{old}_Char_\"",
         "static readonly WiseOwlCobolCompiler WiseOwlCobol = new(); WiseOwlCobol.CompileAndRun(s); "
         "WiseOwlCobol85.Run(); \"WiseOwlCobol_Char_\"", "an identifier that carried the name"),
        (f"docs/{up}_DESIGN.md, [[{up}_REARCHITECTURE_PLAN]], {up}_FILES_DESIGN §3, {up}_OO_SLICE_BRIEFS",
         "docs/DESIGN.md, [[REARCHITECTURE_PLAN]], FILES_DESIGN §3, OO_SLICE_BRIEFS", "the design documents"),
        (f"{up}_CLOCK, {up}_GATE_PLAN, {up}_GATE_PLAN_SHA256, \"{up}_GATE_\", {up}_*, {up}_<NAME>",
         "WISEOWL_COBOL_CLOCK, WISEOWL_COBOL_GATE_PLAN, WISEOWL_COBOL_GATE_PLAN_SHA256, \"WISEOWL_COBOL_GATE_\", "
         "WISEOWL_COBOL_*, WISEOWL_COBOL_<NAME>", "the environment variables, a plan file stem among them"),
        (r'new("\"(COBOL(?:NET)?_[A-Z0-9_]*)\"")', r'new("\"((?:WISEOWL_)?COBOL_[A-Z0-9_]*)\"")',
         "the pattern for the variables the runtime reads"),
        (f"{up}0899 and error {up}1234", "COBOL0899 and error COBOL1234", "a diagnostic code (PB3575's rules first)"),
        (f"the COBOL.NET compiler; Cobol.NET; `{old.lower()}-no-biginteger`; [{old.lower()} compile]; "
         f"CALL \"{up}\"; HELLO FROM {up}.",
         "the WiseOwl COBOL compiler; WiseOwl COBOL; `wiseowl-cobol-no-biginteger`; [wiseowl-cobol compile]; "
         "CALL \"WISEOWL\"; HELLO FROM WISEOWL.", "prose, a lower-case name, a program named for the product"),
        (f"Emits{old[:5]}net1620_OnEach, {old[:5]}net", "EmitsCobol1620_OnEach, WiseOwlCobol",
         "a test name carrying a code"),
        (f"E:\\{leg}\\.claude, E:/{leg}-pins/adj-d1, E:\\\\{leg}\\\\x, E--{leg}\\a7, /mnt/e/{leg}/s, "
         f"WiseOwlSoftware/{leg}.",
         "E:\\COBOL\\.claude, E:/COBOL-pins/adj-d1, E:\\\\COBOL\\\\x, E--COBOL\\a7, /mnt/e/COBOL/s, "
         "WiseOwlSoftware/COBOL.", "the repository's location"),
        (f"src/{leg}.CLI/bin/{leg.lower()}.dll, {leg}.Tests.Unit, {leg.upper()}_LEGACY_DIFFERENTIAL=1",
         "src/Legacy.CLI/bin/legacy.dll, Legacy.Tests.Unit, WISEOWL_COBOL_LEGACY_DIFFERENTIAL=1",
         "the legacy engine's projects, driver and switch"),
        (f"tags: [{leg.lower()}, work, defect]\r\ntags:\r\n  - {leg.lower()}\r\n  - spec\r\n",
         "tags: [wiseowl-cobol, work, defect]\r\ntags:\r\n  - wiseowl-cobol\r\n  - spec\r\n", "the project tag (CRLF)"),
        (f"the legacy {leg} engine; {leg.lower()}-skill-doctor.stamp; \"{leg.upper()}\"",
         "the legacy WiseOwl COBOL engine; wiseowl-cobol-skill-doctor.stamp; \"WISEOWL_COBOL\"", "the legacy name in prose"),
        ("COBOL network, CobolNumeric, Cobol.Networking", "COBOL network, CobolNumeric, Cobol.Networking",
         "a word that only begins with the letters stays"),
    ]
    for before, after, label in cases:
        got, _ = rewrite(before.encode("utf-8"))
        expect(label, got.decode("utf-8"), after)
        expect(label + " (no retired name is left)", [m.group() for m in retired_spans(got)], [])
        again, n = rewrite(got)
        expect(label + " (idempotent: a second run changes nothing)", (again, n), (got, 0))
    latin = b"caf\xe9 " + _OLD
    expect("a byte that is not UTF-8 survives", rewrite(latin)[0], b"caf\xe9 WiseOwl.Cobol")
    mark = diagnostic_prefix.RETIRED_MARK.decode()
    notes = f"{mark} unit:T.X_Is{old[:5]}net1934 and {up}_CLOCK"
    expect("a retired witness keeps its spelling, and the audit excuses it",
           (rewrite(notes.encode())[0].decode(), retired_spans(rewrite(notes.encode())[0])),
           (f"{mark} unit:T.X_Is{old[:5]}net1934 and WISEOWL_COBOL_CLOCK", []))
    expect("a path is renamed segment by segment",
           [renamed_path(p) for p in (f"src/{dotted}.Cli/{dotted}.Cli.csproj", f"docs/{up}_DESIGN.md",
                                      f"tests/{dotted}.Tests.Unit/{old}TestBase.cs", "docs/CONFORMANCE.md")],
           ["src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj", "docs/DESIGN.md",
            "tests/WiseOwl.Cobol.Tests.Unit/WiseOwlCobolTestBase.cs", "docs/CONFORMANCE.md"])
    expect("history and the rule's owners are never renamed", [excluded(p) for p in (
        "DEVLOG/0464-x.md", "adjudication/a/b.json", "docs/rearchitecture/evidence/x.json",
        "docs/rearchitecture/LEGACY-ARCHIVE.md", "kb/Work/PB3837.md", "scripts/rename_diagnostic_prefix.py")],
        [True] * 6)
    expect("a current doc, a note and the source are renamed", [excluded(p) for p in (
        "CLAUDE.md", "kb/Work/PB3838.md", "src/x.cs", "docs/rearchitecture/evidence-index.md")], [False] * 4)

    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415

    root = scratch_dir("rename-retired-")
    for rel, text in ((f"src/{dotted}.Cli/{dotted}.Cli.csproj", f"<RootNamespace>{old}.Cli</RootNamespace>\n"),
                      (f"src/{dotted}.Cli/P.cs", f"namespace {old}.Cli;\n"),
                      (f"docs/{up}_DESIGN.md", "# design\n"),
                      ("README.md", f"See docs/{up}_DESIGN.md and {dotted}.sln.\n"),
                      ("DEVLOG/0001-x.md", f"history {dotted}\n")):
        (root / rel).parent.mkdir(parents=True, exist_ok=True)
        (root / rel).write_bytes(text.encode())
    (root / "b.bin").write_bytes(b"\0" + _OLD)
    repo = ScratchRepo.init(root)
    repo.git("add", "-A")
    expect("the audit is RED on the old tree, naming a path and a line",
           (check(root, min_tracked=1), "README.md:1: See docs/" + up + "_DESIGN.md and " + dotted + ".sln." in
            offenders(root, tracked(root))), (1, True))
    moved = move(root, write=True)
    r = run(root, tracked(root), write=True)
    expect("a run moves the directory and the document, then rewrites the contents (history and binary set aside)",
           (sorted(moved), sorted(r.changed), r.binary),
           (sorted([(f"docs/{up}_DESIGN.md", "docs/DESIGN.md"), (f"src/{dotted}.Cli", "src/WiseOwl.Cobol.Cli"),
                    (f"src/WiseOwl.Cobol.Cli/{dotted}.Cli.csproj", "src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj")]),
            sorted(["README.md", "src/WiseOwl.Cobol.Cli/P.cs", "src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj"]), 1))
    expect("the source now reads the new name", (root / "src/WiseOwl.Cobol.Cli/P.cs").read_bytes(),
           b"namespace WiseOwl.Cobol.Cli;\n")
    expect("the history keeps its spelling", (root / "DEVLOG/0001-x.md").read_bytes(), f"history {dotted}\n".encode())
    expect("a second run moves and changes nothing", (move(root, write=True), run(root, tracked(root), write=True)
                                                      .changed), ([], []))
    expect("the audit is GREEN once the tree is converted", check(root, min_tracked=1), 0)
    # a branch cut before the rename: its new file lands in the OLD directory beside the renamed one (a merge)
    (root / f"src/{dotted}.Cli").mkdir()
    (root / f"src/{dotted}.Cli/Late.cs").write_bytes(f"namespace {old}.Cli;\n".encode())
    repo.git("add", "-A")
    expect("a file a merge left in the old directory moves alone into the renamed one",
           move(root, write=True), [(f"src/{dotted}.Cli/Late.cs", "src/WiseOwl.Cobol.Cli/Late.cs")])
    run(root, tracked(root), write=True)
    expect("... and is rewritten", (root / "src/WiseOwl.Cobol.Cli/Late.cs").read_bytes(), b"namespace WiseOwl.Cobol.Cli;\n")
    expect("an enumeration of too few tracked paths is NOT RUN (exit 2), never a pass", check(root), 2)
    bare = scratch_dir("rename-retired-bare-")
    expect("a directory git cannot enumerate is NOT RUN (exit 2), never a pass", check(bare, min_tracked=1), 2)
    print(f"=== RENAME RETIRED NAMES SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    group = ap.add_mutually_exclusive_group()
    group.add_argument("--dry-run", action="store_true", help="name the moves and files a run would change")
    group.add_argument("--check", action="store_true", help="the audit: exit 1 naming every retired name")
    group.add_argument("--self-test", action="store_true")
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.check:
        return check(args.root)
    moved = move(args.root, write=not args.dry_run)
    r = run(args.root, tracked(args.root), write=not args.dry_run)
    if args.dry_run:
        for a, b in moved:
            print(f"    move {a} -> {b}")
        for rel in r.changed:
            print(f"    {rel}")
    verb = "would" if args.dry_run else "did"
    print(f"=== RENAME RETIRED NAMES: {verb} move {len(moved)} path(s), change {len(r.changed)} file(s) "
          f"({r.substitutions} substitution(s)); {r.binary} binary file(s) set aside ===")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
