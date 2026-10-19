## Entry 2230 — 2026-10-18 17:06 PDT — kb/Work PB3373: a literal after the computer-name is refused in its own format; the closed formats' error production opens at a literal

**The defect.** ISO §12.3.5.2 prints `SOURCE-COMPUTER. [ computer-name-1 ] .` and §12.3.6.2 the OBJECT-COMPUTER clause
set; a literal stands in neither. Re-probed on the worktree build, `SOURCE-COMPUTER. XYZ "LIT".` drew COBOLNET1971 'XYZ'
is not a paragraph of the CONFIGURATION SECTION (and COBOLNET3734 at 85) and `OBJECT-COMPUTER. XYZ "LIT".` COBOLNET1970
'XYZ' is not a clause (and a false COBOLNET0900 at 85): `genericClause`, the one error production's word run, opened
only at an IDENTIFIER, so the name-less reading (§12.3.5.3 SR1 lets the second period go) was the only viable parse.
The sibling sweep over the ten closed formats found the same root elsewhere: a literal at a residue position drew
COBOL0308 "A data-name is expected here, not a literal" (the IDENTIFIER in the expected set was the error production's
own left edge), a bare COBOL0001 or a missing-period guess, and a figurative constant after the name
(`OBJECT-COMPUTER. XYZ ZERO.`) misattributed XYZ through the run's `literal` tail.

**The fix.** `genericClause : residueOpening residueItem*`. The opening is a word, a `residueLiteral` (every one-token
literal except the unsigned integer) or a sign-led numeric literal; an item is a word, any literal but a figurative
constant and NULL (`signedNumericLiteral` holds the numeric shapes), or a concatenation's `& operand`. With the literal
admitted the name's reading is viable too and the ambiguity resolves to it. Each token set was measured: an INTEGERLIT
opening would read the next data description entry's level-number (§8.3.2.2.13) as residue after a forgotten period;
an INTEGERLIT-free tail turned `SEGMENT-LIMIT IS? integer`'s text predicate into a NoViableAlt at the decision's first
token (`OBJECT-COMPUTER. XYZ FOO 12.`); a `literal`-rule tail swallowed a figurative after the name. `ClosedFormatPass`
now quotes the run's opening as written, word or whole literal. `CobolErrorStrategy`'s 0g arm and its Sync take a literal token as they
take a reserved word (`OBJECT-COMPUTER. 12.` is named in its format), `IsLiteral` is one set read from the ATN
(`residueLiteral` plus INTEGERLIT; it had been three tokens), and hint 15 yields to a structural diagnosis.
`VersionConformancePass` no longer counts an error-production clause as an OBJECT-COMPUTER clause, so the false
COBOLNET0900 at 85 is gone. No new diagnostic code (COBOLNET4484-4486 returned).

**Tests.** Negatives at every edition `pb3373-source-computer-literal-clause` and `pb3373-object-computer-literal-clause`
(COBOLNET1970 naming "LIT"); positive at 85 `pb3373_literal_lists_beside_the_residue` (the ALPHABET, CLASS and level-88
literal lists beside the widened residue keep their clause, and the collating sequence still takes effect). Unit:
`ClosedFormatReservedWordTests` gains a literal theory over every format and literal class, an 85 theory, a level-number
control and two figurative cases; `ClosedFormatDriftTests` derives `residueLiteral` from the generated parser.
`ConstantNameLiteralPositionDriftTests` argues `residueLiteral` as the error production's.

**Branch review, round 1 (finding R1-1).** The first shape's tail was one-token literals and INTEGERLIT, so a run
holding a sign-led numeric literal (`-1` is MINUS INTEGERLIT outside the list regions, §8.3.3.3.2 2)), a concatenation
(§8.8.3.3 3)) or a DECIMAL-POINT IS COMMA literal ended at the sign, `&` or comma with a bare parse error and a false
missing-period hint, a regression from main's by-name refusal (`01 A PIC X FOO -1.`, `FOO "A" & "B"`;
`OBJECT-COMPUTER. XYZ -1.` drew a bare COBOL0001). The review finisher restored them: the tail is `literal` less its
reserved words, the `&` continues the run, and the opening takes a sign-led literal, so no FIRST set of the run holds a
reserved word and the 0g arm is untouched. `LiteralScreen` screens the opening's sign as it screens
`signedNumericLiteral`'s (COBOLNET2155 for `- 1`). New negatives at every edition:
`pb3373-source-computer-signed-literal-clause` (COBOLNET1970 '-1') and `pb3373-data-entry-signed-and-concatenated-run`
(COBOLNET1941 'FOO'); twelve more unit cases. `UserWordSlotDriftTests` and `ConstantNameLiteralPositionDriftTests`
argue the new rules as the error production's. The bare leading-comma decimal `,5` does not open the run (its one
spelling is numericLiteralCore's predicated alternative), so `OBJECT-COMPUTER. XYZ ,5.` still quotes XYZ, as main
does; reported as a lead.

**Not closed.** The unsigned integer's right reading depends on the format (a level-number follows only the data
division's lists), which one production cannot express: `SOURCE-COMPUTER. XYZ 12.` and `OBJECT-COMPUTER. XYZ 12.`
still read XYZ as the residue, and `01 X PIC X FOO` / `05 Y PIC X.` still reads `FOO 05 Y` as one run. Reported as a
lead. No inventory row moved (FMT-12.3.5.2, FMT-12.3.6.2 and FMT-12.3.7.2 were already CONFORMS).

