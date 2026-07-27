#!/usr/bin/env python3
"""Generate the Obsidian conformance view over the traceability inventory — AGGREGATES, not one note per rule.

⛔ DESIGN CORRECTION. The first version emitted one note per rule: 3,481 files created in a single run, an 82%
increase in the vault's markdown, on top of two ~3 MB indexed documents. It wedged Obsidian mid-sync. The
per-rule frontmatter did buy live Dataview filtering, but that is not worth a 5x vault and a client that will not
open — and the queryable SSOT already exists as JSON, which jq and python query far better than Dataview does.

So the vault view is now the BURN-DOWN, at ~12 notes: a dashboard plus one note per top-level clause. It shows
aggregates and points at the inventory for the full list. Deliberately NOT an exhaustive per-rule enumeration —
3,790 table rows spread over 11 notes would recreate the same problem in a different shape, and the tracker's own
rule for enumerating drift-prone items applies here too.

kb/Conformance/ is a GITIGNORED build output AND is listed in .obsidian/app.json userIgnoreFilters, so it is
neither committed nor indexed. Regenerate freely.

    python scripts/spec/gen_conformance_notes.py
"""
from __future__ import annotations

import json
import pathlib
import shutil
import sys
from collections import Counter, defaultdict

REPO = pathlib.Path(__file__).resolve().parents[2]
INVENTORY = REPO / "tests" / "version-matrix" / "traceability-inventory.json"
OUT = REPO / "kb" / "Conformance"

KIND_LABEL = {
    "SR": "syntax rules", "GR": "general rules", "AR": "argument rules",
    "RV": "returned value rules", "DOC": "Annex A.1 documentation obligations",
    "FMT": "general formats (syntax diagrams)",
}


def clause_key(c: str):
    return (0, int(c), "") if c.isdigit() else (1, 0, c)


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not INVENTORY.exists():
        sys.exit(f"inventory not found: {INVENTORY}\nRun: python scripts/spec/build_inventory.py")

    rows = json.loads(INVENTORY.read_text(encoding="utf-8"))
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)

    by_clause: dict[str, list[dict]] = defaultdict(list)
    for r in rows:
        by_clause[r["section"].split(".")[0]].append(r)

    total = len(rows)
    gaps = sum(1 for r in rows if r["state"] == "GAP")
    kinds = Counter(r["kind"] for r in rows)

    for clause, items in by_clause.items():
        k = Counter(i["kind"] for i in items)
        g = sum(1 for i in items if i["state"] == "GAP")
        subjects = Counter(i["subject"] for i in items)
        note = [
            "---",
            f'title: "§{clause} conformance"',
            f'description: "{len(items)} normative items in clause {clause}; {g} still GAP."',
            f'clause: "{clause}"', f"items: {len(items)}", f"gap: {g}",
            "generated: true",
            "tags: [cobolsharp, conformance, generated]",
            "---", "",
            f"# §{clause} — {len(items)} items, {g} GAP", "",
            "> ⚙ **Generated** from `tests/version-matrix/traceability-inventory.json`. Do not edit — record",
            "> verdicts in the inventory and re-run `scripts/spec/gen_conformance_notes.py`.", "",
            "| kind | items |", "|---|---:|",
        ]
        for kk, n in sorted(k.items()):
            note.append(f"| {KIND_LABEL.get(kk, kk)} | {n} |")
        note += ["", f"## Largest subjects in this clause", ""]
        for subj, n in subjects.most_common(15):
            note.append(f"- **{subj}** — {n} item(s)")
        note += ["",
                 "## The full list",
                 "",
                 "Deliberately not enumerated here — 3,790 rows across these notes is what wedged the vault the",
                 "first time, and per-item rows drift. Query the inventory instead:",
                 "",
                 "```",
                 "python -c \"import json;rows=json.load(open(r'tests/version-matrix/traceability-inventory.json'"
                 ",encoding='utf-8'));"
                 f"print([r['rule-id'] for r in rows if r['section'].startswith('{clause}.') "
                 "and r['state']=='GAP'][:40])\"",
                 "```", "",
                 "See also [[kb/Conformance/_Dashboard|the conformance dashboard]].", ""]
        (OUT / f"clause-{clause}.md").write_text("\n".join(note), encoding="utf-8")

    dash = [
        "---", "title: Conformance Dashboard",
        f'description: "P14 burn-down over {total} normative ISO/IEC 1989:2023 items; v1.0 = zero GAP."',
        f"items: {total}", f"gap: {gaps}", "generated: true",
        "tags: [cobolsharp, conformance, generated, dashboard]", "---", "",
        "# Conformance dashboard", "",
        "> ⚙ **Generated** from the traceability inventory. `v1.0 = zero GAP` (owner decision D13).",
        "> This view is AGGREGATES by design — the per-rule detail lives in the inventory JSON, which queries",
        "> better than Dataview and does not cost 3,790 vault files.", "",
        f"**{total} items · {gaps} GAP · {total - gaps} resolved**", "",
        "## By kind", "", "| kind | items |", "|---|---:|",
    ]
    for kk, n in sorted(kinds.items()):
        dash.append(f"| {KIND_LABEL.get(kk, kk)} | {n} |")
    dash += ["", "## Burn-down by clause", "", "| clause | items | GAP |", "|---|---:|---:|"]
    for c, i in sorted(by_clause.items(), key=lambda kv: clause_key(kv[0])):
        dash.append(f"| [[kb/Conformance/clause-{c}|§{c}]] | {len(i)} | "
                    f"{sum(1 for r in i if r['state'] == 'GAP')} |")
    dash += ["",
             "## Related", "",
             "- [[kb/Remaining Work Tracker]] — §C carries the spec-transcription corrections",
             "- `docs/rearchitecture/spec-reconciliation/REPAIR-PLAN.md` — the repair order for all 210 defects",
             "- `docs/rearchitecture/DESIGN-spec-conformance-review.md` — the P14 Step-0 methodology", ""]
    (OUT / "_Dashboard.md").write_text("\n".join(dash), encoding="utf-8")

    n = sum(1 for _ in OUT.rglob("*.md"))
    print(f"wrote {n} notes into {OUT.relative_to(REPO)}  (was 3,481 — now aggregates)")
    print(f"  {total} items · {gaps} GAP · {len(by_clause)} clause notes + 1 dashboard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
