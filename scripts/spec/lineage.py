#!/usr/bin/env python3
"""THE RULE LINEAGE: one rule list per COBOL edition, linked by one change record per adjacent pair (kb/Work PB4991).

    python scripts/spec/lineage.py change-batch --state DIR --out FILE # every change record the audit decided (PB4995)
    python scripts/spec/lineage.py --self-test

The design is DESIGN-spec-conformance-review.md §10.2 (owner, 2026-11-08). In short:

- Each edition has its own catalog and its own inventory store (`inventory_store.inventory_dir`). The 2023 store is the
  newest held edition's; an OLDER edition's row carries `next`, its one change record against the next HELD edition:
  `{"kind": "KEPT"|"CHANGED"|"REMOVED", "edition": <int>, "to": [<rule ids of that edition>], "bridge": <bool>,
  "reason": "<why>", "source": "<the audit run or adjudication that decided it>"}`. A rule of the newer edition that no
  older `to` names is NEW: derived, never written. `bridge` is true while an edition between the two is not held (1985
  straight to 2023 until 2002 and 2014 are transcribed, kb/Work PB4813).
- `status` is the rule's standing in its own edition: `normal`, or `obsolete` (§1985:I.1.5.2.3: an obsolete element is
  still required, and is to be deleted at the next revision), a fact of the rule read from its text into its catalog,
  never stored. Deprecation is KEPT into an obsolete rule, then REMOVED.
- A verdict is held once per KEPT chain, on the chain's newest row: a KEPT row carries no verdict, witness or derivation
  of its own (`next_errors` refuses them), and its state is its chain's at its own edition (`state_of`): every `to` row
  is OK, and every `to` row's `editions` band includes the older edition, which is what places the chain's test there.
  A CHANGED or REMOVED row, and a row not yet classified, is judged on its own fields by `Schema.state_for`.

Each edition (its catalog, the catalog key that locates a rule, its store and the adjudicated fields the store holds)
is the schema's `editions.of`, read as `inventory_schema.Edition`; this module is the one definition of the change
record's shape (`next_errors`) and of an older row's state (`state_of`). The view (`inventory_view.py`) joins each edition's catalog with its store through it and runs the audit,
and `record_verdicts.py --edition` writes through it; nothing else spells the rules.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from typing import Any, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from inventory_schema import Edition, edition  # noqa: E402  (each edition: the schema's `editions.of`)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

KEPT, CHANGED, REMOVED = "KEPT", "CHANGED", "REMOVED"
NEXT_KINDS = (KEPT, CHANGED, REMOVED)
NEXT_KEYS = frozenset({"kind", "edition", "to", "bridge", "reason", "source"})


def next_errors(row: dict[str, Any], ed: Edition, targets: dict[str, dict[str, Any]]) -> list[str]:
    """Why `row`'s `next` is not a legal change record of edition `ed` — empty when it is, or when the row has none yet.
    `targets` are the next held edition's rows by id."""
    errs: list[str] = []
    nxt = row.get("next")
    if nxt in (None, {}, ""):
        return errs
    if not isinstance(nxt, dict):
        return [f"next is {type(nxt).__name__}, not an object"]
    if unknown := set(nxt) - NEXT_KEYS:
        errs.append(f"next carries unknown key(s) {sorted(unknown)}; it may carry {sorted(NEXT_KEYS)}")
    kind, to = nxt.get("kind"), nxt.get("to")
    if kind not in NEXT_KINDS:
        errs.append(f"next.kind {kind!r} is not one of {list(NEXT_KINDS)}")
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
    if kind == REMOVED and to:
        errs.append("a REMOVED rule names no rule of the next edition")
    if kind in (KEPT, CHANGED) and not to:
        errs.append(f"a {kind} rule names the rule(s) of the next edition it carries into")
    for t in to:
        if t not in targets:
            errs.append(f"next.to names {t!r}, which is no rule of edition {want}")
    if kind in (CHANGED, REMOVED) and not (nxt.get("reason") or "").strip():
        errs.append(f"a {kind} record states its reason (the words that differ, or what deleted the feature)")
    if not (nxt.get("source") or "").strip():
        errs.append("next.source names the audit run or the adjudication that decided it")
    if kind == KEPT and (own := [f for f in ("verdict", "code-location", "test-ref", "derivation") if row.get(f)]):
        errs.append(f"a KEPT rule holds no verdict of its own (its chain's newest rule does), but it carries {own}")
    return errs


def state_of(row: dict[str, Any], ed: Edition, schema, targets: dict[str, dict[str, Any]]) -> str:
    """`row`'s state at its own edition: a KEPT row's is its chain's, every other row's is its own (`Schema.state_for`).
    `targets` are the next held edition's rows by id, each with its own state."""
    nxt = row.get("next") or {}
    if nxt.get("kind") != KEPT:
        return schema.state_for(row)
    band = ed.spelling
    for t in nxt.get("to") or []:
        target = targets.get(t)
        if target is None or target.get("state") != "OK":
            return "GAP"
        if "editions" in target and band not in schema.split(target.get("editions", "") or "", schema.editions_sep):
            return "GAP"
    return "OK" if nxt.get("to") else "GAP"


def new_rules(ed: Edition, rows: Iterable[dict[str, Any]], targets: dict[str, dict[str, Any]]) -> list[str]:
    """The next edition's rules no row of `ed` carries into: NEW at that edition (derived, never written)."""
    reached = {t for r in rows for t in ((r.get("next") or {}).get("to") or [])}
    return [t for t in targets if t not in reached]


# -- the audit's KEPT records ---------------------------------------------------------------------------------------------

def change_batch(state: pathlib.Path, out: pathlib.Path, source: str) -> int:
    """A record_verdicts batch of every change record the edition audit DECIDED (`results.json`'s `change`, which
    audit_batch.dispositions derives: KEPT for a SAME the verify pass upheld, and the adjudicate pass's KEPT, CHANGED or
    REMOVED once its refuter upheld it, kb/Work PB4995). A contested or unadjudicated rule is the fleet's, and not in it."""
    results = json.loads((state / "results.json").read_text(encoding="utf-8"))
    ed = edition(1985)
    want, bridged = ed.next_held, ed.bridged
    records = [{"rule-id": rid, "next": {"kind": d["change"]["kind"], "edition": want, "to": d["change"]["to"],
                                         "bridge": bridged, "reason": d["change"]["reason"], "source": source}}
               for rid, d in sorted(results.items()) if "change" in d]
    out.write_text(json.dumps({"batch": f"lineage-change:{source}", "records": records}, indent=1, ensure_ascii=False) + "\n",
                   encoding="utf-8", newline="\n")
    kinds = {k: sum(1 for r in records if r["next"]["kind"] == k) for k in NEXT_KINDS}
    print(f"change-batch: {len(records)} change record(s) {kinds} -> {out}")
    return 0


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    from inventory_schema import load_schema  # noqa: PLC0415
    schema = load_schema()
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
        base = {"kind": KEPT, "edition": 2023, "to": ["SR-13.18.38.3-1"], "bridge": True, "reason": "", "source": "planted"}
        return {"rule-id": "1985-SR-VI.5.8.3-1", "verdict": "", "next": {**base, **nxt}}

    ok(ed.adjudicated[-1] == "next" and "editions" not in ed.adjudicated and "status" not in ed.adjudicated
       and edition(2023).newest and not ed.newest,
       "an older edition's store holds next, not editions; status is the catalog's; 2023 is the newest held edition")
    ok(ed.next_held == 2023 and ed.bridged and not edition(2023).bridged,
       "1985's next held edition is 2023, a bridge while 2002 and 2014 are not held")
    ok(ed.blank()["next"] == {} and edition(2023).blank()["editions"] == "" and ed.locator == "locator",
       "an unadjudicated rule's fields are empty, and each edition names the catalog key that locates a rule")
    ok(next_errors(row(), ed, targets) == [], "a KEPT record into a rule of the next edition, bridged, is legal")
    ok(any("bridge must be True" in e for e in next_errors(row(bridge=False), ed, targets)),
       "a 1985 record that does not say it bridges 2002 and 2014 is refused")
    ok(any("no rule of edition 2023" in e for e in next_errors(row(to=["SR-99.9.9-1"]), ed, targets)),
       "a KEPT record into a rule the next edition lacks is refused")
    ok(any("names no rule" in e for e in next_errors(row(kind=REMOVED, reason="deleted"), ed, targets))
       and next_errors(row(kind=REMOVED, to=[], reason="the communication module is deleted"), ed, targets) == [],
       "a REMOVED record names no rule, and states its reason")
    ok(any("states its reason" in e for e in next_errors(row(kind=CHANGED), ed, targets)),
       "a CHANGED record states its reason")
    held_verdict = dict(row(), verdict="CONFORMS")
    ok(any("holds no verdict" in e for e in next_errors(held_verdict, ed, targets)),
       "a KEPT rule holding a verdict of its own is refused: the chain's newest rule holds it")
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
    import copy  # noqa: PLC0415
    from inventory_schema import SCHEMA_PATH, Schema, editions_of, held_edition  # noqa: PLC0415
    from inventory_store import inventory_dir  # noqa: PLC0415
    raw = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
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
              "next": {"kind": KEPT, "edition": 2027, "to": ["SR-1"], "bridge": False, "reason": "", "source": "planted"}}
    t27 = {"SR-1": {"rule-id": "SR-1", "state": "OK", "editions": "85,2002,2014,2023,2027"}}
    ok(next_errors(kept23, e23, t27) == [] and state_of(kept23, e23, s_held, t27) == "OK"
       and state_of(kept23, e23, s_held, {"SR-1": dict(t27["SR-1"], editions="2027")}) == "GAP",
       "a 2023 row KEPT into 2027 is judged by its chain at its own spelling, 2023")
    print(f"=== LINEAGE SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The rule lineage across editions (kb/Work PB4991).")
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    kb = sub.add_parser("change-batch", help="every change record the audit decided (its upheld SAME answers as KEPT, "
                                             "its upheld adjudications)")
    kb.add_argument("--state", required=True, type=pathlib.Path, help="the audit run's directory (results.json)")
    kb.add_argument("--out", required=True, type=pathlib.Path)
    kb.add_argument("--source", help="the run named in each record's next.source (default: the state directory's name)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd == "change-batch":
        return change_batch(a.state, a.out, a.source or f"audit-1985/{a.state.name}")
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
