## Entry 2999 — 2026-11-02 08:25 PST — Error recovery: a consumed terminator ends the recovery, and a left statement lives until its own terminator (PB4436, PB4437, PB4438)

Three diagnostic-recovery defects in `CobolErrorStrategy`, all in the bookkeeping PB4382 added for the statements a
syntax recovery leaves before their end (ISO §14.5.3.2 1), 2)). Every program involved was already refused; the defects
lost a true COBOL0306 (PB4436, PB4438) or drew a false one (PB4437). No inventory row changes verdict.

**PB4438 and PB4436 are one root.** `IF W-N = 1 END-IF END-IF.` drew COBOL2072 only. `Recover` consumed the empty IF's
own END-IF and then resynchronized: the second END-IF is in no recovery set at the sentence level, so it was swallowed
to the period. Worse, ANTLR stays in its error condition until a match succeeds, so even where the second END-IF
reached the parser its report was silenced. PB4436 is the same swallow one level in: the empty inner IF of
`IF A DISPLAY "A" IF B END-IF END-IF DISPLAY "X" END-IF.` consumed its own END-IF, the resynchronization then took the
ENCLOSING IF's END-IF, and the enclosing IF's record outlived its terminator and took the true stray END-IF as its own.
The note blamed `NoteLeftBefore`; the probe showed the records were right and the recovery ate the token. Fix: a
terminator consumed as a statement's own ends the recovery, in both arms that consume one. `Recover` returns after
`ReportMatch` (which ends the error condition, as `Match` does) and `Consume`; `ConsumeUntil` does the same for the
terminator `EndsAStatementTheRecoveryLeft` suppressed the report for (the loop recovery of `Sync`: `IF W-N NOT <> 1
DISPLAY "C" END-IF END-IF.`, the same swallow through the second arm, found by the sibling sweep), through
`_leftStatementsTerminator`. A recovery that returns from a unit ending at its period keeps the discard to the period.

**PB4437.** `EVALUATE W-N WHEN 1 IF W-K = 1 ELSE DISPLAY W-A END-IF DISPLAY "X" END-EVALUATE.` drew a false COBOL0306 at
the IF's END-IF, which then discarded the rest of the sentence. The WHEN group's `statementBlock` cannot take the left
IF's END-IF, so the EVALUATE returns before the next statement without its END-EVALUATE (the END-x is optional), and
`StillOpen` read "the enclosing EVALUATE returned" as "its terminator ended the IF too". Fix: a record lives until its
statement ENDS IN its own terminator (`EndedWithItsTerminator`, used by `StillOpen` and by the record loop), and
`Recover` in a `statementBlock` records the owning statement as left (`OwningStatement`), so the EVALUATE's or SEARCH's
END-x is its own. The sweep covered EVALUATE, SEARCH, PERFORM, ADD ON SIZE ERROR, and an EVALUATE nested in an IF.

**Goldens.** Three refusals in `14.5.3.2-Explicit-scope-termination` (each expects the COBOL0306 sentence the baseline
never drew): `pb4438-…`, `pb4436-…`, `pb4437-…`. Whole-list unit rows in `ParseRecoveryCascadeTests` (three new theories,
15 rows), 285 `ParseRecovery*` tests green.

**Branch review round 1 (R1-1, blocking).** Ending the error condition at the consumed terminator un-masked the failure at
the next phrase word of an enclosing statement the prediction had closed early: `IF A DISPLAY "Q" IF B END-IF ELSE DISPLAY
"Z" END-IF.` drew a false COBOL0001 at the ELSE (main drew COBOL2072 only), and a WHEN or WHEN OTHER after a malformed IF
in an EVALUATE or SEARCH drew a false COBOL0001 or COBOL2073. Fix: `Report` suppresses a word that a still-due left
statement's rule lets follow an imperative block (`ContinuesAStatementTheRecoveryLeft`, `PhraseWords` read from the ATN,
`TakenByALeftStatement` shared with the terminator test). A discard of the tail to the period was tried first and dropped:
it lost genuine later faults main reported (`... MOVE TO W-N. END-IF`). One golden
(`pb4436-stray-end-if-after-an-else-of-a-left-if`) and a 12-row unit theory
(`APhraseWordOfAStatementTheRecoveryLeft_IsItsOwn_AndDrawsNoSecondReport`). The earlier lead 1 (a WHEN after the left IF's
END-IF) is closed by the same change. **Left open, as a lead in the report:** an EVALUATE nested in an IF with such a
first-in-group IF draws a false COBOL0306 at its END-EVALUATE (the outer IF's optional END-IF matches the left IF's
terminator without a failure, so no recovery sees it; identical on main).

**Self-review** (architecture, full code, performance, duplication): the new code runs on the error path only; the
terminator-match test is one helper (`EndedWithItsTerminator`) used by both the record loop and `StillOpen`;
`OwningStatement` repeats `EnclosingStatement`'s ancestor walk without its kind filter (kept apart: different question).
The first gate of the review fix went RED on `ReservedWordPositionConformanceTests` (`IF W-N = 1 MOVE W-X TO` / `ADD 1 TO`
/ `ELSE`: the ADD's own fault at the ELSE was taken for the left IF's phrase), so the suppression holds only where the
statement before the word is complete (`AStatementMayEndBefore`), pinned by a unit row.
Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,899/20,899 · Unit 39,343/39,343 · Characterization 37/37` (run
000002-20261102T160657Z); the four new refusals passed at 85, 2002, 2014 and 2023; `semgrep/verify.py` PASS.

