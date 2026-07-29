#!/usr/bin/env python3
"""Validate a spec citation against `specs/ISO_COBOL.md` — the mechanical check for CLAUDE.md rule 1.

WHY THIS EXISTS. Rule 1 requires the exact §/GR for any semantics question, and the failure mode is not
inventing a citation — it is INHERITING one. A fix-queue entry or design doc carries "§8.4.1.2 GR2", the text
it quotes really does say what it claims, and the clause NUMBER is never re-derived; it then propagates into
code comments, goldens and DEVLOG entries as though it had been checked. Two of the CA10 citations were wrong
this way (the real clauses are §8.4.2.3.4 GR2 and §13.18.38.4 GR7) and nothing would have caught them.

⛔ IT READS THE MARKDOWN, NOT `spec-rule-catalog.json`. The catalog is derived and has two properties that make
it the wrong oracle here: it has no block at all for a clause whose rules are normative PROSE with lettered
items (§14.8.2.3.3 is one — checking against the catalog reports a real citation as false), and its `ordinal`
is a per-block count that does NOT track the printed rule number once a rule has sub-letters (the abs rule
prints as 6)d)2.b) and the catalog calls it 2). Both produce confident wrong answers.

    python scripts/spec/cite.py --check 8.4.2.3.4 "EC-BOUND-SUBSCRIPT exception condition is set to exist"
    python scripts/spec/cite.py --find  "shall fall within the bounds from integer-1"

`--check` exits non-zero unless the text appears INSIDE that clause's own region, so a wrong clause number
cannot pass quietly. That is the load-bearing guarantee. Both modes also print a best-effort RULE PATH
(`6) d)`) to help you write the ordinal — ⚠ it is APPROXIMATE below two levels of nesting, because the standard
mixes `a)` and `b.` styles at the same depth; read the printed rule when the ordinal itself is load-bearing.
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"

# A clause number is either the body's dotted decimal (14.9.39.4) or an ANNEX's letter-headed form
# (E.2, A.4.14, D.2.2.5.1). The annex alternative requires at least one dot segment, which is what keeps it
# from swallowing the annex TITLE headings ("## Annex A") — those name the annex, they are not a clause.
# ⛔ Without the second alternative the whole of Annexes A–G is uncitable: `--check E.2 …` answers
# "there is no clause §E.2", which reads as a bad citation rather than a blind tool.
# The TITLE is optional: all 178 headings of clause 3 (Terms and definitions) are a bare number, the term
# itself being the bold line beneath. Requiring a title made every definition in the standard uncitable.
HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+(.*?))?\s*$")
# The transcription escapes a rule label's delimiter (`1\)`) so Markdown does not eat it as a list — see
# repairs/rule_numbering.py. Match both forms so this works on any revision.
TOP = re.compile(r"^(\d+)\\?\)\s")
SUB = re.compile(r"^\s{2,}([a-z])\\?\)\s")
SUBSUB = re.compile(r"^\s{2,}(\d+)\\?\.\s")


def norm(s: str) -> str:
    """Compare on words only — dashes, quotes and spacing are typography, not content."""
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", s)).strip().lower()


def clauses(lines):
    """(number, title, start, end) for every clause heading, in order."""
    heads = [(m.group(1), m.group(2) or "", i) for i, l in enumerate(lines) if (m := HEADING.match(l))]
    return [(n, t, s, heads[k + 1][2] if k + 1 < len(heads) else len(lines))
            for k, (n, t, s) in enumerate(heads)]


def rule_path(lines, at, start):
    """The PRINTED rule path of the line at `at` — e.g. ['6)', 'd)', '2.'] — by walking back to each level."""
    path, seen_sub, seen_subsub = [], False, False
    for k in range(at, start - 1, -1):
        if not seen_subsub and (m := SUBSUB.match(lines[k])):
            path.append(f"{m.group(1)}."); seen_subsub = True
        elif not seen_sub and (m := SUB.match(lines[k])):
            path.append(f"{m.group(1)})"); seen_sub = True
        elif (m := TOP.match(lines[k])):
            path.append(f"{m.group(1)})")
            break
    return " ".join(reversed(path))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", nargs=2, metavar=("CLAUSE", "TEXT"),
                    help="assert TEXT appears inside CLAUSE's own region; exit 1 if not")
    ap.add_argument("--find", metavar="TEXT", help="which clause(s) contain this text")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8").splitlines()
    cls = clauses(lines)

    if args.find:
        needle = norm(args.find)
        hits = [(n, t, s, e, i) for (n, t, s, e) in cls
                for i in range(s, e) if needle in norm(lines[i])]
        if not hits:
            print(f"NO CLAUSE contains {args.find!r} — the citation names text the standard does not have")
            return 1
        for n, t, s, _e, i in hits[:12]:
            print(f"§{n} {rule_path(lines, i, s)}  ({t})\n    {lines[i].strip()[:190]}\n")
        print(f"{len(hits)} occurrence(s)")
        return 0

    if not args.check:
        ap.print_help()
        return 2

    clause, text = args.check
    region = [c for c in cls if c[0] == clause]
    if not region:
        print(f"FAIL: there is no clause §{clause} in the transcription")
        return 1
    needle = norm(text)
    for n, t, s, e in region:
        for i in range(s, e):
            if needle in norm(lines[i]):
                print(f"OK  §{n} {rule_path(lines, i, s)}  ({t})\n    {lines[i].strip()[:220]}")
                return 0
    print(f"FAIL: §{clause} exists but does NOT contain {text!r}")
    print("      run --find to locate the text and get the clause it is really in")
    return 1


if __name__ == "__main__":
    sys.exit(main())
