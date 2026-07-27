#!/usr/bin/env python3
"""Reconstruct a printed general format from the PDF: the words, their layout, and which words are UNDERLINED.

WHY THIS IS NOW POSSIBLE. Two facts about the PDF had to be established first:

  * the text layer decodes (see `pdf_deobfuscate.py` — the fonts were missing their `/ToUnicode` CMaps, so
    figure text now extracts WITH coordinates), and
  * the delimiters and underlines are vector rectangles (see `figure_geometry.py`).

Put together, a printed general format can be read off mechanically instead of by eye: the WORDS come from the
text layer, and the NOTATION — brackets, choice-indicator bars, and underlining — comes from the geometry.

WHY UNDERLINING SPECIFICALLY MATTERS. §5.2.2 makes an underlined uppercase word REQUIRED; §5.2.3 makes a
non-underlined one an OPTIONAL word that may be written or omitted. Underlining is therefore load-bearing
grammar, and it is exactly the detail a transcription loses silently — the repairs so far have repeatedly found
words called "underlined in the printed standard" that are not, each one turning a legal omission into an
apparent syntax error. Reading it off the page instead of asserting it removes that whole class of defect.

HOW A WORD IS JUDGED UNDERLINED. An underline is a long, thin horizontal rectangle sitting just below the text
baseline. A word counts as underlined when such a rule lies within `MAX_GAP` points below the word's bottom edge
and covers most of the word's horizontal extent. Short horizontal rules are excluded: those are bracket FEET, not
underlines, and telling the two apart is the same distinction `figure_geometry.py` relies on.

    python scripts/spec/figure_extract.py 632
    python scripts/spec/figure_extract.py 607 --band 130 320
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# A bracket FOOT and an underline are both short horizontal rules, and they CANNOT be told apart by width.
# An earlier version of this file used MIN_RULE_W = 9.0 to exclude feet, and silently mis-read every two-letter
# required word in the standard: the underline under `TO` is 8.87 pt and under `IN` is 8.40 pt, both just under
# that cut. The audit then reported the transcription as wrong where it was right — the worst possible direction,
# since acting on it would have stripped correct underlining out of the reference document.
#
# Width is the wrong discriminator. What actually separates them is POSITION: an underline sits just below a
# word and spans most of it, whereas a bracket foot sits at the top or bottom of a bracket stem and is far too
# short to cover a word. The coverage and gap tests below do that work, so the width floor only has to exclude
# rules too short to underline anything at all.
MIN_RULE_W = 3.0
MAX_RULE_H = 3.0
MAX_GAP = 4.0         # how far below a word's bottom edge its underline may sit
MIN_COVER = 0.55      # fraction of the word's width the rule must span


def underlines(page):
    return [r for r in (it["rect"] for it in page.get_drawings())
            if r.width >= MIN_RULE_W and r.height <= MAX_RULE_H]


def is_underlined(word_rect, rules) -> bool:
    x0, y0, x1, y1 = word_rect
    w = max(x1 - x0, 0.1)
    for r in rules:
        if not (-1.0 <= r.y0 - y1 <= MAX_GAP):
            continue
        cover = min(x1, r.x1) - max(x0, r.x0)
        if cover / w >= MIN_COVER:
            return True
    return False


def extract(page, band=None):
    """Words on the page (optionally within a y-band), each tagged with its underline state."""
    rules = underlines(page)
    out = []
    for x0, y0, x1, y1, text, blk, ln, wn in page.get_text("words"):
        if band and not (band[0] <= y0 <= band[1]):
            continue
        out.append({"text": text, "x0": round(x0, 1), "y0": round(y0, 1), "x1": round(x1, 1),
                    "underlined": is_underlined((x0, y0, x1, y1), rules)})
    return sorted(out, key=lambda w: (round(w["y0"], 0), w["x0"]))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", type=int)
    ap.add_argument("--band", nargs=2, type=float, metavar=("Y0", "Y1"), help="restrict to a vertical band")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
             "git submodule update --init specs-private")
    import fitz

    doc = fitz.open(PDF)
    page = doc[args.page - 1]
    words = extract(page, tuple(args.band) if args.band else None)
    if not words:
        sys.exit(f"no text found on page {args.page}"
                 + (f" within y {args.band[0]}..{args.band[1]}" if args.band else "")
                 + " — if the PDF still lacks /ToUnicode, run scripts/spec/pdf_deobfuscate.py first")

    print(f"PDF page {args.page}: {len(words)} words   (UNDERLINED words are marked and shown _like_this_)\n")
    row, last_y = [], None
    for w in words:
        if last_y is not None and abs(w["y0"] - last_y) > 3:
            print("   " + " ".join(row))
            row = []
        row.append(f"_{w['text']}_" if w["underlined"] else w["text"])
        last_y = w["y0"]
    if row:
        print("   " + " ".join(row))

    req = sorted({w["text"] for w in words if w["underlined"]})
    print(f"\nunderlined (REQUIRED per 5.2.2), {len(req)} distinct: {', '.join(req)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
