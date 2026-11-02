## Entry 2963 — 2026-11-01 16:47 PST — The margin-R note names a line that holds text only past margin R, and a tag that recurs is a tag (PB4372)

PB2555 gave a syntax error beside a margin-R cut a warning (COBOL5788). Its landing left two shapes out, and the second carried no owner
decision, which CLAUDE.md rule 3 forbids. PB4372 closes both at the root, in the same files.

**Re-probe.** On the Debug build, a `CALL ... BY` whose argument stands only from position 73 of the next line (blank program-text area),
followed by `STOP RUN.`, gave only `COBOL5506 cannot parse construct near 'STOP'`. A line `DISPLAY WS-MSG.` with `TAG1` at position 73,
then a stray `END-IF.`, gave "the statement that line began may have continued past it" beside the unrelated COBOL0306.

**Shape 1.** `CobolErrorListener.NoteMarginRCut` asked about the line of the token before the offending one and nothing else; a line
between the two holds no token, so none anchored the question. It now asks about every resultant line strictly between the two tokens.
`MarginRCut` gained `HoldsProgramText` (the reference format knows it), and the message for a line without program text says so ("this
line has no program text ... the line reads as blank") instead of claiming the text "ends at position 72".

**Shape 2.** The old rule dropped tag-shaped cuts only when more than half of the code lines carried one, so a file tagging a minority of
its lines (a revision mark on the lines an edit touched) kept firing. Shape cannot tell one tag from the one-word tail of a statement
(`END-IF` is tag-shaped); recurrence can. A tag-shaped word on two lines or more of a file is now a column convention
(`MarginRCut.RecurrenceThatMakesATag`), a tagged blank line counts, and a lone one still draws the note, which says "may". The price is
a file whose only overflows are one-word tails on two lines or more: it hears no note.

**Found on the way (I9).** `MarginRCut.Margin` (a physical file column) doubled as the 1-based position of margin R in the message, so a
line holding a tab named a margin other than 72 (PB2555 review R1-2). PB4373 landed the same fix on main while this branch waited; the
merge takes main's shape (`MarginRCut(Column, LastProgramTextPosition, Text, HoldsProgramText)`: main's `Column` and its tab remark
in the message, this branch's `HoldsProgramText`), and the no-program-text form of the message carries the tab remark too
(`ALineWithNoProgramTextHoldingATab_StatesMarginRAtPosition72`). `ACommentLinePastMarginR_IsNotACut` could not
fail (R1-3); the comment line now stands between the two tokens.

**Tests.** `MarginRCutNoteTests`: 16 cases (6 new: only-past-R, several lines between, a sequence number on a blank line, tags on a
minority, a lone tag, a tab). Conformance refusal `pb4372-statement-only-past-margin-r` (all four editions).

**Rows.** None changes verdict (a diagnostic's quality; every program was already refused); `closes_rows: []` with its reason.

**Review round 1 (R1-1, blocking).** The between-lines arm also asked about lines that HOLD program text and stand tokenless, which are lines the compiler omitted (a false `>>IF` branch, ISO 7.3.16.4 3); a debugging line without debugging mode), so their cut was blamed for the error. The arm now asks only of lines whose program-text area is blank (`HoldsProgramText` false), and the "text of that line may have been needed" message is gone with it. Tests: `ALineOfAnOmittedConditionalBranch_IsNotAskedAbout`, `ADebuggingLineWithoutDebuggingMode_IsNotAskedAbout`. R1-3 (the member summary said two shapes) corrected to three. A line with text ONLY past margin R inside an omitted branch is still asked about: no stage tells the listener which blank lines were omitted.

