- **D-VARYING-AFTER-ORDER — PERFORM VARYING … AFTER under TEST BEFORE — the reset/augment ORDER (§2023:14.9.28.4 GR13 e) 2 from 2002; §1985:VI.6.21.4
  GR10 d) 1 at 1985 orders it the other way, per edition)**: GR13 e) 2's true branch is ordered "a. the induction variable associated with the
  current condition is set to its initialization value", then "b. the condition to the left of the current condition
  becomes the current condition", then "c. the induction variable associated with the new current condition is
  incremented by its associated augment value". GR12 does item identification for the FROM operand "each time … is
  used in a setting or augmenting operation" and GR13's closing paragraph gives every such change "immediate effect",
  so an AFTER level whose FROM reads the level to its LEFT is reset from that level's **pre-augment** contents. For
  `PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3 AFTER B FROM A BY 1 UNTIL B > 3` the rule therefore runs the body
  **eight** times — (1,1)(1,2)(1,3)(2,1)(2,2)(2,3)(3,2)(3,3), leaving A=4 and B=3. **NIST NC201A PFM-TEST-F4-23
  ("ORDER OF INITIALISATION OF VARYING IDENTIFIERS", referenced to the 1985 text as `VI-114 6.20.4 GR10(d)1`) asserts
  SIX** — i.e. the opposite order, augment-then-reset, which is the 1985 text's order.
  **Pinned choice: PER EDITION, each edition's own text (kb/Work PB436, PB5101).** From 2002 (the 2002 transcription
  words GR13 e) 2 as 2023 does) the ISO 2023 text governs: reset-then-augment, **eight** bodies. At `--std 85` the
  1985 text governs: §1985:VI.6.21.4 GR10 d) 1 says "the content of the data item referenced by identifier-2 is
  augmented by literal-2 or the content of the data item referenced by identifier-4" and THEN "the content of the data
  item referenced by identifier-5 is set to literal-3 or the current value of the data item referenced by
  identifier-6" (Figure 2 draws the same order), so the outer variable augments first and the same nest runs **six**
  bodies, leaving A=4 and B=4. The binder records the order in `PerformVarying.AugmentOuterFirst` (edition year below
  2002) and `ControlFlowEmitter.EmitVarying` emits the two operations in that order. NC201A's golden records
  `PASS PFM-TEST-F4-23` at `--std 85`; from 2002 the check FAILs, and that report is the per-edition golden
  `tests/nist/valid-editions/2002/NC201A.txt` its corpus row declares (`BEHAVIOR perform-varying-after-init-inner-first`),
  which the INV-1-strong run at 2023 compares against. No `valid/` golden carries a CCVS failure now (the `CCVS-DEFECT`
  allowance remains for a future one). The TEST AFTER arm is unaffected and is **not** symmetric: GR13 c) 4 (and the 1985 text) state the
  increment BEFORE the reset, and that arm implements exactly it. Witnesses:
  `conformance:cases/14.9.28-PERFORM/pb436_varying_after_from_outer` (2002 and later);
  `conformance:cases/14.9.28-PERFORM/pb5101_varying_after_from_outer_1985`;
  `SpecPinnedNistTests.NC201A_VaryingAfterFromOuterInductionVariable_At1985_RunsSixBodies` and
  `NC201A_VaryingAfterFromOuterInductionVariable_From2002_RunsEightBodies` (2002, 2023).
  (kb/Work PB436, PB5101.)
