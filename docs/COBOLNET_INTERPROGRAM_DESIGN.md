# COBOL.NET — Interprogram (CALL / cross-program data) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §9; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for cross-program data + calls in COBOL.NET (COBOL→typed-native C#→Roslyn). The load-bearing problem is BY REFERENCE on typed-native fields with NO byte window. Resolution: ONE managed-reference carrier (the typed-native re-implementation of `ManagedPointer`, internally `ManagedRef<T>`) that serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE, and SET ADDRESS OF — honoring the owner's singular-pattern rule. Crucially the carrier does NOT box WORKING-STORAGE: an ordinary `01 WS-X PIC 9(4)` stays a native `long`; a carrier is built ONLY at a call site as `ManagedRef<long>.OverField(()=>WS_X, v=>WS_X=v)` (an accessor over the caller's native field) so DISPLAY/MOVE/arithmetic keep zero indirection. The calling convention has two layers: a uniform opaque ABI (`ICobolProgram.Call(CobolArgs)`, the typed analog of the rejected `Entry(ManagedPointer[])`) for dynamic/cross-assembly CALL, and a typed fast path (direct `R=_SUB.Run(...)`) for same-assembly, statically-resolvable, PIC-conforming calls. Each program becomes its own (instantiable, not static) C# class; nested programs are nested classes; recursion/functions/methods get a per-activation instance, plain programs a cached singleton for last-used persistence. RETURNING maps to the C# method return value (idiomatic). The only sanctioned transient-byte boundary is category-mismatch BY REFERENCE (PIC X(4) arg viewed as PIC 9(4)); same-category — the common case — is always fully typed. Pointers are always-typed (never byte-gated).

## Decisions

### D1. ONE managed-reference carrier `ManagedRef<T>` (the typed-native re-implementation of `ManagedPointer`) serves BY REFERENCE args, LINKAGE items, USAGE POINTER, ADDRESS OF, BASED, ALLOCATE/FREE, SET ADDRESS OF — with two construction modes (accessor-over-native-field | standalone cell) plus a Null state.

**Rationale.** Owner singular-pattern + managed-pointer rules mandate exactly ONE carrier for 'a managed reference to a storage location'; BY REFERENCE and pointers are the same concept. Accessor-over-field means WORKING-STORAGE stays native (no boxing), so only genuine aliases (LINKAGE / ALLOCATE) carry indirection — preserving the native-field North Star.

**Rejected alternatives.** (a) `CobolRef<T>` as a NEW parallel reference type — rejected: violates singular-pattern (owner would correct it). (b) Make every aliasable item itself a `CobolRef<T>` heap cell — rejected: re-introduces a uniform indirection layer over all storage (the byte-State sin in new clothes; reads through .Value on a heap object). (c) Legacy `ManagedPointer(byte[],offset,length)` — rejected: that IS the abandoned byte substrate (DEVLOG 457); a managed ref can't serialize to stable bytes anyway. (d) C# `ref` parameters everywhere — rejected: `ref` can't be stored in a field (LINKAGE item must persist for the activation), can't be re-pointed by SET ADDRESS OF, and can't cross the opaque dynamic-CALL ABI.

### D2. Two-layer calling convention: a uniform opaque ABI (`interface ICobolProgram { int Call(CobolArgs args); void Cancel(); }`, `CobolArgs` = ordered (PassMode, caller PicMeta, carrier)) for dynamic + cross-assembly CALL; a typed fast path (direct typed method `R=_SUB.Run(carrier...)`) for same-assembly, statically-resolvable, conforming calls.

**Rationale.** The tightest constraint is `CALL identifier` and CALL to a separate `<name>.dll`: the caller can't see the callee's LINKAGE, forcing a uniform entry signature + uniform carrier array (the typed analog of the rejected `Entry(ManagedPointer[])`). Designing the opaque ABI first means the typed fast path is a pure optimization, not a retrofit.

**Rejected alternatives.** Typed ref-param signatures only — rejected: dynamic CALL and cross-assembly resolution would retrofit badly (the callee signature is unknown at the call site). Trailing-scratch-buffer RETURNING (legacy `InvokeNumericFunction`) — rejected: that is the byte ABI; RETURNING → idiomatic C# method return value instead.

### D3. Each program → its own instantiable (non-static) C# class; nested/contained programs → nested classes; the run unit compiles to one assembly with the first program as entry.

**Rationale.** Recursion, functions, and methods need a per-activation instance (§14.6.2.3); a static class cannot recurse. Plain non-recursive programs use a cached singleton instance to realize last-used persistence. Nested classes mirror COBOL nested-program scope (outer GLOBAL/COMMON visible, outer LOCAL-STORAGE not).

**Rejected alternatives.** Today's single `internal static class Program` bound to the first program unit — rejected: cannot host multiple/nested programs and cannot recurse. One assembly per program by default — deferred to an owner flag (separately-compiled `.dll` is supported via the opaque ABI but loses the typed fast path).

### D4. State→storage matrix: plain → instance fields on a cached singleton (last-used); INITIAL → re-init WS to VALUE each activation; RECURSIVE/function/method → fresh instance per activation; LOCAL-STORAGE → re-init per activation; EXTERNAL → one static run-unit holder per name (not reset by CANCEL); GLOBAL → field on the outer class visible to nested classes; LINKAGE → carrier-bound (never initialized); CANCEL → re-init to initial state on next CALL.

**Rationale.** Directly implements ISO §14.6.2.3 (initial/last-used), §8.6.6 (COMMON/INITIAL/RECURSIVE), §8.6.7 (EXTERNAL sharing), §14.9.5 (CANCEL).

**Rejected alternatives.** All-static fields (legacy) — rejected: cannot represent RECURSIVE per-activation copies or INITIAL re-init cleanly. A per-program ExecutionContext object threading byte StorageBlocks (legacy) — rejected: byte substrate.

### D5. The single sanctioned transient-byte boundary is confined to category-mismatch BY REFERENCE (e.g. a PIC X(4) argument viewed by the callee as PIC 9(4)) and to a data-pointer rebasing a differently-typed BASED item; same-category aliasing is always fully typed.

**Rationale.** `ManagedRef<string>` and `ManagedRef<long>` cannot alias the same typed location, but ISO BY REFERENCE = 'same storage area reinterpreted'. The architecture already permits a transient (never-persisted) byte image at unavoidable boundaries; confining it to category mismatch keeps the overwhelmingly common same-category case fully native.

**Rejected alternatives.** A universal byte window under all BY REFERENCE (legacy) — rejected: byte substrate for the common case. Forbidding category mismatch outright — rejected: it is legal COBOL exercised by NIST.

## C# mapping

PASSING MODES — caller side:
  BY REFERENCE: CALL "INC" USING CTR.  (01 CTR PIC 9(4))  ->  _INC.Run(ManagedRef<long>.OverField(()=>CTR, v=>CTR=v));   // callee mutation visible
  BY CONTENT:   CALL "P" USING BY CONTENT CTR.            ->  _P.Run(ManagedRef<long>.Cell(CTR));                       // copy; not visible
  BY VALUE:     CALL "P" USING BY VALUE N. (2002)         ->  _P.Run(N);                                                // plain value param
  subscripted:  CALL "P" USING TBL(I).   -> int _i=(int)I-1; _P.Run(ManagedRef<long>.OverField(()=>TBL[_i], v=>TBL[_i]=v));  // index captured once (GR3a)
  literal/expr: CALL "P" USING 5.        -> _P.Run(ManagedRef<long>.Cell(5L));                                          // inherently BY CONTENT
  OMITTED:      CALL "P" USING OMITTED.   -> _P.Run(ManagedRef<long>.Null);
CALLEE side — LINKAGE + PROCEDURE DIVISION USING:
  LINKAGE SECTION. 01 LK-CTR PIC 9(4).   PROCEDURE DIVISION USING LK-CTR RETURNING LK-R.
  ->  private ManagedRef<long> LK_CTR;
      public long Run(ManagedRef<long> p0){ LK_CTR = p0; /*proc body*/ return _ret; }   // refs to LK-CTR read/write LK_CTR.Value
  ADD 1 TO LK-CTR.  ->  LK_CTR.Value = CobolNum.Store(LK_CTR.Value + 1L, 0, _P_LK_CTR);   // the one unavoidable indirection
UNIFORM ABI (dynamic / cross-assembly):
  CALL identifier WS-PGM USING A.  ->
      var _p = Registry.Resolve(WS_PGM);
      if (_p is null) { /* ON EXCEPTION s1 | throw EC-PROGRAM-NOT-FOUND */ }
      else { int _rc = _p.Call(new CobolArgs{ Args=[ new CobolArg(PassMode.Reference, PicMeta.A, ManagedRef<...>.OverField(...)) ] }); /* RETURNING; NOT ON EXCEPTION s2 */ }
POINTERS (2002):
  01 P USAGE POINTER. 01 B PIC X(5) BASED.
  SET P TO ADDRESS OF X.       -> P = ManagedRef<string>.OverField(()=>X, v=>X=v);
  SET ADDRESS OF B TO P.       -> _B_ref = P;                          // B has no own storage; refs to B go via _B_ref.Value
  ALLOCATE B.                  -> _B_ref = ManagedRef<string>.Cell(new string(' ',5));
  ALLOCATE 5 CHARACTERS RETURNING P. -> P = ManagedRef<string>.Cell(new string(' ',5));
  FREE P.                      -> P = ManagedRef<string>.Null;         // GC reclaims
  IF P = NULL                  -> if (P.IsNull)
RETURNING / GOBACK:
  GOBACK RETURNING WS-R.  ->  _ret = WS_R; return _ret;   // (in a called program) ; in main -> terminate run unit
EXTERNAL / GLOBAL:
  01 SHARED PIC 9(4) EXTERNAL.  ->  long SHARED { get=>ExternalStore.GetL("SHARED"); set=>ExternalStore.SetL("SHARED",value); }   // one copy per run unit
  01 G PIC 9 GLOBAL. (outer)    ->  field on the OUTER class; nested classes read via the enclosing-instance reference
NESTED:
  PROGRAM-ID Inner inside Outer ->  private sealed class Inner : ICobolProgram { ... }  ; CALL "Inner" -> typed fast path to the nested class

## Hard problems

### BY REFERENCE with category mismatch (arg PIC X(4) seen as formal PIC 9(4)) — same storage reinterpreted, but ManagedRef<string> cannot alias a ManagedRef<long>.

Confine to the sanctioned transient-byte boundary: materialize the arg's value into a scratch byte image, hand the callee a ManagedRef<long> whose get/set decode/encode that buffer, write bytes back through the caller field's codec on return. Same-category (the common case) is always fully typed and never touches this path.

### BY REFERENCE of a group item — `record struct` is a value type; a closure `()=>G` copies on read, so naive aliasing loses callee mutations.

The group carrier round-trips the WHOLE struct per access: get reads a copy, set writes the entire struct back to the caller field, so subordinate-item mutations propagate as a unit at each store. OCCURS table args pass the `T[]` reference directly (element writes are naturally visible).

### RECURSIVE programs and functions/methods need per-activation data; a `static class Program` cannot recurse or hold per-activation copies.

Make each program an instantiable class. RECURSIVE/function/method → new instance per activation; plain program → cached singleton (last-used persistence). Non-RECURSIVE re-entry while active → EC-PROGRAM-RECURSIVE-CALL. This forces instance-ization of the program-class shape now (today's static Program is replaced).

### Opaque ABI for dynamic CALL / cross-assembly: the caller cannot see the callee's LINKAGE to build typed args.

Uniform ICobolProgram.Call(CobolArgs) where CobolArgs is an ordered list of (PassMode, caller PicMeta, carrier) — the typed analog of the rejected ManagedPointer[]. Callee maps positionally onto LINKAGE items. Same-assembly + statically-resolvable + PIC-conforming calls specialize to a direct typed method (Run) as a fast path.

### GOBACK / EXIT PROGRAM semantics differ between a called program and the main program; today's `throw StopRun` always unwinds the whole run unit.

In a called program, GOBACK/EXIT PROGRAM = normal method return to the caller (GOBACK RETURNING sets the return value); only in the main program (or STOP RUN anywhere) does it terminate the run unit. Refine the StopRun signal so a called program returns rather than unwinding past its caller.

### CANCEL must reset a program to initial state on next CALL, close its open files, cascade to contained programs, and be a no-op for never-called/already-canceled or active programs.

Registry.Cancel(name) drops/marks the singleton for re-init (WS reset to VALUE on next CALL), runs implicit CLOSE on its open files (§14.9.5 GR9), cascades to contained programs in reverse order (GR4), raises EC-PROGRAM-CANCEL-ACTIVE if active (GR5), no-ops if never-called/canceled (GR7); EXTERNAL data is NOT reset (GR8).

### Arguments must be evaluated exactly once at CALL time (§14.9.4.4 GR3a) even though carriers are lazy accessors.

Capture subscript/ref-mod bound expressions into locals BEFORE constructing carriers (e.g. `int _i=(int)I-1;` then close over `_i`), so re-evaluating the accessor inside the callee does not re-evaluate the COBOL subscript expression.

## Edge cases

- Transitive passing mode (§14.9.4.4 GR5): default BY REFERENCE; a BY REFERENCE/CONTENT/VALUE phrase applies to all following args until the next phrase (port the legacy CallBinder mode-threading, drop its byte emission).
- Bare argument resolution (§14.9.4.4 GR9): a bare arg with a BY REFERENCE formal becomes BY REFERENCE if it is a valid receiving operand, else BY CONTENT (e.g. a literal/expression).
- OMITTED / trailing-omitted argument (GR11-12): carrier = Null; omitted-argument condition = IsNull; referencing an omitted param otherwise → EC-PROGRAM-ARG-OMITTED.
- Argument/parameter count mismatch → EC-PROGRAM-ARG-MISMATCH (when checking enabled) or diagnostic; a missing parameter behaves as omitted.
- RETURNING a group item → compile-time error (must be elementary, BY VALUE semantics).
- RETURNING with no caller target → value discarded.
- CALL to a NULL program-pointer → EC-PROGRAM-PTR-NULL; unresolvable name → EC-PROGRAM-NOT-FOUND; both route to ON EXCEPTION if present.
- ON EXCEPTION and ON OVERFLOW are synonyms in the grammar; NOT ON EXCEPTION runs only on a successful, non-EC-propagating return (§14.9.4.4 GR3i).
- Variable-occurrence (OCCURS DEPENDING ON) BY CONTENT arg: copy the maximum size; callee honors the DEPENDING-ON value (§14.2.3 GR9).
- BASED item with no associated pointer (unallocated / after FREE): reference is undefined → IsNull guard; double FREE / use-after-free guarded.
- EXTERNAL items survive CANCEL (§14.9.5 GR8) and are shared across all programs describing the same external name (§8.6.7).
- COMMON nested program is callable by sibling contained programs (§8.4.6.3); non-COMMON only by its direct container.
- Recursive COMMON program sharing state — deterministic, allowed.
- REDEFINES-a-pointer-as-bytes or writing a pointer to a file → ISO implementor-defined; reject as undefined (a managed ref cannot serialize to stable bytes).
- ADDRESS OF as a sending operand passed BY REFERENCE across a CALL (address-identifier, §14.9.4 SR3-4) passes the carrier itself.
- ANY LENGTH formal parameter (excluded from BY REFERENCE Format-1 outermost; relevant for functions/methods) — defer to UDF/method slices but reserve carrier metadata to carry length.
- SET ADDRESS OF on a LINKAGE item re-points the callee's formal mid-execution — the carrier field is reassigned; subsequent refs read the new target.

## ISO citations

- ISO/IEC 1989:2023 §14.9.4 CALL statement (formats 1/2, syntax rules, general rules — incl. GR3a args-evaluated-once, GR3b-h resolution + EC-PROGRAM-NOT-FOUND/PTR-NULL/RESOURCES/ARG-MISMATCH/RECURSIVE-CALL, GR3i NOT ON EXCEPTION, GR5 transitive mode, GR11-12 OMITTED)
- §14.2 Procedure division structure: §14.2.2 SR1 (formal param = level 01/77 in LINKAGE, no BASED/REDEFINES), §14.2.3 GR2 positional correspondence, GR4 transitive BY REFERENCE/VALUE, GR6-7 RETURNING (storage in caller), GR8 BY REFERENCE = same storage area, GR9 BY CONTENT = allocated copy then by-reference, GR10 BY VALUE = allocated value copy
- §14.8.2 Parameters / §14.8.3 Returning items / §14.8.4 External items (conformance; EC-EXTERNAL-DATA-MISMATCH / FORMAT-CONFLICT / FILE-MISMATCH)
- §14.6.2.3 Initial and last-used states of data (§14.6.2.3.2 initial, §14.6.2.3.3 last-used); §14.6.2.4 initial state of object data
- §8.6.4 Automatic/initial/static internal items; §8.6.5 Based entries and based data items (ADDRESS OF, association lifetime); §8.6.6 Common, initial, and recursive attributes; §8.6.7 Sharing data items (EXTERNAL)
- §14.9.5 CANCEL statement (GR3 initial state on next CALL, GR4 cascade to contained, GR5 EC-PROGRAM-CANCEL-ACTIVE, GR7 no-op never-called/canceled, GR8 EXTERNAL not reset, GR9 implicit CLOSE)
- §14.9.3 ALLOCATE statement / §14.9.15 FREE statement (managed dynamic storage; INITIALIZED)
- §14.9.39 SET statement — pointer/address forms (SET p TO ADDRESS OF x; SET ADDRESS OF based-item TO p)
- §14.9.14 ENTRY statement (alternate entry points, shared WORKING-STORAGE)
- §13.18.5 BASED clause; §8.4.3 ADDRESS OF identifier; pointer-data-item reference restrictions (§ around 22728-22730)
- §8.4.6 / §8.4.6.3 Scope of names / Scope of program-names (nested + COMMON visibility, program-name resolution)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Program model: compile the whole run unit to ONE assembly (typed fast path available everywhere, nested = nested classes) vs support separately-compiled `<name>.dll` programs (uniform opaque ABI mandatory across assemblies, typed fast path unavailable). This decides how much of the convention can be fully typed.
- The one carrier name + representation: confirm the typed-native carrier may be `ManagedRef<T>` (NOT the legacy byte[]+offset+length `ManagedPointer`) — the `feedback_managed_pointers` memory text still describes the byte form, which was the abandoned byte-substrate era (DEVLOG 457). Keep the public name `ManagedPointer` (owner's choice) over the typed carrier, or rename?
- Cross-assembly dynamic CALL discovery: retain reflective auto-discovery (general profile, like legacy CobolProgramRegistry.DiscoverProgram) vs a prebuilt static registry only (AOT/WASM/trimming-safe profile). Affects deployment + trimming.
- Category-mismatch BY REFERENCE byte boundary: acceptable to materialize a transient byte image for this narrow case, or should category mismatch be a hard compile error (stricter than ISO, but keeps 100% typed)?
