**2026-11-16, landed (wave 20262068, group A).** Re-probe on the branch build at `--std 85`: `m-send-two.cob` `CORR 22`,
`m-recv-two.cob` `[22111111]`, `m-two-sub-rr.cob` `CORR 23A`, as the note says. Fix (the note's first direction):
`CorrespondingBinder.Bind` marks the statement's pre-op queue before it resolves the two group operands, and for a
COBOL-85 MOVE takes the identification steps they registered off the queue (`host.Udf.OperandSteps`, the one stamp) into
`BoundCorresponding.PairIdentification`; `CorrespondingEmitter.Emit` runs them before EACH pair's MOVE, so every pair
evaluates the subscripts immediately before its move (§1985:VI.6.19.4 GR2). After: 29, `[21111211]`, `21B` at 85
(the separate MOVEs in declaration order); 2002 and 2023 unchanged (22, `[22111111]`, `23A`), the freeze those editions
require. The sweep the note asked for: ADD and SUBTRACT share `CorrAccess`, but kb/Work PB5814 (landed in the same change)
makes them identify once at every edition, so a per-pair identification there would now be wrong, and they take none.
Witness `conformance:cases/VI.6.19@1985-MOVE-STATEMENT/pb5767_move_corresponding_redefined_subscript_per_pair_85` (both the
sending and the receiving redefined-subscript shapes). Row 1985-GR-VI.6.19.4-1 PARTIAL to CONFORMS (1985 GAP 35 to 34).
