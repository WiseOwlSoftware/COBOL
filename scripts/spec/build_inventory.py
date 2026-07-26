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

Row schema (one per rule):
    rule-id / section / kind / ordinal / subject / page   from the Phase-A catalog (regenerated, authoritative)
    state           GAP until a verdict is recorded — this is what session-probe counts
    verdict         CONFORMS | DIVERGES | PARTIAL | NOT-IMPLEMENTED | DOCUMENTED-NON-SUPPORT | NEEDS-OWNER-DECISION
    code-location   the implementing file:line, or "" if not located
    test-ref        the covering spec-derived golden/xUnit, or "" (Phase C writes these)
    editions        85 / 2002 / 2014 / 2023 applicability once determined
    notes           the fix-queue item id for a DIVERGES, or the owner decision for a non-support
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

REPO = pathlib.Path(__file__).resolve().parents[2]
CATALOG = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
INVENTORY = REPO / "tests" / "version-matrix" / "traceability-inventory.json"

# A row is "resolved" (no longer a GAP) only with one of these verdicts. NEEDS-OWNER-DECISION deliberately stays a
# GAP: an unanswered question is not coverage.
RESOLVED = {"CONFORMS", "DOCUMENTED-NON-SUPPORT"}
CARRIED = ("verdict", "code-location", "test-ref", "editions", "notes")


def state_for(row: dict) -> str:
    return "OK" if row.get("verdict") in RESOLVED and row.get("test-ref") else "GAP"


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

    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]

    prior: dict[str, dict] = {}
    if INVENTORY.exists():
        for row in json.loads(INVENTORY.read_text(encoding="utf-8")):
            prior[row["rule-id"]] = row

    out: list[dict] = []
    for r in rules:
        row = {
            "rule-id": r["id"], "section": r["section"], "kind": r["kind"], "ordinal": r["ordinal"],
            "subject": r["subject"], "page": r["page"],
            "verdict": "", "code-location": "", "test-ref": "", "editions": "", "notes": "",
        }
        if old := prior.get(r["id"]):
            for k in CARRIED:
                if old.get(k):
                    row[k] = old[k]
        row["state"] = state_for(row)
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
    for sec, n in sorted(by_clause.items(), key=lambda kv: int(kv[0])):
        g = sum(1 for r in out if r["section"].split(".")[0] == sec and r["state"] == "GAP")
        print(f"      §{sec:<3s} {g:5d} / {n}")

    if args.stats:
        return 0

    INVENTORY.parent.mkdir(parents=True, exist_ok=True)
    INVENTORY.write_text(json.dumps(out, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"\nwrote {INVENTORY.relative_to(REPO)}  ({INVENTORY.stat().st_size / 1_048_576:.1f} MB)")
    print("session-probe will now report the live GAP count.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
