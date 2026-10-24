## PB1014 and PB3246: a BY CONTENT boolean argument is stored by the MOVE's store, and an activation's tail picks up before it terminates a SORT

Wave 20261027, group B (both notes name `OoEmitter.cs` and the `CallEmitter.cs` tail it shares).

**PB1014.** A BY CONTENT boolean literal or boolean expression argument was written into a known formal by a second receiving
store, `CallEmitter.BooleanArgumentRecord`, shared by the INVOKE lane and the CALL lane's known-formal arm. Re-probed on this
branch before the change (`p14/q.cob`): `INVOKE O "ME" USING BY CONTENT B"10"` and `B1 B-AND B2` into `PIC XBX` printed `[10 ]`,
the same into `PIC NBN` printed `[10 ]`, and `CALL ... AS NESTED USING BY CONTENT B1 B-AND B2` into `PIC XBX` printed `[10 ]`, where
`MOVE B"10"` into the same pictures gives `[1 0]` (§14.8.2.3.3 2) d): the crossing is a MOVE with the argument as the sending
operand; §14.9.25.4 6) applies the receiver's editing). The hand store skipped the editing and treated an as-if-elementary bit
group as a plain group. Fix: the boolean value is the MOVE's sender. `BoundBoolOperand` gains `AtItemWidth` (the value at
§8.8.2 rule 10's width; `OperandText.AsString` renders it with `BooleanRenderer.RenderAtItemWidth`), and `BooleanArgumentRecord`
keeps only the two arms that are not an elementary MOVE (an ANY LENGTH formal, tested first as before, and a plain group formal)
and sends every other formal through `MoveEmitter.RecordValue`. A numeric formal cannot reach it (COBOLNET0828 at bind) and the arm
throws if one ever does. Golden `pb1014_content_boolean_move_store` (both lanes, literal and expression senders, `XBX`, `NBN`,
JUSTIFIED RIGHT `X(4)`, `1(4)`, `1(2)`, `X ANY LENGTH`, `1 ANY LENGTH`; accept 2002) and the negative
`pb1014-content-boolean-into-numeric-formal`. Sibling sweep: `StrStoreBoolean` has no other hand caller outside the MOVE store,
the COMPUTE format 2 store (a boolean receiver only) and the data-division initializers; the CALL lane's no-formal arm already
reaches the MOVE store through the activated unit's `MoveLanding`.

**PB3246.** Re-probed on both arms (`w3246.cob` CALL, `i3246.cob` INVOKE): a `GOBACK ... RAISING` staged by a declarative that also
ended the activator's SORT (§14.9.40.4 GR17) was never picked up at its activation; it was raised at the activator's next,
unrelated activation (`A caught EC-SIZE-OVERFLOW` printed after `next CALL` / `next INVOKE`). Root cause: the GR17 question
(`SortFileTable.AfterActivationReturn`, which throws `SortTerminationSignal` and so unwinds the rest of the range) was asked before
the pickup on both lanes: inside `ProgramTable.Activate` for a CALL, and in `OoEmitter.EmitInvocationReturned` for an INVOKE. Fix:
one activation tail, `CallEmitter.EmitActivationReturned`, emits the pickup and then the GR17 question (§14.9.18.4 GR1 b): the
condition is raised "after the result ... is returned"; §14.9.40.4 GR17 terminates the SORT after that); `EmitCall` and every INVOKE
form end in it, `OoEmitter.EmitInvocationReturned` (a one-line forwarder) is deleted, `EmitPropagationPickup` is private, and
`ProgramTable.Activate` no longer asks (`CallFromHost`, which emits no pickup, does). Every CALL's emitted C# gains the one
`CobolSort.AfterActivationReturn()` statement the runtime used to run itself. Golden `pb3246_sort_terminated_after_raising_pickup`
(a CALL and an INVOKE arm; accept 2002). All 54 conformance cases that mention RAISING, or a SORT with a CALL, an INVOKE or a
declarative, still produce their `.out` (including the six `pb1177_*` cases). The design doc's pickup/GR17 paragraphs and
CONFORMANCE.md's GR17 determination are current.

**Register.** PB1014 and PB3246 landed, neither claimed an inventory row; the three rows whose witnesses moved
(`GR-14.9.40.4-17`, `GR-14.9.18.4-1`, `GR-14.8.2.3.3-L2.2`) are re-sited by a `record_verdicts` batch (retired:
`OoEmitter.cs#EmitInvocationReturned`, `ProgramTable.cs#CallProgram` for GR17); GAP 63 to 63.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `000002-20261024T051855Z`; the first run, `000001-20261024T051528Z`, was RED on the citations audit - the PB1014 note misattributed a "GR4" to the preceding clause - and was fixed before the build). Leg 1 ran the four added goldens/negatives (`pb1014_content_boolean_move_store` and `pb3246_sort_terminated_after_raising_pickup` at 2002, `pb1014-content-boolean-into-numeric-formal` at 2002, 2014, 2023: all Passed in `leg-1-Conformance.trx`). The Linux gate is the lander's (batched gating). Self-review (pr-test-analyzer, silent-failure-hunter, comment-analyzer): the one new throw is loud and unreachable by bind; no catch, fallback or default value was added; the stale comments naming the deleted `EmitInvocationReturned` and the CALL-side `ProgramTable.CallProgram` question were rewritten.

**Branch review round 1 (R1-1, blocking, answered: fixed).** The reviewer's probe: the activator's own USE procedure, run by the
pickup, CALLs a program; that nested return asked `AfterActivationReturn` and consumed the OUTER activation's pending GR17
request (the run unit holds one slot), terminating the SORT mid-declarative and skipping its RESUME. Root fix: the request
belongs to the activation that left it. `CallEmitter.EmitActivationReturned` now TAKES it out of the slot straight after the
return (`CobolSort.TakeActivationReturnTermination`, into a local) and acts on it after the pickup
(`CobolSort.TerminateAfterActivation`); a site with no pickup (`EcState.Active` false) keeps the one-call
`AfterActivationReturn`, which is now `TerminateAfterActivation(Take...())`, as does the host boundary. The golden
`pb3246_sort_terminated_after_raising_pickup` gains a `CALL` and an `INVOKE` inside A's USE procedure (expected from GR17: the
nested activations terminate nothing; the declarative runs to its end, then the SORT, terminated by B's/MB's return, ends);
the reviewer's probes now agree. Determination recorded in the design doc: a RESUME AT procedure-name taken by the pickup's
declarative leaves the statement before the termination is acted on, so that transfer supersedes the request (GR1 b) puts the
raise, and so the declarative's choice of where control goes, first).
