#!/usr/bin/env python3
"""Self-test for figure_raster.py on PLANTED images (kb/Work PB4817): pages drawn here with known underlines, brackets,
choice bars and braces, then given a scan's skew and speckle, and measured. No PDF is opened: the licensed 1985 and
2002 PDFs are never read by a self-test (PB4812's standing requirement).
Run: python scripts/spec/test_figure_raster.py   (every gate and CI run it through scripts/self_tests.py.)

The drawn geometry is the measured shape of the 2023 vector figures (`test_figure_geometry.py`): 0.5 pt strokes,
3.5 pt feet, SEND's choice bars 0.70 pt clear of the bracket's feet (kb/Work PB3530).
"""
import json
import math
import pathlib
import struct
import sys
import zlib

import numpy as np

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import figure_geometry as G  # noqa: E402
import figure_raster as R  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

S = R.DPI / 72
W_PT, H_PT = 260, 150
STROKE = 0.5
FOOT = 3.5


def canvas():
    return np.full((int(H_PT * S), int(W_PT * S)), 255, dtype=np.uint8)


def fill(img, x0, y0, x1, y1):
    img[int(round(y0 * S)):max(int(round(y1 * S)), int(round(y0 * S)) + 1),
        int(round(x0 * S)):max(int(round(x1 * S)), int(round(x0 * S)) + 1)] = 0


def word(img, x, y, n):
    """A word of n letters, 4 x 7 pt blocks 1.2 pt apart, top at y; returns its right edge."""
    for i in range(n):
        fill(img, x + i * 5.2, y, x + i * 5.2 + 4, y + 7)
    return x + n * 5.2 - 1.2


def underline(img, x0, x1, y):
    fill(img, x0, y + 8.0, x1, y + 8.0 + STROKE)          # 1 pt under the word's bottom edge (y + 7)


def stem(img, x, y0, y1, feet=None):
    fill(img, x, y0, x + STROKE, y1)
    if feet == "L":                                       # opening bracket: feet turn right
        fill(img, x, y0, x + FOOT, y0 + STROKE)
        fill(img, x, y1 - STROKE, x + FOOT, y1)
    elif feet == "R":                                     # closing bracket: feet turn left
        fill(img, x + STROKE - FOOT, y0, x + STROKE, y0 + STROKE)
        fill(img, x + STROKE - FOOT, y1 - STROKE, x + STROKE, y1)


def line(img, xa, ya, xb, yb):
    n = int(max(abs(xb - xa), abs(yb - ya)) * S) + 1
    for t in np.linspace(0, 1, n):
        x, y = xa + (xb - xa) * t, ya + (yb - ya) * t
        fill(img, x, y, x + STROKE, y + STROKE)


def brace(img, x, y0, y1, side):
    """A curly brace: straight halves at x, hooks at the ends, a 3 pt point at the middle ('L' points left)."""
    d = -1 if side == "L" else 1
    mid = (y0 + y1) / 2
    fill(img, x, y0 + 5, x + STROKE, mid - 5)
    fill(img, x, mid + 5, x + STROKE, y1 - 5)
    line(img, x - d * 3, y0, x, y0 + 5)
    line(img, x, mid - 5, x + d * 3, mid)
    line(img, x + d * 3, mid, x, mid + 5)
    line(img, x, y1 - 5, x - d * 3, y1)


def curly(img, x, y0, y1, side):
    """A printed brace as page 217 shows it: no straight half. The edge bows in a smooth S, the ends hook away from
    the point, and a cusp of 2.2 pt marks the middle."""
    d = 1 if side == "L" else -1
    for y in np.arange(y0, y1, 0.1):
        t = (y - y0) / (y1 - y0)
        u = abs(2 * t - 1)
        off = 2.2 + 2.5 * u ** 6 + 0.6 * math.sin(6 * math.pi * t) - 2.2 * math.exp(-(u / 0.12) ** 2)
        fill(img, x + d * off, y, x + d * off + STROKE + 0.3, y + 0.2)


def paren(img, x, y0, y1):
    """A parenthesis: an arc with no straight half and no point."""
    for y in np.arange(y0, y1, 0.1):
        t = (y - y0) / (y1 - y0)
        fill(img, x + 3 * (2 * t - 1) ** 2, y, x + 3 * (2 * t - 1) ** 2 + STROKE, y + 0.2)


def scan(img, deg, seed):
    """Give the page a scan's flaws: the content sheared by `deg`, isolated specks, single pixels dropped from ink."""
    h, w = img.shape
    t = math.tan(math.radians(deg))
    out = np.full_like(img, 255)
    for c in range(w):
        s = int(round(c * t))
        lo, hi = max(0, s), min(h, h + s)
        out[lo:hi, c] = img[lo - s:hi - s, c]
    rng = np.random.RandomState(seed)
    ink_r, ink_c = np.nonzero(out == 0)
    drop = rng.choice(len(ink_r), size=len(ink_r) // 400, replace=False)
    out[ink_r[drop], ink_c[drop]] = 255
    out[rng.randint(0, h, 120), rng.randint(0, w, 120)] = 0          # specks of one pixel
    return out


def read_png(path):
    """The red channel of an 8-bit PNG (gray or RGB, not interlaced) as a uint8 array, read with zlib alone: CI's
    self-test jobs install numpy and nothing that decodes images."""
    data = pathlib.Path(path).read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", f"{path} is not a PNG"
    pos, idat, width = 8, b"", 0
    while pos < len(data):
        n, kind = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if kind == b"IHDR":
            width, height, depth, ctype, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert depth == 8 and ctype in (0, 2) and interlace == 0, f"{path}: only 8-bit gray or RGB is read"
            channels = 1 if ctype == 0 else 3
        elif kind == b"IDAT":
            idat += body
        pos += 12 + n
    raw = zlib.decompress(idat)
    stride = width * channels
    out = np.zeros((height, stride), dtype=np.uint8)
    for y in range(height):
        row = np.frombuffer(raw, dtype=np.uint8, count=stride, offset=y * (stride + 1) + 1).astype(np.int32)
        ft = raw[y * (stride + 1)]
        prev = out[y - 1].astype(np.int32) if y else np.zeros(stride, dtype=np.int32)
        if ft == 0:
            cur = row
        elif ft == 2:
            cur = row + prev
        else:                                   # Sub, Average and Paeth read the pixel to the left: a running loop
            cur = np.zeros(stride, dtype=np.int32)
            for i in range(stride):
                a = cur[i - channels] if i >= channels else 0
                b = prev[i]
                c = prev[i - channels] if i >= channels else 0
                if ft == 1:
                    pred = a
                elif ft == 3:
                    pred = (a + b) // 2
                else:
                    p = a + b - c
                    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                    pred = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                cur[i] = row[i] + pred
        out[y] = (cur & 255).astype(np.uint8)
    return np.ascontiguousarray(out.reshape(height, width, channels)[:, :, 0])


def near(a, b, tol=1.0):
    return abs(a - b) <= tol


def main() -> int:
    fails = []

    def check(name, ok, detail=""):
        if not ok:
            fails.append(f"{name} {detail}".strip())
        return ok

    total = 0

    # 1. A line of words with one underline; the words align to the converted page's tokens.
    img = canvas()
    xs = [10.0]
    xs.append(word(img, xs[0], 20, 5) + 8)
    xs.append(word(img, xs[1], 20, 4) + 8)
    word(img, xs[2], 20, 5)
    underline(img, xs[1], xs[1] + 4 * 5.2 - 1.2, 20)
    # a second line the converted page splits differently: reported, never picked
    word(img, 10, 40, 3)
    word(img, 40, 40, 3)
    rp = R.measure_image(scan(img, 0.3, 1), tokens=[["ALPHA", "BETA", "GAMMA"], ["ONE"]])
    total += 5
    check("scan skew estimated", near(rp.skew_deg, 0.3, 0.06), f"got {rp.skew_deg:+.2f}")
    check("one underline", len(rp.rules) == 1, f"got {len(rp.rules)}")
    if rp.rules:
        u = rp.rules[0]
        check("underline spans BETA", near(u.x0, xs[1]) and near(u.x1, xs[1] + 19.6) and near(u.y0, 28.0),
              f"got {tuple(round(v, 1) for v in u)}")
    first = [w for w in rp.words if near(w["y0"], 20)]
    check("words read left to right with the converted text",
          [w["text"] for w in first] == ["ALPHA", "BETA", "GAMMA"], f"got {[w['text'] for w in first]}")
    check("only BETA is underlined", [w["underlined"] for w in first] == [False, True, False],
          f"got {[w['underlined'] for w in first]}")
    total += 1
    check("a line of differing word count is reported, its words unread",
          len(rp.disagreements) == 1 and rp.disagreements[0]["reason"] == "no converted word pairs with this measured word"
          and all(w["text"] is None for w in rp.words if near(w["y0"], 40)), f"got {rp.disagreements}")

    # 2. SEND's ON EXCEPTION group: '[' two bars ']', each bar 0.70 pt clear of the bracket's foot.
    img = canvas()
    stem(img, 20, 60, 100, "L")
    stem(img, 20 + STROKE + FOOT - STROKE + 0.7, 63.5, 96.5)
    stem(img, 150, 63.5, 96.5)
    stem(img, 154.2, 60, 100, "R")
    rp = R.measure_image(scan(img, -0.1, 2))
    total += 2
    sig = G.signature(G.group_enclosures(R.classify_page(rp))[0])
    check("bracket, two choice bars, bracket", sig == "[ | | ]", f"got {sig!r}")
    check("measured heights", [near(s["h"], h, 1.2) for s, h in zip(sorted(R.classify_page(rp), key=lambda s: s["x"]), (40, 33, 33, 40))] == [True] * 4,
          f"got {[s['h'] for s in R.classify_page(rp)]}")

    # 3. A choice bar whose end touches the bracket's foot is still a bar (PB3530 at pixel scale).
    img = canvas()
    stem(img, 20, 60, 100, "L")
    stem(img, 20 + FOOT, 56, 104)
    stem(img, 150, 60, 100, "R")
    rp = R.measure_image(scan(img, 0.0, 3))
    total += 1
    sig = G.signature(G.group_enclosures(R.classify_page(rp))[0])
    check("a bar touching a foot", sig == "[ | ]", f"got {sig!r}")

    # 4. Braces: '{' and '}' with the stems of a bracket nested in the first; a parenthesis is not a brace.
    img = canvas()
    brace(img, 20, 20, 60, "L")
    stem(img, 40, 70, 100, "L")
    stem(img, 70, 70, 100, "R")
    brace(img, 130, 20, 60, "R")
    paren(img, 180, 20, 60)
    rp = R.measure_image(scan(img, 0.1, 4))
    total += 3
    got = [(s["kind"], s["side"]) for s in R.classify_page(rp)]
    check("brackets with their hands; a brace is no stem (figure_geometry has none)",
          sorted(got) == [("bracket", "L"), ("bracket", "R")], f"got {got}")
    check("the braces are measured with their points",
          sorted((b["side"], round(b["x"])) for b in rp.braces) == [("L", 17), ("R", 127)]
          and all(near(b["point"], 40, 1.5) for b in rp.braces), f"got {rp.braces}")
    check("a parenthesis is unclassified, not a brace", len(rp.unclassified) == 1, f"got {rp.unclassified}")

    # 5. The result feeds the pipeline's own callables in their shapes.
    total += 1
    ok = all(set(s) == {"x", "y0", "y1", "h", "kind", "side"} for s in R.classify_page(rp)) and all(
        set(w) == {"text", "x0", "y0", "x1", "underlined"} for w in R.extract(rp))
    check("classify_page and extract return figure_geometry's and figure_extract's keys", ok)

    # 6. A brace reaches render_figure as the Symbol glyph pieces a vector page holds: { ALFA / BETA } is a
    #    required choice (5.2.6.3), never the optional brackets of 5.2.6.2.
    import figure_extract as FE
    import render_figure as RF
    img = canvas()
    brace(img, 20, 20, 60, "L")
    word(img, 30, 22, 4)
    word(img, 30, 48, 4)
    brace(img, 60, 20, 60, "R")
    rp = R.measure_image(img, tokens=[["ALFA"], ["BETA"]])
    total += 3
    pieces = sorted(w["text"] for w in R.extract(rp) if w["text"] in FE.GLYPH_PIECES)
    check("each brace is three glyph pieces", pieces == sorted("ìíîüýþ"), f"got {pieces}")
    grid, _ = RF.build(rp, 0, 200, R.extract, R.classify_page)
    text = chr(10).join("".join(r).rstrip() for r in grid) if grid else ""
    check("the figure draws braces with their point", all(c in text for c in "╭╮┤├╰╯") and "┌" not in text,
          f"got{chr(10)}{text}")
    check("a brace is not drawn as a bracket", "[" not in text and "]" not in text, f"got{chr(10)}{text}")

    # 7. An underlined word whose components do not pair with its letters is reported, not guessed.
    img = canvas()
    word(img, 10, 20, 4)
    underline(img, 10, 10 + 4 * 5.2 - 1.2, 20)
    rp = R.measure_image(img, tokens=[["ABCDE"]])
    total += 1
    check("an underlined word with unpaired components is a disagreement",
          len(rp.disagreements) == 1 and rp.disagreements[0].get("components") == 4, f"got {rp.disagreements}")

    # 8. Planted cases for what the real pages taught (kb/Work PB4848).
    # 8a. An underline FUSED to a descender (a 'g' crosses the rule): the rule is found whole and the word keeps its
    #     letters, which a bar-only test could not do (the word and its rule were one component).
    img = canvas()
    word(img, 10, 20, 5)
    fill(img, 10 + 2 * 5.2, 20, 10 + 2 * 5.2 + 4, 20 + 9.3)                # letter 3 descends through the rule (7 pt + 2.3)
    underline(img, 10, 10 + 5 * 5.2 - 1.2, 20)
    rp = R.measure_image(img, tokens=[["ABCDE"]])
    total += 3
    check("a fused underline is found whole", len(rp.rules) == 1 and near(rp.rules[0].x0, 10, 1.0)
          and near(rp.rules[0].x1, 10 + 5 * 5.2 - 1.2, 1.0), f"got {[tuple(round(v, 1) for v in r) for r in rp.rules]}")
    check("the fused word keeps its five letters and reads underlined",
          not rp.disagreements and [w["underlined"] for w in rp.words] == [True], f"got {rp.disagreements} {[w['underlined'] for w in rp.words]}")
    check("the descender's tail is one letter with its body", len(rp.words) == 1 and rp.words[0]["text"] == "ABCDE")

    # 8b. A hyphen stands between letters at mid height; an underline lies below them.
    img = canvas()
    word(img, 10, 20, 2)
    fill(img, 10 + 2 * 5.2, 23.0, 10 + 2 * 5.2 + 4.7, 24.2)
    word(img, 10 + 2 * 5.2 + 4.7 + 1.0, 20, 2)
    rp = R.measure_image(img)
    total += 1
    check("a hyphen is not an underline", rp.rules == [], f"got {rp.rules}")

    # 8c. A ragged, tilted rule is a rule although its box is mostly empty (page 196: boxes filled 0.42).
    img = canvas()
    line(img, 10, 28, 120, 29.6)
    rp = R.measure_image(img)
    total += 1
    check("a tilted rule is found", len(rp.rules) == 1 and rp.rules[0].width > 100, f"got {rp.rules}")

    # 8d. One tall brace overstruck from two glyphs with a gap between them is one brace.
    img = canvas()
    curly(img, 20, 20, 90, "L")
    img[int(54.2 * S):int(55.8 * S), :int(30 * S)] = 255                    # the gap lies at the point, as on page 217
    rp = R.measure_image(img)
    total += 2
    check("a brace broken by a gap is one brace", len(rp.braces) == 1 and rp.braces[0]["side"] == "L"
          and near(rp.braces[0]["y1"] - rp.braces[0]["y0"], 70, 1.5), f"got {rp.braces} unclassified {rp.unclassified}")
    check("a tall brace is no bracket stem", R.classify_page(rp) == [], f"got {R.classify_page(rp)}")

    # 8e. A glyph touching a bracket's stem makes a bump, not a point: it stays a bracket.
    img = canvas()
    stem(img, 20, 20, 70, "L")
    fill(img, 20, 42, 24.5, 49)
    rp = R.measure_image(img)
    total += 1
    check("a bracket with a glyph fused to its stem is a bracket", [(s["kind"], s["side"]) for s in R.classify_page(rp)] == [("bracket", "L")]
          and rp.braces == [], f"got {R.classify_page(rp)} {rp.braces}")

    # 8f. A leaning closing bracket (page 196: 1.8 pt over 50) keeps its feet.
    img = canvas()
    line(img, 40, 20, 41.8, 70)
    fill(img, 36.5, 20, 40.5, 20.5)
    fill(img, 38.3, 69.5, 42.3, 70)
    rp = R.measure_image(img)
    total += 1
    check("a leaning bracket is a closing bracket", [(s["kind"], s["side"]) for s in R.classify_page(rp)] == [("bracket", "R")],
          f"got {R.classify_page(rp)}")

    # 8g. A BOX: four rules around words, one of them touched by a bracket's stem; the box and its words are found.
    img = canvas()
    fill(img, 20, 20, 100, 20.6)
    fill(img, 20, 40, 100, 40.6)
    fill(img, 20, 20, 20.6, 40.6)
    fill(img, 99.4, 20, 100, 40.6)
    word(img, 30, 26, 4)
    stem(img, 100.6, 22, 38, "L")
    rp = R.measure_image(img, tokens=[["ABCD"]])
    total += 2
    check("a box is found with the word it encloses", len(rp.boxes) == 1 and near(rp.boxes[0]["x0"], 20, 1.0)
          and near(rp.boxes[0]["x1"], 100, 1.0) and len(rp.boxes[0]["words"]) == 1, f"got {rp.boxes}")
    check("a box's rules are neither underlines nor choice bars", rp.rules == [] and all(s["kind"] != "bar" for s in R.classify_page(rp)),
          f"got {rp.rules} {R.classify_page(rp)}")
    total += 1
    sides = sorted((s["side"], round(s["x"]), round(s["y0"]), round(s["y1"])) for s in R.classify_page(rp) if s["kind"] == "box")
    check("classify_page gives a box as its two sides, the left opening and the right closing (kb/Work PB4849)",
          [(sd, x) for sd, x, _, _ in sides] == [("L", 20), ("R", 100)] and all(near(y0, 20, 1.0) and near(y1, 40.6, 1.0)
                                                                              for _, _, y0, y1 in sides), f"got {sides}")

    # 8h. A box that WRAPS A LINE (kb/Work PB4898): the boxes of two printed lines drawn as one outline, a polygon of two
    #     joined rectangles open where they meet (1985 PDF pages 389 and 684, first paragraph). Each line's rectangle is a
    #     box; every rule is erased, so none is read as an underline, and the words of each line are in its own box.
    img = canvas()
    fill(img, 20, 20, 200, 20.6)                                # line 1: top
    fill(img, 20, 20, 20.6, 34.6)                               # line 1: left side
    fill(img, 199.4, 20, 200, 34.6)                             # line 1: right side
    fill(img, 20, 34, 40, 34.6)                                 # line 1's bottom, left of line 2's box ...
    fill(img, 110, 34, 200, 34.6)                               # ... and right of it: between them the outline is open
    fill(img, 40, 34, 40.6, 48.6)                               # line 2: left side
    fill(img, 109.4, 34, 110, 48.6)                             # line 2: right side
    fill(img, 40, 48, 110, 48.6)                                # line 2: bottom
    word(img, 30, 25, 6)
    word(img, 50, 39, 4)
    rp = R.measure_image(img, tokens=[["ABCDEF"], ["GHIJ"]])
    total += 2
    got = [(round(b["x0"]), round(b["y0"]), round(b["x1"]), round(b["y1"]), len(b["words"])) for b in rp.boxes]
    check("a box that wraps a line is a box per line, each with its line's words",
          len(got) == 2 and all(near(a, b, 1.0) for a, b in zip(got[0], (20, 20, 200, 35, 1)))
          and all(near(a, b, 1.0) for a, b in zip(got[1], (40, 34, 110, 49, 1))), f"got {got}")
    check("a wrapped box's rules, its open seam included, are neither underlines nor choice bars",
          rp.rules == [] and all(s["kind"] != "bar" for s in R.classify_page(rp)), f"got {rp.rules} {R.classify_page(rp)}")

    # 8i. A drawn DIAGRAM (a table of ruled cells, PDF pages 297 and 298) is no level-2 box per cell: its cells share
    #     rules, several faces of one component, and it is judged whole as before (kb/Work PB4898).
    img = canvas()
    for y in (20, 40, 60):
        fill(img, 20, y, 200, y + 0.6)
    for x in (20, 110, 199.4):
        fill(img, x, 20, x + 0.6, 60.6)
    for x, y in ((30, 26), (120, 26), (30, 46), (120, 46)):
        word(img, x, y, 4)
    rp = R.measure_image(img)
    total += 1
    check("a ruled table's cells are not a box each", len(rp.boxes) <= 1, f"got {rp.boxes}")

    # 10. Reading the converted figure's words onto the scan (kb/Work PB4849).
    def dot(img, x, y):
        fill(img, x, y, x + 1.8, y + 1.6)

    def delim(img, x, y):                                       # a typed one-line `[`: 9 pt tall, 2.5 pt wide
        fill(img, x, y - 1.0, x + 2.5, y + 8.0)

    # 10a. An i's dot joins its letter; an ellipsis's three dots are one word; a typed delimiter is a word of its own
    #      although it is set hard against the word it encloses (`[ON`).
    img = canvas()
    for n in range(3):                                          # `iii`: stems with dots 0.6 pt above them
        fill(img, 10 + n * 5.2, 22, 10 + n * 5.2 + 2, 27)
        dot(img, 10 + n * 5.2, 19.8)
    for n in range(3):
        dot(img, 40 + n * 4.0, 25.4)
    delim(img, 70, 20)
    word(img, 73.7, 20, 2)
    rp = R.measure_image(img, tokens=[["iii", "...", "[ON"]])
    total += 4
    texts = [w["text"] for w in rp.words]
    check("an i's dot is part of its letter", any(len(b[4]) == 3 for b in rp.lines[0] if b[0] < 30), f"got {rp.lines}")
    check("an ellipsis is one word", "..." in texts and texts.count("...") == 1, f"got {texts}")
    check("a typed delimiter is a word of its own", texts == ["iii", "...", "[", "ON"], f"got {texts} {rp.disagreements}")
    check("every word is paired", not rp.disagreements, f"got {rp.disagreements}")

    # 10b. A brace's alternatives printed half a line above and below the word beside it, drawn on two rows: the
    #      pairing is over the figure, not row by row, so each word finds its box (ADD Format 1, page 305).
    img = canvas()
    word(img, 10, 26, 3)                                        # ADD, on the brace's point
    curly(img, 30, 18, 42, "L")
    word(img, 36, 20, 6)                                        # identifier-1, half a line up
    word(img, 36, 32, 4)                                        # literal-1, half a line down
    curly(img, 72, 18, 42, "R")
    word(img, 80, 26, 2)                                        # TO
    rp = R.measure_image(img, tokens=[[("ADD", 0), ("IDENTI", 4), ("TO", 12)], [("LITE", 4)]])
    total += 1
    check("words printed half a line apart pair with the converted rows",
          sorted(w["text"] for w in rp.words if w["text"]) == ["ADD", "IDENTI", "LITE", "TO"] and not rp.disagreements,
          f"got {[(w['text'], w['y0']) for w in rp.words]} {rp.disagreements}")

    # 10c. Two typed delimiters between the same two words are told apart by their ORDER only (`[ROUNDED]}`).
    img = canvas()
    delim(img, 10, 20)
    word(img, 13.7, 20, 7)
    delim(img, 13.7 + 7 * 5.2, 20)
    delim(img, 13.7 + 7 * 5.2 + 4.2, 20)
    word(img, 70, 20, 3)
    rp = R.measure_image(img, tokens=[["[ROUNDED]}", "..."[:0] + "ABC"]])
    total += 1
    check("a run of typed delimiters keeps its converted order",
          [w["text"] for w in rp.words] == ["[", "ROUNDED", "]", "}", "ABC"], f"got {[w['text'] for w in rp.words]} {rp.disagreements}")

    # 10d. A typed line that runs uphill stays one line, and the alternative printed below its start stays apart.
    img = canvas()
    for n in range(6):
        word(img, 10 + n * 30, 30 - n * 0.9, 4)                 # rises 4.5 pt across, as page 310's DISPLAY line
    word(img, 40, 36.5, 4)                                      # half a line below the second word
    rp = R.measure_image(img, skew=0.0)
    total += 1
    check("a sloping line is one line; the line below it is another",
          sorted(len(ln) for ln in rp.lines) == [1, 6], f"got {[len(ln) for ln in rp.lines]}")

    # 10f. The assignment is the cheapest one, rectangular either way round.
    total += 2
    square = np.array([[4.0, 1, 3], [2, 0, 5], [3, 2, 2]])
    check("the cheapest pairing of a square cost", R.hungarian(square) == [(0, 1), (1, 0), (2, 2)], f"got {R.hungarian(square)}")
    tall = np.array([[9.0, 1], [1, 9], [0, 0]])
    check("more rows than columns leaves a row out, at the least cost", R.hungarian(tall) == [(0, 1), (2, 0)], f"got {R.hungarian(tall)}")

    # 10g. The OCR layer cannot tell a delimiter from another of its side (page 305 reads `[ROUNDED]}` as `[ROUNDED])`):
    #     a hint naming `)` supports a typed `}` as fully as one naming `}`, and one naming only `{` does not.
    total += 2
    closer = (100.0, 10.0, 103.0, 19.0, [(100.0, 10.0, 103.0, 19.0)])
    frame = R._Frame([(0, 0, closer)], [10.0], [("}", 0, 0)], 1)
    said = lambda *h: {0: list(h)}
    check("an OCR `)` supports a typed `}`", frame.cost(0, 0, said("[ROUNDED])"), None) == frame.cost(0, 0, said("}"), None),
          f"got {frame.cost(0, 0, said('[ROUNDED])'), None)} and {frame.cost(0, 0, said('}'), None)}")
    check("an OCR `{` does not support a typed `}`",
          frame.cost(0, 0, said("{"), None) - frame.cost(0, 0, said("}"), None) == R.PAIR_W_HINT,
          f"got {frame.cost(0, 0, said('{'), None)}")
    # ... and an OCR misreading of no delimiter at all (page 182 reads a typed `}` as `1`) is no evidence of its side:
    #     it costs what a dropped one does (kb/Work PB4849).
    total += 1
    check("an OCR `1` neither supports nor contradicts a typed `}`",
          frame.cost(0, 0, said("1"), None) == frame.cost(0, 0, {0: []}, None) == frame.cost(0, 0, said("}"), None) + R.PAIR_W_HINT / 2,
          f"got {frame.cost(0, 0, said('1'), None)} {frame.cost(0, 0, {0: []}, None)}")

    # 10h. A typed delimiter that ends its printed line, the word drawn after it on its row printed half a line below
    #      (a brace's alternative: `[USAGE IS] {COMP ...`, PDF page 205): the missing neighbour contradicts nothing,
    #      and the `]` is read (kb/Work PB4849).
    img = canvas()
    delim(img, 10, 20)
    right = word(img, 13.7, 20, 5)                              # USAGE
    right = word(img, right + 6.0, 20, 2)                       # IS
    delim(img, right + 1.2, 20)
    word(img, right + 12.0, 26.0, 4)                            # COMP, half a line below
    word(img, right + 12.0, 38.0, 7)                            # DISPLAY, the next alternative
    rp = R.measure_image(img, skew=0.0, tokens=[["[USAGE", "IS]", "COMP"], ["DISPLAY"]], hints=[])  # the OCR read nothing
    total += 1
    check("a delimiter ending its line, the word after it on its row printed half a line below, is read",
          [w["text"] for w in rp.words] == ["[", "USAGE", "IS", "]", "COMP", "DISPLAY"] and not rp.disagreements,
          f"got {[w['text'] for w in rp.words]} {rp.disagreements}")

    # 10i. Two equal words of one row on one line are dealt out in order BEFORE the typed delimiters are placed by them
    #      (`[sentence] ... } ...`, PDF page 182: the two `...` came out crossed, and the `]` and `}` with them).
    img = canvas()
    delim(img, 10, 20)
    right = word(img, 13.7, 20, 8)                              # sentence
    delim(img, right + 1.2, 20)
    for x0 in (right + 8.0, right + 32.0):                      # two ellipses, a typed `}` between them
        for n in range(3):
            dot(img, x0 + n * 4.0, 25.4)
    delim(img, right + 24.0, 20)
    rp = R.measure_image(img, skew=0.0, tokens=[["[sentence]", "...", "}", "..."]], hints=[])
    total += 1
    check("equal words are dealt in order before the delimiters beside them are placed",
          [w["text"] for w in rp.words] == ["[", "sentence", "]", "...", "}", "..."] and not rp.disagreements,
          f"got {[w['text'] for w in rp.words]} {rp.disagreements}")
    # The assignment is free to cross two equal words (their costs tie whenever both lie on one side of both boxes),
    # so the dealing is checked on a crossed pairing itself.
    dots = [(x, 25.4, x + 9.8, 27.0, [(x + 4.0 * n, 25.4, x + 4.0 * n + 1.8, 27.0) for n in range(3)]) for x in (70.0, 94.0)]
    frame = R._Frame([(0, 0, dots[0]), (0, 1, dots[1])], [20.0, 20.0], [("...", 0, 11), ("...", 0, 17)], 1)
    dealt = R._deal(frame, {0: (1, 0.0), 1: (0, 0.0)}, None)
    total += 1
    check("a crossed pairing of equal words is dealt out in order", [dealt[0][0], dealt[1][0]] == [0, 1], f"got {dealt}")

    # 10j. A speck on a line of its own above a one-line format does not stretch the figure's frame (PDF page 321: a
    #      speck 4 pt above `GO TO [procedure-name-1]` priced every pair 2.0 more, and both brackets went unread).
    img = canvas()
    fill(img, 60, 15.0, 61.0, 15.4)                              # the speck: 1.0 x 0.4 pt
    right = word(img, 10, 20, 2)                                 # GO
    right = word(img, right + 6.0, 20, 2)                        # TO
    delim(img, right + 6.0, 20)
    right = word(img, right + 9.7, 20, 16)                       # procedure-name-1
    delim(img, right + 1.2, 20)
    rp = R.measure_image(img, skew=0.0, tokens=[["GO", "TO", "[procedure-name-1]"]])
    total += 1
    check("a speck on a line of its own leaves the frame and the pairing alone",
          [w["text"] for w in rp.words if w["text"]] == ["GO", "TO", "[", "procedure-name-1", "]"] and not rp.disagreements,
          f"got {[w['text'] for w in rp.words]} {rp.disagreements}")

    # 10k. An underlined word whose letters fused into fewer components than it has letters keeps its rule whole, except
    #      for a separator printed after (or before) the letters that the rule never reaches (PDF page 182: the rule
    #      under `DECLARATIVES.` stops short of the period; the printed page underlines the reserved word only).
    from types import SimpleNamespace as _Rule
    fused = [(10.0 + 7 * k, 20.0, 16.0 + 7 * k, 28.0) for k in range(10)] + [(80.0, 25.0, 82.0, 28.0)]  # 12 letters fused to 10, then "."
    for name, rules, text, want, noted in (
            ("the period the rule stops short of is not underlined",
             [_Rule(x0=10.5, x1=76.0, y0=29.0)], "DECLARATIVES.", [(0, 12)], False),
            ("a period the rule reaches keeps the rule whole (and the note)",
             [_Rule(x0=10.5, x1=82.0, y0=29.0)], "DECLARATIVES.", True, True),
            ("a word ending in no separator keeps the rule whole (and the note)",
             [_Rule(x0=10.5, x1=76.0, y0=29.0)], "DECLARATIVESX", True, True)):
        bad = []
        got = R._state((10.0, 20.0, 82.0, 28.0, fused), text, rules, {}, bad)
        total += 1
        check(f"10k: {name}", got == want and bool(bad) == noted, f"got {got} {bad}")
    # ... and a word whose components DO pair with its letters, the rule overrunning the last letter's foot into the
    #     period, past its centre but not under the whole glyph (PDF page 380's `I-O-CONTROL.`): the period is plain.
    cells = [(10.0 + 6 * k, 20.0, 15.4 + 6 * k, 28.0) for k in range(4)] + [(34.1, 25.0, 36.3, 28.0)]
    for name, x1, want in (("an overrun past the period's centre leaves it plain", 35.4, [(0, 4)]),
                           ("a rule struck under the whole period underlines it", 37.0, True)):
        bad = []
        got = R._state((10.0, 20.0, 36.3, 28.0, cells), "WORD.", [_Rule(x0=9.8, x1=x1, y0=29.0)], {}, bad)
        total += 1
        check(f"10k: {name}", got == want and not bad, f"got {got} {bad}")

    # 10e. Two figures that share words are placed TOGETHER, in order (ADD's formats share `[ON SIZE ERROR ...]`).
    def hint(y, *words_):
        return [(10 + 40 * k, y, 40 + 40 * k, y + 8, w) for k, w in enumerate(words_)]
    hints = (hint(100, "Format", "1:") + hint(120, "ADD", "identifier-l", "TO") + hint(140, "[ON", "SIZE", "ERROR")
             + hint(170, "Format", "2:") + hint(190, "ADD", "identifier-l", "GIVING") + hint(210, "[ON", "SIZE", "ERROR"))
    one = [["ADD", "identifier-1", "TO"], ["[ON", "SIZE", "ERROR"]]
    two = [["ADD", "identifier-1", "GIVING"], ["[ON", "SIZE", "ERROR"]]
    bands = R.locate([one, two], hints)
    total += 1
    check("two figures sharing words are placed one after the other, apart",
          bands[0] is not None and bands[1] is not None and bands[0][0] < 120 < 148 < bands[0][1] <= bands[1][0] < 190
          and bands[1][1] > 218, f"got {bands}")

    # 8j. Two bold brackets of consecutive lines stood one on another, the bottom foot of the upper one joined to the
    #     top foot of the lower one (kb/Work PB4897, PDF page 844), with paper at the stem's middle pixel where their
    #     joined feet meet: two brackets of each hand, never one tall bracket and never a brace.
    img = canvas()
    for x, hand in ((20, "L"), (60, "R")):
        for y0, y1 in ((20, 50), (50, 80)):
            fill(img, x, y0, x + 1.2, y1)
            fx = (x, x + 3.5) if hand == "L" else (x + 1.2 - 3.5, x + 1.2)
            fill(img, fx[0], y0, fx[1], y0 + 1.0)
            fill(img, fx[0], y1 - 1.0, fx[1], y1)
        px0, px1 = int(round(x * S)), int(round((x + 1.2) * S))
        mid, col = int(round(50 * S)), (px0 + px1) // 2
        img[mid - 2:mid + 2, col - 1:col + 2] = 255                       # the paper between the joined feet
    rp = R.measure_image(img)
    total += 2
    got = sorted((s["kind"], s["side"], round(s["h"])) for s in R.classify_page(rp))
    check("brackets joined foot to foot are two brackets of each hand",
          got == [("bracket", "L", 30), ("bracket", "L", 30), ("bracket", "R", 30), ("bracket", "R", 30)], f"got {got}")
    check("joined brackets are no brace", rp.braces == [] and rp.unclassified == [], f"got {rp.braces} {rp.unclassified}")

    # 8k. Two braces of consecutive lines joined hook to hook (kb/Work PB4897, PDF page 841's `{+|-}` pairs): two
    #     braces, each with its own point, never one brace pointing at the joint; a single brace as tall is one.
    img = canvas()
    curly(img, 20, 20, 50, "L")
    curly(img, 20, 50, 80, "L")
    curly(img, 60, 20, 50, "R")
    curly(img, 60, 50, 80, "R")
    curly(img, 100, 20, 80, "L")
    rp = R.measure_image(img)
    total += 2
    got = sorted((b["side"], round(b["x"] / 10), b["point"]) for b in rp.braces)
    joined = [g for g in got if g[1] < 9]
    check("braces joined hook to hook are two braces, each pointing at its own middle",
          [g[:2] for g in joined] == [("L", 2), ("L", 2), ("R", 6), ("R", 6)]
          and all(near(g[2], want, 1.5) for g, want in zip(joined, (35, 65, 35, 65))), f"got {got} {rp.unclassified}")
    tall = [g for g in got if g[1] >= 9]
    check("a single tall brace is not cut", len(tall) == 1 and tall[0][0] == "L" and near(tall[0][2], 50, 1.5), f"got {got}")

    # 8b. Underlined words of running text (kb/Work PB5041): hint boxes are 12 pt tall, the baseline 3.5 pt above a box's
    #     bottom. A rule under the word is found; a word with no rule, a word whose letters' feet cover 93 % of its width
    #     (bold Times), a short word whose rule stops at its own width and a rule above the window (the box's top) are not.
    img = canvas()
    fill(img, 20, 40, 60, 40.8)                                  # the rule, 0.8 pt thick, below the baseline (y 40)
    fill(img, 20, 30, 60, 38)                                    # a solid word body above it
    fill(img, 20, 62, 60, 70.5)                                  # no rule
    for i in range(10):                                          # feet at 93 %: a chain with a 0.3 pt gap per 4.3 pt
        fill(img, 20 + i * 4.3, 98, 24 + i * 4.3, 99.2)
    fill(img, 20, 110, 28, 110.8)                                # a rule under a word narrower than 14 pt, stopping at its width
    fill(img, 100, 30, 140, 30.8)                                # a rule at the box's top (y 29), 11 pt above its bottom
    fill(img, 100, 50, 140, 50.5)                                # a rule 3.6 pt above its box's bottom (y 53.6), the top row of the window `TEXT_RULE_BELOW` sets: a displaced OCR box
    hints = [(20, 29, 60, 41.5, "RULED"), (20, 62, 60, 74.0, "BARE"), (20, 88, 60, 100.5, "FEET"), (20, 100, 28, 112.5, "IN"),
             (100, 29, 140, 41.5, "TOPRULE"), (100, 41.5, 140, 53.6, "DISPLACED")]
    got = [h[4] for h in R.underlined_hints(img < 128, hints, S)]
    total += 1
    check("an underline below a word's baseline is found, feet, a lone short word and a rule above the window are not",
          got == ["RULED", "DISPLACED"], f"got {got}")

    #     A short word (under 14 pt, or one character: its feet cover its width whole) is underlined when the rule runs on
    #     across the space beside it; a monospaced word (an OCR box under 10 pt) is judged by its ragged rule (`MONO_RULE_COVER`).
    img = canvas()
    fill(img, 20, 24.5, 85, 25.3)                                # one rule under XX TO YY, spaces included
    fill(img, 20, 54.5, 82, 55.3)                                # one rule under QQ A RR
    fill(img, 110, 24.5, 117, 25.3)                              # the feet of IN, and nothing in the spaces beside it
    fill(img, 20, 106.2, 40, 107)                                # MONO: a ragged rule, a 3.5 pt break (91 %: above MONO_RULE_COVER, below TEXT_RULE_COVER)
    fill(img, 43.5, 106.2, 60, 107)
    fill(img, 20, 136.2, 25, 137)                                # MT: a full rule under a word narrower than MONO_RULE_MIN_W
    fill(img, 20, 126.2, 48, 127)                                # MONOHALF: a rule under 70 % of the word
    fill(img, 70, 136.2, 77, 137)                                # M: a whole rule under a one-glyph word 7 pt wide: the length guard
    hints = [(20, 13, 45, 26.0, "XX"), (49, 13, 56, 26.0, "TO"), (60, 13, 85, 26.0, "YY"),
             (20, 43, 45, 56.0, "QQ"), (49, 43, 53, 56.0, "A"), (57, 43, 82, 56.0, "RR"),
             (95, 13, 107, 26.0, "AA"), (110, 13, 117, 26.0, "IN"), (120, 13, 140, 26.0, "BB"),
             (20, 100, 60, 108.0, "MONO"), (20, 110, 60, 118.0, "MONOBARE"), (20, 120, 60, 128.0, "MONOHALF"),
             (20, 130, 25, 138.0, "MT"), (70, 130, 77, 138.0, "M")]
    got = [h[4] for h in R.underlined_hints(img < 128, hints, S)]
    total += 1
    check("a short word is underlined by the rule across its spaces and a monospaced word by its ragged rule",
          got == ["XX", "TO", "YY", "QQ", "A", "RR", "MONO"], f"got {got}")

    # 9. REAL 1985 pages (kb/Work PB4848, PB4897, PB4899): general-format regions cropped from the licensed scan, binarized,
    #    the stamp margins excluded, each counted BY EYE against the rendered page. The calibration table is
    #    figure_raster_pages/calibration.json; a crop is measured with its page's skew. PDF page 222's crop holds no
    #    box: its underline fused to `SEQUENCE`'s Q read as a box 1.3 pt tall, and erased, until `find_boxes` required
    #    the rectangle its rules enclose to be a box's size (kb/Work PB4849). The prose crops of pages 389 and 684 hold
    #    boxes that wrap a line, one outline around two lines' boxes: read as choice bars and no boxes (page 389) until
    #    `find_boxes` took each rectangle of a face for a box (kb/Work PB4898).
    pages = pathlib.Path(__file__).resolve().parent / "figure_raster_pages"
    cal = json.loads((pages / "calibration.json").read_text(encoding="utf-8"))
    for e in cal["pages"]:
        gray = read_png(pages / e["file"])
        rp = R.measure_image(gray, cal["dpi"], skew=e["skew_deg"])
        got = {"stems_L": sum(s["side"] == "L" for s in rp.stems), "stems_R": sum(s["side"] == "R" for s in rp.stems),
               "bars": sum(s["kind"] == "bar" for s in rp.stems), "braces_L": sum(b["side"] == "L" for b in rp.braces),
               "braces_R": sum(b["side"] == "R" for b in rp.braces), "boxes": len(rp.boxes)}
        if e["underlines"] is not None:
            got["underlines"] = sum(1 for r in rp.rules if e["region_pt"][0] <= r.y0 + e["y0_pt"] and r.y1 + e["y0_pt"] + 8 <= e["region_pt"][1])
        total += 1
        check(f"PDF page {e['page']}: every stroke the eye counts is measured", all(got[k] == e[k] for k in got) and not rp.unclassified,
              f"got {got} want {({k: e[k] for k in got})} unclassified {rp.unclassified}")

    # A rule is at least RULE_MIN_THICK thick on average: a 2.7 pt sliver 0.2 pt thick (1985 PDF page 905, under
    # `WHEN-COMPILED`) clears the vector arm's 2.5 pt width floor but is no underline; a 5 pt rule 0.8 pt thick is.
    scale = R.DPI / 72.0
    def bar(x0_pt, w_pt, thick_pt):
        x0, x1 = x0_pt * scale, (x0_pt + w_pt) * scale
        return R.Component(x0, 100.0, x1, 100.0 + 1.0 * scale, round(thick_pt * scale * (x1 - x0)))
    comps = [bar(10, 5.0, 0.8), bar(40, 2.7, 0.2)]
    rules = [k for k, _ in R.find_rules(comps, set(), scale)]
    total += 1
    check("a sliver of a rule's edge is no underline, a rule is", rules == [1], f"got rules {rules}")

    for f in fails:
        print(f"FAIL: {f}")
    print(f"figure_raster self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
