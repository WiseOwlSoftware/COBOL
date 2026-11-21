#!/usr/bin/env python3
"""Render the owner's **WiseOwl COBOL Completion Ledger** artifact: each edition's state, in the owner's order, computed.

    python scripts/spec/gen_ledger.py                       # write the default --out, <coord>/ledger.html
    python scripts/spec/gen_ledger.py --out ledger.html     # write somewhere else
    python scripts/spec/gen_ledger.py --check               # render in memory, write nothing; non-zero if it cannot

⭐ WHAT THE PAGE IS (kb/Work PB5994, owner 2026-11-20: the ledger "must be grossly out of date and likely needs
replanning, relayout and rewriting"). The mission is four editions, 1985 first (owner 2026-11-15), and the ledger it
replaces (kb/Work PB2912) measured one goal, 2023 v1.0, by the inventory's GAP, which counts neither the open notes nor
the normative text with no catalog row. So the page is now, in this order:

  1. THE EDITIONS: one line per edition in the owner's order (`ORDER`: 1985, 2002, 2023, 2014), its verdict and the
     counts that block it, and the verdict line exactly as `edition_status.py --edition N` prints it;
  2. PER EDITION: its four measured parts, its open notes (HARM first, with titles; then QUESTION, TOOLING, OTHER, by
     id), the text of its transcription in no catalog row, and the work in flight on it (each branch the survey does not
     read as landed, with its lifecycle state and evidence);
  3. the work in flight that names no edition;
  4. PACING: the running account's quota against its allowance (`budget.current`, the reading `budget.py` prints) and
     the landings over the last `WINDOW_DAYS` days, measured from origin/main's merges, with the notes of each edition
     they filed and closed (`landings`). No completion date: no measured rate stands behind one.

⛔ ONE MEASURE, ONE FUNCTION (PB5994 decision 1). Everything the page says about an edition is `edition_status.py`'s:
`edition_status.status` gives the parts and `edition_status.verdict` the verdict, and this file computes nothing about an
edition a second way. Which editions a note blocks is its declared `editions:` field (`work.declared_editions`, kb/Work PB5995), and a branch's are its notes'. A
part `edition_status` cannot measure is shown as not measured, and the edition is NOT COMPLETE.

⛔ NO FIGURE WITHOUT ITS INPUT (PB5994 decision 2, 3). The work in flight is `prune_worktrees.live_survey` (the branch
survey every reader of the lifecycle shares, which reads GitHub's pull requests and merge queue through
`queue_facts.read`); the quota is `budget.current`; the throughput is `git log` of origin/main. An input this machine
cannot read (no coordination directory, no telemetry, no network) is UNMEASURED with its reason, never a zero or an
estimate. `--check` (the gate's rendered-views audit, CI's audits job) renders the page from the repository alone: the
two live inputs are UNMEASURED there by construction, so the audit is offline and fast.

⛔ REMOVED, NOT KEPT BESIDE IT (PB5994 decision 3, CLAUDE.md rule 4): the 2023-only v1.0 buckets and their lanes and
estimate (`ledger_plan.py`, deleted), the GAP burn-down and its trend series (`<coord>/ledger-trend.json` is no longer
read or written), the battery, the A.1/A.3/A.4 documentation posture, the suite and anchor counts.

The URL is the running Claude account's (an artifact belongs to the account that published it): `ledger_state.py` keeps
one per account in the coordination directory, and this script prints it after a render (kb/Work PB2481). The page is
stamped with the last commit that touched an input it reports (`STAMP_PATHS`), which `ledger_state.py` reads to say
whether a publish is owed.
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import html
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "orchestrator"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import code_read_store  # noqa: E402  (the code-read stores' paths: an input of edition_status's part 2)
import coord  # noqa: E402  (names the coordination directory)
import edition_status  # noqa: E402  (THE measure of an edition, kb/Work PB5924)
import inventory_schema  # noqa: E402  (the held editions' stores and the view's files)
import spec_pdf  # noqa: E402  (the 1985 transcription: an input of edition_status's part 4)
import work  # noqa: E402  (THE reader of the register)
from standard_streams import ensure_utf8  # noqa: E402

COORD_DIR = coord.coord_path()
DEFAULT_OUT = COORD_DIR / "ledger.html"
TITLE = "WiseOwl COBOL Completion Ledger"
CSS_PATH = REPO / "scripts" / "spec" / "data" / "ledger.css"

#: THE OWNER'S ORDER of the editions (2026-11-15: 1985 first; 2014 after the other three).
ORDER = (1985, 2002, 2023, 2014)
#: The window the landings are counted over.
WINDOW_DAYS = 7
#: A note id a branch name carries (`pb5449-mechanism`, `fix-PB5917`).
BRANCH_NOTE = re.compile(r"(?i)(?<![a-z0-9])pb(\d+)")

#: The paths whose CONTENT the page reports: it is stamped with the last commit that touched one, not with HEAD, so an
#: unrelated commit cannot make a correct page look stale (ledger_state.py reads the same tuple). The inventory's stores
#: and the files its view reads (part 1), the code-read stores (part 2), the register (part 3), the 1985 transcription
#: and catalog (part 4: the catalog is one of the view's files). The view's files are the schema's (`view_files`) and the
#: refuter records the register names (`refuter_files`), the list `inventory_schema.read_view_ahead` reads (kb/Work PB5374).
STAMP_PATHS = tuple(dict.fromkeys([
    *(ed.store for ed in inventory_schema.EDITIONS if ed.held),
    *inventory_schema.load_schema().view_files(),
    *inventory_schema.refuter_files(inventory_schema.WORKING_TREE.json(inventory_schema.REGISTER_EXTRACT_REL)),
    *(code_read_store.store_dir(ed.year, REPO).relative_to(REPO).as_posix()
      for ed in inventory_schema.EDITIONS if ed.held and not ed.newest),
    "kb/Work",
    spec_pdf.markdown(1985, REPO).relative_to(REPO).as_posix(),
]))


@dataclasses.dataclass(frozen=True)
class Unmeasured:
    """An input this render could not read, and why: the page says UNMEASURED with the reason."""
    why: str


@dataclasses.dataclass(frozen=True)
class Edition:
    """One edition as `edition_status` measures it: its parts, and its verdict (`edition_status.verdict`)."""
    year: int
    parts: tuple[edition_status.Part, ...]
    complete: bool
    line: str

    def part(self, name: str) -> edition_status.Part:
        return next(p for p in self.parts if p.name == name)


@dataclasses.dataclass(frozen=True)
class Branch:
    """A branch the survey does not read as landed: its lifecycle state and evidence, the notes it names, its editions."""
    name: str
    state: str
    why: str
    notes: tuple[str, ...]
    editions: frozenset[int]


@dataclasses.dataclass(frozen=True)
class Ledger:
    """Everything the page shows, measured."""
    head: dict
    editions: tuple[Edition, ...]
    titles: dict[str, str]
    flight: tuple[Branch, ...] | Unmeasured
    budget: dict | Unmeasured
    landings: dict | Unmeasured


# ── measurement ─────────────────────────────────────────────────────────────────────────────────────────────────

def git(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True, encoding="utf-8", errors="replace")


def measure_head() -> dict:
    """The tree this page describes: the last commit that touched a measured input (`STAMP_PATHS`)."""
    line = git("log", "-1", "--format=%h|%cI", "--abbrev=8", "--", *STAMP_PATHS).stdout.strip()
    if not line:
        raise SystemExit("⛔ no commit touches any measured input — is this a git checkout of the repo?")
    sha, _, iso = line.partition("|")
    return {"sha": sha, "date": iso[:10]}


def measure_edition(year: int, sources: edition_status.Sources = edition_status.SOURCES) -> Edition:
    """THE edition's state: `edition_status.status` and `edition_status.verdict`, nothing else."""
    parts = edition_status.status(year, sources)
    complete, line = edition_status.verdict(year, parts)
    return Edition(year, tuple(parts), complete, line)


def note_editions(items: list[dict]) -> dict[str, frozenset[int] | None]:
    """Every note's declared editions (`work.declared_editions`; None for a note that declares none: a terminal note
    closed before the field existed, which `landings` counts apart, never as touching no edition)."""
    return {it["id"]: work.declared_editions(it) for it in items}


def branch_of(name: str, state: str, why: str, lead: str | None, editions_of: dict[str, frozenset[int] | None]) -> Branch:
    """A survey row as the page shows it: the notes its report's lead and its name carry, and the editions those notes
    declare (a branch's name is no declaration: kb/Work PB5995)."""
    notes = tuple(dict.fromkeys([*([lead] if lead else []), *(f"PB{n}" for n in BRANCH_NOTE.findall(name))]))
    eds: set[int] = set()
    for n in notes:
        eds |= editions_of.get(n) or frozenset()
    return Branch(name, state, why, notes, frozenset(eds))


def measure_flight(editions_of: dict[str, frozenset[int] | None]) -> tuple[Branch, ...] | Unmeasured:
    """Every branch the survey does not read as LANDED (`prune_worktrees.live_survey`, after the fetch its own main
    makes), or UNMEASURED with the reason the survey could not run."""
    import prune_worktrees  # noqa: PLC0415  (the branch survey: git, the reports, GitHub; only a write render reads it)
    try:
        prune_worktrees.fetch_origin()
        rows = prune_worktrees.live_survey()
    except (OSError, subprocess.SubprocessError, SystemExit, RuntimeError, ValueError) as e:
        return Unmeasured(f"the branch survey (prune_worktrees.live_survey) failed: {e}")
    return in_flight(rows, editions_of)


def in_flight(rows, editions_of: dict[str, frozenset[int] | None]) -> tuple[Branch, ...]:
    """The survey's rows (`prune_worktrees.Row`) whose work is not on main, as the page shows them."""
    return tuple(branch_of(r.name, r.state, r.why, r.report.lead if r.report else None, editions_of)
                 for r in rows if r.state != "LANDED")


def measure_budget() -> dict | Unmeasured:
    """The running account's quota reading, exactly as `budget.py --json` prints it, or UNMEASURED."""
    import budget  # noqa: PLC0415  (the telemetry sink and the readings: only a write render reads them)
    try:
        return budget.current()
    except (OSError, LookupError, ValueError, KeyError, SystemExit) as e:
        return Unmeasured(f"budget.current() could not read the quota: {e}")


#: A note file's diff header in `git log -p` (the note itself: an addendum carries no status).
NOTE_FILE = re.compile(r"^\+\+\+ b/kb/Work/((?:PB|R)\d+)\.md$")
#: The line `git log --format` writes before each landing (a diff line never opens with it: a hunk header opens with `@@`).
LANDING = "LANDING "
#: A status line a diff adds.
ADDED_STATUS = re.compile(r"^\+status:\s*(\S+)")


def landings(log: str, editions_of: dict[str, frozenset[int] | None]) -> dict:
    """What the landings `log` lists did to the register (`git log --first-parent -m --merges -p -U0 --format=LANDING %h --
    kb/Work`): per edition, the notes FILED (a new note file) and CLOSED (its `status:` set to one of
    `work.TERMINAL_STATUSES`) by them; {filed: {year: n}, closed: {year: n}, undeclared: {filed: n, closed: n}}: a note
    that declares no editions (`note_editions`) is counted apart, never as no edition."""
    filed: collections.Counter[int] = collections.Counter()
    closed: collections.Counter[int] = collections.Counter()
    undeclared: collections.Counter[str] = collections.Counter()

    def count(into: collections.Counter, what: str, nid: str) -> None:
        eds = editions_of.get(nid)
        if eds is None:
            undeclared[what] += 1
        else:
            into.update(eds)
    note, new = None, False
    for line in log.splitlines():
        if line.startswith(LANDING):
            note, new = None, False
        elif line.startswith("--- "):
            new = line == "--- /dev/null"
        elif line.startswith("+++ "):
            note = m.group(1) if (m := NOTE_FILE.match(line)) is not None else None
            if note and new:
                count(filed, "filed", note)
        elif note and (m := ADDED_STATUS.match(line)) is not None and m.group(1) in work.TERMINAL_STATUSES:
            count(closed, "closed", note)
    return {"filed": {y: filed[y] for y in ORDER}, "closed": {y: closed[y] for y in ORDER},
            "undeclared": {k: undeclared[k] for k in ("filed", "closed")}}


def measure_landings(editions_of: dict[str, frozenset[int] | None], days: int = WINDOW_DAYS) -> dict | Unmeasured:
    """The landings on origin/main's first-parent line over the last `days` days (each a merge of the merge queue), and
    the notes of each edition they filed and closed."""
    since = f"--since={days}.days.ago"
    count = git("rev-list", "--count", "--first-parent", "--merges", since, "origin/main")
    log = git("log", "origin/main", "--first-parent", "-m", "--merges", since, "-p", "-U0", f"--format={LANDING}%h",
              "--", "kb/Work")
    if count.returncode != 0 or log.returncode != 0:
        return Unmeasured(f"git log origin/main failed: {(count.stderr or log.stderr).strip()[:200]}")
    return {**landings(log.stdout, editions_of), "total": int(count.stdout.strip()), "days": days}


def measure(live: bool, sources: edition_status.Sources = edition_status.SOURCES) -> Ledger:
    """The page's inputs. `live` False (`--check`): the branch survey and the quota are not read."""
    items = work.load()
    editions_of = note_editions(items)
    offline = Unmeasured("--check renders from the repository alone and reads no live state")
    return Ledger(head=measure_head(), editions=tuple(measure_edition(y, sources) for y in ORDER),
                  titles={it["id"]: str(it.get("title", "")) for it in items},
                  flight=measure_flight(editions_of) if live else offline,
                  budget=measure_budget() if live else offline,
                  landings=measure_landings(editions_of))


# ── the page ────────────────────────────────────────────────────────────────────────────────────────────────────

def e(s) -> str:
    return html.escape(str(s))


def unmeasured_html(u: Unmeasured) -> str:
    return f'<p><span class="pill warn">UNMEASURED</span> {e(u.why)}</p>'


def count_cell(p: edition_status.Part, extra: str = "") -> str:
    if not p.measured:
        return '<td class="r"><span class="pill warn">not measured</span></td>'
    return f'<td class="r num">{p.count:,}{extra}</td>'


def verdict_pill(ed: Edition) -> str:
    return '<span class="pill good">COMPLETE</span>' if ed.complete else '<span class="pill crit">NOT COMPLETE</span>'


def top_table(eds: tuple[Edition, ...]) -> str:
    rows = []
    for ed in eds:
        notes = ed.part("open work notes")
        harm = f' <span class="dim">({len(notes.data.get("HARM", []))} harm)</span>' if notes.measured else ""
        gap = ed.part("inventory GAP")
        # a bounded inventory (edition_status.inventory_count): the GAP rows and the pending items, side by side
        bound = f' <span class="dim">({e(gap.shown)})</span>' if gap.measured and gap.shown else ""
        rows.append(f'<tr><td><a href="#e{ed.year}">{ed.year}</a></td><td>{verdict_pill(ed)}</td>'
                    + count_cell(gap, bound) + count_cell(ed.part("code reads"))
                    + count_cell(notes, harm) + count_cell(ed.part("catalog prose holes")) + "</tr>")
    return ('<div class="tablecard"><table><thead><tr><th>Edition</th><th>Verdict</th><th class="r">Inventory GAP</th>'
            '<th class="r">Code reads owed</th><th class="r">Open notes</th><th class="r">Text in no row</th></tr></thead>'
            f'<tbody>{"".join(rows)}</tbody></table></div>')


def verdict_html(ed: Edition) -> str:
    return f'<p class="verdict {"good" if ed.complete else "crit"}">{e(ed.line)}</p>'


#: The parts whose detail has a section of its own below the list (`notes_html`, `holes_html`).
DETAILED = ("open work notes", "catalog prose holes")


def parts_html(ed: Edition) -> str:
    out = []
    for i, p in enumerate(ed.parts, 1):
        head = f"{i}. {p.name}: " + (f"{p.count:,}" + (f" ({p.shown})" if p.shown else "") if p.measured
                                     else f"not measured — {p.why}")
        lines = () if p.name in DETAILED else p.lines
        out.append(f"<li>{e(head)}" + "".join(f"<br><span class=\"dim\">{e(x)}</span>" for x in lines) + "</li>")
    return f'<ul class="parts">{"".join(out)}</ul>'


def subject(note: str, titles: dict[str, str]) -> str:
    """A note's title without its leading id (`PB5917 - the ...`)."""
    return titles.get(note, "").removeprefix(note).lstrip(" -—")


def notes_html(ed: Edition, titles: dict[str, str]) -> str:
    p = ed.part("open work notes")
    if not p.measured:
        return unmeasured_html(Unmeasured(p.why))
    out = []
    harm = p.data.get("HARM", [])
    if harm:
        out.append('<div class="tablecard"><table><thead><tr><th>Harm</th><th>Note</th></tr></thead><tbody>'
                   + "".join(f'<tr><td class="mono">{e(i)}</td><td>{e(subject(i, titles))}</td></tr>' for i in harm)
                   + "</tbody></table></div>")
    for c in edition_status.CATEGORIES[1:]:
        ids = p.data.get(c, [])
        if ids:
            out.append(f'<p><strong>{c} {len(ids)}</strong> <span class="ids">{e(" ".join(ids))}</span></p>')
    return "".join(out) or "<p>None open.</p>"


def holes_html(ed: Edition) -> str:
    p = ed.part("catalog prose holes")
    if not p.measured:
        return unmeasured_html(Unmeasured(p.why))
    sections = p.data.get("by-section") or {}
    by = (f'<p class="ids">by section: {e(" · ".join(f"{k} {v}" for k, v in sorted(sections.items())))}</p>'
          if sections else "")
    return "".join(f"<p>{e(x)}</p>" for x in p.lines) + by


#: The lifecycle states (prune_worktrees.py) in which a branch waits on a person or a finisher, shown loud.
STUCK = ("BOUNCED", "STOPPED", "DECISION")


def flight_table(branches: list[Branch]) -> str:
    if not branches:
        return "<p>No branch in flight.</p>"
    rows = "".join(f'<tr><td class="mono">{e(b.name)}</td>'
                   f'<td><span class="pill {"crit" if b.state in STUCK else "warn"}">{e(b.state)}</span></td>'
                   f'<td class="mono">{e(" ".join(b.notes))}</td><td><span class="dim">{e(b.why[:160])}</span></td></tr>'
                   for b in sorted(branches, key=lambda b: (b.state, b.name)))
    return ('<div class="tablecard"><table><thead><tr><th>Branch</th><th>State</th><th>Notes</th><th>Evidence</th></tr>'
            f'</thead><tbody>{rows}</tbody></table></div>')


def flight_html(flight: tuple[Branch, ...] | Unmeasured, year: int | None) -> str:
    if isinstance(flight, Unmeasured):
        return unmeasured_html(flight)
    return flight_table([b for b in flight if (year in b.editions if year is not None else not b.editions)])


def pacing_html(budget: dict | Unmeasured, land: dict | Unmeasured) -> str:
    if isinstance(budget, Unmeasured):
        quota = unmeasured_html(budget)
    else:
        used, allow = float(budget["weekly_est_pct"]), float(budget["allowance_pct"])
        quota = (f'<div class="meter-card"><div class="meter-head"><span class="t">Weekly quota, {e(budget["account"])}</span>'
                 f'<span class="v num">{used:g} % used · allowance {allow:g} % (day {budget["day"]}) · '
                 f'{e(budget["decision"])}</span></div><div class="meter"><div class="fill" style="width:'
                 f'{min(100.0, used):.1f}%"></div></div><p class="note">budget.py{e(budget.get("anchor_note") or "")}; '
                 f'headroom {float(budget["headroom_pct"]):g} points under today\'s allowance.</p></div>')
    if isinstance(land, Unmeasured):
        thru = unmeasured_html(land)
    else:
        rows = "".join(f'<tr><td>{y}</td><td class="r num">{land["filed"][y]}</td><td class="r num">{land["closed"][y]}</td>'
                       f'<td class="r num">{land["closed"][y] - land["filed"][y]:+d}</td></tr>' for y in ORDER)
        und = land["undeclared"]
        if und["filed"] or und["closed"]:
            rows += (f'<tr><td>no editions declared</td><td class="r num">{und["filed"]}</td><td class="r num">'
                     f'{und["closed"]}</td><td class="r num">{und["closed"] - und["filed"]:+d}</td></tr>')
        thru = (f"<p>Landings on origin/main over the last {land['days']} days: <strong>{land['total']}</strong> "
                f"({land['total'] / land['days']:.1f} a day). The notes of each edition those landings filed and closed "
                "(a note counts for every edition it declares; one closed before the field existed is counted apart):</p>"
                '<div class="tablecard"><table><thead><tr><th>Edition</th><th class="r">Notes filed</th>'
                f'<th class="r">Notes closed</th><th class="r">Net</th></tr></thead><tbody>{rows}</tbody></table></div>'
                "<p>No completion date is estimated: the open notes are no measured unit of work, and an edition that "
                "gains notes as fast as it closes them has no rate that reaches zero.</p>")
    return quota + thru


def render(led: Ledger) -> str:
    per = []
    for ed in led.editions:
        per.append(f'<section aria-label="Edition {ed.year}" id="e{ed.year}"><h2>Edition {ed.year}</h2>'
                   + verdict_html(ed) + parts_html(ed)
                   + f'<h3>Open notes</h3>{notes_html(ed, led.titles)}'
                   + f'<h3>Text in no catalog row</h3>{holes_html(ed)}'
                   + f'<h3>Work in flight</h3>{flight_html(led.flight, ed.year)}</section>')
    return TEMPLATE.format(
        title=TITLE, css=CSS_PATH.read_text(encoding="utf-8"), head_sha=e(led.head["sha"]), head_date=e(led.head["date"]),
        order=" · ".join(str(y) for y in ORDER), top=top_table(led.editions),
        verdicts="".join(verdict_html(ed) for ed in led.editions), editions="".join(per),
        unattributed=flight_html(led.flight, None), pacing=pacing_html(led.budget, led.landings))


TEMPLATE = """<title>{title}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Serif:wght@600&family=IBM+Plex+Sans:wght@400;500;600&family=IBM+Plex+Mono:wght@400;500&display=swap">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
{css}</style>

<div class="wrap">

<header>
  <p class="eyebrow">Four editions · the owner's order {order} · kb/Work PB5994</p>
  <h1>{title}</h1>
  <p class="asof">As of <b>{head_date}</b> · measured tree <b class="mono">{head_sha}</b></p>
  <p class="mission">An edition is COMPLETE only when <span class="mono">edition_status.py</span> measures all four of its parts at zero: the inventory GAP, the code reads owed, the open notes that touch it, and its normative text in no catalog row. A part it cannot measure keeps the edition NOT COMPLETE.</p>
</header>

<section aria-label="The editions">
  <h2>The editions</h2>
  {top}
  <div style="margin-top:12px">{verdicts}</div>
</section>

{editions}

<section aria-label="Work in flight on no edition">
  <h2>Work in flight on no edition</h2>
  {unattributed}
</section>

<section aria-label="Pacing">
  <h2>Pacing</h2>
  {pacing}
</section>

<footer>
  <p><strong>Every number on this page is computed.</strong> Each edition's state is <span class="mono">scripts/spec/edition_status.py</span>'s (the inventory, the code-read stores, <span class="mono">kb/Work</span> through <span class="mono">work.py</span>, the 1985 transcription and catalog); the work in flight is <span class="mono">prune_worktrees.py</span>'s branch survey, which reads GitHub's pull requests and merge queue; the quota is <span class="mono">budget.py</span>'s; the landings are origin/main's merges. Rendered by <span class="mono">scripts/spec/gen_ledger.py</span>. The work register remains <span class="mono">kb/Work/</span>; this page is a derived view, never a tracker.</p>
  <p class="mono">Snapshot: {head_date} · measured tree {head_sha}, the last commit that touched an input this page reports.</p>
</footer>

</div>
"""


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--out", type=pathlib.Path, default=DEFAULT_OUT, help=f"HTML to write (default {DEFAULT_OUT})")
    ap.add_argument("--check", action="store_true",
                    help="render in memory from the repository alone (no branch survey, no quota), write nothing")
    a = ap.parse_args(argv)
    led = measure(live=not a.check)
    text = render(led)
    if a.check:
        print(f"the ledger renders from the repo ({len(text):,} bytes)")
        return 0
    import account  # noqa: PLC0415
    import ledger_state  # noqa: PLC0415  (imports this module for STAMP_PATHS; loaded only here, after a render)
    try:
        stale = ledger_state.stale_checkout_line(REPO, ledger_state.state(REPO, COORD_DIR, account.current().name))
    except ledger_state.NoMain as err:
        stale = f"main's stamp not read ({err})"
    a.out.parent.mkdir(parents=True, exist_ok=True)
    a.out.write_text(text, encoding="utf-8", newline="\n")
    for ed in led.editions:
        print(ed.line)
    print(f"wrote  : {a.out} ({len(text):,} bytes)")
    print(ledger_state.publish_hint(COORD_DIR, account.current().name))
    if stale:
        print(f"⛔ NOT MAIN'S PAGE: {stale}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
