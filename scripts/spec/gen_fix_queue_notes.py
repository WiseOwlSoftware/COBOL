#!/usr/bin/env python3
"""Turn the prose fix queue into an ADDRESSABLE work list — Obsidian notes + a machine-readable index.

⛔ WHY THIS EXISTS. `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` is 2,400+ lines of forensic prose, and the
prose is worth keeping: each entry carries the measured repro, the citation and the reason a previous summary was
wrong. What it is NOT is answerable. Working out "which defects are open" required a regex over the whole file,
every session — and the answer was chosen badly: on 2026-08-04 the session picked **PB39** (catalog rule-ids that
do not match the standard's numbering — zero wrong answers) over **PB24** (`FUNCTION LENGTH` silently wrong on
four shapes). Both are `[MAJOR]`, so severity could not separate them.

⭐ **SEVERITY IS THE WRONG KEY. THE KEY IS WHAT THE DEFECT DOES TO A USER'S PROGRAM.** This project's own history
says so: PB5 returned 9223372036.85 for a money value, PB13 returned a plausible 31-digit number, PB22 returned a
genuinely valid date, PB33 returned an Int128 saturation artifact. Every one looked like an answer. So each entry
carries a `flags:` line and the views rank on it:

    wrong-answer          returns an incorrect value for legal source
    crashes               aborts at run time, or escapes as a raw CLR/Roslyn error, on legal source
    silent                no diagnostic — pairs with the two above and is what makes them dangerous
    rejects-legal-source  refuses a conforming program
    under-rejects         accepts non-conforming source (a missing screen) — real, but nobody gets a wrong answer
    process               tooling, denominator or id-shape only; no user-visible defect

`FIX NEXT` is `status=open AND (wrong-answer OR crashes) AND not blocked` — which puts PB24 first and drops PB39
off the list entirely, i.e. it would have prevented the actual misprioritisation.

The queue file stays the authored SSOT; this only parses it. Nothing here invents state.

    python scripts/spec/gen_fix_queue_notes.py            # write kb/Fixes/ + the JSON index
    python scripts/spec/gen_fix_queue_notes.py --check    # non-zero if the notes on disk are stale
    python scripts/spec/gen_fix_queue_notes.py --next     # print the top of FIX NEXT (used by session-probe)
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import shutil
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
QUEUE = REPO / "docs" / "rearchitecture" / "CONFORMANCE-FIX-QUEUE.md"
OUT = REPO / "kb" / "Fixes"
INDEX = REPO / "docs" / "rearchitecture" / "fix-queue-index.json"

HEAD = re.compile(
    r"^### (?P<id>PB\d+)\s*·\s*\[(?P<sev>[A-Z]+)\]\s*·\s*(?P<area>[^·]+?)\s*·\s*(?P<rest>.*)$", re.M)
FLAGS = re.compile(r"^>\s*⚙\s*\*\*flags:\*\*\s*(?P<flags>.+)$", re.M)
BLOCKED = re.compile(r"blocked-by:\s*(PB\d+)")
CLAUSE = re.compile(r"§(\d+(?:\.\d+)*)")

KNOWN_FLAGS = {"wrong-answer", "crashes", "silent", "rejects-legal-source", "under-rejects", "process"}


def status_of(rest: str) -> str:
    """⛔ MATCH ON THE PHRASE "HALF LANDED", NOT ON ⚠ OR THE WORD "HALF", AND TEST IT FIRST.

    A first cut read ``"⚠" in rest or "HALF" in rest`` as *half* and misfiled THREE COMPLETED items — PB1, PB14
    and PB32 — as still open, because a landed entry routinely narrates the half it closed: *"LANDED 2026-08-03
    (the CS1503 half; the wrong-VALUE half is now PB38)"*. Putting finished work back on the work list is the one
    failure mode a work list must not have, and it is the exact mistake this script exists to stop. The genuinely
    partial entries announce themselves as "HALF LANDED"; ⚠ means only that the heading carries a caution, which
    most good entries do.
    """
    up = rest.upper()
    if "HALF LANDED" in up:
        return "half"
    if "⚖" in rest or "NEEDS-OWNER-DECISION" in up:
        return "owner"
    if "✅" in rest or "LANDED" in up or "RETIRED" in up:
        return "landed"
    return "open"


def parse() -> list[dict]:
    text = QUEUE.read_text(encoding="utf-8")
    ms = list(HEAD.finditer(text))
    if not ms:
        sys.exit(f"no PB entries parsed from {QUEUE} — the heading format changed; this script must be updated "
                 f"rather than silently reporting an empty queue")
    items: list[dict] = []
    for i, m in enumerate(ms):
        body = text[m.end(): ms[i + 1].start() if i + 1 < len(ms) else len(text)]
        fm = FLAGS.search(body)
        flags = [f.strip() for f in fm.group("flags").split("·")] if fm else []
        flags = [f for f in flags if not f.startswith("blocked-by")]
        rest = m.group("rest")
        items.append({
            "id": m.group("id"),
            "severity": m.group("sev"),
            "area": m.group("area").strip(),
            "status": status_of(rest),
            "summary": re.sub(r"[⛔✅⚠⚖◑]", "", rest).strip(" —-"),
            "flags": sorted(set(flags)),
            "blocked_by": sorted(set(BLOCKED.findall(body))),
            "spec_refs": sorted({c for c in CLAUSE.findall(body)})[:12],
        })
    return items


def unknown_flags(items: list[dict]) -> set[str]:
    return {f for it in items for f in it["flags"]} - KNOWN_FLAGS


def fix_next(items: list[dict]) -> list[dict]:
    """The one view that matters — see the module docstring for why it is not keyed on severity."""
    sev = {"BLOCKER": 0, "MAJOR": 1, "MINOR": 2, "OWNER": 3}
    live = [i for i in items
            if i["status"] in ("open", "half")
            and ({"wrong-answer", "crashes"} & set(i["flags"]))
            and not i["blocked_by"]]
    return sorted(live, key=lambda i: (sev.get(i["severity"], 9), int(i["id"][2:])))


def build(items: list[dict]) -> dict[str, str]:
    notes: dict[str, str] = {}
    for it in items:
        fm = [
            "---",
            f'title: "{it["id"]} — {it["summary"][:70]}"',
            f'id: {it["id"]}', f'severity: {it["severity"]}', f'area: "{it["area"]}"',
            f'status: {it["status"]}',
            # Booleans rather than a list, because Bases filters on scalar properties far more simply than on
            # array membership, and these five questions are exactly what the views ask.
            f'wrong_answer: {str("wrong-answer" in it["flags"]).lower()}',
            f'crashes: {str("crashes" in it["flags"]).lower()}',
            f'silent: {str("silent" in it["flags"]).lower()}',
            f'rejects_legal_source: {str("rejects-legal-source" in it["flags"]).lower()}',
            f'under_rejects: {str("under-rejects" in it["flags"]).lower()}',
            f'process_only: {str("process" in it["flags"]).lower()}',
            f'blocked: {str(bool(it["blocked_by"])).lower()}',
            f'blocked_by: [{", ".join(it["blocked_by"])}]',
            "generated: true",
            "tags: [cobolsharp, fix-queue, generated]",
            "---", "",
            f'# {it["id"]} — {it["summary"]}', "",
            "> ⚙ **Generated** from `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md`, which stays the authored",
            "> source. Do not edit — edit the queue entry and re-run `scripts/spec/gen_fix_queue_notes.py`.", "",
            f'- **status** {it["status"]} · **severity** {it["severity"]} · **area** {it["area"]}',
            f'- **flags** {", ".join(it["flags"]) or "(none recorded)"}',
        ]
        if it["blocked_by"]:
            fm.append(f'- **blocked by** {", ".join(f"[[kb/Fixes/{b}|{b}]]" for b in it["blocked_by"])}')
        if it["spec_refs"]:
            fm.append(f'- **spec** {", ".join("§" + c for c in it["spec_refs"])}')
        fm += ["", "See [[kb/Fixes/_Fix Next|the ranked work list]] and the full forensic entry in the queue.", ""]
        notes[f'{it["id"]}.md'] = "\n".join(fm)

    nxt = fix_next(items)
    open_n = sum(1 for i in items if i["status"] in ("open", "half"))
    idx = [
        "---", "title: Fix Next",
        f'description: "{len(nxt)} unblocked defects that produce a wrong answer or a crash today."',
        f"open: {open_n}", f"actionable: {len(nxt)}", "generated: true",
        "tags: [cobolsharp, fix-queue, generated, dashboard]", "---", "",
        "# Fix next", "",
        "> ⚙ **Generated.** Ranked by what the defect DOES to a user's program, not by its severity label —",
        "> PB24 and PB39 are both `[MAJOR]`, and only one of them returns a wrong answer.", "",
        f"**{open_n} open/half · {len(nxt)} actionable now**", "",
        "| # | item | severity | area | why it is here |", "|---|---|---|---|---|",
    ]
    for n, it in enumerate(nxt, 1):
        why = ", ".join(f for f in it["flags"] if f in {"wrong-answer", "crashes", "silent"})
        idx.append(f'| {n} | [[kb/Fixes/{it["id"]}\\|{it["id"]}]] | {it["severity"]} | {it["area"]} | {why} |')
    idx += ["", "## Deliberately NOT on this list", "",
            "Real work, but nobody gets a wrong answer from them — so they do not outrank the table above:", ""]
    for it in items:
        if it["status"] in ("open", "half") and it not in nxt:
            reason = ("blocked by " + ", ".join(it["blocked_by"])) if it["blocked_by"] else \
                     (", ".join(it["flags"]) or "no flags recorded")
            idx.append(f'- [[kb/Fixes/{it["id"]}|{it["id"]}]] — {reason}')
    idx += ["", "See also [[kb/Fix Queue.base|the sortable Bases view]].", ""]
    notes["_Fix Next.md"] = "\n".join(idx)
    return notes


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="do not write; non-zero if notes on disk are stale")
    ap.add_argument("--next", action="store_true", help="print the top of FIX NEXT and exit")
    ap.add_argument("--top", type=int, default=3)
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not QUEUE.exists():
        sys.exit(f"fix queue not found: {QUEUE}")

    items = parse()
    if bad := unknown_flags(items):
        # ⛔ LOUD, NOT IGNORED. A typo'd flag would silently drop an item out of FIX NEXT, which is the one
        # failure this whole script exists to prevent.
        print(f"⛔ unknown flag(s) in the queue: {sorted(bad)}\n   known: {sorted(KNOWN_FLAGS)}")
        return 1

    if args.next:
        nxt = fix_next(items)
        if not nxt:
            print("next   : (nothing unblocked produces a wrong answer — see kb/Fixes/_Fix Next)")
            return 0
        parts = [f'{i["id"]} ({i["area"]}, {", ".join(f for f in i["flags"] if f != "silent") or "?"})'
                 for i in nxt[:args.top]]
        print("next   : " + " · ".join(parts))
        return 0

    notes = build(items)
    if args.check:
        if not OUT.exists():
            print(f"· {OUT.relative_to(REPO)} does not exist (gitignored build output) — nothing to check.")
            return 0
        disk = {p.name: p.read_text(encoding="utf-8") for p in OUT.glob("*.md")}
        diff = sorted(set(notes) ^ set(disk)) + sorted(n for n in set(notes) & set(disk) if notes[n] != disk[n])
        if not diff:
            print(f"✓ {len(notes)} fix-queue notes match the queue exactly")
            return 0
        print("⛔ THE FIX-QUEUE VIEW IS STALE — it does not match the queue it summarises.")
        for n in diff[:20]:
            print(f"      ~ {n}")
        print("   Run: python scripts/spec/gen_fix_queue_notes.py")
        return 1

    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)
    for name, text in notes.items():
        (OUT / name).write_text(text, encoding="utf-8")
    INDEX.write_text(json.dumps(items, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    nxt = fix_next(items)
    print(f"wrote {len(notes)} notes into {OUT.relative_to(REPO)} and {INDEX.relative_to(REPO)}")
    print(f"  {len(items)} items · {sum(1 for i in items if i['status'] in ('open','half'))} open/half · "
          f"{len(nxt)} actionable now")
    for it in nxt[:5]:
        print(f"      {it['id']:<6} {it['severity']:<8} {it['area']:<22} {', '.join(it['flags'])}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
