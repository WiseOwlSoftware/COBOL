"""For each residual OUTPUT_DIFFERS signature print the first differing line pair after the DISPLAY normalization."""
import json, sys, collections, re
sys.path.insert(0, sys.argv[0].rsplit("\\", 1)[0].rsplit("/", 1)[0])
from normalize import canon_gnu, canon_we

rows = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
ids = set(json.load(open(sys.argv[2])))
res = [r for r in rows if r["id"] in ids]
by = collections.defaultdict(list)
for r in res:
    by[r["signature"]].append(r)
limit = int(sys.argv[3]) if len(sys.argv) > 3 else 3
for s, rs in sorted(by.items(), key=lambda kv: -len(kv[1])):
    print(f"\n=== {s} x{len(rs)}")
    for r in rs[:limit]:
        we = canon_we(r["we"]["output"]).splitlines()
        th = canon_gnu(r["they"]["output"]).splitlines()
        rc = f"we_rc={r['we']['run_rc']} they_rc={r['they']['run_rc']} crashed={r['we']['crashed']}"
        for i in range(max(len(we), len(th))):
            a = we[i] if i < len(we) else "<EOF>"
            b = th[i] if i < len(th) else "<EOF>"
            if a != b:
                print(f"  {r['id']} line {i+1}: WE `{a[:90]}` | GNU `{b[:90]}`  ({rc})")
                break
        else:
            print(f"  {r['id']}: equal after normalization?? ({rc})")
