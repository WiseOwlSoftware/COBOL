## Entry 3188 — 2026-11-05 20:05 PST — PB4788: a dangling level-number before an out-of-order header is read at every entry slot, not only the data description entry's

**Re-probe.** `SCREEN SECTION.` then `01 LINKAGE SECTION.` drew COBOL5506, "The word LINKAGE is not expected here: expected a user-defined word, BINARY, ..." listing every clause of the screen entry. The same in a report section (`RD R.` then `01 LINKAGE SECTION.`). `01 PROCEDURE DIVISION.` there already drew COBOL0307.

**Root.** `ClosedFormats.EntryAwaitingItsName` found the nameless entry from the parser's context and knew only the data description entry. A section's entry loop (`screenSection`) fails its prediction at the header word with the parser still before the level-number, so no entry context exists at all; the slot was never recognized, and hint 0i (the kinds "expected" refusal) filled the gap.

**What it should say.** The note asked for COBOL0307. It is not that here: ISO 13.2.1 prints LINKAGE SECTION before the REPORT and SCREEN SECTIONs, so no period lets the header follow, and PB4706 had settled that an out-of-order header after a dangling level-number is the plain COBOL0001 (`01 FILE SECTION.` in WORKING-STORAGE). The screen, report-group and constant slots now behave as the data slot does: COBOL0001 for an out-of-order header, COBOL0307 where the header may follow.

**Fix.** `EntryAwaitingItsName` returns a `NamelessEntry` for the four entries that print `level-number [name]` (data description, screen description, report group, constant). `ClosedFormats.LevelNumberEntryRules` is the one list of those rules by name, and `ClosedFormatDriftTests` derives the grammar's rules that call `levelNumber` first and requires the list to equal them. `OffendingPosition.FollowsALevelNumber` reads the slot from the walk when the context holds no entry: the word before the token is an integer the parser has not passed and an entry of the list is open. Hint 0i stands down at a slot that yields (`EntrySlotYields`). The PB4645 name fallback stays on the data description entry.

**Goldens and tests.** `conformance-suite/cases/13.9-Screen-section/pb4788-header-out-of-order-after-a-screen-entrys-level-number` (refused at every edition); `ClosedFormatReservedWordTests.AnOutOfOrderHeaderAfterADanglingLevelNumber_IsNoKindExpected` (screen, screen after a complete entry, constant, report group).

**Noticed, not fixed.** A report-group `01 SCREEN SECTION.` takes SCREEN as the entry's name (a free cobolWord at that slot), so its COBOL0307 stands at SECTION, not SCREEN as in the working-storage section. A different mechanism (the name predicate), registered in the report.

