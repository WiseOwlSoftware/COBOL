## Entry 3216 — 2026-11-06 15:06 PST — kb/Work PB4810, PB4759: the data-name, ALL REFERENCES OF and cd-name USE FOR DEBUGGING triggers run

Both notes reproduced on the tree: every data trigger and every cd-name trigger was the staged COBOL1571 (CCVS DB201A on each data
trigger, DB205A on both cd-names). The branch the dispatch named, notes-transcription-diagrams, carried no PB4810 work: its two commits
had already landed on main as PR 1224 under rewritten history, so there was nothing to replay.

**The rules come from the 1985 text, not the notes' GnuCOBOL reading.** The owner's copy of X3.23-1985 (PDF pages 693-698, each rule checked
against the rendered page) states them: general rules 5 and 6 (ALL REFERENCES OF after every referencing statement; the item alone after a
statement that changes it; WRITE and REWRITE before the I-O after the FROM move; GO TO DEPENDING before the transfer; PERFORM after each
initialization, modification or evaluation), rule 7 (once per operand per statement, each imperative verb a statement of its own), rule 8
(the cd-name: ENABLE, DISABLE, SEND, RECEIVE with data, ACCEPT MESSAGE COUNT; PURGE is not listed, which the note's earlier text had wrong),
rules 11-15, 23 and 24 (DEBUG-ITEM). Where the text decides it replaces GnuCOBOL: SORT, MERGE, RELEASE and RETURN run the ALL REFERENCES OF
section, and a cd-name trigger fires. Where it is silent the owner's PB4808 determinations stand (a store an ON SIZE ERROR path skipped
still counts as a change). `docs/CONTROL_FLOW_DESIGN.md` D20 and `docs/CONFORMANCE.md` D-DEBUG-DATA and D-DEBUG-SUB record all of it.

**The shape.** The first design (a trigger call in each statement's lowerer) is a list of 119 nodes, so it was not built. A target is a
`BoundDebugTarget`; `StatementBinder.BindStatement`, the funnel every statement passes through, asks `DebugTriggerPlanner` which fires the
statement itself causes and wraps it, outside its EC wrapper, in `BoundDebugTriggered`. The planner reads the statement's references off the
bound tree structurally (`BoundReferences`, reflection over the stored fields of the bound records, never entering a nested statement) and its
stores through the existing total taxonomy, `BoundStores`, gaining `DirectStoreOf` as its third client. `DebugTriggerEmitter` places each fire
as one guarded call; the moments interior to a verb (before a WRITE's I-O, a RECEIVE's data arrival, a PERFORM's init, test and augment) are
held by the wrapper and landed by the verb's emitter, and a fire no emitter landed is an internal error. DEBUG-CONTENTS is declared as wide
as the largest target; DEBUG-SUB-1 to DEBUG-SUB-3 hold the occurrence numbers, outermost first (the numeric items of rule 10, S9999 SIGN
LEADING SEPARATE, image-carried: `+0003`). New diagnostics: COBOL6334 (a subscripted or reference-modified target,
syntax rules 10 and 12) and COBOL6335 (a target named twice, syntax rule 6). COBOL6336 is returned unused.

**Evidence.** CCVS DB201A now runs with no failing test (4 deleted by the program's own logic, 8 for inspection); before, it did not compile. Goldens
`pb4810-debug-data-name-triggers`, `pb4810-debug-write-go-to-perform`, `pb4810-debug-rewrite-and-failed-write` (the I-O exception paths: duplicate key, unopened file), `pb4810-debug-sub-and-name`, `wiseowl/pb4759_debug_cd_name_triggers`
(accepted at 85) and `pb4810-debug-target-subscripted`, `pb4810-debug-target-repeated` (refused), each expected value derived from the rules above;
`pb4713-debug-cd-name` is now accepted. DebugFacilityTests' staged-COBOL1571 expectations became behavior tests (46 pass).
DB205A compiles and runs but does not finish; the cause was not diagnosed (a communication-facility
matter, listed as a lead). The file-name trigger (PB4811) and the DEBUG-CONTENTS reference-modification gap stay open.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 22,342/22,342 · Unit 40,272/40,272 · Characterization 37/37 cases ran (skipped 0)`
at head a2c47e5314d6 (run 000003-20261106T195442Z). The first gate was RED on six Unit tests of the new code (a namespace literal in an emitted-name scan, two
drift lists naming the renamed PERFORM store sites, two backend-contract rows and a state field in the new walker, the diagnostic-facts file): the walker
became static with no state, the PERFORM store sites kept their names, the rows and the facts file were added. The seven new goldens' results are in the trx, all Passed.

**Branch review round 1 (seven blocking findings, all answered).** (R1-1) A statement that holds a nested transfer (`IF D = 1 GO TO P`, `ADD 1 TO E NOT ON SIZE ERROR GO TO P`)
lost its after-statement section, because the fire sat after the nested GO TO. `StatementEmitter.EmitStatement` now asks `DebugTriggerEmitter.LandBeforeTransfer` before every
`IBoundTransfersControl` leaf, which runs the sections the enclosing statements owe; a path that does not transfer reaches the ordinary after fire. (R1-2, R1-3) The reference walk was
not total (SORT and MERGE keys and CANCEL's tuple operands were missed) and read the lowered tree (MOVE and ADD CORRESPONDING members, a condition-name's conditional variable, the
RELATIVE KEY a START without KEY compares, the record READ INTO and RETURN send fired sections the written statement does not name). Nodes now name the operands the program did
not write (`IBoundImplicitOperands`), the walk reads tuples, `BoundSortMergeKey.Written` and `BoundCorresponding.SendingGroup`/`ReceivingGroup` carry the written operands. (R1-4) The INTO
phrase of READ and RETURN is its own moment, `AfterAction`, landed after the move, so an AT END that moves nothing runs no section; an EVALUATE WHEN that was never evaluated and the
operands a short-circuited condition never reaches are still counted as referenced (a lead: they need a per-phrase evaluated flag in the IF and EVALUATE emitters). (R1-5) DEBUG-SUB-n
is the numeric `S9999 SIGN IS LEADING SEPARATE` item rule 10 states, image-carried (`+0003`), one shared description: a comparison, COMPUTE and DISPLAY read it. The earlier alphanumeric
view was a determination where the text decides; D-DEBUG-SUB is rewritten. An unused member (spaces) is no number, so two pins read it through the DEBUG-ITEM group image. (R1-6, R1-7)
DEBUG-CONTENTS is the storage image of the whole referenced item (rules 24 c, 11, syntax rule 12): a reference-modified statement no longer gives the window, and a binary or packed item
arrives as its bytes. Goldens `pb4810-debug-nested-transfer`, `pb4810-debug-explicit-references`, `pb4810-debug-contents-and-sub-values` (all accepted at 85). The first re-gate was RED on
a CRLF my own edit script wrote (`line_endings.py --fix`), the second on the two spaces-as-number pins above.

**Branch review round 2 (five blocking findings, all answered).** (R2-1) Two conditional transfers were not marker leaves: GO TO DEPENDING ON and EXIT PROGRAM now land the
sections the enclosing statements owe on their own jump arm (`ControlFlowEmitter.EmitGoToDepending`, `CallEmitter.EmitExitProgram`), and the arm that falls through reaches the ordinary
fire. (R2-2) An implied place (a condition-name's conditional variable) no longer hides the positions the program wrote inside it: `YC (I)` references I (`BoundReferences.Visit`).
(R2-3) INITIALIZE holds the written identifier-1 (`BoundInitialize.Written`) and marks the elementary stores it expands into as implied, so `INITIALIZE G` references G and not X
(rule 9), and the bare data-name store taxonomy counts the written operand; SORT, MERGE and RELEASE mark the RECORD VARYING DEPENDING ON item implied through one shared
interface (`IBoundSortVarying`) as RETURN already did. (R2-4) The WHEN phrases of EVALUATE and SEARCH are evaluated only when reached: `IBoundEvaluatedPhrases` names them, the
planner emits one fire carrying the phrase indices, and the emitter sets one flag per phrase inside the phrase's own condition expression, so the section runs only when a named phrase
was evaluated (`DebugTriggerEmitter.PhraseFlags`); an operand short-circuited inside one condition stays unclaimed, the condition being one evaluation. (R2-5) A MOVE to an
alphanumeric item or a compare of a signed image-carried numeric now copies the characters of an invalid image as they stand (`CobolNum.DeSignImage`), so an unused DEBUG-SUB-n is
spaces as in GnuCOBOL; the decode of a valid image is unchanged; docs/CONFORMANCE.md D-DEBUG-SUB records the latitude. Golden `pb4810-debug-reach-and-implicit` (accepted at 85) and a
unit test for the sort DEPENDING item. Left: INITIALIZE of a whole OCCURS DYNAMIC table has no written place (its element stores stay references), RAISE and RESUME unwind past an
enclosing statement's after-statement section.

