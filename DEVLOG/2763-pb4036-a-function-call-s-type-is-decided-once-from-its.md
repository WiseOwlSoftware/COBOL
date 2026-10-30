## Entry 2763 — 2026-10-29 19:56 PDT — PB4036: a function call's type is decided once, from its arguments as written

**What.** After PB3974 a function argument frozen for the left-to-right order (ISO §8.4.3.2.4 GR2) that is an integer arithmetic
expression is held in a scale-0 compiler temporary, and `ArgumentOrder.Window.Settle` swaps that temporary into the call's `Args`.
Every reader that asked `IntrinsicResultType.Resolve(call.Sig, call.Args)` then saw an integer data item where the expression it
replaced was not one, so one call had two types: `PERFORM FUNCTION MAX (A * B, 5) TIMES` was refused (COBOL1646, Numeric) and
`PERFORM FUNCTION MAX (A * B, FUNCTION UDF (A)) TIMES` compiled (Integer). Whether a LATER argument activated a user-defined function
decided the earlier ones' typing.

**Re-probe (base tree, own build).** The two PERFORMs above, `E:/COBOL-coord/scratch/mythos/w-n1/w20261762e/p3.cob`: first form refused,
second compiled.

**Fix.** The type is a property of the node: `BoundIntrinsicCall.ResultType` is asked of `IntrinsicResultType.Resolve` once, when the
node is built from the arguments as written, and `Settle`'s `with { Args = ... }` copies it. The six readers that re-resolved from
`call.Args` (`IsIntegerOperand`, the Index and Numeric/Integer screens in `IntrinsicArgumentRules`, `FunctionValueLane.IsInteger`, the
LENGTH fold) read the node. One rule for both arms; the freezing arm no longer has a type of its own.

**Tests.** `FunctionTypeSettlementTests` (five plain/frozen pairs: the plain and the frozen form compile alike or are refused with the
same codes) and `ArgumentOrderDriftTests.ACallsType_IsReadFromTheNode_NotResolvedAgainFromItsSettledArguments` (no source re-resolves a
bound call's type from `call.Args`). No conformance golden: the observable (an integer-only position over an always-integral expression
argument) turns on a classifier question this change leaves as it was, see below.

**Found, left (report leads).** (1) An arithmetic expression argument that always results in an integer (ExpressionExtent scale 0) is an
Integer argument by §15.3 type 6, so §15.59.1's "All arguments integer" row would make `MAX (A * B, 5)` Integer; `IsIntegerOperand` has no
expression arm and types it Numeric, and the integer-only positions refuse it. (2) A frozen argument's FunctionValueField description
differs from the unfrozen one: `LENGTH (MAX (A * B, 3))` is 4 and `LENGTH (MAX (A * B, UDF (A)))` is 8; a frozen nested `MAX (MAX (C, 5), UDF (A))` is 38
where the unfrozen is 8.

