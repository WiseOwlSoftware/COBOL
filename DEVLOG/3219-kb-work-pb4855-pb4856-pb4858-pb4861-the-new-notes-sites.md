## Entry 3219 — 2026-11-06 15:40 PST — kb/Work PB4855, PB4856, PB4858, PB4861: the new notes' sites corrected against main

The clerk's notes were written while PR 1246 was still in the queue, so three of them said `BoundReferences`, `PhraseOperands` and `DebugRegisters` were not yet on main; the pull request has merged and the sentences are gone.

The enqueue unit checked each note's named members against the files on main. PB4858 named `Binding/DebugRegisters.cs`, which does not exist: the member is `ImplicitRegisters.DebugRegisters`. `BoundTableSortKey` and `BoundSort` are in `BoundSort.cs`, `BoundEvaluateWhen` and `BoundEvaluate.PhraseOperands` in `BoundEvaluate.cs`, and `CobolIntrinsics.MedianWhole` in `CobolIntrinsics.ExactValue.cs`.

