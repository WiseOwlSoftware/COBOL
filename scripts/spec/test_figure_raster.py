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
    import render_figure as RF
    img = canvas()
    brace(img, 20, 20, 60, "L")
    word(img, 30, 22, 4)
    word(img, 30, 48, 4)
    brace(img, 60, 20, 60, "R")
    rp = R.measure_image(img, tokens=[["ALFA"], ["BETA"]])
    total += 3
    pieces = sorted(w["text"] for w in R.extract(rp) if w["text"] in RF.GLYPH_PIECES)
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

    # 9. REAL 1985 pages (kb/Work PB4848, PB4899): thirteen general-format regions cropped from the licensed scan, binarized,
    #    the stamp margins excluded, each counted BY EYE against the rendered page. The calibration table is
    #    figure_raster_pages/calibration.json; a crop is measured with its page's skew. PDF page 222's crop holds no
    #    box: its underline fused to `SEQUENCE`'s Q read as a box 1.3 pt tall, and erased, until `find_boxes` required
    #    the rectangle its rules enclose to be a box's size (kb/Work PB4849).
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

    for f in fails:
        print(f"FAIL: {f}")
    print(f"figure_raster self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
