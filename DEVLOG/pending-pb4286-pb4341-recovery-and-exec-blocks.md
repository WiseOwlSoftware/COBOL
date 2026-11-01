## PB4286, PB4341: the next statement after an incomplete one is read as written; an EXEC block is named

**PB4286 (wave 20261804 group B).** Re-probed on base 2ce2e70a2: `RECEIVE FROM W-X GIVING` then `DISPLAY W-Y.` drew
COBOL0001 at DISPLAY and a false second COBOL0001 at the period, where `ADD 1 TO` then `DISPLAY W-Y.` drew one. The note
named the reserved-operand consume (`IsReportedReservedOperand`), but DISPLAY is not reservation-gated and no COBOL0901
is made; the cascade came from two other mechanisms, one after the other.

1. Single-token deletion dropped the DISPLAY. ISO §14.9.31.2 prints `GIVING identifier-1 data-name-2`, two
   unbracketed operands; PB4133's decline asks only whether the token may follow the ONE missing element, and
   identifier-1's follow is data-name-2, so the deletion acted because W-Y fits identifier-1. The probes found the same
   shape at `RECEIVE FROM` + `DISPLAY W-Y.` and `SEARCH ALL` + `DISPLAY W-Y.` (each drew a false COBOL5506 at the period).
   `SingleTokenDeletion` now also declines when `BeginsTheNextStatement`: the base deletion would act, the token may
   follow the innermost open `statement` completed (the ATN's follow in the real context), and it begins a statement
   that is a complete reading (PB3248's `LookaheadBeginsAStatementThatParses`).
2. With the deletion declined, ANTLR's `Recover` consumed the DISPLAY anyway: the missing identifier-1 resynchronized
   at it, the missing data-name-2 failed at the same `dataReference` ATN state, and the base guard against a recovery
   that makes no progress keys on the state alone, so it consumed one token, and the rest of the sentence was lost into
   the RECEIVE (`... DISPLAY W-Y MOVE 1 W-N.` reported nothing for the MOVE). `CobolErrorStrategy.Resynchronize`
   replaces the base call and keys the guard on (state, invoking state). A loop failing again re-enters the same state
   from the same call and still consumes, and at one token the pairs are finite, so every recovery still makes progress.

Goldens: `wiseowl/cases/14.9.31-RECEIVE/pb4286-receive-giving-operands-omitted` (the repro, COBOL0001 at DISPLAY) and
`pb4286-receive-giving-omitted-next-statement-read` (the MOVE's own fault is reported, which fails under either
mechanism); `ParseRecoveryDeletionTests` pins the count at one for six shapes and at two for the next-statement case.
The whole Unit, Conformance and Characterization projects ran green on the change before the gate (the two
ExternalCorpusPopulation drift tests need the external corpus a fresh worktree lacks). No inventory row changes
verdict. Design: DESIGN-frontend-grammar.md §3.11 carries both rules.

**PB4341.** Re-probed: `EXEC SQL SELECT A INTO :W-X FROM T END-EXEC.` drew `COBOL0001: cannot parse construct near
'SQL'`, the message of a typo, and a program with blocks in WORKING-STORAGE and the procedure division drew ONE
diagnostic in all: each block failed in a decision of `compilationUnit` or `procedureDivision`, whose recovery lost the
rest of the program. ISO/IEC 1989 defines no EXEC construct and §8.9 reserves neither EXEC nor END-EXEC; §4.2.14 names
precompilers as components outside the implementation. No support is claimed, so the block draws a named refusal,
COBOL5818 (`embedded-precompiler-block`), once, at its EXEC. `CobolErrorStrategy` recognizes a block (EXEC that a word
follows, through the first END-EXEC of the unit) only where the parse cannot read it as COBOL: at a decision whose walk
cannot take the block's tokens and the next token (`SkipsEmbeddedBlock`, from `Sync`; the block is consumed there, and
its period with it where the block stood where the list may end, `WasAWholeUnit`), and at a syntax error inside a block
the decision read (`ReportEmbeddedBlock` covers the block; `ConsumeUntil` consumes it whole and leaves a period right
after it for the unit). `DISPLAY EXEC SQL END-EXEC.` naming three data items still compiles. Goldens:
`wiseowl/cases/4.2.10-Nonstandard-extensions/pb4341-exec-sql-block-refused` (four blocks, four COBOL5818, nothing else,
at all four editions) and `pb4341-exec-is-a-user-word` (accepted at all four, output derived from §14.9.11.4 GR1);
`EmbeddedPrecompilerBlockTests` pins the counts. Docs: CONFORMANCE.md §3.2 item 4, the manual's migrating table,
DESIGN-frontend-grammar §3.9 (the one spelling-keyed recognition, and why it is not a guess). Codes used: COBOL5818;
COBOL5819 and COBOL5820 returned.
