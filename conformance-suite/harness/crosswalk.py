#!/usr/bin/env python3
"""The NIST-85 crosswalk: each CCVS85 unit and its mechanical map to the 1985 catalog's rows (DESIGN-conformance-suite.md
section 6.3, kb/Work PB6049).

    python conformance-suite/harness/crosswalk.py             # the map's counts, one verdict line
    python conformance-suite/harness/crosswalk.py --build     # write tests/nist/crosswalk/<program>.json for every program
    python conformance-suite/harness/crosswalk.py --check     # the audit: every file agrees with its program and the catalog

THE UNIT is NIST's own test (section 6.3): one paragraph-name test (a distinct `MOVE "<name>" TO PAR-NAME` literal of a
program, blanks trimmed) and one expected flag (a `Message expected for above statement:` comment) of the flagging
programs. A paragraph move the tool cannot read as a literal (a table-driven name) is listed `unenumerated` and owed,
never dropped. The 17 FIPS programs (the ones whose flags expect NON-CONFORMING STANDARD, decision 11) hold only flags,
every one `excluded` with the reason `fips-pub-21-1`.

THE MAP is mechanical and says so (`basis: "mechanical"`): NIST's references seed it and never decide it (section 6.3).
A unit's `ANSI-REFERENCE` is the last one its program moved before the unit's `PAR-NAME` move, read as page and section
tokens (`VI-106 6.19.4 GR1`) and resolved to the catalog's rows by the section's locator (`VI.6.19.4`). NIST's numbering
is not the held text's after the ENTER statement (a MULTIPLY unit cites 6.19.4, which the held text gives to MOVE), so a
reference is believed only when it also agrees with the unit's own words: the FEATURE and paragraph-name words must
begin or continue a word of the cited section's subject. A unit is

    rows         every cited section resolves to the rule it names, and the section's subject agrees with the unit: its rows
                 (a format-keyed name `MPY-TEST-F1-n` adds the statement's FMT row)
    open         anything else, `resolution` `ambiguous` (candidate rows from the literal reference or from the one section
                 whose subject names the unit) or `unmapped` (none); Stage 4 (PB4696) adjudicates it with a refuter
    excluded     with a reason, a FIPS flag

A flag unit maps through Appendix C.1: the catalog marks each obsolete row `obsolete: C.1(n)`, and the flagged statement
names the element n. An element with one row maps to it; one with several is `ambiguous`.

`unmet(edition)` is the NIST term of `suite_status.py`: the units neither dispositioned nor subsumed (`open`, an
`unenumerated` site, and a `rows` unit with a row no cleared test, determination or derivation witnesses: row level
only, since the coverage points are PB6050's).
"""
from __future__ import annotations

import argparse
import bisect
import json
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
REPO = SUITE.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

VERDICT = "=== CROSSWALK 85: "
NIST = REPO / "tests" / "nist"
PROGRAMS = NIST / "programs"
CROSSWALK = NIST / "crosswalk"
EDITION = "85"
BASIS = "mechanical"
#: the reasons a unit may be excluded (design 6.3); a reason this table lacks is refused
EXCLUSIONS = {"fips-pub-21-1": "FIPS PUB 21-1 subset flagging (decision 11): a US federal layer with no ISO text",
              "ccvs-scaffolding": "CCVS's own machinery"}
#: the rows a `rows` unit is subsumed by: a cleared test, a determination, or a standing derivation settles the row
MET = frozenset({"suite", "determination", "derivation"})
DISPOSITIONS = ("rows", "open", "excluded")
RESOLUTIONS = ("ambiguous", "unmapped")


class CrosswalkError(Exception):
    """An input the tool cannot resolve: a count over it would be no measure, so it fails loudly."""


# ── the program text ─────────────────────────────────────────────────────────────────────────────────────────────────

_SENTENCE_FLAG = re.compile(r"message expected for (?:above|following) statement:\s*(?P<kind>OBSOLETE|NON-CONFORMING)", re.I)
_PAR_LITERAL = re.compile(r'MOVE\s+"(?P<name>[^"]+)"\s+TO\s+(?:FEATURE\s*,\s*)?PAR-NAME\b', re.I)
_PAR_OTHER = re.compile(r"MOVE\s+(?P<src>[^\s\"]+(?:\s*\([^)]*\))?)\s+TO\s+PAR-NAME\b", re.I)
_FEATURE = re.compile(r'MOVE\s+"(?P<text>[^"]*)"\s+TO\s+FEATURE\b', re.I)
_REFERENCE = re.compile(r'MOVE\s+"(?P<text>[^"]*)"\s+TO\s+ANSI-REFERENCE\b', re.I)
#: CCVS's own reset of the name (`IF REC-CT EQUAL TO ZERO MOVE SPACE TO PAR-NAME`): no unit
_RESETS = frozenset({"SPACE", "SPACES"})
_FORMAT_KEYED = re.compile(r"-F(?P<format>\d+)(?:-|$)")


@dataclass(frozen=True)
class SourceLine:
    number: int
    area: str       # columns 8-72: the text a compiler reads
    comment: bool


def source_lines(text: str) -> list[SourceLine]:
    """The fixed-form lines of a CCVS program: sequence area dropped, columns 8-72 kept, a `*` or `/` in column 7 a comment."""
    out = []
    for n, raw in enumerate(text.splitlines(), 1):
        indicator = raw[6] if len(raw) > 6 else " "
        out.append(SourceLine(n, raw[7:72] if len(raw) > 7 else "", indicator in "*/"))
    return out


@dataclass
class Program:
    name: str
    #: name -> (lines, feature, ansi-reference or None); in first-appearance order
    units: dict[str, tuple[list[int], str, str | None]] = field(default_factory=dict)
    #: (line, kind, flagged statement) in file order
    flags: list[tuple[int, str, str]] = field(default_factory=list)
    #: (line, text) of a PAR-NAME move that names no literal
    unenumerated: list[tuple[int, str]] = field(default_factory=list)


def parse_program(name: str, text: str) -> Program:
    lines = source_lines(text)
    code = [ln for ln in lines if not ln.comment]
    body = "\n".join(ln.area for ln in code)
    starts, pos = [], 0
    for ln in code:
        starts.append(pos)
        pos += len(ln.area) + 1

    def line_at(offset: int) -> int:
        return code[bisect.bisect_right(starts, offset) - 1].number

    events = []
    for kind, pattern in (("feature", _FEATURE), ("reference", _REFERENCE), ("unit", _PAR_LITERAL), ("other", _PAR_OTHER)):
        events += [(m.start(), kind, m) for m in pattern.finditer(body)]
    events.sort(key=lambda e: e[0])
    prog = Program(name)
    feature, reference = "", None
    for offset, kind, m in events:
        if kind == "feature":
            feature = m.group("text").strip()
        elif kind == "reference":
            reference = m.group("text").strip() or None
        elif kind == "unit":
            unit = m.group("name").strip()
            if unit in prog.units:
                prog.units[unit][0].append(line_at(offset))
            else:
                prog.units[unit] = ([line_at(offset)], feature, reference)
        elif m.group("src").upper() not in _RESETS:
            prog.unenumerated.append((line_at(offset), re.sub(r"\s+", " ", m.group(0))))
    statement: list[str] = []
    for ln in lines:
        if not ln.comment:
            if ln.area.strip():
                statement.append(ln.area.strip())
            continue
        f = _SENTENCE_FLAG.search(ln.area)
        if f:
            prog.flags.append((ln.number, f.group("kind").upper(), flagged_statement(statement)))
            statement = []
    return prog


def flagged_statement(code: list[str]) -> str:
    """The statement a flag comment follows: the last two sentences of the code since the previous flag."""
    sentences = [s.strip() for s in " ".join(code).split(".") if s.strip()]
    return " ".join(sentences[-2:]).upper()


# ── the ANSI-REFERENCE ───────────────────────────────────────────────────────────────────────────────────────────────

_REF_TOKEN = re.compile(r"""
    (?P<page>(?P<rom>[IVX1]+|[A-C])-(?P<pg>\d+)(?:/\d+)?)
  | (?P<sec>\d+(?:\.\d+)+)
  | (?P<gr>GR\s*(?P<grn>\d+)(?:\s*-\s*(?P<grm>\d+))?)
  | (?P<par>\((?P<parn>\d+)\))
  | (?P<alt>[/+](?P<altn>\d+))
    """, re.X)


@dataclass
class Cite:
    roman: str
    page: int | None = None
    section: str | None = None
    #: rule numbers the reference names (GR1, (5)); the kind comes from the section's last component
    rules: list[int] = field(default_factory=list)

    @property
    def locator(self) -> str | None:
        return f"{self.roman}.{self.section}" if self.section else None


def parse_reference(text: str) -> list[Cite]:
    """The page and section tokens of a NIST reference, in order. NIST's typing is repaired where a digit stands for a
    letter of a Roman numeral (`V1-89`, `1V-41`); a token the grammar does not know is skipped, never guessed."""
    cites: list[Cite] = []
    for m in _REF_TOKEN.finditer(text.upper()):
        if m.group("page"):
            cites.append(Cite(m.group("rom").replace("1", "I"), int(m.group("pg"))))
        elif m.group("sec"):
            if cites and cites[-1].section is None:
                cites[-1].section = m.group("sec")
            else:
                cites.append(Cite(cites[-1].roman if cites else "", None, m.group("sec")))
        elif not cites:
            continue
        elif m.group("gr"):
            cites[-1].rules += range(int(m.group("grn")), int(m.group("grm") or m.group("grn")) + 1)
        elif m.group("par"):
            cites[-1].rules.append(int(m.group("parn")))
        elif m.group("alt") and cites[-1].section:
            head, _, _ = cites[-1].section.rpartition(".")
            cites.append(Cite(cites[-1].roman, None, f"{head}.{m.group('altn')}"))
    return [c for c in cites if c.roman]


# ── the catalog ──────────────────────────────────────────────────────────────────────────────────────────────────────

_C1_ITEM = re.compile(r"C\.1\((\d+)\)")
#: a row of an unnumbered paragraph (`1985-GR-VI.6.4.4-p1`): its ordinal is its place, never a rule's number
_PARAGRAPH_ROW = re.compile(r"-p\d+$")
_STOP = frozenset("THE STATEMENT CLAUSE PARAGRAPH ENTRY MODULE IN OF AND DIVISION SECTION TO FOR INTRODUCTION COMMON "
                  "OPTIONS RULES STATEMENTS LANGUAGE CONCEPTS TEST INIT COMP LEVEL FORMAT".split())
#: the SPELLING of the element each Appendix C.1 flag names, in the order a flagged statement is tried
OBSOLETE_ELEMENTS = (
    (6, re.compile(r"\bMULTIPLE\s+FILE\b")), (4, re.compile(r"\bMEMORY\s+SIZE\b")),
    (7, re.compile(r"\bLABEL\s+RECORDS?\b")), (8, re.compile(r"\bVALUE\s+OF\b")), (9, re.compile(r"\bDATA\s+RECORDS?\b")),
    (10, re.compile(r"\bALTER\b")), (11, re.compile(r"\bDISABLE\b")), (12, re.compile(r"\bENABLE\b")),
    (13, re.compile(r"\bENTER\b")), (16, re.compile(r'\bSTOP\s+"')), (15, re.compile(r"\bREVERSED\b")),
    (14, re.compile(r"\bGO\s+TO\b")), (18, re.compile(r"\bUSE\s+FOR\s+DEBUGGING\b")),
    (17, re.compile(r"\bSEGMENT-LIMIT\b|\bSECTION\s+\d+\b")),
    (3, re.compile(r"\b(?:AUTHOR|INSTALLATION|DATE-WRITTEN|DATE-COMPILED|SECURITY)\b")),
    (5, re.compile(r"\bI-O-CONTROL\b")),
)
#: What Appendix C.1's heading of each item the table above names must say, read back from the transcription by
#: `audit_elements` (the table's item numbers are the catalog's `obsolete: C.1(n)` statuses, which the heading confirms)
C1_HEADINGS = {3: "AUTHOR, INSTALLATION", 4: "MEMORY SIZE", 5: "RERUN", 6: "MULTIPLE FILE TAPE", 7: "LABEL RECORDS",
               8: "VALUE OF", 9: "DATA RECORDS", 10: "ALTER", 11: "DISABLE", 12: "ENABLE", 13: "ENTER", 14: "GO TO",
               15: "REVERSED", 16: "STOP literal", 17: "Segmentation", 18: "Debug"}
SPEC_1985 = REPO / "specs" / "ANSI_COBOL_1985.md"
_C1_ITEM_HEADING = re.compile(r"^\((\d+)\) <u>(.+?)</u>", re.M)


class Catalog:
    """The rows of one edition, indexed the ways a reference reaches them."""

    def __init__(self, rows: Iterable[dict[str, Any]]):
        self.rows = {r["rule-id"]: r for r in rows}
        self.by_locator: dict[str, list[dict[str, Any]]] = {}
        self.by_folio: dict[str, list[dict[str, Any]]] = {}
        self.obsolete: dict[int, list[str]] = {}
        for r in self.rows.values():
            self.by_locator.setdefault(r["locator"], []).append(r)
            self.by_folio.setdefault(r.get("folio") or "", []).append(r)
            for item in _C1_ITEM.findall(r.get("status") or ""):
                self.obsolete.setdefault(int(item), []).append(r["rule-id"])
        self.subjects: dict[str, str] = {}
        for r in self.rows.values():
            self.subjects.setdefault(".".join(r["locator"].split(".")[:3]), r["subject"])

    def under(self, locator: str) -> list[dict[str, Any]]:
        """The rows of a locator, else of everything beneath it (a clause the catalog files by its subclauses)."""
        exact = self.by_locator.get(locator)
        if exact:
            return exact
        return [r for loc, rs in self.by_locator.items() if loc.startswith(locator + ".") for r in rs]


def words(text: str) -> set[str]:
    return {w for w in re.findall(r"[A-Z]{3,}", text.upper()) if w not in _STOP}


def agrees(unit_words: set[str], subject: str) -> bool:
    """Does a word of the unit begin or continue a word of the subject (SYNC, SYNCHRONIZED)? Four letters at least."""
    return any(len(u) >= 4 and len(s) >= 4 and (u.startswith(s) or s.startswith(u))
               for u in unit_words for s in words(subject))


def rule_rows(catalog: Catalog, cite: Cite) -> tuple[list[dict[str, Any]], bool]:
    """The rows a section cite names, and whether they are exactly the rules it names (a section alone is not exact)."""
    rows = catalog.under(cite.locator)
    if not cite.rules:
        return rows, len(rows) == 1
    kind = {"4": "GR", "3": "SR", "2": "FMT"}.get(cite.section.rsplit(".", 1)[-1])
    picked = [r for r in rows if r.get("ordinal") in cite.rules and kind in (None, r["kind"])
              and not _PARAGRAPH_ROW.search(r["rule-id"])]
    return (picked, True) if picked else (rows, False)


def resolve(catalog: Catalog, reference: str | None, unit_words: set[str]) -> dict[str, Any]:
    """Resolve a reference to {rows, exact, agrees, candidates}: the rows the literal reference names, whether every cite
    named its rules and found them, whether the cited sections' subjects agree with the unit's words, and (with no
    agreement) the one section whose subject names the unit, were there exactly one."""
    cites = parse_reference(reference or "")
    ids: list[str] = []
    exact = bool(cites)
    subjects: list[str] = []
    for cite in cites:
        if cite.section:
            rows, named = rule_rows(catalog, cite)
            exact &= named
        else:
            rows, exact = catalog.by_folio.get(f"{cite.roman}-{cite.page}", []), False
        for r in rows:
            if r["rule-id"] not in ids:
                ids.append(r["rule-id"])
            subjects.append(r["subject"])
    if exact and not ids:
        exact = False
    return {"ids": ids, "exact": exact, "agrees": any(agrees(unit_words, s) for s in subjects), "cites": cites}


def by_name(catalog: Catalog, cites: list[Cite], unit_words: set[str]) -> list[str]:
    """The rows of the one section, in a cited chapter, whose subject names the unit, the cite's rule numbers applied to
    it: NIST's numbering is one section short after ENTER, so the section it meant is the one the unit's words name."""
    found: dict[str, str] = {}
    for cite in cites:
        if not cite.section:
            continue
        for locator, subject in catalog.subjects.items():
            if locator.startswith(cite.roman + ".") and agrees(unit_words, subject):
                found[locator] = subject
    if len(found) != 1:
        return []
    locator = next(iter(found))
    out: list[str] = []
    for cite in cites:
        if cite.section and cite.section.count(".") >= 2:
            tail = cite.section.rsplit(".", 1)[-1]
            rows = catalog.under(f"{locator}.{tail}")
            picked = [r for r in rows if r.get("ordinal") in cite.rules] if cite.rules else []
            out += [r["rule-id"] for r in (picked or rows) if r["rule-id"] not in out]
    return out


# ── one program's file ───────────────────────────────────────────────────────────────────────────────────────────────

def unit_words_of(name: str, feature: str) -> set[str]:
    return words(feature) | words(re.sub(r"[-\d]+", " ", name))


def map_unit(catalog: Catalog, name: str, lines: list[int], feature: str, reference: str | None) -> dict[str, Any]:
    unit: dict[str, Any] = {"unit": name, "lines": lines, "feature": feature, "ansi": reference}
    uw = unit_words_of(name, feature)
    got = resolve(catalog, reference, uw)
    if got["exact"] and got["agrees"]:
        rows = list(got["ids"])
        fmt = _FORMAT_KEYED.search(name)
        if fmt:
            for cite in got["cites"]:
                head = cite.section.rpartition(".")[0] if cite.section else ""
                rows += [r["rule-id"] for r in catalog.by_locator.get(f"{cite.roman}.{head}.2", [])
                         if r["kind"] == "FMT" and r["rule-id"] not in rows]
            unit["format"] = int(fmt.group("format"))
        unit.update(disposition="rows", rows=rows, basis=BASIS)
        return unit
    candidates = by_name(catalog, got["cites"], uw)
    unit["disposition"] = "open"
    unit["resolution"] = "ambiguous" if got["ids"] or candidates else "unmapped"
    if got["ids"]:
        unit["literal"] = got["ids"]
        unit["literal-agrees"] = got["agrees"]
    if candidates:
        unit["by-name"] = candidates
    unit["basis"] = BASIS
    return unit


def obsolete_element(statement: str) -> int | None:
    return next((item for item, pattern in OBSOLETE_ELEMENTS if pattern.search(statement)), None)


def map_flag(catalog: Catalog, line: int, kind: str, statement: str, fips: bool) -> dict[str, Any]:
    unit: dict[str, Any] = {"unit": f"{kind}@{line}", "lines": [line], "flag": kind, "statement": statement}
    if fips:
        return unit | {"disposition": "excluded", "reason": "fips-pub-21-1", "basis": BASIS}
    item = obsolete_element(statement)
    rows = catalog.obsolete.get(item, []) if item else []
    unit["element"] = f"C.1({item})" if item else None
    if len(rows) == 1:
        return unit | {"disposition": "rows", "rows": rows, "basis": BASIS}
    unit["disposition"] = "open"
    unit["resolution"] = "ambiguous" if rows else "unmapped"
    if rows:
        unit["candidates"] = rows
    unit["basis"] = BASIS
    return unit


def build_program(catalog: Catalog, name: str, text: str) -> dict[str, Any]:
    prog = parse_program(name, text)
    fips = any(kind == "NON-CONFORMING" for _, kind, _ in prog.flags)
    out: dict[str, Any] = {"program": name, "edition": EDITION}
    units = [map_unit(catalog, n, *rest) for n, rest in prog.units.items()]
    units += [map_flag(catalog, *f, fips) for f in prog.flags]
    out["units"] = units
    if prog.unenumerated:
        out["unenumerated"] = [{"line": n, "text": t, "reason": "a PAR-NAME move naming no literal: its units are owed"}
                               for n, t in prog.unenumerated]
    return out


def dump(obj: dict[str, Any]) -> str:
    return json.dumps(obj, indent=1, ensure_ascii=False) + "\n"


def read_programs(directory: Path = PROGRAMS) -> dict[str, str]:
    return {p.stem: p.read_text(encoding="latin-1") for p in sorted(directory.glob("*.cob"))}


def build_all(catalog: Catalog, programs: dict[str, str]) -> dict[str, dict[str, Any]]:
    return {n: build_program(catalog, n, t) for n, t in programs.items()}


# ── the audit and the term ───────────────────────────────────────────────────────────────────────────────────────────

def audit(catalog: Catalog, programs: dict[str, str], directory: Path) -> list[str]:
    """Every problem of the files in `directory` against the programs and the catalog (empty when it is sound)."""
    problems: list[str] = []
    fresh = build_all(catalog, programs)
    on_disk = {p.stem for p in directory.glob("*.json")} if directory.is_dir() else set()
    problems += [f"{n}: no crosswalk file" for n in sorted(set(programs) - on_disk)]
    problems += [f"{n}: a crosswalk file of no program" for n in sorted(on_disk - set(programs))]
    for name in sorted(set(programs) & on_disk):
        stored = json.loads((directory / f"{name}.json").read_text(encoding="utf-8"))
        want = {u["unit"]: u for u in fresh[name]["units"]}
        have: dict[str, dict[str, Any]] = {}
        for u in stored.get("units", []):
            if u.get("unit") in have:
                problems.append(f"{name}: unit {u['unit']} is listed twice")
            have[u.get("unit")] = u
        problems += [f"{name}: unit {n} (line {want[n]['lines'][0]}) has no entry" for n in sorted(set(want) - set(have))]
        problems += [f"{name}: entry {n} is no unit of the program" for n in sorted(set(have) - set(want))]
        if stored.get("unenumerated") != fresh[name].get("unenumerated"):
            problems.append(f"{name}: the unenumerated sites disagree with the program")
        for n in sorted(set(want) & set(have)):
            problems += audit_unit(catalog, name, want[n], have[n])
    return problems


def audit_elements(spec: str) -> list[str]:
    """The element table against Appendix C.1 of the 1985 text: the table and `C1_HEADINGS` name the same items, and each
    item's heading says the element (the heading's own number is the catalog's `obsolete: C.1(n)`)."""
    problems = [f"element table item {n} has no heading words" for n, _ in OBSOLETE_ELEMENTS if n not in C1_HEADINGS]
    problems += [f"heading words of item {n} name no element of the table" for n in C1_HEADINGS if n not in dict(OBSOLETE_ELEMENTS)]
    start = spec.find("OBSOLETE LANGUAGE ELEMENT LIST")
    end = spec.find("\n## 2.", start)
    if start < 0 or end < 0:
        return problems + ["Appendix C.1 (OBSOLETE LANGUAGE ELEMENT LIST) is not in the 1985 transcription"]
    headings: dict[int, str] = {}
    for number, heading in _C1_ITEM_HEADING.findall(spec[start:end]):
        headings.setdefault(int(number), heading)
    for item, words_ in sorted(C1_HEADINGS.items()):
        if words_.lower() not in headings.get(item, "").lower():
            problems.append(f"Appendix C.1 item {item} is {headings.get(item)!r}, not an item naming {words_!r}")
    return problems


def audit_unit(catalog: Catalog, program: str, want: dict[str, Any], have: dict[str, Any]) -> list[str]:
    where = f"{program}:{have['unit']}"
    problems = [f"{where}: {k} is {have.get(k)!r}, the program says {want.get(k)!r}"
                for k in ("lines", "feature", "ansi", "flag") if have.get(k) != want.get(k)]
    disposition = have.get("disposition")
    if disposition not in DISPOSITIONS:
        return problems + [f"{where}: disposition {disposition!r} is none of {', '.join(DISPOSITIONS)}"]
    if have.get("basis") == BASIS and have != want:
        problems.append(f"{where}: a mechanical entry differs from what the tool now maps (rerun --build)")
    if disposition == "excluded" and have.get("reason") not in EXCLUSIONS:
        problems.append(f"{where}: exclusion reason {have.get('reason')!r} is unknown")
    if disposition == "open" and have.get("resolution") not in RESOLUTIONS:
        problems.append(f"{where}: an open unit names no resolution")
    named = have.get("rows", []) if disposition == "rows" else have.get("literal", []) + have.get("by-name", []) + have.get("candidates", [])
    if disposition == "rows" and not named:
        problems.append(f"{where}: a rows unit names no row")
    problems += [f"{where}: row {r} is no row of the 1985 catalog" for r in named if r not in catalog.rows]
    return problems


def load_crosswalk(directory: Path, programs: dict[str, str]) -> dict[str, dict[str, Any]]:
    missing = sorted(set(programs) - {p.stem for p in directory.glob("*.json")}) if directory.is_dir() else sorted(programs)
    if missing:
        raise CrosswalkError(f"{len(missing)} program(s) have no crosswalk file (run crosswalk.py --build): {', '.join(missing[:5])}")
    return {n: json.loads((directory / f"{n}.json").read_text(encoding="utf-8")) for n in sorted(programs)}


def owed(crosswalk: dict[str, dict[str, Any]], witness: dict[str, str]) -> dict[str, int]:
    """The units still owed, by kind: open units, unenumerated sites, and rows units with a row no met witness holds."""
    c = {"open": 0, "unenumerated": 0, "unsubsumed": 0}
    for obj in crosswalk.values():
        c["unenumerated"] += len(obj.get("unenumerated", []))
        for u in obj["units"]:
            if u["disposition"] == "open":
                c["open"] += 1
            elif u["disposition"] == "rows":
                unknown = [r for r in u["rows"] if r not in witness]
                if unknown:
                    raise CrosswalkError(f"{obj['program']}:{u['unit']} names rows the 1985 catalog lacks: {', '.join(unknown)}")
                c["unsubsumed"] += any(witness[r] not in MET for r in u["rows"])
    return c


def unmet(edition: str) -> int:
    """The NIST term of the status line: the units neither dispositioned nor subsumed."""
    if str(edition) != EDITION:
        raise CrosswalkError(f"the crosswalk maps NIST CCVS85 and no edition but {EDITION} (asked for {edition})")
    import obligations
    facts, _, _ = obligations.facts_of_repository(EDITION)
    return sum(owed(load_crosswalk(CROSSWALK, read_programs()), {f.rule_id: f.witness for f in facts}).values())


def summary(crosswalk: dict[str, dict[str, Any]]) -> dict[str, int]:
    c = dict.fromkeys(("programs", "units", "rows", "ambiguous", "unmapped", "excluded", "unenumerated"), 0)
    c["programs"] = len(crosswalk)
    for obj in crosswalk.values():
        c["unenumerated"] += len(obj.get("unenumerated", []))
        for u in obj["units"]:
            c["units"] += 1
            if u["disposition"] == "open":
                c[u["resolution"]] += 1
            else:
                c[u["disposition"]] += 1
    return c


def verdict_line(c: dict[str, int], owed_units: int) -> str:
    return (f"{VERDICT}{owed_units} of {c['units'] - c['excluded'] + c['unenumerated']} ISO-language units owed ({c['units']} units in "
            f"{c['programs']} programs: mapped {c['rows']}, ambiguous {c['ambiguous']}, unmapped {c['unmapped']}, "
            f"excluded {c['excluded']}; {c['unenumerated']} unenumerated sites) ===")


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    mode = ap.add_mutually_exclusive_group()
    mode.add_argument("--build", action="store_true", help="write tests/nist/crosswalk/<program>.json for every program")
    mode.add_argument("--check", action="store_true", help="the audit: every file against its program and the catalog")
    args = ap.parse_args(argv)
    import inventory_view
    catalog = Catalog(inventory_view.inventory(1985))
    programs = read_programs()
    try:
        if args.build:
            CROSSWALK.mkdir(parents=True, exist_ok=True)
            built = build_all(catalog, programs)
            for name, obj in built.items():
                (CROSSWALK / f"{name}.json").write_bytes(dump(obj).encode("utf-8"))
            print(f"  wrote {len(built)} file(s) to {CROSSWALK.relative_to(REPO).as_posix()}")
            return 0
        if args.check:
            problems = audit_elements(SPEC_1985.read_text(encoding="utf-8")) + audit(catalog, programs, CROSSWALK)
            for p in problems[:50]:
                print(f"  {p}")
            print(f"{VERDICT}{'RED' if problems else 'GREEN'}: {len(problems)} problem(s) over {len(programs)} programs ===")
            return 1 if problems else 0
        import obligations
        facts, _, _ = obligations.facts_of_repository(EDITION)
        crosswalk = load_crosswalk(CROSSWALK, programs)
        c = summary(crosswalk)
        print(verdict_line(c, sum(owed(crosswalk, {f.rule_id: f.witness for f in facts}).values())))
        return 0
    except CrosswalkError as e:
        print(f"{VERDICT}ERROR: {e} ===")
        return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
