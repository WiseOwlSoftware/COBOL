#!/usr/bin/env python3
"""Measure the DELIMITER GEOMETRY of the printed general-format figures, straight from the PDF's vector data.

WHY THIS EXISTS. The reconciliation sweep repeatedly had to decide whether a printed figure carries the §2023:5.2.6.4
CHOICE INDICATOR — a pair of vertical bars just inside a bracket or brace — because the difference is normative:

    [  A / B  ]     per §2023:5.2.6.2   at most ONE of A, B
    [ |A / B| ]     per §2023:5.2.6.4   ZERO OR MORE of A, B, each at most once, in any order
    {  A / B  }     per §2023:5.2.6.3   exactly ONE of A, B
    { |A / B| }     per §2023:5.2.6.4   ONE OR MORE of A, B, each at most once, in any order

Dropping the bars turns legal COBOL into apparently illegal COBOL, which is the falsely-restrictive loss this
whole effort is hunting. Until now that call was made by rendering the page and squinting at it — slow, and a
judgement call at exactly the point where a wrong judgement silently corrupts the grammar.

It does not have to be a judgement call. The brackets and the bars are VECTOR RECTANGLES in the PDF content
stream, so they can be MEASURED. Crucially this is immune to the obfuscated text layer (the fonts carry shifted
cmaps, which is why `render-spec-page.py` exists at all) — vector geometry has no encoding to obfuscate.

HOW A BAR IS TOLD FROM A BRACKET. A bracket stem carries short horizontal FEET at one or both ends, turning
inward; a choice-indicator bar is a bare vertical rule with no feet, and is drawn slightly TALLER than the bracket
it sits inside. Measured on page 632, the ON SIZE ERROR group of COMPUTE Format 1:

    x= 76.29  h=31.98  feet at both ends, turning right   -> '[' bracket stem
    x= 81.19  h=37.58  no feet, taller                    -> '|' choice indicator
    x=304.36  h=37.58  no feet, taller                    -> '|' choice indicator
    x=309.72  h=31.98  feet at both ends, turning left    -> ']' bracket stem

A foot is distinguished from an UNDERLINE (which marks a required word, §2023:5.2.2) by length: feet are 2-8 pt and
start at the stem's own x, underlines are far longer and sit under text.

CAVEAT, stated because it bounds what this tool may be used for. Curly braces are typeset as FONT GLYPHS, not
rects, so a brace does not appear in the vector data at all. This tool therefore reports bars and brackets with
certainty, and infers a brace only from a bar that has no bracket around it. When the enclosing delimiter matters,
render the page. What the tool is authoritative about is the BARS — and those are what the sweep keeps losing.

    python scripts/spec/figure_geometry.py 632              # one page, annotated
    python scripts/spec/figure_geometry.py --all            # every choice-indicator group in the standard
    python scripts/spec/figure_geometry.py --all --json out.json
    python scripts/spec/figure_geometry.py 196 --edition 1985   # another edition (default 2023)

EDITIONS (kb/Work PB4815). `spec_pdf.EDITIONS` records, per edition, whether its general formats are VECTOR strokes (2002,
2023: measured here) or RASTER pixels (1985, a scan). `page_classifier(edition)` returns the page -> stems function of the
edition's arm; the raster arm is `figure_raster.classify_page`, whose page is the scan measured by
`figure_raster.measure_figures` (PB4817, PB4849). This command line reads vector pages only: a scan's page needs the
converted figures' words to be measured against, and `render_figure.py --edition 1985` is the command that has them.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from typing import NamedTuple
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)

# Geometry thresholds, in PDF points. Deliberately loose: the figures are typeset at one size throughout.
MIN_VERT_H = 10.0     # a delimiter stem is at least this tall
MAX_VERT_W = 3.0      # ... and this thin
MAX_HORZ_H = 3.0      # a horizontal rule is at most this thick
FOOT_MIN, FOOT_MAX = 1.5, 9.0   # a bracket foot's length; longer means it is an underline
TOL = 1.2             # coincidence tolerance for "touches the end of the stem"
STROKE_EPS = 0.1      # how far a foot's anchored end may lie outside its stem's own stroke (measurement noise)


class Stroke(NamedTuple):
    """One stroke of a page's vector drawing, as the rectangle it covers."""
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


def strokes(page) -> list[Stroke]:
    """Every stroke of a page's vector drawing as its own rectangle — the one reader of the drawing for the vector arm
    (`_load`'s stems and feet, `figure_extract.underlines`).

    A DRAWING PATH IS NOT A STROKE. 2023 fills one rectangle per path, so a path's bounding box was the stroke; 2002
    strokes LINES and puts many in one path (MULTIPLY Format 1, folio 482: one path of 17 lines holds its underlines
    and both feet and stems of its brackets), whose bounding box is no stroke at all (kb/Work PB4813). So each item
    of a path is measured: a rectangle as itself, a line as its two end points widened by the path's stroke width."""
    out = []
    for path in page.get_drawings():
        if any(item[0] not in ("re", "l") for item in path["items"]):
            out.append(Stroke(*path["rect"]))          # a curve or quad: no stem, foot or underline is drawn so
            continue
        half = (path.get("width") or 0.0) / 2
        for item in path["items"]:
            if item[0] == "re":
                out.append(Stroke(*item[1]))
                continue
            (ax, ay), (bx, by) = item[1], item[2]
            across, down = (half if ax == bx else 0.0), (half if ay == by else 0.0)
            out.append(Stroke(min(ax, bx) - across, min(ay, by) - down, max(ax, bx) + across, max(ay, by) + down))
    return out


def _load(page):
    """Split a page's vector strokes into vertical stems and horizontal rules."""
    verts, horzs = [], []
    for r in strokes(page):
        if r.height >= MIN_VERT_H and r.width <= MAX_VERT_W:
            verts.append(r)
        # A rule as short as a foot (FOOT_MIN) is a horizontal: 2002's bracket feet are 2.7 pt (folio 482), under the
        # 3 pt that every 2023 foot exceeds, so a floor of MAX_HORZ_H read each 2002 bracket as a bare bar (PB4813).
        elif r.width >= FOOT_MIN and r.width > r.height and r.height <= MAX_HORZ_H:
            horzs.append(r)
    return verts, horzs


def _foot_side(v, horzs):
    """Which way this stem's feet turn: 'L' for an opening '[', 'R' for a closing ']', None for a bare bar.

    A bracket's feet turn INWARD, so their direction states the bracket's hand outright. This used to return
    only yes/no and callers recovered the hand from which side of the enclosure's midpoint the stem sat on —
    which is wrong whenever an enclosure is measured on its own, because a single stem is never left of its
    own midpoint and so always drew as a CLOSING bracket. Measuring the feet removes the inference.
    """
    votes = []
    vmid = (v.x0 + v.x1) / 2
    for h in horzs:
        if not (FOOT_MIN <= h.width <= FOOT_MAX):
            continue                                  # too long: that is an underline under a required word
        if min(abs(h.y0 - v.y0), abs(h.y0 - v.y1), abs(h.y1 - v.y0), abs(h.y1 - v.y1)) > TOL:
            continue                                  # not at either end of the stem
        # A foot is ANCHORED IN its own stem's stroke and extends AWAY from it. Accepting a rule that merely
        # touches either end of the stem let a CHOICE-INDICATOR BAR adopt the foot of the bracket beside it: on
        # folio 503 the bracket's foot ends at 243.43 and the bar starts at 244.60, 1.17 pt away. Comparing the
        # near end with the stem's edge within TOL was not enough either: SEND's bars (folio 726) stand 0.70 pt
        # clear of the bracket's foot, whose end then fell within TOL of the bar's FAR edge (0.47 pt of stroke
        # further), so both bars drew as brackets turned the wrong way — `[ ]  ON EXCEPTION …  [ ]` (kb/Work
        # PB3530). A real foot's anchored end lies INSIDE its stem's stroke; a neighbour's foot never reaches it.
        if v.x0 - STROKE_EPS <= h.x0 <= v.x1 + STROKE_EPS and h.x1 > v.x1:
            votes.append("L")                         # foot turns right: an opening bracket
        elif v.x0 - STROKE_EPS <= h.x1 <= v.x1 + STROKE_EPS and h.x0 < v.x0:
            votes.append("R")                         # foot turns left: a closing bracket
    return max(set(votes), key=votes.count) if votes else None


def classify_page(page):
    """Return the delimiter stems on a page, each tagged 'bracket' or 'bar'."""
    verts, horzs = _load(page)
    out = []
    for v in sorted(verts, key=lambda r: (round(r.y0, 1), r.x0)):
        side = _foot_side(v, horzs)
        out.append({
            "x": round(v.x0, 2), "y0": round(v.y0, 2), "y1": round(v.y1, 2), "h": round(v.height, 2),
            "kind": "bracket" if side else "bar", "side": side,
        })
    return out


def page_classifier(edition: int):
    """The page -> delimiter stems function of `edition`'s measuring arm (spec_pdf.geometry): `classify_page` over a
    PyMuPDF page's vector strokes, or `figure_raster.classify_page` over a `figure_raster.RasterPage`, the scan measured
    by `figure_raster.measure_figures` (PB4817, PB4849). `figure_extract.page_extractor` is the same dispatch for the
    words, so a caller takes both from one edition and hands them the same page."""
    arm = spec_pdf.geometry(edition)
    if arm == "vector":
        return classify_page
    if arm == "raster":
        import figure_raster
        return figure_raster.classify_page
    raise ValueError(f"spec_pdf.EDITIONS[{edition}].geometry is {arm!r}, not 'vector' or 'raster'")


def group_enclosures(stems, ytol=6.0):
    """Cluster stems that span the same vertical band — one printed enclosure per cluster."""
    groups, cur = [], []
    for s in sorted(stems, key=lambda s: (round(s["y0"], 0), s["x"])):
        if cur and abs(s["y0"] - cur[0]["y0"]) > ytol:
            groups.append(cur)
            cur = []
        cur.append(s)
    if cur:
        groups.append(cur)
    return groups


def signature(g) -> str:
    """A left-to-right delimiter signature, e.g. '[ | | ]' for a bracket carrying choice indicators.

    Bracket stems are drawn as '[' or ']' by which half of the enclosure they sit in, so the signature reads the
    way the printed figure does.
    """
    ordered = sorted(g, key=lambda s: s["x"])
    mid = (ordered[0]["x"] + ordered[-1]["x"]) / 2 if ordered else 0
    return " ".join(
        ("|" if s["kind"] == "bar" else ("[" if s["x"] < mid else "]")) for s in ordered
    )


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("page", nargs="?", type=int, help="PDF page number")
    ap.add_argument("--all", action="store_true", help="scan every page for choice-indicator groups")
    ap.add_argument("--json", help="write the --all result to this file")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()

    spec_pdf.require_vector(args.edition, "figure_geometry.py")
    pdf = spec_pdf.require(args.edition)
    import pymupdf

    classify = page_classifier(args.edition)
    doc = pymupdf.open(pdf)

    if args.page:
        stems = classify(doc[args.page - 1])
        print(f"PDF page {args.page}: {len(stems)} delimiter stems")
        for g in group_enclosures(stems):
            bars = sum(1 for s in g if s["kind"] == "bar")
            flag = "  <-- CHOICE INDICATORS (5.2.6.4)" if bars >= 2 else ""
            print(f"\n  y={g[0]['y0']:7.2f}  {signature(g):<20}{flag}")
            for s in sorted(g, key=lambda s: s["x"]):
                print(f"      x={s['x']:7.2f}  h={s['h']:6.2f}  {s['kind']}")
        return 0

    if not args.all:
        ap.error("give a page number, or --all")

    found = []
    for pno in range(1, doc.page_count + 1):
        for g in group_enclosures(classify(doc[pno - 1])):
            bars = [s for s in g if s["kind"] == "bar"]
            if len(bars) < 2:
                continue
            found.append({"page": pno, "y": round(g[0]["y0"], 1), "signature": signature(g),
                          "bars": len(bars), "brackets": len(g) - len(bars)})
    print(f"choice-indicator groups printed in the standard: {len(found)}")
    print(f"pages carrying at least one: {len({f['page'] for f in found})}\n")
    print(f"{'page':>5} {'y':>8}  {'bars':>4} {'brkt':>4}  signature")
    for f in found:
        print(f"{f['page']:>5} {f['y']:>8.1f}  {f['bars']:>4} {f['brackets']:>4}  {f['signature']}")
    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(found, indent=2), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
