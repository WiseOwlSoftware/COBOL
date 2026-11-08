#!/usr/bin/env python3
"""THE OBSOLETE STANDING of each catalog rule: `status` (DESIGN-spec-conformance-review.md §10.2, kb/Work PB4993).

    python scripts/spec/obsolete_elements.py --self-test

A standard marks some of its elements obsolete: still required of a conforming implementation, and to be deleted at the
next revision (§1985:I.1.5.2.3, which names the list at §1985:C.1). Whether a rule is obsolete is a fact of the rule in
its own edition's text, so it is a field of the edition's rule catalog (`status`), never of the inventory store: `normal`,
or `obsolete: <list>(<n>)` naming the list entry (or entries) that make it so, so a diagnostic and the conformance
statement can cite it. Deprecation is not a kind of change record: a rule KEPT into an obsolete one, then REMOVED, is the
deprecate-then-remove lifecycle (lineage.py).

THE AUTHORITY IS THE STANDARD'S OWN LIST, held ONCE as data per edition (`extract_rule_catalog.OBSOLETE_OF`), one entry
per list item: its number `n`, the `terms` that name the element in the rules (each a whole word or phrase, matched as
the text spells it), and the `sections` of the transcription that specify it. The item's NAME is the list's own text and
is never copied (`listed`). The marking is decided PER SECTION, never by the kind of element (the owner's decision in
kb/Work PB4993, after review w20261927a R1-1), because several whole elements own no section of their own:

- an OWN section, one whose heading names one of the entry's terms (THE ALTER STATEMENT, THE RERUN CLAUSE, SECTION XV:
  DEBUG MODULE), marks every rule of the section, its general formats included;
- every other named section is SHARED with current syntax (an I-O-CONTROL paragraph beside SAME, a file description entry,
  the Identification Division's organization beside PROGRAM-ID, the OPEN, STOP and GO TO statements). It marks only the
  syntax and general rules whose text names one of the entry's terms; its general format stays current even where it
  carries the obsolete syntax, since what a later edition does to the format is the format's own change record (CHANGED).

OWN or SHARED is DERIVED from the heading, never held in the data, so the two cannot disagree (review R2-1).

THE DRIFT CHECKS (`findings`) hold the data to the text: the entries are exactly the list's items; every section is a
locator of the transcription; every sentence of the text that calls an element obsolete ("... is an obsolete element in
Standard COBOL ...") falls in a section some entry names, or in a section the data names as describing the list's
NOTATION (the Z of the level tables); every entry resolves to at least one rule of its sections that names a term (a
shared format that carries it counts), except an entry the data declares `no-rule` with its reason, which must resolve to
none; and, over the marked rows, no general format of a shared section is marked and no rule outside its entries'
sections is (`marking_errors`). A later edition with its own list gets its own data file, read by this same code.
"""
from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import sys
from collections.abc import Iterable, Mapping, Sequence
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import spec_pdf  # noqa: E402  (the prose box side, BOX_SIDE)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

NORMAL = "normal"
OBSOLETE = "obsolete"
FORMAT = "FMT"
#: A sentence of the text that calls an element obsolete (§1985: "is an obsolete element in Standard COBOL", "are obsolete
#: elements", "is an obsolete feature").
OBSOLETE_SENTENCE = re.compile(r"\b(?:is an|are) obsolete (?:element|feature)s?\b")
#: An item of the list: `(n) <u>name</u>.`
LIST_ITEM = re.compile(r"^\((?P<n>\d+)\)\s+<u>(?P<name>[^<]+)</u>")
_MARKUP = re.compile(r"<[^>]+>|" + re.escape(spec_pdf.BOX_SIDE))


@dataclasses.dataclass(frozen=True)
class Entry:
    """One item of an edition's obsolete list, as its data file states it."""
    n: int
    terms: tuple[str, ...]
    sections: tuple[str, ...]
    #: Why no rule of the catalog states the element (its text is prose outside any rule block), or "" when one does.
    no_rule: str


@dataclasses.dataclass(frozen=True)
class ObsoleteList:
    """An edition's obsolete list: its locator in the text, the sections whose obsolete-sentence describes the list's
    notation rather than an element, and its entries."""
    locator: str
    notation: tuple[str, ...]
    entries: tuple[Entry, ...]


def load(path: pathlib.Path) -> ObsoleteList:
    raw = json.loads(path.read_text(encoding="utf-8"))
    return ObsoleteList(raw["list"], tuple(raw["notation"]["sections"]),
                        tuple(Entry(e["n"], tuple(e["terms"]), tuple(e["sections"]), e.get("no-rule", ""))
                              for e in raw["entries"]))


def plain(text: str) -> str:
    """`text` without its markup: the underline tags and the prose box side."""
    return _MARKUP.sub("", text)


def names(text: str, terms: Iterable[str]) -> bool:
    """Does `text` name one of `terms`, each a whole word or phrase as the text spells it (a hyphen joins a word)?"""
    t = plain(text)
    return any(re.search(rf"(?<![\w-]){re.escape(term)}(?![\w-])", t) for term in terms)


def within(locator: str, section: str) -> bool:
    return locator == section or locator.startswith(section + ".")


def headings(lines: Sequence[str], regions: Iterable[tuple[str, str, int, int]]) -> dict[str, str]:
    """Each locator's heading: its region's title, or the region's first line where the reader records none (a Roman
    section: `SECTION XV: DEBUG MODULE`)."""
    out: dict[str, str] = {}
    for loc, title, start, _end in regions:
        out.setdefault(loc, title or lines[start].lstrip("#").strip())
    return out


def own(section: str, entry: Entry, heads: Mapping[str, str]) -> bool:
    """Is `section` the entry's OWN section (its heading names one of the entry's terms), rather than SHARED?"""
    return names(heads.get(section, ""), entry.terms)


def standing(rows: Iterable[dict[str, Any]], key: str, lst: ObsoleteList,
             heads: Mapping[str, str]) -> dict[str, list[int]]:
    """Each obsolete rule's id -> the numbers of the entries that make it so (in list order). `key` is the catalog
    field that locates a rule (the edition's `locator`)."""
    rows = list(rows)
    out: dict[str, list[int]] = {}
    for e in lst.entries:
        for sec in e.sections:
            whole = own(sec, e, heads)
            for r in rows:
                if within(r[key], sec) and (whole or (r["kind"] != FORMAT and names(r["text"], e.terms))):
                    if e.n not in out.setdefault(r["id"], []):
                        out[r["id"]].append(e.n)
    return out


def status_of(entries: Sequence[int], lst: ObsoleteList) -> str:
    """A rule's `status`: `normal`, or `obsolete: C.1(5), C.1(6)` naming each entry that makes it so."""
    return NORMAL if not entries else f"{OBSOLETE}: " + ", ".join(f"{lst.locator}({n})" for n in entries)


def entries_of(status: str) -> list[int]:
    """The entry numbers a `status` names (none for a normal rule)."""
    return [int(n) for n in re.findall(r"\((\d+)\)", status.partition(":")[2])] if status.startswith(OBSOLETE) else []


def mark(rows: Iterable[dict[str, Any]], key: str, lst: ObsoleteList, heads: Mapping[str, str]) -> None:
    """Set every row's `status` from the list (in place)."""
    rows = list(rows)
    marked = standing(rows, key, lst, heads)
    for r in rows:
        r["status"] = status_of(marked.get(r["id"], []), lst)


def listed(lines: Sequence[str], regions: Iterable[tuple[str, str, int, int]], locator: str) -> dict[int, str]:
    """The list's own items in the text: number -> name."""
    out: dict[int, str] = {}
    for loc, _t, start, end in regions:
        if loc == locator:
            for line in lines[start:end]:
                if m := LIST_ITEM.match(line):
                    out[int(m["n"])] = m["name"].strip()
    return out


def sentences(lines: Sequence[str], regions: Iterable[tuple[str, str, int, int]]) -> list[tuple[int, str]]:
    """Each line of the text that calls an element obsolete, with the locator of the innermost region that holds it
    (a printed page's region, `p.…`, is not a section)."""
    spans = sorted(((loc, s, e) for loc, _t, s, e in regions if not loc.startswith("p.")), key=lambda x: x[2] - x[1])
    out = []
    for i, line in enumerate(lines):
        if OBSOLETE_SENTENCE.search(line):
            out.append((i + 1, next((loc for loc, s, e in spans if s <= i < e), "")))
    return out


def marking_errors(rows: Iterable[dict[str, Any]], key: str, lst: ObsoleteList, heads: Mapping[str, str]) -> list[str]:
    """Rows whose `status` breaks the marking rule: an obsolete rule outside every section of the entries it names, or
    a general format of a section that is SHARED for every entry it names there."""
    by_n = {e.n: e for e in lst.entries}
    bad = []
    for r in rows:
        for n in entries_of(r.get("status", NORMAL)):
            e = by_n.get(n)
            secs = [s for s in (e.sections if e else ()) if within(r[key], s)]
            if not secs:
                bad.append(f"{r['id']} is marked {lst.locator}({n}) but lies in none of that entry's sections")
            elif r["kind"] == FORMAT and not any(own(s, e, heads) for s in secs):
                bad.append(f"{r['id']} is a general format of a section SHARED with current syntax, marked {lst.locator}({n})")
    return bad


def findings(lines: Sequence[str], regions: Sequence[tuple[str, str, int, int]], rows: Sequence[dict[str, Any]],
             key: str, lst: ObsoleteList) -> list[str]:
    """Where the list's data and the text (or the catalog's rows) disagree; empty when they agree."""
    out: list[str] = []
    heads = headings(lines, regions)
    universe = {loc for loc, *_ in regions}
    items = listed(lines, regions, lst.locator)
    if not items:
        out.append(f"the text has no list at {lst.locator}")
    numbers = [e.n for e in lst.entries]
    if numbers != sorted(items) or len(set(numbers)) != len(numbers):
        out.append(f"the data's entries {numbers} are not the list's items {sorted(items)}, each once in order")
    for sec in sorted({s for e in lst.entries for s in e.sections} | set(lst.notation)):
        if sec not in universe:
            out.append(f"section {sec} is no locator of the text")
    named = {s for e in lst.entries for s in e.sections}
    said = sentences(lines, regions)
    for line, loc in said:
        if within(loc, lst.locator):
            continue
        if not any(within(loc, s) for s in named | set(lst.notation)):
            out.append(f"line {line}: an obsolete-element sentence in {loc or 'no section'}, which no entry names")
    for sec in lst.notation:
        if not any(within(loc, sec) for _, loc in said):
            out.append(f"notation section {sec} holds no obsolete-element sentence")
    for e in lst.entries:
        resolving = [r["id"] for r in rows for s in e.sections
                     if within(r[key], s) and (own(s, e, heads) or names(r["text"], e.terms))]
        if e.no_rule and resolving:
            out.append(f"entry {lst.locator}({e.n}) says no rule states it ({e.no_rule}), but {resolving[:3]} do")
        if not e.no_rule and not resolving:
            out.append(f"entry {lst.locator}({e.n}) ({items.get(e.n, '?')}) resolves to no rule of its sections {list(e.sections)}")
    marked = [dict(r, status=status_of(ns, lst)) for r in rows
              if (ns := standing([r], key, lst, heads).get(r["id"]))]
    out += marking_errors(marked, key, lst, heads)
    return out


# -- self-test ------------------------------------------------------------------------------------------------------------

PLANTED = """# SECTION VI: NUCLEUS MODULE
## 3.2 ORGANIZATION
The AUTHOR paragraph is an obsolete element in Standard COBOL because it is to be deleted.
### 3.2.1 Syntax Rules
## 6.7 THE ALTER STATEMENT
### 6.7.1 Function
The ALTER statement is an obsolete element in Standard COBOL because it is to be deleted.
## 6.24 THE STOP STATEMENT
### 6.24.1 Function
The literal variation of the STOP statement is an obsolete element in Standard COBOL.
## 6.30 THE LEFTOVER STATEMENT
The LEFTOVER statement is an obsolete element in Standard COBOL.
# SECTION I: INTRODUCTION
## 2.1 GENERAL DESCRIPTION
The letter Z: this element is an obsolete element in Standard COBOL because it is to be deleted.
# APPENDIX C: LANGUAGE ELEMENT LISTS
## 1. OBSOLETE LANGUAGE ELEMENT LIST
(1) <u>AUTHOR paragraph (1 NUC)</u>. Placed in the obsolete element category.
(2) <u>ALTER statement (1 NUC)</u>. Placed in the obsolete element category.
(3) <u>STOP literal statement (1 NUC)</u>. Placed in the obsolete element category.
"""


def _row(rid: str, loc: str, kind: str, text: str) -> dict[str, Any]:
    return {"id": rid, "locator": loc, "kind": kind, "text": text}


def self_test() -> int:
    import citation  # noqa: PLC0415  (the 1985 regions reader)
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    lines = PLANTED.splitlines()
    regions = citation.regions_1985(lines)
    rows = [_row("FMT-VI.3.2.1", "VI.3.2.1", "FMT", "IDENTIFICATION DIVISION. PROGRAM-ID. [ <u>AUTHOR</u>. ]"),
            _row("SR-VI.3.2.1-1", "VI.3.2.1", "SR", "The AUTHOR paragraph may be omitted."),
            _row("SR-VI.3.2.1-2", "VI.3.2.1", "SR", "The PROGRAM-ID paragraph is required."),
            _row("FMT-VI.6.7.2", "VI.6.7.2", "FMT", "ALTER procedure-name-1 TO procedure-name-2"),
            _row("GR-VI.6.7.4-1", "VI.6.7.4", "GR", "Execution transfers control to procedure-name-2."),
            _row("SR-VI.6.24.3-1", "VI.6.24.3", "SR", "Literal-1 must not be a figurative constant."),
            _row("GR-VI.6.24.4-1", "VI.6.24.4", "GR", "If the RUN phrase is specified, execution ceases."),
            _row("GR-VI.6.24.4-2", "VI.6.24.4", "GR", "Prose about literal-10 is no term.")]
    lst = ObsoleteList("C.1", ("I.2.1",), (Entry(1, ("AUTHOR",), ("VI.3.2",), ""),
                                           Entry(2, ("ALTER",), ("VI.6.7",), ""),
                                           Entry(3, ("literal-1", "Literal-1"), ("VI.6.24",), "")))
    heads = headings(lines, regions)
    marked = standing(rows, "locator", lst, heads)
    ok(marked == {"SR-VI.3.2.1-1": [1], "FMT-VI.6.7.2": [2], "GR-VI.6.7.4-1": [2], "SR-VI.6.24.3-1": [3]},
       "an OWN section marks every rule, its format included; a SHARED one only the rules naming a term, never its format")
    ok(own("VI.6.7", lst.entries[1], heads) and not own("VI.3.2", lst.entries[0], heads),
       "OWN or SHARED is derived from the section's heading")
    ok(status_of([5, 6], lst) == "obsolete: C.1(5), C.1(6)" and entries_of("obsolete: C.1(5), C.1(6)") == [5, 6]
       and status_of([], lst) == NORMAL and entries_of(NORMAL) == [],
       "a status names each entry that makes the rule obsolete, and reads back")
    ok(listed(lines, regions, "C.1") == {1: "AUTHOR paragraph (1 NUC)", 2: "ALTER statement (1 NUC)",
                                          3: "STOP literal statement (1 NUC)"},
       "the list's items are read from the text, never copied into the data")
    got = findings(lines, regions, rows, "locator", lst)
    ok(len(got) == 1 and "VI.6.30" in got[0],
       "an obsolete-element sentence in a section no entry names is a finding (the notation section is not)")
    lst2 = dataclasses.replace(lst, entries=lst.entries[:2])
    ok(any("not the list's items" in f for f in findings(lines, regions, rows, "locator", lst2)),
       "the data's entries must be exactly the list's items")
    lst3 = dataclasses.replace(lst, entries=(*lst.entries[:2], Entry(3, ("Literal-1",), ("VI.6.24",), "prose only")))
    ok(any("says no rule states it" in f for f in findings(lines, regions, rows, "locator", lst3)),
       "an entry declared to state no rule is a finding when a rule names it")
    lst4 = dataclasses.replace(lst, entries=(*lst.entries[:2], Entry(3, ("RERUN",), ("VI.6.24",), "")))
    ok(any("resolves to no rule" in f for f in findings(lines, regions, rows, "locator", lst4)),
       "an entry no rule of its sections names is a finding")
    lst5 = dataclasses.replace(lst, entries=(*lst.entries[:2], Entry(3, ("literal-1",), ("VI.6.99",), "")))
    ok(any("no locator of the text" in f for f in findings(lines, regions, rows, "locator", lst5)),
       "a section the text does not have is a finding")
    bad = [dict(rows[0], status="obsolete: C.1(1)"), dict(rows[6], status="obsolete: C.1(2)")]
    ok(len(marking_errors(bad, "locator", lst, heads)) == 2,
       "a shared section's format, or a rule outside the entry's sections, marked obsolete is a finding")
    ok(names("<u>MEMORY</u> SIZE integer-1", ["MEMORY SIZE"]) and not names("the value of x", ["VALUE OF"])
       and not names("DEBUGGING MODE", ["DEBUG"]) and names("║The DEBUG module║", ["DEBUG"]),
       "a term is a whole word or phrase as the text spells it, markup aside")
    print(f"=== OBSOLETE ELEMENTS SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] != ["--self-test"]:
        sys.exit("usage: python scripts/spec/obsolete_elements.py --self-test  (the catalog extractor applies the list)")
    sys.exit(self_test())
