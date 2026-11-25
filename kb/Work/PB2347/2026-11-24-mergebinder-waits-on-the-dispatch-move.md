## MergeBinder waits on this extract (kb/Work PB2341, wave 20262168 group H)

PB2341 (Extract: SortBinder; targets `SortBinder`, `MergeBinder`) landed the half that needed no new dispatch edge:
`SortMergePhraseBinder` (`src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortMergePhraseBinder.cs`) now binds the
phrases and file screens SORT and MERGE share, once, and `SortBinder` binds the four sort-family statements.

`MergeBinder` as its own statement binder is left for after this note, by decision: the statement dispatch is
`StatementBinder.BindStatement` (`src/WiseOwl.Cobol.Compiler/Binding/Bound/StatementBinder.cs`, `Sort.BindMerge(mrg)`), in
the `compiler.bound` layer, and the tolerated row `WiseOwl.Cobol.Binding.Bound -> WiseOwl.Cobol.Binding.Procedure` of
`docs/rearchitecture/architecture-edges.json` may not grow a pair (DESIGN-architecture-review §8.1), so `StatementBinder`
may not name a new `MergeBinder`; a `SortBinder.BindMerge` that only called `MergeBinder.Bind` would be a forwarder
(CLAUDE.md rule 4). Once this extract moves the dispatch into `Binding/Procedure` (same layer, free edges), the MERGE
statement (`SortBinder.BindMerge`, `SortBinder.BindMergeOf`, about 110 lines) moves to `MergeBinder` with the dispatch
arm naming it directly.

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortBinder.cs` (`SortBinder.BindMerge`, `SortBinder.BindMergeOf`),
`src/WiseOwl.Cobol.Compiler/Binding/Bound/StatementBinder.cs` (`StatementBinder.BindStatement`, the `mergeStatement` arm).
