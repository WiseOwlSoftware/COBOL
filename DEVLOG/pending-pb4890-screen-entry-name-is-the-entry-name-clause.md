## PB4890: the screen entry's name is the entry-name clause, so a reserved name draws COBOL0901

**Mechanism.** The screen description entry had a name rule of its own, `screenName : cobolWord | FILLER`, which admitted no
reservation-gated word. `01 SCREEN LINE 1.` in the SCREEN SECTION therefore left the section loop's `constantEntry` as its one
surviving alternative, and the syntax-kind report said `COBOL5506: The word LINE is not expected here: expected CONSTANT`,
where the same name in WORKING-STORAGE draws `COBOL0901: 'SCREEN' is a reserved word`.

**Re-probe** (fresh Debug build at fe01e3195e): the note's `c22.cob` drew the COBOL5506 at 2002 and 2023, and the
WORKING-STORAGE twin the COBOL0901.

**Fix.** §2023:13.17.2 prints `level-number [ entry-name-clause ]`, and the entry-name clause is §2023:13.18.20, the rule the data
and report group description entries read (§2023:13.18.20.4 GR1: "The word FILLER may be used to name a data, report, or screen
item"). `screenDescriptionEntry` now reads `dataName` behind the same `entryNameHere(RULE_screenDescriptionBody)` predicate as
`reportGroupEntry` (PB4889), and `screenName` is deleted. The predicate is load-bearing: COLUMN, COL, COLS, COLUMNS and NATIONAL are
reservation-gated words that begin a screen clause, so without it `05 COLUMN 5 VALUE "A".` would read COLUMN as the name and draw
COBOL0901 on conforming source. The four readers of the old rule (`ScreenFacility.ReportSection`, `LevelNumberPass.EntryName`,
`ClosedFormats.EntryAwaitingItsName`, `OoExpansion.SlotOf`, which keeps "screen-name" for a `dataName` under a screen entry) read
`dataName()`. `ReadingParsesFollowDriftTests` now finds `screenDescriptionBody` among the predicate's body rules and holds that the
period alone follows it.

**Sibling sweep.** Every rule beginning with `levelNumber`: the data and report group entries already had the predicate, the
constant entry needs none (CONSTANT follows the name). The other definition slots with no `reservedGatedWord` (condition-name,
index-name, report-name) were probed with CRT at 2002 and 2023 and already draw COBOL0901.

**Goldens.** `cases/13.17-Screen-description-entry/pb4890-screen-entry-named-by-a-reserved-word` (`01 CRT LINE 1.`, refused at
2002/2014/2023 with COBOL0901) and `pb4890-screen-entry-named-by-a-free-word-at-85` (refused at 85 by the section's COBOL1560: CRT
is a user word there). Unit: `ScreenFacilityConstructDriftTests.AReservedWordAsAScreenEntrysName_IsRefusedByName_AsInWorkingStorage`
(every §2023:8.9 screen-surface word at every edition: COBOL0901 exactly where reserved, never COBOL5506) and
`AClauseAReservedWordBegins_IsNotReadAsTheScreenEntrysName` (COLUMN/COL/COLS/COLUMNS/NATIONAL as unnamed clauses).

**Gate.** The first gate (run 000001-20261107T213611Z) was RED on one case, `EditionReadDriftTests.EditionReads_AreExactlyTheAdjudicatedSet`: the new predicate is a front-end edition read, adjudicated WordStatus beside the report entry's and recorded there.

**Rows.** None re-verdicted: FMT-13.17.2 and SR-13.17.3-1 stay DOCUMENTED-NON-SUPPORT (Annex A.4.2 is declined).

**Found, not fixed.** At COBOL-85 a program that names a screen item SCREEN draws COBOL0001 at the `SCREEN SECTION` header: the
reservation gate retypes every SCREEN token once the free word is declared, the header's included (reported as a lead).
