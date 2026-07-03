# COBOL.NET — Consolidated Design (SSOT)

> **Status: LIVE / authoritative.** This is the single, internally-consistent source of truth that the COBOL.NET
> implementation follows. It synthesizes the 11 subsystem designs (pipeline, data-model, numeric, control-flow,
> redefines/renames, string-ops, files, interprogram, OO, conditions/exceptions, intrinsics/registers) into one
> coherent design, reconciling every cross-subsystem conflict. Where two subsystem designs disagreed, the loser is
> named explicitly (see §14, Cross-Cutting Consistency). It refines `docs/COBOLNET_ARCHITECTURE.md` (the thin
> overview) and supersedes any contradicting statement in it (notably the §3 `decimal` rows — see §3.1 below).
>
> **Project:** COBOL.NET — a greenfield compiler translating COBOL → idiomatic, typed-native **C# source**, compiled
> by **Roslyn**. New compiler in `src/Cobol.Net.Compiler` + `src/Cobol.Net.Cli` (exe `cobol`) + `src/Cobol.Net.Runtime`;
> the reused front-end (ANTLR lexer/parser/preprocessor) is extracted into `src/Cobol.Net.Frontend` (§17/G0 — DONE;
> namespaces stay `CobolSharp.Compiler.*` until G8). The legacy byte-array implementation is rejected, kept
> only as a differential **behavioral oracle** (it passes 364 NIST tests) until cut-over (G8).

---

## 0. Reading guide

- **§1** Architecture overview + the locked invariants.
- **§2–§13** One section per subsystem: key decisions (with rationale + rejected alternative), the C# mapping, the
  hard problems/edge cases. Condensed — but every real decision survives.
- **§14** The CROSS-CUTTING CONSISTENCY pass: the unified lvalue model, control-flow/declaratives/EXIT interaction,
  REDEFINES × numeric × files, the runtime class roster, and every named conflict resolution.
- **§15** Consolidated OWNER-LEVEL open questions (deduped; the task-named ones verbatim).
- **§16** Dependency-ordered implementation sequence (G1–G8), each step NIST-testable, with the cross-design
  prerequisites surfaced.
- **§17** Project organization / rename / structural discipline (the G0 plan — executed).
- **§18** Settled decisions (the §15 questions, owner-resolved 2026-06-08).

---

## 0.5 Subsystem deep-dive docs

Each subsystem section in THIS document is a **condensed** view kept consistent across subsystems. The full,
decision-complete **deep dive** for each lives in its own doc (decisions + rationale + rejected alternatives + the
C# mapping with worked examples + hard problems + edge cases + ISO citations):

| § | Subsystem | Deep-dive doc |
|---|---|---|
| §2  | Pipeline & emitter architecture | `docs/COBOLNET_PIPELINE_DESIGN.md` |
| §3  | Data model (records/tables/refs) | `docs/COBOLNET_DATA_MODEL_DESIGN.md` |
| §4  | REDEFINES / RENAMES | `docs/COBOLNET_REDEFINES_DESIGN.md` |
| §5  | Control flow (PC dispatcher) | `docs/COBOLNET_CONTROL_FLOW_DESIGN.md` |
| §6  | Numeric model (scaled-integer) | `docs/COBOLNET_NUMERIC_DESIGN.md` |
| §7  | String operations | `docs/COBOLNET_STRING_OPS_DESIGN.md` |
| §8  | Files | `docs/COBOLNET_FILES_DESIGN.md` |
| §9  | Interprogram (CALL/cross-program) | `docs/COBOLNET_INTERPROGRAM_DESIGN.md` |
| §10 | OO → .NET classes | `docs/COBOLNET_OO_DESIGN.md` |
| §11 | Conditions & exceptions | `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` |
| §12 | Intrinsics, registers & misc | `docs/COBOLNET_INTRINSICS_DESIGN.md` |
| §14 | Report Writer (RWCS) | `docs/COBOLNET_REPORT_WRITER_DESIGN.md` |
| §17 | Project organization & rename | `docs/COBOLNET_PROJECT_ORG_DESIGN.md` |

This SSOT stays authoritative for the **locked invariants (§1)**, the **cross-cutting consistency (§14)**, the
**settled decisions (§18)**, and the **build order (§16)**; the deep-dives are authoritative for their subsystem's
internal detail. Where a deep-dive conflicts with §1/§14/§18, the SSOT wins.

---

## 1. Architecture overview + locked invariants

### 1.1 Pipeline (5 phases; a backend-neutral bound tree feeds a SELECTABLE backend)

```
source.cob
  → Frontend  (REUSED ANTLR: Preprocess[reference-format, >>directives, COPY, NIST placeholders] → Lex → Parse)
       → parse tree
  → Bind      (resolve symbols + build a typed/categorized BOUND TREE that PRESERVES COBOL structure)
       → BoundProgram
  → Desugar   (bound→bound passes: MOVE CORRESPONDING, PERFORM VARYING…AFTER, condition-name lowering)
       → BoundProgram (same tree type)
  → Backend   (ICodeGenBackend, SELECTABLE via --backend):
       ├─ Roslyn (default): decomposed emitter → idiomatic C# (.g.cs) → CSharpCompilation → assembly + PDB
       └─ CIL (Cecil):      bound tree → typed-native CIL (its OWN internal structure→branch lowering) → assembly
```

**Backend-neutral bound tree + a selectable codegen backend (owner-confirmed 2026-06-08).** The bound semantic tree
is the single model that both backends consume; codegen is behind an **`ICodeGenBackend`** abstraction with two
implementations, chosen by `--backend roslyn|cil` (default **roslyn**):
- **RoslynBackend (primary):** maps the bound tree's COBOL structure ~1:1 to idiomatic, readable **C# source**
  (`IF`→`if/else`, `EVALUATE`→if-chain, inline `PERFORM`→loops, the PC-dispatcher→`while(true)switch`), then compiles
  it with Roslyn. This is the "best native .NET implementation" output and the v1 deliverable.
- **CilBackend (selectable, future-additive):** emits **typed-native CIL** directly via Mono.Cecil — for callers who
  want no C#-compile step / no Roslyn dependency / direct IL (AOT, faster compile). Because CIL is unstructured, this
  backend does its OWN internal lowering of structured constructs to branches; that lowering is **private to the CIL
  backend, NOT a shared phase**.

**There is NO *shared* lowered IR.** The legacy basic-block IR existed only because its sole target was CIL; here the
backend-neutral bound tree replaces it, and each backend lowers only as far as its target needs (Roslyn: none —
structure is preserved; CIL: branch-level, internally). *(Rejected: the current direct parse-tree walk —
`CSharpEmitter.Resolve` handles only unqualified refs, `RenderCondition` falls back to `"false"`, scale/category
re-derived per call-site; and a shared branch IR — it would re-impose the CIL-shaped lowering on the C# path and
destroy its readable output.)* Bonus: the differential harness (§2) can cross-check the two backends against each
other, not just against the legacy oracle.

### 1.2 The four OWNER-LOCKED invariants (design *within* these; never relitigate)

1. **No byte-array storage substrate.** A COBOL record IS a .NET `record struct`; an elementary item IS a native
   .NET field. No `ProgramState`, no `byte[]`-at-offset, no `(byte[],offset,length)`. A byte image exists only
   *transiently* at an unavoidable boundary (file I/O / `CODE-SET`, a runtime API that needs bytes, a genuine
   REDEFINES byte-pun), built into a fresh scratch buffer, never persisted as program data.
2. **Numerics are native.** Fixed-point → a native integer holding the **unscaled** value (all digits; the decimal
   point is compile-time scale metadata). ≤18 digits → `long`; 19–38 digits → `Int128`; `COMP-1`/`COMP-2` →
   `float`/`double`; `COMP-5` → native int by width (binary-wrap). **No `decimal`, no `BigInteger`.**
3. **Control flow is a single program-counter dispatcher.** Paragraphs/sections are LABELS (PC cases) in one flow,
   NOT separate methods. `GO TO` sets the PC; fall-through is PC++; a `PERFORM` range is a recursive bounded
   dispatch.
4. **Output is idiomatic, readable C# where the construct allows.** Correctness wins over prettiness for irregular
   control flow (the dispatcher backbone is deliberately a state machine; "idiomatic" applies to the *case
   contents* — `IF`→`if/else`, `EVALUATE`→if/else-if chain, inline `PERFORM`→real loops, typed field ops).

### 1.3 Two universal abstractions (the spine of internal consistency — see §14.1, §14.2)

- **`Place`** — the ONE typed-lvalue model. `Place.Read()`/`Place.Write(rhs)` are the ROSLYN backend's RENDERING of a
  backend-neutral structural resolution (item + member-accessor chain + subscript expressions + ref-mod span + 88
  value-set) — the structure, not C# text, is the bound-tree contract (G4: bound nodes carry no pre-rendered
  C#-specific fragments; the CIL backend lowers the same structure to load/store sequences). Built once by
  `ReferenceResolver`. Consumed identically by MOVE, arithmetic, INSPECT/STRING/
  UNSTRING, file READ INTO / WRITE FROM, and CALL-by-reference. There is no second lvalue type.
- **The PC dispatcher** — one `Dispatch(int startPc, int exitPc)` method per program unit; STOP RUN and GOBACK are
  the only control transfers that use exceptions (everything else is integer-pc).

### 1.4 Loud-failure invariant

(1) An unbound/unsupported construct emits a **tracked deferral diagnostic** + a runtime guard
(`throw new NotImplementedCobolFeature(...)`), never a silent `// TODO` no-op. (2) **Bind success ⇒ emit MUST
produce compilable C#** — any Roslyn error on generated code is an ICE (surfaced with the `.g.cs` path), never a user
error. This is the structural enforcement of the project's "fail LOUD" culture.

---

## 2. Pipeline & emitter architecture

**Decisions.**

- **Bound tree, not IR** (§1.1). The bound tree resolves qualified names (OF/IN), subscripts, ref-mod, and
  condition-names ONCE and stores them on the node — the single place to hang semantic diagnostics.
- **Single PC dispatcher per program unit** (§5 has the full design). `int Dispatch(int startPc, int exitPc)`.
- **Decomposed emitter over a shared `EmitContext`** (CodeWriter + bound model + `NameAllocator` + DialectMode +
  DiagnosticBag + EmitConfig): `CSharpEmitter` (orchestrator), `DataEmitter`, `DispatchEmitter`, `StatementEmitter`,
  `ExpressionEmitter`, `ConditionEmitter`, `ProgramEmitter`. Mirrors the legacy `Emission/` split (the legacy
  `CilEmitter` hit 2458 lines before it was split — direct evidence). *(Rejected: one growing `CSharpEmitter` god
  class; visitor double-dispatch — heavier than a switch-on-bound-node-type with per-category collaborators.)*
- **One `NameAllocator`** owns C#-identifier generation: case-insensitive normalization (COBOL `FOO`==`foo`);
  namespace segregation by prefix (COBOL data `d_`; temporaries `__t`; dispatcher locals a fixed reserved set
  `{pc, startPc, exitPc, Main, Dispatch}`); **paragraphs get no identifier — they are pc indices**, so an entire
  identifier-collision class vanishes; allocate-once-store-on-symbol; deterministic `_2`/`_3` disambiguation.
  *(Rejected: the current per-call `Sanitize` — not collision-safe: hyphen non-injective `A-B`/`A_B`, case-sensitive,
  no dedup; hash-suffixing — destroys readability.)*
- **Binder scope tree** for multiple/nested/contained programs (GLOBAL/COMMON/EXTERNAL are lexical-nesting
  questions). The emitter walks ALL program units, not `FirstOrDefault()` (the current bug).
- **Differential conformance harness:** `ICompilerUnderTest { Compile+Run(src, dialect, nist?) }` with `LegacyCompiler`
  and `CobolNetCompiler` impls; `DifferentialNistTests` asserts `CobolNet stdout == Legacy stdout == nist/valid/*.txt`.
  The legacy's 364 passing programs become an instant regression net; the `.txt` oracle backstops a shared bug. Reuse
  the proven `guard-fast` parallelism; run each program in an ISOLATED working dir (file producer/consumer chains).
- **One typed `DialectMode` enum** (`Cobol85/2002/2014/2023`) threaded CLI → Frontend (grammar admit/reject via
  `{isXXXX()}?` gates) → Binder (semantic gating AND flagging). Keep the legacy two-axis (version × strictness) model.
  Default `--std` = COBOL-2023; `--nist` without an explicit `--std` targets 85. ⛔ Per-edition gating is a co-equal
  obligation: each construct must compile + behave per the spec in every edition that HAS it AND draw the correct
  diagnostic in every edition that LACKS it (not-yet-introduced or removed) — driven by
  `docs/VERSION_CHANGE_REFERENCE.md` (130-row edition-change checklist) and validated by
  `docs/VERSION_TEST_MATRIX_DESIGN.md` (the construct × edition matrix).
- **Deploy:** fixed TFM from `Directory.Build.props` — `net10.0` today, with a **.NET 11 upgrade pre-authorized**
  when its features make the goals easier/more productive (owner, 2026-06-10); never `Environment.Version`-by-luck;
  one ConsoleApplication
  assembly per compilation (the run-unit); emit a PDB mapped to the `.g.cs`; write `.g.cs` next to the assembly.

**Hard problems** are all control-flow/data and live in §5/§3. Pipeline-level: disable nullable + suppress unused-var
noise on generated C# so the compiler project's warnings-as-errors doesn't reject valid generated code; verify the
preprocessor (`>>IF`/`>>DEFINE`, COPY REPLACING, NIST placeholders) runs BEFORE binding.

---

## 3. Data model

### 3.1 The substrate (LOCKED — and the one doc correction)

`PicInfo.ClrType` already maps fixed-point → `long`-unscaled (no `decimal`). **The `docs/COBOLNET_ARCHITECTURE.md`
§3 table rows that say scaled/`COMP-3` → `decimal` are SUPERSEDED by this document and by `PicInfo.cs`:** scaled and
`COMP-3` are `long`-unscaled (scale is compile-time metadata); 19–38 digits → `Int128` via a `WidePrecision` flag.
This correction must be applied to the architecture doc in the same change set (no two SSOTs).

### 3.2 Shape mapping

| COBOL | .NET |
|---|---|
| `PIC X(n)` / `A(n)` / `PIC N(n)` (national) | `string` (UTF-16) |
| `PIC 9(n)` / `S9(n)` (≤18 digits, any scale) | `long` (unscaled; scale = metadata) |
| `PIC 9(n)`…(19–38 digits) | `Int128` (`WidePrecision`) |
| `COMP-1` / `COMP-2` | `float` / `double` |
| `COMP-5` / `BINARY-*` | native int by width (`sbyte…ulong`/`Int128`), binary-wrap |
| numeric-edited (`Z * $ + - CR DB B 0 /`) | `string` (the formatted display image) |
| `PIC 1` / `USAGE BIT` | `bool` |
| `01`/group | nested `record struct` named `_T_<csname>` |
| fixed `OCCURS n` | `T[]` (length n) |
| `OCCURS m TO n DEPENDING ON d` | `T[]` allocated at MAX (n) + the length field `d` bounds the live range |
| `USAGE POINTER` / `BASED` / `ADDRESS OF` | `ManagedPointer` (managed ref — see §14.2 / §9) |
| `USAGE OBJECT REFERENCE [class]` / OO class | `class?` (typed) or `object?` (universal) / a .NET class |

Member access falls straight out: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2 - 1].Val`.

### 3.3 The `Place` abstraction (the universal lvalue — §14.1)

```csharp
abstract record Place { DataItem Item; PicInfo Pic; /* structural core: accessor chain, subscripts, ref-mod */ }
// G4 note: the string Read()/Write(rhs) shown below are the RoslynBackend's RENDERING of this structure (C# text);
// the CIL backend lowers the SAME structural Place to load/store sequences — no C# fragments live in bound nodes.
// MemberPlace(path)        Read = path                Write = $"{path} = {rhs};"
// RefModPlace(inner,s,l)   Read = CobolString.RefMod(inner.Read(), s, l)
//                          Write = { var t=inner.Read(); inner.Write(CobolString.SpliceInto(t,s,l,rhs)); }
// Condition88Place(parent, valueset)   Read = CobolCond.In(parent.Read(), …)   Write(true) = set parent to value
```

One resolver — `ReferenceResolver.Resolve(DataReferenceContext) → Place` — is the single entry point for every operand.

### 3.4 The two-phase resolver (the grammar forces it)

The grammar gives `dataReference : cobolWord dataReferenceSuffix*`, and subscript/ref-mod content is a RAW
`subToken+` stream (SUBSCRIPT lexer mode); `(I J)` (subscripts) and `(3:2)` (ref-mod) are syntactically identical —
**the presence of `SUB_COLON` decides.** So:

- **Phase A (syntactic flatten):** walk suffixes into `{qualifiers[], subscriptGroups[][], refMod?}`. A `(...)` group
  is a ref-mod iff it contains `SUB_COLON`; else split on `SUB_WS`/`SUB_COMMA` into N subscripts. **Port the legacy
  `ExpressionBinder` SUB_* token interpreter verbatim** (proven over 364 NIST tests).
- **Phase B (semantic resolve):** resolve base + qualifiers via right-to-left narrowing (port `ResolveQualifiedName` +
  `FindChild`), build the member-access Place, attach subscripts to OCCURS levels (outer→inner), wrap in ref-mod if
  present. Subscripts/positions evaluated ONCE into temps (ISO single-evaluation).

### 3.5 Key data decisions

- **Multi-dimensional OCCURS → array-of-struct-containing-array** (`Rows[i-1].Cols[j-1]`), one C# index per COBOL
  subscript (1-based → `[expr - 1]`). NO flattened 1-D buffer, NO `stepSize` multipliers. *(Rejected: the legacy
  flattened-1-D-with-multipliers model — it IS the byte-offset arithmetic the owner abolished.)*
- **Index-names (`INDEXED BY`) → a C# `long` holding a 1-based OCCURRENCE NUMBER**, not a byte displacement. `SET TO`
  → assign; `SET UP/DOWN BY k` → `±= k`; subscript use → `[idx - 1]`; SEARCH/SEARCH ALL → integer loops. *(Rejected:
  the legacy byte-displacement index — leaks table-element width into a program-visible value.)*
- **Level-88 → C# `bool` properties** over the parent Place (`Ok => St == 1`); SET cond TO TRUE moves the 88's first
  VALUE (low bound of a THRU range) into the parent (ISO §14.9.34/§14.9.39). The binder must **stop skipping 88s**
  (currently dropped) and capture the full multi-literal + THRU value list. *(Rejected: stored bools kept in sync on
  every parent write — redundant state, sync bugs.)*
- **Level-66 RENAMES** folds into the REDEFINES/RENAMES tiers (§4) as a COMPOSED view over existing fields (adds no
  storage). The binder must **stop skipping 66s.**
- **VALUE init** is one recursive object-initializer composed from the leaves, emitted in the static field decl
  (program) or the instance ctor (OO). Extensions: group VALUE, OCCURS VALUE (`Tbl = [.. n elements]`), figurative
  constants (§11). `DataBinder.ExtractValue` currently grabs only `FirstOrDefault` — a bug for 88s and table VALUEs.
- **`ByName` becomes a MULTIMAP** (`Dictionary<string,List<DataItem>>`) — COBOL permits duplicate names disambiguated
  only by qualification; the current single-value Dictionary silently overwrites (latent wrong-item bug).
- **SYNCHRONIZED is a no-op for in-memory typed data** (the CLR aligns a `long` naturally); honored only at the
  byte-serialization boundary (files / REDEFINES Tier-C). **JUSTIFIED RIGHT** and **BLANK WHEN ZERO** are
  display/store-time rules on `PicInfo`. (`DataItem` must gain `IsJustifiedRight`, `IsSynchronized`, `BlankWhenZero`,
  `RedefinesName`, `RenamesInfo`, `WidePrecision`, the 88 value-set, and figurative/ALL-pattern fields.)

### 3.6 Hard problems / edge cases

- **REDEFINES** is a byte-overlay with no clean typed form — resolved by the 4-tier model in §4 (this section just
  captures the clauses; §4 owns the semantics).
- **Ref-mod as a RECEIVER:** C# strings are immutable → `CobolString.SpliceInto(field, s, l, value)` rebuilds the
  string. (§7 owns the string runtime.)
- **OCCURS DEPENDING ON:** array at MAX; the length var bounds the live range; whole-group sending uses `[0..N)`,
  receiving uses MAX (ISO OCCURS GR7 — the ST146A lesson).
- **Whole-group-as-alphanumeric:** a group has no scalar field — its image is the concatenation of leaf display
  images via a generated `string AsImage()` per struct (THE single whole-group image facility — see §14.4). This is
  the G6 boundary; the hook is designed now.
- **FILLER:** a synthetic `_fillerN` member only when it has a VALUE or affects group serialization.

---

## 4. REDEFINES / RENAMES (the storage-overlay subsystem)

> **Conflict resolved (named loser): data-model §8's "independent typed fields, a write to one is not visible in the
> other" is SUPERSEDED.** That phrasing is the *incoherence trap* — it reproduces the exact silent-stale-read that
> triggered the DEVLOG 457 pivot. The authoritative model is the 4-tier **one-canonical-backing** model below. (See
> §14.3.)

### 4.1 The spine

Two differently-typed C# reps over one storage stay coherent with NO shared `byte[]` only if **they do not both
exist**: a "redefines class" (all entries sharing a storage area) has exactly ONE stored backing — the *canonical* —
and EVERY other view is a **computed accessor (a C# property)** over it. Never two stored fields per storage area.

### 4.2 The 4 tiers (priority cascade D > C > B > A; lattice A ⊑ B ⊑ C ⊑ D, join = max tier)

- **A — Alias** (identical PIC+USAGE, or RENAMES without THRU): one typed field; other names are pass-through
  properties (`WS_COUNT_ALIAS { get => WS_COUNTER; set => WS_COUNTER = value; }`).
- **B — StringCanonical** (whole class is character-imageable — alphanumeric/edited/alphabetic, DISPLAY-numeric,
  **and fixed-point BINARY/PACKED**, per the §14.4 digit-image representation): canonical = ONE `string` of class-max
  width (a DISPLAY item's byte image IS its characters; a fixed-point COMP/COMP-3 leaf's window IS its zoned digit
  image — ISO §13.18.60 USAGE GR4 makes the representation, including the sign, implementor-defined); each view = a
  typed accessor (substring / parse-digits→long / format) over it. NO bytes. An image-stored BINARY/PACKED leaf has
  its `Pic.SignKind` REWRITTEN to `TrailingOverpunch` at classification (DataBinder) so every accessor that threads
  its `_P_` profile describes the zoned window — `BinaryMinus` is variable-width and would corrupt the fixed window
  (the observable consequence: DISPLAY of such a leaf shows the zoned overpunch image, the conformant face of the
  GR4 license — Phase 1E / ST134A). **This is the dominant real case and covers the ENTIRE NIST path** (incl.
  SAME RECORD AREA / multi-01 FD classes with COMP leaves).
- **C — ByteCanonical** (puns observing a representation with NO fixed character-digit image cross-view: float
  COMP-1/COMP-2, COMP-5 — whose `BinaryCapacity` discipline stores values exceeding the PICTURE digit count, which a
  Digits-wide window cannot carry — and INDEX): canonical = ONE *class-scoped* `byte[]` of class-max width
  (SYNC-aware offsets, from a ported `StorageLayoutComputer`); each leaf = a typed get/set accessor over
  `(offset,length,usage)` via a small `RedefCodec` runtime helper. Byte image confined to the class, never the
  record, never persisted further. The byte[] is the PERSISTENT canonical (not materialize-on-demand — distinct from
  §14.4's transient whole-group image). NOT YET IMPLEMENTED — verdicts Rejected (loud) in the interim; the Phase-1E
  narrowing moved the decimal-digit usages (BINARY/PACKED) out of this tier into B, where they have an exact
  character representation.
- **D — Reject loud** (spec-forbidden/unmodelable: object/pointer/message-tag/strongly-typed SR12/14; OCCURS
  DEPENDING ON / variable-length / dynamic-length SR5/17): a diagnostic — conformant, since these are already illegal.

Tier selection **reuses the legacy `RecordClassificationPass` transitive-closure shape** (byte propagates across the
REDEFINES class + to all subordinates; monotone; terminating), re-verdicted to the lattice.

### 4.3 Critical sub-decisions

- **Init only from the original** (REDEFINES SR9): a view emits NO stored value field and NO initializer; only the
  canonical original initializes the backing.
- **Class width = MAX storage width across all views** (SR8 exception: a level-01 non-EXTERNAL original may be
  redefined larger).
- **A numeric view still emits its `NumProfile`** (`_P_<view>`) and carries its PICTURE's natural surface type even
  though its stored VALUE field is suppressed — because `EmitArithAssign` references `_P_<CsName>`, so a numeric view
  used as an arithmetic target must compile. (Advisor correction baked in.)
- **RENAMES THRU over a heterogeneous span** composes via each leaf's EXISTING read/write: get = concat each leaf's
  DISPLAY image (`CobolNum.FormatDisplay` for a numeric leaf); set = distribute the value left-to-right by width via
  each leaf's MOVE-into path. Not raw `+` on a long.
- **Cross-type-read guard:** emit a LOUD diagnostic when a write to one view is followed (conservatively: any program
  that both writes and reads two different-typed views of one region) so nothing silently corrupts.
- **`[FieldOffset]`/`StructLayout(Explicit)` overlay is rejected outright** — cannot overlay a `string` on a `long`;
  the dominant pun is alphanumeric↔numeric which it cannot express; even long-over-long overlays VALUES not the byte
  REINTERPRETATION COBOL means.

### 4.4 Edge cases

REDEFINES inside an OCCURS element (overlay is per element); nested REDEFINES chains (follow `RedefinesTarget`
transitively to the true anchor); signed-DISPLAY overpunch in a numeric view (needs §6's `FormatDisplaySigned`/
`ParseDisplay`); lossless 8-bit carrier = **Latin-1** (`Encoding.Latin1`, byte k ↔ U+00kk) — a CROSS-SUBSYSTEM
constant shared with whole-group-image and files (§14.4). RENAMES must immediately follow the record's last entry
(SR2) and is attached to the owning record as a sibling, not into the storage tree.

---

## 5. Control flow

### 5.1 The dispatcher shape (authoritative; pipeline's `goto case` shape is the loser — §14.6)

One C# method per program unit:

```csharp
private const int N = /* paragraph count */;
private static int Dispatch(int startPc, int exitPc) {
  int pc = startPc;
  while ((uint)pc < (uint)N) {
    bool atExit = pc == exitPc;                 // captured BEFORE the body (the body overwrites pc)
    switch (pc) {
      case 0: /* para 0 body */ pc = 1; break;  // fall-through = pc = i+1
      case 1: if (cond) { pc = 3; break; }       // GO TO sets pc
              pc = 2; break;
      // …
      default: pc = N; break;
    }
    if (atExit && pc == exitPc + 1) return pc;   // named THRU exit paragraph fell off its end
  }
  return pc;
}
internal static void Main() { try { Dispatch(/*EntryParagraphIndex*/0, -1); } catch (StopRun) { } }
```

This realizes the legacy's PROVEN **return-address / exit-bounded** dispatch (DEVLOG 259–260) in idiomatic C#.
Control is followed by **pc value, never by physical block extent** — which is why inverted THRU ranges and
GO-TO-out-of-and-back-into a PERFORM range are correct *for free*.

### 5.2 Statement → emission table

| COBOL | C# |
|---|---|
| sequential fall-through | `pc = i+1; break;` (the last stmt of each non-terminating case) |
| `GO TO p` | `pc = idxP; break;` |
| `GO TO p1 p2 … DEPENDING ON sel` | `switch ((int)sel) { case 1: pc=idx1; break; … default: /* no transfer → fall-through */ }` |
| `ALTER g TO PROCEED TO t` | `_alter_g = idxT;` (a mutable `private static int _alter_g = <defaultTarget>;`); the alterable GO TO emits `pc = _alter_g; break;` |
| out-of-line `PERFORM p [THRU q]` | `Dispatch(idxP, idxQ); pc=i+1; break;` (recursive bounded dispatch; idxQ=idxP if no THRU) |
| `PERFORM p n TIMES` | `for (long i=0;i<n;i++) Dispatch(idxP, idxQ);` |
| inline `PERFORM … END-PERFORM` | a REAL C# loop INSIDE the case (`for`/`while`/`do…while`), never a Dispatch call |
| `EXIT PERFORM` / `EXIT PERFORM CYCLE` | `break;` / `continue;` (scoped to the nearest inline PERFORM) |
| `EXIT PARAGRAPH` | `pc = myIdx+1; break;` |
| `EXIT SECTION` | `pc = lastParaIdxInSection+1; break;` |
| `EXIT` (bare) / `CONTINUE` | no-op (no statement) |
| `NEXT SENTENCE` | forward `goto __sent_<n>;` to a sentence-boundary label in the same case |
| `STOP RUN` | `throw new StopRun();` (caught only at run-unit `Main`) |
| `GOBACK` / `EXIT PROGRAM` | `throw new ProgramReturn();` (caught at the current program's `Entry`) |

### 5.3 Key decisions

- **STOP RUN vs GOBACK are DISTINCT exceptions** (the authoritative resolution — see §14.5). `StopRun` unwinds ALL
  Dispatch frames in ALL programs (caught at run-unit `Main`); `ProgramReturn` unwinds only THIS program's PERFORM
  frames and returns to the CALL site (caught at the program's `Entry` wrapper; carries the RETURNING value). Integer
  pc is used ONLY for in-program transfers. *(Rejected: pipeline's "GOBACK → `return;` from the current Dispatch
  level" — a C# `return` exits only the innermost recursive Dispatch, so a GOBACK nested inside a PERFORM would
  wrongly resume the PERFORM caller instead of returning to the program's caller. Also rejected: the legacy
  `pc = -1` return-code propagation — unwinds only one frame and can't cross a CALL boundary.)*
- **ALTER → a mutable `int` field**, not the legacy `int[] _alterTable`. The pc-variable model makes this trivial —
  the decisive reason for switch-on-pc over pure labels.
- **DECLARATIVES/USE:** ONE pc index space over ALL paragraphs INCLUDING declaratives (so every pc value agrees), but
  `Main` starts at `EntryParagraphIndex` (first paragraph after END DECLARATIVES, ISO §14.4). A declarative is
  reached ONLY via a `Dispatch(declStart, declEnd)` call from the runtime I/O/error path, never main fall-through.
  (Avoid the legacy off-by-N: dispatch order excluding declaratives while pc values include them.)
- **EVALUATE → a chained if/else-if/else, NOT a C# switch** (ISO §14.9.13.4 GR4: process each WHEN left-to-right,
  first match). WHEN arms are ranges/conditions/multiple-ALSO/ANY/partial-expressions — not constant case labels.
  Selection subjects hoisted into locals (evaluated once, GR3).
- **PERFORM VARYING…AFTER:** each outer increment RESETS all inner (AFTER) identifiers to their FROM values before
  re-testing the outer UNTIL (ISO §14.9.28 + Annex D.26 figs D.11–D.14 — the #1 VARYING gotcha).
- **Dead-code suppression:** track a `terminated` flag; once an unconditional transfer is emitted (GO TO/STOP/GOBACK/
  EXIT *), stop emitting the rest of that case (unreachable-but-legal COBOL would fail warnings-as-errors).

### 5.4 Hard problems

GO TO that exits/re-enters a PERFORM range (free — return-address model); inverted THRU `B` before `A` (free — never
iterate `[min,max]`; NC102A); overlapping/recursive PERFORM (the C# call stack IS the return-address stack); duplicate
paragraph names across SECTIONs (resolve by paragraph SYMBOL to a distinct PcIndex at bind time, never by name);
NEXT SENTENCE goes to the next *period*, NOT past a scope delimiter (ISO Annex F.1 — the common misconception).

---

## 6. Numeric model

### 6.1 The substrate + the value engine (the central hardening)

Storage is the narrow native type (`long`/`Int128`/native-int/`float`/`double`). The **value engine is
Int128-monomorphic** via a single intermediate carrier:

```csharp
public readonly record struct CobolInt(Int128 Unscaled, int Scale) {
  public static (CobolInt,CobolInt) Align(CobolInt a, CobolInt b);   // scale-align to max(scales)
  public static CobolInt Add(CobolInt a, CobolInt b);                // result scale = max(scales)
  public static CobolInt Sub(CobolInt a, CobolInt b);
  public static CobolInt Mul(CobolInt a, CobolInt b);                // result scale = sum(scales)
  public static CobolInt Div(CobolInt a, CobolInt b, int guardScale, CobolRounding m);
}
```

Every operand widens `long`→`Int128` at op entry, scales-align, computes in `Int128`, and a single
`TryStore` rescales/rounds/truncates/bounds-checks back into the receiver's storage type. **The current
`CobolNum` is long-only and silently overflows real `COMPUTE`** (e.g. `c = a * b` on two `PIC 9(18)` = 36
digits) — `Int128` (38 digits) covers every legal fixed-point picture and its intermediates. *(Rejected:
keep long-only — silently wrong; `decimal`/`BigInteger` intermediates — owner-locked out; generic
`INumber<T>` monomorphized per width — JIT-bloat + reintroduces width-typed overflow.)*

### 6.2 Intermediate precision (ISO §8.8.1; mined from the legacy `decimal` path)

- ADD/SUBTRACT result scale = `max(scales)`; MULTIPLY = `sum(scales)`.
- **DIVIDE/COMPUTE-division** quotient at an explicit `guardScale = max(all receiver scales, all operand scales) +
  DIV_GUARD_DIGITS`, **`DIV_GUARD_DIGITS = 14`** (reproducing the legacy `decimal` accumulator's ~28-sig-digit
  headroom — the one scale `decimal` auto-picked that `Int128` forces explicit); the final per-receiver store rounds
  ONCE to the receiver scale per the ROUNDED mode.
- EXPONENTIATION integer powers expand to repeated multiply (§8.8.1.5.4).
- **Statement arithmetic** (ADD/SUB/MUL/DIV with GIVING) enforces the 31-digit composite-of-operands limit at COMPILE
  time (guaranteed to fit `Int128`); **COMPUTE expressions have NO composite limit** (§8.8.1.2 rule 7) — `Int128` is
  the cap, EC-SIZE-OVERFLOW past ~38 digits.
- **v1 arithmetic mode = NATIVE** (§8.8.1.3, implementor-defined = `Int128` fixed-point) — the corpus default.
  STANDARD-DECIMAL (decimal128) and STANDARD-BINARY (spec-obsolete) are owner-gated/deferred (§15).

### 6.3 Usages & store

- DISPLAY/COMP/COMP-4/BINARY → DigitCount discipline; COMP-3/PACKED → 2n−1; COMP-5/BINARY-* → native
  two's-complement width (binary-WRAP, not digit truncation — `PIC S9(4) COMP-5` = −32768..32767, `PIC 9(4) COMP-5`
  = 0..65535, an unsigned 8-byte needs `ulong`); COMP-1/COMP-2 → IEEE, bypass the scaled engine.
- The 8 ROUNDED modes are the existing `CobolRounding` enum. **`Store` is hardened to `TryStore`** (returns `bool`;
  receiver UNCHANGED on overflow; ROUNDED MODE PROHIBITED → SIZE ERROR on an inexact result). The current `Store`
  (`%= Pow10(Digits)` silent truncate) is only the no-ON-SIZE-ERROR branch. *(See §14.7: `TryStore` and the
  conditions design's `StoreChecked` are the SAME method — settle on `TryStore`.)*

### 6.4 Signed-DISPLAY overpunch (NumProfile gains `SignKind`)

`NumProfile` adds `SignKind ∈ {TrailingOverpunch[default], LeadingOverpunch, LeadingSeparate, TrailingSeparate}`
(currently only a `Signed` bool — which cannot reproduce the external image). **IBM-ASCII overpunch tables** (verified
NIST-exact against the legacy): positive `0→'{',1→'A'…9→'I'`; negative `0→'}',1→'J'…9→'R'`. `PIC S9(3)` +42→`"04B"`,
−42→`"04K"`, −150→`"0015}"`, SIGN LEADING −37→`"}37"`. The default with no SIGN clause is TRAILING overpunch.
**`CobolNum.FormatDisplaySigned`/`ParseDisplay` (encode+decode incl. overpunch + separate sign) must land before §4
Tier-B/C numeric-view accessors are exact** — the current `FormatDisplay` returns magnitude.

### 6.5 Numeric-edited formatting

PORT the proven two-pass legacy `FormatByEditPattern`/`FormatNumericEdited` verbatim into
`CobolEdit.Format(CobolInt value, EditPattern pat, env) → string` (the field's CLR storage is `string`). Covers
`Z * $ + - CR DB B 0 / . ,` fixed+floating insertion, asterisk check-protect, BLANK WHEN ZERO, full-field-blank,
DECIMAL-POINT IS COMMA, CURRENCY SIGN. *(Rejected: rewrite from the §14.9.x grammar — high regression risk against a
battle-tested 364-NIST oracle; .NET format strings — can't express floating insertion/check-protect/overpunch.)*

### 6.6 Hard problems / edge cases

- MOVE rescale is value-preserving with TRUNCATION rounding (never ROUNDED); a scaled→integer MOVE drops the
  fraction; an unsigned receiver stores the magnitude (GR8; watch `long.MinValue`).
- Comparisons align scales then compare the `Int128` unscaled values; `+0 == −0`.
- IS NUMERIC on a typed numeric field folds to `true` (it can't hold non-digits) UNLESS the field aliases external
  bytes (REDEFINES/file/ACCEPT) — then validate the char image (`CobolNum.IsNumericClass(image, profile)`).
- ON SIZE ERROR is two-phase (ISO §14.7.5 rule 4): (a) intermediate-evaluation error → no receiver changes; (b) per-
  receiver store error → only THAT receiver unchanged; the phrase fires if ANY failed (generated C# accumulates a
  `bool __sizeErr`).
- P-scaling (leading adds to FractionScale; trailing rounds to the 10^P grid); divide-by-zero → SIZE ERROR (guard
  `b.Unscaled == 0`, never a .NET `DivideByZeroException`).

---

## 7. String operations

### 7.1 Representation + the runtime class

Alphanumeric/national elementary items are `string` at rest; every mutating op takes the value in and returns the new
value; the emitter assigns ONCE: `FIELD = CobolStrings.InspectReplace(FIELD, …);`. **A new
`CobolNet.Runtime.CobolStrings`** holds the ported INSPECT/STRING/UNSTRING algorithms (mined verbatim from the legacy
`InspectRuntime`/`StorageArea` — proven over 364 NIST tests; only the I/O type changes from `(byte[],offset,len)` to
`string`). *(Rejected: `Span<char>` mutable view — `System.String` is immutable; a byte[] scratch — reintroduces the
banned substrate.)* See §14.8 for the `CobolString` vs `CobolStrings` roster split.

### 7.2 The C# mappings

- **Reference modification read:** `CobolString.RefMod(s, leftmost, length)` (1-based; length omitted → to end as
  `-1`); raises EC-BOUND-REF-MOD on out-of-range/zero (unless REF-MOD-ZERO-LENGTH on → `""`).
- **Ref-mod write:** `RESULT = CobolString.SpliceInto(dst, leftmost, length, newSlice)` rebuilds the string;
  positions evaluated once. Editing is NOT re-applied (spec NOTE 21209).
- **INSPECT** marshals operands into parallel arrays and calls ONE method per statement (per comparison cycle —
  TALLYING/REPLACING share a single left-to-right cycle, ISO §14.9.22.4 GR8). Counters/COUNT route through
  `CobolNum` + `NumProfile` (a counter is NOT initialized, GR11). BACKWARD = reverse target+patterns, run forward,
  reverse back.
- **STRING** seeds the working buffer from the dest's current value (GR7: only written positions change); pointer is
  a C# `int` round-tripped via `CobolNum`; overflow when pointer<1 or >len before a char move (GR8).
- **UNSTRING** uses a per-INTO `UnstringExtract` (earliest delimiter wins, tie→first-listed, ALL skips contiguous
  repeats; two contiguous delimiters → empty extract → space/zero fill via `CobolString.Store`/`CobolNum.Store`).
- **National (PIC N)** uses the IDENTICAL string helpers with NO surrogate-aware handling — one COBOL char = one
  UTF-16 code unit (ISO §8.5.1.4: "each two-octet code element of UTF-16 is treated as though it were itself a
  character"). C# string indexing IS per-code-unit, so .NET `string` is the exact COBOL national model.

### 7.3 Key decisions

- **Short-circuit `&&`/`||`** for AND/OR (a deliberate divergence from the eager legacy oracle) — empirically
  corpus-safe (a scan found ZERO guard-then-same-subscript idioms). *(Local escape hatch if a future program needs
  eager eval: hoist the right operand before `&&`.)*
- **Figurative constants** map: SPACE→`' '`, ZERO→`'0'`/`0L` (by receiver category), QUOTE→`'"'`, **HIGH-VALUE →
  U+00FF (alphanumeric) / U+FFFF (national), LOW-VALUE → U+0000** (the cross-subsystem settlement — §14.9), ALL "x"
  repeat-to-width.
- **A typed `StringLvalue` IS a `Place`** (§14.1) — not a second abstraction.

### 7.4 Hard problems

Non-alphanumeric/group operands "treated as if redefined alphanumeric of same size" → the materialize/writeback path
(§14.4 whole-group image); numeric source → unsigned digit string (GR4d de-signing). The lvalue/subscript/ref-mod
discrimination is the §3.4 SUB_* interpreter. Overlap (spec-undefined) → deterministic-and-safe via read-once /
single-write-back.

---

## 8. Files

### 8.1 The model

The FD/SD record IS a `record struct` (the record area is a typed field, not a byte buffer). The only bytes are at
the on-disk edge, produced by a compiler-GENERATED per-layout codec (`Serialize`/`Deserialize`) running only at READ
and WRITE. **CODE-SET is one `Encoding` parameter threaded into that codec.** The proven 364-NIST legacy handlers are
ported VERBATIM for *control logic* (open-mode tables, ISO §9.1.13 status codes, the file-position-indicator +
key-of-reference + duplicate-arrival state machines) but re-substrated from a byte array to a generic
`FileConnector<TRec>` + an `IRecordCodec`.

```
FD CUST-REC { CUST-ID PIC 9(5); CUST-NAME PIC X(20); CUST-BAL PIC S9(7)V99 COMP-3 }
  → public record struct CUST_REC { public long CUST_ID; public string CUST_NAME; public long CUST_BAL; }
    (CUST_BAL is the UNSCALED long; scale 2 = metadata)
```

The connector exposes Open/Close/Read/ReadPrevious/ReadByKey/Write/Rewrite/Delete/Start/SetKey + `CurrentSlot`,
`LastRecordLength`, `EndOfPage`, `LastStatus`. **There is NO program-supplied key parameter** — the key is the current
value of the typed RECORD KEY / RELATIVE KEY field.

### 8.2 Key decisions

- **Ordering/lookup keys are a typed-derived `CobolKey` comparable** (numeric by decoded value; alphanumeric by image
  + collating with shorter-operand space-extension; composite component-wise), DECOUPLED from the stored payload.
  ONE comparison policy shared by indexed files AND SORT. *(Rejected: the legacy Latin-1 byte-string sorted-dictionary
  trick — silently mis-orders COMP/COMP-3/signed keys, whose on-disk image is not order-preserving.)*
- **Stores:** sequential = `StreamReader/Writer` (line-sequential) or `FileStream` + a 4-byte little-endian length
  prefix (varying); relative = sorted dict `int slot → byte image` with `0xFF` gaps; indexed = sorted dict
  `CobolKey → byte image` (sole source of truth, alternates derived on demand + an arrival map for duplicate
  ordering). The in-memory PAYLOAD is the serialized image (exactly the form persisted on CLOSE; bounded memory),
  deserialized to the typed record only on hand-back. *(This is owner-flagged — §15 Q-file-2.)*
- **Multiple 01s under one FD (and SAME RECORD AREA) = a discriminated record-area wrapper:** READ deserializes the
  raw bytes into EVERY 01 view; WRITE serializes the named view. This is the file-edge analogue of REDEFINES (ISO
  §9.1.2 NOTE / §13.18.33 GR3).
- **SORT/MERGE:** SD record is a typed struct; the sort store holds serialized images ordered by the same `CobolKey`
  policy; key offsets computed into the deterministic serialized image at compile time. Format-2 in-place table SORT
  operates on the typed array directly (the one place the two SORT forms diverge — a typed comparer).
- **The SD/FD record codec IS the generated `AsImage()`/`FromImage()` pair (Phase 1E):** for every image-capable
  record — including mixed-usage records with fixed-point BINARY/PACKED leaves, which serialize each such leaf as
  its §14.4 zoned digit image (width = `Pic.Digits`, trailing-overpunch sign; ISO §13.18.60 USAGE GR4 implementor
  representation) — WRITE/RELEASE send `AsImage()`, READ/RETURN distribute via `FromImage()`. `IRecordCodec` for
  these records is realized by that pair; no separate serializer exists (singular-pattern). A signed COMP/COMP-3
  SORT/MERGE key's compile-time descriptor carries `PicInfo.ImageSignKind` (the IMAGE sign — trailing overpunch),
  never the leaf's stored `BinaryMinus`, so `CobolSort.NumericKey` decodes the zoned window algebraically
  (§14.9.40 GR8 / §8.8.4.2.4 — negatives order before positives; ST108A/ST127A/ST133A/ST134A). The greenfield
  on-disk record width for such records legitimately differs from the legacy's raw-byte form (e.g. 80 chars where
  the legacy wrote 72+4 binary bytes) — chains are same-engine, cross-engine file compatibility is not required.
  Only float (COMP-1/2), COMP-5, and INDEX leaves keep a record outside the codec (loud Tier-C island, §4.2).

### 8.3 Hard problems / edge cases

Variable-length REWRITE must equal the replaced length (status 44; remember last-read length); the read-position
state machine (READ NEXT after AT END = 46; sequential REWRITE/DELETE without a preceding READ = 43; START =
inclusive FPI); EXTERNAL FDs — **IMPLEMENTED (Phase 1F, IC227A; §13.18.22.4 GR4a/GR4b/GR5)**: the connector keys
the run-unit registry by `"::EXT::" + externalized-name` (= the FD name, GR5) instead of the per-program
`PROG::FILE` qualification, so every describer's verbs converge on ONE connector (`CobolFile.Register` keeps an
existing `::EXT::` key — a later describer's activation never clobbers the live connector; CANCEL's `CloseFiles`
skips external connectors, §14.9.5 GR8/GR9), and the record area re-bases onto ONE run-unit `ExternalStore` cell
keyed `"FD::" + externalized-name` via the same Tier-B string-canonical machinery WS EXTERNAL 01s use
(`DataBinder.Linkage.cs::CallBindExternalAndGlobal`); FILE STATUS items stay per-program; the GR6 same-byte-count
cross-describer check is §14.8.4 EC-band work (documented-deferred). GLOBAL FD inheritance — **IMPLEMENTED
(Phase 1F, IC233A/IC234A; §13.18.30)**: ancestors' GLOBAL FileModels merge into a contained unit's `FilesByName`
ONLY (never `Files` — no re-registration/re-qualification/CANCEL-close; the shared FileModel reference keys the
child's verbs to the owner's connector), the GLOBAL FD's records join `CallGlobalRoots` (record-names are global
names → the standard `__outer` ref-bridges), `FileOfRecord` resolves a contained WRITE/REWRITE of the owner's
record through the merge, and the I-O status routes to the owner's local status item (§12.4.5.8.4 GR1 NOTE 1);
OPTIONAL files (OPEN INPUT missing → 05 + EOF; non-optional missing → 35);
sequential RELATIVE WRITE assigns the next slot and MOVEs it into the RELATIVE KEY field; LINAGE (LINAGE-COUNTER,
page reset/overflow, footing area, END-OF-PAGE). After each I/O verb the compiler stores `LastStatus` into the FILE
STATUS item then branches AT END / INVALID KEY on the first char (1 at-end, 2 invalid-key, 3/4/7/9 fatal → a USE
declarative). READ INTO → Read + a typed group MOVE (receiving uses MAX length for ODO records — ST146A).

---

## 9. Interprogram (CALL / cross-program data)

### 9.1 The ONE carrier

`ManagedRef<T>` (the typed-native re-implementation of `ManagedPointer`) serves BY REFERENCE args, LINKAGE items,
USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET ADDRESS OF — honoring the singular-pattern rule. Two
construction modes + a Null state:

```csharp
// accessor-over-native-field (the common case — WORKING-STORAGE stays native, zero boxing):
ManagedRef<long>.OverField(() => WS_X, v => WS_X = v)
// standalone cell (LINKAGE / ALLOCATE / BY CONTENT copy):
ManagedRef<long>.Cell(5L)
ManagedRef<long>.Null
```

Crucially the carrier does NOT box WORKING-STORAGE: an ordinary `01 WS-X PIC 9(4)` stays a native `long`; a carrier
is built ONLY at a call site as an accessor over the caller's native field, so DISPLAY/MOVE/arithmetic keep zero
indirection. *(Rejected: a NEW parallel `CobolRef<T>` — violates singular-pattern; making every aliasable item itself
a heap cell — the byte-State sin in new clothes; C# `ref` everywhere — can't be stored in a LINKAGE field,
re-pointed by SET ADDRESS OF, or crossed over the opaque dynamic-CALL ABI; the legacy `ManagedPointer(byte[],offset,
length)` — the abandoned byte substrate.)* **`ManagedRef<T>.OverField(place.Read, place.Write)` is built FROM a
`Place`** — so it is not a third lvalue mechanism (§14.2).

### 9.2 Two-layer calling convention

- **Uniform opaque ABI** for dynamic + cross-assembly CALL: `interface ICobolProgram { int Call(CobolArgs args); void
  Cancel(); }` where `CobolArgs` = ordered `(PassMode, caller PicMeta, carrier)` — the typed analog of the rejected
  `Entry(ManagedPointer[])`. The callee maps positionally onto LINKAGE items.
- **Typed fast path** for same-assembly, statically-resolvable, PIC-conforming calls: a direct typed method
  `R = _SUB.Run(carrier…)`. RETURNING → the C# method return value (idiomatic, ISO-faithful). *(Designing the opaque
  ABI first makes the fast path a pure optimization, not a retrofit. Rejected: typed-signatures-only — dynamic/
  cross-assembly CALL would retrofit badly; trailing-scratch-buffer RETURNING — the byte ABI.)*

### 9.3 Program model + state matrix

Each program → its own **instantiable** (non-static) C# class; nested/contained → nested classes; run unit → one
assembly, first program = entry. State→storage: plain → instance fields on a cached singleton (last-used); INITIAL →
re-init WS to VALUE each activation; RECURSIVE/function/method → fresh instance per activation; LOCAL-STORAGE → re-init
per activation; EXTERNAL → one static run-unit holder per name (not reset by CANCEL); GLOBAL → field on the outer
class visible to nested classes; LINKAGE → carrier-bound (never initialized); CANCEL → re-init on next CALL. *(The
current single `static class Program` cannot host multiple/nested programs or recurse — this replaces it.)*

### 9.4 Hard problems / edge cases

- **BY REFERENCE category mismatch** (arg `PIC X(4)` seen as formal `PIC 9(4)`) is the ONE sanctioned transient-byte
  boundary: a scratch byte image the callee's `ManagedRef` decodes/encodes; same-category (the common case) is always
  fully typed.
- **Group BY REFERENCE:** the carrier round-trips the WHOLE struct per access (a value type; a closure copies on
  read) so subordinate mutations propagate as a unit; OCCURS args pass the `T[]` reference directly.
- **Args evaluated once at CALL time** (§14.9.4.4 GR3a): capture subscript/ref-mod into locals BEFORE building lazy
  carriers.
- **GOBACK in a called program = `ProgramReturn` caught at `Entry`** (§5.3 / §14.5) — not a competing mechanism; OO's
  "method return" and this "called-program return" are both just *what the `Entry` catch does*.
- Transitive passing mode (default BY REFERENCE; a phrase applies to all following args); OMITTED → `Null` carrier;
  EXTERNAL survives CANCEL; COMMON callable by siblings; a managed ref cannot serialize to stable bytes (reject
  pointer-to-file / REDEFINES-pointer-as-bytes as undefined).

---

## 10. OO

### 10.1 The model

ONE real C# class per CLASS-ID (`public class Foo : CobolObject` or `: Base`); the driver PROGRAM stays the static
`Program` class; instance fields per OBJECT data item; real C# methods per METHOD-ID; INVOKE → real C#
`new`/`obj.M(...)`/`base.M(...)`. **Let Roslyn perform cross-type binding, virtual dispatch, and conformance
checking** — the emitter's "two-pass" shrinks to building its OWN symbol table (class → method → {param modes,
return}) so INVOKE can marshal args + pick the call form. *(Rejected: port the legacy per-instance-ProgramState byte
model — owner-locked out + discards Roslyn's free type-checking; emit via Mono.Cecil — re-introduces the manual
cross-type registry the source target eliminates.)*

`CobolObject` is a runtime base class (every COBOL class derives from it directly or via its `INHERITS` chain) — a
single reflection-free home for universal/dynamic dispatch, NULL/`IS class` semantics, and a future EC-OO surface
(AOT/WASM-safe). *(Rejected: derive from `System.Object` — universal dispatch then needs reflection.)*

### 10.2 Storage scopes (two counterintuitive)

OBJECT-para WORKING-STORAGE → **INSTANCE** fields; METHOD WORKING-STORAGE → **STATIC** fields (ISO §11.7 GR5/§11.8:
method WS persists across activations, shared across instances — NOT per-instance); METHOD LOCAL-STORAGE → C# locals
(re-init each call); LINKAGE → method parameters; a method-local name SHADOWS object data. FACTORY → static
members/methods + static data.

### 10.3 INVOKE + attributes

`Class "NEW" RETURNING o`→`o = new Class()` (the predefined NEW = the generated ctor: base ctor first, then VALUE-init
own instance fields); `obj "M"`→`obj.M(args)` (virtual); `SELF "M"`→`this.M()` (virtual — runtime-class dispatch,
§8.4.3.8 GR2); `SUPER "M"`→`base.M()`; `Class "M"` (non-NEW)→static call; dynamic/universal (method-name in a data
item, or a universal `object?` receiver)→`recv.__CobolInvoke(name, args)` (a per-class switch — reflection-free).
Instance methods are `virtual` by default (COBOL forbids implicit hiding, §11.7 SR4a → never emit C# `new`); OVERRIDE→
`override`; FINAL→`sealed override`; ABSTRACT→`abstract`. BY REFERENCE → typed `ref` (§9.3.6 match-rule 3c requires
same class/category — so `ref` is conformant); RETURNING → C# return value. **Parametric polymorphism deferred** (an
OPTIONAL feature, §12063; no corpus use; `PIC 9(4)`/`PIC 9(8)` both map to `long` and would collide).

### 10.4 The correctness blocker (resolved)

**GOBACK in a method ≠ STOP RUN.** The current emitter throws `StopRun` for BOTH (CSharpEmitter ~204-205); inside a
method that unwinds past the INVOKE caller and drops the RETURNING value. Resolution = the §5.3/§14.5 model: GOBACK/
EXIT METHOD → **throw `ProgramReturn` (carrying the RETURNING value), caught at the method's entry wrapper** (the same
signal/catch a called program uses, §14.5); STOP RUN keeps throwing the run-unit `StopRun`. A bare C# `return` is
WRONG here for the identical reason it is wrong at program level (§14.5): a method has its own `Dispatch` loop and
paragraphs, so GOBACK can sit inside a PERFORM (a recursive `Dispatch` call within the method), and a `return` would
exit only the innermost `Dispatch`, not the method.

### 10.5 Hard problems / edge cases

Multiple class inheritance (`INHERITS FROM {name}…`) — C# has only single inheritance → **v1 restricts to single
inheritance** (verified sufficient for the whole corpus), rejects 2+ bases LOUDLY (owner question — §15); per-instance
vs static field selection threads through the WHOLE statement emitter via the §14.1 Place owner-scope chokepoint;
INVOKE on null → EC-OO-NULL (guard before the call); the binder must add an ObjectReference item kind (currently
silently ignored). Object GROUP/OCCURS data → per-instance `record struct`/array, byte-identical to PROGRAM data
except instance-vs-static.

---

## 11. Conditions & exceptions

### 11.1 Two code shapes

(1) Conditions are PURE side-effect-free C# boolean expressions (`RenderCondition(node) → string`) so they compose
into `if`/`while(!(…))`/`?:`/EVALUATE arms/88 bool properties. The grammar's rule cascade (Or→Xor→And→unary→primary)
already encodes COBOL precedence NOT > AND > XOR > OR — preserved by construction. (2) The EC exception model is
stateful runtime + emitted guards that appear ONLY when a program uses the feature — **EC checking is OFF by default**
(ISO §14.6.13.1.1), so the typed-native fast path emits zero exception scaffolding in the common case.

### 11.2 Key decisions

- **Fully parenthesize every emitted binary boolean node** (`(a && b)`, `(a || b)`, `(a ^ b)`): C# precedence has `^`
  binding TIGHTER than `&&`/`||`, which does NOT match COBOL AND > XOR > OR — explicit parens make grouping match the
  parse tree.
- **Short-circuit `&&`/`||`** (the §7.3 corpus-safe divergence).
- **EVALUATE → chained if/else-if** (§5.3).
- **Level-88 → expression-bodied `bool` properties** derived from the live parent value (§3.5); SET cond TO TRUE
  moves the first VALUE; SET TO FALSE moves the WHEN SET TO FALSE literal (error if none).
- **Class conditions** run over the character image via a new `CobolClass` runtime; for a pure numeric `long`,
  IS NUMERIC folds to `true` (revisit when REDEFINES/file aliasing lets it hold non-digits). ALPHABETIC is the closed
  Latin set `{A-Z,a-z,space}` (ISO §8.8.4.4) — NOT `char.IsLetter`.
- **Conditional phrases (ON SIZE ERROR / AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION) are ALWAYS active when
  written** and do NOT require `>>TURN`; `>>TURN` is resolved at COMPILE time (a `TurnState` walking the procedure
  division) and decides WHETHER an EC guard is emitted at all — OFF compiles to nothing.
- **USE…EXCEPTION/ERROR declaratives** → paragraph-methods + a compile-time registry keyed (EC/file/open-mode); the
  declarative method returns a `ResumeAction {Default, NextStatement, Procedure(name)}` so RESUME can redirect.
- The exception-checking **PERFORM…WHEN** form (M2) is the one place a real C# `try/catch` is used; RAISE/RESUME and
  fatal/nonfatal termination are runtime calls (`CobolException.Raise`; an unhandled fatal EC → `CobolFatalException`
  caught at `Main` → nonzero exit).

### 11.3 EC runtime

`CobolNet.Runtime.Exceptions`: `ExceptionCatalog` (generated from ISO Table 13 — level-3→level-2→EC-ALL hierarchy +
fatality), `ExceptionState` (last-exception register, EXCEPTION-OBJECT, file/location/statement), `CobolException`/
`CobolFatalException`, `ExceptionDispatch` (declarative registry). New diagnostics → a `COBOLNET07xx` band (§14.10).

### 11.4 Hard problems / edge cases

NEXT SENTENCE ≠ CONTINUE (labeled-block + goto, §5.3); IS NUMERIC sign-image validity (overpunch/separate); NOT
POSITIVE = ≤0 (includes zero); abbreviated combined conditions (expand subject+operator); ON SIZE ERROR leaves the
receiver UNCHANGED, ROUNDED before the size test (§6.6); whole-group comparison needs the §14.4 image facility.

---

## 12. Intrinsics, special registers & misc surfaces

### 12.1 Intrinsic catalog (the graded spine)

Reject porting the legacy `decimal`-typed signatures. Build ONE declarative `IntrinsicCatalog`: name → {ISO §15.2
function-type, result category, arity (fixed/optional-trailing/variadic), per-arg category, binding =
compile-time-fold | runtime-method}. §15.2's six types ARE the return-type column, mapped to the substrate:

- **integer-function → `long`** (`Int128` past 18 digits — FACTORIAL);
- **floating-point math (SQRT, trig, LOG/EXP, PI, STANDARD-DEVIATION, VARIANCE, ANNUITY, PRESENT-VALUE, RANDOM) →
  `double`**;
- **exact numeric (SUM, MEAN, MEDIAN, MAX/MIN-numeric, MOD, REM, INTEGER, INTEGER-PART, FRACTION-PART, ABS, SIGN,
  NUMVAL, NUMVAL-C, NUMVAL-F) → `CobolInt`** (so it flows straight into `TryStore` with the receiver's ROUNDED);
- **alphanumeric/national (UPPER-CASE, LOWER-CASE, REVERSE, TRIM, CONCATENATE, SUBSTITUTE, CHAR, NATIONAL-OF,
  DISPLAY-OF, date strings) → `string`**; **boolean → `bool`**.

Runtime homes: `CobolNet.Runtime.CobolIntrinsics` + `CobolNet.Runtime.CobolDate`. Mine the legacy `IntrinsicFunctions`
for BEHAVIOR only (NaN/out-of-domain → EC-ARGUMENT-FUNCTION default result; ORD-MAX/ORD-MIN tie = first; NUMVAL
parsing; date algorithms), not types. MAX/MIN are category-polymorphic (resolve by arg category at the call site);
`table(ALL)` expansion + variadic `params` port from legacy; **FUNCTION LENGTH folds at compile time from PIC
metadata** (and is kept distinct from LENGTH OF / BYTE-LENGTH, which count *bytes*).

### 12.2 Special-register registry

Model every special register as a SYNTHESIZED `DataItem` registered in `DataBinder.ByName` — so a register read/store
reuses the EXACT `CobolNum`/`CobolString` paths (zero special-case in the verb emitters). RETURN-CODE/TALLY/
SORT-RETURN → static `long`; WHEN-COMPILED → a compile-time constant string (timestamp injectable for determinism);
LENGTH OF/BYTE-LENGTH → a folded `long` byte-size from PIC+USAGE; ADDRESS OF → `ManagedPointer`; LINAGE-COUNTER/
LINE-COUNTER/PAGE-COUNTER + XML-*/JSON-* register NAMES are reserved by the registry but attach to their (scope-flagged)
subsystems. **RETURN-CODE is ONE canonical static field** also written by GOBACK/STOP RUN/CALL RETURNING and read as
the process exit code — a cross-subsystem contract (§14, §15).

### 12.3 Smaller surfaces

- **Figurative constants** = context-materialized sentinels resolved at each use site against receiver category+width
  (§8.3.3.6); HIGH/LOW-VALUE per §14.9.
- **INITIALIZE** = a compile-time tree-walk to per-elementary typed stores (default/VALUE/REPLACING; FILLER skipped;
  OCCURS → a for-loop).
- **SET** = dispatch by target kind (index→long, pointer→`ManagedPointer`/NULL, switch→bool, cond-name TO TRUE→store
  the 88's first VALUE) — depends on the §3.5 88/INDEXED-BY binding.
- **ACCEPT/DISPLAY system sources** = a `CobolSystem` runtime with an INJECTABLE clock (DATE/DAY/TIME/DAY-OF-WEEK/
  YYYYMMDD/YYYYDDD; DAY-OF-WEEK remap `((int)DayOfWeek + 6) % 7 + 1` = 1=Mon..7=Sun) + console UPON SYSOUT/SYSERR.
- **ALPHABET/CLASS/CURRENCY/DECIMAL-POINT IS COMMA** = a SPECIAL-NAMES config object threaded into emit (mostly
  compile-time).
- **SCREEN SECTION, JSON/XML GENERATE/PARSE** are scope-flagged big subsystems — designed only to the
  seam (reserve their register names, one-paragraph deferral each). Their scope is an owner question (§15).
  **REPORT WRITER is IMPLEMENTED** (the Phase-1C NIST drive brought it forward from the M3 ordering): the deep-dive
  is `docs/COBOLNET_REPORT_WRITER_DESIGN.md` — the `CobolReport` engine, compose-at-presentation lines, the
  LINE-/PAGE-COUNTER registers, CONTROL/SUM, USE BEFORE REPORTING; RW101A–RW104A byte-match.

---

## 13. The runtime (`CobolNet.Runtime`) — consolidated surface

The generated C# calls a small set of runtime classes (the roster is settled in §14.8):

| Class | Responsibility |
|---|---|
| `CobolInt` (record struct) | the Int128-monomorphic arithmetic carrier (Align/Add/Sub/Mul/Div) |
| `CobolNum` | numeric store/format: `TryStore`, `Rescale`, `FormatDisplay`/`FormatDisplaySigned`/`ParseDisplay`, `IsNumericClass` |
| `CobolEdit` | numeric-edited formatting (`Format(CobolInt, EditPattern, env)`) |
| `CobolString` | single-string ops: `Store`, `Compare`, `RefMod`, `SpliceInto` |
| `CobolStrings` | multi-operand INSPECT/STRING/UNSTRING (`InspectTally`/`InspectReplace`/`InspectConvert`/`StringInto`/`UnstringExtract`) |
| `CobolClass` | class-condition predicates (`IsNumeric`/`IsAlphabetic`/`IsAlphabeticUpper`/`IsUserClass`) |
| `CobolCond` | condition-name membership (`In`) |
| `CobolIntrinsics` / `CobolDate` | the FUNCTION library + date/time |
| `CobolSystem` | ACCEPT/DISPLAY system sources (injectable clock) |
| `ManagedPointer`/`ManagedRef<T>` | the ONE managed-reference carrier (§9.1 / §14.2) |
| `FileConnector<TRec>` / `IRecordCodec` / `CobolKey` / `RedefCodec` | files (§8) / REDEFINES Tier-C codec (§4) |
| `StopRun` / `ProgramReturn` / `CobolException` / `CobolFatalException` / `ExceptionCatalog` / `ExceptionState` / `ExceptionDispatch` | control-flow + EC signals (§5/§11) |
| `NotImplementedCobolFeature` | the loud-failure runtime guard (§1.4) |

Cross-subsystem runtime constants: **Latin-1** (`Encoding.Latin1`) is the ONE lossless 8-bit byte↔char carrier (files,
REDEFINES Tier-C, whole-group image — §14.4/§14.9).

---

## 14. CROSS-CUTTING CONSISTENCY (the reconciliation pass)

This section is the reason the SSOT exists. Where two subsystem designs picked different answers, the **loser is
named**.

### 14.1 ONE lvalue model: `Place` (used identically by every consumer)

`Place` (§3.3) is the universal typed-lvalue. `Place.Read()` / `Place.Write(rhs)` are consumed identically by:
**MOVE** (`dst.Write(srcImage)`), **arithmetic** (`receiver.Write(CobolNum.TryStore(...))`), **INSPECT/STRING/
UNSTRING** (`field.Write(CobolStrings.…(field.Read(), …))`), **files** (`READ INTO` = `Read()` + a typed group MOVE;
`WRITE FROM` = a MOVE + `Write()`), and **CALL/INVOKE BY REFERENCE** (`ManagedRef.OverField(place.Read, place.Write)`).
**Named losers:** string-ops' `StringLvalue` and the idea of a separate per-verb lvalue are SUPERSEDED — `StringLvalue`
IS a `Place`. The legacy `IrLocation(Area,Offset,Length)` is rejected (it IS the byte substrate).

### 14.2 The CALL-by-reference carrier is built FROM a Place (one carrier, one lvalue)

`ManagedRef<T>` (§9.1) is the ONE managed-reference carrier; it is NOT a second lvalue abstraction. At a call site,
`ManagedRef<T>.OverField(place.Read, place.Write)` wraps a `Place`'s closures. **Named loser:** there is no
`CobolRef<T>`. The carrier's PUBLIC name vs the legacy `ManagedPointer` is an owner question (§15) — the type is
typed-native (`ManagedRef<T>`), never the legacy `(byte[],offset,length)`.

### 14.3 The data-model lvalue model is used identically by MOVE / arithmetic / files / CALL — including REDEFINES

REDEFINES is reconciled to §4's **one-canonical-backing** model. The redefining views are `Place`s that compute over
the canonical backing, so MOVE/arithmetic/files/CALL touch them through the SAME `Read()`/`Write()` contract. **Named
loser (the single most important reconciliation):** data-model §8's "separate typed fields; a write to one is not
visible in the other" is SUPERSEDED by redefines/renames' 4-tier model, because separate fields reproduce the exact
silent-stale-read that triggered the DEVLOG 457 rewrite. (A write through a view IS visible through every other view of
the same class — Tier A/B accessors share the canonical; Tier C shares the class `byte[]`.)

### 14.4 ONE whole-group / materialize-to-image facility (G6)

There is ONE facility that turns a typed group/numeric into its alphanumeric image and back: a generated
`string AsImage()` (and `FromImage`) per `record struct`, used by (a) whole-group MOVE/compare, (b) INSPECT/STRING/
UNSTRING of a group/numeric operand (§7.4), (c) ref-mod of a numeric receiver, and (d) RENAMES THRU composition over a
heterogeneous span (§4.3). **Named loser:** the three names (`AsImage` / `GroupImage` / "materialize") are ONE thing
— canonical name `AsImage()`/`FromImage()`. It uses the Latin-1 carrier (§14.9). This is **transient** (built on
demand, never persisted) and is DISTINCT from REDEFINES Tier-C's PERSISTENT class-scoped `byte[]` (which would be the
only storage for a float/COMP-5/INDEX pun class). SETTLED (Phase 1E): `AsImage` IS the permanent mechanism for
mixed-usage (DISPLAY+BINARY+PACKED) groups — the byte path remains only for the genuinely non-character-imageable
usages (see the total rule below).

**Mixed-usage (COMP-leaf) groups — the settled TOTAL rule (Phase 1E; supersedes the DEVLOG 558 interim).** The
standard leaves a binary item's representation to the implementor (§13.18.60 USAGE GR4 — "Each implementor specifies
the precise effect of the USAGE BINARY clause upon the … representation of the data item …, including the
representation of any algebraic sign"; §8.8.4.1.1 — a group operand is alphanumeric over the items'
representations). The typed-native backend DEFINES that representation, totally: **a fixed-point BINARY/PACKED
leaf's character image is its fixed-width zoned digit image — `Pic.Digits` characters, implied decimal point, sign
as a TRAILING OVERPUNCH on the last digit** (`PicInfo.ImageSignKind`, the ONE image-sign mapping). The generated
`AsImage()`/`FromImage()` (gated on `DataItem.IsImageCapable`) implements it for every consumer — whole-group
MOVE/compare/DISPLAY senders, WRITE/RELEASE, READ/RETURN distribution, SORT/MERGE key decode — via
`CobolNum.FormatDisplay`/`ParseDisplay` with the leaf's `_P_` profile `with`-overridden to the image sign (the
leaf's OWN profile keeps `BinaryMinus`, so DISPLAY-statement output of a native leaf is unchanged). **Named losers:**
(a) `OperandText.MixedGroupImage` (the DEVLOG 558 inline concat) — RETIRED: it formatted a signed leaf with its own
`BinaryMinus` profile, a latent VARIABLE-WIDTH bug that would shift every following leaf for a negative value, and
it bailed on fixed-OCCURS children the generated codec handles; (b) leading-separate-sign images (would change
`ImageWidth` and every offset computation, buying only debuggability); (c) raw big-endian bytes as Latin-1 chars
(the legacy on-disk form) — a SECOND representation for one concept (the §4.1 incoherence trap), and cross-engine
file compatibility is not required. Excluded — kept loud: float (no fixed decimal width), COMP-5 (`BinaryCapacity`
stores values beyond the PICTURE digit count), INDEX. A whole-group **MOVE between two mixed groups with
positionally IDENTICAL leaf layouts** (same usage/digits/scale/sign leaf-by-leaf) is still emitted as a **memberwise
leaf copy** (`CSharpEmitter.AlignedLeafPairs`, tried FIRST) — for identical layouts the §14.9.25.4 GR4
representation copy and the memberwise copy are indistinguishable, and the memberwise path skips the encode/decode
round trip (the locked NC107A shape: `MOVE U5 TO U9`, `IF U22 > U12`). Non-aligned receivers fall through to
`FromImage` (ST127A's 10→11-leaf MOVE; ST134A's class-view→SD MOVE). Conformance:
`MixedUsageRecordImageDifferentialTests` + NIST ST108A/ST127A/ST133A/ST134A.

### 14.5 Control-flow PC × declaratives × EXIT × STOP RUN/GOBACK

- **Two exceptions, never integer-pc, for termination:** `StopRun` (run-unit, caught at `Main`) and `ProgramReturn`
  (program boundary, caught at `Entry`). **Named loser:** pipeline's "GOBACK → `return;` from the current Dispatch
  level" — a C# `return` exits only the innermost recursive `Dispatch`, so a GOBACK nested in a PERFORM would resume
  the PERFORM caller, not the program's caller. (Verify against any NIST test with GOBACK inside PERFORM.) OO's
  method-return and interprogram's called-program-return are both *what the `Entry` catch does* — not competing
  mechanisms.
- **Declaratives share the pc index space but are unreachable by fall-through:** ONE index space over ALL paragraphs
  (so every pc value agrees), `Main` starts at `EntryParagraphIndex`; a USE handler runs via `Dispatch(declStart,
  declEnd)` from the runtime I/O/error path and returns a `ResumeAction` (§11.2). This is the same `ResumeAction`
  used by RESUME — one mechanism.
  **IMPLEMENTED (DEVLOG 559) with two refinements:** the handler invocation is the generated `__RunUse(id, start,
  handlerEnd)` (a GR2 re-entrancy-guarded bounded `__Dispatch`) called from the generated `__IoCheck` selector
  emitted after every FILE STATUS store — selection is COMPILE-TIME knowledge (the program's USE set), so there is
  no runtime registry for local dispatch and the return is VOID (continue after the failing statement, GR7b; the
  `ResumeAction` form waits for the §11 EC subsystem where RESUME exists). Two settled deviations: (a) the CCVS
  termination-tail accommodation — a trivial exit paragraph followed by a STOP-RUN tail inside the section caps
  `HandlerEndPc` at the exit paragraph (the SQ212A golden's shape; ISO leaves fatal-path behavior implementor-
  specific, §14.6.3); (b) a successful CLOSE resets the connector's open-mode view to none (§9.1.4) — a failed
  OPEN records the ATTEMPTED mode for GR6b "being opened" scoping. **Cross-program GLOBAL dispatch (GR4b) is
  IMPLEMENTED (Phase 1F, IC233A/IC234A)** as a compile-time `__outer` instance-chain walk — no runtime registry
  (the §5.6 registry sketch predates the instance-chain emission; ONE pattern): `__IoCheck`'s fallthrough (no
  local match, GR4a) calls `__outer.__RunGlobalUse(fileKey)`, which examines that container's `USE … GLOBAL`
  declaratives (file scope before mode scope, GR5), on a match runs the handler via its own `__RunUse` — in the
  DECLARING program's instance, its data (§8.4.6.2) — else forwards to ITS `__outer` ("repeated with the next
  higher directly containing source element", GR4b) or stops at the outermost. A contained program with NO local
  declaratives still emits `__IoCheck` + hooks when an ancestor has GLOBAL ones. The §12.4.5.8.4 GR1 NOTE-1
  corollary is implemented with it: a contained program's I-O on an inherited GLOBAL file stores the I-O status
  into the OWNER's (local-name) FILE STATUS item through the `__outer` chain
  (`CSharpEmitter.Call.cs::_callInheritedStatusPlace`).
- **EXIT family is pure pc moves** (§5.2): EXIT PARAGRAPH/SECTION set pc; EXIT PERFORM/CYCLE map to break/continue in
  the inline-PERFORM loop; bare EXIT/CONTINUE are no-ops. They never touch the termination exceptions.

### 14.6 Dispatcher shape: `pc`-variable + `while` loop (pipeline's `goto case` is the loser)

The dispatcher uses `int pc; while ((uint)pc<(uint)N) switch(pc){…} pre-body atExit check` (§5.1). **Named loser:**
pipeline's `goto case N` for fall-through — it cannot express PERFORM-THRU exit detection (no clean named-exit
boundary) and C# forbids `goto` into another switch section. ALTER/computed-GO-TO/GO-TO-out all reduce cleanly to
"set pc" only with a pc variable.

### 14.7 `TryStore` (numeric) ≡ `StoreChecked` (conditions) — ONE method

The numeric design's `TryStore(CobolInt, NumProfile, mode, out stored) → bool` and the conditions design's
`StoreChecked(value, scale, profile, out sizeErr)` are the SAME operation (store + capacity/inexact check, receiver
unchanged on overflow). **Settle on `CobolNum.TryStore`** (returns `bool`, `false` = ON SIZE ERROR). The conditions
emitter calls it; there is no second checked-store.

### 14.8 Runtime class roster (settled — §13)

`CobolString` = single-string (Store/Compare/RefMod/SpliceInto); `CobolStrings` = multi-operand INSPECT/STRING/
UNSTRING. **Named loser:** the data-model design's `CobolString.RefMod/SpliceInto` and the string-ops design's
`CobolStrings.RefMod` — RefMod/SpliceInto live on `CobolString` (single-string); the multi-operand verbs live on
`CobolStrings`. Numeric store/format on `CobolNum`; edited formatting on `CobolEdit`; class predicates on `CobolClass`;
88-membership on `CobolCond`.

### 14.9 Figurative HIGH-VALUE/LOW-VALUE + the byte↔char codepage (settled)

- **HIGH-VALUE → U+00FF (alphanumeric) / U+FFFF (national); LOW-VALUE → U+0000.** This is the single settlement of
  string-ops' open question and intrinsics' decision — alphanumeric uses the single-octet ordinal extreme (preserves
  ASCII/Latin-1 ordering through the ordinal `CobolString.Compare`), national uses the 2-octet extreme.
- **Latin-1 (`Encoding.Latin1`, byte k ↔ U+00kk) is the ONE lossless 8-bit byte↔char carrier** for files (§8),
  REDEFINES Tier-C (§4), and the whole-group image (§14.4). Full custom-ALPHABET/CODE-SET collating fidelity sits on
  the char↔byte boundary deferred to G6; the API seam `CobolString.Compare(a, b, weights?)` is fixed now so call sites
  never change.

### 14.10 Diagnostic numbering (settled for new diagnostics)

New COBOL.NET diagnostics use ONE band: **`COBOLNET07xx`** (conditions), and per-subsystem sub-bands within a single
`COBOLNET`-prefixed scheme (REDEFINES uses `COBOLNET_REDEF_*` *names* mapped into the numeric band). **Named loser:**
mixing the legacy `COBOLxxxx`/`CBLxxxx` codes into new diagnostics. Whether to additionally *reuse* legacy codes for
continuity with existing diagnostic-asserting tests is an owner question (§15) — but the default for new work is the
`COBOLNET` scheme.

### 14.11 Dialect, EC-default, and the differential oracle are threaded consistently

ONE `DialectMode` enum (§2) is the single dialect source for Frontend + Binder + Emit. EC checking is OFF by default
everywhere (§11.1); conditional phrases are always active. The differential harness (§2) uses the legacy as an oracle
until G8 — keeping the legacy build in the test graph for the duration is an owner question (§15).

---

## 15. OWNER-LEVEL open questions (consolidated; the task-named ones verbatim) — **RESOLVED: see §18** (kept for the option analysis / rationale)

> Deduped across all 11 designs. The five the task names explicitly — **REDEFINES representation, standard-vs-native
> arithmetic, file serialization, signed-DISPLAY overpunch, SCREEN/REPORT-WRITER/JSON-XML scope** — appear verbatim.

1. **REDEFINES representation (Tier-C byte[]).** Confirm the 4-tier model (§4) with a PERSISTENT class-scoped `byte[]`
   canonical for genuine mixed-USAGE byte-puns as the accepted realization of "bytes only at a boundary"
   (**recommended** — Tiers A/B stay 100% typed and cover the entire near-term DISPLAY-homogeneous NIST path, so the
   fork is off the critical path), OR mandate rejecting mixed-USAGE REDEFINES loudly (collapse Tier C into Tier D),
   trading real-program coverage (X-over-COMP layouts are common in production) for zero-byte purity.

2. **Standard-vs-native arithmetic.** v1 ships **NATIVE** arithmetic only (`Int128` fixed-point, §6.2 — fully
   conformant as the default, what the 364-NIST corpus uses). `ARITHMETIC IS STANDARD-DECIMAL` (decimal128, 34
   digits) is incompatible with the locked substrate AND with .NET `decimal`. DECISION NEEDED: (a) permanently scope
   COBOL.NET to native arithmetic (**recommended**), or (b) later add a quarantined decimal-float intermediate type
   usable ONLY under `ARITHMETIC IS STANDARD-DECIMAL`. STANDARD-BINARY is spec-obsolete → stays unimplemented. Also
   confirm `DIV_GUARD_DIGITS = 14` against the NIST division-rounding tests in G5.

3. **File serialization format.** The on-disk format for relative/indexed/variable-sequential files is an internal
   framed convention (4-byte little-endian prefix, `0xFF` gaps), NOT a standard interchange format. (Q-file-1) Keep for
   v1 + add a pluggable file-format provider later, or does commercial quality demand an interoperable format (real
   ISAM / GnuCOBOL-compatible)? (Q-file-2) The relative/indexed connector holds the whole file in memory (byte-image
   payload per record, deserialized only on hand-back) — accept the legitimate-confined-file-bytes framing, or switch
   to an all-typed sorted-dictionary-of-typed-records store (a one-line flip)? (Q-file-3) Scope v1 to in-memory load/
   flush vs a later pluggable on-disk B-tree/SQLite backend?

4. **Signed-DISPLAY overpunch convention.** v1 DECIDES IBM-ASCII overpunch (`{`/`}`, `A-I`/`J-R`) with TRAILING
   overpunch as the no-SIGN-clause default (§6.4 — NIST-verified against the legacy). FLAG for owner confirm:
   ASCII-overpunch vs EBCDIC-overpunch (and whether a dialect/target needs the EBCDIC tables) — the convention is
   target-character-set dependent.

5. **SCREEN / REPORT WRITER / JSON-XML scope.** These are designed only to the seam (registers reserved, deferred to
   their own subsystems — §12.3). Confirm they are NOT part of the near-term graded deliverable, and the intended M2/
   M3/M4 ordering (REPORT WRITER is a COBOL-85 feature the legacy fully implements; SCREEN + JSON/XML are 2002/2014).

6. **Program/assembly model.** Compile the whole run unit to ONE assembly (typed fast path available everywhere,
   nested = nested classes — **recommended**) vs support separately-compiled `<name>.dll` programs (the uniform opaque
   ABI becomes mandatory across assemblies, the typed fast path unavailable). This decides how much of the calling
   convention can be fully typed. (Raised by both pipeline and interprogram.)

7. **Managed-pointer naming.** Confirm the carrier may be the typed-native `ManagedRef<T>` (NOT the legacy
   `(byte[],offset,length)` `ManagedPointer`). Keep the PUBLIC name `ManagedPointer` over the typed carrier (the
   owner's prior choice), or rename to `ManagedRef`? The `feedback_managed_pointers` memory text still describes the
   byte form (the abandoned byte-substrate era, pre-DEVLOG 457).

8. **Multiple class inheritance (OO).** v1 restricts to single inheritance (sufficient for the whole corpus) and
   rejects 2+ `INHERITS FROM` bases loudly. When a multi-base program appears: (a) linearize to one C# base + extract
   secondary supers as C# interfaces the class IMPLEMENTS (with member forwarding), or (b) declare it unsupported.

9. **Int128 substrate timing.** `PicInfo.WidePrecision` + `CobolNum`/`CobolInt` `Int128` overloads are needed before
   any 19–38-digit picture compiles. Is there a corpus program needing >18 digits in the early NIST waves (NC/SM/IC/
   IF), or can `Int128` wait until a later wave? (Resolved in the §16 sequence as a checkpoint, not a floating TODO.)

10. **COMP-5 binary-wrap semantics.** Confirm true two's-complement binary-WRAP by width (`PIC S9(4) COMP-5` wraps at
    ±32768) vs digit-count truncation. §6.3 specifies binary-wrap; the current `CobolNum.Store` has a digit-truncation
    TODO.

11. **Whole-group `AsImage` permanence.** Is the generated `string AsImage()` (clean, readable) acceptable as the
    PERMANENT typed-native mechanism for whole-group MOVE/compare, or must mixed-usage (national/COMP-member) groups go
    through the G6 byte path for byte-exact fidelity? (Pure DISPLAY groups are fine via `AsImage`.)

12. **Diagnostic numbering scheme.** Adopt the fresh `COBOLNET`-prefixed scheme for all new diagnostics (the §14.10
    default), or also reuse the legacy `COBOLxxxx`/`CBLxxxx` codes for continuity with existing diagnostic-asserting
    tests?

13. **Differential-oracle dependency.** Is it acceptable for the conformance/CI harness to DEPEND on the legacy
    compiler as a differential oracle until cut-over (G8)? (Keeps the legacy build in the test graph; the alternative
    is `.txt`-oracle-only, losing the free 364-program diff net.)

14. **Fatal-EC termination policy.** ISO §14.6.13.1.3 lets the implementor continue or terminate an unhandled fatal
    EC. Recommendation: terminate the run unit with a diagnostic + nonzero exit (commercial-quality, safest).

15. **Out-of-range / zero / non-integer reference modification.** Default = THROW (raise EC-BOUND-REF-MOD →
    `CobolException`; conformant, since results are otherwise undefined). Offer a lenient dialect that clamps?
    (Owner-gated, mirrors the legacy dialect-strictness model.)

16. **REDEFINES cross-type-read detection precision.** The loud guard (§4.3) needs a "cross-type read" definition that
    does not false-positive on the safe alias pattern. Proposed conservative over-approximation: flag any program that
    both writes AND reads two different-typed views of one redefined region (routes more programs to the byte fallback
    than strictly necessary). Acceptable for v1?

---

## 16. Dependency-ordered implementation sequence (G1–G8, each NIST-testable)

Threads the existing G1–G8 spine; each step ends at a checkpoint testable against the differential harness (§2). The
cross-design prerequisites the subsystem designs flagged are surfaced inline.

**STATUS (2026-06-10, DEVLOG 520+):** G0 ✅ · G1 ✅ · G2 ✅ · G3-core ✅ · G4 ✅ · G5 sequential file I/O ✅
(sequential file I/O done; SET/index machinery + sections landed; relative/indexed/SORT pending) · G6-core ✅
(REDEFINES Tier A+B, AsImage, ON SIZE ERROR, PICTURE P). 33 NC programs byte-match the golden; 348 conformance + 15
unit tests green; default `--std` = COBOL-2023.

### G1 — Bootstrap ✅ (done)
HELLO end-to-end (preprocess→parse→emit C#→Roslyn→run); DISPLAY of literals; STOP RUN.
**Checkpoint:** the differential harness runs at all; `oo_hello`-class trivial programs.

### G2 — Bind + data model + the universal abstractions (the foundation everything else needs)
- **Pre-1 (owner-gated):** resolve **Q9 Int128 timing** and **Q10 COMP-5 wrap** before flipping any typed numeric.
  Add `Int128`/`CobolInt` overloads to `CobolNum` (the monomorphic value engine, §6.1) and `WidePrecision` to
  `PicInfo`.
- Build the **bound tree** (§2) + `ProcedureBinder`.
- Build **`ReferenceResolver` → `Place`** (§3.3/§3.4) — port the legacy SUB_* interpreter; this is the lvalue every
  later verb uses, so it MUST exist before G3.
- **`DataBinder` must stop dropping** REDEFINES, level-66, level-88; build the `ByName` MULTIMAP; collect OCCURS dims
  + INDEXED-BY index-names; add the deferred-resolution pass for REDEFINES/RENAMES/DEPENDING-ON targets.
- **`NumProfile` gains `SignKind`** (§6.4) and `CobolNum` gains `FormatDisplaySigned`/`ParseDisplay` — needed by
  DISPLAY of signed items AND by §4 numeric views.
- Emit nested `record struct` types + composed VALUE initializers + arrays (replace DEVLOG 458's flatten-to-leaves
  stopgap). **Correct the architecture-doc §3 `decimal` rows** (§3.1) in this same change set.
**Checkpoint:** a program with groups/tables/signed-DISPLAY/88s binds and DISPLAYs its data byte-identically to the
legacy (a slice of NC).

### G3 — Core verbs on typed values (everything routes through `Place` + `CobolNum.TryStore`)
- MOVE (incl. JUSTIFIED, numeric↔alphanumeric image), arithmetic (the `CobolInt` engine + `TryStore`, ROUNDED, ON
  SIZE ERROR two-phase §6.6), IF/EVALUATE (pure conditions §11, fully-parenthesized short-circuit), DISPLAY/ACCEPT
  (system sources §12.3 with the injectable clock), inline PERFORM (real loops §5).
- `CobolStrings` INSPECT/STRING/UNSTRING + `CobolString.RefMod`/`SpliceInto` (§7); `CobolClass` class conditions;
  `CobolCond` 88-membership; `CobolEdit` numeric-edited.
**Checkpoint:** the bulk of NC (the arithmetic/MOVE/IF/EVALUATE/PERFORM-inline/INSPECT/STRING NIST programs) green.

### G4 — Control-flow engine (the PC dispatcher)
- `Dispatch(start,exit)` (§5.1, the `pc`-variable+loop shape §14.6); fall-through/GO TO/GO TO DEPENDING/ALTER;
  out-of-line PERFORM/THRU (recursive bounded dispatch); PERFORM TIMES/UNTIL/VARYING(+AFTER reset); EXIT family;
  NEXT SENTENCE; declaratives at low pc indices + `EntryParagraphIndex`.
- **`StopRun` vs `ProgramReturn`** distinct signals (§5.3/§14.5) — even though CALL lands in G7, define both now so
  GOBACK is never miscompiled.
**Checkpoint:** the GO TO/ALTER/PERFORM-THRU/inverted-range NIST programs (NC102A, NC208A, etc.) green.

### G5 — Drive the NIST corpus to green (NC → SM/IC/IF → SQ/RL/IX/ST) ✅ COMPLETE (DEVLOG 575, 2026-06-11)
- Files subsystem (§8): `FileConnector`/`IRecordCodec`/`CobolKey`; sequential → relative → indexed; FILE STATUS,
  AT END/INVALID KEY, USE declaratives (dispatched via §14.5); SORT/MERGE.
- Interprogram (§9): the opaque ABI + typed fast path; LINKAGE/USING/RETURNING; CALL/CANCEL; EXTERNAL/GLOBAL/COMMON;
  pointers (`ManagedRef`/ADDRESS OF/BASED/ALLOCATE).
- Confirm **Q2 `DIV_GUARD_DIGITS`** empirically here.
**Checkpoint MET:** every golden-bearing NIST program locked byte-exact (318 = 93 NC + 29 ST + 32 RL + 40 IX +
23 IC + 69 SQ + 42 IF + 15 SM + 4 RW + 2 OBSQ); the final census is 357/403 GREEN with zero diffs (residue is
golden-less by NIST design). Includes the intrinsic catalog (§12.1), COPY/SM, LINAGE, and the Report Writer
(`COBOLNET_REPORT_WRITER_DESIGN.md`). Goldens that fossilized verified legacy holes were re-baselined to the
ISO-conforming output under the `LEGACY_DIVERGENT` protocol (`scripts/guard.sh` carries the list + citations).

### G6 — Deferred data cases (the byte-boundary islands) ✅ COMPLETE (DEVLOG 572/563)
- REDEFINES/RENAMES 4-tier model (§4) — Tiers A/B (no bytes) first; Tier C resolved as the ZONED DIGIT-IMAGE
  codec (§4.2 — no `RedefCodec` byte plan needed; ISO §13.18.60 GR4 implementor latitude; Q1 thereby settled:
  the island narrowed to float/COMP-5 puns, which stay loud).
- The whole-group **`AsImage()`/`FromImage()`** facility (§14.4) — wired into whole-group MOVE/compare, file
  records, and SORT, including fixed-point BINARY/PACKED leaves.
- File-record serialization edge cases (variable-length §13.18.43 end-to-end with length framing, multi-01
  overlay, SAME RECORD AREA) finalized; CODE-SET remains accepted-inert (no NIST exercise).
**Checkpoint MET:** the REDEFINES/RENAMES/whole-group-compare programs + all deferred file cases green.

### G7 — Per-edition correctness: features AND diagnostics per `--std` (85|2002|2014|2023; default 2023)

> ⛔ **Four compilers in one executable.** Every feature carries TWO co-equal obligations (owner, 2026-06-10):
> (1) the complete ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that
> LACKS it (not-yet-introduced or removed). Gating is driven by `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
> edition-change checklist) and validated by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` — test
> the compiler as N per-edition compilers; Phase 0 done). NIST-85 is the 85 positive corpus; the negative
> (rejected-construct) corpus is NEW.
> ✔ Reconciled (ALTER family, DEVLOG 543): ALTER + the target-less GO TO are 85-only — REJECTED at
> `--std 2002|2014|2023` as deleted elements (COBOLNET0810/0811, matching the ISO history and the legacy
> CBL3601/3605); §18 #10 and the control-flow deep-dive edition table updated in the same change set.

- OO → .NET classes (§10): single-inheritance, NEW/INVOKE/SELF/SUPER, instance vs static scopes, FACTORY, PROPERTY;
  the conformance corpus (`tests/conformance/<ver>/`).
- Full intrinsic catalog (§12.1); EC exception model (§11: `>>TURN`, RAISE/RESUME, USE…EXCEPTION, PERFORM…WHEN);
  national/boolean; UDF; per the `docs/ISO2023_CONFORMANCE_PLAN.md` §3 catalog. Resolve **Q5** scope for SCREEN/RW/
  JSON-XML.
> ✔ **G7 execution detail is now `docs/COMPLETION_ROADMAP_COUNCIL.md` (RATIFIED 2026-07-03, DEVLOG 581;
> ISO-VALIDATED against the spec DEVLOG 582 — 0 refuted claims; its Appendix carries the audit + the
> DEVLOG-582 amendments: boolean operations, `&`-concatenation, CONSTANT entries, DYNAMIC-LENGTH items,
> the §4.2.16 full conformance-documentation set, >>PROPAGATE re-editioned ≤2014)** —
> Phases 0–8 with exit criteria. Ratified amendments to THIS section: **JSON/XML is removed from the ISO
> catalog** (zero hits in the 2023 spec — re-tagged vendor-dialect, deferred post-G8; the Q5 JSON-XML leg is
> thereby resolved, SCREEN = documented non-support per the A.4 line, RW stays as-built); a behavioral leg at
> `--std 2023` (318-golden re-run) attaches to the validator's permissive flip and INV-1-strong at the default
> edition joins the G7 exit criteria; new named workstreams (W1.5 gate-diagnostic upgrades, the silent-misbind
> loud-guard sweep, the 43-row intrinsics backlog, the ~44 VCR Table-1/5 behavior-row wave, discovery runners,
> CI sweep wiring, the ISO §4.2 conformance document).
**Checkpoint:** the conformance corpus + the post-85 dialect-gated NIST programs green.

### G8 — Cut over
Retire the byte engine, drop the legacy from the test graph (resolve **Q13**), rename `CobolSharp`→`COBOL.NET`
(exe `cobol.exe`), final architecture/doc pass.
> ✔ **Ratified refinement (2026-07-03, `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 9):** G8 executes as THREE
> serial cuts — Cut 1 test-graph (Q13 resolved: the ~47 differential files convert to pinned goldens; the CI
> guard step swaps to the in-repo greenfield guard), Cut 2 byte-engine deletion (legacy preserved at a git
> tag), Cut 3 the atomic rename + committed regen + final doc pass + the ISO §4.2 conformance document. A
> greenfield-guard vs legacy-guard **equivalence proof (roadmap Phase 8) is a hard precondition of Cut 1** —
> the legacy must still run when the verdict-diff executes.
**Checkpoint:** full guard green on COBOL.NET alone; the architecture doc + this SSOT reconciled.

---

*End of SSOT. Every subsystem decision in the 11 input designs is captured above; every named conflict is resolved
with the loser stated (§14); the five task-named owner questions appear verbatim (§15 items 1–5). Implementation
follows §16.*

---

## 17. Project organization, rename & code-structure plan

> Scope of this section: the target solution/project layout, the front-end extraction, the rename, the no-god-class structural rules, and the C# 14/.NET 10 usage guidelines. It expands `COBOLNET_ARCHITECTURE.md` §5 (which currently sketches the layout in three bullets) into the decision-complete plan. Implementation status: **G0 is DONE** — the tree now IS `src/Cobol.Net.{Frontend,Compiler,Runtime,Cli}` + `tests/Cobol.Net.Tests.{Unit,Conformance}` (Steps 1–5 below were executed; the tables are kept as the executed record + rationale). The namespace big-bang + legacy deletion (Step 6) lands at **G8** cut-over.

### A. Findings that drive every decision below

These were verified against the live tree (2026-06-08), not assumed:

1. **The front-end is assembly-cleanly separable.** `CobolNet` consumes exactly four legacy namespaces — `CobolSharp.Compiler.Diagnostics`, `.Generated`, `.Parsing`, `.Preprocessor` (plus `.Common` transitively). The dirs `Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/` have **zero** `using` references to the legacy `Semantics`/`IR`/`CodeGen`/`FlowAnalysis` layers, and the front-end does not reference `CobolSharp.Runtime`. So the front-end can be lifted into its own assembly with no code edits to the moved files.
2. **The dependency the task wants killed is an *assembly* dependency, not a namespace dependency.** Moving `*.cs` files into a new `.csproj` ends the new compiler's reference to `CobolSharp.Compiler.dll` **without renaming a single namespace** — namespaces are independent of the project that compiles them. This lets us split "physical move" (G0) from "cosmetic namespace rename" (G8) and stay green throughout.
3. **The legacy byte engine must keep parsing.** `COBOLNET_ARCHITECTURE.md` keeps `CobolSharp.Compiler` alive as a differential oracle until G8. After extraction, the *legacy* engine, the legacy CLI, and **both test projects** (all four currently reference `CobolSharp.Compiler.csproj`) must repoint to the new Frontend assembly for parsing/diagnostics. The front-end namespaces are consumed compiler-wide by the legacy `Semantics`/`IR`/`CodeGen` (every parse-tree `CobolParserCore.*` context; `DiagnosticBag` everywhere) — which is *why* a namespace rename has a wide blast radius and is deferred.
4. **The front-end is more than `.cs`.** It includes the grammar-generation machinery: `Grammar/` + `Grammar/Core/*.g4`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the `ANTLR4/antlr-4.13.2-complete.jar`, the `EnsureGeneratedFiles`/`CleanGenerated` MSBuild targets, and the `Generated/` output. These move as one unit; the generated namespace is set by the generation script and is held constant through the move.

---

### 1. Solution / project reorganization + rename

### 1.1 Target project set (exact names)

| # | Project (assembly) | Kind | `RootNamespace` | `AssemblyName` | Purpose |
|---|---|---|---|---|---|
| P1 | **`Cobol.Net.Frontend`** | library | `CobolNet.Frontend` | `Cobol.Net.Frontend` | Preprocessor + ANTLR lexer/parser + parse-tree + diagnostics. Extracted from `CobolSharp.Compiler`. The single front-end for both the new compiler and (until G8) the legacy oracle. |
| P2 | **`Cobol.Net.Compiler`** | library | `CobolNet` | `Cobol.Net.Compiler` | Bind → lower → emit C# → Roslyn backend. The compiler proper, minus the CLI shell. |
| P3 | **`Cobol.Net.Cli`** | exe | `CobolNet.Cli` | **`cobol`** | Thin command-line driver (`Main`, arg parsing, file orchestration). Produces **`cobol.exe`**. |
| P4 | **`Cobol.Net.Runtime`** | library | `CobolNet.Runtime` | `Cobol.Net.Runtime` | The typed-native runtime the *generated* programs call (`CobolNum`, `CobolString`, `NumProfile`, `ManagedPointer`, file/format helpers). |
| T1 | **`Cobol.Net.Tests.Unit`** | xUnit | `CobolNet.Tests.Unit` | — | Unit tests for the new compiler + runtime. |
| T2 | **`Cobol.Net.Tests.Conformance`** | xUnit | `CobolNet.Tests.Conformance` | — | NIST + post-85 conformance corpus, run against the new compiler. |

**Decision — name form.** Assembly/package/folder names use the dotted product brand **`Cobol.Net.*`** (reads as the product "COBOL.NET"); **root namespaces stay the single token `CobolNet`** (e.g. `CobolNet.Frontend`, `CobolNet.CodeGen`). Rationale: dotted `Cobol.Net.*` is the marketing/NuGet identity; `CobolNet` as the namespace root avoids a clash with the `.Net`/`System.Net` reading and keeps `using CobolNet.CodeGen;` clean. One rule, applied consistently. (Owner may prefer `Cobol.Net` namespaces too — trivially flippable since it is just the `<RootNamespace>` value; not load-bearing.)

**Decision — CLI split (P2/P3).** Today `Program.cs` lives *inside* the exe project, so tests cannot reference the compiler without referencing an exe. Split it: `Cobol.Net.Compiler` (library, everything except the CLI shell) + `Cobol.Net.Cli` (exe, `<AssemblyName>cobol</AssemblyName>`, ~120-line driver). This mirrors the proven legacy `CobolSharp.Compiler`/`CobolSharp.CLI` split and lets the test projects reference a library.

**Decision — Diagnostics/Common placement.** Fold `Diagnostics/` and `Common/` into `Cobol.Net.Frontend` (a `Diagnostics/` folder + a `Common/` folder) for v1. They are small (4 + 3 files), have no independent consumer, and a separate `Cobol.Net.Diagnostics` would be premature. Revisit only if a non-frontend consumer of diagnostics appears.

### 1.2 Folder layout per project (folder = subsystem)

```
src/Cobol.Net.Frontend/
  Cobol.Net.Frontend.csproj          (carries the ANTLR codegen targets)
  Grammar/         CobolParserCore.g4, CobolDialect/OO/JsonXml/Generics.g4, CobolPreprocessor.g4
    Core/          CobolLexer.g4, CobolData/ControlFlow/Expressions/IO/OO/ReportWriter/Screen/SpecialNames.g4
  Generated/       CobolLexer.cs, CobolParserCore.cs, *Visitor.cs  (build output; git-ignored or tracked per current policy)
  Parsing/         CobolParserCoreBase.cs, CobolErrorListener.cs, CobolErrorStrategy.cs, ZeroTokenRewriter.cs
  Preprocessor/    ReferenceFormatProcessor.cs, ConditionalCompilationProcessor.cs, CopyProcessor.cs, NistPreprocessor.cs
  Diagnostics/     Diagnostic.cs, DiagnosticBag.cs, DiagnosticDescriptors.cs, DiagnosticSeverity.cs
  Common/          SourceText.cs, SourceLocation.cs, TextSpan.cs
  ANTLR4/          antlr-4.13.2-complete.jar
  GenerateIfNewer.ps1, Invoke-Antlr4CSharp.ps1
  Pipeline/        Frontend.cs        (the orchestrator — MOVED from src/CobolNet/Frontend/, the one client of all of the above)

src/Cobol.Net.Compiler/
  Cobol.Net.Compiler.csproj
  Binding/         DataItem.cs, DataBinder.cs, PicInfo.cs, (later: FileBinder, LinkageBinder, OoBinder, ConditionNameBinder)
  Lowering/        (G3/G4: the C#-oriented model — control-flow normalization, CORR expansion, etc.; mirrors legacy Lowering/)
  Emit/            EmissionContext.cs, CSharpProgramEmitter.cs, + one emitter per statement-family (see §2)
    Numerics/      NumericExprRenderer.cs (the NumX machinery), ScaleMath.cs
    Conditions/    ConditionRenderer.cs, ComparisonRenderer.cs
  CodeGen/         CodeWriter.cs, RoslynBackend.cs, ReferenceAssemblies.cs, RuntimeConfigWriter.cs
  CompilerDriver.cs                  (the library entry: source path → result; what Program.Main calls)

src/Cobol.Net.Cli/
  Cobol.Net.Cli.csproj               (<OutputType>Exe</OutputType>, <AssemblyName>cobol</AssemblyName>)
  Program.cs                         (Main + arg orchestration)
  CliOptions.cs                      (the parsed-options record — extracted from Program.cs)

src/Cobol.Net.Runtime/
  Cobol.Net.Runtime.csproj
  Numeric/   CobolNum.cs, NumProfile.cs, CobolRounding.cs, (later CobolDecimal.cs)
  Text/      CobolString.cs
  Control/   StopRun.cs
  Pointers/  ManagedPointer.cs       (ported clean at G7)
  Files/     (G6: typed-record ↔ byte serialization at the medium boundary)
```

> The runtime subsystem folders (`Numeric/`, `Text/`, …) mirror `COBOLNET_ARCHITECTURE.md` §3's data-model rows, so a reader maps "COBOL national string" → `Text/` and "USAGE POINTER" → `Pointers/` directly.

### 1.3 Complete item-by-item mapping

| Current item | Verb | Destination |
|---|---|---|
| `src/CobolNet/Program.cs` | **split + move** | `Cobol.Net.Cli/Program.cs` (Main + Run); the `CliOptions` record → `Cobol.Net.Cli/CliOptions.cs`; the compile orchestration body → `Cobol.Net.Compiler/CompilerDriver.cs` |
| `src/CobolNet/Frontend/Frontend.cs` | **move** | `Cobol.Net.Frontend/Pipeline/Frontend.cs` (it *is* the front-end orchestrator; belongs with what it drives) |
| `src/CobolNet/Binding/*.cs` | move | `Cobol.Net.Compiler/Binding/` |
| `src/CobolNet/CodeGen/CSharpEmitter.cs` | **decompose + move** | `Cobol.Net.Compiler/Emit/**` (see §2 for the split) |
| `src/CobolNet/CodeGen/CodeWriter.cs`, `RoslynBackend.cs` | move | `Cobol.Net.Compiler/CodeGen/` (and factor `ReferenceAssemblies`/`RuntimeConfigWriter` out of `RoslynBackend`) |
| `src/CobolNet.Runtime/**` | move (rename project) | `src/Cobol.Net.Runtime/**`, re-foldered (`CobolString.cs`→`Text/`, `StopRun.cs`→`Control/`) |
| `src/CobolSharp.Compiler/Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/`, `Grammar/`, `ANTLR4/`, `GenerateIfNewer.ps1`, `Invoke-Antlr4CSharp.ps1`, the ANTLR MSBuild targets | **extract** | `src/Cobol.Net.Frontend/` (same subfolder names) |
| `src/CobolSharp.Compiler/` remainder — `Semantics/`, `IR/`, `CodeGen/` (the 11 `Cil*`/`*Lowerer`), `FlowAnalysis/`, `Compilation.cs`, `CompilationResult.cs` | **retire at G8** | deleted at cut-over; until then stays as `CobolSharp.Compiler` (the differential oracle), now referencing `Cobol.Net.Frontend` |
| `src/CobolSharp.Runtime/**` | **retire at G8** | the byte engine's runtime; deleted at cut-over (its clean substrates already ported into `Cobol.Net.Runtime`) |
| `src/CobolSharp.CLI/**` | **retire at G8** | replaced by `Cobol.Net.Cli`; until then repointed to `Cobol.Net.Frontend` |
| `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/` | keep (legacy), **add new** | stay until G8 (test the oracle). New `tests/Cobol.Net.Tests.Unit/` + `tests/Cobol.Net.Tests.Conformance/` added in G0; legacy test projects deleted at G8 |
| `tests/nist/`, `tests/conformance/` | **keep in place** | the corpus is compiler-agnostic; the new conformance test project points at it |
| `docs/COBOLNET_ARCHITECTURE.md`, `COBOLNET_DESIGN.md` (this doc), `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` | keep | update §5 of ARCHITECTURE to point here; add a `DOC_INDEX.md` row for `COBOLNET_DESIGN.md` |

### 1.4 Front-end extraction — how, precisely

The new `Cobol.Net.Frontend.csproj`:
- Inherits TFM/lang from `Directory.Build.props` (net10.0 / C# 14) — **do not** re-declare.
- `<PackageReference Include="Antlr4.Runtime.Standard" />` — **no `Version`** (central package management via `Directory.Packages.props`). It needs `Antlr4.Runtime`; it does **not** need `Mono.Cecil` (that was the byte emitter's IL writer).
- No `ProjectReference` (the front-end is self-contained — confirmed: it doesn't reference `CobolSharp.Runtime`).
- Carries the ANTLR generation: copy `EnsureGeneratedFiles` + `CleanGenerated` targets and the `<None Include="Grammar\…">`/jar items verbatim from the legacy csproj; the `Inputs`/`Outputs` paths stay relative so they work post-move.
- `<InternalsVisibleTo Include="Cobol.Net.Tests.Unit" />` (replaces the legacy one) if any internals need testing.

**Namespaces stay `CobolSharp.Compiler.*` through G0–G7.** The moved files are not edited. Consumers reference the new *assembly*; the `using CobolSharp.Compiler.Parsing;` lines in `Frontend.cs` still resolve. The cosmetic rename `CobolSharp.Compiler.* → CobolNet.Frontend.*` is a single mechanical big-bang at **G8**, when the legacy engine is being deleted anyway — so only the new compiler's `using`s need updating, the smallest possible diff. (Doing it at G0 would force-touch all of legacy `Semantics`/`IR`/`CodeGen`, which we are about to delete — wasted churn.)

### 1.5 Ordered git-mv sequence (build + guard green per step)

Each step is a self-contained commit; `dotnet build CobolSharp.sln` (and the guard, once scripts are repointed) is green at the end of each. `git mv` preserves history.

**Step 1 — Extract the front-end (kills the legacy-assembly dependency).**
1. `git mv` `Parsing/ Preprocessor/ Diagnostics/ Common/ Generated/ Grammar/ ANTLR4/ GenerateIfNewer.ps1 Invoke-Antlr4CSharp.ps1` from `src/CobolSharp.Compiler/` → `src/Cobol.Net.Frontend/`.
2. Create `Cobol.Net.Frontend.csproj` (with the ANTLR targets); add it to `CobolSharp.sln`.
3. Repoint the **four** consumers' `ProjectReference` from `CobolSharp.Compiler.csproj` → `Cobol.Net.Frontend.csproj` **and add** a `CobolSharp.Compiler → Cobol.Net.Frontend` reference (the byte engine now consumes the extracted front-end). Consumers: `src/CobolNet`, `src/CobolSharp.CLI`, both `tests/CobolSharp.Tests.*`. (`CobolSharp.Compiler` keeps its `Cobol.Net.Frontend` ref + its own `Mono.Cecil`/`CobolSharp.Runtime` refs for the byte engine.)
4. Build. The new compiler no longer references `CobolSharp.Compiler.dll`. ✅ *Goal "stop depending on the legacy assembly" met here, with zero namespace churn.*

**Step 2 — Rename the runtime.** `git mv src/CobolNet.Runtime src/Cobol.Net.Runtime`; rename the `.csproj`; set `<AssemblyName>Cobol.Net.Runtime</AssemblyName>`/`<RootNamespace>CobolNet.Runtime</RootNamespace>`; re-fold (`Text/`, `Numeric/`, `Control/`); update the `RoslynBackend` runtime-DLL path constant (`CobolNet.Runtime.dll` → `Cobol.Net.Runtime.dll`); update `.sln`. Build + run a HELLO compile. ✅

**Step 3 — Split + rename the compiler & CLI.** `git mv src/CobolNet src/Cobol.Net.Compiler`; create `Cobol.Net.Cli` and move `Program.cs`/`CliOptions` into it; extract `CompilerDriver` into the library; set assembly/root-namespace/`OutputType` per §1.1 (Cli `<AssemblyName>cobol</AssemblyName>`); update `.sln`. Build; `cobol hello.cob --run`. ✅

**Step 4 — Add the new test projects.** Create `tests/Cobol.Net.Tests.Unit` + `tests/Cobol.Net.Tests.Conformance`, referencing the new compiler library + the in-place `tests/nist`/`tests/conformance` corpus; add `InternalsVisibleTo`. Legacy test projects untouched (still guarding the oracle). ✅

**Step 5 — Solution / scripts / CI / props (one commit).**
- Rename `CobolSharp.sln` → `Cobol.Net.sln` (or keep the filename and just update entries — owner taste; recommend rename for brand consistency).
- Update the **five** scripts that hardcode paths: `scripts/guard.sh`, `guard-fast.sh`, `guard-run-group.sh`, `nist-batch.sh`, `run-suite.sh` (the `.sln` name and any `src/CobolNet*`/`CobolSharp.*` project paths and the runtime-DLL copy target).
- Update CI `.github/workflows/build-and-test.yml` (it hardcodes `CobolSharp.sln` and the two `tests/CobolSharp.Tests.*` paths) in this same commit so CI tracks the rename.
- `Directory.Build.props` / `Directory.Packages.props` need **no functional change** (TFM/lang/central-versions are name-agnostic); confirm the package-id list still covers `Antlr4.Runtime.Standard`, `Microsoft.CodeAnalysis.CSharp`, the test packages. (`Mono.Cecil` becomes legacy-only; keep its `PackageVersion` until G8.)
- Full guard green. ✅

**Step 6 — (at G8, not G0) Namespace big-bang + legacy deletion.** Delete `CobolSharp.Compiler` (`Semantics`/`IR`/`CodeGen`/`FlowAnalysis`/`Compilation*`), `CobolSharp.Runtime`, `CobolSharp.CLI`, and the legacy test projects. Rename `CobolSharp.Compiler.* → CobolNet.Frontend.*` across the surviving Frontend files + the new compiler's `using`s (one search-replace, now small because only the new compiler consumes it). Drop the `Mono.Cecil` `PackageVersion`. Update the generation script's emitted namespace. Final guard green. ✅

> **`.sln` / config implications, summarized:** central package management means every new `.csproj` `PackageReference` is **version-less**; `Directory.Build.props` is the single TFM/lang/nullable/warnings-as-errors source — projects never re-declare those; the `.sln`, the 5 scripts, and the CI YAML are the only places project/solution *paths* are hardcoded and must move in lockstep (Step 5).

---

### 2. No god classes — structural discipline

The legacy `CilEmitter` reached 2600 lines before being split into 11 `Cil*Emitter`s sharing an `EmissionContext`. `CSharpEmitter.cs` is already ~790 lines and growing (display + move + add/subtract/multiply/divide/compute + if + perform + conditions + the whole `NumX` numeric renderer + reference resolution). **Decompose it now, in G0, before G2/G3 push it past 1000 lines** — the proven legacy shape is the template, applied pre-emptively rather than as rescue surgery.

### 2.1 Rules (non-negotiable, in PROMPT.md spirit)

1. **Shared state lives in a context object, never a mega-class.** An `EmissionContext` (the `CodeWriter`, the `DataBinder`, the paragraph table, the division working-scale `_targetScale`, the dialect level) is passed to every emitter. Emitters are stateless-but-for-the-context cooperating units — exactly the legacy `EmissionContext`/`LoweringContext` pattern that already works here.
2. **One file per statement-family emitter.** A verb family = a file. Adding a verb = a method in its family's emitter (or a new file for a new family), never a new branch threaded into a shared switch in a 2000-line file.
3. **Respect the bind → lower → emit boundary** (and `feedback_binder_no_ir`): the **binder** produces the typed model (`DataItem`/`PicInfo`) and resolves references; **lowering** (G3/G4) normalizes hard COBOL shapes (CORR expansion, control-flow flattening) into a C#-friendly form; **emit** turns that into C# text. An emitter must not re-discover semantics the binder owns (e.g. category compatibility), and the binder must not emit text.
4. **Dispatch generically, refactor-first** (`feedback_refactor_first_always`): the statement dispatcher routes by node type to the owning emitter; you never add per-caller if-else chains. New variant ⇒ extend the dispatch table, not each call site.
5. **Size is a *smell*, not the law — SRP is the law.** Heuristic thresholds: a class > ~400 lines or a method > ~60 lines triggers a "does this have one responsibility?" review. *But note `CilDataEmitter.cs` is 44 KB even after the split* — data is intrinsically broad; the test is cohesion, not line count. A 500-line class with one job is fine; a 200-line class doing two jobs is not.
6. **Runtime split by concern** (already followed): `Numeric/`, `Text/`, `Control/`, `Pointers/`, `Files/` — never a `CobolRuntime` god class.

### 2.2 Concrete decomposition of `CSharpEmitter` (the class list)

| New class (file) | Responsibility | Lifted from current `CSharpEmitter` |
|---|---|---|
| `EmissionContext.cs` | Holds `CodeWriter`, `DataBinder`, paragraph table (`_paras`/`_paraIndex`), `_targetScale`, dialect. The shared spine. | the private fields scattered today |
| `CSharpProgramEmitter.cs` | Top-level orchestration: class shell, `Main`, the paragraph→method loop. ~80 lines. | `Emit`, the Main/paragraph loop |
| `Emit/Data/FieldEmitter.cs` | DATA DIVISION → C# fields/profiles; VALUE initializers; (G2) group→`record struct`, OCCURS→`T[]`. | `EmitWorkingStorage`, `EmitFieldRecursive`, `InitializerFor`, `UnscaledAtScale`, `ProfileName` |
| `Emit/Statements/DisplayEmitter.cs` | `DISPLAY` (and later `ACCEPT`). | `EmitDisplay` |
| `Emit/Statements/MoveEmitter.cs` | `MOVE` (+ later CORR). | `EmitMove`, `ConvertToTarget`, `SendAsString`, `SendAsNumber` |
| `Emit/Statements/ArithmeticEmitter.cs` | `ADD`/`SUBTRACT`/`MULTIPLY`/`DIVIDE`/`COMPUTE`; GIVING/ROUNDED/SIZE ERROR. | `EmitAdd`…`EmitCompute`, `AssignScaled`, `AssignDivide`, `EmitArithAssign` |
| `Emit/Statements/ConditionalEmitter.cs` | `IF`/`EVALUATE` block splitting + branch emit. | `EmitIf`, `EmitBlocks` |
| `Emit/Statements/PerformEmitter.cs` | `PERFORM` (inline/out-of-line/THRU/TIMES/UNTIL; G4 VARYING/GO TO). | `EmitPerform`, `EmitInlinePerform`, `EmitUntil`, `EmitLoop`, `TimesCount` |
| `Emit/StatementDispatcher.cs` | Routes a `StatementContext` to its owning emitter (the generic switch). | `EmitStatement` |
| `Emit/Numerics/NumericExprRenderer.cs` | The whole `NumX` scale-tracked renderer (`Num`, `NumChain`, `Combine`, `Align`, `UnscaledLit`, `FieldNum`, …). One cohesive unit. | the `NumX` region |
| `Emit/Conditions/ConditionRenderer.cs` | COBOL condition → C# boolean (`RenderCondition`, logical chains). | `RenderCondition` |
| `Emit/Conditions/ComparisonRenderer.cs` | Relational comparison + `MapOperator` + operand string/number rendering. | `RenderComparison`, `MapOperator`, `IsStringOperand`, `OperandAsString`, `OperandNum` |
| `Binding/ReferenceResolver.cs` | `DataReferenceContext` → `DataItem` (name/qualified/subscript). | `Resolve`, `ReadAsString` |
| `CodeGen/CodeWriter.cs` | unchanged (already single-purpose). | — |
| `CodeGen/RoslynBackend.cs` | split: keep `Compile`; extract `ReferenceAssemblies.cs` + `RuntimeConfigWriter.cs` (each already a distinct concern in the file). | — |

The free helpers (`DecodeCobolString`, `CsStringLiteral`, `Children`, `DataRefs`, `FirstToken`) become a small internal `EmitHelpers` static class or move next to their primary user. `RenderLiteralAsString` lives with whoever owns literals (the renderers).

---

### 3. C# 14+ / .NET 10+ feature usage guideline (.NET 11 upgrade pre-authorized when it pays for itself)

**Principle: readability and correctness first, not feature golf.** A modern feature earns its place only when it makes the *emitter* code clearer or safer. Examples below are drawn from constructs already in this codebase.

| Feature | Use it when | Avoid when | In-repo example |
|---|---|---|---|
| `record` / **`record struct`** | immutable value bundles with value equality — bound model, small renderer results | a type with identity/mutable lifecycle | `readonly record struct NumX(string Expr, int Scale)`; `record struct Result(bool, IReadOnlyList<Diagnostic>)` |
| **primary constructors** | a class/struct whose ctor just captures collaborators into fields | when the param needs validation/transformation before storing | `readonly struct BlockScope(CodeWriter writer)`; apply to the new emitters: `sealed class MoveEmitter(EmissionContext ctx)` |
| **collection expressions `[]`** | initializing lists/arrays | — (always clearer than `new List<T>()`) | `List<Para> _paras = [];`, `_copySearchPaths = []` |
| **property / list patterns** | inspecting the bound model without temp vars | deeply nested patterns that out-clever the reader | `is { Pic: { Category: PicCategory.Numeric, IsFloat: false } } t` |
| **switch expressions** | total mapping from one closed set to another | side-effecting branches (use a `switch` statement) | `MapOperator`, `Combine`, `PicInfo.ClrType` |
| **file-scoped namespaces** | every file (one ns per file here) | — | every `.cs` in the project |
| **`using` aliases** | taming a long generated type name | aliasing for brevity's sake | `using Core = CobolParserCore;` |
| **`required` members** | a non-nullable field with no sensible default | optional/defaulted state | `DataItem.Level`/`.CsName` are `required` |
| **raw / interpolated string literals** | emitting multi-line C#/JSON templates | single tokens | the `$$"""…"""` `runtimeconfig.json` in `RoslynBackend` |
| **`static` lambdas** | a closure that captures nothing (signals + prevents capture) | when capture is intended | `.Where(static p => p.EndsWith(".dll", …))` |
| **`params` collections (C# 13+)** | variadic helpers taking spans/lists | — | candidate for `CodeWriter` multi-line helpers |
| **`field` keyword (C# 14)** | a property needing light backing-field logic without declaring the field | trivial auto-props (no gain) | future: a lazily-built profile cache on `PicInfo` |

**Before / after (compiler-relevant):**

*Statement dispatch — switch statement with cohesive arms (keep) vs. an if-else ladder (avoid):*
```csharp
// GOOD — the existing pattern-switch dispatch, one arm per family, easy to extend:
case var _ when s.moveStatement()    is { } m: _move.Emit(m);   break;
case var _ when s.addStatement()     is { } a: _arith.EmitAdd(a); break;
// BAD — a growing if/else ladder in every caller (forbidden by feedback_refactor_first_always)
```

*Result bundle — `record struct` vs. out-params:*
```csharp
public readonly record struct Result(bool Success, IReadOnlyList<Diagnostic> Diagnostics); // GOOD: named, immutable, value-equal
// BAD: bool Compile(..., out IReadOnlyList<Diagnostic> diags)  — positional, easy to misuse
```

*Emitter construction — primary ctor + context (the decomposition target):*
```csharp
internal sealed class ArithmeticEmitter(EmissionContext ctx)   // GOOD: collaborators captured once
{
    public void EmitAdd(Core.AddStatementContext add) { ... ctx.Writer.Line(...); }
}
// BAD: a 790-line CSharpEmitter holding _data, _paras, _targetScale, and every Emit* method.
```

**Standing conventions** (already in force, restated): full XML doc comments on public surface + inline rationale on non-obvious COBOL semantics (with ISO §citations — `feedback_bare_end`); generated C# written to `<name>.g.cs`, always inspectable; `SymbolDisplay.FormatLiteral` for every emitted string literal (never hand-rolled escaping).

---

## 18. Settled decisions (the §15 open questions, resolved)

**OWNER-CONFIRMED 2026-06-08.** All four consequential forks were reviewed with the owner and the **recommended**
option chosen for each: **#1** mixed-USAGE REDEFINES → confined byte[] pun (in-memory program data stays 100% typed);
**#9** control flow → PC-dispatcher for v1, idiomatic "pretty pass" later; **#2** arithmetic → native only
(ARITHMETIC IS STANDARD-DECIMAL out of scope); **conformance** → the differential harness (legacy vs COBOL.NET
identical stdout). The remaining items below stand as the mechanical defaults (owner reviewed the full list).

1. **Byte-at-boundaries (REDEFINES Tier C + file records).** DECISION: in-memory **program data is 100% typed** (no
   `ProgramState`, ever). A `byte[]` exists ONLY at two genuine boundaries, exactly as the owner-co-authored ADR
   already sanctioned ("byte image only as a classifier-scoped fallback for REDEFINES/RENAMES type-puns, file
   records"): (a) the **external medium** for file records, serialized at READ/WRITE via an `IRecordCodec`
   (CODE-SET = the codec); (b) a **tightly REDEFINES-class-confined** canonical `byte[]` ONLY for a genuine
   **mixed-USAGE type-pun** REDEFINES (Tier C). Tiers A/B (homogeneous redefines — the entire near-term NIST path)
   stay 100% typed. This honors "no byte substrate for program data" without rejecting common production layouts
   (X-over-COMP records). **OWNER-VETOABLE:** if you want zero `byte[]` anywhere, mixed-USAGE REDEFINES and
   non-typed file formats are instead rejected loudly (Tier C → Tier D).
2. **Arithmetic mode.** Native arithmetic only (ISO §8.8.1.3) — fully conformant as the default and what the 364
   NIST corpus uses. `ARITHMETIC IS STANDARD-DECIMAL` (decimal128) would require a banned software decimal-float
   type; not implemented (documented). `STANDARD-BINARY` is spec-obsolete — not implemented.
3. **Numeric carrier + division guard.** `long` (≤18 digits), `Int128` (19–38); a picture/intermediate needing
   >38 digits is rejected loudly (outside the 2002/2014/2023 `--std` conformance scope; revisit only if a target needs it).
   `DIV_GUARD_DIGITS = 14`, validated/tuned against the NIST division-rounding tests in G5.
4. **Int128 timing.** Build the `Int128`/`CobolInt` overloads + `PicInfo.WidePrecision` in the wave that first needs
   a >18-digit picture (not the early NC/SM/IC/IF waves) — not earlier.
5. **COMP-5 / BINARY-\*.** True binary-width two's-complement **wrap** (`PIC S9(4) COMP-5` wraps at ±32768) — the
   locked semantics; replace `CobolNum.Store`'s digit-truncation TODO before COMP-5 tests are in scope.
6. **Diagnostics.** A fresh **`COBOLNET-`**-prefixed code scheme (clean slate); the legacy `COBOLxxxx`/`CBLxxxx`
   codes are not reused.
7. **Conformance harness.** YES — depend on the legacy compiler as a **differential oracle** until cut-over (G8):
   run legacy + CobolNet on each NIST program and assert identical stdout. Turns the 364 passing legacy tests into
   a free regression net; the `.txt` oracles remain the ground truth.
8. **Assembly granularity.** One `.g.cs` + one assembly per compilation (multiple/contained programs → nested
   classes, same-assembly direct CALL); a per-unit/by-name option is a later need.
9. **Dispatcher vs pretty output.** v1 always emits the PC dispatcher (correctness first). Lifting provably
   well-behaved (no GO TO/ALTER) paragraphs to idiomatic structured C# is a **post-conformance** "pretty pass",
   deferred.
10. **ALTER / GOBACK / UNTIL EXIT.** ALTER + the target-less GO TO are **85-ONLY**: obsolete in ANSI X3.23-1985
    (accepted at `--std 85`, no failing diagnostic), DELETED by ISO/IEC 1989:2002 → REJECTED at 2002/2014/2023
    (COBOLNET0810/0811) — the earlier "gated ON through 2014" was reconciled against the ISO history (the 2023
    standard has no ALTER; §14.9.17 GO TO has only Formats 1–2). Realization: the D4 per-paragraph mutable field.
    `PERFORM UNTIL EXIT` in scope (`while(true)` + EXIT PERFORM=`break`); main-program `GOBACK`-with-status →
    `ProgramReturn` carrying the status (process exit code).
11. **CALL BY REFERENCE of an irregular receiver.** Array element → C# `ref`; a reference-modified splice →
    promote to BY CONTENT (lenient default), diagnosable under a strict dialect.
12. **Pointer carrier.** A typed `ManagedRef<T>` (managed reference; NOT the abandoned `byte[]`+offset+length form);
    keep the public name **`ManagedPointer`** (owner preference). The `feedback_managed_pointers` memory note
    describes the abandoned byte form (pre-DEVLOG-457) and is updated.
13. **Boundary codec.** `System.Text.Encoding.Latin1` (lossless 8-bit) is the ONE shared boundary codepage constant
    — used by file serialization, REDEFINES Tier C, and the whole-group image. Settled once in `CobolNet.Runtime`.
14. **Figurative HIGH/LOW-VALUE + collating (SHIPPED, DEVLOG 546).** No PCS ⇒ alphanumeric `HIGH-VALUE`=U+00FF /
    `LOW-VALUE`=U+0000 (national U+FFFF / U+0000). With a PROGRAM COLLATING SEQUENCE they are the sequence's
    EXTREME characters (ISO §8.3.3.6 GR6/7 + §12.3.7 GR8/9 — character identity, ties: highest→last-specified,
    lowest→first-specified). The custom-`ALPHABET` subsystem is LIVE: `CollatingTable` (256-entry position table,
    §12.3.7 GR7 k1–k6 incl. the k3 distinct-ascending unspecified tail), built in `DataBinder.Switches`
    (`Alphabets`/`Collating`), rendered as the generated `__COLLATE` field, consumed by the settled seam
    `CobolString.Compare(a,b,weights)` at every relation/condition-name comparison site (§12.3.6 GR11) and by the
    PCS-aware figurative fills (`EmissionContext.FigFill`). SORT/MERGE keys + CHAR/ORD take the same table when
    those subsystems land (GR13/GR5 precedence).
15. **Reference modification out of range.** Throw (`EC-BOUND-REF-MOD` → `CobolRuntimeException`) by default; a
    lenient clamping dialect is a later option.
16. **Exception model default.** EC checking OFF by default (NIST-faithful, fast — ISO §5000), enabled only by
    `>>TURN`/phrases; an unhandled **fatal** EC terminates the run unit with a diagnostic + nonzero exit.
17. **VALIDATE.** Implemented minimally for the conformance corpus, marked obsolete (2023 Table 13).
18. **OO multiple inheritance.** Single inheritance in v1 (covers the whole current corpus); a multi-base program
    triggers an owner decision then (linearize one C# base + `IMPLEMENTS` interfaces with forwarding).
19. **Intrinsic internals.** Never `decimal` — exact via unscaled `long`/`Int128`, float via `double` (extends the
    owner's decimal/BigInteger ban into intrinsic internals).
20. **RETURN-CODE.** ONE synthesized `static long`, written by CALL RETURNING / GOBACK GIVING, read as the process
    exit code (a single cross-subsystem owner, not duplicated).
21. **Whole-group-as-alphanumeric.** A generated `string AsImage()` / `FromImage()` per record struct is the
    PERMANENT typed-native mechanism for whole-group MOVE/compare of **DISPLAY-homogeneous** groups; a group with a
    COMP/COMP-3/COMP-5/float (non-character) leaf is the genuine mixed-USAGE byte-island routed to the Tier-C/file
    codec (#1). **Numeric-DISPLAY leaves are INCLUDED in the AsImage path (DEVLOG 490, spec-grounded):** ISO §14.9
    MOVE GR4 fills a group with no conversion (a numeric-DISPLAY subordinate may legitimately hold spaces — using it
    numerically is then incompatible data, §14.6.13.2), so a numeric-DISPLAY leaf *under a whole-referenced group* is
    stored as its character image (`DataItem.StoreAsImage`; numeric use via `CobolNum.ParseDisplay`/`FormatDisplay`) —
    byte-faithful with NO byte[]. A numeric-DISPLAY leaf never referenced as part of a whole group stays a native
    `long` (invariant #2).
22. **Doc reconciliation.** `COBOLNET_ARCHITECTURE.md` §3's `decimal` rows are corrected to native
    `long`/`Int128`-unscaled in the same change set that lands this document (no second SSOT).
23. **Selectable codegen backend (owner-confirmed 2026-06-08).** Codegen is behind an `ICodeGenBackend` abstraction
    over the backend-neutral bound tree, selectable via `--backend roslyn|cil` (default **roslyn**): RoslynBackend
    (idiomatic C# source, the v1 deliverable) and CilBackend (typed-native CIL via Mono.Cecil, future-additive —
    no C#-compile step / no Roslyn dependency, for AOT/direct-IL). The CIL backend's structure→branch lowering is
    private to it; there is no shared lowered IR (§1.1). The two backends can cross-check each other in the
    differential harness.
