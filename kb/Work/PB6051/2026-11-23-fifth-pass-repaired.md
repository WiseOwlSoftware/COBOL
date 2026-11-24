## The fifth pass is repaired: 4 confirmed, all applied; two classes now linted; no further full pass (PB6135)

The fifth independent pass (run wf_10fdbb6c-b56) read all 976 pages and left 16 claims: 4 confirmed, 12 refuted, each with
its own adversarial verdict (`1985/compare-*.json`, `1985/verify-*.json`, committed as the run's record). All 4 confirmed
claims are applied, each read on the rendered page first (`LEDGER-1985.json`, claims with `pass: 5`):

- **p532-1** (normative) the FD Format 2 RECORD clause (page 532, X-16) set `[DEPENDING ON data-name-1]` at the
  alternatives' column, with blank rows between, so the brace read as four alternatives. It is now the IS VARYING
  alternative's indented continuation, the three alternatives on consecutive rows as printed. Sibling sweep: every brace
  and bracket of every `<pre>` figure scanned for an alternative opening with `[` at the alternatives' column, and every
  brace whose alternatives are spaced by blank rows (58), each candidate rendered. Same fault: the SD's RECORD clause (page
  559); the SUM clause's `[RESET ON]` bracket inside the SOURCE/VALUE/SUM brace (page 210) and under `{SUM ...}` in 3.19.2
  (page 632); the RECORD clause's Format 2 `[DEPENDING ON]` (page 404). Sweeps `continuation-indent(PB6051 p532-1)`; figure
  verdicts p532-0, p559-0, p210-1 and p404-1 re-stated as `neither`, the figure as now drawn, and a new
  `neither` verdict p632-0 (the regeneration sets page 632's `[RESET ON]` flush). The root of the class is the
  figure regeneration (`sweep_figures`), which drew these continuations flush; its verdicts now hold the printed form.
- **p240-1** page 240's general format and its `DATE-COMPILED. current date.` stand inside the box around all of 3.4 and
  now carry `║ ... ║` as page 237's boxed format does (two `boxes` entries, FORMAT). Sweep: page 387's RECORD DELIMITER
  format, inside the box around all of 2.8, is now `╔ ║ ╚` (sweep `box-figure(PB6051 p240-1)`). New
  `neither` verdicts p240-0 and p387-0: the regeneration measures no box in a format's band when the box is the clause's.
- **p419-1** rule (8) b of READ (page 419): the false `<u>...</u>` (the box edge read as an underline) is removed and the
  `║` span is exactly the boxed sentence. The new lint found no other instance in the file, prose or figures.
- **p32-1** page 32's Qualification and its two children are at the printed columns. Sweep: every row of the
  summary-of-elements tables (pages 31 to 83) measured against the scan's OCR word positions at the page's own column
  pitch, and every page with an unmeasured row or a disagreement rendered (32, 43, 59, 72, 73, 74, 83, 835). Page 32 was
  the only page of the standard's own tables off; pages 74 to 83 carry one more `&nbsp;` than print by Amendment 1's own
  instruction (C64). Amendment 1's intrinsic function summary (page 835, A-5, and its applied copy on page 59) set COBOL
  words, Function-name and Function-Identifier flat; they are indented as printed (sweeps `summary-indent(PB6051 p32-1)`).
  The two `summary-rows(PB4953)` sweeps that named page 32's old rows are re-stated.

The 12 refuted claims (p354-1, p440-1, p963-1 to p972-1) are refuted as the verdicts say.

**Linted now.** `heading_levels.check_box_markup`, run by `verify_publishable.py --edition 1985`, with selftest case 09:
an underline that opens on one side of a level-2 box side and closes on the other (p419-1), and a `<pre>` figure between
two box-marked prose lines with no box sides of its own (p240-1; drawn flowcharts are exempt). Seen to fail on the
pre-repair text (the three instances), clean after. Already linted before this pass: the title forms (`TITLE_FORMS`), the
heading levels, the figure verdicts' identity (`sweep_figures --identity`).

**Still a reading pass's.** A boxed figure that ends its page (its after-neighbour is the folio, as page 240's second
block); an alternative's continuation set at the alternatives' column (the scans used here are candidate finders, too
noisy for a gate: EVALUATE's `[NOT]` and USE's `[ALL REFERENCES OF]` are real alternatives that open with `[`); summary
table indentation (measurable from the scan's OCR layer, but the PDF is not in CI, so a gate would need the measurement
committed as data, as `sublist-levels.json` is for 2023).

**Catalog.** Eight 1985 rows changed text (FMT-VI.3.4.2 and FMT-VII.2.8.2 also box none → whole, level 1 → 2;
GR-VI.3.4.4-1, FMT-VII.3.8.2, GR-VII.4.4.4-8, FMT-X.4.2.2, FMT-XI.3.2.2, FMT-XIII.3.19.2); the six whose digest moved
were re-recorded unchanged through `record_verdicts.py`. Code reads owed: 1985-FMT-VII.2.8.2 and 1985-GR-VII.4.4.4-8.

**`sweep_figures --check --edition 1985`** names one figure awaiting a verdict, page 634's 3.20.2 format ("not located on
the page"); its page is untouched by this pass, so it stands on main as well and is not this pass's.

**PB6051 stays open.** No sixth full pass runs (owner 2026-11-23, kb/Work PB6135): PB6051 closes when every error class found
has a whole-document check GREEN in the gate and a PDF text-layer diff reports no unexplained difference.
