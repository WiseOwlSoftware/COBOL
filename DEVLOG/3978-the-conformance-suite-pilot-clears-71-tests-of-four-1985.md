## Entry 3978 — 2026-11-23 05:35 PST — The conformance-suite pilot clears 71 tests of four 1985 clauses and the lint learns REPLACE

PB3178, the Stage 1 pilot of design revision 4, ran INSPECT (`VI.6.18@1985`), the PICTURE clause, COPY and REPLACE, 105 rows
of the 1985 catalog, with the real tools. Of the 86 tests the slice accepts at 85, 34 cleared as they were (the lint passes,
WiseOwl passes at 85, a GnuCOBOL 3.2.0 verdict is recorded), 21 after a mechanical rewrite and 11 after a lint fix; 20 stay.
The suite now witnesses 75 of the slice's 104 observable rows (2 before), and the catalog's rows with no cleared test fall
from 2,961 to 2,888.

**The rewrite.** 41 of the 52 tests the lint refused first displayed an unsigned numeric item, which the lint refuses because
the device form of a number is the implementor's (§2023:14.9.11.4 GR1). A scratch tool moved each such operand through a
numeric-edited item (`MOVE X TO X-E`, `X-E PIC Z9`) and ran the result at 85; it kept the rewrite only where the new output
equalled the test's own `.out` under zero suppression, so no expected output was copied from a run. 21 of the 41 cleared;
the rest hold a table item, a multi-line DISPLAY or a library text.

**The lint defect.** Eleven more tests were refused for text no compiler reads: `REPLACE ==A== BY ==DISPLAY X==.` was read as a
DISPLAY statement whose operand was `=`. `cobol_source.py` now applies a REPLACE statement to the text after it and a
`COPY ... REPLACING` phrase to the library text it copies, by the comparison cycle of §1985:XII.2.4 GR5 (`replace_text`,
`apply_replace_statements`); `generate.py --self-test` holds four arms (pseudo-text is no statement, a replacement introduces a
judged operand until REPLACE OFF, the first matching pair wins and a lowercase word matches, COPY REPLACING applies). The same
fix cleared nine REPLACE tests and two COPY tests.

**Tests written.** Five rows with no test got one from the rule text, each derived before it was run and each PASS on WiseOwl and
GnuCOBOL: `1985-SR-VI_6_18_3-1` (INSPECT of a group, an alphabetic and a numeric item), `1985-SR-VI_6_18_3-4-at-85` (one AFTER
and one BEFORE on one ALL phrase, the 85-only test the shared row needed), `1985-SR-VI_5_9_3-7` (an asterisk picture and BLANK
WHEN ZERO in separate entries), `1985-SR-XII_2_3-9` and `1985-SR-XII_3_3-7` (pseudo-text-1 holding a separator comma between
words). Each citation went through `cite.py --check`; the checker caught a wrong GR number (the asterisk is §1985:VI.5.9.4
GR8d, not GR6).

**The measurement and the second compiler.** `measurements.py record` replaced the whole edition's record from the results it
was given, so re-measuring tests a clearing rewrote needed a 45-minute run of every test. It now keeps the verdicts of tests
the results do not name (a test that left the population goes); a selftest arm holds it. The baseline gained 91 cleared
verdicts. One failure's bucket text was mojibake in the first record; the re-run gave the clean bucket and the cause and
citation adjudicated under PB6054 were set again unchanged.

**NIST.** 11 of the 74 INSPECT units of NC115A, NC216A and NC221A (NC115A F1-1 to F1-7 and F2-2 to F2-5) were adjudicated to rows
from each INSPECT statement's own phrases, `basis: adjudicated`; the status line's NIST term fell from 6,931 to 6,920. No
refuter ran; the cost table says so.

**What the self-review found.** The rewrite tool first accepted two tests whose output matched but whose lint still failed (a
subscripted table item; a `DISPLAY =` line): the gate-side lint is the check, so both were reverted to their residue. A
`git checkout` of the tree between two steps discarded an uncommitted clearing once; it was redone from the recorded results.

The cost per row, per test cleared, per test written and per NIST unit, and what the pilot did not exercise, are in
`kb/Work/PB3178/2026-11-23-pilot-measurement.md`. Gate: see the landing record.

Review round 1: a separator period inside pseudo-text no longer ends a COPY or REPLACE statement for the lint (`cobol_source._statement_end` steps over each `==` span), so a REPLACING or REPLACE pair after such a period is applied; selftest `selftests/cobol_source`. The REPLACE comparison rule is cited as §1985:XII.3.4 GR6b.

