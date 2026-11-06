## kb/Work PB4810, PB4759: the data-name, ALL REFERENCES OF and cd-name USE FOR DEBUGGING triggers run

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
as the largest target; DEBUG-SUB-1 to DEBUG-SUB-3 hold the occurrence numbers, outermost first (alphanumeric, the four digits and a space,
the MOVE image of the implicit S9(4) SIGN LEADING SEPARATE item). New diagnostics: COBOL6334 (a subscripted or reference-modified target,
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
