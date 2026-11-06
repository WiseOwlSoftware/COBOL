#!/usr/bin/env python3
"""Measure the DELIMITER GEOMETRY of a SCANNED general format (the 1985 PDF) from its pixels (kb/Work PB4817).

WHY THIS EXISTS. `figure_geometry.py` and `figure_extract.py` read a general format's underlines, brackets and
choice-indicator bars from the PDF's VECTOR drawings. The 1985 PDF is a scan: a page has no vector drawing and about
33 images, so those two find nothing and the 2023 pipeline (measure -> read the words with coordinates ->
`render_figure.py` draws -> `sweep_figures.py --check` regenerates and diffs) cannot run on it. PB4812 states why the
drawing must come from measurement, not from a reading of the page: the 2023 formats were wrong until regenerated from
the measured page, and on the pilot (1985 PDF page 196) a model read every word and underline correctly but drifted the
columns of nested braces. The words come from the model; the geometry comes from the pixels.

WHAT IT RETURNS. A `RasterPage` that `classify_page(rp)` and `extract(rp, band)` read in EXACTLY the shapes
`figure_geometry.classify_page(page)` and `figure_extract.extract(page, band)` return, in PDF points, so
`render_figure.py` and the audits take them as their `classify_page` / `extract` arguments unchanged (they are passed
as callables). Which module measures an edition is PB4815's to decide (open: nothing dispatches to this one yet);
this module is the raster one and never a second pipeline.

    stem   {"x", "y0", "y1", "h", "kind": "bracket"|"bar", "side": "L"|"R"|None}   (as figure_geometry)
           side "L" is an opening delimiter ('[' whose feet turn right)
    brace  no stem (figure_geometry has none): extract() returns three Symbol glyph words per brace, the top hook,
           the point and the bottom hook (render_figure.GLYPH_PIECES), as a vector page's text layer does
    word   {"text", "x0", "y0", "x1", "underlined": True|False|[(offset, length), ...]}    (as figure_extract)

HOW IT MEASURES (each step is a function below):
  1. render the region in grayscale at >= 400 dpi, binarize (Otsu), and DESKEW: a scan is rotated by a fraction of a
     degree, which breaks a 40 pt rule into stair steps; the angle that makes the rows of ink sharpest is undone.
  2. label connected components. A STEM is a vertical run of ink >= MIN_VERT_H tall and <= MAX_VERT_W thick whose
     length is the height of its component; a stem shorter than 75 % of its component is a section of a curly shape.
  3. FEET: a bracket stem carries short horizontal runs anchored in its stroke on its END rows, turning inward; a
     choice-indicator bar has none. A foot stops at the next stem's stroke and belongs to the stem whose end rows it
     is on, never to the neighbour it touches (the PB3530 failure, folio 726, here at pixel scale); a run longer
     than FOOT_MAX is an underline.
  4. BRACES: a tall component with no full-height stem whose two halves are straight and aligned and whose middle
     protrudes by a point. A tall curved shape without that point (a parenthesis) is listed in `unclassified`.
  5. UNDERLINES: a component that is nothing but a thin horizontal bar MIN_RULE_W or longer.
  6. WORDS: the remaining components, grouped into lines and words by position. Their TEXT is never read from the
     image and never from the PDF's OCR layer (which has errors: the permission sentence reads "OI" for "or"); the
     caller passes the converted page's words per printed line (PB4816) and `align` pairs them with the boxes. A line
     whose box count differs from its word count is a DISAGREEMENT, reported in `RasterPage.disagreements`, and its
     words keep text None: the tool never picks silently.

    python scripts/spec/figure_raster.py --calibrate 196 197 --edition 1985 --out DIR
        writes one side-by-side image per page (the scan; the same scan with every measured mark drawn on it) for the
        by-eye check against a dozen pages that precedes any real use. Running it against the licensed PDF waits for
        the owner (PB4812); the self-test `test_figure_raster.py` runs on planted images only.
"""
from __future__ import annotations

import argparse
import math
import pathlib
import sys
from dataclasses import dataclass, field
from typing import NamedTuple

import numpy as np

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import figure_extract as FX  # noqa: E402  (the underline tests are one rule: reused, not copied)
import figure_geometry as FG  # noqa: E402  (the stem thresholds are one rule: reused, not copied)
import spec_pdf  # noqa: E402

DPI = 400                      # the measurement resolution; below this a 0.5 pt rule is under 3 px
MAX_SKEW_DEG = 1.5             # a scan is rotated by well under this; the search range
SKEW_STEP_DEG = 0.05
MIN_SKEW_FIX_DEG = 0.03        # a smaller estimate is noise, not rotation
STEM_OVERLAP = 0.5             # adjacent pixel columns belong to one stem when their runs overlap this much
STEM_FRACTION = 0.75           # a stem shorter than this share of its component is a section of a brace
BRACE_MAX_W = 12.0             # a brace is narrower than this, in points
BRACE_STRAIGHT = 0.6           # the halves of a brace are straight to within this many points
BRACE_ALIGNED = 0.8            # ... and aligned with each other to within this many points
BRACE_GLYPH_H = 13.0           # a Symbol brace piece is about this tall (render_figure.hook_row)
BRACE_GLYPHS = {"L": ("ì", "í", "î"), "R": ("ü", "ý", "þ")}   # render_figure.GLYPH_PIECES: top, point, bottom
BRACE_POINT = 1.5              # the middle protrudes at least this far beyond the halves
WORD_GAP_PT = 3.5              # a horizontal gap between components wider than this separates two words
MIN_COMPONENT_PX = 4           # smaller ink is speckle


class Rect(NamedTuple):
    """The edges `figure_extract.is_underlined` and `underline_runs` read from a rule, in points."""
    x0: float
    y0: float
    x1: float
    y1: float

    @property
    def width(self) -> float:
        return self.x1 - self.x0

    @property
    def height(self) -> float:
        return self.y1 - self.y0


class Component(NamedTuple):
    """One connected component of ink: its pixel box (x1, y1 exclusive) and its pixel count."""
    x0: int
    y0: int
    x1: int
    y1: int
    area: int


@dataclass
class RasterPage:
    """Everything measured from one page image, in PDF points (origin top left, as PyMuPDF)."""
    scale: float                                   # pixels per point
    stems: list = field(default_factory=list)      # brackets and bars only, as figure_geometry returns them
    braces: list = field(default_factory=list)     # {"x", "y0", "y1", "side", "point"}; extract() draws them as glyphs
    rules: list = field(default_factory=list)      # Rect: the underlines
    words: list = field(default_factory=list)      # extract()-shaped dicts, plus "y1"
    disagreements: list = field(default_factory=list)
    unclassified: list = field(default_factory=list)
    skew_deg: float = 0.0


# ---------------------------------------------------------------- the image

def render(edition: int, page: int, dpi: int = DPI) -> np.ndarray:
    """The page as a grayscale array (rows x columns, 0 black .. 255 white)."""
    import fitz
    doc = fitz.open(spec_pdf.require(edition))
    pix = doc[page - 1].get_pixmap(matrix=fitz.Matrix(dpi / 72, dpi / 72), colorspace=fitz.csGRAY)
    return np.frombuffer(pix.samples, dtype=np.uint8).reshape(pix.height, pix.width)


def binarize(gray: np.ndarray) -> np.ndarray:
    """Ink as True, by Otsu's threshold (the cut that best separates the paper's grays from the ink's)."""
    hist = np.bincount(gray.ravel(), minlength=256).astype(np.float64)
    total = hist.sum()
    w0 = np.cumsum(hist)
    m0 = np.cumsum(hist * np.arange(256))
    mt = m0[-1]
    w1 = total - w0
    with np.errstate(divide="ignore", invalid="ignore"):
        between = (mt * w0 - total * m0) ** 2 / (w0 * w1)
    cut = int(np.nanargmax(np.where(np.isfinite(between), between, -1.0)))
    return gray <= cut


def estimate_skew(ink: np.ndarray) -> float:
    """The rotation, in degrees, that makes the rows of ink sharpest (positive: a rule's right end is lower).

    A horizontal rule projected on the y axis is one tall spike when the page is level and a smear when it is
    rotated, so the angle that maximizes the sum of squared row counts is the page's skew."""
    ys, xs = np.nonzero(ink)
    if len(ys) == 0:
        return 0.0
    step = max(1, len(ys) // 400_000)
    ys, xs = ys[::step].astype(np.float64), xs[::step].astype(np.float64)
    best, best_score = 0.0, -1.0
    angles = np.round(np.arange(-MAX_SKEW_DEG, MAX_SKEW_DEG + SKEW_STEP_DEG / 2, SKEW_STEP_DEG), 4)
    for deg in sorted(angles, key=abs):                   # the smallest rotation wins a tie
        rows = np.round(ys - xs * math.tan(math.radians(deg))).astype(np.int64)
        rows -= rows.min()
        score = float(np.sum(np.bincount(rows).astype(np.float64) ** 2))
        if score > best_score:
            best, best_score = float(deg), score
    return best


def deskew(ink: np.ndarray, deg: float) -> np.ndarray:
    """Undo a skew of `deg` by shifting each column vertically (a shear), which keeps vertical strokes vertical and
    levels horizontal ones; for the fraction of a degree a scan is off it equals the rotation to a pixel."""
    if abs(deg) < MIN_SKEW_FIX_DEG:
        return ink
    h, w = ink.shape
    shift = np.round(np.arange(w) * math.tan(math.radians(deg))).astype(np.int64)
    out = np.zeros_like(ink)
    rows = np.arange(h)[:, None]
    src = rows + shift[None, :]                       # the output pixel (r, c) is the input pixel (r + shift[c], c)
    ok = (src >= 0) & (src < h)
    cols = np.broadcast_to(np.arange(w)[None, :], (h, w))
    out[ok] = ink[src[ok], cols[ok]]
    return out


def _runs(mask: np.ndarray):
    """The runs of True along axis 1: (row, start, end exclusive) arrays, in row order."""
    h, w = mask.shape
    pad = np.zeros((h, w + 2), dtype=np.int8)
    pad[:, 1:-1] = mask
    d = np.diff(pad, axis=1)
    r0, c0 = np.nonzero(d == 1)
    _, c1 = np.nonzero(d == -1)
    return r0, c0, c1


def _close(mask: np.ndarray, axis: int) -> np.ndarray:
    """Fill a one-pixel hole in a stroke along `axis` (a scan drops single pixels out of a thin line)."""
    m = mask.copy()
    if axis == 0:
        m[1:-1] |= mask[:-2] & mask[2:]
    else:
        m[:, 1:-1] |= mask[:, :-2] & mask[:, 2:]
    return m


def label(ink: np.ndarray):
    """Connected components (8-connected) as (labels, [Component]); label 0 is paper, label k is components[k-1]."""
    h, w = ink.shape
    rows, c0, c1 = _runs(ink)
    n = len(rows)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    # runs are in row order: link each run to the overlapping runs of the previous row
    row_start = np.searchsorted(rows, np.arange(h + 1))
    for r in range(1, h):
        a0, a1 = row_start[r - 1], row_start[r]
        b0, b1 = row_start[r], row_start[r + 1]
        if a0 == a1 or b0 == b1:
            continue
        i = a0
        for j in range(b0, b1):
            while i < a1 and c1[i] < c0[j]:               # previous run ends left of this one (diagonal counts)
                i += 1
            k = i
            while k < a1 and c0[k] <= c1[j]:
                ri, rj = find(k), find(j)
                if ri != rj:
                    parent[ri] = rj
                k += 1
    roots = np.array([find(i) for i in range(n)], dtype=np.int64)
    uniq, inv = np.unique(roots, return_inverse=True)
    labels = np.zeros((h, w), dtype=np.int32)
    comps = []
    area = np.zeros(len(uniq), dtype=np.int64)
    x0 = np.full(len(uniq), w, dtype=np.int64)
    x1 = np.zeros(len(uniq), dtype=np.int64)
    y0 = np.full(len(uniq), h, dtype=np.int64)
    y1 = np.zeros(len(uniq), dtype=np.int64)
    for i in range(n):
        k = inv[i]
        labels[rows[i], c0[i]:c1[i]] = k + 1
        area[k] += c1[i] - c0[i]
        x0[k] = min(x0[k], c0[i])
        x1[k] = max(x1[k], c1[i])
        y0[k] = min(y0[k], rows[i])
        y1[k] = max(y1[k], rows[i] + 1)
    for k in range(len(uniq)):
        comps.append(Component(int(x0[k]), int(y0[k]), int(x1[k]), int(y1[k]), int(area[k])))
    return labels, comps


# ---------------------------------------------------------------- stems, feet, braces

class _Stem(NamedTuple):
    px0: int          # pixel columns [px0, px1)
    px1: int
    py0: int          # pixel rows [py0, py1)
    py1: int
    comp: int         # label of the component it lies in


def find_stems(ink: np.ndarray, labels: np.ndarray, comps: list, scale: float) -> list:
    """Vertical strokes: runs >= MIN_VERT_H tall, merged across adjacent columns, <= MAX_VERT_W thick, as tall as
    their component (a shorter one is a section of a brace and is left to `find_braces`)."""
    cols, r0, r1 = _runs(_close(ink, 0).T)
    keep = (r1 - r0) >= FG.MIN_VERT_H * scale
    cols, r0, r1 = cols[keep], r0[keep], r1[keep]
    n = len(cols)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    by_col: dict = {}
    for i in range(n):
        by_col.setdefault(int(cols[i]), []).append(i)
    for i in range(n):
        for j in by_col.get(int(cols[i]) - 1, ()):
            overlap = min(r1[i], r1[j]) - max(r0[i], r0[j])
            if overlap >= STEM_OVERLAP * min(r1[i] - r0[i], r1[j] - r0[j]):
                parent[find(i)] = find(j)
    groups: dict = {}
    for i in range(n):
        groups.setdefault(find(i), []).append(i)
    out = []
    for members in groups.values():
        c = cols[members]
        if (c.max() - c.min() + 1) > FG.MAX_VERT_W * scale:
            continue                                      # too thick to be a stroke
        y0, y1 = int(np.median(r0[members])), int(np.median(r1[members]))
        x0, x1 = int(c.min()), int(c.max()) + 1
        lab = int(labels[(y0 + y1) // 2, (x0 + x1) // 2])
        if lab == 0:
            continue
        comp = comps[lab - 1]
        if (y1 - y0) < STEM_FRACTION * (comp.y1 - comp.y0):
            continue                                      # a section of a curly shape
        out.append(_Stem(x0, x1, y0, y1, lab))
    return sorted(out, key=lambda s: (s.py0, s.px0))


def _foot_side(stem: _Stem, stems: list, ink: np.ndarray, scale: float):
    """'L' when the stem's feet turn right (an opening bracket), 'R' when they turn left, None for a bare bar.

    A foot is a run of ink ANCHORED in this stem's own stroke on one of the rows that make up the stem's END (the
    stem's thickness deep: a foot is as thick as the stroke), FOOT_MIN..FOOT_MAX long, and it STOPS at another
    stem's stroke: a foot touching the next stem's edge is still this stem's foot, and that neighbour does not
    inherit it (kb/Work PB3530, folio 726, where each choice bar stands 0.70 pt from the bracket's foot and
    adopted it), because the foot's row is not on the neighbour's end rows. A run longer than FOOT_MAX is an
    underline."""
    h, w = ink.shape
    tol = max(2, stem.px1 - stem.px0) + 1
    lo, hi = FG.FOOT_MIN * scale, FG.FOOT_MAX * scale
    others = [s for s in stems if s is not stem]

    def blocked(x, r):
        return any(o.px0 <= x < o.px1 and o.py0 <= r < o.py1 for o in others)

    votes = []
    for ends in (range(stem.py0 - 1, stem.py0 + tol), range(stem.py1 - tol, stem.py1 + 1)):
        for r in ends:
            if not 0 <= r < h:
                continue
            for direction in (+1, -1):                 # +1: turns right (opening), -1: turns left (closing)
                anchor = stem.px1 - 1 if direction > 0 else stem.px0
                if not ink[r, anchor]:
                    continue
                x = anchor
                while 0 <= x + direction < w and ink[r, x + direction] and not blocked(x + direction, r):
                    x += direction
                if not lo <= abs(x - anchor) <= hi:
                    continue
                votes.append("L" if direction > 0 else "R")
    return max(set(votes), key=votes.count) if votes else None


def _classify_curly(comp: Component, ink: np.ndarray, scale: float):
    """('L' or 'R', y of the point in points) for '{' or '}', None when the component is not a brace."""
    sub = ink[comp.y0:comp.y1, comp.x0:comp.x1]
    h = sub.shape[0]
    has = sub.any(axis=1)
    if not has.all():
        return None
    left = np.argmax(sub, axis=1).astype(np.float64) / scale
    right = (sub.shape[1] - 1 - np.argmax(sub[:, ::-1], axis=1)).astype(np.float64) / scale

    def band(a, b):
        return slice(int(h * a), max(int(h * a) + 1, int(h * b)))

    top, bot, mid = band(0.2, 0.35), band(0.65, 0.8), band(0.45, 0.55)
    for edge, sign, side in ((left, -1, "L"), (right, +1, "R")):
        t, b, m = edge[top], edge[bot], edge[mid]
        if np.ptp(t) > BRACE_STRAIGHT or np.ptp(b) > BRACE_STRAIGHT:
            continue
        if abs(float(np.median(t)) - float(np.median(b))) > BRACE_ALIGNED:
            continue
        reach = (float(np.median(t)) - float(m.min())) if sign < 0 else (float(m.max()) - float(np.median(t)))
        if reach >= BRACE_POINT:
            at = band(0.45, 0.55).start + int(np.argmin(m) if sign < 0 else np.argmax(m))
            return side, (comp.y0 + at) / scale
    return None


def find_braces(ink: np.ndarray, comps: list, stem_labels: set, scale: float):
    """Braces among the components that hold no stem; returns (braces, unclassified)."""
    found, other = [], []
    for k, comp in enumerate(comps, start=1):
        height, width = (comp.y1 - comp.y0) / scale, (comp.x1 - comp.x0) / scale
        if k in stem_labels or height < FG.MIN_VERT_H or width > BRACE_MAX_W or comp.area < MIN_COMPONENT_PX:
            continue
        curly = _classify_curly(comp, ink, scale)
        box = {"x": round(comp.x0 / scale, 2), "y0": round(comp.y0 / scale, 2),
               "y1": round(comp.y1 / scale, 2), "h": round(height, 2)}
        if curly:
            found.append({"x": box["x"], "y0": box["y0"], "y1": box["y1"], "side": curly[0],
                          "point": round(curly[1], 2)})
        else:
            other.append(box)
    return found, other


# ---------------------------------------------------------------- rules and words

def find_rules(comps: list, stem_labels: set, scale: float) -> list:
    """Underlines: components that are nothing but a thin horizontal bar (a hyphen is shorter than MIN_RULE_W)."""
    out = []
    for k, c in enumerate(comps, start=1):
        if k in stem_labels:
            continue
        w, h = (c.x1 - c.x0) / scale, (c.y1 - c.y0) / scale
        if w >= FX.MIN_RULE_W and h <= FX.MAX_RULE_H and c.area >= 0.7 * (c.x1 - c.x0) * (c.y1 - c.y0):
            out.append((k, Rect(c.x0 / scale, c.y0 / scale, c.x1 / scale, c.y1 / scale)))
    return out


def find_words(comps: list, skip: set, scale: float) -> list:
    """Word boxes in lines: [[(x0, y0, x1, y1, [component boxes])...] per printed line] in points."""
    glyphs = [(k, c) for k, c in enumerate(comps, start=1) if k not in skip and c.area >= MIN_COMPONENT_PX]
    glyphs.sort(key=lambda kc: (kc[1].y0 + kc[1].y1))
    lines: list = []
    for _, c in glyphs:
        for line in lines:
            ly0, ly1 = min(g.y0 for g in line), max(g.y1 for g in line)
            if min(c.y1, ly1) - max(c.y0, ly0) >= 0.5 * min(c.y1 - c.y0, ly1 - ly0):
                line.append(c)
                break
        else:
            lines.append([c])
    out = []
    for line in sorted(lines, key=lambda ln: min(g.y0 for g in ln)):
        line.sort(key=lambda g: g.x0)
        words, cur = [], [line[0]]
        for g in line[1:]:
            if (g.x0 - max(c.x1 for c in cur)) / scale > WORD_GAP_PT:
                words.append(cur)
                cur = []
            cur.append(g)
        words.append(cur)
        out.append([(min(c.x0 for c in w) / scale, min(c.y0 for c in w) / scale,
                     max(c.x1 for c in w) / scale, max(c.y1 for c in w) / scale,
                     [(c.x0 / scale, c.y0 / scale, c.x1 / scale, c.y1 / scale) for c in w]) for w in words])
    return out


def align(boxes: list, tokens: list | None, rules: list):
    """Pair the measured word boxes with the converted page's words and read each word's underline state.

    `tokens` is one list of words per printed line (PB4816's), top to bottom. The state is `figure_extract`'s own
    (`is_underlined`, then `underline_runs` to trim, which can only remove characters). Returns (words,
    disagreements); a line whose box count and word count differ gets text None for every box and is reported."""
    words, bad = [], []
    for i, line in enumerate(boxes):
        want = tokens[i] if tokens is not None and i < len(tokens) else None
        if tokens is not None and (want is None or len(want) != len(line)):
            bad.append({"line": i, "boxes": len(line), "words": None if want is None else len(want),
                        "tokens": want})
            want = None
        for j, (x0, y0, x1, y1, chars) in enumerate(line):
            text = want[j] if want else None
            state = FX.is_underlined((x0, y0, x1, y1), rules)
            if state is True and text and len(text) > 1:
                own = [r for r in rules if FX.MIN_ABOVE <= r.y0 - y1 <= FX.MAX_GAP
                       and (min(x1, r.x1) - max(x0, r.x0)) / max(x1 - x0, 0.1) >= FX.MIN_COVER]
                if len(chars) == len(text):
                    state = FX.underline_runs(chars, own)
                else:   # components do not pair with letters (fused or split glyphs): the rule is kept whole, reported
                    bad.append({"line": i, "word": j, "text": text, "components": len(chars), "letters": len(text),
                                "reason": "underlined word's components do not match its letters"})
            words.append({"text": text, "x0": round(x0, 1), "y0": round(y0, 1), "x1": round(x1, 1),
                          "y1": round(y1, 1), "underlined": state})
    return words, bad


# ---------------------------------------------------------------- the entry points

def measure_image(gray: np.ndarray, dpi: int = DPI, tokens: list | None = None) -> RasterPage:
    """Measure a grayscale page image. `tokens` as for `align`."""
    scale = dpi / 72
    ink = binarize(gray)
    skew = estimate_skew(ink)
    ink = _close(_close(deskew(ink, skew), 0), 1)
    labels, comps = label(ink)
    stems = find_stems(ink, labels, comps, scale)
    stem_labels = {s.comp for s in stems}
    rp = RasterPage(scale=scale, skew_deg=skew)
    for s in stems:
        side = _foot_side(s, stems, ink, scale)
        rp.stems.append({"x": round(s.px0 / scale, 2), "y0": round(s.py0 / scale, 2), "y1": round(s.py1 / scale, 2),
                         "h": round((s.py1 - s.py0) / scale, 2),
                         "kind": "bracket" if side else "bar", "side": side})
    braces, rp.unclassified = find_braces(ink, comps, stem_labels, scale)
    rp.braces = braces
    rp.stems.sort(key=lambda s: (round(s["y0"], 1), s["x"]))
    rules = find_rules(comps, stem_labels, scale)
    rp.rules = [r for _, r in rules]
    skip = stem_labels | {k for k, _ in rules} | {
        k for k, c in enumerate(comps, start=1)
        if (c.y1 - c.y0) / scale >= FG.MIN_VERT_H and (c.x1 - c.x0) / scale <= BRACE_MAX_W
    }   # tall narrow shapes are delimiters, never words
    rp.words, rp.disagreements = align(find_words(comps, skip, scale), tokens, rp.rules)
    return rp


def measure_page(edition: int, page: int, tokens: list | None = None, dpi: int = DPI) -> RasterPage:
    """Measure one page of an edition's scanned PDF (1-based `page`)."""
    return measure_image(render(edition, page, dpi), dpi, tokens)


def classify_page(rp: RasterPage) -> list:
    """`figure_geometry.classify_page`'s result for a raster page: brackets and bars only (a brace is no stem
    there; `extract` reports it as the Symbol glyph pieces `render_figure.GLYPH_PIECES` decodes)."""
    return rp.stems


def brace_pieces(b: dict) -> list:
    """A brace as the three Symbol glyph pieces a vector PDF's text layer holds for it (top hook, point, bottom
    hook), so `render_figure` draws it as it draws a vector page's. Each glyph is BRACE_GLYPH_H tall: the top
    one starts at the brace's top, the bottom one ends at its bottom, the point one is centred on the point."""
    top, mid, bot = BRACE_GLYPHS[b["side"]]
    last = max(b["y1"] - BRACE_GLYPH_H, b["y0"])
    centre = min(max(b["point"] - BRACE_GLYPH_H / 2, b["y0"]), last)
    return [{"text": t, "x0": b["x"], "y0": round(y, 2), "x1": b["x"], "underlined": False}
            for t, y in ((top, b["y0"]), (mid, centre), (bot, last))]


def extract(rp: RasterPage, band=None) -> list:
    """`figure_extract.extract`'s result for a raster page (the same sort order and keys)."""
    out = [{k: w[k] for k in ("text", "x0", "y0", "x1", "underlined")} for w in rp.words
           if not band or band[0] <= w["y0"] <= band[1]]
    out += [piece for b in rp.braces for piece in brace_pieces(b) if not band or band[0] <= piece["y0"] <= band[1]]
    return sorted(out, key=lambda w: (round(w["y0"], 0), w["x0"]))


# ---------------------------------------------------------------- calibration

def overlay(gray: np.ndarray, rp: RasterPage) -> np.ndarray:
    """The page in RGB with every measured mark drawn on it: stems blue (brackets), red (bars), green (braces),
    underlines magenta, word boxes gray."""
    rgb = np.repeat(gray[:, :, None], 3, axis=2).copy()
    s = rp.scale
    colours = {"bracket": (0, 0, 255), "bar": (255, 0, 0), "brace": (0, 160, 0)}

    def box(x0, y0, x1, y1, col):
        x0, x1, y0, y1 = int(x0 * s) - 2, int(x1 * s) + 2, int(y0 * s) - 2, int(y1 * s) + 2
        h, w = rgb.shape[:2]
        x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, w - 1), min(y1, h - 1)
        rgb[y0:y1, [x0, x1]] = col
        rgb[[y0, y1], x0:x1] = col

    for st in rp.stems:
        box(st["x"] - 1, st["y0"], st["x"] + 1, st["y1"], colours[st["kind"]])
    for b in rp.braces:
        box(b["x"] - 1, b["y0"], b["x"] + 1, b["y1"], colours["brace"])
    for r in rp.rules:
        box(r.x0, r.y0, r.x1, r.y1, (255, 0, 255))
    for w in rp.words:
        box(w["x0"], w["y0"], w["x1"], w["y1"], (160, 160, 160))
    return rgb


def save_png(rgb: np.ndarray, path: pathlib.Path) -> None:
    import fitz
    h, w = rgb.shape[:2]
    fitz.Pixmap(fitz.csRGB, w, h, np.ascontiguousarray(rgb).tobytes(), False).save(str(path))


def calibrate(edition: int, pages: list, out: pathlib.Path) -> None:
    """Write `<page>.png` per page: the scan beside the scan with the measurement drawn on it, at half resolution."""
    out.mkdir(parents=True, exist_ok=True)
    for p in pages:
        gray = render(edition, p)
        rp = measure_image(gray)
        left = np.repeat(gray[:, :, None], 3, axis=2)
        side = np.concatenate([left, overlay(gray, rp)], axis=1)[::2, ::2]
        save_png(side, out / f"{p}.png")
        print(f"page {p}: skew {rp.skew_deg:+.2f} deg, {len(rp.stems)} delimiters, {len(rp.rules)} underlines, "
              f"{len(rp.words)} words, {len(rp.unclassified)} unclassified")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--calibrate", nargs="+", type=int, metavar="PAGE", required=True,
                    help="PDF pages to write side-by-side check images for")
    ap.add_argument("--edition", type=int, default=1985, choices=sorted(spec_pdf.EDITIONS))
    ap.add_argument("--out", type=pathlib.Path, required=True, help="directory for the images")
    args = ap.parse_args()
    calibrate(args.edition, args.calibrate, args.out)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
