**2026-11-11, the route of the 2002 reconciliation decided, and its tooling made ready (kb/Work PB5255).**

The decisions (the project owner's controller, 2026-11-11):
1. The 2002 reconciliation runs through the Workflow `.claude/workflows/spec-reconcile.js` on the plan's quota, with Opus
   agents at effort medium. It does NOT use Message Batches: API credit is money, which is the owner's to spend.
2. All 882 pages are reconciled, the figure pages first. The 57 pages `transcribe_pdf.py status` reports ambiguous and the
   two clauses whose figure count differs (§2002:11.1.1, page 197; §2002:13.14.1, pages 273-274) are reconciled inside the
   same run, in its first ranges.
3. The adversarial verification of every claim stays (one verify agent per claim), and the refuter pass over every drawn
   figure is a second phase of the same campaign.

The tooling (PB5255): the Workflow takes `pages`, `edition`, `model`, `effort`, `outDir` and `workDir`, all required; its
prompt cites 2002's own notation clauses and words the priority from the work unit's figure pages; `page_workunit.py`
slices a page by its `## Page N` heading (it sliced by anchors no transcription has, so every 2002 unit would have been
empty); `merge_reconciliation.py --edition 2002` writes `LEDGER-2002.json` in `LEDGER-1985.json`'s shape; the plan is
`docs/rearchitecture/spec-reconciliation/2002/ranges.json` (74 batches, 29 flagged, 51 with a figure; halves `account1`
and `account2` of 37 batches and seven runs each). How to run a range: that directory's README, "Running a 2002 range".
The figure pages number 305 (445 general formats located by `sweep_figures.drawn_figures`), not 296.

**Stale above.** The paragraph of the conversion run that says the five canary pages are "NOT collected yet" for want of
account 2's labeled key is stale: the conversion run's `state.json` records the canary batch collected, the summaries
were deleted (the Batches route was refused for the license line and the text layer chosen), and the reconciliation no
longer uses the API at all. Nothing in this note waits on account 2's key.

**What remains of this note.** The 14 runs of the plan (7 per account); deciding every confirmed claim (APPLIED with its
edit, or ALREADY, FORMAT, FIGURE, KEPT with a reason) and then registering `LEDGER-2002.json` in
`verify_publishable.LEDGER` so the gate holds the transcription to it; the refuter pass over every drawn figure (the four
carried things, FIGURE-STYLE.md); the `docs/VERSION_CHANGE_REFERENCE.md` entry; then the 2002 rule catalog arm
(`extract_rule_catalog.py --edition 2002` has none) and its inventory (PB4886).
