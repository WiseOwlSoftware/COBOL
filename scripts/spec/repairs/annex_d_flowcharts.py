#!/usr/bin/env python3
"""Draw the Annex D VARYING flowcharts that were never drawn — Figures D.11 and D.13.

WHAT WAS THERE. D.11 was a rough arrow sketch (`→ Condition-1 → True → Exit`, `↑___________|`) and D.13 was
nothing but the chart's labels in reading order. Their siblings D.12 and D.14 ARE drawn, which is the odd
part — whoever drew these did alternate figures and stopped.

THE HOUSE STYLE IS ALREADY SETTLED, by Figure D.8: boxes with CENTRED text, connectors meeting a border at a
`┬`/`┴`/`├` junction rather than ending in a separate arrowhead, and a branch label sitting beside its
connector. Nothing new is invented here; these two are drawn to match what is already in the file.

GEOMETRY IS COMPUTED, NEVER COUNTED. Counting characters by eye is exactly what produced the defects in the
figures that WERE drawn — D.7 has 22 box walls in columns no rule reaches, and D.12's text spills past its own
border (`│  Set identifier-2 to cur- │` inside a 21-wide box). So a box here is sized from its content, every
connector is placed at a computed column, and `put` REFUSES to overwrite a non-blank cell. That guard caught
the first draft drawing its True branch straight through the decision's right wall and its loop line through
the bottom box's border — both of which would have rendered as a plausible-looking picture.

Printed originals: D.11 on folio 1155, D.13 on folio 1157. D.13 is D.11 with the test moved AFTER the body,
which is what TEST AFTER means, so the two differ only in where the decision sits.

    python scripts/spec/repairs/annex_d_flowcharts.py            # render both to stdout
    python scripts/spec/repairs/annex_d_flowcharts.py --apply    # splice into specs/ISO_COBOL.md
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

PAD = 1
AXIS = 30          # the column every box is centred on
LOOP = 4           # the loop-back line runs up this column


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}. "
                         f"A connector is being drawn through something already there.")
            self.cells[(r, c + k)] = ch

    def box(self, top, lines, rounded=False, enter=True, leave=True, width=None):
        """A box centred on AXIS, sized to its content. `enter`/`leave` cut the ┬/┴ the connectors meet."""
        inner = (width if width is not None else max(len(x) for x in lines)) + 2 * PAD
        left = AXIS - (inner + 2) // 2
        tl, tr, bl, br = ("╭", "╮", "╰", "╯") if rounded else ("┌", "┐", "└", "┘")
        mid = AXIS - left
        top_rule = ["─"] * inner
        bot_rule = ["─"] * inner
        if enter:
            top_rule[mid - 1] = "┴"
        if leave:
            bot_rule[mid - 1] = "┬"
        self.put(top, left, tl + "".join(top_rule) + tr)
        for k, text in enumerate(lines):
            self.put(top + 1 + k, left, "│" + text.center(inner) + "│")
        bottom = top + len(lines) + 1
        self.put(bottom, left, bl + "".join(bot_rule) + br)
        return Shape(top, bottom, left, left + inner + 1, top + 1 + (len(lines) - 1) // 2)

    def junction(self, r, c, ch, expect):
        """Deliberately upgrade a border glyph to a junction — `│` becomes `├` where a branch leaves a box.

        This is the ONE case where overwriting is correct, so it is a separate method: `put` must keep
        refusing, or a connector drawn through a wall by accident looks exactly like this on the page."""
        cur = self.cells.get((r, c))
        if cur != expect:
            sys.exit(f"FATAL: expected {expect!r} at row {r} col {c} to upgrade to {ch!r}, found {cur!r}")
        self.cells[(r, c)] = ch

    def vert(self, r0, r1, c):
        for r in range(r0 + 1, r1):
            self.put(r, c, "│")

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


class Shape:
    def __init__(self, top, bottom, left, right, mid):
        self.top, self.bottom, self.left, self.right, self.mid = top, bottom, left, right, mid


# Every box in these two charts is drawn to ONE width, as the printed originals are, so the connectors between
# them are vertical rather than stepped.
W = max(len(s) for s in ("Set identifier-2 equal to", "with current BY value", "set of statements"))


def decision_exit(cv, d, label_true="True", terminal="Exit"):
    """The True branch: out of the decision's right wall to a terminal, and the False label under it."""
    cv.junction(d.mid, d.right, "├", "│")
    cv.put(d.mid, d.right + 1, "─" * 4 + f" {label_true} " + "─" * 4 + "►")
    cv.put(d.mid, d.right + 15 + len(label_true), terminal)


def loop_back(cv, frm, to_row, join_col, join_from):
    """Down out of `frm`, left along the bottom, up the margin, and right into (to_row, join_col).

    The arrival is a real JUNCTION, not an arrowhead floating near the target: whatever is already at the join
    — a box's left wall, or the vertical connector between two boxes — is upgraded to `┤`, so the line is
    visibly attached. The first draft of D.13 ran its arrow to a box edge that was not on that row at all and
    left it pointing into blank space."""
    tail = frm.bottom + 2
    cv.vert(frm.bottom, tail, AXIS)
    cv.put(tail, LOOP, "└" + "─" * (AXIS - LOOP - 1) + "┘")
    cv.vert(to_row, tail, LOOP)
    cv.put(to_row, LOOP, "┌" + "─" * (join_col - LOOP - 1))
    cv.junction(to_row, join_col, "┤", join_from)


def figure_d11():
    """TEST BEFORE, one condition: the decision is tested before the body runs."""
    cv = Canvas()
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 equal to", "current FROM value"], enter=True, width=W)
    cv.vert(0, a.top, AXIS)
    d = cv.box(a.bottom + 3, ["Condition-1"], rounded=True, width=W)
    cv.vert(a.bottom, d.top, AXIS)
    decision_exit(cv, d)
    cv.put(d.bottom + 1, AXIS + 2, "False")
    b = cv.box(d.bottom + 3, ["Execute specified", "set of statements"], width=W)
    cv.vert(d.bottom, b.top, AXIS)
    c = cv.box(b.bottom + 3, ["Augment identifier-2", "with current BY value"], leave=True, width=W)
    cv.vert(b.bottom, c.top, AXIS)
    loop_back(cv, c, d.mid, d.left, '│')
    return cv.render()


def figure_d13():
    """TEST AFTER, one condition: the body runs first, and the loop re-enters ABOVE the body."""
    cv = Canvas()
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 equal to", "current FROM value"], width=W)
    cv.vert(0, a.top, AXIS)
    b = cv.box(a.bottom + 4, ["Execute specified", "set of statements"], width=W)
    cv.vert(a.bottom, b.top, AXIS)
    d = cv.box(b.bottom + 3, ["Condition-1"], rounded=True, width=W)
    cv.vert(b.bottom, d.top, AXIS)
    decision_exit(cv, d)
    cv.put(d.bottom + 1, AXIS + 2, "False")
    c = cv.box(d.bottom + 3, ["Augment identifier-2", "with current BY value"], width=W)
    cv.vert(d.bottom, c.top, AXIS)
    # The loop re-enters between the first box and the body, not at the decision.
    loop_back(cv, c, a.bottom + 2, AXIS, '│')
    return cv.render()


FIGURES = {"D.11": figure_d11, "D.13": figure_d13}


def splice(lines, num, body):
    """Replace whatever stands in for figure `num` with the drawn form."""
    cap = next(i for i, l in enumerate(lines) if l.startswith(f"**Figure {num} — "))
    end = next(i for i in range(cap + 1, len(lines))
               if lines[i].startswith("<a id=") or lines[i].startswith("**Figure "))
    repl = ["", '<pre style="line-height:1">'] + body + ["</pre>", ""]
    lines[cap + 1:end] = repl
    return len(repl), end - cap - 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    drawn = {num: fn() for num, fn in FIGURES.items()}
    if not args.apply:
        for num, body in drawn.items():
            print(f"=== Figure {num} ===")
            print("\n".join(body))
            print()
        return 0

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    for num, body in drawn.items():
        new, old = splice(lines, num, body)
        print(f"Figure {num}: {old} lines → {new}")
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
