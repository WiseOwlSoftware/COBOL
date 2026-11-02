## Syntax-error recovery and wording: a faulted sentence no longer swallows the program, a left statement keeps its terminator, COBOL2073 names only a misplaced WHEN OTHER, a withdrawn verb is named, a literal at a sentence start is refused as one (PB4299, PB4382, PB4300, PB4360, PB4425, PB4301, PB4426)

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

**PB4425 (a literal where the next sentence begins drew a bare COBOL0001).** After `DISPLAY "A".` the literal `"B"`
passes every procedure-division list (each may end) and fails at the program's end, whose expected set (END PROGRAM, the
next program, the end of the file) names nothing about a sentence. Hint 0i now reads the boundary from the parse when
the parser's own set names nothing: `OffendingPosition.SentenceMayBeginAt` re-simulates the rule whose last token is
the period before the token (the sentence, or the paragraph, section or division header) over its own tokens through
its periods, and where every statement start may follow, the refusal is the PB4224 statement arm over FIRST of a
statement block (ISO 14.5.1: a sentence ends at its separator period; 14.5.3.1: a statement begins with the first word
of its statement name). A user-defined word or an integer there may be a paragraph-name whose period is missing (8.3.2.2
3)), which the missing-period guess keeps saying. Rows in `WrittenKindRefusalTests` at four editions, after a sentence,
END-IF, a paragraph header, a section header and the division header; refusal `pb4425-literal-where-the-next-sentence-begins`.

**PB4301 (a misspelled clause keyword was refused as the word after it).** Re-probed: `SELECT F1 ASIGN TO "f"` named TO,
`SINC LEFT` named LEFT, `OCURS 3 TIMES` drew a bare COBOL0001 at the 3 and a phantom level-3 entry's COBOL1941 at TIMES,
and `OCCURS 0 TO TIMES` named TO, a word of the clause itself (ISO 13.18.38.2: "integer-1 TO integer-2"). One root, the
by-name refusal of a foreign word run named the token that ended it, while `ClosedFormatPass` (which names the run's
opening and never runs once a unit has a parse error) names the opening. `OffendingPosition.ResidueRun` finds the error
production's node the parser stands in or has just completed; `ResidueRefusal` words the row's sentence from the run's
opening and the hint's `Subject` stands the report there; the run's tail belongs to the refusal, so no `3 TIMES` entry is
made of it. Where a period would let the sentence at the token follow (`FOO` then `05 C PIC X.`) the run is refused in a
report of its own, once, and the missing period is still reported. `ClosedFormatResidue` defers where the closed
construct before the token takes it (`ClosedConstructTakes`), and `AdmittedKinds` gained a fifth shape for a reserved word
where an operand is due, which also gives the predecessor's lead (`DISPLAY WHEN OTHER`, `ADD TO "X"`, `MOVE 1 TO WHEN`)
a named COBOL5506 where it drew the bare COBOL0001, without discarding the next statement (`DiagnosticHint.LeavesNoTail`).
It is said only of a set with no punctuation (`NUMVAL(A FROM 1)` has a whole argument) and only where the construct
completed just before the token could take nothing the set lacks (`SET ADDRESS OF X UP BY`: the parser has left X, so OF
and a subscript are missing from its set; `gn1-data-address-receiving-set-up-by` keeps its COBOL0001): both are the
PB4426 guard against an incomplete "expected" list. The new arm also took over eleven refusals that drew the bare COBOL0001
or a vague guess (COBOL0312 "check SELECT/ASSIGN TO"), now COBOL5506 naming the operand (`pb333`, `pb2728`, `pb2292`,
`pb3751`, `l1c24`, `l1c27`, `pb4207` x3, `pb4286`) or the run (`pb2727`: CHANNEL, not the IS after it); their `expect`
strings changed with them. i29 (the
unit with another parse error never reaches the pass) is PB4313's trade-off and stays there. Refusals at all four
editions: `pb4301-misspelled-clause-keyword-is-named`, `pb4301-occurs-to-without-integer-2`,
`pb4301-misspelled-occurs-before-an-integer`; `pb4300` now expects COBOL5506.

**PB4426 (the admitted set after a completed operand).** No arm prints that set as an expectation, so none is false today;
the guarantee is structural and stated in the design doc (section 3.9), the continuation walk over a closed WHEN phrase
re-opens the superset grammar and is not built. The dead statement-element counting in `AdmittedKinds.Name` is deleted.
Rows at 85, 2002, 2014 and 2023 for the selection object and the condition, and the refusal
`pb4426-literal-after-a-completed-selection-object`.

Self-review found the terminator pairing too wide in the predecessor's part (narrowed, 2 rows) and, in this part, the
new operand arm's COBOL5506 orphaning the next statement's tail (the PB4286 rows failed: fixed by `LeavesNoTail`) and its
list of the parser's full set for `NUMVAL(A FROM` (a continuation set, so now unsaid). semgrep verify passed with no rule
count risen. 