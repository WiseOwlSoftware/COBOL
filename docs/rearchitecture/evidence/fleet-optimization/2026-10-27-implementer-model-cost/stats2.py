import json, os, collections, statistics
D = os.path.dirname(os.path.abspath(__file__))
R = json.load(open(os.path.join(D, "rows.json")))
byk = collections.defaultdict(list)
for r in R:
    byk[r["lead"]].append(r)
for k in byk:
    byk[k].sort(key=lambda r: r["start"])

print("== bundle size (notes_reported) by model / era")
for e, lo, hi in (("10-07..16", "2026-10-07", "2026-10-17"), ("10-17+", "2026-10-17", "2027")):
    for m in ("sonnet", "opus"):
        rs = [r for r in R if r["model"] == m and lo <= r["date"] < hi]
        if rs:
            print(e, m, len(rs), "mean notes reported %.2f" % statistics.mean(r["notes_reported"] for r in rs), "median", statistics.median(r["notes_reported"] for r in rs))

print("== sev mix 10-17+")
for m in ("sonnet", "opus"):
    print(m, collections.Counter(r["sev"] for r in R if r["model"] == m and r["date"] >= "2026-10-17"))

print("== retries of attempt-1 groups, by first model, per era (10-07+)")
for m in ("sonnet", "opus"):
    tot = 0
    n = 0
    promo = collections.Counter()
    rpts = 0.0
    land = 0
    for r in R:
        if r["model"] != m or r["date"] < "2026-10-07" or r["attempt_no"] != 1:
            continue
        n += 1
        if r["later_attempts"] > 0:
            tot += 1
            rpts += r["retry_pts"]
            for x in byk[r["lead"]][1:]:
                promo[x["model"]] += 1
            if r["landed_ancestry"] == "n" or r["status"] != "DONE":
                pass
    print(m, "first attempts", n, "re-dispatched", tot, "retry pts charged %.2f" % rpts, "later-attempt models", dict(promo))

print("== sonnet first attempts that did NOT land by themselves (10-17+), later model")
c = collections.Counter()
for r in R:
    if r["model"] == "sonnet" and r["date"] >= "2026-10-17" and r["attempt_no"] == 1:
        later = byk[r["lead"]][1:]
        c[(r["status"], r["landed_ancestry"], tuple(x["model"] for x in later))] += 1
print(c)

print("== within-area, 10-17+ and 10-07+ (both models present, n>=3 each)")
for lo in ("2026-10-07", "2026-10-17"):
    print("-- from", lo)
    areas = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in R:
        if r["date"] >= lo:
            areas[r["area"]][r["model"]].append(r)
    for a, d in sorted(areas.items(), key=lambda kv: -sum(len(v) for v in kv[1].values())):
        if len(d.get("sonnet", [])) >= 3 and len(d.get("opus", [])) >= 3:
            line = a
            for m in ("sonnet", "opus"):
                rs = d[m]
                nn = sum(r["notes_verified_landed"] for r in rs)
                line += " | %s n=%d done=%.0f%% land=%.0f%% mean-total=%.2f pts/note=%.2f" % (
                    m, len(rs), 100 * sum(r["status"] == "DONE" for r in rs) / len(rs), 100 * sum(r["landed_ancestry"] == "y" for r in rs) / len(rs),
                    statistics.mean(r["total_pts"] for r in rs), sum(r["total_pts"] for r in rs) / max(nn, 1))
            print(line)
print("== areas of opus vs sonnet 10-17+ (top)")
for m in ("sonnet", "opus"):
    print(m, collections.Counter(r["area"] for r in R if r["model"] == m and r["date"] >= "2026-10-17").most_common(8))
