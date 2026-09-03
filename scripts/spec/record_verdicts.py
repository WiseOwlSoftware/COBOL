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
parses, the register anchor the row's KIND computes is present, and no fragment on an `anchored-files` path is
outside that file's anchor space. All of that is decidable from the record plus the schema, with no disk access.
Whether a `code-location` symbol still exists and whether a `test-ref` names a test that is really on disk
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


def validate(records: list[tuple[pathlib.Path, int, dict]], schema, catalog: dict[str, str]) -> list[str]:
    """Every shape violation across the whole set — reported together, never one at a time.

    Stopping at the first bad record would make a reviewer re-run this once per mistake, and a batch produced by a
    fan-out of agents tends to carry the SAME mistake in many rows: seeing all of them at once is what turns a
    twenty-round correction loop into one.

    `catalog` maps a rule-id to its catalog KIND, because two of the shape rules are per-kind: the computed
    register anchor a `kinds` entry obliges, and the anchor space a listed file's fragments must belong to. A
    batch record carries no `kind` of its own — the catalog owns it — so the lookup happens here rather than the
    record being trusted to restate it.
    """
    bad: list[str] = []
    seen: dict[str, str] = {}

    for path, i, rec in records:
        where = f"{path.name}[{i}]"
        rid = rec.get("rule-id", "")
        if not rid:
            bad.append(f"{where}: no rule-id")
            continue
        if rid not in catalog:
            bad.append(f"{where}: rule-id '{rid}' is not in the catalog")
        if rid in seen and seen[rid] != where:
            bad.append(f"{where}: rule-id '{rid}' recorded twice in this run (also {seen[rid]})")
        seen.setdefault(rid, where)

        if unknown := set(rec) - {"rule-id", *ADJUDICATED}:
            bad.append(f"{where}: unknown field(s) {sorted(unknown)} — a record may set only {list(ADJUDICATED)}")

        # ⛔ TYPE-CHECK BEFORE TOUCHING A FIELD. A batch file is UNTRUSTED input — it is written by an agent, or
        # by hand — and the first real batch to get this wrong supplied `editions` as a JSON LIST, which made the
        # validator itself throw an AttributeError. A validator that crashes on malformed input tells the author
        # nothing about what to fix and reports no other violation in the file; it must FAIL THE RECORD, not the
        # run. Checked here so every field access below is safe.
        if mistyped := sorted(f for f in ADJUDICATED if f in rec and not isinstance(rec[f], str)):
            for f in mistyped:
                bad.append(f"{where}: '{f}' is {type(rec[f]).__name__}, not a string"
                           + (f" — write it as \"{','.join(map(str, rec[f]))}\"" if isinstance(rec[f], list) else ""))
            continue

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

        locations = schema.split(rec.get("code-location", ""), schema.code_location_sep)
        for loc in locations:
            if not schema.code_location_re.match(loc):
                bad.append(f"{where}: code-location '{loc}' is not '<repo-relative-path>[#Symbol]'")
                continue
            # ⛔ A LISTED FILE'S FRAGMENT IS AN ANCHOR OR IT IS A FAILURE — and a BARE citation of one is the
            # weakest form of the same defect, resolving on File.Exists alone. Five live rows carried
            # `docs/CONFORMANCE.md#7`, which the battery gate's word search satisfies against the digit 7
            # anywhere in a 790-line document, and three more cited the bare path.
            file, _, fragment = loc.partition("#")
            if (rx := schema.anchored_files.get(file)) is not None and not rx.match(fragment):
                bad.append(
                    f"{where}: code-location '{loc}' — '{file}' is an anchored file, so its fragment must match "
                    f"{rx.pattern} ({'no fragment at all' if not fragment else f'got {fragment!r}'})")

        # The row's KIND may oblige a COMPUTED register anchor (`kinds` in the schema). It is derived from the
        # rule-id rather than typed, so a mis-filed determination cannot be spelled — kb/Work A11's failure mode.
        # ⚠ Only a verdict that CLAIMS a determination owes it: `anchor_obliged` exempts the verdicts the kind
        # names in `anchor-exempt-verdicts` (a declined facility withdraws the A.1 item, so there is no §7 row).
        probe = {"rule-id": rid, "kind": catalog.get(rid, ""), "verdict": rec.get("verdict", ""),
                 "code-location": rec.get("code-location", "")}
        anchor = schema.anchor_for(probe)
        if schema.anchor_obliged(probe) and anchor not in locations:
            bad.append(f"{where}: kind {catalog[rid]} requires the register anchor '{anchor}' among its "
                       f"code-location(s) — it is computed from the rule-id, never chosen")

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
    catalog = {r["id"]: r.get("kind", "") for r in load_catalog()}

    records: list[tuple[pathlib.Path, int, dict]] = []
    for path in args.batches:
        if not path.exists():
            sys.exit(f"batch not found: {path}")
        records += [(path, i, rec) for i, rec in enumerate(read_batch(path))]

    if not records:
        print("no records in the named batch file(s) — nothing to do")
        return 0

    if bad := validate(records, schema, catalog):
        print(f"⛔ {len(bad)} shape violation(s) — NOTHING was written:\n")
        for b in bad:
            print(f"   {b}")
        return 1

    gap_before = sum(1 for r in rows if r["state"] == "GAP")
    changed, rewritten = 0, []
    verdicts: Counter[str] = Counter()
    closed: Counter[str] = Counter()
    untested: list[str] = []
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
        verdicts[rec["verdict"]] += 1
        if row["state"] == "OK":
            closed[rec["verdict"]] += 1
        elif rec["verdict"] in schema.resolving:
            untested.append(rec["rule-id"])

    gap_after = sum(1 for r in rows if r["state"] == "GAP")

    print(f"records        : {len(records)} across {len(args.batches)} batch file(s)")
    print(f"rows changed   : {changed}")
    for v, n in sorted(verdicts.items(), key=lambda kv: -kv[1]):
        # Report what ACTUALLY closed, per verdict — not what the verdict is nominally capable of closing. A
        # resolving verdict still needs a SPEC-DERIVED covering test, so "CONFORMS n" and "n rows closed" are
        # different numbers, and printing the first as if it were the second is how a burn-down gets overstated.
        note = ""
        if v in schema.resolving:
            note = f" — {closed[v]} closed the GAP, {n - closed[v]} still test-needed"
        print(f"    {v:<24s} {n:5d}{note}")
    if untested:
        print(f"\n  ⓘ {len(untested)} CONFORMS-but-untested row(s) — verdict recorded, row still open until a"
              f" SPEC-DERIVED test covers it (a NIST golden or a *_MatchesLegacy differential does not):")
        for rid in untested[:10]:
            print(f"       {rid}")
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
