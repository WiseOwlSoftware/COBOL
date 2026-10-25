#!/usr/bin/env python3
"""THE WORK REGISTER — one note per work item under `kb/Work/`, and the only place work is authored.

⛔ WHY THIS EXISTS. "What is left to do" was authored in FIVE places, each in a different format, and three of
them declared themselves canonical:

    docs/COBOLNET_REARCHITECTURE_PLAN.md §0 NEXT   "the ONLY live-state SSOT"      buried in 3,614 lines
    docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md   owns its own LANDED tally       2,484 lines of prose
    kb/Remaining Work Tracker.md                   "the canonical remaining-work tracker"   5 days stale
    §11 analysis backlog                           17 analyses                     inside the plan
    the queue's RESIDUE bucket                     ~10 findings                    prose, never itemised

When three artifacts each claim to be the source of truth, none is — and the cost was measurable: the residue
findings include a WRONG-ANSWER defect (`EXCEPTION-STATEMENT` returns `GO` where Table 12 requires `GO TO`) that
no work list could see, because it lived inside a paragraph.

⭐ THE FORM IS ONE NOTE PER ITEM, TRACKED IN GIT, WITH FRONTMATTER. Not one big JSON: a note gives Obsidian a
graph node (link an item to its spec clause and its code-reference note), gives git a per-item history instead of
a 2,484-line diff, and gives Bases every view without a query language. The forensic prose — repro, citation, why
a previous summary was wrong — lives in the note BODY, which is where it already was. Nothing is discarded.

    python scripts/spec/work.py migrate    # ONE TIME: fold the five registers into kb/Work/
    python scripts/spec/work.py check      # validate frontmatter; non-zero on a bad/missing field
    python scripts/spec/work.py next       # the ranked work list (session-probe prints this)
    python scripts/spec/work.py next --cluster PB2108 [--json]   # one campaign's open notes, tier then blocked_by order
    python scripts/spec/work.py stats      # counts by kind/status/harm
    python scripts/spec/work.py --self-test   # the frontmatter reader against tests/version-matrix/work-frontmatter-cases.json

⭐ `inventory_rows:` — THE BACK-LINK TO THE P14 TRACEABILITY INVENTORY, and the register's newest field
(2026-08-31). A note lists the `rule-id`s it owns; that list is the SSOT for ownership, and the note's prose
"Rows:" line is narrative beside it. It exists because the two artifacts answering "what is left to do" had
NOTHING holding them together: of 138 inventory rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, **131 were
invisible to `next`** when it was first measured — ranked by nothing, owed by nobody, and counted by the
burn-down the whole time.

⛔ The field is checked against the inventory by `check` through `work_inventory.py` (this directory), which asserts
that every defective-verdict row is claimed by a note whose status is not terminal and that every claim names a row
the inventory has — so **flipping a note to `landed` without re-verdicting its rows turns the audit red**, which is
exactly the event that used to pass unnoticed. The predicate has to keep holding as the INVENTORY changes underneath a
note, which is why it is asked on every run of `check` (the gate's audit phase, CI's `audits` job) and not once at
landing. It was a Unit drift test until PB2878 (it read every note, which made `kb/` a test input).

⭐ `closes_rows:` — THE OTHER DIRECTION, and the register's newest field (owner decision 2026-09-19,
`kb/Work/PB245`). `inventory_rows` is what a note CLAIMS while it is open; `closes_rows` is what its landing
CLOSED, and it survives the landing. Without it, "which rows did this fix close" was answerable only by
re-measuring: fourteen §15 rows held the GAP open on mechanisms seven landings had already closed, and thirteen
closed CONFORMS the first time anyone looked. A landing therefore writes BOTH halves in the same change set —
the rows leave `inventory_rows` as they are re-verdicted, and they arrive in `closes_rows`. A landing that
closed no row says so with `closes_rows: []` **and** a `closes_rows_reason:`; silence is what the field
replaced. The spelling is `closes_rows`, not the decision's `closes-rows`: every multiword key in this
frontmatter is snake_case, and `kb/Work.base` addresses a property as `note.<key>`, where a hyphen would
at best need quoting and at worst read as an operator. Checked against the inventory by `work_inventory.py` (the
landed-note and the closed-row halves); the SHAPE half is :func:`closes_rows_shape` here.

⛔ AND THE READER ITSELF IS A GATE: `parse_frontmatter` is THE reader of the register — there is no second one — and
`--self-test` holds it to `tests/version-matrix/work-frontmatter-cases.json`, the shapes the register has contained, each
of which broke a reader once (`kb/Work/PB875`: a list WRAPPED across two lines was truncated here and discarded by the
C# twin that PB2878 deleted, and both called the note well-formed).
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import json
import pathlib
import re
import subprocess
import sys
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
QUEUE = REPO / "docs" / "rearchitecture" / "CONFORMANCE-FIX-QUEUE.md"
PLAN = REPO / "docs" / "COBOLNET_REARCHITECTURE_PLAN.md"
WORK = REPO / "kb" / "Work"

KINDS = {"defect", "analysis", "adjudication", "decision"}
STATUSES = {"open", "half", "landed", "owner", "blocked", "retired"}
#: The statuses that mean an item is DONE — the ONE vocabulary every reader of the register asks (kb/Work PB3196:
#: the R0 census and the note generator each read the status their own way, and the census's own copy held four
#: statuses that `check` rejects). A view of open work shall exclude every one of them — see
#: :func:`check_base_agrees`, which is what stops the next one added here from being half-applied. The reader's
#: self-test holds the fixture to it: :func:`reading_view` reports `terminal`, and :func:`self_test` refuses a fixture
#: that leaves any status of STATUSES without a case, so a status added here reaches the reader's answers.
TERMINAL_STATUSES = ("landed", "retired")
HARM = {"wrong-answer", "crashes", "silent", "rejects-legal-source", "under-rejects", "process"}

PB_HEAD = re.compile(
    r"^### (?P<id>PB\d+)\s*·\s*\[(?P<sev>[A-Z]+)\]\s*·\s*(?P<area>[^·]+?)\s*·\s*(?P<rest>.*)$", re.M)
FLAGS = re.compile(r"^>\s*⚙\s*\*\*flags:\*\*\s*(?P<flags>.+)$", re.M)
BLOCKED = re.compile(r"blocked-by:\s*(PB\d+)")
CLAUSE = re.compile(r"§(\d+(?:\.\d+)*)")
A_ROW = re.compile(r"^\|\s*(?P<id>A\d+[a-z]?)\s*\|\s*(?P<title>.+?)\s*\|(?P<rest>.*)\|\s*$", re.M)


def status_of(rest: str) -> str:
    """⛔ MATCH THE PHRASE "HALF LANDED", NOT ⚠ OR THE BARE WORD "HALF", AND TEST IT FIRST. A first cut read
    ``"HALF" in rest or "⚠" in rest`` and misfiled THREE COMPLETED items (PB1, PB14, PB32) as still open, because
    a landed entry routinely narrates the half it closed — "LANDED 2026-08-03 (the CS1503 half; the wrong-VALUE
    half is now PB38)". Putting finished work back on a work list is the one failure a work list must not have."""
    up = rest.upper()
    if "HALF LANDED" in up:
        return "half"
    if "⚖" in rest or "NEEDS-OWNER-DECISION" in up:
        return "owner"
    if "✅" in rest or "LANDED" in up or "RETIRED" in up:
        return "landed"
    return "open"


def fm_line(k: str, v) -> str:
    if isinstance(v, bool):
        return f"{k}: {str(v).lower()}"
    if isinstance(v, list):
        return f"{k}: [{', '.join(str(x) for x in v)}]"
    return f'{k}: "{v}"' if isinstance(v, str) and (":" in v or v.strip() != v) else f"{k}: {v}"


def write_note(item: dict, body: str) -> str:
    keys = ["id", "kind", "status", "severity", "area", "wrong_answer", "crashes", "silent",
            "rejects_legal_source", "under_rejects", "process_only", "blocked", "blocked_by", "spec_refs"]
    fm = ["---", f'title: "{item["id"]} — {item["summary"][:70]}"']
    fm += [fm_line(k, item[k]) for k in keys]
    fm += ["tags: [cobolsharp, work, " + item["kind"] + "]", "---", "",
           f'# {item["id"]} — {item["summary"]}', ""]
    return "\n".join(fm) + body.rstrip() + "\n"


def harm_fields(flags: list[str]) -> dict:
    return {
        "wrong_answer": "wrong-answer" in flags, "crashes": "crashes" in flags,
        "silent": "silent" in flags, "rejects_legal_source": "rejects-legal-source" in flags,
        "under_rejects": "under-rejects" in flags, "process_only": "process" in flags,
    }


def migrate() -> int:
    text = QUEUE.read_text(encoding="utf-8")
    WORK.mkdir(parents=True, exist_ok=True)
    written = 0

    # ── 1. The 39 PB defects. The BODY IS COPIED VERBATIM — this migration must not paraphrase evidence. ──────
    ms = list(PB_HEAD.finditer(text))
    for i, m in enumerate(ms):
        body = text[m.end(): ms[i + 1].start() if i + 1 < len(ms) else len(text)]
        # A trailing section heading that is not a PB entry (the residue block) belongs to nobody.
        body = re.split(r"^### ⚠ RESIDUE", body, flags=re.M)[0]
        fmm = FLAGS.search(body)
        flags = [f.strip() for f in fmm.group("flags").split("·")] if fmm else []
        flags = [f for f in flags if not f.startswith("blocked-by")]
        rest = m.group("rest")
        item = {
            "id": m.group("id"), "kind": "defect", "status": status_of(rest),
            "severity": m.group("sev"), "area": m.group("area").strip(),
            "summary": re.sub(r"[⛔✅⚠⚖◑]", "", rest).strip(" —-"),
            "blocked_by": sorted(set(BLOCKED.findall(body))),
            "spec_refs": sorted({c for c in CLAUSE.findall(body)})[:12],
            **harm_fields(flags),
        }
        item["blocked"] = bool(item["blocked_by"])
        (WORK / f'{item["id"]}.md').write_text(write_note(item, "\n" + body.strip() + "\n"), encoding="utf-8", newline="\n")
        written += 1

    # ── 2. The 17 §11 analyses. ───────────────────────────────────────────────────────────────────────────────
    plan = PLAN.read_text(encoding="utf-8")
    for m in A_ROW.finditer(plan):
        cells = [c.strip() for c in m.group("rest").split("|")]
        title = re.sub(r"\*\*", "", m.group("title"))
        summary = title.split("—")[0].strip()[:110]
        st = "landed" if re.search(r"CLOSED|✅ *DONE", " ".join(cells), re.I) else \
             "blocked" if re.search(r"BLOCKED", " ".join(cells), re.I) else "open"
        item = {
            "id": m.group("id"), "kind": "analysis", "status": st, "severity": "MAJOR",
            "area": "analysis", "summary": summary, "blocked_by": [], "blocked": st == "blocked",
            "spec_refs": sorted({c for c in CLAUSE.findall(m.group(0))})[:6],
            **harm_fields(["process"]),
        }
        body = "\n" + "\n".join(f"- {c}" for c in [title] + cells if c) + "\n"
        (WORK / f'{item["id"]}.md').write_text(write_note(item, body), encoding="utf-8", newline="\n")
        written += 1

    # ── 3. The residue findings, finally itemised. ────────────────────────────────────────────────────────────
    # ⚠ THE HEADING SAYS 16 AND THE PROSE ENUMERATES FEWER. That discrepancy is preserved, not papered over: the
    # items that exist become notes, and R00 records that the count and the enumeration disagree, so nobody later
    # reads "16" as a list they can work.
    rm = re.search(r"^### ⚠ RESIDUE — (?P<n>\d+) findings.*?\n(?P<body>(?:^>.*\n)+)", text, re.M)
    if rm:
        blob = " ".join(l.lstrip("> ").rstrip() for l in rm.group("body").splitlines())
        blob = blob.split("See the evidence ledger")[0]
        parts = [p.strip(" .·") for p in blob.split("·")]
        parts = [p for p in parts if len(p) > 25]
        for n, p in enumerate(parts, 1):
            wrong = bool(re.search(r"returns .* where|truncat|mis-fold|degrades SILENTLY|contaminates", p, re.I))
            rej = bool(re.search(r"refused|rejects|no lexer rule", p, re.I))
            crash = bool(re.search(r"throws at run time", p, re.I))
            flags = [f for f, on in (("wrong-answer", wrong), ("rejects-legal-source", rej),
                                     ("crashes", crash), ("silent", "silent" in p.lower()))]
            flags = [f for f, on in (("wrong-answer", wrong), ("rejects-legal-source", rej),
                                     ("crashes", crash), ("silent", "silent" in p.lower())) if on]
            item = {
                "id": f"R{n:02d}", "kind": "defect", "status": "open", "severity": "MINOR",
                "area": "residue", "summary": re.sub(r"\s+", " ", p)[:110],
                "blocked_by": [], "blocked": False,
                "spec_refs": sorted({c for c in CLAUSE.findall(p)})[:6], **harm_fields(flags),
            }
            (WORK / f'{item["id"]}.md').write_text(
                write_note(item, f"\n{p}\n\n> Migrated from the fix queue's RESIDUE block, which never itemised "
                                 f"these — so no work list could see them.\n"), encoding="utf-8", newline="\n")
            written += 1
        declared = int(rm.group("n"))
        if declared != len(parts):
            item = {"id": "R00", "kind": "adjudication", "status": "open", "severity": "MINOR",
                    "area": "residue", "blocked_by": [], "blocked": False, "spec_refs": [],
                    "summary": f"the residue block declares {declared} findings but enumerates {len(parts)}",
                    **harm_fields(["process"])}
            (WORK / "R00.md").write_text(write_note(item,
                f"\nThe fix queue's residue heading says **{declared} findings**; the prose beneath it enumerates "
                f"**{len(parts)}**. The missing {declared - len(parts)} were never written down, so they cannot be "
                f"worked and cannot be verified as already fixed. Reconcile against the batch-4 evidence ledger "
                f"(`docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md`) and either itemise or retire "
                f"the count.\n"), encoding="utf-8", newline="\n")
            written += 1
    print(f"migrated {written} work items into {WORK.relative_to(REPO)}")
    return 0


#: The frontmatter block: everything between the opening `---` line and the closing one. The register is LF
#: (kb/Work PB3029); `\r?\n` because a reader that hard-codes LF would answer "this is not a note" for a note a
#: Windows tool wrote CRLF — SILENTLY — where the line-endings audit (scripts/line_endings.py) names it.
FRONT = re.compile(r"\A---\r?\n(?P<body>.*?)\r?\n---[ \t]*(?:\r?\n|\Z)", re.S)
#: A frontmatter key. Anything else on a line — a colon inside a title, a wrapped list's continuation — is not
#: one. A VALUE is a list when it opens with `[`, whatever its key: list-ness is a property of the value, so a
#: field added to the register tomorrow needs no edit here (and no second copy of a key vocabulary in C#).
FM_KEY = re.compile(r"\A[a-z_]+\Z")


def _split_list(inner: str) -> list[str]:
    """`"A", B , ` → `['A', 'B']` — the ONE list-member grammar, quoted or bare."""
    return [m for m in (x.strip().strip('"').strip("'").strip() for x in inner.split(",")) if m]


def parse_frontmatter(text: str) -> dict | None:
    """One note's frontmatter as `key -> str | bool | list[str]`, or `None` when the file is not a note.

    ⛔ THIS IS THE ONE READER OF THE REGISTER, and `tests/version-matrix/work-frontmatter-cases.json` is its test
    (`--self-test`). It had a C# twin (`tests/_shared/WorkRegister.cs`, deleted by PB2878) and the two disagreed:
    `kb/Work/PB875` is the note recording what that cost: a list WRAPPED across two lines — the normal YAML shape, and
    the shape `kb/Work/PB205` had carried for months — was read here as a truncated list and in C# as NO list at
    all, so a note went on claiming four inventory rows in the register while the gate that enforces claims saw
    none, and `work.py check` called the same note well-formed.

    ⛔ AND IT FAILED **OPEN**. Both halves answered a malformed value with emptiness, which reads exactly like a
    note that claims nothing — the one answer a register must never invent. An unterminated list is now an ERROR
    CODE in `_errors` (the key is left ABSENT, never silently empty), and the codes are what the fixture compares:
    two malformations look identical under "it was rejected".

    ⛔ A RUNAWAY IS BOUNDED BY THE NEXT KEY, and that is measured, not assumed. A list left open by a missing
    bracket used to keep eating lines until some later line happened to end in `]` — so `tags: [cobolsharp,
    work, defect]` silently became three of `inventory_rows`' members and the malformation was never reported.
    A line that begins a new frontmatter key ends the open list with the error instead; no continuation of a
    real wrapped list can look like one, because its members are rule-ids, clause numbers and tag words.
    """
    m = FRONT.match(text)
    if m is None:
        return None
    out: dict = {"_errors": []}
    key: str | None = None        # the list key whose value is still being accumulated across lines
    buf = ""
    for raw in m.group("body").splitlines():
        line = raw.strip()
        if key is not None:
            head, sep, _ = line.partition(":")
            if sep and FM_KEY.match(head.strip()):
                out["_errors"].append(f"unterminated-list:{key}")
                key, buf = None, ""
            else:
                buf += " " + line
                if buf.endswith("]"):
                    out[key] = _split_list(buf[buf.find("[") + 1:-1])
                    key, buf = None, ""
                continue
        k, sep, v = line.partition(":")
        k, v = k.strip(), v.strip()
        if not sep or not FM_KEY.match(k):
            continue              # a continuation of a wrapped SCALAR, a comment, a blank line
        if not v.startswith("["):
            out[k] = True if v == "true" else False if v == "false" else v.strip('"')
        elif v.endswith("]"):
            out[k] = _split_list(v[1:-1])
        else:
            key, buf = k, v
    if key is not None:
        out["_errors"].append(f"unterminated-list:{key}")
    # The note BODY, so check() can catch the status written a second time in the H1 heading.
    out["_body"] = text[m.end():]
    return out


#: The file names of a register entry: `PB<n>.md`, `A<n>.md`, `R<n>.md` (a trailing letter for a split id). A file under
#: `kb/Work/` with a name like this IS a note; anything else (an index, a README) is not and stays skipped.
NOTE_FILE = re.compile(r"\A(?:PB|A|R)\d+[a-z]?\.md\Z")


def unreadable_why(text: str) -> str:
    """Why `text` has no frontmatter block (FRONT does not match), naming the first bad line: the file opens with a
    byte-order mark, or its first line is not the opening `---`, or the block is never closed by a `---` line."""
    if text.startswith("﻿"):
        return "the file starts with a byte-order mark, so line 1 is not the opening `---`"
    first = text.split(chr(10), 1)[0].rstrip(chr(13))
    if first != "---":
        return f"line 1 is {first[:60]!r}, not the opening `---`"
    return "the frontmatter is never closed by a `---` line"


def read_note(name: str, text: str) -> tuple[dict | None, str]:
    """`(the note, "")` for a file whose frontmatter parses; `(None, "")` for a file that is not named like a note (an
    index, a README: skipped); `(None, why)` for a file NAMED like a note whose frontmatter does not parse (kb/Work
    PB3654). That last answer is a refusal and never a skip: a damaged note that leaves the register is invisible to
    `next`, to the planner's `**Sites:**` reader and to the inventory claim gate, and `check` used to call the register
    "all well-formed" over it (PR #513 merged three such notes, every line prefixed with a backtick)."""
    d = parse_frontmatter(text)
    if d is not None:
        d["_file"] = name
        return d, ""
    if NOTE_FILE.match(name):
        return None, f"{name}: unreadable as a work note — {unreadable_why(text)}"
    return None, ""


def read_notes(work_dir: pathlib.Path = WORK) -> tuple[list[dict], list[str]]:
    """Every note under `work_dir` and a refusal for every file named like a note that is not one (read_note)."""
    items, refused = [], []
    for p in sorted(work_dir.glob("*.md")):
        note, why = read_note(p.name, p.read_text(encoding="utf-8"))
        if note is not None:
            items.append(note)
        elif why:
            refused.append(why)
    return items, refused


def load(work_dir: pathlib.Path = WORK) -> list[dict]:
    """Every note under `work_dir` (the register; a test passes a fixture directory), or `SystemExit` naming each file
    named like a note that cannot be read: a register missing a note answers `next` wrongly and says nothing, so no
    reader of it runs on one (kb/Work PB3654). `check` reports the same files without stopping (read_notes)."""
    items, refused = read_notes(work_dir)
    if refused:
        raise SystemExit("⛔ the work register has " + str(len(refused)) + " unreadable note(s):\n      "
                         + "\n      ".join(refused) + "\n   repair the frontmatter (python scripts/spec/work.py check)")
    return items


# The `# PB45 — LANDED — …` heading writes the status a SECOND time, beside the frontmatter. That is one rule in
# two places, and it drifted in FIVE notes before anyone looked (PB26/PB36/PB41/PB43/PB45 all read OPEN while
# their frontmatter said landed) — because flipping `status:` is what `work.py next` reads, so nothing ever
# contradicted the heading. A reader opening the note sees the heading first.
HEADING_STATUS = re.compile(r"^#\s+\S+\s+—\s+(?P<status>[A-Z][A-Z\- ]*?)\s+—", re.M)
# Heading spellings that mean the same thing as a frontmatter status.
HEADING_ALIASES = {"closed": "landed", "fixed": "landed", "done": "landed"}


def check_base_agrees() -> list[str]:
    """`kb/Work.base`'s **Fix next** filter shall select on the same harm flags as :data:`HARM_FLAGS`.

    ⛔ THE PREDICATE IS WRITTEN TWICE AND CANNOT BE WRITTEN ONCE — `work.py` is Python and the Bases view is a
    YAML expression Obsidian evaluates, with no import between them. Both copies read `wrong_answer or crashes`
    and both were wrong the same way, which is what hid nine open items; CLAUDE.md advertises the two as "the
    same list", so a reader has no way to notice when they stop being it. Since the copies must exist, this
    holds them together.
    """
    base = REPO / "kb" / "Work.base"
    if not base.exists():
        return [f"{base.relative_to(REPO)} is missing — the Fix next view is half the register's UI"]
    text = base.read_text(encoding="utf-8")
    m = re.search(r"name:\s*Fix next\s*\n\s*filters:\s*\n(?P<body>(?:\s{6,}.*\n)+)", text)
    if m is None:
        return ["kb/Work.base: no 'Fix next' view with a filters block — work.py check cannot verify it "
                "agrees with HARM_FLAGS, and CLAUDE.md tells readers the two are the same list"]
    # The one filter line that ORs the harm flags together.
    line = next((l for l in m.group("body").splitlines() if " or " in l and "wrong_answer" in l), "")
    named = set(re.findall(r"[a-z_]+", line)) & set(HARM_FLAGS)
    missing = [f for f in HARM_FLAGS if f not in named]
    out = [] if not missing else [
        f"kb/Work.base 'Fix next' does not select on {missing} — an item whose only harm flag is one of those "
        f"is invisible in the view while work.py next ranks it. Update the filter to match HARM_FLAGS."]

    # ⛔ THE SAME DUPLICATION ONE PROPERTY OVER. Every view of OPEN work spells the terminal statuses out one by
    # one, and `retired` was added to STATUSES after those views were written — so the first retired note went on
    # showing in "Open but no harm flag set" as though it were open, while `work.py next` (status in open/half)
    # correctly ignored it. Two readers of one register, disagreeing about what DONE means, is exactly the shape
    # `check_base_agrees` exists for. The invariant is written so the NEXT terminal status is automatic rather
    # than remembered: a view that filters out ANY terminal status filters out ALL of them. A view that does not
    # filter on status at all (`Everything`, `Analyses`) is not a view of open work and is left alone.
    for v in re.finditer(r"^    name:\s*(?P<name>.+?)\s*\n\s*filters:\s*\n\s*and:\s*\n(?P<body>(?:\s{6,}-.*\n)+)",
                         text, re.M):
        body = v.group("body")
        if not any(f'status != "{s}"' in body for s in TERMINAL_STATUSES):
            continue
        absent = [s for s in TERMINAL_STATUSES if f'status != "{s}"' not in body]
        if absent:
            out.append(
                f"kb/Work.base view {v.group('name')!r} hides some terminal statuses but not {absent} — an item "
                f"in one of those states still shows there as open work, while work.py counts it done. Add "
                f"`- status != \"<status>\"` for each of {list(TERMINAL_STATUSES)}.")
    return out


#: A traceability-inventory rule-id, by SHAPE only — `AR-15.7.3-1`, `SR-13.18.40.3-22`, `FMT-14.9.32.2`,
#: `GR-9.3.6-L4.1`. Whether the id names a row that EXISTS, and whether that row's verdict resolves, is
#: `work_inventory.py`'s question (see :func:`closes_rows_shape`).
RULE_ID_SHAPE = re.compile(r"\A[A-Z]{2,4}-[0-9A-Za-z.]+(?:-[0-9A-Za-z.]+)?\Z")


def closes_rows_shape(it: dict) -> list[str]:
    """The `closes_rows` / `closes_rows_reason` back-link, checked for SHAPE — and only for shape.

    ⭐ THE FIELD (owner decision 2026-09-19, `kb/Work/PB245`). `inventory_rows` is a note's CLAIM while it is
    open; `closes_rows` is what its landing CLOSED, and it is the back-link the register never had. Without it
    "which rows did this fix close" was answerable only by re-measuring, and fourteen §15 rows held the GAP open
    on mechanisms seven landings had already closed — thirteen of them closed CONFORMS on first re-measurement.

    ⛔ WHAT IS CHECKED HERE, AND WHAT IS NOT — the same division `record_verdicts.py` draws, for the same reason.
    This validates what is decidable from the NOTE alone: the value is a list, each member is SPELLED like a
    rule-id, and a reason that is present is not empty. Whether a named row exists, whether its verdict resolves,
    and whether a landed defect note said anything at all belong to `work_inventory.py` (which :func:`check` also
    runs): those predicates need the INVENTORY, and asking them here as well would be one rule in two places
    (`feedback_one_rule_one_place`).
    """
    bad: list[str] = []
    for e in it.get("_errors", []):
        bad.append(f'{it["_file"]}: frontmatter {e} — a wrapped list whose bracket never closes is read as '
                   f'NOTHING by both readers of this register (kb/Work/PB875); close it, or put it on one line')
    rows = it.get("closes_rows")
    if rows is not None and not isinstance(rows, list):
        bad.append(f'{it["_file"]}: closes_rows is {rows!r}, not a list — write `closes_rows: [RULE-ID, …]`')
    elif rows:
        for r in rows:
            if not RULE_ID_SHAPE.match(r):
                bad.append(f'{it["_file"]}: closes_rows names {r!r}, which is not spelled like an inventory '
                           f'rule-id (e.g. AR-15.7.3-1) — it is a row id, never a note id')
    reason = it.get("closes_rows_reason")
    if reason is not None and (not isinstance(reason, str) or not reason.strip()):
        bad.append(f'{it["_file"]}: closes_rows_reason is present but empty — a landing that closed no '
                   f'inventory row says WHY in it, and an empty value is the silence the field replaced')
    return bad


def register_problems(work_dir: pathlib.Path = WORK) -> tuple[list[dict], list[str]]:
    """`(the notes under work_dir, every problem of the register)`: the unreadable notes (read_notes), each note's own
    schema, the topology and the site blocks, duplicate ids and, for the repository's OWN register only, the audits
    that compare it with the rest of the tree (the inventory, the R3 program, the Bases view): a fixture directory
    (`--work`, the self-test) has none of those to be compared with (kb/Work PB3654)."""
    items, bad = read_notes(work_dir)
    for it in items:
        for f in ("id", "kind", "status", "area"):
            if not it.get(f):
                bad.append(f'{it["_file"]}: missing {f}')
        if it.get("kind") not in KINDS:
            bad.append(f'{it["_file"]}: kind {it.get("kind")!r} not in {sorted(KINDS)}')
        if it.get("status") not in STATUSES:
            bad.append(f'{it["_file"]}: status {it.get("status")!r} not in {sorted(STATUSES)}')
        # The H1's status word, when it carries one, shall agree with the frontmatter.
        if (h := HEADING_STATUS.search(it.get("_body", ""))) is not None:
            spelled = h.group("status").strip().lower()
            meant = HEADING_ALIASES.get(spelled, spelled)
            if meant in STATUSES and meant != it.get("status"):
                bad.append(f'{it["_file"]}: heading says {h.group("status").strip()!r} but '
                           f'status is {it.get("status")!r} — a landed note that still reads OPEN is how the '
                           f'register lies to a reader (the frontmatter is what work.py next reads)')
        # ⛔ AN OPEN DEFECT WITH NO HARM FLAG IS INVISIBLE TO `next`, AND SILENCE IS HOW IT STAYS THAT WAY.
        # `actionable` selects on HARM_FLAGS, so a defect that sets none of them is filed and then never
        # ranked — the same disappearance the two-flag predicate used to cause for every false-reject. An item
        # that genuinely does nothing to a user's program says so with `process_only: true`.
        if (it.get("kind") == "defect" and it.get("status") in ("open", "half")
                and not it.get("process_only") and not any(it.get(f) for f in HARM_FLAGS)):
            bad.append(f'{it["_file"]}: open defect with no harm flag set — it can never appear in '
                       f'`work.py next`. Set one of {list(HARM_FLAGS)}, or process_only: true if it '
                       f'genuinely does nothing to a user\'s program.')
        bad += closes_rows_shape(it)
    bad += topology_problems(items)
    bad += sites_problems(items)
    ids = [it.get("id") for it in items]
    for i in set(ids):
        if ids.count(i) > 1:
            bad.append(f"duplicate id {i}")
    if work_dir.resolve() == WORK.resolve():
        import work_inventory  # noqa: PLC0415  (this directory's)

        bad += work_inventory.live_problems(items, TERMINAL_STATUSES)
        bad += r3_program_problems(items)
        bad += check_base_agrees()
    return items, bad


def check(work_dir: pathlib.Path = WORK) -> int:
    if not work_dir.exists():
        print(f"⛔ {work_dir} does not exist — run: python scripts/spec/work.py migrate")
        return 1
    items, bad = register_problems(work_dir)
    if bad:
        print(f"⛔ {len(bad)} problem(s) in the work register:")
        for b in bad[:20]:
            print(f"      {b}")
        return 1
    print(f"✓ {len(items)} work items, all well-formed")
    return 0


#: The harm flags that make an open item ACTIONABLE — what the defect DOES to a user's program.
#:
#: ⛔ THIS SET USED TO BE `wrong_answer or crashes`, AND THE REGISTER THAT EXISTS TO ANSWER "WHAT DO I DO NOW"
#: COULD NOT SEE A DEFECT THAT REJECTS LEGAL SOURCE. Nine open items were hidden by that predicate when it was
#: measured (2026-08-05) — four of them flagged `rejects_legal_source`, which CLAUDE.md rule 4 calls the one
#: outcome forbidden outright, and five `under_rejects`. PB51 (`COMPUTE WS-N = ZERO` refused as a boolean
#: Format-2 ALL literal) is the case that exposed it: a false REJECT crashes nothing and computes nothing wrong,
#: it simply stops the user's program compiling, so the two-flag predicate scored it as no harm at all.
#:
#: ⚠ `silent` is deliberately ABSENT and is not an omission: it QUALIFIES a wrong answer (silently wrong vs.
#: loudly wrong) rather than naming a harm of its own, so an item flagged silent alone would be one whose
#: frontmatter has not said what it actually does. `process_only` is likewise not here — it marks an item that
#: does nothing to a user's program by definition.
HARM_FLAGS = ("wrong_answer", "crashes", "rejects_legal_source", "under_rejects")


def actionable(items: list[dict]) -> list[dict]:
    sev = {"BLOCKER": 0, "MAJOR": 1, "MINOR": 2, "OWNER": 3}
    live = [i for i in items if i.get("status") in ("open", "half")
            and any(i.get(f) for f in HARM_FLAGS)
            and not i.get("process_only") and not i.get("blocked")]
    return sorted(live, key=lambda i: (sev.get(i.get("severity"), 9), i.get("id", "")))


# ── clusters and the blocked_by topology (kb/Work PB2120) ─────────────────────────────────────────────────────
#
# ⭐ A NAMED CLUSTER IS A PLANNABLE UNIT. `actionable` answers "what harms a user's program"; a campaign (the legacy
# retirement, the architecture review) is `process_only` work that ranks nowhere there, so it was dispatched by
# hand. `cluster:` names the campaign a note belongs to (usually its lead note's id, which may list itself) and
# `blocked_by:` orders the campaign. `cluster_order` is the ONE reader of both: `work.py next --cluster`,
# `plan_wave.py --cluster` and `next_unit.py` all call it, so "ready" means one thing everywhere.
#
# ⛔ `blocked:` IS A SECOND SPELLING OF `blocked_by:`, AND THEY DRIFTED. The fix lane skips a note whose flag is
# true (`.agent-fleet.json` skip_flag), so a flag left true after its blocker landed HIDES a live defect from every
# work list; two were found that way when this was written (PB1940 after PB2087 landed, PB1956 after PB322). So
# `check` holds the flag to the list: when `blocked_by` names notes, `blocked` is true exactly while one of them is
# still open. A flag with an empty list stays legal: it is a note blocked on something that is not a note.
#
# ⭐ `tier:` IS THE OWNER'S ORDER INSIDE A CAMPAIGN (kb/Work PB3423). `blocked_by` says what CANNOT start before
# something else; a tier says what SHOULD start first among notes that are all ready (PB3420: the owner's ten known
# SORT and file I/O defects before the rest of cluster PB3353, which the planner had ranked by inventory rows and harm
# alone). It is a list of `CAMPAIGN:N` entries, `tier: ["PB3353:1"]`, N a positive integer, 1 first, each naming a
# campaign the note's `cluster:` lists: SCOPED, because a note belongs to several campaigns (PB1278 to three) and the
# owner's order for one says nothing about another. In a campaign a note has no tier for, it ranks after every tiered
# note. `cluster_order` reads a note's EFFECTIVE tier, the lowest of its own and of every open cluster member it blocks
# (a tier-1 note's blocker is tier 1 too), so ranking by tier and then depth is still a blocked_by order, and
# `plan_wave.campaign_clusters` lifts its clusters the same way, so `work.py next --cluster` and the planner read one
# order. A held note (`status: owner`, a `blocked_by` hold) is still held: a tier ranks the READY notes, nothing more.
TIER = re.compile(r"\A(?P<campaign>[^:\s]+):(?P<tier>[1-9][0-9]*)\Z")


def tier_entries(it: dict) -> tuple[dict[str, int], list[str]]:
    """The note's `tier:` as `({campaign: tier}, [every entry that is not CAMPAIGN:N, or names a campaign twice])`;
    `check` reports the second list, and the first holds only the well-formed entries."""
    v = it.get("tier", [])
    if not isinstance(v, list):
        return {}, [repr(v)]
    good: dict[str, int] = {}
    bad: list[str] = []
    for e in v:
        if (m := TIER.match(e)) is None or m["campaign"] in good:
            bad.append(e)
        else:
            good[m["campaign"]] = int(m["tier"])
    return good, bad


def tier_of(it: dict, campaign: str) -> int | None:
    """The note's own tier in `campaign`, or None when it has none there."""
    return tier_entries(it)[0].get(campaign)


def tier_rank(tier: int | None) -> float:
    """Sort key: tier 1 first, an untiered note after every tiered one."""
    return float("inf") if tier is None else tier


def id_order(nid: str) -> tuple[int, str]:
    """Sort key: a note id by its number (PB98 before PB120), a name without one last."""
    m = re.search(r"\d+", nid or "")
    return (int(m.group()) if m else 1 << 30, nid or "")


def open_blockers(it: dict, by_id: dict[str, dict]) -> list[str]:
    """The note's `blocked_by` entries that are not terminal (an id the register lacks counts as open)."""
    return [b for b in (it.get("blocked_by") or [])
            if by_id.get(b, {}).get("status") not in TERMINAL_STATUSES]


def cluster_members(items: list[dict], lead: str) -> list[dict]:
    """Every note whose `cluster` list names `lead`, terminal or not — THE membership rule, so `cluster_order` and the
    ledger's lanes (`ledger_plan.py`: the campaign and held clusters of WHAT REMAINS) count one population. A list only: a scalar `cluster: PB2108` would
    match by SUBSTRING (`check` reports that shape)."""
    return [i for i in items if isinstance(i.get("cluster"), list) and lead in i["cluster"]]


def cluster_order(items: list[dict], lead: str) -> dict:
    """The cluster `lead` names: `{"cluster", "named", "notes": [...]}`, `notes` being every NON-TERMINAL note whose
    `cluster` list holds `lead`, whatever its kind, harm flags or `process_only`, in tier order and, inside a tier,
    in `blocked_by` topological order (together still a topological order: a blocker's tier is never later).

    Each entry is `{"id", "kind", "status", "area", "title", "tier", "depth", "ready", "waiting_on",
    "blockers_in_cluster"}`: `tier` is the note's EFFECTIVE tier in `lead`, the lowest of its own (`tier_of`) and of
    every open cluster member it blocks, or `None` (ranked after every tiered note); `depth` is 0 for a note no open cluster member
    blocks and one past its deepest open in-cluster blocker otherwise (`None` inside a cycle); `waiting_on` lists every
    open blocker, in the cluster or not, plus the reason a note is held without one (`status: owner`, `status:
    blocked`, or `blocked: true` with no `blocked_by`); `ready` is `waiting_on == []`. Ties sort by note number.
    `named` counts every note that names the cluster, landed or not, so a caller can tell a LANDED cluster (named > 0,
    notes empty) from a misspelled one (named == 0)."""
    by_id = {i.get("id"): i for i in items}
    named = cluster_members(items, lead)
    members = {i["id"]: i for i in named if i.get("status") not in TERMINAL_STATUSES}
    depth: dict[str, int | None] = {}

    def depth_of(nid: str, on_path: set[str]) -> int | None:
        """None exactly when the note reaches a cycle, which does not depend on the path taken, so it memoizes."""
        if nid in depth:
            return depth[nid]
        if nid in on_path:
            return None
        on_path.add(nid)
        ds = [depth_of(b, on_path) for b in open_blockers(members[nid], by_id) if b in members]
        on_path.discard(nid)
        depth[nid] = None if None in ds else (1 + max(ds) if ds else 0)
        return depth[nid]

    # The effective tier: a blocker ranks no later than anything it blocks. A pass only ever lowers a tier, so the
    # loop ends, a cycle included.
    tier = {nid: tier_of(it, lead) for nid, it in members.items()}
    lowered = True
    while lowered:
        lowered = False
        for nid, it in members.items():
            for b in open_blockers(it, by_id):
                if b in members and tier_rank(tier[nid]) < tier_rank(tier[b]):
                    tier[b], lowered = tier[nid], True

    rows = []
    for nid, it in members.items():
        waiting = open_blockers(it, by_id)
        if it.get("status") in ("owner", "blocked"):
            waiting.append(f"status: {it['status']}")
        elif it.get("blocked") and not it.get("blocked_by"):
            waiting.append("blocked: true (no blocked_by note)")
        rows.append({"id": nid, "kind": it.get("kind", ""), "status": it.get("status", ""), "area": it.get("area", ""),
                     "title": str(it.get("title", "")), "tier": tier[nid], "depth": depth_of(nid, set()),
                     "ready": not waiting, "waiting_on": waiting,
                     "blockers_in_cluster": [b for b in waiting if b in members]})
    rows.sort(key=lambda r: (r["depth"] is None, tier_rank(r["tier"]), r["depth"] or 0, id_order(r["id"])))
    return {"cluster": lead, "named": len(named), "notes": rows}


def topology_problems(items: list[dict]) -> list[str]:
    """`blocked_by` names notes that exist, `blocked` agrees with it, open notes form no blocking cycle, `cluster`
    is a list of names (a lead note's id, or a campaign name such as `user-documentation`), and `tier` is a list of
    `CAMPAIGN:N` entries, each naming a campaign of the note's own `cluster` list once (a tier ranks the note inside
    that campaign and means nothing outside it)."""
    by_id = {i.get("id"): i for i in items}
    bad = []
    for it in items:
        f = it["_file"]
        bb, cl = it.get("blocked_by", []), it.get("cluster", [])
        if not isinstance(cl, list) or not all(isinstance(c, str) and c for c in cl):
            bad.append(f"{f}: cluster must be a list of cluster names, got {cl!r}")
        if "tier" in it:
            good, malformed = tier_entries(it)
            for e in malformed:
                bad.append(f"{f}: tier entry {e!r} is not CAMPAIGN:N (N a positive integer, 1 first) naming a campaign "
                           f"once, e.g. tier: [\"PB3353:1\"] (kb/Work PB3423)")
            for c in sorted(set(good) - set(cl if isinstance(cl, list) else [])):
                bad.append(f"{f}: tier names campaign {c!r}, which its cluster list {cl!r} does not: a tier ranks a "
                           f"note inside a campaign it belongs to (kb/Work PB3423)")
        if not isinstance(bb, list):
            bad.append(f"{f}: blocked_by must be a list of note ids, got {bb!r}")
            continue
        unknown = [b for b in bb if b not in by_id]
        for b in unknown:
            bad.append(f"{f}: blocked_by names {b!r}, which is not a kb/Work note id — name the blocking note "
                       f"(an owner question is a decision note), or keep `blocked: true` with an empty list and "
                       f"say what blocks it in the body")
        if unknown or not bb or it.get("status") in TERMINAL_STATUSES:
            continue
        still = open_blockers(it, by_id)
        if bool(it.get("blocked")) != bool(still):
            bad.append(f"{f}: blocked is {bool(it.get('blocked'))} but its blocked_by {bb} "
                       + (f"still has {still} open" if still else "have all landed or retired")
                       + " — set `blocked: " + ("true" if still else "false")
                       + "` (a stale true hides the note from every work list)")
    live = {i["id"]: i for i in items if i.get("status") not in TERMINAL_STATUSES and isinstance(i.get("blocked_by"), list)}
    state: dict[str, int] = {}

    def visit(nid: str, path: list[str]) -> None:
        state[nid] = 1
        for b in live[nid]["blocked_by"]:
            if b not in live:
                continue
            if state.get(b) == 1:
                bad.append("blocked_by cycle among open notes: " + " -> ".join(path[path.index(b):] + [b]))
            elif b not in state:
                visit(b, path + [b])
        state[nid] = 2

    for nid in sorted(live, key=id_order):
        if nid not in state:
            visit(nid, [nid])
    return bad


# ── the R3 program's two prerequisites, checked (kb/Work PB2118 Draft 5, F1 and F5; Draft 6, owner question 5) ──────
# ⛔ A WAVE THE PLANNER CAN DISPATCH IS ONE `blocked_by` LETS IT DISPATCH. Draft 4 left four binding and code-generation
# renames with `blocked_by: []`, so the planner offered them between fix-lane trains before anything measured them.
# On 2026-10-07 the owner made the FILE-SET PARTITION the only gate between restructuring waves and fix-lane trains
# (PB2118 question 5, "Drop it"; R69 §2): the folder hold Draft 5 checked here is gone, and the collision check lives
# where the dispatcher decides (scripts/orchestrator/plan_wave.py, `file_set_collisions`). The register keeps only
# what it can verify alone:
#   1. the slices (DESIGN-external-repository §12, R1 §8.6): an open R3 note whose sites name a file of the §12
#      in-flight file set must reach an open external-repository slice note through `blocked_by`, so the note itself
#      says which slice it waits for (the planner then never sees it until that slice lands);
#   2. the measurement (DESIGN-architecture-review §8.1): an open Move/rename or Extract note of the program must
#      reach the note that lands the architecture drift test through `blocked_by`.
R3_PROGRAM, R3_DRIFT_TEST = "PB2119", "PB2417"
#: THE RESTRUCTURING PROGRAM of R69 §2, "R2 and every R3 wave (the Delete program PB2119 included)": the clusters of the
#: architecture review (PB1754, its R2 findings) and of the Delete program (R3_PROGRAM). The open external-repository
#: slices' §12 file set is reserved from ITS waves only, whichever plan proposes them; a fix campaign (PB3353, PB2911)
#: and the fix lane are never held for a slice (DESIGN-architecture-review §8.7 set (b); kb/Work PB3456).
RESTRUCTURING_PROGRAM = ("PB1754", R3_PROGRAM)
R3_MEASURED_WAVES = ("move", "move-rename", "extract", "re-model")
_WAVE_KIND = re.compile(r"\*\*Wave kind:\*\*\s*([\w-]+)", re.I)
#: The wave kind of an R2 defect routed to the fix lane (scripts/arch/r2_collect.py WAVES; file_census_notes writes it):
#: a FIX-LANE note however it reaches the planner, so its declared set is its sites and never reads the member index.
FIX_LANE_WAVE = "defect-for-fix-lane"
#: The bold label file_census_notes writes before the files ONE STEP of a multi-step note edits: a structured site
#: line, since a step's files can lie outside the census finding's Sites (kb/Work PB3242, branch review R1-1: PB2300's
#: IntrinsicCatalog.cs).
STEP_FILES_LABEL = "The files this step edits"
#: A note's STRUCTURED SITE BLOCK opens with a bold label (a paragraph or a list item) or a heading whose first words
#: are "Code site(s)" or "Site(s)": `**Sites:**`, `**Code site.**`, `**Code sites (located ...).**`, `### Code site`;
#: or with the bold STEP_FILES_LABEL.
_SITE_LABEL = re.compile(r"^[ \t]*(?:[-*][ \t]+)?\*\*(?:(?:Code[ \t]+)?Sites?\b|" + re.escape(STEP_FILES_LABEL)
                         + r")[^*\n]*\*\*", re.I)
_SITE_HEADING = re.compile(r"^(#{1,6})[ \t]+(?:Code[ \t]+)?Sites?\b", re.I)
_HEADING = re.compile(r"^(#{1,6})[ \t]")
_LABELED_LINE = re.compile(r"^[ \t]*(?:[-*][ \t]+)?\*\*[^*\n]+\*\*")
_LIST_ITEM = re.compile(r"^[ \t]*(?:[-*+]|\d+[.)])[ \t]")
#: The names a note moves or changes, read from three structured places only: the code spans of its
#: `**Moves or changes:**` line, its `r2_members:` line (comma-separated), and the documentation ids (`M:Ns.T.M(...)`,
#: a Delete note's census sites) in its site blocks or on that line.
_MOVES_LINE = re.compile(r"^\*\*Moves or changes:\*\*(.*)$", re.M)
_R2_MEMBERS = re.compile(r"^r2_members:[ \t]*(.*)$", re.M)
_CODE_SPAN = re.compile(r"`([^`]+)`")
_DOC_ID = re.compile(r"`[MPFETN]:([^`\s@]+)")
#: A path token in a site block ends at whitespace, a quote, a bracket or a separator; a token holding a placeholder,
#: a glob or an ellipsis is not a path.
_PATH_CHAR = r"[^\s`'\"()\[\],;|]"
_NOT_A_PATH = re.compile(r"[<>*{}…]|\.\.\.")
#: The external-repository slices are the notes whose title says so (kb/Work PB2097-PB2104).
EXTERNAL_SLICE_TITLE = re.compile(r"\A\S+ — external repository slice \d+\b")


def open_slices(items: list[dict]) -> list[str]:
    """The open external-repository slices (EXTERNAL_SLICE_TITLE, not terminal), in id order: THE reading the register's
    R3 check and the planner's slice reservation share."""
    return sorted((i["id"] for i in items if EXTERNAL_SLICE_TITLE.match(i.get("title") or "")
                   and i.get("status") not in TERMINAL_STATUSES), key=id_order)


def restructuring(item: dict) -> bool:
    """The note belongs to the restructuring program (its `cluster` list names a RESTRUCTURING_PROGRAM lead): THE
    predicate of R69 §2's slice reservation, so a note shared with a fix campaign is reserved too (kb/Work PB3456)."""
    return isinstance(item.get("cluster"), list) and any(c in item["cluster"] for c in RESTRUCTURING_PROGRAM)
EXTERNAL_DESIGN = REPO / "docs" / "rearchitecture" / "DESIGN-external-repository.md"
_S12 = re.compile(r"^## 12\. .*?$(.*?)^## 13\. ", re.M | re.S)
_DECL = re.compile(r"\b(?:class|record|struct|interface|enum)\s+(?:class\s+|struct\s+)?([A-Z]\w*)")
#: A member declaration's name: modifiers, an optional return type, the name, then a parameter list, a generic list,
#: an accessor block, an expression body or an initializer. Approximate by design: it only maps a §12 name that is not
#: a type to the file that declares it, and a name declared in more than SLICE_MEMBER_FANOUT files is too generic to
#: be a site (`Main`, `Equals`) and is reported, never guessed.
_MEMBER = re.compile(r"^[ \t]*(?:\[[^\]\n]*\][ \t]*)*(?:(?:public|private|internal|protected|static|readonly|override|"
                     r"virtual|sealed|async|partial|new|abstract|required|unsafe|extern|const)\s+)+"
                     r"(?:(?:\([^)\n]*\)|[\w.]+(?:<[^\n]*?>)?)(?:\[\])*\??\s+)?"
                     r"([A-Za-z_]\w*)\s*(?:\(|<[^>\n]*>\s*\(|\{|=>|=|;)", re.M)
SLICE_MEMBER_FANOUT = 3
_ONE_WORD = re.compile(r"_?[A-Za-z][a-z0-9]*")
_SLICE_ROOTS = ("src", "tests")
_CS_KEYWORDS = frozenset("""abstract as base bool break byte case catch char checked class const continue decimal default
delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface
internal is lock long namespace new null object operator out override params private protected public readonly ref return
sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe
ushort using virtual void volatile while global record var dynamic""".split())


@dataclasses.dataclass(frozen=True)
class NoteSites:
    """What a note DECLARES it works on (kb/Work PB3242): the repo-relative `paths` of its site blocks (files and
    folders; `:line`, `#member` and `(member)` suffixes dropped), the `names` it moves or changes (member_index.py
    resolves them to files and adds their callers), its `**Wave kind:**` (`wave`, empty for a fix-lane note): only
    a restructuring note with no names falls back to the types its sites declare (member_index.note_names), and the
    MEMBERS it names in a file (`members`, kb/Work PB3670)."""
    paths: tuple[str, ...]
    names: tuple[str, ...]
    wave: str
    #: The note HAS a site block, whatever it names: `**Sites:** none (…)` declares that it changes no code file, which
    #: sites_problems accepts, while a note with no block at all reserves nothing and is dispatched UNPARTITIONED
    #: (plan_wave.unpartitioned, kb/Work PB3263, PB3670).
    declared: bool = False
    #: {path: the members its site blocks name in it} (kb/Work PB3670), for a path EVERY mention of which names its
    #: members: `path#Member`, or `path (`member`, `Type.Member`, …)` (a parenthesis right after the path that opens
    #: with a code span; `(§8.7)` for a document's section). A path mentioned bare, with a `:line`, or with a prose
    #: parenthesis is the whole file and is absent here: the partition reads it whole (scripts/arch/source_units.py).
    members: dict[str, tuple[str, ...]] = dataclasses.field(default_factory=dict, hash=False)

    @property
    def fix_lane(self) -> bool:
        """An R2 defect routed to the fix lane (FIX_LANE_WAVE): its `names` (r2_members) say where the defect lives,
        never what a restructuring moves, so they resolve to no callers and its set is its sites (kb/Work PB3242,
        branch review R1-2; DESIGN-architecture-review §8.7: a fix-lane plan never builds the index)."""
        return self.wave == FIX_LANE_WAVE


def _site_blocks(text: str) -> list[str]:
    """The text of every site block: a site heading's section (to the next heading of its level or higher), or a site
    label's paragraph plus the list that follows it (a loose list, or a label alone on its line), ending at a heading
    or at the next labeled line."""
    lines, out, i = text.split(chr(10)), [], 0
    while i < len(lines):
        if h := _SITE_HEADING.match(lines[i]):
            level, block = len(h.group(1)), [lines[i]]
            i += 1
            while i < len(lines) and not ((n := _HEADING.match(lines[i])) and len(n.group(1)) <= level):
                block.append(lines[i])
                i += 1
            out.append(chr(10).join(block))
            continue
        if not _SITE_LABEL.match(lines[i]):
            i += 1
            continue
        block = [lines[i]]
        i += 1
        while i < len(lines):
            if lines[i].strip():
                if _HEADING.match(lines[i]) or _LABELED_LINE.match(lines[i]):
                    break
                block.append(lines[i])
                i += 1
                continue
            j = i
            while j < len(lines) and not lines[j].strip():
                j += 1
            if j < len(lines) and _LIST_ITEM.match(lines[j]) and not _LABELED_LINE.match(lines[j]):
                i = j
                continue
            break
        out.append(chr(10).join(block))
    return out


@functools.lru_cache(maxsize=None)
def _path_token(roots: tuple[str, ...]) -> re.Pattern[str]:
    return re.compile(r"(?<![\w/.-])((?:" + "|".join(map(re.escape, roots)) + r")/" + _PATH_CHAR + "+)")


@functools.lru_cache(maxsize=1)
def campaign_domain():
    """model_rules.json `campaign` (coord.campaign, the one reader of model_rules.json; a coord.CampaignDomain): the
    code roots a site path may start with, never a literal list here (scripts/ is a campaign root that this reader's
    `(src|tests|docs)/` once ignored, so PB3203's own sites declared nothing: kb/Work PB3242)."""
    sys.path.append(str(REPO / "scripts" / "orchestrator"))
    import coord  # noqa: PLC0415
    return coord.campaign()


def note_sites(text: str, roots: tuple[str, ...] | None = None, repo: pathlib.Path | None = None) -> NoteSites:
    """THE ONE READER of a note's structured sites (kb/Work PB3242; DESIGN-architecture-review §8.7): the planner and the
    landing check (plan_wave.declared_sets), the member index (member_index.note_file_set) and this register's checks
    all read a note through it, and no other module scans a note's text for paths. A path counts only inside a site
    block and only under a campaign root (`roots`, default campaign_domain()'s); a path in prose, a `Gate:` line or a
    tool command, and a bare type name, declare nothing. With `repo` (the tree the claim is planned on), a file's
    members count only when every one of them names a unit that file declares there (_members_located): a stale,
    misspelt or prose name claims the file WHOLE, never a part of it no code matches (kb/Work PB3670 review R1-3)."""
    text = text.replace(chr(13) + chr(10), chr(10))
    blocks = _site_blocks(text)
    token = _path_token(roots or campaign_domain().roots)
    paths: set[str] = set()
    mentions: dict[str, list[tuple[str, ...]]] = {}   # path -> the members each mention names (() = the whole file)
    for b in blocks:
        for m in token.finditer(b):
            raw = m.group(1)
            if _NOT_A_PATH.search(raw):
                continue
            p = re.split(r"[:#]", raw, maxsplit=1)[0].rstrip(".,:").rstrip("/")
            if "/" in p:   # a root alone ("src/") is no site
                paths.add(p)
                mentions.setdefault(p, []).append(_mention_members(raw, b, m.end()))
    names: list[str] = []
    moves = _MOVES_LINE.search(text)
    if moves:
        names += [s.strip() for s in _CODE_SPAN.findall(moves.group(1)) if not _DOC_ID.match(f"`{s}")]
    if r2 := _R2_MEMBERS.search(text):
        names += [s.strip() for s in r2.group(1).split(",") if s.strip()]
    for part in blocks + ([moves.group(1)] if moves else []):
        names += [d.split("~", 1)[0] for d in _DOC_ID.findall(part)]
    wave = m.group(1).lower() if (m := _WAVE_KIND.search(text)) else ""
    members = {p: tuple(sorted({n for ms in each for n in ms})) for p, each in mentions.items() if all(each)}
    if repo is not None:
        members = {p: ms for p, ms in members.items() if _members_located(repo / p, ms)}
    return NoteSites(tuple(sorted(paths)), tuple(dict.fromkeys(names)), wave, bool(blocks), members)


#: A member a site block names after its path: an identifier, dotted (`Type.Member`), with an optional `()`; a `~`
#: names a finalizer. A code span that names a file (`Foo.cs`) or holds a space is no member.
_MEMBER_NAME = re.compile(r"~?[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*(?:\(\))?")
_FILE_NAME = re.compile(r"\.(?:cs|py|md|json|ps1|psm1|g4|js|ts|txt|ya?ml|sh|csproj|props|targets|sln)$", re.I)
_SECTION_REF = re.compile(r"§(\d+(?:\.\d+)*)")


def _mention_members(raw: str, block: str, end: int) -> tuple[str, ...]:
    """The members ONE mention of a site path names (kb/Work PB3670), or () for the whole file: its `#Member` suffix,
    else the code spans (and `§N.N` section numbers) of a parenthesis right after it (past its closing backtick) that
    OPENS with a code span or a section sign (`plan_wave.py` (`collision`, `collision_set`)), with or without a `:line`
    suffix (`DataBinder.cs:123` (`DataBinder.Bind`): the line says where to look, the member what is claimed, and the
    binding rule R3 writes sites this way, review R1-1); a bare mention, a `:line` alone and a prose parenthesis ("(the
    shared-primary-file rule)", "(the planner side that honours `x`)") name the whole file (a line number drifts as the
    file changes, so it locates no unit)."""
    if "#" in raw:
        frag = raw.split("#", 1)[1].rstrip(".,:")
        return (frag,) if _MEMBER_NAME.fullmatch(frag) and not _FILE_NAME.search(frag) else ()
    rest = block[end:]
    rest = rest[1:] if rest.startswith("`") else rest
    opened = re.match(r"[ \t]*\(", rest)
    if not opened:
        return ()
    depth, k = 0, opened.end() - 1
    for k in range(opened.end() - 1, len(rest)):
        depth += {"(": 1, ")": -1}.get(rest[k], 0)
        if depth == 0:
            break
    inner = rest[opened.end():k]
    if not inner.lstrip().startswith(("`", "§")):
        return ()
    names = [s.strip() for s in _CODE_SPAN.findall(inner)]
    out = [n.removesuffix("()") for n in names if _MEMBER_NAME.fullmatch(n) and not _FILE_NAME.search(n)]
    out += [f"§{s}" for s in _SECTION_REF.findall(_CODE_SPAN.sub("", inner))]
    return tuple(dict.fromkeys(out))


#: The first note id `check` holds to SITES_RULE (kb/Work PB3263, filed 2026-10-19 at PB3443, the next id unallocated
#: when the rule landed). Every cluster is a campaign the moment an operator runs it (orchestrate.ps1 -Cluster), and on
#: 2026-10-19 198 open notes naming 218 clusters declared no site block, most of them in long-idle clusters: refusing
#: those would turn the check into a backlog, so they stay readable (the planner reads each one's set as empty: it
#: holds nothing and is planned UNPARTITIONED, plan_wave.collision, kb/Work PB3670), and the open notes of the RUNNING campaigns
#: (PB3353, PB3160) were given their site blocks by hand in the change that added this rule. From here on, a note filed
#: into a cluster carries its block from its first commit. A note of a cluster the owner RANKS AS A CAMPAIGN needs one
#: whatever its id (campaign_leads, kb/Work PB3640): the campaign's notes are planned side by side, and two of them
#: planned unsited ran in parallel on one function (wave 20261030: PB3628 and PB3275 on prune_worktrees.lifecycle).
SITES_REQUIRED_FROM = 3443
#: The site-block form SITES_RULE asks for, quoted by MANDATORY-PRACTICES R3 and the registrar brief (a drift check in
#: scripts/orchestrator/test_plan_wave.py holds them to it and to what note_sites reads from it).
SITES_EXAMPLE = "**Sites:** `src/Cobol.Net.Compiler/Binding/DataBinder.cs:123` (`DataBinder.Bind`, `DataBinder.Sync`)"
SITES_RULE = ("an open note that names a cluster declares its sites: a `**Sites:**` paragraph naming each file it "
              "changes AND the members it changes in it, the file's path (a `:line` suffix optional) followed by a "
              "parenthesis of code spans, as in " + SITES_EXAMPLE + ", or `path#Member`; the wave planner partitions "
              "on the members, so a file named without them, or with a name the file does not declare, is claimed "
              "whole and holds every other group on it (kb/Work PB3670); or `**Sites:** none (why)` when it changes "
              "no code file")


def _members_located(path: pathlib.Path, names: tuple[str, ...]) -> bool:
    """Whether every one of `names` matches a unit `path` declares (source_units.match), so a claim on those members
    holds the code its edits reach. A file not on the tree yet (one the change creates) has nothing to check them
    against and keeps them (so does a folder, which is no file claim: member_index.note_file_set drops it); a file
    with no units (no reader for its kind, one the reader cannot balance, one it cannot read) cannot locate any name,
    and a name no unit matches (a deleted function, a local, a prose word in a code span) says nothing about where the
    edit lands: both claim the file whole (kb/Work PB3670 review R1-3)."""
    if not path.is_file():
        return True
    try:
        stat = path.stat()
        declared = _unit_names(str(path), stat.st_mtime_ns, stat.st_size)
    except OSError:
        return False
    return declared is not None and all(any(_source_units().match(n, u) for u in declared) for n in names)


@functools.lru_cache(maxsize=1)
def _source_units():
    """scripts/arch/source_units.py, the one reader of a file's units (kb/Work PB3670)."""
    sys.path.append(str(REPO / "scripts" / "arch"))
    import source_units  # noqa: PLC0415
    return source_units


@functools.lru_cache(maxsize=512)
def _unit_names(path: str, mtime_ns: int, size: int) -> frozenset[str] | None:
    """The unit names of the file at `path`, keyed on its stamp so a file rewritten in this process is read again."""
    us = _source_units().units(path, pathlib.Path(path).read_text(encoding="utf-8", errors="replace"))
    return None if us is None else frozenset(u.name for u in us)


def campaign_leads(items: list[dict]) -> set[str]:
    """The clusters the owner runs as CAMPAIGNS: every campaign a note's `tier:` names (tier_entries). The register
    holds no list of running campaigns and must not grow one (CLAUDE.md rule 8); a `tier:` entry IS the owner's word
    that the campaign's ready notes are ranked and planned (`work.py next --cluster`), so it is the one place the
    register already says which clusters run."""
    return {c for it in items for c in tier_entries(it)[0]}


def sites_problems(items: list[dict]) -> list[str]:
    """SITES_RULE for every open note from SITES_REQUIRED_FROM on (kb/Work PB3263), and for every open note of a
    campaign cluster or that tiers a campaign, whatever its id (campaign_leads, kb/Work PB3640): a note with no site
    block gives the planner's partition nothing to hold or check (kb/Work PB3670: it is planned UNPARTITIONED). A
    `decision` note changes no file by itself and is exempt."""
    bad = []
    campaigns = campaign_leads(items)
    for it in items:
        nid = str(it.get("id") or "")
        clusters = it.get("cluster") if isinstance(it.get("cluster"), list) else []
        in_campaign = bool(campaigns & set(clusters)) or bool(tier_entries(it)[0])
        number = re.fullmatch(r"PB(\d+)", nid)
        from_cutover = bool(clusters) and number is not None and int(number.group(1)) >= SITES_REQUIRED_FROM
        if it.get("kind") == "decision" or it.get("status") in TERMINAL_STATUSES or not (from_cutover or in_campaign):
            continue
        if not note_sites(it.get("_body", "")).declared:
            where = (f"names the campaign cluster(s) {', '.join(sorted(campaigns & set(clusters))) or 'it tiers'}"
                     if in_campaign else f"names the cluster(s) {', '.join(clusters)}")
            bad.append(f"{it['_file']}: {where} but declares no site block — {SITES_RULE}; without one the wave "
                       f"planner partitions nothing for it and plans it UNPARTITIONED (kb/Work PB3263, PB3640, PB3670)")
    return bad


def _slice_rows(repo: pathlib.Path) -> list[list[str]]:
    """DESIGN-external-repository §12's rows whose change has not landed (a row whose "Change" cell opens with
    "landed" is history), each as its three cells: Today, Change, Callers updated in the same change."""
    doc = (repo / EXTERNAL_DESIGN.relative_to(REPO)).read_text(encoding="utf-8")
    m = _S12.search(doc)
    if not m:
        raise SystemExit("⛔ DESIGN-external-repository.md has no §12 table: the slice file set cannot be read")
    rows = []
    for line in m.group(1).splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split(" | ")]
        if not line.startswith("| ") or len(cells) < 3 or cells[0] == "Today" or line.startswith("|---"):
            continue
        if cells[1].lstrip("*").lower().startswith("landed"):
            continue
        rows.append(cells)
    return rows


def _slice_spans(cell: str) -> list[str]:
    """The code spans of one §12 cell with their parenthesized and angle-bracketed parts removed: a parameter or
    argument list (`ObjectRefDescriptor(Kind, Name, Factory, Only)`, `RegisterModule(registrar, …)`) and a generic
    or placeholder (`Cobol.<S>`, `<Version>`) name parameters, never sites."""
    out = []
    for span in re.findall(r"`([^`]+)`", cell):
        prev = None
        while prev != span:
            prev, span = span, re.sub(r"\([^()]*\)|<[^<>]*>", " ", span)
        out.append(span)
    return out


def slice_file_set(repo: pathlib.Path = REPO, unmapped: list[str] | None = None) -> set[str]:
    """DESIGN-external-repository §12's in-flight file set (R1 §8.6): every file a slice still to land edits. All three
    columns of every unlanded row are read (the "Callers updated in the same change" column names files the Today
    column does not: PB2118 Draft 7, the sixth refuter's H4), and every name in a code span counts, not only the
    first. A span that is a path (`docs/CONFORMANCE.md`, a `tests/.../char_*.g.cs.txt` glob) or a build file
    (`Directory.Build.props`, `*.csproj`) maps to itself. Otherwise each dotted chain is read: every TYPE in it maps
    to every file under src/ or tests/ that declares it (each partial), and a member after a type is that type's
    (`BindSession.Repository : GroupRepository` is BindSession's and GroupRepository's files); a lone name that is
    not a type is a MEMBER, in the Today and Callers columns only (a Change cell's lone names are the new shape),
    mapped to the row's own types' files when one of them declares it and otherwise to the files declaring a member
    of that name when at most SLICE_MEMBER_FANOUT do and the name is more than one word (`CheckPrototypeSignaturePairs`
    is a site; `Version`, `Target` and `Main` are prose). A name more generic than that, and a Today-column name declared
    nowhere (the table is stale: the code moved), is appended to `unmapped`, never guessed.
    Repo-relative POSIX paths. One implementation: plan_wave.py's collision check and r3_program_problems read it."""
    rows = _slice_rows(repo)
    words: set[str] = set()
    for cells in rows:
        for span in (s for c in cells for s in _slice_spans(c)):
            words.update(re.findall(r"[A-Za-z_]\w*", span))
    types: dict[str, set[str]] = {}
    members: dict[str, set[str]] = {}
    tracked = tracked_paths(repo)   # the committed tree, never a build output or a stray file (PB1957, 2026-10-07)
    for root in _SLICE_ROOTS:
        for f in (repo / root).rglob("*.cs"):
            rel = f.relative_to(repo).as_posix()
            if rel not in tracked:
                continue
            text = f.read_text(encoding="utf-8", errors="replace")
            for n in set(_DECL.findall(text)) & words:
                types.setdefault(n, set()).add(rel)
            for n in set(_MEMBER.findall(text)) & words:
                members.setdefault(n, set()).add(rel)
    out: set[str] = set()
    for cells in rows:
        row_types: set[str] = set()
        lone: list[tuple[int, str]] = []
        for col, cell in enumerate(cells):
            for span in _slice_spans(cell):
                s = span.strip()
                if "/" in s or re.fullmatch(r"[\w.*-]+\.(?:props|csproj|targets|md|json)", s):
                    found = repo.glob(s) if "/" in s else [*repo.glob(s), *(repo / "src").rglob(s)]
                    out |= {r for g in found if (r := g.relative_to(repo).as_posix()) in tracked and g.is_file()}
                    continue
                for chain in re.findall(r"[A-Za-z_][\w.]*", s):
                    segs = [x for x in chain.split(".") if x and x not in _CS_KEYWORDS]
                    # the first type in a chain is the site; what follows it is that type's member
                    if first := next((x for x in segs if x in types), None):
                        row_types.add(first)
                    elif len(segs) == 1:
                        lone.append((col, segs[0]))
        for n in row_types:
            out |= types[n]
        for col, n in lone:
            if col == 1:
                continue
            mine = {f for t_ in row_types for f in types[t_]} & members.get(n, set())
            if mine:
                out |= mine
            elif n in members and len(members[n]) <= SLICE_MEMBER_FANOUT and not _ONE_WORD.fullmatch(n):
                out |= members[n]
            elif unmapped is not None and n in members:
                unmapped.append(f"{n} ({len(members[n])} declaring files)")
            elif unmapped is not None and col == 0 and n[:1].isupper():
                unmapped.append(f"{n} (declared nowhere: a planned name, or §12's Today column is stale)")
    if unmapped is not None:
        unmapped[:] = sorted(set(unmapped))
    return out


_NAMED_PATH = re.compile(r"`((?:src|tests|scripts|docs|tools)/[^`\s:#]+\.[A-Za-z0-9]+)(?:[:#][^`]*)?`(\s*\(new)?")
# the same path written WITHOUT a code span (the eighth refuter's K1, plant 4): a source file named in prose
_BARE_PATH = re.compile(r"(?<![`\w/.-])((?:src|tests)/[\w./-]+\.(?:cs|g4|py))(?![\w`])(\s*\(new)?")


#: The ANTLR output folder (never committed; the build regenerates it from `Grammar/*.g4`). A path under it is a BUILD
#: OUTPUT: no wave edits it, so it is neither "missing" nor a member of a file set; the note names the grammar.
_GENERATED_DIR = re.compile(r"(?:^|/)Generated/")


def tracked_paths(repo: pathlib.Path) -> frozenset[str]:
    """Every file git tracks in `repo`, and every folder above one. The existence rule reads the COMMITTED tree, never
    the filesystem: a build output (the ANTLR parser under `Generated/`) exists in a built checkout and not in CI's,
    so a filesystem test passed locally and failed CI on PB2197 (CLAUDE.md, the CI invariant). The index is the
    git index of `repo`: a caller that checks many notes reads it once and passes it on."""
    out = subprocess.run(["git", "-C", str(repo), "ls-files", "-z"], capture_output=True, check=True).stdout
    files = [f for f in out.decode("utf-8").split("\0") if f]
    dirs = {"/".join(parts[:k]) for f in files for parts in [f.split("/")] for k in range(1, len(parts))}
    return frozenset(files) | frozenset(dirs)


def is_build_output(path: str) -> bool:
    return bool(_GENERATED_DIR.search(path))


def is_view_output(path: str) -> bool:
    """A registered rendered view's output (scripts/rendered_views.py VIEWS, the one registry)."""
    sys.path.insert(0, str(REPO / "scripts"))
    try:
        import rendered_views  # noqa: PLC0415
    finally:
        sys.path.pop(0)
    return rendered_views.is_view_output(path)


def named_paths(body: str) -> list[tuple[str, bool]]:
    """(path, is new) for every repository path a note names, in code spans or bare in prose (a path followed by
    "(new" is a file the wave creates); placeholders (`<sha>`) and globs are not paths."""
    found = _NAMED_PATH.findall(body) + _BARE_PATH.findall(body)
    return [(p, bool(new)) for p, new in found if "<" not in p and "*" not in p]


def named_missing_paths(body: str, repo: pathlib.Path = REPO, tracked: frozenset[str] | None = None) -> list[str]:
    """The repository paths a note names that the committed tree does not have: a wave's file set is computed from
    what its note names, so a wrong path is a hole in the partition (PB2118 Draft 8, J2: PB2296 named
    `Binding/BinderContext.cs`, which is `Binding/Procedure/`; Draft 9: a bare path was never checked). A build
    output is not missing, and neither is a RENDERED VIEW (kb/Work PB3026: gitignored, rendered on demand, so the committed
    tree never has it); `build_output_problems` holds the note to its source instead."""
    tracked = tracked_paths(repo) if tracked is None else tracked
    return sorted({p for p, new in named_paths(body)
                   if not new and not is_build_output(p) and not is_view_output(p) and p not in tracked})


def build_output_problems(body: str) -> list[str]:
    """A note that names a generated parser file must also name a grammar file (`*.g4`): the wave edits the grammar,
    and the planner's file set is only as good as the source paths the note names (kb/Work PB2197)."""
    paths = [p for p, _ in named_paths(body)]
    outputs = sorted({p for p in paths if is_build_output(p)})
    if outputs and not any(p.endswith(".g4") and not is_build_output(p) for p in paths):
        return [f"names the build output `{outputs[0]}` but no grammar file: no wave edits a generated file — name "
                f"the `.g4` it is generated from"]
    return []


def load_member_index():
    """scripts/arch/member_index.py as a module (the one computation of a note's callers; PB2118 Draft 8, J2)."""
    sys.path.insert(0, str(REPO / "scripts" / "arch"))
    import member_index  # noqa: PLC0415
    return member_index


def r3_program_problems(items: list[dict], slice_files: set[str] | None = None,
                        unknown_names: dict[str, list[str]] | None = None) -> list[str]:
    """The register's own checks of the R3 program (cluster PB2119): every path an open note names exists; every
    name it moves or changes is one the member index knows (so the planner can compute its callers); a note whose
    declared Sites meet the external repository's in-flight set waits for an open slice; and every measured wave
    waits for the drift test. The CALLERS' collision with the slices is the planner's, recomputed at every plan
    (plan_wave.campaign_clusters; DESIGN-architecture-review §8.7): callers change with every landing, so a
    blocked_by written from them would be a hand list that drifts. `unknown_names` is {note id: unknown names}
    (computed here when None)."""
    by_id = {i.get("id"): i for i in items}
    slices = open_slices(items)
    if slice_files is None:
        slice_files = slice_file_set() if slices else set()

    def reaches(it: dict, target: str) -> bool:
        seen, todo = set(), list(it.get("blocked_by") or [])
        while todo:
            b = todo.pop()
            if b == target:
                return True
            if b in seen or b not in by_id:
                continue
            seen.add(b)
            todo.extend(by_id[b].get("blocked_by") or [])
        return False

    r3 = [it for it in items if R3_PROGRAM in (it.get("cluster") or []) and it.get("status") not in TERMINAL_STATUSES]
    if unknown_names is None:
        mi = load_member_index()
        index = mi.load()
        unknown_names = {it["id"]: mi.resolve(index, mi.note_names(it.get("_body", ""), index)[0])[1] for it in r3}
    bad = []
    tracked = tracked_paths(REPO) if r3 else frozenset()
    for it in r3:
        body = it.get("_body", "")
        for p in named_missing_paths(body, tracked=tracked):
            bad.append(f"{it['_file']}: names `{p}`, which does not exist — fix the path (a file the wave creates is "
                       f"marked `(new)`); the planner computes the wave's file set from what the note names")
        bad += [f"{it['_file']}: {msg}" for msg in build_output_problems(body)]
        if unknown := unknown_names.get(it["id"]):
            bad.append(f"{it['_file']}: moves or changes {', '.join(unknown)}, which the member index "
                       f"(scripts/arch/member_index.py) does not know — a typo, or a member renamed since; the "
                       f"wave's callers cannot be computed")
        if it.get("id") == R3_DRIFT_TEST:
            continue
        wave = (m.group(1).lower() if (m := _WAVE_KIND.search(body)) else "")
        hit = sorted(set(note_sites(body).paths) & slice_files)
        if hit and not any(reaches(it, s) for s in slices):
            bad.append(f"{it['_file']}: an R3 {wave or '?'} wave whose sites name {hit[0]}"
                       f"{f' (+{len(hit) - 1} more)' if len(hit) > 1 else ''}, a file of the external repository's "
                       f"in-flight set (DESIGN-external-repository §12; R1 §8.6), and that waits for no open slice — "
                       f"add the slice note that changes it ({', '.join(slices)}) to "
                       f"blocked_by")
        if wave in R3_MEASURED_WAVES and not reaches(it, R3_DRIFT_TEST):
            bad.append(f"{it['_file']}: an R3 {wave} wave that does not wait for {R3_DRIFT_TEST}, the architecture "
                       f"drift test — its neutrality is 'the walk shows no row growing' (DESIGN-architecture-review "
                       f"§8.1), which needs the walk landed first; add {R3_DRIFT_TEST} to blocked_by")
    return bad


def print_cluster(view: dict, as_json: bool) -> int:
    if as_json:
        print(json.dumps(view, ensure_ascii=False))
        return 0 if view["named"] else 1
    lead, notes = view["cluster"], view["notes"]
    if not view["named"]:
        print(f"cluster {lead}: no kb/Work note names it in `cluster:`")
        return 1
    if not notes:
        print(f"cluster {lead}: LANDED ({view['named']} note(s), every one landed or retired)")
        return 0
    ready = sum(n["ready"] for n in notes)
    print(f"cluster {lead}: {len(notes)} open note(s), {ready} ready, {len(notes) - ready} waiting "
          f"(tier, then blocked_by order)")
    for n in notes:
        title = re.sub(rf"^{re.escape(n['id'])}\s*[—-]\s*", "", n["title"])[:90]
        state = "ready  " if n["ready"] else "waiting on " + ", ".join(n["waiting_on"])
        depth = "cyc" if n["depth"] is None else f"{n['depth']:3}"
        tier = "  -" if n["tier"] is None else f"t{n['tier']:<2}"
        print(f"  {tier} {depth} {n['id']:7} {n['kind']:9} {n['area'][:18]:18} {state}"
              + (f"  {title}" if n["ready"] else ""))
    return 0


# ── the reader's self-test ───────────────────────────────────────────────────────────────────────────────────

FIXTURE = REPO / "tests" / "version-matrix" / "work-frontmatter-cases.json"
FIXTURE_REL = "tests/version-matrix/work-frontmatter-cases.json"


def reading_view(file: str, text: str) -> dict:
    """What a reader of this register SEES in the file `file` holding `text` — the normalized answer it must give.

    Absent and empty are deliberately the same answer for a list (`[]`) and for a scalar (`""`): the register's checks
    ask what a note SAYS, and a key that is missing says exactly as much as a key that is empty. What is NOT
    normalized away is `errors` — a malformed value is a third outcome, and collapsing it into "empty" is the
    fail-open that `kb/Work/PB875` is about.

    `terminal` is what the DONE vocabulary (TERMINAL_STATUSES) makes of the status: the register's checks read DONE
    through it, so it is compared like any field (kb/Work PB3196). `refusal` is what read_note says of a file that is
    not a note: empty for an index or a README (skipped), the reason for a file NAMED like a note whose frontmatter
    does not parse (kb/Work PB3654).
    """
    d, refusal = read_note(file, text)
    if d is None:
        return {"note": False, "id": "", "kind": "", "status": "", "terminal": False, "inventory_rows": [],
                "closes_rows": [], "closes_rows_reason": "", "errors": [], "refusal": refusal}
    scalar = lambda k: (v if isinstance(v := d.get(k, ""), str) else str(v))       # noqa: E731
    lst = lambda k: (v if isinstance(v := d.get(k, []), list) else [])             # noqa: E731
    return {"note": True, "id": scalar("id"), "kind": scalar("kind"), "status": scalar("status"),
            "terminal": scalar("status") in TERMINAL_STATUSES,
            "inventory_rows": lst("inventory_rows"), "closes_rows": lst("closes_rows"),
            "closes_rows_reason": scalar("closes_rows_reason"), "errors": sorted(d.get("_errors", [])),
            "refusal": ""}


def _wrapped_list(text: str) -> bool:
    """True when some line opens a `[` that does not close on the same line — the PB875 shape."""
    return any("]" not in line[line.index("["):] for line in text.splitlines() if "[" in line)


def case_findings(fixture: dict, answers: list[dict]) -> list[str]:
    """Where the reader disagrees with the fixture's recorded expectation, field by field."""
    bad = []
    for case, got in zip(fixture["cases"], answers, strict=True):
        for k, want in case["expect"].items():
            # A fixture naming a field the view does not have is a FINDING, not a traceback.
            if k not in got:
                bad.append(f'case "{case["name"]}": the fixture expects a field {k!r} that the reader does not report')
            elif got[k] != want:
                bad.append(f'case "{case["name"]}": {k} is {got[k]!r}, fixture says {want!r}')
    return bad


def directory_findings(fixture: dict) -> list[str]:
    """The fixture's files written into a directory and read as a register (kb/Work PB3654): `register_problems` and
    `load` refuse each file named like a note that does not parse, `check --work` counts the DIRECTORY's notes (it once
    checked `kb/Work` whatever `--work` said, so the seam could not witness a refusal), and an index is skipped."""
    import contextlib  # noqa: PLC0415
    import io  # noqa: PLC0415
    import tempfile  # noqa: PLC0415

    bad: list[str] = []
    with tempfile.TemporaryDirectory(prefix="work-register-cases-") as tmp:
        everything = pathlib.Path(tmp, "everything")
        everything.mkdir()
        refused = {}
        for c in fixture["cases"]:
            (everything / c["file"]).write_bytes(c["text"].encode("utf-8"))
            if c["expect"]["refusal"]:
                refused[c["file"]] = c["expect"]["refusal"]
        _, problems = register_problems(everything)
        bad += [f"register_problems over the fixture directory does not report: {why}"
                for why in refused.values() if why not in problems]
        bad += [f"register_problems reports a file that is no refusal of the fixture: {p}"
                for p in problems if "unreadable as a work note" in p and p not in refused.values()]
        try:
            load(everything)
            bad.append("load() answered a directory holding an unreadable note instead of stopping")
        except SystemExit as stop:
            bad += [f"load()'s refusal does not name {name}" for name in refused if name not in str(stop)]
        good = pathlib.Path(tmp, "good")
        good.mkdir()
        (good / "PB1.md").write_bytes(("---" + chr(10) + "id: PB1" + chr(10) + "kind: defect" + chr(10)
                                       + "status: landed" + chr(10) + "area: x" + chr(10) + "closes_rows: []"
                                       + chr(10) + 'closes_rows_reason: "none"' + chr(10) + "---" + chr(10)
                                       + "body" + chr(10)).encode("utf-8"))
        (good / "README.md").write_bytes(b"not a note")
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = check(good)
        if code != 0 or "1 work items" not in out.getvalue():
            bad.append(f"check --work over one good note and an index answered {code}: {out.getvalue().strip()!r}")
        for name in refused:
            (good / name).write_bytes(next(c["text"] for c in fixture["cases"] if c["file"] == name).encode("utf-8"))
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = check(good)
        if code == 0 or not all(name in out.getvalue() for name in refused):
            bad.append(f"check --work over a directory holding unreadable notes answered {code}: {out.getvalue().strip()!r}")
    return bad


def self_test() -> int:
    """Evaluate `tests/version-matrix/work-frontmatter-cases.json` against `parse_frontmatter`.

    ⛔ THE FIXTURE ASSERTS ITS OWN POPULATION. A run that measured no wrapped list, no malformed value and no non-note
    file would be green about nothing at all — precisely the state `work.py check` was in while `kb/Work/PB390`
    claimed four rows the gate could not see (`feedback_green_gates_arent_evidence`,
    `feedback_verdict_evidence_invariant`).
    """
    if not FIXTURE.exists():
        print(f"⛔ fixture not found: {FIXTURE_REL}")
        return 1
    fixture = json.loads(FIXTURE.read_text(encoding="utf-8"))
    answers = [{"name": c["name"], **reading_view(c["file"], c["text"])} for c in fixture["cases"]]
    findings = case_findings(fixture, answers)

    # POPULATION — the shapes this register actually contains, each of which broke a reader once.
    covered = {
        "a list wrapped across lines": any(_wrapped_list(c["text"]) for c in fixture["cases"]),
        "a malformed value reported as an error": any(a["errors"] for a in answers),
        "a file that is not a note": any(not a["note"] for a in answers),
        "CRLF line endings": any("\r\n" in c["text"] for c in fixture["cases"]),
        "a file named like a note whose frontmatter does not parse": any(a["refusal"] for a in answers),
    }
    findings += [f"the fixture carries no case for {what} — it cannot be measuring that shape"
                 for what, ok in covered.items() if not ok]
    # EVERY STATUS OF THE VOCABULARY, so the `terminal` answer is measured on each one (kb/Work PB3196): a status added
    # to STATUSES or TERMINAL_STATUSES stops this until a case states what it is.
    stated = {a["status"] for a in answers if a["note"]}
    findings += [f"the fixture carries no note whose status is {s!r} — the `terminal` answer for it is not measured"
                 for s in sorted((STATUSES | set(TERMINAL_STATUSES)) - stated)]
    # EVERY FIELD, on every case: a field the fixture leaves unstated is a field the reader may change unseen.
    findings += [f'case "{c["name"]}": the fixture states no {k!r} — it must state every field the view reports'
                 for c, a in zip(fixture["cases"], answers) for k in a if k != "name" and k not in c["expect"]]
    if len(fixture["cases"]) < 8:
        findings.append(f'{len(fixture["cases"])} case(s) — too few to be measuring the reader')

    findings += directory_findings(fixture)

    # FALSIFICATION — the comparison must be able to fail, or "no disagreement" is a statement about nothing.
    corrupted = json.loads(json.dumps(fixture))
    corrupted["cases"][0]["expect"]["id"] = corrupted["cases"][0]["expect"].get("id", "") + "-X"
    if not case_findings(corrupted, [{"name": c["name"], **reading_view(c["file"], c["text"])}
                                     for c in corrupted["cases"]]):
        findings.append("a fixture whose expected id is WRONG produced no finding — the comparison is inert")

    print(f"cases  : {len(answers)} from {FIXTURE_REL}")
    if findings:
        print(f"\n⛔ {len(findings)} finding(s):")
        for f in findings:
            print(f"   {f}")
        return 1
    print("no findings.")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", nargs="?", choices=["migrate", "check", "next", "stats"])
    ap.add_argument("--self-test", action="store_true", help="the frontmatter reader against its cases")
    ap.add_argument("--top", type=int, default=3)
    ap.add_argument("--json", action="store_true", help="emit one machine-readable JSON line (next --cluster)")
    ap.add_argument("--work", default=str(WORK), help="the register directory (default kb/Work; a test seam)")
    ap.add_argument("--cluster", metavar="LEAD", help="next: the open notes whose `cluster` names LEAD, in blocked_by "
                    "order, whatever their harm flags (a campaign; exit 1 when no note names it)")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.cmd is None:
        ap.error("a command (migrate, check, next, stats) or --self-test")
    if a.cmd == "migrate":
        return migrate()
    if a.cmd == "check":
        return check(pathlib.Path(a.work))
    if a.cluster and a.cmd != "next":
        ap.error("--cluster goes with next")
    items = load(pathlib.Path(a.work))
    if a.cmd == "next" and a.cluster:
        return print_cluster(cluster_order(items, a.cluster), a.json)
    if a.cmd == "next":
        nxt = actionable(items)
        if not nxt:
            print("next   : (nothing unblocked harms a user's program)")
            return 0
        print("next   : " + " · ".join(f'{i["id"]} ({i["area"]})' for i in nxt[:a.top])
              + f"   [{len(nxt)} actionable]")
        return 0
    import collections
    print("kind   :", dict(collections.Counter(i.get("kind") for i in items)))
    print("status :", dict(collections.Counter(i.get("status") for i in items)))
    print("actionable now:", len(actionable(items)))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
