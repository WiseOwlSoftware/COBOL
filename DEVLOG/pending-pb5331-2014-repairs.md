## PB5331: the 2014 transcription's 74 confirmed discrepancies repaired from the page, and two figure-generator root causes fixed

The 2014 reconciliation (LEDGER-2014.json) confirmed 74 discrepancies in `specs/ISO_COBOL_2014.md`: 13 normative, 57
structural, 4 cosmetic. Every one is now repaired and decided `APPLIED` with its edits in the ledger, and 43 sibling repairs
that no claim named are recorded as its `sweeps`. Each repair was made from the page: words and positions from the PDF's
text layer, layout checked on the rendered page.

- **Normative.** Two of the misdrawn general formats were the generator's fault, so a hand edit would have been undone by
  the next sweep. `figure_extract.MIN_RULE_W` (3.0 pt) dropped the 2.85 pt underline under the level-number `1` of
  §2014:13.10.1, which made a required word optional; the floor is now 2.5 pt. `render_figure.split_fused` matched a fused
  brace hook to the first raw run with the same text at the same x. OCCURS Formats 1, 2 and 4 all print `}KEY` at x 151.3,
  so Formats 2 and 4 were drawn at Format 1's height; the match is now the run nearest in y. That fix also redraws DIVIDE
  Formats 2 and 4 and SUBTRACT Format 2. `sweep_figures --check` was run before and after on every vector edition: 2014
  changes exactly those six figures, 2023 changes none, and 2002 changes the same six defects in its own text, which the 2002
  repairs apply. Two new `test_render_figure.py` checks fail on the old generator (25/27) and pass on the new one (27/27).
  The other normative repairs are textual: subscripts and superscripts flattened into names (8.8.1.3.1.1 bounds, ADD,
  SUBTRACT, MEAN, SUM, PRESENT-VALUE, VARIANCE), Table 18's numeric-edited sub-rows, and FLOAT-NOT-A-NUMBER-SIGNALING split
  across two reserved-word cells.
- **Swept by mechanism.** Every smaller, raised or lowered span in the 2014 text layer was checked (45 spans). Tables broken
  by a wrapped cell were rebuilt from the text layer's rows and columns, and the words of each region are the same before and
  after. The index (pages 927-951) was rebuilt the same way, with each line's level taken from its indent: 22 pages changed,
  and no word was gained or lost. The Annex D flowcharts, Figure D.3, D.5 and D.6, and Figure 1 are drawn. The one
  non-Latin homoglyph, on page 219, is replaced.
- **Held.** `LEDGER-2014.json` joins `verify_publishable.LEDGER`, so `verify_publishable.py --edition 2014` now refuses a
  claim left undecided or an edit that is not on its page. `merge_reconciliation.py --edition 2014 --check` reports CURRENT.
- **Leads.** The 2023 transcription flattens the same subscripts (§2023:14.9.2.4, §2023:14.9.44.4, §2023:15.60.4,
  §2023:15.74.4, §2023:15.88.4).
