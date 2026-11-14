## PB5385: a debugging section describes the receiver its statement identified at the store

**The defect.** kb/Work PB5294 made each data fire of a USE FOR DEBUGGING section identify its place once, when the statement begins
(`DebugTriggerEmitter.IdentifiedOnce`, §1985:IV.4.3.8). That rule holds "Unless otherwise specified by the rules for a statement", and
MOVE (§1985:VI.6.19.4 GR2), the arithmetic statements with several results (§1985:VI.6.4.6 GR2), DIVIDE's REMAINDER
(§1985:VI.6.11.4 GR4), SET (§1985:VI.6.23.4 GR3d) and READ or RETURN INTO (§1985:VII.4.4.4 GR8b) specify otherwise for their
receivers. The statement's own store honoured that; the debug register was filled from the frozen place. Re-probed on this tree:
`MOVE 3 TO SUB TE (SUB)` printed `D1 +0001 .` where it stores TE (3); `DIVIDE 7 BY 2 GIVING SUB REMAINDER NT (SUB)` and
`ADD 1 TO SUB NT (SUB)` reported +0001 for stores into NT (3) and NT (2); the sweep found COMPUTE, SET and READ INTO the same.

**The fix.** One identification shared by the store and the section. The freeze still runs at the statement's start (one freeze per
reference, recording the locals it hoisted into), and a verb whose rules identify a receiver at its store renders that store through
`DebugTriggerEmitter.IdentifiedAtStore`, which re-renders the receiver with each run-time fragment as `(local = fragment)`
(`PlaceIdentification.IdentifyInto`). The store evaluates the subscript once, exactly where it did before (after the sending operand),
and the section reads the value it left. `MoveEmitter.EmitReceiver`, `ArithmeticEmitter.ReceiverStore` (every arithmetic receiver,
the remainder included) and `SetEmitter.EmitSetTo` ask it; READ and RETURN INTO reach it through the MOVE emitter. A path that
never reached the store keeps the statement-start identification.

**A sibling in the same file.** A quotient size error leaves the remainder unchanged (§1985:VI.6.11.4 GR8a), yet NT's section for the
item alone still ran after `DIVIDE 50 BY 2 GIVING Q REMAINDER NT (SUB) ON SIZE ERROR ...`: the skipped remainder arm never cleared
its "stored" flag. `EmitDivideRemainder` now clears it there (§1985:XV.3.2.4 GR6c).

**Witnesses.** The golden `pb5385-debug-receiver-identified-at-store` (expected values derived from the cited rules: legs for MOVE
in both orders, ADD, COMPUTE, DIVIDE REMAINDER, SET, the skipped remainder, READ INTO and an ALL REFERENCES section after a zero
divisor) and its refusal `pb5385-debug-receiver-at-store-removed-2002` (COBOL0902 from 2002 on). The 1985 rows 1985-GR-XV.3.2.4-14
and -24 are re-verdicted PARTIAL -> CONFORMS (GAP 249 -> 247 in the 1985 store). docs/CONTROL_FLOW_DESIGN.md D20 states the rule.
