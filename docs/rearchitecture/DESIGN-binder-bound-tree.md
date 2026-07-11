# DESIGN — Target Binder Pipeline & Bound-Tree / IR

Status: DESIGN (rearchitecture target). Owner-review required on the OPEN QUESTIONS before execution.
Scope: the binding middle-end of the greenfield compiler (`src/Cobol.Net.Compiler/Binding/**`) and the
bound-tree contract it produces (`Binding/Bound/BoundTree.cs`). Dimension: **Target binder pipeline &
bound-tree/IR design**. Sibling design docs own the data model (`DataItem`/`PicInfo`/`Place`/`StorageForm`),
the emitter decomposition, the editions framework, and the driver/pipeline shell; this doc cites them where
they meet the binder and declares the dependencies explicitly at the end.

SSOT alignment: `docs/COBOLNET_DESIGN.md` §1.1 (no shared lowered IR), §2 (bind-once bound tree), §16 (build
order), §18 (settled decisions). This design **keeps** the owner-locked §1.1/§18.23 "no shared lowered IR"
decision and works within it — see §3.3 for the precise distinction between the *rejected* CIL-shaped branch IR
and the *adopted* semantic-normalization-on-the-bound-tree.

> **The binder is edition-agnostic.** Version conformance is one mechanism, and this doc's binder contract reflects it
> in three ways: (1) the binder makes ZERO `ConstructRegistry.Check` calls — edition gating is a single
> `VersionConformancePass` over the bound tree (the sole syntactic+semantic gate). (2) ⚠ **AS-BUILT (DEVLOG 724): NO
> `.Syntax` back-reference is added to any bound node — the `BoundTree.cs` invariant STANDS.** The pass identifies
> syntactic introduction/removal/phrase gates via a PRESENCE-based parse-tree arm (over `BoundRunUnit.Tree`, running
> after bind) — introduction gates must fire on the construct's RECOGNITION, not its bound node, which a below-edition +
> semantically-invalid construct never produces; semantic gates use bound-node type/attribute. (3) Bind and emit are separate driver-gated
> phases — codegen runs emit-only-if-clean, never on an errored tree. Pipeline: parse → edition-agnostic bind →
> VersionConformancePass → emit-if-clean → backend; there is no `ReservedWordEditionHints`. Full design:
> `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`.

---

## 1. Current problem (grounded)

The binding middle-end is one of the two worst architectural-debt centers in the tree. Five concrete,
code-anchored problems:

### 1.1 Implicit pass ordering — the prime latent-bug class
`DataBinder.BindResolve` (`Binding/DataBinder.cs:210-258`) is a hand-ordered sequence of ~15 post-build passes
(`ExpandTypes → ResolveIndexItems → InheritUsageClauses → InheritSignClauses → ResolveRedefines →
ClassifyRedefinesClasses → CheckStrongTypeDeclarations → OoRouteMethodRedefinesBackings → OdoResolve →
DynamicResolve → ResolveFiles → GateNationalRecords → ResolveReports → CallBindExternalAndGlobal →
PtrBindBasedAndAddressables → the FILE-record whole-group loop`). The ordering constraints are real (the code
comment at :212-217 explains that `InheritSignClauses` MUST precede `ClassifyRedefinesClasses` because a
SEPARATE sign widens the image that feeds class-max width) but they exist ONLY as prose + call order. Nothing
asserts that a pass's inputs were produced. Reordering, inserting, or a future feature's new pass silently
mis-computes with no failure. This is the exact "check gated on the wrong condition → silent mis-compile" class
the rearchitecture targets.

There is a SECOND, hidden pass pipeline: the real middle-end orchestrator is `CSharpEmitter.CallEmitRunUnit`
(`CodeGen/CSharpEmitter.Call.cs:88-147`) — ~12 more implicit sequential passes (collect units, bind
interface/class/program data, validate overrides, bind bodies, build the UDF table, bind procedures, re-sync
`StoreAsImage`, `MarkStoreAsImage`, compute the EC gate, qualify file connectors) buried INSIDE the codegen
class. The driver's own phase names (Bind / Emit / Roslyn) are a fiction: there is no Binder phase boundary.

### 1.2 No symbol table — a public-mutable-dictionary blackboard
`DataBinder` exposes ~30 public get-only-but-mutable collections (`Roots`, `ByName`, `Conditions`,
`IndexFields`, `CapacityRegisters`, `TypeDecls`, `Files`, `FilesByName`, `Switch*`, `Alphabets`, `UserClasses`,
`Reports`, `WholeGroupReferenced`, `OoPendingPropertyOps`, `CompilerTempClones`, …). These ARE the module's API.
Downstream phases both read AND write them: `ReferenceResolver` writes `WholeGroupReferenced` during procedure
binding (`ReferenceResolver.cs:280,303`); `StatementBinder.MoveFigurative` writes `DataItem.StoreAsImage`;
`CSharpEmitter` writes `StoreAsImage` via `MarkStoreAsImage` AFTER binding and re-syncs `CompilerTempClones`.
There is no ownership boundary and no immutability — every collection is an open write channel.

Name lookup is quadrupled: `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` —
because OO method scoping (`ActiveMethodScope`, `OoMethodDataScope`) is a parallel shadow name-model that
consumers must opt into. Callers pick the wrong overload and silently miss method-local shadowing.

### 1.3 God-class binders
`StatementBinder` is ONE `sealed partial class` shattered across 23 files (~9.4k LOC) doing five fused jobs:
procedure-table construction, statement dispatch, expression/condition binding, the single relation checkpoint,
and inline SR/edition validation. The partials share full private state, so "how binding works" requires opening
23 files. `DataBinder` is the same pattern across 7 files (~3.9k LOC). Feature partials give the *illusion* of
decomposition (`Binding/Bound/StatementBinder.KeyedIo.cs`, `.Sort.cs`, `.Intrinsics.cs`, …) but they are the same
object with no enforced boundary.

### 1.4 Hand-maintained god-switches, no exhaustiveness
Statement dispatch is two parallel hand-switches kept in lockstep by convention: `BindStatementCore`
(`StatementBinder.cs:170-231`, ~50 arms) and `EmitStatement` (`CSharpEmitter.cs:347-...`, ~79 cases), each
ending in a loud `_ =>` / `default` that defers an unhandled node to a RUNTIME `LoudStmt`/`BoundUnsupported`
rather than a compile error. The same bound tree is walked by ≥5 more independent type-switches
(`BoundStores.StoreKindOf`, `NumericRenderer.Render/AsNum`, `OperandText.AsString/IsString`,
`ConditionRenderer.Render`, `AlterCollectFields`). Adding a bound node is shotgun surgery across ≥7 sites with
zero compiler help; a forgotten arm ships as a runtime throw.

### 1.5 Smart emitter / semantics leaking past the bound tree
The bound tree is supposed to be the SSOT (`BoundTree.cs:7-13` — "the backend renders the bound tree, it never
re-walks the parse tree"). In practice the emitter re-derives semantics the binder resolved: `EmitMove`'s
`ConvertSource` re-classifies MOVE category at emit time (`CSharpEmitter.cs:481-501,714`); the emitter reads
binder-set `DataItem.StoreAsImage`/`IsStrongGroup`/`ImageWidth` — a binder→emitter channel OUTSIDE the bound
tree; and binder verb partials assemble runtime C# STRINGS directly (`CobolTable.At(...)`, `RefReceiving(v)`,
`$"{Path}.{child.CsName}"` in `Initialize.cs:327-385`) — bind-time code building emit-time C#, blurring the §2
bind/emit boundary. `Place.Read()/Write()` emit raw C# strings (`Place.cs:124-138`).

Net effect: correctness depends on an implicit, undeclared execution order spread across the binder, the shared
data model, and the emitter — the definition of the fragility the rearchitecture must remove.

---

## 2. Target design — overview

Four structural moves, each attacking one problem class, all preserving the HARD INVARIANTS (typed-native only,
spec-first, one canonical mechanism, no shared lowered IR):

1. **A real `Binder` phase** with an explicit, **manifest-driven pass pipeline** (`BindPipeline`) whose passes
   declare `Requires`/`Produces` capability tokens; the DAG is validated at startup and a completion-phase gate
   makes "read a fact before its producing pass ran" a hard error, not a silent miscompile. This absorbs BOTH
   `BindResolve` and the binder half of `CallEmitRunUnit`.
2. **An immutable `BoundCompilation` result + a `SymbolTable`/`ScopeResolver`** replacing the 30 public mutable
   dictionaries. Mutation happens only inside passes via a write-capability handle; downstream phases (emitter)
   receive a read-only view. OO method shadowing folds into the ONE `ScopeResolver.Resolve`, deleting the lookup
   quadruple.
3. **Source-generated exhaustive dispatch** over the bound tree (`IBoundStatementVisitor<T>` + a generated
   `Accept`), so `EmitStatement`, `BoundStores`, and the renderer switches become exhaustive — a missing arm is a
   COMPILE error. The five error-node families get one `IBoundError` marker.
4. **Real collaborator classes** over an injected `BinderContext`, replacing the partial-class god objects; and a
   thin **semantic-normalization step on the bound tree** (NOT a lowered branch IR) that moves emit-time
   re-classification (MOVE kind, storage form, table-access polarity) back onto the bound node, making the
   emitter a pure renderer.

The pipeline shape becomes literal and matches the driver's phase names:

```
Frontend (parse) ──► Binder (BindPipeline: N ordered passes) ──► BoundCompilation ──► Backend (emit) ──► Roslyn
```

---

## 3. Target design — concrete

### 3.1 The pass framework (`Binding/Pipeline/`)

A pass is a unit of forest computation that declares what forest facts it consumes and produces. Capabilities are
an enum of named facts, NOT free strings, so the DAG is compile-time enumerable and greppable.

```csharp
namespace CobolNet.Binding.Pipeline;

/// One computed forest fact. A pass may only READ a capability listed in Requires and may only
/// WRITE (mark produced) a capability listed in Produces. The manifest validates the DAG at startup.
public enum Capability
{
    EntryTree,             // the DataItem forest exists (produced by declaration binding)
    TypesExpanded,         // TYPEDEF/TYPE clones materialized (ExpandTypes)
    UsageMarkersResolved,  // USAGE INDEX/marker PicInfo resolved (was ResolveIndexItems)
    UsageInherited,        // group USAGE pushed to leaves (InheritUsageClauses)
    SignInherited,         // group SIGN pushed to leaves (InheritSignClauses)
    RedefinesResolved,     // REDEFINES/RENAMES targets bound (ResolveRedefines)
    RedefinesClassified,   // shared-storage classes + tiers assigned (ClassifyRedefinesClasses)
    StrongTypesChecked,    // §13.18.57 SR3/SR4
    OdoResolved, DynamicResolved, FilesResolved, NationalGated, ReportsResolved,
    ExternalGlobalBound, PointersBound,
    ProcedureBound,        // PROCEDURE DIVISION bound to BoundProgram(s) (produces WholeGroupReferenced facts)
    StorageFormComputed,   // the single StorageForm/StoreAsImage decision (§3.4) — REQUIRES ProcedureBound
}

public interface IBindPass
{
    string Name { get; }
    IReadOnlyList<Capability> Requires { get; }
    IReadOnlyList<Capability> Produces { get; }
    void Run(BindContext ctx);
}
```

`BindPipeline` owns the ordered list, validates it once, and runs it:

```csharp
public sealed class BindPipeline
{
    private readonly IReadOnlyList<IBindPass> _passes;
    public BindPipeline(IReadOnlyList<IBindPass> passes) { _passes = passes; ValidateDag(); }

    // Startup assertion: for every pass, every Required capability is Produced by an EARLIER pass.
    // A violation throws a CompilerConfigurationException at construction (fail-fast, never at runtime
    // on a user program). This is the structural cure for the pass-ordering latent-bug class (§1.1).
    private void ValidateDag() { /* accumulate produced-set; assert Requires ⊆ produced-so-far */ }

    public void Run(BindContext ctx)
    {
        foreach (var p in _passes) { p.Run(ctx); ctx.MarkProduced(p.Produces); }
    }
}
```

**Completion-phase gate (the second half of the structural cure).** The manifest guards *pass* order; a
complementary guard protects *field reads* on the data model. `BindContext` carries a `Capability` "watermark"
(highest capability produced so far). Late-resolved `DataItem`/`OccursSpec`/`RedefinesClass` facts
(`Class`, `Tier`, `ClassOffset`, `CapacityRegister`, `StorageForm`) are read through accessors that assert the
watermark has reached the producing capability — in Debug builds this is an `assert`; it converts today's silent
"read a null Tier" into a loud, located compiler error. (Reads are pervasive, so the accessor is a thin
`ctx.Require(Capability.RedefinesClassified)` at the small number of pass entry points, not per-field.)

The canonical pass list (the manifest — the ONE place a maintainer reads to learn the order) lives in
`Binding/Pipeline/BindManifest.cs`:

```csharp
public static IReadOnlyList<IBindPass> Standard() =>
[
    new DeclarationPass(),          // Produces EntryTree (OPTIONS, SPECIAL-NAMES, FILE-CONTROL, FD/SD, WS, LINKAGE, PD formals)
    new ExpandTypesPass(),          // R:EntryTree            P:TypesExpanded
    new UsageMarkerPass(),          // R:TypesExpanded        P:UsageMarkersResolved   (renamed from ResolveIndexItems)
    new UsageInheritancePass(),     // R:UsageMarkersResolved P:UsageInherited
    new SignInheritancePass(),      // R:UsageInherited       P:SignInherited
    new RedefinesResolvePass(),     // R:SignInherited        P:RedefinesResolved
    new RedefinesClassifyPass(),    // R:RedefinesResolved    P:RedefinesClassified
    new StrongTypeCheckPass(),      // R:RedefinesClassified  P:StrongTypesChecked
    new OoRedefinesRoutePass(),     // R:RedefinesClassified  P:(routing)
    new OdoResolvePass(),           // R:RedefinesClassified  P:OdoResolved
    new DynamicResolvePass(),       // R:OdoResolved          P:DynamicResolved
    new FilesResolvePass(),         // R:RedefinesClassified  P:FilesResolved
    new NationalGatePass(),         // R:FilesResolved        P:NationalGated
    new ReportsResolvePass(),       // R:FilesResolved        P:ReportsResolved
    new ExternalGlobalPass(),       // R:EntryTree            P:ExternalGlobalBound
    new PointerBindPass(),          // R:EntryTree            P:PointersBound
    new ProcedureBindPass(),        // R:AllDataFacts         P:ProcedureBound      (runs StatementBinder over every unit)
    new StorageFormPass(),          // R:ProcedureBound       P:StorageFormComputed (§3.4 — the ONE StoreAsImage decision)
];
```

This single list REPLACES both `DataBinder.BindResolve:210-258` and the binder half of `CallEmitRunUnit`. Note
`ProcedureBindPass` runs BEFORE `StorageFormPass`: whole-group usage collected during procedure binding is an
INPUT to the storage-form decision, so the today's cross-phase mutation (emitter mutating the binder after the
fact) becomes a normal upstream→downstream data flow inside one pipeline.

### 3.2 The symbol table & the immutable result (`Binding/Model/`)

`Bind()` returns an immutable `BoundCompilation`; the emitter consumes a read-only view.

```csharp
public sealed record BoundCompilation(
    IReadOnlyList<BoundUnit> Units,          // one per program-unit / class-method-set
    OoClassModel Classes,                    // resolved class table (read-only projection)
    SymbolTable Symbols,                     // the ONE name/scope resolver
    IReadOnlyList<Diagnostic> Diagnostics);

public sealed record BoundUnit(
    DataModel Data,                          // the read-only data-division model (Roots + derived facts)
    BoundProgram Procedure,                  // the bound PROCEDURE DIVISION (unchanged node shapes)
    IReadOnlyList<BoundMethod> Methods);
```

`SymbolTable` collapses the lookup quadruple into ONE scoped resolver that already understands OO method
shadowing. There is exactly one lookup entry point:

```csharp
public sealed class SymbolTable
{
    // The ONLY name lookup. `scope` is the active method scope (or Program scope for non-OO code); OO
    // sibling-invisibility (§11.7 GR5) is enforced HERE, not by callers choosing an overload.
    public bool TryResolve(QualifiedName name, Scope scope, out DataItem item);
    public bool TryResolveCondition(QualifiedName name, Scope scope, out Condition88 cond);
    public bool TryResolveIndex(QualifiedName name, Scope scope, out DataItem index);
    public IReadOnlyList<DataItem> Roots(Scope scope);
}
```

`LookupData` / `LookupDataInScopeOf` / `TryGetVisibleIndexField` / `IndexFieldFor` are DELETED; every call site
passes an explicit `Scope` (defaulting to `Scope.Program`). This makes scoped lookup the ONLY lookup — the
singular-pattern rule — and removes the "caller forgot the scoped overload → missed shadowing" bug class.

**Mutation discipline.** During binding, passes need to write into the model (register a condition, a capacity
register, a whole-group-referenced fact). They receive a `SymbolTableBuilder` (a write handle) inside
`BindContext`; the builder is sealed into the immutable `SymbolTable` when the pipeline completes. The emitter
never receives the builder — it receives `BoundCompilation`, whose collections are `IReadOnlyList`/read-only
interfaces. This closes every open write channel in §1.2 by construction. In particular the emitter's
`MarkStoreAsImage` write-back is DELETED (see §3.4).

### 3.3 The bound tree & the IR decision (§18.23 upheld)

**Decision: KEEP §1.1/§18.23 — there is NO shared lowered branch IR.** The bound tree remains the single
backend-neutral SSOT; the Roslyn backend preserves structure, and the future CIL backend does its own
branch-level lowering privately. This is owner-locked; we do not relitigate it.

**But** we draw a sharp line the current code blurs, between two things that both got loosely called "the bound
tree":

- **Lowered IR (REJECTED, unchanged):** CIL-shaped basic blocks / branch instructions. Would destroy the readable
  C# output. Not introduced.
- **Semantic normalization ON the bound tree (ADOPTED):** the bound node carries the fully-resolved *semantic
  classification* of the operation, so no consumer re-derives it. This is not a new IR layer — it is completing
  the bind-once contract that §2/`BoundTree.cs:16` already promises but the emitter violates (§1.5).

Concretely, three classifications move from emit-time re-derivation onto the bound node:

```csharp
// MOVE: BindMove computes the category-conversion kind ONCE; EmitMove becomes a pure renderer.
public enum MoveKind { Group, ElementaryAlphanumeric, ElementaryNumeric, NumericEdited,
                       AlphaEdited, FigurativeFill, FigurativeToNumericImage, RefModSlice }
public sealed record BoundMove(Place Target, BoundOperand Source, MoveKind Kind, StorageForm TargetForm) : BoundStatement;

// Table access polarity travels on the Place, not re-decided at emit (D9 correction already learned this).
// Storage form (§3.4) travels on the Place/operand, not read off a mutated DataItem flag.
```

`ConvertSource`'s emit-time category switch (`CSharpEmitter.cs:714`) is deleted; `EmitMove` reads `move.Kind`.
The emitter stops reading `DataItem.StoreAsImage`/`IsStrongGroup` — those facts arrive on the bound node.

**Place stays the lvalue abstraction but stops being a string.** Today `Place.Read()/Write()` return raw C#
(`Place.cs:124-138`). Target: `Place` holds STRUCTURED path segments (root item + subscript `BoundExpr`s + optional
ref-mod span); the *emitter* owns rendering path→C# text. This removes the "binder builds emit-time strings"
leak (§1.5) and lets the CIL backend render the same structured Place differently. (Detailed Place shape is owned
by the data-model sibling design; this doc requires only that Place carry structure, not text, so the binder
produces no C# strings.)

**Dispatch: source-generated exhaustive visitor.** Add to `BoundTree.cs`:

```csharp
public interface IBoundStatementVisitor<out T> { T VisitMove(BoundMove n); /* … one per node … */ }
public abstract partial record BoundStatement { public abstract T Accept<T>(IBoundStatementVisitor<T> v); }
```

A small **source generator** (`Binding/Bound/BoundVisitor.g`) emits the per-record `Accept` overrides and the
visitor interface from the record hierarchy, so adding a `BoundStatement` record without handling it in every
visitor is a COMPILE error. `EmitStatement`, `BoundStoreAnalysis` (renamed from `BoundStores`),
`NumericRenderer`, `ConditionRenderer`, `OperandText`, `AlterCollectFields` all convert from a `switch`+`_ =>`
into `IBoundStatementVisitor<T>` implementations. The loud runtime defaults are DELETED. The five error families
(`BoundUnsupported`/`BoundOperandError`/`BoundExprError`/`BoundConditionError`/`BoundBoolError`) get one
`IBoundError` marker so a visitor handles "any error node" once. (If the owner declines a source generator, the
fallback is a hand-written `abstract` visitor base with no default method — the compiler then forces every
visitor to implement every node; slightly more boilerplate, same exhaustiveness guarantee. See OPEN QUESTION 1.)

### 3.4 The `StorageForm` pass — killing the `StoreAsImage` mutable flag

`StoreAsImage` is today recomputed/mutated at 7+ sites across three layers (§1.2, dossier duplication-dispatch
HIGH). The data-model sibling design defines a single computed `StorageForm` discriminator (NativeLong / Int128 /
Float / Double / StringImage / TierBWindow / TierCByte / DynTable / ObjectRef / Pointer). This binder design owns
WHERE it is computed: **`StorageFormPass`, a single pass running after `ProcedureBindPass`** so it sees all facts
including whole-group procedure-division use. It writes `StorageForm` as an init-only fact through the builder;
after the pipeline it is immutable. `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) and the
`CompilerTempClones` re-sync are DELETED. The FILE-record whole-group loop at `DataBinder.cs:238-257` folds into
`StorageFormPass` (it is the same rule). This removes the last emitter→binder write-back.

### 3.5 God-class decomposition — collaborators over `BinderContext`

`BindContext` is the shared, mostly-immutable spine threaded to every pass and every collaborator:

```csharp
public sealed class BindContext
{
    public required Core.ProgramUnitContext Parse { get; init; }
    public required EditionInfo Edition { get; init; }          // immutable (editions sibling design)
    public required IDiagnosticSink Diagnostics { get; init; }  // the ONE sink (editions/driver sibling design)
    public required SymbolTableBuilder Symbols { get; init; }
    public required ReferenceResolver Refs { get; init; }
    public required RecordLayout Layout { get; init; }          // the ONE offset/width service (dossier reorg)
    public Capability Watermark { get; private set; }           // completion-phase gate (§3.1)
    // scoped push/pop for OO method binding, replacing ambient ActiveMethodScope mutation:
    public IDisposable EnterMethodScope(Scope s);
}
```

`StatementBinder`'s 23 partials become real classes over `BindContext`, matched to the existing seams:
`ProcedureTableBuilder` (paragraphs/sections/declaratives/pc + `ResolveProcedure`), `ExpressionBinder`,
`ConditionBinder` (incl. `AbbrevCarry`, `CheckedRelational`), and per-verb binders `MoveBinder`, `ArithmeticBinder`,
`IfBinder`, `PerformBinder`, `KeyedIoBinder`, `SequentialIoBinder`, `SortBinder`, `StringBinder`, `InspectBinder`,
`InitializeBinder`, `IntrinsicBinder`, `UdfBinder`, `ReportWriterBinder`, `CallBinder`, `SetBinder`, `OoBinder`.
The core `StatementBinder` shrinks to the dispatch table + shared helpers. `DataBinder` likewise splits into
`FileControlBinder`, `SpecialNamesBinder`, `ReportSectionBinder`, `RedefinesClassifier`, `TypedefExpander`,
`LinkageBinder`, `PointerBinder`, `OoMethodDataBinder` — each a pass or a pass-owned collaborator.

**Inline SR/edition validation moves out.** The `data.Edition.Error(...)` calls smeared through the binder
(MOVE figurative gates, composite-of-operands, boolean/class/pointer relation rules) route through a
`StatementValidation` component beside `Validation/EditionValidator`, keeping the binder about *producing bound
nodes* (`feedback_binder_no_ir` spirit). The binder calls `validation.CheckMove(...)`; the validation component
owns "what is legal at which edition" and reports to the `IDiagnosticSink`.

**Shared helpers deduplicated (one canonical mechanism per job):** `PhraseBlocks.BuildPair(blocks, notFirst)`
(the ON/NOT-ON extractor, ~8 clones → 1), `RecordLayout` (offset/width/key-index, replacing Sort vs KeyedIo vs
FieldEmitter vs OdoModel copies), `CobolLiteral.Decode` (the tripled `DecodeCobolString` → one Common/ codec),
`DataItem.Root` (the 4× RootOf walk → one accessor), `FigurativeConstants` (the 4-site figurative-fill → one
service). These are cross-cutting with the emitter-renderer sibling design; the binder owns the bind-side callers.

---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Binding/Pipeline/IBindPass.cs`, `BindPipeline.cs`, `BindManifest.cs`, `Capability.cs`, `BindContext.cs` | Explicit, DAG-validated pass ordering; the structural cure for §1.1 |
| split | `DataBinder.BindResolve` (`DataBinder.cs:210-258`) | ~15 `IBindPass` classes (`ExpandTypesPass`, `RedefinesClassifyPass`, …) | Each pass declares Requires/Produces; ordering asserted at startup |
| move | binder half of `CSharpEmitter.CallEmitRunUnit` (`CSharpEmitter.Call.cs:88-147`) | `ProcedureBindPass` + `StorageFormPass` + `ExternalGlobalPass` in `BindPipeline` | Extract the hidden second pipeline out of CodeGen into the real Binder phase |
| create | — | `Binding/Model/BoundCompilation.cs`, `BoundUnit.cs`, `DataModel.cs` | Immutable result the emitter consumes; ends the mutable-blackboard API |
| create | — | `Binding/Model/SymbolTable.cs` + `SymbolTableBuilder.cs` | One scoped resolver; write only via builder inside passes |
| merge/delete | `LookupData`, `LookupDataInScopeOf`, `TryGetVisibleIndexField`, `IndexFieldFor` | `SymbolTable.TryResolve*` (scope-aware) | Collapse the lookup quadruple; scoped lookup is the ONLY lookup |
| refactor | `DataBinder` ~30 public mutable dictionaries (`DataBinder.cs:26-77`) | private builder state → sealed into `BoundCompilation`/`SymbolTable` | Close every open write channel; no cross-phase mutation |
| split | `DataBinder` (7 partials, 3.9k LOC) | `FileControlBinder`, `SpecialNamesBinder`, `ReportSectionBinder`, `RedefinesClassifier`, `TypedefExpander`, `LinkageBinder`, `PointerBinder`, `OoMethodDataBinder` | God-class → focused pass collaborators over the model |
| split | `StatementBinder` (23 partials, 9.4k LOC) | `ProcedureTableBuilder`, `ExpressionBinder`, `ConditionBinder`, + per-verb `*Binder` classes | Real class boundaries over `BinderContext`; kill shared-private-state coupling |
| create | — | `Binding/Validation/StatementValidation.cs` | Move inline `data.Edition.Error` SR/edition gates out of the binder |
| create | — | `Binding/Bound/BoundVisitor.g` (source generator) + `IBoundStatementVisitor<T>` etc. in `BoundTree.cs` | Exhaustive dispatch; missing arm = compile error |
| refactor | `BindStatementCore` switch (`StatementBinder.cs:170-231`) | thin `Accept`-dispatching binder table (parse→bound stays a switch; it is the ONE parse-tree seam) | Parse dispatch is inherently a switch; only the BOUND-tree consumers become visitors |
| refactor | `EmitStatement` switch (`CSharpEmitter.cs:347`), `BoundStores.StoreKindOf`, `NumericRenderer`, `ConditionRenderer`, `OperandText` | `IBoundStatementVisitor<T>` implementations | Delete the loud `_ =>` defaults; exhaustiveness by type system |
| add | `BoundMove` (`BoundTree.cs:317`) et al. | `MoveKind` + `StorageForm` fields on the node | MOVE classification computed once in binder; `EmitMove`/`ConvertSource` become pure renderers |
| create | — | `Binding/Pipeline/StorageFormPass.cs` | Single owner of the storage-form decision, after procedure binding |
| delete | `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), `CompilerTempClones` re-sync, FILE whole-group loop (`DataBinder.cs:238-257`) | folded into `StorageFormPass` | Remove the emitter→binder write-back; one StoreAsImage rule |
| rename | `StatementBinder.Accept.cs`, `CSharpEmitter.Accept.cs` (the ACCEPT *verb*) | `*.AcceptStatement.cs` | End the Visitor-term collision once a real visitor exists |
| rename | `BoundStores` | `BoundStoreAnalysis` | It is an analysis, not storage |
| create | — | `Common/CobolLiteral.cs` (Decode), `Binding/RecordLayout.cs`, `Binding/PhraseBlocks.cs`, `DataItem.Root` | One canonical helper per job (dedup) |
| move | `ReferenceResolver` sub-parsers (`SplitSubscriptTokens`/`InterpretSubscripts`, `ReferenceResolver.cs:377-431`) | `SubscriptTokenParser` + `NameResolver` collaborators | Thin the resolver; it becomes an orchestrator over SymbolTable |
| retire (G8) | `using Core = CobolParserCore; using CobolSharp.Compiler.Generated;` (`StatementBinder.cs:6,11`) | `CobolNet.Frontend.Generated` | Decouple from the legacy generated namespace at cut-over (driver/frontend sibling) |

---

## 5. Migration notes — keeping the battery green throughout

> **LANDED STATUS (P6 close, 2026-07-11 — DEVLOG 767–774).** Steps 1–3 below are DONE: (1) the pass framework +
> `ValidateDag` landed at P5 Step 3 and P6 extended it to the whole chain (`BindPipeline.Build` prefix ++
> `GroupTail` manifest, `ValidateFullChainOnce`, the DEBUG watermark gate); (2) the binder half of
> `CallEmitRunUnit` is `Binding/BinderDriver.Bind` → immutable `BoundCompilation`, driver Phase 2 = Bind → gate →
> CheckOnly → EmitBound; (3) the read-only views + the ONE scope-aware `SymbolTable` landed and the
> `LookupData`/…/`IndexFieldFor` quadruple is DELETED (`SymbolTableBuilder`-owned storage deferred to P7 — the
> table wraps the live maps; `ReferenceResolver.ResolveUnqualified` + the StatementBinder condition lookup still
> carry the same precedence inline, P7 candidates). Step 4 is HALF-done: `StorageFormPass.Run` owns the whole
> StoreAsImage settle sequence bind-side and the emitter write-back is gone; the FLAG deletion + reader flips are
> EXEC STEP C (P5 Steps 6–14). Steps 5–7 remain P7 scope; step 8 is G8.

The migration is a sequence of behavior-preserving refactors; the 2028 conformance + 213 unit + NIST-353 legacy
guard stays green at every commit. Order chosen so each step is independently shippable and reversible.

1. **Introduce the pass framework as a no-op wrapper first.** Wrap each existing `BindResolve` method body in an
   `IBindPass` with correct `Requires`/`Produces`, keep the SAME call order, add `BindPipeline.ValidateDag()`.
   Zero behavior change; the manifest now documents the order and the DAG assert catches future reorders. Run the
   full battery. (This alone retires §1.1's silent-reorder risk.)
2. **Extract the binder half of `CallEmitRunUnit` into passes.** Move `ProcedureBindPass`/`ExternalGlobalPass`
   into the pipeline; `CSharpEmitter` calls `BindPipeline.Run` and then only emits. The driver's Phase 2 splits
   into Bind + Emit. Battery green (pure move).
3. **Wrap the mutable collections behind `SymbolTableBuilder`/read-only views WITHOUT changing lookup semantics.**
   Keep `LookupData` etc. as thin shims over `SymbolTable.TryResolve` initially; delete the shims once all callers
   pass a `Scope`. This is the riskiest step for OO shadowing — land it behind the OO conformance goldens and the
   method-scope unit tests; do OO last.
4. **StorageForm pass.** Introduce `StorageFormPass` computing the discriminator; have the emitter READ the new
   fact while `MarkStoreAsImage` still runs, assert they agree across the whole corpus (a temporary
   cross-check), THEN delete `MarkStoreAsImage`. This is the safe way to retire a 7-site mutable flag: prove
   equivalence before deletion. (Ties to the data-model sibling design landing `StorageForm` first.)
5. **Semantic normalization on BoundMove (and peers) + the visitor.** Add `MoveKind` to `BoundMove`, compute it
   in `BindMove`, switch `EmitMove` to read it, delete `ConvertSource`'s re-classification. Then land the source
   generator and convert consumers to `IBoundStatementVisitor<T>` ONE consumer at a time (emitter first, then
   `BoundStoreAnalysis`, then renderers) — each conversion is mechanical and independently testable.
6. **Class-boundary split.** Convert partials to real collaborators over `BinderContext` incrementally, verb by
   verb (the existing per-verb partial files map 1:1 to the new classes). Each verb: extract class, inject
   context, run battery, commit.
7. **Helper dedup + renames** (`CobolLiteral.Decode`, `RecordLayout`, `PhraseBlocks`, `DataItem.Root`,
   `BoundStores`→`BoundStoreAnalysis`, ACCEPT-verb rename) as small independent commits, each battery-green.
8. **G8 namespace retirement** happens with the frontend/driver cut-over, not here.

Rule throughout: **prove-then-delete** for every mutable flag / duplicated computation (compute the new form,
cross-check against the old across the corpus, delete the old). Never delete a mutation site on faith.

---

## 6. Risks

- **R1 — OO method-scope shadowing regression (HIGH).** Collapsing the lookup quadruple into one scoped resolver
  is the single most behavior-sensitive change; §11.7 GR5 sibling-invisibility is subtle. Mitigation: land last,
  behind the OO goldens + method-scope unit tests, with a temporary shim-and-cross-check.
- **R2 — StorageForm equivalence gaps (HIGH).** `StoreAsImage` has accreted special cases (SORT SD records, FILE
  whole-group, compiler temps). A single `StorageFormPass` must reproduce ALL of them. Mitigation: the
  prove-then-delete cross-check in step 4 across the full corpus before removing `MarkStoreAsImage`.
- **R3 — Source-generator build complexity (MEDIUM).** Adds a Roslyn source generator to the build; regen must be
  portable across both OSes (matches the existing ANTLR-regen constraint, `feedback_commit_generated_parser`).
  Mitigation: the hand-written `abstract` visitor fallback (§3.3) removes the generator dependency if it proves
  costly.
- **R4 — Pass granularity churn (MEDIUM).** Over-fine passes multiply forest re-walks (the efficiency critique
  notes ~15 passes each re-walk via a fresh `AllItems()`). Mitigation: passes may share a single cached
  `BindContext.AllItems` snapshot; granularity is chosen at the natural capability boundaries above, not finer.
- **R5 — Place-becomes-structured ripples into the emitter (MEDIUM).** Removing C#-string emission from Place/
  binder touches every renderer. Mitigation: this is co-owned with the emitter sibling design; sequence it with
  their `RuntimeApi` façade work so the structured-Place render lands once.
- **R6 — Long-lived migration branch drift (MEDIUM).** The split spans many commits while feature work
  continues. Mitigation: each step is independently shippable to `main` behind the green battery; no long branch.

---

## 7. Open questions for the owner

1. **Source generator vs hand-written abstract visitor** for exhaustive bound-tree dispatch? The generator gives
   the cleanest ergonomics (auto `Accept` + interface) but adds a build-time generator (portability/regen cost,
   like ANTLR). The hand-written `abstract` visitor base achieves the same compile-time exhaustiveness with more
   boilerplate and no generator. Default recommendation: **source generator**, fallback ready.
2. **Capability granularity — enum vs per-pass typed tokens?** The `Capability` enum is simple and greppable but
   a pass could technically read a capability it did not declare (the enum does not enforce field-level access).
   A stricter design keys each late-resolved field behind a phase token type. Recommendation: **enum + the
   watermark accessor gate** (§3.1) — full field-level enforcement is likely over-engineering.
3. **Does the completion-phase gate ship in Release builds?** Debug-only `assert` is free but catches nothing in
   production; a Release guard costs a branch per pass entry. Recommendation: **Debug assert + a one-time Release
   DAG validation at pipeline construction** (the DAG validation is cheap and always on; the per-read watermark
   check is Debug-only).
4. **Scope of the "semantic normalization on the bound tree" (§3.3).** MOVE is the clear case. Should
   INSPECT/STRING/UNSTRING/arithmetic-store also carry fully-classified kinds now, or only where the emitter
   currently re-derives? Recommendation: **only where the emitter re-derives today** (MOVE, storage form, table
   polarity); expand opportunistically, do not pre-classify everything.
5. **Confirm the parse→bound dispatch stays a switch.** `BindStatementCore` is inherently a parse-tree switch
   (ANTLR contexts, no bound node yet to `Accept`). Only BOUND-tree consumers become visitors. Confirm this
   asymmetry is acceptable (it is the correct design — you cannot visit a node that does not exist yet).

---

## 8. Dependencies on sibling design dimensions

- **Data model (`DataItem`/`PicInfo`/`Place`/`StorageForm`):** owns the `StorageForm` discriminator shape and the
  structured (non-string) `Place`. This design REQUIRES those two and owns *where/when* StorageForm is computed
  (`StorageFormPass`) and that Place carries structure not text.
- **Emitter/renderer decomposition:** consumes `BoundCompilation` (read-only) and the exhaustive visitor;
  co-owns `RecordLayout`, `FigurativeConstants`, `CobolLiteral.Decode`, and the structured-Place render.
- **Editions framework:** provides the immutable `EditionInfo` + the single `IDiagnosticSink` that `BindContext`
  carries; `StatementValidation` reports through it.
- **Driver & pipeline shell:** the extracted Binder phase makes the driver's Phase 2 literally Bind then Emit;
  `CheckOnly`/`NoEmit` becomes "stop after `BindPipeline.Run`" (the efficiency critique's LOW item), enabled by
  this design's phase boundary.
