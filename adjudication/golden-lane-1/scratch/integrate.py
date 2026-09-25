"""Integrate refuter-upheld, passing goldens from one wave into the tree.

usage: python3 integrate.py <wave-label> <slug> [<slug> ...]
Reads /tmp/gl/reports/<slug>.json + <slug>.refute.json. A row lands only when: disposition new-golden/existing-golden,
ran == pass (existing-golden: n/a), refuter upheld, every file exists in out/, no PROGRAM-ID collision. Writes
/tmp/gl/batches/<wave>.json (witness-only records) and /tmp/gl/batches/<wave>.summary.json.
"""
import json, pathlib, re, shutil, sys

def add_enabled(mp, names):
    """Append names to the "enabled" array TEXTUALLY (the manifests have mixed indentation; never re-dump)."""
    t = mp.read_text(encoding="utf-8")
    d = json.loads(t)
    names = [n for n in names if n not in d["enabled"] and n not in d.get("pending", [])]
    if not names: return
    i = t.index('"enabled": [')
    j = t.index("]", i)
    body = t[i:j].rstrip()
    last = body.splitlines()[-1]
    ind = last[:len(last) - len(last.lstrip())]
    ins = "".join(f',\n{ind}"{n}"' for n in names)
    t2 = t[:i] + body + ins + t[i + len(t[i:j].rstrip()):]
    d2 = json.loads(t2)
    assert d2["enabled"] == d["enabled"] + names
    mp.write_text(t2, encoding="utf-8")


import os
REPO = pathlib.Path(os.environ.get("GL_REPO", "/home/user/CobolSharp"))
GL = pathlib.Path(os.environ.get("GL_DIR", "/tmp/gl"))
wave, slugs = sys.argv[1], sys.argv[2:]

existing_ids = {}
for p in (REPO / "tests/conformance").rglob("*.cob"):
    for m in re.finditer(r"PROGRAM-ID\.\s*([A-Za-z0-9-]+)", p.read_text(encoding="utf-8", errors="replace"), re.I):
        existing_ids.setdefault(m.group(1).upper(), p)

records, summary = [], {"wave": wave, "slugs": {}}
man_add = {}
neg_add = []
for slug in slugs:
    rep = json.loads((GL / f"reports/{slug}.json").read_text())
    ref_p = GL / f"reports/{slug}.refute.json"
    ref = json.loads(ref_p.read_text()) if ref_p.exists() else {"verdicts": []}
    upheld, bad_refs = {}, set()
    for v in ref["verdicts"]:
        if not v.get("upheld"):
            bad_refs.update(x.strip() for x in (v.get("test_ref") or "").split(";") if x.strip())
        if v.get("upheld") or v["rule_id"] not in upheld:
            upheld[v["rule_id"]] = v   # any upheld verdict keeps the row; overturned refs are dropped below
    s = {"landed": [], "overturned": [], "defect": [], "not-closable": [], "other": [], "deferred": rep.get("deferred", [])}
    for row in rep["rows"]:
        rid, disp = row["rule_id"], row["disposition"]
        if disp == "suspected-defect":
            s["defect"].append(rid); continue
        if disp == "not-closable":
            s["not-closable"].append({"rule_id": rid, "why": row.get("notes", "")[:300]}); continue
        if disp not in ("new-golden", "existing-golden"):
            s["other"].append({"rule_id": rid, "disposition": disp}); continue
        v = upheld.get(rid)
        if not v or not v.get("upheld"):
            s["overturned"].append({"rule_id": rid, "kind": (v or {}).get("kind", "NO-REFUTER-VERDICT"),
                                    "correction": (v or {}).get("correction", "")[:400]}); continue
        refs = [x.strip() for x in row["test_ref"].split(";") if x.strip()]
        if any(r in bad_refs for r in refs):
            keep = [r for r in refs if r not in bad_refs]
            if not keep:
                s["overturned"].append({"rule_id": rid, "kind": "split", "correction": "all refs overturned"}); continue
            bad_cases = {r.split("/")[-1] for r in refs if r in bad_refs}
            row = dict(row, test_ref="; ".join(keep),
                       files=[f for f in row.get("files", []) if pathlib.Path(f).stem not in bad_cases])
            s.setdefault("split", []).append({"rule_id": rid, "dropped": sorted(set(refs) - set(keep))})
        if disp == "new-golden" and row.get("ran") != "pass":
            s["other"].append({"rule_id": rid, "disposition": "new-golden-not-pass:" + str(row.get("ran"))}); continue
        ok = True
        for f in row.get("files", []):
            src = GL / "out" / slug / f
            if disp == "existing-golden":
                if not (REPO / f).exists(): ok = False
                continue
            if not src.exists():
                ok = False; print(f"MISSING {src}"); continue
            if f.endswith(".cob"):
                for m in re.finditer(r"PROGRAM-ID\.\s*([A-Za-z0-9-]+)", src.read_text(encoding="utf-8"), re.I):
                    pid = m.group(1).upper()
                    if pid in existing_ids and existing_ids[pid] != REPO / f:
                        ok = False; print(f"COLLISION {pid} {src} vs {existing_ids[pid]}")
        if not ok:
            s["other"].append({"rule_id": rid, "disposition": "file-problem"}); continue
        for f in row.get("files", []):
            if disp == "new-golden":
                dst = REPO / f
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(GL / "out" / slug / f, dst)
                if f.endswith(".cob"):
                    for m in re.finditer(r"PROGRAM-ID\.\s*([A-Za-z0-9-]+)", dst.read_text(encoding="utf-8"), re.I):
                        existing_ids[m.group(1).upper()] = dst
                    name, ed = pathlib.Path(f).stem, pathlib.Path(f).parent.name
                    if ed == "negative":
                        if name not in neg_add: neg_add.append(name)
                    else:
                        man_add.setdefault(ed, [])
                        if name not in man_add[ed]: man_add[ed].append(name)
        records.append({"rule-id": rid, "test-ref": row["test_ref"]})
        s["landed"].append({"rule_id": rid, "test_ref": row["test_ref"], "disposition": disp})
    summary["slugs"][slug] = s

for ed, names in man_add.items():
    mp = REPO / f"tests/conformance/{ed}/manifest.json"
    add_enabled(mp, names)
if neg_add:
    mp = REPO / "tests/conformance/negative/manifest.json"
    add_enabled(mp, neg_add)

(GL / "batches").mkdir(exist_ok=True)
(GL / f"batches/{wave}.json").write_text(json.dumps({"batch": f"golden-lane-1-{wave}", "records": records}, indent=1))
summary["manifest_added"] = man_add; summary["negative_added"] = neg_add
(GL / f"batches/{wave}.summary.json").write_text(json.dumps(summary, indent=1))
for slug, s in summary["slugs"].items():
    print(f"{slug}: landed {len(s['landed'])} overturned {len(s['overturned'])} defect {len(s['defect'])} "
          f"not-closable {len(s['not-closable'])} other {len(s['other'])} deferred {len(s['deferred'])}")
print("records", len(records), "manifest", {k: len(v) for k, v in man_add.items()}, "neg", len(neg_add))
