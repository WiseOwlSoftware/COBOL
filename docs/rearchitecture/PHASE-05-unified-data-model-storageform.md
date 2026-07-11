# PHASE 05 — Unified Data Model: `StorageForm` discriminator, `Model/` folder, `RecordLayout`, pass scaffolding

- **Phase:** P5
- **Track:** rearchitecture
- **Risk:** HIGH
- **Depends on:** P0 (migration safety net — characterization harness + oracle bake-out + ref caching), P2 (`Cobol.Net.Editions` leaf assembly + diagnostic registry). Neither is *strictly* required to compile the code in this phase, but the equivalence-assert strategy below (Step 2, Step 6) is far safer with the P0 characterization harness in place. If P0 is NOT yet done, Step 1 below stands up a **phase-local** equivalence harness as a fallback (see Step 1.4).
- **SSOT design:** `docs/rearchitecture/DESIGN-data-model.md` (this phase EXECUTES that design's §2.1, §2.4–§2.8 and its migration §4 phases D0–D4). Read it first.
- **Companion designs (context, owned elsewhere):** `DESIGN-binder-bound-tree.md` (pass pipeline, StatementBinder split — P6/P7), `DESIGN-codegen-backend.md` (Place structural segments, emitter split — P7), `DESIGN-module-topology.md`.

> ## STATUS: RESUMED @ Exec Step C (2026-07-11) — Step 6 GATE ✅ GREEN; executing Steps 7–14
> **Step 6 (the prove-then-delete GATE) is GREEN** — recorded at the PHASE-06 close state: conformance **3159** ·
> unit **281** (incl. `StorageFormEquivalenceTests` identities #1–#5 corpus-wide: Storage↔StoreAsImage,
> IsCharacterImage, ImageWidth, ElementType, RecordLayout.PhysicalWidth↔OdoModel.PhysicalWidth) · characterization
> **32 byte-exact** · FULL legacy guard NIST **353 MATCH** · CI green in BOTH configurations. Exit criterion #3
> stands proven; deletions may begin.
> **P6 RECONCILIATION (what Exec Step B already did to this phase's Step-7 surface):** `MarkStoreAsImage`, the
> CompilerTemp re-sync, and the OO override harmonize no longer live in CodeGen — they are the sub-steps of
> `StorageFormPass.Run` (the GROUP-TAIL manifest pass, P6 Step 3/5), and the harmonize now ALSO covers
> interface-IMPLEMENTS pairs (P6 phase-review fix, DEVLOG 775). They still WRITE the `StoreAsImage` FLAG (D0);
> this phase's remaining job is to make `Storage` computed from COLLECTED FACTS instead of the flag, flip the
> readers, and delete the flag. The `ReferenceResolver` mid-resolve writes are already deleted (Step 5) and
> `WholeGroupReferenced` is `UsageCollectionPass`-owned.
>
> ## (prior) STATUS: PAUSED @ Step 5 DONE — Steps 6–14 resequenced to Exec Step C (AFTER the visitor + P6)
> 🔀 **RESEQUENCED (2026-07-11, owner-directed; `COBOLNET_REARCHITECTURE_PLAN.md §4.1`, [[project_path_a_leverage_tooling]]):**
> Steps 1–5 are DONE; the REMAINING Steps 6–14 (delete `MarkStoreAsImage` + write-back; `StoreAsImage`→`Storage`
> projection; flip readers; delete width copies; `Model/` move; apostrophe golden; close) are PAUSED and run at **Exec
> Step C**, AFTER **Exec Step A** (the source-generated exhaustive visitor, PHASE-07 Step 6 pulled forward) and **Exec
> Step B** (P6 `SymbolTable`) — so the reader-flips leverage the ONE shared visitor instead of hand-rolling. PHASE-05
> OWNS the `MarkStoreAsImage` deletion (P7 Step 8 merely consumes `Storage`). Below is the Step-5 close state.
> Step 5 was REDESIGNED per the owner directive (never workaround — the reflective-walk + keep-mid-resolve attempts were
> rejected as shortcuts; DEVLOG 752/753). `UsageCollectionPass` (an explicit TYPED bound-tree walk, NO reflection) now
> OWNS `WholeGroupReferenced`, collecting the group `Place.Item` at every true whole-image operand position; the
> over-inclusive `ReferenceResolver` mid-resolve mutation is DELETED. **Key finding (`wf_fe251cf8-9d6`, compile-test):**
> the bound tree is the CORRECT oracle; legacy over-collected every RESOLVED group (CORR operands, SEARCH/qualifier
> groups, IX keys, INITIALIZE/ACCEPT — none whole-image). Verified by OUTPUT across the full battery, which CAUGHT a
> real visitor gap (a `DynTablePlace` OCCURS-DYNAMIC element over-collected → `dyn_nested_group_move` regressed; fixed
> by skipping `DynTablePlace` — a dynamic element is a typed `CobolDynTable<T>` handled by the table codec) and one
> output-neutral emit change (`char_initialize` `WS_N` `string`→`long`, snapshot re-baselined with runtime proof).
> Battery: conformance **3157** · unit **258** · characterization **32** · FULL legacy guard NIST 353 MATCH. RESUME: Step
> 6 (the prove-then-delete GATE — battery green is the green light) → Step 7 (delete `MarkStoreAsImage` + the
> emitter→binder write-back; `StoreAsImage` → read-only projection of `Storage`).
>
> ## (prior) STATUS @ Step 4 DONE
> Steps 0–4 landed 2026-07-10 (DEVLOG 747, 749, 750, 751). Step 4 (DESIGN §2.6): `Binding/Model/RecordLayout.cs` — the
> ONE width authority — `ImageWidth` (reads the Step-2 StorageForm; `StorageFormPass.ImageWidthOf` consolidated onto it)
> + `PhysicalWidth` (tier-aware, mirrors `OdoModel.PhysicalWidth`). `StorageFormPass.Verify` gained identity **#5**
> (`RecordLayout.PhysicalWidth == OdoModel.PhysicalWidth` per group, corpus-wide) — the §5.4 drift guard. ADDITIVE +
> test-path only (zero production wiring; readers flip Step 8). **Scoping refinement (deviation from Step 4.1, per rule
> 4):** the OFFSET copies (Sort `SortOffsetInRecord`/`SortPlainOffset` + Keyed `KeyedAreaOffset`/`KeyedKeyIndex`) are
> DEFERRED to Step 8 — the two legacy algorithms use different width bases for the increment (Sort=`PhysicalWidth`
> class-max; Keyed=`ImageWidth`), so `RecordLayout.OffsetOf` cannot be proven byte-equal to BOTH as a pure port; the
> unification onto the codec-correct `PhysicalWidth` basis folds in at Step 8 under the Sort/Keyed goldens. Battery at
> head: greenfield conformance **3157** · unit **258** · characterization **32** byte-exact · FULL legacy guard NIST
> **353 MATCH**. The executing session MUST update this line + the §7 Step ledger on each commit boundary and set `DONE`
> at phase end.
>
> ## (prior) Step 3 DONE — DESIGN §2.5: the DECLARED bind pass pipeline —
> `Binding/Passes/IBindPass.cs` (the `PassPhase` enum + `IBindPass` interface + `BindPass` record) +
> `Binding/Passes/BindPipeline.cs` (`Build(program)` = the ONE ordered pass list [16 resolve passes in the EXACT
> pre-change order + 3 middle-end tail markers for DAG completeness] + `ValidateDag` monotone-chain startup assert).
> `BindResolve` now drives the pipeline (runs the `Produces <= FilesResolved` prefix; the tail is emitter-driven);
> the inline FILE whole-group loop was extracted to `MarkFileRecordImageLeaves()`; 12 resolve methods widened
> `private → internal`. NO-OP wrapper: ZERO reorder, ZERO behavior change (characterization **32 byte-exact**). Exit
> criterion **#5 (pass DAG asserted at startup) SATISFIED.** Battery at head: greenfield conformance **3157** · unit
> **258** (+4 BindPipelineTests) · characterization **32** byte-exact · FULL legacy guard NIST **353 MATCH**. The
> executing session MUST update this line + the §7 Step ledger on each commit boundary and set `DONE` at phase end.

---

## 1. Goal (one paragraph)

Replace the late-mutated, cross-layer `DataItem.StoreAsImage` boolean — written from **9 sites across three layers** (binder data pass, binder procedure pass, AND the CodeGen emitter writing back into the Binding data model) — with **one closed, computed `StorageForm` discriminator** decided **exactly once** by a `StorageFormPass` that runs after all facts (including PROCEDURE-DIVISION whole-group use) are collected, and stored **init-only** on `DataItem`. Extract the pure data model into a `Binding/Model/` folder, slim `DataItem`, extract `PictureAnalyzer` + `StrongTypeModel`, delete the `PicInfo` skeleton scaffolding, make `RedefinesClass.Tier/Width/ClassOffset` init-only, single-source the Tier-C rejection, introduce **one `RecordLayout`** physical-width authority, stand up the **`IBindPass` pipeline scaffolding** (as no-op wrappers with `Requires`/`Produces` + a startup DAG assert — zero behavior change), and land **`Common/CobolLiteral.Decode`** (recognizing BOTH ISO string delimiters) to fix the confirmed apostrophe-delimited-VALUE silent miscompile while deleting the three `DecodeCobolString` twins + the hard-coded double-quote guards. The battery stays green at **every** commit boundary.

## Exit criteria (copied from the phase brief — all must hold at phase end)

1. `StorageForm` is computed **exactly once** (in `StorageFormPass`); every reader consumes it, none re-infers.
2. `CSharpEmitter.MarkStoreAsImage` and the emitter→binder write-back (`CSharpEmitter.Call.cs` re-sync, `CSharpEmitter.Oo.cs` harmonize re-sync) are **deleted**.
3. The corpus-wide `StorageForm` cross-check is **proven equal** to the legacy `StoreAsImage`/image-fact computation **before** any deletion.
4. `RecordLayout` is the **single** physical offset/width authority (the 4 divergent copies deleted).
5. The pass DAG is **asserted at startup** (`BindPipeline.ValidateDag`).
6. An **apostrophe-delimited VALUE** conformance golden is added and green.
7. Full battery green + characterization snapshots neutral (or reviewed-re-baselined with a gate-1 proof).

## Scope

**IN:** `StorageForm` closed discriminator; `UsageCollectionPass`; `StorageFormPass`; the prove-then-delete corpus cross-check; delete `MarkStoreAsImage` + the CompilerTemp re-sync + the FILE whole-group loop's mutation + the OO harmonize re-sync; convert `IsCharacterImage`/`IsImageCapable`/`ImageWidth` from recursive props to cached init-only fields; one `RecordLayout`; move `DataItem`/`PicInfo`/`Place`/`RedefinesModel`/`OdoModel`/`FileModel`/`Condition88` into `Model/`; slim `DataItem`; extract `PictureAnalyzer` + `StrongTypeModel`; delete `PicInfo` skeleton scaffolding + sentinel singletons; `RedefinesClass.Tier/Width/ClassOffset` init-only; single-source Tier-C rejection (`RejectTierC` + `TierCWindow` backstop; the confined `byte[]` codec is **deferred to P11**); the `IBindPass` framework as no-op wrappers with `Requires`/`Produces` + `ValidateDag`; `Common/CobolLiteral.Decode` + delete the 3 `DecodeCobolString` twins + hard-coded `"`-delimiter guards.

**OUT (later phases):** the `SymbolTable` and read-only `BindModel` result object (P6); `BoundCompilation` immutability + the real Bind-phase extraction out of `CSharpEmitter.CallEmitRunUnit` (P6); the binder/emitter god-class split (P6/P7); the exhaustive visitor (P7); structured `Place.Path`/`OffsetExpr` segments (P7 — DESIGN §2.2 item 4, owner-gated Q3); retiring the runtime overload bridge (`Occ`/`StoreDisplay`/`FormatDisplay` triplets — DESIGN D5, keep harmless through this phase).

---

## 2. Rationale — the problems this phase fixes (grounded in code)

The AS-IS survey and `DESIGN-data-model.md` §1 identify these defects; this phase closes them:

1. **`StoreAsImage` is a late-mutated, cross-layer flag** (DESIGN §1.1). `DataItem.StoreAsImage` is a public `get; set;` (`DataItem.cs:169`) written from **9 sites in three layers**:
   - binder data pass: `DataBinder.cs:255` (FILE whole-group loop), `:1562`, `:1577` (REDEFINES Tier-B), `DataBinder.Linkage.cs:275`, `DataBinder.Reports.cs:352`, `DataBinder.Oo.cs:373` (compiler-temp clone init)
   - binder procedure pass: `StatementBinder.MoveFigurative.cs:123`, `:262`
   - **the emitter writes the binder's data model**: `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`), driven from `WholeGroupReferenced` which `ReferenceResolver` itself mutates mid-resolve (`ReferenceResolver.cs:280,303`), plus the CompilerTemp re-sync (`CSharpEmitter.Call.cs:111-120`) and the OO override-harmonize re-sync (`CSharpEmitter.Oo.cs:694-697`).
   Consequence: **emit correctness is order-dependent on a bind-time side effect**, and a representation bug is invisible until the generated C# hits Roslyn (it is reconciled by C# overload resolution — the `Occ`/`StoreDisplay`/`FormatDisplay` triplets).
2. **Recompute-on-read** (DESIGN §1.1.3). `IsCharacterImage`/`IsImageCapable`/`ImageWidth`/`StrongRoot` are recursive computed properties (`DataItem.cs:74-83,245-300`) re-walked at ~119 sites, each an O(subtree) walk silently sensitive to the last `StoreAsImage` flip.
3. **Implicit pass ordering** (DESIGN §1.2). `DataBinder.BindResolve` runs ~15 passes ordered only by call sequence + comments (`DataBinder.cs:210-232`); the *real* middle-end orchestration (incl. `MarkStoreAsImage`) is hidden inside `CSharpEmitter.CallEmitRunUnit`. No pass declares Requires/Produces.
4. **Duplicated physical-width geometry** (DESIGN §1.2). `DataItem.ImageWidth` (`:283`), `OdoModel.PhysicalWidth`, `Sort.cs` and `KeyedIo.cs` geometry each compute record character offsets/widths independently and *must agree* — a drift hazard.
5. **`RedefinesClass.Tier`/`.Width`/`ClassOffset`** are `set` and set-then-overwritten across passes (`RedefinesModel.cs:70,74`; `DataItem.cs:213`) — mutable temporal state.
6. **Tier-C is declared but unimplemented** — `ByteCanonical` (`RedefinesModel.cs:49`) with ~10 scattered deferral guards.
7. **`PicInfo` carries dead skeleton scaffolding** and a 230-line `Analyze` scanner making it not a pure value record (DESIGN §2.7).
8. **Confirmed latent bug:** apostrophe-delimited VALUE literals are silently miscompiled. The three `DecodeCobolString` twins (`StatementBinder.cs:1815`, `EmitCore.cs:133`, `DataBinder.cs:720` [named `DecodeString`]) DO handle both delimiters — but the **guard tests that gate whether to call them** hard-code `"`: `EmitCore.AllLiteralText:167` (`rest[0] == '"'`), `FieldEmitter.cs:331` (`raw[0] == '"'`), `StatementBinder.Initialize.cs:283,288,290,292` (`rest[0]=='"'` / `t[0]=='"'`). A `VALUE 'x'` fails the guard and falls through to `BoundNumericLiteral`/raw text — a silent miscompile (DESIGN §2.8).

---

## 3. Target end-state for this phase (the files/classes/signatures that exist when DONE)

New / moved / changed files under `src/Cobol.Net.Compiler/`:

```
Binding/Model/                         (NEW folder, namespace CobolNet.Binding.Model)
  StorageForm.cs        NEW  sealed abstract record + 9 cases (§2.1 of DESIGN)
  RecordLayout.cs       NEW  the ONE physical offset/width authority (§2.6)
  StrongTypeModel.cs    NEW  SameStrongType/TypeAnchor/StrongRoot/RelativeMemberPath moved off DataItem (§2.4)
  DataItem.cs           MOVED here from Binding/; slimmed; StoreAsImage DELETED; image facts = cached init-only fields
  PicInfo.cs            MOVED here from Binding/; pure value record; Analyze extracted; skeleton scaffolding deleted
  Place.cs              MOVED here from Binding/; OdoGroupPlace folded in; PlaceDecorator base added
  RedefinesModel.cs     MOVED here; RedefinesClass.Tier/Width/ClassOffset init-only; RejectTierC single-source
  OdoModel.cs           MOVED here; PhysicalWidth deleted (→ RecordLayout)
  FileModel.cs          MOVED here
  Condition88.cs        MOVED here
Binding/PictureAnalyzer.cs             NEW  the extracted 230-line PICTURE scanner: PicInfo Analyze(...)
Binding/Passes/                        (NEW folder, namespace CobolNet.Binding.Passes)
  IBindPass.cs          NEW  interface + PassPhase enum
  BindPipeline.cs       NEW  ordered pass list + ValidateDag() asserted at startup
  UsageCollectionPass.cs NEW owns WholeGroupReferenced (collected from the BOUND tree)
  StorageFormPass.cs    NEW  computes DataItem.Storage ONCE; owns the NativeInt→CharImage promotion
Common/CobolLiteral.cs                 NEW  Decode(...) + IsStringLiteral(...) recognizing BOTH ISO delimiters
```

Deleted:
- `DataItem.StoreAsImage` (the mutable flag) and its 9 write sites.
- `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`); the CompilerTemp re-sync (`CSharpEmitter.Call.cs:111-120`); the OO harmonize re-sync mutation (`CSharpEmitter.Oo.cs:694-697` — the *decision* moves into StorageFormPass, the re-sync loop is deleted).
- `ReferenceResolver.cs:280,303` `WholeGroupReferenced.Add` writes (moved to `UsageCollectionPass`).
- `DecodeCobolString`×3 twins + `DecodeString` + the hard-coded `"` guards.
- `OdoModel.PhysicalWidth` + `Sort`/`KeyedIo` geometry copies.
- `PicInfo` skeleton scaffolding (`IsUnimplementedSkeleton`, `SkeletonReached`, the 3 `ReferenceEquals` sentinel singletons).

`DataItem` public surface after this phase (key changes):
```csharp
public StorageForm Storage { get; init; }          // NEW — set once by StorageFormPass
public bool IsCharacterImage { get; init; }         // was recursive prop → cached init-only field
public bool IsImageCapable  { get; init; }          // was recursive prop → cached init-only field
public int  ImageWidth      { get; init; }          // was recursive prop → cached init-only field (via RecordLayout)
// StoreAsImage : DELETED
// ElementType/FieldType/ClrType : now project off Storage (+ Occurs), not (Pic, StoreAsImage)
// SameStrongType/TypeAnchor/StrongRoot/RelativeMemberPath : MOVED to StrongTypeModel
```

New conformance golden: `ApostropheValueDifferentialTests.cs` (or equivalent under `tests/Cobol.Net.Tests.Conformance/`) covering apostrophe-delimited elementary VALUE, group VALUE, `ALL 'x'`, and a Report-Writer SOURCE/VALUE case.

---

## 4. STEP-BY-STEP

> **Discipline for every step:** (a) make the change; (b) build `dotnet build E:/CobolSharp/CobolSharp.sln -c Debug`; (c) run the greenfield battery (`dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance E:/CobolSharp/tests/Cobol.Net.Tests.Unit -c Debug`); (d) at a COMMIT BOUNDARY also run the FULL LEGACY GUARD (`bash scripts/guard.sh` — NIST 353 MATCH) and the characterization snapshots from P0. Only commit when all are green. Follow the repo DEVLOG discipline: add a DEVLOG entry (newest-first) per commit.
>
> **Ordering rationale:** parallel-SSOT-then-flip. We introduce `StorageForm` alongside `StoreAsImage` and PROVE equality corpus-wide (Steps 1–6) BEFORE flipping any reader or deleting anything (Steps 7–12). The low-risk structural cleanups (`CobolLiteral`, `RecordLayout` scaffolding, `Model/` move, pass scaffolding) are interleaved where they de-risk later steps.

### Step 0 — Baseline capture (no code change) — COMMIT BOUNDARY (docs only)
- **Do:** Confirm the battery is green on a clean tree: run the greenfield battery + `bash scripts/guard.sh` + the P0 characterization snapshot test. Record the counts in this doc's STATUS ledger (§7). If P0's characterization harness exists, run it once to (re)seed `tests/characterization/Snapshots/` from the current emitter and commit that seed (if P0 already committed it, skip).
- **Why:** every later "battery green / snapshots neutral" claim is relative to this baseline.
- **Verify:** all green; note the exact test counts (expected ~2028 conformance + 213 unit + NIST 353 MATCH).
- **Commit:** `docs(rearch): P5 baseline captured — battery green, snapshots seeded (DEVLOG NNN)`

### Step 1 — Land `Common/CobolLiteral.cs` (the one literal decoder) — COMMIT BOUNDARY
This is first because it is self-contained, fixes a real bug, and removes a triplicated helper that would otherwise complicate the `Model/` move.

- **1.1 Create** `src/Cobol.Net.Compiler/Common/CobolLiteral.cs`, namespace `CobolNet.Common`:
  ```csharp
  namespace CobolNet.Common;
  /// The ONE COBOL string-literal codec (ISO/IEC 1989:2023 §8.3.1.2 — quotation-mark and apostrophe forms are
  /// equal-standing; a doubled OPENING delimiter is one embedded delimiter). N"…"/B"…"/N'…'/B'…' prefixes
  /// (§8.3.3.4/.5) are part of the token. Replaces the 3 DecodeCobolString twins + the hard-coded '"' guards.
  public static class CobolLiteral
  {
      /// True when raw is a quoted string literal in EITHER delimiter, optionally N/B-prefixed.
      public static bool IsStringLiteral(string raw) { /* letter-prefix strip, then raw[0] is '"' or '\'' && raw[^1]==raw[0] && len>=2 */ }
      /// The decoded character value of a STRINGLIT (or N/B-prefixed literal); returns raw unchanged if not a literal.
      public static string Decode(string raw) { /* body of the existing twins, verbatim */ }
      /// If raw is the figurative `ALL "literal"` / `ALL 'literal'` form, the decoded literal; else null.
      public static string? AllLiteralText(string raw) { /* EmitCore.AllLiteralText but delimiter-agnostic via IsStringLiteral */ }
  }
  ```
  Port the decoder body verbatim from `EmitCore.DecodeCobolString` (`EmitCore.cs:133-142`) — it already handles both delimiters. The FIX is that `IsStringLiteral`/`AllLiteralText` recognize `'` as well as `"`.
- **1.2 Add a focused unit test** `tests/Cobol.Net.Tests.Unit/CobolLiteralTests.cs`: `Decode("'AB''C'")=="AB'C"`, `Decode("\"AB\"\"C\"")=="AB\"C"`, `IsStringLiteral("'x'")==true`, `IsStringLiteral("N'x'")==true`, `AllLiteralText("ALL 'x'")=="x"`.
- **1.3 Replace all call sites** to route through `CobolLiteral`:
  - Delete `StatementBinder.DecodeCobolString` (`StatementBinder.cs:1815`), `EmitCore.DecodeCobolString` (`EmitCore.cs:133`), `DataBinder.DecodeString` (`DataBinder.cs:720`); repoint every caller (grep list above: `StatementBinder.*` ~10 sites, `EmitCore`, `FieldEmitter`, `CSharpEmitter.*`, `DataBinder`, `Initialize`, `ReportWriter`, `Boolean`, `Evaluate`, `Call`, `Inspect`, `Oo`) to `CobolLiteral.Decode`.
  - Replace the hard-coded `"`-only guards with `CobolLiteral.IsStringLiteral(...)`: `EmitCore.AllLiteralText:167`, `FieldEmitter.cs:331`, `StatementBinder.Initialize.cs:283,288,290,292`. Keep the N/B category tagging in `Initialize.cs:288-291` (test the prefix letter, then `IsStringLiteral`).
  - `EmitCore.AllLiteralText` becomes a thin forward to `CobolLiteral.AllLiteralText` (or delete it and repoint callers).
- **1.4 (Fallback if P0 not done) Stand up a phase-local equivalence harness** `tests/Cobol.Net.Tests.Conformance/StorageFormEquivalenceTests.cs` (empty scaffold now; filled in Step 2). This test compiles every conformance-corpus program with the compiler and compares per-leaf verdicts — it is the safety net Steps 2/6 depend on. If P0's characterization harness already provides emitted-C# snapshots over the corpus, prefer that and skip this.
- **Why:** one decoder boundary; fixes the apostrophe silent-miscompile by construction (DESIGN §2.8).
- **Verify:** greenfield battery green; the new `CobolLiteralTests` pass. NOTE: the apostrophe *golden* is added in Step 13 (after StorageForm lands, so the golden exercises the full VALUE-init path); Step 1 fixes the decode/guard plumbing.
- **Commit:** `refactor(cobolnet): P5.1 one CobolLiteral.Decode (both ISO delimiters) — delete 3 DecodeCobolString twins + hard-coded quote guards (DEVLOG NNN)`

### Step 2 — Introduce `StorageForm` as a PARALLEL derived value (no behavior change) — COMMIT BOUNDARY
This is DESIGN Phase **D0**. Add the discriminator and compute it, but do NOT delete `StoreAsImage` — instead derive `StoreAsImage` FROM `Storage`, and add the corpus equivalence assert.

- **2.1 Create** `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs` per DESIGN §2.1 (namespace `CobolNet.Binding.Model`): the sealed abstract record with `IsCharacterImage`/`ImageWidth` abstract members and the 9 cases `NativeInt`, `NativeFloat`, `CharImage(int Width, PicCategory Category)`, `TierBWindow(RedefinesClass, int Offset, int Width)`, `TierCWindow(RedefinesClass, int Offset, int Length, Usage)`, `DynamicTable(StorageForm Element)`, `ObjectRef(string? ClassName)`, `PointerRef`, `IndexCell`.
  - Keep it a pure value classification: no C# strings, no emit logic.
- **2.2 Create** `src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs` with a static entry `Compute(DataBinder data)` that walks every root (WORKING-STORAGE, FILE, LINKAGE, LOCAL-STORAGE, method scopes, compiler temps, OO class/factory data) and assigns each item a `Storage`. Base form from `(Pic, Usage, tier, dynamic)`; then the whole-group promotion `NativeInt → CharImage` for numeric-DISPLAY leaves under a `WholeGroupReferenced` group. **This pass MUST reproduce, exactly**, the union of:
  - `MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) — numeric-DISPLAY leaves under whole-group, recursing through fixed-OCCURS subordinates;
  - the FILE whole-group loop (`DataBinder.cs:238-255`);
  - the REDEFINES Tier-B flips (`DataBinder.cs:1562,1577`);
  - Linkage (`DataBinder.Linkage.cs:275`), Reports (`DataBinder.Reports.cs:352`), MoveFigurative (`StatementBinder.MoveFigurative.cs:123,262`), OO harmonize (`CSharpEmitter.Oo.cs:694-697`), and CompilerTemp clone init (`DataBinder.Oo.cs:373`).
  - For this step, ADD a **temporary** `Storage`-derived shim on `DataItem`: `public StorageForm Storage { get; set; }` (mutable for D0 only — becomes init-only in Step 10), and keep `StoreAsImage` but redefine it read-only: `public bool StoreAsImage => Storage is StorageForm.CharImage { Category: PicCategory.Numeric };`. Remove the 9 `StoreAsImage = true` *writes* by having those sites instead be no-ops OR keep them writing a scratch field that Step 2's cross-check compares against. **Simplest safe approach:** keep the existing `StoreAsImage` boolean field UNCHANGED and ADD `Storage` computed in parallel; the equivalence test compares them. Do NOT redefine `StoreAsImage` until the cross-check is green (defer the read-only redefinition to Step 7).
- **2.3 Wire the pass** to run LAST in the middle-end order — call `StorageFormPass.Compute(data)` at the point in `CSharpEmitter.CallEmitRunUnit` where `MarkStoreAsImage` runs today (right after it), so `Storage` is computed over the same post-procedure-bind state. Do NOT delete `MarkStoreAsImage` yet.
- **2.4 Fill the equivalence test** (`StorageFormEquivalenceTests.cs` from Step 1.4, or extend the P0 characterization harness): over the WHOLE conformance corpus, for every leaf assert:
  - `(item.Storage is CharImage{Category:Numeric}) == item.StoreAsImage`
  - `item.Storage.IsCharacterImage == item.IsCharacterImage` (old recursive prop)
  - `StorageFormPass`-derived `ImageWidth == item.ImageWidth` (old recursive prop)
  - `StorageForm`-derived `ElementType == item.ElementType`
  Drive this by exposing a compiler hook (a `CheckOnly`/bind-only entry that returns the bound `DataBinder` for each unit) or by adding an internal test-visible callback in `CallEmitRunUnit`. This is the **prove** half of prove-then-delete.
- **Why:** DESIGN §4 D0 — parallel SSOT with a corpus equivalence gate; nothing is deleted until proven equal.
- **Verify:** greenfield battery green; the equivalence test GREEN across the whole corpus (this is EXIT CRITERION #3). If any leaf diverges, `StorageFormPass` is wrong — fix it here, before proceeding. Sentinels: whole-group MOVE goldens (`WholeGroupDifferentialTests`, `GroupSenderMoveDifferentialTests`, `GroupNumericLeafDifferentialTests`, `MixedUsageRecordImageDifferentialTests`), ODO (`OdoDifferentialTests`), SORT (`SortMergeDifferentialTests`).
- **Commit:** `feat(cobolnet): P5.2 StorageForm parallel SSOT + StorageFormPass + corpus equivalence assert (D0, no behavior change) (DEVLOG NNN)`

### Step 3 — `IBindPass` scaffolding + `BindPipeline.ValidateDag` (no-op wrappers, zero behavior change) — COMMIT BOUNDARY
DESIGN §2.5. Make the pass order explicit and asserted, WITHOUT reordering anything.

- **3.1 Create** `Binding/Passes/IBindPass.cs`:
  ```csharp
  namespace CobolNet.Binding.Passes;
  public enum PassPhase { None, TypesExpanded, UsageResolved, SignResolved, RedefinesClassified,
      StrongTypeChecked, OccursResolved, FilesResolved, ProcedureBound, UsageCollected, StorageComputed }
  public interface IBindPass { string Name { get; } PassPhase Requires { get; } PassPhase Produces { get; } void Run(DataBinder data); }
  ```
- **3.2 Create** `Binding/Passes/BindPipeline.cs` holding the ordered list of the existing `BindResolve` passes (`DataBinder.cs:218-232`) wrapped as `IBindPass` records that simply call the existing private methods (make them `internal` as needed). Add `ValidateDag()` that asserts, at startup, each pass's `Requires` is `<=` the max `Produces` of all prior passes (a monotone `PassPhase` chain). Call `BindPipeline.ValidateDag()` once at the top of `BindResolve` (or in a static ctor / first-use guard) so a mis-ordering throws immediately.
- **3.3 Route** `BindResolve` to drive the pipeline: replace the comment-ordered method calls with `foreach (var p in BindPipeline.Passes) p.Run(this);` PLUS keep `ExpandTypes()` first (it already runs before the rest). The two middle-end passes hidden in `CallEmitRunUnit` (`UsageCollection` = the `WholeGroupReferenced` set, `StorageForm`) are represented as pipeline entries too but continue to be invoked from `CallEmitRunUnit` for now (they need the bound tree). Record them in the DAG with `Requires = ProcedureBound`.
- **Why:** kills "implicit pass-ordering" structurally (EXIT CRITERION #5) with zero behavior change — the ORDER is identical, only now declared + asserted.
- **Verify:** greenfield battery green; add a unit test `BindPipelineTests` asserting `ValidateDag()` does not throw for the canonical order and DOES throw for a deliberately-swapped order.
- **Commit:** `feat(cobolnet): P5.3 IBindPass pipeline scaffolding + ValidateDag startup assert (no reorder) (DEVLOG NNN)`

### Step 4 — Introduce `RecordLayout` as a PARALLEL width authority (no behavior change) — COMMIT BOUNDARY
DESIGN §2.6. Add the service; assert it equals every existing width computation before deleting any copy.

- **4.1 Create** `Binding/Model/RecordLayout.cs`: `ImageWidth(DataItem)`, `PhysicalWidth(DataItem group)` (tier-aware), `OffsetOf(DataItem leaf)`, `KeyIndexByPosition(...)`. Port the geometry from `DataItem.ImageWidth`/`ElementaryImageWidth` (`DataItem.cs:283-300`) as the canonical implementation. Read `StorageForm.ImageWidth` once StorageForm is trusted (it is, after Step 2's gate).
- **4.2 Add an equivalence assert** (extend the Step 2 test): over the corpus, for every group/leaf assert `RecordLayout.ImageWidth(item) == item.ImageWidth`, `RecordLayout.PhysicalWidth(g) == OdoModel.PhysicalWidth(g)`, and the Sort/Keyed key-geometry equals `RecordLayout.KeyIndexByPosition`/`OffsetOf`. Do NOT delete the copies yet.
- **Why:** single width authority (EXIT CRITERION #4), proven-equal-before-delete (drift risk mitigation, DESIGN §5.4).
- **Verify:** greenfield battery green; width-equivalence assert green corpus-wide.
- **Commit:** `feat(cobolnet): P5.4 RecordLayout parallel width authority + corpus width equivalence assert (DEVLOG NNN)`

### Step 5 — Own `WholeGroupReferenced` in `UsageCollectionPass` (parallel, proven equal) — COMMIT BOUNDARY
DESIGN Phase **D1**, prove half. Move the fact's OWNERSHIP without yet deleting the old writers.

- **5.1 Create** `Binding/Passes/UsageCollectionPass.cs`: `Collect(DataBinder data, BoundProgram[] units, OoClass[] classes)` walks the BOUND tree and records every group used as a whole operand into a NEW set `data.WholeGroupReferencedV2` (a temporary parallel set). It must reproduce the union of the current sources: `ReferenceResolver.cs:280,303` (group refs during resolve), the FILE-record adds (`DataBinder.cs:242`), the OO USING/RETURNING adds (`DataBinder.Oo.cs:281,283`), and the property-group add (`CSharpEmitter.Call.cs:402`).
- **5.2 Assert equality** (extend the Step 2 test): `data.WholeGroupReferencedV2` set-equals `data.WholeGroupReferenced` corpus-wide. Run `UsageCollectionPass.Collect` right before `StorageFormPass.Compute` in `CallEmitRunUnit`.
- **Why:** new single owner of the whole-group fact, collected from the bound tree instead of mid-resolve mutation (DESIGN §2.5 step 9).
- **Verify:** greenfield battery green; set-equality assert green corpus-wide. Sentinels: `WholeGroupDifferentialTests`, `ST103A`/`NC247A` NIST goldens (the FILE-record-child-only case), OO USING/RETURNING (`OoSpineTests`).
- **Commit:** `feat(cobolnet): P5.5 UsageCollectionPass owns WholeGroupReferenced (parallel, proven set-equal) (DEVLOG NNN)`

### Step 6 — GATE: full prove-then-delete checkpoint — COMMIT BOUNDARY (verification only)
- **Do:** Run the ENTIRE battery + full legacy guard + characterization snapshots with all three equivalence asserts (Storage, RecordLayout width, WholeGroupReferenced) ON. This is the last green light before deletions begin.
- **Why:** DESIGN §4 D0/D1 rollback posture — any divergence surfaces here, before a single reader is flipped or a writer deleted.
- **Verify:** EXIT CRITERION #3 satisfied (Storage cross-check equal corpus-wide); width + whole-group asserts equal; NIST 353 MATCH; snapshots neutral.
- **Commit:** `test(cobolnet): P5.6 prove-then-delete gate green — Storage/RecordLayout/WholeGroup equal corpus-wide (DEVLOG NNN)`

### Step 7 — Flip `WholeGroupReferenced` to the new owner; delete `MarkStoreAsImage` + mid-resolve writes — COMMIT BOUNDARY
DESIGN Phase **D1**, delete half.

- **7.1** Rename `WholeGroupReferencedV2` → `WholeGroupReferenced` (delete the old set); `StorageFormPass` consumes the `UsageCollectionPass` set.
- **7.2 Delete** the mid-resolve writes `ReferenceResolver.cs:280,303`; delete `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50-68`) and its 3 call sites (`CSharpEmitter.Call.cs:119-120`); delete the FILE whole-group *mutation* loop's `child.StoreAsImage = true` (keep the `WholeGroupReferenced.Add(rec)` only if UsageCollectionPass doesn't already cover FILE records — it should, so delete the whole `DataBinder.cs:238-255` block and fold FILE-record whole-group into UsageCollectionPass); delete the CompilerTemp re-sync (`CSharpEmitter.Call.cs:111-120`) and the OO harmonize re-sync mutation (`CSharpEmitter.Oo.cs:694-697` — the override-crossing DECISION that a formal must be image-stored moves into StorageFormPass as a rule; the compute-then-repair loop is deleted). Delete the `StoreAsImage = true` writes at `DataBinder.cs:1562,1577`, `DataBinder.Linkage.cs:275`, `DataBinder.Reports.cs:352`, `StatementBinder.MoveFigurative.cs:123,262`, `DataBinder.Oo.cs:373` — each becomes a StorageFormPass rule (they already are, since Step 2 reproduced them; here we remove the now-redundant mutations).
- **7.3** Redefine `DataItem.StoreAsImage` as a read-only projection: `public bool StoreAsImage => Storage is StorageForm.CharImage { Category: PicCategory.Numeric };` (still present so the ~119 readers keep compiling — they are flipped in Step 8). EXIT CRITERION #2 is now satisfied (MarkStoreAsImage + the emitter→binder write-back are deleted).
- **Why:** the cross-layer write-back and mid-resolve mutation are eliminated; the fact is computed once.
- **Verify:** greenfield battery green; full legacy guard NIST 353 MATCH; snapshots neutral. The Storage equivalence assert now compares `Storage` against ITSELF-derived `StoreAsImage` — keep it as a regression guard (it should be trivially true; remove it in Step 12 when the readers are all on `Storage`).
- **Commit:** `refactor(cobolnet): P5.7 delete MarkStoreAsImage + cross-layer write-back; StoreAsImage now read-only projection of Storage (D1) (DEVLOG NNN)`

### Step 8 — Flip readers to `Storage` / `RecordLayout`, file-by-file — MULTIPLE COMMIT BOUNDARIES
DESIGN Phase **D2**. Each file is its own commit + full battery run. Suggested order (leaf → up), matching DESIGN §4 D2:

> **DEFERRED FROM STEP 4 (DEVLOG 751):** `RecordLayout.OffsetOf(root, leaf)` + `RecordLayout.KeyIndexByPosition(file, operand)`
> are CREATED HERE (not in Step 4). Reason: the two legacy offset copies use DIFFERENT width bases for the running
> increment — `SortPlainOffset` advances by `PhysicalWidth` (class-max, matching the emitted codec) while `KeyedAreaOffset`
> advances by `ImageWidth` (the redefined item's own width). They agree only where no key/sort-key follows a redefines
> whose redefiner is wider than its target, so `OffsetOf` cannot be proven byte-equal to BOTH as a pure port. Build
> `RecordLayout.OffsetOf` on the codec-correct `PhysicalWidth` basis, flip the Sort readers (8.6) to it FIRST (their
> goldens gate it), then flip the Keyed readers — and if the `KeyedAreaOffset` `ImageWidth` basis was a latent under-count
> for a wider-redefiner key layout, that is a real fix to make here (spec: ISO §12.4.5.12 GR4 / §14.9.40.3 SR6e — key
> positions are the same byte positions in every record description, i.e. the PHYSICAL layout).
> **⛔ MANDATED FIRST ACTION OF STEP 8 (audit DEVLOG 754):** add a FAILING-FIRST conformance golden for the ONLY shape
> that triggers the divergence — an INDEXED file whose ALTERNATE RECORD KEY sits AFTER a REDEFINES with a WIDER redefiner
> (e.g. `05 A PIC X(3). 05 B REDEFINES A PIC X(5). 05 KEYITEM PIC X(2).`) — so "verified by the Keyed goldens" is a REAL
> net, not a hollow claim. No existing golden covers this; without it the deferral is unbacked.

1. `CodeGen/Emit/FieldEmitter.cs` — the width consumer; flip its width to `RecordLayout` and its `StoreAsImage`/`IsCharacterImage` reads to `Storage`. **First**, so `RecordLayout` becomes the sole width source at the highest-leverage site (DESIGN §5.4 mitigation).
2. `CodeGen/Emit/NumericRenderer.cs:75,105` — `Storage is CharImage{Category:Numeric}` in place of `StoreAsImage`.
3. `CodeGen/Emit/OperandText.cs:67,87` — same.
4. `CodeGen/CSharpEmitter.cs:531,574,664,803,1119,1123` + `.Accept.cs:66,127`, `.Inspect.cs:100`, `.StringUnstring.cs:154,160,211`, `.Sort.cs:187,225`, `.KeyedIo.cs`, `.Call.cs:530,940`, `.Oo.cs:360,662,821`, `.ReportWriter.cs:62` — flip each `StoreAsImage` / `IsCharacterImage` / `IsImageCapable` read to `Storage` / the cached image-fact fields.
5. `Binding/ReferenceResolver.cs:155,175,201` — Place selection now reads `Storage` (DESIGN §2.2 item 2): the `NumericImagePlace` wrap decision uses `item.Storage is CharImage{Category:Numeric}` instead of `!item.StoreAsImage`. Retire the "decided AFTER this text" remark on `NumericImagePlace` (`Place.cs:156-159`) — the form is known at resolve time now.
6. `Binding/Bound/StatementBinder.Inspect.cs`, `.StringUnstring.cs`, `.Corresponding.cs`, `.Sort.cs`, `.Initialize.cs` — flip remaining `StoreAsImage` reads; Sort/Keyed geometry → `RecordLayout`.
- **Why:** every reader consumes the single computed form (EXIT CRITERION #1); the runtime overload bridge stays during this phase so emitted text keeps compiling (DESIGN §4 D2).
- **Verify (per file):** greenfield battery green; at each commit boundary full legacy guard + snapshots.
- **Commits (one per file/cluster):** `refactor(cobolnet): P5.8a FieldEmitter reads Storage + RecordLayout width (D2) (DEVLOG NNN)`, `…P5.8b NumericRenderer/OperandText…`, `…P5.8c CSharpEmitter partials…`, `…P5.8d ReferenceResolver Place selection off Storage…`, `…P5.8e StatementBinder verb partials…`.

### Step 9 — Delete the 4 duplicate width-geometry copies — COMMIT BOUNDARY
DESIGN §2.6 / Phase D3 (width half).
- **Do:** now that FieldEmitter + Sort + Keyed consume `RecordLayout`, delete `OdoModel.PhysicalWidth` (`OdoModel.cs:~155`), the `Sort.cs` geometry (`SortPhysicalWidth`/`SortOffsetInRecord`/`SortPlainOffset`), and the `KeyedIo.cs` geometry (`KeyedAreaOffset`/`KeyedKeyIndex`). Repoint their callers to `RecordLayout`. Keep the Step 4 width-equivalence assert green until the last copy is gone, then delete the assert.
- **Verify:** greenfield battery green; full legacy guard; snapshots neutral.
- **Commit:** `refactor(cobolnet): P5.9 delete 4 duplicate width-geometry copies → RecordLayout single-sourced (DEVLOG NNN)`

### Step 10 — Delete `StoreAsImage`; cache the image facts as init-only fields — COMMIT BOUNDARY
DESIGN Phase **D3**.
- **10.1** Delete the `DataItem.StoreAsImage` projection entirely (the ~5 sites that genuinely need "numeric leaf stored as image" now use `Storage is CharImage{Category:Numeric}` directly — DESIGN §2.1).
- **10.2** Convert `IsCharacterImage`/`IsImageCapable`/`ImageWidth` from recursive computed properties (`DataItem.cs:245-300`) to **cached init-only fields** filled bottom-up in ONE O(n) walk by `StorageFormPass` (image facts) — removes the ~119 O(subtree) re-walks (DESIGN §2.4). `ImageWidth` reads via `RecordLayout` and is cached. Make `DataItem.Storage` **init-only** (drop the D0 mutable setter).
- **10.3** `ElementType`/`FieldType`/`ClrType` (`DataItem.cs:304-314`) become thin projections of `Storage` (+ `Occurs` for the `[]`/`CobolDynTable<>` wrap), not `(Pic, StoreAsImage)`.
- **Why:** the mutable flag is gone; recompute-on-read is gone; `Storage` is the one source.
- **Verify:** greenfield battery green; full legacy guard; snapshots neutral. Add/keep a unit test asserting the cached fields equal a fresh recompute for a representative tree.
- **Commit:** `refactor(cobolnet): P5.10 delete StoreAsImage; image facts = cached init-only fields filled once (D3) (DEVLOG NNN)`

### Step 11 — Move the data model into `Binding/Model/`; slim `DataItem`; extract `PictureAnalyzer` + `StrongTypeModel`; single-source Tier-C — COMMIT BOUNDARY(S)
DESIGN Phase **D4**. These are independent, each green-gated; split into sub-commits.
- **11.1 Move files** (git mv, namespace → `CobolNet.Binding.Model`, add `using CobolNet.Binding.Model;` where needed): `DataItem.cs`, `PicInfo.cs`, `Place.cs`, `RedefinesModel.cs`, `OdoModel.cs`, `FileModel.cs`, `Condition88.cs` → `Binding/Model/`. Fold `OdoGroupPlace` (`OdoModel.cs:89`) into `Model/Place.cs`; add the `PlaceDecorator(Place Inner)` base and derive `NumericImagePlace`/`RefModPlace`/`OdoGroupPlace`/`RenamesPlace` from it (DESIGN §2.2 item 1). Commit: `refactor(cobolnet): P5.11a move data model to Binding/Model/; fold OdoGroupPlace + PlaceDecorator base (DEVLOG NNN)`.
- **11.2 Extract** `StrongTypeModel` (`Binding/Model/StrongTypeModel.cs`): move `SameStrongType`/`TypeAnchor`/`StrongRoot`/`RelativeMemberPath` (`DataItem.cs:74-133`) as static helpers over `DataItem`; leave thin forwarding props on `DataItem` only if callers need them (prefer repointing callers). Commit: `…P5.11b extract StrongTypeModel off DataItem`.
- **11.3 Extract** `PictureAnalyzer` (`Binding/PictureAnalyzer.cs`): move the 230-line `PicInfo.Analyze` scanner (`PicInfo.cs:333-562`) into `PicInfo Analyze(...)`; delete the dead skeleton scaffolding (`IsUnimplementedSkeleton`, `SkeletonReached`, the 3 `ReferenceEquals` sentinel singletons `PicInfo.cs:175,216,691-705`) — replace the sentinels with a proper `PicAnalysis` result discriminant (`Ok | GroupUsageShed | Recover`). `PicInfo` becomes a pure value record. Commit: `…P5.11c PicInfo pure value record; PictureAnalyzer extracted; skeleton scaffolding deleted`.
- **11.4 Init-only `RedefinesClass`** + single-source Tier-C: make `RedefinesClass.Tier`/`.Width` (`RedefinesModel.cs:70,74`) and `DataItem.ClassOffset` (`DataItem.cs:213`) init-only, written once by the `RedefinesClassifier` pass (rename `ClassifyRedefinesClasses` accordingly if convenient — else leave the method name). Add `RedefinesClassifier.RejectTierC(class, reason)` as the ONE Tier-C verdict + a `TierCWindow.Read/Write` internal-error backstop; delete the ~10 scattered inline Tier-C guards (`DataBinder.cs:1606,1631,1642`, `CSharpEmitter.cs:565,615,1759,1794`) in favor of the single verdict. Keep each deleted guard's ISO citation in the `RejectTierC` reason table (DESIGN §5.3 mitigation). The confined `byte[]` codec stays DEFERRED to P11. Commit: `…P5.11d RedefinesClass init-only; single-source Tier-C rejection (byte[] codec deferred to P11)`.
- **11.5 Rename** `ResolveIndexItems` → fold into `UsageInheritancePass` (merge with `InheritUsageClauses`) — DESIGN §2.7. This is a no-behavior rename+merge; keep both effects. Commit: `…P5.11e fold ResolveIndexItems into UsageInheritancePass`.
- **Verify (each sub-commit):** greenfield battery + full legacy guard + snapshots neutral.

### Step 12 — Remove the equivalence scaffolding; finalize the pipeline — COMMIT BOUNDARY
- **Do:** Delete the now-trivial Storage/width/whole-group equivalence asserts added in Steps 2/4/5 (they compared against deleted legacy paths). Confirm `BindPipeline.ValidateDag()` covers the full order incl. `UsageCollectionPass` and `StorageFormPass` and that they are driven from the pipeline (they may still be invoked from `CallEmitRunUnit` since they need the bound tree — that is fine for this phase; the real Bind-phase extraction is P6). Ensure the `PassPhase` DAG asserts `StorageFormPass.Requires == UsageCollected` and `UsageCollectionPass.Requires == ProcedureBound`.
- **Verify:** full battery + legacy guard + snapshots.
- **Commit:** `chore(cobolnet): P5.12 remove equivalence scaffolding; finalize pass DAG (DEVLOG NNN)`

### Step 13 — Apostrophe-delimited VALUE conformance golden — COMMIT BOUNDARY (ISO feature verification)
- **Do:** Add `tests/Cobol.Net.Tests.Conformance/ApostropheValueDifferentialTests.cs` (follow the existing `*DifferentialTests` harness idiom, e.g. `AllLiteralDifferentialTests.cs` / `FigurativeDifferentialTests.cs`). Cover: (a) an elementary `PIC X(3) VALUE 'AB'` and `VALUE 'A''B'` (embedded apostrophe); (b) a group with an apostrophe VALUE child; (c) `INITIALIZE ... REPLACING ... BY 'x'` and a `MOVE ALL 'x'`; (d) a Report-Writer `SOURCE`/`VALUE 'x'` if the RW harness supports it (else note as covered by (a)-(c)). Assert byte-exact output equals the expected (the `"` and `'` forms MUST produce identical output — ISO §8.3.1.2 equal-standing). Verify this test would have FAILED before Step 1 (spot-check by reverting `CobolLiteral.IsStringLiteral` to `"`-only locally, seeing red, restoring).
- **Why:** EXIT CRITERION #6; locks the DESIGN §2.8 fix with a regression golden.
- **Verify:** the new test green; full battery green.
- **Commit:** `test(cobolnet): P5.13 apostrophe-delimited VALUE conformance golden (§8.3.1.2) (DEVLOG NNN)`

### Step 14 — Docs sync + phase close — COMMIT BOUNDARY
- **Do:** Update `DESIGN-data-model.md` status banner to reflect what actually landed vs the design (per the owner's "keep deep-dives current" rule); update `docs/DOC_INDEX.md` if new docs/rows are warranted; mark this phase DONE in the STATUS line + §7 ledger; tick P5 in `docs/COBOLNET_REARCHITECTURE_PLAN.md`'s §4 phase-index checklist (the migration SSOT / ROADMAP). Add the closing DEVLOG entry.
- **Verify:** full battery + legacy guard + snapshots — the phase-end verification (§5).
- **Commit:** `docs(cobolnet): P5 DONE — unified data model / StorageForm landed; DESIGN-data-model synced (DEVLOG NNN)`

---

## 5. Verification (run at phase end)

Full battery, byte-exact / behavior-neutrality:

1. **Greenfield conformance + unit:** `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance E:/CobolSharp/tests/Cobol.Net.Tests.Unit -c Debug` — expect the baseline counts from Step 0 (≈2028 conformance + 213 unit), zero failures, PLUS the new `CobolLiteralTests`, `BindPipelineTests`, and `ApostropheValueDifferentialTests`.
2. **Full legacy differential guard:** `bash scripts/guard.sh` — expect NIST **353 MATCH** + the known `LEGACY_DIVERGENT` set unchanged. (This is the frozen-oracle net; it MUST NOT regress — it is deleted only at G8/P15.)
3. **Characterization snapshots (P0):** run the characterization snapshot test — expect **neutral** (no emitted-C# diff). Any intentional emit change (e.g. `NumericImagePlace` selection now off `Storage`) that alters generated text must be re-baselined WITH a gate-1 (output-golden) proof that behavior is preserved, per `DESIGN-test-build-ci.md` gate policy — but the target of this phase is byte-stable emission, so treat any snapshot diff as suspect and justify it explicitly in the DEVLOG.
4. **Exit-criteria audit:** walk the 7 exit criteria in §1 and confirm each in the DEVLOG close entry, citing the step that satisfied it.

Behavioral spot-checks with the prebuilt CLI (sanity, not a substitute for the battery):
```
dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <prog>.cob --std 2002 -o E:/tmp/out.dll --run
```
Named probes: a whole-group MOVE program (numeric-DISPLAY leaf under a moved group → space-fill visible), an ODO group MOVE, a Tier-B REDEFINES numeric view, and an apostrophe-VALUE program (identical output to its `"`-quoted twin).

---

## 6. Rollback / resumability

- **Resume point = the §7 step ledger + the STATUS line.** Each step is a self-contained commit; `git log --oneline` shows the last `P5.N` commit. Resume at N+1.
- **The prove-then-delete boundary is Step 6.** Everything up to Step 6 is ADDITIVE (parallel SSOT + asserts) — if interrupted, the tree is fully green and nothing is deleted; you can revert the incomplete step with no residue. Deletions begin at Step 7; from there each commit is individually green, so `git revert <sha>` of a single P5.N commit is safe.
- **Highest risk = whole-group promotion parity (DESIGN §5.1).** `StorageFormPass` must reproduce `MarkStoreAsImage` + the FILE/Linkage/Reports/MoveFigurative/OO/CompilerTemp sites EXACTLY. Mitigation: the Step 2/6 corpus equivalence assert is MANDATORY and must be green before Step 7. If a divergence is found after Step 7 (a golden regresses), the fix is in `StorageFormPass`, not a reintroduced mutation — never re-add a `StoreAsImage = true` write.
- **Overload-bridge timing (DESIGN §5.2).** Do NOT touch the runtime `Occ`/`StoreDisplay`/`FormatDisplay` overloads in this phase — they keep emitted text compiling while readers flip in Step 8. Their retirement is DESIGN D5 / a later phase.
- **`RecordLayout` drift (DESIGN §5.4).** Keep the width-equivalence assert (Step 4) green until the last duplicate copy is deleted in Step 9; flip FieldEmitter first (Step 8.1).
- **Immutability vs set-order (DESIGN §5.5).** `Uid`/`Parent` are legitimately set during tree build; freeze in tiers — core after `BindEntries`, each pass-fact after its pass. `Storage` becomes init-only only at Step 10 (mutable during the D0 parallel phase). The `PassPhase` DAG assert catches an out-of-order write.

---

## 7. ISO feature work in this phase

- **Apostrophe-delimited literals — ISO/IEC 1989:2023 §8.3.1.2** (quotation-mark and apostrophe forms are equal-standing; a doubled opening delimiter is one embedded delimiter). Editions: ALL (85/2002/2014/2023 — apostrophe literals are version-invariant). Test/golden: `ApostropheValueDifferentialTests.cs` (Step 13) — elementary/group/`ALL 'x'`/RW VALUE, each proving the `'` form equals the `"` form byte-for-byte. This closes the confirmed silent miscompile (DESIGN §2.8).
- **National/boolean widths (DESIGN open Q5):** `StorageForm.CharImage.Width` inherits the existing D-N1 rule — national is one UTF-16 char per position, `ImageWidth == Length`, never byte-doubled. This phase MUST NOT change that; a future 2-byte layout would be a NEW `StorageForm` case, not a mutation. No new test beyond the existing `NationalBoolean*` suite staying green.
- No other ISO *feature* is introduced in this phase — it is a rearchitecture phase. The MOVE GR4 whole-group semantics (§14.9), REDEFINES tiers (§13.18.44), ODO (§13.18.38), and USAGE (§13.18.60) behaviors are PRESERVED byte-exact (that is the whole point of the prove-then-delete gate), not changed.

### Step ledger (executing session keeps this current)

- [x] Step 0 — baseline captured (counts: 3157 conformance / 227 unit / NIST 353 MATCH) — DEVLOG 747
- [x] Step 1 — CobolLiteral.Decode (both ISO delimiters; 3 twins + hard-coded '"' guards deleted; CobolLiteralTests ×21) — DEVLOG 747
- [x] Step 2 — StorageForm (9 cases; DESIGN §2.1 amended: Width field) + StorageFormPass.Compute (parallel, D0) + StorageFormEquivalenceTests (NIST corpus + crafted, 0 divergences); conformance 3157 · unit 254 · characterization 32 byte-exact — DEVLOG 749
- [x] Step 3 — IBindPass scaffolding + ValidateDag — `IBindPass.cs` (PassPhase enum + interface + BindPass record) + `BindPipeline.cs` (Build ordered list [16 resolve + 3 tail markers] + ValidateDag monotone assert); BindResolve pipeline-driven (Produces<=FilesResolved prefix); inline FILE loop → MarkFileRecordImageLeaves(); 12 methods private→internal; BindPipelineTests ×4; conformance 3157 · unit 258 · characterization 32 byte-exact; exit criterion #5 satisfied — DEVLOG 750
- [x] Step 4 — RecordLayout parallel + width assert — `Binding/Model/RecordLayout.cs` (ImageWidth [reads StorageForm; ImageWidthOf consolidated onto it] + PhysicalWidth [tier-aware, mirrors OdoModel.PhysicalWidth]); Verify identity #5 (RecordLayout.PhysicalWidth == OdoModel.PhysicalWidth per group, corpus-wide) — §5.4 drift guard. ADDITIVE, test-path only. ⚠ SCOPING: OffsetOf/KeyIndexByPosition DEFERRED to Step 8 (Sort=PhysicalWidth vs Keyed=ImageWidth increment divergence — cannot pure-port-prove both). conformance 3157 · unit 258 · characterization 32 byte-exact — DEVLOG 751
- [x] Step 5 — UsageCollectionPass owns WholeGroupReferenced (REDESIGNED per owner: the correct set from an explicit typed bound-tree walk, not legacy's over-inclusive mid-resolve mutation which is DELETED); DynTablePlace skipped (dynamic elements use the table codec); char_initialize snapshot re-baselined (output-neutral WS_N string→long); verified by OUTPUT (conformance 3157 runtime · characterization 32 · guard 353 MATCH) — DEVLOG 752/753
- [x] Step 6 — prove-then-delete GATE green (Exec Step C entry, 2026-07-11: conformance 3159 · unit 281 w/ equivalence identities #1–#5 · characterization 32 byte-exact · NIST 353 MATCH · CI green both configs) — DEVLOG 776
- [x] Step 7 — the FLAG IS DEAD (DEVLOG 777): 9 write sites → the ONE collected `DataBinder.ImageForcedItems` fact set (structural derivation REJECTED — Ptr-forced classes never flagged, the set reproduces truth exactly); `StorageFormPass.Run` = facts→resync→whole-group union → Classify (incl. INTERFACE forests) → `HarmonizeStorageCrossings` (Storage-level fixed point); `StoreAsImage` = read-only projection of `Storage`; the two bind-time `NumericImagePlace` wrap decisions read `IsImageBackedEarly` (identical mid-bind timing, order dependency preserved + noted for P7). 7a ran identity #1 REAL (set-derived vs flag, corpus green) BEFORE 7b deleted. Audit findings: `IsImageCapable` decision-independent (leaf arm now pure-Pic); `IsCharacterImage` readers all emit-time (the SORT-binder comment was stale — it reads IsImageCapable). Byte-exact: 3159 conf · 281 unit · 32 char · guard 353
- [x] Step 8+9 (landed together, DEVLOG 778) — RecordLayout = THE single width/offset authority (OffsetOf /
  OffsetInRecord / AreaWidth / KeyIndexByPosition; leaf widths DECLARED-shape ⇒ phase-free); all 6 geometry
  copies DELETED (Sort ×3, Keyed binder ×2, emitter twin, OdoModel.PhysicalWidth); identity #5 retired.
  ⚠ THE PREMISE DISSOLVED BY THE SPEC: the "wider-redefiner offset divergence" shape is ILLEGAL (§13.18.44.3
  SR8 + SR3) — the fold is a PURE PORT on legal inputs; the REAL finds: SR8 was silently unenforced (now
  COBOLNET1539), and the PhysicalWidth twins' class-skip needed MEMBERS-gating (a multi-record FD's record
  width collapsed to 0; the canonical may itself be a redefiner). KeyedOffsetSpecTests ×4.
  NOTE re the doc's Step-8 reader-flip list: the ~35 emit-time StoreAsImage/IsCharacterImage reads already
  consume Storage TRANSITIVELY through the Step-7 projection — the named projection IS the one definition
  (singular-pattern); a mass textual flip adds churn without behavior and is dropped as a deviation.
- [x] Step 10 (DEVLOG 779, deviations recorded) — ClrType DELETED (zero readers); Storage sealed to the ONE
  writer (internal set; init-only inexpressible with pass assignment); StoreAsImage KEPT as the named projection
  (the one definition beats 35 pattern repetitions); the image-fact caching DEVIATED to P7 (the hazard died with
  the flag; the O(subtree) perf work belongs with the DataItem slimming)
- [x] Step 11 COMPLETE (a–e, DEVLOG 780–784) — a DONE (DEVLOG 780): 6 files git-mv'd to `Binding/Model/` (ns `CobolNet.Binding.Model`);
  `OdoModel.cs` SPLIT (model half → Model/, the DataBinder ODO partial → `DataBinder.Odo.cs` — the old file mixed
  a pure model with a binder pass); `OdoGroupPlace` folded into `Place.cs`; `PlaceDecorator(Place Inner)` base
  forwards Pic/Item + default Read/Write; `NumericImagePlace`/`RefModPlace`/`OdoGroupPlace` derive.
  DEVIATION (in DESIGN §2.2): `RenamesPlace` stays direct — N spanned leaves, no single inner; its Pic/Item are
  the level-66 alias's own (§13.18.45) — deriving = overriding every forward.
  b DONE (DEVLOG 781): `StrongTypeModel` static helpers (`Model/StrongTypeModel.cs`); NO forwarding props — all
  6 caller sites repointed; `DataItem` keeps only the stored `StrongType`/`TypeName` facts.
  c DONE (DEVLOG 782): `PictureAnalyzer` extracted (`Analyze` + `ParseUsage` + helpers; PicInfo 736→~350 lines,
  a pure value record + projections); dead skeleton scaffolding deleted (`IsUnimplementedSkeleton` const-false,
  `SkeletonReached`, 4 guard arms, the constant-false `out bool skeleton` overload);
  DEVIATION (in DESIGN §2.7): the sentinel replacement is `DataItem.Pending : PicPending` + the
  `PicInfo.Recovery` factory, NOT an Analyze-result `PicAnalysis` — the pending sentinels never came from
  Analyze (picture-less entries; the verdict needs the complete forest).
  d DONE (DEVLOG 783): `Tier`/`Width`/`RejectReason` private-set behind the ONE named mutator
  `RedefinesClass.Classify` (two documented callers: the classifier's §13.18.44 verdict; the cell forcer's
  §13.18.22.4 GR5 re-base); `ClassOffset` internal-set (one writer: `AssignClassOffsets`).
  DEVIATIONS: strict init-only UNIMPLEMENTABLE (the cell re-classification is a real second write by design);
  the doc's "~10 scattered Tier-C guards" don't exist as verdicts today (Phase 1E folded the island into
  `ComputeTier`; the emitter loud guards key off `IsImageCapable`, one definition); a `TierCWindow.Read/Write`
  backstop adds nothing (Rejected views already fail loud at resolve; `StorageForm.TierCWindow` = the P11 seam).
  e DONE (DEVLOG 784): ONE `UsageInheritancePass` manifest entry (TypesExpanded → UsageResolved); the two bodies
  stay as private same-named halves (doc anchors intact), called in the original order.
- [x] Step 12 (DEVLOG 779) — identities #1/#5 retired with their subjects; #2/#3/#4 stay (#3 guards two live
  computations); the DAG was finalized at P6 (GroupTail + terminal conformance pass)
- [x] Step 13 (DEVLOG 779) — ApostropheValueDifferentialTests ×3, PROVEN failing-first under a '"'-only
  IsStringLiteral revert (1/3 fails; restored 3/3); exit criterion #6 holds
- [ ] Step 14 — docs sync + phase close
