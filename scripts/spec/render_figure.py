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

WHERE A FIGURE IS is read from the clause structure — the bold `14.9.N.2 General format` heading that opens the
region, the heading that closes it, and the `Format N (label):` lines that separate one format from the next.
See `find_bands`. An explicit `--band` overrides it for a page the structure cannot describe.

    python scripts/spec/render_figure.py 784                     # START, printed page 754 — every figure
    python scripts/spec/render_figure.py 632 --bands-only        # COMPUTE — just report what it located
    python scripts/spec/render_figure.py 607 --band 140 250      # override: one band, given explicitly
"""
from __future__ import annotations

import argparse
import collections
import itertools
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
PAREN_L   = ("╭", "│", "╰")        # curved like a brace, but POINTLESS — a paren groups without choosing
PAREN_R   = ("╮", "│", "╯")
BAR       = "│"
BRACE_EXT = "│"                    # a brace's straight run between hook and point

# Symbol-face extensible DELIMITER pieces, as they decode from the PDF's text layer, each as
# (family, side, role). A tall delimiter is assembled from a top hook, extenders and a bottom hook; a brace
# additionally carries a middle POINT. `ê` is the brace extender and is written by both sides, so it carries
# no side of its own — the side comes from whichever hook shares its column.
#
# The PARENTHESIS family was missing until the whole-standard sweep enumerated every non-ASCII glyph inside a
# figure and found it: `æçè` / `ö÷ø` were being drawn as literal letters, which is the silent-corruption mode
# this file exists to remove. They are COBOL's own `(` and `)` separators, set full height — Adobe Symbol
# 0xE6-0xE8 and 0xF6-0xF8 are the extensible PAREN pieces, and the function-identifier format on page 157
# confirms it by nesting them between two vector brackets, exactly as the transcription draws them.
#
# Brackets themselves never appear as glyphs: every one measured in the standard is a vector rectangle, which
# is why `figure_geometry` alone accounts for them. `ê` is a bare vertical stroke and so is family-agnostic —
# a brace extender and a bracket extender are the same shape, and the side comes from the hooks in its column.
GLYPH_PIECES = {
    "ì": ("brace", "L", "top"), "í": ("brace", "L", "mid"), "î": ("brace", "L", "bot"),
    "ü": ("brace", "R", "top"), "ý": ("brace", "R", "mid"), "þ": ("brace", "R", "bot"),
    "æ": ("paren", "L", "top"), "ç": ("paren", "L", "ext"), "è": ("paren", "L", "bot"),
    "ö": ("paren", "R", "top"), "÷": ("paren", "R", "ext"), "ø": ("paren", "R", "bot"),
    "ê": (None, None, "ext"),
}
# Symbol-face glyphs that are TEXT, not notation: the repetition ellipsis of 5.2.5.
TEXT_GLYPHS = str.maketrans({"¼": "…"})
# Every other non-ASCII character measured inside a figure across the whole standard: the MINUS operator of
# the report-writer LINE clause, and a figure dash inside a flag name. Anything else is unrecognised notation
# and must stop the run rather than be drawn as a letter.
TEXT_SAFE = set("…–‒")

X_TOL = 2.0                           # how far apart two positions may be and still be one column
TOP_SLACK, BOTTOM_INSET = 2.0, 8.0    # how a stem's span maps onto rows of text — see `covers`


def cluster(values, tol):
    """Map each coordinate onto a shared representative, so near-identical positions become one row or column.

    A group is bounded by its WIDTH, not by the gap to the previous value. Single-link chaining looks
    equivalent and is not: page 607's left edge runs 89.5 · 90.8 · 92.2 · 95.9 · 96.4 · 97.7 with every step
    under 4 pt, so a chain swallowed a bracket, its choice-indicator bar, `AT` and `[ END-ACCEPT ]` into ONE
    column — and the delimiters then drew straight through the text.
    """
    keys = sorted(set(values))
    if not keys:
        return {}
    groups, cur = [], [keys[0]]
    for v in keys[1:]:
        if v - cur[0] <= tol:
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


def split_fused(page, words):
    """Separate a delimiter glyph from text it was extracted with.

    `get_text("words")` splits on whitespace, and exactly once in the standard a brace hook is set hard against
    the word beside it — `üSEQUENTIAL` on page 344. Left fused, the hook is neither notation nor a letter: it
    would be drawn as the letter 'u-umlaut' in the middle of a figure. The characters are RE-MEASURED from the
    raw text layer rather than estimated from the word box, so each piece keeps its true column.
    """
    fused = [w for w in words if len(w["text"]) > 1 and any(c in GLYPH_PIECES for c in w["text"])]
    if not fused:
        return words
    # Take the characters from the RAW LINE that produced the word, not from a box around it: the hook and the
    # word sit on different rows (`ü` at y 340.0, `SEQUENTIAL` at 348.3), and any y-window wide enough to hold
    # both also swallows the brace's other pieces, which share the hook's column.
    lines = [line for blk in page.get_text("rawdict")["blocks"] if blk["type"] == 0 for line in blk["lines"]]
    out = [w for w in words if id(w) not in {id(f) for f in fused}]
    for w in fused:
        run = None
        for line in lines:
            for is_space, grp in itertools.groupby((c for span in line["spans"] for c in span["chars"]),
                                                   key=lambda c: c["c"].isspace()):
                if is_space:
                    continue
                cs = list(grp)
                if "".join(c["c"] for c in cs) == w["text"] and abs(cs[0]["bbox"][0] - w["x0"]) < 1.0:
                    run = cs
                    break
            if run:
                break
        if run is None:
            sys.exit(f"FATAL: could not re-measure the fused word {w['text']!r} against the raw text layer — "
                     "splitting it by estimate would put the delimiter in the wrong column")
        for is_glyph, grp in itertools.groupby(run, key=lambda c: c["c"] in GLYPH_PIECES):
            piece = list(grp)
            out.append({"text": "".join(c["c"] for c in piece),
                        "x0": round(piece[0]["bbox"][0], 1), "x1": round(piece[-1]["bbox"][2], 1),
                        "y0": round(min(c["bbox"][1] for c in piece), 1),
                        "underlined": w["underlined"] and not is_glyph})
    return out


def build(page, lo, hi, extract, classify_page):
    """Lay the figure out and return (grid rows, {row: [(col,len)] underlined spans})."""
    words = split_fused(page, [w for w in extract(page) if lo <= w["y0"] <= hi])
    if not words:
        return None, None
    glyphs = [w for w in words if w["text"] in GLYPH_PIECES]
    text_w = [dict(w, text=w["text"].translate(TEXT_GLYPHS)) for w in words if w["text"] not in GLYPH_PIECES]
    if not text_w:
        return None, None
    stray = sorted({ch for w in text_w for ch in w["text"] if ord(ch) > 127 and ch not in TEXT_SAFE})
    if stray:
        sys.exit("FATAL: unrecognised glyph(s) in the figure text: "
                 + ", ".join(f"U+{ord(c):04X} {c!r}" for c in stray)
                 + " — notation drawn as a letter would corrupt the figure silently; map it in GLYPH_PIECES")

    rowof = cluster([w["y0"] for w in text_w], tol=4.0)   # 4.9 pt separates rows, 3.2 pt does not
    rows = sorted(set(rowof.values()))
    rindex = {y: i for i, y in enumerate(rows)}
    stems = [s for s in classify_page(page) if not (s["y1"] < lo or s["y0"] > hi)]

    # WHICH ROWS A STEM ENCLOSES. The bottom test takes an INSET, not slack: a stem stops just above the next
    # row of text, so the `y1 + 6` this once used reached the row BELOW the group — which is how `[ END-START ]`
    # and `[ END-COMPUTE ]` kept being drawn through by the exception bracket above them. The two populations
    # are cleanly separated, so the threshold is not delicate: measured on pages 784 and 632, an ENCLOSED row's
    # top sits 13.7-15.9 pt above its stem's bottom, while the NEXT row's top sits 1.9-3.6 pt below it.
    def covers(s):
        return sorted(rindex[y] for y in rows if s["y0"] - TOP_SLACK <= y <= s["y1"] - BOTTOM_INSET)

    raw_near = lambda y: rindex[min(rows, key=lambda r: abs(r - y))]

    # COLUMNS ARE CLUSTERED PER KIND. A stem sits 1-3 pt from the text beside it, so one clustering over text
    # and delimiters together cannot distinguish "the same column, one row down" from "two adjacent things" —
    # and a shared key is precisely what lets a delimiter overwrite a word. Keys carry the representative x
    # first so packing still runs strictly left to right; the rank only breaks exact ties.
    brackets = [s for s in stems if s["kind"] != "bar"]
    bars = [s for s in stems if s["kind"] == "bar"]

    def keyed(values, rank, tol=X_TOL):
        rep = cluster(values, tol)
        return {v: (rep[v], rank) for v in rep}

    xtext = keyed([w["x0"] for w in text_w], 3)
    xbrk = keyed([s["x"] for s in brackets], 0)
    xbar = keyed([s["x"] for s in bars], 1)
    xbrace = keyed([b["x0"] for b in glyphs], 2)
    stemx = lambda s: (xbar if s["kind"] == "bar" else xbrk)[s["x"]]

    # A brace arrives as separate glyph pieces, but it OCCUPIES every row from its top hook to its bottom one.
    # Reserving only the rows a piece happens to sit on lets text pack straight through the rows between —
    # which is what put `ACCEPT`'s outer brace on top of the `AT` phrase. Spans are computed here, before
    # column packing, so the reservation and the drawing below agree by construction.
    gx = collections.defaultdict(list)
    for b in glyphs:
        gx[xbrace[b["x0"]]].append(b)

    def delimiters(pieces):
        """Split one column's pieces into SEPARATE delimiters, cutting at each top hook.

        A column is not a delimiter: the file-control entry stacks clause after clause, and two braces in the
        same column belonging to different clauses were merging into one tall brace spanning everything
        between — the LOCK MODE brace drew straight up through ACCESS MODE and FILE STATUS. The pieces carry
        their own role, so the cut is exact and needs no gap threshold.
        """
        groups, cur = [], []
        for b in sorted(pieces, key=lambda b: b["y0"]):
            role = GLYPH_PIECES[b["text"]][2]
            if role == "top" and cur:
                groups.append(cur)
                cur = []
            cur.append(b)
            if role == "bot":
                groups.append(cur)
                cur = []
        return groups + ([cur] if cur else [])

    # A HOOK BRACKETS ITS CONTENT, so it maps directionally: a top hook onto the first row at or below it, a
    # bottom hook onto the last row at or above it. Snapping each to the merely NEAREST row put the top hook of
    # the function-identifier's parentheses a row high, onto `function-pointer-name-1`, which belongs to the
    # brace beside it and not inside the parentheses at all.
    def hook_row(b):
        role, y = GLYPH_PIECES[b["text"]][2], b["y0"]
        if role == "top":
            below = [r for r in rows if r >= y - 1.0]
            return rindex[below[0]] if below else raw_near(y)
        if role == "bot":
            above = [r for r in rows if r <= y + 1.0]
            return rindex[above[-1]] if above else raw_near(y)
        return raw_near(y)

    glyph_raw = [(x, min(hook_row(b) for b in grp), max(hook_row(b) for b in grp), grp)
                 for x, ps in gx.items() for grp in delimiters(ps)]

    # A group spanning only TWO rows renders one row tall, because a corner glyph strokes from its cell centre.
    # Insert a spacer row between such a pair — which is also what the printed page does, giving each operand
    # its own row with the phrase label centred between (FIGURE-STYLE rule 5).
    #
    # BOTH families count. This used to consider vector stems only, so a two-alternative BRACE — which is
    # always glyph-drawn — came out two rows tall with no middle piece and therefore no POINT, losing the
    # §5.2.6.3-vs-§5.2.6.2 distinction the point exists to carry. `where encoding-phrase is:` on folio 276 is
    # the smallest case: two alternatives, one brace, and nowhere to put its point.
    spacers = {c[0] for s in stems for c in [covers(s)] if len(c) == 2 and c[1] - c[0] == 1}
    spacers |= {r0 for _, r0, r1, _ in glyph_raw if r1 - r0 == 1}
    shift = {i: i + sum(1 for sp in spacers if sp < i) for i in range(len(rows))}
    nrows = len(rows) + len(spacers)
    row_of = lambda y: shift[rindex[rowof[y]]]
    near = lambda y: shift[raw_near(y)]

    glyph_spans = [(x, shift[r0], shift[r1], grp) for x, r0, r1, grp in glyph_raw]

    items = [(row_of(w["y0"]), xtext[w["x0"]], w["text"]) for w in text_w]
    for x, r0, r1, _ in glyph_spans:
        items += [(r, x, "X") for r in range(r0, r1 + 1)]
    for s in stems:
        cov = covers(s)
        if cov:
            items += [(r, stemx(s), "X") for r in range(shift[cov[0]], shift[cov[-1]] + 1)]
    col = pack_columns(items)

    width = max(col[x] + len(t) for _, x, t in items) + 2
    grid = [[" "] * width for _ in range(nrows)]
    marks = collections.defaultdict(list)

    for w in text_w:
        r, c = row_of(w["y0"]), col[xtext[w["x0"]]]
        for i, ch in enumerate(w["text"]):
            grid[r][c + i] = ch
        if w["underlined"]:
            marks[r].append((c, len(w["text"])))

    # A delimiter must never land on a character. Text is placed first and the delimiters over it, so a clash
    # means the layout is wrong — a stem given a row it does not enclose, or two delimiters clustered into one
    # column — and it corrupts the figure SILENTLY: `[ END-START ]` came out as `|N]-START` and still looked
    # like a figure. That is the same shape as the `line-height: 1` trap, so it fails loudly instead. Collected
    # rather than raised on the spot, so one run reports every clash on the page.
    clashes = []

    def put(r, c, ch):
        if grid[r][c] not in (" ", ch):
            clashes.append((r, c, grid[r][c], ch))
        grid[r][c] = ch

    # Handedness comes from the stem's own FEET, which turn inward (`figure_geometry` measures the direction).
    # It used to be inferred from a midpoint over nearby stems, and that fails in both directions: a page-wide
    # midpoint drew the closing bracket of a narrower lower group as an opening one (START's exception group),
    # and a midpoint over a y-bucket holding a single stem always made it closing (the RESERVE clause of the
    # file-control entry). The midpoint survives only as a fallback for a stem whose feet were not resolved.
    bands = collections.defaultdict(list)
    for s in stems:
        bands[round(s["y0"] / 8)].append(s)
    midof = {k: (min(x["x"] for x in g) + max(x["x"] for x in g)) / 2 for k, g in bands.items()}
    for s in stems:
        cov = covers(s)
        if not cov:
            continue
        opening = s["side"] == "L" if s.get("side") else s["x"] < midof[round(s["y0"] / 8)]
        span = list(range(shift[cov[0]], shift[cov[-1]] + 1))
        c = col[stemx(s)]
        for n, r in enumerate(span):
            if s["kind"] == "bar":
                put(r, c, BAR)
            elif len(span) == 1:
                # One row tall: the printed page draws a single-height bracket, and box-drawing corners
                # cannot express that — `┌ … ┐` on one row reads as an unclosed group. The ASCII separator
                # is what the transcription already uses for `[ END-START ]` and `[ OPTIONAL ]`.
                put(r, c, "[" if opening else "]")
            else:
                g = BRACKET_L if opening else BRACKET_R
                put(r, c, g[0] if n == 0 else (g[2] if n == len(span) - 1 else g[1]))

    # Map each glyph-drawn delimiter onto the house glyphs and fill its reserved span.
    for x, r0, r1, pieces in glyph_spans:
        kinds = [GLYPH_PIECES[b["text"]] for b in pieces]
        family = next((f for f, _, _ in kinds if f), "brace")
        side = next((sd for _, sd, _ in kinds if sd), "L")
        g = ((BRACE_L if side == "L" else BRACE_R) if family == "brace"
             else (PAREN_L if side == "L" else PAREN_R))
        c = col[x]
        # A brace is hook · extension … POINT … extension · hook. The point marks it as a brace rather than a
        # bracket and belongs at the vertical CENTRE — filling every middle row with it (the first version did)
        # turns the brace into a comb.
        span = list(range(r0, r1 + 1))
        # Only a BRACE carries a point — it is what tells 5.2.6.3 "exactly one" from 5.2.6.2 "at most one" at
        # a glance; a paren's middle is plain extension throughout. The point goes where the `mid` piece was
        # MEASURED, not at the arithmetic centre of the span: the ASSIGN clause's inner brace runs four rows
        # with its point on the second, and centring it put the point a row low.
        # ... and it belongs to the INTERIOR of the span. A two-alternative brace has only two rows of text to
        # map onto, so its measured middle piece lands on the top or bottom row, where the hook wins and the
        # point disappears — which is exactly the case rule 5's spacer row exists to make room for.
        points = {near(b["y0"]) for b in pieces if GLYPH_PIECES[b["text"]][2] == "mid"} & set(span[1:-1])
        if family == "brace" and not points:
            points = {span[len(span) // 2]}
        if len(span) == 1:
            put(span[0], c, ("{" if side == "L" else "}") if family == "brace"
                            else ("(" if side == "L" else ")"))
            continue
        for n, r in enumerate(span):
            if n == 0:
                put(r, c, g[0])
            elif n == len(span) - 1:
                put(r, c, g[2])
            elif r in points:
                put(r, c, g[1])
            else:
                put(r, c, BRACE_EXT)

    if clashes:
        detail = "; ".join(f"row {r} col {c}: '{had}' overdrawn by '{ch}'" for r, c, had, ch in clashes[:6])
        sys.exit(f"FATAL: {len(clashes)} delimiter/text collision(s) — the figure would be silently corrupt. "
                 f"{detail}")
    return grid, marks


# ── Where a figure starts and stops is a question about the STANDARD'S STRUCTURE, not about geometry ──
# The first version of this asked geometry — split wherever consecutive rows were more than N points apart —
# and it cannot work, for a reason worth recording: row spacing WITHIN one general format varies more than the
# spacing BETWEEN two of them. ACCEPT Format 3 steps its operand rows 4.9 pt apart and its phrase groups 24 pt
# apart, while COMPUTE's two formats sit 31 pt apart; no threshold separates those. So it split single figures
# and merged neighbouring ones, and a trailing `[ END-xxx ]` got swallowed into the exception group above it,
# where the bracket then drew straight through the text.
#
# The standard already marks its own boundaries, and marks them unambiguously:
#
#   * a bold NUMBERED CLAUSE HEADING opens and closes every region — `14.9.41.2 General format` opens the
#     figures, `14.9.41.3 Syntax rules` closes them. Only a region opened by a "General format(s)" heading
#     holds figures, which is also what keeps reserved-word TABLES out: they are all-uppercase like a figure
#     and their grid rules measure identically to choice-indicator bars, but they live under other headings.
#   * within that region a `Format N (label):` line separates one format from the next.
#
# Note that font SIZE is not usable as the signal, tempting though it looks: figure text is 9.7 pt on page 784
# and 10.7 pt — indistinguishable from the prose beside it — on pages 632 and 607. Each figure is set to fit.
BODY_TOP, BODY_BOTTOM = 60.0, 730.0     # inside the running header and the folio / licence footer
CLAUSE_HEADING = re.compile(r"^\d+(?:\.\d+)+\s+\S")
GENERAL_FORMAT = re.compile(r"general\s+formats?$", re.I)
FORMAT_LABEL = re.compile(r"^Formats?\s+\d+\b")
PAD_ABOVE, PAD_BELOW = 6.0, 14.0        # a band must reach the bracket feet below its last row of text


def headings(page):
    """Every bold numbered clause heading in the body, as (y, text) — the hard structural boundaries.

    The numbering test earns its place: bold also marks emphasis inside prose, and the running header is bold
    11.7 pt exactly like a heading (the body band already excludes it, but the check costs nothing).
    """
    out = []
    for blk in page.get_text("dict")["blocks"]:
        if blk["type"] != 0:
            continue
        for line in blk["lines"]:
            y = line["bbox"][1]
            spans = [s for s in line["spans"] if s["text"].strip()]
            if not (BODY_TOP <= y <= BODY_BOTTOM) or not spans:
                continue
            if not all("bold" in s["font"].lower() for s in spans) or max(s["size"] for s in spans) < 11.0:
                continue
            text = re.sub(r"\s+", " ", "".join(s["text"] for s in spans)).strip()
            if CLAUSE_HEADING.match(text):
                out.append((y, text))
    return sorted(out)


def classify_row(words):
    """What a row IS: a `Format N` label, prose, a figure row, or glue.

    GLUE is a row carrying only brace pieces or stray punctuation. It belongs to a figure but can never be its
    edge, so it extends a run without being able to start one — otherwise a brace's top hook, which sits above
    the first row of text, would drag the band up past the format label above it.
    """
    text = " ".join(w["text"] for w in sorted(words, key=lambda w: w["x0"]))
    if FORMAT_LABEL.match(text):
        return "label"
    if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in words):
        return "prose"                                 # a plain lower-case word; a general format has none
    if not re.search(r"[a-z]+-[a-z0-9]|[A-Z]{2,}", text):
        return "glue"                                  # neither an operand nor a reserved word
    return "figure"


def page_rows(page, extract):
    """Body rows as (y, kind, words), top to bottom. Page furniture falls outside the body band."""
    words = [w for w in extract(page) if BODY_TOP <= w["y0"] <= BODY_BOTTOM]
    if not words:
        return []
    rowof = cluster([w["y0"] for w in words], tol=3.0)
    grouped = collections.defaultdict(list)
    for w in words:
        grouped[rowof[w["y0"]]].append(w)
    return [(min(w["y0"] for w in g), classify_row(g), g) for _, g in sorted(grouped.items())]


def continues_from_previous(page) -> bool:
    """True when the previous page ends INSIDE a general-format region, so this page opens still in one.

    A region that runs over a page break leaves the continuation page with no heading of its own. The standard
    usually re-labels it (`Format 3 (screen):` opens page 607), but relying on the label alone would miss a
    format long enough to break mid-figure.
    """
    doc, n = page.parent, page.number
    if doc is None or n <= 0:
        return False
    prev = headings(doc[n - 1])
    return bool(prev and GENERAL_FORMAT.search(prev[-1][1]))


def find_bands(page, extract):
    """The y-band of every general-format figure on the page, located from the clause structure.

    Returns [(y0, y1), ...] top to bottom, each ready to hand to `build`. A page carrying no general-format
    region returns [] — which is the correct answer for prose, tables and the reserved-word lists, not a
    failure to find something.
    """
    rows = page_rows(page, extract)
    if not rows:
        return []
    heads = headings(page)
    edges = [BODY_TOP] + [y for y, _ in heads] + [BODY_BOTTOM]
    titles = [None] + [t for _, t in heads]

    runs = []
    for i, title in enumerate(titles):
        top, bot = edges[i], edges[i + 1]
        inside = [r for r in rows if top < r[0] < bot]
        if not inside:
            continue
        if title is not None:
            if not GENERAL_FORMAT.search(title):
                continue
        elif inside[0][1] != "label" and not continues_from_previous(page):
            continue                                   # a leading region that is not a figure continuation

        # Split at each `Format N` label, then take the maximal runs of figure rows within each segment. Prose
        # inside the region — the "where rounded-phrase is described in …" note that follows COMPUTE — closes
        # a run rather than joining it. Glue joins a run at either end (a brace's top hook sits ~6 pt above
        # the first row of text, and clipping it off left the brace starting one row down, its point off
        # centre); a run of glue ALONE is not a figure, so a run must contain at least one figure row.
        run, has_figure, above = [], False, top
        for y, kind, _ in inside + [(bot, "prose", [])]:
            if kind in ("figure", "glue"):
                run.append(y)
                has_figure = has_figure or kind == "figure"
            else:                                      # a label or a prose row closes whatever is open
                if run and has_figure:
                    runs.append((run[0], run[-1], above, y))
                run, has_figure, above = [], False, y
    if not runs:
        return []

    # Pad to reach the bracket feet, then clip to the MIDPOINT between the figure and whatever row delimits
    # it — the label or prose above, the next label or the closing heading below. A midpoint is the bound that
    # cannot clip a delimiter off the figure while still refusing to reach into its neighbour.
    bands = []
    for lo, hi, above, below in runs:
        bands.append([max(lo - PAD_ABOVE, (above + lo) / 2), min(hi + PAD_BELOW, (hi + below) / 2)])
    bands.sort()
    for a, b in zip(bands, bands[1:]):
        if a[1] >= b[0]:
            mid = (a[1] + b[0]) / 2
            a[1], b[0] = mid, mid
    return [(lo, hi) for lo, hi in bands]


def render(grid, marks, plain_only):
    """The figure's lines, with the `<u>` tags of 5.2.2 inserted unless suppressed."""
    plain = ["".join(r).rstrip() for r in grid]
    out = list(plain)
    if not plain_only:
        for r in range(len(out)):
            for c, ln in sorted(marks[r], reverse=True):
                if c < len(out[r]):
                    out[r] = out[r][:c] + "<u>" + out[r][c:c + ln] + "</u>" + out[r][c + ln:]
        # The tags must occupy NO layout, or every column after them shifts. Proving that is what makes it safe
        # to run this over 254 figures rather than eyeballing each one.
        if [re.sub(r"</?u>", "", l) for l in out] != plain:
            sys.exit("FATAL: inserting <u> changed the laid-out text — alignment would be wrong")
    return out


def emit(grid, marks, plain_only):
    print("<pre>")
    for line in render(grid, marks, plain_only):
        print(line)
    print("</pre>")


# The sheet exists because a figure style cannot be reviewed as source, only as output — the rule that settled
# FIGURE-STYLE.md, four of whose six rules came from someone looking at a rendering and spotting a defect that
# was invisible in the markup. `line-height: 1` is reproduced here deliberately: box-drawing glyphs only tile
# into continuous rules at exactly one em, so a sheet without it would show gaps this generator did not create.
SHEET_CSS = """
body { background:#fbfbfa; color:#1a1a19; font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;
       margin:0 auto; padding:2.5rem 1.5rem; max-width:70rem; }
h1 { font-size:1.4rem; margin:0 0 .3rem; } h2 { font-size:.95rem; margin:2.2rem 0 .5rem; font-weight:600; }
p.note { color:#5a5a57; font-size:.85rem; margin:.2rem 0 1.6rem; }
.meta { color:#78786f; font-size:.78rem; font-family:ui-monospace,Consolas,monospace; margin:0 0 .35rem; }
pre { font-family:Consolas,'Cascadia Mono','Lucida Console',ui-monospace,monospace;
      line-height:1; font-size:13px; background:#fff; border:1px solid #e4e4df; border-radius:4px;
      padding:1rem; overflow-x:auto; margin:0; }
pre u { text-decoration-thickness:1px; text-underline-offset:2px; }
@media (prefers-color-scheme:dark){ body{background:#16161a;color:#e8e8e3;} p.note{color:#9a9a94;}
  .meta{color:#87877e;} pre{background:#1e1e23;border-color:#33333a;} }
"""


def sheet(doc, pages, out_path, extract, classify_page):
    """Render every figure on the given pages into one standalone HTML page."""
    parts = [f"<style>{SHEET_CSS}</style>", "<h1>General-format figures, generated from measurement</h1>",
             '<p class="note">Every figure below is laid out from the printed page: the words and their '
             'positions from the text layer, the underlining from the underline rectangles, the brackets, '
             'braces, parentheses and choice-indicator bars from the delimiter geometry. Nothing is placed by '
             'hand. Bands are located from the clause structure, not from spacing.</p>']
    for pno in pages:
        page = doc[pno - 1]
        heads = [t for _, t in headings(page) if GENERAL_FORMAT.search(t)]
        title = heads[0] if heads else "(continued from the previous page)"
        bands = find_bands(page, extract)
        for n, (lo, hi) in enumerate(bands, 1):
            grid, marks = build(page, lo, hi, extract, classify_page)
            if grid is None:
                continue
            body = "\n".join(render(grid, marks, False))
            parts.append(f"<h2>{title}</h2>")
            parts.append(f'<p class="meta">printed folio {pno - 30} &middot; PDF page {pno} &middot; '
                         f"figure {n} of {len(bands)} &middot; y {lo:.0f}–{hi:.0f}</p>")
            parts.append(f"<pre>{body}</pre>")
    out_path.write_text("\n".join(parts), encoding="utf-8")
    return len(parts)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", type=int, help="PDF page number (printed folio + 30)")
    ap.add_argument("--band", nargs=2, type=float, metavar=("Y0", "Y1"),
                    help="render ONE band explicitly, instead of the figures the clause structure locates")
    ap.add_argument("--plain", action="store_true", help="omit the <u> tags")
    ap.add_argument("--bands-only", action="store_true", help="report the located bands; render nothing")
    ap.add_argument("--sheet", metavar="OUT.html", help="render PAGE plus --also into one HTML sheet")
    ap.add_argument("--also", nargs="*", type=int, default=[], help="further pages for --sheet")
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
    page = doc[args.page - 1]

    if args.sheet:
        out = pathlib.Path(args.sheet)
        sheet(doc, [args.page] + args.also, out, extract, classify_page)
        print(f"wrote {out}")
        return 0

    if not args.band:
        bands = find_bands(page, extract)
        if args.bands_only:
            for lo, hi in bands:
                print(f"{lo:.1f} {hi:.1f}")
            return 0
        if not bands:
            sys.exit("no general-format figure on this page — no clause region here is headed 'General format'")
        for n, (lo, hi) in enumerate(bands, 1):
            g, m = build(page, lo, hi, extract, classify_page)
            if g is None:
                continue
            print(f"<!-- figure {n} of {len(bands)}, y {lo:.0f}–{hi:.0f} -->")
            emit(g, m, args.plain)
        return 0

    grid, marks = build(page, args.band[0], args.band[1], extract, classify_page)
    if grid is None:
        sys.exit("no figure content in that band")

    emit(grid, marks, args.plain)
    return 0


if __name__ == "__main__":
    sys.exit(main())
