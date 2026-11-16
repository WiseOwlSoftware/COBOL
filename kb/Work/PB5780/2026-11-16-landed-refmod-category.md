**2026-11-16, landed (wave 20262068, group A).** Re-probe on the branch build: `q1.cob` at `--std 85` printed `RM=5 YES`,
`RM2=5 YES` as the note says. Fix at the one site the note named: `OperandText.IsStringVisitor.Visit(BoundFieldOperand)`
now reads `RefModPlace.Category` (the one reader of a reference-modified operand's category, kb/Work PB70) for a
reference-modified operand, so the relation takes the nonnumeric arm. After: `q1.cob` prints `RM=5 NO`, `RM2=5 NO`, the
rest unchanged, at 85 and 2023; `refcmp.cob`'s `IF N5 (1:3) > 5` is FALSE; EVALUATE N3 (1:3) WHEN 5 takes OTHER.
Sibling sweep (`grep -rn "Item.Pic?.Category" src/WiseOwl.Cobol.Compiler/CodeGen/`): `OperandText.SendingVerbatim`
(the sending image, not a class decision), `MoveEmitter.IsNumericOperand` (`MOVE N5 (2:2) TO E3` with E3 `PIC ZZ9` gives
` 23`, the same as the alphanumeric-sender edit, no observable defect found), `CallEmitter.CallPlaceIsString` and
`OoEmitter` (carriers, not comparisons; not probed further). Witnesses
`conformance:cases/IV.4.3.8.4@1985-Reference-Modification/pb5780_refmod_numeric_compares_as_characters_85` and its 2023
twin `conformance:cases/8.4.3.3-Reference-modification/pb5780_refmod_numeric_compares_as_characters` (2002, 2014, 2023).
Rows: 1985-GR-IV.4.3.8.4.4-5 and -2 and the 2023 rows GR-8.4.3.3.4-6 and -2 gained the witnesses (1985 GAP 36 to 35).
