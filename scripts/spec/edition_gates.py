#!/usr/bin/env python3
"""THE COMPILER'S EDITION GATES, HELD TO THE RULE LINEAGE'S EDGES (kb/Work PB4996; DESIGN-spec-conformance-review.md
section 10.2, slice 5).

    python scripts/spec/edition_gates.py --check        # the gate audit (EDITION GATES)
    python scripts/spec/edition_gates.py --list         # every gate, the edge it rests on and the rules that prove it
    python scripts/spec/edition_gates.py --self-test

An EDITION GATE is a place the compiler decides that a construct exists at some editions and not at others: a window of
`tests/version-matrix/constructs/` (`introducedIn`, `removedIn`) and a version predicate of the grammar
(`{is2002()}?`, a gate whose window starts at its edition and never ends). Its EDGE is the edition where the window starts
or ends. The lineage (`lineage.py`) records, between each pair of adjacent held editions, what became of each older rule:
KEPT, CHANGED or REMOVED (`inventory_schema.ChangeRecord`), and a newer rule no record names is NEW. So an edge between two
held editions is a fact the lineage can prove or refute, from the rules the gate's `citation` names:

an edge inside the pair (a START: the gate refuses at the older edition and accepts at the newer one; an END: the
reverse) rests on a cited rule that CHANGES between them: a rule of the newer edition that is NEW or that a record of a
kind whose verdict is its own (`chained` false: CHANGED) names, or a rule of the older edition whose record is of such a
kind (REMOVED, or CHANGED). Either side carries either edge: a start is a construct the newer text adds (a NEW rule, an
alternative added to a format the older text has) or a restriction the older text had and the newer one lifts (the 1985
rule "Identifier-1 must not be reference modified", REMOVED); an end is a construct the newer text drops or a restriction
it adds.

A gate whose cited rules are all KEPT contradicts its chain (the same requirement holds at both editions, yet the gate
refuses one of them) and is refused; so is a gate that cites no rule of either edition, and one resting on an older rule
the lineage has not classified yet. A gate whose window lies wholly between two held editions (2002 to 2014 while only
1985 and 2023 are held) has no edge the lineage spans, and waits for the edition between them (kb/Work PB4813).

A GATE ON A WORD'S RESERVATION is not a rule's edge. A construct whose every citation names a reserved or context-sensitive
word list (§2023:8.9, §2023:8.10: `WORD_LISTS`) is a word becoming or ceasing to be reserved, whose edges are the word
lists' (`reserved-words.json`, `context-sensitive-words.json`, held by CobolWordsDriftTests and
ContextSensitiveWordsDriftTests), and this audit leaves it to them.

The cited rules are read through the one citation parser (`citation.scan`); a clause names every rule under it, a rule
designator (`SR4`, `GR12`) the rule of that kind and ordinal, a `Format n` the clause's general format (its FMT rule: the
catalog does not number formats) and a `Table n` every rule of the clause. What a kind means is the schema's
(`ChangeRecord`): no code here spells one.

A CITATION PROVES AN EDGE ONLY WHEN EVERY RULE IT NAMES PROVES IT. A clause that gained anything after 1985 holds NEW or
CHANGED rules beside KEPT ones, so a gate citing the whole clause would pass whichever of its constructs it moved: a window
that wrongly started CALL ... USING at 2002 citing `§2023:14.9.4` would be proven by CALL's BY VALUE rules. So a gate is
proven by one of its citations whose every named rule changes, and a gate whose citations each mix changing rules with
KEPT ones is refused until it names the SR or GR its edge rests on.

A GENERAL FORMAT PROVES ONLY A WHOLE DIAGRAM'S EDGE. The catalog holds one FMT rule per clause, every format and alternative
of the statement in it, and most general formats changed between 1985 and 2023; so a CHANGED one would prove any construct
of its statement (`Format 1` of CALL proves CALL ... USING at 2002 as readily as CALL ... RETURNING). A general format
proves an edge only when the whole diagram is absent on the edge's other side: a NEW newer FMT rule (the statement is new)
a start, an older one REMOVED an end (`diagram_bound`). A construct the standard states only in its diagrams, with no SR
or GR of its own, therefore has no edge the lineage proves; each such gate is HELD OPEN (`FORMAT_ONLY`), so a new window
resting on a general format alone is still refused.

THE DIRECTION OF AN SR OR GR IS NOT THE LINEAGE'S. A NEW rule is a construct the newer text adds or a restriction it adds,
and a REMOVED rule a construct it drops or a restriction it lifts; the record does not say which, so an SR or GR that
changes proves either edge, and an inverted window (`introducedIn` and `removedIn` swapped) citing one is not caught here
(the version matrix runs each construct's program at the editions around its window). A general format has a direction:
a NEW diagram proves only a start and a REMOVED one only an end.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import re
import sys
from collections import defaultdict
from collections.abc import Callable, Iterable, Mapping
from typing import Any, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (the one citation parser)
import construct_store  # noqa: E402  (the construct catalogue, one file per construct, kb/Work PB5110)
import inventory_view  # noqa: E402  (the one view of each held edition)
from inventory_schema import REPO, Edition, Schema, load_schema  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

CONSTRUCTS_REL = construct_store.STORE_REL
GRAMMAR_REL = "src/WiseOwl.Cobol.Frontend/Grammar"
#: The clauses of the 2023 text that list the reserved words and the context-sensitive words, as citations: a gate citing
#: only these is a word's reservation, whose edges are the word lists' (see the module docstring).
WORD_LISTS = frozenset({"§2023:8.9", "§2023:8.10"})
#: A version predicate of the grammar: `{is2002()}?`, alone or in a disjunction (`{is2002() || retryPhraseAhead()}?`).
_PREDICATE = re.compile(r"\{[^{}]*\bis(\d+)\(\)[^{}]*\}\?")
_RULE = re.compile(r"(SR|GR|AR|RV)(\d+)")
#: A citation of a clause's general format (`Format 2`), which names the clause's rule of the catalog kind FMT.
_FORMAT = re.compile(r"Format\b")
_FORMAT_KIND = "FMT"
#: THE FINDINGS A KNOWN DEFECT HOLDS OPEN, each with the work-register note that carries its fix; checked both ways
#: (`held_open`): an entry whose gate the audit no longer finds anything at is itself a finding. A gate lands here only
#: when the audit has found a defect, of the compiler or of the data it reads (the catalog, the lineage), whose fix is
#: another note's, never to quiet a finding this audit got wrong.
#: The gates whose construct the standard states only in a general format, with no SR or GR naming it (the module
#: docstring); each was searched for a rule of its own (branch review R2-1, wave 20261935 C). The fix is a catalog that
#: numbers each format's alternatives, which no note carries yet (the report's lead names it for the registrar).
FORMAT_ONLY = "the catalog holds the general format as one rule and no SR or GR names this construct (lead R2-1, w20261935 C)"
HELD_OPEN: dict[str, str] = {f"constructs/{c}": FORMAT_ONLY for c in (
    "end-accept-2002", "end-display-2002",                   # a scope terminator; §2023:14.5.3.2 GR1 is KEPT
    "identification-header-optional-2002",                   # the bracketed header of §2023:11.2.1
    "computer-name-optional-2002",                           # §2023:12.3.6.2
    "relative-key-clause-alone-2002",                        # §2023:12.4.5.13.2, a clause of its own
    "file-organization-record-sequential-2023",              # the RECORD word; §2023:12.4.5.10.3 GR3 is KEPT
    "initialize-then-replacing-2002",                        # the THEN connective of §2023:14.9.20.2
    "inline-perform-after-2002",                             # §2023:14.9.28.2 Format 2
    "set-condition-repeated-2002",                           # §2023:14.9.39.2
    "use-without-procedure-2002", "use-without-after-2002",  # optional words of §2023:14.9.49.2
)}
_COMMENT = "//"

Row = dict[str, Any]


class Gate(NamedTuple):
    """One edition gate: where it is, the first edition it accepts, the first it refuses again (None: it never ends),
    and the citations that ground it."""
    where: str
    start: int
    end: int | None
    cites: tuple[citation.Citation, ...]


class Pair(NamedTuple):
    """Two adjacent held editions and the lineage between them: the older edition's catalog rules and each one's record
    kind (None while unclassified), and for each newer rule the kinds of the records that name it (absent: NEW)."""
    older: Edition
    newer: Edition
    older_rules: tuple[Row, ...]
    newer_rules: tuple[Row, ...]
    kind_of: Mapping[str, str | None]
    reached_by: Mapping[str, frozenset[str]]


# -- the gates ----------------------------------------------------------------------------------------------------------

def construct_gates(rows: Iterable[Mapping[str, Any]], year_of: Mapping[str, int]) -> list[Gate]:
    """Every window of the construct catalogue `rows` (tests/version-matrix/constructs/) as a gate, its edges as years: `year_of` maps the
    catalogue's edition spelling (`85`, `2002`) to its edition's year, as a grammar predicate's (`grammar_gates`)."""
    def year(spelling: Any) -> int:
        if str(spelling) not in year_of:
            raise SystemExit(f"{CONSTRUCTS_REL}: edition {spelling!r} names no edition of the schema")
        return year_of[str(spelling)]
    return [Gate(f"constructs/{c['id']}", year(c["introducedIn"]), year(c["removedIn"]) if c.get("removedIn") else None,
                 tuple(x for x in citation.scan(c.get("citation") or "") if x.error is None))
            for c in rows]


def grammar_gates(files: Mapping[str, str], year_of: Mapping[str, int]) -> list[Gate]:
    """Every version predicate in the grammar files `files` (relative path -> text) as a gate starting at its edition.
    Its citations are those of its own rule up to its line: the rule's leading comment block, its header and the
    alternatives above it (back to the blank line or the `;` that ends the rule before it). `year_of` maps a predicate's
    spelling (`2002`, `85`) to its edition's year."""
    gates: list[Gate] = []
    for rel, text in sorted(files.items()):
        lines = text.splitlines()
        for i, line in enumerate(lines):
            code = line.split(_COMMENT, 1)[0]
            for m in _PREDICATE.finditer(code):
                if m[1] not in year_of:
                    raise SystemExit(f"{rel}:{i + 1}: is{m[1]}() names no edition of the schema")
                j = i
                while j > 0 and lines[j - 1].strip() and not lines[j - 1].split(_COMMENT, 1)[0].rstrip().endswith(";"):
                    j -= 1
                cites = tuple(x for x in citation.scan("\n".join(lines[j:i + 1])) if x.error is None)
                gates.append(Gate(f"{rel}:{i + 1}", year_of[m[1]], None, cites))
    return gates


# -- the lineage between adjacent held editions --------------------------------------------------------------------------

def pair_of(older: Edition, newer: Edition, older_rows: Iterable[Row], newer_rows: Iterable[Row],
            schema: Schema) -> Pair:
    """The lineage between `older` and the next held edition `newer`, from their views' rows."""
    olds, news = tuple(older_rows), tuple(newer_rows)
    kind_of: dict[str, str | None] = {}
    reached: dict[str, set[str]] = defaultdict(set)
    for r in olds:
        nxt = r.get("next") or {}
        kind_of[r["rule-id"]] = nxt.get("kind") or None
        for t in nxt.get("to") or []:
            reached[t].add(nxt["kind"])
    return Pair(older, newer, olds, news, kind_of, {t: frozenset(k) for t, k in reached.items()})


def document_year(rule: Row, ed: Edition) -> int:
    """The edition a citation of `rule` names: an older catalog prefixes each id with its document's year (1985, and the
    1989 and 1993 amendments it holds); the newest catalog's ids carry none."""
    head = rule["rule-id"].split("-", 1)[0]
    return int(head) if head.isdigit() else ed.year


def named_rules(c: citation.Citation, rules: Iterable[Row], ed: Edition) -> list[str]:
    """The ids of `rules` (one held edition's catalog) that the citation `c` names: a clause every rule under it, a rule
    designator the rule of that kind and ordinal there, a `Format n` the clause's FMT rule (the module docstring)."""
    under = [r for r in rules if document_year(r, ed) == c.edition
             and (r["section"] == c.locator or r["section"].startswith(c.locator + "."))]
    if c.rule and _FORMAT.match(c.rule):
        return [r["rule-id"] for r in under if r["kind"] == _FORMAT_KIND]
    m = _RULE.match(c.rule) if c.rule else None
    return [r["rule-id"] for r in under if m is None or (r["kind"] == m[1] and r["ordinal"] == int(m[2]))]


def cited_rules(cites: Iterable[citation.Citation], rules: Iterable[Row], ed: Edition) -> list[str]:
    """The ids of `rules` that any of `cites` names (`named_rules`), each once."""
    rules = list(rules)
    return list(dict.fromkeys(t for c in cites for t in named_rules(c, rules, ed)))


def word_gate(g: Gate) -> bool:
    """Does `g` cite only the word lists (`WORD_LISTS`): a word's reservation, not a rule's edge?"""
    return bool(g.cites) and all(citation.render(dataclasses.replace(c, rule=None)) in WORD_LISTS for c in g.cites)


# -- the audit ----------------------------------------------------------------------------------------------------------

class Finding(NamedTuple):
    where: str
    text: str

    def __str__(self) -> str:
        return f"{self.where}: {self.text}"


def edge_findings(g: Gate, p: Pair, schema: Schema) -> tuple[list[Finding], str | None]:
    """What the lineage `p` says of gate `g`'s edges inside it: the findings, and how the edge is proven (None when `g`
    has no edge inside the pair). A citation proves the edge when every rule it names changes between the pair, on
    either side (the module docstring)."""
    lo, hi = p.older.year, p.newer.year
    chained = schema.next.chained.name
    starts = lo < g.start <= hi and (g.end is None or g.end > hi)
    ends = g.start <= lo and g.end is not None and lo < g.end <= hi
    if not (starts or ends):
        return [], None
    if word_gate(g):
        return [], "a word's reservation (the word lists hold it)"
    edge = f"start {g.start}" if starts else f"end {g.end}"
    side_of = {document_year(r, ed): (ed, rules, how)
               for ed, rules, how in ((p.older, p.older_rules, older_change(p, chained)),
                                      (p.newer, p.newer_rules, newer_change(p, chained)))
               for r in rules}
    cites = [c for c in g.cites if c.edition in side_of]
    if not cites:
        return [Finding(g.where, f"{edge}, between {lo} and {hi}, cites nothing of {lo} or {hi}: name the rule that "
                                 f"changes at the edge (§<edition>:<locator> <rule>)")], None
    named: dict[str, tuple[list[str], Callable[[str], str | None], Callable[[str], str | None]]] = {}
    for c in cites:
        ed, rules, how = side_of[c.edition]
        if ids := named_rules(c, rules, ed):
            named[citation.render(c)] = (ids, how, diagram_bound(how, rules, ed is p.newer, starts, schema))
    if not named:
        return [], f"{edge}: prose the catalogs hold no rule of (" + ", ".join(
            citation.render(c) for c in cites[:3]) + "), outside the lineage"
    if proofs := [cite for cite, (ids, _, proves) in named.items() if all(proves(t) for t in ids)]:
        ids, _, proves = named[proofs[0]]
        return [], f"{edge}: {proofs[0]}: " + ", ".join(f"{t} {proves(t)}" for t in ids[:4])
    return unproven(g.where, f"{edge}, between {lo} and {hi},", named, p, chained), None


def unproven(where: str, edge: str, named: Mapping[str, tuple[list[str], Callable[[str], str | None],
                                                              Callable[[str], str | None]]],
             p: Pair, chained: str) -> list[Finding]:
    """The finding for an edge no citation of `named` (each citation -> the rules it names, how each changes, and whether
    it proves the edge: `edge_findings`) proves alone: the most specific reason, broadest first."""
    cited = [t for ids, _, _ in named.values() for t in ids]
    if broad := [t for ids, _, proves in named.values() for t in ids if proves(t)]:
        return [Finding(where, f"{edge} is proven by no citation alone: each it makes ({'; '.join(named)}) names "
                               f"{chained}, unclassified or general-format rules beside the changing ones, so it would prove "
                               f"any construct of its clause; name the SR or GR the edge rests on (candidates "
                               f"{', '.join(broad[:6])})")]
    if pending := [t for t in cited if t in p.kind_of and p.kind_of[t] is None]:
        return [Finding(where, f"{edge} resting on {p.older.year} rule(s) the lineage has not classified "
                               f"({', '.join(pending[:6])}): classify them (record_verdicts.py --edition {p.older.year})")]
    if kept := [t for ids, how, _ in named.values() for t in ids if not how(t)]:
        return [Finding(where, f"{edge} yet every rule it cites that is not a general format is {chained} ({', '.join(kept[:6])}): "
                               f"the same requirement holds at both, so either the gate or the change record is wrong")]
    return [Finding(where, f"{edge} rests only on a general format that changes ({', '.join(cited[:4])}): the catalog "
                           f"holds one FMT rule per clause, every format and alternative of it, so it proves no one "
                           f"construct (only a whole diagram NEW proves a start, REMOVED an end); name the SR or GR the "
                           f"construct rests on")]


def diagram_bound(how: Callable[[str], str | None], rules: Iterable[Row], newer: bool, starts: bool,
                  schema: Schema) -> Callable[[str], str | None]:
    """`how` (one side's change of a rule) narrowed for its general-format rules: the catalog holds ONE FMT rule per
    clause, the whole diagram with every format and alternative, so a CHANGED one proves no construct of it. It proves an
    edge only when the whole diagram is absent on the edge's other side: a NEW newer FMT rule a start, an older one whose
    record names no successor (`to` false: REMOVED) an end."""
    formats = {r["rule-id"] for r in rules if r["kind"] == _FORMAT_KIND}
    kinds = schema.next.kinds

    def bound(t: str) -> str | None:
        kind = how(t)
        if t not in formats or kind is None:
            return kind
        whole = kind == "NEW" if newer else kind in kinds and not kinds[kind].to
        return kind if whole and starts == newer else None
    return bound


def newer_change(p: Pair, chained: str) -> Callable[[str], str | None]:
    """How a newer rule of `p` changes: NEW (no record names it), or the kinds of the records naming it whose verdict is
    their own; None when only `chained` records name it."""
    def how(t: str) -> str | None:
        return "NEW" if t not in p.reached_by else "/".join(sorted(p.reached_by[t] - {chained})) or None
    return how


def older_change(p: Pair, chained: str) -> Callable[[str], str | None]:
    """How an older rule of `p` changes: its record's kind, when it is classified and not `chained`."""
    def how(t: str) -> str | None:
        kind = p.kind_of.get(t)
        return kind if kind not in (None, chained) else None
    return how


def audit(gates: Iterable[Gate], pairs: Iterable[Pair], schema: Schema) -> tuple[list[Finding], dict[str, list[str]]]:
    """Every gate against every adjacent pair of held editions: the findings, and each gate's proven edges."""
    pairs = list(pairs)
    findings: list[Finding] = []
    proven: dict[str, list[str]] = defaultdict(list)
    for g in gates:
        for p in pairs:
            f, how = edge_findings(g, p, schema)
            findings += f
            if how:
                proven[g.where].append(how)
    return findings, dict(proven)


def repository_inputs(schema: Schema, root: pathlib.Path = REPO) -> tuple[list[Gate], list[Pair]]:
    """The repository's gates (the construct catalogue and the grammar) and the lineage between each adjacent pair of its held
    editions, read through the one view."""
    year_of = {e.spelling: e.year for e in schema.all_editions}
    grammar = {p.relative_to(root).as_posix(): p.read_text(encoding="utf-8")
               for p in sorted((root / GRAMMAR_REL).rglob("*.g4"))}
    gates = (construct_gates(construct_store.read_rows(root), year_of)
             + grammar_gates(grammar, year_of))
    held = [e for e in schema.all_editions if e.held]
    views = {e.year: inventory_view.inventory(e.year, schema) for e in held}
    pairs = [pair_of(e, schema.edition(e.next_held), views[e.year], views[e.next_held], schema)
             for e in held if e.next_held is not None]
    return gates, pairs


def held_open(findings: Iterable[Finding], held: Mapping[str, str]) -> tuple[list[Finding], list[Finding]]:
    """`findings` split into those a `held` entry keeps open (each a defect the work register carries) and the rest, with
    a finding for each `held` entry that no longer finds anything (the defect was fixed: retire the entry)."""
    findings = list(findings)
    kept = [f for f in findings if f.where in held]
    stale = [Finding(w, f"is held open ({why}) but the audit finds nothing there now: delete its HELD_OPEN entry")
             for w, why in held.items() if w not in {f.where for f in findings}]
    return kept, [f for f in findings if f.where not in held] + stale


def check() -> int:
    schema = load_schema()
    gates, pairs = repository_inputs(schema)
    findings, proven = audit(gates, pairs, schema)
    kept, findings = held_open(findings, HELD_OPEN)
    windows = sum(1 for g in gates if g.where.startswith("constructs/"))
    print(f"edition gates: {len(gates)} ({windows} construct windows, {len(gates) - windows} grammar predicates); "
          f"{len(proven)} with an edge the held editions span, each proven by the lineage or left to the word lists")
    for f in kept:
        print(f"  ⧗ held open ({HELD_OPEN[f.where]}): {f}")
    for f in findings:
        print(f"  ✗ {f}")
    print(f"=== EDITION GATES: {'GREEN' if not findings else f'RED — {len(findings)} finding(s)'} ===")
    return 1 if findings else 0


def listing() -> int:
    schema = load_schema()
    gates, pairs = repository_inputs(schema)
    findings, proven = audit(gates, pairs, schema)
    bad = {f.where for f in findings}
    for g in gates:
        mark = "✗" if g.where in bad else ("·" if g.where not in proven else "✓")
        print(f"{mark} {g.where} [{g.start}, {g.end or '-'}) " + "; ".join(proven.get(g.where, [])))
    return 0


# -- self-test ----------------------------------------------------------------------------------------------------------

def self_test() -> int:
    import copy  # noqa: PLC0415
    from inventory_schema import SCHEMA_PATH  # noqa: PLC0415
    schema = load_schema()
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    e85, e23 = schema.edition(1985), schema.edition(2023)
    old = [{"rule-id": "1985-SR-VI.6.19.3-1", "section": "VI.6.19.3", "kind": "SR", "ordinal": 1,
            "next": {"kind": "KEPT", "to": ["SR-14.9.1.3-1"]}},
           {"rule-id": "1985-GR-VI.6.24.4-4", "section": "VI.6.24.4", "kind": "GR", "ordinal": 4,
            "next": {"kind": "REMOVED", "to": []}},
           {"rule-id": "1985-FMT-VI.6.15.2", "section": "VI.6.15.2", "kind": "FMT", "ordinal": 0,
            "next": {"kind": "CHANGED", "to": ["FMT-14.9.2.2"]}},
           {"rule-id": "1985-GR-VI.6.9.4-1", "section": "VI.6.9.4", "kind": "GR", "ordinal": 1, "next": {}},
           {"rule-id": "1985-GR-VI.6.15.4-2", "section": "VI.6.15.4", "kind": "GR", "ordinal": 2,
            "next": {"kind": "CHANGED", "to": ["FMT-14.9.2.2"]}},
           {"rule-id": "1985-FMT-VI.6.30.2", "section": "VI.6.30.2", "kind": "FMT", "ordinal": 0,
            "next": {"kind": "REMOVED", "to": []}},
           {"rule-id": "1989-AR-A.2.7.3-1", "section": "A.2.7.3", "kind": "AR", "ordinal": 1,
            "next": {"kind": "KEPT", "to": ["AR-15.10.3-1"]}}]
    new = [{"rule-id": "SR-14.9.1.3-1", "section": "14.9.1.3", "kind": "SR", "ordinal": 1},
           {"rule-id": "FMT-14.9.2.2", "section": "14.9.2.2", "kind": "FMT", "ordinal": 0},
           {"rule-id": "GR-14.9.3.4-1", "section": "14.9.3.4", "kind": "GR", "ordinal": 1},
           {"rule-id": "AR-15.10.3-1", "section": "15.10.3", "kind": "AR", "ordinal": 1},
           {"rule-id": "FMT-13.18.99.2", "section": "13.18.99.2", "kind": "FMT", "ordinal": 0}]
    p = pair_of(e85, e23, old, new, schema)

    def gate(start: int, end: int | None, text: str) -> Gate:
        return Gate("planted", start, end, tuple(citation.scan(text)))

    def finds(g: Gate) -> list[str]:
        return [f.text for f in edge_findings(g, p, schema)[0]]

    ok(p.reached_by["SR-14.9.1.3-1"] == {"KEPT"} and "GR-14.9.3.4-1" not in p.reached_by
       and p.kind_of["1985-GR-VI.6.9.4-1"] is None, "the pair reads each record's kind and the rules it names")
    ok(cited_rules(citation.scan("§2023:14.9 and §1985:VI.6.19.3 SR1"), new, e23) == [r["rule-id"] for r in new[:3]]
       and cited_rules(citation.scan("§1985:VI.6.19.3 SR1"), old, e85) == ["1985-SR-VI.6.19.3-1"]
       and cited_rules(citation.scan("§1985:VI.6.19.3 SR2"), old, e85) == []
       and cited_rules(citation.scan("§1989:A.2.7.3"), old, e85) == ["1989-AR-A.2.7.3-1"]
       and cited_rules(citation.scan("§1985:A.2.7.3"), old, e85) == [],
       "a clause names every rule under it, a designator its kind and ordinal, each in its own document's numbering")
    ok(cited_rules(citation.scan("§2023:14.9.2 Format 1"), new, e23) == ["FMT-14.9.2.2"],
       "a Format n names the clause's general format, its FMT rule")
    ok(finds(gate(2002, None, "§2023:14.9.3.4 GR1")) == []
       and edge_findings(gate(2002, None, "§2023:14.9.3.4 GR1"), p, schema)[1]
       == "start 2002: §2023:14.9.3.4 GR1: GR-14.9.3.4-1 NEW",
       "a start rests on a NEW rule, or on one a CHANGED record names")
    ok(all(any("rests only on a" in f for f in finds(g))
           for g in (gate(2014, None, "§2023:14.9.2 Format 1"), gate(2014, None, "§2023:14.9.2.2"),
                     gate(2002, None, "§1985:VI.6.15.2"), gate(1985, 2023, "§1985:VI.6.15.2"))),
       "a CHANGED general format proves no edge, of either edition: the catalog's one FMT rule holds every format and "
       "alternative of its clause, so it would prove any construct of the statement (branch review R2-1)")
    ok(finds(gate(2002, None, "§2023:13.18.99.2")) == [] and finds(gate(1985, 2002, "§1985:VI.6.30.2")) == []
       and any("rests only on a" in f for f in finds(gate(1985, 2002, "§2023:13.18.99.2")))
       and any("rests only on a" in f for f in finds(gate(2002, None, "§1985:VI.6.30.2"))),
       "a whole diagram's edge is proven by its general format, with its direction: a NEW one proves only a start, a "
       "REMOVED one only an end")
    ok(any("proven by no citation alone" in f for f in finds(gate(2002, None, "§2023:14.9")))
       and any("proven by no citation alone" in f for f in finds(gate(1985, 2023, "§1985:VI.6")))
       and finds(gate(2002, None, "§2023:14.9; §2023:14.9.3.4 GR1")) == [],
       "a citation naming KEPT rules beside the changing ones proves nothing (it would prove any construct of its clause); one "
       "citation whose every rule proves the edge does")
    ok(any("is KEPT (" in f for f in finds(gate(2002, None, "§2023:14.9.1.3 SR1")))
       and any("is KEPT (" in f for f in finds(gate(2002, None, "§1985:VI.6.19.3 SR1"))),
       "a start whose every cited rule, of either edition, is KEPT contradicts its chain")
    ok(any("cites nothing of 1985 or 2023" in f for f in finds(gate(2002, None, "ISO 14.9.3")))
       and any("cites nothing of 1985 or 2023" in f for f in finds(gate(1985, 2002, "ISO 14.9.3"))),
       "an edge citing nothing of either held edition is refused (a bare clause number is no citation)")
    ok(edge_findings(gate(2002, None, "§2023:6.2.3.1"), p, schema)
       == ([], "start 2002: prose the catalogs hold no rule of (§2023:6.2.3.1), outside the lineage"),
       "a start citing only prose the catalog holds no rule of rests on its citation (the CITATIONS audit checks it)")
    ok(finds(gate(2002, None, "§1985:VI.6.24.4 GR4")) == [] and finds(gate(2002, None, "§1985:VI.6.15.4 GR2")) == [],
       "a start rests on an older rule REMOVED or CHANGED too: a restriction the newer text lifts")
    ok(finds(gate(1985, 2002, "§1985:VI.6.24.4 GR4")) == [] and finds(gate(1985, 2023, "§1985:VI.6.15.4 GR2")) == []
       and finds(gate(1985, 2002, "§2023:14.9.3.4 GR1")) == [],
       "an end rests on an older rule REMOVED, or CHANGED, or on a NEW rule: a restriction the newer text adds")
    ok(any("is KEPT (" in f for f in finds(gate(1985, 2014, "§1985:VI.6.19.3 SR1"))),
       "an end whose every cited rule is KEPT contradicts its chain")
    ok(any("has not classified" in f for f in finds(gate(1985, 2002, "§1985:VI.6.9.4 GR1"))),
       "an edge resting on an unclassified rule waits for its classification, refused")
    ok(edge_findings(gate(2002, 2014, "nothing"), p, schema) == ([], None)
       and edge_findings(gate(1985, None, "nothing"), p, schema) == ([], None)
       and edge_findings(gate(2023, None, "§2023:14.9.3.4 GR1"), p, schema)[1] is not None,
       "a window wholly between held editions, or open at both ends, has no edge the pair spans")
    ok(edge_findings(gate(1985, 2002, "§2023:8.9 interval"), p, schema) == ([], "a word's reservation (the word lists hold it)")
       and not word_gate(gate(1985, 2002, "§2023:8.9; §2023:14.9.3")),
       "a gate citing only the word lists is a word's reservation, left to them")
    found = [Finding("a", "x"), Finding("b", "y")]
    kept, rest = held_open(found, {"a": "PB1", "c": "PB2"})
    ok(kept == [Finding("a", "x")] and [f.where for f in rest] == ["b", "c"] and "delete its HELD_OPEN entry" in rest[1].text,
       "a finding a known defect holds open is kept apart, and an entry that finds nothing any more is itself a finding")
    # The kinds are DATA: a kind the schema alone adds, whose verdict is its own, proves an edge with no code change.
    raw = copy.deepcopy(json.loads(SCHEMA_PATH.read_text(encoding="utf-8")))
    raw["next"]["kinds"]["SPLIT"] = {"meaning": "planted", "to": True, "reason": True, "chained": False}
    s_split = Schema(raw)
    p_split = pair_of(e85, e23, [dict(old[0], next={"kind": "SPLIT", "to": ["SR-14.9.1.3-1"]})], new, s_split)
    ok(edge_findings(gate(2002, None, "§2023:14.9.1.3 SR1"), p_split, s_split)[0] == [],
       "a kind the schema alone adds is read from its flags")
    grammar = {"g/A.g4": "// the X phrase (§2023:14.9.3.4 GR1)\nrule : {is2002()}? x\n | y // {is2023()}? in a comment\n"
                         "    ;\nother : ({is2023() || ahead()}? z)? ;\n"}
    gg = grammar_gates(grammar, {"85": 1985, "2002": 2002, "2023": 2023})
    ok([(g.where, g.start, [c.locator for c in g.cites]) for g in gg] == [("g/A.g4:2", 2002, ["14.9.3.4"]),
                                                                          ("g/A.g4:5", 2023, [])],
       "a grammar predicate is a gate at its edition, citing its rule's comment and lines above it; a comment is no gate")
    grammar = {"g/B.g4": "// the Y phrase (§2023:14.9.2.2)\nrule\n    : a\n    | {is2014()}? b\n    ;\n"}
    gg = grammar_gates(grammar, {"2014": 2014})
    ok([(g.where, g.start, [c.locator for c in g.cites]) for g in gg] == [("g/B.g4:4", 2014, ["14.9.2.2"])],
       "a predicate on a later alternative reads its rule's leading comment, across the header and the alternatives")
    year_of = {e.spelling: e.year for e in schema.all_editions}
    construct = construct_gates([{"id": "c", "introducedIn": 85, "removedIn": 2002,
                                      "citation": "§2023:14.9.3.4 GR1; §5000:1 malformed"}],  # citation-audit: example
                                year_of)
    ok(construct == [Gate("constructs/c", 1985, 2002, construct[0].cites)] and len(construct[0].cites) == 1,
       "a construct's window is a gate in years (85 is 1985) citing what its citation names, a malformed citation "
       "dropped (CITATIONS refuses it)")
    print(f"=== EDITION GATES SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The compiler's edition gates held to the rule lineage (kb/Work PB4996).")
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true")
    mode.add_argument("--list", action="store_true")
    mode.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    return self_test() if a.self_test else listing() if a.list else check()


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
