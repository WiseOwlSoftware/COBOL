#!/usr/bin/env python3
"""test_population.py — did the union of the runs execute EXACTLY the discovered population? (kb/Work PB1718;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.3–3.14.4)

⛔ ONE TOOL FOR EVERY WHOLE-ASSEMBLY RUN. The gate's soundness rests on "every discovered test case ran exactly
once", and until this tool three runs asserted it three different ways or not at all: CI summed each shard's
`Total:` line against a `grep -c` of the listing (a count cannot see a dropped case offset by one that ran twice),
while the battery accepted any `Passed!` line with exit 0 and the gate checked nothing. Its callers are the
battery (`scripts/battery.sh` PHASE 1, all three assemblies) and CI's `ci-gate` job (each platform's weighted
shards' trx files, per assembly, kb/Work PB4329); the gate (scripts/gate.py) calls `list_population` and `check` per
assembly over its host's trx.

THE QUESTION, answered as multisets of display names:
  population  `dotnet test --list-tests` — vstest DISCOVERY, which never calls the executor and so cannot see an
              in-assembly leg filter. Listed by THIS tool in a scrubbed environment (below), so an environment
              that narrows the run cannot narrow the population it is checked against.
  executed    the union of the trx files' TEST DEFINITIONS (`<UnitTest name>`, one per discovered case that
              executed). NOT the result names: xunit lists a theory whose data it cannot serialize as ONE case and
              then reports one result per row, so Unit's results carry names `--list-tests` never printed while
              its definitions equal the listing exactly (evidence `impact-map-pb1708/b8`).
  NEVER RAN   a listed name short in the definitions — red, each named (NOT RUN instead when the caller says the
              run stopped early: the verdict is RED/INCOMPLETE either way);
  RAN TWICE   a listed name over — red, named (two shards whose filters overlap);
  NOT IN THE POPULATION  a definition the listing never printed — red, named (a trx from another build or
              assembly);
  skipped     a definition with no Passed or Failed result (a `[Fact(Skip)]` is NotExecuted) — counted on the
              line, never as ran.
A run that stopped early (a fail-fast stop) still answers RAN TWICE and NOT IN THE POPULATION:
only NEVER RAN depends on the run having finished (`Population.defective`, kb/Work PB4274).

⛔ ONE SPELLING (kb/Work PB4274). The two sides do NOT spell a display name the same way. xunit's argument formatter
writes a string argument with C escapes (`\\\\`, `\\"`, `\\n`, a C0 control as `\\x01`, a LONE surrogate as `\\xd800`)
and leaves every other character literal, a supplementary-plane one included; `--list-tests` prints that name as
UTF-8. The trx logger then rewrites every UTF-16 code unit outside XML's character range `[^\\x09\\x0A\\x0D\\x20-\\uD7FF
\\uE000-\\uFFFD]` as `\\u` and four lowercase hex digits (Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger,
`RemoveInvalidXmlChar`), so a supplementary character, a valid surrogate PAIR, reaches the trx as the twelve
characters `\\ud834\\udd5e`. Every comparison of a listed or in-process name with a trx name therefore goes through
`trx_spelling`, that rewrite stated once: it is what the logger does, so a case always meets itself, and it is the identity on a trx name. It cannot
be inverted (the logger escapes no backslash), so two listed names it maps to one spelling are an ERROR the check
names, never a silent merge; an xunit string argument holding the literal text `\\ud834` is listed as `\\\\ud834` and
stays distinct from the character.

⛔ THE SCRUB (§3.14.3). Three channels can make a whole-assembly `dotnet test` run a PART of its assembly while
exiting 0, and every one of them is an ENVIRONMENT variable:
  WISEOWL_COBOL_GATE_*     the gate's own channel to a test host (`WISEOWL_COBOL_GATE_FIRST`, the classes its orderer
                           runs first; scripts/gate.py `FIRST_CLASSES`): only the gate may hand it to a host;
  VSTest*                  MSBuild imports every environment variable as a property, and the VSTest target reads
                           `VSTestTestCaseFilter`, `VSTestSetting`, `VSTestListTests`, … — MEASURED 2026-09-28:
                           `VSTestTestCaseFilter=FullyQualifiedName~Snapshot` narrowed Characterization from 33
                           cases to 31, in the run AND in `--list-tests`, exit 0;
  RunSettingsFilePath      the same channel through the Test SDK's run-settings property (MEASURED: a runsettings
                           file carrying a TestCaseFilter narrowed the listing to 31 the same way);
  WISEOWL_COBOL_SHARD      CI's weighted shards (kb/Work PB4329): the test framework runs only shard k of N. The one
                           variable a workflow may set, and only one that also runs `check` over its shards' trx
                           (`ci-gate`), which is what makes the narrowing a division and never a loss.
Because the VSTest channel narrows DISCOVERY too, a population listed in the caller's environment would be blind
to it — which is why the listing here always runs scrubbed. `is_scrubbed` is the ONE statement of the rule;
python callers pass `env=scrubbed_env()`, every other caller runs `python scripts/test_population.py scrubbed
dotnet test …`, and `audit-callers` (GateLegDriftTests arm 6) is red on a `dotnet test` under `scripts/` that
does neither, or on a CI workflow that SETS a scrubbed variable (a GitHub-hosted job's environment is exactly
what the workflow declares, so a workflow that sets none is scrubbed by construction).

Usage:
  python scripts/test_population.py check --label L (--project P [--configuration C] [--save-listing F]
                                                     | --listing F [F …]) --trx T [T …] [--stopped-early] [--method M]
  python scripts/test_population.py list --project P [--configuration C] --out F
  python scripts/test_population.py scrubbed <command> [args …]
  python scripts/test_population.py audit-callers
  python scripts/test_population.py --self-test

Several `--listing` files (one per CI shard) must be identical — the shards ran one build — or the check is an
ERROR. exit 0 EXACT · 1 RED (never ran / ran twice / not in the population / stopped early) · 2 the check could
not run (a missing or unparsable input, an empty population, listings that disagree): an error, never a finding.
"""

from __future__ import annotations

import argparse
import ast
import os
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parent.parent
LISTED_MARK = "The following Tests are available:"
LISTED_LINE = re.compile(r"^ {4}\S")
TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
RAN_OUTCOMES = frozenset({"Passed", "Failed"})
RC_EXACT, RC_RED, RC_ERROR = 0, 1, 2

#: THE GATED ASSEMBLIES — every whole-population run's three test projects, by the key the gate, the weights file and
#: CI's shards name them with (the test assembly's name without `WiseOwl.Cobol.Tests.`). Their one statement: the gate
#: and the shard tools read it from here.
GATED_ASSEMBLIES = {
    "Conformance": "tests/WiseOwl.Cobol.Tests.Conformance",
    "Unit": "tests/WiseOwl.Cobol.Tests.Unit",
    "Characterization": "tests/WiseOwl.Cobol.Tests.Characterization",
}

#: CI's weighted shards (kb/Work PB4329; tests/_shared/GateLegs.cs `GateShard`): the one narrowing a WORKFLOW may set,
#: because the workflow that sets it checks its shards' union with `check` (`audit_callers` holds it to that).
SHARD = "WISEOWL_COBOL_SHARD"
_SCRUBBED_PREFIXES = ("WISEOWL_COBOL_GATE_", "VSTEST")
_SCRUBBED_NAMES = frozenset({"RUNSETTINGSFILEPATH", SHARD})


class PopulationError(Exception):
    """The check could not run — a missing observation, never a negative one (exit 2)."""


class UnreadableTrx(PopulationError):
    """A trx file that cannot be read or is not a trx file: it names the file (`path`), so a caller that keeps the
    evidence (the gate driver) and a reader of the message both know which one (kb/Work PB3661)."""

    def __init__(self, path: str | Path, why: str):
        super().__init__(f"{path}: {why}")
        self.path = Path(path)


def is_scrubbed(name: str) -> bool:
    """THE scrub rule: may this environment variable narrow a `dotnet test` run? Case-insensitive, because both
    the Windows environment and MSBuild property names are."""
    upper = name.upper()
    return upper.startswith(_SCRUBBED_PREFIXES) or upper in _SCRUBBED_NAMES


def scrubbed_env(base: dict[str, str] | None = None) -> dict[str, str]:
    """`base` (default: this process's environment) without every variable `is_scrubbed` names. A caller that
    must add variables of its own merges them over the result: `{**scrubbed_env(), "X": "1"}`."""
    return {k: v for k, v in (os.environ if base is None else base).items() if not is_scrubbed(k)}


# ── the one spelling (kb/Work PB4274) ─────────────────────────────────────────────────────────────────────────


#: XML 1.0's character range, the one the trx logger keeps (`RemoveInvalidXmlChar`), stated ONCE and as escapes (kb/Work
#: PB4274's review: a tool once decoded the escapes it was handed and wrote the literal characters U+D7FF, U+E000 and
#: U+FFFD into this file). `_IS_XML` and `_NOT_XML` are both derived from it. A Python str holds code points, so a
#: supplementary character (above U+FFFF) is in neither the range nor `_IS_XML`, and `_NOT_XML` matches it. One
#: compiled search finds the few names that need the rewrite, where a per-character test of every name cost each gate
#: the whole population (kb/Work PB4274 review R1-3).
XML_RANGE = "\\t\\n\\r\\x20-\\ud7ff\\ue000-\\ufffd"
_IS_XML = re.compile(f"[{XML_RANGE}]")
_NOT_XML = re.compile(f"[^{XML_RANGE}]")


def trx_spelling(name: str) -> str:
    """`name` as the trx logger writes it: every UTF-16 code unit outside XML's character range becomes `\\u` and four
    lowercase hex digits, a supplementary character as its two surrogates (the module docstring, ONE SPELLING). The
    identity on a name read from a trx."""
    if not _NOT_XML.search(name):
        return name
    out = []
    for c in name:
        cp = ord(c)
        if _IS_XML.match(c):
            out.append(c)
        elif cp > 0xFFFF:
            cp -= 0x10000
            out.append(f"\\u{0xD800 + (cp >> 10):04x}\\u{0xDC00 + (cp & 0x3FF):04x}")
        else:
            out.append(f"\\u{cp:04x}")
    return "".join(out)


def spelled_as_trx(names: Counter, source: str) -> Counter:
    """A multiset of listed names in the trx's spelling. Two distinct names the logger spells alike cannot be told
    apart by any trx, so they are an ERROR that names both, never one merged count."""
    spelled: Counter = Counter()
    first: dict[str, str] = {}
    for name, n in names.items():
        key = trx_spelling(name)
        if first.setdefault(key, name) != name:
            raise PopulationError(f"{source}: two listed names are one name in a trx ({first[key]!r} and {name!r} are "
                                  f"both written {key!r}) — no trx can tell which of them ran")
        spelled[key] += n
    return spelled


# ── the population ────────────────────────────────────────────────────────────────────────────────────────────


def parse_listing(text: str, source: str) -> Counter:
    """The display names a `dotnet test --list-tests` output lists, as a multiset (xunit truncates long theory
    arguments with `···`, so one name can cover several rows)."""
    if LISTED_MARK not in text:
        raise PopulationError(f"{source}: no `{LISTED_MARK}` line — not a `dotnet test --list-tests` output")
    return Counter(line[4:] for line in text.split(LISTED_MARK, 1)[1].splitlines() if LISTED_LINE.match(line))


def list_population(project: str | Path, configuration: str | None = None,
                    cwd: str | Path = REPO, **spawn) -> tuple[Counter, str]:
    """Discover `project`'s population with `--list-tests`, scrubbed, in English. Returns (names, raw output).
    `spawn` is extra `subprocess` keyword arguments: a gate-slot holder passes `**slot.spawn_kwargs()`, so the
    listing runs inside the cap like every other child of the holder (DESIGN §3.14.6)."""
    cmd = ["dotnet", "test", str(project), "--no-build", "--list-tests"]
    if configuration:
        cmd += ["--configuration", configuration]
    # The caller's `env` (a slot holder's, say) is scrubbed like any other: nothing may narrow the population.
    env = {**scrubbed_env(spawn.pop("env", None)), "DOTNET_CLI_UI_LANGUAGE": "en"}
    proc = subprocess.run(cmd, cwd=cwd, capture_output=True, env=env, **spawn)
    try:
        # STRICT: a mis-decoded name would read as NEVER RAN beside a NOT IN THE POPULATION twin.
        text = proc.stdout.decode("utf-8")
    except UnicodeDecodeError as e:
        raise PopulationError(f"{project}: the listing is not UTF-8 ({e})") from e
    if proc.returncode != 0 or LISTED_MARK not in text:
        tail = (text + proc.stderr.decode("utf-8", "replace")).strip()[-1500:]
        raise PopulationError(f"{project}: `--list-tests` failed (exit {proc.returncode}) — build the solution "
                              f"first, it runs --no-build:\n{tail}")
    return parse_listing(text, str(project)), text


def of_method(listed: Counter, method: str) -> Counter:
    """The listed cases of the test method named `method` (a display name's segment after its last `.` before its
    first `(`): the population of a run filtered `FullyQualifiedName~<method>` where the method name is unique in the
    assembly, as INV-1-strong's NistProgram_MatchesGolden is (kb/Work PB4329)."""
    return Counter({n: c for n, c in listed.items() if n.split("(", 1)[0].rsplit(".", 1)[-1] == method})


def read_listings(paths: list[str]) -> Counter:
    """One population from one or more saved listings — which must agree: the runs came from one build."""
    listed: Counter | None = None
    first = ""
    for p in paths:
        try:
            names = parse_listing(Path(p).read_text(encoding="utf-8"), p)
        except (OSError, UnicodeDecodeError) as e:
            raise PopulationError(f"{p}: cannot read the listing ({e})") from e
        if listed is None:
            listed, first = names, p
        elif names != listed:
            raise PopulationError(f"LISTINGS DISAGREE: {p} and {first} differ by "
                                  f"{sum(((names - listed) + (listed - names)).values())} name(s) — the trx "
                                  "files do not come from one build")
    if listed is None:
        raise PopulationError("no listing given")
    return listed


# ── the executed set ──────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class TrxCases:
    definitions: Counter   # every <UnitTest name> — one per discovered case that executed
    skipped: Counter       # the definitions with no Passed or Failed result


def trx_root(path: str | Path) -> ET.Element:
    """THE ONE READER of a trx file (kb/Work PB3661): its root element, or an `UnreadableTrx` that names the file. Every
    consumer of a trx under scripts/ (the population check's `read_trx`, the gate's `red_classes`,
    the profiler) reads through it, so none can fail with a parse error that names no file."""
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as e:
        raise UnreadableTrx(path, f"cannot read the trx ({e})") from e
    if root.tag != TRX_NS + "TestRun":
        raise UnreadableTrx(path, f"not a trx file (root element {root.tag})")
    return root


def red_classes(paths: list[str | Path]) -> set[str]:
    """THE CLASSES WITH A RED CASE in these trx files (kb/Work PB4328): the `className` of the test method of every
    definition one of whose results Failed — the spelling the weights file, the shards and the gate's orderer use
    (tests/_shared/GateLegs.cs `GateShard.ClassOf`). A file that cannot be read raises `UnreadableTrx` naming it."""
    classes: set[str] = set()
    for path in paths:
        root = trx_root(path)
        failed = {r.get("testId", "") for r in root.iter(TRX_NS + "UnitTestResult") if r.get("outcome") == "Failed"}
        for u in root.iter(TRX_NS + "UnitTest"):
            method = u.find(TRX_NS + "TestMethod")
            if u.get("id", "") in failed and method is not None and method.get("className"):
                classes.add(method.get("className"))
    return classes


# ── RED AT THE PREVIOUS GATE MEANS ANY HOST'S (kb/Work PB3155, PB3184, PB3644): the sources of the first classes ──────
#: ⛔ EVERY LIVE TOOL THAT WRITES TEST RESULTS (a trx file) INTO A WORKTREE IS ONE ROW HERE: the results directory it
#: writes, relative to the worktree (None: it writes none there), and what the gate takes from it (scripts/gate.py `Gate._first`).
#: `GateLegDriftTests` holds this table equal to the live tools that write a trx file (`audit-trx-writers`), so a new
#: writer is classified here or that test is red.
TRX_WRITERS: dict[str, tuple[str | None, str]] = {
    "scripts/gate.py": ("TestResults/build-local", "the previous run (`Gate.previous_run`): its red classes run first"),
    "scripts/linux-gate.sh": ("TestResults/linux-gate", "the hand-run Linux gate's newest run: its red classes run first"),
    "scripts/battery.sh": (None, "none: the battery runs in its own detached worktree at a batch head, never a gate "
                                 "of a branch"),
}
LINUX_GATE_RESULTS = Path(TRX_WRITERS["scripts/linux-gate.sh"][0])
#: What makes a live tool a trx writer: the vstest logger argument naming the trx file (split here, so this file's own
#: mention is the one its row explains).
TRX_LOGGER_MARK = "trx;" + "LogFileName="


def audit_trx_writers(repo: Path) -> tuple[list[str], list[str]]:
    """(the live tools that write a trx file, each with its TRX_WRITERS row; the offenders): a writer with no row, a row
    whose tool is gone or writes no trx file, and a row whose results directory its tool never names."""
    import live_tools  # noqa: PLC0415  (only the audit reads the live tools)
    writers = [f for f in live_tools.tracked(repo)
               if TRX_LOGGER_MARK in (repo / f).read_text(encoding="utf-8", errors="replace")]
    seen = [f"  writer  {f}  {TRX_WRITERS[f][0] or '-'}  {TRX_WRITERS[f][1]}" for f in writers if f in TRX_WRITERS]
    bad = [f"  {f}: writes a trx file and has no row in test_population.TRX_WRITERS — say whether its red classes run first at "
           f"the next gate" for f in writers if f not in TRX_WRITERS]
    for f, (results, _) in sorted(TRX_WRITERS.items()):
        if f not in writers:
            bad.append(f"  {f}: a TRX_WRITERS row, but no tracked live tool of that name writes a trx file")
        elif results is not None and results not in (repo / f).read_text(encoding="utf-8", errors="replace"):
            bad.append(f"  {f}: its row says it writes {results}/, which it never names")
    return seen, bad
#: ⛔ ONE READER OF A TRX FILE (kb/Work PB3661): `trx_root`, which names the file it cannot read. A live tool that names
#: the trx schema and imports an XML parser is a second reader, and it fails with a parse error that names no file (the
#: gate planner did, and so did the profiler). Both marks are built from parts, so this file's own audit is not a hit.
TRX_READER = "scripts/test_population.py"
TRX_SCHEMA_MARK = "Team" + "Test/2010"
XML_PARSER_RE = re.compile("Element" + "Tree|mini" + "dom|l" + "xml|xml[.]sax|from" + "string")


def audit_trx_readers(repo: Path = REPO) -> tuple[list[str], list[str]]:
    """(the live tools that name the trx schema, each marked reader or not; the offenders): every live tool that names the
    trx schema AND imports an XML parser, other than TRX_READER, reads a trx file itself."""
    import live_tools  # noqa: PLC0415  (only the audit reads the live tools)
    seen, bad = [], []
    for f in live_tools.tracked(repo, (".py", ".sh", ".ps1")):
        text = (repo / f).read_text(encoding="utf-8", errors="replace")
        if TRX_SCHEMA_MARK not in text:
            continue
        parses = XML_PARSER_RE.search(text) is not None
        seen.append(f"  trx-schema  {f}  {'parses XML' if parses else 'no XML parser'}")
        if parses and f != TRX_READER:
            bad.append(f"  {f}: names the trx schema and imports an XML parser, so it reads a trx file itself — read "
                       f"it through {TRX_READER}'s trx_root (the one reader, which names a file it cannot read)")
    if not any(TRX_READER in line and "parses XML" in line for line in seen):
        bad.append(f"  {TRX_READER}: the one trx reader is not seen — the audit is broken, not the tree clean")
    return seen, bad


def read_trx(path: str | Path) -> TrxCases:
    root = trx_root(path)
    outcomes: dict[str, set[str]] = defaultdict(set)
    for r in root.iter(TRX_NS + "UnitTestResult"):
        outcomes[r.get("testId", "")].add(r.get("outcome", ""))
    definitions, skipped = Counter(), Counter()
    for u in root.iter(TRX_NS + "UnitTest"):
        name = u.get("name", "")
        definitions[name] += 1
        if not outcomes[u.get("id", "")] & RAN_OUTCOMES:
            skipped[name] += 1
    return TrxCases(definitions, skipped)


# ── the check ─────────────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class Population:
    label: str
    discovered: int
    ran: int
    skipped: int
    trx_files: int
    never_ran: Counter
    ran_twice: Counter
    not_listed: Counter
    stopped_early: bool

    @property
    def exact(self) -> bool:
        return not (self.never_ran or self.ran_twice or self.not_listed or self.stopped_early)

    @property
    def defective(self) -> bool:
        """A finding whether or not the run finished: a case twice, a name the listing never printed, or (a finished
        run only) a case that never ran. A stopped run's remainder is NOT RUN, its own verdict and no defect; the rest
        is (kb/Work PB4274: leg-1-only gates printed NOT IN THE POPULATION in 15 runs and stayed GREEN)."""
        return bool(self.ran_twice or self.not_listed or (self.never_ran and not self.stopped_early))

    def verdict_line(self) -> str:
        tally = (f"{self.ran:,}/{self.discovered:,} cases ran (skipped {self.skipped:,}) across "
                 f"{self.trx_files} trx")
        if self.exact:
            return f"=== POPULATION {self.label}: EXACT — {tally} ==="
        parts = [f"{sum(self.never_ran.values()):,} "
                 + ("NOT RUN (the run stopped early)" if self.stopped_early else "NEVER RAN")] if self.never_ran else []
        if self.ran_twice:
            parts.append(f"{sum(self.ran_twice.values()):,} RAN TWICE")
        if self.not_listed:
            parts.append(f"{sum(self.not_listed.values()):,} NOT IN THE POPULATION")
        head = "RED/INCOMPLETE" if self.stopped_early else "RED"
        return f"=== POPULATION {self.label}: {head} — {' · '.join(parts) or 'the run stopped early'} — {tally} ==="

    def report(self) -> list[str]:
        lines = [self.verdict_line()]
        tag = "NOT RUN" if self.stopped_early else "NEVER RAN"
        for label, names in ((tag, self.never_ran), ("RAN TWICE", self.ran_twice),
                             ("NOT IN THE POPULATION", self.not_listed)):
            for name in sorted(names):
                lines.append(f"  {label:<21} x{names[name]}  {name}")
        return lines


def check(label: str, listed: Counter, trx_paths: list[str], stopped_early: bool = False) -> Population:
    """Did the union of `trx_paths` execute exactly `listed`? Raises PopulationError when it cannot tell."""
    if not listed:
        raise PopulationError(f"{label}: the population is EMPTY — a check over nothing can never be clean")
    if not trx_paths:
        raise PopulationError(f"{label}: no trx file given")
    listed = spelled_as_trx(listed, label)   # ONE SPELLING (kb/Work PB4274): the trx logger's, on both sides
    definitions, skipped = Counter(), Counter()
    for p in trx_paths:
        cases = read_trx(p)
        for n, c in cases.definitions.items():
            definitions[trx_spelling(n)] += c
        for n, c in cases.skipped.items():
            skipped[trx_spelling(n)] += c
    over = definitions - listed
    ran_twice = Counter({n: c for n, c in over.items() if n in listed})
    not_listed = Counter({n: c for n, c in over.items() if n not in listed})
    skip_total = sum(skipped.values())
    return Population(label=label, discovered=sum(listed.values()),
                      ran=sum(definitions.values()) - skip_total, skipped=skip_total, trx_files=len(trx_paths),
                      never_ran=listed - definitions, ran_twice=ran_twice, not_listed=not_listed,
                      stopped_early=stopped_early)


# ── the caller audit (GateLegDriftTests arm 6) ────────────────────────────────────────────────────────────────

#: `dotnet test` in COMMAND position: at the start of a line or string, or after a shell separator, `&`, `(`,
#: `$(` or a quote (not a backtick: in this tree that is Markdown prose far more often than a substitution). The
#: scrubbed form (`… test_population.py scrubbed dotnet test`) is never in command position, so it needs no
#: exemption of its own.
_COMMAND_POSITION = re.compile(r"(?:^|[;&|(\"']|\$\()\s*dotnet\s+test\b")
_ANY_DOTNET_TEST = re.compile(r"\bdotnet\s+test\b")
_SCRUBBED_RUN = re.compile(r"\bscrubbed\s+dotnet\s+test\b")
#: A name immediately followed by `=` or `:` — an assignment, an `env:` key, `-p:Name=`, `$env:Name =`.
_ASSIGNED = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*[:=]")
#: A run of the union check (`check` always reads `--trx`): what licenses a workflow to set SHARD.
_CHECKS_UNION = re.compile(r"\btest_population\.py\s+check\b")
MECHANISM = "scripts/test_population.py"
#: Documentation and data under scripts/ — prose that may QUOTE a command, never a runner.
_PROSE_SUFFIXES = frozenset({".md", ".txt", ".json", ".tsv", ".csv"})


def _code_lines(text: str) -> list[tuple[int, str]]:
    """Every line that is not a WHOLE-line `#` comment (bash, PowerShell, YAML)."""
    return [(i, line) for i, line in enumerate(text.split("\n"), 1) if not line.lstrip().startswith("#")]


def _python_findings(text: str, rel: str) -> tuple[bool, list[str]]:
    """A python file runs `dotnet test` through a `["dotnet", "test", …]` literal (must then call
    `scrubbed_env`) or prints one as an instruction (must then print the scrubbed form). Docstrings are prose."""
    tree = ast.parse(text, filename=rel)
    docstrings = set()
    for node in ast.walk(tree):
        if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)) and node.body:
            first = node.body[0]
            if isinstance(first, ast.Expr) and isinstance(first.value, ast.Constant):
                docstrings.add(id(first.value))
    subprocess_run, printed, scrubs, findings = False, False, False, []
    for node in ast.walk(tree):
        if isinstance(node, (ast.List, ast.Tuple)) and len(node.elts) >= 2 and all(
                isinstance(e, ast.Constant) for e in node.elts[:2]) and \
                [e.value for e in node.elts[:2]] == ["dotnet", "test"]:
            subprocess_run = True
        elif isinstance(node, ast.Call):
            f = node.func
            scrubs |= (isinstance(f, ast.Name) and f.id == "scrubbed_env") or (
                isinstance(f, ast.Attribute) and f.attr == "scrubbed_env")
        elif isinstance(node, ast.Constant) and isinstance(node.value, str) and id(node) not in docstrings:
            for line in node.value.splitlines():
                printed |= bool(_SCRUBBED_RUN.search(line))
                if _COMMAND_POSITION.search(line):
                    printed = True
                    findings.append(f"{rel}:{node.lineno}: prints an unscrubbed `dotnet test` instruction: "
                                    f"{line.strip()[:120]}")
    if subprocess_run and not scrubs:
        findings.append(f"{rel}: runs `dotnet test` without `env=scrubbed_env()`")
    return subprocess_run or printed, findings


def audit_callers(root: Path = REPO) -> tuple[list[str], list[str]]:
    """(the sites that run `dotnet test`, the offenders). Scope: every script under `scripts/` except the hooks
    (they receive commands as data and run no test) and this mechanism; every workflow under `.github/workflows/`."""
    sites, offenders = [], []
    for path in sorted((root / "scripts").rglob("*")):
        rel = path.relative_to(root).as_posix()
        if not path.is_file() or "__pycache__" in rel or rel.startswith("scripts/hooks/") or rel == MECHANISM \
                or path.suffix in _PROSE_SUFFIXES:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        if path.suffix not in (".sh", ".ps1", ".py"):
            # FAIL CLOSED: a runner this audit cannot parse is named, never skipped as clean.
            if _ANY_DOTNET_TEST.search(text):
                sites.append(rel)
                offenders.append(f"{rel}: mentions `dotnet test` in a file type this audit cannot check "
                                 f"({path.suffix or 'no extension'}) — make it a .sh, .ps1 or .py script")
            continue
        if path.suffix == ".py":
            runs, findings = _python_findings(text, rel)
            if runs:
                sites.append(rel)
            offenders += findings
            continue
        code = _code_lines(text)
        if any(_ANY_DOTNET_TEST.search(line) for _, line in code):
            sites.append(rel)
        offenders += [f"{rel}:{i}: runs `dotnet test` unscrubbed: {line.strip()[:120]}"
                      for i, line in code if _COMMAND_POSITION.search(line)]
    wf_dir = root / ".github" / "workflows"
    for path in sorted([*wf_dir.glob("*.yml"), *wf_dir.glob("*.yaml")]) if wf_dir.is_dir() else []:
        rel = path.relative_to(root).as_posix()
        code = _code_lines(path.read_text(encoding="utf-8", errors="replace"))
        if any(_ANY_DOTNET_TEST.search(line) for _, line in code):
            sites.append(rel)
        # The shard variable divides a run only where the same workflow checks the shards' union (`ci-gate`).
        checks_union = any(_CHECKS_UNION.search(line) for _, line in code)
        offenders += [f"{rel}:{i}: sets `{m.group(1)}`, a variable that narrows a `dotnet test` run"
                      + (", and runs no `test_population.py check` over its shards' trx" if m.group(1).upper() == SHARD else "")
                      for i, line in code for m in _ASSIGNED.finditer(line)
                      if is_scrubbed(m.group(1)) and not (m.group(1).upper() == SHARD and checks_union)]
    return sites, offenders


# ── the self-test ─────────────────────────────────────────────────────────────────────────────────────────────


def synthetic_trx(path: Path, cases: list[tuple[str, list[str]]], stray_results: list[tuple[str, str, str]] = ()) -> str:
    """A minimal trx: each case is (definition name, [result outcomes]), its test method's `className` the name up to
    its last `.` before any `(`; a stray result is (definition name it belongs to, the RESULT's own name, outcome) —
    the shape of a theory xunit could not serialize."""
    from xml.sax.saxutils import quoteattr
    ids = {}
    defs, results = [], []
    for i, (name, outs) in enumerate(cases):
        tid = f"00000000-0000-0000-0000-{i:012d}"
        ids.setdefault(name, tid)
        cls = name.partition("(")[0].rpartition(".")[0]
        defs.append(f"<UnitTest name={quoteattr(name)} id=\"{tid}\"><TestMethod className={quoteattr(cls)} /></UnitTest>")
        results += [f"<UnitTestResult testId=\"{tid}\" testName={quoteattr(name)} outcome=\"{o}\" />" for o in outs]
    for owner, result_name, outcome in stray_results:
        results.append(f"<UnitTestResult testId=\"{ids[owner]}\" testName={quoteattr(result_name)} "
                       f"outcome=\"{outcome}\" />")
    path.write_text("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                    "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">"
                    f"<Results>{''.join(results)}</Results><TestDefinitions>{''.join(defs)}</TestDefinitions>"
                    "</TestRun>\n", encoding="utf-8", newline="\n")
    return str(path)


def synthetic_listing(path: Path, names: list[str]) -> str:
    path.write_text("Test run for X.dll (.NETCoreApp,Version=v10.0)\n" + LISTED_MARK + "\n"
                    + "".join(f"    {n}\n" for n in names), encoding="utf-8", newline="\n")
    return str(path)


def self_test() -> int:
    from scratch_repo import remove_tree  # noqa: PLC0415  (scripts/: the one scratch fixture)
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    def rc_of(fn) -> tuple[int, str]:
        try:
            return (RC_EXACT, "") if fn() else (RC_RED, "")
        except PopulationError as e:
            return RC_ERROR, str(e)

    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp)
        trunc = "N.T.Theory(text: \"aaaa\"···"
        population = ["N.A.One", "N.A.Two", "N.B.Three", trunc, trunc, "N.C.Skipped"]
        listing = synthetic_listing(d / "all.list.txt", population)
        listed = read_listings([listing])
        ok = lambda outs=("Passed",): list(outs)  # noqa: E731
        full = [(n, ok()) for n in population[:-1]] + [("N.C.Skipped", ["NotExecuted"])]

        p = check("exact", listed, [synthetic_trx(d / "exact.trx", full)])
        arm("exact population, a truncated name listed twice, one skip", p.exact and p.discovered == 6
            and p.ran == 5 and p.skipped == 1 and "EXACT — 5/6 cases ran (skipped 1)" in p.verdict_line(),
            p.verdict_line())

        p = check("short", listed, [synthetic_trx(d / "short.trx", [c for c in full if c[0] != "N.A.Two"])])
        arm("short: a dropped case is NEVER RAN, named", not p.exact and p.never_ran == Counter({"N.A.Two": 1})
            and any("NEVER RAN" in ln and "N.A.Two" in ln for ln in p.report()[1:]), p.verdict_line())

        p = check("truncated", listed, [synthetic_trx(d / "trunc.trx", [c for c in full if c[0] != trunc] + [(trunc, ok())])])
        arm("short by multiplicity: one of two rows sharing a truncated name dropped",
            p.never_ran == Counter({trunc: 1}), p.verdict_line())

        p = check("over", listed, [synthetic_trx(d / "over.trx", full + [("N.B.Three", ok())])])
        arm("over: a case twice is RAN TWICE, named", p.ran_twice == Counter({"N.B.Three": 1}) and not p.never_ran
            and "1 RAN TWICE" in p.verdict_line(), p.verdict_line())

        shard1 = synthetic_trx(d / "shard1.trx", [c for c in full if c[0] in ("N.A.One", "N.A.Two", trunc)])
        shard2 = synthetic_trx(d / "shard2.trx", [c for c in full if c[0] in ("N.A.Two", "N.C.Skipped")])
        executed = sum(read_trx(shard1).definitions.values()) + sum(read_trx(shard2).definitions.values())
        p = check("overlap", listed, [shard1, shard2])
        arm("shard overlap that KEEPS THE COUNT (a count guard passes it; this is red)",
            executed == sum(listed.values()) and p.ran_twice == Counter({"N.A.Two": 1})
            and p.never_ran == Counter({"N.B.Three": 1}), f"counted {executed}; {p.verdict_line()}")

        methods = read_listings([synthetic_listing(d / "methods.list.txt",
                                                   ["N.G_P0.Golden(p: 1)", "N.G_P1.Golden(p: 2)", "N.G_P1.Golden",
                                                    "N.G_P1.NotGolden(p: \"x.Golden(\")", "N.H.Golden_Other"])])
        golden = of_method(methods, "Golden")
        arm("--method keeps exactly the listed cases of that method, partition classes included (kb/Work PB4329: "
            "INV-1-strong's filtered shards)", sorted(golden) == ["N.G_P0.Golden(p: 1)", "N.G_P1.Golden",
                                                                 "N.G_P1.Golden(p: 2)"], str(sorted(golden)))
        rc, why = rc_of(lambda: check("none", of_method(methods, "Absent"), [synthetic_trx(d / "m.trx", [])]).exact)
        arm("--method naming no listed method is an EMPTY population: an ERROR, never clean", rc == RC_ERROR
            and "EMPTY" in why, why)

        p = check("skip", listed, [synthetic_trx(d / "skip.trx", full)])
        arm("skipped: a NotExecuted-only definition is counted skipped, never ran", p.skipped == 1 and p.ran == 5)

        stray = [("N.A.One", "N.A.One(row: 1)", "Passed"), ("N.A.One", "N.A.One(row: 2)", "Passed")]
        p = check("defs", listed, [synthetic_trx(d / "defs.trx", full, stray)])
        arm("definitions, not result names: an unserializable theory's per-row results are not cases",
            p.exact, p.verdict_line())

        p = check("foreign", listed, [synthetic_trx(d / "foreign.trx", full + [("Other.Assembly.Test", ok())])])
        arm("a definition the listing never printed is NOT IN THE POPULATION", not p.exact
            and p.not_listed == Counter({"Other.Assembly.Test": 1}), p.verdict_line())

        p = check("early", listed, [synthetic_trx(d / "early.trx", full[:2])], stopped_early=True)
        arm("stopped early: the remainder is NOT RUN and the verdict RED/INCOMPLETE", not p.exact
            and "RED/INCOMPLETE" in p.verdict_line() and "NOT RUN" in p.verdict_line() and not p.defective,
            p.verdict_line())
        p = check("early-foreign", listed, [synthetic_trx(d / "early-foreign.trx", full[:2] + [("Other.T", ok())])],
                  stopped_early=True)
        arm("stopped early, a name the listing never printed is still a DEFECT (kb/Work PB4274: a leg-1-only gate "
            "printed it and stayed GREEN)", p.defective and p.not_listed == Counter({"Other.T": 1}), p.verdict_line())

        # ONE SPELLING (kb/Work PB4274): the listing prints xunit's display name, the trx logger rewrites every UTF-16
        # code unit outside XML's range as a backslash-u escape. Each trx name below is spelled as that logger writes it.
        clef, adlam = "\U0001D15E", "\U0001E900"
        uni = [f'N.U.T(text: "{clef}")', f'N.U.T(text: "a{adlam} ")', 'N.U.T(text: "x\\xdc00y")',
               f'N.U.T(text: "z{chr(0xFFFF)}")', 'N.U.T(text: "\\\\ud834\\\\udd5e")']
        as_trx = ['N.U.T(text: "\\ud834\\udd5e")', 'N.U.T(text: "a\\ud83a\\udd00 ")', 'N.U.T(text: "x\\xdc00y")',
                  'N.U.T(text: "z\\uffff")', 'N.U.T(text: "\\\\ud834\\\\udd5e")']
        uni_listed = read_listings([synthetic_listing(d / "uni.list.txt", uni)])
        arm("trx_spelling is the trx logger's rewrite: a supplementary character is its two surrogates, U+FFFF is "
            "escaped, a lone surrogate xunit already wrote as \\xdc00 and a doubled backslash are untouched, and it is "
            "the identity on its own output", [trx_spelling(n) for n in uni] == as_trx
            and [trx_spelling(n) for n in as_trx] == as_trx, str([ascii(trx_spelling(n)) for n in uni]))
        p = check("uni", uni_listed, [synthetic_trx(d / "uni.trx", [(n, ok()) for n in as_trx])])
        arm("a supplementary pair, a lone surrogate, U+FFFF and the literal escape text each meet their own trx name: "
            "EXACT", p.exact and p.discovered == 5, "\n        ".join(p.report()))
        p = check("uni-short", uni_listed, [synthetic_trx(d / "uni-short.trx", [(n, ok()) for n in as_trx[1:]])])
        arm("the literal text \\\\ud834\\\\udd5e that ran does not stand in for the character that did not: NEVER RAN, "
            "named in the trx spelling", p.never_ran == Counter({as_trx[0]: 1}) and not p.not_listed,
            "\n        ".join(p.report()))
        clash = synthetic_listing(d / "clash.list.txt", [f'N.U.T(text: "{clef}")', 'N.U.T(text: "\\ud834\\udd5e")'])
        rc, why = rc_of(lambda: check("clash", read_listings([clash]), [synthetic_trx(d / "clash.trx", [])]).exact)
        arm("two listed names the logger writes alike (the character, and that text unescaped by a display name) are "
            "an ERROR naming both, never one merged count", rc == RC_ERROR and "two listed names" in why, why)

        other = synthetic_listing(d / "other.list.txt", population[:-1])
        rc, why = rc_of(lambda: read_listings([listing, other]))
        arm("two listings that disagree are an ERROR", rc == RC_ERROR and "LISTINGS DISAGREE" in why, why)
        rc, why = rc_of(lambda: check("gone", listed, [str(d / "absent.trx")]).exact)
        arm("a missing trx is an ERROR, never a finding", rc == RC_ERROR, why)
        (d / "bad.trx").write_text("<not-a-trx/>", encoding="utf-8", newline="\n")
        rc, why = rc_of(lambda: check("bad", listed, [str(d / "bad.trx")]).exact)
        arm("a file that is not a trx is an ERROR", rc == RC_ERROR, why)
        (d / "torn.trx").write_text("<TestRun><Results><UnitTestResult outcome=\"" + chr(1) + "\"/>", encoding="utf-8",
                                    newline="\n")
        try:
            trx_root(d / "torn.trx")
            torn = None
        except UnreadableTrx as e:
            torn = e
        arm("a torn trx is an UnreadableTrx that names the file and carries its path (kb/Work PB3661), the one reader "
            "every trx consumer shares", torn is not None and torn.path == d / "torn.trx" and str(d / "torn.trx") in str(torn),
            str(torn))
        (d / "nomark.txt").write_text("No test is available in X.dll\n", encoding="utf-8", newline="\n")
        rc, why = rc_of(lambda: read_listings([str(d / "nomark.txt")]))
        arm("a listing without the vstest marker is an ERROR", rc == RC_ERROR, why)
        rc, why = rc_of(lambda: check("empty", read_listings([synthetic_listing(d / "e.txt", [])]),
                                      [synthetic_trx(d / "e.trx", [])]).exact)
        arm("an EMPTY population is an ERROR, never clean", rc == RC_ERROR and "EMPTY" in why, why)
        body = parse_listing(LISTED_MARK + "\n    A.B\n     \n  not a case\n    C.D  \n", "inline")
        arm("the listing parser keeps 4-space-indented names verbatim", body == Counter({"A.B": 1, "C.D  ": 1}),
            str(body))

        scrubbed = ["WISEOWL_COBOL_GATE_FIRST", "wiseowl_cobol_gate_first", "WISEOWL_COBOL_GATE_ANYTHING", "VSTestTestCaseFilter",
                    "VSTestSetting", "vstest_host_debug", "RunSettingsFilePath"]
        kept = ["WISEOWL_COBOL_COMPILE_CACHE", "PATH", "RUNSETTINGS", "WISEOWL_COBOL_GATE"]
        scrubbed.append("wiseowl_cobol_shard")
        kept.append("WISEOWL_COBOL_SHARDS")
        arm("the scrub rule names the gate's channel (WISEOWL_COBOL_GATE_*), VSTest*, RunSettingsFilePath and CI's shard "
            "variable (any case), nothing else", all(map(is_scrubbed, scrubbed)) and not any(map(is_scrubbed, kept))
            and is_scrubbed(SHARD))
        planted = {**os.environ, "WISEOWL_COBOL_GATE_LEG": "1", "VSTestTestCaseFilter": "X~Y", "W71D_KEEP": "kept"}
        probe = subprocess.run([sys.executable, __file__, "scrubbed", sys.executable, "-c",
                                "import os; print(sorted(k for k in os.environ if k.upper() in "
                                "('WISEOWL_COBOL_GATE_LEG', 'VSTESTTESTCASEFILTER', 'W71D_KEEP')))"],
                               env=planted, capture_output=True, text=True)
        arm("`scrubbed <command>` runs the command without a planted gate variable or VSTest variable",
            probe.returncode == 0 and probe.stdout.strip() == "['W71D_KEEP']", probe.stdout + probe.stderr)

        r = d / "repo"
        files = {
            "scripts/bad.sh": "dotnet test tests/X --no-build\n",
            "scripts/good.sh": "\"$PY\" scripts/test_population.py scrubbed dotnet test tests/X --no-build\n"
                               "# dotnet test in a comment is not a run\n",
            "scripts/bad.ps1": "& dotnet test @testArgs *> $log\n",
            "scripts/good.ps1": "& python scripts/test_population.py scrubbed dotnet test @testArgs *> $log\n",
            "scripts/bad.py": "import subprocess\nsubprocess.run([\"dotnet\", \"test\", \"x\"])\n",
            "scripts/good.py": "from test_population import scrubbed_env\nimport subprocess\n"
                               "subprocess.run([\"dotnet\", \"test\", \"x\"], env=scrubbed_env())\n",
            "scripts/prose.py": "\"\"\"Runs one `dotnet test` leg.\n\ndotnet test tests/X\n\"\"\"\n"
                                "HELP = 'the leg\\'s complete `dotnet test` output'\n",
            "scripts/instruction.py": "GATE = f'    dotnet test {1} --no-build'\n",
            "scripts/good_instruction.py": "GATE = f'    python scripts/test_population.py scrubbed dotnet test {1}'\n",
            "scripts/hooks/hook.sh": "dotnet test tests/X\n",
            "scripts/runner.cmd": "dotnet test tests/X\n",
            "scripts/NOTES.md": "run dotnet test tests/X by hand\n",
            ".github/workflows/bad.yml": "jobs:\n  a:\n    env:\n      VSTestTestCaseFilter: x\n    steps:\n"
                                        "      - run: dotnet test tests/X\n"
                                        "      - run: echo \"WISEOWL_COBOL_GATE_LEG=1\" >> $GITHUB_ENV\n",
            ".github/workflows/good.yml": "jobs:\n  a:\n    env:\n      WISEOWL_COBOL_NIST_STD: '2023'\n    steps:\n"
                                         "      - run: dotnet test tests/X\n",
            # CI's shards (kb/Work PB4329): set where the workflow checks their union, a narrowing everywhere else.
            ".github/workflows/sharded.yml": "jobs:\n  a:\n    env:\n      WISEOWL_COBOL_SHARD: 1/2\n    steps:\n"
                                            "      - run: dotnet test tests/X\n  gate:\n    steps:\n"
                                            "      - run: python scripts/test_population.py check --label X \\\n"
                                            "          --listing l.txt --trx s-*/X.trx\n",
            ".github/workflows/unchecked.yml": "jobs:\n  a:\n    env:\n      WISEOWL_COBOL_SHARD: 1/2\n    steps:\n"
                                              "      - run: dotnet test tests/X\n"
                                              "      # python scripts/test_population.py check in a comment checks nothing\n",
        }
        for rel, text in files.items():
            (r / rel).parent.mkdir(parents=True, exist_ok=True)
            (r / rel).write_text(text, encoding="utf-8", newline="\n")
        sites, offenders = audit_callers(r)
        flagged = {o.split(":", 1)[0] for o in offenders}
        want = {"scripts/bad.sh", "scripts/bad.ps1", "scripts/bad.py", "scripts/instruction.py",
                "scripts/runner.cmd", ".github/workflows/bad.yml", ".github/workflows/unchecked.yml"}
        arm("audit-callers flags every unscrubbed shape and passes every scrubbed one (an unparsable runner type "
            "fails closed, a Markdown note is prose)", flagged == want,
            f"flagged {sorted(flagged)}")
        arm("audit-callers: a docstring or a help string is prose, a hook is out of scope, a comment is not a run",
            "scripts/prose.py" not in sites and "scripts/hooks/hook.sh" not in sites and "scripts/good.sh" in sites
            and "scripts/good_instruction.py" in sites and "scripts/good.py" in sites
            and sum(1 for o in offenders if o.startswith(".github/workflows/bad.yml")) == 2, f"sites {sites}")
        remove_tree(r)

        # THE RED CLASSES (kb/Work PB4328): the classes of the definitions with a Failed result, read through trx_root.
        reds = d / "reds"
        reds.mkdir()
        red_files = [synthetic_trx(reds / "a.trx", [("N.A.One", ["Passed"]), ("N.A.Two", ["Failed"]),
                                                    ("N.B.T(x: \"a.b\")", ["Failed"])]),
                     synthetic_trx(reds / "b.trx", [("N.C.One", ["NotExecuted"]), ("N.D.One", ["Passed", "Failed"])])]
        arm("red_classes: the class of every definition with a Failed result, across files; none for a pass or a skip",
            red_classes(red_files) == {"N.A", "N.B", "N.D"} and red_classes([]) == set(), str(red_classes(red_files)))
        (reds / "torn.trx").write_text("<TestRun", encoding="utf-8", newline="\n")
        rc, why = rc_of(lambda: red_classes([reds / "torn.trx"]))
        arm("red_classes: an unreadable trx raises UnreadableTrx naming the file", rc == RC_ERROR and "torn.trx" in why, why)
        arm("the XML range is stated once, as escapes: the kept and the rewritten characters are its two sides",
            all(ord(ch) < 0x80 for ch in XML_RANGE) and _IS_XML.match("\ud7ff") and _IS_XML.match("\ue000")
            and _IS_XML.match("\ufffd") and not _IS_XML.match("\uffff") and _NOT_XML.search("\uffff")
            and _NOT_XML.search("\U0001d15e") and not _NOT_XML.search("\t\n\r ~"), XML_RANGE)

    for name, passed, detail in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {name}" + ("" if passed or not detail else f"\n        {detail}"))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== test_population SELF-TEST: {'PASS' if failed == 0 else f'FAIL ({failed})'} "
          f"({len(results)} arms) ===")
    return RC_EXACT if failed == 0 else RC_RED


# ── the command line ──────────────────────────────────────────────────────────────────────────────────────────


def main(argv: list[str]) -> int:
    if argv[:1] == ["scrubbed"]:
        if len(argv) < 2:
            print("test_population.py scrubbed: no command given", file=sys.stderr)
            return RC_ERROR
        # Not exec: on Windows os.exec* does not replace the process, and the caller waits on OUR exit code.
        return subprocess.run(argv[1:], env=scrubbed_env()).returncode
    if argv[:1] == ["--self-test"]:
        return self_test()

    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check", help="assert the trx files' union is exactly the discovered population")
    c.add_argument("--label", required=True)
    source = c.add_mutually_exclusive_group(required=True)
    source.add_argument("--project", help="list the population now, scrubbed (the project must be built)")
    source.add_argument("--listing", nargs="+", help="saved `--list-tests` output(s) — several must agree")
    c.add_argument("--configuration", help="with --project: the configuration that was built")
    c.add_argument("--save-listing", help="with --project: also write the raw listing here")
    c.add_argument("--trx", nargs="+", required=True)
    c.add_argument("--stopped-early", action="store_true", help="the run stopped at a red: short = NOT RUN")
    c.add_argument("--method", help="check only the listed cases of this test method (a run filtered to it)")
    ls = sub.add_parser("list", help="write a project's scrubbed `--list-tests` output to a file")
    ls.add_argument("--project", required=True)
    ls.add_argument("--configuration")
    ls.add_argument("--out", required=True)
    sub.add_parser("audit-callers", help="every `dotnet test` caller scrubs (GateLegDriftTests arm 6)")
    sub.add_parser("audit-trx-writers", help="every live tool that writes a trx file has its TRX_WRITERS row (PB3155)")
    sub.add_parser("audit-trx-readers", help="every live tool reads a trx file through trx_root (kb/Work PB3661)")
    args = ap.parse_args(argv)

    try:
        if args.cmd == "list":
            names, raw = list_population(args.project, args.configuration)
            Path(args.out).write_text(raw, encoding="utf-8", newline="\n")
            print(f"test_population: {sum(names.values()):,} cases listed for {args.project} -> {args.out}")
            return RC_EXACT
        if args.cmd == "audit-trx-writers":
            seen, bad = audit_trx_writers(REPO)
            print("\n".join([*seen, *bad, f"=== TRX WRITER AUDIT: {'CLEAN' if not bad else f'{len(bad)} OFFENDER(S)'} ==="]))
            return RC_EXACT if not bad else RC_RED
        if args.cmd == "audit-trx-readers":
            seen, bad = audit_trx_readers()
            print("\n".join([*seen, *bad, f"=== TRX READER AUDIT: {'CLEAN' if not bad else f'{len(bad)} OFFENDER(S)'} ==="]))
            return RC_EXACT if not bad else RC_RED
        if args.cmd == "audit-callers":
            sites, offenders = audit_callers()
            for s in sites:
                print(f"  site  {s}")
            for o in offenders:
                print(f"  ⛔ {o}")
            if not sites:
                print("=== CALLER AUDIT: ERROR — found no `dotnet test` caller at all; the scan is broken ===")
                return RC_ERROR
            print(f"=== CALLER AUDIT: {'CLEAN' if not offenders else f'{len(offenders)} UNSCRUBBED'} — "
                  f"{len(sites)} site(s) run `dotnet test` ===")
            return RC_EXACT if not offenders else RC_RED
        if args.project:
            listed, raw = list_population(args.project, args.configuration)
            if args.save_listing:
                Path(args.save_listing).write_text(raw, encoding="utf-8", newline="\n")
        else:
            if args.configuration or args.save_listing:
                ap.error("--configuration and --save-listing go with --project")
            listed = read_listings(args.listing)
        if args.method:
            listed = of_method(listed, args.method)
        result = check(args.label, listed, args.trx, args.stopped_early)
    except PopulationError as e:
        print(f"=== POPULATION {getattr(args, 'label', '')}: ERROR — the check could not run: {e} ===")
        return RC_ERROR
    print("\n".join(result.report()))
    return RC_EXACT if result.exact else RC_RED


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
