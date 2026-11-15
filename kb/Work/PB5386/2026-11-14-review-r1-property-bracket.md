**Review round 1, R1-1 fixed (2026-11-14, review finisher w20262030b).** A receiving object property whose reference modifier carries a
function got two brackets, in two `BoundReceiverBrackets` nodes (OoBinder's property bracket, StatementBinder's identification bracket), and
`ReceiverBracketEmitter.Find` returned only the first match, so the property bracket was never placed and the emitter threw its internal
error on `MOVE "QQ" TO NM OF AR(1) (FUNCTION INTEGER(K):2)`, which main compiled. `Find` now returns every matching bracket across the scopes
in force, the property's outermost (its identified object is fetched before the modifier is evaluated); `Receive` opens them in that order,
closes them in reverse, and `Identify` / `Settle` / `Settles` walk the list. Golden `14.9.25-MOVE/pb5386_move_property_refmod_function`
(`[AQQDEF]`, 2002 and later). **Sites:** `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReceiverBracketEmitter.cs` (`Find`, `Receive`, `Identify`, `Settle`, `Unsettled`).
