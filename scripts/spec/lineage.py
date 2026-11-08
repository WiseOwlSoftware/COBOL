#!/usr/bin/env python3
"""THE RULE LINEAGE: one rule list per COBOL edition, linked by one change record per adjacent pair (kb/Work PB4991).

    python scripts/spec/lineage.py --check              # the gate audit: every older edition's store is consistent
    python scripts/spec/lineage.py kept-batch --state DIR --out FILE   # the audit's upheld SAME answers as KEPT records
    python scripts/spec/lineage.py --self-test

The design is DESIGN-spec-conformance-review.md §10.2 (owner, 2026-11-08). In short:

- Each edition has its own catalog and its own inventory store (`inventory_store.inventory_dir`). The 2023 store is the
  newest held edition's; an OLDER edition's row carries `next`, its one change record against the next HELD edition:
  `{"kind": "KEPT"|"CHANGED"|"REMOVED", "edition": <int>, "to": [<rule ids of that edition>], "bridge": <bool>,
  "reason": "<why>", "source": "<the audit run or adjudication that decided it>"}`. A rule of the newer edition that no
  older `to` names is NEW: derived, never written. `bridge` is true while an edition between the two is not held (1985
  straight to 2023 until 2002 and 2014 are transcribed, kb/Work PB4813).
- `status` is the rule's standing in its own edition: `normal`, or `obsolete` (§1985:I.1.5.2.3: an obsolete element is
  still required, and is to be deleted at the next revision). Deprecation is KEPT into an obsolete rule, then REMOVED.
- A verdict is held once per KEPT chain, on the chain's newest row: a KEPT row carries no verdict, witness or derivation
  of its own (`next_errors` refuses them), and its state is its chain's at its own edition (`state_of`): every `to` row
  is OK, and every `to` row's `editions` band includes the older edition, which is what places the chain's test there.
  A CHANGED or REMOVED row, and a row not yet classified, is judged on its own fields by `Schema.state_for`.

This module is the one definition of an older edition's row: its fields (`Edition`), the change record's shape
(`next_errors`) and its state (`state_of`). `record_verdicts.py --edition`, `build_inventory.py --edition` and this
audit read it; nothing else spells the rules.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import sys
from collections import Counter
from typing import Any, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from extract_rule_catalog import OUT, OUT_1985  # noqa: E402  (each edition's catalog)
from inventory_schema import ADJUDICATED, WITNESS_FIELDS  # noqa: E402
from inventory_store import inventory_dir, load_inventory  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

KEPT, CHANGED, REMOVED = "KEPT", "CHANGED", "REMOVED"
NEXT_KINDS = (KEPT, CHANGED, REMOVED)
NEXT_KEYS = frozenset({"kind", "edition", "to", "bridge", "reason", "source"})
STATUSES = ("normal", "obsolete")
#: Every edition in order, held or not: the change record names the next HELD one, and is a bridge over any between.
EDITIONS = (1985, 2002, 2014, 2023)
#: The `editions` spelling of an edition in the 2023 rows' bands (inventory-schema.json `editions.values`).
BAND_NAME = {1985: "85", 2002: "2002", 2014: "2014", 2023: "2023"}


@dataclasses.dataclass(frozen=True)
class Edition:
    """An edition's rule list: its catalog, its store and the adjudicated fields its rows carry."""
    year: int
    catalog: pathlib.Path

    @property
    def newest(self) -> bool:
        return self.year == HELD[-1]

    @property
    def adjudicated(self) -> tuple[str, ...]:
        """The newest edition's rows carry their `editions` band; an older edition's rows are rules of that edition
        alone, and carry the change record and the status instead (kb/Work PB4993)."""
        if self.newest:
            return ADJUDICATED
        return tuple(f for f in ADJUDICATED if f != "editions") + ("next", "status")

    @property
    def statements(self) -> tuple[str, ...]:
        return tuple(f for f in self.adjudicated if f not in WITNESS_FIELDS)

    def next_held(self) -> int | None:
        later = [e for e in HELD if e > self.year]
        return later[0] if later else None

    def rules(self) -> list[dict[str, Any]]:
        if not self.catalog.exists():
            raise SystemExit(f"catalog not found: {self.catalog}\nRun: python scripts/spec/extract_rule_catalog.py "
                             f"--edition {self.year}")
        return json.loads(self.catalog.read_text(encoding="utf-8"))["rules"]


#: The HELD editions, oldest first: those with a catalog and a store.
EDITION_OF: dict[int, Edition] = {1985: Edition(1985, OUT_1985), 2023: Edition(2023, OUT)}
HELD: tuple[int, ...] = tuple(sorted(EDITION_OF))


def edition(year: int) -> Edition:
    if year not in EDITION_OF:
        raise SystemExit(f"lineage: edition {year} is not held (lineage.EDITION_OF names {sorted(EDITION_OF)})")
    return EDITION_OF[year]


def next_errors(row: dict[str, Any], ed: Edition, targets: dict[str, dict[str, Any]]) -> list[str]:
    """Why `row`'s `next` (and `status`) are not a legal change record of edition `ed` — empty when they are, or when
    the row has none yet. `targets` are the next held edition's rows by id."""
    errs: list[str] = []
    status = row.get("status") or "normal"
    if status not in STATUSES:
        errs.append(f"status {status!r} is not one of {list(STATUSES)}")
    nxt = row.get("next")
    if nxt in (None, {}, ""):
        return errs
    if not isinstance(nxt, dict):
        return errs + [f"next is {type(nxt).__name__}, not an object"]
    if unknown := set(nxt) - NEXT_KEYS:
        errs.append(f"next carries unknown key(s) {sorted(unknown)}; it may carry {sorted(NEXT_KEYS)}")
    kind, to = nxt.get("kind"), nxt.get("to")
    if kind not in NEXT_KINDS:
        errs.append(f"next.kind {kind!r} is not one of {list(NEXT_KINDS)}")
    want = ed.next_held()
    if nxt.get("edition") != want:
        errs.append(f"next.edition must be {want}, the next held edition after {ed.year}")
    bridged = any(ed.year < e < (want or 0) for e in EDITIONS)
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
    band = BAND_NAME[ed.year]
    for t in nxt.get("to") or []:
        target = targets.get(t)
        if target is None or target.get("state") != "OK":
            return "GAP"
        if "editions" in target and band not in schema.split(target.get("editions", "") or "", schema.editions_sep):
            return "GAP"
    return "OK" if nxt.get("to") else "GAP"


def targets_of(ed: Edition) -> dict[str, dict[str, Any]]:
    """The next held edition's rows by id (its store as written, each with its stored state)."""
    nxt = ed.next_held()
    if nxt is None:
        return {}
    return {r["rule-id"]: r for r in load_inventory(inventory_dir(nxt))}


def new_rules(ed: Edition, rows: Iterable[dict[str, Any]], targets: dict[str, dict[str, Any]]) -> list[str]:
    """The next edition's rules no row of `ed` carries into: NEW at that edition (derived, never written)."""
    reached = {t for r in rows for t in ((r.get("next") or {}).get("to") or [])}
    return [t for t in targets if t not in reached]


# -- the audit ------------------------------------------------------------------------------------------------------------

def check(schema=None) -> int:
    """Every older edition's store: one row per catalog rule, a legal change record and status on each row, and each
    row's stored state its derived one. Prints the counts per edition; returns 1 on a finding."""
    from inventory_schema import load_schema  # noqa: PLC0415
    schema = schema or load_schema()
    findings: list[str] = []
    for year in HELD[:-1]:
        ed = EDITION_OF[year]
        d = inventory_dir(year)
        if not d.is_dir():
            findings.append(f"{year}: no store at {d} (python scripts/spec/build_inventory.py --edition {year})")
            continue
        rows = load_inventory(d)
        targets = targets_of(ed)
        ids = {r["id"] for r in ed.rules()}
        have = {r["rule-id"] for r in rows}
        findings += [f"{year}: {rid}: no catalog rule (the store is stale: rebuild it)" for rid in sorted(have - ids)]
        findings += [f"{year}: {rid}: a catalog rule with no row" for rid in sorted(ids - have)]
        kinds: Counter[str] = Counter()
        for r in rows:
            for e in next_errors(r, ed, targets):
                findings.append(f"{year}: {r['rule-id']}: {e}")
            if (want := state_of(r, ed, schema, targets)) != r.get("state"):
                findings.append(f"{year}: {r['rule-id']}: state {r.get('state')!r} is stored, {want!r} is derived")
            kinds[(r.get("next") or {}).get("kind") or "unclassified"] += 1
        gaps = sum(1 for r in rows if r.get("state") == "GAP")
        print(f"lineage {year}: {len(rows)} rows, {gaps} GAP; change records {dict(sorted(kinds.items()))}; "
              f"{len(new_rules(ed, rows, targets))} rule(s) of {ed.next_held()} NEW (derived)")
    for f in findings[:40]:
        print(f"  ✗ {f}")
    print(f"=== LINEAGE: {'GREEN' if not findings else f'RED — {len(findings)} finding(s)'} ===")
    return 1 if findings else 0


# -- the audit's KEPT records ---------------------------------------------------------------------------------------------

def kept_batch(state: pathlib.Path, out: pathlib.Path, source: str) -> int:
    """A record_verdicts batch of the edition audit's CARRIES rules (map SAME, verify UPHELD) as KEPT change records.
    The audit's DISPUTED and OWN-ROW rules are the fleet's (kb/Work PB4995) and are not in it."""
    results = json.loads((state / "results.json").read_text(encoding="utf-8"))
    ed = EDITION_OF[1985]
    want, bridged = ed.next_held(), any(1985 < e < (ed.next_held() or 0) for e in EDITIONS)
    records = [{"rule-id": rid, "next": {"kind": KEPT, "edition": want, "to": d["rules_2023"], "bridge": bridged,
                                         "reason": "", "source": source}}
               for rid, d in sorted(results.items()) if d["disposition"] == "CARRIES"]
    out.write_text(json.dumps({"batch": f"lineage-kept:{source}", "records": records}, indent=1, ensure_ascii=False) + "\n",
                   encoding="utf-8", newline="\n")
    print(f"kept-batch: {len(records)} KEPT record(s) -> {out}")
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

    ed = EDITION_OF[1985]
    targets = {"SR-13.18.38.3-1": {"rule-id": "SR-13.18.38.3-1", "state": "OK", "editions": "85,2002,2014,2023"},
               "SR-13.18.38.3-2": {"rule-id": "SR-13.18.38.3-2", "state": "OK", "editions": "2002,2014,2023"},
               "GR-14.9.21.4-2": {"rule-id": "GR-14.9.21.4-2", "state": "GAP", "editions": "85,2002,2014,2023"}}

    def row(**nxt: Any) -> dict[str, Any]:
        base = {"kind": KEPT, "edition": 2023, "to": ["SR-13.18.38.3-1"], "bridge": True, "reason": "", "source": "planted"}
        return {"rule-id": "1985-SR-VI.5.8.3-1", "verdict": "", "next": {**base, **nxt}}

    ok(ed.adjudicated[-2:] == ("next", "status") and "editions" not in ed.adjudicated and EDITION_OF[2023].newest,
       "an older edition's rows carry next and status, not editions; 2023 is the newest held edition")
    ok(ed.next_held() == 2023, "1985's next held edition is 2023 while 2002 and 2014 are not held")
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
    ok(any("status" in e for e in next_errors(dict(row(), status="deprecated"), ed, targets)),
       "a status outside normal and obsolete is refused")
    ok(state_of(row(), ed, schema, targets) == "OK", "a KEPT row is OK when its chain's row is OK and its band includes 85")
    ok(state_of(row(to=["SR-13.18.38.3-2"]), ed, schema, targets) == "GAP",
       "a KEPT row whose chain's test is not placed at 85 (the band starts at 2002) is a GAP")
    ok(state_of(row(to=["SR-13.18.38.3-1", "GR-14.9.21.4-2"]), ed, schema, targets) == "GAP",
       "a KEPT row into two rules is a GAP while either is")
    ok(state_of({"rule-id": "x", "verdict": ""}, ed, schema, targets) == "GAP", "an unclassified row with no verdict is a GAP")
    ok(new_rules(ed, [row()], targets) == ["SR-13.18.38.3-2", "GR-14.9.21.4-2"],
       "a rule of the next edition no record reaches is NEW")
    print(f"=== LINEAGE SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The rule lineage across editions (kb/Work PB4991).")
    ap.add_argument("--check", action="store_true", help="the gate audit of every older edition's store")
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    kb = sub.add_parser("kept-batch", help="the audit's upheld SAME answers as KEPT records")
    kb.add_argument("--state", required=True, type=pathlib.Path, help="the audit run's directory (results.json)")
    kb.add_argument("--out", required=True, type=pathlib.Path)
    kb.add_argument("--source", help="the run named in each record's next.source (default: the state directory's name)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.cmd == "kept-batch":
        return kept_batch(a.state, a.out, a.source or f"audit-1985/{a.state.name}")
    if a.check:
        return check()
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
