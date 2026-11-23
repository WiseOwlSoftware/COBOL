## Sequenced after PB2327 (wave 20262147, group A)

Not started by wave 20262147 group A. The note's own target and DESIGN-architecture-review §8.3 make this "a separate data-ize
wave after PB2327's CobolEdit/FloatEdit extract", and PB2327 is still `open`: running it first would change every edited
store's emitted call twice (once here, once when the extract moves the entries), where the design asks for one explained
re-baseline. `blocked_by` now names PB2327 so the planner holds it.

What the next implementer will find (read on the branch at bcb9ff169d): the runtime entries `CobolEdit.Format`, `TryFormat`,
`DeEdit`, the float family (`FormatFloatStore`/`U`, `TryFormatFloat`/`U`, `TruncateToFloatShape`, `DeEditFloat`, each re-running
`FloatMask.Parse`) and `FormatSimpleInsertion` all take the expanded mask string plus the per-item configuration
(`EmitContext.EditCfg`: `currencyString:`, `commaMode:`, `edits:`), and `RuntimeApi.EditsArg` emits
`new CobolEdit.EditRule[] { … }` inline, a second per-call allocation the note does not count. The hoisting shape exists:
`CodeGen/GroupAtomTable.cs` states each distinct compile-time value once as a `static readonly` field of a module class
(kb/Work PB2690), and an `EditMask` table can follow it. Call sites: about 40 `CobolEdit.Format(` and 14 `DeEdit(` in tests.
