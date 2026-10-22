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
    for f in fails:
        print(f"FAIL: {f}")
    print(f"figure_geometry self-test: {len(CASES) - len(fails)}/{len(CASES)} " + ("GREEN" if not fails else "RED"))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
