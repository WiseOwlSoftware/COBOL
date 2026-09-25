#!/usr/bin/env python3
"""Summarize the Claude Code telemetry otlp_sink.py recorded: tokens and cost per agent / skill / model.

    python scripts/telemetry/usage_report.py                  # today (UTC)
    python scripts/telemetry/usage_report.py 2026-09-25 ...   # named days
    python scripts/telemetry/usage_report.py --all

Reads the `api_request` log events (one per model call: model, input/output/cache tokens, cost) and groups them by the
identifying attributes Claude Code attaches (agent name, else query source; skill when present). Attribute names are
read defensively — an event that lacks one is grouped under "-", never dropped, and the event total is printed so a
parse that silently matched nothing is visible.
"""
import collections
import datetime
import json
import pathlib
import sys

DIR = pathlib.Path.home() / ".claude" / "telemetry"
TOKENS = ("input_tokens", "output_tokens", "cache_read_tokens", "cache_creation_tokens")


def attrs(lst):
    out = {}
    for a in lst or []:
        v = a.get("value") or {}
        for k in ("stringValue", "intValue", "doubleValue", "boolValue"):
            if k in v:
                out[a.get("key")] = v[k]
                break
    return out


def num(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return 0.0


def events(files):
    for f in files:
        for line in f.read_text(encoding="utf-8").splitlines():
            try:
                rec = json.loads(line)
            except ValueError:
                continue
            for rl in (rec.get("body") or {}).get("resourceLogs", []):
                for sl in rl.get("scopeLogs", []):
                    for lr in sl.get("logRecords", []):
                        a = attrs(lr.get("attributes"))
                        name = str(a.get("event.name") or (lr.get("body") or {}).get("stringValue") or "")
                        if name.endswith("api_request"):
                            yield a


def main(argv):
    if "--all" in argv:
        files = sorted(DIR.glob("*.jsonl"))
    else:
        days = [d for d in argv if not d.startswith("-")] or [f"{datetime.datetime.now(datetime.timezone.utc):%Y-%m-%d}"]
        files = [DIR / f"{d}.jsonl" for d in days if (DIR / f"{d}.jsonl").exists()]
    if not files:
        print(f"no telemetry under {DIR} for {argv or 'today'} — is otlp_sink.py running and telemetry enabled?")
        return 1
    groups = collections.defaultdict(lambda: collections.Counter())
    n = 0
    for a in events(files):
        n += 1
        who = a.get("agent.name") or a.get("agent_type") or a.get("query_source") or "-"
        key = (str(who), str(a.get("skill.name") or "-"), str(a.get("model") or "-"))
        c = groups[key]
        c["calls"] += 1
        for t in TOKENS:
            c[t] += num(a.get(t))
        c["cost_usd"] += num(a.get("cost_usd"))
    print(f"{n} api_request events in {len(files)} file(s)")
    rows = sorted(groups.items(), key=lambda kv: -(kv[1]["cache_read_tokens"] + kv[1]["input_tokens"]))
    print(f"{'agent/source':32} {'skill':20} {'model':24} {'calls':>6} {'in':>10} {'out':>9} {'cache-rd':>12} "
          f"{'cache-wr':>11} {'usd':>9}")
    for (who, skill, model), c in rows:
        print(f"{who[:32]:32} {skill[:20]:20} {model[:24]:24} {int(c['calls']):6} {int(c['input_tokens']):10} "
              f"{int(c['output_tokens']):9} {int(c['cache_read_tokens']):12} {int(c['cache_creation_tokens']):11} "
              f"{c['cost_usd']:9.2f}")
    return 0 if n else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
