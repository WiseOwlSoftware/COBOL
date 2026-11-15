## Entry 3692 — 2026-11-15 11:48 PST — golden-2002-2: 86 own-edition witnesses at --std 2002 from 12 gl2002 files; 87 CONFORMS + 2 DIVERGES 2002 records; GAP 2258 -> 2172

The second landing of the 2002 golden lane (gl2002, kb/Work PB5597). It integrates 12 of the lane's 85 writer files:
inspect-p2, communication-description, intrinsic-functions, cancel, stop, open, move-p2, initiate, same-as-p2,
redefines-p2, disable and qualification. It follows the first landing's method (DEVLOG for golden-2002-1).

**Shape.** The 12 reports hold 95 rows. By work kind: needs-own-witness 67, refused-evidence 16, own-verdict-from-pointer
9 and refused-needs-accepting-case 3. By disposition: new-golden 86, existing-golden 1, defect-suspected 1 and
not-closable 7. The refuters overturned 22 verdicts: does-not-exercise-rule 7, citation 6, not-spec-derived 4,
expected-value 3 and convention 2. The fixers applied every one, and none was refused. No writer deferred a row.

**Landed.** 86 cases: 50 accepted at [2002] and 36 refusals (`refuse-at: ["2002"]`). No separate validation stage ran,
so every case was validated on this branch's build of main 63ab0c4ece. `run.py` at 2002, on a manifest of exactly the
50 accepted cases, gave PASS 50 and FAIL 0. All 36 refusals were refused at --std 2002 with their expected codes.

**Records.** `record_verdicts.py --edition 2002` wrote 87 CONFORMS and closed 86 GAP rows: 2002 GAP 2,258 -> 2,172. Each
test-ref was checked mechanically: it must be accepted at 2002 in the generated wiseowl manifest, or be a refusal whose
`refuse-at` holds 2002. A refusal-only record was kept only for an SR or FMT rule whose text grants no form. Some
records also name existing cases as witnesses: pb1551 (two, for INVOKE SR19), pb416 (two, for MOVE SR6), pb3683 (for
SIGN SR1) and pb5210 (the existing-golden row of OPEN GR24). Each runs at 2002 in the role its rule needs.

**Mechanical repairs** (no expectation changed):
- 8 PROGRAM-IDs collided with existing cases and were renumbered (G2AMC03-04, G2DIS06-09, G2SGN03-04).
- 2 test-refs were re-cased to `8.4.2.2@2002-FUNCTION-IDENTIFIER`, the directory the cases are tracked in.
- 6 code-locations were split into the `path#Symbol` form.
- docs/conformance D-INS1 to D-INS4 now name the 2002 INSPECT overlap goldens in their `Pinned by`.

**Defects** (both repros re-run on this build at --std 2002):
- kb/Work PB5677 (new): HIGH-VALUE, LOW-VALUE or QUOTE into a noninteger numeric or numeric-edited item is filled by
  position, at every edition that allows the move.
- kb/Work PB5351 (extended): the 2002 text answers its open question with the integer reading (ALL "5" into 9(3)V99 is
  555.00). Row 2002-GR-14.8.24.3-4 is recorded DIVERGES naming both notes.
- kb/Work PB5678 (new): an edited sender moved to two or more receivers is edited again by the frozen temporary, at
  every edition, which contradicts the 2023 row GR-14.9.25.4-1 too. Row 2002-GR-14.8.24.3-1 is recorded DIVERGES.

**Not closable (7 rows):**
- 2002-GR-A.1.2.3-6 and -30 need a run unit the MCS scheduled, and a suite case cannot set the environment variable.
  The draft for -6 is held.
- 2002-GR-14.8.38.3-2 to -4 observe only the process exit status, which the harness does not judge.
- 2002-GR-14.8.19.3-8 and 2002-GR-13.16.47.3-4 have no testable requirement and are left to the registrar.

**Held:** `g2-subscript-cd-destination-no-table`. Its fixer moved it from row 2002-SR-8.4.1.2.2-2 to 2002-SR-A.1.2.2-11,
and no row of these reports records that rule.

