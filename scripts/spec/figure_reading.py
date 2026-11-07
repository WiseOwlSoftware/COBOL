#!/usr/bin/env python3
"""Read a DRAWN general format back into the four things it carries, and compare two drawings of one format.

WHY THIS EXISTS (kb/Work PB4849). The 1985 transcription's general formats were drawn by a model from the page image (the
batch of PB4816), and each is now also REGENERATED from the measured page (`figure_raster` reads the scan, `render_figure`
draws it). Two independent drawings of one printed format either agree or one of them is wrong, and the four things a
general format carries are what they must agree on (PB4812, FIGURE-STYLE.md):

    grouping     the brackets (§5.2.6.2: at most one of the options) and braces (§5.2.6.3: exactly one) that hold a word
    bars         the choice indicators (§5.2.6.4) that hold it
    underlines   which of its letters are underlined (§5.2.2: an underlined word is required)
    alignment    whether the next word is printed beside it or stacked below it (§5.2.6.1: stacked words are alternatives)

and, on a scan, a fifth: the BOXES that hold it (a 1985 box marks syntax unique to level 2 of a module; it groups and
chooses nothing, FIGURE-STYLE.md rule 9). Either drawing's box is read: the house style's double-line sides, and the
batch's full double-line rectangle.

The two drawings are TEXT: the generated one in the house style (box drawing, FIGURE-STYLE.md), the batch's in whatever
it chose (box drawing, the Miscellaneous Technical brace pieces, typed `[ ]`). `read` turns either into words, each with
its row, its column, its underlined letters and the delimiters around it; `compare` pairs the two word lists and names
every word on which they differ, and how. It reads, it never decides: a disagreement goes to a refuter who looks at the
printed page.

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
    the word they are set against, as the printed page sets them (`[ON`), and `figure_raster.split_token` splits them."""
    out = []
    for line in lines:
        text, _ = plain_row(line)
        text = "".join(" " if ch in COLUMN_GLYPHS or ch in RULE_GLYPHS or ch in OTHER_NOTATION else ch for ch in text)
        out.append([(m.group(0), m.start()) for m in re.finditer(r"\S+", text)])
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


def _pair(delims: list[tuple[str, str | None, int, int, int]]) -> list[Enclosure]:
    """Pair openers with closers: the nearest unpaired closer of the same family to the right whose rows overlap the
    opener's by half the shorter, innermost first. Bars pair left to right among themselves the same way."""
    out, used = [], set()
    openers = sorted((d for d in delims if d[1] == "L"), key=lambda d: -d[2])
    for fam, _, c, r0, r1 in openers:
        best = None
        for k, (f2, s2, c2, q0, q1) in enumerate(delims):
            if k in used or f2 != fam or s2 != "R" or c2 <= c:
                continue
            if min(r1, q1) - max(r0, q0) + 1 < 0.5 * min(r1 - r0 + 1, q1 - q0 + 1):
                continue
            if best is None or c2 < delims[best][2]:
                best = k
        if best is not None:
            used.add(best)
            out.append(Enclosure(fam, min(r0, delims[best][3]), max(r1, delims[best][4]), c, delims[best][2]))
    for family in ("bar", "box"):                 # a bar, and a box side drawn without a corner, have no hand
        bars = sorted((d for d in delims if d[0] == family and d[1] is None), key=lambda d: d[2])
        taken = set()
        for a in range(len(bars)):
            if a in taken:
                continue
            for b in range(a + 1, len(bars)):
                if b in taken:
                    continue
                _, _, c0, r0, r1 = bars[a]
                _, _, c1, q0, q1 = bars[b]
                if min(r1, q1) - max(r0, q0) + 1 >= 0.5 * min(r1 - r0 + 1, q1 - q0 + 1):
                    taken |= {a, b}
                    out.append(Enclosure(family, min(r0, q0), max(r1, q1), c0, c1))
                    break
    return out


def enclosures(lines: list[str]) -> list[Enclosure]:
    """Every delimiter pair a drawn figure draws: brackets, braces, parentheses, choice bars and boxes."""
    rows = [plain_row(line)[0] for line in lines]
    delims = _columns(rows)
    for r, text in enumerate(rows):
        for c, ch in enumerate(text):
            if ch in ONE_ROW:
                delims.append((ONE_ROW[ch][0], ONE_ROW[ch][1], c, r, r))
            elif ch == BAR:
                delims.append(("bar", None, c, r, r))
    return _pair(delims)


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
    pairs = [(x, partner[id(x)]) for x in a if id(x) in partner]
    for x, y in pairs:
        if _grouping(x) != _grouping(y):
            out["grouping"].append(f"{x.text}: drawn {'/'.join(_grouping(x)) or '-'}, generated {'/'.join(_grouping(y)) or '-'}")
        if _bars(x) != _bars(y):
            out["bars"].append(f"{x.text}: drawn {_bars(x)}, generated {_bars(y)}")
        if _boxes(x) != _boxes(y):
            out["boxes"].append(f"{x.text}: drawn {_boxes(x)}, generated {_boxes(y)}")
        if x.underlined != y.underlined:
            out["underlines"].append(f"{x.text}: drawn {list(x.underlined)}, generated {list(y.underlined)}")
    # ALIGNMENT is §5.2.6.1's: inside one group, words on one row are in sequence and words on different rows are
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


def self_test() -> int:
    fails = []

    def check(name, ok, detail=""):
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
    total = 17
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
