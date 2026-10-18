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

**Goldens.** Positive `pb3370_computer_paragraph_second_period` (85 up); negatives
`pb3370-source-computer-reserved-word-clause`, `pb3370-object-computer-reserved-word-clause`,
`pb3370-source-computer-period-missing`, `pb3370-object-computer-period-missing` (every edition). Unit cases in
`ClosedFormatReservedWordTests`; `ParagraphPeriodDriftTests` asserts the two rules. No inventory row moved (all four
rules were CONFORMS; the source was always refused). A literal after the computer-name (`XYZ "LIT".`) is still
misattributed, because `genericClause` opens only at an IDENTIFIER. It is reported as a lead and not widened here.
