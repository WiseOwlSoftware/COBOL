## Error recovery: the nearest unterminated statement owns a left statement's terminator and phrases, and a hint 0i that named a legal statement start is silent (PB4447, PB4448, PB4449)

Three diagnostic-recovery defects from the review of the PB4436 branch, in `CobolErrorStrategy` and `AdmittedKinds`. Every
program involved was already refused; the defects drew a false diagnostic (PB4447, PB4448) or lost a true one (PB4449).
No inventory row changes verdict, none is claimed. Codes COBOL5956-5958 were not needed.

**PB4448.** `PERFORM UNTIL W-N = 2 DISPLAY "A" IF W-K = 2 END-IF END-IF END-PERFORM.` drew COBOL5506 "unexpected 'IF' ...
expected END-PERFORM, FINALLY or WHEN" and nothing after it. The note blamed hint 0i winning over hint 0a by priority; the
re-probe showed hint 0a is right to stay silent (IF may begin a statement there). The IF is the malformed statement, the
inline PERFORM's END-PERFORM is mandatory, so the parser leaves the block before the IF and its admitted set holds no
statement start; `AdmittedKinds.Refusal` named the IF "not expected" against that set. The operand arm of PB4426 already
asked what the construct completed just before the token could still take; the question belongs to every arm. Now a token
that construct could take is a fault further on: `Report` carries into the closed block, hint 0a names the IF's missing
statement-1 at the END-IF (COBOL2072), and the stray END-IF draws its COBOL0306. One golden, six unit rows (PERFORM with
UNTIL, TIMES, F3, an EVALUATE inside, an IF with an empty ELSE).

**PB4447.** `IF A EVALUATE W WHEN 1 IF B ELSE DISPLAY X END-IF DISPLAY "Y" END-EVALUATE END-IF.` drew COBOL2072 and a
false COBOL0306 at END-EVALUATE. The note's mechanism (`EndedWithItsTerminator` killing the EVALUATE's record) was not the
one: logging Recover, NoteLeft and the tokens the resynchronization consumed showed the EVALUATE never had a record. The
inner IF's recovery stops AT its ELSE (the outer IF's ELSE is in the recovery set), the outer IF takes `ELSE DISPLAY X` as
its else branch, the WHEN group's block cannot take the END-IF, the EVALUATE (optional END-EVALUATE) returns, and the
outer IF's optional END-IF matches the left IF's END-IF without any failure. Without the outer IF, `Recover` of the WHEN
group's block records the EVALUATE (PB4437); with one the prediction accepts the END-IF and no recovery runs. It is wider
than the note: an END-SEARCH, END-ADD after `ON SIZE ERROR`, any statement with an optional END-x inside any IF. Fix, the
nearest unterminated statement wins (ISO 14.5.3.2 1), 14.5.3.3 4)): `Recover` consumes a fault's own phrase word with it
(the ELSE the failed IF stopped at), as it consumes its terminator; and `Sync`, which the generated parser calls before
every optional END-x and every loop decision, consumes a scope terminator as a left statement's own where a statement of its
kind is open on the stack and a record due began after it (`ConsumesALeftStatementsTerminator`), so the older statement
never sees it. Found by the sibling sweep and fixed in the same change: a fault deep inside a statement ends the lists
around it early, not only the nearest (`IF A DISPLAY "Z" IF B IF C ELSE ... END-IF END-IF ELSE DISPLAY "Y" END-IF` drew a
COBOL0001 at the ELSE and a false COBOL0306 on main), so `NoteLeftBefore` now climbs the enclosing statements. The record
became a class (`LeftStatement`) so a predicate can read its start. Two goldens (a stray END-IF and a stray END-PERFORM
that the old recovery swallowed), thirteen unit rows, nine of which fail on main.

**PB4449.** The PB4436 suppression of a phrase word kept the left statement's record after taking the word, so every later
occurrence was silent: `IF A DISPLAY "Q" IF B END-IF ELSE DISPLAY "Z" ELSE DISPLAY "Y" END-IF.` drew COBOL2072 only, and a
second WHEN OTHER after a left EVALUATE or PERFORM drew nothing where a lone one draws COBOL2073. The citation finding was
right too: ISO 14.5.3.2 2) says only that a terminator also ends the unterminated statements between; the
phrase-association rule is 14.5.3.3 4), and the comment, the test's comment and the design paragraph now cite it. The note
suggested a permitted count read from the ATN; a count per word cannot tell WHEN from WHEN OTHER, and the ATN already
says it all if it is walked. `ResimulationWalk.AfterABlockOf(rule)` is seeded after every `statementBlock` call of the
statement's rule (`BlockFollows`, with the frames of the calls down to it, replacing `PhraseWords`), and each
`LeftStatement` keeps the phrase words taken for it; a word is its own only where the walk takes the earlier phrases and
then this one (`TakesPhraseAt`: the phrase's words up to the first position that takes a statement, then one CONTINUE for
the block). After ELSE and its block the IF's walk takes only END-IF; after WHEN OTHER the EVALUATE's only END-EVALUATE;
the WHEN loop takes any number of WHENs. The repeated word is then reported as on a statement the parse did not leave, and
hint 0h names the left statement that prints it (`NameOfALeftStatementPrinting`), the IF, not the DISPLAY before the
word. One golden, seven unit rows and a Fact for the name. **Not fixed, a lead:** the paired conditional phrases (`NOT ON
SIZE ERROR` twice after a left ADD) are one `conditionalPhrases` loop whose count a predicate decides; the walk does not
evaluate it, so such a repeat stays silent as before. A first attempt that refused them by kind turned the silence into a
false COBOL0306 at the END-ADD (the carried report), which is worse, so it was not kept.

**Branch review round 1, R1-1 (blocking, fixed).** `Sync`'s `ConsumesALeftStatementsTerminator` took an older open IF's
END-IF as a left IF's own even after the next phrase of a containing statement had ended the left IF (ISO §14.5.3.3 4) b)):
`IF A IF B ELSE DISPLAY X ELSE DISPLAY Y END-IF END-IF.` gave COBOL2072 only where main gave the true stray COBOL0306 at the
second END-IF (the same with a WHEN of an open EVALUATE). The record hygiene retired a record only at a period, at its
own terminator or when an enclosing statement of another kind ended; nothing retired it at a phrase the parse gave to the
open statement without a failure. `EndsLeftStatementsAtAContainingStatementsPhrase` now runs first in `Sync`: where the
token is a phrase word an open statement takes (`OpenStatementTaking`, a walk of the rule invocations outward over their own
next tokens), the records begun after that statement are decided newest first, the word the next phrase of the first
whose format permits it (recorded, so a repeat decides the other way) and a record whose format does not ended by it.
The first attempt retired a record at the second synchronization point of the same word (the word was already in its
`Phrases`); a word taken already is the record's own. One golden refused at the four editions, six unit rows (the
finding's two scenarios, the correct whole programs, the condition-fault IF whose ELSE is its own). Sweep over the arms
(WHEN OTHER twice, ON SIZE ERROR/NOT ON SIZE ERROR inside an IF, EVALUATE inside an IF): three inputs changed, each
toward the spec (the stray END-IF or END-ADD now drawn where the old recovery swallowed it). R1-2 (the CONTINUE citation,
§14.9.9.1) fixed. R1-3 stays a lead (the repeated paired phrase needs the left-statement arm in
`ConditionalPhraseAssociation`, another file). New lead: an open statement the parse let take a phrase that the ideal
reading gives the left IF (`IF A IF B NOT <> 1 DISPLAY C ELSE D ELSE E END-IF END-IF`) has none for the second ELSE.

**Self-review.** Architecture: the two terminator decisions (unowned, older-open) share `TakenByALeftStatement` and one
record type; the phrase test is one walk, no second list of words. Full code: the citations are `cite.py --check`ed (14.5.3.2
1), 2); 14.5.3.3 4); 5.2.6.3). Performance: the walk runs on the error path only, seeds cached per rule as descriptors (an
`ATNConfig` a closure merges into is never shared). Duplication: `PhraseWords`, `CollectPhraseWords` deleted, no shim.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,377/39,377 · Characterization 37/37 cases ran (skipped 0) ===` (run 000001-20261102T194504Z in the review finisher worktree, at the code head 397c11b7d; the DEVLOG line added after it is inert). The four new goldens' refusals ran at all four editions in `Conformance.trx`. Leads for the registrar: a repeated paired phrase after a left statement stays silent; an enclosing statement that returns normally after a recovery has no record (a false COBOL0306 at its END-IF, identical on main); an open statement the parse let take a phrase the ideal reading gives the left IF leaves none for the next one.

**Branch review round 2, R2-1 (blocking, fixed).** `EndsLeftStatementsAtAContainingStatementsPhrase` retired a left
statement's record at the START of the next statement of a containing block, not only at a phrase word: at a
synchronization point inside the statement before it (a MOVE or ADD receiving list, a DISPLAY operand list) the rule the
parser is in does not take DISPLAY, the enclosing block does, so `OpenStatementTaking` returned the open IF or PERFORM, the
left IF was no format that takes DISPLAY as its phrase, and its record was retired: its own END-IF then went to the open
statement and the open statement's END-IF drew a false COBOL0306 (`IF A IF B NOT <> 1 MOVE 1 TO N DISPLAY 'C' END-IF
END-IF.`; also ADD, two DISPLAYs before an ELSE, an inline PERFORM). The fix is the doc comment's own claim made true: a
token in FIRST(`statement`) (`BeginsAStatement`, read once from the ATN, no word list) is no phrase and ends nothing
(ISO §14.5.3.3 4) b)). Five unit rows (`TheStartOfTheNextStatement_EndsNoStatementTheRecoveryLeft`), the reviewer's five
inputs; the probe on the reviewer's first input drew COBOL0001 + COBOL0306 before and COBOL0001 alone after. No golden:
the program is refused at its COBOL0001 whatever follows, and a refusal golden's `expect` names a substring that must be
present, so it cannot assert that a cascade diagnostic is absent; the unit theory asserts the exact diagnostic list.

Gate after R2-1: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,382/39,382 · Characterization 37/37 cases ran (skipped 0) ===` (run 000001-20261102T195835Z at the code head a86d082e7; the DEVLOG line added after it is inert).
