import json, collections, re, os, glob, csv, statistics

D = os.path.dirname(os.path.abspath(__file__))
A = json.load(open(os.path.join(D, "all_agents.json")))
RATE = {"opus": 1.6e6, "sonnet": 4.0e6}


def fam(m):
    m = m or ""
    if "opus" in m:
        return "opus"
    if "sonnet" in m:
        return "sonnet"
    if "haiku" in m:
        return "haiku"
    return "opus"  # unknown_model_rate


def pts(o):
    p = 0.0
    for m, n in o["counted"].items():
        f = fam(m)
        r = RATE.get(f, 12e6)
        p += n / r
    return p


def tokens(o):
    return sum(o["counted"].values())


def model_of(o):
    ms = o["models"]
    if not ms:
        return o.get("metamodel") or "?"
    return fam(max(ms, key=ms.get))


# ---- kb/Work notes
notes = {}
for f in glob.glob(r"E:\COBOL\kb\Work\PB*.md"):
    m = re.match(r"(PB\d+)", os.path.basename(f))
    if not m:
        continue
    st = ar = sv = None
    for i, line in enumerate(open(f, encoding="utf-8", errors="replace")):
        if i > 30:
            break
        if line.startswith("status:"):
            st = line.split(":", 1)[1].strip()
        elif line.startswith("area:"):
            ar = line.split(":", 1)[1].strip()
        elif line.startswith("severity:"):
            sv = line.split(":", 1)[1].strip()
    notes[m.group(1)] = dict(status=st, area=ar, sev=sv)

# ---- git
main_full = [l.strip() for l in open(os.path.join(D, "mainshas.txt"))]
main_pref = set(s[:9] for s in main_full)
commits = []
for l in open(os.path.join(D, "gitlog.txt"), encoding="utf-8"):
    p = l.rstrip("\n").split("|", 3)
    if len(p) == 4:
        commits.append(p)
pb_commits = collections.defaultdict(list)  # PB -> list of date
for h, d, par, s in commits:
    if s.startswith("Merge"):
        continue
    head = s.split(":", 1)[0]
    for pb in re.findall(r"PB\d+", head):
        pb_commits[pb].append(d)


def in_main(sha):
    return bool(sha) and sha[:9] in main_pref


# ---- group assembly
GID = re.compile(r"^(w\d+[a-z]\d*)-PB(\d+)")


def gid_of(o):
    r = o["result"] if isinstance(o["result"], dict) else {}
    rep = os.path.basename(r.get("report") or "")
    m = GID.match(rep)
    if m:
        return m.group(1), "PB" + m.group(2)
    return None, None


groups = {}


def G(gid):
    if gid not in groups:
        groups[gid] = dict(gid=gid, impl=[], fix=[], rev=[], fin=[], lead=None, branches=set(), heads=set(), notes=set(), wf=set())
    return groups[gid]


unassigned = []
for o in A:
    if o["atype"] == "cobol-implementer":
        gid, lead = gid_of(o)
        lab = o["label"] or ""
        if gid is None:
            # fallback: wf + letter
            m = re.match(r"(?:impl|fix)-([A-Z]\d*)", lab)
            gid = "%s:%s" % (o["wf"], m.group(1)) if m else None
            if lead is None:
                m2 = re.search(r"PB(\d+)", lab)
                lead = "PB" + m2.group(1) if m2 else None
        if gid is None:
            unassigned.append(o)
            continue
        g = G(gid)
        g["wf"].add(o["wf"])
        if lead and not g["lead"]:
            g["lead"] = lead
        kind = "impl" if lab.startswith("impl") or lab.startswith("architect") else "fix"
        g[kind].append(o)
        r = o["result"] if isinstance(o["result"], dict) else {}
        if r.get("branch"):
            g["branches"].add(r["branch"])
        if r.get("head"):
            g["heads"].add(r["head"])
        for pb in re.findall(r"PB\d+", str(r.get("notes_landed") or "")):
            g["notes"].add(pb)
    elif o["atype"] == "cobol-reviewer":
        m = re.match(r"(w\d+[a-z]\d*)-review-r(\d+)", o["label"] or "")
        if m:
            G(m.group(1))["rev"].append(o)
    elif o["atype"] == "cobol-finisher":
        m = re.match(r"finish:(w\d+[a-z]\d*)", o["label"] or "")
        if m:
            g = G(m.group(1))
            g["fin"].append(o)
            txt = str(o["result"])
            for b in re.findall(r"worktree-wf_[0-9a-f]+-[0-9a-f]+-\d+", txt):
                g["branches"].add(b)
            for h in re.findall(r"\b[0-9a-f]{9,40}\b", txt):
                g["heads"].add(h)

# lander-train labelled finisher in old era etc. are not tracked individually.
rows = []
for gid, g in groups.items():
    if not g["impl"]:
        continue
    g["impl"].sort(key=lambda o: o["first"] or "")
    i0 = g["impl"][0]
    r0 = i0["result"] if isinstance(i0["result"], dict) else {}
    lead = g["lead"]
    st = r0.get("status") or ("NORESULT" if not i0["returned"] else "?")
    pi = pts(i0)
    pf = sum(pts(o) for o in g["fix"])
    pr = sum(pts(o) for o in g["rev"])
    pfin = sum(pts(o) for o in g["fin"])
    fixmodels = collections.Counter(model_of(o) for o in g["fix"])
    ancestors = any(in_main(h) for h in g["heads"])
    # note verification
    vnotes = [pb for pb in g["notes"] if notes.get(pb, {}).get("status") in ("landed", "retired")]
    start = i0["first"] or ""
    ev = False
    for pb in ([lead] if lead else []) + sorted(g["notes"]):
        if any(d >= start for d in pb_commits.get(pb, [])):
            ev = True
    nt = notes.get(lead, {})
    rows.append(dict(
        workflow=sorted(g["wf"])[0], wave=(re.match(r"w(\d+)", gid) or [None, "?"])[1] if not gid.startswith("wf_") else "?", group=gid, lead=lead,
        area=nt.get("area"), sev=nt.get("sev"), branch=r0.get("branch"), model=model_of(i0), turns=i0["turns"], tokens=tokens(i0),
        points=round(pi, 3), status=st, fix_rounds=len(g["fix"]), fix_pts=round(pf, 3), fix_models="/".join(sorted(fixmodels)),
        review_rounds=len(g["rev"]), review_pts=round(pr, 3), finisher_passes=len(g["fin"]), finisher_pts=round(pfin, 3),
        total_pts=round(pi + pf + pr + pfin, 3), landed_ancestry="y" if ancestors else "n", commit_evidence="y" if ev else "n",
        notes_reported=len(g["notes"]), notes_verified_landed=len(vnotes), notes_list=" ".join(sorted(g["notes"])),
        date=start[:10], start=start))

# chain by lead: attempts sorted by start
chains = collections.defaultdict(list)
for r in rows:
    chains[r["lead"]].append(r)
for lead, lst in chains.items():
    lst.sort(key=lambda r: r["start"])
    for i, r in enumerate(lst):
        r["attempt_no"] = i + 1
        r["later_attempts"] = len(lst) - 1 - i
        r["retry_pts"] = round(sum(x["total_pts"] for x in lst[i + 1:]), 3)
        # any later attempt (same lead) landed or any ancestry in chain
        r["chain_landed_anc"] = "y" if any(x["landed_ancestry"] == "y" for x in lst[i:]) else "n"
        r["chain_notes"] = len(set(n for x in lst[i:] for n in x["notes_list"].split() if notes.get(n, {}).get("status") in ("landed", "retired")))

cols = ["workflow", "wave", "group", "lead", "attempt_no", "later_attempts", "area", "sev", "branch", "model", "turns", "tokens", "points", "status",
        "review_rounds", "review_pts", "fix_rounds", "fix_pts", "fix_models", "finisher_passes", "finisher_pts", "total_pts", "retry_pts",
        "landed_ancestry", "commit_evidence", "chain_landed_anc", "notes_reported", "notes_verified_landed", "chain_notes", "notes_list", "date"]
rows.sort(key=lambda r: r["start"])
with open(os.path.join(D, "results.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.DictWriter(f, fieldnames=cols, extrasaction="ignore")
    w.writeheader()
    w.writerows(rows)
print("groups", len(rows), "unassigned impl agents", len(unassigned))
json.dump(rows, open(os.path.join(D, "rows.json"), "w"))
