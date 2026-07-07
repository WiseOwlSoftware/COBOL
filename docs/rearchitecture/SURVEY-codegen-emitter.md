# SURVEY — CSharpEmitter codegen subsystem

**Subsystem:** `src/Cobol.Net.Compiler/CodeGen/` — the Roslyn (C#) backend: `CSharpEmitter` (15 partials) +
`CodeWriter` + `RoslynBackend` + the `Emit/` renderer sub-package.
**Reviewer:** codegen-emitter survey agent. **Date:** 2026-07-07. **Method:** full read of `CSharpEmitter.cs`
(1813 LOC) + all 14 partials + `CodeWriter.cs` + `RoslynBackend.cs` + `Emit/EmitCore.cs` + `Emit/NumericRenderer.cs`
+ `Binding/Place.cs`; spot-reads of `Call.cs`, `Oo.cs`, `Evaluate.cs`; grep census of dispatch arms, shared fields,
runtime coupling, and `StoreAsImage` reads.

---

## 1. Responsibilities & place in the pipeline

`CompilerDriver.Compile` (`CompilerDriver.cs:106-138`) runs: parse → `EditionValidator` →
**`new CSharpEmitter().Emit(tree, edition, turnEvents)` → `string csharp`** (`CompilerDriver.cs:112`) → write
`.g.cs` → **`RoslynBackend.Compile(csharp, outputDll, assemblyName)`** (`CompilerDriver.cs:130`). So the emit
architecture is:

```
parse tree ──► CSharpEmitter ──► C# SOURCE TEXT (string) ──► RoslynBackend (re-parses the text) ──► .dll
                (bind + emit)      (CodeWriter/StringBuilder)   CSharpSyntaxTree.ParseText → CSharpCompilation.Emit
```

`CSharpEmitter` is the whole code generator. `RoslynBackend` (`RoslynBackend.cs:13`, a *static* class) is only a
string→assembly compiler: it `ParseText`s the emitted string (`RoslynBackend.cs:26`), builds a
`CSharpCompilation` (`:29`), `Emit`s the dll (`:39`), and deploys `.runtimeconfig.json` + the runtime dll
(`:42-43`). The emitter consumes the **bound tree** for the PROCEDURE DIVISION (never re-walks parse — `EmitStatement`
`CSharpEmitter.cs:347`) but `CallEmitRunUnit` *also drives all binding* (see §4).

**Emit architecture verdict: STRING-building codegen, not Roslyn `SyntaxFactory`.** `CodeWriter`
(`CodeWriter.cs:12-14`) is a `StringBuilder` + indentation. Every emit is `w.Line($"…{interpolated C#}…")`. The
Roslyn API surface (`Microsoft.CodeAnalysis`) is touched in only 4 files (`RoslynBackend.cs`, `EmitCore.cs`,
`CSharpEmitter.cs`, `Oo.cs`) and only for (a) the final compile and (b) `SymbolDisplay.FormatLiteral` used as the
C#-string-literal *escaper* (`EmitText.CsLiteral`, `EmitCore.cs:105`). This is a deliberate, defensible choice — the
generated `.g.cs` is meant to read like hand-written C#, and the mandatory Roslyn compile step already gives
type-safety — but it means **the emitter's entire output contract is "C# text," which is the crux of the
second-backend problem** (§3, §5).

---

## 2. Key types

| Type | File / LOC | Role | Assessment |
|---|---|---|---|
| `CSharpEmitter` | `CSharpEmitter.cs` + 14 partials, **~7.9k LOC total** | The code generator: dispatcher, statement emit, MOVE/arith, file I/O, CALL/OO/EC/RW/Sort/Inspect/String, **and all bind orchestration** | God class **and** de-facto middle-end. One `sealed partial class` (`CSharpEmitter.cs:24`) sharing **~39 mutable private fields** across all 15 files. See §3.1. |
| `CodeWriter` | `CodeWriter.cs` / 73 | `StringBuilder` text sink + `Block`/`BlockScope` indentation | Correct, minimal, keep. The one clean piece. |
| `RoslynBackend` | `RoslynBackend.cs` / 105 | `string csharp` → `.dll` via Roslyn; runtimeconfig + runtime-dll deploy | **Static class, not behind any interface.** `ReferenceAssemblies()` rebuilds ~180 `MetadataReference`s *uncached every compile* (`:73-83`); mixes pure compile with filesystem side-effects (`DeployRuntime`, `WriteRuntimeConfig`). |
| `EmissionContext` | `Emit/EmitCore.cs:15` / (in 227) | Shared spine: `Writer`, `Data`, derived `CollateArg`/`EditCfgArgs`/`FigFill`, **+ mutable `TargetScale`/`TargetReal`/`TargetRounding`/`InSizeErrorContext`** (`:60-79`) | Real collaborator, but the 4 mutable `Target*` setters are the **H1 staleness hazard** — written before an RHS render, read deep in 3 renderers, reset by hand. Name also collides with a legacy `EmissionContext`. |
| `NumX` | `Emit/EmitCore.cs:91` | `readonly record struct (Expr, Scale, Dec, Real)` — a rendered numeric C# expr + its scale | Good backend-neutral-ish value **except** `Expr` is a C# string. |
| `EmitText` | `Emit/EmitCore.cs:95` | `LoudStmt`/`LoudValue`, `CsLiteral`, `FileKeyExpr`, `FigurativeFill`, literal decode/repeat | The literal/escape boundary. `AllLiteralText` (`:162`) hard-codes the `"` delimiter — the apostrophe-VALUE decode bug. |
| `NumericRenderer` | `Emit/NumericRenderer.cs:16` / 259 | `BoundExpr`→`NumX` (real separate class) | Good decomposition. But mutually-recursive with `IntrinsicRenderer` (which has a *parallel* division/float-incapable static channel). |
| `ConditionRenderer`, `IntrinsicRenderer`, `BooleanRenderer`, `OperandText`, `FieldEmitter` | `Emit/*` | Genuine separate collaborator classes | The **real** decomposition lives here, not in the partials. `FieldEmitter` (484 LOC) is a DATA-DIVISION emitter miscategorized under `Emit/` and re-`new`'d ad hoc (`Call.cs:573`, `Oo.cs:647`). |
| `Place` (+ ~10 subtypes) | `Binding/Place.cs:13` | The lvalue abstraction | **Carries pre-rendered C# text** via `abstract string Read()`/`Write(rhs)` (`:22-25`). This is in the *Binding* layer, so **the bound tree itself is C#-shaped** (see §3.2). |

---

## 3. Architecture smells (severity · file:line)

### 3.1 CRITICAL — `CSharpEmitter` is a god class *and* the middle-end orchestrator, sharing ~39 mutable fields
- The `sealed partial class` split across 15 files is **file-slicing, not decomposition**: every partial is
  `public sealed partial class CSharpEmitter` (`CSharpEmitter.cs:24`, `.Call.cs:24`, `.Oo.cs:22`, `.Evaluate.cs:7`,
  …). All 15 files share **one object and ~39 mutable private fields** — `_ctx,_num,_cond,_refs`
  (`CSharpEmitter.cs:26-29`), `_currentPc,_depCounter,_sizeErrCounter,_storeTmpCounter,_readCounter,_sizeErrVar`
  (`:72-77`), `_useDecls,_callOuterGlobalUse` (`:85-86`), `_dispatchName` (`:135`), `_sentenceEndLabel` (`:268`),
  `_loopCounter/_setCounter/_searchCounter` (`:1158,1255-1256`), plus `_callSelfPath,_callReturningPlace,
  _callCounter,_callUidBand,_callInheritedStatusPlace` (`Call.cs:65-76`), `_ooClasses,_ooIfaceData` (`Oo.cs:44-50`),
  and the EC band (`_ecActive,_ecCounter,_ecInfo,_turnState,_sizeErrEcVar,_ecUnitHasF3/F4` in `Exceptions.cs`).
  There is **zero encapsulation between "verbs"** — `EmitSearch` and `EmitCall` can each stomp the other's counters.
- **Worse: the codegen class runs the entire middle-end.** `CallEmitRunUnit` (`CSharpEmitter.Call.cs:88-194`)
  executes ~12 binding passes *inside the emitter*: collect units (`:96`), bind interface/class/program **data**
  (`:98-108`), validate override signatures (`:101`), bind bodies + build the UDF table + bind procedures
  (`:109-110`), the `CompilerTempClones`/`StoreAsImage` re-sync (`:115-120`), `MarkStoreAsImage` (`:119-120`),
  compute the EC gate (`:126-127`), and qualify file connectors (`:138-147`) — *then* emits. There is **no Bind
  phase boundary**; "how binding is ordered" lives one layer below the driver, in the code generator. This is the
  single worst coupling in the subsystem.

### 3.2 CRITICAL (blocks a second backend) — the bound tree carries pre-rendered C# text
The owner goal is a backend-neutral bound tree feeding a selectable `ICodeGenBackend`. Reality:
- `Place.Read()`/`Place.Write(rhs)` return **C# strings** (`Binding/Place.cs:22-25`). Every subtype hard-codes
  runtime call syntax: `MemberPlace.Write` → `"{Path} = {rhs};"` (`:45`); `RedefViewPlace` →
  `"CobolString.RefMod(...)"` / `"CobolString.SpliceInto(...)"` (`:94,100-101`); `NumericImagePlace` →
  `"CobolNum.FormatDisplay(...)"` / `"CobolNum.StoreDisplay(...)"` (`:160-164`); `CapacityRegisterPlace.Read` →
  `"{TablePath}.Capacity"` (`:185`); `DynTablePlace` carries **two** precomputed path strings (`:58,67,70`).
- **`RenamesPlace.Write` (`Place.cs:124-138`) emits an entire C# *block statement*** with a local
  `{ string __ren = CobolString.Store(...); leaf.Write(__ren.Substring(...)); … }` — a compound C# statement baked
  into a "bound" node. This is the starkest proof the tree is C#-shaped.
- Because `Place` lives in `Binding/`, this C# text is **in the bound tree the backend is supposed to be neutral
  over**. A CIL backend cannot consume any of it — it would re-impose C# syntax on a supposedly neutral tree. **This
  is the largest structural blocker to the dual-backend goal.**

### 3.3 HIGH — two hand-maintained, non-exhaustive god-switches, with a runtime (not compile-time) miss default
- `EmitStatement` (`CSharpEmitter.cs:347-455`) is a **79-arm** `switch` ending in
  `default: w.Line(LoudStmt($"bound statement '{s.GetType().Name}'"))` (`:453`). Its sibling
  `StatementBinder.BindStatementCore` (~55 arms, ends `_ => new BoundUnsupported(...)`) must be kept in lockstep
  **by convention**. A forgotten arm ships as a **runtime `LoudStmt`, not a compile error.**
- The same sealed `Bound*` hierarchy (99 records in `BoundTree.cs`) is independently type-switched by ≥4 more
  consumers: `NumericRenderer.Render/AsNum` (`NumericRenderer.cs:24,50`), `ConditionRenderer.Render`,
  `OperandText.AsString/IsString`, `BoundStores.StoreKindOf`. Adding a bound node is **shotgun surgery across 6+
  switches with no compiler enforcement.**

### 3.4 HIGH — the "triple-maintenance" per-verb parallel structure
The per-verb split is mirrored in **three** places, wired only by naming convention:
`Binding/Bound/StatementBinder.{X}.cs` (21 partials) **↔** `CodeGen/CSharpEmitter.{X}.cs` (15 partials) **↔** a
renderer. Confirmed parallel sets: `Accept, AlterSwitches, Call, Corresponding, Evaluate, Exceptions, Initialize,
Inspect, KeyedIo, Oo, Ptr, ReportWriter, Sort, StringUnstring` exist on **both** sides. Adding/altering a verb means
editing the binder partial, the emitter partial, and often a renderer — with no type that forces the three to agree.

### 3.5 HIGH — the emitter re-derives binder facts at emit time (semantics leak past the bound tree)
- `EmitMove` (`CSharpEmitter.cs:479-502`) and `ConvertSource` (`:714-807`) **re-classify MOVE category at emit time**
  — group vs elementary, numeric-edited, alpha-edited, figurative-fill — logic the binder already had. `ConvertSource`
  is a 90-line receiver-category `switch`.
- The emitter reads the binder-set mutable flag `DataItem.StoreAsImage` at **~49 sites across `CodeGen/`**
  (`grep` census: `CSharpEmitter.cs`×13, `Call.cs`×7, `Oo.cs`×6, `FieldEmitter.cs`×7, plus Accept/Inspect/Sort/
  StringUnstring/ReportWriter/ConditionRenderer/NumericRenderer/OperandText). This flag is *mutated during emit*
  by `MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) and re-synced (`Call.cs:115-120`, `Oo.cs`) — a binder→emitter
  channel **outside the bound tree**, and a mutable-shared-state latent-bug surface.

### 3.6 HIGH — untyped runtime coupling (the ABI is a pile of string literals)
Codegen names the runtime API entirely **by bare string**: grep census over `CodeGen/` — `CobolNum.`×83,
`CobolFile.`×61, `CobolString.`×40, `CobolRounding.`×38, `CobolIntrinsics.`×34, `CobolEdit.`×19, `CobolSort.`×18,
`CobolDate.`×12, plus `CobolFloat/Inspect/Ptr/ArgAdapt/Dec/Bool/Table/Object` — **~60 runtime members, hundreds of
occurrences.** A runtime rename or signature change is invisible until the *generated* C# fails to Roslyn-compile at
run time. There is no typed façade.

### 3.7 MEDIUM — mutable ambient receiver state (the H1 hazard)
`EmissionContext.{TargetScale,TargetReal,TargetRounding,InSizeErrorContext}` are public get/set
(`EmitCore.cs:60-79`), written by `CSharpEmitter` before rendering an RHS (`SetTarget` `:952`; `EmitDivide` `:849-851`;
`EmitCompute` `:933-935`) and read deep in `NumericRenderer`/`IntrinsicRenderer`/`ConditionRenderer`. The code
literally documents a manual "H1 staleness discipline" (`EmitCore.cs:66`). A missed reset silently mis-scales or
float-promotes.

### 3.8 MEDIUM — duplicated/quadruplicated helpers
- **Parallel numeric evaluator:** `IntrinsicRenderer`'s static channel (`NumStatic/StaticAdditive/StaticMul`)
  re-implements a division/float-*incapable* subset of `NumericRenderer` because it lacks an `EmissionContext`.
- **Figurative-fill ×4** with divergent return types: `EmitText.FigurativeFill` (`EmitCore.cs:119`),
  `FieldEmitter.FillCharFor`, `ConditionRenderer.FigurativeFillChar`, `EmissionContext.FigFill` (`EmitCore.cs:42-57`)
  — a real HIGH/LOW-VALUE divergence risk.
- `FieldEmitter` (484 LOC, a DATA-DIVISION emitter) is parked under `CodeGen/Emit/` and `new`'d ad hoc bundling ≥4
  concerns (record struct, group image codec, VALUE slicing, initializers).

### 3.9 MEDIUM — `RoslynBackend` throughput + packaging
`ReferenceAssemblies()` rebuilds ~180 `MetadataReference`s uncached on every compile (`RoslynBackend.cs:73-83`) —
dominates in-process test/batch cost; and mixes pure compilation with side-effecting `File.Copy`/`WriteAllText`
(`:42-43,61-66,89-104`).

---

## 4. Coupling / shared mutable state / cross-layer reach

- **Emitter ↔ binder fusion:** `CallEmitRunUnit` (`Call.cs:88`) *is* the middle-end — it constructs `DataBinder`s,
  `StatementBinder`s, `ReferenceResolver`s, the UDF table, the OO class table, and mutates `StoreAsImage` — all
  inside the codegen type. The driver's "Phase 2 — bind … then emit" comment (`CompilerDriver.cs:101`) is a fiction;
  there is no separable bind phase.
- **~39 mutable private fields** shared implicitly across 15 partials (§3.1). `_ctx/_num/_cond/_refs` are
  re-assigned per unit inside `CallEmitProgramClass` (`Call.cs:496-499`) and per class in `Oo.cs` — the object is
  reused as a mutable cursor over units, so nothing is thread-safe or re-entrant-safe by construction.
- **Cross-layer reach into `Binding`:** the emitter reaches through `Place` (which emits C# for it), reads
  `DataItem.StoreAsImage/IsGroup/ImageWidth/ProfileName/Pic`, `FileModel.*`, `DataBinder.Files/Reports/Collating`
  directly. The emit logic depends on the *shape of the data model*, not on a rendered/neutral view of it.
- **Ambient `EmissionContext.Target*`** (§3.7) is shared mutable state read by 3 renderers.

---

## 5. Latent-bug risks (string-emit specific)

- **Injection / escaping:** string *content* is escaped correctly and consistently through the ONE path
  `EmitText.CsLiteral` = Roslyn `SymbolDisplay.FormatLiteral` (`EmitCore.cs:105`; also used inline at
  `CSharpEmitter.cs:355` for STOP-literal). No raw-COBOL-string-into-C# path was found that bypasses it.
- **Identifier collision:** avoided by two conventions — the `__` prefix for all emitter-internal names (COBOL
  data-names cannot contain `__`, documented `CSharpEmitter.cs:97`) and `DataItem.Sanitize` for data-name→C#-ident.
  Uniqueness of throwaway temps rides ~8 per-verb counters (`_depCounter`, `_setCounter`, `_storeTmpCounter`, …) that
  are *instance* fields shared across partials — correct only because they monotonically increment and are never
  reset mid-unit; a future reset (e.g. for re-entrancy) would silently collide.
- **CONFIRMED silent miscompile (apostrophe VALUE):** `EmitText.AllLiteralText` (`EmitCore.cs:162-168`) and
  `FieldEmitter.GroupValueText` hard-code the `"` delimiter, while `DecodeCobolString` (`:133-142`) handles **both**
  `"` and `'`. A `VALUE 'x'` / `ALL 'x'` decodes wrong with **no diagnostic** — a singular-pattern violation that
  corrupts data. This is a real, shipping latent bug.
- **`#pragma warning disable CS0164`** (`Call.cs:163`) blanket-suppresses unreferenced-label warnings because
  SEARCH/NEXT-SENTENCE emit per-boundary labels; a genuinely mis-emitted dead label would be masked.
- **Overload-resolution-as-dispatch:** `NumericImagePlace` (`Place.cs:156-164`) deliberately relies on **C# overload
  resolution at backend-compile time** to pick native-long vs string-image `FormatDisplay/StoreDisplay`. Elegant for
  Roslyn, but it means a *semantic* decision is deferred to the C# compiler — invisible to any non-C# backend and to
  static review.

---

## 6. Reorganization suggestions

**The class breakup (kill the god class):**
1. **Extract the middle-end out of codegen.** Move `CallEmitRunUnit`'s ~12 bind passes (`Call.cs:88-157`) into a
   `BindPipeline` returning an immutable `BoundCompilation`. The backend must *receive* a fully-bound compilation and
   perform **no binding**. This is the highest-value single change and the precondition for everything below.
2. **Real collaborator classes over an immutable context** (not partials of one object):
   `ProgramEmitter` (per-unit type + entry wrapper), `DispatchEmitter` (the PC dispatcher/ALTER fields),
   `StatementEmitter` (statement dispatch), per-verb `Verbs/{KeyedIo,Sort,String,Inspect,ReportWriter,Call,Oo,
   Initialize,Ptr,Evaluate,Corresponding,AlterSwitch,AcceptDisplay}Emitter`, plus the existing renderers. Each takes
   its collaborators by ctor injection — no shared mutable field bag.
3. **Immutable `EmitContext`** (rename off the legacy collision); replace `Target*` with a
   `readonly record struct ReceiverContext(Scale, Real, Rounding, InSizeError)` **passed as a parameter** to the
   numeric renders — closes H1 by construction and lets the `IntrinsicRenderer` static channel be deleted.
4. **Split `FieldEmitter`** into `DataDivision/{RecordStructEmitter,GroupImageCodec,GroupValueSlicer,
   ValueInitializer}` and move it out of `Emit/`. Unify the 4 figurative-fill copies into one `FigurativeConstants`.
5. **Generated exhaustive visitor** over the sealed `Bound*` roots: `IBoundStatementVisitor<bool>` (+ expr/cond/
   operand siblings) so a missing arm is a **compile error**; delete every `default: LoudStmt`. Also collapses the 6
   parallel type-switches to one dispatch per family per backend, and dissolves the triple-maintenance risk (§3.4) —
   the binder side stays a *parse-context* switch, but the emit/analysis lockstep is compiler-enforced.
6. **`RuntimeApi` typed façade** (`nameof`-anchored) over the ~60 runtime members; forbid bare `Cobol*.` literals in
   `CodeGen/` by an analyzer test — a runtime rename then breaks one file at author time.

**The backend seam (the owner's dual-backend goal):**
7. **Create `ICodeGenBackend { BackendId Id; BackendArtifact Emit(BoundCompilation, BackendOptions); }`** and make
   `RoslynBackend : ICodeGenBackend`. Split `AssemblyPackager` (runtimeconfig + dll deploy) out of it; cache the
   framework `MetadataReference` set in a `static readonly Lazy<…>`.
8. **Backend-neutralize `Place` and the bound nodes.** `Place` becomes structural — an `AccessPath` of
   `AccessSegment`s + `BoundExpr` subscripts + a ref-mod span, with **no `Read()/Write()`**. A Roslyn-side
   `PlaceRenderer(EmitContext, RuntimeApi)` owns the current C#-string logic; a CIL backend lowers the *same*
   structure to load/store. This is the change that makes a second backend possible at all.

---

## ROADMAP GAP CHECK

I read `DESIGN-codegen-backend.md` and `PHASE-07-visitor-dispatch-emitter-decomposition.md`. **Both are unusually
strong and already name essentially every smell above with matching file:line anchors.** They correctly decide:
keep string emit / reject SyntaxFactory (DESIGN §2.1); materialize `ICodeGenBackend` (§2.2 / P7 Step 1); structural
`Place` + `PlaceRenderer` (§2.3 / Step 11); generated exhaustive visitor (§2.4 / Step 6); immutable `EmitContext` +
`ReceiverContext` (§2.5 / Step 3); `RuntimeApi` façade (§3 / Step 4); split `FieldEmitter`, `AssemblyPackager`, cache
refs, unify figurative-fill; and push MOVE-kind/storage-form onto the node killing `ConvertSource`/`StoreAsImage`
(Steps 7-8). **On the two questions asked: yes, the plan breaks up the god class (Steps 3,9,10) and yes, it makes the
statement/expr/condition/operand leaves + `Place` one-visitor-per-backend over a neutralized tree (Steps 6,11).**
The gaps are at the edges:

1. **GAP (biggest) — the program SKELETON is never neutralized; "one visitor per backend over a neutral IR" covers
   only the leaves.** The visitor plan neutralizes `BoundStatement/Expr/Condition/Operand` + `Place`. But roughly
   *half of `CSharpEmitter.cs` and most of `Call.cs`* is program-**skeleton** emission that is **not** a bound-tree
   walk and is **not** brought under any neutral model: the PC dispatcher as a C# `switch`
   (`EmitDispatchMethod` `CSharpEmitter.cs:142-170`), the USE/`__RunUse`/`__IoCheck` machinery (`:179-266`), the
   `ICobolProgram` ABI + LINKAGE carriers + `__outer` GLOBAL `ref`-bridges + entry wrapper + file registration
   (`Call.cs:493-756`), and the Report-Writer engine members (`RwEmitReportMembers`). Under the plan these move into
   Roslyn-specific `ProgramEmitter`/`DispatchEmitter`/`DataEmitter` — so a CIL backend must **re-implement all of it
   from scratch.** That may be acceptable under SSOT §1.1 ("no shared lowered IR; each backend lowers itself"), but
   the plan never says so, never enumerates which skeleton inputs are already neutral (pc integers, `EntryPc`,
   `Declaratives`, `StartPc/HandlerEndPc`, the EC gate) versus which are C#-runtime-ABI choices, and never states
   that `ICobolProgram/CobolArg/ProgramRegistry/ManagedPointer` is the **shared runtime contract** both backends
   target. **Correction:** add a short "program-skeleton neutrality" subsection giving the CIL backend a checklist of
   the skeleton surface and declaring the runtime ABI the neutral boundary; otherwise the dual-backend goal is only
   half-delivered and the neutrality test (Step 11) gives false confidence (it only checks that *nodes/Places* expose
   no string render method).

2. **GAP — `RuntimeApi` is Roslyn-only and does not double as the neutral runtime-op vocabulary the CIL backend
   needs.** The real backend-neutral "instruction set" is the *set of runtime operations* (`Store`, `RefMod`,
   `SpliceInto`, `FormatDisplay`, `Compare`, `StoreDisplay`, …) and **which op realizes each semantic**
   (e.g. "a `RedefViewPlace` write is a `SpliceInto`"). The plan keeps that op-selection *inside* `PlaceRenderer`/
   `ExpressionRenderer` as C# strings via `RuntimeApi` (a Roslyn class returning C# fragments). A CIL backend must
   independently re-derive every op selection. **Correction:** consider lowering `Place`/expr to a neutral
   *runtime-op + operands* node that each backend merely *renders* (Roslyn→string, CIL→`callvirt`), so op-selection
   is shared and only rendering differs. At minimum, flag this as residual duplication the CIL backend inherits.

3. **CORRECTION — P7 Step 1 is internally inconsistent / mis-sequenced.** Step 1 says "wrap the CURRENT
   `CSharpEmitter.Emit(...)` + `Compile(...)` verbatim … no behavior change," but the seam signature is
   `Emit(BoundCompilation, BackendOptions)` while today's `CSharpEmitter.Emit(tree, edition, turnEvents)` takes a
   **parse tree and binds internally** (`CompilerDriver.cs:112`, `CallEmitRunUnit` `Call.cs:88`). You cannot both
   consume a `BoundCompilation` *and* wrap the parse-tree-binding emitter verbatim. The "Depends on: P6 DONE" note
   papers over this, but then Step 1 is **not** a no-op wrap — it presupposes the entire middle-end (§3.1) has already
   left the codegen class in P6. **Correction:** state explicitly that Step 1 is blocked on P6's `BoundCompilation`
   extraction, or give the seam an interim `Emit(parseTree,…)` signature re-shaped at Step 11/M6.

4. **DEPENDENCY RISK the plan under-weights — the single worst coupling (§3.1) is delegated wholesale to P6.** P7
   Step 9 explicitly assumes "the OO/bind orchestration P6 already extracted from `CallEmitRunUnit` does NOT reappear
   here." If P6 under-delivers the `BindPipeline`, `ProgramEmitter` inherits the god-orchestrator and the god class is
   not actually killed. This is the survey's #1 smell and it lives entirely in a *cross-phase dependency*. The plan's
   Preconditions grep (Step 1 §1) checks that `BoundCompilation`/`BindPipeline` *exist*, but not that
   `CallEmitRunUnit`'s bind passes are actually *gone from CodeGen*. **Correction:** add an explicit P7 entry-gate
   assertion: `grep -n "new DataBinder\|new StatementBinder\|MarkStoreAsImage\|\.Bind(" src/…/CodeGen` returns
   nothing before Step 9 begins.

5. **MINOR — `--backend` selection and the `CheckOnly` diagnostic surface.** DESIGN §2.2 adds `--backend roslyn|cil`
   but neither doc notes that today the emit call **doubles as the bind-diagnostic surface for `CheckOnly`**
   (`CompilerDriver.cs:112-120` runs `Emit` even in check-only to collect edition diagnostics accrued during binding).
   Once binding moves to `BindPipeline`, check-only must bind-without-emit; the driver contract should be restated so
   `--backend`/check-only interact cleanly. Low risk, but currently unaddressed.

**Net:** the plan is decision-complete for the *leaf* dispatch, `Place`, context immutability, and the seam
mechanics; it is **incomplete on the program-skeleton half of the emitter** (the largest remaining C#/Roslyn-coupled
surface) and carries a load-bearing, under-asserted dependency on P6 for the worst coupling it claims to fix.
