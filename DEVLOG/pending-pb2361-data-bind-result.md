## PB2361: the backend reads the frozen data binding, never the binder

The backend input carried the data binder. `BoundUnit.Data`, `OoClassUnit.Data` and `FactoryData`,
`BoundCompilation.InterfaceData` and `EmitContext.Data` held a `DataBinder`, and the bound units also held the
`ReferenceResolver`. An emitter could reach every table, writer and fact set of the bind through them. A second
backend would have needed the binder to emit anything (kb/Work PB2293, DESIGN-backend-abstraction §1.3 L6).

**Re-probe.** This is a behavior-neutral extract, so there was nothing to reproduce. On the carried head `ca1eed46c`, the
edges walk measured `CodeGen → Binding` at 143 pairs: 19 onto `DataBinder`, 15 onto `DataBindState`, 11 onto
`ConfigurationSlice` and 4 onto `LinkageBinder`. It measured `CodeGen.Emit → Binding` at 37 pairs. In the
`Binding.Model → Binding` row, `BoundUnit`, `OoClassUnit` and `BoundCompilation` named `DataBinder`, and the two units
named `ReferenceResolver`.

**Fix shape** (DESIGN-architecture-review §8.3 (1), step 11; the "as built" paragraph there):
- `DataBindResult` (`Binding/Model/DataBindResult.cs`) is what the backend reads. `DataBinder.Result` makes one per
  element. `ElementBinders.Freeze` calls it after the group tail and the validation passes, so the procedure binders'
  two late doors are already shut. The freeze refuses any binder whose watermark is not the tail's terminal phase.
- The result holds the frozen `ForestSlice`, `FileSlice` and `StorageTierSlice`, which moved to `Binding/Model` with it.
  It also holds `ReportScope`, `UnitChecking`, `ConfigurationFacts` (from `ConfigurationSlice.Facts`), `LinkageFacts`,
  the range collations, APPLY COMMIT, the three edition facts, and the answers the emitters used to ask the binder:
  `EmitsStaticReset`, `DeclaredCapacityRegisterTables` and the cell lifetimes.
- The binders and resolvers stay on the binder's side, in `ElementBinders` on `BindSession.Binders` (program units,
  class halves and interfaces). `BoundUnit.Refs`, `OoClassUnit.Refs` and `FactoryRefs` are deleted. Every binder-side
  reader now asks the table: `InheritStep`, the group passes, the repository provider and codec, the validation passes,
  `OoDriver`, and `BinderDriver`'s signature tables through `ElementBinders.SignatureOf`. `BinderDriver.Session` gives
  the session to a binder-side reader (a test of a binder rule) after the bind.
- `BoundUnit.AnchorOf` moved to `StorageTierSlice.AnchorOf`, and `DataBinder.IndexDeclarationsUnder` to
  `IndexDeclaration.Under`. Every caller changed in the same commit, and neither old member is left as a forwarder.

**Measured.** The behavior oracle against the carried head reports
`=== ARCH-ORACLE: IDENTICAL — 16536 cases (baseline ca1eed46c253, new 2a581b75c606-dirty) ===`. The edges walk
changed as follows:

| Row | Before (pairs) | After (pairs) |
|---|---|---|
| `CodeGen → Binding` | 143 | 72 |
| `CodeGen.Emit → Binding` | 37 | 22 |
| `Binding.Bound → Binding` | 48 | 46 |
| `Binding.Model → Binding` | 21 | 21 |

The `Binding.Model → Binding` row lost eight binder pairs and gained eight pairs onto value types that the result's
parts name. Those eight are tolerated for PB2399 (the alphabet family, `LinkageFormal`) and PB2420 (`ReportScope`,
`RangeCollationCarrier`, `NumericDisplayForm`, `CellLifetime`, `CallExternalBacking`). The census was recorded at
`2a581b75c` and the ratchet keyed the edges file to it.

**What the gate's tests changed.** The backend contract now measures the slices. Their name tables are classified as
COBOL words. The storage tier's member names, the cell lifetimes and the suppressed linkage root fields are recorded as
§1.3 L4 leaks: they always crossed the seam, but the binder hid them from the walk. Three inventory code locations
followed the moves (record_verdicts batch `w20261855a-pb2361-member-moves`, three code locations re-sited, GAP 1 → 1).

**Not closed.** PB2361 stays open on its second half. The members the earlier extracts widened from `private` to
`internal` still need their final owners and a reader-list drift test. `DataItem` immutability after binding belongs
to PB2323. PB2293 and PB4273 wait on PB2361.
