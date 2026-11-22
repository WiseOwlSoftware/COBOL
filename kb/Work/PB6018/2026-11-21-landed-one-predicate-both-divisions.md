## Landed (wave 20262131 group p): one predicate for both divisions

Re-probed on the branch's base build (633db13477, group n's head), `run_probe.py --compile-only`, programs in
`E:/COBOL-coord/scratch/w20262131p/src/`, before/after in `E:/COBOL-coord/scratch/w20262131p/before.txt` and `after4.txt`.
At --std 85 each shape below is now ONE `COBOL0307: cannot parse construct near '<header>'. A period may be missing at the end
of the previous sentence.` at the header:

| Probe | Shape | Before (85) |
|---|---|---|
| NOPERP | `MOVE "B" TO A` then `P1.` | COBOL3735 + COBOL1639 'P1' is not defined |
| NPDISP | `DISPLAY A` then `P1.` | COBOL3735 + COBOL1639 |
| NPIF | `IF A = "A" MOVE "B" TO A` then `P1.` (a sentence ending inside a nested statement) | COBOL3735 + COBOL1639 |
| NPSECT | `MOVE "B" TO A` then `S1 SECTION.` | COBOL0307 at SECTION, S1 read as a receiver |
| NPNUM | `DISPLAY "X"` then `100.` | COBOL3735 alone (100 read as a DISPLAY operand) |
| NPGOTO | `GO TO P2` then `P1.` | COBOL2172 (a Format 2 GO TO lacking DEPENDING) at the period |
| NPPERF | `PERFORM P2` then `P1.` | COBOL5506 "expected ... TIMES" at the period |

At --std 2023 every probe's diagnostics are unchanged (no areas, §2023:6.3.1).

The mechanism: `CobolParserCoreBase.nextEntryOpensHere` has two arms chosen by the enclosing rule: in a `sentence`, a token that
begins its line in area A and is followed by the period or SECTION (§1985:IV.7.3.3, §1985:IV.7.3.2, §1985:IV.7.2.2); in the
`dataDivision`, an integer (PB6011). The generated `cobolWord` carries it on every alternative (`scripts/gen-cobol-words.ps1`),
`procedureName`, `enterOperand` and `routineName`'s gated arm by hand; `AreaAOpeningDriftTests` scrapes every operand-led
alternative. The recovery reads the same fact (`OffendingPosition.OpensTheNextEntry`). A found sibling, fixed here: a guard
alone in a rule entered for a REQUIRED operand (`PERFORM` then `P1.`, `GO TO` then `100.`) threw a FailedPredicateException that
reached the user as the raw `COBOL0001: rule procedureName failed predicate`; `CobolErrorStrategy.ReportFailedPredicate` reports
the guard's refusal as the token's mismatch. The same path served PB6011's `integerLiteral` (`05 E PIC X OCCURS` then `01 B`:
now one COBOL0307 at the 01).

Decision recorded in DESIGN-frontend-grammar.md: an operand written in area A and followed by a period (`MOVE "B" TO` then
`A.`) takes the reference format's reading, a header, and the report is the missing period at it; an area-A operand that
writes no header keeps COBOL3735.
