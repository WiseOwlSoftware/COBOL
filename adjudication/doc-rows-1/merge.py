#!/usr/bin/env python3
"""Merge the six §7-determination batches (writer rows.jsonl + independent refuter refute.jsonl) into merged.jsonl.

Rule (integrator brief, kb/Work PB1522 step 3): per item take the writer line and apply the refuter —
upheld → keep the writer's row/record (a refuter corrected_record still replaces the record: the refuter
re-verified an edge and updated the notes/editions); not upheld → corrected_decision / corrected_row /
corrected_record replace the writer's; a refuter OMIT drops the row. Facts on which the two disagreed were
re-checked by the integrator (see `integrator_check` in the output) — never picked by vote.
"""
import json, pathlib, sys
HERE = pathlib.Path(__file__).parent
CHECKS = json.loads((HERE / "integrator-checks.json").read_text(encoding="utf-8"))
out = []
for b in range(1, 7):
    d = HERE / f"doc-w1-s{b}"
    rows = {}
    for l in (d / "rows.jsonl").read_text(encoding="utf-8").splitlines():
        if l.strip():
            r = json.loads(l); rows[r["item"]] = r
    ref = {}
    for l in (d / "refute.jsonl").read_text(encoding="utf-8").splitlines():
        if l.strip():
            r = json.loads(l); ref[r["item"]] = r
    for it in sorted(rows):
        w, f = rows[it], ref.get(it)
        m = {"item": it, "rule-id": w["rule-id"], "batch": f"doc-w1-s{b}", "voluntary": w.get("voluntary", False),
             "writer_decision": w["decision"], "decision": w["decision"], "row": w.get("row") or "",
             "record": w.get("record"), "owning_note": w.get("owning_note"), "defect": w.get("defect"),
             "refuter": None}
        if f:
            m["refuter"] = {"upheld": f["upheld"], "reason": f.get("reason", "")}
            if f.get("corrected_record"):
                m["record"] = f["corrected_record"]
            if not f["upheld"]:
                m["decision"] = f.get("corrected_decision") or w["decision"]
                if f.get("corrected_row"):
                    m["row"] = f["corrected_row"]
        if m["decision"] in ("OMIT", "SKIP"):
            m["row"] = ""
        c = CHECKS.get(str(it))
        if c:
            m["integrator_check"] = c["check"]
            if c.get("decision"):
                m["decision"] = c["decision"]
            for old, new in c.get("row_replace", []):
                assert old in m["row"], (it, old)
                m["row"] = m["row"].replace(old, new)
            for k, v in c.get("record", {}).items():
                m["record"][k] = v
            if c.get("notes_append"):
                m["record"]["notes"] = m["record"]["notes"].rstrip() + " " + c["notes_append"]
        out.append(m)
(HERE / "merged.jsonl").write_text("".join(json.dumps(m, ensure_ascii=False) + "\n" for m in out), encoding="utf-8")
from collections import Counter
print(len(out), Counter(m["decision"] for m in out))
