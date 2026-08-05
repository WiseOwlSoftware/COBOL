# COBOL.NET — Data Model (records, tables, references) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §3; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

DECISION-COMPLETE DESIGN: COBOL DATA DIVISION → typed-native C#, for COBOL.NET.

== 0. THE CENTRAL IDEA: every reference is a Place ==
The whole subsystem is organized around ONE abstraction — a `Place` — that names a typed C# location and serves MOVE, arithmetic, file I/O, and CALL-by-reference identically. This replaces the legacy `(byte[],offset,length)` IrLocation. A Place has two emission methods used by every consumer (the RoslynBackend's rendering of the Place's backend-neutral structured resolution — see the §11 backend note / SSOT §18 #23):
  • `string ReadExpr()`   — a C# rvalue expression of the item's CLR type (e.g. `WsRec.Count`, `Tbl[i-1]`, `Name.Substring(s-1, l)`).
  • `string WriteStmt(string rhs)` — a C# statement that stores `rhs` into the place (e.g. `WsRec.Count = …;`, `Tbl[i-1] = …;`, a runtime ref-mod splice call).
Because both `record struct` members and array elements are addressable lvalues in C#, a Place composes by string concatenation of a *member access path*. The Place is built by ONE resolver, `ReferenceResolver.Resolve(DataReferenceContext) → Place`, that consumes the parse tree's base-word + flat suffix list once and is the single entry point for every operand.

This is the load-bearing decision: the legacy compiler had a byte offset model that every verb had to special-case; here every verb just calls `Read()`/`Write()` on a Place. Owner constraint satisfied: no byte[], no offsets — the .NET member path IS the storage.

== 1. THE SHAPE: groups → record struct, elementary → field, table → array ==
A 01/77 item becomes a STATIC FIELD (program data) or INSTANCE FIELD (OO object data) of a generated C# type. A group becomes a nested `record struct` TYPE (named `_T_<csname>`) declared at class scope; the group item itself is a field/member of that struct type. An elementary item is a member of the struct type whose C# type is its PicInfo.ClrType (string/long/decimal-free→long/Int128/float/double/bool). A fixed OCCURS n is an array member `T[]` of length n. Nested groups nest structs; an OCCURS group is `_T_Row[]`.

Example:
  01 WS-REC.            ->  record struct _T_WsRec { public string Name; public long Count;
     05 NAME PIC X(10).                                public _T_Item[] Items; }
     05 COUNT PIC 9(4). ->  record struct _T_Item { public long Val; }
     05 ITEMS OCCURS 3.     private static _T_WsRec WsRec = new _T_WsRec { Name=new string(' ',10), Count=0L, Items=…3… };
        10 VAL PIC 9(2).
Member access falls straight out: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2-1].Val`.

== 2. THE HARD PART: parse-tree shape forces a two-phase resolver ==
The grammar (CobolParserCore.g4 §336-403) gives `dataReference: cobolWord dataReferenceSuffix*` where each suffix is subscriptPart | refModPart | qualification, AND subscript/ref-mod content is captured as a RAW token stream `subToken+` (SUBSCRIPT lexer mode) that the binder must re-parse — the grammar deliberately does NOT distinguish a subscript list `(I J)` from a ref-mod `(s:l)`; the presence of SUB_COLON decides. So `Resolve` runs two phases:
  Phase A (syntactic flatten): walk suffixes into an ordered list of {Qualifier names}, {Subscript token-groups}, {RefMod token-group}. A `(...)` group is a ref-mod iff it contains SUB_COLON; else it is a subscript list (split on SUB_WS/SUB_COMMA). This mirrors legacy ExpressionBinder's SUB_* token interpretation — reuse that exact tokenizing logic (it is proven over 364 NIST tests), porting it onto the new DataItem tree.
  Phase B (semantic resolve): resolve the base name + qualifiers to a DataItem via right-to-left narrowing, build the member-access Place, attach subscripts to the OCCURS levels (outer→inner), then wrap in a ref-mod Place if present.

== 3. QUALIFIED RESOLUTION (X OF Y OF Z), ISO §8.4.2.2 ==
Algorithm (port legacy ResolveQualifiedName): resolve the rightmost qualifier Z as a 01/standalone, then FindChild(Z,Y), then FindChild(ctxY, X) — successively LESS inclusive. FindChild searches the group subtree (recursive). A bare unqualified name resolves by: unique-in-program → that item; else require qualification (diagnose CBL-ambiguous if >1 candidate and no qualifier — ISO §8.4.2.2 rule: uniqueness must be established). The DataBinder.ByName index is a MULTIMAP (`Dictionary<string, List<DataItem>>` — IMPLEMENTED, `src/Cobol.Net.Compiler/Binding/DataBinder.cs`) because COBOL permits duplicate names disambiguated only by qualification. The Place records the full member path (Z.Y.X chain) so emission is `Z.Y.X`.

== 4. SUBSCRIPTING / INDEXING, ISO §8.4.2.3 ==
OCCURS dimensions are collected by walking item→ancestors (legacy LocationResolver does exactly this). COBOL subscripts are 1-BASED and listed OUTER→INNER (`T(outer, inner)`); C# arrays are 0-based — so each subscript emits as `[expr - 1]`. Multi-dim: COBOL-85 caps a table at 7 dimensions (the 3-dim cap was ANSI-74, out of scope); 2002+ removes the fixed cap — store dims as a list, no fixed cap; >7 dims at `--std 85` ⇒ diagnostic (G1, per-edition gating section). Each dimension is a SEPARATE C# array index because a 2-D OCCURS is an array-of-structs-containing-array (`Rows[i-1].Cols[j-1]`), NOT a flattened 1-D — this is the natural .NET shape and removes all the legacy multiplier/stepSize offset arithmetic.
  • Subscript forms: integer literal, data-name, index-name, and relative `index ± literal` (ISO §8.4.2.3) → `idx ± lit - 1`.
  • INDEX-NAMEs (INDEXED BY): an index-name is a DISTINCT entity, NOT a data item (ISO §8.4.2.3 / §13.18.38). DECISION: an index-name → a C# `long` field holding a 1-BASED OCCURRENCE NUMBER (not a byte displacement; the legacy byte-displacement model is rejected as it leaks layout). SET idx TO n → `idx = n`; SET idx UP/DOWN BY k → `idx ±= k`; using idx as a subscript → `[idx - 1]`. (Rationale: occurrence-number semantics are layout-free and make SEARCH/SEARCH ALL emit as plain integer loops; the only observable difference — idx surviving a redefine of the table element width — is implementor-defined and not in the conformance corpus.) Index-name lives in the same static/instance scope as its table.

== 5. REFERENCE MODIFICATION x(s:l), ISO §8.4.3.3 (general rules §8.4.3.3.4; spec lines 6952-7020) ==
A typed substring over the item's CHARACTER image. DECISION: ref-mod always operates on the STRING image of the item (ISO §8.4.3.3.4 rule 2: a non-alphanumeric DISPLAY item is treated as if redefined alphanumeric of the same size; rule 3: NATIONAL likewise). So:
  • Read: `CobolString.RefMod(<charImage>, s, l)` → `charImage.Substring(s-1, lengthOrRest)`; l omitted → to end (ISO §8.4.3.3.4 rule 5c: default length = remaining). `<charImage>` is the item's display image (string field directly; a numeric item via `CobolNum.FormatDisplay` first — but a numeric ref-mod is rare and the spec says treat-as-alphanumeric-redefinition, so we render the raw digit/zoned image).
  • Write (ref-mod as a RECEIVER, the genuinely hard case): cannot reassign a substring in place on an immutable C# string. Runtime helper `CobolString.SpliceInto(ref string field, int start1, int len, string value)` rebuilds the string: `field = field[..(s-1)] + value-fitted-to-len + field[(s-1+len)..]`. The Place's WriteStmt for a ref-mod emits this. For ref-mod over a numeric/COMP item used as a receiver, route through the byte-image fallback (G6, deferred) — flag loud meanwhile. s and l are arbitrary arithmetic expressions (evaluated once into temps to avoid double-eval; ISO requires single evaluation of subscripts/positions).
  • REF-MOD OVER A **VALUE** THAT HAS NO PLACE (ISO §8.4.3.3.3 SR2 — the result of a function-identifier; fix-queue PB8). `RefModPlace` decorates a `Place`, and a function result is not storage, so the value form attaches instead as `RefModSpec` on `BoundIntrinsicCall.RefMod` and renders through the SAME `CobolString.RefMod` — one slicer, so the §8.4.3.3.4 item-5c bounds check and EC-BOUND-REF-MOD are shared rather than re-implemented. It is READ-ONLY by construction (§8.4.3.2.3 SR1 makes a function-identifier a non-receiving operand), so there is no `SpliceInto` counterpart to write. A USER-DEFINED function's result is different and needs nothing new: it is already a real `Place` (the §8.4.3.2.4 GR1 caller temp cloned from the RETURNING item), so it goes through `RefModPlace` exactly as a data item does. **A RIDER ON THE CALL NODE, NOT A WRAPPER AROUND IT** — the alphanumeric string channel is selected by pattern-matching `BoundComputedOperand { Expr: BoundIntrinsicCall }` at several sites (`OperandText.AsString`, the nested-argument visitor, `IntrinsicArgumentRules`, `EcBinder`), and a wrapper node would have silently stopped matching at every one, whose failure mode is a DROPPED ref-mod rather than a compile error. §8.4.3.3.4 GR6 preserves class and category for the three categories SR2 admits, so the rider leaves `ResultCategory` correct with no extra rule.
  • BOTH SOURCE CARRIERS REDUCE THROUGH ONE READER. The lexer decides at the `(`, and the decision is frozen there: a ref-mod arrives either as the parsed DEFAULT-mode `refModPart` or as a SUBSCRIPT-mode captured token group with a depth-0 `SUB_COLON`. `ReferenceResolver.ReadRefMod` has an overload per carrier and both return `RefModSpec`, so "how a ref-mod's start and length are read off the source" is written down once. Which carrier a given shape uses is pinned by `CobolLexerModeDriftTests`, because the PB8 fix depends on it: a ref-mod after a FUNCTION name or after an argument list's `)` is DEFAULT-mode, and widening the SUBSCRIPT trigger would silently break the `functionCall` ref-mod tail.
  • ZERO-LENGTH ref-mod (l=0) is EDITION-VARYING (`VERSION_CHANGE_REFERENCE.md` #30): pre-2023 the result is undefined; at `--std 2023` it is allowed (yields "") ONLY when the REF-MOD-ZERO-LENGTH directive (§7.3.23) is in effect — otherwise EC-BOUND-REF-MOD is raised; FLAG-14 flags the ambiguous case (spec line 4523). Gate the emit by edition + directive state.

== 6. NATIVE NUMERIC MODEL (owner-locked, reaffirm) ==
Fixed-point = native `long` holding the UNSCALED value; scale is compile-time metadata on PicInfo (already implemented). 19-38 digit pictures → `Int128` (PicInfo gains a `WidePrecision` flag selecting Int128 vs long for ClrType + the runtime overloads; CobolNum must gain Int128 overloads — currently long-only). COMP-1/2 → float/double; COMP-5 → native int by width with binary wrap (PicInfo.StorageWidth already computes the byte width; runtime needs the wrap path, deferred). decimal/BigInteger essentially unused. This is settled; the data-model design only needs to thread `WidePrecision` into ClrType, DefaultInitializer, ProfileInitializer, and the NumX scale-tracking expression type so wide items pick Int128 literals (`123` not `123L`).

== 7. LEVELS 66 (RENAMES) and 88 (condition-names) ==
  • 88 condition-name: NOT a storage item — a named boolean predicate over its parent (the conditional variable). DECISION: emit each 88 as a C# `static bool` PROPERTY (or a method) over the parent Place: `private static bool LvlOk => CobolCond.In(Parent.Read(), <value-or-range-set>);` where the value set comes from the (possibly multi-valued, THRU-ranged) VALUE clause. SET cond TO TRUE → assign the parent its first/low value (ISO §14.9.39.4 GR6). The binder captures 88 entries as `Condition88` records on their conditional variable (IMPLEMENTED — `DataBinder.Conditions` multimap); ensure the captured VALUE list covers THRU ranges + multiple literals.
  • 66 RENAMES: a re-grouping alias over a contiguous run FROM..THRU of sibling elementary items. DECISION: model as a Place that is an ALIAS — for the common case (RENAMES of a single elementary, or a whole-group read/write) emit a computed property that concatenates/splits the underlying members' char images. The general overlapping-bytes RENAMES is a storage-overlay case → defer to G6 (the byte-image fallback) and flag loud. Capture RenamesInfo (FROM/THRU + qualifiers) now; resolution is deferred-pass like legacy.

== 8. REDEFINES — the storage-overlay boundary ==
> **Canonical REDEFINES design.** The 4-tier one-canonical-backing model below is the design; the SSOT is
> `COBOLNET_DESIGN.md` §4 and the deep-dive `COBOLNET_REDEFINES_DESIGN.md`. A write through one view must be visible
> through every other view of the same storage — two independent typed fields cannot guarantee that, which is why a
> single stored canonical + computed accessors (not separate fields) is used.

REDEFINES makes two differently-typed views share storage. **Model (4 tiers; priority cascade D>C>B>A — see
`COBOLNET_REDEFINES_DESIGN.md`):** a "redefines class" (all entries over one storage area) has exactly ONE *stored*
backing — the **canonical** — and EVERY other view is a **computed accessor (a C# property/`Place`)** over it; never
two stored fields per area, so a write through any view is coherent across all.
  • **A — Alias** (identical PIC+USAGE / RENAMES-without-THRU): one typed field; other names are pass-through accessors.
  • **B — StringCanonical** (whole class is USAGE DISPLAY — the dominant case, and the ENTIRE near-term NIST path):
    canonical = ONE `string` of class-max width (a DISPLAY item's byte image IS its characters); each view is a typed
    accessor (substring / `ParseDisplay`→long / `FormatDisplay`/`CobolEdit`) over it. NO bytes.
  • **C — ByteCanonical** (genuine mixed-USAGE pun observing COMP/COMP-3/5/INDEX cross-view): canonical = ONE
    *class-scoped* `byte[]` (SYNC-aware offsets), each leaf a typed get/set codec over `(offset,length,usage)`. The
    byte image is confined to the REDEFINES class, never the record, never persisted (owner decision §18 #1).
  • **D — Reject loud** (object/pointer/strongly-typed/variable-length puns — already spec-illegal): a diagnostic.
The redefining view emits NO stored VALUE field (init only from the original, REDEFINES SR9); a numeric view still
emits its `_P_` NumProfile (an arithmetic target must compile). FILLER under a REDEFINES needs no field. Tier selection
(`DataBinder.ClassifyRedefinesClasses` → `ComputeTier`) runs a transitive-closure over each redefines class, verdicted
to the A⊑B⊑C⊑D lattice (join = max tier). The one-canonical-backing model keeps Tiers A/B 100% typed (no `byte[]`)
while staying coherent, and confines bytes to genuine mixed-USAGE puns only.

== 9. CLAUSES: SYNCHRONIZED / JUSTIFIED / BLANK WHEN ZERO ==
  • SYNCHRONIZED: alignment is a BYTE-LAYOUT concept. In a typed-native model there are no byte boundaries to align to (a `long` field is naturally aligned by the CLR). DECISION: SYNC is a NO-OP for in-memory typed data; it only matters at the file/byte serialization boundary (G6), where the record-image builder honors it. Capture IsSynchronized for that future use. (Rationale: the only observable effect of SYNC absent byte access is on REDEFINES-overlay size, which is already the G6 byte path.)
  • JUSTIFIED RIGHT: already threaded — CobolString.Store(value,width,justifiedRight) pads/truncates on the left. `DataItem.Justified` carries the flag; the Place's WriteStmt for an alphanumeric receiver passes it.
  • BLANK WHEN ZERO: an OUTPUT/edit-time rule (ISO §13.18.8) — a numeric/numeric-edited item displays as all spaces when its value is zero. DECISION: a property of the item's display rendering, applied in CobolNum.FormatDisplay / the numeric-edited formatter: if value==0 emit spaces of the picture width. Capture BlankWhenZero on PicInfo.

== 10. VALUE INITIALIZATION incl. TABLES, ISO §13.18.63 / §14.9.4 ==
Default (no VALUE): alphanumeric→spaces, numeric→0 (unscaled), index→1, pointer→null. Already implemented for flat items. Extensions needed:
  • Group with VALUE: initialize every subordinate per the group literal spread across the group's char positions (rare; the common form is per-elementary VALUE). 
  • OCCURS with VALUE: ISO permits VALUE on a table item → every element initialized to that value (Format 1 GR9). Emit a collection-expression / array initializer: `Items = [.. Enumerable.Repeat(elemInit, n)]` or an explicit `new _T_Item[]{ init, init, … }`. The **Format 2 (table) VALUE** (ISO §13.18.63.2, COBOL-2002) is NOT a bare one-literal-per-element positional list — it REQUIRES a `FROM (subscript-1)` phrase (optional `TO (subscript-2)`) keying a literal list to occurrence RANGES with cyclic reuse (GR13), no-TO = fill to the maximum (GR14), later-FROM-wins on overlap (GR15), and the GR16 dynamic-capacity computation. Emit per-occurrence: a fixed table becomes `new _T_Item[]{ ElemInit(1), …, ElemInit(n) }` (default outside every range — §13.18.63.4 leaves those occurrences UNDEFINED, so this is the implementation default, not a spec guarantee), a dynamic table a `CobolDynTable` opened at the GR16 initial capacity with a per-occurrence seed. Landable today = a single-dimension table on its own OCCURS entry; a multi-dimension odometer or a subordinate-item table VALUE is staged (COBOLNET0899, P14 GAP).
  • Figurative constants (ZERO/SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/ALL "x"): map to the typed default of the right width — ZERO→0L or "0000", SPACE→new string(' ',w), ALL "AB"→repeat to width. Capture FigurativeInit + AllLiteralPattern (legacy already models these; port).
  The whole 01 field's initializer is a single C# object-initializer expression composed recursively from the leaves — emitted in the static field declaration (program) or the instance ctor (OO), one initializer per instance.

== 11. THE PLACE MODEL — concrete C# emission contract ==
`abstract record Place { abstract string Read(); abstract string Write(string rhs); PicInfo Pic; bool IsNumeric/IsAlpha; }`

> **Backend note (dual backend, SSOT §18 #23).** The backend-NEUTRAL content of a `Place` is its STRUCTURED
> resolution — the member-path segments, the bound subscript expressions, the ref-mod start/length bound
> expressions, the REDEFINES tier/backing, and the bound `DataItem`/`PicInfo`. The `Read()`/`Write()` C#-text
> methods are the RoslynBackend's RENDERING of that structure; the future CilBackend (behind `ICodeGenBackend`,
> `--backend roslyn|cil`) consumes the same structured `Place` and performs its OWN private lowering — it never
> sees the C# strings. Keep the structure primary and the string emission in the renderer; never let a bound
> node carry a pre-rendered C# fragment where the structured form is feasible (G4 discipline).
Concrete kinds:
  • MemberPlace(path)        Read=`path`            Write=`path = rhs;`           (qualified+nested+array indices folded into one access string)
  • RefModPlace(inner,s,l)   Read=`CobolString.RefMod(inner.Read(),s,l)`  Write=`{var t=inner.Read(); inner.Write(CobolString.SpliceInto(t,s,l,rhs));}`
  • Condition88Place(parent,valueset)  Read=`CobolCond.In(parent.Read(),…)`  Write(true)=set parent to value.
Every verb emitter (MOVE/ADD/COMPUTE/file READ INTO/WRITE FROM/CALL USING) takes Places and never touches layout — the unification the task demands. CALL BY REFERENCE passes the receiver Place's address: since a `record struct` member or array element is a C# variable, emit `ref` (e.g. `Sub(ref WsRec.Count)`) for BY REFERENCE; BY CONTENT copies the Read(). (A ref-mod or 88 receiver cannot be passed by ref → diagnose or pass by content per ISO.)

== 12. SUMMARY OF REQUIRED CHANGES TO EXISTING CODE ==
> **Note.** The list below is the DATA DIVISION design inventory; most of it is implemented in
> `src/Cobol.Net.Compiler` (`Place`/`ReferenceResolver`, nested record-struct emission, the `ByName` multimap,
> `Condition88` capture, `WidePrecision`/Int128). The live remaining-work tracker is the plan's §0 (`docs/COBOLNET_REARCHITECTURE_PLAN.md`), not this
> section.
DataItem: add IsJustifiedRight, IsSynchronized, BlankWhenZero, RedefinesName/Redefines, RenamesInfo, OccursInfo (min/max/dependingOn/indexNames/keys), IsGroup-aware ClrType using `_T_` struct names + array `[]`, Level-88 ValueSet, FigurativeInit/AllLiteralPattern, WidePrecision. DataBinder: stop skipping 66/88; capture all clauses; build a name MULTIMAP; collect OCCURS dims + INDEXED BY index-names as their own entities; deferred-resolution pass for REDEFINES/RENAMES/DEPENDING-ON targets. PicInfo: WidePrecision (Int128), BlankWhenZero, IsJustifiedRight; ParseUsage already covers usages. New `ReferenceResolver`(→Place) + the suffix flattener (port legacy SUB_* interpretation). Emitter (`CodeGen/DataDivision/{DataEmitter,RecordStructEmitter}`): emit nested record-struct TYPES + composed initializers rather than flattening groups to leaves; route every operand through Place. Runtime: CobolString.RefMod/SpliceInto, CobolCond.In, Int128 overloads of CobolNum, COMP-5 wrap.

## Decisions

### D1. A single `Place` abstraction (Read()/Write() emitting C# rvalue/lvalue text) is the universal lvalue model, built by ONE ReferenceResolver and consumed identically by MOVE, arithmetic, files, and CALL.

**Rationale.** The task explicitly requires one lvalue model that serves all consumers identically; record-struct members and array elements are genuine C# lvalues so a member-access path composes by string concatenation, eliminating the per-verb special-casing the legacy byte-offset model forced.

**Rejected alternatives.** (a) Keep the legacy IrLocation (Area,Offset,Length) — rejected: it IS the byte substrate the owner abolished. (b) Emit a runtime accessor object per reference — rejected: defeats idiomatic/readable C# and adds indirection the JIT must undo.

### D2. Multi-dimensional OCCURS maps to array-of-struct-containing-array (Rows[i-1].Cols[j-1]), one C# index per COBOL subscript — NOT a flattened 1-D buffer with stepSize multipliers.

**Rationale.** This is the natural .NET shape, layout-free, and removes all offset arithmetic; subscript emission is a uniform [expr-1] per dimension.

**Rejected alternatives.** Flattened 1-D long[] with computed multipliers (the legacy ComputeMultipliers/stepSize model) — rejected: reintroduces byte-offset arithmetic and is unreadable.

### D3. Index-names (INDEXED BY) are C# `long` holding 1-based occurrence numbers, not byte displacements.

**Rationale.** Occurrence-number semantics are layout-independent, make SET / SEARCH emit as plain integer ops, and produce readable code; the displacement model leaks table-element width into a program-visible value.

**Rejected alternatives.** Legacy byte-displacement index — rejected: layout leak, and undefined under element-width redefines (not in the corpus).

### D4. Level-88 condition-names emit as C# bool properties over the parent Place; level-66 RENAMES emit as alias properties (overlapping-byte RENAMES deferred to G6).

**Rationale.** An 88 is a predicate, not storage; a property is the idiomatic, zero-storage encoding and SET TO TRUE just assigns the parent. RENAMES of single/whole items composes from member images; only the overlapping-byte case needs the byte fallback.

**Rejected alternatives.** Materializing 88s as stored booleans kept in sync on every parent write — rejected: redundant state, sync bugs, non-idiomatic.

### D5. REDEFINES uses the 4-tier ONE-canonical-backing model (A Alias / B StringCanonical / C class-scoped ByteCanonical / D reject-loud); every non-canonical view is a computed `Place` accessor over the single backing — never two stored fields per storage area. [Canonical: `COBOLNET_DESIGN.md` §4 + `COBOLNET_REDEFINES_DESIGN.md`]

**Rationale.** A write through any view must be visible through every other view of the same area (ISO §13.18.44 "same storage area"). One stored canonical + computed accessors guarantees that coherence with NO byte substrate for the dominant DISPLAY-homogeneous case (Tiers A/B — the entire near-term NIST path), and confines `byte[]` to genuine mixed-USAGE puns only (Tier C, owner decision §18 #1).

**Rejected alternatives.** (a) **Separate independent typed fields** — rejected: two independent fields cannot stay coherent under a cross-type pun, reproducing the silent-stale-read the no-byte-substrate model exists to prevent — even a loud cross-type-read guard only *detects* it, it does not make the program correct. (b) Global `byte[]` for all REDEFINES — rejected: the abolished substrate. (c) `[StructLayout(Explicit)]`/`[FieldOffset]` overlay — rejected: cannot overlay a `string` on a `long`, which is the dominant pun.

### D6. SYNCHRONIZED is a no-op for in-memory typed data; honored only at the file/byte-serialization boundary (G6). BLANK WHEN ZERO and JUSTIFIED are display/store-time rules on PicInfo.

**Rationale.** Alignment has no meaning without byte addresses (the CLR aligns a `long` naturally); its only observable effect is on overlay/serialization size, which is already the byte path. BLANK-WHEN-ZERO/JUSTIFIED are pure value-rendering rules.

**Rejected alternatives.** Modeling SYNC by inserting padding fields — rejected: reintroduces layout and is invisible to typed access.

### D7. Fixed-point stays native long (unscaled, compile-time scale); 19-38 digits → Int128 via a WidePrecision flag; no decimal/BigInteger.

> **Refinement.** The wide selector is the DERIVED property `PicInfo.IsWide`
> (`Numeric && !IsFloat && Digits > 18`) driving `ClrType`/`DefaultInitializer` — not a stored flag (`Digits` is
> already on PicInfo; a parallel flag could drift from it). Literals wider than long emit `Int128.Parse("…")`
> (`EmitCore.IntLiteral` — C# has no Int128 literal form); the store boundary narrows via the width-aware
> `ArithmeticEmitter.Narrow`. The SURFACE cap is per-edition (18 at `--std 85`, 31 at 2002+, >31 rejected everywhere —
> `EditionContext.CheckDigitCapacity`, COBOLNET0801/0802); Int128's 38 digits are substrate headroom only.

**Rationale.** Owner-locked: hardware-native, exact, DISPLAY image falls out for free; Int128 is a fixed-size value type far cheaper than BigInteger.

**Rejected alternatives.** decimal (software, not hardware-native) and BigInteger (heap-allocating) — both rejected by the owner.

### D8. National and boolean data ride the fixed-width STRING substrate (the full decision set D-N1..D-N4/D-B1 lives in `PHASE4_RECONCILIATION.md`).

- **D-N1 national**: an elementary national item is a plain C# `string` of `Length` characters — .NET strings are
  natively UTF-16, so "two bytes per character position" is the documented implementor choice (§13.18.60.4 GR8 +
  §8.1.2 NOTE 2). ALL width machinery stays CHARACTER-position based; `ImageWidth` is NEVER byte-doubled.
- **D-N2 byte≠char containment**: every byte-addressed surface REFUSES a national leaf loud (REDEFINES ComputeTier,
  EXTERNAL/ADDRESS-OF/BASED cells via ForceStringCanonical, FD/SD records, SORT keys) until the 2-byte layout
  residue lands (RESIDUE-11 coordination with the pointer track).
- **D-N3 collating**: national comparisons order by UTF-16 code-unit ordinal (the implementor default national
  sequence); the ALPHANUMERIC program collating sequence never applies (separate sequences — §8.8.4.2.9; the
  alphanumeric table is a distinct full-UTF-16-range sequence, never consulted for national comparisons and no longer `& 0xFF`-masked, post-CA26). A NON-native national sequence exists via
  `ALPHABET … FOR NATIONAL` literal phrases (§12.3.7) + `PROGRAM COLLATING SEQUENCE FOR NATIONAL` (§12.3.6) — the
  sparse `NationalCollatingTable`/`__COLLATE_NAT` channel; the coded-set names collapse to the D-N3 identity:
  UCS-4's ISO 10646 order over one-code-unit-per-position characters IS code-unit order (§8.5.1.4 — COBOL
  recognizes no surrogate pairs, so the supplementary-plane codepoint/code-unit divergence is unreachable), and
  UTF-8/UTF-16 name coded character sets only (§12.3.7 GR7 Table 6 — never a collating sequence).
- **D-N4 repertoire**: the FULL national repertoire — one UTF-16 code unit per position (the Latin-1-only
  staged guard was lifted with the DISPLAY-OF/NATIONAL-OF wave; the §8.1.2 correspondence for NAT→ANUM remains
  the Latin-1 subset identity with '?'+EC-DATA-CONVERSION substitution beyond it).
- **D-B1 boolean (SUPERSEDED 2026-08-04 by D19 — see below).** It read: "one alphanumeric character '0'/'1' per
  boolean position for BOTH usage display AND usage BIT — the §13.18.40.4 GR14 R14 license, a PERMANENTLY
  conforming choice." **The display half is right and stays. The USAGE BIT half was wrong**, and the word
  "PERMANENTLY" is what a reader should distrust: it rested on GR14 without reading §13.18.60.4 GR5. The **VALUE
  CARRIER** is still a `'0'`/`'1'` string for both usages — that part never changed and is not observable to a
  COBOL program; what changed is the LAYOUT (fix-queue PB43).

**Rationale.** The string substrate gives MOVE/compare/ref-mod/images for free on the ONE proven machinery;
the alternatives (a 1-byte national size, C# bool for PIC 1, bit-packing) each break a spec surface —
see the reconciliation design's F4 and the residue ledger.

**Rejected alternatives.** (a) `PIC 1 → bool` — rejected (multi-position
PIC 1(n)/fills/ref-mod need character semantics; a bool cannot carry them). (b) GR8's size-equal-alphanumeric
national — rejected for forward-compat (the non-Latin-1/NX"/BYTE-LENGTH residue presumes 2-byte). (c) ~~True
bit-packing for USAGE BIT — optional forever under R14~~ — **that alternative was not ours to reject; see D19.**

### D19. `USAGE BIT` OCCUPIES BITS. The value carrier stays a string; the LAYOUT becomes bit-granular. (Supersedes the USAGE BIT half of D-B1; fix-queue PB43, owner decision 2026-08-04.)

**The rule D-B1 did not read.** §13.18.40.4 GR14 says a boolean character "can be represented in storage as a bit,
an alphanumeric character, or a national character" — but that lists the AVAILABLE representations; **the USAGE
clause SELECTS one**, and both selections are mandatory:

| declaration | governing rule | required representation |
|---|---|---|
| `PIC 1(n)` with no USAGE | **§13.18.60.3 SR13(b)** implies USAGE DISPLAY → **§13.18.60.4 GR7** "an alphanumeric coded character set shall be used" | one character per boolean position — **what COBOL.NET already does; unchanged** |
| `PIC 1(n) USAGE BIT` | **§13.18.60.4 GR5** "the USAGE BIT clause specifies that **bits shall be used** … alignment … is specified in 8.5.1.6.3" | **bits**, aligned per §8.5.1.6.3 |

Two storage forms for one category is **not** the two-mechanisms anti-pattern — it is precisely what the USAGE
clause is for. And §8.5.1.6.3 exists solely to align items that occupy bits: a clause with nothing to say if
USAGE BIT were char-per-position.

⛔ **A GROUP OF BIT ITEMS IS *NOT* A BIT GROUP.** §13.18.29.4 GR3 — a group with no GROUP-USAGE clause specified
or implied "is an **alphanumeric group item**"; §13.16.4 GR1 implies GROUP-USAGE BIT only for a group
*subordinate to a bit group*. So `01 G. 05 A PIC 1(5) BIT. 05 B PIC 1(3) BIT.` is an alphanumeric group and
`FUNCTION LENGTH(G)` is **§15.50.4 r3** — character positions, = **1** — not r1's boolean positions. Misreading
"bit group item" as "a group of bit items" is the same error as misreading GR14, one level up.

#### The architecture: V59's numeric byte forms, one granularity finer

A `PIC 9(4) COMP` already holds its VALUE in a native `long` and occupies its **byte form**
(`PicInfo.StorageWidth`) in the record image; COMP-3 images as BCD. **The C# carrier is not what the standard
constrains** — sizes, offsets, overlays and the record image are. So a bit item keeps its `'0'`/`'1'` string
carrier (every MOVE/compare/ref-mod/fill path is untouched) and gains a **bit width** and a **bit-granular
offset**. The one thing bit items need that no numeric byte form does is **sub-byte sharing** — two same-level bit
items inside one byte — which is exactly why `RecordLayout`'s `int` character offset is insufficient.

#### The layout function (ISO §8.5.1.6.3, transcribed to a walk)

`BitLayout.ExtentBits(group)` walks the non-redefining children, carrying a bit cursor:

1. A **bit item immediately following an elementary bit item or bit group of the SAME LEVEL** → placed at the next
   bit position (**no padding** — this is the only case that shares a byte).
2. **Any other bit item** → advance the cursor to the next byte boundary, then place. (Covers a bit item after a
   character item, after a bit item of a *different* level, and the first item of a group.)
3. A **non-bit item** → advance to the next byte boundary (its natural boundary), then place `ByteWidth × 8` bits.
   The implicit filler this generates is §8.5.1.6.3's "as needed to advance alignment to a required natural
   boundary for the next item within that group".
4. **At the group's end**, if the cursor is not byte-aligned → pad to a byte boundary. §8.5.1.6.3's trailing-filler
   rule is stated for "a record that is an alphanumeric group or strongly-typed group item", and GR3 makes every
   GROUP-USAGE-less group alphanumeric, so this fires for all of them.
   ⚠ Its NOTE excludes "a record that is entirely a bit group, a level 77 item, or a level 1 elementary item" —
   which is why an **elementary** bit item is not padded and keeps its exact bit count.
5. **§15.50.4 r5** requires every implicit FILLER position generated above to be COUNTED. It is, by construction:
   the cursor advances through filler.

**Bits per character position is a pinned implementor choice** (§8.1.2 makes it implementor-specified): **8**,
consistent with `ByteWidth`'s existing "DISPLAY = 1 byte per character position". Documented in
`docs/CONFORMANCE.md` §4.2.16.

#### What derives from it

| surface | rule |
|---|---|
| `DataItem.ElementaryBitWidth` | a USAGE BIT leaf → `Pic.Length`; anything else → `ElementaryByteWidth × 8` |
| `DataItem.ElementaryByteWidth` for USAGE BIT | `ceil(Length / 8)` (was: `Length`) |
| `DataItem.ElementaryImageWidth` for USAGE BIT | `ceil(Length / 8)` — the character positions it OCCUPIES |
| group `ImageWidth` / `ByteWidth` | `ceil(BitLayout.ExtentBits / 8)` **iff the group has a USAGE BIT descendant**, else the existing sum, byte-for-byte unchanged |
| `FUNCTION LENGTH` of an ELEMENTARY bit item or a bit group | **§15.50.4 r1** — `Pic.Length` **boolean** positions, NOT `ImageWidth`. These coincided only by accident before; they must now be read from different members |
| `FUNCTION BYTE-LENGTH` | `ByteWidth`, so `PIC 1(8) BIT` = 1 |

⛔ **THE GATE IS "HAS A USAGE BIT DESCENDANT", AND THAT IS A CORRECTNESS CHOICE, NOT TIMIDITY.** Without a bit
item there are no sub-byte runs, so the bit walk and the character sum agree *by construction* — gating on it
makes the change provably byte-identical for every program that writes no `USAGE BIT`, which is the overwhelming
majority and includes the entire existing corpus. (The same discipline as PB41's scale-0 fast path.)

#### Scope boundary, stated rather than discovered later

**IN:** the layout function, the sizing surfaces above, `FUNCTION LENGTH`/`BYTE-LENGTH`, and the record image
codec packing a bit run into its bytes.
**OUT, and each is loud rather than silently wrong:** `GROUP-USAGE BIT` (§13.18.29) is still not modelled —
`DataBinder` says so — so a *declared* bit group stays rejected; and a sub-byte `REDEFINES` overlay (a redefiner
starting mid-byte) is refused rather than given a rounded offset. Both are recorded on [[PB43]].

### D9. OCCURS DYNAMIC (dynamic-capacity tables, §13.18.38 Format 4, COBOL-2014) — an out-of-line growable `CobolDynTable<T>`; sending/receiving direction carried by `Place`; a CORE ships whole, variable-length-group ops staged LOUD.

*Load-bearing spec anchors: §8.5.1.9.1 :8189 (dynamic-capacity definition — physical=logical capacity, current
capacity, non-contiguous implementor-defined allocation, FROM/VALUE initial, TO expected); §13.18.38 Format 4 :19858
(`OCCURS DYNAMIC [CAPACITY IN data-name-3] [FROM integer-4] [TO integer-5] [INITIALIZED]` + ASC/DESC KEY + INDEXED
BY); `ExceptionCatalog` registers EC-BOUND-OVERFLOW(nonfatal)/-TABLE-LIMIT(fatal)/EC-FLOW-SEARCH(fatal).*

**Storage (decided).** A dynamic table is NOT an inline record run (the spec permits non-contiguous occurrences and
adjacent fixed record fields keep their positions, §8.5.1.9.1 :8197). A new runtime class
**`CobolNet.Runtime.CobolDynTable<T>`** (out-of-line, referenced by a field on the owning `record struct`): a growable
`T[] _store`, `int _count` (= current capacity, §8.5.1.9.1 :8189), a `Func<T> _seed`, immutable `_min`/`_expected`
limits; it exposes `ref T` element access (rejected `List<T>` — its value-copy indexer breaks the `ref T` write
contract; rejected `Array.Resize`+a loose counter — no home for the grow/EC/register/seed policy). `T` stays a value
type (element `record struct`) or `string`. This is the singular home for grow-on-receiving, the capacity counter,
the CAPACITY bridge, the SEARCH bound, and every capacity EC.

**Growth (decided; §8.5.1.9.3/.9.4).** TWO paths, direction carried by `Place` (no new sender/receiver plumbing):
`Read()`→sending, `Write(rhs)`→receiving. (1) IMPLICIT — a *receiving* subscript > current capacity grows to it (+
seeds skipped intermediates), no CAPACITY phrase needed; a *sending* OOB subscript is EC-BOUND-SUBSCRIPT (fatal,
§8.4.2.3). (2) EXPLICIT — SET Format 14 (`SET dn {TO|UP BY|DOWN BY} n`, syntactically the existing SET — the binder
re-routes) only if CAPACITY present; may raise/lower. New occurrences seed with the one-occurrence element
initializer (reuse `GroupValueSlicer.ComposedInit`/`ValueInitializer.InitializerFor`; EVERY occurrence — including skipped intermediates — must be seeded) —
satisfies INITIALIZED exactly, and is crash-safe for the "undefined" (INITIALIZED-absent) case.

**CAPACITY register (decided).** A view over `table.Capacity` (no own storage): the binder maps `name → owning
dynamic-table DataItem` (`DataBinder._capacityRegisters`); resolution returns a `CapacityRegisterPlace(table)` whose
`Read()` emits `{tablePath}.Capacity` (synthetic unsigned-integer PicInfo) and whose `Write()` is illegal except via
SET Format 14 (SR30–32). Initial capacity = `FROM ?? 0` (§8.5.1.9.1 :8199). `FUNCTION LENGTH` over a dynamic
table/containing group = `Capacity * elemWidth` (§15.50, not a static width).

**Group image (decided).** A dynamic table is NOT image-capable (`IsCharacterImage`/`IsImageCapable` return false) —
a containing group drops out of the static record codec exactly like the Tier-C float/COMP-5 island; the element
`record struct` keeps its own AsImage/FromImage (single-element MOVE works). Whole-group ops on a containing group →
staged LOUD.

**CORE ships whole:** declaration (all phrases, order-independent) · out-of-line growable storage · CAPACITY
read + SET Format 14 write · implicit + explicit growth · INITIALIZED seeding · bounds/capacity ECs
(EC-BOUND-SUBSCRIPT/-OVERFLOW/-TABLE-LIMIT/-SET, EC-FLOW-SEARCH via a per-table `_inSearch` guard) · SEARCH/SEARCH
ALL over current capacity · `INITIALIZE <dynamic-table>` · the 2014 edition gate + matrix/VCR rows. **Staged LOUD
(diagnostic, not a silent wrong answer):** variable-length-group MOVE/COMPARE + whole-group image of a containing
group (**COBOLNET1527**, §14.6.9) · VALUE-derived initial capacity (**1528**, §13.18.63 GR16) · ref-mod of a
subordinate (**1526**, §13.7.1 SR6) · REDEFINES subject/object a dynamic table (**1525**, §13.18.44 SR5 + §8.5.1.9.1).

**Grammar (SHARED .g4 → FULL legacy guard; additive).** A new `CAPACITY` lexer token; a DYNAMIC alt on
`occursClause` (LL-disjoint — DYNAMIC is not an integerLiteral; superset parse — no edition predicate) with an
order-independent `occursDynamicPhrase*`; the COBOL-2014 introduction gate is the bind-time
`ConstructRegistry.Check(OccursDynamic2014)` at `OdoModel.OdoBindOccursSpec` → **COBOLNET0900** (the
`VersionConformancePass` funnel at end state — `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`).
NO SET grammar change (Format 14 is the
existing SET syntax, binder-rerouted). Diagnostics: **1522** (declaration/placement/SR28 — FILE SECTION, ODO-nesting,
FROM/TO bounds, dup phrase), **1523** (CAPACITY register misuse SR30–32), **1524** (SET Format 14 misuse), 1525–1528
(staged-loud). (08xx band exhausted; 15xx, last-used 1521.)

**Implemented (the CORE increments):** (1)
grammar (`CAPACITY` token + the `OCCURS DYNAMIC occursDynamicPhrase* …` alt, `{is2014()}?`-gated) + `OccursSpec`
dynamic fields + `DataItem.IsDynamicTable`/`IsTable`/`FieldType` (+ image-capable exclusions) + `CobolDynTable<T>` +
`FieldInit` dynamic branch + `OdoBindOccursSpec` Format-4 branch + `EditionGateHints` gate + the matrix row
(`occurs-dynamic-2014`, active) — the ONLY grammar/legacy-guard slice → golden `dyn_declare` (a group-element table,
greenfield-only). **Two plan refinements:** (a) NO VCR row — the VCR's own preamble (line 20)
states 2002→2014 *introductions* are captured by the matrix `introducedIn` tag, not the VCR (which carries only
2014→2023 Annex-E deltas + a few 2002→2014 behavior rows); the `constructs.json` + `ConstructDialectStatus` pair IS the
canonical introduction record. (b) NO separate `dyn_pre2014` corpus golden — the corpus runner asserts compile+run
SUCCESS only; the below-2014 **COBOLNET0900** rejection is asserted by the matrix row's `expectDiagnostic` at editions
85/2002 (`VersionMatrixTests`), the one place negative gating belongs. `DynamicResolve` (CAPACITY-register/access
resolution) belongs with increments 2/3 where the register becomes a readable item;
(2) CAPACITY register read + SET Format 14. The register is a `CapacityRegisterPlace`
(a VIEW over `{tablePath}.Capacity`, synthesized in a `DataBinder.DynamicResolve` post-build pass, indexed by
`DataBinder.CapacityRegisters`, resolved via an early `ReferenceResolver.Resolve` hook + the new
`ReferenceResolver.TablePath` whole-table-path helper); an unsigned-integer native-binary `PicInfo` so numeric reads
hit the plain scale-0 branch, its `NumProfile` emitted (not a field) for the DISPLAY/`FormatDisplay` path. SET Format
14 (TO/UP BY/DOWN BY) reroutes in `BindSetTo`/`BindSetUpDown` on a `CapacityRegisterPlace` first target →
`BoundSetCapacity` → `SetCapacity`/`CapacityUpBy`/`CapacityDownBy`. Diagnostics: **1523** (register as an ordinary
receiver — the `ResolveReceiving` chokepoint; and the SR30 implicit-definition name collision in `DynamicResolve`),
**1524** (SET F14 with a second/mixed target). A whole (unsubscripted) dynamic-table reference fails LOUD (a
`PlaceForItem` guard) rather than emitting a bare `CobolDynTable<T>` object. Goldens `dyn_capacity_read`/
`dyn_capacity_set`/`dyn_capacity_bounds` (greenfield-only). **Landed since (P13):** (a) **EC-BOUND-OVERFLOW** on exceeding the
TO/expected capacity is NONFATAL and checking-gated — the raise is LIVE (`ExceptionEngine.BoundOverflowError` via the
ambient `BoundOverflowChecking` statement gate; first crossing only, §8.5.1.9.6 GR1); with checking off (the default)
the operation continues either way, so the runtime `_expected` enforcement + the `>>TURN … CHECKING ON` gate
ride the EC-integration pass (`dyn_capacity_bounds` proves the checking-off continue). (b) **FUNCTION LENGTH over a
dynamic table** (= `Capacity × elemWidth`) + the `DynWholeTablePlace` whole-table place + its value-funnel loud belong
with the §14.6.9 **1527** whole-/variable-length-group work (one home for whole-dynamic-table operations); the clean
whole-table loud guard is the interim. (3) subscripted element access. A single `MemberPlace` path cannot carry
read-vs-write polarity (`ref T` covers the fixed case only), so an `AccessDir { Sending, Receiving }` is threaded
through `AccessPath`, and a `DynTablePlace(SendingPath, ReceivingPath, Item)` whose `Read()` emits `…RefSending(occ)`
and `Write(rhs)` emits `…RefReceiving(occ) = rhs;` (the receiving side grows-and-seeds skipped intermediates). Arity
recognition uses `IsTable` at the OCCURS-level count; the dynamic segment renders the accessor and any group-
field/fixed-OCCURS tail is appended after it (`{tbl}.RefSending(i).Field`). A sending OOB stays benign scratch;
EC-BOUND-SUBSCRIPT-under-checking rides the general subscript-checking gate (a cross-cutting later increment, not
dyn-specific). → `dyn_implicit_grow`/`dyn_initialized`; (4) SEARCH/SEARCH ALL over
current capacity + INITIALIZE. SEARCH: the table guard accepts `IsTable` (dynamic too), `OdoModel.SearchBound` gains a
dynamic branch returning `{tablePath}.Capacity` (a run-time bound, threaded via `BoundSearch.DependCount`), and
`BoundSearch.DynTable` carries the table path so `EmitSearch` brackets the scan in
`{tbl}.EnterSearch(); try { … } finally { {tbl}.ExitSearch(); }` — a SET Format 14 on that same table WHILE searching
raises EC-FLOW-SEARCH (GR31). INITIALIZE: per §14.9.20 GR10 (":28023") all
occurrences up to current capacity are initialized by the INITIALIZE statement's OWN stores (the CATEGORY DEFAULTS /
REPLACING / VALUE-phrase), NOT the OCCURS grow-seed, capacity unchanged (a naive re-seed with the VALUE-inclusive
image would be WRONG for a VALUE element). Implemented as an `InitializeDynLoop(var,
{tablePath}.Capacity, body)` (a RUN-TIME-bounded loop, sibling of `InitializeLoop`) over an `InitializeDynCursor`
that yields a `DynTablePlace` (writes via `RefReceiving`, within bounds so no growth). A group CONTAINING a dynamic
table (the other GR10 case) is a variable-length group → staged LOUD (the §14.6.9 1527 family). → `dyn_search`/
`dyn_initialize`; (5) the staged-loud
guards. **1522** (`DynamicResolve`): SR28 (:19987) — TO ≤ FROM rejected. **1525** (`ClassifyRedefinesClasses`, via a
`ContainsDynamicTable` subtree walk): §13.18.44 SR5 (data-name-2 shall not contain an OCCURS clause — which includes
OCCURS DYNAMIC, so a dynamic table cannot be the REDEFINES OBJECT) + §8.5.1.9.1 (a dynamic table's out-of-line,
implementor-allocated storage cannot form the fixed overlay §13.18.44.4 GR1 requires of the SUBJECT; §13.18.44 NOTE 3
permits REDEFINES only SUBORDINATE to a dynamic table) — a dynamic table (out-of-line) shall be neither the
subject nor object of a REDEFINES; the class is forced `Rejected`. **1528** (`DynamicResolve`): §13.18.38 GR16 /
§13.18.63 GR6 (:24102) — a VALUE on an ELEMENTARY dynamic entry derives the initial capacity (staged); a VALUE on a
GROUP dynamic table's SUBORDINATE is the element seed (capacity = FROM) and is NOT caught. **1527** (the containing-
group INITIALIZE `InitializeErrorAction` message; the whole-dynamic-table value op stays a runtime `NotImplemented`
loud) — the §14.6.9 variable-length-group family. **1526 is unnecessary and omitted:** reference modification of a
dynamic-table element works correctly (a `RefModPlace` over the
`DynTablePlace` — `WS-E(i)(1:2)` gives the right substring), and the cited "§13.7.1 SR6" restriction is actually the
§8.4.3.11.4 ADDRESS-OF/bit-item SR6, not a general ref-mod prohibition — so a 1526 guard would over-restrict valid
code. Negative tests: `OccursDynamicGuardTests` (1522/1525/1528 + the positive companions). **EC-BOUND-OVERFLOW**
is LIVE since P13 (the ambient checking-gated raise); **FULL variable-length-group MOVE/COMPARE** (a bind-time 1527 + a
`DynWholeTablePlace` carrying FUNCTION LENGTH = `Capacity × elemWidth`) remains the flagged follow-on (an EC-integration pass + a
whole-dynamic-table-operations pass); today both are LOUD, never silently wrong.

**Hardening (current invariants).** A whole-GROUP receiving MOVE into a group nested BELOW the dynamic level routes
through `ReceivingPath` (`RefReceiving`) via a `DynTablePlace` arm in `EmitGroupMove`, so it grows rather than
silently losing an out-of-capacity write. `CorrEligible` gates on `!IsTable` (not `Occurs is null`) so CORRESPONDING
never emits member access on a `CobolDynTable<T>` field (§14.7.6 rule 4). SEARCH of a dynamic table nested under a
fixed OCCURS (`TablePath` null) fails LOUD rather than scanning ZERO occurrences (a subscripted-capacity path is a
later increment). OCCURS DYNAMIC in the FILE SECTION is rejected **COBOLNET1526** (§8.5.1.9.1 item 3 — "any place
OTHER THAN the file section"). The SET Format 14 capacity peek uses a PURE `ReferenceResolver.CapacityRegisterFor`
(never `refs.Resolve`, which would route an OO `prop OF obj` first target through the property hook and enqueue a
spurious pending op). The **1528** guard also covers a GROUP dynamic table with a subordinate VALUE AND a TO (the
§13.18.63 GR16 superordinate-scope derivation; a subordinate VALUE with NO TO stays supported = capacity FROM).
`CobolDynTable` wires **EC-BOUND-OVERFLOW** since P13 (the receiving-subscript implicit grow past the expected
capacity raises through the ambient `BoundOverflowChecking` gate, first crossing only); **EC-BOUND-SET** remains the
flagged nonfatal follow-on.
Resolved open questions: VALUE-capacity staged; EC-FLOW-SEARCH in CORE.

### D17. TYPEDEF + the TYPE clause (§13.18.58 / §13.18.57 / §13.16, COBOL-2002) — a template registry + a subtree clone spliced into the forest at declaration-bind; a FRONT-END + BINDER-ONLY feature (ZERO emitter change).

**Key architectural fact.** A weak TYPE reference is pure MACRO-EXPANSION — the cloned subtree emits exactly as a
hand-written group would (§8.5.3.2 NOTE); a STRONG type is stored IDENTICALLY and adds only COMPILE-TIME checks
(§8.5.3.3). So the whole feature = grammar (one token + one clause) + a template registry + a subtree clone spliced
into the forest during declaration binding + a same-type guard at MOVE/compare. **No emitter change.**

**CORE (ships whole, program/global scope).** (1) A **`TypeDecls`** registry (name → template subtree; case-insensitive,
mirrors `ByName`). The TYPEDEF entry allocates NO storage (not in `Roots`) and its subordinate names are NOT globally
referenceable (§13.18.58.4 GR2/GR1) — the template is built WITHOUT `RegisterName`. (2) **TYPE reference cloning**
(`TYPE [IS] type-name`, the grammar ALREADY exists at `CobolData.g4:253`): a deep clone of the template subtree into
the referencing entry (§13.18.57.4 GR1/GR2), handling elementary + group types, subject-owned OCCURS (array-of-type,
§13.16 SR14) and subject VALUE override (GR3), GR1 exclusions (the type's level/name/GLOBAL/SELECT WHEN/TYPEDEF are
not copied). (3) **STRONG typing** (§13.18.58.2): declaration SRs (§13.18.57.3 SR3/SR4/SR6) + same-type gating at MOVE
(§14.9.25.3 SR2 — a strongly-typed group RECEIVER wants a same-type sender), comparison (§8.8.4.2.3 SR1),
class-condition (§8.8.4.4.3 SR1);
intra-element same-type = template identity (`TypeName` + relative path, §8.5.3 NOTE). (4) **level-88s inside a TYPEDEF** (GR1 — part of the
type): cloned + re-registered. (5) recursion/placement guards. **This also fixes a current SILENT-DROP bug:** at 2002+
`TYPE IS name` parses and is silently dropped (no `typeClause` binder branch) — CORE wires it (unresolved → 1530).

**Mechanism.** `DataItem` gains `TypeName`/`StrongType`. A Pass-0 **`CollectTypeDecls`** (in `BindDeclarations`, before
`BindEntries`) builds each TYPEDEF template (fresh `Uid`s, `Parent`/`Children`, but NO `Roots.Add`/`RegisterName`) and
returns the consumed entries so the main pass SKIPS them. A recursive **`CloneSubtree`/`CloneItem`** (generalizes the
flat `CreateCompilerTemp`, `DataBinder.Oo.cs:362`): a fresh `Uid` per node (CRITICAL — `StructName`/`ProfileName` ride
on it), shares the immutable `Pic`, copies the description fields, re-uniquifies `CsName` in the NEW scope, and DOES
`RegisterName` (clones ARE referenceable, unlike the template). **`ExpandType`** runs inside `BindEntries` right after
an item is placed (so the clone is in the forest BEFORE `BindResolve` — every post-build pass sees it automatically,
the same invariant `CreateCompilerTemp` relies on). STRONG equivalence: `DataItem.StrongRoot` (walk
to the outermost `StrongType` ancestor) + `SameStrongType(a,b)` (equal strong-root `TypeName` + relative `CsName`
path), checked in `BindMove` (`CheckStrongMove`) / `CheckedRelational` (the ONE relation chokepoint) / the
class-condition arm (`CheckClassConditionOperand`).

**Grammar (shared-.g4 rules → FULL legacy guard per change).** `typedefClause : IS? TYPEDEF STRONG? ;` and
`sameAsClause : SAME AS cobolWord ((OF|IN) cobolWord)* ;` on `dataDescriptionClause` (superset parse — no edition
predicates); the TYPE-reference rule pre-existed. The COBOL-2002 introduction gates are the `VersionConformancePass`
parse arms (`VisitTypedefClause` / `VisitTypeClause` / `VisitSameAsClause`, recognition-based) → **COBOLNET0900**
(rows `typedef-def-2002` / `type-clause-2002` / `same-as-clause-2002`). The `AS` token is shared with the
CONSTANT-entry surface (both §8.9-interval words; `AS` is nameSlot-only — the FU-1 ledger). `POINTER TO type-name`
stays out.

**SAME AS (§13.18.49) rides the SAME machinery** — it is the TYPE expansion with a DATA-NAME source:
`DataItem.SameAsName`/`SameAsQualifiers` → `ExpandSameAs` (inside the ONE `ExpandTypes` pass, AFTER the TYPE loop so
targets copy their expanded description; chains recurse, cycles = the expanding-set + subject-ancestor walks). The
copy = the shared `CopyEntryDescription` (also ExpandType's §13.18.58.4 GR3 body; SYNCHRONIZED copies for SAME AS
only — §13.18.49 GR1 has no alignment exclusion, §13.18.57.4 GR1 does) + `CloneItem` with a `levelDelta` (GR2b
subordinate renumbering relative to the subject, may exceed 49 per GR2c; TYPE flows pass 0). GR1 exclusions honored:
data-name-1's level/name/CONSTANT RECORD/EXTERNAL/GLOBAL/REDEFINES are not copied. GR3/GR5: a USAGE/SIGN on a group
containing data-name-1 applies as though specified for the subject (mirrored onto the copied Pic at expansion —
the subject's chain cannot see the target's ancestors). A copied TYPE identity keeps the §8.5.3 anchors
(`SameStrongType` holds across `B SAME AS A` pairs) and re-checks the §13.18.57.3 SR6 strong placement.

**EXTERNAL type declarations (§13.18.22) are LIVE** — a conformance surface + record-external attribution, NOT a
cross-program type registry: `DataItem.IsExternalTypedef` on the template; `ExpandType` enforces GR2 (a data
description containing an external type shall be level-1) and SR5 (an external record of a STRONG type requires the
type external too) → **1558**, and marks GR3 records `ExternalFromType`; `CallBindExternalAndGlobal` re-bases those
roots onto the run-unit `ExternalStore` cell exactly like explicitly-EXTERNAL records (GR6 matching by externalized
name rides the existing mechanism). Cross-source-unit §8.5.3 same-type equivalence for external types remains the
recorded follow-up in `StrongTypeModel`.

**Diagnostics (15xx).** **1529** malformed TYPEDEF (SR15 level-1/named; SR1 STRONG-on-elementary; TYPEDEF ×
REDEFINES/BASED/CONSTANT RECORD/PROPERTY); **1530** TYPE unresolved/recursive; **1531** illegal TYPE-reference
context (immediate subordinate/88; disallowed sibling clause; 77-of-group; type-with-INDEXED-BY used ≥2×);
**1532** STRONG declaration violation; **1533** STRONG use incompatibility (MOVE/compare non-same-type; strong group
in a class condition — split by descriptor: `strong-move-mismatch`/`strong-compare-mismatch`/`strong-class-condition`);
**1535** two descriptors on one code: `strong-compare-ordering` (§8.8.4.2.3 SR4 — a boolean/object/pointer-bearing
strong group compares for equality/inequality only; the NAMED spec rule) and `typedef-renames-staged`
(RENAMES-in-TYPEDEF, staged); **1555/1556/1557** SAME AS subject-entry / referenced-entry / cycle rule families
(§13.16.3 SR12 + §13.18.49.3); **1558** EXTERNAL-type conformance (§13.18.22 GR2/SR5); **0899**
`strong-group-ordering-signed-leaf` (ordering same-type strong groups with a SIGNED numeric leaf needs the
§8.8.4.2.12 element-by-element algebraic order the image comparison cannot honor — equality and unsigned/character
orderings are image-equivalent and live).

**Implemented (the CORE increments).** (1)
grammar (`STRONG` token + `typedefClause`; `EditionGateHints.TypedefClause` → 0900; the
`typedef-def-2002` matrix/registry row) + the weak-TYPE spine (`DataItem` `IsTypedef`/`TypedefStrong`/`TypeRefName`;
`BindEntries` routes a TYPEDEF root to `TypeDecls`, off `Roots`/`ByName`; `RegisterTypeDecl` → **1529**; a post-build
`ExpandTypes` at the top of `BindResolve` clones each `TYPE IS type-name` via `CloneItem` — fresh `Uid`/re-uniquified
`CsName`/registered — elementary→copy PIC, group→clone children, forward refs OK; unresolved/recursive → **1530**).
`TypeName`/`StrongType` are populated here (the STRONG checks are increment (2)). — **the ONLY grammar/legacy-guard slice** → goldens
`typedef_weak_elem`/`typedef_weak_group`. (2) STRONG typing (all BINDER-ONLY): the
`DataItem.StrongRoot` walk (outermost `StrongType` ancestor — §8.5.3.1, a group SUBORDINATE to a strong group is
itself strongly typed) + `IsStrongGroup`/`IsStronglyTyped` + the static `SameStrongType(a,b)` (equal strong-root
`TypeName` + equal relative member-name path). USE gates → **1533**: `CheckStrongMove` in `BindMove` (§14.9.25.3 SR2),
the strong-group same-type check in the ONE `CheckedRelational` chokepoint (§8.8.4.2.3 SR1 — so it also covers
EVALUATE/PERFORM UNTIL/SEARCH WHEN), a strong-group guard in `CheckClassConditionOperand` (§8.8.4.4.3 SR1). DECL gates
→ **1532**: SR6 at clone time in `ExpandType` (level-1-or-under-strong; also catches SR7's 77→group), SR3/SR4 in a
post-resolution `CheckStrongTypeDeclarations` pass (a RENAMES/REDEFINES over any part of a strong subtree, INTERNAL
template redefines excluded via the shared-strong-root test). Golden `typedef_strong_ok` (same-type whole-record
MOVE+compare byte-verified) + `TypedefStrongTests` ×8 negatives (SR2/SR1/class-cond/SR6/SR4/SR3/relative-path + a
clean companion). (3) level-88 condition-names inside a TYPEDEF (§13.18.58.4 GR1):
`DataItem.Own88s` (the item's own 88s), `BindCondition(…, registerGlobal: !rootIsTemplate)` keeps a template's 88s OFF
the global by-name index (GR1), and `ExpandType`/`CloneItem` call `CloneConditionOnto` to clone them onto each
reference (registered globally — clones ARE referenceable). Golden `typedef_88` + `TypedefConditionTests` ×2.
(4) the residue rules: **1558** EXTERNAL-type conformance (§13.18.22 GR2/SR5 in
`ExpandType` — the external-record attribution itself is LIVE, see the EXTERNAL-type paragraph above), **1535**
RENAMES-in-TYPEDEF staged (the in-template level-66 guard in `BindEntries`, descriptor `typedef-renames-staged`) +
the NAMED §8.8.4.2.3 SR4 equality-only rule for a strong group with boolean/object/pointer elements (descriptor
`strong-compare-ordering`, in the relation checkpoint), **0899** `strong-group-ordering-signed-leaf` (the
§8.8.4.2.12 signed-leaf element ordering, staged), **1531** an INDEXED-BY type referenced ≥2× (the
`_typedIndexNames` collision set in `CloneItem`). Goldens `typedef_indexed` (a single INDEXED-type reference works),
`typedef_same_as` (elementary+VALUE / group+qualified / nested renumbered / OCCURS composition / strong-copy
relations), `typedef_external` (two programs, one ExternalStore cell) + `TypedefResidueTests`/`SameAsTests`.
**Matrix note:** the STRONG phrase rides the SAME `typedefClause` gate as `typedef-def-2002` (introduction gating
already covered); SAME AS carries its own row `same-as-clause-2002`; an EXTERNAL-typedef row is NOT warranted
(TYPEDEF is unreachable below 2002, so the composition needs no second 0900); 1531–1535/1555–1558 are
edition-INVARIANT compile-time diagnostics (no cross-edition behavior variance). The restricted data-pointer
`USAGE POINTER TO type-name` (ISO §13.18.60.2 / Annex D.9.2.2) stays deferred — catalogued as the pending matrix
row `usage-pointer-to-type-2014` (the P12 re-scout re-anchored the former "`TYPE TO` pointer-target" label: plain
`TYPE [TO]` is the TYPE clause's optional word, §13.18.57.2, already live).

**Additional hardening (current invariants).** `ExpandType` sets `TypeName`/`StrongType` BEFORE cloning children, so a
nested TYPE ref's SR6 ancestor walk sees the enclosing strong item (no false SR6 strong-in-strong rejection).
`DataItem.TypeAnchor` (the NEAREST TYPE-carrying ancestor) drives `SameStrongType`, so a nested `TYPE INNER-T` subgroup
matches a standalone INNER-T item (§8.5.3 bullet 1). A cloned OCCURS DEPENDING ON resolves data-name-1 in the clone's
OWN record subtree first (`OdoResolve` `FindInSubtree` before the global-scope lookup — §13.18.57.4 GR1 / §13.18.38
SR20), not a globally-first same-named counter. Three §13.18.57.3 syntax rules are enforced: **1536** SR7 (a level-77
subject needs an elementary type — weak-invariant, not just STRONG), **1537** SR2 (a TYPE entry must be followed
immediately by a subordinate or level-88 entry, else a silent member-merge / CS1061 leak), **1538** SR5 (no USAGE/SIGN
on a group superordinate to a TYPE subject). The 15xx TYPEDEF band spans **1529–1538**.

**RISKS flagged:** `OccursSpec` sharing on clone (verify it holds NAMES re-resolved by `OdoResolve`, not cached
resolved items); `INDEXED BY` in a TYPEDEF used ≥2× = a global index-name collision (staged loud 1531); method/OO-scope
typedefs are program/global-scope-first (the `OoRootOwner` parallel forest → staged loud follow-up); STRONG group
alignment (GR2d/§8.5.1.6.5) is D6/SYNC domain, out of scope.

### D18. A FUNCTION-IDENTIFIER in a subscript or reference-modification position materializes into a COMPILER TEMP hoisted as a statement pre-op — never a new arm on `RenderSegment`, and never an early `BoundExpr` carrier migration.

**The problem (fix-queue PB17).** `MOVE W-E(FUNCTION INTEGER(3)) TO W-R` and
`MOVE W-A(FUNCTION INTEGER(3):2) TO W-R` are **legal source** that compiles clean and throws
`NotImplementedCobolFeatureException` at run time — the PB7/DA7 wrong-stage family. The chain, every link
`cite.py --check`ed: **§8.4.3.1.2** Format 1 makes a function-identifier an identifier → **§15.4** "the evaluation
of a function produces a returned value in a temporary elementary data item" → **§15.2** items 5–6 put integer and
numeric functions "of the class and category numeric" → **§8.8.1.1** "an arithmetic expression may be an identifier
referencing a numeric data item" → **§8.4.2.3.2** + **§8.4.2.3.4 GR1b** admit `arithmetic-expression-1` as a
subscript, and **§8.4.3.3.3 SR4** as a ref-mod position.
⚠ **§8.4.3.2.3 SR11/SR12 do NOT bar it** — they bar functions where an *integer* or *unsigned integer* is
required, and a subscript is neither: **GR1b sets EC-BOUND-SUBSCRIPT when the expression does not evaluate to an
integer**, a runtime condition that would be pointless if the position required one syntactically (SR14 confirms
it from the other side, having to impose that restriction *specially* for a BY REFERENCE bit item).

**Root cause.** `ReferenceResolver.RenderSegment` is a hand-rolled expression compiler over flat SUBSCRIPT-mode
tokens that emits C# text at BIND time; its `default:` arm literally lists `FUNCTION` among the token types it
cannot render.

**Two tempting fixes, both REJECTED.**
· *Add a FUNCTION arm to `RenderSegment`* — that hand-writes intrinsic rendering into a `StringBuilder`, i.e. a
  THIRD expression compiler beside `ExpressionBinder` and `IntrinsicRenderer`.
· *Migrate `RefModPlace.Start`/`Length` to `BoundExpr`* — **forbidden here**: they are the documented **D10
  TRANSITIONAL carrier**, deliberately the same shape as `RefModSpec` "so PHASE 15 migrates both in one move
  rather than leaving a second, differently-shaped ref-mod behind", and **D10 is an owner ruling relocated to
  PHASE 15 §"CUT 2.5"**, blocked while the frozen legacy compiler still shares `SUB_*`/`SubscriptEntryContext`.
  The string carrier is deliberate sequencing, not decay.

**The decision.** Materialize what §15.4 already describes. Bind the function through the ONE function pipeline
(`IntrinsicBinder.BindIntrinsicCore` — shared by the FUNCTION-keyword form, the keyword-omitted re-parse and every
nested recursion, so a USER-defined function in a subscript falls out of the same change); synthesize the temp via
`DataBinder.CreateCompilerTemp`, already "THE ONE synthesized-compiler-temp constructor"; register a statement-scoped
pending PRE-op drained at the `BindStatement` chokepoint — the mark-on-entry / drain-own-suffix protocol that
ALREADY serves two clients (`Udf.PendingCount`, `data.OoPendingPropertyOps`). The subscript segment then renders
as an ordinary data-name through the existing `ResolveSubscriptName`.
⭐ **Prefer GENERALIZING the UDF pending list to a third list**: a function-identifier is never a receiving operand
(**§8.4.3.2.3 SR1**), so intrinsic and user-function activations are both unconditional pre-ops with identical
hoist rules — two mechanisms for one job would be the banned anti-pattern.

**⛔ THE §15.4 TEMPORARY'S DESCRIPTION IS A CORRECTNESS DECISION, NOT A FORMALITY — AND THE FIRST ANSWER WAS
WRONG.** §15.4.1 leaves the temporary's "characteristics and representation … defined by the implementor" when
native arithmetic is in effect, so the shape is a genuine implementor choice; the ONE constraint on it is that it
must not destroy the fact §8.4.2.3.4 GR1b tests. This design originally specified `Scale: 0` on the reasoning that
a subscript is an occurrence number. **That is exactly wrong.** GR1b makes a subscript whose expression "does not
result in an integer" set EC-BOUND-SUBSCRIPT, and §8.4.3.3.4 item 5)c says the same for a ref-mod position — so a
scale-0 temp TRUNCATES the value on the way in and `W-E(FUNCTION SQRT(2))` silently indexes occurrence 1 instead
of raising. The temp's own description would have turned legal source into a wrong answer, in the very code
written to stop legal source from throwing. **The temp is `PIC S9(21)V9(9)` — 30 digits, scale 9** — so it takes
the `Int128` wide tier (`PicInfo.IsWide`, >18 digits) and no subscript a program can express overflows it, which
matters because high-order truncation could WRAP an out-of-range subscript into an in-range one and convert a
detectable error into a silent one. It is synthetic, never enters `DataBinder.ConformanceForest`, so the edition
digit-capacity gates (18 at COBOL-85) do not reach it — it is not a PICTURE the programmer wrote.

**⛔ AND THE INTEGRALITY RULE IS ONE READ SHARED WITH ORDINARY SCALED SUBSCRIPTS (fix-queue PB41).** Asking what a
scale-0 temp would do to a NUMERIC function result is what exposed the pre-existing sibling: a COBOL.NET numeric
item stores UNSCALED — `PIC 9V9 VALUE 2.0` is the field `20L` at scale 1 — and `ResolveSubscriptName` never
consulted `Pic.Scale`, so `W-E(W-S)` with `W-S = 2.0` indexed **occurrence 20**, fell outside `1..5`, and read the
benign scratch slot. Compiled clean, ran to completion, wrong answer. Both clauses are about the VALUE, so both
resolve at **ONE place — `ReferenceResolver.PositionRead`** — which renders `CobolTable.Occ(path, scale)` for a
subscript and `CobolString.RefModPosition(path, scale)` for a ref-mod position; the shared de-scale/integrality
arithmetic is `CobolNum.HasFraction`/`PositionOf`, and the two wrappers exist ONLY because the positions name
different Table 13 conditions (EC-BOUND-SUBSCRIPT vs EC-BOUND-REF-MOD). **The position kind therefore travels as a
parameter (`SegmentPosition`), never as renderer state** — `RenderSegment` is re-entrant through `ReadRefMod`, and
ambient state goes stale across a re-entrant descent. A scale-0 item keeps the exact previous text, so the emitted
C# for ordinary subscripts is byte-identical and no de-scaling division is emitted where none is needed.

⚠ **A COMPOUND SEGMENT CARRYING A SCALED OPERAND ROUTES TO THE TEMP, NOT TO OPERAND-WISE DE-SCALING.** GR1b tests
the integrality of **the result** of the whole expression, not of each operand: `W-E(W-P + W-Q)` with
`W-P = W-Q = 1.5` has the integral result 3.0 and is a legal subscript, while de-scaling each operand first yields
`1 + 1 = 2` **and** raises the condition twice on source that never violated it. Such a segment therefore takes
the same materialization route, which evaluates it at full precision and applies the rule exactly once, where the
standard applies it. A SINGLE scaled operand IS the result, so it needs no detour.

⛔ **THE CORRECTNESS TRAP — DO NOT HOIST OUT OF A REPEATEDLY-EVALUATED CONDITION.** §8.8.4.13 r2 evaluates a
function "if and when the conditions containing them are evaluated", so a subscript inside a PERFORM UNTIL /
SEARCH WHEN / EVALUATE object must not be lifted to a statement pre-op. `UdfBinder` already solves this
(`UdfAttachPerEvaluation` / `BoundUdfEvaluated`) and STAGES LOUD the windows it does not reach
(`UdfStagePerEvaluationResidue`, COBOLNET1509) — **follow that precedent exactly, including its loud residue.**

**On the "do NOT re-grammar this" guidance below:** this adds an ISOLATED
`subscriptExpressionFragment : arithmeticExpression EOF ;` entry rule reachable ONLY from the binder re-parse and
referenced by nothing in `compilationUnit` — the `functionArgListFragment` / `compileTimeOperandFragment`
precedent, whose own comment records "ZERO blast radius on the main parse". The main subscript grammar is
untouched.

**Deleted by D10.** When PHASE 15 §"CUT 2.5" removes the SUBSCRIPT lexer mode and the string carrier becomes
`BoundExpr`, the temp path goes with it — this is a decision that is *designed to be deleted*, which is why it
must not grow a second carrier in the meantime.
⚠ **The PB41 half is NOT deleted with it.** The integrality rule belongs to the two clauses, not to the carrier:
after CUT 2.5 the position read moves into the expression renderer with the rest of the carrier, still naming the
position's own Table 13 condition. Only the *materialization* is transitional; `HasFraction`/`PositionOf` and the
two wrappers are permanent, and the goldens `pb41_scaled_position` / `pb41_position_not_integer` pin them across
the migration.

## C# mapping

CONCRETE COBOL→C# MAPPINGS:

— Elementary —
  05 NAME  PIC X(10) VALUE "BOB".        →  public string Name; … Name = CobolString.Store("BOB",10);
  05 CT    PIC 9(4).                      →  public long Ct;  // unscaled, scale 0; init 0L
  05 AMT   PIC S9(5)V99 COMP-3.           →  public long Amt; // unscaled (7 digits), scale=2; profile threads truncation=Packed
  05 BIG   PIC 9(30).                     →  public Int128 Big;  // WidePrecision → Int128
  05 RATE  COMP-2.                        →  public double Rate;
  05 FLAG  PIC 1(4).                      →  public string Flag; // boolean: one '0'/'1' CHAR per position (D8) — NOT a C# bool (multi-position PIC 1(n), MOVE fills, and ref-mod need character semantics; §13.18.40.4 GR14 R14 licenses the character representation). 2002+ — 0900 at --std 85.
  05 NNAME PIC N(4).                      →  public string Nname; // national: one UTF-16 char per national position (D8); usage NATIONAL implied (§13.18.60.4 SR13a). 2002+ — 0900 at --std 85.

— Group → nested record struct —
  01 WS-REC.                              →  record struct _T_WsRec { public string Name; public long Ct; }
     05 NAME PIC X(10).                       private static _T_WsRec WsRec =
     05 CT   PIC 9(4).                            new() { Name = new string(' ',10), Ct = 0L };
  reference NAME OF WS-REC                 →  WsRec.Name

— Fixed OCCURS (1-D) —
  05 TBL OCCURS 3 PIC 9(2).               →  public long[] Tbl;  init: Tbl = [0L,0L,0L];
  TBL(2)                                   →  Tbl[2 - 1]
  TBL(I)                                   →  Tbl[I - 1]        (I a numeric data-name)
  TBL(IDX + 1)                             →  Tbl[IDX + 1 - 1]  (relative index, IDX an index-name = occurrence #)

— OCCURS group (2-D), the array-of-structs shape —
  05 ROW OCCURS 4.                         →  record struct _T_Row { public long Col; }   // 1 col here
     10 COL OCCURS 5 PIC 9.                    public long[] Col;  // per-row
  COL OF ROW(2)(3)  (i.e. ROW(2), COL(3))  →  Row[2 - 1].Col[3 - 1]
  (each COBOL subscript → its own [n-1]; NO flattening, NO offset multipliers)

— OCCURS DEPENDING ON —
  05 N PIC 9(3).                           →  public long N;            // the length var
  05 ITM OCCURS 1 TO 100 DEPENDING ON N    →  public _T_Itm[] Itm;      // allocated at MAX (100); N bounds the live range
      PIC X(4).                                // sending op uses [0..N); a receiving whole-group uses [0..N) when N is OUTSIDE the group (ISO §13.18.38 GR8a), MAX only when N is INSIDE (GR8b)
  ITM(K)                                   →  Itm[K - 1]                // bounds checked vs N at runtime if EC enabled

— Index-name (INDEXED BY) = occurrence number —
  05 T OCCURS 10 INDEXED BY IX PIC X.      →  public string[] T; … private static long Ix = 1;
  SET IX TO 5                              →  Ix = 5;
  SET IX UP BY 2                           →  Ix += 2;
  T(IX)                                    →  T[Ix - 1]

— Reference modification —
  NAME(3:4)        (read)                  →  CobolString.RefMod(WsRec.Name, 3, 4)        // = Name.Substring(2,4)
  NAME(3:)         (read, length omitted)  →  CobolString.RefMod(WsRec.Name, 3, -1)       // -1 = to end
  MOVE "XY" TO NAME(3:2)  (write)          →  { var _t = WsRec.Name; WsRec.Name = CobolString.SpliceInto(_t, 3, 2, CobolString.Store("XY",2)); }

— Level 88 condition name —
  05 ST PIC 9.                             →  public long St;
     88 OK   VALUE 1.                       →  private static bool Ok      => St == 1;
     88 BAD  VALUE 2 THRU 9.                →  private static bool Bad     => St >= 2 && St <= 9;
     88 MIX  VALUE 0 5 7.                   →  private static bool Mix     => St==0 || St==5 || St==7;
  SET OK TO TRUE                           →  St = 1;     // parent set to the (first) condition value (ISO §14.9.39.4 GR6)

— Qualified + subscripted + ref-modded together —
  VAL OF ITEMS(I) OF WS-REC (3:2)          →  CobolString.RefMod(WsRec.Items[I - 1].Val_as_image, 3, 2)
                                              (Val numeric → its display image first per ISO §8.4.3.3.4 rule 2)

— CALL BY REFERENCE (Place → C# ref) —
  CALL "SUB" USING CT.                      →  Sub(ref WsRec.Ct);
  CALL "SUB" USING BY CONTENT NAME.         →  Sub(WsRec.Name);   // copy

— VALUE on a table —
  05 TBL OCCURS 3 PIC 9 VALUE ZERO.         →  Tbl = [0L, 0L, 0L];
  05 DAYS OCCURS 2 PIC X(3) VALUE "JAN".    →  Days = [CobolString.Store("JAN",3), CobolString.Store("JAN",3)];

## Hard problems

### The grammar captures subscript and ref-mod content as ONE undifferentiated raw token stream (subToken+ in SUBSCRIPT lexer mode); `(I J)` (2 subscripts) and `(3:2)` (ref-mod) and `(I)(3:2)` (subscript THEN ref-mod) are syntactically identical at the rule level — only the presence of SUB_COLON distinguishes them.

Port the legacy ExpressionBinder's SUB_* token interpreter verbatim (it is proven over 364 NIST tests): for each `(...)` suffix group, scan tokens — if it contains SUB_COLON it is a ref-mod (split into start/length sub-expressions at the colon), else it is a subscript list (split on SUB_WS / SUB_COMMA into N subscript expressions, each itself possibly a relative `idx ± lit`). Phase-A flatten produces a clean {qualifiers[], subscriptGroups[][], refMod?} that Phase-B resolves. Do NOT try to re-grammar this — the SUBSCRIPT-mode design is intentional and reusing the interpreter avoids re-deriving COBOL subscript edge cases.

⚠ **The interpreter is a token renderer, not an expression compiler, and the difference is where it ends (D18).** `RenderSegment` renders integer literals, data-names, index-names, `+ - * /` and parentheses one token at a time; **everything else is re-parsed through the isolated `subscriptExpressionFragment` rule and bound by the real pipeline** — never grown a new hand-written arm, which is how a token renderer turns into a third expression compiler. **The renderer is an OPTIMIZATION over that route, never the arbiter of what is legal in the position, and stating it the other way round cost a defect: PB42.** For one commit the D18 gate asked "is this segment FUNCTION-bearing?" instead of "can the renderer render it", so `W-E(W-I ** 2)` and `W-E(2.0)` — plain arithmetic under §8.8.1.1 ("a numeric literal … separated by arithmetic operators") with §8.3.2.4.2 listing `**` as an arithmetic operator — kept compiling clean and throwing at run time. ⛔ **Routing everything is NOT the unaudited-table mistake PB1 taught, and the reason is structural: the fragment rule is `arithmeticExpression EOF`, so THE GRAMMAR ADJUDICATES.** A shape §8.8.1.1 does not admit — an alphanumeric literal, an `ALL` figurative constant (legal only in the §8.4.2.3.3 r6 positions) — cannot parse as an arithmetic expression, the fragment returns null, and the caller keeps its exact loud posture. Admission is by parsing, not by assertion, which is why the next arithmetic form needs no edit here. ⚠ Those two remain **run-time** throws where they should be bind-time diagnostics — right position, wrong stage; that is PB42's recorded residue.

### REDEFINES is a byte-level storage overlay with no clean typed-native equivalent: two differently-typed C# fields cannot share memory, so a write through one view is invisible to the other.

**Resolution (4-tier one-canonical-backing; see `COBOLNET_REDEFINES_DESIGN.md`).** One STORED canonical per redefines class + every other view a computed `Place` accessor. **Tier A** (identical PIC/USAGE) = pass-through accessor; **Tier B** (whole class USAGE DISPLAY — the corpus majority) = a single `string` canonical with substring/`ParseDisplay`/`FormatDisplay` accessors (NO bytes); **Tier C** (genuine mixed-USAGE pun) = one class-scoped `byte[]` canonical with per-leaf typed codecs, confined to the class and never persisted; **Tier D** (unmodelable) = reject loud. Independent typed fields + a loud cross-type-read guard (SSOT §14.3) cannot stay coherent under a pun, so detection alone never makes the program correct — only the single shared canonical does.

### Reference modification as a RECEIVER: C# strings are immutable, so `NAME(3:2) = x` cannot splice in place.

Place.Write for a RefModPlace rebuilds the whole string: `field = field[..(s-1)] + fit(value,len) + field[(s-1+len)..]` via runtime CobolString.SpliceInto(string,start1,len,value). Evaluate s and l into temps ONCE (ISO requires single evaluation of positions/subscripts) to avoid double-eval and side-effect duplication. For numeric/COMP receivers under ref-mod (rare), defer to the byte-image fallback and flag loud.

### OCCURS DEPENDING ON: the array size varies at runtime, but a C# array has a fixed allocated length; and ISO OCCURS GR8 (§13.18.38.4) governs the extent of a whole occurs-depending group operand in TWO cases — GR8a: when the DEPENDING-ON item (data-name-1) is OUTSIDE the group, the CURRENT count is used for BOTH sending and receiving; GR8b: when data-name-1 is INSIDE the group, a SENDING operand uses the CURRENT count while a RECEIVING operand uses the MAXIMUM length (data-name-1 is itself overwritten by the operation and so cannot bound it).

Allocate the array at MAX occurrences once; the length variable (DEPENDING ON item) bounds the LIVE range. Element access `Itm[K-1]` is unaffected. Whole-group operations consult the GR8 quadrant carried by `OdoGroupPlace.DependingInside`: **sending** (either quadrant) → the current extent [0..N); **receiving with data-name-1 OUTSIDE the group** (GR8a, the common case) → the same current extent [0..N), positions past N left unmodified; **receiving with data-name-1 INSIDE the group** (GR8b) → the full MAX length. Bounds-check K vs N only when the EC-bound checking class is enabled (later).

### Duplicate data-names disambiguated only by qualification — a single-value name index would silently overwrite and resolve the WRONG item. (IMPLEMENTED: `DataBinder.ByName` is the multimap described below.)

Make ByName a MULTIMAP (Dictionary<string,List<DataItem>>). Unqualified resolution: if the list has one entry use it; if >1 and a qualifier is required for uniqueness, emit the ISO §8.4.2.2 ambiguity diagnostic. Qualified resolution: right-to-left narrowing (resolve outermost qualifier, FindChild inward) — port legacy ResolveQualifiedName + FindChild exactly.

### Multi-dimensional OCCURS: legacy flattened to a 1-D byte buffer with per-dimension stepSize multipliers — an offset-arithmetic model that is exactly the byte substrate the owner rejected.

Use the natural .NET shape: a 2-D OCCURS is an array-of-(struct-containing-array), accessed `Rows[i-1].Cols[j-1]`. Each COBOL subscript maps to its own C# array index; NO multipliers, NO stepSize, NO flattened offset. This is simpler AND layout-free. Collect dimensions by walking item→ancestors (the only piece of LocationResolver worth keeping).

### Index-names (INDEXED BY) — legacy modeled them as byte displacements into the table, which leaks layout into a value the program can SET/compare.

Model an index-name as a C# `long` holding a 1-BASED OCCURRENCE NUMBER, not a displacement. SET TO n → assign n; SET UP/DOWN BY k → ±= k; subscript use → [idx-1]; SEARCH/SEARCH ALL emit as integer loops over the occurrence range. The only behavioral difference (an index surviving a table-element-width redefine) is implementor-defined and absent from the conformance corpus.

## Edge cases

- FILLER: no C# member name needed when it carries no VALUE and is never referenced; but a FILLER WITH a VALUE must still initialize its position (matters for whole-group reads and the G6 byte image). Generate a synthetic _fillerN member only when it has a VALUE or affects group serialization.
- Group item used as an alphanumeric operand (MOVE WS-REC TO X, or IF WS-REC = SPACES): a group has no scalar field — its char image is the left-to-right concatenation of all leaf display images. A generated `string AsImage()`/`FromImage()` per struct concatenates/distributes members; whole-group MOVE/compare uses it (all-string leaves and numeric-DISPLAY leaves alike). **Numeric-DISPLAY leaf refinement:** ISO §14.9 MOVE GR4 fills a group "without consideration for the individual elementary items" with **no conversion**, so a numeric-DISPLAY subordinate can receive non-numeric characters (e.g. spaces). A native `long` cannot hold that, so a numeric-DISPLAY leaf **under a group used as a whole operand** is stored as its CHARACTER IMAGE (a `string`); `DataItem.StoreAsImage` — a read-only projection of the item's computed `Storage` form, settled once by `StorageFormPass` — reports it, making `AsImage`/`FromImage` byte-faithful with NO byte[]. Numeric use of such a leaf decodes via `CobolNum.ParseDisplay` / formats via `FormatDisplay`. A leaf never referenced as part of a whole group stays a native `long` (locked invariant #2). A group with a COMP/COMP-3/COMP-5/float leaf is the genuine mixed-usage byte-island (Tier-C), still deferred/loud.
- Numeric item under reference modification (ISO §8.4.3.3.4 rule 2): operate as if redefined alphanumeric of the same size — render the raw zoned/digit display image, ref-mod that, and (if receiving) re-parse back. Defer receiving-numeric-refmod to byte fallback, flag loud.
- Subscript/position single-evaluation (ISO): `TBL(F(X)) (G(Y):H(Z))` — F,G,H must each evaluate exactly once. Emit temps for every non-trivial subscript and ref-mod position before composing the Place.
- Relative subscript `idx - 1` where idx is at occurrence 1 → index 0-1 = -1: a runtime bounds violation (EC-BOUND-SUBSCRIPT). Honor with a checked path when EC enabled; otherwise undefined (matches no-EC corpus behavior).
- Level-88 VALUE with multiple literals AND THRU ranges mixed (VALUE 0 5 THRU 9 12): the condition is an OR over each literal/range — `St==0 || (St>=5&&St<=9) || St==12`. Capture the full value-item list, not just the first (DataBinder.ExtractValue currently grabs only FirstOrDefault — a bug for 88s and for multi-literal table VALUEs).
- SET cond-name TO TRUE picks the FIRST value of a THRU range (the low bound) or the first literal (ISO §14.9.39.4 GR6); SET TO FALSE uses the WHEN SET TO FALSE literal if present (ISO §14.9.39.4 GR7; grammar valueClause supports it).
- JUSTIFIED RIGHT interacts with ref-mod and with numeric MOVE: JUST only applies to alphanumeric/alphabetic receivers (ISO §13.18.32) — diagnose/ignore on numeric. Already plumbed in CobolString.Store(justifiedRight).
- COMP-5 / BINARY-* with no PIC: width-bounded native int with TWO'S-COMPLEMENT WRAP on overflow (not digit truncation) — PicInfo.StorageWidth picks the byte width. `CobolNum.Store` WRAPs and `TryStore` range-checks by `NumProfile.StorageLength` (`WrapBinary`/`InBinaryRange`, branching signed vs unsigned; §14.9.25 GR8 magnitude for unsigned). The BINARY-CHAR family synthesizes via `PicInfo.BinaryItem` (PICTURE-less, §13.16.3 SR8 prohibits a picture → COBOLNET0870).
- 19-38 digit pictures overflow `long` (max 18 digits) → Int128. PicInfo.ClrType/DefaultInitializer/ProfileInitializer and the NumX literal renderer must branch on WidePrecision; CobolNum needs Int128 overloads. Pictures >38 (NATIONAL/2014) are out of scope for v1.
- REDEFINES of a table or by a table; REDEFINES chains (A redefines B redefines C): resolve the ultimate base; the whole chain is ONE redefines class with ONE canonical backing per the 4-tier verdict (A alias / B string canonical / C class-scoped byte canonical / D reject loud — `COBOLNET_REDEFINES_DESIGN.md`); never independent stored fields per view.
- Qualification of an index-name or a LINAGE-COUNTER by file/report name (grammar dataReference alts): index-name qualification is by table name (ISO §8.4.2.2 rule 6) — resolve via the owning table; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER are special registers, not data items — they get dedicated Places.

## Per-edition gating (the G1 obligation)

`cobol.exe` is four compilers in one (`--std 85|2002|2014|2023`, default COBOL-2023). Every edition-varying
construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition
that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests
(NIST etc.) only VERIFY; they never SCOPE. Framework: `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows; derive 85↔2002 gating from the 2002
standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) + `docs/VERSION_TEST_MATRIX_DESIGN.md` (the
(construct × edition) matrix; Phase 0 done). Data-model constructs that MUST be gated:
  • Table dimensions: COBOL-85 caps a table at 7 dimensions; 2002+ removes the fixed cap. >7 dims at
    `--std 85` ⇒ diagnostic; dims are stored as a list with no engine cap.
  • Numeric size: COBOL-85 caps fixed-point at 18 digits; 2002+ raises the cap to 31. PIC 9(19+) at `--std 85`
    ⇒ diagnostic; >31 digits ⇒ diagnostic at every edition (Int128's 38-digit headroom is substrate, not surface).
  • Boolean data (PICTURE symbol `1`, USAGE BIT): 2002 introduction (derive from the 2002 standard) — diagnose
    at `--std 85`.
  • Format 2 (table) VALUE — literals keyed to occurrence ranges via a mandatory `FROM (subscript)` phrase
    (§13.18.63.2): 2002 introduction (derived — the 2023 Annex E VALUE rows are numeric-edited only, the A1
    authority gap) — diagnose at `--std 85` (COBOLNET0900); construct `value-table-format-2002`.
  • SET condition-name TO FALSE / the `VALUE … WHEN SET TO FALSE` phrase: 2002 introduction — diagnose at
    `--std 85`.
  • Zero-length reference modification: edition-varying, 2023-only directive control — see §5
    (REF-MOD-ZERO-LENGTH / EC-BOUND-REF-MOD, `VERSION_CHANGE_REFERENCE.md` #30).
  • `BINARY-CHAR…BINARY-DOUBLE` usages: 2002 introduction — diagnose at `--std 85`; `COMP-5` is a dialect
    extension — flag per the extension policy.
Each bullet gets a (construct × edition) case in the version test matrix: accepted-with-correct-behavior at
editions that have it, rejected-with-the-right-diagnostic below its intro edition.

## ISO citations

- ISO/IEC 1989:2023 §8.4.2.2 Qualification — uniqueness of reference; qualifiers specified in order of successively more inclusive levels; rules 4-6 cover 88 (includes the conditional variable in the hierarchy), condition-name, and index-name (by table-name) qualification
- §8.4.2.3 Subscripts — 1-based occurrence numbers; integer literal / data-name / index-name / relative index ± integer forms; outer-to-inner ordering for multi-dim
- §8.4.3.3 Reference-modification (general rules §8.4.3.3.4, spec lines 6952-7020) — leftmost position + length define a unique subset; rule 2 (non-alphanumeric DISPLAY treated as redefined alphanumeric of same size), rule 3 (NATIONAL likewise), rule 5 (creates a unique data-item subset); rule 5c default length = remaining characters
- §13.18.38 OCCURS clause; GR8 (occurs-depending whole-group extent: with data-name-1 OUTSIDE the group the CURRENT count is used for both directions (GR8a), with it INSIDE the group sending uses the current count and receiving uses the MAX length (GR8b)); INDEXED BY index-names are distinct entities
- §13.18.44 REDEFINES — same storage area; (file-record implicit redefinition)
- §13.18.45 RENAMES (level 66) — alternative grouping over a contiguous run
- §13.18.55 SYNCHRONIZED — natural-boundary alignment (a byte-layout concern; no-op for typed in-memory data)
- §13.18.32 JUSTIFIED — right-justification for alphanumeric/alphabetic receivers only
- §13.18.8 BLANK WHEN ZERO — display all spaces when value is zero
- §13.18.63 / §14.9.4 VALUE clause and initial state — default initial values; table initialization; figurative constants
- §14.9.39 SET statement — condition-name SET TO TRUE assigns the first/low value (GR6), SET TO FALSE the WHEN-SET-TO-FALSE literal (GR7); index-name SET semantics
- §8.8.1 / §14.9.25 arithmetic on the algebraic value regardless of representation; MOVE rules (justify, truncation, GR8 unsigned magnitude)
- Conditional-flag REF-MOD-ZERO-LENGTH (spec line 4523) — zero-length reference modification permitted, yields empty

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Int128 substrate timing — **RESOLVED:** the value engine is Int128-monomorphic (SSOT: the `CobolInt(Int128,scale)` carrier) and `CobolNum`/the numeric renderer carry Int128 support; `WidePrecision` selects the stored type. The SURFACE digit cap stays per-edition (18 at `--std 85`, 31 at 2002+ — see the per-edition gating section); Int128's 38 digits are substrate headroom only.
- COMP-5 / BINARY-* two's-complement WRAP semantics — **RESOLVED (SSOT numeric model; §6 above):** true binary-width wrap by storage width (PIC S9(4) COMP-5 wraps at ±32768), NOT digit-count truncation. The wrap path is in `CobolNum.Store` (`WrapBinary`) and `CobolNum.TryStore` (`InBinaryRange` → SIZE ERROR), keyed off `NumProfile.StorageLength`, signed vs unsigned; the `BINARY-CHAR…DOUBLE` family rides it. Note `BINARY-CHAR…DOUBLE` are 2002+ (per-edition gating section); `COMP-5` is a dialect extension.
- Whole-group-as-alphanumeric — **RESOLVED (SSOT §18 #21):** the generated `string AsImage()`/`FromImage()` per struct IS the permanent typed-native mechanism for whole-group MOVE/compare of **DISPLAY-homogeneous** groups, INCLUDING numeric-DISPLAY leaves (those store their character image via `StoreAsImage` when whole-referenced — see the edge case above). Only groups with a COMP/COMP-3/COMP-5/float (non-character) leaf are the genuine mixed-usage byte-island routed to the Tier-C codec (§4); national-member groups use the same `AsImage` over UTF-16. No byte[] for any DISPLAY-homogeneous group.
- REDEFINES cross-type-read detection — **RESOLVED (the 4-tier model, SSOT §14.3):** with ONE canonical backing per redefines class every write is visible through every view, so no cross-type-read guard exists or is needed; genuine mixed-USAGE puns are Tier C (class-scoped byte canonical) and unmodelable puns are Tier D (reject loud). See `COBOLNET_REDEFINES_DESIGN.md`.
- Passing a ref-modded or subscripted-with-variable receiver as CALL BY REFERENCE: C# `ref` to an array element is legal but `ref` to a ref-mod splice is not. Confirm the policy — diagnose (strict) vs silently promote to BY CONTENT (lenient) — and whether it should be dialect-gated.
