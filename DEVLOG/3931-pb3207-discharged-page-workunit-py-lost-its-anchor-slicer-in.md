## Entry 3931 — 2026-11-22 15:25 PST — PB3207 discharged: page_workunit.py lost its anchor slicer in PB5255

PB3207 (an R2 architecture finding of the PB1754 review) said `scripts/spec/page_workunit.py` sliced the 2023
transcription on the `<a id="page-N">` anchors de-paging removed, so every work unit of spec-reconcile,
spec-grammar-impact, spec-grammar-conformance and `clause_page.py --render` carried an empty markdown slice.

The re-probe at main a012d50751 shows the mechanism is gone. `git log -S'a id="page-'` names 6a11f91bcc (PB5255) as the
commit that deleted the anchor regex: `page_workunit.units` now slices by `spec_pdf.pages_of` (`## Page N`), exits 2 on a
page the transcription lacks, and for 2023, which keeps no pages, writes the rendered page alone (`md: null`) and says
so. `page_workunit.py --self-test` PASS; `page_workunit.py 557` and `clause_page.py 14.2.1 --render` each wrote
`spec_p557.png` and a `page-557.json` with `"md": null` and no `page-557.md`. spec-reconcile.js refuses 2023; the two
grammar workflows use the tool only to render the printed page.

The note's Target (a 2023 page slice built from `clause_page.index`) is not taken: no caller reads one. The note is
retired with no inventory row, its evidence in `kb/Work/PB3207/2026-11-22-discharged-by-pb5255.md`. The one stale line
found on the way, the `FRONT_MATTER` comment in `clause_page.py` that quoted the plan's sentence about the deleted
anchors, now states the measured fact (clause 14.2.1 on PDF page 557, folio 527).

