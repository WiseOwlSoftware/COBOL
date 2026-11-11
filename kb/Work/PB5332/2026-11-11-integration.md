# 2026-11-11 - integration: the 2002 and 2014 repairs land as one branch, from one figure generator

**Decision (the operator, 2026-11-11 15:3x).** `fix-pb5332-2002-repairs` (this note) and `fix-pb5331-2014-repairs`
(kb/Work PB5331) were finished, gated GREEN and unreviewed when they were found to conflict. Both fixed the shared figure
generator (`scripts/spec/figure_extract.py`, `scripts/spec/render_figure.py`), and each generator fix redraws the figures
of the other editions. Landed apart, whichever landed second would leave the first edition's figures drawn by a
generator that no longer exists (`sweep_figures.py --check` red on it). So they land as ONE branch: the 2014 branch is
merged into the 2002 branch, and every edition's figures come from the one merged generator.

**The merge (finisher, 2026-11-11).** Five files conflicted; each was resolved keeping both fixes.
- `figure_extract.MIN_RULE_W`: both sides set 2.5 pt for the same measured 2.85 pt rule (the level number `1` of
  §2002:13.9.1, PDF page 267, and of §2014:13.10.1, PDF page 306). One comment now names both pages.
- `render_figure.split_fused`: both sides take the raw run nearest in y. PB5332's measure, the run's top over all its
  characters, is kept. The comment names 2002 PDF page 234 and §2014:13.18.38.1, PDF page 370.
- `test_render_figure.py`: both sides' checks are kept (UNIT_CHECKS 14 + 6 + 2 = 22). Each check was run against its old
  generator: with the first-match `split_fused`, both `split_fused` checks fail; with `MIN_RULE_W` 3.0, both underline
  checks fail ("32/36 RED", then "34/36 RED"). The merged generator is "36/36 GREEN".
- `verify_publishable.LEDGER` holds 1985, 2002 and 2014.
- `spec-reconciliation/README.md` keeps PB5332's LEDGER-2002 row and PB5331's LEDGER-1985 row.

**The redraw.** The 2002 branch's generator already carried both of PB5331's corrections plus its own (continuation
indent, `rejoin_lines`, `INTRODUCES_FORMAT`). The merged generator therefore draws 2002 and 2023 exactly as the 2002 branch
did. The only redraws are 5 figures of the 2014 text, which had never met PB5332's corrections. Each was checked against
its rendered PDF page (`scripts/render-spec-page.py --edition 2014`):
- §2014:12.3.6.1 (page 255): `IS literal-5 …` keeps its printed indent under `CLASS class-name-1`.
- §2014:13.14.1 (page 309): `[ HEADING IS integer-3 ]` keeps its printed indent under `PAGE`.
- §2014:13.18.35.1 Format 1 (page 362): the `…` is on the row of `LINE NUMBERS ARE` and `integer-2`, as printed.
- §2014:14.9.21.1 Format 4 (page 539): `INSPECT identifier-1 CONVERTING`, `TO` and `[ after-before-phrase ]` are one
  printed line, at the braces' point.
- §2014:14.9.43.1 (page 629): `UNLOCK file-name-1` is one printed line beside the bracket, no longer stacked as
  alternatives.

Each redraw is recorded as a `figure-generator` sweep in `LEDGER-2014.json` (LEDGER-2002's shape), which brings the sweeps
from 43 to 48.

**After the merge:**
- `sweep_figures.py --check` is clean for 2002 (446 figures) and 2014 (464).
- For 2023 it reports the same 2 drifted figures as the base and as main: §2023:14.9.9.2 and §2023:14.9.28.2 Format 3. No
  branch changed them (PB5331's note measured them before and after).
- 1985 is a scan. Its raster arm reads only `MIN_RULE_W`, and both sides set it to the same value.
- `merge_reconciliation.py --check` is CURRENT for 2002 and for 2014.
- `verify_publishable.py` passes for both: 882 of 882 and 952 of 952 pages compared, every claim decided.
- `extract_rule_catalog.py --check`: FINDINGS 0 for 2023 and 2002, no findings for 1985. 2014 has no catalog.
- `inventory_view.py --check` is GREEN, with no row re-affirmed by the merge.

**A lead the redraw showed, not fixed here.** §2014:13.14.1 (and the same figure in 2002 and 2023) prints the two
`LAST` brackets indented under `PAGE` like `[ HEADING …`. The continuation rule (`render_figure.build`, the `indent` map)
measures only word cells, so a line that opens with a delimiter keeps no indent and is still drawn flush. The figure's
words and grouping are right; only that indent is lost.
