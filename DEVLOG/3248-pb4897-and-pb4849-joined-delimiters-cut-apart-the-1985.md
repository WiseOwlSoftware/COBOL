## Entry 3248 — 2026-11-07 05:25 PST — PB4897 and PB4849: joined delimiters cut apart, the 1985 pairing repaired, and the figure comparison tightened

**PB4897 (landed; replayed from wave 20261891 B, commit 298949190e).** Amendment 1's bold face stands the brackets
and braces of consecutive lines one on another, joined end to end, and the measurer read them as one tall bracket or a
brace pointing at the joint (PDF pages 841 and 844, §1985:IV.4.3.8.2.2 as Amendment 1 replaces it). `find_stems`
takes a stem's component by majority, `split_stacked_stems` cuts at an interior foot band, and `_curlies` cuts a brace
whose centre line bends toward the hooks at a joint. The replay onto main conflicted with wave 20261893's wrapped-box
work in the calibration docstring and the self-test; the predecessor's planted cases are now 8j and 8k, and the
calibration covers fifteen regions including 841 and 844.

**PB4849 (in part).** Re-probe: `sweep_figures.py --edition 1985 --cross-check` gave 178 agree, 73 disagree, 210
unpaired, 40 unlocated; `--check` 357 blocks awaiting a verdict. Three pairing faults in `figure_raster`:
- a typed delimiter ending its printed line paid 1.5 for a missing neighbour when the word drawn after it on its row is
  printed half a line off (page 205, `[USAGE IS] {COMP ...`): a missing neighbour now contradicts nothing when the other
  drawing's neighbour lies on another line or row (`_Frame.cost`);
- two equal words of one row on one line were dealt in order only after the delimiters were placed by them, so the
  delimiters crossed with them (page 182): `_deal` deals them first;
- an OCR reading of no delimiter (`1` for `}`) cost as much as a delimiter of the other side, and a speck on a line of
  its own stretched the figure's frame (page 321): the first now costs what a dropped reading does, and the frame's
  extents leave out specks (`_is_speck`, the one test `align` already used inline).
The cross-check moved to 187 agree / 195 unpaired. The newly paired figures showed `figure_reading.compare` agreeing
with wrong regenerations: page 321's typed brackets, each in a level-2 box too small for `find_boxes`, came out as
bars with underlined brackets, and the ellipsis of pages 399 and 654 a row lower after another group. `compare` now
counts each drawing's enclosures by family and its underlined delimiters, and compares what each ellipsis follows
(§2023:5.2.7); those three await a verdict. Five agreed figures were written with `--apply` (pages 182, 206, 212,
223 and 321 Format 2, checked against the rendered pages). `--check`: 0 drifted, the rest await a verdict.

Tests: planted 10h, 10i (and a direct `_deal` case), 10j and an OCR-misreading case in `test_figure_raster.py`; three
new `figure_reading` self-test cases. The new raster cases fail against the base measurer (67/70 RED, the `_deal` case
missing outright). Left as leads: a box around one typed delimiter, and `render_figure`'s ellipsis row after a tall
brace. PB4850 is not started (blocked by PB4849). closes_rows: none (tooling and transcription).

