## Entry 3154 — 2026-11-05 12:19 PST — PB4706: a header written after a level-number is the missing period, and PROCEDURE names no entry

A dangling level-number before a division or section header named the header's first word as the entry. Fixed at the root, and the sweep found the root was also an under-rejection.

**Re-probe.** On a fresh build every lead reproduced: `01` then `PROCEDURE DIVISION.` drew COBOL1941 at DIVISION "for 'PROCEDURE'" and a second COBOL0001 at the next paragraph; `01 LOCAL-STORAGE SECTION.`, `01 SCREEN SECTION.` and `01 MAIN SECTION.` drew COBOL1941 at SECTION; `01 LINKAGE SECTION FOO.` drew neither report (R1-1). The sweep added: `01 FILE SECTION.`, `01 WORKING-STORAGE SECTION.`, `01 DATA DIVISION.` drew COBOL0901 at the header word; `01 A PIC X` then `LOCAL-STORAGE SECTION.` drew COBOL0901; `01 PROCEDURE PIC X.` COMPILED at every edition, against §8.3.2.1 1) and §8.9.

**The PROCEDURE alternative.** `dataName` kept a hand-written PROCEDURE alternative because PB300 recorded that NC205A names an item with it. It does not: NC205A's `77 PROCEDURE` continues on its next line with `DIVISION`, one word. Deleted. `01 PROCEDURE PIC X.` is COBOL0901; `01` then `PROCEDURE DIVISION.` is one COBOL0307 at PROCEDURE with no cascade, at every slot that takes a `dataName`; NC205A compiles.

**The gated words.** `dataName`'s `reservedGatedWord` alternative carries `noHeaderWrittenHere()`, since no format follows a data-name with SECTION or DIVISION (pinned on FOLLOW(dataName)). `01 LOCAL-STORAGE SECTION.` at 2002 and later is COBOL0307 at the word; at 85 the word is a user word, no such section exists, and the report is COBOL0001 at SECTION.

**A header is never a clause or a name.** `OffendingPosition.WritesAHeader` keeps `ClosedFormatResidue` and the slot from those readings, so a header no period lets follow draws the plain report it draws after a complete entry, and `ReservedWordUse` stands down for a header keyword a period lets follow.

**One question.** `BeginsWhatFollows` asked two tokens and hint 14 a whole sentence, so a word could pass one and fail the other (PB4646 review R1-1). Both now ask `PeriodIsMissingBeforeTheEntrySlot`, and `SentenceTypesFrom` ends a reading at a header's closing word. `01 LINKAGE SECTION FOO.` is COBOL0307 and then the fault at FOO.

**Goldens.** Seven `pb4706-*` cases in `13.16-Data-description`; `ClosedFormatReservedWordTests` gains four theories; `ReadingParsesFollowDriftTests` gains the FOLLOW and FIRST pin. No inventory row re-verdicted (`closes_rows: []` with its reason). No new diagnostic code: COBOL6211-COBOL6213 returned.

**Self-review.** Architecture: one predicate, one position question, the walk the position already holds; the second walk from the entry's end is gone. Full code: §8.3.2.1 1), §8.9, §13.16.2 and §13.2.1 each `--check`ed; sibling slots (report group, constant, screen, CD, FD, SELECT) probed. Performance: `noHeaderWrittenHere` is one LA at a gated word; the rest is error path. Duplication: the header-closing test is `ClosesAHeader` in one place.

**Left as a lead.** A screen entry's slot reads the word as COBOL5506 when the header is a section keyword (`01 LINKAGE SECTION.` in the SCREEN SECTION): truthful and one report, but not the missing period, because `EntryAwaitingItsName` is the data description entry's alone.


Gate: === BUILD-LOCAL GATE: GREEN — Conformance 22,041/22,041 · Unit 40,166/40,166 · Characterization 37/37 cases ran (skipped 0) (run 000001-20261105T194248Z, worktree-wf_e221b103-63b-2 head 8ca141e74) ===

