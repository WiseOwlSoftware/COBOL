## Entry 2815 — 2026-10-30 15:02 PDT — SEND and RECEIVE name operands are asked their written shape (PB4118)

`DeclinedFacilityBinder.ClassifySend` took every operand after TO that is no message-tag item for a message-server-name-1
without asking how it was written. §14.9.38.2 prints that operand as a name in both formats (message-server-name-1, §14.9.38.3
SR2; data-name-2, a qualified-data-name, §8.4.2.2.2 Format 1), so a subscripted, reference-modified or special-register
operand is neither, and each compiled clean.

**Re-probe (Debug build, 0/0).** `SEND TO TBL(1) FROM M RETURNING R`, `SEND TO R(1:2) ...`, `SEND TO LINE-COUNTER ...` and
`SEND TO C OF B FROM M RETURNING R` all compiled; so did `SEND TO NOP OF B ...` (NOP declared nowhere). The same shapes as
RETURNING's data-name-1 and RECEIVE's data-name-1 and data-name-2 compiled too: the siblings of the one missing question, in
the same file, fixed with it (the owner's I9 rule). `RETURN-CODE` after TO still reads as a bare server name (it is no ISO
special register and parses as a word); it is not touched.

**Fix.** `DeclinedFacilityBinder.ScreenName` is the single call of the existing data-name-n shape screen
(`DataBinder.ScreenDataNameShape`, COBOL2024) for the four name operands. A refused operand is not resolved and gives the
format no evidence; a SEND with no other evidence is `SendFormat.Unclassified` (the old `Mixed`, renamed because it now also
means "a diagnostic already refused the format"), so it is not also asked for a RETURNING it may not need. No new diagnostic
code: COBOL5554-5556 are returned.

**A departure from the note's text.** The note asked to refuse a qualified TO operand as well. The standard does not: a
data-name is a qualified-data-name (§8.4.2.2.2 Format 1), so `A OF B` is a legal data-name-2. What a qualifier decides is the
FORMAT: a message-server-name-1 is a bare name (SR2), so a qualified TO operand is Format 2's evidence exactly as a message-tag
item is. It resolves like any data reference (an undefined one is COBOL1639) and a RETURNING beside it is COBOL5488.

**Goldens.** Nine cases under `wiseowl/cases/14.9.38-SEND/` and `14.9.31-RECEIVE/` (`pb4118-*`): one refusal per shape and one
accepted case with `.out` (a qualified data-name-2, a subscripted FROM and GIVING identifier). `SendFormatClassificationTests`
pins the absences (one COBOL2024, no COBOL1850 / 5488 / 1639 cascade) and the legal qualified forms;
`DeclinedFacilityOperandDriftTests.EveryNameOperandOfTheDeclinedStatements_IsScreenedForItsWrittenShape` keeps the census of
name operands. `docs/CONFORMANCE.md` §4 item 1 states the screen. No inventory row moved (the MCS rows keep their
DOCUMENTED-NON-SUPPORT verdict).

**Self-review.** Architecture: one screen reused, not a second copy of the three shapes. Full code: a refused operand is
neither resolved nor classified. Duplication: none added. The COBOL2024 text speaks of "the clause's general format prints
data-name-n", which is loose for a Format 1 server name; the operand face and the statement's rule sentence carry the
precision, and the shared message is not forked for one caller.

