## Three 1985 rules that declare their own result undefined close on a derivation (PB5373 class 1)

kb/Work PB5373 decided how the 21 no-witness rules of the 1985 edition close, and PB5374 built the derivation arm for a
rule whose own text says its result is undefined. This branch records the first rows on that arm.

**What closed.** §1985:XIV.2.2.4 GR7 and its level 1 twin GR29 ("If the MCS attempts to schedule a program lacking an
INITIAL clause, the results are undefined.") and §1985:XIV.3.6.4 GR5 ("The effect of having special control characters
... is undefined."). Each citation was checked with `cite.py --check`. Each row has a section 8 entry in
`docs/CONFORMANCE.md` that quotes the undefined words from the rule's own catalog text, signed `claude: 2026-11-12`, and
states what WiseOwl does anyway: a scheduled program with no INITIAL CD runs as any other program, and control characters
are carried as data. Batch `w20262028a/batch-class1.json`: 3 rows changed, 1985 GAP 210 to 207. `audit_derivations.py
--check` reports 48 derivation rows, all standing.

**What did not, and why.** §1985:IV.4.4.2 items L2.1 and L2.2 were planned for the same arm, but the sentence declaring
the flow of control undefined closes the list, and the 1985 catalog files it under item L2.5. The arm checks a quotation
against the row's own text, so these two stay open; the catalog defect is reported as a lead. The class 2 rows (no reel,
no device with vertical positioning or superimposition, no source listing) are argued arms and need an UPHELD refuter
record before they are recorded. Their documentation is already in place, and the report proposes each derivation.

**Also.** `record_verdicts.py` printed "owner-signed DERIVATION" for every derivation closure; it now says "signed",
since a deciding session can sign too. The branch carries w20262027a's PB5118 group (merged, one conflict in
`tests/doc-facts/compiler-facts.json` resolved by keeping both codes).
