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

HOW IT MEASURES (each step is a function below; every constant carries the PDF page that set it, kb/Work PB4848):
  1. render the region in grayscale at >= 400 dpi, binarize (Otsu), and DESKEW: a scan is rotated by a fraction of a
     degree, which breaks a 40 pt rule into stair steps; the angle that makes the rows of ink sharpest is undone. A
     CROP of a page is measured with the page's own angle (`skew=`): a crop holds too little ink to estimate it.
  2. label connected components. BOXES first (`find_boxes`): a component whose four sides are rules is a box of the
     high level, and its rules are erased so they are not read again as underlines or choice bars.
  3. UNDERLINES FUSED to their word's glyphs (`split_fused_rules`): a band of rows with a run longer than any glyph's
     stroke is a rule; it is erased and the letters are components again (a descender's tail rejoins its letter).
  4. STEMS: a vertical run of ink >= MIN_VERT_H tall and <= MAX_VERT_W thick whose length is the height of its
     component. FEET: a bracket stem carries short horizontal runs anchored in its stroke on its END rows, turning
     inward; a choice-indicator bar has none. A foot stops at the next stem's stroke and belongs to the stem whose end
     rows it is on (the PB3530 failure, folio 726, here at pixel scale). A stem must be STRAIGHT (`_straight`, a line
     that may lean): the long middle of a tall brace and its hooks otherwise read as a bracket.
  5. BRACES: a tall narrow component, or several stacked with a gap (the printer overstruck the glyphs), whose edge
     departs from its shoulders at the middle by a point (`_classify_curly`); the side is the hooks' direction. The
     halves are NOT straight. A tall curved shape without a point (a parenthesis) is listed in `unclassified`.
  6. UNDERLINES that are components of their own: a thin bar that is no hyphen (`find_rules`, `is_hyphen`).
  7. WORDS: the remaining components, grouped into lines and words by position. Their TEXT is never read from the
     image and never from the PDF's OCR layer (which has errors: the permission sentence reads "OI" for "or"); the
     caller passes the converted page's words per printed line (PB4816) and `align` pairs them with the boxes. A line
     whose box count differs from its word count is a DISAGREEMENT, reported in `RasterPage.disagreements`, and its
     words keep text None: the tool never picks silently. Each box lists the words it encloses.

WHAT IT WAS CALIBRATED ON. Twelve general-format regions of the 1985 scan (PDF pages 196, 217, 252, 389, 396, 426, 444,
493, 568, 684, 693, 845: the Environment, Nucleus, Sequential, Relative, Indexed, Sort-Merge and Communication modules,
Debug, and Amendment 1), each counted BY EYE against the rendered page: brackets by side, braces by side, boxes and
(on three pages) underlines. The crops, with the stamp margins excluded, and the counts are in `figure_raster_pages/`
(`calibration.json`); `test_figure_raster.py` measures every crop and asserts the counts. PB4848's table in kb/Work
lists the measured values and what is not yet right (Amendment 1's bold face, PDF pages 841 and 844). The measurer is
for the MONOSPACED general-format band: on proportional prose (the amendments' Times text) serif chains read as rules.

    python scripts/spec/figure_raster.py --calibrate 196 217 --edition 1985 --out DIR
        writes one side-by-side image per page (the scan; the same scan with every measured mark drawn on it: brackets
        blue, bars red, braces green, underlines magenta, boxes orange, words gray) for the by-eye check.
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
BRACE_SHOULDER = 0.15          # the shoulders of a brace's point lie this share of its height above and below it (page 196)
BRACE_MID = (0.4, 0.6)         # the point lies in this band of the brace's height
BRACKET_STRAIGHT = 1.1         # a bracket's stem is straight (or leaning evenly) to within this many points: 72 pt bracket on page 684 wanders 0.82, the 22 pt braces of 196 and 217 more than 1.7
BRACE_HOOK = 0.15              # the hooked ends bend this far from the halves, away from the point (a sign test: 0.15 read pages 217 and 844 best of 0.5, 0.3, 0.15)
BRACE_HOOK_ROWS = 0.08         # ... judged over this share of the height at each end
BRACE_SYMMETRY = 1.6           # the top and bottom halves of a brace mirror within this many points (median): 0.5 at 22 pt, 1.3 at the 57 pt braces of page 217
BRACE_GLYPH_H = 10.2           # a brace's last enclosed row begins this far above its bottom: median of 120 braces on PDF pages 196, 217, 252, 389, 396, 426 (p10 7.2, p90 11.9; kb/Work PB4848)
BRACE_GLYPHS = {"L": ("ì", "í", "î"), "R": ("ü", "ý", "þ")}   # render_figure.GLYPH_PIECES: top, point, bottom
BRACE_POINT = 1.0              # the middle departs this far from the shoulders: 2.5 at 22 pt (page 196), 1.0 to 1.8 at 26 pt and 33 pt (page 844, 196); a parenthesis stays under 0.5
WORD_GAP_PT = 3.5              # a horizontal gap between components wider than this separates two words
MIN_COMPONENT_PX = 4           # smaller ink is speckle
BRACE_STACK_DX = 1.5           # the pieces of one tall brace share a left edge to within this many points (page 217: 1.1) ...
BRACE_STACK_GAP = 4.0          # ... and are this far apart or less (the printer overstruck the glyphs; page 217: 1.8)
FUSED_MIN_RUN = 9.0            # an underline fused to its word's glyphs is a run longer than any one glyph's stroke: the 8 pt heading face of page 196 reaches 8.3, so 7 read its serifs as rules
RULE_MEAN_THICK = 1.5          # a printed underline is a thin ragged bar: its ink is at most this thick on average (area / width)
HYPHEN_MAX_W = 6.0             # a hyphen is a bar no wider than this (page 196: 4.5 to 5.0), standing between letters at mid height
HYPHEN_GAP = 1.5               # ... with a glyph this close beside it
BOX_MIN_W, BOX_MIN_H = 20.0, 9.0   # a box is at least this wide and tall, in points (page 389's smallest: 48 x 12.8)
BOX_TILT = 3.0                 # a box's rule is level to within this many points along its whole side (page 396's 430 pt box leans 1.5)
BOX_RULE = 2.0                 # ... and is at most this thick
BOX_SIDE = 0.6                 # ... and is inked along at least this share of the side (page 493's thin EXTEND box keeps 0.6 to 0.8; 0.9 lost it)
DESCENDER_MAX_H = 5.0          # a descender's tail below an underline is no taller than this


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
    boxes: list = field(default_factory=list)      # {"x0", "y0", "x1", "y1", "words": [indices into words]}
    disagreements: list = field(default_factory=list)
    unclassified: list = field(default_factory=list)
    skew_deg: float = 0.0


# ---------------------------------------------------------------- the image

def render(edition: int, page: int, dpi: int = DPI) -> np.ndarray:
    """The page as a grayscale array (rows x columns, 0 black .. 255 white)."""
    import pymupdf
    doc = pymupdf.open(spec_pdf.require(edition))
    pix = doc[page - 1].get_pixmap(matrix=pymupdf.Matrix(dpi / 72, dpi / 72), colorspace=pymupdf.csGRAY)
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
        y0, y1 = int(r0[members].min()), int(r1[members].max())   # a printed stem LEANS (page 196's closing bracket
        #                                                           drifts 1.8 pt over 50): its columns end at different rows
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
            stroke = np.nonzero(ink[r, stem.px0:stem.px1])[0]
            if len(stroke) == 0:
                continue                               # a leaning stem is not on this row yet
            for direction in (+1, -1):                 # +1: turns right (opening), -1: turns left (closing)
                anchor = stem.px0 + int(stroke[-1] if direction > 0 else stroke[0])   # the stroke's own edge on THIS row
                x = anchor
                while 0 <= x + direction < w and ink[r, x + direction] and not blocked(x + direction, r):
                    x += direction
                if not lo <= abs(x - anchor) <= hi:
                    continue
                votes.append("L" if direction > 0 else "R")
    return max(set(votes), key=votes.count) if votes else None


def _classify_curly(box: tuple, ink: np.ndarray, scale: float):
    """('L' or 'R', y of the point in points) for '{' or '}' in the pixel box (x0, y0, x1, y1), None for anything else.

    A brace is told from a parenthesis by its POINT: in the middle of the stroke an edge departs from its shoulders (the
    edge BRACE_SHOULDER of the height either side of it) by BRACE_POINT or more, in either direction, where a
    parenthesis's arc is still within a fraction of a point of its extreme. The point is a dip of the outer edge in most
    faces and a bulge of both edges in the bold one of PDF page 844's 80 pt brace, so the DIRECTION of the departure
    decides nothing; the SIDE is the hooks', a '{' bends its ends toward larger x than its halves and a '}' toward
    smaller (`BRACE_HOOK`, on the stroke's centre line; one pronounced end is enough, page 844's small brace hooks at
    its foot only). The two halves MIRROR each other (BRACE_SYMMETRY). They are NOT straight: a printed brace taller
    than two lines bows in a smooth S (page 196: 0.9 to 1.5 pt of wander in the quarter bands). A tall brace is
    overstruck from stacked glyphs with a gap between them (page 217): rows with no ink take the edge interpolated
    across the gap (kb/Work PB4848)."""
    x0, y0, x1, y1 = box
    sub = ink[y0:y1, x0:x1]
    n = sub.shape[0]
    has = sub.any(axis=1)
    if n < 8 or not has[0] or not has[-1]:
        return None
    left = np.argmax(sub, axis=1).astype(np.float64) / scale
    right = (sub.shape[1] - 1 - np.argmax(sub[:, ::-1], axis=1)).astype(np.float64) / scale
    if not has.all():
        rows = np.arange(n)
        left, right = np.interp(rows, rows[has], left[has]), np.interp(rows, rows[has], right[has])
    s = max(1, int(round(n * BRACE_SHOULDER)))
    lo, hi = int(n * BRACE_MID[0]), max(int(n * BRACE_MID[1]), int(n * BRACE_MID[0]) + 1)
    best = (0.0, 0)
    for edge in (left, right):
        for m in range(max(lo, s), min(hi, n - s)):
            dev = abs(edge[m] - (edge[m - s] + edge[m + s]) / 2)
            if dev > best[0]:
                best = (dev, m)
    if best[0] < BRACE_POINT:
        return None
    m = best[1]
    quarter = np.arange(int(n * 0.05), int(n * 0.45))
    if float(np.median(np.abs(left[quarter] - left[n - 1 - quarter]))) > BRACE_SYMMETRY:
        return None
    centre = (left + right) / 2
    k = max(1, int(round(n * BRACE_HOOK_ROWS)))
    up = centre[:k].mean() - np.median(centre[int(n * .15):int(n * .35)])
    down = centre[n - k:].mean() - np.median(centre[int(n * .65):int(n * .85)])
    bend = up if abs(up) >= abs(down) else down
    if abs(bend) < BRACE_HOOK:
        return None
    return ("L" if bend > 0 else "R"), (y0 + m) / scale


def _stack_pieces(pieces: list, scale: float) -> list:
    """Join the vertically stacked pieces of one tall brace: `pieces` are (label, Component); a piece joins the group
    above it when its left edge is within BRACE_STACK_DX of the group's and the gap is at most BRACE_STACK_GAP.
    Returns [(pixel box (x0, y0, x1, y1), [labels])]."""
    groups: list = []
    for k, c in sorted(pieces, key=lambda kc: kc[1].y0):
        for g in groups:
            if abs(c.x0 - g[0]) / scale <= BRACE_STACK_DX and 0 <= (c.y0 - g[3]) / scale <= BRACE_STACK_GAP:
                g[0], g[2], g[3] = min(g[0], c.x0), max(g[2], c.x1), c.y1
                g[4].append(k)
                break
        else:
            groups.append([c.x0, c.y0, c.x1, c.y1, [k]])
    return [((g[0], g[1], g[2], g[3]), g[4]) for g in groups]


def find_braces(ink: np.ndarray, comps: list, stem_labels: set, scale: float):
    """Braces among the tall narrow components that are no stem; returns (braces, unclassified). Components that stand
    one above another are one brace (`_stack_pieces`)."""
    found, other = [], []
    pieces = [(k, c) for k, c in enumerate(comps, start=1)
              if k not in stem_labels and (c.y1 - c.y0) / scale >= FG.MIN_VERT_H
              and (c.x1 - c.x0) / scale <= BRACE_MAX_W and c.area >= MIN_COMPONENT_PX]

    def take(box):
        curly = _classify_curly(box, ink, scale)
        x0, y0, x1, y1 = box
        shape = {"x": round(x0 / scale, 2), "y0": round(y0 / scale, 2), "y1": round(y1 / scale, 2)}
        if curly:
            found.append({**shape, "side": curly[0], "point": round(curly[1], 2)})
            return True
        other.append({**shape, "h": round((y1 - y0) / scale, 2)})
        return False

    # a piece that is a brace by itself is one (two small braces stood one above the other, page 844, are two);
    # only the pieces that are not are stacked and judged together
    rest = [(k, c) for k, c in pieces if not _classify_curly((c.x0, c.y0, c.x1, c.y1), ink, scale)]
    alone = {k for k, _ in pieces} - {k for k, _ in rest}
    for k, c in pieces:
        if k in alone:
            take((c.x0, c.y0, c.x1, c.y1))
    for box, _ in _stack_pieces(rest, scale):
        take(box)
    return found, other


def _straight(stem: _Stem, side: str | None, labels: np.ndarray, scale: float) -> bool:
    """Is the stem a STRAIGHT stroke (a bracket's stem or a choice bar), as against the long middle of a tall brace?
    A brace taller than about four lines has a straight middle longer than STEM_FRACTION of its height and hooks that
    read as feet, so neither tells it from a bracket; its edge wanders (PDF page 844: a five-line brace). The stroke's
    flat edge (left for feet turning right, right for feet turning left, both for a bar) fits a line, which may LEAN
    (page 196's closing bracket drifts 1.8 pt over 50), to within BRACKET_STRAIGHT over the stem's middle 80 %."""
    e = max(1, int(0.1 * (stem.py1 - stem.py0)))
    rows = slice(stem.py0 + e, stem.py1 - e)
    mine = labels[rows, max(0, stem.px0 - 12):stem.px1 + 12] == stem.comp
    if not mine.any(axis=1).all():
        return True
    y = np.arange(mine.shape[0], dtype=np.float64)
    edges = []
    if side != "R":
        edges.append(np.argmax(mine, axis=1))
    if side != "L":
        edges.append(mine.shape[1] - 1 - np.argmax(mine[:, ::-1], axis=1))
    for edge in edges:
        fit = np.polyval(np.polyfit(y, edge.astype(np.float64), 1), y)
        if np.ptp(edge - fit) / scale > BRACKET_STRAIGHT:
            return False
    return True


# ---------------------------------------------------------------- rules and words

def _side(mask: np.ndarray, scale: float):
    """Is the top of `mask` (a component's pixels) a RULE: a line, nearly level, one stroke thick, along BOX_SIDE of the
    width? Returns (first ink row per column, -1 where that column holds no rule; rule thickness in rows) or None.
    A box's rule is level to within BOX_TILT of its median row (a box's own tilt differs from the page's by a fraction
    of a degree: 1.5 pt over page 396's 430 pt LINAGE box) and is broken in places by the scan; the top of a block of
    text is not level (its ascenders vary by a letter's height)."""
    has = mask.any(axis=0)
    if not has.any():
        return None
    first = np.where(has, mask.argmax(axis=0), -1)
    med = float(np.median(first[has]))
    on_rule = has & (np.abs(first - med) <= BOX_TILT * scale)
    if on_rule.mean() < BOX_SIDE:
        return None
    runs = []
    for col in np.nonzero(on_rule)[0][::7]:                # a sample of columns is enough for the median
        column = mask[first[col]:, col]
        runs.append(int(column.argmin()) if not column.all() else len(column))
    thick = int(np.median(runs))
    return (np.where(on_rule, first, -1), thick) if thick <= BOX_RULE * scale else None


def find_boxes(ink: np.ndarray, labels: np.ndarray, comps: list, scale: float):
    """The BOXES of the standard's high level (kb/Work PB4848: "specifications unique to level 2 of the modules are
    enclosed in boxes", PDF page 23; page 147: "specifications unique to the high level are enclosed in boxes"; the
    amendments delete and move boxes). A box is one component of four joined rules (`_side` on each of its four
    sides). Its rules are often fused to what stands against them (a brace or bracket touching the right edge: pages
    396, 389), so only each rule's own rows or columns are erased, never the component's other ink. Returns (ink with
    the boxes' rules erased, [Rect]); the caller relabels, so the erased rules are neither read again as underlines
    nor their sides as choice bars, and what touched them is its own component again."""
    ink = ink.copy()
    found = []
    for k, c in enumerate(comps, start=1):
        if (c.x1 - c.x0) < BOX_MIN_W * scale or (c.y1 - c.y0) < BOX_MIN_H * scale:
            continue
        mine = labels[c.y0:c.y1, c.x0:c.x1] == k
        # The component's box may reach past the rules (a descender from the line above fused to the top rule, page 396):
        # find the top and bottom rules first and judge the left and right sides between them, then the reverse.
        top, bot = _side(mine, scale), _side(mine[::-1], scale)
        if top is None or bot is None:
            continue
        r0, r1 = int(np.percentile(top[0][top[0] >= 0], 1)), mine.shape[0] - int(np.percentile(bot[0][bot[0] >= 0], 1))
        band = mine[r0:r1]
        left, right = _side(band.T, scale), _side(band.T[::-1], scale)
        if left is None or right is None:
            continue
        c0, c1 = int(np.percentile(left[0][left[0] >= 0], 1)), band.shape[1] - int(np.percentile(right[0][right[0] >= 0], 1))
        box = band[:, c0:c1]
        sides = [_side(box, scale), _side(box[::-1], scale), _side(box.T, scale), _side(box.T[::-1], scale)]
        if any(x is None for x in sides):
            continue
        sub = ink[c.y0 + r0:c.y0 + r1, c.x0 + c0:c.x0 + c1]
        views = [sub, sub[::-1], sub.T, sub.T[::-1]]                 # the same four orientations, as views of `sub`
        for (first, thick), view in zip(sides, views):
            for col in np.nonzero(first >= 0)[0]:
                view[first[col]:first[col] + thick + 1, col] = False
        found.append(Rect((c.x0 + c0) / scale, (c.y0 + r0) / scale, (c.x0 + c1) / scale, (c.y0 + r1) / scale))
    return ink, found


def split_fused_rules(ink: np.ndarray, comps: list, scale: float):
    """Find the underlines FUSED to their word's glyphs (kb/Work PB4848): where the underline touches a letter (a
    descender: g, p, q, y; or a line that drifted) the word and its rule are ONE component, which `find_rules` cannot
    take for a bar. A fused rule is a band of rows, at most MAX_RULE_H thick, whose longest horizontal run is longer
    than any one glyph's stroke (FUSED_MIN_RUN). The band is erased and the ink relabelled so each letter is its own
    component again; a descender's tail below the band is joined back to the letter above it. Returns
    (ink without the bands, labels, components, [Rect]); labels and components are None when nothing was fused."""
    ink = ink.copy()
    rules = []
    bands = []
    for c in comps:
        if (c.y1 - c.y0) / scale <= FX.MAX_RULE_H or (c.x1 - c.x0) / scale < FUSED_MIN_RUN:
            continue
        sub = ink[c.y0:c.y1, c.x0:c.x1]
        rows, a, b = _runs(sub)
        long_ = (b - a) >= FUSED_MIN_RUN * scale
        if not long_.any():
            continue
        rows, a, b = rows[long_], a[long_], b[long_]
        start = 0
        for i in range(1, len(rows) + 1):
            if i == len(rows) or rows[i] - rows[i - 1] > 1:
                band = slice(start, i)
                start = i
                if (rows[band][-1] - rows[band][0] + 1) / scale > FX.MAX_RULE_H:
                    continue                      # too thick to be a rule: ink, not an underline
                spans = sorted(zip(a[band].tolist(), b[band].tolist()))
                merged = [list(spans[0])]
                for lo, hi in spans[1:]:
                    if lo <= merged[-1][1]:
                        merged[-1][1] = max(merged[-1][1], hi)
                    else:
                        merged.append([lo, hi])
                for lo, hi in merged:
                    r0, r1 = c.y0 + int(rows[band][0]), c.y0 + int(rows[band][-1]) + 1
                    ink[max(r0 - 1, 0):r1 + 1, c.x0 + lo:c.x0 + hi] = False     # one row more each side: a ragged rule's
                    #                                                         stair steps are not left as bars of their own
                    bands.append((c.x0 + lo, r0, c.x0 + hi, r1))
                    rules.append(Rect((c.x0 + lo) / scale, r0 / scale, (c.x0 + hi) / scale, r1 / scale))
    if not bands:
        return ink, None, None, []
    labels, comps = label(ink)
    comps = list(comps)
    for bx0, by0, bx1, by1 in bands:                    # a descender's tail joins the letter above it
        for k, t in enumerate(comps):
            if (t.y0 < by1 - 1 or t.y0 > by1 + 2 or t.x0 < bx0 - 1 or t.x1 > bx1 + 1
                    or (t.y1 - t.y0) / scale > DESCENDER_MAX_H or t.area == 0):
                continue
            best, best_overlap = None, 0
            for j, u in enumerate(comps):
                overlap = min(t.x1, u.x1) - max(t.x0, u.x0)
                if j != k and u.area and by0 - 2 <= u.y1 <= by0 + 2 and overlap > best_overlap:
                    best, best_overlap = j, overlap
            if best is not None:
                u = comps[best]
                labels[labels == k + 1] = best + 1
                comps[best] = Component(min(t.x0, u.x0), min(t.y0, u.y0), max(t.x1, u.x1), max(t.y1, u.y1), t.area + u.area)
                comps[k] = Component(0, 0, 0, 0, 0)
    return ink, labels, comps, rules


def is_hyphen(c: Component, comps: list, scale: float) -> bool:
    """A short bar standing at mid height beside a glyph: a hyphen, not an underline (an underline lies BELOW the
    glyphs it underlines, so no glyph's rows contain its middle)."""
    if (c.x1 - c.x0) / scale > HYPHEN_MAX_W:
        return False
    mid, reach = (c.y0 + c.y1) / 2, HYPHEN_GAP * scale
    return any(o is not c and o.area >= MIN_COMPONENT_PX and o.y0 < mid < o.y1
               and (0 <= c.x0 - o.x1 <= reach or 0 <= o.x0 - c.x1 <= reach) for o in comps)


def find_rules(comps: list, stem_labels: set, scale: float) -> list:
    """Underlines that are components of their own: a thin horizontal bar that is no hyphen. A printed rule is
    ragged and tilts a little, so its box is no measure of it (page 196: boxes filled 0.42 to 0.76); its MEAN
    thickness, area over width, is: 0.5 to 1.1 pt for a rule, 2.7 pt for a letter."""
    out = []
    for k, c in enumerate(comps, start=1):
        if k in stem_labels:
            continue
        w, h = (c.x1 - c.x0) / scale, (c.y1 - c.y0) / scale
        if (w >= FX.MIN_RULE_W and h <= FX.MAX_RULE_H and c.area <= RULE_MEAN_THICK * scale * (c.x1 - c.x0)
                and not is_hyphen(c, comps, scale)):
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

def measure_image(gray: np.ndarray, dpi: int = DPI, tokens: list | None = None, skew: float | None = None) -> RasterPage:
    """Measure a grayscale page image. `tokens` as for `align`. `skew` is the page's rotation in degrees when the image
    is only a CROP of it (the calibration fixtures): a crop holds too little ink to estimate the page's angle."""
    scale = dpi / 72
    ink = binarize(gray)
    skew = estimate_skew(ink) if skew is None else skew
    ink = _close(_close(deskew(ink, skew), 0), 1)
    labels, comps = label(ink)
    ink, boxes = find_boxes(ink, labels, comps, scale)
    if boxes:
        labels, comps = label(ink)
    ink, relabelled, rejoined, fused = split_fused_rules(ink, comps, scale)
    if relabelled is not None:
        labels, comps = relabelled, rejoined
    stems = find_stems(ink, labels, comps, scale)
    sides = {id(s): _foot_side(s, stems, ink, scale) for s in stems}
    stems = [s for s in stems if _straight(s, sides[id(s)], labels, scale)]
    stem_labels = {s.comp for s in stems}
    rp = RasterPage(scale=scale, skew_deg=skew)
    for s in stems:
        side = sides[id(s)]
        rp.stems.append({"x": round(s.px0 / scale, 2), "y0": round(s.py0 / scale, 2), "y1": round(s.py1 / scale, 2),
                         "h": round((s.py1 - s.py0) / scale, 2),
                         "kind": "bracket" if side else "bar", "side": side})
    braces, rp.unclassified = find_braces(ink, comps, stem_labels, scale)
    rp.braces = braces
    rp.stems.sort(key=lambda s: (round(s["y0"], 1), s["x"]))
    rules = find_rules(comps, stem_labels, scale)
    rp.rules = sorted(fused + [r for _, r in rules], key=lambda r: (r.y0, r.x0))
    skip = stem_labels | {k for k, _ in rules} | {
        k for k, c in enumerate(comps, start=1)
        if (c.y1 - c.y0) / scale >= FG.MIN_VERT_H and (c.x1 - c.x0) / scale <= BRACE_MAX_W
    }   # tall narrow shapes are delimiters, never words
    rp.words, rp.disagreements = align(find_words(comps, skip, scale), tokens, rp.rules)
    for b in sorted(boxes, key=lambda r: (r.y0, r.x0)):
        inside = [i for i, w in enumerate(rp.words)
                  if b.x0 < (w["x0"] + w["x1"]) / 2 < b.x1 and b.y0 < (w["y0"] + w["y1"]) / 2 < b.y1]
        rp.boxes.append({"x0": round(b.x0, 2), "y0": round(b.y0, 2), "x1": round(b.x1, 2), "y1": round(b.y1, 2),
                         "words": inside})
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
    for b in rp.boxes:
        box(b["x0"], b["y0"], b["x1"], b["y1"], (255, 140, 0))
    for w in rp.words:
        box(w["x0"], w["y0"], w["x1"], w["y1"], (160, 160, 160))
    return rgb


def save_png(rgb: np.ndarray, path: pathlib.Path) -> None:
    import pymupdf
    h, w = rgb.shape[:2]
    pymupdf.Pixmap(pymupdf.csRGB, w, h, np.ascontiguousarray(rgb).tobytes(), False).save(str(path))


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
