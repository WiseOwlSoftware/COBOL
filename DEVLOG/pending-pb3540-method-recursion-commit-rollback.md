## COMMIT, ROLLBACK and APPLY COMMIT now see a running method as a recursive runtime element (PB3540)

**What was wrong.** ISO §8.6.6 makes every method recursive ("Functions and methods are always recursive."), and §14.9.7.4 GR2,
§14.9.36.4 GR2, §12.4.6.3.4 GR3 and §9.1.18.7 raise EC-FLOW-COMMIT, EC-FLOW-ROLLBACK and EC-FLOW-APPLY-COMMIT for a statement or
an activation "under the control of a recursive runtime element". The check read `ProgramTable.AnyRecursiveActive`, which scans
the programs and functions of the run unit, and a method is no node there. Re-probed on this wave's build: a program with an
active APPLY COMMIT clause INVOKEs an object method that CALLs a program executing COMMIT and ROLLBACK, checking on. No condition
was raised, the COMMIT committed record 1 and the program's own ROLLBACK then restored nothing (`RECORD 1=AAAA`; the standard
requires `RECORD 1 ABSENT`).

**The fix and its shape.** A method is a frame on the run unit's `ModuleStack`: the emitted method body pushes it (the §15.65.4
r5 activation, `OoEmitter.EmitMethod`), as `ProgramTable` does for a program or function. The recursion fact now lives on that
frame. `ModuleStack.Push` and `PushMain` take `recursive` (a RECURSIVE program, a function, a method), `ModuleStack.AnyRecursive`
answers in constant time (a count kept with the frames), and `TransactionControl.FlowForbidden` asks it. The scan in `ProgramTable`
is deleted, with no wrapper left behind; `TransactionControl` takes the module stack in its constructor in place of the table.
COMMIT, ROLLBACK, an APPLY COMMIT activation and §14.6.13.1.3 item 8's unconditional ROLLBACK share `FlowForbidden`, so one change
reaches the four arms; object, factory and property methods all pass through the one `EmitMethod`. The shipped runtime surface
changed (two signatures and one property), so the runtime major is 14 and `RuntimeAbiPinDriftTests` pins the new hash.

**The static reach.** A COMMIT or ROLLBACK written inside a method was already refused (COBOLNET1690, §14.9.7.3 SR1 and §14.9.36.3
SR1: "This statement shall not be specified in a recursive source element."), but no golden pinned it. Two refusals now do, one
for an object method and one for a factory method.

**Goldens.** `pb3540_commit_rollback_under_method` (checking on: both conditions, then `RECORD 1 ABSENT`),
`pb3540_commit_under_factory_method_unchecked` (a factory method, checking off, the COMMIT commits nothing),
`pb3540_ec_flow_apply_commit_under_method` (the activation, checking on and off at the CALL), `pb3540_commit_in_object_method` and
`pb3540_rollback_in_factory_method` (COBOLNET1690) and `pb3540_commit_under_method_below_2023` (the clause is refused at 2002 and
2014, COBOLNET1970). Unit: `ModuleStackInvariantTests.AnyRecursive_HoldsFromTheRecursiveFramesPushToItsPop` and, in
`TransactionControlTests`, COMMIT, ROLLBACK, the activation and item 8 under a method, with the negative control that a program
with no RECURSIVE attribute under no recursive element commits.

**Rows.** GR-12.4.6.3.4-3, GR-14.9.7.4-2, GR-14.9.36.4-2, SR-9.1.18.6-p1, GR-9.1.18.7-p1 and GR-9.1.18.7-p2 moved from PARTIAL to
CONFORMS (batch `w1157b-pb3540`, GAP 74 to 68); each retires the `AnyRecursiveActive` witness it cited. docs/CONFORMANCE.md A.4.3
states the one rule and drops the known gap.

**Self-review.** Citations were re-run through `cite.py --check`; one of the two refusal goldens first quoted §14.9.7.3 SR1 from
memory ("recursive program, function or method") and the check caught it (the text is "recursive source element"). No other
reader of the old table query existed. An unbalanced `Pop` stays a no-op on an empty stack and the count cannot go negative.

**Gate.** GATE_VERDICT_LINE
