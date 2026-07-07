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
  • INDEX-NAMEs (INDEXED BY): an index-name is a DISTINCT entity, NOT a data item (ISO §8.4.2.3 / §13.18.40). DECISION: an index-name → a C# `long` field holding a 1-BASED OCCURRENCE NUMBER (not a byte displacement; the legacy byte-displacement model is rejected as it leaks layout). SET idx TO n → `idx = n`; SET idx UP/DOWN BY k → `idx ±= k`; using idx as a subscript → `[idx - 1]`. (Rationale: occurrence-number semantics are layout-free and make SEARCH/SEARCH ALL emit as plain integer loops; the only observable difference — idx surviving a redefine of the table element width — is implementor-defined and not in the conformance corpus.) Index-name lives in the same static/instance scope as its table.

== 5. REFERENCE MODIFICATION x(s:l), ISO §8.4.2.4 (spec lines 7028-7081) ==
A typed substring over the item's CHARACTER image. DECISION: ref-mod always operates on the STRING image of the item (ISO §8.4.2.4 rule 2: a non-alphanumeric DISPLAY item is treated as if redefined alphanumeric of the same size; rule 3: NATIONAL likewise). So:
  • Read: `CobolString.RefMod(<charImage>, s, l)` → `charImage.Substring(s-1, lengthOrRest)`; l omitted → to end (ISO §8.4.2.4: default length = remaining). `<charImage>` is the item's display image (string field directly; a numeric item via `CobolNum.FormatDisplay` first — but a numeric ref-mod is rare and the spec says treat-as-alphanumeric-redefinition, so we render the raw digit/zoned image).
  • Write (ref-mod as a RECEIVER, the genuinely hard case): cannot reassign a substring in place on an immutable C# string. Runtime helper `CobolString.SpliceInto(ref string field, int start1, int len, string value)` rebuilds the string: `field = field[..(s-1)] + value-fitted-to-len + field[(s-1+len)..]`. The Place's WriteStmt for a ref-mod emits this. For ref-mod over a numeric/COMP item used as a receiver, route through the byte-image fallback (G6, deferred) — flag loud meanwhile. s and l are arbitrary arithmetic expressions (evaluated once into temps to avoid double-eval; ISO requires single evaluation of subscripts/positions).
  • ZERO-LENGTH ref-mod (l=0) is EDITION-VARYING (`VERSION_CHANGE_REFERENCE.md` #30): pre-2023 the result is undefined; at `--std 2023` it is allowed (yields "") ONLY when the REF-MOD-ZERO-LENGTH directive (§7.3.23) is in effect — otherwise EC-BOUND-REF-MOD is raised; FLAG-14 flags the ambiguous case (spec line 4523). Gate the emit by edition + directive state.

== 6. NATIVE NUMERIC MODEL (owner-locked, reaffirm) ==
Fixed-point = native `long` holding the UNSCALED value; scale is compile-time metadata on PicInfo (already implemented). 19-38 digit pictures → `Int128` (PicInfo gains a `WidePrecision` flag selecting Int128 vs long for ClrType + the runtime overloads; CobolNum must gain Int128 overloads — currently long-only). COMP-1/2 → float/double; COMP-5 → native int by width with binary wrap (PicInfo.StorageWidth already computes the byte width; runtime needs the wrap path, deferred). decimal/BigInteger essentially unused. This is settled (DEVLOG 462); the data-model design only needs to thread `WidePrecision` into ClrType, DefaultInitializer, ProfileInitializer, and the NumX scale-tracking expression type so wide items pick Int128 literals (`123` not `123L`).

== 7. LEVELS 66 (RENAMES) and 88 (condition-names) ==
  • 88 condition-name: NOT a storage item — a named boolean predicate over its parent (the conditional variable). DECISION: emit each 88 as a C# `static bool` PROPERTY (or a method) over the parent Place: `private static bool LvlOk => CobolCond.In(Parent.Read(), <value-or-range-set>);` where the value set comes from the (possibly multi-valued, THRU-ranged) VALUE clause. SET cond TO TRUE → assign the parent its first/low value (ISO §14.9.34). The binder captures 88 entries as `Condition88` records on their conditional variable (IMPLEMENTED — `DataBinder.Conditions` multimap); ensure the captured VALUE list covers THRU ranges + multiple literals.
  • 66 RENAMES: a re-grouping alias over a contiguous run FROM..THRU of sibling elementary items. DECISION: model as a Place that is an ALIAS — for the common case (RENAMES of a single elementary, or a whole-group read/write) emit a computed property that concatenates/splits the underlying members' char images. The general overlapping-bytes RENAMES is a storage-overlay case → defer to G6 (the byte-image fallback) and flag loud. Capture RenamesInfo (FROM/THRU + qualifiers) now; resolution is deferred-pass like legacy.

== 8. REDEFINES — the storage-overlay boundary ==
> ⚠ **SUPERSEDED (2026-06-08).** This section's original DECISION ("the redefining item and the redefined item are
> SEPARATE typed fields; a write to one is NOT auto-visible in the other") is **rejected**. SSOT
> `COBOLNET_DESIGN.md` §14.3 names it the loser: **separate fields reproduce the exact silent-stale-read that
> triggered the whole no-byte-substrate pivot** (a write through one view must be visible through every other view of
> the same storage; two independent fields cannot guarantee that). The **canonical REDEFINES design is the 4-tier
> one-canonical-backing model** in `COBOLNET_DESIGN.md` §4 and the deep-dive `COBOLNET_REDEFINES_DESIGN.md` — follow
> those, not the paragraph below.

REDEFINES makes two differently-typed views share storage. **Current model (4 tiers; priority cascade D>C>B>A — see
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
reuses the legacy `RecordClassificationPass` transitive-closure shape, re-verdicted to the A⊑B⊑C⊑D lattice (join = max
tier). **Why the original "separate fields" plan was dropped:** it silently corrupts on any cross-type pun and re-opens
the DEVLOG-457 failure mode; the one-canonical-backing model keeps Tiers A/B 100% typed (no `byte[]`) while staying
coherent, and confines bytes to genuine mixed-USAGE puns only.

== 9. CLAUSES: SYNCHRONIZED / JUSTIFIED / BLANK WHEN ZERO ==
  • SYNCHRONIZED: alignment is a BYTE-LAYOUT concept. In a typed-native model there are no byte boundaries to align to (a `long` field is naturally aligned by the CLR). DECISION: SYNC is a NO-OP for in-memory typed data; it only matters at the file/byte serialization boundary (G6), where the record-image builder honors it. Capture IsSynchronized for that future use. (Rationale: the only observable effect of SYNC absent byte access is on REDEFINES-overlay size, which is already the G6 byte path.)
  • JUSTIFIED RIGHT: already threaded — CobolString.Store(value,width,justifiedRight) pads/truncates on the left. PicInfo/DataItem must carry IsJustifiedRight (currently not captured); the Place's WriteStmt for an alphanumeric receiver passes it.
  • BLANK WHEN ZERO: an OUTPUT/edit-time rule (ISO §13.18.6) — a numeric/numeric-edited item displays as all spaces when its value is zero. DECISION: a property of the item's display rendering, applied in CobolNum.FormatDisplay / the numeric-edited formatter: if value==0 emit spaces of the picture width. Capture BlankWhenZero on PicInfo.

== 10. VALUE INITIALIZATION incl. TABLES, ISO §13.18.63 / §14.9.4 ==
Default (no VALUE): alphanumeric→spaces, numeric→0 (unscaled), index→1, pointer→null. Already implemented for flat items. Extensions needed:
  • Group with VALUE: initialize every subordinate per the group literal spread across the group's char positions (rare; the common form is per-elementary VALUE). 
  • OCCURS with VALUE: ISO permits VALUE on a table item → every element initialized to that value. Emit a collection-expression / array initializer: `Items = [.. Enumerable.Repeat(elemInit, n)]` or an explicit `new _T_Item[]{ init, init, … }`. Multi-literal table VALUE (one literal per element, COBOL-2002) → positional initializer list, padded with the last/default.
  • Figurative constants (ZERO/SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/ALL "x"): map to the typed default of the right width — ZERO→0L or "0000", SPACE→new string(' ',w), ALL "AB"→repeat to width. Capture FigurativeInit + AllLiteralPattern (legacy already models these; port).
  The whole 01 field's initializer is a single C# object-initializer expression composed recursively from the leaves — emitted in the static field declaration (program) or the instance ctor (OO), matching DEVLOG 456's per-instance init.

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
> **Status (2026-06-10):** this was the original G2 worklist; much has LANDED in `src/Cobol.Net.Compiler`
> (`Place`/`ReferenceResolver`, nested record-struct emission, the `ByName` multimap, `Condition88` capture,
> `WidePrecision`/Int128). Treat the list below as the design inventory; the live remaining-work tracker is
> `resume-prompt.md`, not this section.
DataItem: add IsJustifiedRight, IsSynchronized, BlankWhenZero, RedefinesName/Redefines, RenamesInfo, OccursInfo (min/max/dependingOn/indexNames/keys), IsGroup-aware ClrType using `_T_` struct names + array `[]`, Level-88 ValueSet, FigurativeInit/AllLiteralPattern, WidePrecision. DataBinder: stop skipping 66/88; capture all clauses; build a name MULTIMAP; collect OCCURS dims + INDEXED BY index-names as their own entities; deferred-resolution pass for REDEFINES/RENAMES/DEPENDING-ON targets. PicInfo: WidePrecision (Int128), BlankWhenZero, IsJustifiedRight; ParseUsage already covers usages. New `ReferenceResolver`(→Place) + the suffix flattener (port legacy SUB_* interpretation). CSharpEmitter: stop flattening groups to leaves (DEVLOG 458's stopgap) — emit nested record-struct TYPES + composed initializers; route every operand through Place. Runtime: CobolString.RefMod/SpliceInto, CobolCond.In, Int128 overloads of CobolNum, COMP-5 wrap.

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

### D5. REDEFINES uses the 4-tier ONE-canonical-backing model (A Alias / B StringCanonical / C class-scoped ByteCanonical / D reject-loud); every non-canonical view is a computed `Place` accessor over the single backing — never two stored fields per storage area. [REVISED 2026-06-08; canonical: `COBOLNET_DESIGN.md` §4 + `COBOLNET_REDEFINES_DESIGN.md`]

**Rationale.** A write through any view must be visible through every other view of the same area (ISO §13.18.42 "same storage area"). One stored canonical + computed accessors guarantees that coherence with NO byte substrate for the dominant DISPLAY-homogeneous case (Tiers A/B — the entire near-term NIST path), and confines `byte[]` to genuine mixed-USAGE puns only (Tier C, owner decision §18 #1).

**Rejected alternatives.** (a) **The original D5 "separate independent typed fields"** — rejected (SSOT §14.3 names it the loser): two independent fields cannot stay coherent under a cross-type pun, reproducing the silent-stale-read that triggered the DEVLOG 457 pivot — even a loud cross-type-read guard only *detects* it, it does not make the program correct. (b) Global `byte[]` for all REDEFINES — rejected: the abolished substrate. (c) `[StructLayout(Explicit)]`/`[FieldOffset]` overlay — rejected: cannot overlay a `string` on a `long`, which is the dominant pun.

### D6. SYNCHRONIZED is a no-op for in-memory typed data; honored only at the file/byte-serialization boundary (G6). BLANK WHEN ZERO and JUSTIFIED are display/store-time rules on PicInfo.

**Rationale.** Alignment has no meaning without byte addresses (the CLR aligns a `long` naturally); its only observable effect is on overlay/serialization size, which is already the byte path. BLANK-WHEN-ZERO/JUSTIFIED are pure value-rendering rules.

**Rejected alternatives.** Modeling SYNC by inserting padding fields — rejected: reintroduces layout and is invisible to typed access.

### D7. Fixed-point stays native long (unscaled, compile-time scale); 19-38 digits → Int128 via a WidePrecision flag; no decimal/BigInteger.

> **✅ SHIPPED (DEVLOG 540), refinement:** the wide selector landed as the DERIVED property `PicInfo.IsWide`
> (`Numeric && !IsFloat && Digits > 18`) driving `ClrType`/`DefaultInitializer` — not a stored flag (`Digits` is
> already on PicInfo; a parallel flag could drift from it). Literals wider than long emit `Int128.Parse("…")`
> (`EmitText.IntLiteral` — C# has no Int128 literal form); the store boundary narrows via the width-aware
> `CSharpEmitter.Narrow`. The SURFACE cap is per-edition (18 at `--std 85`, 31 at 2002+, >31 rejected everywhere —
> `EditionContext.CheckDigitCapacity`, COBOLNET0801/0802); Int128's 38 digits are substrate headroom only.

**Rationale.** Owner-locked (DEVLOG 462): hardware-native, exact, DISPLAY image falls out for free; Int128 is a fixed-size value type far cheaper than BigInteger.

**Rejected alternatives.** decimal (software, not hardware-native) and BigInteger (heap-allocating) — both rejected by the owner.

### D8. National and boolean data ride the fixed-width STRING substrate (Phase 4a, 2026-07-05 — the M2-DATA-3/4 design in `PHASE4_RECONCILIATION.md` carries the full decision set D-N1..D-N4/D-B1).

- **D-N1 national**: an elementary national item is a plain C# `string` of `Length` characters — .NET strings are
  natively UTF-16, so "two bytes per character position" is the documented implementor choice (§13.18.60.4 GR8 +
  §8.1.2 NOTE 2). ALL width machinery stays CHARACTER-position based; `ImageWidth` is NEVER byte-doubled.
- **D-N2 byte≠char containment**: every byte-addressed surface REFUSES a national leaf loud (REDEFINES ComputeTier,
  EXTERNAL/ADDRESS-OF/BASED cells via ForceStringCanonical, FD/SD records, SORT keys) until the 2-byte layout
  residue lands (RESIDUE-11 coordination with the pointer track).
- **D-N3 collating**: national comparisons order by UTF-16 ordinal (the implementor default national sequence);
  the ALPHANUMERIC program collating sequence never applies (separate sequences — §8.8.4.2.9; the 256-entry
  weight table would alias national chars through `& 0xFF`).
- **D-N4 repertoire**: Latin-1 (≤ U+00FF) this phase; the only wider-char source is an N"…" literal, 0814 at bind.
- **D-B1 boolean**: one alphanumeric character '0'/'1' per boolean position for BOTH usage display AND usage BIT —
  the §13.18.40.4 GR14 R14 license, a PERMANENTLY conforming choice. byte=char HOLDS: boolean leaves are admitted
  at every character surface (Tier-B windows, images, records, cells for the display form). The category
  difference is carried entirely by the CobolString pad parameter ('0' fills, §14.6.8.6).

**Rationale.** The string substrate gives MOVE/compare/ref-mod/images for free on the ONE proven machinery;
the alternatives (a 1-byte national size, C# bool for PIC 1, bit-packing) each break a spec surface —
see the reconciliation design's F4 and the residue ledger.

**Rejected alternatives.** (a) The C# mapping sketch's original `PIC 1 → bool` — superseded (multi-position
PIC 1(n)/fills/ref-mod need character semantics; a bool cannot carry them). (b) GR8's size-equal-alphanumeric
national — rejected for forward-compat (the non-Latin-1/NX"/BYTE-LENGTH residue presumes 2-byte). (c) True
bit-packing for USAGE BIT — optional forever under R14; revisit only with GROUP-USAGE BIT.

### D9. OCCURS DYNAMIC (dynamic-capacity tables, §13.18.38 Format 4, COBOL-2014) — an out-of-line growable `CobolDynTable<T>`; sending/receiving direction carried by `Place`; a CORE ships whole, variable-length-group ops staged LOUD.

*Decision-complete design — recon wf_32407556-b96 (2026-07-07). Load-bearing claims verified verbatim: §8.5.1.9.1
:8189 (dynamic-capacity definition — physical=logical capacity, current capacity, non-contiguous implementor-defined
allocation, FROM/VALUE initial, TO expected); §13.18.38 Format 4 :19858 (`OCCURS DYNAMIC [CAPACITY IN data-name-3]
[FROM integer-4] [TO integer-5] [INITIALIZED]` + ASC/DESC KEY + INDEXED BY); the `occursClause` grammar has NO
DYNAMIC form (CobolData.g4:330); `ExceptionCatalog` ALREADY registers EC-BOUND-OVERFLOW(nonfatal)/-TABLE-LIMIT(fatal)/
EC-FLOW-SEARCH(fatal). Full design in the recon transcript; the decision + increments are here (SSOT).*

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
initializer (reuse `FieldEmitter.ComposedInit`/`InitializerFor`; heed the DEVLOG-643 seed-EVERY-occurrence lesson) —
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
subordinate (**1526**, §13.7.1 SR6) · REDEFINES subject/object a dynamic table (**1525**, §13.18.44 SR17).

**Grammar (SHARED .g4 → FULL legacy guard; additive).** A new `CAPACITY` lexer token; a `{is2014()}?`-gated DYNAMIC
alt on `occursClause` (LL-disjoint — DYNAMIC is not an integerLiteral) with an order-independent `occursDynamicPhrase*`;
`EditionGateHints` entry so a pre-2014 probe upgrades to **COBOLNET0900**. NO SET grammar change (Format 14 is the
existing SET syntax, binder-rerouted). Diagnostics: **1522** (declaration/placement/SR28 — FILE SECTION, ODO-nesting,
FROM/TO bounds, dup phrase), **1523** (CAPACITY register misuse SR30–32), **1524** (SET Format 14 misuse), 1525–1528
(staged-loud). (08xx band exhausted; 15xx, last-used 1521.)

**Increments (each: build → full greenfield battery → legacy guard → commit):** (1) **✅ LANDED (DEVLOG 652)** —
grammar (`CAPACITY` token + the `OCCURS DYNAMIC occursDynamicPhrase* …` alt, `{is2014()}?`-gated) + `OccursSpec`
dynamic fields + `DataItem.IsDynamicTable`/`IsTable`/`FieldType` (+ image-capable exclusions) + `CobolDynTable<T>` +
`FieldInit` dynamic branch + `OdoBindOccursSpec` Format-4 branch + `EditionGateHints` gate + the matrix row
(`occurs-dynamic-2014`, active) — the ONLY grammar/legacy-guard slice → golden `dyn_declare` (a group-element table,
greenfield-only). **Two plan refinements recorded (process rule):** (a) NO VCR row — the VCR's own preamble (line 20)
states 2002→2014 *introductions* are captured by the matrix `introducedIn` tag, not the VCR (which carries only
2014→2023 Annex-E deltas + a few 2002→2014 behavior rows); the `constructs.json` + `ConstructDialectStatus` pair IS the
canonical introduction record. (b) NO separate `dyn_pre2014` corpus golden — the corpus runner asserts compile+run
SUCCESS only; the below-2014 **COBOLNET0900** rejection is asserted by the matrix row's `expectDiagnostic` at editions
85/2002 (`VersionMatrixTests`), the one place negative gating belongs. `DynamicResolve` (CAPACITY-register/access
resolution) moved to inc 2/3 where the register becomes a readable item;
(2) CAPACITY read + SET Format 14 + capacity ECs → `dyn_declare_capacity`/`dyn_from_to`/`dyn_set_grow`/
`dyn_set_up_down`/`dyn_bounds_overflow`; (3) subscripted element access (`AccessDir` in `AccessPath` +
`CobolTable.At(CobolDynTable, occ, receiving)` + `RefSending`/`RefReceiving`/`GrowTo`) → `dyn_implicit_grow`/
`dyn_initialized`; (4) SEARCH over current capacity + EC-FLOW-SEARCH + `INITIALIZE` → `dyn_search`, flip the matrix
row active; (5) the staged-loud guards (1525–1528) + doc/DOC_INDEX + negative goldens. Increments 2–5 are
greenfield-only (guard-fast; CI is the backstop). All 3 recon open questions resolved to their recommended defaults
(VALUE-capacity staged; EC-FLOW-SEARCH in CORE; extend THIS doc).

## C# mapping

CONCRETE COBOL→C# MAPPINGS:

— Elementary —
  05 NAME  PIC X(10) VALUE "BOB".        →  public string Name; … Name = CobolString.Store("BOB",10);
  05 CT    PIC 9(4).                      →  public long Ct;  // unscaled, scale 0; init 0L
  05 AMT   PIC S9(5)V99 COMP-3.           →  public long Amt; // unscaled (7 digits), scale=2; profile threads truncation=Packed
  05 BIG   PIC 9(30).                     →  public Int128 Big;  // WidePrecision → Int128
  05 RATE  COMP-2.                        →  public double Rate;
  05 FLAG  PIC 1(4).                      →  public string Flag; // boolean: one '0'/'1' CHAR per position (D8) — NOT a C# bool (superseded 2026-07-05: the original bool sketch predated multi-position PIC 1(n), MOVE fills, and ref-mod; §13.18.40.4 GR14 R14 licenses the character representation). 2002+ — 0900 at --std 85.
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
      PIC X(4).                                // sending op uses [0..N); receiving whole-group uses MAX (ISO OCCURS GR7)
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
  SET OK TO TRUE                           →  St = 1;     // parent set to the (first) condition value (ISO §14.9.34)

— Qualified + subscripted + ref-modded together —
  VAL OF ITEMS(I) OF WS-REC (3:2)          →  CobolString.RefMod(WsRec.Items[I - 1].Val_as_image, 3, 2)
                                              (Val numeric → its display image first per ISO §8.4.2.4 rule 2)

— CALL BY REFERENCE (Place → C# ref) —
  CALL "SUB" USING CT.                      →  Sub(ref WsRec.Ct);
  CALL "SUB" USING BY CONTENT NAME.         →  Sub(WsRec.Name);   // copy

— VALUE on a table —
  05 TBL OCCURS 3 PIC 9 VALUE ZERO.         →  Tbl = [0L, 0L, 0L];
  05 DAYS OCCURS 2 PIC X(3) VALUE "JAN".    →  Days = [CobolString.Store("JAN",3), CobolString.Store("JAN",3)];

## Hard problems

### The grammar captures subscript and ref-mod content as ONE undifferentiated raw token stream (subToken+ in SUBSCRIPT lexer mode); `(I J)` (2 subscripts) and `(3:2)` (ref-mod) and `(I)(3:2)` (subscript THEN ref-mod) are syntactically identical at the rule level — only the presence of SUB_COLON distinguishes them.

Port the legacy ExpressionBinder's SUB_* token interpreter verbatim (it is proven over 364 NIST tests): for each `(...)` suffix group, scan tokens — if it contains SUB_COLON it is a ref-mod (split into start/length sub-expressions at the colon), else it is a subscript list (split on SUB_WS / SUB_COMMA into N subscript expressions, each itself possibly a relative `idx ± lit`). Phase-A flatten produces a clean {qualifiers[], subscriptGroups[][], refMod?} that Phase-B resolves. Do NOT try to re-grammar this — the SUBSCRIPT-mode design is intentional and reusing the interpreter avoids re-deriving COBOL subscript edge cases.

### REDEFINES is a byte-level storage overlay with no clean typed-native equivalent: two differently-typed C# fields cannot share memory, so a write through one view is invisible to the other.

**Resolution (REVISED — 4-tier one-canonical-backing; see `COBOLNET_REDEFINES_DESIGN.md`).** One STORED canonical per redefines class + every other view a computed `Place` accessor. **Tier A** (identical PIC/USAGE) = pass-through accessor; **Tier B** (whole class USAGE DISPLAY — the corpus majority) = a single `string` canonical with substring/`ParseDisplay`/`FormatDisplay` accessors (NO bytes); **Tier C** (genuine mixed-USAGE pun) = one class-scoped `byte[]` canonical with per-leaf typed codecs, confined to the class and never persisted; **Tier D** (unmodelable) = reject loud. The earlier "independent typed fields + a loud cross-type-read guard" plan is SUPERSEDED (SSOT §14.3): independent fields cannot stay coherent under a pun, so detection alone never makes the program correct — only the single shared canonical does.

### Reference modification as a RECEIVER: C# strings are immutable, so `NAME(3:2) = x` cannot splice in place.

Place.Write for a RefModPlace rebuilds the whole string: `field = field[..(s-1)] + fit(value,len) + field[(s-1+len)..]` via runtime CobolString.SpliceInto(string,start1,len,value). Evaluate s and l into temps ONCE (ISO requires single evaluation of positions/subscripts) to avoid double-eval and side-effect duplication. For numeric/COMP receivers under ref-mod (rare), defer to the byte-image fallback and flag loud.

### OCCURS DEPENDING ON: the array size varies at runtime, but a C# array has a fixed allocated length; and ISO OCCURS GR7 mandates that a RECEIVING whole-group operand uses the MAXIMUM length while a SENDING operand uses the CURRENT (DEPENDING-ON) length.

Allocate the array at MAX occurrences once; the length variable (DEPENDING ON item) bounds the LIVE range. Element access `Itm[K-1]` is unaffected. Whole-group operations branch on direction: sending → slice [0..N); receiving → full MAX (matches legacy IrOdoGroupLocation receiving:true logic, DEVLOG 290). When the DEPENDING-ON var is INSIDE the group, a receiving op still uses MAX (legacy dependOnInside rule). Bounds-check K vs N only when the EC-bound checking class is enabled (later).

### Duplicate data-names disambiguated only by qualification — a single-value name index would silently overwrite and resolve the WRONG item. (IMPLEMENTED: `DataBinder.ByName` is the multimap described below.)

Make ByName a MULTIMAP (Dictionary<string,List<DataItem>>). Unqualified resolution: if the list has one entry use it; if >1 and a qualifier is required for uniqueness, emit the ISO §8.4.2.2 ambiguity diagnostic. Qualified resolution: right-to-left narrowing (resolve outermost qualifier, FindChild inward) — port legacy ResolveQualifiedName + FindChild exactly.

### Multi-dimensional OCCURS: legacy flattened to a 1-D byte buffer with per-dimension stepSize multipliers — an offset-arithmetic model that is exactly the byte substrate the owner rejected.

Use the natural .NET shape: a 2-D OCCURS is an array-of-(struct-containing-array), accessed `Rows[i-1].Cols[j-1]`. Each COBOL subscript maps to its own C# array index; NO multipliers, NO stepSize, NO flattened offset. This is simpler AND layout-free. Collect dimensions by walking item→ancestors (the only piece of LocationResolver worth keeping).

### Index-names (INDEXED BY) — legacy modeled them as byte displacements into the table, which leaks layout into a value the program can SET/compare.

Model an index-name as a C# `long` holding a 1-BASED OCCURRENCE NUMBER, not a displacement. SET TO n → assign n; SET UP/DOWN BY k → ±= k; subscript use → [idx-1]; SEARCH/SEARCH ALL emit as integer loops over the occurrence range. The only behavioral difference (an index surviving a table-element-width redefine) is implementor-defined and absent from the conformance corpus.

## Edge cases

- FILLER: no C# member name needed when it carries no VALUE and is never referenced; but a FILLER WITH a VALUE must still initialize its position (matters for whole-group reads and the G6 byte image). Generate a synthetic _fillerN member only when it has a VALUE or affects group serialization.
- Group item used as an alphanumeric operand (MOVE WS-REC TO X, or IF WS-REC = SPACES): a group has no scalar field — its char image is the left-to-right concatenation of all leaf display images. A generated `string AsImage()`/`FromImage()` per struct concatenates/distributes members; whole-group MOVE/compare uses it (IMPLEMENTED — DEVLOG 488 for all-string leaves; DEVLOG 490 for numeric-DISPLAY leaves). **Numeric-DISPLAY leaf refinement (IMPLEMENTED, DEVLOG 490 — spec-grounded):** ISO §14.9 MOVE GR4 fills a group "without consideration for the individual elementary items" with **no conversion**, so a numeric-DISPLAY subordinate can receive non-numeric characters (e.g. spaces). A native `long` cannot hold that, so a numeric-DISPLAY leaf **under a group used as a whole operand** is stored as its CHARACTER IMAGE (a `string`; `DataItem.StoreAsImage`, set by the bind-time whole-group pass), making `AsImage`/`FromImage` byte-faithful with NO byte[]. Numeric use of such a leaf decodes via `CobolNum.ParseDisplay` / formats via `FormatDisplay`. A leaf never referenced as part of a whole group stays a native `long` (locked invariant #2). A group with a COMP/COMP-3/COMP-5/float leaf is the genuine mixed-usage byte-island (Tier-C), still deferred/loud.
- Numeric item under reference modification (ISO §8.4.2.4 rule 2): operate as if redefined alphanumeric of the same size — render the raw zoned/digit display image, ref-mod that, and (if receiving) re-parse back. Defer receiving-numeric-refmod to byte fallback, flag loud.
- Subscript/position single-evaluation (ISO): `TBL(F(X)) (G(Y):H(Z))` — F,G,H must each evaluate exactly once. Emit temps for every non-trivial subscript and ref-mod position before composing the Place.
- Relative subscript `idx - 1` where idx is at occurrence 1 → index 0-1 = -1: a runtime bounds violation (EC-BOUND-SUBSCRIPT). Honor with a checked path when EC enabled; otherwise undefined (matches no-EC corpus behavior).
- Level-88 VALUE with multiple literals AND THRU ranges mixed (VALUE 0 5 THRU 9 12): the condition is an OR over each literal/range — `St==0 || (St>=5&&St<=9) || St==12`. Capture the full value-item list, not just the first (DataBinder.ExtractValue currently grabs only FirstOrDefault — a bug for 88s and for multi-literal table VALUEs).
- SET cond-name TO TRUE picks the FIRST value of a THRU range (the low bound) or the first literal (ISO §14.9.34); SET TO FALSE uses the WHEN SET TO FALSE literal if present (grammar valueClause supports it).
- JUSTIFIED RIGHT interacts with ref-mod and with numeric MOVE: JUST only applies to alphanumeric/alphabetic receivers (ISO §13.18.30) — diagnose/ignore on numeric. Already plumbed in CobolString.Store(justifiedRight).
- COMP-5 / BINARY-* with no PIC: width-bounded native int with TWO'S-COMPLEMENT WRAP on overflow (not digit truncation) — PicInfo.StorageWidth picks the byte width. **IMPLEMENTED (DEVLOG 614, M2-DATA-1):** `CobolNum.Store` WRAPs and `TryStore` range-checks by `NumProfile.StorageLength` (`WrapBinary`/`InBinaryRange`, branching signed vs unsigned; §14.9.25 GR8 magnitude for unsigned). The BINARY-CHAR family synthesizes via `PicInfo.BinaryItem` (PICTURE-less, §13.16.3 SR8 prohibits a picture → COBOLNET0870). The former `%= Pow10(Digits)` stub is retired.
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
  • Multi-literal table VALUE (one literal per element): 2002 introduction — diagnose at `--std 85`.
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
- §8.4.2.4 Reference modification (spec lines 7028-7081) — leftmost position + length define a unique subset; rule 2 (non-alphanumeric DISPLAY treated as redefined alphanumeric of same size), rule 3 (NATIONAL likewise), rule 5 (creates a unique alphanumeric subset); default length = remaining characters
- §13.18.40 OCCURS clause + GR7 (DEPENDING ON receiving uses MAX length, sending uses current; INDEXED BY index-names are distinct entities)
- §13.18.42 REDEFINES — same storage area; (file-record implicit redefinition)
- §13.18.43 RENAMES (level 66) — alternative grouping over a contiguous run
- §13.18.55 SYNCHRONIZED — natural-boundary alignment (a byte-layout concern; no-op for typed in-memory data)
- §13.18.30 JUSTIFIED — right-justification for alphanumeric/alphabetic receivers only
- §13.18.6 BLANK WHEN ZERO — display all spaces when value is zero
- §13.18.63 / §14.9.4 VALUE clause and initial state — default initial values; table initialization; figurative constants
- §14.9.34 SET statement — condition-name SET TO TRUE assigns the first/low value; index-name SET semantics
- §8.8.1 / §14.9.25 arithmetic on the algebraic value regardless of representation; MOVE rules (justify, truncation, GR8 unsigned magnitude)
- Conditional-flag REF-MOD-ZERO-LENGTH (spec line 4523) — zero-length reference modification permitted, yields empty

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Int128 substrate timing — **RESOLVED:** the value engine is Int128-monomorphic (SSOT: the `CobolInt(Int128,scale)` carrier) and `CobolNum`/the numeric renderer carry Int128 support; `WidePrecision` selects the stored type. The SURFACE digit cap stays per-edition (18 at `--std 85`, 31 at 2002+ — see the per-edition gating section); Int128's 38 digits are substrate headroom only.
- COMP-5 / BINARY-* two's-complement WRAP semantics — **RESOLVED + SHIPPED (SSOT numeric model; §6 above; DEVLOG 462 → 614):** true binary-width wrap by storage width (PIC S9(4) COMP-5 wraps at ±32768), NOT digit-count truncation. The wrap path is LANDED in `CobolNum.Store` (`WrapBinary`) and `CobolNum.TryStore` (`InBinaryRange` → SIZE ERROR), keyed off `NumProfile.StorageLength`, signed vs unsigned (DEVLOG 614, M2-DATA-1); the `BINARY-CHAR…DOUBLE` family rides it. Note `BINARY-CHAR…DOUBLE` are 2002+ (per-edition gating section); `COMP-5` is a dialect extension.
- Whole-group-as-alphanumeric — **RESOLVED (SSOT §18 #21; DEVLOG 488/490):** the generated `string AsImage()`/`FromImage()` per struct IS the permanent typed-native mechanism for whole-group MOVE/compare of **DISPLAY-homogeneous** groups, INCLUDING numeric-DISPLAY leaves (those store their character image via `StoreAsImage` when whole-referenced — see the edge case above). Only groups with a COMP/COMP-3/COMP-5/float (non-character) leaf are the genuine mixed-usage byte-island routed to the Tier-C codec (§4); national-member groups use the same `AsImage` over UTF-16. No byte[] for any DISPLAY-homogeneous group.
- REDEFINES cross-type-read detection — **SUPERSEDED (the 4-tier model, SSOT §14.3):** with ONE canonical backing per redefines class every write is visible through every view, so no cross-type-read guard exists or is needed; genuine mixed-USAGE puns are Tier C (class-scoped byte canonical) and unmodelable puns are Tier D (reject loud). See `COBOLNET_REDEFINES_DESIGN.md`.
- Passing a ref-modded or subscripted-with-variable receiver as CALL BY REFERENCE: C# `ref` to an array element is legal but `ref` to a ref-mod splice is not. Confirm the policy — diagnose (strict) vs silently promote to BY CONTENT (lenient) — and whether it should be dialect-gated.
