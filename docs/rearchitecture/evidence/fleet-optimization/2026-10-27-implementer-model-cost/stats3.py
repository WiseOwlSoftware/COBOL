import json, os, statistics
D = os.path.dirname(os.path.abspath(__file__))
R = json.load(open(os.path.join(D, "rows.json")))
for m in ("sonnet", "opus"):
    rs = [r for r in R if r["model"] == m and r["date"] >= "2026-10-17"]
    n = len(rs)
    clean = [r for r in rs if r["status"] == "DONE" and r["fix_rounds"] == 0]
    print(m, "n", n, "DONE&no-fix-round %.0f%%" % (100 * len(clean) / n),
          "impl pts sum %.1f fix %.1f review %.1f fin %.1f" % (sum(r["points"] for r in rs), sum(r["fix_pts"] for r in rs), sum(r["review_pts"] for r in rs), sum(r["finisher_pts"] for r in rs)),
          "mean review rounds %.2f" % statistics.mean(r["review_rounds"] for r in rs), "fix model mix", {k: sum(1 for r in rs if r["fix_models"] == k) for k in set(r["fix_models"] for r in rs)})
print("== known sonnet waves 1001/1002/1017 rows")
for w in ("1001", "1002", "1017"):
    rs = [r for r in R if r["wave"] == w]
    print(w, len(rs), set(r["model"] for r in rs), [(r["group"], r["status"], r["notes_reported"], r["points"]) for r in rs][:10])
print("sonnet rows pre-10-07 by wave", sorted(set(r["wave"] for r in R if r["model"] == "sonnet" and r["date"] < "2026-10-07")))
print("first sonnet date", min(r["date"] for r in R if r["model"] == "sonnet"), "first opus", min(r["date"] for r in R if r["model"] == "opus"))
print("rows total", len(R), "date range", min(r["date"] for r in R), max(r["date"] for r in R))
