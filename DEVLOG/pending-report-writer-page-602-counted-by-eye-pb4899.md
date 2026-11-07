## The Report Writer general-format page counted by eye for the raster measurer's calibration (PB4899)

PB4848 calibrated `scripts/spec/figure_raster.py` on twelve 1985 general-format regions, each counted by eye, and left
the Report Writer page (PDF page 602) measured but not counted. This entry closes that gap.

**The count.** Page 602 (3.9.2, the report group description entry, both formats) was rendered with
`scripts/render-spec-page.py --edition 1985` and counted by eye over the band 200 to 670 pt: brackets 3/3 (the two
LINE NUMBER clauses and NEXT GROUP; the typed `[ ]` glyphs are text), braces 13/13 (the 14-line TYPE IS brace, its nine
inner two-line braces, the two LINE NUMBER braces and the three-line NEXT GROUP brace), no boxes, 41 underlines.

**The measurer against it.** Brackets, braces and boxes match, and no shape is left unclassified. Underlines do not:
the measurer reports 42. The extra one is a 9.0 pt run along the fused foot serifs of "am" in "data-name-3" (y 437.3 pt,
x 318 to 327 pt). `split_fused_rules` takes it for an underline fused to its word because the run is exactly
`FUSED_MIN_RUN` long. The note scoped this work to the count ("a miscount it finds becomes its own note"), so the
measurer is unchanged and the false rule goes to the orchestrator as a lead.

**What changed.** The crop `scripts/spec/figure_raster_pages/p0602.png` (binarized, stamp margins excluded) and its
`calibration.json` entry: `test_figure_raster.py` now asserts page 602's brackets, braces and boxes, and no underline
count until the serif-chain defect is fixed. PB4848's calibration table carries the page 602 row, and the module
docstring names thirteen calibration pages. PB4899 is landed; it closes no inventory row (tooling).
