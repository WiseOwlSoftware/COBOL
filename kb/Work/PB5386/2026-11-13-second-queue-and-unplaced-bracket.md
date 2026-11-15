**2026-11-13, lead filed by the notes batch 20261113e.** UNVERIFIED LEAD: re-probe before fixing.

Source: Group B, leads 7 and 8, `E:/COBOL-coord/scratch/op-20261113/leads-final-batch.md`. Verbatim: "Lead 7: an object-property reference used as a receiver's subscript keeps its GET in the pre-statement hoist (ProcedureBindState.PropertyOps queue); PB5386 moved only the pre-op subscripts to the store. Repro: `MOVE 2 TO I, WE (N OF OBJ)`. Same root as PB5386, second queue.

Lead 8: READ INTO with no record area would register an identification bracket that is never placed, which fails the compile loudly: SequentialIoBinder.cs:269, KeyedIoBinder.cs:54. Shares the PB5386 mechanism."
