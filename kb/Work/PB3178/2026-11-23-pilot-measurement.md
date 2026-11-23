# PB3178 - the pilot's measurement (wave 20262148, group A)

Slice: INSPECT (`VI.6.18@1985`), the PICTURE clause (`VI.5.9@1985`), COPY (`XII.2@1985`) and REPLACE (`XII.3@1985`), 105
rows of the 1985 catalog (design §8 Stage 1). Every figure is from this session; the commands are named.

## What the slice held and what it holds now

| | before | after |
|---|---|---|
| rows witnessed by a cleared test (`obligations.py`, the slice's rows) | 2 | 75 of 104 observable (+1 derivation) |
| catalog rows with no cleared test, whole 85 catalog (`suite_status.py --edition 85`) | 2,961 | 2,888 |
| tests in the suites (`generate.py`) | 156 | 227 (66 cleared and 5 written) |
| NIST units owed (status line) | 6,931 | 6,920 |

The 29 rows left: 21 accepted-uncleared (20 tests in the residue below) and 8 refusal-only, which are violation tests the
design judges in the diagnosis layer at 85 (§8), never a FAIL.

## The three outcomes of the 86 tests the slice accepts at 85

| outcome | tests | share | how |
|---|---|---|---|
| clears as it is | 34 | 40 % | `portable: unverified` removed; lint-clean; WiseOwl PASS at 85; a GnuCOBOL verdict already recorded (7 FAIL, each with its recorded cause) |
| clears after a mechanical rewrite | 21 | 24 % | each `DISPLAY` of an unsigned numeric item goes through a numeric-edited item (`MOVE X TO X-E`, `X-E PIC Z..9`), the new output checked equal to the old `.out` under zero suppression (leading `0` to space, a space inserted for a one-digit item) |
| clears once the lint reads the right text | 11 | 13 % | the lint did not apply `REPLACE` or `COPY ... REPLACING`, so pseudo-text holding `DISPLAY` was read as a statement: a lint defect, fixed at its root in `cobol_source.py` (design section 3.3), self-tested in `generate.py --self-test` |
| stays | 20 | 23 % | 15 a numeric DISPLAY the rewrite does not reach (a table item `CNT (1)`, a multi-line DISPLAY, a test needing a library text or two program units), 3 unclassifiable names, 1 function result, 1 operand `.` |

No test differed in a way that was a WiseOwl defect or an edition difference: every outcome was a lint outcome. WiseOwl
passed every cleared test at 85 (`run.py --compiler wiseowl`, 34, 55 and 11 tests, all PASS). GnuCOBOL 3.2.0 at
`-std=cobol85`: 45 PASS and 10 FAIL of the 55 first-cleared tests, 5 of 5 of the written tests, 10 of 11 of the third
batch; every FAIL has its recorded cause (the 1 bucket whose text a mojibake garbled was re-bucketed with its cause and
citation unchanged).

## Cost

Tokens are not metered inside a session, so the proxy is tool calls (this implementer's, counted approximately, orientation separate); wall time
is the command's own. Orientation (the design sections, the harness, the notes): 25 calls, shared by everything below.

| step | calls | wall time | yield |
|---|---|---|---|
| lint a folder: strip the flag, `generate.py --check`, restore the refused (`clear.py`, scratch) | 6 to build, 1 per folder | 45-70 s per run over four folders | 34 tests cleared, 52 refused with the lint's own reason |
| record what a clearing changes: `coverage.py --record`, `generate.py`, `run.py` on WiseOwl and GnuCOBOL (WSL), `measurements.py record` | 4 per batch | 22 s + 17 s + 12 s + a few s | 3 batches |
| rewrite the numeric DISPLAYs (`rewrite.py`, scratch; the new output checked against the old `.out`) | 9 to build and tune | about 1 s per test (52 tests in 52 s, each compiled and run) | 21 cleared |
| fix the lint for REPLACE and COPY REPLACING | 8 | 25 s per gate-less check | 11 cleared |
| write a test from the text | 12 for 5 tests (rule text read, citation checked, file written, run on both compilers) | 30 s per run of the five | 5 rows with no test witnessed, about 2.4 calls per test |
| adjudicate NIST units to rows (`nist_map.py`, scratch, from each INSPECT statement's own phrases) | 9 | 1 s | 11 of the 74 INSPECT units of NC115A, NC216A and NC221A; the rest are `open` |

Per row closed: 73 rows for about 80 calls, 1.1 calls per row; per test cleared: 66 tests (34 + 21 + 11) for about 55
calls, 0.8 per test; per test written: 2.4 calls; per NIST unit: 0.8 calls for the 11 mapped, 9 calls of tool-building.
The mechanical outcomes (as it is, rewrite, lint fix) are 77 % of the accepted tests and cost under one call per test; the
written tests and the residue cost the calls.

## What the pilot did not exercise

- A refuter for the NIST adjudication and for a written test. The 11 mapped units name the rows their statement's phrases
  name (§1985:VI.6.18.4 GR2, 6, 7, 8, 10, 11, 12, 14, 15), `basis: adjudicated`; nobody re-derived them.
- The points half of subsumption (`coverage.py` credit): the status line's NIST term falls by the 11 units at row level.
- The 8 refusal-only rows' diagnosis layer, and the residue's 20 tests, which a second pilot would take.
- Format 3 and CONVERTING NIST units: the statement finder in `nist_map.py` took the nearest INSPECT before a unit's
  `PAR-NAME` line, which is wrong for a unit whose statement is a deleted or table-driven one (NC115A F2-1, F3-1..8);
  those units stay `open`.

## What this sets for the stop rule (design §8)

Stage 2 on the whole 85 catalog is mostly the mechanical outcomes above: of 1,913 accepted-uncleared tests, the slice's 77 %
clears with a rewriter this size, so the estimate for the rest is the residue's share (23 %) plus the authored rows. A
reviewer who wants the rewriter's reach beyond DISPLAY of unsigned items sees the 15 numeric residue tests: a table item
and a multi-line DISPLAY are the next two cases.

## Sites

`conformance-suite/harness/cobol_source.py` (`expand_copies`, `replace_text`, `apply_replace_statements`, `_split_on_by`);
`conformance-suite/harness/generate.py` (`_check_program`, the lint self-test); `conformance-suite/harness/measurements.py`
(`record`); `conformance-suite/harness/selftests/measurements/test_measurements.py`; `conformance-suite/cases/VI.6.18@1985-INSPECT-STATEMENT/`,
`VI.5.9@1985-PICTURE-CLAUSE/`, `XII.2@1985-COPY-STATEMENT/`, `XII.3@1985-REPLACE-STATEMENT/`; `conformance-suite/baselines/gnucobol.json`,
`conformance-suite/measurements/gnucobol.json`, `conformance-suite/wiseowl/points*`; `tests/nist/crosswalk/NC115A.json`.
