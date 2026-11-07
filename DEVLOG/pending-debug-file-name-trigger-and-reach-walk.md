## kb/Work PB4811, PB4854, PB4855, PB4857, PB4858: the file-name USE FOR DEBUGGING trigger runs, and the debug reach walk stops counting operands the program did not write

Five notes, one root: the debug facility's reach walk (`BoundReferences`, `DebugTriggerPlanner`) and its emitters. Every note was re-probed on this
build first with a program of its own (`E:/COBOL-coord/scratch/w20261887a/p1..p7.cob`); PB4811, PB4854, PB4857 reproduced, PB4855 and PB4858
reproduced in part, and one claim (DB205A) turned out not to be a defect.

**PB4811, the file-name trigger (the MAJOR one: legal source refused COBOL1571).** The 1985 text decides it (the owner's copy, PDF pages 694 and
698): rule 2 runs the section after an OPEN or CLOSE that references the file, after a READ "not resulting in the execution of an associated AT END
or INVALID KEY imperative statement", and after a DELETE or START; rule 23 puts the file-name in DEBUG-NAME, the entire record read in DEBUG-CONTENTS
for a READ and spaces for every other reference; syntax rule 6 names a file once. Rule 7 makes the INVALID KEY statement a statement of its own, so a
DELETE or START section runs when the I-O is done and before that statement (GnuCOBOL's "after delete but before exception test"); DELETE FILE, which
the 1985 text has not, follows GnuCOBOL. Shape: `DebugTargetKind.FileName` with `BoundDebugTarget.File`; `DebugFire.Referenced` is nullable and
`DebugFire.Contents` says what DEBUG-CONTENTS holds; `DebugTriggerPlanner.PlanFile` plans OPEN and CLOSE as `After` and READ, DELETE, DELETE FILE and
START as `AfterAction`, which the sequential and keyed READ, DELETE, DELETE FILE and START emitters land through the existing `Debug.LandAfterAction`
(the keyed READ opens its success block for it, the loud-statement early returns land it so no fire goes unlanded). WRITE, REWRITE and the sort verbs
run none. `AddDebugFileTarget` also widens DEBUG-CONTENTS by the file's record area. Goldens: `pb4811-debug-file-name-triggers` (accept 85),
`pb4811-debug-file-name-repeated` (COBOL6335), `pb4811-debug-file-name-removed-2002` (COBOL0902); `docs/CONTROL_FLOW_DESIGN.md` D20 and the new
determination D-DEBUG-FILE. The staged-COBOL1571 test for a file operand became the compiles test.

**PB4854, the OCCURS DEPENDING ON item was counted as written.** `INITIALIZE TB` and `SEARCH E` over `E OCCURS 1 TO 5 DEPENDING ON N` ran the section for
ALL REFERENCES OF N. `AllCount.Places` is now the one list of the places a count reads, and every node that holds a count names them implicit:
`BoundInitialize` (also with no written place), `BoundSearch.DependItem`, `BoundTableSort.Count` and, by the sibling sweep, the element moves of a group
`BoundMove`. Golden `pb4854-debug-odo-count-is-implicit` (accept 85) and `-removed-2002`.

**PB4855, DataItem-only operands.** ALLOCATE's based item, SET ADDRESS OF's receiver and ADDRESS OF's operand now carry the place the statement wrote
(`BoundAllocate.Written`, `BoundPointerReceiver.BasedWritten`, `BoundAddressOf.Written`), so the walk reads them (`DebugFacilityTests.PointerVerbOperands_AreReferences`,
a 2002 `--permissive` compile because the verbs are 2002 and the facility 1985). The WHOLE-TABLE family (table SORT and its keys, the table SEARCH
names, whole dynamic-table INITIALIZE) stays open: it needs a whole-table place with a storage image, which the place model does not have
(`DeferredShape.DynamicWholeTable`), a redesign beyond this slice; PB4855 says so and keeps its sites.

**PB4857, RAISE and RESUME.** Marking them `IBoundTransfersControl`, as the lead proposed, would have landed the owed sections on the arm that continues too
(and twice under a global declarative). The root is that a RESUME AT procedure-name is a GO TO (§14.9.33.4 GR3) and every raise site lands it through
`DispatchState.ResumeTransfer`; that one landing, and `EcEmitter.EmitResume`'s unwinding arm, now run the sections the statements around them owe, through
one edge (`DispatchState.OwedBeforeTransfer` to `DebugTriggerEmitter.OwedBeforeTransferText`). RESUME NEXT STATEMENT owes none.
`DebugFacilityTests.RaiseAndResume_LandTheSectionsTheAbandonedStatementsOwe`.

**PB4858, legality adjudicated from the text.** `DEBUG-CONTENTS (1:12)` is legal (the section may reference-modify an alphanumeric register): fixed in
`ReferenceResolver.Identify`. A sum counter as identifier-1 is legal (syntax rule 9): `ProcedureTableBuilder.SumCounterOf`; golden
`pb4858-debug-sum-counter-target`. DB205A does not finish because it is operator-driven (RECEIVE-TEST-2 writes "ENTER ONE MESSAGE NOW." and waits),
not because of the facility. LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER as targets stay the staged COBOL1571 (no place to match), recorded in PB4858.

**Self-review.** Architecture: one list (`AllCount.Places`), one edge (`OwedBeforeTransfer`), one planner arm; no C# text in a bound node. Code: each
rule cited to the 1985 pages read (cite.py has no 1985 or 2002 transcription in this tree, so those were read from the rendered pages; the 2023 cites
were checked). Performance: no new work on a program without a debug target, and the nodes' implicit lists are computed only when the walk asks.
Duplication: `LandBeforeTransfer` and `OwedBeforeTransferText` share `OwedTexts`; `EmitFire` and the transfer text share `FireText`. Found and fixed on
the way: the keyed and sequential READ early returns would have left a fire unlanded.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 22,406/22,406 · Unit 40,356/40,356 · Characterization 37/37 cases ran (skipped 0) (run 000002-20261107T013136Z) ===`;
the first run was RED on LINE ENDINGS only (the three new `.out` goldens written CRLF by the Windows run; `scripts/line_endings.py --fix`), with no test run.
`scripts/semgrep/verify.py`: PASS, no rule's count raised.
