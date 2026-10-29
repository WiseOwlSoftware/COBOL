#!/usr/bin/env python3
"""fuzz_history.py — THE CUMULATIVE RECORD OF THE DIFFERENTIAL-FUZZ NIGHTS AND THE PAGE RENDERED FROM IT (kb/Work PB4006).

    python scripts/fuzz_history.py render [--out <file.html>]     # the page AND FUZZ-LEDGER.md, one ledger version
    python scripts/fuzz_history.py mark-published [--page <html>] # after publishing main's version to the Artifact
    python scripts/fuzz_history.py status                         # is the Artifact in sync with main's FUZZ-LEDGER.md?
    python scripts/fuzz_history.py show                           # the history, one line per night
    python scripts/test_fuzz_history.py                           # the self-test (discovered by scripts/self_tests.py)

A night enters the history through `python scripts/fuzz_triage.py history --triage <dir> --write`, the last step of
every morning's triage (docs/rearchitecture/DESIGN-differential-fuzz.md §6): it reads the run directory, the triage's
verdict files and the register, and writes one record into HISTORY, a tracked file, because the run directories are
git-ignored and the triage directories are coordination scratch. It (and `render`) draws the HTML page and FUZZ-LEDGER.md
together, both stamped with one ledger version; once that FUZZ-LEDGER.md is on main, the session publishes the page to
the one Artifact whose URL HISTORY carries (`artifact`) and runs `mark-published`, which refuses a page that is not main's
version, so the Artifact and the repository file never show different ledgers. The page is machine-generated and says
so: no number on it is typed by hand, and every status on it is read from kb/Work at render time.

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
import hashlib
import html
import json
import pathlib
import re
import subprocess
import sys

from standard_streams import ensure_utf8

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
           "hours": meta.get("hours"), "edition": meta.get("edition"), "sections": meta.get("seeds")}
    for key, pat in HEADLINE.items():
        m = re.search(pat, head)
        out[key] = int(m.group(1)) if m else None
    m = SURVIVORS.search(report)
    out["survivors"], out["survivor_signatures"] = (int(m.group(1)), int(m.group(2))) if m else (None, None)
    missing = [k for k in ("programs", "judged", "usable", "survivors") if out[k] is None]
    if missing:
        raise SystemExit(f"⛔ {run / 'report.md'}: no {', '.join(missing)} in its headline or survivor line; the history "
                         "records a night only from a report it can read")
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


def is_finding(it: dict) -> bool:
    return it.get("kind") in ("defect", "adjudication")


def defects_of(items: list[dict], run_id: str) -> list[str]:
    """The notes the night filed on the WiseOwl side: a defect or adjudication note whose `source` names the run. The ONE
    rule every record is built with; what each note IS (a compiler bug, a test-harness bug, a settled question, a retired
    finding) is read from the register when the page is drawn (`finding_kind`), never stored."""
    return sorted(i["id"] for i in items if is_finding(i) and run_id in str(i.get("source", "")))


#: What a night's note is, in the page's words: (key, label). Only "compiler" counts as a bug found in WiseOwl.
FINDING_KINDS = {"compiler": "compiler bug", "harness": "test-harness bug", "settled": "question settled, no bug",
                 "retired": "not a bug after all"}

#: The product's code: a defect whose declared sites reach under it is a compiler bug (kb/Work PB4010).
PRODUCT_ROOT = "src/"

#: The fuzz defects filed before `fuzz_triage.py history` required a site block, with what each one is (kb/Work PB4010).
#: A CLOSED record, never extended: a defect not named here must declare its sites, and `finding_kind` refuses one that
#: does not. Their landings predate the src/ rename, so no site block could be backfilled from them.
LEGACY_KINDS = {**dict.fromkeys(("PB3490", "PB3523"), "harness"),
                **dict.fromkeys(("PB3489", "PB3491", "PB3510", "PB3511", "PB3512", "PB3517", "PB3538", "PB3562", "PB3563",
                                 "PB3564", "PB3566", "PB3567", "PB3568", "PB3570"), "compiler")}


def register():
    """scripts/spec/work.py, the register's one reader (of notes and of their site blocks)."""
    sys.path.insert(0, str(HERE / "spec"))
    import work  # noqa: PLC0415
    return work


def site_kind(it: dict) -> str | None:
    """"compiler" when the defect's declared sites (work.note_sites) name a path or glob under the product's code, "harness"
    when they name only others or `none (why)`, its LEGACY_KINDS entry when it has no site block; None when nothing says:
    no block, or a block naming only types and members (which declares no path: the same bar as work.sites_problems)."""
    sites = register().note_sites(str(it.get("_body", "")))
    named = (*sites.paths, *sites.globs)
    if named:
        return "compiler" if any(p.startswith(PRODUCT_ROOT) for p in named) else "harness"
    if sites.no_code:
        return "harness"
    return LEGACY_KINDS.get(str(it.get("id")))


def finding_kind(it: dict) -> str:
    """What a note is on the page: retired, a settled question, or a defect classified by `site_kind`. A defect nothing
    classifies stops the render, naming it, and so does a note the register lacks or one that is no finding (an analysis,
    a decision): it is never guessed into either count."""
    if "kind" not in it:
        raise SystemExit(f"⛔ {it['id']}: a night lists it but the register has no such note")
    if not is_finding(it):
        raise SystemExit(f"⛔ {it['id']}: a night lists it but it is a `{it['kind']}` note, not a defect or an adjudication")
    if it.get("status") == "retired":
        return "retired"
    if it.get("kind") == "adjudication":
        return "settled"
    kind = site_kind(it)
    if kind is None:
        raise SystemExit(f"⛔ {it['id']}: no **Sites:** block naming a path, a glob or `none (why)`, so the fuzz ledger cannot "
                         "tell a compiler bug from a harness bug (kb/Work PB4010)")
    return kind


def unrecorded_runs(history: dict, items: list[dict]) -> list[tuple[dict, str]]:
    """(run, note id) for every finding whose `source` names a fuzz run the history holds no night for (an aborted or
    shortened run, kb/Work PB4010) and no night already lists: the testing found it, so the page counts it."""
    recorded = {n["run_id"] for n in history["nights"]}
    listed = {d for n in history["nights"] for d in n["defects"]}
    out = []
    for it in items:
        if not is_finding(it) or it["id"] in listed:
            continue
        runs = sorted(set(RUN_ID.findall(str(it.get("source", "")))) - recorded)
        if runs:
            out.append(({"run_id": runs[0]}, it["id"]))
    return sorted(out, key=lambda rd: (rd[0]["run_id"], rd[1]))


def from_fuzzing(it: dict) -> bool:
    """A GnuCOBOL record the fuzzing produced: its `source` names the fuzz triage or the upstream-harvest plan (PB3838)."""
    s = str(it.get("source", ""))
    return it.get("kind") == "upstream" and it.get("upstream_project") == "GnuCOBOL" and it.get("status") != "retired" \
        and ("fuzz" in s or "PB3838" in s)


def night_record(run: pathlib.Path, night: int, verdicts: list[dict] | None, defects: list[str], judge: str | None,
                 evidence: str | None = None) -> dict:
    rec = {"night": night, **run_stats(run), "judge": judge, "verdicts": verdict_tally(verdicts) if verdicts else None,
           "defects": sorted(set(defects))}
    if evidence:
        rec["evidence"] = evidence
    return rec


def load_history(path: pathlib.Path | None = None) -> dict:
    path = path or HISTORY
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else {"artifact": None, "nights": []}


def upsert(history: dict, rec: dict) -> dict:
    """The history with `rec` in place of the record of the same run (or added), nights in order."""
    nights = [n for n in history["nights"] if n["run_id"] != rec["run_id"]] + [rec]
    return {**history, "nights": sorted(nights, key=lambda n: n["night"])}


def write_history(history: dict, path: pathlib.Path | None = None) -> None:
    (path or HISTORY).write_text(json.dumps(history, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")



# ── what the page says ───────────────────────────────────────────────────────────────────────────────────────────────
# The prose is written ONCE and drawn twice: as the Artifact's HTML page (`render_html`) and as FUZZ-LEDGER.md at the
# repository root (`render_markdown`), so the two cannot drift. `**x**` is bold; `{name}` is filled from `context`.

INTRO = (
    "**WiseOwl COBOL** is a new compiler for COBOL, the programming language that much of the world's banking, insurance and "
    "government software is still written in. It is being built largely by AI coding agents working under one engineer's "
    "supervision, and its goal is to follow the international COBOL standard, ISO/IEC 1989, exactly. **GnuCOBOL** is the "
    "established free, open-source COBOL compiler, maintained for more than twenty years.",
    "This page tracks a nightly test called **differential fuzzing**. An AI model picks sections of the standard at random, "
    "each one defining a COBOL statement, built-in function, data-description clause or kind of expression, and writes small "
    "test programs that use what that section defines. Both compilers run every one of those thousands of programs. When the "
    "two give different answers, at least one of them is wrong, or the standard allows both answers. Each disagreement is "
    "then settled by reading the standard itself, never by trusting either compiler. The totals below run from {first} to "
    "{last}.",
)
STAMP = ("This page is machine-generated. A script (`scripts/fuzz_history.py`) rebuilds it, and it is republished "
         "automatically after each night's testing is reviewed. Nothing on it is typed by hand: the numbers come from a history "
         "file kept with the project's source code (`conformance-suite/fuzz-history.json`), and the status of each bug is read "
         "from the project's work tracker at the moment the page is built. Ledger version {version}.")
STEPS = (
    ("Write test programs", "For {hours} hours, an AI model running on a local graphics card works through sections of the "
     "COBOL standard chosen at random ({sections} sections covering the statements, built-in functions, data-description "
     "clauses and expressions). For each section it reads that section's text and writes a few short programs that use what "
     "the section defines. If GnuCOBOL cannot compile a program, the model sees the error messages and gets up to three tries "
     "to fix it."),
    ("Run them on both compilers", "Every program is compiled and run by WiseOwl and by GnuCOBOL. When both produce the same "
     "output, the program is set aside. Every difference is kept: one compiler refuses the program, the outputs differ, or one "
     "crashes or never finishes. Programs that differ in the same way are grouped together."),
    ("Decide who is right", "The next morning a team of AI reviewers, each given one share of the groups and no permission to "
     "change anything, read the standard and decide each group. Every quotation from the standard they rely on is checked "
     "automatically against the standard's actual text."),
    ("Challenge the result", "Each claimed GnuCOBOL bug is handed to a separate AI reviewer whose only job is to re-run it and "
     "try to prove the claim wrong. Only claims that survive are recorded."),
    ("Record it", "A WiseOwl bug is re-run to confirm it and goes onto the project's list of work, where the project's owner "
     "decides when to fix it. A GnuCOBOL bug is written up for that project's maintainers."),
)
CAPTIONS = {
    "programs": "Many generated programs are not valid COBOL at all; both compilers rightly refuse those, and they tell us "
                "nothing.",
    "bugs": "Most bugs were fixed within a day or two of the night that found them, so a falling count means the same kind of "
            "testing is finding less to fix.",
    "who": "How each night's groups of disagreements were decided. {untallied}",
    "decisions": "Groups of disagreements / programs in them. \"Both allowed by the standard\" means the standard leaves that "
                 "detail to each compiler, so both answers are correct.",
    "findings": "Every note the testing filed on the WiseOwl side, including a run that stopped before a full night (\"Run "
                "of\" its date). Only compiler bugs count in the totals above. \"landed\" means "
                "the fix is in the compiler; \"open\" means it is waiting to be fixed; \"retired\" means closer study showed it "
                "was not a bug after all.",
    "gnucobol": "The testing also checks the reference compiler: the GnuCOBOL bugs it found, by the day each was written up.",
}
HEADINGS = {"how": "How a night of testing works", "programs": "How many programs each night",
            "bugs": "Bugs found in WiseOwl, night by night", "who": "Who was right", "nights": "The nights",
            "decisions": "Decisions, night by night", "findings": "Everything the testing found in WiseOwl",
            "gnucobol": "Bugs recorded in GnuCOBOL"}
LEGEND_PROGRAMS = (("judged", "programs written"), ("usable", "usable (both compilers could run it)"),
                   ("surv", "the compilers disagreed"))

#: The palette, light and dark, shared by the page's CSS and the standalone chart files.
LIGHT = {"bg": "#f6f7f4", "panel": "#ffffff", "fg": "#1d2420", "muted": "#5e6a63", "line": "#d9ddd6", "accent": "#2f6b4f",
         "ok": "#2f7d55", "bad": "#c2412d", "warn": "#b7791f", "gnu": "#3866b5", "lat": "#8a7fb0", "known": "#6b8f9e",
         "fixed": "#78a383", "uninit": "#d2a046", "nc": "#a7aca8", "model": "#cfd3cf", "undec": "#e3869f"}
DARK = {"bg": "#141816", "panel": "#1c211e", "fg": "#e4e9e5", "muted": "#9aa59f", "line": "#323a35", "accent": "#7cc49d",
        "ok": "#5fb684", "bad": "#e8705b", "warn": "#e0a84a", "gnu": "#7a9fe3", "lat": "#a99fd0", "known": "#8db3c2",
        "fixed": "#94c09f", "uninit": "#e2b866", "nc": "#7d837f", "model": "#565c58", "undec": "#ee9db2"}
#: A chart mark's class -> its palette token.
MARK = {"judged": "nc", "usable": "accent", "surv": "warn", **{c: c for _v, c, _n in VERDICTS}}

E = html.escape


def context(history: dict, items: list[dict]) -> dict:
    """Everything either rendering shows, computed once from the history and the register."""
    by_id = {i["id"]: i for i in items}
    nights = history["nights"]
    findings = [(n, d, finding_kind(by_id.get(d, {"id": d}))) for n in nights for d in n["defects"]] + [
        (r, d, finding_kind(by_id.get(d, {"id": d}))) for r, d in unrecorded_runs(history, items)]
    compiler = [d for _n, d, k in findings if k == "compiler"]
    upstream = [i for i in items if from_fuzzing(i)]
    hours = sorted({n["hours"] for n in nights if n.get("hours")})
    sections = sorted({n["sections"] for n in nights if n.get("sections")})
    untallied = [night_label(n) for n in nights if not n.get("verdicts")]
    triaged = [n for n in nights if n.get("verdicts")]
    shown = [v for v, _c, _n in VERDICTS if any(v in n["verdicts"] for n in triaged)]
    fill = {"first": night_date(nights[0]) if nights else "", "last": night_date(nights[-1]) if nights else "",
            "version": "",
            "hours": " or ".join(f"{h:g}" for h in hours) or "several",
            "sections": f"about {sections[-1]:,}" if sections else "many",
            "untallied": (" and ".join(untallied) + (" were" if len(untallied) > 1 else " was") + " reviewed before these "
                          "decisions were recorded one by one, so " + ("they are" if len(untallied) > 1 else "it is")
                          + " not shown.") if untallied else ""}
    return {"by_id": by_id, "nights": nights, "findings": findings, "triaged": triaged, "shown": shown, "fill": fill,
            "bugs": {n["run_id"]: [d for m, d, k in findings if m is n and k == "compiler"] for n in nights},
            "tiles": [(f"{len(nights)}", "nights of testing"),
                      (f"{sum(n.get('judged') or n.get('programs') or 0 for n in nights):,}", "test programs written and run"),
                      (f"{sum(n.get('survivors') or 0 for n in nights):,}", "on which the two compilers disagreed"),
                      (f"{len(compiler)}", "bugs found in WiseOwl"),
                      (f"{sum(1 for d in compiler if by_id.get(d, {}).get('status') == 'landed')}", "of those bugs fixed"),
                      (f"{len(upstream)}", "bugs recorded in GnuCOBOL")],
            "filed": sorted(collections.Counter(str(i.get("filed", "")) for i in upstream).items())}


def night_label(n: dict) -> str:
    """"Night N" for a recorded night; a run the history holds no night for is named by its date."""
    return f"Night {n['night']}" if "night" in n else f"Run of {night_date(n)}"


def night_date(n: dict) -> str:
    """The evening the run started, in the run id's own local date (the id is the local start time)."""
    d = n["run_id"][len("fuzz-"):len("fuzz-") + 8]
    return f"{d[:4]}-{d[4:6]}-{d[6:]}"


def short_title(it: dict) -> str:
    t = str(it.get("title", ""))
    t = re.sub(r"^PB\d+\s*[—-]\s*", "", t)
    t = re.sub(r"^(Defect( \(tooling\))?|RETIRED|RESOLVED READING)\s*[:—-]\s*", "", t)
    return t if len(t) <= 150 else t[:147].rstrip() + "…"


# ── the charts: one SVG drawing, used inline by the page and as a file by the Markdown ───────────────────────────────

def bar_chart(series: list[tuple[str, list[tuple[str, int, str]]]], width: int = 720, height: int = 220) -> str:
    """Grouped vertical bars: one group per label, one bar per (name, value, mark class). One y scale for every bar."""
    top = max((v for _l, bars in series for _n, v, _c in bars), default=1) or 1
    step = 10 ** max(0, len(str(top)) - 1)
    ymax = -(-top // step) * step
    left, bottom, right = 46, 28, 8
    plot_w, plot_h = width - left - right, height - bottom - 10
    gw = plot_w / max(1, len(series))
    nb = max([1] + [len(b) for _l, b in series])
    bw = min(26, (gw - 10) / nb)
    out = [f'<svg viewBox="0 0 {width} {height}" width="100%" role="img" class="chart">']
    for k in range(5):
        y = 10 + plot_h - plot_h * k / 4
        out.append(f'<line x1="{left}" x2="{width - right}" y1="{y:.1f}" y2="{y:.1f}" class="grid"/>'
                   f'<text x="{left - 6}" y="{y + 4:.1f}" class="axis" text-anchor="end">{ymax * k / 4:,.0f}</text>')
    for gi, (label, bars) in enumerate(series):
        x0 = left + gi * gw + (gw - nb * bw) / 2
        for bi, (name, v, cls) in enumerate(bars):
            h = plot_h * v / ymax
            out.append(f'<rect x="{x0 + bi * bw:.1f}" y="{10 + plot_h - h:.1f}" width="{bw - 2:.1f}" height="{h:.1f}" '
                       f'class="m-{cls}"><title>{E(label)}: {E(name)} {v:,}</title></rect>')
        out.append(f'<text x="{left + gi * gw + gw / 2:.1f}" y="{height - 8}" class="axis" text-anchor="middle">{E(label)}</text>')
    out.append("</svg>")
    return "".join(out)


def stacked_chart(rows: list[tuple[str, dict[str, int]]], width: int = 720) -> str:
    """One horizontal 100 % bar per night: each verdict's share of the night's decided signatures."""
    left, right, bar_h, gap = 70, 44, 18, 10
    height = len(rows) * (bar_h + gap) + gap
    span = width - left - right
    out = [f'<svg viewBox="0 0 {width} {height}" width="100%" role="img" class="chart">']
    for ri, (label, counts) in enumerate(rows):
        y = gap + ri * (bar_h + gap)
        total = sum(counts.values()) or 1
        x = left
        out.append(f'<text x="{left - 8}" y="{y + 13}" class="axis" text-anchor="end">{E(label)}</text>')
        for v, cls, name in VERDICTS:
            n = counts.get(v, 0)
            if n:
                w = span * n / total
                out.append(f'<rect x="{x:.1f}" y="{y}" width="{w:.1f}" height="{bar_h}" class="m-{cls}">'
                           f'<title>{E(label)}: {E(name)} {n} of {total}</title></rect>')
                x += w
        out.append(f'<text x="{width - right + 6}" y="{y + 13}" class="axis">{total}</text>')
    out.append("</svg>")
    return "".join(out)


def chart_css(prefix: str = "") -> str:
    """The chart classes, read from palette custom properties (`--name`)."""
    marks = " ".join(f"{prefix}.m-{c} {{ fill:var(--{t}); }}" for c, t in MARK.items())
    return (f"{prefix}.grid {{ stroke:var(--line); stroke-width:1; }} {prefix}.axis {{ fill:var(--muted); "
            f"font:11px 'IBM Plex Mono',ui-monospace,monospace; }} {marks}")


def palette(tokens: dict[str, str]) -> str:
    return "; ".join(f"--{k}:{v}" for k, v in tokens.items())


def standalone(svg: str) -> str:
    """A chart as a file of its own (GitHub shows it through an <img>): its own palette, light and dark."""
    style = (f"<style>svg {{ {palette(LIGHT)}; }} @media (prefers-color-scheme: dark) {{ svg {{ {palette(DARK)}; }} }} "
             f"{chart_css()}</style>")
    return svg.replace('<svg ', '<svg xmlns="http://www.w3.org/2000/svg" ', 1).replace(' class="chart">', ">" + style, 1) + "\n"


def charts(ctx: dict) -> dict[str, str]:
    nights = ctx["nights"]
    return {"programs": bar_chart([(night_label(n), [("written", n.get("judged") or n.get("programs") or 0, "judged"),
                                                     ("usable", n.get("usable") or 0, "usable"),
                                                     ("disagreed", n.get("survivors") or 0, "surv")]) for n in nights]),
            "bugs": bar_chart([(night_label(n), [("bugs found in WiseOwl", len(ctx["bugs"][n["run_id"]]), "bad")])
                               for n in nights], height=180),
            "who": stacked_chart([(night_label(n), {v: t["signatures"] for v, t in n["verdicts"].items()})
                                  for n in ctx["triaged"]])}


# ── the HTML page (the Artifact) ─────────────────────────────────────────────────────────────────────────────────────

def html_text(s: str, fill: dict) -> str:
    out = E(s.format(**fill))
    out = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", out)
    return re.sub(r"`(.+?)`", r"<code>\1</code>", out)


def html_table(head: list[str], rows: list[list[str]], numeric: set[int], wrap_style: str = "") -> str:
    th = "".join(f"<th{' class=num' if i in numeric else ''}>{E(h)}</th>" for i, h in enumerate(head))
    body = "".join("<tr>" + "".join(f"<td{' class=num' if i in numeric else ''}>{c}</td>" for i, c in enumerate(r)) + "</tr>"
                   for r in rows)
    return f'<div class="tablewrap"{wrap_style}><table><thead><tr>{th}</tr></thead><tbody>{body}</tbody></table></div>'


def status_pill(status: str) -> str:
    cls = {"landed": "ok", "open": "bad", "owner": "warn", "retired": "muted", "half": "warn"}.get(status, "muted")
    return f'<span class="pill {cls}">{E(status)}</span>'


def table_rows(ctx: dict) -> dict[str, tuple[list[str], list[list[str]], set[int]]]:
    """The four tables as (header, rows of PLAIN text, numeric columns); each renderer escapes or links the cells."""
    by_id, shown = ctx["by_id"], ctx["shown"]
    return {
        "nights": (["Night", "Started", "AI model", "Programs", "Usable", "Disagreed", "Groups", "WiseOwl bugs", "Decisions"],
                   [[night_label(n), night_date(n), str(n.get("model") or ""), f"{(n.get('judged') or n.get('programs') or 0):,}",
                     f"{(n.get('usable') or 0):,}", f"{(n.get('survivors') or 0):,}", str(n.get("survivor_signatures") or ""),
                     str(len(ctx["bugs"][n["run_id"]])), "itemized" if n.get("verdicts") else "not itemized"]
                    for n in ctx["nights"]], {3, 4, 5, 6, 7}),
        "decisions": (["Night"] + [name for v, _c, name in VERDICTS if v in shown],
                      [[night_label(n)] + [f"{n['verdicts'].get(v, {}).get('signatures', 0)} / "
                                           f"{n['verdicts'].get(v, {}).get('programs', 0):,}" for v in shown]
                       for n in ctx["triaged"]], set(range(1, len(shown) + 1))),
        "findings": (["Night", "Tracker id", "What it was", "Status", "Title"],
                     [[night_label(n), d, FINDING_KINDS[k], str(by_id.get(d, {}).get("status", "missing")),
                       short_title(by_id.get(d, {}))] for n, d, k in ctx["findings"]], set()),
        "gnucobol": (["Recorded on", "Bugs"], [[day, str(k)] for day, k in ctx["filed"]], {1}),
    }


def render_html(ctx: dict) -> str:
    fill, ch, tb = ctx["fill"], charts(ctx), table_rows(ctx)

    def plain(name: str, wrap: str = "") -> str:
        head, rows, num = tb[name]
        if name == "findings":
            rows = [[E(r[0]), f"<code>{E(r[1])}</code>", E(r[2]), status_pill(r[3]), E(r[4])] for r in rows]
        else:
            rows = [[E(c) for c in r] for r in rows]
        return html_table(head, rows, num, wrap)

    legend_programs = "".join(f'<span class="key"><span class="sw m-{c}"></span>{E(t)}</span>' for c, t in LEGEND_PROGRAMS)
    legend_who = "".join(f'<span class="key"><span class="sw m-{c}"></span>{E(name)}</span>' for v, c, name in VERDICTS
                         if v in ctx["shown"])
    tiles = "".join(f'<div class="tile"><span class="big">{E(v)}</span><span class="cap">{E(c)}</span></div>' for v, c in ctx["tiles"])
    steps = "".join(f"<li><b>{E(t)}.</b> {html_text(s, fill)}</li>" for t, s in STEPS)
    cap = {k: html_text(v, fill) for k, v in CAPTIONS.items()}
    body = f"""<div class="wrap">
<header style="display:grid;gap:12px"><h1>Differential Fuzz Ledger</h1>
{"".join(f"<p>{html_text(p, fill)}</p>" for p in INTRO)}
<div class="stamp">{html_text(STAMP, fill)}</div></header>
<div class="tiles">{tiles}</div>
<section><h2>{HEADINGS["how"]}</h2><ol class="steps">{steps}</ol></section>
<section><h2>{HEADINGS["programs"]}</h2>{ch["programs"]}<div class="legend">{legend_programs}</div><p class="sub">{cap["programs"]}</p></section>
<div class="cols">
<section><h2>{HEADINGS["bugs"]}</h2>{ch["bugs"]}<p class="sub">{cap["bugs"]}</p></section>
<section><h2>{HEADINGS["who"]}</h2>{ch["who"]}<div class="legend">{legend_who}</div><p class="sub">{cap["who"]}</p></section>
</div>
<section><h2>{HEADINGS["nights"]}</h2>{plain("nights")}</section>
<section><h2>{HEADINGS["decisions"]}</h2>{plain("decisions")}<p class="sub">{cap["decisions"]}</p></section>
<section><h2>{HEADINGS["findings"]}</h2>{plain("findings")}<p class="sub">{cap["findings"]}</p></section>
<section><h2>{HEADINGS["gnucobol"]}</h2>{plain("gnucobol", ' style="max-width:360px"')}<p class="sub">{cap["gnucobol"]}</p></section>
</div>
"""
    return HEAD.replace("/*PALETTE-LIGHT*/", palette(LIGHT)).replace("/*PALETTE-DARK*/", palette(DARK)).replace(
        "/*CHART-CSS*/", chart_css(".chart ") + " " + " ".join(f".sw.m-{c} {{ background:var(--{t}); }}" for c, t in MARK.items())
    ) + body


HEAD = """<title>Differential Fuzz Ledger</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;600&family=IBM+Plex+Mono:wght@400;600&family=Fraunces:opsz,wght@9..144,600&display=swap">
<style>
/* Layout: one reading column; summary tiles, then the charts, then the tables that back them. */
:root { /*PALETTE-LIGHT*/; --display:"Fraunces",Georgia,serif; --body:"IBM Plex Sans",system-ui,sans-serif;
  --mono:"IBM Plex Mono",ui-monospace,monospace; }
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) { /*PALETTE-DARK*/; color-scheme:dark; } }
:root[data-theme="dark"] { /*PALETTE-DARK*/; color-scheme:dark; }
body { background:var(--bg); color:var(--fg); font:15px/1.55 var(--body); }
.wrap { max-width:960px; margin:0 auto; padding-block:28px 56px; padding-left:max(16px, env(safe-area-inset-left, 0px));
  padding-right:max(16px, env(safe-area-inset-right, 0px)); display:grid; gap:28px; }
h1 { font:600 2rem/1.15 var(--display); margin:0; text-wrap:balance; }
h2 { font:600 1.15rem/1.3 var(--body); margin:0 0 10px; text-wrap:balance; }
p { margin:0 0 10px; max-width:68ch; }
p.sub { color:var(--muted); margin-top:8px; }
.stamp { font:12px/1.5 var(--mono); color:var(--muted); border:1px solid var(--line); border-radius:6px; padding:8px 12px;
  background:var(--panel); }
.tiles { display:grid; grid-template-columns:repeat(auto-fit,minmax(140px,1fr)); gap:12px; }
.tile { background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:12px 14px; display:grid; gap:2px; }
.big { font:600 1.7rem/1.1 var(--display); font-variant-numeric:tabular-nums; }
.cap { color:var(--muted); font-size:13px; }
section { min-width:0; }
ol.steps { margin:0; padding-left:1.3em; display:grid; gap:6px; max-width:70ch; }
.chart { display:block; background:var(--panel); border:1px solid var(--line); border-radius:8px; }
/*CHART-CSS*/
.legend { display:flex; flex-wrap:wrap; gap:6px 14px; font-size:13px; color:var(--muted); margin:8px 0 0; }
.key { display:inline-flex; align-items:center; gap:6px; }
.sw { width:11px; height:11px; border-radius:2px; display:inline-block; }
.tablewrap { overflow-x:auto; background:var(--panel); border:1px solid var(--line); border-radius:8px; }
table { border-collapse:collapse; width:100%; font-size:13px; }
th,td { text-align:left; padding:7px 10px; border-bottom:1px solid var(--line); vertical-align:top; }
th { font-weight:600; color:var(--muted); font-size:12px; text-transform:uppercase; letter-spacing:.04em; }
td.num,th.num { text-align:right; font-variant-numeric:tabular-nums; white-space:nowrap; }
code { font:12px var(--mono); }
.pill { font:600 11px var(--mono); padding:2px 7px; border-radius:10px; border:1px solid currentColor; white-space:nowrap; }
.pill.ok { color:var(--ok); } .pill.bad { color:var(--bad); } .pill.warn { color:var(--warn); } .pill.muted { color:var(--muted); }
.cols { display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:20px; }
</style>
"""


# ── the Markdown page (FUZZ-LEDGER.md at the repository root) ────────────────────────────────────────────────────────

LEDGER_MD = REPO / "FUZZ-LEDGER.md"
CHART_DIR = REPO / "docs" / "fuzz-ledger"


def md_cell(s: str) -> str:
    return s.replace("|", "\\|").replace("\n", " ")


def md_table(head: list[str], rows: list[list[str]], numeric: set[int]) -> str:
    rule = ["---:" if i in numeric else "---" for i in range(len(head))]
    return "\n".join("| " + " | ".join(r) + " |" for r in [head, rule] + rows)


def render_markdown(ctx: dict, artifact: str | None) -> tuple[str, dict[str, str]]:
    """FUZZ-LEDGER.md and its chart files (name -> SVG text), the same content as the page."""
    fill, tb = ctx["fill"], table_rows(ctx)
    files = {f"{k}.svg": standalone(v) for k, v in charts(ctx).items()}
    rel = CHART_DIR.relative_to(REPO).as_posix()

    def table(name: str) -> str:
        head, rows, num = tb[name]
        if name == "findings":
            rows = [[r[0], f"[{r[1]}](kb/Work/{r[1]}.md)", r[2], f"**{r[3]}**" if r[3] == "open" else r[3], md_cell(r[4])]
                    for r in rows]
        return md_table(head, [[md_cell(c) for c in r] for r in rows], num)

    legend_who = " · ".join(name for v, _c, name in VERDICTS if v in ctx["shown"])
    lines = ["<!-- MACHINE-GENERATED by scripts/fuzz_history.py (kb/Work PB4006): do not edit; the nightly triage rewrites "
             "this file. -->", "", "# Differential Fuzz Ledger", ""]
    for p in INTRO:
        lines += [p.format(**fill), ""]
    lines += ["> " + STAMP.format(**fill) + (f" The same page is published at {artifact}." if artifact else ""), ""]
    lines += [" · ".join(f"**{v}** {c}" for v, c in ctx["tiles"]), ""]
    lines += [f"## {HEADINGS['how']}", ""] + [f"{i}. **{t}.** {s.format(**fill)}" for i, (t, s) in enumerate(STEPS, 1)] + [""]
    lines += [f"## {HEADINGS['programs']}", "", f"![{HEADINGS['programs']}]({rel}/programs.svg)", "",
              "Bars: " + " · ".join(t for _c, t in LEGEND_PROGRAMS) + ". " + CAPTIONS["programs"].format(**fill), ""]
    lines += [f"## {HEADINGS['bugs']}", "", f"![{HEADINGS['bugs']}]({rel}/bugs.svg)", "", CAPTIONS["bugs"].format(**fill), ""]
    lines += [f"## {HEADINGS['who']}", "", f"![{HEADINGS['who']}]({rel}/who.svg)", "", "Colours, left to right: "
              + legend_who + ". " + CAPTIONS["who"].format(**fill), ""]
    for name in ("nights", "decisions", "findings", "gnucobol"):
        lines += [f"## {HEADINGS[name]}", "", table(name), ""]
        if name in CAPTIONS:
            lines += [CAPTIONS[name].format(**fill), ""]
    return "\n".join(lines).rstrip() + "\n", files


def write_markdown(ctx: dict, artifact: str | None, root: pathlib.Path | None = None) -> list[pathlib.Path]:
    """Write FUZZ-LEDGER.md and docs/fuzz-ledger/*.svg under `root` (the repository); the paths written."""
    root = root or REPO
    md, files = render_markdown(ctx, artifact)
    out = [root / LEDGER_MD.relative_to(REPO)]
    out[0].write_text(md, encoding="utf-8", newline="\n")
    d = root / CHART_DIR.relative_to(REPO)
    d.mkdir(parents=True, exist_ok=True)
    for name, text in files.items():
        (d / name).write_text(text, encoding="utf-8", newline="\n")
        out.append(d / name)
    return out


# ── keeping the Artifact and FUZZ-LEDGER.md in sync (owner 2026-10-28, kb/Work PB4010) ──────────────────────────────
# The two are ONE rendering: `write_ledger` draws both from one context and stamps both with the same LEDGER VERSION, a
# digest of the content alone (the Markdown and its charts drawn with an empty stamp), so it names what the page shows,
# never when it was drawn. The Artifact is published from main's FUZZ-LEDGER.md only: `mark-published` refuses a page
# whose version is not the version on origin/main, and records the one it published; `status` (session-probe prints it)
# says when the Artifact lags main or main's file lags the register.

VERSION = re.compile(r"[Ll]edger version ([0-9a-f]{12})")
PAGE_NAME = "fuzz-summary.html"
PUBLISHED = "fuzz-ledger-published.json"
MAIN_REF = "origin/main"
NEXT_STEPS = ("next: commit conformance-suite/fuzz-history.json, FUZZ-LEDGER.md and docs/fuzz-ledger/ and land them; once they "
              "are on main, publish {page} to the Artifact {artifact} and run `python scripts/fuzz_history.py mark-published "
              "--page {page}` (the Artifact must show the ledger version FUZZ-LEDGER.md carries on main)")


def ledger_version(ctx: dict, artifact: str | None) -> str:
    blank = {**ctx, "fill": {**ctx["fill"], "version": ""}}
    md, files = render_markdown(blank, artifact)
    digest = hashlib.sha256(md.encode("utf-8"))
    for name in sorted(files):
        digest.update(name.encode("utf-8") + b"\0" + files[name].encode("utf-8"))
    return digest.hexdigest()[:12]


def write_ledger(ctx: dict, artifact: str | None, page: pathlib.Path,
                 root: pathlib.Path | None = None) -> tuple[str, list[pathlib.Path]]:
    """THE ONE WRITER of both renderings: the HTML page at `page` and FUZZ-LEDGER.md with its charts under `root`, stamped
    with one ledger version; (version, paths written)."""
    version = ledger_version(ctx, artifact)
    ctx = {**ctx, "fill": {**ctx["fill"], "version": version}}
    page.parent.mkdir(parents=True, exist_ok=True)
    page.write_text(render_html(ctx), encoding="utf-8", newline="\n")
    return version, [page, *write_markdown(ctx, artifact, root)]


def version_of(text: str) -> str | None:
    m = VERSION.search(text)
    return m.group(1) if m else None


def _coord():
    sys.path.insert(0, str(HERE / "orchestrator"))
    import coord  # noqa: PLC0415
    return coord


def default_page(coord_override: str | None = None) -> pathlib.Path:
    return _coord().coord_dir(coord_override or None) / "scratch" / PAGE_NAME


def main_ledger(repo: pathlib.Path = REPO, fetch: bool = False) -> str:
    """FUZZ-LEDGER.md as origin/main carries it."""
    if fetch:
        subprocess.run(["git", "-C", str(repo), "fetch", "-q", "origin", "main"], check=True)
    out = subprocess.run(["git", "-C", str(repo), "show", f"{MAIN_REF}:{LEDGER_MD.relative_to(REPO).as_posix()}"],
                         capture_output=True, text=True, encoding="utf-8")
    if out.returncode:
        raise SystemExit(f"⛔ cannot read FUZZ-LEDGER.md on {MAIN_REF}: {out.stderr.strip()}")
    return out.stdout


def published_record(coord_override: str | None = None) -> dict:
    path = _coord().coord_dir(coord_override or None) / PUBLISHED
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}


def mark_published(page: pathlib.Path, url: str, main_text: str, coord_override: str | None = None) -> int:
    """Record that the Artifact `url` (always the history's `artifact`, the one FUZZ-LEDGER.md links) now shows `page`,
    refusing unless `page` is the ledger version main's FUZZ-LEDGER.md is."""
    have, want = version_of(page.read_text(encoding="utf-8")), version_of(main_text)
    if not have or have != want:
        print(f"⛔ {page.as_posix()} is ledger version {have or '(none)'}, FUZZ-LEDGER.md on {MAIN_REF} is {want or '(none)'}: "
              "the Artifact must show what main carries. Render from a checkout at main (or land the render), publish that "
              "page, then mark it")
        return 1
    path = _coord().coord_dir(coord_override or None) / PUBLISHED
    record = {"url": url, "version": have, "published_at": datetime.datetime.now().astimezone().isoformat(timespec="seconds")}
    path.write_text(json.dumps(record, indent=1) + "\n", encoding="utf-8", newline="\n")
    print(f"marked: the Artifact {url} shows ledger version {have}, the version on {MAIN_REF}")
    return 0


def sync_status(main_text: str, published: dict, artifact: str | None, fresh_md: str | None) -> str:
    """One line: does the Artifact (the history's `artifact`) show the version main's FUZZ-LEDGER.md carries, and is main's
    file exactly what a render gives now? `fresh_md` is that render, or None when the checkout is not at main (a checkout
    behind main would report drift that main does not have, kb/Work PB2987): then the second question is not asked."""
    on_main, shown = version_of(main_text), published.get("version")
    regenerate = "`fuzz_history.py render` on a branch, land it, then publish and mark"
    if on_main is None:
        return f"fuzz ledger: ⚠ REGENERATE OWED — FUZZ-LEDGER.md on {MAIN_REF} carries no ledger version: {regenerate}"
    if shown != on_main or published.get("url") != artifact:
        where = "" if published.get("url") in (None, artifact) else f" (the record names another Artifact, {published['url']})"
        return (f"fuzz ledger: ⛔ PUBLISH OWED — FUZZ-LEDGER.md on {MAIN_REF} is version {on_main}, the Artifact {artifact} "
                f"shows {shown or 'no recorded version'}{where}: render at main, publish the page, `fuzz_history.py mark-published`")
    if fresh_md is None:
        return (f"fuzz ledger: published version {on_main} is main's; whether the register moved since is UNCHECKED (this "
                f"checkout is not at {MAIN_REF}, or its history, kb/Work, work.py or fuzz_history.py carry uncommitted edits)")
    if fresh_md != main_text:
        return (f"fuzz ledger: ⚠ REGENERATE OWED — a render now (version {version_of(fresh_md)}) differs from FUZZ-LEDGER.md on "
                f"{MAIN_REF} (version {on_main}): {regenerate}")
    return f"fuzz ledger: current — the Artifact and FUZZ-LEDGER.md on {MAIN_REF} are version {on_main}"


def rendered_markdown(ctx: dict, artifact: str | None) -> str:
    """FUZZ-LEDGER.md exactly as `write_ledger` would write it (it carries no time, so equal content is equal bytes)."""
    return render_markdown({**ctx, "fill": {**ctx["fill"], "version": ledger_version(ctx, artifact)}}, artifact)[0]


#: What a render reads from the working tree: `status` compares it with main only when these are main's own content.
RENDER_INPUTS = ("conformance-suite/fuzz-history.json", "kb/Work", "scripts/fuzz_history.py", "scripts/spec/work.py")


def at_main(repo: pathlib.Path = REPO) -> bool:
    """HEAD is origin/main AND the render's inputs carry no uncommitted change (review R3-1): only then does a render here
    show what main's own content renders to."""
    heads = [subprocess.run(["git", "-C", str(repo), "rev-parse", ref], capture_output=True, text=True).stdout.strip()
             for ref in ("HEAD", MAIN_REF)]
    dirty = subprocess.run(["git", "-C", str(repo), "status", "--porcelain", "--", *RENDER_INPUTS],
                           capture_output=True, text=True).stdout.strip()
    return bool(heads[0]) and heads[0] == heads[1] and not dirty


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("render", help="write the summary page AND FUZZ-LEDGER.md with its charts, one ledger version")
    r.add_argument("--out", help=f"the HTML page to write (default: <coord>/scratch/{PAGE_NAME})")
    m = sub.add_parser("mark-published", help="record that the Artifact shows --page, which must be main's ledger version")
    m.add_argument("--page", help=f"the HTML page just published to the history's `artifact` (default: <coord>/scratch/{PAGE_NAME})")
    sub.add_parser("status", help="one line: is the Artifact in sync with main's FUZZ-LEDGER.md, and main's with the register")
    for p in (r, sub.choices["status"]):
        p.add_argument("--work", default=str(REPO / "kb" / "Work"), help="the register directory")
    sub.add_parser("show", help="print the history, one line per night")
    args = ap.parse_args(argv)
    history = load_history()
    if args.cmd == "show":
        for n in history["nights"]:
            print(f"night {n['night']}  {n['run_id']}  judged {n.get('judged')}  survivors {n.get('survivors')}  "
                  f"notes {','.join(n['defects']) or '-'}  verdicts {'yes' if n.get('verdicts') else 'no'}")
        print(f"artifact: {history.get('artifact')}")
        return 0
    if args.cmd == "mark-published":
        page = pathlib.Path(args.page) if args.page else default_page()
        if not history.get("artifact"):
            raise SystemExit("⛔ the history names no `artifact`: publish the page once, record its URL in the history, land it")
        return mark_published(page, history["artifact"], main_ledger(fetch=True))
    if args.cmd == "status":
        fresh = (rendered_markdown(context(history, register().load(pathlib.Path(args.work))), history.get("artifact"))
                 if at_main() else None)
        print(sync_status(main_ledger(), published_record(), history.get("artifact"), fresh))
        return 0
    ctx = context(history, register().load(pathlib.Path(args.work)))
    page = pathlib.Path(args.out) if args.out else default_page()
    version, written = write_ledger(ctx, history.get("artifact"), page)
    print("wrote " + ", ".join(p.as_posix() for p in written) + f" (ledger version {version})")
    print(NEXT_STEPS.format(page=page.as_posix(), artifact=history.get("artifact")))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    raise SystemExit(main())
