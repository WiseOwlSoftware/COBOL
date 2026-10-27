import json, os, collections, statistics, sys
D = os.path.dirname(os.path.abspath(__file__))
R = json.load(open(os.path.join(D, "rows.json")))


def era(r):
    d = r["date"]
    return "pre-10-07" if d < "2026-10-07" else ("10-07..10-16" if d < "2026-10-17" else "10-17+")


def summ(rs, label):
    n = len(rs)
    if not n:
        print(label, "n=0")
        return
    pts = [r["points"] for r in rs]
    tot = sum(r["total_pts"] for r in rs)
    landed = [r for r in rs if r["landed_ancestry"] == "y"]
    ev = [r for r in rs if r["landed_ancestry"] == "y" or r["commit_evidence"] == "y"]
    notes_v = sum(r["notes_verified_landed"] for r in rs)
    nl_anc = sum(r["notes_verified_landed"] for r in landed)
    done = [r for r in rs if r["status"] == "DONE"]
    fin = [r for r in rs if r["finisher_passes"] > 0]
    fixed = [r for r in rs if r["fix_rounds"] > 0]
    retried = [r for r in rs if r["later_attempts"] > 0]
    chain_pts = sum(r["total_pts"] + r["retry_pts"] for r in rs if r["attempt_no"] == 1)
    print("%-28s n=%3d impl-pts mean %.3f med %.3f | total(incl rev/fix/fin) %.1f | DONE %.0f%% | fix-round %.0f%% | finisher %.0f%% | retried-later %.0f%%"
          % (label, n, statistics.mean(pts), statistics.median(pts), tot, 100 * len(done) / n, 100 * len(fixed) / n, 100 * len(fin) / n, 100 * len(retried) / n))
    print("   landed(ancestry) %d/%d=%.0f%%  landed(anc|commit-evidence) %d/%d=%.0f%% | verified-landed notes (all returned) %d, in ancestry-landed groups %d | pts/note(all) %.2f  pts/note(anc groups) %.2f"
          % (len(landed), n, 100 * len(landed) / n, len(ev), n, 100 * len(ev) / n, notes_v, nl_anc, tot / max(notes_v, 1), tot / max(nl_anc, 1)))
    # mean total points per group
    print("   mean total pts/group %.3f  median %.3f; tokens mean %.0f turns mean %.0f" % (statistics.mean(r["total_pts"] for r in rs), statistics.median(r["total_pts"] for r in rs),
          statistics.mean(r["tokens"] for r in rs), statistics.mean(r["turns"] for r in rs)))
    first = [r for r in rs if r["attempt_no"] == 1]
    cp = sum(r["total_pts"] + r["retry_pts"] for r in first)
    cn = sum(r["chain_notes"] for r in first)
    print("   chain-charged (attempt-1 groups only): n=%d pts incl all later attempts %.1f, notes verified landed in chain %d -> %.2f pts/note" % (len(first), cp, cn, cp / max(cn, 1)))


mode = sys.argv[1] if len(sys.argv) > 1 else "all"
if mode == "all":
    for m in ("sonnet", "opus"):
        summ([r for r in R if r["model"] == m], m)
    for e in ("pre-10-07", "10-07..10-16", "10-17+"):
        for m in ("sonnet", "opus"):
            summ([r for r in R if r["model"] == m and era(r) == e], "%s %s" % (e, m))
    print(collections.Counter((r["model"], r["status"]) for r in R))
    print("done+landed-status check", collections.Counter((r["model"], r["status"], r["landed_ancestry"]) for r in R if r["date"] >= "2026-10-17"))
if mode == "area":
    areas = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in R:
        if r["date"] >= "2026-10-07":
            areas[r["area"]][r["model"]].append(r)
    for a, d in sorted(areas.items(), key=lambda kv: -sum(len(v) for v in kv[1].values())):
        if "sonnet" in d and "opus" in d:
            print("AREA", a)
            for m in ("sonnet", "opus"):
                summ(d[m], "  " + m)
