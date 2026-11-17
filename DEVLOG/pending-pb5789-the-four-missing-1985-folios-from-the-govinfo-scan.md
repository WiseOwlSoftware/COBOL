## PB5789: the four printed 1985 folios our scan lacks are transcribed from a public scan, and their 13 rows are decided again against the printed text

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
provisional. Eleven store records had been decided against reconstructed text. The proposals were refuted and ruled
(`kb/Work/PB5683/2026-11-17-rulings-pb5789-folios.md`), and one `record_verdicts.py --edition 1985` batch recorded the 13 rows:
- Ten KEPT as proposed. SR2 now maps to SR-13.18.57.3-13 and -14, SR4 to -10 and -14, the new GR11 (GENERATE report-name
  subtotalling) to GR-14.9.16.4-2, and GR12 (the reset rule) to GR-13.18.54.4-2.
- `1985-GR-XIII.3.10.7-1` and `1985-FMT-XIII.3.20.2` are CONFORMS and CHANGED. The FMT row's qualified operand is re-cited to
  §1985:IV.4.3.8.1.
- `1985-GR-XIII.3.19.4-9` is PARTIAL, with two defects filed. kb/Work PB5894 (wrong answer): crossfooting runs in dependency order,
  not definition order. kb/Work PB5895 (rejects legal source): a sum counter that is its own addend is refused under a 2023 rule.
- Four witnesses were retired, each with its reason.
- The stored code reads were not edited. Nine are stale, and `code_reads.py plan --edition 1985` lists nine KEPT rows owed a read.

Before (main), `inventory_view.py --summary`:
```
1985 2274 rules · 0 GAP (v1.0 = zero GAP)
1985: 901 adjudicated · CONFORMS 893 · DOCUMENTED-NON-SUPPORT 8
1985: code reads 1354 confirmed · 1 contradicted · 0 undecided · 0 stale · 0 KEPT rows owed a read (kb/Work PB5759)
```
After:
```
1985 2275 rules · 1 GAP (1 open) (v1.0 = zero GAP)
1985: 902 adjudicated · CONFORMS 893 · DOCUMENTED-NON-SUPPORT 8 · PARTIAL 1
1985: code reads 1346 confirmed · 0 contradicted · 0 undecided · 9 stale · 9 KEPT rows owed a read (kb/Work PB5759)
```
The one GAP is `1985-GR-XIII.3.19.4-9`, claimed by PB5894 and PB5895.

**The citations.** g85_bf09_09 cites the printed GR12 and names its row. Four other cases, `VersionConformancePass`, a derivation
entry and the design's section 10.1 stop citing a reconstruction. `audit_code_citations` and `audit_doc_citations` report no finding.
kb/Work PB5789 is landed: its row is CONFORMS on its own witness, `pb5789_page_heading_relative_after_rh_85`.

**New witnesses.** Ten 1985 conformance cases (`pb5789_*`, `pb5789-*`) witness the printed rules. Two accepted cases
cover the page heading after a report heading's NEXT GROUP and a nonprintable page heading. Two cover GENERATE report-name subtotalling
with no detail group and with one. Six refusals cover TYPE SR2, SR3 and SR4.
