## Entry 3207 — 2026-11-06 12:22 PST — Every spec tool takes an edition (1985, 2002, 2023) from one table in spec_pdf.py

**Mechanism (kb/Work PB4815, campaign PB4812).** The tools of the transcription process were welded to the 2023 edition: each
spelled `specs/ISO_COBOL_2023.md` itself (about 45 files) and the PDF-measuring ones called `spec_pdf.require(2023)` at module
level. The owner's order of 2026-11-06 was to make the tooling edition-aware now and not to run any of it against the 1985 or
2002 PDFs until the batch returns are complete.

**Re-probe.** The note's grep reproduced on this tree: `PDF = spec_pdf.path(2023)` in eleven tools, the transcription path built
by 38 scripts. The 1985 and 2002 PDFs are not in this worktree (the private submodule is empty), so nothing could be run
against them, which is what the order asks.

**Fix shape.**
- `spec_pdf.py` becomes the one edition table. `EDITIONS[year]` is an `Edition` row: PDF file, Markdown file, geometry (`vector`
  for 2002 and 2023, `raster` for the 1985 scan) and the folio offset (30 for 2023, unmeasured for the others). Accessors:
  `markdown`, `geometry`, `folio_offset`, `require_vector`, `add_edition_argument` (the one `--edition {1985,2002,2023}`,
  default 2023) and `edition_data` (a tool's per-edition table; a missing edition exits naming the table, never falls back).
- Every tool in the note takes `--edition`: `cite.py` (`load(edition)`), `verify_publishable.py`, `verify_acknowledgment.py`,
  `lint_rendering.py`, `sweep_figures.py`, `render_figure.py`, `figure_extract.py`, `figure_geometry.py`, the four audits,
  `render-spec-page.py` (now argparse) and `page_workunit.py`; `spec-reconcile.js` takes `{pages, edition}` and no longer
  spells a path (it passes `--edition` to `page_workunit.py`).
- `figure_geometry.page_classifier(edition)` dispatches on the table: vector strokes with `classify_page`, the scan with
  `figure_raster.measure_page(edition, page)` (PB4817 creates it; until then the arm exits naming PB4817 instead of reporting
  a clean page). A tool that reads underlines from the vector drawings refuses a raster edition for the same reason.
- Edition DATA is keyed in the tool: stamp shapes and the acknowledgment and Preface patterns in `verify_publishable.py`, the
  acknowledgment page, words and stamp shape in `verify_acknowledgment.py`, the page-furniture lines in `sweep_figures.py`.
  Only 2023 (and a 1985 stamp shape from PB4812's recorded "order #") have rows; the rest exit naming the table. The tree check
  searches the stamp shapes of every edition with a row on every run. Shapes only: no licensee, order number or stamp text.
- The drift sweep: `spec_pdf.py --self-test` fails if any tracked Python file under `scripts/` or `.claude/` builds a
  transcription path itself (`"specs" / "...COBOL....md"`); the 38 existing sites (repairs, extractors, the fuzzer) now call
  `spec_pdf.markdown(2023)`.
- `spec_pdf.py` is now an argparse entry point (the standard-streams rule probes it), listing the editions.

**Goldens.** None: a tooling change that touches no compiled program. New self-test arms: `spec_pdf.py` (edition table,
accessors, `--edition`, `require_vector`, the transcription sweep and its planted cases), `cite.py` (a planted 2002 transcription
read through `load(2002)`; an absent 1985 exits naming it), `verify_publishable.py` and `verify_acknowledgment.py` (new
`--self-test`s on planted texts, a missing edition row exits), `test_figure_geometry.py` (the raster arm on a planted module,
and absent), `audit_grammar_optional_words.py` (`--edition 1985` refused naming PB4817).

**Rows.** None (`closes_rows: []`, tooling).

**Self-review.** Architecture: one table, no second copy of a path. Code: the vector arm is byte-for-byte the old one (the
furniture regex was checked equivalent on planted lines; `cite.py`, `lint_rendering.py` and `verify_publishable.py` ran clean
on the 2023 file). Found and left (not in the note's list, now in its Sites paragraph): `clause_page.py` (`FRONT_MATTER`,
`find_pdf`), `measure_index_levels.py`, `measure_sublist_levels.py`, `pdf_deobfuscate.py` and `repairs/rule_label_delimiter.py`
still call `spec_pdf.require(2023)`.

Branch review round 1 found the transcription-path sweep blind to a path written as one string literal (and to PowerShell): `scripts/docchecks/vcr.py`, `scripts/fix_spec_pagebreaks.py` and `scripts/gen-reserved-words.ps1` still read the 2023 file by their own literal while the self-test said the tree was clean. The sweep now matches both spellings in `.py`, `.ps1` and `.sh`; the three tools ask `spec_pdf.markdown(2023)` (the PowerShell one through the new `spec_pdf.py --markdown EDITION`); five lines that only name the file as repository data (`inert_lane.py` classification keys and fixtures, the `audit_evidence_supersession.py` rename map) carry a visible `# spec_pdf: names-the-file` marker. The self-test plants every spelling.

