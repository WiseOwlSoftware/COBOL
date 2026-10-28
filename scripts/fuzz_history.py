#!/usr/bin/env python3
"""fuzz_history.py — THE CUMULATIVE RECORD OF THE DIFFERENTIAL-FUZZ NIGHTS AND THE PAGE RENDERED FROM IT (kb/Work PB4006).

    python scripts/fuzz_history.py render [--out <file.html>]     # the summary page, from the history and the register
    python scripts/fuzz_history.py show                           # the history, one line per night
    python scripts/test_fuzz_history.py                           # the self-test (discovered by scripts/self_tests.py)

A night enters the history through `python scripts/fuzz_triage.py history --triage <dir> --write`, the last step of
every morning's triage (docs/rearchitecture/DESIGN-differential-fuzz.md §6): it reads the run directory, the triage's
verdict files and the register, and writes one record into HISTORY, a tracked file, because the run directories are
git-ignored and the triage directories are coordination scratch. `render` then draws the page the morning session
publishes to the one Artifact whose URL HISTORY carries (`artifact`). The page is machine-generated and says so: no
number on it is typed by hand, and every status on it is read from kb/Work at render time.

What a record holds and where each field comes from:
  run       run.json (model, seed, start, edition) and report.md's headline (programs, usable, survivors)
  classes   results.jsonl, one count per harness class
  verdicts  the adjudicators' verdict files: signatures and programs per verdict (absent for a night triaged by hand)
  defects   the kb/Work notes the night filed against WiseOwl COBOL (statuses are looked up live, never stored)
"""
from __future__ import annotations

import argparse
import collections
import datetime
import html
import json
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent
HISTORY = REPO / "conformance-suite" / "fuzz-history.json"
RUN_ID = re.compile(r"fuzz-\d{8}-\d{6}")

#: Display order and colour token of every verdict an adjudicator may write (design §4.1); the test holds this list equal
#: to fuzz_triage's vocabulary, so a new verdict cannot reach the page without a colour.
VERDICTS = (("WISEOWL-DEFECT", "bad", "WiseOwl was wrong"), ("WISEOWL-CORRECT", "ok", "WiseOwl was right"),
            ("GNUCOBOL-SIDE", "gnu", "GnuCOBOL was wrong"), ("LATITUDE", "lat", "Both allowed by the standard"),
            ("KNOWN", "known", "Already known"), ("FIXED-SINCE", "fixed", "Already fixed"),
            ("UNINITIALIZED", "uninit", "Test read unset data"), ("NOT-COMPARABLE", "nc", "Results not comparable"),
            ("MODEL-ERROR", "model", "Bad test program"), ("UNDECIDED", "undec", "Undecided"))

HEADLINE = {"programs": r"Programs:\s*(\d+)", "judged": r"judged:\s*(\d+)", "usable": r"usable:\s*(\d+)",
            "usable_first": r"usable on the first attempt:\s*(\d+)", "they_reject": r"GnuCOBOL alone refused[^:]*:\s*(\d+)"}
SURVIVORS = re.compile(r"^## Survivors:\s*(\d+) programs in (\d+) signatures", re.M)


# ── recording a night ────────────────────────────────────────────────────────────────────────────────────────────────

def run_stats(run: pathlib.Path) -> dict:
    """The run's own numbers: run.json, report.md's headline and survivor line, and the harness classes of results.jsonl."""
    meta = json.loads((run / "run.json").read_text(encoding="utf-8"))
    report = (run / "report.md").read_text(encoding="utf-8", errors="replace")
    head = next((ln for ln in report.splitlines() if ln.startswith("Programs:")), "")
    out = {"run_id": run.name, "started": meta.get("started"), "model": meta.get("model"), "seed": meta.get("seed"),
           "hours": meta.get("hours"), "edition": meta.get("edition")}
    for key, pat in HEADLINE.items():
        m = re.search(pat, head)
        out[key] = int(m.group(1)) if m else None
    m = SURVIVORS.search(report)
    out["survivors"], out["survivor_signatures"] = (int(m.group(1)), int(m.group(2))) if m else (None, None)
    classes = collections.Counter()
    for line in (run / "results.jsonl").read_text(encoding="utf-8").splitlines():
        if line.strip():
            classes[json.loads(line)["verdict"]] += 1
    out["classes"] = dict(classes.most_common())
    return out


def verdict_tally(verdicts: list[dict]) -> dict[str, dict[str, int]]:
    """Signatures and programs per verdict, from verdict lines (`verdict`, `count`)."""
    out: dict[str, dict[str, int]] = {}
    for v in verdicts:
        t = out.setdefault(v["verdict"], {"signatures": 0, "programs": 0})
        t["signatures"] += 1
        t["programs"] += int(v["count"])
    return out


def defects_of(items: list[dict], run_id: str) -> list[str]:
    """The notes filed against WiseOwl COBOL from this run: a defect or adjudication note whose `source` names it."""
    return sorted(i["id"] for i in items if i.get("kind") in ("defect", "adjudication") and run_id in str(i.get("source", "")))


def night_record(run: pathlib.Path, night: int, verdicts: list[dict] | None, defects: list[str], judge: str | None,
                 evidence: str | None = None) -> dict:
    rec = {"night": night, **run_stats(run), "judge": judge, "verdicts": verdict_tally(verdicts) if verdicts else None,
           "defects": sorted(set(defects))}
    if evidence:
        rec["evidence"] = evidence
    return rec


def load_history(path: pathlib.Path = HISTORY) -> dict:
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else {"artifact": None, "nights": []}


def upsert(history: dict, rec: dict) -> dict:
    """The history with `rec` in place of the record of the same run (or added), nights in order."""
    nights = [n for n in history["nights"] if n["run_id"] != rec["run_id"]] + [rec]
    return {**history, "nights": sorted(nights, key=lambda n: n["night"])}


def write_history(history: dict, path: pathlib.Path = HISTORY) -> None:
    path.write_text(json.dumps(history, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


# ── rendering ────────────────────────────────────────────────────────────────────────────────────────────────────────

E = html.escape


def bar_chart(series: list[tuple[str, list[tuple[str, int, str]]]], width: int = 720, height: int = 220) -> str:
    """Grouped vertical bars: one group per label, one bar per (name, value, css class). One y scale for every bar."""
    top = max((v for _l, bars in series for _n, v, _c in bars), default=1) or 1
    step = 10 ** max(0, len(str(top)) - 1)
    ymax = -(-top // step) * step
    left, bottom, right = 46, 28, 8
    plot_w, plot_h = width - left - right, height - bottom - 10
    gw = plot_w / max(1, len(series))
    nb = max(1, max(len(b) for _l, b in series))
    bw = min(26, (gw - 10) / nb)
    out = [f'<svg viewBox="0 0 {width} {height}" width="100%" role="img" class="chart">']
    for k in range(5):
        y = 10 + plot_h - plot_h * k / 4
        val = ymax * k / 4
        out.append(f'<line x1="{left}" x2="{width - right}" y1="{y:.1f}" y2="{y:.1f}" class="grid"/>'
                   f'<text x="{left - 6}" y="{y + 4:.1f}" class="axis" text-anchor="end">{val:,.0f}</text>')
    for gi, (label, bars) in enumerate(series):
        x0 = left + gi * gw + (gw - nb * bw) / 2
        for bi, (name, v, cls) in enumerate(bars):
            h = plot_h * v / ymax
            x = x0 + bi * bw
            out.append(f'<rect x="{x:.1f}" y="{10 + plot_h - h:.1f}" width="{bw - 2:.1f}" height="{h:.1f}" class="{cls}">'
                       f'<title>{E(label)}: {E(name)} {v:,}</title></rect>')
        out.append(f'<text x="{left + gi * gw + gw / 2:.1f}" y="{height - 8}" class="axis" text-anchor="middle">{E(label)}</text>')
    out.append("</svg>")
    return "".join(out)


def stacked_rows(rows: list[tuple[str, dict[str, int], int]]) -> str:
    """One horizontal 100 % bar per night: the share of decided signatures per verdict."""
    css = {v: c for v, c, _ in VERDICTS}
    out = ['<div class="stack">']
    for label, counts, total in rows:
        cells = []
        for v, _c, name in VERDICTS:
            n = counts.get(v, 0)
            if n:
                cells.append(f'<span class="seg {css[v]}" style="flex:{n}" title="{E(name)}: {n} of {total}"></span>')
        out.append(f'<div class="srow"><span class="slabel">{E(label)}</span><span class="sbar">{"".join(cells)}</span>'
                   f'<span class="stotal">{total}</span></div>')
    out.append("</div>")
    return "".join(out)


def status_pill(status: str) -> str:
    cls = {"landed": "ok", "open": "bad", "owner": "warn", "retired": "muted", "half": "warn"}.get(status, "muted")
    return f'<span class="pill {cls}">{E(status)}</span>'


def short_title(it: dict) -> str:
    t = str(it.get("title", ""))
    t = re.sub(r"^PB\d+\s*[—-]\s*", "", t)
    t = re.sub(r"^(Defect( \(tooling\))?|RETIRED|RESOLVED READING)\s*[:—-]\s*", "", t)
    return t if len(t) <= 150 else t[:147].rstrip() + "…"


def night_label(n: dict) -> str:
    return f"Night {n['night']}"


def night_date(n: dict) -> str:
    """The evening the run started, in the run id's own local date (the id is the local start time)."""
    d = n["run_id"][len("fuzz-"):len("fuzz-") + 8]
    return f"{d[:4]}-{d[4:6]}-{d[6:]}"


def render(history: dict, items: list[dict], now: str | None = None) -> str:
    by_id = {i["id"]: i for i in items}
    nights = history["nights"]
    now = now or datetime.datetime.now().astimezone().strftime("%Y-%m-%d %H:%M %Z")
    total_programs = sum(n.get("judged") or n.get("programs") or 0 for n in nights)
    total_survivors = sum(n.get("survivors") or 0 for n in nights)
    defects = [(n, d) for n in nights for d in n["defects"]]
    fixed = sum(1 for _n, d in defects if by_id.get(d, {}).get("status") == "landed")
    upstream = [i for i in items if i.get("kind") == "upstream" and i.get("upstream_project") == "GnuCOBOL"]
    tiles = [(f"{len(nights)}", "nights of testing"), (f"{total_programs:,}", "test programs written and run"),
             (f"{total_survivors:,}", "on which the two compilers disagreed"), (f"{len(defects)}", "bugs found in WiseOwl"),
             (f"{fixed}", "of those bugs fixed"), (f"{len(upstream)}", "bugs recorded in GnuCOBOL")]

    yield_chart = bar_chart([(night_label(n), [("written", n.get("judged") or 0, "c-judged"),
                                               ("usable", n.get("usable") or 0, "c-usable"),
                                               ("disagreed", n.get("survivors") or 0, "c-surv")]) for n in nights])
    defect_chart = bar_chart([(night_label(n), [("bugs found in WiseOwl", len(n["defects"]), "bad")]) for n in nights],
                             height=180)
    triaged = [n for n in nights if n.get("verdicts")]
    stack = stacked_rows([(night_label(n), {v: t["signatures"] for v, t in n["verdicts"].items()},
                           sum(t["signatures"] for t in n["verdicts"].values())) for n in triaged])
    legend = "".join(f'<span class="key"><span class="sw {c}"></span>{E(name)}</span>' for _v, c, name in VERDICTS
                     if any(v == _v for n in triaged for v in n["verdicts"]))

    night_rows = "".join(
        f"<tr><td>{night_label(n)}</td><td>{night_date(n)}</td><td>{E(str(n.get('model') or ''))}</td>"
        f"<td class=num>{(n.get('judged') or n.get('programs') or 0):,}</td><td class=num>{(n.get('usable') or 0):,}</td>"
        f"<td class=num>{(n.get('survivors') or 0):,}</td><td class=num>{n.get('survivor_signatures') or ''}</td>"
        f"<td class=num>{len(n['defects'])}</td><td>{'itemized' if n.get('verdicts') else 'not itemized'}</td></tr>"
        for n in nights)

    verdict_head = "".join(f"<th class=num>{E(name)}</th>" for v, _c, name in VERDICTS if any(v in n["verdicts"] for n in triaged))
    shown = [v for v, _c, _n in VERDICTS if any(v in n["verdicts"] for n in triaged)]
    verdict_rows = "".join(
        f"<tr><td>{night_label(n)}</td>" + "".join(
            f"<td class=num>{n['verdicts'].get(v, {}).get('signatures', 0)}"
            f"<span class=sub> / {n['verdicts'].get(v, {}).get('programs', 0):,}</span></td>" for v in shown) + "</tr>"
        for n in triaged)

    defect_rows = "".join(
        f"<tr><td>{night_label(n)}</td><td><code>{E(d)}</code></td>"
        f"<td>{status_pill(str(by_id.get(d, {}).get('status', 'missing')))}</td><td>{E(short_title(by_id.get(d, {})))}</td></tr>"
        for n, d in defects)

    filed = collections.Counter(str(i.get("filed", "")) for i in upstream)
    up_rows = "".join(f"<tr><td>{E(day)}</td><td class=num>{k}</td></tr>" for day, k in sorted(filed.items()))
    tiles_html = "".join(f'<div class="tile"><span class="big">{v}</span><span class="cap">{E(c)}</span></div>' for v, c in tiles)
    return PAGE.format(now=E(now), tiles=tiles_html, yield_chart=yield_chart, defect_chart=defect_chart, stack=stack,
                       legend=legend, night_rows=night_rows, verdict_head=verdict_head, verdict_rows=verdict_rows,
                       defect_rows=defect_rows, up_rows=up_rows, first=night_date(nights[0]) if nights else "",
                       last=night_date(nights[-1]) if nights else "")


PAGE = """<title>Differential Fuzz Ledger</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;600&family=IBM+Plex+Mono:wght@400;600&family=Fraunces:opsz,wght@9..144,600&display=swap">
<style>
/* Layout: one reading column; summary tiles, then the charts, then the tables that back them. */
:root {{ --bg:#f6f7f4; --panel:#ffffff; --fg:#1d2420; --muted:#5e6a63; --line:#d9ddd6; --accent:#2f6b4f;
  --ok:#2f7d55; --bad:#c2412d; --warn:#b7791f; --gnu:#3866b5; --lat:#8a7fb0; --known:#6b8f9e; --fixed:#78a383;
  --uninit:#d2a046; --nc:#a7aca8; --model:#cfd3cf; --undec:#e3869f;
  --display:"Fraunces",Georgia,serif; --body:"IBM Plex Sans",system-ui,sans-serif; --mono:"IBM Plex Mono",ui-monospace,monospace; }}
@media (prefers-color-scheme: dark) {{ :root:not([data-theme="light"]) {{ --bg:#141816; --panel:#1c211e; --fg:#e4e9e5;
  --muted:#9aa59f; --line:#323a35; --accent:#7cc49d; --ok:#5fb684; --bad:#e8705b; --warn:#e0a84a; --gnu:#7a9fe3;
  --lat:#a99fd0; --known:#8db3c2; --fixed:#94c09f; --uninit:#e2b866; --nc:#7d837f; --model:#565c58; --undec:#ee9db2; color-scheme:dark; }} }}
:root[data-theme="dark"] {{ --bg:#141816; --panel:#1c211e; --fg:#e4e9e5; --muted:#9aa59f; --line:#323a35; --accent:#7cc49d;
  --ok:#5fb684; --bad:#e8705b; --warn:#e0a84a; --gnu:#7a9fe3; --lat:#a99fd0; --known:#8db3c2; --fixed:#94c09f;
  --uninit:#e2b866; --nc:#7d837f; --model:#565c58; --undec:#ee9db2; color-scheme:dark; }}
body {{ background:var(--bg); color:var(--fg); font:15px/1.55 var(--body); }}
.wrap {{ max-width:960px; margin:0 auto; padding-block:28px 56px; padding-left:max(16px, env(safe-area-inset-left, 0px));
  padding-right:max(16px, env(safe-area-inset-right, 0px)); display:grid; gap:28px; }}
h1 {{ font:600 2rem/1.15 var(--display); margin:0; text-wrap:balance; }}
h2 {{ font:600 1.15rem/1.3 var(--body); margin:0 0 10px; text-wrap:balance; }}
p {{ margin:0 0 10px; max-width:68ch; }}
.stamp {{ font:12px/1.5 var(--mono); color:var(--muted); border:1px solid var(--line); border-radius:6px; padding:8px 12px;
  background:var(--panel); }}
.tiles {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(140px,1fr)); gap:12px; }}
.tile {{ background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:12px 14px; display:grid; gap:2px; }}
.big {{ font:600 1.7rem/1.1 var(--display); font-variant-numeric:tabular-nums; }}
.cap {{ color:var(--muted); font-size:13px; }}
section {{ min-width:0; }}
ol.steps {{ margin:0; padding-left:1.3em; display:grid; gap:6px; max-width:70ch; }}
.chart {{ display:block; background:var(--panel); border:1px solid var(--line); border-radius:8px; }}
.chart .grid {{ stroke:var(--line); stroke-width:1; }}
.chart .axis {{ fill:var(--muted); font:11px var(--mono); }}
.c-judged {{ fill:var(--nc); }} .c-usable {{ fill:var(--accent); }} .c-surv {{ fill:var(--warn); }}
rect.bad {{ fill:var(--bad); }}
.legend {{ display:flex; flex-wrap:wrap; gap:6px 14px; font-size:13px; color:var(--muted); margin:8px 0 0; }}
.key {{ display:inline-flex; align-items:center; gap:6px; }}
.sw {{ width:11px; height:11px; border-radius:2px; display:inline-block; }}
.stack {{ display:grid; gap:8px; background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:12px; }}
.srow {{ display:grid; grid-template-columns:70px 1fr 44px; align-items:center; gap:10px; font-size:13px; }}
.sbar {{ display:flex; height:18px; border-radius:3px; overflow:hidden; }}
.stotal {{ text-align:right; font:12px var(--mono); color:var(--muted); }}
.seg.ok,.sw.ok {{ background:var(--ok); }} .seg.bad,.sw.bad {{ background:var(--bad); }} .seg.gnu,.sw.gnu {{ background:var(--gnu); }}
.seg.lat,.sw.lat {{ background:var(--lat); }} .seg.known,.sw.known {{ background:var(--known); }}
.seg.fixed,.sw.fixed {{ background:var(--fixed); }} .seg.uninit,.sw.uninit {{ background:var(--uninit); }}
.seg.nc,.sw.nc {{ background:var(--nc); }} .seg.model,.sw.model {{ background:var(--model); }} .seg.undec,.sw.undec {{ background:var(--undec); }}
.tablewrap {{ overflow-x:auto; background:var(--panel); border:1px solid var(--line); border-radius:8px; }}
table {{ border-collapse:collapse; width:100%; font-size:13px; }}
th,td {{ text-align:left; padding:7px 10px; border-bottom:1px solid var(--line); vertical-align:top; }}
th {{ font-weight:600; color:var(--muted); font-size:12px; text-transform:uppercase; letter-spacing:.04em; }}
td.num,th.num {{ text-align:right; font-variant-numeric:tabular-nums; white-space:nowrap; }}
.sub {{ color:var(--muted); }}
code {{ font:12px var(--mono); }}
.pill {{ font:600 11px var(--mono); padding:2px 7px; border-radius:10px; border:1px solid currentColor; white-space:nowrap; }}
.pill.ok {{ color:var(--ok); }} .pill.bad {{ color:var(--bad); }} .pill.warn {{ color:var(--warn); }} .pill.muted {{ color:var(--muted); }}
.cols {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:20px; }}
</style>
<div class="wrap">
<header style="display:grid;gap:12px">
  <h1>Differential Fuzz Ledger</h1>
  <p><b>WiseOwl COBOL</b> is a new compiler for COBOL, the programming language that much of the world's banking, insurance
  and government software is still written in. It is being built largely by AI coding agents working under one engineer's
  supervision, and its goal is to follow the international COBOL standard, ISO/IEC 1989, exactly. <b>GnuCOBOL</b> is the
  established free, open-source COBOL compiler, maintained for more than twenty years.</p>
  <p>This page tracks a nightly test called <b>differential fuzzing</b>: a computer writes thousands of small, random COBOL
  programs, and both compilers run every one of them. When the two give different answers, at least one of them is wrong, or
  the standard allows both answers. Each disagreement is then settled by reading the standard itself, never by trusting
  either compiler. The totals below run from {first} to {last}.</p>
  <div class="stamp">This page is machine-generated. A script (<code>scripts/fuzz_history.py</code>) rebuilds it and it is
  republished automatically after each night's testing is reviewed. Nothing on it is typed by hand: the numbers come from a
  history file kept with the project's source code, and the status of each bug is read from the project's work tracker at the
  moment the page is built. Built {now}.</div>
</header>
<div class="tiles">{tiles}</div>
<section>
  <h2>How a night of testing works</h2>
  <ol class="steps">
    <li><b>Write test programs.</b> For eight hours, an AI model running on a local graphics card is handed one rule of the
    COBOL standard at a time and asked to write a short program that uses it. If GnuCOBOL cannot compile a program, the model
    sees the error messages and gets up to three tries to fix it.</li>
    <li><b>Run them on both compilers.</b> Every program is compiled and run by WiseOwl and by GnuCOBOL. When both produce the
    same output, the program is set aside. Every difference is kept: one compiler refuses the program, the outputs differ, or
    one crashes or never finishes. Programs that differ in the same way are grouped together.</li>
    <li><b>Decide who is right.</b> The next morning about a dozen AI reviewers, each given one share of the groups and no
    permission to change anything, read the standard and decide each group. Every quotation from the standard they rely on is
    checked automatically against the standard's actual text.</li>
    <li><b>Challenge the result.</b> Each claimed GnuCOBOL bug is handed to a separate AI reviewer whose only job is to re-run it
    and try to prove the claim wrong. Only claims that survive are recorded.</li>
    <li><b>Record it.</b> A WiseOwl bug is re-run to confirm it and goes onto the project's list of work, where the project's
    owner decides when to fix it. A GnuCOBOL bug is written up for that project's maintainers.</li>
  </ol>
</section>
<section>
  <h2>How many programs each night</h2>
  {yield_chart}
  <div class="legend"><span class="key"><span class="sw nc"></span>programs written</span><span class="key"><span class="sw" style="background:var(--accent)"></span>usable (both compilers could run it)</span><span class="key"><span class="sw" style="background:var(--warn)"></span>the compilers disagreed</span></div>
  <p class="sub" style="margin-top:8px">Many generated programs are not valid COBOL at all; both compilers rightly refuse those,
  and they tell us nothing.</p>
</section>
<div class="cols">
<section>
  <h2>Bugs found in WiseOwl, night by night</h2>
  {defect_chart}
  <p class="sub" style="margin-top:8px">Most bugs were fixed within a day or two of the night that found them, so a falling count
  means the same kind of testing is finding less to fix.</p>
</section>
<section>
  <h2>Who was right</h2>
  {stack}
  <div class="legend">{legend}</div>
  <p class="sub" style="margin-top:8px">How each night's groups of disagreements were decided. Nights 1 and 2 were reviewed before
  these decisions were recorded one by one, so they are not shown.</p>
</section>
</div>
<section>
  <h2>The nights</h2>
  <div class="tablewrap"><table><thead><tr><th>Night</th><th>Started</th><th>AI model</th><th class=num>Programs</th><th class=num>Usable</th>
  <th class=num>Disagreed</th><th class=num>Groups</th><th class=num>WiseOwl bugs</th><th>Decisions</th></tr></thead>
  <tbody>{night_rows}</tbody></table></div>
</section>
<section>
  <h2>Decisions, night by night</h2>
  <div class="tablewrap"><table><thead><tr><th>Night</th>{verdict_head}</tr></thead><tbody>{verdict_rows}</tbody></table></div>
  <p class="sub" style="margin-top:8px">Groups of disagreements / programs in them. "Both allowed by the standard" means the
  standard leaves that detail to each compiler, so both answers are correct.</p>
</section>
<section>
  <h2>Every bug the testing found in WiseOwl</h2>
  <div class="tablewrap"><table><thead><tr><th>Night</th><th>Tracker id</th><th>Status</th><th>What was wrong</th></tr></thead>
  <tbody>{defect_rows}</tbody></table></div>
  <p class="sub" style="margin-top:8px">"landed" means the fix is in the compiler; "open" means it is waiting to be fixed;
  "retired" means closer study showed it was not a bug after all.</p>
</section>
<section>
  <h2>Bugs recorded in GnuCOBOL</h2>
  <div class="tablewrap" style="max-width:360px"><table><thead><tr><th>Recorded on</th><th class=num>Bugs</th></tr></thead>
  <tbody>{up_rows}</tbody></table></div>
  <p class="sub" style="margin-top:8px">The testing also checks the reference compiler. The first six nights' GnuCOBOL findings
  were written up together on 2026-10-27; each later night records its own.</p>
</section>
</div>
"""


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("render", help="render the summary page")
    r.add_argument("--out", help="the HTML file to write (default: print to standard output)")
    r.add_argument("--work", default=str(REPO / "kb" / "Work"), help="the register directory")
    sub.add_parser("show", help="print the history, one line per night")
    args = ap.parse_args(argv)
    history = load_history()
    if args.cmd == "show":
        for n in history["nights"]:
            print(f"night {n['night']}  {n['run_id']}  judged {n.get('judged')}  survivors {n.get('survivors')}  "
                  f"defects {','.join(n['defects']) or '-'}  verdicts {'yes' if n.get('verdicts') else 'no'}")
        print(f"artifact: {history.get('artifact')}")
        return 0
    sys.path.insert(0, str(HERE / "spec"))
    import work  # noqa: PLC0415  (the register's one reader)
    page = render(history, work.load(pathlib.Path(args.work)))
    if args.out:
        pathlib.Path(args.out).write_text(page, encoding="utf-8", newline="\n")
        print(f"wrote {args.out}")
    else:
        print(page)
    return 0


if __name__ == "__main__":
    from standard_streams import ensure_utf8

    ensure_utf8()
    raise SystemExit(main())
