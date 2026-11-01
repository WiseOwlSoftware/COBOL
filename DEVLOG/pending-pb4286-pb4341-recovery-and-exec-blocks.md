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
COBOL5818 (`embedded-precompiler-block`), once, at its EXEC.

The first shape found a block only at a parse decision whose walk could not read it as COBOL and consumed it as nothing.
Branch review round 1 refuted it twice. R1-1: a block that was the only statement of an IF's or an ELSE's branch or a
WHEN left the construct without its statement (`IF … EXEC SQL OPEN C1 END-EXEC ELSE … END-IF` drew a false COBOL0306
at END-IF, `WHEN 1 EXEC … WHEN OTHER` a false COBOL2073, blocks in both branches lost every later block and fault). Read
as a statement in place, it was still wrong: a prediction made BEFORE the parser met the block had looked into its text
(`WHEN 1 EXEC SQL` predicted the class condition "1 IS EXEC") or failed on it (`WHEN OTHER EXEC SQL` drew COBOL2072 at
WHEN), so no arm the parser reaches at the block can be right. R1-2: the block's scan stopped at ANY period, so the
qualified SQL name `T.A` left the block unrecognized, with the PB4341 symptom.

So the block is one token from before the first reader of the stream. `EmbeddedBlockReading.Read`, in
`Frontend.LexAndParseOnce` after the token rewriters, makes each block (EXEC, a word, and the text through the first
END-EXEC with no SEPARATOR period between, §8.3.5 rule 3) one `EMBEDDED_BLOCK` token, a parser `tokens {}` type no lexer
rule emits, and moves the rest to the new `EMBEDDED_BLOCK_TEXT` channel. Where a statement may begin the grammar takes it
as `embeddedBlockStatement` (one `statement` alternative, keyword-token-led, so no decision's lookahead changes);
elsewhere `CobolErrorStrategy.Sync` consumes it as nothing (`SkipsEmbeddedBlock`, with the period after it where the
block was the whole entry, `WasAWholeUnit`). The reading reports COBOL5818 after the parse, placed by position, and a
source that holds a block never parses. The separator, literal, PICTURE and word-character screens skip the block's
text, which is another language, through one new test, `ProgramText.Holds` (with PB1705's `ABSENT_DEBUG_LINE`), so SQL's
`T.A` and `(A,B)` no longer draw COBOL2632 and COBOL2630. EXEC is not reserved, so the source reads EXEC as an opener
only when every EXEC of it opens a block; a source that writes EXEC as a word anywhere is read as COBOL throughout, as the
standard reads it. The reading also answers lead 1 of the first report: `DISPLAY W-X` then `EXEC CICS RETURN END-EXEC.`
drew three COBOL1639 and now draws the one COBOL5818. The walk-based arms (`EmbeddedBlock.Around`,
`ReportEmbeddedBlock`, the `ConsumeUntil` block arm) are deleted.

Goldens: `wiseowl/cases/4.2.10-Nonstandard-extensions/pb4341-exec-sql-block-refused` (four blocks, four COBOL5818,
nothing else, at all four editions), `pb4341-exec-sql-block-in-branches` (IF and ELSE, WHEN 1 and WHEN OTHER, an inline
PERFORM, `T.A`: six COBOL5818 at all four editions) and `pb4341-exec-is-a-user-word` (accepted at all four, output
derived from §14.9.11.4 GR1); `EmbeddedPrecompilerBlockTests` pins the counts (thirteen one-block shapes, every block
of a program and the fault after them, both branches and everything after, a §8.9 gate re-parse, a source that writes
EXEC as a word, qualified names), and its compile of the user-word program, a duplicate of the golden (review lead
R1-3), is gone. Docs: CONFORMANCE.md §3.2 item 4, the manual's migrating table, DESIGN-frontend-grammar §3.9. Codes
used: COBOL5818; COBOL5819 and COBOL5820 returned.

**Self-review and gate.** Four lenses: each recovery rule lives in one override (`SingleTokenDeletion`, `Resynchronize`,
`Sync`, `Report`); a block is found in one place, before the parse, and reported in one place, after it; the reading is
one linear scan that reads the text only of a four-character IDENTIFIER and of a block's own tokens, and the strategy's
hot path gains one token-type test; the "not program text" channel rule is written once (`ProgramText`) for its four
readers. The first shape's gate (run 000001-20261101T111516Z on 45b42a830) read `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run ===`; the review finisher's head carries its
own leg-1 gate record, which `enqueue.py land` reads, and the targeted Unit tests (692) and the conformance generator
check ran green on it before that gate.
