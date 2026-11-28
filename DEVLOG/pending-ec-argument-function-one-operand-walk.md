## The EC-ARGUMENT-FUNCTION operand gate reads every operand position through one generated walk

**Notes:** PB6223 (a function-identifier CANCEL target carried no guard), PB6328 (a class condition, a range membership and a
per-evaluation condition carried none), and PB6333 (the same two hand walks seen as duplication). One mechanism: the walk that
decides whether a statement holds an intrinsic, and so needs the EC-ARGUMENT-FUNCTION / EC-ORDER-NOT-SUPPORTED guard, missed
positions. §2023:15.3 AR14 attaches the condition to the function reference with no statement-kind or condition-kind qualification.

**Re-probe on the unchanged tree** (`>>TURN EC-ARGUMENT-FUNCTION CHECKING ON`, `FUNCTION CHAR(W-Z)` with W-Z = 0, a declarative that
prints CAUGHT): the relation raised; `IS NUMERIC`, a user class, an alphabet-name class and `CANCEL FUNCTION CHAR(W-Z)` printed
nothing. After the change every position prints CAUGHT, at 2002, 2014 and 2023.

**Fix shape.** Two parts.
1. `BoundVisitorGenerator` reads a tuple's elements wherever it reads a record's properties (`Members`, over
   `ITypeSymbol.TupleElements`, in `CollectValueSources`, `CollectSources`, `IsValueBearing` and `IsStatementBearing`). A `ValueTuple`
   declares fields, so the property walk saw none, and `BoundCancel.Targets`, `(string? LiteralName, BoundOperand? DynamicName)`, yielded
   nothing from `OwnValueParts`.
2. The generator also emits `BoundStatementTree.ValueChildren`, the value nodes each value node holds directly, from every property of
   every condition, expression, operand and boolean leaf. `BoundValueWalk.SelfAndDescendants` recurses over it. `EcBinder.PartHasIntrinsic`
   is "any node is a `BoundIntrinsicCall`" and `OperandsIn` is "every `BoundOperand`" over that walk. `ConditionOperands`,
   `CondHasIntrinsic`, `ExprHasIntrinsic`, `OpHasIntrinsic` and `BoolExprHasIntrinsic`, hand switches with `_ => false` defaults that
   disagreed, are deleted. The walk no longer stops at the operands it was told about: an intrinsic nested in a sign condition's
   expression, a boolean shift count or another call's arguments is found by the same descent.

**Pins.** `BoundValueWalkDriftTests` populates each value-holding property of each statement and value leaf with a sentinel node and
fails when `OwnValueParts` or `ValueChildren` does not yield that object, or when a property mentions a value node through a shape
the generator does not read (an array, a `List`, a dictionary). It also asserts `EcBinder.OperandRaisableNames` declares
EC-ARGUMENT-FUNCTION for eleven positions. Conformance `15.3-Arguments/pb6328_ec_argument_function_every_operand_position`
(accepted 2002, 2014, 2023; ten positions) and its 1985 refusal `..._below_2002` (COBOL0900, the `>>TURN` directive is a 2002
introduction). No row of the inventory was re-verdicted; PB6223 and PB6328 carried none.

**Registered, not fixed.** The report description models (`ReportModel`, `ReportGroupModel`) hold SOURCE, SUM and PRESENT WHEN
operands in `List<T>` properties of a cyclic graph, so `GENERATE` does not see a function reference inside a report group
(probe: `SOURCE FUNCTION LOG10(W-Z)` raised nothing). The drift test names those two types as its one exception. `UsageCollectionPass.Cond`
is a further hand switch over conditions with a default arm; no wrong answer was observed from it.

**Self-review.** Architecture: one walk, no C# text in a bound node. Code: the cited rule is AR14 as written, with no
condition-kind qualification, and the relation and per-evaluation arms now agree. Performance: bind time only; each part is walked
once per `OperandRaises` row, as the switches were. Duplication: five walkers and their shared default arm are gone. The
`PHASE4_RECONCILIATION` text that names the deleted walkers is a dated reconciliation record and was left as it was.

**Gate.** `build-local` at this branch's head; its verdict line is in the wave report.
