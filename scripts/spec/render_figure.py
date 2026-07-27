#!/usr/bin/env python3
"""Render a printed general format as a Markdown `<pre>` block, generated entirely from measurement.

WHY GENERATE RATHER THAN TRANSCRIBE. Every input is measurable: the words and their positions come from the
decoded text layer, whether each word is UNDERLINED comes from the underline rectangles, and the brackets,
braces and choice-indicator bars come from the delimiter geometry. A figure assembled from those is correct by
construction — a stronger property than any audit, because it can be regenerated and diffed rather than trusted.

Hand-placing is what this replaces, and the risk is not hypothetical: a hand-built ACCEPT Format 3 flattened two
operand rows and got the row count wrong (seven in print, five as drawn). Hand-specifying reintroduces exactly
the drift the reconciliation exists to remove.

THE HOUSE STYLE it emits — glyphs, spacing, minimum row count, and the `line-height: 1` requirement — is settled
and documented in `docs/rearchitecture/spec-reconciliation/FIGURE-STYLE.md`. Read that before changing anything
here: every rule was established by rendering candidates and looking at them, and several are counter-intuitive.

    python scripts/spec/render_figure.py 784 --band 180 340      # START,  printed page 754
    python scripts/spec/render_figure.py 607 --band 140 250      # ACCEPT Format 3, printed page 577
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# THE HOUSE STYLE — see FIGURE-STYLE.md for the measurements behind each choice.
# BOX DRAWING ONLY (U+2500–U+257F). The Miscellaneous Technical extension glyphs (⎡⎧⎪, U+23A1–U+23AD) look right
# and are unusable: measured on Windows, NOT ONE monospace font contains them, so a browser substitutes a
# proportional face per glyph and columns drift — even within one figure, the substitutes not being mutually
# consistent in width. Box drawing is in every monospace font because the block was designed for character grids.
BRACKET_L = ("┌", "│", "└")
BRACKET_R = ("┐", "│", "┘")
BRACE_L   = ("╭", "┤", "╰")        # curved corners + the point, so a brace stays distinct at any height
BRACE_R   = ("╮", "├", "╯")
BAR       = "│"
BRACE_EXT = "│"                    # a brace's straight run between hook and point

# Symbol-face brace pieces, as they decode from the PDF's text layer.
BRACE_PIECES = {"ì": ("L", 0), "í": ("L", 1), "î": ("L", 2),
                "ü": ("R", 0), "ý": ("R", 1), "þ": ("R", 2)}
EXTENDER = "ê"


def cluster(values, tol):
    """Map each coordinate onto a shared representative, so near-identical positions become one row or column."""
    keys = sorted(set(values))
    if not keys:
        return {}
    groups, cur = [], [keys[0]]
    for v in keys[1:]:
        if v - cur[-1] <= tol:
            cur.append(v)
        else:
            groups.append(cur)
            cur = [v]
    groups.append(cur)
    out = {}
    for g in groups:
        rep = sum(g) / len(g)
        for v in g:
            out[v] = rep
    return out


def pack_columns(items):
    """Assign text columns to measured x positions.

    Vertical alignment must be preserved — items printed at the same x belong in one column — but printed x
    values must NOT be scaled directly: a single global scale lets the widest word anywhere set the gap
    everywhere and the figure balloons (the first version of this file did exactly that). So columns are packed
    LEFT TO RIGHT, each starting just past whatever already occupies its own rows.

    items: iterable of (row, xkey, text). Returns {xkey: column}.
    """
    by_col = collections.defaultdict(list)
    for r, x, t in items:
        by_col[x].append((r, t))
    col, row_end = {}, collections.defaultdict(int)
    for x in sorted(by_col):
        start = max((row_end[r] for r, _ in by_col[x]), default=0)
        col[x] = start + 1 if start else 0
        for r, t in by_col[x]:
            row_end[r] = col[x] + len(t)
    return col


def build(page, lo, hi, extract, classify_page):
    """Lay the figure out and return (grid rows, {row: [(col,len)] underlined spans})."""
    words = [w for w in extract(page) if lo <= w["y0"] <= hi]
    if not words:
        return None, None
    braces = [w for w in words if w["text"] in BRACE_PIECES or w["text"] == EXTENDER]
    text_w = [w for w in words if w["text"] not in BRACE_PIECES and w["text"] != EXTENDER]
    if not text_w:
        return None, None

    rowof = cluster([w["y0"] for w in text_w], tol=4.0)   # 4.9 pt separates rows, 3.2 pt does not
    rows = sorted(set(rowof.values()))
    rindex = {y: i for i, y in enumerate(rows)}
    stems = [s for s in classify_page(page) if not (s["y1"] < lo or s["y0"] > hi)]

    def covers(s):
        return sorted(rindex[y] for y in rows if s["y0"] - 6 <= y <= s["y1"] + 6)

    # A group spanning only TWO rows renders one row tall, because a corner glyph strokes from its cell centre.
    # Insert a spacer row between such a pair — which is also what the printed page does, giving each operand
    # its own row with the phrase label centred between.
    spacers = {c[0] for s in stems for c in [covers(s)] if len(c) == 2 and c[1] - c[0] == 1}
    shift = {i: i + sum(1 for sp in spacers if sp < i) for i in range(len(rows))}
    nrows = len(rows) + len(spacers)
    row_of = lambda y: shift[rindex[rowof[y]]]
    near = lambda y: shift[rindex[min(rows, key=lambda r: abs(r - y))]]

    xof = cluster([w["x0"] for w in text_w] + [s["x"] for s in stems] + [b["x0"] for b in braces], tol=4.0)
    items = [(row_of(w["y0"]), xof[w["x0"]], w["text"]) for w in text_w]
    items += [(near(b["y0"]), xof[b["x0"]], "X") for b in braces]
    for s in stems:
        cov = covers(s)
        if cov:
            items += [(r, xof[s["x"]], "X") for r in range(shift[cov[0]], shift[cov[-1]] + 1)]
    col = pack_columns(items)

    width = max(col[x] + len(t) for _, x, t in items) + 2
    grid = [[" "] * width for _ in range(nrows)]
    marks = collections.defaultdict(list)

    for w in text_w:
        r, c = row_of(w["y0"]), col[xof[w["x0"]]]
        for i, ch in enumerate(w["text"]):
            grid[r][c + i] = ch
        if w["underlined"]:
            marks[r].append((c, len(w["text"])))

    # Handedness is decided WITHIN each enclosure, never across the page. One page-wide midpoint draws the
    # closing bracket of a narrower lower group as an OPENING bracket, because its right-hand stem still sits
    # left of the page centre — which is exactly what happened to START's exception group.
    bands = collections.defaultdict(list)
    for s in stems:
        bands[round(s["y0"] / 8)].append(s)
    midof = {k: (min(x["x"] for x in g) + max(x["x"] for x in g)) / 2 for k, g in bands.items()}
    for s in stems:
        cov = covers(s)
        if not cov:
            continue
        mid = midof[round(s["y0"] / 8)]
        span = list(range(shift[cov[0]], shift[cov[-1]] + 1))
        c = col[xof[s["x"]]]
        for n, r in enumerate(span):
            if s["kind"] == "bar":
                grid[r][c] = BAR
            else:
                g = BRACKET_L if s["x"] < mid else BRACKET_R
                grid[r][c] = g[0] if n == 0 else (g[2] if n == len(span) - 1 else g[1])

    # Braces arrive as positioned glyphs; map each piece onto the house brace and fill the rows between.
    bx = collections.defaultdict(list)
    for b in braces:
        bx[xof[b["x0"]]].append(b)
    for x, pieces in bx.items():
        rs = sorted(near(b["y0"]) for b in pieces)
        side = next((BRACE_PIECES[b["text"]][0] for b in pieces if b["text"] in BRACE_PIECES), "L")
        g = BRACE_L if side == "L" else BRACE_R
        c = col[x]
        # A brace is hook · extension … POINT … extension · hook. The point marks it as a brace rather than a
        # bracket and belongs at the vertical CENTRE — filling every middle row with it (the first version did)
        # turns the brace into a comb.
        span = list(range(rs[0], rs[-1] + 1))
        centre = len(span) // 2
        for n, r in enumerate(span):
            if n == 0:
                grid[r][c] = g[0]
            elif n == len(span) - 1:
                grid[r][c] = g[2]
            elif n == centre:
                grid[r][c] = g[1]
            else:
                grid[r][c] = BRACE_EXT
    return grid, marks


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", type=int, help="PDF page number (printed folio + 30)")
    ap.add_argument("--band", nargs=2, type=float, required=True, metavar=("Y0", "Y1"))
    ap.add_argument("--plain", action="store_true", help="omit the <u> tags")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a "
                 "PRIVATE submodule: git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from figure_extract import extract
    from figure_geometry import classify_page

    doc = fitz.open(PDF)
    grid, marks = build(doc[args.page - 1], args.band[0], args.band[1], extract, classify_page)
    if grid is None:
        sys.exit("no figure content in that band")

    plain = ["".join(r).rstrip() for r in grid]
    out = list(plain)
    if not args.plain:
        for r in range(len(out)):
            for c, ln in sorted(marks[r], reverse=True):
                if c < len(out[r]):
                    out[r] = out[r][:c] + "<u>" + out[r][c:c + ln] + "</u>" + out[r][c + ln:]
        # The tags must occupy NO layout, or every column after them shifts. Proving that is what makes it safe
        # to run this over 254 figures rather than eyeballing each one.
        if [re.sub(r"</?u>", "", l) for l in out] != plain:
            sys.exit("FATAL: inserting <u> changed the laid-out text — alignment would be wrong")

    print("<pre>")
    for l in out:
        print(l)
    print("</pre>")
    return 0


if __name__ == "__main__":
    sys.exit(main())
