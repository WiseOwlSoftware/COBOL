## Recovery cascades: a faulted sentence no longer swallows the program, a left statement keeps its terminator, and COBOL2073 names only a misplaced WHEN OTHER (PB4299, PB4382, PB4300)

Wave 20261822, group A: the `CobolErrorStrategy` notes, fixed at their root in harm order.

**PB4299 (a faulted sentence's tail swallowed the rest of the program).** Re-probed on 83a1e47f4, the note understated
it: after `IF A = 1 THEN ADD TO A END-IF.` the recovery stopped at the tail's `A` because ANTLR's recovery set holds a
paragraph header's first word (the rules around the sentence put it there, one token deep and predicate-blind); the
sentence could not go on, the paragraph list could not take `A END-IF`, and the procedure division's own recovery then
consumed every token to the end of the program. Every later fault went unreported (V2, V12, V18, V19, R3, R14).
`ConsumeUntil` now splits the set: what the rules up to the innermost period-terminated unit hold
(`UnitTailDiscard.HeldWithin`, plus what the caller added, a loop's continuation) still stops the resynchronization; a
token held only by the rules around the unit stops it only where `UnitTailDiscard.Resumes` holds, the tail discard's
two-token test, so `word.` is the sentence's last operand and never a phantom header (ISO 14.4.3: "A paragraph consists
of a paragraph-name followed by a separator period"). The R10 cascade (`IF A = 1 THEN ADD TO "X" ELSE MOVE 1 A
END-IF.`, a false COBOL0306) was a second mechanism: the recovery resumed at the ADD whose own prediction had died at
the TO, every enclosing list predicted the same malformed statement and gave up, and the IF ended there. `Recover` now
consumes the first token of a construct whose own prediction failed ahead of it (not where PB4218's retype re-parses it,
never a period).

**PB4382 (a branch with no statement drew a false COBOL0306 at END-IF, or COBOL2073 for the wrong fault).** The empty
statement-1 of `IF W-N = 1 ELSE …` fails at the IF's own THEN?/block decision, which ANTLR's generated rule answers by
returning: no recovery inside the rule can keep the IF open, so the note's "present-but-empty block" direction was not
the root. The root is the terminator's pairing (ISO 14.5.3.2 1): an END-x terminates "the most-recently preceding
unterminated statement having the statement-name for which that scope terminator is defined"). `Recover` records each
statement an explicit scope terminator closes whose rule it leaves (and the statement a consumed first token opens),
`Report` records the constructs a prediction closed early when it carries a report into them, and a later terminator of
that kind in the same unit (no separator period between, 14.5.3.3 1) c)) is consumed with the statement's tail and
reports nothing. Fixing PB4299 surfaced the same cascade for a fault in an IF's condition (`IF X NOT <> 1 DISPLAY "C"
END-IF`), whose `RequiredImperativeStatementDriftTests` rows had passed only because the rest of the program was being
swallowed; the record answers them too. `EVALUATE W-N WHEN 1 WHEN OTHER …` now draws COBOL2072 for the WHEN group.

**PB4300 (COBOL2073 at a program's only WHEN OTHER).** Hint 0b decided from what follows the OTHER alone. It now reads
the construct around the parser (`WhenOtherPlacement`; the WHEN OTHER rules and their callers read from the ATN): the
phrase is misplaced where the construct already holds one or holds no WHEN group before it; in its place after an
incomplete statement (`WHEN A = 2 DISPLAY WHEN OTHER`) 0b says nothing, and where the parse took the WHEN as the
group's next selection phrase it names the group's missing statement. The note's "layout dependence" (a true repeat
sharing a line drew COBOL2269) was the probes': those lines ran past column 72 of fixed form, cutting `WHEN OTHER` to
`WHEN OTH`; in free form the same-line repeat draws COBOL2073.

**Witnesses.** `ParseRecoveryCascadeTests` asserts the whole diagnostic list of each probe shape (21 rows: V2, V18, V19,
R14, R15, R10, R11, e1, e2, i14, S10, the nested and PERFORM siblings, the true repeats). Refusals at every edition:
`pb4299-move-fault-after-a-faulted-sentence-tail` (the MOVE fault after the faulted sentence, lost before),
`pb4382-evaluate-when-group-without-statement` (COBOL2072, was COBOL2073), and
`pb4300-evaluate-only-when-other-after-incomplete-display` (no COBOL2073). No inventory row changes verdict: every
program involved was already refused. Design doc section 3.9 carries the four rules.

**PB4360 (a withdrawn verb drew a bare COBOL0001).** After `>>COBOL-WORDS UNDEFINE "DISPLAY"`, `DISPLAY "X".` is a
user-defined word and a literal, not a statement (ISO 7.3.10.4 GR3: syntax requiring the word "shall not be available
for use in this compilation group"), and the error never named the directive. A new warning, COBOL5926, reported by
`CobolErrorListener.NoteWithdrawnWord` beside the syntax error where the withdrawn word is the offending token or the
one before it, names the UNDEFINE or SUBSTITUTE at the directive's own line (`CobolWordsMap.RemovedBy`), once per
directive: the parse-layer twin of PB4309's COBOL5776, placed like the margin-R note because the listener holds the line
map. Pinned by `CobolWordsDirectiveTests` (UNDEFINE, SUBSTITUTE, one note for two faults, and the control).
