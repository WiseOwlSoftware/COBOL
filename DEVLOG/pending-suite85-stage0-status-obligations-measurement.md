## Suite 85 Stage 0: the status line, the 1985 row obligations and the second-compiler measurement (PB6047, PB6048, PB6054)

Revision 4 of the conformance-suite design counts the 85 suite on the 1985 catalog, so Stage 0 builds the three tools that
measure it. All three are Python in `conformance-suite/harness/`; no compiled program changed.

**PB6048, `obligations.py`.** It gives each of the 3,109 rows of the 1985 catalog its section 6.1 obligation and the first
witness kind that holds: non-support 9, suite 24, derivation 115, accepted-uncleared 1,974 (2,276 after the repair), refusal-only
340, and the 647 rows no header names. Those 647 split mechanically: 302 `header-omits` (the inventory `test-ref` names a test
that accepts 85 alone whose header lacks the row), 224 `shared-test` and 121 `no-test`. The first attempt wrote all 526
omitted pairs into headers and the generator refused 461 tests: a test that accepts 85 and 2002 cannot name a 1985 row, whose
band is 85 alone (a 1985 row's test runs at 85 alone, design 12.1). So `shared-test` rows are authoring work for Stage 3, and
`--repair-headers` wrote the 371 names of the 302 rows into 178 headers. `generate.py --check` stays GREEN at 11,098 tests.

**PB6054, `measurements.py`.** A baseline may name only tests of the edition's suite, and 1,742 of the 1,762 tests the 1985
rows name are not cleared, so the verdicts live in `measurements/gnucobol.json` (verdict per test; bucket and cause per
failure) and the 20 cleared tests' verdicts also reach `baselines/gnucobol.json`. The 1,624 verdicts are the 2026-11-22 run
(no conformance-suite file had changed since 608ccf507c, so it was not repeated); the 138 tests the header repair added ran
on cobc 3.2.0 under WSL at `-std=cobol85` (PASS 95, FAIL 43). Totals: PASS 1,271, FAIL 470, NOT APPLICABLE 7, NO VERDICT 14;
484 failures in 165 buckets. Every cause is `unclassified`: classifying a bucket as a GnuCOBOL non-conformance (with its 1985
citation), a non-portable test or a WiseOwl expectation defect is Stage 2's adjudication, so PB6054 is half landed.
`generate.py --check` refuses a measurement naming a test of no population, a failure with no bucket, a cause outside the
three and a citation off the held text (two planted arms in its self-test).

**PB6047, `suite_status.py` and the report.** `generate.py`'s completeness report counts an edition whose own text is held
on that catalog, split as design 4.1 asks, instead of the 2023 rows banded at 85 (33 of 1,870). `suite_status.py --edition 85`
prints each term and one line. Verdict now: `=== SUITE 85: INCOMPLETE — rows 2961 · points UNMEASURED · NIST UNMEASURED ·
second compiler 484 · transcription UNMEASURED ===`. A term whose tool does not exist prints UNMEASURED, never 0; a tool that
exists and cannot answer exits 2. The self-test plants each term red. Design section 13's sentence that every revision-3 slice
is blocked by the one before is corrected (S8 waits on the pilot, S5 on Stage 4).

**Sweep and self-review.** Sibling: every other consumer of `Inputs.older` (the 2023 band loop, `rows`, `suite_ids`) was
read; the 2002 catalog now also prints on its own rows (header-omits 451, shared-test 315, no-test 1,436), which the plan
wanted per edition anyway. Architecture: `generate.py` stays the one reader of headers, the new modules take parsed tests and
return facts. Code: obligations follow section 6.1 row by row; an unknown row kind raises. Duplication: one witness
classification, used by the report and the status line. Performance: one pass over the cases per call.
