## PB5380 PB5415 PB5414 PB5291 PB5104 PB5276 PB5275: the COBOL-85 dispatch and sort-merge placement group

Seven notes of the 1985 code reads, fixed at their roots in `DispatchEmitter` and the binder beside it.

**PB5380.** `PERFORM S60 2 TIMES` re-entered the independent segment in its initial state on the second pass, undoing the range's own ALTER:
every `__Dispatch` call restores the caller's `__seg`, so the loop head saw a segment change and called `__EnterSegment`. Re-probe on the
branch's base: `S60 INITIAL`, `S60 INITIAL`, `T70 INITIAL 1`, `T70 INITIAL 2`. §1985:XVI.1.4.3 puts a segment in its last-used state on an
implicit transfer from another segment, and each iteration after the first is entered from the PERFORM's control mechanism (§1985:IV.4.4.2 GR1).
Fix: an iterative out-of-line PERFORM declares `bool __segIter<n>` before its loop, `DispatchState.DispatchCall` passes it as `__Dispatch`'s
`__lastUsed` argument and sets it, and the call starts in the range's own segment when it is set. SORT, MERGE and USE calls keep the
reset (§1985:XVI.1.4.3 (2)). Result: `S60 INITIAL`, `S60 ALTERED`, `T70 INITIAL 1`, `T70 ALTERED 2`.

**PB5415, PB5414.** The START PROGRAM cause was emitted in every activation, so a second CALL of a debugged program reported START PROGRAM again; a
section subject fired on every entry to its first pc, so a loop `GO TO P1` inside S2 ran S2's debugging section three times for one execution of S2.
Fix: `__dbgStarted` (an instance flag; a later CALL enters as a transfer with spaces, DEBUG-LINE blank, §1985:XV.3.2.4 GR16, GR11), and the cause
assignments of every transfer now also record the paragraph they were made from (`__dbgFrom`, `DispatchState.DebugEntryFrom`), which
`__RunDebug` tests against the subject section's own pcs (`BoundDebugSubject.SubjectEndPc`): a section subject runs when control reaches it from
outside. A PERFORM or SORT/MERGE procedure that names only part of the section is "from" its first pc. d14 prints one `DS` line; d4m prints
START PROGRAM once. The note's reading that GR9 also excludes `GO TO P1 OF S2` is not adopted (GR9 is about reference, GR3 a about execution); recorded in
D-DEBUG-PROCEDURE.

**PB5291.** Without WITH DEBUGGING MODE a debugging section whose body was not COBOL syntax was refused by the parser. `DebuggingLineRewriter` (the
one place the mode switch decides) now moves the tokens of a USE FOR DEBUGGING section to the absent channel for a unit with no mode, at 85 and under
--permissive, together with the DECLARATIVES header and END DECLARATIVES when the portion held nothing else. `m1i.cob` compiles and prints `P1 A`.

**PB5275, PB5276, PB5104.** A SORT or MERGE in a debugging section compiled: `EnclosingContext.InDeclarative` now includes a debugging section
(`ProcedureTableBuilder.InDebuggingSection`; no `BoundDeclarative`, which would have joined the USE selection machinery). Two GIVING files of one SORT in
one SAME RECORD AREA clause are refused below 2002 (§1985:XI.4.4.3 SR9; COBOL1757). A RELEASE or RETURN outside the range of a SORT or MERGE procedure
that names its file is refused at 85 (COBOL7081, `VersionConformancePass.GateReleaseReturnRange`), the range being `ProcedureReach.Closure` with GO TO
followed, so a statement in a performed paragraph is within it (§1985:XI.4.4.4 GR7, GR10).

Goldens: `pb5380_perform_iteration_last_used_85`, `pb5414_debug_section_subject_entry_85`, `pb5415_debug_start_program_first_execution_85`,
`pb5291_debug_section_is_comment_without_mode_85`, `pb5104_release_return_in_performed_range_85`, `pb5276_sort_giving_same_record_area_2023`, and the
refusals `pb5275-sort-merge-in-debugging-section`, `pb5276-sort-giving-same-record-area-85`, `pb5104-release-of-file-no-sort-names-85`,
`pb5104-return-of-file-no-sort-names-85`. Unit tests: four in `DebuggingLineRewriterTests`. Rows: none recorded (PB5094 keeps 1985 ids out of a note);
all seven notes landed with `closes_rows: []` and a reason. Docs: CONFORMANCE D-DEBUG-PROCEDURE, D-DEBUG-COMMENT, D-SORT-RELEASE-RETURN-RANGE-85,
D-SORT-GIVING-SAME-85, CONTROL_FLOW_DESIGN D4.
