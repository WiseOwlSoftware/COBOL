#!/usr/bin/env python3
"""Generate the Obsidian conformance view over the traceability inventory.

The inventory JSON is the SSOT (session-probe and CI read it). This emits a GENERATED, GITIGNORED vault view of it
under kb/Conformance/ — exactly the pattern kb/Reference/ already uses — so the 3,210-rule burn-down is queryable
with Dataview instead of only greppable.

Why a note per rule rather than one big table: Dataview queries FRONTMATTER, so per-rule frontmatter is what makes
"show every DIVERGES in §14 that has no test" a one-line query. Each note also carries a `description:` (the rule
text, truncated) so relevance is judgeable from the index without opening the note — the progressive-disclosure
hook, the same reason kb/Reference notes carry one.

    python scripts/spec/gen_conformance_notes.py

Regenerate after any inventory change. Never hand-edit the output — record verdicts in the inventory JSON.
"""
from __future__ import annotations

import json
import pathlib
import re
import shutil
import sys
from collections import Counter, defaultdict

REPO = pathlib.Path(__file__).resolve().parents[2]
INVENTORY = REPO / "tests" / "version-matrix" / "traceability-inventory.json"
OUT = REPO / "kb" / "Conformance"

KIND_LABEL = {"SR": "Syntax rule", "GR": "General rule", "AR": "Argument rule", "RV": "Returned value rule"}


def yaml_str(s: str, limit: int = 200) -> str:
    s = re.sub(r"\s+", " ", s).strip()
    if len(s) > limit:
        s = s[: limit - 3].rstrip() + "..."
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def slug(rule_id: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]", "-", rule_id)


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not INVENTORY.exists():
        sys.exit(f"inventory not found: {INVENTORY}\nRun: python scripts/spec/build_inventory.py")

    rows = json.loads(INVENTORY.read_text(encoding="utf-8"))
    catalog = json.loads((REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json").read_text(encoding="utf-8"))
    text_of = {r["id"]: r["text"] for r in catalog["rules"]}

    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)

    by_clause: dict[str, list[dict]] = defaultdict(list)
    for row in rows:
        by_clause[row["section"].split(".")[0]].append(row)

    for clause, items in by_clause.items():
        d = OUT / f"clause-{clause}"
        d.mkdir(parents=True, exist_ok=True)
        for row in items:
            rid = row["rule-id"]
            body = text_of.get(rid, "")
            note = f"""---
title: {rid}
description: {yaml_str(body)}
rule-id: {rid}
section: "{row['section']}"
clause: "{clause}"
kind: {row['kind']}
ordinal: {row['ordinal']}
subject: {yaml_str(row['subject'], 120)}
page: {row['page']}
state: {row['state']}
verdict: {row['verdict'] or '""'}
code-location: {yaml_str(row['code-location'], 160) if row['code-location'] else '""'}
test-ref: {yaml_str(row['test-ref'], 160) if row['test-ref'] else '""'}
editions: {row['editions'] or '""'}
generated: true
tags:
  - cobolsharp
  - conformance
  - generated
  - rule/{row['kind']}
  - clause/{clause}
---

# {rid}

> ⚙ **Generated** from `tests/version-matrix/traceability-inventory.json` — do not edit. Record verdicts in the
> inventory, then re-run `scripts/spec/gen_conformance_notes.py`.

**{KIND_LABEL.get(row['kind'], row['kind'])} {row['ordinal']}** of §{row['section']} — *{row['subject']}*
· printed page {row['page']} · **state: {row['state']}**

## Rule text

{body}

## Verdict

| field | value |
|---|---|
| verdict | {row['verdict'] or '— *not yet adjudicated*'} |
| code-location | {row['code-location'] or '— *not located*'} |
| test-ref | {row['test-ref'] or '— *test needed*'} |
| editions | {row['editions'] or '— *not determined*'} |
| notes | {row['notes'] or '—'} |

## See also
- [[kb/Conformance/clause-{clause}/_Index|§{clause} index]] · [[kb/Conformance/_Dashboard|conformance dashboard]]
"""
            (d / f"{slug(rid)}.md").write_text(note, encoding="utf-8")

        gaps = sum(1 for r in items if r["state"] == "GAP")
        idx = [f"""---
title: "§{clause} conformance index"
description: "{len(items)} normative rules in clause {clause}; {gaps} still GAP."
clause: "{clause}"
generated: true
tags: [cobolsharp, conformance, generated, moc]
---

# §{clause} — {len(items)} rules, {gaps} GAP

```dataview
TABLE kind, subject, state, verdict, test-ref
FROM #conformance AND "kb/Conformance/clause-{clause}"
WHERE file.name != "_Index"
SORT section ASC, ordinal ASC
```

## Rules
"""]
        for row in sorted(items, key=lambda r: (r["section"], r["ordinal"])):
            mark = " " if row["state"] == "GAP" else "x"
            idx.append(f"- [{mark}] [[kb/Conformance/clause-{clause}/{slug(row['rule-id'])}|{row['rule-id']}]] "
                       f"— {row['subject']}")
        (d / "_Index.md").write_text("\n".join(idx) + "\n", encoding="utf-8")

    total = len(rows)
    gaps = sum(1 for r in rows if r["state"] == "GAP")
    kinds = Counter(r["kind"] for r in rows)
    dash = f"""---
title: Conformance Dashboard
description: "P14 burn-down over all {total} normative ISO/IEC 1989:2023 rules; v1.0 = zero GAP."
generated: true
tags: [cobolsharp, conformance, generated, moc, dashboard]
---

# Conformance dashboard

> ⚙ **Generated** from the traceability inventory. `v1.0 = zero GAP` (owner decision D13).

**{total} normative rules · {gaps} GAP · {total - gaps} resolved**

Kinds: {" · ".join(f"{n} {k}" for k, n in sorted(kinds.items()))}

## Burn-down by clause

| clause | rules | GAP |
|---|---:|---:|
""" + "\n".join(
        f"| [[kb/Conformance/clause-{c}/_Index|§{c}]] | {len(i)} | {sum(1 for r in i if r['state'] == 'GAP')} |"
        for c, i in sorted(by_clause.items(), key=lambda kv: (0, int(kv[0]), "") if kv[0].isdigit() else (1, 0, kv[0]))
    ) + f"""

## Live queries

Everything still unadjudicated:
```dataview
TABLE section, kind, subject
FROM #conformance
WHERE state = "GAP" AND verdict = ""
SORT section ASC
LIMIT 100
```

Divergences with no covering test — the Phase-C worklist:
```dataview
TABLE section, subject, notes
FROM #conformance
WHERE verdict = "DIVERGES" AND test-ref = ""
SORT section ASC
```

Conforms-but-untested — coverage asserted rather than proven:
```dataview
TABLE section, subject, code-location
FROM #conformance
WHERE verdict = "CONFORMS" AND test-ref = ""
SORT section ASC
```

Awaiting an owner decision:
```dataview
TABLE section, subject, notes
FROM #conformance
WHERE verdict = "NEEDS-OWNER-DECISION"
```
"""
    (OUT / "_Dashboard.md").write_text(dash, encoding="utf-8")

    n = sum(1 for _ in OUT.rglob("*.md"))
    print(f"wrote {n} notes into {OUT.relative_to(REPO)}")
    print(f"  {total} rules · {gaps} GAP · {len(by_clause)} clause indexes + 1 dashboard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
