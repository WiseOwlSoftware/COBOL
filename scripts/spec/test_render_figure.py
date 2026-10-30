#!/usr/bin/env python3
"""Self-test for render_figure.py's brace-group layout: every shape the row and span rules handle, one case each.
Run: python scripts/spec/test_render_figure.py   (every gate and CI run it through scripts/self_tests.py.)

WHY MEASURED FIXTURES. The generator's input is the licensed PDF, which lives in a private submodule that CI and a
fresh worktree do not have. `build` takes its measurement functions as arguments, so each case below replays the
words and stems measured from one printed figure (folio and band named on the case) through the real layout code
and compares the drawing with the one verified against the printed page. A layout change that moves any of these
figures fails here without the PDF; `sweep_figures.py --check` remains the whole-standard net where the PDF is.

THE SHAPES (FIGURE-STYLE.md, "Layout rules, for the generator"; kb/Work PB3530):
  SIBLING_BRACE_BESIDE_A_TALLER_ONE          — §13.18.35.2 Format 1: `LINE NUMBER IS` / `LINES ARE` are alternatives
                                               of the three-way brace beside a taller one, never labels to snap.
  HELD_WORD_CLUSTERED_ACROSS_ITS_HOOK        — `FOR ALPHANUMERIC` clusters with `CODE-SET` onto a row whose average
                                               lies above its own brace's hook; the WHOLE row moves inside, so
                                               `CODE-SET` stays on the point row it labels (review R1-1).
  NEXT_LINE_BESIDE_A_TALLER_BRACE            — `THROUGH`, 3.9 pt below `identifier-3` and within the row width, is
                                               the next printed line: the step ends the row (LINE_GAP).
  NEXT_LINE_AT_THE_NARROWEST_GAP             — the `…` 3.6 pt below `object-class-name-1`, the narrowest step
                                               that is a new line, repeats the argument INSIDE the parentheses.
  ENCLOSURE_LABEL_BETWEEN_INNER_ALTERNATIVES — `ASSIGN`, the label of the enclosing brace, snaps rather than
                                               subdividing `{ device-name-1 / literal-1 }`.
  BOUNDARY_OF_A_GROUP_ALONGSIDE              — `condition-1` sits 2.2 pt above the `{ BEFORE / AFTER }` hook beside
                                               it; nothing that brace holds disagrees, so the row is not torn.
  CHOICE_BARS_BESIDE_A_TALLER_BRACE          — WRITE's `{ | BEFORE / AFTER | }` bars stop at the last row they hold.
  LABEL_ON_ITS_OWN_POINT_ROW                 — ACCEPT Format 3's `LINE NUMBER` stays on its own brace's point row.
"""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import render_figure as R  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

# name: (measured words as (text, x0, x1, y0, underlined), measured stems as (x, y0, y1, kind, side), the drawing)
FIGURES = {
    # folio 420 (PDF page 450), band y 259-352
    'SIBLING_BRACE_BESIDE_A_TALLER_ONE': (
        [
            ('ì', 192.8, 198.0, 265.1, False),
            ('ü', 343.0, 348.3, 265.1, False),
            ('integer-1', 204.7, 246.8, 273.0, False),
            ('[', 249.1, 252.9, 273.0, False),
            ('ON', 255.2, 269.5, 273.0, False),
            ('NEXT', 272.6, 298.4, 273.0, True),
            ('PAGE', 302.3, 327.7, 273.0, True),
            (']', 330.8, 334.5, 273.0, False),
            ('ê', 192.8, 198.0, 274.3, False),
            ('ê', 343.0, 348.3, 274.3, False),
            ('ì', 70.0, 75.3, 283.4, False),
            ('ü', 183.1, 188.4, 283.4, False),
            ('ê', 192.8, 198.0, 283.5, False),
            ('ê', 343.0, 348.3, 283.5, False),
            ('LINE', 81.9, 104.5, 284.1, True),
            ('NUMBER', 107.6, 149.8, 284.1, False),
            ('IS', 152.2, 160.9, 284.1, False),
            ('ê', 70.0, 75.3, 292.6, False),
            ('ê', 183.1, 188.4, 292.6, False),
            ('ê', 192.8, 198.0, 292.6, False),
            ('ê', 343.0, 348.3, 292.6, False),
            ('ì', 204.6, 209.9, 294.3, False),
            ('ü', 246.6, 251.9, 294.3, False),
            ('PLUS', 216.4, 240.4, 296.1, True),
            ('...', 355.4, 362.0, 299.2, False),
            ('LINE', 81.9, 104.5, 301.4, True),
            ('NUMBERS', 107.6, 155.1, 301.4, False),
            ('ARE', 157.4, 176.9, 301.4, False),
            ('í', 70.0, 75.3, 301.8, False),
            ('ý', 183.1, 188.4, 301.8, False),
            ('í', 192.8, 198.0, 301.8, False),
            ('ý', 343.0, 348.3, 301.8, False),
            ('integer-2', 254.4, 296.5, 302.6, False),
            ('í', 204.6, 209.9, 303.5, False),
            ('ý', 246.6, 251.9, 303.5, False),
            ('ê', 70.0, 75.3, 310.9, False),
            ('ê', 183.1, 188.4, 310.9, False),
            ('ê', 192.8, 198.0, 311.0, False),
            ('ê', 343.0, 348.3, 311.0, False),
            ('î', 204.6, 209.9, 312.7, False),
            ('þ', 246.6, 251.9, 312.7, False),
            ('+', 216.4, 222.3, 313.4, False),
            ('LINES', 81.9, 109.8, 318.7, True),
            ('ARE', 112.9, 132.3, 318.7, False),
            ('î', 70.0, 75.3, 320.1, False),
            ('þ', 183.1, 188.4, 320.1, False),
            ('ê', 192.8, 198.0, 320.1, False),
            ('ê', 343.0, 348.3, 320.1, False),
            ('ê', 192.8, 198.0, 329.3, False),
            ('ê', 343.0, 348.3, 329.3, False),
            ('ON', 204.6, 218.9, 329.8, False),
            ('NEXT', 221.9, 247.8, 329.8, True),
            ('PAGE', 251.7, 277.1, 329.8, True),
            ('î', 192.8, 198.0, 338.5, False),
            ('þ', 343.0, 348.3, 338.5, False),
        ],
        [
        ],
        [
            '                     ╭ integer-1 [ ON NEXT PAGE ] ╮',
            '╭ LINE NUMBER IS   ╮ │                            │',
            '│                  │ │ ╭ PLUS ╮                   │ …',
            '┤ LINE NUMBERS ARE ├ ┤ ┤      ├ integer-2         ├',
            '│                  │ │ ╰ +    ╯                   │',
            '╰ LINES ARE        ╯ │                            │',
            '                     ╰ ON NEXT PAGE               ╯',
        ],
    ),
    # folio 619 (PDF page 649), band y 121-174
    'NEXT_LINE_BESIDE_A_TALLER_BRACE': (
        [
            ('ì', 86.5, 91.8, 126.7, False),
            ('ü', 214.8, 220.1, 126.7, False),
            ('ì', 300.4, 305.7, 126.7, False),
            ('ü', 428.7, 433.9, 126.7, False),
            ('identifier-3', 98.4, 150.4, 132.2, False),
            ('identifier-4', 312.2, 364.2, 132.2, False),
            ('ê', 86.5, 91.8, 135.9, False),
            ('ê', 214.8, 220.1, 135.9, False),
            ('ì', 224.6, 229.9, 135.9, False),
            ('ü', 290.7, 295.9, 135.9, False),
            ('ê', 300.4, 305.7, 135.9, False),
            ('ê', 428.7, 433.9, 135.9, False),
            ('THROUGH', 236.4, 284.5, 136.1, True),
            ('[', 438.4, 442.2, 144.2, False),
            ('IN', 444.5, 455.3, 144.2, False),
            ('alphabet-name-1', 457.6, 535.7, 144.2, False),
            (']', 538.1, 541.8, 144.2, False),
            ('í', 86.5, 91.8, 145.1, False),
            ('ý', 214.8, 220.1, 145.1, False),
            ('í', 224.6, 229.9, 145.1, False),
            ('ý', 290.7, 295.9, 145.1, False),
            ('í', 300.4, 305.7, 145.1, False),
            ('ý', 428.7, 433.9, 145.1, False),
            ('literal-3', 98.4, 135.1, 146.3, False),
            ('literal-4', 312.2, 348.9, 146.3, False),
            ('THRU', 236.4, 263.6, 153.4, True),
            ('ê', 86.5, 91.8, 154.2, False),
            ('ê', 214.8, 220.1, 154.2, False),
            ('î', 224.6, 229.9, 154.2, False),
            ('þ', 290.7, 295.9, 154.2, False),
            ('ê', 300.4, 305.7, 154.2, False),
            ('ê', 428.7, 433.9, 154.2, False),
            ('arithmetic-expression-3', 98.4, 208.6, 160.4, False),
            ('arithmetic-expression-4', 312.2, 422.5, 160.4, False),
            ('î', 86.5, 91.8, 163.4, False),
            ('þ', 214.8, 220.1, 163.4, False),
            ('î', 300.4, 305.7, 163.4, False),
            ('þ', 428.7, 433.9, 163.4, False),
        ],
        [
        ],
        [
            '╭ identifier-3            ╮             ╭ identifier-4            ╮',
            '│                         │ ╭ THROUGH ╮ │                         │',
            '┤ literal-3               ├ ┤         ├ ┤ literal-4               ├ [ IN alphabet-name-1 ]',
            '│                         │ ╰ THRU    ╯ │                         │',
            '╰ arithmetic-expression-3 ╯             ╰ arithmetic-expression-4 ╯',
        ],
    ),
    # folio 312 (PDF page 342), band y 170-235
    'ENCLOSURE_LABEL_BETWEEN_INNER_ALTERNATIVES': (
        [
            ('ì', 115.2, 120.5, 175.0, False),
            ('ü', 370.2, 375.5, 175.0, False),
            ('ê', 115.2, 120.5, 184.2, False),
            ('ê', 370.2, 375.5, 184.2, False),
            ('ì', 151.1, 156.4, 188.1, False),
            ('ü', 236.8, 242.1, 188.1, False),
            ('device-name-1', 163.0, 230.7, 192.2, False),
            ('ê', 115.2, 120.5, 193.3, False),
            ('ê', 370.2, 375.5, 193.3, False),
            ('TO', 132.7, 146.0, 196.3, False),
            ('...', 246.6, 253.2, 196.3, False),
            ('[', 255.5, 259.3, 196.3, False),
            ('USING', 262.4, 291.9, 196.3, True),
            ('data-name-1', 295.0, 353.2, 196.3, False),
            (']', 355.6, 359.3, 196.3, False),
            ('í', 151.1, 156.4, 197.2, False),
            ('ý', 236.8, 242.1, 197.2, False),
            ('ASSIGN', 75.7, 110.2, 201.6, True),
            ('í', 115.2, 120.5, 202.5, False),
            ('ý', 370.2, 375.5, 202.5, False),
            ('literal-1', 162.9, 199.6, 206.3, False),
            ('î', 151.1, 156.4, 206.4, False),
            ('þ', 236.8, 242.1, 206.4, False),
            ('ê', 115.2, 120.5, 211.7, False),
            ('ê', 370.2, 375.5, 211.7, False),
            ('ê', 115.2, 120.5, 220.8, False),
            ('ê', 370.2, 375.5, 220.8, False),
            ('USING', 132.5, 162.0, 226.0, True),
            ('data-name-1', 165.1, 223.4, 226.0, False),
            ('î', 115.2, 120.5, 230.0, False),
            ('þ', 370.2, 375.5, 230.0, False),
        ],
        [
        ],
        [
            '       ╭    ╭ device-name-1 ╮                         ╮',
            '       │ TO ┤               ├ … [ USING data-name-1 ] │',
            'ASSIGN ┤    ╰ literal-1     ╯                         ├',
            '       │                                              │',
            '       ╰ USING data-name-1                            ╯',
        ],
    ),
    # folio 683 (PDF page 713), band y 199-238
    'BOUNDARY_OF_A_GROUP_ALONGSIDE': (
        [
            ('condition-1', 270.6, 323.6, 205.0, False),
            ('ì', 259.7, 265.0, 205.4, False),
            ('ü', 325.1, 330.4, 205.4, False),
            ('ì', 150.9, 156.2, 207.1, False),
            ('ü', 207.1, 212.4, 207.1, False),
            ('BEFORE', 162.8, 200.9, 207.2, True),
            ('UNTIL', 227.6, 257.4, 213.6, True),
            ('í', 259.7, 265.0, 214.6, False),
            ('ý', 325.1, 330.4, 214.6, False),
            ('WITH', 91.6, 118.6, 215.3, False),
            ('TEST', 121.7, 145.8, 215.3, True),
            ('í', 150.9, 156.2, 216.3, False),
            ('ý', 207.1, 212.4, 216.3, False),
            ('EXIT', 282.6, 306.9, 220.0, True),
            ('î', 259.7, 265.0, 223.7, False),
            ('þ', 325.1, 330.4, 223.7, False),
            ('AFTER', 162.8, 194.3, 224.6, True),
            ('î', 150.9, 156.2, 225.4, False),
            ('þ', 207.1, 212.4, 225.4, False),
        ],
        [
            (82.13, 202.24, 241.92, 'bracket', 'L'),
            (219.85, 202.24, 241.92, 'bracket', 'R'),
        ],
        [
            '┌           ╭ BEFORE ╮ ┐       ╭ condition-1 ╮',
            '│ WITH TEST ┤        ├ │ UNTIL ┤             ├',
            '│           │        │ │       │ EXIT        │',
            '└           ╰ AFTER  ╯ ┘       ╰             ╯',
        ],
    ),
    # folio 785 (PDF page 815), band y 295-380
    'CHOICE_BARS_BESIDE_A_TALLER_BRACE': (
        [
            ('ì', 241.6, 246.9, 302.5, False),
            ('ü', 383.2, 388.4, 302.5, False),
            ('ê', 241.6, 246.9, 311.7, False),
            ('ê', 383.2, 388.4, 311.7, False),
            ('ì', 253.4, 258.7, 312.0, False),
            ('ü', 323.4, 328.7, 312.0, False),
            ('identifier-2', 265.2, 317.2, 315.3, False),
            ('LINE', 339.4, 362.0, 315.3, False),
            ('ê', 241.6, 246.9, 320.8, False),
            ('ê', 383.2, 388.4, 320.8, False),
            ('í', 253.4, 258.7, 321.1, False),
            ('ý', 323.4, 328.7, 321.1, False),
            ('integer-1', 265.2, 307.4, 329.4, False),
            ('LINES', 339.4, 367.3, 329.4, False),
            ('ì', 107.1, 112.4, 330.0, False),
            ('ü', 169.4, 174.6, 330.0, False),
            ('ê', 241.6, 246.9, 330.0, False),
            ('ê', 383.2, 388.4, 330.0, False),
            ('BEFORE', 122.0, 160.1, 330.1, True),
            ('î', 253.4, 258.7, 330.3, False),
            ('þ', 323.4, 328.7, 330.3, False),
            ('ADVANCING', 179.1, 236.5, 338.3, False),
            ('í', 107.1, 112.4, 339.2, False),
            ('ý', 169.4, 174.6, 339.2, False),
            ('í', 241.6, 246.9, 339.2, False),
            ('ý', 383.2, 388.4, 339.2, False),
            ('AFTER', 122.0, 153.5, 347.5, True),
            ('î', 107.1, 112.4, 348.3, False),
            ('þ', 169.4, 174.6, 348.3, False),
            ('ê', 241.6, 246.9, 348.3, False),
            ('ê', 383.2, 388.4, 348.3, False),
            ('ì', 253.4, 258.7, 351.8, False),
            ('ü', 358.0, 363.2, 351.8, False),
            ('mnemonic-name-1', 265.2, 351.8, 353.5, False),
            ('ê', 241.6, 246.9, 357.5, False),
            ('ê', 383.2, 388.4, 357.5, False),
            ('í', 253.4, 258.7, 360.9, False),
            ('ý', 358.0, 363.2, 360.9, False),
            ('ê', 241.6, 246.9, 366.7, False),
            ('ê', 383.2, 388.4, 366.7, False),
            ('PAGE', 265.2, 290.6, 367.6, True),
            ('î', 253.4, 258.7, 370.1, False),
            ('þ', 358.0, 363.2, 370.1, False),
            ('î', 241.6, 246.9, 375.8, False),
            ('þ', 383.2, 388.4, 375.8, False),
        ],
        [
            (330.03, 313.35, 343.93, 'bracket', 'L'),
            (375.55, 313.35, 343.93, 'bracket', 'R'),
            (114.34, 326.89, 364.47, 'bar', None),
            (165.93, 326.89, 364.47, 'bar', None),
        ],
        [
            '                         ╭ ╭ identifier-2 ╮ ┌ LINE  ┐ ╮',
            '                         │ ┤              ├ │       │ │',
            '╭ │ BEFORE │ ╮           │ ╰ integer-1    ╯ └ LINES ┘ │',
            '┤ │        │ ├ ADVANCING ┤                            ├',
            '╰ │ AFTER  │ ╯           │                            │',
            '                         │ ╭ mnemonic-name-1 ╮        │',
            '                         │ ┤                 ├        │',
            '                         ╰ ╰ PAGE            ╯        ╯',
        ],
    ),
    # folio 577 (PDF page 607), band y 116.8-315.2
    'LABEL_ON_ITS_OWN_POINT_ROW': (
        [
            ('ACCEPT', 81.0, 118.3, 122.8, True),
            ('screen-name-1', 121.4, 189.8, 122.8, False),
            ('ì', 115.7, 121.0, 149.2, False),
            ('ü', 341.5, 346.7, 149.2, False),
            ('ê', 115.7, 121.0, 158.4, False),
            ('ê', 341.5, 346.7, 158.4, False),
            ('ì', 209.3, 214.6, 166.7, False),
            ('ü', 279.3, 284.6, 166.7, False),
            ('ê', 115.7, 121.0, 167.5, False),
            ('ê', 341.5, 346.7, 167.5, False),
            ('identifier-3', 221.1, 273.1, 170.1, False),
            ('LINE', 136.3, 158.9, 175.0, True),
            ('NUMBER', 162.0, 204.2, 175.0, False),
            ('í', 209.3, 214.6, 175.9, False),
            ('ý', 279.3, 284.6, 175.9, False),
            ('ê', 115.7, 121.0, 176.7, False),
            ('ê', 341.5, 346.7, 176.7, False),
            ('integer-1', 221.1, 263.3, 184.2, False),
            ('î', 209.3, 214.6, 185.1, False),
            ('þ', 279.3, 284.6, 185.1, False),
            ('ê', 115.7, 121.0, 185.9, False),
            ('ê', 341.5, 346.7, 185.9, False),
            ('AT', 97.7, 110.7, 194.1, False),
            ('í', 115.7, 121.0, 195.0, False),
            ('ý', 341.5, 346.7, 195.0, False),
            ('ê', 115.7, 121.0, 204.2, False),
            ('ê', 341.5, 346.7, 204.2, False),
            ('ê', 115.7, 121.0, 213.4, False),
            ('ê', 341.5, 346.7, 213.4, False),
            ('ì', 136.2, 141.5, 214.0, False),
            ('ü', 195.9, 201.1, 214.0, False),
            ('ì', 252.9, 258.2, 214.0, False),
            ('ü', 322.9, 328.2, 214.0, False),
            ('COLUMN', 148.0, 189.7, 214.2, True),
            ('identifier-4', 264.8, 316.8, 217.4, False),
            ('NUMBER', 205.6, 247.8, 222.3, False),
            ('ê', 115.7, 121.0, 222.5, False),
            ('ê', 341.5, 346.7, 222.5, False),
            ('í', 136.2, 141.5, 223.2, False),
            ('ý', 195.9, 201.1, 223.2, False),
            ('í', 252.9, 258.2, 223.2, False),
            ('ý', 322.9, 328.2, 223.2, False),
            ('COL', 148.0, 166.8, 231.5, True),
            ('integer-2', 264.8, 306.9, 231.5, False),
            ('ê', 115.7, 121.0, 231.7, False),
            ('ê', 341.5, 346.7, 231.7, False),
            ('î', 136.2, 141.5, 232.4, False),
            ('þ', 195.9, 201.1, 232.4, False),
            ('î', 252.9, 258.2, 232.4, False),
            ('þ', 322.9, 328.2, 232.4, False),
            ('î', 115.7, 121.0, 240.9, False),
            ('þ', 341.5, 346.7, 240.9, False),
            ('ON', 103.3, 117.6, 264.9, False),
            ('EXCEPTION', 120.7, 175.2, 264.9, True),
            ('imperative-statement-1', 178.3, 287.0, 264.9, False),
            ('NOT', 103.3, 123.9, 282.2, True),
            ('ON', 127.0, 141.2, 282.2, False),
            ('EXCEPTION', 144.3, 198.9, 282.2, True),
            ('imperative-statement-2', 202.0, 310.7, 282.2, False),
            ('[', 89.5, 93.3, 301.2, False),
            ('END-ACCEPT', 96.4, 157.7, 301.2, True),
            (']', 160.8, 164.5, 301.2, False),
        ],
        [
            (92.16, 143.64, 256.86, 'bracket', 'L'),
            (348.01, 143.64, 256.86, 'bracket', 'R'),
            (122.98, 154.38, 251.72, 'bar', None),
            (337.97, 154.38, 251.72, 'bar', None),
            (95.9, 261.76, 299.11, 'bar', None),
            (316.73, 261.76, 299.11, 'bar', None),
            (90.76, 265.5, 297.24, 'bracket', 'L'),
            (322.1, 265.5, 297.24, 'bracket', 'R'),
        ],
        [
            'ACCEPT screen-name-1',
            '',
            '┌    ╭ │             ╭ identifier-3 ╮       │ ╮ ┐',
            '│    │ │ LINE NUMBER ┤              ├       │ │ │',
            '│    │ │             ╰ integer-1    ╯       │ │ │',
            '│ AT ┤ │                                    │ ├ │',
            '│    │ │ ╭ COLUMN ╮        ╭ identifier-4 ╮ │ │ │',
            '│    │ │ ┤        ├ NUMBER ┤              ├ │ │ │',
            '└    ╰ │ ╰ COL    ╯        ╰ integer-2    ╯ │ ╯ ┘',
            '',
            '┌ │ ON EXCEPTION imperative-statement-1     │ ┐',
            '│ │                                         │ │',
            '└ │ NOT ON EXCEPTION imperative-statement-2 │ ┘',
            '',
            '[ END-ACCEPT ]',
        ],
    ),
    # folio 342 (PDF page 372), band y 500-575
    'HELD_WORD_CLUSTERED_ACROSS_ITS_HOOK': (
        [
            ('ì', 128.0, 133.3, 513.9, False),
            ('ü', 369.5, 374.8, 513.9, False),
            ('IS', 140.0, 148.7, 522.7, False),
            ('alphabet-name-1', 151.1, 229.2, 522.7, False),
            ('[', 231.5, 235.3, 522.7, False),
            ('alphabet-name-2]', 237.6, 319.5, 522.7, False),
            ('ê', 128.0, 133.3, 523.1, False),
            ('ê', 369.5, 374.8, 523.1, False),
            ('ê', 128.0, 133.3, 532.3, False),
            ('ê', 369.5, 374.8, 532.3, False),
            ('.', 381.9, 384.1, 538.8, False),
            ('CODE-SET', 75.5, 123.1, 540.5, True),
            ('í', 128.0, 133.3, 541.5, False),
            ('ý', 369.5, 374.8, 541.5, False),
            ('ì', 139.8, 145.1, 541.8, False),
            ('ü', 358.6, 363.9, 541.8, False),
            ('FOR', 154.8, 175.6, 542.0, False),
            ('ALPHANUMERIC', 179.5, 257.1, 542.0, True),
            ('IS', 260.2, 269.0, 542.0, False),
            ('alphabet-name-1', 271.3, 349.4, 542.0, False),
            ('ê', 128.0, 133.3, 550.6, False),
            ('ê', 369.5, 374.8, 550.6, False),
            ('í', 139.8, 145.1, 551.0, False),
            ('ý', 358.6, 363.9, 551.0, False),
            ('FOR', 154.7, 174.1, 559.3, False),
            ('NATIONAL', 177.2, 227.6, 559.3, True),
            ('IS', 230.7, 239.4, 559.3, False),
            ('alphabet-name-2', 241.8, 319.9, 559.3, False),
            ('ê', 128.0, 133.3, 559.8, False),
            ('ê', 369.5, 374.8, 559.8, False),
            ('î', 139.8, 145.1, 560.1, False),
            ('þ', 358.6, 363.9, 560.1, False),
            ('î', 128.0, 133.3, 569.0, False),
            ('þ', 369.5, 374.8, 569.0, False),
        ],
        [
            (72.56, 509.20, 585.53, 'bracket', 'L'),
            (376.25, 509.20, 585.53, 'bracket', 'R'),
            (147.49, 538.61, 576.20, 'bar', None),
            (355.48, 538.61, 576.20, 'bar', None),
        ],
        [
            '┌          ╭ IS alphabet-name-1 [ alphabet-name-2]       ╮ ┐',
            '│          │                                             │ │',
            '│ CODE-SET ┤ ╭ │ FOR ALPHANUMERIC IS alphabet-name-1 │ ╮ ├ │ .',
            '│          │ ┤ │                                     │ ├ │ │',
            '└          ╰ ╰ │ FOR NATIONAL IS alphabet-name-2     │ ╯ ╯ ┘',
        ],
    ),
    # folio 133 (PDF page 163), band y 328.9-422.3
    'NEXT_LINE_AT_THE_NARROWEST_GAP': (
        [
            ('ì', 285.9, 291.2, 334.9, False),
            ('ü', 414.2, 419.5, 334.9, False),
            ('arithmetic-expression-1', 297.8, 408.1, 343.8, False),
            ('ê', 285.9, 291.2, 344.1, False),
            ('ê', 414.2, 419.5, 344.1, False),
            ('ê', 285.9, 291.2, 353.3, False),
            ('ê', 414.2, 419.5, 353.3, False),
            ('boolean-expression-1', 297.8, 397.0, 357.9, False),
            ('ì', 92.2, 97.5, 362.4, False),
            ('ü', 202.0, 207.3, 362.4, False),
            ('ê', 285.9, 291.2, 362.4, False),
            ('ê', 414.2, 419.5, 362.4, False),
            ('object-class-name-1', 104.0, 195.8, 366.5, False),
            ('¼', 424.0, 434.7, 370.1, False),
            ('::', 211.7, 217.4, 370.7, False),
            ('literal-1', 219.7, 256.4, 370.7, False),
            ('(', 276.8, 280.9, 370.7, False),
            (')', 439.8, 443.9, 370.7, False),
            ('í', 92.2, 97.5, 371.6, False),
            ('ý', 202.0, 207.3, 371.6, False),
            ('í', 285.9, 291.2, 371.6, False),
            ('ý', 414.2, 419.5, 371.6, False),
            ('identifier-2', 297.7, 349.7, 372.0, False),
            ('identifier-1', 104.0, 156.0, 380.6, False),
            ('î', 92.2, 97.5, 380.8, False),
            ('þ', 202.0, 207.3, 380.8, False),
            ('ê', 285.9, 291.2, 380.8, False),
            ('ê', 414.2, 419.5, 380.8, False),
            ('literal-2', 297.7, 334.4, 386.1, False),
            ('ê', 285.9, 291.2, 390.0, False),
            ('ê', 414.2, 419.5, 390.0, False),
            ('ê', 285.9, 291.2, 399.1, False),
            ('ê', 414.2, 419.5, 399.1, False),
            ('OMITTED', 297.7, 342.7, 400.2, True),
            ('î', 285.9, 291.2, 408.3, False),
            ('þ', 414.2, 419.5, 408.3, False),
        ],
        [
            (267.24, 329.92, 424.70, 'bracket', 'L'),
            (452.12, 329.92, 424.70, 'bracket', 'R'),
        ],
        [
            '                                     ┌   ╭ arithmetic-expression-1 ╮     ┐',
            '                                     │   │                         │     │',
            '                                     │   │ boolean-expression-1    │     │',
            '╭ object-class-name-1 ╮              │   │                         │     │',
            '┤                     ├ :: literal-1 │ ( ┤ identifier-2            ├ … ) │',
            '╰ identifier-1        ╯              │   │                         │     │',
            '                                     │   │ literal-2               │     │',
            '                                     │   │                         │     │',
            '                                     └   ╰ OMITTED                 ╯     ┘',
        ],
    ),
}


def draw(words, stems):
    """Lay one measured figure out through the real `build`, replaying the measurement instead of the PDF."""
    ws = [{"text": t, "x0": x0, "x1": x1, "y0": y0, "underlined": u} for t, x0, x1, y0, u in words]
    ss = [{"x": x, "y0": y0, "y1": y1, "h": y1 - y0, "kind": k, "side": sd} for x, y0, y1, k, sd in stems]
    lo, hi = min(w["y0"] for w in ws), max(w["y0"] for w in ws)
    grid, marks = R.build(None, lo, hi, lambda _page: ws, lambda _page: ss)
    return R.render(grid, marks, True)


def piece(text, x, y):
    return {"text": text, "x0": x, "x1": x + 5.0, "y0": y, "underlined": False}


UNIT_CHECKS = 10                      # the checks unit_failures makes, so the verdict counts every one


def unit_failures():
    """The mechanisms underneath the figures, each held on its own."""
    fails = []

    # Two braces stacked in ONE column are two delimiters, cut at the second top hook.
    stacked = [piece("ì", 10, 0), piece("í", 10, 9), piece("î", 10, 18),
               piece("ì", 10, 40), piece("í", 10, 49), piece("î", 10, 58)]
    groups = R.split_delimiters(stacked)
    if [len(g) for g in groups] != [3, 3]:
        fails.append(f"split_delimiters: two stacked braces split as {[len(g) for g in groups]}, want [3, 3]")

    def brace(x, y0, y1, side):
        t, m, b = ("ì", "í", "î") if side == "L" else ("ü", "ý", "þ")
        return [piece(t, x, y0), piece(m, x, (y0 + y1) / 2), piece(b, x, y1)]

    # Nested braces of different heights pair inner-first; two siblings of one height pair left to right.
    nested = brace(0, 0, 60, "L") + brace(10, 15, 45, "L") + brace(50, 15, 45, "R") + brace(60, 0, 60, "R")
    got = sorted((xo, xc) for xo, xc, *_ in R.enclosures(nested, []))
    if got != [(0, 60), (10, 50)]:
        fails.append(f"enclosures: nested braces paired as {got}, want [(0, 60), (10, 50)]")
    siblings = brace(0, 0, 20, "L") + brace(30, 0, 20, "R") + brace(40, 0, 20, "L") + brace(70, 0, 20, "R")
    got = sorted((xo, xc) for xo, xc, *_ in R.enclosures(siblings, []))
    if got != [(0, 30), (40, 70)]:
        fails.append(f"enclosures: sibling braces paired as {got}, want [(0, 30), (40, 70)]")
    # Choice bars have no hand: the second bar of a height closes the first.
    bars = [{"x": 5, "y0": 0, "y1": 30, "h": 30, "kind": "bar", "side": None},
            {"x": 50, "y0": 0, "y1": 30, "h": 30, "kind": "bar", "side": None}]
    got = [(e.x_open, e.x_close, e.stems == (bars[0], bars[1]), e.family) for e in R.enclosures([], bars)]
    if got != [(5, 50, True, "bar")]:
        fails.append(f"enclosures: a pair of choice bars paired as {got}, want [(5, 50, True, 'bar')]")
    # Each pair names its family and how low its opener reaches, which is where a glyph delimiter's band ends.
    got = sorted((e.x_open, e.family, e.low) for e in R.enclosures(nested, []))
    if got != [(0, "brace", 60), (10, "brace", 45)]:
        fails.append(f"enclosures: nested braces paired as (x, family, low) {got}, want [(0, brace, 60), (10, brace, 45)]")

    # A row keeps its words together: a held word below its hook moves the WHOLE row inside, and the word beside
    # the enclosure goes with it (review R1-1: `CODE-SET` was torn off the row of `FOR ALPHANUMERIC`).
    words = [{"text": "beside", "x0": 0, "y0": 100.0}, {"text": "held", "x0": 30, "y0": 103.9}]
    encl = [R.Enclosure(20, 60, 103.0, None, (None, None), "brace", 120.0)]
    rowof = R.place_rows(R.cluster([100.0, 103.9], 4.0), words, encl)
    if rowof[100.0] != rowof[103.9] or rowof[103.9] < 103.0:
        fails.append(f"place_rows: a row holding a word below its hook was not moved inside whole: {rowof}")
    # A row whose average already agrees with every word is left where it is.
    words = [{"text": "alongside", "x0": 0, "y0": 100.0}, {"text": "held", "x0": 30, "y0": 102.2}]
    encl = [R.Enclosure(20, 60, 101.0, None, (None, None), "brace", 120.0)]
    rowof = R.place_rows(R.cluster([100.0, 102.2], 4.0), words, encl)
    if rowof[100.0] != rowof[102.2] or rowof[100.0] != 101.1:
        fails.append(f"place_rows: a row whose average already agrees with the held word was moved: {rowof}")
    # Only words that cannot share one row are split: one inside the hook and one above it, between the same pair.
    words = [{"text": "above", "x0": 25, "y0": 100.0}, {"text": "held", "x0": 30, "y0": 103.9}]
    encl = [R.Enclosure(20, 60, 103.0, None, (None, None), "brace", 120.0)]
    rowof = R.place_rows(R.cluster([100.0, 103.9], 4.0), words, encl)
    if not (rowof[100.0] < 103.0 <= rowof[103.9]):
        fails.append(f"place_rows: two words that cannot share a row across a hook were not split: {rowof}")
    # One printed line: the width bound, and a step wider than the line gap inside it.
    if len(set(R.cluster([0.0, 1.7, 3.2], R.ROW_TOL, R.LINE_GAP).values())) != 1:
        fails.append("cluster: words 1.7 and 1.5 pt apart (3.2 pt end to end) were not one row")
    if len(set(R.cluster([0.0, 3.6], R.ROW_TOL, R.LINE_GAP).values())) != 2:
        fails.append("cluster: a 3.6 pt step inside the row width did not end the row")
    if len(set(R.cluster([0.0, 3.6], R.ROW_TOL).values())) != 1:
        fails.append("cluster: without a gap the width bound alone no longer groups a 3.6 pt step")
    return fails


def main() -> int:
    fails = unit_failures()
    for name, (words, stems, want) in FIGURES.items():
        got = draw(words, stems)
        if got != want:
            fails.append(f"{name}: the drawing changed\n  want:\n    " + "\n    ".join(want)
                         + "\n  got:\n    " + "\n    ".join(got))
    for f in fails:
        print(f"FAIL: {f}")
    total = len(FIGURES) + UNIT_CHECKS
    print(f"render_figure self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
