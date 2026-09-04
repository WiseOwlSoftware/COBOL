#!/usr/bin/env python3
"""Build / refresh the traceability inventory — the P14 burn-down instrument.

`scripts/session-probe.ps1` already looks for tests/version-matrix/traceability-inventory.json and reports
"invent: N rows · M GAP (v1.0 = zero GAP)". Until now that file did not exist, so the probe said "not built yet"
and the project had no burn-down number at all.

This seeds one inventory row per normative rule from the Phase-A catalog, and is RESUMABLE by design: an existing
row's verdict, code-location, test-ref and notes are PRESERVED across re-runs (the design doc requires that a
rule's verdict persist across sessions — Phase B is many sessions of work). Re-running after the spec submodule
moves adds new rules and reports removed ones; it never silently discards adjudicated work.

    python scripts/spec/build_inventory.py            # create or refresh
    python scripts/spec/build_inventory.py --stats    # report only, write nothing

⛔ THIS SCRIPT SEEDS ROWS; IT NEVER WRITES A VERDICT. Verdicts are recorded by `record_verdicts.py`, which merges
a validated batch file — see there for why hand-editing the inventory is not an option. The two agree because
neither owns the rules: the verdict vocabulary, each verdict's required evidence and the GAP/OK derivation are
DATA, in `tests/version-matrix/inventory-schema.json`, loaded through `inventory_schema.py`.

Row schema (one per rule):
    rule-id / section / kind / ordinal / subject         from the Phase-A catalog (regenerated, authoritative)
    state                                    derived — never written by hand; the GAP count is what session-probe
                                             reports and what v1.0 is defined against
    verdict / code-location / test-ref /     the adjudicated fields, preserved across every rebuild;
      editions / notes                       their vocabulary and requirements live in inventory-schema.json
"""
from __future__ import annotations

import argparse
import importlib
import json
import sys
from collections import Counter

from inventory_schema import (ADJUDICATED as CARRIED, CATALOG_PATH as CATALOG,
                              INVENTORY_PATH as INVENTORY, REPO, load_schema, write_inventory)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--stats", action="store_true", help="report only; write nothing")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if not CATALOG.exists():
        sys.exit(f"catalog not found: {CATALOG}\nRun: python scripts/spec/extract_rule_catalog.py")

    schema = load_schema()
    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]

    prior: dict[str, dict] = {}
    if INVENTORY.exists():
        for row in json.loads(INVENTORY.read_text(encoding="utf-8")):
            prior[row["rule-id"]] = row

    out: list[dict] = []
    for r in rules:
        row = {
            "rule-id": r["id"], "section": r["section"], "kind": r["kind"], "ordinal": r["ordinal"],
            "subject": r["subject"],
            "verdict": "", "code-location": "", "test-ref": "", "derivation": "", "editions": "", "notes": "",
        }
        if old := prior.get(r["id"]):
            for k in CARRIED:
                if old.get(k):
                    row[k] = old[k]
        row["state"] = schema.state_for(row)
        out.append(row)

    added = [r["rule-id"] for r in out if r["rule-id"] not in prior]
    removed = [rid for rid in prior if rid not in {r["rule-id"] for r in out}]
    carried = sum(1 for r in out if any(r[k] for k in CARRIED))

    gaps = sum(1 for r in out if r["state"] == "GAP")
    print(f"rules in catalog : {len(rules)}")
    print(f"inventory rows   : {len(out)}")
    print(f"  GAP            : {gaps}")
    print(f"  resolved       : {len(out) - gaps}")
    if prior:
        print(f"  carried forward: {carried} row(s) with adjudicated work preserved")
        if added:
            print(f"  NEW            : {len(added)} (spec moved — these need verdicts)")
        if removed:
            print(f"  ⚠ REMOVED      : {len(removed)} rows no longer in the spec — verdicts dropped:")
            for rid in removed[:10]:
                print(f"       {rid}")

    by_clause = Counter(r["section"].split(".")[0] for r in out)
    print("\n  GAP by top-level clause (the P14 work map):")
    for sec, n in sorted(by_clause.items(), key=lambda kv: (0, int(kv[0]), "") if kv[0].isdigit() else (1, 0, kv[0])):
        g = sum(1 for r in out if r["section"].split(".")[0] == sec and r["state"] == "GAP")
        print(f"      §{sec:<3s} {g:5d} / {n}")

    if args.stats:
        return 0

    write_inventory(out)
    print(f"\nwrote {INVENTORY.relative_to(REPO)}  ({INVENTORY.stat().st_size / 1_048_576:.1f} MB)")
    print("session-probe will now report the live GAP count.")

    # ⛔ REGENERATE THE VAULT VIEW IN THE SAME RUN THAT MOVES THE INVENTORY (2026-08-04). kb/Conformance/ is a
    # generated burn-down over exactly this file, and because nothing ever re-ran its generator it drifted: five
    # of twelve clause notes were wrong and §15's claimed 533 GAP where the live number was 481 — the dashboard
    # UNDER-REPORTED real progress by 52 rows and was still read as current. A view regenerated only when someone
    # remembers is a hand-maintained artifact wearing a generator's clothes. Wiring it here means the two cannot
    # diverge without bypassing both; `gen_conformance_notes.py --check` is the gate that proves they have not.
    # ⚠ NON-FATAL BY DESIGN: the inventory is the SSOT and must be written even if the vault view cannot be —
    # failing the inventory write because a derived note failed would invert their importance.
    try:
        import gen_conformance_notes
        importlib.reload(gen_conformance_notes)
        rc = gen_conformance_notes.main_write()
        print(f"regenerated the kb/Conformance burn-down view (rc={rc})")
    except Exception as exc:  # noqa: BLE001 - a derived view must never block the SSOT
        print(f"⚠ could not regenerate kb/Conformance: {exc}\n"
              f"  run: python scripts/spec/gen_conformance_notes.py")
    return 0


if __name__ == "__main__":
    sys.exit(main())
