#!/usr/bin/env python3
"""THE STANDING of each catalog rule: `status` (DESIGN-spec-conformance-review.md §10.2, kb/Work PB4993).

    python scripts/spec/element_standing.py --self-test

A standard marks some of its language elements with a STANDING by naming them in a list of its own. 1985 has one list,
the obsolete elements (§1985:I.1.5.2.3 defines it, §1985:C.1 holds it: still required, to be deleted at the next
revision). 2023 has two (§2023:4.2.12 and §2023:4.2.13 define them, Annex F holds them): the ARCHAIC elements of
§2023:F.1 (still required, discouraged, on no deletion schedule) and the OBSOLETE elements of §2023:F.2 (still required,
to be removed at the next edition). Whether a rule carries a standing is a fact of the rule in its own edition's text, so
it is a field of the edition's rule catalog (`status`), never of the inventory store: `normal`, or one clause per
standing naming the list entries that confer it, `obsolete: C.1(5), C.1(6)` or `archaic: F.1(2)` (two standings join
with `; `), so a diagnostic and the conformance statement can cite it. Deprecation is not a kind of change record: a
rule KEPT into an obsolete one, then REMOVED, is the deprecate-then-remove lifecycle (lineage.py).

THE AUTHORITY IS THE STANDARD'S OWN LISTS, held ONCE as data per edition (`extract_rule_catalog.STANDING_OF`), each list
naming its `standing` and its locator, one entry per list item: its number `n`, the `terms` that name the element in
the rules (each a whole word or phrase, matched as the text spells it), the `sections` of the transcription that
specify it, and, where one section needs a term that would name something else in the entry's other sections, that
section's own extra terms (`section-terms`). The item's NAME is the list's own text and is never copied (`listed`). The
marking is decided PER SECTION, never by the kind of element (the owner's decision in kb/Work PB4993, after review
w20261927a R1-1), because several whole elements own no section of their own:

- an OWN section, one whose heading names one of the entry's terms (THE ALTER STATEMENT, SECTION XV: DEBUG MODULE,
  FLAG-02 directive, VALIDATE statement), marks every rule of the section, its general formats included;
- every other named section is SHARED with current syntax (an I-O-CONTROL paragraph beside SAME, the OPEN, STOP, IF,
  SEARCH and MOVE statements, the ARITHMETIC clause). It marks only the syntax and general rules whose text names one
  of the entry's terms; its general format stays current even where it carries the element's syntax, since what a
  later edition does to the format is the format's own change record (CHANGED).

OWN or SHARED is DERIVED from the heading, never held in the data, so the two cannot disagree (review R2-1).

THE DRIFT CHECKS (`findings`) hold the data to the text, per list: the entries are exactly the list's items; every
section is a locator of the text; every sentence of the text that gives an element the list's standing ("... is an
obsolete element in Standard COBOL ...", "... is an archaic feature.") falls in a section some entry names, or in a
section the data names as a REMARK (a sentence that is not the element's specification: the 1985 level tables' Z
notation, a 2023 definition's note, an informative annex); every entry marks at least one rule (`standing`, the
marking itself, so a section whose only mention is its general format does not count), except an entry the data
declares `no-rule` with its reason, which must mark none; and, over the marked rows, no general format of a shared
section is marked and no rule outside its entries' sections is (`marking_errors`). An edition's lists are one data file,
read by this same code; `apply` is the one entry point both catalog arms call.
"""
from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import sys
from collections import Counter
from collections.abc import Iterable, Mapping, Sequence
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import spec_pdf  # noqa: E402  (the prose box side, BOX_SIDE)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

NORMAL = "normal"
#: The standings a list may confer, each the word the text uses for it (§1985:I.1.5.2.3; §2023:4.2.12, §2023:4.2.13).
STANDINGS = ("archaic", "obsolete")
FORMAT = "FMT"
#: An item of a list: 1985 `(n) <u>name</u>.`; 2023 `n\) **name.**`.
LIST_ITEMS = (re.compile(r"^\((?P<n>\d+)\)\s+<u>(?P<name>[^<]+)</u>"),
              re.compile(r"^(?P<n>\d+)[\\]\)\s+\*\*(?P<name>[^*]+?)\.?\*\*"))
_MARKUP = re.compile(r"<[^>]+>|" + re.escape(spec_pdf.BOX_SIDE))
_CLAUSE = re.compile(r"(?P<locator>[A-Z0-9.]+)\((?P<n>\d+)\)")


def sentence(standing: str) -> re.Pattern[str]:
    """A sentence of the text that gives an element `standing`: "is an obsolete element in Standard COBOL"
    (§1985:VI.6.7.1), "are obsolete elements" (§1985:VI.3.2), "is an obsolete feature" (§1985:IV.4.1), "are an obsolete
    feature" (§2023:14.6.13.1.6), "is an archaic feature" (§2023:14.9.19.2)."""
    return re.compile(rf"\b(?:is an|are an|are) {standing} (?:element|feature)s?\b")


@dataclasses.dataclass(frozen=True)
class Entry:
    """One item of a list, as its data file states it."""
    n: int
    terms: tuple[str, ...]
    sections: tuple[str, ...]
    #: Extra terms that name the element in one section only (a format number, a phrase of a shared clause).
    section_terms: Mapping[str, tuple[str, ...]]
    #: Why no rule of the catalog states the element (its text is prose outside any rule block), or "" when one does.
    no_rule: str

    def terms_in(self, section: str) -> tuple[str, ...]:
        return self.terms + self.section_terms.get(section, ())


@dataclasses.dataclass(frozen=True)
class ElementList:
    """One list of an edition: the standing it confers, its locator in the text, the sections whose standing sentence
    is a remark rather than an element's specification, and its entries."""
    standing: str
    locator: str
    remarks: tuple[str, ...]
    entries: tuple[Entry, ...]


def load(path: pathlib.Path) -> tuple[ElementList, ...]:
    raw = json.loads(path.read_text(encoding="utf-8"))
    return tuple(ElementList(lst["standing"], lst["list"], tuple(lst["remarks"]["sections"]),
                             tuple(Entry(e["n"], tuple(e["terms"]), tuple(e["sections"]),
                                         {s: tuple(t) for s, t in e.get("section-terms", {}).items()},
                                         e.get("no-rule", ""))
                                   for e in lst["entries"]))
                 for lst in raw["lists"])


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
    return names(heads.get(section, ""), entry.terms_in(section))


def standing(rows: Iterable[dict[str, Any]], key: str, lst: ElementList,
             heads: Mapping[str, str]) -> dict[str, list[int]]:
    """Each rule's id that `lst` marks -> the numbers of the entries that mark it (in list order). `key` is the catalog
    field that locates a rule (the edition's `locator` or `section`)."""
    rows = list(rows)
    out: dict[str, list[int]] = {}
    for e in lst.entries:
        for sec in e.sections:
            whole = own(sec, e, heads)
            for r in rows:
                if within(r[key], sec) and (whole or (r["kind"] != FORMAT and names(r["text"], e.terms_in(sec)))):
                    if e.n not in out.setdefault(r["id"], []):
                        out[r["id"]].append(e.n)
    return out


def clause_of(entries: Sequence[int], lst: ElementList) -> str:
    """One standing's clause of a `status`: `obsolete: C.1(5), C.1(6)`."""
    return f"{lst.standing}: " + ", ".join(f"{lst.locator}({n})" for n in entries)


def status_of(marks: Sequence[tuple[ElementList, Sequence[int]]]) -> str:
    """A rule's `status`: `normal`, or each list's clause that marks it, in the data's list order, joined by `; `."""
    clauses = [clause_of(entries, lst) for lst, entries in marks if entries]
    return "; ".join(clauses) if clauses else NORMAL


def entries_of(status: str) -> list[tuple[str, str, int]]:
    """The (standing, list locator, entry number) each clause of a `status` names (none for a normal rule)."""
    if status == NORMAL:
        return []
    out = []
    for clause in status.split("; "):
        word, _, rest = clause.partition(": ")
        out += [(word, m["locator"], int(m["n"])) for m in _CLAUSE.finditer(rest)]
    return out


def mark(rows: Iterable[dict[str, Any]], key: str, lists: Sequence[ElementList], heads: Mapping[str, str]) -> None:
    """Set every row's `status` from the lists (in place)."""
    rows = list(rows)
    marked = [(lst, standing(rows, key, lst, heads)) for lst in lists]
    for r in rows:
        r["status"] = status_of([(lst, m.get(r["id"], [])) for lst, m in marked])


def listed(lines: Sequence[str], regions: Iterable[tuple[str, str, int, int]], locator: str) -> dict[int, str]:
    """The list's own items in the text: number -> name."""
    out: dict[int, str] = {}
    for loc, _t, start, end in regions:
        if loc == locator:
            for line in lines[start:end]:
                if m := next((m for p in LIST_ITEMS if (m := p.match(line))), None):
                    out[int(m["n"])] = m["name"].strip()
    return out


def sentences(lines: Sequence[str], regions: Iterable[tuple[str, str, int, int]],
              standing_word: str) -> list[tuple[int, str]]:
    """Each line of the text that gives an element `standing_word`, with the locator of the innermost region that holds
    it (a printed page's region, `p.…`, is not a section)."""
    spans = sorted(((loc, s, e) for loc, _t, s, e in regions if not loc.startswith("p.")), key=lambda x: x[2] - x[1])
    said = sentence(standing_word)
    out = []
    for i, line in enumerate(lines):
        if said.search(line):
            out.append((i + 1, next((loc for loc, s, e in spans if s <= i < e), "")))
    return out


def marking_errors(rows: Iterable[dict[str, Any]], key: str, lists: Sequence[ElementList],
                   heads: Mapping[str, str]) -> list[str]:
    """Rows whose `status` breaks the marking rule: a clause naming a list the data does not hold, an entry the list does
    not have, a rule outside every section of the entry it names, or a general format of a section that is SHARED for
    every entry it names there."""
    by_list = {(lst.standing, lst.locator): lst for lst in lists}
    bad = []
    for r in rows:
        for word, locator, n in entries_of(r.get("status", NORMAL)):
            lst = by_list.get((word, locator))
            e = next((e for e in lst.entries if e.n == n), None) if lst else None
            secs = [s for s in (e.sections if e else ()) if within(r[key], s)]
            if not secs:
                bad.append(f"{r['id']} is marked {word} by {locator}({n}) but lies in none of that entry's sections")
            elif r["kind"] == FORMAT and not any(own(s, e, heads) for s in secs):
                bad.append(f"{r['id']} is a general format of a section SHARED with current syntax, marked "
                           f"{locator}({n})")
    return bad


def findings(lines: Sequence[str], regions: Sequence[tuple[str, str, int, int]], rows: Sequence[dict[str, Any]],
             key: str, lists: Sequence[ElementList]) -> list[str]:
    """Where the lists' data and the text (or the catalog's rows) disagree; empty when they agree."""
    out: list[str] = []
    heads = headings(lines, regions)
    universe = {loc for loc, *_ in regions}
    for word in sorted({lst.standing for lst in lists} - set(STANDINGS)):
        out.append(f"a list confers the standing {word!r}, which is none of {list(STANDINGS)}")
    marks = []
    for lst in lists:
        items = listed(lines, regions, lst.locator)
        if not items:
            out.append(f"the text has no list at {lst.locator}")
        numbers = [e.n for e in lst.entries]
        if numbers != sorted(items) or len(set(numbers)) != len(numbers):
            out.append(f"{lst.locator}: the data's entries {numbers} are not the list's items {sorted(items)}, each "
                       "once in order")
        for e in lst.entries:
            for sec in sorted(set(e.section_terms) - set(e.sections)):
                out.append(f"entry {lst.locator}({e.n}) gives terms to {sec}, which is none of its sections")
        for sec in sorted({s for e in lst.entries for s in e.sections} | set(lst.remarks)):
            if sec not in universe:
                out.append(f"{lst.locator}: section {sec} is no locator of the text")
        named = {s for e in lst.entries for s in e.sections} | set(lst.remarks)
        said = sentences(lines, regions, lst.standing)
        for line, loc in said:
            if not within(loc, lst.locator) and not any(within(loc, s) for s in named):
                out.append(f"line {line}: an {lst.standing}-element sentence in {loc or 'no section'}, which no "
                           f"entry of {lst.locator} names")
        for sec in lst.remarks:
            if not any(within(loc, sec) for _, loc in said):
                out.append(f"{lst.locator}: remark section {sec} holds no {lst.standing}-element sentence")
        marked = standing(rows, key, lst, heads)
        for e in lst.entries:
            resolving = [rid for rid, ns in marked.items() if e.n in ns]
            if e.no_rule and resolving:
                out.append(f"entry {lst.locator}({e.n}) says no rule states it ({e.no_rule}), but {resolving[:3]} do")
            if not e.no_rule and not resolving:
                out.append(f"entry {lst.locator}({e.n}) ({items.get(e.n, '?')}) marks no rule of its sections "
                           f"{list(e.sections)}")
        marks.append((lst, marked))
    ids = {rid for _, m in marks for rid in m}
    out += marking_errors([dict(r, status=status_of([(lst, m.get(r["id"], [])) for lst, m in marks]))
                           for r in rows if r["id"] in ids], key, lists, heads)
    return out


def apply(rows: list[dict[str, Any]], key: str, lines: Sequence[str],
          regions: Sequence[tuple[str, str, int, int]], data: pathlib.Path) -> list[str]:
    """THE ONE ENTRY POINT of both catalog arms: mark each row's `status` from the edition's lists held at `data`, and
    return where the data and the text disagree (the arm's `--check` fails on any)."""
    lists = load(data)
    mark(rows, key, lists, headings(lines, regions))
    return findings(lines, regions, rows, key, lists)


def tally(rows: Iterable[dict[str, Any]]) -> list[str]:
    """The catalog summary's standing lines: per standing a rule carries, its rule count and its rules by list entry
    (in list order); one line saying so when no rule carries any."""
    counts: dict[str, Counter[tuple[str, int]]] = {}
    marked: Counter[str] = Counter()
    for r in rows:
        named = entries_of(r["status"])
        marked.update({word for word, _, _ in named})
        for word, locator, n in named:
            counts.setdefault(word, Counter())[(locator, n)] += 1
    return [f"   {word} rules {marked[word]}; by list entry: "
            + ", ".join(f"{loc}({n}) {c}" for (loc, n), c in sorted(counts[word].items()))
            for word in STANDINGS if word in marked] or ["   no rule carries a standing"]


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

#: An ISO-shaped text with two lists, to hold the 2023 shape: an archaic and an obsolete list, a section the archaic
#: entry shares, and an obsolete entry whose OWN section is named by its heading.
PLANTED_ISO = """#### 14.9.19 IF statement
##### 14.9.19.2 General formats
NOTE NEXT SENTENCE is an archaic feature.
##### 14.9.19.3 Syntax rules
#### 14.9.50 VALIDATE statement
##### 14.9.50.1 General
NOTE The VALIDATE facility is an obsolete feature.
### F.1 Archaic language elements
1\\) **The EXIT PROGRAM Statement.** The EXIT PROGRAM statement provides the same functionality.
2\\) **NEXT SENTENCE phrase in the IF and SEARCH statements.** This phrase can be confusing.
### F.2 Obsolete language elements
1\\) **Validate facility.** The VALIDATE facility has not been implemented.
"""


def _row(rid: str, loc: str, kind: str, text: str) -> dict[str, Any]:
    return {"id": rid, "locator": loc, "kind": kind, "text": text}


def _iso_row(rid: str, section: str, kind: str, text: str) -> dict[str, Any]:
    return {"id": rid, "section": section, "kind": kind, "text": text}


def self_test() -> int:
    import citation  # noqa: PLC0415  (the 1985 and ISO regions readers)
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

    def entry(n: int, terms: tuple[str, ...], sections: tuple[str, ...], no_rule: str = "",
              section_terms: Mapping[str, tuple[str, ...]] | None = None) -> Entry:
        return Entry(n, terms, sections, section_terms or {}, no_rule)

    lst = ElementList("obsolete", "C.1", ("I.2.1",), (entry(1, ("AUTHOR",), ("VI.3.2",)),
                                                     entry(2, ("ALTER",), ("VI.6.7",)),
                                                     entry(3, ("literal-1", "Literal-1"), ("VI.6.24",))))
    heads = headings(lines, regions)
    marked = standing(rows, "locator", lst, heads)
    ok(marked == {"SR-VI.3.2.1-1": [1], "FMT-VI.6.7.2": [2], "GR-VI.6.7.4-1": [2], "SR-VI.6.24.3-1": [3]},
       "an OWN section marks every rule, its format included; a SHARED one only the rules naming a term, never its format")
    ok(own("VI.6.7", lst.entries[1], heads) and not own("VI.3.2", lst.entries[0], heads),
       "OWN or SHARED is derived from the section's heading")
    lst_b = dataclasses.replace(lst, standing="archaic", locator="F.1")
    both = "obsolete: C.1(5), C.1(6); archaic: F.1(2)"
    ok(status_of([(lst, [5, 6]), (lst_b, [2])]) == both
       and entries_of(both) == [("obsolete", "C.1", 5), ("obsolete", "C.1", 6), ("archaic", "F.1", 2)]
       and status_of([(lst, [])]) == NORMAL and entries_of(NORMAL) == [],
       "a status names each list and entry that gives the rule its standing, and reads back")
    ok(listed(lines, regions, "C.1") == {1: "AUTHOR paragraph (1 NUC)", 2: "ALTER statement (1 NUC)",
                                          3: "STOP literal statement (1 NUC)"},
       "the list's items are read from the text, never copied into the data")
    got = findings(lines, regions, rows, "locator", [lst])
    ok(len(got) == 1 and "VI.6.30" in got[0],
       "a standing sentence in a section no entry names is a finding (a remark section is not)")
    lst2 = dataclasses.replace(lst, entries=lst.entries[:2])
    ok(any("not the list's items" in f for f in findings(lines, regions, rows, "locator", [lst2])),
       "the data's entries must be exactly the list's items")
    lst3 = dataclasses.replace(lst, entries=(*lst.entries[:2], entry(3, ("Literal-1",), ("VI.6.24",), "prose only")))
    ok(any("says no rule states it" in f for f in findings(lines, regions, rows, "locator", [lst3])),
       "an entry declared to state no rule is a finding when a rule names it")
    lst4 = dataclasses.replace(lst, entries=(*lst.entries[:2], entry(3, ("RERUN",), ("VI.6.24",))))
    ok(any("marks no rule" in f for f in findings(lines, regions, rows, "locator", [lst4])),
       "an entry no rule of its sections names is a finding")
    lst4b = dataclasses.replace(lst, entries=(entry(1, ("AUTHOR",), ("VI.3.2",)), *lst.entries[1:]))
    shared_fmt_only = [r for r in rows if r["id"] != "SR-VI.3.2.1-1"]
    ok(any("C.1(1)" in f and "marks no rule" in f
           for f in findings(lines, regions, shared_fmt_only, "locator", [lst4b])),
       "a SHARED section's general format naming a term does not resolve an entry: the marking never marks it")
    lst5 = dataclasses.replace(lst, entries=(*lst.entries[:2], entry(3, ("literal-1",), ("VI.6.99",))))
    ok(any("no locator of the text" in f for f in findings(lines, regions, rows, "locator", [lst5])),
       "a section the text does not have is a finding")
    lst6 = dataclasses.replace(lst, entries=(*lst.entries[:2], entry(3, ("literal-1", "Literal-1"), ("VI.6.24",),
                                                                     section_terms={"VI.6.7": ("ALTER",)})))
    ok(any("gives terms to VI.6.7" in f for f in findings(lines, regions, rows, "locator", [lst6])),
       "a section's own terms must belong to one of the entry's sections")
    bad = [dict(rows[0], status="obsolete: C.1(1)"), dict(rows[6], status="obsolete: C.1(2)"),
           dict(rows[1], status="archaic: C.1(1)")]
    ok(len(marking_errors(bad, "locator", [lst], heads)) == 3,
       "a shared section's format, a rule outside the entry's sections, or a list the data lacks is a finding")
    ok(names("<u>MEMORY</u> SIZE integer-1", ["MEMORY SIZE"]) and not names("the value of x", ["VALUE OF"])
       and not names("DEBUGGING MODE", ["DEBUG"]) and names("║The DEBUG module║", ["DEBUG"]),
       "a term is a whole word or phrase as the text spells it, markup aside")

    iso = PLANTED_ISO.splitlines()
    iso_regions = citation.iso_regions(iso)
    iso_rows = [_iso_row("FMT-14.9.19.2", "14.9.19.2", "FMT", "IF condition-1 THEN NEXT SENTENCE"),
                _iso_row("SR-14.9.19.3-1", "14.9.19.3", "SR", "The ELSE NEXT SENTENCE phrase may be omitted."),
                _iso_row("SR-14.9.19.3-2", "14.9.19.3", "SR", "Condition-1 is a conditional expression."),
                _iso_row("FMT-14.9.50.2", "14.9.50.2", "FMT", "VALIDATE identifier-1"),
                _iso_row("SR-14.9.50.3-1", "14.9.50.3", "SR", "Identifier-1 shall reference a data item.")]
    archaic = ElementList("archaic", "F.1", (), (entry(1, ("EXIT PROGRAM",), ("14.9.19",),
                                                       "planted: the EXIT statement is not in the text"),
                                                 entry(2, ("NEXT SENTENCE",), ("14.9.19",))))
    obsolete = ElementList("obsolete", "F.2", (), (entry(1, ("VALIDATE",), ("14.9.50",)),))
    ok(listed(iso, iso_regions, "F.1") == {1: "The EXIT PROGRAM Statement",
                                           2: "NEXT SENTENCE phrase in the IF and SEARCH statements"},
       "an ISO list's items are read from the text (`n\\) **name.**`)")
    got = findings(iso, iso_regions, iso_rows, "section", [archaic, obsolete])
    ok(got == [], f"two lists of one edition, each with its own standing sentence, agree with the text: {got}")
    mark(iso_rows, "section", [archaic, obsolete], headings(iso, iso_regions))
    ok([r["status"] for r in iso_rows] == [NORMAL, "archaic: F.1(2)", NORMAL, "obsolete: F.2(1)", "obsolete: F.2(1)"],
       "each list confers its own standing: a SHARED archaic section, an OWN obsolete one")
    stray = ElementList("obsolete", "F.2", (), (entry(1, ("VALIDATE",), ("14.9.19",)),))
    ok(any("obsolete-element sentence in 14.9.50.1" in f
           for f in findings(iso, iso_regions, iso_rows, "section", [archaic, stray])),
       "a list's sentences are checked against that list's sections alone")
    print(f"=== ELEMENT STANDING SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] != ["--self-test"]:
        sys.exit("usage: python scripts/spec/element_standing.py --self-test  (the catalog extractor applies the lists)")
    sys.exit(self_test())
