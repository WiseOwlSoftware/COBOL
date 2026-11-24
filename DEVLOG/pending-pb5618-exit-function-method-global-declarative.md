## PB5618: EXIT FUNCTION and EXIT METHOD in a GLOBAL declarative are refused at 2002 and 2014

The 2002 read found that `EXIT FUNCTION` and `EXIT METHOD` inside a declarative whose USE statement carries GLOBAL compiled
and ran (the note's repro printed 7), although `EXIT PROGRAM` and `GOBACK` there were refused with COBOL2102.
§2002:14.8.13.2 SR2 states the prohibition for the EXIT statement in the program, function and method formats
(band FORMATS 2, 3 AND 4, checked with `cite.py --check`).

Re-probe on this build before the fix: the note's `exitfn-global.cob` at `--std 2002` compiled and ran. Cause: the
EXIT dispatcher routed EXIT METHOD to `OoBinder.OoBindExitMethod` and EXIT FUNCTION to
`ControlFlowBinder.BindExitFunction`, and neither asked `PlacementRules.RefusedInGlobalDeclarative`, which only the
EXIT PROGRAM arm and GOBACK asked.

Fix: both arms now ask the one predicate after their placement check. `EcRaiseSite.GlobalDeclarativeCite` names
§2002:14.8.13.2 SR2 at COBOL-2002 for an EXIT format (it printed the 2023 rule number there, EXIT PROGRAM included);
2014 and 2023 keep the 2023 citation. After the fix the repro is refused with COBOL2102.

Goldens: `g2-exit-function-in-global-declarative` and `g2-exit-method-in-global-declarative` (refused, COBOL2102; the
accepted forms are the existing `g2_exit_function_ends_function` and `g2_exit_method_ends_method`). Row
`2002-SR-14.8.13.2-2` is CONFORMS (GAP 1137 to 1136). The run-time EC-FLOW-GLOBAL-EXIT sibling is a separate rule
(§2002:14.8.13.3 GR2, landed by PB5715) and was not touched.
