# COBOL.NET — Data Model (records, tables, references) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §3; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

DECISION-COMPLETE DESIGN: COBOL DATA DIVISION → typed-native C#, for COBOL.NET.

== 0. THE CENTRAL IDEA: every reference is a Place ==
The whole subsystem is organized around ONE abstraction — a `Place` — that names a typed C# location and serves MOVE, arithmetic, file I/O, and CALL-by-reference identically. This replaces the legacy `(byte[],offset,length)` IrLocation. A Place has two emission methods used by every consumer:
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
Algorithm (port legacy ResolveQualifiedName): resolve the rightmost qualifier Z as a 01/standalone, then FindChild(Z,Y), then FindChild(ctxY, X) — successively LESS inclusive. FindChild searches the group subtree (recursive). A bare unqualified name resolves by: unique-in-program → that item; else require qualification (diagnose CBL-ambiguous if >1 candidate and no qualifier — ISO §8.4.2.2 rule: uniqueness must be established). The DataBinder.ByName index must become a MULTIMAP (List<DataItem> per name) because COBOL permits duplicate names disambiguated only by qualification — the current single-value Dictionary is a latent bug. The Place records the full member path (Z.Y.X chain) so emission is `Z.Y.X`.

== 4. SUBSCRIPTING / INDEXING, ISO §8.4.2.3 ==
OCCURS dimensions are collected by walking item→ancestors (legacy LocationResolver does exactly this). COBOL subscripts are 1-BASED and listed OUTER→INNER (`T(outer, inner)`); C# arrays are 0-based — so each subscript emits as `[expr - 1]`. Multi-dim 1-3 (COBOL-85 cap; 2002 raises to 7 — store dims as a list, no fixed cap). Each dimension is a SEPARATE C# array index because a 2-D OCCURS is an array-of-structs-containing-array (`Rows[i-1].Cols[j-1]`), NOT a flattened 1-D — this is the natural .NET shape and removes all the legacy multiplier/stepSize offset arithmetic.
  • Subscript forms: integer literal, data-name, index-name, and relative `index ± literal` (ISO §8.4.2.3) → `idx ± lit - 1`.
  • INDEX-NAMEs (INDEXED BY): an index-name is a DISTINCT entity, NOT a data item (ISO §8.4.2.3 / §13.18.40). DECISION: an index-name → a C# `long` field holding a 1-BASED OCCURRENCE NUMBER (not a byte displacement; the legacy byte-displacement model is rejected as it leaks layout). SET idx TO n → `idx = n`; SET idx UP/DOWN BY k → `idx ±= k`; using idx as a subscript → `[idx - 1]`. (Rationale: occurrence-number semantics are layout-free and make SEARCH/SEARCH ALL emit as plain integer loops; the only observable difference — idx surviving a redefine of the table element width — is implementor-defined and not in the conformance corpus.) Index-name lives in the same static/instance scope as its table.

== 5. REFERENCE MODIFICATION x(s:l), ISO §8.4.2.4 (spec lines 7028-7081) ==
A typed substring over the item's CHARACTER image. DECISION: ref-mod always operates on the STRING image of the item (ISO §8.4.2.4 rule 2: a non-alphanumeric DISPLAY item is treated as if redefined alphanumeric of the same size; rule 3: NATIONAL likewise). So:
  • Read: `CobolString.RefMod(<charImage>, s, l)` → `charImage.Substring(s-1, lengthOrRest)`; l omitted → to end (ISO §8.4.2.4: default length = remaining). `<charImage>` is the item's display image (string field directly; a numeric item via `CobolNum.FormatDisplay` first — but a numeric ref-mod is rare and the spec says treat-as-alphanumeric-redefinition, so we render the raw digit/zoned image).
  • Write (ref-mod as a RECEIVER, the genuinely hard case): cannot reassign a substring in place on an immutable C# string. Runtime helper `CobolString.SpliceInto(ref string field, int start1, int len, string value)` rebuilds the string: `field = field[..(s-1)] + value-fitted-to-len + field[(s-1+len)..]`. The Place's WriteStmt for a ref-mod emits this. For ref-mod over a numeric/COMP item used as a receiver, route through the byte-image fallback (G6, deferred) — flag loud meanwhile. s and l are arbitrary arithmetic expressions (evaluated once into temps to avoid double-eval; ISO requires single evaluation of subscripts/positions).
  • ZERO-LENGTH ref-mod (l=0) is conditionally-flagged per spec line 4523 (REF-MOD-ZERO-LENGTH) — allowed, yields "".

== 6. NATIVE NUMERIC MODEL (owner-locked, reaffirm) ==
Fixed-point = native `long` holding the UNSCALED value; scale is compile-time metadata on PicInfo (already implemented). 19-38 digit pictures → `Int128` (PicInfo gains a `WidePrecision` flag selecting Int128 vs long for ClrType + the runtime overloads; CobolNum must gain Int128 overloads — currently long-only). COMP-1/2 → float/double; COMP-5 → native int by width with binary wrap (PicInfo.StorageWidth already computes the byte width; runtime needs the wrap path, deferred). decimal/BigInteger essentially unused. This is settled (DEVLOG 462); the data-model design only needs to thread `WidePrecision` into ClrType, DefaultInitializer, ProfileInitializer, and the NumX scale-tracking expression type so wide items pick Int128 literals (`123` not `123L`).

== 7. LEVELS 66 (RENAMES) and 88 (condition-names) ==
  • 88 condition-name: NOT a storage item — a named boolean predicate over its parent (the conditional variable). DECISION: emit each 88 as a C# `static bool` PROPERTY (or a method) over the parent Place: `private static bool LvlOk => CobolCond.In(Parent.Read(), <value-or-range-set>);` where the value set comes from the (possibly multi-valued, THRU-ranged) VALUE clause. SET cond TO TRUE → assign the parent its first/low value (ISO §14.9.34). The binder must capture 88 entries (currently SKIPPED in DataBinder.BindEntry) and their VALUE list incl. THRU ranges + multiple literals.
  • 66 RENAMES: a re-grouping alias over a contiguous run FROM..THRU of sibling elementary items. DECISION: model as a Place that is an ALIAS — for the common case (RENAMES of a single elementary, or a whole-group read/write) emit a computed property that concatenates/splits the underlying members' char images. The general overlapping-bytes RENAMES is a storage-overlay case → defer to G6 (the byte-image fallback) and flag loud. Capture RenamesInfo (FROM/THRU + qualifiers) now; resolution is deferred-pass like legacy.

== 8. REDEFINES — the storage-overlay boundary (G6) ==
REDEFINES makes two differently-typed views share storage; that is fundamentally a byte-overlay and has NO clean typed-native form. DECISION (matches architecture §3 "deferred unbounded cases"): in the typed model, the redefining item and the redefined item are SEPARATE typed fields; a write to one is NOT auto-visible in the other. This is correct ONLY when the program reads each view consistently (the overwhelming corpus majority — REDEFINES used for a VALUE-init alias or an alternate-name). When a program writes one view and reads the OTHER's type (a genuine byte pun), the typed model is wrong → that is the classifier-scoped byte-image fallback (G6): both views share a `byte[]` scratch materialized at the overlay boundary, exactly the legacy mechanism but ISLANDED to redefined groups only. The data-model design's job now: (a) capture RedefinesName/Redefines (currently dropped in DataBinder), (b) for the safe case emit independent fields with the redefining field's VALUE init suppressed unless it has its own VALUE, (c) emit a LOUD diagnostic when a cross-type read of a redefined region is detected, so nothing silently corrupts (the lesson from DEVLOG 457). FILLER under a REDEFINES needs no field.

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
Concrete kinds:
  • MemberPlace(path)        Read=`path`            Write=`path = rhs;`           (qualified+nested+array indices folded into one access string)
  • RefModPlace(inner,s,l)   Read=`CobolString.RefMod(inner.Read(),s,l)`  Write=`{var t=inner.Read(); inner.Write(CobolString.SpliceInto(t,s,l,rhs));}`
  • Condition88Place(parent,valueset)  Read=`CobolCond.In(parent.Read(),…)`  Write(true)=set parent to value.
Every verb emitter (MOVE/ADD/COMPUTE/file READ INTO/WRITE FROM/CALL USING) takes Places and never touches layout — the unification the task demands. CALL BY REFERENCE passes the receiver Place's address: since a `record struct` member or array element is a C# variable, emit `ref` (e.g. `Sub(ref WsRec.Count)`) for BY REFERENCE; BY CONTENT copies the Read(). (A ref-mod or 88 receiver cannot be passed by ref → diagnose or pass by content per ISO.)

== 12. SUMMARY OF REQUIRED CHANGES TO EXISTING CODE ==
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

### D5. REDEFINES emits independent typed fields (safe tier) with a LOUD diagnostic on detected cross-type reads; genuine byte-puns route to a classifier-scoped byte[] scratch islanded to the overlay (G6).

**Rationale.** The corpus majority uses REDEFINES as an alias/VALUE-init, which the typed-separate-fields model handles correctly and readably; the byte fallback is confined to true overlays, honoring 'byte image only as a scoped fallback' (architecture §3). The loud-on-cross-type-read guard prevents exactly the silent stale-byte corruption that caused the DEVLOG 457 pivot.

**Rejected alternatives.** (a) Global byte[] for all REDEFINES — rejected: that is the abolished substrate. (b) Silently independent fields with no guard — rejected: silent corruption on a real pun, the precise failure that triggered the rewrite.

### D6. SYNCHRONIZED is a no-op for in-memory typed data; honored only at the file/byte-serialization boundary (G6). BLANK WHEN ZERO and JUSTIFIED are display/store-time rules on PicInfo.

**Rationale.** Alignment has no meaning without byte addresses (the CLR aligns a `long` naturally); its only observable effect is on overlay/serialization size, which is already the byte path. BLANK-WHEN-ZERO/JUSTIFIED are pure value-rendering rules.

**Rejected alternatives.** Modeling SYNC by inserting padding fields — rejected: reintroduces layout and is invisible to typed access.

### D7. Fixed-point stays native long (unscaled, compile-time scale); 19-38 digits → Int128 via a WidePrecision flag; no decimal/BigInteger.

**Rationale.** Owner-locked (DEVLOG 462): hardware-native, exact, DISPLAY image falls out for free; Int128 is a fixed-size value type far cheaper than BigInteger.

**Rejected alternatives.** decimal (software, not hardware-native) and BigInteger (heap-allocating) — both rejected by the owner.

## C# mapping

CONCRETE COBOL→C# MAPPINGS:

— Elementary —
  05 NAME  PIC X(10) VALUE "BOB".        →  public string Name; … Name = CobolString.Store("BOB",10);
  05 CT    PIC 9(4).                      →  public long Ct;  // unscaled, scale 0; init 0L
  05 AMT   PIC S9(5)V99 COMP-3.           →  public long Amt; // unscaled (7 digits), scale=2; profile threads truncation=Packed
  05 BIG   PIC 9(30).                     →  public Int128 Big;  // WidePrecision → Int128
  05 RATE  COMP-2.                        →  public double Rate;
  05 FLAG  PIC 1.                         →  public bool Flag;

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

Two-tier. SAFE tier (corpus majority): emit independent typed fields; suppress the redefining item's auto-init unless it has its own VALUE; this is correct whenever each view is read consistently. UNSAFE tier (genuine byte-pun: write one type, read another): route the overlaid region to a classifier-scoped byte[] scratch materialized only at the overlay boundary (G6) — the legacy mechanism ISLANDED to redefined groups, not the global substrate. CRITICAL: emit a LOUD diagnostic when a cross-type read of a redefined region is detected so nothing silently corrupts — this is the exact failure mode (silent stale-byte read on a typed record) that triggered the DEVLOG 457 pivot.

### Reference modification as a RECEIVER: C# strings are immutable, so `NAME(3:2) = x` cannot splice in place.

Place.Write for a RefModPlace rebuilds the whole string: `field = field[..(s-1)] + fit(value,len) + field[(s-1+len)..]` via runtime CobolString.SpliceInto(string,start1,len,value). Evaluate s and l into temps ONCE (ISO requires single evaluation of positions/subscripts) to avoid double-eval and side-effect duplication. For numeric/COMP receivers under ref-mod (rare), defer to the byte-image fallback and flag loud.

### OCCURS DEPENDING ON: the array size varies at runtime, but a C# array has a fixed allocated length; and ISO OCCURS GR7 mandates that a RECEIVING whole-group operand uses the MAXIMUM length while a SENDING operand uses the CURRENT (DEPENDING-ON) length.

Allocate the array at MAX occurrences once; the length variable (DEPENDING ON item) bounds the LIVE range. Element access `Itm[K-1]` is unaffected. Whole-group operations branch on direction: sending → slice [0..N); receiving → full MAX (matches legacy IrOdoGroupLocation receiving:true logic, DEVLOG 290). When the DEPENDING-ON var is INSIDE the group, a receiving op still uses MAX (legacy dependOnInside rule). Bounds-check K vs N only when the EC-bound checking class is enabled (later).

### Duplicate data-names disambiguated only by qualification — the current DataBinder.ByName is a single-value Dictionary, which silently overwrites and would resolve the WRONG item.

Make ByName a MULTIMAP (Dictionary<string,List<DataItem>>). Unqualified resolution: if the list has one entry use it; if >1 and a qualifier is required for uniqueness, emit the ISO §8.4.2.2 ambiguity diagnostic. Qualified resolution: right-to-left narrowing (resolve outermost qualifier, FindChild inward) — port legacy ResolveQualifiedName + FindChild exactly.

### Multi-dimensional OCCURS: legacy flattened to a 1-D byte buffer with per-dimension stepSize multipliers — an offset-arithmetic model that is exactly the byte substrate the owner rejected.

Use the natural .NET shape: a 2-D OCCURS is an array-of-(struct-containing-array), accessed `Rows[i-1].Cols[j-1]`. Each COBOL subscript maps to its own C# array index; NO multipliers, NO stepSize, NO flattened offset. This is simpler AND layout-free. Collect dimensions by walking item→ancestors (the only piece of LocationResolver worth keeping).

### Index-names (INDEXED BY) — legacy modeled them as byte displacements into the table, which leaks layout into a value the program can SET/compare.

Model an index-name as a C# `long` holding a 1-BASED OCCURRENCE NUMBER, not a displacement. SET TO n → assign n; SET UP/DOWN BY k → ±= k; subscript use → [idx-1]; SEARCH/SEARCH ALL emit as integer loops over the occurrence range. The only behavioral difference (an index surviving a table-element-width redefine) is implementor-defined and absent from the conformance corpus.

## Edge cases

- FILLER: no C# member name needed when it carries no VALUE and is never referenced; but a FILLER WITH a VALUE must still initialize its position (matters for whole-group reads and the G6 byte image). Generate a synthetic _fillerN member only when it has a VALUE or affects group serialization.
- Group item used as an alphanumeric operand (MOVE WS-REC TO X, or IF WS-REC = SPACES): a group has no scalar field — its char image is the left-to-right concatenation of all leaf display images. Emit a generated `string AsImage()` per struct that concatenates members; whole-group MOVE/compare uses it. This is the 'whole-group alphanumeric' deferred case (architecture §3 / G6) — design the AsImage hook now even if filled later.
- Numeric item under reference modification (ISO §8.4.2.4 rule 2): operate as if redefined alphanumeric of the same size — render the raw zoned/digit display image, ref-mod that, and (if receiving) re-parse back. Defer receiving-numeric-refmod to byte fallback, flag loud.
- Subscript/position single-evaluation (ISO): `TBL(F(X)) (G(Y):H(Z))` — F,G,H must each evaluate exactly once. Emit temps for every non-trivial subscript and ref-mod position before composing the Place.
- Relative subscript `idx - 1` where idx is at occurrence 1 → index 0-1 = -1: a runtime bounds violation (EC-BOUND-SUBSCRIPT). Honor with a checked path when EC enabled; otherwise undefined (matches no-EC corpus behavior).
- Level-88 VALUE with multiple literals AND THRU ranges mixed (VALUE 0 5 THRU 9 12): the condition is an OR over each literal/range — `St==0 || (St>=5&&St<=9) || St==12`. Capture the full value-item list, not just the first (DataBinder.ExtractValue currently grabs only FirstOrDefault — a bug for 88s and for multi-literal table VALUEs).
- SET cond-name TO TRUE picks the FIRST value of a THRU range (the low bound) or the first literal (ISO §14.9.34); SET TO FALSE uses the WHEN SET TO FALSE literal if present (grammar valueClause supports it).
- JUSTIFIED RIGHT interacts with ref-mod and with numeric MOVE: JUST only applies to alphanumeric/alphabetic receivers (ISO §13.18.30) — diagnose/ignore on numeric. Already plumbed in CobolString.Store(justifiedRight).
- COMP-5 / BINARY-* with no PIC: width-bounded native int with TWO'S-COMPLEMENT WRAP on overflow (not digit truncation) — PicInfo.StorageWidth picks the byte width; the wrap path in CobolNum.Store is currently a TODO (`%= Pow10(Digits)` is wrong for BinaryCapacity). Must add the wrap before COMP-5 tests.
- 19-38 digit pictures overflow `long` (max 18 digits) → Int128. PicInfo.ClrType/DefaultInitializer/ProfileInitializer and the NumX literal renderer must branch on WidePrecision; CobolNum needs Int128 overloads. Pictures >38 (NATIONAL/2014) are out of scope for v1.
- REDEFINES of a table or by a table; REDEFINES chains (A redefines B redefines C): resolve the ultimate base; in the safe tier each is an independent typed field; in the unsafe tier all share one scratch region (G6).
- Qualification of an index-name or a LINAGE-COUNTER by file/report name (grammar dataReference alts): index-name qualification is by table name (ISO §8.4.2.2 rule 6) — resolve via the owning table; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER are special registers, not data items — they get dedicated Places.

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

- Int128 substrate timing: PicInfo.WidePrecision + CobolNum Int128 overloads are needed before any 19-38 digit picture compiles. Is there a corpus program needing >18 digits in the early NIST waves (NC/SM/IC/IF), or can Int128 wait until a later wave? (Affects whether to build it in G2 or defer.)
- COMP-5 / BINARY-* two's-complement WRAP semantics: confirm the owner wants true binary-width wrap (PIC S9(4) COMP-5 wraps at +-32768) vs digit-count truncation. The architecture says binary-wrap; the current CobolNum.Store has a digit-truncation TODO. Confirm before COMP-5 tests are in scope.
- Whole-group-as-alphanumeric (AsImage concatenation) vs the byte-image fallback: is the generated `string AsImage()` per struct acceptable as the PERMANENT typed-native mechanism for whole-group MOVE/compare (clean, readable), or must whole-group operations go through the G6 byte path for byte-exact fidelity with national/COMP members? (Pure DISPLAY groups are fine via AsImage; mixed-usage groups are the question.)
- REDEFINES cross-type-read detection: the loud guard needs a definition of 'cross-type read' precise enough to not false-positive on the safe alias pattern. Proposed: flag only when a write to view-A is followed (data-flow) by a read of view-B with an incompatible category. Is a conservative compile-time over-approximation (flag any program that both writes and reads two different-typed views of one redefined region) acceptable for v1, accepting that it routes more programs to the byte fallback than strictly necessary?
- Passing a ref-modded or subscripted-with-variable receiver as CALL BY REFERENCE: C# `ref` to an array element is legal but `ref` to a ref-mod splice is not. Confirm the policy — diagnose (strict) vs silently promote to BY CONTENT (lenient) — and whether it should be dialect-gated.
