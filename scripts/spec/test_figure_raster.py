#!/usr/bin/env python3
"""Self-test for figure_raster.py on PLANTED images (kb/Work PB4817): pages drawn here with known underlines, brackets,
choice bars and braces, then given a scan's skew and speckle, and measured. No PDF is opened: the licensed 1985 and
2002 PDFs are never read by a self-test (PB4812's standing requirement).
Run: python scripts/spec/test_figure_raster.py   (every gate and CI run it through scripts/self_tests.py.)

The drawn geometry is the measured shape of the 2023 vector figures (`test_figure_geometry.py`): 0.5 pt strokes,
3.5 pt feet, SEND's choice bars 0.70 pt clear of the bracket's feet (kb/Work PB3530).
"""
import math
import pathlib
import sys

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
          len(rp.disagreements) == 1 and rp.disagreements[0]["boxes"] == 2
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

    # 9. REAL 1985 pages (kb/Work PB4848): twelve general-format regions cropped from the licensed scan, binarized,
    #    the stamp margins excluded, each counted BY EYE against the rendered page. The calibration table is
    #    figure_raster_pages/calibration.json; a crop is measured with its page's skew.
    import json
    import pymupdf
    pages = pathlib.Path(__file__).resolve().parent / "figure_raster_pages"
    cal = json.loads((pages / "calibration.json").read_text(encoding="utf-8"))
    for e in cal["pages"]:
        pix = pymupdf.Pixmap(str(pages / e["file"]))
        gray = np.frombuffer(pix.samples, dtype=np.uint8).reshape(pix.height, pix.width, pix.n)[:, :, 0].copy()
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
