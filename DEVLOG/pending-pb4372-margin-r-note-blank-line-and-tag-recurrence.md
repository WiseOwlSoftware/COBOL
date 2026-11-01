## The margin-R note names a line that holds text only past margin R, and a tag that recurs is a tag (PB4372)

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
line holding a tab named a margin other than 72 (PB2555 review R1-2); `LastProgramTextPosition` is its own member now, set from
`FixedFormLine.MarginR`, and the last cut position counts characters, not UTF-16 units. `ACommentLinePastMarginR_IsNotACut` could not
fail (R1-3); the comment line now stands between the two tokens.

**Tests.** `MarginRCutNoteTests`: 16 cases (6 new: only-past-R, several lines between, a sequence number on a blank line, tags on a
minority, a lone tag, a tab). Conformance refusal `pb4372-statement-only-past-margin-r` (all four editions).

**Rows.** None changes verdict (a diagnostic's quality; every program was already refused); `closes_rows: []` with its reason.
