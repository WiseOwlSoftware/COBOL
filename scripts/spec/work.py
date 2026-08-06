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
    python scripts/spec/work.py stats      # counts by kind/status/harm
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
QUEUE = REPO / "docs" / "rearchitecture" / "CONFORMANCE-FIX-QUEUE.md"
PLAN = REPO / "docs" / "COBOLNET_REARCHITECTURE_PLAN.md"
WORK = REPO / "kb" / "Work"

KINDS = {"defect", "analysis", "adjudication", "decision"}
STATUSES = {"open", "half", "landed", "owner", "blocked", "retired"}
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
        (WORK / f'{item["id"]}.md').write_text(write_note(item, "\n" + body.strip() + "\n"), encoding="utf-8")
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
        (WORK / f'{item["id"]}.md').write_text(write_note(item, body), encoding="utf-8")
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
                                 f"these — so no work list could see them.\n"), encoding="utf-8")
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
                f"the count.\n"), encoding="utf-8")
            written += 1
    print(f"migrated {written} work items into {WORK.relative_to(REPO)}")
    return 0


def load() -> list[dict]:
    items = []
    for p in sorted(WORK.glob("*.md")):
        t = p.read_text(encoding="utf-8")
        m = re.search(r"^---\n(.*?)\n---", t, re.S)
        if not m:
            continue
        d = {"_file": p.name}
        for line in m.group(1).splitlines():
            if ":" not in line:
                continue
            k, _, v = line.partition(":")
            v = v.strip().strip('"')
            if v in ("true", "false"):
                v = v == "true"
            elif v.startswith("["):
                v = [x.strip() for x in v.strip("[]").split(",") if x.strip()]
            d[k.strip()] = v
        # The note BODY, so check() can catch the status written a second time in the H1 heading.
        d["_body"] = t[m.end():]
        items.append(d)
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
    return [] if not missing else [
        f"kb/Work.base 'Fix next' does not select on {missing} — an item whose only harm flag is one of those "
        f"is invisible in the view while work.py next ranks it. Update the filter to match HARM_FLAGS."]


def check() -> int:
    if not WORK.exists():
        print(f"⛔ {WORK.relative_to(REPO)} does not exist — run: python scripts/spec/work.py migrate")
        return 1
    items, bad = load(), []
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
    ids = [it.get("id") for it in items]
    for i in set(ids):
        if ids.count(i) > 1:
            bad.append(f"duplicate id {i}")
    bad += check_base_agrees()
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


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["migrate", "check", "next", "stats"])
    ap.add_argument("--top", type=int, default=3)
    a = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if a.cmd == "migrate":
        return migrate()
    if a.cmd == "check":
        return check()
    items = load()
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
    sys.exit(main())
