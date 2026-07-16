# PHASE 09 — M2 OO: subsystem rearchitecture (`Oo/` + `OoDriver`) + mandatory 2002 OO completion

- **Phase number / title:** P9 — M2 OO subsystem rearchitecture and 2002 OO completion
- **Track:** feature-iso
- **Risk:** HIGH (XL — the largest single feature milestone; touches Binding + CodeGen across four cooperating slices and ~4,400 LOC)
- **Depends on:** P4 (frontend consolidation / typed `Cst` façade + generated-namespace rename) and P7 (exhaustive visitor dispatch + binder/emitter god-class decomposition + `BinderContext`). Both MUST be DONE before starting: P9 relocates OO onto the `BinderContext` scoped-state mechanism P7 introduces and the visitor dispatch P7 generates, and it consumes the `Cst` façade / `CobolNet.Frontend.Generated` namespace P4 lands.
- **Goal (one paragraph):** Move all four OO slices (the pass-1 symbol table, per-method DATA binding, INVOKE/SELF/SUPER/FACTORY/universal statement binding, and OO emission) out of their scattered partial-file homes into ONE cohesive `Oo/` folder + `CobolNet.Compiler.Oo` namespace, fronted by a real `OoDriver` that owns the 8-step orchestration currently inlined inside `CSharpEmitter.CallEmitRunUnit`. Split the `OoClassTable` god-class into a pure symbol table + an `OoConformance` service; replace the four ambient mutable binder flags (`ActiveMethodScope` / `OoInFactory` / `OoCurrentClass` / `OoIsClassUnit`) with scoped push/pop on `BinderContext`; move emit-form facts to an emit-side `OoClassLayout` + one `NamingConvention`; make `OoMethodSymbol` phase-explicit by splitting its bound signature into an `OoMethodBinding` attached after data-bind. On that clean foundation, close the remaining mandatory COBOL-2002 OO surface: the multi-base `INHERITS` loud rejection (currently silently dropped), the `ANY LENGTH` clause (§13.18.2), the §4.2.2 conformance-checking suboption interface leg, exception objects via `RAISE identifier` with GLOBAL-walkable Format-3 declaratives, and re-land the 14 legacy `OoTests` as greenfield facts.
- **Exit criteria:**
  1. OO lives in ONE `src/Cobol.Net.Compiler/Oo/` folder + `CobolNet.Compiler.Oo` namespace; `OoDriver` owns the orchestration and **no OO pass runs inside CodeGen** (`CSharpEmitter.CallEmitRunUnit` calls `OoDriver` once and only renders bound facts).
  2. `OoClassTable` is a pure symbol table; conformance checking lives in `OoConformance`; `AdapterPairs` is a RETURN value, not a mutated public field.
  3. The four ambient binder flags are gone — replaced by scoped push/pop on `BinderContext` that guarantees reset.
  4. Emit-form facts (`OoStaticRootFields`, `OoStaticIndexCells`, the `__`/`::INST::`/`::FACT::`/`::EXT::` naming) live in `OoClassLayout` + `NamingConvention`, not in `DataBinder`.
  5. `OoMethodSymbol` is phase-explicit (immutable pass-1 identity; bound signature on a separately-attached `OoMethodBinding`).
  6. FACTORY / PROPERTY / INTERFACE-ID / universal-reference / EC-OO remain green with conformance goldens; multi-base `INHERITS` is rejected LOUDLY (new diagnostic + negative golden); `ANY LENGTH` and the §4.2.2 conformance interface leg land with `oo_*` conformance pairs + reject-at-85 matrix rows.
  7. The 14 `OoTests` are re-landed as greenfield facts (in `OoSpineTests` or a sibling).
  8. Full battery GREEN: greenfield conformance + unit + the FULL legacy guard (NIST 353 MATCH), with the emitted-C# characterization snapshots either byte-identical or reviewed-and-re-baselined for each intentional emit change.

- **STATUS:** `IN PROGRESS @ step 1` (2026-07-15)
  > The executing session updates this line to `IN PROGRESS @ step N` after each step and `DONE` at phase end. Keep the DEVLOG entry-per-commit discipline (`DEVLOG.md`, newest-first) and push every commit boundary (`feedback_fully_autonomous_push`).
  >
  > ⚠ **AS-BUILT DRIFT LEDGER (scouted 2026-07-15 — adapt every step to these seams, not the doc's stale anchors):**
  > P6/P7 already relocated much of what Steps 4/5/8 assume: the 8-step OO orchestration runs in
  > `Binding/BinderDriver.Bind` (:44–97) behind the `IOoBindHost`+`BindSession` seam (`CSharpEmitter` implements it;
  > `CallEmitRunUnit`/`CSharpEmitter.Call.cs` no longer exist); the harmonize is `StorageFormPass.
  > HarmonizeStorageCrossings` (still a fixed point — Step 5's single-pass goal stands, new home);
  > `QualifyClassFiles` lives on `BinderDriver` (:398); `MarkStoreAsImage` is GONE (P5's `ComputePromotedSet`);
  > OO emission is ALREADY split out to `CodeGen/Verbs/OoEmitter.cs` (659 lines — Step 8 is largely pre-done;
  > `CSharpEmitter.Oo.cs` is 172 lines of BIND bodies only, which Step 4 moves into `OoDriver`); OO statement
  > binding is `Binding/Procedure/Verbs/OoBinder.cs` (904 lines; NO `StatementBinder.Oo.cs`); `BinderContext` is at
  > `Binding/Procedure/BinderContext.cs` and ALREADY has the scoped push/pop (`EnterMethodScope`/`BindPositionScope`
  > governing `ActiveMethodScope`) — Step 6 EXTENDS it to `OoInFactory`/`OoCurrentClass`/`OoIsClassUnit` (writes:
  > `CSharpEmitter.Oo.cs:42,62,79,100,110,111`). Multi-base INHERITS read = `OoClassTable.cs:484`. The
  > `__GET_`/`__SET_` name builders are FOUR copies (both rosters in `OoClassTable` ~:512/:558 + `DataBinder.Oo.cs
  > :403,405` + `ReferenceResolver.cs:66-67`). `InstanceKeyField` is a `FileModel` property set in
  > `BinderDriver.QualifyClassFiles`, not a `DataBinder` field. `AdapterPairs` (mutated public list,
  > `OoClassTable.cs:43,:167`; read `OoEmitter.cs:85`) and the `OoMethodSymbol` sentinels (:864–894) are genuinely
  > un-started (Steps 2/3 as written). Confirm 0849 is free before minting (Step 10).

---

## 1. Preconditions checklist (verify BEFORE step 1)

Run these and confirm before touching code. If any fails, STOP — the phase's assumptions are invalid.

```bash
cd /e/CobolSharp
git submodule update --init --recursive        # specs/ISO_COBOL.md present
dotnet build CobolSharp.sln -v quiet            # clean build
dotnet test tests/Cobol.Net.Tests.Conformance --nologo   # green baseline
dotnet test tests/Cobol.Net.Tests.Unit --nologo          # green baseline
bash scripts/guard.sh                            # legacy NIST 353 MATCH (the differential net)
```

- **P4 done?** The generated parser namespace is `CobolNet.Frontend.Generated` (NOT `CobolSharp.Compiler.Generated`) and a typed `Cst` façade exists — both in place: the OO files (e.g. `OoClassTable.cs`) already `using CobolNet.Frontend.Generated;`, so the moves below inherit the P4 namespace.
- **P7 done?** `BinderContext` exists (scoped-state carrier), the per-verb binder collaborator classes exist, and a source-generated exhaustive `IBoundStatementVisitor<T>` dispatch replaced the hand-maintained `EmitStatement`/`BindStatementCore` god-switches. P9 pushes OO onto these seams; without them, steps 5–7 have nowhere to land.
- **Characterization harness (P0) present?** `tests/Cobol.Net.Tests.Characterization` with seeded `.g.cs` snapshots. Every OO move in this phase is behavior-preserving; the snapshot gate is how you prove it. If P0's harness is absent, seed OO snapshots first (see §5).

---

## 2. Rationale — the problems this phase fixes

Cited from the AS-IS OO survey (the "OO (object orientation) — cross-layer" dossier section) and the current code:

| # | Problem (severity) | Evidence (file:line) |
|---|---|---|
| R1 | **OO orchestration lives inside the emitter's run-unit driver, not a dedicated OO pass.** `CallEmitRunUnit` hand-sequences 8 OO steps (`OoBindInterfaceData` → `OoBindClassData` → `ValidateOverrideSignatures` → `ValidateImplements` → `OoBindClassBody` → `MarkStoreAsImage` → `OoHarmonizeOverrideCrossings` → `OoQualifyClassFiles` → `OoEmitClassUnit`) interleaved with program binding. (HIGH) | `CodeGen/CSharpEmitter.Call.cs:96–147,178–181` |
| R2 | **Temporal coupling via public-mutable ambient pass flags set cross-layer by the emitter.** The CodeGen layer WRITES `data.ActiveMethodScope`, `OoInFactory`, `OoCurrentClass`, `OoIsClassUnit` on the binder; a missed reset silently mis-binds a sibling method's names. (HIGH) | `Binding/DataBinder.Oo.cs:38–47`; `Binding/Bound/StatementBinder.Oo.cs:97–106,255–267` |
| R3 | **`OoClassTable` is a god-class:** symbol table + inter-class conformance validator + runtime-descriptor projector + covariant-adapter accumulator in one 923-LOC type; `AdapterPairs` is a public list mutated during validation. (HIGH) | `Binding/OoClassTable.cs:39,99–161,204–342` |
| R4 | **Emit-form facts computed and stored in the Binding layer.** `OoStaticRootFields`/`OoStaticIndexCells` + the `__`/`::INST::`/`::FACT::`/`::EXT::` naming live on `DataBinder`, read by the emitter. (HIGH) | `Binding/DataBinder.Oo.cs:52,63,197,229`; `CodeGen/CSharpEmitter.Oo.cs:184–208` |
| R5 | **`OoHarmonizeOverrideCrossings` is a compute-then-repair fixed-point pass** run from the emitter. (MEDIUM) | `CodeGen/CSharpEmitter.Oo.cs:674–700`; `Call.cs:121` |
| R6 | **`CSharpEmitter.Oo` bundles five codegen concerns in one 885-LOC partial** (class shell, factory singleton, interfaces, methods, INVOKE/universal/SET). (MEDIUM) | `CodeGen/CSharpEmitter.Oo.cs:364–505` |
| R7 | **`OoMethodSymbol` is filled across three phases** with `-1`/empty sentinels for the not-yet-bound signature — a too-early read is silent, not a type error. | `Binding/OoClassTable.cs` (`OoMethodSymbol`, 85 LOC) |
| R8 | **Duplicated `__GET_`/`__SET_` accessor-name construction in three sites.** (LOW) | `Binding/OoClassTable.cs:492–540`; `Binding/DataBinder.Oo.cs:421–425`; `Binding/ReferenceResolver.cs:66–68` |
| R9 | **Multi-base `INHERITS FROM` is silently dropped, not rejected.** `BaseName = id.className().Length > 1 ? id.className(1).GetText() : null` reads only the FIRST base; a program with 2+ bases compiles as if only the first existed — a silent-miscompile against SSOT §18 item 18 ("reject 2+ bases LOUDLY") and A.4.10. | `Binding/OoClassTable.cs:465` |
| R10 | **`ANY LENGTH` (§13.18.2) is absent** — no grammar, binder, or emitter reach (grep returns nothing). A mandatory 2002 conformance item. | (no occurrences) |

The rearchitecture (R1–R8) MUST land FIRST and behavior-neutral; the feature completion (R9, R10, the §4.2.2 interface leg, RAISE-identifier exception objects) lands on the clean foundation so each new rule has ONE home. This matches the target topology (`DESIGN-module-topology.md`: "Move all four OO slices into `Oo/` (`CobolNet.Compiler.Oo`): `OoClassTable` split into pure symbol table + `OoConformance`; `OoDriver` owns the 8-step orchestration; emit facts move to `OoClassLayout`; ambient binder flags become scoped push/pop on `BinderContext`").

**Important reality check (do not re-implement what exists):** FACTORY, PROPERTY (declarations + references), INTERFACE-ID / IMPLEMENTS, universal reference (`__CobolInvoke`), and EC-OO (RAISE / GOBACK RAISING / EXCEPTION-OBJECT) are ALREADY LANDED and byte-exact (goldens `oo_factory`, `oo_property*`, `oo_interface*`, `oo_universal*`, `oo_ec_*` all present under `tests/conformance/2002/`). This phase's "completion" work is (a) the rearchitecture that gives those features a clean home, and (b) the genuinely-missing edges: multi-base loud reject, `ANY LENGTH`, the §4.2.2 conformance suboption interface leg, RAISE-identifier exception-object construction if not already complete, and re-landing the 14 legacy `OoTests`. Verify each "missing" item against the code before implementing (`feedback_spec_scopes_not_tests`: implement to spec, but do NOT re-build a landed feature).

---

## 3. Target end-state (concrete file/class inventory when P9 is DONE)

New folder `src/Cobol.Net.Compiler/Oo/`, namespace `CobolNet.Compiler.Oo`:

```
src/Cobol.Net.Compiler/Oo/
  OoDriver.cs            — the 8-step orchestrator. Entry: OoBind(compilation, BinderContext, EditionContext) → OoModel.
                           Owns: bind interface data → bind class/factory data → validate override signatures →
                           validate implements → bind class bodies → harmonize crossings (single-pass decision) →
                           qualify class files. Returns an OoModel (classes+interfaces+bound facts). NO emission.
  OoClassTable.cs        — PURE symbol table: _byName/_ifaceByName, Classes, Interfaces, Find/FindInterface,
                           roster access, INHERITS/IMPLEMENTS closure walks. NO validation, NO adapter list,
                           NO descriptor projection.
  OoConformance.cs       — the conformance SERVICE: ValidateOverrideSignatures, ValidateImplements (RETURNS
                           IReadOnlyList<AdapterPair>), DescriptionMismatch, ConformanceDescriptor,
                           ObjectRefWideningMismatch, the §9.3.8.2 / §9.3.11 / §4.2.2-suboption interface leg.
  OoClassSymbol.cs       — one class: identity, Base link, IReadOnlyList<string> Bases (the FULL INHERITS list,
                           so multi-base is representable and rejectable), rosters, chain walks. Immutable after Build.
  OoInterfaceSymbol.cs   — one interface: prototype roster + AllPrototypes() INHERITS closure.
  OoMethodSymbol.cs      — pass-1 IMMUTABLE identity ONLY (Name, CsName, Accessor, HasOverride, IsFinal, IsAnyLength).
  OoMethodBinding.cs     — the AFTER-data-bind signature: Formals, Returning, EntryPc, EndPc, LinkageRoots,
                           LocalRoots, StaticRoots, OverrideOf. Attached to OoMethodSymbol post-bind; reading it
                           before it is attached is a null-deref (type error), not a silent -1.
  OoMethodDataBinder.cs  — per-method DATA binding (was DataBinder.Oo.cs): LINKAGE→params, LOCAL-STORAGE→locals,
                           method-WS→statics, §11.7 GR5 shadowing, PROPERTY accessor synthesis, compiler temps.
                           Consumes BinderContext scope; NO ambient flags.
  OoStatementBinder.cs   — INVOKE/SET-objref/method-GOBACK/EXIT-METHOD/universal/property-desugar (was
                           StatementBinder.Oo.cs). A per-verb binder collaborator (P7 shape) over BinderContext.
  OoClassLayout.cs       — EMIT-side facts: StaticRootFields, StaticIndexCells, InstanceKeyField,
                           CallSuppressedRootFields — computed from bound facts, owned by CodeGen, NOT DataBinder.
  NamingConvention.cs    — the ONE home for every OO C# name convention: ClassCsName, FactoryCsName,
                           GetAccessorName/SetAccessorName (kills the 3 __GET_/__SET_ copies), the
                           ::INST::/::FACT::/::EXT:: file-key prefixes, __outer/__Instance/__New.
```

New folder `src/Cobol.Net.Compiler/CodeGen/Oo/` (emission renderers, namespace `CobolNet.CodeGen.Oo` — parallels the P7 emitter decomposition):

```
src/Cobol.Net.Compiler/CodeGen/Oo/
  OoClassEmitter.cs      — the class shell + factory singleton + instance/factory fields (was CSharpEmitter.Oo.cs
                           class-shell part). Renders OoModel + OoClassLayout facts.
  OoMethodEmitter.cs     — method-body emission (the PC-dispatch slice into a method).
  OoInvokeEmitter.cs     — INVOKE / SET object-reference rendering (typed fast path).
  OoUniversalEmitter.cs  — the __CobolInvoke switch + universal unbox/rebox (was the universal-dispatch part).
  OoInterfaceEmitter.cs  — C# interface emission + the covariant adapter explicit-implementations.
```

`DataBinder` loses its `.Oo.cs` partial and all `Oo*` public fields. `StatementBinder` loses its `.Oo.cs` partial. `CSharpEmitter` loses its `.Oo.cs` partial and the OO orchestration inside `CallEmitRunUnit` — which becomes a single call: `var oo = _ooDriver.OoBind(...); ... _ooEmit.Emit(oo, w);`.

**Diagnostic band unchanged where landed** (0813, 0820–0848 stay their current codes so goldens/matrix rows do not churn); NEW rejections get NEW codes in the OO band (see §7).

---

## 4. STEP-BY-STEP

> Ordering principle: rearchitecture FIRST (pure moves + mechanical splits, each behavior-neutral and snapshot-gated), THEN feature completion. Every numbered step names exact paths, the change, the WHY, the verify command + expected result, and whether it is a COMMIT BOUNDARY. Keep the battery green at every boundary. Work ONE step at a time (`feedback_iterate_one_at_a_time`).

### Part A — Rearchitecture (behavior-neutral; snapshot-gated)

#### Step 1 — Create the `Oo/` folder and move the symbol-table types verbatim (namespace flip only)
- **Files:** create `src/Cobol.Net.Compiler/Oo/`. MOVE `Binding/OoClassTable.cs` → `Oo/OoClassTable.cs`. Extract the nested/sibling types `OoClassSymbol`, `OoInterfaceSymbol`, `OoMethodSymbol` into their own files `Oo/OoClassSymbol.cs`, `Oo/OoInterfaceSymbol.cs`, `Oo/OoMethodSymbol.cs` (one public type per file, per the topology rule "file name = its single public type").
- **Change:** namespace `CobolNet.Binding` → `CobolNet.Compiler.Oo`. Add `using CobolNet.Binding;` where `DataItem`/`PicInfo`/`Place` are referenced. Do NOT split responsibilities yet — pure move + namespace. Update every consumer's `using` (grep `OoClassTable`, `OoClassSymbol`, `OoInterfaceSymbol`, `OoMethodSymbol`): `DataBinder.cs`, `DataBinder.Oo.cs`, `DataBinder.Ptr.cs`, `StatementBinder.Oo.cs`, `StatementBinder.cs`, `ReferenceResolver.cs`, `OdoModel.cs`, `CSharpEmitter.Call.cs`, `CSharpEmitter.Oo.cs`, `tests/.../OoSpineTests.cs`.
- **Why:** establishes the target namespace with zero behavior change so the risky splits land against a stable home (R3 prep).
- **Verify:** `dotnet build CobolSharp.sln -v quiet` (0 errors) → `dotnet test tests/Cobol.Net.Tests.Conformance --filter OoSpineTests` green → `dotnet test tests/Cobol.Net.Tests.Characterization` snapshots byte-identical (no `.g.cs` diff — this is a pure move, emission is unchanged).
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 1 — move OO symbol-table types into Oo/ folder + CobolNet.Compiler.Oo namespace (behavior-neutral)`

#### Step 2 — Split `OoClassTable` → pure symbol table + `OoConformance` service (return `AdapterPairs`)
- **Files:** create `Oo/OoConformance.cs`. From `Oo/OoClassTable.cs` MOVE: `ValidateOverrideSignatures`, `ValidateImplements`, `DescriptionMismatch`, `ConformanceDescriptor`, `ObjectRefWideningMismatch`, and the closure helpers those need for validation. `OoClassTable` keeps `Build`, `Find`/`FindInterface`, `Classes`/`Interfaces`, rosters, `AllPrototypes`, INHERITS/IMPLEMENTS closure walks.
- **Change:** `OoConformance` takes the `OoClassTable` + `EditionContext` (or the P7 `DiagnosticSink`). `ValidateImplements` **returns `IReadOnlyList<AdapterPair>`** instead of mutating `OoClassTable.AdapterPairs`; delete the public `AdapterPairs` field from `OoClassTable`. Introduce `public readonly record struct AdapterPair(OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory);` in `Oo/OoConformance.cs`. The orchestrator (step 4) threads the returned list to the interface emitter.
- **Why:** R3 — a symbol table should not validate or accumulate emit state; a mutated public list is a shared-blackboard hazard.
- **Verify:** build + `OoSpineTests` (the 0829/0841 override/implements facts) green; `oo_interface_covariant.out` byte-exact (the adapter path). Characterization snapshots byte-identical.
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 2 — extract OoConformance service; AdapterPairs is a return value not a mutated field (R3)`

#### Step 3 — Make `OoMethodSymbol` phase-explicit (`OoMethodBinding`)
- **Files:** `Oo/OoMethodSymbol.cs`, create `Oo/OoMethodBinding.cs`.
- **Change:** `OoMethodSymbol` keeps only immutable pass-1 identity (`Name`, `CsName`, `Accessor`, `HasOverride`, `IsFinal`; add `IsAnyLength` = false for now, wired in step 12). MOVE the after-data-bind fields (`Formals`, `Returning`, `EntryPc`, `EndPc`, `LinkageRoots`, `LocalRoots`, `StaticRoots`, `OverrideOf`) to `OoMethodBinding`. Add `public OoMethodBinding? Binding { get; internal set; }` on `OoMethodSymbol`; `OoMethodDataBinder` sets it after binding the method's data. Every reader of the old sentinel fields (`EntryPc == -1`) becomes `sym.Binding!.EntryPc` — a null-deref if read too early, which is the point (R7).
- **Why:** R7 — the `-1`/empty sentinels hid ordering bugs; a separate object makes "signature not yet bound" a type-level fact.
- **Verify:** build + `OoSpineTests` green (method PC-dispatch traps #4/#7). Snapshots byte-identical.
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 3 — OoMethodSymbol phase-explicit; bound signature on OoMethodBinding (R7)`

#### Step 4 — Introduce `OoDriver` owning the 8-step orchestration (lift it OUT of `CSharpEmitter.CallEmitRunUnit`)
- **Files:** create `Oo/OoDriver.cs`. Edit `CodeGen/CSharpEmitter.Call.cs`.
- **Change:** `OoDriver.OoBind(IReadOnlyList<ClassUnit> classes, OoClassTable table, BinderContext ctx, EditionContext edition) → OoModel`. Move `CallEmitRunUnit` lines 98–102, 119 (the class-side `MarkStoreAsImage` calls stay coordinated with the data-model pass from P5/P6 — see note), 121, 147 into `OoDriver`: bind interface data → bind class/factory data → `OoConformance.ValidateOverrideSignatures` → `adapters = OoConformance.ValidateImplements` → bind class bodies → harmonize crossings (single-pass, step 5) → qualify class files. `OoDriver` returns `OoModel { Classes, Interfaces, Adapters, ClassLayout }`. `CallEmitRunUnit` now calls `var oo = _ooDriver.OoBind(...)` once, then hands `oo` to the emitters (step 8). Create `Oo/OoModel.cs` as the immutable result record.
- **Note on `MarkStoreAsImage`:** the cross-layer `StoreAsImage` write is being removed by the P5 `StorageForm` pass. If P5 has already deleted `MarkStoreAsImage`, there is nothing to move — `OoDriver` consumes the already-computed storage form. If P5 has NOT landed, keep the class-side `MarkStoreAsImage(cls.Data)/(cls.FactoryData)` call INSIDE `OoDriver` (still behavior-neutral) and leave a `// TODO(P5): StorageForm pass owns this` marker. Do not regress the flag deletion.
- **Why:** R1 — OO orchestration must be a real pass, not emitter control flow; makes it unit-testable and removes OO pass-ordering from CodeGen.
- **Verify:** build + FULL `OoSpineTests` + the whole `oo_*` corpus (`dotnet test tests/Cobol.Net.Tests.Conformance` — CorpusRunnerTests runs every enabled `oo_*.out`). All byte-exact. Snapshots byte-identical (orchestration order preserved).
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 4 — OoDriver owns the 8-step OO orchestration; CodeGen calls it once (R1)`

#### Step 5 — Fold `OoHarmonizeOverrideCrossings` into a single-pass crossing decision inside `OoDriver`
- **Files:** `Oo/OoDriver.cs` (absorbing the logic from `CodeGen/CSharpEmitter.Oo.cs:674–700`).
- **Change:** replace the compute-then-repair fixed point with a single per-override-family decision computed once (walk each override chain; pick the crossing form once from the family's root signature; stamp it on `OoMethodBinding.CrossingForm`). Delete `OoHarmonizeOverrideCrossings` from the emitter and the `Call.cs:121` call.
- **Why:** R5 — a fixed-point repair over a shared mutable state is fragile; the crossing form is determinable in one pass.
- **Verify:** build + `oo_inherit`, `oo_override_final`, `oo_interface_covariant` byte-exact. Snapshots byte-identical (the emitted override signatures must be identical — this is the highest-risk neutrality check; diff `.g.cs` explicitly).
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 5 — single-pass override-crossing decision in OoDriver (R5)`

#### Step 6 — Replace the four ambient binder flags with scoped push/pop on `BinderContext`
- **Files:** `Binding/BinderContext.cs` (from P7), `Oo/OoMethodDataBinder.cs`, `Oo/OoStatementBinder.cs`, and every reader of `ActiveMethodScope`/`OoInFactory`/`OoCurrentClass`/`OoIsClassUnit`.
- **Change:** add to `BinderContext` an immutable `OoScope` value (`MethodScope? Method`, `bool InFactory`, `OoClassSymbol? CurrentClass`, `bool IsClassUnit`) exposed via a `using` disposable: `using (ctx.EnterMethod(scope)) { … }`, `using (ctx.EnterFactory()) { … }`, `using (ctx.EnterClass(sym, isClassUnit)) { … }` — each pushes and, on `Dispose`, restores the prior value (so a missed reset is impossible by construction). Delete the four `public … { get; set; }` properties from `DataBinder`/`StatementBinder`. Consumers read `ctx.Oo.Method` etc. The post-build passes (`OdoResolve`, `ResolveRedefines`) that today read `ActiveMethodScope == null` and fall back to `OoRootOwner` now consult `ctx.Oo` or the `OoRootOwner` map explicitly (keep `OoRootOwner` — it is a legitimate item→method map, not an ambient flag).
- **Why:** R2 — the ambient flags are the OO subsystem's single worst temporal-coupling hazard (the emitter mutating binder state mid-orchestration). Scoped push/pop makes lifetime structural.
- **Verify:** build + FULL `oo_*` corpus + `OoSpineTests` (esp. the per-method-scope traps #4/#5/#6/#10 — sibling-method name invisibility). Snapshots byte-identical.
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 6 — scoped push/pop OO state on BinderContext; delete the 4 ambient flags (R2)`

#### Step 7 — Move emit-form facts to `OoClassLayout` + centralize names in `NamingConvention`
- **Files:** create `Oo/OoClassLayout.cs`, `Oo/NamingConvention.cs`. Edit `DataBinder.Oo.cs` (→ `Oo/OoMethodDataBinder.cs`), `CodeGen/CSharpEmitter.Oo.cs`, `ReferenceResolver.cs`.
- **Change:** MOVE `OoStaticRootFields`, `OoStaticIndexCells`, `InstanceKeyField`, `CallSuppressedRootFields` off `DataBinder` onto `OoClassLayout` (computed by `OoDriver` from bound facts, consumed by the emitters). Create `NamingConvention` with `GetAccessorName(prop)`/`SetAccessorName(prop)` (delete the 3 duplicate `__GET_`/`__SET_` builders at `OoClassTable.cs:492–540`, `DataBinder.Oo.cs:421–425`, `ReferenceResolver.cs:66–68`), plus `ClassCsName`, `FactoryCsName`, and the `::INST::`/`::FACT::`/`::EXT::`/`__outer`/`__Instance`/`__New` constants (single-source the strings scattered across `CSharpEmitter.Oo.cs` and `Call.cs:140–147`).
- **Why:** R4 + R8 — the Binding layer must not own emit facts; three copies of an accessor name is a drift hazard.
- **Verify:** build + FULL `oo_*` corpus + `oo_property*` goldens byte-exact (accessor names are load-bearing in the emitted C#). Snapshots byte-identical.
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 7 — OoClassLayout owns emit facts; NamingConvention single-sources OO names (R4/R8)`

#### Step 8 — Split `CSharpEmitter.Oo.cs` into `CodeGen/Oo/` renderer classes
- **Files:** create `CodeGen/Oo/OoClassEmitter.cs`, `OoMethodEmitter.cs`, `OoInvokeEmitter.cs`, `OoUniversalEmitter.cs`, `OoInterfaceEmitter.cs`. Retire `CodeGen/CSharpEmitter.Oo.cs`.
- **Change:** move each of the five concerns (class shell/factory singleton, method bodies, INVOKE/SET, universal `__CobolInvoke`, interface + adapters) into its own renderer over the P7 `EmitContext` (immutable) + `OoModel`. `CallEmitRunUnit`'s `OoEmitClassUnit`/`OoEmitInterfaceUnit` calls become `_ooClassEmitter.Emit(oo, cls, w)` etc.
- **Why:** R6 — an 885-LOC partial doing five jobs; the topology target is per-concern renderer classes.
- **Verify:** build + FULL `oo_*` corpus byte-exact. Snapshots byte-identical (this is a mechanical move of emission code; ANY `.g.cs` diff here is a bug — investigate before re-baselining).
- **COMMIT BOUNDARY.** `refactor(cobolnet): P9 step 8 — split OO emission into CodeGen/Oo/ renderers (R6); Oo/ rearchitecture COMPLETE`

> After step 8, the OO subsystem is fully re-homed and behavior-neutral. Run the **full battery** (§5) and confirm green before starting Part B. Update the STATUS line to `IN PROGRESS @ step 9`.

### Part B — Mandatory 2002 OO completion (feature work; goldens + matrix rows + negatives)

#### Step 9 — Re-land the 14 legacy `OoTests` as greenfield facts
- **Files:** `tests/Cobol.Net.Tests.Conformance/OoSpineTests.cs` (or a new sibling `OoPortedTests.cs`). Source of truth: `tests/CobolSharp.Tests.Integration/OoTests.cs` (14 `[Fact]`/`[Theory]`).
- **Change:** port each legacy `OoTests` case (they assert real caught legacy bugs — multi-method fall-through, two-object independence, SELF/SUPER dispatch, override signature, etc.) to compile+run through `CobolNet.CompilerDriver.Compile` at `--std 2002` and byte-compare stdout (reuse `OoSpineTests.CompileAndRun` / `CutRunner`). Any that duplicate an existing `OoSpineTests` fact or an enabled corpus program (`oo_instance_data`/`oo_object_group` already cover trap #1) — note the coverage and skip the duplicate rather than double-assert. Do NOT delete the legacy `OoTests.cs` (it is the frozen oracle until G8/P15).
- **Why:** exit criterion 7 — the 14 OO regression guards must survive the legacy retirement.
- **Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance --filter Oo` green; count the re-landed facts (expect 14 minus documented duplicates).
- **COMMIT BOUNDARY.** `test(cobolnet): P9 step 9 — re-land the 14 legacy OoTests as greenfield facts`

#### Step 10 — Multi-base `INHERITS FROM` loud rejection (R9; SSOT §18 item 18; A.4.10)
- **Spec:** ISO §11.3.2 (`CLASS-ID … INHERITS FROM object-class-name-2 …` permits multiple bases). SSOT §18 item 18 + `COBOLNET_OO_DESIGN.md` "SETTLED": v1 restricts to single inheritance and rejects 2+ bases LOUDLY (multiple inheritance / parametric polymorphism rejected per A.4.10).
- **Files:** `Oo/OoClassSymbol.cs`, `Oo/OoClassTable.cs` (the `Build` reader at the current `OoClassTable.cs:465`).
- **Change:** capture the FULL base list: `Bases = [.. id.className().Skip(1).Select(c => c.GetText())]` on `OoClassSymbol`; keep `BaseName` = `Bases.Count >= 1 ? Bases[0] : null` for the single-base path. In `Build` (or `OoConformance`), if `Bases.Count > 1` emit a NEW diagnostic **COBOLNET0849** (next free code in the OO band; confirm 0849 is unused — the band today runs 0813, 0820–0848): `"class '{name}': INHERITS FROM {n} base classes — COBOL.NET v1 supports single inheritance only; multiple inheritance is rejected (ISO §11.3.2; A.4.10)."` Register 0849 in the diagnostic registry (P2/P7) and in `constructs.json` if the edition framework tracks it.
- **Why:** R9 — today the 2nd+ base is silently dropped, a spec-violating silent miscompile.
- **Verify (negative golden):** create `tests/conformance/negative/oo-multi-base-inherits.cob` (a class with `INHERITS FROM A B`) + `.err` containing `COBOLNET0849`; add it to `tests/conformance/negative/manifest.json` (enabled at 2002/2014/2023). Add an `OoSpineTests` `[Fact]` asserting the diagnostic. `dotnet test tests/Cobol.Net.Tests.Conformance --filter CorpusRunner` green (the negative corpus inverts the contract — it MUST fail with that code).
- **COMMIT BOUNDARY.** `feat(cobolnet): P9 step 10 — reject multi-base INHERITS FROM loudly (COBOLNET0849; §11.3.2 / A.4.10)`

#### Step 11 — `ANY LENGTH` clause (§13.18.2)
- **Spec:** ISO §13.18.2 — `ANY LENGTH` on an elementary alphanumeric/national item in the LINKAGE SECTION of a method/program; the item's length is that of the corresponding argument at run time (COBOL-2002; reject at 85). This is the mandatory 2002 item flagged in the phase scope.
- **Files:**
  - Grammar: `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` — add the `ANY LENGTH` clause to the data-description entry, `{is2002()}?`-gated (per `feedback_grammar_version_factoring`; log + full legacy guard). Add the `ANY`/`LENGTH` tokens if absent (LENGTH exists; add `ANY` reserved-word handling). Regenerate (`GenerateIfNewer.ps1`) and keep the FULL legacy guard green.
  - Binder: `Oo/OoMethodDataBinder.cs` + `Binding/DataBinder.cs` (BindEntry) — set `DataItem.IsAnyLength`; validate it is elementary alnum/national, LINKAGE-only, and that no fixed length is also stated (SR checks → new **COBOLNET084A**/next code, or reuse the appropriate SR band). Propagate `IsAnyLength` onto `OoMethodSymbol.IsAnyLength` for the method's formal.
  - Emitter: the LINKAGE→`ref` parameter becomes length-agnostic (the runtime `CobolString` carrier already length-agnostic; the emitted `FUNCTION LENGTH`/reference-mod reads the actual argument length). Verify the emitted method signature and any `LENGTH OF` intrinsic over an ANY-LENGTH item resolves to the runtime length, not a compile-time constant.
- **Why:** mandatory 2002 OO/interop surface; absent today (R10).
- **Verify (positive golden):** `tests/conformance/2002/oo_any_length.cob` — a method taking an `ANY LENGTH` LINKAGE item, called with two different-length arguments, `DISPLAY FUNCTION LENGTH OF the-item` proving the length tracks the argument; `.out` with both lengths. Add to `tests/conformance/2002/manifest.json` enabled. **Reject-at-85 matrix row:** `tests/conformance/negative/any-length-at-85.cob` + `.err` (`ANY LENGTH` at `--std 85` → the introduction diagnostic COBOLNET0900/0901); manifest entry naming edition 85. Update `docs/VERSION_CHANGE_REFERENCE.md` (or the generated VCR) with the introduction row.
- **Verify commands:** `dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/2002/oo_any_length.cob --std 2002 -o /e/tmp/al.dll --run` → matches `.out`; then `--std 85` → the introduction diagnostic; `dotnet test tests/Cobol.Net.Tests.Conformance`; `bash scripts/guard.sh` (grammar change ⇒ FULL legacy guard, per `feedback_autonomous_grammar_nist`).
- **COMMIT BOUNDARY.** `feat(cobolnet): P9 step 11 — ANY LENGTH clause (§13.18.2, 2002); oo_any_length golden + reject-at-85 row`

#### Step 12 — §4.2.2 conformance-checking suboption — interface leg
- **Spec:** ISO §4.2.2 (the conformance-checking / arithmetic + interface-conformance suboptions) — the interface leg: strict interface-conformance checking of INVOKE argument/return descriptions against the resolved method signature (§14.8.2 STRICT). Much of this is already the binder-authoritative §9.3.8.2/§9.3.11 pass; this step closes the SUBOPTION surface — the interface conformance-checking mode is selectable and its diagnostics are complete for the interface (prototype) leg.
- **Files:** `Oo/OoConformance.cs` (extend `ValidateImplements`/the conformance descriptor path), `Validation/EditionValidator.cs`/the edition framework (register the suboption), and the CLI/`Options` if a `--conformance` suboption switch is exposed (confirm with `COMPLETION_ROADMAP_COUNCIL.md` whether the switch is in scope; if not, land the interface-leg CHECKS unconditionally under STRICT and defer the selectable suboption).
- **Change:** ensure every INVOKE over an interface-typed receiver checks the argument descriptions against the resolved prototype (`AllPrototypes()` closure) and returns the correct 0841/0828-band diagnostic on mismatch; add the interface-leg cases the existing pass does not cover (e.g. prototype RETURNING description mismatch on an interface-typed call, not just class-typed).
- **Why:** the phase's named §4.2.2 interface leg — completes the four-compilers-in-one interface conformance obligation.
- **Verify:** `tests/conformance/2002/oo_interface_conformance.cob` (positive — a conforming interface call) + a `tests/conformance/negative/oo-interface-arg-mismatch.cob` + `.err` (non-conforming argument → the conformance diagnostic). Manifest entries; `dotnet test tests/Cobol.Net.Tests.Conformance`.
- **COMMIT BOUNDARY.** `feat(cobolnet): P9 step 12 — §4.2.2 conformance-checking interface leg; oo_interface_conformance golden + negative`

#### Step 13 — Exception objects: `RAISE identifier` + GLOBAL-walkable Format-3 declaratives (verify/complete EC-OO)
- **Spec:** ISO §14.6.13 (RAISE / RESUME), §11.7 (exception objects), §14.9 declaratives Format-3 (USE … EXCEPTION OBJECT), §11.4 GLOBAL. NOTE: `oo_ec_raise_object.cob`, `oo_ec_goback_raising.cob` goldens already exist — much of EC-OO is landed. This step VERIFIES completeness and closes any gap in exception-OBJECT construction (`RAISE identifier` where identifier is an object reference) and GLOBAL-walkable F3 declaratives (a contained program's F3 handler reaching a GLOBAL exception object).
- **Files:** `Oo/OoStatementBinder.cs` (RAISE binding), the EC bridge in the emitter, declaratives handling. Check `StatementBinder.Oo.cs` current RAISE handling and the `RAISE NULL`/`RAISE N4` → 0848 negatives already in `OoSpineTests:2297–2299`.
- **Change:** if `RAISE identifier` with an object-reference operand already constructs and propagates the exception object (verify via the existing golden), and GLOBAL F3 declaratives already walk correctly, this step is a VERIFY-ONLY confirmation (add a golden that exercises the GLOBAL-walk if absent). If a gap exists (e.g. an object exception raised in a contained program not caught by a container's GLOBAL F3), fix it and add the golden.
- **Why:** phase scope names "EC-OO + exception objects (RAISE identifier) + GLOBAL-walkable F3 declaratives" — confirm the mandatory surface is complete, not partially landed.
- **Verify:** `tests/conformance/2002/oo_ec_global_f3.cob` (new, if the GLOBAL-walk is not already covered) + `.out`; existing `oo_ec_*` goldens stay byte-exact. `dotnet test tests/Cobol.Net.Tests.Conformance`.
- **COMMIT BOUNDARY.** `feat(cobolnet): P9 step 13 — EC-OO exception-object completeness: RAISE identifier + GLOBAL-walkable F3 (§14.6.13/§11.7)`

#### Step 14 — GOBACK-vs-STOP-RUN bind-time split verification + 2023 GOBACK status-phrase decision
- **Spec:** ISO §14.6 GOBACK / §14.6.9 STOP; the D8 `BoundMethodReturn` vs `BoundStopRun` split. COBOL-2023 adds a GOBACK status phrase.
- **Files:** `Oo/OoStatementBinder.cs` (method GOBACK/EXIT METHOD), `Binding/Bound/StatementBinder.cs` (BindGoback).
- **Change:** confirm `BoundMethodReturn` (method context) and `BoundStopRun` (run-unit) are distinct bound nodes and that the emitter renders a method `return` vs the run-unit stop (D8 is landed per `COBOLNET_OO_DESIGN.md`). For the **2023 GOBACK status phrase**: either implement it (if in scope per `COMPLETION_ROADMAP_COUNCIL.md`) with a 2014-vs-2023 continuity matrix row, OR explicitly DEFER it with a `// DEFERRED(P13-2023): GOBACK status phrase` marker and a note in `docs/ISO2023_CONFORMANCE_PLAN.md`. The phase scope permits "explicitly deferring it" — but the deferral must be RECORDED, not silent.
- **Why:** phase scope names the GOBACK/STOP-RUN split and the 2023 status phrase; the split is the OO "only correctness blocker" (`COBOLNET_OO_DESIGN.md`).
- **Verify:** `OoSpineTests` D8 control-flow facts (method GOBACK vs STOP RUN vs EXIT METHOD) green; if deferring 2023, add a `pending` matrix row rather than a passing test.
- **COMMIT BOUNDARY.** `feat(cobolnet): P9 step 14 — verify D8 GOBACK/STOP-RUN split; record 2023 GOBACK status-phrase disposition`

---

## 5. Verification — the full battery at phase end

Run ALL of these; each must be green (or reviewed-and-re-baselined for intentional emit changes):

```bash
cd /e/CobolSharp
dotnet build CobolSharp.sln -v quiet
dotnet test tests/Cobol.Net.Tests.Conformance --nologo    # OoSpineTests + CorpusRunnerTests (every oo_*.out) + negatives
dotnet test tests/Cobol.Net.Tests.Unit --nologo
dotnet test tests/Cobol.Net.Tests.Characterization --nologo   # emitted-C# snapshots (gate 3)
bash scripts/guard.sh                                      # FULL legacy differential — NIST 353 MATCH (grammar changed in step 11)
```

Behavior-neutrality / byte-exactness checks:
- **Steps 1–8 (rearchitecture):** the characterization `.g.cs` snapshots MUST be byte-identical — these steps change zero emitted output. A snapshot diff on a rearchitecture step is a BUG; investigate before re-baselining (do NOT `COBOLNET_UPDATE_SNAPSHOTS=1` past a rearchitecture step without proving the diff is spurious).
- **Steps 10–14 (features):** new goldens under `tests/conformance/2002/` (positive) and `tests/conformance/negative/` (reject-at-85 / mismatch), each byte-compared by `CorpusRunnerTests`; snapshot re-baselines allowed WITH review (gate-3 re-baseline is fine when gate-1 output goldens prove correctness).
- **Every enabled `oo_*` corpus program** (`oo_hello`, `oo_inherit`, `oo_self`, `oo_super`, `oo_factory*`, `oo_property*`, `oo_interface*`, `oo_universal*`, `oo_ec_*`, `oo_method_*`, `oo_object_*`, `oo_override_final`) stays byte-exact against its `.out`.
- **The battery count** must not regress from the P8-exit baseline (≥2028 greenfield conformance + 213 unit) except for net-new tests added here (the 14 ported OoTests + the step-10/11/12/13 goldens).

CLI spot-checks (real behavior, per the prompt's prebuilt CLI):
```bash
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/2002/oo_factory.cob --std 2002 -o /e/tmp/f.dll --run   # matches oo_factory.out
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/2002/oo_any_length.cob --std 2002 -o /e/tmp/al.dll --run # step 11
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/negative/oo-multi-base-inherits.cob --std 2002 -o /e/tmp/m.dll  # COBOLNET0849, exit 65
```

---

## 6. Rollback / resumability

- **Resume point:** the STATUS line at the top of this doc records `IN PROGRESS @ step N`. On resume, read it, then `git log --oneline` — each step above is a COMMIT BOUNDARY with the shown message, so the last commit tells you the last completed step. Re-run the step-N verify command to confirm the tree is clean, then start step N+1.
- **Each step is independently green** and independently revertable (`git revert <sha>`). Because the rearchitecture steps (1–8) are behavior-neutral and snapshot-gated, a bad step is caught by the characterization gate at THAT commit, not later.
- **Interrupted mid-step:** the moves (steps 1, 8) are large multi-file edits — if the build is red mid-step, `git stash` or `git checkout -- .` to the last commit and restart the step (the moves are mechanical and idempotent). Do not commit a red build.
- **Risks + mitigations:**
  - *Step 5 (override crossing) and step 6 (scoped state) are the highest-behavior-risk rearchitecture steps* — the emitted override signatures and per-method name shadowing are subtle. Mitigation: diff the `.g.cs` snapshots explicitly (not just pass/fail) on `oo_inherit`/`oo_interface_covariant`/`oo_method_redefines_*`; any diff is investigated before the commit.
  - *Step 11 (grammar change)* — a `.g4` edit needs the FULL legacy guard (`scripts/guard.sh`, NIST 353 MATCH), not just the fast guard, and java+pwsh present for ANTLR regen (`feedback_commit_generated_parser`). Mitigation: run the full guard at the step-11 boundary and confirm 353 MATCH before committing.
  - *P4/P7 drift* — if P4/P7 changed `BinderContext`/emitter shapes after this doc was written, steps 4/6/8 must adapt to the AS-BUILT seams. Mitigation: re-read `BinderContext.cs` and the P7 emitter decomposition at step 4 before lifting orchestration.
  - *Re-implementing landed features* — FACTORY/PROPERTY/INTERFACE/universal/EC-OO already exist. Mitigation: steps 12–14 START by verifying the existing goldens and reading the current code; only fill genuine gaps (`feedback_no_workarounds_root_cause` — do not rebuild working code).

---

## 7. ISO feature work in this phase (spec sections, editions, tests)

| Step | Feature | Spec § | Edition (intro) | Reject-at | Conformance test(s) | Diagnostic |
|---|---|---|---|---|---|---|
| 10 | Multi-base INHERITS loud reject | §11.3.2; A.4.10; SSOT §18 #18 | n/a (restriction) | any (2002+) | `negative/oo-multi-base-inherits` (+ OoSpineTests fact) | **COBOLNET0849** (new) |
| 11 | `ANY LENGTH` clause | §13.18.2 | 2002 | 85 | `2002/oo_any_length` (positive) + `negative/any-length-at-85` | intro 0900/0901; SR check (new **084A** or SR band) |
| 12 | §4.2.2 conformance interface leg | §4.2.2, §14.8.2, §9.3.8.2/§9.3.11 | 2002 | 85 | `2002/oo_interface_conformance` + `negative/oo-interface-arg-mismatch` | 0841/0828 band |
| 13 | EC-OO exception objects / RAISE identifier / GLOBAL F3 | §14.6.13, §11.7, §14.9 F3 | 2002 | 85 | `2002/oo_ec_global_f3` (if gap) + existing `oo_ec_*` | 0848 band |
| 14 | GOBACK/STOP-RUN split; 2023 GOBACK status phrase | §14.6, §14.6.9 | 2002 (split) / 2023 (status phrase) | — | OoSpineTests D8 facts; 2023 phrase DEFERRED→P13 unless in scope | — |

Conformance-test discipline (`feedback_conformance_tests_per_feature`, `feedback_parse_and_emit_together`): every feature step ships its `.cob`+`.out` (positive) and/or `.cob`+`.err` (negative) in the SAME commit, registered in the edition `manifest.json`, auto-discovered by `CorpusRunnerTests`. Every new construct enters the VERSION TEST MATRIX (`docs/VERSION_CHANGE_REFERENCE.md` / the generated VCR) with its introduction/continuity/behavior rows (`feedback_version_test_matrix`). Update `docs/COBOLNET_OO_DESIGN.md` (the OO deep-dive) and `docs/DOC_INDEX.md` in the same change set when the subsystem structure changes (`feedback_follow_design_docs_and_spec`, `feedback_grammar_doc_sync`).

### Documentation to keep current (same change sets)
- `docs/COBOLNET_OO_DESIGN.md` — update the STATUS block + the "Greenfield seams" section to describe the `Oo/` folder, `OoDriver`, `OoConformance`, `OoClassLayout`, `NamingConvention`, `OoMethodBinding`, and scoped `BinderContext` OO state (why the ambient flags were removed).
- `docs/DOC_INDEX.md` — no new doc, but note the OO subsystem's new file topology if the index tracks folders.
- `docs/ISO2023_CONFORMANCE_PLAN.md` — tick ANY LENGTH, the §4.2.2 interface leg, multi-base reject; record the 2023 GOBACK-status-phrase disposition.
- `DEVLOG.md` — one entry per commit boundary (newest-first, real timestamp).
- `resume-prompt.md` — update the top STATE banner at phase end (`feedback_plan_updates`).
