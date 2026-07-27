#!/usr/bin/env python3
"""Render a printed general format as a Markdown `<pre>` block, generated entirely from measurement.

WHY GENERATE RATHER THAN TRANSCRIBE. Every input is now measurable: the words and their positions come from the
decoded text layer, whether each word is UNDERLINED comes from the underline rectangles, and the brackets, braces
and choice-indicator bars come from the delimiter geometry. A figure assembled from those is correct by
construction — a stronger property than any audit, because it can be regenerated and diffed rather than trusted.

WHY `<pre>` AND NOT A FENCE. A fenced block cannot carry underlining: `<u>` renders literally inside one, and so
does `**`. Underlining is not decoration here — §5.2.2 makes an underlined word REQUIRED and §5.2.3 makes a
non-underlined one OPTIONAL — so a fence structurally cannot express the figure's grammar. `<pre>` preserves
monospace and alignment and admits `<u>`.

WHY UNDERLINE AND NOT BOLD. They cost the same and render equally well, so the standard's own convention wins.

WHY TAGS ARE APPLIED AFTER LAYOUT. `<u>…</u>` adds seven characters that occupy no rendered width. Laying out
first in plain text and inserting the tags afterwards means the block aligns on RENDERED width, which is what a
reader sees — and means nobody ever hand-aligns a figure around invisible characters.

DELIMITERS COME FROM TWO PLACES, because the standard typesets them differently: brackets and bars are vector
RECTANGLES, whose measured y-span says exactly which rows they cover; braces are Symbol-face GLYPHS that arrive
in the text stream already positioned. Both are mapped onto the Unicode extension glyphs (U+23A1–U+23AD), and
choice indicators onto `│` (U+2502), which joins vertically where `|` does not.

    python scripts/spec/render_figure.py 784 --band 180 340
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# THE HOUSE STYLE — settled 2026-07-27 by rendering candidates in a browser, not by reasoning about markup.
# Full rationale and the measurements behind each choice: docs/rearchitecture/spec-reconciliation/FIGURE-STYLE.md
#
# BOX DRAWING ONLY (U+2500–U+257F). The Miscellaneous Technical extension glyphs (⎡⎧⎪, U+23A1–U+23AD) look
# right and are unusable: measured on Windows, NOT ONE monospace font contains them, so the browser substitutes
# a proportional face per glyph and the columns drift — even within a single figure, because the substituted
# glyphs are not mutually consistent in width. Box drawing is in every monospace font by design.
BRACKET_L = ("┌", "│", "└")          # square corners: a bracket
BRACKET_R = ("┐", "│", "┘")
BRACE_L   = ("╭", "┤", "╰")          # curved corners + the point: a brace, distinct at any height
BRACE_R   = ("╮", "├", "╯")
BAR       = "│"                      # choice indicator, kept ONE SPACE clear of any adjacent delimiter
MIN_ROWS  = 3                        # a corner glyph strokes from its cell CENTRE, so a two-row group renders
                                     # one row tall; a blank middle row restores full height — and matches the
                                     # printed layout, which gives each operand its own row.
# Symbol-face brace pieces in the PDF's text layer, mapped to the house glyphs above.
BRACE = {"ì": BRACE_L[0], "í": BRACE_L[1], "î": BRACE_L[2],
         "ü": BRACE_R[0], "ý": BRACE_R[1], "þ": BRACE_R[2]}
EXTENDER = "ê"


def column_grid(items, bucket=3.0):
    """Map printed x positions onto text columns via a SHARED grid.

    Scaling x by one global factor gives a faithful but unreadable figure: the widest word anywhere sets the
    scale, and every other gap inflates to match. Instead, every distinct x position across the whole figure
    becomes a column stop, and each stop is given exactly the width its widest occupant needs. Vertical
    alignment is preserved — items printed at the same x land in the same column — and the result is as tight
    as the content allows.
    """
    stops = sorted({round(x / bucket) for x, _ in items})
    widest = {s: 1 for s in stops}
    for x, text in items:
        s = round(x / bucket)
        widest[s] = max(widest[s], len(text))
    col, run = {}, 0
    for s in stops:
        col[s] = run
        run += widest[s] + 1
    return (lambda x: col[min(stops, key=lambda s: abs(s - round(x / bucket)))]), run + 2


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", type=int)
    ap.add_argument("--band", nargs=2, type=float, required=True, metavar=("Y0", "Y1"))
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from figure_extract import extract
    from figure_geometry import classify_page

    doc = fitz.open(PDF)
    page = doc[args.page - 1]
    lo, hi = args.band

    words = [w for w in extract(page) if lo <= w["y0"] <= hi]
    if not words:
        sys.exit("no words in that band")

    # Rows, keyed by baseline. Brace pieces are pulled out: they are delimiters, not text.
    rows = collections.defaultdict(list)
    braces = []
    for w in words:
        t = w["text"]
        if t in BRACE or t == EXTENDER:
            braces.append(w)
            continue
        rows[round(w["y0"] / 6)].append(w)
    ykeys = sorted(rows)
    yof = {k: i for i, k in enumerate(ykeys)}
    ymid = {k: sum(x["y0"] for x in rows[k]) / len(rows[k]) for k in ykeys}

    stems_all = [s for s in classify_page(page) if not (s["y1"] < lo or s["y0"] > hi)]
    col, width = column_grid([(w["x0"], w["text"]) for w in words]
                             + [(s["x"], "|") for s in stems_all])
    grid = [[" "] * width for _ in ykeys]
    marks = collections.defaultdict(list)          # row -> [(col, len)] for underlined words

    for k in ykeys:
        r = yof[k]
        for w in rows[k]:
            c = col(w["x0"])
            for i, ch in enumerate(w["text"]):
                if c + i < width:
                    grid[r][c + i] = ch
            if w["underlined"]:
                marks[r].append((c, len(w["text"])))

    def nearest_row(y):
        return yof[min(ykeys, key=lambda k: abs(ymid[k] - y))]

    # Braces: place each measured piece on its nearest row.
    for b in braces:
        r, c = nearest_row(b["y0"]), col(b["x0"])
        if 0 <= c < width:
            grid[r][c] = BRACE.get(b["text"], "⎪")

    # Brackets and bars come from the rectangles, which say precisely which rows they span.
    # Handedness is decided WITHIN each enclosure, never across the page. Using one page-wide midpoint drew the
    # closing bracket of a narrower lower group as an opening bracket, because its right-hand stem still sat
    # left of the page centre.
    groups = collections.defaultdict(list)
    for s in stems_all:
        groups[round(s["y0"] / 8)].append(s)
    mids = {}
    for key, g in groups.items():
        mids[key] = (min(x["x"] for x in g) + max(x["x"] for x in g)) / 2
    for s in stems_all:
        mid = mids[round(s["y0"] / 8)]
        spans = [yof[k] for k in ykeys if s["y0"] - 6 <= ymid[k] <= s["y1"] + 6]
        if not spans:
            continue
        c = col(s["x"])
        if not (0 <= c < width):
            continue
        for n, r in enumerate(spans):
            if s["kind"] == "bar":
                grid[r][c] = BAR
            else:
                top, ext, bot = BRACKET_L if s["x"] < mid else BRACKET_R
                grid[r][c] = top if n == 0 else (bot if n == len(spans) - 1 else ext)
    # NOTE: consumers must render these blocks inside <pre> at line-height 1. Box-drawing glyphs TILE — the
    # stroke spans the full em box — so the verticals join into one rule only when rows are exactly one em
    # apart. Any leading silently breaks every choice indicator in the document.

    # Tags LAST, right to left, so the columns above are untouched by their width.
    out = []
    for r, _ in enumerate(ykeys):
        line = "".join(grid[r]).rstrip()
        for c, ln in sorted(marks[r], reverse=True):
            if c < len(line):
                line = line[:c] + "<u>" + line[c:c + ln] + "</u>" + line[c + ln:]
        out.append(line)

    print("<pre>")
    for l in out:
        print(l)
    print("</pre>")
    return 0


if __name__ == "__main__":
    sys.exit(main())
