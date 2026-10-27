import json, glob, os, collections

roots = [r"C:\Users\brent\.claude", r"C:\Users\brent\.claude-acct2"]
out = []
for root in roots:
    for d in glob.glob(os.path.join(root, "projects", "E--COBOL*", "*", "subagents", "workflows", "wf_*")):
        wf = os.path.basename(d)
        acct = os.path.basename(root)
        labels = {}
        results = {}
        phase = {}
        jp = os.path.join(d, "journal.jsonl")
        if not os.path.exists(jp):
            continue
        for line in open(jp, encoding="utf-8"):
            try:
                o = json.loads(line)
            except Exception:
                continue
            if o.get("type") == "started":
                labels[o["agentId"]] = o.get("label")
                phase[o["agentId"]] = o.get("phase")
            elif o.get("type") == "result":
                results[o["agentId"]] = o.get("result")
        for aid, label in labels.items():
            meta = {}
            try:
                meta = json.load(open(os.path.join(d, "agent-%s.meta.json" % aid), encoding="utf-8"))
            except Exception:
                pass
            f = os.path.join(d, "agent-%s.jsonl" % aid)
            cnt = collections.Counter()
            turns = 0
            seen = set()
            models = collections.Counter()
            first = None
            if os.path.exists(f):
                for line in open(f, encoding="utf-8"):
                    try:
                        o = json.loads(line)
                    except Exception:
                        continue
                    if first is None and o.get("timestamp"):
                        first = o["timestamp"]
                    if o.get("type") != "assistant":
                        continue
                    m = o.get("message") or {}
                    mid = m.get("id")
                    if mid and mid not in seen:
                        seen.add(mid)
                        turns += 1
                        u = m.get("usage") or {}
                        mod = m.get("model")
                        models[mod] += 1
                        cnt[mod] += u.get("input_tokens", 0) + u.get("output_tokens", 0) + u.get("cache_creation_input_tokens", 0)
            out.append(dict(acct=acct, wf=wf, aid=aid, label=label, phase=phase.get(aid), atype=meta.get("agentType"),
                            metamodel=meta.get("model"), turns=turns, models=dict(models), counted=dict(cnt),
                            first=first, returned=aid in results, result=results.get(aid)))
json.dump(out, open(os.path.join(os.path.dirname(__file__), "all_agents.json"), "w"), default=str)
print(len(out))
print(collections.Counter((o["atype"], o["metamodel"], tuple(o["models"])) for o in out).most_common(40))
