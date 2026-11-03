## A bare verb is a header only where a procedure reference names it, a stray word belongs to the statement that wrote it, and diagnostics quote what the user wrote

The operator lane ran out of quota, so the Mythos lane took over its idle branch (`worktree-wf_a45e0bcf-285-1`, finished
and gated, unreviewed) and finished its NEXT list. The merge with main had one conflict: main had split
`ReferenceResolver`, so the branch's qualified-index-name edition gate (PB4305) moved to
`NameResolution.ResolveIndexName`, where the index lookup now lives. PB2103's part 2 was dropped (main landed it).

### PB4283: a verb and its period alone is a header only where the program names the word

`FREE.`, `VALIDATE.`, `ALLOCATE.`, `RAISE.` and `INVOKE.` alone in a paragraph drew COBOL0901 ("reserved word used as a
user-defined word") at `--std 2023`, where the sentence is a statement lacking its operand. The branch's earlier fix
(`STOP.`, `DISPLAY.`, `READ.`, the verbs reserved at every edition) had decided it with no evidence, and PB4206 pins the
opposite for a header that a `PERFORM RECEIVE.` references. The two disagreed because the only evidence that the source
USES the word as a name (§8.3.2.1 1) is a procedure reference elsewhere, which the parse does not know and the bound tree does.

The decision is one now, asked in three places that were three copies of the question. `IsBareVerbSentence` (a statement
opener read off the ATN, a period on each side) and `NamedAsAProcedureElsewhere` (every other token of the word's type is
re-read in its own sentence with the word retyped `IDENTIFIER`; a reading that puts it under `procedureName` is a reference,
so `FREE ptr.` is not) answer `procedureHeaderHere` (the grammar's predicate for a gated word), `ReservedWordUse`
(no violation, so the failed prediction reports as the statement's own) and `ReservedProcedureName` (the readings of a verb
reserved everywhere). With the evidence, a word is named once, at its first use: `CobolErrorStrategy` keeps the messages it
gave (`_procedureWordsNamed`), the bound-tree funnel's rule for a gated word. `PERFORM DISPLAY.` with the header `DISPLAY.` had
drawn COBOL0901 at the reference and a COBOL0001 at the header; it draws the one COBOL0901. Without it `FREE.` draws
`cannot parse construct near 'FREE'` and `DISPLAY.` `unexpected '.'`. Goldens: two cases under `8.3.2.1-General` (the refusal at
2002, 2014 and 2023, and `VALIDATE.` as a legal header at COBOL-85) and twelve rows of
`ReservedWordPositionConformanceTests.ABareVerbSentence_IsTheHeaderOnlyWhereAProcedureReferenceNamesIt`.
`DESIGN-frontend-grammar.md`'s reserved-procedure-name paragraph is rewritten.

### PB4285 item 3: the stray word of the next statement

`MOVE W-X TO` then `ADD 1 FROM W-Y.` drew one COBOL0901 at the ADD and lost the ADD's own fault: `ADD 1` parses as a prefix
and FROM follows no statement, so the statement reading was None. The weighing the note proposed (every refusal of two tokens
or more, as `NameReach` weighs one) made `MOVE FREE W-X TO W-Y.` a statement too, which PB3248's R2-1 rows pin as the operand.
What tells them apart is whether the sentence needs the token the prefix stopped at: `MOVE X TO W-Y.` reads whole with the prefix
as one operand, `MOVE W-X TO X FROM W-Y.` does not. `ReadingParses` reports where a reading stopped and whether the stop is a
token the rule is followed by nowhere; `StatementReading.CutShort` is `Faulted` and covers every stop; a stray stop asks
`SentenceReadsWithOneOperandFor`. After: the missing receiver (COBOL5506) and the ADD's stray FROM (COBOL2269), no COBOL0901.
Item 2 is discharged: a statement reading that ends at another verb's phrase word (`... FREE W-Y ELSE`) is complete exactly as
at the period, and no wrong report exists to build a repro from.

### PB4310 and PB4431 and PB4434

`CompilerDirectiveLine.TryParse` no longer trims a trailing CR (owner decision 2026-10-31, DOC-A.1-156): `>>SOURCE FORMAT
FIXED` + CR + CR + LF switched the format and now draws COBOL1911, as the same CR on a source line is refused. The two dead tab
rules are deleted, and `PhysicalLinesDriftTests` enumerates the `WiseOwl.Cobol.Editions` assembly, which had been outside its
population. CallBinder quoted five operand nodes by `GetText()` (`AOFG`, a glued qualified name) in its BY VALUE and bare-argument
messages, and looked a qualified SCREEN SECTION name up by its glued text, so it never matched; all quote `WrittenText` now. The
other binders still hold about ninety such messages (PB4252's analysis). PB4434's stale `GroupRepository.Find` prose in
`DESIGN-external-repository.md` names `GroupRepositoryProvider`; the `SearchAllFormat2Rules` comment was already current.
