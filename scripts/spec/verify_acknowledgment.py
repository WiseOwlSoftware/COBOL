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

    python scripts/spec/verify_acknowledgment.py [--edition 1985|2002|2023]
    python scripts/spec/verify_acknowledgment.py --self-test       # planted text, no PDF

The acknowledgment's first and last words, the PDF page that prints it and the shape of the per-copy stamp to strip from
that page are keyed by edition (ACKNOWLEDGMENT); an edition with no row exits naming that table, never borrowing 2023's
(kb/Work PB4815).
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys
from typing import NamedTuple
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)


class Acknowledgment(NamedTuple):
    """Where one edition prints its acknowledgment, how it opens and closes, and the per-copy stamp shape on that page."""
    pdf_page: int       # the PDF page that prints it (1-based)
    first: str          # its first words
    last: str           # its last words
    stamp: str          # the shape of the reseller's per-copy stamp, removed from the printed page before comparing


# The acknowledgment runs from the first paragraph to the end of the authorization sentence. The trademark
# footnotes are attached to it and are carried too. Keyed by edition: 1985 prints the CODASYL grant on PDF page 3 and
# 2002 prints none (PB4812, PB4813), so their rows are read from those PDFs when their runs begin.
ACKNOWLEDGMENT: dict[int, Acknowledgment] = {
    2023: Acknowledgment(28, "COBOL is an industry language and is not the property of any company",
                         "programming manuals or similar publications.", r"Licensed to .*?prohibited\."),
}


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


def main(edition: int = spec_pdf.DEFAULT_EDITION) -> int:
    ack = spec_pdf.edition_data(ACKNOWLEDGMENT, edition, "acknowledgment text (verify_acknowledgment.ACKNOWLEDGMENT)")
    first, last = ack.first, ack.last
    pdf = spec_pdf.require(edition)
    import fitz

    md = spec_pdf.markdown(edition).read_text(encoding="utf-8")
    if not md.lstrip().startswith("# Preface"):
        sys.exit("FATAL: the file does not open with the Preface — page 28 asks for the acknowledgment "
                 "'as part of the preface', so it has to come first")

    # Both copies are delimited by the DOCUMENT STRUCTURE, not by page anchors: pages have been removed from
    # the transcription (a layout artifact of one typesetting, meaningless in Markdown), so the front matter
    # ends at clause 1 and the in-position copy lives in the Introduction.
    # 1 — the preface copy: everything before the first clause.
    cut = md.find('<a id="section-1"></a>')
    if cut < 0:
        sys.exit("FATAL: the clause-1 anchor is missing; cannot delimit the front matter")
    preface = slice_between(md[:cut], first, last, "preface")

    # 2 — the in-position copy, inside the Introduction.
    intro = md.find("\n## Introduction")
    if intro < 0:
        sys.exit("FATAL: the Introduction heading is missing; cannot locate the in-position acknowledgment")
    inplace = slice_between(md[intro:cut], first, last, "the Introduction")

    # 3 — the printed text.
    doc = fitz.open(pdf)
    printed_raw = re.sub(ack.stamp, " ", doc[ack.pdf_page - 1].get_text(), flags=re.S)
    printed = slice_between(printed_raw, first, last, "the PDF")

    ok = True
    for label, got in (("preface vs printed", preface), ("Introduction vs printed", inplace)):
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


def self_test() -> int:
    """The edition plumbing and the slicing, on a PLANTED text: no PDF and no transcription are read."""
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(("  ok    " if cond else "  FAIL  ") + name)
        if not cond:
            failures.append(name)

    ack = ACKNOWLEDGMENT[2023]
    planted = f"front {ack.first} and the middle,\n<sup>1</sup> a footnote {ack.last} then the body"
    got = slice_between(planted, ack.first, ack.last, "planted")
    check("the acknowledgment is sliced from its first words to its last, across a line wrap and a footnote marker",
          got.startswith(ack.first) and got.endswith(ack.last) and "1 a footnote" in got)
    stamped = f"x Licensed to someone, single copy. Copying prohibited. y {ack.first}"
    check("the stamp shape of the edition is removed before the comparison",
          ack.first in re.sub(ack.stamp, " ", stamped, flags=re.S) and "Licensed" not in re.sub(ack.stamp, " ", stamped, flags=re.S))
    for edition in (1985, 2002):
        try:
            main(edition)
            check(f"{edition} with no acknowledgment row exits", False)
        except SystemExit as ex:
            check(f"{edition} with no row exits naming the table and the edition, before it reads any file",
                  str(edition) in str(ex.code) and "verify_acknowledgment.ACKNOWLEDGMENT" in str(ex.code))
    print(f"=== VERIFY ACKNOWLEDGMENT SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the planted-text self-test")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    sys.exit(self_test() if args.self_test else main(args.edition))
