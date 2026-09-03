"""Lane 3 merge — turn one batch's adjudications + refutations on disk into a record_verdicts batch and a findings digest.

    python merge_batch.py <out-dir> <batch-label>

Reads out-<slug>.json (adjudicator results; falls back to adjudicate-<slug>.jsonl checkpoints), refute-<slug>.jsonl
(refuter checkpoints: one verdict per line). A CONFORMS record whose file has NO refuter checkpoint is DOWNGRADED to
'unrefuted' and EXCLUDED from the batch — the doctrine is that only a refuted closing verdict is recorded. Refuted rows
take the refuter's corrected verdict with the reason appended to notes. Defective verdicts (PARTIAL / NOT-IMPLEMENTED /
DIVERGES) need a register note in `notes` before record_verdicts accepts them — the digest lists the mechanisms so the
note-writer can allocate ids and the notes field is filled in a second pass (this script leaves a `@@NOTE@@` marker).
"""
import json, pathlib, sys, collections, re
sys.stdout.reconfigure(encoding="utf-8")
out = pathlib.Path(sys.argv[1]); label = sys.argv[2]

records, digest, unrefuted, skipped = [], collections.OrderedDict(), [], []
for f in sorted(out.glob("out-*.json")) + sorted(out.glob("adjudicate-*.jsonl")):
    slug = f.stem.split("-", 1)[1]
    if any(r["_slug"] == slug for r in records):
        continue  # out-file already consumed; the .jsonl is its checkpoint twin
    if f.suffix == ".json":
        data = json.loads(f.read_text(encoding="utf-8"))
        recs, findings = data.get("records", []), data.get("findings", [])
    else:
        recs = [json.loads(l) for l in f.read_text(encoding="utf-8").splitlines() if l.strip()]
        ff = out / f"findings-{slug}.json"
        findings = json.loads(ff.read_text(encoding="utf-8")) if ff.exists() else []
    ref = out / f"refute-{slug}.jsonl"
    refuted = {}
    if ref.exists():
        for l in ref.read_text(encoding="utf-8").splitlines():
            if l.strip():
                v = json.loads(l); refuted[v["rule-id"]] = v
    for r in recs:
        r = dict(r); r["_slug"] = slug
        rid = r["rule-id"]
        if r["verdict"] == "CONFORMS":
            if rid in refuted:
                v = refuted[rid]
                if not v["upheld"]:
                    # A refuter's corrected_verdict is free text around a vocabulary word ("CONFORMS, but with the
                    # test-ref WITHDRAWN …", "CONFORMS (test-ref amended: add conformance:2002/x …)"). Take the word;
                    # apply the two instruction shapes the refuters actually write; anything else is PARTIAL.
                    cv = v.get("corrected_verdict") or ""
                    word = next((w for w in ("CONFORMS", "PARTIAL", "DIVERGES", "NOT-IMPLEMENTED", "NEEDS-OWNER-DECISION") if cv.upper().startswith(w)), "PARTIAL")
                    r["verdict"] = word
                    if "WITHDRAWN" in cv.upper():
                        r["test-ref"] = ""
                    m = re.search(r"amended:\s*add\s+([A-Za-z0-9_:./-]+)", cv)
                    if m:
                        r["test-ref"] = "; ".join(x for x in [r.get("test-ref", ""), m.group(1)] if x)
                    r["notes"] = (r.get("notes", "") + " || REFUTED: " + cv + " — " + v.get("why", "")).strip()
            else:
                unrefuted.append(rid); continue
        if r["verdict"] == "NEEDS-OWNER-DECISION":
            skipped.append((rid, "owner")); continue
        if r["verdict"] in ("PARTIAL", "NOT-IMPLEMENTED", "DIVERGES") and "@@NOTE@@" not in r.get("notes", ""):
            r["notes"] = "@@NOTE@@ " + r.get("notes", "")
        records.append(r)
    for fd in findings:
        digest[(slug, fd["mechanism"])] = fd

batch = {"batch": label, "records": [{k: v for k, v in r.items() if not k.startswith("_")} for r in records]}
(out / f"batch-{label}.json").write_text(json.dumps(batch, indent=1, ensure_ascii=False), encoding="utf-8")
(out / f"findings-{label}.json").write_text(json.dumps([{"slug": s, **fd} for (s, _), fd in digest.items()], indent=1, ensure_ascii=False), encoding="utf-8")
c = collections.Counter(r["verdict"] for r in records)
print(f"{label}: {len(records)} records -> batch-{label}.json  {dict(c)}")
print(f"  CONFORMS with test-ref (would close): {sum(1 for r in records if r['verdict']=='CONFORMS' and r.get('test-ref'))}")
print(f"  unrefuted CONFORMS excluded: {len(unrefuted)}  owner-decision excluded: {len(skipped)}")
print(f"  findings (mechanisms): {len(digest)} -> findings-{label}.json")
print(f"  defective records awaiting a note id: {sum(1 for r in records if '@@NOTE@@' in r.get('notes',''))}")
