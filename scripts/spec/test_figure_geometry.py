#!/usr/bin/env python3
"""Self-test for figure_geometry.py's bracket-versus-bar call: a foot belongs only to the stem whose stroke it starts in.
Run: python scripts/spec/test_figure_geometry.py   (every gate and CI run it through scripts/self_tests.py.)

The rects are the vector geometry measured from the printed standard (no PDF needed here): SEND's ON EXCEPTION
group (folio 726), where each choice bar stands 0.70 pt clear of the foot of the bracket outside it and was drawn as
a bracket turned the wrong way (kb/Work PB3530). The genuine brackets beside those bars still read as brackets.
"""
import pathlib
import sys
from typing import NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import figure_geometry as G  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402


class Rect(NamedTuple):
    """The four edges `_foot_side` reads from a PyMuPDF rect, and its width."""
    x0: float
    y0: float
    x1: float
    y1: float

    @property
    def width(self) -> float:
        return self.x1 - self.x0


# SEND Format 1's ON EXCEPTION group, folio 726 (PDF page 756): every horizontal rule within reach of its stems.
FEET = [
    Rect(86.33, 303.78, 89.83, 304.25), Rect(86.1, 345.33, 89.83, 345.8),       # the opening bracket's feet
    Rect(312.06, 303.78, 315.33, 304.25), Rect(312.06, 345.33, 315.56, 345.8),  # the closing bracket's feet
    Rect(90.53, 344.63, 91.0, 344.86), Rect(311.36, 344.63, 311.82, 344.86),    # the bars' end caps: too short
    Rect(99.4, 341.13, 117.61, 341.6),                                          # an underline: too long
]
CASES = [
    ("SEND's opening bracket", Rect(86.1, 303.78, 86.56, 345.56), "L"),
    ("SEND's left choice bar, 0.70 pt clear of that bracket's foot", Rect(90.53, 307.28, 91.0, 344.63), None),
    ("SEND's right choice bar", Rect(311.36, 307.28, 311.82, 344.63), None),
    ("SEND's closing bracket", Rect(315.09, 303.78, 315.56, 345.56), "R"),
]


def main() -> int:
    fails = []
    for name, stem, want in CASES:
        got = G._foot_side(stem, FEET)
        if got != want:
            fails.append(f"{name}: read as {got!r}, want {want!r}")
    dispatch = edition_dispatch()
    fails.extend(dispatch)
    for f in fails:
        print(f"FAIL: {f}")
    total = len(CASES) + EDITION_CASES
    print(f"figure_geometry self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    return 1 if fails else 0


#: How many checks `edition_dispatch` makes: the stems and the words of two vector editions and of the raster one.
EDITION_CASES = 6


def edition_dispatch() -> list[str]:
    """`page_classifier` and `figure_extract.page_extractor` pick the arm the edition table records (kb/Work PB4815): the
    vector readers of a PyMuPDF page, or the raster ones of a `figure_raster.RasterPage` (PB4849). One table, one
    dispatch: a raster stem read with words read from vectors would draw a scan's figure with no underline at all."""
    import figure_extract as X
    import figure_raster
    fails = []
    for edition in (2002, 2023):
        if G.page_classifier(edition) is not G.classify_page:
            fails.append(f"{edition} is a vector edition and must read strokes with classify_page")
        if X.page_extractor(edition) is not X.extract:
            fails.append(f"{edition} is a vector edition and must read words with figure_extract.extract")
    if G.page_classifier(1985) is not figure_raster.classify_page:
        fails.append("1985 is a raster edition and must read stems with figure_raster.classify_page")
    if X.page_extractor(1985) is not figure_raster.extract:
        fails.append("1985 is a raster edition and must read words with figure_raster.extract")
    return fails


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
