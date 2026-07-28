#!/usr/bin/env python3
"""Draw the Annex D condition-evaluation flowcharts — Figures D.7, D.8, D.9.

THE FAMILY. Each is a chain of `Evaluate condition-N` boxes alternating with a rounded decision, where each
decision's yes/no exits either continue down the chain or leave for a RAIL that runs to one of the two
terminals, `Truth value is true` / `Truth value is false`. Only the decision wording and where each exit goes
differ between them, so one generator draws all three.

  D.7  condition-1 AND condition-2 AND … condition-n     decisions ask `false`; any yes ⇒ false
  D.8  condition-1 OR  condition-2 OR  … condition-n     decisions ask `true`;  any yes ⇒ true
  D.9  condition-1 OR condition-2 AND condition-3        mixed, and the only one using BOTH rails

WHY IT IS SAFE TO REGENERATE D.8, WHICH WAS ALREADY CORRECT. It is the test. D.8 is the one member of the
family that was drawn properly by hand, so the generator has to reproduce it CHARACTER FOR CHARACTER before
its output for the other two can be trusted — `--verify` asserts exactly that against the file. Without it,
this script would be an untested tool applied to figures nobody has checked.

D.7 was the ragged one: `│Condition-1│` is eleven characters inside a nine-wide box, its rail sits in a column
no rule reaches (22 such walls), and its arrows are `→` rather than box-drawing. D.9 was never drawn at all.

    python scripts/spec/repairs/annex_d_truth_charts.py --verify   # generator reproduces the printed D.8
    python scripts/spec/repairs/annex_d_truth_charts.py            # render all three
    python scripts/spec/repairs/annex_d_truth_charts.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

# Taken from the hand-drawn D.8, which the generator must reproduce exactly.
INNER = 21                 # width inside a box's walls
LEFT = 12                  # column of a box's left wall
AXIS = LEFT + INNER // 2 + 1
RAIL_R = 57                # the right-hand rail
RAIL_L = 6                 # the left-hand rail, used only by D.9. Far enough from the margin that
                           # its terminal label centres on it instead of being clipped at column 0.


def centre(text, width):
    """Centre with the ODD space on the right, which is what the hand-drawn D.8 does.

    `str.center` is not this function: for an odd margin it puts the extra space on the LEFT (its rule is
    `marg // 2 + (marg & width & 1)`), so every box came out one column off and the `--verify` check against
    D.8 failed on every text row."""
    pad = width - len(text)
    return " " * (pad // 2) + text + " " * (pad - pad // 2)


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}")
            self.cells[(r, c + k)] = ch

    def junction(self, r, c, ch, expect):
        """The one legitimate overwrite: a border glyph becoming a junction where a branch attaches."""
        cur = self.cells.get((r, c))
        if cur != expect:
            sys.exit(f"FATAL: expected {expect!r} at row {r} col {c}, found {cur!r}")
        self.cells[(r, c)] = ch

    def box(self, top, lines, rounded=False, enter=True, leave=True):
        tl, tr, bl, br = ("╭", "╮", "╰", "╯") if rounded else ("┌", "┐", "└", "┘")
        mid = AXIS - LEFT
        rule = lambda ch: "".join(ch if i == mid - 1 else "─" for i in range(INNER))
        self.put(top, LEFT, tl + (rule("┴") if enter else "─" * INNER) + tr)
        for k, text in enumerate(lines):
            self.put(top + 1 + k, LEFT, "│" + centre(text, INNER) + "│")
        bottom = top + len(lines) + 1
        self.put(bottom, LEFT, bl + (rule("┬") if leave else "─" * INNER) + br)
        return top, bottom

    def vert(self, r0, r1, c):
        for r in range(r0 + 1, r1):
            self.put(r, c, "│")

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


def chart(steps, down_terminal, right_terminal, left_terminal=None):
    """steps: (condition-name, decision-word, yes-target, no-target); target ∈ right | left | down."""
    cv = Canvas()
    row = 0
    rail_r_from = rail_l_from = None
    for n, (cond, word, yes, no) in enumerate(steps):
        _, ebot = cv.box(row, ["Evaluate", cond], enter=(n > 0))
        dtop, dbot = cv.box(ebot + 2, [f"{cond[0].upper() + cond[1:]}", word], rounded=True)
        cv.vert(ebot, dtop, AXIS)
        mid = dtop + 1                                   # the decision's first text row
        if yes == "right":
            cv.junction(mid, LEFT + INNER + 1, "├", "│")
            cv.put(mid, LEFT + INNER + 2, "─" * (RAIL_R - LEFT - INNER - 2))
            cv.put(mid - 1, LEFT + INNER + 6, "yes")
            cv.put(mid, RAIL_R, "┐" if rail_r_from is None else "┤")
            if rail_r_from is not None:
                cv.vert(rail_r_from, mid, RAIL_R)
            rail_r_from = mid
        elif yes == "left":
            cv.junction(mid, LEFT, "┤", "│")
            cv.put(mid, RAIL_L + 1, "─" * (LEFT - RAIL_L - 1))
            # Sits in the gap between the rail and the box, never over either.
            cv.put(mid - 1, LEFT - 4, "yes")
            cv.put(mid, RAIL_L, "┌" if rail_l_from is None else "├")
            if rail_l_from is not None:
                cv.vert(rail_l_from, mid, RAIL_L)
            rail_l_from = mid
        if no == "down":
            cv.vert(dbot, dbot + 2, AXIS)          # the connector the `no` label sits beside
            cv.put(dbot + 1, AXIS + 2, "no")
            row = dbot + 2
        elif no == "right":
            cv.put(dbot + 1, AXIS + 2, "no")
            cv.vert(dbot, dbot + 2, AXIS)
            cv.put(dbot + 2, AXIS, "└" + "─" * (RAIL_R - AXIS - 1) + "┤")
            cv.vert(rail_r_from, dbot + 2, RAIL_R)
            rail_r_from = dbot + 2
            row = dbot + 3
    # The last `no` row IS the row the arrowhead sits on; an extra blank row here made the chart a row
    # taller than the hand-drawn D.8 and broke the character-for-character check.
    end = row
    if down_terminal:
        cv.vert(row - 2, end, AXIS)
        cv.put(end, AXIS, "▼")
        for k, line in enumerate(down_terminal):
            cv.put(end + 1 + k, AXIS - len(line) // 2, line)
    if rail_r_from is not None:
        cv.vert(rail_r_from, end, RAIL_R)
        cv.put(end, RAIL_R, "▼")
        for k, line in enumerate(right_terminal):
            cv.put(end + 1 + k, RAIL_R - len(line) // 2, line)
    if rail_l_from is not None and left_terminal:
        cv.vert(rail_l_from, end, RAIL_L)
        cv.put(end, RAIL_L, "▼")
        for k, line in enumerate(left_terminal):
            cv.put(end + 1 + k, max(0, RAIL_L - len(line) // 2), line)
    return cv.render()


TRUE = ["Truth value", "is true"]
FALSE = ["Truth value", "is false"]


def d7():
    steps = [(f"condition-{n}", "false", "right", "down") for n in ("1", "2", "n")]
    return chart(steps, TRUE, FALSE)


def d8():
    steps = [(f"condition-{n}", "true", "right", "down") for n in ("1", "2", "n")]
    return chart(steps, FALSE, TRUE)


def d9():
    steps = [("condition-1", "true", "right", "down"),
             ("condition-2", "false", "left", "down"),
             ("condition-3", "false", "left", "right")]
    return chart(steps, None, TRUE, FALSE)


FIGURES = {"D.7": d7, "D.8": d8, "D.9": d9}


def body_of(lines, num):
    cap = next(i for i, l in enumerate(lines) if l.startswith(f"**Figure {num} — "))
    start = next((i for i in range(cap, cap + 12) if lines[i].strip().startswith("<pre")), None)
    if start is None:
        return cap, None, None
    end = next(i for i in range(start, len(lines)) if lines[i].strip() == "</pre>")
    return cap, start, end


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--verify", action="store_true", help="assert the generator reproduces the printed D.8")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    if args.verify:
        _, s, e = body_of(lines, "D.8")
        have = [l.rstrip() for l in lines[s + 1:e]]
        want = d8()
        diff = [k for k in range(max(len(have), len(want)))
                if (have[k] if k < len(have) else None) != (want[k] if k < len(want) else None)]
        # ONE KNOWN CORRECTION. In the hand-drawn D.8 the right terminal's two lines are centred on DIFFERENT
        # columns — `Truth value` on 57, `is true` on 56 — so the second line sits visibly askew under its own
        # arrowhead. The generator centres both on the rail, which changes that one row. Everything else has
        # to match character for character, and this fails if anything else does.
        known = {len(want) - 1}
        unexpected = [k for k in diff if k not in known]
        for k in diff:
            h = have[k] if k < len(have) else "<missing>"
            w = want[k] if k < len(want) else "<missing>"
            tag = "CORRECTED" if k in known else "UNEXPECTED"
            print(f"   row {k} [{tag}]")
            print(f"     file: {h!r}")
            print(f"     gen : {w!r}")
        if unexpected:
            print(f"\n✗ {len(unexpected)} unexpected difference(s) — the generator does not match D.8")
            return 1
        print(f"\n✓ reproduces the hand-drawn D.8 in {len(want) - len(diff)} of {len(want)} rows. The only "
              f"change is the right terminal's second line, which the original left one column off from its "
              f"own first line.")
        return 0

    drawn = {n: fn() for n, fn in FIGURES.items()}
    if not args.apply:
        for n, body in drawn.items():
            print(f"=== Figure {n} ===")
            print("\n".join(body))
            print()
        return 0

    for num in ("D.9", "D.8", "D.7"):                    # bottom-up so indices stay valid
        cap, s, e = body_of(lines, num)
        repl = ["", '<pre style="line-height:1">'] + drawn[num] + ["</pre>", ""]
        end = e + 1 if s is not None else next(
            i for i in range(cap + 1, len(lines)) if lines[i].startswith("<a id="))
        lines[cap + 1:end] = repl
        print(f"Figure {num}: redrawn ({len(drawn[num])} rows)")
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
