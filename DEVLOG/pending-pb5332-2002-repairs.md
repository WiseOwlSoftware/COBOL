## The 2002 transcription repaired from its rendered pages, every reconciliation claim decided, and the 2002 ledger lands (PB5332, PB4813)

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
