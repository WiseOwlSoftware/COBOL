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

HOW A WORD IS JUDGED UNDERLINED. An underline is a long, thin horizontal rectangle sitting near the text
baseline. A word counts as underlined when such a rule lies between `MIN_ABOVE` and `MAX_GAP` of the word's
bottom edge and covers most of the word's horizontal extent. Short horizontal rules are excluded: those are
bracket FEET, not underlines, and telling the two apart is the same distinction `figure_geometry.py` relies on.

⚠ NOTE THAT THE BAND EXTENDS ABOVE THE WORD BOX, and this cannot be checked by `sweep_figures.py --check`. The
generator marks required words from this function, so a word this function misses is missing from the generated
figure AND from the transcription — they agree with each other and both differ from the page. `--check` proves
consistency, not correctness, and the only thing that can catch a blind spot here is measuring against the raw
rectangles. That is how the `MIN_ABOVE` population was found: a prose sub-heading on printed page 598 is plainly
underlined on the rendered page and this function said it was not.

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
# …and how far ABOVE it. A word's bounding box includes DESCENDER space, so a rule drawn tight to the baseline
# of a word with no descender sits INSIDE the box rather than under it. Measured over the whole standard, the
# offsets form three clean populations: +1.0 pt (340 samples — every general format), −2.0 pt (14 — table
# column headers, the CLOSE statement's prose sub-headings, Annex D example labels and the `true`/`false`
# terminals of the D.7–D.10 flowcharts), and +5.0 pt (25 — a table's TOP BORDER sitting under its caption,
# which is not an underline and must stay rejected). The old −1.0 bound split the middle population off and
# under-reported it; −2.5 takes it while staying far from the border population.
MIN_ABOVE = -2.5
MIN_COVER = 0.55      # fraction of the word's width the rule must span
# How much of a CHARACTER's box a rule must overlap for that character to count as underlined when the rule
# does not reach its centre. Separates two measured populations cleanly: a first glyph's side bearing (2.2 pt
# of overlap under `I-O-CONTROL`, folio 333 — underlined) from a glyph the rule stops at (0.0 pt for the `>>`
# of `>>CALL-CONVENTION`, folio 59 — not underlined).
MIN_CHAR_OVERLAP = 1.0


def underlines(page):
    return [r for r in (it["rect"] for it in page.get_drawings())
            if r.width >= MIN_RULE_W and r.height <= MAX_RULE_H]


def is_underlined(word_rect, rules) -> bool:
    x0, y0, x1, y1 = word_rect
    w = max(x1 - x0, 0.1)
    for r in rules:
        if not (MIN_ABOVE <= r.y0 - y1 <= MAX_GAP):
            continue
        cover = min(x1, r.x1) - max(x0, r.x0)
        if cover / w >= MIN_COVER:
            return True
    return False


def underline_runs(chars, rules):
    """Which CHARACTERS of one word the rule covers — as [(offset, length), …] over the word's text.

    ⛔ WHY THIS IS NOT `is_underlined` PER WORD. `get_text("words")` splits on whitespace, but the standard
    prints the compiler-directive indicator hard against its keyword — `>>CALL-CONVENTION` is ONE whitespace
    token and the underline covers only `CALL-CONVENTION` (measured on printed folio 59: the token spans
    x 81.0–185.5, the rule 95.0–184.4). Coverage there is 86% of the token, so a per-word test says "wholly
    underlined" and the transcription then marks `>>` as a required WORD. It is not a word at all.

    The split is not a geometry hack — §7.3 syntax rule 5 makes it normative: "A compiler directive is composed
    of the compiler directive indicator, optionally followed by the COBOL character space, followed by
    compiler-instruction. The compiler directive indicator shall be treated as though it were followed by a
    space if no space is specified after the indicator." So `>>CALL-CONVENTION` IS two tokens; whitespace
    tokenization simply cannot see the boundary the standard declares. Measuring per character finds it, and
    finds any other partially-underlined token for free.

    ⛔ ONLY EVER CALLED TO **TRIM** A WORD THE PER-WORD TEST ALREADY ACCEPTED, and only the rules that passed
    it are passed in. Per-character measurement on its own is dangerous in exactly the way this file's header
    warns about: a bracket FOOT is a short horizontal rule indistinguishable from an underline by width, and
    while it can never cover 55% of a word it easily covers ONE GLYPH. Unguarded, the two 3.7 pt feet flanking
    `AREAS` on folio 312 marked its `S` alone, and three feet under §13.14.2 turned `code-clause`,
    `control-clause` and `page-clause` into `code-claus`, `e`, `e`, `e`. So promotion is impossible here by
    construction; the only thing this can do is remove characters from a span.

    Returns True/False for the whole-word cases so callers that only need a flag keep working."""
    runs, cur = [], None
    for i, (cx0, cy0, cx1, cy1) in enumerate(chars):
        mid = (cx0 + cx1) / 2
        # A rule is drawn under the visible STROKE, so it starts a little inside the first glyph's box — its
        # side bearing. On folio 333 the rule under `I-O-CONTROL` begins 2.2 pt right of the word box, which a
        # bare centre test reads as an unlined `I`. Overlap of at least MIN_CHAR_OVERLAP absorbs the bearing
        # while still rejecting a glyph the rule never reaches: on folio 59 the rule starts exactly at the end
        # of `>>`, giving zero overlap, so the indicator stays plain.
        on = any(MIN_ABOVE <= r.y0 - cy1 <= MAX_GAP
                 and (r.x0 <= mid <= r.x1 or min(cx1, r.x1) - max(cx0, r.x0) >= MIN_CHAR_OVERLAP)
                 for r in rules)
        if on and cur is None:
            cur = i
        elif not on and cur is not None:
            runs.append((cur, i - cur))
            cur = None
    if cur is not None:
        runs.append((cur, len(chars) - cur))
    if not runs:
        return False
    return True if runs == [(0, len(chars))] else runs


def char_index(page):
    """Every non-space character on the page with its box — built ONCE, so the per-character underline test
    costs one rawdict pass rather than one per word."""
    out = []
    for blk in page.get_text("rawdict")["blocks"]:
        if blk["type"] != 0:
            continue
        for line in blk["lines"]:
            for span in line["spans"]:
                for c in span["chars"]:
                    if not c["c"].isspace():
                        out.append((c["c"], c["bbox"]))
    return out


def extract(page, band=None):
    """Words on the page (optionally within a y-band), each tagged with its underline state.

    `underlined` is True, False, or [(offset, length), …] when the rule covers only PART of the word — which
    is how `>>CALL-CONVENTION` keeps its keyword underlined and its indicator plain. See `underline_runs`."""
    rules = underlines(page)
    chars = char_index(page)
    out = []
    for x0, y0, x1, y1, text, blk, ln, wn in page.get_text("words"):
        if band and not (band[0] <= y0 <= band[1]):
            continue
        state = is_underlined((x0, y0, x1, y1), rules)
        # Re-measure per character ONLY to trim a word already accepted, and only against the rules that
        # accepted it — see `underline_runs`. Promotion must stay impossible: bracket feet are short rules
        # that cannot cover 55% of a word but can easily cover one glyph.
        if state is True and len(text) > 1:
            own = [r for r in rules
                   if MIN_ABOVE <= r.y0 - y1 <= MAX_GAP
                   and (min(x1, r.x1) - max(x0, r.x0)) / max(x1 - x0, 0.1) >= MIN_COVER]
            boxes = [b for ch, b in chars
                     if x0 - 0.5 <= (b[0] + b[2]) / 2 <= x1 + 0.5 and y0 - 0.5 <= (b[1] + b[3]) / 2 <= y1 + 0.5]
            if len(boxes) == len(text):
                state = underline_runs(boxes, own)
        out.append({"text": text, "x0": round(x0, 1), "y0": round(y0, 1), "x1": round(x1, 1),
                    "underlined": state})
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
