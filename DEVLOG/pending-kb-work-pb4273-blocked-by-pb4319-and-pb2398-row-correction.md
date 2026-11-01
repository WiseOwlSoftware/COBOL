## kb/Work: PB4273 waits for PB4319, and PB2398 gives up the Frontend.Parsing row to PB2347

PB4273 (the model-assembly split) now lists PB4319 in `blocked_by`. The split cannot compile the three emitters while the tolerated `CodeGen → Binding.Repository` row stands, and PB4319 moves the repository records to `Binding.Model` and removes that row. PB4319 is blocked only by PB2417 and PB2104, so the edge adds no cycle. The lead came from PB2417's finisher.

PB2398's Removes line names `Binding.Bound → Frontend.Parsing` "with PB2347", but `BoundOo.cs` has no use that reaches that namespace. The row's one use is `StatementBinder.cs:696`, which PB2347 owns, and `architecture-edges.json` already names PB2347 alone as remover. PB2398 now carries a correction paragraph telling its implementer to drop the pair from its Removes line in the targets record. No evidence or edges file changed.
