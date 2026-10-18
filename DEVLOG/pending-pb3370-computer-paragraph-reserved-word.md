## kb/Work PB3370: a reserved word after a SOURCE-COMPUTER or OBJECT-COMPUTER computer-name is named in the paragraph's format

**Mechanism.** `SOURCE-COMPUTER. XYZ GLOBAL.` (and the OBJECT-COMPUTER twin) drew a bare `COBOL0307: unexpected
'GLOBAL'`. XYZ is either the computer-name or an unrecognized configuration paragraph; both readings fail at GLOBAL,
and ANTLR's error-time prediction (`GetSynValidOrSemInvalidAltThatFinishedDecisionEntryRule`) returns the lowest
alternative that has already left the decision's rule. While the paragraph's second period was required, only the
exit alternative had left it, so XYZ was read through the configuration section's `unrecognizedClause` and GLOBAL
failed where no closed format stands. The re-probe on this worktree's build reproduced both repros and four siblings
(`XYZ GLOBAL EXTERNAL.`, `XYZ WIBBLE GLOBAL.` in both paragraphs, the SOURCE-COMPUTER form before OBJECT-COMPUTER).

**Fix shape.** Both rules superset-parse the second period (`… DOT?`), so the name's alternative leaves the rule too
and wins the error-time choice. The existing CobolErrorStrategy Sync then names GLOBAL (COBOLNET1970, "not a clause
of the SOURCE-COMPUTER paragraph"). The period is required back through the one table: `ParagraphPeriods` gains
SOURCE-COMPUTER and OBJECT-COMPUTER rows (§12.3.5.3 SR1, §12.3.6.3 SR4 let it go only with nothing after the header),
so a name or clause without it is COBOLNET3408 at the name, where it was a COBOL0307 at the next header. The sibling
arm: in `SOURCE-COMPUTER. XYZ WIBBLE GLOBAL.` the run WIBBLE ends at GLOBAL and the paragraph has no list loop to
Sync. CobolErrorStrategy now treats a Sync inside the word run as a residue position, and its 0g closure records the
list whose call of the production the parser is still inside. This also names the residue after a word run in the
configuration section and the identification division, the other two formats whose production is not in a loop.
The first gate found a recovery regression: an optional period gets no single-token deletion, so `OBJECT-COMPUTER.
XYZ ) .` ended the paragraph at ')' and the recovery swallowed the next SELECT entry's error (DiagnosticPositionTests'
NIST column case). Sync now deletes a token that no path can take when a period follows it and the rule could take
the period, as ANTLR does before a required one.

**Branch review round 1.** R1-1: that deletion also took a paragraph HEADER word followed by its own period.
`SPECIAL-NAMES. DECIMAL-POINT IS COMMA` (period missing) then `FILE-CONTROL.` deleted FILE-CONTROL and refused the
SELECT entries as SPECIAL-NAMES clauses (one diagnostic on main, three on the branch; five at I-O-CONTROL, three at
REPOSITORY). The deletion now skips `HeaderWords`, the ATN-read set the closed-format arm already excludes, and every
probe is back to main's single diagnostic at the header. `ClosedFormatReservedWordTests` gains a theory with one case
per `ParagraphPeriods` row and a fact that fails when a row has no case. R1-2: the design doc's old "⚠ Not reached"
paragraph for `SOURCE-COMPUTER. XYZ GLOBAL.` is gone, and `ParagraphPeriods`' summary no longer counts its rows.

**Goldens.** Positive `pb3370_computer_paragraph_second_period` (85 up); negatives
`pb3370-source-computer-reserved-word-clause`, `pb3370-object-computer-reserved-word-clause`,
`pb3370-source-computer-period-missing`, `pb3370-object-computer-period-missing` (every edition). Unit cases in
`ClosedFormatReservedWordTests`; `ParagraphPeriodDriftTests` asserts the two rules. No inventory row moved (all four
rules were CONFORMS; the source was always refused). A literal after the computer-name (`XYZ "LIT".`) is still
misattributed, because `genericClause` opens only at an IDENTIFIER. It is reported as a lead and not widened here.
