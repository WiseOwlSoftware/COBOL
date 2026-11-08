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
as callables). `figure_extract.page_extractor` and `figure_geometry.page_classifier` dispatch an edition to this module
by `spec_pdf.geometry` (PB4815, PB4849); it is the raster arm and never a second pipeline.

    stem   {"x", "y0", "y1", "h", "kind": "bracket"|"bar", "side": "L"|"R"|None}   (as figure_geometry)
           side "L" is an opening delimiter ('[' whose feet turn right)
    brace  no stem (figure_geometry has none): extract() returns three Symbol glyph words per brace, the top hook,
           the point and the bottom hook (figure_extract.GLYPH_PIECES), as a vector page's text layer does
    word   {"text", "x0", "y0", "x1", "underlined": True|False|[(offset, length), ...]}    (as figure_extract)

HOW IT MEASURES (each step is a function below; every constant carries the PDF page that set it, kb/Work PB4848):
  1. render the region in grayscale at >= 400 dpi, binarize (Otsu), and DESKEW: a scan is rotated by a fraction of a
     degree, which breaks a 40 pt rule into stair steps; the angle that makes the rows of ink sharpest is undone. A
     CROP of a page is measured with the page's own angle (`skew=`): a crop holds too little ink to estimate it.
  2. label connected components. BOXES first (`find_boxes`): a rectangle of four rules is a box of the high level, and
     its rules are erased so they are not read again as underlines or choice bars. The boxes of two printed lines may
     be one outline, a polygon open where they meet (a span that wraps a line, PB4898): each line's rectangle is a box.
  3. UNDERLINES FUSED to their word's glyphs (`split_fused_rules`): a band of rows with a run longer than any glyph's
     stroke is a rule; it is erased and the letters are components again (a descender's tail rejoins its letter).
  4. STEMS: a vertical run of ink >= MIN_VERT_H tall and <= MAX_VERT_W thick whose length is the height of its
     component. FEET: a bracket stem carries short horizontal runs anchored in its stroke on its END rows, turning
     inward; a choice-indicator bar has none. A foot stops at the next stem's stroke and belongs to the stem whose end
     rows it is on (the PB3530 failure, folio 726, here at pixel scale). Brackets of consecutive lines that touch are
     one stem whose joined feet stand INSIDE it; it is cut there (`split_stacked_stems`). A stem must be STRAIGHT
     (`_straight`, a line that may lean): the long middle of a tall brace and its hooks otherwise read as a bracket.
  5. BRACES: a tall narrow component, or several stacked with a gap (the printer overstruck the glyphs), whose edge
     departs from its shoulders at the middle by a point (`_classify_curly`); the side is the hooks' direction. The
     halves are NOT straight. Braces of consecutive lines joined hook to hook are cut at the joint (`_curlies`). A
     tall curved shape without a point (a parenthesis) is listed in `unclassified`.
  6. UNDERLINES that are components of their own: a thin bar that is no hyphen (`find_rules`, `is_hyphen`).
  7. WORDS: the remaining components, grouped into lines (by the letters nearest each glyph, so a line may run
     uphill) and words by position (`find_words`: an i's dot joins its letter, an ellipsis's dots stay one word, a typed
     one-line delimiter is a word of its own). Their TEXT is never read from the image and never from the PDF's OCR
     layer (which has errors: the permission sentence reads "OI" for "or"); the caller passes the converted figure's
     words (PB4816) and `align` pairs them with the boxes by ONE assignment over the figure (the converted rows are not
     the printed lines: a brace's alternatives are printed half a line from the words beside them). The OCR words are
     a POSITION hint only (`text_layer_hints`), and `locate` uses them to place each figure on its page (PB4849). A word
     no box pairs with, a box no word pairs with, a pair too unlike, and a line paired out of order are each a
     DISAGREEMENT in `RasterPage.disagreements`, and the word keeps text None: the tool never picks silently. Each box
     lists the words it encloses.

WHAT IT WAS CALIBRATED ON. Fifteen general-format regions of the 1985 scan (PDF pages 196, 217, 252, 389, 396, 426,
444, 493, 568, 602, 684, 693, and Amendment 1's 841, 844 and 845: the Environment, Nucleus, Sequential, Relative,
Indexed, Sort-Merge, Report Writer and Communication modules, Debug, and both faces of the amendment), each counted BY
EYE against the rendered page: brackets by side, braces by side, boxes and (on three pages) underlines; with them, the
boxed first paragraphs of pages 389 and 684, whose boxes wrap a line (PB4898), and page 222's underline fused to a
descender (PB4849). The crops, with the stamp margins excluded, and the counts are in `figure_raster_pages/`
(`calibration.json`); `test_figure_raster.py` measures every crop and asserts the counts. PB4848's and PB4897's notes
in kb/Work list the measured values and what is not yet right (on page 602 the eye counts 41 underlines and the
measurer 42, the extra one the fused foot serifs of "am" in "data-name-3", so that page asserts no underline count).
The measurer is for the MONOSPACED general-format band: on proportional prose (the amendments' Times text) serif
chains read as rules.

    python scripts/spec/figure_raster.py --calibrate 196 217 --edition 1985 --out DIR
        writes one side-by-side image per page (the scan; the same scan with every measured mark drawn on it: brackets
        blue, bars red, braces green, underlines magenta, boxes orange, words gray) for the by-eye check.
"""
from __future__ import annotations

import argparse
import bisect
import collections
import difflib
import math
import re
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
SKEW_BODY = (30.0, 760.0)      # a whole page's skew is measured between these heights, clear of the per-copy stamp's lines
STEM_OVERLAP = 0.5             # adjacent pixel columns belong to one stem when their runs overlap this much
STEM_FRACTION = 0.75           # a stem shorter than this share of its component is a section of a brace
BRACE_MAX_W = 12.0             # a brace is narrower than this, in points
BRACE_SHOULDER = 0.15          # the shoulders of a brace's point lie this share of its height above and below it (page 196)
BRACE_MID = (0.4, 0.6)         # the point lies in this band of the brace's height
STACK_JOINT_H = 3.0           # the fused feet of two brackets stood one on another are at most this thick: 2.0 to 2.3 on PDF pages 841 and 844 (kb/Work PB4897); a glyph fused to a stem is 5 or more
BRACKET_STRAIGHT = 1.1         # a bracket's stem is straight (or leaning evenly) to within this many points: 72 pt bracket on page 684 wanders 0.82, the 22 pt braces of 196 and 217 more than 1.7
BRACE_HOOK = 0.15              # the hooked ends bend this far from the halves, away from the point (a sign test: 0.15 read pages 217 and 844 best of 0.5, 0.3, 0.15)
BRACE_HOOK_ROWS = 0.08         # ... judged over this share of the height at each end
BRACE_SYMMETRY = 1.6           # the top and bottom halves of a brace mirror within this many points (median): 0.5 at 22 pt, 1.3 at the 57 pt braces of page 217
BRACE_JOINT = 0.6             # two braces joined hook to hook bend their centre line toward the hooks this far or more at the joint: 1.1 on PDF page 841; a single brace's middle 0.3 at most (kb/Work PB4897)
BRACE_GLYPH_H = 10.2           # a brace's last enclosed row begins this far above its bottom: median of 120 braces on PDF pages 196, 217, 252, 389, 396, 426 (p10 7.2, p90 11.9; kb/Work PB4848)
BRACE_GLYPHS = {"L": ("ì", "í", "î"), "R": ("ü", "ý", "þ")}   # figure_extract.GLYPH_PIECES: top, point, bottom
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
DOT_MAX = 2.5                  # a period, an i's dot or an ellipsis dot is no wider or taller than this (page 305: 1.4 to 2.0)
LINE_REACH_PT = 15.0           # a glyph is set on a line by that line's letters within this distance of it (`find_words`)
SLIVER_MAX = 0.6              # ink no taller than this is what an erased box rule leaves behind (page 305: 0.1 to 0.2)
DOT_ABOVE = 1.5               # ... and an i's dot stands this far or less above its stem (page 305: 0.6)
ELLIPSIS_GAP_PT = 5.0          # the dots of one ellipsis are this far apart or less (page 305: 3.9 to 4.1, one character)
DELIM_MIN_H = 7.6              # a typed one-line delimiter is at least this tall (page 305: 8.8 to 9.2, page 196's `{` 7.9;
                               # a capital is 7.0, a letter with a descender 7.2) ...
DELIM_MAX_W = 4.0              # ... and no wider than this (page 305: 2.5 to 3.4)
# Pairing the converted words with the measured boxes (`align`, `locate`; kb/Work PB4849). The weights make one glyph
# of length difference, a whole figure's height and a quarter of its width cost about the same; a right pair on PDF
# pages 196 and 305 costs under 1.3, a wrong one over 2.5.
PAIR_W_LEN, PAIR_W_ROW, PAIR_W_COL, PAIR_W_HINT = 1.0, 4.0, 1.0, 2.0
PAIR_ACCEPT = 2.5
PITCH_BEARING = 0.2            # a word's ink is its letters' cells less one glyph's side bearings, in pitches (page 305)
PAIR_W_NEIGHBOUR = 1.5         # a typed delimiter whose word beside it is not the converted word beside it
HINT_DY = 5.0                  # an OCR word overlaps a box when their middles are this close in height
HINT_LINE_DY = 3.0             # OCR words this close in height are one printed line
LOCATE_MATCH = 0.7             # an OCR word matches a converted word this alike (`_similarity`)
LOCATE_MIN_WORD = 3            # ... and an OCR word shorter than this is not counted (see `locate`)
LOCATE_PAD = 8.0              # a located figure reaches this far beyond its first and last printed lines (a brace's hooks)
OCR_FOLD = str.maketrans("l|io", "1110")       # the OCR layer's commonest confusions, after lower-casing (`file-name-l`)


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
    lines: list = field(default_factory=list)      # find_words(): the measured word boxes per printed line
    words: list = field(default_factory=list)      # extract()-shaped dicts, plus "y1", in `lines` order (`read`)
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
        # The component is the one that holds most of the stroke's pixels. Its middle pixel is no witness: two brackets
        # stood one on another meet there, and on PDF page 844 that pixel is paper between their joined feet, which
        # dropped the stem and left its ink to be read as a brace (kb/Work PB4897).
        held = labels[y0:y1, x0:x1]
        held = held[held > 0]
        if held.size == 0:
            continue
        lab = int(np.bincount(held).argmax())
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
    tol = max(2, stem.px1 - stem.px0) + 1
    others = [s for s in stems if s is not stem]
    votes = []
    for ends in (range(stem.py0 - 1, stem.py0 + tol), range(stem.py1 - tol, stem.py1 + 1)):
        for r in ends:
            for direction in (+1, -1):                 # +1: turns right (opening), -1: turns left (closing)
                if _is_foot(stem, r, direction, others, ink, scale):
                    votes.append("L" if direction > 0 else "R")
    return max(set(votes), key=votes.count) if votes else None


def _is_foot(stem: _Stem, r: int, direction: int, others: list, ink: np.ndarray, scale: float) -> bool:
    """Does a FOOT leave the stem's stroke on pixel row `r`, turning `direction` (+1 right, -1 left)? A foot is a run of
    ink anchored on the stroke's own edge on that row, FOOT_MIN..FOOT_MAX long, that stops at another stem's stroke."""
    h, w = ink.shape
    if not 0 <= r < h:
        return False
    stroke = np.nonzero(ink[r, stem.px0:stem.px1])[0]
    if len(stroke) == 0:
        return False                                   # a leaning stem is not on this row yet
    anchor = stem.px0 + int(stroke[-1] if direction > 0 else stroke[0])   # the stroke's own edge on THIS row
    x = anchor
    while (0 <= x + direction < w and ink[r, x + direction]
           and not any(o.px0 <= x + direction < o.px1 and o.py0 <= r < o.py1 for o in others)):
        x += direction
    return FG.FOOT_MIN * scale <= abs(x - anchor) <= FG.FOOT_MAX * scale


def split_stacked_stems(stems: list, ink: np.ndarray, scale: float) -> list:
    """Cut a stem that is two or more BRACKETS stood one on another, joined foot to foot (kb/Work PB4897). In a format
    whose alternatives each carry an optional phrase (PDF pages 841 and 844, the subscript format: `data-name-2
    [{+|-} integer-2]` over `index-name-1 [{+|-} integer-3]`), the brackets of consecutive lines touch: the bottom foot
    of the upper bracket and the top foot of the lower one fuse into a band, at most STACK_JOINT_H thick, that turns the
    same way as the stem's end feet, inside the stem (a bracket's stem has no foot between its ends). The stem is cut
    at the middle of each such band, and each piece must stand at least MIN_VERT_H tall and be a bracket of the same
    hand by itself. A bar (no feet) is never cut. Returns the stems, each piece a stem of its own."""
    out = []
    for stem in stems:
        side = _foot_side(stem, stems, ink, scale)
        if side is None:
            out.append(stem)
            continue
        direction = +1 if side == "L" else -1
        others = [s for s in stems if s is not stem]
        reach = int(FG.MIN_VERT_H * scale)
        rows = [r for r in range(stem.py0 + reach, stem.py1 - reach) if _is_foot(stem, r, direction, others, ink, scale)]
        cuts, start = [], 0
        for i in range(1, len(rows) + 1):            # a joint's rows may skip a row or two where the feet part
            if i == len(rows) or rows[i] - rows[i - 1] > STACK_JOINT_H * scale / 2:
                band = rows[start:i]
                start = i
                if band and (band[-1] - band[0] + 1) <= STACK_JOINT_H * scale:
                    cuts.append((band[0] + band[-1] + 1) // 2)
        pieces = [stem._replace(py0=a, py1=b) for a, b in zip([stem.py0] + cuts, cuts + [stem.py1])]
        if (len(pieces) > 1 and all(p.py1 - p.py0 >= reach for p in pieces)
                and all(_foot_side(p, [p] + others, ink, scale) == side for p in pieces)):
            out.extend(pieces)
        else:
            out.append(stem)
    return sorted(out, key=lambda s: (s.py0, s.px0))


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
    edges = _edges(box, ink, scale)
    if edges is None:
        return None
    left, right = edges
    n = len(left)
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


def _edges(box: tuple, ink: np.ndarray, scale: float):
    """(left, right): the stroke's edges per pixel row of the box, in points, or None when the box is too short or its
    first or last row holds no ink. Rows with no ink (the gap of an overstruck brace) take the edge interpolated."""
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
    return left, right


def _curlies(box: tuple, ink: np.ndarray, scale: float) -> list:
    """The braces in the pixel box (x0, y0, x1, y1), as [(side, y of the point in points, y0, y1)]: none, one, or
    several stood one on another and JOINED HOOK TO HOOK (kb/Work PB4897: PDF page 841's `{+|-}` of two consecutive
    lines are one component, whose joint departs from its shoulders like a point and read as ONE brace pointing at the
    joint). The joint is where the stroke's centre line bends TOWARD the hooks' side, as a brace's ends do, by
    BRACE_JOINT or more (1.1 pt at page 841's joints); a brace's point bends the other way, and the middle of page 841's
    80 pt five-line brace bends toward its hooks by 0.3 at most. A shape tall enough to hold two braces is cut at each such
    bend between MIN_VERT_H from either end, sharpest first; when both pieces hold braces of the hand the bend points to,
    they are the braces. Otherwise the shape is judged whole (`_classify_curly`)."""
    x0, y0, x1, y1 = box
    reach = int(FG.MIN_VERT_H * scale)
    edges = _edges(box, ink, scale) if y1 - y0 >= 2 * reach else None
    if edges is not None:
        centre = (edges[0] + edges[1]) / 2
        n = len(centre)
        s = max(1, int(round(n * BRACE_SHOULDER)))
        rows = np.arange(max(reach, s), min(n - reach, n - s))
        bend = centre[rows] - (centre[rows - s] + centre[rows + s]) / 2
        tried: list = []
        for i in np.argsort(-np.abs(bend), kind="stable"):
            m = int(rows[i])
            if abs(bend[i]) < BRACE_JOINT:
                break
            if any(abs(m - t) <= s for t in tried):
                continue
            tried.append(m)
            hand = "L" if bend[i] > 0 else "R"        # a '{' hooks toward larger x, a '}' toward smaller
            upper = _curlies((x0, y0, x1, y0 + m), ink, scale)
            lower = _curlies((x0, y0 + m, x1, y1), ink, scale)
            if upper and lower and {c[0] for c in upper + lower} == {hand}:
                return upper + lower
    one = _classify_curly(box, ink, scale)
    return [(one[0], one[1], y0, y1)] if one else []


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

    def take(box, curlies):
        x0, y0, x1, y1 = box
        for side, point, b0, b1 in curlies:
            found.append({"x": round(x0 / scale, 2), "y0": round(b0 / scale, 2), "y1": round(b1 / scale, 2),
                          "side": side, "point": round(point, 2)})
        if not curlies:
            other.append({"x": round(x0 / scale, 2), "y0": round(y0 / scale, 2), "y1": round(y1 / scale, 2),
                          "h": round((y1 - y0) / scale, 2)})

    # a piece that holds braces by itself is taken as it is (two small braces stood one above the other, page 844,
    # are two); only the pieces that hold none are stacked and judged together
    rest = []
    for k, c in pieces:
        box = (c.x0, c.y0, c.x1, c.y1)
        curlies = _curlies(box, ink, scale)
        if curlies:
            take(box, curlies)
        else:
            rest.append((k, c))
    for box, _ in _stack_pieces(rest, scale):
        take(box, _curlies(box, ink, scale))
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
    amendments delete and move boxes). A box is a rectangle of four rules (`_box`). A span that wraps a line is boxed
    once per printed line, and where the boxes of two lines touch the printer drew ONE outline: a polygon of two joined
    rectangles, open where they meet (PDF page 389's first paragraph: `and the` over `and the MULTIPLE`, `location ...
    reel.` over `FILE TAPE clause`; page 684's: `a message segment, ... a` over `or more`; kb/Work PB4898). So a
    component's boxes are found from the FACES its rules enclose, one box per rectangle of a face (`_faces`); a
    component that encloses no face of a box's size (a rule the scan broke), or several (a drawn diagram's cells), is
    judged whole. Its rules are often fused
    to what stands against them (a brace or bracket touching the right edge: pages 396, 389), so only each rule's own
    rows or columns are erased, never the component's other ink. Returns (ink with the boxes' rules erased, [Rect], one
    per rectangle); the caller relabels, so the erased rules are neither read again as underlines nor their sides as
    choice bars, and what touched them is its own component again."""
    ink = ink.copy()
    found = []
    for k, c in enumerate(comps, start=1):
        if (c.x1 - c.x0) < BOX_MIN_W * scale or (c.y1 - c.y0) < BOX_MIN_H * scale:
            continue
        mine = labels[c.y0:c.y1, c.x0:c.x1] == k
        whole = (0, mine.shape[0], 0, mine.shape[1], np.zeros_like(mine), np.zeros_like(mine))
        for y0, y1, x0, x1, joint, beyond in _faces(mine, scale) or [whole]:
            box = _box((mine[y0:y1, x0:x1] & ~beyond) | joint, scale)
            if box is None:
                continue
            r0, r1, c0, c1, sides = box
            oy, ox = c.y0 + y0 + r0, c.x0 + x0 + c0
            sub = ink[oy:oy + r1 - r0, ox:ox + c1 - c0]
            seam = joint[r0:r1, c0:c1]
            # The four orientations `_box` judged, as views of `sub`; where a side is the open seam of a polygon there is
            # no rule to erase, and the ink there (a descender) is kept.
            for (first, thick), view, open_ in zip(sides, [sub, sub[::-1], sub.T, sub.T[::-1]],
                                                   [seam, seam[::-1], seam.T, seam.T[::-1]]):
                for col in np.nonzero(first >= 0)[0]:
                    view[first[col]:first[col] + thick + 1, col] &= open_[first[col]:first[col] + thick + 1, col]
            found.append(Rect(ox / scale, oy / scale, (ox + c1 - c0) / scale, (oy + r1 - r0) / scale))
    return ink, found


def _faces(mine: np.ndarray, scale: float) -> list:
    """One window per RECTANGLE of each FACE a component's rules enclose that is a box's size (kb/Work PB4898), as
    (y0, y1, x0, x1, seam, beyond): the window's rows and columns in `mine`; the SEAM, a rule drawn where the printer drew
    none, across the columns this rectangle shares with the face's next rectangle above or below; and BEYOND, the ink
    past the seam (the next line's glyphs) that `_box` must not see; so `_box` judges each rectangle as the box it is,
    on (window & ~beyond) | seam. A face is the paper the component's ink cuts off from the paper around it; a printed
    line's rectangle is a run of the face's rows whose left and right ends stay within BOX_TILT (a polygon's ends jump
    by a column's width where its next line begins). Ink fused inside a box (a bracket whose feet touch its side:
    `[RECORD]` on PDF page 389) cuts small faces off the box's own; a small face belongs to the box-sized face whose rows
    hold it, so the rectangle still reaches the box's rules. Each window reaches a rule's thickness and tilt beyond its
    rectangle, so it holds the rules that enclose it. A component that encloses SEVERAL box-sized faces is no level-2
    box: it is a drawn diagram or table whose cells share rules (PDF pages 114, 295 to 298, 346 to 349: a run that took
    each cell for a box measured 18 and 21 "boxes" on pages 297 and 298), and it gets no windows, so `find_boxes`
    judges it whole as it judges a component with no face."""
    h, w = mine.shape
    paper = np.ones((h + 2, w + 2), dtype=bool)                  # a margin of paper joins everything outside the rules
    paper[1:-1, 1:-1] = ~mine
    # `label` joins pixels that touch at a corner, which is right for ink and wrong for paper: paper would cross a rule
    # where the scan steps it diagonally. Widening the ink by a pixel closes every such step.
    paper[1:-1, 1:-2] &= ~mine[:, 1:]
    paper[1:-1, 2:-1] &= ~mine[:, :-1]
    labels, holes = label(paper)
    labels = labels[1:-1, 1:-1]
    inner = [(k, f) for k, f in enumerate(holes, start=1)
             if f.x0 > 0 and f.y0 > 0 and f.x1 < w + 2 and f.y1 < h + 2]   # the one touching the margin is outside
    big = [(k, f) for k, f in inner if (f.x1 - f.x0) >= BOX_MIN_W * scale and (f.y1 - f.y0) >= BOX_MIN_H * scale]
    if len(big) != 1:
        return []           # no face: judged whole (a rule the scan broke); several: a drawn diagram's cells, judged whole
    pad = int(math.ceil((BOX_RULE + BOX_TILT) * scale))
    tilt = BOX_TILT * scale
    line = max(1, int(BOX_MIN_H * scale / 2))                   # a jump of the ends shorter than this is a glyph, not a line
    thick = max(1, int(BOX_RULE * scale / 2))
    windows = []
    for k, f in big:
        face = labels == k
        for j, g in inner:
            if (j, g) not in big and g.y0 >= f.y0 - 1 and g.y1 <= f.y1 + 1 and g.x0 >= f.x0 - pad and g.x1 <= f.x1 + pad:
                face |= labels == j
        rows = np.nonzero(face.any(axis=1))[0]
        lo = face[rows].argmax(axis=1)
        hi = w - face[rows][:, ::-1].argmax(axis=1)
        # The ends per row, smoothed over a line's half height so a glyph fused to a side (a bracket's foot) is no jump.
        span = 2 * (line // 2) + 1
        lo, hi = (np.median(np.lib.stride_tricks.sliding_window_view(np.pad(e, line // 2, mode="edge"), span), axis=1)
                  for e in (lo, hi))
        rects = []                                               # [first row, last row + 1, left, right]
        for r, a, b in zip(rows, lo, hi):
            if rects and abs(a - rects[-1][2]) <= tilt and abs(b - rects[-1][3]) <= tilt and r == rects[-1][1]:
                rects[-1][1] = int(r) + 1
            else:
                rects.append([int(r), int(r) + 1, int(a), int(b)])
        # A run shorter than half a line is no printed line: the first or last rows of a face under a rule the scan
        # tilted, which reach only part of its width. It joins the line beside it, and no seam is drawn between them.
        lines = []
        for rect in rects:
            if lines and rect[1] - rect[0] < line:
                lines[-1][1] = rect[1]
            elif lines and lines[-1][1] - lines[-1][0] < line:
                lines[-1] = [lines[-1][0], *rect[1:]]
            else:
                lines.append(rect)
        rects = lines
        for i, (r0, r1, a, b) in enumerate(rects):
            y0, y1, x0, x1 = max(0, r0 - pad), min(h, r1 + pad), max(0, a - pad), min(w, b + pad)
            seam = np.zeros((y1 - y0, x1 - x0), dtype=bool)
            beyond = np.zeros_like(seam)
            for n, edge, toward in ((i - 1, r0 - y0, -1), (i + 1, r1 - y0, 1)):
                if 0 <= n < len(rects):
                    s0, s1 = max(a, rects[n][2]) - x0, min(b, rects[n][3]) - x0
                    if s1 > s0:
                        # the seam: a rule just outside this rectangle, and nothing beyond it (the next line's glyphs)
                        seam[max(0, edge - thick):edge if toward < 0 else edge + thick, s0:s1] = True
                        beyond[(slice(0, max(0, edge - thick)) if toward < 0 else slice(edge + thick, None)), s0:s1] = True
            windows.append((y0, y1, x0, x1, seam, beyond))
    return windows


def _box(mine: np.ndarray, scale: float):
    """Is `mine` (a component's pixels, or a window of them around one face) a BOX: four rules (`_side`) that enclose a
    rectangle of a box's size? Returns (r0, r1, c0, c1, [top, bottom, left, right] as `_side` gives them over the box
    rectangle mine[r0:r1, c0:c1]) or None."""
    # The window may reach past the rules (a descender from the line above fused to the top rule, page 396): find the
    # top and bottom rules first and judge the left and right sides between them, then the reverse.
    top, bot = _side(mine, scale), _side(mine[::-1], scale)
    if top is None or bot is None:
        return None
    r0, r1 = int(np.percentile(top[0][top[0] >= 0], 1)), mine.shape[0] - int(np.percentile(bot[0][bot[0] >= 0], 1))
    band = mine[r0:r1]
    left, right = _side(band.T, scale), _side(band.T[::-1], scale)
    if left is None or right is None:
        return None
    c0, c1 = int(np.percentile(left[0][left[0] >= 0], 1)), band.shape[1] - int(np.percentile(right[0][right[0] >= 0], 1))
    # The rules found may enclose far less than the window: one side of a drawn diagram's frame read as "two sides"
    # 1.4 pt apart (PDF page 353), an underline fused to its word's descender as "a top and a bottom" 1.3 pt apart
    # (page 222; its crop is a calibration case), and the false box's "rules" were erased. A box is the rectangle its
    # rules enclose, so that rectangle must itself be a box's size (kb/Work PB4849).
    if (c1 - c0) < BOX_MIN_W * scale or (r1 - r0) < BOX_MIN_H * scale:
        return None
    box = band[:, c0:c1]
    sides = [_side(box, scale), _side(box[::-1], scale), _side(box.T, scale), _side(box.T[::-1], scale)]
    return None if any(x is None for x in sides) else (r0, r1, c0, c1, sides)


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


def _is_dot(c: Component, scale: float) -> bool:
    """A speck of ink no bigger than a period: an i's dot, a period, one dot of an ellipsis (page 305: 1.4 to 2.0 pt)."""
    return (c.x1 - c.x0) / scale <= DOT_MAX and (c.y1 - c.y0) / scale <= DOT_MAX


def _is_delimiter_glyph(c: Component, scale: float) -> bool:
    """A one-line delimiter TYPED as a character, `[`, `]`, `{`, `}`, `(`, `)`: taller than any letter (it reaches below
    the baseline and above the capitals) and narrow (page 305: 8.8 to 9.2 pt tall, 2.5 to 3.4 pt wide; a capital is 7.0,
    a descender letter 7.2)."""
    return (c.y1 - c.y0) / scale >= DELIM_MIN_H and (c.x1 - c.x0) / scale <= DELIM_MAX_W


def _dotted_letters(glyphs: list, scale: float) -> list:
    """The glyphs with each i's and j's dot joined to the stem below it, so a letter is ONE component (page 305: the dot
    of `identifier` stands 0.6 pt above its stem). A word's components then pair with its letters (`align`), and the dot
    is never a word of its own."""
    dots = [c for c in glyphs if _is_dot(c, scale)]
    body = [c for c in glyphs if not _is_dot(c, scale)]
    out, taken = list(body), set()
    for d in dots:
        mid = (d.x0 + d.x1) / 2
        below = [i for i, c in enumerate(out) if c.x0 <= mid <= c.x1 and 0 <= c.y0 - d.y1 <= DOT_ABOVE * scale
                 and not _is_dot(c, scale)]
        if below:
            i = min(below, key=lambda i: out[i].y0 - d.y1)
            c = out[i]
            out[i] = Component(min(c.x0, d.x0), d.y0, max(c.x1, d.x1), c.y1, c.area + d.area)
            taken.add(id(d))
    return out + [d for d in dots if id(d) not in taken]


def find_words(comps: list, skip: set, scale: float) -> list:
    """Word boxes in lines: [[(x0, y0, x1, y1, [component boxes])...] per printed line] in points.

    Three shapes are not split by spacing alone (kb/Work PB4849, measured on PDF page 305): an i's dot is joined to its
    letter (`_dotted_letters`); the dots of an ellipsis, spaced a character apart, stay one word `...`; and a typed
    delimiter (`_is_delimiter_glyph`) is a word of its own even when it is set hard against the word it encloses, as the
    converted page writes `[ON` and `[ROUNDED]}`: the pairing splits the converted words at the same delimiters."""
    glyphs = _dotted_letters([c for k, c in enumerate(comps, start=1) if k not in skip and c.area >= MIN_COMPONENT_PX],
                             scale)
    glyphs.sort(key=lambda c: (c.y0 + c.y1))
    # The LETTERS set the lines; a dot, a hyphen and a typed delimiter then join the line whose letters they overlap
    # most. Seeding a line from whatever came first let a speck above `[INPUT-OUTPUT` start a line the tall `[` then
    # stretched over the hyphen and the period, splitting INPUT-OUTPUT across two lines (page 196).
    def letter(c):                                     # a glyph no taller than a letter: a taller one (a typed
        return DOT_MAX < (c.y1 - c.y0) / scale < DELIM_MIN_H   # delimiter, or letters fused to a rule, page 310's
                                                       # `NO`) joins a line but never stretches it over the next one

    # A glyph is measured against the line's letters NEAREST it, not against the whole line's extent: a typed line can
    # run a little uphill (page 310 rises 4 pt across one format), and an extent stretched by that slope swallowed the
    # brace alternative printed half a line below the line's start.
    lines: list = []                                   # [[glyphs], [(x0, y0, y1) of its letters, sorted by x]]

    def local(ln, c):
        xs = ln[1]
        at = bisect.bisect_left(xs, (c.x0,))
        reach = LINE_REACH_PT * scale
        near = [p for p in xs[max(at - 3, 0):at + 3] if abs(p[0] - c.x0) <= reach]
        if not near:
            near = [min(xs, key=lambda p: abs(p[0] - c.x0))]
        return min(p[1] for p in near), max(p[2] for p in near)

    def best_line(c):
        fits = []
        for ln in lines:
            if not ln[1]:
                continue
            y0, y1 = local(ln, c)
            o = min(c.y1, y1) - max(c.y0, y0)
            if o >= 0.5 * min(c.y1 - c.y0, y1 - y0):
                fits.append((o, ln))
        return max(fits, key=lambda f: f[0])[1] if fits else None

    for c in [g for g in glyphs if letter(g)] + [g for g in glyphs if not letter(g)]:
        ln = best_line(c)
        if ln is None:
            ln = [[], []]
            lines.append(ln)
        ln[0].append(c)
        if letter(c) or not ln[1]:
            bisect.insort(ln[1], (c.x0, c.y0, c.y1))
    lines = [ln[0] for ln in lines]
    out = []
    for line in sorted(lines, key=lambda ln: min(g.y0 for g in ln)):
        line.sort(key=lambda g: g.x0)
        words, cur = [], [line[0]]
        for g in line[1:]:
            gap = (g.x0 - max(c.x1 for c in cur)) / scale
            ellipsis = _is_dot(g, scale) and _is_dot(cur[-1], scale) and gap <= ELLIPSIS_GAP_PT
            if (gap > WORD_GAP_PT and not ellipsis) or _is_delimiter_glyph(g, scale) or _is_delimiter_glyph(cur[-1], scale):
                words.append(cur)
                cur = []
            cur.append(g)
        words.append(cur)
        out.append([(min(c.x0 for c in w) / scale, min(c.y0 for c in w) / scale,
                     max(c.x1 for c in w) / scale, max(c.y1 for c in w) / scale,
                     [(c.x0 / scale, c.y0 / scale, c.x1 / scale, c.y1 / scale) for c in w]) for w in words])
    return out


def split_token(text: str) -> list:
    """A converted word split at its typed one-line delimiters, as `find_words` splits the measured word: `[ROUNDED]}`
    is `[`, `ROUNDED`, `]`, `}`."""
    return re.findall(r"[\[\]{}()]|[^\[\]{}()]+", text)


def token_rows(rows: list) -> list:
    """`[[(text, column), ...] per row]`: the converted words of one figure, each split by `split_token`. A row is a
    list of words (a column is then the word's offset in the row joined by spaces) or of (word, column) pairs."""
    out = []
    for row in rows:
        toks, col = [], 0
        for t in row:
            word, at = (t, col) if isinstance(t, str) else t
            for piece in split_token(word):
                toks.append((piece, at))
                at += len(piece)
            col = at + 1
        out.append(toks)
    return out


def _similarity(a: str, b: str) -> float:
    """How alike two spellings of one word are, 0 to 1, over the letters an OCR layer confuses (`l`/`1`, `O`/`0`)."""
    def fold(t):
        return t.lower().translate(OCR_FOLD)
    return difflib.SequenceMatcher(None, fold(a), fold(b)).ratio()


def _hint(box, hints: list) -> list:
    """The text-layer words that overlap a measured word box: (x0, y0, x1, y1, text) in deskewed points."""
    x0, y0, x1, y1 = box[:4]
    mid = (y0 + y1) / 2
    return [h[4] for h in hints if h[0] < x1 and h[2] > x0 and abs((h[1] + h[3]) / 2 - mid) <= HINT_DY]


#: The one disagreement that is a NOTE, not a failure to read: the word is paired and underlined, and only the trim of
#: `underline_runs` could not run, so the whole word keeps its rule.
TRIM_NOTE = "underlined word's components do not match its letters"


def _state(box, text, rules, where: dict, bad: list):
    """A paired word's underline state, `figure_extract`'s own (`is_underlined`, then `underline_runs` to trim, which can
    only remove characters), then a separator at either end the rule does not span taken off (`_trim_separators`). An
    underlined word whose components do not pair with its letters keeps its rule whole (but for those separators), and
    is reported when nothing could be trimmed."""
    x0, y0, x1, y1, chars = box
    state = FX.is_underlined((x0, y0, x1, y1), rules)
    if state is True and text and len(text) > 1:
        own = [r for r in rules if FX.MIN_ABOVE <= r.y0 - y1 <= FX.MAX_GAP
               and (min(x1, r.x1) - max(x0, r.x0)) / max(x1 - x0, 0.1) >= FX.MIN_COVER]
        matched = len(chars) == len(text)
        state = _trim_separators(FX.underline_runs(chars, own) if matched else True, chars, text, own)
        if state is True and not matched:
            bad.append(dict(where, text=text, components=len(chars), letters=len(text), reason=TRIM_NOTE))
    return state


#: A separator glyph a word may carry hard against it (`DECLARATIVES.`, `FILE:`): punctuation, never a letter.
SEPARATOR_GLYPHS = frozenset(".,;:")


def _trim_separators(state, chars, text: str, rules):
    """`state` (True, False or `underline_runs`' runs) with the separators at either end of the word taken off when the
    rule does not SPAN the separator's glyph (kb/Work PB4849). The 1985 standard is typewritten: an underline is struck
    as underscores, one per character cell, so an underlined separator has the rule running wholly under it, and a rule
    that stops at its foot or short of it underlines the letters only. On PDF page 182 the rule under `DECLARATIVES.`
    stops 3.6 pt short of the period; on page 380 the rule under `I-O-CONTROL.` overruns the L's foot 1.3 pt into the
    period's 2.2 pt glyph (past its centre), and both whole tokens were read as underlined where the printed page
    underlines the reserved word only. The glyph is the word's first or last component whether or not its components
    pair with its letters (an underline touching a letter fuses the two, never a separator printed clear of the rule).
    Like `underline_runs`, this only removes characters, and only separators."""
    if state is False or not chars:
        return state
    seps = "".join(SEPARATOR_GLYPHS)
    lead = len(text) - len(text.lstrip(seps))
    tail = len(text) - len(text.rstrip(seps))
    if lead + tail >= len(text):
        return state

    def spanned(c):
        cx0, _, cx1, cy1 = c
        return any(FX.MIN_ABOVE <= r.y0 - cy1 <= FX.MAX_GAP and r.x0 <= cx0 and r.x1 >= cx1 for r in rules)
    lead = lead if lead and not spanned(chars[0]) else 0
    tail = tail if tail and not spanned(chars[-1]) else 0
    if not (lead or tail):
        return state
    on = [True] * len(text) if state is True else [any(o <= i < o + n for o, n in state) for i in range(len(text))]
    for i in [*range(lead), *range(len(text) - tail, len(text))]:
        on[i] = False
    runs, cur = [], None
    for i, v in enumerate(on + [False]):
        if v and cur is None:
            cur = i
        elif not v and cur is not None:
            runs.append((cur, i - cur))
            cur = None
    return runs or False


DELIMS = frozenset("[]{}()")
#: The typed delimiters an OCR layer cannot tell apart, by side: it reads page 305's `[ROUNDED]}` as `[ROUNDED])`, as a
#: scan cannot tell `]` from `}` (`align`), so a hint naming any delimiter of the word's side supports it.
DELIM_SIDE = {ch: frozenset("[{(") if ch in "[{(" else frozenset("]})") for ch in DELIMS}


def _is_speck(box) -> bool:
    """A measured word that is no word: one glyph no taller than a dot (a speck of the scan), or a sliver an erased box
    rule left behind (`align` pairs no converted word with it and names no disagreement for it)."""
    return (len(box[4]) == 1 and (box[3] - box[1]) <= DOT_MAX) or (box[3] - box[1]) <= SLIVER_MAX


def _is_delimiter_box(box) -> bool:
    """A measured word that is one typed delimiter glyph (`_is_delimiter_glyph`, in points)."""
    x0, y0, x1, y1, _ = box
    return y1 - y0 >= DELIM_MIN_H and x1 - x0 <= DELIM_MAX_W


class _Frame:
    """The figure's two coordinate frames for `_assign`: the measured boxes in points and the converted words in rows and
    columns, each normalized to its own extent, and each side's word neighbours on its line or row."""

    def __init__(self, flat: list, tops: list, toks: list, nrow: int):
        self.flat, self.tops, self.toks, self.nrow = flat, tops, toks, nrow
        # The extents are the WORDS': a speck on a line of its own above a one-line format (PDF page 321, 4 pt above
        # `GO TO [procedure-name-1]`) stretched the figure's height over it and priced every pair 2.0 more (PB4849).
        real = [k for k, (_, _, b) in enumerate(flat) if not _is_speck(b)] or list(range(len(flat)))
        self.ymin = min(tops[k] for k in real)        # a box's height is its LINE's (`_line_top`): an ellipsis's ink
        self.yspan = max(max(tops[k] for k in real) - self.ymin, 1.0)  # starts 4.5 pt below its line's: no row of its own
        self.xmin = min(flat[k][2][0] for k in real)
        self.xspan = max(max(flat[k][2][2] for k in real) - self.xmin, 1.0)
        self.cmin = min((c for _, _, c in toks), default=0)
        self.cspan = max(max((c + len(t) for t, _, c in toks), default=1) - self.cmin, 1)
        # The typewriter's PITCH, so a word's length can be read from its width as well as from its glyph count: a scan
        # fuses touching letters (ERROR's `RR`, page 305) and breaks thin ones, and either alone miscounts.
        per = sorted((b[2] - b[0]) / len(b[4]) for _, _, b in flat if len(b[4]) >= 3)
        self.pitch = per[len(per) // 2] if per else None
        word_box = [k for k, (_, _, b) in enumerate(flat) if not (len(b[4]) == 1 and _is_delimiter_box(b))]
        word_tok = [t for t, (w, _, _) in enumerate(toks) if w not in DELIMS]
        self.box_side = {k: self._sides(k, [(flat[n][0], flat[n][2][0], n) for n in word_box], flat[k][0], flat[k][2][0])
                         for k in range(len(flat))}
        self.tok_side = {t: self._sides(t, [(toks[n][1], toks[n][2], n) for n in word_tok], toks[t][1], toks[t][2])
                         for t in range(len(toks))}

    @staticmethod
    def _sides(me, words, line, at):
        """The nearest word left and right of position `at` on `line`, as indices, or None."""
        same = [(x, n) for ln, x, n in words if ln == line and n != me]
        left = max((p for p in same if p[0] < at), default=None)
        right = min((p for p in same if p[0] > at), default=None)
        return (left[1] if left else None, right[1] if right else None)

    def cost(self, t: int, k: int, near, words: dict | None, placed: dict | None = None) -> float:
        """What pairing converted word `t` with measured box `k` costs. When `t` is a typed delimiter, `words` is the
        words' pairing ({box: (word, cost)}) and `placed` its inverse ({word: box})."""
        word, r, c = self.toks[t]
        box = self.flat[k][2]
        miss = abs(len(box[4]) - len(word))
        if self.pitch:
            miss = min(miss, abs((box[2] - box[0]) / self.pitch + PITCH_BEARING - len(word)))
        v = miss * PAIR_W_LEN
        # Half a row either way is free: a brace's alternatives and the words beside it are printed half a line apart
        # and drawn on whole rows (DISPLAY's `identifier-1` / `literal-1`, page 310).
        v += max(abs(r / self.nrow - (self.tops[k] - self.ymin) / self.yspan) - 0.5 / self.nrow, 0.0) * PAIR_W_ROW
        v += abs((c - self.cmin) / self.cspan - (box[0] - self.xmin) / self.xspan) * PAIR_W_COL
        if near is not None:
            said = near[k]
            if not said:
                v += PAIR_W_HINT / 2                  # the OCR layer drops punctuation and ellipses: no evidence either way
            elif word in DELIMS:
                # Only a delimiter of the other side is evidence against: the OCR reads a typed `}` as `1` too (PDF
                # page 182), which says nothing of its side, as a dropped one says nothing (kb/Work PB4849).
                v += (0.0 if any(DELIM_SIDE[word] & set(h) for h in said)
                      else PAIR_W_HINT if any(DELIMS & set(h) for h in said) else PAIR_W_HINT / 2)
            else:
                v += (1 - max(_similarity(h, word) for h in said)) * PAIR_W_HINT
        if words is not None:                         # a delimiter: its neighbours must be the word's
            line = self.flat[k][0]
            placed = placed if placed is not None else {w: b for b, (w, _) in words.items()}
            for box_side, tok_side in zip(self.box_side[k], self.tok_side[t]):
                got = words.get(box_side, (None,))[0] if box_side is not None else None
                if got == tok_side:
                    continue
                # A neighbour missing on one side contradicts nothing when the other side's neighbour is printed on
                # another line, or drawn on another row: the converted row sets a brace's alternative, printed half a
                # line off, beside the delimiter (`[USAGE IS] {COMP ...`, PDF page 205: the `]` ends its printed line,
                # and COMP, drawn after it on its row, is printed half a line below; kb/Work PB4849).
                if box_side is None and tok_side in placed and self.flat[placed[tok_side]][0] != line:
                    continue
                if tok_side is None and got is not None and self.toks[got][1] != r:
                    continue
                v += PAIR_W_NEIGHBOUR
        return v


def hungarian(cost: np.ndarray) -> list:
    """The cheapest one-to-one pairing of the rows of `cost` with its columns, as [(row, column), ...]: min(rows,
    columns) pairs. The Hungarian method with potentials, O(n^2 m), its inner loop over columns in numpy (numpy is the
    measurer's one numeric dependency; CI's self-tests install it and nothing more)."""
    if cost.shape[0] > cost.shape[1]:
        return sorted((r, c) for c, r in hungarian(cost.T))
    n, m = cost.shape
    u, v = np.zeros(n + 1), np.zeros(m + 1)
    p = np.zeros(m + 1, dtype=np.int64)              # p[j]: the row (1-based) holding column j; 0 is none
    way = np.zeros(m + 1, dtype=np.int64)
    for i in range(1, n + 1):
        p[0], j0 = i, 0
        minv = np.full(m + 1, np.inf)
        used = np.zeros(m + 1, dtype=bool)
        while True:
            used[j0] = True
            i0 = p[j0]
            cur = cost[i0 - 1] - u[i0] - v[1:]
            free = ~used[1:]
            better = free & (cur < minv[1:])
            minv[1:][better] = cur[better]
            way[1:][better] = j0
            j1 = int(np.argmin(np.where(free, minv[1:], np.inf))) + 1
            delta = minv[j1]
            u[p[used]] += delta
            v[used] -= delta
            minv[~used] -= delta
            j0 = j1
            if p[j0] == 0:
                break
        while j0:
            j1 = way[j0]
            p[j0] = p[j1]
            j0 = j1
    return sorted((int(p[j]) - 1, j - 1) for j in range(1, m + 1) if p[j])


def _assign(frame: _Frame, toks: list, boxes: list, near, words: dict | None) -> dict:
    """The cheapest one-to-one pairing of the converted words `toks` with the measured boxes `boxes` (indices into the
    frame): {box: (word, cost)}."""
    if not toks or not boxes:
        return {}
    placed = {w: b for b, (w, _) in words.items()} if words is not None else None
    cost = np.array([[frame.cost(t, k, near, words, placed) for k in boxes] for t in toks])
    return {boxes[c]: (toks[r], float(cost[r, c])) for r, c in hungarian(cost)}


def _deal(frame: _Frame, paired: dict, near) -> dict:
    """`paired` ({box: (word, cost)}) with the words of one spelling from one row that are paired on one printed line
    dealt out to those boxes in order: they are interchangeable, so which went to which box carries no information,
    and the typed delimiters are placed by them next (`[sentence] ... } ...`, PDF page 182: the two `...` came out
    crossed, and so did the `]` and the `}` placed by them; kb/Work PB4849)."""
    groups = collections.defaultdict(list)
    for k, (t, _) in paired.items():
        word, row, _ = frame.toks[t]
        groups[(frame.flat[k][0], row, word)].append(k)
    out = dict(paired)
    for ks in groups.values():
        if len(ks) > 1:
            boxes = sorted(ks, key=lambda k: frame.flat[k][1])
            words = sorted((paired[k][0] for k in ks), key=lambda t: frame.toks[t][2])
            out.update({k: (t, frame.cost(t, k, near, None)) for k, t in zip(boxes, words)})
    return out


def align(lines: list, tokens: list | None, rules: list, hints: list | None = None):
    """Pair the measured word boxes with the converted figure's words and read each word's underline state.

    `lines` is `find_words`' result (printed lines, top to bottom). `tokens` is the converted figure's words as rows,
    top to bottom (`token_rows`' input). The rows are the converted figure's, and they are NOT the printed lines: the
    converted page sets a brace's alternatives, printed half a line apart, on one row (PDF page 196). So the pairing is
    one ASSIGNMENT over the whole figure (`hungarian`), never row by row; the words first, then the typed delimiters by
    the words beside them (`_Frame.cost`). A pair costs
    - the difference between the word's letter count and the box's length, read as its glyph count (a glyph is a
      letter once `_dotted_letters` joins the i's dot) or as its width over the typewriter's pitch, whichever is closer,
    - the difference of their relative heights in the figure beyond half a row (the row, blank rows counted, against
      the top of the box's printed line), and of their relative columns,
    - and, when `hints` (`text_layer_hints`) are given, how unlike the word is to the OCR text that overlaps the box.
    The OCR text is a HINT of position only: a word's text is always the converted word (PB4817: the OCR layer misreads
    `or` as `OI`).

    The tool never picks silently. Each of these is a DISAGREEMENT: a pair costing more than PAIR_ACCEPT (that box keeps
    text None); a box no word pairs with (a speck or a sliver excepted: it is no word), or a line whose words are paired
    out of their converted order (every box of that line keeps text None: its pairing is in doubt); a word no box pairs
    with. Returns (words, disagreements), the words in `lines`' order."""
    flat = [(i, j, box) for i, line in enumerate(lines) for j, box in enumerate(line)]
    text = {}
    bad: list = []
    if tokens is not None and flat:
        toks = [(t, r, c) for r, row in enumerate(token_rows(tokens)) for t, c in row]
        frame = _Frame(flat, [_line_top(lines[i], b) for i, _, b in flat], toks, max(len(tokens) - 1, 1))
        near = {k: _hint(box, hints) for k, (_, _, box) in enumerate(flat)} if hints is not None else None
        is_delim = [len(b[4]) == 1 and _is_delimiter_box(b) for _, _, b in flat]
        index = {(i, j): k for k, (i, j, _) in enumerate(flat)}
        # Words first, then the typed delimiters by the words beside them: every `[` looks alike, so a bracket is
        # placed by its neighbours, never by its shape alone (`[ROUNDED]` came out `] ROUNDED [`, page 305).
        paired = _deal(frame, _assign(frame, [t for t in range(len(toks)) if toks[t][0] not in DELIMS],
                                      [k for k in range(len(flat)) if not is_delim[k]], near, None), near)
        paired.update(_assign(frame, [t for t in range(len(toks)) if toks[t][0] in DELIMS],
                              [k for k in range(len(flat)) if is_delim[k]], near, paired))
        spoiled = set()
        for k, (i, j, box) in enumerate(flat):
            got = paired.get(k)
            if got is None:
                if _is_speck(box):
                    continue                                   # a speck, or a sliver a box's erased rule left: no word
                bad.append({"line": i, "word": j, "reason": "no converted word pairs with this measured word",
                            "box": [round(v, 1) for v in box[:4]]})
                spoiled.add(i)
            elif got[1] > PAIR_ACCEPT:
                bad.append({"line": i, "word": j, "text": toks[got[0]][0], "cost": round(got[1], 2),
                            "reason": "the closest converted word is too unlike this measured word"})
            else:
                text[(i, j)] = got[0]
        for t_i in sorted(set(range(len(toks))) - {t for t, _ in paired.values()}):
            bad.append({"row": toks[t_i][1], "text": toks[t_i][0], "reason": "no measured word pairs with this converted word"})
        for i, line in enumerate(lines):                       # a line's words keep their converted order
            # Words of one spelling from one row are interchangeable: the `]]]]` that closes page 196's last line is
            # four equal words, and which `]` went to which box carries no information. They are dealt out in order.
            # So are the typed delimiters of one run between two words: a scan cannot tell `]` from `}`, and in
            # `[ROUNDED]}` (page 305) only their order says which is which.
            same = collections.defaultdict(list)
            run = 0
            for j in range(len(line)):
                delim = is_delim[index[(i, j)]]
                run += not delim
                if (i, j) in text:
                    t = toks[text[(i, j)]]
                    same[(t[1], "delims", run) if delim and t[0] in DELIMS else (t[1], t[0])].append(j)
            for js in same.values():
                for j, t_i in zip(js, sorted((text[(i, j)] for j in js), key=lambda t: toks[t][2])):
                    text[(i, j)] = t_i
            seq = [text[(i, j)] for j in range(len(line)) if (i, j) in text]
            by_row = collections.defaultdict(list)
            for t_i in seq:
                by_row[toks[t_i][1]].append(toks[t_i][2])
            if any(cols != sorted(cols) for cols in by_row.values()):
                bad.append({"line": i, "reason": "words paired out of their converted order",
                            "tokens": [toks[t_i][0] for t_i in seq]})
                spoiled.add(i)
        text = {key: toks[t_i][0] for key, t_i in text.items() if key[0] not in spoiled}
    words = []
    for i, j, box in flat:
        t = text.get((i, j))
        x0, _, x1, y1, _ = box
        words.append({"text": t, "x0": round(x0, 1), "y0": round(_line_top(lines[i], box), 1), "x1": round(x1, 1),
                      "y1": round(y1, 1), "underlined": _state(box, t, rules, {"line": i, "word": j}, bad)})
    return words, bad


def _line_top(line: list, box) -> float:
    """A word's top as `render_figure` reads it: the top of its printed LINE, as a text layer's word box is the font's,
    not the top of its own ink. An ellipsis's ink starts 4.5 pt below its line's capitals and a typed bracket's above
    them (page 305), and either read as its own top lands on a row of its own. A line of dots or brackets alone keeps
    its own top."""
    letters = [b[1] for b in line if not _is_delimiter_box(b) and (b[3] - b[1]) > DOT_MAX]
    return min(letters) if letters else box[1]


def text_layer_hints(page, skew_deg: float) -> list:
    """The PDF's OCR words as position hints for `align`: (x0, y0, x1, y1, text) moved into the deskewed frame the
    measurement is in (`deskew` moves the ink at x up by x * tan(skew))."""
    tan = math.tan(math.radians(skew_deg)) if abs(skew_deg) >= MIN_SKEW_FIX_DEG else 0.0
    out = []
    for x0, y0, x1, y1, t, *_ in page.get_text("words"):
        dy = -((x0 + x1) / 2) * tan
        out.append((x0, y0 + dy, x1, y1 + dy, t))
    return out


def _hint_lines(hints: list) -> list:
    """The hints grouped into printed lines, top to bottom."""
    lines: list = []
    for h in sorted(hints, key=lambda h: (h[1], h[0])):
        if lines and abs(h[1] - lines[-1][0][1]) <= HINT_LINE_DY:
            lines[-1].append(h)
        else:
            lines.append([h])
    return lines


def locate(figures: list, hints: list) -> list:
    """Where each converted figure is printed on the page: [(y0, y1) or None per figure], in deskewed points.

    The figures are in reading order and do not overlap, so they are placed TOGETHER, as disjoint runs of the page's
    printed lines in that order, by dynamic programming: a run scores one for each OCR word that matches a still unused
    word of its figure (`_similarity` >= LOCATE_MATCH) and loses one for each that does not. Placing each figure on its
    own fails on the commonest page, a statement's formats: ADD's three formats share `[ON SIZE ERROR ...]`, and the
    first format alone claims all three (PDF page 305). A figure no run scores above zero for is None: unlocated."""
    lines = _hint_lines(hints)
    n, f = len(lines), len(figures)
    bags = [collections.Counter(p for row in token_rows(fig) for p, _ in row if not re.fullmatch(r"[\[\]{}()]", p))
            for fig in figures]

    # Each OCR word's closest word of each figure, once: (matched word or None) per figure, per line.
    # A word shorter than LOCATE_MIN_WORD is not counted either way: `ON`, `TO` are in every figure, and the OCR layer
    # reads a tall brace's pieces as two-letter garbage (`Am`, `Cm` beside ADD Format 3's brace, page 305).
    said = [[w for h in line if len(w := h[4].strip("[]{}().,|")) >= LOCATE_MIN_WORD] for line in lines]
    found = [[collections.Counter() for _ in range(n)] for _ in range(f)]
    for k in range(f):
        for li, words in enumerate(said):
            for word in words:
                best = max(bags[k], key=lambda w: _similarity(word, w), default=None)
                if best is not None and _similarity(word, best) >= LOCATE_MATCH:
                    found[k][li][best] += 1
    table = {}                                 # (k, a, b) -> score of figure k on lines a..b
    for k in range(f):
        for a in range(n):
            have, total = collections.Counter(), 0
            for b in range(a, n):
                have += found[k][b]
                total += len(said[b])
                matched = sum(min(c, bags[k][w]) for w, c in have.items())   # a word of the figure is used once
                table[(k, a, b)] = matched - max(total - matched, 0)

    neg = float("-inf")
    best = [[0.0] * (n + 1)] + [[neg] * (n + 1) for _ in range(f)]
    back: dict = {}
    for k in range(f):
        for j in range(1, n + 1):
            if best[k + 1][j - 1] > best[k + 1][j]:
                best[k + 1][j], back[(k + 1, j)] = best[k + 1][j - 1], None
            for a in range(j):
                if best[k][a] == neg:
                    continue
                v = best[k][a] + table[(k, a, j - 1)]
                if v > best[k + 1][j]:
                    best[k + 1][j], back[(k + 1, j)] = v, a
    out: list = [None] * f
    k, j = f, n
    while k > 0 and j > 0:
        if (k, j) not in back:
            break
        a = back[(k, j)]
        if a is None:
            j -= 1
            continue
        if table[(k - 1, a, j - 1)] > 0:
            out[k - 1] = (min(h[1] for h in lines[a]) - LOCATE_PAD, max(h[3] for h in lines[j - 1]) + LOCATE_PAD)
        k, j = k - 1, a
    placed = [k for k, b in enumerate(out) if b is not None]
    for k0, k1 in zip(placed, placed[1:]):            # two padded bands meet at their midpoint, never overlap
        if out[k0][1] > out[k1][0]:
            mid = (out[k0][1] + out[k1][0]) / 2
            out[k0], out[k1] = (out[k0][0], mid), (mid, out[k1][1])
    return out


# ---------------------------------------------------------------- the entry points

def measure_image(gray: np.ndarray, dpi: int = DPI, tokens: list | None = None, skew: float | None = None,
                  hints: list | None = None) -> RasterPage:
    """Measure a grayscale page image. `tokens` and `hints` as for `align`, the whole image being one figure (`read`
    reads several). `skew` is the page's rotation in degrees when the image is only a CROP of it (the calibration
    fixtures): a crop holds too little ink to estimate the page's angle."""
    scale = dpi / 72
    ink = binarize(gray)
    if skew is None:
        # The angle is the SCAN's, so only the scanned body is measured: the per-copy stamp printed into the page's top
        # and bottom margins is level, and on a page of few lines it outweighed the text (page 310: 0.0 found, the text
        # rising 4 pt across the format, so one printed line read as two).
        top, bottom = int(SKEW_BODY[0] * scale), int(SKEW_BODY[1] * scale)
        skew = estimate_skew(ink[top:bottom]) if ink.shape[0] > bottom else estimate_skew(ink)
    ink = _close(_close(deskew(ink, skew), 0), 1)
    labels, comps = label(ink)
    ink, boxes = find_boxes(ink, labels, comps, scale)
    if boxes:
        labels, comps = label(ink)
    ink, relabelled, rejoined, fused = split_fused_rules(ink, comps, scale)
    if relabelled is not None:
        labels, comps = relabelled, rejoined
    stems = split_stacked_stems(find_stems(ink, labels, comps, scale), ink, scale)
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
    rp.lines = find_words(comps, skip, scale)
    rp.boxes = [{"x0": round(b.x0, 2), "y0": round(b.y0, 2), "x1": round(b.x1, 2), "y1": round(b.y1, 2), "words": []}
                for b in sorted(boxes, key=lambda r: (r.y0, r.x0))]
    read(rp, [(None, tokens)], hints)
    return rp


def read(rp: RasterPage, figures: list, hints: list | None = None) -> None:
    """Give the measured words their text: each `(band, tokens)` of `figures` pairs `tokens` (`align`) with the measured
    lines whose words lie in `band` ((y0, y1) in points; None for the whole page). The other lines keep text None. Sets
    `rp.words`, `rp.disagreements` (each carrying its figure's index) and each box's `words`."""
    rp.words, rp.disagreements = [], []
    claimed: dict = {}
    for k, (band, tokens) in enumerate(figures):
        for i, line in enumerate(rp.lines):
            if i not in claimed and (band is None or all(band[0] <= w[1] <= band[1] for w in line)):
                claimed[i] = k
    by_figure = collections.defaultdict(list)
    for i, k in claimed.items():
        by_figure[k].append(i)
    read_lines: dict = {}
    for k, (band, tokens) in enumerate(figures):
        idx = sorted(by_figure[k])
        if tokens is not None and not idx:
            rp.disagreements.append({"figure": k, "reason": "no measured line lies in the figure's band"})
            continue
        words, bad = align([rp.lines[i] for i in idx], tokens, rp.rules, hints)
        rp.disagreements += [dict(d, figure=k, line=idx[d["line"]]) if "line" in d else dict(d, figure=k) for d in bad]
        it = iter(words)
        for i in idx:
            read_lines[i] = [next(it) for _ in rp.lines[i]]
    for i, line in enumerate(rp.lines):
        rp.words += read_lines.get(i) or align([line], None, rp.rules)[0]
    for b in rp.boxes:
        b["words"] = [i for i, w in enumerate(rp.words)
                      if b["x0"] < (w["x0"] + w["x1"]) / 2 < b["x1"] and b["y0"] < (w["y0"] + w["y1"]) / 2 < b["y1"]]


def measure_page(edition: int, page: int, tokens: list | None = None, dpi: int = DPI) -> RasterPage:
    """Measure one page of an edition's scanned PDF (1-based `page`)."""
    return measure_image(render(edition, page, dpi), dpi, tokens)


def measure_figures(edition: int, page: int, figures: list, doc=None, dpi: int = DPI) -> tuple:
    """Measure one scanned page and read the converted figures printed on it: `figures` is each figure's words as rows
    (`token_rows`' input), in reading order. Each figure is placed by `locate` and its words paired by `align`, both with
    the PDF's OCR words as position hints. Returns (the RasterPage, [(y0, y1) or None per figure])."""
    import pymupdf
    doc = doc if doc is not None else pymupdf.open(spec_pdf.require(edition))
    rp = measure_image(render(edition, page, dpi), dpi)
    hints = text_layer_hints(doc[page - 1], rp.skew_deg)
    bands = locate(figures, hints)
    read(rp, [(b, t) for b, t in zip(bands, figures) if b is not None], hints)
    index = [k for k, b in enumerate(bands) if b is not None]    # read()'s figure k is figures[index[k]]
    for d in rp.disagreements:
        d["figure"] = index[d["figure"]]
    rp.disagreements += [{"figure": k, "reason": "the figure was not located on the page"}
                         for k, b in enumerate(bands) if b is None]
    return rp, bands


def classify_page(rp: RasterPage) -> list:
    """`figure_geometry.classify_page`'s result for a raster page: its brackets and bars, and each level-2 box as its
    two sides, stems of kind "box" with the side they close (a vector page has no box; `render_figure` draws them by
    FIGURE-STYLE.md rule 9, kb/Work PB4849). A brace is no stem there: `extract` reports it as the Symbol glyph pieces
    `figure_extract.GLYPH_PIECES` decodes."""
    return rp.stems + [box_side(b, side) for b in rp.boxes for side in ("L", "R")]


def box_side(b: dict, side: str) -> dict:
    """One side of a measured box as a stem: the left edge opens, the right edge closes, and both span the box's
    rules, so `render_figure` encloses the rows between them as it does a bracket's."""
    return {"x": b["x0"] if side == "L" else b["x1"], "y0": b["y0"], "y1": b["y1"], "h": b["y1"] - b["y0"],
            "kind": "box", "side": side}


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
    """`figure_extract.extract`'s result for a raster page (the same sort order and keys). A word `read` gave no text
    is left out: it is in `rp.disagreements` already, and the figure drawn without it fails the sweep's word gate."""
    out = [{k: w[k] for k in ("text", "x0", "y0", "x1", "underlined")} for w in rp.words
           if w["text"] is not None and (not band or band[0] <= w["y0"] <= band[1])]
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
