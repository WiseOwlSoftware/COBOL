## Entry 3397 — 2026-11-09 16:18 PST — PB5053: the 2023 reserved-word list's misprints corrected and attributed (C15, C16)

Under the owner's most-accurate-text rule (kb/Work PB5045), the misprints TRANSCRIPTION-STATE.md still kept "as
printed" in the §2023:8.9 reserved-word list are corrected in place, each flagged with the printed form and page and
listed in the Addendum.

- **Re-probe.** Rendered PDF pages 235 to 237 (printed 205 to 207). Page 206 prints `i-O` and `I-OICONTROL`; the
  PDF text layer finds each once in 1,261 pages against 8 `I-O-CONTROL` on 6 pages (12.4.6, the context-sensitive
  table, 9.1.18.2, an example, the index), and the 2002 list prints `I-O` and `I-O-CONTROL`. Addendum **C15**.
- **Sibling sweep.** The note named one ordering slip (`END-RECEIVE` before `END-READ`). Checking every adjacent pair
  of the list found three more of the same kind: `BY` among the `B-` words (page 205), `FARTHEST-FROM-ZERO` before
  `FALSE` (206), `SEND` before `SELF` (207). The 2002 list orders all three as corrected. Moved, Addendum **C16**,
  which also records why two other out-of-order pairs stay (`FLOAT-BINARY-64` before `-128`, the `B-SHIFT-` pairs:
  systematic, not slips).
- **Tooling.** `gen-reserved-words.ps1` lost its EMD-START / I-OICONTROL remap (a rerun regenerates every file
  unchanged); `reserved_word_gaps.py`'s `PRINTED_QUIRKS` maps the C15 printed forms so `--check` still holds the list
  to the page; `reserved_word_list.py` and `VERSION_TEST_MATRIX_DESIGN.md` no longer describe the misprints as kept.
- **TRANSCRIPTION-STATE.md**'s "ISO's own defects" section now states the rule (corrected, flagged, listed,
  reversible; only an uncertain reading stays as printed). `>>IF` / `>> IF` remains as printed: both are legal.
- No inventory row: `closes_rows: []` with its reason.

