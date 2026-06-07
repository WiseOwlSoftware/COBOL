# Record-Struct Storage Substrate — Staged Engineering Design

**Status:** Approved direction (owner decision 2026-06-06, DEVLOG 400) · **Parent:** `docs/DATA_MODEL_ARCHITECTURE.md` (the ADR)
**Scope:** the concrete, guard-green, staged build of the ADR §9 *literal* storage intent — COBOL records as live .NET
`record struct`s — within the existing compiler.

> This document is the engineering roadmap; it does **not** re-open the ADR's settled vision (typed-native default,
> character → `string`, byte islands as a classifier-scoped fallback, pointers → managed refs, OO → classes). It
> resolves the one thing the ADR left **non-functional**: its nominal storage home (`RecordLayoutBuilder` →
> `record struct`) is dead code, so "where typed values live" had to be decided. The owner chose **Option B: build
> the real record-`struct` substrate** (over a parallel-static-field shortcut, and explicitly over the
> dual-write/shadow hack — which is rejected as a transitional hack, `feedback_no_transitional_hacks`).

## 0. The reconciling insight (why this is tractable, not a rip-out)

Building "records as live `record struct`s" does **not** require removing `ProgramState`'s three `byte[]` areas.
Byte-backed items — file records (trigger 5), REDEFINES/RENAMES islands (1/2), LINKAGE (12), EXTERNAL/GLOBAL (8),
edited (13), and **every not-yet-flipped item** — **stay** in the byte areas, which become the migration's
permanent safety floor (ADR §1.6). Only items the `RecordClassificationPass` marks **Typed** move out of the byte
areas into native fields of an emitted `record struct`. The two representations meet only at the §2.5 `IDataSlot`
chokepoint. So the substrate grows by *flipping one rule at a time*, never by a wholesale storage swap, and the
fully-tested byte engine is always a valid fallback — the guard stays green at every step.

## 1. Current state (grounded)

- **Live storage:** `ProgramState` (`src/CobolSharp.Runtime/StorageArea.cs`) = three `byte[]` (`WorkingStorage`,
  `FileSection`, `LocalStorage`), one static `State` field per program type (`CilEmitter.EmitProgramState`).
- **Live layout:** `StorageLayoutComputer` (Semantics, `Compilation.cs:123`) assigns `(Area, Offset, Length, Pic)`
  per `DataSymbol` into `SemanticModel` storage locations; the byte engine (`PicRuntime`/`StorageHelpers`) slices
  `area[offset..offset+length]`.
- **Dead subsystem:** `RecordLayoutBuilder.Build` (`Binder.BuildRecordTypes`, line 250) emits an `IrRecordType`
  with `IrField`s into `module.Types`, but nothing accesses those fields (`IrLoadField`/`IrStoreField` have **zero
  producers**). Its **only live effect** is setting `DataSymbol.ElementSize` (also computed by `FieldSizeCalculator`,
  used by `StorageLayoutComputer`). `ARCHITECTURE_ASSESSMENT.md` item 17 lists it for excision.
- **Classifier:** `RecordClassificationPass` (Phases A+B+C) is **complete** (DEVLOG 397–398), additive, not yet
  consumed by codegen.
- **Substrate runtime:** `CobolNum`/`CobolDecimal`/`NumProfile` (numeric, DEVLOG 394–396) and `CobolString`
  (character + Latin-1 boundary codec, DEVLOG 399) are landed and differential-oracle-proven.

## 2. Target representation

For each `01`/`77` record the layout produces **two products** (ADR §9):

1. A `record struct` .NET type whose members are the record's **typed** items, by ADR §4 mapping:
   `PIC X/A/N` → `string`; `PIC 9..` integer → `int`/`long`; scaled/`COMP-3` → `decimal`; `COMP-1/2` → `float`/
   `double`; `PIC 1` → `bool`; `OCCURS` of a typed element → `CobolTable<T>`/`T[]`; a **byte island** inside the
   record → an inline blittable byte member (`[InlineArray(n)]`) embedded in the struct (ADR §2.2/§2.3), never a
   heap `byte[]`.
2. The **byte image** for the byte-backed remainder (today's `StorageLocation` offsets into `ProgramState`), used
   unchanged by the byte engine and by file/LINKAGE/EXTERNAL interop.

A record with no typed items (overlay-heavy NIST records) emits **no** typed struct — it stays purely byte-backed,
exactly as today. A record with only typed items emits a struct and needs **no** byte-area slice. A mixed record
emits a struct (typed fields + inline byte islands) and keeps the byte slice for the islands. The program holds one
static instance per typed/mixed record (the LINKAGE/EXTERNAL static-field precedent), initialized per ADR §1.7.

## 3. Addressing — the `IrDataSlot` sum type

`LocationResolver` becomes the single fork (ADR §2.5):

```
IrDataSlot
 ├─ TypedFieldSlot(recordInstanceRef, fieldRef, FieldShape)   // a native field of an emitted record struct
 └─ ByteWindowSlot(IrLocation, FieldShape)                    // today's (area,offset,length) quad — re-targeted
```

- `RecordClassification.IsTyped(symbol)` → `TypedFieldSlot`; otherwise the existing `IrLocation` wrapped as
  `ByteWindowSlot`. **Byte items are never re-addressed.** The existing `IrStaticLocation`/`IrElementRef`/
  `IrRefModLocation`/`IrOdoGroupLocation` hierarchy is *re-targeted* to produce `ByteWindowSlot` (ADR §9), not
  redesigned.
- MOVE/COMPARE/arith/DISPLAY dispatch on the static slot-kind **pair** (ADR §2.5 table). The universal fallback:
  **any `TypedFieldSlot` can materialize a `ByteWindowSlot`** by encoding its value into a scratch span via
  `CobolString.ToWindow` / the numeric codec, so the byte×byte cell (the fully-tested engine) implements the other
  three for any op a typed fast-path doesn't yet cover. This is the safety floor, not a shadow copy: the window is
  transient scratch, reconstructed from the canonical typed value each time, and written back to the typed field
  only when it is a receiver (`CobolString.FromWindow`). **No dual-write, no drift.**

## 4. The dead `RecordLayoutBuilder` — rebuild, don't shadow

`RecordLayoutBuilder`/`IrRecordType`/`IrField`/`IrLoadField`/`IrStoreField` are rebuilt as the **real** producer of
the typed struct + typed field access (this is what makes Option B "the real substrate," not a parallel hack). No
vestigial dead emission is left behind (PROMPT.md zero-dead-code): every type/field/IR node added is reachable in
the commit that adds it.

**`ElementSize` — make `StorageLayoutComputer` the sole writer (review-confirmed latent hazard).** Today
`DataSymbol.ElementSize` is written **twice**: by `StorageLayoutComputer` (read by `ExpressionBinder`, which runs
*before* the Binder) and again by `RecordLayoutBuilder.LayoutOne` (lines 88, 97; read by `LocationResolver`/
`FileIoLowerer`/`CilLocationEmitter`, which run *after*). Two writers feeding different consumers by temporal
ordering is a maintenance trap, harmless today only because both compute via `FieldSizeCalculator`. The S3 rebuild
**deletes the `ElementSize` assignments from `RecordLayoutBuilder`**, leaving `StorageLayoutComputer` the single
writer, guarded by a `Debug.Assert` that the value `RecordLayoutBuilder` would have computed equals the already-set
`ElementSize` (any divergence becomes a test failure, never silent corruption).

## 5. Initialization (ADR §1.7) — never `default(T)`

Every typed field gets a COBOL-correct initializer at program load (and per-invocation re-init for LOCAL-STORAGE):
alphanumeric → `new string(' ', n)` or the padded `VALUE` literal; numeric → the `VALUE` or `0` via `CobolNum`;
figurative `VALUE` → the materialized Latin-1 constant (`HIGH-VALUE`=U+00FF, `LOW-VALUE`=U+0000). A `VALUE` not
expressible as a typed initializer (group-level over mixed members; value read back through an overlay) is itself a
classification trigger → byte-backed, initialized by writing its byte image (today's path). `INITIALIZE` lowers to
the same per-field initializers.

## 6. Staged plan — guard-green at every step (1184 / 481 / 364)

Each stage is a commit; the guard must be ALL GREEN before the next. The kill-switch
`CompilationOptions.EnableTypedFields` (default OFF until S3) makes the substrate reversible to byte-identical.
**Staging is fixed (review-confirmed): S1 is standalone and reachable; the IR/resolver scaffolding and the
`RecordLayoutBuilder` rebuild do NOT land as separate gated-OFF stages — an unconstructed `TypedFieldSlot` or an
emitted-but-unused struct would be dead code — so they land *with* the first flip in S3, where they gain a producer
the moment they exist.** No `[CompilerScaffold]`/warning-suppression escape hatch.

- **S1 — wire the classifier into the Binder.** Add `CompilationOptions.EnableTypedFields = false`; run the
  (complete) `RecordClassificationPass` in `Binder.Bind` after `BoundProgram` is built; store the result on
  `LoweringContext`; force all-byte while the flag is OFF. Fully reachable and zero-dead-code: the classifier runs
  on the whole corpus and a unit test asserts the forced-byte invariant. No behavior change; guard byte-identical.
- **S3 — the first flip, in one commit** (subsumes the former S0/S2 scaffolding): introduce `IrDataSlot`/
  `TypedFieldSlot`/`ByteWindowSlot`/`FieldShape`, rebuild `RecordLayoutBuilder` as the real typed-struct producer
  (emit the `record struct` type + static instance; `StorageLayoutComputer` becomes the sole `ElementSize` writer
  per §4; excise the now-replaced dead `IrLoadField`/`IrStoreField` or make them the real accessors), and **flip an
  all-character `01` record → a `record struct` of `string` fields**, gated ON for the narrowest subset (records of
  only elementary `PIC X/A/N` items, **no** triggers 1–15). Implements the `TypedFieldSlot` cells the subset needs
  — typed↔typed MOVE (ref copy / `CobolString.Store`), typed↔byte materialize, DISPLAY (native `string`),
  alphanumeric COMPARE (`CobolString.Compare`) — everything else materializes to byte. Every added member is
  reachable on the flipped path. Ships a `tests/conformance/2002/` test. Overlay-heavy NIST records stay byte-backed
  and green.
- **S4+ — widen one rule at a time**, each its own guard-green commit + conformance test: **numeric typed fields —
  HARD GATE: the `CobolNum` differential oracle (full digits×scale×sign×usage×rounding×overflow grid, with the
  independent >28-digit `BigInteger` reference, ADR §12 Open-Q#1 / R2 / R5) must be green BEFORE the first numeric
  `TypedFieldSlot` is emitted**; then group MOVE/COMPARE materialization; `OCCURS` → `CobolTable<T>`; then pointers
  + OO (ADR Stage 4) and the Roslyn backend (ADR Stage 5).

### 6.1 S3 implementation checklist (code-grounded, DEVLOG 402)

Derived from reading the actual emit seams. S3 is **one atomic commit** (gated by `EnableTypedFields`, default
OFF, so the whole guard stays byte-identical; a dedicated flag-ON conformance test drives + verifies the typed
path and makes every new member reachable / zero-dead-code).

1. **Type emission is reusable.** `CilEmitter.DefineType` (`CilEmitter.cs:870`) already emits an `IrRecordType`
   as a sealed `SequentialLayout` `ValueType` with `public` fields and records each in `_fieldMap`. For a flipped
   all-character record, have `RecordLayoutBuilder.MapToIrType` (`RecordLayoutBuilder.cs:120`) return
   `IrPrimitiveType.String` (instead of `ByteArray`) for a classifier-Typed elementary `PIC X/A/N` item — then
   `DefineType` emits a `record struct` of `string` fields with no further change.
2. **Static instance.** Emit one `static <RecordStruct> _<record>` field on the program type (alongside `State`),
   the home of the flipped record's typed fields (mirrors how `State`/LINKAGE/EXTERNAL static fields are emitted
   in `EmitProgramState`).
3. **Init (ADR §1.7).** In `InitializeState`, set each typed `string` field to `new string(' ', n)` or the padded
   `VALUE` literal / materialized Latin-1 figurative — never `default(string)`.
4. **Location fork (the IrDataSlot chokepoint, §3).** Add `IrDataSlot`/`TypedFieldSlot`/`ByteWindowSlot` +
   `FieldShape`; `LocationResolver` returns a `TypedFieldSlot` (record instance + field) when
   `_ctx.Classification.IsTyped(sym) && EnableTypedFields && sym` is an elementary char field of a flipped record,
   else the existing `IrLocation` wrapped as `ByteWindowSlot`. **Do NOT** reuse the dead `IrLoadField`/
   `IrStoreField` — they are a *register-model* relic (load a field into an `IrValue`) that does not fit COBOL's
   *location-model* MOVE/COMPARE; that mismatch is why they are dead. They are excised as part of this commit.
5. **Emit cells.** `CilDataEmitter`/`CilLocationEmitter` dispatch on the slot-kind pair: typed→typed MOVE = ref
   copy / `CobolString.Store`; typed↔byte = `CobolString.ToWindow`/`FromWindow` materialize at the boundary;
   DISPLAY of a typed string = native; alphanumeric COMPARE = `CobolString.Compare`; **every other op materializes
   to a byte window** (the §1.6 floor) — `Span<byte>` scratch from the typed value, run the existing byte op, read
   back if a receiver. The slot-pair switch has **no fall-through** (a missing cell is a compile-time emit assert).
6. **`ElementSize` (§4) — verify before deleting.** `StorageLayoutComputer` already sets `ElementSize` (lines
   256/272/318/366) *before* the Binder, so `RecordLayoutBuilder`'s writes (88/97) are redundant. They compute the
   elementary size via the **same** `FieldSizeCalculator.ComputeElementSize`, but the two passes sum **group**
   sizes independently — so before making `StorageLayoutComputer` the sole writer, assert (or guard-diff) that the
   two agree on every group, not just elementary items.
7. **Test/harness.** A `tests/conformance/2002/typed_char_*.cob` (+`.out`) compiled with `EnableTypedFields=ON`
   (a harness hook) exercises store→typed + DISPLAY/COMPARE; the rest of the corpus (flag OFF) stays byte-identical.

## 7. Risks (beyond the classifier's own, covered in `DATA_MODEL_REVIEW.md` / RecordClassification)

| # | Risk | Mitigation |
|---|---|---|
| 1 | Re-addressing a byte item by mistake breaks file/EXTERNAL/REDEFINES | Byte items are **never** re-addressed — they keep their `IrLocation`/`StorageLocation`; only Typed items get `TypedFieldSlot`. |
| 2 | A `TypedFieldSlot` reaches an op with no typed cell and no materialize fallback → `InvalidProgramException` | The §2.5 guarantee: every typed slot can materialize a `ByteWindowSlot`; the emitter's slot-pair dispatch has **no fall-through** (a missing cell is a compile-time emitter assert, not silent NOP). |
| 3 | Typed init diverges from COBOL fill | ADR §1.7 explicit initializers; `tests/conformance` VALUE/INITIALIZE cases per flipped category; never `default(T)`. |
| 4 | `ElementSize`/size side-effect lost when rebuilding `RecordLayoutBuilder` | Preserve it (relocate to `FieldSizeCalculator`/`StorageLayoutComputer`); a size-regression is caught by the whole guard (offsets feed the byte engine). |
| 5 | Dead-code/transitional-hack drift | No shadow fields; one canonical home per item; kill-switch reverts to byte-identical; each commit flips ≥1 complete reachable path; adversarial review per stage. |
| 6 | Aliasing: a `record struct` is copy-on-assignment, but COBOL items are reference cells | Receivers use by-`ref` discipline (ADR §1.8): `ref`-returning OCCURS indexer; groups passed `ref` or byte-backed; `ADDRESS OF`/`CALL BY REFERENCE`/LINKAGE are byte (triggers 6/11/12). |

## 8. Definition of done (this substrate)

A representative ordinary program (DISPLAY-heavy, copybook `PIC X` fields, simple MOVE/IF chains) compiles with its
records as `record struct`s of native fields, debuggable as `customer.CustName == "ACME"`; the byte engine handles
overlay/file/interop unchanged; the full guard (≥1184 / 481 / 364) is green at every commit; each post-'85 flip
ships a `tests/conformance/<ver>/` test. The Roslyn C# backend (ADR Stage 5) later emits these structs as
steppable `.cs`.

## 9. OCCURS → typed tables (S4 continuation) — implementation design

**Vocabulary reconciliation (what actually shipped).** §3/§6.1 above were authored around an `IrDataSlot`/
`TypedFieldSlot` sum type. The flips that landed (DEVLOG 403–420) instead **re-targeted the existing `IrLocation`
hierarchy** per ADR §9: a flipped standalone/member item is an **`IrTypedFieldLocation`** (`FieldName`, `Width`,
`Pic`, optional `InstanceName` for a record-struct member); the Binder records the flip in `TypedFieldRefs`;
`CilEmitter` emits a static field (`string`/`long`/`decimal` via `TypedFieldClrType`); the cells dispatch on
`is IrTypedFieldLocation`. Numeric byte-identity is achieved by **leaning on the byte codec at the §2.5 boundary**:
sender-materialize (`EncodeNumeric`→scratch), receiver prologue/epilogue (decode the result back), DISPLAY of a
`decimal` via `GetDisplayString`, MOVE-literal via a compile-time `Encode→Decode` round-trip. OCCURS extends this
same model to an *indexed* location — no new substrate philosophy, only a new location shape.

### 9.1 Representation

A **fixed** `OCCURS n` (no `DEPENDING ON`) over a *flippable elementary* element (the same char / unsigned-integer /
signed-scaled rules as a standalone item, every element sharing one PICTURE) → a typed **.NET array** static field on
the program type: `string[n]` / `long[n]` / `decimal[n]` (ADR §4 sanctions `T[]` for fixed OCCURS). This mirrors the
flat typed field exactly, one indirection deeper. `CobolTable<T>` (the `[InlineArray]`-backed richer wrapper) is
deferred — a plain `T[]` is byte-identical for fixed tables and avoids a new runtime type; revisit only if/when ODO
or whole-table value semantics need it.

Out of scope for the first OCCURS slices (stay **byte**-backed, untouched): `OCCURS DEPENDING ON` (ADR trigger 15
for whole-group operands; element-level typed access can come later), tables whose element is a **group** (needs the
record-struct-array combo — after nested groups), multi-dimensional OCCURS, `REDEFINES` over a table, `INDEXED BY`
with `SET`/`SEARCH` on a typed table (index arithmetic), and any element reached by a byte trigger.

### 9.2 New IR — a shared typed-location base

Introduce an abstract **`IrTypedLocation : IrLocation`** carrying the common `Width` + `Pic` (and the derived
`IsDecimalNumeric`). Two concrete subclasses:
- `IrTypedFieldLocation` (existing) — flat field or record-struct member.
- **`IrTypedElementLocation`** (new) — `ArrayFieldName`, `Index` (an `IrExpression`, already lowered to a **0-based**
  element index — COBOL's 1-based subscript minus one), plus `Width`/`Pic`. (Record-struct *member* arrays and
  multi-dim come later; v1 is a flat program-level array.)

The 18 current `is IrTypedFieldLocation` dispatch sites become `is IrTypedLocation` where the logic is
shape-agnostic (everything that only reads `Width`/`Pic`/`IsDecimalNumeric` — i.e. the materialize encode/decode,
COMPARE, arithmetic prologue/epilogue, DISPLAY formatting), and the **three** value-access primitives gain an
element arm:
- `EmitTypedFieldValueLoad` → load the element: `ldsfld array; <emit Index>; ldelem.ref` (string) / `ldelem.i8`
  (long) / `ldelem` (decimal, or `ldelema; ldobj`).
- `EmitTypedStorePrefix` → push container addressing *before* the value: `ldsfld array; <emit Index>` (for `stelem`)
  / `ldelema` (for a `decimal` `stobj`, if needed).
- `EmitTypedStoreSuffix` → the store op: `stelem.ref`/`stelem.i8`/`stelem`/`stobj`.

With those three generalized, **every existing numeric/char cell works on an array element unchanged** (materialize,
COMPARE, arithmetic, DISPLAY, MOVE-literal, field↔field MOVE) — because they are all expressed in terms of those
three primitives plus `Width`/`Pic`. This is the crux that makes 18 sites tractable: generalize the *primitives*,
not each cell.

### 9.3 Binder + resolver + init

- **Classifier — a new demotion (REQUIRED, discovered DEVLOG 421).** A flipped `T[]` has **no byte home** for a
  *whole-table* operand (`MOVE TABLE TO …`, `DISPLAY TABLE`, `INITIALIZE TABLE`, a group MOVE that spans it, etc.).
  Today the classifier has no OCCURS/whole-operand trigger and the Binder excludes all OCCURS items, so this never
  arose. Slice 1 adds a `ProcedureScanner` demotion: **a non-subscripted (whole) reference to an `OCCURS` item — or
  any reference to a group that transitively contains one — demotes that item to byte.** So only tables that are
  *exclusively element-accessed* (`ARR(i)`) flip; any whole-table use keeps the entire table byte-backed (the byte
  engine handles it unchanged, exactly as today). This mirrors the whole-group-operand rule (ADR trigger 15) and
  keeps slice 1 free of a whole-array↔byte materialize cell (deferred until a real case needs it).
- **Binder.** A new branch: an elementary item with `Occurs is { DependingOnSymbol: null }`, flippable element PIC,
  classifier-Typed → register an `IrTypedArrayDef(name, elementCount, elementKind, elementInit, byteWidth)` and a
  `TypedFieldRefs` entry tagged as an array (so the resolver knows to index). Element init = the element's
  VALUE-derived value (same `Encode→Decode` round-trip for numerics; spaces for char), applied to **every** slot.
- **CilEmitter.** Emit `static T[] _T_<name>`; in `InitializeState`, `newarr` of `n` and a small init loop / unrolled
  stores setting each slot to its initial value (never `default(T)`, ADR §1.7).
- **LocationResolver.** When a subscripted reference's symbol is a typed array: lower the (single, v1) subscript to
  an `IrExpression`, subtract 1 (0-based), and return `IrTypedElementLocation`. Constant subscripts fold to a
  constant index node. Variable subscripts carry the lowered expression. (The existing byte path — `IrStaticLocation`
  fold / `IrElementRef` — is untouched for non-flipped tables.)

### 9.4 Byte-identity argument

An element op is the flat-field op with the array slot selected first. The per-element value is stored/loaded with
the **same** CLR type and the **same** codec calls as a standalone field of that PICTURE, so VALUE/MOVE/DISPLAY/
COMPARE/arithmetic are byte-identical element-by-element by the already-proven flat-field argument; array indexing
only chooses *which* element, and the 1-based→0-based adjustment happens once in the resolver. Bounds: COBOL does not
mandate runtime subscript checking by default; `T[]` raises `IndexOutOfRangeException` where the byte path would read
out-of-slot — a *divergence only on already-undefined behavior*; if a conformance case needs the byte semantics,
that table is classifier-excluded (byte) — never silently mis-indexed.

### 9.5 Staged sub-slices (each its own guard-green commit + flip test)

1. **Char element, any subscript** — `string[]`; DISPLAY + MOVE-literal + field↔field MOVE + COMPARE of `ARR(i)`.
   Lands the `IrTypedLocation` base, `IrTypedElementLocation`, the three generalized primitives, `IrTypedArrayDef`,
   emitter + resolver. (Largest commit — the scaffolding.) **DONE (DEVLOG 422).**
2. **Numeric element** — `long[]` / `decimal[]`. The cells/emitter/resolver already handle it (the primitives index
   generically, and the binder branch was trivial to extend). **BLOCKED, NOT a code problem (DEVLOG 423):** the
   byte path's **VALUE-on-`OCCURS` initialization is quirky and self-inconsistent** — a numeric `OCCURS … VALUE n`
   element shows the zero-filled image (`000`, the value *ignored*), while the same item *without* VALUE shows spaces
   (empty), and a scaled element is inconsistent across occurrences. A typed `long`/`decimal` init cannot be made
   byte-identical to that without first **reconciling (most likely fixing) the byte-path `OCCURS`+VALUE init
   semantics** — which is a byte-engine correctness question (`feedback_diff_is_a_bug`), to be settled before the
   numeric-table flip. (Char tables are unaffected: the byte path inits `OCCURS` char to spaces, which the typed
   spaces-init matches exactly — verified DISPLAY + `= SPACES` compare.) The `ClassifyTypedNumeric` core (shared by
   the standalone long/decimal predicates) is already in place for when this unblocks.
3. **PERFORM VARYING / SEARCH over a typed table** — index-driven loops; verify byte-identity with a varying subscript.
4. **(later)** record-struct element (group table), multi-dim, ODO element access, `INDEXED BY`.

## 10. Stage-4 pointers — implementation design (managed references, ONE carrier)

**Settled (owner directives, DEVLOG 428–429):** pointers are **managed .NET references** (no native heap, no handle
table, **no `unsafe`**); the **`PointerRegistry` is REJECTED**; and there is **exactly ONE carrier type** — the
existing **`ManagedPointer(byte[] Buffer, int Offset, int Length, PicDescriptor Pic)`** — used for BOTH
`CALL … BY REFERENCE` and `USAGE POINTER`/`ADDRESS OF`/`BASED`. **Do NOT add a parallel `ManagedPointer`**
([[feedback_singular_pattern]]); the ADR's "CobolDataPointer → ManagedPointer (rename) (Stage 4)" is a *migrate-to-one-type*, and
since `ADDRESS OF` demotes its target to byte (classifier **trigger 6**), a pointer's owner is **always** a managed
`byte[]` — so `ManagedPointer`'s `byte[] Buffer` is exactly correct (the ADR's `object? Owner` is unnecessary).
Deref = `Buffer.AsSpan(Offset, Length)` → the existing byte-path runtime ops. Safe, bounds-checked, GC-transparent.

### 10.1 Current state (investigation, DEVLOG 429)
- `USAGE POINTER` Phase-1 = an **8-byte byte-window handle** inline in WS (NOT a managed ref); `SET p TO NULL/q` =
  byte MOVE; `= NULL`/`= q` = 8-byte ordinal compare. `ManagedPointer` is a *separate* CALL-arg carrier today.
- `SET ADDRESS OF … TO …` **parses** (`setAddressStatement`, `CobolParserCore.g4:846`) but **BindSet has no branch**
  → silently dropped. `ADDRESS OF` as a *sender* has **no grammar**. `SET … UP/DOWN BY` parses (index path).
- `BASED` **lexes as a generic noise word** (swallowed by `genericDataClause`) — and the item **wrongly gets WS
  storage** (`StorageLayoutComputer.LayoutElementary`). Must become a real clause + `IsBased` flag + **skip layout**.
- Classifier **trigger 6** (ADDRESS OF → byte) is deliberately deferred (unreachable until SET ADDRESS OF binds).
- `ALLOCATE`/`FREE` — no tokens, no grammar, unimplemented.

### 10.2 Representation — ONE form: always `ManagedPointer`, never an 8-byte byte handle
**Singular pattern (owner directive, DEVLOG 431):** a pointer value (`USAGE POINTER`, a `BASED` item's address, an
`ALLOCATE` result) is **ALWAYS** a `ManagedPointer` — there is **no** 8-byte byte-handle representation. The Phase-1
8-byte handle was a placeholder and is **eliminated**, replaced everywhere by `ManagedPointer` (NULL = `default`/
`!IsValid`, `SET` = struct copy, `=` = equality — the `pointer_data` conformance test stays green). Pointers are
**always-typed, NOT gated by `EnableTypedFields`** (a managed reference is the only correct representation; the flag
gates the *other* typed flips where byte-identity matters).

Crucially the usual typed/byte duality (the §2.5 byte-materialize floor) **does not apply to pointers**: a
`ManagedPointer` **cannot be serialized to stable bytes** (the `Buffer` reference is GC-relocatable), so there is no
byte image to fall back to — and fabricating an 8-byte handle would be a *second representation that can't even hold
a real pointer* (the dual pattern we reject). The cases that would have needed a byte image are all
**implementor-defined / non-portable** in ISO, so we do **not** synthesize a byte handle for them:
- **REDEFINES** a pointer ↔ bytes — type-punning a managed ref is meaningless → **reject** (diagnostic) / undefined.
- **Pointer written to a file** — a serialized address is non-portable → same.
- **`CALL … BY REFERENCE` of a pointer item** — the callee aliases the caller's `ManagedPointer` *field* (a managed
  alias), not a byte window.
- **EXTERNAL pointer** — a shared `ManagedPointer` field.

`LENGTH OF` a pointer / a group's size may still report the **logical** implementor-defined 8 without an 8-byte
storage slot. A `BASED` item has **no storage**; its references deref through its associated `ManagedPointer` via a
new **pointer-relative `IrLocation`** (load the pointer's `Buffer`+`Offset`, form the window) — `IrStaticLocation`
hard-binds a fixed area+offset, so a new location kind is required.

### 10.3 Grammar (approved DEVLOG 429) + bind/lower/emit
- Lexer: add `BASED`, `ALLOCATE`, `FREE` (+ `INITIALIZED`, `CHARACTERS`, `RETURNING` as needed); `ADDRESS`/`NULL_`/
  `POINTER` exist. Data clause: `basedClause : BASED` (level 01/77) → `DataSymbol.IsBased`; `StorageLayoutComputer`
  skips allocation for `IsBased`. Statement list: `{is2002()}? allocateStatement | freeStatement`.
- `SET ADDRESS OF based TO ptr`: add the missing `ctx.setAddressStatement()` branch in `BindSet` → new
  `BoundSetAddressStatement` → store the pointer as the based item's base.
- `SET ptr TO ADDRESS OF x`: add `ADDRESS OF id` as a SET *sender* (new grammar alt) → `BoundAddressOfExpression` →
  build a `ManagedPointer` over x's region. (Reuse `ManagedPointer.CreateByReference`.)
- `SET ptr UP/DOWN BY n`: in `BindSetIndex`, branch on `Category==Pointer` → pointer-offset add/sub (not the numeric
  `IrPicAdd`).
- `ALLOCATE {expr CHARACTERS | based-item} [INITIALIZED] [RETURNING ptr]` (ISO §14.9.3) = a `ManagedPointer` over
  `new byte[n]`; `FREE ptr…` (ISO §14.9.15) = set the pointer to NULL (drop the ref; GC reclaims; `EC-STORAGE-NOT-ALLOC`
  later). Wire end-to-end like the recent `deleteFileStatement` (grammar→regen→BoundTreeBuilder→bind→Binder switch→
  lower→emit). **Generated parser is committed — regenerate from the .g4.**
- Classifier **trigger 6**: add a `BoundSetAddressStatement` / `BoundAddressOfExpression` case in `ProcedureScanner`
  → `Demote.Add(addressed-item)` so the target stays byte (keeping `Buffer` a `byte[]`).

### 10.4 Staged slices (each guard-green + a `tests/conformance/2002/` test)
1. **`BASED` + `ADDRESS OF` + `SET ADDRESS OF` + deref** — the core loop (`SET p TO ADDRESS OF x` / `SET ADDRESS OF b
   TO p` / `DISPLAY b`). Lands the lexer/grammar/regen, `IsBased` + layout skip, `BoundAddressOfExpression` +
   `BoundSetAddressStatement`, the pointer-relative `IrLocation`, trigger 6, and the typed `ManagedPointer` pointer
   field. (Largest — the scaffolding.)
2. **Pointer arithmetic** — `SET p UP/DOWN BY n`, pointer relations.
3. **`ALLOCATE` / `FREE`** (+ `INITIALIZED`, `RETURNING`) — heap-less managed allocation.
4. **(later)** `PROGRAM-POINTER`/`FUNCTION-POINTER`, `CALL … BY REFERENCE` migrated onto the same `ManagedPointer`
   path (already is), pointer in a typed record/table.

### 10.5 Current-state surface map — the EXACT sites slice 1b rearchitects (confirmed DEVLOG 431)
The Phase-1 pointer = an **8-byte byte handle inline in WS**. To make it the single `ManagedPointer` field, touch:
- **Storage / sizing** — `Semantics/FieldSizeCalculator.cs:28` (`if (data.Usage == UsageKind.Pointer)` → 8). A pointer
  no longer occupies WS bytes; it becomes a per-item `static ManagedPointer` field. Keep an 8 only as the *logical*
  `LENGTH OF` value (not a storage slot). `PicUsageResolver.cs:52` sets `Category=Pointer` (keep). A standalone
  `USAGE POINTER` is already `IsElementary` (`DataSymbol.cs:126`).
- **Classifier** — `Semantics/DataItemClassifier.cs:175` (`Category==Pointer`). Make a pointer **always-typed**
  (never byte-demoted) — pointers are NOT gated by `EnableTypedFields`. Add **trigger 6**: a `BoundAddressOfExpression`
  on item X demotes X (not the pointer) to byte so X keeps a `byte[]` Buffer.
- **Pointer-field pass** — a NEW always-on pass (sibling to the gated `Binder.CollectTypedFields`, `Binder.cs:278`):
  for every `USAGE POINTER` item AND every `IsBased` item, register a `_PTR_<name>` `ManagedPointer` field
  (`IrModule.PointerFieldDefs` + `LoweringContext.PointerFieldRefs`). `CilEmitter` emits `static ManagedPointer
  _PTR_<name>` (defaults to NULL = correct initial value, so no explicit init — the one place `default(T)` is right).
- **SET dispatch** — `Semantics/Bound/Binding/DataStatementBinder.cs`: `BindSetObjectReference` (`:187-204`, currently
  `SET p TO NULL/q` → `BoundMoveStatement` byte) and `BindSetToValue` (`:301`, `SET p TO q`) must emit
  pointer-field ops, NOT a byte MOVE. Add the `setAddressStatement` branch in `BindSet` (`:160-184`) for both
  `SET ADDRESS OF b TO p` and `SET p TO ADDRESS OF x` (grammar already lands per slice 1a).
- **Compare** — the comparison subsystem currently does an 8-byte ordinal char compare: `ConditionLowerer.cs:125`
  maps `FigurativeKind.Null → '\x00'` so `IF p = NULL` becomes "p's 8 bytes vs 8 NUL bytes." Reroute a
  **pointer-category relation** to a pointer compare: `p = NULL` → `!p.IsValid` (Buffer==null); `p = q` →
  `ReferenceEquals(p.Buffer, q.Buffer) && p.Offset == q.Offset` (**address identity — NOT** record-struct `Equals`,
  which also compares `Length`/`Pic` and would wrongly distinguish two pointers to the same address typed differently).
  Both NULL: `ReferenceEquals(null,null)=true`, `0==0` → equal. ✓ Find the relation lowering near `ConditionLowerer.cs`
  + `CategoryCompatibility.cs:75` (`(Pointer,Pointer)`).
- **Dead paths to DELETE once rerouted** (no other consumer — only `pointer_data` uses pointers, corpus-wide):
  `DataMovementLowerer.cs:303` (`IsPointerLike()` INITIALIZE skip — keep; that's correct), the pointer-MOVE branch
  `CilDataEmitter.cs:616` (`IsPointerLike` → `MoveAlphanumericToAlphanumeric` 8-byte handle copy) and the
  `StorageLocation.cs:190-210` pointer `PicDescriptor` synth (an 8-byte char slot) — both exist only to serve the
  handle and go away when SET/compare move to `ManagedPointer`.
- **Blast radius (confirmed DEVLOG 431):** grep of the whole test corpus → **exactly one** program uses pointers
  (`tests/conformance/2002/pointer_data.cob`). So the first boundary is fully verifiable against that test + the guard;
  there is no other pointer consumer to regress. (CALL `BY REFERENCE` uses `ManagedPointer` already but never via a
  `USAGE POINTER` data item.)
- **New IR**: `IrPointerStore { PtrField; Kind = Null | FromPointer | FromAddressOf; Source }`,
  `IrPointerCompare`, and an `IrBasedDerefLocation { PtrField; Length }` (a new `IrLocation` kind — `IrStaticLocation`
  hard-binds area+offset). Deref emit: `ldsflda _PTR_b; ldfld Buffer` (→ `byte[]`), `ldsflda _PTR_b; ldfld Offset`
  (→ int), `ldc Length` → the existing byte-path runtime ops consume the (`byte[]`,offset,len) triple.
- **First commit boundary (guard-green)**: storage + classifier-always-typed + the pointer-field pass + `SET`/compare
  reimplemented on `ManagedPointer` → the existing `pointer_data` conformance test stays green (observably identical:
  NULL/SET/=); the 8-byte handle code is then deleted. THEN `ADDRESS OF`/`SET ADDRESS OF`/BASED-deref + a new
  `based_pointer` conformance test (`SET p TO ADDRESS OF x` / `SET ADDRESS OF b TO p` / `DISPLAY b`).
