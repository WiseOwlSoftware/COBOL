## Entry 3961 — 2026-11-22 23:44 PST — Suite 85 Stage 0: the coverage-point catalog of the 1985 general formats and its credit from WiseOwl's parse (PB6050); PB6054: every GnuCOBOL 85 failure has its cause, and suite 85's second-compiler term is 0

### Suite 85 Stage 0: the coverage-point catalog of the 1985 general formats and its credit from WiseOwl's parse (PB6050)

The 85 suite's points denominator did not exist: `suite_status.py --edition 85` printed `points UNMEASURED`. This landing builds it
(design section 3.4, S14 and S15 cut to the 1985 figures, kb/Work PB6046 revision 4) and changes no test's expected behaviour.

**The catalog.** `harness/formats.py` reads one decided figure of `specs/ANSI_COBOL_1985.md` (the batch's drawing where the refuter
agreed, the refuter's own where it did not: `figures-1985/`) into a tree from its own delimiters: the multi-row brackets and braces
`figure_reading` pairs, and the typed pairs it spans across lines. Rows stacked inside a brace are alternatives; rows of a bracket
that do not begin in one column are one phrase wrapped. A level-2 box that holds only an ellipsis (`║ … ║`) repeats the portion
before it. `harness/points.py` maps each figure to its FMT row (the heading above it and its page: the clause numbers repeat across
modules) and derives the points; 213 rows, 252 formats, 3,846 points in `scripts/spec/data/format-points-1985.json`, with a digest.
Every tree is checked to carry exactly the words of its drawing (a word lost or doubled fails the build), which found the drawn
parentheses of the 42 function figures and a stray `. .`. Two figures whose typed braces open and close lines apart (EVALUATE; the
report group SIGN clause) are read by hand from the page in `formats.DECIDED_READINGS`, held to the same words check. Page 396's
heading prints `3.2.2.` with a full stop, which `sweep_figures`' heading reader does not match; `points.py` reads it.

**The credit.** `cobol points-batch` (`ParseNodeReader`) parses each test under its edition and prints, per parse node of the rules a
row names, its tokens: a nested statement is one token, a longer operand context is one token, a one-token operand is its token.
`coverage.Matcher` chart-parses each node whole against each format of the rows that name its rule and credits the points on a
derivation. Re-probes while building it moved the numbers: crediting from node text without nested entries, without the context
words a clause row prints before its keyword, with function names kept (`FUNCTION COS` lexes COS a user word) and with the
subscripts of a reference kept (`--flat`) took the credit from 1,851 to 2,882 points; deduplicating an optional word that is also an
alternative took 85 duplicate points out of the catalog. The record is `wiseowl/points/<test id>.json` as point-number ranges (ids
weighed 16 MB; ranges 1.7 MB) with `wiseowl/points-inputs.json` holding the catalog's digest, the reading's, and each test's text
digest; `coverage.unmet` raises rather than count a record that is not a measure of the tests as they stand.

**Measured.** 3,846 points; 2,852 credited, 994 owed. The rows no parse node carries keep every point owed and the report names
them: COPY and REPLACE (the preprocessor reads them), a section header, a nonnumeric literal (one lexer token), the four
division-structure rows. Alternatives whose operands share a token class (`identifier-1` and `index-name-1`) are credited together
until the operand-kind map (S15's remainder) names their grammar elements.

**Tests.** Self-tests `selftests/formats` (12 cases), `points` (10) and `coverage` (16: the matcher on planted formats, the operand
kinds, the frames, the record's encoding, the loud failures); `PointsBatchTests` (9, the reader and the verb). No golden: nothing
compiled changes. **Self-review** (architecture, full code, performance, duplication): one mechanism per job (reading, points,
credit, parse reading), no C# text in a bound node, the grammar's rule names owned by the caller so none is written into the compiler;
the matcher memoises its chart and the record dedupes node shapes (4,565 of 113,668 nodes). **Gate:** the verdict line is in the
branch report.

### PB6054: every GnuCOBOL 85 failure has its cause, and suite 85's second-compiler term is 0

kb/Work PB6054 (suite 85 Stage 0, the second-compiler denominator) was `half`: 211 of the 484 GnuCOBOL failures at
`-std=cobol85` were still `unclassified`, so `suite_status.py --edition 85` printed `second compiler 211`. This
continuation (wave 20262143, group D) carries the PB6050 branch (the format-point catalog, its own pending entry beside
this one) and lands both.

**Re-probe.** The 211 were re-run on cobc 3.2.0 under WSL through `harness/run.py --compiler gnucobol --keep`: all 211
still FAIL. Each kept binary was re-run in a fresh directory to capture its whole output, which was diffed against the
test's `.out` with non-printing characters shown (NUL lines that look like spaces are cobc's undefined initial values).

**Adjudication.** Each diff was read against the 1985 rule the test names, and the source where a leg turned on it. 176
are `gnucobol-nonconformance`, each citing the rule cobc's output breaks on a line that rule fixes, every citation
checked with `cite.py --check` and a quotation; 35 are `non-portable-test` (a leg the 1985 text leaves to the implementor:
the STOP literal's operator device, the Debug module's object-time switch, the optional Communication module, undefined
initial values, which fixed file attributes OPEN validates, two connectors on one physical file, the native character
set's size, the default sign representation, print-file and report-record representation, the ACCEPT transfer size,
the truth value of a relation with a size error). None is a `wiseowl-expectation-defect`. Report Writer accounts for 81
of the 211: cobc's LINE-COUNTER reads 0 in unpaged reports, its sum counters read by data-name are NUL (never zeroed by
INITIATE), SUPPRESS PRINTING fails at run time, GROUP INDICATE is ignored, no page advance happens at PAGE LIMIT, and
entries without COLUMN NUMBER are printed; positional diffs were re-derived from each RD's PAGE, LINE and NEXT GROUP
integers. `measurements/gnucobol.json` now holds 284 gnucobol-nonconformance and 200 non-portable-test of 484;
`suite_status.py --edition 85` prints `rows 2961 · points 994 · NIST 6931 · second compiler 0 · transcription UNMEASURED`.
The per-test causes and reasons are `kb/Work/PB6054/2026-11-22-cause-adjudication-complete.md`; PB6054 is `landed`,
`closes_rows: []` (a measurement slice).

**The PB6050 review's leads, fixed here.** R1-1: `coverage.inputs_of` digested only the operand rules and frames, so a
record measured under another row-to-frame mapping or matcher passed `unmet`; the reading is now `coverage.reading_of`
(the frames, `FRAME_OVERRIDES`, `NO_FRAME`, `SUBJECT`, the matcher's tables and the source of `frames_of`, `operand_ok`,
`token_of`, `Matcher`), and `wiseowl/points-inputs.json` was re-recorded (every per-test record unchanged, 994 owed).
R1-4: the per-row report read the record without `unmet`'s checks; it now runs them first and fails loudly. R1-2: the
FIGURATIVES comment cited a clause that does not exist; it now cites §1985:IV.4.2.2.2.3. R1-3: the stale docstrings of
`coverage.py` and `points.py` and the unused `OPERAND_KINDS` are corrected or gone. R1-5: the `points-batch` verb is in
the command-line manual and the README, and the doc facts now list every verb's options
(`DocumentedFactsDriftTests.Facts`), so the manual audit holds the batch verbs' options as it holds the compile's (the
first gate's DOC AUDITS red: the page documented `--frames` and the facts knew only the compile's options). Two self-test cases pin R1-1 and R1-4 (coverage self-test 18 cases, PASS).

**Design.** `DESIGN-conformance-suite.md` section 5 states how a cause is set and the final counts, section 3.4 what the
reading digest holds, and section 8's "Today" line reads `points 994 · second compiler 0`.

