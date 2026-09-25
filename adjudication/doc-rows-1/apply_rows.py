#!/usr/bin/env python3
"""Write merged.jsonl's WRITE / KEEP / NOT-PROVIDED rows into docs/CONFORMANCE.md §7 (PB1522 step 3).

A row for an item §7 already carries REPLACES that line in place; every other row is appended at the END of the
§7 table in item order. Each row must be one table line with exactly four cells. Idempotent."""
import json, pathlib, re, sys
ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "docs" / "CONFORMANCE.md"
merged = [json.loads(l) for l in (pathlib.Path(__file__).parent / "merged.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
lines = DOC.read_text(encoding="utf-8").split("\n")
start = next(i for i, l in enumerate(lines) if l.startswith("## 7. Annex A.1"))
end = next(i for i, l in enumerate(lines) if i > start and l.startswith("## 8."))
rowidx = {}
last = None
for i in range(start, end):
    m = re.match(r"^\| (DOC-A\.1-\d+) \|", lines[i])
    if m:
        rowidx.setdefault(m.group(1), []).append(i); last = i

def cells(row):
    s = row.strip()
    assert s.startswith("|") and s.endswith("|"), row[:80]
    # a backslash-escaped pipe is not a cell boundary
    return [c for c in re.split(r"(?<!\\)\|", s[1:-1])]

append = []
for m in sorted(merged, key=lambda m: m["item"]):
    if m["decision"] not in ("WRITE", "KEEP", "NOT-PROVIDED") or not m["row"]:
        continue
    row = m["row"].strip()
    assert "\n" not in row, m["item"]
    c = cells(row)
    assert len(c) == 4, (m["item"], len(c))
    assert c[0].strip() == m["rule-id"], (m["item"], c[0])
    if m["rule-id"] in rowidx:
        idx = rowidx[m["rule-id"]]
        assert len(idx) == 1, (m["rule-id"], idx)
        lines[idx[0]] = row
    else:
        append.append(row)
lines[last + 1:last + 1] = append
DOC.write_text("\n".join(lines), encoding="utf-8")
print(f"replaced {sum(1 for m in merged if m['rule-id'] in rowidx and m['row'] and m['decision'] in ('WRITE','KEEP','NOT-PROVIDED'))}, appended {len(append)}")
