#!/usr/bin/env python3
"""Read a DRAWN general format back into the four things it carries, and compare two drawings of one format.

WHY THIS EXISTS (kb/Work PB4849). The 1985 transcription's general formats were drawn by a model from the page image (the
batch of PB4816), and each is now also REGENERATED from the measured page (`figure_raster` reads the scan, `render_figure`
draws it). Two independent drawings of one printed format either agree or one of them is wrong, and the four things a
general format carries are what they must agree on (PB4812, FIGURE-STYLE.md):

    grouping     the brackets (§2023:5.2.6.2: at most one of the options) and braces (§2023:5.2.6.3: exactly one) that hold a word
    bars         the choice indicators (§2023:5.2.6.4) that hold it
    underlines   which of its letters are underlined (§2023:5.2.2: an underlined word is required)
    alignment    whether the next word is printed beside it or stacked below it (§2023:5.2.6.1: stacked words are alternatives)

and, on a scan, a fifth: the BOXES that hold it (a 1985 box marks syntax unique to level 2 of a module; it groups and
chooses nothing, FIGURE-STYLE.md rule 9). Either drawing's box is read: the house style's double-line sides, and the
batch's full double-line rectangle.

The two drawings are TEXT: the generated one in the house style (box drawing, FIGURE-STYLE.md), the batch's in whatever
it chose (box drawing, the Miscellaneous Technical brace pieces, typed `[ ]`). `read` turns either into words, each with
its row, its column, its underlined letters and the delimiters around it; `compare` pairs the two word lists and names
every word on which they differ, and how. It reads, it never decides: a disagreement goes to a refuter who looks at the
printed page. When the page bears the batch's drawing out, `restyle` redraws it in the house style, reading the same.

    python scripts/spec/figure_reading.py --self-test
"""
from __future__ import annotations

import argparse
import collections
import html
import pathlib
import re
import sys
from typing import NamedTuple

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

# A multi-row delimiter is a column of these glyphs: (family, side) for its top and bottom pieces; the middle pieces
# carry a side only when they are a brace's point. `round` is a curved top that becomes a brace when its column carries
# a point and a parenthesis when it does not (FIGURE-STYLE.md rules 3 and 6).
TOP = {"┌": ("bracket", "L"), "┐": ("bracket", "R"), "⎡": ("bracket", "L"), "⎤": ("bracket", "R"),
       "╭": ("round", "L"), "╮": ("round", "R"), "⎧": ("brace", "L"), "⎫": ("brace", "R"),
       "╔": ("box", "L"), "╗": ("box", "R")}
BOTTOM = {"└": ("bracket", "L"), "┘": ("bracket", "R"), "⎣": ("bracket", "L"), "⎦": ("bracket", "R"),
          "╰": ("round", "L"), "╯": ("round", "R"), "⎩": ("brace", "L"), "⎭": ("brace", "R"),
          "╚": ("box", "L"), "╝": ("box", "R")}
POINT = {"┤": "L", "├": "R", "⎨": "L", "⎬": "R"}
BOX_SIDE = "║"                       # a level-2 box's side, in double lines: no other family uses them
EXTEND = set("│⎪⎢⎥") | {BOX_SIDE}
COLUMN_GLYPHS = set(TOP) | set(BOTTOM) | set(POINT) | EXTEND
RULE_GLYPHS = set("─═┬┴┼")            # a box's top and bottom rules: no word, no delimiter
ONE_ROW = {"[": ("bracket", "L"), "]": ("bracket", "R"), "{": ("brace", "L"), "}": ("brace", "R"),
           "(": ("paren", "L"), ")": ("paren", "R")}
BAR = "|"
#: The four things, in the order a report names them, and a scan's level-2 boxes (FIGURE-STYLE.md rule 9).
THINGS = ("grouping", "bars", "underlines", "alignment", "boxes")


class Enclosure(NamedTuple):
    family: str                     # bracket, brace, paren, bar, box
    top: int                        # first and last row it holds
    bottom: int
    left: int                       # its two delimiters' columns
    right: int


class Word(NamedTuple):
    text: str
    row: int
    col: int
    underlined: tuple               # ((offset, length), ...) of underlined letters; () when none
    holders: tuple                  # the families of the enclosures holding it, outermost first
    inner: Enclosure | None = None  # the innermost of them


def plain_row(line: str) -> tuple[str, set]:
    """A drawn row without its markup: (the text, the set of underlined columns). `<u>` takes no column (it is zero
    width when rendered), and an HTML entity is one character."""
    text, under, inside = [], set(), False
    for part in re.split(r"(</?u>|&[a-zA-Z]+;|&#\d+;)", line):
        if part == "<u>":
            inside = True
        elif part == "</u>":
            inside = False
        elif part:
            chars = html.unescape(part) if part.startswith("&") else part
            for ch in chars:
                if inside:
                    under.add(len(text))
                text.append(ch)
    return "".join(text), under


#: Drawn notation that is no word and no delimiter `read` pairs: the batch's arrows and broken bars.
OTHER_NOTATION = set("▼►◄↓¦")


def token_rows(lines: list[str]) -> list[list[tuple[str, int]]]:
    """A drawn figure's words as `figure_raster.token_rows` takes them: [[(word, column), ...] per row], blank rows
    kept (their count is the figure's height). The multi-row delimiter glyphs are dropped; the typed one-row ones stay in
    the word they are set against, as the printed page sets them (`[ON`), and `figure_raster.split_token` splits them.

    A word is given as PRINTED, whichever style drew it, so a figure regenerated from its own house-style drawing pairs
    as it did from the batch's: the house style's ellipsis `…` (render_figure.TEXT_GLYPHS) is the page's three dots,
    `...` (the pairing reads a word's length from its glyphs, and the scan prints three). The spaces around a typed
    delimiter stay as drawn: the batch draws the page's own (`} ... ] ...`, PDF page 592), so none can be taken away."""
    out = []
    for line in lines:
        text, _ = plain_row(line)
        text = "".join(" " if ch in COLUMN_GLYPHS or ch in RULE_GLYPHS or ch in OTHER_NOTATION else ch for ch in text)
        out.append([(m.group(0).replace("…", "..."), m.start()) for m in re.finditer(r"\S+", text)])
    return out


def _columns(grid: list[str]) -> list[tuple[str, str | None, int, int, int]]:
    """The multi-row delimiters: (family, side, column, first row, last row) per column run of COLUMN_GLYPHS, a run
    ending at a bottom piece."""
    width = max((len(r) for r in grid), default=0)
    out = []
    for c in range(width):
        run: list[tuple[int, str]] = []
        for r in range(len(grid) + 1):
            ch = grid[r][c] if r < len(grid) and c < len(grid[r]) else " "
            if ch in COLUMN_GLYPHS and not (ch in TOP and run):
                run.append((r, ch))
                if ch in BOTTOM:
                    out.append(_column(c, run))
                    run = []
                continue
            if run:
                out.append(_column(c, run))
            run = [(r, ch)] if ch in TOP else []
    return [d for d in out if d is not None]


def _column(c: int, run: list[tuple[int, str]]):
    glyphs = [g for _, g in run]
    family, side = None, None
    for g in glyphs:
        if g in TOP:
            family, side = TOP[g]
        elif g in BOTTOM and family is None:
            family, side = BOTTOM[g]
    points = [POINT[g] for g in glyphs if g in POINT]
    if family == "round":
        family = "brace" if points else "paren"
    elif family is None:
        if points:
            family, side = "brace", points[0]
        elif set(glyphs) == {BOX_SIDE}:
            family = "box"                         # a box side with no corner: a one-row box (FIGURE-STYLE.md rule 9)
        else:
            family = "bar"                         # a choice bar, one row or more, drawn with the column glyph
    return family, side, c, run[0][0], run[-1][0]


def _pair(delims: list[tuple[str, str | None, int, int, int]]) -> tuple[list[Enclosure], list[tuple]]:
    """Pair openers with closers: the nearest unpaired closer of the same family to the right whose rows overlap the
    opener's by half the shorter, innermost first. Bars pair left to right among themselves the same way. Returns the
    pairs and the delimiters left without a partner."""
    out, used = [], set()
    openers = sorted((k for k, d in enumerate(delims) if d[1] == "L"), key=lambda k: -delims[k][2])
    for o in openers:
        fam, _, c, r0, r1 = delims[o]
        best = None
        for k, (f2, s2, c2, q0, q1) in enumerate(delims):
            if k in used or f2 != fam or s2 != "R" or c2 <= c:
                continue
            if min(r1, q1) - max(r0, q0) + 1 < 0.5 * min(r1 - r0 + 1, q1 - q0 + 1):
                continue
            if best is None or c2 < delims[best][2]:
                best = k
        if best is not None:
            used |= {best, o}
            out.append(Enclosure(fam, min(r0, delims[best][3]), max(r1, delims[best][4]), c, delims[best][2]))
    for family in ("bar", "box"):                 # a bar, and a box side drawn without a corner, have no hand
        bars = sorted((k for k, d in enumerate(delims) if d[0] == family and d[1] is None), key=lambda k: delims[k][2])
        for i, a in enumerate(bars):
            if a in used:
                continue
            for b in bars[i + 1:]:
                if b in used:
                    continue
                _, _, c0, r0, r1 = delims[a]
                _, _, c1, q0, q1 = delims[b]
                if min(r1, q1) - max(r0, q0) + 1 >= 0.5 * min(r1 - r0 + 1, q1 - q0 + 1):
                    used |= {a, b}
                    out.append(Enclosure(family, min(r0, q0), max(r1, q1), c0, c1))
                    break
    return out, [d for k, d in enumerate(delims) if k not in used]


def _delimiters(lines: list[str]) -> list[tuple[str, str | None, int, int, int]]:
    rows = [plain_row(line)[0] for line in lines]
    delims = _columns(rows)
    for r, text in enumerate(rows):
        for c, ch in enumerate(text):
            if ch in ONE_ROW:
                delims.append((ONE_ROW[ch][0], ONE_ROW[ch][1], c, r, r))
            elif ch == BAR:
                delims.append(("bar", None, c, r, r))
    return delims


def enclosures(lines: list[str]) -> list[Enclosure]:
    """Every delimiter pair a drawn figure draws: brackets, braces, parentheses, choice bars and boxes."""
    return _pair(_delimiters(lines))[0]


def strays(lines: list[str]) -> list[tuple[str, str | None, int, int]]:
    """Every delimiter a drawn figure draws that pairs with nothing, as (family, side, first row, last row), in reading
    order. The 1985 page sets a typed bracket that opens on one line and closes on another (`[report-description-entry`
    ... `] ...`, PDF page 592), which no row pairs; a stroke beside a figure (the left side of page 522's page-wide
    level-2 box, measured alone, drawn as a lone `│`) is one too."""
    left = _pair(_delimiters(lines))[1]
    return [(fam, side, r0, r1) for fam, side, _, r0, r1 in sorted(left, key=lambda d: (d[3], d[2]))]


def read(lines: list[str]) -> list[Word]:
    """The words of a drawn figure in reading order (row, then column), each with what holds it."""
    rows = [plain_row(line) for line in lines]
    encl = enclosures(lines)
    words = []
    stop = COLUMN_GLYPHS | RULE_GLYPHS | set(ONE_ROW) | {BAR, " "}
    for r, (text, under) in enumerate(rows):
        for m in re.finditer(r"[^" + re.escape("".join(sorted(stop))) + r"]+", text):
            a, b = m.start(), m.end()
            runs, cur = [], None
            for i in range(a, b):
                if i in under and cur is None:
                    cur = i
                elif i not in under and cur is not None:
                    runs.append((cur - a, i - cur))
                    cur = None
            if cur is not None:
                runs.append((cur - a, b - cur))
            held = sorted((e for e in encl if e.top <= r <= e.bottom and e.left < a and b <= e.right),
                          key=lambda e: e.left - e.right)
            words.append(Word(m.group(0), r, a, tuple(runs), tuple(e.family for e in held), held[-1] if held else None))
    return words


def _grouping(w: Word) -> tuple:
    return tuple(f for f in w.holders if f in ("bracket", "brace", "paren"))


def _bars(w: Word) -> int:
    return sum(1 for f in w.holders if f == "bar")


def _boxes(w: Word) -> int:
    return sum(1 for f in w.holders if f == "box")


#: Which of THINGS a difference in an enclosure family's count is.
FAMILY_THING = {"bar": "bars", "box": "boxes"}


def _family_counts(lines: list[str]) -> collections.Counter:
    """How many enclosures of each family a drawing draws, holding a word or not."""
    return collections.Counter(e.family for e in enclosures(lines))


def _underlined_delimiters(lines: list[str]) -> int:
    """How many delimiter or column glyphs a drawing underlines: a delimiter is never a required word (§2023:5.2.2
    underlines words), so one underlined is a rule the measurer took for an underline."""
    n = 0
    for line in lines:
        text, under = plain_row(line)
        n += sum(1 for c in under if c < len(text) and (text[c] in ONE_ROW or text[c] == BAR or text[c] in COLUMN_GLYPHS))
    return n


def _antecedent(lines: list[str], words: list[Word], w: Word) -> tuple:
    """What an ellipsis `w` repeats (§2023:5.2.7: the portion of the format just before it): the word, or the enclosure
    with the words it holds, that ends nearest to its left on its row; () when nothing does."""
    def spelt(t: str) -> str:
        return t.replace("...", "…")
    best, out = -1, ()                            # the last column of what ends nearest
    for x in words:
        last = x.col + len(x.text) - 1
        if x.row == w.row and x is not w and best < last < w.col:
            best, out = last, ("word", spelt(x.text))
    for e in enclosures(lines):
        if e.top <= w.row <= e.bottom and best < e.right < w.col:
            held = tuple(spelt(y.text) for y in words if e.top <= y.row <= e.bottom and e.left < y.col
                         and y.col + len(y.text) <= e.right)
            best, out = e.right, (e.family, held)
    return out


def compare(drawn: list[str], generated: list[str]) -> dict:
    """How two drawings of one format differ: {"words": [...], thing: [...] for each of THINGS}; every list empty when
    they agree. Words are paired in reading order by their text (difflib); a word only one side has is in "words"."""
    a, b = read(drawn), read(generated)
    out: dict = {"words": []}
    for t in THINGS:
        out[t] = []
    def spelling(w: Word) -> str:                 # the ellipsis is set `...` or `…` (render_figure.TEXT_GLYPHS)
        return w.text.replace("...", "…")
    # The n-th word of a spelling pairs with the n-th of that spelling, in reading order. Reading order alone cannot
    # pair them: a brace's first alternative is printed half a line ABOVE the word before the brace (ADD's
    # `identifier-1`, page 305), so one drawing reads it first and the other second.
    seen_a, seen_b = collections.defaultdict(list), collections.defaultdict(list)
    for w in a:
        seen_a[spelling(w)].append(w)
    for w in b:
        seen_b[spelling(w)].append(w)
    partner = {}
    for key in seen_a.keys() | seen_b.keys():
        xs, ys = seen_a.get(key, []), seen_b.get(key, [])
        partner.update({id(x): y for x, y in zip(xs, ys)})
        out["words"] += [f"drawn only: {w.text}" for w in xs[len(ys):]] + [f"generated only: {w.text}" for w in ys[len(xs):]]
    # A stroke the GENERATED drawing leaves unpaired, beyond those the drawn one leaves (a bracket set across two lines
    # is unpaired in both), holds no word: the comparison of holders below cannot see it, and it would be written.
    extra = collections.Counter((f, s) for f, s, _, _ in strays(generated)) - \
        collections.Counter((f, s) for f, s, _, _ in strays(drawn))
    out["grouping"] += [f"generated: a {f} in rows {r0}-{r1} pairs with nothing"
                        for f, s, r0, r1 in strays(generated) if extra[(f, s)]]
    pairs = [(x, partner[id(x)]) for x in a if id(x) in partner]
    # An ellipsis repeats what is printed just before it (§2023:5.2.7), so that is part of its alignment: page 399's
    # `{data-name-1} ...` came out with the ellipsis a row lower, after the brace of `RECORD IS` / `RECORDS ARE`.
    for x, y in pairs:
        if spelling(x) == "…" and _antecedent(drawn, a, x) != _antecedent(generated, b, y):
            out["alignment"].append(f"the ellipsis on drawn row {x.row}: drawn after {_antecedent(drawn, a, x)}, "
                                    f"generated after {_antecedent(generated, b, y)}")
    for x, y in pairs:
        if _grouping(x) != _grouping(y):
            out["grouping"].append(f"{x.text}: drawn {'/'.join(_grouping(x)) or '-'}, generated {'/'.join(_grouping(y)) or '-'}")
        if _bars(x) != _bars(y):
            out["bars"].append(f"{x.text}: drawn {_bars(x)}, generated {_bars(y)}")
        if _boxes(x) != _boxes(y):
            out["boxes"].append(f"{x.text}: drawn {_boxes(x)}, generated {_boxes(y)}")
        if x.underlined != y.underlined:
            out["underlines"].append(f"{x.text}: drawn {list(x.underlined)}, generated {list(y.underlined)}")
    # An enclosure that holds no word, and a delimiter underlined, are invisible to the words' holders and letters: page
    # 321's `GO TO [procedure-name-1]` came out `│ <u>[</u> │ procedure-name-1 │ <u>]</u> │` (each typed bracket in a level-2
    # box too small to measure as one) and agreed word for word (kb/Work PB4849). So the drawings' enclosures are
    # counted by family, and their underlined delimiters, where the words have not named the difference already.
    fa, fb = _family_counts(drawn), _family_counts(generated)
    for fam in sorted(fa.keys() | fb.keys()):
        if fa[fam] != fb[fam] and not out[FAMILY_THING.get(fam, "grouping")]:
            out[FAMILY_THING.get(fam, "grouping")].append(f"{fam}: drawn {fa[fam]}, generated {fb[fam]}")
    ua, ub = _underlined_delimiters(drawn), _underlined_delimiters(generated)
    if ua != ub:
        out["underlines"].append(f"underlined delimiters: drawn {ua}, generated {ub}")
    # ALIGNMENT is §2023:5.2.6.1's: inside one group, words on one row are in sequence and words on different rows are
    # stacked alternatives. Outside every group a row that ends simply continues the format, so only words that share
    # their innermost group in the drawing are compared.
    for (x0, y0), (x1, y1) in zip(pairs, pairs[1:]):
        if not x0.holders or x0.inner != x1.inner:
            continue
        def relation(p, q):
            return "beside" if p.row == q.row else "stacked"
        if relation(x0, x1) != relation(y0, y1):
            out["alignment"].append(f"{x0.text} -> {x1.text}: drawn {relation(x0, x1)}, generated {relation(y0, y1)}")
    return out


#: The batch's Miscellaneous Technical delimiter pieces, which FIGURE-STYLE.md rule 2 forbids in a written figure (no
#: monospace font carries them), and the box-drawing piece each is redrawn as (rules 3 and 6).
BATCH_GLYPHS = {"⎧": "╭", "⎫": "╮", "⎩": "╰", "⎭": "╯", "⎨": "┤", "⎬": "├", "⎪": "│",
                "⎡": "┌", "⎤": "┐", "⎣": "└", "⎦": "┘", "⎢": "│", "⎥": "│"}
#: A delimiter's middle piece, by family and side, for a row `restyle` adds inside it.
_EXTENSION = {("bracket", "L"): "│", ("bracket", "R"): "│", ("brace", "L"): "⎪", ("brace", "R"): "⎪",
              ("paren", "L"): "│", ("paren", "R"): "│", ("bar", None): "│", ("box", "L"): BOX_SIDE,
              ("box", "R"): BOX_SIDE, ("box", None): BOX_SIDE}
_POINT_OF = {"L": "⎨", "R": "⎬"}
_ONE_ROW_OF = {("bracket", "L"): "[", ("bracket", "R"): "]", ("brace", "L"): "{", ("brace", "R"): "}"}


def emit_row(text: str, under: set) -> str:
    """`plain_row` inverted: a row's text with its underlined columns wrapped in `<u>`, `<`, `>` and `&` written as
    entities (a `<pre>` block is raw HTML: FIGURE-STYLE.md, lint_rendering's SWALLOWED)."""
    out, inside = [], False
    for c, ch in enumerate(text):
        if (c in under) != inside:
            out.append("<u>" if not inside else "</u>")
            inside = not inside
        out.append(html.escape(ch, quote=False))
    if inside:
        out.append("</u>")
    return "".join(out)


def restyle(lines: list[str], strict: bool = True) -> list[str]:
    """The batch's drawing of a format redrawn in the house style (FIGURE-STYLE.md), carrying the same words and the
    same four things and boxes (`compare` is asserted empty): what a refuter writes when the printed page bears the
    batch's drawing out (`sweep_figures` verdict `drawn`, kb/Work PB4849). The words keep their rows and columns; only
    the notation changes:
    - rule 5: a bracket or brace two rows tall gains a blank middle row, the delimiters crossing it extended through it
      (a two-row brace's point goes there);
    - rule 3: a brace's pieces become `╭ ┤ ╰` / `╮ ├ ╯`, a bracket's `┌ │ └` / `┐ │ ┘` (rule 2: the Miscellaneous
      Technical pieces are in no monospace font), and a brace of three rows or more drawn without a point gets one on
      its middle row, the piece that tells it from a parenthesis;
    - rule 7: a multi-row piece standing on one row is the typed delimiter `[ ] { }`;
    - the ellipsis is the house `…` (render_figure.TEXT_GLYPHS), its two spare columns kept as spaces so that every
      later word keeps its column.
    A full double-line box rectangle (rule 9 draws a box's sides only: its rules need rows of their own) and the
    batch's other notation (arrows, broken bars) have no house form this can derive: ValueError, and a refuter draws
    that figure by hand (verdict `neither`). So is, while `strict`, a drawing holding a multi-row piece that pairs with
    nothing: usually its columns are off (the `<u>` tags hide a one-column slip), and the redrawing would keep the
    wrong reading. A refuter who has checked the page's own unpaired delimiter (a bracket opened on one line and closed
    on the next, PDF page 245) passes `strict=False`."""
    plain = [plain_row(line) for line in lines]
    for text, _ in plain:
        odd = sorted(set(text) & (RULE_GLYPHS | OTHER_NOTATION))
        if odd:
            raise ValueError(f"no house form for {''.join(odd)!r} (a box's rules, or other notation): draw it by hand")
    # A multi-row piece that pairs with nothing is a drawing whose columns are off (a closer set one column left of
    # the rows beside it, PDF page 253): it reads wrongly, and redrawing it would keep the wrong reading.
    # A corner alone on its row is the page's typed delimiter, which may pair across lines (`[report-description-entry`
    # ... `] ...`, PDF page 592); rule 7 types it below.
    # A corner beside its partner one column off (`┌A ┐` over `└B┘`) is the misdrawn case.
    columns = set(_columns([t for t, _ in plain]))
    left = [d for d in _pair(_delimiters(lines))[1] if d in columns]
    corners = [d for d in columns if d[3] == d[4] and plain[d[3]][0][d[2]] in set(TOP) | set(BOTTOM)]
    lost = [d for d in left if d not in corners] + [
        d for d in left if d in corners and any(e != d and e[:2] == d[:2] and abs(e[3] - d[3]) == 1
                                                and abs(e[2] - d[2]) <= 1 for e in corners)]
    if lost and strict:
        fam, side, c, r0, r1 = lost[0]
        raise ValueError(f"a {fam} piece in column {c}, rows {r0}-{r1}, pairs with nothing: the drawing's columns are off")
    grid, under = [list(t) for t, _ in plain], [set(u) for _, u in plain]

    def cell(r, c):
        return grid[r][c] if c < len(grid[r]) else " "

    while True:                                   # rule 5, one two-row group at a time (rows shift after each)
        runs = _columns(["".join(g) for g in grid])
        short = [d for d in runs if d[0] in ("bracket", "brace") and d[4] - d[3] == 1]
        if not short:
            break
        top = short[0][3]
        width = max(len(g) for g in grid)
        row = [" "] * width
        for fam, side, c, r0, r1 in runs:
            if r0 <= top < r1:
                row[c] = _POINT_OF[side] if (fam == "brace" and r1 - r0 == 1 and not any(
                    cell(r, c) in POINT for r in (r0, r1))) else _EXTENSION.get((fam, side), "│")
        grid.insert(top + 1, row)
        under.insert(top + 1, set())
    def wordy(r, c):
        ch = cell(r, c) if c >= 0 else " "
        return ch != " " and ch not in COLUMN_GLYPHS and ch not in BATCH_GLYPHS and ch not in ONE_ROW and ch != BAR

    def clear(fam_side_c_r0_r1) -> int | None:
        """Where a blank column goes to set a multi-row delimiter one space clear of what it holds (the house layout,
        and rule 4 for a choice bar beside a delimiter); None when it is clear, or when a column there would split a
        word on some row (a word of another row's clause crossing the delimiter's column)."""
        _, side, c, r0, r1 = fam_side_c_r0_r1
        at = c + 1 if side == "L" else c
        near = c + 1 if side == "L" else c - 1
        if near < 0 or all(cell(r, near) == " " for r in range(r0, r1 + 1)):
            return None
        return None if any(wordy(r, at - 1) and wordy(r, at) for r in range(len(grid))) else at

    moved = True
    while moved:
        moved = False
        for d in _columns(["".join(g) for g in grid]):
            at = clear(d) if d[1] in ("L", "R") else None
            if at is not None:
                for r in range(len(grid)):
                    if at <= len(grid[r]):
                        grid[r].insert(at, " ")
                    under[r] = {x + 1 if x >= at else x for x in under[r]}
                moved = True
                break
    for fam, side, c, r0, r1 in _columns(["".join(g) for g in grid]):
        if fam == "brace" and r1 - r0 >= 2 and not any(cell(r, c) in POINT for r in range(r0, r1 + 1)):
            grid[(r0 + r1) // 2][c] = _POINT_OF[side]  # a brace without its point would read as a parenthesis
        if r0 == r1 and (fam, side) in _ONE_ROW_OF and cell(r0, c) in set(TOP) | set(BOTTOM):
            grid[r0][c] = _ONE_ROW_OF[(fam, side)]
    out = []
    for g, u in zip(grid, under):
        text = "".join(BATCH_GLYPHS.get(ch, ch) for ch in g).replace("...", "…  ")
        keep = len(text.rstrip())
        out.append(emit_row(text[:keep], {c for c in u if c < keep}))
    diff = {t: v for t, v in compare(lines, out).items() if v}
    if diff:
        raise ValueError(f"the redrawn figure reads differently from the drawing: {diff}")
    return out


def foreign_glyphs(lines: list[str]) -> str:
    """The delimiter pieces in `lines` that FIGURE-STYLE.md rule 2 forbids (the batch's Miscellaneous Technical ones)."""
    return "".join(sorted({ch for line in lines for ch in line if ch in BATCH_GLYPHS}))


def self_test() -> int:
    fails, ran = [], []

    def check(name, ok, detail=""):
        ran.append(name)
        if not ok:
            fails.append(f"{name} {detail}")

    house = ["    ╭ identifier-1 ╮",
             "<u>ADD</u> ┤              ├ … <u>TO</u> { identifier-2 [ <u>ROUNDED</u> ] } …",
             "    ╰ literal-1    ╯",
             "",
             "[ ON <u>SIZE</u> <u>ERROR</u> imperative-statement-1 ]"]
    batch = ["<u>ADD</u> ⎧identifier-1⎫ ...   <u>TO</u> {identifier-2 [<u>ROUNDED</u>]} ...",
             "    ⎩literal-1   ⎭",
             "",
             "    [ON <u>SIZE</u> <u>ERROR</u> imperative-statement-1]"]
    words = {w.text: w for w in read(house)}
    check("a house brace holds its alternatives", words["literal-1"].holders == ("brace",), str(words["literal-1"]))
    check("a one-row group nests", words["ROUNDED"].holders == ("brace", "bracket"), str(words["ROUNDED"]))
    check("an underline is the word's letters", words["ROUNDED"].underlined == ((0, 7),), str(words["ROUNDED"]))
    check("a word outside every group is held by none", words["ADD"].holders == (), str(words["ADD"]))
    batch_words = {w.text: w for w in read(batch)}
    check("the batch's two-row brace holds both alternatives",
          batch_words["identifier-1"].holders == ("brace",) and batch_words["literal-1"].holders == ("brace",),
          str(batch_words["identifier-1"]) + str(batch_words["literal-1"]))
    diff = compare(batch, [h.replace("…", "...") for h in house])
    check("the two drawings of ADD agree on grouping, bars and underlines",
          not diff["grouping"] and not diff["bars"] and not diff["underlines"], str(diff))
    wrong = [h.replace("<u>ROUNDED</u>", "ROUNDED").replace("…", "...") for h in house]
    diff = compare(batch, wrong)
    check("a lost underline is named", diff["underlines"] == ["ROUNDED: drawn [(0, 7)], generated []"], str(diff))
    bars = ["┌ │ <u>ALPHA</u> │ ┐", "│ │ BETA  │ │", "└ │ GAMMA │ ┘"]
    w = {x.text: x for x in read(bars)}
    check("choice bars inside a bracket", w["BETA"].holders == ("bracket", "bar"), str(w["BETA"]))
    no_bars = ["┌ <u>ALPHA</u> ┐", "│ BETA  │", "└ GAMMA ┘"]
    diff = compare(bars, no_bars)
    check("lost choice bars are named", len(diff["bars"]) == 3, str(diff))
    # Enclosures that hold no word and underlined delimiters are named too (PDF page 321's boxed typed brackets read as
    # bars and underlines; kb/Work PB4849).
    diff = compare(["<u>GO</u> TO [procedure-name-1]"], ["<u>GO</u> TO │ <u>[</u> │ procedure-name-1 │ <u>]</u> │"])
    check("bars holding no word and underlined delimiters are named",
          diff["bars"] == ["bar: drawn 0, generated 2"] and diff["underlines"] == ["underlined delimiters: drawn 0, generated 2"]
          and not diff["words"], str(diff))
    # An ellipsis repeats what precedes it (§2023:5.2.7): one moved after another group is an alignment difference
    # (PDF page 399's `{data-name-1} ...` regenerated a row lower, after the brace of RECORD IS / RECORDS ARE).
    drawn_ell = ["     ⎧<u>RECORD</u> IS  ⎫", "<u>DATA</u> ⎨           ⎬ {data-name-1} ...", "     ⎩<u>RECORDS</u> ARE⎭"]
    moved = ["     ╭ <u>RECORD</u> IS   ╮", "<u>DATA</u> ┤             ├ { data-name-1 }", "     │             │",
             "     ╰ <u>RECORDS</u> ARE ╯ …"]
    kept = ["     ╭ <u>RECORD</u> IS   ╮", "<u>DATA</u> ┤             ├ { data-name-1 } …", "     ╰ <u>RECORDS</u> ARE ╯"]
    check("an ellipsis moved after another group is named", len(compare(drawn_ell, moved)["alignment"]) == 1,
          str(compare(drawn_ell, moved)))
    check("an ellipsis after the same group agrees", not any(compare(drawn_ell, kept).values()), str(compare(drawn_ell, kept)))
    check("stacked alternatives stay stacked", not diff["alignment"], str(diff))
    flat = ["┌ <u>ALPHA</u> BETA ┐", "└ GAMMA      ┘"]
    diff = compare(no_bars, flat)
    check("an alternative set beside instead of below is an alignment difference",
          diff["alignment"] == ["ALPHA -> BETA: drawn stacked, generated beside"], str(diff))
    check("entities are one column", plain_row("&lt;<u>A</u>") == ("<A", {1}), str(plain_row("&lt;<u>A</u>")))
    # Level-2 boxes (FIGURE-STYLE.md rule 9): the house style's sides, one row and several, and the batch's rectangle.
    boxed = ["[ <u>SAME</u> ║ [ <u>RECORD</u> ] ║ AREA ]",
             "",
             "╔ <u>RERUN</u> ┌ ON ┐ ╗",
             "║          │    │ ║",
             "╚          └ IN ┘ ╝"]
    w = {x.text: x for x in read(boxed)}
    check("a one-row box holds its words and groups nothing", w["RECORD"].holders == ("bracket", "box", "bracket")
          and _grouping(w["RECORD"]) == ("bracket", "bracket") and w["AREA"].holders == ("bracket",), str(w["RECORD"]))
    check("a multi-row box holds every row between its corners", w["RERUN"].holders == ("box",)
          and w["IN"].holders == ("box", "bracket"), str(w["IN"]))
    rectangle = ["[ <u>SAME</u>  ╔════════════╗  AREA ]",
                 "          ║[ <u>RECORD</u> ]║",
                 "          ╚════════════╝",
                 "╔═══════════════════╗",
                 "║ <u>RERUN</u> ┌ ON ┐",
                 "║          │    │",
                 "║          └ IN ┘",
                 "╚═══════════════════╝"]
    w = {x.text: x for x in read(rectangle)}
    check("the batch's rectangle is read as a box", _boxes(w["RECORD"]) == 1 and _boxes(w["IN"]) == 1
          and _boxes(w["AREA"]) == 0, str(w["RECORD"]) + str(w["IN"]))
    unboxed = [re.sub(r" ?[║╔╚╗╝] ?", " ", b).strip() for b in boxed]
    diff = compare(unboxed, boxed)
    check("a lost box is named on every word it held", diff["boxes"] == ["RECORD: drawn 0, generated 1",
          "RERUN: drawn 0, generated 1", "ON: drawn 0, generated 1", "IN: drawn 0, generated 1"], str(diff))
    lone = ["identification-division", "", "│ [ procedure-division ]", "│", "│ [ end-program-header ]"]
    plain = ["identification-division", "", "[procedure-division]", "", "[end-program-header]"]
    check("a stroke that pairs with nothing is a grouping difference",
          compare(plain, lone)["grouping"] == ["generated: a bar in rows 2-4 pairs with nothing"], str(compare(plain, lone)))
    check("paired strokes leave no stray", strays(lone[:1] + ["│ x │"]) == [] and strays(plain) == [])
    across = ["[report-description-entry", "", "{report-group-description-entry} ... ] ..."]
    house_across = ["[ report-description-entry", "{ report-group-description-entry } … ] …"]
    check("a bracket set across two lines, unpaired in both drawings, is no difference",
          not compare(across, house_across)["grouping"] and len(strays(across)) == 2, str(compare(across, house_across)))
    check("a house-style ellipsis is read as printed", token_rows(["<u>ADD</u> x … TO"]) == [[("ADD", 0), ("x", 4),
          ("...", 6), ("TO", 8)]], str(token_rows(["<u>ADD</u> x … TO"])))
    check("the spaces round a typed delimiter stay as drawn", token_rows(["{a} ... ] ..."]) == [[("{a}", 0), ("...", 4),
          ("]", 8), ("...", 10)]], str(token_rows(["{a} ... ] ..."])))
    # restyle: the batch's drawing redrawn in the house style, reading the same (kb/Work PB4849).
    got = restyle(batch)
    check("restyle: a two-row brace gains its point row, the pieces become box drawing, the ellipsis is the house one",
          got == ["<u>ADD</u> ╭ identifier-1╮ …     <u>TO</u> {identifier-2 [<u>ROUNDED</u>]} …",
                  "    ┤             ├",
                  "    ╰ literal-1   ╯",
                  "",
                  "    [ ON <u>SIZE</u> <u>ERROR</u> imperative-statement-1]"], str(got))
    check("restyle: the redrawn figure reads as the drawing", not any(compare(batch, got).values()))
    check("restyle: no forbidden piece is left", foreign_glyphs(got) == "" and foreign_glyphs(batch) == "⎧⎩⎫⎭",
          foreign_glyphs(batch))
    pointless = ["⎧A⎫", "⎪B⎪", "⎩C⎭"]
    check("restyle: a brace drawn without its point gets one on its middle row",
          restyle(pointless) == ["╭ A ╮", "┤ B ├", "╰ C ╯"], str(restyle(pointless)))
    check("restyle: a two-row bracket gains a middle row, a bar beside it set one space clear",
          restyle(["┌│X│┐", "└│Y│┘"]) == ["┌ │X│ ┐", "│ │ │ │", "└ │Y│ ┘"], str(restyle(["┌│X│┐", "└│Y│┘"])))
    check("restyle: a corner on one row is the typed delimiter", restyle(["┌ A ┐"]) == ["[ A ]"], str(restyle(["┌ A ┐"])))
    crossed = ["⎧A⎫", "⎨B⎬", "⎩C⎭", "WORDWORD"]
    check("restyle: no blank column is set through another row's word",
          restyle(crossed) == ["╭A╮", "┤B├", "╰C╯", "WORDWORD"], str(restyle(crossed)))
    try:
        restyle(["┌A ┐", "└B┘"])              # the closer one column left on its second row (PDF page 253)
        refused = False
    except ValueError as exc:
        refused = "pairs with nothing" in str(exc)
    check("restyle: a drawing whose columns are off is refused", refused)
    try:
        restyle(rectangle)
        refused = False
    except ValueError:
        refused = True
    check("restyle: a box's full rectangle has no derived house form", refused)
    check("emit_row inverts plain_row", emit_row(*plain_row("&lt;<u>A</u>B<u>C</u>")) == "&lt;<u>A</u>B<u>C</u>",
          emit_row(*plain_row("&lt;<u>A</u>B<u>C</u>")))
    total = len(ran)
    print(f"figure_reading self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    for f in fails:
        print("  FAIL", f)
    return 1 if fails else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    ap.error("nothing to do: --self-test")
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
