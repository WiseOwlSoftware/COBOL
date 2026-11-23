## A separator period where a statement's operand is due is refused by naming the statement

PB6113. `SEARCH E AT END MOVE 1 TO.` drew the bare COBOL0001 "unexpected '.'". Re-probe on this build showed the AT END phrase is not the cause: `MOVE 1 TO.`, `ADD 1 TO.`, `DISPLAY.`, `MULTIPLY 2 BY.`, `ADD 1 TO N GIVING.` and the same inside ON SIZE ERROR, WHEN and READ's AT END all drew it. Hint 0i's operand arm (`AdmittedKinds.Refusal`) said "expected a user-defined word ..." only for a reserved word written where an operand is due; the period fell through.

Fix at that arm: the period counts when a statement is open around the parser (`CobolErrorStrategy.OpenStatementName`, so a data entry's or header's period is untouched), and the sentence names the statement: `'.' ends the MOVE statement where an operand is still due: expected a user-defined word, ... (§2023:5.2.6.2)` (COBOL5506; 1985 cites §1985:IV.2.1.4). Goldens refused at 85, 2002, 2014 and 2023: pb6113-search-at-end-move-no-receiver, pb6113-move-size-error-no-receiver, pb6113-add-no-receiver.

Not fixed, registered in the wave report: `SET I TO.` and `COMPUTE N =.` stay "cannot parse construct near '.'" because the walk at their failed prediction admits every statement start, which the arm's statement-position guard reads as a sentence boundary.

Verb-by-verb the code now differs: a bare verb whose period is also a paragraph header's (FREE, VALIDATE, RAISE, INVOKE, ALLOCATE) or whose form takes a statement (inline PERFORM) keeps the bare COBOL0001; the tests that pinned COBOL0001 for bare verbs accept either code. Existing tests that pinned COBOL0001 at an operand-less period (22) now expect COBOL5506.

No inventory row closes. Self-review found nothing further. Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 29,223/29,223 ran · RepoDrift 244/244 ran` (run 000004-20261123T155416Z, at 7017430), the three goldens passed in Conformance.trx.
