#!/usr/bin/env python3
"""THE RULE LINEAGE: one rule list per COBOL edition, linked by one change record per adjacent pair (kb/Work PB4991).

    python scripts/spec/lineage.py --self-test

The design is DESIGN-spec-conformance-review.md §10.2 (owner, 2026-11-08). In short:

- Each edition has its own catalog and its own inventory store (`inventory_store.inventory_dir`). The 2023 store is the
  newest held edition's; an OLDER edition's row carries `next`, its one change record against the next HELD edition:
  `{"kind": "KEPT"|"CHANGED"|"REMOVED", "edition": <int>, "to": [<rule ids of that edition>], "bridge": <bool>,
  "reason": "<why>", "source": "<the audit run or adjudication that decided it>"}`, its `reason` omitted when it states
  none (one stored form: an empty reason is refused, kb/Work PB4995). Its keys and its kinds are DATA, the
  schema's `next` (`inventory_schema.ChangeRecord`, kb/Work PB4993): no code here spells a kind. A rule of the newer
  edition that no older `to` names is NEW: derived, never written. `bridge` is true while an edition between the two is
  not held (1985 straight to 2023 until 2002 and 2014 are transcribed, kb/Work PB4813).
- `status` is the rule's standing in its own edition: `normal`, archaic (§2023:4.2.12: still required, discouraged) or
  obsolete (§1985:I.1.5.2.3, §2023:4.2.13: still required, to be deleted at the next revision), a fact of the rule read
  from its text into its catalog (`element_standing.py`), never stored. Deprecation is KEPT into an obsolete rule, then
  REMOVED.
- A verdict is held once per chain, on the chain's newest row: a row whose record is of the chained kind (KEPT) carries
  none of the schema's `next.verdict-fields` (`next_errors` refuses them), and its state is its chain's at its own edition
  (`state_of`): every `to` row is OK, and every `to` row's `editions` band includes the older edition, which is what
  places the chain's test there. A CHANGED or REMOVED row, and a row not yet classified, is judged on its own fields by
  `Schema.state_for`.

Each edition (its catalog, the catalog key that locates a rule, its store and the adjudicated fields the store holds)
is the schema's `editions.of`, read as `inventory_schema.Edition`; this module is the one reader of the change record's
shape (`next_errors`) and the one definition of an older row's state (`state_of`). The view (`inventory_view.py`) joins
each edition's catalog with its store through it and runs the audit, and `record_verdicts.py --edition` writes through
it; nothing else spells the rules.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from typing import Any, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from inventory_schema import Edition, Schema, edition, load_schema  # noqa: E402  (the schema's `editions.of`, `next`)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402


def chained(row: dict[str, Any], schema: Schema) -> bool:
    """Is `row`'s change record of the chained kind (KEPT), whose verdict is its chain's newest row's?"""
    return (row.get("next") or {}).get("kind") == schema.next.chained.name


def next_errors(row: dict[str, Any], ed: Edition, schema: Schema, targets: dict[str, dict[str, Any]]) -> list[str]:
    """Why `row`'s `next` is not a legal change record of edition `ed` (the schema's `next`) — empty when it is, or when
    the row has none yet. `targets` are the next held edition's rows by id."""
    errs: list[str] = []
    nxt = row.get("next")
    if nxt in (None, {}, ""):
        return errs
    if not isinstance(nxt, dict):
        return [f"next is {type(nxt).__name__}, not an object"]
    shape = schema.next
    if unknown := set(nxt) - shape.keys:
        errs.append(f"next carries unknown key(s) {sorted(unknown)}; it may carry {sorted(shape.keys)}")
    kind, to = shape.kinds.get(nxt.get("kind")), nxt.get("to")
    if kind is None:
        errs.append(f"next.kind {nxt.get('kind')!r} is not one of {list(shape.kinds)}")
    want = ed.next_held
    if nxt.get("edition") != want:
        errs.append(f"next.edition must be {want}, the next held edition after {ed.year}")
    bridged = ed.bridged
    if nxt.get("bridge") is not bridged:
        errs.append(f"next.bridge must be {bridged}: an edition between {ed.year} and {want} is "
                    f"{'not ' if bridged else ''}missing")
    if not isinstance(to, list) or not all(isinstance(t, str) for t in to):
        errs.append("next.to must be a list of rule ids")
        to = []
    for t in to:
        if t not in targets:
            errs.append(f"next.to names {t!r}, which is no rule of edition {want}")
    if not (nxt.get("source") or "").strip():
        errs.append("next.source names the audit run or the adjudication that decided it")
    if kind is None:
        return errs
    if kind.to and not to:
        errs.append(f"a {kind.name} record names the rule(s) of the next edition it carries into")
    if not kind.to and to:
        errs.append(f"a {kind.name} record names no rule of the next edition")
    if kind.reason and not (nxt.get("reason") or "").strip():
        errs.append(f"a {kind.name} record states its reason (the words that differ, or what deleted the feature)")
    elif "reason" in nxt and not (nxt["reason"] or "").strip():
        errs.append("next.reason is empty: a record that states no reason omits the key (one stored form)")
    if kind.chained and (own := [f for f in shape.verdict_fields if row.get(f)]):
        errs.append(f"a {kind.name} rule holds no verdict of its own (its chain's newest rule does), but it carries {own}")
    return errs


def state_of(row: dict[str, Any], ed: Edition, schema: Schema, targets: dict[str, dict[str, Any]]) -> str:
    """`row`'s state at its own edition: a chained (KEPT) row's is its chain's, every other row's is its own
    (`Schema.state_for`). `targets` are the next held edition's rows by id, each with its own state."""
    if not chained(row, schema):
        return schema.state_for(row)
    band = ed.spelling
    to = row["next"].get("to") or []
    for t in to:
        target = targets.get(t)
        if target is None or target.get("state") != "OK":
            return "GAP"
        if "editions" in target and band not in schema.split(target.get("editions", "") or "", schema.editions_sep):
            return "GAP"
    return "OK" if to else "GAP"


def new_rules(ed: Edition, rows: Iterable[dict[str, Any]], targets: dict[str, dict[str, Any]]) -> list[str]:
    """The next edition's rules no row of `ed` carries into: NEW at that edition (derived, never written)."""
    reached = {t for r in rows for t in ((r.get("next") or {}).get("to") or [])}
    return [t for t in targets if t not in reached]


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    import copy  # noqa: PLC0415
    from inventory_schema import SCHEMA_PATH, editions_of, held_edition  # noqa: PLC0415
    from inventory_store import inventory_dir  # noqa: PLC0415
    schema = load_schema()
    raw = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    ed = edition(1985)
    targets = {"SR-13.18.38.3-1": {"rule-id": "SR-13.18.38.3-1", "state": "OK", "editions": "85,2002,2014,2023"},
               "SR-13.18.38.3-2": {"rule-id": "SR-13.18.38.3-2", "state": "OK", "editions": "2002,2014,2023"},
               "GR-14.9.21.4-2": {"rule-id": "GR-14.9.21.4-2", "state": "GAP", "editions": "85,2002,2014,2023"}}

    def row(**nxt: Any) -> dict[str, Any]:
        base = {"kind": "KEPT", "edition": 2023, "to": ["SR-13.18.38.3-1"], "bridge": True, "source": "planted"}
        return {"rule-id": "1985-SR-VI.5.8.3-1", "verdict": "", "next": {**base, **nxt}}

    def errs(r: dict[str, Any], s: Schema = schema) -> list[str]:
        return next_errors(r, ed, s, targets)

    ok(ed.adjudicated[-1] == "next" and "editions" not in ed.adjudicated and "status" not in ed.adjudicated
       and edition(2023).newest and not ed.newest,
       "an older edition's store holds next, not editions; status is the catalog's; 2023 is the newest held edition")
    ok(ed.next_held == 2023 and ed.bridged and not edition(2023).bridged,
       "1985's next held edition is 2023, a bridge while 2002 and 2014 are not held")
    ok(ed.blank()["next"] == {} and edition(2023).blank()["editions"] == "" and ed.locator == "locator",
       "an unadjudicated rule's fields are empty, and each edition names the catalog key that locates a rule")
    ok(schema.next.chained.name == "KEPT" and set(schema.next.kinds) == {"KEPT", "CHANGED", "REMOVED"},
       "the schema's next names the three kinds of section 10.2, and KEPT is the chained one")
    ok(errs(row()) == [], "a KEPT record into a rule of the next edition, bridged, is legal")
    ok(any("bridge must be True" in e for e in errs(row(bridge=False))),
       "a 1985 record that does not say it bridges 2002 and 2014 is refused")
    ok(any("no rule of edition 2023" in e for e in errs(row(to=["SR-99.9.9-1"]))),
       "a KEPT record into a rule the next edition lacks is refused")
    ok(any("names no rule" in e for e in errs(row(kind="REMOVED", reason="deleted")))
       and errs(row(kind="REMOVED", to=[], reason="the communication module is deleted")) == [],
       "a REMOVED record names no rule, and states its reason")
    ok(any("states its reason" in e for e in errs(row(kind="CHANGED"))), "a CHANGED record states its reason")
    ok(any("omits the key" in e for e in errs(row(reason=""))) and errs(row(reason="the same in other words")) == [],
       "a KEPT record states a reason or omits the key: an empty reason is not stored (one stored form)")
    ok(any("carries into" in e for e in errs(row(kind="CHANGED", to=[], reason="x"))),
       "a CHANGED record names the rule(s) it carries into")
    ok(any("is not one of" in e for e in errs(row(kind="DEPRECATED"))), "a kind the schema does not name is refused")
    ok(any("unknown key" in e for e in errs(row(evidence="§2023:13.18.38.3 SR1"))),
       "a record carrying a key the schema does not name (its evidence is read from the catalogs) is refused")
    ok(any("holds no verdict" in e for e in errs(dict(row(), verdict="CONFORMS"))),
       "a KEPT rule holding a verdict of its own is refused: the chain's newest rule holds it")
    # The kinds are DATA (kb/Work PB4993): a kind the schema alone adds is validated by its flags, with no code change.
    split = copy.deepcopy(raw)
    split["next"]["kinds"]["SPLIT"] = {"meaning": "planted", "to": True, "reason": True, "chained": False}
    s_split = Schema(split)
    ok(errs(row(kind="SPLIT", reason="two rules"), s_split) == []
       and any("states its reason" in e for e in errs(row(kind="SPLIT"), s_split)),
       "a kind the schema alone adds is read from its flags")
    two = copy.deepcopy(raw)
    two["next"]["kinds"]["CHANGED"]["chained"] = True
    try:
        Schema(two).next
        ok(False, "a schema with two chained kinds is refused")
    except SystemExit:
        ok(True, "a schema with two chained kinds is refused")
    # A history reader joins the newest edition at a commit whose schema predates `next` (the gate's WITNESS LOSS audit
    # reads main's): that schema loads, and only a reader that asks for the change record is refused by name.
    before = copy.deepcopy(raw)
    del before["next"]
    old = Schema(before)
    try:
        old.next
        ok(False, "a schema before the change record loads, and asking it for `next` is refused")
    except SystemExit:
        ok(old.state_for({"rule-id": "x", "verdict": ""}) == "GAP",
           "a schema before the change record loads, and asking it for `next` is refused")
    ok(state_of(row(), ed, schema, targets) == "OK", "a KEPT row is OK when its chain's row is OK and its band includes 85")
    ok(state_of(row(to=["SR-13.18.38.3-2"]), ed, schema, targets) == "GAP",
       "a KEPT row whose chain's test is not placed at 85 (the band starts at 2002) is a GAP")
    ok(state_of(row(to=["SR-13.18.38.3-1", "GR-14.9.21.4-2"]), ed, schema, targets) == "GAP",
       "a KEPT row into two rules is a GAP while either is")
    ok(state_of({"rule-id": "x", "verdict": ""}, ed, schema, targets) == "GAP", "an unclassified row with no verdict is a GAP")
    ok(new_rules(ed, [row()], targets) == ["SR-13.18.38.3-2", "GR-14.9.21.4-2"],
       "a rule of the next edition no record reaches is NEW")

    # Branch review R1-1: an edition is the schema's `editions.of` and nothing else, so adding one there is the whole
    # change: the order, the bridges, the newest edition, the band spelling and the store all follow, with no KeyError.
    unheld, held = copy.deepcopy(raw), copy.deepcopy(raw)
    unheld["editions"]["of"].append({"year": 2027, "spelling": "2027"})
    held["editions"]["of"].append({"year": 2027, "spelling": "2027", "catalog": "docs/rearchitecture/spec-rule-catalog-2027.json",
                                   "locator": "section", "store": "tests/version-matrix/inventory-2027",
                                   "adjudicated": list(edition(2023).adjudicated)})
    s_unheld, s_held = Schema(unheld), Schema(held)
    ok(s_unheld.newest.year == 2023 and s_unheld.edition(2023).newest and s_unheld.editions[-1] == "2027"
       and s_unheld.spell_editions(["2027", "2023"]) == "2023,2027",
       "an edition named in the schema alone is in the order and the bands, and is not held")
    e85, e23, e27 = (held_edition(editions_of(held), y) for y in (1985, 2023, 2027))
    ok(e27.newest and s_held.newest == e27 and not e23.newest and e23.next_held == 2027 and not e23.bridged
       and e85.next_held == 2023 and e85.bridged and inventory_dir(e27).name == "inventory-2027",
       "a held edition named in the schema alone becomes the newest; 2023 then carries a change record into it, unbridged")
    kept23 = {"rule-id": "SR-13.18.38.3-1", "verdict": "",
              "next": {"kind": "KEPT", "edition": 2027, "to": ["SR-1"], "bridge": False, "source": "planted"}}
    t27 = {"SR-1": {"rule-id": "SR-1", "state": "OK", "editions": "85,2002,2014,2023,2027"}}
    ok(next_errors(kept23, e23, s_held, t27) == [] and state_of(kept23, e23, s_held, t27) == "OK"
       and state_of(kept23, e23, s_held, {"SR-1": dict(t27["SR-1"], editions="2027")}) == "GAP",
       "a 2023 row KEPT into 2027 is judged by its chain at its own spelling, 2023")
    print(f"=== LINEAGE SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The rule lineage across editions (kb/Work PB4991). The audit's kept change "
                                             "records are written by audit_batch.py change-batch.")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
