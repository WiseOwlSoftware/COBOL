## Entry 2774 — 2026-10-29 21:44 PDT — PB3843: a fault found after its statements closed draws one diagnostic, not the recovery's second

**Mechanism.** An explicit scope terminator is optional (ISO §14.5.3.2), so a statement ends wherever its next token is not
its own and the refusal arrives at the sentence's period or its `statement+` loop, after the enclosing IF, STRING, ACCEPT or
EVALUATE has closed. ANTLR's recovery then resumed at the first token a statement could take and parsed the rest of the
sentence as statements of a sentence the construct was no longer part of: its END-IF met no IF, its `NOT ON …` phrase no
statement. The re-probe reproduced every row of the note on the build: `IF c MOVE 1 TO N END-ADD MOVE 2 TO N END-IF.` (true
COBOL0306, false COBOL0306 at END-IF), PBGF05 and `STRING … ON OVERFLOW … ON OVERFLOW … END-STRING` and
`pb331-read-key-before-lock` (true COBOL2269, false COBOL0306 at END-x), fz06606 (COBOL2269, a second at NOT, COBOL0901 at
END-ACCEPT), and PBGF09 (COBOL2073, false COBOL0306 at END-EVALUATE). One shared cause, so one fix.

**Fix.** `CobolErrorStrategy.Report` marks a fault whose winning hint is COBOL2269 (a phrase the statement does not print
there), COBOL2073 (a repeated WHEN OTHER) or COBOL0306 (a terminator no open statement owns) as found after its constructs
closed (`EndsAConstructEarly`), and `ConsumeUntil`, which both the loop recovery of `Sync` and `Recover` reach, then stops
that fault's resynchronization only at the sentence's separator period (`ResynchronizationSet`); the sentence's own period
is consumed when the sentence is the rule that recovers. The next sentence is parsed afresh, so a second FAULT in it is a
second diagnostic. Documented in `DESIGN-frontend-grammar.md` section 3.10.

**Tests.** `ParseRecoveryTailTests` (Unit): every scope terminator the grammar spells, read from the ATN map the hint reads
(`ScopeTerminatorWords`), against a closed statement and four kinds of open enclosing statement, exactly one diagnostic
each (COBOL0306, or COBOL0901 for the four that are also reservation-gated words); nine rows of the note and the sibling
phrases; the next sentence parsed afresh. The re-probe found that `pb331-read-two-from-one-lock-bracket` had pinned the
cascade: its RETRY source drew COBOL0901 (the gate) and then COBOL0001 at CONTINUE, the golden expected the second. The
source now writes ADVANCING ON LOCK with IGNORING LOCK (two of the bracket's three) and expects COBOL2269 at every edition.

**Not fixed, left as leads.** The reservation gate reads a stray END-ACCEPT / END-DISPLAY / END-RECEIVE / END-SEND, and a
misplaced RETRY, as user words (COBOL0901 where the statement's own refusal would name the fault). PBGF04 / 07 / 10 draw
COBOL0001 and then COBOL2072 after ANTLR's single-token deletion inside a statement header (a different recovery).

**Self-review.** Architecture: one place bends the resynchronization set, read by both recoveries. Full code: the winning
code is computed as `BuildMessage` picks it, and the flag is bounded by the report's token range so a stale mark never
reaches a later fault. Performance: no allocation on the success path. Duplication: the terminator list is the hint's own map.

