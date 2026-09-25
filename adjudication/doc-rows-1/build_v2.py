#!/usr/bin/env python3
"""Build batch-doc-v2.json (PB1522 step 3): the re-verdicted records of the WRITE / KEEP / NOT-PROVIDED items.

WRITE / KEEP → the merged record (writer's, refuter-corrected, integrator-checked). NOT-PROVIDED (all five are
A.1-OPTIONAL items) → the record `derive_verdict_batch.py a1-optional-not-provided` emits for the row, i.e.
DOCUMENTED-NON-SUPPORT per owner decision kb/Work PB280 Q1, enriched with the writer's greenfield site(s) and
spec-derived test-ref (each also named in the row's Pinned by). OMIT / SKIP items stay verdict-less.
"""
import json, pathlib, subprocess, sys, tempfile
HERE = pathlib.Path(__file__).parent
ROOT = HERE.parents[1]
KEYS = ("rule-id", "verdict", "code-location", "test-ref", "editions", "notes")
merged = [json.loads(l) for l in (HERE / "merged.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
with tempfile.TemporaryDirectory() as t:
    out = pathlib.Path(t) / "np.json"
    subprocess.run([sys.executable, "scripts/spec/derive_verdict_batch.py", "a1-optional-not-provided", "-o", str(out)],
                   cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
    derived = {r["rule-id"]: r for r in json.loads(out.read_text(encoding="utf-8"))["records"]}
recs = []
for m in sorted(merged, key=lambda m: m["item"]):
    if m["decision"] not in ("WRITE", "KEEP", "NOT-PROVIDED"):
        continue
    w = m["record"]
    if m["decision"] == "NOT-PROVIDED":
        d = derived[m["rule-id"]]  # KeyError = the selector does not take this row: fail loudly
        locs = [x.strip() for x in d["code-location"].split(";") if x.strip()]
        locs += [x.strip() for x in w["code-location"].split(";") if x.strip() and x.strip() not in locs]
        r = {"rule-id": m["rule-id"], "verdict": d["verdict"], "code-location": "; ".join(locs),
             "test-ref": w.get("test-ref", ""), "editions": w.get("editions", ""),
             "notes": "Derived: a1-optional-not-provided (kb/Work PB280 Q1 — an A.1-OPTIONAL item whose §7 row opens "
                      "'Not provided.' is DOCUMENTED-NON-SUPPORT). Determination evidence (PB1522 step 3): " + w["notes"]}
    else:
        r = {k: w.get(k, "") for k in KEYS}
    assert r["rule-id"] == m["rule-id"]
    recs.append(r)
(HERE / "batch-doc-v2.json").write_text(json.dumps({"batch": "doc-rows-1-batch-doc-v2", "records": recs}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
from collections import Counter
print(len(recs), Counter(r["verdict"] for r in recs))
