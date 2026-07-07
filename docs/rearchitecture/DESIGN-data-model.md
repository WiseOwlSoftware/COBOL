# DESIGN — Target Unified Data Model

> **Status:** DESIGN (rearchitecture wave). Decision-complete target for the data-representation dimension.
> **Owner sign-off required** on the open questions in the last section before execution.
> **Scope:** the typed-native storage model spanning `DataItem` / `PicInfo` / `Place` / `NumProfile`, from
> bind → emit → runtime. Companion designs (pass-pipeline, emitter decomposition, diagnostics registry, editions)
> are cross-referenced but owned elsewhere.
> **Upholds the hard invariants:** typed-native data only (byte[] confined to a genuine Tier-C / file boundary);
> spec-first (ISO/IEC 1989:2023, cited by §); battery stays green every phase; singular pattern; no-god-class;
> C# 14 / .NET 10; four-editions-in-one.

---

## 1. The current problem (grounded in the code)

The compiler has **one canonical lvalue contract** (`Place`, `Binding/Place.cs:13`) but **no canonical value
representation**. A single COBOL value can be stored five structurally different ways, and *which one* is decided by
facts scattered across three layers and, for the pivotal case, **mutated late**:

| Representation | Where it lives today | Decided by |
|---|---|---|
| Native `long` / `Int128` | `PicInfo.ClrType` → `DataItem.ElementType` (`DataItem.cs:242,304`) | PICTURE analysis (early) |
| Native `float` / `double` | `PicInfo.ClrType` (`PicInfo.cs:244`) | PICTURE/USAGE analysis (early) |
| `string` character image | `DataItem.StoreAsImage` flips `ElementType` to `"string"` (`DataItem.cs:304`) | **late mutation** |
| Tier-B `(offset,width)` window over one shared `string` | `RedefViewPlace` + `RedefinesClass.Tier` (`Place.cs:82`, `RedefinesModel.cs:46`) | REDEFINES classification pass |
| Out-of-line `CobolDynTable<T>` | `DataItem.FieldType` (`DataItem.cs:309`) | OCCURS DYNAMIC (early) |
| (declared, unimplemented) Tier-C `byte[]` | `RedefinesTier.ByteCanonical` (`RedefinesModel.cs:48`) | classification → **Rejected loud** |

### 1.1 The pivotal defect: `StoreAsImage` is a late-mutated, cross-layer flag

A numeric `USAGE DISPLAY` leaf is stored as a native `long`/`Int128` **unless** it lives under a group referenced as
a whole operand — then it must become a `string` (ISO §14.9 MOVE GR4 fills a group "without consideration for the
individual elementary items", so the leaf can receive spaces a `long` cannot hold). That fact is only known **after
PROCEDURE DIVISION binding**. Today it is realized by mutating `DataItem.StoreAsImage` (`DataItem.cs:169`, a public
`get; set;`) from **7+ sites across three layers**:

- binder data pass — `DataBinder.cs:255,1562,1577`, `DataBinder.Linkage.cs:275`, `DataBinder.Reports.cs:352`
- binder procedure pass — `StatementBinder.MoveFigurative.cs:123,262`
- **the emitter writes the binder's data model** — `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`),
  driven from `WholeGroupReferenced`, which `ReferenceResolver` itself mutates *during* procedure resolution
  (`ReferenceResolver.cs:280,303`), plus an OO re-sync (`CSharpEmitter.Oo.cs:697`) and a CompilerTemp re-sync
  (`CSharpEmitter.Call.cs:118`).

Consequences, all called out by the survey/critique:

1. **Cross-layer write-back** — CodeGen mutates the Binding data model (`MarkStoreAsImage`), so *emit correctness is
   order-dependent on a bind-time side effect*. The data model has no single owning phase.
2. **Reconciled by C# overload resolution.** Because the field type is undecided when the binder emits reference text,
   the runtime exposes *polymorphic overloads* so the SAME emitted string compiles either way:
   `CobolTable.Occ(long)` / `Occ(string)` (`CobolTable.cs:41,44`); `CobolNum.StoreDisplay(…, long)` /
   `(…, Int128)` / `(…, string)` (`CobolNum.cs:227,231,235`); `FormatDisplay(Int128,…)` / `(string,…)`
   (`CobolNum.cs:212,220`). Elegant, but it means a representation bug is **invisible until the generated C# hits
   Roslyn** and picks the wrong overload.
3. **Recompute-on-read.** `IsCharacterImage` / `IsImageCapable` / `ImageWidth` / `StrongRoot` are recursive
   computed properties (`DataItem.cs:74-83,245-300`) re-walked at ~119 sites — each an O(subtree) walk, and each
   silently sensitive to whenever `StoreAsImage` last flipped.

### 1.2 Secondary structural problems

- **`DataItem` is a mutable god-struct** (`DataItem.cs`, 326 loc): ~20 `set`/`init` members mixing an *immutable*
  declared shape (level, name, PIC, children) with *pass-computed* facts (`RedefinesTarget`, `Class`, `ClassOffset`,
  `IsCanonical`, `StoreAsImage`, `TypeName`, `StrongType`, `Uid`). Validity is phase-dependent with no guard.
- **Implicit pass ordering.** `DataBinder.BindResolve` runs ~15 passes ordered only by call sequence + comments
  (`DataBinder.cs:210-232`); the *real* middle-end orchestration (another ~12 passes incl. `MarkStoreAsImage`) is
  hidden inside `CSharpEmitter.CallEmitRunUnit` (`CSharpEmitter.Call.cs`). No pass declares what it requires/produces.
- **Duplicated physical-width geometry.** `DataItem.ImageWidth` (`:283`), `OdoModel.PhysicalWidth` (`:155`), and
  `FieldEmitter`'s layout each compute record character offsets/widths independently and *must agree*
  (`OdoModel.cs:154` comment: "mirrors FieldEmitter's physical layout"). Sort/Keyed add two more copies.
- **`RedefinesClass.Tier` / `.Width` / `ClassOffset`** are set-then-overwritten across passes (mutable temporal state).
- **Tier-C is declared but unimplemented** — `ByteCanonical` exists in the enum and ~10 scattered guards defer to it
  (`DataBinder.cs:1606,1631,1642`, `RedefinesModel.cs:48`, `CSharpEmitter.cs:565,615,1759,1794`).
- **PicInfo carries dead skeleton scaffolding** (`IsUnimplementedSkeleton => false`, `SkeletonReached`,
  three `ReferenceEquals` sentinel singletons — `PicInfo.cs:175,216,691-705`), and its 230-line `Analyze` scanner
  makes it not a pure value record.
- **Latent correctness bug (critique):** apostrophe-delimited VALUE literals are silently miscompiled because the
  VALUE-emit path hard-codes the `"` delimiter (`EmitCore.cs:162-168`, `FieldEmitter.cs:329-332`). This is a
  data-model-adjacent symptom of ad-hoc value handling and is folded into this design's cleanup.

---

## 2. The target design

### 2.0 One-paragraph thesis

Introduce **`StorageForm`** — a single, closed, computed discriminator that names *exactly how a value is stored*
(native int / native float / character image / Tier-B window / Tier-C byte / out-of-line table / object-ref /
pointer / index). It is computed **once**, by a **`StorageFormPass`** that runs after all facts (including
procedure-division whole-group use) are collected, and stored **init-only** on `DataItem`. Every downstream reader —
`FieldEmitter`, `NumericRenderer`, `OperandText`, `Place` construction — asks `StorageForm`, never re-infers from
`(Pic, StoreAsImage, Class.Tier, IsDynamicTable)`. `StoreAsImage` the mutable flag is **deleted**; the emitter's
`MarkStoreAsImage` write-back is **deleted**. The runtime overload bridge remains as a convenience but is **no longer
load-bearing** — by emit time the form is known, so the emitter *chooses* the read/write expression explicitly.

### 2.1 `StorageForm` — the canonical value-representation discriminator

New file `Binding/Model/StorageForm.cs`. A sealed abstract record with one case per physical representation. It is a
**value classification** (no C# strings, no emit logic) — the emitter interprets it.

```csharp
namespace CobolNet.Binding.Model;

/// The canonical storage representation of an elementary item or table element (COBOLNET_DESIGN §3/§4/§14.4).
/// Computed ONCE by StorageFormPass; init-only on DataItem. Closed set — a new representation is a new case,
/// and every switch over it is exhaustive (a missing arm is a COMPILE error, not a runtime LoudStmt).
public abstract record StorageForm
{
    /// Does this leaf contribute character positions to an enclosing group's AsImage()/FromImage() codec (§14.4)?
    public abstract bool IsCharacterImage { get; }
    /// Character-image width contributed (meaningful only when IsCharacterImage); 0 otherwise.
    public abstract int ImageWidth { get; }

    // ── The cases ─────────────────────────────────────────────────────────────────────────────────────────
    /// Native scaled integer: long (≤18 digits) or Int128 (19–38). NOT character-imageable as itself, but
    /// IS image-CAPABLE (its zoned digit image is derived on demand — see IsImageCapable on the item).
    public sealed record NativeInt(bool Wide, int Digits) : StorageForm { … }
    /// Native IEEE float (COMP-1/FLOAT-SHORT) or double. Never in a static record image (loud Tier-C island).
    public sealed record NativeFloat(bool Single) : StorageForm { … }
    /// A C# string of exactly Width characters: alphanumeric / numeric-edited / national / boolean, OR a
    /// numeric-DISPLAY leaf promoted to its zoned image because it is used under a whole-group operand (§14.9 GR4).
    /// Category is retained so the numeric pipeline decodes/encodes zoned images (ParseDisplay/FormatDisplay).
    public sealed record CharImage(int Width, PicCategory Category) : StorageForm { … }
    /// A Tier-B REDEFINES view: a typed (offset, width) window over the class's ONE shared string backing.
    public sealed record TierBWindow(RedefinesClass Class, int Offset, int Width) : StorageForm { … }
    /// A Tier-C REDEFINES view over the class's ONE confined byte[] (§4.2 tier C). QUARANTINED until implemented.
    public sealed record TierCWindow(RedefinesClass Class, int Offset, int Length, Usage Usage) : StorageForm { … }
    /// An out-of-line OCCURS DYNAMIC table (CobolDynTable<T>, D9); Element is the per-occurrence element's form.
    public sealed record DynamicTable(StorageForm Element) : StorageForm { … }
    /// A .NET object reference (typed class or universal CobolObject?). Zero character positions.
    public sealed record ObjectRef(string? ClassName) : StorageForm { … }
    /// A data pointer (ManagedPointer). Zero character positions.
    public sealed record PointerRef : StorageForm { … }
    /// A USAGE INDEX cell (long occurrence number). Zero character positions.
    public sealed record IndexCell : StorageForm { … }
}
```

**Key rule (unifies invariant #1 + #2):** `CharImage` is the ONE case that subsumes *every* string-stored leaf,
including a numeric-DISPLAY leaf promoted by whole-group use and an image-stored Tier-B binary/packed view. The
promotion is a `StorageForm.NativeInt → CharImage` transition **inside `StorageFormPass`**, never a mutable bool flip.

The old boolean triad is replaced by pure functions on the item's form (or cached fields — see §2.4):

- `DataItem.IsCharacterImage` → `Storage.IsCharacterImage` (leaf) / `Children.All(c => c.Storage.IsCharacterImage)` (group)
- `DataItem.ImageWidth` → owned by `RecordLayout` (§2.6), reads `Storage.ImageWidth`
- `DataItem.StoreAsImage` → **deleted**; the equivalent query is `Storage is CharImage { Category: not Alphanumeric }`
  where a call site truly needs "numeric leaf stored as image" (there are ~5 such sites; the rest just needed the type).

### 2.2 The canonical `Place` lvalue model

`Place` stays the ONE lvalue contract (`Read()` → rvalue C# expr, `Write(rhs)` → C# store statement). The
consolidation:

1. **One file, one decorator base.** Move `OdoGroupPlace` (currently in `OdoModel.cs:89`) into `Place.cs`. Introduce
   `abstract record PlaceDecorator(Place Inner) : Place` forwarding `Pic`/`Item`; make `NumericImagePlace`,
   `RefModPlace`, `OdoGroupPlace`, `RenamesPlace` derive from it. Leaf places (`MemberPlace`, `DynTablePlace`,
   `RedefViewPlace`, `CapacityRegisterPlace`) stay direct.
2. **`Place` is built from `StorageForm`, not re-inference.** `ReferenceResolver` selects the concrete place from the
   resolved item's `Storage`:
   - `NativeInt`/`NativeFloat`/`CharImage`(non-numeric) → `MemberPlace`
   - `CharImage`(numeric, i.e. a promoted DISPLAY leaf) used numerically → `MemberPlace` whose numeric read/write goes
     through `NumericImagePlace` (the ParseDisplay/FormatDisplay bridge) — but the *decision* is now read off
     `Storage`, not the late `StoreAsImage` flag, so `NumericImagePlace`'s remark about "decided AFTER this text is
     produced" (`Place.cs:156-159`) is **retired**.
   - `TierBWindow` → `RedefViewPlace`
   - `DynamicTable` element → `DynTablePlace`
   - a level-66 view → `RenamesPlace`
3. **`CapacityRegisterPlace`** becomes a proper read-only view type (it already throws on `Write`); mark it
   `IReadOnlyPlace` so the store-polarity analysis can reject a write at bind time structurally rather than by comment.
4. **Longer-term (owner-gated, see open Q3):** replace the raw-string `Path`/`OffsetExpr` fields with structured
   segments (`item + subscript BoundExpr[]`) so the binder stops assembling emit-time C# (`Initialize.cs:327-385`,
   `TryExpandAll`). This restores the §2 bind/emit boundary. **Phase-gated** — not required to land StorageForm.

### 2.3 The REDEFINES tier model (unchanged semantics, owned representation)

The 4-tier lattice (A Alias ⊑ B StringCanonical ⊑ C ByteCanonical ⊑ D Rejected, `RedefinesModel.cs:37-53`) is
**correct and kept**. Changes are about ownership, not behavior:

- Tier classification produces `StorageForm` for each member: Tier-A view → the canonical's form; Tier-B view →
  `TierBWindow`; Tier-C view → `TierCWindow`; Tier-D → a rejection diagnostic (no form emitted).
- `RedefinesClass.Tier`/`.Width` become **init-only**, set once by `RedefinesClassifier` (§2.5) — no set-then-overwrite.
- **Tier-C decision (resolves the ~10 scattered guards):** implement the confined `byte[]` codec **OR** single-source
  the rejection. This design recommends **single-source-the-rejection now, implement later**: one
  `RedefinesClassifier.RejectTierC(class, reason)` emitting one diagnostic code, and `TierCWindow.Read/Write` throw
  the internal-error backstop. Delete the ~10 inline Tier-C-deferred `if` guards in favor of the single verdict.
  (Full byte[] codec is a separately-scheduled increment — the sanctioned single byte boundary of invariant #1.)

### 2.4 `DataItem` — slim core + init-only pass facts

Split the god-struct into a **mostly-immutable declared core** plus **init-only pass-computed facts**, all in
`Binding/Model/`:

- **Declared core (immutable after `BindEntries`)**: `Level`, `CobolName`, `CsName`, `Pic`, `OwnSign`, `OwnUsage`,
  `RawValue`, `Occurs`, `OccursSpec`, `IndexNames`, `Justified`, `BlankWhenZero`, `Children`, `Own88s`,
  `RedefinesTargetName`, `Renames`, `IsTypedef`, `TypedefStrong`, `Parent`. (`Parent`/`Uid` set during tree build,
  then frozen.)
- **Pass-computed facts → init-only, each written by exactly ONE named pass** (asserted by the pipeline, §2.5):
  `TypeRefName`/`TypeName`/`StrongType` (TypedefExpander), `RedefinesTarget`/`Class`/`ClassOffset`/`IsCanonical`
  (RedefinesClassifier), `IsBased` (PointerBinder), **`Storage`** (StorageFormPass).
- **Deleted:** `StoreAsImage` (the mutable flag). **Replaced:** the recursive `IsCharacterImage`/`IsImageCapable`/
  `ImageWidth`/`StrongRoot` computed properties become **cached init-only fields** filled bottom-up in a single
  O(n) walk by StorageFormPass (image facts) and TypedefExpander (`StrongRoot`). This removes the ~119 per-access
  re-walks (critique efficiency finding).
- `ElementType`/`FieldType`/`ClrType` become thin projections of `Storage` (+ `Occurs` for the `[]` wrap), not of
  `Pic` + `StoreAsImage`.

`SameStrongType`/`TypeAnchor`/`RelativeMemberPath` (`DataItem.cs:101-133`) move to a `StrongTypeModel` static helper —
they are strong-typing logic, not core shape.

### 2.5 The explicit bind pipeline (the pass contract)

Replace `BindResolve`'s comment-ordered calls **and** the emitter-hidden binder passes with a declared pipeline in
`Binding/Passes/`. (Full pipeline design is the companion `DESIGN-pass-pipeline.md`; this section fixes only the
**data-model-relevant ordering** this dimension depends on.)

```csharp
interface IBindPass { string Name { get; } PassPhase Requires { get; } PassPhase Produces { get; } void Run(BindModel m); }
```

Ordered, with the **data-model dependency chain made explicit**:

1. `ExpandTypesPass` (TYPEDEF/TYPE clone; produces TypeName/StrongType; sets StrongRoot cache)
2. `UsageInheritancePass` (merge of `InheritUsageClauses` + `ResolveIndexItems`, renamed — §2.7)
3. `SignInheritancePass`
4. `RedefinesClassifier` (produces Class/Tier/ClassOffset/IsCanonical/Width; Tier-C verdict)
5. `StrongTypeCheckPass`
6. `OdoResolvePass`, `DynamicResolvePass` (produce OccursSpec.Depending/CapacityRegister)
7. `FileResolvePass`, `ReportResolvePass`, `Linkage/Pointer/Oo` binders
8. **procedure binding** (StatementBinder → BoundProgram)
9. **`UsageCollectionPass`** — walks the BOUND tree, collects `WholeGroupReferenced` (moved OUT of
   `ReferenceResolver`'s mid-resolve mutation and OUT of the emitter). **This is the new owner of that fact.**
10. **`StorageFormPass`** — the LAST data-model pass. Computes `DataItem.Storage` for every item: base form from
    `Pic`/`Usage`/tier/dynamic, then applies the whole-group promotion (`NativeInt → CharImage`) for numeric-DISPLAY
    leaves under a `WholeGroupReferenced` group. Fills the image-fact caches bottom-up.
11. **emit** — reads `Storage` only; no data-model writes.

The pipeline asserts at startup that no pass reads a fact before its producing pass (a `PassPhase` enum guards
width/offset/form reads). This is where "implicit pass-ordering" — the prime smell — is structurally killed.

### 2.6 `RecordLayout` — one physical-width authority

New `Binding/Model/RecordLayout.cs`: the single owner of character offset/width geometry over the record tree, reading
`StorageForm.ImageWidth`. Exposes `ImageWidth(item)`, `PhysicalWidth(group)` (tier-aware), `OffsetOf(leaf)`,
`KeyIndexByPosition(...)`. Delete the four independent copies: `DataItem.ImageWidth` recursion (`:283`),
`OdoModel.PhysicalWidth` (`:155`), `Sort` geometry (`Sort.cs:483-540`), `KeyedIo` geometry (`KeyedIo.cs:335-375`).
`FieldEmitter` and the codec consume `RecordLayout` so they cannot drift.

### 2.7 `PicInfo` / `PictureAnalyzer` / `NumProfile`

- `PicInfo` becomes a **pure analyzed-picture value record**. Extract the 230-line `Analyze` scanner
  (`PicInfo.cs:333-562`) into `Binding/PictureAnalyzer.cs` (`PicInfo Analyze(...)`). Delete the dead skeleton
  scaffolding (`IsUnimplementedSkeleton => false`, `SkeletonReached`, and the three `ReferenceEquals` sentinel
  singletons `NationalUsagePending`/`BitUsagePending`/`RecoveryItem` — replace with a proper `PicAnalysis` result
  discriminant `Ok | GroupUsageShed | Recover`).
- Rename `ResolveIndexItems` → fold into `UsageInheritancePass` (§2.5 step 2); it does USAGE-marker resolution, not
  index-only work.
- `NumProfile` (runtime) stays the runtime projection of `PicInfo`. Today `PicInfo` re-materializes it as an
  initializer STRING (`PicInfo.cs:301`). Keep that boundary (Binding must not depend on a runtime value type for its
  own logic) but generate it through the emitter's `RuntimeApi` façade (companion emitter design) so a
  `NumProfile` field rename breaks one file, not silently at generated-compile time.

### 2.8 Value-literal decoding (folds the apostrophe latent bug)

Route **every** VALUE / figurative-literal recognition through ONE `CobolLiteral.Decode` in a low layer both Binding
and CodeGen reference (companion `Common/` codec). Delete the hard-coded `"`-delimiter tests at `EmitCore.cs:162-168`,
`FieldEmitter.cs:329-332`, `CSharpEmitter.ReportWriter.cs:99` and the triplicated `DecodeCobolString`
(`StatementBinder.cs:1814`, `EmitCore.cs:133`, `DataBinder.cs:719`). Add conformance goldens for apostrophe-delimited
elementary / group / `ALL 'x'` / Report-Writer SOURCE VALUEs. This closes the confirmed silent-miscompile and enforces
"one string-literal boundary" as part of the data-model cleanup.

---

## 3. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Binding/Model/StorageForm.cs` | The one computed value-representation discriminator (§2.1); replaces the `(Pic, StoreAsImage, Class.Tier, IsDynamicTable)` scatter. |
| create | — | `Binding/Passes/StorageFormPass.cs` | Computes `DataItem.Storage` ONCE after all facts; owns the numeric-DISPLAY→image promotion (§2.5 step 10). |
| create | — | `Binding/Passes/UsageCollectionPass.cs` | New owner of `WholeGroupReferenced`, collected from the BOUND tree — removes the `ReferenceResolver` mid-resolve mutation and the emitter's write-back. |
| create | — | `Binding/Passes/IBindPass.cs` + `BindPipeline.cs` | Declared, asserted pass order with Requires/Produces (§2.5); kills implicit ordering. |
| create | — | `Binding/Model/RecordLayout.cs` | Single physical offset/width authority (§2.6). |
| create | — | `Binding/PictureAnalyzer.cs` | The extracted 230-line PICTURE scanner; makes `PicInfo` a pure value record (§2.7). |
| create | — | `Binding/Model/StrongTypeModel.cs` | Homes `SameStrongType`/`TypeAnchor`/`RelativeMemberPath` moved off `DataItem`. |
| create | — | `Common/CobolLiteral.cs` | One literal decoder; fixes the apostrophe-VALUE silent bug (§2.8). |
| delete | `DataItem.StoreAsImage` setter (`DataItem.cs:169`) | — | Replaced by `Storage is CharImage` (§2.1); removes the late-mutated cross-layer flag. |
| delete | `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) | — | Cross-layer write-back eliminated; logic moves into StorageFormPass. |
| move | `OdoGroupPlace` (`OdoModel.cs:89`) | `Binding/Place.cs` | Consolidate the Place hierarchy under one file (§2.2). |
| create | — | `Binding/PlaceDecorator` base (in `Place.cs`) | Common base for wrapping places (§2.2). |
| move | `SameStrongType`/`TypeAnchor`/`StrongRoot` logic (`DataItem.cs:74-133`) | `StrongTypeModel` | Strong-typing is not core shape (§2.4). |
| refactor | `DataItem.ImageWidth`/`IsCharacterImage`/`IsImageCapable` recursion (`DataItem.cs:245-300`) | cached init-only fields filled by StorageFormPass; width via `RecordLayout` | Removes ~119 O(subtree) re-walks; single owner (§2.4/§2.6). |
| merge | `OdoModel.PhysicalWidth` + `Sort`/`KeyedIo` geometry | `RecordLayout` | One layout authority; deletes 4 divergent copies (§2.6). |
| refactor | `RedefinesClass.Tier`/`.Width`/`ClassOffset` (`set`) | init-only, written once by `RedefinesClassifier` | Removes set-then-overwrite temporal state (§2.3). |
| refactor | ~10 inline Tier-C guards (`DataBinder.cs:1606,1631,1642`, `CSharpEmitter.cs:565…`) | one `RedefinesClassifier.RejectTierC` + `TierCWindow` backstop | Single-source the Tier-C rejection (§2.3). |
| split | `PicInfo.Analyze` (`PicInfo.cs:333-562`) | `PictureAnalyzer.Analyze` | PicInfo becomes a pure value record (§2.7). |
| delete | `PicInfo` skeleton scaffolding (`:175,216,691-705`) | `PicAnalysis` result discriminant | Removes dead sentinels/branches (§2.7). |
| rename | `DataBinder.ResolveIndexItems` | folded into `UsageInheritancePass` | Name matched actual scope (USAGE markers, not index-only) (§2.5/§2.7). |
| delete | `DecodeCobolString` × 3 + hard-coded `"` delimiter tests | `CobolLiteral.Decode` | One decoder; fixes apostrophe VALUE bug (§2.8). |
| refactor | `DataBinder` 9+ public mutable collections (`DataBinder.cs:26-77`) | read-only `BindModel` result object with explicit mutation methods | Encapsulate the blackboard so passes own their writes (§2.4/§2.5). |

---

## 4. Migration — keeping the battery green throughout

The battery (2028 greenfield conformance + 213 unit + legacy guard NIST 353 MATCH) must stay green **every phase**.
Strategy: introduce the new SSOT **in parallel**, prove byte-equivalence, then flip readers and delete the old fact.

**Phase D0 — Parallel `StorageForm` (no behavior change).**
Add `StorageForm` + `StorageFormPass`, computing `Storage` for every item. Do NOT delete `StoreAsImage` yet — instead
derive it read-only: `bool StoreAsImage => Storage is StorageForm.CharImage { Category: PicCategory.Numeric }`. Add a
unit test asserting, over the whole conformance corpus, that the new pass's per-leaf verdict equals the old
`MarkStoreAsImage` + PIC-derived computation for `ElementType`/`ImageWidth`/`IsCharacterImage`. Battery unchanged.

**Phase D1 — Own `WholeGroupReferenced`.**
Add `UsageCollectionPass` computing `WholeGroupReferenced` from the bound tree. Assert it equals the set the
`ReferenceResolver` mutation produced. Then delete the `ReferenceResolver.cs:280,303` writes and
`CSharpEmitter.MarkStoreAsImage`, having `StorageFormPass` consume the pass's set. Green gate: full battery + legacy
guard (this touches the whole-group path — NC247A/ODO and group-MOVE goldens are the sentinels).

**Phase D2 — Flip readers to `Storage`.**
Migrate the ~119 `StoreAsImage`/`IsCharacterImage`/`ImageWidth`/`FieldType` sites to `Storage`/`RecordLayout`,
file-by-file (FieldEmitter → NumericRenderer → OperandText → Place construction → Sort/Keyed geometry). Each file is
its own commit + battery run. The runtime overload bridge (`Occ`/`StoreDisplay`/`FormatDisplay` triplets) stays
during this phase so emitted text keeps compiling; it becomes redundant-but-harmless once every emit path chooses the
expression from `Storage`.

**Phase D3 — Delete `StoreAsImage` + cache image facts.**
Remove the derived `StoreAsImage`, convert `IsCharacterImage`/`IsImageCapable`/`ImageWidth` to init-only cached
fields filled by StorageFormPass, route width through `RecordLayout`. Delete the 4 duplicate geometry copies.

**Phase D4 — Structural cleanups (independent, each green-gated).**
Slim `DataItem` (immutable core + init-only facts); extract `PictureAnalyzer`; move `StrongTypeModel`; init-only
`RedefinesClass`; single-source Tier-C; `CobolLiteral.Decode` + apostrophe goldens; explicit `BindPipeline` with
Requires/Produces asserts.

**Phase D5 — Retire the overload bridge (optional).**
Once every emit path selects its expression from `Storage`, collapse the `Occ`/`StoreDisplay`/`FormatDisplay`
overload sets to the single form actually reachable per site (or keep them as a documented runtime convenience — see
open Q2). No behavior change; a pure simplification.

**Rollback posture:** D0/D1 are additive with equivalence asserts, so any diff surfaces in the parallel-run test
before a reader is flipped. Each later phase is a self-contained commit with the full battery as the gate.

---

## 5. Risks

1. **Whole-group promotion parity (highest).** `StorageFormPass` must reproduce `MarkStoreAsImage` *exactly*,
   including the fixed-OCCURS-under-whole-group recursion (`CSharpEmitter.cs:59-65`), the OO re-sync
   (`Oo.cs:697`), CompilerTemp clones (`Call.cs:118`), Linkage (`Linkage.cs:275`) and Reports (`Reports.cs:352`)
   sites. *Mitigation:* the D0 corpus-wide equivalence test is mandatory before any deletion; the sentinels are the
   group-MOVE and ODO goldens.
2. **Overload-bridge removal timing.** Removing the runtime overloads before every reader is flipped would break
   generated-C# compilation. *Mitigation:* the bridge is retired LAST (D5), gated on D2 completion.
3. **Tier-C quarantine scope.** Collapsing ~10 guards into one verdict risks accepting a case previously rejected
   piecemeal. *Mitigation:* keep each guard's ISO citation in the single `RejectTierC` reason table; a golden per
   previously-guarded shape.
4. **`RecordLayout` vs emitter drift during migration.** Until FieldEmitter consumes `RecordLayout`, two width
   computations coexist. *Mitigation:* D2 flips FieldEmitter first and asserts width equality across the corpus.
5. **`DataItem` immutability vs existing set-order.** Some facts are legitimately set late (e.g. `Uid` during tree
   build). *Mitigation:* freeze in tiers — core frozen after `BindEntries`, each pass-fact frozen after its pass;
   the pipeline's Requires/Produces asserts catch an out-of-order write.

---

## 6. Open questions for the owner

1. **Tier-C now or later?** Recommend single-source the *rejection* now (§2.3) and schedule the confined-`byte[]`
   codec as a separate increment. Confirm, or direct implementing the codec within this wave (larger, but closes a
   real REDEFINES type-punning gap).
2. **Retire the runtime overload bridge (D5)?** Keeping `Occ`/`StoreDisplay`/`FormatDisplay` polymorphic overloads is
   harmless and reduces churn; removing them makes storage-form selection fully explicit at the emitter. Preference?
3. **Structured `Place` segments (§2.2 item 4)?** Converting `Place.Path`/`OffsetExpr` from raw C# strings to
   structured `item + BoundExpr[]` segments is the clean fix for binder-assembles-emit-C# (`Initialize`/`TryExpandAll`),
   but it is a large, separable change touching every verb. Land it in this wave, or defer to the emitter-decomposition
   wave?
4. **`BindModel` boundary strictness.** Should `Bind()` return a fully read-only `BindModel` (passes mutate only via
   explicit methods), or is init-only-fields-on-DataItem + a thin accessor object sufficient? The former is cleaner
   but a wider refactor of the ~30 public collections.
5. **National / boolean widths under the model.** Today national is one UTF-16 char per position (D-N1) so
   `ImageWidth == Length` (never byte-doubled). `StorageForm.CharImage.Width` inherits that. Confirm national stays
   character-width in the unified model (a future 2-byte layout would be a *new* `StorageForm` case, not a mutation).
