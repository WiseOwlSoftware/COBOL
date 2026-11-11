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
  2. the same paragraphs transcribed IN POSITION (2023: its Introduction, PDF page 28; 1985: PDF page 3; 2014: PDF page 2),
  3. the PRINTED TEXT, read from the PDF.

Comparing 1 and 2 to each other would only prove they agree with one another; the PDF is what makes it a check
of FIDELITY rather than of internal consistency.

Whitespace and line wrapping are normalised — those are typesetting, not text. Everything else must match
exactly, and any difference fails loudly.

    python scripts/spec/verify_acknowledgment.py [--edition 1985|2002|2014|2023]
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
    in_position: str    # the line of the transcription that opens the region holding the in-position copy
    misread: tuple[tuple[str, str], ...] = ()
    """A SCANNED page's text layer is OCR, not the printed text: each pair is one misreading of that layer (the
    normalised text it reads, then what the rendered page prints), every one confirmed against the rendered page and
    applied before comparing. Whatever else differs still fails. A vector page's text layer is the printed text and
    carries none."""


# The acknowledgment runs from the first paragraph to the end of the authorization sentence. The trademark
# footnotes are attached to it and are carried too. Keyed by edition: 2002 prints none (PB4813). 1985 prints the CODASYL
# grant on PDF page 3 (kb/Work PB4850): the same paragraphs as 2023 with the 1985 punctuation (no period after
# "therewith" or before "Such"), and the scan's text layer misreads "or" as "OI", drops the space in "1958, 1959" and
# breaks two words at a line-end hyphen; the rendered page prints each as the second string of its pair.
ACKNOWLEDGMENT: dict[int, Acknowledgment] = {
    1985: Acknowledgment(3, "COBOL is an industry language and is not the property of any company",
                         "programming manuals or similar publications.", r"Copyright by INCITS\..*?prohibited\.", "## Page 3",
                         ((" OI ", " or "), ("1958,1959", "1958, 1959"), ("copy- righted", "copyrighted"),
                          ("ex- tends", "extends"))),
    # 2014 prints the grant and the paragraphs on its INCITS cover page, PDF page 2 (kb/Work PB5268), as true text: "OI"
    # for "or", "1958,1959", "Translater" and the missing full stops after "therewith" and "specifications" are on the
    # rendered page itself (looked at 2026-11-11), so they are the printed text and no misreading is listed.
    2014: Acknowledgment(2, "COBOL is an industry language and is not the property of any company",
                         "programming manuals OI similar publications.", r"Copyright by INCITS\..*?prohibited\.",
                         "## Page 2"),
    2023: Acknowledgment(28, "COBOL is an industry language and is not the property of any company",
                         "programming manuals or similar publications.", r"Licensed to .*?prohibited\.",
                         "## Introduction"),
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


def read_printed(text_layer: str, ack: Acknowledgment) -> str:
    """The acknowledgment as the page prints it: the text layer, normalised, with the edition's confirmed misreadings
    of a scanned page put right, sliced from its first words to its last."""
    text = norm(text_layer)
    for read, printed in ack.misread:
        text = text.replace(read, printed)
    return slice_between(text, ack.first, ack.last, "the PDF")


def main(edition: int = spec_pdf.DEFAULT_EDITION) -> int:
    ack = spec_pdf.edition_data(ACKNOWLEDGMENT, edition, "acknowledgment text (verify_acknowledgment.ACKNOWLEDGMENT)")
    first, last = ack.first, ack.last
    pdf = spec_pdf.require(edition)
    import pymupdf

    md = spec_pdf.markdown(edition).read_text(encoding="utf-8")
    if not md.lstrip().startswith("# Preface"):
        sys.exit(f"FATAL: the file does not open with the Preface — PDF page {ack.pdf_page} asks for the acknowledgment "
                 "'as part of the preface', so it has to come first")

    # Both copies are delimited by the DOCUMENT STRUCTURE: the front matter ends at the edition's body line
    # (spec_pdf.EDITIONS body), and the in-position copy lives after the edition's `in_position` line.
    # 1 — the preface copy: everything before the body, up to the in-position region.
    front = spec_pdf.front_matter(md, edition)
    if front is None:
        sys.exit(f"FATAL: the body's opening line ({spec_pdf.EDITIONS[edition].body}) is missing; cannot delimit the "
                 "front matter")
    at = re.search(rf"^{re.escape(ack.in_position)}$", front, re.M)
    if at is None:
        sys.exit(f"FATAL: the line {ack.in_position!r} is missing; cannot locate the in-position acknowledgment")
    preface = slice_between(front[:at.start()], first, last, "preface")

    # 2 — the in-position copy.
    inplace = slice_between(front[at.start():], first, last, f"the in-position copy ({ack.in_position})")

    # 3 — the printed text.
    doc = pymupdf.open(pdf)
    printed_raw = re.sub(ack.stamp, " ", doc[ack.pdf_page - 1].get_text(), flags=re.S)
    printed = read_printed(printed_raw, ack)

    ok = True
    for label, got in (("preface vs printed", preface), ("in position vs printed", inplace)):
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
        sys.exit("\nFATAL: the acknowledgment is not reproduced verbatim. The standard asks for it 'in their "
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
    try:
        main(2002)
        check("2002 with no acknowledgment row exits", False)
    except SystemExit as ex:
        check("2002 with no row exits naming the table and the edition, before it reads any file",
              "2002" in str(ex.code) and "verify_acknowledgment.ACKNOWLEDGMENT" in str(ex.code))
    scan = ACKNOWLEDGMENT[1985]
    layer = (f"x {scan.first} OI group,\ncopyrighted 1958,1959, copy-\nrighted; it ex-\ntends to {scan.last}\n"
             "Copyright by INCITS. Licensed to a planted name. Copying prohibited.")
    check("a scanned page's confirmed misreadings are put right before the comparison, and its stamp line removed",
          read_printed(re.sub(scan.stamp, " ", layer, flags=re.S), scan)
          == f"{scan.first} or group, copyrighted 1958, 1959, copyrighted; it extends to {scan.last}")
    check("a misreading the row does not name still differs",
          read_printed(layer.replace("OI group", "OR group"), scan) != read_printed(layer, scan))
    check("a vector edition names no misreading: its text layer is the printed text", ACKNOWLEDGMENT[2023].misread == ())
    vector_2014 = ACKNOWLEDGMENT[2014]
    check("2014 compares its printed 'OI' as printed: its true text layer is not put right as a scan's would be (PB5268)",
          vector_2014.misread == ()
          and read_printed(f"{vector_2014.first} OI group, {vector_2014.last}", vector_2014)
          == f"{vector_2014.first} OI group, {vector_2014.last}")
    print(f"=== VERIFY ACKNOWLEDGMENT SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the planted-text self-test")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    sys.exit(self_test() if args.self_test else main(args.edition))
