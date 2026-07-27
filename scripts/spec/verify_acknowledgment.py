#!/usr/bin/env python3
"""Verify that the ISO acknowledgment paragraphs are reproduced verbatim, in the preface and in place.

WHY THIS IS A GATE AND NOT A CONVENTION. ISO/IEC 1989:2023's Introduction (page 28) grants permission to
reproduce the standard "in whole or in part … for any other purpose", and attaches one request: that the
acknowledgment paragraphs be reproduced "in their entirety as part of the preface to any such publication". Those
paragraphs are therefore the condition on which this transcription may be distributed at all. They are not
ordinary prose that can be tidied, reflowed, or summarised, and a well-meaning edit that drops a clause is a
different class of mistake from a typo.

WHAT IS CHECKED, all three against each other:
  1. the paragraphs in the file's PREFACE,
  2. the same paragraphs transcribed IN POSITION at page 28,
  3. the PRINTED TEXT, read from the PDF.

Comparing 1 and 2 to each other would only prove they agree with one another; the PDF is what makes it a check
of FIDELITY rather than of internal consistency.

Whitespace and line wrapping are normalised — those are typesetting, not text. Everything else must match
exactly, and any difference fails loudly.

    python scripts/spec/verify_acknowledgment.py
"""
from __future__ import annotations

import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# The acknowledgment runs from the first paragraph to the end of the authorization sentence. The trademark
# footnotes are attached to it and are carried too.
FIRST = "COBOL is an industry language and is not the property of any company"
LAST = "programming manuals or similar publications."


def norm(s: str) -> str:
    """Collapse whitespace and strip transcription markup; keep every word and punctuation mark."""
    s = re.sub(r"<sup>\s*(\d+)\s*</sup>", r"\1", s)      # footnote markers survive as bare digits
    s = re.sub(r"[*_`>]", " ", s)                        # markdown emphasis / blockquote markers
    s = s.replace("—", "—").replace("’", "’")
    return " ".join(s.split())


def slice_between(text: str, first: str, last: str, label: str) -> str:
    """Normalise FIRST, then slice. Searching the raw text fails whenever a marker straddles a line break —
    which it does in all three sources, since each wraps at a different column."""
    text = norm(text)
    i = text.find(first)
    if i < 0:
        sys.exit(f"FATAL: {label}: the acknowledgment does not start where expected ({first[:40]!r} not found)")
    j = text.find(last, i)
    if j < 0:
        sys.exit(f"FATAL: {label}: the acknowledgment does not end where expected ({last[:40]!r} not found)")
    return text[i:j + len(last)]


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
             "git submodule update --init specs-private")
    import fitz

    md = SPEC_MD.read_text(encoding="utf-8")
    if not md.lstrip().startswith("# Preface"):
        sys.exit("FATAL: the file does not open with the Preface — page 28 asks for the acknowledgment "
                 "'as part of the preface', so it has to come first")

    # 1 — the preface copy: everything before the page-1 anchor.
    cut = md.find('<a id="page-1"></a>')
    if cut < 0:
        sys.exit("FATAL: the page-1 anchor is missing; cannot delimit the preface")
    preface = slice_between(md[:cut], FIRST, LAST, "preface")

    # 2 — the in-position copy, inside the page-28 slice.
    p28 = md.find('<a id="page-28"></a>')
    p29 = md.find('<a id="page-29"></a>')
    if p28 < 0 or p29 < 0:
        sys.exit("FATAL: the page-28/29 anchors are missing")
    inplace = slice_between(md[p28:p29], FIRST, LAST, "page 28")

    # 3 — the printed text.
    doc = fitz.open(PDF)
    printed_raw = re.sub(r"Licensed to .*?prohibited\.", " ", doc[27].get_text(), flags=re.S)
    printed = slice_between(printed_raw, FIRST, LAST, "the PDF")

    ok = True
    for label, got in (("preface vs printed", preface), ("page 28 vs printed", inplace)):
        if got == printed:
            print(f"  ✓ {label}: verbatim ({len(got)} chars)")
        else:
            ok = False
            print(f"  ✗ {label}: DIFFERS")
            for i, (a, b) in enumerate(zip(got, printed)):
                if a != b:
                    print(f"      first difference at char {i}:")
                    print(f"        file    …{got[max(0, i - 60):i + 60]}…")
                    print(f"        printed …{printed[max(0, i - 60):i + 60]}…")
                    break
            else:
                print(f"      one is a prefix of the other: file={len(got)} printed={len(printed)} chars")
    if not ok:
        sys.exit("\nFATAL: the acknowledgment is not reproduced verbatim. Page 28 asks for it 'in their "
                 "entirety'; it is the condition this transcription is distributed under.")
    print("\nacknowledgment reproduced verbatim in the preface and in position — distribution condition met")
    return 0


if __name__ == "__main__":
    sys.exit(main())
