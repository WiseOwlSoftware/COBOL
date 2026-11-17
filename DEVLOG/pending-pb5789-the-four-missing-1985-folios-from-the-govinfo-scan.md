## PB5789: the four printed 1985 folios our scan lacks are transcribed from a public scan, and the rows decided against the reconstructions are reopened

**The source.** Our scan of X3.23-1985 lacks folios XIII-30, XIII-31, XIII-54 and XIII-55 (kb/Work PB4952). Since 2026-11-09 the
transcription had held three RECONSTRUCTED blocks in their place. The owner approved finding a public scan on 2026-11-17. GovInfo
prints the four pages on its PDF pages 608, 609, 632 and 633. The PDF stays out of the repository.

**The transcription.** `specs/ANSI_COBOL_1985.md` holds the printed text, read from the rendered page images. Each pair stands at the
top of the PDF page after its gap (`## Page 612`, `## Page 634`), after a `> ⚠ **SOURCE — folios …, not in our copy.**` note, and
ends at its own folio line. The Preface says so. What the printed text changes:
- §1985:XIII.3.19.4 has twelve general rules. GR11 is new (the GENERATE report-name subtotalling paragraph), the reset rule is GR12,
  and GR9 a has two more paragraphs (the crossfooting order, and a counter that is its own addend).
- §1985:XIII.3.20.3: SR2 is the once-only rule and SR4 the CONTROL-clause rule. The reconstruction had them the other way round.
- §1985:XIII.3.10.7 and Table 2 keep their rules and grid, in the printed words.

**The tooling.**
- `cite.SOURCE` is the note's shape, and `EDITORIAL_NOTE` includes it.
- `rule_catalog_1985.parse` treats a folio line as the end of a printed page, so it skips the running header after one inside a PDF
  page. It puts a rule on the first folio below it (`folio_below`; `folio_of` deleted). No row outside these folios changed.
- `verify_publishable.check_reconstructions` counts a SOURCE note's folios as printed, each as a folio line below the note.
- Each has a planted self-test.

**The rows.** `extract_rule_catalog.py --edition 1985` regenerated the catalog: 2,275 rows (adds `1985-GR-XIII.3.19.4-12`), none
provisional. Eleven store records were decided against reconstructed text, and their `rule-digest` no longer matches. The view
refuses them until they are decided again against the printed text. They keep their witnesses and notes for that decision. Nine
stored code reads are stale; none was edited.

**The citations.** g85_bf09_09 cites the printed GR12 and names its row. Four other cases, `VersionConformancePass`, a derivation
entry and the design's section 10.1 stop citing a reconstruction. `audit_code_citations` and `audit_doc_citations` report no finding.
kb/Work PB5789 stays open for the code read of GR1.
