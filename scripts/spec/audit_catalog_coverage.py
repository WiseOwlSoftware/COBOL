#!/usr/bin/env python3
"""Audit the rule catalog's COVERAGE of the transcription, clause by clause (fix-queue PB689).

`extract_rule_catalog.py` builds THE DENOMINATOR of the P14 traceability inventory. Its completeness critics all
ask "did every rule-block heading yield SOMETHING?" — a block that yields 16 of its 42 rules passes every one of
them, because 16 is not zero. PB689 is exactly that shape: `specs/ISO_COBOL_2023.md` rendered the in-clause group label
`SEQUENTIAL FILES` as `## SEQUENTIAL FILES` inside §2023:14.9.51.4's general rules, the extractor's block scan closes a
block at ANY heading, and GR17–GR42 of the WRITE statement — twenty-six normative rules, GR23's '71' among them —
never reached the catalog. The published GAP was therefore computed against a denominator that was silently short.

This audit is the missing critic, and it is keyed on COUNTS rather than on emptiness:

  1. per rule-block clause, the top-level ordinals the transcription PRINTS versus the rows the catalog HOLDS;
  2. every unnumbered interior heading sitting inside a numbered clause's text — the mechanism that hides them.

The printed count is measured independently of the extractor: the clause body runs to the next CLAUSE-NUMBERED
heading (or an annex heading), and the count is the longest ascending column-0 run. That deliberately does NOT
share the extractor's segmentation stack — a critic that reuses the code it audits cannot see that code's bug.

Usage:
    python scripts/spec/audit_catalog_coverage.py                  # full report, the 2023 edition
    python scripts/spec/audit_catalog_coverage.py --edition 2002   # another ISO edition (extract_rule_catalog.ISO)
    python scripts/spec/audit_catalog_coverage.py --held --check   # every held ISO edition: the gate's CATALOG COVERAGE
    python scripts/spec/audit_catalog_coverage.py --json      # machine-readable, for the drift test

EDITION-DRIVEN (kb/Work PB5256, branch review R1-7): an ISO edition's clause headings, its furniture and its page markers
are its own (`IsoEdition`), and `--held` reads every edition the inventory schema holds that the ISO reader reads (1985
is read by rule_catalog_1985.py, whose completeness checks are its own). Where an unreconciled edition's catalog records a
finding at a clause (an ordinal no list explains, a nested tail at column 0), the difference that finding explains is
named, not failed: the finding is the reconciliation's.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from extract_rule_catalog import (  # noqa: E402
    ISO,
    ORDINAL,
    PAGE,
    PARAGRAPH_BLOCKS,
    IsoEdition,
    kind_of,
)
import inventory_schema  # noqa: E402  (the held editions: the schema's `editions.of`)
import spec_pdf  # noqa: E402  (the one edition option)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402


def squash(text: str) -> str:
    """Whitespace and the transcription's `\\)` / `\\.` escapes are not content (extract_rule_catalog's own rule)."""
    return re.sub(r"\s+", " ", text.replace("\\", "")).strip()

REPO = pathlib.Path(__file__).resolve().parents[2]

#: Any markdown heading.
ANY_HEADING = re.compile(r"^#{1,6}\s+\S")
#: A heading whose text OPENS WITH A CLAUSE NUMBER, title or no title. ⛔ Wider than `HEADING`, deliberately:
#: the terminology clause transcribes its 300+ terms as `### 3.1` with the term on the FOLLOWING line, so
#: `HEADING` (which requires a title) does not match them. They are numbered clauses all the same, and a critic
#: that calls them "interior headings" reports 187 findings of which 185 are itself.
NUMBERED_HEADING = re.compile(r"^#{1,6}\s+(?P<num>\d+(?:\.\d+)*)\s*(?P<title>.*?)\s*$")


def numbered_heading(ed: IsoEdition) -> re.Pattern[str]:
    """`NUMBERED_HEADING` of `ed`: a rule-bearing annex's clauses (2002's `A.1.2.3`) are numbered clauses too."""
    if not ed.rule_annexes:
        return NUMBERED_HEADING
    return re.compile(rf"^#{{1,6}}\s+(?P<num>[{ed.rule_annexes}](?:\.\d+)+|\d+(?:\.\d+)*)\s*(?P<title>.*?)\s*$")
#: A heading that legitimately ENDS a numbered clause without carrying a clause number of its own: the annexes,
#: the bibliography and the index. Everything else unnumbered is INTERIOR to whatever clause encloses it.
STRUCTURAL_HEADING = re.compile(
    r"^#{1,6}\s+\*{0,2}(?:Annex\s+[A-Z]\b|Bibliography\b|Index\b|INTERNATIONAL\s+STANDARD\b|"
    r"Information\s+technology\b|BIBLIOGRAPHY\b|Preface\b|Foreword\b|Introduction\b|Tables\b|Figures\b|"
    # the transcription's own parts (verify_publishable.PARTS: 2002's `# Clauses 1 to 16`, `# Annexes A to H`)
    r"Clauses\s+\d|Annexes\s+[A-Z]\b|Front\s+matter\b)",
    re.I)

#: Clauses whose printed ordinals legitimately exceed what the catalog files as top-level rules, with the reason.
#: ⛔ Each entry is an adjudication, not a silencer: the extractor files the surplus ordinals under SUB-LIST ids
#: (`SR-x-L2.1`), so the ROWS exist — only the top-level ordinal arithmetic differs. Anything not declared here
#: is a coverage defect and fails --check.
EXPECTED_SUBLIST_CLAUSES: dict[str, str] = {}


def clause_bodies(lines: list[str], ed: IsoEdition = ISO[2023]) -> tuple[dict[str, list[str]], dict[str, str], list[dict]]:
    """Split the spec into numbered-clause bodies; also return every interior unnumbered heading.

    A clause's body runs to the next NUMBERED heading or the next structural (annex/bibliography) heading —
    never to an unnumbered interior heading, which is the whole point of the audit.
    """
    bodies: dict[str, list[str]] = {}
    titles: dict[str, str] = {}
    interior: list[dict] = []
    cur: str | None = None
    furniture, numbered = ed.furniture, numbered_heading(ed)
    for i, line in enumerate(lines, start=1):
        if furniture.match(line) or PAGE.match(line):
            continue
        if ANY_HEADING.match(line):
            if (m := numbered.match(line)) and not line.lstrip().startswith("["):
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
    ap.add_argument("--held", action="store_true", help="every held edition the ISO reader reads (the gate's audit)")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if not args.held:
        return audit(spec_pdf.edition_data(ISO, args.edition, "ISO rule catalog arm"), args)
    years = [y for y in inventory_schema.HELD if y in ISO]
    print(f"held editions {list(inventory_schema.HELD)}; the ISO reader's {years} (1985's completeness is rule_catalog_1985.py's)")
    verdicts = {y: audit(ISO[y], args) for y in years}
    print(f"=== CATALOG COVERAGE: {'GREEN' if not any(verdicts.values()) else 'RED'} — "
          + " · ".join(f"{y} {'ok' if v == 0 else 'RED'}" for y, v in verdicts.items()) + " ===")
    return 1 if any(verdicts.values()) else 0


def audit(ed: IsoEdition, args: argparse.Namespace) -> int:
    """The critic over one ISO edition `ed`: its report, and its verdict under `--check`."""
    EDITION = ed
    lines = EDITION.spec.read_text(encoding="utf-8", errors="replace").splitlines()
    bodies, titles, interior = clause_bodies(lines, ed)

    admitted: dict[str, object] = {}
    if EDITION.unharvested.exists():
        for sec, info in json.loads(EDITION.unharvested.read_text(encoding="utf-8"))["blocks"].items():
            if info.get("disposition") == "rules":
                admitted[sec] = info["kind"]

    prose_blocks: set[str] = set()
    if EDITION.unharvested.exists():
        prose_blocks = {sec for sec in json.loads(EDITION.unharvested.read_text(encoding="utf-8")).get(PARAGRAPH_BLOCKS, {})
                        if not sec.startswith("_")}

    catalog = json.loads(EDITION.catalog.read_text(encoding="utf-8"))
    # The clauses where the catalog itself records a finding that changes the count: kept in the rule it interrupts
    # (an ordinal no list explains) or folded into its parent (a nested tail at column 0), each one printed ordinal line
    # and no row. An unreconciled edition's findings are its reconciliation's (extract_rule_catalog.Finding).
    recorded: dict[str, list[str]] = {}
    for f in catalog.get("findings", []):
        if f["kind"] in ("unexplained-ordinal", "flattened-tail"):
            recorded.setdefault(f["clause"].split(":", 1)[-1], []).append(f"{f['kind']} line {f['line']}")
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
        if sec in prose_blocks:
            # A typed block whose rule is printed unnumbered is admitted as paragraph rows (kb/Work PB5332,
            # §2002:8.8.4.1.5.2): it has no ordinal to count, and the prose check below holds its rows to its text.
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
            "declared": EXPECTED_SUBLIST_CLAUSES.get(sec) or (
                f"the catalog records {', '.join(recorded[sec])}"
                if sec in recorded and len(printed) - len(rows) == len(recorded[sec]) else None),
        })
    mismatches.sort(key=lambda m: -(m["printed_count"] - m["catalog_rows"]))

    # ⛔ A PROSE CLAUSE ADMITTED AS PARAGRAPH ROWS HOLDS THE CLAUSE, SENTENCE FOR SENTENCE (kb/Work PB284 part 3).
    # No ordinal counts here, so the invariant is the stronger one: the rows' text, in order, IS the clause's body
    # (furniture removed) — a sentence that fell between two paragraphs, a list item that opened a row of its own or
    # a page-break tail dropped from the clause shows as a difference, and so does a row the clause no longer has.
    prose_held: dict[str, list[dict]] = {}
    for r in catalog["rules"]:
        if r.get("shape") == "paragraph":
            prose_held.setdefault(r["section"], []).append(r)
    prose_mismatches: list[dict] = []
    for sec in sorted(set(prose_blocks) | set(prose_held)):
        rows = prose_held.get(sec, [])
        printed = squash(" ".join(bodies.get(sec, [])))
        held_text = squash(" ".join(r["text"] for r in rows))
        why = ("not admitted in the manifest" if sec not in prose_blocks else
               "no rows" if not rows else
               "ordinals are not 1..n" if [r["ordinal"] for r in rows] != list(range(1, len(rows) + 1)) else
               "" if printed == held_text else "row text differs from the clause body")
        if why:
            prose_mismatches.append({"clause": sec, "rows": len(rows), "why": why})

    interior_in_clause = [h for h in interior if h["clause"] is not None]

    if args.json:
        print(json.dumps({
            "mismatches": mismatches,
            "prose_mismatches": prose_mismatches,
            "interior_headings": interior_in_clause,
            "unattached_headings": [h for h in interior if h["clause"] is None],
        }, indent=1, ensure_ascii=False))
    else:
        print(f"\n════ {ed.year} ════\nspec: {EDITION.spec.relative_to(REPO)} ({len(lines):,} lines)   "
              f"catalog: {catalog['rule_count']:,} rules")
        print()
        print(f"⛔ {len(interior_in_clause)} UNNUMBERED INTERIOR HEADING(S) inside a numbered clause's text")
        print("   (each one TRUNCATES its clause's rule block in extract_rule_catalog.py's block scan):")
        for h in interior_in_clause:
            print(f"      line {h['line']:>6}  {ed.label(h['clause']):<18} {h['text']}")
        if not interior_in_clause:
            print("      (none)")
        print()
        print(f"⛔ {len(mismatches)} RULE-BLOCK CLAUSE(S) where the printed ordinals and the catalog rows disagree:")
        for m in mismatches:
            note = f"   [declared: {m['declared']}]" if m["declared"] else ""
            print(f"      {ed.label(m['clause']):<20} printed {m['printed_count']:>3} ordinal line(s) (max {m['printed_max']})"
                  f"   catalog {m['catalog_rows']:>3} row(s)"
                  f"   [{m['printed_count'] - m['catalog_rows']:+d}]{note}")
            for t in m["interior_headings"]:
                print(f"                        interior heading: {t}")
        if not mismatches:
            print("      ✓ every rule-block clause holds exactly the ordinals the transcription prints")
        print()
        print(f"⛔ {len(prose_mismatches)} PROSE CLAUSE(S) admitted as paragraph rows whose rows are not the clause:")
        for m in prose_mismatches:
            print(f"      {ed.label(m['clause']):<20} {m['rows']:>3} row(s)   {m['why']}")
        if not prose_mismatches:
            print(f"      ✓ all {len(prose_blocks)} admitted prose clauses are held sentence for sentence")

    undeclared = [m for m in mismatches if not m["declared"]]
    if args.check:
        return 1 if (undeclared or interior_in_clause or prose_mismatches) else 0
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
