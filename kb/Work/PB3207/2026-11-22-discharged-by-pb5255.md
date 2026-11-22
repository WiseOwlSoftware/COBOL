# PB3207 — discharged: PB5255 removed the anchor-keyed slicer before this note was worked

Re-probed 2026-11-22 by the wave 20262139 group G implementer, at main a012d50751.

**What the note measured (cd85e4901):** `page_workunit.slice_for` sliced `specs/ISO_COBOL_2023.md` at
`<a id="page-N">` anchors the de-paging removed, so every unit carried an empty `page-N.md`.

**What the tree does now.** `git log -S'a id="page-' -- scripts/spec/page_workunit.py` names 6a11f91bcc (PB5255,
"the spec-reconcile tooling takes the 2002 edition") as the commit that deleted the anchor regex. `slice_for` no longer
exists; `page_workunit.units` slices by `spec_pdf.pages_of` (the `## Page N` heading reader, the one page reader of the
transcriptions that keep pages), exits 2 on a page the transcription lacks, and for a transcription that keeps no pages
(2023) writes the rendered page alone with `md: null` and says so on its output.

- `grep -c '<a id="page-' specs/ISO_COBOL_2023.md` → 0; `grep -c '^## Page ' specs/ISO_COBOL_2023.md` → 0.
- `python scripts/spec/page_workunit.py --self-test` → `=== PAGE WORKUNIT SELF-TEST: PASS ===` (its last case is the
  2023 shape: no pages kept, the unit is the rendered page alone, nothing missing).
- `python scripts/spec/page_workunit.py 557 --out <scratch>/probe2` → `spec_p557.png` and `page-557.json` with
  `"md": null`, no `page-557.md`, and the line "the 2023 transcription keeps no pages: each unit is the rendered page
  alone (no page-N.md)".
- `python scripts/spec/clause_page.py 14.2.1 --render <scratch>/probe3` → `PDF page 557 printed folio 527`, then the
  same unit: the rendered page alone.

**The callers.** `spec-reconcile.js` has no 2023 row and refuses the edition (its lines 47-49 and 74), so no agent is
handed an empty 2023 slice to compare. `spec-grammar-impact.js:138` and `spec-grammar-conformance.js:163` run
`page_workunit.py` only to render the printed page and tell the agent the markdown is the artifact under suspicion, so
they need the image and nothing else.

**The Target is not taken.** Building a 2023 page's markdown from `clause_page.index` would give the grammar workflows a
slice they are told not to read, and spec-reconcile a comparison whose 2023 sweep is complete (spec-reconciliation
LEDGER.json). The rule the note cited (one locator per job) holds: `clause_page.index` is the one page-to-clause locator
over the PDF and `spec_pdf.pages_of` the one page slicer over a page-keyed transcription; no second locator keyed on the
deleted anchors remains in either file. PB811 (`audit_underlining.py`'s anchor buckets) is a separate tool and stays open.

**Changed with the discharge:** `scripts/spec/clause_page.py` (the `FRONT_MATTER` comment quoted the plan's sentence
about the deleted `#page-N` anchors; it now states the fact itself: 30 front-matter pages, clause 14.2.1 on PDF page 557,
folio 527, as measured above).
