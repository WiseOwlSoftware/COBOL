#!/usr/bin/env python3
"""THE TRACEABILITY INVENTORY, AS A VIEW: an edition's catalog joined with its store, each rule's state computed
(DESIGN-spec-conformance-review.md section 4, kb/Work PB4992).

    python scripts/spec/inventory_view.py --check              # the gate audit: every held edition's view, refused or not
    python scripts/spec/inventory_view.py --summary            # each edition's rules, GAP and verdicts (the session probe)
    python scripts/spec/inventory_view.py --states [--edition Y]   # `JSON {rule-id: state}`, the C# view's parity witness
    python scripts/spec/inventory_view.py --self-test

EACH FACT HAS ONE HOME (owner 2026-11-08: "We must eliminate redundant code and data. The new implementation should be as
if we originally architected it"). A rule's id, locator, kind, ordinal and subject are its edition's catalog's; its
adjudication is its edition's store's (`inventory_store.py`, one file per adjudicated rule); its state (OK or GAP) is
computed here and stored nowhere. `inventory(edition)` is the ONE view per edition, and every reader reads it: none opens a
store file or rebuilds a row from a catalog. Its twin in C# is `tests/_shared/TraceabilityInventory.cs`.

A ROW of the view is the catalog rule (its fields less `id` and `text`; `section` is the rule's locator, `Edition.locator`),
its `rule-id`, the `rule-digest` of its text, each adjudicated field of its edition (`Edition.adjudicated`, empty when the
store holds none) and its `state`: the newest edition's by `Schema.state_for`, an older one's through its change record
(`lineage.state_of`, against the next held edition's view). Rows come in the spec's order (section or locator, numeric
parts numerically and Roman ones by value, then ordinal, then id).

THE VIEW REFUSES (`View.problems`; `inventory()` raises them, naming each): a store file of a rule its catalog no longer
has, a store file holding a field no store file holds (a catalog field or a state copied in), and a store file whose
`rule-digest` is not its rule's text (an adjudication decided against other text: a positional sub-list id that shifted
under it, kb/Work PB698). The store refuses a stray, misnamed or case-colliding file itself (`read_store`).

A VIEW READS ONE TREE (`inventory_schema.Tree`): the working tree at a root (the repository's by default, another
worktree's for `doc_audits.py --root`), or a commit. Every file the state rule reads (the schema and its editions, the
catalog, the store, docs/CONFORMANCE.md's registers, the Annex A.2 list) is read from that tree (branch review R1-3, R1-4).

HISTORY. `inventory_at(rev)` is the view at a commit: that commit's store joined with that commit's catalog under that
commit's schema and registers. A commit before PB4992 stored whole rows (the catalog's fields and the state beside the
adjudication), and those are its rows as committed.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import sys
from collections import Counter
from collections.abc import Iterable
from typing import Any, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import lineage  # noqa: E402  (an older edition's change record and its state)
from inventory_schema import (HELD, NEWEST, REPO, WORKING_TREE, Edition, Schema, Tree, edition,  # noqa: E402
                              load_schema)
from inventory_store import STORE_KEYS, inventory_dir, read_store, store_at  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

Row = dict[str, Any]
#: Whitespace and the transcription's markdown escapes are not content: two texts that differ only there are the same
#: rule, and a digest that said otherwise would cry wolf on every re-transcription.
_SQUASH = re.compile(r"\s+")
#: A Roman section of the 1985 document (`VI` in `VI.5.7.3`), ordered by its value.
_ROMAN = re.compile(r"[IVX]+")
_ROMAN_VALUE = {"I": 1, "V": 5, "X": 10}
#: The catalog keys a row does not carry: the id is its `rule-id`, and the text is the catalog's alone (the digest
#: stands for it).
_NOT_ROW = frozenset({"id", "text"})


def rule_digest(text: str) -> str:
    """A short content fingerprint of a rule's catalog text: the text an adjudication is decided against.

    ⛔ WHY A STORE FILE CARRIES ONE (kb/Work PB698). An adjudication is keyed by rule id alone, which is only safe while an
    id names the same RULE it named when the adjudication was recorded. It does not always: a sub-list id is POSITIONAL
    (`…-L<sublist>.<n>`), so when a sub-list above it stops existing, as it does whenever a nested list's transcription is
    re-levelled, every id below SHIFTS DOWN and would silently inherit a verdict adjudicated against different text.
    PB698's repair moved six such rows at once. The digest is the observation that sees it, and the view refuses it."""
    return hashlib.sha1(_SQUASH.sub(" ", text.replace("\\", "")).strip().encode("utf-8")).hexdigest()[:12]


def _roman(part: str) -> int:
    total = 0
    for a, b in zip(part, part[1:] + " "):
        v = _ROMAN_VALUE[a]
        total += -v if b != " " and _ROMAN_VALUE[b] > v else v
    return total


def _section_key(section: str) -> list[tuple[int, int, str]]:
    return [(0, int(p), "") if p.isdigit() else (0, _roman(p), "") if _ROMAN.fullmatch(p) else (1, 0, p)
            for p in section.split(".")]


def spec_order(rows: Iterable[Row]) -> list[Row]:
    """`rows` in the spec's order: section (or locator), catalog ordinal, rule id. A Roman part orders by its value, and a
    lettered part (`A.1`, Amendment 1's `A.2`) after the numbered ones."""
    return sorted(rows, key=lambda r: (_section_key(r["section"]), r["ordinal"], r["rule-id"]))


class Problem(NamedTuple):
    """A store record the view refuses: its rule, why, and whether only its text moved under it (`drifted`), which a
    re-adjudication of that rule cures (`record_verdicts.py`)."""
    rule_id: str
    text: str
    drifted: bool = False

    def __str__(self) -> str:
        return f"{self.rule_id}: {self.text}"


class View(NamedTuple):
    """An edition's view: its rows, and the store records it refuses."""
    rows: list[Row]
    problems: list[Problem]


def row_of(ed: Edition, rule: dict[str, Any], record: dict[str, Any]) -> Row:
    """The row of catalog `rule` with its store `record` (empty when unadjudicated), without its state."""
    return {"rule-id": rule["id"], **{k: v for k, v in rule.items() if k not in _NOT_ROW},
            "section": rule[ed.locator], "rule-digest": rule_digest(rule["text"]),
            **ed.blank(), **{f: record[f] for f in ed.adjudicated if f in record}}


def state_of(ed: Edition, row: Row, schema: Schema, targets: dict[str, Row]) -> str:
    """`row`'s state at its edition: the newest edition's by the one state rule, an older one's through its change record."""
    return schema.state_for(row) if ed.newest else lineage.state_of(row, ed, schema, targets)


def record_problems(ed: Edition, by_id: dict[str, dict[str, Any]],
                    records: dict[str, dict[str, Any]]) -> list[Problem]:
    """What the view refuses of `records` against the catalog rules `by_id` (see the module docstring)."""
    allowed = set(STORE_KEYS) | set(ed.adjudicated)
    problems: list[Problem] = []
    for rid, record in sorted(records.items()):
        rule = by_id.get(rid)
        if rule is None:
            problems.append(Problem(rid, f"adjudicates no rule of the {ed.year} catalog (the rule was dropped or renamed: "
                                         f"move or withdraw the adjudication through record_verdicts.py)"))
            continue
        if extra := sorted(set(record) - allowed):
            problems.append(Problem(rid, f"holds {extra}, which a store file never holds (the catalog's, or computed: "
                                         f"DESIGN-spec-conformance-review.md section 4)"))
        if record.get("rule-digest") != (want := rule_digest(rule["text"])):
            problems.append(Problem(rid, f"was adjudicated against other text (rule-digest {record.get('rule-digest')!r}, "
                                         f"the catalog's text is {want!r}): re-adjudicate it through record_verdicts.py "
                                         f"(kb/Work PB698)", drifted=True))
    return problems


def join(ed: Edition, rules: list[dict[str, Any]], records: dict[str, dict[str, Any]], schema: Schema,
         targets: dict[str, Row]) -> View:
    """The view of catalog `rules` with store `records`: one row per rule, in spec order, and the records it refuses.
    `targets` are the next held edition's rows by id (empty for the newest edition)."""
    rows = []
    for rule in rules:
        row = row_of(ed, rule, records.get(rule["id"], {}))
        row["state"] = state_of(ed, row, schema, targets)
        rows.append(row)
    return View(spec_order(rows), record_problems(ed, {r["id"]: r for r in rules}, records))


def _edition(schema: Schema, year: int | None) -> Edition:
    return schema.newest if year is None else schema.edition(year)


def _join_tree(ed: Edition, schema: Schema, tree: Tree, records: dict[str, dict[str, Any]]) -> View:
    """`ed`'s view in `tree` of `records`: its catalog and, for an older edition, the next held edition's rows as `tree`
    has them (a commit that holds an older store holds the store its change records name, or the commit is refused)."""
    if ed.newest:
        targets: list[Row] = []
    elif tree.rev is None:
        targets = inventory(ed.next_held, schema, tree)
    elif (at := inventory_at(tree.rev, ed.next_held, tree.root)) is None:
        raise SystemExit(f"inventory_at {tree.rev}: the commit holds a {ed.year} store but no {ed.next_held} store, "
                         f"which its change records name")
    else:
        targets = at
    return join(ed, ed.rules(tree), records, schema, {r["rule-id"]: r for r in targets})


def view(year: int | None = None, schema: Schema | None = None, tree: Tree = WORKING_TREE) -> View:
    """The view of the held edition `year` (the newest by default) in the working tree `tree`, refused records and all."""
    if tree.rev is not None:
        raise ValueError(f"view() and inventory() read a working tree; the view at {tree.rev} is inventory_at")
    schema = schema or load_schema(tree)
    ed = _edition(schema, year)
    return _join_tree(ed, schema, tree, read_store(inventory_dir(ed, tree.root)))


def refuse(year: int, problems: list[Problem]) -> None:
    """A SystemExit naming every problem of `year`'s view; nothing when there is none."""
    if problems:
        raise SystemExit(f"inventory {year}: the view refuses {len(problems)} store record(s):\n  "
                         + "\n  ".join(map(str, problems)))


def inventory(year: int | None = None, schema: Schema | None = None, tree: Tree = WORKING_TREE) -> list[Row]:
    """THE VIEW: every rule of the held edition `year` (the newest by default) in `tree`, in spec order, its adjudication
    joined and its state computed. A refused store record is a SystemExit naming each one: a row silently read past would
    be a row silently wrong. `tree` is a working tree (the repository's by default); a commit's view is `inventory_at`."""
    schema = schema or load_schema(tree)
    v = view(year, schema, tree)
    refuse(_edition(schema, year).year, v.problems)
    return v.rows


def inventory_at(rev: str, year: int | None = None, repo: pathlib.Path = REPO) -> list[Row] | None:
    """The held edition `year`'s view (the newest by default) as committed at `rev` of `repo` (spec order), or None when
    `rev` has no store of it: that commit's store joined with that commit's catalog under that commit's schema and
    registers, so a change to any of them is seen as the change it is. A commit before PB4992 stored whole rows, which are
    its rows as committed. A record the view would refuse is history, not this reader's to refuse.

    The store is located by the working tree's editions (a commit before the editions were the schema's has none to
    name it), and joined under the commit's own."""
    tree = Tree(repo, rev)
    records = store_at(tree, NEWEST if year is None else edition(year))
    if records is None:
        return None
    if all("state" in r for r in records):      # a commit before PB4992: no record of the view's holds a state
        return spec_order(records)
    schema = load_schema(tree)
    return _join_tree(_edition(schema, year), schema, tree, {r["rule-id"]: r for r in records}).rows


# -- the audit, the summary, the parity witness ---------------------------------------------------------------------------

def check() -> int:
    """THE GATE AUDIT: every held edition's view, and each older edition's change records (`lineage.next_errors`).
    Prints one line per edition; returns 1 on a finding."""
    schema = load_schema()
    findings: list[str] = []
    for year in sorted(HELD, reverse=True):
        ed = schema.edition(year)
        v = view(year, schema)
        findings += [f"{year}: {p}" for p in v.problems]
        line = f"inventory {year}: {len(v.rows)} rules, {_gaps(v.rows)} GAP"
        if not ed.newest:
            targets = {r["rule-id"]: r for r in inventory(ed.next_held, schema)}
            findings += [f"{year}: {r['rule-id']}: {e}" for r in v.rows for e in lineage.next_errors(r, ed, targets)]
            kinds = Counter((r["next"] or {}).get("kind") or "unclassified" for r in v.rows)
            line += (f"; change records {dict(sorted(kinds.items()))}; "
                     f"{len(lineage.new_rules(ed, v.rows, targets))} rule(s) of {ed.next_held} NEW (derived)")
        print(line)
    for f in findings[:40]:
        print(f"  ✗ {f}")
    print(f"=== INVENTORY VIEW: {'GREEN' if not findings else f'RED — {len(findings)} finding(s)'} ===")
    return 1 if findings else 0


def _gaps(rows: list[Row]) -> int:
    return sum(1 for r in rows if r["state"] == "GAP")


def summary() -> list[str]:
    """The session probe's lines: each held edition's rules and GAP count side by side (the burn-down metric), then each
    edition's adjudicated verdicts, and the CONFORMS rows a spec-derived test would close."""
    schema = load_schema()
    views = {year: inventory(year, schema) for year in sorted(HELD, reverse=True)}
    lines = ["invent : " + " | ".join(f"{year} {len(rows)} rules · {_gaps(rows)} GAP" for year, rows in views.items())
             + " (v1.0 = zero GAP)"]
    for year, rows in views.items():
        adjudicated = [r for r in rows if r["verdict"]]
        if adjudicated:
            by_verdict = Counter(r["verdict"] for r in adjudicated).most_common()
            lines.append(f"       {year}: {len(adjudicated)} adjudicated · " + " · ".join(f"{v} {n}" for v, n in by_verdict))
        # "test-needed" is a row that DID NOT CLOSE, not a row with an empty test-ref: a row can cite a NIST golden or a
        # *_MatchesLegacy differential, which resolves on disk, and still be open (only a SPEC-DERIVED test closes one).
        if need := sum(1 for r in adjudicated if r["verdict"] == "CONFORMS" and r["state"] == "GAP"):
            lines.append(f"       {year}: {need} CONFORMS still test-needed (each is one spec-derived golden from OK)")
    return lines


def states(year: int | None = None) -> dict[str, str]:
    """Each rule of the held edition `year`'s view (the newest by default) and its computed state: what the C# view's state
    rule is held to."""
    return {r["rule-id"]: r["state"] for r in inventory(year)}


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    from inventory_schema import SCHEMA_REL  # noqa: PLC0415
    from inventory_store import write_store  # noqa: PLC0415
    schema = load_schema()
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    ed = edition(2023)
    rules = [{"id": "GR-4.10-1", "section": "4.10", "kind": "GR", "ordinal": 1, "sublist": None, "subject": "s",
              "text": "Ten."},
             {"id": "GR-4.2-1", "section": "4.2", "kind": "GR", "ordinal": 1, "sublist": None, "subject": "s",
              "text": "Two  \\*x*."},
             {"id": "DOC-A.1-1", "section": "A.1", "kind": "DOC", "ordinal": 1, "sublist": None, "subject": "d",
              "text": "Doc."}]
    adjudicated = {"rule-id": "GR-4.2-1", "rule-digest": rule_digest("Two *x*."), "verdict": "PARTIAL",
                   "code-location": "src/WiseOwl.Cobol.Compiler/X.cs", "notes": "n"}
    v = join(ed, rules, {"GR-4.2-1": adjudicated}, schema, {})
    ok([r["rule-id"] for r in v.rows] == ["GR-4.2-1", "GR-4.10-1", "DOC-A.1-1"] and not v.problems,
       "one row per catalog rule, in spec order (numeric parts numerically, the Annex after the clauses)")
    two, ten = v.rows[0], v.rows[1]
    ok(two["verdict"] == "PARTIAL" and two["kind"] == "GR" and two["section"] == "4.2" and two["state"] == "GAP"
       and "text" not in two and "id" not in two, "a row joins its catalog fields and its adjudication, its state computed")
    ok(ten["verdict"] == "" and ten["editions"] == "" and ten["state"] == "GAP",
       "a rule with no store file is a row with empty adjudication, and a GAP")
    ok(rule_digest("Two  \\*x*.") == rule_digest("Two *x*.") != rule_digest("Two y."),
       "the digest ignores whitespace and the transcription's escapes, and sees the words")

    def refused(records: dict[str, dict[str, Any]]) -> list[Problem]:
        return join(ed, rules, records, schema, {}).problems

    ok([(p.rule_id, p.drifted) for p in refused({"GR-9.9-1": {"rule-id": "GR-9.9-1"}})] == [("GR-9.9-1", False)]
       and "no rule of the 2023 catalog" in str(refused({"GR-9.9-1": {"rule-id": "GR-9.9-1"}})[0]),
       "a store file of a rule the catalog no longer has is refused")
    ok(any("never holds" in p.text and "'state'" in p.text and "'section'" in p.text and not p.drifted
           for p in refused({"GR-4.2-1": {**adjudicated, "section": "4.2", "state": "GAP"}})),
       "a store file holding a catalog field or a state is refused: each fact has one home")
    ok([(p.rule_id, p.drifted) for p in refused({"GR-4.2-1": {**adjudicated, "rule-digest": "000000000000"}})]
       == [("GR-4.2-1", True)], "a store file decided against other text is refused, as a drift (kb/Work PB698)")
    try:
        refuse(2023, refused({"GR-9.9-1": {"rule-id": "GR-9.9-1"}}))
        named = ""
    except SystemExit as e:
        named = str(e.code)
    ok("GR-9.9-1: adjudicates no rule" in named, "a refusal names each refused rule")
    old = edition(1985)
    roman = [{"id": f"1985-GR-{loc}-1", "locator": loc, "section": loc.split(".")[0], "kind": "GR", "ordinal": 1,
              "subject": "s", "text": loc} for loc in ("IX.2.3", "V.1.2", "XVII.1", "IV.4.2", "A.2.5")]
    rows85 = join(old, roman, {}, schema, {}).rows
    ok([r["section"] for r in rows85] == ["IV.4.2", "V.1.2", "IX.2.3", "XVII.1", "A.2.5"]
       and all(r["next"] == {} and "editions" not in r and r["state"] == "GAP" for r in rows85),
       "an older edition's row is located by its locator, a Roman part by its value, and carries next, not editions")

    # The view at a commit: that commit's store, catalog and schema; and a commit before PB4992's whole rows as committed.
    t = scratch_dir("inventory-view-")
    repo = ScratchRepo.init(t / "repo")
    repo.commit("pre-PB4992", {ed.store + "/GR-4.2-1.json": json.dumps(
        {"rule-id": "GR-4.2-1", "section": "4.2", "kind": "GR", "ordinal": 1, "verdict": "PARTIAL", "state": "GAP"})})
    ok([(r["rule-id"], r["state"]) for r in inventory_at("HEAD", 2023, repo.path) or []] == [("GR-4.2-1", "GAP")],
       "a commit before PB4992 is read as its whole rows, as committed")
    repo.write({ed.store + "/GR-4.2-1.json": None})
    write_store([adjudicated], repo.path / ed.store)
    repo.commit("the view", {ed.catalog: json.dumps({"rules": rules}), SCHEMA_REL: WORKING_TREE.text(SCHEMA_REL)})
    at = inventory_at("HEAD", 2023, repo.path) or []
    ok([(r["rule-id"], r["verdict"], r["state"]) for r in at]
       == [("GR-4.2-1", "PARTIAL", "GAP"), ("GR-4.10-1", "", "GAP"), ("DOC-A.1-1", "", "GAP")],
       "a later commit's view joins its own catalog and store, its states computed under its own schema")
    ok(inventory_at("HEAD", 1985, repo.path) is None and inventory_at("no-such-rev", 2023, repo.path) is None,
       "a commit with no store of an edition has no view of it")
    # Branch review R1-3: the state rule's other inputs (docs/CONFORMANCE.md's registers, the Annex A.2 list, the catalog's
    # rule ids) are read as the commit has them, never from the working tree.
    at_schema = load_schema(Tree(repo.path, "HEAD"))
    undefined_rel = at_schema.derivation.undefined_list
    repo.commit("registers", {
        "docs/CONFORMANCE.md": "## 7. Annex A.1\n\n| A.1 item | Element | Our determination | Pinned by |\n|---|---|---|---|\n"
                               "| DOC-A.1-1 | e | Provided. | t |\n",
        undefined_rel: json.dumps({"items": [{"item": 1, "rule-ids": ["GR-4.2-1"]}]})})
    at_schema = load_schema(Tree(repo.path, "HEAD"))
    ok(at_schema.register_items() == {"DOC-A.1-1"} and at_schema.register_items() != schema.register_items()
       and at_schema.derivation.undefined == {1: {"GR-4.2-1"}} and at_schema.derivation.register == {}
       and at_schema.rule_ids == {"GR-4.10-1", "GR-4.2-1", "DOC-A.1-1"},
       "a commit's schema reads that commit's §7 and §8 registers, Annex A.2 list and catalog, not the working tree's")

    live = view(2023, schema)
    ok(not live.problems and len(live.rows) == len(ed.rules()), "the working tree's 2023 view refuses nothing, one row per rule")
    print(f"=== INVENTORY VIEW SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The traceability inventory, as a view (kb/Work PB4992).")
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="the gate audit of every held edition's view")
    mode.add_argument("--summary", action="store_true", help="each edition's rules, GAP and verdicts")
    mode.add_argument("--states", action="store_true", help="`JSON {rule-id: state}` of one edition's view")
    mode.add_argument("--self-test", action="store_true")
    ap.add_argument("--edition", type=int, help="the held edition --states reads (default: the newest)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.check:
        return check()
    if a.summary:
        print("\n".join(summary()))
        return 0
    print("JSON " + json.dumps(states(a.edition), separators=(",", ":")))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
