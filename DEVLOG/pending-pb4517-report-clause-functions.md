## PB4517: a function in a report clause runs at each evaluation, and PRESENT WHEN FUNCTION compiles

**The defect.** `PRESENT WHEN FUNCTION MOD (WS-N, 2) = 1` was refused COBOL0899 "not yet implemented", though
§13.18.41.2 Format 1's condition-1 is any condition. The re-probe on the branch base reproduced it.

**The root was wider than the refusal.** The report clauses bind before the first statement and outside every statement
(`ReportWriterBinder.BindReportGroupClauses`). A function-identifier, a function-bearing subscript or an inline invocation
in them still registers an operand-evaluation step on the statement-scoped pending list, and the FIRST procedure statement
hoisted it. The step then never ran when a group was presented. The refusal hid this for PRESENT WHEN only. Two siblings
were silent wrong answers on the same base: `SOURCE WS-T (FUNCTION MOD (WS-N, 2) + 1)` printed spaces on every line, and
`SOURCE FUNCTION user-fn (WS-N)` printed 0 on every line.

**The fix.** Each report clause operand is now its own per-evaluation window, drained by the one
`UdfBinder.DrainPerEvaluation`:
- A PRESENT WHEN condition becomes a `BoundUdfEvaluated`, whose IIFE runs in the presence probe.
- A SUM addend and a VARYING FROM or BY become a `BoundUdfEvaluatedExpr`.
- A SOURCE operand and the CODE identifier keep their steps beside the value.

`ReportWriterEmitter.EmitSteps` runs the steps at each evaluation: before the placement in the compose, inside the sum and
rolled-total closures, before each VARYING setting, and inside the CODE read. A BY with steps is stepped rather than taken
in closed form, because §13.18.64.4 GR3 b) adds arithmetic-expression-2 at each occurrence. A GENERATE or TERMINATE that can
reach such a report takes a `BoundActivationSite` (§14.9.33.4 GR2 a) 2.). `BindReportGroupClauses` throws if it leaves
anything on the lists, so a new clause operand that skips the window fails at compile time. The COBOL0899 arm, its catalog
entry and `ReportSectionBinder.HasToken` are deleted. `docs/REPORT_WRITER_DESIGN.md` and `docs/CONFORMANCE.md` A.4.11 now
describe the window. The A.4.11 sentence also listed the GLOBAL clause and GROUP INDICATE on a relative COLUMN as staged;
both are live, and the sentence no longer says otherwise.

**Goldens.** `conformance-suite/cases/13.18.41-PRESENT-WHEN/pb4517-report-clause-functions` (2002) covers every arm.
Its values are derived in the header: ODD/EVN/ODD, 02 04 06, "BIG" on the third line only, both SUMs 012, V = 08 10 12, and
CODE B/A/B. `pb4517-present-when-function-at-85` is refused COBOL0900 by the clause's own 2002 gate.

**Left open.** A report-clause activation takes the empty §14.9.18.4 GR1 b) activator profile, not the presenting
statement's. This is a lead in the wave report.
