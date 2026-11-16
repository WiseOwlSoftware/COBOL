**2026-11-16, landed (wave 20262068, group A).** Re-probe on the branch build: `nn1.cob` at `--std 85` was refused
(COBOL0001 near NOT), as the note says. Fix at the one site the note named: `CobolParserCoreBase.logicalNotBeforeRelationalOperator`
no longer excludes a NOT followed by NOT, so `NOT NOT < C` parses as the logical NOT (GRL2.2) of the abbreviated relation
`NOT < C` (GRL2.1 read against the word after the second NOT). The grammar is the superset and the form keeps its 2002
refusal through its construct gate: at `--std 2002` and `--std 2023` nn1.cob now draws COBOL0902 (abbreviated logical NOT
removed in 2002) on both forms, where the first drew COBOL0001. `NOT NOT C` (two logical operators, §1985:VI.6.3.2.3) still
parses as nothing. After: nn1.cob prints `T1`, `T2` at 85. Witness
`conformance:cases/VI.6.3@1985-CONDITIONAL-EXPRESSIONS/pb5784_not_not_abbreviated_relation_85` (T1, T2, F3); rows
1985-GR-VI.6.3.3-L2.1 and -L2.2 gained it.
