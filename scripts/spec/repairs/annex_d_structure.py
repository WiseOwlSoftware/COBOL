#!/usr/bin/env python3
"""Draw Annex D Figure D.3 — Compilation group and run unit structures (printed folio 1044).

NOT A FLOWCHART, which is why neither of the other two generators covers it. D.3 is a nested schematic in
which the BORDER WEIGHT is the notation: the printed figure distinguishes four entities purely by how their
rectangle is drawn, and its own Legend says so. Box-drawing has exactly those weights, so the distinction
survives the transcription:

    ┏━━┓  heavy   compilation group · run unit
    ┌──┐  light   compilation unit · source unit · runtime module
    ┌╌╌┐  dashed  source element · runtime element

What the figure SAYS, and what therefore has to survive: containment (a compilation group holds compilation
units, which hold source units, which hold source elements), the example hierarchy P-1 / P-1-1 / P-1-2 /
P-1-2-1 / P-1-2-2, and — the point of the whole diagram — that runtime modules from one compilation group need
not land in the same run unit, and a run unit need not draw from one compilation group. That last part is
carried by the `To other run unit` / `From other comp. group` arrows on the flanking units.

    python scripts/spec/repairs/annex_d_structure.py            # render
    python scripts/spec/repairs/annex_d_structure.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

STYLES = {
    "heavy": ("┏", "┓", "┗", "┛", "━", "┃"),
    "light": ("┌", "┐", "└", "┘", "─", "│"),
    "dashed": ("┌", "┐", "└", "┘", "╌", "╎"),
}


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        """⚠ REFUSES to overwrite. This class was written without the guard the other two generators have,
        and the two label bands promptly merged into `Fromtotherucomp.tgroup` — unreadable, and silent. The
        guard is not optional infrastructure; it is the only thing standing between a layout slip and a
        plausible-looking picture."""
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}")
            self.cells[(r, c + k)] = ch

    def rect(self, r0, c0, r1, c1, style, label=None):
        """A rectangle in one of the three weights, optionally with a label cut into its top edge."""
        tl, tr, bl, br, h, v = STYLES[style]
        top = tl + h * (c1 - c0 - 1) + tr
        if label:
            at = 2
            top = top[:at] + f" {label} " + top[at + len(label) + 2:]
        self.put(r0, c0, top)
        self.put(r1, c0, bl + h * (c1 - c0 - 1) + br)
        for r in range(r0 + 1, r1):
            self.put(r, c0, v)
            self.put(r, c1, v)

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


def figure_d3():
    cv = Canvas()
    # ── the compilation group ───────────────────────────────────────────────────────────────────────
    cv.rect(0, 0, 24, 70, "heavy")
    cv.rect(1, 2, 23, 12, "light")                       # a compilation unit, flanking
    cv.rect(1, 58, 23, 68, "light")                      # …and another
    cv.rect(1, 14, 23, 56, "light")                      # the compilation unit being expanded
    cv.rect(2, 16, 22, 54, "light", "P-1")               # its source unit
    cv.rect(3, 18, 6, 52, "dashed")                      # a source element of P-1
    cv.rect(7, 18, 21, 32, "light", "P-1-1")
    cv.rect(8, 20, 20, 30, "dashed")
    cv.rect(7, 34, 21, 52, "light", "P-1-2")
    cv.rect(8, 36, 11, 50, "dashed")
    # P-1-2 holds two further source units; their names are printed above them, inside P-1-2.
    cv.put(12, 36, "P-1-2-1")
    cv.rect(13, 36, 20, 42, "light")
    cv.put(12, 44, "P-1-2-2")
    cv.rect(13, 44, 20, 50, "light")
    # …each holding a source element of its own, as the printed figure shows.
    cv.rect(14, 38, 19, 40, "dashed")
    cv.rect(14, 46, 19, 48, "dashed")

    # ── how modules reach run units ─────────────────────────────────────────────────────────────────
    for c, text in ((7, "To other run unit"), (63, "To other run unit")):
        cv.put(25, c, "│")
        cv.put(26, c, "▼")
        cv.put(27, max(0, c - len(text) // 2), text)
    cv.put(25, 35, "│")
    cv.put(26, 35, "▼")
    cv.put(27, 35, "│")

    # ── the run unit ────────────────────────────────────────────────────────────────────────────────
    # The two label bands need rows of their own: the first draft put both on row 27 and they overwrote
    # each other into nonsense.
    base = 32
    for c, text in ((7, "From other comp. group"), (63, "From other comp. group")):
        cv.put(base - 3, max(0, c - len(text) // 2), text)
        cv.put(base - 2, c, "│")
        cv.put(base - 1, c, "▼")
    cv.put(base - 2, 35, "│")
    cv.put(base - 1, 35, "▼")
    cv.rect(base, 0, base + 20, 70, "heavy")
    cv.rect(base + 1, 2, base + 19, 12, "light")         # a runtime module
    cv.rect(base + 1, 58, base + 19, 68, "light")
    cv.rect(base + 1, 14, base + 19, 56, "light")
    cv.rect(base + 3, 18, base + 6, 52, "dashed")        # runtime elements, mirroring the source elements
    cv.rect(base + 8, 18, base + 18, 32, "dashed")
    cv.rect(base + 8, 34, base + 11, 52, "dashed")
    cv.rect(base + 13, 36, base + 18, 42, "dashed")
    cv.rect(base + 13, 44, base + 18, 50, "dashed")

    # ── the legend, which is where the notation is stated ───────────────────────────────────────────
    L = 74
    cv.put(0, L + 6, "Legend")
    rows = [("heavy", "Compilation Group"), ("light", "Compilation Unit"),
            ("light", "Source Unit"), ("dashed", "Source Element"),
            ("heavy", "Run Unit"), ("light", "Runtime Module"), ("dashed", "Runtime Element")]
    r = 2
    for style, name in rows:
        cv.rect(r, L, r + 2, L + 21, style)
        cv.put(r + 1, L + 2, name)
        r += 4
    return cv.render()


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    body = figure_d3()
    if not args.apply:
        print("\n".join(body))
        return 0

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    cap = next(i for i, l in enumerate(lines) if l.startswith("**Figure D.3 — "))
    end = next(i for i in range(cap + 1, len(lines))
               if lines[i].startswith("<a id=") or lines[i].startswith("**Figure "))

    def is_prose(l):
        s = l.strip()
        return s.endswith(".") and len(s.split()) > 6 and not s.startswith(("│", "<", "┃"))

    first = next((i for i in range(cap + 1, end) if is_prose(lines[i])), None)
    if first is not None:
        end = first                       # never swallow the standard's prose — see annex_d_flowcharts.splice
    lines[cap + 1:end] = ["", '<pre style="line-height:1">'] + body + ["</pre>", ""]
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Figure D.3: drawn ({len(body)} rows) — applied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
