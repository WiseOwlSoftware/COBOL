#!/usr/bin/env python3
"""Emit a verdict BATCH FILE from a `derived-verdicts` selector — the Python half of the shared predicate.

    python scripts/spec/derive_verdict_batch.py --list
    python scripts/spec/derive_verdict_batch.py screen-handling-only validate-only -o batch.json
    python scripts/spec/derive_verdict_batch.py --all -o out/a4-derived.json --notes-from decision

⛔ WHY THIS SCRIPT EXISTS AT ALL, AND WHY IT IS NOT A SECOND COPY OF THE PREDICATE. `inventory-schema.json`'s
`derived-verdicts` $comment has claimed since kb/Work PB198 that "the batch generator (Python) and the drift test
(C#) read the SAME predicate" — the whole justification for writing a selector as DATA rather than as code. That
was **false**: a grep for `derived-verdicts` across the repo returned this schema, `DerivedVerdictDriftTests.cs`
and some prose, and NOTHING under `scripts/`. The PB198 batch of sixteen rows was produced by hand, so the second
reader that makes the data-shape worth its cost did not exist, and three of the six A.4 module refuters found
that independently on the same day. The evaluator lives in `inventory_schema.py` (`DerivedSelector`); this file
is only a CLI over it, and the C# `DerivedVerdictDriftTests.Select` is the other engine. Neither carries a
selector.

⛔ THE SAFETY PROPERTY THIS SCRIPT OWNS, AND IT IS THE ONLY ONE `record_verdicts.py` CANNOT. A derived verdict is
sound exactly when the determination it derives from makes every selected rule UNREACHABLE — so a selector that
lands on a row already adjudicated CONFORMS, PARTIAL or DIVERGES has not found a new row, it has CONTRADICTED a
human reading of the code, silently, in a batch of three hundred. `record_verdicts.py` reports such a rewrite
(`⚠ n row(s) RE-ADJUDICATED`) but still applies it, which is right for an adjudication batch and wrong for this
one. Here it is a HARD FAILURE and nothing is written. A selected row may be blank, or may already carry the
selector's own verdict (agreement — the PB198 cross-check running in the reassuring direction); anything else
means the selector or the determination is wrong, and the answer is to re-derive, never to overwrite.

⛔ AND THE ROWS DO NOT CLOSE. DOCUMENTED-NON-SUPPORT `resolves`, but `state_for` still demands a SPEC-DERIVED
test, because the schema closes a declined row only on evidence that the documented posture is what ACTUALLY
happens (see the schema's $verdict-vs-state). Every row a derived batch stamps stays a GAP until its module's
witness lands. This script prints the projected GAP delta — expected ZERO — so that is visible at generation
time rather than discovered as a disappointment after the merge.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

from inventory_schema import load_catalog, load_inventory, load_schema

RECORD = "python scripts/spec/record_verdicts.py --dry-run {0}"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("selectors", nargs="*", help="derived-verdicts entry name(s)")
    ap.add_argument("--all", action="store_true", help="every selector in the schema")
    ap.add_argument("--list", action="store_true", help="name the selectors and their measured populations")
    ap.add_argument("-o", "--out", type=pathlib.Path, help="write the batch file here")
    ap.add_argument("--notes", default="", help="notes text for every record (default: the selector's decision)")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    schema = load_schema()
    rules = load_catalog()
    inventory = {r["rule-id"]: r for r in load_inventory()}

    if args.list:
        for name, sel in schema.derived.items():
            ids = sel.select(rules)
            held = sum(1 for i in ids if (inventory.get(i, {}).get("verdict") or "") == sel.verdict)
            print(f"  {name:<28s} {len(ids):5d} rules  ({len(ids) - held} to stamp, {held} already at the verdict)"
                  f"  -> {sel.verdict}")
        return 0

    names = list(schema.derived) if args.all else args.selectors
    if not names:
        ap.error("name at least one selector, or pass --all / --list")
    if unknown := [n for n in names if n not in schema.derived]:
        sys.exit(f"unknown selector(s) {unknown} — known: {sorted(schema.derived)}")

    records: list[dict[str, str]] = []
    conflicts: list[str] = []
    orphans: list[str] = []
    held: Counter[str] = Counter()
    emitted: Counter[str] = Counter()
    seen: dict[str, str] = {}

    # Select ONCE per selector. Each pass is 4,311 rules × the arms' regexes, so re-running it inside the record
    # loop turns a two-second script into a quadratic one — measured, on the first run of this file.
    selected = {name: schema.derived[name].select(rules) for name in names}

    for name in names:
        sel = schema.derived[name]
        for rid in selected[name]:
            row = inventory.get(rid)
            if row is None:
                # The catalog and the inventory disagree about what rules exist. That is a rebuild, not a batch.
                orphans.append(f"{rid} (selected by {name})")
                continue
            if rid in seen and seen[rid] != name:
                conflicts.append(f"{rid}: selected by BOTH {seen[rid]} and {name} — two determinations, one row")
                continue
            seen[rid] = name
            had = row.get("verdict") or ""
            if had and had != sel.verdict:
                conflicts.append(f"{rid}: already {had} — a derived selector must not overwrite an adjudicated "
                                 f"verdict ({name})")
                continue
            if had == sel.verdict:
                held[name] += 1
            emitted[name] += 1
            records.append({
                "rule-id": rid,
                "verdict": sel.verdict,
                # ⛔ EVERY OTHER FIELD IS CARRIED FORWARD, NEVER SYNTHESISED. A batch record REPLACES all five
                # adjudicated fields (record_verdicts.py writes `row[field] = rec.get(field, "")`), so a
                # generator that filled only `verdict` and `notes` would silently blank the code-location of
                # every row it touched. Two of these are worth stating outright:
                #  · `notes` — the eleven rows already at this verdict carry per-ROW forensic prose written by
                #    hand ("the DISPLAY Format-2 omitted-COLUMN default …"). The selector's `decision` string is
                #    strictly weaker than that, so an existing note WINS and the decision only fills a blank.
                #    Overwriting them would have traded eleven adjudications for one boilerplate sentence.
                #  · `editions` — left EMPTY where the row has none, and NOT defaulted to all four. FORMAT and
                #    SELECT WHEN are 2002-and-later, so "85,2002,2014,2023" on an A.4.8 row would assert that a
                #    rule which does not exist at COBOL-85 is DECLINED there. An empty field claims nothing;
                #    the edition band is the introduction record's answer and belongs to whoever writes it.
                "code-location": row.get("code-location", ""),
                "test-ref": row.get("test-ref", ""),
                "editions": row.get("editions", ""),
                "notes": row.get("notes", "") or args.notes or sel.decision,
            })

    if orphans:
        print(f"⛔ {len(orphans)} selected rule(s) have NO INVENTORY ROW — rebuild first "
              f"(python scripts/spec/build_inventory.py):")
        for o in orphans[:10]:
            print(f"   {o}")
        return 1
    if conflicts:
        print(f"⛔ {len(conflicts)} CONFLICT(S) — NOTHING was written. A derived verdict claims its rules are "
              f"unreachable; a row that already carries a different verdict is a contradiction to resolve, "
              f"never a row to overwrite:")
        for c in conflicts:
            print(f"   {c}")
        return 1

    for name in names:
        sel = schema.derived[name]
        print(f"  {name:<28s} {emitted[name]:5d} record(s)  "
              f"({held[name]} already at {sel.verdict}, held not changed)")

    would_close = sum(1 for r in records
                      if schema.state_for({**inventory[r["rule-id"]], **{k: r[k] for k in r if k != "rule-id"}})
                      == "OK")
    print(f"\nrecords        : {len(records)}")
    print(f"rows already at the derived verdict : {sum(held.values())}  (no-ops, kept for the drift test)")
    print(f"projected GAP delta : -{would_close}   "
          f"({'as expected — a declined row closes on its WITNESS, not on its verdict' if not would_close else
             'unexpected: check the test-ref that closed it'})")

    if not args.out:
        print("\nno --out given; nothing written.")
        return 0
    args.out.parent.mkdir(parents=True, exist_ok=True)
    payload = {"batch": "derived:" + "+".join(names), "records": records}
    args.out.write_text(json.dumps(payload, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"\nwrote {args.out}")
    print("⛔ NOW VALIDATE IT:\n    " + RECORD.format(args.out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
