- **D-DEBUG-DATA — when a data-name, ALL REFERENCES OF or cd-name USE FOR DEBUGGING section runs** (kb/Work PB4810, PB4759, PB4808).
  ISO/IEC 1989:2023 prints nothing on the facility, so the text is X3.23-1985's Debug module (the owner's copy,
  `specs-private/ANSI_COBOL_1985.pdf`, PDF pages 693-698), USE FOR DEBUGGING general rules 5-9 and 23-24 and syntax rules 6, 9, 10 and
  12; the design is `docs/CONTROL_FLOW_DESIGN.md` D20. **Determinations** where the text leaves the choice: (1) "causes the content … to
  be changed" (rule 6 c) is read as STORED INTO by the executed statement, whether or not the value differs, but not when an ON SIZE
  ERROR path skipped the store: §1985:VI.6.4.2 leaves the receiver's content unchanged there, so the statement does not cause it to change
  (GnuCOBOL 3.3-dev `typeck.c:1210-1232` fires there; the 1985 text decides, kb/Work PB5296); a BY REFERENCE CALL argument is not a store
  (the callee's effect is not decidable at the CALL). A condition-name is no explicit reference to its conditional variable (§1985:IV.4.4.1), so SET
  condition-name TO TRUE runs neither the variable's ALL REFERENCES OF section nor its bare section. (2) The
  loop moments of rule 5 c / 6 b are each initialization, each augment and each evaluation of a VARYING, AFTER or UNTIL phrase that
  references the item, TEST BEFORE and TEST AFTER alike. (3) A statement nested in another is a statement of its own (rule 7), so an
  IF's condition and its nested MOVE are planned separately, and the section of an item the IF's condition references runs after the
  IF, except that a nested transfer of control leaves the statement and the section runs immediately before it. (4) A group reference does not reference its subordinate items, nor a subordinate item its group, and a qualifier is no reference
  (rule 9). (5) DEBUG-CONTENTS is the STORAGE image of the whole referenced item, whatever window the statement used (rule 24 c, syntax rule 12) and with no conversion of internal representation (rule 11), the CD record-area image for a cd-name (rule 23 e), and DEBUG-LINE the
  physical source line of the referencing statement (rule 12 leaves the form to the implementor: "The content of DEBUG-LINE is the
  implementor-defined means of identifying a particular source statement"). That line is the line within the file that holds the
  statement, so a statement brought in by COPY reports the library text's own line, and two statements on one line, or a copied
  statement and a main-text statement with the same line number, report the same value. A variable-length group's DEBUG-CONTENTS is
  its CURRENT length with the rest of the register space-filled (§1985:VI.5.8.4 GR3, §1985:XV.3.2.4 GR11, GR24 c; kb/Work PB5293). A subscripted
  reference is identified once, when the statement begins (§1985:IV.4.3.8), so DEBUG-SUB-n and DEBUG-CONTENTS describe the occurrence the statement
  named even when it changes its own subscript (kb/Work PB5294), and only a statement written in a debugging section causes no debugging section
  (§1985:XV.3.2.4 GR1; kb/Work PB5292). (6) RECEIVE's cd-name section runs
  when the data arrived and after the WITH DATA statements, which are inside the RECEIVE (§1985:XIV.3.5.4 GR4; kb/Work PB5295). **Departures from GnuCOBOL, each where the 1985 text decides:** SORT, MERGE,
  RELEASE and RETURN run the ALL REFERENCES OF section (rule 5 d: "any other COBOL statement"); a cd-name section runs (rule 8; GnuCOBOL
  never fires it). A REPORT SECTION sum counter is a target (syntax rule 9: "Identifier-1 must not reference any data item defined in the Report Section except sum counters"; the item watched is the counter's register, kb/Work PB4858), as is any data item or special register that has a place; LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER are special-register data items nothing in the format excludes (ISO/IEC 1989:2023 §2023:8.4.3.14.4 GR1, §2023:8.4.3.15.4 GR1), runtime-sourced and with no place, so the target names the register (`BoundDebugTarget.Register`), a statement's read of it is a register reference (`Reach.Registers`) and DEBUG-CONTENTS is its storage image; a store changes only PAGE-COUNTER (LINAGE-COUNTER and LINE-COUNTER are changed by the I-O and report engines alone), so the bare form of the other two never fires (witness `conformance:cases/_unassigned/pb4858-debug-counter-registers`). DEBUG-CONTENTS (1:n) in a debugging section is the reference modification of an alphanumeric item like any other. CCVS DB205A is an operator-driven program (its RECEIVE-TEST-2 waits at the terminal for "ENTER ONE MESSAGE NOW": a RECEIVE with no NO DATA phrase waits for data, `docs/rearchitecture/DESIGN-communication-facility.md`), so it does not finish unattended whatever the debugging facility does. Witnesses: `conformance:cases/_unassigned/pb4810-debug-data-name-triggers`,
  `conformance:cases/_unassigned/pb4810-debug-write-go-to-perform`, `conformance:cases/_unassigned/pb4810-debug-rewrite-and-failed-write`, `conformance:cases/_unassigned/pb4810-debug-sub-and-name`, `conformance:cases/_unassigned/pb4810-debug-nested-transfer`, `conformance:cases/_unassigned/pb4810-debug-explicit-references`,
  `conformance:wiseowl/cases/_unassigned/pb4759_debug_cd_name_triggers`, `conformance:cases/_unassigned/pb4810-debug-target-subscripted`
  (COBOL6334), `conformance:cases/_unassigned/pb4810-debug-target-repeated` (COBOL6335),
  `conformance-test:DebugFacilityTests.StatementInADebuggingSection_At85_CausesNoSection`; CCVS DB201A runs with no failing test.
