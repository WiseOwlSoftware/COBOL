# PHASE 07 — Exhaustive Visitor Dispatch + Semantic Normalization + Binder/Emitter God-Class Decomposition

- **Phase:** P7
- **Track:** rearchitecture
- **Risk:** HIGH (broadest structural phase; the structural-`Place` change ripples through every renderer)
- **Depends on:** **P6** (Real Binder phase) must be DONE — this phase consumes the `BoundCompilation` result,
  the `BindPipeline`/`IBindPass` manifest, the `SymbolTable`/`ScopeResolver`, and the immutable data model that P6
  produces. It also assumes the `StorageForm` discriminator and `StorageFormPass` from **P5/P6** exist (this phase
  reads `StorageForm` off the bound node/`Place`, never a mutable `DataItem` flag). P0 (characterization harness +
  emitted-C# snapshots + oracle bake-out) and P1 (namespace rename) should be DONE; if P0's reference-assembly
  caching is not yet landed, Step 2 lands it.
- **Goal (one paragraph):** Replace the two hand-maintained god-switches (`StatementBinder.BindStatementCore`,
  `CSharpEmitter.EmitStatement`) and the four renderer switches with a **source-generated exhaustive visitor** over
  the sealed `Bound*` hierarchy, so a missing dispatch arm is a **compile error**, not a runtime `LoudStmt`. Push
  the emit-time re-classification (MOVE category, storage form, table-access polarity) back onto the **bound node**
  so `EmitMove`/`ConvertSource` become pure renderers and the emitter stops reading `DataItem.StoreAsImage`/
  `IsStrongGroup`. Make `Place` **structural** (an `AccessPath` of `AccessSegment`s + `BoundExpr` subscripts + a
  ref-mod span) with a Roslyn-side `PlaceRenderer` owning all C# text, behind a materialized `ICodeGenBackend`
  seam. Break `StatementBinder` (21 partials) into real collaborators — `ProcedureTableBuilder`,
  `ExpressionBinder`, `ConditionBinder`, `PhraseBlocks`, and ~18 `Verbs/*Binder` classes over a `BinderContext` —
  lifting inline edition-invariant SR (semantic) checks to `Validation/StatementValidation` (edition gating lives
  ONLY in the `VersionConformancePass` — see `DESIGN-version-conformance-pipeline.md`); and break `CSharpEmitter`
  (15 partials) into `ProgramEmitter`/`DispatchEmitter`/`StatementEmitter` + per-verb emitters + renderers over an
  **immutable** `EmitContext` (replacing the mutable `TargetScale/TargetReal/TargetRounding/InSizeErrorContext` H1
  hazard with a `ReceiverContext` value parameter). Introduce a typed, `nameof`-anchored `RuntimeApi` façade over
  the ~60 emitted runtime members. Throughout, the phase **preserves the bind-vs-emit separation** of the
  version-conformance pipeline (`DESIGN-version-conformance-pipeline.md`): the decomposed emitters contain no
  edition gating, and emit stays unreachable when any diagnostics exist.
- **Exit criteria:** A missing bound-node arm is a **compile error** (every loud `_ =>`/`default => LoudStmt`
  runtime default deleted); `EmitMove`/`ConvertSource` emit-time re-classification is gone (MOVE kind + storage
  form travel on the node); `Place` carries **structure, not C# strings** (no `Read()/Write()` on `Place`;
  `PlaceRenderer` owns rendering); `StatementBinder`/`CSharpEmitter` are thin dispatch + real collaborator
  classes; `RuntimeApi` is the single codegen↔runtime contract (grep-forbidden bare `Cobol*.` literals in
  `CodeGen/`); bind-vs-emit separation is preserved (`DESIGN-version-conformance-pipeline.md`) — emitters contain **no
  edition gating**, and emit is **unreachable with non-empty diagnostics**; the full battery is green and the
  emitted-C# snapshots are reviewed-neutral.
- **STATUS: ✅ DONE — Steps 1–12 ALL COMPLETE (PHASE-07 CLOSED).** In place: the exhaustive
  generated bound-tree visitor (Step 6), the `ICodeGenBackend` seam (Step 1), the `AssemblyPackager` split (Step 2),
  the immutable `EmitContext` + `ReceiverContext`-by-parameter (Step 3), the `RuntimeApi` façade + `FigurativeConstants`
  service (Step 4), the ACCEPT-verb renames (Step 5), and `MoveKind` on the bound node (Steps 7–8). **Step 9 dissolved
  the emitter god class** — `ProgramEmitter` → the `UnitEmitters` per-unit composition root → `DispatchEmitter` /
  `StatementEmitter` (the generated `IBoundStatementVisitor<bool>`) + 18 `Verbs/*Emitter` + `EcEmitter` + the
  `DataDivision/` five behind `DataEmitter`, all over the `nameof`-anchored static `RuntimeApi`; `CSharpEmitter`
  survives ONLY as the thin bind-host facade (Bind/EmitBound + `IOoBindHost` + the OO BIND half) until P9. **Step 10
  dissolved the binder god class** — `StatementBinder` is now dispatch + the mark/drain wrap + the composition root
  over the `BinderContext` spine → `ProcedureTableBuilder` + `ExpressionBinder` + `ConditionBinder` + `EcBinder` +
  `OoBinder` + `PhraseBlocks` + 22 `Verbs/*Binder` + the edition-invariant SR band in `Validation/StatementValidation`;
  collaborators reach siblings through the root's accessors. **Step 11 dissolved the string-carrying `Place`** — all
  eight subtypes are STRUCTURAL over an `AccessPath` (`Binding/Model/AccessPath.cs`), rendered by `CodeGen.Roslyn.
  PlaceRenderer`; `Place.Read()/Write()` are DELETED and the R5 neutrality test (`PlaceNeutralityTests`) locks G4 (the
  bound tree is backend-neutral). ONE documented deferral: subscript / ref-mod / RedefView-offset INDEX expressions
  stay transitional STRINGS, folding into the D10 SUBSCRIPT-mode removal (PHASE 15). **Step 12 landed the
  FUNCTION-arg grammar (the grammar route, as preferred)** — `functionCall : FUNCTION functionName (LPAREN
  functionArgList? RPAREN)?` with each `functionArgument` a REAL parse tree (§8.4.3.2 SR8: phrase word / OMITTED /
  non-numeric literal / arithmeticExpression), enabled by the lexer's FUNCTION two-token-lookbehind SUBSCRIPT-push
  suppression + the argument-region-gated `SIGNED_*` twins (§8.7.1 operator spacing / §8.3.3.3.2 sign adjacency:
  `MAX(A -4)` = two args) + the `FNARG_SEPARATOR` token (§8.3.5 — the argument boundary survives so `MAX(A * B,
  (C + 1) / 2, …)` never mis-lexes the group as a subscript of B); the hand-rolled recursive-descent arg parser is
  DELETED OUTRIGHT — the D2 keyword-omitted form re-parses its captured argument text through the SAME
  `functionArgList` rule (`FunctionArgFragment` + `CobolLexer.PrimeFunctionArgs`), and `UdfBinder` binds through the
  same `BindArgOperand`; the `IntrinsicRenderer` STATIC channel is DELETED (single instance channel over the ONE
  `NumericRenderer` under `ReceiverContext.None`; `OperandText.AsString` takes the per-unit renderer; the public
  render entries are save/restore RE-ENTRANT so the mid-render default-receiver call cannot go stale; the file is
  fully `RuntimeApi`-routed and OFF the ratchet whitelist). As-landed deviations: the renderer STAYS in
  `CodeGen/Emit/` (the `Roslyn/` consolidation rides P9 with `ExpressionRenderer`); the §8.9 funnel skips a BARE
  function-argument word (a §15 phrase-word position is not provable); `**` in arguments now folds LEFT-associative
  per §8.8.1.2 r3 (the hand parser's right-assoc was a latent spec bug; corpus had zero `**` args); the frozen
  legacy oracle adapts via `MapFunctionArgTokens` (per-argument CST leaves → SUB-token twins → its proven
  `BindSubscriptSegment`), proven by the full NIST net. The ~19 inline edition gates moved VERBATIM
  with their verbs — the pass-folding is Exec Step E's scope.

---

## 1. Preconditions & how to resume

Before starting, confirm P6 landed the following (grep to verify — all must exist):

```bash
cd E:/CobolSharp
grep -rln "record BoundCompilation"  src/Cobol.Net.Compiler/Binding   # P6 immutable result
grep -rln "class BindPipeline"       src/Cobol.Net.Compiler/Binding   # P6 pass manifest
grep -rln "class SymbolTable"        src/Cobol.Net.Compiler/Binding   # P6 scoped resolver
grep -rln "enum StorageForm"         src/Cobol.Net.Compiler/Binding   # P5/P6 storage discriminator
grep -rln "class StorageFormPass"    src/Cobol.Net.Compiler/Binding   # P5/P6 single owner of the store decision
```

P5/P6 have landed all of these — `BoundCompilation`, `BindPipeline`, `SymbolTable`, `StorageForm`, and
`StorageFormPass` all exist, and `DataItem.StoreAsImage` is a read-only projection of `Storage` (the mutable flag +
`MarkStoreAsImage` write-back are gone).

**The battery (run at every commit boundary; must stay green):**

```bash
# 1. Greenfield conformance (the primary net: ~3166 cases)
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet
# 2. Greenfield unit (~281)
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet
# 3. FULL legacy differential guard (NIST 353 MATCH + 11 LEGACY_DIVERGENT). Only needed at commits that touch the
#    SHARED grammar (.g4) — Step 12 (FUNCTION-arg grammar). Pure C# refactor commits need only 1+2, but run 3 at
#    the END of the phase regardless.
bash scripts/guard-fast.sh
# 4. Emitted-C# snapshot neutrality (P0 characterization harness). A gate-3 diff with NO intended emit change is a
#    RED; an intended change re-baselines with review. NEVER set the update env in CI.
dotnet test tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj -v quiet
```

**Behavioral probe (prebuilt CLI):**

```bash
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <source.cob> --std 2002 -o /tmp/out.dll --run
```

**Resuming mid-phase:** every step below is an independent COMMIT BOUNDARY that leaves the battery green. To resume,
read the STATUS line, `git log --oneline` to find the last `P7 stepNN` commit, and continue at the next step. No step
leaves the tree in a non-compiling state at its commit boundary. The two multi-sub-commit steps (Step 6 visitor
conversion, Step 11 structural `Place`) carry their own internal resumability notes (§6).

---

## 2. Rationale — the problems this phase fixes

Grounded in the AS-IS dossier, the two sibling designs (`DESIGN-binder-bound-tree.md`, `DESIGN-codegen-backend.md`),
and the current code:

1. **Two hand-maintained god-switches, non-exhaustive.** `BindStatementCore` (`StatementBinder.cs:170-231`, ~50
   arms ending in `_ => new BoundUnsupported(...)`) and `EmitStatement` (`CSharpEmitter.cs:349-455`, ~79 `case`
   arms ending in `default: w.Line(LoudStmt(...))`). A forgotten arm ships as a **runtime throw**, not a compile
   error. The same 99-record sealed hierarchy (`BoundTree.cs`, `grep -c "record Bound"` = 99) is walked by ≥4 more
   independent type-switches: `BoundStores.StoreKindOf`, `NumericRenderer.Render/AsNum`,
   `OperandText.AsString/IsString`, `ConditionRenderer.Render` (+ `AlterCollectFields`). Adding a bound node is
   shotgun surgery across 6+ switches with zero compiler help (`DESIGN-codegen-backend.md §1.3`,
   `DESIGN-binder-bound-tree.md §1.4`).

2. **Smart emitter / semantics leaking past the bound tree.** The bound tree is the SSOT
   (`BoundTree.cs:7-13` — "the backend renders the bound tree, it never re-walks the parse tree"), yet the emitter
   re-derives semantics the binder resolved: `EmitMove`→`ConvertSource` re-classifies MOVE category at emit time
   (`CSharpEmitter.cs:479-540, 714-...`), and the emitter reads binder-set `DataItem.StoreAsImage` at 20+ sites
   across `CodeGen/` (`grep StoreAsImage src/Cobol.Net.Compiler/CodeGen` — a binder→emitter channel OUTSIDE the
   bound tree, `DESIGN-binder-bound-tree.md §1.5`).

3. **`Place` carries C# text (the blocking neutrality violation).** `Place.Read()/Write(rhs)` return **C# strings**
   (`Place.cs:22-25`); every subtype hard-codes runtime call text (`MemberPlace` → `"{Path} = {rhs};"`;
   `RedefViewPlace` → `"CobolString.SpliceInto(...)"` `Place.cs:100-101`; `NumericImagePlace` →
   `"CobolNum.FormatDisplay(...)"` `Place.cs:160-164`; `DynTablePlace` carries TWO precomputed path strings
   `Place.cs:58`). A future CIL backend cannot consume any of this. This is the single largest structural blocker
   (`DESIGN-codegen-backend.md §1.2, §2.3`).

4. **`CSharpEmitter` is a god class with mutable per-emit state (the H1 hazard).** 15 partials, ~12 mutable fields;
   `EmissionContext` exposes public get/set `TargetScale/TargetReal/TargetRounding/InSizeErrorContext`
   (`EmitCore.cs:60-79`) written before an RHS render and read deep in three renderers — a missed reset silently
   mis-scales or float-promotes (`DESIGN-codegen-backend.md §1.5`). `StatementBinder` is the same pattern across 21
   partials (~9.4k LOC) doing five fused jobs (`DESIGN-binder-bound-tree.md §1.3`).

5. **Untyped runtime coupling.** ~60 runtime members are named **by bare string** in `CodeGen/`
   (`grep -hoE "Cobol[A-Za-z]+\." src/Cobol.Net.Compiler/CodeGen` → `CobolNum.`×83, `CobolFile.`×61,
   `CobolString.`×40, …). A runtime rename is invisible until the *generated* C# fails to Roslyn-compile at run
   time (`DESIGN-codegen-backend.md §1.6, §3`).

6. **The `ICodeGenBackend` seam is promised (`COBOLNET_DESIGN §1.1/§18 #23`) but does not exist**
   (`grep ICodeGenBackend src/` → 0 hits). Renderer duplication: `IntrinsicRenderer`'s static channel
   (`NumStatic/StaticAdditive/StaticMul`, `IntrinsicRenderer.cs:353-380`) re-implements a division/float-incapable
   subset of `NumericRenderer` because it lacks an `EmissionContext`; figurative-fill is quadruplicated with
   divergent return types (`EmitText.FigurativeFill`, `FieldEmitter.FillCharFor`,
   `ConditionRenderer.FigurativeFillChar`, `EmissionContext.FigFill`).

---

## 3. Target end-state for this phase (concrete)

When P7 is DONE, these files/types exist with these responsibilities. (Folders under
`src/Cobol.Net.Compiler/CodeGen/Roslyn/` group the backend-specific renderers; `Binding/Procedure/` and
`Binding/Procedure/Verbs/` group the binder collaborators.)

**Backend seam & neutral tree**
- `CodeGen/ICodeGenBackend.cs` — `ICodeGenBackend { BackendId Id; BackendArtifact Emit(BoundCompilation, BackendOptions); }`, plus `BackendId`, `BackendOptions`, `BackendArtifact`, `BackendFactory`.
- `CodeGen/RoslynBackend.cs` — `RoslynBackend : ICodeGenBackend`; the only owner of C# syntax knowledge; drives `ProgramEmitter` per unit; hands packaging to `AssemblyPackager`.
- `CodeGen/AssemblyPackager.cs` — runtimeconfig write + runtime-dll deploy, split out of `RoslynBackend`; framework `MetadataReference` set cached in a `static readonly Lazy<ImmutableArray<MetadataReference>>`.
- `Binding/Place.cs` — **structural** `Place`: `abstract record Place { DataItem Item; PicInfo? Pic; }` with NO `Read()/Write()`. Subtypes carry structure: `MemberPlace(AccessPath, DataItem)`, `RefModPlace(Place, BoundExpr Start, BoundExpr? Length)`, `RedefViewPlace(Place Backing, BoundExpr ZeroOffset, int Width, DataItem)`, `NumericImagePlace(Place Inner)`, `RenamesPlace(IReadOnlyList<Place>, DataItem)`, `CapacityRegisterPlace(Place Table, DataItem)`. New: `AccessPath(IReadOnlyList<AccessSegment>)`, `AccessSegment` (`RootFieldSegment`, `MemberSegment`, `IndexSegment(BoundExpr)`, `FixedTableSegment(BoundExpr, AccessDir)`, `DynTableSegment(BoundExpr, AccessDir)`).

**Exhaustive dispatch** (✅ 6a delivered — see Step 6 §6a for the as-built design)
- `Binding/Bound/BoundTree.cs` — `[BoundNode]` on all **7** sealed roots (`BoundStatement`, `BoundExpr`, `BoundCondition`, `BoundOperand`, `BoundBoolExpr`, `BoundPerformControl`, `BoundSetTarget`). The `[BoundNode]` attribute + the generated `I{Root}Visitor<T>` interfaces + the `BoundVisitor.Accept<T>` extensions all arrive from the generator (post-init attribute source + `BoundVisitors.g.cs`); no hand-written types in the tree.
- `src/Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs` — Roslyn **incremental** source generator (netstandard2.0 analyzer, `ReferenceOutputAssembly=false`; no new java/pwsh prereq) emitting the visitor interfaces + `Accept<T>` extension `switch` from the `[BoundNode]` hierarchy via the **semantic model** (`INamedTypeSymbol.BaseType`). Correctness net `tests/…Unit/BoundVisitorGeneratorTests.cs`; no drift test (generation is not a committed artifact). The error nodes (`BoundUnsupported`/`BoundOperandError`/`BoundExprError`/`BoundConditionError`/`BoundBoolError`) each get their own `Visit` — no `IBoundError` marker (OPEN Q1 resolved: source generator chosen; the hand-written-abstract-visitor fallback was not needed).

**Semantic normalization on the node**
- `Binding/Bound/BoundTree.cs` — `enum MoveKind { Group, ElementaryAlphanumeric, ElementaryNumeric, NumericEdited, AlphaEdited, FigurativeFill, FigurativeToNumericImage, RefModSlice }`; `BoundMove(IReadOnlyList<Place> Targets, BoundOperand Source, MoveKind Kind, StorageForm TargetForm)`. `ConvertSource`'s emit-time category switch is deleted.

**Emitter collaborators (immutable context)**
- `CodeGen/Roslyn/EmitContext.cs` — immutable per-unit config (`Writer`, `Unit`, `Edition`, `Names`, derived `CollateArg`/`EditCfgArgs`); NO `Target*` fields.
- `CodeGen/Roslyn/ReceiverContext.cs` — `readonly record struct ReceiverContext(int Scale, bool Real, CobolRounding Rounding, bool InSizeError)` passed as a parameter to numeric renders.
- `CodeGen/ProgramEmitter.cs`, `CodeGen/DispatchEmitter.cs`, `CodeGen/StatementEmitter.cs` (the `IBoundStatementVisitor<bool>` core), `CodeGen/Verbs/{KeyedIoEmitter, SortEmitter, StringEmitter, InspectEmitter, ReportWriterEmitter, CallEmitter, OoEmitter, InitializeEmitter, PtrEmitter, EvaluateEmitter, CorrespondingEmitter, AlterSwitchEmitter, AcceptDisplayEmitter}.cs`.
- `CodeGen/Roslyn/PlaceRenderer.cs` — sole owner of `Place`+`AccessSegment` → C# read/write text.
- `CodeGen/Roslyn/ExpressionRenderer.cs` (was `NumericRenderer`, `IBoundExprVisitor<NumX>`), `CodeGen/Roslyn/ConditionRenderer.cs` (`IBoundConditionVisitor<string>`), `CodeGen/Roslyn/IntrinsicRenderer.cs` (single channel — static channel deleted), `CodeGen/Roslyn/BooleanRenderer.cs`.
- `CodeGen/Roslyn/RuntimeApi.cs` — typed `nameof`-anchored façade over the ~60 runtime members.
- `CodeGen/Roslyn/FigurativeConstants.cs` — ONE figurative service.
- `CodeGen/DataDivision/{RecordStructEmitter, GroupImageCodec, GroupValueSlicer, ValueInitializer}.cs` — `FieldEmitter` split 4 ways, moved out of `Emit/`.
- `Binding/Bound/BoundStoreAnalysis.cs` — `BoundStores` renamed; an analysis visitor.

**Binder collaborators**
- `Binding/Procedure/{StatementBinder(thin dispatch), BinderContext, ProcedureTableBuilder, ExpressionBinder, ConditionBinder, PhraseBlocks}.cs`.
- `Binding/Procedure/Verbs/{MoveBinder, ArithmeticBinder, IfBinder, PerformBinder, KeyedIoBinder, SequentialIoBinder, SortBinder, StringBinder, InspectBinder, InitializeBinder, IntrinsicBinder, UdfBinder, ReportWriterBinder, CallBinder, SetBinder, OoBinder, EvaluateBinder, SearchBinder, AcceptDisplayBinder}.cs`.
- `Binding/Validation/StatementValidation.cs` — the inline edition-invariant SR (semantic) checks lifted out of the
  binder. Edition gating does NOT live here: the `VersionConformancePass` is the sole edition-gating funnel
  (`DESIGN-version-conformance-pipeline.md`); the binder stays edition-agnostic.

**Deleted at this phase's end**
- `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), the `CompilerTempClones` re-sync (`CSharpEmitter.Call.cs:111-120`), the OO `StoreAsImage` re-sync (`CSharpEmitter.Oo.cs:694-697`) — folded into `StorageFormPass` (P5/P6) and read off the node.
- `Place.Read()/Write()` (all subtypes).
- `IntrinsicRenderer` static channel (`NumStatic/NumStaticExpr/StaticAdditive/StaticMul`).
- The loud `_ =>`/`default: LoudStmt(...)` arms in every bound-tree consumer.
- The 4 divergent figurative-fill copies.

---

## 4. STEP-BY-STEP

> Ordering principle: **mechanical/low-blast-radius first, structural `Place` last** (matches `DESIGN-codegen-backend.md`
> M0–M7 and `DESIGN-binder-bound-tree.md` §5). Each numbered step is a COMMIT BOUNDARY. Run battery items 1+2 at every
> boundary; item 3 (legacy guard) only where noted (grammar touch) and once at phase end; item 4 (snapshots) at every
> boundary. "prove-then-delete" governs every mutable-flag/duplicated-computation removal: compute the new form,
> cross-check it against the old across the whole corpus, THEN delete the old — never delete a mutation site on faith.

### Step 1 — Materialize the `ICodeGenBackend` seam (no behavior change)

- **Files:** create `CodeGen/ICodeGenBackend.cs`; edit `CodeGen/RoslynBackend.cs`, `CompilerDriver.cs`.
- **Change:** Add the interface + record types exactly per `DESIGN-codegen-backend.md §2.2`:
  ```csharp
  namespace CobolNet.CodeGen;
  public enum BackendId { Roslyn, Cil }
  public sealed record BackendOptions(string OutputPath, string AssemblyName, EditionInfo Edition,
      bool EmitPdb = true, bool WriteSource = true);
  public sealed record BackendArtifact(bool Success, IReadOnlyList<Diagnostic> Diagnostics,
      string? GeneratedSourcePath, string? AssemblyPath);
  public interface ICodeGenBackend { BackendId Id { get; } BackendArtifact Emit(BoundCompilation program, BackendOptions options); }
  public static class BackendFactory { public static ICodeGenBackend For(BackendId id) => id switch { BackendId.Roslyn => new RoslynBackend(), _ => throw new NotSupportedException() }; }
  ```
  Make `RoslynBackend : ICodeGenBackend`, wrapping the CURRENT `CSharpEmitter.Emit(...)` + `Compile(...)` verbatim
  inside `Emit(BoundCompilation, BackendOptions)`. The driver calls `BackendFactory.For(BackendId.Roslyn).Emit(...)`.
  Pure indirection — NO output change.
- **Why:** Establishes the backend-neutral boundary every later step renders behind (fixes rationale #6). `Emit`
  receives a `BoundCompilation` (from P6) and performs NO binding — the extraction P6 already did.
- **Verify:** battery 1+2+4 green. `grep -rn ICodeGenBackend src/` shows the interface.
- **COMMIT:** `P7 step1: materialize ICodeGenBackend seam (RoslynBackend : ICodeGenBackend) — no behavior change`

### Step 2 — Split `AssemblyPackager`; cache framework references

- **Files:** create `CodeGen/AssemblyPackager.cs`; edit `CodeGen/RoslynBackend.cs`.
- **Change:** Move `DeployRuntime` (`File.Copy`) + `WriteRuntimeConfig` out of `RoslynBackend` into
  `AssemblyPackager.Package(EmitResult, BackendOptions)`. Keep `RoslynBackend.Compile` PURE (C#→`EmitResult`).
  Change `ReferenceAssemblies()` (uncached ~180 refs/compile, `RoslynBackend.cs:73-83`) to
  `static readonly Lazy<ImmutableArray<MetadataReference>>`.
- **Why:** Isolate packaging side effects; the ref cache is the single highest-leverage test-throughput win
  (rationale #7 / `DESIGN-codegen-backend.md §1.7, M1`). *Skip the `Lazy` change if P0 already landed it* — grep
  `Lazy<ImmutableArray<MetadataReference>>` first.
- **Verify:** battery 1+2+4 green; note the wall-clock drop on the conformance run.
- **COMMIT:** `P7 step2: split AssemblyPackager out of RoslynBackend; cache framework MetadataReferences`

### Step 3 — Rename `EmissionContext` → `EmitContext`; make it immutable; introduce `ReceiverContext`

- **Files:** rename `CodeGen/Emit/EmitCore.cs`'s `EmissionContext` → `EmitContext`; create
  `CodeGen/Roslyn/ReceiverContext.cs`; edit every renderer + `CSharpEmitter*` reader of `Target*`.
- **Change:** (a) Rename the type (ends the legacy-tree name collision with
  `src/CobolSharp.Compiler/CodeGen/Emission/EmissionContext.cs`). (b) DELETE the four mutable fields
  `TargetScale/TargetReal/TargetRounding/InSizeErrorContext` (`EmitCore.cs:60-79`) and the manual "H1 staleness
  discipline" resets (`CSharpEmitter.cs:849-851,933-935,952,999,1016`). (c) Add
  `readonly record struct ReceiverContext(int Scale, bool Real, CobolRounding Rounding, bool InSizeError)` and
  thread it as an `in ReceiverContext rcv` parameter into `NumericRenderer.Render/AsNum/Combine/Fold` and the
  arithmetic-store emit sites. Where no receiver is in scope (e.g. a condition operand), pass
  `ReceiverContext.None` (scale 0, not-real, truncation, no-size-error).
- **Why:** Closes the H1 staleness class **by construction** (rationale #4 / `DESIGN-codegen-backend.md §2.5, M2`).
  This is the enabler for deleting the `IntrinsicRenderer` static channel (Step 12).
- **Verify:** battery 1+2+4 green. This is the refactor the full battery must prove behavior-identical — pay close
  attention to any snapshot diff (there should be NONE).
- **COMMIT:** `P7 step3: EmissionContext→EmitContext (immutable); replace Target* mutable state with ReceiverContext param`

### Step 4 — `RuntimeApi` façade + `FigurativeConstants` service

- **Files:** create `CodeGen/Roslyn/RuntimeApi.cs`, `CodeGen/Roslyn/FigurativeConstants.cs`; edit all renderers.
- **Change:** (a) `RuntimeApi` — one instance per `EmitContext`; one method per emitted runtime member, returning a
  C# fragment string, `nameof`-anchored where possible (`$"{nameof(CobolNum)}.{nameof(CobolNum.Store)}(...)"`).
  Route every renderer's runtime-call emission through it. (b) `FigurativeConstants.For(word/kind, PicCategory?,
  collate)` → `{ char RuntimeChar, string CsLiteral }`; delete the 4 divergent copies (`EmitText.FigurativeFill`,
  `FieldEmitter.FillCharFor`, `ConditionRenderer.FigurativeFillChar`, `EmitContext.FigFill`) and route every
  VALUE-init / comparison-fill / membership-fill through it. (c) Add a **guard test** (in the Characterization
  project) that greps `CodeGen/**/*.cs` for bare `Cobol[A-Za-z]+\.` literals outside `RuntimeApi.cs` and FAILS if
  any remain — enforce incrementally (whitelist the not-yet-migrated files, shrink the whitelist to empty by
  Step 10).
- **Why:** Closes the untyped-runtime-coupling smell (rationale #5) and the figurative-fill quadruplication
  (rationale #6). A runtime rename now breaks ONE file at compile time (`DESIGN-codegen-backend.md §3`).
  > **Note on `CsLiteralCodec` / the apostrophe-VALUE fix:** the single literal-decode boundary
  > (`Common/CobolLiteral.Decode` recognizing BOTH ISO string delimiters) is owned by the **data-model phase (P5)**.
  > If P5 already landed it, route decodes through it here. If NOT, do NOT fix the apostrophe-VALUE miscompile in
  > this phase — it is a red→green behavior change that must ship with its conformance goldens in P5. This phase is
  > behavior-neutral except where §7 explicitly says otherwise.
- **Verify:** battery 1+2+4 green; the bare-`Cobol*.` guard test passes for the migrated files.
- **COMMIT:** `P7 step4: RuntimeApi typed façade + FigurativeConstants service; forbid bare Cobol*. in migrated CodeGen`

### Step 5 — Rename the ACCEPT-verb partials (end the Visitor-term collision)

- **Files:** rename `Binding/Bound/StatementBinder.Accept.cs` → `Binding/Procedure/Verbs/AcceptDisplayBinder.cs`
  (content still a partial for now — real class split is Step 9); rename `CodeGen/CSharpEmitter.Accept.cs` →
  `CodeGen/Verbs/AcceptDisplayEmitter.cs` (partial for now). Update any doc-comment/xref.
- **Change:** Pure file rename + the `partial` stays; NO code change. Do this BEFORE Step 6 so "Accept" unambiguously
  means the visitor method. *(LANDED note: `AcceptDisplayBinder.cs` also carries the `BoundAccept` record +
  `AcceptKind` enum — bound-NODE types that rode along in the pure rename; the Step 9/10 real split must relocate
  them to the bound-tree home, not leave node definitions in a `Verbs/` file.)*
- **Why:** `StatementBinder.Accept.cs` binds the ACCEPT *verb*; once `Accept<T>(visitor)` exists (Step 6) the name
  collides confusingly (`DESIGN-binder-bound-tree.md §4` rename row).
- **Verify:** battery 1+2 green (compile-only rename).
- **COMMIT:** `P7 step5: rename ACCEPT-verb partials (StatementBinder.Accept→AcceptDisplayBinder, CSharpEmitter.Accept→AcceptDisplayEmitter)`

### Step 6 — Source-generated exhaustive visitor over the bound tree

This is a MULTI-SUB-COMMIT step (one consumer per sub-commit); each sub-commit is battery-green.

- **6a — Generator + interfaces. ✅ DONE (2026-07-11, commit see STATUS).**
  - Files (as built): project `src/Cobol.Net.Compiler.SourceGen/` (a `netstandard2.0` Roslyn **incremental** source
    generator, `IsRoslynComponent`, referenced by `Cobol.Net.Compiler.csproj` with
    `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`); `BoundVisitorGenerator.cs`; `[BoundNode]` marks all
    **seven** roots in `Binding/Bound/BoundTree.cs` (`BoundStatement`/`BoundExpr`/`BoundCondition`/`BoundOperand`/
    `BoundBoolExpr`/`BoundPerformControl`/`BoundSetTarget` — the design's "five" undercounted). The `[BoundNode]`
    attribute is emitted into the compilation by the generator's `RegisterPostInitializationOutput` (self-contained;
    no attributes assembly). Generated `BoundVisitors.g.cs` = 7 interfaces + 120 Visit overloads + a `BoundVisitor`
    static class with 7 `Accept<T>` extensions (120 arms). Correctness net: `BoundVisitorGeneratorTests` (reflection —
    each interface's Visit-params == the compiled non-abstract descendants of its root; **no drift test needed —
    generation runs every build off the live type graph, so there is no committed artifact to drift**).
  - How it discovers leaves: through the Roslyn **semantic model** (`INamedTypeSymbol.BaseType` chain), NOT a text
    scan — a base-less helper record (e.g. `BoundCallArg`) can never be mis-bound to a following root, and the file
    cannot drift. Adding a leaf without a `Visit` overload is a **compile error** in every implementing visitor. NO
    consumer converted yet — the machinery is additive; existing switches keep working.
  - **Design choices that refine the original sketch (kept CURRENT here per the owner doc-sync rule):**
    - **Roslyn source generator, NOT a hand-rolled generator script.** An interim regex-based pwsh script
      (`gen-constructs.ps1`-style, committed `.g.cs` + drift test) was written and then **removed** — parsing C# with
      a regex is the hand-rolled-parser anti-pattern (it immediately mis-captured `BoundCallArg` across a `;`), and
      the owner directed the canonical C#→C# tooling. The pwsh `gen-*` scripts remain correct for the *grammar/registry*
      artifacts (they also emit **ANTLR `.g4`**, which a C# source generator cannot) — genuinely a different job, so
      [[feedback_singular_pattern]] (best tool per job) is honored, not violated.
    - **`Accept` is an extension-method `switch` with a `_ => throw`, not a `partial`-method-per-leaf.** The records
      are not `partial`; the extension form needs zero edits to 120 declarations. The `_ => throw` satisfies CS8509
      (C# does not prove closed-hierarchy exhaustiveness for class types) and is unreachable-by-construction — the
      switch is regenerated from the live leaf set every build. The compile-time exhaustiveness guarantee lives in the
      **interface** (a consumer must implement every `Visit`), which is the property that matters.
    - **The five error nodes each get their own `Visit`** (no `IBoundError` marker / collapsed `VisitError`). Explicit
      per-node Visits are strictly more exhaustive; a consumer that wants uniform error handling calls one shared
      helper from each — no machinery, no lost guarantee. (Supersedes the 6b `IBoundError`-routing note below.)
  - Verify (done): `dotnet build src/Cobol.Net.Compiler` succeeds, generator emits `BoundVisitors.g.cs`
    (`-p:EmitCompilerGeneratedFiles=true` confirmed 120 leaves / 7 roots); unit battery green (266 + 7 new).
  - COMMIT: `P7 step6a: Roslyn BoundVisitorGenerator + [BoundNode] on 7 roots (exhaustive I{Root}Visitor/Accept; no consumers yet)`

- **6b..6f — Convert consumers ONE at a time** (emitter first, then analyses, then renderers):
  - **6b ✅ DONE (2026-07-11, commit see STATUS).** `EmitStatement` → `CSharpEmitter : IBoundStatementVisitor<bool>`
    (bool = "unconditionally transfers control", preserving today's contract). The 79 `case` arms became 79 `Visit`
    methods in a NEW partial `CodeGen/CSharpEmitter.Dispatch.cs` (bodies verbatim — the arm's pattern var renamed to
    `n`, `w` re-localized per method); `EmitStatement` is now `=> s.Accept(this)`; the `default: LoudStmt(...)` is
    DELETED. **As-built refinement:** the emitter stays ONE class (the interface is added on the Dispatch partial) —
    a separate `StatementEmitter` collaborator is the P6-dependent decomposition deferred to Exec Step D (it needs
    the immutable `EmitContext`/`BinderContext`); pulling it out now would just fight the shared mutable emitter
    state. The five error nodes each get their own `Visit` (no `IBoundError` collapse — see §6a). Proven byte-exact:
    32 characterization snapshots + 3158 conformance unchanged.
  - **6c ✅ DONE (2026-07-11, DEVLOG 761).** `BoundStores.StoreKindOf` → `StoreKindVisitor :
    IBoundStatementVisitor<StoreKind?>` carrying `DataItem item` (the local funcs `Hit`/`TargetHit`/`ReceiversHit`/
    `Kids`/`StoreOrKids`/`InitStores` are now instance methods; recursion is `child.Accept(this)`); `StoreKindOf(s,
    item) => s.Accept(new StoreKindVisitor(item))`. FAITHFUL, byte-neutral relocation — each of the 79 arms transcribed
    VERBATIM (recursion kept node-specific, NOT swapped to `StatementChildren`), the compiler proving all 79 covered.
    The 9 former `_ => null` catch-all nodes (`BoundAllocate`/`BoundFree`/`BoundInvokeUniversal`/`BoundRaiseObject`/
    `BoundSetAddressOfBased`/`BoundSetCapacity`/`BoundSetObjectRef`/`BoundSetPointer`/`BoundSetPointerUpDown`) are now
    explicit `=> null` Visits, so a NEW leaf can no longer silently fall into the stage-loud bucket — it is a compile
    error until classified. 3158 conf + 32 char + 269 unit unchanged.
    - **The flagged `BoundKeyedDelete`/`InvalidKey` "latent bug" was RESOLVED as a NON-bug.** `StatementBinder.BindStatement`
      mark/drains property ops per-statement, and every nested body binds through its OWN `BindStatement` (via
      `BindBlocks`/`Select(BindStatement)`), so a property temp lives ONLY in the carrying statement's direct
      operands/condition — never in a separately-wrapped nested handler. The `Kids` recursion into handler bodies is
      therefore DEFENSIVELY total (it never actually finds the temp there), which is exactly why `BoundKeyedDelete`
      omitting it is equivalent, not a silent-lost-store. (Captured in the `BoundStores` `<remarks>`.) A future cleanup
      could drop the redundant recursion, but that is a separate, non-byte-covered change — not done here.
  - **6h ⏳ REMAINS — completeness sweep (the 6b–6f list was NOT exhaustive; found by a post-6c grep, DEVLOG 762).**
    The generated visitor exists for all 7 roots, but these bound-node dispatches were outside the enumerated list:
    - **✅ `BooleanRenderer.Render` (BoundBoolExpr) — DONE (DEVLOG 762).** A cached nested `IBoundBoolExprVisitor<string>`
      (static class, like OperandText); the loud `_ =>` deleted. This was the only remaining LOUD default over a whole
      root; byte-exact (32 char + 3158 conf).
    - **✅ `IntrinsicRenderer.StrStatic`/`NumStatic` (BoundOperand) + `NumStaticExpr` (BoundExpr) — DONE (DEVLOG 763).**
      Three cached nested visitors (`StrStaticVisitor : IBoundOperandVisitor<string>`, `NumStaticVisitor :
      IBoundOperandVisitor<NumX>` — separate classes, same-param/different-return can't overload — and
      `NumStaticExprVisitor : IBoundExprVisitor<NumX>`); the deliberately-partial H3 loud arms are now EXPLICIT `Visit`s
      (byte-identical `Loud(n)`). Byte-exact (32 char + 3158 conf). ✅ `StatementBinder.Intrinsics.ArgExpr` (BoundOperand
      → BoundExpr) also DONE (DEVLOG 764).
    - **✅ SYSTEMATIC AUDIT COMPLETE (DEVLOG 765) — grep-classified every bound-node switch across the 11 files, each
      decision tied to an ISO § (per the owner's directive: validate against the SPEC, not the prior implementation or a
      green corpus).** CONVERTED — the completeness-critical dispatches: the TOTAL result-dispatches (emitter, all 5
      renderers, ArgExpr, StoreKindOf) and the five STATEMENT WALKERS (`UsageCollectionPass`, `VersionConformancePass.Recurse`,
      `ContainsNextSentence`, `AlterCollectFields`, `ContainsIntrinsic`) — the last five now recurse via
      `StatementChildren`, deleting the prose-synced hand-lists. KEPT (spec-grounded, a visitor would be gratuitous):
      partial predicates whose default is a meaningful value correct for every unlisted leaf (`CountExpr`/`LinesExpr` §14.9.51,
      the category/width predicates), selective classifiers (`GateStatement` — only later-edition constructs gate, the
      SSOT is `constructs.json`), spec-stable emit-dispatches over tiny closed roots (`PerformControl` `default:`=once
      §14.9.28 GR1; `Set*Target` §14.9.39), partial error-defaulted dispatches whose default is a spec-appropriate
      "unsupported form" diagnostic (CALL BY-CONTENT arg §14.9.6; `BindLengthFold` §15.50.4), and the intentional
      PARSE-context switches (`BindStatementCore`/`BindCondition`, OPEN Q5). Final grep pasted in DEVLOG 765: no
      total-dispatch loud/error default remains. (A `BindLengthFold` numeric-literal "gap" was flagged then RETRACTED —
      §15.50.3 restricts a LITERAL argument to alphanumeric/national/boolean, so the error is spec-correct; DEVLOG 765→766
      — a reminder to read the SPECIFIC governing rule, not the nearest general sentence.)
    - **Emitter `PerformControl` switch (`default:` = `PerformOnce`) + `StoreSetTarget`/`AugmentSetTarget` (no default)
      — REASONED KEEP (DEVLOG 763), not converted.** `void` + closure-heavy (`body`/`inline`/`value`/`amount`), so the
      generic-return `IBound*Visitor<T>` would force a dummy `T` + a state-carrying per-call visitor — uglier than the
      switch — over tiny closed roots (4/2 leaves) with NO loud default; a new leaf surfaces as a missed emit (caught in
      test), not silent wrong output. Revisit IF a void-visitor generator variant lands.
    - **`UsageCollectionPass` (Step-5) — the one remaining walker worth converting; deferred to a FOCUSED pass (task
      #17).** Its own hand `Visitor` over the WHOLE tree (statements+exprs+operands+conditions+places) — the biggest
      walker and the completeness-bug origin. Wants the generated statement visitor + `StatementChildren` (+ the
      non-statement roots' generated visitors). Substantial; not byte-driven the way the renderers are.
  - **6d ✅ DONE (2026-07-11).** `NumericRenderer` implements `IBoundExprVisitor<NumX>, IBoundOperandVisitor<NumX>`;
    `Render`/`AsNum` are thin `=> e.Accept(this)` dispatchers + 11 expr + 8 operand `Visit` methods. The `Render`
    `_ =>` was already dead (all 11 expr leaves covered); `AsNum`'s `_ =>` caught `BoundAllLiteral`/`BoundBoolOperand`,
    now explicit loud `Visit`s (byte-identical loud value — `nameof` == the old `GetType().Name`). Byte-exact: 32 + 3158.
  - **6e ✅ DONE (2026-07-11).** `ConditionRenderer : IBoundConditionVisitor<string>`; `Render` keeps its
    `ctx.TargetReal = false` preamble then `=> c.Accept(this)` + 10 `Visit` methods (the two `BoundLogical` pattern
    arms merged into one `Visit` with an `Operands.Count == 0` guard). `_ =>` was dead. Byte-exact: 32 + 3158.
  - **6f (OperandText) ✅ DONE (2026-07-11).** `OperandText.AsString/IsString` (a `static` class) now dispatch through
    THREE cached private nested operand-visitors — `AsStringVisitor(deSign:false)`, `AsStringVisitor(deSign:true)`
    (`deSign` carried on the instance → zero per-call allocation), and `IsStringVisitor` — each
    `IBoundOperandVisitor<string/bool>`. `AsString`'s `_ =>` caught `BoundBoolOperand` (now explicit loud, byte-ident);
    `IsString`'s four `_ => false` leaves are now explicit. Byte-exact: 32 + 3158.
  - **6f (statement analyses) ✅ DONE (2026-07-11, DEVLOG 760) — converted onto the 6g `StatementChildren`.**
    `ContainsNextSentence` collapsed to `stmts.Any(HasNextSentence)` + `HasNextSentence(s) => s is BoundNextSentence ||
    s.StatementChildren().Any(HasNextSentence)` (the 20-arm hand-walker + `InSizeError` + the whole
    `KeyedHasNextSentence`/`KeyedNs` pair DELETED). `AlterCollectFields` collapsed to `if (s is BoundGoToAlterable g)
    fields.TryAdd(…); foreach (child in s.StatementChildren()) recurse` (the 13-arm switch + `AlterCollectLists` +
    `AlterCollectPhrase` DELETED). Both are now MORE complete than the hand-walkers (which missed EVALUATE/CALL/WRITE /
    SEQUENCE/keyed/RETURN phrase bodies — a latent label-less-goto / undeclared-field loud-fail) yet byte-exact-neutral
    on the corpus (32 char + 3158 conf + 269 unit unchanged — the corpus doesn't exercise the closed gaps). **REMAINS:
    6c `StoreKindOf` — the last walker (per-node polarity; see its bullet).**
  - Each sub-step: battery 1+2+4 green (mechanical relocation, identical output); COMMIT
    `P7 step6X: convert <consumer> to exhaustive IBound*Visitor; delete loud default`.
- **Why:** Kills the two god-switches + four renderer switches; a missing arm becomes a compile error
  (rationale #1 / `DESIGN-codegen-backend.md §2.4`, `DESIGN-binder-bound-tree.md §3.3`). The parse→bound dispatch
  `BindStatementCore` **stays a `switch`** — it dispatches over ANTLR *parse* contexts, there is no bound node to
  `Accept` yet (OPEN Q5, confirmed correct).

#### Step 6g — the WALKER variant: a generated `StatementChildren` primitive (✅ PRIMITIVE DONE 2026-07-11, DEVLOG 759; consumers next)

The exhaustive no-default `IBoundStatementVisitor<T>` fixed the *dispatch* consumers (6b/6d/6e/6f-OperandText) but is
the WRONG tool for the statement-tree WALKERS — `StoreKindOf` (6c) and the analyses `AlterCollectFields` /
`ContainsNextSentence` / `KeyedHasNextSentence`. A walker recurses into nested statements; forcing a `Visit` per node
does NOT stop it forgetting to RECURSE into a new node's children (the author writes `Visit(BoundNew) => false` just as
easily as forgetting a switch arm) — the exact PHASE-05 `UsageCollectionPass` completeness bug. The fix must centralize
+ drift-proof the *"child statements of node X"* knowledge, with completeness **by construction**.

**Child-statement taxonomy (verified 2026-07-11 — the FULL set; max containment depth = 1):**
- *Direct* `BoundStatement` / `IReadOnlyList<BoundStatement>?` props: `BoundIf.{Then,Else}`, `BoundInlinePerform.Body`,
  `BoundSequence.Steps`, `BoundEcChecked.Inner`, `BoundCallProgram.{OnException,NotOnException}`,
  `BoundKeyedDeleteFile.{OnException,NotOnException}`, `BoundRead.{AtEnd,NotAtEnd}`, `BoundWrite.{AtEop,NotAtEop}`,
  `BoundStringStmt.{OnOverflow,NotOnOverflow}`, `BoundUnstringStmt.{OnOverflow,NotOnOverflow}`,
  `BoundReturn.{AtEnd,NotAtEnd}` (+ any future direct phrase).
- *Single-depth helper records* (each holds `IReadOnlyList<BoundStatement>?` directly): `SizeErrorPhrase{OnError,NotOnError}`
  (arithmetic verbs' `.SizeError`), `KeyedInvalidKey{Invalid,NotInvalid}` (keyed I/O `.InvalidKey`).
- *Lists of helper records*: `BoundSearch.Whens` (`BoundSearchWhen.Statements`) + `BoundSearch.AtEnd`;
  `BoundEvaluate.Whens` (`BoundEvaluateWhen.Statements`) + `BoundEvaluate.Other`.

**Mechanism (BUILT): `BoundVisitorGenerator` emits `BoundStatementTree.StatementChildren(this BoundStatement) : IEnumerable<BoundStatement>`.**
Completeness is BY CONSTRUCTION — it reads every property via the semantic model, so it cannot forget one. Per-property
rule (recursive, `IsStatementLike` = IS-or-derives-`BoundStatement`; the list/child props are typed as the *root*
`BoundStatement`, so equality — not just derivation — must count): a prop that IS a statement → `One(x.P)`;
`IReadOnlyList<BoundStatement>` → `Nz(x.P)` (null→empty); a *statement-bearing record* → recurse its props (`x.P?.Q`);
`IReadOnlyList<record>` → `(x.P ?? Empty).SelectMany(e => …)`. (A "statement-bearing record" = a non-Bound-root record
in our assembly with ≥1 transitively-statement-yielding prop; a visited-set bounds the walk through cyclic data-model
types like `DataItem.Children`; the four helper records SizeErrorPhrase/KeyedInvalidKey/BoundSearchWhen/BoundEvaluateWhen
are picked up with NO hard-coding.) The emitted switch (28 container arms + `_ => []`) was verified to match the child
taxonomy above AND `StoreKindOf`'s hand-listed `Kids` calls exactly. `BoundStatementChildrenTests` locks it:
direct-children-returned, one-level-not-transitive, empty-leaf, and — the completeness/robustness guard — EVERY leaf
called on an uninitialized node returns empty without throwing (exercises each arm's null guards). Additive /
behavior-neutral (269 unit incl. 4 new + 32 char + 3158 conf; no consumer touched — like 6a). **NEXT: the consumers.**

**Consumers on it:** `ContainsNextSentence`/`AlterCollectFields`/`KeyedHasNextSentence` become a generic recurse over
`StatementChildren` + node-specific collection (a new node's children recurse automatically). `StoreKindOf` (6c) stays
a per-node `StoreKindVisitor` (its polarity IS node-specific — see the 6c bullet), but its `Kids` recursion rides
`StatementChildren`, so the RECURSION completeness is guaranteed and only the per-node polarity is hand-authored (the 9
`_ => null` nodes stay `null`). **Rejected:** a `Bound{Root}Walker` base with fixed aggregation — the consumers
aggregate differently (OR-short-circuit / first-hit / accumulate), so the flexible shared core is the `StatementChildren`
*primitive*, not a fixed-fold base. **Rejected:** a hand-written single-source `StatementChildren` — loses
completeness-by-construction (a reflection test can't verify a hand-written body without materializing instances).

### Step 7 — Semantic normalization: `MoveKind` + storage form onto `BoundMove`; delete `ConvertSource` re-classification

- **Files:** edit `Binding/Bound/BoundTree.cs` (`BoundMove` record), `Binding/Procedure/Verbs/MoveBinder.cs`
  (was `BindMove` + `MoveFigurative`), `CodeGen/Verbs/` `EmitMove`/`ConvertSource`.
- **Change:** Add `MoveKind Kind` and `StorageForm TargetForm` to `BoundMove`
  (`BoundMove(IReadOnlyList<Place> Targets, BoundOperand Source, MoveKind Kind, StorageForm TargetForm)`). Compute
  `Kind` ONCE in the binder (the classification currently at `CSharpEmitter.cs:481-501` + `ConvertSource`'s
  category switch): group vs elementary-alphanumeric vs elementary-numeric vs numeric-edited vs alpha-edited vs
  figurative-fill vs figurative-to-numeric-image vs ref-mod-slice. `EmitMove` becomes a pure renderer that
  `switch (m.Kind)` and reads `m.TargetForm` — it NO LONGER reads `DataItem.StoreAsImage`/`IsStrongGroup` or calls
  `ConvertSource`'s re-classifier. Delete `ConvertSource`'s category-derivation switch (`CSharpEmitter.cs:714-...`);
  the per-kind rendering (edit-mask format, figurative fill, image write) moves into per-`MoveKind` render helpers
  keyed off the node.
- **Why:** Completes the bind-once contract `BoundTree.cs:7-13` promises (rationale #2 /
  `DESIGN-binder-bound-tree.md §3.3`). This is the FIRST removal of a `DataItem.StoreAsImage` emit-time read — the
  storage form now travels on the node (`TargetForm`, from P5/P6's `StorageFormPass`).
- **Verify:** battery 1+2+4 green. Probe the MOVE edge cases explicitly:
  ```bash
  # group MOVE, numeric-edited MOVE, figurative-to-numeric fill, ref-mod-slice MOVE — all must be byte-identical.
  dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/move_*.cob --std 2002 -o /tmp/m.dll --run
  ```
  The emitted-C# snapshot for MOVE-heavy programs must be reviewed-neutral (bodies moved, output identical).
- **COMMIT:** `P7 step7: BoundMove carries MoveKind+StorageForm; EmitMove is a pure renderer; delete ConvertSource re-classification`

### Step 8 — Retire the remaining emitter reads of `DataItem.StoreAsImage`; delete `MarkStoreAsImage` (prove-then-delete)

- **Files:** edit every `CodeGen/` reader of `StoreAsImage` (the ~20 sites from
  `grep StoreAsImage src/Cobol.Net.Compiler/CodeGen`: `CodeGen/Verbs/AcceptDisplayEmitter.cs:67,128` (renamed at Step 5), `CSharpEmitter.cs:531,574,
  664,803,1119,1123`, `CSharpEmitter.Inspect.cs:100`, `CSharpEmitter.Oo.cs:662,821`, `CSharpEmitter.Call.cs:530,940`,
  `CSharpEmitter.ReportWriter.cs:62`, …); delete `MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), the `CompilerTempClones`
  re-sync (`CSharpEmitter.Call.cs:111-120`), the OO re-sync (`CSharpEmitter.Oo.cs:694-697`).
- **Change:** Each emitter read of `item.StoreAsImage` becomes a read of the storage form carried on the bound
  node/`Place` (`place.Item` still exists but its store decision is now the immutable `StorageForm` from
  `StorageFormPass`, read via `item.Storage == StorageForm.CharImage`, an **init-only** fact — NOT a mutable flag).
  **Prove-then-delete:** first land `StorageFormPass` reads ALONGSIDE the still-running `MarkStoreAsImage`, add a
  temporary corpus-wide assert that `item.Storage==CharImage` ⟺ the old `StoreAsImage` for every item across the
  whole conformance corpus, run the full battery to prove equivalence, THEN delete `MarkStoreAsImage` and the
  re-syncs and the mutable setter.
- **Why:** Removes the last emitter→binder write-back and the 7-site mutable flag (rationale #2/#4,
  `DESIGN-binder-bound-tree.md §3.4`, `DESIGN-data-model.md §1.1`). This is the highest-value latent-bug removal in
  the phase (R2 risk — `StoreAsImage` has accreted SORT-SD/FILE-whole-group/compiler-temp special cases;
  `StorageFormPass` must reproduce ALL of them, which the cross-check verifies before deletion).
- **Verify:** battery 1+2+4 green; the temporary equivalence assert passes across the whole corpus before deletion;
  after deletion, `grep StoreAsImage src/Cobol.Net.Compiler/CodeGen` returns ONLY comments (or nothing).
  *(AS RECONCILED, P7 Steps 7+8 / DEVLOG 793: this step was DONE BY P5.7 — flag/write-back/re-syncs deleted, the
  corpus assert ran and retired. The remaining CodeGen `StoreAsImage` READS are the read-only projection of
  `Storage` — the intended "init-only fact" read, encapsulated — so the grep criterion is SATISFIED in spirit and
  retired as written.)*
- **COMMIT:** `P7 step8: read StorageForm off the node; delete MarkStoreAsImage write-back + StoreAsImage re-syncs (prove-then-delete)`

### Step 9 — Decompose `CSharpEmitter` into `ProgramEmitter` + `StatementEmitter` + per-verb emitters + `DataEmitter`

Incremental — one verb group per sub-commit.

> **AS-BUILT PLAN (Exec Step D, 2026-07-11 — decision-complete, from the fresh 16-file coupling census
> `wf_d677d614-5fb`; supersedes the sketch below where noted).** The census established: the hub edges are
> `EmitMove` (← Call/KeyedIo/Sort/Corresponding/Initialize), `EmitStatementList` (← Call/KeyedIo/Sort/String/
> Evaluate/EC), `StoreArith`+`EmitArith` (← KeyedIo/Sort/String/Inspect/Corresponding), `EmitUseHook`/
> `EmitStoreFileStatus`/`EmitImageInto` (← KeyedIo/Sort), `ConvertSource` (← ReportWriter); counters are file-local
> except `_storeTmpCounter` (core+Oo) and `_ecCounter` (core+EC+Call); the EC scratch trio
> (`_sizeErrVar`/`_sizeErrEcVar`/`_ecInfo`) interlocks the Exceptions partial with arithmetic; `Dispatch.cs` reads
> only `_ctx`/`_num`/`_currentPc`/`_sentenceEndLabel`. The EC↔statement cycle (`EcEmitChecked` calls
> `EmitStatement`; statements contain EC-checked children) means NO pure bottom-up extraction order exists — a
> composition root must wire the cycle.
>
> **Sub-commit sequence** (each battery 1+2+4 green + the ratchet; the commit issued as a SEPARATE action after
> reading the verdict line — the DEVLOG-792 rule):
> - **9a `NameAllocator`** — the 15 run-unit unique-name counters move to the ONE allocator the DESIGN §2.5 table
>   already plans (15 distinct `Next*()` methods — per-counter sequences preserved byte-exactly, the audit's
>   counter risk). ONE instance per run unit, threaded onto every per-unit `EmitContext` as `Names` (the 3
>   construction sites: Call.cs program-class, Oo.cs class-unit, Oo.cs interface-unit).
> - **9b typed state objects** — the scattered cross-partial mutable fields become three per-scope objects (the
>   audit's explicit-threading risk): `DispatchState` (per unit: `CurrentPc`, `SentenceEndLabel`, `DispatchName`,
>   `UseDecls`, `OuterGlobalUse`), `EcState` (run-unit `EcActive`; per-unit `UnitHasF3/F4`; statement-scoped
>   `Info`/`SizeErrEcVar`/`SizeErrVar` — the EC↔arith interlock travels as ONE object), `CallUnitState` (per unit:
>   `SelfPath`, `ReturningPlace`, `InheritedStatusPlace`). These ARE the end-state (multiple readers each), not
>   interim churn.
> - **9c–9k per-verb extraction, low-coupling first**; each sub-commit ALSO migrates that file's bare-`Cobol*.`
>   sites to `RuntimeApi` (its ratchet entry → 0, then deleted): 9c Evaluate+Initialize+Corresponding+AlterSwitch+
>   AcceptDisplay (real class; `EmitDisplay` MOVES IN — the Step-5 filename becomes honest) · 9d Inspect+String+
>   Ptr · 9e KeyedIo+Sort · 9f ReportWriter · 9g Move (MOVE family + `ConvertSource`, which STAYS the shared
>   Convert renderer for RW/INITIALIZE per Step 7's deviation) · 9h Arithmetic (incl. the `EmitArith`/`StoreArith`
>   service; the EC size-handling seam) · 9i ControlFlow (IF/PERFORM/SEARCH/GO TO DEPENDING) + Set (SET family) ·
>   9j SequentialIo (registration, OPEN/CLOSE/UNLOCK/WRITE/READ/REWRITE, LINAGE, `EmitUseHook`/
>   `EmitStoreFileStatus`/`EmitImageInto` — the file-I/O common service KeyedIo/Sort consume) · 9k Ec (the
>   Exceptions partial; owns `EcState`) + Call (the CALL/CANCEL/GOBACK/EXIT PROGRAM verb half of Call.cs).
> - **9l** FieldEmitter → `DataDivision/` split (the 4 named classes; its 5th concern — the physical-field
>   model+memoization — gets its home decided at execution, recorded here).
> - **9m** OoEmitter — the OO EMIT half ONLY; the bind half stays on `CSharpEmitter` (the P6→P9 `IOoBindHost` seam).
> - **9n (final)** ProgramEmitter (module preamble + unit loop + program-class emission + entry wrapper) +
>   DispatchEmitter (`__Dispatch`/pc cases/ALTER fields/USE machinery/paragraph-sentence bodies; parameterized over
>   program AND class units — the `__MDispatch` swap rides `DispatchState.DispatchName`) + StatementEmitter (the
>   79-Visit `IBoundStatementVisitor<bool>` + `EmitStatement`/`EmitStatementList`). **DEVIATION from this step's
>   original "CSharpEmitter is gone" slogan: `CSharpEmitter` SURVIVES as the thin bind-host facade** — `Bind`/
>   `EmitBound` entries + `IOoBindHost` + the OO bind bodies + the run-unit bind-session state
>   (`_bindSession`/`_turnState`/`_ooClasses`/`_ooIfaceData`/`_ecActive`) — until P9 relocates the OO bind bodies
>   into the binder (the audit's host-survival constraint); `BackendFactory`'s interim `(id, bindHost)` signature
>   reconciles then, not before.
>
> **Migration wiring:** during 9c–9m each extracted emitter is constructed PER UNIT (at the 3 sites where the
> `_ctx`/`_num`/`_cond`/`_refs` quadruple is re-newed) taking `(EmitContext, the renderers/state it needs,
> CSharpEmitter host)` and reaches not-yet-extracted hub methods through the host (internal) — the host IS the
> composition root while methods remain on it. At 9n the host edges retarget to the real collaborators, wired by
> the per-unit composition root `UnitEmitters` (cyclic edges — StatementEmitter↔verbs↔EC — property-wired by the
> root post-construction). The transitional host edge is this doc's own incremental-sub-commit ordering, not a
> kept pattern — 9n deletes it. The renderers stay under `Emit/` this step (their `Roslyn/` relocation is the
> Step 11 topology move); new emitter classes go to `CodeGen/Verbs/` (verbs) and `CodeGen/` root (spine), all in
> the flat `CobolNet.CodeGen` namespace per the existing convention.
>
> **9n AS LANDED (`22911690` + the `72ec6633` sweep; DEVLOG 809–810).** The composition root's construction
> discipline: `UnitEmitters`' ctor builds ctx/renderers + all 20 collaborators in ACYCLIC ctor order and
> property-wires only the cyclic edges (each verb's `Statements`; `Ec.Statements`; `KeyedIo.SeqIo` — SeqIo's
> ctor already takes KeyedIo for the shared READ bodies, so the reverse edge cannot be a ctor argument);
> `StatementEmitter`'s ctor COPIES direct refs out of the root (every collaborator exists by then), so dispatch
> reads fields, never a hub. `ProgramEmitter` owns the run-unit state (`NameAllocator` + the three Step-9b
> state objects are its fields now — true run-unit lifetime) and `OoEmitter` reads the per-unit set through
> the LIVE `ProgramEmitter.Current` (the 9m hazard preserved); the OO class table + interface forests come
> from the immutable `BoundCompilation`, so emission touches ZERO bind-host session state. Deviations beyond
> the planned host survival: (a) the dead `Turn` accessor and the emit-time `_turnState`/`_ooClasses` restores
> were deleted with the Call partial (no emit-time reader existed — grep-proven); (b) `EmitInterfaceUnit`
> passes `null!` for the resolver — interfaces emit FIRST, before any unit begins, so no live resolver exists
> and none is consulted (the pre-9n code passed the host's null/stale `_refs` FIELD; the live-accessor rewrite
> turned that into an NRE the conformance net caught pre-commit — DEVLOG 809).

- **Files:** create `CodeGen/ProgramEmitter.cs`, `CodeGen/DispatchEmitter.cs`, `CodeGen/StatementEmitter.cs`,
  `CodeGen/Verbs/*Emitter.cs`; split `CodeGen/Emit/FieldEmitter.cs` →
  `CodeGen/DataDivision/{RecordStructEmitter, GroupImageCodec, GroupValueSlicer, ValueInitializer}.cs`.
- **Change:** Convert the 15 `CSharpEmitter` partials into real classes over the immutable `EmitContext`
  (from Step 3), invoked by `StatementEmitter` (the `IBoundStatementVisitor<bool>` from Step 6b). `ProgramEmitter`
  owns per-unit type + `__Dispatch` + entry wrapper; `DispatchEmitter` owns the PC dispatcher (`__Dispatch`, pc
  cases, ALTER fields). Move `FieldEmitter` out of `CodeGen/Emit/` (it is a DATA-DIVISION emitter, not a value
  renderer) and split its ≥4 concerns. Each per-verb partial (`CSharpEmitter.KeyedIo.cs`, `.Sort.cs`, `.Inspect.cs`,
  `.StringUnstring.cs`, `.ReportWriter.cs`, `.Call.cs`, `.Oo.cs`, `.Initialize.cs`, `.Ptr.cs`, `.Evaluate.cs`,
  `.Corresponding.cs`, `.AlterSwitches.cs`, `AcceptDisplayEmitter.cs`) maps 1:1 to a `Verbs/*Emitter` class taking
  `EmitContext` + the renderers by ctor injection. **The OO bind-orchestration** that P6 already extracted from
  `CallEmitRunUnit` does NOT reappear here — `ProgramEmitter` only emits already-bound units.
- **Why:** Kill the god class; real class boundaries stop the accidental shared-private-state coupling
  (rationale #4 / `DESIGN-codegen-backend.md §2.5, M5`).
- **Verify:** battery 1+2+4 green after EACH verb group (purely structural relocation — output identical).
- **COMMIT (per group):** `P7 step9X: extract <Verb>Emitter from CSharpEmitter over immutable EmitContext`
  and a final `P7 step9-final: CSharpEmitter is gone; ProgramEmitter/StatementEmitter + Verbs/* remain`.

### Step 10 — Decompose `StatementBinder` into collaborators over `BinderContext`; lift edition-invariant SR checks to `StatementValidation`

Incremental — one collaborator/verb group per sub-commit.

> **AS-BUILT PLAN (Exec Step D, 2026-07-11 — decision-complete, from the fresh 20-file coupling census
> `wf_b788936b-ca2`; the full per-file census is `p7-step10-census.json`, session 61dab794; supersedes the
> sketch below where noted). Landed so far: 10a `34410d7f` (SymbolTable residuals) · 10b `a01b5c77`
> (`PhraseBlocks`). Batch sequence (each battery-1+2+4-green + verdict-gated; Step-9-style transitional host
> edges through `StatementBinder`, deleted at the final wiring):**
> - **10c** `BinderContext` + `StatementValidation` scaffolds + `Verbs/InspectBinder` (the census's cleanest
>   file proves the pattern; 0846/0847 error-halves lift as PURE checks; the 0845 BACKWARD gate moves
>   VERBATIM). **10d** StringUnstring + Evaluate (pure leaves). **10e** Initialize + Move (absorbs
>   .MoveFigurative — the `MarkImageForced` firing points move verbatim) + Corresponding (the ONE
>   `_corrCounter` owner; its `:219` ≥2002 pair-selection window STAYS in-binder — BEHAVIORAL semantics, not
>   a gate; documented as a sanctioned in-binder edition read). **10f** ReportWriter + the ResolveReceiving
>   hoist. **10g** FileLock + Ptr (the raw `PtrResolveBased` ByName bypass stays byte-identical — the Symbols
>   convergence is a FLAGGED follow-up, like DEVLOG 773). **10h** SequentialIo + KeyedIo as ONE batch (the
>   emitter-mirror cycle) + the SPECIAL-NAMES mnemonic registry onto ctx + AcceptDisplayBinder detaches from
>   the partial class. **10i** Sort. **10j** Call (Oo/Ec host hooks outlive the batch). **10k** Intrinsic +
>   Udf together (bidirectional §12.3.8.2 GR12 coupling; the D8 catalog windows + the documented
>   `ConstructRegistry.Check` exception move VERBATIM; `CompileClock` relocates with same-commit test
>   updates; the recursive-descent arg parser stays one block for Step 12's deletion). **10l** Perform + If +
>   ControlFlow (the audit-extension class mirroring `ControlFlowEmitter` — topology row added). **10m** Set +
>   Search. **10n** SetAlter (the lazy ALTER prepass reads the procedure table via host edges until 10t).
>   **10o** `ConditionBinder` + the .Boolean partial in the SAME batch (bidirectional; `AbbrevCarry` hoists;
>   `CheckedRelational` stays THE one checkpoint; the 1511 checks + relation SR bodies lift as pure checks).
>   **10p** Arithmetic. **10q** `ExpressionBinder` — the expression-spine flip (operand + receiving families;
>   `CheckLiteral` carries its digit-cap window AS-IS — the sanctioned binder-side pattern). **10r** `EcBinder` +
>   `EcBindState` on ctx (the 7 feature bits + PD-RAISING + `_declEcPairs`; `EcWrap` still invoked at the host
>   BindStatement exit; the hand-rolled intrinsic-presence walks move verbatim, generated-visitor conversion
>   FLAGGED as a behavior-sensitive follow-up). **10s** `OoBinder` LAST behind the OO goldens + method-scope
>   tests: scoped `ctx.EnterMethodScope` replaces the ambient ordered quadruple mutation; the OO host state
>   stays behind `IOoBindHost` until P9. **10t** FINAL WIRING — `ProcedureTableBuilder` (+ the .Declaratives
>   half) as ONE table+cursor model (`SectionInfo` hoists; the `_paras ∥ _paraSection ∥ _paraMethod` lockstep
>   preserved), thin StatementBinder = dispatch switch + mark/drain wrap protocol + composition root, ALL host
>   edges deleted, full doc sweep (topology reconciliation per [[feedback_propagate_reconciliations]]:
>   `.Boolean`→ConditionBinder and `.Exceptions`→EcBinder — the phase doc wins over the stale topology row;
>   If/Evaluate stay SEPARATE).
> **BinderContext as-built shape** (supersedes the sketch's `Parse`/`SymbolTableBuilder`/`IDiagnosticSink`
> names): ONE per unit/class-roster; CORE = `Data` (DataBinder) · `Refs` · `Edition => Data.Edition` (the ONE
> sink AND edition surface — EditionContext as-built) · `Symbols => Data.Symbols` · `ActiveScope`
> (recomputed per read, never captured) · `Options => Data.Options` · `InNestedProgram`; PER-UNIT STATE =
> `EcBindState` · `BindCursor` (CurrentBindPc/CurrentSection/CurrentMethodScope — one cursor model; scoped
> EnterMethodScope at 10s; table ownership → ProcedureTableBuilder at 10t) · the OO host state behind
> `IOoBindHost` · `MnemonicRegistry` (10h); SPINE SERVICES as transitional host delegates flipped per batch
> (`Blocks`/`Expr`/`Cond`/`ResolveProcedure`/`EcWrap`); COLLABORATOR HANDLES registered as they land (ONE
> instance each — counters/memos are per-unit lifetime). **StatementValidation convention (fixed at 10c):**
> `Check*` methods report to `ctx.Edition` and return bool — the verb binder owns the error+placeholder
> control flow (PURE checks only lift). **Residual edition gates move VERBATIM with their verbs — the
> pass-folding is Exec Step E's scope (recorded; the census enumerates the ~19 sites).**

- **Files:** create `Binding/Procedure/{BinderContext, ProcedureTableBuilder, ExpressionBinder, ConditionBinder, PhraseBlocks}.cs`,
  `Binding/Procedure/Verbs/*Binder.cs`, `Binding/Validation/StatementValidation.cs`; move the 21 `StatementBinder.*`
  partials into these classes.
- **Change:** `BinderContext` is the shared spine (`Parse`, immutable `EditionInfo`, the ONE `IDiagnosticSink`,
  `SymbolTableBuilder`, `ReferenceResolver`, `RecordLayout`, scoped `EnterMethodScope` push/pop replacing the
  ambient `ActiveMethodScope` mutation) — per `DESIGN-binder-bound-tree.md §3.5`. Convert the partials verb-by-verb:
  `ProcedureTableBuilder` (paragraphs/sections/declaratives/pc + `ResolveProcedure`), `ExpressionBinder`,
  `ConditionBinder` (incl. `AbbrevCarry`/`CheckedRelational`), and `Verbs/{Move,Arithmetic,If,Perform,KeyedIo,
  SequentialIo,Sort,String,Inspect,Initialize,Intrinsic,Udf,ReportWriter,Call,Set,Oo,Evaluate,Search,AcceptDisplay}Binder`.
  Lift every inline edition-invariant SR (semantic) check (MOVE figurative/category rules, composite-of-operands,
  boolean/class/pointer relation rules) into `StatementValidation.Check*(...)` which reports to the
  `IDiagnosticSink` — the binder calls `validation.CheckMove(...)` and stays about *producing bound nodes*
  (`feedback_binder_no_ir` spirit). **Edition gating is NOT lifted here**: the `VersionConformancePass` is the sole
  edition-gating funnel and the binder is edition-agnostic (`DESIGN-version-conformance-pipeline.md`); if the
  decomposition uncovers any residual inline edition gate, it relocates into the pass, never into
  `StatementValidation`. Extract `PhraseBlocks.BuildPair(blocks, notFirst)` — the ON/NOT-ON extractor
  (~8 clones: `KeyedIo.cs:104,230,322`, `StringUnstring.cs:198`, `Sort.cs:330`, `Call.cs:154`,
  `StatementBinder.cs:364,400`) → ONE helper.
- **Why:** Kill the second god class; one canonical phrase-block helper and one validation home (rationale #1/#4,
  `DESIGN-binder-bound-tree.md §3.5`). `BindStatementCore` shrinks to the parse-dispatch `switch` + shared helpers.
- **Verify:** battery 1+2+4 green after each verb group. OO shadowing is the sensitive spot — do the `OoBinder` +
  scoped `EnterMethodScope` conversion LAST, behind the OO conformance goldens + method-scope unit tests (R1).
- **COMMIT (per group):** `P7 step10X: extract <Verb>Binder from StatementBinder over BinderContext; lift edition-invariant SR checks to StatementValidation`.

### Step 11 — Structural `Place` + `PlaceRenderer` (highest risk — subtype at a time) ✅ COMPLETE

> ✅ **DONE (2026-07-12, DEVLOG 834–838).** All eight `Place` subtypes are structural over an `AccessPath`
> (`Binding/Model/AccessPath.cs`), rendered by `CodeGen/Roslyn/PlaceRenderer.cs`; `Place.Read()/Write()` are deleted
> and `PlaceNeutralityTests` (R5) locks G4. Landed 11a (seam + route ~124 consumers) · 11b (Renames/Odo) · 11c/d
> (RefMod/NumericImage) · 11e (the leaf tier — Member/DynTable/RedefView/Capacity + the `AccessPath` builders +
> `CorrespondingBinder`/`CorrespondingHoist`/`InitializeBinder`/`ProgramEmitter` construction sites, plus the forced
> `BoundSetCapacity`/`BoundSearch` binder-side-leak fixes) · 11-final (delete + R5). Battery byte-neutral throughout
> (char 33/33, unit 282/282, conf 3166/3166). **Deviations recorded:** `PlaceRenderer` is a STATIC class (not
> `(EmitContext, RuntimeApi)` — rendering is context-free); `DynTablePlace` KEPT distinct (not folded into
> `MemberPlace`) to bound blast radius; per-segment `AccessDir` DROPPED (polarity is a render-time choice); **the
> subscript / ref-mod / offset index expressions stay transitional STRINGS, deferred to D10/PHASE 15 (owner decision,
> DEVLOG 836)** — so the R5 test bans render METHODS, not the transitional string properties. The plan below is the
> as-executed record.

MULTI-SUB-COMMIT. This is the riskiest step; do it LAST, after the visitor + decomposition made consumers few and
well-typed. The P0 differential/snapshot harness "earns its keep" here — output MUST be byte-identical pre/post each
subtype.

**Execution plan (decided; refines the sketch below on two points).**
- **`PlaceRenderer` is a STATIC class** (`CodeGen/Roslyn/PlaceRenderer.cs`), NOT `PlaceRenderer(EmitContext ctx,
  RuntimeApi rt)`. Every current `Place.Read()/Write()` is context-free (self-contained on the `Place`'s own fields),
  and `RuntimeApi` is static, so no `EmitContext` is threaded. It lives in `CodeGen`; the binder NEVER references it
  (a `Binding → CodeGen` call would invert the layering the seam enforces).
- **Migration = route consumers FIRST, then migrate subtypes.** Because `Place` is in `Binding` and `PlaceRenderer`
  in `CodeGen`, `Place.Read()` cannot delegate to `PlaceRenderer` (layer inversion). So the shim lives INSIDE
  `PlaceRenderer` (default arm → the legacy `p.Read()`), and consumers are routed `p.Read()` → `PlaceRenderer.Read(p)`
  FIRST (each routing is byte-identical); THEN each subtype is converted to structure one at a time (its fields + an
  explicit `PlaceRenderer` arm + its construction sites in `ReferenceResolver`/`CorrespondingBinder`/
  `InitializeBinder`/`OdoModel`); FINALLY `Place.Read()/Write()` are deleted and the R5 neutrality test lands.
- **⛔ LEAF-TIER COUPLING (scouted 2026-07-12 — the leaf subtypes are ONE atomic commit, not four).** `MemberPlace`,
  `DynTablePlace` (eliminated — folded into `MemberPlace` with a `DynTableSegment` + render-time Sending/Receiving
  polarity), and `RedefViewPlace` must migrate TOGETHER because three binder-side path-rendering sites consume all
  three by string: (1) **`CorrespondingBinder`** — `CorrAccess` builds child `MemberPlace`/`RedefViewPlace` by string
  concatenation and the anchor hoist is `CorrespondingHoist(string Local, string Init, bool IsRef)` — a BOUND-NODE
  (`Bound/BoundCorresponding.cs`) whose `Init` C# string (`m.Path` / the view offset) `CorrespondingEmitter` emits as
  `ref var {Local} = ref {Init};`; migrating `MemberPlace` requires `CorrespondingHoist.Init` to carry structure
  (a `Place`/`AccessPath`, rendered at emit) + the emitter change, AND the `m.Path.Contains("CobolTable.At(")` sniff →
  `m.Path.Segments.Any(s => s is FixedTableSegment or DynTableSegment)`. (2) **`InitializeBinder`** — the
  `Initialize{Member,Dyn,View}Cursor` records accumulate string paths (`"{Path}.{child}"`, `CobolTable.At({Path},…)`)
  and `ToPlace()` builds the three subtypes → the cursors must accumulate `AccessPath` segments. (3)
  **`ProgramEmitter.CallPrefixPlace`** re-roots a `MemberPlace`/`RedefViewPlace` by `prefix + m.Path` → structural
  segment-prepend. Plus the two binder-side C#-string LEAKS (`OdoModel.SearchBound`'s `dp.Read()` +
  `TablePath`-string; the sniff). `CapacityRegisterPlace` (whole-table path, no subscript) can ride the same
  `AccessPath` build. NET: the leaf tier is a single ~10-file atomic change with byte-exact CORRESPONDING/INITIALIZE
  emit — a dedicated focused effort, not a quick sub-step.
- **⛔ TOP-DOWN migration order (a load-bearing constraint).** An UN-migrated wrapper subtype renders by calling its
  inner's `Place.Read()/Write()` directly (it is in `Binding`, so it CANNOT route through `PlaceRenderer`). Therefore a
  subtype may replace its legacy `Read()/Write()` with the throwing tripwire ONLY once **nothing un-migrated wraps
  it** — else the un-migrated wrapper's internal `Inner.Read()` hits the throw (this bit as NC224A: a still-legacy
  `RefModPlace` over a `NumericImagePlace`). So migrate OUTERMOST-first down the wraps-DAG: top-level results
  (`RefModPlace`, `RenamesPlace`, `OdoGroupPlace` — none is ever an `Inner`) → `NumericImagePlace` (wrapped only by
  `RefMod`/`Renames`) → the leaves (`MemberPlace`, `RedefViewPlace`, `DynTablePlace`, `CapacityRegisterPlace`). A
  migrated wrapper's `PlaceRenderer` arm routes its inner through `PlaceRenderer.Read`, so once every wrapper is
  migrated no `Inner.Read()` chain reaches a leaf, and the leaves can change fields + drop their legacy render.
- **Sub-step order:** **11a ✅** route all CodeGen consumers → `PlaceRenderer`. **11b ✅** the two top-level subtypes
  with no field change and no index need — `RenamesPlace` + `OdoGroupPlace`. **11c ✅** `RefModPlace` (render moved to
  `PlaceRenderer`; Start/Length kept as the D10 transitional string carrier). **11d ✅** `NumericImagePlace` (unblocked
  once `RefMod`/`Renames`, its only wrappers, were migrated). ALL FOUR DECORATORS DONE. NEXT: the leaf subtypes —
  `MemberPlace` (the `.`-joined access path + `CobolTable.At` table accessors move to `PlaceRenderer`; the field-name
  chain becomes a structural `AccessPath` of segments, each table segment carrying its subscript as the transitional
  string), `DynTablePlace` (fold into the path with render-time Sending/Receiving polarity), `RedefViewPlace` (backing
  → a structural backing-field access; offset the transitional string), `CapacityRegisterPlace` (whole-table path,
  no subscript → fully structural) — with their `ReferenceResolver`/`CorrespondingBinder`/`InitializeBinder`
  construction sites. Then the binder-side C#-string leaks (`BoundSearch.DependCount` via `OdoModel.SearchBound`; the
  `CorrespondingBinder` `m.Path.Contains("CobolTable.At(")` string-sniff → a structural has-index-segment query) →
  delete `Place.Read()/Write()` + the R5 neutrality test.
- **⛔ Subscript index expressions stay a TRANSITIONAL STRING carrier (owner decision) — deferred to D10/PHASE 15.**
  A subscript / ref-mod position is a flat SUBSCRIPT-mode token stream `ReferenceResolver.RenderSegment` renders to a
  C# `long` expression; there is NO `BoundExpr` path for it, and the mechanism that would give one (removing the
  SUBSCRIPT lexer mode) is the D10 sub-track owned by PHASE 15 §"CUT 2.5" (blocked until the legacy cutover).
  Producing a `BoundExpr` subscript now would mean HAND-ROLLING a subscript parser — the very anti-pattern D10/Step 12
  remove. So Step 11 moves the runtime-call text (`SpliceInto`/`RefMod`/`FormatDisplay`/`CobolTable.At`) and the
  access-path composition off the bound tree into `PlaceRenderer` NOW, while a subscript index stays the rendered
  `string` it is today (a `Place`/segment field, byte-exact); the subscript→`BoundExpr` conversion FOLDS INTO D10
  when the flat-token mechanism is actually deleted. The R5 neutrality test is therefore scoped to string-returning
  RENDER METHODS (which are gone), not the deferred subscript-string fields. (See `project_phase04_d10_deferral`.)
- **`FixedTableSegment`/`DynTableSegment` carry NO fixed `AccessDir`** (refines the sketch): direction is
  operation-driven at render time — `PlaceRenderer.Read` picks `CobolTable.At`/`RefSending`, `PlaceRenderer.Write`
  picks `CobolTable.At`/`RefReceiving`. `DynTablePlace`'s two precomputed paths fold into ONE structural path; a fixed
  per-segment `Dir` would mis-handle a receiver that is also read (e.g. `ADD 1 TO A(I)` over a dynamic table).
- **`RedefViewPlace`'s backing** is a synthesized backing-field access (the `_redef_X` class member has NO `DataItem`,
  reached via `ReferenceResolver.BackingPath`, optionally displaced by `CobolPtr.OffsetOf` for a BASED class), so the
  sketch's `Place Backing` is realized as a backing-field segment, not a full `Place`.

- **Files:** edit `Binding/Model/Place.cs`; create `CodeGen/Roslyn/PlaceRenderer.cs`; edit every `Verbs/*Emitter` +
  `ExpressionRenderer`/`ConditionRenderer` reader of `place.Read()/Write()`.
- **Change:** Introduce the structural shapes (`DESIGN-codegen-backend.md §2.3`, `DESIGN-data-model.md`):
  ```csharp
  public abstract record Place { public abstract DataItem Item { get; } public abstract PicInfo? Pic { get; } }
  public sealed record AccessPath(IReadOnlyList<AccessSegment> Segments);
  public abstract record AccessSegment;
  public sealed record RootFieldSegment(string CsField) : AccessSegment;
  public sealed record MemberSegment(string CsMember) : AccessSegment;
  public sealed record IndexSegment(BoundExpr ZeroBased) : AccessSegment;
  public sealed record FixedTableSegment(BoundExpr OneBased, AccessDir Dir) : AccessSegment;
  public sealed record DynTableSegment(BoundExpr OneBased, AccessDir Dir) : AccessSegment;
  public sealed record MemberPlace(AccessPath Path, DataItem MemberItem) : Place;
  public sealed record RefModPlace(Place Inner, BoundExpr Start, BoundExpr? Length) : Place;
  public sealed record RedefViewPlace(Place Backing, BoundExpr ZeroBasedOffset, int Width, DataItem ViewItem) : Place;
  public sealed record NumericImagePlace(Place Inner) : Place;
  public sealed record RenamesPlace(IReadOnlyList<Place> Leaves, DataItem AliasItem) : Place;
  public sealed record CapacityRegisterPlace(Place Table, DataItem RegisterItem) : Place;
  ```
  `PlaceRenderer(EmitContext ctx, RuntimeApi rt)` owns `string Read(Place)`, `string Write(Place, string rhs)`,
  `string WriteFill(RefModPlace, string fillChar)` — moving the ENTIRE current `Place.cs` render logic (the
  `MemberPlace`→assignment, `RedefViewPlace`→`SpliceInto`, `NumericImagePlace`→`FormatDisplay`/`StoreDisplay`,
  `DynTablePlace`'s two-string polarity via `AccessDir`, `CapacityRegisterPlace`'s read-only view) into it, ALL
  via `RuntimeApi`. `CapacityRegisterPlace.Write` becoming an ICE is expressed by `PlaceRenderer.Write` rejecting
  it (the throwing `Write` override on the record disappears).
- **Migration (per subtype, behind a shim):** convert ONE `Place` subtype at a time. Keep a temporary
  `PlaceRenderer.RenderReadLegacy(Place)` bridge that calls the OLD `Place.Read()` for un-migrated subtypes so the
  tree compiles throughout. When all subtypes and all verb emitters consume `PlaceRenderer`, delete the shim and the
  `Read()/Write()` abstract members. `ReferenceResolver` (P6-thinned) now builds STRUCTURAL places (root item +
  `BoundExpr` subscripts) — the binder produces NO C# strings.
- **Why:** Backend-neutral bound tree (the G4 invariant); the blocking enabler for a future CIL backend and the fix
  for the leaky-emitter/duplication class (rationale #3 / `DESIGN-codegen-backend.md §2.3, M7`,
  `DESIGN-binder-bound-tree.md §3.3`).
- **Verify:** battery 1+2+4 green after EACH subtype. The emitted-C# snapshots are the primary guard — byte-identical
  pre/post. Add the neutrality reflection test (`DESIGN-codegen-backend.md §6 R5`): assert no `Place` subtype and no
  bound node exposes a `string`-returning render method (keeps G4 enforced even without a live CIL backend).
- **COMMIT (per subtype):** `P7 step11X: structural <Subtype>Place → PlaceRenderer (no C# text in the bound tree)`
  and a final `P7 step11-final: delete Place.Read()/Write() + the legacy shim; add neutrality test`.

### Step 12 — Delete the `IntrinsicRenderer` static channel (parse FUNCTION args as real expressions)

- **Files:** edit `Grammar/Core/CobolExpressions.g4` (FUNCTION-arg rule) — a SHARED `.g4` change → **full legacy
  guard required**; edit `Binding/Procedure/Verbs/IntrinsicBinder.cs` (was `StatementBinder.Intrinsics.cs`); delete
  `IntrinsicRenderer`'s `NumStatic/NumStaticExpr/StaticAdditive/StaticMul` (`IntrinsicRenderer.cs:353-380`).
- **Change:** Make FUNCTION arguments parse as real `arithmeticExpression`s (a superset-grammar rule — no
  parse-time edition predicate) so the
  hand-rolled recursive-descent arg parser (`Intrinsics.cs:686-839` — `ParseAdditive/Multiplicative/Power/Unary/
  ArgPrimary`) is DELETED and args bind through the ONE `ExpressionBinder`/`BindExpr`. The `IntrinsicRenderer` then
  calls the ONE `ExpressionRenderer` via a default `ReceiverContext` (Step 3 enabled this) — delete the parallel
  static channel. **If the grammar change is too large/risky**, the reduced-scope fallback is: factor the hand-rolled
  parser into its own `IntrinsicArgParser` type (no grammar change) AND still delete the static channel by giving the
  string channel a `ReceiverContext`. Prefer the grammar route; log the decision in DEVLOG.
- **Why:** Removes the duplicated expression evaluator and the division/float-incapable static channel (rationale #6,
  `DESIGN-codegen-backend.md §1.6, §2.5`).
- **Verify:** battery 1+2+**3**+4 green (grammar touch ⇒ run the legacy guard: NIST 353 MATCH must hold). Probe an
  intrinsic with a compound arithmetic arg and a division:
  ```bash
  dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/func_expr_arg.cob --std 2014 -o /tmp/f.dll --run
  ```
- **COMMIT:** `P7 step12: FUNCTION args parse as arithmeticExpression; delete IntrinsicRenderer static channel (full legacy guard)`
- **AS LANDED (2026-07-12):** the grammar route, in full — see the STATUS banner for the complete as-landed
  design (lexer suppression + `SIGNED_*` twins + `FNARG_SEPARATOR`; the D2 keyword-omitted fragment re-parse
  through the ONE `functionArgList` rule; the outright deletion of the hand parser incl. the UdfBinder token
  path; the instance single-channel renderer + `RuntimeApi` routing + re-entrant render entries; the §8.9
  funnel's bare-argument-word skip; the `MapFunctionArgTokens` legacy shim; the §8.8.1.2 r3 left-associative
  `**` correction). New nets: the Step-12 section of `IntrinsicFunctionDifferentialTests` (compound/division
  args, sign-adjacency discrimination, `**` associativity, string-channel division + numeric-edited de-edit,
  ref-mod args, empty parens, keyword-omitted expression args, OMITTED→COBOLNET1544) and the
  `tests/conformance/2014/func_expr_arg.cob` probe.

---

## 5. Verification (phase end)

Run the COMPLETE battery and confirm all green + neutral:

```bash
cd E:/CobolSharp
dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -v quiet          # generator + all projects compile
dotnet test  tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet   # ~2028 green, 0 diffs
dotnet test  tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet                 # ~213 green
dotnet test  tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj -v quiet  # snapshots neutral
bash scripts/guard-fast.sh                                            # legacy guard: NIST 353 MATCH + 11 LEGACY_DIVERGENT
```

Behavior-neutrality / structural checks (all must pass):

1. **No loud dispatch default survives.** `grep -rn "LoudStmt\|BoundUnsupported\|default =>" src/Cobol.Net.Compiler`
   in the emit/analysis/renderer paths returns only the intentional `BoundUnsupported` *production* in
   `BindStatementCore` (the parse-dispatch `switch`, which stays) — NO `default`/`_ =>` arm in any `IBound*Visitor`
   implementation.
2. **A missing arm is a compile error.** Add a throwaway `sealed record BoundProbe : BoundStatement;` to
   `BoundTree.cs`, `dotnet build` → the generated `Accept` + every visitor FAIL TO COMPILE. Remove the probe.
   (Documents the exhaustiveness guarantee; do not commit the probe.)
3. **No C# text in the bound tree.** `grep -rn "Read()\|Write(" src/Cobol.Net.Compiler/Binding/Model/Place.cs` → gone; the
   neutrality reflection test (Step 11) passes.
4. **RuntimeApi is the single ABI.** The bare-`Cobol*.` guard test (Step 4) passes with an EMPTY whitelist — no bare
   runtime literal anywhere in `CodeGen/` except `RuntimeApi.cs`.
5. **No mutable `StoreAsImage`.** `grep -rn "StoreAsImage" src/Cobol.Net.Compiler` → no `set`; reads only via the
   init-only `StorageForm` (or the flag is gone entirely if P5/P6 renamed it).
6. **Snapshot neutrality.** The emitted-C# for a curated one-per-feature-family set is byte-identical to the P0 seed
   EXCEPT any intentional emit change reviewed and re-baselined (this phase intends NONE unless §7 applies).

---

## 6. Rollback / resumability

- **Every numbered step is an independent, battery-green commit.** To resume after interruption: read the STATUS
  line, `git log --oneline | grep "P7 step"` for the last landed sub-commit, continue at the next.
- **Step 6 (visitor) internal resume:** sub-commits 6a→6f are independent; if 6a's generator misbehaves on one OS,
  fall back to the hand-written `abstract` visitor (§Step 6 fallback) — the consumer conversions 6b–6f are identical
  either way.
- **Step 8 (StoreAsImage deletion) rollback:** the prove-then-delete cross-check means `MarkStoreAsImage` still runs
  until the equivalence assert is green corpus-wide. If the assert ever fails, DO NOT delete — the failing item is a
  `StorageFormPass` gap (P5/P6); fix the pass, re-run, then delete. Reverting Step 8's commit restores the flag.
- **Step 11 (structural Place) rollback:** the per-subtype `RenderReadLegacy` shim means any single subtype's
  conversion is revertible in isolation without touching the others. Never delete the shim until ALL subtypes +
  emitters are migrated.
- **Risks & mitigations (from the sibling designs):**
  - **R1 — OO method-scope shadowing regression (HIGH).** Collapsing to one scoped `SymbolTable` + `EnterMethodScope`
    is behavior-sensitive (§11.7 GR5 sibling-invisibility). Do `OoBinder` LAST in Step 10, behind the OO goldens +
    method-scope unit tests.
  - **R2 — StorageForm equivalence gaps (HIGH).** `StoreAsImage`'s SORT-SD/FILE-whole-group/compiler-temp special
    cases must ALL be reproduced by `StorageFormPass`. Mitigation: Step 8's corpus-wide prove-then-delete cross-check.
  - **R3 — source-generator build complexity (MEDIUM).** Regen must be portable across both OSes
    (`feedback_commit_generated_parser`). Mitigation: the hand-written `abstract` visitor fallback removes the
    dependency.
  - **R5 — structural-Place blast radius (HIGH).** ~all verbs read `Place.Read()/Write()`. Mitigation: last, behind
    the subtype-at-a-time shim, with the differential/snapshot harness asserting byte-identical output.
  - **R6 — long-lived branch drift (MEDIUM).** Every step ships to `main` behind the green battery; no long branch.

---

## 7. ISO feature work in this phase

P7 is a **rearchitecture** phase — it is **behavior-neutral by design**. It adds NO new ISO construct and changes NO
observable output, with these two explicitly-bounded exceptions:

1. **Apostrophe-delimited VALUE / literal miscompile (§8.3.1 string literals; both `"` and `'` ISO delimiters).**
   This is a confirmed HIGH silent miscompile (`DESIGN-codegen-backend.md §1.6`, `DESIGN-data-model.md`), fixed by
   the ONE `Common/CobolLiteral.Decode` codec. **Ownership: the data-model phase (P5).** If P5 already landed it,
   this phase merely routes decodes through it (Step 4) with no behavior change. If P5 has NOT landed it, this phase
   does NOT fix it (it is a red→green change that must ship with its conformance goldens —
   `elementary VALUE 'x'`, `group VALUE 'x'`, `ALL 'x'`, Report-Writer SOURCE `'x'` — in P5, not here). Do not smuggle
   a behavior change into a neutral refactor phase.
2. **No other spec sections change.** The visitor/decomposition/`Place`-structural work touches MOVE (§14.9.25),
   arithmetic (§14.x), conditions (§8.8), reference modification (§8.4.2.4), OCCURS DYNAMIC polarity (§8.5.1.9),
   REDEFINES views (§13.18.44), RENAMES (§13.18.45), and the CAPACITY register (§13.18.38) ONLY in HOW they are
   *represented internally* — the emitted C# and program output are unchanged (proven by the snapshot + differential
   nets). No new goldens are added by this phase except, if applicable, the P5 apostrophe set above.

**Editions:** the four-editions-in-one matrix is unaffected — the `VersionConformancePass` rules and the two
load-bearing forward-detect predicates (the OPEN `retryPhraseAhead()` site and the boolean-condition entry) are
untouched; Step 12's FUNCTION-arg rule is edition-invariant (arithmetic args are legal in every edition that has
the function).
Run the version-continuity sweep (`scripts/version-continuity-sweep.sh`) at phase end to confirm no per-edition
regression.

---

## Appendix A — file/line anchors (the phase-START tree, before Steps 1–10)

> ⚠ These anchors describe the pre-decomposition tree this phase began from. Steps 9–12 dissolved both god classes
> and restructured the renderers, so the emit dispatch, `MarkStoreAsImage`, `EmissionContext.Target*`, and the
> `CSharpEmitter`/`StatementBinder` partials named below **no longer exist at these locations** — the STATUS line
> describes the as-landed layout (the phase is complete; no separate site map exists).

| Concern | phase-start location |
|---|---|
| Parse→bound dispatch (stays a switch) | `Binding/Bound/StatementBinder.cs:170-231` |
| Emit dispatch god-switch (→ visitor) | `CodeGen/CSharpEmitter.cs:349-455` |
| `EmitMove` + `ConvertSource` re-classify | `CodeGen/CSharpEmitter.cs:479-540, 705-...` |
| `MarkStoreAsImage` write-back | `CodeGen/CSharpEmitter.cs:50-68`; re-syncs `Call.cs:111-120`, `Oo.cs:694-697` |
| Emitter `StoreAsImage` reads (~20) | `grep StoreAsImage src/Cobol.Net.Compiler/CodeGen` |
| `Place.Read()/Write()` strings | `Binding/Place.cs:22-25` + every subtype |
| Mutable `EmissionContext.Target*` (H1) | `CodeGen/Emit/EmitCore.cs:60-79` |
| Bare `Cobol*.` literals (~60 members) | `grep -hoE "Cobol[A-Za-z]+\." src/Cobol.Net.Compiler/CodeGen` |
| `IntrinsicRenderer` static channel | `CodeGen/Emit/IntrinsicRenderer.cs:353-380`; arg parser `Intrinsics.cs:686-839` |
| Figurative-fill (4 copies) | `EmitCore.cs:119`, `FieldEmitter.cs:469`, `ConditionRenderer.cs:305`, `EmitContext.FigFill` |
| ON/NOT-ON extractor (~8 clones) | `KeyedIo.cs:104,230,322`, `StringUnstring.cs:198`, `Sort.cs:330`, `Call.cs:154`, `StatementBinder.cs:364,400` |
| `StatementBinder` partials (21) | `Binding/Bound/StatementBinder*.cs` |
| `CSharpEmitter` partials (15) | `CodeGen/CSharpEmitter*.cs` |
| Emit renderers (7) | `CodeGen/Emit/{BooleanRenderer,ConditionRenderer,EmitCore,FieldEmitter,IntrinsicRenderer,NumericRenderer,OperandText}.cs` |
| Bound-node records (99) | `Binding/Bound/BoundTree.cs` |
