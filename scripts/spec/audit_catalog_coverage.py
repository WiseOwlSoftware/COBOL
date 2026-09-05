#!/usr/bin/env python3
"""Audit the rule catalog's COVERAGE of the transcription, clause by clause (fix-queue PB689).

`extract_rule_catalog.py` builds THE DENOMINATOR of the P14 traceability inventory. Its completeness critics all
ask "did every rule-block heading yield SOMETHING?" — a block that yields 16 of its 42 rules passes every one of
them, because 16 is not zero. PB689 is exactly that shape: `specs/ISO_COBOL.md` rendered the in-clause group label
`SEQUENTIAL FILES` as `## SEQUENTIAL FILES` inside §14.9.51.4's general rules, the extractor's block scan closes a
block at ANY heading, and GR17–GR42 of the WRITE statement — twenty-six normative rules, GR23's '71' among them —
never reached the catalog. The published GAP was therefore computed against a denominator that was silently short.

This audit is the missing critic, and it is keyed on COUNTS rather than on emptiness:

  1. per rule-block clause, the top-level ordinals the transcription PRINTS versus the rows the catalog HOLDS;
  2. every unnumbered interior heading sitting inside a numbered clause's text — the mechanism that hides them.

The printed count is measured independently of the extractor: the clause body runs to the next CLAUSE-NUMBERED
heading (or an annex heading), and the count is the longest ascending column-0 run. That deliberately does NOT
share the extractor's segmentation stack — a critic that reuses the code it audits cannot see that code's bug.

Usage:
    python scripts/spec/audit_catalog_coverage.py            # full report
    python scripts/spec/audit_catalog_coverage.py --check     # non-zero exit if any clause disagrees
    python scripts/spec/audit_catalog_coverage.py --json      # machine-readable, for the drift test
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from extract_rule_catalog import (  # noqa: E402
    FURNITURE,
    HEADING,
    ORDINAL,
    OUT,
    SPEC,
    UNHARVESTED,
    kind_of,
)

REPO = pathlib.Path(__file__).resolve().parents[2]

#: Any markdown heading.
ANY_HEADING = re.compile(r"^#{1,6}\s+\S")
#: A heading whose text OPENS WITH A CLAUSE NUMBER, title or no title. ⛔ Wider than `HEADING`, deliberately:
#: the terminology clause transcribes its 300+ terms as `### 3.1` with the term on the FOLLOWING line, so
#: `HEADING` (which requires a title) does not match them. They are numbered clauses all the same, and a critic
#: that calls them "interior headings" reports 187 findings of which 185 are itself.
NUMBERED_HEADING = re.compile(r"^#{1,6}\s+(?P<num>\d+(?:\.\d+)*)\s*(?P<title>.*?)\s*$")
#: A heading that legitimately ENDS a numbered clause without carrying a clause number of its own: the annexes,
#: the bibliography and the index. Everything else unnumbered is INTERIOR to whatever clause encloses it.
STRUCTURAL_HEADING = re.compile(
    r"^#{1,6}\s+\*{0,2}(?:Annex\s+[A-Z]\b|Bibliography\b|Index\b|INTERNATIONAL\s+STANDARD\b|"
    r"Information\s+technology\b|BIBLIOGRAPHY\b|Preface\b|Foreword\b|Introduction\b|Tables\b|Figures\b)",
    re.I)

#: Clauses whose printed ordinals legitimately exceed what the catalog files as top-level rules, with the reason.
#: ⛔ Each entry is an adjudication, not a silencer: the extractor files the surplus ordinals under SUB-LIST ids
#: (`SR-x-L2.1`), so the ROWS exist — only the top-level ordinal arithmetic differs. Anything not declared here
#: is a coverage defect and fails --check.
EXPECTED_SUBLIST_CLAUSES: dict[str, str] = {}


def clause_bodies(lines: list[str]) -> tuple[dict[str, list[str]], dict[str, str], list[dict]]:
    """Split the spec into numbered-clause bodies; also return every interior unnumbered heading.

    A clause's body runs to the next NUMBERED heading or the next structural (annex/bibliography) heading —
    never to an unnumbered interior heading, which is the whole point of the audit.
    """
    bodies: dict[str, list[str]] = {}
    titles: dict[str, str] = {}
    interior: list[dict] = []
    cur: str | None = None
    for i, line in enumerate(lines, start=1):
        if FURNITURE.match(line):
            continue
        if ANY_HEADING.match(line):
            if (m := NUMBERED_HEADING.match(line)) and not line.lstrip().startswith("["):
                cur = m.group("num")
                if m.group("title"):
                    titles.setdefault(cur, m.group("title"))
                bodies.setdefault(cur, [])
                continue
            if STRUCTURAL_HEADING.match(line):
                cur = None
                continue
            # an unnumbered, non-structural heading — INTERIOR to whatever clause encloses it
            if cur is not None:
                interior.append({"line": i, "clause": cur, "text": line.rstrip()})
                bodies[cur].append(line)
            else:
                interior.append({"line": i, "clause": None, "text": line.rstrip()})
            continue
        if cur is not None:
            bodies[cur].append(line)
    return bodies, titles, interior


def printed_ordinals(body: list[str]) -> list[int]:
    """EVERY column-0 ordinal line the clause prints, in order — one per rule the transcription shows.

    ⛔ THE INVARIANT IS ONE-ORDINAL-LINE ⇒ ONE CATALOG ROW, and it is counted rather than reconstructed on
    purpose. The extractor's level stack decides what each ordinal is CALLED (top-level `GR-x-7` versus
    sub-list `GR-x-L3.2`); it never legitimately DISCARDS one — an ordinal it cannot place is filed as a nest
    and reported (`unexplained`). So a clause whose row count is below its printed-ordinal count has lost
    rules, whatever the segmentation says, and that comparison needs none of the segmentation logic — which
    is what lets this critic see a bug in the code it audits.

    Sub-items ("a)", indented "1.") are not column-0 and are not counted; they are part of their parent rule.
    """
    return [int(m.group("n")) for ln in body if (m := ORDINAL.match(ln))]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="non-zero exit if any clause disagrees")
    ap.add_argument("--json", action="store_true", help="machine-readable report")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8", errors="replace").splitlines()
    bodies, titles, interior = clause_bodies(lines)

    admitted: dict[str, object] = {}
    if UNHARVESTED.exists():
        for sec, info in json.loads(UNHARVESTED.read_text(encoding="utf-8"))["blocks"].items():
            if info.get("disposition") == "rules":
                admitted[sec] = info["kind"]

    catalog = json.loads(OUT.read_text(encoding="utf-8"))
    held: dict[str, list[dict]] = {}
    for r in catalog["rules"]:
        if r["kind"] in ("FMT", "DOC"):
            continue
        held.setdefault(r["section"], []).append(r)

    mismatches: list[dict] = []
    for sec, body in bodies.items():
        title = titles.get(sec, "")
        if kind_of(title) is None and sec not in admitted:
            continue
        printed = printed_ordinals(body)
        rows = held.get(sec, [])
        if len(printed) == len(rows):
            continue
        mismatches.append({
            "clause": sec,
            "title": title,
            "printed_count": len(printed),
            "printed_max": max(printed) if printed else 0,
            "catalog_rows": len(rows),
            "catalog_top_count": sum(1 for r in rows if r.get("sublist", 1) == 1),
            "interior_headings": [h["text"] for h in interior if h["clause"] == sec],
            "declared": EXPECTED_SUBLIST_CLAUSES.get(sec),
        })
    mismatches.sort(key=lambda m: -(m["printed_count"] - m["catalog_rows"]))

    interior_in_clause = [h for h in interior if h["clause"] is not None]

    if args.json:
        print(json.dumps({
            "mismatches": mismatches,
            "interior_headings": interior_in_clause,
            "unattached_headings": [h for h in interior if h["clause"] is None],
        }, indent=1, ensure_ascii=False))
    else:
        print(f"spec: {SPEC.relative_to(REPO)} ({len(lines):,} lines)   catalog: {catalog['rule_count']:,} rules")
        print()
        print(f"⛔ {len(interior_in_clause)} UNNUMBERED INTERIOR HEADING(S) inside a numbered clause's text")
        print("   (each one TRUNCATES its clause's rule block in extract_rule_catalog.py's block scan):")
        for h in interior_in_clause:
            print(f"      line {h['line']:>6}  §{h['clause']:<12} {h['text']}")
        if not interior_in_clause:
            print("      (none)")
        print()
        print(f"⛔ {len(mismatches)} RULE-BLOCK CLAUSE(S) where the printed ordinals and the catalog rows disagree:")
        for m in mismatches:
            note = f"   [declared: {m['declared']}]" if m["declared"] else ""
            print(f"      §{m['clause']:<14} printed {m['printed_count']:>3} ordinal line(s) (max {m['printed_max']})"
                  f"   catalog {m['catalog_rows']:>3} row(s)"
                  f"   [{m['printed_count'] - m['catalog_rows']:+d}]{note}")
            for t in m["interior_headings"]:
                print(f"                        interior heading: {t}")
        if not mismatches:
            print("      ✓ every rule-block clause holds exactly the ordinals the transcription prints")

    undeclared = [m for m in mismatches if not m["declared"]]
    if args.check:
        return 1 if (undeclared or interior_in_clause) else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
