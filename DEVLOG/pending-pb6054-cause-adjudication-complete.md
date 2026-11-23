## PB6054: every GnuCOBOL 85 failure has its cause, and suite 85's second-compiler term is 0

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
the command-line manual and the README. Two self-test cases pin R1-1 and R1-4 (coverage self-test 18 cases, PASS).

**Design.** `DESIGN-conformance-suite.md` section 5 states how a cause is set and the final counts, section 3.4 what the
reading digest holds, and section 8's "Today" line reads `points 994 · second compiler 0`.
