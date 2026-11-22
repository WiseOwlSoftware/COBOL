#!/usr/bin/env python3
"""THE EDITIONS A NOTE BLOCKS: the register's `editions:` field, its check, its seed and its recorded narrowing
(kb/Work PB5995).

    python scripts/spec/work.py backfill-editions [--write]          # seed the field on every non-terminal note lacking it
    python scripts/spec/work.py set-editions --from <file.json> [--write]   # a recorded judgment: the field and its reason

⭐ WHAT THE FIELD MEANS (the controller's ruling of wave 20262131, written once here as `MEANING` and quoted by every
refusal). `editions:` lists the editions whose COMPLETION the note blocks: a defect names the `--std` values at which
the wrong behaviour occurs (a 2002-only defect is `[2002]` even if its text cites the 1985 fix beside it); a tooling,
analysis or decision note names the editions whose inventory, catalog, reads or status it must change before that
edition can be called complete; `[]` is a note no edition's completion waits on. `edition_status.py` part 3 reads the
field and nothing else (`work.declared_editions`, the one reader), and so does the completion ledger.

⛔ WHY A DECLARED FIELD. Part 3 used to decide "touches" by regular expressions over the note's text. It counted a note
that mentions an edition in passing, which buried the notes that block it, and missed a note about an edition that
never spells it (kb/Work PB5995; CLAUDE.md rule 5).

THE CHECK (`problems`, run by `work.py check`):
  - a non-terminal note without the field is refused, with `MEANING` and the command that seeds it;
  - a value that is not a list of members of `work.EDITIONS`, or names one twice, is refused on any note;
  - THE DRIFT RULE: a non-terminal note whose text cites an edition's text (`§1985:`, `§2002:`, ..., an amendment's
    `§1989:` and `§1993:` counting for 1985: `citation.SOURCE_EDITION`) and does not declare that edition is refused,
    unless its newest recorded judgment (`RECORDED`, the line `set-editions` writes into an addendum) holds exactly the
    value the frontmatter declares: a narrowing is a recorded judgment with a reason, never a hand edit.

THE SEED (`seed`): what `backfill-editions` writes, and what a tool that files a note writes when its author states
nothing (`seeded`). It is today's text match (`SEED_PATTERNS`, the table `edition_status.py` used to measure with)
united with the editions the text cites, so a seeded note always passes the drift rule, and over-counting is the safe
direction: a reader and a refuter narrow it afterwards through `set-editions`. The seed is no measure: nothing reads
it but the two writers.

The line goes directly after the `tags:` entry (after its last line when the list wraps), far from `status:`, so a
branch that flips a status and a branch that writes the field merge with plain git.
"""
from __future__ import annotations

import datetime
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (the one citation format and its parser)
import work  # noqa: E402  (THE reader of the register)

FIELD = "editions"

MEANING = ("`editions:` lists the editions whose COMPLETION the note blocks: a defect names the `--std` values at which "
           "the wrong behaviour occurs; a tooling, analysis or decision note names the editions whose inventory, "
           "catalog, reads or status it must change before that edition can be called complete; `[]` is a note no "
           "edition's completion waits on (kb/Work PB5995)")

#: THE SEED's text match, one row per edition (formerly `edition_status.EDITION_PATTERNS`, the measure itself). A year
#: not inside a longer word (`PB2014` is a note id) names 1985, 2002 and 2014; 2023's year is the standard's own number
#: in nearly every note, so 2023 is named only by a citation, a `--std` spelling or a 2023 rule id. 1985 is also named
#: by its amendments' citations and the standard's number (`X3.23`).
SEED_PATTERNS: dict[int, tuple[str, ...]] = {
    1985: (r"(?<!\w)1985(?!\d)", r"\bstd[ =-]?85\b", r"§1989:", r"\bX3\.23"),
    2002: (r"(?<!\w)2002(?!\d)", r"\bstd[ =-]?(?:2002|02)\b"),
    2014: (r"(?<!\w)2014(?!\d)", r"\bstd[ =-]?(?:2014|14)\b"),
    2023: (r"§2023:", r"\bstd[ =-]?(?:2023|23)\b", r"(?<![\w-])(?:SR|GR|AR|RV|FMT|DOC)-[A-Z]?\d+(?:\.\d+)*-\d"),
}

#: The line `set-editions` writes into a note's addendum: the judgment it records. The newest one in the note's text is
#: the note's recorded value.
RECORDED = re.compile(r"^\*\*editions:\*\* \[(?P<eds>[^\]\n]*)\]", re.M)


def render_value(eds) -> str:
    """`[1985, 2002]`: the field's value in the editions' order."""
    return "[" + ", ".join(str(y) for y in work.EDITIONS if y in eds) + "]"


def text_of(it: dict) -> str:
    """The text of a note as the seed and the drift rule read it: its frontmatter (every value but the reader's own `_`
    keys) and its body with its addenda (`_body`, as `work.read_notes` folds them)."""
    return "\n".join(f"{k}: {v}" for k, v in it.items() if not k.startswith("_")) + "\n" + str(it.get("_body", ""))


def cited(it: dict) -> frozenset[int]:
    """The editions whose text the note cites in the edition form (`citation.scan`; an amendment's citation counts for
    the edition it amends, `citation.SOURCE_EDITION`). A malformed citation names no text and counts for none."""
    out = set()
    for c in citation.scan(text_of(it)):
        if c.error is None and c.edition is not None:
            y = citation.SOURCE_EDITION.get(c.edition, c.edition)
            if y in work.EDITIONS:
                out.add(y)
    return frozenset(out)


def seed(it: dict) -> frozenset[int]:
    """THE SEED (module docstring): the text match united with the citations."""
    text = text_of(it)
    matched = {y for y, pats in SEED_PATTERNS.items() if any(re.search(p, text, re.IGNORECASE) for p in pats)}
    return frozenset(matched) | cited(it)


def recorded(it: dict) -> frozenset[int] | None:
    """The value of the note's newest recorded judgment (`RECORDED`), or None when it has none (or one this reader cannot
    read, which is no judgment)."""
    found = RECORDED.findall(str(it.get("_body", "")))
    if not found:
        return None
    try:
        return frozenset(int(x) for x in work._split_list(found[-1]))
    except ValueError:
        return None


def problems(items: list[dict]) -> list[str]:
    """The field's refusals over the register (module docstring)."""
    bad: list[str] = []
    for it in items:
        name = it.get("_file", it.get("id", "?"))
        open_ = it.get("status") not in work.TERMINAL_STATUSES
        if FIELD not in it:
            if open_:
                bad.append(f"{name}: a {it.get('status')} note declares no `editions:` — {MEANING}; seed it with "
                           "`python scripts/spec/work.py backfill-editions --write`, then narrow it by a recorded "
                           "judgment (`work.py set-editions`)")
            continue
        v = it[FIELD]
        if not isinstance(v, list) or any(x not in {str(y) for y in work.EDITIONS} for x in v) or len(set(v)) != len(v):
            bad.append(f"{name}: editions is {v!r} — a list of distinct editions of {list(work.EDITIONS)}; {MEANING}")
            continue
        if not open_:
            continue
        declared = work.declared_editions(it)
        missing = cited(it) - declared
        if missing and recorded(it) != declared:
            bad.append(f"{name}: cites the text of {render_value(missing)} but declares editions: {render_value(declared)} "
                       f"— add the edition, or record why it is not blocked (`work.py set-editions` writes the value "
                       f"and its reason); {MEANING}")
    return bad


# -- writing the field ---------------------------------------------------------------------------------------------------

def _frontmatter_span(lines: list[str]) -> int | None:
    """The index of the closing `---` line of the frontmatter `lines` opens with, or None."""
    if not lines or lines[0].rstrip("\r\n") != "---":
        return None
    return next((i for i in range(1, len(lines)) if lines[i].rstrip("\r\n").rstrip() == "---"), None)


def _entry_end(lines: list[str], i: int, end: int) -> int:
    """The index of the last line of the frontmatter entry opening at `i` (a wrapped flow list, a block list)."""
    value = lines[i].partition(":")[2].strip()
    if value.startswith("[") and not value.endswith("]"):
        j = i
        while j + 1 < end and not lines[j].rstrip("\r\n").rstrip().endswith("]"):
            j += 1
        return j
    if not value:
        j = i
        while j + 1 < end and re.match(r"[ \t]+-", lines[j + 1]):
            j += 1
        return j
    return i


def with_field(text: str, eds) -> str:
    """`text` with its frontmatter's `editions:` set to `eds`: an existing entry is replaced in place, else the line goes
    directly after the `tags:` entry, else before the closing `---`. Refuses a text with no frontmatter."""
    lines = text.splitlines(keepends=True)
    end = _frontmatter_span(lines)
    if end is None:
        raise SystemExit("⛔ not a note: the text has no frontmatter block")
    nl = "\r\n" if lines[0].endswith("\r\n") else "\n"
    new = f"{FIELD}: {render_value(eds)}{nl}"
    key = re.compile(rf"\A{FIELD}\s*:")
    at = next((i for i in range(1, end) if key.match(lines[i])), None)
    if at is not None:
        return "".join(lines[:at] + [new] + lines[_entry_end(lines, at, end) + 1:])
    tags = next((i for i in range(1, end) if re.match(r"\Atags\s*:", lines[i])), None)
    at = _entry_end(lines, tags, end) + 1 if tags is not None else end
    return "".join(lines[:at] + [new] + lines[at:])


def seeded(text: str, eds=None) -> str:
    """A NEW note's text with its `editions:` written: `eds` when its author states it, else the note's seed. A text that
    declares the field already is returned as it is. The one function every tool that files a note calls."""
    it = work.parse_frontmatter(text)
    if it is None:
        raise SystemExit("⛔ not a note: the text has no frontmatter block")
    if FIELD in it and eds is None:
        return text
    return with_field(text, seed(it) if eds is None else eds)


def _counts(by: dict[str, frozenset[int]]) -> str:
    return " · ".join(f"{y} {sum(1 for e in by.values() if y in e)}" for y in work.EDITIONS) + \
        f" · none {sum(1 for e in by.values() if not e)}"


def backfill(work_dir: pathlib.Path, write: bool) -> int:
    """`backfill-editions`: the seed written on every non-terminal note that lacks the field; re-runnable (a note that
    has it is never touched). Without `write` it prints what it would write."""
    notes = [it for it in work.load(work_dir) if it.get("status") not in work.TERMINAL_STATUSES and FIELD not in it]
    todo = {it["id"]: seed(it) for it in notes}
    for it in notes if write else ():
        p = work_dir / it["_file"]
        p.write_bytes(with_field(p.read_bytes().decode("utf-8"), todo[it["id"]]).encode("utf-8"))
    verb = "wrote" if write else "would write"
    print(f"=== BACKFILL-EDITIONS: {verb} editions on {len(todo)} non-terminal note(s) lacking it — {_counts(todo)} ===")
    return 0


def _records(path: pathlib.Path, by_id: dict[str, dict]) -> tuple[list[dict], list[str]]:
    try:
        recs = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        return [], [f"{path}: unreadable ({e})"]
    if not isinstance(recs, list):
        return [], [f"{path}: a list of {{id, editions, reason}} records is expected"]
    bad, seen = [], set()
    for n, r in enumerate(recs):
        where = f"record {n} ({r.get('id') if isinstance(r, dict) else r!r})"
        if not isinstance(r, dict) or set(r) != {"id", "editions", "reason"}:
            bad.append(f"{where}: exactly the keys id, editions, reason")
            continue
        if r["id"] not in by_id:
            bad.append(f"{where}: no note {r['id']} in the register")
        if r["id"] in seen:
            bad.append(f"{where}: {r['id']} twice in one batch")
        seen.add(r["id"])
        eds = r["editions"]
        if not isinstance(eds, list) or any(type(y) is not int or y not in work.EDITIONS for y in eds) \
                or len(set(eds)) != len(eds):
            bad.append(f"{where}: editions {eds!r} is not a list of distinct editions of {list(work.EDITIONS)}")
        if not isinstance(r["reason"], str) or not r["reason"].strip() or "\n" in r["reason"].strip():
            bad.append(f"{where}: the reason is one non-empty sentence")
    return recs, bad


def addendum_path(work_dir: pathlib.Path, nid: str, day: str) -> pathlib.Path:
    """`<work_dir>/<id>/<day>-editions.md`, or `<day>-editions02.md` and on when that day already holds one.

    THE NAME ORDERS THE JUDGMENTS: a note's addenda fold in name order (`work.read_addenda`), and `recorded` takes the
    last judgment the folded body holds, so a later judgment of the same day must sort after every earlier one. A
    `-editions-2.md` sorted BEFORE `-editions.md` (`-` is below `.`), so a second narrowing read as the first one's value
    and the register refused the note it had just narrowed (wave 20262132 group c); a digit sorts after `.`, and two
    digits keep the suffixed ones in order."""
    d = work_dir / nid
    p, n = d / f"{day}-editions.md", 2
    while p.exists():
        p, n = d / f"{day}-editions{n:02d}.md", n + 1
    return p


def set_editions(work_dir: pathlib.Path, source: pathlib.Path, write: bool, day: str | None = None) -> int:
    """`set-editions`: each record's value written into its note's frontmatter, and its reason appended as an addendum
    (`kb/Work/<id>/<day>-editions.md`, rule 8: an addition after filing is a new file) carrying the `RECORDED` line the
    drift rule reads. All or nothing: one bad record refuses the batch and writes nothing."""
    by_id = {it["id"]: it for it in work.load(work_dir)}
    recs, bad = _records(source, by_id)
    if bad:
        print(f"⛔ set-editions refused {source}, nothing written:")
        for b in bad:
            print(f"      {b}")
        return 1
    day = day or datetime.date.today().isoformat()
    for r in recs:
        it, eds = by_id[r["id"]], frozenset(r["editions"])
        was = work.declared_editions(it)
        p = work_dir / it["_file"]
        add = addendum_path(work_dir, r["id"], day)
        print(f"{r['id']}: editions {render_value(was) if was is not None else '(none)'} -> {render_value(eds)}")
        if write:
            p.write_bytes(with_field(p.read_bytes().decode("utf-8"), eds).encode("utf-8"))
            add.parent.mkdir(exist_ok=True)
            add.write_bytes((f"## {day} - editions set to {render_value(eds)} (work.py set-editions)\n\n"
                             f"**editions:** {render_value(eds)} (was "
                             f"{render_value(was) if was is not None else 'undeclared'})\n\n{r['reason'].strip()}\n")
                            .encode("utf-8"))
    print(f"=== SET-EDITIONS: {'wrote' if write else 'would write'} {len(recs)} note(s) ===")
    return 0
