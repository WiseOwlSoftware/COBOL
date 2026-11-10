## PB5060 (1985 half): the 1985 reserved-word list is swept and its order is a gate check

**The question.** PB5053 swept the 2023 reserved-word list pair by pair and corrected four entries out of order and two
misprints (the 2023 Addendum's C15 and C16). The 1985 and 2002 lists had no such sweep. This landing does 1985; 2002
waits for its transcription's reconciliation (the owner's 1985-before-2002 rule) and stays open in kb/Work PB5060.

**The re-probe.** PDF pages 191 and 192 of the 1985 standard (folios IV-45, IV-46) were rendered and read against
`specs/ANSI_COBOL_1985.md`. An adjacent-pair check of the list, down each column and across columns and pages, found
no entry out of order, and no word is misprinted: the list's 350 words are exactly the 350 that
`tests/version-matrix/reserved-words.json` flags for COBOL-85, which come from GnuCOBOL's list and not from this text.
So the printed 1985 list needs no Errata entry, unlike 2023's.

**What was wrong.** One slip of the transcription, not of the standard. The page leaves a blank line where the initial
letter changes. When Amendment 2's `FUNCTION` was applied in place (Errata C207) it was written into the blank
between `FROM` and `GENERATE`, so the F and G words ran together. The blank is restored: column 3 of the page-191
table moves down one row below `FUNCTION` and the table gains a row. `LEDGER-1985.json` records it as the sweep
`reserved-word-letter-gap(PB5060)`.

**The fix shape.** `scripts/spec/reserved_word_order.py` is the sweep kept as a check: one row per swept edition (a
reader of that transcription's list, whether it keeps the printed letter gaps, and the pairs the standard prints
against the order on purpose), and four rules read from the printed lists: each word greater than the one before it
(a hyphen and a digit sort before a letter, the order of the ASCII codes), a kept pair still adjacent and still
against the order, the special-character words last, and one blank between letter groups inside a column.
`verify_publishable.py` calls it, so the gate's SPEC CORRECTIONS and SPEC CORRECTIONS 1985 audits run it with no new
audit entry. The 2023 row pins PB5053's result, which until now no check held: `B-SHIFT-R` before `B-SHIFT-LC` and
`FLOAT-BINARY-64` before `FLOAT-BINARY-128` are its two kept pairs (C16). 2002 has no row, and the audit prints that
its order is not checked.

**Seen to fail.** On the tree before the repair the check reports `no blank line stands between FUNCTION and GENERATE:
one separates two letter groups`. The self-test plants each rule's violation (19 cases), among them C16's
`END-RECEIVE` before `END-READ`.

**Goldens and rows.** None: the change reaches no compiler behaviour (the compiler's reserved words do not come from
these lists) and closes no inventory row. No diagnostic code was used (COBOL6811 to COBOL6813 returned).

**Self-review.** Architecture: one home for the order rule, and the publishable audit only calls it. Full code: an
unreadable or empty list is a finding, never a pass, and an edition with no row is named, never passed. Duplication:
the bounds of the 2023 list are also read by the two one-shot repair scripts under `scripts/spec/repairs/` and by
`gen-reserved-words.ps1`; they stay as they are, since the repairs are history and the generator is PowerShell.
