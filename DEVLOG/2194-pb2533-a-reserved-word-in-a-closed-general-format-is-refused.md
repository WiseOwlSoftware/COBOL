## Entry 2194 — 2026-10-18 03:50 PDT — PB2533: a reserved word in a closed general format is refused by name, at the parse layer, from the same table

**The defect.** Ten closed general formats end their alternative list in the `unrecognizedClause` error production,
and `ClosedFormatPass` refuses whatever it recognized by name (COBOLNET1941 / 1970 / 1971). The production begins at
an IDENTIFIER, so a RESERVED word the format does not print never entered it. `SD SF GLOBAL.` and `SD SF2 EXTERNAL.`
(kb/Work PB2533's repro; §13.4.6.2 prints `SD file-name-1 [ record-clause ] .`, and §13.18.27.3 SR1 d) admits GLOBAL
only in "A file description entry") drew `COBOL0307: unexpected 'GLOBAL'. A period may be missing …`. The re-probe on
this worktree's build covered all ten formats: the data description entry, FD, SD, SELECT and the computer paragraphs
drew COBOL0307 or COBOL0312, and I-O-CONTROL, SPECIAL-NAMES, the configuration section and the identification division
drew a bare COBOL0001. The SPECIAL-NAMES one was reported in `compilationUnit`, where nothing remembers the paragraph.

**Why not the grammar.** A reserved-word-led `genericClause` would compete with every named alternative of its list
(ANTLR would read a malformed `SD SF RECORD.` as a foreign word, which is false), and in the paragraph lists it would
compete with the headers that follow (`REPOSITORY.` after SPECIAL-NAMES would continue the paragraph). The grammar is
unchanged; only a comment on `genericClause` records why.

**The fix, in `CobolErrorStrategy`.** The 0g hint, `ClosedFormatResidue`, simulates the ATN from the parser's state
and its real context to the offending token. It re-takes the tokens a failed adaptive prediction looked past. If the
closure there reaches a call of `unrecognizedClause`, the calling rule's format is named through the new
`ClosedFormats.ByRuleIndex`, and the innermost open list wins. Two ATN-derived exclusions keep it off structure errors.
The first is a word that may follow the list's separator period (a missing period before `PROCEDURE DIVISION`). The
second is a word that opens a header (`HeaderWords`: the first token of a rule followed within it by a period, SECTION
or DIVISION, such as `I-O-CONTROL.` written before `FILE-CONTROL.`). A `Sync` override handles lists that may END. At
a state whose closure reaches the production (a per-state flag), it reports at once a reserved word that is outside the
full-context LL(1) set, so the parse fails at that token whatever follows and no legal program is touched. Without the
override, the base strategy exits the list and the error surfaces later (`SD SF RECORD 80 EXTERNAL.`,
`01 X PIC X BLOCK …`). The code and sentence are the pass's own. `ClosedFormat` now carries its catalogue
`Diagnostic`, which deletes the pass's three-way code mapping, and a `Refusal(word, owner)` wording. `OwnerName` moved
to `ClosedFormats`. Hints 7 (COBOL0305) and 19 (COBOL0312) are suppressed once a structural hint names the rule.

**After.** `SD SF GLOBAL.` gives `COBOLNET1970: unexpected 'GLOBAL'. 'GLOBAL' is not a clause of the sort-merge file
description entry for 'SF' — the ISO §13.4.6.2 general format lists the clauses that may be specified, and it is a
closed list.` Every format the parser reaches now gives one diagnostic. Not reached: a reserved word after a
SOURCE-/OBJECT-COMPUTER computer-name. There, ANTLR's error-recovery prediction takes the paragraph's exit alternative
and reads the name as an unrecognized configuration paragraph. It is reported as a lead.

**Goldens and witnesses.** Four negatives, at 85 2002 2014 2023:
- `pb2533-sd-reserved-word-clause`
- `pb2533-data-entry-reserved-word-clause`
- `pb2533-special-names-reserved-word-clause`
- `pb2533-identification-reserved-word-paragraph`

Four existing expectations now carry the named refusal, each checked against its source: `l1c08-entry-name-qualified`
('OF'), `l1c21-property-no-get-and-set` ('SET'), `pb1252-linkage-entry-using-phrase` ('USING') and
`pb489-linage-phrases-out-of-order` ('WITH'). Every wiseowl refusal whose expectation a parse-layer change can alter
was run at its editions: 2112 pairs, none lost apart from the case-only `da6-alphanumeric-numeric-operand`, which
predates this change. `ClosedFormatReservedWordTests` pins each format, one diagnostic each, plus three controls (the
missing period, a header out of order, a malformed RECORD clause). `ClosedFormatDriftTests` holds every rule that
calls the production to a `ByRuleIndex` entry. No inventory row was defective (the sources were already refused), so
`closes_rows: []` with its reason.

