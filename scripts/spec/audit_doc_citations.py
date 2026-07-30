#!/usr/bin/env python3
"""Find INHERITED citations in the docs — a quoted fragment that is real, but filed under the wrong clause.

    python scripts/spec/audit_doc_citations.py            # the precise check (misfiled citations only)
    python scripts/spec/audit_doc_citations.py --all      # also list quotes found nowhere (noisy; see below)

⛔ WHAT THIS LOOKS FOR, AND WHY IT IS NARROW ON PURPOSE. CLAUDE.md rule 1: "The failure mode is not inventing a
citation, it is INHERITING one — a queue entry or design doc carries a §, its quoted text is genuinely in the
standard, and the clause NUMBER is never re-derived before it propagates." That defect has an exact mechanical
signature: the quoted text IS in the spec, and it is NOT in the clause the doc names.

⚠ That signal is SHARP, not perfect, and the first run proved it — do not treat a hit as a defect without reading
the line. `--find` reports the first clause containing the text, and a phrase can legitimately appear in several;
a doc may also name two clauses in one sentence, or quote a defined TERM rather than spec prose. The pattern below
is calibrated against both, and every remaining hit still deserves a human glance before an edit.

⚠ THE BROAD VERSION OF THIS CHECK IS WORTHLESS, and it was written first. Matching every `§N.N … "quoted text"`
pair and demanding the text be inside that clause reported 133 failures out of 183, essentially all of them its
own fault: quotes captured across a markdown blockquote marker, paraphrases attributed to a clause, quoted LABELS
(`"shall-not-with-APPLY-COMMIT"`, `"element; para; line"`) that were never claims about spec text at all, and
`§1.6`-style references to a DOC's own sections. A doc uses quotation marks for both quoting and naming, and
nothing in the text distinguishes them — so that check cannot be made precise and is not offered as a gate.
It survives behind `--all`, clearly labelled, because the residue is worth eyeballing occasionally.

This is the same lesson the figure audits taught (`plan §0`: "MY CHECKERS WERE BUGGIER THAN THE TRANSCRIPTION" —
76 findings went to 1 as three tool bugs came out). Confirm a measured defect before changing anything.

FOUND BY THE FIRST RUN (all corrected in the same change set):
  · `COBOLNET_DESIGN.md` cited "§12.3.7 GR7 k3 … distinct ascending" TWICE — a phrase that appears NOWHERE in the
    standard. A Phase-B agent inherited it into a fix-queue finding. The real rule is §12.3.7.4 GR7 1.3.
  · `COBOLNET_REDEFINES_DESIGN.md` cited §13.18.60 for GR4/GR11 text really in §13.18.60.4 — one level short,
    the CA37/CA38 shape.
  · `PHASE-11-scout-notes.md` cited §13.18.62 for text really in §13.18.63.4 GR4c.
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
CITE = REPO / "scripts" / "spec" / "cite.py"

#: A citation we can check: a clause with at least two dots (an ISO clause shape, not a doc's own §4), followed on
#: the SAME line by a quoted lower-case fragment long enough to be prose rather than a label.
#:
#: ⚠ Two calibrations, each earned by a false positive on the first run:
#:  · `[^§"“\n]` between the clause and the quote — NOT `[^"“\n]`. A sentence may name two clauses before it
#:    quotes ("never the §13.18.38 window — §14.2.3 GR8 says …"), and the citation being made is the NEAREST one.
#:    The greedy form blamed the wrong clause and reported a CORRECT citation as misfiled.
#:  · a 30-character floor, not 18. Below that a quote is usually a defined TERM rather than spec prose
#:    ("implementor-defined", "the end of the PERFORM"), and a term legitimately appears in many clauses — so
#:    --find locating it elsewhere says nothing about the clause the doc named.
PAT = re.compile(r'§(?P<cl>\d+(?:\.\d+){2,})[^§"“\n]{0,80}["“](?P<q>[a-z][^"”\n]{30,90})["”]')

#: Marks that a "quote" is elided, marked up, or otherwise not verbatim — never a checkable citation.
NOT_VERBATIM = ("…", ">", "*", "`")


def cite(*args: str) -> tuple[int, str]:
    r = subprocess.run([sys.executable, str(CITE), *args],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    return r.returncode, (r.stdout or r.stderr).strip()


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    show_all = "--all" in sys.argv

    docs = sorted(REPO.joinpath("docs").rglob("*.md")) + [REPO / "CLAUDE.md"]
    # ⛔ SOURCE DOC-COMMENTS TOO. Scanning only docs/ measures half the prose: an XML comment above the
    # implementing method carries a citation at the same density and with the same authority as the design doc,
    # and is read by the same person for the same reason. PB3 proved it — the fabricated "GR7 k3" survived a full
    # docs/ sweep in THIRTEEN places across five source files, including the comments on the one implementation
    # that gets the rule right.
    docs += sorted(REPO.joinpath("src").rglob("*.cs")) + sorted(REPO.joinpath("src").rglob("*.g4"))
    seen: set[tuple[str, str]] = set()
    misfiled: list[tuple[str, str, str, str]] = []
    absent: list[tuple[str, str, str]] = []
    ok = 0

    for doc in docs:
        try:
            text = doc.read_text(encoding="utf-8")
        except OSError:
            continue
        for m in PAT.finditer(text):
            clause, quote = m.group("cl"), m.group("q")
            if any(t in quote for t in NOT_VERBATIM) or (clause, quote) in seen:
                continue
            seen.add((clause, quote))
            if cite("--check", clause, quote)[0] == 0:
                ok += 1
                continue
            code, out = cite("--find", quote)
            head = out.splitlines()[0].strip() if out else ""
            rel = str(doc.relative_to(REPO)).replace("\\", "/")
            if code == 0 and head.startswith("§"):
                misfiled.append((rel, clause, quote, head))
            else:
                absent.append((rel, clause, quote))

    print(f"{ok + len(misfiled) + len(absent)} verbatim-shaped citations checked · {ok} correct")
    print(f"⛔ {len(misfiled)} MISFILED (the text is real, the clause is wrong) — these are defects\n")
    for rel, clause, quote, head in misfiled:
        print(f"  {rel}\n     says §{clause}  \"{quote[:70]}\"\n     really {head}\n")

    if show_all:
        print(f"\n— {len(absent)} quote(s) not found anywhere in the spec. MOSTLY NOT DEFECTS: a doc quotes labels")
        print("  and paraphrases as well as spec text, and nothing distinguishes them mechanically. Eyeball only.\n")
        for rel, clause, quote in absent:
            print(f"  {rel}: §{clause} \"{quote[:70]}\"")

    return 1 if misfiled else 0


if __name__ == "__main__":
    sys.exit(main())
