## PB1369: the compile-time arithmetic determination is re-verdicted against the code, and the three places that still told the old story are corrected

**What the note claimed.** The registrar's batch 15a found that the Annex A.1 item 29 documentation (the mode of
compile-time arithmetic, §7.3.6.3 GR2, and the intermediate rounding, §7.3.6.2 SR2) contradicted the code: it called
`System.Decimal` a standard mode, said division truncates, named the deleted `EvalConstExpr`, and the evaluator
reported an over-range operand as an SR1 b) violation. The note still claimed four rows: GR-7.3.6.3-2, SR-7.3.6.2-2,
DOC-A.1-79 and DOC-A.1-80.

**What the re-probe showed.** Earlier landings had already done the work (DOC-A.1-29 rewritten by the doc-rows
integrator and PB1592, the operand reporting moved to SR2, DOC-A.1-79 and -80 closed by PB1376 and PB1375). On this
build, with `--source-format free`: `2 / 3 * 3` is 2, `100 / 7 * 7` is 100, `1 / 3 * 3` is 0 and `-7 / 2` is -3 at 2023
and 2014; the tie `0.0000000000000000000000000015 / 2 * 10000000000000000000000000000` is 8 at 2023 (nearest-even) and 7
at 2014 (standard arithmetic, nearest-away-from-zero); `>>DEFINE V3 AS 2 / 3 * 3` with `>>IF V3 = 2` selects; the
31-digit operand `1234567890123456789012345678901 + 0` and the product `9999999999999999999999999999 * 10` are refused at
2023 (COBOL1547 in a constant entry, COBOL1619 in a directive, both citing the documented SR2 limit) and evaluate at
2014 (`99999999999999999999999999990`). Every figure DOC-A.1-29 states is what the evaluator does.

**The fix.** No behaviour changed. GR-7.3.6.3-2 and SR-7.3.6.2-2 are re-verdicted CONFORMS (batch
`w20261759a-pb1369-ct-arith`, GAP 23 to 21) with the conformance cases `gl2m2_ct_arith_mode`,
`gl2m2-ct-arith-intermediate-overflow`, `pb1592_ct_arith_standard` and `CompileTimeArithmeticModeTests`. Three leftovers
that still described the retired evaluator are corrected: the `docs/CONFORMANCE.md` section 3 bullet said `System.Decimal`
for every expression (false at 2002 and 2014, where the mode is standard arithmetic) and now states both modes and points to
item 29; `docs/rearchitecture/DESIGN-compile-time-expressions.md` told the C2 defect story and named `EvalConstExpr`
three times and now describes the one evaluator, `EvalArith`; the section comment in `CompileTimeExpressionEvaluator.cs`
named the same deleted evaluator. DOC-A.1-79 and DOC-A.1-80 leave the note's `inventory_rows` (they were closed
by PB1376 and PB1375). `audit_annex_a1.py --check` reports no findings and no remaining obligations.

**Self-review.** The diff is documentation and one comment. Each sentence added to the section 3 bullet was checked against
`DOC-A.1-29` and against the probe values above; no second copy of the determination was created (the bullet points to
item 29 and states only the edition split).
