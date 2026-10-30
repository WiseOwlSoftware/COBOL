## Entry 2793 — 2026-10-30 11:48 PDT — A refused >>DEFINE reports once, and a quoted compile-time operand keeps its spaces (PB3119, PB3429)

Two diagnostic-quality defects in the one compile-time expression evaluator (wave 20261769, group D). Neither changes
what a correct program compiles to; both made a wrong program's error list longer or harder to read than the mistake.

**PB3429 — COBOL1547 quoted `FOO OF X` as `'FOOOFX'`.** Re-probe on the wave's build: `01 C CONSTANT AS FOO OF X.` at
`--std 2023` answered `'FOOOFX' — all operands of the compile-time arithmetic expression …`. The evaluator quoted a
parse node with `GetText()`, which joins the tokens with no separators. `WrittenSource.Of` (the one "node as the
programmer wrote it" reader, already used by `ArithmeticFormationRules` and the binders) replaces it at every site that
quotes a multi-token operand: the qualified-name arm and the primary arm of `EvalArith`, `NonNumericOperand`,
`BooleanLeaf` (two messages), `ShiftCount` and the bare-operand arm of `EvalRelationOrBoolean`. The sweep found one
sibling outside the evaluator, the CONSTANT binder's "literal-1 shall not be a figurative constant" message
(`ALL "X"` printed run together), which reads `DataBinder.WrittenText` now.

**PB3119 — a refused `>>DEFINE` cascaded.** Re-probe: `>>DEFINE FOO AS` (also `AS 1 +` and `AS 'A' OFF`) reported the
DEFINE, then one more error at every later use of `FOO`: "not a previously-defined compilation variable" at a
comparison operand and a boolean operand, a "all operands … shall be fixed-point numeric literals" at an arithmetic
operand, and (found by the sibling sweep) "defined before the directive" at a `>>COBOL-WORDS` literal slot. PB1367 had
meant a malformed line not to cascade by not applying it; the root was that a refusal left no trace, so a name it wrote
could not be told from a name nobody wrote. The standard fixes the refusal (§7.3.11.2) and leaves the recovery to the
implementor; the choice is one report, at the directive.

The fix: `ApplyDefine` returns a refusal as a `DefineEffect` that names the variable (the omitted-branch and the
nameless cases still return nothing). The run records it twice, each for its reader. The live evaluator reads a refused
set that moves with the table (the DEFINE directive-state carrier now saves and restores both, so PUSH/POP act on it);
`CompileTimeExpressionEvaluator` takes it as the optional `isRefusedName` and fails silently at the four sites that
read a name. The later stages read the timeline: `CompilationVariableEvent` gained `Refused`, `DefinitionAt` passes
over such an event (a refused redefinition keeps the earlier value), and the new `StandingAt` reports it so
`CobolWordsDirectiveProcessor.Literal` stays silent. The CONSTANT binder passes no set (its failed constants already read
the recovery value `1`), and `CONSTANT FROM` needed nothing: the binder does not run after a preprocessing error.

**Goldens.** Refused programs `conformance-suite/cases/13.10-Constant/pb3429-constant-qualified-operand.cob` and
`conformance-suite/cases/7.3.11-DEFINE/pb3119-define-refused-then-referenced.cob` (both `refuse-at` 2002, 2014, 2023;
`diagnostics.json`); that the later lines add nothing is held by the unit tests, since a harness expectation names only
what must appear. Unit: `ConditionalCompilationDefineTests` (once-only for three refusal shapes with a control over an
unwritten name; a PARAMETER value past the digit capacity; a good redefinition ends the refusal; an omitted-branch
DEFINE refuses nothing; POP restores the refusals; the quoted operand in the arithmetic, boolean and shift arms) and
`CobolWordsDirectiveTests` (a literal slot over a refused name, a refused redefinition, an OFF after a refusal).
`generate.py --check` is GREEN. Rows: none claimed, none closed (both notes `closes_rows: []` with the reason).

**Self-review.** Architecture: one decision (a refusal names its variable) with two readers fed from the one
`DefineEffect`; no wrapper. Full code: the recovery sits where a name is read, not at each message; the cited rules
were run through `cite.py --check`. Duplication: the live set and the timeline event record the same fact for the same
reason `_defines` and the DEFINE events already do. Performance: the refused set is consulted only on a name that
already failed to resolve.

