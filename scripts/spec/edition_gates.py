#!/usr/bin/env python3
"""THE COMPILER'S EDITION GATES, HELD TO THE RULE LINEAGE'S EDGES (kb/Work PB4996; DESIGN-spec-conformance-review.md
section 10.2, slice 5).

    python scripts/spec/edition_gates.py --check        # the gate audit (EDITION GATES)
    python scripts/spec/edition_gates.py --list         # every gate, the edge it rests on and the rules that prove it
    python scripts/spec/edition_gates.py --self-test

An EDITION GATE is a place the compiler decides that a construct exists at some editions and not at others: a window of
`tests/version-matrix/constructs.json` (`introducedIn`, `removedIn`) and a version predicate of the grammar
(`{is2002()}?`, a gate whose window starts at its edition and never ends). Its EDGE is the edition where the window starts
or ends. The lineage (`lineage.py`) records, between each pair of adjacent held editions, what became of each older rule:
KEPT, CHANGED or REMOVED (`inventory_schema.ChangeRecord`), and a newer rule no record names is NEW. So an edge between two
held editions is a fact the lineage can prove or refute, from the rules the gate's `citation` names:

- a START inside the pair (the gate refuses at the older edition and accepts at the newer one) rests on a cited rule of the
  newer edition that is NEW, or that a record of a kind whose verdict is its own (`chained` false: CHANGED) names: the
  construct is new, or is the change a kept subject underwent (an alternative added to a format the older text has);
- an END inside the pair (the gate accepts at the older edition and refuses at the newer one) rests on a cited rule of the
  older edition whose record is of such a kind (REMOVED, or CHANGED: an alternative the newer format dropped).

A gate whose cited rules are all KEPT contradicts its chain (the same requirement holds at both editions, yet the gate
refuses one of them) and is refused; so is a gate that cites no rule of the edition its edge needs, and an end resting on
an older rule the lineage has not classified yet. A gate whose window lies wholly between two held editions (2002 to 2014
while only 1985 and 2023 are held) has no edge the lineage spans, and waits for the edition between them (kb/Work PB4813).

A GATE ON A WORD'S RESERVATION is not a rule's edge. A construct whose every citation names a reserved or context-sensitive
word list (§2023:8.9, §2023:8.10: `WORD_LISTS`) is a word becoming or ceasing to be reserved, whose edges are the word
lists' (`reserved-words.json`, `context-sensitive-words.json`, held by CobolWordsDriftTests and
ContextSensitiveWordsDriftTests), and this audit leaves it to them.

The cited rules are read through the one citation parser (`citation.scan`); a clause names every rule under it, a rule
designator (`SR4`, `GR12`) the rule of that kind and ordinal, and a `Format n` or `Table n` every rule of the clause (the
catalog does not number formats). What a kind means is the schema's (`ChangeRecord`): no code here spells one.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import defaultdict
from collections.abc import Iterable, Mapping
from typing import Any, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (the one citation parser)
import inventory_view  # noqa: E402  (the one view of each held edition)
from inventory_schema import REPO, Edition, Schema, load_schema  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

CONSTRUCTS_REL = "tests/version-matrix/constructs.json"
GRAMMAR_REL = "src/WiseOwl.Cobol.Frontend/Grammar"
#: The clauses of the 2023 text that list the reserved words (8.9) and the context-sensitive words (8.10): a gate citing
#: only these is a word's reservation, whose edges are the word lists' (see the module docstring).
WORD_LISTS = frozenset({"8.9", "8.10"})
#: A version predicate of the grammar: `{is2002()}?`, alone or in a disjunction (`{is2002() || retryPhraseAhead()}?`).
_PREDICATE = re.compile(r"\{[^{}]*\bis(\d+)\(\)[^{}]*\}\?")
_RULE = re.compile(r"(SR|GR|AR|RV)(\d+)")
#: THE FINDINGS A KNOWN DEFECT HOLDS OPEN, each with the work-register note that carries its fix; checked both ways
#: (`held_open`): an entry whose gate the audit no longer finds anything at is itself a finding. A gate lands here only
#: when the audit has found a compiler defect whose fix is another note's, never to quiet a finding this audit got wrong.
HELD_OPEN: dict[str, str] = {
    # ANSI X3.23-1985 deleted REMARKS (§1985:A, its history of COBOL, item 8: "The deletion of NOTE and REMARKS in
    # favor of a general comment capability for all divisions"), so the window that accepts the REMARKS paragraph at
    # --std 85 accepts what the 1985 text does not have (the open question of kb/Work PB1802 item 2, answered by the text).
    "constructs.json remarks-removed-2002": "kb/Work PB4996 lead: REMARKS accepted at --std 85, a leniency for CCVS",
}
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

def construct_gates(raw: Mapping[str, Any]) -> list[Gate]:
    """Every window of the construct catalogue `raw` (constructs.json) as a gate."""
    return [Gate(f"constructs.json {c['id']}", int(c["introducedIn"]), int(c["removedIn"]) if c.get("removedIn") else None,
                 tuple(x for x in citation.scan(c.get("citation") or "") if x.error is None))
            for c in raw["constructs"]]


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


def cited_rules(cites: Iterable[citation.Citation], rules: Iterable[Row], ed: Edition) -> list[str]:
    """The ids of `rules` (one held edition's catalog) that `cites` name: a clause every rule under it, a rule designator
    the rule of that kind and ordinal there."""
    rules = list(rules)
    out: list[str] = []
    for c in cites:
        under = [r for r in rules if document_year(r, ed) == c.edition
                 and (r["section"] == c.locator or r["section"].startswith(c.locator + "."))]
        m = _RULE.fullmatch(re.sub(r"[a-z].*$", "", c.rule or "")) if c.rule else None
        out += [r["rule-id"] for r in under if m is None or (r["kind"] == m[1] and r["ordinal"] == int(m[2]))]
    return list(dict.fromkeys(out))


def word_gate(g: Gate) -> bool:
    """Does `g` cite only the word lists (`WORD_LISTS`): a word's reservation, not a rule's edge?"""
    return bool(g.cites) and all(c.edition == 2023 and c.locator in WORD_LISTS for c in g.cites)


# -- the audit ----------------------------------------------------------------------------------------------------------

class Finding(NamedTuple):
    where: str
    text: str

    def __str__(self) -> str:
        return f"{self.where}: {self.text}"


def edge_findings(g: Gate, p: Pair, schema: Schema) -> tuple[list[Finding], str | None]:
    """What the lineage `p` says of gate `g`'s edges inside it: the findings, and how the edge is proven (None when `g`
    has no edge inside the pair)."""
    lo, hi = p.older.year, p.newer.year
    chained = schema.next.chained.name
    starts = lo < g.start <= hi and (g.end is None or g.end > hi)
    ends = g.start <= lo and g.end is not None and lo < g.end <= hi
    if not (starts or ends):
        return [], None
    if word_gate(g):
        return [], "a word's reservation (the word lists hold it)"
    ed, rules = (p.newer, p.newer_rules) if starts else (p.older, p.older_rules)
    edge = f"start {g.start}" if starts else f"end {g.end}"
    years = {document_year(r, ed) for r in rules}
    cites = [c for c in g.cites if c.edition in years]
    if not cites:
        owed = "the construct is NEW or CHANGED in" if starts else "whose change record removes or changes the construct"
        return [Finding(g.where, f"{edge}, between {lo} and {hi}, cites nothing of {ed.year}: name the {ed.year} rule "
                                 f"{owed} (§{ed.year}:<locator> <rule>)")], None
    cited = cited_rules(cites, rules, ed)
    if not cited:
        return [], f"{edge}: prose the {ed.year} catalog holds no rule of (" + ", ".join(
            citation.render(c) for c in cites[:3]) + "), outside the lineage"
    if starts:
        proof = [t for t in cited if t not in p.reached_by or p.reached_by[t] - {chained}]
        if not proof:
            return [Finding(g.where, f"{edge}, yet every {hi} rule it cites is {chained} from {lo} "
                                     f"({', '.join(cited[:6])}): the same requirement holds at {lo}, so either the gate "
                                     f"or the change record is wrong")], None
        return [], f"{edge}: " + ", ".join(
            f"{t} {'NEW' if t not in p.reached_by else '/'.join(sorted(p.reached_by[t] - {chained}))}" for t in proof[:4])
    proof = [t for t in cited if p.kind_of.get(t) not in (None, chained)]
    if proof:
        return [], f"{edge}: " + ", ".join(f"{t} {p.kind_of[t]}" for t in proof[:4])
    if pending := [t for t in cited if p.kind_of.get(t) is None]:
        return [Finding(g.where, f"{edge}, resting on {lo} rule(s) the lineage has not classified "
                                 f"({', '.join(pending[:6])}): classify them (record_verdicts.py --edition {lo})")], None
    return [Finding(g.where, f"{edge}, yet every {lo} rule it cites is {chained} into {hi} "
                             f"({', '.join(cited[:6])}): either the gate or the change record is wrong")], None


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
    """The repository's gates (constructs.json and the grammar) and the lineage between each adjacent pair of its held
    editions, read through the one view."""
    year_of = {e.spelling: e.year for e in schema.all_editions}
    grammar = {p.relative_to(root).as_posix(): p.read_text(encoding="utf-8")
               for p in sorted((root / GRAMMAR_REL).rglob("*.g4"))}
    gates = construct_gates(json.loads((root / CONSTRUCTS_REL).read_text(encoding="utf-8"))) + grammar_gates(grammar, year_of)
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
    windows = sum(1 for g in gates if g.where.startswith("constructs.json"))
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
           {"rule-id": "1989-AR-A.2.7.3-1", "section": "A.2.7.3", "kind": "AR", "ordinal": 1,
            "next": {"kind": "KEPT", "to": ["AR-15.10.3-1"]}}]
    new = [{"rule-id": "SR-14.9.1.3-1", "section": "14.9.1.3", "kind": "SR", "ordinal": 1},
           {"rule-id": "FMT-14.9.2.2", "section": "14.9.2.2", "kind": "FMT", "ordinal": 0},
           {"rule-id": "GR-14.9.3.4-1", "section": "14.9.3.4", "kind": "GR", "ordinal": 1},
           {"rule-id": "AR-15.10.3-1", "section": "15.10.3", "kind": "AR", "ordinal": 1}]
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
    ok(finds(gate(2002, None, "§2023:14.9.3.4 GR1")) == [] and finds(gate(2014, None, "§2023:14.9.2.2")) == []
       and edge_findings(gate(2002, None, "§2023:14.9.3.4 GR1"), p, schema)[1] == "start 2002: GR-14.9.3.4-1 NEW",
       "a start rests on a NEW rule, or on one a CHANGED record names")
    ok(any("KEPT from 1985" in f for f in finds(gate(2002, None, "§2023:14.9.1.3 SR1"))),
       "a start whose every cited rule is KEPT contradicts its chain")
    ok(any("cites nothing of 2023" in f for f in finds(gate(2002, None, "ISO 14.9.3"))),
       "a start citing nothing of the newer edition is refused (a bare clause number is no citation)")
    ok(edge_findings(gate(2002, None, "§2023:6.2.3.1"), p, schema)
       == ([], "start 2002: prose the 2023 catalog holds no rule of (§2023:6.2.3.1), outside the lineage"),
       "a start citing only prose the catalog holds no rule of rests on its citation (the CITATIONS audit checks it)")
    ok(finds(gate(85, 2002, "§1985:VI.6.24.4 GR4")) == [] and finds(gate(85, 2023, "§1985:VI.6.15.2")) == [],
       "an end rests on an older rule REMOVED, or CHANGED")
    ok(any("KEPT into 2023" in f for f in finds(gate(85, 2014, "§1985:VI.6.19.3 SR1"))),
       "an end whose every cited rule is KEPT contradicts its chain")
    ok(any("has not classified" in f for f in finds(gate(85, 2002, "§1985:VI.6.9.4 GR1"))),
       "an end resting on an unclassified rule waits for its classification, refused")
    ok(any("cites nothing of 1985" in f for f in finds(gate(85, 2002, "§2023:14.9.3.4 GR1"))),
       "an end citing nothing of the older edition is refused")
    ok(edge_findings(gate(2002, 2014, "nothing"), p, schema) == ([], None)
       and edge_findings(gate(85, None, "nothing"), p, schema) == ([], None)
       and edge_findings(gate(2023, None, "§2023:14.9.3.4 GR1"), p, schema)[1] is not None,
       "a window wholly between held editions, or open at both ends, has no edge the pair spans")
    ok(edge_findings(gate(85, 2002, "§2023:8.9 interval"), p, schema) == ([], "a word's reservation (the word lists hold it)")
       and not word_gate(gate(85, 2002, "§2023:8.9; §2023:14.9.3")),
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
    construct = construct_gates({"constructs": [{"id": "c", "introducedIn": 2002, "removedIn": None,
                                                 "citation": "§2023:14.9.3.4 GR1; §5000:1 malformed"}]})  # citation-audit: example
    ok(construct == [Gate("constructs.json c", 2002, None, construct[0].cites)] and len(construct[0].cites) == 1,
       "a construct's window is a gate citing what its citation names, a malformed citation dropped (CITATIONS refuses it)")
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
