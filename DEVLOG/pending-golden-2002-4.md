## golden-2002-4: 108 own-edition witnesses at --std 2002 from 18 gl2002 files; 86 CONFORMS 2002 records; GAP 2169 -> 2084

The fourth landing of the 2002 golden lane (gl2002, kb/Work PB5597). It integrates 18 of the lane's 85 writer files:
6.1.2 floating indicators, 7.1.3 REPLACE, 7.2.19 TURN, 8.3.1.2.1 alphanumeric and 8.3.1.2.4 national literals, 8.7.5
relational operators, 11.6 METHOD-ID, 12.2 configuration section, 12.2.5 OBJECT-COMPUTER, 12.2.6 SPECIAL-NAMES,
12.3.4.10 PADDING CHARACTER, 13.3 file section, 13.9 constant (two files), 13.13 report group description, 13.16.15
CONTROL, 13.16.25 GLOBAL and 13.16.38 PICTURE (part 2). It follows the second landing's method.

**Shape.** The 18 reports hold 92 rows. By work kind: needs-own-witness 61, refused-evidence 21,
refused-needs-accepting-case 9 and own-verdict-from-pointer 1. By disposition: new-golden 86 and not-closable 6 (the
fixers' REPORT.json files moved two rows from new-golden to not-closable). The refuters overturned 25 verdicts:
convention 12 (header open marks, PROGRAM-ID collisions, a false header claim), does-not-exercise-rule 7,
expected-value 2, citation 2, nondeterministic 1 and evidence-kind 1. The fixers applied every one and refused none. No
writer deferred a row.

**Landed.** 108 cases: 51 accepted at [2002] and 57 refusals (`refuse-at: ["2002"]`). No separate validation stage
ran, so every case was validated on this branch's build of main d077329121: `run.py` at 2002 on a manifest of exactly
the 51 accepted cases gave PASS 51 and FAIL 0, and all 57 refusals were refused at --std 2002 with their expected codes.

**Records.** `record_verdicts.py --edition 2002` wrote 86 CONFORMS and the 2002 GAP went from 2,169 to 2,084. Each
test-ref was checked mechanically: accepted at 2002 in the generated wiseowl manifest, or a refusal whose `refuse-at`
holds 2002; a refusal-only record was kept only for an SR or FMT rule whose text grants no form. One record,
2002-SR-13.3.5.2-3, replaced an existing CONFORMS record and kept its witness; its new note said WRITE FILE naming an SD
is not refused at 2002, which is false (pb5579-write-file-sort-merge-file is refused COBOL1692 at --std 2002), so the
note was corrected.

**Mechanical repairs** (no expectation changed):
- 6 PROGRAM-IDs collided with cases already on main or with each other and were renumbered (G2CAT02, G2COL03,
  G2SPN04-06, G2SRC03). Merging main after golden-2002-3 landed brought 3 more collisions, renumbered G2REL03, G2SRC04
  and G2SRC05.
- 2002-SR-12.2.2-2 was recorded by both this landing and PB5609 (DEVLOG 3701); the merged record keeps both witnesses.
- On the tree merged with main 1d13746fce the 2002 GAP is 1,991.
- docs/conformance DOC-A.1-204 now names g2tn05_with_location in its `Pinned by`.

**Defects** (both repros re-run on this build):
- kb/Work PB5698 (new): FUNCTION EXCEPTION-STATEMENT is 63 characters at every edition; §2002:15.27.2 RV1/RV2 and
  §2014:15.29.2 RV2 make it 31. Blocks 2002-RV-15.27.2-1 and -2.
- kb/Work PB5580 (dated finding): the multiple-inheritance refusal COBOL0849 at --std 2002 blocks the inherited-FINAL
  leg of 2002-SR-11.2.2-6, already in the note's rows.

**Not closable (6 rows):**
- 2002-GR-7.2.16.3-2: PAGE directive page ejection needs a source listing, which the compiler does not produce.
- 2002-GR-8.1.2.1-5: substitute graphics are permitted only where a graphic is missing, and none is.
- 2002-SR-11.8.5.2-1: no accepting program can exist at 2002, since WiseOwl defines no entry-convention name
  (DOC-A.1-64).
- 2002-GR-12.3.4.10.3-1 and -3: no WiseOwl file takes padding (docs/conformance/3.6), so GR1's branch and GR3's choice of
  value are unobservable.
- 2002-GR-13.16.15.3-6: a zero-length CONTROL group needs a variable-occurrence entry, which SR5 forbids, so only a
  refusal could show it, and a refusal cannot witness a GR.
