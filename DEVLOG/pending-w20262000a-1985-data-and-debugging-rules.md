## PB5071, PB5081, PB5088, PB5091, PB5180: seven COBOL-85 restrictions the compiler applied the later editions' way, and duplicate-name messages that pointed at the wrong lines

Wave 20262000 group A. Every note was re-probed on the branch's build first, and each reproduced: the five --std 85
programs of PB5071, PB5088 and PB5180 compiled (exit 0), so did the six PB5081 programs, and PB5091's COBOL2122 read
"(paragraph 'P1' at line 9; paragraph 'P1' at line 11)" for paragraph-names on lines 8 and 10.

**PB5071, the file section.** 1985 requires record description entries after every FD (§1985:VII.3.2.3 SR3, and
the same rule in VIII and IX), and allows only ASSIGN after a sort-merge file's name in FILE-CONTROL (§1985:XI.2.3.3
SR2). The compiler applied 2002's rules at 85. `FileControlBinder.MaterializeImpliedRecord` now refuses a record-less
FD below 2002 with COBOL1837, before it asks for a RECORD clause, so the COBOL1836 fix (add a RECORD clause, which
1985 does not allow) is no longer suggested at 85. The file control entry now captures the first thing written after
SELECT and ASSIGN (`FileModel.BeyondAssign`, OPTIONAL included), and `FileControlKeyRules.Screen` refuses it for an
SD file below 2002 with COBOL1900, replacing the 2023 sort-merge rows there. The golden
`pb345_record_less_fd_area_85` had pinned the opposite reading ("carry no edition gate"), so it is now the 85 refusal.
The pb345 COBOL1836 refusals now run from 2002. FILES_DESIGN D18 says so.

**PB5088 and PB5180, INITIALIZE.** Both are COBOL-85 restrictions that the 2002 text lifts, so each is a construct row
(COBOL0900, VCR row 7.52 d and e). `initialize-variable-occurrence-2002` covers §1985:VI.6.17.3 SR4, which forbids
INITIALIZE of an item whose description, or a subordinate's, has OCCURS DEPENDING ON. The 2002 syntax rules 14.8.19.2
drop it. It is asked once for each resolved identifier-1 (`InitializeBinder.GateCobol85VariableOccurrence`).
`initialize-replacing-category-set-2002` covers §1985:VI.6.17.2, which prints a REPLACING item's category words in
plain braces, so an item names one category (§1985:IV.2.1.4). The 2002 format 14.8.19.1 adds choice indicators. The
gate reports at the item's second word (`VersionConformancePass.VisitInitializeReplacingItem`). That construct rests
on general formats alone, so it joins `edition_gates.py`'s FORMAT_ONLY held-open list. `pb415_initialize_multi_category_85`
had pinned the acceptance and is now the refusal. `pb393_initialize_odo_group` now runs from 2002. The INITIALIZE leg
of `pb4854-debug-odo-count-is-implicit` became MOVE ZEROS TO TB, which tests the same reach rule with the same output.

**PB5081, the debugging facility.** Whether a section is a debugging section is now stored on the section
(`SectionInfo.IsDebugging`, one writer). It replaces the `_pendingDebug` scan behind `IsDebuggingSection`, which is
deleted, and its callers read the stored value. Six §1985:XV.3.2.3 rules are now enforced:
- SR1: a debugging section that follows another declarative section is COBOL6948.
- SR3: a reference to a debugging procedure from outside the debugging sections is COBOL6946, in
  `CheckDeclarativesBoundary`, PERFORM included.
- SR5: a debugging procedure named as a subject is COBOL6946.
- SR6: a procedure-name named twice is COBOL6335. `AddDebugSubject` used to drop the repeat without a word.
- SR7: ALL PROCEDURES written twice is COBOL6947.
- SR8: a procedure-name subject in a program with ALL PROCEDURES is COBOL6947.

`pb4811-debug-file-name-failures` had its debugging sections after its USE sections, which SR1 forbids. They now come
first, with the same output. docs/CONFORMANCE.md D-DEBUG-PROGRAM-RULES.

**PB5091.** `AtLine` printed `_paraLine`, which holds each paragraph's first-sentence line for DEBUG-LINE. Paragraph-names
now have their own lines (`_paraHeaderLine`), and so do sections (`SectionInfo.HeaderLine`), so COBOL2122 and COBOL2121
give the lines where the names are declared, including empty paragraphs and sections.

**Rows.** 1985-FMT-IV.6.3.2.2.1 and 1985-FMT-VI.6.17.2 were PARTIAL and are now CONFORMS (record_verdicts, 1985 GAP 606
to 604). The notes claim no rows: PB5094 still blocks 1985 ids in a note.

New codes COBOL6946 to COBOL6948. The gate's verdict is in the branch's report.
