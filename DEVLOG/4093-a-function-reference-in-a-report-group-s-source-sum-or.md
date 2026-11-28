## Entry 4093 — 2026-11-27 23:51 PST — A function reference in a report group's SOURCE, SUM or PRESENT WHEN raises EC-ARGUMENT-FUNCTION at GENERATE and TERMINATE

**Note:** PB6349 (wrong answer, silent). §2023:15.3 AR14 attaches EC-ARGUMENT-FUNCTION to the function reference with no statement or
position qualification. A GENERATE evaluates a report's SOURCE operands, SUM addends and PRESENT WHEN conditions
(§2023:14.9.16.4 GR1, §2023:13.18.53.4 GR2, §2023:13.18.54.4 GR3, §2023:13.18.41.4 GR2) and a TERMINATE evaluates the control footings and
report footing it prints (§2023:14.9.46.4 GR3). The operands live in `ReportModel`, which the statement holds by reference and the generated
`OwnValueParts` walk does not enter (the model is a graph that cycles back to itself), so the landing before this one (PB6223, PB6328) named
it as the drift test's one exception.

**Re-probe on the unchanged tree** (`>>TURN EC-ARGUMENT-FUNCTION CHECKING ON`, `FUNCTION LOG10 (W-Z)` with W-Z = 0, a declarative that prints
CAUGHT): SOURCE, SUM and PRESENT WHEN each printed their label and no CAUGHT. Beyond the three the note names, the same silence held for a
SOURCE whose subscript is a function, a user function's argument, a CODE clause subscript, a control footing's SOURCE at TERMINATE, and a SUM
of another report whose UPON names this report's detail. After the change every one of them prints CAUGHT at 2002, 2014 and 2023.

**Fix shape.** `ReportValueWalk` (new, `Binding/ReportValueWalk.cs`) walks the report description by containment, each model node entered once
through a reference-equality set, and yields the value nodes it holds: line, field, SUM and occurrence PRESENT WHEN conditions, SOURCE operands
and expressions, SUM addends, VARYING FROM and BY, and the value parts of the operand-evaluation steps a SOURCE or CODE clause runs (the
subscript store or function activation, a statement bound outside the procedure division that runs inside the GENERATE). The references that
name a part of the description rather than hold one (`Family`, `Report`, `Group`, `PrintedIn`, `ReportSumTerm.Upon`) are not followed.
A SUM term of another report that names a detail of this report in UPON runs at a GENERATE of that detail (§2023:13.18.54.4 GR7 c) 2));
`ReportSectionBinder.RecordUponTriggers` records it on the owning report (`ReportModel.UponTriggeredTerms`) once the UPON operands resolve.
`EcBinder.OperandRaisableNames` asks `ReportValueWalk.EvaluatedBy(statement)` (GENERATE and TERMINATE yield, every other statement none) beside
`OwnValueParts`, so `PartHasIntrinsic` and `OperandsIn` keep no report arm.

**Pins.** `ReportValueWalkDriftTests` replaces `BoundValueWalkDriftTests.ModelsHeldByReference`, a name list that excused two types: the
description types are now derived by reachability from `ReportModel`, every property of every one that holds a value node, a step list or
another description type is populated with a sentinel and must be read by `ReportValueWalk.Holds` (removing the `ReportSumTerm` or `From/By`
arm fails four tests), each bound node holding the description carries a decision (GENERATE and TERMINATE evaluate it; INITIATE, SUPPRESS and
a counter reference do not), and twelve end-to-end positions assert `EcBinder.OperandRaisableNames` declares EC-ARGUMENT-FUNCTION.
Conformance `15.3-Arguments/pb6349_ec_argument_function_report_operands` (accepted 2002, 2014, 2023; eight positions) and its 1985 refusal
`..._below_2002` (COBOL0900, the `>>TURN` directive is a 2002 introduction). No inventory row was re-verdicted; PB6349 carried none.

