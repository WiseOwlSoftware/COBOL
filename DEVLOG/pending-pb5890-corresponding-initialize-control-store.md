## PB5890: a USE BEFORE REPORTING procedure may MOVE, ADD or SUBTRACT CORRESPONDING, or INITIALIZE ... REPLACING, into a group whose control data item they do not store

Wave 20262099 group A: the store answer kb/Work PB5885 made the one answer to "what does this statement store into" answered
for the operand a CORRESPONDING statement or an INITIALIZE names, not for the items it stores.

**PB5890.** With CTL, the control data item of R1, the first (alphanumeric) item of GRP, a USE BEFORE REPORTING procedure's
`MOVE CORRESPONDING SRC TO GRP`, `ADD CORRESPONDING SRC TO GRP`, `SUBTRACT CORRESPONDING SRC FROM GRP` and
`INITIALIZE GRP REPLACING NUMERIC DATA BY 7` (and `BY 0`) were refused COBOL2378 at --std 85 and 2023 (re-probed: the refuter's
`rcorr.cob`, exit 65, five diagnostics), though none stores CTL: a CORRESPONDING statement stores the receiving item of each
corresponding pair (§1985:VI.6.19.4 GR1, §2023:14.9.25.4 GR11) and SRC has no CTL; INITIALIZE ... REPLACING stores an item only
of the category it names (§1985:VI.6.17.4 GR2, §2023:14.9.20.4 GR5c2). `BoundStores.StoreKindVisitor` answered both through
the operand as written (`BoundCorresponding.ReceivingGroup`, `BoundInitialize.Written`), which the debug triggers (kb/Work
PB4810) need, and `ReportWriterBinder.SameStorageLine` matched the group holding CTL. The visitor now takes a `StoreView`:
`Operands` (the property-temp query, whose temp is the written operand, and the debug query: X3.23-1985 USE FOR DEBUGGING
rule 9, a group reference does not reference its subordinates) asks the receiving group and identifier-1; `Storage` (the
control-data query, `StoresInto`) asks the pair receivers and the elementary stores of INITIALIZE's expansion, after its
category selection, exclusions (§1985:VI.6.17.4 GR3, GR4) and default (GR5). Every other arm stores what it names and both
views agree on it. After: `rcorr.cob` exit 0 at 85 and 2023, printing `USE AA 0000 QQQ`, `USE BB 0000 QQQ`, `END 2 BB`.
Goldens `14.9.49-USE/pb5890_corresponding_initialize_keep_control` (2014, 2023) and
`XIII.4.9@1985-.../pb5890_corresponding_initialize_keep_control_85`; 18 new `DeclarativesStructuralRulesTests` cases at 85
and 2023 (the eight acceptances are red with the visitor at main; the refusals that stay: a pair at the control item, a plain
INITIALIZE, REPLACING ALPHANUMERIC, a MOVE to the group). SR-14.9.49.3-11 stays CONFORMS with its notes restated and three
witnesses; 1985-SR-XIII.4.9.3-4 gained its 85 witness. No 2002 verdict (frozen).

No NIST program uses USE BEFORE REPORTING, and none names a CORRESPONDING or INITIALIZE operand in a USE FOR DEBUGGING program.
Gate: run by the dispatching session.
