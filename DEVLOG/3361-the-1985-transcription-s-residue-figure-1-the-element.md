## Entry 3361 — 2026-11-08 21:47 PST — The 1985 transcription's residue: Figure 1, the element-summary tables, body headings and Amendment 1's heads (PB4953)

PB4812's reconciliation compared every page of `specs/ANSI_COBOL_1985.md` against its scan, but it repaired only what a claim
pointed at. Four things were left, each now read against the rendered page.

**Figure 1 (PDF page 114).** The drawing lacked two connectors. The first SEND now reaches the communication description, a
second line runs from it to the output queue, and the third device joins back to the left input queue. The figure verdict
`figures-1985/p114-0.json` carries the same lines (`sweep_figures.py --edition 1985 --check` is clean).

**Element-summary tables (pages 31-83).** The scan has no text layer for a row's indentation or underline, so both were measured:
a row's left edge (8.4 pixels to the character at 100 dpi, counted from the leftmost row of its block) and the longest run of
ink under it. Rows were matched to the table by order, with a wrapped label taking two printed lines and a module column's
extra lines none; pages that did not match one to one (60, 71, 84) were read by eye. Pages already indented (34, 36, 41, 44, 47-49
and others) measured to zero differences, which proved the method. 462 rows gained their indentation or underline, among them bold group labels the
page underlines, and TRANSCRIPTION-STATE.md now names the convention (one `&nbsp;` per printed character).

**Body headings (pages 85-906).** 2,374 heading-shaped lines were matched to the PDF's text layer and checked for an underline
under the words. 33 top-level headings the page underlines gained `<u>` over their words and not their number
(`4. <u>TABLE HANDLING</u>`, the `GENERAL FORMAT FOR ...` titles, the condition-format labels, five `SECTION` titles). 49 headings could not be
matched (the `# SECTION` dividers that the transcription places after a folio) and were left as they were.

**Amendment 1's change list (pages 831-856).** The running head had six shapes. It is now the table header row where a change
table follows, and the single line `<u>Page No.</u> | <u>Change to ...</u>` on a prose page. Page 837's invented `| Page No. | Change |`
reads as the printed head. The three tables of pages 850-852, written without outer pipes, have them.

**What the tidy-up exposed.** Those three tables had been invisible to the 1985 rule catalog, which reads a change row by its
leading pipe. Two of their rows, X-27 and XIV-26 ("add new syntax rule 4 as follows"), named a rule the catalog could not
place, because `rule_catalog_1985.CHANGE_ADDS` knew only "as new ... rule n". It reads "add new ... rule n" too, and
`spec-rule-catalog-1985.json` was regenerated (two added rules, no other change).

**Ledger.** 533 sweep entries (`summary-rows`, `heading-underlines`, `amendment-1-heads`, `figure-1-connectors`) were added to
`LEDGER-1985.json`, and the page-853 claim and sweep entries whose text the new head replaced were updated, so `verify_publishable.py
--edition 1985` agrees with the file. Rows closed: none (a transcription residue).

