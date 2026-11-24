# PB3223, 2026-11-23: the operand-evaluation window has its own owner (wave 20262147 group B)

**Re-probe (base a23ed881c2 plus the w20262139d replay).** Structural: `UdfBinder` still held `Pending`, `PendingMark`,
`Mark`, `DrainPending`, `OperandSteps`, `DrainPerEvaluation`, `UdfWrapCalls` and both `UdfAttachPerEvaluation` overloads,
reached as `host.Udf.*` from `StatementBinder`, `ExpressionBinder`, `ConditionBinder`, `ControlFlowBinder`
(+`.PerformVarying`), `CorrespondingBinder`, `EvaluateBinder`, `ReportWriterBinder` and `SearchBinder` (the `git grep` of
`host\.Udf\.(Mark|UdfAttachPerEvaluation|DrainPerEvaluation|OperandSteps)|UdfBinder\.PendingMark` listed eight files), and
`UdfBindExitFunction` lived beside them.

**Fixed.** `OperandEvaluationWindow` (`src/WiseOwl.Cobol.Compiler/Binding/Procedure/OperandEvaluationWindow.cs`, reached as
`StatementBinder.Window`) owns the mark of both pending lists, the one drain and its operand-evaluation stamp, the statement
hoist (`Hoist`, was `UdfWrapCalls`) and the two per-evaluation attachments (`AttachPerEvaluation`, was
`UdfAttachPerEvaluation`). `UdfBinder` keeps function activation only (`UdfBindCall`, `UdfBindPointerCall`, `UdfActivate`,
`UdfArg`) and registers on the pre-op list. EXIT FUNCTION is `ControlFlowBinder.BindExitFunction` beside the rest of
`BindExit`. Every caller changed; no forwarder. Drift tests re-pointed: `OperandActivationDriftTests` (the stamp lives in the
window's file), `ImplicitStatementSeriesDriftTests` (the hoist is `Hoist`). Nine inventory rows re-sited by record_verdicts
(six 2023, three 2002; GAP unchanged). DESIGN-architecture-review section 8.3 names the window's owner. Behavior-neutral:
the bound nodes, their order and the emitted text are unchanged (the moved bodies are byte-identical apart from names).

**Left as is.** The bound node names `BoundUdfEvaluated` / `BoundUdfEvaluatedExpr` still say "Udf" though they carry any
per-evaluation step; renaming them touches the emitters and the condition renderer, a separate behavior-neutral rename.
