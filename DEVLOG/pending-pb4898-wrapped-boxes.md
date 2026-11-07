## PB4898: a box that wraps a line is measured as one box per printed line

**Carried.** This branch carries the two unlanded branches of wave 20261891 onto main 440ca75f81 by cherry-pick:
PB4849's raster arm and level-2 boxes (five commits), and PB4899's page 602 calibration crop. Their bare citations
became edition citations (`§2023:5.2.6.2` and the rest; PB4887 refuses a bare one), each checked with `cite.py`. The
tree re-gated GREEN before new work.

**Re-probe.** The first paragraphs of 1985 PDF pages 389 and 684 put level-2 text in boxes, one box per printed line.
Where the boxes of two lines touch, the printer drew ONE outline, a polygon open where they meet (389: `and the` over
`and the MULTIPLE`, `location ... reel.` over `FILE TAPE clause`; 684: `a message segment, ... a` over `or more`). The
old `find_boxes` judged a component as one four-rule rectangle. It measured 389's paragraph as no box and six choice
bars (the polygons' sides), and it lost 684's `or more` and read two bars.

**The fix.** `find_boxes` finds a component's boxes from the faces its rules enclose (`_faces`). A face is the paper
the ink cuts off, joined 4-connected so it does not cross a rule at a diagonal step of the scan. Each face is cut into
one rectangle per printed line: a run of rows whose ends stay within `BOX_TILT`, smoothed over half a line so a glyph
fused to a side is no jump. Each rectangle is judged by the unchanged four-rule test (`_box`, factored out of
`find_boxes`) with a seam drawn where it joins the next rectangle. The erasure skips the seam, so a descender there
keeps its ink. A component with no face of a box's size (a rule the scan broke) is still judged whole. So is one with
several: the first cross-check run with the new code took each cell of a drawn table for a box (18 and 21 "boxes" on
pages 297 and 298, and three figures fell to unlocated), and cells that share rules are a diagram, not level-2 text.

**Tests.** Two new stamp-free crops, `p0389-prose.png` (4 boxes) and `p0684-prose.png` (3 boxes), were counted by
eye, and a planted case (8h) draws a two-line polygon. Against the old measurer all three fail (53/57). A second planted
case (8i) is a ruled table that must not become a box per cell. `test_figure_raster.py` passes 58/58.

**The cross-check again** (976 pages, 501 drawn figures): the verdicts are unchanged (181 agree, 68 disagree, 212
unpaired, 40 unlocated). 114 measured boxes now reach a figure instead of 104: the wrapped boxes of the general
formats on pages 245, 354, 409, 543 (both formats) and 661. Figures that disagree on choice bars fell from 28 to 26. `test_render_figure.py` 21/21, `figure_reading.py --self-test` 17/17,
`sweep_figures.py --self-test` 1/1 and `test_figure_geometry.py` 10/10 are unchanged.

**Rows.** None: PB4898 is landed with `closes_rows: []` (tooling). PB4849 stays open for its pairing, refuter, apply
and audit steps.
