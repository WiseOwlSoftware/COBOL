#!/usr/bin/env python3
"""Pre-publication check for the spec transcription: no per-copy licence data, and the acknowledgment present.

WHY THIS EXISTS. `specs/ISO_COBOL.md` is published; the PDF it was transcribed from is NOT. Two things therefore
have to be true of the Markdown before it leaves the private submodule, and neither is guaranteed by any other
gate:

  1. **It carries no per-copy licence stamp.** Every page of the source PDF is stamped by the reseller with the
     purchaser's name, order number and download date — "Licensed to …. Single user license only. Copying and
     networking prohibited." That is not part of ISO/IEC 1989:2023: it is applied per purchaser, it is personally
     identifying, and its terms are the reseller's, not the standard's. The OCR pass picked it up once, at the
     end of the cover page, where it had been concatenated onto the genuine "© ISO/IEC 2023" line. One
     occurrence in 53,000 lines is exactly the kind of thing a human review misses and a grep does not.

  2. **The ISO acknowledgment opens the file.** Page 28 of the standard grants reproduction "in whole or in part
     … for any other purpose" and asks that the acknowledgment paragraphs be reproduced "in their entirety as
     part of the preface to any such publication". That request is the condition the transcription is published
     under.

DELIBERATELY RUNS WITHOUT THE PDF. `verify_acknowledgment.py` checks the acknowledgment WORD FOR WORD against the
printed page, and needs the PDF to do it. This check needs to run in the public repository, where the PDF is
absent by design — so it verifies presence and structure only, and defers fidelity to the other gate. The two are
complements, not duplicates.

    python scripts/spec/verify_publishable.py
"""
from __future__ import annotations

import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

# The reseller's per-copy stamp, in pieces, so a partial survival is caught as readily as the whole line.
FORBIDDEN = [
    (r"Licensed to\b", "per-copy licence stamp"),
    (r"Single user licen[cs]e", "per-copy licence stamp"),
    (r"Copying and networking", "per-copy licence stamp"),
    (r"ANSI order\b", "reseller order reference"),
    (r"\bX_\d{6}\b", "reseller order number"),
    (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
]

# Must be present. The acknowledgment's own opening words, and the preface framing that marks our editorial
# matter as ours.
REQUIRED = [
    (r"^#\s*Preface", "the file must open with the Preface (page 28: 'as part of the preface')"),
    (r"COBOL is an industry language and is not the property of any company",
     "the acknowledgment's first paragraph"),
    (r"No warranty, expressed or implied, is made by any contributor",
     "the acknowledgment's no-warranty paragraph"),
    (r"have specifically authorized the use of this material in whole or in part",
     "the acknowledgment's authorization paragraph"),
    (r"NOT part of ISO/IEC 1989:2023",
     "the Preface must state that it is not part of the standard"),
]


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not SPEC_MD.exists():
        sys.exit(f"FATAL: {SPEC_MD} not found")
    text = SPEC_MD.read_text(encoding="utf-8")
    lines = text.splitlines()

    failures = []
    print("per-copy licence data — must be ABSENT:")
    for pat, what in FORBIDDEN:
        hits = [i + 1 for i, l in enumerate(lines) if re.search(pat, l)]
        if hits:
            failures.append(f"{what} ({pat}) at line(s) {hits[:5]}")
            print(f"  ✗ {pat:<34} FOUND at {hits[:5]}")
        else:
            print(f"  ✓ {pat:<34} absent")

    print("\nacknowledgment and preface — must be PRESENT:")
    for pat, what in REQUIRED:
        if re.search(pat, text, re.M):
            print(f"  ✓ {what}")
        else:
            failures.append(f"missing: {what}")
            print(f"  ✗ MISSING — {what}")

    # Every correction must be both FLAGGED where it was made and LISTED in the Addendum, and the two sets must
    # agree. The Addendum exists so a correction can be reversed if it later proves mistaken; that only works
    # while the cross-references hold, and a note or an entry can be edited away independently of the other.
    print("\ncorrections — every ⚠ flag must match an Addendum entry:")
    referenced = set(re.findall(r"see the Addendum \((C\d+)\)", text))
    defined = set(re.findall(r"^###\s+(C\d+)\s+·", text, re.M))
    if not defined:
        failures.append("the Addendum lists no corrections (### Cn entries)")
        print("  ✗ no correction entries found in the Addendum")
    for cid in sorted(referenced | defined):
        if cid in referenced and cid in defined:
            print(f"  ✓ {cid}: flagged in place and listed in the Addendum")
        elif cid in defined:
            failures.append(f"{cid} is listed in the Addendum but nothing in the text points at it")
            print(f"  ✗ {cid}: in the Addendum, but never referenced from the text")
        else:
            failures.append(f"{cid} is referenced from the text but missing from the Addendum")
            print(f"  ✗ {cid}: referenced from the text, but MISSING from the Addendum")

    # The acknowledgment has to be in the preface, i.e. before the body starts, not only in position at page 28.
    cut = text.find('<a id="page-1"></a>')
    if cut < 0:
        failures.append("the page-1 anchor is missing; cannot locate the preface")
    elif "COBOL is an industry language" not in text[:cut]:
        failures.append("the acknowledgment appears in the body but NOT in the preface")
        print("  ✗ the acknowledgment is not in the preface (it must precede the page-1 anchor)")
    else:
        print("  ✓ the acknowledgment precedes the body, as the preface")

    if failures:
        print("\n".join(["", "FATAL — not publishable:"] + [f"  - {f}" for f in failures]))
        return 1
    print(f"\nPUBLISHABLE — {len(lines):,} lines, no per-copy licence data, acknowledgment in the preface")
    print("(word-for-word fidelity of the acknowledgment is verify_acknowledgment.py, which needs the PDF)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
