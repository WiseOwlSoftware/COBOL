## EC-FLOW-GLOBAL-EXIT is set at 2002 and 2014, the 2002 omitted-argument and function-not-found conditions carry the 2002 names, PB5751 retired

Wave 20262150, group E (PB5715, PB5751, PB6107): three notes about one subsystem, the exception conditions an edition's text names.

**PB5715 — EC-FLOW-GLOBAL-EXIT.** Re-probe at `--std 2002` (the note's `global_exit.cob`, free form): `IN-GLOBAL-DECL FS=35`, `D-X-EXITING`, `SUB-BACK`, exit 0, where
§2002:14.8.13.3 GR2 (and §2014:14.9.13.3 GR2) set the fatal condition. `docs/conformance/3/D-GLOBAL-EXIT.md` had applied the 2023 finding, that no rule sets it,
at every edition. Fix: a new `DialectBehavior.FlowGlobalExitIsNeverSet` (changed in 2023, VCR row 7.118); `EcBinder.NodeKindRaises` declares the name for
`BoundExitProgram` and `BoundGoback` unless that behaviour is active (`NodeKindRaise.NeverFrom`, `EcRaiseScope.EditionYear`); `CallEmitter.EmitExitProgram` and `EmitGoback`
write `ExceptionState.FlowGlobalExitError` behind the same run-time `__useActive` range test as EC-FLOW-GLOBAL-GOBACK (`CallEmitter.EmitFlowGlobalExit`); the runtime
gained the `FlowGlobalExit` flag and the helper pair; `EcEmitter.FatalAmbientGates` gained the row; the name left `declined-raise-sites.json`. EXIT FUNCTION is bound as the
GOBACK it equals, so it shares the arm; EXIT METHOD is unreachable until a method may carry declaratives. After the fix the probe ends with `abnormal run-unit termination:
EC-FLOW-GLOBAL-EXIT (fatal)` at 2002 and 2014 and runs on at 2023. The note's second probe (`global_fn_goback.cob`) writes its `>>TURN` after the declarative that holds the
GOBACK, and §2002:7.2.19.3 GR4 enables checking only for the statements that follow the directive, so it is silent by the standard; with the directive first it terminates at 2002.
Goldens: `pb5715_exit_program_global_declarative_range` and `pb5715_goback_in_function_global_declarative_range` (2002), `pb5715_flow_global_exit_refused_85`. Rows closed:
`2002-GR-14.8.13.3-2`, `2002-GR-14.8.17.3-3` (GAP 1161 to 1159).

**PB6107 — the 2002 names.** §2002 states one omitted-argument condition, EC-PROGRAM-ARG-OMITTED, for a program, a function and a method (§2002:14.8.4.3 GR12, §2002:8.4.2.2.3 GR7,
§2002:14.8.22.3 GR10) and the function locate miss as EC-PROGRAM-NOT-FOUND (§2002:8.4.2.2.3 GR5); §2014 (E.2 item 18) split them. The compiler raised the 2014 names at every
edition, and `>>TURN EC-FUNCTION-ARG-OMITTED` is refused at 2002 (COBOL0878), so no 2002 program could enable or handle what was raised. Fix: one table,
`ExceptionCatalog.NameInEdition`; `EcBinder.EcWrap` maps each declared name through it, `EcBinder.MarkFormals` queries `>>TURN` for the edition's name and stamps the guard with
`OmittedFormal.RaisedKind` (the program kind at 2002), and the activation emitters pass the edition's miss name with a new `asFunction: true`, which replaces the runtime's reading of
function-ness off the spelling of `notFoundEc`. `SET ... TO ADDRESS OF FUNCTION` needs `USAGE FUNCTION-POINTER`, refused below 2014, so that arm keeps its 2014 names. At 2002 the
function probe (`fn_omit_2002.cob`, from the 2014 golden with the program name) prints `F-CAUGHT 01=EC-PROGRAM-ARG-OMITTED`. Tests: golden `pb6107_function_arg_omitted_2002` with its
refused twin, `ArgOmittedConditionTests.ElementaryReference_At2002_RaisesEcProgramArgOmitted`, `InterProgramFileDifferentialTests.Udf_AbsentFunctionDefinition_RaisesTheEditionsLocateMissCondition`
(2002 and 2014; the old test pinned the 2014 name at 2002), `EditionExceptionNameTests`. Rows closed: `2002-GR-8.4.2.2.3-7`, `2002-GR-8.4.2.2.3-5`, `2002-GR-14.8.22.3-10` (GAP 1159 to 1156).

**PB5751 — retired, not a defect.** The note read the phrase branch of §2002:14.6.4 and the informative E.15 as "no condition, no last exception status". The normative text of all three
editions says the EC-SIZE exception condition exists with the phrase written and only the declaratives are skipped; `FUNCTION EXCEPTION-STATUS` naming EC-SIZE-TRUNCATION inside the
phrase is what the existing 2023 golden `pb74_sdidi_store_size_error` pins. Addendum `kb/Work/PB5751/2026-11-23-not-a-defect.md` records the citations and the one-arm change should the
owner rule for E.15.

**Self-review.** The EXIT PROGRAM raise precedes `__asCalled` (GR2 sets the condition when the statement is executed, GR5's CONTINUE follows). A GOBACK raises it only when it returns to an
activator (`__asCalled`, true in every function activation); a main program's GOBACK is STOP and sets nothing. With checking off the helper returns and the statement proceeds, the recorded
determination GOBACK's twin already makes.
