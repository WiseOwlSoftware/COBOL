#!/usr/bin/env python3
"""Record a batch of Phase-B conformance verdicts into the traceability inventory.

THE PROBLEM THIS SOLVES. Phase A left a 3,790-rule catalog and an inventory row per rule, and then there was no
way to write a verdict into one. `build_inventory.py` only ever PRESERVES the adjudicated fields across a rebuild;
it never sets them. The alternative — reviewers hand-editing a 1 MB JSON array — loses a batch the first time two
of them overlap, and offers no point at which anything is checked.

So verdicts are never written by hand. A reviewer (or a Phase-B agent) emits a BATCH FILE; this merges it.

    python scripts/spec/record_verdicts.py <batch.json> [<batch.json> ...]
    python scripts/spec/record_verdicts.py --dry-run <batch.json>     # validate + report, write nothing

A batch file is a JSON list of records, or an object with a "records" list (and optionally a "batch" label):

    {"batch": "phase-b-15.7-abs",
     "records": [
       {"rule-id": "AR-15.7-1",
        "verdict": "CONFORMS",
        "code-location": "src/Cobol.Net.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
        "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
        "editions": "85,2002,2014,2023",
        "notes": ""}]}

ALL-OR-NOTHING. Every record in every named batch is validated before ANY of them is merged, and the merge itself
writes through a temp file and an atomic replace. A batch that is half-right leaves the inventory untouched rather
than half-adjudicated — a partially applied batch is the worst outcome available here, because the rows that DID
land look exactly like reviewed work and nothing afterwards can tell them from it.

WHAT IS CHECKED HERE, AND WHAT IS NOT. This validates SHAPE only: the rule-id is real, the verdict is in the
vocabulary, the fields that verdict requires are present, the editions are legal, the syntax of each reference
parses. Whether a `code-location` symbol still exists and whether a `test-ref` names a test that is really on disk
is checked by the C# battery gate `SpecTraceabilityInventoryDriftTests` — deliberately NOT here. That check has to
keep holding as the tree changes under an already-recorded row, so it belongs to something that runs every build,
and implementing it in both places would be exactly the duplication CLAUDE.md rule 5 forbids.
See `scripts/spec/inventory_schema.py` for the full division of labour.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

from inventory_schema import (ADJUDICATED, load_catalog, load_inventory, load_schema, write_inventory)

GATE = 'dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"'
INVENTORY_REL = "tests/version-matrix/traceability-inventory.json"


def read_batch(path: pathlib.Path) -> list[dict]:
    raw = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(raw, dict):
        raw = raw.get("records", [])
    if not isinstance(raw, list):
        raise SystemExit(f"{path}: expected a list of records, or an object with a 'records' list")
    return raw


def validate(records: list[tuple[pathlib.Path, int, dict]], schema, known: set[str]) -> list[str]:
    """Every shape violation across the whole set — reported together, never one at a time.

    Stopping at the first bad record would make a reviewer re-run this once per mistake, and a batch produced by a
    fan-out of agents tends to carry the SAME mistake in many rows: seeing all of them at once is what turns a
    twenty-round correction loop into one.
    """
    bad: list[str] = []
    seen: dict[str, str] = {}

    for path, i, rec in records:
        where = f"{path.name}[{i}]"
        rid = rec.get("rule-id", "")
        if not rid:
            bad.append(f"{where}: no rule-id")
            continue
        if rid not in known:
            bad.append(f"{where}: rule-id '{rid}' is not in the catalog")
        if rid in seen and seen[rid] != where:
            bad.append(f"{where}: rule-id '{rid}' recorded twice in this run (also {seen[rid]})")
        seen.setdefault(rid, where)

        if unknown := set(rec) - {"rule-id", *ADJUDICATED}:
            bad.append(f"{where}: unknown field(s) {sorted(unknown)} — a record may set only {list(ADJUDICATED)}")

        verdict = rec.get("verdict", "")
        if not verdict:
            bad.append(f"{where}: no verdict")
            continue
        if verdict not in schema.verdicts:
            bad.append(f"{where}: verdict '{verdict}' is not in the vocabulary {sorted(schema.verdicts)}")
            continue

        for field in schema.requires(verdict):
            if not rec.get(field):
                bad.append(f"{where}: verdict {verdict} requires a non-empty '{field}'")

        if ed := rec.get("editions", ""):
            if illegal := [e for e in schema.split(ed, ",") if e not in schema.editions]:
                bad.append(f"{where}: editions {illegal} not in {schema.editions}")

        for loc in schema.split(rec.get("code-location", ""), schema.code_location_sep):
            if not schema.code_location_re.match(loc):
                bad.append(f"{where}: code-location '{loc}' is not '<repo-relative-path>[#Symbol]'")

        for ref in schema.split(rec.get("test-ref", ""), schema.test_ref_sep):
            scheme = ref.split(":", 1)[0]
            if scheme not in schema.test_ref_forms:
                bad.append(f"{where}: test-ref '{ref}' — unknown form '{scheme}', "
                           f"expected one of {sorted(schema.test_ref_forms)}")
            elif ":" not in ref or not ref.split(":", 1)[1].strip():
                bad.append(f"{where}: test-ref '{ref}' has an empty body after '{scheme}:'")

    return bad


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("batches", nargs="+", type=pathlib.Path, help="one or more verdict batch files")
    ap.add_argument("--dry-run", action="store_true", help="validate and report; write nothing")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    schema = load_schema()
    rows = load_inventory()
    by_id = {r["rule-id"]: r for r in rows}
    known = {r["id"] for r in load_catalog()}

    records: list[tuple[pathlib.Path, int, dict]] = []
    for path in args.batches:
        if not path.exists():
            sys.exit(f"batch not found: {path}")
        records += [(path, i, rec) for i, rec in enumerate(read_batch(path))]

    if not records:
        print("no records in the named batch file(s) — nothing to do")
        return 0

    if bad := validate(records, schema, known):
        print(f"⛔ {len(bad)} shape violation(s) — NOTHING was written:\n")
        for b in bad:
            print(f"   {b}")
        return 1

    gap_before = sum(1 for r in rows if r["state"] == "GAP")
    changed, rewritten = 0, []
    for _, _, rec in records:
        row = by_id[rec["rule-id"]]
        had = row.get("verdict") or ""
        if had and had != rec["verdict"]:
            rewritten.append(f"{rec['rule-id']}: {had} → {rec['verdict']}")
        before = {k: row.get(k, "") for k in ADJUDICATED}
        for field in ADJUDICATED:
            row[field] = rec.get(field, "")
        row["state"] = schema.state_for(row)
        if any(row[k] != before[k] for k in ADJUDICATED):
            changed += 1

    gap_after = sum(1 for r in rows if r["state"] == "GAP")
    verdicts = Counter(rec["verdict"] for _, _, rec in records)

    print(f"records        : {len(records)} across {len(args.batches)} batch file(s)")
    print(f"rows changed   : {changed}")
    for v, n in sorted(verdicts.items(), key=lambda kv: -kv[1]):
        closes = " (closes the GAP)" if v in schema.resolving else ""
        print(f"    {v:<24s} {n:5d}{closes}")
    if rewritten:
        print(f"\n  ⚠ {len(rewritten)} row(s) RE-ADJUDICATED — a prior verdict was replaced:")
        for r in rewritten[:10]:
            print(f"       {r}")
    print(f"\nGAP            : {gap_before} → {gap_after}  ({gap_before - gap_after:+d} closed of {len(rows)})")

    if args.dry_run:
        print("\n--dry-run: nothing written.")
        return 0

    write_inventory(rows)
    print(f"\nwrote {INVENTORY_REL}")
    print(f"⛔ NOW RUN THE GATE — it is what checks that these references actually resolve:\n    {GATE}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
