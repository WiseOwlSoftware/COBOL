## Entry 3513 — 2026-11-11 16:44 PST — PB5331: the 2014 transcription's 74 confirmed discrepancies repaired from the page, and two figure-generator root causes fixed; The 2002 transcription repaired from its rendered pages, every reconciliation claim decided, and the 2002 ledger lands (PB5332, PB4813)

### PB5331: the 2014 transcription's 74 confirmed discrepancies repaired from the page, and two figure-generator root causes fixed

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

### The 2002 transcription repaired from its rendered pages, every reconciliation claim decided, and the 2002 ledger lands (PB5332, PB4813)

This branch lands the held `LEDGER-2002.json`, rewritten at its own head, together with the repairs it asked for.

**The repairs.** All 83 confirmed claims are decided APPLIED, and 46 sweeps record siblings that no claim named. Each
repair was made from the rendered 2002 page or from the PDF's measured text layer:
- **Sub- and superscripts:** every lowered or raised span in the text layer (37 spans on 9 pages), restored as
  `<sub>` or `<sup>`.
- **General formats:** the six claimed figures were fixed in the figure generator. The fix also redrew eight sibling
  2002 figures and five 2023 figures that had the same faults. Only their layout changed.
- **Tables:** header rows restored, wrapped cells put back, and lines paired across columns, swept by pattern.
- **The Index:** regenerated from its measured levels.
- **Annex E:** the illustrations drawn as box art.
- **Headings:** two wrapped clause headings joined.

**PB5332's six catalog findings:**
- The four headings are typed GR in `KINDS`, the kind 2023 gives each paired clause.
- The taxonomy clause headed "Rules" stays out of the catalog through `excluded_blocks`.
- §2002:8.8.4.1.5.2 prints its one rule without a number, so it is admitted as a paragraph row.
- Two text repairs: §2002:14.8.1.3 item 2 is indented as printed, and the CURRENT-DATE table keeps '99.' in its cell.

**The numbers:**
- 2002 rule catalog: 3290 -> 3298 rules, 6 -> 0 findings.
- 2002 rules paired to 2023: 1771 -> 1770. Two were gained. Three function rules were lost because 2002 now carries
  its printed subscripts, which 2023 prints flat.
- `LEDGER-2002.json` joins `verify_publishable.LEDGER`, which holds the transcription to it.
- Re-affirmed against the redrawn figures: three 2023 FMT rows and ten 1985 change records.

**Landed together with the 2014 repairs (PB5331).** Both branches fixed the shared figure generator, so the 2014 branch
was merged into this one and every edition's figures come from one generator (kb/Work/PB5332/2026-11-11-integration.md).
The merged generator redrew 5 figures of the 2014 text, each checked against its printed page and recorded as a
`figure-generator` sweep in `LEDGER-2014.json`. It redrew no 2002 or 2023 figure. On the 1985 scan, it indented one figure's
continuation line as printed (page 203). It also exposed a missing floor on a rule's thickness in the raster arm: a 0.2 pt
sliver read as an underline on page 905. `figure_raster.RULE_MIN_THICK` adds that floor.

