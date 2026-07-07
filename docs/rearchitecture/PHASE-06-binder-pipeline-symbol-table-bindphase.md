# PHASE 06 — Real Binder Phase: Manifest Pass Pipeline, SymbolTable, BoundCompilation, BindPhase Extraction

- **Phase:** P6
- **Track:** rearchitecture
- **Risk:** HIGH (the OO method-scope lookup collapse is the single most behavior-sensitive change in the phase)
- **Depends on:** P5 (Unified data model — `StorageForm` discriminator, `Binding/Model/` folder, `RecordLayout`,
  and the no-op pass scaffolding). Also assumes P0 (characterization harness + green baseline) and P2 (the
  `Cobol.Net.Editions` leaf assembly + diagnostic registry) are landed, but P6 does not require P2 to *complete*
  — where P6 touches diagnostics it keeps the current `EditionContext` sink and lets P2/P7 re-home it.
- **Blocks:** P7 (binder/emitter collaborator split + exhaustive visitor) and P9 (OO subsystem move) both consume
  the `BoundCompilation` + `BindPipeline` this phase produces. Neither is in scope here.

## GOAL (one paragraph)

Turn the binder into a **real compiler phase**. Today there is no Binder phase boundary: `DataBinder.BindResolve`
runs ~15 post-build passes ordered only by call sequence + comments (`Binding/DataBinder.cs:210-258`), and the
*real* middle-end orchestrator — another ~12 implicit passes including the cross-layer `StoreAsImage` write-back —
is buried inside the codegen class (`CodeGen/CSharpEmitter.Call.cs:88-194`, `CallEmitRunUnit`). This phase promotes
the P5 no-op passes into real `IBindPass` classes over ONE canonical `BindManifest`; extracts the binder half of
`CallEmitRunUnit` into a `BindPipeline`/`BinderDriver` returning an **immutable `BoundCompilation`** so the driver's
Phase 2 is literally *Bind then Emit* and `CheckOnly`/`NoEmit` stops after Bind; introduces ONE scope-aware
`SymbolTable` that collapses the `LookupData` / `LookupDataInScopeOf` / `TryGetVisibleIndexField` / `IndexFieldFor`
quadruple (`Binding/DataBinder.Oo.cs:76-121`) with §11.7 GR5 sibling-invisibility folded in; seals the ~30 public
mutable dictionaries into read-only views; and adds a completion-phase watermark gate so "read a fact before its
producing pass ran" becomes a loud, located compiler error rather than a silent miscompile. The exhaustive visitor,
the god-class collaborator split, and the OO subsystem move are explicitly **out of scope** (P7 / P9).

## EXIT CRITERIA (all must hold at phase end)

1. `BindPipeline.ValidateDag()` runs at pipeline construction and is green; a deliberately mis-ordered manifest
   throws at construction (a unit test proves it).
2. `BoundCompilation` is immutable and the emitter consumes it **read-only** — no CodeGen writes into the Binding
   data model (`CSharpEmitter.MarkStoreAsImage` write-back is gone; grep proves it).
3. The lookup quadruple is collapsed to ONE scoped resolver (`SymbolTable.TryResolve` / `TryResolveCondition` /
   `TryResolveIndex`); `LookupData` / `LookupDataInScopeOf` / `TryGetVisibleIndexField` / `IndexFieldFor` are
   **deleted** (grep returns zero definitions and zero call sites).
4. `CheckOnly` halts after Bind — it never constructs C# text (verified by a test that a construct which only fails
   at emit/Roslyn still returns `Success` under `CheckOnly`, and by the absence of an `Emit` call on the
   `CheckOnly` path).
5. The full battery is green: 2028+ greenfield conformance + 213+ unit, the FULL legacy guard NIST **353 MATCH**,
   and the OO method-scope goldens specifically. Emitted-C# snapshots (gate 3) are neutral or reviewed-re-baselined.

## STATUS

`NOT STARTED`

> The executing session MUST update this line as it goes: `IN PROGRESS @ step N` after each completed step, and
> `DONE` once Verification (§5) passes. If resuming, read this line first, then re-run Step 0's battery to confirm
> the tree is green before continuing.

---

## 1. Rationale — the problems this phase fixes

Every item below is grounded in the as-built code and the rearchitecture survey (`DESIGN-binder-bound-tree.md`
§1, `DESIGN-data-model.md` §1, `DESIGN-module-topology.md`, and the driver dossier).

1. **Two hidden, undeclared pass pipelines (the prime latent-bug class).**
   - `DataBinder.BindResolve` (`Binding/DataBinder.cs:210-258`) hand-orders ~15 passes (`ExpandTypes →
     ResolveIndexItems → InheritUsageClauses → InheritSignClauses → ResolveRedefines → ClassifyRedefinesClasses →
     CheckStrongTypeDeclarations → OoRouteMethodRedefinesBackings → OdoResolve → DynamicResolve → ResolveFiles →
     GateNationalRecords → ResolveReports → CallBindExternalAndGlobal → PtrBindBasedAndAddressables → the FILE-record
     whole-group loop`). The ordering constraints are real (the `:212-217` comment explains `InheritSignClauses`
     MUST precede `ClassifyRedefinesClasses`) but exist ONLY as prose. Nothing asserts a pass's inputs were produced.
   - The **second, hidden pipeline** is `CSharpEmitter.CallEmitRunUnit` (`CodeGen/CSharpEmitter.Call.cs:88-194`):
     `TurnState.Build → CallCollectUnits → OO data/body binding → CallBindUnitData (per unit) →
     CallBuildUserFunctionTable → CallBindUnitProcedure (per unit) → CompilerTemp re-sync → MarkStoreAsImage (per
     class/unit) → OoHarmonizeOverrideCrossings → EC gate → file-connector qualification`, then emission. The
     driver's own phase names (Bind / Emit / Roslyn, `CompilerDriver.cs:101-114`) are a fiction: there is no Binder
     phase boundary — `CSharpEmitter.Emit` binds *and* emits in one call.

2. **No symbol table — a public-mutable-dictionary blackboard.** `DataBinder` exposes ~30 public
   get-only-but-mutable collections (`Binding/DataBinder.cs:26-77` and on: `Roots`, `ByName`, `Conditions`,
   `IndexFields`, `CapacityRegisters`, `TypeDecls`, `Files`, `FilesByName`, `WholeGroupReferenced`,
   `CompilerTempClones`, …). These ARE the module's API; downstream phases both read and WRITE them — most
   egregiously `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) and `ReferenceResolver` mutating
   `WholeGroupReferenced` mid-resolve (`ReferenceResolver.cs:280,303`). No ownership boundary, no immutability.

3. **The lookup quadruple.** Name lookup is quadrupled because OO method scoping is a parallel shadow name-model
   callers must opt into: `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor`
   (`Binding/DataBinder.Oo.cs:76-121`). A caller that reaches `ByName`/`IndexFields` directly silently misses
   §8.4.6.2.1 rule 3a / §11.7.4 GR5 sibling-invisibility — a torn read/write of the wrong storage. Call sites are
   scattered across `ReferenceResolver.cs:52,681`, `OdoModel.cs:307`, `StatementBinder.Sort.cs:174`,
   `StatementBinder.Intrinsics.cs:81,829`, `StatementBinder.Initialize.cs:124,138`, and
   `StatementBinder.cs:1006,1019,1026,1054,1072,1082,1162`.

4. **`CheckOnly` is a misnomer.** `CompilerDriver.Compile` under `CheckOnly` still calls the full
   `CSharpEmitter().Emit(...)` (`CompilerDriver.cs:112`) and only skips the Roslyn backend — it builds the entire
   C# string it then throws away. With a real Bind phase, `CheckOnly` becomes "stop after `BindPipeline.Run`".

This phase makes the pipeline shape literal and matches the driver's phase names:

```
Frontend (parse) ─► Binder (BindPipeline: N ordered passes) ─► BoundCompilation ─► Backend (emit) ─► Roslyn
```

---

## 2. Target end-state for this phase (concrete)

When this phase is DONE the following exist. (P7/P9 will further split god classes and move OO; those are NOT
required here.)

### New files (`src/Cobol.Net.Compiler/`)

- `Binding/Pipeline/Capability.cs` — the `Capability` enum (the closed set of computed forest facts).
- `Binding/Pipeline/IBindPass.cs` — `interface IBindPass { string Name; IReadOnlyList<Capability> Requires;
  IReadOnlyList<Capability> Produces; void Run(BindContext ctx); }`.
- `Binding/Pipeline/BindContext.cs` — the shared spine threaded to every pass (parse ctx, edition, diagnostics
  sink, the `SymbolTableBuilder`, `ReferenceResolver`, `RecordLayout`, and the `Watermark` gate).
- `Binding/Pipeline/BindPipeline.cs` — owns the ordered `IReadOnlyList<IBindPass>`, `ValidateDag()` (throws
  `CompilerConfigurationException` on a Requires-before-Produces violation), and `Run(ctx)`.
- `Binding/Pipeline/BindManifest.cs` — the ONE canonical ordered pass list (`Standard()`), the single place a
  maintainer reads to learn the order.
- `Binding/Pipeline/*Pass.cs` — the real `IBindPass` classes: `DeclarationPass`, `ExpandTypesPass`,
  `UsageMarkerPass` (renamed from `ResolveIndexItems`), `UsageInheritancePass`, `SignInheritancePass`,
  `RedefinesResolvePass`, `RedefinesClassifyPass`, `StrongTypeCheckPass`, `OoRedefinesRoutePass`, `OdoResolvePass`,
  `DynamicResolvePass`, `FilesResolvePass`, `NationalGatePass`, `ReportsResolvePass`, `ExternalGlobalPass`,
  `PointerBindPass`, `ProcedureBindPass`, `StorageFormPass`. (Several may be thin adapters delegating to existing
  `DataBinder` method bodies in this phase — the *class boundary + Requires/Produces contract* is what matters
  now; the internal body split into focused collaborators is P7.)
- `Binding/BinderDriver.cs` — the extracted binder half of `CallEmitRunUnit`. `public BoundCompilation
  Bind(Core.CompilationUnitContext tree, EditionContext edition, IReadOnlyList<TurnEvent>? turnEvents,
  OoBindCallbacks oo)`.
- `Binding/Model/BoundCompilation.cs` — the immutable result: `record BoundCompilation(IReadOnlyList<BoundUnit>
  Units, OoClassTable Classes, SymbolTable Symbols, TurnState Turn, bool EcActive, bool AnyFiles,
  IReadOnlyList<string> Diagnostics)`.
- `Binding/Model/BoundUnit.cs` — the per-program-unit bound artifact (renamed/relocated from the emitter's private
  `CallUnit`): identity/containment/attributes + `DataBinder Data`, `ReferenceResolver Refs`, `BoundProgram Bound`,
  `IReadOnlyList<CallBridge> Bridges`, `IsFunction`/`IsPrototype`.
- `Binding/Model/SymbolTable.cs` + `Binding/Model/SymbolTableBuilder.cs` + `Binding/Model/Scope.cs` — the ONE
  scope-aware resolver (`TryResolve`/`TryResolveCondition`/`TryResolveIndex`/`Roots`) and its write handle; `Scope`
  is `Program` or a method scope carrying the `OoMethodDataScope`.

### Deleted / relocated

- `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), the `CompilerTempClones` re-sync
  (`CSharpEmitter.Call.cs:115-118`), and the FILE whole-group loop (`DataBinder.cs:238-257`) — all folded into
  `StorageFormPass` (a Bind-phase pass). No CodeGen write into the Binding model remains.
- `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` — deleted; every caller uses
  `SymbolTable.TryResolve*` with an explicit `Scope`.
- The private `CallUnit` / `OoClassUnit` modeling is relocated to `Binding/Model/` (the emitter consumes the model
  type, no longer owns it).

### Changed

- `CompilerDriver.Compile` — Phase 2 is `var comp = BinderDriver.Bind(...); if (edition.HasErrors) return
  BindError; if (options.CheckOnly) return Success; string cs = new CSharpEmitter().EmitBound(comp); …`.
- `CSharpEmitter.Emit(tree, edition, turnEvents)` retained as a thin shim = `EmitBound(BinderDriver.Bind(...))`
  for any remaining direct callers; the emit-only entry point is `EmitBound(BoundCompilation)`.

---

## 3. STEP-BY-STEP

> **Conventions.** All paths are absolute-from-repo-root under `E:/CobolSharp/`. "Full battery" = §5. On Windows the
> greenfield suites run via `dotnet test`; the LEGACY NIST guard runs on WSL/Linux (`scripts/guard-fast.sh`, see
> `MEMORY.md` → `reference_wsl_linux_repro`). Each numbered step ends by stating whether it is a **COMMIT
> BOUNDARY**. Keep the battery green at every commit boundary. Never delete a mutation site or a lookup method on
> faith — **prove-then-delete** (introduce the replacement, cross-check across the corpus, then delete).

### Step 0 — Preflight: baseline, confirm P5 deliverables, seed snapshots

**Do:**
1. Build and run the full battery (§5). Record the exact counts (conformance / unit / NIST MATCH) into the DEVLOG
   working notes — this is the neutrality baseline every later step compares against.
2. Confirm P5's deliverables are present, because P6 builds directly on them. Grep/read:
   - `Binding/Model/StorageForm.cs` exists (the discriminator).
   - `Binding/Model/RecordLayout.cs` exists (the one offset/width authority).
   - The P5 "pass scaffolding" exists in some form (either `Binding/Pipeline/` skeletons or the `BindResolve`
     method bodies already wrapped as no-op `IBindPass` shells). If P5 shipped only `StorageForm` + `RecordLayout`
     and left `BindResolve` un-wrapped, that is fine — Step 1 does the wrapping; just note it in STATUS.
   - Whether P5's `StorageFormPass` already exists and computes `StorageForm` in parallel with `MarkStoreAsImage`
     under a cross-check. If it does, Step 3 *relocates* the invocation; if it does not, Step 3 creates the pass
     from `MarkStoreAsImage`'s body.
3. Seed / refresh the characterization emitted-C# snapshots (P0 harness) so gate 3 has a pre-P6 baseline:
   `COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test tests/Cobol.Net.Tests.Characterization` (skip if P0's project is not
   yet present — then gate 3 is manual `.g.cs` diffing of a handful of representative programs).

**Verify:** battery green; the P5 files above exist. If the battery is not green at HEAD, STOP — do not start P6 on
a red tree.

**Commit boundary:** No (read-only preflight). Update STATUS to `IN PROGRESS @ step 1`.

---

### Step 1 — Promote the data-division passes into real `IBindPass` classes over `BindManifest`

**Why:** kill the first hidden pipeline (`BindResolve`) by making each post-build pass a class that declares
`Requires`/`Produces`, and asserting the DAG at construction. This is a pure refactor — same passes, same order,
zero behavior change — so it is the safe first move.

**Do:**
1. Create `Binding/Pipeline/Capability.cs` with the closed enum (from `DESIGN-binder-bound-tree.md` §3.1):
   `EntryTree, TypesExpanded, UsageMarkersResolved, UsageInherited, SignInherited, RedefinesResolved,
   RedefinesClassified, StrongTypesChecked, OoRedefinesRouted, OdoResolved, DynamicResolved, FilesResolved,
   NationalGated, ReportsResolved, ExternalGlobalBound, PointersBound, ProcedureBound, StorageFormComputed`.
2. Create `Binding/Pipeline/IBindPass.cs`, `Binding/Pipeline/BindContext.cs`, `Binding/Pipeline/BindPipeline.cs`
   (with `ValidateDag()` accumulating the produced-set and asserting `Requires ⊆ produced-so-far` for each pass,
   throwing `CompilerConfigurationException` on violation), and `Binding/Pipeline/BindManifest.cs` with
   `Standard()` returning the ordered list.
3. For each current `BindResolve` call, create a `*Pass` class whose `Run(ctx)` calls the existing `DataBinder`
   method body (keep the body in `DataBinder` for now — this step establishes the *contract*, not the internal
   split). Rename `ResolveIndexItems` → `UsageMarkerPass` at the class level only (the design's §2.7 rename); the
   underlying method can keep its name until P7. Map exactly:

   | Manifest pass | Wraps (`DataBinder` member) | Requires | Produces |
   |---|---|---|---|
   | `ExpandTypesPass` | `ExpandTypes` | EntryTree | TypesExpanded |
   | `UsageMarkerPass` | `ResolveIndexItems` | TypesExpanded | UsageMarkersResolved |
   | `UsageInheritancePass` | `InheritUsageClauses` | UsageMarkersResolved | UsageInherited |
   | `SignInheritancePass` | `InheritSignClauses` | UsageInherited | SignInherited |
   | `RedefinesResolvePass` | `ResolveRedefines` | SignInherited | RedefinesResolved |
   | `RedefinesClassifyPass` | `ClassifyRedefinesClasses` | RedefinesResolved | RedefinesClassified |
   | `StrongTypeCheckPass` | `CheckStrongTypeDeclarations` | RedefinesClassified | StrongTypesChecked |
   | `OoRedefinesRoutePass` | `OoRouteMethodRedefinesBackings` | RedefinesClassified | OoRedefinesRouted |
   | `OdoResolvePass` | `OdoResolve` | RedefinesClassified | OdoResolved |
   | `DynamicResolvePass` | `DynamicResolve` | OdoResolved | DynamicResolved |
   | `FilesResolvePass` | `ResolveFiles` | RedefinesClassified | FilesResolved |
   | `NationalGatePass` | `GateNationalRecords` | FilesResolved | NationalGated |
   | `ReportsResolvePass` | `ResolveReports` | FilesResolved | ReportsResolved |
   | `ExternalGlobalPass` | `CallBindExternalAndGlobal` | EntryTree | ExternalGlobalBound |
   | `PointerBindPass` | `PtrBindBasedAndAddressables` | EntryTree | PointersBound |

   (`DeclarationPass` wrapping `BindDeclarations`/`BindEntries` producing `EntryTree`, and `ProcedureBindPass` +
   `StorageFormPass`, are added in Steps 2/3 — this step covers only the data-division post-build passes plus the
   declaration pass. The FILE whole-group loop at `DataBinder.cs:238-257` stays put for now; Step 3 moves it.)
4. Rewrite `DataBinder.BindResolve` to build a data-division sub-pipeline from these passes and `Run` it against a
   `BindContext` seeded with the current binder (a thin adapter so `ctx` exposes the `DataBinder` this phase). The
   call order MUST be byte-identical to the current `:218-232` sequence. Preserve the `InheritSignClauses`-before-
   `ClassifyRedefinesClasses` constraint — the DAG assert now encodes it (`SignInherited` before
   `RedefinesResolved`/`RedefinesClassified`).
5. Add a unit test `BindPipelineTests.ValidateDag_RejectsOutOfOrderManifest` that constructs a `BindPipeline` with
   two passes swapped and asserts it throws `CompilerConfigurationException`. Add
   `ValidateDag_AcceptsStandardManifest` asserting `BindManifest.Standard()` validates clean.

**Verify:**
- `dotnet build src/Cobol.Net.Compiler` clean.
- `dotnet test tests/Cobol.Net.Tests.Unit --filter BindPipeline` green (the two new tests).
- Full battery (§5) green — this is a no-op refactor, so conformance/NIST counts must be **identical** to Step 0.

**Commit boundary: YES.**
`refactor(cobolnet): P6.1 — promote BindResolve post-build passes to IBindPass over a DAG-validated BindManifest (no-op)`

---

### Step 2 — Extract the binder half of `CallEmitRunUnit` into `BinderDriver` returning `BoundCompilation`

**Why:** kill the second hidden pipeline. Give the compiler a real Binder phase boundary; make the emitter consume
a bound result instead of orchestrating binding.

**Do:**
1. Relocate the emitter's private `CallUnit` type (`CSharpEmitter.Call.cs:30-57`) to `Binding/Model/BoundUnit.cs`
   as an `internal sealed class BoundUnit` with the same fields (`Name`, `ClassName`, `Ctx`, `Parent`, `Children`,
   `Initial/Common/Recursive`, `IsFunction`, `IsPrototype`, `Data`, `Refs`, `Bound`, `Bridges`, `Path`,
   `ClassRef`). Relocate `CallBridge` (`:63`) beside it. This is a mechanical rename — the emitter's ~40 references
   to `CallUnit`/`unit.*` compile unchanged once the type moves and a `using CobolNet.Binding.Model;` is added.
   Keep `OoClassUnit` where it is for now (P9 owns OO relocation).
2. Create `Binding/Model/BoundCompilation.cs`:
   ```csharp
   internal sealed record BoundCompilation(
       IReadOnlyList<BoundUnit> Units,
       OoClassTable Classes,
       SymbolTable? Symbols,      // null until Step 7 lands SymbolTable; filled then
       TurnState Turn,
       bool EcActive,
       bool AnyFiles);
   ```
   (`Symbols` is added now as nullable to avoid a second signature churn in Step 7; the emitter does not read it
   until the lookup migration.)
3. Create `Binding/BinderDriver.cs`. **Move** the binder half of `CallEmitRunUnit` (`CSharpEmitter.Call.cs:94-157`)
   into `BinderDriver.Bind`. That body is: `TurnState.Build` → `CallCollectUnits` → the OO data/body binding calls
   → `CallBindUnitData` per unit → `CallBuildUserFunctionTable` → `CallBindUnitProcedure` per unit → the
   CompilerTemp re-sync + `MarkStoreAsImage` loop (LEAVE these two here for Step 3 to move) → `OoHarmonizeOverride
   Crossings` → the EC gate (`_ecActive`) → the two file-connector qualification loops → compute `anyFiles`. It
   returns `new BoundCompilation(units, _ooClasses, Symbols: null, turnState, ecActive, anyFiles)`.
4. **The OO entanglement seam (keeps P9 clean).** The OO orchestration methods (`OoBindInterfaceData`,
   `OoBindClassData`, `ValidateOverrideSignatures`, `ValidateImplements`, `OoBindClassBody`,
   `OoHarmonizeOverrideCrossings`, `OoQualifyClassFiles`) currently live on `CSharpEmitter` partials and only
   *mutate binder state* (they do not emit). Do NOT move them in P6. Instead, define
   `Binding/Model/OoBindCallbacks.cs` — a bundle of delegates the caller supplies:
   ```csharp
   internal sealed record OoBindCallbacks(
       Action<OoClassTable> BuildClassTableInto,           // sets _ooClasses on the caller if it needs it
       Action BindInterfaceAndClassData, Action ValidateOo, Action BindClassBodies,
       Action HarmonizeOverrideCrossings, Action<OoClassTable> QualifyClassFiles);
   ```
   `CSharpEmitter` constructs this bundle from its existing OO methods and passes it to `BinderDriver.Bind`. This
   preserves behavior exactly, keeps the OO code physically in the emitter for P9 to move, and still yields the
   `BoundCompilation`. (Document this as the intentional P6→P9 seam.)
5. Rewrite `CSharpEmitter`:
   - `public string Emit(tree, edition, turnEvents)` becomes a shim: `return EmitBound(new BinderDriver().Bind(tree,
     edition, turnEvents, BuildOoCallbacks()));`.
   - Add `internal string EmitBound(BoundCompilation comp)` = the emit half of `CallEmitRunUnit` (`:158-194`): the
     `CodeWriter` setup, the `using`s, interface/class/program-class emission, and the entry wrapper. It reads
     `comp.Units`, `comp.Classes`, `comp.Turn`, `comp.EcActive`, `comp.AnyFiles` — no binding.
   - The per-unit emit methods (`CallEmitProgramClass`, etc.) already take a `CallUnit`; they now take a
     `BoundUnit` (same fields). `_turnState`, `_ecActive`, `_ooClasses` are set from `comp` at the top of
     `EmitBound`.

**Verify:**
- Build clean. The move must be a pure relocation — diff should show code moving, not changing.
- Full battery green with **identical** conformance/NIST counts to Step 0. Emitted-C# snapshots (gate 3)
  **byte-identical** (this is the strongest neutrality check for this step — the generated `.g.cs` must not move).

**Commit boundary: YES.**
`refactor(cobolnet): P6.2 — extract the binder half of CallEmitRunUnit into BinderDriver → immutable BoundCompilation; emitter becomes EmitBound(comp)`

---

### Step 3 — Fold `MarkStoreAsImage` + CompilerTemp re-sync + FILE whole-group loop into `StorageFormPass`

**Why:** the emitter writing the binder's data model (`MarkStoreAsImage`) is the last CodeGen→Binding write-back.
Exit criterion #2 (immutable `BoundCompilation`) cannot hold while it exists. Move the whole-group→image decision
into a Bind-phase pass that runs after procedure binding, where the `WholeGroupReferenced` fact is fully collected.

**Do:**
1. Create `Binding/Pipeline/ProcedureBindPass.cs` wrapping the per-unit `CallBindUnitProcedure` loop (it produces
   `ProcedureBound` and, as a side effect today, `WholeGroupReferenced` via `ReferenceResolver`). `Requires` all
   data-division capabilities; `Produces ProcedureBound`. (In this phase `ProcedureBindPass` may still run inside
   `BinderDriver` rather than the manifest `Run` loop if unit-order threading makes a single `Run` awkward — the
   *contract* and ordering are what matter. Prefer routing it through the manifest; if you cannot without churn,
   leave a `// TODO(P7): route through BindPipeline.Run` and keep the explicit call, still after all data passes.)
2. Create `Binding/Pipeline/StorageFormPass.cs`. Its `Run` does exactly what these three fragments do today, in
   this order, over every unit's `DataBinder` (and each class's `Data`/`FactoryData`):
   - the CompilerTemp re-sync (`CSharpEmitter.Call.cs:115-118`): `foreach temp: temp.StoreAsImage =
     model.StoreAsImage`.
   - `MarkStoreAsImage` (`CSharpEmitter.cs:50-68`): flag numeric-DISPLAY leaves under each `WholeGroupReferenced`
     group.
   - the FILE whole-group loop (`DataBinder.cs:238-257`): add each group FD/SD record to `WholeGroupReferenced`
     and `MarkImageLeaves`.
   `Requires ProcedureBound`; `Produces StorageFormComputed`.
   > **Note on P5 overlap.** If P5 already created a `StorageFormPass` that computes the `StorageForm` discriminator
   > and cross-checks it against `MarkStoreAsImage`, MERGE into that class: keep the cross-check assert, and this
   > step simply makes it the manifest-ordered owner and deletes the emitter-side invocation. Do NOT delete the
   > `StoreAsImage` *flag* here — that flag's removal is the data-model track's prove-then-delete (P5/later); P6
   > only relocates *where the decision runs* so no CodeGen write remains.
3. Delete `CSharpEmitter.MarkStoreAsImage` and its two call sites (`CSharpEmitter.Call.cs:119-120`), the
   CompilerTemp re-sync loop (`:115-118`), and the FILE whole-group loop in `DataBinder.BindResolve`
   (`:238-257`). `BinderDriver.Bind` now calls `ProcedureBindPass` then `StorageFormPass` (via the manifest or the
   explicit ordered calls) before returning `BoundCompilation`.
4. Add `ProcedureBindPass` and `StorageFormPass` to `BindManifest.Standard()` as the final two entries.

**Verify:**
- `grep -rn "MarkStoreAsImage" src/Cobol.Net.Compiler/CodeGen` returns nothing.
- Full battery green with identical counts. The sentinels for this step are the whole-group / ODO / SORT-SD /
  group-MOVE goldens (per `DESIGN-data-model.md` §4 risk 1): NC247A-class ODO programs, ST102A/ST103A (SD/FILE
  record image), and any group-MOVE differential. If any diff appears, `StorageFormPass` missed a site — compare
  the emitted `.g.cs` field storage against the pre-step snapshot.

**Commit boundary: YES.**
`refactor(cobolnet): P6.3 — fold MarkStoreAsImage + CompilerTemp re-sync + FILE whole-group loop into StorageFormPass; delete the emitter→binder write-back`

---

### Step 4 — Driver: Phase 2 = Bind then Emit; `CheckOnly` halts after Bind

**Why:** make the phase boundary real at the driver, and fix the `CheckOnly` misnomer (exit criterion #4).

**Do:**
1. Edit `CompilerDriver.Compile` (`CompilerDriver.cs:101-120`). Replace the single `Emit(...)` with:
   ```csharp
   var edition = new Binding.EditionContext(options.DialectLevel, options.Permissive);
   new Validation.EditionValidator(edition).Validate(tree);
   if (edition.HasErrors) return new Result(Outcome.BindError, ...);

   var comp = new Binding.BinderDriver().Bind(tree, edition, frontend.TurnEvents);   // Phase 2a — BIND
   if (edition.HasErrors || edition.Diagnostics.Count > 0)
       return new Result(Outcome.BindError, "", null, edition.Diagnostics, [.. feWarnings, .. edition.Warnings]);

   if (options.CheckOnly)   // stop after Bind — no C# text is built
       return new Result(Outcome.Success, "", null, [], [.. feWarnings, .. edition.Warnings]);

   string csharp = new CodeGen.CSharpEmitter().EmitBound(comp);   // Phase 2b — EMIT
   ```
   (Give `BinderDriver.Bind` a driver-facing overload that constructs the `OoBindCallbacks` internally, or expose a
   small `Binder` façade so the driver need not know about the OO seam — keep the CLI/driver surface clean.)
2. Update the XML doc on `Options.CheckOnly` to say "stop after Bind" (it currently says "bind/emit ONLY").
3. Strengthen the existing `CheckOnlyCompileTests` (`tests/Cobol.Net.Tests.Unit`): add a case whose program BINDS
   clean but would fail only at emit/Roslyn (e.g. a construct that emits a `LoudStmt`/`NotImplemented` but binds),
   and assert `CheckOnly` returns `Outcome.Success`. Add an assertion (a spy or an instrumentation flag) that
   `EmitBound` is NOT invoked on the `CheckOnly` path.

**Verify:**
- `dotnet test tests/Cobol.Net.Tests.Unit --filter CheckOnly` green.
- The INV-1 continuity sweep still passes (it drives `check-batch`, which uses `CheckOnly`):
  `scripts/version-continuity-sweep.sh` (or the in-process equivalent) — no `BREAKS`.
- Full battery green.

**Commit boundary: YES.**
`feat(cobolnet): P6.4 — driver Phase 2 splits into Bind then Emit; CheckOnly halts after Bind (no wasted C# emission)`

---

### Step 5 — Seal the mutable dictionaries into read-only views on `BoundCompilation`

**Why:** close every open write channel (exit criterion #2). After Steps 2-3 the emitter no longer writes the
model; now make that impossible by construction — the emitter receives read-only views.

**Do:**
1. Add a `BindModel` accessor object (or expose read-only projections directly on `BoundUnit.Data`): change the
   emitter-facing surface of `DataBinder`'s collections (`Roots`, `ByName`, `Conditions`, `IndexFields`,
   `CapacityRegisters`, `Files`, `FilesByName`, `WholeGroupReferenced`, …) from public mutable
   `Dictionary`/`List`/`HashSet` to `IReadOnlyDictionary`/`IReadOnlyList`/`IReadOnlySet` **as seen by CodeGen**.
   The simplest low-churn realization: keep the backing collections private-settable inside `DataBinder`/the
   passes, and expose `IReadOnly*` properties the emitter binds to. Where the emitter currently mutates (there
   should be none left after Step 3 — verify), convert to a pass-side write.
2. `ReferenceResolver.WholeGroupReferenced.Add` at `ReferenceResolver.cs:280,303` writes during procedure binding
   — that is a *Bind-phase* write and is legitimate; keep it, but ensure the collection is only writable through
   the binder, not exposed mutable to CodeGen. (The clean move — a dedicated `UsageCollectionPass` owning
   `WholeGroupReferenced` — is a data-model-track item; P6 only needs the collection sealed against CodeGen. If
   `UsageCollectionPass` already exists from P5, prefer it.)
3. Make `BoundCompilation` / `BoundUnit` expose only `IReadOnly*` collections to `EmitBound`.

**Verify:**
- `grep` the CodeGen tree for writes into binder collections (`.Add(`, `.StoreAsImage =`, `[…] =`) targeting
  `DataItem`/`DataBinder` members — must be zero.
- Full battery green, identical counts. Snapshots neutral.

**Commit boundary: YES.**
`refactor(cobolnet): P6.5 — seal the binder's collections into read-only views on BoundCompilation; emitter is write-free over the data model`

---

### Step 6 — Completion-phase watermark gate

**Why:** the DAG assert guards *pass* order; the watermark guards *field reads* — it converts today's silent "read
a null `Tier`/`CapacityRegister`/`StorageForm`" into a loud, located compiler error.

**Do:**
1. Add `Capability Watermark { get; private set; }` to `BindContext` and `void MarkProduced(IReadOnlyList<Capability>)`
   advancing it; `BindPipeline.Run` calls `ctx.MarkProduced(p.Produces)` after each pass.
2. Add `void Require(Capability c)` on `BindContext` that asserts `Watermark >= c` (Debug: `Debug.Assert` with a
   located message naming the pass and the missing capability; the DAG validation at construction stays always-on
   in Release). Call `ctx.Require(...)` at the entry of each pass and at the small number of late-fact read points
   the design flags (`RedefinesClassified` before any `Tier`/`ClassOffset` read; `DynamicResolved` before a
   `CapacityRegister` read; `StorageFormComputed` before any storage-form read at emit).
3. Add a unit test that a pass reading a not-yet-produced capability trips the assert (Debug build).

**Verify:**
- `dotnet test tests/Cobol.Net.Tests.Unit --filter Watermark` green.
- Full battery green (the gate must never fire on a real program — if it does, the manifest order is wrong; fix
  the order, not the gate).

**Commit boundary: YES.**
`feat(cobolnet): P6.6 — completion-phase watermark gate: reading a fact before its producing pass is a located compiler error`

---

### Step 7 — Collapse the lookup quadruple into ONE scope-aware `SymbolTable` (HIGH RISK — land last, behind goldens)

**Why:** the singular-pattern fix and the phase's headline risk. This is done LAST and in TWO commits
(introduce+shim, then migrate+delete) so the OO §11.7 GR5 sibling-invisibility behavior is proven before the old
methods are removed.

#### Step 7a — Introduce `SymbolTable`/`SymbolTableBuilder`/`Scope`; wire the quadruple as thin shims

**Do:**
1. Create `Binding/Model/Scope.cs`: `readonly record struct Scope(OoMethodDataScope? Method)` with a static
   `Scope Program => new(null)`.
2. Create `Binding/Model/SymbolTable.cs` with the ONE lookup surface (semantics copied verbatim from the quadruple
   so behavior is identical):
   ```csharp
   internal sealed class SymbolTable
   {
       // §8.4.6.2.1 rule 3a / §11.7.4 GR5: a method-local name REPLACES (never unions) the object/program name.
       public bool TryResolve(string name, Scope scope, out IReadOnlyList<DataItem> items);      // ← LookupData / LookupDataInScopeOf
       public bool TryResolveCondition(string name, Scope scope, out IReadOnlyList<Condition88> conds);
       public bool TryResolveIndex(string name, Scope scope, out string field);                  // ← TryGetVisibleIndexField / IndexFieldFor
       public IReadOnlyList<DataItem> Roots(Scope scope);
   }
   ```
   - `TryResolve(name, scope)` folds BOTH `LookupData` (active-method-scope-first, `DataBinder.Oo.cs:76-81`) and
     `LookupDataInScopeOf` (anchor-root-owner-scope-first, `:88-94`): the `Scope` carries the method scope
     explicitly, so the "which overload" decision the caller used to make becomes a parameter.
   - `TryResolveIndex` folds `TryGetVisibleIndexField` (`:100-108`, returns false when a method-local *data-name*
     shadows the index-name) and `IndexFieldFor` (`:120-121`, the resolved-cell accessor). Preserve the
     data-name-shadows-index-name rule exactly.
3. Create `Binding/Model/SymbolTableBuilder.cs` — the write handle passes use to register names/conditions/index
   fields/capacity registers during binding; `Build()` seals it into the immutable `SymbolTable`. Back it by the
   existing `ByName`/`Conditions`/`IndexFields` collections initially (a wrapper), so no data moves yet.
4. Construct the `SymbolTable` in `BinderDriver.Bind` after binding and put it on `BoundCompilation.Symbols`.
5. **Re-express the four `DataBinder` methods as thin shims** over the new resolver, so nothing else changes yet:
   `LookupData(name)` → `Symbols.TryResolve(name, ActiveScope, out var l) ? l.ToList() : null` (where `ActiveScope`
   derives from the current `ActiveMethodScope`); `LookupDataInScopeOf(root, name)` →
   `Symbols.TryResolve(name, ScopeOf(root), out …)`; `TryGetVisibleIndexField` / `IndexFieldFor` → the two
   `TryResolveIndex` shapes. This proves the folded resolver is byte-equivalent while every call site is untouched.
6. Add `tests/Cobol.Net.Tests.Unit/SymbolTableTests.cs`: a focused set covering (a) program-scope resolution,
   (b) a method-local data-name shadowing an object-level name (§8.4.6.2.1 rule 3a), (c) a method-local
   data-name shadowing an object-level index-name (`TryResolveIndex` returns false), (d) a method-local index-name
   with its own cell shadowing an object index-name (§11.7.4 GR5), (e) an unshadowed object name visible from a
   method (the `LookupDataInScopeOf` global fallback). Mirror the `OoSpineTests` shadowing cases
   (`tests/Cobol.Net.Tests.Conformance/OoSpineTests.cs:1311`).

**Verify:** build clean; `dotnet test --filter SymbolTable` green; **full battery green with identical counts**
(the shims make this a pure equivalence step). This is the cross-check that the folded resolver matches the
quadruple before any deletion.

**Commit boundary: YES.**
`feat(cobolnet): P6.7a — introduce scope-aware SymbolTable; wire LookupData/…/IndexFieldFor as thin shims (byte-equivalent)`

#### Step 7b — Migrate every call site to `SymbolTable`, then delete the quadruple

**Do:**
1. Migrate each call site to `Symbols.TryResolve*` with an explicit `Scope`, one file per small commit if you
   prefer finer granularity (the battery must pass after each). The full site list:
   - `ReferenceResolver.cs:52` (`LookupData` in `recvItem`) → `TryResolve(recv, scope, …)`.
   - `ReferenceResolver.cs:681` (`TryGetVisibleIndexField`) → `TryResolveIndex`.
   - `OdoModel.cs:307` (`LookupDataInScopeOf(RootOf(item), depName)`) → `TryResolve(depName, ScopeOf(RootOf(item)), …)`.
   - `StatementBinder.Sort.cs:174`, `StatementBinder.Intrinsics.cs:81`, `StatementBinder.Initialize.cs:124,138`,
     `StatementBinder.cs:1006,1054,1162` (`LookupData`) → `TryResolve(…, scope, …)`.
   - `StatementBinder.Intrinsics.cs:829`, `StatementBinder.cs:1082` (`TryGetVisibleIndexField`) → `TryResolveIndex`.
   - `StatementBinder.cs:1019,1026,1072` (`IndexFieldFor`) → the resolved-cell shape of `TryResolveIndex`.
   The `Scope` at a `StatementBinder` call site is the active method scope (thread it from `ActiveMethodScope`
   through `BindContext.EnterMethodScope`/the current binder field); at a post-build `DataBinder`/`OdoModel` site
   it is `ScopeOf(anchorRoot)` (`OoRootOwner` lookup) since `ActiveMethodScope` is null there — exactly the reason
   `LookupDataInScopeOf` existed.
2. Once every call site is migrated and green, **delete** `LookupData`, `LookupDataInScopeOf`,
   `TryGetVisibleIndexField`, `IndexFieldFor` from `DataBinder.Oo.cs`. `grep -rn "LookupData\|LookupDataInScopeOf\|
   TryGetVisibleIndexField\|IndexFieldFor" src` must return zero.
3. Optionally back the `SymbolTableBuilder` by its own storage now instead of wrapping `ByName`/`IndexFields`
   (cleaner), but this can defer to P7 — the *interface* collapse is what P6 requires.

**Verify (the phase's most important gate):**
- Full battery green, with **the OO method-scope goldens specifically green**: `OoSpineTests` (all shadowing
  cases), and the NIST OO/keyed-scope programs. Run the greenfield conformance suite in full, not a filter.
- Legacy guard NIST **353 MATCH** unchanged.
- Snapshots neutral (a shadowing regression shows as a changed `.g.cs` storage reference — a `.g.cs` diff here is a
  RED, investigate before re-baselining).

**Commit boundary: YES (may be several).**
`refactor(cobolnet): P6.7b — migrate all call sites to SymbolTable.TryResolve*; delete the LookupData/…/IndexFieldFor quadruple`

---

### Step 8 — Phase-end: docs + design-currency

**Do:**
1. Update `resume-prompt.md`'s top STATE banner and add a DEVLOG entry (DESCENDING, real timestamp from
   `date "+%Y-%m-%d %H:%M %Z"`) summarizing the phase: the real Binder phase, the manifest, `BoundCompilation`,
   the SymbolTable collapse, and any deviations from this doc (per `feedback_follow_design_docs_and_spec` —
   deviations MUST be recorded in the design doc in the same change set).
2. Update `docs/DOC_INDEX.md` if any doc's status changed; mark `DESIGN-binder-bound-tree.md` §5 steps 1-4 as
   landed (or note what remains for P7).
3. Update this file's STATUS line to `DONE`.

**Commit boundary: YES.**
`docs(cobolnet): P6 — real Binder phase complete; sync resume-prompt/DEVLOG/DOC_INDEX`

---

## 4. What is explicitly NOT in this phase (guard against scope creep)

- The `StatementBinder`/`CSharpEmitter` collaborator split (23/15 partials → real classes) and the
  source-generated exhaustive visitor — **P7**. In P6 the passes may still delegate to existing `DataBinder`/
  emitter method bodies; only the *class boundary + Requires/Produces contract + the BinderDriver seam* land now.
- Moving the OO subsystem into `Oo/` + `OoDriver` — **P9**. P6 keeps OO binding methods physically in the emitter,
  invoked through the `OoBindCallbacks` seam.
- Deleting the `DataItem.StoreAsImage` flag itself and retiring the runtime overload bridge — **data-model track /
  P5 prove-then-delete**. P6 only relocates *where the storage-form decision runs* (into `StorageFormPass`) so no
  CodeGen write-back remains.
- Making `Place` structured (non-string) and the `BindStatementCore`/`EmitStatement` god-switch conversion — **P7**.

---

## 5. Verification — the full battery to run at every commit boundary and at phase end

**Greenfield (Windows, `dotnet test`):**
```
dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance    # 2028+ conformance
dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit           # 213+ unit
```
**Legacy differential guard (WSL/Linux — the frozen oracle; see MEMORY reference_wsl_linux_repro):**
```
bash E:/CobolSharp/scripts/guard-fast.sh                        # NIST 353 MATCH + golden-cleanliness
```
**Version continuity (drives the greenfield CLI's check-batch — validates Step 4):**
```
bash E:/CobolSharp/scripts/version-continuity-sweep.sh          # INV-1: no BREAKS across 4 editions
```
**Characterization / emitted-C# snapshots (gate 3, advisory — P0 harness):**
```
dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Characterization
# a .g.cs diff with NO source change in the commit is a RED; an intentional emit change re-baselines with review
# (COBOLNET_UPDATE_SNAPSHOTS=1) ONLY after gate 1 proves it behavior-preserving.
```
**Behavior-neutrality checks specific to P6:**
- Steps 1, 2, 3, 5, 6 are refactors → conformance/NIST counts **identical** to the Step 0 baseline, and the
  emitted `.g.cs` for a representative corpus **byte-identical** (Step 2/3 are the storage-form-sensitive ones).
- Step 7 is the behavior-sensitive one → run the FULL conformance suite (not a filter); the OO method-scope
  goldens (`OoSpineTests`) and the keyed/SEARCH scope programs are the sentinels; NIST 353 MATCH unchanged.
- Exit-criterion greps: `MarkStoreAsImage` absent from `CodeGen/`; the four lookup methods absent from `src/`; no
  CodeGen write into `DataItem`/`DataBinder` collections.

**Manual CLI smoke (a fast local confidence check):**
```
dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <prog.cob> --std 2002 -o E:/tmp/out.dll --run
```
Use an OO program with a method-local name that shadows an object-level name (Step 7 sentinel) and a group-MOVE /
ODO program (Step 3 sentinel).

---

## 6. Rollback / resumability

- **Resuming mid-phase:** read the STATUS line, then re-run §5 to confirm the tree is green at the last commit. Each
  step is a self-contained commit behind the green battery — resume at the next unstarted step. Never start a step
  on a red tree.
- **Every step is independently revertible** (`git revert <sha>`), because each is behavior-neutral at its boundary
  and does not depend on a later step. The one ordering rule: Step 7 (SymbolTable) MUST come after Steps 1-6 —
  it is the HIGH-risk change and relies on the immutable `BoundCompilation` (Step 2) and the sealed views (Step 5)
  being in place.
- **Risks + mitigations:**
  - **R1 — OO method-scope shadowing regression (HIGH).** The quadruple collapse is the single most behavior-
    sensitive change. Mitigation: Step 7 is split into 7a (introduce + shim, byte-equivalent, cross-checked
    against the quadruple across the whole corpus) and 7b (migrate + delete). The `SymbolTableTests` +
    `OoSpineTests` must be green before 7b's deletion. If a shadowing golden regresses in 7b, the `Scope` passed at
    that call site is wrong (most likely `Scope.Program` where a method scope was needed) — fix the call site, not
    the resolver.
  - **R2 — StorageForm equivalence gaps (HIGH).** `StorageFormPass` must reproduce ALL of `MarkStoreAsImage`'s
    accreted sites (SORT SD, FILE whole-group, CompilerTemp clones, Linkage `:275`, Reports `:352`, the OO re-sync
    `Oo.cs:697`). Mitigation: Step 3 keeps the P5 cross-check assert if present; the group-MOVE/ODO/SD goldens are
    the sentinels; a `.g.cs` field-storage diff pinpoints the missed site.
  - **R3 — the OO `OoBindCallbacks` seam leaks emit state into Bind (MEDIUM).** The OO binding methods stay on the
    emitter in P6. Mitigation: they only mutate binder state today (verified in the survey); the seam is a
    documented P6→P9 bridge, removed when P9 moves OO into `OoDriver`.
  - **R4 — pass-granularity re-walk cost (LOW/MEDIUM).** Wrapping bodies as passes must not multiply
    `AllItems()` walks. Mitigation: passes share the `DataBinder`'s existing forest; do not add fresh full-tree
    walks in the wrappers.

---

## 7. ISO feature work in this phase

**None new.** P6 is a pure rearchitecture phase — it adds no COBOL construct and changes no observable behavior.
Its spec obligation is **preservation**: the collapsed `SymbolTable` must exactly preserve the name-resolution
semantics of §8.4.6.2.1 rule 3a ("a method-local declaration REPLACES, never unions with, the object/program-level
name") and §11.7.4 GR5 (method sibling-invisibility / index-name privacy). No new goldens for language features are
required; the **new tests are structural**:

- `BindPipelineTests` — DAG validation accepts the standard manifest, rejects a mis-ordered one.
- `WatermarkTests` — reading a not-yet-produced capability trips the gate (Debug).
- `SymbolTableTests` — the five scope/shadowing cases enumerated in Step 7a, mirroring the existing
  `OoSpineTests` method-local-shadowing conformance cases (which remain the authoritative behavior net).
- A `CheckOnly` test proving a bind-clean/emit-failing program returns `Success` under `CheckOnly` and that
  `EmitBound` is not invoked.

The authoritative regression that P6 must keep green is the existing OO method-scope conformance corpus
(`OoSpineTests`) plus the full NIST 353-MATCH legacy guard — those are the spec-behavior net for the one
semantics-sensitive change (the lookup collapse).
